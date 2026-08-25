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
		// ADMIN MASTER PAGE READS (SubDealerEmployeeMaster.razor)
		// Admin sees all; SM sees state-scoped; AVP sees zone-scoped.
		// =====================================================================

		// GET /api/subdealeremployee/all-subdealers
		// Returns all sub dealers with geographic scoping based on role.
		[HttpGet("all-subdealers")]
		public async Task<ActionResult<List<SubDealerEmployeeItemDto>>> GetAllSubDealers()
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value
				?? User.FindFirst("role")?.Value ?? "";

			IQueryable<SubDealerBeneficiary> query = _db.SubDealerBeneficiaries.AsNoTracking();

			if (role is "SMD" or "SMM")
			{
				var stateId = int.TryParse(User.FindFirst("spic:state_id")?.Value, out var s) ? s : 0;
				if (stateId <= 0) return Ok(new List<SubDealerEmployeeItemDto>());

				var dealerCodes = await _db.DealerRegistrations
					.AsNoTracking()
					.Where(d => d.StateId == stateId && d.DealerCode != null)
					.Select(d => d.DealerCode!)
					.Distinct()
					.ToListAsync();

				query = query.Where(sd => dealerCodes.Contains(sd.DealerCode));
			}
			else if (role == "AVP")
			{
				var regionId = int.TryParse(User.FindFirst("spic:region_id")?.Value, out var r) ? r : 0;
				if (regionId <= 0) return Ok(new List<SubDealerEmployeeItemDto>());

				var dealerCodes = await _db.DealerRegistrations
					.AsNoTracking()
					.Where(d => d.Region == regionId && d.DealerCode != null)
					.Select(d => d.DealerCode!)
					.Distinct()
					.ToListAsync();

				query = query.Where(sd => dealerCodes.Contains(sd.DealerCode));
			}
			// Admin/CorporateAdmin: no geographic filter

			var items = await query
				.OrderByDescending(sd => sd.CreatedAt)
				.Select(sd => new SubDealerEmployeeItemDto
				{
					Id = sd.Id,
					BeneficiaryId = sd.BeneficiaryId,
					DealerCode = sd.DealerCode,
					MainDealerFirmName = sd.MainDealerFirmName,
					HQ = sd.HQ,
					BranchDistrict = sd.BranchDistrict,
					SubDealerCode = sd.SubDealerCode,
					SubDealerName = sd.SubDealerName,
					SubDealerDistrict = sd.SubDealerDistrict,
					NomineeName = sd.NomineeName,
					BeneficiaryName = sd.BeneficiaryName,
					DOB = sd.DOB,
					Relationship = sd.Relationship,
					IsActive = sd.IsActive,
					SMApproved = sd.SMApproved,
					AVPApproved = sd.AVPApproved,
					CreatedAt = sd.CreatedAt,
					UpdatedAt = sd.UpdatedAt,
					UpdatedBy = sd.UpdatedBy
				})
				.ToListAsync();

			return Ok(items);
		}

		// GET /api/subdealeremployee/all-employees
		// Returns all employees with geographic scoping based on role.
		[HttpGet("all-employees")]
		public async Task<ActionResult<List<SubDealerEmployeeItemDto>>> GetAllEmployees()
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value
				?? User.FindFirst("role")?.Value ?? "";

			IQueryable<EmployeeBeneficiary> query = _db.EmployeeBeneficiaries.AsNoTracking();

			if (role is "SMD" or "SMM")
			{
				var stateId = int.TryParse(User.FindFirst("spic:state_id")?.Value, out var s) ? s : 0;
				if (stateId <= 0) return Ok(new List<SubDealerEmployeeItemDto>());

				var dealerCodes = await _db.DealerRegistrations
					.AsNoTracking()
					.Where(d => d.StateId == stateId && d.DealerCode != null)
					.Select(d => d.DealerCode!)
					.Distinct()
					.ToListAsync();

				query = query.Where(e => dealerCodes.Contains(e.DealerCode));
			}
			else if (role == "AVP")
			{
				var regionId = int.TryParse(User.FindFirst("spic:region_id")?.Value, out var r) ? r : 0;
				if (regionId <= 0) return Ok(new List<SubDealerEmployeeItemDto>());

				var dealerCodes = await _db.DealerRegistrations
					.AsNoTracking()
					.Where(d => d.Region == regionId && d.DealerCode != null)
					.Select(d => d.DealerCode!)
					.Distinct()
					.ToListAsync();

				query = query.Where(e => dealerCodes.Contains(e.DealerCode));
			}
			// Admin/CorporateAdmin: no geographic filter

			var items = await query
				.OrderByDescending(e => e.CreatedAt)
				.Select(e => new SubDealerEmployeeItemDto
				{
					Id = e.Id,
					BeneficiaryId = e.BeneficiaryId,
					DealerCode = e.DealerCode,
					EmployeeName = e.EmployeeName,
					BeneficiaryName = e.BeneficiaryName,
					DOB = e.DOB,
					Relationship = e.Relationship,
					MaritalStatus = e.MaritalStatus,
					EducationalQualification = e.EducationalQualification,
					IsActive = e.IsActive,
					SMApproved = e.SMApproved,
					AVPApproved = e.AVPApproved,
					CreatedAt = e.CreatedAt,
					UpdatedAt = e.UpdatedAt,
					UpdatedBy = e.UpdatedBy
				})
				.ToListAsync();

			return Ok(items);
		}

		// =====================================================================
		// DEALER-SCOPED READS for the SDWA Welfare Application
		// =====================================================================

		// GET /api/subdealeremployee/my-sub-dealers
		// Returns ONLY the active, fully approved (SM + AVP) sub dealers belonging to the logged-in dealer.
		[HttpGet("my-sub-dealers")]
		public async Task<ActionResult<List<SubDealerDto>>> GetMySubDealers()
		{
			var dealerCode = await ResolveCurrentDealerCodeAsync();
			if (string.IsNullOrEmpty(dealerCode))
				return Ok(new List<SubDealerDto>());

			var subDealers = await _db.SubDealerBeneficiaries
				.AsNoTracking()
				.Where(sd => sd.IsActive && sd.DealerCode == dealerCode
					&& sd.AVPApproved == true)
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
		// Returns ONLY the active, fully approved (SM + AVP) employees belonging to the logged-in dealer.
		[HttpGet("my-employees")]
		public async Task<ActionResult<List<EmployeeDto>>> GetMyEmployees()
		{
			var dealerCode = await ResolveCurrentDealerCodeAsync();
			if (string.IsNullOrEmpty(dealerCode))
				return Ok(new List<EmployeeDto>());

			var employees = await _db.EmployeeBeneficiaries
				.AsNoTracking()
				.Where(e => e.IsActive && e.DealerCode == dealerCode
					&& e.AVPApproved == true)
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

		// =====================================================================
		// APPROVAL WORKFLOW – SM (SMD/SMM) and AVP
		// =====================================================================

		// GET /api/subdealeremployee/pending-sm-approvals
		// SM sees only records where Dealer is in their state (SMApproved null, AVPApproved null).
		[Authorize(Roles = "SMD,SMM")]
		[HttpGet("pending-sm-approvals")]
		public async Task<ActionResult<List<PendingApprovalItemDto>>> GetPendingSMApprovals()
		{
			var stateId = int.TryParse(User.FindFirst("spic:state_id")?.Value, out var s) ? s : 0;
			if (stateId <= 0)
				return Ok(new List<PendingApprovalItemDto>());

			var dealerIdsInState = await _db.DealerRegistrations
				.AsNoTracking()
				.Where(d => d.StateId == stateId)
				.Select(d => d.Id)
				.ToListAsync();

			var dealerCodes = await _db.DealerRegistrations
				.AsNoTracking()
				.Where(d => d.StateId == stateId && d.DealerCode != null)
				.Select(d => d.DealerCode!)
				.Distinct()
				.ToListAsync();

			var pendingSubDealers = await _db.SubDealerBeneficiaries
				.AsNoTracking()
				.Where(sd => sd.IsActive && sd.SMApproved == null && dealerCodes.Contains(sd.DealerCode))
				.OrderBy(sd => sd.CreatedAt)
				.Select(sd => new PendingApprovalItemDto
				{
					Id = sd.Id, Type = "Sub Dealer",
					DealerCode = sd.DealerCode, ItemCode = sd.SubDealerCode,
					ItemName = sd.SubDealerName,
					BeneficiaryName = sd.BeneficiaryName,
					NomineeName = sd.NomineeName,
					SMApproved = sd.SMApproved, AVPApproved = sd.AVPApproved,
					OverallStatus = "Pending SM"
				})
				.ToListAsync();

			var pendingEmployees = await _db.EmployeeBeneficiaries
				.AsNoTracking()
				.Where(e => e.IsActive && e.SMApproved == null && dealerCodes.Contains(e.DealerCode))
				.OrderBy(e => e.CreatedAt)
				.Select(e => new PendingApprovalItemDto
				{
					Id = e.Id, Type = "Employee",
					DealerCode = e.DealerCode, ItemCode = e.EmployeeName,
					ItemName = e.EmployeeName,
					BeneficiaryName = e.BeneficiaryName,
					NomineeName = null,
					SMApproved = e.SMApproved, AVPApproved = e.AVPApproved,
					OverallStatus = "Pending SM"
				})
				.ToListAsync();

			return Ok(pendingSubDealers.Concat(pendingEmployees).ToList());
		}

		// POST /api/subdealeremployee/sm-approve/{id}?type=subdealer|employee
		[Authorize(Roles = "SMD,SMM")]
		[HttpPost("sm-approve/{id:int}")]
		public async Task<IActionResult> SMApprove(int id, [FromQuery] string type, [FromBody] ApprovalActionRequest? request)
		{
			return await ProcessApprovalAction(id, type, isApproval: true, isSM: true, request?.Remarks);
		}

		// POST /api/subdealeremployee/sm-reject/{id}?type=subdealer|employee
		[Authorize(Roles = "SMD,SMM")]
		[HttpPost("sm-reject/{id:int}")]
		public async Task<IActionResult> SMReject(int id, [FromQuery] string type, [FromBody] ApprovalActionRequest? request)
		{
			return await ProcessApprovalAction(id, type, isApproval: false, isSM: true, request?.Remarks);
		}

		// GET /api/subdealeremployee/pending-avp-approvals
		// AVP sees all records where SMApproved == true and AVPApproved == null (across all states).
		[Authorize(Roles = "AVP")]
		[HttpGet("pending-avp-approvals")]
		public async Task<ActionResult<List<PendingApprovalItemDto>>> GetPendingAVPApprovals()
		{
			var pendingSubDealers = await _db.SubDealerBeneficiaries
				.AsNoTracking()
				.Where(sd => sd.IsActive && sd.SMApproved == true && sd.AVPApproved == null)
				.OrderBy(sd => sd.CreatedAt)
				.Select(sd => new PendingApprovalItemDto
				{
					Id = sd.Id, Type = "Sub Dealer",
					DealerCode = sd.DealerCode, ItemCode = sd.SubDealerCode,
					ItemName = sd.SubDealerName,
					BeneficiaryName = sd.BeneficiaryName,
					NomineeName = sd.NomineeName,
					SMApproved = sd.SMApproved, AVPApproved = sd.AVPApproved,
					OverallStatus = "Pending AVP"
				})
				.ToListAsync();

			var pendingEmployees = await _db.EmployeeBeneficiaries
				.AsNoTracking()
				.Where(e => e.IsActive && e.SMApproved == true && e.AVPApproved == null)
				.OrderBy(e => e.CreatedAt)
				.Select(e => new PendingApprovalItemDto
				{
					Id = e.Id, Type = "Employee",
					DealerCode = e.DealerCode, ItemCode = e.EmployeeName,
					ItemName = e.EmployeeName,
					BeneficiaryName = e.BeneficiaryName,
					NomineeName = null,
					SMApproved = e.SMApproved, AVPApproved = e.AVPApproved,
					OverallStatus = "Pending AVP"
				})
				.ToListAsync();

			return Ok(pendingSubDealers.Concat(pendingEmployees).ToList());
		}

		// POST /api/subdealeremployee/avp-approve/{id}?type=subdealer|employee
		[Authorize(Roles = "AVP")]
		[HttpPost("avp-approve/{id:int}")]
		public async Task<IActionResult> AVPApprove(int id, [FromQuery] string type, [FromBody] ApprovalActionRequest? request)
		{
			return await ProcessApprovalAction(id, type, isApproval: true, isSM: false, request?.Remarks);
		}

		// POST /api/subdealeremployee/avp-reject/{id}?type=subdealer|employee
		[Authorize(Roles = "AVP")]
		[HttpPost("avp-reject/{id:int}")]
		public async Task<IActionResult> AVPReject(int id, [FromQuery] string type, [FromBody] ApprovalActionRequest? request)
		{
			return await ProcessApprovalAction(id, type, isApproval: false, isSM: false, request?.Remarks);
		}

		// Shared approval logic for both SM and AVP
		private async Task<IActionResult> ProcessApprovalAction(
			int id, string type, bool isApproval, bool isSM, string? remarks)
		{
			var t = (type ?? "").Trim().ToLowerInvariant();
			if (t != "subdealer" && t != "employee")
				return BadRequest(new { Success = false, Message = "type must be 'subdealer' or 'employee'" });

			var actorName =
				User.FindFirst("name")?.Value
				?? User.FindFirst(ClaimTypes.Name)?.Value
				?? "Unknown";

			bool? smResult = null;
			bool? avpResult = null;
			string singularType = t == "subdealer" ? "Sub Dealer" : "Employee";

			if (t == "subdealer")
			{
				var item = await _db.SubDealerBeneficiaries.FindAsync(id);
				if (item == null) return NotFound(new { Success = false, Message = "Sub Dealer not found" });

				if (isSM)
				{
					if (item.SMApproved != null)
						return Conflict(new { Success = false, Message = "This Sub Dealer has already been processed at SM level" });

					item.SMApproved = isApproval;
					item.SMApprovedBy = actorName;
					item.SMApprovedAt = DateTime.UtcNow;
					item.SMApprovalRemarks = remarks;
					smResult = isApproval;
					avpResult = item.AVPApproved;
				}
				else
				{
					if (item.SMApproved != true)
						return Conflict(new { Success = false, Message = "SM must approve before AVP can process" });
					if (item.AVPApproved != null)
						return Conflict(new { Success = false, Message = "This Sub Dealer has already been processed at AVP level" });

					item.AVPApproved = isApproval;
					item.AVPApprovedBy = actorName;
					item.AVPApprovedAt = DateTime.UtcNow;
					item.AVPApprovalRemarks = remarks;
					smResult = item.SMApproved;
					avpResult = isApproval;
				}
			}
			else
			{
				var item = await _db.EmployeeBeneficiaries.FindAsync(id);
				if (item == null) return NotFound(new { Success = false, Message = "Employee not found" });

				if (isSM)
				{
					if (item.SMApproved != null)
						return Conflict(new { Success = false, Message = "This Employee has already been processed at SM level" });

					item.SMApproved = isApproval;
					item.SMApprovedBy = actorName;
					item.SMApprovedAt = DateTime.UtcNow;
					item.SMApprovalRemarks = remarks;
					smResult = isApproval;
					avpResult = item.AVPApproved;
				}
				else
				{
					if (item.SMApproved != true)
						return Conflict(new { Success = false, Message = "SM must approve before AVP can process" });
					if (item.AVPApproved != null)
						return Conflict(new { Success = false, Message = "This Employee has already been processed at AVP level" });

					item.AVPApproved = isApproval;
					item.AVPApprovedBy = actorName;
					item.AVPApprovedAt = DateTime.UtcNow;
					item.AVPApprovalRemarks = remarks;
					smResult = item.SMApproved;
					avpResult = isApproval;
				}
			}

			await _db.SaveChangesAsync();

			var overall = (smResult, avpResult) switch
			{
				(true, true) => "Approved",
				(false, _) => "SM Rejected",
				(true, false) => "AVP Rejected",
				_ => "Pending"
			};

			var action = isApproval ? "Approved" : "Rejected";
			var level = isSM ? "SM" : "AVP";

			_logger.LogInformation("{Actor} {Action} {Type} {Id} at {Level} level", actorName, action, singularType, id, level);

			return Ok(new ApprovalActionResponse
			{
				Success = true,
				Message = $"{singularType} {action.ToLowerInvariant()} by {level}",
				OverallStatus = overall
			});
		}

		// Resolves the logged-in dealer's code the same way as SDWADashboard/Welfare controllers:
		// NameIdentifier claim -> Users table -> DealerRegistrations.UserTableId -> DealerCode
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

			var code = dealer?.DealerCode;
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
