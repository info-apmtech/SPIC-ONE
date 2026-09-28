using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Computes one UTC day's AppUsageDaily + AppRouteDaily rows from the raw tables, in memory from
/// narrow projections (a day is at most a few hundred thousand small rows). Used by the rollup for
/// stored days and by the Metrics page for today (live).
///
///   usage : per (App, Role), plus App Unknown = all apps and Role "" = all roles (so the all / all
///           row is (Unknown, "")). Requests without X-Spic-Client only count in the all-apps rows.
///           Requests without a signed-in user use the role "Anonymous".
///           ActiveUsers = distinct UserId over requests and page views; Errors = AppErrorLogs rows.
///   route : Page rows from page views (Errors = Client / Js error rows on that route) and Endpoint
///           rows "{Method} {Path}" from requests (Errors = requests that returned 5xx or produced an
///           Api error row with the same trace id), per app and for all apps.
/// P95 uses percentile_cont semantics (linear interpolation); 0 when there are no rows.
/// </summary>
public static class TelemetryAggregator
{
    public const string AnonymousRole = "Anonymous";

    public sealed record DayResult(DateTime Day, List<AppUsageDaily> Usage, List<AppRouteDaily> Routes);

    public static async Task<DayResult> ComputeDayAsync(AppDbContext db, DateTime day, CancellationToken ct)
    {
        day = day.Date;
        var end = day.AddDays(1);

        var requests = await db.AppRequestLogs.AsNoTracking()
            .Where(r => r.At >= day && r.At < end)
            .Select(r => new { r.App, r.Role, r.UserId, r.DurationMs, r.Method, r.Path, r.StatusCode, r.TraceId })
            .ToListAsync(ct);
        var views = await db.AppPageViews.AsNoTracking()
            .Where(v => v.At >= day && v.At < end)
            .Select(v => new { v.App, v.Role, v.UserId, v.Route })
            .ToListAsync(ct);
        var errors = await db.AppErrorLogs.AsNoTracking()
            .Where(e => e.At >= day && e.At < end)
            .Select(e => new { e.App, e.Role, e.Source, e.Route, e.TraceId })
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        // ---------------------------------------------------------------- usage
        var usage = new Dictionary<(TelemetryApp, string), UsageBucket>();
        IEnumerable<UsageBucket> UsageBuckets(TelemetryApp app, string? role)
        {
            var r = RoleKey(role);
            var keys = new HashSet<(TelemetryApp, string)> { (TelemetryApp.Unknown, r), (TelemetryApp.Unknown, "") };
            if (app != TelemetryApp.Unknown)
            {
                keys.Add((app, r));
                keys.Add((app, ""));
            }
            foreach (var key in keys)
            {
                if (!usage.TryGetValue(key, out var bucket)) usage[key] = bucket = new UsageBucket();
                yield return bucket;
            }
        }

        foreach (var r in requests)
        {
            foreach (var b in UsageBuckets(r.App, r.Role))
            {
                b.Requests++;
                b.Durations.Add(r.DurationMs);
                if (!string.IsNullOrEmpty(r.UserId)) b.Users.Add(r.UserId);
            }
        }
        foreach (var v in views)
        {
            foreach (var b in UsageBuckets(v.App, v.Role))
            {
                b.PageViews++;
                if (!string.IsNullOrEmpty(v.UserId)) b.Users.Add(v.UserId);
            }
        }
        foreach (var e in errors)
        {
            foreach (var b in UsageBuckets(e.App, e.Role)) b.Errors++;
        }

        var usageRows = usage.Select(kv => new AppUsageDaily
        {
            Day = day,
            App = kv.Key.Item1,
            Role = kv.Key.Item2,
            ActiveUsers = kv.Value.Users.Count,
            Requests = kv.Value.Requests,
            PageViews = kv.Value.PageViews,
            Errors = kv.Value.Errors,
            AvgDurationMs = Average(kv.Value.Durations),
            P95DurationMs = Percentile(kv.Value.Durations, 0.95),
            ComputedAt = now
        }).ToList();

        // ---------------------------------------------------------------- routes
        var routes = new Dictionary<(TelemetryApp, TelemetryRouteKind, string), RouteBucket>();
        IEnumerable<RouteBucket> RouteBuckets(TelemetryApp app, TelemetryRouteKind kind, string route)
        {
            var key = (TelemetryApp.Unknown, kind, route);
            if (!routes.TryGetValue(key, out var all)) routes[key] = all = new RouteBucket();
            yield return all;
            if (app != TelemetryApp.Unknown)
            {
                var appKey = (app, kind, route);
                if (!routes.TryGetValue(appKey, out var one)) routes[appKey] = one = new RouteBucket();
                yield return one;
            }
        }

        foreach (var v in views)
        {
            var route = TelemetryHttp.TrimRequired(v.Route, 320, "/");
            foreach (var b in RouteBuckets(v.App, TelemetryRouteKind.Page, route))
            {
                b.Hits++;
                if (!string.IsNullOrEmpty(v.UserId)) b.Users.Add(v.UserId);
            }
        }

        // Page errors: client / js error rows whose route has page views that day.
        foreach (var e in errors.Where(e => (e.Source == TelemetrySource.Client || e.Source == TelemetrySource.Js) && !string.IsNullOrEmpty(e.Route)))
        {
            var route = TelemetryHttp.TrimRequired(e.Route, 320, "/");
            if (routes.TryGetValue((TelemetryApp.Unknown, TelemetryRouteKind.Page, route), out var all)) all.Errors++;
            if (e.App != TelemetryApp.Unknown && routes.TryGetValue((e.App, TelemetryRouteKind.Page, route), out var one)) one.Errors++;
        }

        var apiErrorTraces = errors
            .Where(e => e.Source == TelemetrySource.Api && !string.IsNullOrEmpty(e.TraceId))
            .Select(e => e.TraceId!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var r in requests)
        {
            var route = TelemetryHttp.TrimRequired($"{r.Method} {r.Path}", 320, "/");
            var failed = r.StatusCode >= 500 || (!string.IsNullOrEmpty(r.TraceId) && apiErrorTraces.Contains(r.TraceId));
            foreach (var b in RouteBuckets(r.App, TelemetryRouteKind.Endpoint, route))
            {
                b.Hits++;
                b.Durations.Add(r.DurationMs);
                if (failed) b.Errors++;
                if (!string.IsNullOrEmpty(r.UserId)) b.Users.Add(r.UserId);
            }
        }

        var routeRows = routes.Select(kv => new AppRouteDaily
        {
            Day = day,
            App = kv.Key.Item1,
            Kind = kv.Key.Item2,
            Route = kv.Key.Item3,
            Hits = kv.Value.Hits,
            Users = kv.Value.Users.Count,
            Errors = kv.Value.Errors,
            AvgDurationMs = Average(kv.Value.Durations),
            P95DurationMs = Percentile(kv.Value.Durations, 0.95)
        }).ToList();

        return new DayResult(day, usageRows, routeRows);
    }

    public static string RoleKey(string? role) =>
        string.IsNullOrWhiteSpace(role) ? AnonymousRole : TelemetryHttp.TrimRequired(role, 64, AnonymousRole);

    public static int Average(List<int> values) =>
        values.Count == 0 ? 0 : (int)Math.Round(values.Average(v => (double)v));

    /// <summary>percentile_cont: linear interpolation between the closest ranks.</summary>
    public static int Percentile(List<int> values, double fraction)
    {
        if (values.Count == 0) return 0;
        var sorted = values.ToArray();
        Array.Sort(sorted);
        var position = fraction * (sorted.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        var value = sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        return (int)Math.Round(value);
    }

    private sealed class UsageBucket
    {
        public readonly HashSet<string> Users = new(StringComparer.Ordinal);
        public readonly List<int> Durations = new();
        public int Requests;
        public int PageViews;
        public int Errors;
    }

    private sealed class RouteBucket
    {
        public readonly HashSet<string> Users = new(StringComparer.Ordinal);
        public readonly List<int> Durations = new();
        public int Hits;
        public int Errors;
    }
}
