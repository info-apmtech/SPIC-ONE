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

		private static HttpRequestMessage Build(HttpMethod method, string path)
		{
			var baseUrl = IfmsRelaySettings.ApiBase.TrimEnd('/');

			var request = new HttpRequestMessage(method, $"{baseUrl}/{path}");
			request.Headers.Add("X-Device-Key", IfmsRelaySettings.DeviceKey);

			return request;
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
