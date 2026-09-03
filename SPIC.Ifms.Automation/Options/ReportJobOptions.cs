using System.Collections.Generic;

namespace SPIC.Ifms.Automation.Options
{
	/// <summary>
	/// The catalogue of reports to pull each night. One entry per
	/// report-plus-filter-combination, so "Retail Stocks for Tamil Nadu" and
	/// "Retail Stocks for Karnataka" are two jobs sharing category "One".
	/// </summary>
	public sealed class ReportJobsOptions
	{
		public const string SectionName = "ReportJobs";

		public List<ReportJob> Jobs { get; set; } = new();
	}

	public sealed class ReportJob
	{
		/// <summary>Stable id used in logs, alerts and the dashboard.</summary>
		public string Key { get; set; } = string.Empty;

		/// <summary>Human label, e.g. "Retail Stocks — Tamil Nadu".</summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Which portal login this report is pulled under — "spic" or "greenstar",
		/// matching IfmsPortalAccounts.AccountKey.
		///
		/// The same report usually exists for both companies with different filters
		/// inside the report page, so it becomes two jobs sharing a CategoryId.
		/// Leave empty to run the job under every active login.
		/// </summary>
		public string AccountKey { get; set; } = string.Empty;

		/// <summary>
		/// Upload category understood by the existing ExcelBulkUploadService:
		/// One   = Retail Stocks (DPT)
		/// Two   = Company Sales
		/// Three = Sales and Receipt
		/// Four  = Wholesale Sales
		/// Five  = Wholesale Stock As On Today
		/// Six   = State Wise Global Stock Reconciliation
		/// Seven = Warehouse and RakePoint Global Stock
		/// </summary>
		public string CategoryId { get; set; } = string.Empty;

		public bool Enabled { get; set; } = true;

		/// <summary>Lower numbers run first. Ties fall back to catalogue order.</summary>
		public int Order { get; set; }

		/// <summary>
		/// Overrides Schedule.ReportDateOffsetDays for this job. Some IFMS reports
		/// publish a day later than others.
		/// </summary>
		public int? ReportDateOffsetDays { get; set; }

		/// <summary>
		/// The click path from the logged-in landing page to the report's export
		/// button, expressed as ordered steps. See <see cref="PortalStep"/>.
		/// </summary>
		public List<PortalStep> Steps { get; set; } = new();

		/// <summary>
		/// The step that starts the file download. Kept separate from
		/// <see cref="Steps"/> because Playwright has to arm its download handler
		/// before the click happens.
		/// </summary>
		public PortalStep? DownloadStep { get; set; }

		/// <summary>
		/// Some portals hand back the file from a direct URL once filters are in
		/// session. When set, this is fetched instead of clicking DownloadStep.
		/// Supports the same {{tokens}} as step values.
		/// </summary>
		public string? DirectDownloadUrl { get; set; }

		/// <summary>
		/// Repeat this job across one or more filters, as a cartesian product.
		///
		/// One dimension covers the Retailer Stock Report, which insists on one
		/// state at a time. Two covers the Global Stock Reconciliation, which wants
		/// every plant crossed with every product. Written out by hand these would
		/// be dozens of near-identical jobs.
		/// </summary>
		public List<ReportJobLoop> ForEach { get; set; } = new();

		/// <summary>
		/// Name for the saved file, supporting the same tokens as step values plus
		/// whatever <see cref="ForEach"/> contributes — for example
		/// "{{company}}_{{state}}_retailerstock". The extension is added.
		/// Falls back to the job key and a timestamp.
		/// </summary>
		public string? FileNameTemplate { get; set; }

		/// <summary>
		/// Extension to force when the portal sends a nameless stream. The iFMS
		/// exports are CSV despite the button saying Excel.
		/// </summary>
		public string ExpectedExtension { get; set; } = ".csv";

		/// <summary>
		/// Reject a download smaller than this. A 400-byte "no data found" HTML
		/// page is the classic silent failure on these portals.
		/// </summary>
		public long MinimumBytes { get; set; } = 2_048;

