using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
    /// <summary>
    /// Daily cleanup for JMDO location history: deletes JMDOTrackingPoint/JMDOTrackingSession rows
    /// older than the configurable JmdoTracking:RetentionDays (appsettings.json), so retention is an
    /// adjustable setting ahead of production rather than something enforced only by hand. Runs once
    /// at startup (after a short delay, so it doesn't compete with the app's own startup work) and
    /// then once every 24 hours - a single self-contained IHostedService, no extra job-scheduling
    /// package needed.
    ///
    /// Only ever deletes sessions that are already terminal (Completed or AutoEnded) - an Active
    /// session is never touched here regardless of age, since "no point in N days" for an Active
    /// session just means it should have been auto-ended already (JMDOTrackingController.
    /// CloseStaleSessionsAsync, which runs on every Start call) - this service is pure history
    /// cleanup, not a second place that decides a session's lifecycle.
    /// </summary>
    public class TrackingRetentionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<TrackingRetentionCleanupService> _logger;

        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        public TrackingRetentionCleanupService(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<TrackingRetentionCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // Never let one bad cleanup pass crash the host - try again on the next tick.
                    _logger.LogError(ex, "JMDO tracking retention cleanup failed.");
                }
            }
            while (!stoppingToken.IsCancellationRequested
                && await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            var retentionDays = _config.GetValue<int?>("JmdoTracking:RetentionDays") ?? 180;
            if (retentionDays <= 0) return; // 0 or negative = retention disabled, keep everything

            var cutoff = DateTime.Now.AddDays(-retentionDays);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var staleSessionIds = await db.JMDOTrackingSessions
                .Where(s => s.EndTime != null && s.EndTime < cutoff
                    && (s.Status == JMDOTrackingSessionStatus.Completed || s.Status == JMDOTrackingSessionStatus.AutoEnded))
                .Select(s => s.Id)
                .ToListAsync(ct);

            if (staleSessionIds.Count == 0) return;

            var deletedPoints = await db.JMDOTrackingPoints
                .Where(p => staleSessionIds.Contains(p.TrackingSessionId))
                .ExecuteDeleteAsync(ct);

            var deletedSessions = await db.JMDOTrackingSessions
                .Where(s => staleSessionIds.Contains(s.Id))
                .ExecuteDeleteAsync(ct);

            _logger.LogInformation(
                "JMDO tracking retention cleanup: removed {Sessions} session(s) and {Points} point(s) older than {RetentionDays} day(s).",
                deletedSessions, deletedPoints, retentionDays);
        }
    }
}
