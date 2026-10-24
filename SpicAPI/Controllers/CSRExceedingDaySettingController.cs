using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CSRExceedingDaySettingController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CSRExceedingDaySettingController(AppDbContext db)
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
        // GET ALL
        // GET: api/CSRExceedingDaySetting/all
        // ============================================================

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _db.CSRExceedingDaySettings
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(data);
        }


        // ============================================================
        // GET ACTIVE SETTING
        // GET: api/CSRExceedingDaySetting/active
        // ============================================================

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var data = await _db.CSRExceedingDaySettings
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync();

            if (data == null)
            {
                return NotFound(new
                {
                    message = "No active CSR exceeding day setting found."
                });
            }

            return Ok(data);
        }


        // ============================================================
        // GET BY ID
        // GET: api/CSRExceedingDaySetting/1
        // ============================================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _db.CSRExceedingDaySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (data == null)
            {
                return NotFound(new
                {
                    message = "CSR exceeding day setting not found."
                });
            }

            return Ok(data);
        }


        // ============================================================
        // CREATE
        // POST: api/CSRExceedingDaySetting
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CSRExceedingDaySetting request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Invalid request."
                });
            }


            if (request.CSR1Day <= 0 || request.CSR1Day > 31)
            {
                return BadRequest(new
                {
                    message = "CSR1 Day must be between 1 and 31."
                });
            }


            if (request.CSR2Day <= 0 || request.CSR2Day > 31)
            {
                return BadRequest(new
                {
                    message = "CSR2 Day must be between 1 and 31."
                });
            }


            // If new setting is Active, make all old settings inactive
            if (request.IsActive)
            {
                var activeRows = await _db.CSRExceedingDaySettings
                    .Where(x => x.IsActive)
                    .ToListAsync();

                foreach (var row in activeRows)
                {
                    row.IsActive = false;
                    row.UpdatedAt = DateTime.UtcNow;
                    row.UpdatedBy = CurrentUserId;
                }
            }


            var entity = new CSRExceedingDaySetting
            {
                CSR1Day = request.CSR1Day,
                CSR2Day = request.CSR2Day,
                IsActive = request.IsActive,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUserId
            };


            _db.CSRExceedingDaySettings.Add(entity);

            await _db.SaveChangesAsync();


            return Ok(new
            {
                message = "CSR exceeding day setting created successfully.",
                id = entity.Id
            });
        }


        // ============================================================
        // UPDATE
        // PUT: api/CSRExceedingDaySetting/1
        // ============================================================

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] CSRExceedingDaySetting request)
        {
            var entity = await _db.CSRExceedingDaySettings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound(new
                {
                    message = "CSR exceeding day setting not found."
                });
            }


            if (request.CSR1Day <= 0 || request.CSR1Day > 31)
            {
                return BadRequest(new
                {
                    message = "CSR1 Day must be between 1 and 31."
                });
            }


            if (request.CSR2Day <= 0 || request.CSR2Day > 31)
            {
                return BadRequest(new
                {
                    message = "CSR2 Day must be between 1 and 31."
                });
            }


            // Only one Active record
            if (request.IsActive)
            {
                var otherActiveRows =
                    await _db.CSRExceedingDaySettings
                        .Where(x =>
                            x.Id != id &&
                            x.IsActive)
                        .ToListAsync();

                foreach (var row in otherActiveRows)
                {
                    row.IsActive = false;
                    row.UpdatedAt = DateTime.UtcNow;
                    row.UpdatedBy = CurrentUserId;
                }
            }


            entity.CSR1Day = request.CSR1Day;
            entity.CSR2Day = request.CSR2Day;
            entity.IsActive = request.IsActive;

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = CurrentUserId;


            await _db.SaveChangesAsync();


            return Ok(new
            {
                message = "CSR exceeding day setting updated successfully."
            });
        }


        // ============================================================
        // STATUS
        // PATCH: api/CSRExceedingDaySetting/1/status?isActive=true
        // ============================================================

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromQuery] bool isActive)
        {
            var entity = await _db.CSRExceedingDaySettings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound(new
                {
                    message = "CSR exceeding day setting not found."
                });
            }


            // When activating this record,
            // deactivate all other records.
            if (isActive)
            {
                var otherRows =
                    await _db.CSRExceedingDaySettings
                        .Where(x =>
                            x.Id != id &&
                            x.IsActive)
                        .ToListAsync();

                foreach (var row in otherRows)
                {
                    row.IsActive = false;
                    row.UpdatedAt = DateTime.UtcNow;
                    row.UpdatedBy = CurrentUserId;
                }
            }


            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = CurrentUserId;


            await _db.SaveChangesAsync();


            return Ok(new
            {
                message = isActive
                    ? "Setting activated successfully."
                    : "Setting deactivated successfully."
            });
        }


        // ============================================================
        // DELETE
        // DELETE: api/CSRExceedingDaySetting/1
        // ============================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.CSRExceedingDaySettings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
            {
                return NotFound(new
                {
                    message = "CSR exceeding day setting not found."
                });
            }


            _db.CSRExceedingDaySettings.Remove(entity);

            await _db.SaveChangesAsync();


            return Ok(new
            {
                message = "CSR exceeding day setting deleted successfully."
            });
        }
    }
}