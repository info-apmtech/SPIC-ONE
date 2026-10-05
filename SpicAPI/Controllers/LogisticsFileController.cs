using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SpicAPI.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class LogisticsFileController : ControllerBase
	{
		private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
		private const long MaxRequestSize = 12 * 1024 * 1024;

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
			IFormFile file)
		{
			if (entityId <= 0)
				return BadRequest("A valid Logistics record id is required.");

			if (!IsSupportedEntity(entityType))
				return BadRequest("entityType must be Warehouse, RackPoint or Port.");

			if (string.IsNullOrWhiteSpace(documentType))
				return BadRequest("A valid document type is required.");

			if (file == null || file.Length == 0)
				return BadRequest("Please select a file.");

			if (file.Length > MaxFileSize)
				return BadRequest("File size must be 10 MB or less.");

			var extension = Path.GetExtension(file.FileName);
			if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
				return BadRequest("Unsupported file type.");

			var safeEntityType = SanitizeSegment(entityType);
			var safeDocumentType = SanitizeSegment(documentType);

			if (string.IsNullOrWhiteSpace(safeEntityType) || string.IsNullOrWhiteSpace(safeDocumentType))
				return BadRequest("Invalid upload path information.");

			var webRoot = GetWebRoot();

			// This remains compatible with the existing frontend document type keys,
			// including Insurance, Insurance_SPIC and Insurance_GFL.
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

			var storedFileName =
				$"{safeBaseName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

			var physicalPath = Path.Combine(folder, storedFileName);

			await using (var stream = new FileStream(
				physicalPath,
				FileMode.CreateNew,
				FileAccess.Write,
				FileShare.None))
			{
				await file.CopyToAsync(stream);
			}

			var relativePath =
				$"/uploads/logistics/{safeEntityType.ToLowerInvariant()}/{entityId}/{safeDocumentType.ToLowerInvariant()}/{storedFileName}";

			return Ok(new
			{
				filePath = relativePath,
				fileName = file.FileName
			});
		}

		[HttpGet("view/{*filePath}")]
		public IActionResult ViewFile(string filePath)
		{
			var (fullPath, fileName) = ResolveFilePath(filePath);
			if (fullPath == null || fileName == null)
				return NotFound("File not found.");

			var contentType = GetContentType(fileName);
			return PhysicalFile(fullPath, contentType);
		}

		[HttpGet("download/{*filePath}")]
		public IActionResult DownloadFile(string filePath)
		{
			var (fullPath, fileName) = ResolveFilePath(filePath);
			if (fullPath == null || fileName == null)
				return NotFound("File not found.");

			var contentType = GetContentType(fileName);
			return PhysicalFile(fullPath, contentType, fileName);
		}

		private (string? fullPath, string? fileName) ResolveFilePath(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath) ||
				filePath.Contains("..", StringComparison.Ordinal))
			{
				return (null, null);
			}

			// Accept both values used by existing code:
			//   logistics/warehouse/...
			//   /uploads/logistics/warehouse/...
			var normalized = filePath
				.Replace('\\', '/')
				.Trim();

			normalized = normalized.TrimStart('/');

			if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
				normalized = normalized["uploads/".Length..];

			if (string.IsNullOrWhiteSpace(normalized) || Path.IsPathRooted(normalized))
				return (null, null);

			var webRoot = GetWebRoot();
			var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads"));

			var relativePath = normalized.Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, relativePath));

			var uploadsRootWithSeparator = uploadsRoot.TrimEnd(
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

			if (!fullPath.StartsWith(uploadsRootWithSeparator, StringComparison.OrdinalIgnoreCase))
				return (null, null);

			if (!System.IO.File.Exists(fullPath))
				return (null, null);

			return (fullPath, Path.GetFileName(fullPath));
		}

		private string GetWebRoot()
		{
			var webRoot = _environment.WebRootPath;
			if (string.IsNullOrWhiteSpace(webRoot))
				webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

			return webRoot;
		}

		private static string GetContentType(string fileName)
		{
			var extension = Path.GetExtension(fileName).ToLowerInvariant();
			return extension switch
			{
				".pdf" => "application/pdf",
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".webp" => "image/webp",
				".doc" => "application/msword",
				".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
				".xls" => "application/vnd.ms-excel",
				".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				_ => "application/octet-stream"
			};
		}

		private static bool IsSupportedEntity(string value) =>
			string.Equals(value, "Warehouse", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "RackPoint", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(value, "Port", StringComparison.OrdinalIgnoreCase);

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