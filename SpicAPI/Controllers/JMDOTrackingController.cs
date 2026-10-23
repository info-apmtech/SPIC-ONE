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
    /// JMDO GPS Tracking: Start Tracking plus the points captured while a session is Active.
    /// A session belongs to the caller's own JMDOAttendance row for today. Only one Active session
    /// per JMDO at a time - enforced here by returning the existing one instead of creating another.
    ///
    /// Deliberately independent of attendance's DutyStatus and of every task-allocation event:
    /// marking attendance, going off duty, starting/completing/closing a task, and an MDO approving
    /// or rejecting a task never start or stop a tracking session. The ONLY way a session reaches
    /// Status=Completed is the JMDO calling Stop directly. A session that's gone silent too long is
    /// auto-ended by CloseStaleSessionsAsync into the separate AutoEnded status instead, specifically
    /// so "the JMDO stopped tracking" and "the server gave up waiting" are never reported as the same
    /// thing anywhere in the API or UI.
    ///
    /// Conventions follow JMDOAttendanceController: AsNoTracking reads, { Success = false, Message }
    /// error envelopes, audit from User.Identity?.Name, UserId resolved from ClaimTypes.NameIdentifier
    /// only.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JMDOTrackingController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public JMDOTrackingController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private int IntervalSeconds => _config.GetValue<int?>("JmdoTracking:IntervalSeconds") ?? 60;
        private double AutoEndAfterHours => _config.GetValue<double?>("JmdoTracking:AutoEndAfterHours") ?? 24;

        // POST /api/JMDOTracking/start
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] JMDOTrackingStartDto dto)
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            var userId = CurrentUserId();
            var today = DateTime.Today;

            // A session that's been silent too long must not be resumed as if it were still live -
            // see the class doc comment. Closed at its last recorded point (or its own start, if it
            // has none), not "now", so the silent gap isn't counted as working time.
            await CloseStaleSessionsAsync(userId);

            // Idempotent on purpose: a JMDO can only have one Active session at a time, so
            // returning the existing one (instead of creating another) is both the duplicate-
            // prevention rule and how the dashboard "resumes" an in-progress session on reload.
            var existing = await _db.JMDOTrackingSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == JMDOTrackingSessionStatus.Active);
            if (existing is not null)
                return Ok(await MapAsync(existing));

            // Attendance marked today is still required (so AttendanceId is always a real FK, and
            // tracking stays something only a JMDO who showed up for the day can start) - but
            // DutyStatus itself is deliberately NOT checked: Live Tracking is its own explicit
            // action, independent of the attendance/duty-status value.
            var attendance = await _db.JMDOAttendances
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);
            if (attendance is null)
                return BadRequest(new { Success = false, Message = "Mark today's attendance before starting live tracking." });

            if (dto == null || !IsValidCoordinate(dto.Latitude, dto.Longitude))
                return BadRequest(new { Success = false, Message = "A valid current location (latitude/longitude) is required to start tracking." });

            var actor = User.Identity?.Name;
            var now = DateTime.Now;

            var session = new JMDOTrackingSession
            {
                AttendanceId = attendance.Id,
                UserId = userId,
                UserName = actor,
                StartTime = now,
                StartLatitude = dto.Latitude,
                StartLongitude = dto.Longitude,
                Status = JMDOTrackingSessionStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.JMDOTrackingSessions.Add(session);
            await _db.SaveChangesAsync();

            return Ok(await MapAsync(session));
        }

        // GET /api/JMDOTracking/mine/active
        // The caller's current Active session (with its computed LiveStatus), or 204 if none - lets
        // any page (FieldDashboard, a future header widget) reflect real tracking state without the
        // side effect of starting a session just to find out.
        [HttpGet("mine/active")]
        public async Task<IActionResult> MineActive()
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            var userId = CurrentUserId();
            var session = await _db.JMDOTrackingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == JMDOTrackingSessionStatus.Active);

            if (session is null)
                return NoContent();

            return Ok(await MapAsync(session));
        }

        // GET /api/JMDOTracking/mine/latest
        // The caller's most recent session today, regardless of status - unlike MineActive() this
        // also returns a session that's already Completed/AutoEnded, so a page reached right after
        // Stop (MarkAttendance's "View Tracking Details") can still show the route that was just
        // recorded instead of finding nothing.
        [HttpGet("mine/latest")]
        public async Task<IActionResult> MineLatest()
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            var userId = CurrentUserId();
            var today = DateTime.Today;
            var session = await _db.JMDOTrackingSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.StartTime >= today)
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefaultAsync();

            if (session is null)
                return NoContent();

            return Ok(await MapAsync(session));
        }

        // GET /api/JMDOTracking/mine/today
        // Every one of the caller's own sessions that started today, oldest first - a JMDO can
        // Start/Stop Live Tracking more than once in a day (separate site visits), and each one is
        // its own session/route; MineActive/MineLatest only ever surface a single session, so
        // MarkAttendance's session picker uses this to let the JMDO switch between all of them.
        [HttpGet("mine/today")]
        public async Task<IActionResult> MineToday()
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            var userId = CurrentUserId();
            var today = DateTime.Today;
            var sessions = await _db.JMDOTrackingSessions
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.StartTime >= today)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var result = new List<JMDOTrackingSessionDto>(sessions.Count);
            foreach (var session in sessions)
                result.Add(await MapAsync(session));

            return Ok(result);
        }

        // POST /api/JMDOTracking/sessions/{sessionId}/points
        [HttpPost("sessions/{sessionId:int}/points")]
        public async Task<IActionResult> AddPoint(int sessionId, [FromBody] JMDOTrackingPointUpsertDto dto)
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            if (dto == null || !IsValidCoordinate(dto.Latitude, dto.Longitude))
                return BadRequest(new { Success = false, Message = "A valid latitude/longitude is required." });

            var userId = CurrentUserId();
            var session = await _db.JMDOTrackingSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session is null)
                return NotFound(new { Success = false, Message = "Tracking session not found." });
            if (session.Status != JMDOTrackingSessionStatus.Active)
                return BadRequest(new { Success = false, Message = "This tracking session is no longer active." });

            var now = DateTime.Now;
            _db.JMDOTrackingPoints.Add(NewPoint(session.Id, dto, now));
            await _db.SaveChangesAsync();

            return Ok();
        }

        // POST /api/JMDOTracking/sessions/{sessionId}/points/batch
        // Offline-queue flush-on-reconnect: a device that collected points while it had no
        // connectivity uploads its backlog here in one call once it's back online. Each point keeps
        // its own DeviceCapturedAt, so a flushed backlog still reflects real movement history instead
        // of every point collapsing onto the moment of the upload.
        [HttpPost("sessions/{sessionId:int}/points/batch")]
        public async Task<IActionResult> AddPointsBatch(int sessionId, [FromBody] List<JMDOTrackingPointUpsertDto> points)
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            if (points == null || points.Count == 0)
                return BadRequest(new { Success = false, Message = "At least one point is required." });
            if (points.Count > 500)
                return BadRequest(new { Success = false, Message = "At most 500 points per batch." });
            if (points.Any(p => !IsValidCoordinate(p.Latitude, p.Longitude)))
                return BadRequest(new { Success = false, Message = "Every point requires a valid latitude/longitude." });

            var userId = CurrentUserId();
            var session = await _db.JMDOTrackingSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session is null)
                return NotFound(new { Success = false, Message = "Tracking session not found." });
            if (session.Status != JMDOTrackingSessionStatus.Active)
                return BadRequest(new { Success = false, Message = "This tracking session is no longer active." });

            var now = DateTime.Now;
            var ordered = points
                .OrderBy(p => p.DeviceCapturedAt ?? now)
                .Select(p => NewPoint(session.Id, p, now));

            _db.JMDOTrackingPoints.AddRange(ordered);
            await _db.SaveChangesAsync();

            return Ok();
        }

        // POST /api/JMDOTracking/stop
        // The ONLY path in this controller that sets Status=Completed. Never called by attendance,
        // never called by any task-allocation event, never called automatically.
        [HttpPost("stop")]
        public async Task<IActionResult> Stop([FromBody] JMDOTrackingStopDto dto)
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            if (dto == null || !IsValidCoordinate(dto.Latitude, dto.Longitude))
                return BadRequest(new { Success = false, Message = "A valid current location (latitude/longitude) is required to stop tracking." });

            var userId = CurrentUserId();

            // No session id is accepted from the client - the caller's one Active session is
            // resolved the same way Start resolves it. This also makes a repeat call safe: once
            // stopped there is no Active session left to find, so a duplicate click/request gets
            // a clear "nothing to stop" error instead of completing the session twice.
            var session = await _db.JMDOTrackingSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == JMDOTrackingSessionStatus.Active);

            if (session is null)
                return BadRequest(new { Success = false, Message = "No active tracking session found." });

            var now = DateTime.Now;

            var point = new JMDOTrackingPoint
            {
                TrackingSessionId = session.Id,
                Latitude = dto.Latitude!.Value,
                Longitude = dto.Longitude!.Value,
                RecordedAt = now,
                Accuracy = dto.Accuracy,
                CreatedAt = now,
                DeviceCapturedAt = now
            };
            _db.JMDOTrackingPoints.Add(point);

            session.EndTime = now;
            session.Duration = now - session.StartTime;
            session.Status = JMDOTrackingSessionStatus.Completed;
            session.EndLatitude = dto.Latitude;
            session.EndLongitude = dto.Longitude;
            session.UpdatedAt = now;

            // Attendance's DutyStatus is deliberately left untouched - tracking lifecycle no longer
            // has any side effect on attendance, in either direction (see Start and the class doc
            // comment).

            await _db.SaveChangesAsync();

            return Ok(new JMDOTrackingSessionDto
            {
                Id = session.Id,
                AttendanceId = session.AttendanceId,
                UserId = session.UserId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.Duration,
                StartLatitude = session.StartLatitude,
                StartLongitude = session.StartLongitude,
                Status = session.Status,
                LiveStatus = JmdoTrackingLiveStatus.Stopped,
                LastPointAt = point.RecordedAt,
                LastLatitude = point.Latitude,
                LastLongitude = point.Longitude
            });
        }

        // GET /api/JMDOTracking/team
        // Supervisor visibility: every active-login JMDO at the caller's own HQ (same scope
        // TeamController.TeamJmdoLogins uses for api/Team/jmdos - an MDO/MO/Admin here can only ever
        // see their own HQ's team, never cross-HQ), each with their current tracking status.
        [Authorize(Roles = "Admin,MO,MDO")]
        [HttpGet("team")]
        public async Task<IActionResult> Team()
        {
            var hqId = CurrentHqId();
            if (hqId is not > 0)
                return Ok(new List<JMDOTrackingTeamRowDto>());

            var team = await (
                from login in _db.Employeelogins
                join info in _db.EmployeeInformation on login.EmployeeInformationID equals info.Id
                where login.Role == AppRole.JMDO && login.HeadquartersId == hqId.Value && login.IsActive
                orderby info.Name
                select new { login.UserId, info.Name }
                ).ToListAsync();

            if (team.Count == 0)
                return Ok(new List<JMDOTrackingTeamRowDto>());

            var userIds = team.Select(t => t.UserId).ToList();

            // Latest session per user (Active first if one exists, else the most recently started)
            // - a JMDO with no session at all simply gets no row's worth of session data, surfaced
            // as LiveStatus = AutoEnded-like "never started" via a null session below.
            var sessions = await _db.JMDOTrackingSessions
                .AsNoTracking()
                .Where(s => userIds.Contains(s.UserId))
                .ToListAsync();

            var latestByUser = sessions
                .GroupBy(s => s.UserId)
                .ToDictionary(g => g.Key, g => g
                    .OrderByDescending(s => s.Status == JMDOTrackingSessionStatus.Active)
                    .ThenByDescending(s => s.StartTime)
                    .First());

            var sessionIds = latestByUser.Values.Select(s => s.Id).ToList();
            var lastPoints = await _db.JMDOTrackingPoints
                .AsNoTracking()
                .Where(p => sessionIds.Contains(p.TrackingSessionId))
                .GroupBy(p => p.TrackingSessionId)
                .Select(g => g.OrderByDescending(p => p.RecordedAt).First())
                .ToListAsync();
            var lastPointBySession = lastPoints.ToDictionary(p => p.TrackingSessionId);

            var rows = team.Select(t =>
            {
                latestByUser.TryGetValue(t.UserId, out var session);
                lastPointBySession.TryGetValue(session?.Id ?? 0, out var lastPoint);

                return new JMDOTrackingTeamRowDto
                {
                    UserId = t.UserId,
                    JmdoName = t.Name,
                    LiveStatus = session is null ? JmdoTrackingLiveStatus.NotStarted : ResolveLiveStatus(session, lastPoint?.RecordedAt),
                    LastPointAt = lastPoint?.RecordedAt,
                    LastLatitude = lastPoint?.Latitude,
                    LastLongitude = lastPoint?.Longitude,
                    SessionStartTime = session?.StartTime
                };
            }).ToList();

            return Ok(rows);
        }

        // GET /api/JMDOTracking/sessions/{sessionId}/points
        // Oldest-first points for drawing a session's route on a map. A JMDO can only ever fetch
        // their own session (any status - including a Stopped/AutoEnded one, so a past day's route
        // can still be reviewed); an Admin/MO/MDO can fetch any session belonging to a JMDO on their
        // own HQ team, same scope as Team() - never cross-HQ. Capped at 3000 points (~16h of 20s-
        // interval tracking) so one request can't pull an unbounded payload.
        [HttpGet("sessions/{sessionId:int}/points")]
        public async Task<IActionResult> GetSessionPoints(int sessionId)
        {
            var session = await _db.JMDOTrackingSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session is null)
                return NotFound(new { Success = false, Message = "Tracking session not found." });

            var userId = CurrentUserId();
            if (session.UserId != userId)
            {
                if (!IsSupervisor())
                    return Forbid403("You can only view your own tracking sessions.");

                var hqId = CurrentHqId();
                var sameHq = hqId is > 0 && await _db.Employeelogins
                    .AsNoTracking()
                    .AnyAsync(l => l.UserId == session.UserId && l.HeadquartersId == hqId.Value);
                if (!sameHq)
                    return Forbid403("You can only view tracking sessions for your own HQ team.");
            }

            var points = await _db.JMDOTrackingPoints
                .AsNoTracking()
                .Where(p => p.TrackingSessionId == sessionId)
                .OrderBy(p => p.RecordedAt)
                .Take(3000)
                .Select(p => new JMDOTrackingPointDto
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    RecordedAt = p.RecordedAt,
                    DeviceCapturedAt = p.DeviceCapturedAt,
                    Accuracy = p.Accuracy
                })
                .ToListAsync();

            return Ok(points);
        }

        // Ends the caller's Active session(s) that have gone silent for longer than
        // JmdoTracking:AutoEndAfterHours, at their last recorded point (or their own start, if they
        // have none) - never "now", so the silent gap isn't counted as working time. Status =
        // AutoEnded, never Completed (see the class doc comment) - this is server hygiene, not the
        // JMDO choosing to stop.
        private async Task CloseStaleSessionsAsync(string userId)
        {
            var cutoff = DateTime.Now.AddHours(-AutoEndAfterHours);

            var stale = await _db.JMDOTrackingSessions
                .Where(s => s.UserId == userId && s.Status == JMDOTrackingSessionStatus.Active)
                .Select(s => new
                {
                    Session = s,
                    LastPointAt = s.Points.Max(p => (DateTime?)p.RecordedAt)
                })
                .ToListAsync();

            var toClose = stale
                .Where(item => (item.LastPointAt ?? item.Session.StartTime) < cutoff)
                .ToList();
            if (toClose.Count == 0) return;

            var now = DateTime.Now;
            foreach (var item in toClose)
            {
                var end = item.LastPointAt is { } last && last > item.Session.StartTime ? last : item.Session.StartTime;
                item.Session.EndTime = end;
                item.Session.Duration = end - item.Session.StartTime;
                item.Session.Status = JMDOTrackingSessionStatus.AutoEnded;
                item.Session.UpdatedAt = now;
            }

            await _db.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- mapping / status

        private static JMDOTrackingPoint NewPoint(int sessionId, JMDOTrackingPointUpsertDto dto, DateTime now) => new()
        {
            TrackingSessionId = sessionId,
            Latitude = dto.Latitude!.Value,
            Longitude = dto.Longitude!.Value,
            RecordedAt = now,
            Accuracy = dto.Accuracy,
            CreatedAt = now,
            DeviceCapturedAt = dto.DeviceCapturedAt ?? now
        };

        private async Task<JMDOTrackingSessionDto> MapAsync(JMDOTrackingSession s)
        {
            var lastPoint = await _db.JMDOTrackingPoints
                .AsNoTracking()
                .Where(p => p.TrackingSessionId == s.Id)
                .OrderByDescending(p => p.RecordedAt)
                .FirstOrDefaultAsync();

            return new JMDOTrackingSessionDto
            {
                Id = s.Id,
                AttendanceId = s.AttendanceId,
                UserId = s.UserId,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Duration = s.Duration,
                StartLatitude = s.StartLatitude,
                StartLongitude = s.StartLongitude,
                Status = s.Status,
                LiveStatus = ResolveLiveStatus(s, lastPoint?.RecordedAt),
                LastPointAt = lastPoint?.RecordedAt,
                LastLatitude = lastPoint?.Latitude,
                LastLongitude = lastPoint?.Longitude
            };
        }

        // Stopped/AutoEnded reflect the stored Status (set exactly once, at Stop or at
        // CloseStaleSessionsAsync, never recomputed). For a still-Active session, Live/Stale/Offline
        // is derived purely from how long ago the server actually received a point - never from
        // anything a device claims about its own local/offline-queue state, so a device that's been
        // silent for an hour reads Offline here even while it's still quietly collecting points to
        // flush later.
        private JmdoTrackingLiveStatus ResolveLiveStatus(JMDOTrackingSession session, DateTime? lastPointAt)
        {
            if (session.Status == JMDOTrackingSessionStatus.Completed) return JmdoTrackingLiveStatus.Stopped;
            if (session.Status == JMDOTrackingSessionStatus.AutoEnded) return JmdoTrackingLiveStatus.AutoEnded;
            if (session.Status == JMDOTrackingSessionStatus.Cancelled) return JmdoTrackingLiveStatus.Stopped;

            var since = DateTime.Now - (lastPointAt ?? session.StartTime);
            if (since <= TimeSpan.FromSeconds(IntervalSeconds * 2)) return JmdoTrackingLiveStatus.Live;
            if (since <= TimeSpan.FromMinutes(15)) return JmdoTrackingLiveStatus.Stale;
            return JmdoTrackingLiveStatus.Offline;
        }

        // ---------------------------------------------------------------- identity

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private AppRole? CurrentRole()
        {
            var raw = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<AppRole>(raw, true, out var role) ? role : null;
        }

        private bool IsJmdo() => CurrentRole() == AppRole.JMDO;

        private bool IsSupervisor() => CurrentRole() is AppRole.Admin or AppRole.MO or AppRole.MDO;

        // Same claim-reading rule as JmdoTaskAllocationController/TeamController, kept local to this
        // controller since Team() is the only action here that needs it.
        private int? CurrentHqId()
        {
            var value = User.FindFirst("spic:hq_id")?.Value;
            return int.TryParse(value, out var id) && id > 0 ? id : null;
        }

        private ObjectResult Forbid403(string message) =>
            StatusCode(403, new { Success = false, Message = message });

        private static bool IsValidCoordinate(double? latitude, double? longitude)
        {
            if (latitude is null || longitude is null) return false;
            if (double.IsNaN(latitude.Value) || double.IsInfinity(latitude.Value)) return false;
            if (double.IsNaN(longitude.Value) || double.IsInfinity(longitude.Value)) return false;
            return latitude.Value is >= -90 and <= 90 && longitude.Value is >= -180 and <= 180;
        }
    }
}
