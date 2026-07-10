using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DealerBulkUploadController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DealerBulkUploadController> _logger;

        public DealerBulkUploadController(AppDbContext db, ILogger<DealerBulkUploadController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

            // ── State lookup ───────────────────────────────────────────
            var stateNameToId = _db.States
                .Select(s => new { s.StateName, s.Id })
                .AsEnumerable()
                .ToDictionary(s => s.StateName.Trim(), s => s.Id,
                    StringComparer.OrdinalIgnoreCase);

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            // ── Build header map ───────────────────────────────────────
            var headerRow = ws.Row(1);
            var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

            if (lastCol == 0)
                return BadRequest(new { Success = false, Message = "Empty worksheet or missing header row" });

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = headerRow.Cell(c).GetString().Trim()
                                 .Replace(" ", "").ToLowerInvariant();
                if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h))
                    headerMap[h] = c;
            }

            string GetCell(IXLRow row, string key) =>
                headerMap.TryGetValue(key, out var col)
                    ? row.Cell(col).GetString().Trim()
                    : string.Empty;

            if (!headerMap.ContainsKey("customer"))
                return BadRequest(new { Success = false, Message = "Missing required column: CUSTOMER" });
            if (!headerMap.ContainsKey("customername"))
                return BadRequest(new { Success = false, Message = "Missing required column: CUSTOMER NAME" });

            var rows = ws.RowsUsed().Skip(1).ToList();

            // ── Pass 1: collect all numeric codes from the file ────────
            var allNumericCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var customer = GetCell(row, "customer");
                if (string.IsNullOrEmpty(customer)) continue;
                var code = Regex.Replace(customer, @"[^0-9]", "");
                if (!string.IsNullOrEmpty(code)) allNumericCodes.Add(code);
            }

            // ── Pass 2: load only matching dealers from DB ─────────────
            // Match against SPICCode (D-prefix), GreenStarCode (Z/N/T-prefix), TnCode
            var matchingDealers = await _db.DealerRegistrations
                .Where(d =>
                    (d.SPICCode != null && allNumericCodes.Contains(d.SPICCode)) ||
                    (d.GreenStarCode != null && allNumericCodes.Contains(d.GreenStarCode)) ||
				   (d.TnCode != null && allNumericCodes.Contains(d.TnCode)) ||
					(d.NCode != null && allNumericCodes.Contains(d.NCode)) ||
					(d.DealerCode != null && allNumericCodes.Contains(d.DealerCode)))
				.ToListAsync();
            // Index by every code field so we can find a dealer by numeric part
            var dealerByNumeric = new Dictionary<string, DealerRegistration>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var d in matchingDealers)
            {
                if (!string.IsNullOrEmpty(d.SPICCode))
                    dealerByNumeric.TryAdd(d.SPICCode, d);
                if (!string.IsNullOrEmpty(d.GreenStarCode))
                    dealerByNumeric.TryAdd(d.GreenStarCode, d);
				if (!string.IsNullOrEmpty(d.TnCode))
					dealerByNumeric.TryAdd(d.TnCode, d);
				if (!string.IsNullOrEmpty(d.NCode))
					dealerByNumeric.TryAdd(d.NCode, d);
				if (!string.IsNullOrEmpty(d.DealerCode))
					dealerByNumeric.TryAdd(d.DealerCode, d);
			}

            // ── Error tracking ─────────────────────────────────────────
            var groupedErrors = new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

            void AddGrouped(string group, string item)
            {
                if (!groupedErrors.TryGetValue(group, out var list))
                    groupedErrors[group] = list = new List<string>();
                list.Add(item);
            }

            var now = DateTime.UtcNow;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            int inserted = 0, updated = 0;

            // In-batch tracking: numericCode → new entity (not yet saved)
            var batchNew = new Dictionary<string, DealerRegistration>(
                StringComparer.OrdinalIgnoreCase);

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        var customer = GetCell(row, "customer");
                        var customerName = GetCell(row, "customername");
                        var stateName = GetCell(row, "state");

                        if (string.IsNullOrEmpty(customer))
                        {
                            AddGrouped("Empty CUSTOMER value", $"Row {row.RowNumber()}");
                            continue;
                        }

                        // ── Strip the letter prefix to get numeric part ─────
                        // DB stores DealerCode as pure numeric, e.g. "41089157"
                        var numericCode = Regex.Replace(customer, @"[^0-9]", "");
                        if (string.IsNullOrEmpty(numericCode))
                        {
                            AddGrouped("No numeric digits in CUSTOMER",
                                $"Row {row.RowNumber()}: '{customer}'");
                            continue;
                        }

                        // ── Determine prefix ───────────────────────────────
                        var prefix = char.IsLetter(customer[0])
                            ? char.ToUpperInvariant(customer[0])
                            : ' ';

                        // ── Prefix → company mapping ───────────────────────
                        // D         → SPIC   → SPICCode field
                        // Z, N, T   → GreenStar → GreenStarCode field
                        // (no prefix / unknown) → treat as SPIC
                        bool isSpic = prefix == 'D' || prefix == ' ';
                        bool isGreenStar = prefix == 'Z' || prefix == 'N' || prefix == 'T';

                        if (!isSpic && !isGreenStar)
                        {
                            AddGrouped($"Unknown prefix '{prefix}', skipped",
                                $"Row {row.RowNumber()}: '{customer}' — {customerName}");
                            continue;
                        }

                        // ── Resolve state ──────────────────────────────────
                        int stateId = 0;
                        if (!string.IsNullOrEmpty(stateName) &&
                            !stateNameToId.TryGetValue(stateName, out stateId))
                        {
                            AddGrouped("State not found in database",
                                $"Row {row.RowNumber()}: '{stateName}' (dealer: {customerName})");
                        }

                        // ── Find existing dealer ───────────────────────────
                        // Try by numeric code (matches DealerCode, SPICCode, GreenStarCode, TnCode)
                        var existingDealer =
                            dealerByNumeric.TryGetValue(numericCode, out var dbDealer) ? dbDealer :
                            batchNew.TryGetValue(numericCode, out var batchDealer) ? batchDealer :
                            null;

                        if (existingDealer != null)
                        {
                            // ── UPDATE: add the new company code to existing record ──
                            bool changed = false;

							// Each prefix routes to its own dedicated code field.
							if (prefix == 'D' || prefix == ' ')
							{
								if (string.IsNullOrEmpty(existingDealer.SPICCode))
								{
									existingDealer.SPICCode = customer;
									existingDealer.InSpic = true;
									changed = true;
								}
								else
									AddGrouped("SPICCode already set, skipped", $"{numericCode} — {customerName}");
							}
							else if (prefix == 'Z')
							{
								if (string.IsNullOrEmpty(existingDealer.GreenStarCode))
								{
									existingDealer.GreenStarCode = customer;
									existingDealer.InGreenStar = true;
									changed = true;
								}
								else
									AddGrouped("GreenStarCode already set, skipped", $"{numericCode} — {customerName}");
							}
							else if (prefix == 'T')
							{
								if (string.IsNullOrEmpty(existingDealer.TnCode))
								{
									existingDealer.TnCode = customer;
									existingDealer.InGreenStar = true;
									changed = true;
								}
								else
									AddGrouped("TnCode already set, skipped", $"{numericCode} — {customerName}");
							}
							else if (prefix == 'N')
							{
								if (string.IsNullOrEmpty(existingDealer.NCode))
								{
									existingDealer.NCode = customer;
									existingDealer.InGreenStar = true;
									changed = true;
								}
								else
									AddGrouped("NCode already set, skipped", $"{numericCode} — {customerName}");
							}

							if (changed)
                            {
                                existingDealer.UpdatedAt = now;
                                existingDealer.UpdatedBy = userId;

                                // Fill state if the existing record has none
                                if (existingDealer.StateId == 0 && stateId > 0)
                                {
                                    existingDealer.StateId = stateId;
                                    existingDealer.DealerStateId = stateId;
                                }

                                // Fill firm name if blank
                                if (string.IsNullOrWhiteSpace(existingDealer.FirmName) &&
                                    !string.IsNullOrWhiteSpace(customerName))
                                {
                                    existingDealer.FirmName = customerName.ToUpperInvariant();
                                }

                                updated++;
                            }
                        }
                        else
                        {
                            // ── INSERT: create new dealer ──────────────────
                            var dealer = new DealerRegistration
                            {
                                // DealerCode always = numeric part only (no prefix)
                                DealerCode = numericCode,

								// Each prefix routes to its own dedicated code field
								SPICCode = (prefix == 'D' || prefix == ' ') ? customer : null,
								GreenStarCode = prefix == 'Z' ? customer : null,
								TnCode = prefix == 'T' ? customer : null,
								NCode = prefix == 'N' ? customer : null,

								FirmName = customerName?.ToUpperInvariant() ?? string.Empty,
                                StateId = stateId,
                                DealerStateId = stateId,
                                InSpic = isSpic,
                                InGreenStar = isGreenStar,
                                IsDealer = true,
                                Status = DealerStatus.Active,
                                CreatedAt = now,
                                UpdatedAt = now,
                                CreatedBy = userId,
                                UpdatedBy = userId,

                                // Required non-null string fields
                                UserTableId = string.Empty,
                                ShopNoORRoomNoOrBlockNo = string.Empty,
                                Village = string.Empty,
                                PinCode = string.Empty,
                                OfficialContactNumber = string.Empty,
                                WhatsAppNumber = string.Empty,
                                AccountHolderName = string.Empty,
                                AccountNumber = string.Empty,
                                Branch = string.Empty,
                                IFSC = string.Empty,
                            };

                            _db.DealerRegistrations.Add(dealer);

                            // Track in batchNew so later rows in the same file can find this dealer
                            batchNew[numericCode] = dealer;

                            // Also index by the full code so duplicate rows are caught
                            if (isSpic)
                                batchNew[customer] = dealer;
                            else
                                batchNew[customer] = dealer;

                            inserted++;
                        }
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogWarning(exRow, "Row {Row} parse error", row.RowNumber());
                        AddGrouped("Parse errors", $"Row {row.RowNumber()}: {exRow.Message}");
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Dealer bulk import failed");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Import failed",
                    Error = ex.Message
                });
            }

            var totalSkipped = groupedErrors.Values.Sum(v => v.Count);
            return Ok(new
            {
                Success = true,
                Message = $"Import completed. {inserted} dealer(s) inserted, " +
                               $"{updated} updated, {totalSkipped} skipped.",
                Inserted = inserted,
                Updated = updated,
                GroupedErrors = groupedErrors,
                TotalSkipped = totalSkipped
            });
        }
		// POST /api/dealerbulkupload/manual-entry
		// Single-dealer version of Import: same prefix routing (D→SPICCode, Z→GreenStarCode,
		// T→TnCode, N→NCode), same "merge into existing dealer by numeric code" behaviour.
		// Only saves the dealer record — does NOT run the full registration wizard flow.
		public class ManualDealerRequest
		{
			public string Customer { get; set; } = string.Empty;      // e.g. "D41089157"
			public string? CustomerName { get; set; }
			public int StateId { get; set; }                          // 0 = not provided
		}

		[HttpPost("manual-entry")]
		public async Task<IActionResult> ManualEntry([FromBody] ManualDealerRequest req)
		{
			if (req == null || string.IsNullOrWhiteSpace(req.Customer))
				return BadRequest(new { Success = false, Message = "CUSTOMER code is required" });

			var customer = req.Customer.Trim();
			var customerName = req.CustomerName?.Trim() ?? string.Empty;

			var numericCode = Regex.Replace(customer, @"[^0-9]", "");
			if (string.IsNullOrEmpty(numericCode))
				return BadRequest(new { Success = false, Message = $"No numeric digits in CUSTOMER '{customer}'" });

			var prefix = char.IsLetter(customer[0])
				? char.ToUpperInvariant(customer[0])
				: ' ';

			bool isSpic = prefix == 'D' || prefix == ' ';
			bool isGreenStar = prefix == 'Z' || prefix == 'N' || prefix == 'T';

			if (!isSpic && !isGreenStar)
				return BadRequest(new { Success = false, Message = $"Unknown prefix '{prefix}' in CUSTOMER '{customer}'. Allowed: D, Z, T, N." });

			int stateId = req.StateId;

			var now = DateTime.UtcNow;
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

			using var tx = await _db.Database.BeginTransactionAsync();
			try
			{
				// Find an existing dealer by the numeric part across every code field.
				var existingDealer = await _db.DealerRegistrations
					.FirstOrDefaultAsync(d =>
						d.SPICCode == numericCode || d.SPICCode == customer ||
						d.GreenStarCode == numericCode || d.GreenStarCode == customer ||
						d.TnCode == numericCode || d.TnCode == customer ||
						d.NCode == numericCode || d.NCode == customer ||
						d.DealerCode == numericCode);

				bool inserted = false;

				if (existingDealer != null)
				{
					bool changed = false;

					if (prefix == 'D' || prefix == ' ')
					{
						if (string.IsNullOrEmpty(existingDealer.SPICCode))
						{ existingDealer.SPICCode = customer; existingDealer.InSpic = true; changed = true; }
						else
							return Conflict(new { Success = false, Message = $"SPICCode already set for dealer {numericCode}." });
					}
					else if (prefix == 'Z')
					{
						if (string.IsNullOrEmpty(existingDealer.GreenStarCode))
						{ existingDealer.GreenStarCode = customer; existingDealer.InGreenStar = true; changed = true; }
						else
							return Conflict(new { Success = false, Message = $"GreenStarCode already set for dealer {numericCode}." });
					}
					else if (prefix == 'T')
					{
						if (string.IsNullOrEmpty(existingDealer.TnCode))
						{ existingDealer.TnCode = customer; existingDealer.InGreenStar = true; changed = true; }
						else
							return Conflict(new { Success = false, Message = $"TnCode already set for dealer {numericCode}." });
					}
					else if (prefix == 'N')
					{
						if (string.IsNullOrEmpty(existingDealer.NCode))
						{ existingDealer.NCode = customer; existingDealer.InGreenStar = true; changed = true; }
						else
							return Conflict(new { Success = false, Message = $"NCode already set for dealer {numericCode}." });
					}

					if (changed)
					{
						existingDealer.UpdatedAt = now;
						existingDealer.UpdatedBy = userId;
						if (existingDealer.StateId == 0 && stateId > 0)
						{
							existingDealer.StateId = stateId;
							existingDealer.DealerStateId = stateId;
						}
						if (string.IsNullOrWhiteSpace(existingDealer.FirmName) && !string.IsNullOrWhiteSpace(customerName))
							existingDealer.FirmName = customerName.ToUpperInvariant();
					}
				}
				else
				{
					var dealer = new DealerRegistration
					{
						DealerCode = numericCode,
						SPICCode = (prefix == 'D' || prefix == ' ') ? customer : null,
						GreenStarCode = prefix == 'Z' ? customer : null,
						TnCode = prefix == 'T' ? customer : null,
						NCode = prefix == 'N' ? customer : null,
						FirmName = customerName?.ToUpperInvariant() ?? string.Empty,
						StateId = stateId,
						DealerStateId = stateId,
						InSpic = isSpic,
						InGreenStar = isGreenStar,
						IsDealer = true,
						Status = DealerStatus.Active,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = userId,
						UpdatedBy = userId,
						UserTableId = string.Empty,
						ShopNoORRoomNoOrBlockNo = string.Empty,
						Village = string.Empty,
						PinCode = string.Empty,
						OfficialContactNumber = string.Empty,
						WhatsAppNumber = string.Empty,
						AccountHolderName = string.Empty,
						AccountNumber = string.Empty,
						Branch = string.Empty,
						IFSC = string.Empty,
					};
					_db.DealerRegistrations.Add(dealer);
					inserted = true;
				}

				await _db.SaveChangesAsync();
				await tx.CommitAsync();

				return Ok(new
				{
					Success = true,
					Inserted = inserted,
					Updated = !inserted,
					Message = inserted
						? $"Dealer {customer} added successfully."
						: $"Dealer {numericCode} updated with {customer}."
				});
			}
			catch (Exception ex)
			{
				await tx.RollbackAsync();
				_logger.LogError(ex, "Manual dealer entry failed");
				return StatusCode(500, new { Success = false, Message = "Manual entry failed", Error = ex.Message });
			}
		}

		// GET /api/dealerbulkupload/sample-template
		[HttpGet("sample-template")]
		public IActionResult SampleTemplate()
		{
            var headers = new[] { "CUSTOMER", "CUSTOMER NAME", "State" };

            var sampleRows = new[]
            {
        new[] { "D41089157", "SRI KRISHNA AGENCIES", "Tamil Nadu" },   // D -> SPIC
        new[] { "Z50234781", "BHARATHI TRADERS",     "Tamil Nadu" },   // Z -> GreenStar
        new[] { "N60112233", "AMMAN FERTILIZERS",    "Tamil Nadu" },   // N -> GreenStar
        new[] { "T70998877", "VELAN AGRO CENTRE",    "Tamil Nadu" },   // T -> GreenStar + TnCode
    };

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Dealers");

            // Row 1: headers (controller parses row 1)
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#16a34a");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Row 2+: sample data
            for (int r = 0; r < sampleRows.Length; r++)
                for (int c = 0; c < sampleRows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = sampleRows[r][c];

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            // Instructions sheet — prefix rules travel with the file
            var notes = wb.Worksheets.Add("Instructions");
            var lines = new[]
            {
        "Dealer Import - Instructions",
        "",
        "Required columns (Row 1): CUSTOMER, CUSTOMER NAME, State",
        "",
        "CUSTOMER prefix rules:",
        "  D        -> SPIC      (stored in SPICCode)",
        "  Z, N, T  -> GreenStar (stored in GreenStarCode; T also fills TnCode)",
        "  no prefix -> treated as SPIC",
        "",
        "The numeric part is used as DealerCode. A dealer that already exists",
        "(matched by numeric code) gets the new company code added to it.",
        "",
        "State must already exist in master data, otherwise it is left blank.",
    };
            for (int i = 0; i < lines.Length; i++)
            {
                var cell = notes.Cell(i + 1, 1);
                cell.Value = lines[i];
                if (i == 0) { cell.Style.Font.Bold = true; cell.Style.Font.FontSize = 13; }
            }
            notes.Column(1).Width = 80;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Dealer_Import_Sample_Template.xlsx");
        }
    }
}