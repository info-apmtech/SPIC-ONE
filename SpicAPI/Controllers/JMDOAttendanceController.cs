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
    /// JMDO Attendance: one row per (UserId, Date). Marking attendance also moves the JMDO to
    /// On Duty. Contracts are fixed in SPIC.Core/DTOs/JMDOAttendanceDtos.cs (route list in its
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
            attendance.DutyStatus = JMDODutyStatus.OnDuty;
            attendance.AttendanceUpdatedAt = now;
            attendance.AttendanceLatitude = dto.Latitude;
            attendance.AttendanceLongitude = dto.Longitude;
            attendance.UpdatedBy = actor;
            attendance.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return Ok(Map(attendance));
        }

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
