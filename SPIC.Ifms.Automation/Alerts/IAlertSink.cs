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

		/// <summary>
		/// A short standalone message that is not about a run — today, that the
		/// relay phone has gone quiet.
		///
		/// Deliberately separate from the run summary: this fires in the evening,
		/// while there is still time to plug the phone in, rather than at 04:05
		/// when it is already too late to matter.
		/// </summary>
		Task SendNoticeAsync(
			string title,
			string body,
			bool urgent,
			CancellationToken cancellationToken)
			=> Task.CompletedTask;
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
