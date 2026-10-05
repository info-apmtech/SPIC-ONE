using System.Text.Json.Serialization;

namespace SPIC.Ifms.Relay.Services
{
	// The wire shapes of SpicAPI's IfmsAutomation controller, copied here rather
	// than referenced from SPIC.Core so this app pulls in nothing but MAUI.
	// Property names must match the server's DTOs; the serializer is
	// case-insensitive so casing on the wire does not matter.

	/// <summary>A CAPTCHA the automation could not read, waiting for a person.</summary>
	public sealed class PendingChallenge
	{
		public int Id { get; set; }
		public int? RunId { get; set; }
		public string? ChallengeType { get; set; }

		/// <summary>PNG, base64.</summary>
		public string? ImageBase64 { get; set; }

		public string? Prompt { get; set; }
		public int Round { get; set; }

		/// <summary>What the automatic solvers read before giving up.</summary>
		public string? FailedGuesses { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime ExpiresAt { get; set; }

		/// <summary>Seconds left before the automation stops waiting.</summary>
		public int SecondsRemaining { get; set; }
	}

	/// <summary>The subset of the server's run summary the Home page shows.</summary>
	public sealed class RunSummary
	{
		public int Id { get; set; }
		public DateTime ReportDate { get; set; }
		public DateTime StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
		public string Status { get; set; } = string.Empty;
		public int ReportsTotal { get; set; }
		public int ReportsSucceeded { get; set; }
		public int ReportsFailed { get; set; }
		public int RowsInserted { get; set; }
		public int RowsUpdated { get; set; }
		public string? ErrorMessage { get; set; }
	}

	/// <summary>GET api/IfmsAutomation/status.</summary>
	public sealed class RelayStatus
	{
		public PendingChallenge? PendingChallenge { get; set; }
		public RunSummary? LatestRun { get; set; }
		public bool NeedsAttention { get; set; }
		public string Headline { get; set; } = string.Empty;
	}

	public sealed class SmsRequest
	{
		public string DeviceId { get; set; } = string.Empty;
		public string Sender { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public DateTime ReceivedAt { get; set; }
	}

	public sealed class AnswerRequest
	{
		public string Answer { get; set; } = string.Empty;
	}

	public sealed class RegisterRequest
	{
		public string DeviceId { get; set; } = string.Empty;
		public string DeviceName { get; set; } = string.Empty;
		public string AppVersion { get; set; } = string.Empty;
		public string Platform { get; set; } = string.Empty;
	}

	public sealed class RegisterResult
	{
		public bool Success { get; set; }
		public string? Token { get; set; }
		public bool ReplacedExisting { get; set; }
	}

	/// <summary>The controller's uniform failure body.</summary>
	public sealed class ServerMessage
	{
		public bool Success { get; set; }
		public string? Message { get; set; }
	}

	/// <summary>
	/// GET api/IfmsAutomation/alerts/email: how the nightly automation reports
	/// by email. The stored password never comes back; only
	/// <see cref="HasPassword"/> says whether one is on file.
	/// </summary>
	public sealed class AlertSettings
	{
		public bool EmailEnabled { get; set; }
		public string? SmtpHost { get; set; }
		public int SmtpPort { get; set; }
		public bool UseStartTls { get; set; }
		public string? UserName { get; set; }
		public bool HasPassword { get; set; }
		public string? FromAddress { get; set; }
		public string? FromName { get; set; }
		public string? ToAddresses { get; set; }
		public string? CcAddresses { get; set; }
		public bool FailuresOnly { get; set; }
		public bool AttachReports { get; set; }
		public DateTime? TestRequestedAt { get; set; }
		public DateTime? LastTestAt { get; set; }
		public string? LastTestResult { get; set; }
		public DateTime? LastSentAt { get; set; }
		public string? LastSendResult { get; set; }
		public DateTime UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }
	}

	/// <summary>
	/// PUT api/IfmsAutomation/alerts/email. <see cref="Password"/> is null when
	/// the person left the field blank, which tells the server to keep the one
	/// it already has; it is sent once and never kept on the phone.
	/// </summary>
	public sealed class AlertSettingsUpdate
	{
		public bool EmailEnabled { get; set; }
		public string SmtpHost { get; set; } = string.Empty;
		public int SmtpPort { get; set; }
		public bool UseStartTls { get; set; }
		public string UserName { get; set; } = string.Empty;
		public string? Password { get; set; }
		public string FromAddress { get; set; } = string.Empty;
		public string FromName { get; set; } = string.Empty;
		public string ToAddresses { get; set; } = string.Empty;
		public string CcAddresses { get; set; } = string.Empty;
		public bool FailuresOnly { get; set; }
		public bool AttachReports { get; set; }
	}

	/// <summary>
	/// Source-generated serializer for every wire shape above. Reflection-based
	/// serialization would work, but it earns trimming warnings in the Release
	/// build and this is cheaper on a phone that wakes for one request.
	/// </summary>
	[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
	[JsonSerializable(typeof(PendingChallenge))]
	[JsonSerializable(typeof(RelayStatus))]
	[JsonSerializable(typeof(SmsRequest))]
	[JsonSerializable(typeof(AnswerRequest))]
	[JsonSerializable(typeof(RegisterRequest))]
	[JsonSerializable(typeof(RegisterResult))]
	[JsonSerializable(typeof(ServerMessage))]
	[JsonSerializable(typeof(AlertSettings))]
	[JsonSerializable(typeof(AlertSettingsUpdate))]
	internal sealed partial class RelayJson : JsonSerializerContext
	{
	}
}
