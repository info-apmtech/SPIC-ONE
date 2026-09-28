using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.Entities;

/// <summary>
/// Usage and error telemetry (docs/metrics-telemetry-plan.md, 2026-09-28). Rows are written only
/// by the global capture points (API middleware, logger providers, the layouts' error boundary,
/// the navigation tracker and the JS hook) - never by pages or controllers, so a new page is
/// covered automatically. Tables are pruned by TelemetryRollupService (config Telemetry:Retention).
/// </summary>
public enum TelemetryApp
{
    Unknown = 0,
    Web = 1,
    Android = 2,
    iOS = 3,
    Windows = 4,
    MacCatalyst = 5,
    /// <summary>Server-originated (no client involved).</summary>
    Server = 6
}

/// <summary>Where an error was caught.</summary>
public enum TelemetrySource
{
    /// <summary>API middleware / API logger provider.</summary>
    Api = 0,
    /// <summary>Blazor Server host logger provider (circuit, SignalR, renderer).</summary>
    WebHost = 1,
    /// <summary>Client code: error boundary, MAUI logger provider, HttpClient failures.</summary>
    Client = 2,
    /// <summary>window.onerror / unhandledrejection.</summary>
    Js = 3
}

/// <summary>One API request (RequestTelemetryMiddleware). No query strings, no bodies.</summary>
public class AppRequestLog
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    [MaxLength(10)] public string Method { get; set; } = "";
    /// <summary>Route template (api/Lab/batches/{id}); ids never expand the cardinality.</summary>
    [MaxLength(300)] public string Path { get; set; } = "";
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    [MaxLength(450)] public string? UserId { get; set; }
    [MaxLength(256)] public string? UserName { get; set; }
    [MaxLength(64)] public string? Role { get; set; }
    public TelemetryApp App { get; set; }
    [MaxLength(40)] public string? AppVersion { get; set; }
    [MaxLength(64)] public string? Ip { get; set; }
    [MaxLength(64)] public string? TraceId { get; set; }
}

/// <summary>One client navigation (ClientTelemetry.LocationChanged).</summary>
public class AppPageView
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    /// <summary>Normalised app route: "/Lab/Tracking/{id}" (numeric and guid segments replaced).</summary>
    [MaxLength(300)] public string Route { get; set; } = "";
    [MaxLength(450)] public string? UserId { get; set; }
    [MaxLength(256)] public string? UserName { get; set; }
    [MaxLength(64)] public string? Role { get; set; }
    public TelemetryApp App { get; set; }
    [MaxLength(40)] public string? AppVersion { get; set; }
    /// <summary>Circuit id on the web host, an app-run id on MAUI.</summary>
    [MaxLength(128)] public string? SessionId { get; set; }
}

/// <summary>One error occurrence. Grouped on the Metrics page by <see cref="Fingerprint"/>.</summary>
public class AppErrorLog
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public TelemetrySource Source { get; set; }
    public TelemetryApp App { get; set; }
    [MaxLength(40)] public string? AppVersion { get; set; }
    /// <summary>SHA-256 (hex, 64) of source + exception type + normalised message + top frame.</summary>
    [MaxLength(64)] public string Fingerprint { get; set; } = "";
    [MaxLength(200)] public string ExceptionType { get; set; } = "";
    [MaxLength(2000)] public string Message { get; set; } = "";
    [MaxLength(8000)] public string? StackTrace { get; set; }
    /// <summary>API route template or client route.</summary>
    [MaxLength(300)] public string? Route { get; set; }
    [MaxLength(10)] public string? Method { get; set; }
    public int? StatusCode { get; set; }
    /// <summary>Logger category (e.g. SpicAPI.Controllers.LabController) when caught by a logger provider.</summary>
    [MaxLength(300)] public string? Category { get; set; }
    [MaxLength(450)] public string? UserId { get; set; }
    [MaxLength(256)] public string? UserName { get; set; }
    [MaxLength(64)] public string? Role { get; set; }
    [MaxLength(64)] public string? TraceId { get; set; }
    [MaxLength(128)] public string? SessionId { get; set; }
    /// <summary>Resolved is per fingerprint: resolving a group flags every row of it.</summary>
    public bool IsResolved { get; set; }
    [MaxLength(256)] public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>Day x app x role rollup (Role "" = all roles; App Unknown = all apps).</summary>
public class AppUsageDaily
{
    public int Id { get; set; }
    /// <summary>Calendar day (UTC date, time 00:00).</summary>
    public DateTime Day { get; set; }
    public TelemetryApp App { get; set; }
    [MaxLength(64)] public string Role { get; set; } = "";
    public int ActiveUsers { get; set; }
    public int Requests { get; set; }
    public int PageViews { get; set; }
    public int Errors { get; set; }
    public int AvgDurationMs { get; set; }
    public int P95DurationMs { get; set; }
    public DateTime ComputedAt { get; set; }
}

public enum TelemetryRouteKind
{
    /// <summary>A client route from AppPageViews.</summary>
    Page = 0,
    /// <summary>An API route template from AppRequestLogs ("GET api/Lab/batches/{id}").</summary>
    Endpoint = 1
}

/// <summary>Day x app x route rollup for the Pages and API tabs.</summary>
public class AppRouteDaily
{
    public int Id { get; set; }
    public DateTime Day { get; set; }
    public TelemetryApp App { get; set; }
    public TelemetryRouteKind Kind { get; set; }
    /// <summary>"/Lab/Tracking/{id}" for pages; "GET api/Lab/batches/{id}" for endpoints.</summary>
    [MaxLength(320)] public string Route { get; set; } = "";
    /// <summary>Page views or requests.</summary>
    public int Hits { get; set; }
    public int Users { get; set; }
    /// <summary>Errors matched to the route (5xx + logged errors for endpoints; client errors for pages).</summary>
    public int Errors { get; set; }
    public int AvgDurationMs { get; set; }
    public int P95DurationMs { get; set; }
}
