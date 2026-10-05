using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// The ONLY HTTP surface of the JMDO Tracking module (<c>api/JMDOTracking/...</c>; route list in
/// SPIC.Core/DTOs/JMDOTrackingDtos.cs). Mirrors JMDOAttendanceApi's conventions: every call is
/// defensive - a network problem or a non-2xx answer returns null/false, so the page never has to
/// catch anything.
///
/// Registered with <c>services.AddScoped&lt;JMDOTrackingApi&gt;()</c> in both hosts
/// (SPIC.MauiBlazorApp.Web/Program.cs and SPIC.MauiBlazorApp/MauiProgram.cs).
/// </summary>
public sealed class JMDOTrackingApi
{
    private const string Root = "api/JMDOTracking";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ToastService _toast;

    public JMDOTrackingApi(HttpClient http, ToastService toast)
    {
        _http = http;
        _toast = toast;
    }

    /// <summary>
    /// Starts a new Active tracking session for the caller, or returns their existing Active one
    /// (the server is idempotent here, so this also doubles as "resume the in-progress session").
    /// </summary>
    public async Task<JMDOTrackingSessionDto?> StartAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            var dto = new JMDOTrackingStartDto { Latitude = latitude, Longitude = longitude };
            var response = await _http.PostAsJsonAsync($"{Root}/start", dto, Json, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<JMDOTrackingSessionDto>(Json, ct);

            var message = await ReadErrorAsync(response, ct);
            _toast.Error(message ?? "Could not start tracking.");
            return null;
        }
        catch (Exception)
        {
            _toast.Error("Could not reach the server to start tracking.");
            return null;
        }
    }

    /// <summary>
    /// Saves one GPS fix against the given session. Returns false on failure without raising a
    /// toast - a momentary network blip on a periodic background call shouldn't interrupt the
    /// field user every interval.
    /// </summary>
    public async Task<bool> AddPointAsync(int sessionId, double latitude, double longitude, double? accuracy, CancellationToken ct = default)
    {
        try
        {
            var dto = new JMDOTrackingPointUpsertDto { Latitude = latitude, Longitude = longitude, Accuracy = accuracy };
            var response = await _http.PostAsJsonAsync($"{Root}/sessions/{sessionId}/points", dto, Json, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Completes the caller's own Active session: saves one final GPS point, then sets
    /// EndTime/Duration/Status=Completed server-side. Unlike <see cref="AddPointAsync"/> this is a
    /// deliberate user action, so a failure raises a toast - the session stays Active either way.
    /// </summary>
    public async Task<JMDOTrackingSessionDto?> StopAsync(double latitude, double longitude, double? accuracy = null, CancellationToken ct = default)
    {
        try
        {
            var dto = new JMDOTrackingStopDto { Latitude = latitude, Longitude = longitude, Accuracy = accuracy };
            var response = await _http.PostAsJsonAsync($"{Root}/stop", dto, Json, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<JMDOTrackingSessionDto>(Json, ct);

            var message = await ReadErrorAsync(response, ct);
            _toast.Error(message ?? "Could not stop tracking.");
            return null;
        }
        catch (Exception)
        {
            _toast.Error("Could not reach the server to stop tracking.");
            return null;
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body)) return null;

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("Message", out var m) ? m.GetString()
                : doc.RootElement.TryGetProperty("message", out var m2) ? m2.GetString()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
