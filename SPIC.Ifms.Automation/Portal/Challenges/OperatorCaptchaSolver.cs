using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Alerts;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Portal.Challenges
{
	/// <summary>
	/// The fallback that runs only after every automatic attempt is spent: parks
	/// the CAPTCHA image where the SPIC Android app can show it, pushes a
	/// notification, and waits for a person to type what they see.
	///
	/// It is built for a late reply. The image goes to the phone with a long
	/// window, and if the portal has expired the page by the time the answer
	/// lands, the caller reloads the login, grabs a fresh CAPTCHA and asks again —
	/// this time with a short window, because whoever answered is holding the
	/// phone right now.
	/// </summary>
	public sealed class OperatorCaptchaSolver : ICaptchaSolver
	{
		public string Name => "Operator";

		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IChallengeNotifier _notifier;
		private readonly IfmsCaptchaOptions _options;
		private readonly ILogger<OperatorCaptchaSolver> _logger;

		public OperatorCaptchaSolver(
			IServiceScopeFactory scopeFactory,
			IChallengeNotifier notifier,
			IOptions<IfmsOptions> options,
			ILogger<OperatorCaptchaSolver> logger)
		{
			_scopeFactory = scopeFactory;
			_notifier = notifier;
			_options = options.Value.Captcha;
			_logger = logger;
		}

		public Task<CaptchaAnswer?> SolveAsync(
			CaptchaChallenge challenge,
			CancellationToken cancellationToken) =>
			SolveAsync(challenge, round: 1, cancellationToken);

		/// <summary>
		/// <paramref name="round"/> 1 is the first ask, which waits a long time
		/// because nobody is awake at 04:05. Later rounds follow a reply that came
		/// in too late, so they wait only minutes.
		/// </summary>
		public async Task<CaptchaAnswer?> SolveAsync(
			CaptchaChallenge challenge,
			int round,
			CancellationToken cancellationToken)
		{
			if (challenge.ImagePng is null && string.IsNullOrWhiteSpace(challenge.Text))
				return null;

			var waitMinutes = round <= 1
				? _options.OperatorFirstWaitMinutes
				: _options.OperatorFollowUpWaitMinutes;

			var expiresAt = DateTime.UtcNow.AddMinutes(waitMinutes);

			await CancelStalePendingAsync(cancellationToken);

			int requestId;
			await using (var scope = _scopeFactory.CreateAsyncScope())
			{
				var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				var request = new IfmsChallengeRequest
				{
					RunId = challenge.RunId,
					AccountKey = challenge.AccountKey,
					CompanyName = challenge.CompanyName,
					ChallengeType = "Captcha",
					ImageBase64 = challenge.ImagePng is null
						? null
						: Convert.ToBase64String(challenge.ImagePng),
					Prompt = string.IsNullOrWhiteSpace(challenge.Text)
						? $"Type the CAPTCHA shown, to sign in as {challenge.CompanyName}."
						: challenge.Text,
					Round = round,
					FailedGuesses = challenge.FailedGuesses,
					CreatedAt = DateTime.UtcNow,
					ExpiresAt = expiresAt,
					Status = "Pending"
				};

				db.IfmsChallengeRequests.Add(request);
				await db.SaveChangesAsync(cancellationToken);
				requestId = request.Id;
			}

			_logger.LogWarning(
				"Automatic CAPTCHA solving failed; challenge {RequestId} (round {Round}) sent to the app, " +
				"waiting until {ExpiresAt:HH:mm} UTC.",
				requestId, round, expiresAt);

			// Fire-and-forget by design: a push failure must not cost us the answer,
			// because the app also polls for pending challenges.
			await _notifier.NotifyCaptchaWaitingAsync(requestId, round, expiresAt, cancellationToken);

			var poll = TimeSpan.FromSeconds(5);

			while (DateTime.UtcNow < expiresAt)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await Task.Delay(poll, cancellationToken);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

				var answer = await db.IfmsChallengeRequests
					.AsNoTracking()
					.Where(c => c.Id == requestId && c.AnsweredAt != null)
					.Select(c => c.Answer)
					.FirstOrDefaultAsync(cancellationToken);

				if (!string.IsNullOrWhiteSpace(answer))
				{
					_logger.LogInformation(
						"Challenge {RequestId} answered from the app after {Elapsed:0} minutes.",
						requestId, (DateTime.UtcNow - expiresAt.AddMinutes(-waitMinutes)).TotalMinutes);

					return new CaptchaAnswer
					{
						Value = answer.Trim(),
						Method = Name,
						ChallengeRequestId = requestId
					};
				}
			}

			await SetStatusAsync(requestId, "Expired", cancellationToken);
			_logger.LogError("Challenge {RequestId} expired with no answer from the app.", requestId);
			return null;
		}

		/// <summary>
		/// Records whether the portal accepted the human answer, so the dashboard
		/// and the app can show "accepted" rather than leaving it ambiguous.
		/// </summary>
		public Task ReportOutcomeAsync(int requestId, bool accepted, CancellationToken cancellationToken) =>
			SetStatusAsync(requestId, accepted ? "Accepted" : "Rejected", cancellationToken);

		private async Task SetStatusAsync(int requestId, string status, CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var request = await db.IfmsChallengeRequests
				.FirstOrDefaultAsync(c => c.Id == requestId, cancellationToken);

			if (request is null)
				return;

			request.Status = status;
			await db.SaveChangesAsync(cancellationToken);
		}

		/// <summary>
		/// Retires anything still pending from an earlier run so the app never
		/// shows a CAPTCHA that belongs to a dead login.
		/// </summary>
		private async Task CancelStalePendingAsync(CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var stale = await db.IfmsChallengeRequests
				.Where(c => c.Status == "Pending")
				.ToListAsync(cancellationToken);

			if (stale.Count == 0)
				return;

			foreach (var request in stale)
				request.Status = "Cancelled";

			await db.SaveChangesAsync(cancellationToken);
		}
	}
}
