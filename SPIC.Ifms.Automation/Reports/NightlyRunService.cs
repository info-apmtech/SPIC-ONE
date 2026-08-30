using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Alerts;
using SPIC.Core.Interfaces;
using SPIC.Ifms.Automation.Options;
using SPIC.Ifms.Automation.Portal;

namespace SPIC.Ifms.Automation.Reports
{
	public interface INightlyRunService
	{
		Task<RunSummary> RunAsync(
			DateTime reportDate,
			IfmsRunTrigger trigger,
			int attempt,
			IReadOnlyCollection<string>? onlyJobKeys,
			CancellationToken cancellationToken,
			int? existingRunId = null);
	}

	/// <summary>
	/// One end-to-end pass: wait for the portal, sign in, pull every enabled
	/// report, import each one, then tell somebody what happened.
	///
	/// The ordering rule that matters: one report failing never stops the rest.
	/// Six good reports and a clear note about the seventh beats an all-or-nothing
	/// abort that leaves the morning with nothing.
	/// </summary>
	public sealed class NightlyRunService : INightlyRunService
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ISiteProbe _siteProbe;
		private readonly IReportImporter _importer;
		private readonly IAlertDispatcher _alerts;
		private readonly IfmsOptions _ifms;
		private readonly ScheduleOptions _schedule;
		private readonly ReportJobsOptions _jobs;
		private readonly ILogger<NightlyRunService> _logger;

		public NightlyRunService(
			IServiceScopeFactory scopeFactory,
			ISiteProbe siteProbe,
			IReportImporter importer,
			IAlertDispatcher alerts,
			IOptions<IfmsOptions> ifms,
			IOptions<ScheduleOptions> schedule,
			IOptions<ReportJobsOptions> jobs,
			ILogger<NightlyRunService> logger)
		{
			_scopeFactory = scopeFactory;
			_siteProbe = siteProbe;
			_importer = importer;
			_alerts = alerts;
			_ifms = ifms.Value;
			_schedule = schedule.Value;
			_jobs = jobs.Value;
			_logger = logger;
		}

