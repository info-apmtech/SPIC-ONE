using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public sealed class ExcelBulkUploadController : ControllerBase
	{
		private const long MaxUploadBytes = 100L * 1024L * 1024L;

		private static readonly HashSet<string> SupportedCategories =
			new(StringComparer.Ordinal)
			{
				"One", "Two", "Three", "Four", "Five", "Six", "Seven"
			};

		private readonly IExcelBulkUploadService _uploadService;
		private readonly IConfiguration _config;

		public ExcelBulkUploadController(
			IExcelBulkUploadService uploadService,
			IConfiguration config)
		{
			_uploadService = uploadService;
			_config = config;
		}

		/// <summary>
		/// The nightly automation uploads through this endpoint too, and there is
		/// nobody signed in at four in the morning to supply a JWT.
		///
		/// It carries a shared key instead. The key only reaches this one endpoint
		/// and it cannot read anything — the worst it permits is importing a
		/// report, which is the thing it exists to do.
		/// </summary>
		private bool HasAutomationKey()
		{
			var expected = _config["IfmsAutomation:AutomationKey"];

			if (string.IsNullOrWhiteSpace(expected))
				return false;

			var supplied = Request.Headers["X-Automation-Key"].ToString();

			return !string.IsNullOrEmpty(supplied) &&
				   CryptographicOperations.FixedTimeEquals(
					   Encoding.UTF8.GetBytes(supplied),
					   Encoding.UTF8.GetBytes(expected));
		}

		[AllowAnonymous]
		[HttpPost("import")]
		[Consumes("multipart/form-data")]
		[RequestSizeLimit(MaxUploadBytes)]
		[RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
		public async Task<IActionResult> Import(
			IFormFile? file,
			[FromForm] string? categoryId,
			[FromForm] DateTime? reportDate,
			CancellationToken cancellationToken)
		{
			var automated = HasAutomationKey();

			if (User.Identity?.IsAuthenticated != true && !automated)
			{
				return Unauthorized(new
				{
					Success = false,
					Message = "Sign in, or supply a valid X-Automation-Key."
				});
			}

			if (file is null || file.Length == 0)
			{
				return BadRequest(new
				{
					Success = false,
					Message = "No file uploaded."
				});
			}

			if (file.Length > MaxUploadBytes)
			{
				return BadRequest(new
				{
					Success = false,
					Message = "The file exceeds the 100 MB upload limit."
				});
			}

			var normalizedCategoryId = (categoryId ?? string.Empty).Trim();
			if (!SupportedCategories.Contains(normalizedCategoryId))
			{
				return BadRequest(new
				{
					Success = false,
					Message = $"Unsupported upload category '{normalizedCategoryId}'."
				});
			}

			var safeFileName = Path.GetFileName(file.FileName);
			var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
			if (extension is not ".xlsx" and not ".csv")
			{
				return BadRequest(new
				{
					Success = false,
					Message = "Only Excel .xlsx and CSV .csv files are supported. Legacy .xls is not supported."
				});
			}

			if (RequiresReportDate(normalizedCategoryId) && !reportDate.HasValue)
			{
				return BadRequest(new
				{
					Success = false,
					Message = "Report Date is required for this upload category."
				});
			}

			// Stamped on every row, so an automated import is distinguishable from
			// a hand upload months later without consulting a log.
			var currentUserId =
				User.FindFirstValue(ClaimTypes.NameIdentifier) ??
				User.FindFirstValue(ClaimTypes.Name) ??
				(automated ? "IFMS-Automation" : "System");

			await using var stream = file.OpenReadStream();
			var result = await _uploadService.ImportAsync(
				stream,
				currentUserId,
				extension,
				normalizedCategoryId,
				safeFileName,
				reportDate,
				cancellationToken);

			return result.Success ? Ok(result) : BadRequest(result);
		}

		private static bool RequiresReportDate(string categoryId) =>
			categoryId is "One" or "Three" or "Six" or "Seven";
	}
}