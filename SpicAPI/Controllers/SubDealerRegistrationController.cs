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

	private readonly AppDbContext	 _db;

	public SubDealerRegistrationController(AppDbContext db)
	{
		_db = db;
	}

	[HttpGet("lookup")]
	public async Task<ActionResult<List<SubDealerLookupDto>>> Lookup(
		CancellationToken cancellationToken)
	{
		var items = await _db.SubDealerRegistrations
			.AsNoTracking()
			.OrderBy(x => x.SubDealerCode)
			.Select(x => new SubDealerLookupDto
			{
				Id = x.Id,
				SubDealerCode = x.SubDealerCode ?? string.Empty,
				FirmName = x.FirmName,
				StateId = x.StateId
			})
			.ToListAsync(cancellationToken);

		return Ok(items);
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
	/// Admin-only: reads the Excel master and returns the five required text columns.
	/// State/Region/HQ names are mapped to IDs by the UI using the same LookupCache
	/// already used by the Register page.
	/// </summary>
	[Authorize(Roles = "Admin")]
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
	/// Admin-only atomic upsert. Existing code => update name + State/Region/HQ only.
	/// New code => insert a new Active Sub Dealer with empty detail fields.
	/// </summary>
	[Authorize(Roles = "Admin")]
	[HttpPost("bulk-import")]
	public async Task<ActionResult<SubDealerBulkImportResponse>> BulkImport(
		[FromBody] SubDealerBulkImportRequest request,
		CancellationToken cancellationToken)
	{
		var result = new SubDealerBulkImportResponse();

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

		await using var transaction =
			await _db.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			var now = DateTime.Now;
			var importedBy = string.IsNullOrWhiteSpace(request.ImportedBy)
				? "Admin Excel Import"
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