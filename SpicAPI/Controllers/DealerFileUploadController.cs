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
			if (ext != ".pdf")
				return BadRequest("Only PDF files are allowed.");

			if (file.Length > 5 * 1024 * 1024)
				return BadRequest("File size must be less than 5 MB.");

			var allowedDocTypes = new[] { "GST", "PAN", "Aadhaar", "WholesaleLicense", "RetailLicense" };
			if (!allowedDocTypes.Contains(docType))
				return BadRequest("Invalid document type.");

			var folderPath = Path.Combine(_env.ContentRootPath, "Uploads", "DealerRegistration", dealerId.ToString());
			Directory.CreateDirectory(folderPath);

			var fileName = $"{docType}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
			var filePath = Path.Combine(folderPath, fileName);

			using var stream = new FileStream(filePath, FileMode.Create);
			await file.CopyToAsync(stream);

			var relativePath = $"DealerRegistration/{dealerId}/{fileName}";
			return Ok(new { filePath = relativePath });
		}
	}
}
