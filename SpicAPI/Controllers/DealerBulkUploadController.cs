using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace SpicAPI.Controllers
{
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
					(d.SPICCode      != null && allNumericCodes.Contains(d.SPICCode))      ||
					(d.GreenStarCode != null && allNumericCodes.Contains(d.GreenStarCode)) ||
					(d.TnCode        != null && allNumericCodes.Contains(d.TnCode))        ||
					(d.DealerCode    != null && allNumericCodes.Contains(d.DealerCode)))
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

							if (isSpic)
							{
								// D prefix → store full code (e.g. "D41089157") in SPICCode
								if (string.IsNullOrEmpty(existingDealer.SPICCode))
								{
									existingDealer.SPICCode = customer;
									existingDealer.InSpic   = true;
									changed = true;
								}
								else
								{
									AddGrouped("SPICCode already set, skipped",
										$"{numericCode} — {customerName}");
								}
							}
							else // isGreenStar: Z, N, T
							{
								// Z/N/T prefix → store full code in GreenStarCode
								if (string.IsNullOrEmpty(existingDealer.GreenStarCode))
								{
									existingDealer.GreenStarCode = customer;
									existingDealer.InGreenStar   = true;
									changed = true;
								}
								else
								{
									AddGrouped("GreenStarCode already set, skipped",
										$"{numericCode} — {customerName}");
								}

								// T prefix also populates TnCode
								if (prefix == 'T' && string.IsNullOrEmpty(existingDealer.TnCode))
								{
									existingDealer.TnCode = customer;
									changed = true;
								}
							}

							if (changed)
							{
								existingDealer.UpdatedAt = now;
								existingDealer.UpdatedBy = userId;

								// Fill state if the existing record has none
								if (existingDealer.StateId == 0 && stateId > 0)
								{
									existingDealer.StateId      = stateId;
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
								DealerCode  = numericCode,

								// SPICCode = full code for D prefix, null otherwise
								SPICCode    = isSpic ? customer : null,

								// GreenStarCode = full code for Z/N/T prefix, null otherwise
								GreenStarCode = isGreenStar ? customer : null,

								// TnCode = full code only for T prefix
								TnCode      = prefix == 'T' ? customer : null,

								FirmName    = customerName?.ToUpperInvariant() ?? string.Empty,
								StateId     = stateId,
								DealerStateId = stateId,
								InSpic      = isSpic,
								InGreenStar = isGreenStar,
								IsDealer    = true,
								Status      = DealerStatus.Active,
								CreatedAt   = now,
								UpdatedAt   = now,
								CreatedBy   = userId,
								UpdatedBy   = userId,

								// Required non-null string fields
								UserTableId             = string.Empty,
								ShopNoORRoomNoOrBlockNo = string.Empty,
								Village                 = string.Empty,
								PinCode                 = string.Empty,
								OfficialContactNumber   = string.Empty,
								WhatsAppNumber          = string.Empty,
								AccountHolderName       = string.Empty,
								AccountNumber           = string.Empty,
								Branch                  = string.Empty,
								IFSC                    = string.Empty,
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
	}
}