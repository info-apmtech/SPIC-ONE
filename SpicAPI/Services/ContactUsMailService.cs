using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SpicAPI.Services
{
	/// <summary>
	/// Sends the Contact Us enquiry email through the same SMTP server and
	/// configuration section (Alerts:Email) that SPIC.Ifms.Automation uses for its
	/// alert email. Only the message content differs — Contact Us paths the
	/// enquiry to its own recipients, subject and body — the connection settings,
	/// credentials and environment-variable secret pattern are shared, so there is
	/// no second SMTP configuration to maintain.
	///
	/// This is deliberately NOT EmailAlertSink: it is a small, standalone MailKit
	/// adapter so the automation's alert flow is never referenced or affected.
	/// </summary>
	public sealed class ContactUsMailService : IContactUsMailService
	{
		private readonly ContactUsMailOptions _options;
		private readonly ILogger<ContactUsMailService> _logger;

		public ContactUsMailService(
			IOptions<ContactUsMailOptions> options,
			ILogger<ContactUsMailService> logger)
		{
			_options = options.Value;
			_logger = logger;
		}

		public async Task SendAsync(
			string replyToEmail,
			string replyToName,
			string subject,
			string htmlBody,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(_options.Host))
			{
				throw new InvalidOperationException(
					"Alerts:Email:Host is not configured. Set it in appsettings.json " +
					"or via the Alerts__Email__Host environment variable.");
			}

			if (_options.To.Count == 0 || _options.To.All(string.IsNullOrWhiteSpace))
			{
				throw new InvalidOperationException(
					"Alerts:Email:To is empty. Add the Contact Us recipient so the " +
					"enquiry has somewhere to go.");
			}

			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(
				string.IsNullOrWhiteSpace(_options.FromName) ? "SPIC" : _options.FromName,
				_options.FromAddress));

			if (!string.IsNullOrWhiteSpace(replyToEmail))
				message.ReplyTo.Add(new MailboxAddress(
					string.IsNullOrWhiteSpace(replyToName) ? replyToEmail : replyToName,
					replyToEmail));

			foreach (var to in _options.To.Where(a => !string.IsNullOrWhiteSpace(a)))
				message.To.Add(MailboxAddress.Parse(to.Trim()));

			message.Subject = subject;
			message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

			using var client = new SmtpClient();

			var security = _options.UseStartTls
				? SecureSocketOptions.StartTls
				: SecureSocketOptions.Auto;

			await client.ConnectAsync(_options.Host, _options.Port, security, cancellationToken);

			if (!string.IsNullOrWhiteSpace(_options.UserName))
				await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);

			await client.SendAsync(message, cancellationToken);
			await client.DisconnectAsync(true, cancellationToken);

			_logger.LogInformation(
				"Contact Us enquiry emailed to {Recipients}.",
				string.Join(", ", _options.To));
		}
	}
}