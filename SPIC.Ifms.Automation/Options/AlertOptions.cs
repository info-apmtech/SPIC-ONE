using System.Collections.Generic;

namespace SPIC.Ifms.Automation.Options
{
	/// <summary>
	/// Where the nightly success/failure notice goes. Every sink is independent:
	/// one being misconfigured never stops the others, and never fails the run.
	/// </summary>
	public sealed class AlertOptions
	{
		public const string SectionName = "Alerts";

		/// <summary>
		/// Send a notice even when everything worked. Turn this off once you trust
		/// it and you will only hear from it when something breaks.
		/// </summary>
		public bool NotifyOnSuccess { get; set; } = true;
		public bool NotifyOnFailure { get; set; } = true;

		public EmailAlertOptions Email { get; set; } = new();
		public PushAlertOptions Push { get; set; } = new();
		public WhatsAppAlertOptions WhatsApp { get; set; } = new();
	}

	public sealed class EmailAlertOptions
	{
		public bool Enabled { get; set; }

		public string Host { get; set; } = string.Empty;
		public int Port { get; set; } = 587;
		public bool UseStartTls { get; set; } = true;

		public string UserName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;

		public string FromAddress { get; set; } = string.Empty;
		public string FromName { get; set; } = "SPIC IFMS Automation";

		public List<string> To { get; set; } = new();
		public List<string> Cc { get; set; } = new();

		/// <summary>Only mail failures, never the daily all-clear.</summary>
		public bool FailuresOnly { get; set; }

		/// <summary>
		/// Attach the downloaded workbooks. Off by default: seven reports is a lot
		/// of megabytes to post every morning.
		/// </summary>
		public bool AttachReports { get; set; }
	}

	public sealed class PushAlertOptions
	{
		/// <summary>
		/// Push to the SPIC Android app. The automation POSTs the summary to
		/// SpicAPI, which is what the phone already talks to.
		/// </summary>
		public bool Enabled { get; set; }

		/// <summary>SpicAPI base address, e.g. https://api.spic.example</summary>
		public string ApiBaseUrl { get; set; } = string.Empty;

		/// <summary>Shared secret sent as X-Automation-Key.</summary>
		public string ApiKey { get; set; } = string.Empty;

		public string NotifyPath { get; set; } = "/api/IfmsAutomation/notify";
	}

	public sealed class WhatsAppAlertOptions
	{
		public bool Enabled { get; set; }

		/// <summary>
		/// Generic HTTP sender so it works with whichever gateway you already pay
		/// for. {{message}} in the URL or body is replaced with the alert text.
		/// </summary>
		public string RequestUrl { get; set; } = string.Empty;
		public string Method { get; set; } = "POST";
		public string? BodyTemplate { get; set; }
		public string ContentType { get; set; } = "application/json";
		public Dictionary<string, string> Headers { get; set; } = new();

		/// <summary>Numbers in international format without a plus, e.g. 9198…</summary>
		public List<string> Recipients { get; set; } = new();

		/// <summary>WhatsApp is noisy. Failures only, by default.</summary>
		public bool FailuresOnly { get; set; } = true;
	}
}
