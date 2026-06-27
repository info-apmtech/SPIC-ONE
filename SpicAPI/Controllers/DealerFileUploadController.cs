using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Tesseract;
using System.Text.RegularExpressions;

namespace SpicAPI.Controllers
{
    [ApiController]
    [Route("api/DealerFile")]
    public class DealerFileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DealerFileUploadController> _logger;

        public DealerFileUploadController(IWebHostEnvironment env, ILogger<DealerFileUploadController> logger)
        {
            _env = env;
            _logger = logger;
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

            var pdfOrImageDocTypes = new[] { "GST", "PAN", "Aadhaar", "Cheque", "WholesaleLicense", "RetailLicense", "PartnerAadhaar", "PartnerPAN",
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

					var safeFileName = Path.GetFileName(file.FileName);
					var tempFile = Path.Combine(tempDir, $"ocr_{DateTime.Now:yyyyMMddHHmmssfff}_{safeFileName}");

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
				_logger.LogWarning(ex, "OCR extraction failed for file {FileName}", file.FileName);
			}

			return Ok(new
			{
				Aadhaar = extractedAadhaar,
				PAN = extractedPan,
				GST = extractedGst
			});
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

        [HttpPost("ocr-extract-cheque")]
        public async Task<IActionResult> OcrExtractCheque(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Cheque OCR called with null or empty file");
                return Ok(new { AccountNumber = (string?)null, IFSC = (string?)null, Branch = (string?)null, AccountHolderName = (string?)null });
            }

            _logger.LogInformation("Cheque OCR starting for file: {FileName}, size: {Size} bytes, type: {ContentType}",
                file.FileName, file.Length, file.ContentType);

            string? accountNumber = null;
            string? ifsc = null;
            string? branch = null;
            string? accountHolderName = null;

            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var imageExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!imageExts.Contains(ext) && ext != ".pdf")
                {
                    _logger.LogWarning("Cheque OCR: unsupported file extension {Ext} for file {FileName}", ext, file.FileName);
                    return Ok(new { AccountNumber = accountNumber, IFSC = ifsc, Branch = branch, AccountHolderName = accountHolderName });
                }

