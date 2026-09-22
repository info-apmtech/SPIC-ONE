using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Alerts
{
	/// <summary>
	/// Emails the run summary and notices.
	///
	/// Settings are resolved at send time, not at startup: when the
	/// IfmsAlertSettings row exists and is switched on it wins, otherwise
	/// Alerts:Email from configuration applies exactly as before. Resolving late
	/// is the whole point — a password or recipient changed from the phone must
	/// take effect on the next send without a restart.
	/// </summary>
	public sealed class EmailAlertSink : IAlertSink
	{
		public string Name => "Email";

		private readonly EmailAlertOptions _options;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<EmailAlertSink> _logger;

		public EmailAlertSink(
			IOptions<AlertOptions> options,
			IServiceScopeFactory scopeFactory,
			ILogger<EmailAlertSink> logger)
		{
			_options = options.Value.Email;
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		/// <summary>
		/// Whether a send would have somewhere to go. Reads the database
		/// synchronously because the dispatcher asks on every run and the answer
		/// has to be current; false on any error rather than a throw, so a
		/// database blip never surfaces as an alert failure.
		/// </summary>
		public bool Enabled
		{
			get
			{
				try
				{
					return ResolveSync().Enabled;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Could not read the alert email settings; treating email as off.");
					return false;
				}
			}
		}

		public async Task SendRunSummaryAsync(RunSummary summary, CancellationToken cancellationToken)
		{
			var email = await ResolveAsync(cancellationToken);

			if (!email.Enabled)
			{
				_logger.LogDebug("Email sink is not configured; skipping the run summary.");
				return;
			}

			if (email.FailuresOnly && !summary.IsFailure)
			{
				_logger.LogDebug("Email sink is set to failures only; skipping the success notice.");
				return;
			}

			var flag = summary.Status switch
			{
				IfmsRunStatus.Succeeded => "OK",
				IfmsRunStatus.PartiallySucceeded => "PARTIAL",
				_ => "FAILED"
			};

			var subject = $"[IFMS {flag}] {summary.ReportDate:dd-MMM-yyyy} — " +
						  $"{summary.ReportsSucceeded}/{summary.Reports.Count} reports imported";

			var builder = new BodyBuilder { HtmlBody = BuildHtml(summary) };

			if (email.AttachReports)
			{
				foreach (var report in summary.Reports)
				{
					if (string.IsNullOrWhiteSpace(report.ArchivedFilePath) ||
						!File.Exists(report.ArchivedFilePath))
					{
						continue;
					}

					await builder.Attachments.AddAsync(report.ArchivedFilePath, cancellationToken);
				}
			}

			var message = BuildMessage(email, subject, builder.ToMessageBody(), includeCc: true);
			var result = await DeliverAsync(email, message, recordOutcome: true, cancellationToken);

			_logger.LogInformation("Run summary emailed ({Source} settings): {Result}.", email.Source, result);
		}

		public async Task SendNoticeAsync(
			string title,
			string body,
			bool urgent,
			CancellationToken cancellationToken)
		{
			var email = await ResolveAsync(cancellationToken);

			if (!email.Enabled)
				return;

			var message = BuildMessage(
				email,
				urgent ? $"[IFMS ACTION] {title}" : $"[IFMS] {title}",
				new BodyBuilder { HtmlBody = BuildNoticeHtml(title, body, urgent) }.ToMessageBody(),
				includeCc: false);

			await DeliverAsync(email, message, recordOutcome: true, cancellationToken);

			_logger.LogInformation("Notice emailed: {Title}", title);
		}

		/// <summary>
		/// Sends a short test message with whatever settings are in effect and
		/// returns a one-line result — "Sent to …" or the SMTP error — rather
		/// than throwing, because the result is what gets shown on the phone.
		/// A test is not a real alert, so it never touches LastSentAt.
		/// </summary>
		public async Task<string> SendTestAsync(CancellationToken cancellationToken)
		{
			EffectiveEmail email;

			try
			{
				email = await ResolveAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				return "Could not read the alert email settings: " + Describe(ex);
			}

			if (!email.Enabled)
			{
				return email.SwitchedOn
					? "Email settings are incomplete: an SMTP host and at least one To address are needed."
					: "Email is switched off in both the database and configuration; nothing to test.";
			}

			var body =
				"This is a test email from the SPIC IFMS automation.\n\n" +
				$"Server time : {DateTime.Now:dd MMM yyyy HH:mm:ss} ({TimeZoneInfo.Local.Id})\n" +
				$"Host        : {Environment.MachineName}\n" +
				$"Settings    : {email.Source}\n" +
				$"SMTP        : {email.Host}:{email.Port}, STARTTLS {(email.UseStartTls ? "on" : "off")}";

			var message = BuildMessage(
				email,
				"[IFMS] Test email from the automation",
				new BodyBuilder { HtmlBody = BuildNoticeHtml("SPIC IFMS test email", body, urgent: false) }.ToMessageBody(),
				includeCc: true);

			try
			{
				return await DeliverAsync(email, message, recordOutcome: false, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "The test email could not be sent.");
				return Describe(ex);
			}
		}

		/// <summary>What the CLI prints before a test: which settings apply and their shape.</summary>
		public async Task<string> DescribeAsync(CancellationToken cancellationToken)
		{
			var email = await ResolveAsync(cancellationToken);

			return
				$"Settings  : {email.Source}\n" +
				$"Enabled   : {email.SwitchedOn}\n" +
				$"Host      : {email.Host}:{email.Port}\n" +
				$"StartTls  : {email.UseStartTls}\n" +
				$"From      : {email.FromAddress}\n" +
				$"To        : {string.Join(", ", email.To)}\n" +
				$"Password  : {(string.IsNullOrEmpty(email.Password) ? "NOT SET" : "set")}";
		}

		// ------------------------------------------------------------ settings

		/// <summary>
		/// The settings a send actually uses, from whichever source won. A record
		/// so the choice is made once per send and cannot drift between connect
		/// and authenticate.
		/// </summary>
		private sealed record EffectiveEmail(
			string Source,
			bool SwitchedOn,
			string Host,
			int Port,
			bool UseStartTls,
			string UserName,
			string? Password,
			string FromAddress,
			string FromName,
			IReadOnlyList<string> To,
			IReadOnlyList<string> Cc,
			bool FailuresOnly,
			bool AttachReports)
		{
			public bool Enabled => SwitchedOn && !string.IsNullOrWhiteSpace(Host) && To.Count > 0;
		}

		private EffectiveEmail ResolveSync()
		{
			using var scope = _scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<IfmsDbContext>();
			var store = scope.ServiceProvider.GetRequiredService<IIfmsAlertSettingsStore>();

			var row = db.IfmsAlertSettings
				.AsNoTracking()
				.FirstOrDefault(s => s.Id == IfmsAlertSettingsStore.SingletonId);

			return Resolve(row, store);
		}

		private async Task<EffectiveEmail> ResolveAsync(CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var store = scope.ServiceProvider.GetRequiredService<IIfmsAlertSettingsStore>();

			return Resolve(await store.GetAsync(cancellationToken), store);
		}

		private EffectiveEmail Resolve(IfmsAlertSettings? row, IIfmsAlertSettingsStore store)
		{
			if (row is not null && row.EmailEnabled)
			{
				return new EffectiveEmail(
					Source: "database",
					SwitchedOn: true,
					Host: row.SmtpHost,
					Port: row.SmtpPort,
					UseStartTls: row.UseStartTls,
					UserName: row.UserName,
					Password: store.DecryptPassword(row),
					FromAddress: row.FromAddress,
					FromName: row.FromName,
					To: IfmsAlertSettingsStore.SplitAddresses(row.ToAddresses),
					Cc: IfmsAlertSettingsStore.SplitAddresses(row.CcAddresses),
					FailuresOnly: row.FailuresOnly,
					AttachReports: row.AttachReports);
			}

			return new EffectiveEmail(
				Source: "configuration",
				SwitchedOn: _options.Enabled,
				Host: _options.Host,
				Port: _options.Port,
				UseStartTls: _options.UseStartTls,
				UserName: _options.UserName,
				Password: _options.Password,
				FromAddress: _options.FromAddress,
				FromName: _options.FromName,
				To: CleanList(_options.To),
				Cc: CleanList(_options.Cc),
				FailuresOnly: _options.FailuresOnly,
				AttachReports: _options.AttachReports);
		}

		private static IReadOnlyList<string> CleanList(IEnumerable<string> addresses) =>
			addresses
				.Where(a => !string.IsNullOrWhiteSpace(a))
				.Select(a => a.Trim())
				.ToList();

		// ------------------------------------------------------------- sending

		private static MimeMessage BuildMessage(
			EffectiveEmail email,
			string subject,
			MimeEntity body,
			bool includeCc)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(email.FromName, email.FromAddress));

			foreach (var to in email.To)
				message.To.Add(MailboxAddress.Parse(to));

			if (includeCc)
			{
				foreach (var cc in email.Cc)
					message.Cc.Add(MailboxAddress.Parse(cc));
			}

			message.Subject = subject;
			message.Body = body;

			return message;
		}

		/// <summary>
		/// Talks to the SMTP server and returns "Sent to …". When
		/// <paramref name="recordOutcome"/> is set the result — or the error — is
		/// written to the settings row so the phone can show it; that write is
		/// best-effort and can never turn a delivered email into a failure.
		/// </summary>
		private async Task<string> DeliverAsync(
			EffectiveEmail email,
			MimeMessage message,
			bool recordOutcome,
			CancellationToken cancellationToken)
		{
			string result;

			try
			{
				using var client = new SmtpClient();

				var security = email.UseStartTls
					? SecureSocketOptions.StartTls
					: SecureSocketOptions.Auto;

				await client.ConnectAsync(email.Host, email.Port, security, cancellationToken);

				if (!string.IsNullOrWhiteSpace(email.UserName))
					await client.AuthenticateAsync(email.UserName, email.Password ?? string.Empty, cancellationToken);

				await client.SendAsync(message, cancellationToken);
				await client.DisconnectAsync(true, cancellationToken);

				result = "Sent to " + string.Join(", ", message.To.Mailboxes.Select(m => m.Address));
			}
			catch (Exception ex)
			{
				if (recordOutcome)
					await RecordSendResultAsync(Describe(ex));

				throw;
			}

			if (recordOutcome)
				await RecordSendResultAsync(result);

			return result;
		}

		private async Task RecordSendResultAsync(string result)
		{
			try
			{
				await using var scope = _scopeFactory.CreateAsyncScope();
				var store = scope.ServiceProvider.GetRequiredService<IIfmsAlertSettingsStore>();

				await store.RecordSendResultAsync(result, CancellationToken.None);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Could not record the email send result; the email itself was unaffected.");
			}
		}

		private static string Describe(Exception ex) =>
			ex.InnerException is null
				? $"{ex.GetType().Name}: {ex.Message}"
				: $"{ex.GetType().Name}: {ex.Message} ({ex.InnerException.Message})";

		// ---------------------------------------------------------------- html

		private static string BuildNoticeHtml(string title, string body, bool urgent) =>
			"<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px\">" +
			$"<h2 style=\"margin:0 0 8px;color:{(urgent ? "#b42318" : "#1f2328")}\">{Escape(title)}</h2>" +
			$"<p style=\"margin:0;white-space:pre-wrap\">{Escape(body)}</p></div>";

		private static string BuildHtml(RunSummary summary)
		{
			var accent = summary.Status switch
			{
				IfmsRunStatus.Succeeded => "#1a7f37",
				IfmsRunStatus.PartiallySucceeded => "#9a6700",
				_ => "#b42318"
			};

			var sb = new StringBuilder();

			sb.Append("<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2328\">");
			sb.Append($"<h2 style=\"margin:0 0 4px;color:{accent}\">{Escape(summary.Headline)}</h2>");
			sb.Append($"<p style=\"margin:0 0 16px;color:#57606a\">Report date {summary.ReportDate:dd MMM yyyy} · " +
					  $"started {summary.StartedAtLocal:HH:mm} · took {summary.Duration.TotalMinutes:0} min · " +
					  $"attempt {summary.Attempt}</p>");

			if (!string.IsNullOrWhiteSpace(summary.ErrorMessage))
			{
				sb.Append("<div style=\"padding:10px 12px;border-left:3px solid #b42318;background:#fff5f5;" +
						  "margin-bottom:16px;white-space:pre-wrap\">");
				sb.Append(Escape(summary.ErrorMessage));
				sb.Append("</div>");
			}

			sb.Append("<table cellspacing=\"0\" cellpadding=\"8\" style=\"border-collapse:collapse;width:100%\">");
			sb.Append("<tr style=\"background:#f6f8fa;text-align:left\">");
			sb.Append("<th>Report</th><th>Status</th><th style=\"text-align:right\">Rows</th>");
			sb.Append("<th style=\"text-align:right\">New</th><th style=\"text-align:right\">Updated</th>");
			sb.Append("<th style=\"text-align:right\">Skipped</th></tr>");

			foreach (var report in summary.Reports)
			{
				var ok = report.Status == IfmsRunStatus.Succeeded;
				var colour = ok ? "#1a7f37" : "#b42318";
				var statusLabel = ok ? "Imported" : report.Status.ToString();

				// The same report title exists under both company logins, so the
				// company has to be on the row or the reader cannot tell them apart.
				var reportLabel = string.IsNullOrWhiteSpace(report.CompanyName)
					? report.Title
					: $"{report.CompanyName} — {report.Title}";

				sb.Append("<tr style=\"border-top:1px solid #d0d7de\">");
				sb.Append($"<td>{Escape(reportLabel)}<div style=\"color:#57606a;font-size:12px\">" +
						  $"{Escape(report.FileName ?? "no file")}</div></td>");
				sb.Append($"<td style=\"color:{colour};font-weight:600\">{Escape(statusLabel)}</td>");
				sb.Append($"<td align=\"right\">{report.TotalRows:N0}</td>");
				sb.Append($"<td align=\"right\">{report.RowsInserted:N0}</td>");
				sb.Append($"<td align=\"right\">{report.RowsUpdated:N0}</td>");
				sb.Append($"<td align=\"right\">{report.RowsSkipped:N0}</td>");
				sb.Append("</tr>");

				if (!string.IsNullOrWhiteSpace(report.ErrorMessage))
				{
					sb.Append("<tr><td colspan=\"6\" style=\"color:#b42318;font-size:12px;" +
							  "padding-top:0;white-space:pre-wrap\">");
					sb.Append(Escape(report.ErrorMessage));
					sb.Append("</td></tr>");
				}

				if (report.Warnings.Count > 0)
				{
					sb.Append("<tr><td colspan=\"6\" style=\"color:#9a6700;font-size:12px;padding-top:0\">");
					sb.Append(Escape(string.Join(" · ", report.Warnings.Take(8))));
					if (report.Warnings.Count > 8)
						sb.Append($" · and {report.Warnings.Count - 8} more");
					sb.Append("</td></tr>");
				}
			}

			sb.Append("</table>");

			sb.Append("<p style=\"margin-top:20px;padding:10px 12px;background:#f6f8fa;border-radius:6px\">");
			sb.Append($"<strong>Next step:</strong> {Escape(summary.ActionRequired)}</p>");

			sb.Append("<p style=\"color:#8c959f;font-size:12px;margin-top:24px\">" +
					  "Sent by SPIC IFMS Automation. Run id " + summary.RunId + ".</p>");
			sb.Append("</div>");

			return sb.ToString();
		}

		private static string Escape(string? value) =>
			string.IsNullOrEmpty(value)
				? string.Empty
				: value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}
}
