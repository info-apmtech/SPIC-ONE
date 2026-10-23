using Microsoft.JSInterop;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Owns the periodic "capture a GPS fix, post it" loop for JMDO live tracking, independently of
/// any one page's component lifecycle.
///
/// Previously this loop lived inside FieldDashboard.razor itself (a CancellationTokenSource field
/// cancelled in that component's Dispose()) - which meant tracking silently stopped the moment the
/// JMDO navigated to TaskDetail/TasksAllocation/anywhere else, defeating the point, since field
/// work *is* navigating around the app. Registered AddScoped (not AddSingleton) in both hosts:
///   - Web host (Blazor Server, AddInteractiveServerComponents): Scoped = one instance per circuit
///     (per connected browser tab/signed-in user) - AddSingleton here would be a real bug, sharing
///     one loop/JS runtime/HttpClient across every signed-in user on the server.
///   - Native MAUI host (BlazorWebView): one root component scope for the whole app run, so Scoped
///     behaves like a de-facto singleton for that single user's session - exactly what's wanted.
/// Either way the loop now survives in-app navigation for as long as the circuit/app session does,
/// which is the fix this type exists for. On native, this is a foreground-only complement to the
/// real continuous background service (see DeviceInterop.StartBackgroundTracking) - this loop keeps
/// running as a fallback/UI-refresh source whether or not the native service is also active.
/// </summary>
public sealed class JmdoTrackingClientLoop : IDisposable
{
    private const int PointIntervalSeconds = 20;

    private readonly IJSRuntime _js;
    private readonly JMDOTrackingApi _trackingApi;

    private CancellationTokenSource? _cts;
    private int? _sessionId;

    public JmdoTrackingClientLoop(IJSRuntime js, JMDOTrackingApi trackingApi)
    {
        _js = js;
        _trackingApi = trackingApi;
    }

    public bool IsRunning => _cts is not null;
    public int? SessionId => _sessionId;

    /// <summary>Idempotent - calling this while already running for the same session is a no-op, so
    /// a page that reloads mid-session (or a fresh component after navigating) can call this
    /// unconditionally without worrying about starting a second loop.</summary>
    public void Start(int sessionId)
    {
        if (_cts is not null && _sessionId == sessionId) return;

        StopInternal();

        _sessionId = sessionId;
        _cts = new CancellationTokenSource();
        _ = RunAsync(sessionId, _cts.Token);
    }

    public void Stop() => StopInternal();

    private void StopInternal()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _sessionId = null;
    }

    private async Task RunAsync(int sessionId, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(PointIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                GeoResult? position;
                try
                {
                    position = await _js.InvokeAsync<GeoResult?>("getCurrentPosition", ct, Array.Empty<object>());
                }
                catch (Exception)
                {
                    continue; // a transient failure just skips this interval's point
                }

                if (position is null || !string.IsNullOrWhiteSpace(position.Error)) continue;

                await _trackingApi.AddPointAsync(sessionId, position.Latitude, position.Longitude, position.Accuracy, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on Stop()/circuit teardown
        }
    }

    private sealed class GeoResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public string? Error { get; set; }
    }

    public void Dispose() => StopInternal();
}
