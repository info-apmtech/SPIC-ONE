using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// Metrics page + telemetry ingest contract (2026-09-28; docs/metrics-telemetry-plan.md).
/// Enums serialize as integers, dates are UTC, every list is paged (PageResult&lt;T&gt;, default 20,
/// max 100). "days" is 7 / 30 / 90 (default 7; clamped 1..400). "app" filters by TelemetryApp
/// (omitted = all).
///
/// INGEST (TelemetryController)
///   POST api/Telemetry/batch                 body ClientTelemetryBatchDto -> 202
///        bearer token  : rows attributed to the caller (page views + errors)
///        X-Telemetry-Key (config Telemetry:IngestKey): server-attributed (Source WebHost), no limits
///        anonymous     : errors only, max 5 per batch, 60 batches / minute / IP
///
/// READ (MetricsController; page key "Metrics" by designation, or role SuperAdmin / Admin / CorporateAdmin)
///   GET  api/Metrics/summary?days=                        -> MetricsSummaryDto
///   GET  api/Metrics/live                                 -> MetricsLiveDto (last 15 min / hour, raw tables)
///   GET  api/Metrics/series?days=&amp;app=                    -> MetricsSeriesDto (one point per day, today live)
///   GET  api/Metrics/pages?days=&amp;app=&amp;top=                -> List&lt;MetricsRouteRowDto&gt; (Kind Page)
///   GET  api/Metrics/endpoints?days=&amp;app=&amp;top=&amp;sort=      -> List&lt;MetricsRouteRowDto&gt; (Kind Endpoint; sort = hits | slow | errors)
///   GET  api/Metrics/users?days=&amp;q=&amp;role=&amp;app=&amp;page=&amp;pageSize= -> PageResult&lt;MetricsUserRowDto&gt; (last seen desc)
///   GET  api/Metrics/errors?days=&amp;q=&amp;source=&amp;app=&amp;resolved=&amp;page=&amp;pageSize= -> PageResult&lt;MetricsErrorGroupDto&gt; (last seen desc; resolved = true | false | omitted = all)
///   GET  api/Metrics/errors/{id}                          -> MetricsErrorDetailDto (one occurrence + its group)
///   POST api/Metrics/errors/{fingerprint}/resolve          body MetricsResolveDto -> MetricsErrorGroupDto
/// </summary>
public class ClientPageViewDto
{
    public string Route { get; set; } = "";
    public DateTime At { get; set; }
    public string? SessionId { get; set; }
}

public class ClientErrorReportDto
{
    public TelemetrySource Source { get; set; } = TelemetrySource.Client;
    public DateTime At { get; set; }
    public string ExceptionType { get; set; } = "";
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
    public string? Route { get; set; }
    public string? Category { get; set; }
    public string? SessionId { get; set; }
    /// <summary>Set by the web host's logger provider when it could map the circuit to a user.</summary>
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Role { get; set; }
}

public class ClientTelemetryBatchDto
{
    public TelemetryApp App { get; set; }
    public string? AppVersion { get; set; }
    public List<ClientPageViewDto> PageViews { get; set; } = new();
    public List<ClientErrorReportDto> Errors { get; set; } = new();
}

// ---------------------------------------------------------------- read models

public class MetricsAppSliceDto
{
    public TelemetryApp App { get; set; }
    public int ActiveUsers { get; set; }
    public int PageViews { get; set; }
    public int Requests { get; set; }
    public int Errors { get; set; }
}

public class MetricsRoleSliceDto
{
    public string Role { get; set; } = "";
    public int ActiveUsers { get; set; }
    public int PageViews { get; set; }
}

