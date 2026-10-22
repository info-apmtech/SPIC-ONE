using System.Threading;
using System.Threading.Tasks;

namespace SpicAPI.Services
{
	public interface IContactUsMailService
	{
		Task SendAsync(
			string replyToEmail,
			string replyToName,
			string subject,
			string htmlBody,
			CancellationToken cancellationToken);

		/// <summary>
		/// Sends a message with one file attached, to the recipients the caller
		/// names. It uses the same SMTP server and the same "Alerts:Email"
		/// configuration as <see cref="SendAsync"/>, so there is still only one
		/// mail setup in the application.
		///
		/// The recipients are the caller's, not the configured Contact Us list:
		/// the reconciliation report must go exactly where the user typed it.
		/// </summary>
		Task SendWithAttachmentAsync(
			string to,
			string? cc,
			string subject,
			string htmlBody,
			string attachmentFileName,
			byte[] attachmentContent,
			string attachmentContentType,
			CancellationToken cancellationToken);
	}
}