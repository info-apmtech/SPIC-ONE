using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
    // Reuses the existing generic CRUD routes and overrides only CropType-specific writes.
    [Authorize]
    [Route("api/CropType")]
    public class CropTypeController : GenericCrudController<CropType>
    {
        private readonly AppDbContext _db;

        public CropTypeController(IGenericRepository<CropType> repo, AppDbContext db)
            : base(repo) => _db = db;

        private string? UserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("spic:user_id");

        private async Task<string?> ValidateAsync(CropType entity, int? excludeId = null)
        {
            if (entity.CropId <= 0) return "Select a crop.";
            if (string.IsNullOrWhiteSpace(entity.Name)) return "Crop Type name is required.";
            if (entity.Name.Trim().Length > 200) return "Crop Type name must be 200 characters or fewer.";
            if (!await _db.Set<Crop>().AnyAsync(c => c.Id == entity.CropId && c.IsActive))
                return "Select an existing active crop.";

            var name = entity.Name.Trim().ToLower();
            bool duplicate = await _db.Set<CropType>().AnyAsync(x =>
                x.CropId == entity.CropId &&
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                x.Name.Trim().ToLower() == name);
            return duplicate ? "This Crop Type already exists for the selected crop." : null;
        }

        [HttpPost]
        public override async Task<IActionResult> Create([FromBody] CropType entity)
        {
            var error = await ValidateAsync(entity);
            if (error != null) return BadRequest(error);
            var userId = UserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            var now = DateTime.UtcNow;
            var cropType = new CropType
            {
                CropId = entity.CropId,
                Name = entity.Name.Trim(),
                IsActive = entity.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
                UpdatedBy = userId
            };
            _db.Set<CropType>().Add(cropType);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Crop Type created successfully", data = cropType });
        }

        [HttpPut("{id}")]
        public override async Task<IActionResult> Update(int id, [FromBody] CropType entity)
        {
            var existing = await _db.Set<CropType>().FindAsync(id);
            if (existing == null) return NotFound();
            var error = await ValidateAsync(entity, id);
            if (error != null) return BadRequest(error);
            var userId = UserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            existing.CropId = entity.CropId;
            existing.Name = entity.Name.Trim();
            existing.IsActive = entity.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Crop Type updated successfully", data = existing });
        }

        [HttpPatch("{id}/status")]
        public override async Task<IActionResult> ChangeStatus(int id, [FromQuery] bool isActive)
        {
            var existing = await _db.Set<CropType>().FindAsync(id);
            if (existing == null) return NotFound();
            var userId = UserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
            if (isActive && !await _db.Set<Crop>().AnyAsync(c => c.Id == existing.CropId && c.IsActive))
                return BadRequest("Cannot activate a Crop Type when its Crop is inactive.");
            existing.IsActive = isActive;
            existing.UpdatedBy = userId;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Crop Type status updated successfully", data = existing });
        }

        // Inherited GET /, GET /all, GET /{id}, and DELETE /{id} remain available.
        // Ensure referenced Crop Types are not deleted: configure FK Restrict on demo records.
    }
}
