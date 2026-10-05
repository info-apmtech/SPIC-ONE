using Microsoft.JSInterop;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Scoped mirror of the browser's <c>navigator.onLine</c>, kept current through the
/// <c>online</c>/<c>offline</c> events that <c>feedback-interop.js</c> forwards. Works in
/// browsers and inside the MAUI BlazorWebView (Android/iOS/Windows all fire the events).
///
/// Register with <c>services.AddScoped&lt;ConnectivityState&gt;()</c>. It starts listening the
/// first time <see cref="InitializeAsync"/> is called (which <c>OfflineBanner</c> does after
/// its first render, when JS interop is available) and stops on dispose.
/// </summary>
public sealed class ConnectivityState : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<ConnectivityState>? _selfRef;
    private bool _initialized;
    private bool _disposed;

    public ConnectivityState(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>True until the browser reports otherwise (optimistic default).</summary>
    public bool IsOnline { get; private set; } = true;

    /// <summary>When the current offline period began (UTC); null while online.</summary>
    public DateTime? OfflineSinceUtc { get; private set; }

    /// <summary>True once <see cref="InitializeAsync"/> has completed and events are wired.</summary>
    public bool IsListening => _initialized;

    /// <summary>Raised on every online/offline transition (from the JS interop thread; use InvokeAsync in components).</summary>
    public event Action? OnChange;

    /// <summary>Raised when the connection comes back after an offline period.</summary>
    public event Action? OnBackOnline;

    /// <summary>
    /// Read the current state and start listening. Safe to call more than once; only the
    /// first call does work. Must be called after the first render (needs JS interop).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized || _disposed) return;

        try
        {
            _selfRef ??= DotNetObjectReference.Create(this);
            var online = await _js.InvokeAsync<bool>("spicFeedback.connectivity.subscribe", _selfRef);
            _initialized = true;
            Apply(online);
        }
        catch (JSException)
        {
            // feedback-interop.js not loaded on this page: stay optimistic (online) and silent.
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
            // Pre-render / no JS runtime yet.
        }
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline) => Apply(isOnline);

    private void Apply(bool online)
    {
        if (online == IsOnline) return;

        var wasOffline = !IsOnline;
        IsOnline = online;
        OfflineSinceUtc = online ? null : DateTime.UtcNow;

        OnChange?.Invoke();
        if (online && wasOffline) OnBackOnline?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_initialized)
        {
            try
            {
                await _js.InvokeVoidAsync("spicFeedback.connectivity.unsubscribe");
            }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
        }

        _selfRef?.Dispose();
        _selfRef = null;
    }
}