public class MetricsSummaryDto
{
    public int Days { get; set; }
    public int ActiveUsersToday { get; set; }
    public int ActiveUsersPeriod { get; set; }
    public int ActiveUsers30d { get; set; }
    /// <summary>Registered, active accounts (AspNetUsers).</summary>
    public int TotalUsers { get; set; }
    public int RequestsToday { get; set; }
    public int RequestsPeriod { get; set; }
    public int PageViewsToday { get; set; }
    public int PageViewsPeriod { get; set; }
    public int ErrorsToday { get; set; }
    public int ErrorsPeriod { get; set; }
    /// <summary>Distinct unresolved fingerprints in the period.</summary>
    public int OpenErrorGroups { get; set; }
    public int AvgDurationMsPeriod { get; set; }
    public int P95DurationMsPeriod { get; set; }
    public List<MetricsAppSliceDto> ByApp { get; set; } = new();
    public List<MetricsRoleSliceDto> ByRole { get; set; } = new();
    /// <summary>Rows the channel had to drop since the API started (0 = healthy).</summary>
    public long DroppedSinceStart { get; set; }
}

public class MetricsLiveDto
{
    public int ActiveUsers15Min { get; set; }
    public int RequestsLastHour { get; set; }
    public int ErrorsLastHour { get; set; }
    public int AvgDurationMsLastHour { get; set; }
    public DateTime ServerTimeUtc { get; set; }
}

public class MetricsDayPointDto
{
    public DateTime Day { get; set; }
    public int ActiveUsers { get; set; }
    public int Requests { get; set; }
    public int PageViews { get; set; }
    public int Errors { get; set; }
    public int AvgDurationMs { get; set; }
    public int P95DurationMs { get; set; }
}

public class MetricsSeriesDto
{
    public int Days { get; set; }
    public TelemetryApp? App { get; set; }
    public List<MetricsDayPointDto> Points { get; set; } = new();
}

public class MetricsRouteRowDto
{
    public TelemetryRouteKind Kind { get; set; }
    /// <summary>"/Lab/Tracking/{id}" or "GET api/Lab/batches/{id}".</summary>
    public string Route { get; set; } = "";
    public int Hits { get; set; }
    public int Users { get; set; }
    public int Errors { get; set; }
    public int AvgDurationMs { get; set; }
    public int P95DurationMs { get; set; }
}

public class MetricsUserRowDto
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? Name { get; set; }
    public string? Role { get; set; }
    public string? Designation { get; set; }
    /// <summary>App of the most recent request / page view.</summary>
    public TelemetryApp LastApp { get; set; }
    public string? LastAppVersion { get; set; }
    public DateTime LastSeen { get; set; }
    public string? LastRoute { get; set; }
    public int Requests { get; set; }
    public int PageViews { get; set; }
    public int Errors { get; set; }
    public int ActiveDays { get; set; }
}

public class MetricsErrorGroupDto
{
    public string Fingerprint { get; set; } = "";
    public TelemetrySource Source { get; set; }
    public TelemetryApp App { get; set; }
    public string ExceptionType { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Route { get; set; }
    public string? Category { get; set; }
    public int Count { get; set; }
    public int Users { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    /// <summary>Id of the most recent occurrence (for api/Metrics/errors/{id}).</summary>
    public long LastId { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class MetricsErrorOccurrenceDto
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public string? UserName { get; set; }
    public string? Role { get; set; }
    public TelemetryApp App { get; set; }
    public string? AppVersion { get; set; }
    public string? Route { get; set; }
    public int? StatusCode { get; set; }
    public string? TraceId { get; set; }
}

public class MetricsErrorDetailDto
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public TelemetrySource Source { get; set; }
    public TelemetryApp App { get; set; }
    public string? AppVersion { get; set; }
    public string Fingerprint { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
    public string? Route { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public string? Category { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Role { get; set; }
    public string? TraceId { get; set; }
    public string? SessionId { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    /// <summary>Whole group, not limited by "days".</summary>
    public int Occurrences { get; set; }
    public int AffectedUsers { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    /// <summary>Latest 20 occurrences of the group, newest first.</summary>
    public List<MetricsErrorOccurrenceDto> Recent { get; set; } = new();
}

public class MetricsResolveDto
{
    public bool Resolved { get; set; } = true;
}
