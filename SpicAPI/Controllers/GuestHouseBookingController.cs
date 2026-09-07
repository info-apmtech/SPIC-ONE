using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.IO;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Customer-facing guest house booking flow (GuestHouse -> GuestHouseBooking -> Rooms).
	///
	/// Unlike GuestHouseMasterController (admin-only Settings management), this controller is
	/// intentionally not restricted to admin roles so any logged-in dealer/employee can browse
	/// guest houses and check room availability.
	///
	/// Availability is computed from the existing GuestHouseRoomAvailability records for the
	/// selected stay period; per-date availability overrides the master inventory when present.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	public class GuestHouseBookingController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly IWebHostEnvironment _env;

		public GuestHouseBookingController(AppDbContext db, IWebHostEnvironment env)
		{
			_db = db;
			_env = env;
		}

		// GET /api/GuestHouseBooking/houses
		// Active guest houses shown on the GuestHouse selection page.
		[HttpGet("houses")]
		public async Task<ActionResult<List<GuestHouseCardDto>>> GetHouses()
		{
			var items = await _db.GuestHouses
				.AsNoTracking()
				.Where(h => h.IsActive)
				.OrderBy(h => h.Name)
				.Select(h => new GuestHouseCardDto
				{
					Id = h.Id,
					Name = h.Name,
					Address = h.Address,
					PhoneNumber = h.PhoneNumber,
					ImagePath = h.Images
						.Where(i => i.IsActive)
						.OrderBy(i => i.IsPrimary ? 0 : 1)
						.ThenBy(i => i.DisplayOrder)
						.Select(i => i.FilePath)
						.FirstOrDefault()
				})
				.ToListAsync();

			return Ok(items);
		}

		// GET /api/GuestHouseBooking/image/{*filePath}
		// Serves an uploaded guest house image with path-traversal protection.
		//
		// The stored FilePath has historically been an uploads-root relative path
		// (e.g. "/uploads/guesthouse/1/cover_....jpg") whose file physically lives
		// under the web root (wwwroot/uploads/...), while newer uploads are stored
		// as a ContentRoot-relative path (e.g. "GuestHouse/1/cover_....jpg") under
		// the ContentRoot Uploads folder. Resolve against BOTH real storage roots so
		// every historically stored value and every new upload is served correctly.
		[Authorize]
		[HttpGet("image/{*filePath}")]
		public IActionResult ViewImage(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath) ||
				filePath.Contains("..", StringComparison.Ordinal) ||
				filePath.IndexOf(':') >= 0)
			{
				return NotFound("Image not found.");
			}

			var normalized = filePath.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);

			var roots = new[] { GetUploadsRoot(), GetWebRoot() };

			foreach (var root in roots)
			{
				var fullPath = Path.GetFullPath(Path.Combine(root, normalized));

				var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					+ Path.DirectorySeparatorChar;
				if (!fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
					continue;

				if (!System.IO.File.Exists(fullPath))
					continue;

				var ext = Path.GetExtension(fullPath).ToLowerInvariant();
				var contentType = ext switch
				{
					".jpg" or ".jpeg" => "image/jpeg",
					".png" => "image/png",
					".webp" => "image/webp",
					_ => "application/octet-stream"
				};

				return PhysicalFile(fullPath, contentType);
			}

			return NotFound("Image not found.");
		}

		// GET /api/GuestHouseBooking/availability?guestHouseId=1&checkIn=2026-09-10T14:00&checkOut=2026-09-12T12:00
		// Returns the rooms of the selected guest house that are available for the whole stay.
		[HttpGet("availability")]
		public async Task<IActionResult> GetAvailability([FromQuery] int guestHouseId, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
		{
			if (checkOut.Date <= checkIn.Date)
				return BadRequest(new { Success = false, Message = "Check-out must be after check-in." });

			var house = await _db.GuestHouses
				.AsNoTracking()
				.FirstOrDefaultAsync(h => h.Id == guestHouseId && h.IsActive);
			if (house == null)
				return NotFound(new { Success = false, Message = "Guest House not found." });

			// Nights of the stay: every calendar date from check-in until (but not including) check-out.
			var nights = Enumerable
				.Range(0, (checkOut.Date - checkIn.Date).Days)
				.Select(i => checkIn.Date.AddDays(i))
				.ToList();

			var rooms = await _db.GuestHouseRooms
				.AsNoTracking()
				.Where(r => r.GuestHouseId == guestHouseId && r.IsActive)
				.OrderBy(r => r.RoomType)
				.ToListAsync();

			var roomIds = rooms.Select(r => r.Id).ToList();

			// Per-date availability rows covering the requested stay.
			var availabilityRows = await _db.GuestHouseRoomAvailabilities
				.AsNoTracking()
				.Where(a => roomIds.Contains(a.GuestHouseRoomId) && a.Date >= checkIn.Date && a.Date < checkOut.Date)
				.ToListAsync();

			var availableRooms = new List<AvailableRoomDto>();

			foreach (var room in rooms)
			{
				// Default to the master inventory for nights without availability records.
				var periodAvailability = room.AvailableQuantity;
				bool hasAnyRows = false;

				foreach (var night in nights)
				{
					var rowsForNight = availabilityRows
						.Where(a => a.GuestHouseRoomId == room.Id && a.Date.Date == night)
						.ToList();
					if (rowsForNight.Count == 0)
						continue;

					hasAnyRows = true;
					if (rowsForNight.Any(a => a.IsBlocked))
					{
						periodAvailability = 0;
						break;
					}

					var nightAvailable = Math.Max(0, rowsForNight.Min(a => a.AvailableRooms));
					periodAvailability = Math.Min(periodAvailability, nightAvailable);
					if (periodAvailability <= 0)
						break;
				}

				// Only include the room when it is available on every night of the stay.
				if (hasAnyRows && periodAvailability <= 0)
					continue;

				availableRooms.Add(new AvailableRoomDto
				{
					RoomId = room.Id,
					RoomTypeId = room.Id,
					RoomType = room.RoomType ?? "Room",
					RoomNumber = room.RoomNumber,
					Description = room.Description,
					PricePerNight = room.PricePerNight,
					ExtraCotPrice = room.ExtraCotPrice,
					Capacity = room.Capacity,
					NumberOfAdults = room.NumberOfAdults,
					AvailableQuantity = periodAvailability > 0 ? periodAvailability : 0
				});
			}

			return Ok(new AvailabilityResultDto
			{
				GuestHouseId = house.Id,
				GuestHouseName = house.Name,
				CheckIn = checkIn,
				CheckOut = checkOut,
				Rooms = availableRooms
			});
		}

		// GET /api/GuestHouseBooking/rooms/{roomId}
		// Canonical room + guest house details used by the GuestDetails booking summary.
		[HttpGet("rooms/{roomId:int}")]
		public async Task<IActionResult> GetRoom(int roomId)
		{
			var room = await _db.GuestHouseRooms
				.AsNoTracking()
				.Include(r => r.GuestHouse)
				.FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);
			if (room == null)
				return NotFound(new { Success = false, Message = "Room not found." });

			return Ok(new RoomDetailDto
			{
				RoomId = room.Id,
				RoomTypeId = room.Id,
				GuestHouseId = room.GuestHouseId,
				GuestHouseName = room.GuestHouse?.Name ?? "",
				RoomType = room.RoomType ?? "Room",
				RoomNumber = room.RoomNumber,
				Description = room.Description,
				PricePerNight = room.PricePerNight,
				ExtraCotPrice = room.ExtraCotPrice,
				Capacity = room.Capacity,
				NumberOfAdults = room.NumberOfAdults
			});
		}

		private string GetUploadsRoot()
		{
			return Path.Combine(_env.ContentRootPath, "Uploads");
		}

		private string GetWebRoot()
		{
			var webRoot = _env.WebRootPath;
			if (string.IsNullOrWhiteSpace(webRoot))
				webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
			return webRoot;
		}
	}

	public class GuestHouseCardDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public string? Address { get; set; }
		public string? PhoneNumber { get; set; }
		public string? ImagePath { get; set; }
	}

	public class AvailableRoomDto
	{
		public int RoomId { get; set; }
		public int RoomTypeId { get; set; }
		public string RoomType { get; set; } = "";
		public string? RoomNumber { get; set; }
		public string? Description { get; set; }
		public decimal PricePerNight { get; set; }
		public decimal? ExtraCotPrice { get; set; }
		public int? Capacity { get; set; }
		public int? NumberOfAdults { get; set; }
		public int AvailableQuantity { get; set; }
	}

	public class AvailabilityResultDto
	{
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public DateTime CheckIn { get; set; }
		public DateTime CheckOut { get; set; }
		public List<AvailableRoomDto> Rooms { get; set; } = new List<AvailableRoomDto>();
	}

	public class RoomDetailDto
	{
		public int RoomId { get; set; }
		public int RoomTypeId { get; set; }
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string RoomType { get; set; } = "";
		public string? RoomNumber { get; set; }
		public string? Description { get; set; }
		public decimal PricePerNight { get; set; }
		public decimal? ExtraCotPrice { get; set; }
		public int? Capacity { get; set; }
		public int? NumberOfAdults { get; set; }
	}
}