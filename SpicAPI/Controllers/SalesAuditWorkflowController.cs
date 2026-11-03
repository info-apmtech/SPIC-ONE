using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace SpicAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/SalesAudit")]
public sealed class SalesAuditWorkflowController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SalesAuditEligibilityOptions _options;
    public SalesAuditWorkflowController(AppDbContext db, IOptions<SalesAuditEligibilityOptions> options)
    { _db = db; _options = options.Value; }

    public sealed class LineDto
    {
        public int ProductId { get; set; }
        public decimal QuantityMT { get; set; }
        public decimal ValueRs { get; set; }
    }
    public sealed class SaveAuditDto
    {
        public int CSR2Id { get; set; }
        public DateTime? SchemeFrom { get; set; }
        public DateTime? SchemeTo { get; set; }
        public decimal? AdditionalApprovedBudgetRs { get; set; }
        public List<LineDto> Plan { get; set; } = new();
        public List<LineDto> Achievement { get; set; } = new();
    }

    private async Task<bool> CanEditAsync(int csr2Id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || csr2Id <= 0) return false;
        if (!await _db.Set<EmployeeRegistration.Employeelogin>().AsNoTracking()
                 .AnyAsync(x => x.IsActive && x.UserId == userId, HttpContext.RequestAborted)) return false;
        var eligibleIds = (_options.EligibleProgramIds ?? Array.Empty<int>()).Where(x => x > 0).ToArray();
        return await (from c2 in _db.Set<CSR2>().AsNoTracking()
                      join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                      where c2.Id == csr2Id && c2.Status == CSR2Status.Approved &&
                            c1.IsActive && c1.Status == CSR1Status.Approved && c1.ResponsiblePersonId == userId &&
                            (eligibleIds.Length == 0 || eligibleIds.Contains(c1.ProgramId))
                      select c2.Id).AnyAsync(HttpContext.RequestAborted);
    }

    // CSR-2-linked, read-only overview; values come from actual source records.
    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest("Valid csr2Id is required.");
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var info = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                          join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                          join pm in _db.Set<ProgramMaster>().AsNoTracking() on c1.ProgramId equals pm.Id
                          where c2.Id == csr2Id
                          select new
                          {
                              CSR2Id = c2.Id,
                              CSR1Id = c1.Id,
                              SanctionNumber = c1.SanctionNo,
                              ProgramName = pm.Name,
                              ProgramType = pm.ProgramType.Name,
                              CSR2Status = c2.Status,
                              CSR2ApprovedAt = c2.ApprovedAt,
                              ActualEventDate = c2.ActualEventDate,
                              Location = c2.ActualLocation,
                              Csr1BudgetRs = c1.Budget
                          }).SingleOrDefaultAsync(HttpContext.RequestAborted);
        if (info == null) return NotFound();
        var record = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id)
            .Select(x => new { x.AdditionalApprovedBudgetRs, x.SchemeFrom, x.SchemeTo, x.DocumentStatus, x.IsFinalSubmitted })
            .SingleOrDefaultAsync(HttpContext.RequestAborted);
        // Additional MD budget is a saved value; no MD approval source has been connected yet.
        var additional = record?.AdditionalApprovedBudgetRs ?? 0m;
        var total = info.Csr1BudgetRs + additional;
        const decimal budgetPerCroreRs = 150_000m;
        const decimal oneCroreRs = 10_000_000m;
        return Ok(new
        {
            info.CSR2Id,
            info.CSR1Id,
            info.SanctionNumber,
            info.ProgramName,
            info.ProgramType,
            info.Location,
            CSR2Status = info.CSR2Status.ToString(),
            info.CSR2ApprovedAt,
            info.ActualEventDate,
            info.Csr1BudgetRs,
            AdditionalApprovedBudgetRs = additional,
            TotalApprovedBudgetRs = total,
            RequiredTurnoverCr = total / budgetPerCroreRs,
            SchemeFrom = record?.SchemeFrom,
            SchemeTo = record?.SchemeTo,
            DocumentStatus = record?.DocumentStatus ?? "Not Started",
            IsFinalSubmitted = record?.IsFinalSubmitted ?? false,
            // No authoritative mapping to State / Region / HQ or MD approvals provided.
            State = (string?)null,
            Region = (string?)null,
            HQ = (string?)null
        });
    }

    // Only initializes a draft. Does NOT delete or replace product plan/achievement lines.
    [HttpPost("overview/save-draft")]
    public async Task<IActionResult> SaveOverviewDraft([FromQuery] int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest("Valid csr2Id is required.");
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var record = await _db.Set<SalesAuditRecord>()
            .SingleOrDefaultAsync(x => x.CSR2Id == csr2Id, HttpContext.RequestAborted);
        if (record?.IsFinalSubmitted == true) return Conflict("Final submitted audit is read-only.");
        if (record == null)
        {
            record = new SalesAuditRecord
            {
                CSR2Id = csr2Id,
                CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier),
                DocumentStatus = "Draft"
            };
            _db.Set<SalesAuditRecord>().Add(record);
        }
        else if (record.DocumentStatus == "Not Started") record.DocumentStatus = "Draft";
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(new { record.CSR2Id, record.DocumentStatus });
    }

    [HttpGet("details")]
    public async Task<IActionResult> Details([FromQuery] int csr2Id)
    {
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var program = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                             join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                             join p in _db.Set<ProgramMaster>().AsNoTracking() on c1.ProgramId equals p.Id
                             where c2.Id == csr2Id
                             select new
                             {
                                 c2.Id,
                                 c1.SanctionNo,
                                 ProgramName = p.Name,
                                 c2.ActualLocation,
                                 c1.Budget,
                                 c1.ResponsiblePersonId,
                                 c2.ActualEventDate
                             })
                            .SingleAsync(HttpContext.RequestAborted);
        var audit = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id).FirstOrDefaultAsync(HttpContext.RequestAborted);
        var plan = await _db.Set<SalesAuditPlanLine>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id).Select(x => new LineDto { ProductId = x.ProductId, QuantityMT = x.QuantityMT, ValueRs = x.ValueRs })
            .ToListAsync(HttpContext.RequestAborted);
        var achievement = await _db.Set<SalesAuditAchievementLine>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id).Select(x => new LineDto { ProductId = x.ProductId, QuantityMT = x.QuantityMT, ValueRs = x.ValueRs })
            .ToListAsync(HttpContext.RequestAborted);
        // This is the product master, not a static dropdown. Narrow to approved CSR-2 focus
        // products in production once the exact product-group relationship is confirmed.
        var products = await _db.Set<Product>().AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(HttpContext.RequestAborted);
        var totalBudget = program.Budget + (audit?.AdditionalApprovedBudgetRs ?? 0m);
        var requiredTurnoverRs = totalBudget / 150_000m * 10_000_000m; // INR 1 crore turnover per INR 1.5 lakh budget.
        return Ok(new
        {
            csr2Id,
            program.SanctionNo,
            program.ProgramName,
            Location = program.ActualLocation,
            program.ResponsiblePersonId,
            program.ActualEventDate,
            Csr1BudgetRs = program.Budget,
            audit?.AdditionalApprovedBudgetRs,
            TotalApprovedBudgetRs = totalBudget,
            RequiredTurnoverRs = requiredTurnoverRs,
            audit?.SchemeFrom,
            audit?.SchemeTo,
            DocumentStatus = audit?.DocumentStatus ?? "Not Started",
            IsFinalSubmitted = audit?.IsFinalSubmitted ?? false,
            Plan = plan,
            Achievement = achievement,
            Products = products,
            PlanTotalRs = plan.Sum(x => x.ValueRs),
            AchievementTotalRs = achievement.Sum(x => x.ValueRs)
        });
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveAuditDto request, [FromQuery] bool finalSubmit = false)
    {
        if (!await CanEditAsync(request.CSR2Id)) return Forbid();
        if (request.AdditionalApprovedBudgetRs is < 0 ||
            (request.SchemeFrom.HasValue && request.SchemeTo.HasValue && request.SchemeFrom > request.SchemeTo))
            return BadRequest("Invalid budget or scheme period.");
        if (request.Plan == null || request.Achievement == null) return BadRequest("Product lines required.");
        if (request.Plan.Count > 200 || request.Achievement.Count > 500 ||
            request.Plan.Concat(request.Achievement).Any(x => x.ProductId <= 0 || x.ValueRs < 0 || x.QuantityMT < 0))
            return BadRequest("Invalid product rows.");
        if (request.Plan.GroupBy(x => x.ProductId).Any(x => x.Count() > 1) ||
            request.Achievement.GroupBy(x => x.ProductId).Any(x => x.Count() > 1))
            return BadRequest("Duplicate products must be combined into one row.");
        var productIds = request.Plan.Concat(request.Achievement).Select(x => x.ProductId).Distinct().ToArray();
        var validProducts = await _db.Set<Product>().AsNoTracking()
            .CountAsync(x => productIds.Contains(x.Id) && x.IsActive, HttpContext.RequestAborted);
        if (validProducts != productIds.Length) return BadRequest("One or more products are invalid.");
        var budget = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                            join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                            where c2.Id == request.CSR2Id
                            select c1.Budget)
                           .SingleAsync(HttpContext.RequestAborted);
        var requiredRs = (budget + (request.AdditionalApprovedBudgetRs ?? 0m)) / 150_000m * 10_000_000m;
        if (finalSubmit && (!request.SchemeFrom.HasValue || !request.SchemeTo.HasValue ||
                            request.Plan.Count == 0 || request.Plan.Sum(x => x.ValueRs) < requiredRs ||
                            request.Achievement.Sum(x => x.ValueRs) < requiredRs))
            return BadRequest("Final submit requires scheme dates and both plan and achieved sales meeting the required turnover.");
        var audit = await _db.Set<SalesAuditRecord>().SingleOrDefaultAsync(x => x.CSR2Id == request.CSR2Id, HttpContext.RequestAborted);
        if (audit?.IsFinalSubmitted == true) return Conflict("Final-submitted Sales Audit is read-only.");
        if (audit == null)
        {
            audit = new SalesAuditRecord { CSR2Id = request.CSR2Id, CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) };
            _db.Set<SalesAuditRecord>().Add(audit);
        }
        audit.SchemeFrom = request.SchemeFrom;
        audit.SchemeTo = request.SchemeTo;
        audit.AdditionalApprovedBudgetRs = request.AdditionalApprovedBudgetRs;
        audit.RequiredTurnover = requiredRs / 10_000_000m; // Existing list contract is crore.
        audit.ActualTurnover = request.Achievement.Sum(x => x.ValueRs) / 10_000_000m;
        audit.IsFinalSubmitted = finalSubmit;
        audit.DocumentStatus = finalSubmit ? "Completed" : "Draft";
        audit.FinalSubmittedAt = finalSubmit ? DateTime.UtcNow : null;
        audit.UpdatedAt = DateTime.UtcNow;
        audit.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingPlan = await _db.Set<SalesAuditPlanLine>().Where(x => x.CSR2Id == request.CSR2Id).ToListAsync(HttpContext.RequestAborted);
        var existingAchievements = await _db.Set<SalesAuditAchievementLine>().Where(x => x.CSR2Id == request.CSR2Id).ToListAsync(HttpContext.RequestAborted);
        _db.Set<SalesAuditPlanLine>().RemoveRange(existingPlan);
        _db.Set<SalesAuditAchievementLine>().RemoveRange(existingAchievements);
        _db.Set<SalesAuditPlanLine>().AddRange(request.Plan.Select(x => new SalesAuditPlanLine { CSR2Id = request.CSR2Id, ProductId = x.ProductId, QuantityMT = x.QuantityMT, ValueRs = x.ValueRs }));
        _db.Set<SalesAuditAchievementLine>().AddRange(request.Achievement.Select(x => new SalesAuditAchievementLine { CSR2Id = request.CSR2Id, ProductId = x.ProductId, QuantityMT = x.QuantityMT, ValueRs = x.ValueRs }));
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(new { audit.CSR2Id, audit.DocumentStatus, audit.RequiredTurnover, audit.ActualTurnover });
    }
}
