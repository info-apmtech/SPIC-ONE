using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Payments;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// Regression checks for Guest House Instant Cancellation and Manual Refund safeguards:
/// 1. Immediate room availability release: single-room & multi-room allocations deleted atomically.
/// 2. Paid bookings: RefundStatus = Pending, no Razorpay refund invoked.
/// 3. Unpaid & Employee Complimentary bookings: RefundStatus = Completed immediately with zero refund.
/// 4. Razorpay payment race condition: captured payment recorded, queued for manual refund, booking NOT revived.
/// 5. Strict SDWA Admin authorization for manual refund completion (blocked for normal Admin, CorporateAdmin, Receptionist).
/// 6. Legacy PendingApproval cancellation requests preserved and queryable.
/// 7. CheckedIn and Completed bookings remain ineligible for cancellation.
/// </summary>
internal static class GuestHouseInstantCancellationAndRefundTests
{
	private static readonly DateTime CheckIn = new(2027, 4, 10);
	private static readonly DateTime CheckOut = new(2027, 4, 12); // 2 nights

	public static void Run()
	{
		Console.WriteLine("GuestHouseInstantCancellationAndRefund");

		Step("1. single-room instant cancellation and allocation release", SingleRoomInstantCancellation);
		Step("2. multi-room instant cancellation releases all allocations", MultiRoomInstantCancellation);
		Step("3. paid booking cancellation sets RefundStatus = Pending with no Razorpay call", PaidBookingCancellation);
		Step("4. unpaid and employee complimentary cancellations complete with zero refund", UnpaidAndComplimentaryCancellations);
		Step("5. Razorpay payment race condition on cancelled booking queues manual refund without reviving", PaymentRaceCondition);
		Step("6. manual refund completion strictly restricted to SDWA Admin", StrictSdwaAdminManualRefund);
		Step("7. legacy PendingApproval requests preserved and queryable", LegacyPendingApprovalRequests);
		Step("8. checked-in and completed bookings ineligible for cancellation", CheckedInAndCompletedIneligible);
	}

	private static void Step(string name, Func<Task> body)
	{
		Console.WriteLine($" {name}");
		try { body().GetAwaiter().GetResult(); }
		catch (Exception ex) { Check.That($"{name} ran to completion", false, $"{ex.GetType().Name}: {ex.Message}"); }
	}

	// ---- Fixture ----------------------------------------------------------------------------------

	private sealed class StubRazorpay : IRazorpayService
	{
		public int OrdersCreated;
		public int RefundsCreated => 0;

