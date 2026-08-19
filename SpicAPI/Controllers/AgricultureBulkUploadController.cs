using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
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

            var rows = worksheet.RowsUsed().Skip(1);
            var now = DateTime.UtcNow;
            var rejectedRecords = new List<RejectedRecord>();
            var totalRecords = 0;
            var insertedCount = 0;
            var updatedCount = 0;

            var seenProductNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                                    var ent = new Crop { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Crops.Add(ent);
                                    insertedCount++;
                                }
                                break;
                            case "competitor":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var ent = new Competitor { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Competitors.Add(ent);
                                    insertedCount++;
                                }
                                break;
                            case "sector":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var ent = new Sector { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Sectors.Add(ent);
                                    insertedCount++;
                                }
                                break;
                            case "unit":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var ent = new Unit { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Units.Add(ent);
                                    insertedCount++;
                                }
                                break;
                            case "category":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    if (!TryParseIntFromRow(row, headerMap, "unitid", out var unitId)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "UnitId invalid or missing")); break; }
                                    var isSpec = ParseBoolCellByValue(GetCellString(row, headerMap, "isspecialityproduct"));
                                    var ent = new Category { Name = name, UnitId = unitId, IsSpecialityProduct = isSpec, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Categories.Add(ent);
                                    insertedCount++;
                                }
                                break;
                            case "productgroup":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), name, "Name is empty")); break; }
                                    var ent = new ProductGroup { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.ProductGroups.Add(ent);
                                    insertedCount++;
                                }
                                break;
                            case "product":
                                {
                                    var productName = GetCellString(row, headerMap, "productname");
                                    if (string.IsNullOrEmpty(productName)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), "", "Product Name is required")); break; }

                                    var catName = GetCellString(row, headerMap, "category");
                                    if (string.IsNullOrEmpty(catName)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Category is required")); break; }

                                    var category = _db.Categories.FirstOrDefault(c => c.Name.ToLower() == catName.ToLower());
                                    if (category is null) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Category not found in master data")); break; }
                                    var catId = category.Id;

                                    var pgName = GetCellString(row, headerMap, "productgroup");
                                    if (string.IsNullOrEmpty(pgName)) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Product Group is required")); break; }

                                    var pg = _db.ProductGroups.FirstOrDefault(p => p.Name.ToLower() == pgName.ToLower());
                                    if (pg is null) { rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Product Group not found in master data")); break; }
                                    var pgId = pg.Id;

                                    var rpuStr = GetCellString(row, headerMap, "rpu");
                                    if (string.IsNullOrEmpty(rpuStr) || !decimal.TryParse(rpuStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rpuVal))
                                    {
                                        rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Invalid RPU"));
                                        break;
                                    }

                                    var existing = _db.Products.FirstOrDefault(p => p.Name.ToLower() == productName.ToLower());
                                    if (existing != null)
                                    {
                                        var updated = false;
                                        if (existing.CategoryId == null) { existing.CategoryId = catId; updated = true; }
                                        if (existing.ProductGroupId == null) { existing.ProductGroupId = pgId; updated = true; }
                                        if (existing.RPU == null) { existing.RPU = rpuVal; updated = true; }

                                        if (updated)
                                        {
                                            existing.UpdatedAt = now;
                                            existing.UpdatedBy = "bulk-upload";
                                            updatedCount++;
                                        }
                                        else
                                        {
                                            rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Product already exists"));
                                        }
                                        break;
                                    }

                                    if (!seenProductNames.Add(productName))
                                    {
                                        rejectedRecords.Add(new RejectedRecord(row.RowNumber(), productName, "Duplicate Product"));
                                        break;
                                    }

                                    var ent = new Product
                                    {
                                        Name = productName,
                                        CategoryId = catId,
                                        ProductGroupId = pgId,
                                        RPU = rpuVal,
                                        IsActive = true,
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    };
                                    _db.Products.Add(ent);
                                    insertedCount++;
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

        private static string NormalizeHeader(string h) => (h ?? string.Empty).Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();

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
