using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Telemetry;

/// <summary>
/// Keeps the daily tables current and the raw tables small (plan section 3). First run 1 min after
/// start, then every Telemetry:RollupMinutes: today and yesterday (UTC) are recomputed (delete + insert
/// in one transaction). The first run also fills any day of the raw-retention window that has no
/// rollup yet (API down over midnight). Purge by Telemetry:Retention once a day from 03:00 UTC (and on
/// the first run), in batches of 5000 rows so a big table is never locked for long.
/// </summary>
public sealed class TelemetryRollupService : BackgroundService
{
    private const int PurgeBatch = 5000;

    private readonly IServiceScopeFactory _scopes;
    private readonly IOptionsMonitor<TelemetryOptions> _options;
    private readonly ILogger<TelemetryRollupService> _logger;
    private DateTime? _lastPurgeDay;
    private bool _backfilled;

    public TelemetryRollupService(IServiceScopeFactory scopes, IOptionsMonitor<TelemetryOptions> options, ILogger<TelemetryRollupService> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_options.CurrentValue.Enabled)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Telemetry rollup failed: {Message}", ex.GetBaseException().Message);
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(Math.Clamp(_options.CurrentValue.RollupMinutes, 1, 1440)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var _ = TelemetryScope.Suppress();
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        var today = DateTime.UtcNow.Date;
        var days = new List<DateTime> { today.AddDays(-1), today };

        if (!_backfilled)
        {
            days.InsertRange(0, await MissingDaysAsync(db, today, ct));
            _backfilled = true;
        }

        foreach (var day in days.Distinct().OrderBy(d => d))
        {
            await RollupDayAsync(db, day, ct);
        }
        _logger.LogInformation("Telemetry rollup: {Count} day(s) recomputed up to {Day:yyyy-MM-dd}", days.Distinct().Count(), today);

        var now = DateTime.UtcNow;
        if (_lastPurgeDay is null || (_lastPurgeDay < now.Date && now.Hour >= 3))
        {
            await PurgeAsync(db, now, ct);
            _lastPurgeDay = now.Date;
        }
    }

    /// <summary>Days of the raw window with request / page-view rows but no all / all rollup row.</summary>
    private async Task<List<DateTime>> MissingDaysAsync(AppDbContext db, DateTime today, CancellationToken ct)
    {
        var from = today.AddDays(-Math.Max(1, _options.CurrentValue.Retention.RequestDays));
        var yesterday = today.AddDays(-1);

        var rolled = await db.AppUsageDaily.AsNoTracking()
            .Where(u => u.Day >= from && u.Day < yesterday && u.App == TelemetryApp.Unknown && u.Role == "")
            .Select(u => u.Day)
            .ToListAsync(ct);
        var requestDays = await db.AppRequestLogs.AsNoTracking()
            .Where(r => r.At >= from && r.At < yesterday)
            .Select(r => r.At.Date)
            .Distinct()
            .ToListAsync(ct);
        var viewDays = await db.AppPageViews.AsNoTracking()
            .Where(v => v.At >= from && v.At < yesterday)
            .Select(v => v.At.Date)
            .Distinct()
            .ToListAsync(ct);

        var done = rolled.Select(d => d.Date).ToHashSet();
        return requestDays.Concat(viewDays).Select(d => d.Date).Distinct().Where(d => !done.Contains(d)).OrderBy(d => d).ToList();
    }

    private static async Task RollupDayAsync(AppDbContext db, DateTime day, CancellationToken ct)
    {
        var result = await TelemetryAggregator.ComputeDayAsync(db, day, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.AppUsageDaily.Where(u => u.Day == result.Day).ExecuteDeleteAsync(ct);
        await db.AppRouteDaily.Where(r => r.Day == result.Day).ExecuteDeleteAsync(ct);
        if (result.Usage.Count > 0) db.AppUsageDaily.AddRange(result.Usage);
        if (result.Routes.Count > 0) db.AppRouteDaily.AddRange(result.Routes);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        db.ChangeTracker.Clear();
    }

    private async Task PurgeAsync(AppDbContext db, DateTime now, CancellationToken ct)
    {
        var r = _options.CurrentValue.Retention;
        var requestCut = now.AddDays(-Math.Max(1, r.RequestDays));
        var viewCut = now.AddDays(-Math.Max(1, r.PageViewDays));
        var errorCut = now.AddDays(-Math.Max(1, r.ErrorDays));
        var dailyCut = now.Date.AddDays(-Math.Max(1, r.DailyDays));

        var removed = 0;
        removed += await PurgeBatchedAsync(() => db.AppRequestLogs.Where(x => x.At < requestCut).OrderBy(x => x.Id).Take(PurgeBatch).ExecuteDeleteAsync(ct));
        removed += await PurgeBatchedAsync(() => db.AppPageViews.Where(x => x.At < viewCut).OrderBy(x => x.Id).Take(PurgeBatch).ExecuteDeleteAsync(ct));
        removed += await PurgeBatchedAsync(() => db.AppErrorLogs.Where(x => x.At < errorCut).OrderBy(x => x.Id).Take(PurgeBatch).ExecuteDeleteAsync(ct));
        removed += await PurgeBatchedAsync(() => db.AppUsageDaily.Where(x => x.Day < dailyCut).OrderBy(x => x.Id).Take(PurgeBatch).ExecuteDeleteAsync(ct));
        removed += await PurgeBatchedAsync(() => db.AppRouteDaily.Where(x => x.Day < dailyCut).OrderBy(x => x.Id).Take(PurgeBatch).ExecuteDeleteAsync(ct));

        _logger.LogInformation("Telemetry purge: {Count} expired row(s) removed", removed);
    }

    private static async Task<int> PurgeBatchedAsync(Func<Task<int>> deleteBatch)
    {
        var total = 0;
        while (true)
        {
            var n = await deleteBatch();
            total += n;
            if (n < PurgeBatch) return total;
        }
    }
}