                if (imageExts.Contains(ext))
                {
                    _logger.LogInformation("Cheque OCR processing as image: {FileName}", file.FileName);
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    ms.Position = 0;
                    var text = OcrImageBytesToText(ms.ToArray());
                    _logger.LogInformation("Cheque image OCR produced text length: {Length}", text?.Length ?? 0);
                    (accountNumber, ifsc, branch, accountHolderName) = ExtractChequeValuesFromText(text);
                }
                else
                {
                    _logger.LogInformation("Cheque OCR processing as PDF: {FileName}", file.FileName);
                    var tempDir = Path.Combine(_env.ContentRootPath, "Uploads", "_temp");
                    Directory.CreateDirectory(tempDir);
                    var tempFile = Path.Combine(tempDir, $"ocr_cheque_{DateTime.Now:yyyyMMddHHmmssfff}_{file.FileName}");
                    try
                    {
                        await using (var fs = new FileStream(tempFile, FileMode.Create))
                            await file.CopyToAsync(fs);

                        _logger.LogInformation("Cheque PDF saved to temp file: {TempFile}", tempFile);
                        (accountNumber, ifsc, branch, accountHolderName) = ExtractChequeFromPdf(tempFile);
                    }
                    finally
                    {
                        if (System.IO.File.Exists(tempFile))
                        {
                            System.IO.File.Delete(tempFile);
                            _logger.LogInformation("Cheque temp file deleted: {TempFile}", tempFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cheque OCR extraction failed for file {FileName}", file.FileName);
            }

            _logger.LogInformation("Cheque OCR result - AccountNumber: {Acc}, IFSC: {Ifsc}, Branch: {Branch}, AccountHolderName: {Holder}",
                accountNumber ?? "(null)", ifsc ?? "(null)", branch ?? "(null)", accountHolderName ?? "(null)");

            return Ok(new { AccountNumber = accountNumber, IFSC = ifsc, Branch = branch, AccountHolderName = accountHolderName });
        }

        private (string? accountNumber, string? ifsc, string? branch, string? accountHolderName) ExtractChequeFromPdf(string pdfPath)
        {
            try
            {
                var text = ExtractRawPdfPigText(pdfPath);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var (acc, ifs, br, holder) = ExtractChequeValuesFromText(text);
                    if (acc != null || ifs != null || br != null || holder != null)
                        return (acc, ifs, br, holder);
                }
                _logger.LogInformation("PdfPig extracted text from cheque PDF but no bank values found, falling back to image OCR");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PdfPig text extraction failed for cheque PDF, falling back to image OCR: {PdfPath}", pdfPath);
            }

            _logger.LogInformation("Running image-based OCR on cheque PDF: {PdfPath}", pdfPath);
            try
            {
                var text = OcrPdfPagesToText(pdfPath);
                if (!string.IsNullOrWhiteSpace(text))
                    return ExtractChequeValuesFromText(text);
                _logger.LogWarning("Image-based OCR returned no text for cheque PDF: {PdfPath}", pdfPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image-based OCR failed for cheque PDF: {PdfPath}", pdfPath);
            }

            return (null, null, null, null);
        }

        private static string ExtractRawPdfPigText(string pdfPath)
        {
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
            var sb = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.Append(page.Text);
                sb.Append(' ');
            }
            return sb.ToString().Trim();
        }

        private string OcrPdfPagesToText(string pdfPath)
        {
            try
            {
                var pdfBytes = System.IO.File.ReadAllBytes(pdfPath);
                var pageCount = PDFtoImage.Conversion.GetPageCount(pdfBytes, null);
                _logger.LogInformation("Cheque PDF has {PageCount} page(s) for OCR", pageCount);

                using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
                var fullText = new System.Text.StringBuilder();

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
                        var pageText = page.GetText() ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(pageText))
                            _logger.LogInformation("Cheque PDF page {PageIndex} OCR produced {CharCount} chars", pageIndex, pageText.Length);

                        fullText.Append(pageText);
                        fullText.Append('\n');
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "OCR failed for cheque PDF page {PageIndex} of {PdfPath}", pageIndex, pdfPath);
                    }
                }

                var result = fullText.ToString().Trim();
                _logger.LogInformation("Cheque PDF OCR total text length: {Length}", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PDF OCR for cheque: {PdfPath}", pdfPath);
                return string.Empty;
            }
        }

        private static string OcrImageBytesToText(byte[] imageBytes)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            using var img = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(img);
            return (page.GetText() ?? string.Empty).Trim();
        }

        private static (string? accountNumber, string? ifsc, string? branch, string? accountHolderName) ExtractChequeValuesFromText(string rawText)
        {
            string? accountNumber = null;
            string? ifsc = null;
            string? branch = null;
            string? accountHolderName = null;

            // Normalize for existing IFSC/Account/Branch extraction
            var text = System.Text.RegularExpressions.Regex.Replace(rawText, "\r\n|\n|\r", " ").Trim();

            // ── IFSC Code ────────────────────────────────────────────────────
            var ifscMatch = System.Text.RegularExpressions.Regex.Match(text, "[A-Z]{4}0[A-Z0-9]{6}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (ifscMatch.Success)
                ifsc = ifscMatch.Value.ToUpperInvariant();

            if (ifsc == null)
            {
                var compact = text.Replace(" ", "");
                ifscMatch = System.Text.RegularExpressions.Regex.Match(compact, "[A-Z]{4}0[A-Z0-9]{6}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (ifscMatch.Success)
                    ifsc = ifscMatch.Value.ToUpperInvariant();
            }

            // ── Account Number ────────────────────────────────────────────────
            var accountMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b\d{9,18}\b");
            if (accountMatch.Success)
            {
                accountNumber = accountMatch.Value;
            }
            else
            {
                var spacedMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b\d{2,6}(?:\s?\d{2,6}){2,5}\b");
                if (spacedMatch.Success)
                {
                    var cleaned = System.Text.RegularExpressions.Regex.Replace(spacedMatch.Value, @"\s+", "");
                    if (cleaned.Length >= 9 && cleaned.Length <= 18)
                        accountNumber = cleaned;
                }
            }

            if (accountNumber == null)
            {
                var anyDigits = System.Text.RegularExpressions.Regex.Matches(text, @"\d{9,}");
                foreach (System.Text.RegularExpressions.Match m in anyDigits)
                {
                    var candidate = m.Value;
                    if (candidate.Length >= 9 && candidate.Length <= 18)
                    {
                        accountNumber = candidate;
                        break;
                    }
                }
            }

            // ── Branch Name ──────────────────────────────────────────────────
            if (ifsc != null)
            {
                var ifscIndex = text.IndexOf(ifsc, StringComparison.OrdinalIgnoreCase);
                if (ifscIndex >= 0)
                {
                    var afterIfsc = text.Substring(ifscIndex + ifsc.Length).Trim();
                    var branchMatch = System.Text.RegularExpressions.Regex.Match(afterIfsc, @"^[\s,]*([A-Za-z\s]+?)(?=[,\d]|$)");
                    if (branchMatch.Success)
                        branch = branchMatch.Groups[1].Value.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(branch))
            {
                var branchKeywordMatch = System.Text.RegularExpressions.Regex.Match(text, @"BRANCH\s*[:\-]?\s*([A-Za-z\s]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (branchKeywordMatch.Success)
                    branch = branchKeywordMatch.Groups[1].Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(branch))
            {
                var atMatch = System.Text.RegularExpressions.Regex.Match(text, @"\bAT\s+([A-Za-z\s]+?)(?:\s+(?:PO|DIST|DISTRICT|STATE))", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (atMatch.Success)
                    branch = atMatch.Groups[1].Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(branch))
            {
                var cityMatch = System.Text.RegularExpressions.Regex.Match(text, @"\b(?:CITY|VILL|VILLAGE|TOWN)\s*[:\-]?\s*([A-Za-z\s]+?)(?:\s+(?:DIST|DISTRICT|STATE|PIN))", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (cityMatch.Success)
                    branch = cityMatch.Groups[1].Value.Trim();
            }

            // ── Account Holder Name ──────────────────────────────────────────
            // Extract line-by-line from raw text to respect line boundaries.
            // Only the line containing M/S/MESSRS is used; subsequent lines with
            // OCR noise, MICR numbers, or cheque instructions are ignored.
            foreach (var line in rawText.Split('\n', '\r'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                    continue;

                var holderMatch = System.Text.RegularExpressions.Regex.Match(trimmed,
                    @"^(?:M/S|M/S\.|MESSRS|MESSRS\.)\s*(.+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (holderMatch.Success)
                {
                    accountHolderName = holderMatch.Groups[1].Value.Trim();
                    accountHolderName = System.Text.RegularExpressions.Regex.Replace(accountHolderName, @"\s+", " ");
                    accountHolderName = accountHolderName.TrimEnd(',', '.', ';', '-', '/');
                    if (string.IsNullOrWhiteSpace(accountHolderName))
                        accountHolderName = null;
                    break;
                }
            }

            return (accountNumber, ifsc, branch, accountHolderName);
        }

		private static (string? aadhaar, string? pan, string? gst) ExtractValuesFromText(string text)
		{
			string? aadhaar = null;
			string? pan = null;
			string? gst = null;

			text ??= "";

			var normalized = Regex.Replace(text, @"\r\n|\n|\r", " ");
			normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

			// PAN
			var panMatch = Regex.Match(normalized, @"\b[A-Z]{5}[0-9]{4}[A-Z]\b", RegexOptions.IgnoreCase);
			if (panMatch.Success)
				pan = panMatch.Value.ToUpperInvariant();

			// GST
			var gstMatch = Regex.Match(normalized, @"\b\d{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]\b", RegexOptions.IgnoreCase);
			if (gstMatch.Success)
				gst = gstMatch.Value.ToUpperInvariant();

			// IMPORTANT: remove any 16 digit VID first
			normalized = Regex.Replace(
				normalized,
				@"(?<!\d)\d{4}\s?\d{4}\s?\d{4}\s?\d{4}(?!\d)",
				" ",
				RegexOptions.IgnoreCase);

			// Remove masked Aadhaar also
			normalized = Regex.Replace(
				normalized,
				@"(?:X|x|\*){4}\s?(?:X|x|\*){4}\s?\d{4}",
				" ",
				RegexOptions.IgnoreCase);

			// Extract Aadhaar only after removing VID
			var aadhaarMatches = Regex.Matches(
				normalized,
				@"(?<!\d)\d{4}\s?\d{4}\s?\d{4}(?!\d)"
			);

			foreach (Match match in aadhaarMatches)
			{
				var candidate = Regex.Replace(match.Value, @"\D", "");

				if (IsValidAadhaarCandidate(candidate))
				{
					aadhaar = candidate;
					break;
				}
			}

			return (aadhaar, pan, gst);
		}

		private static bool IsValidAadhaarCandidate(string candidate)
		{
			candidate = Regex.Replace(candidate ?? "", @"\D", "");

			if (candidate.Length != 12)
				return false;

			if (candidate.StartsWith("0") || candidate.StartsWith("1"))
				return false;

			if (Regex.IsMatch(candidate, @"^(\d)\1{11}$"))
				return false;

			return true;
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
