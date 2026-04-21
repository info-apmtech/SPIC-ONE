using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Linq;
using System.IO;

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
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

            var errors = new List<string>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            // Validate header row and build header map (normalized header -> column index)
            var headerRow = worksheet.Row(1);
            var lastHeaderCell = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (lastHeaderCell == 0)
                return BadRequest(new { Success = false, Message = "Empty worksheet or missing header row" });

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
                return BadRequest(new { Success = false, Message = "Unknown type. Use Zone, State, District, SubDistrict, Region, Headquarter" });

            // check required headers present
            // accept LGD/FMS prefixes and a few common variants
            List<string> missingList = new();
            string t = type?.ToLowerInvariant() ?? "";

            static bool HeaderExists(Dictionary<string, int> map, string key)
            {
                if (map.ContainsKey(key)) return true;
                if (map.ContainsKey("lgd" + key)) return true;
                if (map.ContainsKey("fms" + key)) return true;
                return false;
            }

            if (t == "state")
            {
                if (!HeaderExists(headerMap, "statename")) missingList.Add("StateName or FMSStateName");
                if (!HeaderExists(headerMap, "zoneid") && !HeaderExists(headerMap, "zonename")) missingList.Add("ZoneId or ZoneName");
            }
            else if (t == "district")
            {
                if (!HeaderExists(headerMap, "districtname")) missingList.Add("DistrictName");
                if (!HeaderExists(headerMap, "stateid") && !HeaderExists(headerMap, "statename")) missingList.Add("StateId or StateName or FMSStateName");
            }
            else if (t == "subdistrict" || t == "sub-district" || t == "sub_district")
            {
                if (!HeaderExists(headerMap, "subdistrictname")) missingList.Add("SubDistrictName");
                if (!HeaderExists(headerMap, "districtid") && !HeaderExists(headerMap, "districtname")) missingList.Add("DistrictId or DistrictName or FMSDistrictName");
            }
            else if (t == "region")
            {
                if (!HeaderExists(headerMap, "regionname")) missingList.Add("RegionName");
                if (!HeaderExists(headerMap, "stateid") && !HeaderExists(headerMap, "statename")) missingList.Add("StateId or StateName");
            }
            else if (t == "headquarter" || t == "headquarters")
            {
                if (!HeaderExists(headerMap, "headquartername")) missingList.Add("HeadquarterName");
                if (!HeaderExists(headerMap, "regionid") && !HeaderExists(headerMap, "regionname")) missingList.Add("RegionId or RegionName");
            }
            else
            {
                // default strict check
                var missing = expected.Where(h => !HeaderExists(headerMap, h)).ToList();
                missingList.AddRange(missing.Select(h => PrettyHeader(h)));
            }

            if (missingList.Any())
            {
                return BadRequest(new { Success = false, Message = "Invalid template. Missing columns", Missing = missingList });
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
                                    // skip duplicate zones
                                    if (_db.Zones.Any(z => z.ZoneName.ToLower() == zoneName.ToLower()))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: Zone '{zoneName}' already exists, skipped");
                                        break;
                                    }
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
                                    // state name may come from StateName or FMSStateName column
                                    var stateName = GetCellString(row, headerMap, "statename");
                                    if (string.IsNullOrEmpty(stateName)) stateName = GetCellString(row, headerMap, "fmsstatename");
                                    if (string.IsNullOrEmpty(stateName)) { errors.Add($"Row {row.RowNumber()}: State name empty"); break; }
                                    // skip duplicate state name
                                    if (_db.States.Any(s => s.StateName.ToLower() == stateName.ToLower()))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: State '{stateName}' already exists, skipped");
                                        break;
                                    }
                                    if (!TryResolveFkIdFromKeys(row, headerMap, new[] { "zoneid", "zonename" }, "zone", out var zidParsed))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: ZoneId or ZoneName invalid or missing");
                                        break;
                                    }
                                    var state = new State
                                    {
                                        StateName = stateName,
                                        ZoneId = zidParsed,
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
                                    if (!TryResolveFkIdFromKeys(row, headerMap, new[] { "stateid", "statename", "fmsstatename" , "lgdstateid"}, "state", out var stateId))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: StateId or StateName invalid or missing");
                                        break;
                                    }
                                    // skip duplicate district within same state
                                    if (_db.Districts.Any(d => d.DistrictName.ToLower() == districtName.ToLower() && d.StateId == stateId))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: District '{districtName}' for StateId {stateId} already exists, skipped");
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
                                    if (!TryResolveFkIdFromKeys(row, headerMap, new[] { "districtid", "districtname", "fmsdistrictname", "lgddistrictid" }, "district", out var districtId))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: DistrictId or DistrictName invalid or missing");
                                        break;
                                    }
                                    // skip duplicate subdistrict within same district
                                    if (_db.SubDistricts.Any(su => su.SubDistrictName.ToLower() == subName.ToLower() && su.DistrictId == districtId))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: SubDistrict '{subName}' for DistrictId {districtId} already exists, skipped");
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
                                    if (!TryResolveFkIdFromKeys(row, headerMap, new[] { "stateid", "statename", "fmsstatename", "lgdstateid" }, "state", out var stateId2))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: StateId or StateName invalid or missing");
                                        break;
                                    }
                                    // skip duplicate region within same state
                                    if (_db.Regions.Any(r => r.RegionName.ToLower() == regionName.ToLower() && r.StateId == stateId2))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: Region '{regionName}' for StateId {stateId2} already exists, skipped");
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
                                    if (!TryResolveFkIdFromKeys(row, headerMap, new[] { "regionid", "regionname" }, "region", out var regionId2))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: RegionId or RegionName invalid or missing");
                                        break;
                                    }
                                    // skip duplicate HQ within same region
                                    if (_db.Headquarters.Any(h => h.HeadquarterName.ToLower() == hqName.ToLower() && h.RegionId == regionId2))
                                    {
                                        errors.Add($"Row {row.RowNumber()}: Headquarter '{hqName}' for RegionId {regionId2} already exists, skipped");
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
                                return BadRequest(new { Success = false, Message = "Unknown type. Use Zone, State, District, SubDistrict, Region, Headquarter" });
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
                return StatusCode(500, new { Success = false, Message = "Bulk upload failed", Error = ex.Message });
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

        // Try resolve FK by checking multiple header keys (e.g., numeric id or name columns)
        private bool TryResolveFkIdFromKeys(IXLRow row, Dictionary<string, int> headerMap, string[] keys, string entity, out int id)
        {
            id = 0;
            foreach (var k in keys)
            {
                if (!headerMap.ContainsKey(k)) continue;
                var raw = GetCellString(row, headerMap, k);
                if (string.IsNullOrEmpty(raw)) continue;
                if (int.TryParse(raw, out var parsed)) { id = parsed; return true; }
                // try name lookup using existing helper by temporarily mapping value
                var name = raw.Trim();
                switch (entity.ToLowerInvariant())
                {
                    case "zone":
                        var z = _db.Zones.FirstOrDefault(x => x.ZoneName.ToLower() == name.ToLower());
                        if (z != null) { id = z.Id; return true; }
                        break;
                    case "state":
                        var s = _db.States.FirstOrDefault(x => x.StateName.ToLower() == name.ToLower());
                        if (s != null) { id = s.Id; return true; }
                        break;
                        case "district":
                        // If the sheet also provides a state name (FMSStateName or StateName) use it to narrow the district search
                        string stateName = string.Empty;
                        if (headerMap.ContainsKey("fmsstatename")) stateName = GetCellString(row, headerMap, "fmsstatename");
                        if (string.IsNullOrEmpty(stateName) && headerMap.ContainsKey("statename")) stateName = GetCellString(row, headerMap, "statename");
                        var nameLower = name.ToLower();
                        if (!string.IsNullOrEmpty(stateName))
                        {
                            var state = _db.States.FirstOrDefault(x => x.StateName.ToLower() == stateName.ToLower());
                            if (state != null)
                            {
                                // try exact match within state
                                var d2 = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower() == nameLower && x.StateId == state.Id);
                                if (d2 != null) { id = d2.Id; return true; }
                                // try fuzzy contains within state (helps when FMS name is shorter)
                                var d3 = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower().Contains(nameLower) && x.StateId == state.Id);
                                if (d3 != null) { id = d3.Id; return true; }
                            }
                        }
                        // fallback to global exact match
                        var d = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower() == nameLower);
                        if (d != null) { id = d.Id; return true; }
                        // global fuzzy contains fallback
                        var d4 = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower().Contains(nameLower));
                        if (d4 != null) { id = d4.Id; return true; }
                        break;
                    case "region":
                        var r = _db.Regions.FirstOrDefault(x => x.RegionName.ToLower() == name.ToLower());
                        if (r != null) { id = r.Id; return true; }
                        break;
                }
            }

            return false;
        }

        // Helpers for header-mapped access
        private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string key)
        {
            // headerMap keys are normalized (spaces/underscores/hyphens removed, lowercased)
            // Accept plain key or prefixed variants like lgd{key} or fms{key}
            if (headerMap.TryGetValue(key, out var col)) return row.Cell(col).GetString().Trim();
            var lgd = "lgd" + key;
            if (headerMap.TryGetValue(lgd, out col)) return row.Cell(col).GetString().Trim();
            var fms = "fms" + key;
            if (headerMap.TryGetValue(fms, out col)) return row.Cell(col).GetString().Trim();

            return string.Empty;
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

        // Try resolve FK either by numeric value in the cell or by name lookup in the database
        private bool TryResolveFkId(IXLRow row, Dictionary<string, int> headerMap, string key, string entity, out int id)
        {
            id = 0;
            var raw = GetCellString(row, headerMap, key);
            if (string.IsNullOrEmpty(raw)) return false;

            // try numeric
            if (int.TryParse(raw, out var parsed))
            {
                id = parsed;
                return true;
            }

            // fallback to name lookup depending on entity
            var name = raw.Trim();
            switch (entity.ToLowerInvariant())
            {
                case "zone":
                    var z = _db.Zones.FirstOrDefault(z => z.ZoneName.ToLower() == name.ToLower());
                    if (z != null) { id = z.Id; return true; }
                    break;
                case "state":
                    var s = _db.States.FirstOrDefault(x => x.StateName.ToLower() == name.ToLower());
                    if (s != null) { id = s.Id; return true; }
                    break;
                case "district":
                    var d = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower() == name.ToLower());
                    if (d != null) { id = d.Id; return true; }
                    break;
                case "region":
                    var r = _db.Regions.FirstOrDefault(x => x.RegionName.ToLower() == name.ToLower());
                    if (r != null) { id = r.Id; return true; }
                    break;
            }

            return false;
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