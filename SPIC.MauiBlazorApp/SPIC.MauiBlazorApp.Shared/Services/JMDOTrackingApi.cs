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
            {
                var session = await response.Content.ReadFromJsonAsync<JMDOTrackingSessionDto>(Json, ct);
                SyncInterval(session);
                return session;
            }

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
    /// The caller's current Active session (with its computed LiveStatus), or null if they don't
    /// have one. Read-only - unlike <see cref="StartAsync"/> this never creates a session, so it's
    /// safe to call on every page load just to reflect real state. Silent on failure (null), same
    /// reasoning as <see cref="AddPointAsync"/> - a page falling back to "no active session" on a
    /// transient error is better than a toast on every load.
    /// </summary>
    public async Task<JMDOTrackingSessionDto?> GetMineActiveAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/mine/active", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
                return null;
            var session = await response.Content.ReadFromJsonAsync<JMDOTrackingSessionDto>(Json, ct);
            SyncInterval(session);
            return session;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// The caller's most recent session today, regardless of status - unlike
    /// <see cref="GetMineActiveAsync"/> this also returns an already-Completed/AutoEnded session, so
    /// a page reached right after Stop can still show the route that was just recorded.
    /// </summary>
    public async Task<JMDOTrackingSessionDto?> GetMineLatestAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/mine/latest", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || !response.IsSuccessStatusCode)
                return null;
            var session = await response.Content.ReadFromJsonAsync<JMDOTrackingSessionDto>(Json, ct);
            SyncInterval(session);
            return session;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void SyncInterval(JMDOTrackingSessionDto? session)
    {
        if (session?.IntervalSeconds is >= GpsTrackingConfiguration.MinIntervalSeconds and <= GpsTrackingConfiguration.MaxIntervalSeconds)
        {
            GpsTrackingConfiguration.IntervalSeconds = session.IntervalSeconds.Value;
        }
    }

    /// <summary>
    /// Every one of the caller's own sessions that started today, oldest first - a JMDO can
    /// Start/Stop Live Tracking more than once a day (separate site visits), and each one is its own
    /// session/route. Silent on failure (empty list), same reasoning as <see cref="GetMineActiveAsync"/>.
    /// </summary>
    public async Task<List<JMDOTrackingSessionDto>> GetMineTodayAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<JMDOTrackingSessionDto>>($"{Root}/mine/today", Json, ct);
            return result ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    /// <summary>
    /// Supervisor visibility (Admin/MO/MDO only): every JMDO on the caller's own HQ team with their
    /// current tracking status. Silent on failure (empty list) - same reasoning as
    /// <see cref="GetMineActiveAsync"/>.
    /// </summary>
    public async Task<List<JMDOTrackingTeamRowDto>> GetTeamAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<JMDOTrackingTeamRowDto>>($"{Root}/team", Json, ct);
            return result ?? new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    /// <summary>
    /// Flushes a batch of offline-queued points (capped at 500 server-side) against the given
    /// session in one call, preserving each point's own DeviceCapturedAt. Returns false on failure
    /// without raising a toast, same reasoning as <see cref="AddPointAsync"/> - the caller (the
    /// offline queue drain loop) is expected to retry later, not interrupt the user.
    /// </summary>
    public async Task<bool> AddPointsBatchAsync(int sessionId, IReadOnlyList<JMDOTrackingPointUpsertDto> points, CancellationToken ct = default)
    {
        if (points.Count == 0) return true;
        try
        {
            var response = await _http.PostAsJsonAsync($"{Root}/sessions/{sessionId}/points/batch", points, Json, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
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
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = await response.Content.ReadFromJsonAsync<GpsPointResponseDto>(Json, ct);
                    if (data?.IntervalSeconds is >= GpsTrackingConfiguration.MinIntervalSeconds and <= GpsTrackingConfiguration.MaxIntervalSeconds)
                    {
                        GpsTrackingConfiguration.IntervalSeconds = data.IntervalSeconds.Value;
                    }
                }
                catch { }
                return true;
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private sealed class GpsPointResponseDto
    {
        public int? IntervalSeconds { get; set; }
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

    /// <summary>
    /// Oldest-first GPS points for the given session, for drawing its route on a map. Silent on
    /// failure (empty list), same reasoning as <see cref="GetMineActiveAsync"/> - a map that just
    /// shows no route on a transient error is better than a toast.
    /// </summary>
    public async Task<List<JMDOTrackingPointDto>> GetSessionPointsAsync(int sessionId, CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<JMDOTrackingPointDto>>($"{Root}/sessions/{sessionId}/points", Json, ct);
            return result ?? new();
        }
        catch (Exception)
        {
            return new();
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