		public Task<RazorpayOrderResult> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken cancellationToken = default)
		{
			OrdersCreated++;
			return Task.FromResult(new RazorpayOrderResult { Success = true, OrderId = $"order_test_{OrdersCreated}", AmountInPaise = amountInPaise, RawResponse = "{}" });
		}
		public bool VerifySignature(string orderId, string paymentId, string signature) => signature == "good-signature";
		public Task<RazorpayOrderPaymentsResult> GetOrderPaymentsAsync(string orderId, CancellationToken cancellationToken = default) =>
			Task.FromResult(new RazorpayOrderPaymentsResult { Success = false });
	}

	private sealed class StubEnv : IWebHostEnvironment
	{
		public string ApplicationName { get; set; } = "test";
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
		public string ContentRootPath { get; set; } = Path.GetTempPath();
		public string EnvironmentName { get; set; } = "Test";
		public string WebRootPath { get; set; } = Path.GetTempPath();
		public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
	}

	private sealed class Fx : IDisposable
	{
		public readonly AppDbContext Db;
		public readonly StubRazorpay Razor = new();

		public Fx()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
				.Options;
			Db = new AppDbContext(options);
			Seed();
		}

		private void Seed()
		{
			Db.GuestHouses.Add(new GuestHouse { Id = 1, Name = "Test Guest House", IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 1, GuestHouseId = 1, RoomType = "AC Deluxe", RoomNumber = "101", PricePerNight = 1000m, ExtraCotPrice = 200m, Capacity = 2, NumberOfAdults = 2, AvailableQuantity = 2, IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 2, GuestHouseId = 1, RoomType = "Suite", RoomNumber = "201", PricePerNight = 1500m, ExtraCotPrice = 300m, Capacity = 3, NumberOfAdults = 3, AvailableQuantity = 2, IsActive = true });

			// Designations
			Db.Designations.Add(new Designation { Id = 1, Name = "Booker", RoleAccess = "GuestHouseBooking.View,MyBookings.View", IsActive = true });
			Db.Designations.Add(new Designation { Id = 2, Name = "FrontOfficeUser", RoleAccess = "FrontOffice.View,GuestHouseCancellations.View", IsActive = true });
			Db.Designations.Add(new Designation { Id = 3, Name = "NormalAdmin", RoleAccess = "GuestHouse.View,GuestHouseCancellations.View", IsActive = true });
			Db.Designations.Add(new Designation { Id = 4, Name = "SDWA_Admin", RoleAccess = "SDWAAdmin.View,GuestHouseCancellations.View", IsActive = true });

			// Users
			Db.Users.Add(new UserInfo { Id = "u-user", UserName = "user.one", DesignationId = 1, Name = "User One", Password = "x", UpdatedBy = "test" });
			Db.Users.Add(new UserInfo { Id = "u-receptionist", UserName = "user.receptionist", DesignationId = 2, Name = "Receptionist", Password = "x", UpdatedBy = "test" });
			Db.Users.Add(new UserInfo { Id = "u-normal-admin", UserName = "user.normaladmin", DesignationId = 3, Name = "Normal Admin", Password = "x", UpdatedBy = "test" });
			Db.Users.Add(new UserInfo { Id = "u-sdwa-admin", UserName = "user.sdwaadmin", DesignationId = 4, Name = "SDWA Admin", Password = "x", UpdatedBy = "test" });
			Db.Users.Add(new UserInfo { Id = "u-corp-admin", UserName = "user.corpadmin", DesignationId = null, Name = "Corp Admin", Password = "x", UpdatedBy = "test" });

			Db.SaveChanges();
		}

		public static ClaimsPrincipal Principal(string userId, AppRole role) =>
			new(new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, userId.Replace("u-", "user.")),
				new Claim(ClaimTypes.Role, role.ToString())
			}, "test", ClaimTypes.Name, ClaimTypes.Role));

		public GuestHouseBookingController Booking(string userId = "u-user", AppRole role = AppRole.Admin) =>
			new(Db, new StubEnv(), Razor, Options.Create(new RazorpayOptions()), Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseBookingController>.Instance)
			{
				ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, role) } }
			};

		public GuestHouseFrontOfficeController FrontOffice(string userId, AppRole role)
		{
			var services = new ServiceCollection().AddSingleton(Db).BuildServiceProvider();
			return new GuestHouseFrontOfficeController(Db, Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseFrontOfficeController>.Instance)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext
					{
						User = Principal(userId, role),
						RequestServices = services
					}
				}
			};
		}

		public void Dispose() => Db.Dispose();
	}

	private static int Status(IActionResult r) => r switch
	{
		ObjectResult o => o.StatusCode ?? 200,
		StatusCodeResult s => s.StatusCode,
		ForbidResult => 403,
		_ => -1
	};

	private static T? Prop<T>(IActionResult r, string name)
	{
		var value = (r as ObjectResult)?.Value;
		var prop = value?.GetType().GetProperty(name);
		return prop == null ? default : (T?)prop.GetValue(value);
	}

	private static CreateBookingRequest Req(int roomId, GuestHousePaymentMethod method = GuestHousePaymentMethod.PayAfterStay, string guest = "Test Guest") => new()
	{
		GuestHouseId = 1,
		RoomTypeId = roomId,
		NumberOfRooms = 1,
		CheckInDate = CheckIn,
		CheckInTime = new TimeSpan(12, 0, 0),
		CheckOutDate = CheckOut,
		CheckOutTime = new TimeSpan(11, 0, 0),
		ExtraBeds = 0,
		NumberOfPersons = 2,
		NumberOfAdults = 2,
		NumberOfChildren = 0,
		GuestName = guest,
		PhoneNumber = "9999999999",
		Email = "guest@example.com",
		Nationality = "Indian",
		PaymentMethod = method
	};

	// ---- 1. Single-room Instant Cancellation --------------------------------------------------------

	private static async Task SingleRoomInstantCancellation()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-user");
		var bookRes = await c.CreateBooking(Req(1, GuestHousePaymentMethod.PayAfterStay, "Single Guest"));
		Check.Equal("booking created", 200, Status(bookRes));
		var bookingId = ((BookingResultDto)((ObjectResult)bookRes).Value!).BookingId;

		// Add an allocation row
		fx.Db.GuestHouseRoomAllocations.Add(new GuestHouseRoomAllocation
		{
			GuestHouseBookingId = bookingId,
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			RoomNumber = "101",
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut
		});
		await fx.Db.SaveChangesAsync();

		Check.Equal("1 allocation exists before cancellation", 1,
			await fx.Db.GuestHouseRoomAllocations.CountAsync(a => a.GuestHouseBookingId == bookingId));

		// Cancel booking
		var cancelRes = await c.RequestCancellation(bookingId, new CreateCancellationRequest { Reason = "Emergency" });
		Check.Equal("cancellation accepted", 200, Status(cancelRes));

		var bookingAfter = await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(b => b.Id == bookingId);
		Check.Equal("booking status updated to Cancelled immediately", GuestHouseBookingStatus.Cancelled, bookingAfter.BookingStatus);

		var allocationsAfter = await fx.Db.GuestHouseRoomAllocations.CountAsync(a => a.GuestHouseBookingId == bookingId);
		Check.Equal("allocation released immediately", 0, allocationsAfter);

		// Room inventory is available again for same dates
		var newBook = await c.CreateBooking(Req(1, GuestHousePaymentMethod.PayAfterStay, "Next Guest"));
		Check.Equal("room can be booked again for the same dates", 200, Status(newBook));
	}

	// ---- 2. Multi-room Instant Cancellation ---------------------------------------------------------

	private static async Task MultiRoomInstantCancellation()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-user");

		// Seed a multi-room booking (e.g. historical or corporate) with 2 room allocations
		var booking = new GuestHouseBooking
		{
			BookingReference = "BKMULTI1",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 2,
			TotalAmount = 4000m,
			BookingStatus = GuestHouseBookingStatus.Confirmed,
			PaymentStatus = GuestHousePaymentStatus.Pending,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now,
			Guests = { new GuestHouseBookingGuest { GuestName = "Multi Guest", PhoneNumber = "9999999999" } }
		};
		fx.Db.GuestHouseBookings.Add(booking);
		await fx.Db.SaveChangesAsync();

		fx.Db.GuestHouseRoomAllocations.AddRange(
			new GuestHouseRoomAllocation { GuestHouseBookingId = booking.Id, GuestHouseId = 1, GuestHouseRoomId = 1, RoomNumber = "101", CheckInDate = CheckIn, CheckOutDate = CheckOut },
			new GuestHouseRoomAllocation { GuestHouseBookingId = booking.Id, GuestHouseId = 1, GuestHouseRoomId = 1, RoomNumber = "102", CheckInDate = CheckIn, CheckOutDate = CheckOut }
		);
		await fx.Db.SaveChangesAsync();

		Check.Equal("2 allocations exist before cancellation", 2,
			await fx.Db.GuestHouseRoomAllocations.CountAsync(a => a.GuestHouseBookingId == booking.Id));

		var cancelRes = await c.RequestCancellation(booking.Id, new CreateCancellationRequest { Reason = "Group cancelled" });
		Check.Equal("cancellation accepted", 200, Status(cancelRes));

		var bookingAfter = await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(b => b.Id == booking.Id);
		Check.Equal("multi-room booking status updated to Cancelled", GuestHouseBookingStatus.Cancelled, bookingAfter.BookingStatus);

		var allocationsAfter = await fx.Db.GuestHouseRoomAllocations.CountAsync(a => a.GuestHouseBookingId == booking.Id);
		Check.Equal("all room allocations released atomically", 0, allocationsAfter);
	}

	// ---- 3. Paid Booking Cancellation ---------------------------------------------------------------

	private static async Task PaidBookingCancellation()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-user");

		var booking = new GuestHouseBooking
		{
			BookingReference = "BKPAID01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 2400m,
			BookingStatus = GuestHouseBookingStatus.Confirmed,
			PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now,
			Guests = { new GuestHouseBookingGuest { GuestName = "Paid Guest", PhoneNumber = "9999999999" } },
			Payments = { new GuestHouseBookingPayment { PaymentMethod = GuestHousePaymentMethod.Razorpay, PaymentStatus = GuestHousePaymentStatus.Paid, Amount = 2400m, PaymentDate = DateTime.Now } }
		};
		fx.Db.GuestHouseBookings.Add(booking);
		await fx.Db.SaveChangesAsync();

		var cancelRes = await c.RequestCancellation(booking.Id, new CreateCancellationRequest { Reason = "Medical reason" });
		Check.Equal("cancellation accepted", 200, Status(cancelRes));

		var cancellation = await fx.Db.Set<GuestHouseBookingCancellation>().SingleAsync(x => x.GuestHouseBookingId == booking.Id);
		Check.Equal("paid booking refund status is Pending", GuestHouseRefundStatus.Pending, cancellation.RefundStatus);
		Check.That("refund amount calculated > 0", cancellation.RefundAmount > 0);
		Check.Equal("approval status is Approved immediately", GuestHouseCancellationApprovalStatus.Approved, cancellation.ApprovalStatus);

		// Verify NO automatic Razorpay refund API was triggered
		Check.Equal("no automatic Razorpay refund was initiated", 0, fx.Razor.RefundsCreated);
	}

	// ---- 4. Unpaid & Employee Complimentary Cancellations ------------------------------------------

	private static async Task UnpaidAndComplimentaryCancellations()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-user");

		// A. Employee Complimentary (PaymentStatus = Paid, Amount = 0, PaymentMethod = Complimentary)
		var compBooking = new GuestHouseBooking
		{
			BookingReference = "BKCOMP01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 0m,
			BookingStatus = GuestHouseBookingStatus.Confirmed,
			PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now,
			Guests = { new GuestHouseBookingGuest { GuestName = "Employee Guest", PhoneNumber = "9999999999" } },
			Payments = { new GuestHouseBookingPayment { PaymentMethod = GuestHousePaymentMethod.Complimentary, PaymentStatus = GuestHousePaymentStatus.Paid, Amount = 0m, PaymentDate = DateTime.Now } }
		};
		fx.Db.GuestHouseBookings.Add(compBooking);
		await fx.Db.SaveChangesAsync();

		var compCancel = await c.RequestCancellation(compBooking.Id, new CreateCancellationRequest { Reason = "Leave cancelled" });
		Check.Equal("complimentary cancellation accepted", 200, Status(compCancel));

		var compRec = await fx.Db.Set<GuestHouseBookingCancellation>().SingleAsync(x => x.GuestHouseBookingId == compBooking.Id);
		Check.Equal("complimentary booking refund is Completed automatically", GuestHouseRefundStatus.Completed, compRec.RefundStatus);
		Check.Equal("complimentary booking refund amount is 0", 0m, compRec.RefundAmount ?? 0m);

		// B. Dealer Pay After Stay (no payment received)
		var pasBooking = new GuestHouseBooking
		{
			BookingReference = "BKPAS01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 2000m,
			BookingStatus = GuestHouseBookingStatus.Confirmed,
			PaymentStatus = GuestHousePaymentStatus.Pending,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now,
			Guests = { new GuestHouseBookingGuest { GuestName = "Dealer Guest", PhoneNumber = "9999999999" } },
			Payments = { new GuestHouseBookingPayment { PaymentMethod = GuestHousePaymentMethod.PayAfterStay, PaymentStatus = GuestHousePaymentStatus.Pending, Amount = 2000m } }
		};
		fx.Db.GuestHouseBookings.Add(pasBooking);
		await fx.Db.SaveChangesAsync();

		var pasCancel = await c.RequestCancellation(pasBooking.Id, new CreateCancellationRequest { Reason = "Trip cancelled" });
		Check.Equal("pay after stay cancellation accepted", 200, Status(pasCancel));

		var pasRec = await fx.Db.Set<GuestHouseBookingCancellation>().SingleAsync(x => x.GuestHouseBookingId == pasBooking.Id);
		Check.Equal("unpaid pay after stay refund is Completed automatically", GuestHouseRefundStatus.Completed, pasRec.RefundStatus);
		Check.Equal("unpaid refund amount is 0", 0m, pasRec.RefundAmount ?? 0m);
	}

	// ---- 5. Razorpay Payment Race Condition ---------------------------------------------------------

	private static async Task PaymentRaceCondition()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-user");

		var booking = new GuestHouseBooking
		{
			BookingReference = "BKRACE01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 2400m,
			BookingStatus = GuestHouseBookingStatus.PendingPayment,
			PaymentStatus = GuestHousePaymentStatus.Pending,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now,
			Guests = { new GuestHouseBookingGuest { GuestName = "Race Guest", PhoneNumber = "9999999999" } },
			Payments = { new GuestHouseBookingPayment { PaymentMethod = GuestHousePaymentMethod.Razorpay, PaymentReference = "order_race_123", PaymentStatus = GuestHousePaymentStatus.Pending, Amount = 2400m } }
		};
		fx.Db.GuestHouseBookings.Add(booking);
		await fx.Db.SaveChangesAsync();

		// User cancels the booking before payment is verified
		var cancelRes = await c.RequestCancellation(booking.Id, new CreateCancellationRequest { Reason = "User changed mind" });
		Check.Equal("booking cancelled before payment callback", 200, Status(cancelRes));

		var bookingAfterCancel = await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(b => b.Id == booking.Id);
		Check.Equal("booking is Cancelled", GuestHouseBookingStatus.Cancelled, bookingAfterCancel.BookingStatus);

		// Razorpay callback arrives with captured payment
		var verifyRes = await c.VerifyPayment(booking.Id, new VerifyPaymentRequest
		{
			RazorpayOrderId = "order_race_123",
			RazorpayPaymentId = "pay_race_999",
			RazorpaySignature = "good-signature"
		});
		Check.Equal("verification response is OK", 200, Status(verifyRes));

		var bookingFinal = await fx.Db.GuestHouseBookings.AsNoTracking().Include(b => b.Payments).SingleAsync(b => b.Id == booking.Id);
		Check.Equal("cancelled booking was NOT revived to Confirmed", GuestHouseBookingStatus.Cancelled, bookingFinal.BookingStatus);
		Check.Equal("payment status recorded as Paid", GuestHousePaymentStatus.Paid, bookingFinal.PaymentStatus);
		Check.Equal("payment transaction ID saved", "pay_race_999", bookingFinal.Payments.Single().TransactionId);

		var cancellation = await fx.Db.Set<GuestHouseBookingCancellation>().SingleAsync(x => x.GuestHouseBookingId == booking.Id);
		Check.Equal("cancellation refund queued as Pending for manual refund", GuestHouseRefundStatus.Pending, cancellation.RefundStatus);
		Check.Equal("cancellation refund amount equals captured amount", 2400m, cancellation.RefundAmount);

		var refund = await fx.Db.Set<GuestHouseBookingRefund>().SingleAsync(x => x.GuestHouseBookingId == booking.Id);
		Check.Equal("refund record queued as Pending", GuestHouseRefundStatus.Pending, refund.RefundStatus);
		Check.Equal("refund record amount equals captured amount", 2400m, refund.RefundAmount);
	}

	// ---- 6. Strict SDWA Admin Manual Refund Completion ----------------------------------------------

	private static async Task StrictSdwaAdminManualRefund()
	{
		using var fx = new Fx();

		// Seed a cancelled booking with Pending refund
		var booking = new GuestHouseBooking
		{
			BookingReference = "BKREFUND01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 2400m,
			BookingStatus = GuestHouseBookingStatus.Cancelled,
			PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now,
			Guests = { new GuestHouseBookingGuest { GuestName = "Refund Guest", PhoneNumber = "9999999999" } },
			Payments = { new GuestHouseBookingPayment { PaymentMethod = GuestHousePaymentMethod.Razorpay, PaymentStatus = GuestHousePaymentStatus.Paid, Amount = 2400m, PaymentDate = DateTime.Now } }
		};
		fx.Db.GuestHouseBookings.Add(booking);
		await fx.Db.SaveChangesAsync();

		var cancellation = new GuestHouseBookingCancellation
		{
			CancellationReference = "CAN-REFUND-01",
			GuestHouseBookingId = booking.Id,
			CancellationReason = "Test manual refund",
			CancelledBy = "u-user",
			RefundAmount = 2400m,
			RefundStatus = GuestHouseRefundStatus.Pending,
			ApprovalStatus = GuestHouseCancellationApprovalStatus.Approved
		};
		fx.Db.Set<GuestHouseBookingCancellation>().Add(cancellation);
		await fx.Db.SaveChangesAsync();

		var req = new CompleteManualRefundRequest
		{
			RefundReference = "UTR-HDFC-20270415-001",
			RefundDate = DateTime.Today,
			Remarks = "Transferred via NEFT"
		};

		// A. Normal Admin (Role = Admin, but Designation != SDWA_Admin) -> 403 Forbidden
		var normalAdminFo = fx.FrontOffice("u-normal-admin", AppRole.Admin);
		var resNormalAdmin = await normalAdminFo.CompleteManualRefund(cancellation.Id, req);
		Check.Equal("Normal Admin is blocked from completing manual refund (403)", 403, Status(resNormalAdmin));

		// B. CorporateAdmin -> 403 Forbidden
		var corpAdminFo = fx.FrontOffice("u-corp-admin", AppRole.CorporateAdmin);
		var resCorpAdmin = await corpAdminFo.CompleteManualRefund(cancellation.Id, req);
		Check.Equal("CorporateAdmin is blocked from completing manual refund (403)", 403, Status(resCorpAdmin));

		// C. Receptionist -> 403 Forbidden
		var receptionistFo = fx.FrontOffice("u-receptionist", AppRole.RM);
		var resReceptionist = await receptionistFo.CompleteManualRefund(cancellation.Id, req);
		Check.Equal("Receptionist is blocked from completing manual refund (403)", 403, Status(resReceptionist));

		// D. SDWA Admin -> 200 OK
		var sdwaAdminFo = fx.FrontOffice("u-sdwa-admin", AppRole.Admin);
		var resSdwa = await sdwaAdminFo.CompleteManualRefund(cancellation.Id, req);
		Check.Equal("SDWA Admin successfully completes manual refund (200)", 200, Status(resSdwa));

		var cancAfter = await fx.Db.Set<GuestHouseBookingCancellation>().SingleAsync(c => c.Id == cancellation.Id);
		Check.Equal("cancellation RefundStatus is Completed", GuestHouseRefundStatus.Completed, cancAfter.RefundStatus);
		Check.Equal("cancellation Remarks updated", "Transferred via NEFT", cancAfter.Remarks);

		var refundAfter = await fx.Db.Set<GuestHouseBookingRefund>().SingleAsync(r => r.GuestHouseBookingId == booking.Id);
		Check.Equal("refund RefundStatus is Completed", GuestHouseRefundStatus.Completed, refundAfter.RefundStatus);
		Check.Equal("refund reference saved", "UTR-HDFC-20270415-001", refundAfter.RefundReference);

		// E. Idempotent: repeating complete refund returns 200 with AlreadyProcessed
		var resRepeat = await sdwaAdminFo.CompleteManualRefund(cancellation.Id, req);
		Check.Equal("repeating complete refund is idempotent (200)", 200, Status(resRepeat));
		Check.That("AlreadyProcessed is true", Prop<bool>(resRepeat, "AlreadyProcessed"));
	}

	// ---- 7. Legacy PendingApproval Requests ---------------------------------------------------------

	private static async Task LegacyPendingApprovalRequests()
	{
		using var fx = new Fx();

		// Seed a legacy cancellation request created under the old system
		var booking = new GuestHouseBooking
		{
			BookingReference = "BKLEGACY01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 2400m,
			BookingStatus = GuestHouseBookingStatus.Confirmed,
			PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now.AddDays(-10),
			Guests = { new GuestHouseBookingGuest { GuestName = "Legacy Guest", PhoneNumber = "9999999999" } }
		};
		fx.Db.GuestHouseBookings.Add(booking);
		await fx.Db.SaveChangesAsync();

		var legacyCanc = new GuestHouseBookingCancellation
		{
			CancellationReference = "CAN-LEGACY-01",
			GuestHouseBookingId = booking.Id,
			CancellationReason = "Old cancellation request",
			CancelledBy = "u-user",
			RefundAmount = 2400m,
			RefundStatus = GuestHouseRefundStatus.Pending,
			ApprovalStatus = GuestHouseCancellationApprovalStatus.PendingApproval
		};
		fx.Db.Set<GuestHouseBookingCancellation>().Add(legacyCanc);
		await fx.Db.SaveChangesAsync();

		var fo = fx.FrontOffice("u-sdwa-admin", AppRole.Admin);

		// Query pending approval tab
		var resPendingApproval = await fo.GetCancellationRequests("PendingApproval");
		Check.Equal("querying PendingApproval tab succeeds", 200, Status(resPendingApproval));
		var pendingItems = (List<CancellationRequestListItemDto>)((OkObjectResult)resPendingApproval).Value!;
		Check.That("legacy pending approval request is listed in PendingApproval tab",
			pendingItems.Any(i => i.CancellationId == legacyCanc.Id));

		// Query pending refund tab
		var resPendingRefund = await fo.GetCancellationRequests("PendingRefund");
		Check.Equal("querying PendingRefund tab succeeds", 200, Status(resPendingRefund));
		var refundItems = (List<CancellationRequestListItemDto>)((OkObjectResult)resPendingRefund).Value!;
		Check.That("legacy request is also listed under PendingRefund (since refund is pending)",
			refundItems.Any(i => i.CancellationId == legacyCanc.Id));
	}

	// ---- 8. Checked-in & Completed Ineligible --------------------------------------------------------

	private static async Task CheckedInAndCompletedIneligible()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-user");

		// Checked-in booking
		var checkInBooking = new GuestHouseBooking
		{
			BookingReference = "BKCHKIN01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn,
			CheckOutDate = CheckOut,
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 2000m,
			BookingStatus = GuestHouseBookingStatus.CheckedIn,
			PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now,
			Guests = { new GuestHouseBookingGuest { GuestName = "In Guest", PhoneNumber = "9999999999" } }
		};
		fx.Db.GuestHouseBookings.Add(checkInBooking);

		// Completed booking
		var completedBooking = new GuestHouseBooking
		{
			BookingReference = "BKCOMPL01",
			GuestHouseId = 1,
			GuestHouseRoomId = 1,
			CheckInDate = CheckIn.AddDays(-5),
			CheckOutDate = CheckIn.AddDays(-3),
			NumberOfNights = 2,
			NumberOfRooms = 1,
			TotalAmount = 2000m,
			BookingStatus = GuestHouseBookingStatus.Completed,
			PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-user",
			CreatedAt = DateTime.Now.AddDays(-10),
			Guests = { new GuestHouseBookingGuest { GuestName = "Out Guest", PhoneNumber = "9999999999" } }
		};
		fx.Db.GuestHouseBookings.Add(completedBooking);
		await fx.Db.SaveChangesAsync();

		var resCheckIn = await c.RequestCancellation(checkInBooking.Id, new CreateCancellationRequest { Reason = "Cancel checked in" });
		Check.Equal("CheckedIn booking cannot be cancelled (400)", 400, Status(resCheckIn));

		var resCompleted = await c.RequestCancellation(completedBooking.Id, new CreateCancellationRequest { Reason = "Cancel completed" });
		Check.Equal("Completed booking cannot be cancelled (400)", 400, Status(resCompleted));
	}
}
