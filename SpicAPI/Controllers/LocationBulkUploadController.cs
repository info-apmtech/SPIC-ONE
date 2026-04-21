using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationBulkUploadController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<LocationBulkUploadController> _logger;

        public LocationBulkUploadController(AppDbContext db, ILogger<LocationBulkUploadController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // POST /api/locationbulkupload/bulk-upload?type=Zone
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload([FromQuery] string type, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest("Only Excel files (.xlsx/.xls) are supported");

            var errors = new List<string>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            // Validate header row and build header map (normalized header -> column index)
            var headerRow = worksheet.Row(1);
            var lastHeaderCell = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (lastHeaderCell == 0)
                return BadRequest("Empty worksheet or missing header row");

            Dictionary<string, int> headerMap = new();
            for (int c = 1; c <= lastHeaderCell; c++)
            {
                var raw = headerRow.Cell(c).GetString();
                var n = NormalizeHeader(raw);
                if (!string.IsNullOrEmpty(n) && !headerMap.ContainsKey(n)) headerMap[n] = c;
            }

            // expected columns per type (normalized)
            string[] expected = type?.ToLowerInvariant() switch
            {
                "zone" => new[] { "zonename", "zonecode", "zonecolorcode", "isactive" },
                "state" => new[] { "statename", "zoneid", "isactive" },
                "district" => new[] { "districtname", "stateid", "isactive" },
                "subdistrict" => new[] { "subdistrictname", "districtid", "isactive" },
                "sub-district" => new[] { "subdistrictname", "districtid", "isactive" },
                "sub_district" => new[] { "subdistrictname", "districtid", "isactive" },
                "region" => new[] { "regionname", "stateid", "isactive" },
                "headquarter" => new[] { "headquartername", "regionid", "isactive" },
                "headquarters" => new[] { "headquartername", "regionid", "isactive" },
                _ => Array.Empty<string>()
            };

            if (expected.Length == 0)
                return BadRequest("Unknown type. Use Zone, State, District, SubDistrict, Region, Headquarter");

            // check required headers present
            var missing = expected.Where(h => !headerMap.ContainsKey(h)).ToList();
            if (missing.Any())
            {
                var display = string.Join(", ", missing.Select(h => PrettyHeader(h)));
                return BadRequest($"Invalid template. Missing columns: {display}");
            }

            var rows = worksheet.RowsUsed().Skip(1); // data rows
            var now = DateTime.UtcNow;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        switch (type?.ToLowerInvariant())
                        {
                            case "zone":
                                {
                                    var zoneName = GetCellString(row, headerMap, "zonename");
                                    if (string.IsNullOrEmpty(zoneName)) { errors.Add($"Row {row.RowNumber()}: Zone name empty"); break; }
                                    var zone = new Zone
                                    {
                                        ZoneName = zoneName,
                                        ZoneCode = GetCellString(row, headerMap, "zonecode"),
                                        ZoneColorCode = GetCellString(row, headerMap, "zonecolorcode"),
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    };
                                    _db.Zones.Add(zone);
                                }
                                break;

                            case "state":
                                {
                                    var stateName = GetCellString(row, headerMap, "statename");
                                    if (string.IsNullOrEmpty(stateName)) { errors.Add($"Row {row.RowNumber()}: State name empty"); break; }
                                    int? zoneRef = null;
                                    if (!TryParseIntFromRow(row, headerMap, "zoneid", out var zidParsed))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: ZoneId invalid or missing");
                                        break;
                                    }
                                    zoneRef = zidParsed;
                                    var state = new State
                                    {
                                        StateName = stateName,
                                        ZoneId = zoneRef.Value,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    };
                                    _db.States.Add(state);
                                }
                                break;

                            case "district":
                                {
                                    var districtName = GetCellString(row, headerMap, "districtname");
                                    if (string.IsNullOrEmpty(districtName)) { errors.Add($"Row {row.RowNumber()}: District name empty"); break; }
                                    if (!TryParseIntFromRow(row, headerMap, "stateid", out var stateId))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: StateId invalid or missing");
                                        break;
                                    }
                                    var district = new District
                                    {
                                        DistrictName = districtName,
                                        StateId = stateId,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    };
                                    _db.Districts.Add(district);
                                }
                                break;

                            case "subdistrict":
                            case "sub-district":
                            case "sub_district":
                                {
                                    var subName = GetCellString(row, headerMap, "subdistrictname");
                                    if (string.IsNullOrEmpty(subName)) { errors.Add($"Row {row.RowNumber()}: SubDistrict name empty"); break; }
                                    if (!TryParseIntFromRow(row, headerMap, "districtid", out var districtId))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: DistrictId invalid or missing");
                                        break;
                                    }
                                    var sub = new SubDistrict
                                    {
                                        SubDistrictName = subName,
                                        DistrictId = districtId,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    };
                                    _db.SubDistricts.Add(sub);
                                }
                                break;

                            case "region":
                                {
                                    var regionName = GetCellString(row, headerMap, "regionname");
                                    if (string.IsNullOrEmpty(regionName)) { errors.Add($"Row {row.RowNumber()}: Region name empty"); break; }
                                    if (!TryParseIntFromRow(row, headerMap, "stateid", out var stateId2))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: StateId invalid or missing");
                                        break;
                                    }
                                    var region = new Region
                                    {
                                        RegionName = regionName,
                                        StateId = stateId2,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    };
                                    _db.Regions.Add(region);
                                }
                                break;

                            case "headquarter":
                            case "headquarters":
                                {
                                    var hqName = GetCellString(row, headerMap, "headquartername");
                                    if (string.IsNullOrEmpty(hqName)) { errors.Add($"Row {row.RowNumber()}: Headquarter name empty"); break; }
                                    if (!TryParseIntFromRow(row, headerMap, "regionid", out var regionId2))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: RegionId invalid or missing");
                                        break;
                                    }
                                    var hq = new Headquarter
                                    {
                                        HeadquarterName = hqName,
                                        RegionId = regionId2,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    };
                                    _db.Headquarters.Add(hq);
                                }
                                break;

                            default:
                                return BadRequest("Unknown type. Use Zone, State, District, SubDistrict, Region, Headquarter");
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

        private static int? ParseIntCell(IXLCell cell)
        {
            if (cell == null) return null;
            if (int.TryParse(cell.GetString(), out var v)) return v;
            if (cell.TryGetValue(out double d)) return (int)d;
            return null;
        }

        private static bool ParseBoolCell(IXLCell cell)
        {
            if (cell == null) return true;
            var s = cell.GetString().Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(s)) return true; // default active
            if (s == "1" || s == "true" || s == "yes") return true;
            return false;
        }

        // Helpers for header-mapped access
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
            // try parse double
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

        private static string PrettyHeader(string h)
        {
            if (string.IsNullOrEmpty(h)) return h;
            // insert spaces before capital letters or numbers? simple mapping
            return h switch
            {
                "zonename" => "ZoneName",
                "zonecode" => "ZoneCode",
                "zonecolorcode" => "ZoneColorCode",
                "isactive" => "IsActive",
                "statename" => "StateName",
                "zoneid" => "ZoneId",
                "districtname" => "DistrictName",
                "stateid" => "StateId",
                "subdistrictname" => "SubDistrictName",
                "districtid" => "DistrictId",
                "regionname" => "RegionName",
                "regionid" => "RegionId",
                "headquartername" => "HeadquarterName",
                _ => h
            };
        }
    }
}