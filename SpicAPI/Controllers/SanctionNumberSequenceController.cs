using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    // IMPORTANT: replace AppDbContext and its namespace with the project's real DbContext.
    // Configure DbSet<SanctionNumberSequence> SanctionNumberSequences in that context.
    [ApiController]
    [Route("api/SanctionNumberSequence")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SanctionNumberSequenceController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SanctionNumberSequenceController(AppDbContext db) => _db = db;

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private static SanctionNumberSequenceDto ToDto(SanctionNumberSequence x) => new()
        {
            Id = x.Id,
            StateId = x.StateId,
            RegionId = x.RegionId,
            StateSAPCode = x.StateSAPCode,
            RegionSAPCode = x.RegionSAPCode,
            SanctionNumber = x.SanctionNumber,
            SanctionNumberState = x.SanctionNumberState,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt
        };

        [HttpGet]
        public async Task<IActionResult> List() => Ok((await _db.SanctionNumberSequences
            .AsNoTracking().OrderBy(x => x.StateId).ThenBy(x => x.RegionId)
            .ToListAsync()).Select(ToDto));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var row = await _db.SanctionNumberSequences.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return row == null ? NotFound() : Ok(ToDto(row));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSanctionNumberSequenceDto dto)
        {
            if (dto.RegionId.HasValue && dto.RegionId.Value <= 0)
                return BadRequest(new { message = "RegionId must be positive or null." });
            if (dto.RegionId.HasValue && string.IsNullOrWhiteSpace(dto.RegionSAPCode))
                return BadRequest(new { message = "Region code is required for a regional row." });
            if (!dto.RegionId.HasValue && dto.SanctionNumber != 0)
                return BadRequest(new { message = "State rows cannot store a regional counter." });
            if (dto.RegionId.HasValue && dto.SanctionNumberState != 0)
                return BadRequest(new { message = "Regional rows cannot store the shared state counter." });

            var exists = await _db.SanctionNumberSequences.AnyAsync(x =>
                x.StateId == dto.StateId && x.RegionId == dto.RegionId);
            if (exists) return Conflict(new { message = "A sequence already exists for this state/region." });

            var row = new SanctionNumberSequence
            {
                StateId = dto.StateId,
                RegionId = dto.RegionId,
                StateSAPCode = dto.StateSAPCode.Trim().ToUpperInvariant(),
                RegionSAPCode = dto.RegionId.HasValue ? dto.RegionSAPCode!.Trim().ToUpperInvariant() : null,
                SanctionNumber = dto.SanctionNumber,
                SanctionNumberState = dto.SanctionNumberState,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.SanctionNumberSequences.Add(row);
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { return Conflict(new { message = "Could not create sequence. Check uniqueness and master references." }); }
            return CreatedAtAction(nameof(Get), new { id = row.Id }, ToDto(row));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSanctionNumberSequenceDto dto)
        {
            var row = await _db.SanctionNumberSequences.FindAsync(id);
            if (row == null) return NotFound();
            if (row.RegionId.HasValue && string.IsNullOrWhiteSpace(dto.RegionSAPCode))
                return BadRequest(new { message = "Region code is required." });
            row.StateSAPCode = dto.StateSAPCode.Trim().ToUpperInvariant();
            row.RegionSAPCode = row.RegionId.HasValue ? dto.RegionSAPCode!.Trim().ToUpperInvariant() : null;
            row.UpdatedBy = UserId;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(ToDto(row));
        }

        // Intentionally NO delete, reset or increment endpoint here.
        // Only final CSR-1 approval may atomically advance a sequence.
    }
}
