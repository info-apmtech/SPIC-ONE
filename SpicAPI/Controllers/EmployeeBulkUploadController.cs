using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.MasterData;
using SPIC.Core.Entities;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeBulkUploadController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<EmployeeBulkUploadController> _logger;
        private readonly UserManager<UserInfo> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		public EmployeeBulkUploadController(
            AppDbContext db,
            ILogger<EmployeeBulkUploadController> logger,
            UserManager<UserInfo> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _logger = logger;
            _userManager = userManager;
			_roleManager = roleManager;
		}

        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

            // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // MASTER LOOKUPS (read-only).
            //
            // MasterLookup centralises the canonical normalization and builds every map with
            // GroupBy(...).First() rather than ToDictionary, because a master table may already
            // hold two rows whose names normalize to the same key (for example "Puducherry" and
            // "Puducherry "). ToDictionary throws "An item with the same key has already been
            // added" in that case and takes the whole upload down; grouping keeps the first row.
            //
            // Region is keyed by (normalized RegionName, StateId) and Headquarter by
            // (normalized HeadquarterName, RegionId), so the same child name under two different
            // parents stays two different masters instead of resolving to whichever loaded first.
            // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var masters = await MasterLookup.LoadAsync(_db);

            var designationMap = (await _db.Designations
                    .Select(d => new { d.Id, d.Name, d.IsActive })
                    .ToListAsync())
                .GroupBy(d => MasterNormalizer.Normalize(d.Name))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => new { g.First().Id, g.First().IsActive });

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            var headerRow = worksheet.Row(1);
            var lastHeaderCell = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (lastHeaderCell == 0)
                return BadRequest(new { Success = false, Message = "Empty worksheet or missing header row" });

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastHeaderCell; c++)
            {
                var n = NormalizeHeader(headerRow.Cell(c).GetString());
                if (!string.IsNullOrEmpty(n) && !headerMap.ContainsKey(n))
                    headerMap[n] = c;
            }

            string Cell(IXLRow row, string key) =>
                headerMap.TryGetValue(key, out var col) ? row.Cell(col).GetString().Trim() : string.Empty;

            var rows = worksheet.RowsUsed().Skip(1).ToList();
            var now = DateTime.UtcNow;

            var groupedErrors = new Dictionary<string, List<string>>();
            void AddError(string group, string msg)
            {
                if (!groupedErrors.ContainsKey(group)) groupedErrors[group] = new();
                groupedErrors[group].Add(msg);
            }

            int inserted = 0;
            int skipped = 0;

            // Collect existing employees to avoid DB hits and handle duplicates smartly
            var existingEmployees = await _db.EmployeeInformation
                .Where(e => e.EmployeeCode != null && e.EmployeeCode != "")
                .GroupBy(e => e.EmployeeCode)
                .ToDictionaryAsync(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // FIX: Check the actual AppUsers table for existing UserNames to prevent silent crashes
            var existingUserNames = await _userManager.Users
                .Where(u => u.UserName != null)
                .Select(u => u.UserName)
                .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

            // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // MASTER VALIDATION PASS (read-only).
            //
            // Runs BEFORE the transaction is opened and before any SaveChangesAsync, so
            // when master data is missing the database is left completely unchanged:
            // no employee, no user, no employeelogin and no master record is written.
            //
            // Bulk upload never creates master data. Every Zone / State / Region /
            // Headquarters / Designation value the file depends on must already exist.
            //
            // Location columns are validated ONLY for the roles that actually require them:
            //   AVP            -> Zone
            //   SMD, SMM       -> State
            //   RM, RMD        -> State + Region
            //   MDO, MO, JMDO  -> State + Region + Headquarters
            // A column that is blank or holds an unknown value for a role that does not use it
            // is ignored, so an SMM row carrying an unrelated Region value is not rejected.
            //
            // Rows whose Permission is not an accepted role, and rows with an unparsable
            // Permission, are left to the row loop below so the existing per-row error
            // messages stay unchanged.
            // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var missing = new MissingMasterCollector();

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = row.RowNumber();

                var permission = Cell(row, "permission");
                if (!Enum.TryParse<AppRole>(permission, ignoreCase: true, out var role))
                    continue;
                if (!IsBulkUploadRole(role))
                    continue;

                var zoneName = Cell(row, "zone");
                var stateName = Cell(row, "state");
                var regionName = Cell(row, "region");
                var hqName = Cell(row, "hq");
                var designationName = Cell(row, "designation");

                // Designation is optional, but when supplied it must exist: it is a master
                // reference like any other and must never be invented here.
                if (!string.IsNullOrWhiteSpace(designationName) &&
                    !designationMap.ContainsKey(MasterNormalizer.Normalize(designationName)))
                {
                    missing.Add("Designation", designationName, rowNumber);
                }

                var needsZone = role == AppRole.AVP;
                var needsState = role == AppRole.SMD || role == AppRole.SMM
                                 || role == AppRole.RM || role == AppRole.RMD
                                 || role == AppRole.MDO || role == AppRole.MO || role == AppRole.JMDO;
                var needsRegion = role == AppRole.RM || role == AppRole.RMD
                                  || role == AppRole.MDO || role == AppRole.MO || role == AppRole.JMDO;
                var needsHq = role == AppRole.MDO || role == AppRole.MO || role == AppRole.JMDO;

                if (needsZone && !string.IsNullOrWhiteSpace(zoneName) &&
                    !masters.TryGetZone(zoneName, out _))
                {
                    missing.Add("Zone", zoneName, rowNumber);
                }

                var stateOk = true;
                if (needsState && !string.IsNullOrWhiteSpace(stateName))
                    stateOk = masters.TryGetState(stateName, out _);

                if (needsState && stateOk)
                {
                    // Region is scoped by StateId and Headquarters by RegionId, so each level is
                    // resolved against the parent the row actually named. A name that exists under a
                    // different parent is reported as missing rather than silently mismatched.
                    if (needsRegion && !string.IsNullOrWhiteSpace(regionName))
                    {
                        masters.TryGetState(stateName, out var parentStateId);
                        if (!masters.TryGetRegion(regionName, parentStateId, out _))
                            missing.Add("Region", regionName, rowNumber);
                    }

                    if (needsHq && !string.IsNullOrWhiteSpace(hqName))
                    {
                        masters.TryGetState(stateName, out var hqStateId);
                        var regionIdForHq = 0;
                        var regionResolvable = !needsRegion ||
                            masters.TryGetRegion(regionName, hqStateId, out regionIdForHq);
                        if (regionResolvable && !masters.TryGetHeadquarter(hqName, regionIdForHq, out _))
                            missing.Add("Headquarters", hqName, rowNumber);
                    }
                }
            }

            if (missing.HasAny)
            {
                var (message, missingLines) = missing.Build("employee");

                _logger.LogWarning(
                    "Employee bulk upload rejected: {Count} missing master record(s): {Masters}",
                    missing.Entries.Count,
                    string.Join(", ", missing.Entries.Select(m => $"{m.Kind}='{m.Value}'")));

                return BadRequest(new
                {
                    Success = false,
                    Message = message,
                    MissingMasterLines = missingLines,
                    MissingMasters = missing.Entries
                        .Select(m => new { m.Kind, m.Value, RowNumbers = m.RowNumbers })
                        .ToList()
                });
            }

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        var employeeId = Cell(row, "employeeid");
                        var empName = Cell(row, "empname");
                        if (string.IsNullOrWhiteSpace(empName)) empName = Cell(row, "employeename");
                        var userName = Cell(row, "username");
                        var permission = Cell(row, "permission");
                        var stateName = Cell(row, "state");
                        var regionName = Cell(row, "region");
                        var hqName = Cell(row, "hq");
                        var zoneName = Cell(row, "zone");
                        var phone = Cell(row, "phonenumber");
                        var email = Cell(row, "emailid");
                        var designationName = Cell(row, "designation");

                        if (string.IsNullOrWhiteSpace(email)) email = Cell(row, "email");

                        // --- Validation ---
                        if (string.IsNullOrWhiteSpace(empName))
                        {
                            AddError("Missing Name", $"Row {rowNum}: Employee name is empty.");
                            skipped++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(userName))
                        {
                            AddError("Missing UserName", $"Row {rowNum} ({empName}): UserName is empty.");
                            skipped++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(phone))
                        {
                            AddError("Missing Phone", $"Row {rowNum} ({empName}): PhoneNumber is empty â€” used as password.");
                            skipped++;
                            continue;
                        }

                        // Phone number must be at least 6 digits to pass Identity Password requirements
                        if (phone.Length < 6)
                        {
                            AddError("Invalid Password length", $"Row {rowNum} ({empName}): Phone number must be at least 6 characters.");
                            skipped++;
                            continue;
                        }

                        if (existingUserNames.Contains(userName))
                        {
                            AddError("Duplicate UserName", $"Row {rowNum} ({empName}): UserName '{userName}' already exists.");
                            skipped++;
                            continue;
                        }

                        if (!Enum.TryParse<AppRole>(permission, ignoreCase: true, out var role))
                        {
                            AddError("Invalid Role", $"Row {rowNum} ({empName}): Permission '{permission}' is not a valid role.");
                            skipped++;
                            continue;
                        }

                        // --- Role Definitions ---
                        bool isAvpRole = role == AppRole.AVP;
                        bool isSmRole = role == AppRole.SMD || role == AppRole.SMM;
                        bool isRmRole = role == AppRole.RM || role == AppRole.RMD;
                        bool isMoRole = role == AppRole.MDO || role == AppRole.MO || role == AppRole.JMDO;

                        if (!isAvpRole && !isSmRole && !isRmRole && !isMoRole)
                        {
                            AddError("Role Not Allowed", $"Row {rowNum} ({empName}): Role '{role}' cannot be created via bulk upload.");
                            skipped++;
                            continue;
                        }

                        int stateId = 0, regionId = 0, hqId = 0, zoneId = 0;
                        var currentUser = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                          ?? User?.Identity?.Name
                                          ?? "Unknown";

                        // Master records are never created here. The read-only validation pass above
                        // already stopped the upload if any value below was absent from the master
                        // tables, so these lookups are expected to resolve. Region is resolved under
                        // the row's State and Headquarters under that row's Region, so an identically
                        // named master belonging to a different parent is never picked up by mistake.
                        if (!string.IsNullOrWhiteSpace(stateName))
                            masters.TryGetState(stateName, out stateId);

                        if (!string.IsNullOrWhiteSpace(regionName))
                            masters.TryGetRegion(regionName, stateId, out regionId);

                        if (!string.IsNullOrWhiteSpace(hqName))
                            masters.TryGetHeadquarter(hqName, regionId, out hqId);

                        if (!string.IsNullOrWhiteSpace(zoneName))
                            masters.TryGetZone(zoneName, out zoneId);

                        // --- Apply Your Specific Rules ---
                        if (isAvpRole) // AVP -> Requires Zone
                        {
                            if (zoneId == 0) AddError("Missing Zone", $"Row {rowNum} ({empName}): Zone is required for {role}.");
                        }
                        else if (isMoRole) // MO, MDO, JMDO -> Requires State, Region, HQ
                        {
                            if (stateId == 0) AddError("Missing State", $"Row {rowNum} ({empName}): State is required for {role}.");
                            if (regionId == 0) AddError("Missing Region", $"Row {rowNum} ({empName}): Region is required for {role}.");
                            if (hqId == 0) AddError("Missing HQ", $"Row {rowNum} ({empName}): HQ is required for {role}.");
                        }
                        else if (isRmRole) // RM, RMD -> Requires State, Region
                        {
                            if (stateId == 0) AddError("Missing State", $"Row {rowNum} ({empName}): State is required for {role}.");
                            if (regionId == 0) AddError("Missing Region", $"Row {rowNum} ({empName}): Region is required for {role}.");
                        }
                        else if (isSmRole) // SMD, SMM -> Requires State
                        {
                            if (stateId == 0) AddError("Missing State", $"Row {rowNum} ({empName}): State is required for {role}.");
                        }

                        // Skip row if any required location failed validation
                        if ((isAvpRole && zoneId == 0) ||
                            (isMoRole && (stateId == 0 || regionId == 0 || hqId == 0)) ||
                            (isRmRole && (stateId == 0 || regionId == 0)) ||
                            (isSmRole && stateId == 0))
                        {
                            skipped++;
                            continue;
                        }

                        // --- Resolve Designation (optional, but if provided it MUST be valid and active) ---
                        int? designationId = null;
                        if (!string.IsNullOrWhiteSpace(designationName))
                        {
                            if (!designationMap.TryGetValue(MasterNormalizer.Normalize(designationName), out var desig))
                            {
                                AddError("Unknown Designation",
                                    $"Row {rowNum} ({empName}): Designation '{designationName}' does not exist.");
                                skipped++;
                                continue;
                            }

                            if (!desig.IsActive)
                            {
                                AddError("Inactive Designation",
                                    $"Row {rowNum} ({empName}): Designation '{designationName}' is deactivated. Activate it first, or use a different one.");
                                skipped++;
                                continue;
                            }

                            designationId = desig.Id;
                        }

                        // ---  1. Create AppUsers (UserInfo) First ---

                        var user = new UserInfo
                        {
                            UserName = userName,
                            Password = phone,
                            Email = email ?? "",
                            PhoneNumber = phone,
                            Name = empName,
                            Role = role,
                            DesignationId = designationId,
                            CreatedAt = now,
                            CreatedBy = currentUser,
                            UpdatedAt = now,
                            UpdatedBy = currentUser,
                            IsActive = true
                        };

                        var identityResult = await _userManager.CreateAsync(user, phone);

                        if (!identityResult.Succeeded)
                        {
                            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                            AddError("AppUser Save Error", $"Row {rowNum} ({empName}): {errors}");
                            skipped++;
                            continue;
                        }
						try
						{
							await EnsureRoleAndAssignAsync(user, role);
						}
						catch (Exception roleEx)
						{
							AddError("Role Save Error", $"Row {rowNum} ({empName}): {roleEx.Message}");
							skipped++;
							continue;
						}
						// ---  2. Insert OR Reuse EmployeeInformation ---
						EmployeeInformation emp;

                        if (!string.IsNullOrWhiteSpace(employeeId) && existingEmployees.TryGetValue(employeeId, out var existingEmp))
                        {
                            // REUSE existing employee record â€” update missing fields to match the incoming Excel row
                            emp = existingEmp;
                            var changed = false;
                            if (string.IsNullOrWhiteSpace(emp.Name) && !string.IsNullOrWhiteSpace(empName)) { emp.Name = empName; changed = true; }
                            if (string.IsNullOrWhiteSpace(emp.PersonalPhoneNumber) && !string.IsNullOrWhiteSpace(phone)) { emp.PersonalPhoneNumber = phone; changed = true; }
                            if (string.IsNullOrWhiteSpace(emp.OfficialPhoneNumber) && !string.IsNullOrWhiteSpace(phone)) { emp.OfficialPhoneNumber = phone; changed = true; }
                            if (string.IsNullOrWhiteSpace(emp.Email) && !string.IsNullOrWhiteSpace(email)) { emp.Email = email; changed = true; }
                            if (changed)
                            {
                                emp.UpdatedAt = now;
                                emp.UpdatedBy = currentUser;
                                _db.EmployeeInformation.Update(emp);
                                await _db.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            // CREATE new employee record
                            emp = new EmployeeInformation
                            {
                                EmployeeCode = employeeId ?? "",
                                Name = empName,
                                PersonalPhoneNumber = phone,
                                OfficialPhoneNumber = phone,
                                Email = email ?? "",
                                CreatedBy = currentUser,
                                UpdatedBy = currentUser,
                                CreatedAt = now,
                                UpdatedAt = now
                            };
                            _db.EmployeeInformation.Add(emp);
                            await _db.SaveChangesAsync(); // flush to get emp.Id

                            // Add to dictionary so subsequent rows in the same excel file can reuse it
                            if (!string.IsNullOrWhiteSpace(employeeId))
                            {
                                existingEmployees[employeeId] = emp;
                            }
                        }

                        // --- 3. Insert Employeelogin ---
                        var login = new Employeelogin
                        {
                            EmployeeInformationID = emp.Id,
                            UserId = user.Id,
                            Role = role,
                            StateId = stateId,
                            RegionId = regionId,
                            HeadquartersId = hqId,
                            ZoneId = zoneId,
                            IsActive = true
                        };
                        _db.Employeelogins.Add(login);

                        existingUserNames.Add(userName);
                        inserted++;
                    }
                    catch (Exception exRow)
                    {
                        _logger.LogWarning(exRow, "Row {Row} parse error", rowNum);
                        AddError("Row Error", $"Row {rowNum}: {exRow.Message}");
                        skipped++;
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

            return Ok(new
            {
                Success = true,
                Inserted = inserted,
                Skipped = skipped,
                GroupedErrors = groupedErrors
            });
        }
        // GET /api/EmployeeBulkUpload/sample-template
        [HttpGet("sample-template")]
        public IActionResult SampleTemplate()
        {
            var headers = new[]
            {
        "EmployeeID", "EmpName", "UserName", "Permission",
        "State", "Region", "HQ", "PhoneNumber", "EmailID", "Designation", "Zone"
    };

            // Sample rows that demonstrate each role tier's location requirement
            var sampleRows = new[]
            {
        // MO/MDO/JMDO -> State + Region + HQ
        new[] { "EMP001", "Ravi Kumar", "ravi.kumar", "MO", "Tamil Nadu", "Chennai Region", "Chennai HQ", "9876543210", "ravi@example.com", "Field Officer", "" },
        // RM/RMD -> State + Region
        new[] { "EMP002", "Priya S", "priya.s", "RM", "Tamil Nadu", "Chennai Region", "", "9876543211", "priya@example.com", "", "" },
        // SMD/SMM -> State only
        new[] { "EMP003", "Arun M", "arun.m", "SMM", "Tamil Nadu", "", "", "9876543212", "arun@example.com", "", "" },
        // AVP -> Zone only
        new[] { "EMP004", "Vikram N", "vikram.n", "AVP", "", "", "", "9876543213", "vikram@example.com", "", "South Zone" },
    };

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Employees");

            // Header row
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#059669");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Sample data rows
            for (int r = 0; r < sampleRows.Length; r++)
                for (int c = 0; c < sampleRows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = sampleRows[r][c];

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            // Instructions sheet so rules stay attached to the file
            var notes = wb.Worksheets.Add("Instructions");
            var lines = new[]
            {
        "Bulk Upload - Employee Instructions",
        "",
        "Columns: EmployeeID, EmpName, UserName, Permission, State, Region, HQ, PhoneNumber, EmailID, Designation, Zone",
        "",
        "PhoneNumber is used as the login password (minimum 6 characters).",
        "Designation is optional. If given, it must match an existing ACTIVE designation name.",
        "",
        "Role -> Required location columns:",
        "  AVP           -> Zone",
        "  SMD, SMM      -> State",
        "  RM, RMD       -> State + Region",
        "  MDO, MO, JMDO -> State + Region + HQ",
        "",
        "IMPORTANT: this upload never creates master data.",
        "Every Zone / State / Region / HQ value used in this file must already exist in master data.",
        "If any is missing, the whole upload is stopped, nothing is saved, and all missing",
        "values are listed so you can create them first and then upload this same file again.",
        "",
        "UserName must be unique - duplicates are skipped.",
    };
            for (int i = 0; i < lines.Length; i++)
            {
                var cell = notes.Cell(i + 1, 1);
                cell.Value = lines[i];
                if (i == 0) { cell.Style.Font.Bold = true; cell.Style.Font.FontSize = 13; }
            }
            notes.Column(1).Width = 90;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Employee_Sample_Template.xlsx");
        }

        private static string NormalizeHeader(string h) => MasterNormalizer.NormalizeHeader(h);


		/// <summary>
		/// Roles accepted by employee bulk upload and the location each one requires:
		/// AVP -> Zone, SMD/SMM -> State, RM/RMD -> State + Region, MDO/MO/JMDO -> State + Region + HQ.
		/// </summary>
		private static bool IsBulkUploadRole(AppRole role) =>
			role == AppRole.AVP ||
			role == AppRole.SMD || role == AppRole.SMM ||
			role == AppRole.RM || role == AppRole.RMD ||
			role == AppRole.MDO || role == AppRole.MO || role == AppRole.JMDO;
		private async Task EnsureRoleAndAssignAsync(UserInfo user, AppRole role)
		{
			var roleName = role.ToString();

			if (!await _roleManager.RoleExistsAsync(roleName))
			{
				var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));

				if (!roleResult.Succeeded)
					throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
			}

			if (!await _userManager.IsInRoleAsync(user, roleName))
			{
				var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);

				if (!addRoleResult.Succeeded)
					throw new Exception(string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
			}
		}
	}
}
