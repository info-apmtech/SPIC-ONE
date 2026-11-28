using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using SpicAPI.Services;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Farmer portal Task 6 (docs/farmer-portal.md): the leaf-photo deficiency check. Farmer role only; every query is
    /// scoped to the caller's user id.
    ///
    ///   GET    api/Farmer/leaf-check/status        today's allowance, crops, whether a full SPIC AI check is possible
    ///   POST   api/Farmer/leaf-check               multipart: photo (max 8 MB, JPEG / PNG / WebP by magic bytes), crop, leafAge
    ///   GET    api/Farmer/leaf-check/history       the caller's latest 30 checks
    ///   GET    api/Farmer/leaf-check/{id}          one check in full
    ///   GET    api/Farmer/leaf-check/{id}/image    the stored photo (owner only; ?thumb=true = 240 px for lists)
    ///   DELETE api/Farmer/leaf-check/{id}          soft delete (the row stays for the daily count; the photo file is removed)
    ///
    /// A check: validate, decode / turn upright / shrink / re-encode (<see cref="LeafImageProcessor"/>, EXIF and GPS
    /// dropped), colour metrics (<see cref="LeafColorAnalyzer"/>), save the photo under PrivateUploads/FarmerLeaf/{userId}/ (a
    /// root no generic file route serves, <see cref="PrivateRoot"/>),
    /// count and store the check under a per-farmer advisory lock (10 per IST day, FarmerPortal:LeafCheckDailyLimit), take
    /// one unit of the shared daily AI budget (<see cref="IFarmerAiBudget"/>, kind LeafCheck), ask Claude vision
    /// (<see cref="LeafVisionService"/>) and, with no key / budget / switch or an unreadable answer, give the labelled
    /// "basic colour check" (<see cref="LeafCheckRules.RuleDiagnosis"/>). Correctives come from NutrientCorrectives.
    /// </summary>
    [Authorize(Roles = nameof(AppRole.Farmer))]
    [ApiController]
    [Route("api/Farmer/leaf-check")]
    public class FarmerLeafCheckController : ControllerBase
    {
        public const string UploadFolder = "FarmerLeaf";
        private const long MaxRequestBytes = LeafCheckRules.MaxPhotoBytes + 256 * 1024;

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly AppDbContext _db;
        private readonly IFarmerAiBudget _budget;
        private readonly LeafVisionService _vision;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<FarmerLeafCheckController> _log;

        public FarmerLeafCheckController(AppDbContext db, IFarmerAiBudget budget, LeafVisionService vision, IWebHostEnvironment env,
            IConfiguration config, ILogger<FarmerLeafCheckController> log)
        {
            _db = db;
            _budget = budget;
            _vision = vision;
            _env = env;
            _config = config;
            _log = log;
        }

        /// <summary>The multipart form of POST api/Farmer/leaf-check.</summary>
        public sealed class LeafCheckForm
        {
            public IFormFile? Photo { get; set; }
            public string? Crop { get; set; }
            public string? LeafAge { get; set; }
        }

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private int DailyLimit => LeafCheckRules.Limit(_config["FarmerPortal:LeafCheckDailyLimit"]);

        /// <summary>
        /// Where leaf photos are kept (fix round 1, review I1): a PRIVATE root outside the shared Uploads tree, which several
        /// generic file routes (api/DealerFile/view, api/Sas/file, api/Community/file, the guest house image route) serve to
        /// any signed-in user. Default ContentRootPath/PrivateUploads; FarmerPortal:LeafPhotoRoot overrides it. In Azure it
        /// must be a persistent volume (the api-private Azure Files share mounted at /app/PrivateUploads, infra/azure/main.bicep).
        /// </summary>
        public static string PrivateRoot(string contentRoot, string? configured) =>
            string.IsNullOrWhiteSpace(configured) ? Path.Combine(contentRoot, "PrivateUploads") : configured.Trim();

        private string PhotoRoot => PrivateRoot(_env.ContentRootPath, _config["FarmerPortal:LeafPhotoRoot"]);

        /// <summary>Checks the farmer made since IST midnight (deleted ones included).</summary>
        private Task<int> UsedTodayAsync(string userId, DateTime utcNow, CancellationToken ct)
        {
            var start = FarmerAssistantRules.IstDayStartUtc(utcNow);
            return _db.LeafDiagnoses.CountAsync(d => d.FarmerUserId == userId && d.CreatedAtUtc >= start, ct);
        }

        // ------------------------------------------------------------------ status

        [HttpGet("status")]
        public async Task<ActionResult<LeafCheckStatusDto>> Status(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var now = DateTime.UtcNow;
            var used = await UsedTodayAsync(userId, now, ct);
            var limit = DailyLimit;
            return new LeafCheckStatusDto
            {
                DailyLimit = limit,
                UsedToday = used,
                LeftToday = Math.Max(0, limit - used),
                ResetsAtUtc = FarmerAssistantRules.NextResetUtc(now),
                FullCheck = await _budget.AvailableAsync(FarmerAiKind.LeafCheck, ct),
                Crops = LeafCheckRules.Crops.ToList(),
                MaxPhotoBytes = LeafCheckRules.MaxPhotoBytes
            };
        }

        // ------------------------------------------------------------------ check

        [HttpPost]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<IActionResult> Check([FromForm] LeafCheckForm form, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var errors = LeafCheckRules.ValidateForm(form.Crop, form.LeafAge);
            var photo = form.Photo;
            if (photo is null || photo.Length == 0) errors["photo"] = LeafCheckRules.ValidateUpload(0, default)!;
            else if (photo.Length > LeafCheckRules.MaxPhotoBytes) errors["photo"] = LeafCheckRules.ValidateUpload(photo.Length, default)!;
            if (errors.Count > 0) return Problem400(errors);

            var crop = LeafCheckRules.NormaliseCrop(form.Crop)!;
            var leafAge = LeafCheckRules.NormaliseLeafAge(form.LeafAge);
            var limit = DailyLimit;
            var now = DateTime.UtcNow;

            // A capped farmer gets the 429 before the photo is decoded; the locked count below stays the authority.
            if (await UsedTodayAsync(userId, now, ct) >= limit) return Capped(limit, now);

            // Read at most 8 MB (the multipart length was checked above) and trust only the first bytes, never the
            // content type the client claims.
            byte[] bytes;
            using (var ms = new MemoryStream((int)photo!.Length))
            {
                await using var input = photo.OpenReadStream();
                await input.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
            }
            var uploadProblem = LeafCheckRules.ValidateUpload(bytes.Length, bytes.AsSpan(0, Math.Min(bytes.Length, 16)));
            if (uploadProblem is not null) return Problem400(new() { ["photo"] = uploadProblem });

            var (image, decodeError) = await LeafImageProcessor.ProcessAsync(bytes, ct);
            if (image is null) return Problem400(new() { ["photo"] = decodeError ?? "We could not read this photo." });
            var metrics = LeafColorAnalyzer.Analyze(image.MetricsRgb, image.MetricsWidth, image.MetricsHeight);

            // The photo is written first (a new random name), then counted and stored under the per-farmer lock;
            // when the cap was reached meanwhile the file is removed again.
            var relative = $"{UploadFolder}/{SafeSegment(userId)}/{Guid.NewGuid():N}.jpg";
            var fullPath = FullPath(relative)!;
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await System.IO.File.WriteAllBytesAsync(fullPath, image.Jpeg, ct);

            LeafDiagnosis row;
            var npgsql = _db.Database.IsNpgsql();
            try
            {
                await using var tx = npgsql ? await _db.Database.BeginTransactionAsync(ct) : null;
                if (npgsql)
                    await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"fleaf:" + userId}, 0))", ct);
                var used = await UsedTodayAsync(userId, now, ct);
                if (used >= limit)
                {
                    TryDelete(fullPath);
                    _log.LogInformation("Leaf check daily cap reached ({Used}/{Limit}).", used, limit);
                    return Capped(limit, now);
                }
                row = new LeafDiagnosis
                {
                    FarmerUserId = userId,
                    Crop = crop,
                    LeafAgeHint = leafAge,
                    ImagePath = relative,
                    ImageWidth = image.Width,
                    ImageHeight = image.Height,
                    ImageBytes = image.Jpeg.Length,
                    MetricsJson = JsonSerializer.Serialize(metrics, Json),
                    CreatedAtUtc = now
                };
                _db.LeafDiagnoses.Add(row);
                await _db.SaveChangesAsync(ct);
                if (tx is not null) await tx.CommitAsync(ct);
            }
            catch
            {
                TryDelete(fullPath);
                throw;
            }

            // From here the check is stored and counted: it is finished and saved even if the farmer leaves the page.
            string provider = "basic";
            string? reason = null;
            LeafDiagnosisResult? result = null;
            if (metrics.LeafShare < LeafColorAnalyzer.NoLeafShare)
            {
                // Nothing plant-coloured in the photo: no AI call (and no budget) for it.
                reason = "not-a-leaf";
            }
            else
            {
                var decision = await _budget.TryConsumeAsync(FarmerAiKind.LeafCheck, userId, CancellationToken.None);
                if (decision == FarmerAiDecision.Allowed)
                {
                    var language = await _db.FarmerProfiles.AsNoTracking().Where(p => p.UserId == userId)
                        .Select(p => p.PreferredLanguage).FirstOrDefaultAsync(CancellationToken.None);
                    var outcome = await _vision.DiagnoseAsync(image.Jpeg, crop, leafAge, metrics,
                        FarmerAssistantRules.LanguageName(FarmerAssistantRules.LanguageCode(language)), CancellationToken.None);
                    if (outcome.Result is { } ai)
                    {
                        result = ai;
                        provider = "ai";
                    }
                    else reason = "ai-failed";
                }
                else reason = LeafCheckRules.BasicReasonFor(decision);
            }
            result ??= LeafCheckRules.RuleDiagnosis(metrics, crop, leafAge);

            var correctives = result.NotALeaf || result.Condition == "healthy" || result.Nutrients.Count == 0
                ? new List<NutrientCorrective>()
                : LeafCheckRules.SelectCorrectives(
                    await _db.NutrientCorrectives.AsNoTracking().Where(c => c.IsActive).ToListAsync(CancellationToken.None),
                    crop, result.Nutrients);

            var dto = Build(row, result, metrics, provider, reason, correctives);
            row.ResultJson = JsonSerializer.Serialize(dto, Json);
            row.Provider = provider;
            row.BasicReason = reason;
            row.Condition = dto.Condition;
            row.TopNutrient = dto.Nutrients.FirstOrDefault()?.Code;
            row.Confidence = dto.Confidence;
            await _db.SaveChangesAsync(CancellationToken.None);

            _log.LogInformation("Leaf check {Id}: {Provider} ({Reason}), condition {Condition}, pattern {Pattern}, LCC {Lcc}.",
                row.Id, provider, reason ?? "-", dto.Condition, metrics.Pattern, metrics.LccScore);
            return Ok(dto);
        }

        // ------------------------------------------------------------------ history / one / image / delete

        [HttpGet("history")]
        public async Task<ActionResult<List<LeafCheckSummaryDto>>> History(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var rows = await _db.LeafDiagnoses.AsNoTracking()
                .Where(d => d.FarmerUserId == userId && !d.IsDeleted && d.ResultJson != null)
                .OrderByDescending(d => d.CreatedAtUtc).ThenByDescending(d => d.Id)
                .Take(LeafCheckRules.HistoryLimit)
                .Select(d => new { d.Id, d.Crop, d.CreatedAtUtc, d.Condition, d.TopNutrient, d.Confidence, d.Provider, d.MetricsJson, d.ImagePath, d.ResultJson })
                .ToListAsync(ct);
            return rows.Select(d =>
            {
                var notALeaf = Read<LeafCheckResultDto>(d.ResultJson)?.NotALeaf ?? false;
                return new LeafCheckSummaryDto
                {
                    Id = d.Id,
                    Crop = d.Crop,
                    CreatedAtUtc = Utc(d.CreatedAtUtc),
                    Condition = d.Condition ?? "unclear",
                    ConditionLabel = LeafCheckRules.ConditionLabel(d.Condition, notALeaf),
                    NotALeaf = notALeaf,
                    TopNutrient = d.TopNutrient,
                    TopNutrientName = d.TopNutrient is null ? null : LeafCheckRules.NutrientName(d.TopNutrient),
                    Confidence = d.Confidence ?? 0,
                    Provider = d.Provider ?? "basic",
                    LccScore = Read<LeafMetrics>(d.MetricsJson)?.LccScore ?? 0,
                    HasImage = d.ImagePath is not null
                };
            }).ToList();
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> One(long id, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var row = await _db.LeafDiagnoses.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && d.FarmerUserId == userId && !d.IsDeleted && d.ResultJson != null, ct);
            var dto = row is null ? null : Read<LeafCheckResultDto>(row.ResultJson);
            if (row is null || dto is null) return NotFound(new LeafCheckErrorDto { Message = "This leaf check was not found. It may have been deleted." });
            dto.Id = row.Id;
            dto.CreatedAtUtc = Utc(row.CreatedAtUtc);
            dto.HasImage = row.ImagePath is not null;
            return Ok(dto);
        }

        [HttpGet("{id:long}/image")]
        public async Task<IActionResult> Image(long id, [FromQuery] bool thumb, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var path = await _db.LeafDiagnoses.AsNoTracking()
                .Where(d => d.Id == id && d.FarmerUserId == userId && !d.IsDeleted)
                .Select(d => d.ImagePath)
                .FirstOrDefaultAsync(ct);
            var full = FullPath(path);
            if (full is null || !System.IO.File.Exists(full))
                return NotFound(new LeafCheckErrorDto { Message = "This photo was not found." });
            Response.Headers.CacheControl = "private, max-age=3600";
            if (thumb)
            {
                var small = LeafImageProcessor.Thumbnail(await System.IO.File.ReadAllBytesAsync(full, ct));
                if (small is not null) return File(small, "image/jpeg");
            }
            return PhysicalFile(full, "image/jpeg");
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var row = await _db.LeafDiagnoses.FirstOrDefaultAsync(d => d.Id == id && d.FarmerUserId == userId && !d.IsDeleted, ct);
            if (row is null) return NotFound(new LeafCheckErrorDto { Message = "This leaf check was not found. It may have been deleted." });
            var full = FullPath(row.ImagePath);
            row.IsDeleted = true;
            row.DeletedAtUtc = DateTime.UtcNow;
            row.ImagePath = null;
            await _db.SaveChangesAsync(ct);
            if (full is not null) TryDelete(full);
            return NoContent();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>The full result (public for the harness).</summary>
        public static LeafCheckResultDto Build(LeafDiagnosis row, LeafDiagnosisResult r, LeafMetrics metrics, string provider, string? reason,
            IReadOnlyList<NutrientCorrective> correctives)
        {
            var basic = provider != "ai";
            return new LeafCheckResultDto
            {
                Id = row.Id,
                Crop = row.Crop,
                LeafAgeHint = row.LeafAgeHint,
                CreatedAtUtc = Utc(row.CreatedAtUtc),
                Provider = provider,
                BasicReason = basic ? reason : null,
                BasicLabel = basic ? LeafCheckRules.BasicLabel : null,
                Notice = basic ? LeafCheckRules.BasicNotice(reason) : null,
                Condition = r.Condition,
                ConditionLabel = LeafCheckRules.ConditionLabel(r.Condition, r.NotALeaf),
                Confidence = r.Confidence,
                NotALeaf = r.NotALeaf,
                Nutrients = r.Nutrients.Select(n => new LeafNutrientDto
                {
                    Code = n.Code,
                    Name = LeafCheckRules.NutrientName(n.Code),
                    Likelihood = n.Likelihood,
                    LikelihoodLabel = LeafCheckRules.LikelihoodLabel(n.Likelihood)
                }).ToList(),
                Symptoms = r.Symptoms,
                LeafAge = r.LeafAge,
                Advice = r.Advice,
                NeedsSoilTest = r.NeedsSoilTest,
                Metrics = metrics,
                PatternLabel = LeafCheckRules.PatternLabel(metrics.Pattern),
                LccLabel = LeafCheckRules.LccLabel(metrics.LccScore),
                Correctives = correctives.Select(c => new LeafCorrectiveDto
                {
                    Nutrient = c.Nutrient,
                    NutrientName = LeafCheckRules.NutrientName(c.Nutrient),
                    Product = c.Product,
                    Method = c.Method,
                    Dose = c.Dose,
                    Note = c.Note,
                    Source = c.Source,
                    SourceUrl = c.SourceUrl,
                    ForCrop = c.Crop is not null
                }).ToList(),
                Disclaimer = basic ? LeafCheckRules.BasicDisclaimer : LeafCheckRules.Disclaimer,
                HasImage = row.ImagePath is not null
            };
        }

        /// <summary>A user id as a folder name: letters, digits and '-' only (Identity ids are GUIDs).</summary>
        public static string SafeSegment(string userId) =>
            new string(userId.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-').ToArray()) is { Length: > 0 } s ? s : "unknown";

        /// <summary>The file for a stored relative path, or null when it is empty or would leave the private photo root.</summary>
        private string? FullPath(string? relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || relative.Contains("..", StringComparison.Ordinal)) return null;
            var root = PhotoRoot;
            var full = Path.GetFullPath(Path.Combine(root, relative.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar)));
            var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return full.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) ? full : null;
        }

        private void TryDelete(string fullPath)
        {
            try { if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath); }
            catch (Exception ex) { _log.LogWarning(ex, "Leaf check photo could not be removed."); }
        }

        private BadRequestObjectResult Problem400(Dictionary<string, string> errors) =>
            BadRequest(new LeafCheckErrorDto
            {
                Message = errors.Count == 1 ? errors.Values.First() : "Please check the marked fields.",
                Errors = errors
            });

        private IActionResult Capped(int limit, DateTime now)
        {
            Response.Headers.RetryAfter = FarmerAssistantRules.SecondsUntilReset(now).ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new LeafCheckErrorDto
            {
                Message = LeafCheckRules.CapMessage(limit),
                RetryAfterSeconds = FarmerAssistantRules.SecondsUntilReset(now),
                ResetsAtUtc = FarmerAssistantRules.NextResetUtc(now),
                DailyLimit = limit
            });
        }

        /// <summary>Timestamps are stored as UTC without a kind: send them with "Z".</summary>
        private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private static T? Read<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<T>(json, Json); }
            catch (JsonException) { return null; }
        }
    }
}