		/// <summary>Per-job download retries before the job is marked failed.</summary>
		public int MaxAttempts { get; set; } = 2;

		/// <summary>
		/// Treat "no rows for this date" as success rather than failure. Set true
		/// for reports that are legitimately empty on holidays.
		/// </summary>
		public bool AllowEmpty { get; set; }
	}

	/// <summary>
	/// One dimension of a job's fan-out.
	///
	/// Either list the values explicitly, or let the run read them off the page.
	/// Listing them is usually better: it keeps the nightly run to the states that
	/// actually matter instead of all thirty-six, and it does not silently grow
	/// when the portal adds one.
	/// </summary>
	public sealed class ReportJobLoop
	{
		/// <summary>Token the value is exposed as, e.g. "state" for {{state}}.</summary>
		public string TokenName { get; set; } = "value";

		/// <summary>
		/// The values to iterate. When empty, they are discovered from
		/// <see cref="DiscoverFromSelector"/>.
		/// </summary>
		public List<string> Values { get; set; } = new();

		/// <summary>
		/// A select element whose options become the values. Discovery navigates to
		/// the job's first "goto" step and reads them there.
		/// </summary>
		public string? DiscoverFromSelector { get; set; }

		/// <summary>
		/// Option labels to ignore during discovery — the placeholder row, mostly.
		/// Matched case-insensitively.
		/// </summary>
		public List<string> ExcludeLabels { get; set; } = new() { "Select" };

		/// <summary>
		/// Carry on to the next value when one fails. True by default: thirty-five
		/// states downloading and one failing is a far better morning than nothing
		/// at all because Assam timed out.
		/// </summary>
		public bool ContinueOnFailure { get; set; } = true;

		/// <summary>
		/// Treat an empty result as success. Many states legitimately have no rows
		/// for a company that does not trade there, and that is not a failure.
		/// </summary>
		public bool AllowEmptyPerValue { get; set; } = true;
	}

	/// <summary>
	/// One action against the portal page. The set is deliberately small; anything
	/// the portal needs should be expressible as a sequence of these.
	/// </summary>
	public sealed class PortalStep
	{
		/// <summary>
		/// goto      — navigate to <see cref="Value"/> (relative or absolute)
		/// click     — click <see cref="Selector"/>
		/// fill      — type <see cref="Value"/> into <see cref="Selector"/>
		/// select    — choose option <see cref="Value"/> in a select element
		/// selectText— choose by visible label instead of value
		/// check     — tick a checkbox or radio
		/// uncheck   — untick a checkbox
		/// waitFor   — wait until <see cref="Selector"/> is visible
		/// waitHidden— wait until <see cref="Selector"/> disappears (spinner)
		/// wait      — sleep <see cref="TimeoutMs"/> milliseconds
		/// press     — send key <see cref="Value"/> to <see cref="Selector"/>
		/// frame     — switch subsequent steps into iframe <see cref="Selector"/>
		/// mainFrame — switch back out of an iframe
		/// eval      — run <see cref="Value"/> as JavaScript (escape hatch)
		/// </summary>
		public string Action { get; set; } = "click";

		public string? Selector { get; set; }

		/// <summary>
		/// Supports tokens resolved at run time:
		/// {{reportDate}}         report date, default dd/MM/yyyy
		/// {{reportDate:format}}  report date in a custom .NET date format
		/// {{fromDate}} {{toDate}} same as reportDate unless the job sets a range
		/// {{today}} {{yesterday}}
		/// {{userName}}
		/// </summary>
		public string? Value { get; set; }

		/// <summary>Override the default action timeout for a slow grid.</summary>
		public int? TimeoutMs { get; set; }

		/// <summary>
		/// Do not fail the run if this step's selector is missing. Use for optional
		/// popups, cookie banners and "click OK on the disclaimer" dialogs.
		/// </summary>
		public bool Optional { get; set; }

		/// <summary>Short note that shows up in the log line for this step.</summary>
		public string? Description { get; set; }
	}
}
