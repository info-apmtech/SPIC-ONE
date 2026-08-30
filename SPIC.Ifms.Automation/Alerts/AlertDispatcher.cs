using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Alerts
{
	public interface IAlertDispatcher
	{
		Task DispatchAsync(RunSummary summary, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Fans the run summary out to every enabled sink. Each sink is isolated:
	/// a dead SMTP host must not stop the WhatsApp message, and no alert failure
	/// is ever allowed to change the outcome of the run itself.
	/// </summary>
	public sealed class AlertDispatcher : IAlertDispatcher
	{
		private readonly IEnumerable<IAlertSink> _sinks;
		private readonly AlertOptions _options;
		private readonly ILogger<AlertDispatcher> _logger;

		public AlertDispatcher(
			IEnumerable<IAlertSink> sinks,
			IOptions<AlertOptions> options,
			ILogger<AlertDispatcher> logger)
		{
			_sinks = sinks;
			_options = options.Value;
			_logger = logger;
		}

		public async Task DispatchAsync(RunSummary summary, CancellationToken cancellationToken)
		{
			var wanted = summary.IsFailure || summary.Status == IfmsRunStatus.Failed
				? _options.NotifyOnFailure
				: _options.NotifyOnSuccess;

			if (!wanted)
			{
				_logger.LogDebug("Alerting is switched off for {Status} runs.", summary.Status);
				return;
			}

			var active = _sinks.Where(s => s.Enabled).ToList();

			if (active.Count == 0)
			{
				_logger.LogWarning(
					"No alert channel is enabled, so nobody will hear about run {RunId}. " +
					"Configure at least Alerts:Email or Alerts:Push.",
					summary.RunId);
				return;
			}

			foreach (var sink in active)
			{
				try
				{
					await sink.SendRunSummaryAsync(summary, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Alert sink {Sink} failed for run {RunId}.", sink.Name, summary.RunId);
				}
			}
		}
	}
}
