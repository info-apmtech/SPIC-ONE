using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.MasterData;
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

            // ─────────────────────────────────────────────────────────────────────────
            // PRE-LOAD + VALIDATION PASS (read-only, before the transaction opens).
            //
            // The sheet's own subject (Zone / State / District / SubDistrict / Region /
            // Headquarter) is what the admin is uploading, so those rows are still created.
            // What must never happen is a *referenced* master being invented: a State row
            // pointing at a Zone that does not exist, a SubDistrict pointing at a District
            // that does not exist, and so on. Every unresolved parent across the whole file
            // is collected here and reported together, before a single row is written.
            //
            // All name matching runs through MasterNormalizer via MasterLookup, and the
            // lookup is built with GroupBy(...).First() so pre-existing duplicate master rows
            // cannot throw. District/Region/Headquarter resolve parent-scoped.
            // ─────────────────────────────────────────────────────────────────────────
            var masters = await MasterLookup.LoadAsync(_db);

            // Numeric-id columns are accepted too, but a raw number is only trusted when it
            // actually points at a row that exists — previously a stale id was taken at face value.
            var zoneIdSet = (await _db.Zones.AsNoTracking().Select(z => z.Id).ToListAsync()).ToHashSet();
            var stateIdSet = (await _db.States.AsNoTracking().Select(s => s.Id).ToListAsync()).ToHashSet();
            var districtIdSet = (await _db.Districts.AsNoTracking().Select(d => d.Id).ToListAsync()).ToHashSet();
            var regionIdSet = (await _db.Regions.AsNoTracking().Select(r => r.Id).ToListAsync()).ToHashSet();

            // Unscoped district name → candidates, used only to accept an unambiguous name
            // when the row carries no state context to scope it with.
            var districtCandidates = (await _db.Districts.AsNoTracking()
                    .Select(d => new { d.Id, d.DistrictName, d.StateId })
                    .ToListAsync())
                .GroupBy(d => MasterNormalizer.Normalize(d.DistrictName))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.Select(d => (d.Id, d.StateId)).ToList());

            // ── Existing rows, keyed canonically (tracked, so the loop can update them) ──
            //
            // Nothing here creates a location master. Each sheet's own subject must already
            // exist in master maintenance; this upload only updates the existing row. GroupBy
            // (...First() keeps an existing duplicate in the table from throwing.
            var zoneByName = (await _db.Zones.ToListAsync())
                .GroupBy(z => MasterNormalizer.Normalize(z.ZoneName))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            var stateByName = (await _db.States.ToListAsync())
                .GroupBy(s => MasterNormalizer.Normalize(s.StateName))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            var districtByKey = (await _db.Districts.ToListAsync())
                .GroupBy(d => MasterNormalizer.Scoped(d.DistrictName, d.StateId))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            var subDistrictByKey = (await _db.SubDistricts.ToListAsync())
                .GroupBy(sd => MasterNormalizer.Scoped(sd.SubDistrictName, sd.DistrictId))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            var regionByKey = (await _db.Regions.ToListAsync())
                .GroupBy(r => MasterNormalizer.Scoped(r.RegionName, r.StateId))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            var headquarterByKey = (await _db.Headquarters.ToListAsync())
                .GroupBy(h => MasterNormalizer.Scoped(h.HeadquarterName, h.RegionId))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First());

            // In-batch duplicate tracking (separate from DB-existing sets so error messages stay distinct)
            var batchNames = new HashSet<string>(StringComparer.Ordinal); // zone / state
            var batchKeys = new HashSet<string>(StringComparer.Ordinal);  // Scoped(name, parentId)

            // ── Local FK resolvers: pre-loaded dicts only — zero DB calls inside the loop ──
            // Each returns the value it tried, so a failure can name the exact master to create.
            bool TryResolveZoneId(IXLRow row, out int id, out string? tried)
            {
                id = 0;
                var raw = GetCellString(row, headerMap, "zoneid");
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out var parsed))
                {
                    tried = raw;
                    if (zoneIdSet.Contains(parsed)) { id = parsed; return true; }
                    return false;
                }
                var name = GetCellString(row, headerMap, "zonename");
                tried = name;
                return !MasterNormalizer.IsBlank(name) && masters.TryGetZone(name, out id);
            }

            bool TryResolveStateId(IXLRow row, out int id, out string? tried)
            {
                id = 0;
                // Name columns take priority — numeric stateid may be an LGD code, not a DB id
                var name = GetCellString(row, headerMap, "fmsstatename");
                if (string.IsNullOrEmpty(name)) name = GetCellString(row, headerMap, "statename");
                if (!MasterNormalizer.IsBlank(name))
                {
                    tried = name;
                    return masters.TryGetState(name, out id);
                }
                var raw = GetCellString(row, headerMap, "stateid");
                tried = raw;
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out var parsed) && stateIdSet.Contains(parsed))
                { id = parsed; return true; }
                return false;
            }

            bool TryResolveDistrictId(IXLRow row, out int id, out string? tried)
            {
                id = 0;
                var distName = GetCellString(row, headerMap, "fmsdistrictname");
                if (string.IsNullOrEmpty(distName)) distName = GetCellString(row, headerMap, "districtname");
                if (!MasterNormalizer.IsBlank(distName))
                {
                    tried = distName;
                    var stateName = GetCellString(row, headerMap, "fmsstatename");
                    if (string.IsNullOrEmpty(stateName)) stateName = GetCellString(row, headerMap, "statename");
                    if (!MasterNormalizer.IsBlank(stateName) && masters.TryGetState(stateName, out var sid))
                        return masters.TryGetDistrict(distName, sid, out id);

                    // No usable state context: only accept the name when it is unambiguous,
                    // otherwise guessing a parent is exactly what this rule forbids.
                    if (districtCandidates.TryGetValue(MasterNormalizer.Normalize(distName), out var cands) &&
                        cands.Count == 1)
                    { id = cands[0].Id; return true; }
                    return false;
                }
                var raw = GetCellString(row, headerMap, "districtid");
                tried = raw;
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out var parsed) && districtIdSet.Contains(parsed))
                { id = parsed; return true; }
                return false;
            }

            bool TryResolveRegionId(IXLRow row, out int id, out string? tried)
            {
                id = 0;
                var name = GetCellString(row, headerMap, "regionname");
                if (!MasterNormalizer.IsBlank(name))
                {
                    tried = name;
                    // Region is unique per state, so it can only be resolved with its state.
                    if (!TryResolveStateId(row, out var sid, out _))
                        return false;
                    return masters.TryGetRegion(name, sid, out id);
                }
                var raw = GetCellString(row, headerMap, "regionid");
                tried = raw;
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, out var parsed) && regionIdSet.Contains(parsed))
                { id = parsed; return true; }
                return false;
            }

            // ── Validation pass ────────────────────────────────────────────────────
            // A blank parent column on a required relationship is reported too: it is still a
            // master the row needs, and stopping here beats writing the rest of the file.
            const string BlankMarker = "<blank>";
            var missingMasters = new MissingMasterCollector();

            foreach (var row in rows)
            {
                switch (t)
                {
                    case "zone":
                        {
                            var name = GetCellString(row, headerMap, "zonename");
                            if (MasterNormalizer.IsBlank(name)) break;
                            if (!zoneByName.ContainsKey(MasterNormalizer.Normalize(name)))
                                missingMasters.Add("Zone", name, row.RowNumber());
                            break;
                        }
                    case "state":
                        {
                            var name = GetCellString(row, headerMap, "statename");
                            if (string.IsNullOrEmpty(name)) name = GetCellString(row, headerMap, "fmsstatename");
                            if (MasterNormalizer.IsBlank(name)) break; // row already reported as "Empty name"

                            if (!stateByName.ContainsKey(MasterNormalizer.Normalize(name)))
                                missingMasters.Add("State", name, row.RowNumber());

                            if (!TryResolveZoneId(row, out _, out var zone))
                                missingMasters.Add("Zone", MasterNormalizer.IsBlank(zone) ? BlankMarker : zone, row.RowNumber());
                            break;
                        }
                    case "district":
                        {
                            var name = GetCellString(row, headerMap, "districtname");
                            if (MasterNormalizer.IsBlank(name)) break;

                            if (TryResolveStateId(row, out var stateId, out var state))
                            {
                                if (!districtByKey.ContainsKey(MasterNormalizer.Scoped(name, stateId)))
                                    missingMasters.Add("District", name, row.RowNumber());
                            }
                            else
                            {
                                missingMasters.Add("State", MasterNormalizer.IsBlank(state) ? BlankMarker : state, row.RowNumber());
                            }
                            break;
                        }
                    case "subdistrict":
                    case "sub-district":
                    case "sub_district":
                        {
                            var name = GetCellString(row, headerMap, "subdistrictname");
                            if (MasterNormalizer.IsBlank(name)) break;

                            if (TryResolveDistrictId(row, out var districtId, out var district))
                            {
                                if (!subDistrictByKey.ContainsKey(MasterNormalizer.Scoped(name, districtId)))
                                    missingMasters.Add("SubDistrict", name, row.RowNumber());
                            }
                            else
                            {
                                missingMasters.Add("District", MasterNormalizer.IsBlank(district) ? BlankMarker : district, row.RowNumber());
                            }
                            break;
                        }
                    case "region":
                        {
                            var name = GetCellString(row, headerMap, "regionname");
                            if (MasterNormalizer.IsBlank(name)) break;

                            if (TryResolveStateId(row, out var stateId, out var state))
                            {
                                if (!regionByKey.ContainsKey(MasterNormalizer.Scoped(name, stateId)))
                                    missingMasters.Add("Region", name, row.RowNumber());
                            }
                            else
                            {
                                missingMasters.Add("State", MasterNormalizer.IsBlank(state) ? BlankMarker : state, row.RowNumber());
                            }
                            break;
                        }
                    case "headquarter":
                    case "headquarters":
                        {
                            var name = GetCellString(row, headerMap, "headquartername");
                            if (MasterNormalizer.IsBlank(name)) break;

                            if (TryResolveRegionId(row, out var regionId, out var region))
                            {
                                if (!headquarterByKey.ContainsKey(MasterNormalizer.Scoped(name, regionId)))
                                    missingMasters.Add("Headquarter", name, row.RowNumber());
                            }
                            else
                            {
                                missingMasters.Add("Region", MasterNormalizer.IsBlank(region) ? BlankMarker : region, row.RowNumber());
                            }
                            break;
                        }
                }
            }

            if (missingMasters.HasAny)
            {
                var (message, missingLines) = missingMasters.Build("location master");

                _logger.LogWarning(
                    "Location bulk upload rejected: {Count} missing master record(s): {Masters}",
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
                                    var zoneKey = MasterNormalizer.Normalize(zoneName);
                                    if (!batchNames.Add(zoneKey)) { AddGrouped("Duplicated in this file", $"'{zoneName}' (Row {row.RowNumber()})"); break; }
                                    if (!zoneByName.TryGetValue(zoneKey, out var ent)) { AddGrouped("Zone does not exist in master data", $"'{zoneName}' (Row {row.RowNumber()})"); break; }
                                    var zoneCode = GetCellString(row, headerMap, "zonecode");
                                    if (!string.IsNullOrEmpty(zoneCode)) ent.ZoneCode = zoneCode;
                                    var zoneColor = GetCellString(row, headerMap, "zonecolorcode");
                                    if (!string.IsNullOrEmpty(zoneColor)) ent.ZoneColorCode = zoneColor;
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                }
                                break;

                            case "state":
                                {
                                    var stateName = GetCellString(row, headerMap, "statename");
                                    if (string.IsNullOrEmpty(stateName)) stateName = GetCellString(row, headerMap, "fmsstatename");
                                    if (string.IsNullOrEmpty(stateName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    var stateKey = MasterNormalizer.Normalize(stateName);
                                    if (!batchNames.Add(stateKey)) { AddGrouped("Duplicated in this file", $"'{stateName}' (Row {row.RowNumber()})"); break; }
                                    if (!stateByName.TryGetValue(stateKey, out var ent)) { AddGrouped("State does not exist in master data", $"'{stateName}' (Row {row.RowNumber()})"); break; }
                                    if (!TryResolveZoneId(row, out var zoneId, out _))
                                    {
                                        // Unreachable: the validation pass already rejected every
                                        // unresolvable Zone before the transaction opened.
                                        AddGrouped("Zone not found in database", $"'{stateName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    ent.ZoneId = zoneId;
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                }
                                break;

                            case "district":
                                {
                                    var districtName = GetCellString(row, headerMap, "districtname");
                                    if (string.IsNullOrEmpty(districtName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveStateId(row, out var stateId, out _))
                                    {
                                        AddGrouped("State not found in database", $"'{districtName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = MasterNormalizer.Scoped(districtName, stateId);
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{districtName}' (Row {row.RowNumber()})"); break; }
                                    if (!districtByKey.TryGetValue(key, out var ent)) { AddGrouped("District does not exist in master data", $"'{districtName}' (Row {row.RowNumber()})"); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                }
                                break;

                            case "subdistrict":
                            case "sub-district":
                            case "sub_district":
                                {
                                    var subName = GetCellString(row, headerMap, "subdistrictname");
                                    if (string.IsNullOrEmpty(subName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveDistrictId(row, out var districtId, out _))
                                    {
                                        AddGrouped("District not found in database", $"'{subName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = MasterNormalizer.Scoped(subName, districtId);
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{subName}' (Row {row.RowNumber()})"); break; }
                                    if (!subDistrictByKey.TryGetValue(key, out var ent)) { AddGrouped("SubDistrict does not exist in master data", $"'{subName}' (Row {row.RowNumber()})"); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                }
                                break;

                            case "region":
                                {
                                    var regionName = GetCellString(row, headerMap, "regionname");
                                    if (string.IsNullOrEmpty(regionName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveStateId(row, out var stateId, out _))
                                    {
                                        AddGrouped("State not found in database", $"'{regionName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = MasterNormalizer.Scoped(regionName, stateId);
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{regionName}' (Row {row.RowNumber()})"); break; }
                                    if (!regionByKey.TryGetValue(key, out var ent)) { AddGrouped("Region does not exist in master data", $"'{regionName}' (Row {row.RowNumber()})"); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
                                }
                                break;

                            case "headquarter":
                            case "headquarters":
                                {
                                    var hqName = GetCellString(row, headerMap, "headquartername");
                                    if (string.IsNullOrEmpty(hqName)) { AddGrouped("Empty name", $"Row {row.RowNumber()}"); break; }
                                    if (!TryResolveRegionId(row, out var regionId, out _))
                                    {
                                        AddGrouped("Region not found in database", $"'{hqName}' (Row {row.RowNumber()})");
                                        break;
                                    }
                                    var key = MasterNormalizer.Scoped(hqName, regionId);
                                    if (!batchKeys.Add(key)) { AddGrouped("Duplicated in this file", $"'{hqName}' (Row {row.RowNumber()})"); break; }
                                    if (!headquarterByKey.TryGetValue(key, out var ent)) { AddGrouped("Headquarter does not exist in master data", $"'{hqName}' (Row {row.RowNumber()})"); break; }
                                    ent.IsActive = ParseBoolCellByValue(GetCellString(row, headerMap, "isactive"));
                                    ent.UpdatedAt = now;
                                    ent.UpdatedBy = "bulk-upload";
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

        private static string NormalizeHeader(string h) => MasterNormalizer.NormalizeHeader(h);

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