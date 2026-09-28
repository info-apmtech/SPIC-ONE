using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// One AppRequestLogs row per API request (plan section 2, #1): method, route template, status,
/// duration, caller claims, X-Spic-Client / X-Spic-Version, IP and trace id. Skips OPTIONS, the
/// excluded paths and api/Telemetry itself. Never stores query strings or bodies. The row is read
/// after the rest of the pipeline ran, so the claims and the matched endpoint are known even for
/// requests the authorization middleware short-circuits (401 / 403).
/// </summary>
public sealed class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TelemetryChannel _channel;
    private readonly IOptionsMonitor<TelemetryOptions> _options;

    public RequestTelemetryMiddleware(RequestDelegate next, TelemetryChannel channel, IOptionsMonitor<TelemetryOptions> options)
    {
        _next = next;
        _channel = channel;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldTrack(context))
        {
            await _next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        int? forcedStatus = null;
        try
        {
            TelemetryHttp.TraceIdOf(context);
            await _next(context);
        }
        catch (Exception ex)
        {
            // The exception middleware (outside this one) turns it into a 500; a client that went
            // away is recorded as 499 (nginx convention) and never as an error.
            forcedStatus = ExceptionTelemetryMiddleware.IsClientAbort(ex, context) ? 499 : 500;
            throw;
        }
        finally
        {
            Record(context, forcedStatus, Stopwatch.GetElapsedTime(started));
        }
    }

    private bool ShouldTrack(HttpContext context)
    {
        try
        {
            var options = _options.CurrentValue;
            if (!options.Enabled) return false;
            if (HttpMethods.IsOptions(context.Request.Method)) return false;

            var path = context.Request.Path;
            if (path.StartsWithSegments("/api/Telemetry", StringComparison.OrdinalIgnoreCase)) return false;
            foreach (var excluded in options.EffectiveExcludePaths)
            {
                if (string.IsNullOrWhiteSpace(excluded)) continue;
                if (excluded == "/")
                {
                    if (!path.HasValue || path.Value == "/") return false;
                }
                else if (path.StartsWithSegments(new PathString(excluded.StartsWith('/') ? excluded.TrimEnd('/') : "/" + excluded.TrimEnd('/')),
                             StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Record(HttpContext context, int? forcedStatus, TimeSpan elapsed)
    {
        try
        {
            var (userId, userName, role) = TelemetryHttp.UserOf(context.User);
            _channel.Enqueue(new AppRequestLog
            {
                At = DateTime.UtcNow,
                Method = TelemetryHttp.TrimRequired(context.Request.Method, 10, "GET"),
                Path = TelemetryHttp.RouteOf(context),
                StatusCode = forcedStatus ?? context.Response.StatusCode,
                DurationMs = (int)Math.Min(int.MaxValue, Math.Max(0, elapsed.TotalMilliseconds)),
                UserId = userId,
                UserName = userName,
                Role = role,
                App = TelemetryHttp.AppOf(context),
                AppVersion = TelemetryHttp.VersionOf(context),
                Ip = TelemetryHttp.IpOf(context),
                TraceId = TelemetryHttp.TraceIdOf(context)
            });
        }
        catch
        {
            // telemetry never fails a request
        }
    }
}
