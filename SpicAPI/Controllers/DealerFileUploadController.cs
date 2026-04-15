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

		[HttpPost("upload/{dealerId}/{docType}")]
		public async Task<IActionResult> Upload(int dealerId, string docType, IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("No file uploaded.");

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

			var pdfDocTypes = new[] { "GST", "PAN", "Aadhaar", "WholesaleLicense", "RetailLicense", "PartnerAadhaar", "PartnerPAN" };
			var imageDocTypes = new[] { "ProprietorImage" };

			if (pdfDocTypes.Contains(docType))
			{
				if (ext != ".pdf")
					return BadRequest("Only PDF files are allowed for this document type.");
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

			if (file.Length > 5 * 1024 * 1024)
				return BadRequest("File size must be less than 5 MB.");

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
