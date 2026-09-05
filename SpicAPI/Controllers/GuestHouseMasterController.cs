using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Guest House + Room master data management (Settings > Guest House), admin only.
	///
	/// The UI works in two steps on the same page (tabbed, SubDealerEmployeeMaster style):
	///   1) Guest House tab  - create/update the guest house records first.
	///   2) Rooms tab        - map rooms to the selected guest house.
	///
	/// Bulk upload files:
	///   type=house  -> Name | Address | PhoneNumber | Description
	///   type=rooms  -> GuestHouseName | RoomType | RoomNumber | NoOfRooms | Rate
	///
	/// Upsert behaviour: matching (Guest House + RoomType + RoomNumber) room rows are
	/// updated in place, new ones are inserted. Nothing is ever deleted by the upload.
	/// </summary>
	[Authorize(Roles = "Admin,CorporateAdmin")]
	[ApiController]
	[Route("api/[controller]")]
	public class GuestHouseMasterController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly ILogger<GuestHouseMasterController> _logger;

		public GuestHouseMasterController(AppDbContext db, ILogger<GuestHouseMasterController> logger)
		{
			_db = db;
			_logger = logger;
		}

		// =====================================================================
		// BULK UPLOAD (SubDealerEmployeeController pattern)
		// =====================================================================

		// POST /api/GuestHouseMaster/bulk-upload?type=house|rooms
		[HttpPost("bulk-upload")]
		public async Task<IActionResult> BulkUpload([FromQuery] string type, IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { Success = false, Message = "No file uploaded" });

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (ext != ".xlsx" && ext != ".xls")
				return BadRequest(new { Success = false, Message = "Only Excel files (.xlsx/.xls) are supported" });

			var t = (type ?? "").Trim().ToLowerInvariant();
			if (t != "house" && t != "rooms" && t != "room")
				return BadRequest(new { Success = false, Message = "Unknown type. Use house or rooms" });

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

			string[] requiredHeaders = t == "house"
				? ["name"]
				: ["guesthousename", "roomtype", "noofrooms", "rate"];

			var missingList = requiredHeaders
				.Where(h => !headerMap.ContainsKey(h))
				.Select(PrettyHeader)
				.ToList();

			if (missingList.Any())
				return BadRequest(new { Success = false, Message = "Invalid template. Missing columns", Missing = missingList });

			var rows = worksheet.RowsUsed().Skip(1).ToList();
			if (rows.Count == 0)
				return BadRequest(new { Success = false, Message = "Worksheet has no data rows" });

			var now = DateTime.UtcNow;

			using var tx = await _db.Database.BeginTransactionAsync();
			int insertedCount = 0;
			int updatedCount = 0;
			try
			{
				if (t == "house")
				{
					var result = await BulkUpsertHousesAsync(rows, headerMap, AddGrouped, now);
					insertedCount = result.Inserted;
					updatedCount = result.Updated;
				}
				else
				{
					var result = await BulkUpsertRoomsAsync(rows, headerMap, AddGrouped, now);
					insertedCount = result.Inserted;
					updatedCount = result.Updated;
				}

				await _db.SaveChangesAsync();
				await tx.CommitAsync();
			}
			catch (Exception ex)
			{
				await tx.RollbackAsync();
				_logger.LogError(ex, "Guest House bulk upload failed");
				return StatusCode(500, new { Success = false, Message = "Bulk upload failed", Error = ex.Message });
			}

			var totalSkipped = groupedErrors.Values.Sum(v => v.Count);
			return Ok(new
			{
				Success = true,
				Message = totalSkipped > 0
					? $"Upload completed. {insertedCount} inserted, {updatedCount} updated, {totalSkipped} skipped."
					: $"Upload completed successfully. {insertedCount} inserted, {updatedCount} updated.",
				InsertedCount = insertedCount,
				UpdatedCount = updatedCount,
				GroupedErrors = groupedErrors,
				TotalSkipped = totalSkipped
			});
		}

		// =====================================================================
		// SAMPLE TEMPLATE
		// =====================================================================

		// GET /api/GuestHouseMaster/sample-template?type=house|rooms
		[HttpGet("sample-template")]
		public IActionResult SampleTemplate([FromQuery] string type)
		{
			var t = (type ?? "").Trim().ToLowerInvariant();

			(string Header, string Sample)[] columns = t == "house"
				?
				[
					("Name", "T-Nagar Guest House"),
					("Address", "30, Whites Road, T-Nagar, Chennai"),
					("PhoneNumber", "044-2815 0000"),
					("Description", "Corporate guest house")
				]
				:
				[
					("GuestHouseName", "T-Nagar Guest House"),
					("RoomType", "Non AC"),
					("RoomNumber", "101"),
					("NoOfRooms", "4"),
					("Rate", "750")
				];

			if (t != "house" && t != "rooms" && t != "room")
				return BadRequest(new { Success = false, Message = "Unknown type. Use house or rooms" });

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
				t == "house" ? "GuestHouse_Sample_Template.xlsx" : "GuestHouseRooms_Sample_Template.xlsx");
		}

		// =====================================================================
		// READS for the Settings master page
		// =====================================================================

		// GET /api/GuestHouseMaster/houses
		[HttpGet("houses")]
		public async Task<ActionResult<List<GuestHouseDto>>> GetAllHouses()
		{
			var items = await _db.GuestHouses
				.AsNoTracking()
				.OrderBy(h => h.Name)
				.Select(h => new GuestHouseDto
				{
					Id = h.Id,
					Name = h.Name,
					Address = h.Address,
					PhoneNumber = h.PhoneNumber,
					Description = h.Description,
					IsActive = h.IsActive,
					RoomCount = h.Rooms.Count,
					CreatedAt = h.CreatedAt,
					UpdatedAt = h.UpdatedAt
				})
				.ToListAsync();

			return Ok(items);
		}

		// GET /api/GuestHouseMaster/rooms
		[HttpGet("rooms")]
		public async Task<ActionResult<List<GuestHouseRoomDto>>> GetAllRooms()
		{
			var items = await _db.GuestHouseRooms
				.AsNoTracking()
				.Include(r => r.GuestHouse)
				.OrderBy(r => r.GuestHouse!.Name)
				.ThenBy(r => r.RoomNumber)
				.Select(r => new GuestHouseRoomDto
				{
					Id = r.Id,
					GuestHouseId = r.GuestHouseId,
					GuestHouseName = r.GuestHouse != null ? r.GuestHouse.Name : "",
					RoomType = r.RoomType,
					RoomNumber = r.RoomNumber,
					PricePerNight = r.PricePerNight,
					AvailableQuantity = r.AvailableQuantity,
					IsActive = r.IsActive,
					UpdatedAt = r.UpdatedAt
				})
				.ToListAsync();

			return Ok(items);
		}

		// =====================================================================
		// GUEST HOUSE CRUD (admin page form)
		// =====================================================================

		[HttpPost("houses")]
		public async Task<IActionResult> CreateHouse([FromBody] GuestHousePayload? payload)
		{
			if (payload is null || string.IsNullOrWhiteSpace(payload.Name))
				return BadRequest(new { Success = false, Message = "Guest House Name is required." });

			var name = payload.Name.Trim();
			if (await _db.GuestHouses.AnyAsync(h => h.Name.ToLower() == name.ToLower()))
				return Conflict(new { Success = false, Message = $"Guest House '{name}' already exists." });

			var now = DateTime.UtcNow;
			var house = new GuestHouse
			{
				Name = name,
				Address = NullIfEmpty(payload.Address),
				PhoneNumber = NullIfEmpty(payload.PhoneNumber),
				Description = NullIfEmpty(payload.Description),
				IsActive = payload.IsActive,
				CreatedBy = "current-user",
				CreatedAt = now,
				UpdatedBy = "current-user",
				UpdatedAt = now
			};
			_db.GuestHouses.Add(house);
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Guest House created successfully.", Id = house.Id });
		}

		[HttpPut("houses/{id:int}")]
		public async Task<IActionResult> UpdateHouse(int id, [FromBody] GuestHousePayload? payload)
		{
			if (payload is null || string.IsNullOrWhiteSpace(payload.Name))
				return BadRequest(new { Success = false, Message = "Guest House Name is required." });

			var house = await _db.GuestHouses.FindAsync(id);
			if (house == null)
				return NotFound(new { Success = false, Message = "Guest House not found." });

			var name = payload.Name.Trim();
			var dupe = await _db.GuestHouses
				.AnyAsync(h => h.Id != id && h.Name.ToLower() == name.ToLower());
			if (dupe)
				return Conflict(new { Success = false, Message = $"Guest House '{name}' already exists." });

			house.Name = name;
			house.Address = NullIfEmpty(payload.Address);
			house.PhoneNumber = NullIfEmpty(payload.PhoneNumber);
			house.Description = NullIfEmpty(payload.Description);
			house.IsActive = payload.IsActive;
			house.UpdatedBy = "current-user";
			house.UpdatedAt = DateTime.UtcNow;

			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Guest House updated successfully." });
		}

		[HttpDelete("houses/{id:int}")]
		public async Task<IActionResult> DeleteHouse(int id)
		{
			var house = await _db.GuestHouses.FindAsync(id);
			if (house == null)
				return NotFound(new { Success = false, Message = "Guest House not found." });

			if (await _db.GuestHouseRooms.AnyAsync(r => r.GuestHouseId == id))
				return Conflict(new { Success = false, Message = "Cannot delete this Guest House because it has rooms mapped to it. Delete or deactivate its rooms first." });

			_db.GuestHouses.Remove(house);
			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Guest House deleted successfully." });
		}

		[HttpPatch("houses/{id:int}/status")]
		public async Task<IActionResult> ToggleHouseStatus(int id, [FromQuery] bool isActive)
		{
			var house = await _db.GuestHouses.FindAsync(id);
			if (house == null)
				return NotFound(new { Success = false, Message = "Guest House not found." });

			house.IsActive = isActive;
			house.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Guest House status updated." });
		}

		// =====================================================================
		// ROOM CRUD (admin page form)
		// =====================================================================

		[HttpPost("rooms")]
		public async Task<IActionResult> CreateRoom([FromBody] GuestHouseRoomPayload? payload)
		{
			if (payload is null)
				return BadRequest(new { Success = false, Message = "Invalid request." });

			var validation = ValidateRoomPayload(payload);
			if (validation != null) return validation;

			if (!await _db.GuestHouses.AnyAsync(h => h.Id == payload.GuestHouseId))
				return BadRequest(new { Success = false, Message = "Selected Guest House does not exist." });

			var now = DateTime.UtcNow;
			var room = new GuestHouseRoom
			{
				GuestHouseId = payload.GuestHouseId,
				RoomType = payload.RoomType.Trim(),
				RoomNumber = NullIfEmpty(payload.RoomNumber),
				PricePerNight = payload.PricePerNight,
				AvailableQuantity = payload.AvailableQuantity < 1 ? 1 : payload.AvailableQuantity,
				IsActive = payload.IsActive,
				CreatedBy = "current-user",
				CreatedAt = now,
				UpdatedBy = "current-user",
				UpdatedAt = now
			};
			_db.GuestHouseRooms.Add(room);
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Room created successfully.", Id = room.Id });
		}

		[HttpPut("rooms/{id:int}")]
		public async Task<IActionResult> UpdateRoom(int id, [FromBody] GuestHouseRoomPayload? payload)
		{
			if (payload is null)
				return BadRequest(new { Success = false, Message = "Invalid request." });

			var validation = ValidateRoomPayload(payload);
			if (validation != null) return validation;

			var room = await _db.GuestHouseRooms.FindAsync(id);
			if (room == null)
				return NotFound(new { Success = false, Message = "Room not found." });

			if (!await _db.GuestHouses.AnyAsync(h => h.Id == payload.GuestHouseId))
				return BadRequest(new { Success = false, Message = "Selected Guest House does not exist." });

			room.GuestHouseId = payload.GuestHouseId;
			room.RoomType = payload.RoomType.Trim();
			room.RoomNumber = NullIfEmpty(payload.RoomNumber);
			room.PricePerNight = payload.PricePerNight;
			room.AvailableQuantity = payload.AvailableQuantity < 1 ? 1 : payload.AvailableQuantity;
			room.IsActive = payload.IsActive;
			room.UpdatedBy = "current-user";
			room.UpdatedAt = DateTime.UtcNow;

			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Room updated successfully." });
		}

		[HttpDelete("rooms/{id:int}")]
		public async Task<IActionResult> DeleteRoom(int id)
		{
			var room = await _db.GuestHouseRooms.FindAsync(id);
			if (room == null)
				return NotFound(new { Success = false, Message = "Room not found." });

			_db.GuestHouseRooms.Remove(room);
			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Room deleted successfully." });
		}

		[HttpPatch("rooms/{id:int}/status")]
		public async Task<IActionResult> ToggleRoomStatus(int id, [FromQuery] bool isActive)
		{
			var room = await _db.GuestHouseRooms.FindAsync(id);
			if (room == null)
				return NotFound(new { Success = false, Message = "Room not found." });

			room.IsActive = isActive;
			room.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Room status updated." });
		}

		// =====================================================================
		// REPOSITORY HELPERS (upsert logic)
		// =====================================================================

		private async Task<(int Inserted, int Updated)> BulkUpsertHousesAsync(
			List<IXLRow> rows,
			Dictionary<string, int> headerMap,
			Action<string, string> AddGrouped,
			DateTime now)
		{
			var houses = await _db.GuestHouses
				.AsNoTracking()
				.ToListAsync();
			var existingHouses = houses.ToDictionary(h => h.Name.Trim().ToUpperInvariant(), h => h);

			var batchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var insertedCount = 0;
			var updatedCount = 0;

			foreach (var row in rows)
			{
				try
				{
					var name = GetCellString(row, headerMap, "name");
					if (string.IsNullOrWhiteSpace(name))
					{
						AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: Name is required");
						continue;
					}

					var key = name.Trim().ToUpperInvariant();
					if (!batchKeys.Add(key))
					{
						AddGrouped("Duplicated in this file", $"'{name}' (Row {row.RowNumber()})");
						continue;
					}

					if (existingHouses.TryGetValue(key, out var existing))
					{
						existing.Address = NullIfEmpty(GetCellString(row, headerMap, "address"));
						existing.PhoneNumber = NullIfEmpty(GetCellString(row, headerMap, "phonenumber"));
						existing.Description = NullIfEmpty(GetCellString(row, headerMap, "description"));
						existing.UpdatedBy = "bulk-upload";
						existing.UpdatedAt = now;
						_db.GuestHouses.Update(existing);
						updatedCount++;
					}
					else
					{
						_db.GuestHouses.Add(new GuestHouse
						{
							Name = name.Trim(),
							Address = NullIfEmpty(GetCellString(row, headerMap, "address")),
							PhoneNumber = NullIfEmpty(GetCellString(row, headerMap, "phonenumber")),
							Description = NullIfEmpty(GetCellString(row, headerMap, "description")),
							IsActive = true,
							CreatedBy = "bulk-upload",
							CreatedAt = now,
							UpdatedBy = "bulk-upload",
							UpdatedAt = now
						});
						insertedCount++;
					}
				}
				catch (Exception exRow)
				{
					_logger.LogWarning(exRow, "Guest House bulk upload row parse error");
					AddGrouped("Parse errors", $"Row {row.RowNumber()}: {exRow.Message}");
				}
			}

			return (insertedCount, updatedCount);
		}

		private async Task<(int Inserted, int Updated)> BulkUpsertRoomsAsync(
			List<IXLRow> rows,
			Dictionary<string, int> headerMap,
			Action<string, string> AddGrouped,
			DateTime now)
		{
			// Guest houses must exist first (created on the Guest House tab).
			var houses = await _db.GuestHouses
				.AsNoTracking()
				.ToListAsync();
			var houseByName = houses.ToDictionary(h => h.Name.Trim().ToUpperInvariant(), h => h);

			var rooms = await _db.GuestHouseRooms
				.AsNoTracking()
				.Include(r => r.GuestHouse)
				.Where(r => r.GuestHouse != null)
				.ToListAsync();
			var existingRoomKeys = rooms
				.Select(r => $"{r.GuestHouse!.Name.Trim().ToUpperInvariant()}|{Normalize(RoomKeyPart(r.RoomType))}|{Normalize(RoomKeyPart(r.RoomNumber))}")
				.ToHashSet(StringComparer.Ordinal);

			var batchKeys = new HashSet<string>(StringComparer.Ordinal);
			var insertedCount = 0;
			var updatedCount = 0;

			foreach (var row in rows)
			{
				try
				{
					var houseName = GetCellString(row, headerMap, "guesthousename");
					var roomType = GetCellString(row, headerMap, "roomtype");
					var roomNumber = GetCellString(row, headerMap, "roomnumber");
					var noOfRoomsRaw = GetCellString(row, headerMap, "noofrooms");
					var rateRaw = GetCellString(row, headerMap, "rate");

					if (string.IsNullOrWhiteSpace(houseName))
					{
						AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: GuestHouseName is required");
						continue;
					}
					if (string.IsNullOrWhiteSpace(roomType))
					{
						AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: RoomType is required");
						continue;
					}
					if (string.IsNullOrWhiteSpace(noOfRoomsRaw))
					{
						AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: NoOfRooms is required");
						continue;
					}
					if (string.IsNullOrWhiteSpace(rateRaw))
					{
						AddGrouped("Empty mandatory field", $"Row {row.RowNumber()}: Rate is required");
						continue;
					}

					if (!houseByName.TryGetValue(houseName.Trim().ToUpperInvariant(), out var house))
					{
						AddGrouped("GuestHouseName not found in database",
							$"Row {row.RowNumber()}: '{houseName}' has not been created yet. Add this Guest House first, then retry.");
						continue;
					}

					if (!int.TryParse(noOfRoomsRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var noOfRooms) || noOfRooms < 1)
					{
						AddGrouped("Invalid NoOfRooms", $"Row {row.RowNumber()}: '{noOfRoomsRaw}' is not a valid positive whole number");
						continue;
					}

					if (!decimal.TryParse(rateRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate < 0)
					{
						AddGrouped("Invalid Rate", $"Row {row.RowNumber()}: '{rateRaw}' is not a valid rate");
						continue;
					}

					var key = $"{houseName.Trim().ToUpperInvariant()}|{Normalize(RoomKeyPart(roomType))}|{Normalize(RoomKeyPart(roomNumber))}";
					if (!batchKeys.Add(key))
					{
						AddGrouped("Duplicated in this file",
							$"'{roomType}' room '{(string.IsNullOrWhiteSpace(roomNumber) ? "(all)" : roomNumber)}' of '{houseName}' (Row {row.RowNumber()})");
						continue;
					}

					if (existingRoomKeys.Contains(key))
					{
						// AsNoTracking pre-load, so re-attach to persist the update.
						var existing = rooms.First(r =>
							string.Equals(r.GuestHouse!.Name.Trim(), houseName.Trim(), StringComparison.OrdinalIgnoreCase) &&
							string.Equals(RoomKeyPart(r.RoomType), RoomKeyPart(roomType), StringComparison.OrdinalIgnoreCase) &&
							string.Equals(RoomKeyPart(r.RoomNumber), RoomKeyPart(roomNumber), StringComparison.OrdinalIgnoreCase));

						existing.RoomType = roomType;
						existing.RoomNumber = string.IsNullOrWhiteSpace(roomNumber) ? null : roomNumber;
						existing.PricePerNight = rate;
						existing.AvailableQuantity = noOfRooms;
						existing.IsActive = true;
						existing.UpdatedBy = "bulk-upload";
						existing.UpdatedAt = now;
						_db.GuestHouseRooms.Update(existing);
						updatedCount++;
					}
					else
					{
						_db.GuestHouseRooms.Add(new GuestHouseRoom
						{
							GuestHouseId = house.Id,
							RoomType = roomType,
							RoomNumber = string.IsNullOrWhiteSpace(roomNumber) ? null : roomNumber,
							PricePerNight = rate,
							AvailableQuantity = noOfRooms,
							IsActive = true,
							CreatedBy = "bulk-upload",
							CreatedAt = now,
							UpdatedBy = "bulk-upload",
							UpdatedAt = now
						});
						insertedCount++;
					}
				}
				catch (Exception exRow)
				{
					_logger.LogWarning(exRow, "Guest House room bulk upload row parse error");
					AddGrouped("Parse errors", $"Row {row.RowNumber()}: {exRow.Message}");
				}
			}

			return (insertedCount, updatedCount);
		}

		private IActionResult? ValidateRoomPayload(GuestHouseRoomPayload payload)
		{
			if (string.IsNullOrWhiteSpace(payload.RoomType))
				return BadRequest(new { Success = false, Message = "RoomType is required." });
			if (payload.PricePerNight < 0)
				return BadRequest(new { Success = false, Message = "Rate must be zero or greater." });
			return null;
		}

		// =====================================================================
		// HELPERS (SubDealerEmployeeController pattern)
		// =====================================================================

		private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

		private static string RoomKeyPart(string? s) => string.IsNullOrWhiteSpace(s) ? "(none)" : s.Trim();

		private static string Normalize(string s) =>
			(s ?? string.Empty).Trim().Replace(" ", "").Replace("-", "").Replace(".", "").Replace("/", "").ToLowerInvariant();

		private static string GetCellString(IXLRow row, Dictionary<string, int> headerMap, string key)
		{
			if (!headerMap.TryGetValue(key, out var col)) return string.Empty;
			var cell = row.Cell(col);
			if (cell.DataType == XLDataType.Number && cell.TryGetValue(out double numeric))
				return numeric.ToString("0.####", CultureInfo.InvariantCulture);
			return cell.GetString().Trim();
		}

		private static string NormalizeHeader(string h) =>
			(h ?? string.Empty).Trim().Replace(" ", "").Replace("_", "").Replace("&", "").ToLowerInvariant();

		private static string PrettyHeader(string h) => h switch
		{
			"name" => "Name",
			"address" => "Address",
			"phonenumber" => "PhoneNumber",
			"description" => "Description",
			"guesthousename" => "GuestHouseName",
			"roomtype" => "RoomType",
			"roomnumber" => "RoomNumber",
			"noofrooms" => "NoOfRooms",
			"rate" => "Rate",
			_ => h
		};
	}

	public class GuestHouseDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public string? Address { get; set; }
		public string? PhoneNumber { get; set; }
		public string? Description { get; set; }
		public bool IsActive { get; set; }
		public int RoomCount { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class GuestHouseRoomDto
	{
		public int Id { get; set; }
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string? RoomType { get; set; }
		public string? RoomNumber { get; set; }
		public decimal PricePerNight { get; set; }
		public int AvailableQuantity { get; set; }
		public bool IsActive { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class GuestHousePayload
	{
		public string Name { get; set; } = "";
		public string? Address { get; set; }
		public string? PhoneNumber { get; set; }
		public string? Description { get; set; }
		public bool IsActive { get; set; } = true;
	}

	public class GuestHouseRoomPayload
	{
		public int GuestHouseId { get; set; }
		public string RoomType { get; set; } = "";
		public string? RoomNumber { get; set; }
		public decimal PricePerNight { get; set; }
		public int AvailableQuantity { get; set; } = 1;
		public bool IsActive { get; set; } = true;
	}
}