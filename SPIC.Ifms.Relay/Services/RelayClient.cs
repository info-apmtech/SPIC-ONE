using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SPIC.Ifms.Relay.Services
{
	/// <summary>
	/// The calls this app makes to SpicAPI's IfmsAutomation controller (relay,
	/// CAPTCHA, status and the email-alert settings), and nothing else. Every call swallows its own exceptions into a
	/// <see cref="RelayResult"/> with a sentence a person can read, because the
	/// callers are a broadcast receiver, a foreground service and a page — none
	/// of which should ever be taken down by a flaky mobile connection.
	///
	/// Every call that carries the device token also refreshes LastSeenAt on the
	/// server, so the watcher's polling doubles as the heartbeat.
	/// </summary>
	public static class RelayClient
	{
		private static readonly HttpClient Http = new()
		{
			Timeout = TimeSpan.FromSeconds(30)
		};

		/// <summary>Forwards one SMS. The receiver retries once on failure.</summary>
		public static async Task<RelayResult> RelaySmsAsync(
			string sender,
			string body,
			DateTime receivedAtUtc,
			CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult.NotPaired;

			try
			{
				using var request = Build(HttpMethod.Post, "api/IfmsAutomation/sms");

				request.Content = JsonContent.Create(new SmsRequest
				{
					DeviceId = RelaySettings.DeviceId,
					Sender = sender,
					Body = body,
					ReceivedAt = receivedAtUtc
				}, RelayJson.Default.SmsRequest);

				using var response = await Http.SendAsync(request, cancellationToken);
				return await ToResultAsync(response, "Forwarded.", cancellationToken);
			}
			catch (Exception ex)
			{
				return RelayResult.From(ex);
			}
		}

		/// <summary>
		/// The CAPTCHA waiting for a person. Success with a null value means the
		/// server answered and nothing is pending; failure means we do not know.
		/// </summary>
		public static async Task<RelayResult<PendingChallenge>> GetPendingChallengeAsync(
			CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult<PendingChallenge>.NotPaired;

			try
			{
				using var request = Build(HttpMethod.Get, "api/IfmsAutomation/challenge/pending");
				using var response = await Http.SendAsync(request, cancellationToken);
				var json = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
					return new RelayResult<PendingChallenge>(false, Describe(response.StatusCode, json), null);

				Touch();

				var challenge = string.IsNullOrWhiteSpace(json) || json == "null"
					? null
					: JsonSerializer.Deserialize(json, RelayJson.Default.PendingChallenge);

				return new RelayResult<PendingChallenge>(true, "OK", challenge);
			}
			catch (Exception ex)
			{
				return new RelayResult<PendingChallenge>(false, RelayResult.From(ex).Message, null);
			}
		}

		/// <summary>Submits a human reading of the CAPTCHA. The message is the server's own.</summary>
		public static async Task<RelayResult> AnswerChallengeAsync(
			int challengeId,
			string answer,
			CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult.NotPaired;

			try
			{
				using var request = Build(
					HttpMethod.Post, $"api/IfmsAutomation/challenge/{challengeId}/answer");

				request.Content = JsonContent.Create(
					new AnswerRequest { Answer = answer }, RelayJson.Default.AnswerRequest);

				using var response = await Http.SendAsync(request, cancellationToken);
				return await ToResultAsync(response, "Sent. The download is continuing.", cancellationToken);
			}
			catch (Exception ex)
			{
				return RelayResult.From(ex);
			}
		}

		/// <summary>
		/// Pairs this handset and stores the token it is issued. The pairing key
		/// is used here and nowhere else, so it can be rotated on the server
		/// without touching phones that are already paired.
		/// </summary>
		public static async Task<RelayResult> RegisterAsync(
			string apiBase,
			string pairingKey,
			CancellationToken cancellationToken = default)
		{
			try
			{
				var baseUrl = apiBase.TrimEnd('/');

				using var request = new HttpRequestMessage(
					HttpMethod.Post, $"{baseUrl}/api/IfmsAutomation/devices/register");

				request.Headers.Add("X-Device-Key", pairingKey);

				request.Content = JsonContent.Create(new RegisterRequest
				{
					DeviceId = RelaySettings.DeviceId,
					DeviceName = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
					AppVersion = AppInfo.Current.VersionString,
					Platform = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}"
				}, RelayJson.Default.RegisterRequest);

				using var response = await Http.SendAsync(request, cancellationToken);
				var body = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					return new RelayResult(false, response.StatusCode == HttpStatusCode.Unauthorized
						? "The server rejected that pairing key."
						: $"Pairing failed: {Describe(response.StatusCode, body)}");
				}

				var result = JsonSerializer.Deserialize(body, RelayJson.Default.RegisterResult);

				if (string.IsNullOrWhiteSpace(result?.Token))
					return new RelayResult(false, "The server did not return a device token.");

				RelaySettings.DeviceToken = result.Token;
				Touch();

				return new RelayResult(true, result.ReplacedExisting
					? "Re-paired. The previous token for this phone no longer works."
					: "Paired.");
			}
			catch (Exception ex)
			{
				return RelayResult.From(ex);
			}
		}

		/// <summary>
		/// An explicit "I am alive". The watcher's polls already count, so this is
		/// only used when a poll has not happened — after pairing, for instance.
		/// </summary>
		public static async Task<RelayResult> HeartbeatAsync(CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult.NotPaired;

			try
			{
				using var request = Build(HttpMethod.Post, "api/IfmsAutomation/devices/heartbeat");
				using var response = await Http.SendAsync(request, cancellationToken);
				return await ToResultAsync(response, "Heartbeat sent.", cancellationToken);
			}
			catch (Exception ex)
			{
				return RelayResult.From(ex);
			}
		}

		/// <summary>The pending CAPTCHA, if any, and how the last run went, in one call.</summary>
		public static async Task<RelayResult<RelayStatus>> GetStatusAsync(
			CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult<RelayStatus>.NotPaired;

			try
			{
				using var request = Build(HttpMethod.Get, "api/IfmsAutomation/status");
				using var response = await Http.SendAsync(request, cancellationToken);
				var json = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
					return new RelayResult<RelayStatus>(false, Describe(response.StatusCode, json), null);

				Touch();

				var status = JsonSerializer.Deserialize(json, RelayJson.Default.RelayStatus);

				return status is null
					? new RelayResult<RelayStatus>(false, "The server returned an empty status.", null)
					: new RelayResult<RelayStatus>(true, "OK", status);
			}
			catch (Exception ex)
			{
				return new RelayResult<RelayStatus>(false, RelayResult.From(ex).Message, null);
			}
		}

		/// <summary>
		/// What the "Test connection" button runs: the cheapest authenticated call
		/// there is, turned into a sentence about whether the server knows us.
		/// </summary>
		public static async Task<RelayResult> TestConnectionAsync(CancellationToken cancellationToken = default)
		{
			var status = await GetStatusAsync(cancellationToken);

			if (!status.Success)
				return new RelayResult(false, status.Message);

			var headline = string.IsNullOrWhiteSpace(status.Value?.Headline)
				? "The server accepted this phone's token."
				: status.Value.Headline;

			return new RelayResult(true, $"Connected. {headline}");
		}

		// ------------------------------------------------------------- alerts

		/// <summary>
		/// The nightly automation's email-alert settings. The password is never
		/// part of the answer; <see cref="AlertSettings.HasPassword"/> is all the
		/// phone gets to know.
		/// </summary>
		public static async Task<RelayResult<AlertSettings>> GetAlertSettingsAsync(
			CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult<AlertSettings>.NotPaired;

			try
			{
				using var request = Build(HttpMethod.Get, "api/IfmsAutomation/alerts/email");
				using var response = await Http.SendAsync(request, cancellationToken);
				return await ToAlertSettingsAsync(response, cancellationToken);
			}
			catch (Exception ex)
			{
				return new RelayResult<AlertSettings>(false, RelayResult.From(ex).Message, null);
			}
		}

		/// <summary>
		/// Replaces the alert settings. The password inside <paramref name="update"/>
		/// goes over the wire once and is not logged or kept here; a null one
		/// tells the server to leave the stored password alone. The answer is the
		/// saved state, so the page can redraw from it.
		/// </summary>
		public static async Task<RelayResult<AlertSettings>> SaveAlertSettingsAsync(
			AlertSettingsUpdate update,
			CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult<AlertSettings>.NotPaired;

			try
			{
				using var request = Build(HttpMethod.Put, "api/IfmsAutomation/alerts/email");
				request.Content = JsonContent.Create(update, RelayJson.Default.AlertSettingsUpdate);

				using var response = await Http.SendAsync(request, cancellationToken);
				return await ToAlertSettingsAsync(response, cancellationToken);
			}
			catch (Exception ex)
			{
				return new RelayResult<AlertSettings>(false, RelayResult.From(ex).Message, null);
			}
		}

		/// <summary>
		/// Asks the automation to send a test email. It answers 202 straight
		/// away and sends within about twenty seconds; the outcome shows up as
		/// LastTestAt / LastTestResult on the next <see cref="GetAlertSettingsAsync"/>.
		/// </summary>
		public static async Task<RelayResult> RequestTestEmailAsync(CancellationToken cancellationToken = default)
		{
			if (!RelaySettings.IsConfigured)
				return RelayResult.NotPaired;

			try
			{
				using var request = Build(HttpMethod.Post, "api/IfmsAutomation/alerts/email/test");
				using var response = await Http.SendAsync(request, cancellationToken);
				var body = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
					return new RelayResult(false, Describe(response.StatusCode, body));

				Touch();

				// The 202 body carries the server's own sentence; prefer it.
				var message = ServerMessageOf(body);
				return new RelayResult(true, string.IsNullOrWhiteSpace(message) ? "Test queued." : message);
			}
			catch (Exception ex)
			{
				return RelayResult.From(ex);
			}
		}

		/// <summary>GET and PUT both answer with the full settings shape.</summary>
		private static async Task<RelayResult<AlertSettings>> ToAlertSettingsAsync(
			HttpResponseMessage response,
			CancellationToken cancellationToken)
		{
			var json = await response.Content.ReadAsStringAsync(cancellationToken);

			if (!response.IsSuccessStatusCode)
				return new RelayResult<AlertSettings>(false, Describe(response.StatusCode, json), null);

			Touch();

			var settings = string.IsNullOrWhiteSpace(json) || json == "null"
				? null
				: JsonSerializer.Deserialize(json, RelayJson.Default.AlertSettings);

			return settings is null
				? new RelayResult<AlertSettings>(false, "The server returned no alert settings.", null)
				: new RelayResult<AlertSettings>(true, "OK", settings);
		}

		/// <summary>The Message of a <c>{ Success, Message }</c> body, or null if it is not one.</summary>
		private static string? ServerMessageOf(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return null;

			try
			{
				return JsonSerializer.Deserialize(body, RelayJson.Default.ServerMessage)?.Message;
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static HttpRequestMessage Build(HttpMethod method, string path)
		{
			var baseUrl = RelaySettings.ApiBase.TrimEnd('/');

			var request = new HttpRequestMessage(method, $"{baseUrl}/{path}");
			request.Headers.Add("X-Device-Token", RelaySettings.DeviceToken);

			return request;
		}

		private static async Task<RelayResult> ToResultAsync(
			HttpResponseMessage response,
			string successMessage,
			CancellationToken cancellationToken)
		{
			if (response.IsSuccessStatusCode)
			{
				Touch();
				return new RelayResult(true, successMessage);
			}

			var body = await response.Content.ReadAsStringAsync(cancellationToken);
			return new RelayResult(false, Describe(response.StatusCode, body));
		}

		/// <summary>
		/// The controller answers failures with <c>{ Success, Message }</c>. Pull
		/// the message out when it is there; otherwise fall back to the status.
		/// </summary>
		private static string Describe(HttpStatusCode status, string body)
		{
			if (!string.IsNullOrWhiteSpace(body))
			{
				try
				{
					var parsed = JsonSerializer.Deserialize(body, RelayJson.Default.ServerMessage);

					if (!string.IsNullOrWhiteSpace(parsed?.Message))
						return parsed.Message;
				}
				catch (JsonException)
				{
					// Plain-text body; use it as is below.
				}

				if (body.Length <= 200 && !body.TrimStart().StartsWith('<'))
					return body.Trim();
			}

			return status switch
			{
				HttpStatusCode.Unauthorized => "The server no longer recognises this phone. Pair it again.",
				HttpStatusCode.NotFound => "The server has no such endpoint. Check the API address.",
				_ => $"The server answered {(int)status} {status}."
			};
		}

		/// <summary>Records a successful round trip; the Home page shows it as "last contact".</summary>
		private static void Touch() => RelaySettings.LastContactUtc = DateTime.UtcNow;
	}

	/// <summary>The outcome of one call, with a sentence for the person.</summary>
	public sealed record RelayResult(bool Success, string Message)
	{
		public static readonly RelayResult NotPaired = new(false, "This phone is not paired.");

		public static RelayResult From(Exception ex) => ex switch
		{
			TaskCanceledException => new RelayResult(false, "Timed out talking to the server."),
			HttpRequestException => new RelayResult(false, $"Could not reach the server: {ex.Message}"),
			_ => new RelayResult(false, $"Unexpected error: {ex.Message}")
		};
	}

	public sealed record RelayResult<T>(bool Success, string Message, T? Value) where T : class
	{
		public static readonly RelayResult<T> NotPaired = new(false, RelayResult.NotPaired.Message, null);
	}
}
