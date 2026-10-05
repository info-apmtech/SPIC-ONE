using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Linq;
using System.IO;

namespace SpicAPI.Controllers
{
    [Authorize]
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

            var groupedErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            void AddGrouped(string group, string item)
            {
                if (!groupedErrors.TryGetValue(group, out var lst)) groupedErrors[group] = lst = [];
                lst.Add(item);
            }

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
                if (!HeaderExists(headerMap, "fmsstatename") && !HeaderExists(headerMap, "statename") && !HeaderExists(headerMap, "stateid")) missingList.Add("FMSStateName or StateName or StateId");
            }
            else if (t == "subdistrict" || t == "sub-district" || t == "sub_district")
            {
                if (!HeaderExists(headerMap, "subdistrictname")) missingList.Add("SubDistrictName");
                if (!HeaderExists(headerMap, "fmsdistrictname") && !HeaderExists(headerMap, "districtname") && !HeaderExists(headerMap, "districtid")) missingList.Add("FMSDistrictName or DistrictName or DistrictId");
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

            var rows = worksheet.RowsUsed().Skip(1).ToList(); // materialize once
            var now = DateTime.UtcNow;

            // ── Pre-load phase: pull all needed reference data into memory before the loop.
            //    This reduces DB round-trips from O(N) to at most 3 queries per upload, regardless of file size.
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // zone / state name dedup
            var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // "name|parentId" dedup
            var zoneNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var stateNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var regionNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // districtName → [(districtId, stateId)] for state-context narrowing in subdistrict upload
            var districtsByName = new Dictionary<string, List<(int Id, int StateId)>>(StringComparer.OrdinalIgnoreCase);

            switch (t)
            {
                case "zone":
                    foreach (var n in _db.Zones.Select(z => z.ZoneName).AsEnumerable())
                        existingNames.Add(n);
                    break;
                case "state":
                    foreach (var n in _db.States.Select(s => s.StateName).AsEnumerable())
                        existingNames.Add(n);
                    foreach (var z in _db.Zones.Select(z => new { z.ZoneName, z.Id }).AsEnumerable())
                        zoneNameToId[z.ZoneName] = z.Id;
                    break;
                case "district":
                    foreach (var d in _db.Districts.Select(d => new { d.DistrictName, d.StateId }).AsEnumerable())
                        existingKeys.Add($"{d.DistrictName}|{d.StateId}");
                    foreach (var s in _db.States.Select(s => new { s.StateName, s.Id }).AsEnumerable())
                        stateNameToId[s.StateName] = s.Id;
                    break;
                case "subdistrict":
                case "sub-district":
                case "sub_district":
                    foreach (var sd in _db.SubDistricts.Select(s => new { s.SubDistrictName, s.DistrictId }).AsEnumerable())
                        existingKeys.Add($"{sd.SubDistrictName}|{sd.DistrictId}");
                    foreach (var s in _db.States.Select(s => new { s.StateName, s.Id }).AsEnumerable())
                        stateNameToId[s.StateName] = s.Id;
                    foreach (var d in _db.Districts.Select(d => new { d.Id, d.DistrictName, d.StateId }).AsEnumerable())
                    {
                        if (!districtsByName.TryGetValue(d.DistrictName, out var lst))
                            districtsByName[d.DistrictName] = lst = new();
                        lst.Add((d.Id, d.StateId));
                    }
                    break;
                case "region":
                    foreach (var r in _db.Regions.Select(r => new { r.RegionName, r.StateId }).AsEnumerable())
                        existingKeys.Add($"{r.RegionName}|{r.StateId}");
                    foreach (var s in _db.States.Select(s => new { s.StateName, s.Id }).AsEnumerable())
                        stateNameToId[s.StateName] = s.Id;
                    break;
                case "headquarter":
                case "headquarters":
                    foreach (var h in _db.Headquarters.Select(h => new { h.HeadquarterName, h.RegionId }).AsEnumerable())
                        existingKeys.Add($"{h.HeadquarterName}|{h.RegionId}");
                    foreach (var r in _db.Regions.Select(r => new { r.RegionName, r.Id }).AsEnumerable())
                        regionNameToId[r.RegionName] = r.Id;
                    break;
            }

            // In-batch duplicate tracking (separate from DB-existing sets so error messages stay distinct)
            var batchNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // zone / state
            var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // "name|parentId"

            // ── Local FK resolvers: use pre-loaded dicts — zero DB calls inside the loop ──
            bool TryResolveZoneId(IXLRow row, out int id)
            {
                id = 0;
                var raw = GetCellString(row, headerMap, "zoneid");
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out id)) return true;
                var name = GetCellString(row, headerMap, "zonename");
                return !string.IsNullOrEmpty(name) && zoneNameToId.TryGetValue(name, out id);
            }

            bool TryResolveStateId(IXLRow row, out int id)
            {
                id = 0;
                // Name columns take priority — numeric stateid may be an LGD code, not a DB id
                var name = GetCellString(row, headerMap, "fmsstatename");
                if (string.IsNullOrEmpty(name)) name = GetCellString(row, headerMap, "statename");
                if (!string.IsNullOrEmpty(name)) return stateNameToId.TryGetValue(name, out id);
                var raw = GetCellString(row, headerMap, "stateid");
                return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out id);
            }

            bool TryResolveDistrictId(IXLRow row, out int id)
            {
                id = 0;
                var distName = GetCellString(row, headerMap, "fmsdistrictname");
                if (string.IsNullOrEmpty(distName)) distName = GetCellString(row, headerMap, "districtname");
                if (!string.IsNullOrEmpty(distName) && districtsByName.TryGetValue(distName, out var candidates))
                {
                    // narrow by state context when available
                    var stateName = GetCellString(row, headerMap, "fmsstatename");
                    if (string.IsNullOrEmpty(stateName)) stateName = GetCellString(row, headerMap, "statename");
                    if (!string.IsNullOrEmpty(stateName) && stateNameToId.TryGetValue(stateName, out var sid))
                    {
                        var match = candidates.FirstOrDefault(c => c.StateId == sid);
                        if (match.Id != 0) { id = match.Id; return true; }
                    }
                    if (candidates.Count > 0) { id = candidates[0].Id; return true; }
                }
                var raw = GetCellString(row, headerMap, "districtid");
                return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out id);
            }

            bool TryResolveRegionId(IXLRow row, out int id)
            {
                id = 0;
                var name = GetCellString(row, headerMap, "regionname");
                if (!string.IsNullOrEmpty(name)) return regionNameToId.TryGetValue(name, out id);
                var raw = GetCellString(row, headerMap, "regionid");
                return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out id);
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        switch (t)
                        {
                            case "zone":
                                {
                                    var zoneName = GetCellString(row, headerMap, "zonename");
                                    if (string.IsNullOrEmpty(zoneName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (existingNames.Contains(zoneName)) { AddGrouped("Already exists in database", $"'{zoneName}' (Row {row.RowNumber()})"); break; }
                                    if (!batchNames.Add(zoneName)) { AddGrouped("Duplicated in this file", $"'{zoneName}' (Row {row.RowNumber()})"); break; }
                                    _db.Zones.Add(new Zone
                                    {
                                        ZoneName = zoneName,
                                        ZoneCode = GetCellString(row, headerMap, "zonecode"),
                                        ZoneColorCode = GetCellString(row, headerMap, "zonecolorcode"),
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    });
                                }
                                break;

                            case "state":
                                {
                                    var stateName = GetCellString(row, headerMap, "statename");
                                    if (string.IsNullOrEmpty(stateName)) stateName = GetCellString(row, headerMap, "fmsstatename");
                                    if (string.IsNullOrEmpty(stateName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (existingNames.Contains(stateName)) { AddGrouped("Already exists in database", $"'{stateName}' (Row {row.RowNumber()})"); break; }
                                    if (!batchNames.Add(stateName)) { AddGrouped("Duplicated in this file", $"'{stateName}' (Row {row.RowNumber()})"); break; }
                                    if (!TryResolveZoneId(row, out var zoneId))
                                    {
                                        var triedZone = GetCellString(row, headerMap, "zonename");
                                        if (string.IsNullOrEmpty(triedZone)) triedZone = GetCellString(row, headerMap, "zoneid");
                                        AddGrouped($"Zone '{triedZone}' not found in database", $"'{stateName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    _db.States.Add(new State
                                    {
                                        StateName = stateName,
                                        ZoneId = zoneId,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    });
                                }
                                break;

                            case "district":
                                {
                                    var districtName = GetCellString(row, headerMap, "districtname");
                                    if (string.IsNullOrEmpty(districtName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveStateId(row, out var stateId))
                                    {
                                        var triedState = GetCellString(row, headerMap, "fmsstatename");
                                        if (string.IsNullOrEmpty(triedState)) triedState = GetCellString(row, headerMap, "statename");
                                        if (string.IsNullOrEmpty(triedState)) triedState = GetCellString(row, headerMap, "stateid");
                                        AddGrouped($"State '{triedState}' not found in database", $"'{districtName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = $"{districtName}|{stateId}";
                                    if (existingKeys.Contains(key)) { AddGrouped("Already exists in database", $"'{districtName}' (Row {row.RowNumber()})"); break; }
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{districtName}' (Row {row.RowNumber()})"); break; }
                                    _db.Districts.Add(new District
                                    {
                                        DistrictName = districtName,
                                        StateId = stateId,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    });
                                }
                                break;

                            case "subdistrict":
                            case "sub-district":
                            case "sub_district":
                                {
                                    var subName = GetCellString(row, headerMap, "subdistrictname");
                                    if (string.IsNullOrEmpty(subName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveDistrictId(row, out var districtId))
                                    {
                                        var triedDistrict = GetCellString(row, headerMap, "fmsdistrictname");
                                        if (string.IsNullOrEmpty(triedDistrict)) triedDistrict = GetCellString(row, headerMap, "districtname");
                                        if (string.IsNullOrEmpty(triedDistrict)) triedDistrict = GetCellString(row, headerMap, "districtid");
                                        AddGrouped($"District '{triedDistrict}' not found in database", $"'{subName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = $"{subName}|{districtId}";
                                    if (existingKeys.Contains(key)) { AddGrouped("Already exists in database", $"'{subName}' (Row {row.RowNumber()})"); break; }
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{subName}' (Row {row.RowNumber()})"); break; }
                                    _db.SubDistricts.Add(new SubDistrict
                                    {
                                        SubDistrictName = subName,
                                        DistrictId = districtId,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    });
                                }
                                break;

                            case "region":
                                {
                                    var regionName = GetCellString(row, headerMap, "regionname");
                                    if (string.IsNullOrEmpty(regionName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveStateId(row, out var stateId))
                                    {
                                        var triedState = GetCellString(row, headerMap, "fmsstatename");
                                        if (string.IsNullOrEmpty(triedState)) triedState = GetCellString(row, headerMap, "statename");
                                        if (string.IsNullOrEmpty(triedState)) triedState = GetCellString(row, headerMap, "stateid");
                                        AddGrouped($"State '{triedState}' not found in database", $"'{regionName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = $"{regionName}|{stateId}";
                                    if (existingKeys.Contains(key)) { AddGrouped("Already exists in database", $"'{regionName}' (Row {row.RowNumber()})"); break; }
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{regionName}' (Row {row.RowNumber()})"); break; }
                                    _db.Regions.Add(new Region
                                    {
                                        RegionName = regionName,
                                        StateId = stateId,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    });
                                }
                                break;

                            case "headquarter":
                            case "headquarters":
                                {
                                    var hqName = GetCellString(row, headerMap, "headquartername");
                                    if (string.IsNullOrEmpty(hqName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveRegionId(row, out var regionId))
                                    {
                                        var triedRegion = GetCellString(row, headerMap, "regionname");
                                        if (string.IsNullOrEmpty(triedRegion)) triedRegion = GetCellString(row, headerMap, "regionid");
                                        AddGrouped($"Region '{triedRegion}' not found in database", $"'{hqName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = $"{hqName}|{regionId}";
                                    if (existingKeys.Contains(key)) { AddGrouped("Already exists in database", $"'{hqName}' (Row {row.RowNumber()})"); break; }
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{hqName}' (Row {row.RowNumber()})"); break; }
                                    _db.Headquarters.Add(new Headquarter
                                    {
                                        HeadquarterName = hqName,
                                        RegionId = regionId,
                                        IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive")),
                                        CreatedAt = now,
                                        UpdatedAt = now,
                                        UpdatedBy = "bulk-upload"
                                    });
                                }
                                break;

                            default:
                                return BadRequest(new { Success = false, Message = "Unknown type. Use Zone, State, District, SubDistrict, Region, Headquarter" });
                        }
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogWarning(exRow, "Row parse error");
                        AddGrouped("Parse errors", $"Row {row.RowNumber()}: {exRow.Message}");
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

            var totalSkipped = groupedErrors.Values.Sum(v => v.Count);
            return Ok(new { Success = true, Message = "Upload completed", GroupedErrors = groupedErrors, TotalSkipped = totalSkipped });
        }

        // GET /api/locationbulkupload/sample-template?type=zone
        [HttpGet("sample-template")]
        public IActionResult SampleTemplate([FromQuery] string type)
        {
            var t = type?.ToLowerInvariant() ?? "";

            (string Header, string Sample)[] columns = t switch
            {
                "zone" => new[]
                {
            ("ZoneName", "North Zone"),
            ("ZoneCode", "NZ"),
            ("ZoneColorCode", "#3B82F6"),
            ("IsActive", "TRUE")
        },
                "state" => new[]
                {
            ("StateName", "Tamil Nadu"),
            ("ZoneName", "South Zone"),
            ("IsActive", "TRUE")
        },
                "district" => new[]
                {
            ("DistrictName", "Chennai"),
            ("StateName", "Tamil Nadu"),
            ("IsActive", "TRUE")
        },
                "subdistrict" or "sub-district" or "sub_district" => new[]
                {
            ("SubDistrictName", "Egmore"),
            ("DistrictName", "Chennai"),
            ("StateName", "Tamil Nadu"),
            ("IsActive", "TRUE")
        },
                "region" => new[]
                {
            ("RegionName", "Chennai Region"),
            ("StateName", "Tamil Nadu"),
            ("IsActive", "TRUE")
        },
                "headquarter" or "headquarters" => new[]
                {
            ("HeadquarterName", "Chennai HQ"),
            ("RegionName", "Chennai Region"),
            ("IsActive", "TRUE")
        },
                _ => Array.Empty<(string, string)>()
            };

            if (columns.Length == 0)
                return BadRequest(new { Success = false, Message = "Unknown type. Use Zone, State, District, SubDistrict, Region, Headquarter" });

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

                // sample data row (guidance for the user)
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

        // Try resolve FK by checking multiple header keys (e.g., numeric id or name columns).
        // GetCellString handles lgd/fms prefix column variants automatically.
        // Keys ending with "id" try numeric parse first (internal DB id).
        // Keys ending with "name" always do DB name lookup (never use LGD numeric codes as FK).
        private bool TryResolveFkIdFromKeys(IXLRow row, Dictionary<string, int> headerMap, string[] keys, string entity, out int id)
        {
            id = 0;
            foreach (var k in keys)
            {
                // GetCellString tries key, lgd+key, fms+key automatically
                var raw = GetCellString(row, headerMap, k);
                if (string.IsNullOrEmpty(raw)) continue;

                // ID-type keys: try numeric parse first (internal DB id)
                if (k.EndsWith("id", StringComparison.OrdinalIgnoreCase) && int.TryParse(raw, out var parsed))
                {
                    id = parsed;
                    return true;
                }

                // Name-type keys: always resolve via DB name lookup
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
                        // Narrow by state context from the same row when available
                        var stateCtx = GetCellString(row, headerMap, "fmsstatename");
                        if (string.IsNullOrEmpty(stateCtx)) stateCtx = GetCellString(row, headerMap, "statename");
                        var nameLower = name.ToLower();
                        if (!string.IsNullOrEmpty(stateCtx))
                        {
                            var st = _db.States.FirstOrDefault(x => x.StateName.ToLower() == stateCtx.ToLower());
                            if (st != null)
                            {
                                var d2 = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower() == nameLower && x.StateId == st.Id);
                                if (d2 != null) { id = d2.Id; return true; }
                                var d3 = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower().Contains(nameLower) && x.StateId == st.Id);
                                if (d3 != null) { id = d3.Id; return true; }
                            }
                        }
                        var d = _db.Districts.FirstOrDefault(x => x.DistrictName.ToLower() == nameLower);
                        if (d != null) { id = d.Id; return true; }
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