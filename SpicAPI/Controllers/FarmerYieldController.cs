using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Farmer portal Task 7 (docs/farmer-portal.md): the harvest yield forecast. Farmer role only; every query is
    /// scoped to the caller's user id. No AI call: the estimate is the documented rule model <see cref="YieldEstimator"/>
    /// over the seeded crop norms (CropYieldNorms).
    ///
    ///   GET  api/Farmer/yield/options        crops (a norm per irrigation class), methods, slider ranges, prefill
    ///   POST api/Farmer/yield/predict        the forecast (saved)
    ///   GET  api/Farmer/yield/history        the caller's latest 20 forecasts
    ///   GET  api/Farmer/yield/history/{id}   one of the caller's forecasts in full
    ///
    /// Prefill: crop, area and irrigation from the farmer's profile; NPK applied stays at the sketch's 90 % because a
    /// fertilizer run says what to apply, not what was applied (the latest run's crop and date are returned so the
    /// screen can remind the farmer of it).
    /// </summary>
    [Authorize(Roles = nameof(AppRole.Farmer))]
    [ApiController]
    [Route("api/Farmer/yield")]
    public class FarmerYieldController : ControllerBase
    {
        private static readonly JsonSerializerOptions StoreJson = new() { WriteIndented = false };

        private readonly AppDbContext _db;
        private readonly ILogger<FarmerYieldController> _log;

        public FarmerYieldController(AppDbContext db, ILogger<FarmerYieldController> log)
        {
            _db = db;
            _log = log;
        }

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private Task<List<CropYieldNorm>> NormsAsync(CancellationToken ct) =>
            _db.CropYieldNorms.AsNoTracking()
                .Where(n => n.IsActive)
                .OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
                .ToListAsync(ct);

        // ------------------------------------------------------------------ options

        [HttpGet("options")]
        public async Task<ActionResult<YieldOptionsDto>> Options(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var norms = await NormsAsync(ct);
            var dto = BuildOptions(norms);

            var profile = await _db.FarmerProfiles.AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new { p.PrimaryCrops, p.LandAcres, p.IrrigationType })
                .FirstOrDefaultAsync(ct);
            var lastRun = await _db.FertilizerRecommendationRuns.AsNoTracking()
                .Where(r => r.FarmerUserId == userId)
                .OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
                .Select(r => new { r.Crop, r.CreatedAtUtc })
                .FirstOrDefaultAsync(ct);
            dto.Prefill = Prefill(norms, FarmerProfileRules.SplitCrops(profile?.PrimaryCrops), profile?.LandAcres, profile?.IrrigationType,
                lastRun?.Crop, lastRun?.CreatedAtUtc);
            return Ok(dto);
        }

        /// <summary>Crops, methods and the three sliders (owner's sketch: NDVI 0.35-0.92 default 0.72, rainfall 400-1400 mm
        /// default 850, NPK applied 50-120 % default 90).</summary>
        public static YieldOptionsDto BuildOptions(IReadOnlyList<CropYieldNorm> norms)
        {
            var dto = new YieldOptionsDto
            {
                Methods = YieldEstimator.Methods.Select(m => new YieldMethodOptionDto
                {
                    Code = m.Code, Label = m.Label, IrrigationClass = m.IrrigationClass, Icon = m.Icon
                }).ToList(),
                Ndvi = new YieldSliderDto
                {
                    Min = YieldEstimator.NdviMin, Max = YieldEstimator.NdviMax, Step = YieldEstimator.NdviStep, Default = YieldEstimator.NdviDefault,
                    LowBelow = YieldEstimator.NdviFullFrom, HighAbove = YieldEstimator.NdviLodgingAbove,
                    LowLabel = "Thin", MediumLabel = "Healthy", HighLabel = "Very dense"
                },
                Looks = YieldEstimator.Looks.Select(l => new YieldLookOptionDto { Key = l.Key, Label = l.Label, Hint = l.Hint, Ndvi = l.Ndvi }).ToList(),
                // Zones of the rainfall slider follow the chosen crop (its band); these are the paddy defaults.
                Rainfall = new YieldSliderDto
                {
                    Min = YieldEstimator.RainMin, Max = YieldEstimator.RainMax, Step = YieldEstimator.RainStep, Default = YieldEstimator.RainDefault,
                    LowBelow = 800, HighAbove = 1300, LowLabel = "Below need", MediumLabel = "In range", HighLabel = "Very wet"
                },
                Npk = new YieldSliderDto
                {
                    Min = YieldEstimator.NpkMin, Max = YieldEstimator.NpkMax, Step = YieldEstimator.NpkStep, Default = YieldEstimator.NpkDefault,
                    LowBelow = 75, HighAbove = 100, LowLabel = "Too little", MediumLabel = "Good", HighLabel = "More than needed"
                }
            };
            foreach (var group in norms.GroupBy(n => n.Crop))
            {
                dto.Crops.Add(new YieldCropOptionDto
                {
                    Crop = group.Key,
                    Product = group.First().Product,
                    Norms = group.OrderBy(n => n.IrrigationClass == YieldEstimator.Irrigated ? 0 : 1).Select(n => new YieldNormOptionDto
                    {
                        NormId = n.Id,
                        IrrigationClass = n.IrrigationClass,
                        TypicalTPerHa = (double)n.TypicalTPerHa,
                        LowTPerHa = (double)n.LowTPerHa,
                        HighTPerHa = (double)n.HighTPerHa,
                        RainLowMm = n.RainLowMm,
                        RainHighMm = n.RainHighMm
                    }).ToList()
                });
            }
            return dto;
        }

        /// <summary>
        /// The form's starting values: the first profile crop that has a norm (else Paddy), the profile's acres (else
        /// 3.0) and irrigation (canal / borewell / mixed = "Canal / Borewell Irrigated"; a rainfed profile on a crop with
        /// no rainfed norm falls back to canal / borewell). NPK applied 90 % (no run records what was applied).
        /// </summary>
        public static YieldPrefillDto Prefill(IReadOnlyList<CropYieldNorm> norms, IReadOnlyList<string> profileCrops, decimal? landAcres,
            FarmerIrrigationType? irrigation, string? lastFertilizerCrop, DateTime? lastFertilizerAt)
        {
            var crops = norms.Select(n => n.Crop).Distinct().ToList();
            var profileCrop = YieldEstimator.ProfileCrop(profileCrops, crops);
            var crop = profileCrop ?? (crops.Contains(YieldEstimator.DefaultCrop) ? YieldEstimator.DefaultCrop : crops.FirstOrDefault());
            var method = YieldEstimator.MethodForProfile(irrigation);
            var methodFromProfile = irrigation is not null;
            if (YieldEstimator.PickNorm(norms, crop, method) is null)
            {
                method = YieldEstimator.MethodCanalBorewell;
                methodFromProfile = false;
            }
            var areaOk = landAcres is { } a && a >= YieldEstimator.MinAcres && a <= YieldEstimator.MaxAcres;
            return new YieldPrefillDto
            {
                Crop = crop,
                CropFromProfile = profileCrop is not null,
                AreaAcres = areaOk ? Math.Round(landAcres!.Value, 2) : YieldEstimator.DefaultAcres,
                AreaFromProfile = areaOk,
                Method = method,
                MethodFromProfile = methodFromProfile,
                NpkPercent = YieldEstimator.NpkDefault,
                LastFertilizerCrop = lastFertilizerCrop,
                LastFertilizerAtUtc = lastFertilizerAt
            };
        }

        // ------------------------------------------------------------------ predict

        [HttpPost("predict")]
        public async Task<IActionResult> Predict([FromBody] YieldPredictRequest? request, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var norms = await NormsAsync(ct);
            var cropKnown = request is not null && norms.Any(n => string.Equals(n.Crop, request.Crop?.Trim(), StringComparison.OrdinalIgnoreCase));
            var norm = request is null ? null : YieldEstimator.PickNorm(norms, request.Crop, request.Method);
            var errors = YieldEstimator.Validate(request, norm, cropKnown);
            if (errors.Count > 0)
                return BadRequest(new YieldErrorDto { Message = errors.Count == 1 ? errors.Values.First() : "Please check the marked fields.", Errors = errors });

            request!.Method = YieldEstimator.FindMethod(request.Method)!.Code;
            request.AreaAcres = Math.Round(request.AreaAcres, 2);
            var result = YieldEstimator.Estimate(norm!, request);

            var row = new YieldPrediction
            {
                FarmerUserId = userId,
                NormId = norm!.Id,
                Crop = norm.Crop,
                Method = result.Method,
                IrrigationClass = norm.IrrigationClass,
                AreaAcres = request.AreaAcres,
                Ndvi = (decimal)result.Ndvi,
                NdviSource = request.NdviSource,
                RainfallMm = request.RainfallMm,
                NpkPercent = request.NpkPercent,
                ExpectedT = (decimal)result.ExpectedT,
                LowT = (decimal)result.LowT,
                HighT = (decimal)result.HighT,
                ConfidenceLabel = result.ConfidenceLabel,
                ResultJson = "{}",
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.YieldPredictions.Add(row);
            await _db.SaveChangesAsync(ct);

            result.Id = row.Id;
            result.CreatedAtUtc = row.CreatedAtUtc;
            row.ResultJson = JsonSerializer.Serialize(result, StoreJson);
            await _db.SaveChangesAsync(ct);

            _log.LogInformation("Yield forecast {Id}: norm {NormId}, {Acres} acres", row.Id, norm.Id, request.AreaAcres);
            return Ok(result);
        }

        // ------------------------------------------------------------------ history

        [HttpGet("history")]
        public async Task<ActionResult<List<YieldPredictionSummaryDto>>> History(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var rows = await _db.YieldPredictions.AsNoTracking()
                .Where(p => p.FarmerUserId == userId)
                .OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
                .Take(YieldEstimator.HistorySize)
                .Select(p => new YieldPredictionSummaryDto
                {
                    Id = p.Id,
                    CreatedAtUtc = p.CreatedAtUtc,
                    Crop = p.Crop,
                    Method = p.Method,
                    AreaAcres = p.AreaAcres,
                    ExpectedT = p.ExpectedT,
                    LowT = p.LowT,
                    HighT = p.HighT,
                    ConfidenceLabel = p.ConfidenceLabel
                })
                .ToListAsync(ct);
            return Ok(rows);
        }

        [HttpGet("history/{id:long}")]
        public async Task<IActionResult> One(long id, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var json = await _db.YieldPredictions.AsNoTracking()
                .Where(p => p.Id == id && p.FarmerUserId == userId)
                .Select(p => p.ResultJson)
                .FirstOrDefaultAsync(ct);
            var dto = json is null ? null : JsonSerializer.Deserialize<YieldPredictionDto>(json, StoreJson);
            return dto is null ? NotFound(new YieldErrorDto { Message = "This forecast was not found." }) : Ok(dto);
        }
    }
}
