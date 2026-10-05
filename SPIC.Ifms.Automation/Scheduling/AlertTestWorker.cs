using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Services;
using SPIC.Ifms.Automation.Alerts;

namespace SPIC.Ifms.Automation.Scheduling
{
	/// <summary>
	/// Sends the test email somebody asked for from the phone.
	///
	/// The request is a flag on the settings row rather than a call into this
	/// service, for the same reason manual runs are queued: SpicAPI has no SMTP
	/// access and no idea where this host is, and a flag survives a restart of
	/// either side. Polled at the same cadence as the manual trigger so the
	/// "about twenty seconds" promise the API makes holds.
	/// </summary>
	public sealed class AlertTestWorker : BackgroundService
	{
		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);

		private readonly EmailAlertSink _email;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<AlertTestWorker> _logger;

		public AlertTestWorker(
			EmailAlertSink email,
			IServiceScopeFactory scopeFactory,
			ILogger<AlertTestWorker> logger)
		{
			_email = email;
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await SendPendingTestAsync(stoppingToken);
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					return;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "The alert test poller hit an error.");
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

		private async Task SendPendingTestAsync(CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var store = scope.ServiceProvider.GetRequiredService<IIfmsAlertSettingsStore>();

			var row = await store.GetAsync(cancellationToken);

			if (row?.TestRequestedAt is null)
				return;

			// A result newer than the request means this one is already done and
			// the API simply has not cleared the flag yet.
			if (row.LastTestAt is not null && row.LastTestAt >= row.TestRequestedAt)
				return;

			_logger.LogInformation(
				"Test email requested by {RequestedBy} at {RequestedAt:HH:mm:ss}; sending.",
				row.TestRequestedBy ?? "unknown", row.TestRequestedAt);

			var result = await _email.SendTestAsync(cancellationToken);

			await store.RecordTestResultAsync(result, cancellationToken);

			_logger.LogInformation("Test email result: {Result}", result);
		}
	}
}
