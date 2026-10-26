using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GpsSettingController : ControllerBase
    {
        private const int DefaultIntervalSeconds = 20;
        private const int MinIntervalSeconds = 1;
        private const int MaxIntervalSeconds = 100;

        private readonly AppDbContext _db;

        public GpsSettingController(AppDbContext db)
        {
            _db = db;
        }

        private string CurrentUserId =>
            User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst("sub")?.Value
            ?? User?.FindFirst("id")?.Value
            ?? User?.Identity?.Name
            ?? "System";

        // ============================================================
        // GET ACTIVE SETTING
        // GET: api/GpsSetting/active
        // Available to any authenticated user (Admin or JMDO mobile client)
        // ============================================================
        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetActive()
        {
            var data = await _db.GpsSettings
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync();

            if (data == null)
            {
                return Ok(new GpsSettingDto
                {
                    Id = 0,
                    IntervalSeconds = DefaultIntervalSeconds,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            return Ok(new GpsSettingDto
            {
                Id = data.Id,
                IntervalSeconds = data.IntervalSeconds,
                IsActive = data.IsActive,
                CreatedAt = data.CreatedAt,
                CreatedBy = data.CreatedBy,
                UpdatedAt = data.UpdatedAt,
                UpdatedBy = data.UpdatedBy
            });
        }

        // ============================================================
        // GET ALL SETTINGS
        // GET: api/GpsSetting/all
        // Admin-only audit trail
        // ============================================================
        [HttpGet("all")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.GpsSettings
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new GpsSettingDto
                {
                    Id = x.Id,
                    IntervalSeconds = x.IntervalSeconds,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    CreatedBy = x.CreatedBy,
                    UpdatedAt = x.UpdatedAt,
                    UpdatedBy = x.UpdatedBy
                })
                .ToListAsync();

            return Ok(list);
        }

        // ============================================================
        // SAVE GPS SETTING
        // POST: api/GpsSetting
        // Admin-only interval configuration
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Save([FromBody] UpdateGpsSettingDto request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid GPS settings request."
                });
            }

            if (request.IntervalSeconds < MinIntervalSeconds || request.IntervalSeconds > MaxIntervalSeconds)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Tracking interval must be between {MinIntervalSeconds} and {MaxIntervalSeconds} seconds."
                });
            }

            // Deactivate existing active settings
            var activeSettings = await _db.GpsSettings
                .Where(x => x.IsActive)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var userId = CurrentUserId;

            foreach (var setting in activeSettings)
            {
                setting.IsActive = false;
                setting.UpdatedAt = now;
                setting.UpdatedBy = userId;
            }

            var entity = new GpsSetting
            {
                IntervalSeconds = request.IntervalSeconds,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = userId
            };

            _db.GpsSettings.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "GPS tracking interval updated successfully.",
                intervalSeconds = entity.IntervalSeconds,
                id = entity.Id
            });
        }
    }
}
