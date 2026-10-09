using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.MasterData;
using SPIC.Core.Entities;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Guest House + Room master data management (Settings > Guest House).
	///
	/// Access is decided by <see cref="GuestHouseMasterAccessAttribute"/> below (the old class-level
	/// [Authorize(Roles = "Admin,CorporateAdmin")] could only read the JWT role claim and therefore
	/// never saw a Designation).
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
	[Authorize]
	[GuestHouseMasterAccess]
	[ApiController]
	[Route("api/[controller]")]
	public class GuestHouseMasterController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly ILogger<GuestHouseMasterController> _logger;
		private readonly IWebHostEnvironment _env;

		public GuestHouseMasterController(AppDbContext db, ILogger<GuestHouseMasterController> logger, IWebHostEnvironment env)
		{
			_db = db;
			_logger = logger;
			_env = env;
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
					StateId = h.StateId,
					StateName = _db.States.Where(s => s.Id == h.StateId).Select(s => s.StateName).FirstOrDefault(),
					ImagePath = h.Images
						.Where(i => i.IsActive)
						.OrderBy(i => i.IsPrimary ? 0 : 1)
						.ThenBy(i => i.DisplayOrder)
						.Select(i => i.FilePath)
						.FirstOrDefault(),
					IsActive = h.IsActive,
					RoomCount = h.Rooms.Count,
					CreatedAt = h.CreatedAt,
					UpdatedAt = h.UpdatedAt
				})
				.ToListAsync();

			return Ok(items);
		}

		// GET /api/GuestHouseMaster/room-types
		[HttpGet("room-types")]
		public async Task<ActionResult<List<GuestHouseRoomTypeDto>>> GetAllRoomTypes()
		{
			var items = await _db.GuestHouseRoomTypes
				.AsNoTracking()
				.OrderBy(rt => rt.Name)
				.Select(rt => new GuestHouseRoomTypeDto
				{
					Id = rt.Id,
					Name = rt.Name,
					IsActive = rt.IsActive,
					RoomCount = rt.Rooms.Count,
					CreatedAt = rt.CreatedAt,
					UpdatedAt = rt.UpdatedAt
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
				.Include(r => r.RoomTypeMaster)
				.OrderBy(r => r.GuestHouse!.Name)
				.ThenBy(r => r.RoomNumber)
				.Select(r => new GuestHouseRoomDto
				{
					Id = r.Id,
					GuestHouseId = r.GuestHouseId,
					GuestHouseName = r.GuestHouse != null ? r.GuestHouse.Name : "",
					RoomTypeId = r.RoomTypeId,
					RoomTypeName = r.RoomTypeMaster != null ? r.RoomTypeMaster.Name : null,
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

			if (payload.StateId > 0 && !await _db.States.AnyAsync(s => s.Id == payload.StateId))
				return BadRequest(new { Success = false, Message = "Selected State was not found." });

			var now = DateTime.UtcNow;
			var house = new GuestHouse
			{
				Name = name,
				Address = NullIfEmpty(payload.Address),
				PhoneNumber = NullIfEmpty(payload.PhoneNumber),
				Description = NullIfEmpty(payload.Description),
				StateId = payload.StateId > 0 ? payload.StateId : null,
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

			if (payload.StateId > 0 && !await _db.States.AnyAsync(s => s.Id == payload.StateId))
				return BadRequest(new { Success = false, Message = "Selected State was not found." });

			house.Name = name;
			house.Address = NullIfEmpty(payload.Address);
			house.PhoneNumber = NullIfEmpty(payload.PhoneNumber);
			house.Description = NullIfEmpty(payload.Description);
			house.StateId = payload.StateId > 0 ? payload.StateId : null;
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

		// POST /api/GuestHouseMaster/houses/{id}/image
		// Uploads/replaces the cover image of a guest house. Stored under Uploads/GuestHouse/{id}
		// and linked through the existing GuestHouseImage table (no schema change needed).
		[HttpPost("houses/{id:int}/image")]
		[RequestSizeLimit(6 * 1024 * 1024)]
		public async Task<IActionResult> UploadHouseImage(int id, IFormFile? file)
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { Success = false, Message = "No file uploaded." });

			var house = await _db.GuestHouses.FindAsync(id);
			if (house == null)
				return NotFound(new { Success = false, Message = "Guest House not found." });

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
			if (!allowedExts.Contains(ext))
				return BadRequest(new { Success = false, Message = "Only JPG, PNG, or WEBP images are allowed." });

			if (file.Length > 5 * 1024 * 1024)
				return BadRequest(new { Success = false, Message = "Image must be 5 MB or less." });

			var uploadsRoot = Path.Combine(_env.ContentRootPath, "Uploads");

			var folder = Path.Combine(uploadsRoot, "GuestHouse", id.ToString());
			Directory.CreateDirectory(folder);

			var storedName = $"cover_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
			var physicalPath = Path.Combine(folder, storedName);

			await using (var stream = new FileStream(physicalPath, FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			var relativePath = $"GuestHouse/{id}/{storedName}";

			// Demote any existing primary image, then store the new one as the active primary.
			var existing = await _db.GuestHouseImages
				.Where(i => i.GuestHouseId == id)
				.ToListAsync();
			foreach (var img in existing)
			{
				img.IsPrimary = false;
				img.IsActive = false;
			}

			_db.GuestHouseImages.Add(new GuestHouseImage
			{
				GuestHouseId = id,
				FileName = Path.GetFileName(file.FileName),
				FilePath = relativePath,
				IsPrimary = true,
				DisplayOrder = 0,
				IsActive = true,
				CreatedBy = "current-user",
				CreatedAt = DateTime.UtcNow
			});
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Image uploaded successfully.", FilePath = relativePath });
		}

		// =====================================================================
		// ROOM TYPE CRUD (admin page form)
		// =====================================================================

		[HttpPost("room-types")]
		public async Task<IActionResult> CreateRoomType([FromBody] GuestHouseRoomTypePayload? payload)
		{
			if (payload is null || string.IsNullOrWhiteSpace(payload.Name))
				return BadRequest(new { Success = false, Message = "Room Type Name is required." });

			var name = payload.Name.Trim();
			if (await _db.GuestHouseRoomTypes.AnyAsync(rt => rt.Name.ToLower() == name.ToLower()))
				return Conflict(new { Success = false, Message = $"Room Type '{name}' already exists." });

			var now = DateTime.UtcNow;
			var roomType = new GuestHouseRoomType
			{
				Name = name,
				IsActive = payload.IsActive,
				CreatedBy = "current-user",
				CreatedAt = now,
				UpdatedBy = "current-user",
				UpdatedAt = now
			};
			_db.GuestHouseRoomTypes.Add(roomType);
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Room Type created successfully.", Id = roomType.Id });
		}

		[HttpPut("room-types/{id:int}")]
		public async Task<IActionResult> UpdateRoomType(int id, [FromBody] GuestHouseRoomTypePayload? payload)
		{
			if (payload is null || string.IsNullOrWhiteSpace(payload.Name))
				return BadRequest(new { Success = false, Message = "Room Type Name is required." });

			var roomType = await _db.GuestHouseRoomTypes.FindAsync(id);
			if (roomType == null)
				return NotFound(new { Success = false, Message = "Room Type not found." });

			var name = payload.Name.Trim();
			var dupe = await _db.GuestHouseRoomTypes
				.AnyAsync(rt => rt.Id != id && rt.Name.ToLower() == name.ToLower());
			if (dupe)
				return Conflict(new { Success = false, Message = $"Room Type '{name}' already exists." });

			roomType.Name = name;
			roomType.IsActive = payload.IsActive;
			roomType.UpdatedBy = "current-user";
			roomType.UpdatedAt = DateTime.UtcNow;

			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Room Type updated successfully." });
		}

		[HttpDelete("room-types/{id:int}")]
		public async Task<IActionResult> DeleteRoomType(int id)
		{
			var roomType = await _db.GuestHouseRoomTypes.FindAsync(id);
			if (roomType == null)
				return NotFound(new { Success = false, Message = "Room Type not found." });

			if (await _db.GuestHouseRooms.AnyAsync(r => r.RoomTypeId == id))
				return Conflict(new { Success = false, Message = "Cannot delete this Room Type because it has rooms mapped to it. Unlink or delete those rooms first." });

			_db.GuestHouseRoomTypes.Remove(roomType);
			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Room Type deleted successfully." });
		}

		[HttpPatch("room-types/{id:int}/status")]
		public async Task<IActionResult> ToggleRoomTypeStatus(int id, [FromQuery] bool isActive)
		{
			var roomType = await _db.GuestHouseRoomTypes.FindAsync(id);
			if (roomType == null)
				return NotFound(new { Success = false, Message = "Room Type not found." });

			roomType.IsActive = isActive;
			roomType.UpdatedAt = DateTime.UtcNow;
			await _db.SaveChangesAsync();
			return Ok(new { Success = true, Message = "Room Type status updated." });
		}

		// =====================================================================
		// ROOM CRUD (admin page form)
		// =====================================================================

		[HttpPost("rooms")]
		public async Task<IActionResult> CreateRoom([FromBody] GuestHouseRoomPayload? payload)
		{
			if (payload is null)
				return BadRequest(new { Success = false, Message = "Invalid request." });

			if (payload.RoomTypeId.HasValue && payload.RoomTypeId.Value > 0)
			{
				var masterType = await _db.GuestHouseRoomTypes.FindAsync(payload.RoomTypeId.Value);
				if (masterType == null)
					return BadRequest(new { Success = false, Message = "Selected Room Type Master does not exist." });

				if (string.IsNullOrWhiteSpace(payload.RoomType))
					payload.RoomType = masterType.Name;
			}

			var validation = ValidateRoomPayload(payload);
			if (validation != null) return validation;

			if (!await _db.GuestHouses.AnyAsync(h => h.Id == payload.GuestHouseId))
				return BadRequest(new { Success = false, Message = "Selected Guest House does not exist." });

			var now = DateTime.UtcNow;
			var room = new GuestHouseRoom
			{
				GuestHouseId = payload.GuestHouseId,
				RoomTypeId = payload.RoomTypeId > 0 ? payload.RoomTypeId : null,
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

			if (payload.RoomTypeId.HasValue && payload.RoomTypeId.Value > 0)
			{
				var masterType = await _db.GuestHouseRoomTypes.FindAsync(payload.RoomTypeId.Value);
				if (masterType == null)
					return BadRequest(new { Success = false, Message = "Selected Room Type Master does not exist." });

				if (string.IsNullOrWhiteSpace(payload.RoomType))
					payload.RoomType = masterType.Name;
			}

			var validation = ValidateRoomPayload(payload);
			if (validation != null) return validation;

			var room = await _db.GuestHouseRooms.FindAsync(id);
			if (room == null)
				return NotFound(new { Success = false, Message = "Room not found." });

			if (!await _db.GuestHouses.AnyAsync(h => h.Id == payload.GuestHouseId))
				return BadRequest(new { Success = false, Message = "Selected Guest House does not exist." });

			room.GuestHouseId = payload.GuestHouseId;
			room.RoomTypeId = payload.RoomTypeId > 0 ? payload.RoomTypeId : null;
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
			// GroupBy(...).First() rather than ToDictionary: two guest houses whose names
			// normalize to the same key used to throw "An item with the same key has already
			// been added" and fail the whole upload. Canonical normalization also makes
			// "  north  lodge " and "North Lodge" the same guest house.
			var existingHouses = houses
				.GroupBy(h => MasterNormalizer.Normalize(h.Name))
				.Where(g => g.Key.Length > 0)
				.ToDictionary(g => g.Key, g => g.First());

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

					var key = MasterNormalizer.Normalize(name);
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
			// GroupBy(...).First() rather than ToDictionary: duplicate guest house names in the
			// master table used to throw and fail the room upload with a 500.
			var houseByName = houses
				.GroupBy(h => MasterNormalizer.Normalize(h.Name))
				.Where(g => g.Key.Length > 0)
				.ToDictionary(g => g.Key, g => g.First());

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

					if (!houseByName.TryGetValue(MasterNormalizer.Normalize(houseName), out var house))
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

					var key = $"{MasterNormalizer.Normalize(houseName)}|{Normalize(RoomKeyPart(roomType))}|{Normalize(RoomKeyPart(roomNumber))}";
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
		public int? StateId { get; set; }
		public string? StateName { get; set; }
		public string? ImagePath { get; set; }
		public bool IsActive { get; set; }
		public int RoomCount { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class GuestHouseRoomTypeDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public bool IsActive { get; set; }
		public int RoomCount { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class GuestHouseRoomTypePayload
	{
		public string Name { get; set; } = "";
		public bool IsActive { get; set; } = true;
	}

	public class GuestHouseRoomDto
	{
		public int Id { get; set; }
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public int? RoomTypeId { get; set; }
		public string? RoomTypeName { get; set; }
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
		public int? StateId { get; set; }
		public bool IsActive { get; set; } = true;
	}

	public class GuestHouseRoomPayload
	{
		public int GuestHouseId { get; set; }
		public int? RoomTypeId { get; set; }
		public string RoomType { get; set; } = "";
		public string? RoomNumber { get; set; }
		public decimal PricePerNight { get; set; }
		public int AvailableQuantity { get; set; } = 1;
		public bool IsActive { get; set; } = true;
	}

	/// <summary>
	/// Route guard for the Guest House &amp; Room master page (every <c>/api/GuestHouseMaster/*</c> action).
	///
	/// WHY THIS EXISTS: the controller used to carry <c>[Authorize(Roles = "Admin,CorporateAdmin")]</c>,
	/// which only reads the JWT role claim and can therefore never honour a Designation. The SDWA
	/// sidebar entry, PageGuard and GuestHouseMaster.razor all decide with
	/// <c>LoginState.CanAccess(PagePermission.GuestHouse)</c>, so an employee whose Designation grants
	/// GuestHouse could open the page and then got 403 from here ("You do not have permission to view
	/// this data.").
	///
	/// IT IS NOT A SECOND PERMISSION SYSTEM: the Designation check below is the SAME primitive the
	/// rest of the API already uses for this exact purpose (LibraryController.CanManageAsync,
	/// CommunityController.CanManageLibraryAsync, LabReportsController.ResolveAccessAsync,
	/// SasPaymentsController.ResolveAccessAsync, WelfareSchemeApprovalController.
	/// UserHasDesignationPermissionAsync) -
	/// <c>UserInfo.DesignationId -&gt; Designation.RoleAccess -&gt; RoleAccessPermissions.HasPage</c>,
	/// resolved exactly the way AuthenticationController.Login resolves RoleAccess client-side.
	///
	/// RULES:
	/// <list type="bullet">
	/// <item>Admin / CorporateAdmin - unchanged: they keep every action, exactly as the previous
	/// <c>[Authorize(Roles = ...)]</c> granted them.</item>
	/// <item>Any other signed-in user - READS only, and only when their Designation's RoleAccess
	/// grants <c>PagePermission.GuestHouse</c>. No SDWA / FrontOffice / GenerateBill token, and no
	/// AppRole, substitutes for it.</item>
	/// <item>Everything else - 403, which GuestHouseMaster.razor already renders as
	/// "You do not have permission to view this data." (the previous outcome for these callers too,
	/// so no caller gets a new answer).</item>
	/// </list>
	/// The WRITE actions (POST / PUT / DELETE / PATCH, incl. bulk-upload and image upload) keep their
	/// original Admin/CorporateAdmin-only restriction - they are the CRUD rules of this master page.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	internal sealed class GuestHouseMasterAccessAttribute : Attribute, IAsyncAuthorizationFilter
	{
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

			// Writes keep the original Admin/CorporateAdmin-only restriction.
			if (!string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
			{
				context.Result = Forbidden();
				return;
			}

			// Designation rule: does THIS user's Designation.RoleAccess grant one of the
			// PagePermissions this route is allowed to read?
			var allowedPermissions = ReadPermissionsFor(context.HttpContext.Request);
			var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
			if (allowedPermissions.Length > 0 && !string.IsNullOrWhiteSpace(userId))
			{
				var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
				var designationId = await db.Users
					.AsNoTracking()
					.Where(u => u.Id == userId)
					.Select(u => u.DesignationId)
					.FirstOrDefaultAsync();

				if (designationId is > 0)
				{
					var roleAccess = await db.Designations
						.AsNoTracking()
						.Where(d => d.Id == designationId && d.IsActive)
						.Select(d => d.RoleAccess)
						.FirstOrDefaultAsync();

					foreach (var allowed in allowedPermissions)
					{
						if (RoleAccessPermissions.HasPage(roleAccess, allowed))
							return;
					}
				}
			}

			context.Result = Forbidden();
		}

		/// <summary>
		/// PagePermissions allowed to READ this route.
		///
		/// The house list is the Guest House grid's own data (PagePermission.GuestHouse) and it is
		/// ALSO the "Guest House" picker of the SDWA Company Details page, which is guarded by its
		/// own PagePermission.SdwaCompanyMaster - so that one endpoint additionally accepts the
		/// SdwaCompanyMaster Designation grant. Every other endpoint keeps PagePermission.GuestHouse
		/// only; no endpoint is opened by being logged in.
		/// </summary>
		private static PagePermission[] ReadPermissionsFor(HttpRequest request)
		{
			if (request.Path.StartsWithSegments("/api/GuestHouseMaster/houses", StringComparison.OrdinalIgnoreCase))
				return new[] { PagePermission.GuestHouse, PagePermission.SdwaCompanyMaster };

			return new[] { PagePermission.GuestHouse };
		}

		private static ObjectResult Forbidden() =>
			new(new { Success = false, Message = "You do not have permission to view this data." })
			{
				StatusCode = StatusCodes.Status403Forbidden
			};
	}
}