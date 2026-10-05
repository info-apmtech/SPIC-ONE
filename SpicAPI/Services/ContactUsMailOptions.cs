using System.Collections.Generic;

namespace SpicAPI.Services
{
	/// <summary>
	/// SMTP connection used to send the Contact Us enquiry email. Bound to the
	/// EXACT SAME configuration section ("Alerts:Email") that SPIC.Ifms.Automation
	/// uses for its alert email, so there is one SMTP configuration/pattern and no
	/// second, independent email setup to maintain.
	///
	/// The password is never stored here — it comes from the same environment
	/// variable the automation reads: Alerts__Email__Password (empty in config).
	/// </summary>
	public sealed class ContactUsMailOptions
	{
		public const string SectionName = "Alerts:Email";

		public string Host { get; set; } = string.Empty;
		public int Port { get; set; } = 587;
		public bool UseStartTls { get; set; } = true;

		public string UserName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;

		public string FromAddress { get; set; } = string.Empty;
		public string FromName { get; set; } = "SPIC IFMS Automation";

		// Contact Us has its own recipients: never the automation's To/Cc.
		public List<string> To { get; set; } = new();
	}
}