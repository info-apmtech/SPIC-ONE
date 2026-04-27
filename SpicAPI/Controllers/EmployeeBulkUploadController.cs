using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeBulkUploadController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<EmployeeBulkUploadController> _logger;
        private readonly UserManager<UserInfo> _userManager;

        public EmployeeBulkUploadController(
            AppDbContext db,
            ILogger<EmployeeBulkUploadController> logger,
            UserManager<UserInfo> userManager)
        {
            _db = db;
            _logger = logger;
            _userManager = userManager;
        }

        // POST /api/employeebulkupload/bulk-upload
        // Excel columns: EmployeeID | EmpName | UserName | Permission | State | Region | HQ | PhoneNumber | EmailID
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

            // Grouped errors: category → list of messages
            var groupedErrors = new Dictionary<string, List<string>>();
            void AddError(string group, string msg)
            {
                if (!groupedErrors.ContainsKey(group)) groupedErrors[group] = new();
                groupedErrors[group].Add(msg);
            }

            int inserted = 0;
            int skipped = 0;

            // Collect existing employee codes and usernames to avoid repeated DB hits
            var existingCodes = await _db.EmployeeInformation
                .Where(e => e.EmployeeCode != null && e.EmployeeCode != "")
                .Select(e => e.EmployeeCode)
                .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

            var existingUserNames = await _db.Employeelogins
                .Select(l => l.UserId)
                .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    int rowNum = row.RowNumber();
                    try
                    {
                        // --- Read columns ---
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

                        // Duplicate checks
                        if (!string.IsNullOrWhiteSpace(employeeId) && existingCodes.Contains(employeeId))
                        {
                            AddError("Duplicate EmployeeID", $"Row {rowNum} ({empName}): EmployeeID '{employeeId}' already exists.");
                            skipped++;
                            continue;
                        }

                        if (existingUserNames.Contains(userName))
                        {
                            AddError("Duplicate UserName", $"Row {rowNum} ({empName}): UserName '{userName}' already exists.");
                            skipped++;
                            continue;
                        }

                        // Parse role (Permission column)
                        if (!Enum.TryParse<AppRole>(permission, ignoreCase: true, out var role))
                        {
                            AddError("Invalid Role", $"Row {rowNum} ({empName}): Permission '{permission}' is not a valid role.");
                            skipped++;
                            continue;
                        }

                        // Only specific roles are allowed via bulk upload
                        // SMD, SMM  → State only
                        // RM, RMD   → State + Region
                        // MDO, MO, JMDO → HQ only
                        bool isStateRole = role == AppRole.SMD || role == AppRole.SMM;
                        bool isRegionRole = role == AppRole.RM || role == AppRole.RMD;
                        bool isHqRole = role == AppRole.MDO || role == AppRole.MO || role == AppRole.JMDO;

                        if (!isStateRole && !isRegionRole && !isHqRole)
                        {
                            AddError("Role Not Allowed", $"Row {rowNum} ({empName}): Role '{role}' cannot be created via bulk upload.");
                            skipped++;
                            continue;
                        }

                        // Resolve location IDs based on role rules
                        int stateId = 0, regionId = 0, hqId = 0;

                        if (isStateRole || isRegionRole)
                        {
                            if (string.IsNullOrWhiteSpace(stateName))
                                AddError("Missing State", $"Row {rowNum} ({empName}): State is required for role '{role}'.");
                            else if (!stateMap.TryGetValue(stateName, out stateId))
                                AddError("Unknown State", $"Row {rowNum} ({empName}): State '{stateName}' not found — set to 0.");
                        }

                        if (isRegionRole)
                        {
                            if (string.IsNullOrWhiteSpace(regionName))
                                AddError("Missing Region", $"Row {rowNum} ({empName}): Region is required for role '{role}'.");
                            else if (!regionMap.TryGetValue(regionName, out regionId))
                                AddError("Unknown Region", $"Row {rowNum} ({empName}): Region '{regionName}' not found — set to 0.");
                        }

                        if (isHqRole)
                        {
                            if (string.IsNullOrWhiteSpace(hqName))
                                AddError("Missing HQ", $"Row {rowNum} ({empName}): HQ is required for role '{role}'.");
                            else if (!hqMap.TryGetValue(hqName, out hqId))
                                AddError("Unknown HQ", $"Row {rowNum} ({empName}): HQ '{hqName}' not found — set to 0.");
                        }

                        // ---  Create ASP.NET Core Identity User First ---
                        var user = new UserInfo
                        {
                            UserName = userName,
                            Password = phone,
                            Email = email ?? "",
                            PhoneNumber = phone,
                            Name = empName,
                            Role = role,
                            CreatedAt = now,
                            CreatedBy = "bulk-upload",
                            UpdatedAt = now,
                            UpdatedBy = "bulk-upload",
                            IsActive = true
                        };

                        // Use phone number as the initial password
                        var identityResult = await _userManager.CreateAsync(user, phone);

                        if (!identityResult.Succeeded)
                        {
                            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                            AddError("Identity Error", $"Row {rowNum} ({empName}): Failed to create login - {errors}");
                            skipped++;
                            continue;
                        }

                        // ---  Insert EmployeeInformation ---
                        var emp = new EmployeeInformation
                        {
                            EmployeeCode = employeeId ?? "",
                            Name = empName,
                            PersonalPhoneNumber = phone,
                            OfficialPhoneNumber = phone,
                            Email = email ?? "",
                            CreatedBy = "bulk-upload",
                            UpdatedBy = "bulk-upload",
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        _db.EmployeeInformation.Add(emp);
                        await _db.SaveChangesAsync(); // flush to get emp.Id

                        // --- Insert Employeelogin (phone number as password via UserId) ---
                        var login = new Employeelogin
                        {
                            EmployeeInformationID = emp.Id,
                            UserId = userName,   // username
                            Role = role,
                            StateId = stateId,
                            RegionId = regionId,
                            HeadquartersId = hqId,
                            ZoneId = 0,
                            IsActive = true
                        };
                        _db.Employeelogins.Add(login);

                        // Track to catch duplicates within the same file
                        existingCodes.Add(employeeId ?? "");
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

        private static string NormalizeHeader(string h) =>
            (h ?? string.Empty).Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
    }
}