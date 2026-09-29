using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// API logger provider (plan section 2, #3): anything any code logs at Telemetry:MinimumLogLevel
/// (Error) or above becomes an AppErrorLogs row (Source Api) with the current request's route / user /
/// app, else App Server. Skips its own namespace, <see cref="TelemetryEvents.AlreadyRecorded"/>,
/// exceptions the middleware already recorded and anything logged while the writer / rollup run.
/// Deduped per fingerprint for 60 s. Dependencies are resolved on the first log (the provider is
/// built together with the logger factory, before the channel exists).
/// </summary>
public sealed class DbTelemetryLoggerProvider : ILoggerProvider
{
    private const string OwnNamespace = "Spic.Infrastructure.Services.Telemetry";
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromSeconds(60);

    private readonly IServiceProvider _services;
    private readonly ConcurrentDictionary<string, DateTime> _recent = new();
    private TelemetryChannel? _channel;
    private IHttpContextAccessor? _http;
    private IOptionsMonitor<TelemetryOptions>? _options;
    private bool _resolved;

    [ThreadStatic] private static bool _inside;

    public DbTelemetryLoggerProvider(IServiceProvider services) => _services = services;

    public ILogger CreateLogger(string categoryName) => new DbTelemetryLogger(this, categoryName ?? "");

    public void Dispose()
    {
    }

    private bool Resolve()
    {
        if (_resolved) return _channel is not null;
        try
        {
            _options = _services.GetService<IOptionsMonitor<TelemetryOptions>>();
            _http = _services.GetService<IHttpContextAccessor>();
            _channel = _services.GetService<TelemetryChannel>();
        }
        catch
        {
            _channel = null;
        }
        _resolved = true;
        return _channel is not null;
    }

    /// <summary>Configured level, never below Warning (Error until the options are resolved).</summary>
    private LogLevel MinimumLevel
    {
        get
        {
            var raw = _options?.CurrentValue.MinimumLogLevel;
            var level = Enum.TryParse<LogLevel>(raw, true, out var parsed) ? parsed : LogLevel.Error;
            return level < LogLevel.Warning ? LogLevel.Warning : level;
        }
    }

    internal bool IsEnabled(string category, LogLevel level)
    {
        if (level == LogLevel.None || level < LogLevel.Warning) return false;
        if (category.StartsWith(OwnNamespace, StringComparison.Ordinal)) return false;
        // Before the first capture the options are unknown: let Warning through, Capture re-checks.
        return _resolved ? level >= MinimumLevel : true;
    }

    internal void Capture<TState>(string category, LogLevel level, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (_inside) return;
        _inside = true;
        try
        {
            if (eventId.Id == TelemetryEvents.AlreadyRecorded.Id || TelemetryScope.IsSuppressed) return;
            if (TelemetryHttp.IsRecorded(exception)) return;
            if (!Resolve() || _channel is null || !_channel.Enabled) return;
            if (level < MinimumLevel) return;

            string message;
            try { message = formatter(state, exception); } catch { message = exception?.Message ?? ""; }
            if (string.IsNullOrWhiteSpace(message)) message = exception?.Message ?? "(no message)";

            var type = exception is null ? "LogError" : exception.GetType().FullName ?? exception.GetType().Name;
            var context = SafeContext();
            var route = context is null ? null : TelemetryHttp.RouteOf(context);
            var fingerprint = TelemetryFingerprint.Compute(TelemetrySource.Api, type, message,
                TelemetryFingerprint.TopFrame(exception?.StackTrace) ?? category);

            if (IsDuplicate(fingerprint)) return;

            var (userId, userName, role) = context is null ? (null, null, null) : TelemetryHttp.UserOf(context.User);
            _channel.Enqueue(new AppErrorLog
            {
                At = DateTime.UtcNow,
                Source = TelemetrySource.Api,
                App = context is null ? TelemetryApp.Server : TelemetryHttp.AppOf(context),
                AppVersion = context is null ? null : TelemetryHttp.VersionOf(context),
                Fingerprint = fingerprint,
                ExceptionType = TelemetryHttp.TrimRequired(type, 200, "LogError"),
                Message = TelemetryHttp.TrimRequired(message, 2000, "(no message)"),
                StackTrace = TelemetryHttp.Trim(exception?.ToString(), 8000),
                Route = route,
                Method = context is null ? null : TelemetryHttp.Trim(context.Request.Method, 10),
                StatusCode = null,
                Category = TelemetryHttp.Trim(category, 300),
                UserId = userId,
                UserName = userName,
                Role = role,
                TraceId = context is null ? null : TelemetryHttp.TraceIdOf(context)
            });
        }
        catch
        {
            // a logger must never throw
        }
        finally
        {
            _inside = false;
        }
    }

    private HttpContext? SafeContext()
    {
        try { return _http?.HttpContext; } catch { return null; }
    }

    private bool IsDuplicate(string fingerprint)
    {
        var now = DateTime.UtcNow;
        if (_recent.TryGetValue(fingerprint, out var last) && now - last < DedupeWindow) return true;
        _recent[fingerprint] = now;

        if (_recent.Count > 500)
        {
            foreach (var entry in _recent)
            {
                if (now - entry.Value >= DedupeWindow) _recent.TryRemove(entry.Key, out _);
            }
        }
        return false;
    }

    private sealed class DbTelemetryLogger : ILogger
    {
        private readonly DbTelemetryLoggerProvider _provider;
        private readonly string _category;

        public DbTelemetryLogger(DbTelemetryLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(_category, logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            _provider.Capture(_category, logLevel, eventId, state, exception, formatter);
        }
    }
}