		public async Task<RunSummary> RunAsync(
			DateTime reportDate,
			IfmsRunTrigger trigger,
			int attempt,
			IReadOnlyCollection<string>? onlyJobKeys,
			CancellationToken cancellationToken,
			int? existingRunId = null)
		{
			var startedAt = DateTime.Now;

			var jobs = _jobs.Jobs
				.Where(j => j.Enabled)
				.Where(j => onlyJobKeys is null || onlyJobKeys.Contains(j.Key, StringComparer.OrdinalIgnoreCase))
				.OrderBy(j => j.Order)
				.ToList();

			var run = await StartRunAsync(reportDate, trigger, attempt, jobs.Count, existingRunId, cancellationToken);

			_logger.LogInformation(
				"Run {RunId} started for report date {ReportDate:dd-MMM-yyyy} with {Count} report(s).",
				run.Id, reportDate, jobs.Count);

			var reports = new List<ReportSummary>();
			string? fatalError = null;
			var reachable = false;
			var loggedIn = false;
			string? captchaMethod = null;
			var captchaAttempts = 0;
			string? otpMethod = null;

			var accountsSucceeded = 0;
			var accountsTotal = 0;

			try
			{
				reachable = await _siteProbe.WaitUntilUpAsync(cancellationToken);

				if (!reachable)
				{
					fatalError =
						$"The IFMS portal did not respond within {_schedule.SiteProbeMaxWaitMinutes} minutes " +
						$"of the scheduled start.";
				}
				else
				{
					var accounts = await WithAccountStoreAsync(
						store => store.GetActiveAsync(cancellationToken));
					accountsTotal = accounts.Count;

					if (accounts.Count == 0)
					{
						fatalError =
							"No IFMS portal logins are configured. Add them on the IFMS Logins page, " +
							"or with: dotnet run -- set-credentials <key> <username> <password>";
					}

					// Each company is a separate login and a separate browser session,
					// signed in one after another. Sequential on purpose: two
					// simultaneous logins would race for the OTP arriving on the same
					// handset, and there would be no way to tell the codes apart.
					foreach (var account in accounts)
					{
						cancellationToken.ThrowIfCancellationRequested();

						var accountJobs = jobs
							.Where(j => string.IsNullOrWhiteSpace(j.AccountKey) ||
										string.Equals(j.AccountKey, account.AccountKey,
													  StringComparison.OrdinalIgnoreCase))
							.ToList();

						if (accountJobs.Count == 0)
						{
							_logger.LogInformation(
								"No enabled reports are assigned to {Company}; skipping that login.",
								account.CompanyName);

							accountsTotal--;
							continue;
						}

						WarnIfPasswordExpiring(account);

						var outcome = await RunAccountAsync(
							account, run.Id, accountJobs, reportDate, cancellationToken);

						reports.AddRange(outcome.Reports);

						if (outcome.LoggedIn)
						{
							loggedIn = true;
							accountsSucceeded++;
						}
						else
						{
							// Name the company. "Login failed" across two accounts is
							// not an actionable message.
							var note = $"{account.CompanyName}: {outcome.FailureReason}";
							fatalError = fatalError is null ? note : fatalError + " | " + note;
						}

						captchaMethod ??= outcome.CaptchaMethod;
						captchaAttempts += outcome.CaptchaAttempts;
						otpMethod ??= outcome.OtpMethod;
					}
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				fatalError = "The run was cancelled because the service is shutting down.";
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Run {RunId} failed outside of any single report.", run.Id);
				fatalError = ex.Message;
			}
			finally
			{
				PruneArchive();
			}

			var status = DetermineStatus(reports, fatalError, loggedIn);

			var summaryModel = new RunSummary
			{
				RunId = run.Id,
				ReportDate = reportDate,
				StartedAtLocal = startedAt,
				CompletedAtLocal = DateTime.Now,
				Status = status,
				Attempt = attempt,
				SitePortalReachable = reachable,
				LoginSucceeded = loggedIn,
				CaptchaMethod = captchaMethod,
				CaptchaAttempts = captchaAttempts,
				OtpMethod = otpMethod,
				AccountsTotal = accountsTotal,
				AccountsSucceeded = accountsSucceeded,
				ErrorMessage = fatalError,
				Reports = reports
			};

			await CompleteRunAsync(run.Id, summaryModel, cancellationToken);
			await _alerts.DispatchAsync(summaryModel, cancellationToken);
			await MarkAlertSentAsync(run.Id, cancellationToken);

			_logger.LogInformation(
				"Run {RunId} finished as {Status} in {Minutes:0.0} minutes.",
				run.Id, status, summaryModel.Duration.TotalMinutes);

			return summaryModel;
		}

		private async Task<ReportSummary> RunJobAsync(
			IfmsPortalClient portal,
			int runId,
			IfmsAccountCredentials account,
			ReportJob job,
			DateTime runReportDate,
			CancellationToken cancellationToken)
		{
			var reportDate = job.ReportDateOffsetDays.HasValue
				? DateTime.Today.AddDays(job.ReportDateOffsetDays.Value)
				: runReportDate;

			var record = await CreateReportRecordAsync(runId, account, job, reportDate, cancellationToken);

			var maxAttempts = Math.Max(1, job.MaxAttempts);
			Exception? lastError = null;

			for (var attempt = 1; attempt <= maxAttempts; attempt++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					_logger.LogInformation(
						"Fetching {Title} for {Company} (attempt {Attempt}/{Max}).",
						job.Title, account.CompanyName, attempt, maxAttempts);

					var tokens = new RunTokens(reportDate, DateTime.Now, account.UserName)
						.WithLiteral("company", account.CompanyName)
						.WithLiteral("accountKey", account.AccountKey);
					var folder = ArchiveFolder(reportDate);

					var download = await portal.DownloadReportAsync(job, tokens, folder, cancellationToken);

					var needsDate = RequiresReportDate(job.CategoryId);

					var import = await _importer.ImportAsync(
						job,
						download,
						needsDate ? reportDate : null,
						cancellationToken);

					if (!import.Success)
						throw new InvalidOperationException(import.Message);

					if (import.TotalRows == 0 && !job.AllowEmpty)
					{
						throw new InvalidOperationException(
							"The report downloaded but contained no data rows. If this report is " +
							"legitimately empty some days, set AllowEmpty on the job.");
					}

					var summary = new ReportSummary
					{
						JobKey = job.Key,
						AccountKey = account.AccountKey,
						CompanyName = account.CompanyName,
						Title = job.Title,
						CategoryId = job.CategoryId,
						Status = IfmsRunStatus.Succeeded,
						FileName = download.FileName,
						ArchivedFilePath = download.FilePath,
						DownloadedBytes = download.Bytes,
						TotalRows = import.TotalRows,
						RowsInserted = import.RowsInserted,
						RowsUpdated = import.RowsUpdated,
						RowsSkipped = import.RowsSkipped,
						Warnings = import.Warnings
					};

					await CompleteReportRecordAsync(record.Id, summary, attempt, cancellationToken);
					return summary;
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					throw;
				}
				catch (Exception ex)
				{
					lastError = ex;
					_logger.LogWarning(
						ex, "Attempt {Attempt} at {Title} failed.", attempt, job.Title);

					if (attempt < maxAttempts)
						await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
				}
			}

			var failed = new ReportSummary
			{
				JobKey = job.Key,
				AccountKey = account.AccountKey,
				CompanyName = account.CompanyName,
				Title = job.Title,
				CategoryId = job.CategoryId,
				Status = IfmsRunStatus.Failed,
				ErrorMessage = lastError?.Message ?? "Unknown failure."
			};

			await CompleteReportRecordAsync(record.Id, failed, maxAttempts, cancellationToken);
			return failed;
		}

