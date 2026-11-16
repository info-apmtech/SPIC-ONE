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
    public async Task<string?> AudioUrlAsync(string module, string sectionId, string lang, CancellationToken ct = default)
    {
        try
        {
            var path = $"audio/{Uri.EscapeDataString(module)}/{Uri.EscapeDataString(sectionId)}/{Uri.EscapeDataString(lang)}";
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

    private sealed record VoiceStatus(bool Enabled);

    private sealed record ClipKey(string Key);
}
