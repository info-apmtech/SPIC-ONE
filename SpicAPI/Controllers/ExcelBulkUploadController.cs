using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Interfaces;

namespace SpicAPI.Controllers
{
	[Authorize]
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

		public ExcelBulkUploadController(IExcelBulkUploadService uploadService)
		{
			_uploadService = uploadService;
		}

		[HttpPost("import")]
		[Consumes("multipart/form-data")]
		[RequestSizeLimit(MaxUploadBytes)]
		[RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
		public async Task<IActionResult> Import(
			[FromForm] IFormFile? file,
			[FromForm] string? categoryId,
			[FromForm] DateTime? reportDate,
			CancellationToken cancellationToken)
		{
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

			var currentUserId =
				User.FindFirstValue(ClaimTypes.NameIdentifier) ??
				User.FindFirstValue(ClaimTypes.Name) ??
				"System";

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