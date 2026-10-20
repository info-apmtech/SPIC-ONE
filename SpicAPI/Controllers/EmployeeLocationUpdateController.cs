using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.MasterData;
using SPIC.Core.Entities;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Temporary SuperAdmin-only update for EXISTING employee login assignments.
    /// Writes ONLY RegionId and HeadquartersId. StateId is read for validation,
    /// but is never modified. No employees, users, logins or masters are inserted.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    [Route("api/[controller]")]
    public class EmployeeLocationUpdateController : ControllerBase
    {
        private const long MaxExcelBytes = 5 * 1024 * 1024;
        private const int MaxExcelDataRows = 500;
        private readonly AppDbContext _db;
        private readonly UserManager<UserInfo> _userManager;
        private readonly ILogger<EmployeeLocationUpdateController> _logger;

        public EmployeeLocationUpdateController(
            AppDbContext db,
            UserManager<UserInfo> userManager,
            ILogger<EmployeeLocationUpdateController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        // Only the SuperAdmin can reach this endpoint; the UI uses it to show the button.
        [HttpGet("access")]
        public IActionResult Access() => Ok(new { Allowed = true });

        [HttpGet("sample-template")]
        public IActionResult SampleTemplate()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("LocationUpdates");
            var headers = new[] { "EmployeeID", "UserName", "Region", "HQ" };
            for (var col = 1; col <= headers.Length; col++)
            {
                var cell = sheet.Cell(1, col);
                cell.Value = headers[col - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B7F69");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Text format preserves EmployeeIDs with leading zeroes / prefixes.
            sheet.Columns("A:D").Style.NumberFormat.Format = "@";
            sheet.Column(1).Width = 24;
            sheet.Column(2).Width = 28;
            sheet.Column(3).Width = 34;
            sheet.Column(4).Width = 34;
            sheet.SheetView.FreezeRows(1);
            sheet.Range("A1:D1").SetAutoFilter();

            var instructions = workbook.Worksheets.Add("Instructions");
            var lines = new[]
            {
                "TEMPORARY SUPERADMIN REGION / HQ UPDATE - EXISTING LOGINS ONLY",
                "Excel headers: EmployeeID, UserName, Region, HQ.",
                "EmployeeID + UserName must match exactly one existing employee login.",
                "State is NOT uploaded or changed. The employee login's existing StateId is used.",
                "Region must exist under the existing State. HQ must exist under the selected Region.",
                "Blank Region and HQ CLEAR both assignments for SMD/SMM (when allowed).",
                "For RM/RMD, Region is mandatory; HQ is optional and blank clears HQ.",
                "For MO/MDO/JMDO, both Region and HQ are mandatory.",
                "If you want to keep an assigned Region or HQ, enter its existing name in Excel.",
                "The entire upload is rejected if any row is invalid. Preview before Apply.",
                "This feature NEVER creates records or changes State, Role, Zone, Name, Password, etc.",
                "Only .xlsx files, maximum 500 data rows and 5 MB."
            };
            for (var i = 0; i < lines.Length; i++)
            {
                instructions.Cell(i + 1, 1).Value = lines[i];
                if (i == 0) instructions.Cell(i + 1, 1).Style.Font.Bold = true;
            }
            instructions.Column(1).Width = 112;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Employee_Region_HQ_Update_Template.xlsx");
        }

        [HttpPost("preview")]
        [RequestSizeLimit(MaxExcelBytes + 16 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Preview(IFormFile file, CancellationToken cancellationToken)
        {
            var error = CheckFile(file);
            if (error != null) return BadRequest(new { Success = false, Message = error });

            try
            {
                var plan = await BuildPlanAsync(file, cancellationToken);
                return Ok(new
                {
                    Success = plan.Errors.Count == 0,
                    Message = plan.Errors.Count == 0
                        ? "Preview ready. No database records have changed. State stays unchanged."
                        : "Preview found errors. Nothing has been changed.",
                    plan.Errors,
                    Rows = plan.Rows.Select(r => new
                    {
                        r.ExcelRow,
                        r.EmployeeID,
                        r.UserName,
                        r.Role,
                        r.StateId,
                        r.OldRegionId,
                        r.NewRegionId,
                        r.OldHqId,
                        r.NewHqId,
                        r.NewRegionName,
                        r.NewHqName,
                        Status = r.HasChanges ? "Update" : "Unchanged"
                    }).ToList(),
                    Changed = plan.Rows.Count(r => r.HasChanges),
                    Unchanged = plan.Rows.Count(r => !r.HasChanges),
                    PreviewHash = plan.Errors.Count == 0 ? PlanHash(plan.Rows) : ""
                });
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
            {
                return BadRequest(new { Success = false, Message = "Unable to read Excel: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Employee Region/HQ preview failed");
                return StatusCode(500, new { Success = false, Message = "Region/HQ preview failed." });
            }
        }

        [HttpPost("apply")]
        [RequestSizeLimit(MaxExcelBytes + 16 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Apply(
            [FromForm] EmployeeLocationUpdateApplyDto request,
            CancellationToken cancellationToken)
        {
            var file = request.File;
            var previewHash = request.PreviewHash;
            var error = CheckFile(file);
            if (error != null) return BadRequest(new { Success = false, Message = error });
            if (string.IsNullOrWhiteSpace(previewHash))
                return BadRequest(new { Success = false, Message = "Preview the file before applying." });

            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

                // Revalidate the file and current DB state inside the transaction.
                var plan = await BuildPlanAsync(file, cancellationToken);
                if (plan.Errors.Count > 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Nothing saved. Correct all row errors and preview again.",
                        plan.Errors
                    });
                }

                if (!string.Equals(previewHash, PlanHash(plan.Rows), StringComparison.OrdinalIgnoreCase))
                {
                    await tx.RollbackAsync(cancellationToken);
                    return Conflict(new
                    {
                        Success = false,
                        Message = "The file or database assignments changed since Preview. Please preview again."
                    });
                }

                var updated = 0;
                foreach (var row in plan.Rows.Where(r => r.HasChanges))
                {
                    // IMPORTANT: these are the ONLY two entity fields modified by this feature.
                    row.Login.RegionId = row.NewRegionId;
                    row.Login.HeadquartersId = row.NewHqId;
                    updated++;
                }

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Employee Region/HQ update committed by {Actor}; updated={Updated}; unchanged={Unchanged}",
                    User?.Identity?.Name ?? "SuperAdmin", updated, plan.Rows.Count - updated);

                return Ok(new
                {
                    Success = true,
                    Message = $"Updated {updated} existing login(s). StateId was not modified.",
                    Updated = updated,
                    Unchanged = plan.Rows.Count - updated
                });
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
            {
                return BadRequest(new { Success = false, Message = "Unable to read Excel: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Employee Region/HQ apply failed; no successful batch confirmation");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Region/HQ update failed. Check logs and DB before retrying."
                });
            }
        }

        private static string? CheckFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Select a non-empty Excel file.";
            if (file.Length > MaxExcelBytes) return "Excel exceeds the 5 MB limit.";
            if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                return "Only .xlsx files are supported.";
            return null;
        }

        private static string Key(string? value) => (value ?? "").Trim().ToUpperInvariant();

        private async Task<UpdatePlan> BuildPlanAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var plan = new UpdatePlan();
            using var input = file.OpenReadStream();
            using var workbook = new XLWorkbook(input);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                plan.Errors.Add("Excel workbook contains no worksheet.");
                return plan;
            }

            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastHeader = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (var col = 1; col <= lastHeader; col++)
            {
                var header = MasterNormalizer.NormalizeHeader(sheet.Cell(1, col).GetString());
                if (header.Length > 0 && !headers.ContainsKey(header))
                    headers.Add(header, col);
            }

            foreach (var name in new[] { "employeeid", "username", "region", "hq" })
                if (!headers.ContainsKey(name))
                    plan.Errors.Add($"Missing required Excel header: {name}.");
            if (plan.Errors.Count > 0) return plan;

            string Cell(IXLRow row, string name) => row.Cell(headers[name]).GetString().Trim();
            var dataRows = sheet.RowsUsed().Skip(1).ToList();
            if (dataRows.Count == 0)
            {
                plan.Errors.Add("No employee rows found in Excel.");
                return plan;
            }
            if (dataRows.Count > MaxExcelDataRows)
            {
                plan.Errors.Add($"Excel has {dataRows.Count} rows; limit is {MaxExcelDataRows}.");
                return plan;
            }

            var inputs = new List<ExcelRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in dataRows)
            {
                var employeeId = Cell(row, "employeeid");
                var userName = Cell(row, "username");
                var region = Cell(row, "region");
                var hq = Cell(row, "hq");
                var rowNum = row.RowNumber();

                if (employeeId.Length == 0 || userName.Length == 0)
                {
                    plan.Errors.Add($"Row {rowNum}: EmployeeID and UserName are required.");
                    continue;
                }
                var key = Key(employeeId) + "\u001f" + Key(userName);
                if (!seen.Add(key))
                {
                    plan.Errors.Add($"Row {rowNum}: Duplicate EmployeeID / UserName ({employeeId} / {userName}).");
                    continue;
                }
                inputs.Add(new ExcelRow(rowNum, employeeId, userName, region, hq));
            }
            if (plan.Errors.Count > 0) return plan;

            var masters = await MasterLookup.LoadAsync(_db);
            var employeeCodes = inputs.Select(i => Key(i.EmployeeId)).Distinct().ToList();
            var employees = await _db.EmployeeInformation
                .AsNoTracking()
                .Where(e => e.EmployeeCode != null && employeeCodes.Contains(e.EmployeeCode.Trim().ToUpper()))
                .Select(e => new { e.Id, e.EmployeeCode })
                .ToListAsync(cancellationToken);
            var employeeMap = employees.GroupBy(e => Key(e.EmployeeCode))
                .ToDictionary(g => g.Key, g => g.ToList());

            var employeeIds = employees.Select(e => e.Id).ToList();
            var logins = await _db.Employeelogins
                .Where(l => employeeIds.Contains(l.EmployeeInformationID))
                .ToListAsync(cancellationToken);
            var loginMap = logins.ToLookup(l => l.EmployeeInformationID);
            var userIds = logins.Select(l => l.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct().ToList();
            var users = await _userManager.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(cancellationToken);
            var usernames = users.ToDictionary(u => u.Id, u => Key(u.UserName));

            foreach (var entry in inputs)
            {
                if (!employeeMap.TryGetValue(Key(entry.EmployeeId), out var employeeMatches) || employeeMatches.Count == 0)
                {
                    plan.Errors.Add($"Row {entry.RowNumber}: EmployeeID '{entry.EmployeeId}' not found. No records will be created.");
                    continue;
                }
                if (employeeMatches.Count != 1)
                {
                    plan.Errors.Add($"Row {entry.RowNumber}: EmployeeID '{entry.EmployeeId}' is duplicated in DB; fix manually.");
                    continue;
                }

                var loginMatches = loginMap[employeeMatches[0].Id]
                    .Where(l => !string.IsNullOrWhiteSpace(l.UserId) &&
                        usernames.TryGetValue(l.UserId, out var name) &&
                        name == Key(entry.UserName))
                    .ToList();
                if (loginMatches.Count != 1)
                {
                    plan.Errors.Add($"Row {entry.RowNumber}: Expected one login for {entry.EmployeeId} / {entry.UserName}; found {loginMatches.Count}.");
                    continue;
                }
                var login = loginMatches[0];

                bool needsRegion = login.Role == AppRole.RM || login.Role == AppRole.RMD ||
                                   login.Role == AppRole.MO || login.Role == AppRole.MDO ||
                                   login.Role == AppRole.JMDO;
                bool needsHq = login.Role == AppRole.MO || login.Role == AppRole.MDO ||
                               login.Role == AppRole.JMDO;
                bool eligible = needsRegion || login.Role == AppRole.SMD || login.Role == AppRole.SMM;
                if (!eligible)
                {
                    plan.Errors.Add($"Row {entry.RowNumber}: Role '{login.Role}' does not support Region/HQ bulk editing.");
                    continue;
                }

                // State is read from the EXISTING login, not Excel. NEVER write StateId.
                var currentStateId = Convert.ToInt32(login.StateId);
                if (currentStateId <= 0)
                {
                    plan.Errors.Add($"Row {entry.RowNumber}: Existing login has no StateId. Fix State in Employee Registration first.");
                    continue;
                }

                int newRegionId = 0;
                if (entry.Region.Length == 0)
                {
                    if (needsRegion)
                    {
                        plan.Errors.Add($"Row {entry.RowNumber}: Region cannot be blank for role {login.Role}.");
                        continue;
                    }
                }
                else if (!masters.TryGetRegion(entry.Region, currentStateId, out newRegionId) || newRegionId == 0)
                {
                    plan.Errors.Add($"Row {entry.RowNumber}: Region '{entry.Region}' is not under existing StateId {currentStateId}.");
                    continue;
                }

                int newHqId = 0;
                if (entry.Hq.Length == 0)
                {
                    if (needsHq)
                    {
                        plan.Errors.Add($"Row {entry.RowNumber}: HQ cannot be blank for role {login.Role}.");
                        continue;
                    }
                }
                else
                {
                    if (newRegionId == 0)
                    {
                        plan.Errors.Add($"Row {entry.RowNumber}: HQ '{entry.Hq}' requires a Region in the same Excel row.");
                        continue;
                    }
                    if (!masters.TryGetHeadquarter(entry.Hq, newRegionId, out newHqId) || newHqId == 0)
                    {
                        plan.Errors.Add($"Row {entry.RowNumber}: HQ '{entry.Hq}' is not under Region '{entry.Region}' for StateId {currentStateId}.");
                        continue;
                    }
                }

                plan.Rows.Add(new UpdateRow
                {
                    ExcelRow = entry.RowNumber,
                    EmployeeID = entry.EmployeeId,
                    UserName = entry.UserName,
                    Role = login.Role.ToString(),
                    Login = login,
                    StateId = currentStateId,
                    OldRegionId = Convert.ToInt32(login.RegionId),
                    NewRegionId = newRegionId,
                    OldHqId = Convert.ToInt32(login.HeadquartersId),
                    NewHqId = newHqId,
                    NewRegionName = entry.Region.Length == 0 ? "(none)" : entry.Region,
                    NewHqName = entry.Hq.Length == 0 ? "(none)" : entry.Hq
                });
            }

            return plan;
        }

        private static string PlanHash(IEnumerable<UpdateRow> rows)
        {
            // Preview binds input, resolved IDs AND old state so stale data cannot be applied.
            var payload = string.Join("\n", rows.OrderBy(r => r.ExcelRow).Select(r =>
                $"{r.ExcelRow}|{Key(r.EmployeeID)}|{Key(r.UserName)}|{r.Login.Id}|{r.Role}|" +
                $"{r.StateId}|{r.OldRegionId}|{r.OldHqId}|{r.NewRegionId}|{r.NewHqId}"));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        }

        private sealed record ExcelRow(int RowNumber, string EmployeeId, string UserName, string Region, string Hq);

        private sealed class UpdatePlan
        {
            public List<UpdateRow> Rows { get; } = new();
            public List<string> Errors { get; } = new();
        }

        private sealed class UpdateRow
        {
            public int ExcelRow { get; init; }
            public string EmployeeID { get; init; } = "";
            public string UserName { get; init; } = "";
            public string Role { get; init; } = "";
            public Employeelogin Login { get; init; } = null!;
            public int StateId { get; init; }
            public int OldRegionId { get; init; }
            public int NewRegionId { get; init; }
            public int OldHqId { get; init; }
            public int NewHqId { get; init; }
            public string NewRegionName { get; init; } = "";
            public string NewHqName { get; init; } = "";
            public bool HasChanges => OldRegionId != NewRegionId || OldHqId != NewHqId;
        }
    }

    public class EmployeeLocationUpdateApplyDto
    {
        public IFormFile File { get; set; } = null!;
        public string PreviewHash { get; set; } = string.Empty;
    }
}

