using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services.Telemetry;

/// <summary>
/// Page views and client errors for one circuit / app run (docs/metrics-telemetry-plan.md, rows 6-8).
/// Started once from Routes.razor, so every page and both layouts are covered without touching them:
///   * NavigationManager.LocationChanged -> a page view (normalised route) while signed in;
///   * ReportAsync (TelemetryErrorBoundary) and ReportJsError (window.onerror via app-interop.js);
///   * queued and posted to api/Telemetry/batch every 20 s, at 25 items, right after an error and
///     on dispose. Every path swallows its exceptions: telemetry never breaks a page and never toasts.
/// Scoped in both hosts.
/// </summary>
public sealed class ClientTelemetry : IAsyncDisposable, IDisposable
{
    private const string BatchUrl = "api/Telemetry/batch";
    private const int FlushAt = 25;
    private const int MaxQueued = 500;
    private static readonly TimeSpan FlushEvery = TimeSpan.FromSeconds(20);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>One id per MAUI app run (the web host uses the circuit id instead).</summary>
    private static readonly string AppRunId = Guid.NewGuid().ToString("N");

    private readonly NavigationManager _nav;
    private readonly LoginState _login;
    private readonly ShellSession _shell;
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private readonly ClientInfo _info;
    private readonly TelemetryCircuitRegistry _registry;

    private readonly object _gate = new();
    private List<ClientPageViewDto> _views = new();
    private List<ClientErrorReportDto> _errors = new();
    private int _flushing;

    private Timer? _timer;
    private DotNetObjectReference<ClientTelemetry>? _selfRef;
    private bool _started;
    private bool _disposed;

    private string _route = "/";
    private string? _lastViewRoute;
    private DateTime _lastViewAt;
    private string? _lastErrorKey;
    private DateTime _lastErrorAt;
    private string? _sessionId;

    public ClientTelemetry(NavigationManager nav, LoginState login, ShellSession shell, HttpClient http,
        IJSRuntime js, ClientInfo info, TelemetryCircuitRegistry registry)
    {
        _nav = nav;
        _login = login;
        _shell = shell;
        _http = http;
        _js = js;
        _info = info;
        _registry = registry;
        if (info.App != TelemetryApp.Web) _sessionId = AppRunId;
    }

    /// <summary>Circuit id on the web host (set by TelemetryCircuitHandler); an app-run id on MAUI.</summary>
    public string? SessionId
    {
        get => _sessionId;
        set
        {
            if (_sessionId == value) return;
            _registry.Remove(_sessionId);
            _sessionId = value;
            UpdateRegistry();
        }
    }

    /// <summary>Called once from Routes.razor. Later calls do nothing.</summary>
    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;

        try
        {
            _route = NormalizeRoute(_nav.Uri, _nav.BaseUri);
            _nav.LocationChanged += OnLocationChanged;
            _login.OnChange += OnLoginChanged;
            _timer = new Timer(_ => _ = FlushAsync(), null, FlushEvery, FlushEvery);
            UpdateRegistry();
            if (_login.IsLoggedIn) AddView(_route);
        }
        catch
        {
            // telemetry must never break the app
        }

