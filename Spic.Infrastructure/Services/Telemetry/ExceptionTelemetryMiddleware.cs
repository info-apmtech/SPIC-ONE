using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// First middleware of the pipeline (plan section 2, #2): an unhandled exception becomes an
/// AppErrorLogs row (Source Api), is still logged through ILogger at Error so the container console
/// keeps it (event <see cref="TelemetryEvents.AlreadyRecorded"/>: the logger provider skips it), and
/// the caller gets 500 { success:false, message, traceId } in the project's JSON convention. A client
/// that disconnected (OperationCanceledException on RequestAborted) is not an error: nothing recorded.
/// </summary>
public sealed class ExceptionTelemetryMiddleware
{
    // Same casing as MVC's defaults (AddControllers() without custom JSON options): camelCase.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly TelemetryChannel _channel;
    private readonly ILogger<ExceptionTelemetryMiddleware> _logger;

    public ExceptionTelemetryMiddleware(RequestDelegate next, TelemetryChannel channel, ILogger<ExceptionTelemetryMiddleware> logger)
    {
        _next = next;
        _channel = channel;
        _logger = logger;
    }

    public static bool IsClientAbort(Exception ex, HttpContext context) =>
        ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (IsClientAbort(ex, context))
        {
            // client went away: nothing to answer, nothing to record
        }
        catch (Exception ex)
        {
            var traceId = SafeTraceId(context);
            var route = SafeRoute(context);
            Record(context, ex, traceId, route);

            try
            {
                _logger.LogError(TelemetryEvents.AlreadyRecorded, ex, "Unhandled exception on {Method} {Route} (trace {TraceId})",
                    context.Request.Method, route, traceId);
            }
            catch
            {
                // logging must not hide the response
            }

            if (context.Response.HasStarted) throw;

            try
            {
                // Headers are kept on purpose: the CORS headers must stay so the browser can read it.
                context.Response.ContentLength = null;
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";
                var body = JsonSerializer.Serialize(new
                {
                    Success = false,
                    Message = $"Something went wrong. Reference {traceId}.",
                    TraceId = traceId
                }, Json);
                await context.Response.WriteAsync(body);
            }
            catch
            {
                // the connection is gone; nothing more to do
            }
        }
    }

    private void Record(HttpContext context, Exception ex, string traceId, string route)
    {
        try
        {
            TelemetryHttp.MarkRecorded(ex);
            var (userId, userName, role) = TelemetryHttp.UserOf(context.User);
            var type = ex.GetType().FullName ?? ex.GetType().Name;
            var message = ex.Message ?? "";
            var stack = ex.ToString();
            _channel.Enqueue(new AppErrorLog
            {
                At = DateTime.UtcNow,
                Source = TelemetrySource.Api,
                App = TelemetryHttp.AppOf(context),
                AppVersion = TelemetryHttp.VersionOf(context),
                Fingerprint = TelemetryFingerprint.Compute(TelemetrySource.Api, type, message, TelemetryFingerprint.TopFrame(ex.StackTrace) ?? route),
                ExceptionType = TelemetryHttp.TrimRequired(type, 200, "Exception"),
                Message = TelemetryHttp.TrimRequired(message, 2000, "(no message)"),
                StackTrace = TelemetryHttp.Trim(stack, 8000),
                Route = route,
                Method = TelemetryHttp.Trim(context.Request.Method, 10),
                StatusCode = StatusCodes.Status500InternalServerError,
                Category = TelemetryHttp.Trim(typeof(ExceptionTelemetryMiddleware).FullName, 300),
                UserId = userId,
                UserName = userName,
                Role = role,
                TraceId = traceId
            });
        }
        catch
        {
            // telemetry never fails a request
        }
    }

    private static string SafeTraceId(HttpContext context)
    {
        try { return TelemetryHttp.TraceIdOf(context); } catch { return context.TraceIdentifier ?? ""; }
    }

    private static string SafeRoute(HttpContext context)
    {
        try { return TelemetryHttp.RouteOf(context); } catch { return "/"; }
    }
}
