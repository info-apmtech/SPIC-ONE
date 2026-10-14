using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// JMDO Attendance: one row per (UserId, Date). Marking attendance as Present or Half Day moves
    /// the JMDO to On Duty; On Leave and Absent are Off Duty. Contracts are fixed in SPIC.Core/DTOs/JMDOAttendanceDtos.cs (route list in its
    /// header). Conventions follow SasController: AsNoTracking reads, { Success = false, Message }
    /// error envelopes, audit from User.Identity?.Name.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JMDOAttendanceController : ControllerBase
    {
        private readonly AppDbContext _db;

        public JMDOAttendanceController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/JMDOAttendance/today
        [HttpGet("today")]
        public async Task<IActionResult> GetToday()
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users mark attendance here.");

            var userId = CurrentUserId();
            var today = DateTime.Today;

            var attendance = await _db.JMDOAttendances.AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

            return Ok(attendance is null ? null : Map(attendance));
        }

        // POST /api/JMDOAttendance
        [HttpPost]
        public async Task<IActionResult> UpdateAttendance([FromBody] JMDOAttendanceUpsertDto dto)
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users mark attendance here.");

            if (dto == null)
                return BadRequest(new { Success = false, Message = "Request body is required." });
            if (dto.Date == default)
                return BadRequest(new { Success = false, Message = "Date is required." });
            if (string.IsNullOrWhiteSpace(dto.VisitArea))
                return BadRequest(new { Success = false, Message = "Visit Area / Place is required." });
            if (!Enum.IsDefined(typeof(JMDOAttendanceStatus), dto.AttendanceStatus))
                return BadRequest(new { Success = false, Message = "Attendance Status is required." });
            if (!IsValidCoordinate(dto.Latitude, dto.Longitude))
                return BadRequest(new { Success = false, Message = "A valid current location (latitude/longitude) is required to mark attendance." });

            var userId = CurrentUserId();
            var date = dto.Date.Date;
            var actor = User.Identity?.Name;
            var now = DateTime.Now;

            var attendance = await _db.JMDOAttendances
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == date);

            if (attendance is null)
            {
                attendance = new JMDOAttendance
                {
                    UserId = userId,
                    UserName = actor,
                    Date = date,
                    CreatedBy = actor,
                    CreatedAt = now
                };
                _db.JMDOAttendances.Add(attendance);
            }

            attendance.VisitArea = dto.VisitArea.Trim();
            attendance.AttendanceStatus = dto.AttendanceStatus;
            attendance.DutyStatus = DutyStatusFor(dto.AttendanceStatus);
            attendance.AttendanceUpdatedAt = now;
            attendance.AttendanceLatitude = dto.Latitude;
            attendance.AttendanceLongitude = dto.Longitude;
            attendance.UpdatedBy = actor;
            attendance.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return Ok(Map(attendance));
        }

        // GET /api/JMDOAttendance/week
        // The caller's current Monday-Sunday week, one row per day. Server local time throughout,
        // the same clock GetToday/UpdateAttendance and the tracking endpoints stamp rows with.
        [HttpGet("week")]
        public async Task<IActionResult> GetWeek()
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users mark attendance here.");

            var userId = CurrentUserId();
            var now = DateTime.Now;
            var weekStart = WeekStartMonday(now.Date);
            var weekEndExclusive = weekStart.AddDays(7);

            var attendances = await _db.JMDOAttendances.AsNoTracking()
                .Where(a => a.UserId == userId && a.Date >= weekStart && a.Date < weekEndExclusive)
                .Select(a => new { a.Id, a.Date, a.AttendanceStatus, a.DutyStatus, a.VisitArea })
                .ToListAsync();
            var attendanceIds = attendances.Select(a => a.Id).ToList();

            var sessions = await _db.JMDOTrackingSessions.AsNoTracking()
                .Where(s => s.UserId == userId && attendanceIds.Contains(s.AttendanceId))
                .Select(s => new { s.Id, s.AttendanceId, s.StartTime, s.EndTime, s.Duration, s.Status, s.StartLatitude, s.StartLongitude })
                .ToListAsync();
            var sessionIds = sessions.Select(s => s.Id).ToList();

            var pointsBySession = (await _db.JMDOTrackingPoints.AsNoTracking()
                    .Where(p => sessionIds.Contains(p.TrackingSessionId))
                    .Select(p => new { p.TrackingSessionId, p.Id, p.Latitude, p.Longitude, p.RecordedAt })
                    .ToListAsync())
                .GroupBy(p => p.TrackingSessionId)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.RecordedAt).ThenBy(p => p.Id).ToList());

            var taskStarts = await TaskStartTimesAsync(userId, weekStart, weekEndExclusive);

            var durationByAttendance = new Dictionary<int, TimeSpan>();
            var distanceByAttendance = new Dictionary<int, double>();
            foreach (var s in sessions)
            {
                pointsBySession.TryGetValue(s.Id, out var points);

                // Completed: the stored Duration (EndTime - StartTime as a fallback). Active: elapsed
                // to now - unless it was started on an earlier day and never stopped, in which case
                // "now" would count the whole abandoned gap, so it ends at its last recorded point.
                TimeSpan duration;
                if (s.Status == JMDOTrackingSessionStatus.Active)
                {
                    var end = s.StartTime.Date == now.Date ? now : (points?.LastOrDefault()?.RecordedAt ?? s.StartTime);
                    duration = end - s.StartTime;
                }
                else
                {
                    duration = s.Duration ?? (s.EndTime is { } endTime ? endTime - s.StartTime : TimeSpan.Zero);
                }
                if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
                durationByAttendance[s.AttendanceId] = durationByAttendance.GetValueOrDefault(s.AttendanceId) + duration;

                // One path per session: the Start fix, then its points in RecordedAt order - never
                // joined to another session's points.
                var path = new List<(double Lat, double Lng)>();
                if (s.StartLatitude is { } startLat && s.StartLongitude is { } startLng)
                    path.Add((startLat, startLng));
                if (points != null)
                    path.AddRange(points.Select(p => (p.Latitude, p.Longitude)));
                distanceByAttendance[s.AttendanceId] = distanceByAttendance.GetValueOrDefault(s.AttendanceId) + PathDistanceKm(path);
            }

            var days = new List<JMDOWeeklyDayDto>(7);
            for (var date = weekStart; date < weekEndExclusive; date = date.AddDays(1))
            {
                var dayEnd = date.AddDays(1);
                var attendance = attendances.FirstOrDefault(a => a.Date >= date && a.Date < dayEnd);
                days.Add(new JMDOWeeklyDayDto
                {
                    Date = date,
                    AttendanceStatus = attendance?.AttendanceStatus,
                    DutyStatus = attendance?.DutyStatus,
                    VisitArea = attendance?.VisitArea,
                    WorkingDuration = attendance is null ? TimeSpan.Zero : durationByAttendance.GetValueOrDefault(attendance.Id),
                    TasksStarted = taskStarts.Count(t => t >= date && t < dayEnd),
                    DistanceKm = attendance is null ? 0 : Math.Round(distanceByAttendance.GetValueOrDefault(attendance.Id), 2)
                });
            }

            return Ok(new JMDOWeeklySummaryDto
            {
                WeekStart = weekStart,
                WeekEnd = weekStart.AddDays(6),
                Days = days,
                TotalWorkingDuration = days.Aggregate(TimeSpan.Zero, (sum, d) => sum + d.WorkingDuration),
                TotalTasksStarted = days.Sum(d => d.TasksStarted),
                TotalDistanceKm = Math.Round(days.Sum(d => d.DistanceKm), 2)
            });
        }

        // Monday of the week containing date (Sunday belongs to the week that started six days earlier).
        private static DateTime WeekStartMonday(DateTime date) =>
            date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

        // Every task the caller started in [from, to): each task row has one StartedAt, stamped once
        // (JmdoTaskAllocationController.UpdateMyTaskStatus never overwrites it), so each start counts once.
        private async Task<List<DateTime>> TaskStartTimesAsync(string userId, DateTime from, DateTime to)
        {
            var dealerStarts = await _db.JmdoTaskAllocationDealers.AsNoTracking()
                .Where(d => d.Allocation!.SubmittedByUserId == userId && d.StartedAt >= from && d.StartedAt < to)
                .Select(d => d.StartedAt!.Value)
                .ToListAsync();

            var programStarts = await _db.JmdoTaskAllocationPrograms.AsNoTracking()
                .Where(p => p.Allocation!.SubmittedByUserId == userId && p.StartedAt >= from && p.StartedAt < to)
                .Select(p => p.StartedAt!.Value)
                .ToListAsync();

            // SPCM / soil / PoS are one-per-allocation, so their StartedAt lives on the header row.
            var headerStarts = await _db.JmdoTaskAllocations.AsNoTracking()
                .Where(a => a.SubmittedByUserId == userId
                    && ((a.SpcmStartedAt >= from && a.SpcmStartedAt < to)
                        || (a.SoilSampleStartedAt >= from && a.SoilSampleStartedAt < to)
                        || (a.PosLiquidationStartedAt >= from && a.PosLiquidationStartedAt < to)))
                .Select(a => new { a.SpcmStartedAt, a.SoilSampleStartedAt, a.PosLiquidationStartedAt })
                .ToListAsync();

            var starts = dealerStarts.Concat(programStarts).ToList();
            foreach (var h in headerStarts)
                foreach (var started in new[] { h.SpcmStartedAt, h.SoilSampleStartedAt, h.PosLiquidationStartedAt })
                    if (started is { } t && t >= from && t < to)
                        starts.Add(t);

            return starts;
        }

        // Straight-line (haversine) distance along consecutive valid points - an estimate from
        // periodic GPS fixes, not road distance. Invalid coordinates are skipped, not joined across.
        private static double PathDistanceKm(List<(double Lat, double Lng)> path)
        {
            const double EarthRadiusKm = 6371.0;
            double total = 0;
            (double Lat, double Lng)? previous = null;

            foreach (var point in path)
            {
                if (!IsValidCoordinate(point.Lat, point.Lng)) continue;

                if (previous is { } p)
                {
                    var dLat = (point.Lat - p.Lat) * Math.PI / 180;
                    var dLng = (point.Lng - p.Lng) * Math.PI / 180;
                    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                        + Math.Cos(p.Lat * Math.PI / 180) * Math.Cos(point.Lat * Math.PI / 180)
                        * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
                    total += EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                }
                previous = point;
            }

            return total;
        }

        // Present and Half Day put the JMDO on duty; On Leave and Absent are Off Duty.
        private static JMDODutyStatus DutyStatusFor(JMDOAttendanceStatus status) => status switch
        {
            JMDOAttendanceStatus.Present or JMDOAttendanceStatus.HalfDay => JMDODutyStatus.OnDuty,
            _ => JMDODutyStatus.OffDuty
        };

        // ---------------------------------------------------------------- identity

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private AppRole? CurrentRole()
        {
            var raw = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<AppRole>(raw, true, out var role) ? role : null;
        }

        private bool IsJmdo() => CurrentRole() == AppRole.JMDO;

        private static bool IsValidCoordinate(double? latitude, double? longitude)
        {
            if (latitude is null || longitude is null) return false;
            if (double.IsNaN(latitude.Value) || double.IsInfinity(latitude.Value)) return false;
            if (double.IsNaN(longitude.Value) || double.IsInfinity(longitude.Value)) return false;
            return latitude.Value is >= -90 and <= 90 && longitude.Value is >= -180 and <= 180;
        }

        private ObjectResult Forbid403(string message) =>
            StatusCode(403, new { Success = false, Message = message });

        private static JMDOAttendanceDto Map(JMDOAttendance a) => new()
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = a.UserName,
            Date = a.Date,
            VisitArea = a.VisitArea ?? "",
            AttendanceStatus = a.AttendanceStatus,
            DutyStatus = a.DutyStatus,
            AttendanceUpdatedAt = a.AttendanceUpdatedAt,
            AttendanceLatitude = a.AttendanceLatitude,
            AttendanceLongitude = a.AttendanceLongitude
        };
    }
}
