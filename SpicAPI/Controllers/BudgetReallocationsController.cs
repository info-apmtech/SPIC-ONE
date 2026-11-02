using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers;

/// <summary>
/// Budget reallocation workflow using actual State/Region/HQ approved allocations.
/// Final approval uses approved CSR-2 expenses; ambiguous historical scope remains fail-closed.
/// </summary>
[ApiController]
[Authorize]
[Route("api/budget-reallocations")]
public sealed class BudgetReallocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public BudgetReallocationsController(AppDbContext db) => _db = db;
    private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string? InitiatorRole() => new[] { "RM", "SMM", "Admin" }.FirstOrDefault(User.IsInRole);
    private static ReallocationStatus InitialStatus(string role) => role switch
    {
        "RM" => ReallocationStatus.PendingSMM,
        "SMM" => ReallocationStatus.PendingAVP,
        "Admin" => ReallocationStatus.PendingMDHOD,
        _ => throw new InvalidOperationException("Invalid applicant role")
    };
    private static string? ApproverRole(ReallocationStatus status) => status switch
    {
        ReallocationStatus.PendingSMM => "SMM",
        ReallocationStatus.PendingAVP => "AVP",
        ReallocationStatus.PendingMDHOD => "MDHOD",
        _ => null
    };
    private static ReallocationStatus NextStatus(ReallocationStatus status) => status switch
    {
        ReallocationStatus.PendingSMM => ReallocationStatus.PendingAVP,
        ReallocationStatus.PendingAVP => ReallocationStatus.PendingMDHOD,
        ReallocationStatus.PendingMDHOD => ReallocationStatus.Approved,
        _ => throw new InvalidOperationException("Not pending approval")
    };
    private static ReallocationLevel InitiatorBudgetLevel(string role) => role switch
    {
        "Admin" => ReallocationLevel.State,
        "SMM" => ReallocationLevel.Region,
        "RM" => ReallocationLevel.Headquarters,
        _ => throw new InvalidOperationException("Unsupported reallocation applicant")
    };
    // Only sibling budgets may be transferred by a given initiator.
    private async Task<bool> SameAllowedParentAsync(string role, BudgetPoolKey source, BudgetPoolKey destination, CancellationToken ct)
    {
        if (source.Level != InitiatorBudgetLevel(role) || destination.Level != source.Level ||
            source.FinancialYearId != destination.FinancialYearId || source.EntityId == destination.EntityId)
            return false;
        if (role == "Admin") return true;
        var login = await _db.Set<Employeelogin>().AsNoTracking()
            .Where(x => x.IsActive && x.UserId == Actor)
            .Select(x => new { x.StateId, x.RegionId })
            .FirstOrDefaultAsync(ct);
        if (login == null) return false;
        if (role == "SMM")
        {
            if (login.StateId <= 0) return false;
            var matching = await _db.Set<RegionBudgetAllocation>().AsNoTracking()
                .Where(x => x.StateId == login.StateId &&
                    x.RegionBudgetSummary != null &&
                    x.RegionBudgetSummary.FY == source.FinancialYearId &&
                    (x.RegionId == source.EntityId || x.RegionId == destination.EntityId))
                .Select(x => x.RegionId).Distinct().ToListAsync(ct);
            return matching.Contains(source.EntityId) && matching.Contains(destination.EntityId);
        }
        if (login.RegionId <= 0) return false;
        var hqs = await _db.Set<HeadquarterBudgetAllocation>().AsNoTracking()
            .Where(x => x.RegionId == login.RegionId &&
                x.HeadquarterBudgetSummary != null &&
                x.HeadquarterBudgetSummary.FY == source.FinancialYearId &&
                (x.HeadquarterId == source.EntityId || x.HeadquarterId == destination.EntityId))
            .Select(x => x.HeadquarterId).Distinct().ToListAsync(ct);
        return hqs.Contains(source.EntityId) && hqs.Contains(destination.EntityId);
    }
    private static BudgetPoolKey Key(int fy, ReallocationLevel level, int entityId) => new(fy, level, entityId);
    private static bool ValidLevel(ReallocationLevel level) => Enum.IsDefined(typeof(ReallocationLevel), level);
    private IActionResult IntegrationUnavailable() => StatusCode(StatusCodes.Status503ServiceUnavailable,
        new { Code = "BUDGET_MAPPING_REQUIRED", Message = "Budget pool or CSR-2 spending classification unavailable. Check saved allocation, FY and CSR-1 creator geography/role mapping." });

    // Uses the real Employeelogin geography for non-global roles.
    // AVP is intentionally limited to its recorded login scope; no blanket access.
    private async Task<bool?> CanAccessBudgetAsync(string userId, string role, BudgetPoolKey pool, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId) || pool.FinancialYearId <= 0 || pool.EntityId <= 0)
            return false;
        if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("MDHOD", StringComparison.OrdinalIgnoreCase)) return true;

        var login = await _db.Set<Employeelogin>().AsNoTracking()
            .Where(x => x.IsActive && x.UserId == userId)
            .Select(x => new { x.StateId, x.RegionId, x.HeadquartersId })
            .FirstOrDefaultAsync(ct);
        if (login == null) return false;

        if (pool.Level == ReallocationLevel.State)
            return login.StateId > 0 && login.StateId == pool.EntityId;
        if (pool.Level == ReallocationLevel.Region)
        {
            if (login.RegionId > 0 && login.RegionId == pool.EntityId) return true;
            return login.StateId > 0 && await _db.Set<RegionBudgetAllocation>()
                .AsNoTracking().AnyAsync(x => x.RegionId == pool.EntityId && x.StateId == login.StateId &&
                    x.RegionBudgetSummary != null &&
                    x.RegionBudgetSummary.FY == pool.FinancialYearId, ct);
        }
        if (pool.Level == ReallocationLevel.Headquarters)
        {
            if (login.HeadquartersId > 0 && login.HeadquartersId == pool.EntityId) return true;
            if (login.RegionId > 0 && await _db.Set<HeadquarterBudgetAllocation>()
                .AsNoTracking().AnyAsync(x => x.HeadquarterId == pool.EntityId &&
                    x.RegionId == login.RegionId &&
                    x.HeadquarterBudgetSummary != null &&
                    x.HeadquarterBudgetSummary.FY == pool.FinancialYearId, ct)) return true;
            if (login.StateId > 0) return await (
                from h in _db.Set<HeadquarterBudgetAllocation>().AsNoTracking()
                join r in _db.Set<RegionBudgetAllocation>().AsNoTracking()
                    on h.RegionId equals r.RegionId
                where h.HeadquarterId == pool.EntityId &&
                      h.HeadquarterBudgetSummary != null &&
                      h.HeadquarterBudgetSummary.FY == pool.FinancialYearId &&
                      r.StateId == login.StateId &&
                      r.RegionBudgetSummary != null &&
                      r.RegionBudgetSummary.FY == pool.FinancialYearId
                select h.Id).AnyAsync(ct);
        }
        return false;
    }

    // Source allocation uses saved allocation amounts for the selected FY, regardless of allocation/summary status.
    // Include incoming/outgoing APPROVED reallocations once; pending requests change nothing.
    // The CSR-2 spending component is computed by ApprovedCsr2UsedAsync.
    private async Task<decimal?> AvailableBudgetAsync(BudgetPoolKey pool, CancellationToken ct)
    {
        decimal? allocated = pool.Level switch
        {
            ReallocationLevel.State => await _db.Set<StateBudgetAllocation>().AsNoTracking()
                .Where(x => x.StateId == pool.EntityId &&
                    x.StateBudgetSummary != null && x.StateBudgetSummary.FY == pool.FinancialYearId)
                .SumAsync(x => (decimal?)x.Amount, ct),
            ReallocationLevel.Region => await _db.Set<RegionBudgetAllocation>().AsNoTracking()
                .Where(x => x.RegionId == pool.EntityId &&
                    x.RegionBudgetSummary != null && x.RegionBudgetSummary.FY == pool.FinancialYearId)
                .SumAsync(x => (decimal?)x.Amount, ct),
            ReallocationLevel.Headquarters => await _db.Set<HeadquarterBudgetAllocation>().AsNoTracking()
                .Where(x => x.HeadquarterId == pool.EntityId &&
                    x.HeadquarterBudgetSummary != null && x.HeadquarterBudgetSummary.FY == pool.FinancialYearId)
                .SumAsync(x => (decimal?)x.Amount, ct),
            _ => null
        };
        // No saved positive allocation for this FY means this budget pool has no funds.
        if (allocated is null || allocated <= 0) return null;
        var transfers = _db.Set<BudgetReallocation>().AsNoTracking()
            .Where(x => x.Status == ReallocationStatus.Approved &&
                x.FinancialYearId == pool.FinancialYearId);
        var incoming = await transfers.Where(x => x.DestinationLevel == pool.Level &&
            x.DestinationId == pool.EntityId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var outgoing = await transfers.Where(x => x.SourceLevel == pool.Level &&
            x.SourceId == pool.EntityId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var used = await ApprovedCsr2UsedAsync(pool, ct);
        if (!used.HasValue) return null;
        return allocated.Value + incoming - outgoing - used.Value;
    }

    // Approved CSR-2 consumption by the ACTUAL budget-spending level, not all ancestors.
    // Scope classification uses the CSR-1 creator's employee login role:
    // SMDO/SMD => State SMDO pool; RMDO => Region RMDO pool; MDO/JMDO/MO => HQ.
    // FY is mapped using CSR-2 ActualEventDate and FinancialYear.StartDate/EndDate.
    // Any unclassified approved event in the FY makes the balance unavailable (fail closed).
    private async Task<decimal?> ApprovedCsr2UsedAsync(BudgetPoolKey pool, CancellationToken ct)
    {
        var fy = await _db.Set<FinancialYear>().AsNoTracking()
            .Where(x => x.Id == pool.FinancialYearId)
            .Select(x => new { x.StartDate, x.EndDate }).SingleOrDefaultAsync(ct);
        if (fy == null) return null;
        // Do not silently exclude approved CSR-2 records with no event date.
        var rows = await (
            from report in _db.Set<CSR2>().AsNoTracking()
            join plan in _db.Set<CSR1>().AsNoTracking() on report.CSR1Id equals plan.Id
            where report.Status == CSR2Status.Approved &&
                (!report.ActualEventDate.HasValue ||
                 (report.ActualEventDate.Value.Date >= fy.StartDate.Date &&
                  report.ActualEventDate.Value.Date <= fy.EndDate.Date))
            select new
            {
                report.Id,
                report.ActualEventDate,
                plan.CreatedBy,
                plan.HeadquarterId,
                Expense = report.Expenses.Sum(e => (decimal?)e.ActualAmount) ?? 0m
            }).ToListAsync(ct);
        if (rows.Any(x => !x.ActualEventDate.HasValue)) return null;
        if (rows.Count == 0) return 0m;
        var creators = rows.Select(x => x.CreatedBy).Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct().ToList();
        if (creators.Count != rows.Select(x => x.CreatedBy).Distinct().Count()) return null;
        var logins = await _db.Set<Employeelogin>().AsNoTracking()
            .Where(x => creators.Contains(x.UserId))
            .Select(x => new { x.UserId, x.StateId, x.RegionId, x.HeadquartersId, x.Role })
            .ToListAsync(ct);
        decimal used = 0m;
        foreach (var row in rows)
        {
            var creatorLogins = logins.Where(x => x.UserId == row.CreatedBy).ToList();
            if (creatorLogins.Count != 1) return null; // no guess for ambiguous/historical roles
            var login = creatorLogins[0];
            var role = login.Role.ToString();
            ReallocationLevel actualLevel;
            int actualId;
            if (role is "SMDO" or "SMD")
            {
                actualLevel = ReallocationLevel.State; actualId = login.StateId;
            }
            else if (role is "RMDO")
            {
                actualLevel = ReallocationLevel.Region; actualId = login.RegionId;
            }
            else if (role is "JMDO" or "MDO" or "MO")
            {
                actualLevel = ReallocationLevel.Headquarters;
                actualId = Convert.ToInt32(row.HeadquarterId) > 0 ? Convert.ToInt32(row.HeadquarterId) : login.HeadquartersId;
            }
            else return null; // Unrecognized spender must be explicitly mapped.
            if (actualId <= 0) return null;
            if (actualLevel == pool.Level && actualId == pool.EntityId)
                used += row.Expense;
        }
        return used;
    }

    // Cascading FY -> State -> Region -> HQ budget pickers.
    // Saved allocation rows for the chosen FY are offered (regardless of status). IDs are always
    // actual organization IDs, never allocation-row IDs.
    [HttpGet("budget-options")]
    public async Task<IActionResult> BudgetOptions(int financialYearId, ReallocationLevel level,
        int? stateId, int? regionId, CancellationToken ct)
    {
        if (financialYearId <= 0 || !ValidLevel(level)) return BadRequest("Select a valid financial year and level.");
        var role = InitiatorRole();
        if (role == null || string.IsNullOrEmpty(Actor)) return Forbid();
        if ((role == "Admin" && level != ReallocationLevel.State) ||
            (role == "SMM" && level == ReallocationLevel.Headquarters)) return Forbid();

        // For SMM/RM the State selector is a geographic parent filter, not a
        // spendable State pool. It must not depend on StateBudgetAllocations.
        if (level == ReallocationLevel.State && (role is "SMM" or "RM"))
        {
            var assignedStateId = await _db.Set<Employeelogin>().AsNoTracking()
                .Where(x => x.IsActive && x.UserId == Actor)
                .Select(x => x.StateId)
                .FirstOrDefaultAsync(ct);
            if (assignedStateId <= 0)
                return Conflict(new { Code = "STATE_NOT_ASSIGNED", Message = "No State is mapped to this employee login." });
            var assignedState = await _db.Set<State>().AsNoTracking()
                .Where(x => x.Id == assignedStateId && x.IsActive)
                .Select(x => new { x.Id, Label = x.StateName })
                .FirstOrDefaultAsync(ct);
            if (assignedState == null)
                return Conflict(new { Code = "STATE_NOT_FOUND", Message = "The employee's assigned State is missing or inactive." });
            return Ok(new[] { assignedState });
        }

        var candidates = new List<(int Id, int StateId, int RegionId)>();
        if (level == ReallocationLevel.State && role == "Admin")
        {
            // Parent State selection is a geographic filter, not permission to transfer.
            // Show active master States even when a specific FY has no approved
            // StateBudgetAllocation row. Actual From/To pools are validated later.
            var stateIds = await _db.Set<State>().AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.Id).ToListAsync(ct);
            candidates.AddRange(stateIds.Select(id => (id, id, 0)));
        }
        else if (level == ReallocationLevel.State)
        {
            var rows = await _db.Set<StateBudgetAllocation>().AsNoTracking()
                .Where(x => x.StateBudgetSummary != null && x.StateBudgetSummary.FY == financialYearId)
                .Select(x => x.StateId).Distinct().ToListAsync(ct);
            // Region allocations can exist for a State even when the parent State
            // allocation has no independently approved row. Include their StateIds
            // so the cascading picker can reach the approved Region/HQ budgets.
            var regionStateIds = await _db.Set<RegionBudgetAllocation>().AsNoTracking()
                .Where(x => x.StateId > 0 &&
                    x.RegionBudgetSummary != null &&
                    x.RegionBudgetSummary.FY == financialYearId)
                .Select(x => x.StateId).Distinct().ToListAsync(ct);
            candidates.AddRange(rows.Concat(regionStateIds).Distinct().Select(id => (id, id, 0)));
        }
        else if (level == ReallocationLevel.Region)
        {
            if (stateId is null or <= 0) return BadRequest("Select State first.");
            var rows = await _db.Set<RegionBudgetAllocation>().AsNoTracking()
                .Where(x => x.StateId == stateId &&
                    x.RegionBudgetSummary != null &&
                    x.RegionBudgetSummary.FY == financialYearId)
                .Select(x => x.RegionId).Distinct().ToListAsync(ct);
            candidates.AddRange(rows.Select(id => (id, stateId.Value, id)));
        }
        else
        {
            if (regionId is null or <= 0) return BadRequest("Select Region first.");
            var rows = await _db.Set<HeadquarterBudgetAllocation>().AsNoTracking()
                .Where(x => x.RegionId == regionId &&
                    x.HeadquarterBudgetSummary != null &&
                    x.HeadquarterBudgetSummary.FY == financialYearId)
                .Select(x => x.HeadquarterId).Distinct().ToListAsync(ct);
            candidates.AddRange(rows.Select(id => (id, stateId ?? 0, regionId.Value)));
        }
        // Resolve actual State names from the State master rather than showing IDs.
        var candidateIds = candidates.Select(x => x.Id).Distinct().ToList();
        var stateNames = level == ReallocationLevel.State
            ? await _db.Set<State>().AsNoTracking()
                .Where(x => x.IsActive && candidateIds.Contains(x.Id))
                .Select(x => new { x.Id, x.StateName })
                .ToDictionaryAsync(x => x.Id, x => x.StateName, ct)
            : new Dictionary<int, string>();
        var result = new List<object>();
        foreach (var candidate in candidates.OrderBy(x => x.Id))
        {
            var pool = Key(financialYearId, level, candidate.Id);
            // The State picker selects a parent geographic scope, not necessarily
            // a State-level spendable pool. Allow State options via Region allocations
            // while retaining the actual pool authorization below for Region/HQ.
            if (level != ReallocationLevel.State &&
                await CanAccessBudgetAsync(Actor, role, pool, ct) != true) continue;
            if (level == ReallocationLevel.State && role != "Admin" &&
                await CanAccessBudgetAsync(Actor, role, pool, ct) != true) continue;
            // Expose the underlying StateBudgetAllocations amount independently from
            // the computed balance. This allows the UI to distinguish missing allocation from
            // an unavailable CSR-2 classification, without bypassing server validation.
            decimal? stateAllocatedBudget = null;
            bool? hasStateAllocation = null;
            if (level == ReallocationLevel.State)
            {
                var allocationRows = await _db.Set<StateBudgetAllocation>().AsNoTracking()
                    .Where(x => x.StateId == candidate.Id && x.StateBudgetSummary != null &&
                        x.StateBudgetSummary.FY == financialYearId)
                    .Select(x => new
                    {
                        x.Amount
                    }).ToListAsync(ct);
                if (allocationRows.Count > 0)
                {
                    stateAllocatedBudget = allocationRows.Sum(x => x.Amount);
                    hasStateAllocation = allocationRows.Any(x => x.Amount > 0);
                }
                else
                {
                    hasStateAllocation = false;
                }
            }
            var available = await AvailableBudgetAsync(pool, ct);
            var used = await ApprovedCsr2UsedAsync(pool, ct);
            var availabilityIssue = available.HasValue ? null
                : level == ReallocationLevel.State && hasStateAllocation == false
                    ? "No positive StateBudgetAllocations amount for this financial year."
                    : !used.HasValue
                        ? "Approved CSR-2 spending cannot be classified safely. Check event date and CSR-1 creator geography/role mapping."
                        : "Available budget could not be determined from the saved allocation.";
            var label = level == ReallocationLevel.State &&
                stateNames.TryGetValue(candidate.Id, out var stateName)
                    ? stateName
                    : $"{level} #{candidate.Id}";
            result.Add(new
            {
                candidate.Id,
                Label = label,
                candidate.StateId,
                candidate.RegionId,
                Available = available,
                StateAllocatedBudget = stateAllocatedBudget,
                AvailabilityIssue = availabilityIssue,
                ApprovedCSR2Expense = used
            });
        }
        return Ok(result);
    }

    [HttpGet("financial-years")]
    public async Task<IActionResult> FinancialYears(CancellationToken ct)
    {
        var years = await _db.Set<AnnualBudgeting>().AsNoTracking()
            .Where(x => x.IsActive && x.FinancialYearId > 0)
            .Select(x => x.FinancialYearId).Distinct().OrderByDescending(x => x).ToListAsync(ct);
        var labels = await _db.Set<FinancialYear>().AsNoTracking()
            .Where(x => x.IsActive && years.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(ct);
        return Ok(labels.Select(x => new { x.Id, Label = x.Name }));
    }

    // The Region/State assigned to RM is independent of whether an allocation is approved.
    // Never accept arbitrary employee IDs from the client.
    [HttpGet("assigned-geography")]
    public async Task<IActionResult> AssignedGeography(CancellationToken ct)
    {
        var role = InitiatorRole();
        if (role is not ("RM" or "SMM") || string.IsNullOrWhiteSpace(Actor)) return Forbid();
        var geography = await _db.Set<Employeelogin>().AsNoTracking()
            .Where(x => x.UserId == Actor && x.IsActive)
            .Select(x => new { x.StateId, x.RegionId }).FirstOrDefaultAsync(ct);
        if (geography == null || geography.StateId <= 0)
            return Conflict(new { Code = "EMPLOYEE_GEOGRAPHY_MISSING", Message = "State is not assigned to the employee." });
        if (role == "RM" && geography.RegionId <= 0)
            return Conflict(new { Code = "EMPLOYEE_REGION_MISSING", Message = "Region is not assigned to the RM." });
        return Ok(geography);
    }

    [HttpGet("role-context")]
    public IActionResult RoleContext() => Ok(new
    {
        Roles = new[] { "RM", "SMM", "Admin", "AVP", "MDHOD" }
        .Where(User.IsInRole).ToArray()
    });

    [HttpGet("apply-context")]
    public IActionResult ApplyContext()
    {
        var role = InitiatorRole();
        if (role is null || string.IsNullOrWhiteSpace(Actor)) return Forbid();
        string[] route = role switch
        {
            "RM" => new[] { "RM", "SMM", "AVP", "MD HOD" },
            "SMM" => new[] { "SMM", "AVP", "MD HOD" },
            _ => new[] { "Admin", "MD HOD" }
        };
        return Ok(new
        {
            InitiatorRole = role,
            ApprovalRoute = route,
            ReallocationLevel = InitiatorBudgetLevel(role)
        });
    }

    // The initiator sees their own requests; the current approver sees only pending
    // requests in their authorized State/Region/HQ scope.
    private async Task<bool> CanApproveRequestAsync(BudgetReallocation r, CancellationToken ct)
    {
        var role = ApproverRole(r.Status);
        if (role == null || !User.IsInRole(role) || r.InitiatedBy == Actor)
            return false;
        var source = Key(r.FinancialYearId, r.SourceLevel, r.SourceId);
        var destination = Key(r.FinancialYearId, r.DestinationLevel, r.DestinationId);
        return await CanAccessBudgetAsync(Actor, role, source, ct) == true &&
               await CanAccessBudgetAsync(Actor, role, destination, ct) == true;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Actor)) return Forbid();
        var pending = new List<ReallocationStatus>();
        if (User.IsInRole("SMM")) pending.Add(ReallocationStatus.PendingSMM);
        if (User.IsInRole("AVP")) pending.Add(ReallocationStatus.PendingAVP);
        if (User.IsInRole("MDHOD")) pending.Add(ReallocationStatus.PendingMDHOD);

        // Filter by creator/current stage BEFORE retrieving entities. Enforce
        // exact geographical authorization per request below.
        var candidates = await _db.Set<BudgetReallocation>().AsNoTracking()
            .Where(x => x.InitiatedBy == Actor || pending.Contains(x.Status))
            .OrderByDescending(x => x.Id).Take(300).ToListAsync(ct);

        var results = new List<object>();
        foreach (var r in candidates)
        {
            bool canApprove = await CanApproveRequestAsync(r, ct);
            if (r.InitiatedBy != Actor && !canApprove) continue;
            results.Add(new
            {
                r.Id,
                r.FinancialYearId,
                r.SourceLevel,
                r.SourceId,
                r.DestinationLevel,
                r.DestinationId,
                r.Amount,
                r.Reason,
                r.InitiatorRole,
                r.InitiatedBy,
                r.CreatedAt,
                Status = r.Status.ToString(),
                CanApprove = canApprove,
                IsOwnRequest = r.InitiatedBy == Actor
            });
        }
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Actor)) return Forbid();
        var r = await _db.Set<BudgetReallocation>().AsNoTracking()
            .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r == null) return NotFound();
        if (r.InitiatedBy != Actor && !await CanApproveRequestAsync(r, ct)) return Forbid();
        return Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] CreateBudgetReallocationDto dto, CancellationToken ct)
    {
        var role = InitiatorRole();
        if (role is null || string.IsNullOrWhiteSpace(Actor)) return Forbid();
        if (dto.FinancialYearId <= 0 || dto.SourceId <= 0 || dto.DestinationId <= 0 ||
            dto.Amount <= 0 || string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest("Financial year, budgets, positive amount and reason required.");
        if (!ValidLevel(dto.SourceLevel) || !ValidLevel(dto.DestinationLevel))
            return BadRequest("Invalid reallocation level.");
        if (dto.SourceLevel == dto.DestinationLevel && dto.SourceId == dto.DestinationId)
            return BadRequest("Source and destination budgets must differ.");
        var source = Key(dto.FinancialYearId, dto.SourceLevel, dto.SourceId);
        var destination = Key(dto.FinancialYearId, dto.DestinationLevel, dto.DestinationId);
        if (!await SameAllowedParentAsync(role, source, destination, ct))
            return BadRequest("Transfers must stay within the applicant's authorized budget level and parent State/Region.");
        var canSource = await CanAccessBudgetAsync(Actor, role, source, ct);
        var canDestination = await CanAccessBudgetAsync(Actor, role, destination, ct);
        if (!canSource.HasValue || !canDestination.HasValue) return IntegrationUnavailable();
        if (!canSource.Value || !canDestination.Value) return Forbid();
        var available = await AvailableBudgetAsync(source, ct);
        if (!available.HasValue) return IntegrationUnavailable();
        if (dto.Amount > available.Value)
            return Conflict(new
            {
                Code = "BUDGET_SHORTAGE",
                Available = available.Value,
                Shortage = dto.Amount - available.Value,
                Message = "Insufficient source budget."
            });
        var r = new BudgetReallocation
        {
            FinancialYearId = dto.FinancialYearId,
            SourceLevel = dto.SourceLevel,
            SourceId = dto.SourceId,
            DestinationLevel = dto.DestinationLevel,
            DestinationId = dto.DestinationId,
            Amount = dto.Amount,
            Reason = dto.Reason.Trim(),
            InitiatorRole = role,
            InitiatedBy = Actor,
            Status = InitialStatus(role),
            CreatedAt = DateTime.UtcNow
        };
        r.Approvals.Add(new BudgetReallocationApproval
        {
            StageRole = role,
            Decision = ReallocationDecision.Submitted,
            ApprovedBy = Actor,
            ApprovedAt = DateTime.UtcNow
        });
        _db.Set<BudgetReallocation>().Add(r);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Details), new { id = r.Id }, new { r.Id, Status = r.Status.ToString() });
    }

    [HttpPost("{id:int}/decision")]
    public async Task<IActionResult> Decide(int id, [FromBody] ReallocationDecisionDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Actor)) return Forbid();
        await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var r = await _db.Set<BudgetReallocation>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return NotFound();
        var stageRole = ApproverRole(r.Status);
        if (stageRole == null) return Conflict("Request is not pending approval.");
        if (!await CanApproveRequestAsync(r, ct)) return Forbid();
        var source = Key(r.FinancialYearId, r.SourceLevel, r.SourceId);
        if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Remarks))
            return BadRequest("Rejection remarks required.");
        if (dto.Approve && r.Status == ReallocationStatus.PendingMDHOD)
        {
            // Availability query MUST lock the corresponding pool/budget row for
            // concurrent final approvals, in addition to this serializable transaction.
            var available = await AvailableBudgetAsync(source, ct);
            if (!available.HasValue) return IntegrationUnavailable();
            if (r.Amount > available.Value)
                return Conflict(new
                {
                    Code = "BUDGET_SHORTAGE",
                    Available = available.Value,
                    Shortage = r.Amount - available.Value,
                    Message = "Insufficient source budget."
                });
        }
        r.Status = dto.Approve ? NextStatus(r.Status) : ReallocationStatus.Rejected;
        r.UpdatedAt = DateTime.UtcNow;
        _db.Set<BudgetReallocationApproval>().Add(new BudgetReallocationApproval
        {
            BudgetReallocationId = r.Id,
            StageRole = stageRole,
            Decision = dto.Approve ? ReallocationDecision.Approved : ReallocationDecision.Rejected,
            ApprovedBy = Actor,
            ApprovedAt = DateTime.UtcNow,
            Remarks = dto.Remarks?.Trim()
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        // Effective budgets are derived ONLY from Approved BudgetReallocations.
        return Ok(new { r.Id, Status = r.Status.ToString() });
    }
}
