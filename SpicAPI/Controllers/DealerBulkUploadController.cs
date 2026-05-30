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

        // POST /api/dealerbulkupload/import
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

            // Pre-load states: stateName → stateId
            var stateNameToId = _db.States
                .Select(s => new { s.StateName, s.Id })
                .AsEnumerable()
                .ToDictionary(s => s.StateName.Trim(), s => s.Id, StringComparer.OrdinalIgnoreCase);

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            // Build header map (normalized → column index)
            var headerRow = ws.Row(1);
            var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (lastCol == 0)
                return BadRequest(new { Success = false, Message = "Empty worksheet or missing header row" });

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var h = headerRow.Cell(c).GetString().Trim().Replace(" ", "").ToLowerInvariant();
                if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h))
                    headerMap[h] = c;
            }

            string GetCell(IXLRow row, string key)
            {
                return headerMap.TryGetValue(key, out var col) ? row.Cell(col).GetString().Trim() : string.Empty;
            }

            // Required: CUSTOMER and CUSTOMERNAME
            if (!headerMap.ContainsKey("customer"))
                return BadRequest(new { Success = false, Message = "Missing required column: CUSTOMER" });
            if (!headerMap.ContainsKey("customername"))
                return BadRequest(new { Success = false, Message = "Missing required column: CUSTOMER NAME" });

            var rows = ws.RowsUsed().Skip(1).ToList();

            // ── Pass 1: collect all numeric codes from the file ──
            var allNumericCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var customer = GetCell(row, "customer");
                if (string.IsNullOrEmpty(customer)) continue;
                var code = Regex.Replace(customer, @"[^0-9]", "");
                if (!string.IsNullOrEmpty(code)) allNumericCodes.Add(code);
            }

            // ── Pass 2: load ONLY the dealers whose codes appear in the file ──
            //    This avoids a full-table scan while still returning tracked EF entities for updates.
            var matchingDealers = await _db.DealerRegistrations
                .Where(d =>
                    (d.SPICCode != null && allNumericCodes.Contains(d.SPICCode)) ||
                    (d.GreenStarCode != null && allNumericCodes.Contains(d.GreenStarCode)) ||
                    (d.TnCode != null && allNumericCodes.Contains(d.TnCode)))
                .ToListAsync();

            var dealerByNumeric = new Dictionary<string, DealerRegistration>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in matchingDealers)
            {
                if (!string.IsNullOrEmpty(d.SPICCode)) dealerByNumeric.TryAdd(d.SPICCode, d);
                if (!string.IsNullOrEmpty(d.GreenStarCode)) dealerByNumeric.TryAdd(d.GreenStarCode, d);
                if (!string.IsNullOrEmpty(d.TnCode)) dealerByNumeric.TryAdd(d.TnCode, d);
            }

            var groupedErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            void AddGrouped(string group, string item)
            {
                if (!groupedErrors.TryGetValue(group, out var list))
                    groupedErrors[group] = list = new List<string>();
                list.Add(item);
            }

            var now = DateTime.UtcNow;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            int inserted = 0;
            int updated = 0;

            // In-batch new-entity tracking: numericCode → pending DealerRegistration (not yet saved)
            var batchNew = new Dictionary<string, DealerRegistration>(StringComparer.OrdinalIgnoreCase);

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

                        // Extract numeric part only
                        var numericCode = Regex.Replace(customer, @"[^0-9]", "");
                        if (string.IsNullOrEmpty(numericCode))
                        {
                            AddGrouped("No numeric digits in CUSTOMER", $"Row {row.RowNumber()}: '{customer}'");
                            continue;
                        }

                        // Resolve state (warn but continue — state may already be on existing record)
                        int stateId = 0;
                        if (!string.IsNullOrEmpty(stateName) && !stateNameToId.TryGetValue(stateName, out stateId))
                            AddGrouped("State not found in database", $"Row {row.RowNumber()}: '{stateName}' (dealer: {customerName})");

                        var prefix = char.ToUpperInvariant(customer[0]);

                        // ── Find existing dealer by numeric code (DB or earlier in this batch) ──
                        var existingDealer = dealerByNumeric.TryGetValue(numericCode, out var dbDealer) ? dbDealer
                            : batchNew.TryGetValue(numericCode, out var batchDealer) ? batchDealer
                            : null;

                        if (existingDealer != null)
                        {
                            // UPDATE: add the new code field to the existing record
                            bool changed = false;
                            switch (prefix)
                            {
                                case 'Z':
                                    if (string.IsNullOrEmpty(existingDealer.SPICCode))
                                    { existingDealer.SPICCode = customer; existingDealer.InSpic = true; changed = true; }
                                    else { AddGrouped("SPICCode already set, skipped", $"{numericCode} — {customerName}"); }
                                    break;
                                case 'N':
                                    if (string.IsNullOrEmpty(existingDealer.GreenStarCode))
                                    { existingDealer.GreenStarCode = customer; existingDealer.InGreenStar = true; changed = true; }
                                    else { AddGrouped("GreenStarCode already set, skipped", $"{numericCode} — {customerName}"); }
                                    break;
                                case 'D':
                                    if (string.IsNullOrEmpty(existingDealer.TnCode))
                                    { existingDealer.TnCode = customer; changed = true; }
                                    else { AddGrouped("TnCode already set, skipped", $"{numericCode} — {customerName}"); }
                                    break;
                                default:
                                    AddGrouped($"Unknown prefix '{prefix}', skipped", $"Row {row.RowNumber()}: '{customer}' — {customerName}");
                                    break;
                            }
                            if (changed)
                            {
                                existingDealer.UpdatedAt = now;
                                existingDealer.UpdatedBy = userId;
                                // Fill state if missing on the existing record
                                if (existingDealer.StateId == 0 && stateId > 0)
                                { existingDealer.StateId = stateId; existingDealer.DealerStateId = stateId; }
                                updated++;
                            }
                        }
                        else
                        {
                            // INSERT: create new dealer record
                            var dealer = new DealerRegistration
                            {
                                FirmName = customerName,
                                StateId = stateId,
                                DealerStateId = stateId,
                                InSpic = prefix == 'Z',
                                InGreenStar = prefix == 'N',
                                IsDealer = true,
                                Status = DealerStatus.Active,
                                CreatedAt = now,
                                UpdatedAt = now,
                                UpdatedBy = userId,
                                CreatedBy = userId,
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

                            switch (prefix)
                            {
                                case 'Z':
                                    dealer.SPICCode = customer;
                                    dealer.DealerCode = numericCode;
                                    break;
                                case 'N':
                                    dealer.GreenStarCode = customer;
                                    dealer.DealerCode = numericCode;
                                    break;
                                case 'D':
                                    dealer.TnCode = customer;
                                    dealer.DealerCode = numericCode;
                                    break;
                                default:
                                    AddGrouped($"Unknown prefix '{prefix}', skipped", $"Row {row.RowNumber()}: '{customer}' — {customerName}");
                                    continue;
                            }

                            _db.DealerRegistrations.Add(dealer);
                            batchNew[numericCode] = dealer;   // track for cross-row upsert within same file
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
                return StatusCode(500, new { Success = false, Message = "Import failed", Error = ex.Message });
            }

            var totalSkipped = groupedErrors.Values.Sum(v => v.Count);
            return Ok(new
            {
                Success = true,
                Message = $"Import completed. {inserted} dealer(s) inserted, {updated} updated, {totalSkipped} skipped.",
                Inserted = inserted,
                Updated = updated,
                GroupedErrors = groupedErrors,
                TotalSkipped = totalSkipped
            });
        }
    }
}
