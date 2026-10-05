using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.MasterData;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AgricultureBulkUploadController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AgricultureBulkUploadController> _logger;

        public AgricultureBulkUploadController(AppDbContext db, ILogger<AgricultureBulkUploadController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // POST /api/agriculturebulkupload/bulk-upload?type=Crop
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload([FromQuery] string type, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest("Only Excel files (.xlsx/.xls) are supported");

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            // header map
            var headerRow = worksheet.Row(1);
            var lastHeaderCell = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (lastHeaderCell == 0)
                return BadRequest("Empty worksheet or missing header row");

            // Build header map with normalized column names
            var rawHeaders = new List<string>();
            var headerMap = new Dictionary<string, int>();
            for (int c = 1; c <= lastHeaderCell; c++)
            {
                var raw = headerRow.Cell(c).GetString().Trim();
                rawHeaders.Add(raw);
                var n = NormalizeHeader(raw);
                if (!string.IsNullOrEmpty(n) && !headerMap.ContainsKey(n))
                    headerMap[n] = c;
            }

            // Add alias entries so common variations (e.g. "Group" -> "productgroup") resolve correctly
            AddAliasEntries(headerMap);

            _logger.LogInformation("Detected Columns: {Columns}", string.Join(" | ", rawHeaders));

            string[] expected = type?.ToLowerInvariant() switch
            {
                "crop" => new[] { "name", "isactive" },
                "competitor" => new[] { "name", "isactive" },
                "sector" => new[] { "name", "isactive" },
                "unit" => new[] { "name", "unitcode", "isactive" },
                "category" => new[] { "name", "unitid", "isspecialityproduct", "isactive" },
                "productgroup" => new[] { "name", "isactive" },
                "product" => new[] { "category", "productname", "productgroup", "rpu" },
                _ => Array.Empty<string>()
            };

            if (expected.Length == 0)
                return BadRequest("Unknown type. Use Crop, Competitor, Sector, Unit, Category, Product");

            var missing = expected.Where(h => !headerMap.ContainsKey(h)).ToList();
            if (missing.Any())
            {
                var display = string.Join(", ", missing.Select(PrettyHeader));
                return BadRequest($"Invalid template. Missing columns: {display}");
            }

            var rows = worksheet.RowsUsed().Skip(1).ToList();
            var now = DateTime.UtcNow;
            var rejectedRecords = new List<RejectedRecord>();
            var totalRecords = 0;
            // This uploader no longer creates anything, so there is no insert count to
            // report; the field stays on the response for existing callers.
            var insertedCount = 0;
            var updatedCount = 0;

            var t = type?.ToLowerInvariant() ?? "";

            // ─────────────────────────────────────────────────────────────────────────
            // MASTER LOOKUPS + VALIDATION PASS (read-only, before the transaction).
            //
            // Nothing in this uploader creates a master. Every row must already exist in
            // master maintenance; this upload only updates the existing record. A row naming
            // something that does not exist is reported as a missing master and the whole file
            // is refused, so a typo can never quietly invent a new Crop, Unit or Category.
            //
            // Every name is compared through MasterNormalizer, and every lookup is built with
            // GroupBy(...).First() so duplicate rows already in the master tables cannot throw.
            // Category is scoped by its Unit, so the same category name under two units stays two
            // different masters.
            // ─────────────────────────────────────────────────────────────────────────
            var unitIds = (await _db.Units.AsNoTracking().Select(u => u.Id).ToListAsync()).ToHashSet();

            var cropByName = KeyByName(await _db.Crops.ToListAsync(), c => c.Name);
            var competitorByName = KeyByName(await _db.Competitors.ToListAsync(), c => c.Name);
            var sectorByName = KeyByName(await _db.Sectors.ToListAsync(), c => c.Name);
            var unitByName = KeyByName(await _db.Units.ToListAsync(), c => c.Name);
            var productGroupEntities = KeyByName(await _db.ProductGroups.ToListAsync(), c => c.Name);

            // Category carries its Unit, so it is keyed by (name, unitId) rather than name alone.
            var categoryByScopedName = (await _db.Categories.ToListAsync())
                .GroupBy(c => MasterNormalizer.Scoped(c.Name, c.UnitId))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            var productGroupByName = productGroupEntities.ToDictionary(kv => kv.Key, kv => kv.Value.Id);
            var categoryByName = categoryByScopedName.Values
                .GroupBy(c => MasterNormalizer.Normalize(c.Name))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            // Existing products, keyed canonically. GroupBy(...).First() keeps a pre-existing
            // duplicate from throwing; the row loop only needs one representative per name.
            var productEntities = (await _db.Products.ToListAsync())
                .GroupBy(p => MasterNormalizer.Normalize(p.Name))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());
            var existingProductIds = productEntities.ToDictionary(kv => kv.Key, kv => kv.Value.Id);

            var missingMasters = new MissingMasterCollector();

            foreach (var row in rows)
            {
                switch (t)
                {
                    case "crop":
                        {
                            var name = GetCellString(row, headerMap, "name");
                            if (!MasterNormalizer.IsBlank(name) && !cropByName.ContainsKey(MasterNormalizer.Normalize(name)))
                                missingMasters.Add("Crop", name, row.RowNumber());
                            break;
                        }
                    case "competitor":
                        {
                            var name = GetCellString(row, headerMap, "name");
                            if (!MasterNormalizer.IsBlank(name) && !competitorByName.ContainsKey(MasterNormalizer.Normalize(name)))
                                missingMasters.Add("Competitor", name, row.RowNumber());
                            break;
                        }
                    case "sector":
                        {
                            var name = GetCellString(row, headerMap, "name");
                            if (!MasterNormalizer.IsBlank(name) && !sectorByName.ContainsKey(MasterNormalizer.Normalize(name)))
                                missingMasters.Add("Sector", name, row.RowNumber());
                            break;
                        }
                    case "unit":
                        {
                            var name = GetCellString(row, headerMap, "name");
                            if (!MasterNormalizer.IsBlank(name) && !unitByName.ContainsKey(MasterNormalizer.Normalize(name)))
                                missingMasters.Add("Unit", name, row.RowNumber());
                            break;
                        }
                    case "category":
                        {
                            var name = GetCellString(row, headerMap, "name");
                            if (MasterNormalizer.IsBlank(name))
                                break;

                            // Both halves of the Category's identity are reported: the category
                            // itself and the Unit it hangs off.
                            if (!TryParseIntFromRow(row, headerMap, "unitid", out var unitId))
                            {
                                missingMasters.Add("Unit", "<blank or invalid UnitId>", row.RowNumber());
                                break;
                            }

                            if (!unitIds.Contains(unitId))
                                missingMasters.Add("Unit", unitId.ToString(System.Globalization.CultureInfo.InvariantCulture), row.RowNumber());

                            if (!categoryByScopedName.ContainsKey(MasterNormalizer.Scoped(name, unitId)))
                                missingMasters.Add("Category", name, row.RowNumber());
                            break;
                        }
                    case "productgroup":
                        {
                            var name = GetCellString(row, headerMap, "name");
                            if (!MasterNormalizer.IsBlank(name) && !productGroupByName.ContainsKey(MasterNormalizer.Normalize(name)))
                                missingMasters.Add("Product Group", name, row.RowNumber());
                            break;
                        }
                    case "product":
                        {
                            var productName = GetCellString(row, headerMap, "productname");
                            if (!MasterNormalizer.IsBlank(productName) && !existingProductIds.ContainsKey(MasterNormalizer.Normalize(productName)))
                                missingMasters.Add("Product", productName, row.RowNumber());

                            var catName = GetCellString(row, headerMap, "category");
                            if (!MasterNormalizer.IsBlank(catName) &&
                                !categoryByName.ContainsKey(MasterNormalizer.Normalize(catName)))
                                missingMasters.Add("Category", catName, row.RowNumber());

                            var pgName = GetCellString(row, headerMap, "productgroup");
                            if (!MasterNormalizer.IsBlank(pgName) &&
                                !productGroupByName.ContainsKey(MasterNormalizer.Normalize(pgName)))
                                missingMasters.Add("Product Group", pgName, row.RowNumber());
                            break;
                        }
                }
            }

            if (missingMasters.HasAny)
            {
                var (message, missingLines) = missingMasters.Build("agriculture master");

                _logger.LogWarning(
                    "Agriculture bulk upload rejected: {Count} missing master record(s): {Masters}",
                    missingMasters.Entries.Count,
                    string.Join(", ", missingMasters.Entries.Select(m => $"{m.Kind}='{m.Value}'")));

                return BadRequest(new
                {
                    Success = false,
                    Message = message,
                    MissingMasterLines = missingLines,
                    MissingMasters = missingMasters.Entries
                        .Select(m => new { m.Kind, m.Value, RowNumbers = m.RowNumbers })
                        .ToList()
                });
            }

            // In-batch duplicate tracking, so the same row twice in one file is reported
            // rather than applied twice.
            var batchNames = new HashSet<string>(StringComparer.Ordinal);

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    totalRecords++;
                    try
                    {
                        switch (type?.ToLowerInvariant())
                        {
                            case "crop":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var key = MasterNormalizer.Normalize(name);
                                    if (!batchNames.Add(key)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Duplicate Crop in this file")); break; }
                                    if (!cropByName.TryGetValue(key, out var ent)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Crop does not exist in master data")); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                    updatedCount++;
                                }
                                break;
                            case "competitor":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var key = MasterNormalizer.Normalize(name);
                                    if (!batchNames.Add(key)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Duplicate Competitor in this file")); break; }
                                    if (!competitorByName.TryGetValue(key, out var ent)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Competitor does not exist in master data")); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                    updatedCount++;
                                }
                                break;
                            case "sector":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var key = MasterNormalizer.Normalize(name);
                                    if (!batchNames.Add(key)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Duplicate Sector in this file")); break; }
                                    if (!sectorByName.TryGetValue(key, out var ent)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Sector does not exist in master data")); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                    updatedCount++;
                                }
                                break;
                            case "unit":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var key = MasterNormalizer.Normalize(name);
                                    if (!batchNames.Add(key)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Duplicate Unit in this file")); break; }
                                    if (!unitByName.TryGetValue(key, out var ent)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Unit does not exist in master data")); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    // UnitCode is accepted by the template for readability but the
                                    // Unit entity has no such column, so there is nothing to write.
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                    updatedCount++;
                                }
                                break;
                            case "category":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    if (!TryParseIntFromRow(row, headerMap, "unitid", out var unitId)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "UnitId invalid or missing")); break; }
                                    // Category is scoped by its Unit: the same name under two units is
                                    // two different masters, so the lookup key carries the unit id.
                                    var catKey = MasterNormalizer.Scoped(name, unitId);
                                    if (!batchNames.Add(catKey)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Duplicate Category for this Unit in this file")); break; }
                                    if (!categoryByScopedName.TryGetValue(catKey, out var ent)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Category does not exist in master data for this Unit")); break; }
                                    ent.IsSpecialityProduct = ParseBoolCellByValue(GetCellString(row, headerMap, "isspecialityproduct"));
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                    updatedCount++;
                                }
                                break;
                            case "productgroup":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var key = MasterNormalizer.Normalize(name);
                                    if (!batchNames.Add(key)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Duplicate Product Group in this file")); break; }
                                    if (!productGroupEntities.TryGetValue(key, out var ent)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Product Group does not exist in master data")); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                    updatedCount++;
                                }
                                break;
                            case "product":
                                {
                                    var productName = GetCellString(row, headerMap, "productname");
                                    if (string.IsNullOrEmpty(productName)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), "", "Product Name is required")); break; }

                                    var catName = GetCellString(row, headerMap, "category");
                                    if (string.IsNullOrEmpty(catName)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Category is required")); break; }

                                    if (!categoryByName.TryGetValue(MasterNormalizer.Normalize(catName), out var category))
                                    { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Category not found in master data")); break; }
                                    var catId = category.Id;

                                    var pgName = GetCellString(row, headerMap, "productgroup");
                                    if (string.IsNullOrEmpty(pgName)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Product Group is required")); break; }

                                    if (!productGroupByName.TryGetValue(MasterNormalizer.Normalize(pgName), out var pgId))
                                    { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Product Group not found in master data")); break; }

                                    var rpuStr = GetCellString(row, headerMap, "rpu");
                                    if (string.IsNullOrEmpty(rpuStr) || !decimal.TryParse(rpuStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rpuVal))
                                    {
                                        rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Invalid RPU"));
                                        break;
                                    }

                                    var productKey = MasterNormalizer.Normalize(productName);
                                    if (!batchNames.Add(productKey)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Duplicate Product in this file")); break; }
                                    if (!productEntities.TryGetValue(productKey, out var existing))
                                    { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Product does not exist in master data")); break; }

                                    existing.CategoryId = catId;
                                    existing.ProductGroupId = pgId;
                                    existing.RPU = rpuVal;
                                    existing.UpdatedAt = now;
                                    existing.UpdatedBy = "bulk-upload";
                                    updatedCount++;
                                }
                                break;
                            default:
                                return BadRequest("Unknown type");
                        }
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogWarning(exRow, "Row parse error");
                        rejectedRecords.Add(new RejectedRecord(row.RowNumber(), "", exRow.Message));
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Bulk upload failed");
                return StatusCode(500, "Bulk upload failed: " + ex.Message);
            }

            var response = new BulkUploadResponse
            {
                TotalRecords = totalRecords,
                InsertedCount = insertedCount,
                UpdatedCount = updatedCount,
                RejectedCount = rejectedRecords.Count,
                RejectedRecords = rejectedRecords
            };

            return Ok(response);
        }

        // GET /api/agriculturebulkupload/sample-template?type=crop
        [HttpGet("sample-template")]
        public IActionResult SampleTemplate([FromQuery] string type)
        {
            var t = type?.ToLowerInvariant() ?? "";

            (string Header, string Sample)[] columns = t switch
            {
                "crop" => new[]
                {
            ("Name", "Paddy"),
            ("IsActive", "TRUE")
        },
                "competitor" => new[]
                {
            ("Name", "ABC Fertilizers"),
            ("IsActive", "TRUE")
        },
                "sector" => new[]
                {
            ("Name", "Agriculture"),
            ("IsActive", "TRUE")
        },
                "unit" => new[]
                {
            ("Name", "Kilogram"),
            ("UnitCode", "KG"),
            ("IsActive", "TRUE")
        },
                "category" => new[]
                {
            ("Name", "Fertilizer"),
            ("UnitId", "1"),
            ("IsSpecialityProduct", "FALSE"),
            ("IsActive", "TRUE")
        },
                "productgroup" => new[]
                {
            ("Name", "Urea Group"),
            ("IsActive", "TRUE")
        },
                "product" => new[]
                {
            ("Category", "Fertilizer"),
            ("ProductName", "Urea 50kg"),
            ("ProductGroup", "Urea Group"),
            ("RPU", "266.50")
        },
                _ => Array.Empty<(string, string)>()
            };

            if (columns.Length == 0)
                return BadRequest("Unknown type. Use Crop, Competitor, Sector, Unit, Category, ProductGroup, Product");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Template");

            for (int i = 0; i < columns.Length; i++)
            {
                var headerCell = ws.Cell(1, i + 1);
                headerCell.Value = columns[i].Header;
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(2, i + 1).Value = columns[i].Sample;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            var fileName = $"{type}_Sample_Template.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // helpers (same approach as location upload)
        private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string key)
        {
            if (!headerMap.TryGetValue(key, out var col)) return string.Empty;
            return row.Cell(col).GetString().Trim();
        }

        private static bool TryParseIntFromRow(IXLRow row, Dictionary<string, int> headerMap, string key, out int value)
        {
            value = 0;
            var s = GetCellString(row, headerMap, key);
            if (string.IsNullOrEmpty(s)) return false;
            if (int.TryParse(s, out var v)) { value = v; return true; }
            if (double.TryParse(s, out var d)) { value = (int)d; return true; }
            return false;
        }

        private static bool ParseBoolCellByValue(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            s = s.Trim().ToLowerInvariant();
            return s == "1" || s == "true" || s == "yes";
        }

        /// <summary>
        /// Keys master rows by their canonical name. GroupBy(...).First() means a table that
        /// already contains duplicate names resolves to one representative instead of throwing
        /// from ToDictionary, which used to turn a stale duplicate into a 500 mid-upload.
        /// </summary>
        private static Dictionary<string, T> KeyByName<T>(IEnumerable<T> rows, Func<T, string?> name)
        {
            return rows
                .GroupBy(r => MasterNormalizer.Normalize(name(r)))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());
        }

        private static string NormalizeHeader(string h) => MasterNormalizer.NormalizeHeader(h);

        private static void AddAliasEntries(Dictionary<string, int> headerMap)
        {
            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "name", "productname" },
                { "product", "productname" },
                { "group", "productgroup" },
                { "rateperunit", "rpu" },
                { "rate", "rpu" },
            };

            foreach (var kvp in headerMap.ToList())
            {
                if (aliases.TryGetValue(kvp.Key, out var alias) && !headerMap.ContainsKey(alias))
                    headerMap[alias] = kvp.Value;
            }
        }
        private static string PrettyHeader(string h) => h;
    }

    public class RejectedRecord
    {
        public int RowNumber { get; set; }
        public string ProductName { get; set; } = "";
        public string Reason { get; set; } = "";

        public RejectedRecord() { }

        public RejectedRecord(int rowNumber, string productName, string reason)
        {
            RowNumber = rowNumber;
            ProductName = productName;
            Reason = reason;
        }
    }

    public class BulkUploadResponse
    {
        public int TotalRecords { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int RejectedCount { get; set; }
        public List<RejectedRecord> RejectedRecords { get; set; } = new();
        public List<object> DuplicateRecords { get; set; } = new();
    }
}
