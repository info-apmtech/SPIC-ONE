using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Payments;
using SpicAPI.Services;
using SPIC.Core.Entities;
using System.Data;
using System.Globalization;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Front Office / Room Management for the Guest House booking system.
	///
	/// Built entirely on top of the existing Guest House booking data model:
	///   - GuestHouse / GuestHouseRoom (room types, each a display "room" row)
	///   - GuestHouseBooking (booking life-cycle: Confirmed -> CheckedIn -> Completed)
	///   - GuestHouseBookingPayment / GuestHouseBookingGuest
	///
	/// Room availability is NOT hard-coded and is NOT derived from the check-in date
	/// passing. A room's current status is computed dynamically from the actual
	/// booking records for that room type:
	///   - Checked-In: there is a booking with GuestHouseBookingStatus.CheckedIn
	///   - Reserved/Booked: there is a valid (Confirmed) booking not yet checked in
	///   - Available/Completed: no active booking remains (previous stay checked out)
	///
	/// Accessible only to Admin / CorporateAdmin (the existing administrative roles).
	/// </summary>
	/// <remarks>
	/// Reads are additionally open to the Designation permission of the SDWA page that owns them
	/// (FrontOffice / GenerateBill / GuestHouseCancellations); see
	/// <see cref="GuestHouseFrontOfficeAccessAttribute"/> below. The old class-level
	/// [Authorize(Roles = "Admin,CorporateAdmin")] could only read the JWT role claim and therefore
	/// never saw a Designation. Writes are decided by the same attribute (GHAdmin / SDWAAdmin
	/// designations and Receptionists for the Front Office / Generate Bill actions; cancellation
	/// approve / reject stay Admin / CorporateAdmin / SuperAdmin only).
	/// </remarks>
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	[GuestHouseFrontOfficeAccess]
	public class GuestHouseFrontOfficeController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly GuestHouseBookingOptions _bookingOptions;
		private readonly ILogger<GuestHouseFrontOfficeController> _logger;

		public GuestHouseFrontOfficeController(
			AppDbContext db,
			IOptions<GuestHouseBookingOptions> bookingOptions,
			ILogger<GuestHouseFrontOfficeController> logger)
		{
			_db = db;
			_bookingOptions = bookingOptions.Value;
			_logger = logger;
		}

		// A PendingPayment booking only holds its room for this long from its own CreatedAt -
		// same rule, same configuration, as GuestHouseBookingController.GetCommittedRoomsByRoomAsync,
		// so Front Office room occupancy never disagrees with the customer-facing availability check.
		private DateTime PendingPaymentHoldCutoff => DateTime.Now.AddMinutes(-_bookingOptions.PendingPaymentHoldMinutes);

		// GET /api/GuestHouseFrontOffice/rooms
		// Returns every active guest house and its rooms with a dynamically computed
		// current status plus the occupying booking's details (if any).
		//
		// A GuestHouseRoom row is a ROOM TYPE with an inventory quantity (e.g. "AC" with
		// AvailableQuantity 4 starting at RoomNumber 111 => physical rooms 111, 112, 113,
		// 114). The grid therefore lists one row PER PHYSICAL ROOM so the Front Office can
		// see exactly which room numbers are Available, Reserved or Checked-In today.
		// A room is treated as occupied while ANY active booking (Draft/PendingPayment/
		// Confirmed/CheckedIn - the same statuses the availability engine counts) with a
		// stay overlapping today holds it, either via an exact GuestHouseRoomAllocation or
		// via the deterministic lowest-free-number assumption for yet-unallocated bookings.
		[HttpGet("rooms")]
		public async Task<IActionResult> GetRooms()
		{
			// Receptionist state scope (null = unrestricted): only the user's state's guest house.
			var scope = await GetGuestHouseScopeAsync();

			var houses = await _db.GuestHouses
				.AsNoTracking()
				.Where(h => h.IsActive)
				.Where(h => scope == null || scope.Contains(h.Id))
				.OrderBy(h => h.Name)
				.Select(h => new FrontOfficeHouseDto
				{
					GuestHouseId = h.Id,
					GuestHouseName = h.Name
				})
				.ToListAsync();

			var rooms = await _db.GuestHouseRooms
				.AsNoTracking()
				.Where(r => r.IsActive)
				.Where(r => scope == null || scope.Contains(r.GuestHouseId))
				.Include(r => r.GuestHouse)
				.OrderBy(r => r.GuestHouseId)
				.ThenBy(r => r.RoomNumber)
				.ToListAsync();

			var todayStart = DateTime.Today;
			var todayEnd = todayStart.AddDays(1);

			// Every inventory-holding booking overlapping today: Cancelled/Completed never hold
			// inventory; Confirmed/CheckedIn always do; PendingPayment only within its configured
			// hold window (PendingPaymentHoldCutoff) - matching GuestHouseBookingController's
			// GetCommittedRoomsByRoomAsync exactly, so this view never disagrees with availability.
			var pendingPaymentHoldCutoff = PendingPaymentHoldCutoff;
			var activeBookings = await _db.GuestHouseBookings
				.AsNoTracking()
				.Where(b => b.BookingStatus != GuestHouseBookingStatus.Cancelled
					&& b.BookingStatus != GuestHouseBookingStatus.Completed
					&& (b.BookingStatus != GuestHouseBookingStatus.PendingPayment || b.CreatedAt >= pendingPaymentHoldCutoff)
					&& b.CheckInDate.HasValue && b.CheckOutDate.HasValue
					&& b.CheckInDate.Value.Date < todayEnd
					&& b.CheckOutDate.Value.Date > todayStart)
				.Where(b => scope == null || scope.Contains(b.GuestHouseId))
				.Include(b => b.Guests)
				.ToListAsync();

			var activeBookingIds = activeBookings.Select(b => b.Id).ToList();
			var allocations = activeBookingIds.Count > 0
				? await _db.GuestHouseRoomAllocations
					.AsNoTracking()
					.Where(a => activeBookingIds.Contains(a.GuestHouseBookingId))
					.ToListAsync()
				: new List<GuestHouseRoomAllocation>();

			var housesById = houses.ToDictionary(h => h.GuestHouseId);
			foreach (var house in houses)
			{
				house.Rooms ??= new List<FrontOfficeRoomDto>();
			}

			foreach (var room in rooms)
			{
				if (room.GuestHouse == null || !housesById.TryGetValue(room.GuestHouse!.Id, out var house))
					continue;

				var numbers = EnumeratePhysicalRoomNumbers(room, room.AvailableQuantity);
				var roomAllocations = allocations.Where(a => a.GuestHouseRoomId == room.Id).ToList();

				// Build per-booking unallocated room counts for this room type.
				// Each booking's remaining = max(0, NumberOfRooms - already-allocated rows).
				var activeForType = activeBookings
					.Where(b => b.GuestHouseRoomId == room.Id)
					.OrderBy(b => b.CheckInDate)
					.ThenBy(b => b.Id)
					.ToList();

				var unallocatedRemaining = new Dictionary<int, int>();
				foreach (var b in activeForType)
				{
					var allocated = roomAllocations
						.Count(a => a.GuestHouseBookingId == b.Id && ContainsNumber(numbers, a.RoomNumber));
					var remaining = Math.Max(0, (b.NumberOfRooms ?? 1) - allocated);
					if (remaining > 0)
						unallocatedRemaining[b.Id] = remaining;
				}

				// Pre-build deterministic room-to-booking assignment for unallocated rooms.
				// Assigns the lowest free room numbers to bookings in CheckInDate then Id order.
				var roomToBooking = new Dictionary<string, GuestHouseBooking>();
				foreach (var b in activeForType)
				{
					if (!unallocatedRemaining.TryGetValue(b.Id, out var slotsNeeded) || slotsNeeded <= 0)
						continue;
					var assigned = 0;
					foreach (var n in numbers)
					{
						if (assigned >= slotsNeeded) break;
						if (roomToBooking.ContainsKey(n)) continue;
						roomToBooking[n] = b;
						assigned++;
					}
				}

				foreach (var number in numbers)
				{
					GuestHouseBooking? occupant = null;

					// 1. Exact room already allocated to an active booking.
					var exactBooking = roomAllocations
						.Where(a => string.Equals(a.RoomNumber, number, StringComparison.OrdinalIgnoreCase))
						.Select(a => activeBookings.FirstOrDefault(b => b.Id == a.GuestHouseBookingId))
						.Where(b => b != null)
						.OrderByDescending(b => b!.BookingStatus == GuestHouseBookingStatus.CheckedIn)
						.ThenBy(b => b!.CheckInDate)
						.FirstOrDefault();
					if (exactBooking != null)
					{
						occupant = exactBooking;
					}
					// 2. Deterministic per-booking assignment: each unallocated booking
					//    gets its own distinct physical room(s), not the same first booking.
					else if (roomToBooking.TryGetValue(number, out var assignedBooking))
					{
						occupant = assignedBooking;
					}

					var roomDto = new FrontOfficeRoomDto
					{
						RoomId = room.Id,
						RoomNumber = number,
						RoomType = room.RoomType ?? "Room",
						GuestHouseId = room.GuestHouseId,
						GuestHouseName = house.GuestHouseName,
						Capacity = room.Capacity,
						PricePerNight = room.PricePerNight,
						AvailableQuantity = room.AvailableQuantity
					};

					if (occupant != null)
					{
						var status = occupant.BookingStatus == GuestHouseBookingStatus.CheckedIn
							? FrontOfficeRoomStatus.CheckedIn
							: FrontOfficeRoomStatus.Reserved;

						roomDto.RoomStatus = status;
						roomDto.StatusText = StatusText(status);
						roomDto.CurrentBookingId = occupant.Id;
						roomDto.BookingReference = occupant.BookingReference ?? $"BK{occupant.Id}";
						roomDto.GuestName = occupant.Guests.FirstOrDefault()?.GuestName;
						roomDto.CheckInDate = occupant.CheckInDate;
						roomDto.CheckOutDate = occupant.CheckOutDate;
						roomDto.BookingStatus = occupant.BookingStatus;
						roomDto.PaymentStatus = occupant.PaymentStatus;
						roomDto.PaymentMethod = occupant.Payments.FirstOrDefault()?.PaymentMethod;
					}
					else
					{
						roomDto.RoomStatus = FrontOfficeRoomStatus.Available;
						roomDto.StatusText = StatusText(FrontOfficeRoomStatus.Available);
					}

					house.Rooms.Add(roomDto);
				}
			}

			return Ok(houses);
		}

		// GET /api/GuestHouseFrontOffice/bookings/{bookingId}
		// Returns the details needed to display the Check-In form and validates that
		// the booking is currently eligible for check-in.
		[HttpGet("bookings/{bookingId:int}")]
		public async Task<IActionResult> GetBookingForCheckIn(int bookingId)
		{
			var booking = await LoadBookingAsync(bookingId);
			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found." });

			var checkInState = CheckInEligibility(booking);
			var guest = booking.Guests.FirstOrDefault();
			var payment = booking.Payments.FirstOrDefault();

			// The exact physical room numbers the Front Office may allocate for this stay:
			// everything free for the booked type over the booked dates (excluding any room
			// this same booking holds). "Occupied" are the rooms already taken by other
			// overlapping active stays (allocated or deterministically count-held).
			var eligible = checkInState.CanCheckIn
				&& booking.GuestHouseRoomId > 0
				&& booking.CheckInDate.HasValue && booking.CheckOutDate.HasValue;
			var freeRooms = eligible
				? await GetFreePhysicalRoomsAsync(booking.GuestHouseRoomId, booking.CheckInDate!.Value.Date, booking.CheckOutDate!.Value.Date, booking.Id)
				: new FreeRoomsResult(new List<string>(), new List<string>());

			return Ok(new FrontOfficeBookingDto
			{
				BookingId = booking.Id,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomType = booking.GuestHouseRoom?.RoomType ?? "Room",
				RoomNumber = AllocatedRoomNumber(booking),
				GuestName = guest?.GuestName,
				NumberOfPersons = booking.NumberOfPersons ?? guest?.NumberOfPersons,
				ExtraBeds = booking.ExtraCotQuantity ?? 0,
				Capacity = booking.GuestHouseRoom?.Capacity,
				CheckInDate = booking.CheckInDate,
				CheckInTime = booking.CheckInTime,
				CheckOutDate = booking.CheckOutDate,
				CheckOutTime = booking.CheckOutTime,
				BookingStatus = booking.BookingStatus,
				PaymentStatus = booking.PaymentStatus,
				PaymentMethod = payment?.PaymentMethod,
				CanCheckIn = checkInState.CanCheckIn,
				CheckInMessage = checkInState.Message,
				CanCheckOut = booking.BookingStatus == GuestHouseBookingStatus.CheckedIn,
				TotalAmount = booking.TotalAmount,
				ActualCheckInAt = booking.ActualCheckInAt,
				AvailableRoomNumbers = freeRooms.Free,
				AllocatedRoomNumbers = freeRooms.Occupied,
				RequiredRoomNumbers = Math.Max(1, booking.NumberOfRooms ?? 1),
				Documents = booking.Documents
					.Select(d => new FrontOfficeBookingDocumentDto
					{
						DocumentId = d.Id,
						DocumentType = d.DocumentType,
						FileName = d.FileName,
						ContentType = d.ContentType,
						FileSize = d.FileSize
					})
					.ToList()
			});
		}

		// POST /api/GuestHouseFrontOffice/checkin
		// Marks a valid, confirmed booking as Checked-In. Also allows the Front Office
		// to update the number of persons and extra beds on the existing booking record.
		//
		// The Front Office MUST explicitly select the exact physical RoomNumber(s) to
		// assign (SelectedRoomNumbers - one per booked room). The assignment is persistecd
		// as a GuestHouseRoomAllocation row per room and is validated atomically: the final
		// free-room check and the allocation insert run inside a serializable transaction,
		// so two desks can never assign the same physical room to overlapping stays.
		[HttpPost("checkin")]
		public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
		{
			if (request == null || request.BookingId <= 0)
				return BadRequest(new { Success = false, Message = "Invalid request." });

			var booking = await LoadBookingAsync(request.BookingId);
			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found." });

			var eligibility = CheckInEligibility(booking);
			if (!eligibility.CanCheckIn)
				return BadRequest(new { Success = false, Message = eligibility.Message });

			// Guard against duplicate check-in (server-enforced even if the UI is stale).
			if (booking.BookingStatus == GuestHouseBookingStatus.CheckedIn)
				return BadRequest(new { Success = false, Message = "This booking has already been checked in." });
			if (booking.ActualCheckInAt.HasValue)
				return BadRequest(new { Success = false, Message = "This booking has already been checked in." });

			if (booking.GuestHouseRoomId <= 0 || !booking.CheckInDate.HasValue || !booking.CheckOutDate.HasValue)
				return BadRequest(new { Success = false, Message = "This booking's stay details are incomplete." });

			// Require one explicit physical room number per booked room.
			var requiredRooms = Math.Max(1, booking.NumberOfRooms ?? 1);
			var selected = (request.SelectedRoomNumbers ?? new List<string>())
				.Select(s => s?.Trim())
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s!)
				.ToList();
			if (selected.Count != requiredRooms)
				return BadRequest(new { Success = false, Message = $"Please select exactly {requiredRooms} free physical room(s) for this booking." });
			if (selected.Distinct(StringComparer.OrdinalIgnoreCase).Count() != selected.Count)
				return BadRequest(new { Success = false, Message = "Duplicate room numbers cannot be assigned." });

			// The free-room re-check and the allocation insert must happen atomically. An
			// EXCLUDE constraint on the allocation table (added with the required schema
			// migration) makes PostgreSQL reject the same physical room number being assigned
			// to two overlapping active stays; serializable isolation turns the resulting
			// race into SqlState 40001 at commit time, which we surface as a friendly message.
			await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

			FreeRoomsResult freeRooms;
			try
			{
				freeRooms = await GetFreePhysicalRoomsAsync(booking.GuestHouseRoomId, booking.CheckInDate.Value.Date, booking.CheckOutDate.Value.Date, booking.Id);
			}
			catch (PostgresException ex) when (ex.SqlState == "40001")
			{
				await transaction.RollbackAsync();
				return Conflict(new { Success = false, Message = "The room list changed. Please refresh and try again." });
			}

			var noLongerFree = selected
				.Where(s => !freeRooms.Free.Contains(s, StringComparer.OrdinalIgnoreCase))
				.ToList();
			if (noLongerFree.Count > 0)
			{
				await transaction.RollbackAsync();
				return Conflict(new
				{
					Success = false,
					Message = $"Room(s) {string.Join(", ", noLongerFree)} are no longer free for the selected dates. Please choose another room."
				});
			}

			// Persist the Front Office updates onto the existing booking record.
			var guest = booking.Guests.FirstOrDefault();
			if (request.NumberOfPersons.HasValue && request.NumberOfPersons.Value >= 1)
			{
				booking.NumberOfPersons = request.NumberOfPersons.Value;
				if (guest != null) guest.NumberOfPersons = request.NumberOfPersons.Value;
			}
			if (request.ExtraBeds.HasValue)
			{
				var extraBeds = Math.Max(0, request.ExtraBeds.Value);
				booking.ExtraCotQuantity = extraBeds;
			}

			booking.BookingStatus = GuestHouseBookingStatus.CheckedIn;
			booking.ActualCheckInAt = DateTime.Now;
			booking.UpdatedAt = DateTime.Now;
			booking.UpdatedBy = User.Identity?.Name;

			// Make the check-in idempotent: replace any earlier allocations of this booking
			// with the freshly selected room numbers.
			var existingAllocations = await _db.GuestHouseRoomAllocations
				.Where(a => a.GuestHouseBookingId == booking.Id)
				.ToListAsync();
			_db.GuestHouseRoomAllocations.RemoveRange(existingAllocations);

			var assignedBy = User.Identity?.Name;
			var checkInDate = booking.CheckInDate.Value.Date;
			var checkOutDate = booking.CheckOutDate.Value.Date;
			foreach (var number in selected)
			{
				_db.GuestHouseRoomAllocations.Add(new GuestHouseRoomAllocation
				{
					GuestHouseBookingId = booking.Id,
					GuestHouseId = booking.GuestHouseId,
					GuestHouseRoomId = booking.GuestHouseRoomId,
					RoomNumber = number,
					CheckInDate = checkInDate,
					CheckOutDate = checkOutDate,
					AssignedBy = assignedBy,
					AssignedAt = DateTime.Now
				});
			}

			try
			{
				await _db.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "40001" })
			{
				await transaction.RollbackAsync();
				return Conflict(new { Success = false, Message = "These rooms were just assigned to another overlapping stay. Please refresh and try again." });
			}
			catch (PostgresException ex) when (ex.SqlState == "40001")
			{
				await transaction.RollbackAsync();
				return Conflict(new { Success = false, Message = "These rooms were just assigned to another overlapping stay. Please refresh and try again." });
			}

			return Ok(new
			{
				Success = true,
				BookingId = booking.Id,
				BookingReference = booking.BookingReference,
				RoomNumbers = selected,
				Message = "Guest checked in successfully."
			});
		}

		// GET /api/GuestHouseFrontOffice/checkout/{bookingId}
		// Returns the Check-Out confirmation summary (guest, dates, persons, extra beds,
		// total amount, payment status) for a booking that is currently Checked-In.
		[HttpGet("checkout/{bookingId:int}")]
		public async Task<IActionResult> GetCheckOutSummary(int bookingId)
		{
			var booking = await LoadBookingAsync(bookingId);
			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found." });

			if (booking.BookingStatus != GuestHouseBookingStatus.CheckedIn)
				return BadRequest(new { Success = false, Message = "Check-Out is only allowed for a booking that is currently checked in." });

			var guest = booking.Guests.FirstOrDefault();
			var payment = booking.Payments.FirstOrDefault();
			var paymentReceived = payment != null && payment.PaymentStatus == GuestHousePaymentStatus.Paid
				|| booking.PaymentStatus == GuestHousePaymentStatus.Paid;

			// Total already snapshotted = room cost + extra cot cost + tax.
			return Ok(new CheckOutSummaryDto
			{
				BookingId = booking.Id,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				GuestName = guest?.GuestName,
				RoomNumber = AllocatedRoomNumber(booking),
				RoomType = booking.GuestHouseRoom?.RoomType ?? "Room",
				CheckInDate = booking.CheckInDate,
				ActualCheckInAt = booking.ActualCheckInAt,
				CheckOutDate = booking.CheckOutDate,
				NumberOfPersons = booking.NumberOfPersons ?? guest?.NumberOfPersons,
				ExtraBeds = booking.ExtraCotQuantity ?? 0,
				TotalAmount = booking.TotalAmount ?? 0,
				PaymentStatus = booking.PaymentStatus,
				PaymentMethod = payment?.PaymentMethod,
				PaymentReceived = paymentReceived
			});
		}

		// POST /api/GuestHouseFrontOffice/pay
		// Records/confirms payment at check-out for "Pay After Stay" (or any pending)
		// bookings. Idempotent: re-recording a payment for an already paid booking is a no-op.
		[HttpPost("pay")]
		public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
		{
			if (request == null || request.BookingId <= 0)
				return BadRequest(new { Success = false, Message = "Invalid request." });

			var booking = await LoadBookingAsync(request.BookingId);
			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found." });

			if (booking.BookingStatus != GuestHouseBookingStatus.CheckedIn)
				return BadRequest(new { Success = false, Message = "Payment can only be recorded for a booking that is currently checked in." });

			if (booking.PaymentStatus == GuestHousePaymentStatus.Paid)
				return Ok(new { Success = true, Message = "Payment has already been received." });

			booking.PaymentStatus = GuestHousePaymentStatus.Paid;
			booking.UpdatedAt = DateTime.Now;
			booking.UpdatedBy = User.Identity?.Name;

			var payment = booking.Payments.FirstOrDefault();
			if (payment != null)
			{
				payment.PaymentStatus = GuestHousePaymentStatus.Paid;
				payment.PaymentDate = DateTime.Now;
				if (!string.IsNullOrWhiteSpace(request.TransactionId))
					payment.TransactionId = request.TransactionId;
				payment.UpdatedAt = DateTime.Now;
			}
			else
			{
				booking.Payments.Add(new GuestHouseBookingPayment
				{
					PaymentMethod = GuestHousePaymentMethod.PayAfterStay,
					PaymentStatus = GuestHousePaymentStatus.Paid,
					Amount = booking.TotalAmount ?? 0,
					TransactionId = request.TransactionId,
					PaymentDate = DateTime.Now,
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now
				});
			}

			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Payment received and confirmed." });
		}

		// POST /api/GuestHouseFrontOffice/checkout
		// Completes the stay for a currently Checked-In booking, marking it Completed and
		// making the room available for a new booking. Requires payment to have been received.
		[HttpPost("checkout")]
		public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
		{
			if (request == null || request.BookingId <= 0)
				return BadRequest(new { Success = false, Message = "Invalid request." });

			var booking = await LoadBookingAsync(request.BookingId);
			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found." });

			if (booking.BookingStatus != GuestHouseBookingStatus.CheckedIn)
				return BadRequest(new { Success = false, Message = "Check-Out is only allowed for a booking that is currently checked in." });

			var paymentReceived = booking.PaymentStatus == GuestHousePaymentStatus.Paid
				|| booking.Payments.Any(p => p.PaymentStatus == GuestHousePaymentStatus.Paid);
			if (!paymentReceived)
				return BadRequest(new { Success = false, Message = "Payment must be received before check-out." });

			booking.BookingStatus = GuestHouseBookingStatus.Completed;
			booking.ActualCheckOutAt = DateTime.Now;
			booking.UpdatedAt = DateTime.Now;
			booking.UpdatedBy = User.Identity?.Name;

			// Snapshot the exact physical room number(s) BEFORE the allocation rows are
			// removed below - this is the only place the true assigned room is recorded,
			// and billing/invoicing (which only runs after check-out) needs it.
			var allocatedRoomNumber = AllocatedRoomNumber(booking);
			if (!string.IsNullOrWhiteSpace(allocatedRoomNumber))
				booking.AllocatedRoomNumber = allocatedRoomNumber;

			// Release the physical room(s) back to the pool so they can be assigned again.
			var allocatedRooms = await _db.GuestHouseRoomAllocations
				.Where(a => a.GuestHouseBookingId == booking.Id)
				.ToListAsync();
			_db.GuestHouseRoomAllocations.RemoveRange(allocatedRooms);

			await _db.SaveChangesAsync();

			return Ok(new
			{
				Success = true,
				BookingId = booking.Id,
				BookingReference = booking.BookingReference,
				Message = "Check-out completed. Room is now available."
			});
		}

		// =====================================================================
		//  CANCELLATION APPROVAL (Admin side)
		//
		//  Dealer request (GuestHouseBookingController POST bookings/{id}/cancel) only
		//  records a PendingApproval GuestHouseBookingCancellation. These endpoints are the
		//  only place that ever cancels the booking or releases the room. Refunds are NOT
		//  automatic: they are processed manually outside the application (no Razorpay refund).
		//  Approve / reject: Admin / CorporateAdmin / SuperAdmin only - enforced by
		//  GuestHouseFrontOfficeAccessAttribute.
		// =====================================================================

		// GET /api/GuestHouseFrontOffice/cancellations?status=PendingApproval|Approved|Rejected|All
		[HttpGet("cancellations")]
		public async Task<IActionResult> GetCancellationRequests([FromQuery] string? status)
		{
			var query = _db.Set<GuestHouseBookingCancellation>()
				.AsNoTracking()
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.GuestHouse)
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.GuestHouseRoom)
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.Guests)
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.Payments)
				.AsQueryable();

			var scope = await GetGuestHouseScopeAsync();
			if (scope != null)
				query = query.Where(c => scope.Contains(c.GuestHouseBooking!.GuestHouseId));

			if (string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(c => c.ApprovalStatus == GuestHouseCancellationApprovalStatus.PendingApproval);
			}
			else if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
			{
				if (!Enum.TryParse<GuestHouseCancellationApprovalStatus>(status, true, out var parsed))
					return BadRequest(new { Success = false, Message = "Invalid status filter." });
				query = query.Where(c => c.ApprovalStatus == parsed);
			}

			var rows = await query.OrderByDescending(c => c.CancelledAt).ToListAsync();

			var bookingIds = rows.Select(c => c.GuestHouseBookingId).ToList();
			var refunds = await _db.Set<GuestHouseBookingRefund>()
				.AsNoTracking()
				.Where(r => bookingIds.Contains(r.GuestHouseBookingId))
				.ToDictionaryAsync(r => r.GuestHouseBookingId);

			var items = rows.Select(c =>
			{
				var b = c.GuestHouseBooking!;
				var guest = b.Guests.FirstOrDefault();
				var payment = b.Payments.FirstOrDefault();
				refunds.TryGetValue(c.GuestHouseBookingId, out var refund);
				return new CancellationRequestListItemDto
				{
					CancellationId = c.Id,
					CancellationReference = c.CancellationReference,
					BookingId = b.Id,
					BookingReference = b.BookingReference ?? $"BK{b.Id}",
					GuestHouseName = b.GuestHouse?.Name ?? "",
					RoomType = b.GuestHouseRoom?.RoomType ?? "Room",
					GuestName = guest?.GuestName,
					EmployeeOrDealerCode = guest?.EmployeeOrDealerCode,
					PhoneNumber = guest?.PhoneNumber,
					CheckInDate = b.CheckInDate,
					CheckOutDate = b.CheckOutDate,
					BookingStatus = b.BookingStatus.ToString(),
					PaymentStatus = b.PaymentStatus.ToString(),
					PaymentMethod = payment?.PaymentMethod.ToString() ?? "",
					TotalAmount = b.TotalAmount ?? 0,
					CancellationCharge = c.CancellationCharge ?? 0,
					TaxAdjustment = c.TaxAdjustment ?? 0,
					RefundAmount = c.RefundAmount ?? 0,
					CancellationReason = c.CancellationReason,
					RequestedBy = c.CancelledBy,
					RequestedAt = c.CancelledAt,
					ApprovalStatus = c.ApprovalStatus.ToString(),
					RefundStatus = c.RefundStatus.ToString(),
					AdminDecisionBy = c.AdminDecisionBy,
					AdminDecisionAt = c.AdminDecisionAt,
					RejectionReason = c.RejectionReason,
					Remarks = c.Remarks,
					RefundReference = refund?.RefundReference,
					EstimatedRefundDate = c.EstimatedRefundDate
				};
			}).ToList();

			return Ok(items);
		}

		// POST /api/GuestHouseFrontOffice/cancellations/{id}/approve
		//
		// Approves the dealer's cancellation request: the booking is cancelled and its room
		// released. No refund is created here - the refund is processed manually outside the
		// application (client requirement), so the request is recorded with RefundStatus Pending
		// when a refund is due.
		//
		// Duplicate safety: an already Approved request returns its current state, and the
		// PendingApproval -> Approved transition is a conditional UPDATE (see
		// GuestHouseRefundStatusSync.FinalizeApprovalAsync), so a concurrent second approve or a
		// reject affects 0 rows.
		[HttpPost("cancellations/{id:int}/approve")]
		public async Task<IActionResult> ApproveCancellation(int id)
		{
			var adminName = User.Identity?.Name;

			var scope = await GetGuestHouseScopeAsync();
			var cancellation = await _db.Set<GuestHouseBookingCancellation>()
				.AsNoTracking()
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.Payments)
				.Where(c => scope == null || scope.Contains(c.GuestHouseBooking!.GuestHouseId))
				.FirstOrDefaultAsync(c => c.Id == id);
			if (cancellation == null || cancellation.GuestHouseBooking == null)
				return NotFound(new { Success = false, Message = "Cancellation request not found." });

			var booking = cancellation.GuestHouseBooking;

			if (cancellation.ApprovalStatus == GuestHouseCancellationApprovalStatus.Approved)
			{
				return Ok(new
				{
					Success = true,
					AlreadyProcessed = true,
					ApprovalStatus = cancellation.ApprovalStatus.ToString(),
					RefundStatus = cancellation.RefundStatus.ToString(),
					Message = "This cancellation has already been approved."
				});
			}
			if (cancellation.ApprovalStatus == GuestHouseCancellationApprovalStatus.Rejected)
				return BadRequest(new { Success = false, Message = "This cancellation request has already been rejected." });

			switch (booking.BookingStatus)
			{
				case GuestHouseBookingStatus.Cancelled:
					return BadRequest(new { Success = false, Message = "This booking is already cancelled." });
				case GuestHouseBookingStatus.Completed:
					return BadRequest(new { Success = false, Message = "A completed stay cannot be cancelled." });
				case GuestHouseBookingStatus.CheckedIn:
					return BadRequest(new { Success = false, Message = "The guest is checked in. Check the guest out instead of cancelling." });
			}

			GuestHouseRefundStatusSync.FinalizeOutcome finalized;
			try
			{
				finalized = await GuestHouseRefundStatusSync.FinalizeApprovalAsync(_db, cancellation.Id, adminName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Approving cancellation {CancellationId} (booking {BookingId}) failed.", cancellation.Id, booking.Id);
				return StatusCode(500, new { Success = false, Message = "The approval could not be saved. The booking has NOT been cancelled; please try again." });
			}

			var current = await _db.Set<GuestHouseBookingCancellation>().AsNoTracking().FirstAsync(c => c.Id == cancellation.Id);
			if (finalized == GuestHouseRefundStatusSync.FinalizeOutcome.AlreadyFinalized)
			{
				if (current.ApprovalStatus == GuestHouseCancellationApprovalStatus.Rejected)
					return BadRequest(new { Success = false, Message = "This cancellation request has already been rejected." });
				return Ok(new
				{
					Success = true,
					AlreadyProcessed = true,
					ApprovalStatus = current.ApprovalStatus.ToString(),
					RefundStatus = current.RefundStatus.ToString(),
					Message = "This cancellation has already been approved."
				});
			}

			var refundAmount = current.RefundAmount ?? 0m;
			return Ok(new
			{
				Success = true,
				AlreadyProcessed = false,
				ApprovalStatus = current.ApprovalStatus.ToString(),
				RefundStatus = current.RefundStatus.ToString(),
				RefundAmount = refundAmount,
				EstimatedRefundDate = current.EstimatedRefundDate,
				Message = refundAmount > 0
					? "Cancellation approved and room released. The refund will be processed manually."
					: "Cancellation approved and room released. No refund was due."
			});
		}

		// POST /api/GuestHouseFrontOffice/cancellations/{id}/reject
		// Records the Admin's rejection only - no refund, the booking stays active and the
		// room stays held.
		[HttpPost("cancellations/{id:int}/reject")]
		public async Task<IActionResult> RejectCancellation(int id, [FromBody] RejectCancellationRequest? request)
		{
			var reason = request?.Reason?.Trim();
			if (string.IsNullOrWhiteSpace(reason))
				return BadRequest(new { Success = false, Message = "Please enter a reason for rejection." });

			var adminName = User.Identity?.Name;
			var now = DateTime.Now;

			// Receptionist state scope: another state's cancellation is "not found".
			var scope = await GetGuestHouseScopeAsync();
			if (scope != null && !await _db.Set<GuestHouseBookingCancellation>()
					.AnyAsync(c => c.Id == id && scope.Contains(c.GuestHouseBooking!.GuestHouseId)))
				return NotFound(new { Success = false, Message = "Cancellation request not found." });

			// Conditional update so a reject can never race an in-flight approval/refund.
			var updated = await _db.Set<GuestHouseBookingCancellation>()
				.Where(c => c.Id == id
					&& c.ApprovalStatus == GuestHouseCancellationApprovalStatus.PendingApproval
					&& c.RefundStatus != GuestHouseRefundStatus.Processing)
				.ExecuteUpdateAsync(s => s
					.SetProperty(c => c.ApprovalStatus, GuestHouseCancellationApprovalStatus.Rejected)
					.SetProperty(c => c.RejectionReason, reason)
					.SetProperty(c => c.AdminDecisionBy, adminName)
					.SetProperty(c => c.AdminDecisionAt, now));

			if (updated == 1)
				return Ok(new { Success = true, AlreadyProcessed = false, ApprovalStatus = GuestHouseCancellationApprovalStatus.Rejected.ToString(), Message = "Cancellation request rejected." });

			var existing = await _db.Set<GuestHouseBookingCancellation>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
			if (existing == null)
				return NotFound(new { Success = false, Message = "Cancellation request not found." });
			if (existing.ApprovalStatus == GuestHouseCancellationApprovalStatus.Rejected)
				return Ok(new { Success = true, AlreadyProcessed = true, ApprovalStatus = existing.ApprovalStatus.ToString(), Message = "This request has already been rejected." });
			if (existing.ApprovalStatus == GuestHouseCancellationApprovalStatus.Approved)
				return BadRequest(new { Success = false, Message = "This request has already been approved and cannot be rejected." });
			return Conflict(new { Success = false, Message = "A refund for this request is in progress or unconfirmed, so it cannot be rejected now." });
		}

		// =====================================================================
		//  BILLING (Generate Bill -> Bill List -> Print / Download Invoice)
		// =====================================================================

		// GET /api/GuestHouseFrontOffice/bills/booking/{bookingId}
		// Returns the booking data needed to build the Generate Bill page, pre-filled
		// with default room / additional-bed line items computed from the booking.
		// Also returns whether a bill already exists (to prevent duplicate generation).
		[HttpGet("bills/booking/{bookingId:int}")]
		public async Task<IActionResult> GetBillDraft(int bookingId)
		{
			var booking = await LoadBookingAsync(bookingId);
			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found." });

			return await BuildBillDraftAsync(booking);
		}

		// GET /api/GuestHouseFrontOffice/bills/booking/byref/{reference}
		// Same as the numeric draft lookup but resolves the booking by its reference string.
		[HttpGet("bills/booking/byref/{reference}")]
		public async Task<IActionResult> GetBillDraftByReference(string reference)
		{
			if (string.IsNullOrWhiteSpace(reference))
				return BadRequest(new { Success = false, Message = "Booking reference is required." });

			var scope = await GetGuestHouseScopeAsync();
			var booking = await _db.GuestHouseBookings
				.Include(b => b.GuestHouse)
				.Include(b => b.GuestHouseRoom)
				.Include(b => b.Guests)
				.Include(b => b.Payments)
				.Where(b => scope == null || scope.Contains(b.GuestHouseId))
				.FirstOrDefaultAsync(b => b.BookingReference == reference.Trim());

			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found for the given reference." });

			return await BuildBillDraftAsync(booking);
		}

		// GET /api/GuestHouseFrontOffice/billing/completed?date=yyyy-MM-dd  (single-day, backward-compatible)
		// GET /api/GuestHouseFrontOffice/billing/completed?from=yyyy-MM-dd&to=yyyy-MM-dd  (date range)
		// Returns the bookings that are eligible for bill generation within the specified
		// date window (matched against the actual check-out / completion date). When no
		// date parameters are supplied, defaults to today. Only COMPLETED bookings whose
		// payment has been received are returned. Each row also reports whether a bill
		// already exists for the booking.
		[HttpGet("billing/completed")]
		public async Task<IActionResult> GetCompletedBookingsForBilling([FromQuery] DateTime? date, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
		{
			DateTime rangeFrom;
			DateTime rangeTo;

			if (from.HasValue || to.HasValue)
			{
				rangeFrom = (from ?? DateTime.Today).Date;
				rangeTo = (to ?? DateTime.Today).Date.AddDays(1);
			}
			else
			{
				var target = date?.Date ?? DateTime.Today;
				rangeFrom = target;
				rangeTo = target.AddDays(1);
			}

			var scope = await GetGuestHouseScopeAsync();
			var bookings = await _db.GuestHouseBookings
				.AsNoTracking()
				.Where(b => scope == null || scope.Contains(b.GuestHouseId))
				.Where(b =>
					b.BookingStatus == GuestHouseBookingStatus.Completed
					&& (b.PaymentStatus == GuestHousePaymentStatus.Paid
						|| b.Payments.Any(p => p.PaymentStatus == GuestHousePaymentStatus.Paid))
					&& b.ActualCheckOutAt != null
					&& b.ActualCheckOutAt.Value >= rangeFrom
					&& b.ActualCheckOutAt.Value < rangeTo)
				.Include(b => b.GuestHouse)
				.Include(b => b.GuestHouseRoom)
				.Include(b => b.Guests)
				.OrderBy(b => b.ActualCheckOutAt)
				.ToListAsync();

			var existingBillBookingIds = await _db.GuestHouseBills
				.AsNoTracking()
				.Select(b => b.GuestHouseBookingId)
				.ToListAsync();

			var list = bookings.Select(b =>
			{
				var guest = b.Guests.FirstOrDefault();
				var hasBill = existingBillBookingIds.Contains(b.Id);
				return new CompletedBookingForBillingDto
				{
					BookingId = b.Id,
					BookingReference = b.BookingReference ?? $"BK{b.Id}",
					GuestName = guest?.GuestName,
					GuestHouseName = b.GuestHouse?.Name ?? "",
					RoomNumber = ResolveBillRoomNumber(b),
					RoomType = b.GuestHouseRoom?.RoomType ?? "Room",
					CheckInDate = b.ActualCheckInAt ?? (b.CheckInDate.HasValue ? b.CheckInDate.Value.Date.Add(b.CheckInTime ?? TimeSpan.Zero) : (DateTime?)null),
					CheckOutDate = b.ActualCheckOutAt,
					Amount = b.TotalAmount ?? 0,
					PaymentStatus = b.PaymentStatus,
					HasBill = hasBill
				};
			}).ToList();

			return Ok(new
			{
				Date = rangeFrom,
				Count = list.Count,
				Bookings = list
			});
		}

		// GET /api/GuestHouseFrontOffice/bills
		// GET /api/GuestHouseFrontOffice/bills?from=yyyy-MM-dd&to=yyyy-MM-dd
		// Returns generated bills (Bill List page), most recent first.
		// Optional from/to filter by BillDate.
		[HttpGet("bills")]
		public async Task<IActionResult> GetBills([FromQuery] DateTime? from, [FromQuery] DateTime? to)
		{
			var scope = await GetGuestHouseScopeAsync();
			var query = _db.GuestHouseBills.AsNoTracking()
				.Where(b => scope == null || scope.Contains(b.GuestHouseBooking!.GuestHouseId))
				.AsQueryable();

			if (from.HasValue)
				query = query.Where(b => b.BillDate >= from.Value.Date);
			if (to.HasValue)
				query = query.Where(b => b.BillDate < to.Value.Date.AddDays(1));

			var bills = await query.OrderByDescending(b => b.BillDate).ToListAsync();

			var items = bills.Select(b => new BillListItemDto
			{
				BillId = b.Id,
				BillNumber = b.BillNumber ?? $"BILL-{b.Id:000000}",
				BookingReference = b.BookingReference ?? $"BK{b.GuestHouseBookingId}",
				BookingId = b.GuestHouseBookingId,
				GuestName = b.GuestName,
				GuestHouseName = b.GuestHouseName,
				RoomNumber = b.RoomNumber,
				BillDate = b.BillDate,
				CheckInDate = b.CheckInAt,
				CheckOutDate = b.CheckOutAt,
				TotalAmount = b.TotalAmount,
				BalanceAmount = b.BalanceAmount,
				PaymentStatus = (int)b.PaymentStatus,
				PaymentMethod = (int)b.PaymentMethod
			}).ToList();

			return Ok(items);
		}

		// GET /api/GuestHouseFrontOffice/bill/{billId}
		// Returns a single generated bill with its line items (View Bill).
		[HttpGet("bill/{billId:int}")]
		public async Task<IActionResult> GetBill(int billId)
		{
			var scope = await GetGuestHouseScopeAsync();
			var bill = await _db.GuestHouseBills
				.AsNoTracking()
				.Include(b => b.LineItems)
				.Where(b => scope == null || scope.Contains(b.GuestHouseBooking!.GuestHouseId))
				.FirstOrDefaultAsync(b => b.Id == billId);
			if (bill == null)
				return NotFound(new { Success = false, Message = "Bill not found." });

			var (companyName, gstNumber) = await ResolveGuestCompanyAndGstAsync(bill.GuestHouseBookingId);
			return Ok(ToBillViewDto(bill, companyName, gstNumber));
		}

		// POST /api/GuestHouseFrontOffice/generate-bill
		// Validates and creates a bill against a completed booking. Prevents duplicate bills.
		[HttpPost("generate-bill")]
		public async Task<IActionResult> GenerateBill([FromBody] GenerateBillRequest request)
		{
			if (request == null || request.BookingId <= 0)
				return BadRequest(new { Success = false, Message = "Invalid request." });

			var booking = await LoadBookingAsync(request.BookingId);
			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found." });

			if (booking.BookingStatus != GuestHouseBookingStatus.Completed)
				return BadRequest(new { Success = false, Message = "A bill can only be generated for a booking that has been checked out (Completed)." });

			var hasExisting = await _db.GuestHouseBills.AnyAsync(b => b.GuestHouseBookingId == request.BookingId);
			if (hasExisting)
				return BadRequest(new { Success = false, Message = "A bill has already been generated for this booking." });

			if (request.LineItems == null || request.LineItems.Count == 0)
				return BadRequest(new { Success = false, Message = "At least one billing line item is required." });

			if (request.LineItems.Any(i => i.Quantity < 0 || i.Rate < 0 || i.CgstPercent < 0 || i.SgstPercent < 0))
				return BadRequest(new { Success = false, Message = "Quantities, rates and GST percentages must not be negative." });

			// The effective GST for the predefined room / additional-beds lines is the
			// booking's own persisted tax rate (same derivation as the bill draft), so the
			// server never trusts a client-supplied CGST/SGST percentage for them.
			var subTotalForTax = booking.SubTotal ?? 0m;
			var totalGstPercent = subTotalForTax > 0m
				? Math.Round(((booking.TaxAmount ?? 0m) / subTotalForTax) * 100m, 2)
				: 0m;
			var bookingCgstPercent = Math.Round(totalGstPercent / 2m, 2);
			var bookingSgstPercent = Math.Round(totalGstPercent / 2m, 2);

			// Build line items and compute taxes dynamically per line.
			var lineItems = new List<GuestHouseBillLineItem>();
			decimal subtotal = 0m;
			decimal cgstTotal = 0m;
			decimal sgstTotal = 0m;

			foreach (var li in request.LineItems)
			{
				var desc = li.Description ?? "";
				var description = desc;
				var cgstPercent = li.CgstPercent;
				var sgstPercent = li.SgstPercent;

				if (desc.Contains("Room Charge", StringComparison.OrdinalIgnoreCase))
				{
					// System line: description and GST come from the booking, never the client.
					description = $"{booking.GuestHouseRoom?.RoomType ?? "Room"} - Room Charge";
					cgstPercent = bookingCgstPercent;
					sgstPercent = bookingSgstPercent;
				}
				else if (desc.Contains("Additional Beds", StringComparison.OrdinalIgnoreCase)
					|| desc.Contains("Extra Cot", StringComparison.OrdinalIgnoreCase))
				{
					description = "Additional Beds / Extra Cot";
					cgstPercent = bookingCgstPercent;
					sgstPercent = bookingSgstPercent;
				}
				else if (desc.Equals("Food Bill", StringComparison.OrdinalIgnoreCase))
				{
					description = "Food Bill";
					cgstPercent = 0m;
					sgstPercent = 0m;
				}
				else if (desc.Equals("Laundry / Others", StringComparison.OrdinalIgnoreCase))
				{
					description = "Laundry / Others";
					cgstPercent = 0m;
					sgstPercent = 0m;
				}

				var amount = Math.Round(li.Quantity * li.Rate, 2, MidpointRounding.AwayFromZero);
				var cgst = Math.Round(amount * (cgstPercent / 100m), 2, MidpointRounding.AwayFromZero);
				var sgst = Math.Round(amount * (sgstPercent / 100m), 2, MidpointRounding.AwayFromZero);

				subtotal += amount;
				cgstTotal += cgst;
				sgstTotal += sgst;

				lineItems.Add(new GuestHouseBillLineItem
				{
					Description = description,
					Quantity = li.Quantity,
					Rate = li.Rate,
					Amount = amount,
					CgstPercent = cgstPercent,
					SgstPercent = sgstPercent,
					CgstAmount = cgst,
					SgstAmount = sgst,
					LineTotal = amount + cgst + sgst,
					CreatedAt = DateTime.Now
				});
			}

			subtotal = Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);
			cgstTotal = Math.Round(cgstTotal, 2, MidpointRounding.AwayFromZero);
			sgstTotal = Math.Round(sgstTotal, 2, MidpointRounding.AwayFromZero);

			var discount = Math.Max(0, request.Discount ?? 0);
			var advance = Math.Max(0, request.AdvancePayment ?? 0);
			var roundOff = request.RoundOff ?? 0;
			var grossTotal = subtotal + cgstTotal + sgstTotal;
			var balance = grossTotal - discount - advance + roundOff;

			// Validation: discount / advance cannot incorrectly exceed the applicable amount.
			if (discount > grossTotal)
				return BadRequest(new { Success = false, Message = "Discount cannot exceed the total bill amount." });
			if (advance > grossTotal - discount)
				return BadRequest(new { Success = false, Message = "Advance Payment cannot exceed Grand Total." });
			if (balance < 0)
				return BadRequest(new { Success = false, Message = "Bill balance cannot be negative. Check discount, advance and round off values." });

			var guest = booking.Guests.FirstOrDefault();
			var payment = booking.Payments.FirstOrDefault();

			var bill = new GuestHouseBill
			{
				BillNumber = await GenerateBillNumberAsync(),
				GuestHouseBookingId = booking.Id,
				BillDate = DateTime.Now,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestName = guest?.GuestName,
				CompanyName = guest?.CompanyName,
				GstinNumber = guest?.GstinNumber,
				Address = guest?.Address,
				Email = guest?.Email,
				PhoneNumber = guest?.PhoneNumber,
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomNumber = ResolveBillRoomNumber(booking),
				RoomType = booking.GuestHouseRoom?.RoomType ?? "Room",
				CheckInAt = booking.ActualCheckInAt ?? (booking.CheckInDate.HasValue ? booking.CheckInDate.Value.Date.Add(booking.CheckInTime ?? TimeSpan.Zero) : (DateTime?)null),
				CheckOutAt = booking.ActualCheckOutAt ?? (booking.CheckOutDate.HasValue ? booking.CheckOutDate.Value.Date.Add(booking.CheckOutTime ?? TimeSpan.Zero) : (DateTime?)null),
				NumberOfNights = booking.NumberOfNights ?? request.LineItems.Sum(i => (int)Math.Ceiling(i.Quantity)),
				NumberOfRooms = booking.NumberOfRooms,
				NumberOfPersons = booking.NumberOfPersons ?? guest?.NumberOfPersons,
				ExtraBeds = booking.ExtraCotQuantity,
				Subtotal = subtotal,
				CgstAmount = cgstTotal,
				SgstAmount = sgstTotal,
				TotalAmount = grossTotal,
				Discount = discount,
				AdvancePayment = advance,
				RoundOff = roundOff,
				BalanceAmount = balance,
				Remarks = request.Remarks,
				PaymentMethod = (GuestHousePaymentMethod)request.PaymentMethod,
				PaymentStatus = request.PaymentReceived ? GuestHousePaymentStatus.Paid : GuestHousePaymentStatus.Pending,
				CreatedBy = User.Identity?.Name,
				CreatedAt = DateTime.Now,
				UpdatedBy = User.Identity?.Name,
				UpdatedAt = DateTime.Now,
				LineItems = lineItems
			};

			// If the bill is settled now, reflect that on the booking's payment too.
			if (request.PaymentReceived && booking.PaymentStatus != GuestHousePaymentStatus.Paid)
			{
				booking.PaymentStatus = GuestHousePaymentStatus.Paid;
				booking.UpdatedAt = DateTime.Now;
				booking.UpdatedBy = User.Identity?.Name;
				if (payment != null)
				{
					payment.PaymentStatus = GuestHousePaymentStatus.Paid;
					payment.PaymentDate = DateTime.Now;
					payment.UpdatedAt = DateTime.Now;
				}
			}

			_db.GuestHouseBills.Add(bill);
			await _db.SaveChangesAsync();

			return Ok(new
			{
				Success = true,
				BillId = bill.Id,
				BillNumber = bill.BillNumber,
				Message = "Bill generated successfully."
			});
		}

		// GET /api/GuestHouseFrontOffice/bill/{billId}/pdf
		// Generates a print-ready / downloadable Guest House invoice (QuestPDF, reuse of the
		// existing WelfareApplicationPdfBuilder pattern).
		[HttpGet("bill/{billId:int}/pdf")]
		public async Task<IActionResult> GetBillPdf(int billId)
		{
			var scope = await GetGuestHouseScopeAsync();
			var bill = await _db.GuestHouseBills
				.AsNoTracking()
				.Include(b => b.LineItems)
				.Where(b => scope == null || scope.Contains(b.GuestHouseBooking!.GuestHouseId))
				.FirstOrDefaultAsync(b => b.Id == billId);
			if (bill == null)
				return NotFound(new { Success = false, Message = "Bill not found." });

			try
			{
				var (companyName, gstNumber) = await ResolveGuestCompanyAndGstAsync(bill.GuestHouseBookingId);
				var bytes = GuestHouseInvoicePdfBuilder.Build(ToBillViewDto(bill, companyName, gstNumber));
				var fileName = $"{bill.BillNumber ?? $"BILL-{bill.Id:000000}"}.pdf";
				return File(bytes, "application/pdf", fileName);
			}
			catch (Exception)
			{
				return StatusCode(500, new { Success = false, Message = "Failed to generate the invoice PDF. Please try again." });
			}
		}

		private async Task<string> GenerateBillNumberAsync()
		{
			// Monotonic, collision-safe bill reference.
			var datePart = DateTime.Now.ToString("yyyyMMdd");
			var last = await _db.GuestHouseBills
				.AsNoTracking()
				.OrderByDescending(b => b.Id)
				.Select(b => b.Id)
				.FirstOrDefaultAsync();
			return $"BILL-{datePart}-{(last + 1):0000}";
		}

		// The guest's Company Name (captured on the booking's guest record) and GST No
		// (looked up from the Dealer master by the Employee/Dealer Code entered for the
		// stay, when it resolves to a registered dealer) shown on the Tax Invoice. Neither
		// is persisted on GuestHouseBill, so it is resolved live from the still-intact
		// booking/guest/dealer data every time the bill is viewed or printed.
		private async Task<(string? CompanyName, string? GstNumber)> ResolveGuestCompanyAndGstAsync(int guestHouseBookingId) =>
			await GuestHouseBillPdfHelper.ResolveGuestCompanyAndGstAsync(_db, guestHouseBookingId);

		// Single source of truth shared with the customer-side Download Invoice so the PDF
		// is byte-identical to the Front Office / Admin download (see GuestHouseBillPdfHelper).
		private static BillViewDto ToBillViewDto(GuestHouseBill bill, string? companyName = null, string? gstNumber = null) =>
			GuestHouseBillPdfHelper.ToBillViewDto(bill, companyName, gstNumber);

		private async Task<IActionResult> BuildBillDraftAsync(GuestHouseBooking booking)
		{
			if (booking.BookingStatus != GuestHouseBookingStatus.Completed)
				return BadRequest(new { Success = false, Message = "A bill can only be generated for a booking that has been checked out (Completed)." });

			var guest = booking.Guests.FirstOrDefault();

			// A genuine "advance" is money actually paid before/at check-in (e.g. an online
			// payment taken at booking time). It must NOT include payment collected at the
			// Front Office check-out gate (RecordPayment) - that is the final settlement for
			// the stay, not a pre-stay advance, and belongs in "Payment received" at billing time.
			var advancePayment = booking.Payments
				.Where(p => p.PaymentMethod != GuestHousePaymentMethod.PayAfterStay
					&& p.PaymentStatus == GuestHousePaymentStatus.Paid
					&& p.PaymentDate.HasValue
					&& booking.ActualCheckInAt.HasValue
					&& p.PaymentDate.Value <= booking.ActualCheckInAt.Value)
				.Sum(p => p.Amount);

			var existing = await _db.GuestHouseBills
				.AsNoTracking()
				.FirstOrDefaultAsync(b => b.GuestHouseBookingId == booking.Id);

			var nights = booking.NumberOfNights ?? Math.Max(1, (int)((booking.CheckOutDate?.Date ?? booking.CheckInDate?.Date ?? DateTime.Today) - (booking.CheckInDate?.Date ?? DateTime.Today)).TotalDays);
			var numberOfRooms = booking.NumberOfRooms ?? 1;
			var extraBeds = booking.ExtraCotQuantity ?? 0;

			// Carry the booking's existing GST (as already calculated and persisted at
			// booking time) into the bill line-item CGST/SGST rates, so the pre-filled
			// Generate Bill form and the resulting Tax Invoice PDF show the same GST the
			// Booking Summary calculated. The effective rate is derived from the booking's
			// own persisted TaxAmount/SubTotal - never recomputed and never hard-coded. A
			// flat total rate (e.g. 5%) is split equally into CGST + SGST to match the
			// existing per-line CGST/SGST billing model.
			var subTotalForTax = booking.SubTotal ?? 0m;
			var totalGstPercent = subTotalForTax > 0m
				? Math.Round(((booking.TaxAmount ?? 0m) / subTotalForTax) * 100m, 2)
				: 0m;
			var cgstPercent = Math.Round(totalGstPercent / 2m, 2);
			var sgstPercent = Math.Round(totalGstPercent / 2m, 2);

			var roomLine = new BillLineItemDto
			{
				Description = $"{booking.GuestHouseRoom?.RoomType ?? "Room"} - Room Charge",
				Quantity = numberOfRooms * nights,
				Rate = booking.RoomPrice,
				CgstPercent = cgstPercent,
				SgstPercent = sgstPercent,
				IsEditable = false
			};

			var lines = new List<BillLineItemDto> { roomLine };

			if (extraBeds > 0 && (booking.ExtraCotPrice ?? 0) > 0)
			{
				lines.Add(new BillLineItemDto
				{
					Description = "Additional Beds / Extra Cot",
					Quantity = extraBeds * nights,
					Rate = booking.ExtraCotPrice ?? 0,
					CgstPercent = cgstPercent,
					SgstPercent = sgstPercent,
					IsEditable = false
				});
			}

			var draft = new BillDraftDto
			{
				BookingId = booking.Id,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestName = guest?.GuestName,
				CompanyName = guest?.CompanyName,
				GstinNumber = guest?.GstinNumber,
				Address = guest?.Address,
				Email = guest?.Email,
				PhoneNumber = guest?.PhoneNumber,
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomNumber = ResolveBillRoomNumber(booking),
				RoomType = booking.GuestHouseRoom?.RoomType ?? "Room",
				CheckInAt = booking.ActualCheckInAt ?? (booking.CheckInDate.HasValue ? booking.CheckInDate.Value.Date.Add(booking.CheckInTime ?? TimeSpan.Zero) : (DateTime?)null),
				CheckOutAt = booking.ActualCheckOutAt ?? (booking.CheckOutDate.HasValue ? booking.CheckOutDate.Value.Date.Add(booking.CheckOutTime ?? TimeSpan.Zero) : (DateTime?)null),
				NumberOfNights = nights,
				NumberOfRooms = numberOfRooms,
				NumberOfPersons = booking.NumberOfPersons ?? guest?.NumberOfPersons,
				ExtraBeds = extraBeds,
				LineItems = lines,
				AdvancePayment = advancePayment,
				ExistingBillId = existing?.Id,
				ExistingBillNumber = existing?.BillNumber,
				IsComplimentary = booking.Payments.Any(p => p.PaymentMethod == GuestHousePaymentMethod.Complimentary)
			};

			return Ok(draft);
		}

		// ---- Helpers ----

		private async Task<GuestHouseBooking?> LoadBookingAsync(int bookingId)		{
			var query = _db.GuestHouseBookings
				.Include(b => b.GuestHouse)
				.Include(b => b.GuestHouseRoom)
				.Include(b => b.Guests)
				.Include(b => b.Payments)
				.Include(b => b.RoomAllocations)
				.Include(b => b.Documents)
				.AsQueryable();

			// Receptionist state scope: a booking of another state's guest house is "not found".
			var scope = await GetGuestHouseScopeAsync();
			if (scope != null)
				query = query.Where(b => scope.Contains(b.GuestHouseId));

			return await query.FirstOrDefaultAsync(b => b.Id == bookingId);
		}

		// Guest house ids the signed-in user may see/operate on, or null when unrestricted.
		// Resolved once per request - see GuestHouseReceptionistScope.
		private List<int>? _guestHouseScope;
		private bool _guestHouseScopeResolved;

		private async Task<List<int>?> GetGuestHouseScopeAsync()
		{
			if (!_guestHouseScopeResolved)
			{
				_guestHouseScope = await GuestHouseReceptionistScope.GetAllowedGuestHouseIdsAsync(_db, User);
				_guestHouseScopeResolved = true;
			}
			return _guestHouseScope;
		}

		// ---- Physical room helpers ----
		//
		// GuestHouseRoom.RoomNumber is the FIRST physical room number of the type and
		// GuestHouseRoom.AvailableQuantity is how many such rooms exist. Physical rooms are
		// derived, never stored or hardcoded: a numeric base (e.g. "111" qty 4) yields 111,
		// 112, 113, 114; a non numeric base (e.g. "D1") yields D1-1, D1-2, ...; a missing
		// base yields "Room 1", "Room 2", ...

		private static string DeriveRoomNumber(string? baseNumber, int offset)
		{
			var baseText = (baseNumber ?? string.Empty).Trim();
			if (int.TryParse(baseText, out var baseValue))
			{
				var raw = baseText.TrimStart('-');
				var width = raw.Length > 1 ? raw.Length : 0;
				var number = (baseValue + offset).ToString(CultureInfo.InvariantCulture);
				return width > number.Length ? number.PadLeft(width, '0') : number;
			}
			return string.IsNullOrWhiteSpace(baseText) ? $"Room {offset + 1}" : $"{baseText}-{offset + 1}";
		}

		private static List<string> EnumeratePhysicalRoomNumbers(GuestHouseRoom room, int quantity)
		{
			var count = Math.Max(1, quantity);
			var numbers = new List<string>(count);
			for (var i = 0; i < count; i++)
				numbers.Add(DeriveRoomNumber(room.RoomNumber, i));
			return numbers;
		}

		private static bool ContainsNumber(IReadOnlyCollection<string> numbers, string roomNumber)
		{
			return numbers.Contains(roomNumber, StringComparer.OrdinalIgnoreCase);
		}

		// The exact physical room number(s) actually allocated to a checked-in booking.
		// Returns null when the stay is not yet allocated (the authoritative room number
		// only exists after the Front Office assigns it at check-in).
		private static string? AllocatedRoomNumber(GuestHouseBooking booking)
		{
			var allocated = booking.RoomAllocations?
				.Select(a => a.RoomNumber)
				.Where(n => !string.IsNullOrWhiteSpace(n))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(n => n)
				.ToList();
			return allocated == null || allocated.Count == 0 ? null : string.Join(", ", allocated);
		}

		// The Room No to print on a bill/invoice: prefer the physical room(s) actually
		// assigned by the Front Office (snapshotted onto the booking at Check-Out, since
		// GuestHouseRoomAllocation rows are released at that point). Falls back to the
		// room type's base RoomNumber only for legacy bookings checked out before this
		// snapshot existed.
		private static string? ResolveBillRoomNumber(GuestHouseBooking booking) =>
			!string.IsNullOrWhiteSpace(booking.AllocatedRoomNumber)
				? booking.AllocatedRoomNumber
				: booking.GuestHouseRoom?.RoomNumber;

		// Computes the exact physical room numbers currently free for a room type over
		// [checkInDate, checkOutDate) - the authoritative set shown at check-in.
		//
		// A room is NOT free when:
		//   1. an active overlapping booking (Draft/PendingPayment/Confirmed/CheckedIn)
		//      has an EXACT allocation for that number (GuestHouseRoomAllocation), or
		//   2. it falls inside the deterministic lowest-number block still owed by
		//      overlapping bookings that have (for now) no exact allocation yet
		//      (Σ NumberOfRooms − allocated rows). This matches the "reserved by booker"
		//      assumption used by the availability engine, so two check-ins can never pick
		//      the same silently-unassigned room.
		private async Task<FreeRoomsResult> GetFreePhysicalRoomsAsync(
			int roomId, DateTime checkInDate, DateTime checkOutDate, int excludeBookingId = 0)
		{
			var room = await _db.GuestHouseRooms
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);
			if (room == null || room.AvailableQuantity <= 0)
				return new FreeRoomsResult(new List<string>(), new List<string>());

			var numbers = EnumeratePhysicalRoomNumbers(room, room.AvailableQuantity);

			var pendingPaymentHoldCutoff = PendingPaymentHoldCutoff;
			var overlapQuery = _db.GuestHouseBookings
				.AsNoTracking()
				.Where(b => b.GuestHouseRoomId == roomId
					&& b.BookingStatus != GuestHouseBookingStatus.Cancelled
					&& b.BookingStatus != GuestHouseBookingStatus.Completed
					// Same PendingPayment hold-window rule as the customer-facing availability
					// check (GuestHouseBookingController.GetCommittedRoomsByRoomAsync) - an
					// expired, unpaid hold stops reserving a physical room number here too.
					&& (b.BookingStatus != GuestHouseBookingStatus.PendingPayment || b.CreatedAt >= pendingPaymentHoldCutoff)
					&& b.CheckInDate.HasValue && b.CheckOutDate.HasValue
					&& b.CheckInDate.Value.Date < checkOutDate.Date
					&& b.CheckOutDate.Value.Date > checkInDate.Date);
			if (excludeBookingId > 0)
				overlapQuery = overlapQuery.Where(b => b.Id != excludeBookingId);

			var bookings = await overlapQuery
				.Select(b => new { b.Id, b.NumberOfRooms })
				.ToListAsync();
			var bookingIds = bookings.Select(b => b.Id).ToList();

			// Exact allocations of those overlapping bookings for this room type.
			var allocationRows = bookingIds.Count > 0
				? await _db.GuestHouseRoomAllocations
					.AsNoTracking()
					.Where(a => bookingIds.Contains(a.GuestHouseBookingId) && a.GuestHouseRoomId == roomId)
					.ToListAsync()
				: new List<GuestHouseRoomAllocation>();

			var allocatedCountByBooking = allocationRows
				.Where(a => ContainsNumber(numbers, a.RoomNumber))
				.GroupBy(a => a.GuestHouseBookingId)
				.ToDictionary(g => g.Key, g => g.Count());

			// Rooms still owed (not yet exactly allocated) by the overlapping bookings.
			var unallocatedCommitted = bookings.Sum(b =>
				Math.Max(0, (b.NumberOfRooms ?? 1) - allocatedCountByBooking.GetValueOrDefault(b.Id)));

			var occupiedExact = allocationRows
				.Where(a => ContainsNumber(numbers, a.RoomNumber))
				.Select(a => a.RoomNumber)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			var remaining = numbers
				.Where(n => !ContainsNumber(occupiedExact, n))
				.ToList();
			var countBlocked = Math.Min(Math.Max(0, unallocatedCommitted), remaining.Count);

			var occupied = occupiedExact
				.Union(remaining.Take(countBlocked), StringComparer.OrdinalIgnoreCase)
				.ToList();
			var free = remaining.Skip(countBlocked).ToList();

			return new FreeRoomsResult(free, occupied);
		}

		private static CheckInEligibilityResult CheckInEligibility(GuestHouseBooking booking)
		{
			if (booking.BookingStatus == GuestHouseBookingStatus.CheckedIn)
				return new CheckInEligibilityResult(false, "This booking has already been checked in.");

			if (booking.BookingStatus == GuestHouseBookingStatus.Cancelled)
				return new CheckInEligibilityResult(false, "This booking has been cancelled and cannot be checked in.");

			if (booking.BookingStatus == GuestHouseBookingStatus.Completed)
				return new CheckInEligibilityResult(false, "This booking is already completed.");

			if (booking.BookingStatus != GuestHouseBookingStatus.Confirmed)
				return new CheckInEligibilityResult(false, "Only a valid, confirmed booking can be checked in.");

			// A booking is valid for check-in while the stay window is active.
			var now = DateTime.Now;
			var checkInDate = booking.CheckInDate?.Date;
			var checkOutDate = booking.CheckOutDate?.Date;

			if (checkInDate.HasValue && now.Date < checkInDate.Value)
				return new CheckInEligibilityResult(false, "This booking's check-in date has not arrived yet.");

			if (checkOutDate.HasValue && now.Date > checkOutDate.Value)
				return new CheckInEligibilityResult(false, "This booking's stay period has passed and it can no longer be checked in.");

			return new CheckInEligibilityResult(true, "This booking is eligible for check-in.");
		}

		private static string StatusText(FrontOfficeRoomStatus status) => status switch
		{
			FrontOfficeRoomStatus.Reserved => "Reserved / Booked",
			FrontOfficeRoomStatus.CheckedIn => "Checked-In",
			_ => "Available"
		};

		private sealed record CheckInEligibilityResult(bool CanCheckIn, string Message);

		private sealed record FreeRoomsResult(List<string> Free, List<string> Occupied);
	}

	public enum FrontOfficeRoomStatus
	{
		Available = 0,
		Reserved = 1,
		CheckedIn = 2
	}

	public class FrontOfficeHouseDto
	{
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public List<FrontOfficeRoomDto> Rooms { get; set; } = new List<FrontOfficeRoomDto>();
	}

	public class FrontOfficeRoomDto
	{
		public int RoomId { get; set; }
		public string? RoomNumber { get; set; }
		public string RoomType { get; set; } = "";
		public int GuestHouseId { get; set; }
		public string GuestHouseName { get; set; } = "";
		public int? Capacity { get; set; }
		public decimal PricePerNight { get; set; }
		public int AvailableQuantity { get; set; }

		public FrontOfficeRoomStatus RoomStatus { get; set; }
		public string StatusText { get; set; } = "";

		public int? CurrentBookingId { get; set; }
		public string? BookingReference { get; set; }
		public string? GuestName { get; set; }
		public DateTime? CheckInDate { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public GuestHouseBookingStatus? BookingStatus { get; set; }
		public GuestHousePaymentStatus? PaymentStatus { get; set; }
		public GuestHousePaymentMethod? PaymentMethod { get; set; }
	}

	public class FrontOfficeBookingDto
	{
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public string GuestHouseName { get; set; } = "";
		public string RoomType { get; set; } = "";
		public string? RoomNumber { get; set; }
		public string? GuestName { get; set; }
		public int? NumberOfPersons { get; set; }
		public int ExtraBeds { get; set; }
		public int? Capacity { get; set; }
		public DateTime? CheckInDate { get; set; }
		public TimeSpan? CheckInTime { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public TimeSpan? CheckOutTime { get; set; }
		public GuestHouseBookingStatus BookingStatus { get; set; }
		public GuestHousePaymentStatus PaymentStatus { get; set; }
		public GuestHousePaymentMethod? PaymentMethod { get; set; }
		public bool CanCheckIn { get; set; }
		public string CheckInMessage { get; set; } = "";
		public bool CanCheckOut { get; set; }
		public decimal? TotalAmount { get; set; }
		public DateTime? ActualCheckInAt { get; set; }

		// Exact physical room numbers for the Front Office to choose at check-in.
		public List<string> AvailableRoomNumbers { get; set; } = new List<string>();
		public List<string> AllocatedRoomNumbers { get; set; } = new List<string>();
		public int RequiredRoomNumbers { get; set; }

		// ID Proof document(s) uploaded by the guest, for Front Office reference only.
		public List<FrontOfficeBookingDocumentDto> Documents { get; set; } = new List<FrontOfficeBookingDocumentDto>();
	}

	public class FrontOfficeBookingDocumentDto
	{
		public int DocumentId { get; set; }
		public string? DocumentType { get; set; }
		public string? FileName { get; set; }
		public string? ContentType { get; set; }
		public long? FileSize { get; set; }
	}

	public class CheckOutSummaryDto
	{
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public string GuestHouseName { get; set; } = "";
		public string? GuestName { get; set; }
		public string? RoomNumber { get; set; }
		public string RoomType { get; set; } = "";
		public DateTime? CheckInDate { get; set; }
		public DateTime? ActualCheckInAt { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public int? NumberOfPersons { get; set; }
		public int ExtraBeds { get; set; }
		public decimal TotalAmount { get; set; }
		public GuestHousePaymentStatus PaymentStatus { get; set; }
		public GuestHousePaymentMethod? PaymentMethod { get; set; }
		public bool PaymentReceived { get; set; }
	}

	public class CheckInRequest
	{
		public int BookingId { get; set; }
		public int? NumberOfPersons { get; set; }
		public int? ExtraBeds { get; set; }
		public List<string>? SelectedRoomNumbers { get; set; }    // Exact physical room numbers assigned at check-in (one per booked room)
	}

	public class RecordPaymentRequest
	{
		public int BookingId { get; set; }
		public string? TransactionId { get; set; }
	}

	public class CheckOutRequest
	{
		public int BookingId { get; set; }
	}

	// -------------------- Billing DTOs --------------------

	public class BillLineItemDto
	{
		public string? Description { get; set; }
		public decimal Quantity { get; set; }
		public decimal Rate { get; set; }
		public decimal CgstPercent { get; set; }
		public decimal SgstPercent { get; set; }
		public bool IsEditable { get; set; } = true;
	}

	public class BillDraftDto
	{
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public string? GuestName { get; set; }
		public string? CompanyName { get; set; }
		public string? GstinNumber { get; set; }
		public string? Address { get; set; }
		public string? Email { get; set; }
		public string? PhoneNumber { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string? RoomNumber { get; set; }
		public string RoomType { get; set; } = "";
		public DateTime? CheckInAt { get; set; }
		public DateTime? CheckOutAt { get; set; }
		public int NumberOfNights { get; set; }
		public int? NumberOfRooms { get; set; }
		public int? NumberOfPersons { get; set; }
		public int? ExtraBeds { get; set; }
		public List<BillLineItemDto> LineItems { get; set; } = new List<BillLineItemDto>();
		public decimal? AdvancePayment { get; set; }
		public int? ExistingBillId { get; set; }
		public string? ExistingBillNumber { get; set; }

		// True for a free (₹0) employee booking (payment method Complimentary). Lets the
		// Generate Bill page keep the ₹0 Room line so the bill still has a line item.
		public bool IsComplimentary { get; set; }
	}

	public class BillListItemDto
	{
		public int BillId { get; set; }
		public string BillNumber { get; set; } = "";
		public string BookingReference { get; set; } = "";
		public int BookingId { get; set; }
		public string? GuestName { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string? RoomNumber { get; set; }
		public DateTime BillDate { get; set; }
		public DateTime? CheckInDate { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public decimal TotalAmount { get; set; }
		public decimal BalanceAmount { get; set; }
		public int PaymentStatus { get; set; }
		public int PaymentMethod { get; set; }
	}

	public class GenerateBillRequest
	{
		public int BookingId { get; set; }
		public List<BillLineItemDto> LineItems { get; set; } = new List<BillLineItemDto>();
		public decimal? Discount { get; set; }
		public decimal? AdvancePayment { get; set; }
		public decimal? RoundOff { get; set; }
		public bool PaymentReceived { get; set; }
		public int PaymentMethod { get; set; }
		public string? Remarks { get; set; }
	}

	public class BillViewLineItemDto
	{
		public string? Description { get; set; }
		public decimal Quantity { get; set; }
		public decimal Rate { get; set; }
		public decimal Amount { get; set; }
		public decimal CgstPercent { get; set; }
		public decimal SgstPercent { get; set; }
		public decimal CgstAmount { get; set; }
		public decimal SgstAmount { get; set; }
		public decimal LineTotal { get; set; }
	}

	public class BillViewDto
	{
		public int BillId { get; set; }
		public string BillNumber { get; set; } = "";
		public string BookingReference { get; set; } = "";
		public int BookingId { get; set; }
		public DateTime BillDate { get; set; }
		public string? GuestName { get; set; }
		public string? CompanyName { get; set; }
		public string? GstNumber { get; set; }
		public string? Address { get; set; }
		public string? Email { get; set; }
		public string? PhoneNumber { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string? RoomNumber { get; set; }
		public string RoomType { get; set; } = "";
		public DateTime? CheckInAt { get; set; }
		public DateTime? CheckOutAt { get; set; }
		public int? NumberOfNights { get; set; }
		public int? NumberOfRooms { get; set; }
		public int? NumberOfPersons { get; set; }
		public int? ExtraBeds { get; set; }
		public List<BillViewLineItemDto> LineItems { get; set; } = new List<BillViewLineItemDto>();
		public decimal Subtotal { get; set; }
		public decimal CgstAmount { get; set; }
		public decimal SgstAmount { get; set; }
		public decimal TotalAmount { get; set; }
		public decimal Discount { get; set; }
		public decimal AdvancePayment { get; set; }
		public decimal RoundOff { get; set; }
		public decimal BalanceAmount { get; set; }
		public string? Remarks { get; set; }
		public int PaymentStatus { get; set; }
		public int PaymentMethod { get; set; }
	}

	public class CompletedBookingForBillingDto
	{
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public string? GuestName { get; set; }
		public string GuestHouseName { get; set; } = "";
		public string? RoomNumber { get; set; }
		public string RoomType { get; set; } = "";
		public DateTime? CheckInDate { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public decimal Amount { get; set; }
		public GuestHousePaymentStatus PaymentStatus { get; set; }
		public bool HasBill { get; set; }
	}

	public class RejectCancellationRequest
	{
		public string? Reason { get; set; }
	}

	public class CancellationRequestListItemDto
	{
		public int CancellationId { get; set; }
		public string? CancellationReference { get; set; }
		public int BookingId { get; set; }
		public string BookingReference { get; set; } = "";
		public string GuestHouseName { get; set; } = "";
		public string RoomType { get; set; } = "";
		public string? GuestName { get; set; }
		public string? EmployeeOrDealerCode { get; set; }
		public string? PhoneNumber { get; set; }
		public DateTime? CheckInDate { get; set; }
		public DateTime? CheckOutDate { get; set; }
		public string BookingStatus { get; set; } = "";
		public string PaymentStatus { get; set; } = "";
		public string PaymentMethod { get; set; } = "";
		public decimal TotalAmount { get; set; }
		public decimal CancellationCharge { get; set; }
		public decimal TaxAdjustment { get; set; }
		public decimal RefundAmount { get; set; }
		public string? CancellationReason { get; set; }
		public string? RequestedBy { get; set; }
		public DateTime RequestedAt { get; set; }
		public string ApprovalStatus { get; set; } = "";
		public string RefundStatus { get; set; } = "";
		public string? AdminDecisionBy { get; set; }
		public DateTime? AdminDecisionAt { get; set; }
		public string? RejectionReason { get; set; }
		public string? Remarks { get; set; }
		public string? RefundReference { get; set; }
		public DateTime? EstimatedRefundDate { get; set; } // 2-working-day target (client requirement)
	}

	/// <summary>
	/// Read/write gate for the Front Office cluster (Front Office / Generate Bill / Cancellation Requests).
	///
	/// Admin, CorporateAdmin, and SuperAdmin keep every endpoint unrestricted.
	///
	/// Everything else must be an authenticated request whose route maps to a PagePermission that this
	/// user's Designation (Designation.RoleAccess) actually grants.
	///
	/// Reads: Allowed for designations granting GHAdmin, SDWAAdmin, or the endpoint's specific PagePermission.
	/// Writes (check-in, payment, check-out, generate-bill): Allowed for designations granting GHAdmin, SDWAAdmin,
	/// or a Receptionist designation granting the endpoint's specific PagePermission (scoped to their own state).
	/// Cancellation approve/reject stay Admin / CorporateAdmin / SuperAdmin only.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	internal sealed class GuestHouseFrontOfficeAccessAttribute : Attribute, IAsyncAuthorizationFilter
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
			// SuperAdmin is preserved alongside Admin/CorporateAdmin.
			if (user.IsInRole(nameof(AppRole.Admin))
				|| user.IsInRole(nameof(AppRole.CorporateAdmin))
				|| user.IsInRole(nameof(AppRole.SuperAdmin)))
				return;

			// Reads are opened by the Designation's page permission. Actions stay Admin /
			// CorporateAdmin / SuperAdmin only, EXCEPT the Front Office (check-in / payment / check-out) and
			// Generate Bill actions, which users with GHAdmin or SDWAAdmin page permission, or a Receptionist
			// whose Designation grants that page, may perform.
			var isGet = string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
			var path = context.HttpContext.Request.Path.Value ?? string.Empty;
			var requiredPermission = isGet ? RequiredReadPermission(path) : RequiredActionPermission(path);
			if (requiredPermission is null)
			{
				context.Result = Forbidden();
				return;
			}

			// Designation rule: does THIS user's Designation.RoleAccess grant that PagePermission?
			var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
			var designation = await GuestHouseReceptionistScope.GetDesignationAsync(db, user);
			if (designation != null)
			{
				var hasAdminPermission = GuestHouseReceptionistScope.HasGuestHouseAdminPermission(designation);

				var hasEndpointPermission = RoleAccessPermissions.HasPage(designation.RoleAccess, requiredPermission.Value);

				var isAllowed = isGet
					? (hasAdminPermission || hasEndpointPermission)
					: (hasAdminPermission || (GuestHouseReceptionistScope.IsReceptionist(designation.Name) && hasEndpointPermission));

				if (isAllowed)
					return;
			}

			context.Result = Forbidden();
		}

		// Action route -> the PagePermission needed to perform it. Every other
		// action (cancellation approve / reject) maps to null and stays Admin / CorporateAdmin / SuperAdmin only.
		private static PagePermission? RequiredActionPermission(string path) =>
			path.EndsWith("/checkin", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith("/pay", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith("/checkout", StringComparison.OrdinalIgnoreCase) ? PagePermission.FrontOffice
			: path.EndsWith("/generate-bill", StringComparison.OrdinalIgnoreCase) ? PagePermission.GenerateBill
			: null;

		// Route -> the PagePermission of the SDWA page that reads it. Unmapped GETs stay closed.
		private static PagePermission? RequiredReadPermission(string path) =>
			path.Contains("/cancellations", StringComparison.OrdinalIgnoreCase) ? PagePermission.GuestHouseCancellations
			: path.Contains("/bill", StringComparison.OrdinalIgnoreCase) ? PagePermission.GenerateBill
			: path.Contains("/rooms", StringComparison.OrdinalIgnoreCase) ? PagePermission.FrontOffice
			: path.Contains("/bookings/", StringComparison.OrdinalIgnoreCase) ? PagePermission.FrontOffice
			: path.Contains("/checkout/", StringComparison.OrdinalIgnoreCase) ? PagePermission.FrontOffice
			: null;

		private static ObjectResult Forbidden() =>
			new(new { Success = false, Message = "You do not have permission to view this data." })
			{
				StatusCode = StatusCodes.Status403Forbidden
			};
	}

	/// <summary>
	/// State scope for Receptionists on the Front Office cluster (Front Office / Generate Bill /
	/// Bill List / Cancellations / Report Dashboard reads).
	///
	/// A user whose Designation is "Receptionist" only sees and operates on the guest house(s)
	/// whose GuestHouse.StateId equals their own state - the existing spic:state_id claim
	/// (Employeelogin.StateId, written at login). The client never supplies the state.
	/// Everyone else (Admin, CorporateAdmin, GM and any other designation) is unrestricted,
	/// exactly as before. Admin / SuperAdmin are never restricted, whatever their designation. A
	/// GHAdmin / SDWAAdmin grant does not lift a Receptionist's state restriction.
	/// A Receptionist with no state (or whose state has no guest house) sees nothing.
	/// </summary>
	internal static class GuestHouseReceptionistScope
	{
		public const string ReceptionistDesignation = "Receptionist";

		public sealed record UserDesignation(string Name, string? RoleAccess);

		// The signed-in user's active Designation (name + RoleAccess), or null when none.
		public static async Task<UserDesignation?> GetDesignationAsync(AppDbContext db, ClaimsPrincipal user)
		{
			var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrWhiteSpace(userId))
				return null;

			var designationId = await db.Users
				.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => u.DesignationId)
				.FirstOrDefaultAsync();
			if (designationId is not > 0)
				return null;

			return await db.Designations
				.AsNoTracking()
				.Where(d => d.Id == designationId && d.IsActive)
				.Select(d => new UserDesignation(d.Name, d.RoleAccess))
				.FirstOrDefaultAsync();
		}

		public static bool IsReceptionist(string? designationName) =>
			string.Equals(designationName?.Trim(), ReceptionistDesignation, StringComparison.OrdinalIgnoreCase);

		// Guest House administrator: the Designation's RoleAccess grants the GHAdmin or SDWAAdmin
		// page permission (RoleAccessPermissions.HasPage - never the Designation name).
		public static bool HasGuestHouseAdminPermission(UserDesignation? designation) =>
			designation != null
			&& (RoleAccessPermissions.HasPage(designation.RoleAccess, PagePermission.GHAdmin)
				|| RoleAccessPermissions.HasPage(designation.RoleAccess, PagePermission.SDWAAdmin));

		// Action-level: does the Designation grant this action ("View" / "Entry" / "Update" / "Delete",
		// the Designation grid's actions) on the page? Same PageAuthorization.CanPerformAction the client's
		// LoginState.Can uses; a bare legacy page token grants every action. Role bypasses are handled by
		// the callers, so no role is passed here.
		public static bool HasAction(UserDesignation? designation, PagePermission page, string action) =>
			designation != null
			&& PageAuthorization.GetEffectivePagePermissions((AppRole?)null, designation.RoleAccess)
				.CanPerformAction(null, page, action);

		// The action on GHAdmin or SDWAAdmin.
		public static bool HasGuestHouseAdminAction(UserDesignation? designation, string action) =>
			HasAction(designation, PagePermission.GHAdmin, action)
			|| HasAction(designation, PagePermission.SDWAAdmin, action);

		// Guest house ids the user may see/operate on, or null when unrestricted.
		public static async Task<List<int>?> GetAllowedGuestHouseIdsAsync(AppDbContext db, ClaimsPrincipal user)
		{
			if (user.IsInRole(nameof(AppRole.Admin)) || user.IsInRole(nameof(AppRole.SuperAdmin)))
				return null;

			var designation = await GetDesignationAsync(db, user);
			if (designation == null)
				return null;

			// Every non-Receptionist designation - including any granting GHAdmin / SDWAAdmin - sees
			// all states. A Receptionist stays scoped to their own state even when their designation
			// also grants GHAdmin / SDWAAdmin: a page grant never lifts the state restriction.
			if (!IsReceptionist(designation.Name))
				return null;

			var stateId = int.TryParse(user.FindFirst("spic:state_id")?.Value, out var parsed) ? parsed : 0;
			if (stateId <= 0)
				return new List<int>();

			return await db.GuestHouses
				.AsNoTracking()
				.Where(h => h.StateId == stateId)
				.Select(h => h.Id)
				.ToListAsync();
		}
	}
}
