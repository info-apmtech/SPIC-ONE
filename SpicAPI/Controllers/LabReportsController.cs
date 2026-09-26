using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.LabReports;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// SAS Lab portal, reports (docs/sas-lab-portal-plan.md; contract in SPIC.Core/DTOs/LabDtos.cs):
    ///   GET  api/Lab/reports/stats?financialYear=
    ///   GET  api/Lab/reports/batches?stateId=&amp;regionId=&amp;hqId=&amp;sampleType=&amp;financialYear=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize=
    ///   GET  api/Lab/batches/{id}/report
    ///   GET  api/Lab/batches/{id}/report/download?lang=&amp;format=pdf|zip
    ///   GET  api/Lab/reports?batchId=&amp;sampleType=&amp;status=&amp;crop=&amp;village=&amp;financialYear=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize=
    ///   GET  api/Lab/reports/{id}
    ///   GET  api/Lab/reports/{id}/pdf?lang=
    ///   GET  api/Lab/reports/{id}/xlsx?lang=
    ///   POST api/Lab/reports/{id}/printed
    /// The pdf / xlsx / download routes also accept ?access_token= (allowlist in Program.cs).
    ///
    /// Access (designation RoleAccess, same parsing as LibraryController):
    ///   Admin / CorporateAdmin, or LabTracking / LabConsignments / LabAnalysis : every report.
    ///   LabTestEntry (analyst; also holds LabDashboard / LabReports)            : batches assigned to them.
    ///   LabDashboard / LabReports without LabTestEntry (coordinator)            : every report.
    ///   Farmer : only reports of samples carrying their own farmer record (v1 scoping: SasFarmer.UserId,
    ///            or the mobile number = the account's user name / phone), farmer languages only,
    ///            no batch routes.
    ///   Anyone else signed in (Dealer, field staff): the v1 read rule (every collection), read-only.
    /// Marking Printed needs a lab page or admin. sampleType= takes Soil / Water / SoilAndWater (or
    /// 0 / 1 / 2) and Paid / Free; status= takes Generated / Downloaded / Printed (or 0 / 1 / 2).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/Lab")]
    public class LabReportsController : ControllerBase
    {
        private const int DefaultPageSize = 16;
        private const int MaxPageSize = 50;
        private const string FallbackHeader = "X-Report-Language-Fallback";

        private static readonly string[] DefaultLanguages = { "en", "ta", "te", "mr" };
        private static readonly string[] DefaultFarmerLanguages = { "en", "ta" };

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly LabReportFiles _files;
        private readonly LabReportReader _reader;
        private readonly ILogger<LabReportsController> _logger;

        public LabReportsController(AppDbContext db, IConfiguration config, LabReportFiles files, LabReportReader reader,
            ILogger<LabReportsController> logger)
        {
            _db = db;
            _config = config;
            _files = files;
            _reader = reader;
            _logger = logger;
        }

        // ================================================================ stats

        // GET api/Lab/reports/stats?financialYear=
        [HttpGet("reports/stats")]
        public async Task<IActionResult> GetStats([FromQuery] int? financialYear)
        {
            var access = await ResolveAccessAsync();
            var reports = Reports(access);
            var currentFy = LabReportRules.FinancialYearStart(DateTime.Now);
            var inFy = financialYear.HasValue ? reports.Where(r => r.FinancialYearStart == financialYear.Value) : reports;

            int batchGroups;
            if (access.Scope == LabScope.Farmer)
            {
                batchGroups = await inFy.Select(r => r.BatchId).Distinct().CountAsync();
            }
            else
            {
                // "Batch-wise report groups": batches in scope that have at least one report.
                var batches = Batches(access).Where(b => b.Reports.Any());
                if (financialYear.HasValue)
                {
                    var (from, to) = LabReportRules.FinancialYearRange(financialYear.Value);
                    batches = batches.Where(b => b.BatchDate >= from && b.BatchDate < to);
                }
                batchGroups = await batches.CountAsync();
            }

            var today = DateTime.Today;
            return Ok(new LabReportStatsDto
            {
                TotalReports = await reports.CountAsync(),
                ThisFinancialYear = await reports.CountAsync(r => r.FinancialYearStart == currentFy),
                PreviousFinancialYears = await reports.CountAsync(r => r.FinancialYearStart < currentFy),
                BatchGroups = batchGroups,
                SampleReports = await inFy.CountAsync(),
                GeneratedToday = await inFy.CountAsync(r => r.GeneratedAt >= today),
                DownloadsCompleted = await inFy.SumAsync(r => (int?)r.DownloadCount) ?? 0
            });
        }

        // ================================================================ batch-wise summary

        // GET api/Lab/reports/batches
        [HttpGet("reports/batches")]
        public async Task<IActionResult> GetReportBatches(
            [FromQuery] int? stateId, [FromQuery] int? regionId, [FromQuery] int? hqId, [FromQuery] string? sampleType,
            [FromQuery] int? financialYear, [FromQuery] string? q, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            var access = await ResolveAccessAsync();
            if (access.Scope == LabScope.Farmer) return Forbid403("Farmers see their reports under My Reports.");

            var batches = Batches(access);
            if (financialYear.HasValue)
            {
                var (fyFrom, fyTo) = LabReportRules.FinancialYearRange(financialYear.Value);
                batches = batches.Where(b => b.BatchDate >= fyFrom && b.BatchDate < fyTo);
            }
            if (from.HasValue) batches = batches.Where(b => b.BatchDate >= from.Value.Date);
            if (to.HasValue) batches = batches.Where(b => b.BatchDate < to.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = $"%{q.Trim()}%";
                batches = batches.Where(b =>
                    EF.Functions.ILike(b.Code, term) ||
                    (b.ReportCode != null && EF.Functions.ILike(b.ReportCode, term)) ||
                    b.Consignments.Any(c => EF.Functions.ILike(c.Code, term)) ||
                    b.Reports.Any(r => EF.Functions.ILike(r.Code, term)));
            }

            var list = await batches.OrderByDescending(b => b.BatchDate).ThenByDescending(b => b.Id).ToListAsync();
            var rows = await BuildReportBatchRowsAsync(list);

            if (stateId.HasValue) rows = rows.Where(r => r.Location.StateIds.Contains(stateId.Value)).ToList();
            if (regionId.HasValue) rows = rows.Where(r => r.Location.RegionIds.Contains(regionId.Value)).ToList();
            if (hqId.HasValue) rows = rows.Where(r => r.Location.HqIds.Contains(hqId.Value)).ToList();
            if (!string.IsNullOrWhiteSpace(sampleType))
            {
                var filter = ParseSampleType(sampleType);
                if (filter.Invalid) return BadRequest(new { Success = false, Message = $"Unknown sampleType '{sampleType}'." });
                if (filter.Payment.HasValue) rows = rows.Where(r => r.Row.SampleType == filter.Payment.Value).ToList();
                if (filter.Type == SampleType.Soil) rows = rows.Where(r => r.Row.SoilSamples > 0).ToList();
                if (filter.Type == SampleType.Water) rows = rows.Where(r => r.Row.WaterSamples > 0).ToList();
                if (filter.Type == SampleType.SoilAndWater) rows = rows.Where(r => r.Row.SoilSamples > 0 && r.Row.WaterSamples > 0).ToList();
            }

            var (p, size) = Paging(page, pageSize);
            return Ok(new PageResult<LabReportBatchRowDto>
            {
                Items = rows.Skip((p - 1) * size).Take(size).Select(r => r.Row).ToList(),
                Total = rows.Count,
                Page = p,
                PageSize = size
            });
        }

        // GET api/Lab/batches/{id}/report
        [HttpGet("batches/{id:int}/report")]
        public async Task<IActionResult> GetBatchReport(int id)
        {
            var access = await ResolveAccessAsync();
            if (access.Scope == LabScope.Farmer) return Forbid403("Farmers see their reports under My Reports.");

            var batch = await _db.SampleBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
            if (batch == null) return NotFound(new { Success = false, Message = "Batch not found." });
            if (!await Batches(access).AnyAsync(b => b.Id == id)) return Forbid403("This batch is not assigned to you.");

            var summary = (await BuildReportBatchRowsAsync(new List<SampleBatch> { batch })).First().Row;
            var batchRow = (await BuildBatchRowsAsync(new List<SampleBatch> { batch })).First();
            var consignmentDates = await _db.SampleConsignments.AsNoTracking()
                .Where(c => c.BatchId == id && !c.IsDeleted)
                .Select(c => c.DispatchedAt)
                .ToListAsync();

            var sampleReports = await RowsAsync(Reports(access)
                .Where(r => r.BatchId == id)
                .OrderBy(r => r.Code));

            return Ok(new LabBatchReportDto
            {
                Summary = summary,
                Batch = batchRow,
                BatchCreatedByName = batch.CreatedByName,
                BatchCreatedAt = batch.CreatedAt,
                ShipmentDate = consignmentDates.Count == 0 ? batch.BatchDate : consignmentDates.Min(),
                AssignedAt = batch.AssignedAt,
                AnalysisCompletedAt = batch.AnalysisCompletedAt,
                AnalysisDays = batchRow.AnalysisDays,
                FinalCompletedAt = batch.CompletedAt,
                FinalRemarks = batch.FinalRemarks,
                Timeline = await TimelineAsync(batch),
                SampleReports = sampleReports
            });
        }

        // GET api/Lab/batches/{id}/report/download?lang=&format=pdf|zip
        [HttpGet("batches/{id:int}/report/download")]
        public async Task<IActionResult> DownloadBatch(int id, [FromQuery] string? lang, [FromQuery] string? format, CancellationToken ct)
        {
            var access = await ResolveAccessAsync();
            if (access.Scope == LabScope.Farmer) return Forbid403("Farmers download their own reports one by one.");
            if (!TryLanguage(access, lang, out var language, out var error)) return error!;

            var fmt = string.IsNullOrWhiteSpace(format) ? "pdf" : format.Trim().ToLowerInvariant();
            if (fmt != "pdf" && fmt != "zip") return BadRequest(new { Success = false, Message = "format must be pdf or zip." });

            var batch = await _db.SampleBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct);
            if (batch == null) return NotFound(new { Success = false, Message = "Batch not found." });
            if (!await Batches(access).AnyAsync(b => b.Id == id, ct)) return Forbid403("This batch is not assigned to you.");

            var ids = await Reports(access).Where(r => r.BatchId == id).OrderBy(r => r.Code).Select(r => r.Id).ToListAsync(ct);
            if (ids.Count == 0) return NotFound(new { Success = false, Message = "No reports have been generated for this batch yet." });

            var file = await _files.BatchAsync(ids, batch.Code, language, fmt == "zip", ct);
            if (file == null) return NotFound(new { Success = false, Message = "No reports have been generated for this batch yet." });

            await CountDownloadsAsync(ids);
            return FileResult(file);
        }

        // ================================================================ sample-wise reports

        // GET api/Lab/reports
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports(
            [FromQuery] int? batchId, [FromQuery] string? sampleType, [FromQuery] string? status, [FromQuery] string? crop,
            [FromQuery] string? village, [FromQuery] int? financialYear, [FromQuery] string? q,
            [FromQuery] DateTime? from, [FromQuery] DateTime? to,
            [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            var access = await ResolveAccessAsync();
            var query = Reports(access);

            if (batchId.HasValue) query = query.Where(r => r.BatchId == batchId.Value);
            if (financialYear.HasValue) query = query.Where(r => r.FinancialYearStart == financialYear.Value);
            if (from.HasValue) query = query.Where(r => r.GeneratedAt >= from.Value.Date);
            if (to.HasValue) query = query.Where(r => r.GeneratedAt < to.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(sampleType))
            {
                var filter = ParseSampleType(sampleType);
                if (filter.Invalid) return BadRequest(new { Success = false, Message = $"Unknown sampleType '{sampleType}'." });
                if (filter.Payment.HasValue) query = query.Where(r => r.SampleItem!.Collection!.PaymentType == filter.Payment.Value);
                if (filter.Type == SampleType.SoilAndWater) query = query.Where(r => r.SampleItem!.SampleType == SampleType.SoilAndWater);
                else if (filter.Type.HasValue) query = query.Where(r => r.SampleType == filter.Type.Value);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!TryParseEnum<LabReportStatus>(status, out var st))
                    return BadRequest(new { Success = false, Message = $"Unknown status '{status}'." });
                query = query.Where(r => r.Status == st);
            }
            if (!string.IsNullOrWhiteSpace(crop))
            {
                var c = crop.Trim();
                query = query.Where(r => (r.SampleItem!.Crop1 != null && EF.Functions.ILike(r.SampleItem.Crop1, c)) ||
                                         (r.SampleItem!.Crop2 != null && EF.Functions.ILike(r.SampleItem.Crop2, c)));
            }
            if (!string.IsNullOrWhiteSpace(village))
            {
                var v = $"%{village.Trim()}%";
                query = query.Where(r => r.SampleItem!.Farmer!.Village != null && EF.Functions.ILike(r.SampleItem.Farmer.Village, v));
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = $"%{q.Trim()}%";
                query = query.Where(r =>
                    EF.Functions.ILike(r.Code, term) ||
                    EF.Functions.ILike(r.Batch!.Code, term) ||
                    EF.Functions.ILike(r.SampleItem!.Code, term) ||
                    EF.Functions.ILike(r.SampleItem!.Farmer!.Name, term) ||
                    EF.Functions.ILike(r.SampleItem!.Farmer!.Mobile, term) ||
                    (r.SampleItem!.Crop1 != null && EF.Functions.ILike(r.SampleItem.Crop1, term)) ||
                    (r.SampleItem!.Farmer!.Village != null && EF.Functions.ILike(r.SampleItem.Farmer.Village, term)));
            }

            var (p, size) = Paging(page, pageSize);
            var total = await query.CountAsync();
            var items = await RowsAsync(query
                .OrderByDescending(r => r.GeneratedAt).ThenByDescending(r => r.Id)
                .Skip((p - 1) * size).Take(size));

            return Ok(new PageResult<LabReportRowDto> { Items = items, Total = total, Page = p, PageSize = size });
        }

        // GET api/Lab/reports/{id}
        [HttpGet("reports/{id:int}")]
        public async Task<IActionResult> GetReport(int id, CancellationToken ct)
        {
            var access = await ResolveAccessAsync();
            var denied = await CheckReportAsync(access, id);
            if (denied != null) return denied;

            var summary = (await RowsAsync(_db.LabReports.AsNoTracking().Where(r => r.Id == id))).First();
            var batch = await _db.SampleBatches.AsNoTracking().FirstAsync(b => b.Id == summary.BatchId, ct);
            var model = await _reader.LoadAsync(id, ct);
            if (model == null) return NotFound(new { Success = false, Message = "Report not found." });

            var consignments = await _db.SampleConsignments.AsNoTracking()
                .Where(c => c.BatchId == batch.Id && !c.IsDeleted)
                .Select(c => c.DispatchedAt)
                .ToListAsync(ct);
            var hqId = await _db.SampleItems.AsNoTracking()
                .Where(i => i.Id == summary.SampleItemId)
                .Select(i => i.Collection!.HeadquarterId)
                .FirstOrDefaultAsync(ct);
            var loc = hqId.HasValue
                ? (await LabReportReader.LocationNamesAsync(_db, new[] { hqId.Value }, ct)).GetValueOrDefault(hqId.Value)
                : default;

            return Ok(new LabReportDetailDto
            {
                Summary = summary,
                BatchCreatedAt = batch.CreatedAt,
                BatchCreatedByName = batch.CreatedByName,
                BatchStatus = batch.Status,
                ShipmentDate = consignments.Count == 0 ? batch.BatchDate : consignments.Min(),
                RegionName = loc.Region,
                HeadquarterName = loc.Hq,
                AssignedAt = batch.AssignedAt,
                AnalysisCompletedAt = batch.AnalysisCompletedAt,
                AnalysisDays = AnalysisDays(batch),
                FinalCompletedAt = batch.CompletedAt,
                FinalRemarks = batch.FinalRemarks,
                Timeline = await TimelineAsync(batch),
                Sample = LabReportReader.ToSampleReportDto(model, batch.Status == SampleBatchStatus.Completed),
                Languages = access.Languages.ToList()
            });
        }

        // GET api/Lab/reports/{id}/pdf?lang=
        [HttpGet("reports/{id:int}/pdf")]
        public async Task<IActionResult> GetPdf(int id, [FromQuery] string? lang, CancellationToken ct)
        {
            var access = await ResolveAccessAsync();
            var denied = await CheckReportAsync(access, id);
            if (denied != null) return denied;
            if (!TryLanguage(access, lang, out var language, out var error)) return error!;

            var file = await _files.PdfAsync(id, language, ct);
            if (file == null) return NotFound(new { Success = false, Message = "Report not found." });
            await CountDownloadsAsync(new[] { id });
            return FileResult(file);
        }

        // GET api/Lab/reports/{id}/xlsx?lang=
        [HttpGet("reports/{id:int}/xlsx")]
        public async Task<IActionResult> GetXlsx(int id, [FromQuery] string? lang, CancellationToken ct)
        {
            var access = await ResolveAccessAsync();
            var denied = await CheckReportAsync(access, id);
            if (denied != null) return denied;
            if (!TryLanguage(access, lang, out var language, out var error)) return error!;

            var file = await _files.XlsxAsync(id, language, ct);
            if (file == null) return NotFound(new { Success = false, Message = "Report not found." });
            await CountDownloadsAsync(new[] { id });
            return FileResult(file);
        }

        // POST api/Lab/reports/{id}/printed
        [HttpPost("reports/{id:int}/printed")]
        public async Task<IActionResult> MarkPrinted(int id)
        {
            var access = await ResolveAccessAsync();
            var denied = await CheckReportAsync(access, id);
            if (denied != null) return denied;
            if (!access.CanMarkPrinted) return Forbid403("Only the lab can mark a report as printed.");

            var now = DateTime.Now;
            await _db.LabReports.Where(r => r.Id == id).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.PrintedAt, now)
                .SetProperty(r => r.Status, LabReportStatus.Printed));

            return Ok((await RowsAsync(_db.LabReports.AsNoTracking().Where(r => r.Id == id))).First());
        }

        // ================================================================ access

        private enum LabScope { All, Own, Farmer, ReadOnly }

        private sealed class LabAccess
        {
            public LabScope Scope { get; init; }
            public string UserId { get; init; } = "";
            public bool CanMarkPrinted { get; init; }
            public List<int> FarmerIds { get; init; } = new();
            public string[] Languages { get; init; } = Array.Empty<string>();
        }

        private LabAccess? _access;

        private async Task<LabAccess> ResolveAccessAsync()
        {
            if (_access != null) return _access;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var roleRaw = User.FindFirst(ClaimTypes.Role)?.Value;
            Enum.TryParse<AppRole>(roleRaw, true, out var role);
            var hasRole = !string.IsNullOrWhiteSpace(roleRaw) && Enum.TryParse<AppRole>(roleRaw, true, out _);
            var languages = Languages("Sas:Lab:ReportLanguages", DefaultLanguages);

            if (hasRole && (role == AppRole.Admin || role == AppRole.CorporateAdmin))
                return _access = new LabAccess { Scope = LabScope.All, UserId = userId, CanMarkPrinted = true, Languages = languages };

            if (hasRole && role == AppRole.Farmer)
                return _access = new LabAccess
                {
                    Scope = LabScope.Farmer,
                    UserId = userId,
                    FarmerIds = await MyFarmerIdsAsync(userId),
                    Languages = Languages("Sas:Lab:FarmerReportLanguages", DefaultFarmerLanguages)
                };

            var roleAccess = await RoleAccessAsync(userId);
            bool Has(PagePermission page) => RoleAccessPermissions.HasPage(roleAccess, page);

            if (Has(PagePermission.LabTracking) || Has(PagePermission.LabConsignments) || Has(PagePermission.LabAnalysis))
                return _access = new LabAccess { Scope = LabScope.All, UserId = userId, CanMarkPrinted = true, Languages = languages };
            if (Has(PagePermission.LabTestEntry))
                return _access = new LabAccess { Scope = LabScope.Own, UserId = userId, CanMarkPrinted = true, Languages = languages };
            if (Has(PagePermission.LabDashboard) || Has(PagePermission.LabReports))
                return _access = new LabAccess { Scope = LabScope.All, UserId = userId, CanMarkPrinted = true, Languages = languages };

            // v1 read rule (SasController): every signed-in user other than a Farmer reads every collection.
            return _access = new LabAccess { Scope = LabScope.ReadOnly, UserId = userId, Languages = languages };
        }

        private async Task<string?> RoleAccessAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var designationId = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.DesignationId)
                .FirstOrDefaultAsync();
            if (designationId is not > 0) return null;
            return await _db.Designations.AsNoTracking()
                .Where(d => d.Id == designationId && d.IsActive)
                .Select(d => d.RoleAccess)
                .FirstOrDefaultAsync();
        }

        /// <summary>Same matching as SasController.MyFarmerIdsAsync (v1 farmer scoping).</summary>
        private async Task<List<int>> MyFarmerIdsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<int>();
            var account = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.UserName, u.PhoneNumber })
                .FirstOrDefaultAsync();
            var userName = account?.UserName ?? "";
            var phone = account?.PhoneNumber ?? "";
            return await _db.SasFarmers.AsNoTracking()
                .Where(f => !f.IsDeleted && (
                    f.UserId == userId ||
                    (userName != "" && f.Mobile == userName) ||
                    (phone != "" && f.Mobile == phone)))
                .Select(f => f.Id)
                .ToListAsync();
        }

        private string[] Languages(string key, string[] fallback)
        {
            var configured = _config.GetSection(key).Get<string[]>();
            var list = (configured == null || configured.Length == 0 ? fallback : configured)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            if (!list.Contains("en")) list.Insert(0, "en");
            return list.ToArray();
        }

        private bool TryLanguage(LabAccess access, string? lang, out string language, out IActionResult? error)
        {
            language = string.IsNullOrWhiteSpace(lang) ? "en" : lang.Trim().ToLowerInvariant();
            error = null;
            if (access.Languages.Contains(language)) return true;
            error = BadRequest(new { Success = false, Message = $"Language '{language}' is not available. Available: {string.Join(", ", access.Languages)}." });
            return false;
        }

        private IQueryable<LabReport> Reports(LabAccess access)
        {
            var query = _db.LabReports.AsNoTracking()
                .Where(r => !r.Batch!.IsDeleted && !r.SampleItem!.IsDeleted && !r.SampleItem.Collection!.IsDeleted);
            return access.Scope switch
            {
                LabScope.Own => query.Where(r => r.Batch!.AssignedToUserId == access.UserId),
                LabScope.Farmer => access.FarmerIds.Count == 0
                    ? query.Where(r => false)
                    : query.Where(r => access.FarmerIds.Contains(r.SampleItem!.FarmerId)),
                _ => query
            };
        }

        private IQueryable<SampleBatch> Batches(LabAccess access)
        {
            var query = _db.SampleBatches.AsNoTracking().Where(b => !b.IsDeleted);
            return access.Scope switch
            {
                LabScope.Own => query.Where(b => b.AssignedToUserId == access.UserId),
                LabScope.Farmer => query.Where(b => false),
                _ => query
            };
        }

        /// <summary>404 when the report does not exist, 403 when it exists outside the caller's scope.</summary>
        private async Task<IActionResult?> CheckReportAsync(LabAccess access, int id)
        {
            if (await Reports(access).AnyAsync(r => r.Id == id)) return null;
            if (await _db.LabReports.AnyAsync(r => r.Id == id))
                return Forbid403(access.Scope == LabScope.Farmer ? "This report is not yours." : "This report is not in your batches.");
            return NotFound(new { Success = false, Message = "Report not found." });
        }

        // ================================================================ projections

        private async Task<List<LabReportRowDto>> RowsAsync(IQueryable<LabReport> query)
        {
            var rows = await query.Select(r => new
            {
                r.Id,
                r.Code,
                r.BatchId,
                BatchCode = r.Batch!.Code,
                r.Batch.AssignedToName,
                r.SampleItemId,
                ItemCode = r.SampleItem!.Code,
                FarmerName = r.SampleItem.Farmer != null ? r.SampleItem.Farmer.Name : "",
                FarmerState = r.SampleItem.Farmer != null ? r.SampleItem.Farmer.StateName : null,
                Village = r.SampleItem.Farmer != null ? r.SampleItem.Farmer.Village : null,
                r.SampleItem.Crop1,
                r.SampleType,
                PaymentType = r.SampleItem.Collection!.PaymentType,
                HqId = r.SampleItem.Collection.HeadquarterId,
                r.GeneratedAt,
                r.Status,
                r.DownloadCount,
                r.FinancialYearStart
            }).ToListAsync();

            var batchIds = rows.Select(r => r.BatchId).Distinct().ToList();
            var consignments = await ConsignmentCodesAsync(batchIds);
            var hqIds = rows.Where(r => r.HqId.HasValue).Select(r => r.HqId!.Value).Distinct().ToList();
            var locations = await LabReportReader.LocationNamesAsync(_db, hqIds);
            var sampleIds = await Spic.Infrastructure.Services.Lab.LabSampleIds.ForBatchesAsync(_db, batchIds);

            return rows.Select(r => new LabReportRowDto
            {
                Id = r.Id,
                Code = r.Code,
                BatchId = r.BatchId,
                BatchCode = r.BatchCode,
                ConsignmentCodes = consignments.GetValueOrDefault(r.BatchId) ?? new List<string>(),
                SampleItemId = r.SampleItemId,
                SampleCode = r.ItemCode,
                FarmerName = r.FarmerName,
                Crop = r.Crop1,
                Village = r.Village,
                SampleType = r.SampleType,
                PaymentType = r.PaymentType,
                StateName = r.HqId.HasValue && locations.TryGetValue(r.HqId.Value, out var loc) && loc.State != null ? loc.State : r.FarmerState,
                AssignedToName = r.AssignedToName,
                GeneratedAt = r.GeneratedAt,
                Status = r.Status,
                DownloadCount = r.DownloadCount,
                FinancialYearStart = r.FinancialYearStart,
                SampleId = sampleIds.GetValueOrDefault(r.SampleItemId) ?? ""
            }).ToList();
        }

        private async Task<Dictionary<int, List<string>>> ConsignmentCodesAsync(List<int> batchIds)
        {
            if (batchIds.Count == 0) return new();
            var rows = await _db.SampleConsignments.AsNoTracking()
                .Where(c => c.BatchId != null && batchIds.Contains(c.BatchId.Value) && !c.IsDeleted)
                .OrderBy(c => c.Id)
                .Select(c => new { BatchId = c.BatchId!.Value, c.Code })
                .ToListAsync();
            return rows.GroupBy(r => r.BatchId).ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToList());
        }

        private sealed class BatchLocation
        {
            public HashSet<int> StateIds { get; } = new();
            public HashSet<int> RegionIds { get; } = new();
            public HashSet<int> HqIds { get; } = new();
        }

        private async Task<List<(LabReportBatchRowDto Row, BatchLocation Location)>> BuildReportBatchRowsAsync(List<SampleBatch> batches)
        {
            var ids = batches.Select(b => b.Id).ToList();
            if (ids.Count == 0) return new();

            var consignments = await ConsignmentCodesAsync(ids);
            var items = await _db.SampleItems.AsNoTracking()
                .Where(i => !i.IsDeleted && !i.Collection!.IsDeleted && i.Collection.Consignment != null &&
                            i.Collection.Consignment.BatchId != null && ids.Contains(i.Collection.Consignment.BatchId.Value))
                .Select(i => new
                {
                    BatchId = i.Collection!.Consignment!.BatchId!.Value,
                    i.SampleType,
                    i.Collection.PaymentType,
                    i.Collection.HeadquarterId,
                    FarmerStateId = i.Farmer != null ? i.Farmer.StateId : null,
                    FarmerState = i.Farmer != null ? i.Farmer.StateName : null
                })
                .ToListAsync();
            var reports = await _db.LabReports.AsNoTracking()
                .Where(r => ids.Contains(r.BatchId))
                .GroupBy(r => r.BatchId)
                .Select(g => new { BatchId = g.Key, Count = g.Count(), Downloads = g.Sum(r => r.DownloadCount) })
                .ToListAsync();
            var hqIds = items.Where(i => i.HeadquarterId.HasValue).Select(i => i.HeadquarterId!.Value).Distinct().ToList();
            var locations = await LabReportReader.LocationNamesAsync(_db, hqIds);

            var result = new List<(LabReportBatchRowDto, BatchLocation)>();
            foreach (var b in batches)
            {
                var own = items.Where(i => i.BatchId == b.Id).ToList();
                var soil = own.Count(i => i.SampleType != SampleType.Water);
                var water = own.Count(i => i.SampleType != SampleType.Soil);
                var rep = reports.FirstOrDefault(r => r.BatchId == b.Id);
                var generated = rep?.Count ?? 0;

                var location = new BatchLocation();
                string? state = null, region = null, hq = null;
                foreach (var i in own)
                {
                    if (i.HeadquarterId.HasValue && locations.TryGetValue(i.HeadquarterId.Value, out var loc))
                    {
                        location.HqIds.Add(i.HeadquarterId.Value);
                        if (loc.RegionId.HasValue) location.RegionIds.Add(loc.RegionId.Value);
                        if (loc.StateId.HasValue) location.StateIds.Add(loc.StateId.Value);
                        state ??= loc.State;
                        region ??= loc.Region;
                        hq ??= loc.Hq;
                    }
                    if (i.FarmerStateId.HasValue) location.StateIds.Add(i.FarmerStateId.Value);
                    state ??= i.FarmerState;
                }

                result.Add((new LabReportBatchRowDto
                {
                    BatchId = b.Id,
                    BatchCode = b.Code,
                    ReportCode = b.ReportCode,
                    ConsignmentCodes = consignments.GetValueOrDefault(b.Id) ?? new List<string>(),
                    SampleType = own.Any(i => i.PaymentType == SamplePaymentType.Paid) ? SamplePaymentType.Paid : SamplePaymentType.Free,
                    SoilSamples = soil,
                    WaterSamples = water,
                    TotalSamples = own.Count,
                    ReportsGenerated = generated,
                    PendingReports = Math.Max(0, soil + water - generated),
                    DownloadsCompleted = rep?.Downloads ?? 0,
                    StateName = state,
                    RegionName = region,
                    HeadquarterName = hq,
                    BatchDate = b.BatchDate,
                    ReportGeneratedAt = b.ReportGeneratedAt,
                    BatchStatus = b.Status,
                    FinancialYearStart = LabReportRules.FinancialYearStart(b.BatchDate),
                    AssignedToName = b.AssignedToName
                }, location));
            }
            return result;
        }

        private async Task<List<LabBatchRowDto>> BuildBatchRowsAsync(List<SampleBatch> batches)
        {
            var ids = batches.Select(b => b.Id).ToList();
            var consignments = await ConsignmentCodesAsync(ids);
            var items = await _db.SampleItems.AsNoTracking()
                .Where(i => !i.IsDeleted && !i.Collection!.IsDeleted && i.Collection.Consignment != null &&
                            i.Collection.Consignment.BatchId != null && ids.Contains(i.Collection.Consignment.BatchId.Value))
                .Select(i => new { BatchId = i.Collection!.Consignment!.BatchId!.Value, i.AnalysisStatus, i.Collection.PaymentType })
                .ToListAsync();
            var firstReports = await _db.LabReports.AsNoTracking()
                .Where(r => ids.Contains(r.BatchId))
                .GroupBy(r => r.BatchId)
                .Select(g => new { BatchId = g.Key, Id = g.Min(r => r.Id) })
                .ToListAsync();

            return batches.Select(b =>
            {
                var own = items.Where(i => i.BatchId == b.Id).ToList();
                var days = AnalysisDays(b);
                return new LabBatchRowDto
                {
                    Id = b.Id,
                    Code = b.Code,
                    BatchDate = b.BatchDate,
                    ConsignmentCodes = consignments.GetValueOrDefault(b.Id) ?? new List<string>(),
                    SampleCount = own.Count > 0 ? own.Count : b.SampleCount,
                    SampleType = own.Any(i => i.PaymentType == SamplePaymentType.Paid) ? SamplePaymentType.Paid : SamplePaymentType.Free,
                    AssignedToUserId = b.AssignedToUserId,
                    AssignedToName = b.AssignedToName,
                    AssignedAt = b.AssignedAt,
                    AssignedByName = b.AssignedByName,
                    Priority = b.Priority,
                    Status = b.Status,
                    AnalysisDays = days,
                    IsDelayed = days.HasValue && b.Status < SampleBatchStatus.AnalysisCompleted && days.Value > DelayedAfterDays(),
                    PendingEntries = own.Count(i => i.AnalysisStatus != SampleAnalysisStatus.Completed),
                    CompletedEntries = own.Count(i => i.AnalysisStatus == SampleAnalysisStatus.Completed),
                    ReportId = firstReports.FirstOrDefault(r => r.BatchId == b.Id)?.Id,
                    ReportCode = b.ReportCode
                };
            }).ToList();
        }

        private async Task<List<LabBatchStepDto>> TimelineAsync(SampleBatch b)
        {
            var reportBy = await _db.LabActivities.AsNoTracking()
                .Where(a => a.BatchId == b.Id && a.Kind == LabActivityKind.ReportGenerated)
                .OrderByDescending(a => a.At)
                .Select(a => a.ByName)
                .FirstOrDefaultAsync();
            var completedAt = b.CompletedAt ?? b.ReportGeneratedAt;
            var steps = new List<LabBatchStepDto>
            {
                new()
                {
                    Status = SampleBatchStatus.Created, Title = "Created",
                    Description = $"Batch {b.Code} created", At = b.CreatedAt, ByName = b.CreatedByName, IsDone = true
                },
                new()
                {
                    Status = SampleBatchStatus.TakenForAnalysis, Title = "Taken for Analysis",
                    Description = string.IsNullOrWhiteSpace(b.AssignedToName) ? "Taken for analysis" : $"Assigned to {b.AssignedToName}",
                    At = b.TakenForAnalysisAt, ByName = b.AssignedByName ?? b.CreatedByName,
                    IsDone = b.Status >= SampleBatchStatus.TakenForAnalysis || b.TakenForAnalysisAt.HasValue
                },
                new()
                {
                    Status = SampleBatchStatus.AnalysisCompleted, Title = "Analysis Completed",
                    Description = "Values entered and results generated for every sample", At = b.AnalysisCompletedAt, ByName = b.AssignedToName,
                    IsDone = b.Status >= SampleBatchStatus.AnalysisCompleted || b.AnalysisCompletedAt.HasValue
                },
                new()
                {
                    Status = SampleBatchStatus.Completed, Title = "Completed",
                    Description = string.IsNullOrWhiteSpace(b.ReportCode) ? "Reports generated" : $"Reports generated ({b.ReportCode})",
                    At = completedAt, ByName = reportBy,
                    IsDone = b.Status == SampleBatchStatus.Completed || b.ReportGeneratedAt.HasValue
                }
            };
            var current = steps.LastOrDefault(s => s.IsDone);
            if (current != null) current.IsCurrent = true;
            return steps;
        }

        private int? AnalysisDays(SampleBatch b)
        {
            if (!b.TakenForAnalysisAt.HasValue) return null;
            var end = b.AnalysisCompletedAt ?? DateTime.Now;
            return Math.Max(0, (int)(end.Date - b.TakenForAnalysisAt.Value.Date).TotalDays);
        }

        private int DelayedAfterDays() => _config.GetValue<int?>("Sas:Lab:DelayedAfterDays") ?? 5;

        // ================================================================ helpers

        private async Task CountDownloadsAsync(IReadOnlyCollection<int> ids)
        {
            var now = DateTime.Now;
            try
            {
                await _db.LabReports.Where(r => ids.Contains(r.Id)).ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.DownloadCount, r => r.DownloadCount + 1)
                    .SetProperty(r => r.LastDownloadedAt, now)
                    .SetProperty(r => r.Status, r => r.Status == LabReportStatus.Printed ? LabReportStatus.Printed : LabReportStatus.Downloaded));
            }
            catch (Exception ex)
            {
                // The file is still served; only the counter is lost.
                _logger.LogWarning(ex, "LabReports: could not count the download of {Ids}.", string.Join(",", ids));
            }
        }

        private IActionResult FileResult(LabReportFile file)
        {
            if (file.FallbackFrom != null) Response.Headers[FallbackHeader] = file.FallbackFrom;
            return File(file.Content, file.ContentType, file.FileName);
        }

        private static (int Page, int Size) Paging(int page, int pageSize) =>
            (Math.Max(1, page), pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize));

        private readonly record struct SampleTypeFilter(SampleType? Type, SamplePaymentType? Payment, bool Invalid);

        private static SampleTypeFilter ParseSampleType(string raw)
        {
            var v = raw.Trim();
            if (Enum.TryParse<SamplePaymentType>(v, true, out var payment) && !int.TryParse(v, out _))
                return new SampleTypeFilter(null, payment, false);
            if (TryParseEnum<SampleType>(v, out var type)) return new SampleTypeFilter(type, null, false);
            return new SampleTypeFilter(null, null, true);
        }

        private static bool TryParseEnum<TEnum>(string? raw, out TEnum value) where TEnum : struct, Enum
        {
            value = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            return Enum.TryParse(raw.Trim(), true, out value) && Enum.IsDefined(value);
        }

        private ObjectResult Forbid403(string message) => StatusCode(403, new { Success = false, Message = message });
    }
}
