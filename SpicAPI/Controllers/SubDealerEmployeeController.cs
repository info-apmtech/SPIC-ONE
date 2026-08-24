using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// CRUD endpoints for the Sub Dealer beneficiary master.
	/// Admin only - non-admin users must never reach this master.
	/// </summary>
	[Authorize(Roles = "Admin,CorporateAdmin")]
	[ApiController]
	[Route("api/[controller]")]
	public class SubDealerBeneficiaryController(IGenericRepository<SubDealerBeneficiary> repo)
		: GenericCrudController<SubDealerBeneficiary>(repo);

	/// <summary>
	/// CRUD endpoints for the Approved Employee beneficiary master.
	/// Admin only - non-admin users must never reach this master.
	/// </summary>
	[Authorize(Roles = "Admin,CorporateAdmin")]
	[ApiController]
	[Route("api/[controller]")]
	public class EmployeeBeneficiaryController(IGenericRepository<EmployeeBeneficiary> repo)
		: GenericCrudController<EmployeeBeneficiary>(repo);

	/// <summary>
	/// Bulk upload / template download for the Sub Dealer and Employee masters,
	/// plus the dealer-scoped read endpoints consumed by the SDWA Welfare Application.
	/// Sub Dealer and Employee logic is kept separate internally (type = subdealer | employee).
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class SubDealerEmployeeController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly ILogger<SubDealerEmployeeController> _logger;

		public SubDealerEmployeeController(AppDbContext db, ILogger<SubDealerEmployeeController> logger)
		{
			_db = db;
			_logger = logger;
		}

		// =====================================================================
		// BULK UPLOAD  (LocationBulkUploadController pattern)
		// =====================================================================

		// POST /api/subdealeremployee/bulk-upload?type=subdealer|employee
		[Authorize(Roles = "Admin,CorporateAdmin")]
		[HttpPost("bulk-upload")]
		public async Task<IActionResult> BulkUpload([FromQuery] string type, IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { Success = false, Message = "No file uploaded" });

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (ext != ".xlsx" && ext != ".xls")
				return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

			var t = (type ?? "").Trim().ToLowerInvariant();
			if (t != "subdealer" && t != "sub-dealer" && t != "sub_dealer" && t != "employee")
				return BadRequest(new { Success = false, Message = "Unknown type. Use subdealer or employee" });

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

			// required columns - must match the downloaded template header structure exactly
			string[] requiredHeaders = t switch
			{
				"subdealer" or "sub-dealer" or "sub_dealer" => new[]
				{
					"beneficiaryid", "dealercode", "maindealerfirmname", "hq", "branchdistrict",
					"subdealercode", "subdealername", "subdealerdistrict", "nomineename",
					"beneficiaryname", "dob", "relationship"
				},
				_ => new[]
				{
					"beneficiaryid", "dealercode", "employeename", "beneficiaryname",
					"dob", "relationship", "maritalstatus", "educationalqualification"
				}
			};

			var missingList = requiredHeaders
				.Where(h => !headerMap.ContainsKey(h))
				.Select(PrettyHeader)
				.ToList();

			if (missingList.Any())
				return BadRequest(new { Success = false, Message = "Invalid template. Missing columns", Missing = missingList });

			var rows = worksheet.RowsUsed().Skip(1).ToList();

			// â”€â”€ Pre-load phase: reference data in memory before the loop â”€â”€
			// Existing business keys ("dealerCode|key") so DB duplicates are skipped
			var existingSubDealerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var sd in _db.SubDealerBeneficiaries
				.Select(s => new { s.DealerCode, s.SubDealerCode }).AsEnumerable())
				existingSubDealerKeys.Add($"{sd.DealerCode}|{sd.SubDealerCode}");

			var existingEmployeeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var e in _db.EmployeeBeneficiaries
				.Select(e => new { e.DealerCode, e.EmployeeName }).AsEnumerable())
				existingEmployeeKeys.Add($"{e.DealerCode}|{e.EmployeeName}");

			// Valid dealer codes (same resolution used across the app: SPICCode ?? DealerCode)
			var validDealerCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var d in _db.DealerRegistrations
				.Where(d => d.DealerCode != null || d.SPICCode != null)
				.Select(d => new { d.DealerCode, d.SPICCode }).AsEnumerable())
			{
				if (!string.IsNullOrWhiteSpace(d.SPICCode)) validDealerCodes.Add(d.SPICCode.Trim());
				if (!string.IsNullOrWhiteSpace(d.DealerCode)) validDealerCodes.Add(d.DealerCode.Trim());
			}

			// In-batch duplicate tracking (distinct error group from DB-existing duplicates)
			var batchSubDealerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var batchEmployeeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var now = DateTime.UtcNow;
			int insertedCount = 0;

			using var tx = await _db.Database.BeginTransactionAsync();
			try
			{
				foreach (var row in rows)
				{
					try
					{
						var beneficiaryIdRaw = GetCellString(row, headerMap, "beneficiaryid");
						long? beneficiaryId = null;
						if (!string.IsNullOrEmpty(beneficiaryIdRaw))
						{
							if (long.TryParse(beneficiaryIdRaw, out var bid)) beneficiaryId = bid;
							else
							{
								AddGrouped("Invalid BeneficiaryID", $"Row {row.RowNumber()}: '{beneficiaryIdRaw}'");
								continue;
							}
						}

						var dobParsed = ParseDateCell(row, headerMap, "dob");
						if (!dobParsed.IsValid)
						{
							AddGrouped("Invalid DOB", $"Row {row.RowNumber()}: '{GetCellString(row, headerMap, "dob")}' is not a valid date");
							continue;
						}
						var dobValue = dobParsed.Value;

						if (t == "subdealer" || t == "sub-dealer" || t == "sub_dealer")
						{
							var dealerCode = GetCellString(row, headerMap, "dealercode");
							var mainDealerFirmName = GetCellString(row, headerMap, "maindealerfirmname");
							var subDealerCode = GetCellString(row, headerMap, "subdealercode");
							var subDealerName = GetCellString(row, headerMap, "subdealername");
							var beneficiaryName = GetCellString(row, headerMap, "beneficiaryname");

							if (string.IsNullOrEmpty(dealerCode)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: DealerCode is required"); continue; }
							if (string.IsNullOrEmpty(mainDealerFirmName)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: MainDealerFirmName is required"); continue; }
							if (string.IsNullOrEmpty(subDealerCode)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: SubDealerCode is required"); continue; }
							if (string.IsNullOrEmpty(subDealerName)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: SubDealerName is required"); continue; }
							if (string.IsNullOrEmpty(beneficiaryName)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: BeneficiaryName is required"); continue; }

							if (!validDealerCodes.Contains(dealerCode))
							{
								AddGrouped($"DealerCode '{dealerCode}' not found in database", $"Row {row.RowNumber()}");
								continue;
							}

							var key = $"{dealerCode}|{subDealerCode}";
							if (existingSubDealerKeys.Contains(key))
							{
								AddGrouped("Already exists in database", $"'{subDealerCode}' of dealer '{dealerCode}' (Row {row.RowNumber()})");
								continue;
							}
							if (!batchSubDealerKeys.Add(key))
							{
								AddGrouped("Duplicated in this file", $"'{subDealerCode}' of dealer '{dealerCode}' (Row {row.RowNumber()})");
								continue;
							}

							_db.SubDealerBeneficiaries.Add(new SubDealerBeneficiary
							{
								BeneficiaryId = beneficiaryId,
								DealerCode = dealerCode,
								MainDealerFirmName = mainDealerFirmName,
								HQ = NullIfEmpty(GetCellString(row, headerMap, "hq")),
								BranchDistrict = NullIfEmpty(GetCellString(row, headerMap, "branchdistrict")),
								SubDealerCode = subDealerCode,
								SubDealerName = subDealerName,
								SubDealerDistrict = NullIfEmpty(GetCellString(row, headerMap, "subdealerdistrict")),
								NomineeName = NullIfEmpty(GetCellString(row, headerMap, "nomineename")),
								BeneficiaryName = beneficiaryName,
								DOB = dobValue?.Date,
								Relationship = NullIfEmpty(GetCellString(row, headerMap, "relationship")),
								IsActive = true,
								CreatedAt = now,
								UpdatedAt = now,
								UpdatedBy = "bulk-upload"
							});
							insertedCount++;
						}
						else
						{
							var dealerCode = GetCellString(row, headerMap, "dealercode");
							var employeeName = GetCellString(row, headerMap, "employeename");
							var beneficiaryName = GetCellString(row, headerMap, "beneficiaryname");

							if (string.IsNullOrEmpty(dealerCode)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: DealerCode is required"); continue; }
							if (string.IsNullOrEmpty(employeeName)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: EmployeeName is required"); continue; }
							if (string.IsNullOrEmpty(beneficiaryName)) { AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: BeneficiaryName is required"); continue; }

							if (!validDealerCodes.Contains(dealerCode))
							{
								AddGrouped($"DealerCode '{dealerCode}' not found in database", $"Row {row.RowNumber()}");
								continue;
							}

							var key = $"{dealerCode}|{employeeName}";
							if (existingEmployeeKeys.Contains(key))
							{
								AddGrouped("Already exists in database", $"'{employeeName}' of dealer '{dealerCode}' (Row {row.RowNumber()})");
								continue;
							}
							if (!batchEmployeeKeys.Add(key))
							{
								AddGrouped("Duplicated in this file", $"'{employeeName}' of dealer '{dealerCode}' (Row {row.RowNumber()})");
								continue;
							}

							_db.EmployeeBeneficiaries.Add(new EmployeeBeneficiary
							{
								BeneficiaryId = beneficiaryId,
								DealerCode = dealerCode,
								EmployeeName = employeeName,
								BeneficiaryName = beneficiaryName,
								DOB = dobValue?.Date,
								Relationship = NullIfEmpty(GetCellString(row, headerMap, "relationship")),
								MaritalStatus = NullIfEmpty(GetCellString(row, headerMap, "maritalstatus")),
								EducationalQualification = NullIfEmpty(GetCellString(row, headerMap, "educationalqualification")),
								IsActive = true,
								CreatedAt = now,
								UpdatedAt = now,
								UpdatedBy = "bulk-upload"
							});
							insertedCount++;
						}
					}
					catch (Exception exRow)
					{
						_logger.LogWarning(exRow, "SubDealer/Employee bulk upload row parse error");
						AddGrouped("Parse errors", $"Row {row.RowNumber()}: {exRow.Message}");
					}
				}
				await _db.SaveChangesAsync();
				await tx.CommitAsync();
			}
			catch (Exception ex)
			{
				await tx.RollbackAsync();
				_logger.LogError(ex, "SubDealer/Employee bulk upload failed");
				return StatusCode(500, new { Success = false, Message = "Bulk upload failed", Error = ex.Message });
			}

			var totalSkipped = groupedErrors.Values.Sum(v => v.Count);
			return Ok(new
			{
				Success = true,
				Message = totalSkipped > 0
					? $"Upload completed. {insertedCount} row(s) inserted, {totalSkipped} skipped."
					: $"Upload completed successfully. All {insertedCount} row(s) inserted.",
				InsertedCount = insertedCount,
				GroupedErrors = groupedErrors,
				TotalSkipped = totalSkipped
			});
		}

		// GET /api/subdealeremployee/sample-template?type=subdealer|employee
		[Authorize(Roles = "Admin,CorporateAdmin")]
		[HttpGet("sample-template")]
		public IActionResult SampleTemplate([FromQuery] string type)
		{
			var t = (type ?? "").Trim().ToLowerInvariant();

			(string Header, string Sample)[] columns = t switch
			{
				"subdealer" or "sub-dealer" or "sub_dealer" => new[]
				{
					("BeneficiaryID", "1001"),
					("DealerCode", "D1001"),
					("MainDealerFirmName", "Sri Amman Agro Agencies"),
					("HQ", "Trichy"),
					("Branch&District", "Trichy/Trichy"),
					("SubDealerCode", "SD2001"),
					("SubDealerName", "Murugan Traders"),
					("SubDealerDistrict", "Trichy"),
					("NomineeName", "Kala Murugan"),
					("BeneficiaryName", "Kala Murugan"),
					("DOB", "1988-05-14"),
					("Relationship", "Spouse")
				},
				"employee" => new[]
				{
					("BeneficiaryID", "2001"),
					("DealerCode", "D1001"),
					("EmployeeName", "Ravi Kumar"),
					("BeneficiaryName", "Lakshmi Ravi"),
					("DOB", "1990-02-21"),
					("Relationship", "Spouse"),
					("MaritalStatus", "Married"),
					("EducationalQualification", "B.Sc Agriculture")
				},
				_ => Array.Empty<(string, string)>()
			};

			if (columns.Length == 0)
				return BadRequest(new { Success = false, Message = "Unknown type. Use subdealer or employee" });

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

			return File(bytes,
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				$"{t}_Sample_Template.xlsx");
		}

		// =====================================================================
		// DEALER-SCOPED READS for the SDWA Welfare Application
		// =====================================================================

		// GET /api/subdealeremployee/my-sub-dealers
		// Returns ONLY the active sub dealers belonging to the logged-in dealer.
		[HttpGet("my-sub-dealers")]
		public async Task<ActionResult<List<SubDealerDto>>> GetMySubDealers()
		{
			var dealerCode = await ResolveCurrentDealerCodeAsync();
			if (string.IsNullOrEmpty(dealerCode))
				return Ok(new List<SubDealerDto>());

			var subDealers = await _db.SubDealerBeneficiaries
				.AsNoTracking()
				.Where(sd => sd.IsActive && sd.DealerCode == dealerCode)
				.OrderBy(sd => sd.SubDealerName)
				.Select(sd => new SubDealerDto
				{
					Id = sd.Id,
					SubDealerCode = sd.SubDealerCode,
					FirmName = sd.SubDealerName,
					NomineeName = sd.NomineeName,
					BeneficiaryName = sd.BeneficiaryName,
					DOB = sd.DOB,
					Relationship = sd.Relationship
				})
				.ToListAsync();

			return Ok(subDealers);
		}

		// GET /api/subdealeremployee/my-employees
		// Returns ONLY the active employees belonging to the logged-in dealer.
		[HttpGet("my-employees")]
		public async Task<ActionResult<List<EmployeeDto>>> GetMyEmployees()
		{
			var dealerCode = await ResolveCurrentDealerCodeAsync();
			if (string.IsNullOrEmpty(dealerCode))
				return Ok(new List<EmployeeDto>());

			var employees = await _db.EmployeeBeneficiaries
				.AsNoTracking()
				.Where(e => e.IsActive && e.DealerCode == dealerCode)
				.OrderBy(e => e.EmployeeName)
				.Select(e => new EmployeeDto
				{
					Id = e.Id,
					EmployeeName = e.EmployeeName,
					EmployeeCode = e.DealerCode,
					BeneficiaryName = e.BeneficiaryName,
					DOB = e.DOB,
					Relationship = e.Relationship
				})
				.ToListAsync();

			return Ok(employees);
		}

		// Resolves the logged-in dealer's code the same way as SDWADashboard/Welfare controllers:
		// NameIdentifier claim -> Users table -> DealerRegistrations.UserTableId -> SPICCode ?? DealerCode
		private async Task<string?> ResolveCurrentDealerCodeAsync()
		{
			var userIdStr =
				User.FindFirstValue(ClaimTypes.NameIdentifier)
				?? User.FindFirstValue("sub");

			if (string.IsNullOrWhiteSpace(userIdStr))
				return null;

			var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userIdStr);
			if (user == null)
				return null;

			var dealer = await _db.DealerRegistrations
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.UserTableId == user.Id);

			var code = dealer?.SPICCode ?? dealer?.DealerCode;
			return string.IsNullOrWhiteSpace(code) ? null : code.Trim();
		}

		// =====================================================================
		// HELPERS (LocationBulkUploadController pattern)
		// =====================================================================

		// Parses the DOB column. Empty cell -> (true, null) because DOB is optional;
		// unparseable text -> (false, null) so the row is reported, never silently inserted.
		private static (bool IsValid, DateTime? Value) ParseDateCell(IXLRow row, Dictionary<string, int> headerMap, string key)
		{
			var raw = GetCellString(row, headerMap, key);
			if (string.IsNullOrEmpty(raw)) return (true, null);

			// Excel-native date cell
			if (TryGetHeaderColumn(headerMap, key, out var col))
			{
				var cell = row.Cell(col);
				if (cell.DataType == XLDataType.DateTime && cell.TryGetValue(out DateTime excelDate))
					return (true, excelDate);
			}

			string[] formats =
			{
				"yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy",
				"dd-MM-yyyy", "dd.MM.yyyy", "dd-MMM-yyyy", "dd MMM yyyy"
			};
			if (DateTime.TryParseExact(raw.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
				return (true, parsed);

			if (DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
				return (true, parsed);

			return (false, null);
		}

		private static bool TryGetHeaderColumn(Dictionary<string, int> headerMap, string key, out int col)
		{
			if (headerMap.TryGetValue(key, out col)) return true;
			if (headerMap.TryGetValue("lgd" + key, out col)) return true;
			if (headerMap.TryGetValue("fms" + key, out col)) return true;
			col = 0;
			return false;
		}

		private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

		// Header-mapped access accepting plain/lgd/fms prefixed variants
		private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string key)
		{
			if (headerMap.TryGetValue(key, out var col)) return row.Cell(col).GetString().Trim();
			var lgd = "lgd" + key;
			if (headerMap.TryGetValue(lgd, out col)) return row.Cell(col).GetString().Trim();
			var fms = "fms" + key;
			if (headerMap.TryGetValue(fms, out col)) return row.Cell(col).GetString().Trim();

			return string.Empty;
		}

		private static string NormalizeHeader(string h) => (h ?? string.Empty).Trim().Replace(" ", "").Replace("_", "").Replace("&", "").ToLowerInvariant();

		private static string PrettyHeader(string h) => h switch
		{
			"beneficiaryid" => "BeneficiaryID",
			"dealercode" => "DealerCode",
			"maindealerfirmname" => "MainDealerFirmName",
			"hq" => "HQ",
			"branchdistrict" => "Branch&District",
			"subdealercode" => "SubDealerCode",
			"subdealername" => "SubDealerName",
			"subdealerdistrict" => "SubDealerDistrict",
			"nomineename" => "NomineeName",
			"beneficiaryname" => "BeneficiaryName",
			"dob" => "DOB",
			"relationship" => "Relationship",
			"employeename" => "EmployeeName",
			"maritalstatus" => "MaritalStatus",
			"educationalqualification" => "EducationalQualification",
			_ => h
		};
	}
}
