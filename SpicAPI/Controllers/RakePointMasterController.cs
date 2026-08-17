using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Rake Point Master API. Mirrors the existing PVTMasterController endpoints
	/// (all / search / save) and reuses the Excel bulk-upload pattern from
	/// AgricultureBulkUploadController. The RakePointMasters table is created
	/// manually, so no EF migration is involved.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class RakePointMasterController : ControllerBase
	{
		private readonly AppDbContext _context;

		public RakePointMasterController(AppDbContext context)
		{
			_context = context;
		}

		/// <summary>
		/// Get all active Rake Point Master records for dropdown/list.
		/// Mirrors PVTMasterController.GetAll.
		/// </summary>
		[HttpGet("all")]
		public IActionResult GetAll()
		{
			var records = _context.RakePointMasters
				.Where(x => x.IsActive)
				.Select(x => new
				{
					x.Id,
					x.RakePointCode,
					x.Name
				})
				.OrderBy(x => x.Name)
				.ToList();

			return Ok(records);
		}

		/// <summary>
		/// Search Rake Point Master by code or name.
		/// </summary>
		[HttpGet("search")]
		public IActionResult Search(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				return BadRequest(new { message = "Query is required" });

			var records = _context.RakePointMasters
				.Where(x => x.IsActive &&
					(x.RakePointCode.Contains(query) || x.Name.Contains(query)))
				.Select(x => new
				{
					x.Id,
					x.RakePointCode,
					x.Name
				})
				.OrderBy(x => x.Name)
				.Take(20)
				.ToList();

			return Ok(records);
		}

		/// <summary>
		/// Save a single Rake Point Master record.
		/// </summary>
		[HttpPost("save")]
		public async Task<IActionResult> SaveRakePointMaster([FromBody] RakePointMasterSaveDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.RakePointCode) || string.IsNullOrWhiteSpace(dto.Name))
				return BadRequest(new { message = "RakePointCode and Name are required" });

			try
			{
				var rakePointMaster = new RakePointMaster
				{
					RakePointCode = dto.RakePointCode.Trim(),
					Name = dto.Name.Trim(),
					IsActive = true,
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now,
					CreatedBy = "System",
					UpdatedBy = "System"
				};

				_context.RakePointMasters.Add(rakePointMaster);
				await _context.SaveChangesAsync();

				return Ok(new
				{
					message = "Rake Point Master saved successfully",
					id = rakePointMaster.Id
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = $"Save failed: {ex.Message}" });
			}
		}

		// POST /api/RakePointMaster/bulk-upload
		// Excel template columns: "Rakepoint Code" and "Name".
		[HttpPost("bulk-upload")]
		public async Task<IActionResult> BulkUpload(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { message = "No file uploaded" });

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (ext != ".xlsx" && ext != ".xls")
				return BadRequest(new { message = "Only Excel files (.xlsx/.xls) are supported" });

			using var stream = file.OpenReadStream();
			using var workbook = new XLWorkbook(stream);
			var worksheet = workbook.Worksheets.First();

			var headerRow = worksheet.Row(1);
			var lastHeaderCell = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
			if (lastHeaderCell == 0)
				return BadRequest(new { message = "Empty worksheet or missing header row" });

			var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int c = 1; c <= lastHeaderCell; c++)
			{
				var n = NormalizeHeader(headerRow.Cell(c).GetString());
				if (!string.IsNullOrEmpty(n) && !headerMap.ContainsKey(n))
					headerMap[n] = c;
			}

			AddAliasEntries(headerMap);

			var expected = new[] { "rakepointcode", "name" };
			var missing = expected.Where(h => !headerMap.ContainsKey(h)).ToList();
			if (missing.Any())
				return BadRequest(new { message = $"Invalid template. Missing columns: {string.Join(", ", missing)}" });

			var dataRows = worksheet.RowsUsed().Skip(1).ToList();
			var now = DateTime.Now;
			var insertedCount = 0;
			var updatedCount = 0;
			var rejectedRecords = new List<RejectedRecord>();

			// Pre-load existing rows keyed by code so re-uploads update instead of duplicating.
			var existingByCode = new Dictionary<string, RakePointMaster>(StringComparer.OrdinalIgnoreCase);
			foreach (var existing in _context.RakePointMasters)
				existingByCode[existing.RakePointCode.Trim()] = existing;

			try
			{
				foreach (var row in dataRows)
				{
					var code = GetCellString(row, headerMap, "rakepointcode");
					var name = GetCellString(row, headerMap, "name");

					if (string.IsNullOrWhiteSpace(code))
					{
						rejectedRecords.Add(new RejectedRecord(row.RowNumber(), code, "RakePoint Code is empty"));
						continue;
					}

					if (string.IsNullOrWhiteSpace(name))
					{
						rejectedRecords.Add(new RejectedRecord(row.RowNumber(), code, "Name is empty"));
						continue;
					}

					var key = code.Trim();
					if (existingByCode.TryGetValue(key, out var existing))
					{
						existing.Name = name.Trim();
						existing.IsActive = true;
						existing.UpdatedAt = now;
						existing.UpdatedBy = "bulk-upload";
						updatedCount++;
					}
					else
					{
						var ent = new RakePointMaster
						{
							RakePointCode = key,
							Name = name.Trim(),
							IsActive = true,
							CreatedAt = now,
							UpdatedAt = now,
							CreatedBy = "bulk-upload",
							UpdatedBy = "bulk-upload"
						};
						_context.RakePointMasters.Add(ent);
						existingByCode[key] = ent;
						insertedCount++;
					}
				}

				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Bulk upload failed: " + ex.Message });
			}

			var response = new BulkUploadResponse
			{
				TotalRecords = dataRows.Count,
				InsertedCount = insertedCount,
				UpdatedCount = updatedCount,
				RejectedCount = rejectedRecords.Count,
				RejectedRecords = rejectedRecords
			};

			return Ok(response);
		}

		// GET /api/RakePointMaster/sample-template
		[HttpGet("sample-template")]
		public IActionResult SampleTemplate()
		{
			(string Header, string Sample)[] columns = new[]
			{
				("Rakepoint Code", "PABS"),
				("Name", "Abohar Rkpt")
			};

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

				ws.Cell(2, i + 1).Value = columns[i].Sample;
			}

			ws.Columns().AdjustToContents();
			ws.SheetView.FreezeRows(1);

			using var ms = new MemoryStream();
			wb.SaveAs(ms);
			var bytes = ms.ToArray();

			return File(bytes,
				"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				"RakePointMaster_Sample_Template.xlsx");
		}

		private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string key)
		{
			if (!headerMap.TryGetValue(key, out var col)) return string.Empty;
			return row.Cell(col).GetString().Trim();
		}

		private static string NormalizeHeader(string h) =>
			(h ?? string.Empty).Trim().Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();

		private static void AddAliasEntries(Dictionary<string, int> headerMap)
		{
			var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "code", "rakepointcode" },
				{ "rkptcode", "rakepointcode" },
				{ "rakepoint", "rakepointcode" }
			};

			foreach (var kvp in headerMap.ToList())
			{
				if (aliases.TryGetValue(kvp.Key, out var alias) && !headerMap.ContainsKey(alias))
					headerMap[alias] = kvp.Value;
			}
		}
	}

	public class RakePointMasterSaveDto
	{
		public string RakePointCode { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
	}
}
