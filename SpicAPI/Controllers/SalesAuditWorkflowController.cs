using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using ExcelDataReader;
using System.Globalization;
using System.Text;

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
                              Csr1BudgetRs = c1.Budget,
                              c1.HeadquarterId,
                              c1.LocationId
                          }).SingleOrDefaultAsync(HttpContext.RequestAborted);
        if (info == null) return NotFound();

        // Resolve only actual mapped master rows. Unknown/ambiguous relationships stay null.
        var geography = await ResolveGeographyAsync(info.HeadquarterId, info.LocationId);

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
            IsPlanConfirmed = record?.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed" || record?.IsFinalSubmitted == true,
            State = geography.State,
            Region = geography.Region,
            HQ = geography.HQ
        });
    }


    // Geography is resolved inside this controller; no extra service or class file.
    // The CSR1 entity supplies HeadquarterId and LocationId, but their master schema
    // was not provided. The metadata-based lookup therefore only accepts unambiguous
    // mapped master entities and real rows. It never invents names or IDs.
    private sealed record GeographyNames(string? State, string? Region, string? HQ);

    private async Task<GeographyNames> ResolveGeographyAsync(int? headquarterId, int? locationId)
    {
        var ct = HttpContext.RequestAborted;
        var hq = await FindMasterAsync("HQ", headquarterId, ct);
        var location = await FindMasterAsync("Location", locationId, ct);

        // Prefer the selected HQ's actual relation, then the selected location's relation.
        var regionId = ReadInt(hq, "RegionId") ?? ReadInt(location, "RegionId");
        var region = await FindMasterAsync("Region", regionId, ct);

        // State often belongs to the region; check HQ and location as safe alternatives.
        var stateId = ReadInt(region, "StateId")
            ?? ReadInt(hq, "StateId")
            ?? ReadInt(location, "StateId");
        var state = await FindMasterAsync("State", stateId, ct);

        return new GeographyNames(
            GetDisplayName(state, "State"),
            GetDisplayName(region, "Region"),
            GetDisplayName(hq, "HQ"));
    }

    private async Task<object?> FindMasterAsync(string kind, int? id, CancellationToken ct)
    {
        if (id is not > 0) return null;

        // Prefer conventional *exact* entity names. Any ambiguous result is rejected.
        var names = kind switch
        {
            "HQ" => new[] { "Headquarter", "Headquarters", "HeadQuarter", "HQ", "HeadquarterMaster", "HeadQuarterMaster", "HQMaster", "HeadquarterInformation" },
            "Region" => new[] { "Region", "Regions", "RegionMaster", "RegionInformation" },
            "State" => new[] { "State", "States", "StateMaster", "StateInformation" },
            _ => new[] { "Location", "Locations", "LocationMaster", "LocationInformation" }
        };

        var candidates = _db.Model.GetEntityTypes()
            .Where(e => e.ClrType != null && names.Contains(e.ClrType.Name, StringComparer.OrdinalIgnoreCase)
                && e.FindPrimaryKey()?.Properties.Count == 1)
            .ToList();
        if (candidates.Count != 1) return null;

        var type = candidates[0];
        var pk = type.FindPrimaryKey()!.Properties[0];
        object key;
        try
        {
            key = Convert.ChangeType(id.Value, Nullable.GetUnderlyingType(pk.ClrType) ?? pk.ClrType);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }

        // Only reads a configured EF Core entity; never performs a write.
        return await _db.FindAsync(type.ClrType, new object?[] { key }, ct);
    }

    private static int? ReadInt(object? entity, string propertyName)
    {
        if (entity == null) return null;
        var p = entity.GetType().GetProperties()
            .SingleOrDefault(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        var raw = p?.GetValue(entity);
        if (raw == null) return null;
        try { return Convert.ToInt32(raw); }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        { return null; }
    }

    private static string? GetDisplayName(object? entity, string kind)
    {
        if (entity == null) return null;
        var names = kind switch
        {
            "HQ" => new[] { "Name", "HQName", "HeadquarterName", "HeadQuarterName" },
            "State" => new[] { "Name", "StateName" },
            "Region" => new[] { "Name", "RegionName" },
            _ => new[] { "Name" }
        };
        foreach (var name in names)
        {
            var prop = entity.GetType().GetProperties()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (prop?.GetValue(entity) is string value && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    // Only initializes a draft. Does NOT delete or replace product plan/achievement lines.
    [HttpPost("overview/save-draft")]
    public async Task<IActionResult> SaveOverviewDraft([FromQuery] int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest("Valid csr2Id is required.");
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var record = await _db.Set<SalesAuditRecord>()
            .SingleOrDefaultAsync(x => x.CSR2Id == csr2Id, HttpContext.RequestAborted);
        if (record?.IsFinalSubmitted == true || record?.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed")
            return Conflict("Confirmed or final-submitted plan is read-only.");
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
            IsPlanConfirmed = audit?.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed" || audit?.IsFinalSubmitted == true,
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
        if ((audit?.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed") && !finalSubmit)
            return Conflict("Sales Plan is confirmed and locked; do not overwrite it through draft save.");
        if (finalSubmit && audit?.DocumentStatus is not ("Plan Confirmed" or "In Progress"))
            return Conflict("Confirm the Sales Plan in Tab 4 before final achievement submission.");
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

    // Dealer mappings store only CSR2Id and DealerRegistrationId; no commitment columns.
    // Tab 4: confirmation of the planned commitment only. Actual achievements
    // are submitted separately in Tab 5. No new entity or database migration.
    [HttpPost("confirm-plan")]
    public async Task<IActionResult> ConfirmPlan([FromQuery] int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest("Valid csr2Id is required.");
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var ct = HttpContext.RequestAborted;
        var audit = await _db.Set<SalesAuditRecord>()
            .SingleOrDefaultAsync(x => x.CSR2Id == csr2Id, ct);
        if (audit is null) return Conflict("Save Sales Plan before confirming it.");
        if (audit.IsFinalSubmitted) return Conflict("Sales Audit is already finally submitted.");
        if (audit.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed")
            return Ok(new { CSR2Id = csr2Id, IsPlanConfirmed = true, DocumentStatus = audit.DocumentStatus });
        if (!audit.SchemeFrom.HasValue || !audit.SchemeTo.HasValue ||
            audit.SchemeFrom.Value.Date > audit.SchemeTo.Value.Date)
            return BadRequest("Save a valid scheme period in Sales Plan first.");

        var budget = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                            join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                            where c2.Id == csr2Id
                            select (decimal?)c1.Budget).SingleOrDefaultAsync(ct);
        if (!budget.HasValue) return NotFound("CSR-2 programme budget was not found.");
        var requiredRs = (budget.Value + (audit.AdditionalApprovedBudgetRs ?? 0m))
            / 150_000m * 10_000_000m;

        var planLines = await _db.Set<SalesAuditPlanLine>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id)
            .Select(x => new { x.ProductId, x.QuantityMT, x.ValueRs })
            .ToListAsync(ct);
        if (planLines.Count == 0 || planLines.Any(x => x.ProductId <= 0 || x.QuantityMT < 0 || x.ValueRs < 0) ||
            planLines.GroupBy(x => x.ProductId).Any(x => x.Count() > 1))
            return BadRequest("Save valid product-wise Sales Plan lines before confirmation.");
        var planTotal = planLines.Sum(x => x.ValueRs);
        if (planTotal < requiredRs)
            return BadRequest("Planned turnover is below the required turnover.");

        // Only selected dealer membership is stored in the new entity.
        // Product plan target remains validated above; per-dealer allocation is no longer available.
        var mappedDealerCount = await _db.Set<SalesAuditDealerMapping>().AsNoTracking()
            .CountAsync(x => x.CSR2Id == csr2Id, ct);
        if (mappedDealerCount == 0)
            return BadRequest("Upload at least one dealer before confirming the Sales Plan.");

        audit.DocumentStatus = "Plan Confirmed";
        audit.UpdatedAt = DateTime.UtcNow;
        audit.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _db.SaveChangesAsync(ct);
        return Ok(new { CSR2Id = csr2Id, IsPlanConfirmed = true, DocumentStatus = audit.DocumentStatus });
    }

    public sealed record DealerUploadError(int RowNo, string DealerCode, string Reason);

    // Map by DealerRegistration.Id, not DealerCode or repeated dealer-name fields.
    [HttpGet("dealer-mapping")]
    public async Task<IActionResult> GetDealerMapping([FromQuery] int csr2Id)
    {
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var ct = HttpContext.RequestAborted;
        var audit = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id)
            .Select(x => new { x.IsFinalSubmitted, x.DocumentStatus }).SingleOrDefaultAsync(ct);
        var rows = await (from m in _db.Set<SalesAuditDealerMapping>().AsNoTracking()
                          join d in _db.Set<DealerRegistration>().AsNoTracking()
                          on m.DealerRegistrationId equals d.Id
                          where m.CSR2Id == csr2Id
                          orderby d.FirmName, d.DealerCode
                          select new
                          {
                              m.Id,
                              m.DealerRegistrationId,
                              d.DealerCode,
                              DealerName = d.FirmName,
                              StateId = d.StateId,
                              RegionId = d.Region,
                              HeadquartersName = d.HQ
                          }).ToListAsync(ct);
        var planTotal = await _db.Set<SalesAuditPlanLine>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id).SumAsync(x => (decimal?)x.ValueRs, ct) ?? 0m;
        var budget = await (from c2 in _db.Set<CSR2>().AsNoTracking()
                            join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                            where c2.Id == csr2Id
                            select (decimal?)c1.Budget).SingleOrDefaultAsync(ct);
        if (!budget.HasValue) return NotFound("CSR-2 not found.");
        var additional = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id).Select(x => x.AdditionalApprovedBudgetRs)
            .SingleOrDefaultAsync(ct) ?? 0m;
        return Ok(new
        {
            CSR2Id = csr2Id,
            RequiredTurnoverRs = (budget.Value + additional) / 150_000m * 10_000_000m,
            ProductPlanTotalRs = planTotal,
            IsReadOnly = audit?.IsFinalSubmitted == true || audit?.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed",
            Rows = rows.Select(x => new
            {
                x.Id,
                x.DealerRegistrationId,
                x.DealerCode,
                x.DealerName,
                // Resolve these IDs using your actual state/region masters if UI labels are required.
                StateName = x.StateId.ToString(),
                RegionName = x.RegionId.ToString(),
                HeadquartersName = Convert.ToString(x.HeadquartersName, System.Globalization.CultureInfo.InvariantCulture) ?? ""
            })
        });
    }


    [HttpPost("dealer-mapping/upload")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadDealerMapping(
        [FromQuery] int csr2Id,
        [FromForm] IFormFile? file)
    {
        var ct = HttpContext.RequestAborted;

        // STEP 1: Validate CSR2 and authorization.
        if (csr2Id <= 0)
            return BadRequest(new
            {
                Message = "Invalid CSR2 ID."
            });

        if (!await CanEditAsync(csr2Id))
        {
            return StatusCode(403, new
            {
                Message = "You do not have permission to edit this CSR-2 Sales Audit.",
                Reason = "CSR2_ACCESS_DENIED"
            });
        }

        // STEP 2: Check Sales Audit status.
        var audit = await _db.Set<SalesAuditRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CSR2Id == csr2Id, ct);

        if (audit == null)
            return BadRequest(new
            {
                Message = "Sales Plan must be saved before uploading dealers."
            });

        if (audit.IsFinalSubmitted ||
            audit.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed")
        {
            return Conflict(new
            {
                Message = "Confirmed or final-submitted Sales Audit is read-only."
            });
        }

        // STEP 3: Validate uploaded file.
        if (file == null || file.Length == 0)
            return BadRequest(new
            {
                Message = "Please select an Excel file."
            });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new
            {
                Message = "Maximum upload size is 5 MB."
            });

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        if (extension is not (".xlsx" or ".xls"))
            return BadRequest(new
            {
                Message = "Only .xlsx and .xls files are supported."
            });

        // Dealer uploads are validated by DealerRegistrations.DealerCode only.
        // State, Region and HQ are intentionally not upload eligibility conditions.

        // STEP 5: Read Excel Dealer Codes.
        var errors = new List<DealerUploadError>();

        var codes = new List<(int RowNo, string Code)>();

        var seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        Encoding.RegisterProvider(
            CodePagesEncodingProvider.Instance);

        using (var input = file.OpenReadStream())
        {
            IExcelDataReader reader;

            try
            {
                reader = ExcelReaderFactory.CreateReader(input);
            }
            catch
            {
                return BadRequest(new
                {
                    Message = "Invalid or unreadable Excel workbook."
                });
            }

            using (reader)
            {
                if (!reader.Read())
                    return BadRequest(new
                    {
                        Message = "Excel worksheet is empty."
                    });

                var columns = Enumerable
                    .Range(0, reader.FieldCount)
                    .Where(i =>
                        Norm(Convert.ToString(reader.GetValue(i)) ?? "")
                        == "dealercode")
                    .ToArray();

                if (columns.Length != 1)
                    return BadRequest(new
                    {
                        Message = "Excel must contain exactly one 'Dealer Code' header."
                    });

                int dealerCodeColumn = columns[0];
                int rowNo = 1;

                while (reader.Read())
                {
                    rowNo++;

                    if (rowNo > 10001)
                        return BadRequest(new
                        {
                            Message = "Maximum 10,000 dealer rows are allowed."
                        });

                    var raw = reader.GetValue(dealerCodeColumn);

                    string dealerCode = raw switch
                    {
                        string s => s.Trim(),

                        double d when
                            d >= 0 &&
                            d == Math.Truncate(d) =>
                            d.ToString("0", CultureInfo.InvariantCulture),

                        int i when i >= 0 =>
                            i.ToString(CultureInfo.InvariantCulture),

                        long l when l >= 0 =>
                            l.ToString(CultureInfo.InvariantCulture),

                        null => "",

                        _ => "#INVALID#"
                    };

                    if (string.IsNullOrWhiteSpace(dealerCode))
                    {
                        errors.Add(new DealerUploadError(
                            rowNo, "", "Dealer Code is empty."));
                        continue;
                    }

                    if (dealerCode.Length > 50 ||
                        dealerCode.Any(c =>
                            !char.IsAsciiLetterOrDigit(c) &&
                            c != '/' && c != '_' && c != '-'))
                    {
                        errors.Add(new DealerUploadError(
                            rowNo, dealerCode, "Invalid Dealer Code format."));
                        continue;
                    }

                    if (!seen.Add(dealerCode))
                    {
                        errors.Add(new DealerUploadError(
                            rowNo, dealerCode, "Duplicate Dealer Code in Excel."));
                        continue;
                    }

                    codes.Add((rowNo, dealerCode));
                }
            }
        }

        if (codes.Count == 0)
            return BadRequest(new
            {
                Message = "No valid Dealer Codes found.",
                InvalidRows = errors.Count,
                ValidationErrors = errors
            });

        // STEP 6: Fetch dealers from DealerRegistrations.
        var lookupCodes = codes
            .Select(x => x.Code.ToUpperInvariant())
            .Distinct()
            .ToArray();

        var dealerMaster = await _db.Set<DealerRegistration>()
            .AsNoTracking()
            .Where(x =>
                x.DealerCode != null &&
                lookupCodes.Contains(x.DealerCode.Trim().ToUpper()))
            .Select(x => new
            {
                x.Id,
                x.DealerCode,
                x.FirmName
            })
            .ToListAsync(ct);

        var dealerLookup = dealerMaster
            .GroupBy(
                x => x.DealerCode.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.ToList(),
                StringComparer.OrdinalIgnoreCase);

        // STEP 7: Validate uploaded dealers.
        var acceptedDealerIds = new List<int>();

        foreach (var row in codes)
        {
            if (!dealerLookup.TryGetValue(
                    row.Code, out var matches) ||
                matches.Count == 0)
            {
                errors.Add(new DealerUploadError(
                    row.RowNo,
                    row.Code,
                    "Dealer Code not found in DealerRegistrations."));

                continue;
            }

            if (matches.Count != 1)
            {
                errors.Add(new DealerUploadError(
                    row.RowNo,
                    row.Code,
                    "Duplicate or ambiguous Dealer Code in master."));

                continue;
            }

            var dealer = matches[0];

            acceptedDealerIds.Add(dealer.Id);
        }

        // Import valid dealer codes even when other rows are invalid or unmatched.
        // Never remove previously saved dealer mappings.
        if (acceptedDealerIds.Count == 0)
            return BadRequest(new
            {
                Message = "No Dealer Codes matched DealerRegistrations. Existing mappings unchanged.",
                ImportedRows = 0,
                InvalidRows = errors.Count,
                ValidationErrors = errors
            });

        // STEP 9: Save only CSR2Id and DealerRegistrationId.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // Recheck lock status in the save transaction.
            await _db.Set<SalesAuditRecord>()
                .FromSqlInterpolated(
                    $@"SELECT * FROM ""SalesAuditRecords""
                   WHERE ""CSR2Id"" = {csr2Id}
                   FOR UPDATE")
                .AsTracking()
                .SingleAsync(ct);

            var currentAudit = await _db.Set<SalesAuditRecord>()
                .AsNoTracking()
                .SingleAsync(x => x.CSR2Id == csr2Id, ct);

            if (currentAudit.IsFinalSubmitted ||
                currentAudit.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed")
            {
                await transaction.RollbackAsync(ct);
                return Conflict(new
                {
                    Message = "Sales Audit became read-only during upload."
                });
            }

            // Add-only merge: the same dealer must not be saved twice for this CSR-2.
            var existingDealerIds = await _db.Set<SalesAuditDealerMapping>()
                .AsNoTracking()
                .Where(x => x.CSR2Id == csr2Id)
                .Select(x => x.DealerRegistrationId)
                .ToListAsync(ct);

            var existingSet = existingDealerIds.ToHashSet();
            var distinctMatched = acceptedDealerIds.Distinct().ToArray();
            var newMappings = distinctMatched
                .Where(dealerId => !existingSet.Contains(dealerId))
                .Select(dealerId => new SalesAuditDealerMapping
                {
                    CSR2Id = csr2Id,
                    DealerRegistrationId = dealerId
                })
                .ToList();

            if (newMappings.Count > 0)
            {
                _db.Set<SalesAuditDealerMapping>().AddRange(newMappings);
                await _db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);

            return Ok(new
            {
                Message = "Dealer upload processed. Existing mappings preserved.",
                ImportedRows = distinctMatched.Length,
                AddedRows = newMappings.Count,
                AlreadyMappedRows = distinctMatched.Length - newMappings.Count,
                InvalidRows = errors.Count,
                ValidationErrors = errors
            });
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }


    // Membership is saved by the upload itself. The old monetary save API is deprecated.
    [HttpPost("dealer-mapping/save")]
    public IActionResult SaveDealerCommitments() => StatusCode(410,
        new { message = "Dealer commitments are no longer stored. Upload the Dealer Code list instead." });

    [HttpDelete("dealer-mapping/{id:int}")]
    public async Task<IActionResult> DeleteDealerMapping([FromRoute] int id, [FromQuery] int csr2Id)
    {
        if (id <= 0 || csr2Id <= 0) return BadRequest("Valid IDs are required.");
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var ct = HttpContext.RequestAborted;
        if (await _db.Set<SalesAuditRecord>().AsNoTracking()
            .AnyAsync(x => x.CSR2Id == csr2Id &&
                (x.IsFinalSubmitted || x.DocumentStatus == "Plan Confirmed" || x.DocumentStatus == "In Progress" || x.DocumentStatus == "Completed"), ct))
            return Conflict("Confirmed or final-submitted Sales Audit is read-only.");
        var row = await _db.Set<SalesAuditDealerMapping>()
            .SingleOrDefaultAsync(x => x.Id == id && x.CSR2Id == csr2Id, ct);
        if (row is null) return NotFound();
        _db.Set<SalesAuditDealerMapping>().Remove(row);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // Tab 5: import a complete SAP invoice snapshot as product achievements.
    // Uploaded dealer commitments are NEVER implicitly counted as verified achievement.
    // Each accepted invoice item is checked against the confirmed scheme and Product master.
    [HttpPost("tracking/import-achievements")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ImportTrackingAchievements([FromQuery] int csr2Id, [FromForm] IFormFile? file)
    {
        if (csr2Id <= 0) return BadRequest("Valid csr2Id is required.");
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var ct = HttpContext.RequestAborted;
        var audit = await _db.Set<SalesAuditRecord>()
            .SingleOrDefaultAsync(x => x.CSR2Id == csr2Id, ct);
        if (audit == null || audit.DocumentStatus is not ("Plan Confirmed" or "In Progress") || audit.IsFinalSubmitted)
            return Conflict("Confirm the sales plan first. Completed audits cannot be changed.");
        // Never allow the legacy full-snapshot importer to overwrite invoice-backed totals.
        if (await _db.Set<SalesAuditMatchedInvoice>().AsNoTracking()
            .AnyAsync(x => x.CSR2Id == csr2Id, ct))
            return Conflict("This audit uses saved matched SAP invoices. Use Save Sales Progress instead of the legacy tracking importer.");
        if (!audit.SchemeFrom.HasValue || !audit.SchemeTo.HasValue ||
            audit.SchemeFrom.Value.Date > audit.SchemeTo.Value.Date)
            return Conflict("A valid confirmed scheme period is required.");
        if (file == null || file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            return BadRequest("Upload a non-empty SAP Excel file up to 5 MB.");
        if (Path.GetExtension(file.FileName).ToLowerInvariant() is not (".xlsx" or ".xls"))
            return BadRequest("Only xlsx and xls workbooks are supported.");

        // Map ONLY by unambiguous Product.Name. Product Code cannot be inferred to
        // ProductId until the authoritative SPIC product-code master is supplied.
        var products = await _db.Set<Product>().AsNoTracking().Where(x => x.IsActive)
            .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var productByName = products.GroupBy(x => Norm(x.Name))
            .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single().Id);

        var sums = new Dictionary<int, (decimal Quantity, decimal Value)>();
        var invoiceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var issues = new List<string>();
        int total = 0;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using (var stream = file.OpenReadStream())
        {
            IExcelDataReader reader;
            try { reader = ExcelReaderFactory.CreateReader(stream); }
            catch (Exception) { return BadRequest("Invalid Excel workbook."); }
            using (reader)
            {
                if (!reader.Read()) return BadRequest("The workbook is empty.");
                var headers = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => new { Name = Norm(Convert.ToString(reader.GetValue(i)) ?? ""), Index = i })
                    .Where(x => x.Name.Length > 0)
                    .GroupBy(x => x.Name)
                    .ToDictionary(x => x.Key, x => x.First().Index);
                string[] required = { "billingdocument", "billingdate", "item", "productcode", "productname", "quantity", "unitofmeasure", "grossamount" };
                var missing = required.Where(x => !headers.ContainsKey(x)).ToArray();
                if (missing.Length > 0) return BadRequest("Missing columns: " + string.Join(", ", missing));
                object? Cell(string header) => headers.TryGetValue(Norm(header), out var i) ? reader.GetValue(i) : null;
                string Text(string header) => Convert.ToString(Cell(header), CultureInfo.InvariantCulture)?.Trim() ?? "";
                int row = 1;
                while (reader.Read())
                {
                    row++;
                    if (row > 10002) return BadRequest("Maximum 10,000 invoice rows per upload.");
                    string invoice = Text("Billing Document"), item = Text("Item"),
                        code = Text("Product Code"), name = Text("Product Name");
                    if (string.IsNullOrWhiteSpace(invoice) && string.IsNullOrWhiteSpace(name) &&
                        string.IsNullOrWhiteSpace(code)) continue;
                    total++;
                    if (string.IsNullOrWhiteSpace(invoice) || string.IsNullOrWhiteSpace(item) ||
                        string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                    { issues.Add($"Row {row}: invoice, item and product identifiers are required."); continue; }
                    string key = invoice + "\u001f" + item + "\u001f" + code;
                    if (!invoiceKeys.Add(key))
                    { issues.Add($"Row {row}: duplicate invoice/item/product in this upload."); continue; }
                    if (!string.Equals(Text("Unit of Measure"), "MT", StringComparison.OrdinalIgnoreCase) ||
                        !TryMoney(Text("Quantity"), out var qty) || qty < 0 ||
                        !TryMoney(Text("Gross Amount"), out var amount) || amount < 0)
                    { issues.Add($"Row {row}: invalid MT quantity or gross amount."); continue; }
                    if (!TryInvoiceDate(Cell("Billing Date"), out var billed) ||
                        billed.Date < audit.SchemeFrom.Value.Date ||
                        billed.Date > audit.SchemeTo.Value.Date || billed.Date > DateTime.Today)
                    { issues.Add($"Row {row}: billing date is missing, future-dated or outside the scheme period."); continue; }
                    if (!productByName.TryGetValue(Norm(name), out var productId))
                    { issues.Add($"Row {row}: product '{name}' does not uniquely match an active Product.Name."); continue; }
                    var previous = sums.GetValueOrDefault(productId);
                    sums[productId] = (previous.Quantity + qty, previous.Value + amount);
                }
            }
        }
        // All-or-nothing: never silently discard invoice lines and report an incomplete turnover.
        if (issues.Count > 0)
            return BadRequest(new
            {
                Message = "No changes saved. Resolve every invoice validation error and re-upload a complete snapshot.",
                Errors = issues.Take(50).ToList(),
                TotalErrors = issues.Count
            });
        if (total == 0 || sums.Count == 0) return BadRequest("No valid invoice items found.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var old = await _db.Set<SalesAuditAchievementLine>()
            .Where(x => x.CSR2Id == csr2Id).ToListAsync(ct);
        _db.Set<SalesAuditAchievementLine>().RemoveRange(old);
        // Apply removal before insertion in case a unique CSR2Id/ProductId index exists.
        await _db.SaveChangesAsync(ct);
        _db.Set<SalesAuditAchievementLine>().AddRange(sums.Select(x => new SalesAuditAchievementLine
        {
            CSR2Id = csr2Id,
            ProductId = x.Key,
            QuantityMT = x.Value.Quantity,
            ValueRs = x.Value.Value
        }));
        audit.ActualTurnover = sums.Sum(x => x.Value.Value) / 10_000_000m; // crore, as in existing list contract
        audit.UpdatedAt = DateTime.UtcNow;
        audit.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new
        {
            CSR2Id = csr2Id,
            ImportedInvoiceLines = total,
            ProductAchievementLines = sums.Count,
            ActualTurnoverRs = sums.Sum(x => x.Value.Value),
            Warning = "Totals are validated against uploaded invoice fields, not independently verified with SAP."
        });
    }

    private static bool TryInvoiceDate(object? raw, out DateTime date)
    {
        date = default;
        if (raw is DateTime dateTime) { date = dateTime.Date; return true; }
        if (raw is double number && number >= 1 && number < 2958466)
        { try { date = DateTime.FromOADate(number).Date; return true; } catch (ArgumentException) { return false; } }
        var text = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim();
        return DateTime.TryParseExact(text,
            new[] { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "d-M-yyyy", "d/M/yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static string Norm(string text)
    {
        return new string(
            (text ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static bool TryMoney(string? text, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Replace(",", "")
                   .Replace("₹", "")
                   .Trim();

        return decimal.TryParse(
            text,
            System.Globalization.NumberStyles.Number |
            System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);
    }

    // Read-only comparison: existing dealer mappings + planned products + standalone SAP sales.
    // No CSR ID is stored on SalesAuditDealerSales. No achievement is written here.
    [HttpGet("dealer-mapping/sap-sales")]
    public async Task<IActionResult> GetMappedSapSales([FromQuery] int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest(new { message = "Valid csr2Id required." });
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var ct = HttpContext.RequestAborted;
        var audit = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id)
            .Select(x => new { x.SchemeFrom, x.SchemeTo }).FirstOrDefaultAsync(ct);
        if (audit?.SchemeFrom == null || audit.SchemeTo == null || audit.SchemeFrom > audit.SchemeTo)
            return BadRequest(new { message = "Save a valid From Date / To Date in Sales Plan first." });

        var mapped = await (from m in _db.Set<SalesAuditDealerMapping>().AsNoTracking()
                            join d in _db.Set<DealerRegistration>().AsNoTracking()
                                on m.DealerRegistrationId equals d.Id
                            where m.CSR2Id == csr2Id
                            select new { d.DealerCode, DealerName = d.FirmName }).ToListAsync(ct);
        var plan = await (from p in _db.Set<SalesAuditPlanLine>().AsNoTracking()
                          join product in _db.Set<Product>().AsNoTracking() on p.ProductId equals product.Id
                          where p.CSR2Id == csr2Id
                          select new { p.ProductId, ProductName = product.Name, p.QuantityMT, p.ValueRs }).ToListAsync(ct);
        // Confirmed master relationship is Product.Id/Name. No verified SAP product-code mapping exists here.
        // Only unambiguous exact normalized Product.Name <-> SAP Product Name is accepted.
        string NormalizeProduct(string name) => new string((name ?? "")
            .Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        var planByName = plan.GroupBy(x => NormalizeProduct(x.ProductName))
            .Where(g => g.Key.Length > 0 && g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single());
        var ambiguousNames = plan.GroupBy(x => NormalizeProduct(x.ProductName))
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

        // SAP Customer Number may have ONE leading letter (e.g. D47420546).
        // DealerRegistrations.DealerCode stores the code without that letter (47420546).
        // Do not change stored source JSON or unrelated dealer-upload validation.
        static string DealerKey(string? code)
        {
            var key = (code ?? "").Trim().ToUpperInvariant();
            return key.Length > 1 && key[0] >= 'A' && key[0] <= 'Z'
                ? key.Substring(1) : key;
        }

        var dealerCodes = mapped.Where(x => !string.IsNullOrWhiteSpace(x.DealerCode))
            .Select(x => x.DealerCode.Trim().ToUpperInvariant()).Distinct().ToArray();
        var dealerNames = mapped.Where(x => !string.IsNullOrWhiteSpace(x.DealerCode))
            .GroupBy(x => x.DealerCode.Trim().ToUpperInvariant())
            .ToDictionary(x => x.Key, x => x.First().DealerName);
        var dateFrom = audit.SchemeFrom.Value.Date;
        var dateTo = audit.SchemeTo.Value.Date;
        var matches = new List<object>();
        var amountsByProduct = new Dictionary<int, (decimal Quantity, decimal Gross, int Lines)>();
        var unmatchedProducts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int excludedOutsideDates = 0, excludedUnits = 0, invalidDates = 0, candidateRows = 0;
        if (dealerCodes.Length > 0 && planByName.Count > 0)
        {
            var connection = _db.Database.GetDbConnection();
            var opened = connection.State != System.Data.ConnectionState.Open;
            if (opened) await connection.OpenAsync(ct);
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT ""Id"", ""SourceRowJson""::text
                    FROM ""SalesAuditDealerSales""
                    WHERE (CASE
                        WHEN UPPER(BTRIM(COALESCE(""SourceRowJson""->>'Customer Number', ''))) ~ '^[A-Z]'
                          THEN SUBSTRING(UPPER(BTRIM(""SourceRowJson""->>'Customer Number')) FROM 2)
                        ELSE UPPER(BTRIM(COALESCE(""SourceRowJson""->>'Customer Number', '')))
                    END) = ANY(@codes)
                    ORDER BY ""Id""";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@codes";
                parameter.Value = dealerCodes;
                command.Parameters.Add(parameter);
                using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    candidateRows++;
                    using var document = System.Text.Json.JsonDocument.Parse(reader.GetString(1));
                    var row = document.RootElement;
                    string Field(string key)
                    {
                        if (row.TryGetProperty(key, out var value)) return value.ToString();
                        var keyNorm = new string(key.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
                        foreach (var entry in row.EnumerateObject())
                            if (new string(entry.Name.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray()) == keyNorm)
                                return entry.Value.ToString();
                        return "";
                    }
                    var dateText = Field("Billing Date");
                    DateTime billDate;
                    if (!DateTime.TryParseExact(dateText,
                         new[] { "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "d/M/yyyy", "d-M-yyyy" },
                         System.Globalization.CultureInfo.InvariantCulture,
                         System.Globalization.DateTimeStyles.None, out billDate))
                    { invalidDates++; continue; }
                    if (billDate.Date < dateFrom || billDate.Date > dateTo)
                    { excludedOutsideDates++; continue; }
                    var sapProduct = Field("Product Name");
                    var productNameKey = NormalizeProduct(sapProduct);
                    if (ambiguousNames.Contains(productNameKey) || !planByName.TryGetValue(productNameKey, out var line))
                    { unmatchedProducts.Add(sapProduct); continue; }
                    var unit = Field("Unit of Measure").Trim();
                    if (!string.Equals(unit, "MT", StringComparison.OrdinalIgnoreCase))
                    { excludedUnits++; continue; }
                    bool ParseMoney(string text, out decimal value) => decimal.TryParse(
                        text.Replace(",", "").Replace("₹", ""),
                        System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowLeadingSign,
                        System.Globalization.CultureInfo.InvariantCulture, out value);
                    if (!ParseMoney(Field("Quantity"), out var qty) ||
                        !ParseMoney(Field("Gross Amount"), out var gross)) continue;
                    var sourceCustomerNumber = Field("Customer Number").Trim();
                    var code = DealerKey(sourceCustomerNumber);
                    var previous = amountsByProduct.TryGetValue(line.ProductId, out var sum) ? sum : default;
                    amountsByProduct[line.ProductId] = (previous.Quantity + qty, previous.Gross + gross, previous.Lines + 1);
                    matches.Add(new
                    {
                        SaleId = reader.GetInt64(0),
                        DealerCode = code,
                        SapCustomerNumber = sourceCustomerNumber,
                        DealerName = dealerNames.TryGetValue(code, out var dealerName) ? dealerName : Field("Customer Name"),
                        BillingDate = billDate.Date,
                        BillingDocument = Field("Billing Document"),
                        Item = Field("Item"),
                        ProductId = line.ProductId,
                        ProductCode = Field("Product Code"),
                        ProductName = line.ProductName,
                        QuantityMT = qty,
                        GrossAmountRs = gross
                    });
                }
            }
            finally { if (opened) await connection.CloseAsync(); }
        }
        var productSummary = plan.Select(x =>
        {
            var actual = amountsByProduct.TryGetValue(x.ProductId, out var sum) ? sum : default;
            return new
            {
                x.ProductId,
                x.ProductName,
                PlannedQuantityMT = x.QuantityMT,
                PlannedValueRs = x.ValueRs,
                ActualQuantityMT = actual.Quantity,
                ActualGrossAmountRs = actual.Gross,
                MatchedInvoiceLines = actual.Lines,
                BalanceQuantityMT = x.QuantityMT - actual.Quantity,
                BalanceValueRs = x.ValueRs - actual.Gross
            };
        }).ToList();
        return Ok(new
        {
            CSR2Id = csr2Id,
            SchemeFrom = dateFrom,
            SchemeTo = dateTo,
            MappedDealers = mapped.Count,
            PlannedProducts = plan.Count,
            MatchedInvoiceLines = matches.Count,
            CandidateInvoiceLines = candidateRows,
            ExcludedOutsideDates = excludedOutsideDates,
            ExcludedDifferentUnits = excludedUnits,
            InvalidDates = invalidDates,
            UnmatchedSapProductNames = unmatchedProducts.OrderBy(x => x).Take(30).ToArray(),
            TotalPlannedValueRs = productSummary.Sum(x => x.PlannedValueRs),
            TotalMatchedGrossRs = productSummary.Sum(x => x.ActualGrossAmountRs),
            Products = productSummary,
            Rows = matches
        });
    }


    // Daily matched-invoice progress is independent from plan confirmation.
    // Incremental, idempotent upsert by (CSR2Id, SourceSaleId); never replace historical days.
    // The UI sends CSR2Id only; invoice amounts and identities are always reconstructed on server.
    private sealed class MatchedSaveResponse
    {
        public int CSR2Id { get; set; }
        public DateTime SchemeFrom { get; set; }
        public DateTime SchemeTo { get; set; }
        public List<MatchedSaveRow> Rows { get; set; } = new();
    }
    private sealed class MatchedSaveRow
    {
        public long SaleId { get; set; }
        public string DealerCode { get; set; } = "";
        public string DealerName { get; set; } = "";
        public DateTime BillingDate { get; set; }
        public string BillingDocument { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal QuantityMT { get; set; }
        public decimal GrossAmountRs { get; set; }
    }

    [HttpPost("matched-invoices/save")]
    public async Task<IActionResult> SaveMatchedInvoices([FromQuery] int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest(new { message = "Valid CSR-2 ID is required." });
        if (!await CanEditAsync(csr2Id)) return Forbid();
        var ct = HttpContext.RequestAborted;

        // Same matching function as the read-only invoice table: mapped dealer,
        // planned product name, scheme date and MT unit.
        var matchingAction = await GetMappedSapSales(csr2Id);
        if (matchingAction is not OkObjectResult matchedResult || matchedResult.Value is null)
            return matchingAction;
        var json = System.Text.Json.JsonSerializer.Serialize(matchedResult.Value);
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<MatchedSaveResponse>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (snapshot is null) return Conflict(new { message = "Matched SAP invoice response could not be read." });
        if (snapshot.Rows.Count == 0)
            return Conflict(new { message = "No eligible matched invoices found. Upload daily SAP sales and refresh." });
        if (snapshot.Rows.Count > 100000)
            return BadRequest(new { message = "Too many invoice lines in one save." });
        if (snapshot.Rows.Any(x => x.SaleId <= 0 || x.ProductId <= 0 ||
            x.GrossAmountRs < 0 || x.QuantityMT < 0 ||
            x.BillingDate.Date < snapshot.SchemeFrom.Date ||
            x.BillingDate.Date > snapshot.SchemeTo.Date ||
            x.BillingDate.Date > DateTime.UtcNow.Date ||
            string.IsNullOrWhiteSpace(x.BillingDocument) ||
            string.IsNullOrWhiteSpace(x.DealerCode)))
            return Conflict(new { message = "Matched SAP records contain invalid identifiers, amounts, or dates. No changes saved." });
        if (snapshot.Rows.GroupBy(x => x.SaleId).Any(x => x.Count() > 1))
            return Conflict(new { message = "Matching returned duplicate SAP source sale IDs. No changes saved." });

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        // Serialize concurrent saves for the same audit. The table name comes from
        // the existing SalesAuditRecord SQL mapping shown in the supplied code.
        var audit = await _db.Set<SalesAuditRecord>().FromSqlInterpolated(
            $@"SELECT * FROM ""SalesAuditRecords"" WHERE ""CSR2Id"" = {csr2Id} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (audit is null) return Conflict(new { message = "Save and confirm the Sales Plan before daily achievement updates." });
        if (audit.IsFinalSubmitted || audit.DocumentStatus == "Completed")
            return Conflict(new { message = "This Sales Audit is completed and closed. No further changes permitted." });
        if (audit.DocumentStatus is not ("Plan Confirmed" or "In Progress"))
            return Conflict(new { message = "Confirm the Sales Plan before saving daily sales progress." });
        if (!audit.SchemeFrom.HasValue || !audit.SchemeTo.HasValue ||
            snapshot.SchemeFrom.Date != audit.SchemeFrom.Value.Date ||
            snapshot.SchemeTo.Date != audit.SchemeTo.Value.Date)
            return Conflict(new { message = "Scheme period changed during matching. Refresh and retry." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var now = DateTime.UtcNow;
        var existing = await _db.Set<SalesAuditMatchedInvoice>()
            .Where(x => x.CSR2Id == csr2Id).ToListAsync(ct);

        // Previous totals from legacy imports cannot safely be reconciled
        // against invoice identities without a one-time migration.
        if (existing.Count == 0 && (audit.ActualTurnover > 0m ||
            await _db.Set<SalesAuditAchievementLine>().AsNoTracking()
                .AnyAsync(x => x.CSR2Id == csr2Id, ct)))
            return Conflict(new { message = "Existing achievement totals have no invoice identities. Reconcile legacy achievements first; no changes saved." });

        var currentMatches = snapshot.Rows.ToDictionary(x => x.SaleId);
        var existingIds = existing.Select(x => x.SourceSaleId).ToHashSet();
        // Never silently lose historical achievement when source rows are
        // removed/edited or stop matching the confirmed audit definition.
        var unresolvable = existingIds.Where(id => !currentMatches.ContainsKey(id)).ToArray();
        if (unresolvable.Length > 0)
            return Conflict(new { message = "Previously saved SAP sales no longer match the current source/dealer/product/date rules. Reconcile before saving; no data changed.", MissingSourceSaleIds = unresolvable.Take(30) });

        var addedRows = snapshot.Rows
            .Where(row => !existingIds.Contains(row.SaleId))
            .Select(row => new SalesAuditMatchedInvoice
            {
                CSR2Id = csr2Id,
                SourceSaleId = row.SaleId,
                CreatedAt = now
            }).ToList();
        _db.Set<SalesAuditMatchedInvoice>().AddRange(addedRows);

        // Only store the link IDs. All quantities, values, dates, dealer and
        // product attributes are read from the authoritative source snapshot.
        var savedSaleIds = existingIds.Concat(addedRows.Select(x => x.SourceSaleId)).ToHashSet();
        var savedSourceRows = snapshot.Rows
            .Where(x => savedSaleIds.Contains(x.SaleId)).ToList();
        decimal totalRs = savedSourceRows.Sum(x => x.GrossAmountRs);
        if (!audit.RequiredTurnover.HasValue || audit.RequiredTurnover.Value <= 0m)
            return Conflict(new { message = "Required Turnover is missing or invalid. Cannot save achievement." });
        decimal requiredRs = audit.RequiredTurnover.Value * 10_000_000m;
        if (requiredRs <= 0m)
            return Conflict(new { message = "Required Turnover must be greater than zero before saving achievement." });
        // Store exact source-invoice achievement in crores. This needs
        // SalesAuditRecords.ActualTurnover numeric(20,9) in PostgreSQL.
        // For example: Rs 207565.00 => 0.020756500 Cr (no lost paise).
        audit.ActualTurnover = totalRs / 10_000_000m;
        var completed = totalRs >= requiredRs;
        audit.DocumentStatus = completed ? "Completed" : "In Progress";
        audit.IsFinalSubmitted = completed;
        if (completed) audit.FinalSubmittedAt = now;
        audit.UpdatedAt = now;
        audit.UpdatedBy = userId;

        // Rebuild only the existing product-level totals. No invoice values
        // are copied to SalesAuditMatchedInvoices.
        var oldLines = await _db.Set<SalesAuditAchievementLine>()
            .Where(x => x.CSR2Id == csr2Id).ToListAsync(ct);
        _db.Set<SalesAuditAchievementLine>().RemoveRange(oldLines);
        await _db.SaveChangesAsync(ct);
        _db.Set<SalesAuditAchievementLine>().AddRange(savedSourceRows
            .GroupBy(x => x.ProductId)
            .Select(g => new SalesAuditAchievementLine
            {
                CSR2Id = csr2Id,
                ProductId = g.Key,
                QuantityMT = g.Sum(x => x.QuantityMT),
                ValueRs = g.Sum(x => x.GrossAmountRs)
            }));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new
        {
            CSR2Id = csr2Id,
            AddedInvoiceLines = addedRows.Count,
            RefreshedInvoiceLines = existing.Count,
            SavedInvoiceLines = savedSaleIds.Count,
            RequiredTurnoverRs = requiredRs,
            AchievementAmountRs = totalRs,
            ActualTurnoverCr = audit.ActualTurnover,
            ProgressPercent = totalRs / requiredRs * 100m,
            audit.DocumentStatus,
            IsAuditCompleted = completed
        });
    }

}
