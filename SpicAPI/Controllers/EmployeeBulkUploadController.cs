using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
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

            // Pre-load lookup tables (name → id, case-insensitive)
            var stateMap = await _db.States
                .ToDictionaryAsync(s => s.StateName.Trim(), s => s.Id, StringComparer.OrdinalIgnoreCase);

            var regionMap = await _db.Regions
                .ToDictionaryAsync(r => r.RegionName.Trim(), r => r.Id, StringComparer.OrdinalIgnoreCase);

            var hqMap = await _db.Headquarters
                .ToDictionaryAsync(h => h.HeadquarterName.Trim(), h => h.Id, StringComparer.OrdinalIgnoreCase);

            var designationMap = await _db.Designations
    .ToDictionaryAsync(
        d => d.Name.Trim(),
        d => new { d.Id, d.IsActive },
        StringComparer.OrdinalIgnoreCase);

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
                            AddError("Missing Phone", $"Row {rowNum} ({empName}): PhoneNumber is empty — used as password.");
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
                        bool isSmRole = role == AppRole.SMD || role == AppRole.SMM;
                        bool isRmRole = role == AppRole.RM || role == AppRole.RMD;
                        bool isMoRole = role == AppRole.MDO || role == AppRole.MO || role == AppRole.JMDO;

                        if (!isSmRole && !isRmRole && !isMoRole)
                        {
                            AddError("Role Not Allowed", $"Row {rowNum} ({empName}): Role '{role}' cannot be created via bulk upload.");
                            skipped++;
                            continue;
                        }

                        int stateId = 0, regionId = 0, hqId = 0;
                        var currentUser = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                          ?? User?.Identity?.Name
                                          ?? "Unknown";

                        // 1. Resolve State (Strictly required to exist)
                        if (!string.IsNullOrWhiteSpace(stateName))
                        {
                            if (stateMap.TryGetValue(stateName, out var sId))
                            {
                                stateId = sId;
                            }
                            else
                            {
                                AddError("Unknown State", $"Row {rowNum} ({empName}): State '{stateName}' not found in database.");
                            }
                        }

                        // 2. Resolve Region (Auto-create if missing)
                        if (!string.IsNullOrWhiteSpace(regionName))
                        {
                            if (regionMap.TryGetValue(regionName, out var rId))
                            {
                                regionId = rId;
                            }
                            else if (stateId > 0) // Only auto-create if we have a valid State to link it to
                            {
                                var newRegion = new Region
                                {
                                    RegionName = regionName,
                                    StateId = stateId,
                                    IsActive = true,
                                    CreatedAt = now,
                                    UpdatedBy = currentUser
                                };

                                _db.Regions.Add(newRegion);
                                await _db.SaveChangesAsync();

                                regionId = newRegion.Id;
                                regionMap[regionName] = regionId;
                            }
                            else
                            {
                                AddError("Missing/Invalid State", $"Row {rowNum} ({empName}): Cannot auto-create Region '{regionName}' because a valid State is missing.");
                            }
                        }

                        // 3. Resolve Headquarter (Auto-create if missing)
                        if (!string.IsNullOrWhiteSpace(hqName))
                        {
                            if (hqMap.TryGetValue(hqName, out var hId))
                            {
                                hqId = hId;
                            }
                            else if (regionId > 0) // Only auto-create if we have a valid Region to link it to
                            {
                                var newHq = new Headquarter
                                {
                                    HeadquarterName = hqName,
                                    RegionId = regionId,
                                    IsActive = true,
                                    CreatedAt = now,
                                    UpdatedBy = currentUser
                                };

                                _db.Headquarters.Add(newHq);
                                await _db.SaveChangesAsync();

                                hqId = newHq.Id;
                                hqMap[hqName] = hqId;
                            }
                            else
                            {
                                AddError("Missing/Invalid Region", $"Row {rowNum} ({empName}): Cannot auto-create HQ '{hqName}' because a valid Region is missing.");
                            }
                        }

                        // --- Extract All Provided Locations ---
                        //if (!string.IsNullOrWhiteSpace(stateName))
                        //{
                        //    if (stateMap.TryGetValue(stateName, out var sId)) stateId = sId;
                        //    else AddError("Unknown State", $"Row {rowNum} ({empName}): State '{stateName}' not found.");
                        //}

                        //if (!string.IsNullOrWhiteSpace(regionName))
                        //{
                        //    if (regionMap.TryGetValue(regionName, out var rId)) regionId = rId;
                        //    else AddError("Unknown Region", $"Row {rowNum} ({empName}): Region '{regionName}' not found.");
                        //}

                        //if (!string.IsNullOrWhiteSpace(hqName))
                        //{
                        //    if (hqMap.TryGetValue(hqName, out var hId)) hqId = hId;
                        //    else AddError("Unknown HQ", $"Row {rowNum} ({empName}): HQ '{hqName}' not found.");
                        //}

                        // --- Apply Your Specific Rules ---
                        if (isMoRole) // MO, MDO, JMDO -> Requires State, Region, HQ
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
                        if ((isMoRole && (stateId == 0 || regionId == 0 || hqId == 0)) ||
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
                            if (!designationMap.TryGetValue(designationName, out var desig))
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
                            // REUSE existing employee record — update missing fields to match the incoming Excel row
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
                            ZoneId = 0,
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
        "State", "Region", "HQ", "PhoneNumber", "EmailID", "Designation"
    };

            // Sample rows that demonstrate each role tier's location requirement
            var sampleRows = new[]
            {
        // MO/MDO/JMDO -> State + Region + HQ
        new[] { "EMP001", "Ravi Kumar", "ravi.kumar", "MO", "Tamil Nadu", "Chennai Region", "Chennai HQ", "9876543210", "ravi@example.com", "Field Officer" },
        // RM/RMD -> State + Region
        new[] { "EMP002", "Priya S", "priya.s", "RM", "Tamil Nadu", "Chennai Region", "", "9876543211", "priya@example.com", "" },
        // SMD/SMM -> State only
        new[] { "EMP003", "Arun M", "arun.m", "SMM", "Tamil Nadu", "", "", "9876543212", "arun@example.com", "" },
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
        "Required columns: EmployeeID, EmpName, UserName, Permission, State, Region, HQ, PhoneNumber, EmailID, Designation",
        "",
        "PhoneNumber is used as the login password (minimum 6 characters).",
        "Designation is optional. If given, it must match an existing ACTIVE designation name.",
        "",
        "Role -> Required location columns:",
        "  SMD, SMM    -> State",
        "  RM, RMD     -> State + Region",
        "  MDO, MO, JMDO -> State + Region + HQ",
        "",
        "State must already exist in master data.",
        "Region / HQ are auto-created if missing (only when their parent State/Region is valid).",
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

        private static string NormalizeHeader(string h) =>
            (h ?? string.Empty).Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
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