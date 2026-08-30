using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
	/// <summary>
	/// An SMS forwarded by the SPIC Android companion so the automation can read
	/// the IFMS one-time password without anybody being awake.
	/// </summary>
	public sealed class IfmsSmsRelayDto
	{
		public string? DeviceId { get; set; }
		public string? Sender { get; set; }
		public string Body { get; set; } = string.Empty;

		/// <summary>Device clock, UTC. The server falls back to its own now.</summary>
		public DateTime? ReceivedAt { get; set; }
	}

	/// <summary>A CAPTCHA the automation could not read, waiting for a person.</summary>
	public sealed class IfmsPendingChallengeDto
	{
		public int Id { get; set; }
		public int? RunId { get; set; }
		public string ChallengeType { get; set; } = "Captcha";

		/// <summary>PNG, base64. Render it directly with an image source.</summary>
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

	public sealed class IfmsChallengeAnswerDto
	{
		public string Answer { get; set; } = string.Empty;
	}

	public sealed class IfmsRunListItemDto
	{
		public int Id { get; set; }
		public DateTime ReportDate { get; set; }
		public DateTime StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
		public string Status { get; set; } = string.Empty;
		public string Trigger { get; set; } = string.Empty;
		public int Attempt { get; set; }

		public bool SitePortalReachable { get; set; }
		public bool LoginSucceeded { get; set; }
		public string? CaptchaMethod { get; set; }
		public int CaptchaAttempts { get; set; }
		public string? OtpMethod { get; set; }

		public int ReportsTotal { get; set; }
		public int ReportsSucceeded { get; set; }
		public int ReportsFailed { get; set; }

		public int RowsInserted { get; set; }
		public int RowsUpdated { get; set; }
		public int RowsSkipped { get; set; }

		public string? ErrorMessage { get; set; }

		public List<IfmsReportRunDto> Reports { get; set; } = new();
	}

	public sealed class IfmsReportRunDto
	{
		public int Id { get; set; }
		public string JobKey { get; set; } = string.Empty;
		public string CategoryId { get; set; } = string.Empty;
		public string ReportTitle { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;

		public DateTime? ReportDate { get; set; }
		public string? AppliedFilters { get; set; }

		public string? DownloadedFileName { get; set; }
		public long DownloadedBytes { get; set; }

		public int TotalRows { get; set; }
		public int RowsInserted { get; set; }
		public int RowsUpdated { get; set; }
		public int RowsSkipped { get; set; }

		public string? Warnings { get; set; }
		public string? ErrorMessage { get; set; }

		public DateTime StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
		public int Attempt { get; set; }
	}

	public sealed class IfmsQueueRunDto
	{
		/// <summary>Defaults to yesterday, matching the nightly schedule.</summary>
		public DateTime? ReportDate { get; set; }
	}

	/// <summary>
	/// A portal login as the dashboard sees it. Note what is absent: the password
	/// is never returned by the API, in any form.
	/// </summary>
	public sealed class IfmsPortalAccountDto
	{
		public int Id { get; set; }
		public string AccountKey { get; set; } = string.Empty;
		public string CompanyName { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;

		public bool IsActive { get; set; }
		public int Order { get; set; }

		public bool HasPassword { get; set; }

		/// <summary>
		/// TEMPORARY. Only populated while Ifms:StorePlainPasswordForTesting is on,
		/// so the stored password can be checked by eye during commissioning.
		/// Null once the flag is off.
		/// </summary>
		public string? PlainPasswordForTesting { get; set; }
		public DateTime PasswordSetAt { get; set; }
		public DateTime PasswordExpiresAt { get; set; }
		public int PasswordRotationDays { get; set; }

		/// <summary>Negative once expired, which is why it is not unsigned.</summary>
		public int DaysUntilExpiry { get; set; }
		public bool PasswordExpired { get; set; }

		/// <summary>True inside the last ten days, when it is worth acting on.</summary>
		public bool PasswordExpiringSoon { get; set; }

		public DateTime? LastLoginAt { get; set; }
		public bool LastLoginSucceeded { get; set; }
		public string? LastLoginMessage { get; set; }

		public string? OtpMobileNumber { get; set; }
	}

	public sealed class IfmsSetAccountDto
	{
		/// <summary>Short key used in the report jobs, e.g. "spic" or "greenstar".</summary>
		public string AccountKey { get; set; } = string.Empty;

		public string CompanyName { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;

		public string? OtpMobileNumber { get; set; }
	}

	public sealed class IfmsRegisterDeviceDto
	{
		/// <summary>Stable id the phone generates for itself, once.</summary>
		public string DeviceId { get; set; } = string.Empty;

		/// <summary>Something a person recognises in the device list.</summary>
		public string DeviceName { get; set; } = string.Empty;

		public string? AppVersion { get; set; }
		public string? Platform { get; set; }
	}

	/// <summary>
	/// What the phone shows on its IFMS screen in a single call: the pending
	/// CAPTCHA, if any, and how last night went.
	/// </summary>
	public sealed class IfmsAutomationStatusDto
	{
		public IfmsPendingChallengeDto? PendingChallenge { get; set; }
		public IfmsRunListItemDto? LatestRun { get; set; }
		public bool NeedsAttention { get; set; }
		public string Headline { get; set; } = string.Empty;
	}
}