		private static IfmsRunStatus DetermineStatus(
			List<ReportSummary> reports,
			string? fatalError,
			bool loggedIn)
		{
			if (!loggedIn || (fatalError is not null && reports.Count == 0))
				return IfmsRunStatus.Failed;

			var succeeded = reports.Count(r => r.Status == IfmsRunStatus.Succeeded);

			if (succeeded == reports.Count && fatalError is null)
				return IfmsRunStatus.Succeeded;

			return succeeded == 0 ? IfmsRunStatus.Failed : IfmsRunStatus.PartiallySucceeded;
		}

		/// <summary>
		/// Signs in as one company and pulls its reports, in its own browser and its
		/// own session, closed before the next company starts.
		/// </summary>
		private async Task<AccountOutcome> RunAccountAsync(
			IfmsAccountCredentials account,
			int runId,
			List<ReportJob> jobs,
			DateTime reportDate,
			CancellationToken cancellationToken)
		{
			var reports = new List<ReportSummary>();

			await using var scope = _scopeFactory.CreateAsyncScope();
			await using var portal = scope.ServiceProvider.GetRequiredService<IfmsPortalClient>();

			var login = await portal.LoginAsync(account, runId, cancellationToken);

			await WithAccountStoreAsync(async store =>
			{
				await store.RecordLoginAsync(
					account.AccountId, login.Success, login.FailureReason, cancellationToken);

				return true;
			});

			if (!login.Success)
			{
				return new AccountOutcome(
					false, login.FailureReason ?? "Login failed.", reports,
					login.CaptchaMethod, login.CaptchaAttempts, login.OtpMethod);
			}

			foreach (var job in jobs)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var summary = await RunJobAsync(
					portal, runId, account, job, reportDate, cancellationToken);

				reports.Add(summary);
			}

