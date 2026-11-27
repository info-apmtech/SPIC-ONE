using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
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
    /// The rating thresholds are the lab parameter master's (S-N, S-P, S-K, S-PH) and the dose factors the soil
    /// report's (<see cref="LabFertilizerSchedule"/>, Sas:Lab:DoseFactors), so this advice and the SPIC soil
    /// report agree. The arithmetic is <see cref="FertilizerAdvisor"/>.
    /// </summary>
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

        public FarmerFertilizerController(AppDbContext db, LabFertilizerSchedule schedule, ILogger<FarmerFertilizerController> log)
        {
            _db = db;
            _schedule = schedule;
            _log = log;
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
            var t = await ThresholdsAsync(ct);

            var dto = new FertilizerOptionsDto
            {
                SoilTypes = FertilizerAdvisor.SoilTypes.ToList(),
                Ranges = new List<FertilizerRangeDto>
                {
                    Range(t.N, 0, 800, 1, 100),
                    Range(t.P, 0, 60, 0.5, 5),
                    Range(t.K, 0, 500, 1, 50),
                    Range(t.Ph, 4.5, 9.5, 0.1, 7.0)
                },
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

        /// <summary>Slider range: the default range, widened (to 150 % of the High threshold) when the High zone would be
        /// thinner than a quarter of the threshold, so it stays visible and the scale labels do not collide.</summary>
        public static FertilizerRangeDto Range(FertilizerAdvisor.Threshold t, double min, double max, double step, double def)
        {
            var high = (double)t.HighAbove;
            if (t.Code != "pH" && high * 1.25 > max) max = Math.Ceiling(high * 1.5 / 10) * 10;
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
                FromLabMaster = t.FromLabMaster
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
            dto.N = ReadValue(rows, _schedule.ParameterCodeFor("N") ?? "S-N", "Nitrogen", "Nitrogen", true, dto.Notes);
            dto.P = ReadValue(rows, _schedule.ParameterCodeFor("P") ?? "S-P", "Phosphorus", "Phosphorus", true, dto.Notes);
            dto.K = ReadValue(rows, _schedule.ParameterCodeFor("K") ?? "S-K", "Potassium", "Potassium", true, dto.Notes);
            dto.Ph = ReadValue(rows, PhCode, "pH", "pH", false, dto.Notes);
            if (dto.N is null && dto.P is null && dto.K is null && dto.Ph is null)
            {
                dto.Available = false;
                dto.Message = "Your latest soil report has no N, P, K or pH values we can use.";
            }
            return Ok(dto);
        }

        public sealed record SoilReportRow(string? Code, SampleType? AppliesTo, string Parameter, string? Value, string? Unit);

        /// <summary>
        /// The value of one parameter in kg/ha (pH as it is): the row of the master code, else (v1 rows without a
        /// master link) the row named like the parameter. Values in kg/acre or ppm are converted
        /// (<see cref="FertilizerAdvisor.ToKgPerHa"/>); a value that cannot be read adds a note and gives null.
        /// </summary>
        public static double? ReadValue(IReadOnlyList<SoilReportRow> rows, string code, string name, string label, bool convert, List<string> notes)
        {
            var row = rows.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase))
                      ?? rows.FirstOrDefault(r => r.Code is null && string.Equals(r.Parameter?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            if (row is null || string.IsNullOrWhiteSpace(row.Value)) return null;
            if (!decimal.TryParse(row.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                notes.Add($"{label}: the report value \"{row.Value.Trim()}\" is not a number, so it was not used.");
                return null;
            }
            if (!convert) return (double)value;
            var kg = FertilizerAdvisor.ToKgPerHa(value, row.Unit, out var note);
            if (kg is null)
            {
                notes.Add($"{label}: {note}, so it was not used.");
                return null;
            }
            if (note is not null) notes.Add($"{label}: {value.ToString("0.##", CultureInfo.InvariantCulture)} {row.Unit} {note} = {kg.Value.ToString("0.#", CultureInfo.InvariantCulture)} kg/ha.");
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
            var t = await ThresholdsAsync(ct);

            var result = FertilizerAdvisor.Build(new FertilizerAdvisor.Inputs(norm!, norm!.Splits.ToList(), tips, t.N, t.P, t.K, t.Ph,
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
                SoilP = request.P,
                SoilK = request.K,
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
                ResultJson = "{}",
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.FertilizerRecommendationRuns.Add(run);
            await _db.SaveChangesAsync(ct);

            result.RunId = run.Id;
            result.CreatedAtUtc = run.CreatedAtUtc;
            run.ResultJson = JsonSerializer.Serialize(result, StoreJson);
            await _db.SaveChangesAsync(ct);

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
            var json = await _db.FertilizerRecommendationRuns.AsNoTracking()
                .Where(r => r.Id == id && r.FarmerUserId == userId)
                .Select(r => r.ResultJson)
                .FirstOrDefaultAsync(ct);
            if (json is null) return NotFound(new FertilizerErrorDto { Message = "This recommendation was not found." });
            var dto = JsonSerializer.Deserialize<FertilizerRecommendationDto>(json, StoreJson);
            return dto is null ? NotFound(new FertilizerErrorDto { Message = "This recommendation was not found." }) : Ok(dto);
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

        private sealed record Thresholds(FertilizerAdvisor.Threshold N, FertilizerAdvisor.Threshold P, FertilizerAdvisor.Threshold K, FertilizerAdvisor.Threshold Ph);

        /// <summary>The lab master's ranges for N / P / K (codes from Sas:Lab:NutrientParameters) and pH; defaults when missing.</summary>
        private async Task<Thresholds> ThresholdsAsync(CancellationToken ct)
        {
            var nCode = _schedule.ParameterCodeFor("N") ?? "S-N";
            var pCode = _schedule.ParameterCodeFor("P") ?? "S-P";
            var kCode = _schedule.ParameterCodeFor("K") ?? "S-K";
            var codes = new[] { nCode, pCode, kCode, PhCode };
            var master = await _db.LabParameters.AsNoTracking().Where(p => codes.Contains(p.Code) && p.IsActive).ToListAsync(ct);
            LabParameter? Row(string code) => master.FirstOrDefault(p => p.Code == code);
            return new Thresholds(
                FertilizerAdvisor.FromLab(Row(nCode), FertilizerAdvisor.DefaultN),
                FertilizerAdvisor.FromLab(Row(pCode), FertilizerAdvisor.DefaultP),
                FertilizerAdvisor.FromLab(Row(kCode), FertilizerAdvisor.DefaultK),
                FertilizerAdvisor.FromLab(Row(PhCode), FertilizerAdvisor.DefaultPh));
        }

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
