using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
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

        public EmployeeBulkUploadController(AppDbContext db, ILogger<EmployeeBulkUploadController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            var headerRow = worksheet.Row(1);
            var lastHeaderCell = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (lastHeaderCell == 0)
                return BadRequest(new { Success = false, Message = "Empty worksheet or missing header row" });

            var headerMap = new Dictionary<string,int>();
            for (int c = 1; c <= lastHeaderCell; c++)
            {
                var raw = headerRow.Cell(c).GetString();
                var n = NormalizeHeader(raw);
                if (!string.IsNullOrEmpty(n) && !headerMap.ContainsKey(n)) headerMap[n] = c;
            }

            // required header: name
            if (!headerMap.ContainsKey("name") && !headerMap.ContainsKey("employeename") && !headerMap.ContainsKey("employeename"))
            {
                return BadRequest(new { Success = false, Message = "Invalid template. Missing column: Name" });
            }

            var rows = worksheet.RowsUsed().Skip(1);
            var now = DateTime.UtcNow;
            var errors = new List<string>();
            int inserted = 0;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        var name = GetCellString(row, headerMap, "name");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            errors.Add($"Row {row.RowNumber()}: Name empty");
                            continue;
                        }

                        var code = GetCellString(row, headerMap, "employeecode");
                        var personalPhone = GetCellString(row, headerMap, "personalphonenumber");
                        var officialPhone = GetCellString(row, headerMap, "officialphonenumber");
                        var email = GetCellString(row, headerMap, "email");
                        var createdBy = GetCellString(row, headerMap, "createdby");

                        // duplicate check: prefer code, then email
                        if (!string.IsNullOrEmpty(code) && _db.EmployeeInformation.Any(e => e.EmployeeCode.ToLower() == code.ToLower()))
                        {
                            errors.Add($"Row {row.RowNumber()}: EmployeeCode '{code}' already exists, skipped");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(email) && _db.EmployeeInformation.Any(e => e.Email.ToLower() == email.ToLower()))
                        {
                            errors.Add($"Row {row.RowNumber()}: Email '{email}' already exists, skipped");
                            continue;
                        }

                        var ent = new EmployeeInformation
                        {
                            EmployeeCode = string.IsNullOrWhiteSpace(code) ? null ?? "" : code,
                            Name = name,
                            PersonalPhoneNumber = personalPhone,
                            OfficialPhoneNumber = officialPhone,
                            Email = email,
                            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "bulk-upload" : createdBy,
                            UpdatedBy = "bulk-upload",
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        _db.Add(ent);
                        inserted++;
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

            return Ok(new { Success = true, Inserted = inserted, Errors = errors });
        }

        private static string GetCellString(IXLRow row, Dictionary<string,int> headerMap, string key)
        {
            if (headerMap.TryGetValue(key, out var col)) return row.Cell(col).GetString().Trim();
            var lgd = "lgd" + key;
            if (headerMap.TryGetValue(lgd, out col)) return row.Cell(col).GetString().Trim();
            var fms = "fms" + key;
            if (headerMap.TryGetValue(fms, out col)) return row.Cell(col).GetString().Trim();
            return string.Empty;
        }

        private static string NormalizeHeader(string h) => (h ?? string.Empty).Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
    }
}
