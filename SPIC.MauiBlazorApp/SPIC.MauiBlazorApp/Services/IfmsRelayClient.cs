using System.Net.Http.Json;
using System.Text.Json;

namespace SPIC.MauiBlazorApp.Services
{
	/// <summary>
	/// A tiny, dependency-free HTTP client for the IFMS endpoints.
	///
	/// It deliberately does not go through the app's <see cref="HttpClient"/>
	/// registration: that one attaches the signed-in user's JWT, and everything
	/// here has to run with nobody signed in.
	/// </summary>
	public static class IfmsRelayClient
	{
		private static readonly HttpClient Http = new()
		{
			Timeout = TimeSpan.FromSeconds(30)
		};

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};

		/// <summary>Forwards one SMS. Returns false on any failure; the caller retries.</summary>
		public static async Task<bool> RelaySmsAsync(
			string sender,
			string body,
			DateTime receivedAtUtc,
			CancellationToken cancellationToken = default)
		{
			if (!IfmsRelaySettings.IsConfigured)
				return false;

			try
			{
				using var request = Build(HttpMethod.Post, "api/IfmsAutomation/sms");

				request.Content = JsonContent.Create(new
				{
					DeviceId = IfmsRelaySettings.DeviceId,
					Sender = sender,
					Body = body,
					ReceivedAt = receivedAtUtc
				});

				using var response = await Http.SendAsync(request, cancellationToken);
				return response.IsSuccessStatusCode;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>The CAPTCHA waiting for a person, or null.</summary>
		public static async Task<PendingChallenge?> GetPendingChallengeAsync(
			CancellationToken cancellationToken = default)
		{
			if (!IfmsRelaySettings.IsConfigured)
				return null;

			try
			{
				using var request = Build(HttpMethod.Get, "api/IfmsAutomation/challenge/pending");
				using var response = await Http.SendAsync(request, cancellationToken);

				if (!response.IsSuccessStatusCode)
					return null;

				var json = await response.Content.ReadAsStringAsync(cancellationToken);

				return string.IsNullOrWhiteSpace(json) || json == "null"
					? null
					: JsonSerializer.Deserialize<PendingChallenge>(json, JsonOptions);
			}
			catch
			{
				return null;
			}
		}

		public static async Task<bool> AnswerChallengeAsync(
			int challengeId,
			string answer,
			CancellationToken cancellationToken = default)
		{
			if (!IfmsRelaySettings.IsConfigured)
				return false;

			try
			{
				using var request = Build(
					HttpMethod.Post, $"api/IfmsAutomation/challenge/{challengeId}/answer");

				request.Content = JsonContent.Create(new { Answer = answer });

				using var response = await Http.SendAsync(request, cancellationToken);
				return response.IsSuccessStatusCode;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Pairs this handset and stores the token it is issued.
		///
		/// The pairing key is only used here. Everything afterwards carries the
		/// per-device token instead, so the key can be rotated on the server
		/// without touching phones that are already paired.
		/// </summary>
		public static async Task<(bool Success, string Message)> RegisterAsync(
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

				request.Content = JsonContent.Create(new
				{
					DeviceId = IfmsRelaySettings.DeviceId,
					DeviceName = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}",
					AppVersion = AppInfo.Current.VersionString,
					Platform = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}"
				});

				using var response = await Http.SendAsync(request, cancellationToken);
				var body = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					return (false, response.StatusCode == System.Net.HttpStatusCode.Unauthorized
						? "The server rejected that pairing key."
						: $"Pairing failed: {body}");
				}

				var result = JsonSerializer.Deserialize<RegisterResult>(body, JsonOptions);

				if (string.IsNullOrWhiteSpace(result?.Token))
					return (false, "The server did not return a device token.");

				IfmsRelaySettings.DeviceToken = result.Token;

				return (true, result.ReplacedExisting
					? "Re-paired. The previous token for this phone no longer works."
					: "Paired.");
			}
			catch (Exception ex)
			{
				return (false, $"Could not reach the server: {ex.Message}");
			}
		}

		/// <summary>
		/// Tells the server this phone is alive. Without it a handset that is off
		/// or has had the app killed looks identical to one with nothing to say,
		/// and that is only discovered when the OTP never arrives at 4am.
		/// </summary>
		public static async Task<bool> HeartbeatAsync(CancellationToken cancellationToken = default)
		{
			if (!IfmsRelaySettings.IsConfigured)
				return false;

			try
			{
				using var request = Build(HttpMethod.Post, "api/IfmsAutomation/devices/heartbeat");
				using var response = await Http.SendAsync(request, cancellationToken);

				return response.IsSuccessStatusCode;
			}
			catch
			{
				return false;
			}
		}

		private static HttpRequestMessage Build(HttpMethod method, string path)
		{
			var baseUrl = IfmsRelaySettings.ApiBase.TrimEnd('/');

			var request = new HttpRequestMessage(method, $"{baseUrl}/{path}");
			request.Headers.Add("X-Device-Token", IfmsRelaySettings.DeviceToken);

			return request;
		}

		private sealed class RegisterResult
		{
			public string? Token { get; set; }
			public bool ReplacedExisting { get; set; }
		}

		public sealed class PendingChallenge
		{
			public int Id { get; set; }
			public string? ImageBase64 { get; set; }
			public string? Prompt { get; set; }
			public int Round { get; set; }
			public string? FailedGuesses { get; set; }
			public int SecondsRemaining { get; set; }
		}
	}
}
