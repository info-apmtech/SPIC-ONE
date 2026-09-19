using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SPIC.Core.Entities;
using SpicAPI.Services;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ContactUsController : ControllerBase
	{
		private readonly IContactUsMailService _mail;
		private readonly ILogger<ContactUsController> _logger;

		public ContactUsController(
			IContactUsMailService mail,
			ILogger<ContactUsController> logger)
		{
			_mail = mail;
			_logger = logger;
		}

		/// <summary>
		/// Sends a Contact Us enquiry email to the recipients configured under
		/// Alerts:Email. The enquiry form is public, so this endpoint is anonymous.
		/// </summary>
		[AllowAnonymous]
		[HttpPost("enquiry")]
		public async Task<IActionResult> SendEnquiry(
			[FromBody] ContactUsMessage? message,
			CancellationToken cancellationToken)
		{
			if (message is null)
				return BadRequest("A message is required.");

			if (string.IsNullOrWhiteSpace(message.FullName) ||
				string.IsNullOrWhiteSpace(message.EmailAddress) ||
				string.IsNullOrWhiteSpace(message.Subject) ||
				string.IsNullOrWhiteSpace(message.Message))
			{
				return BadRequest("Full name, email address, subject and message are all required.");
			}

			var enquiryType = message.EnquiryType?.ToString() ?? "General Enquiry";

			var subject =
				$"Contact Us Enquiry - {enquiryType} - {message.Subject}";

			var body =
				$"<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2328\">" +
				$"<h2 style=\"margin:0 0 8px\">Contact Us Enquiry</h2>" +
				$"<table cellspacing=\"0\" cellpadding=\"6\" style=\"border-collapse:collapse\">" +
				$"<tr><td style=\"color:#57606a;padding-right:16px\">Name</td><td>{Escape(message.FullName)}</td></tr>" +
				$"<tr><td style=\"color:#57606a;padding-right:16px\">Email</td><td>{Escape(message.EmailAddress)}</td></tr>" +
				$"<tr><td style=\"color:#57606a;padding-right:16px\">Enquiry Type</td><td>{Escape(enquiryType)}</td></tr>" +
				$"<tr><td style=\"color:#57606a;padding-right:16px\">Subject</td><td>{Escape(message.Subject)}</td></tr>" +
				$"<tr><td style=\"color:#57606a;padding-right:16px\">Message</td><td>{Escape(message.Message)}</td></tr>" +
				$"</table></div>";

			try
			{
				await _mail.SendAsync(
					message.EmailAddress,
					message.FullName,
					subject,
					body,
					cancellationToken);

				return Ok(new { sent = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send Contact Us enquiry email.");
				return StatusCode(500, "The enquiry could not be sent right now. Please try again later.");
			}
		}

		private static string Escape(string? value) =>
			string.IsNullOrEmpty(value)
				? string.Empty
				: value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}
}