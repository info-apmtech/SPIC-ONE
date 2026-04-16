using Microsoft.AspNetCore.Mvc;

namespace SpicAPI.Controllers
{
	[ApiController]
	[Route("api/DealerFile")]
	public class DealerFileUploadController : ControllerBase
	{
		private readonly IWebHostEnvironment _env;

		public DealerFileUploadController(IWebHostEnvironment env)
		{
			_env = env;
		}

		[HttpGet("view/{*filePath}")]
		public IActionResult ViewFile(string filePath)
		{
			var fullPath = Path.Combine(_env.ContentRootPath, "Uploads", filePath);
			if (!System.IO.File.Exists(fullPath))
				return NotFound("File not found.");

			var ext = Path.GetExtension(fullPath).ToLowerInvariant();
			var contentType = ext switch
			{
				".pdf" => "application/pdf",
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".webp" => "image/webp",
				_ => "application/octet-stream"
			};

			return PhysicalFile(fullPath, contentType);
		}

		[HttpPost("upload/{dealerId}/{docType}")]
		public async Task<IActionResult> Upload(int dealerId, string docType, IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("No file uploaded.");

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

			var pdfOrImageDocTypes = new[] { "GST", "PAN", "Aadhaar", "WholesaleLicense", "RetailLicense", "PartnerAadhaar", "PartnerPAN",
				"Specimen", "BankGuarantee", "ITReturn1", "ITReturn2", "ValuationCertificate", "RetailerList", "PartnershipDeed", "BoardResolution", "Affidavit" };
			var imageDocTypes = new[] { "ProprietorImage" };

			if (pdfOrImageDocTypes.Contains(docType))
			{
				var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
				if (!allowedExts.Contains(ext))
					return BadRequest("Only PDF, JPG, PNG, or WEBP files are allowed.");
			}
			else if (imageDocTypes.Contains(docType))
			{
				var allowedImageExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
				if (!allowedImageExts.Contains(ext))
					return BadRequest("Only JPG, PNG, or WEBP images are allowed.");
			}
			else
			{
				return BadRequest("Invalid document type.");
			}

			if (file.Length > 10 * 1024 * 1024)
				return BadRequest("File size must be less than 10 MB.");

			var folderPath = Path.Combine(_env.ContentRootPath, "Uploads", "DealerRegistration", dealerId.ToString());
			Directory.CreateDirectory(folderPath);

			var fileName = $"{docType}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
			var filePath = Path.Combine(folderPath, fileName);

			using var stream = new FileStream(filePath, FileMode.Create);
			await file.CopyToAsync(stream);

			var relativePath = $"DealerRegistration/{dealerId}/{fileName}";
			return Ok(new { filePath = relativePath });
		}
	}
}
