using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Reports;

namespace SPIC.Ifms.Automation.Scheduling
{
	/// <summary>
	/// Picks up runs queued from the portal's "Run now" and "Retry" buttons.
	///
	/// The dashboard writes a Pending row rather than calling the automation
	/// directly, which keeps SpicAPI and this service decoupled: the API never
	/// needs to know where the browser host lives, and a queued retry survives a
	/// restart of either side.
	/// </summary>
	public sealed class ManualTriggerWorker : BackgroundService
	{
		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);

		private readonly INightlyRunService _runner;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<ManualTriggerWorker> _logger;

		public ManualTriggerWorker(
			INightlyRunService runner,
			IServiceScopeFactory scopeFactory,
			ILogger<ManualTriggerWorker> logger)
		{
			_runner = runner;
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// Anything left Running when the process died is a lie by the time we
			// come back up; clear it so the dashboard is honest.
			await ReleaseOrphanedRunsAsync(stoppingToken);

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var queued = await ClaimNextAsync(stoppingToken);

					if (queued is not null)
					{
						_logger.LogInformation(
							"Picked up manually queued run {RunId} for {ReportDate:dd-MMM-yyyy}.",
							queued.Id, queued.ReportDate);

						await _runner.RunAsync(
							queued.ReportDate,
							IfmsRunTrigger.Manual,
							attempt: 1,
							onlyJobKeys: null,
							stoppingToken,
							existingRunId: queued.Id);

						continue;
					}
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					return;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "The manual trigger poller hit an error.");
				}

				try
				{
					await Task.Delay(PollInterval, stoppingToken);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}
		}

		private async Task<IfmsAutomationRun?> ClaimNextAsync(CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			return await db.IfmsAutomationRuns
				.AsNoTracking()
				.Where(r => r.Status == IfmsRunStatus.Pending)
				.OrderBy(r => r.CreatedAt)
				.FirstOrDefaultAsync(cancellationToken);
		}

		private async Task ReleaseOrphanedRunsAsync(CancellationToken cancellationToken)
		{
			try
			{
				await using var scope = _scopeFactory.CreateAsyncScope();
				var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				var orphans = await db.IfmsAutomationRuns
					.Where(r => r.Status == IfmsRunStatus.Running)
					.ToListAsync(cancellationToken);

				if (orphans.Count == 0)
					return;

				foreach (var run in orphans)
				{
					run.Status = IfmsRunStatus.Failed;
					run.CompletedAt = DateTime.UtcNow;
					run.ErrorMessage = "The automation service restarted while this run was in progress.";
					run.UpdatedAt = DateTime.UtcNow;
				}

				await db.SaveChangesAsync(cancellationToken);

				_logger.LogWarning(
					"Closed {Count} run(s) that were left in progress by a previous shutdown.",
					orphans.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Could not tidy up orphaned runs at startup.");
			}
		}
	}
}
