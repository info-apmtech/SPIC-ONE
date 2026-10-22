using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;
using SpicAPI.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// State-wise Global Stock Reconciliation.
	///
	/// The three steps are three endpoints, and they all read the SAME
	/// StockReconciliationResult:
	///
	///   generate     - uploads the five reports, validates them and calculates
	///                  the state-wise table once;
	///   export-excel - writes the workbook from that result (no recalculation);
	///   send-email   - attaches that same workbook (no recalculation).
	///
	/// The result travels with the request instead of being cached on the server,
	/// because the API runs as several replicas and a cached result would not be
	/// found again by the replica that receives the next call. The calculation
	/// happens once, in "generate"; the other two endpoints only format it.
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public sealed class StockReconciliationController : ControllerBase
	{
		private const long MaxUploadBytes = 100L * 1024L * 1024L;
		private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

		private readonly IStockReconciliationService _service;
		private readonly IContactUsMailService _mail;
		private readonly ContactUsMailOptions _mailOptions;
		private readonly ILogger<StockReconciliationController> _logger;

		public StockReconciliationController(
			IStockReconciliationService service,
			IContactUsMailService mail,
			IOptions<ContactUsMailOptions> mailOptions,
			ILogger<StockReconciliationController> logger)
		{
			_service = service;
			_mail = mail;
			_mailOptions = mailOptions.Value;
			_logger = logger;
		}

		/// <summary>
		/// Uploads the five report files, validates them and returns the
		/// state-wise reconciliation. When "autoSendEmail" is set the workbook is
		/// attached to one email; a failing email never fails the reconciliation.
		/// </summary>
		[HttpPost("generate")]
		[Consumes("multipart/form-data")]
		[RequestSizeLimit(MaxUploadBytes)]
		[RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
		public async Task<IActionResult> Generate(
			IFormFile? globalStockFile,
			IFormFile? salesReceiptFile,
			IFormFile[]? retailPointFiles,
			IFormFile? companySalesFile,
			IFormFile? wholesalerSalesFile,
			[FromForm] DateTime? asOnDate,
			[FromForm] string? to,
			[FromForm] string? cc,
			[FromForm] string? subject,
			[FromForm] bool autoSendEmail,
			CancellationToken cancellationToken)
		{
			if (!asOnDate.HasValue)
			{
				return BadRequest(new StockReconciliationGenerateResponse
				{
					Success = false,
					Message = "As On Date is required."
				});
			}

			var missing = new List<string>();

			if (globalStockFile is null || globalStockFile.Length == 0)
				missing.Add("Global Stock report");
			if (salesReceiptFile is null || salesReceiptFile.Length == 0)
				missing.Add("Sales & Receipt report");
			if (companySalesFile is null || companySalesFile.Length == 0)
				missing.Add("Company Sales report");
			if (wholesalerSalesFile is null || wholesalerSalesFile.Length == 0)
				missing.Add("Wholesaler Sales report");
			if (retailPointFiles is null || retailPointFiles.Length == 0 ||
				retailPointFiles.All(f => f is null || f.Length == 0))
			{
				missing.Add("Retail Point (DPT) report");
			}

			if (missing.Count > 0)
			{
				return BadRequest(new StockReconciliationGenerateResponse
				{
					Success = false,
					Message = "These files are required: " + string.Join(", ", missing) + ".",
					ValidationErrors = missing.Select(m => $"Missing file: {m}.").ToList()
				});
			}

			var unsupported = new List<string>();
			foreach (var file in AllFiles(globalStockFile, salesReceiptFile, companySalesFile, wholesalerSalesFile)
				.Concat(retailPointFiles ?? Array.Empty<IFormFile>()))
			{
				if (!IsSupported(file))
					unsupported.Add(Path.GetFileName(file.FileName));
			}

			if (unsupported.Count > 0)
			{
				return BadRequest(new StockReconciliationGenerateResponse
				{
					Success = false,
					Message = "Only Excel .xlsx and CSV .csv files are supported: " +
						string.Join(", ", unsupported.Distinct()) + "."
				});
			}

			var input = new StockReconciliationInput();

			await using (var stream = globalStockFile!.OpenReadStream())
				input.GlobalStock = await ReadFileAsync(globalStockFile, stream, cancellationToken);

			await using (var stream = salesReceiptFile!.OpenReadStream())
				input.SalesReceipt = await ReadFileAsync(salesReceiptFile, stream, cancellationToken);

			await using (var stream = companySalesFile!.OpenReadStream())
				input.CompanySales = await ReadFileAsync(companySalesFile, stream, cancellationToken);

			await using (var stream = wholesalerSalesFile!.OpenReadStream())
				input.WholesalerSales = await ReadFileAsync(wholesalerSalesFile, stream, cancellationToken);

			foreach (var file in retailPointFiles!)
			{
				if (file is null || file.Length == 0)
					continue;

				await using var stream = file.OpenReadStream();
				input.RetailPoint.Add(await ReadFileAsync(file, stream, cancellationToken));
			}

			StockReconciliationResult result;

			try
			{
				result = await _service.BuildAsync(input, asOnDate.Value.Date, cancellationToken);
			}
			catch (StockReconciliationValidationException ex)
			{
				return BadRequest(new StockReconciliationGenerateResponse
				{
					Success = false,
					Message = ex.Message,
					ValidationErrors = ex.Errors.ToList()
				});
			}

			var response = new StockReconciliationGenerateResponse
			{
				Success = true,
				Message = $"Reconciliation generated for {result.Rows.Count} states.",
				Warnings = result.Warnings ?? new List<string>(),
				Result = result
			};

			if (autoSendEmail)
			{
				if (string.IsNullOrWhiteSpace(to))
				{
					response.Email = new StockReconciliationEmailOutcome
					{
						Attempted = true,
						Sent = false,
						Message = "No recipient was entered, so the email was not sent. The reconciliation was still generated."
					};
				}
				else
				{
					response.Email = await SendReportAsync(
						result,
						to!,
						cc,
						string.IsNullOrWhiteSpace(subject) ? DefaultSubject(result) : subject!,
						cancellationToken);
				}
			}

			return Ok(response);
		}

		/// <summary>
		/// Returns the available master filter options (companies, plants, products, available dates)
		/// for the database-backed reconciliation report.
		/// </summary>
		[HttpGet("filter-options")]
		public async Task<IActionResult> FilterOptions(CancellationToken cancellationToken)
		{
			var options = await _service.GetFilterOptionsAsync(cancellationToken);
			return Ok(options);
		}

		/// <summary>
		/// Loads the state-wise reconciliation directly from already-imported database records.
		/// When "autoSendEmail" is true and a recipient email is supplied, the report is also mailed.
		/// </summary>
		[HttpPost("view")]
		public async Task<IActionResult> ViewFromDatabase(
			[FromBody] StockReconciliationDatabaseQueryRequest request,
			CancellationToken cancellationToken)
		{
			if (request is null)
				return BadRequest(new StockReconciliationGenerateResponse { Success = false, Message = "Invalid query request." });

			try
			{
				var result = await _service.BuildFromDatabaseAsync(request, cancellationToken);

				var response = new StockReconciliationGenerateResponse
				{
					Success = true,
					Message = result.Rows.Count > 0
						? "State-wise reconciliation loaded successfully."
						: "No records found for the selected filters.",
					Warnings = result.Warnings,
					Result = result
				};

				if (request.AutoSendEmail && !string.IsNullOrWhiteSpace(request.To))
				{
					response.Email = await SendReportAsync(
						result,
						request.To!,
						request.Cc,
						string.IsNullOrWhiteSpace(request.Subject) ? DefaultSubject(result) : request.Subject!,
						cancellationToken);
				}

				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error building reconciliation from database.");
				return StatusCode(500, new StockReconciliationGenerateResponse
				{
					Success = false,
					Message = "An error occurred while querying reconciliation data: " + ex.Message
				});
			}
		}

		/// <summary>
		/// Writes the workbook for a result the client already holds. It never
		/// recalculates anything, so the workbook always matches the table on the
		/// screen.
		/// </summary>
		[HttpPost("export-excel")]
		public IActionResult ExportExcel([FromBody] StockReconciliationResult? result)
		{
			if (result is null)
				return BadRequest("No reconciliation result was supplied.");

			var bytes = _service.BuildExcel(result);
			var fileName = string.IsNullOrWhiteSpace(result.FileName)
				? _service.BuildFileName(result.AsOnDate)
				: result.FileName;

			return File(bytes, ExcelContentType, fileName);
		}

		/// <summary>
		/// Attaches the workbook of a result the client already holds and mails
		/// it to the recipients the user typed.
		/// </summary>
		[HttpPost("send-email")]
		public async Task<IActionResult> SendEmail(
			[FromBody] StockReconciliationSendEmailRequest? request,
			CancellationToken cancellationToken)
		{
			if (request?.Result is null)
				return BadRequest("No reconciliation result was supplied.");

			if (string.IsNullOrWhiteSpace(request.To))
				return BadRequest("Enter at least one recipient email address.");

			var subject = string.IsNullOrWhiteSpace(request.Subject)
				? DefaultSubject(request.Result)
				: request.Subject!;

			var outcome = await SendReportAsync(
				request.Result,
				request.To!,
				request.Cc,
				subject,
				cancellationToken);

			return outcome.Sent
				? Ok(new StockReconciliationEmailOutcome
				{
					Attempted = true,
					Sent = true,
					Message = outcome.Message
				})
				: StatusCode(502, new StockReconciliationEmailOutcome
				{
					Attempted = true,
					Sent = false,
					Message = outcome.Message
				});
		}

		/// <summary>
		/// Sends the combined daily stock email (Urea stock as on PreviousDate + GFL stock as on CurrentDate)
		/// with the combined 2-sheet Excel workbook attached.
		/// Reused by manual user send and future automated scheduler.
		/// </summary>
		[HttpPost("send-daily-mail")]
		public async Task<IActionResult> SendDailyMail(
			[FromBody] DailyCombinedStockMailRequest? request,
			CancellationToken cancellationToken)
		{
			if (request is null)
				return BadRequest(new StockReconciliationEmailOutcome { Attempted = false, Sent = false, Message = "Invalid request payload." });

			if (string.IsNullOrWhiteSpace(request.To))
				return BadRequest(new StockReconciliationEmailOutcome { Attempted = false, Sent = false, Message = "Enter at least one recipient email address." });

			var currentDate = request.AsOnDate?.Date ?? GetIndiaBusinessDate();

			try
			{
				var mailPackage = await _service.BuildDailyStockMailAsync(currentDate, cancellationToken);
				if (!mailPackage.IsValid)
				{
					var errorMsg = string.Join(" ", mailPackage.ValidationErrors);
					return BadRequest(new StockReconciliationEmailOutcome
					{
						Attempted = false,
						Sent = false,
						Message = errorMsg
					});
				}

				var subject = string.IsNullOrWhiteSpace(request.Subject)
					? mailPackage.Subject
					: request.Subject!;

				await _mail.SendWithAttachmentAsync(
					request.To!,
					request.Cc,
					subject,
					mailPackage.HtmlBody,
					mailPackage.AttachmentFileName,
					mailPackage.ExcelAttachment ?? Array.Empty<byte>(),
					ExcelContentType,
					cancellationToken);

				return Ok(new StockReconciliationEmailOutcome
				{
					Attempted = true,
					Sent = true,
					Message = $"Daily stock report emailed successfully to {request.To}."
				});
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send combined daily stock email.");
				return StatusCode(502, new StockReconciliationEmailOutcome
				{
					Attempted = true,
					Sent = false,
					Message = $"Could not send daily stock email: {ex.Message}"
				});
			}
		}

		/// <summary>
		/// Previews the combined daily stock email (subject, HTML body with 2 tables, and validation).
		/// </summary>
		[HttpPost("preview-daily-mail")]
		public async Task<IActionResult> PreviewDailyMail(
			[FromBody] DailyCombinedStockMailRequest? request,
			CancellationToken cancellationToken)
		{
			var currentDate = request?.AsOnDate?.Date ?? GetIndiaBusinessDate();
			var mailPackage = await _service.BuildDailyStockMailAsync(currentDate, cancellationToken);
			return Ok(mailPackage);
		}

		/// <summary>
		/// Exports the combined 2-sheet Excel workbook (Sheet 1: Urea Stock, Sheet 2: GFL Stock).
		/// </summary>
		[HttpPost("export-daily-excel")]
		public async Task<IActionResult> ExportDailyExcel(
			[FromBody] DailyCombinedStockMailRequest? request,
			CancellationToken cancellationToken)
		{
			var currentDate = request?.AsOnDate?.Date ?? GetIndiaBusinessDate();
			var mailPackage = await _service.BuildDailyStockMailAsync(currentDate, cancellationToken);

			if (mailPackage.ExcelAttachment == null || mailPackage.ExcelAttachment.Length == 0)
			{
				return BadRequest(new { Message = "Could not generate workbook. " + string.Join(" ", mailPackage.ValidationErrors) });
			}

			return File(mailPackage.ExcelAttachment, ExcelContentType, mailPackage.AttachmentFileName);
		}

		private static DateTime GetIndiaBusinessDate()
		{
			try
			{
				var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
				return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone).Date;
			}
			catch
			{
				try
				{
					var istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
					return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone).Date;
				}
				catch
				{
					return DateTime.UtcNow.AddHours(5.5).Date;
				}
			}
		}

		/// <summary>
		/// The values the screen needs to pre-fill the email form. The recipient
		/// is deliberately NOT returned: the reconciliation must go exactly where
		/// the user typed it.
		/// </summary>
		[HttpGet("email-defaults")]
		public IActionResult EmailDefaults()
		{
			return Ok(new
			{
				FromAddress = _mailOptions.FromAddress,
				FromName = _mailOptions.FromName,
				SubjectTemplate = "Urea & GFL Stock - {CurrentDate:dd-MM-yyyy}",
				Host = _mailOptions.Host
			});
		}

		// ------------------------------------------------------------------ helpers

		private async Task<StockReconciliationEmailOutcome> SendReportAsync(
			StockReconciliationResult result,
			string to,
			string? cc,
			string subject,
			CancellationToken cancellationToken)
		{
			try
			{
				var bytes = _service.BuildExcel(result);
				var fileName = string.IsNullOrWhiteSpace(result.FileName)
					? _service.BuildFileName(result.AsOnDate)
					: result.FileName;

				await _mail.SendWithAttachmentAsync(
					to,
					cc,
					subject,
					BuildEmailBody(result),
					fileName,
					bytes,
					ExcelContentType,
					cancellationToken);

				return new StockReconciliationEmailOutcome
				{
					Attempted = true,
					Sent = true,
					Message = $"The reconciliation was emailed to {to}."
				};
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				// The reconciliation itself was calculated fine, so the failure is
				// reported on its own and never turns into a failed generation.
				_logger.LogError(ex, "State-wise stock reconciliation email failed.");

				return new StockReconciliationEmailOutcome
				{
					Attempted = true,
					Sent = false,
					Message = $"The reconciliation was generated, but the email could not be sent: {ex.Message}"
				};
			}
		}

		private static string DefaultSubject(StockReconciliationResult result) =>
			$"State-wise Global Stock Reconciliation - {result.AsOnDate:dd-MM-yyyy}";

		private static string BuildEmailBody(StockReconciliationResult result)
		{
			var totals = result.Totals ?? new StockReconciliationTotals();

			var builder = new StringBuilder();
			builder.Append("<html><body style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#0f172a\">");
			builder.Append("<h2 style=\"margin:0 0 4px 0\">State-wise Global Stock Reconciliation</h2>");
			builder.Append("<p style=\"margin:0 0 16px 0;color:#475569\">As on ");
			builder.Append(result.AsOnDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));

			if (!string.IsNullOrWhiteSpace(result.Company))
				builder.Append(" | Company: ").Append(result.Company);
			if (!string.IsNullOrWhiteSpace(result.Plant))
				builder.Append(" | Plant: ").Append(result.Plant);
			if (!string.IsNullOrWhiteSpace(result.Product))
				builder.Append(" | Product: ").Append(result.Product);

			builder.Append("</p>");
			builder.Append("<table cellpadding=\"6\" cellspacing=\"0\" border=\"1\" style=\"border-collapse:collapse;border-color:#cbd5e1\">");
			builder.Append("<tr style=\"background:#0f172a;color:#ffffff;font-weight:bold\">");
			foreach (var header in new[]
			{
				"State", "Closing Stock", "Closing GIT", "Global Stock Total",
				"Wholesaler Stock", "Retail Point Stock", "Company Sales in GIT",
				"Wholesaler Sales in GIT", "Channel Total"
			})
			{
				builder.Append("<th style=\"text-align:right\">").Append(header).Append("</th>");
			}

			builder.Append("</tr>");

			foreach (var row in result.Rows ?? new List<StockReconciliationRow>())
			{
				builder.Append("<tr>");
				builder.Append("<td>").Append(row.State).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.ClosingStock)).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.ClosingGit)).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.GlobalStockTotal)).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.WholesalerStock)).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.RetailPointStock)).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.CompanySalesInGit)).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.WholesalerSalesInGit)).Append("</td>");
				builder.Append("<td style=\"text-align:right\">").Append(Number(row.ChannelTotal)).Append("</td>");
				builder.Append("</tr>");
			}

			builder.Append("<tr style=\"font-weight:bold;background:#f1f5f9\">");
			builder.Append("<td>Overall Total</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.ClosingStock)).Append("</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.ClosingGit)).Append("</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.GlobalStockTotal)).Append("</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.WholesalerStock)).Append("</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.RetailPointStock)).Append("</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.CompanySalesInGit)).Append("</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.WholesalerSalesInGit)).Append("</td>");
			builder.Append("<td style=\"text-align:right\">").Append(Number(totals.ChannelTotal)).Append("</td>");
			builder.Append("</tr></table>");
			builder.Append("<p style=\"margin:16px 0 0 0;color:#64748b\">The workbook is attached.</p>");
			builder.Append("</body></html>");

			return builder.ToString();
		}

		private static string Number(decimal value) =>
			value.ToString(
				value == decimal.Truncate(value) ? "#,##0;-#,##0;0" : "#,##0.###;-#,##0.###;0",
				CultureInfo.InvariantCulture);

		private static IEnumerable<IFormFile> AllFiles(params IFormFile?[] files) =>
			files.Where(file => file is not null).Select(file => file!);

		private static bool IsSupported(IFormFile file)
		{
			var extension = Path.GetExtension(Path.GetFileName(file.FileName)).ToLowerInvariant();
			return extension is ".xlsx" or ".csv";
		}

		private static async Task<StockReconciliationSourceFile> ReadFileAsync(
			IFormFile file,
			Stream stream,
			CancellationToken cancellationToken)
		{
			// The file is buffered once: ClosedXML needs a seekable stream and the
			// reader must be able to look at the file more than once.
			var buffer = new MemoryStream();
			await stream.CopyToAsync(buffer, cancellationToken);
			buffer.Position = 0;

			return new StockReconciliationSourceFile
			{
				Content = buffer,
				FileName = Path.GetFileName(file.FileName)
			};
		}
	}
}
