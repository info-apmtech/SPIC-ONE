namespace SPIC.Ifms.Automation.Options
{
	/// <summary>
	/// Where downloaded reports are sent to be imported.
	///
	/// The automation does not write report data itself. It posts each file to
	/// SpicAPI's upload endpoint — the same one the Excel Upload page uses — so
	/// there is exactly one import path, it writes to the portal's own database,
	/// and this service never needs to know the SPIC schema.
	///
	/// It also means the portal can move. When SPIC ONE is hosted on the
	/// customer's own domain, this is the one setting that changes.
	/// </summary>
	public sealed class UploadOptions
	{
		public const string SectionName = "Upload";

		/// <summary>SpicAPI's address, e.g. https://spicapi.apmiot.com</summary>
		public string ApiBaseUrl { get; set; } = string.Empty;

		public string Path { get; set; } = "/api/ExcelBulkUpload/import";

		/// <summary>
		/// Sent as X-Automation-Key. Must match IfmsAutomation:AutomationKey on
		/// SpicAPI. The upload endpoint otherwise needs a signed-in user, and there
		/// is nobody signed in at four in the morning.
		/// </summary>
		public string ApiKey { get; set; } = string.Empty;

		/// <summary>
		/// Generous by design: a hundred thousand rows of retail stock takes a
		/// while to parse, and a timeout here would re-download a file that had
		/// already imported.
		/// </summary>
		public int TimeoutSeconds { get; set; } = 900;

		/// <summary>Retries for a network failure. The import itself is not retried.</summary>
		public int MaxAttempts { get; set; } = 3;
	}
}
