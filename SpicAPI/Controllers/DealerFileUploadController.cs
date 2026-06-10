using Microsoft.AspNetCore.Mvc;
using Tesseract;

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

        [HttpPost("ocr-extract")]
        public async Task<IActionResult> OcrExtract(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Ok(new { Aadhaar = (string?)null, PAN = (string?)null, GST = (string?)null });

            string? extractedAadhaar = null;
            string? extractedPan = null;
            string? extractedGst = null;

            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var imageExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!imageExts.Contains(ext) && ext != ".pdf")
                    return Ok(new { Aadhaar = extractedAadhaar, PAN = extractedPan, GST = extractedGst });

                if (imageExts.Contains(ext))
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    ms.Position = 0;
                    (extractedAadhaar, extractedPan, extractedGst) = OcrImageBytes(ms.ToArray());
                }
                else
                {
                    var tempDir = Path.Combine(_env.ContentRootPath, "Uploads", "_temp");
                    Directory.CreateDirectory(tempDir);
                    var tempFile = Path.Combine(tempDir, $"ocr_{DateTime.Now:yyyyMMddHHmmssfff}_{file.FileName}");
                    try
                    {
                        await using (var fs = new FileStream(tempFile, FileMode.Create))
                            await file.CopyToAsync(fs);

                        (extractedAadhaar, extractedPan, extractedGst) = ExtractFromPdf(tempFile);
                    }
                    finally
                    {
                        if (System.IO.File.Exists(tempFile))
                            System.IO.File.Delete(tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                //_logger.LogWarning(ex, "OCR extraction failed for file {FileName}", file.FileName);
            }

            return Ok(new { Aadhaar = extractedAadhaar, PAN = extractedPan, GST = extractedGst });
        }

        private (string? aadhaar, string? pan, string? gst) ExtractFromPdf(string pdfPath)
        {
            // Step 1: Try direct text extraction via PdfPig (for text-based PDFs)
            string? aadhaar, pan, gst;
            (aadhaar, pan, gst) = ExtractTextFromPdfPig(pdfPath);

            if (aadhaar != null || pan != null || gst != null)
            {
                //_logger.LogInformation("PdfPig extracted values from PDF: Aadhaar={Aadhaar}, PAN={Pan}, GST={Gst}", aadhaar, pan, gst);
                return (aadhaar, pan, gst);
            }

            // Step 2: No values found — treat as scanned/image-based PDF, fall back to OCR
            //_logger.LogInformation("PdfPig returned no values, falling back to image-based OCR for {PdfPath}", pdfPath);
            (aadhaar, pan, gst) = OcrPdfPages(pdfPath);

            return (aadhaar, pan, gst);
        }

        private static (string? aadhaar, string? pan, string? gst) ExtractTextFromPdfPig(string pdfPath)
        {
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
            var sb = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.Append(page.Text);
                sb.Append(' ');
            }
            var norm = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "\r\n|\n|\r", " ").Trim();
            return ExtractValuesFromText(norm);
        }

        private (string? aadhaar, string? pan, string? gst) OcrPdfPages(string pdfPath)
        {
            string? aadhaar = null, pan = null, gst = null;

            var pdfBytes = System.IO.File.ReadAllBytes(pdfPath);
            var pageCount = PDFtoImage.Conversion.GetPageCount(pdfBytes, null);
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                try
                {
                    using var ms = new MemoryStream();
                    PDFtoImage.Conversion.SavePng(ms, pdfBytes, (Index)pageIndex, null, new PDFtoImage.RenderOptions());
                    ms.Position = 0;
                    var pngBytes = ms.ToArray();

                    using var pix = Pix.LoadFromMemory(pngBytes);
                    using var page = engine.Process(pix);
                    var text = page.GetText() ?? string.Empty;
                    var norm = System.Text.RegularExpressions.Regex.Replace(text, "\r\n|\n|\r", " ").Trim();

                    var (a, p, g) = ExtractValuesFromText(norm);
                    aadhaar ??= a;
                    pan ??= p;
                    gst ??= g;

                    if (aadhaar != null && pan != null && gst != null)
                        break;
                }
                catch (Exception ex)
                {
                    //_logger.LogWarning(ex, "OCR failed for PDF page {PageIndex} of {PdfPath}", pageIndex, pdfPath);
                }
            }

            return (aadhaar, pan, gst);
        }

        //private (string? aadhaar, string? pan, string? gst) OcrImageFile(string imagePath)
        //{
        //    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        //    using var imgStream = System.IO.File.OpenRead(imagePath);
        //    using var img = Pix.LoadFromMemory(SpicAPI.Controllers.Helpers.StreamUtils.ReadStreamFully(imgStream));
        //    using var page = engine.Process(img);
        //    var text = page.GetText() ?? string.Empty;
        //    var norm = System.Text.RegularExpressions.Regex.Replace(text, "\r\n|\n|\r", " ").Trim();
        //    return ExtractValuesFromText(norm);
        //}

        private static (string? aadhaar, string? pan, string? gst) OcrImageBytes(byte[] imageBytes)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            using var img = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(img);
            var text = page.GetText() ?? string.Empty;
            var norm = System.Text.RegularExpressions.Regex.Replace(text, "\r\n|\n|\r", " ").Trim();
            return ExtractValuesFromText(norm);
        }

        private static (string? aadhaar, string? pan, string? gst) ExtractValuesFromText(string text)
        {
            string? aadhaar = null, pan = null, gst = null;

            // Aadhaar: try with optional spaces first, then bare 12 digits
            var aadhaarMatch = System.Text.RegularExpressions.Regex.Match(text, "(\\d{4}\\s?\\d{4}\\s?\\d{4})|(\\d{12})");
            if (aadhaarMatch.Success)
                aadhaar = aadhaarMatch.Value.Replace(" ", "");

            // PAN: try exact match first
            var panMatch = System.Text.RegularExpressions.Regex.Match(text, "[A-Z]{5}[0-9]{4}[A-Z]{1}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (panMatch.Success)
                pan = panMatch.Value.ToUpperInvariant();

            // GST: try exact match first
            var gstMatch = System.Text.RegularExpressions.Regex.Match(text, "\\d{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (gstMatch.Success)
                gst = gstMatch.Value.ToUpperInvariant();

            // If any value still missing, retry on space-stripped text (OCR/PdfPig may insert spaces)
            if (pan == null || gst == null)
            {
                var compact = text.Replace(" ", "");

                if (pan == null)
                {
                    panMatch = System.Text.RegularExpressions.Regex.Match(compact, "[A-Z]{5}[0-9]{4}[A-Z]{1}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (panMatch.Success)
                        pan = panMatch.Value.ToUpperInvariant();
                }

                if (gst == null)
                {
                    gstMatch = System.Text.RegularExpressions.Regex.Match(compact, "\\d{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (gstMatch.Success)
                        gst = gstMatch.Value.ToUpperInvariant();
                }
            }

            return (aadhaar, pan, gst);
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
