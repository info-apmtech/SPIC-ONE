using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    [Route("api/CropStage")]
    public class CropStageController : GenericCrudController<CropStage>
    {
        private readonly AppDbContext _db;

        public CropStageController(IGenericRepository<CropStage> repo, AppDbContext db)
            : base(repo) => _db = db;

        private string? CurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("spic:user_id");

        // CropStages currently uses PostgreSQL timestamp WITHOUT time zone.
        // Store UTC clock values with DateTimeKind.Unspecified for this column type.
        // If columns are later migrated to timestamptz, use DateTime.UtcNow directly.
        private static DateTime DatabaseNowUtc() =>
            DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        private async Task<string?> ValidateAsync(CropStage entity, int? exceptId = null)
        {
            if (entity.CropId <= 0) return "Select a crop.";
            if (string.IsNullOrWhiteSpace(entity.Name)) return "Crop Stage name is required.";
            if (entity.Name.Trim().Length > 200) return "Crop Stage name must be at most 200 characters.";
            if (!await _db.Set<Crop>().AsNoTracking()
                .AnyAsync(c => c.Id == entity.CropId && c.IsActive, HttpContext.RequestAborted))
                return "Select an existing active crop.";

            var normalized = entity.Name.Trim().ToLower();
            if (await _db.Set<CropStage>().AsNoTracking().AnyAsync(s =>
                s.CropId == entity.CropId &&
                (!exceptId.HasValue || s.Id != exceptId.Value) &&
                s.Name.Trim().ToLower() == normalized, HttpContext.RequestAborted))
                return "This Crop Stage already exists for the selected crop.";
            return null;
        }

        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] CropStage entity)
        {
            var validation = await ValidateAsync(entity);
            if (validation != null) return BadRequest(validation);
            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            var now = DatabaseNowUtc();
            var created = new CropStage
            {
                CropId = entity.CropId,
                Name = entity.Name.Trim(),
                IsActive = entity.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
                UpdatedBy = userId
            };
            _db.Set<CropStage>().Add(created);
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(new { message = "Crop Stage created successfully", data = created });
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] CropStage entity)
        {
            var existing = await _db.Set<CropStage>().FindAsync(new object[] { id }, HttpContext.RequestAborted);
            if (existing == null) return NotFound();
            var validation = await ValidateAsync(entity, id);
            if (validation != null) return BadRequest(validation);
            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            existing.CropId = entity.CropId;
            existing.Name = entity.Name.Trim();
            existing.IsActive = entity.IsActive;
            // Keep the original CreatedAt unchanged.
            existing.UpdatedAt = DatabaseNowUtc();
            existing.UpdatedBy = userId;
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(new { message = "Crop Stage updated successfully", data = existing });
        }

        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> ChangeStatus(int id, [FromQuery] bool isActive)
        {
            var existing = await _db.Set<CropStage>().FindAsync(new object[] { id }, HttpContext.RequestAborted);
            if (existing == null) return NotFound();
            var userId = CurrentUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            if (isActive && !await _db.Set<Crop>().AsNoTracking()
                .AnyAsync(c => c.Id == existing.CropId && c.IsActive, HttpContext.RequestAborted))
                return BadRequest("Cannot activate a Crop Stage for an inactive crop.");
            existing.IsActive = isActive;
            existing.UpdatedAt = DatabaseNowUtc();
            existing.UpdatedBy = userId;
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(new { message = "Crop Stage status updated successfully", data = existing });
        }
    }
}
