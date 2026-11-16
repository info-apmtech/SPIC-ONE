using System.Net.Http.Json;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Help Desk API client (api/Help). Voice calls are QUIET: a missing or failing voice only hides
/// the Listen button, it never raises an error toast.
/// </summary>
public sealed class HelpApi : ApiClientBase
{
    private bool? _voiceEnabled;

    public HelpApi(HttpClient http, LoginState login, ToastService toast) : base(http, login, toast, "api/Help") { }

    /// <summary>Whether the API has a Speech key (cached for the session once known).</summary>
    public async Task<bool> VoiceEnabledAsync(CancellationToken ct = default)
    {
        if (_voiceEnabled is bool known) return known;
        if (!Login.IsLoggedIn) return false;
        try
        {
            var response = await Http.GetAsync(Url("audio/status"), ct);
            if (!response.IsSuccessStatusCode) return false;
            var status = await response.Content.ReadFromJsonAsync<VoiceStatus>(Json, ct);
            _voiceEnabled = status?.Enabled ?? false;
            return _voiceEnabled.Value;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Absolute URL of a section's voice clip, carrying the clip key (?v=) so the browser / app
    /// caches it until the text changes, and the access token so an &lt;audio&gt; tag can play it.
    /// Null when the voice is unavailable.
    /// </summary>
    public async Task<string?> AudioUrlAsync(string module, string sectionId, string lang, CancellationToken ct = default) =>
        await AudioUrlAsync(module, sectionId, lang, null, ct);

    /// <summary>As above; with <paramref name="step"/> the clip of that one tour step.</summary>
    public async Task<string?> AudioUrlAsync(string module, string sectionId, string lang, int? step, CancellationToken ct = default)
    {
        try
        {
            var path = $"audio/{Uri.EscapeDataString(module)}/{Uri.EscapeDataString(sectionId)}/{Uri.EscapeDataString(lang)}";
            if (step is { } n) path += $"/step/{n}";
            var response = await Http.GetAsync(Url("audio/key/" + path["audio/".Length..]), ct);
            if (!response.IsSuccessStatusCode) return null;
            var key = (await response.Content.ReadFromJsonAsync<ClipKey>(Json, ct))?.Key;
            if (string.IsNullOrWhiteSpace(key)) return null;

            var baseUrl = Http.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
            var token = Login.Token ?? string.Empty;
            if (token.StartsWith("Bearer ", StringComparison.Ordinal)) token = token["Bearer ".Length..];
            return $"{baseUrl}/{Url(path)}?v={Uri.EscapeDataString(key)}&access_token={Uri.EscapeDataString(token)}";
        }
        catch
        {
            return null;
        }
    }

    // ------------------------------------------------------------------ review

    private readonly Dictionary<string, IReadOnlySet<string>> _approved = new(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> NoneApproved = new HashSet<string>();

    /// <summary>Approval keys (module/sectionId/translationHash) for a language, from the cache (empty
    /// until loaded). Plugged into HelpCatalog.ApprovedKeys.</summary>
    public IReadOnlySet<string> Approved(string lang) =>
        _approved.TryGetValue(lang, out var set) ? set : NoneApproved;

    /// <summary>Loads the approved keys for a language once per session (quietly). True when newly loaded.</summary>
    public async Task<bool> EnsureApprovedAsync(string lang, CancellationToken ct = default)
    {
        if (string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) || _approved.ContainsKey(lang) || !Login.IsLoggedIn) return false;
        try
        {
            var response = await Http.GetAsync(Url($"review/approved/{Uri.EscapeDataString(lang)}"), ct);
            if (!response.IsSuccessStatusCode) return false;
            var keys = await response.Content.ReadFromJsonAsync<List<string>>(Json, ct) ?? new List<string>();
            _approved[lang] = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<SPIC.Core.DTOs.HelpReviewAccessDto?> ReviewAccessAsync(CancellationToken ct = default) =>
        GetAsync<SPIC.Core.DTOs.HelpReviewAccessDto>("review/access", "review access", ct);

    public Task<List<SPIC.Core.DTOs.HelpReviewRowDto>?> ReviewListAsync(string lang, CancellationToken ct = default) =>
        GetAsync<List<SPIC.Core.DTOs.HelpReviewRowDto>>($"review/{Uri.EscapeDataString(lang)}", "translations to review", ct);

    public async Task<bool> SubmitReviewAsync(SPIC.Core.DTOs.HelpReviewSubmitDto dto, CancellationToken ct = default)
    {
        var ok = await SendAsync(HttpMethod.Post, "review", dto, "save the review", ct);
        if (ok) _approved.Remove(dto.Language); // approvals changed: reload next time
        return ok;
    }

    public Task<SPIC.Core.DTOs.HelpReviewExportDto?> ReviewExportAsync(string lang, CancellationToken ct = default) =>
        GetAsync<SPIC.Core.DTOs.HelpReviewExportDto>($"review/export/{Uri.EscapeDataString(lang)}", "corrections", ct);

    public Task<List<SPIC.Core.DTOs.HelpReviewerDto>?> ReviewersAsync(CancellationToken ct = default) =>
        GetAsync<List<SPIC.Core.DTOs.HelpReviewerDto>>("review/reviewers", "reviewers", ct);

    public Task<bool> SetReviewerLanguagesAsync(string userId, IReadOnlyList<string> languages, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"review/reviewers/{Uri.EscapeDataString(userId)}", new SPIC.Core.DTOs.HelpReviewerUpdateDto(languages), "save the reviewer's languages", ct);

    private sealed record VoiceStatus(bool Enabled);

    private sealed record ClipKey(string Key);
}
