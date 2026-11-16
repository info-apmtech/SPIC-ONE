using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SPIC.Core.Entities;
using SPIC.Core.Help;
using Spic.Infrastructure.Services.Help;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Help Desk API (docs/superpowers/specs/2026-11-16-help-desk-design.md).
    ///
    /// Voice: each guide section's clip is generated ONCE per (text, language) by Azure AI Speech and
    /// saved under Uploads/Help/audio; every later request streams the saved file. The spoken text
    /// always comes from the guide files embedded in SPIC.Core - the client only names
    /// module / section / language, it can never send text to be spoken.
    /// GET audio/... is on the access_token allowlist in Program.cs so an &lt;audio&gt; tag can play it.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HelpController : ControllerBase
    {
        private readonly HelpLibrary _library;
        private readonly HelpAudioCache _audio;
        private readonly HelpSpeechOptions _speech;
        private readonly ILogger<HelpController> _logger;

        public HelpController(
            HelpLibrary library,
            HelpAudioCache audio,
            IOptions<HelpSpeechOptions> speech,
            ILogger<HelpController> logger)
        {
            _library = library;
            _audio = audio;
            _speech = speech.Value;
            _logger = logger;
        }

        /// <summary>Whether the Listen button should show.</summary>
        [HttpGet("audio/status")]
        public IActionResult AudioStatus() => Ok(new { enabled = _speech.Enabled });

        /// <summary>The clip key for a section; the client adds it to the audio URL (?v=key) so
        /// devices cache the clip and re-download only when the text changes.</summary>
        [HttpGet("audio/key/{module}/{sectionId}/{lang}")]
        public IActionResult AudioKey(string module, string sectionId, string lang)
        {
            if (!_speech.Enabled) return NotFound();
            var clip = ClipFor(module, sectionId, lang);
            return clip is null ? NotFound() : Ok(new { key = HelpAudioCache.KeyFor(clip.Value.Ssml) });
        }

        [HttpGet("audio/{module}/{sectionId}/{lang}")]
        public async Task<IActionResult> Audio(string module, string sectionId, string lang, CancellationToken ct)
        {
            if (!_speech.Enabled) return NotFound();
            var clip = ClipFor(module, sectionId, lang);
            if (clip is null) return NotFound();
            var (voiceLang, ssml) = clip.Value;

            string path;
            try
            {
                path = await _audio.GetOrCreateAsync(voiceLang, ssml, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Help voice generation failed for {Module}/{Section}/{Lang}", module, sectionId, lang);
                return StatusCode(503, new { Success = false, Message = "The voice for this guide is not available right now." });
            }

            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            Response.Headers.ETag = $"\"{HelpAudioCache.KeyFor(ssml)}\"";
            return PhysicalFile(path, "audio/mpeg", enableRangeProcessing: true);
        }

        /// <summary>
        /// Generates every missing clip (all modules, sections, languages) in the background. Run once
        /// after a deploy that changed guide text, so nobody waits for a first-time clip. Admin only.
        /// </summary>
        [HttpPost("audio/warm")]
        public IActionResult WarmAudio()
        {
            if (!IsAdmin()) return StatusCode(403, new { Success = false, Message = "Admins only." });
            if (!_speech.Enabled) return BadRequest(new { Success = false, Message = "Help voice is not configured." });

            var jobs = new List<(string Lang, string Ssml)>();
            foreach (var module in _library.Modules)
                foreach (var lang in _library.Config.Languages)
                {
                    var guide = _library.Localized(module, lang.Code);
                    if (guide is null) continue;
                    foreach (var s in guide.Sections)
                    {
                        // An untranslated section is spoken by its English voice, not a mismatched one.
                        var effective = EffectiveLanguage(lang, s);
                        var ssml = HelpSpeech.BuildSsml(HelpMarkdown.ToSpeech(s.Title, s.Body), effective, _library.GlossaryFor(effective.Code));
                        if (_audio.Existing(effective.Code, ssml) is null) jobs.Add((effective.Code, ssml));
                    }
                }

            var distinct = jobs.DistinctBy(j => j.Ssml).ToList();
            _ = Task.Run(async () =>
            {
                foreach (var (lang, ssml) in distinct)
                {
                    try { await _audio.GetOrCreateAsync(lang, ssml, CancellationToken.None); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Help voice warm-up failed for one {Lang} clip", lang); }
                }
                _logger.LogInformation("Help voice warm-up finished: {Count} clips", distinct.Count);
            });

            return Ok(new { queued = distinct.Count });
        }

        /// <summary>The voice language actually used (English for an untranslated section) and the SSML.</summary>
        private (string Lang, string Ssml)? ClipFor(string module, string sectionId, string lang)
        {
            var language = _library.Config.Languages.FirstOrDefault(l => string.Equals(l.Code, lang, StringComparison.OrdinalIgnoreCase));
            if (language is null) return null;
            var section = _library.Localized(module, language.Code)?.Sections
                .FirstOrDefault(s => string.Equals(s.Id, sectionId, StringComparison.OrdinalIgnoreCase));
            if (section is null) return null;

            var effective = EffectiveLanguage(language, section);
            return (effective.Code, HelpSpeech.BuildSsml(HelpMarkdown.ToSpeech(section.Title, section.Body), effective, _library.GlossaryFor(effective.Code)));
        }

        // A non-English view of an untranslated section shows English text, so it uses the English voice.
        private HelpLanguage EffectiveLanguage(HelpLanguage requested, HelpSection section) =>
            section.Confidence == HelpConfidence.Source
                ? _library.Config.Languages.FirstOrDefault(l => l.Code == "en") ?? requested
                : requested;

        private bool IsAdmin()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(role, nameof(AppRole.Admin), StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, nameof(AppRole.SuperAdmin), StringComparison.OrdinalIgnoreCase);
        }
    }
}
