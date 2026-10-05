using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Alerts
{
	/// <summary>
	/// Deliberately gateway-agnostic. Rather than bind to one vendor's SDK it
	/// posts to whatever URL and body template you configure, with {{message}}
	/// and {{recipient}} substituted — so it works with Twilio, Gupshup, MSG91,
	/// WATI or a self-hosted bridge without a code change.
	/// </summary>
	public sealed class WhatsAppAlertSink : IAlertSink
	{
		public string Name => "WhatsApp";

		private readonly IHttpClientFactory _httpClientFactory;
		private readonly WhatsAppAlertOptions _options;
		private readonly ILogger<WhatsAppAlertSink> _logger;

		public WhatsAppAlertSink(
			IHttpClientFactory httpClientFactory,
			IOptions<AlertOptions> options,
			ILogger<WhatsAppAlertSink> logger)
		{
			_httpClientFactory = httpClientFactory;
			_options = options.Value.WhatsApp;
			_logger = logger;
		}

		public bool Enabled =>
			_options.Enabled &&
			!string.IsNullOrWhiteSpace(_options.RequestUrl) &&
			_options.Recipients.Count > 0;

		public async Task SendRunSummaryAsync(RunSummary summary, CancellationToken cancellationToken)
		{
			if (_options.FailuresOnly && !summary.IsFailure)
				return;

			var message = BuildMessage(summary);

			foreach (var recipient in _options.Recipients)
			{
				if (string.IsNullOrWhiteSpace(recipient))
					continue;

				try
				{
					await SendOneAsync(recipient.Trim(), message, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "WhatsApp alert to {Recipient} failed.", recipient);
				}
			}
		}

		public async Task SendNoticeAsync(
			string title,
			string body,
			bool urgent,
			CancellationToken cancellationToken)
		{
			// A quiet phone is exactly the case WhatsApp is good for, so this one
			// ignores FailuresOnly - it is already only sent when something is wrong.
			if (!Enabled)
				return;

			var text = $"{title}\n\n{body}";

			foreach (var recipient in _options.Recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
			{
				try
				{
					await SendOneAsync(recipient.Trim(), text, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "WhatsApp notice to {Recipient} failed.", recipient);
				}
			}
		}

		private async Task SendOneAsync(string recipient, string message, CancellationToken cancellationToken)
		{
			var client = _httpClientFactory.CreateClient("whatsapp");

			var url = Substitute(_options.RequestUrl, recipient, message, urlEncode: true);
			var method = _options.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
				? HttpMethod.Get
				: HttpMethod.Post;

			using var request = new HttpRequestMessage(method, url);

			foreach (var (key, value) in _options.Headers)
				request.Headers.TryAddWithoutValidation(key, value);

			if (method == HttpMethod.Post && !string.IsNullOrWhiteSpace(_options.BodyTemplate))
			{
				var body = Substitute(_options.BodyTemplate, recipient, message, urlEncode: false);
				request.Content = new StringContent(body, Encoding.UTF8, _options.ContentType);
			}

			using var response = await client.SendAsync(request, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"WhatsApp gateway returned {Status} for {Recipient}.",
					(int)response.StatusCode, recipient);
			}
		}

		/// <summary>
		/// JSON-escapes the message when it is going into a body template, so a
		/// quote or newline in an error text cannot produce malformed JSON.
		/// </summary>
		private static string Substitute(string template, string recipient, string message, bool urlEncode)
		{
			var encoded = urlEncode
				? Uri.EscapeDataString(message)
				: JsonEncodedText.Encode(message).ToString();

			return template
				.Replace("{{recipient}}", urlEncode ? Uri.EscapeDataString(recipient) : recipient)
				.Replace("{{message}}", encoded);
		}

		private static string BuildMessage(RunSummary summary)
		{
			var sb = new StringBuilder();
			sb.AppendLine(summary.Headline);
			sb.AppendLine();

			foreach (var report in summary.Reports)
			{
				var mark = report.Status == SPIC.Core.Entities.IfmsRunStatus.Succeeded ? "OK  " : "FAIL";

				var label = string.IsNullOrWhiteSpace(report.CompanyName)
					? report.Title
					: $"{report.CompanyName} {report.Title}";

				sb.AppendLine($"{mark} {label} — {report.TotalRows:N0} rows");
			}

			sb.AppendLine();
			sb.Append(summary.ActionRequired);

			return sb.ToString();
		}
	}
}
