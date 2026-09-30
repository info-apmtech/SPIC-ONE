using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Read side of api/Metrics (contract SPIC.Core/DTOs/MetricsDtos.cs). A period of N days is today
/// plus the N-1 previous UTC days. Counts and timings: the daily tables for past days + today live
/// (TelemetryTodayCache). Distinct users over a period and per-route users come from the raw tables
/// (they cannot be summed across days), so they cover at most the raw retention (30 days). Every
/// query is AsNoTracking.
/// </summary>
public sealed class MetricsQueryService
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int DefaultTop = 20;

    private readonly AppDbContext _db;
    private readonly TelemetryTodayCache _today;
    private readonly TelemetryChannel _channel;

    public MetricsQueryService(AppDbContext db, TelemetryTodayCache today, TelemetryChannel channel)
    {
        _db = db;
        _today = today;
        _channel = channel;
    }

    public static int ClampDays(int? days) => Math.Clamp(days ?? 7, 1, 400);

    private static (DateTime Today, DateTime From) Period(int days)
    {
        var today = DateTime.UtcNow.Date;
        return (today, today.AddDays(-(days - 1)));
    }

    // ------------------------------------------------------------------ summary

    public async Task<MetricsSummaryDto> SummaryAsync(int? daysArg, CancellationToken ct)
    {
        var days = ClampDays(daysArg);
        var (today, from) = Period(days);
        var live = await _today.GetAsync(_db, ct);

        var past = await _db.AppUsageDaily.AsNoTracking()
            .Where(u => u.Day >= from && u.Day < today && (u.Role == "" || u.App == TelemetryApp.Unknown))
            .ToListAsync(ct);
        var rows = past.Concat(live.Usage.Where(u => u.Role == "" || u.App == TelemetryApp.Unknown)).ToList();
        var all = rows.Where(u => u.App == TelemetryApp.Unknown && u.Role == "").ToList();
        var todayAll = live.Usage.FirstOrDefault(u => u.App == TelemetryApp.Unknown && u.Role == "");

        // Distinct (app, role, user) of the period from the raw tables.
        var requestUsers = await _db.AppRequestLogs.AsNoTracking()
            .Where(r => r.At >= from && r.UserId != null)
            .Select(r => new { r.App, r.Role, r.UserId })
            .Distinct()
            .ToListAsync(ct);
        var viewUsers = await _db.AppPageViews.AsNoTracking()
            .Where(v => v.At >= from && v.UserId != null)
            .Select(v => new { v.App, v.Role, v.UserId })
            .Distinct()
            .ToListAsync(ct);
        var triples = requestUsers.Select(x => (x.App, Role: TelemetryAggregator.RoleKey(x.Role), UserId: x.UserId!))
            .Concat(viewUsers.Select(x => (x.App, Role: TelemetryAggregator.RoleKey(x.Role), UserId: x.UserId!)))
            .ToList();

        int activeUsers30d;
        if (days == 30)
        {
            activeUsers30d = triples.Select(t => t.UserId).Distinct().Count();
        }
        else
        {
            var from30 = today.AddDays(-29);
            activeUsers30d = await _db.AppRequestLogs.AsNoTracking()
                .Where(r => r.At >= from30 && r.UserId != null).Select(r => r.UserId)
                .Union(_db.AppPageViews.AsNoTracking().Where(v => v.At >= from30 && v.UserId != null).Select(v => v.UserId))
                .CountAsync(ct);
        }

        var requestsPeriod = all.Sum(u => u.Requests);
        var weightedAvg = requestsPeriod == 0 ? 0 : (int)Math.Round(all.Sum(u => (double)u.AvgDurationMs * u.Requests) / requestsPeriod);

        var dto = new MetricsSummaryDto
        {
            Days = days,
            ActiveUsersToday = todayAll?.ActiveUsers ?? 0,
            ActiveUsersPeriod = triples.Select(t => t.UserId).Distinct().Count(),
            ActiveUsers30d = activeUsers30d,
            TotalUsers = await _db.Users.AsNoTracking().CountAsync(u => u.IsActive, ct),
            RequestsToday = todayAll?.Requests ?? 0,
            RequestsPeriod = requestsPeriod,
            PageViewsToday = todayAll?.PageViews ?? 0,
            PageViewsPeriod = all.Sum(u => u.PageViews),
            ErrorsToday = todayAll?.Errors ?? 0,
            ErrorsPeriod = all.Sum(u => u.Errors),
            OpenErrorGroups = await _db.AppErrorLogs.AsNoTracking()
                .Where(e => e.At >= from && !e.IsResolved)
                .Select(e => e.Fingerprint)
                .Distinct()
                .CountAsync(ct),
            AvgDurationMsPeriod = weightedAvg,
            P95DurationMsPeriod = await P95Async(from, ct),
            DroppedSinceStart = _channel.DroppedSinceStart
        };

        // By app: known apps from their own rows; traffic without X-Spic-Client is the remainder.
        var appRows = rows.Where(u => u.Role == "" && u.App != TelemetryApp.Unknown).ToList();
        var apps = appRows.Select(u => u.App).Concat(triples.Select(t => t.App)).Where(a => a != TelemetryApp.Unknown).Distinct();
        foreach (var app in apps.OrderBy(a => a))
        {
            var mine = appRows.Where(u => u.App == app).ToList();
            dto.ByApp.Add(new MetricsAppSliceDto
            {
                App = app,
                ActiveUsers = triples.Where(t => t.App == app).Select(t => t.UserId).Distinct().Count(),
                Requests = mine.Sum(u => u.Requests),
                PageViews = mine.Sum(u => u.PageViews),
                Errors = mine.Sum(u => u.Errors)
            });
        }
        var unknown = new MetricsAppSliceDto
        {
            App = TelemetryApp.Unknown,
            ActiveUsers = triples.Where(t => t.App == TelemetryApp.Unknown).Select(t => t.UserId).Distinct().Count(),
            Requests = Math.Max(0, dto.RequestsPeriod - dto.ByApp.Sum(a => a.Requests)),
            PageViews = Math.Max(0, dto.PageViewsPeriod - dto.ByApp.Sum(a => a.PageViews)),
            Errors = Math.Max(0, dto.ErrorsPeriod - dto.ByApp.Sum(a => a.Errors))
        };
        if (unknown.ActiveUsers + unknown.Requests + unknown.PageViews + unknown.Errors > 0) dto.ByApp.Add(unknown);

        // By role (all apps).
        var roleRows = rows.Where(u => u.App == TelemetryApp.Unknown && u.Role != "").ToList();
        foreach (var role in roleRows.Select(u => u.Role).Concat(triples.Select(t => t.Role)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slice = new MetricsRoleSliceDto
            {
                Role = role,
                ActiveUsers = triples.Where(t => string.Equals(t.Role, role, StringComparison.OrdinalIgnoreCase)).Select(t => t.UserId).Distinct().Count(),
                PageViews = roleRows.Where(u => string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase)).Sum(u => u.PageViews)
            };
            if (slice.ActiveUsers + slice.PageViews > 0) dto.ByRole.Add(slice);
        }
        dto.ByRole = dto.ByRole.OrderByDescending(r => r.ActiveUsers).ThenByDescending(r => r.PageViews).ThenBy(r => r.Role).ToList();

        return dto;
    }

    /// <summary>P95 request duration over the raw rows since <paramref name="from"/> (percentile_cont).</summary>
    private async Task<int> P95Async(DateTime from, CancellationToken ct)
    {
        // An aggregate without GROUP BY always returns exactly one row (NULL when there are no rows).
        var values = await _db.Database
            .SqlQuery<double?>($"SELECT percentile_cont(0.95) WITHIN GROUP (ORDER BY \"DurationMs\") AS \"Value\" FROM \"AppRequestLogs\" WHERE \"At\" >= {from}")
            .ToListAsync(ct);
        return values.FirstOrDefault() is double v ? (int)Math.Round(v) : 0;
    }

    // ------------------------------------------------------------------ live

    public async Task<MetricsLiveDto> LiveAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var since15 = now.AddMinutes(-15);
        var since60 = now.AddHours(-1);

        var active = await _db.AppRequestLogs.AsNoTracking()
            .Where(r => r.At >= since15 && r.UserId != null).Select(r => r.UserId)
            .Union(_db.AppPageViews.AsNoTracking().Where(v => v.At >= since15 && v.UserId != null).Select(v => v.UserId))
            .CountAsync(ct);
        var lastHour = _db.AppRequestLogs.AsNoTracking().Where(r => r.At >= since60);
        var avg = await lastHour.Select(r => (double?)r.DurationMs).AverageAsync(ct);

        return new MetricsLiveDto
        {
            ActiveUsers15Min = active,
            RequestsLastHour = await lastHour.CountAsync(ct),
            ErrorsLastHour = await _db.AppErrorLogs.AsNoTracking().CountAsync(e => e.At >= since60, ct),
            AvgDurationMsLastHour = avg is double a ? (int)Math.Round(a) : 0,
            ServerTimeUtc = now
        };
    }

    // ------------------------------------------------------------------ series

    public async Task<MetricsSeriesDto> SeriesAsync(int? daysArg, TelemetryApp? app, CancellationToken ct)
    {
        var days = ClampDays(daysArg);
        var (today, from) = Period(days);
        var appKey = app ?? TelemetryApp.Unknown;
        var live = await _today.GetAsync(_db, ct);

        var byDay = (await _db.AppUsageDaily.AsNoTracking()
                .Where(u => u.Day >= from && u.Day < today && u.App == appKey && u.Role == "")
                .ToListAsync(ct))
            .GroupBy(u => u.Day.Date)
            .ToDictionary(g => g.Key, g => g.First());
        var todayRow = live.Usage.FirstOrDefault(u => u.App == appKey && u.Role == "");
        if (todayRow is not null) byDay[today] = todayRow;

        var dto = new MetricsSeriesDto { Days = days, App = app };
        for (var d = from; d <= today; d = d.AddDays(1))
        {
            byDay.TryGetValue(d, out var u);
            dto.Points.Add(new MetricsDayPointDto
            {
                Day = DateTime.SpecifyKind(d, DateTimeKind.Utc),
                ActiveUsers = u?.ActiveUsers ?? 0,
                Requests = u?.Requests ?? 0,
                PageViews = u?.PageViews ?? 0,
                Errors = u?.Errors ?? 0,
                AvgDurationMs = u?.AvgDurationMs ?? 0,
                P95DurationMs = u?.P95DurationMs ?? 0
            });
        }
        return dto;
    }

    // ------------------------------------------------------------------ pages / endpoints

    public async Task<List<MetricsRouteRowDto>> RoutesAsync(TelemetryRouteKind kind, int? daysArg, TelemetryApp? app, int? topArg,
        string? sort, CancellationToken ct)
    {
        var days = ClampDays(daysArg);
        var (today, from) = Period(days);
        var top = Math.Clamp(topArg ?? DefaultTop, 1, MaxPageSize);
        var appKey = app ?? TelemetryApp.Unknown;
        var live = await _today.GetAsync(_db, ct);

        var past = await _db.AppRouteDaily.AsNoTracking()
            .Where(r => r.Day >= from && r.Day < today && r.App == appKey && r.Kind == kind)
            .GroupBy(r => r.Route)
            .Select(g => new
            {
                Route = g.Key,
                Hits = g.Sum(x => x.Hits),
                Errors = g.Sum(x => x.Errors),
                MaxUsers = g.Max(x => x.Users),
                AvgWeighted = g.Sum(x => (long)x.AvgDurationMs * x.Hits),
                P95Weighted = g.Sum(x => (long)x.P95DurationMs * x.Hits)
            })
            .ToListAsync(ct);

        var merged = new Dictionary<string, RouteAcc>(StringComparer.Ordinal);
        foreach (var p in past)
        {
            merged[p.Route] = new RouteAcc { Hits = p.Hits, Errors = p.Errors, MaxUsers = p.MaxUsers, AvgWeighted = p.AvgWeighted, P95Weighted = p.P95Weighted };
        }
        foreach (var r in live.Routes.Where(r => r.App == appKey && r.Kind == kind))
        {
            if (!merged.TryGetValue(r.Route, out var acc)) merged[r.Route] = acc = new RouteAcc();
            acc.Hits += r.Hits;
            acc.Errors += r.Errors;
            acc.MaxUsers = Math.Max(acc.MaxUsers, r.Users);
            acc.AvgWeighted += (long)r.AvgDurationMs * r.Hits;
            acc.P95Weighted += (long)r.P95DurationMs * r.Hits;
        }

        var rows = merged.Select(kv => new MetricsRouteRowDto
        {
            Kind = kind,
            Route = kv.Key,
            Hits = kv.Value.Hits,
            Errors = kv.Value.Errors,
            Users = kv.Value.MaxUsers,
            AvgDurationMs = kv.Value.Hits == 0 ? 0 : (int)Math.Round((double)kv.Value.AvgWeighted / kv.Value.Hits),
            P95DurationMs = kv.Value.Hits == 0 ? 0 : (int)Math.Round((double)kv.Value.P95Weighted / kv.Value.Hits)
        });

        rows = (sort ?? "hits").Trim().ToLowerInvariant() switch
        {
            "slow" => rows.OrderByDescending(r => r.P95DurationMs).ThenByDescending(r => r.AvgDurationMs).ThenByDescending(r => r.Hits),
            "errors" => rows.OrderByDescending(r => r.Errors).ThenByDescending(r => r.Hits),
            _ => rows.OrderByDescending(r => r.Hits).ThenBy(r => r.Route)
        };
        var result = rows.Take(top).ToList();
        if (result.Count == 0) return result;

        // Distinct users of each listed route over the period (raw tables).
        if (kind == TelemetryRouteKind.Page)
        {
            var routes = result.Select(r => r.Route).ToList();
            var users = await _db.AppPageViews.AsNoTracking()
                .Where(v => v.At >= from && v.UserId != null && routes.Contains(v.Route) && (app == null || v.App == app))
                .GroupBy(v => v.Route)
                .Select(g => new { Route = g.Key, Users = g.Select(x => x.UserId).Distinct().Count() })
                .ToListAsync(ct);
            foreach (var u in users)
            {
                var row = result.FirstOrDefault(r => r.Route == u.Route);
                if (row is not null) row.Users = Math.Max(row.Users, u.Users);
            }
        }
        else
        {
            var paths = result.Select(r => SplitEndpoint(r.Route).Path).Distinct().ToList();
            var users = await _db.AppRequestLogs.AsNoTracking()
                .Where(q => q.At >= from && q.UserId != null && paths.Contains(q.Path) && (app == null || q.App == app))
                .GroupBy(q => new { q.Method, q.Path })
                .Select(g => new { g.Key.Method, g.Key.Path, Users = g.Select(x => x.UserId).Distinct().Count() })
                .ToListAsync(ct);
            foreach (var u in users)
            {
                var row = result.FirstOrDefault(r => r.Route == $"{u.Method} {u.Path}");
                if (row is not null) row.Users = Math.Max(row.Users, u.Users);
            }
        }
        return result;
    }

    private static (string Method, string Path) SplitEndpoint(string route)
    {
        var space = route.IndexOf(' ');
        return space < 0 ? ("", route) : (route[..space], route[(space + 1)..]);
    }

    private sealed class RouteAcc
    {
        public int Hits;
        public int Errors;
        public int MaxUsers;
        public long AvgWeighted;
        public long P95Weighted;
    }

    // ------------------------------------------------------------------ users

    public async Task<PageResult<MetricsUserRowDto>> UsersAsync(int? daysArg, string? q, string? role, TelemetryApp? app,
        int? pageArg, int? pageSizeArg, CancellationToken ct)
    {
        var days = ClampDays(daysArg);
        var (_, from) = Period(days);
        var page = Math.Max(1, pageArg ?? 1);
        var pageSize = Math.Clamp(pageSizeArg ?? DefaultPageSize, 1, MaxPageSize);

        var requests = _db.AppRequestLogs.AsNoTracking().Where(r => r.At >= from && r.UserId != null && (app == null || r.App == app));
        var views = _db.AppPageViews.AsNoTracking().Where(v => v.At >= from && v.UserId != null && (app == null || v.App == app));
        var errors = _db.AppErrorLogs.AsNoTracking().Where(e => e.At >= from && e.UserId != null && (app == null || e.App == app));

        var requestAgg = await requests.GroupBy(r => r.UserId!)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Last = g.Max(x => x.At), UserName = g.Max(x => x.UserName), Role = g.Max(x => x.Role) })
            .ToListAsync(ct);
        var viewAgg = await views.GroupBy(v => v.UserId!)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Last = g.Max(x => x.At), UserName = g.Max(x => x.UserName), Role = g.Max(x => x.Role) })
            .ToListAsync(ct);
        var errorAgg = await errors.GroupBy(e => e.UserId!)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var requestDays = await requests.Select(r => new { UserId = r.UserId!, Day = r.At.Date }).Distinct().ToListAsync(ct);
        var viewDays = await views.Select(v => new { UserId = v.UserId!, Day = v.At.Date }).Distinct().ToListAsync(ct);

        var stats = new Dictionary<string, UserAcc>(StringComparer.Ordinal);
        UserAcc Acc(string id) => stats.TryGetValue(id, out var a) ? a : stats[id] = new UserAcc();
        foreach (var r in requestAgg)
        {
            var a = Acc(r.UserId);
            a.Requests = r.Count;
            a.LastSeen = Max(a.LastSeen, r.Last);
            a.UserName ??= r.UserName;
            a.Role ??= r.Role;
        }
        foreach (var v in viewAgg)
        {
            var a = Acc(v.UserId);
            a.PageViews = v.Count;
            a.LastSeen = Max(a.LastSeen, v.Last);
            a.UserName ??= v.UserName;
            a.Role ??= v.Role;
        }
        foreach (var e in errorAgg)
        {
            if (stats.TryGetValue(e.UserId, out var a)) a.Errors = e.Count;
        }
        foreach (var d in requestDays.Concat(viewDays))
        {
            if (stats.TryGetValue(d.UserId, out var a)) a.Days.Add(d.Day.Date);
        }

        var ids = stats.Keys.ToList();
        var profiles = ids.Count == 0
            ? new Dictionary<string, (string? UserName, string? Name, AppRole Role, string? Designation)>()
            : (await _db.Users.AsNoTracking()
                    .Where(u => ids.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserName, u.Name, u.Role, Designation = u.Designation != null ? u.Designation.Name : null })
                    .ToListAsync(ct))
                .ToDictionary(u => u.Id, u => (u.UserName, u.Name, u.Role, u.Designation));

        var rows = stats.Select(kv =>
        {
            profiles.TryGetValue(kv.Key, out var p);
            var hasProfile = profiles.ContainsKey(kv.Key);
            return new MetricsUserRowDto
            {
                UserId = kv.Key,
                UserName = (hasProfile ? p.UserName : null) ?? kv.Value.UserName ?? kv.Key,
                Name = hasProfile ? p.Name : null,
                Role = hasProfile ? p.Role.ToString() : kv.Value.Role,
                Designation = hasProfile ? p.Designation : null,
                LastSeen = kv.Value.LastSeen,
                Requests = kv.Value.Requests,
                PageViews = kv.Value.PageViews,
                Errors = kv.Value.Errors,
                ActiveDays = kv.Value.Days.Count
            };
        });

        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            rows = rows.Where(r => Has(r.UserName, t) || Has(r.Name, t) || Has(r.Designation, t) || Has(r.Role, t));
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            var wanted = role.Trim();
            rows = rows.Where(r => string.Equals(r.Role, wanted, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = rows.OrderByDescending(r => r.LastSeen).ThenBy(r => r.UserName).ToList();
        var items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        if (items.Count > 0)
        {
            var pageIds = items.Select(i => i.UserId).ToList();
            var lastViews = await views.Where(v => pageIds.Contains(v.UserId!))
                .GroupBy(v => v.UserId)
                .Select(g => g.OrderByDescending(v => v.At).Select(v => new { v.UserId, v.At, v.Route, v.App, v.AppVersion }).First())
                .ToListAsync(ct);
            var lastRequests = await requests.Where(r => pageIds.Contains(r.UserId!))
                .GroupBy(r => r.UserId)
                .Select(g => g.OrderByDescending(r => r.At).Select(r => new { r.UserId, r.At, r.Path, r.App, r.AppVersion }).First())
                .ToListAsync(ct);

            foreach (var item in items)
            {
                var view = lastViews.FirstOrDefault(v => v.UserId == item.UserId);
                var request = lastRequests.FirstOrDefault(r => r.UserId == item.UserId);
                item.LastRoute = view?.Route ?? request?.Path;
                if (view is not null && (request is null || view.At >= request.At))
                {
                    item.LastApp = view.App;
                    item.LastAppVersion = view.AppVersion;
                }
                else if (request is not null)
                {
                    item.LastApp = request.App;
                    item.LastAppVersion = request.AppVersion;
                }
            }
        }

        return new PageResult<MetricsUserRowDto> { Items = items, Total = filtered.Count, Page = page, PageSize = pageSize };
    }

    private static bool Has(string? value, string term) =>
        value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;

    private sealed class UserAcc
    {
        public int Requests;
        public int PageViews;
        public int Errors;
        public DateTime LastSeen = DateTime.MinValue;
        public string? UserName;
        public string? Role;
        public readonly HashSet<DateTime> Days = new();
    }

    // ------------------------------------------------------------------ errors

    public async Task<PageResult<MetricsErrorGroupDto>> ErrorsAsync(int? daysArg, string? q, TelemetrySource? source, TelemetryApp? app,
        bool? resolved, int? pageArg, int? pageSizeArg, CancellationToken ct)
    {
        var days = ClampDays(daysArg);
        var (_, from) = Period(days);
        var page = Math.Max(1, pageArg ?? 1);
        var pageSize = Math.Clamp(pageSizeArg ?? DefaultPageSize, 1, MaxPageSize);

        var rows = _db.AppErrorLogs.AsNoTracking().Where(e => e.At >= from);
        if (source.HasValue) rows = rows.Where(e => e.Source == source.Value);
        if (app.HasValue) rows = rows.Where(e => e.App == app.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim().ToLower();
            rows = rows.Where(e => e.Message.ToLower().Contains(t)
                                || e.ExceptionType.ToLower().Contains(t)
                                || (e.Route != null && e.Route.ToLower().Contains(t))
                                || (e.Category != null && e.Category.ToLower().Contains(t)));
        }

        var groups = rows.GroupBy(e => e.Fingerprint).Select(g => new
        {
            Fingerprint = g.Key,
            Count = g.Count(),
            Users = g.Select(x => x.UserId).Distinct().Count(),
            FirstSeen = g.Min(x => x.At),
            LastSeen = g.Max(x => x.At),
            LastId = g.Max(x => x.Id),
            Open = g.Count(x => !x.IsResolved)
        });
        if (resolved == true) groups = groups.Where(g => g.Open == 0);
        else if (resolved == false) groups = groups.Where(g => g.Open > 0);

        var total = await groups.CountAsync(ct);
        var slice = await groups.OrderByDescending(g => g.LastSeen).ThenByDescending(g => g.LastId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var lastIds = slice.Select(g => g.LastId).ToList();
        var latest = lastIds.Count == 0
            ? new Dictionary<long, AppErrorLog>()
            : await _db.AppErrorLogs.AsNoTracking().Where(e => lastIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, ct);

        var items = slice.Select(g =>
        {
            latest.TryGetValue(g.LastId, out var e);
            return new MetricsErrorGroupDto
            {
                Fingerprint = g.Fingerprint,
                Source = e?.Source ?? TelemetrySource.Api,
                App = e?.App ?? TelemetryApp.Unknown,
                ExceptionType = e?.ExceptionType ?? "",
                Message = e?.Message ?? "",
                Route = e?.Route,
                Category = e?.Category,
                Count = g.Count,
                Users = g.Users,
                FirstSeen = g.FirstSeen,
                LastSeen = g.LastSeen,
                LastId = g.LastId,
                IsResolved = g.Open == 0,
                ResolvedBy = g.Open == 0 ? e?.ResolvedBy : null,
                ResolvedAt = g.Open == 0 ? e?.ResolvedAt : null
            };
        }).ToList();

        return new PageResult<MetricsErrorGroupDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<MetricsErrorDetailDto?> ErrorDetailAsync(long id, CancellationToken ct)
    {
        var e = await _db.AppErrorLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return null;

        var stats = await GroupStatsAsync(e.Fingerprint, ct);
        var recent = await _db.AppErrorLogs.AsNoTracking()
            .Where(x => x.Fingerprint == e.Fingerprint)
            .OrderByDescending(x => x.At).ThenByDescending(x => x.Id)
            .Take(20)
            .Select(x => new MetricsErrorOccurrenceDto
            {
                Id = x.Id,
                At = x.At,
                UserName = x.UserName,
                Role = x.Role,
                App = x.App,
                AppVersion = x.AppVersion,
                Route = x.Route,
                StatusCode = x.StatusCode,
                TraceId = x.TraceId
            })
            .ToListAsync(ct);

        return new MetricsErrorDetailDto
        {
            Id = e.Id,
            At = e.At,
            Source = e.Source,
            App = e.App,
            AppVersion = e.AppVersion,
            Fingerprint = e.Fingerprint,
            ExceptionType = e.ExceptionType,
            Message = e.Message,
            StackTrace = e.StackTrace,
            Route = e.Route,
            Method = e.Method,
            StatusCode = e.StatusCode,
            Category = e.Category,
            UserId = e.UserId,
            UserName = e.UserName,
            Role = e.Role,
            TraceId = e.TraceId,
            SessionId = e.SessionId,
            IsResolved = e.IsResolved,
            ResolvedBy = e.ResolvedBy,
            ResolvedAt = e.ResolvedAt,
            Occurrences = stats?.Count ?? 1,
            AffectedUsers = stats?.Users ?? 0,
            FirstSeen = stats?.FirstSeen ?? e.At,
            LastSeen = stats?.LastSeen ?? e.At,
            Recent = recent
        };
    }

    /// <summary>Flags every row of the group; null when the fingerprint has no rows.</summary>
    public async Task<MetricsErrorGroupDto?> ResolveAsync(string fingerprint, bool resolved, string resolvedBy, CancellationToken ct)
    {
        var fp = (fingerprint ?? "").Trim();
        if (fp.Length == 0 || fp.Length > 64) return null;

        string? by = resolved ? TelemetryHttp.Trim(resolvedBy, 256) : null;
        DateTime? at = resolved ? DateTime.UtcNow : null;
        var changed = await _db.AppErrorLogs
            .Where(e => e.Fingerprint == fp)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsResolved, resolved)
                .SetProperty(e => e.ResolvedBy, by)
                .SetProperty(e => e.ResolvedAt, at), ct);
        if (changed == 0) return null;

        return await GroupAsync(fp, ct);
    }

    /// <summary>The whole group (not limited by days).</summary>
    public async Task<MetricsErrorGroupDto?> GroupAsync(string fingerprint, CancellationToken ct)
    {
        var stats = await GroupStatsAsync(fingerprint, ct);
        if (stats is null) return null;
        var e = await _db.AppErrorLogs.AsNoTracking().FirstAsync(x => x.Id == stats.LastId, ct);
        return new MetricsErrorGroupDto
        {
            Fingerprint = fingerprint,
            Source = e.Source,
            App = e.App,
            ExceptionType = e.ExceptionType,
            Message = e.Message,
            Route = e.Route,
            Category = e.Category,
            Count = stats.Count,
            Users = stats.Users,
            FirstSeen = stats.FirstSeen,
            LastSeen = stats.LastSeen,
            LastId = stats.LastId,
            IsResolved = stats.Open == 0,
            ResolvedBy = stats.Open == 0 ? e.ResolvedBy : null,
            ResolvedAt = stats.Open == 0 ? e.ResolvedAt : null
        };
    }

    private sealed record GroupStats(int Count, int Users, DateTime FirstSeen, DateTime LastSeen, long LastId, int Open);

    private async Task<GroupStats?> GroupStatsAsync(string fingerprint, CancellationToken ct)
    {
        return await _db.AppErrorLogs.AsNoTracking()
            .Where(e => e.Fingerprint == fingerprint)
            .GroupBy(e => e.Fingerprint)
            .Select(g => new GroupStats(
                g.Count(),
                g.Select(x => x.UserId).Distinct().Count(),
                g.Min(x => x.At),
                g.Max(x => x.At),
                g.Max(x => x.Id),
                g.Count(x => !x.IsResolved)))
            .FirstOrDefaultAsync(ct);
    }
}