        _ = AttachJsAsync();
    }

    // ---------------------------------------------------------------- capture

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            _route = NormalizeRoute(e.Location, _nav.BaseUri);
            UpdateRegistry();
            if (_login.IsLoggedIn) AddView(_route);
        }
        catch
        {
        }
    }

    private void OnLoginChanged()
    {
        try
        {
            UpdateRegistry();
            // MAUI keeps this scope across a logout / login: forget the last view so the landing
            // page of the next sign-in is counted. The web host gets a new circuit instead.
            if (!_login.IsLoggedIn) _lastViewRoute = null;
            // the page the user lands on after a refresh is not a LocationChanged
            if (_login.IsLoggedIn && _lastViewRoute is null) AddView(_route);
        }
        catch
        {
        }
    }

    private void AddView(string route)
    {
        if (route.Equals("/login", StringComparison.OrdinalIgnoreCase)) return;

        var now = DateTime.UtcNow;
        // a query-string change (tab=, page=) on the same page is not another view
        if (route == _lastViewRoute && now - _lastViewAt < TimeSpan.FromMinutes(1)) return;
        _lastViewRoute = route;
        _lastViewAt = now;

        Enqueue(new ClientPageViewDto { Route = route, At = now, SessionId = _sessionId }, null);
    }

    /// <summary>An exception caught by the layouts' error boundary (or any client code). Never throws.</summary>
    public Task ReportAsync(Exception ex, string? category)
    {
        try
        {
            if (ex is OperationCanceledException or JSDisconnectedException) return Task.CompletedTask;

            var type = ex.GetType().FullName ?? ex.GetType().Name;
            AddError(TelemetrySource.Client, type, ex.Message, ex.ToString(), _route, category);
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

    /// <summary>A JavaScript error reported from outside the JS hook.</summary>
    public void ReportJs(string? message, string? source, string? stack, string? url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var route = string.IsNullOrWhiteSpace(url) ? _route : NormalizeRoute(url, _nav.BaseUri);
            AddError(TelemetrySource.Js, "JavaScriptError", message, stack, route, Clip(source, 300));
        }
        catch
        {
        }
    }

    /// <summary>Called by window.spic.telemetry (app-interop.js).</summary>
    [JSInvokable]
    public Task ReportJsError(string message, string? source, string? stack, string? url)
    {
        ReportJs(message, source, stack, url);
        return Task.CompletedTask;
    }

    private void AddError(TelemetrySource source, string type, string? message, string? stack, string? route, string? category)
    {
        var text = Clip(message, 2000) ?? "";
        var now = DateTime.UtcNow;

        // the same error in a render loop or a timer: one report per 5 seconds
        var key = type + "|" + text;
        if (key == _lastErrorKey && now - _lastErrorAt < TimeSpan.FromSeconds(5)) return;
        _lastErrorKey = key;
        _lastErrorAt = now;

        Enqueue(null, new ClientErrorReportDto
        {
            Source = source,
            At = now,
            ExceptionType = Clip(type, 200) ?? "Error",
            Message = text,
            StackTrace = Clip(stack, 8000),
            Route = route,
            Category = category,
            SessionId = _sessionId
        });

        _ = FlushAsync();
    }

    private void Enqueue(ClientPageViewDto? view, ClientErrorReportDto? error)
    {
        if (_disposed) return;
        int count;
        lock (_gate)
        {
            if (_views.Count + _errors.Count >= MaxQueued)
            {
                // the API has been unreachable for a long time: start again rather than grow
                _views.Clear();
                _errors.Clear();
            }

            if (view is not null) _views.Add(view);
            if (error is not null) _errors.Add(error);
            count = _views.Count + _errors.Count;
        }

        if (count >= FlushAt) _ = FlushAsync();
    }

    // ---------------------------------------------------------------- flush

    private async Task FlushAsync()
    {
        if (Interlocked.Exchange(ref _flushing, 1) == 1) return;

        try
        {
            List<ClientPageViewDto> views;
            List<ClientErrorReportDto> errors;
            lock (_gate)
            {
                if (_views.Count == 0 && _errors.Count == 0) return;
                views = _views;
                errors = _errors;
                _views = new List<ClientPageViewDto>();
                _errors = new List<ClientErrorReportDto>();
            }

            var batch = new ClientTelemetryBatchDto
            {
                App = _info.App,
                AppVersion = _info.Version,
                PageViews = views,
                Errors = errors
            };

            try
            {
                using var response = await _http.PostAsJsonAsync(BatchUrl, batch, Json).ConfigureAwait(false);
            }
            catch (Exception) when (!_disposed)
            {
                // offline or API down: keep the rows for the next tick, within the cap
                lock (_gate)
                {
                    if (_views.Count + _errors.Count + views.Count + errors.Count <= MaxQueued)
                    {
                        _views.InsertRange(0, views);
                        _errors.InsertRange(0, errors);
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            Volatile.Write(ref _flushing, 0);
        }
    }

    // ---------------------------------------------------------------- JS hook

    private async Task AttachJsAsync()
    {
        try
        {
            _selfRef ??= DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("spic.telemetry.attach", _selfRef);
        }
        catch
        {
            // no JS yet (prerender, WebView not ready) or app-interop.js missing
        }
    }

    // ---------------------------------------------------------------- registry

    private void UpdateRegistry()
    {
        var id = _sessionId;
        if (string.IsNullOrEmpty(id)) return;

        try
        {
            if (_login.IsLoggedIn)
            {
                _registry.Set(id, new TelemetryCircuitRegistry.Entry(
                    _login.UserId, _shell.UserName, _login.UserRole?.ToString(), _route));
            }
            else
            {
                _registry.Set(id, new TelemetryCircuitRegistry.Entry(null, null, null, _route));
            }

            if (_info.App != TelemetryApp.Web) _registry.AppToken = _login.Token;
        }
        catch
        {
        }
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>
    /// "/Lab/Tracking/{id}": base-relative path with a leading slash, no query or fragment,
    /// all-digit and GUID segments replaced with {id}, at most 300 characters, "/" for the root.
    /// </summary>
    public static string NormalizeRoute(string? uri, string? baseUri)
    {
        if (string.IsNullOrEmpty(uri)) return "/";

        var path = uri.AsSpan();
        if (!string.IsNullOrEmpty(baseUri) && uri.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase))
        {
            path = path[baseUri.Length..];
        }
        else if (Uri.TryCreate(uri, UriKind.Absolute, out var abs))
        {
            path = abs.AbsolutePath.AsSpan();
        }

        var cut = path.IndexOfAny('?', '#');
        if (cut >= 0) path = path[..cut];
        path = path.Trim('/');
        if (path.IsEmpty) return "/";

        // fast path: nothing to replace
        if (!HasIdSegment(path))
        {
            var plain = "/" + path.ToString();
            return plain.Length > 300 ? plain[..300] : plain;
        }

        var sb = new StringBuilder(path.Length + 8);
        foreach (var range in path.Split('/'))
        {
            var segment = path[range];
            if (segment.IsEmpty) continue;
            sb.Append('/');
            if (IsId(segment)) sb.Append("{id}");
            else sb.Append(segment);
            if (sb.Length >= 300) break;
        }

        var result = sb.Length == 0 ? "/" : sb.ToString();
        return result.Length > 300 ? result[..300] : result;
    }

    private static bool HasIdSegment(ReadOnlySpan<char> path)
    {
        foreach (var range in path.Split('/'))
        {
            if (IsId(path[range])) return true;
        }
        return false;
    }

    private static bool IsId(ReadOnlySpan<char> segment)
    {
        if (segment.IsEmpty) return false;
        if (Guid.TryParse(segment, out _)) return true;
        foreach (var c in segment)
        {
            if (!char.IsAsciiDigit(c)) return false;
        }
        return true;
    }

    private static string? Clip(string? text, int max) =>
        text is null ? null : text.Length > max ? text[..max] : text;

    // ---------------------------------------------------------------- dispose

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        Detach();

        try
        {
            await FlushAsync();
        }
        catch
        {
        }

        _disposed = true;
        _selfRef?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Detach();
        _ = FlushAsync();
        _disposed = true;
        _selfRef?.Dispose();
    }

    private void Detach()
    {
        try
        {
            _timer?.Dispose();
            _nav.LocationChanged -= OnLocationChanged;
            _login.OnChange -= OnLoginChanged;
            _registry.Remove(_sessionId);
        }
        catch
        {
        }
    }
}
