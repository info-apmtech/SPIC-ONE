using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using Tesseract;
using System.Text.RegularExpressions;

namespace SpicAPI.Controllers
{
	[Authorize]
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
				   "LlpAgreement", "AuthorizationLetter",
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
			return Ok(new { FilePath = relativePath });
		}

		// =====================================================================
		//  Aadhaar / PAN / GST OCR
		// =====================================================================

		[HttpPost("ocr-extract")]
		public async Task<IActionResult> OcrExtract(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return Ok(new
				{
					Aadhaar = (string?)null,
					PAN = (string?)null,
					GST = (string?)null,
					GSTLegalName = (string?)null,
					GSTTradeName = (string?)null,
					GSTConstitutionofBusiness = (string?)null
				});

			string? extractedAadhaar = null;
			string? extractedPan = null;
			string? extractedGst = null;
			string? legalName = null;
			string? tradeName = null;
			string? gstConstitution = null;

			try
			{
				var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
				var imageExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };

				if (!imageExts.Contains(ext) && ext != ".pdf")
					return Ok(new { Aadhaar = extractedAadhaar, PAN = extractedPan, GST = extractedGst, GSTLegalName = legalName, GSTTradeName = tradeName, GSTConstitutionofBusiness = gstConstitution });

				if (imageExts.Contains(ext))
				{
					using var ms = new MemoryStream();
					await file.CopyToAsync(ms);
					ms.Position = 0;

					(extractedAadhaar, extractedPan, extractedGst, legalName, tradeName, gstConstitution) = OcrImageBytes(ms.ToArray());
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

						(extractedAadhaar, extractedPan, extractedGst, legalName, tradeName, gstConstitution) = ExtractFromPdf(tempFile);
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
				GST = extractedGst,
				GSTLegalName = legalName,
				GSTTradeName = tradeName,
				GSTConstitutionofBusiness = gstConstitution
			});
		}

		private (string? aadhaar, string? pan, string? gst, string? legalName, string? tradeName, string? gstConstitution)
		ExtractFromPdf(string pdfPath)
		{
			var result = ExtractTextFromPdfPig(pdfPath);

			if (result.aadhaar != null || result.pan != null || result.gst != null ||
				result.legalName != null || result.tradeName != null || result.gstConstitution != null)
			{
				return result;
			}

			return OcrPdfPages(pdfPath);
		}

		private static (string? aadhaar, string? pan, string? gst, string? legalName, string? tradeName, string? gstConstitution)
		ExtractTextFromPdfPig(string pdfPath)
		{
			using var pdf = UglyToad.PdfPig.PdfDocument.Open(pdfPath);

			var sb = new System.Text.StringBuilder();

			foreach (var page in pdf.GetPages())
			{
				// page.Text often jams words together with no spaces, which breaks
				// GSTIN word boundaries and makes each field swallow the next label.
				// GetWords() gives one token per visual word — join with a space so
				// the label/value regexes have real separators to work with.
				var words = page.GetWords();
				if (words != null && words.Any())
					sb.Append(string.Join(" ", words.Select(w => w.Text)));
				else
					sb.Append(page.Text);   // fallback if no word layer

				sb.Append(' ');
			}

			var norm = Regex.Replace(sb.ToString(), "\r\n|\n|\r", " ").Trim();

			return ExtractValuesFromText(norm);
		}

		private (string? aadhaar, string? pan, string? gst, string? legalName, string? tradeName, string? gstConstitution)
		OcrPdfPages(string pdfPath)
		{
			string? aadhaar = null;
			string? pan = null;
			string? gst = null;
			string? legalName = null;
			string? tradeName = null;
			string? gstConstitution = null;

			var pdfBytes = System.IO.File.ReadAllBytes(pdfPath);
			var pageCount = PDFtoImage.Conversion.GetPageCount(pdfBytes, null);

			using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

			for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
			{
				try
				{
					using var ms = new MemoryStream();

					PDFtoImage.Conversion.SavePng(
						ms,
						pdfBytes,
						(Index)pageIndex,
						null,
						new PDFtoImage.RenderOptions { Dpi = 300 });

					ms.Position = 0;

					using var pix = Pix.LoadFromMemory(ms.ToArray());
					using var page = engine.Process(pix);

					var text = page.GetText() ?? string.Empty;
					var norm = Regex.Replace(text, "\r\n|\n|\r", " ").Trim();

					var result = ExtractValuesFromText(norm);

					aadhaar ??= result.aadhaar;
					pan ??= result.pan;
					gst ??= result.gst;
					legalName ??= result.legalName;
					tradeName ??= result.tradeName;
					gstConstitution ??= result.gstConstitution;

					if (aadhaar != null && pan != null && gst != null &&
						legalName != null && tradeName != null && gstConstitution != null)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "OCR failed for PDF page {PageIndex} of {PdfPath}", pageIndex, pdfPath);
				}
			}

			return (aadhaar, pan, gst, legalName, tradeName, gstConstitution);
		}

		private static (string? aadhaar, string? pan, string? gst, string? legalName, string? tradeName, string? gstConstitution)
		OcrImageBytes(byte[] imageBytes)
		{
			using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
			using var img = Pix.LoadFromMemory(imageBytes);
			using var page = engine.Process(img);

			var text = page.GetText() ?? string.Empty;
			var norm = Regex.Replace(text, "\r\n|\n|\r", " ").Trim();

			return ExtractValuesFromText(norm);
		}

		// =====================================================================
		//  Cheque OCR  (preprocessing + multi-pass + voting)
		// =====================================================================

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
					(accountNumber, ifsc, branch, accountHolderName) = OcrChequeFromPng(ms.ToArray());
				}
				else
				{
					_logger.LogInformation("Cheque OCR processing as PDF: {FileName}", file.FileName);
					var tempDir = Path.Combine(_env.ContentRootPath, "Uploads", "_temp");
					Directory.CreateDirectory(tempDir);
					var safeFileName = Path.GetFileName(file.FileName);
					var tempFile = Path.Combine(tempDir, $"ocr_cheque_{DateTime.Now:yyyyMMddHHmmssfff}_{safeFileName}");
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
			// 1) Digital PDF text layer (non-scanned PDFs)
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

			// 2) Render each page -> multi-pass OCR + vote
			string? acc2 = null, ifsc2 = null, branch2 = null, holder2 = null;
			try
			{
				var pdfBytes = System.IO.File.ReadAllBytes(pdfPath);
				var pageCount = PDFtoImage.Conversion.GetPageCount(pdfBytes, null);
				_logger.LogInformation("Cheque PDF has {PageCount} page(s) for OCR", pageCount);

				for (int p = 0; p < pageCount; p++)
				{
					try
					{
						using var msPng = new MemoryStream();
						// Dpi 300 important — default (~96dpi) gives poor OCR
						PDFtoImage.Conversion.SavePng(msPng, pdfBytes, (Index)p, null,
							new PDFtoImage.RenderOptions { Dpi = 300 });

						var (a, i, b, h) = OcrChequeFromPng(msPng.ToArray());
						acc2 ??= a;
						ifsc2 ??= i;
						branch2 ??= b;
						holder2 ??= h;

						if (acc2 != null && ifsc2 != null) break;
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "OCR failed for cheque PDF page {Page} of {PdfPath}", p, pdfPath);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Image-based OCR failed for cheque PDF: {PdfPath}", pdfPath);
			}

			return (acc2, ifsc2, branch2, holder2);
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

		// Multi-pass OCR on a single PNG/JPEG cheque, with voting for robustness.
		private (string? acc, string? ifsc, string? branch, string? holder) OcrChequeFromPng(byte[] sourceImageBytes)
		{
			// (scale, threshold, page-seg-mode). null threshold = grayscale only.
			// Binarized passes read the account number on coloured/mesh backgrounds;
			// raw-grayscale passes read the printed holder NAME more cleanly.
			var passes = new (double scale, float? thr, PageSegMode psm)[]
			{
				(2.0, 0.50f,  PageSegMode.Auto),
				(2.0, 0.50f,  PageSegMode.SingleBlock),
				(1.0, 0.486f, PageSegMode.Auto),
				(1.0, null,   PageSegMode.Auto),
				(1.0, null,   PageSegMode.SingleColumn),
			};

			var accVotes = new Dictionary<string, int>();
			var ifscVotes = new Dictionary<string, int>();
			var holderVotes = new Dictionary<string, int>();
			string? branch = null;

			using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

			foreach (var (scale, thr, psm) in passes)
			{
				try
				{
					var pre = PreprocessPng(sourceImageBytes, scale, thr);
					var text = OcrPngToText(engine, pre, psm);
					if (string.IsNullOrWhiteSpace(text)) continue;

					var (a, i, b, h) = ExtractChequeValuesFromText(text);
					if (!string.IsNullOrWhiteSpace(a)) accVotes[a!] = accVotes.GetValueOrDefault(a!) + 1;
					if (!string.IsNullOrWhiteSpace(i)) ifscVotes[i!] = ifscVotes.GetValueOrDefault(i!) + 1;
					if (!string.IsNullOrWhiteSpace(h)) holderVotes[h!] = holderVotes.GetValueOrDefault(h!) + 1;
					branch ??= b;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Cheque OCR pass failed (scale={Scale}, thr={Thr})", scale, thr);
				}
			}

			var bestAcc = PickBestAccount(accVotes);
			var bestIfsc = PickBestIfsc(ifscVotes);
			var bestHolder = holderVotes.Count == 0 ? null :
				holderVotes.OrderByDescending(k => k.Value).ThenByDescending(k => k.Key.Length)
						   .Select(k => k.Key).First();

			_logger.LogInformation("Cheque vote summary - accVotes=[{Acc}] ifscVotes=[{Ifsc}] holderVotes=[{Holder}]",
				string.Join(",", accVotes.Select(k => $"{k.Key}:{k.Value}")),
				string.Join(",", ifscVotes.Select(k => $"{k.Key}:{k.Value}")),
				string.Join(",", holderVotes.Select(k => $"{k.Key}:{k.Value}")));

			return (bestAcc, bestIfsc, branch, bestHolder);
		}

		private string OcrPngToText(TesseractEngine engine, byte[] pngBytes, PageSegMode psm)
		{
			engine.DefaultPageSegMode = psm;
			using var pix = Pix.LoadFromMemory(pngBytes);
			using var page = engine.Process(pix);
			return (page.GetText() ?? string.Empty).Trim();
		}

		// Fold truncated account variants into the longer form that contains them,
		// then pick by (votes, length). Handles OCR dropping leading/trailing digits.
		private static string? PickBestAccount(Dictionary<string, int> votes)
		{
			if (votes.Count == 0) return null;

			var merged = new Dictionary<string, int>();
			foreach (var k in votes.Keys.OrderByDescending(s => s.Length))
			{
				var host = merged.Keys.FirstOrDefault(m => m.Contains(k));
				if (host != null) merged[host] += votes[k];
				else merged[k] = votes[k];
			}

			return merged
				.OrderByDescending(kv => kv.Value)
				.ThenByDescending(kv => kv.Key.Length)
				.Select(kv => kv.Key)
				.First();
		}

		// Prefer a canonical IFSC (4 letters + '0' + 6 digits) over OCR letter-misreads.
		private static string? PickBestIfsc(Dictionary<string, int> votes)
		{
			if (votes.Count == 0) return null;
			return votes
				.OrderByDescending(kv => Regex.IsMatch(kv.Key, @"^[A-Z]{4}0[0-9]{6}$") ? 1 : 0)
				.ThenByDescending(kv => kv.Value)
				.Select(kv => kv.Key)
				.First();
		}

		// =====================================================================
		//  OCR image preprocessing (SkiaSharp — comes transitively with PDFtoImage)
		// =====================================================================

		private static byte[] PreprocessPng(byte[] src, double scale, float? threshold)
		{
			using var input = SKBitmap.Decode(src);
			if (input == null) return src;

			SKBitmap bmp;
			if (Math.Abs(scale - 1.0) < 0.001)
			{
				bmp = input;
			}
			else
			{
				int w = Math.Max(1, (int)(input.Width * scale));
				int h = Math.Max(1, (int)(input.Height * scale));
				var info = new SKImageInfo(w, h, input.ColorType, input.AlphaType);
				bmp = input.Resize(info, SKFilterQuality.Medium) ?? input;  // SkiaSharp 2.88.x
			}

			try
			{
				var pixels = bmp.Pixels; // SKColor[]
				for (int i = 0; i < pixels.Length; i++)
				{
					var c = pixels[i];
					int g = (c.Red * 299 + c.Green * 587 + c.Blue * 114) / 1000; // luminance
					byte v = threshold.HasValue
						? (byte)(g < threshold.Value * 255f ? 0 : 255)   // binarize
						: (byte)g;                                       // grayscale only
					pixels[i] = new SKColor(v, v, v);
				}
				bmp.Pixels = pixels;

				using var image = SKImage.FromBitmap(bmp);
				using var data = image.Encode(SKEncodedImageFormat.Png, 100);
				return data.ToArray();
			}
			finally
			{
				if (!ReferenceEquals(bmp, input))
					bmp.Dispose();
			}
		}

		// =====================================================================
		//  Cheque field extraction (regex)
		// =====================================================================

		private static (string? accountNumber, string? ifsc, string? branch, string? accountHolderName)
			ExtractChequeValuesFromText(string rawText)
		{
			rawText ??= "";

			var text = Regex.Replace(rawText, @"\r\n|\n|\r", " ");
			text = Regex.Replace(text, @"\s+", " ").Trim();

			var ifsc = TryExtractIfsc(text);
			var accountNumber = TryExtractAccountNumber(text);

			// ── Branch ───────────────────────────────────────────────
			string? branch = null;
			var branchMatch = Regex.Match(text,
				@"\bBRANCH\s*[:\-]?\s*([A-Za-z\s]+?)(?=\s+(?:IFSC|IFS|MICR|A\/C|ACCOUNT|$))",
				RegexOptions.IgnoreCase);
			if (branchMatch.Success)
				branch = Regex.Replace(branchMatch.Groups[1].Value, @"\s+", " ").Trim();

			// ── Account holder name ──────────────────────────────────
			// Only accept names introduced by "For" / "M/S" / "MESSRS".
			// If the cheque has no such marker (e.g. personal SBI cheque), leave empty.
			string? accountHolderName = null;
			var holderPatterns = new[]
			{
				@"^(?:For|M/S|M/S\.|MESSRS|MESSRS\.)\s+([A-Z].+)",
				@"\bFor\s+([A-Z][A-Za-z\s&.]+?)(?:\s+(?:ACCOUNT|CHEQUE|ADDRESS|AUTHORISED|AUTHORIZED|PLEASE|SIGNATOR|PROP|PARTNER)|$)",
				@"\b(?:M/S|MESSRS)\.?\s+([A-Z][A-Za-z\s&.]+)"
			};
			foreach (var line in rawText.Split('\n', '\r'))
			{
				var trimmed = line.Trim();
				if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 3) continue;

				foreach (var pattern in holderPatterns)
				{
					var hm = Regex.Match(trimmed, pattern, RegexOptions.IgnoreCase);
					if (hm.Success)
					{
						var candidate = Regex.Replace(hm.Groups[1].Value, @"\s+", " ").Trim();
						candidate = candidate.TrimEnd(',', '.', ';', '-', '/', '*');
						// reject cheque boilerplate that isn't a real account-holder name
						if (Regex.IsMatch(candidate,
								@"\b(MONTH|MONTHS|VALID|ONLY|BEARER|RUPEES|PAYABLE|BRANCH|CLEARING|TRANSFER|SIGN|SIGNATOR|HOLIDAY|SUNDAY|PREFERRED)\b",
								RegexOptions.IgnoreCase))
							continue;
						// must look like a real name: enough letters, not just an abbreviation
						var letters = candidate.Count(char.IsLetter);
						if (candidate.Length >= 4 && letters >= 4)
						{
							accountHolderName = candidate.ToUpperInvariant();
							break;
						}
					}
				}
				if (!string.IsNullOrWhiteSpace(accountHolderName))
					break;
			}

			return (accountNumber, ifsc, branch, accountHolderName);
		}

		// ---------- IFSC (OCR-tolerant) ----------
		private static string? TryExtractIfsc(string rawText)
		{
			if (string.IsNullOrWhiteSpace(rawText)) return null;
			var text = Regex.Replace(rawText, @"\s+", " ");

			var labelRx = new Regex(
				@"(?:IFSC|IFS\s*Code|RTGS\s*/?\s*NEFT\s*IFSC|NEFT\s*IFSC|\bCode)\s*[\s:;.\-]*([A-Za-z0-9]{4}[0OoDQ][A-Za-z0-9]{6})",
				RegexOptions.IgnoreCase);
			foreach (Match m in labelRx.Matches(text))
			{
				var f = NormalizeIfsc(m.Groups[1].Value);
				if (f != null) return f;
			}

			var looseRx = new Regex(@"\b([A-Za-z]{4}[0Oo][A-Za-z0-9]{6})\b");
			foreach (Match m in looseRx.Matches(text))
			{
				var f = NormalizeIfsc(m.Groups[1].Value);
				if (f != null) return f;
			}
			return null;
		}

		private static string? NormalizeIfsc(string token)
		{
			if (string.IsNullOrWhiteSpace(token)) return null;
			token = token.Trim().ToUpperInvariant();
			if (token.Length != 11) return null;

			var c = token.ToCharArray();
			for (int i = 0; i < 4; i++) c[i] = DigitToLetter(c[i]); // bank code = letters
			if (c[4] is 'O' or 'Q' or 'D') c[4] = '0';              // reserved 5th char = 0

			var s = new string(c);
			return Regex.IsMatch(s, @"^[A-Z]{4}0[A-Z0-9]{6}$") ? s : null;
		}

		private static char DigitToLetter(char ch) => ch switch
		{
			'0' => 'O',
			'1' => 'I',
			'2' => 'Z',
			'5' => 'S',
			'6' => 'G',
			'8' => 'B',
			_ => ch
		};

		// ---------- Account number ----------
		// The account number sits beside the "A/c No." / "खाता सं." field and appears
		// BEFORE noise numbers (validity-box amount, MICR line, txn no, prefix).
		//  1) a contiguous run preceded by an A/c label
		//  2) else earliest valid contiguous run not preceded by a noise keyword
		//  3) else a spaced/dashed group as last resort
		// NOTE: generic "Account" is intentionally NOT a label — "SB ACCOUNT" would
		// otherwise grab the wrong number on SBI cheques.
		private static string? TryExtractAccountNumber(string rawText)
		{
			if (string.IsNullOrWhiteSpace(rawText)) return null;
			var text = Regex.Replace(rawText, @"[ \t]+", " ");

			var negKeyword = new Regex(
				@"(PREFIX|MICR|CHEQUE\s*NO|CHQ|CTS|VALID|UPTO|LACS|LAKHS|TEL|FAX|PHONE|SWIFT|SERIES|CODE|DATE|BRANCH)\D{0,12}$",
				RegexOptions.IgnoreCase);

			// A/c No / Alc No / AicNo (OCR variants) or खाता
			var acctLabel = new Regex(
				@"(?:\bA\s*[/\\il]?\s*c\b\.?\s*(?:No|N0)?|\u0916\u093E\u0924\u093E)",
				RegexOptions.IgnoreCase);

			var runs = new List<(string val, int idx)>();
			foreach (Match m in Regex.Matches(text, @"\d{9,18}"))
				if (IsValidChequeAccountNo(m.Value))
					runs.Add((m.Value, m.Index));
			runs = runs.OrderBy(r => r.idx).ToList();

			// 1) run preceded (within 35 chars) by an A/c label
			foreach (var r in runs)
			{
				int from = Math.Max(0, r.idx - 35);
				var pre = text.Substring(from, r.idx - from);
				if (negKeyword.IsMatch(pre)) continue;
				if (acctLabel.IsMatch(pre)) return r.val;
			}

			// 2) earliest run not preceded by a noise keyword
			foreach (var r in runs)
			{
				int from = Math.Max(0, r.idx - 16);
				var pre = text.Substring(from, r.idx - from);
				if (negKeyword.IsMatch(pre)) continue;
				return r.val;
			}

			// 3) last resort: spaced/dashed groups
			foreach (Match m in Regex.Matches(text, @"\d{3,6}(?:[ \-]\d{3,6}){1,4}"))
			{
				var d = Regex.Replace(m.Value, @"\D", "");
				if (d.Length < 9 || d.Length > 18 || !IsValidChequeAccountNo(d)) continue;
				int from = Math.Max(0, m.Index - 16);
				var pre = text.Substring(from, m.Index - from);
				if (!negKeyword.IsMatch(pre)) return d;
			}

			return null;
		}

		private static bool IsValidChequeAccountNo(string candidate)
		{
			candidate = Regex.Replace(candidate ?? "", @"\D", "");

			if (candidate.Length < 8 || candidate.Length > 20)
				return false;

			if (Regex.IsMatch(candidate, @"^(\d)\1+$"))
				return false;

			if (candidate.StartsWith("0000"))
				return false;

			var falsePositives = new[]
			{
				"0523600100",      // SBI prefix
                "0000000000",
				"1111111111",
				"9999999999"
			};
			if (falsePositives.Contains(candidate))
				return false;

			if (Regex.IsMatch(candidate, @"^[0-9]{3}[0-9]{6}[0-9]{3}$"))
			{
				if (candidate.Length == 11 && !candidate.StartsWith("20") && !candidate.StartsWith("44") && !candidate.StartsWith("09"))
					return false;
			}

			return true;
		}

		// =====================================================================
		//  Aadhaar / PAN / GST field extraction (regex)
		// =====================================================================

		private static (string? aadhaar, string? pan, string? gst, string? legalName, string? tradeName, string? gstConstitution)
		ExtractValuesFromText(string text)
		{
			string? aadhaar = null;
			string? pan = null;
			string? gst = null;
			string? legalName = null;
			string? tradeName = null;
			string? gstConstitution = null;

			text ??= "";

			var normalized = Regex.Replace(text, @"\r\n|\n|\r", " ");
			normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

			var panMatch = Regex.Match(normalized, @"\b[A-Z]{5}[0-9]{4}[A-Z]\b", RegexOptions.IgnoreCase);
			if (panMatch.Success)
				pan = panMatch.Value.ToUpperInvariant();

			var gstMatch = Regex.Match(normalized, @"\b\d{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]\b", RegexOptions.IgnoreCase);
			if (gstMatch.Success)
				gst = gstMatch.Value.ToUpperInvariant();

			// ── GST certificate (REG-06) detail fields ────────────────
			legalName = ExtractGstFieldValue(normalized, @"Legal\s*Name\s*(?:of\s*Business)?");
			tradeName = ExtractGstFieldValue(normalized, @"Trade\s*Name\s*,?\s*if\s*any|Trade\s*Name");
			gstConstitution = ExtractGstFieldValue(normalized, @"Constitution\s*of\s*Business");

			// Constitution fallback: match a known constitution type directly
			// (handles row-number / spacing noise after the label).
			if (string.IsNullOrWhiteSpace(gstConstitution))
			{
				// (a) fuzzy keyword anywhere (tolerates minor OCR errors in the middle)
				var cm = Regex.Match(normalized,
					@"\b(Propri\w*|Partnership|Private\s+Limited\s+Company|Public\s+Limited\s+Company|Limited\s+Liability\s+Partnership|Hindu\s+Undivided\s+Family|Society|Trust|Government\s+Department|Public\s+Sector\s+Undertaking|Unlimited\s+Company|Local\s+Authority|Statutory\s+Body|Foreign\s+Company)\b",
					RegexOptions.IgnoreCase);
				if (cm.Success)
					gstConstitution = cm.Groups[1].Value;
			}

			// (b) last resort: take the word(s) right after the label
			if (string.IsNullOrWhiteSpace(gstConstitution))
			{
				var lm = Regex.Match(normalized,
					@"Constitution\s*of\s*Business\s*[:\-]?\s*(?:\d+\s*[\.\)]\s*)?([A-Za-z][A-Za-z ]{2,40}?)(?=\s+(?:\d+\s*[\.\)]|Address|Date|Type|Particulars)\b|$)",
					RegexOptions.IgnoreCase);
				if (lm.Success)
					gstConstitution = lm.Groups[1].Value.Trim();
			}

			// ── Aadhaar ───────────────────────────────────────────────
			// remove 16-digit VID and masked aadhaar first
			normalized = Regex.Replace(normalized,
				@"(?<!\d)\d{4}\s?\d{4}\s?\d{4}\s?\d{4}(?!\d)", " ", RegexOptions.IgnoreCase);
			normalized = Regex.Replace(normalized,
				@"(?:X|x|\*){4}\s?(?:X|x|\*){4}\s?\d{4}", " ", RegexOptions.IgnoreCase);

			var aadhaarMatches = Regex.Matches(normalized, @"(?<!\d)\d{4}\s?\d{4}\s?\d{4}(?!\d)");
			foreach (Match match in aadhaarMatches)
			{
				var candidate = Regex.Replace(match.Value, @"\D", "");
				if (IsValidAadhaarCandidate(candidate))
				{
					aadhaar = candidate;
					break;
				}
			}

			return (aadhaar, pan, gst, legalName, tradeName, gstConstitution);
		}

		// Extract the value following a GST-certificate field label, stopping at the next known label.
		// Extract the value following a GST-certificate field label, stopping at the next known label.
		// Works across REG-06 layout variants (with or without the "Additional trade names" row)
		// because it keys off label text, not the printed row number.
		private static string? ExtractGstFieldValue(string text, string labelPattern)
		{
			// Order matters: longer / more-specific labels must appear BEFORE shorter ones
			// in the alternation, so "Additional trade names" is caught before "Trade Name".
			var stopWords =
				@"GSTIN|Legal\s*Name|Additional\s*trade\s*names|Trade\s*Name|Constitution\s*of\s*Business|Address|Date\s*of\s*Liability|Date\s*of\s*Registration|Date\s*of\s*Validity|Taxpayer\s*Type|Type\s*of\s*Registration|Status|Centre\s*Jurisdiction|State\s*Jurisdiction|Principal\s*Place|Particulars\s*of\s*Approving";

			var pattern = $@"(?:{labelPattern})\s*[:\-]?\s*(.+?)(?=\s+(?:\d+\s*[\.\)]\s+)?(?:{stopWords})\b|$)";

			var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
			if (!match.Success)
				return null;

			var value = Regex.Replace(match.Groups[1].Value, @"\s+", " ").Trim();
			value = Regex.Replace(value, @"^\d+\s*[\.\)]\s*", "");        // strip leading row number
			value = Regex.Replace(value, @"\s+\d+\s*[\.\)]?\s*$", "");    // strip trailing row number ("... 3.")
			value = value.Trim(':', '-', '.', ',', ';');
			value = value.TrimEnd();

			return string.IsNullOrWhiteSpace(value) || value.Length < 2 ? null : value;
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