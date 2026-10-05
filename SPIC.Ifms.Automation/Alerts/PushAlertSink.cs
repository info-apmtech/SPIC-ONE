using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Alerts
{
	/// <summary>
	/// Talks to SpicAPI, which is the thing the Android app already trusts.
	/// Handles both the nightly summary and the urgent "a CAPTCHA is waiting"
	/// prompt that the operator fallback raises.
	/// </summary>
	public sealed class PushAlertSink : IAlertSink, IChallengeNotifier
	{
		public string Name => "Push";

		private readonly IHttpClientFactory _httpClientFactory;
		private readonly PushAlertOptions _options;
		private readonly ILogger<PushAlertSink> _logger;

		public PushAlertSink(
			IHttpClientFactory httpClientFactory,
			IOptions<AlertOptions> options,
			ILogger<PushAlertSink> logger)
		{
			_httpClientFactory = httpClientFactory;
			_options = options.Value.Push;
			_logger = logger;
		}

		public bool Enabled =>
			_options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiBaseUrl);

		public Task SendRunSummaryAsync(RunSummary summary, CancellationToken cancellationToken) =>
			PostAsync(new
			{
				Kind = "RunSummary",
				summary.RunId,
				ReportDate = summary.ReportDate.ToString("yyyy-MM-dd"),
				Status = summary.Status.ToString(),
				Title = summary.Status == IfmsRunStatus.Succeeded
					? "IFMS import complete"
					: "IFMS import needs attention",
				Body = summary.Headline,
				Action = summary.ActionRequired,
				summary.ReportsSucceeded,
				ReportsTotal = summary.Reports.Count,
				summary.RowsInserted,
				summary.RowsUpdated,
				Failed = summary.Reports
					.Where(r => r.Status != IfmsRunStatus.Succeeded)
					.Select(r => new { r.JobKey, r.Title, r.ErrorMessage })
					.ToList()
			}, cancellationToken);

		public Task SendNoticeAsync(
			string title,
			string body,
			bool urgent,
			CancellationToken cancellationToken) =>
			PostAsync(new
			{
				Kind = "Notice",
				Title = title,
				Body = body,
				Urgent = urgent
			}, cancellationToken);

		public Task NotifyCaptchaWaitingAsync(
			int challengeRequestId,
			int round,
			DateTime expiresAtUtc,
			CancellationToken cancellationToken) =>
			PostAsync(new
			{
				Kind = "CaptchaWaiting",
				ChallengeRequestId = challengeRequestId,
				Round = round,
				ExpiresAtUtc = expiresAtUtc,
				Title = "IFMS needs the CAPTCHA",
				Body = round <= 1
					? "The automation could not read today's CAPTCHA. Open the app and type it."
					: "That code arrived too late — here is a fresh CAPTCHA. Please type it now.",
				Urgent = true
			}, cancellationToken);

		private async Task PostAsync(object payload, CancellationToken cancellationToken)
		{
			if (!Enabled)
				return;

			try
			{
				var client = _httpClientFactory.CreateClient("spic-api");
				client.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");

				if (!string.IsNullOrWhiteSpace(_options.ApiKey))
					client.DefaultRequestHeaders.Add("X-Automation-Key", _options.ApiKey);

				var path = _options.NotifyPath.TrimStart('/');
				using var response = await client.PostAsJsonAsync(path, payload, cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					_logger.LogWarning(
						"Push notification rejected by the API with {Status}.",
						(int)response.StatusCode);
				}
			}
			catch (Exception ex)
			{
				// Never let a notification failure take down a run that otherwise
				// worked. The app also polls, so a missed push is recoverable.
				_logger.LogError(ex, "Could not push the notification to SpicAPI.");
			}
		}
	}

	/// <summary>Used when push is switched off, so the solver needs no null checks.</summary>
	public sealed class NullChallengeNotifier : IChallengeNotifier
	{
		public Task NotifyCaptchaWaitingAsync(
			int challengeRequestId,
			int round,
			DateTime expiresAtUtc,
			CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
