using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
    // MD Portal - Program Master page: Program Master add/edit and state-wise
    // applicability (ProgramStateMappings). The program-budget endpoints in
    // BudgetController (api/Budget/...) are untouched; nothing here changes how
    // they select programs. ProgramMasters has no IsActive column, so there is
    // no status toggle or delete.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramMasterController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProgramMasterController(AppDbContext db)
        {
            _db = db;
        }

        private string CurrentUser =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "System";

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var items = await _db.ProgramMasters
                .AsNoTracking()
                .Select(x => new ProgramMasterListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProgramTypeId = x.ProgramTypeId,
                    ProgramType = x.ProgramType != null ? x.ProgramType.Name : "",
                    IsMO = x.IsMO,
                    IsRMDO = x.IsRMDO,
                    IsSMDO = x.IsSMDO,
                    ApplicableStateCount = _db.ProgramStateMappings
                        .Count(m => m.ProgramId == x.Id && m.IsApplicable),
                    CreatedAt = x.CreatedAt
                })
                .OrderBy(x => x.ProgramType)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProgramMasterSaveRequest request)
        {
            var error = await Validate(request, null);
            if (error != null) return error;

            var entity = new ProgramMaster
            {
                Name = request.Name.Trim(),
                ProgramTypeId = request.ProgramTypeId,
                IsMO = request.IsMO,
                IsRMDO = request.IsRMDO,
                IsSMDO = request.IsSMDO,
                CreatedBy = CurrentUser,
                UpdatedBy = CurrentUser,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.ProgramMasters.Add(entity);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Program created successfully", data = new { entity.Id, entity.Name } });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProgramMasterSaveRequest request)
        {
            var entity = await _db.ProgramMasters.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound(new { message = "Program not found." });

            var error = await Validate(request, id);
            if (error != null) return error;

            entity.Name = request.Name.Trim();
            entity.ProgramTypeId = request.ProgramTypeId;
            entity.IsMO = request.IsMO;
            entity.IsRMDO = request.IsRMDO;
            entity.IsSMDO = request.IsSMDO;
            entity.UpdatedBy = CurrentUser;
            entity.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Program updated successfully", data = new { entity.Id, entity.Name } });
        }

        // Every State from the State master, with this program's applicability.
        // States with no ProgramStateMappings row are returned as Not Applicable.
        [HttpGet("{id}/state-mappings")]
        public async Task<IActionResult> GetStateMappings(int id)
        {
            if (!await _db.ProgramMasters.AnyAsync(x => x.Id == id))
                return NotFound(new { message = "Program not found." });

            var mappings = await _db.ProgramStateMappings
                .AsNoTracking()
                .Where(m => m.ProgramId == id)
                .ToDictionaryAsync(m => m.StateId, m => m.IsApplicable);

            // Active states, plus any inactive state that already has a mapping row
            // so an existing value is never hidden.
            var mappedStateIds = mappings.Keys.ToList();
            var states = await _db.States
                .AsNoTracking()
                .Where(s => s.IsActive || mappedStateIds.Contains(s.Id))
                .OrderBy(s => s.StateName)
                .Select(s => new { s.Id, s.StateName })
                .ToListAsync();

            var result = states.Select(s => new ProgramStateApplicabilityDto
            {
                StateId = s.Id,
                StateName = s.StateName,
                HasMapping = mappings.ContainsKey(s.Id),
                IsApplicable = mappings.TryGetValue(s.Id, out var applicable) && applicable
            }).ToList();

            return Ok(result);
        }

        // Upsert of the single (ProgramId, StateId) row - never inserts a second row
        // for the same pair (also enforced by the unique index on ProgramStateMappings).
        [HttpPut("{id}/state-mappings/{stateId}")]
        public async Task<IActionResult> SetStateMapping(int id, int stateId, [FromBody] ProgramStateApplicabilityRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            if (!await _db.ProgramMasters.AnyAsync(x => x.Id == id))
                return NotFound(new { message = "Program not found." });

            if (!await _db.States.AnyAsync(s => s.Id == stateId))
                return NotFound(new { message = "State not found." });

            var mapping = await _db.ProgramStateMappings
                .FirstOrDefaultAsync(m => m.ProgramId == id && m.StateId == stateId);

            if (mapping == null)
            {
                mapping = new ProgramStateMapping
                {
                    ProgramId = id,
                    StateId = stateId,
                    IsApplicable = request.IsApplicable,
                    CreatedBy = CurrentUser,
                    UpdatedBy = CurrentUser,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _db.ProgramStateMappings.Add(mapping);
            }
            else
            {
                mapping.IsApplicable = request.IsApplicable;
                mapping.UpdatedBy = CurrentUser;
                mapping.UpdatedAt = DateTime.Now;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent request created the same (ProgramId, StateId) row first;
                // the unique index rejected the duplicate - apply the value to that row.
                _db.ChangeTracker.Clear();
                var existing = await _db.ProgramStateMappings
                    .FirstOrDefaultAsync(m => m.ProgramId == id && m.StateId == stateId);
                if (existing == null) throw;

                existing.IsApplicable = request.IsApplicable;
                existing.UpdatedBy = CurrentUser;
                existing.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                mapping = existing;
            }

            return Ok(new
            {
                message = request.IsApplicable ? "State marked Applicable" : "State marked Not Applicable",
                data = new ProgramStateApplicabilityDto
                {
                    StateId = stateId,
                    IsApplicable = mapping.IsApplicable,
                    HasMapping = true
                }
            });
        }

        private async Task<IActionResult?> Validate(ProgramMasterSaveRequest? request, int? excludeId)
        {
            var name = request?.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Program name is required." });

            if (request!.ProgramTypeId <= 0 || !await _db.ProgramTypes.AnyAsync(t => t.Id == request.ProgramTypeId))
                return BadRequest(new { message = "Please select a valid Program Type." });

            var lower = name.ToLower();
            var duplicate = await _db.ProgramMasters.AnyAsync(x =>
                x.ProgramTypeId == request.ProgramTypeId &&
                x.Name.Trim().ToLower() == lower &&
                (excludeId == null || x.Id != excludeId));
            if (duplicate)
                return Conflict(new { message = $"Program \"{name}\" already exists under this Program Type." });

            return null;
        }

        // ─── State-wise Program Amount (ProgramStateBudgets) ──────────────────────
        // Returns every active State alongside the existing BudgetAmount for this
        // program, or null when no ProgramStateBudgets row exists yet.
        // This is READ-ONLY access that does NOT interfere with the BudgetController
        // workflow, which only reads ProgramStateBudgets via GetAll() to validate
        // that a program is allocated to a state before accepting a budget submission.
        [HttpGet("{id}/state-budgets")]
        public async Task<IActionResult> GetStateBudgets(int id)
        {
            if (!await _db.ProgramMasters.AnyAsync(x => x.Id == id))
                return NotFound(new { message = "Program not found." });

            // Load existing ProgramStateBudgets rows for this program.
            var existingBudgets = await _db.ProgramStateBudgets
                .AsNoTracking()
                .Where(b => b.ProgramId == id)
                .ToDictionaryAsync(b => b.StateId, b => b.BudgetAmount);

            // Return active states; also include any state that already has a budget
            // row even if it has since been deactivated, so existing amounts are
            // never hidden.
            var budgetedStateIds = existingBudgets.Keys.ToList();
            var states = await _db.States
                .AsNoTracking()
                .Where(s => s.IsActive || budgetedStateIds.Contains(s.Id))
                .OrderBy(s => s.StateName)
                .Select(s => new { s.Id, s.StateName })
                .ToListAsync();

            var result = states.Select(s => new ProgramStateBudgetDto
            {
                StateId = s.Id,
                StateName = s.StateName,
                HasBudget = existingBudgets.ContainsKey(s.Id),
                BudgetAmount = existingBudgets.TryGetValue(s.Id, out var amt) ? amt : null
            }).ToList();

            return Ok(result);
        }

        // Upsert for a single (ProgramId, StateId) row in ProgramStateBudgets.
        // Prevents duplicate rows: if one already exists it is updated, not inserted.
        // The existing BudgetController behaviour is unchanged because it only calls
        // _programStateBudgetRepo.GetAll() / FirstOrDefault on ProgramStateBudgets
        // for validation/display; it does NOT insert/update ProgramStateBudgets rows.
        [HttpPut("{id}/state-budgets/{stateId}")]
        public async Task<IActionResult> SetStateBudget(int id, int stateId,
            [FromBody] ProgramStateBudgetSaveRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            if (request.BudgetAmount < 0)
                return BadRequest(new { message = "Program Amount must not be negative." });

            if (!await _db.ProgramMasters.AnyAsync(x => x.Id == id))
                return NotFound(new { message = "Program not found." });

            if (!await _db.States.AnyAsync(s => s.Id == stateId))
                return NotFound(new { message = "State not found." });

            var existing = await _db.ProgramStateBudgets
                .FirstOrDefaultAsync(b => b.ProgramId == id && b.StateId == stateId);

            if (existing == null)
            {
                existing = new ProgramStateBudget
                {
                    ProgramId = id,
                    StateId = stateId,
                    BudgetAmount = request.BudgetAmount
                };
                _db.ProgramStateBudgets.Add(existing);
            }
            else
            {
                existing.BudgetAmount = request.BudgetAmount;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Concurrent request won the race; apply the value to the winning row.
                _db.ChangeTracker.Clear();
                var concurrent = await _db.ProgramStateBudgets
                    .FirstOrDefaultAsync(b => b.ProgramId == id && b.StateId == stateId);
                if (concurrent == null) throw;
                concurrent.BudgetAmount = request.BudgetAmount;
                await _db.SaveChangesAsync();
                existing = concurrent;
            }

            return Ok(new
            {
                message = $"Program Amount saved successfully.",
                data = new ProgramStateBudgetDto
                {
                    StateId = stateId,
                    BudgetAmount = existing.BudgetAmount,
                    HasBudget = true
                }
            });
        }
    }
}
