using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Options;
using SPIC.Ifms.Automation.Reports;

namespace SPIC.Ifms.Automation.Scheduling
{
	/// <summary>
	/// Fires the nightly run at the configured local time and nowhere else.
	///
	/// Written against a real time zone rather than the machine's clock, because
	/// the portal opens at 04:00 India time whatever the server thinks the time
	/// is — a VPS defaulting to UTC would otherwise run five and a half hours out.
	/// </summary>
	public sealed class DailyScheduleWorker : BackgroundService
	{
		private readonly INightlyRunService _runner;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ScheduleOptions _options;
		private readonly ILogger<DailyScheduleWorker> _logger;
		private readonly TimeZoneInfo _timeZone;
		private readonly TimeSpan _runAt;

		public DailyScheduleWorker(
			INightlyRunService runner,
			IServiceScopeFactory scopeFactory,
			IOptions<ScheduleOptions> options,
			ILogger<DailyScheduleWorker> logger)
		{
			_runner = runner;
			_scopeFactory = scopeFactory;
			_options = options.Value;
			_logger = logger;
			_timeZone = ResolveTimeZone(_options.TimeZone, logger);
			_runAt = ParseRunAt(_options.RunAt, logger);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			if (!_options.Enabled)
			{
				_logger.LogWarning("The schedule is disabled; no automatic runs will happen.");
				return;
			}

			_logger.LogInformation(
				"IFMS automation is armed for {RunAt:hh\\:mm} {TimeZone} each day.",
				_runAt, _timeZone.Id);

			if (_options.RunOnStartup)
			{
				_logger.LogInformation("RunOnStartup is set; running once now.");
				await RunWithRetriesAsync(IfmsRunTrigger.Manual, stoppingToken);
			}
			else if (_options.CatchUpMissedRuns && await ShouldCatchUpAsync(stoppingToken))
			{
				_logger.LogWarning(
					"Today's scheduled run was missed and is still inside the {Hours}h catch-up window; running now.",
					_options.CatchUpWindowHours);

				await RunWithRetriesAsync(IfmsRunTrigger.Schedule, stoppingToken);
			}

			while (!stoppingToken.IsCancellationRequested)
			{
				var delay = TimeUntilNextRun();

				_logger.LogInformation(
					"Next IFMS run in {Hours}h {Minutes}m (at {At:dd-MMM HH:mm} {Zone}).",
					(int)delay.TotalHours, delay.Minutes,
					NowInZone().Add(delay), _timeZone.Id);

				try
				{
					await Task.Delay(delay, stoppingToken);
				}
				catch (OperationCanceledException)
				{
					return;
				}

				await RunWithRetriesAsync(IfmsRunTrigger.Schedule, stoppingToken);
			}
		}

