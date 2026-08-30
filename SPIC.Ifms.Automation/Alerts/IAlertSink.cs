using System;
using System.Threading;
using System.Threading.Tasks;

namespace SPIC.Ifms.Automation.Alerts
{
	public interface IAlertSink
	{
		string Name { get; }

		bool Enabled { get; }

		Task SendRunSummaryAsync(RunSummary summary, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Separate from <see cref="IAlertSink"/> because a CAPTCHA prompt is urgent
	/// and interactive, while a run summary is a report. Only the push channel
	/// implements it meaningfully.
	/// </summary>
	public interface IChallengeNotifier
	{
		Task NotifyCaptchaWaitingAsync(
			int challengeRequestId,
			int round,
			DateTime expiresAtUtc,
			CancellationToken cancellationToken);
	}
}
