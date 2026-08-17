using System.Text.RegularExpressions;
using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubDealerRegistrationController : ControllerBase
{
	private const long MaxExcelImportSize = 10 * 1024 * 1024;

	private readonly AppDbContext _db;

	public SubDealerRegistrationController(AppDbContext db)
	{
		_db = db;
	}

	[HttpGet("lookup")]
	public async Task<ActionResult<List<SubDealerLookupDto>>> Lookup(
		[FromQuery] int? stateId,
		[FromQuery] int? regionId,
		[FromQuery] int? hqId,
		CancellationToken cancellationToken)
	{
		// Keep this query server-side. Role/location conditions are applied
		// before ToListAsync so MO users do not download the entire master.
		var query = _db.SubDealerRegistrations
			.AsNoTracking()
			.AsQueryable();

		var role = CurrentRole();
		var userId = CurrentUserId();

		var isUnrestrictedRole =
			IsUnrestrictedRole(role);

		if (!isUnrestrictedRole && IsMoRole(role))
		{
			// MO/MDO/JMDO must be HQ-scoped.
			// Prefer authenticated claim; use the page query parameter as a
			// local/dev fallback. Never return all rows for an MO.
			var effectiveHqId = CurrentHqId() ?? hqId;

			if (!effectiveHqId.HasValue || effectiveHqId.Value <= 0)
				return Ok(new List<SubDealerLookupDto>());

			query = query.Where(x => x.HQ == effectiveHqId.Value);
		}
		else if (!isUnrestrictedRole && IsRegionRole(role))
		{
			var effectiveRegionId = CurrentRegionId() ?? regionId;

			if (!effectiveRegionId.HasValue || effectiveRegionId.Value <= 0)
				return Ok(new List<SubDealerLookupDto>());

			query = query.Where(x => x.Region == effectiveRegionId.Value);
		}
		else if (!isUnrestrictedRole && IsStateRole(role))
		{
			var effectiveStateId = CurrentStateId() ?? stateId;

			if (!effectiveStateId.HasValue || effectiveStateId.Value <= 0)
				return Ok(new List<SubDealerLookupDto>());

			query = query.Where(x => x.StateId == effectiveStateId.Value);
		}
		else if (!isUnrestrictedRole && !string.IsNullOrWhiteSpace(role))
		{
			// Preserve existing fallback for other restricted/custom roles.
			if (string.IsNullOrWhiteSpace(userId))
				return Ok(new List<SubDealerLookupDto>());

			query = query.Where(x => x.CreatedBy == userId);
		}
		else
		{
			// Local/dev fallback when the request has no role claim.
			// The Blazor page sends only the logged-in user's correct scope.
			if (hqId.HasValue && hqId.Value > 0)
				query = query.Where(x => x.HQ == hqId.Value);
			else if (regionId.HasValue && regionId.Value > 0)
				query = query.Where(x => x.Region == regionId.Value);
			else if (stateId.HasValue && stateId.Value > 0)
				query = query.Where(x => x.StateId == stateId.Value);
		}

		var items = await query
			.OrderBy(x => x.SubDealerCode)
			.Select(x => new SubDealerLookupDto
			{
				Id = x.Id,
				SubDealerCode = x.SubDealerCode ?? string.Empty,
				FirmName = x.FirmName,
				StateId = x.StateId,
				RegionId = x.Region,
				HQId = x.HQ
			})
			.ToListAsync(cancellationToken);

		return Ok(items);
	}

	private string CurrentRole() =>
		User.FindFirst(ClaimTypes.Role)?.Value ??
		User.FindFirst("Role")?.Value ??
		string.Empty;

	private string? CurrentUserId() =>
		User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
		User.FindFirst("sub")?.Value ??
		User.FindFirst("spic:user_id")?.Value;

	private int? CurrentStateId() =>
		ReadIntClaim("spic:state_id", "StateId");

	private int? CurrentRegionId() =>
		ReadIntClaim("spic:region_id", "RegionId");

	private int? CurrentHqId() =>
		ReadIntClaim("spic:hq_id", "HQId", "HqId");

	private int? ReadIntClaim(params string[] names)
	{
		foreach (var name in names)
		{
			var value = User.FindFirst(name)?.Value;
			if (int.TryParse(value, out var id) && id > 0)
				return id;
		}

		return null;
	}

	private static bool IsMoRole(string role) =>
		role.Equals("MO", StringComparison.OrdinalIgnoreCase) ||
		role.Equals("MDO", StringComparison.OrdinalIgnoreCase) ||
		role.Equals("JMDO", StringComparison.OrdinalIgnoreCase);

	private static bool IsRegionRole(string role) =>
		role.Equals("RM", StringComparison.OrdinalIgnoreCase) ||
		role.Equals("RMD", StringComparison.OrdinalIgnoreCase);

	private static bool IsStateRole(string role) =>
		role.Equals("SMM", StringComparison.OrdinalIgnoreCase) ||
		role.Equals("SMD", StringComparison.OrdinalIgnoreCase);

	private static bool IsUnrestrictedRole(string role) =>
		role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
		role.Equals("CorporateAdmin", StringComparison.OrdinalIgnoreCase) ||
		role.Equals("Director", StringComparison.OrdinalIgnoreCase) ||
		role.Equals("AVP", StringComparison.OrdinalIgnoreCase);


	[HttpGet("list")]
	public async Task<ActionResult<SubDealerPagedListResponse>> GetList(
		[FromQuery] string? search,
		[FromQuery] int? status,
		[FromQuery] int? stateId,
		[FromQuery] int? regionId,
		[FromQuery] int? hqId,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		CancellationToken cancellationToken = default)
	{
		page = page < 1 ? 1 : page;
		pageSize = pageSize < 10 ? 10 : pageSize > 100 ? 100 : pageSize;

		var query = _db.SubDealerRegistrations
			.AsNoTracking()
			.AsQueryable();

		var role = CurrentRole();
		var userId = CurrentUserId();
		var unrestricted = IsUnrestrictedRole(role);

		// --------------------------------------------------------------
		// LIST VISIBILITY - NO NEW DB PROPERTY
		// --------------------------------------------------------------
		// Bulk import creates Active rows with Code/Name/State/Region/HQ
		// while the Sub Dealer form details remain empty.
		//
		// A record becomes list-visible after it is maintained from the
		// Sub Dealer form:
		// - Active records: save requires location/contact/GST/PAN/mFMS data.
		// - Inactive records: those details are optional, so Inactive itself
		//   is enough to represent a submitted/updated record.
		query = query.Where(x =>
			x.Status == SubDealerStatus.InActive ||

			x.Latitude.HasValue ||
			x.Longitude.HasValue ||
			x.DistrictId.HasValue ||
			x.DealerStateId.HasValue ||

			!string.IsNullOrEmpty(x.ShopNoORRoomNoOrBlockNo) ||
			!string.IsNullOrEmpty(x.Village) ||
			!string.IsNullOrEmpty(x.PinCode) ||

			!string.IsNullOrEmpty(x.OfficialContactNumber) ||
			!string.IsNullOrEmpty(x.WhatsAppNumber) ||
			!string.IsNullOrEmpty(x.AlternativeNumber) ||

			!string.IsNullOrEmpty(x.GSTNumber) ||
			!string.IsNullOrEmpty(x.GSTFilePath) ||
			!string.IsNullOrEmpty(x.PANNo) ||

			!string.IsNullOrEmpty(x.WholesaleMFMSId) ||
			!string.IsNullOrEmpty(x.RetailMFMSId));

		// --------------------------------------------------------------
		// SECURITY / DATA SCOPE
		// Apply role scope BEFORE search, count and paging.
		// --------------------------------------------------------------
		if (!unrestricted && IsMoRole(role))
		{
			var effectiveHqId = CurrentHqId() ?? hqId;

			if (!effectiveHqId.HasValue || effectiveHqId.Value <= 0)
				return Ok(EmptyPagedResult(page, pageSize));

			// MO/MDO/JMDO: all saved/updated Sub Dealers in logged-in HQ.
			query = query.Where(x => x.HQ == effectiveHqId.Value);
		}
		else if (!unrestricted && IsRegionRole(role))
		{
			// RM/RMD: all saved/updated Sub Dealers in logged-in Region.
			var effectiveRegionId = CurrentRegionId() ?? regionId;

			if (!effectiveRegionId.HasValue || effectiveRegionId.Value <= 0)
				return Ok(EmptyPagedResult(page, pageSize));

			query = query.Where(x => x.Region == effectiveRegionId.Value);
		}
		else if (!unrestricted && IsStateRole(role))
		{
			// SMM/SMD: all saved/updated Sub Dealers in logged-in State.
			var effectiveStateId = CurrentStateId() ?? stateId;

			if (!effectiveStateId.HasValue || effectiveStateId.Value <= 0)
				return Ok(EmptyPagedResult(page, pageSize));

			query = query.Where(x => x.StateId == effectiveStateId.Value);
		}
		else if (!unrestricted && !string.IsNullOrWhiteSpace(role))
		{
			// Preserve existing fallback behavior for custom restricted roles.
			if (string.IsNullOrWhiteSpace(userId))
				return Ok(EmptyPagedResult(page, pageSize));

			query = query.Where(x => x.CreatedBy == userId);
		}
		else
		{
			// Local/dev fallback when auth claims are unavailable.
			if (hqId.HasValue && hqId.Value > 0)
				query = query.Where(x => x.HQ == hqId.Value);
			else if (regionId.HasValue && regionId.Value > 0)
				query = query.Where(x => x.Region == regionId.Value);
			else if (stateId.HasValue && stateId.Value > 0)
				query = query.Where(x => x.StateId == stateId.Value);
		}

		// --------------------------------------------------------------
		// UI filters can only NARROW the already role-scoped query.
		// --------------------------------------------------------------
		if (stateId.HasValue && stateId.Value > 0)
			query = query.Where(x => x.StateId == stateId.Value);

		if (regionId.HasValue && regionId.Value > 0)
			query = query.Where(x => x.Region == regionId.Value);

		if (hqId.HasValue && hqId.Value > 0)
			query = query.Where(x => x.HQ == hqId.Value);

		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim().ToLower();

			query = query.Where(x =>
				(x.SubDealerCode != null && x.SubDealerCode.ToLower().Contains(term)) ||
				x.FirmName.ToLower().Contains(term) ||
				(x.OfficialContactNumber != null && x.OfficialContactNumber.ToLower().Contains(term)) ||
				(x.WhatsAppNumber != null && x.WhatsAppNumber.ToLower().Contains(term)) ||
				(x.GSTNumber != null && x.GSTNumber.ToLower().Contains(term)) ||
				(x.PANNo != null && x.PANNo.ToLower().Contains(term)) ||
				(x.WholesaleMFMSId != null && x.WholesaleMFMSId.ToLower().Contains(term)) ||
				(x.RetailMFMSId != null && x.RetailMFMSId.ToLower().Contains(term)));
		}

		// Counts follow role + location + search filters, but not the selected
		// status filter. This lets the page show useful Active/Inactive totals.
		var statusCounts = await query
			.GroupBy(x => x.Status)
			.Select(g => new
			{
				Status = g.Key,
				Count = g.Count()
			})
			.ToListAsync(cancellationToken);

		var activeCount = statusCounts
			.FirstOrDefault(x => x.Status == SubDealerStatus.Active)?.Count ?? 0;

		var inactiveCount = statusCounts
			.FirstOrDefault(x => x.Status == SubDealerStatus.InActive)?.Count ?? 0;

		if (status.HasValue &&
			(status.Value == (int)SubDealerStatus.Active ||
			 status.Value == (int)SubDealerStatus.InActive))
		{
			var requestedStatus = (SubDealerStatus)status.Value;
			query = query.Where(x => x.Status == requestedStatus);
		}

		var totalCount = await query.CountAsync(cancellationToken);
		var totalPages = totalCount == 0
			? 0
			: (int)Math.Ceiling(totalCount / (double)pageSize);

		if (totalPages > 0 && page > totalPages)
			page = totalPages;

		var skip = (page - 1) * pageSize;

		var items = await query
			.OrderBy(x => x.FirmName)
			.ThenBy(x => x.SubDealerCode)
			.Skip(skip)
			.Take(pageSize)
			.Select(x => new SubDealerListItemDto
			{
				Id = x.Id,
				SubDealerCode = x.SubDealerCode ?? string.Empty,
				FirmName = x.FirmName,

				StateId = x.StateId,
				RegionId = x.Region,
				HQId = x.HQ,

				Status = x.Status,

				OfficialContactNumber = x.OfficialContactNumber,
				WhatsAppNumber = x.WhatsAppNumber,

				GSTNumber = x.GSTNumber,
				PANNo = x.PANNo,

				WholesaleMFMSId = x.WholesaleMFMSId,
				RetailMFMSId = x.RetailMFMSId,

				UpdatedAt = x.UpdatedAt
			})
			.ToListAsync(cancellationToken);

		return Ok(new SubDealerPagedListResponse
		{
			Items = items,
			Page = page,
			PageSize = pageSize,
			TotalCount = totalCount,
			TotalPages = totalPages,
			ActiveCount = activeCount,
			InactiveCount = inactiveCount
		});
	}

	private static SubDealerPagedListResponse EmptyPagedResult(
		int page,
		int pageSize)
	{
		return new SubDealerPagedListResponse
		{
			Items = new List<SubDealerListItemDto>(),
			Page = page,
			PageSize = pageSize,
			TotalCount = 0,
			TotalPages = 0,
			ActiveCount = 0,
			InactiveCount = 0
		};
	}


	[HttpGet("{id:int}")]
	public async Task<ActionResult<SubDealerFormModel>> GetById(
		int id,
		CancellationToken cancellationToken)
	{
		var entity = await _db.SubDealerRegistrations
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

		if (entity == null)
			return NotFound($"Sub Dealer with Id {id} was not found.");

		return Ok(ToModel(entity));
	}

	[HttpPost]
	public async Task<ActionResult<SubDealerFormModel>> Create(
		[FromBody] SubDealerFormModel model,
		CancellationToken cancellationToken)
	{
		var validationError = ValidateModel(model);
		if (validationError != null)
			return BadRequest(validationError);

		var entity = new SubDealerRegistration();
		ApplyModel(entity, model);

		// Manual creation: generate a Sub Dealer code after insert.
		// Excel import uses the code provided in the master file instead.
		entity.SubDealerCode = null;
		entity.CreatedAt = DateTime.Now;
		entity.UpdatedAt = DateTime.Now;

		_db.SubDealerRegistrations.Add(entity);
		await _db.SaveChangesAsync(cancellationToken);

		entity.SubDealerCode = $"SD{entity.Id:D6}";
		await _db.SaveChangesAsync(cancellationToken);

		return Ok(ToModel(entity));
	}

	[HttpPut("{id:int}")]
	public async Task<ActionResult<SubDealerFormModel>> Update(
		int id,
		[FromBody] SubDealerFormModel model,
		CancellationToken cancellationToken)
	{
		if (id != model.Id)
			return BadRequest("Route Id and model Id do not match.");

		var validationError = ValidateModel(model);
		if (validationError != null)
			return BadRequest(validationError);

		var entity = await _db.SubDealerRegistrations
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

		if (entity == null)
			return NotFound($"Sub Dealer with Id {id} was not found.");

		// Code is immutable from normal edit screen.
		ApplyModel(entity, model);
		entity.UpdatedAt = DateTime.Now;

		await _db.SaveChangesAsync(cancellationToken);
		return Ok(ToModel(entity));
	}

	/// <summary>
	/// Admin / MO roles: reads the Excel master and returns the five required text columns.
	/// State/Region/HQ names are mapped to IDs by the UI using the same LookupCache
	/// already used by the Register page.
	/// </summary>
	[Authorize(Roles = "Admin,MO,MDO,JMDO")]
	[HttpPost("parse-import-excel")]
	[RequestSizeLimit(MaxExcelImportSize)]
	public async Task<ActionResult<SubDealerExcelParseResponse>> ParseImportExcel(
		IFormFile file,
		CancellationToken cancellationToken)
	{
		var result = new SubDealerExcelParseResponse();

		if (file == null || file.Length == 0)
			return BadRequest("Excel file is required.");

		if (file.Length > MaxExcelImportSize)
			return BadRequest("Excel file size must be less than 10 MB.");

		if (!string.Equals(
				Path.GetExtension(file.FileName),
				".xlsx",
				StringComparison.OrdinalIgnoreCase))
		{
			return BadRequest("Only .xlsx Excel files are allowed.");
		}

		try
		{
			await using var source = file.OpenReadStream();
			using var memory = new MemoryStream();
			await source.CopyToAsync(memory, cancellationToken);
			memory.Position = 0;

			using var workbook = new XLWorkbook(memory);
			var worksheet = workbook.Worksheets.FirstOrDefault();

			if (worksheet == null)
				return BadRequest("The Excel workbook does not contain a worksheet.");

			var firstRow = worksheet.FirstRowUsed();
			var lastRow = worksheet.LastRowUsed();

			if (firstRow == null || lastRow == null)
				return BadRequest("The Excel worksheet is empty.");

			var headerRowNumber = firstRow.RowNumber();
			var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			foreach (var cell in firstRow.CellsUsed())
			{
				var header = NormalizeHeader(cell.GetString());
				if (!string.IsNullOrWhiteSpace(header))
					headerMap[header] = cell.Address.ColumnNumber;
			}

			var requiredHeaders = new[]
			{
				"SUBDEALERCODE",
				"SUBDEALERNAME",
				"HQ",
				"REGION",
				"STATE"
			};

			var missingHeaders = requiredHeaders
				.Where(x => !headerMap.ContainsKey(x))
				.ToList();

			if (missingHeaders.Count > 0)
			{
				result.Errors.Add(
					"Missing required Excel column(s): " +
					string.Join(", ", missingHeaders.Select(ToDisplayHeader)));
				return Ok(result);
			}

			var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			for (var rowNumber = headerRowNumber + 1;
				 rowNumber <= lastRow.RowNumber();
				 rowNumber++)
			{
				var row = worksheet.Row(rowNumber);

				var code = row.Cell(headerMap["SUBDEALERCODE"]).GetString().Trim();
				var name = row.Cell(headerMap["SUBDEALERNAME"]).GetString().Trim();
				var hq = row.Cell(headerMap["HQ"]).GetString().Trim();
				var region = row.Cell(headerMap["REGION"]).GetString().Trim();
				var state = row.Cell(headerMap["STATE"]).GetString().Trim();

				// Ignore completely blank rows.
				if (string.IsNullOrWhiteSpace(code) &&
					string.IsNullOrWhiteSpace(name) &&
					string.IsNullOrWhiteSpace(hq) &&
					string.IsNullOrWhiteSpace(region) &&
					string.IsNullOrWhiteSpace(state))
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(code))
					result.Errors.Add($"Excel row {rowNumber}: Sub Dealer Code is required.");

				if (string.IsNullOrWhiteSpace(name))
					result.Errors.Add($"Excel row {rowNumber}: Sub Dealer Name is required.");

				if (string.IsNullOrWhiteSpace(hq))
					result.Errors.Add($"Excel row {rowNumber}: HQ is required.");

				if (string.IsNullOrWhiteSpace(region))
					result.Errors.Add($"Excel row {rowNumber}: Region is required.");

				if (string.IsNullOrWhiteSpace(state))
					result.Errors.Add($"Excel row {rowNumber}: State is required.");

				if (!string.IsNullOrWhiteSpace(code) && !seenCodes.Add(code))
					result.Errors.Add($"Excel row {rowNumber}: duplicate Sub Dealer Code '{code}'.");

				result.Rows.Add(new SubDealerExcelRowDto
				{
					RowNumber = rowNumber,
					SubDealerCode = code,
					SubDealerName = name,
					HQ = hq,
					Region = region,
					State = state
				});
			}

			result.TotalRows = result.Rows.Count;
			return Ok(result);
		}
		catch (Exception ex)
		{
			return BadRequest($"Unable to read Excel file: {ex.Message}");
		}
	}

	/// <summary>
	/// Admin / MO atomic upsert. Existing code => update name + State/Region/HQ only.
	/// New code => insert a new Active Sub Dealer with empty detail fields.
	/// </summary>
	[Authorize(Roles = "Admin,MO,MDO,JMDO")]
	[HttpPost("bulk-import")]
	public async Task<ActionResult<SubDealerBulkImportResponse>> BulkImport(
		[FromBody] SubDealerBulkImportRequest request,
		CancellationToken cancellationToken)
	{
		var result = new SubDealerBulkImportResponse();

		var role = CurrentRole();
		var isMoImport = IsMoRole(role);
		var currentHqId = CurrentHqId();

		if (isMoImport && (!currentHqId.HasValue || currentHqId.Value <= 0))
		{
			result.Errors.Add("Your login does not have a valid HQ mapping. Excel import is not allowed.");
			return BadRequest(result);
		}

		if (request.Rows == null || request.Rows.Count == 0)
			return BadRequest("No Sub Dealer rows were supplied for import.");

		var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var row in request.Rows)
		{
			var code = row.SubDealerCode?.Trim() ?? string.Empty;
			var name = row.FirmName?.Trim() ?? string.Empty;

			if (string.IsNullOrWhiteSpace(code))
				result.Errors.Add($"Excel row {row.ExcelRowNumber}: Sub Dealer Code is required.");

			if (string.IsNullOrWhiteSpace(name))
				result.Errors.Add($"Excel row {row.ExcelRowNumber}: Sub Dealer Name is required.");

			if (row.StateId <= 0)
				result.Errors.Add($"Excel row {row.ExcelRowNumber}: State mapping is invalid.");

			if (row.RegionId <= 0)
				result.Errors.Add($"Excel row {row.ExcelRowNumber}: Region mapping is invalid.");

			if (row.HQId <= 0)
				result.Errors.Add($"Excel row {row.ExcelRowNumber}: HQ mapping is invalid.");

			if (isMoImport && currentHqId.HasValue && row.HQId != currentHqId.Value)
			{
				result.Errors.Add(
					$"Excel row {row.ExcelRowNumber}: HQ is outside your assigned MO HQ. Import is allowed only for HQ Id {currentHqId.Value}.");
			}

			if (!string.IsNullOrWhiteSpace(code) && !seenCodes.Add(code))
				result.Errors.Add($"Excel row {row.ExcelRowNumber}: duplicate Sub Dealer Code '{code}'.");
		}

		if (result.Errors.Count > 0)
			return BadRequest(result);

		var codes = request.Rows
			.Select(x => x.SubDealerCode.Trim().ToUpperInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		var existingList = await _db.SubDealerRegistrations
			.Where(x => x.SubDealerCode != null && codes.Contains(x.SubDealerCode.ToUpper()))
			.ToListAsync(cancellationToken);

		var existingByCode = existingList
			.Where(x => !string.IsNullOrWhiteSpace(x.SubDealerCode))
			.ToDictionary(
				x => x.SubDealerCode!.Trim(),
				StringComparer.OrdinalIgnoreCase);

		if (isMoImport && currentHqId.HasValue)
		{
			foreach (var row in request.Rows)
			{
				var code = row.SubDealerCode.Trim();
				if (existingByCode.TryGetValue(code, out var existing) &&
					existing.HQ != currentHqId.Value)
				{
					result.Errors.Add(
						$"Excel row {row.ExcelRowNumber}: Sub Dealer Code '{code}' belongs to another HQ and cannot be updated by this MO.");
				}
			}

			if (result.Errors.Count > 0)
				return BadRequest(result);
		}

		await using var transaction =
			await _db.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			var now = DateTime.Now;
			var importedBy = string.IsNullOrWhiteSpace(request.ImportedBy)
				? (isMoImport
					? CurrentUserId() ?? "MO Excel Import"
					: "Admin Excel Import")
				: request.ImportedBy.Trim();

			foreach (var row in request.Rows)
			{
				var code = row.SubDealerCode.Trim().ToUpperInvariant();
				var firmName = row.FirmName.Trim().ToUpperInvariant();

				if (existingByCode.TryGetValue(code, out var existing))
				{
					// Preserve contact/GST/location/trade-deposit/status data already
					// maintained in the form. Excel master controls only these fields.
					existing.FirmName = firmName;
					existing.StateId = row.StateId;
					existing.Region = row.RegionId;
					existing.HQ = row.HQId;
					existing.UpdatedBy = importedBy;
					existing.UpdatedAt = now;
					result.Updated++;
				}
				else
				{
					var entity = new SubDealerRegistration
					{
						SubDealerCode = code,
						FirmName = firmName,
						StateId = row.StateId,
						Region = row.RegionId,
						HQ = row.HQId,
						Status = SubDealerStatus.Active,

						// Remaining details will be maintained from the Sub Dealer page.
						ShopNoORRoomNoOrBlockNo = string.Empty,
						Village = string.Empty,
						PinCode = string.Empty,
						OfficialContactNumber = string.Empty,
						WhatsAppNumber = string.Empty,

						CreatedBy = importedBy,
						CreatedAt = now,
						UpdatedBy = importedBy,
						UpdatedAt = now
					};

					_db.SubDealerRegistrations.Add(entity);
					existingByCode[code] = entity;
					result.Inserted++;
				}
			}

			await _db.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			return Ok(result);
		}
		catch (Exception ex)
		{
			await transaction.RollbackAsync(cancellationToken);
			result.Errors.Add($"Database import failed: {ex.Message}");
			return BadRequest(result);
		}
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(
		int id,
		CancellationToken cancellationToken)
	{
		var entity = await _db.SubDealerRegistrations
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

		if (entity == null)
			return NotFound();

		_db.SubDealerRegistrations.Remove(entity);
		await _db.SaveChangesAsync(cancellationToken);

		return NoContent();
	}

	private static string NormalizeHeader(string? header)
	{
		if (string.IsNullOrWhiteSpace(header))
			return string.Empty;

		return new string(header
			.Trim()
			.Where(char.IsLetterOrDigit)
			.Select(char.ToUpperInvariant)
			.ToArray());
	}

	private static string ToDisplayHeader(string normalizedHeader)
	{
		return normalizedHeader switch
		{
			"SUBDEALERCODE" => "Sub Dealer Code",
			"SUBDEALERNAME" => "Sub Dealer Name",
			"HQ" => "HQ",
			"REGION" => "Region",
			"STATE" => "State",
			_ => normalizedHeader
		};
	}

	private static string? ValidateModel(SubDealerFormModel model)
	{
		if (model.StateId <= 0)
			return "State is required.";
		if (model.Region <= 0)
			return "Region is required.";
		if (model.HQ <= 0)
			return "HQ is required.";
		if (string.IsNullOrWhiteSpace(model.FirmName))
			return "Firm Name is required.";
		if (model.Status is not SubDealerStatus.Active and not SubDealerStatus.InActive)
			return "Only Active or Inactive status is allowed.";

		if (model.Status == SubDealerStatus.Active)
		{
			// Wholesale mFMS ID is optional for Active Sub Dealers.
			// Retail mFMS ID keeps the existing mandatory rule.
			if (string.IsNullOrWhiteSpace(model.RetailMFMSId))
				return "Retail mFMS ID is required for Active Sub Dealer.";

			if (string.IsNullOrWhiteSpace(model.PANNo))
				return "PAN No is required for Active Sub Dealer.";
		}

		// PAN is optional for Inactive, but if entered the format is still validated.
		if (!string.IsNullOrWhiteSpace(model.PANNo) &&
			!Regex.IsMatch(
				model.PANNo.Trim().ToUpperInvariant(),
				@"^[A-Z]{5}[0-9]{4}[A-Z]$"))
		{
			return "Invalid PAN format (e.g., ABCDE1234F).";
		}

		return null;
	}

	private static void ApplyModel(
		SubDealerRegistration entity,
		SubDealerFormModel model)
	{
		entity.StateId = model.StateId;
		entity.Region = model.Region;
		entity.HQ = model.HQ;
		entity.Status = model.Status;

		entity.FirmName = model.FirmName.Trim().ToUpperInvariant();

		entity.GoogleMapURL = model.GoogleMapURL;
		entity.Latitude = model.Latitude;
		entity.Longitude = model.Longitude;
		entity.ShopNoORRoomNoOrBlockNo = model.ShopNoORRoomNoOrBlockNo ?? string.Empty;
		entity.Street = model.Street;
		entity.SubVillage = model.SubVillage;
		entity.Village = model.Village ?? string.Empty;
		entity.PinCode = model.PinCode ?? string.Empty;
		entity.Block = model.Block;
		entity.Taluk = model.Taluk;
		entity.DistrictId = model.DistrictId;
		entity.DealerStateId = model.DealerStateId;
		entity.OfficialContactNumber = model.OfficialContactNumber ?? string.Empty;
		entity.WhatsAppNumber = model.WhatsAppNumber ?? string.Empty;
		entity.AlternativeNumber = model.AlternativeNumber;

		entity.WholesaleMFMSId = string.IsNullOrWhiteSpace(model.WholesaleMFMSId)
			? null
			: model.WholesaleMFMSId.Trim();

		entity.RetailMFMSId = string.IsNullOrWhiteSpace(model.RetailMFMSId)
			? null
			: model.RetailMFMSId.Trim();

		entity.PANNo = string.IsNullOrWhiteSpace(model.PANNo)
			? null
			: model.PANNo.Trim().ToUpperInvariant();

		entity.GSTNumber = model.GSTNumber?.Trim().ToUpperInvariant();
		entity.GSTLegalName = model.GSTLegalName;
		entity.GSTTradeName = model.GSTTradeName;
		entity.GSTConstitutionofBusiness = model.GSTConstitutionofBusiness;
		entity.GSTFilePath = model.GSTFilePath;

		entity.CreatedBy ??= model.CreatedBy;
		entity.UpdatedBy = model.UpdatedBy;
	}

	private static SubDealerFormModel ToModel(SubDealerRegistration entity)
	{
		return new SubDealerFormModel
		{
			Id = entity.Id,
			SubDealerCode = entity.SubDealerCode,

			StateId = entity.StateId,
			Region = entity.Region,
			HQ = entity.HQ,
			Status = entity.Status,

			FirmName = entity.FirmName,

			GoogleMapURL = entity.GoogleMapURL,
			Latitude = entity.Latitude,
			Longitude = entity.Longitude,
			ShopNoORRoomNoOrBlockNo = entity.ShopNoORRoomNoOrBlockNo,
			Street = entity.Street,
			SubVillage = entity.SubVillage,
			Village = entity.Village,
			PinCode = entity.PinCode,
			Block = entity.Block,
			Taluk = entity.Taluk,
			DistrictId = entity.DistrictId,
			DealerStateId = entity.DealerStateId,
			OfficialContactNumber = entity.OfficialContactNumber,
			WhatsAppNumber = entity.WhatsAppNumber,
			AlternativeNumber = entity.AlternativeNumber,

			WholesaleMFMSId = entity.WholesaleMFMSId,
			RetailMFMSId = entity.RetailMFMSId,
			PANNo = entity.PANNo,

			GSTNumber = entity.GSTNumber,
			GSTLegalName = entity.GSTLegalName,
			GSTTradeName = entity.GSTTradeName,
			GSTConstitutionofBusiness = entity.GSTConstitutionofBusiness,
			GSTFilePath = entity.GSTFilePath,

			CreatedBy = entity.CreatedBy,
			CreatedAt = entity.CreatedAt,
			UpdatedBy = entity.UpdatedBy,
			UpdatedAt = entity.UpdatedAt
		};
	}
}