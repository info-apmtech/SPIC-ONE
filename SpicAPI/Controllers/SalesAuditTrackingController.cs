using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace SpicAPI.Controllers;

// Read-only tracking. Does NOT reuse CanEditAsync: responsible-person editing permission
// and RM/SMM/SMDO geography tracking are different authorization checks.
[ApiController]
[Authorize]
[Route("api/SalesAudit/tracking")]
public sealed class SalesAuditTrackingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SalesAuditEligibilityOptions _options;
    private const decimal Crore = 10_000_000m;
    public SalesAuditTrackingController(AppDbContext db, IOptions<SalesAuditEligibilityOptions> options)
    { _db = db; _options = options.Value; }

    private sealed record Geography(int? StateId, int? RegionId, int? HeadquartersId,
        string? State, string? Region, string? Headquarters);
    private sealed record Scope(string Role, HashSet<int> StateIds, HashSet<int> RegionIds,
        HashSet<int> HeadquartersIds, string UserId);
    private sealed record Candidate(int CSR2Id, int ProgramId, string ProgramName,
        string SanctionNumber, int? HeadquarterId, int? LocationId, string? ResponsiblePersonId);

    private static string Clean(string? value) => (value ?? "").Trim();
    private static string Normalize(string? value) => new string(Clean(value).Where(char.IsLetterOrDigit)
        .Select(char.ToUpperInvariant).ToArray());
    private static string DealerKey(string? code)
    {
        var value = Clean(code).ToUpperInvariant();
        return value.Length > 1 && value[0] is >= 'A' and <= 'Z' ? value[1..] : value;
    }

    // Never trust StateId/RegionId/HQ values supplied as query parameters.
    // Role comes from the server-validated auth identity; geography comes from the employee row.
    private async Task<Scope?> GetScopeAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var employee = await _db.Set<EmployeeRegistration.Employeelogin>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct);
        if (employee is null) return null;
        var roleNames = User.FindAll(ClaimTypes.Role).Concat(User.FindAll("role"))
            .Concat(User.FindAll("roles")).Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Explicitly scoped monitoring roles. Add further roles only after business approval.
        var role = roleNames.Contains("SMM") ? "SMM" :
                   roleNames.Contains("RM") || roleNames.Contains("RMM") ? "RM" :
                   roleNames.Contains("SMDO") ? "SMDO" :
                   roleNames.Contains("SMD") ? "SMM" : "";
        if (role.Length == 0) return null;

        // Supports current scalar columns plus optional same-table integer[] assignments.
        // No new assignment table; absent/missing scope always denies access.
        var state = ReadIds(employee, "StateId", "StateIds");
        var region = ReadIds(employee, "RegionId", "RegionIds");
        var hq = ReadIds(employee, "HeadquartersId", "HeadquarterId", "HQId", "HeadquartersIds", "HeadquarterIds");
        if (role == "SMM" && state.Count == 0) return null;
        if (role == "RM" && region.Count == 0) return null;
        if (role == "SMDO" && state.Count == 0 && hq.Count == 0) return null;
        return new Scope(role, state, region, hq, userId);
    }

    private static HashSet<int> ReadIds(object entity, params string[] names)
    {
        var result = new HashSet<int>();
        foreach (var name in names)
        {
            var prop = entity.GetType().GetProperties().FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (prop is null) continue;
            var raw = prop.GetValue(entity);
            if (raw is int value && value > 0) result.Add(value);
            if (raw is IEnumerable<int> array)
                foreach (var id in array.Where(x => x > 0)) result.Add(id);
        }
        return result;
    }

    // Uses only one unambiguous EF-mapped master entity per level.
    // Do not derive State/Region IDs from a name or silently treat LocationId as StateId.
    private async Task<object?> FindMasterAsync(string level, int? id, CancellationToken ct)
    {
        if (id is null or <= 0) return null;
        string[] names = level switch
        {
            "HQ" => new[] { "Headquarter", "Headquarters", "HeadQuarter", "HQ", "HeadquarterMaster", "HeadQuarterMaster", "HQMaster", "HeadquarterInformation" },
            "Region" => new[] { "Region", "Regions", "RegionMaster", "RegionInformation" },
            "State" => new[] { "State", "States", "StateMaster", "StateInformation" },
            _ => new[] { "Location", "Locations", "LocationMaster", "LocationInformation" }
        };
        var matches = _db.Model.GetEntityTypes().Where(x => names.Contains(x.ClrType.Name,
            StringComparer.OrdinalIgnoreCase) && x.FindPrimaryKey()?.Properties.Count == 1).ToList();
        if (matches.Count != 1) return null;
        var pk = matches[0].FindPrimaryKey()!.Properties[0];
        object key;
        try { key = Convert.ChangeType(id.Value, Nullable.GetUnderlyingType(pk.ClrType) ?? pk.ClrType); }
        catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException) { return null; }
        return await _db.FindAsync(matches[0].ClrType, new object?[] { key }, ct);
    }
    private static int? RefId(object? entity, params string[] names)
    {
        if (entity is null) return null;
        foreach (var name in names)
        {
            var raw = entity.GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(name,
                StringComparison.OrdinalIgnoreCase))?.GetValue(entity);
            if (raw is null) continue;
            try { var value = Convert.ToInt32(raw, CultureInfo.InvariantCulture); if (value > 0) return value; }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException) { }
        }
        return null;
    }
    private static string? MasterName(object? entity, params string[] names)
    {
        if (entity is null) return null;
        foreach (var name in names)
        {
            var raw = entity.GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(name,
                StringComparison.OrdinalIgnoreCase))?.GetValue(entity) as string;
            if (!string.IsNullOrWhiteSpace(raw)) return raw.Trim();
        }
        return null;
    }
    private async Task<Geography> ResolveAsync(Candidate candidate, CancellationToken ct)
    {
        var hq = await FindMasterAsync("HQ", candidate.HeadquarterId, ct);
        var location = await FindMasterAsync("Location", candidate.LocationId, ct);
        var regionId = RefId(hq, "RegionId") ?? RefId(location, "RegionId");
        var region = await FindMasterAsync("Region", regionId, ct);
        var stateId = RefId(region, "StateId") ?? RefId(hq, "StateId") ?? RefId(location, "StateId");
        var state = await FindMasterAsync("State", stateId, ct);
        return new Geography(stateId, regionId, candidate.HeadquarterId,
            MasterName(state, "Name", "StateName"), MasterName(region, "Name", "RegionName"),
            MasterName(hq, "Name", "HQName", "HeadquarterName", "HeadQuarterName"));
    }
    private static bool Authorized(Scope scope, Geography geo) => scope.Role switch
    {
        "SMM" => geo.StateId.HasValue && scope.StateIds.Contains(geo.StateId.Value),
        "RM" => geo.RegionId.HasValue && scope.RegionIds.Contains(geo.RegionId.Value),
        // State SMDO can monitor its state; an HQ-specific assignment narrows to HQ.
        "SMDO" => scope.HeadquartersIds.Count > 0
            ? geo.HeadquartersId.HasValue && scope.HeadquartersIds.Contains(geo.HeadquartersId.Value)
            : geo.StateId.HasValue && scope.StateIds.Contains(geo.StateId.Value),
        _ => false
    };

    private async Task<List<(Candidate Candidate, Geography Geo)>> AccessibleAsync(Scope scope, CancellationToken ct)
    {
        var eligibleIds = (_options.EligibleProgramIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToArray();
        var query = from c2 in _db.Set<CSR2>().AsNoTracking()
                    join c1 in _db.Set<CSR1>().AsNoTracking() on c2.CSR1Id equals c1.Id
                    join pm in _db.Set<ProgramMaster>().AsNoTracking() on c1.ProgramId equals pm.Id
                    where c2.Status == CSR2Status.Approved && c1.Status == CSR1Status.Approved && c1.IsActive
                    select new Candidate(c2.Id, c1.ProgramId, pm.Name, c1.SanctionNo ?? "",
                        c1.HeadquarterId, c1.LocationId, c1.ResponsiblePersonId);
        if (eligibleIds.Length > 0) query = query.Where(x => eligibleIds.Contains(x.ProgramId));
        var candidates = await query.ToListAsync(ct);
        var result = new List<(Candidate, Geography)>();
        foreach (var candidate in candidates)
        {
            var geo = await ResolveAsync(candidate, ct);
            if (Authorized(scope, geo)) result.Add((candidate, geo));
        }
        return result;
    }

    [HttpGet("accessible")]
    public async Task<IActionResult> Accessible()
    {
        var ct = HttpContext.RequestAborted;
        var scope = await GetScopeAsync(ct);
        if (scope is null) return Forbid();
        var programs = await AccessibleAsync(scope, ct);
        var ids = programs.Select(x => x.Candidate.CSR2Id).ToArray();
        var records = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .Where(x => ids.Contains(x.CSR2Id))
            .Select(x => new {
                x.CSR2Id,
                x.SchemeFrom,
                x.SchemeTo,
                x.DocumentStatus,
                x.RequiredTurnover,
                x.ActualTurnover
            })
            .ToListAsync(ct);
        var byId = records.ToDictionary(x => x.CSR2Id);
        return Ok(new
        {
            Role = scope.Role,
            Programs = programs.OrderByDescending(x => x.Candidate.CSR2Id).Select(x =>
            {
                byId.TryGetValue(x.Candidate.CSR2Id, out var audit);
                return new
                {
                    x.Candidate.CSR2Id,
                    x.Candidate.ProgramName,
                    x.Candidate.SanctionNumber,
                    State = x.Geo.State,
                    Region = x.Geo.Region,
                    Headquarters = x.Geo.Headquarters,
                    x.Geo.StateId,
                    x.Geo.RegionId,
                    x.Geo.HeadquartersId,
                    SchemeFrom = audit?.SchemeFrom,
                    SchemeTo = audit?.SchemeTo,
                    DocumentStatus = audit?.DocumentStatus ?? "Not Started",
                    RequiredTurnoverRs = (audit?.RequiredTurnover ?? 0m) * Crore,
                    ActualTurnoverRs = (audit?.ActualTurnover ?? 0m) * Crore
                };
            }).ToList()
        });
    }

    private sealed record Invoice(long SaleId, DateTime BillingDate, string DealerCode,
        string BillingDocument, decimal GrossRs);
    private static string GetField(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var direct)) return direct.ToString();
        var key = Normalize(name);
        foreach (var field in obj.EnumerateObject())
            if (Normalize(field.Name) == key) return field.Value.ToString();
        return "";
    }
    private static bool Number(string raw, out decimal value) => decimal.TryParse(
        Clean(raw).Replace(",", "").Replace("₹", ""), NumberStyles.Number |
        NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    private static bool Date(string raw, out DateTime date) => DateTime.TryParseExact(Clean(raw),
        new[] { "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "d/M/yyyy", "d-M-yyyy", "yyyy-MM-ddTHH:mm:ss" },
        CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int csr2Id)
    {
        if (csr2Id <= 0) return BadRequest(new { message = "Valid csr2Id required." });
        var ct = HttpContext.RequestAborted;
        var scope = await GetScopeAsync(ct);
        if (scope is null) return Forbid();
        // Server-side verification on EVERY detail request; client filters are never authorization.
        var allowed = (await AccessibleAsync(scope, ct)).FirstOrDefault(x => x.Candidate.CSR2Id == csr2Id);
        if (allowed.Candidate is null) return Forbid();
        var audit = await _db.Set<SalesAuditRecord>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.CSR2Id == csr2Id, ct);
        var plans = await (from p in _db.Set<SalesAuditPlanLine>().AsNoTracking()
                           join product in _db.Set<Product>().AsNoTracking() on p.ProductId equals product.Id
                           where p.CSR2Id == csr2Id
                           select new { p.ProductId, Name = product.Name, p.QuantityMT, p.ValueRs }).ToListAsync(ct);
        var achieved = await _db.Set<SalesAuditAchievementLine>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id)
            .Select(x => new { x.ProductId, x.QuantityMT, x.ValueRs }).ToListAsync(ct);
        var productTotals = achieved.GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => new { Qty = x.Sum(y => y.QuantityMT), Value = x.Sum(y => y.ValueRs) });
        var productRows = plans.Select(x => new
        {
            x.ProductId,
            x.Name,
            PlannedQtyMT = x.QuantityMT,
            PlannedRs = x.ValueRs,
            ActualQtyMT = productTotals.TryGetValue(x.ProductId, out var v) ? v.Qty : 0m,
            ActualRs = productTotals.TryGetValue(x.ProductId, out var a) ? a.Value : 0m
        }).ToList();
        var mappedDealers = await (from m in _db.Set<SalesAuditDealerMapping>().AsNoTracking()
                                   join d in _db.Set<DealerRegistration>().AsNoTracking()
                                       on m.DealerRegistrationId equals d.Id
                                   where m.CSR2Id == csr2Id
                                   select new { d.DealerCode, d.FirmName }).ToListAsync(ct);
        var dealersByCode = mappedDealers.Where(x => !string.IsNullOrWhiteSpace(x.DealerCode))
            .GroupBy(x => Clean(x.DealerCode).ToUpperInvariant())
            .ToDictionary(x => x.Key, x => x.First().FirmName);
        var sourceIds = await _db.Set<SalesAuditMatchedInvoice>().AsNoTracking()
            .Where(x => x.CSR2Id == csr2Id).Select(x => x.SourceSaleId).ToArrayAsync(ct);
        var invoices = new List<Invoice>();
        if (sourceIds.Length > 0)
        {
            // Read ONLY linked/source-approved invoices, never all dealer SAP rows.
            var connection = _db.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await connection.OpenAsync(ct);
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT \"Id\", \"SourceRowJson\"::text FROM \"SalesAuditDealerSales\" WHERE \"Id\" = ANY(@saleIds) ORDER BY \"Id\"";
                var param = command.CreateParameter();
                param.ParameterName = "@saleIds";
                param.Value = sourceIds;
                command.Parameters.Add(param);
                using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    using var json = JsonDocument.Parse(reader.GetString(1));
                    var obj = json.RootElement;
                    if (!Date(GetField(obj, "Billing Date"), out var billed) ||
                        !Number(GetField(obj, "Gross Amount"), out var gross)) continue;
                    invoices.Add(new Invoice(reader.GetInt64(0), billed.Date,
                        DealerKey(GetField(obj, "Customer Number")), GetField(obj, "Billing Document"), gross));
                }
            }
            finally { if (opened) await connection.CloseAsync(); }
        }
        var byDealer = invoices.GroupBy(x => x.DealerCode)
            .Select(x => new {
                DealerCode = x.Key,
                DealerName = dealersByCode.TryGetValue(x.Key, out var name) ? name : x.Key,
                InvoiceLines = x.Count(),
                ActualRs = x.Sum(y => y.GrossRs)
            })
            .OrderByDescending(x => x.ActualRs).ToList();
        var trend = invoices.GroupBy(x => new { x.BillingDate.Year, x.BillingDate.Month })
            .OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
            .Select(x => new { Month = $"{x.Key.Year}-{x.Key.Month:00}", InvoiceLines = x.Count(), ActualRs = x.Sum(y => y.GrossRs) })
            .ToList();
        var plannedRs = plans.Sum(x => x.ValueRs);
        var actualRs = (audit?.ActualTurnover ?? 0m) * Crore;
        var requiredRs = (audit?.RequiredTurnover ?? 0m) * Crore;
        var linkedGrossRs = invoices.Sum(x => x.GrossRs);
        return Ok(new
        {
            csr2Id,
            allowed.Candidate.ProgramName,
            allowed.Candidate.SanctionNumber,
            Role = scope.Role,
            State = allowed.Geo.State,
            Region = allowed.Geo.Region,
            Headquarters = allowed.Geo.Headquarters,
            SchemeFrom = audit?.SchemeFrom,
            SchemeTo = audit?.SchemeTo,
            DocumentStatus = audit?.DocumentStatus ?? "Not Started",
            IsPlanConfirmed = audit?.DocumentStatus is "Plan Confirmed" or "In Progress" or "Completed",
            RequiredTurnoverRs = requiredRs,
            PlannedTurnoverRs = plannedRs,
            ActualTurnoverRs = actualRs,
            RemainingRs = Math.Max(0m, requiredRs - actualRs),
            ProgressPercent = requiredRs > 0m ? actualRs / requiredRs * 100m : 0m,
            ProductSales = productRows,
            MappedDealerCount = mappedDealers.Count,
            DealerSales = byDealer,
            MonthlyTrend = trend,
            SavedInvoiceLines = sourceIds.Length,
            ReadableInvoiceLines = invoices.Count,
            InvoiceTotalRs = linkedGrossRs,
            // A mismatch must be visible; do not silently equate the aggregated value with source links.
            NeedsReconciliation = sourceIds.Length != invoices.Count || linkedGrossRs != actualRs
        });
    }
}
