using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Upload endpoint used only by Logistics Master documents.
	/// It does not change the existing GenericCrudController flow.
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class LogisticsFileController : ControllerBase
	{
		private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
		private const long MaxRequestSize = 12 * 1024 * 1024; // multipart overhead included

		private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
		{
			".pdf", ".jpg", ".jpeg", ".png", ".webp",
			".doc", ".docx", ".xls", ".xlsx"
		};

		private readonly IWebHostEnvironment _environment;

		public LogisticsFileController(IWebHostEnvironment environment)
		{
			_environment = environment;
		}

		[HttpPost("upload/{entityType}/{entityId:int}/{documentType}")]
		[RequestSizeLimit(MaxRequestSize)]
		public async Task<IActionResult> Upload(
			string entityType,
			int entityId,
			string documentType,
			[FromForm] IFormFile file)
		{
			if (entityId <= 0)
				return BadRequest("A valid Logistics record id is required.");

			if (!IsSupportedEntity(entityType))
				return BadRequest("entityType must be Warehouse, CandFWarehouse or RackPoint.");

			if (file == null || file.Length == 0)
				return BadRequest("Please select a file.");

			if (file.Length > MaxFileSize)
				return BadRequest("File size must be 10 MB or less.");

			var extension = Path.GetExtension(file.FileName);
			if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
				return BadRequest("Unsupported file type.");

			var safeEntityType = SanitizeSegment(entityType);
			var safeDocumentType = SanitizeSegment(documentType);

			var webRoot = _environment.WebRootPath;
			if (string.IsNullOrWhiteSpace(webRoot))
				webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

			var folder = Path.Combine(
				webRoot,
				"uploads",
				"logistics",
				safeEntityType.ToLowerInvariant(),
				entityId.ToString(),
				safeDocumentType.ToLowerInvariant());

			Directory.CreateDirectory(folder);

			var originalBaseName = Path.GetFileNameWithoutExtension(file.FileName);
			var safeBaseName = SanitizeFileName(originalBaseName);
			if (string.IsNullOrWhiteSpace(safeBaseName))
				safeBaseName = "document";

			var storedFileName = $"{safeBaseName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
			var physicalPath = Path.Combine(folder, storedFileName);

			await using (var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				await file.CopyToAsync(stream);
			}

			var relativePath = $"/uploads/logistics/{safeEntityType.ToLowerInvariant()}/{entityId}/{safeDocumentType.ToLowerInvariant()}/{storedFileName}";

			return Ok(new
			{
				filePath = relativePath,
				fileName = file.FileName
			});
		}

		private static bool IsSupportedEntity(string value) =>
			string.Equals(value, "Warehouse", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "CandFWarehouse", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "RackPoint", StringComparison.OrdinalIgnoreCase);

		private static string SanitizeSegment(string value)
		{
			var chars = value
				.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
				.ToArray();

			return new string(chars);
		}

		private static string SanitizeFileName(string value)
		{
			var invalid = Path.GetInvalidFileNameChars();
			var chars = value
				.Where(ch => !invalid.Contains(ch))
				.Select(ch => char.IsWhiteSpace(ch) ? '_' : ch)
				.ToArray();

			return new string(chars);
		}
	}
}