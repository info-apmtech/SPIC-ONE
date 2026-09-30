using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>Well-known log event ids of the telemetry pipeline.</summary>
public static class TelemetryEvents
{
    /// <summary>The row was already recorded (exception middleware); the logger provider skips it.</summary>
    public static readonly EventId AlreadyRecorded = new(90417, "TelemetryAlreadyRecorded");
}

/// <summary>
/// Async-local "do not capture" flag: set while the writer / rollup talk to the database so an EF
/// error logged there does not queue another row that would fail the same way.
/// </summary>
public static class TelemetryScope
{
    private static readonly AsyncLocal<bool> Suppressed = new();

    public static bool IsSuppressed => Suppressed.Value;

    public static IDisposable Suppress()
    {
        var previous = Suppressed.Value;
        Suppressed.Value = true;
        return new Restore(previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly bool _previous;
        public Restore(bool previous) => _previous = previous;
        public void Dispose() => Suppressed.Value = _previous;
    }
}

/// <summary>
/// Request helpers shared by the middlewares, the logger provider and the ingest controller:
/// client headers, route template, user claims, trace id, string caps.
/// </summary>
public static partial class TelemetryHttp
{
    public const string ClientHeader = "X-Spic-Client";
    public const string VersionHeader = "X-Spic-Version";
    public const string KeyHeader = "X-Telemetry-Key";

    private const string TraceIdItem = "spic.telemetry.traceId";

    /// <summary>Set by the host (Program.cs) because the endpoint / RoutePattern types live in the
    /// ASP.NET shared framework: returns the matched RouteEndpoint's RoutePattern.RawText.</summary>
    public static Func<HttpContext, string?>? RouteTemplateResolver { get; set; }

    // Exceptions already written as an error row (exception middleware); a later log of the same
    // exception object (Kestrel, a rethrow) is skipped by the logger provider.
    private static readonly ConditionalWeakTable<Exception, object> RecordedExceptions = new();

    public static void MarkRecorded(Exception ex)
    {
        try { RecordedExceptions.AddOrUpdate(ex, ex); } catch { /* best effort */ }
    }

    public static bool IsRecorded(Exception? ex) => ex is not null && RecordedExceptions.TryGetValue(ex, out _);

    public static string? Trim(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }

    public static string TrimRequired(string? value, int max, string fallback = "") =>
        Trim(value, max) is { Length: > 0 } v ? v : fallback;

    /// <summary>web | android | ios | windows | maccatalyst (case-insensitive), else Unknown.</summary>
    public static TelemetryApp ParseApp(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "web" => TelemetryApp.Web,
        "android" => TelemetryApp.Android,
        "ios" => TelemetryApp.iOS,
        "windows" => TelemetryApp.Windows,
        "maccatalyst" => TelemetryApp.MacCatalyst,
        _ => TelemetryApp.Unknown
    };

    public static TelemetryApp AppOf(HttpContext ctx) => ParseApp(ctx.Request.Headers[ClientHeader].ToString());

    public static string? VersionOf(HttpContext ctx) => Trim(ctx.Request.Headers[VersionHeader].ToString(), 40) is { Length: > 0 } v ? v : null;

    public static (string? UserId, string? UserName, string? Role) UserOf(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true) return (null, null, null);
        return (Trim(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, 450),
                Trim(user.Identity?.Name, 256),
                Trim(user.FindFirst(ClaimTypes.Role)?.Value, 64));
    }

    /// <summary>One id per request, shared by its request row and every error row it produces.</summary>
    public static string TraceIdOf(HttpContext ctx)
    {
        if (ctx.Items.TryGetValue(TraceIdItem, out var existing) && existing is string s) return s;
        var id = Trim(Activity.Current?.Id ?? ctx.TraceIdentifier, 64) ?? "";
        ctx.Items[TraceIdItem] = id;
        return id;
    }

    public static string? IpOf(HttpContext ctx)
    {
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            if (first.Length > 0) return Trim(first, 64);
        }
        return Trim(ctx.Connection.RemoteIpAddress?.ToString(), 64);
    }

    /// <summary>Route template ("api/Lab/batches/{id}") of the matched endpoint, else the path with
    /// numeric / guid segments replaced by {id}. Never contains a query string.</summary>
    public static string RouteOf(HttpContext ctx)
    {
        string? raw = null;
        try { raw = RouteTemplateResolver?.Invoke(ctx); } catch { /* fall back to the path */ }
        var route = !string.IsNullOrWhiteSpace(raw)
            ? NormalizeTemplate(raw!)
            : NormalizePath(ctx.Request.Path.Value, keepLeadingSlash: false);
        return TrimRequired(route, 300, "/");
    }

    /// <summary>"/api/Sas/payments/{id:int}/proof" -> "api/Sas/payments/{id}/proof".</summary>
    public static string NormalizeTemplate(string raw)
    {
        var t = TemplateToken().Replace(raw.Trim(), m => "{" + m.Groups[1].Value + "}");
        t = t.TrimStart('/');
        return t.Length == 0 ? "/" : t;
    }

    /// <summary>Query / fragment removed, numeric and guid segments replaced by {id}.</summary>
    public static string NormalizePath(string? path, bool keepLeadingSlash)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var p = path.Trim();
        var cut = p.IndexOfAny(new[] { '?', '#' });
        if (cut >= 0) p = p[..cut];

        var segments = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            if (IsIdSegment(segments[i])) segments[i] = "{id}";
        }
        var joined = string.Join('/', segments);
        if (joined.Length == 0) return "/";
        return keepLeadingSlash ? "/" + joined : joined;
    }

    private static bool IsIdSegment(string segment) =>
        segment.All(char.IsDigit) || Guid.TryParse(segment, out _) || LongHex().IsMatch(segment);

    [GeneratedRegex(@"\{\**([A-Za-z0-9_]+)[^}]*\}")]
    private static partial Regex TemplateToken();

    // 24+ hex chars (object ids, hashes) are ids too.
    [GeneratedRegex(@"^[0-9a-fA-F]{24,}$")]
    private static partial Regex LongHex();
}
