using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
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

            var headerMap = new Dictionary<string,int>();
            for (int c = 1; c <= lastHeaderCell; c++)
            {
                var raw = headerRow.Cell(c).GetString();
                var n = NormalizeHeader(raw);
                if (!string.IsNullOrEmpty(n) && !headerMap.ContainsKey(n)) headerMap[n] = c;
            }

            string[] expected = type?.ToLowerInvariant() switch
            {
                "crop" => new[] { "name", "isactive" },
                "competitor" => new[] { "name", "isactive" },
                "sector" => new[] { "name", "isactive" },
                "unit" => new[] { "name", "unitcode", "isactive" },
                "category" => new[] { "name", "unitid", "isspecialityproduct", "isactive" },
                "productgroup" => new[] { "name", "isactive" },
                "product" => new[] { "name", "category", "productgroup", "rpu", "isactive" },
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
            var errors = new List<string>();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        switch (type?.ToLowerInvariant())
                        {
                            case "crop":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { errors.Add($"Row {row.RowNumber()}: Name empty"); break; }
                                    var ent = new Crop { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Crops.Add(ent);
                                }
                                break;
                            case "competitor":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { errors.Add($"Row {row.RowNumber()}: Name empty"); break; }
                                    var ent = new Competitor { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Competitors.Add(ent);
                                }
                                break;
                            case "sector":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { errors.Add($"Row {row.RowNumber()}: Name empty"); break; }
                                    var ent = new Sector { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Sectors.Add(ent);
                                }
                                break;
                            case "unit":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { errors.Add($"Row {row.RowNumber()}: Name empty"); break; }
                                    var ent = new Unit { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Units.Add(ent);
                                }
                                break;
                            case "category":
                                {
                                    var name = GetCellString(row, headerMap, "name");
                                    if (string.IsNullOrEmpty(name)) { errors.Add($"Row {row.RowNumber()}: Name empty"); break; }
                                    if (!TryParseIntFromRow(row, headerMap, "unitid", out var unitId)) { errors.Add($"Row {row.RowNumber()}: UnitId invalid or missing"); break; }
                                    var isSpec = ParseBoolCellByValue(GetCellString(row, headerMap, "isspecialityproduct"));
                                    var ent = new Category { Name = name, UnitId = unitId, IsSpecialityProduct = isSpec, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
                                    _db.Categories.Add(ent);
                                }
                                break;
							case "productgroup":
								{
									var name = GetCellString(row, headerMap, "name");
									if (string.IsNullOrEmpty(name)) { errors.Add($"Row {row.RowNumber()}: Name empty"); break; }
									var ent = new ProductGroup { Name = name, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
									_db.ProductGroups.Add(ent);
								}
								break;
							case "product":
								{
									var name = GetCellString(row, headerMap, "name");
									if (string.IsNullOrEmpty(name)) { errors.Add($"Row {row.RowNumber()}: Name empty"); break; }
									var catName = GetCellString(row, headerMap, "category");
									if (string.IsNullOrEmpty(catName)) { errors.Add($"Row {row.RowNumber()}: Category empty"); break; }
									var category = _db.Categories.FirstOrDefault(c => c.Name.ToLower() == catName.ToLower());
									if (category is null) { errors.Add($"Row {row.RowNumber()}: Category '{catName}' not found"); break; }
									var catId = category.Id;
									var pgName = GetCellString(row, headerMap, "productgroup");
									int? pgId = null;
									if (!string.IsNullOrEmpty(pgName))
									{
										var pg = _db.ProductGroups.FirstOrDefault(p => p.Name.ToLower() == pgName.ToLower());
										if (pg is null)
											errors.Add($"Row {row.RowNumber()}: Product Group '{pgName}' not found");
										else
											pgId = pg.Id;
									}
									decimal? rpu = null;
									var rpuStr = GetCellString(row, headerMap, "rpu");
									if (!string.IsNullOrEmpty(rpuStr) && decimal.TryParse(rpuStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rpuVal))
										rpu = rpuVal;
									var ent = new Product { Name = name, CategoryId = catId, ProductGroupId = pgId, RPU = rpu, IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")), CreatedAt = now, UpdatedAt = now, UpdatedBy = "bulk-upload" };
									_db.Products.Add(ent);
								}
								break;
                            default:
                                return BadRequest("Unknown type");
                        }
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogWarning(exRow, "Row parse error");
                        errors.Add($"Row {row.RowNumber()} error: {exRow.Message}");
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

            return Ok(new { Success = true, Message = "Upload completed", Errors = errors });
        }

        // helpers (same approach as location upload)
        private static string GetCellString(IXLRow row, Dictionary<string,int> headerMap, string key)
        {
            if (!headerMap.TryGetValue(key, out var col)) return string.Empty;
            return row.Cell(col).GetString().Trim();
        }

        private static bool TryParseIntFromRow(IXLRow row, Dictionary<string,int> headerMap, string key, out int value)
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
        private static string PrettyHeader(string h) => h;
    }
}
