using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
	/// Admin / CorporateAdmin, or a Designation granting the matching
	/// PagePermission.SubDealerEmployeeMaster action (see <see cref="SubDealerEmployeeMasterAccessAttribute"/>).
	/// </summary>
	[Authorize]
	[SubDealerEmployeeMasterAccess]
	[ApiController]
	[Route("api/[controller]")]
	public class SubDealerBeneficiaryController(IGenericRepository<SubDealerBeneficiary> repo, AppDbContext db)
		: GenericCrudController<SubDealerBeneficiary>(repo)
	{
		private const string DuplicateMessage =
			"A beneficiary with the same Dealer Code, Sub Dealer Code, Beneficiary Name and DOB already exists.";

		// Same duplicate rule as the bulk upload (SubDealerEmployeeController.SubDealerBeneficiaryKey).
		// Audit identity is server-derived: UpdatedBy is the authenticated AspNetUsers.Id (the
		// NameIdentifier claim, resolved like SubDealerRegistrationController.Actor). Whatever the
		// client sends for UpdatedBy / CreatedAt / the SM-AVP approval fields is ignored. This entity
		// has no CreatedBy column, so only UpdatedBy is stamped.
		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] SubDealerBeneficiary entity)
		{
			var actor = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actor is null) return Unauthorized();
			if (await SubDealerEmployeeController.SubDealerBeneficiaryExistsAsync(_repo.GetAllWithInactive(), entity, excludeId: null))
				return Conflict(new { message = DuplicateMessage });

			var now = DateTime.Now;
			entity.UpdatedBy = actor;
			entity.CreatedAt = now;
			entity.UpdatedAt = now;
			// A new row always starts pending: approvals are only set by the SM / AVP approval endpoints.
			entity.SMApproved = null; entity.SMApprovedBy = null; entity.SMApprovedAt = null; entity.SMApprovalRemarks = null;
			entity.AVPApproved = null; entity.AVPApprovedBy = null; entity.AVPApprovedAt = null; entity.AVPApprovalRemarks = null;
			return await base.Create(entity);
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> Update(int id, [FromBody] SubDealerBeneficiary entity)
		{
			var actor = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actor is null) return Unauthorized();
			if (await SubDealerEmployeeController.SubDealerBeneficiaryExistsAsync(_repo.GetAllWithInactive(), entity, excludeId: id))
				return Conflict(new { message = DuplicateMessage });

			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();

			// PatchAsync copies every scalar from the request body, so pin the protected values first:
			// the editor becomes UpdatedBy, CreatedAt is kept, and an ordinary edit never resets or
			// forges the SM / AVP approval state or its approver / timestamp / remarks.
			entity.UpdatedBy = actor;
			entity.CreatedAt = existing.CreatedAt;
			entity.SMApproved = existing.SMApproved; entity.SMApprovedBy = existing.SMApprovedBy;
			entity.SMApprovedAt = existing.SMApprovedAt; entity.SMApprovalRemarks = existing.SMApprovalRemarks;
			entity.AVPApproved = existing.AVPApproved; entity.AVPApprovedBy = existing.AVPApprovedBy;
			entity.AVPApprovedAt = existing.AVPApprovedAt; entity.AVPApprovalRemarks = existing.AVPApprovalRemarks;
			return await base.Update(id, entity);
		}

		[HttpPatch("{id}/status")]
		public override async Task<IActionResult> ChangeStatus(int id, [FromQuery] bool isActive)
		{
			var actor = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actor is null) return Unauthorized();

			// Same tracked instance the repository's ChangeStatusAsync saves.
			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();
			existing.UpdatedBy = actor;
			return await base.ChangeStatus(id, isActive);
		}

		// Welfare applications keep the picked sub dealer's id (my-sub-dealers returns this table's Id) with
		// no database FK, so nothing else would stop the delete and the application would point at nothing.
		[HttpDelete("{id}")]
		public override async Task<IActionResult> Delete(int id)
		{
			if (await db.WelfareApplications.AsNoTracking().AnyAsync(a => a.SubDealerId == id))
				return Conflict(new { message = "Cannot delete this Sub Dealer because welfare applications reference it. Deactivate it instead." });

			try
			{
				return await base.Delete(id);
			}
			catch (DbUpdateException)
			{
				return Conflict(new { message = "Cannot delete this Sub Dealer because other records still depend on it. Deactivate it instead." });
			}
		}
	}

	/// <summary>
	/// CRUD endpoints for the Approved Employee beneficiary master.
	/// Admin / CorporateAdmin, or a Designation granting the matching
	/// PagePermission.SubDealerEmployeeMaster action (see <see cref="SubDealerEmployeeMasterAccessAttribute"/>).
	/// </summary>
	[Authorize]
	[SubDealerEmployeeMasterAccess]
	[ApiController]
	[Route("api/[controller]")]
	public class EmployeeBeneficiaryController(IGenericRepository<EmployeeBeneficiary> repo, AppDbContext db)
		: GenericCrudController<EmployeeBeneficiary>(repo)
	{
		private const string DuplicateMessage =
			"An employee with the same Dealer Code, Employee Name and DOB already exists.";

		// Same duplicate rule as the bulk upload (SubDealerEmployeeController.EmployeeBeneficiaryKey).
		// Audit handling mirrors SubDealerBeneficiaryController: server-stamped UpdatedBy (authenticated
		// AspNetUsers.Id), server timestamps, approvals not client-controlled. No CreatedBy column exists.
		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] EmployeeBeneficiary entity)
		{
			var actor = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actor is null) return Unauthorized();
			if (await SubDealerEmployeeController.EmployeeBeneficiaryExistsAsync(_repo.GetAllWithInactive(), entity, excludeId: null))
				return Conflict(new { message = DuplicateMessage });

			var now = DateTime.Now;
			entity.UpdatedBy = actor;
			entity.CreatedAt = now;
			entity.UpdatedAt = now;
			entity.SMApproved = null; entity.SMApprovedBy = null; entity.SMApprovedAt = null; entity.SMApprovalRemarks = null;
			entity.AVPApproved = null; entity.AVPApprovedBy = null; entity.AVPApprovedAt = null; entity.AVPApprovalRemarks = null;
			return await base.Create(entity);
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> Update(int id, [FromBody] EmployeeBeneficiary entity)
		{
			var actor = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actor is null) return Unauthorized();
			if (await SubDealerEmployeeController.EmployeeBeneficiaryExistsAsync(_repo.GetAllWithInactive(), entity, excludeId: id))
				return Conflict(new { message = DuplicateMessage });

			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();

			entity.UpdatedBy = actor;
			entity.CreatedAt = existing.CreatedAt;
			entity.SMApproved = existing.SMApproved; entity.SMApprovedBy = existing.SMApprovedBy;
			entity.SMApprovedAt = existing.SMApprovedAt; entity.SMApprovalRemarks = existing.SMApprovalRemarks;
			entity.AVPApproved = existing.AVPApproved; entity.AVPApprovedBy = existing.AVPApprovedBy;
			entity.AVPApprovedAt = existing.AVPApprovedAt; entity.AVPApprovalRemarks = existing.AVPApprovalRemarks;
			return await base.Update(id, entity);
		}

		[HttpPatch("{id}/status")]
		public override async Task<IActionResult> ChangeStatus(int id, [FromQuery] bool isActive)
		{
			var actor = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actor is null) return Unauthorized();

			var existing = await _repo.GetByIdAsync(id);
			if (existing == null) return NotFound();
			existing.UpdatedBy = actor;
			return await base.ChangeStatus(id, isActive);
		}

		// See SubDealerBeneficiaryController.Delete: welfare applications reference the employee id softly.
		[HttpDelete("{id}")]
		public override async Task<IActionResult> Delete(int id)
		{
			if (await db.WelfareApplications.AsNoTracking().AnyAsync(a => a.EmployeeId == id))
				return Conflict(new { message = "Cannot delete this Employee because welfare applications reference it. Deactivate it instead." });

			try
			{
				return await base.Delete(id);
			}
			catch (DbUpdateException)
			{
				return Conflict(new { message = "Cannot delete this Employee because other records still depend on it. Deactivate it instead." });
			}
		}
	}

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
		[SubDealerEmployeeMasterAccess("Entry")]
		[HttpPost("bulk-upload")]
		public async Task<IActionResult> BulkUpload([FromQuery] string type, IFormFile file)
		{
			// The importing user's AspNetUsers.Id is the audit actor of every inserted row.
			var actor = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actor is null) return Unauthorized();

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
			// Valid dealer codes (same resolution used across the app: SPICCode ?? DealerCode)
			var validDealerCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var d in _db.DealerRegistrations
				.Where(d => d.DealerCode != null || d.SPICCode != null)
				.Select(d => new { d.DealerCode, d.SPICCode }).AsEnumerable())
			{
				if (!string.IsNullOrWhiteSpace(d.SPICCode)) validDealerCodes.Add(d.SPICCode.Trim());
				if (!string.IsNullOrWhiteSpace(d.DealerCode)) validDealerCodes.Add(d.DealerCode.Trim());
			}

			// Duplicate beneficiary keys. One Sub Dealer / Employee may have several beneficiaries, so the
			// key is the full beneficiary identity, never the business key alone:
			//   Sub Dealer: DealerCode + SubDealerCode + BeneficiaryName + DOB
			//   Employee:   DealerCode + EmployeeName + DOB
			// Loaded once (only the key columns) so large files never query per row. Rows without a DOB
			// are never treated as duplicates.
			var isSubDealerUpload = t is "subdealer" or "sub-dealer" or "sub_dealer";
			var existingBeneficiaryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (isSubDealerUpload)
			{
				foreach (var sd in _db.SubDealerBeneficiaries.AsNoTracking()
					.Where(s => s.DOB != null)
					.Select(s => new { s.DealerCode, s.SubDealerCode, s.BeneficiaryName, s.DOB }).AsEnumerable())
					existingBeneficiaryKeys.Add(SubDealerBeneficiaryKey(sd.DealerCode, sd.SubDealerCode, sd.BeneficiaryName, sd.DOB!.Value));
			}
			else
			{
				foreach (var e in _db.EmployeeBeneficiaries.AsNoTracking()
					.Where(e => e.DOB != null)
					.Select(e => new { e.DealerCode, e.EmployeeName, e.DOB }).AsEnumerable())
					existingBeneficiaryKeys.Add(EmployeeBeneficiaryKey(e.DealerCode, e.EmployeeName, e.DOB!.Value));
			}
			// Keys of rows already accepted from this file.
			var batchBeneficiaryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

							if (dobValue.HasValue)
							{
								var key = SubDealerBeneficiaryKey(dealerCode, subDealerCode, beneficiaryName, dobValue.Value);
								if (existingBeneficiaryKeys.Contains(key))
								{
									AddGrouped("Already exists in database", $"Row {row.RowNumber()}: beneficiary '{beneficiaryName}' (DOB {dobValue.Value:dd-MM-yyyy}) of sub dealer '{subDealerCode}', dealer '{dealerCode}'");
									continue;
								}
								if (!batchBeneficiaryKeys.Add(key))
								{
									AddGrouped("Duplicated in this file", $"Row {row.RowNumber()}: beneficiary '{beneficiaryName}' (DOB {dobValue.Value:dd-MM-yyyy}) of sub dealer '{subDealerCode}', dealer '{dealerCode}'");
									continue;
								}
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
								UpdatedBy = actor
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

							if (dobValue.HasValue)
							{
								var key = EmployeeBeneficiaryKey(dealerCode, employeeName, dobValue.Value);
								if (existingBeneficiaryKeys.Contains(key))
								{
									AddGrouped("Already exists in database", $"Row {row.RowNumber()}: employee '{employeeName}' (DOB {dobValue.Value:dd-MM-yyyy}) of dealer '{dealerCode}'");
									continue;
								}
								if (!batchBeneficiaryKeys.Add(key))
								{
									AddGrouped("Duplicated in this file", $"Row {row.RowNumber()}: employee '{employeeName}' (DOB {dobValue.Value:dd-MM-yyyy}) of dealer '{dealerCode}'");
									continue;
								}
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
								UpdatedBy = actor
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
		[SubDealerEmployeeMasterAccess("Entry")]
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

				var dealers = await _db.DealerRegistrations
					.AsNoTracking()
					.Where(d => d.StateId == stateId)
					.Select(d => new { d.DealerCode, d.SPICCode })
					.ToListAsync();

				var dealerCodes = dealers
					.SelectMany(d => new[] { d.DealerCode, d.SPICCode })
					.Where(c => c != null)
					.Select(c => c!.Trim())
					.Distinct()
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

				query = query.Where(sd => dealerCodes.Contains(sd.DealerCode.Trim()));
			}
			else if (role == "AVP")
			{
				// AVP sees only sub dealers belonging to the Zone assigned to the logged-in AVP.
				var zoneDealerCodes = await GetZoneDealerCodesAsync();
				if (zoneDealerCodes == null)
					return Ok(new List<SubDealerEmployeeItemDto>());

				query = query.Where(sd => zoneDealerCodes.Contains(sd.DealerCode.Trim()));
			}

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

				var dealers = await _db.DealerRegistrations
					.AsNoTracking()
					.Where(d => d.StateId == stateId)
					.Select(d => new { d.DealerCode, d.SPICCode })
					.ToListAsync();

				var dealerCodes = dealers
					.SelectMany(d => new[] { d.DealerCode, d.SPICCode })
					.Where(c => c != null)
					.Select(c => c!.Trim())
					.Distinct()
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

				query = query.Where(e => dealerCodes.Contains(e.DealerCode.Trim()));
			}
			else if (role == "AVP")
			{
				// AVP sees only employees belonging to the Zone assigned to the logged-in AVP.
				var zoneDealerCodes = await GetZoneDealerCodesAsync();
				if (zoneDealerCodes == null)
					return Ok(new List<SubDealerEmployeeItemDto>());

				query = query.Where(e => zoneDealerCodes.Contains(e.DealerCode.Trim()));
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
		// AVP sees records where SMApproved == true and AVPApproved == null,
		// scoped to the Zone assigned to the logged-in AVP.
		[Authorize(Roles = "AVP")]
		[HttpGet("pending-avp-approvals")]
		public async Task<ActionResult<List<PendingApprovalItemDto>>> GetPendingAVPApprovals()
		{
			var zoneDealerCodes = await GetZoneDealerCodesAsync();
			if (zoneDealerCodes == null)
				return Ok(new List<PendingApprovalItemDto>());

			var pendingSubDealers = await _db.SubDealerBeneficiaries
				.AsNoTracking()
				.Where(sd => sd.IsActive && sd.SMApproved == true && sd.AVPApproved == null
					&& zoneDealerCodes.Contains(sd.DealerCode.Trim()))
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
				.Where(e => e.IsActive && e.SMApproved == true && e.AVPApproved == null
					&& zoneDealerCodes.Contains(e.DealerCode.Trim()))
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

			// The approver is recorded as the authenticated user's AspNetUsers.Id (the same server-side actor as the
			// master's UpdatedBy). Nothing in the SDWA screens reads SMApprovedBy / AVPApprovedBy, so no display name
			// is needed here; the old username fallback ("Unknown") could not tell two approvers apart.
			var actorId = SPIC.Api.Controllers.SubDealerRegistrationController.Actor(User);
			if (actorId is null)
				return Unauthorized(new { Success = false, Message = "Authentication required." });

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
					item.SMApprovedBy = actorId;
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
					item.AVPApprovedBy = actorId;
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
					item.SMApprovedBy = actorId;
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
					item.AVPApprovedBy = actorId;
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

			_logger.LogInformation("{Actor} {Action} {Type} {Id} at {Level} level", actorId, action, singularType, id, level);

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

		// Resolves the dealer codes that belong to the logged-in AVP's Zone
		// (Dealer -> State -> Zone). Returns null when the AVP has no Zone
		// assigned, which callers treat as an empty result set.
		private async Task<HashSet<string>?> GetZoneDealerCodesAsync()
		{
			var zoneId = int.TryParse(User.FindFirst("spic:zone_id")?.Value, out var z) ? z : 0;
			if (zoneId <= 0)
				return null;

			var stateIdsInZone = await _db.States
				.AsNoTracking()
				.Where(s => s.ZoneId == zoneId)
				.Select(s => s.Id)
				.ToListAsync();

			var dealers = await _db.DealerRegistrations
				.AsNoTracking()
				.Where(d => d.StateId != 0 && stateIdsInZone.Contains(d.StateId))
				.Select(d => new { d.DealerCode, d.SPICCode })
				.ToListAsync();

			return dealers
				.SelectMany(d => new[] { d.DealerCode, d.SPICCode })
				.Where(c => c != null)
				.Select(c => c!.Trim())
				.Distinct()
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
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

		// Duplicate-beneficiary keys for bulk upload. Codes and names are trimmed, DOB is date-only;
		// callers compare with OrdinalIgnoreCase so names/codes match case-insensitively. \u001F (unit
		// separator) cannot appear in a typed cell, so adjacent fields never run together.
		internal static string SubDealerBeneficiaryKey(string? dealerCode, string? subDealerCode, string? beneficiaryName, DateTime dob) =>
			$"{dealerCode?.Trim()}\u001F{subDealerCode?.Trim()}\u001F{beneficiaryName?.Trim()}\u001F{dob.Date:yyyy-MM-dd}";

		internal static string EmployeeBeneficiaryKey(string? dealerCode, string? employeeName, DateTime dob) =>
			$"{dealerCode?.Trim()}\u001F{employeeName?.Trim()}\u001F{dob.Date:yyyy-MM-dd}";

		// Manual Add / Edit duplicate checks (SubDealerBeneficiary / EmployeeBeneficiary controllers),
		// using the same keys as the bulk upload. No DOB => never a duplicate. Only records born on the
		// same date are loaded, then compared by key; excludeId skips the record being updated.
		internal static async Task<bool> SubDealerBeneficiaryExistsAsync(
			IQueryable<SubDealerBeneficiary> source, SubDealerBeneficiary candidate, int? excludeId)
		{
			if (candidate?.DOB is not DateTime dob) return false;
			var day = dob.Date;
			var nextDay = day.AddDays(1);
			var key = SubDealerBeneficiaryKey(candidate.DealerCode, candidate.SubDealerCode, candidate.BeneficiaryName, dob);

			var sameDay = await source.AsNoTracking()
				.Where(s => s.DOB >= day && s.DOB < nextDay && (excludeId == null || s.Id != excludeId))
				.Select(s => new { s.DealerCode, s.SubDealerCode, s.BeneficiaryName, s.DOB })
				.ToListAsync();
			return sameDay.Any(s => string.Equals(
				SubDealerBeneficiaryKey(s.DealerCode, s.SubDealerCode, s.BeneficiaryName, s.DOB!.Value), key, StringComparison.OrdinalIgnoreCase));
		}

		internal static async Task<bool> EmployeeBeneficiaryExistsAsync(
			IQueryable<EmployeeBeneficiary> source, EmployeeBeneficiary candidate, int? excludeId)
		{
			if (candidate?.DOB is not DateTime dob) return false;
			var day = dob.Date;
			var nextDay = day.AddDays(1);
			var key = EmployeeBeneficiaryKey(candidate.DealerCode, candidate.EmployeeName, dob);

			var sameDay = await source.AsNoTracking()
				.Where(e => e.DOB >= day && e.DOB < nextDay && (excludeId == null || e.Id != excludeId))
				.Select(e => new { e.DealerCode, e.EmployeeName, e.DOB })
				.ToListAsync();
			return sameDay.Any(e => string.Equals(
				EmployeeBeneficiaryKey(e.DealerCode, e.EmployeeName, e.DOB!.Value), key, StringComparison.OrdinalIgnoreCase));
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

	/// <summary>
	/// Authorization gate for the Sub Dealer &amp; Employee master (SubDealerBeneficiary /
	/// EmployeeBeneficiary CRUD, bulk upload, sample template).
	/// <para>
	/// WHY THIS EXISTS: these endpoints used to carry <c>[Authorize(Roles = "Admin,CorporateAdmin")]</c>,
	/// which only reads the JWT role claim, so a Designation-driven user (e.g. an SDWA Admin
	/// designation) whose RoleAccess grants <c>SubDealerEmployeeMaster.Entry</c> was rejected even
	/// though the permission was assigned.
	/// </para>
	/// Allowed: Admin / CorporateAdmin (the existing role rule, verbatim), or a Designation granting
	/// the required action on <c>PagePermission.SubDealerEmployeeMaster</c>. With an explicit action
	/// that action is required; otherwise it is derived from the HTTP method (GET = any grant on the
	/// page, POST = Entry, PUT / PATCH = Update, DELETE = Delete). A View-only grant never writes.
	/// Resolved from Designation.RoleAccess, never the Designation name.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	internal sealed class SubDealerEmployeeMasterAccessAttribute : Attribute, IAsyncAuthorizationFilter
	{
		private readonly string? _action;

		public SubDealerEmployeeMasterAccessAttribute() { }

		public SubDealerEmployeeMasterAccessAttribute(string action) => _action = action;

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.User;
			if (user?.Identity?.IsAuthenticated != true)
			{
				context.Result = new UnauthorizedObjectResult(new { Success = false, Message = "Authentication required." });
				return;
			}

			// Existing role rule, verbatim from [Authorize(Roles = "Admin,CorporateAdmin")].
			if (user.IsInRole(nameof(AppRole.Admin)) || user.IsInRole(nameof(AppRole.CorporateAdmin)))
				return;

			var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
			var designation = await GuestHouseReceptionistScope.GetDesignationAsync(db, user);

			var action = _action ?? ActionFor(context.HttpContext.Request.Method);
			var allowed = action == null
				? designation != null && RoleAccessPermissions.HasPage(designation.RoleAccess, PagePermission.SubDealerEmployeeMaster)
				: GuestHouseReceptionistScope.HasAction(designation, PagePermission.SubDealerEmployeeMaster, action);
			if (allowed)
				return;

			context.Result = new ObjectResult(new { Success = false, Message = "You do not have permission to perform this action on the Sub Dealer & Employee master." })
			{
				StatusCode = StatusCodes.Status403Forbidden
			};
		}

		// null = a read: any grant on the page is enough.
		private static string? ActionFor(string method) =>
			HttpMethods.IsPost(method) ? "Entry"
			: HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) ? "Update"
			: HttpMethods.IsDelete(method) ? "Delete"
			: null;
	}
}
