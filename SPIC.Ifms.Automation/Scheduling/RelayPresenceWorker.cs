using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.Interfaces;
using SPIC.Ifms.Automation.Alerts;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Scheduling
{
	/// <summary>
	/// Watches whether the relay phone is still alive, and says so while there is
	/// still time to do something about it.
	///
	/// Without this the phone is a silent single point of failure: it can be off,
	/// out of signal, or have had the app killed by Android's battery optimiser,
	/// and nothing reveals that until the OTP never arrives at 04:05 and the whole
	/// night's import is lost. The phone checks in every minute, so a few hours of
	/// silence is unambiguous.
	/// </summary>
	public sealed class RelayPresenceWorker : BackgroundService
	{
		private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

		/// <summary>
		/// Long enough that a phone briefly out of signal does not nag, short
		/// enough that an evening alert still leaves the night to fix it.
		/// </summary>
		private static readonly TimeSpan RepeatAlertAfter = TimeSpan.FromHours(6);

		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IAlertDispatcher _alerts;
		private readonly IfmsOptions _options;
		private readonly ILogger<RelayPresenceWorker> _logger;

		private DateTime? _lastAlertedAt;
		private bool _wasHealthy = true;

		public RelayPresenceWorker(
			IServiceScopeFactory scopeFactory,
			IAlertDispatcher alerts,
			IOptions<IfmsOptions> options,
			ILogger<RelayPresenceWorker> logger)
		{
			_scopeFactory = scopeFactory;
			_alerts = alerts;
			_options = options.Value;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// Nothing to watch until a phone has been paired at least once, and a
			// fresh install should not start complaining about a phone that has
			// never existed.
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await CheckAsync(stoppingToken);
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					return;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "The relay presence check failed.");
				}

				try
				{
					await Task.Delay(CheckInterval, stoppingToken);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}
		}

		private async Task CheckAsync(CancellationToken cancellationToken)
		{
			var staleAfter = TimeSpan.FromHours(Math.Max(1, _options.RelayStaleAfterHours));

			await using var scope = _scopeFactory.CreateAsyncScope();
			var devices = scope.ServiceProvider.GetRequiredService<IIfmsRelayDeviceStore>();

			var all = await devices.ListAsync(_options.RelayStaleAfterHours, cancellationToken);
			var active = all.Where(d => d.IsActive).ToList();

			if (active.Count == 0)
			{
				// No phone paired at all. Worth one line in the log, but not an
				// alert every half hour on a system nobody has finished setting up.
				_logger.LogDebug("No relay device is paired yet.");
				return;
			}

			var lastSeen = await devices.LastSeenAcrossActiveAsync(cancellationToken);
			var quietFor = lastSeen.HasValue ? DateTime.UtcNow - lastSeen.Value : (TimeSpan?)null;
			var healthy = quietFor.HasValue && quietFor < staleAfter;

			if (healthy)
			{
				// Say so once on recovery rather than staying silent, so whoever
				// acted on the alert knows it worked.
				if (!_wasHealthy)
				{
					_logger.LogInformation("The relay phone is checking in again.");

					await _alerts.NoticeAsync(
						"IFMS relay phone is back",
						"The paired phone is checking in again. Nothing further to do.",
						urgent: false,
						cancellationToken);
				}

				_wasHealthy = true;
				_lastAlertedAt = null;
				return;
			}

			_wasHealthy = false;

			if (_lastAlertedAt.HasValue && DateTime.UtcNow - _lastAlertedAt.Value < RepeatAlertAfter)
				return;

			var names = string.Join(", ", active.Select(d => d.DeviceName));

			var quietText = quietFor.HasValue
				? $"{quietFor.Value.TotalHours:0.#} hours"
				: "as long as we have been watching";

			_logger.LogError(
				"The relay phone has not checked in for {Quiet}. The 04:05 login will fail " +
				"without it.", quietText);

			await _alerts.NoticeAsync(
				"IFMS relay phone has gone quiet",
				$"No check-in from {names} for {quietText}.\n\n" +
				"The phone forwards the IFMS one-time password, so tonight's 04:05 run will " +
				"stall at the login without it.\n\n" +
				"Check the phone is on, has signal and data, and that the SPIC app is running. " +
				"Android's battery optimiser is the usual culprit — exclude the app from it.",
				urgent: true,
				cancellationToken);

			_lastAlertedAt = DateTime.UtcNow;
		}
	}
}