			return new AccountOutcome(
				true, null, reports,
				login.CaptchaMethod, login.CaptchaAttempts, login.OtpMethod);
		}

		/// <summary>
		/// The account store is scoped because it holds a DbContext, while this
		/// service is a singleton driving a long-running job. Every use gets its
		/// own scope rather than capturing one for the life of the run.
		/// </summary>
		private async Task<T> WithAccountStoreAsync<T>(Func<IIfmsAccountStore, Task<T>> work)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var store = scope.ServiceProvider.GetRequiredService<IIfmsAccountStore>();

			return await work(store);
		}

		private sealed record AccountOutcome(
			bool LoggedIn,
			string? FailureReason,
			List<ReportSummary> Reports,
			string? CaptchaMethod,
			int CaptchaAttempts,
			string? OtpMethod);

		/// <summary>
		/// The portal expires passwords every 80 days. Nobody should discover that
		/// from a failed 4am run, so warn while there is still time to act.
		/// </summary>
		private void WarnIfPasswordExpiring(IfmsAccountCredentials account)
		{
			if (account.PasswordExpired)
			{
				_logger.LogError(
					"The portal password for {Company} expired on {Date:dd MMM yyyy}. " +
					"Change it on the portal, then update it on the IFMS Logins page.",
					account.CompanyName, account.PasswordExpiresAt);
			}
			else if (account.DaysUntilPasswordExpires <= 10)
			{
				_logger.LogWarning(
					"The portal password for {Company} expires in {Days} days ({Date:dd MMM yyyy}).",
					account.CompanyName, account.DaysUntilPasswordExpires, account.PasswordExpiresAt);
			}
		}

		/// <summary>Matches ExcelBulkUploadController.RequiresReportDate.</summary>
		private static bool RequiresReportDate(string categoryId) =>
			categoryId is "One" or "Three" or "Six" or "Seven";

		private string ArchiveFolder(DateTime reportDate)
		{
			var root = _ifms.DownloadRoot;
			if (!Path.IsPathRooted(root))
				root = Path.Combine(AppContext.BaseDirectory, root);

			var folder = Path.Combine(root, reportDate.ToString("yyyy-MM-dd"));
			Directory.CreateDirectory(folder);
			return folder;
		}

		/// <summary>
		/// Keeps the archive from growing without bound. The originals are worth
		/// holding for a while: when a number looks wrong months later, the file
		/// the portal actually served settles the argument.
		/// </summary>
		private void PruneArchive()
		{
			if (_ifms.ArchiveRetentionDays <= 0)
				return;

			try
			{
				var root = _ifms.DownloadRoot;
				if (!Path.IsPathRooted(root))
					root = Path.Combine(AppContext.BaseDirectory, root);

				if (!Directory.Exists(root))
					return;

				var cutoff = DateTime.Today.AddDays(-_ifms.ArchiveRetentionDays);

				foreach (var folder in Directory.GetDirectories(root))
				{
					var name = Path.GetFileName(folder);
					if (DateTime.TryParse(name, out var folderDate) && folderDate < cutoff)
						Directory.Delete(folder, recursive: true);
				}
			}
			catch (Exception ex)
			{
				_logger.LogDebug("Archive pruning skipped: {Message}", ex.Message);
			}
		}

		// ------------------------------------------------------------ persistence

		/// <summary>
		/// Claims the row for this run. A manual trigger from the dashboard has
		/// already queued one as Pending, so we adopt it rather than creating a
		/// second row that would leave the queued one hanging.
		/// </summary>
		private async Task<IfmsAutomationRun> StartRunAsync(
			DateTime reportDate,
			IfmsRunTrigger trigger,
			int attempt,
			int reportCount,
			int? existingRunId,
			CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			if (existingRunId is int id)
			{
				var queued = await db.IfmsAutomationRuns
					.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

				if (queued is not null)
				{
					queued.Status = IfmsRunStatus.Running;
					queued.StartedAt = DateTime.UtcNow;
					queued.Attempt = attempt;
					queued.ReportsTotal = reportCount;
					queued.UpdatedAt = DateTime.UtcNow;

					await db.SaveChangesAsync(cancellationToken);
					return queued;
				}
			}

			var run = new IfmsAutomationRun
			{
				ReportDate = reportDate.Date,
				StartedAt = DateTime.UtcNow,
				Status = IfmsRunStatus.Running,
				Trigger = trigger,
				Attempt = attempt,
				ReportsTotal = reportCount,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			db.IfmsAutomationRuns.Add(run);
			await db.SaveChangesAsync(cancellationToken);
			return run;
		}

		private async Task<IfmsAutomationReportRun> CreateReportRecordAsync(
			int runId,
			IfmsAccountCredentials account,
			ReportJob job,
			DateTime reportDate,
			CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var record = new IfmsAutomationReportRun
			{
				RunId = runId,
				JobKey = job.Key,
				AccountKey = account.AccountKey,
				CategoryId = job.CategoryId,
				ReportTitle = account.CompanyName + " - " + job.Title,
				ReportDate = reportDate.Date,
				Status = IfmsRunStatus.Running,
				StartedAt = DateTime.UtcNow,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				AppliedFilters = SerialiseFilters(job)
			};

			db.IfmsAutomationReportRuns.Add(record);
			await db.SaveChangesAsync(cancellationToken);
			return record;
		}

		private static string? SerialiseFilters(ReportJob job)
		{
			var filters = job.Steps
				.Where(s => s.Action is "fill" or "select" or "selectText")
				.Where(s => !string.IsNullOrWhiteSpace(s.Value))
				.ToDictionary(
					s => s.Description ?? s.Selector ?? s.Action,
					s => s.Value!,
					StringComparer.OrdinalIgnoreCase);

			return filters.Count == 0 ? null : JsonSerializer.Serialize(filters);
		}

		private async Task CompleteReportRecordAsync(
			int recordId,
			ReportSummary summary,
			int attempt,
			CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var record = await db.IfmsAutomationReportRuns
				.FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);

			if (record is null)
				return;

			record.Status = summary.Status;
			record.Attempt = attempt;
			record.CompletedAt = DateTime.UtcNow;
			record.DownloadedFileName = summary.FileName;
			record.ArchivedFilePath = summary.ArchivedFilePath;
			record.DownloadedBytes = summary.DownloadedBytes;
			record.TotalRows = summary.TotalRows;
			record.RowsInserted = summary.RowsInserted;
			record.RowsUpdated = summary.RowsUpdated;
			record.RowsSkipped = summary.RowsSkipped;
			record.Warnings = summary.Warnings.Count == 0
				? null
				: string.Join(Environment.NewLine, summary.Warnings);
			record.ErrorMessage = Truncate(summary.ErrorMessage, 4000);
			record.UpdatedAt = DateTime.UtcNow;

			await db.SaveChangesAsync(cancellationToken);
		}

		private async Task CompleteRunAsync(
			int runId,
			RunSummary summary,
			CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var run = await db.IfmsAutomationRuns
				.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

			if (run is null)
				return;

			run.Status = summary.Status;
			run.CompletedAt = DateTime.UtcNow;
			run.SitePortalReachable = summary.SitePortalReachable;
			run.LoginSucceeded = summary.LoginSucceeded;
			run.CaptchaMethod = summary.CaptchaMethod;
			run.CaptchaAttempts = summary.CaptchaAttempts;
			run.OtpMethod = summary.OtpMethod;
			run.AccountsTotal = summary.AccountsTotal;
			run.AccountsSucceeded = summary.AccountsSucceeded;
			run.ReportsTotal = summary.Reports.Count;
			run.ReportsSucceeded = summary.ReportsSucceeded;
			run.ReportsFailed = summary.ReportsFailed;
			run.RowsInserted = summary.RowsInserted;
			run.RowsUpdated = summary.RowsUpdated;
			run.RowsSkipped = summary.RowsSkipped;
			run.ErrorMessage = Truncate(summary.ErrorMessage, 4000);
			run.UpdatedAt = DateTime.UtcNow;

			await db.SaveChangesAsync(cancellationToken);
		}

		private async Task MarkAlertSentAsync(int runId, CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var run = await db.IfmsAutomationRuns
				.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

			if (run is null)
				return;

			run.AlertSent = true;
			await db.SaveChangesAsync(cancellationToken);
		}

		private static string? Truncate(string? value, int max) =>
			value is null || value.Length <= max ? value : value[..max];
	}
}
