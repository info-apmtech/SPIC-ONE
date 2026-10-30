using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using Spic.Infrastructure.Data;

namespace SpicAPI.Controllers;

/// <summary>
/// Four-file module. IMPORTANT: implement the two clearly marked budget/scope
/// methods against the real six allocation tables before enabling transfers.
/// Until then, applying and approving are safely disabled (503).
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
    private static BudgetPoolKey Key(int fy, ReallocationLevel level, int entityId) => new(fy, level, entityId);
    private static bool ValidLevel(ReallocationLevel level) => Enum.IsDefined(typeof(ReallocationLevel), level);
    private IActionResult IntegrationUnavailable() => StatusCode(StatusCodes.Status503ServiceUnavailable,
        new { Code = "BUDGET_MAPPING_REQUIRED", Message = "Connect State/Region/HQ allocation and scope queries in BudgetReallocationsController before applying or approving." });

    // TODO: Replace with actual State/Region/HQ access + FY allocation lookup.
    // Null = NOT CONFIGURED, false = forbidden, true = authorized.
    // Must check both source AND destination scopes and proper initiator/approver jurisdiction.
    private Task<bool?> CanAccessBudgetAsync(string userId, string role, BudgetPoolKey pool, CancellationToken ct)
        => Task.FromResult<bool?>(null);

    // TODO: Calculate allocated + approved incoming transfers - approved outgoing transfers
    // - finally approved CSR2 expenses - active reservations/other commitments.
    // Implement against the real allocation columns and financial-year mapping.
    // Re-check under a concurrency-safe DB lock before final approval.
    // Null means NOT CONFIGURED; never silently treat as zero or unlimited funds.
    private Task<decimal?> AvailableBudgetAsync(BudgetPoolKey pool, CancellationToken ct)
        => Task.FromResult<decimal?>(null);

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
        return Ok(new { InitiatorRole = role, ApprovalRoute = route });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Actor)) return Forbid();
        // Show only own submissions until scope mapping is implemented.
        // Do not expose all employee records to approvers without jurisdiction filters.
        var rows = await _db.Set<BudgetReallocation>().AsNoTracking()
            .Where(x => x.InitiatedBy == Actor).OrderByDescending(x => x.Id)
            .Take(100).Select(x => new {
                x.Id,
                x.FinancialYearId,
                x.SourceLevel,
                x.SourceId,
                x.DestinationLevel,
                x.DestinationId,
                x.Amount,
                x.Reason,
                Status = x.Status.ToString(),
                x.CreatedAt
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var r = await _db.Set<BudgetReallocation>().AsNoTracking()
            .Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r == null) return NotFound();
        if (r.InitiatedBy != Actor) return Forbid(); // expand via scope check once mapped
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
        if (!User.IsInRole(stageRole) || Actor == r.InitiatedBy) return Forbid();
        var source = Key(r.FinancialYearId, r.SourceLevel, r.SourceId);
        var dest = Key(r.FinancialYearId, r.DestinationLevel, r.DestinationId);
        var canSource = await CanAccessBudgetAsync(Actor, stageRole, source, ct);
        var canDest = await CanAccessBudgetAsync(Actor, stageRole, dest, ct);
        if (!canSource.HasValue || !canDest.HasValue) return IntegrationUnavailable();
        if (!canSource.Value || !canDest.Value) return Forbid();
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
