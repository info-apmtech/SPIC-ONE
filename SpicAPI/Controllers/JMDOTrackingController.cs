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
    /// Stop Tracking, duration and View Tracking Details are a later step. Conventions follow
    /// JMDOAttendanceController: AsNoTracking reads, { Success = false, Message } error envelopes,
    /// audit from User.Identity?.Name, UserId resolved from ClaimTypes.NameIdentifier only.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JMDOTrackingController : ControllerBase
    {
        private readonly AppDbContext _db;

        public JMDOTrackingController(AppDbContext db)
        {
            _db = db;
        }

        // POST /api/JMDOTracking/start
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] JMDOTrackingStartDto dto)
        {
            if (!IsJmdo())
                return Forbid403("Only JMDO users track field visits here.");

            var userId = CurrentUserId();

            // Idempotent on purpose: a JMDO can only have one Active session at a time, so
            // returning the existing one (instead of creating another) is both the duplicate-
            // prevention rule and how the dashboard "resumes" an in-progress session on reload.
            var existing = await _db.JMDOTrackingSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == JMDOTrackingSessionStatus.Active);
            if (existing is not null)
                return Ok(Map(existing));

            var today = DateTime.Today;
            var attendance = await _db.JMDOAttendances
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);
            if (attendance is null)
                return BadRequest(new { Success = false, Message = "Mark today's attendance before starting tracking." });

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

            return Ok(Map(session));
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
            var point = new JMDOTrackingPoint
            {
                TrackingSessionId = session.Id,
                Latitude = dto.Latitude!.Value,
                Longitude = dto.Longitude!.Value,
                RecordedAt = now,
                Accuracy = dto.Accuracy,
                CreatedAt = now
            };

            _db.JMDOTrackingPoints.Add(point);
            await _db.SaveChangesAsync();

            return Ok();
        }

        // POST /api/JMDOTracking/stop
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
                CreatedAt = now
            };
            _db.JMDOTrackingPoints.Add(point);

            session.EndTime = now;
            session.Duration = now - session.StartTime;
            session.Status = JMDOTrackingSessionStatus.Completed;
            session.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return Ok(Map(session));
        }

        // ---------------------------------------------------------------- identity

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private AppRole? CurrentRole()
        {
            var raw = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<AppRole>(raw, true, out var role) ? role : null;
        }

        private bool IsJmdo() => CurrentRole() == AppRole.JMDO;

        private ObjectResult Forbid403(string message) =>
            StatusCode(403, new { Success = false, Message = message });

        private static bool IsValidCoordinate(double? latitude, double? longitude)
        {
            if (latitude is null || longitude is null) return false;
            if (double.IsNaN(latitude.Value) || double.IsInfinity(latitude.Value)) return false;
            if (double.IsNaN(longitude.Value) || double.IsInfinity(longitude.Value)) return false;
            return latitude.Value is >= -90 and <= 90 && longitude.Value is >= -180 and <= 180;
        }

        private static JMDOTrackingSessionDto Map(JMDOTrackingSession s) => new()
        {
            Id = s.Id,
            AttendanceId = s.AttendanceId,
            UserId = s.UserId,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Duration = s.Duration,
            StartLatitude = s.StartLatitude,
            StartLongitude = s.StartLongitude,
            Status = s.Status
        };
    }
}
