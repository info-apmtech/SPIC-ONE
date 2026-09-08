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

        // POST /api/GuestHouseBooking/book
        // Creates a guest house booking (save to DB). Re-checks room availability for the
        // selected period before saving so a room that became unavailable cannot be booked.
        [HttpPost("book")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            if (request == null)
                return BadRequest(new { Success = false, Message = "Invalid request." });

            var userId = User.Identity?.Name ?? request.EmployeeOrDealerCode;

            // Validate guest house and room.
            if (request.GuestHouseId <= 0 || request.RoomTypeId <= 0)
                return BadRequest(new { Success = false, Message = "Guest House and Room are required." });

            var house = await _db.GuestHouses.AsNoTracking().FirstOrDefaultAsync(h => h.Id == request.GuestHouseId && h.IsActive);
            if (house == null)
                return NotFound(new { Success = false, Message = "Guest House not found." });

            var room = await _db.GuestHouseRooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == request.RoomTypeId && r.GuestHouseId == request.GuestHouseId && r.IsActive);
            if (room == null)
                return NotFound(new { Success = false, Message = "Room not found." });

            // Validate check-in / check-out.
            if (!request.CheckInDate.HasValue || !request.CheckOutDate.HasValue)
                return BadRequest(new { Success = false, Message = "Check-in and Check-out are required." });

            var checkIn = request.CheckInDate.Value.Date.Add(request.CheckInTime ?? TimeSpan.FromHours(14));
            var checkOut = request.CheckOutDate.Value.Date.Add(request.CheckOutTime ?? TimeSpan.FromHours(12));
            if (checkOut <= checkIn)
                return BadRequest(new { Success = false, Message = "Check-out must be after check-in." });

            var numberOfNights = (checkOut.Date - checkIn.Date).Days;
            if (numberOfNights < 1)
                return BadRequest(new { Success = false, Message = "Invalid stay duration." });

            var numberOfRooms = request.NumberOfRooms ?? 1;
            if (numberOfRooms < 1)
                return BadRequest(new { Success = false, Message = "Number of rooms must be at least 1." });

            var extraCotQuantity = request.ExtraBeds ?? 0;
            if (extraCotQuantity < 0)
                return BadRequest(new { Success = false, Message = "Invalid additional bed quantity." });

            // Validate required guest details.
            if (string.IsNullOrWhiteSpace(request.GuestName))
                return BadRequest(new { Success = false, Message = "Guest Name is required." });
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return BadRequest(new { Success = false, Message = "Phone Number is required." });

            // Re-check availability for the whole stay period.
            var available = await GetAvailableQuantityForPeriodAsync(room.Id, checkIn.Date, checkOut.Date);
            if (available < numberOfRooms)
                return Conflict(new
                {
                    Success = false,
                    Message = $"Only {available} room(s) of this type are available for the selected dates. Please reduce the number of rooms or choose another room type."
                });

            // Pricing (5% GST per the Guest House requirement).
            var roomPrice = room.PricePerNight;
            var extraCotPrice = room.ExtraCotPrice ?? 0m;
            var roomCost = roomPrice * numberOfRooms * numberOfNights;
            var extraBedCost = extraCotPrice * extraCotQuantity * numberOfNights;
            var subtotal = roomCost + extraBedCost;
            var tax = Math.Round(subtotal * 0.05m, 2, MidpointRounding.AwayFromZero);
            var total = subtotal + tax;

            // Payment method & status.
            var paymentMethod = request.PaymentMethod;
            var isPayAfterStay = paymentMethod == GuestHousePaymentMethod.PayAfterStay;
            var bookingStatus = isPayAfterStay ? GuestHouseBookingStatus.Confirmed : GuestHouseBookingStatus.Draft;
            var paymentStatus = isPayAfterStay ? GuestHousePaymentStatus.Pending : GuestHousePaymentStatus.Pending;

            var booking = new GuestHouseBooking
            {
                BookingReference = GenerateBookingReference(),
                GuestHouseId = house.Id,
                GuestHouseRoomId = room.Id,
                CheckInDate = checkIn.Date,
                CheckInTime = request.CheckInTime,
                CheckOutDate = checkOut.Date,
                CheckOutTime = request.CheckOutTime,
                NumberOfNights = numberOfNights,
                NumberOfRooms = numberOfRooms,
                NumberOfPersons = request.NumberOfPersons,
                NumberOfAdults = request.NumberOfAdults,
                NumberOfChildren = request.NumberOfChildren,
                ExtraCotQuantity = extraCotQuantity,
                RoomPrice = roomPrice,
                ExtraCotPrice = extraCotPrice > 0 ? extraCotPrice : null,
                SubTotal = subtotal,
                TaxAmount = tax,
                TotalAmount = total,
                BookingStatus = bookingStatus,
                PaymentStatus = paymentStatus,
                CreatedBy = userId,
                CreatedAt = DateTime.Now,
                UpdatedBy = userId,
                UpdatedAt = DateTime.Now,
                Guests = new List<GuestHouseBookingGuest>
                {
                    new GuestHouseBookingGuest
                    {
                        EmployeeOrDealerCode = request.EmployeeOrDealerCode,
                        GuestName = request.GuestName,
                        CompanyName = request.CompanyName,
                        PhoneNumber = request.PhoneNumber,
                        Email = request.Email,
                        AadhaarOrPassportNumber = request.AadhaarOrPassportNumber,
                        Nationality = request.Nationality,
                        NumberOfPersons = request.NumberOfPersons,
                        NumberOfAdults = request.NumberOfAdults,
                        NumberOfChildren = request.NumberOfChildren,
                        Address = request.Address
                    }
                },
                Payments = new List<GuestHouseBookingPayment>
                {
                    new GuestHouseBookingPayment
                    {
                        PaymentMethod = paymentMethod,
                        PaymentStatus = paymentStatus,
                        Amount = total,
                        PaymentDate = isPayAfterStay ? null : DateTime.Now,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }
                }
            };

            _db.GuestHouseBookings.Add(booking);
            await _db.SaveChangesAsync();

            return Ok(new BookingResultDto
            {
                Success = true,
                BookingId = booking.Id,
                BookingReference = booking.BookingReference,
                GuestHouseId = house.Id,
                GuestHouseName = house.Name,
                RoomType = room.RoomType ?? "Room",
                CheckIn = checkIn,
                CheckOut = checkOut,
                NumberOfNights = numberOfNights,
                NumberOfRooms = numberOfRooms,
                ExtraBeds = extraCotQuantity,
                SubTotal = subtotal,
                TaxAmount = tax,
                TotalAmount = total,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                BookingStatus = bookingStatus
            });
        }

        // GET /api/GuestHouseBooking/my-bookings
		// Bookings belonging to the currently logged-in user (server-enforced via the JWT name claim).
		[Authorize]
		[HttpGet("my-bookings")]
		public async Task<IActionResult> GetMyBookings()
		{
			var userName = User.Identity?.Name;
			if (string.IsNullOrWhiteSpace(userName))
				return Unauthorized(new { Success = false, Message = "Authentication required." });

			var bookings = await _db.GuestHouseBookings
				.AsNoTracking()
				.Where(b => b.CreatedBy == userName)
				.Include(b => b.GuestHouse)
				.Include(b => b.GuestHouseRoom)
				.Include(b => b.Guests)
				.OrderByDescending(b => b.CreatedAt)
				.ToListAsync();

			var houseIds = bookings.Select(b => b.GuestHouseId).Distinct().ToList();
			var images = houseIds.Count > 0
				? await _db.GuestHouseImages
					.AsNoTracking()
					.Where(i => houseIds.Contains(i.GuestHouseId) && i.IsActive)
					.ToListAsync()
				: new List<GuestHouseImage>();

			var today = DateTime.Today;
			var summary = new BookingSummaryDto
			{
				Total = bookings.Count
			};

			foreach (var b in bookings)
			{
				if (b.BookingStatus == GuestHouseBookingStatus.Cancelled) summary.Cancelled++;
				else if (b.BookingStatus == GuestHouseBookingStatus.Completed) summary.Completed++;
				else if (b.CheckInDate.HasValue && b.CheckInDate.Value.Date >= today) summary.Upcoming++;
			}

			var items = bookings.Select(b =>
			{
				var cover = images
					.Where(i => i.GuestHouseId == b.GuestHouseId)
					.OrderBy(i => i.IsPrimary ? 0 : 1)
					.ThenBy(i => i.DisplayOrder)
					.Select(i => i.FilePath)
					.FirstOrDefault();

				var guest = b.Guests.FirstOrDefault();
				var numberOfGuests = b.NumberOfPersons
					?? guest?.NumberOfPersons
					?? (b.NumberOfAdults ?? 0) + (b.NumberOfChildren ?? 0);

				return new MyBookingDto
				{
					BookingId = b.Id,
					BookingReference = b.BookingReference ?? $"BK{b.Id}",
					GuestHouseId = b.GuestHouseId,
					GuestHouseName = b.GuestHouse?.Name ?? "",
					RoomImagePath = cover,
					RoomType = b.GuestHouseRoom?.RoomType ?? "Room",
					RoomNumber = b.GuestHouseRoom?.RoomNumber,
					BookingStatus = BookingStatusName(b.BookingStatus),
					PaymentStatus = PaymentStatusName(b.PaymentStatus),
					CheckInDate = b.CheckInDate,
					CheckInTime = b.CheckInTime,
					CheckOutDate = b.CheckOutDate,
					CheckOutTime = b.CheckOutTime,
					NumberOfNights = b.NumberOfNights,
					NumberOfRooms = b.NumberOfRooms,
					NumberOfGuests = numberOfGuests,
					TotalAmount = b.TotalAmount,
					CreatedAt = b.CreatedAt
				};
			}).ToList();

			return Ok(new MyBookingsResponse
			{
				Bookings = items,
				Summary = summary
			});
		}

		// GET /api/GuestHouseBooking/bookings/{id}
		// Full booking summary. Ownership is enforced server-side: a booking can only be read
		// by the same logged-in user that created it.
		[Authorize]
		[HttpGet("bookings/{id:int}")]
		public async Task<IActionResult> GetBookingDetails(int id)
		{
			var userName = User.Identity?.Name;
			if (string.IsNullOrWhiteSpace(userName))
				return Unauthorized(new { Success = false, Message = "Authentication required." });

			var booking = await _db.GuestHouseBookings
				.AsNoTracking()
				.Include(b => b.GuestHouse)
				.ThenInclude(h => h.Images.Where(i => i.IsActive))
				.Include(b => b.GuestHouseRoom)
				.Include(b => b.Guests)
				.Include(b => b.Payments)
				.FirstOrDefaultAsync(b => b.Id == id);

			if (booking == null || !string.Equals(booking.CreatedBy, userName, StringComparison.OrdinalIgnoreCase))
				return NotFound(new { Success = false, Message = "Booking not found." });

			var cover = booking.GuestHouse?.Images
				.OrderBy(i => i.IsPrimary ? 0 : 1)
				.ThenBy(i => i.DisplayOrder)
				.Select(i => i.FilePath)
				.FirstOrDefault();

			var guest = booking.Guests.FirstOrDefault();
			var payment = booking.Payments.FirstOrDefault();

			var numberOfRooms = booking.NumberOfRooms ?? 1;
			var numberOfNights = booking.NumberOfNights ?? 1;
			var roomCost = booking.RoomPrice * numberOfRooms * numberOfNights;
			var extraBedCharges = (booking.ExtraCotPrice ?? 0m) * (booking.ExtraCotQuantity ?? 0) * numberOfNights;

			return Ok(new BookingDetailsDto
			{
				BookingId = booking.Id,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomImagePath = cover,
				RoomType = booking.GuestHouseRoom?.RoomType ?? "Room",
				RoomNumber = booking.GuestHouseRoom?.RoomNumber,
				CheckInDate = booking.CheckInDate,
				CheckInTime = booking.CheckInTime,
				CheckOutDate = booking.CheckOutDate,
				CheckOutTime = booking.CheckOutTime,
				NumberOfNights = booking.NumberOfNights,
				NumberOfRooms = booking.NumberOfRooms,
				NumberOfPersons = booking.NumberOfPersons,
				NumberOfAdults = booking.NumberOfAdults,
				NumberOfChildren = booking.NumberOfChildren,
				ExtraBeds = booking.ExtraCotQuantity,
				RoomPrice = booking.RoomPrice,
				ExtraCotPrice = booking.ExtraCotPrice,
				RoomCost = roomCost,
				ExtraBedCharges = extraBedCharges,
				SubTotal = booking.SubTotal,
				TaxAmount = booking.TaxAmount,
				TotalAmount = booking.TotalAmount,
				BookingStatus = BookingStatusName(booking.BookingStatus),
				PaymentStatus = PaymentStatusName(booking.PaymentStatus),
				PaymentMethod = payment != null ? PaymentMethodName(payment.PaymentMethod) : "",
				PaymentDate = payment?.PaymentDate,
				CreatedAt = booking.CreatedAt,
				CancellationStatus = booking.BookingStatus == GuestHouseBookingStatus.Cancelled ? "Cancelled" : null,
				EmployeeOrDealerCode = guest?.EmployeeOrDealerCode,
				GuestName = guest?.GuestName,
				CompanyName = guest?.CompanyName,
				PhoneNumber = guest?.PhoneNumber,
				Email = guest?.Email,
				AadhaarOrPassportNumber = guest?.AadhaarOrPassportNumber,
				Nationality = guest?.Nationality,
				Address = guest?.Address
			});
		}

		private static string BookingStatusName(GuestHouseBookingStatus status)
		{
			return status switch
			{
				GuestHouseBookingStatus.Draft => "Draft",
				GuestHouseBookingStatus.PendingPayment => "Pending Payment",
				GuestHouseBookingStatus.Confirmed => "Confirmed",
				GuestHouseBookingStatus.Cancelled => "Cancelled",
				GuestHouseBookingStatus.Completed => "Completed",
				_ => status.ToString()
			};
		}

		private static string PaymentStatusName(GuestHousePaymentStatus status)
		{
			return status switch
			{
				GuestHousePaymentStatus.Pending => "Pending",
				GuestHousePaymentStatus.Paid => "Paid",
				GuestHousePaymentStatus.Failed => "Failed",
				GuestHousePaymentStatus.Refunded => "Refunded",
				GuestHousePaymentStatus.PartiallyRefunded => "Partially Refunded",
				_ => status.ToString()
			};
		}

		private static string PaymentMethodName(GuestHousePaymentMethod method)
		{
			return method switch
			{
				GuestHousePaymentMethod.Razorpay => "Razorpay",
				GuestHousePaymentMethod.UPI => "UPI",
				GuestHousePaymentMethod.Card => "Card",
				GuestHousePaymentMethod.NetBanking => "Net Banking",
				GuestHousePaymentMethod.PayAfterStay => "Pay After Stay",
				_ => method.ToString()
			};
		}

        // Computes the minimum number of available rooms for this room type across every night
        // of the stay, reusing the same logic as the availability endpoint.
        private async Task<int> GetAvailableQuantityForPeriodAsync(int roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            var room = await _db.GuestHouseRooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return 0;

            var nights = Enumerable
                .Range(0, (checkOutDate - checkInDate).Days)
                .Select(i => checkInDate.AddDays(i))
                .ToList();

            var availabilityRows = await _db.GuestHouseRoomAvailabilities
                .AsNoTracking()
                .Where(a => a.GuestHouseRoomId == roomId && a.Date >= checkInDate && a.Date < checkOutDate)
                .ToListAsync();

            var periodAvailability = room.AvailableQuantity;
            bool hasAnyRows = false;

            foreach (var night in nights)
            {
                var rowsForNight = availabilityRows.Where(a => a.Date.Date == night).ToList();
                if (rowsForNight.Count == 0) continue;

                hasAnyRows = true;
                if (rowsForNight.Any(a => a.IsBlocked))
                {
                    periodAvailability = 0;
                    break;
                }

                var nightAvailable = Math.Max(0, rowsForNight.Min(a => a.AvailableRooms));
                periodAvailability = Math.Min(periodAvailability, nightAvailable);
                if (periodAvailability <= 0) break;
            }

            return periodAvailability > 0 ? periodAvailability : 0;
        }

        private static string GenerateBookingReference()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var code = new char[8];
            for (int i = 0; i < code.Length; i++)
                code[i] = chars[random.Next(chars.Length)];
            return "BK" + new string(code);
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

	public class CreateBookingRequest
	{
		public int GuestHouseId { get; set; }
		public int RoomTypeId { get; set; }
		public DateTime? CheckInDate { get; set; }
		public TimeSpan? CheckInTime { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public TimeSpan? CheckOutTime { get; set; }
		public int? NumberOfRooms { get; set; }
		public int? ExtraBeds { get; set; }
		public int? NumberOfPersons { get; set; }
		public int? NumberOfAdults { get; set; }
		public int? NumberOfChildren { get; set; }
		public string? EmployeeOrDealerCode { get; set; }
		public string? GuestName { get; set; }
		public string? CompanyName { get; set; }
		public string? PhoneNumber { get; set; }
		public string? Email { get; set; }
		public string? AadhaarOrPassportNumber { get; set; }
		public string? Nationality { get; set; }
		public string? Address { get; set; }
		public GuestHousePaymentMethod PaymentMethod { get; set; }
	}

	public class BookingResultDto
	{
		public bool Success { get; set; }
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string RoomType { get; set; } = "";
		public DateTime CheckIn { get; set; }
		public DateTime CheckOut { get; set; }
		public int NumberOfNights { get; set; }
		public int NumberOfRooms { get; set; }
		public int ExtraBeds { get; set; }
		public decimal SubTotal { get; set; }
		public decimal TaxAmount { get; set; }
		public decimal TotalAmount { get; set; }
		public GuestHousePaymentMethod PaymentMethod { get; set; }
		public GuestHousePaymentStatus PaymentStatus { get; set; }
		public GuestHouseBookingStatus BookingStatus { get; set; }
	}

	public class MyBookingsResponse
	{
		public List<MyBookingDto> Bookings { get; set; } = new List<MyBookingDto>();
		public BookingSummaryDto Summary { get; set; } = new BookingSummaryDto();
	}

	public class MyBookingDto
	{
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string? RoomImagePath { get; set; }
		public string RoomType { get; set; } = "";
		public string? RoomNumber { get; set; }
		public string BookingStatus { get; set; } = "";
		public string PaymentStatus { get; set; } = "";
		public DateTime? CheckInDate { get; set; }
		public TimeSpan? CheckInTime { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public TimeSpan? CheckOutTime { get; set; }
		public int? NumberOfNights { get; set; }
		public int? NumberOfRooms { get; set; }
		public int? NumberOfGuests { get; set; }
		public decimal? TotalAmount { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class BookingSummaryDto
	{
		public int Total { get; set; }
		public int Upcoming { get; set; }
		public int Completed { get; set; }
		public int Cancelled { get; set; }
	}

	public class BookingDetailsDto
	{
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public string GuestHouseName { get; set; } = "";
		public string? RoomImagePath { get; set; }
		public string RoomType { get; set; } = "";
		public string? RoomNumber { get; set; }

		public DateTime? CheckInDate { get; set; }
		public TimeSpan? CheckInTime { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public TimeSpan? CheckOutTime { get; set; }
		public int? NumberOfNights { get; set; }
		public int? NumberOfRooms { get; set; }
		public int? NumberOfPersons { get; set; }
		public int? NumberOfAdults { get; set; }
		public int? NumberOfChildren { get; set; }
		public int? ExtraBeds { get; set; }

		public decimal RoomPrice { get; set; }
		public decimal? ExtraCotPrice { get; set; }
		public decimal RoomCost { get; set; }
		public decimal ExtraBedCharges { get; set; }
		public decimal? SubTotal { get; set; }
		public decimal? TaxAmount { get; set; }
		public decimal? TotalAmount { get; set; }

		public string BookingStatus { get; set; } = "";
		public string PaymentStatus { get; set; } = "";
		public string PaymentMethod { get; set; } = "";
		public DateTime? PaymentDate { get; set; }
		public DateTime CreatedAt { get; set; }
		public string? CancellationStatus { get; set; }

		public string? EmployeeOrDealerCode { get; set; }
		public string? GuestName { get; set; }
		public string? CompanyName { get; set; }
		public string? PhoneNumber { get; set; }
		public string? Email { get; set; }
		public string? AadhaarOrPassportNumber { get; set; }
		public string? Nationality { get; set; }
		public string? Address { get; set; }
	}
}