		/// <summary>
		/// Retries the whole run — new browser, new login — when a night fails.
		/// Deliberately whole-run: nearly every transient failure at this hour is
		/// the portal being half-awake, and a fresh session is the cure.
		/// </summary>
		private async Task RunWithRetriesAsync(IfmsRunTrigger trigger, CancellationToken stoppingToken)
		{
			var reportDate = NowInZone().Date.AddDays(_options.ReportDateOffsetDays);
			var maxAttempts = Math.Max(1, _options.MaxAttempts);

			for (var attempt = 1; attempt <= maxAttempts; attempt++)
			{
				if (stoppingToken.IsCancellationRequested)
					return;

				try
				{
					var summary = await _runner.RunAsync(
						reportDate,
						attempt == 1 ? trigger : IfmsRunTrigger.Retry,
						attempt,
						onlyJobKeys: null,
						stoppingToken);

					if (summary.Status == IfmsRunStatus.Succeeded)
						return;

					// Nothing about a missing login gets better by waiting fifteen
					// minutes and trying again; it just produces three identical
					// alerts and delays the operator noticing the real message.
					if (summary.IsConfigurationProblem)
					{
						_logger.LogError(
							"Run {RunId} failed because the automation is not fully configured. " +
							"Not retrying — fix the configuration and use Run now, or wait for " +
							"tomorrow's schedule.",
							summary.RunId);
						return;
					}

					// A partial success has already imported what it could and has
					// already alerted. Retrying the whole thing would re-download
					// the reports that worked, so stop here and let the operator
					// pick up the named stragglers.
					if (summary.Status == IfmsRunStatus.PartiallySucceeded)
					{
						_logger.LogWarning(
							"Run {RunId} was partial; not retrying automatically so the successful " +
							"imports are not repeated.", summary.RunId);
						return;
					}
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					return;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Attempt {Attempt} of the nightly run threw.", attempt);
				}

				if (attempt < maxAttempts)
				{
					var wait = TimeSpan.FromMinutes(Math.Max(1, _options.RetryDelayMinutes));
					_logger.LogInformation("Retrying the whole run in {Minutes} minutes.", wait.TotalMinutes);

					try
					{
						await Task.Delay(wait, stoppingToken);
					}
					catch (OperationCanceledException)
					{
						return;
					}
				}
			}

			_logger.LogError("The nightly run failed all {Attempts} attempts.", maxAttempts);
		}

		/// <summary>
		/// True when today's slot has passed, we are still within the catch-up
		/// window, and no successful run exists for today's report date.
		/// </summary>
		private async Task<bool> ShouldCatchUpAsync(CancellationToken cancellationToken)
		{
			var now = NowInZone();
			var slot = now.Date.Add(_runAt);

			if (now < slot)
				return false;

			if (now - slot > TimeSpan.FromHours(Math.Max(1, _options.CatchUpWindowHours)))
				return false;

			var reportDate = now.Date.AddDays(_options.ReportDateOffsetDays);

			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var alreadyDone = await db.IfmsAutomationRuns
				.AnyAsync(
					r => r.ReportDate == reportDate.Date &&
						 (r.Status == IfmsRunStatus.Succeeded || r.Status == IfmsRunStatus.PartiallySucceeded),
					cancellationToken);

			return !alreadyDone;
		}

		private TimeSpan TimeUntilNextRun()
		{
			var now = NowInZone();
			var next = now.Date.Add(_runAt);

			if (next <= now)
				next = next.AddDays(1);

			var delay = next - now;

			// Never return zero or negative: a mis-set clock would spin the loop.
			return delay < TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : delay;
		}

		private DateTime NowInZone() =>
			TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

		/// <summary>
		/// Accepts either the Windows id or the IANA id, so one appsettings.json
		/// works on a Windows desktop and a Linux VPS without editing.
		/// </summary>
		private static TimeZoneInfo ResolveTimeZone(string id, ILogger logger)
		{
			var candidates = new[]
			{
				id,
				id == "Asia/Kolkata" ? "India Standard Time" : "Asia/Kolkata"
			};

			foreach (var candidate in candidates.Distinct())
			{
				try
				{
					return TimeZoneInfo.FindSystemTimeZoneById(candidate);
				}
				catch (TimeZoneNotFoundException)
				{
					// Try the next spelling.
				}
				catch (InvalidTimeZoneException)
				{
					// Try the next spelling.
				}
			}

			logger.LogError(
				"Time zone '{Id}' was not found on this machine; falling back to the local zone. " +
				"On Linux install tzdata, on Windows use 'India Standard Time'.", id);

			return TimeZoneInfo.Local;
		}

		private static TimeSpan ParseRunAt(string value, ILogger logger)
		{
			if (TimeSpan.TryParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture, out var parsed))
				return parsed;

			if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out parsed))
				return parsed;

			logger.LogError("Schedule:RunAt '{Value}' is not a HH:mm time; defaulting to 04:05.", value);
			return new TimeSpan(4, 5, 0);
		}
	}
}
