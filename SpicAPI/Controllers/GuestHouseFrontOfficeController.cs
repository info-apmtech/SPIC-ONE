using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SpicAPI.Services;
using SPIC.Core.Entities;

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
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "Admin,CorporateAdmin")]
	public class GuestHouseFrontOfficeController : ControllerBase
	{
		private readonly AppDbContext _db;

		public GuestHouseFrontOfficeController(AppDbContext db)
		{
			_db = db;
		}

		// GET /api/GuestHouseFrontOffice/rooms
		// Returns every active guest house and its rooms with a dynamically computed
		// current status plus the occupying booking's details (if any).
		[HttpGet("rooms")]
		public async Task<IActionResult> GetRooms()
		{
			var houses = await _db.GuestHouses
				.AsNoTracking()
				.Where(h => h.IsActive)
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
				.Include(r => r.GuestHouse)
				.Include(r => r.Bookings)
					.ThenInclude(b => b.Guests)
				.OrderBy(r => r.GuestHouseId)
				.ThenBy(r => r.RoomNumber)
				.ToListAsync();

			var housesById = houses.ToDictionary(h => h.GuestHouseId);
			foreach (var house in houses)
			{
				house.Rooms ??= new List<FrontOfficeRoomDto>();
			}

			var now = DateTime.Now;

			foreach (var room in rooms)
			{
				if (room.GuestHouse == null || !housesById.TryGetValue(room.GuestHouse!.Id, out var house))
					continue;

				// Find the occupying booking.
				// 1. A booking that has actually been checked in takes priority.
				// 2. Otherwise the nearest valid (Confirmed) booking.
				var activeBookings = room.Bookings
					.Where(b => b.BookingStatus == GuestHouseBookingStatus.Confirmed
						|| b.BookingStatus == GuestHouseBookingStatus.CheckedIn)
					.OrderBy(b => b.CheckInDate)
					.ToList();

				GuestHouseBooking? occupant = activeBookings
					.FirstOrDefault(b => b.BookingStatus == GuestHouseBookingStatus.CheckedIn)
					?? activeBookings.FirstOrDefault();

				var roomDto = new FrontOfficeRoomDto
				{
					RoomId = room.Id,
					RoomNumber = room.RoomNumber,
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

			return Ok(new FrontOfficeBookingDto
			{
				BookingId = booking.Id,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomType = booking.GuestHouseRoom?.RoomType ?? "Room",
				RoomNumber = booking.GuestHouseRoom?.RoomNumber,
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
				ActualCheckInAt = booking.ActualCheckInAt
			});
		}

		// POST /api/GuestHouseFrontOffice/checkin
		// Marks a valid, confirmed booking as Checked-In. Also allows the Front Office
		// to update the number of persons and extra beds on the existing booking record.
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

			await _db.SaveChangesAsync();

			return Ok(new
			{
				Success = true,
				BookingId = booking.Id,
				BookingReference = booking.BookingReference,
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
				RoomNumber = booking.GuestHouseRoom?.RoomNumber,
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

			var booking = await _db.GuestHouseBookings
				.Include(b => b.GuestHouse)
				.Include(b => b.GuestHouseRoom)
				.Include(b => b.Guests)
				.Include(b => b.Payments)
				.FirstOrDefaultAsync(b => b.BookingReference == reference.Trim());

			if (booking == null)
				return NotFound(new { Success = false, Message = "Booking not found for the given reference." });

			return await BuildBillDraftAsync(booking);
		}

		// GET /api/GuestHouseFrontOffice/billing/completed?date=yyyy-MM-dd
		// Returns the bookings that are eligible for bill generation for a given date
		// (the date is matched against the actual check-out / completion date). When no
		// date is supplied, defaults to today. Only COMPLETED bookings whose payment has
		// been received are returned, so cancelled / pending / active / upcoming bookings
		// never appear. Each row also reports whether a bill already exists for the booking.
		[HttpGet("billing/completed")]
		public async Task<IActionResult> GetCompletedBookingsForBilling([FromQuery] DateTime? date)
		{
			var target = date?.Date ?? DateTime.Today;
			var from = target;
			var to = target.AddDays(1);

			var bookings = await _db.GuestHouseBookings
				.AsNoTracking()
				.Where(b =>
					b.BookingStatus == GuestHouseBookingStatus.Completed
					&& (b.PaymentStatus == GuestHousePaymentStatus.Paid
						|| b.Payments.Any(p => p.PaymentStatus == GuestHousePaymentStatus.Paid))
					&& b.ActualCheckOutAt != null
					&& b.ActualCheckOutAt.Value >= from
					&& b.ActualCheckOutAt.Value < to)
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
					RoomNumber = b.GuestHouseRoom?.RoomNumber,
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
				Date = target,
				Count = list.Count,
				Bookings = list
			});
		}

		// GET /api/GuestHouseFrontOffice/bills
		// Returns all generated bills (Bill List page), most recent first.
		[HttpGet("bills")]
		public async Task<IActionResult> GetBills()
		{
			var bills = await _db.GuestHouseBills
				.AsNoTracking()
				.OrderByDescending(b => b.BillDate)
				.ToListAsync();

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
			var bill = await _db.GuestHouseBills
				.AsNoTracking()
				.Include(b => b.LineItems)
				.FirstOrDefaultAsync(b => b.Id == billId);
			if (bill == null)
				return NotFound(new { Success = false, Message = "Bill not found." });

			return Ok(ToBillViewDto(bill));
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

			if (request.LineItems.Any(i => i.Quantity < 0 || i.Rate < 0))
				return BadRequest(new { Success = false, Message = "Quantities and rates must not be negative." });

			// Build line items and compute taxes dynamically per line.
			var lineItems = new List<GuestHouseBillLineItem>();
			decimal subtotal = 0m;
			decimal cgstTotal = 0m;
			decimal sgstTotal = 0m;

			foreach (var li in request.LineItems)
			{
				var amount = Math.Round(li.Quantity * li.Rate, 2, MidpointRounding.AwayFromZero);
				var cgst = Math.Round(amount * (li.CgstPercent / 100m), 2, MidpointRounding.AwayFromZero);
				var sgst = Math.Round(amount * (li.SgstPercent / 100m), 2, MidpointRounding.AwayFromZero);

				subtotal += amount;
				cgstTotal += cgst;
				sgstTotal += sgst;

				lineItems.Add(new GuestHouseBillLineItem
				{
					Description = li.Description,
					Quantity = li.Quantity,
					Rate = li.Rate,
					Amount = amount,
					CgstPercent = li.CgstPercent,
					SgstPercent = li.SgstPercent,
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
				Address = guest?.Address,
				Email = guest?.Email,
				PhoneNumber = guest?.PhoneNumber,
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomNumber = booking.GuestHouseRoom?.RoomNumber,
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
			var bill = await _db.GuestHouseBills
				.AsNoTracking()
				.Include(b => b.LineItems)
				.FirstOrDefaultAsync(b => b.Id == billId);
			if (bill == null)
				return NotFound(new { Success = false, Message = "Bill not found." });

			try
			{
				var bytes = GuestHouseInvoicePdfBuilder.Build(ToBillViewDto(bill));
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

		private static BillViewDto ToBillViewDto(GuestHouseBill bill)
		{
			var lines = bill.LineItems
				.OrderBy(i => i.Id)
				.Select(i => new BillViewLineItemDto
				{
					Description = i.Description,
					Quantity = i.Quantity,
					Rate = i.Rate,
					Amount = i.Amount,
					CgstPercent = i.CgstPercent,
					SgstPercent = i.SgstPercent,
					CgstAmount = i.CgstAmount,
					SgstAmount = i.SgstAmount,
					LineTotal = i.LineTotal
				}).ToList();

			return new BillViewDto
			{
				BillId = bill.Id,
				BillNumber = bill.BillNumber ?? $"BILL-{bill.Id:000000}",
				BookingReference = bill.BookingReference ?? $"BK{bill.GuestHouseBookingId}",
				BookingId = bill.GuestHouseBookingId,
				BillDate = bill.BillDate,
				GuestName = bill.GuestName,
				Address = bill.Address,
				Email = bill.Email,
				PhoneNumber = bill.PhoneNumber,
				GuestHouseName = bill.GuestHouseName,
				RoomNumber = bill.RoomNumber,
				RoomType = bill.RoomType,
				CheckInAt = bill.CheckInAt,
				CheckOutAt = bill.CheckOutAt,
				NumberOfNights = bill.NumberOfNights,
				NumberOfRooms = bill.NumberOfRooms,
				NumberOfPersons = bill.NumberOfPersons,
				ExtraBeds = bill.ExtraBeds,
				LineItems = lines,
				Subtotal = bill.Subtotal,
				CgstAmount = bill.CgstAmount,
				SgstAmount = bill.SgstAmount,
				TotalAmount = bill.TotalAmount,
				Discount = bill.Discount,
				AdvancePayment = bill.AdvancePayment,
				RoundOff = bill.RoundOff,
				BalanceAmount = bill.BalanceAmount,
				Remarks = bill.Remarks,
				PaymentStatus = (int)bill.PaymentStatus,
				PaymentMethod = (int)bill.PaymentMethod
			};
		}

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

			var roomLine = new BillLineItemDto
			{
				Description = $"{booking.GuestHouseRoom?.RoomType ?? "Room"} - Room Charge",
				Quantity = numberOfRooms * nights,
				Rate = booking.RoomPrice,
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
					IsEditable = false
				});
			}

			var draft = new BillDraftDto
			{
				BookingId = booking.Id,
				BookingReference = booking.BookingReference ?? $"BK{booking.Id}",
				GuestName = guest?.GuestName,
				Address = guest?.Address,
				Email = guest?.Email,
				PhoneNumber = guest?.PhoneNumber,
				GuestHouseName = booking.GuestHouse?.Name ?? "",
				RoomNumber = booking.GuestHouseRoom?.RoomNumber,
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
				ExistingBillNumber = existing?.BillNumber
			};

			return Ok(draft);
		}

		// ---- Helpers ----

		private async Task<GuestHouseBooking?> LoadBookingAsync(int bookingId)		{
			return await _db.GuestHouseBookings
				.Include(b => b.GuestHouse)
				.Include(b => b.GuestHouseRoom)
				.Include(b => b.Guests)
				.Include(b => b.Payments)
				.FirstOrDefaultAsync(b => b.Id == bookingId);
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
}
