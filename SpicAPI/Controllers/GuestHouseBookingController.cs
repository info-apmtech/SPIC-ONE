using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spic.Infrastructure.Data;
using SpicAPI.Services;
using SPIC.Core.Entities;
using System.Data;
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

		// POST /api/GuestHouseBooking/documents/upload-temp
		// Stage 1 of the ID Proof upload flow: the customer selects a file on the Guest
		// Details page, BEFORE a booking exists. The file is validated and stored under a
		// temporary holding folder (same Uploads root used by every other upload in the
		// app - no new storage architecture); a TempToken is returned so it can be linked
		// to the real booking once CreateBooking succeeds. No database row is created yet
		// (GuestHouseBookingDocument.GuestHouseBookingId is a required FK - there is no
		// booking to point at until the booking is actually created).
		[Authorize]
		[HttpPost("documents/upload-temp")]
		[RequestSizeLimit(6 * 1024 * 1024)]
		public async Task<IActionResult> UploadTempDocument(IFormFile? file)
		{
			if (file == null || file.Length == 0)
				return BadRequest(new { Success = false, Message = "No file uploaded." });

			var validationError = ValidateIdProofFile(file);
			if (validationError != null)
				return BadRequest(new { Success = false, Message = validationError });

			try
			{
				var tempRoot = Path.Combine(GetUploadsRoot(), "GuestHouseBookingDocuments", "_temp");
				Directory.CreateDirectory(tempRoot);
				CleanupOldTempFiles(tempRoot);

				var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
				var token = Guid.NewGuid().ToString("N");
				var physicalPath = Path.Combine(tempRoot, token + ext);

				await using (var stream = new FileStream(physicalPath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				return Ok(new
				{
					Success = true,
					TempToken = token,
					FileName = Path.GetFileName(file.FileName),
					ContentType = ResolveContentType(ext),
					FileSize = file.Length
				});
			}
			catch (Exception)
			{
				return StatusCode(500, new { Success = false, Message = "The file could not be uploaded. Please try again." });
			}
		}

		// DELETE /api/GuestHouseBooking/documents/upload-temp/{token}
		// Lets the customer remove a not-yet-submitted ID Proof from the Guest Details page
		// before continuing. Safe no-op if the token does not resolve to a temp file.
		[Authorize]
		[HttpDelete("documents/upload-temp/{token}")]
		public IActionResult RemoveTempDocument(string token)
		{
			if (!IsValidToken(token))
				return BadRequest(new { Success = false, Message = "Invalid document reference." });

			var tempRoot = Path.Combine(GetUploadsRoot(), "GuestHouseBookingDocuments", "_temp");
			if (Directory.Exists(tempRoot))
			{
				foreach (var f in Directory.GetFiles(tempRoot, token + ".*"))
				{
					try { System.IO.File.Delete(f); } catch (Exception) { /* best effort */ }
				}
			}

			return Ok(new { Success = true });
		}

		// GET /api/GuestHouseBooking/documents/{documentId}/view
		// Opens the document inline (PDF viewer / image) - no Content-Disposition header.
		[Authorize]
		[HttpGet("documents/{documentId:int}/view")]
		public async Task<IActionResult> ViewDocument(int documentId) => await ServeDocumentAsync(documentId, download: false);

		// GET /api/GuestHouseBooking/documents/{documentId}/download
		// Forces a download of the actual stored file (never Base64-in-JSON).
		[Authorize]
		[HttpGet("documents/{documentId:int}/download")]
		public async Task<IActionResult> DownloadDocument(int documentId) => await ServeDocumentAsync(documentId, download: true);

		// Shared authorization + file-serving for both View and Download.
		// Authorization: the booking's owner (CreatedBy) OR Admin/CorporateAdmin (Front Office).
		// A mismatch returns 404 (not 403) so a guessed BookingId/DocumentId cannot even
		// reveal that the document exists - the same anti-enumeration pattern already used
		// by GetBookingDetails/DownloadInvoice.
		private async Task<IActionResult> ServeDocumentAsync(int documentId, bool download)
		{
			var userName = User.Identity?.Name;
			if (string.IsNullOrWhiteSpace(userName))
				return Unauthorized(new { Success = false, Message = "Authentication required." });

			var doc = await _db.Set<GuestHouseBookingDocument>()
				.AsNoTracking()
				.Include(d => d.GuestHouseBooking)
				.FirstOrDefaultAsync(d => d.Id == documentId);

			if (doc?.GuestHouseBooking == null || string.IsNullOrWhiteSpace(doc.FilePath))
				return NotFound(new { Success = false, Message = "Document not found." });

			var isOwner = string.Equals(doc.GuestHouseBooking.CreatedBy, userName, StringComparison.OrdinalIgnoreCase);
			var isFrontOffice = User.IsInRole("Admin") || User.IsInRole("CorporateAdmin");
			if (!isOwner && !isFrontOffice)
				return NotFound(new { Success = false, Message = "Document not found." });

			var root = GetUploadsRoot();
			var normalized = doc.FilePath.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
			var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

			if (!fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
				return NotFound(new { Success = false, Message = "Document not found." });

			var contentType = string.IsNullOrWhiteSpace(doc.ContentType) ? "application/octet-stream" : doc.ContentType;

			return download
				? PhysicalFile(fullPath, contentType, string.IsNullOrWhiteSpace(doc.FileName) ? "document" : doc.FileName)
				: PhysicalFile(fullPath, contentType);
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

			// Rooms already committed (Draft/PendingPayment/Confirmed/CheckedIn) by an existing
			// booking whose stay window overlaps the requested period. The master inventory rows
			// above are only ever the capacity ceiling (admin-maintained); this is what actually
			// tracks live demand against that ceiling.
			var committedByRoom = await GetCommittedRoomsByRoomAsync(roomIds, checkIn.Date, checkOut.Date);

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

				// Subtract rooms already held by overlapping bookings for this room type.
				var committed = committedByRoom.TryGetValue(room.Id, out var committedCount) ? committedCount : 0;
				periodAvailability = Math.Max(0, periodAvailability - committed);

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

            // The availability re-check and the insert must happen atomically: without this,
            // two nearly-simultaneous requests for the same room/overlapping dates could both
            // read "room available" before either has saved, and both would then be allowed to
            // book it. Serializable isolation makes Postgres detect that race at commit time
            // (the second commit fails with SqlState 40001) instead of silently double-booking.
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            int available;
            try
            {
                available = await GetAvailableQuantityForPeriodAsync(room.Id, checkIn.Date, checkOut.Date);
            }
            catch (PostgresException ex) when (ex.SqlState == "40001")
            {
                await transaction.RollbackAsync();
                return Conflict(new
                {
                    Success = false,
                    Message = "This room was just booked by someone else for the selected dates. Please try again."
                });
            }

            if (available < numberOfRooms)
            {
                await transaction.RollbackAsync();
                return Conflict(new
                {
                    Success = false,
                    Message = $"Only {available} room(s) of this type are available for the selected dates. Please reduce the number of rooms or choose another room type."
                });
            }

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

            try
            {
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "40001" })
            {
                await transaction.RollbackAsync();
                return Conflict(new
                {
                    Success = false,
                    Message = "This room was just booked by someone else for the selected dates. Please try again."
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "40001")
            {
                await transaction.RollbackAsync();
                return Conflict(new
                {
                    Success = false,
                    Message = "This room was just booked by someone else for the selected dates. Please try again."
                });
            }

            // Best-effort, additive: the booking itself is already fully committed above.
            // Linking the previously-uploaded ID Proof (if any) happens AFTER commit so a
            // problem here can never roll back or corrupt the booking that was just created.
            string? documentWarning = null;
            if (!string.IsNullOrWhiteSpace(request.IdProofTempToken))
            {
                documentWarning = await LinkIdProofDocumentAsync(booking.Id, request.IdProofTempToken, request.IdProofFileName, userId);
            }

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
                BookingStatus = bookingStatus,
                DocumentWarning = documentWarning
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
				.Include(b => b.Documents)
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
			var hasInvoice = await _db.GuestHouseBills.AnyAsync(b => b.GuestHouseBookingId == id);

			return Ok(new BookingDetailsDto
			{
				BookingId = booking.Id,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomImagePath = cover,
				RoomType = booking.GuestHouseRoom?.RoomType ?? "Room",
				RoomNumber = booking.GuestHouseRoom?.RoomNumber,
				HasInvoice = hasInvoice,
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
				Address = guest?.Address,
				Documents = booking.Documents
					.Select(d => new BookingDocumentDto
					{
						DocumentId = d.Id,
						DocumentType = d.DocumentType,
						FileName = d.FileName,
						ContentType = d.ContentType,
						FileSize = d.FileSize,
						UploadedAt = d.UploadedAt
					})
					.ToList()
			});
		}

		// GET /api/GuestHouseBooking/bookings/{id}/invoice
		// Customer-side Download Invoice. Reuses the SAME generated GuestHouseBill data and the
		// SAME Tax Invoice PDF builder (GuestHouseInvoicePdfBuilder) as the Front Office / Admin
		// download, so the customer's invoice is byte-identical to the admin invoice. It is a thin
		// authorization wrapper ONLY: it does NOT create a bill, does NOT recalculate GST/billing,
		// and does NOT change the existing Generate Bill flow.
		//
		// Server-side authorization chain:
		//   authenticated customer -> owns/has access to booking -> payment is Paid ->
		//   existing bill belongs to booking -> allow PDF download
		[Authorize]
		[HttpGet("bookings/{id:int}/invoice")]
		public async Task<IActionResult> DownloadInvoice(int id)
		{
			var userName = User.Identity?.Name;
			if (string.IsNullOrWhiteSpace(userName))
				return Unauthorized(new { Success = false, Message = "Authentication required." });

			var booking = await _db.GuestHouseBookings
				.AsNoTracking()
				.FirstOrDefaultAsync(b => b.Id == id);

			// Ownership: only the user who created the booking may download its invoice. We
			// return NotFound (same as GetBookingDetails) so a bookingId belonging to another
			// customer never reveals that the booking/invoice exists.
			if (booking == null || !string.Equals(booking.CreatedBy, userName, StringComparison.OrdinalIgnoreCase))
				return NotFound(new { Success = false, Message = "Booking not found." });

			// Payment: a final Tax Invoice is only downloadable once the payment is actually
			// completed (existing backend PaymentStatus).
			if (booking.PaymentStatus != GuestHousePaymentStatus.Paid)
				return BadRequest(new { Success = false, Message = "The invoice is available only after payment has been completed." });

			// Bill: reuse the existing generated bill for this booking. We never create one here.
			var bill = await _db.GuestHouseBills
				.AsNoTracking()
				.Include(b => b.LineItems)
				.FirstOrDefaultAsync(b => b.GuestHouseBookingId == id);
			if (bill == null)
				return NotFound(new { Success = false, Message = "No invoice has been generated for this booking yet." });

			try
			{
				var (companyName, gstNumber) = await GuestHouseBillPdfHelper.ResolveGuestCompanyAndGstAsync(_db, bill.GuestHouseBookingId);
				var bytes = GuestHouseInvoicePdfBuilder.Build(GuestHouseBillPdfHelper.ToBillViewDto(bill, companyName, gstNumber));
				var fileName = $"{bill.BillNumber ?? $"BILL-{bill.Id:000000}"}.pdf";
				return File(bytes, "application/pdf", fileName);
			}
			catch (Exception)
			{
				return StatusCode(500, new { Success = false, Message = "Failed to generate the invoice PDF. Please try again." });
			}
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
        // of the stay, reusing the same logic as the availability endpoint, and then subtracts
        // whatever is already committed by overlapping bookings so the check reflects real demand.
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

            periodAvailability = periodAvailability > 0 ? periodAvailability : 0;

            var committedByRoom = await GetCommittedRoomsByRoomAsync(new[] { roomId }, checkInDate, checkOutDate);
            var committed = committedByRoom.TryGetValue(roomId, out var committedCount) ? committedCount : 0;

            var remaining = periodAvailability - committed;
            return remaining > 0 ? remaining : 0;
        }

        // Sum of NumberOfRooms already held (every status except Cancelled and Completed -
        // i.e. Draft/PendingPayment/Confirmed/CheckedIn all still occupy inventory) by bookings
        // of the given room(s) whose stay window overlaps [checkInDate, checkOutDate).
        //
        // This is the single, authoritative source of "how much of a room type's inventory is
        // currently spoken for" by real bookings. GuestHouseRoom.AvailableQuantity and
        // GuestHouseRoomAvailability remain exactly what they were: an admin-maintained capacity
        // ceiling. Neither is written here or anywhere in the booking lifecycle - only read.
        private async Task<Dictionary<int, int>> GetCommittedRoomsByRoomAsync(IReadOnlyCollection<int> roomIds, DateTime checkInDate, DateTime checkOutDate)
        {
            if (roomIds.Count == 0)
                return new Dictionary<int, int>();

            var rows = await _db.GuestHouseBookings
                .AsNoTracking()
                .Where(b => roomIds.Contains(b.GuestHouseRoomId)
                    && b.BookingStatus != GuestHouseBookingStatus.Cancelled
                    && b.BookingStatus != GuestHouseBookingStatus.Completed
                    && b.CheckInDate.HasValue && b.CheckOutDate.HasValue
                    && b.CheckInDate.Value.Date < checkOutDate.Date
                    && b.CheckOutDate.Value.Date > checkInDate.Date)
                .Select(b => new { b.GuestHouseRoomId, b.NumberOfRooms })
                .ToListAsync();

            return rows
                .GroupBy(r => r.GuestHouseRoomId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.NumberOfRooms ?? 1));
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

		// ---- ID Proof document helpers ----

		private static readonly string[] AllowedIdProofExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

		// Same ad hoc extension + size validation style already used by UploadHouseImage -
		// there is no centralized file-validation service in the project to reuse.
		private static string? ValidateIdProofFile(IFormFile file)
		{
			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedIdProofExtensions.Contains(ext))
				return "Only PDF, JPG, or PNG files are allowed.";

			if (file.Length > 5 * 1024 * 1024)
				return "File must be 5 MB or less.";

			return null;
		}

		private static string ResolveContentType(string ext) => ext switch
		{
			".pdf" => "application/pdf",
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			_ => "application/octet-stream"
		};

		private static bool IsValidToken(string? token) =>
			!string.IsNullOrWhiteSpace(token) &&
			System.Text.RegularExpressions.Regex.IsMatch(token, "^[0-9a-fA-F]{32}$");

		// Opportunistic cleanup of abandoned temp uploads (customer selected a file but never
		// completed the booking). Runs best-effort on every new temp upload; never throws.
		private static void CleanupOldTempFiles(string tempRoot)
		{
			try
			{
				var cutoff = DateTime.UtcNow.AddHours(-24);
				foreach (var f in Directory.GetFiles(tempRoot))
				{
					if (System.IO.File.GetCreationTimeUtc(f) < cutoff)
					{
						try { System.IO.File.Delete(f); } catch (Exception) { /* best effort */ }
					}
				}
			}
			catch (Exception) { /* best effort */ }
		}

		// Stage 2 of the ID Proof upload flow: moves the temp file into its final
		// per-booking folder and creates the GuestHouseBookingDocument row, now that the
		// booking has a real Id. Reuses the SAME entity the model already defines
		// (SPIC.Core.Entities.GuestHouseBookingDocument / table "GuestHouseBookingDocument"),
		// accessed via _db.Set&lt;T&gt;() since it is already part of the EF model through the
		// GuestHouseBooking.Documents navigation - no schema change, no DbSet needed.
		// Returns a user-facing warning string on failure, or null on success.
		private async Task<string?> LinkIdProofDocumentAsync(int bookingId, string tempToken, string? originalFileName, string? userId)
		{
			try
			{
				if (!IsValidToken(tempToken))
					return "The uploaded ID proof reference was invalid, so it was not attached to this booking.";

				var tempRoot = Path.Combine(GetUploadsRoot(), "GuestHouseBookingDocuments", "_temp");
				var tempFile = Directory.Exists(tempRoot)
					? Directory.GetFiles(tempRoot, tempToken + ".*").FirstOrDefault()
					: null;

				if (tempFile == null)
					return "The uploaded ID proof could not be found, so it was not attached to this booking. You can add it later from My Bookings.";

				var ext = Path.GetExtension(tempFile);
				var finalFolder = Path.Combine(GetUploadsRoot(), "GuestHouseBookingDocuments", bookingId.ToString());
				Directory.CreateDirectory(finalFolder);

				var storedName = $"idproof_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
				var finalPath = Path.Combine(finalFolder, storedName);
				System.IO.File.Move(tempFile, finalPath);

				var relativePath = $"GuestHouseBookingDocuments/{bookingId}/{storedName}";

				_db.Set<GuestHouseBookingDocument>().Add(new GuestHouseBookingDocument
				{
					GuestHouseBookingId = bookingId,
					DocumentType = "IDProof",
					FileName = string.IsNullOrWhiteSpace(originalFileName) ? storedName : originalFileName,
					FilePath = relativePath,
					ContentType = ResolveContentType(ext),
					FileSize = new FileInfo(finalPath).Length,
					IsVerified = false,
					UploadedBy = userId,
					UploadedAt = DateTime.Now
				});
				await _db.SaveChangesAsync();

				return null;
			}
			catch (Exception)
			{
				return "The booking was created, but the ID proof document could not be attached. You can upload it again from My Bookings.";
			}
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

		// Optional ID Proof, uploaded to documents/upload-temp on the Guest Details page
		// before the booking existed. When present, it is linked to the booking right
		// after it is created.
		public string? IdProofTempToken { get; set; }
		public string? IdProofFileName { get; set; }
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

		// Non-null only when an ID Proof was supplied but could not be attached; the
		// booking itself is always created successfully regardless of this value.
		public string? DocumentWarning { get; set; }
	}

	public class BookingDocumentDto
	{
		public int DocumentId { get; set; }
		public string? DocumentType { get; set; }
		public string? FileName { get; set; }
		public string? ContentType { get; set; }
		public long? FileSize { get; set; }
		public DateTime UploadedAt { get; set; }
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

		// True when a Tax Invoice (GuestHouseBill) has already been generated for this
		// booking, i.e. the customer may download it from Booking Details.
		public bool HasInvoice { get; set; }

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

		public List<BookingDocumentDto> Documents { get; set; } = new List<BookingDocumentDto>();
	}
}