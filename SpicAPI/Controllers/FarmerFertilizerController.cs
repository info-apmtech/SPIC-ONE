using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using SpicAPI.Services;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Lab;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Farmer portal Task 4 (docs/farmer-portal.md): fertilizer recommendation. Farmer role only; every query
    /// is scoped to the caller's user id.
    ///
    ///   GET  api/Farmer/fertilizer/options        crops / variants / soil types / slider ranges / products
    ///   GET  api/Farmer/fertilizer/soil-report    N / P / K / pH of the caller's latest SPIC soil report (kg/ha)
    ///   POST api/Farmer/fertilizer/recommend      the recommendation (saved as a run)
    ///   GET  api/Farmer/fertilizer/history        the caller's latest 20 runs
    ///   GET  api/Farmer/fertilizer/history/{id}   one of the caller's runs in full
    ///   GET  api/Farmer/fertilizer/latest         the caller's latest run in short (204 when none)
    ///
    /// Ratings use the farmer tool's elemental thresholds (fix round 1: N 280 / 560, P 11 / 22 kg P/ha, K 118 / 280
    /// kg K/ha, pH 6.5 / 7.5; P may be entered as P2O5 and K as K2O); the dose factors are the soil report's
    /// (<see cref="LabFertilizerSchedule"/>, Sas:Lab:DoseFactors). The arithmetic is <see cref="FertilizerAdvisor"/>.
    /// </summary>
    [FarmerAccess]
    [Authorize(Roles = nameof(AppRole.Farmer))]
    [ApiController]
    [Route("api/Farmer/fertilizer")]
    public class FarmerFertilizerController : ControllerBase
    {
        public const string PhCode = "S-PH";

        private static readonly JsonSerializerOptions StoreJson = new() { WriteIndented = false };

        private readonly AppDbContext _db;
        private readonly LabFertilizerSchedule _schedule;
        private readonly ILogger<FarmerFertilizerController> _log;
        private readonly IConfiguration _config;

        public FarmerFertilizerController(AppDbContext db, LabFertilizerSchedule schedule, ILogger<FarmerFertilizerController> log, IConfiguration config)
        {
            _db = db;
            _schedule = schedule;
            _log = log;
            _config = config;
        }

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ------------------------------------------------------------------ options

        [HttpGet("options")]
        public async Task<ActionResult<FertilizerOptionsDto>> Options(CancellationToken ct)
        {
            var norms = await _db.FertilizerCropNorms.AsNoTracking()
                .Where(n => n.IsActive)
                .OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
                .ToListAsync(ct);
            var dto = new FertilizerOptionsDto
            {
                SoilTypes = FertilizerAdvisor.SoilTypes.ToList(),
                Ranges = Ranges(),
                Products = FertilizerAdvisor.Products.Select(p => new FertilizerProductDto
                {
                    Code = p.Code, Name = p.Name, Grade = p.Grade, SpicName = p.SpicName, BagKg = p.BagKg
                }).ToList()
            };
            foreach (var group in norms.GroupBy(n => n.Crop))
            {
                dto.Crops.Add(new FertilizerCropOptionDto
                {
                    Crop = group.Key,
                    Variants = group.Select(n => new FertilizerVariantOptionDto
                    {
                        NormId = n.Id, Variant = n.Variant, SoilType = n.SoilType, WaterRegime = n.WaterRegime,
                        Basis = n.Basis, PlantsPerHectare = n.PlantsPerHectare, Spacing = n.Spacing
                    }).ToList()
                });
            }
            return Ok(dto);
        }

        /// <summary>The four sliders (owner's sketch): N 0-800 default 100, P 0-60 default 5 (kg P/ha; switchable to P2O5
        /// x 2.29), K 0-500 default 50 (kg K/ha; switchable to K2O x 1.2), pH 4.5-9.5 default 7.0.</summary>
        public static List<FertilizerRangeDto> Ranges() => new()
        {
            Range(FertilizerAdvisor.DefaultN, 0, 800, 1, 100, "N", null, null),
            Range(FertilizerAdvisor.DefaultP, 0, 60, 0.5, 5, FertilizerAdvisor.BasisP, FertilizerAdvisor.BasisP2O5, FertilizerAdvisor.P2O5PerP),
            Range(FertilizerAdvisor.DefaultK, 0, 500, 1, 50, FertilizerAdvisor.BasisK, FertilizerAdvisor.BasisK2O, FertilizerAdvisor.K2OPerK),
            Range(FertilizerAdvisor.DefaultPh, 4.5, 9.5, 0.1, 7.0, "", null, null)
        };

        public static FertilizerRangeDto Range(FertilizerAdvisor.Threshold t, double min, double max, double step, double def,
            string basis, string? altBasis, decimal? altFactor)
        {
            var high = (double)t.HighAbove;
            return new FertilizerRangeDto
            {
                Code = t.Code,
                Label = t.Label,
                Unit = t.Unit,
                Min = min,
                Max = max,
                Step = step,
                Default = def,
                LowBelow = (double)t.LowBelow,
                HighAbove = high,
                ModerateFrom = t.ModerateFrom is { } m ? (double)m : null,
                LowLabel = t.LowLabel,
                MediumLabel = t.NormalLabel,
                HighLabel = t.HighLabel,
                FromLabMaster = false,
                Basis = basis,
                AltBasis = altBasis,
                AltFactor = altFactor is { } f ? (double)f : null
            };
        }

        // ------------------------------------------------------------------ soil report

        [HttpGet("soil-report")]
        public async Task<ActionResult<FertilizerSoilReportDto>> SoilReport(CancellationToken ct)
        {
            var farmerIds = await MyFarmerIdsAsync(ct);
            if (farmerIds.Count == 0)
                return Ok(new FertilizerSoilReportDto { Message = "Your account is not linked to a SPIC soil test yet. Ask your SPIC officer to test your soil." });

            var report = await MyReports(farmerIds)
                .OrderByDescending(r => r.GeneratedAt).ThenByDescending(r => r.Id)
                .Select(r => new { r.Id, r.Code, r.GeneratedAt, r.SampleItemId, Crop = r.SampleItem!.Crop1 })
                .FirstOrDefaultAsync(ct);
            if (report is null)
                return Ok(new FertilizerSoilReportDto { Message = "No soil report is ready for you yet. It appears here when the SPIC lab finishes your test." });

            var rows = await (
                from result in _db.SampleLabResults.AsNoTracking()
                join p in _db.LabParameters.AsNoTracking() on result.LabParameterId equals p.Id into pj
                from p in pj.DefaultIfEmpty()
                where result.SampleItemId == report.SampleItemId
                select new SoilReportRow(p != null ? p.Code : null, p != null ? p.AppliesTo : (SampleType?)null, result.Parameter, result.EnteredValue,
                    result.Unit ?? (p != null ? p.Unit : null)))
                .ToListAsync(ct);

            var dto = new FertilizerSoilReportDto
            {
                Available = true,
                ReportId = report.Id,
                ReportCode = report.Code,
                GeneratedAt = report.GeneratedAt,
                SampleCrop = string.IsNullOrWhiteSpace(report.Crop) ? null : report.Crop.Trim()
            };
            Fill(dto, rows, _schedule.ParameterCodeFor("N") ?? "S-N", _schedule.ParameterCodeFor("P") ?? "S-P", _schedule.ParameterCodeFor("K") ?? "S-K");
            return Ok(dto);
        }

        public sealed record SoilReportRow(string? Code, SampleType? AppliesTo, string Parameter, string? Value, string? Unit);

        /// <summary>
        /// Fills N / P / K / pH of a report on the farmer tool's basis (<see cref="FertilizerAdvisor.ReportToElemental"/>:
        /// P read as P2O5 kg/ha and converted to P, ppm x 2.24). A value the report does not give is listed in
        /// <see cref="FertilizerSoilReportDto.Missing"/> with a note, so the screen leaves it for the farmer to set.
        /// </summary>
        public static void Fill(FertilizerSoilReportDto dto, IReadOnlyList<SoilReportRow> rows, string nCode, string pCode, string kCode)
        {
            dto.N = ReadValue(rows, nCode, "Nitrogen", "Nitrogen", "N", dto.Notes, out _);
            dto.P = ReadValue(rows, pCode, "Phosphorus", "Phosphorus", "P", dto.Notes, out var pUnit);
            dto.K = ReadValue(rows, kCode, "Potassium", "Potassium", "K", dto.Notes, out _);
            dto.Ph = ReadValue(rows, PhCode, "pH", "pH", null, dto.Notes, out _);
            if (dto.P is { } pElemental)
            {
                var u = (pUnit ?? "").Trim().ToLowerInvariant().Replace(" ", "");
                var ppm = u is "ppm" or "mg/kg";
                dto.ReportPBasis = ppm ? FertilizerAdvisor.BasisP : FertilizerAdvisor.BasisP2O5;
                // kg/ha P2O5 as printed (kg/acre x 2.47105); ppm is already elemental P.
                dto.PAsReported = ppm ? pElemental : ReadValue(rows, pCode, "Phosphorus", "Phosphorus", "N", new List<string>(), out _);
            }
            foreach (var (code, label, value) in new[] { ("N", "Nitrogen", dto.N), ("P", "Phosphorus", dto.P), ("K", "Potassium", dto.K), ("pH", "Soil pH", dto.Ph) })
            {
                if (value is not null) continue;
                dto.Missing.Add(code);
                dto.Notes.Add($"{label} is not in your report or could not be read: enter it from your soil card.");
            }
            if (dto.Missing.Count == 4)
            {
                dto.Available = false;
                dto.Message = "Your latest soil report has no N, P, K or pH values we can use.";
                dto.Notes.Clear();
            }
        }

        /// <summary>
        /// The value of one parameter: the row of the master code, else (v1 rows without a master link) the row named
        /// like the parameter. N / P / K go through <see cref="FertilizerAdvisor.ReportToElemental"/> (kg/ha, P as P);
        /// pH (nutrient null) as it is. A value that cannot be read adds a note and gives null.
        /// </summary>
        public static double? ReadValue(IReadOnlyList<SoilReportRow> rows, string code, string name, string label, string? nutrient, List<string> notes, out string? unit)
        {
            unit = null;
            var row = rows.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase))
                      ?? rows.FirstOrDefault(r => r.Code is null && string.Equals(r.Parameter?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (row is null || string.IsNullOrWhiteSpace(row.Value)) return null;
            unit = row.Unit;
            if (!decimal.TryParse(row.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                notes.Add($"{label}: the report value \"{row.Value.Trim()}\" is not a number, so it was not used.");
                return null;
            }
            if (nutrient is null) return (double)value;
            var kg = FertilizerAdvisor.ReportToElemental(nutrient, value, row.Unit, out var note);
            if (kg is null)
            {
                notes.Add($"{label}: {note}, so it was not used.");
                return null;
            }
            // The SPIC lab's own kg/ha P2O5 value is not a "conversion" the farmer needs to read about: the screen shows it
            // on the P2O5 basis. Only real unit changes (kg/acre, ppm) get a note.
            if (note is not null && (nutrient != "P" || note.Contains("kg/acre") || note.Contains("ppm")))
            {
                var basis = nutrient == "P" ? "kg P/ha" : "kg/ha";
                notes.Add($"{label}: {value.ToString("0.##", CultureInfo.InvariantCulture)} {row.Unit} {note} = {kg.Value.ToString("0.#", CultureInfo.InvariantCulture)} {basis}.");
            }
            return (double)kg.Value;
        }

        // ------------------------------------------------------------------ recommend

        [HttpPost("recommend")]
        public async Task<IActionResult> Recommend([FromBody] FertilizerRecommendRequest? request, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            if (request is null) return BadRequest(new FertilizerErrorDto { Message = "Please fill in the form." });

            var norm = await _db.FertilizerCropNorms.AsNoTracking()
                .Include(n => n.Splits)
                .FirstOrDefaultAsync(n => n.Id == request.NormId && n.IsActive, ct);
            var errors = FertilizerAdvisor.Validate(request, norm);
            if (request.SoilReportId is { } reportId && errors.Count == 0)
            {
                var farmerIds = await MyFarmerIdsAsync(ct);
                if (farmerIds.Count == 0 || !await MyReports(farmerIds).AnyAsync(r => r.Id == reportId, ct))
                    errors["soilReportId"] = "This soil report is not yours.";
            }
            if (errors.Count > 0)
                return BadRequest(new FertilizerErrorDto { Message = errors.Values.First(), Errors = errors });

            var tips = await _db.CropSoilTips.AsNoTracking()
                .Where(t => t.IsActive && (t.Crop == null || t.Crop == norm!.Crop))
                .ToListAsync(ct);
            var result = FertilizerAdvisor.Build(new FertilizerAdvisor.Inputs(norm!, norm!.Splits.ToList(), tips,
                FertilizerAdvisor.DefaultN, FertilizerAdvisor.DefaultP, FertilizerAdvisor.DefaultK, FertilizerAdvisor.DefaultPh,
                (code, status) => _schedule.FactorFor(code, status.ToString()), request));

            var run = new FertilizerRecommendationRun
            {
                FarmerUserId = userId,
                NormId = norm.Id,
                Crop = norm.Crop,
                Variant = norm.Variant,
                SoilType = request.SoilType,
                AreaAcres = request.AreaAcres,
                SoilN = request.N,
                SoilP = result.Nutrients[1].SoilValue,
                SoilK = result.Nutrients[2].SoilValue,
                SoilPh = Math.Round(request.Ph, 1, MidpointRounding.AwayFromZero),
                NStatus = result.Nutrients[0].Level,
                PStatus = result.Nutrients[1].Level,
                KStatus = result.Nutrients[2].Level,
                PhBand = result.Ph.Band,
                NeedNKgPerAcre = result.Nutrients[0].NeedKgPerAcre,
                NeedPKgPerAcre = result.Nutrients[1].NeedKgPerAcre,
                NeedKKgPerAcre = result.Nutrients[2].NeedKgPerAcre,
                FromSoilReport = request.SoilReportId is not null,
                SoilReportId = request.SoilReportId,
                Summary = result.Summary.Length > 500 ? result.Summary[..500] : result.Summary,
                InputsJson = JsonSerializer.Serialize(request, StoreJson),
                CreatedAtUtc = DateTime.UtcNow
            };
            // One SaveChanges (final fix wave, review M2): the stored result has no id; the id and time are the row's
            // own columns and are put back when the run is read (Run below).
            result.CreatedAtUtc = run.CreatedAtUtc;
            run.ResultJson = JsonSerializer.Serialize(result, StoreJson);

            // Per-farmer daily cap, counted and inserted under one advisory lock (FarmerDailyCapRules).
            var limit = FarmerDailyCapRules.Limit(_config["FarmerPortal:FertilizerDailyLimit"], FarmerDailyCapRules.FertilizerRunsDefault);
            await using (var tx = await FarmerDailyLock.AcquireAsync(_db, "fp:fert:" + userId, ct))
            {
                var start = FarmerAssistantRules.IstDayStartUtc(run.CreatedAtUtc);
                var used = await _db.FertilizerRecommendationRuns.CountAsync(r => r.FarmerUserId == userId && r.CreatedAtUtc >= start, ct);
                if (!FarmerDailyCapRules.Allows(used, limit))
                {
                    Response.Headers.RetryAfter = FarmerAssistantRules.SecondsUntilReset(run.CreatedAtUtc).ToString();
                    return StatusCode(StatusCodes.Status429TooManyRequests, new FertilizerErrorDto { Message = FarmerDailyCapRules.FertilizerMessage(limit) });
                }
                _db.FertilizerRecommendationRuns.Add(run);
                await _db.SaveChangesAsync(ct);
                if (tx is not null) await tx.CommitAsync(ct);
            }
            result.RunId = run.Id;

            _log.LogInformation("Fertilizer run {RunId}: norm {NormId}, {Acres} acres", run.Id, norm.Id, request.AreaAcres);
            return Ok(result);
        }

        // ------------------------------------------------------------------ history

        [HttpGet("history")]
        public async Task<ActionResult<List<FertilizerRunSummaryDto>>> History(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var runs = await _db.FertilizerRecommendationRuns.AsNoTracking()
                .Where(r => r.FarmerUserId == userId)
                .OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
                .Take(FertilizerAdvisor.HistorySize)
                .Select(Summary)
                .ToListAsync(ct);
            return Ok(runs);
        }

        [HttpGet("history/{id:long}")]
        public async Task<IActionResult> Run(long id, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var row = await _db.FertilizerRecommendationRuns.AsNoTracking()
                .Where(r => r.Id == id && r.FarmerUserId == userId)
                .Select(r => new { r.Id, r.CreatedAtUtc, r.ResultJson })
                .FirstOrDefaultAsync(ct);
            if (row is null) return NotFound(new FertilizerErrorDto { Message = "This recommendation was not found." });
            var dto = JsonSerializer.Deserialize<FertilizerRecommendationDto>(row.ResultJson, StoreJson);
            if (dto is null) return NotFound(new FertilizerErrorDto { Message = "This recommendation was not found." });
            // The id and time come from the row (runs saved since the final fix wave store the result without its id).
            dto.RunId = row.Id;
            dto.CreatedAtUtc = DateTime.SpecifyKind(row.CreatedAtUtc, DateTimeKind.Utc);
            return Ok(dto);
        }

        /// <summary>The caller's latest run in short, for the chatbot context and the yield forecast prefill.</summary>
        [HttpGet("latest")]
        public async Task<IActionResult> Latest(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var run = await _db.FertilizerRecommendationRuns.AsNoTracking()
                .Where(r => r.FarmerUserId == userId)
                .OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
                .Select(Summary)
                .FirstOrDefaultAsync(ct);
            return run is null ? NoContent() : Ok(run);
        }

        private static readonly System.Linq.Expressions.Expression<Func<FertilizerRecommendationRun, FertilizerRunSummaryDto>> Summary = r => new FertilizerRunSummaryDto
        {
            Id = r.Id,
            CreatedAtUtc = r.CreatedAtUtc,
            NormId = r.NormId,
            Crop = r.Crop,
            Variant = r.Variant,
            SoilType = r.SoilType,
            AreaAcres = r.AreaAcres,
            SoilN = r.SoilN,
            SoilP = r.SoilP,
            SoilK = r.SoilK,
            SoilPh = r.SoilPh,
            NStatus = r.NStatus,
            PStatus = r.PStatus,
            KStatus = r.KStatus,
            PhBand = r.PhBand,
            NeedNKgPerAcre = r.NeedNKgPerAcre,
            NeedPKgPerAcre = r.NeedPKgPerAcre,
            NeedKKgPerAcre = r.NeedKKgPerAcre,
            FromSoilReport = r.FromSoilReport,
            Summary = r.Summary
        };

        // ------------------------------------------------------------------ helpers

        /// <summary>The caller's SAS farmer records: linked by user id, or the same mobile (as LabReportsController.MyFarmerIdsAsync).</summary>
        private async Task<List<int>> MyFarmerIdsAsync(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrWhiteSpace(userId)) return new List<int>();
            var account = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.UserName, u.PhoneNumber })
                .FirstOrDefaultAsync(ct);
            var userName = account?.UserName ?? "";
            var phone = account?.PhoneNumber ?? "";
            return await _db.SasFarmers.AsNoTracking()
                .Where(f => !f.IsDeleted && (
                    f.UserId == userId ||
                    (userName != "" && f.Mobile == userName) ||
                    (phone != "" && f.Mobile == phone)))
                .Select(f => f.Id)
                .ToListAsync(ct);
        }

        /// <summary>Completed soil reports of these SAS farmers (deleted batches / samples / collections left out).</summary>
        private IQueryable<LabReport> MyReports(List<int> farmerIds) =>
            _db.LabReports.AsNoTracking()
                .Where(r => r.SampleType == SampleType.Soil
                            && !r.Batch!.IsDeleted && !r.SampleItem!.IsDeleted && !r.SampleItem.Collection!.IsDeleted
                            && farmerIds.Contains(r.SampleItem.FarmerId));
    }
}
