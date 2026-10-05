using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SPIC.Core.Entities;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Alerts
{
	public sealed class EmailAlertSink : IAlertSink
	{
		public string Name => "Email";

		private readonly EmailAlertOptions _options;
		private readonly ILogger<EmailAlertSink> _logger;

		public EmailAlertSink(IOptions<AlertOptions> options, ILogger<EmailAlertSink> logger)
		{
			_options = options.Value.Email;
			_logger = logger;
		}

		public bool Enabled =>
			_options.Enabled &&
			!string.IsNullOrWhiteSpace(_options.Host) &&
			_options.To.Count > 0;

		public async Task SendRunSummaryAsync(RunSummary summary, CancellationToken cancellationToken)
		{
			if (_options.FailuresOnly && !summary.IsFailure)
			{
				_logger.LogDebug("Email sink is set to failures only; skipping the success notice.");
				return;
			}

			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

			foreach (var to in _options.To.Where(a => !string.IsNullOrWhiteSpace(a)))
				message.To.Add(MailboxAddress.Parse(to.Trim()));

			foreach (var cc in _options.Cc.Where(a => !string.IsNullOrWhiteSpace(a)))
				message.Cc.Add(MailboxAddress.Parse(cc.Trim()));

			var flag = summary.Status switch
			{
				IfmsRunStatus.Succeeded => "OK",
				IfmsRunStatus.PartiallySucceeded => "PARTIAL",
				_ => "FAILED"
			};

			message.Subject = $"[IFMS {flag}] {summary.ReportDate:dd-MMM-yyyy} — " +
							  $"{summary.ReportsSucceeded}/{summary.Reports.Count} reports imported";

			var builder = new BodyBuilder { HtmlBody = BuildHtml(summary) };

			if (_options.AttachReports)
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

			message.Body = builder.ToMessageBody();

			using var client = new SmtpClient();

			var security = _options.UseStartTls
				? SecureSocketOptions.StartTls
				: SecureSocketOptions.Auto;

			await client.ConnectAsync(_options.Host, _options.Port, security, cancellationToken);

			if (!string.IsNullOrWhiteSpace(_options.UserName))
				await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);

			await client.SendAsync(message, cancellationToken);
			await client.DisconnectAsync(true, cancellationToken);

			_logger.LogInformation("Run summary emailed to {Recipients}.", string.Join(", ", _options.To));
		}

		public async Task SendNoticeAsync(
			string title,
			string body,
			bool urgent,
			CancellationToken cancellationToken)
		{
			if (!Enabled)
				return;

			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

			foreach (var to in _options.To.Where(a => !string.IsNullOrWhiteSpace(a)))
				message.To.Add(MailboxAddress.Parse(to.Trim()));

			message.Subject = urgent ? $"[IFMS ACTION] {title}" : $"[IFMS] {title}";

			message.Body = new BodyBuilder
			{
				HtmlBody =
					"<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px\">" +
					$"<h2 style=\"margin:0 0 8px;color:{(urgent ? "#b42318" : "#1f2328")}\">{Escape(title)}</h2>" +
					$"<p style=\"margin:0;white-space:pre-wrap\">{Escape(body)}</p></div>"
			}.ToMessageBody();

			using var client = new SmtpClient();

			var security = _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
			await client.ConnectAsync(_options.Host, _options.Port, security, cancellationToken);

			if (!string.IsNullOrWhiteSpace(_options.UserName))
				await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);

			await client.SendAsync(message, cancellationToken);
			await client.DisconnectAsync(true, cancellationToken);

			_logger.LogInformation("Notice emailed: {Title}", title);
		}

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
