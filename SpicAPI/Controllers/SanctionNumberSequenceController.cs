using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SpicAPI.Controllers
{
    [ApiController]
    [Route("api/SanctionNumberSequence")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SanctionNumberSequenceController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SanctionNumberSequenceController(AppDbContext db)
        {
            _db = db;
        }

        private string? UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        // One read projection for list and individual sequence responses.
        // Left joins keep legacy rows visible if a master reference needs repair.
        private IQueryable<SanctionNumberSequenceDto> SequenceQuery()
        {
            return
                from sequence in _db.SanctionNumberSequences.AsNoTracking()
                join state in _db.States.AsNoTracking()
                    on sequence.StateId equals state.Id into stateGroup
                from state in stateGroup.DefaultIfEmpty()
                join region in _db.Regions.AsNoTracking()
                    on new { sequence.StateId, RegionId = sequence.RegionId }
                    equals new { region.StateId, RegionId = (int?)region.Id }
                    into regionGroup
                from region in regionGroup.DefaultIfEmpty()
                select new SanctionNumberSequenceDto
                {
                    Id = sequence.Id,
                    StateId = sequence.StateId,
                    RegionId = sequence.RegionId,
                    StateName = state == null ? "" : state.StateName,
                    RegionName = region == null ? null : region.RegionName,
                    StateCode = state == null ? null : state.StateCode,
                    RegionCode = region == null ? null : region.RegionCode,
                    SanctionNumber = sequence.SanctionNumber,
                    SanctionNumberState = sequence.SanctionNumberState,
                    CreatedBy = sequence.CreatedBy,
                    CreatedAt = sequence.CreatedAt,
                    UpdatedBy = sequence.UpdatedBy,
                    UpdatedAt = sequence.UpdatedAt
                };
        }

        [HttpGet("states")]
        public async Task<IActionResult> States(CancellationToken ct)
        {
            var states = await _db.States
                .AsNoTracking()
                .OrderBy(x => x.StateName)
                .Select(x => new
                {
                    x.Id,
                    Name = x.StateName,
                    Code = x.StateCode
                })
                .ToListAsync(ct);

            return Ok(states);
        }

        [HttpGet("regions/{stateId:int}")]
        public async Task<IActionResult> Regions(int stateId, CancellationToken ct)
        {
            if (stateId <= 0)
            {
                return BadRequest(new { message = "StateId must be positive." });
            }

            var regions = await _db.Regions
                .AsNoTracking()
                .Where(x => x.StateId == stateId)
                .OrderBy(x => x.RegionName)
                .Select(x => new
                {
                    x.Id,
                    Name = x.RegionName,
                    Code = x.RegionCode
                })
                .ToListAsync(ct);

            return Ok(regions);
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var rows = await SequenceQuery()
                .OrderBy(x => x.StateId)
                .ThenBy(x => x.RegionId)
                .ToListAsync(ct);

            return Ok(rows);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var row = await SequenceQuery()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return row == null ? NotFound() : Ok(row);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSanctionNumberSequenceDto dto,
            CancellationToken ct)
        {
            if (dto.StateId <= 0)
            {
                return BadRequest(new { message = "StateId must be positive." });
            }

            if (dto.RegionId.HasValue && dto.RegionId.Value <= 0)
            {
                return BadRequest(new { message = "RegionId must be positive or null." });
            }

            if (dto.SanctionNumber < 0 || dto.SanctionNumberState < 0)
            {
                return BadRequest(new { message = "Starting value cannot be negative." });
            }

            if (!dto.RegionId.HasValue && dto.SanctionNumber != 0)
            {
                return BadRequest(new { message = "State rows cannot store a regional counter." });
            }

            if (dto.RegionId.HasValue && dto.SanctionNumberState != 0)
            {
                return BadRequest(new { message = "Regional rows cannot store the shared state counter." });
            }

            var state = await _db.States
                .AsNoTracking()
                .Where(x => x.Id == dto.StateId)
                .Select(x => new { x.Id, x.StateName, x.StateCode })
                .FirstOrDefaultAsync(ct);

            if (state == null)
            {
                return BadRequest(new { message = "Selected State was not found." });
            }

            if (string.IsNullOrWhiteSpace(state.StateCode))
            {
                return BadRequest(new
                {
                    message = "State Code is missing. Update the selected State master first."
                });
            }

            string? regionName = null;
            string? regionCode = null;

            if (dto.RegionId.HasValue)
            {
                var region = await _db.Regions
                    .AsNoTracking()
                    .Where(x => x.Id == dto.RegionId.Value && x.StateId == dto.StateId)
                    .Select(x => new { x.Id, x.RegionName, x.RegionCode })
                    .FirstOrDefaultAsync(ct);

                if (region == null)
                {
                    return BadRequest(new
                    {
                        message = "Selected Region was not found or does not belong to the selected State."
                    });
                }

                if (string.IsNullOrWhiteSpace(region.RegionCode))
                {
                    return BadRequest(new
                    {
                        message = "Region Code is missing. Update the selected Region master first."
                    });
                }

                regionName = region.RegionName;
                regionCode = region.RegionCode;
            }

            var exists = await _db.SanctionNumberSequences.AnyAsync(x =>
                x.StateId == dto.StateId && x.RegionId == dto.RegionId, ct);

            if (exists)
            {
                return Conflict(new { message = "A sequence already exists for this state/region." });
            }

            var row = new SanctionNumberSequence
            {
                StateId = dto.StateId,
                RegionId = dto.RegionId,
                SanctionNumber = dto.SanctionNumber,
                SanctionNumberState = dto.SanctionNumberState,
                CreatedBy = UserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.SanctionNumberSequences.Add(row);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Could not create sequence. Check uniqueness and master references."
                });
            }

            // Use the masters already validated above for the create response.
            // This avoids another database read after the insert has committed.
            var result = new SanctionNumberSequenceDto
            {
                Id = row.Id,
                StateId = row.StateId,
                RegionId = row.RegionId,
                StateName = state.StateName,
                RegionName = regionName,
                StateCode = state.StateCode,
                RegionCode = regionCode,
                SanctionNumber = row.SanctionNumber,
                SanctionNumberState = row.SanctionNumberState,
                CreatedBy = row.CreatedBy,
                CreatedAt = row.CreatedAt,
                UpdatedBy = row.UpdatedBy,
                UpdatedAt = row.UpdatedAt
            };

            return CreatedAtAction(nameof(Get), new { id = row.Id }, result);
        }

        // Codes are maintained in the State/Region masters, so no code-edit PUT.
        // No delete, reset or increment endpoint is exposed here.
        // Only final CSR-1 approval may atomically advance a sequence.
    }
}
