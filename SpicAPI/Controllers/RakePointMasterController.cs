using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
	[Authorize]
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
				x.Name,
				x.StateId
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
					x.Name,
					x.StateId
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

		// POST /api/RakePointMaster/map-state
		// Only the State mapping is editable. The SAP Code is never changed.
		[HttpPost("map-state")]
		public async Task<IActionResult> MapState([FromBody] RakePointMapStateDto dto)
		{
			if (dto == null || dto.Id <= 0)
				return BadRequest(new { message = "Invalid request" });

			try
			{
				var record = await _context.RakePointMasters
					.FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsActive);

				if (record == null)
					return NotFound(new { message = "SAP Code record not found." });

				if (dto.StateId.HasValue &&
					dto.StateId.Value > 0 &&
					!(await _context.States.AnyAsync(s => s.Id == dto.StateId.Value)))
					return BadRequest(new { message = "Selected State does not exist." });

				var now = DateTime.Now;
				var userName = User?.Identity?.Name ?? "System";

				record.StateId = dto.StateId.HasValue && dto.StateId.Value > 0
					? dto.StateId.Value
					: (int?)null;
				record.UpdatedAt = now;
				record.UpdatedBy = userName;

				await _context.SaveChangesAsync();

				return Ok(new
				{
					message = "State mapping saved successfully.",
					id = record.Id,
					stateId = record.StateId
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = $"Save failed: {ex.Message}" });
			}
		}

		// POST /api/RakePointMaster/bulk-upload
		// Excel template columns: "Rakepoint Code", "Name" and an optional "State".
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
			// Do not assume the first worksheet: the active table may live in another
			// sheet (e.g. a leading "Instructions"/"Read Me" tab). Skip worksheets
			// without any used row and pick the first one (in tab order) whose header
			// row is recognized. Header titles are matched after normalization
			// (lowercase; trim + strip ALL whitespace, underscores and hyphens), so
			// "Rake Point Code", "Rake_Point_Code", "Rake-Point-Code" and
			// "Rakepoint Code" all map to "rakepointcode".
			IXLWorksheet? worksheet = null;
			var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var headerRowNumber = 0;
			var sheetDebug = new List<object>();

			foreach (var ws in workbook.Worksheets)
			{
				var lastRow = ws.LastRowUsed();
				if (lastRow == null)
				{
					sheetDebug.Add(new { name = ws.Name, rows = new List<object>() });
					continue;
				}

				var scanLimit = Math.Min(lastRow.RowNumber(), 10);
				var rowsDebug = new List<object>();
				var candidates = new List<Dictionary<string, int>>();

				for (int r = 1; r <= scanLimit; r++)
				{
					var row = ws.Row(r);
					var lastCell = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
					var values = new List<string>();
					var candidate = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

					for (int c = 1; c <= lastCell; c++)
					{
						var raw = row.Cell(c).GetString();
						values.Add(raw);

						var n = NormalizeHeader(raw);
						if (!string.IsNullOrEmpty(n) && !candidate.ContainsKey(n))
							candidate[n] = c;
					}

					AddAliasEntries(candidate);
					candidates.Add(candidate);

					rowsDebug.Add(new
					{
						row = r,
						values,
						normalized = candidate.Select(kvp => $"{kvp.Key}=C{kvp.Value}").ToList()
					});
				}

				for (int r = 0; r < candidates.Count && worksheet == null; r++)
				{
					if (candidates[r].ContainsKey("rakepointcode") && candidates[r].ContainsKey("name"))
					{
						headerMap = candidates[r];
						headerRowNumber = r + 1;
						worksheet = ws;
					}
				}

				sheetDebug.Add(new { name = ws.Name, rows = rowsDebug });

				if (worksheet != null)
					break;
			}

			if (worksheet == null)
			{
				return BadRequest(new
				{
					message = "Empty worksheet or missing header row",
					debug = new
					{
						sheets = sheetDebug,
						note = "A header row containing both 'rakepointcode' and 'name' was not found in the first 10 rows of any worksheet."
					}
				});
			}

			var dataRows = worksheet.RowsUsed().Where(row => row.RowNumber() > headerRowNumber).ToList();
			var now = DateTime.Now;
			var insertedCount = 0;
			var updatedCount = 0;
			var totalRows = 0;
			var rejectedRecords = new List<RejectedRecord>();
			var duplicateRecords = new List<object>();
			var uploadedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// Pre-load existing rows keyed by code to detect database duplicates.
			var existingByCode = new Dictionary<string, RakePointMaster>(StringComparer.OrdinalIgnoreCase);
			foreach (var existing in _context.RakePointMasters)
				existingByCode[existing.RakePointCode.Trim()] = existing;

			// Pre-load the State master to resolve the optional "State" column.
			var stateIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var state in _context.States)
			{
				if (!string.IsNullOrWhiteSpace(state.StateName))
					stateIdByName[state.StateName.Trim()] = state.Id;
			}

			try
			{
				foreach (var row in dataRows)
				{
					var code = GetCellString(row, headerMap, "rakepointcode");
					var name = GetCellString(row, headerMap, "name");
					// Optional State column (blank when the workbook has no State header).
					var stateName = GetCellString(row, headerMap, "state");

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

					totalRows++;

					var key = code.Trim();

					// Duplicate SAP Code within the same uploaded Excel file
					if (!uploadedCodes.Add(key))
					{
						duplicateRecords.Add(new
						{
							rowNumber = row.RowNumber(),
							sapCode = key,
							rakePointName = name.Trim(),
							reason = "Duplicate SAP Code in uploaded file"
						});
						continue;
					}

					// Resolve the optional State column. Empty State -> StateId is left
					// unchanged (or null for inserts). Unknown State name -> skip only
					// this row and report the missing State.
					int? resolvedStateId = null;

					if (!string.IsNullOrWhiteSpace(stateName))
					{
						if (!stateIdByName.TryGetValue(
							stateName,
							out var stateId))
						{
							duplicateRecords.Add(new
							{
								rowNumber = row.RowNumber(),
								sapCode = key,
								rakePointName = name.Trim(),
								reason = $"Invalid State: '{stateName}' not found in State Master"
							});
							continue;
						}

						resolvedStateId = stateId;
					}

					// Existing RakePoint Code -> update StateId only. The existing
					// Code and Name are never changed.
					if (existingByCode.TryGetValue(
						key,
						out var existingRecord))
					{
						if (resolvedStateId.HasValue &&
							existingRecord.StateId != resolvedStateId.Value)
						{
							existingRecord.StateId = resolvedStateId.Value;
							existingRecord.UpdatedAt = now;
							existingRecord.UpdatedBy = "bulk-upload";
						}

						updatedCount++;
					}
					else
					{
						var ent = new RakePointMaster
						{
							RakePointCode = key,
							Name = name.Trim(),
							StateId = resolvedStateId,
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
				TotalRecords = totalRows,
				InsertedCount = insertedCount,
				UpdatedCount = updatedCount,
				RejectedCount = rejectedRecords.Count,
				RejectedRecords = rejectedRecords,
				DuplicateRecords = duplicateRecords
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
				("Name", "Abohar Rkpt"),
				("State", "Punjab")
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
			string.IsNullOrWhiteSpace(h) ? string.Empty : new string(h
				.Trim()
				.Where(ch => !char.IsWhiteSpace(ch) && ch != '_' && ch != '-')
				.Select(char.ToLowerInvariant)
				.ToArray());

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

	public class RakePointMapStateDto
	{
		public int Id { get; set; }
		public int? StateId { get; set; }
	}
}
