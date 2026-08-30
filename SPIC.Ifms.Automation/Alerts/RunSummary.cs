using System;
using System.Collections.Generic;
using System.Linq;
using SPIC.Core.Entities;

namespace SPIC.Ifms.Automation.Alerts
{
	/// <summary>
	/// The shape every alert sink renders. Built once at the end of a run so the
	/// email, the push and the WhatsApp message all say exactly the same thing.
	/// </summary>
	public sealed class RunSummary
	{
		public required int RunId { get; init; }
		public required DateTime ReportDate { get; init; }
		public required DateTime StartedAtLocal { get; init; }
		public required DateTime CompletedAtLocal { get; init; }
		public required IfmsRunStatus Status { get; init; }
		public required int Attempt { get; init; }

		public bool SitePortalReachable { get; init; }
		public bool LoginSucceeded { get; init; }
		public string? CaptchaMethod { get; init; }
		public int CaptchaAttempts { get; init; }
		public string? OtpMethod { get; init; }

		/// <summary>How many company logins this run covered, and how many worked.</summary>
		public int AccountsTotal { get; init; }
		public int AccountsSucceeded { get; init; }

		public string? ErrorMessage { get; init; }

		public List<ReportSummary> Reports { get; init; } = new();

		public TimeSpan Duration => CompletedAtLocal - StartedAtLocal;

		public int ReportsSucceeded => Reports.Count(r => r.Status == IfmsRunStatus.Succeeded);
		public int ReportsFailed => Reports.Count(r => r.Status == IfmsRunStatus.Failed);
		public int RowsInserted => Reports.Sum(r => r.RowsInserted);
		public int RowsUpdated => Reports.Sum(r => r.RowsUpdated);
		public int RowsSkipped => Reports.Sum(r => r.RowsSkipped);

		public bool IsFailure =>
			Status is IfmsRunStatus.Failed or IfmsRunStatus.PartiallySucceeded;

		/// <summary>One line fit for an SMS or a notification title.</summary>
		public string Headline => Status switch
		{
			IfmsRunStatus.Succeeded =>
				$"IFMS {ReportDate:dd-MMM}: all {Reports.Count} reports imported across " +
				$"{AccountsTotal} logins ({RowsInserted:N0} new, {RowsUpdated:N0} updated).",
			IfmsRunStatus.PartiallySucceeded =>
				$"IFMS {ReportDate:dd-MMM}: {ReportsSucceeded} of {Reports.Count} reports imported, " +
				$"{ReportsFailed} failed.",
			IfmsRunStatus.Skipped =>
				$"IFMS {ReportDate:dd-MMM}: run skipped.",
			_ =>
				$"IFMS {ReportDate:dd-MMM}: run FAILED. {ErrorMessage}"
		};

		/// <summary>
		/// What the reader should actually do. Kept explicit because the whole
		/// point of the automation is that nobody has to work out the next step.
		/// </summary>
		public string ActionRequired
		{
			get
			{
				if (Status == IfmsRunStatus.Succeeded)
					return "Nothing to do.";

				if (!SitePortalReachable)
					return "The IFMS portal never came up. The run will retry; " +
						   "no manual download is needed yet.";

				if (!LoginSucceeded)
					return "No login completed. If the CAPTCHA prompt on your phone went " +
						   "unanswered, download today's reports manually from IFMS and " +
						   "upload them on the Excel Upload page.";

				if (AccountsSucceeded < AccountsTotal)
					return $"Only {AccountsSucceeded} of {AccountsTotal} logins worked. " +
						   "Check the error above, then fetch that company's reports by hand.";

				var failed = Reports.Where(r => r.Status == IfmsRunStatus.Failed).ToList();
				if (failed.Count == 0)
					return "Nothing to do.";

				// Name the company on each one: the same report title exists under
				// both logins, so the title alone does not say which to fetch.
				return "Download and upload these manually: " +
					   string.Join(", ", failed.Select(r =>
						   string.IsNullOrWhiteSpace(r.CompanyName)
							   ? r.Title
							   : $"{r.CompanyName} {r.Title}"));
			}
		}
	}

	public sealed class ReportSummary
	{
		public required string JobKey { get; init; }

		/// <summary>Which company login produced it.</summary>
		public string AccountKey { get; init; } = string.Empty;
		public string CompanyName { get; init; } = string.Empty;

		public required string Title { get; init; }
		public required string CategoryId { get; init; }
		public required IfmsRunStatus Status { get; init; }

		public string? FileName { get; init; }
		public string? ArchivedFilePath { get; init; }
		public long DownloadedBytes { get; init; }

		public int TotalRows { get; init; }
		public int RowsInserted { get; init; }
		public int RowsUpdated { get; init; }
		public int RowsSkipped { get; init; }

		public string? ErrorMessage { get; init; }
		public List<string> Warnings { get; init; } = new();
	}
}
