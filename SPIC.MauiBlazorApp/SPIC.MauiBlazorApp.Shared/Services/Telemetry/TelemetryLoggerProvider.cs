using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services.Telemetry;

/// <summary>
/// Anything the HOST logs at Error or above (circuit crashes, renderer / SignalR errors on the web
/// host; the same on MAUI) is posted to api/Telemetry/batch (docs/metrics-telemetry-plan.md rows 4-5).
/// Deduped per fingerprint for 60 s, bounded queue of 200, flushed every 10 s by a background
/// loop with its own HttpClient. Web host: X-Telemetry-Key when configured, user / route from
/// TelemetryCircuitRegistry when the message or a scope names a known circuit. MAUI: posts with
/// the signed-in user's token when there is one. Never throws and never logs.
/// </summary>
public sealed class TelemetryLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private const string OwnCategory = "SPIC.MauiBlazorApp.Shared.Services.Telemetry";
    private const int QueueSize = 200;
    private static readonly TimeSpan FlushEvery = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DedupeFor = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ClientInfo _info;
    private readonly TelemetryCircuitRegistry _registry;
    private readonly TelemetrySource _source;
    private readonly string? _ingestKey;
    private readonly HttpClient? _http;
    private readonly Channel<ClientErrorReportDto> _queue;
    private readonly ConcurrentDictionary<string, DateTime> _recent = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _stop = new();
    private IExternalScopeProvider? _scopes;

    /// <param name="source">WebHost on the web host, Client on MAUI.</param>
    /// <param name="ingestKey">Telemetry:IngestKey (web host only; blank = post anonymously).</param>
    public TelemetryLoggerProvider(ClientInfo info, TelemetryCircuitRegistry registry, string? apiBaseUrl,
        TelemetrySource source, string? ingestKey = null)
    {
        _info = info;
        _registry = registry;
        _source = source;
        _ingestKey = string.IsNullOrWhiteSpace(ingestKey) ? null : ingestKey.Trim();
        _queue = Channel.CreateBounded<ClientErrorReportDto>(new BoundedChannelOptions(QueueSize)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true
        });

        try
        {
            if (Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri))
            {
                _http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(15) };
                _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Spic-Client", info.ClientHeader);
                _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Spic-Version", info.Version);
                if (_ingestKey is not null)
                    _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Telemetry-Key", _ingestKey);
                _ = Task.Run(RunAsync);
            }
        }
        catch
        {
            _http = null;
        }
    }

    public ILogger CreateLogger(string categoryName) => new TelemetryLogger(this, categoryName);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    // ---------------------------------------------------------------- capture

    private bool Accepts(string category) =>
        _http is not null && !category.StartsWith(OwnCategory, StringComparison.Ordinal);

    private void Capture(string category, LogLevel level, string? message, Exception? exception)
    {
        try
        {
            if (IsNoise(category, message, exception)) return;

            var type = exception?.GetType().FullName ?? $"Log{level}";
            // the exception's own message groups well; the log line (often with a circuit id) goes under the stack
            var text = Clip(exception?.Message is { Length: > 0 } m ? m : message, 2000) ?? "";
            if (text.Length == 0) text = type;
            var stack = exception is null ? null : $"{exception}\n--- logged as: {message}";

            var now = DateTime.UtcNow;
            var fingerprint = type + "|" + Normalise(text);
            if (_recent.TryGetValue(fingerprint, out var last) && now - last < DedupeFor) return;
            _recent[fingerprint] = now;
            if (_recent.Count > 500) Prune(now);

            var report = new ClientErrorReportDto
            {
                Source = _source,
                At = now,
                ExceptionType = Clip(type, 200) ?? "Error",
                Message = text,
                StackTrace = Clip(stack, 8000),
                Category = Clip(category, 300)
            };

            if (FindSession(message, out var entry))
            {
                report.UserId = entry.UserId;
                report.UserName = entry.UserName;
                report.Role = entry.Role;
                report.Route = entry.Route;
            }

            _queue.Writer.TryWrite(report);
        }
        catch
        {
            // never throw from a logger
        }
    }

    private static bool IsNoise(string category, string? message, Exception? exception)
    {
        // a closed tab or a navigation mid-call: not an application error
        if (exception is OperationCanceledException or JSDisconnectedException) return true;
        if (exception?.InnerException is OperationCanceledException or JSDisconnectedException) return true;

        // WebView chatter about a page that is going away
        if (category.StartsWith("Microsoft.AspNetCore.Components.WebView", StringComparison.Ordinal)
            && message is not null
            && message.Contains("disconnected", StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    private bool FindSession(string? message, out TelemetryCircuitRegistry.Entry entry)
    {
        entry = default;
        if (_registry.Count == 0) return false;

        if (_registry.TryFindIn(message, out entry)) return true;

        var scopes = _scopes;
        if (scopes is not null)
        {
            var found = false;
            TelemetryCircuitRegistry.Entry hit = default;
            scopes.ForEachScope((scope, _) =>
            {
                if (found || scope is null) return;
                if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
                {
                    foreach (var pair in pairs)
                    {
                        if (pair.Value is string s && _registry.TryGet(s, out hit)) { found = true; return; }
                    }
                }
                else if (_registry.TryFindIn(scope.ToString(), out hit))
                {
                    found = true;
                }
            }, (object?)null);

            if (found)
            {
                entry = hit;
                return true;
            }
        }

        // MAUI: one user per process
        return _source != TelemetrySource.WebHost && _registry.TryGetSingle(out entry);
    }

    // ---------------------------------------------------------------- post

    private async Task RunAsync()
    {
        using var timer = new PeriodicTimer(FlushEvery);
        try
        {
            while (await timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false))
            {
                await FlushAsync(_stop.Token).ConfigureAwait(false);
            }
        }
        catch
        {
            // stopping
        }
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        try
        {
            if (_http is null) return;

            var errors = new List<ClientErrorReportDto>();
            while (_queue.Reader.TryRead(out var e)) errors.Add(e);
            if (errors.Count == 0) return;

            var token = _source == TelemetrySource.WebHost ? null : _registry.AppToken;
            // anonymous batches are capped at 5 errors by the API
            var chunk = _ingestKey is not null || !string.IsNullOrEmpty(token) ? 50 : 5;

            for (var i = 0; i < errors.Count; i += chunk)
            {
                var batch = new ClientTelemetryBatchDto
                {
                    App = _info.App,
                    AppVersion = _info.Version,
                    Errors = errors.GetRange(i, Math.Min(chunk, errors.Count - i))
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/Telemetry/batch")
                {
                    Content = JsonContent.Create(batch, options: Json)
                };
                if (!string.IsNullOrEmpty(token))
                {
                    var raw = token.StartsWith("Bearer ", StringComparison.Ordinal) ? token[7..] : token;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", raw);
                }

                using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            }
        }
        catch
        {
            // API unreachable: these rows are dropped
        }
    }

    // ---------------------------------------------------------------- helpers

    private void Prune(DateTime now)
    {
        foreach (var pair in _recent)
        {
            if (now - pair.Value >= DedupeFor) _recent.TryRemove(pair.Key, out _);
        }
    }

    /// <summary>Digits collapse to '#', so "row 12" and "row 13" dedupe together.</summary>
    private static string Normalise(string text)
    {
        var sb = new StringBuilder(Math.Min(text.Length, 300));
        var lastDigit = false;
        foreach (var c in text)
        {
            if (sb.Length >= 300) break;
            if (char.IsAsciiDigit(c))
            {
                if (!lastDigit) sb.Append('#');
                lastDigit = true;
            }
            else
            {
                sb.Append(c);
                lastDigit = false;
            }
        }
        return sb.ToString();
    }

    private static string? Clip(string? text, int max) =>
        text is null ? null : text.Length > max ? text[..max] : text;

    public void Dispose()
    {
        try
        {
            _stop.Cancel();
            // last chance for what is queued; bounded so shutdown is never held up
            Task.Run(() => FlushAsync(CancellationToken.None)).Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
        }

        try
        {
            _http?.Dispose();
        }
        catch
        {
        }
    }

    private sealed class TelemetryLogger : ILogger
    {
        private readonly TelemetryLoggerProvider _owner;
        private readonly string _category;
        private readonly bool _accepts;

        public TelemetryLogger(TelemetryLoggerProvider owner, string category)
        {
            _owner = owner;
            _category = category;
            _accepts = owner.Accepts(category);
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            _owner._scopes?.Push(state);

        public bool IsEnabled(LogLevel logLevel) => _accepts && logLevel >= LogLevel.Error && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string? message;
            try
            {
                message = formatter(state, exception);
            }
            catch
            {
                message = null;
            }

            _owner.Capture(_category, logLevel, message, exception);
        }
    }
}
