using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.Entities
{
	/// <summary>
	/// Lifecycle of an automated IFMS fetch. Applies both to the whole nightly run
	/// and to each individual report inside it.
	/// </summary>
	public enum IfmsRunStatus
	{
		Pending = 0,
		Running = 1,
		Succeeded = 2,
		PartiallySucceeded = 3,
		Failed = 4,
		Skipped = 5
	}

	public enum IfmsRunTrigger
	{
		Schedule = 0,
		Manual = 1,
		Retry = 2
	}

	/// <summary>
	/// One nightly attempt at logging into the IFMS portal and pulling every
	/// configured report. A single run owns many <see cref="IfmsAutomationReportRun"/>.
	/// </summary>
	public class IfmsAutomationRun
	{
		[Key]
		public int Id { get; set; }

		/// <summary>Business date the reports belong to (normally "yesterday" at 04:05).</summary>
		public DateTime ReportDate { get; set; }

		public DateTime StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }

		public IfmsRunStatus Status { get; set; }
		public IfmsRunTrigger Trigger { get; set; }

		/// <summary>1 for the first attempt of the night, 2+ for automatic retries.</summary>
		public int Attempt { get; set; }

		public bool SitePortalReachable { get; set; }
		public bool LoginSucceeded { get; set; }

		/// <summary>How many portal accounts this run signed in as, and how many worked.</summary>
		public int AccountsTotal { get; set; }
		public int AccountsSucceeded { get; set; }

		/// <summary>How the CAPTCHA was answered: HtmlText, Ocr, Operator, NotRequired.</summary>
		public string? CaptchaMethod { get; set; }
		public int CaptchaAttempts { get; set; }

		/// <summary>How the OTP was answered: SmsRelay, Operator, NotRequired.</summary>
		public string? OtpMethod { get; set; }

		public int ReportsTotal { get; set; }
		public int ReportsSucceeded { get; set; }
		public int ReportsFailed { get; set; }

		public int RowsInserted { get; set; }
		public int RowsUpdated { get; set; }
		public int RowsSkipped { get; set; }

		[MaxLength(4000)]
		public string? ErrorMessage { get; set; }

		public bool AlertSent { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public string UpdatedBy { get; set; } = "IfmsAutomation";

		public List<IfmsAutomationReportRun> Reports { get; set; } = new();
	}

	/// <summary>
	/// One report (category + filter combination) inside a nightly run: downloaded,
	/// then handed to the existing ExcelBulkUploadService for import.
	/// </summary>
	public class IfmsAutomationReportRun
	{
		[Key]
		public int Id { get; set; }

		public int RunId { get; set; }
		public IfmsAutomationRun? Run { get; set; }

		/// <summary>Job key from configuration, e.g. "retail-stocks-tn".</summary>
		[MaxLength(120)]
		public string JobKey { get; set; } = string.Empty;

		/// <summary>Which portal login produced this report — "spic" or "greenstar".</summary>
		[MaxLength(40)]
		public string AccountKey { get; set; } = string.Empty;

		/// <summary>Upload category consumed by ExcelBulkUploadService: One..Seven.</summary>
		[MaxLength(10)]
		public string CategoryId { get; set; } = string.Empty;

		[MaxLength(200)]
		public string ReportTitle { get; set; } = string.Empty;

		/// <summary>Filter values actually applied on the portal, serialised as JSON.</summary>
		public string? AppliedFilters { get; set; }

		public DateTime? ReportDate { get; set; }

		public IfmsRunStatus Status { get; set; }
		public int Attempt { get; set; }

		public DateTime StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }

		[MaxLength(400)]
		public string? DownloadedFileName { get; set; }

		/// <summary>Absolute path of the archived copy on the automation host.</summary>
		[MaxLength(1000)]
		public string? ArchivedFilePath { get; set; }

		public long DownloadedBytes { get; set; }

		public int TotalRows { get; set; }
		public int RowsInserted { get; set; }
		public int RowsUpdated { get; set; }
		public int RowsSkipped { get; set; }

		/// <summary>Import warnings from ExcelBulkUploadService, newline separated.</summary>
		public string? Warnings { get; set; }

		[MaxLength(4000)]
		public string? ErrorMessage { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public string UpdatedBy { get; set; } = "IfmsAutomation";
	}

	/// <summary>
	/// An SMS forwarded by the SPIC Android companion. The automation polls this
	/// table for the IFMS one-time password instead of waiting for a human.
	/// </summary>
	public class IfmsOtpMessage
	{
		[Key]
		public int Id { get; set; }

		/// <summary>Android device that relayed the message (for auditing).</summary>
		[MaxLength(120)]
		public string? DeviceId { get; set; }

		[MaxLength(120)]
		public string? Sender { get; set; }

		[MaxLength(2000)]
		public string Body { get; set; } = string.Empty;

		/// <summary>Digits pulled out of <see cref="Body"/> by the OTP regex.</summary>
		[MaxLength(20)]
		public string? ExtractedOtp { get; set; }

		/// <summary>When the phone received it (device clock, UTC).</summary>
		public DateTime ReceivedAt { get; set; }

		/// <summary>When the server accepted it.</summary>
		public DateTime CreatedAt { get; set; }

		/// <summary>Set once a login has used this OTP, so it is never replayed.</summary>
		public DateTime? ConsumedAt { get; set; }

		public int? ConsumedByRunId { get; set; }

		/// <summary>Which login consumed it. Both companies may share one handset.</summary>
		[MaxLength(40)]
		public string? ConsumedByAccountKey { get; set; }
	}

	/// <summary>
	/// Cookies from a successful IFMS login, reused on the next run so that the
	/// CAPTCHA and OTP are only needed when the portal actually expires the session.
	/// </summary>
	public class IfmsPortalSession
	{
		[Key]
		public int Id { get; set; }

		[MaxLength(120)]
		public string PortalUserName { get; set; } = string.Empty;

		/// <summary>Playwright storage-state JSON (cookies + localStorage).</summary>
		public string StorageStateJson { get; set; } = string.Empty;

		public DateTime CapturedAt { get; set; }
		public DateTime? LastValidatedAt { get; set; }
		public DateTime? InvalidatedAt { get; set; }

		[MaxLength(400)]
		public string? InvalidationReason { get; set; }

		public bool IsActive { get; set; }
	}

	/// <summary>
	/// A CAPTCHA the automatic solvers could not read after every attempt was
	/// spent. It is parked here, pushed to the SPIC Android app, and the login
	/// waits for a human answer.
	///
	/// A late answer is expected and handled: if the portal has expired the page
	/// by the time the reply lands, the automation reloads the login, captures a
	/// fresh CAPTCHA and raises a new request straight away — so answering at 7am
	/// still gets the reports in.
	/// </summary>
	public class IfmsChallengeRequest
	{
		[Key]
		public int Id { get; set; }

		public int? RunId { get; set; }

		/// <summary>Which company's login is blocked, so the app can say so.</summary>
		[MaxLength(40)]
		public string? AccountKey { get; set; }

		[MaxLength(120)]
		public string? CompanyName { get; set; }

		/// <summary>Captcha or Otp.</summary>
		[MaxLength(20)]
		public string ChallengeType { get; set; } = "Captcha";

		/// <summary>PNG of the CAPTCHA, base64, so the phone can render it.</summary>
		public string? ImageBase64 { get; set; }

		[MaxLength(400)]
		public string? Prompt { get; set; }

		/// <summary>Which round of asking this is, 1-based.</summary>
		public int Round { get; set; } = 1;

		/// <summary>What the automatic solvers guessed before giving up, for context.</summary>
		[MaxLength(200)]
		public string? FailedGuesses { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime ExpiresAt { get; set; }

		[MaxLength(50)]
		public string? Answer { get; set; }

		public DateTime? AnsweredAt { get; set; }

		[MaxLength(120)]
		public string? AnsweredBy { get; set; }

		/// <summary>Pending, Answered, Accepted, Rejected, Expired, Cancelled.</summary>
		[MaxLength(20)]
		public string Status { get; set; } = "Pending";
	}

	/// <summary>
	/// A login on the iFMS portal. There is one row per company — SPIC and
	/// Greenstar — because each has its own credentials and its own set of
	/// reports, and the nightly run signs in as each in turn.
	///
	/// Credentials live here rather than in appsettings.json for one concrete
	/// reason: the portal expires passwords every 80 days, so they have to be
	/// changeable without a redeploy, and their age has to be tracked.
	/// </summary>
	public class IfmsPortalAccount
	{
		[Key]
		public int Id { get; set; }

		/// <summary>Stable short key used in config and logs, e.g. "spic".</summary>
		[MaxLength(40)]
		public string AccountKey { get; set; } = string.Empty;

		/// <summary>Display name, e.g. "SPIC" or "Greenstar".</summary>
		[MaxLength(120)]
		public string CompanyName { get; set; } = string.Empty;

		[MaxLength(120)]
		public string UserName { get; set; } = string.Empty;

		/// <summary>
		/// The password, encrypted with ASP.NET Data Protection. Never stored or
		/// logged in the clear.
		///
		/// This ciphertext is only readable by a process using the same Data
		/// Protection key ring, so that key directory must be persisted and backed
		/// up — lose it and every stored password has to be re-entered.
		/// </summary>
		public string ProtectedPassword { get; set; } = string.Empty;

		/// <summary>
		/// TEMPORARY. The same password in the clear, so it can be eyeballed while
		/// the automation is being commissioned.
		///
		/// Only written when Ifms:StorePlainPasswordForTesting is true, and the run
		/// logs a warning every time it starts while that flag is on — because a
		/// column added "for a few days" is exactly the kind of thing that is still
		/// there in three years.
		///
		/// To retire it: set the flag false, then drop this column in a migration.
		/// Nothing reads it; ProtectedPassword is always the source of truth.
		/// </summary>
		[MaxLength(200)]
		public string? PlainPasswordForTesting { get; set; }

		public bool IsActive { get; set; } = true;

		/// <summary>Lower numbers sign in first.</summary>
		public int Order { get; set; }

		/// <summary>When this password was last set, which starts the 80-day clock.</summary>
		public DateTime PasswordSetAt { get; set; }

		/// <summary>
		/// How long the portal lets a password live. 80 days today; kept per-account
		/// so a policy change on one company does not disturb the other.
		/// </summary>
		public int PasswordRotationDays { get; set; } = 80;

		/// <summary>Derived on write so the dashboard can sort and warn on it.</summary>
		public DateTime PasswordExpiresAt { get; set; }

		/// <summary>Set when a person has been told the password is about to expire.</summary>
		public DateTime? ExpiryWarningSentAt { get; set; }

		public DateTime? LastLoginAt { get; set; }
		public bool LastLoginSucceeded { get; set; }

		[MaxLength(400)]
		public string? LastLoginMessage { get; set; }

		/// <summary>
		/// Mobile number the portal sends this account's OTP to. Recorded for the
		/// audit trail and to help work out which handset must hold which SIM.
		/// </summary>
		[MaxLength(20)]
		public string? OtpMobileNumber { get; set; }

		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		[MaxLength(120)]
		public string UpdatedBy { get; set; } = "System";
	}

	/// <summary>
	/// Audit of every password change, so a login failure can be traced to the
	/// change that caused it. Deliberately records no password, old or new.
	/// </summary>
	public class IfmsPasswordChange
	{
		[Key]
		public int Id { get; set; }

		public int AccountId { get; set; }

		public DateTime ChangedAt { get; set; }

		/// <summary>Manual, ScheduledRotation or PortalForced.</summary>
		[MaxLength(30)]
		public string Reason { get; set; } = "Manual";

		[MaxLength(120)]
		public string ChangedBy { get; set; } = "System";

		/// <summary>Whether a login has since succeeded with the new password.</summary>
		public bool VerifiedByLogin { get; set; }
	}

	/// <summary>
	/// A phone paired to relay the IFMS one-time password.
	///
	/// One row per handset, rather than one shared key, so that replacing a phone
	/// actually revokes the old one. With a single shared secret the retired
	/// handset keeps working forever, which is only ever discovered the hard way.
	/// </summary>
	public class IfmsRelayDevice
	{
		[Key]
		public int Id { get; set; }

		/// <summary>Stable id the phone generates for itself at first pairing.</summary>
		[MaxLength(120)]
		public string DeviceId { get; set; } = string.Empty;

		/// <summary>Something a person recognises, e.g. "Redmi Note 12 - Satham".</summary>
		[MaxLength(160)]
		public string DeviceName { get; set; } = string.Empty;

		/// <summary>
		/// SHA-256 of the token issued at pairing. A hash rather than the token
		/// itself: the server only ever needs to check one, never to reproduce it,
		/// so there is no reason to keep anything replayable.
		/// </summary>
		[MaxLength(64)]
		public string TokenHash { get; set; } = string.Empty;

		public DateTime RegisteredAt { get; set; }

		[MaxLength(120)]
		public string? RegisteredBy { get; set; }

		/// <summary>
		/// Updated on every call the phone makes. This is what turns a dead handset
		/// from a 4am surprise into something noticed the evening before.
		/// </summary>
		public DateTime? LastSeenAt { get; set; }

		[MaxLength(60)]
		public string? LastSeenAction { get; set; }

		public int MessagesRelayed { get; set; }

		public bool IsActive { get; set; } = true;

		public DateTime? RevokedAt { get; set; }

		[MaxLength(120)]
		public string? RevokedBy { get; set; }

		/// <summary>App version, for working out whether an old build is the problem.</summary>
		[MaxLength(40)]
		public string? AppVersion { get; set; }

		[MaxLength(120)]
		public string? Platform { get; set; }
	}
}
