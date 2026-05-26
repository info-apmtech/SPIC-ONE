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
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".csv" => "text/csv",
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
                "Specimen", "GreenstarSpecimen", "BankGuarantee", "DeedOfGuarantee", "ITReturn1", "ITReturn2", "ValuationCertificate", "PartnershipDeed", "BoardResolution", "Affidavit",
				// Investment / Assets related docs
				"LandEC", "LandPropertyDoc", "LandValuationCert",
                "BuildingEC", "BuildingPropertyDoc", "BuildingValuationCert" };
            var imageDocTypes = new[] { "ProprietorImage" };
            var excelCsvDocTypes = new[] { "RetailerList" };

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
            else if (excelCsvDocTypes.Contains(docType))
            {
                var allowedExcelCsvExts = new[] { ".xlsx", ".xls", ".csv" };
                if (!allowedExcelCsvExts.Contains(ext))
                    return BadRequest("Only Excel (.xlsx, .xls) or CSV (.csv) files are allowed.");
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
            // return file path in response
            return Ok(new { FilePath = relativePath });
        }

        [HttpDelete("delete")]
        public IActionResult DeleteFile([FromQuery] string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return BadRequest("File path is required.");

            // Prevent directory traversal attacks
            if (filePath.Contains("..") || Path.IsPathRooted(filePath))
                return BadRequest("Invalid file path.");

            try
            {
                var fullPath = Path.Combine(_env.ContentRootPath, "Uploads", filePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Ok(new { message = "File deleted successfully", filePath });
                }

                return NotFound(new { message = "File not found on disk", filePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to delete file: {ex.Message}" });
            }
        }
    }
}
