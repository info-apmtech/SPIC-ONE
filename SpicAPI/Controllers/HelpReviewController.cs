using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Help;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Help Desk translation review (spec §6.9). Native speakers approve or correct machine-translated
    /// guide sections and report bad pronunciation; the guide FILES stay the source of truth.
    ///
    /// Who: Admin / SuperAdmin, or a designation granting the HelpTranslationReview page. A reviewer's
    /// languages come from HelpReviewers (empty = all). Status is computed from the current files
    /// (HelpReviewStatus) and never stored, so an English or translation change resets it.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/Help/review")]
    public class HelpReviewController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly HelpLibrary _library;

        public HelpReviewController(AppDbContext db, HelpLibrary library)
        {
            _db = db;
            _library = library;
        }

        // =====================================================================
        //  Read: approvals (every signed-in user) and the reviewer's list
        // =====================================================================

        /// <summary>"module/sectionId" of every currently approved section in a language - the Help
        /// pages hide the "Machine translated" note for these.</summary>
        [HttpGet("approved/{lang}")]
        public async Task<IActionResult> Approved(string lang)
        {
            if (!IsTranslatedLanguage(lang)) return Ok(Array.Empty<string>());
            var items = await ItemsFor(lang);
            var keys = new List<string>();
            foreach (var (module, en, tr) in TranslatedSections(lang))
                if (HelpReviewStatus.StateOf(en, tr, ItemsOf(items, module, tr.Id)) == HelpReviewState.Approved)
                    keys.Add($"{module}/{tr.Id}");
            return Ok(keys);
        }

        [HttpGet("access")]
        public async Task<IActionResult> Access()
        {
            var (isReviewer, isAdmin, languages) = await ReviewerScope();
            return Ok(new HelpReviewAccessDto(isReviewer, isAdmin, languages));
        }

        [HttpGet("{lang}")]
        public async Task<IActionResult> List(string lang)
        {
            if (!IsTranslatedLanguage(lang)) return NotFound();
            if (!await MayReview(lang)) return Forbidden();

            var items = await ItemsFor(lang);
            var rows = TranslatedSections(lang)
                .Select(x => new HelpReviewRowDto(
                    x.Module, x.Translated.Id, x.Translated.Title,
                    HelpReviewStatus.StateOf(x.English, x.Translated, ItemsOf(items, x.Module, x.Translated.Id)),
                    x.Translated.Confidence == HelpConfidence.Low ? "low" : "ok"))
                .OrderBy(r => r.State == HelpReviewState.Approved ? 1 : 0)
                .ThenBy(r => r.Confidence == "low" ? 0 : 1)
                .ToList();
            return Ok(rows);
        }

        // =====================================================================
        //  Write: approve / correct / report pronunciation
        // =====================================================================

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] HelpReviewSubmitDto dto)
        {
            if (dto is null || !IsTranslatedLanguage(dto.Language)) return BadRequest(Fail("Unknown language."));
            if (!await MayReview(dto.Language)) return Forbidden();

            var match = TranslatedSections(dto.Language)
                .FirstOrDefault(x => x.Module.Equals(dto.Module, StringComparison.OrdinalIgnoreCase)
                                     && x.Translated.Id.Equals(dto.SectionId, StringComparison.OrdinalIgnoreCase));
            if (match.Translated is null) return BadRequest(Fail("This section has no current translation to review."));

            switch (dto.Kind)
            {
                case HelpReviewKind.Correction when string.IsNullOrWhiteSpace(dto.SuggestedBody):
                    return BadRequest(Fail("Enter the corrected text."));
                case HelpReviewKind.Pronunciation when string.IsNullOrWhiteSpace(dto.Word) || string.IsNullOrWhiteSpace(dto.SayAs):
                    return BadRequest(Fail("Enter the word and how it should be said."));
            }

            _db.HelpReviewItems.Add(new HelpReviewItem
            {
                Module = match.Module,
                SectionId = match.Translated.Id,
                Language = dto.Language.ToLowerInvariant(),
                Kind = dto.Kind,
                // Hashes are always taken from the files on the server, never from the client.
                SourceHash = HelpHash.Of(match.English.Title, match.English.Body),
                TranslationHash = HelpHash.Of(match.Translated.Title, match.Translated.Body),
                SuggestedTitle = dto.Kind == HelpReviewKind.Correction ? Trim(dto.SuggestedTitle, 300) ?? match.Translated.Title : null,
                SuggestedBody = dto.Kind == HelpReviewKind.Correction ? dto.SuggestedBody!.Trim() : null,
                Comment = Trim(dto.Comment, 1000),
                Word = dto.Kind == HelpReviewKind.Pronunciation ? Trim(dto.Word, 120) : null,
                SayAs = dto.Kind == HelpReviewKind.Pronunciation ? Trim(dto.SayAs, 200) : null,
                ReviewerUserId = CurrentUserId ?? string.Empty,
                ReviewerName = User.Identity?.Name ?? "reviewer",
                CreatedAtUtc = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Open corrections (against the current English, not yet in the file) and pronunciation
        /// reports, for <c>help-tools apply-reviews</c>.</summary>
        [HttpGet("export/{lang}")]
        public async Task<IActionResult> Export(string lang)
        {
            if (!IsTranslatedLanguage(lang)) return NotFound();
            if (!await MayReview(lang)) return Forbidden();

            var items = await ItemsFor(lang);
            var corrections = new List<HelpCorrectionExportDto>();
            foreach (var (module, en, tr) in TranslatedSections(lang))
            {
                var source = HelpHash.Of(en.Title, en.Body);
                corrections.AddRange(ItemsOf(items, module, tr.Id)
                    .Where(i => i.Kind == HelpReviewKind.Correction && i.SourceHash == source && !HelpReviewStatus.IsApplied(i, tr))
                    .Select(i => new HelpCorrectionExportDto(module, tr.Id, i.Language, i.SourceHash, i.SuggestedTitle ?? tr.Title, i.SuggestedBody ?? string.Empty, i.ReviewerName, i.CreatedAtUtc)));
            }
            var pronunciations = items
                .Where(i => i.Kind == HelpReviewKind.Pronunciation)
                .OrderByDescending(i => i.CreatedAtUtc)
                .Select(i => new HelpPronunciationExportDto(i.Language, i.Word ?? string.Empty, i.SayAs ?? string.Empty, i.Comment, i.ReviewerName))
                .ToList();
            return Ok(new HelpReviewExportDto(corrections, pronunciations));
        }

        // =====================================================================
        //  Admin: who reviews which languages
        // =====================================================================

        [HttpGet("reviewers")]
        public async Task<IActionResult> Reviewers()
        {
            if (!IsAdmin()) return Forbidden();

            var designations = await _db.Designations.AsNoTracking()
                .Where(d => d.IsActive)
                .Select(d => new { d.Id, d.Name, d.RoleAccess })
                .ToListAsync();
            var grantIds = designations
                .Where(d => RoleAccessPermissions.HasPage(d.RoleAccess, PagePermission.HelpTranslationReview))
                .ToDictionary(d => d.Id, d => d.Name);

            var users = await _db.Users.AsNoTracking()
                .Where(u => u.DesignationId.HasValue && grantIds.Keys.Contains(u.DesignationId.Value))
                .Select(u => new { u.Id, u.Name, u.UserName, u.DesignationId })
                .ToListAsync();
            var ids = users.Select(u => u.Id).ToList();
            var scopes = await _db.HelpReviewers.AsNoTracking().Where(r => ids.Contains(r.UserId)).ToDictionaryAsync(r => r.UserId, r => r.Languages);

            return Ok(users
                .OrderBy(u => u.Name ?? u.UserName)
                .Select(u => new HelpReviewerDto(u.Id, u.Name ?? u.UserName ?? u.Id, grantIds[u.DesignationId!.Value], SplitLanguages(scopes.GetValueOrDefault(u.Id))))
                .ToList());
        }

        [HttpPut("reviewers/{userId}")]
        public async Task<IActionResult> SetReviewer(string userId, [FromBody] HelpReviewerUpdateDto dto)
        {
            if (!IsAdmin()) return Forbidden();
            if (!await _db.Users.AnyAsync(u => u.Id == userId)) return NotFound();

            var languages = (dto?.Languages ?? Array.Empty<string>())
                .Where(IsTranslatedLanguage)
                .Select(l => l.ToLowerInvariant())
                .Distinct()
                .ToList();
            var row = await _db.HelpReviewers.FindAsync(userId);
            if (row is null) _db.HelpReviewers.Add(row = new HelpReviewer { UserId = userId });
            row.Languages = string.Join(",", languages);
            row.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool IsAdmin()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(role, nameof(AppRole.Admin), StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, nameof(AppRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsTranslatedLanguage(string? lang) =>
            !string.IsNullOrWhiteSpace(lang)
            && !lang.Equals("en", StringComparison.OrdinalIgnoreCase)
            && _library.Config.Languages.Any(l => l.Code.Equals(lang, StringComparison.OrdinalIgnoreCase));

        private async Task<(bool IsReviewer, bool IsAdmin, IReadOnlyList<string> Languages)> ReviewerScope()
        {
            var all = _library.Config.Languages.Select(l => l.Code).Where(c => c != "en").ToList();
            if (IsAdmin()) return (true, true, all);

            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return (false, false, Array.Empty<string>());
            var designationId = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.DesignationId).FirstOrDefaultAsync();
            if (!designationId.HasValue) return (false, false, Array.Empty<string>());
            var roleAccess = await _db.Designations.AsNoTracking()
                .Where(d => d.Id == designationId.Value && d.IsActive)
                .Select(d => d.RoleAccess)
                .FirstOrDefaultAsync();
            if (!RoleAccessPermissions.HasPage(roleAccess, PagePermission.HelpTranslationReview)) return (false, false, Array.Empty<string>());

            var scope = await _db.HelpReviewers.AsNoTracking().Where(r => r.UserId == userId).Select(r => r.Languages).FirstOrDefaultAsync();
            var languages = SplitLanguages(scope);
            return (true, false, languages.Count == 0 ? all : languages.Where(all.Contains).ToList());
        }

        private async Task<bool> MayReview(string lang)
        {
            var (isReviewer, _, languages) = await ReviewerScope();
            return isReviewer && languages.Contains(lang.ToLowerInvariant());
        }

        private Task<List<HelpReviewItem>> ItemsFor(string lang)
        {
            var code = lang.ToLowerInvariant();
            return _db.HelpReviewItems.AsNoTracking().Where(i => i.Language == code).ToListAsync();
        }

        private static IEnumerable<HelpReviewItem> ItemsOf(List<HelpReviewItem> items, string module, string sectionId) =>
            items.Where(i => i.Module.Equals(module, StringComparison.OrdinalIgnoreCase)
                             && i.SectionId.Equals(sectionId, StringComparison.OrdinalIgnoreCase));

        /// <summary>Sections that currently HAVE a translation (an English fallback is not reviewable).</summary>
        private IEnumerable<(string Module, HelpSection English, HelpSection Translated)> TranslatedSections(string lang)
        {
            foreach (var module in _library.Modules)
            {
                var en = _library.English(module)!;
                var localized = _library.Localized(module, lang);
                if (localized is null) continue;
                foreach (var tr in localized.Sections.Where(s => s.Confidence != HelpConfidence.Source))
                {
                    var source = en.Sections.First(s => s.Id.Equals(tr.Id, StringComparison.OrdinalIgnoreCase));
                    yield return (module, source, tr);
                }
            }
        }

        private static IReadOnlyList<string> SplitLanguages(string? csv) =>
            (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.ToLowerInvariant()).ToList();

        private static string? Trim(string? s, int max) =>
            string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length <= max ? s.Trim() : s.Trim()[..max]);

        private static object Fail(string message) => new { Success = false, Message = message };

        private ObjectResult Forbidden() =>
            StatusCode(403, new { Success = false, Message = "You don't have Help Desk translation review access for this language." });
    }
}
