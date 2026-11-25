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
using GuestHouseBillPdfHelper = SpicAPI.Services.GuestHouseBillPdfHelper;

namespace SPIC.RegressionHarness;

/// <summary>
/// "One booking = one room = one separate bill" - API regression checks.
///
/// These call the REAL GuestHouseBookingController / GuestHouseFrontOfficeController actions against an
/// EF Core InMemory database (a fresh, private one per test - no PostgreSQL, no production data) with a
/// stubbed IRazorpayService (no network, no real payment is ever created).
///
/// Limits of the InMemory provider, so these are NOT covered here (they need PostgreSQL):
///  - the Serializable-transaction race itself (InMemory ignores transactions);
///  - ExecuteUpdate / ExecuteDelete paths: payment/failed, cancellation approve/reject and therefore
///    refund processing (GuestHouseRefundStatusSync). Cancellation REQUESTS are covered, refund approval is not;
///  - controller action filters run by the MVC pipeline are not executed when an action is called directly,
///    so GuestHouseBookingAccessAttribute is exercised separately by invoking it on a hand-built context.
/// </summary>
internal static class GuestHouseSingleRoomBookingTests
{
	private static readonly DateTime CheckIn = new(2027, 3, 10);
	private static readonly DateTime CheckOut = new(2027, 3, 12); // 2 nights

	public static void Run()
	{
		Console.WriteLine("GuestHouseSingleRoomBooking");

		Step("1. single-room booking", SingleRoomBooking);
		Step("2. multi-room requests rejected", MultiRoomRejected);
		Step("3. separate bookings and separate bills", SeparateBookingsAndBills);
		Step("4. payment methods unchanged", PaymentMethods);
		Step("5. cancellation isolation", CancellationIsolation);
		Step("6. historical multi-room booking", HistoricalMultiRoom);
		Step("7. duplicate submission protection", DuplicateSubmission);
		Step("8. authorization and roles", Authorization);
		Step("9. Employee ID / Dealer Code binding", CodeBinding);
		Step("10. Complimentary eligibility drives the binding", CodeEligibility);
		Step("11. persons / adults / children rule", GuestCountRule);
		Step("12. guest counts through the booking API", GuestCountsApi);
		Step("13. Guest Details page wiring (source checks)", GuestDetailsWiring);
		Step("14. 'Employee Stay' wording; internal values unchanged", EmployeeStayWording);
		Step("15. edited guest code: identity, eligibility and invoice GSTIN unaffected", EditedGuestCode);
	}

	// A crash inside one scenario must show up as a failure, not abort the whole harness.
	private static void Step(string name, Func<Task> body)
	{
		Console.WriteLine($" {name}");
		try { body().GetAwaiter().GetResult(); }
		catch (Exception ex) { Check.That($"{name} ran to completion", false, $"{ex.GetType().Name}: {ex.Message}"); }
	}

	// ---- fixture ----------------------------------------------------------------------------------

	private sealed class StubRazorpay : IRazorpayService
	{
		public int OrdersCreated;
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
				// The booking code opens a Serializable transaction; InMemory has none, so just ignore it.
				.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
				.Options;
			Db = new AppDbContext(options);
			Seed();
		}

		private void Seed()
		{
			Db.GuestHouses.Add(new GuestHouse { Id = 1, Name = "Test Guest House", IsActive = true });
			// Room 1 / Room 2 are two different room records (two different "rooms" a user can pick).
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 1, GuestHouseId = 1, RoomType = "AC Deluxe", RoomNumber = "101", PricePerNight = 1000m, ExtraCotPrice = 200m, Capacity = 2, NumberOfAdults = 2, AvailableQuantity = 3, IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 2, GuestHouseId = 1, RoomType = "Suite", RoomNumber = "201", PricePerNight = 1500m, ExtraCotPrice = 300m, Capacity = 3, NumberOfAdults = 3, AvailableQuantity = 2, IsActive = true });

			// Designations as stored in production: RoleAccess is a CSV of page permissions.
			Db.Designations.Add(new Designation { Id = 1, Name = "Booker", RoleAccess = "GuestHouseBooking.View,MyBookings.View", IsActive = true });
			Db.Designations.Add(new Designation { Id = 2, Name = "BookingOnly", RoleAccess = "GuestHouseBooking.View", IsActive = true });
			Db.Designations.Add(new Designation { Id = 3, Name = "Nothing", RoleAccess = "Dashboard.View", IsActive = true });
			Db.Users.Add(TestUser("u-a", "user.a", 1));
			Db.Users.Add(TestUser("u-b", "user.b", 1));
			Db.Users.Add(TestUser("u-bookingonly", "user.bookingonly", 2));
			Db.Users.Add(TestUser("u-none", "user.none", 3));

			// A large room, so the guest-count checks are not confused with the capacity rule.
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 3, GuestHouseId = 1, RoomType = "Dormitory", RoomNumber = "301", PricePerNight = 500m, Capacity = 6, NumberOfAdults = 6, AvailableQuantity = 2, IsActive = true });

			// Employee / Dealer masters behind the Employee ID / Dealer Code binding.
			Db.EmployeeInformation.Add(Employee(50, "EMP-0050", "Emp Fifty"));
			Db.EmployeeInformation.Add(Employee(51, "OLD-CODE", "Old Row"));
			Db.EmployeeInformation.Add(Employee(52, "EMP-0052", "Emp Fifty-two"));
			Db.EmployeeInformation.Add(Employee(53, "  ", "No Code"));
			Db.Employeelogins.Add(new EmployeeRegistration.Employeelogin { Id = 1, UserId = "u-emp", Role = AppRole.RM, EmployeeInformationID = 50, IsActive = true });
			// A user with a stale deactivated row and a current active one: the shared rule picks the active row.
			Db.Employeelogins.Add(new EmployeeRegistration.Employeelogin { Id = 2, UserId = "u-emp2", Role = AppRole.RM, EmployeeInformationID = 51, IsActive = false });
			Db.Employeelogins.Add(new EmployeeRegistration.Employeelogin { Id = 3, UserId = "u-emp2", Role = AppRole.RM, EmployeeInformationID = 52, IsActive = true });
			Db.Employeelogins.Add(new EmployeeRegistration.Employeelogin { Id = 4, UserId = "u-nocode", Role = AppRole.RM, EmployeeInformationID = 53, IsActive = true });
			Db.DealerRegistrations.Add(Dealer(70, "u-dealer", "D-1234"));
			Db.DealerRegistrations.Add(Dealer(71, "u-dealer2", "D-5678"));
			// The users the earlier scenarios book as: a Dealer (u-a, u-b as Dealer) and an RM (u-b as RM).
			Db.DealerRegistrations.Add(Dealer(73, "u-a", "D-A001"));
			Db.DealerRegistrations.Add(Dealer(74, "u-b", "D-B001"));
			Db.EmployeeInformation.Add(Employee(54, "EMP-B054", "Emp B"));
			Db.Employeelogins.Add(new EmployeeRegistration.Employeelogin { Id = 5, UserId = "u-b", Role = AppRole.RM, EmployeeInformationID = 54, IsActive = true });
			// A dealer who has registered but not yet been approved has no code.
			Db.DealerRegistrations.Add(Dealer(72, "u-newdealer", null));
			Db.SaveChanges();
		}

		private static EmployeeRegistration.EmployeeInformation Employee(int id, string code, string name) =>
			new() { Id = id, EmployeeCode = code, Name = name, Email = "e@example.com", PersonalPhoneNumber = "1", OfficialPhoneNumber = "2", CreatedBy = "test" };

		private static DealerRegistration Dealer(int id, string userId, string? code) =>
			new()
			{
				Id = id, UserTableId = userId, DealerCode = code, FirmName = "Test Firm", AccountHolderName = "Test", AccountNumber = "0", Branch = "Test", IFSC = "TEST0000000",
				OfficialContactNumber = "1", WhatsAppNumber = "1", PinCode = "600001", ShopNoORRoomNoOrBlockNo = "1", Village = "Test", CreatedBy = "test", UpdatedBy = "test"
			};

		private static UserInfo TestUser(string id, string userName, int designationId) =>
			new() { Id = id, UserName = userName, DesignationId = designationId, Name = userName, Password = "not-a-real-password", UpdatedBy = "test" };

		public static ClaimsPrincipal Principal(string userId, AppRole role) =>
			new(new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, userId.Replace("u-", "user.")),
				new Claim(ClaimTypes.Role, role.ToString())
			}, "test", ClaimTypes.Name, ClaimTypes.Role));

		public GuestHouseBookingController Booking(string userId, AppRole role = AppRole.Dealer) =>
			new(Db, new StubEnv(), Razor, Options.Create(new RazorpayOptions()), Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseBookingController>.Instance)
			{
				ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal(userId, role) } }
			};

		public GuestHouseFrontOfficeController FrontOffice() =>
			new(Db, Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseFrontOfficeController>.Instance)
			{
				// Admin: never state-scoped, so the receptionist scope does not interfere with billing checks.
				ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Principal("u-admin", AppRole.Admin) } }
			};

		public void Dispose() => Db.Dispose();
	}

	// Persons and adults are required by the guest-count rule, so by default every guest is an adult.
	private const int SameAsPersons = int.MinValue;

	private static CreateBookingRequest Req(int roomId, int? rooms = 1, GuestHousePaymentMethod method = GuestHousePaymentMethod.Razorpay,
		string guest = "Guest One", int? persons = 2, int? extraBeds = 0, int? adults = SameAsPersons, int? children = 0, string? code = null) => new()
	{
		GuestHouseId = 1,
		RoomTypeId = roomId,
		CheckInDate = CheckIn,
		CheckOutDate = CheckOut,
		NumberOfRooms = rooms,
		ExtraBeds = extraBeds,
		NumberOfPersons = persons,
		NumberOfAdults = adults == SameAsPersons ? persons : adults,
		NumberOfChildren = children,
		EmployeeOrDealerCode = code,
		GuestName = guest,
		PhoneNumber = "9999999999",
		Email = "g@example.com",
		Nationality = "Indian",
		PaymentMethod = method
	};

	private static int Status(IActionResult r) => r switch
	{
		ObjectResult o => o.StatusCode ?? 200,
		StatusCodeResult s => s.StatusCode,
		ForbidResult => 403,
		FileResult => 200,
		_ => -1
	};

	private static T? Prop<T>(IActionResult r, string name)
	{
		var value = (r as ObjectResult)?.Value;
		var prop = value?.GetType().GetProperty(name);
		return prop == null ? default : (T?)prop.GetValue(value);
	}

	private static BookingResultDto Result(IActionResult r) => (BookingResultDto)((ObjectResult)r).Value!;

	private static async Task<BookingResultDto> Book(GuestHouseBookingController c, CreateBookingRequest req)
	{
		var r = await c.CreateBooking(req);
		Check.Equal($"booking for room {req.RoomTypeId} succeeds (HTTP)", 200, Status(r));
		return Result(r);
	}

	private static async Task Counts(Fx fx, string label, int bookings, int payments, int bills)
	{
		Check.Equal($"{label}: booking rows", bookings, await fx.Db.GuestHouseBookings.CountAsync());
		Check.Equal($"{label}: payment rows", payments, await fx.Db.GuestHouseBookingPayments.CountAsync());
		Check.Equal($"{label}: bill rows", bills, await fx.Db.GuestHouseBills.CountAsync());
	}

	// ---- 1. single-room booking ----------------------------------------------------------------------

	private static async Task SingleRoomBooking()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-a");
		var result = await Book(c, Req(1, rooms: 1));

		Check.That("booking reference is generated", !string.IsNullOrWhiteSpace(result.BookingReference));
		Check.That("booking reference has the BK + 8 character format", System.Text.RegularExpressions.Regex.IsMatch(result.BookingReference, "^BK[A-Z2-9]{8}$"), result.BookingReference);

		var b = await fx.Db.GuestHouseBookings.Include(x => x.Payments).Include(x => x.RoomAllocations).SingleAsync();
		Check.Equal("exactly one room on the booking", 1, b.NumberOfRooms ?? 0);
		Check.Equal("the selected room record is saved", 1, b.GuestHouseRoomId);
		Check.Equal("no multi-record allocation rows are created", 0, b.RoomAllocations.Count);
		Check.Equal("exactly one payment record", 1, b.Payments.Count);
		Check.Equal("reference on the row matches the response", result.BookingReference, b.BookingReference ?? "");
		// 2 nights x 1000, 5% GST: the server priced it (the request carries no money fields).
		Check.Equal("server-calculated total (2 nights x 1000 + 5% GST)", 2100m, b.TotalAmount ?? -1m);
		Check.Equal("payment amount equals the booking total", 2100m, b.Payments.Single().Amount);

		// A missing quantity (older clients) still means exactly one room.
		var fx2 = new Fx();
		var r2 = await fx2.Booking("u-a").CreateBooking(Req(1, rooms: null));
		Check.Equal("null NumberOfRooms is accepted as 1", 200, Status(r2));
		Check.Equal("null NumberOfRooms is stored as 1", 1, (await fx2.Db.GuestHouseBookings.SingleAsync()).NumberOfRooms ?? 0);
		fx2.Dispose();
	}

	// ---- 2. reject multiple rooms --------------------------------------------------------------------

	private static async Task MultiRoomRejected()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-a");

		foreach (var qty in new[] { 2, 3, 5, 0, -1, 100 })
		{
			var r = await c.CreateBooking(Req(1, rooms: qty));
			Check.Equal($"NumberOfRooms = {qty} returns HTTP 400", 400, Status(r));
		}
		await Counts(fx, "after rejected multi-room requests", 0, 0, 0);

		// Other tampering the same endpoint must still refuse.
		Check.Equal("extra beds beyond one per room are rejected", 400, Status(await c.CreateBooking(Req(1, extraBeds: 2))));
		Check.Equal("negative extra beds are rejected", 400, Status(await c.CreateBooking(Req(1, extraBeds: -1))));
		Check.Equal("guests beyond capacity (+ extra beds) are rejected", 400, Status(await c.CreateBooking(Req(1, persons: 4))));
		Check.Equal("negative guest count is rejected", 400, Status(await c.CreateBooking(Req(1, persons: -1))));
		await Counts(fx, "after the other rejected requests", 0, 0, 0);

		// Boundary: capacity 2 + 1 extra bed = 3 guests is allowed.
		Check.Equal("capacity + one extra bed is accepted", 200, Status(await c.CreateBooking(Req(1, persons: 3, extraBeds: 1, guest: "Boundary Guest"))));
	}

	// ---- 3. separate bookings and bills --------------------------------------------------------------

	private static async Task SeparateBookingsAndBills()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-a");
		var a = await Book(c, Req(1, guest: "Same Customer"));
		var b = await Book(c, Req(2, guest: "Same Customer"));

		Check.That("two distinct booking ids", a.BookingId != b.BookingId);
		Check.That("two distinct booking references", a.BookingReference != b.BookingReference);
		await Counts(fx, "after two bookings", 2, 2, 0);

		var rows = await fx.Db.GuestHouseBookings.Include(x => x.Payments).OrderBy(x => x.Id).ToListAsync();
		Check.That("both bookings belong to the same user with identical dates",
			rows.All(x => x.CreatedBy == "u-a" && x.CheckInDate == CheckIn && x.CheckOutDate == CheckOut));
		Check.That("each booking has exactly one room, on its own room record",
			rows[0].NumberOfRooms == 1 && rows[1].NumberOfRooms == 1 && rows[0].GuestHouseRoomId == 1 && rows[1].GuestHouseRoomId == 2);
		Check.That("each booking has its own single payment row for its own amount",
			rows[0].Payments.Count == 1 && rows[1].Payments.Count == 1
			&& rows[0].Payments.Single().Amount == 2100m && rows[1].Payments.Single().Amount == 3150m
			&& rows[0].Payments.Single().Id != rows[1].Payments.Single().Id);

		// Check-out (Completed) is a front-office step; set it directly, then bill through the real endpoint.
		foreach (var row in rows) row.BookingStatus = GuestHouseBookingStatus.Completed;
		await fx.Db.SaveChangesAsync();

		var fo = fx.FrontOffice();
		var billA = await fo.GenerateBill(new GenerateBillRequest { BookingId = a.BookingId, LineItems = { new BillLineItemDto { Description = "Room Charge", Quantity = 2, Rate = 1000m } }, PaymentMethod = (int)GuestHousePaymentMethod.Razorpay });
		var billB = await fo.GenerateBill(new GenerateBillRequest { BookingId = b.BookingId, LineItems = { new BillLineItemDto { Description = "Room Charge", Quantity = 2, Rate = 1500m } }, PaymentMethod = (int)GuestHousePaymentMethod.Razorpay });
		Check.Equal("bill for booking A generated", 200, Status(billA));
		Check.Equal("bill for booking B generated", 200, Status(billB));

		var bills = await fx.Db.GuestHouseBills.Include(x => x.LineItems).OrderBy(x => x.Id).ToListAsync();
		Check.Equal("two separate bills", 2, bills.Count);
		Check.Equal("bill A references only booking A", a.BookingId, bills[0].GuestHouseBookingId);
		Check.Equal("bill B references only booking B", b.BookingId, bills[1].GuestHouseBookingId);
		Check.That("bills carry their own booking reference", bills[0].BookingReference == a.BookingReference && bills[1].BookingReference == b.BookingReference);
		Check.That("bill numbers are distinct", bills[0].BillNumber != bills[1].BillNumber);
		Check.That("each bill has only its own room line (no combined line items)", bills[0].LineItems.Count == 1 && bills[1].LineItems.Count == 1);
		Check.That("bill A charges are room 1 only (2 x 1000 + GST)", bills[0].Subtotal == 2000m && bills[0].TotalAmount == 2100m, $"{bills[0].Subtotal}/{bills[0].TotalAmount}");
		Check.That("bill B charges are room 2 only (2 x 1500 + GST)", bills[1].Subtotal == 3000m && bills[1].TotalAmount == 3150m, $"{bills[1].Subtotal}/{bills[1].TotalAmount}");
		Check.That("bills describe their own room type", bills[0].RoomType == "AC Deluxe" && bills[1].RoomType == "Suite");
		Check.That("bills each show one room", bills[0].NumberOfRooms == 1 && bills[1].NumberOfRooms == 1);
		Check.That("no bill holds the sum of both bookings", bills.All(x => x.TotalAmount != 5250m));

		var again = await fo.GenerateBill(new GenerateBillRequest { BookingId = a.BookingId, LineItems = { new BillLineItemDto { Description = "Room Charge", Quantity = 2, Rate = 1000m } } });
		Check.Equal("a second bill for the same booking is refused", 400, Status(again));
		Check.Equal("still exactly two bills", 2, await fx.Db.GuestHouseBills.CountAsync());
	}

	// ---- 4. payment methods ----------------------------------------------------------------------------

	private static async Task PaymentMethods()
	{
		using var fx = new Fx();

		// Pay Now (Razorpay): PendingPayment + Pending until the signature is verified.
		var dealer = fx.Booking("u-a", AppRole.Dealer);
		var online = await Book(dealer, Req(1, guest: "Online Guest"));
		var onlineRow = await fx.Db.GuestHouseBookings.Include(x => x.Payments).SingleAsync(x => x.Id == online.BookingId);
		Check.Equal("Razorpay: booking is PendingPayment", GuestHouseBookingStatus.PendingPayment, onlineRow.BookingStatus);
		Check.Equal("Razorpay: payment is Pending", GuestHousePaymentStatus.Pending, onlineRow.PaymentStatus);
		Check.Equal("Razorpay: payment method stored", GuestHousePaymentMethod.Razorpay, onlineRow.Payments.Single().PaymentMethod);
		Check.That("Razorpay: nothing is marked paid yet", onlineRow.Payments.Single().PaymentDate == null);

		// A second, independent online booking: payment verification must touch only its own booking.
		var online2 = await Book(dealer, Req(2, guest: "Online Guest 2"));

		var order = await dealer.CreatePaymentOrder(online.BookingId);
		Check.Equal("create-order succeeds (stubbed Razorpay, no network)", 200, Status(order));
		Check.Equal("the order amount is the server total in paise", 210000L, Prop<long>(order, "AmountInPaise"));
		Check.Equal("exactly one stub order was created", 1, fx.Razor.OrdersCreated);

		var bad = await dealer.VerifyPayment(online.BookingId, new VerifyPaymentRequest { RazorpayOrderId = "order_test_1", RazorpayPaymentId = "pay_1", RazorpaySignature = "forged" });
		Check.Equal("a forged signature is rejected", 400, Status(bad));
		Check.Equal("a forged signature leaves the booking unpaid", GuestHousePaymentStatus.Pending, (await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(x => x.Id == online.BookingId)).PaymentStatus);

		var good = await dealer.VerifyPayment(online.BookingId, new VerifyPaymentRequest { RazorpayOrderId = "order_test_1", RazorpayPaymentId = "pay_1", RazorpaySignature = "good-signature" });
		Check.Equal("a valid signature is accepted", 200, Status(good));
		var paid = await fx.Db.GuestHouseBookings.AsNoTracking().Include(x => x.Payments).SingleAsync(x => x.Id == online.BookingId);
		Check.Equal("verified booking becomes Confirmed", GuestHouseBookingStatus.Confirmed, paid.BookingStatus);
		Check.Equal("verified booking becomes Paid", GuestHousePaymentStatus.Paid, paid.PaymentStatus);
		var other = await fx.Db.GuestHouseBookings.AsNoTracking().Include(x => x.Payments).SingleAsync(x => x.Id == online2.BookingId);
		Check.That("verification did not touch the other booking",
			other.BookingStatus == GuestHouseBookingStatus.PendingPayment && other.PaymentStatus == GuestHousePaymentStatus.Pending
			&& other.Payments.Single().PaymentStatus == GuestHousePaymentStatus.Pending && other.Payments.Single().PaymentReference == null);

		// Pay After Stay: Confirmed immediately, payment stays Pending.
		var pas = await Book(fx.Booking("u-a", AppRole.Dealer), Req(1, method: GuestHousePaymentMethod.PayAfterStay, guest: "Later Guest"));
		var pasRow = await fx.Db.GuestHouseBookings.Include(x => x.Payments).SingleAsync(x => x.Id == pas.BookingId);
		Check.Equal("Pay After Stay: booking is Confirmed", GuestHouseBookingStatus.Confirmed, pasRow.BookingStatus);
		Check.Equal("Pay After Stay: payment is Pending", GuestHousePaymentStatus.Pending, pasRow.PaymentStatus);
		Check.Equal("Pay After Stay: method stored", GuestHousePaymentMethod.PayAfterStay, pasRow.Payments.Single().PaymentMethod);
		Check.That("Pay After Stay: not marked paid, no payment date", pasRow.Payments.Single().PaymentDate == null);
		Check.That("Pay After Stay: not flagged as a free booking", !pas.IsFreeBooking);

		// Complimentary: decided from the role claim only, GBP 0, Confirmed + Paid.
		var free = await Book(fx.Booking("u-b", AppRole.RM), Req(1, guest: "Employee Guest"));
		Check.That("employee (RM) booking is flagged free", free.IsFreeBooking);
		var freeRow = await fx.Db.GuestHouseBookings.Include(x => x.Payments).SingleAsync(x => x.Id == free.BookingId);
		Check.That("Complimentary: Confirmed + Paid, total 0", freeRow.BookingStatus == GuestHouseBookingStatus.Confirmed && freeRow.PaymentStatus == GuestHousePaymentStatus.Paid && freeRow.TotalAmount == 0m);
		Check.That("Complimentary: payment row is Complimentary / EMPLOYEE-FREE / 0",
			freeRow.Payments.Single().PaymentMethod == GuestHousePaymentMethod.Complimentary && freeRow.Payments.Single().TransactionId == "EMPLOYEE-FREE" && freeRow.Payments.Single().Amount == 0m);
		Check.Equal("Complimentary: still exactly one room", 1, freeRow.NumberOfRooms ?? 0);

		// A paying role cannot ask for Complimentary.
		var cheat = await fx.Booking("u-a", AppRole.Dealer).CreateBooking(Req(2, method: GuestHousePaymentMethod.Complimentary, guest: "Cheat"));
		Check.Equal("a Dealer cannot request Complimentary", 400, Status(cheat));
		var farmerFree = await fx.Booking("u-a", AppRole.Farmer).CreateBooking(Req(2, guest: "Farmer Guest"));
		Check.That("a Farmer still pays (not free)", Status(farmerFree) == 200 && !Result(farmerFree).IsFreeBooking);
	}

	// ---- 5. cancellation isolation ----------------------------------------------------------------------

	private static async Task CancellationIsolation()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-a");
		var a = await Book(c, Req(1, method: GuestHousePaymentMethod.PayAfterStay, guest: "Cancel A"));
		var b = await Book(c, Req(2, method: GuestHousePaymentMethod.PayAfterStay, guest: "Cancel B"));
		var bBefore = await fx.Db.GuestHouseBookings.AsNoTracking().Include(x => x.Payments).SingleAsync(x => x.Id == b.BookingId);

		var cancel = await c.RequestCancellation(a.BookingId, new CreateCancellationRequest { Reason = "Change in travel plans" });
		Check.Equal("cancellation request for booking A accepted", 200, Status(cancel));
		Check.That("a cancellation row exists for A only",
			await fx.Db.Set<GuestHouseBookingCancellation>().CountAsync(x => x.GuestHouseBookingId == a.BookingId) == 1
			&& await fx.Db.Set<GuestHouseBookingCancellation>().CountAsync(x => x.GuestHouseBookingId == b.BookingId) == 0);
		Check.Equal("cancellation starts pending approval", "PendingApproval", Prop<string>(cancel, "ApprovalStatus") ?? "");

		var again = await c.RequestCancellation(a.BookingId, new CreateCancellationRequest { Reason = "Change in travel plans" });
		Check.That("repeating the request is idempotent", Status(again) == 200 && Prop<bool>(again, "AlreadyRequested"));
		Check.Equal("still one cancellation row in total", 1, await fx.Db.Set<GuestHouseBookingCancellation>().CountAsync());

		var bAfter = await fx.Db.GuestHouseBookings.AsNoTracking().Include(x => x.Payments).SingleAsync(x => x.Id == b.BookingId);
		Check.That("booking B is unchanged (status, payment, amounts, room)",
			bAfter.BookingStatus == bBefore.BookingStatus && bAfter.PaymentStatus == bBefore.PaymentStatus && bAfter.TotalAmount == bBefore.TotalAmount
			&& bAfter.GuestHouseRoomId == bBefore.GuestHouseRoomId && bAfter.Payments.Single().PaymentStatus == bBefore.Payments.Single().PaymentStatus);
		Check.That("no refund row was created for either booking", !await fx.Db.Set<GuestHouseBookingRefund>().AnyAsync());
		Check.Equal("a request never cancels the booking itself", GuestHouseBookingStatus.Confirmed,
			(await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(x => x.Id == a.BookingId)).BookingStatus);
	}

	// ---- 6. historical multi-room booking -----------------------------------------------------------------

	private static async Task HistoricalMultiRoom()
	{
		using var fx = new Fx();
		// A booking made before this change: 2 rooms, two inventory records, one bill covering both.
		fx.Db.GuestHouseBookings.Add(new GuestHouseBooking
		{
			Id = 900, BookingReference = "BKHISTORY", GuestHouseId = 1, GuestHouseRoomId = 1,
			CheckInDate = CheckIn, CheckOutDate = CheckOut, NumberOfNights = 2, NumberOfRooms = 2,
			RoomPrice = 1000m, SubTotal = 4000m, TaxAmount = 200m, TotalAmount = 4200m,
			BookingStatus = GuestHouseBookingStatus.Completed, PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-a", CreatedAt = DateTime.Now.AddDays(-30), UpdatedBy = "u-a", UpdatedAt = DateTime.Now.AddDays(-30),
			Guests = { new GuestHouseBookingGuest { GuestName = "Historic Guest", PhoneNumber = "8888888888" } },
			Payments = { new GuestHouseBookingPayment { PaymentMethod = GuestHousePaymentMethod.Razorpay, PaymentStatus = GuestHousePaymentStatus.Paid, Amount = 4200m, PaymentDate = DateTime.Now.AddDays(-30) } }
		});
		fx.Db.GuestHouseBills.Add(new GuestHouseBill
		{
			Id = 900, BillNumber = "BILL-HIST-1", GuestHouseBookingId = 900, BookingReference = "BKHISTORY", GuestName = "Historic Guest",
			GuestHouseName = "Test Guest House", RoomType = "AC Deluxe", NumberOfNights = 2, NumberOfRooms = 2,
			Subtotal = 4000m, CgstAmount = 100m, SgstAmount = 100m, TotalAmount = 4200m, BalanceAmount = 4200m,
			PaymentStatus = GuestHousePaymentStatus.Paid, CreatedBy = "u-a", UpdatedBy = "u-a",
			LineItems =
			{
				new GuestHouseBillLineItem { Description = "AC Deluxe - Room Charge", Quantity = 4m, Rate = 1000m, Amount = 4000m, CgstPercent = 2.5m, SgstPercent = 2.5m, CgstAmount = 100m, SgstAmount = 100m, LineTotal = 4200m },
				new GuestHouseBillLineItem { Description = "Food Bill", Quantity = 1m, Rate = 0m, Amount = 0m, LineTotal = 0m }
			}
		});
		await fx.Db.SaveChangesAsync();

		// New single-room activity must not disturb it.
		var c = fx.Booking("u-a");
		await Book(c, Req(2, guest: "Newer Guest"));

		var details = await c.GetBookingDetails(900);
		Check.Equal("historical booking details are retrievable", 200, Status(details));
		var dto = (BookingDetailsDto)((ObjectResult)details).Value!;
		Check.Equal("historical room quantity is still 2", 2, dto.NumberOfRooms ?? 0);
		Check.Equal("historical room cost still covers 2 rooms x 2 nights", 4000m, dto.RoomCost);
		Check.Equal("historical total is unchanged", 4200m, dto.TotalAmount ?? -1m);
		Check.That("historical booking still reports its invoice", dto.HasInvoice);

		var invoice = await c.DownloadInvoice(900);
		Check.Equal("historical invoice can still be downloaded", 200, Status(invoice));
		var pdf = (invoice as FileContentResult)?.FileContents ?? Array.Empty<byte>();
		Check.That("historical invoice is a real PDF", pdf.Length > 100 && pdf[0] == (byte)'%' && pdf[1] == (byte)'P');

		var bill = await fx.Db.GuestHouseBills.AsNoTracking().Include(x => x.LineItems).SingleAsync(x => x.Id == 900);
		Check.That("historical bill rows are unchanged", bill.NumberOfRooms == 2 && bill.TotalAmount == 4200m && bill.LineItems.Count == 2 && bill.LineItems.Any(l => l.Quantity == 4m));
		var row = await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(x => x.Id == 900);
		Check.That("historical booking row is unchanged", row.NumberOfRooms == 2 && row.TotalAmount == 4200m && row.BookingReference == "BKHISTORY");

		var viaFrontOffice = await fx.FrontOffice().GetBill(900);
		Check.Equal("front-office bill view for the historical bill still works", 200, Status(viaFrontOffice));
	}

	// ---- 7. duplicate submission -----------------------------------------------------------------------------

	private static async Task DuplicateSubmission()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-a");

		var first = await Book(c, Req(1, guest: "Dup Guest"));
		var second = await Book(c, Req(1, guest: "Dup Guest")); // identical, immediately (double click)
		Check.Equal("an identical rapid duplicate returns the same booking", first.BookingId, second.BookingId);
		Check.Equal("and the same reference", first.BookingReference, second.BookingReference);
		await Counts(fx, "after an identical duplicate", 1, 1, 0);

		var other = await Book(c, Req(2, guest: "Dup Guest")); // a deliberate second booking, another room
		Check.That("a booking for another room is still allowed", other.BookingId != first.BookingId);
		await Counts(fx, "after a deliberate second booking", 2, 2, 0);

		var sameRoomOtherGuest = await Book(c, Req(1, guest: "Different Guest"));
		Check.That("the same room for a different guest is still allowed", sameRoomOtherGuest.BookingId != first.BookingId);

		// Outside the duplicate window the same request is a new, deliberate booking.
		var row = await fx.Db.GuestHouseBookings.SingleAsync(x => x.Id == first.BookingId);
		row.CreatedAt = DateTime.Now.AddMinutes(-1);
		await fx.Db.SaveChangesAsync();
		var later = await Book(c, Req(1, guest: "Dup Guest"));
		Check.That("an identical request after the window is a new booking", later.BookingId != first.BookingId);

		// A different user is never treated as a duplicate of someone else's booking.
		var otherUser = await Book(fx.Booking("u-b"), Req(2, guest: "Dup Guest"));
		Check.That("another user's identical request creates their own booking", otherUser.BookingId != other.BookingId);
		var ownerOf = await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(x => x.Id == otherUser.BookingId);
		Check.Equal("and it belongs to that user", "u-b", ownerOf.CreatedBy ?? "");
	}

	// ---- 8. authorization and roles ---------------------------------------------------------------------------

	private static async Task<IActionResult?> RunFilter(AppDbContext db, ClaimsPrincipal? user, PagePermission permission)
	{
		var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
		var http = new DefaultHttpContext { RequestServices = services };
		if (user != null) http.User = user;
		var ctx = new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());
		await new GuestHouseBookingAccessAttribute(permission).OnAuthorizationAsync(ctx);
		return ctx.Result;
	}

	private static async Task Authorization()
	{
		using var fx = new Fx();

		// Existing role / designation rules (the filter itself is untouched; this pins it).
		var anon = await RunFilter(fx.Db, new ClaimsPrincipal(new ClaimsIdentity()), PagePermission.GuestHouseBooking);
		Check.Equal("unauthenticated caller gets 401", 401, anon == null ? 0 : Status(anon));
		Check.That("Admin passes the booking filter", await RunFilter(fx.Db, Fx.Principal("u-admin", AppRole.Admin), PagePermission.GuestHouseBooking) == null);
		Check.That("SuperAdmin passes the My Bookings filter", await RunFilter(fx.Db, Fx.Principal("u-admin", AppRole.SuperAdmin), PagePermission.MyBookings) == null);
		Check.That("Dealer with GuestHouseBooking passes the booking filter", await RunFilter(fx.Db, Fx.Principal("u-a", AppRole.Dealer), PagePermission.GuestHouseBooking) == null);
		Check.That("RM with MyBookings passes the My Bookings filter", await RunFilter(fx.Db, Fx.Principal("u-b", AppRole.RM), PagePermission.MyBookings) == null);
		var noMy = await RunFilter(fx.Db, Fx.Principal("u-bookingonly", AppRole.Dealer), PagePermission.MyBookings);
		Check.Equal("booking-only designation is refused on My Bookings endpoints (403)", 403, noMy == null ? 0 : Status(noMy));
		var noBook = await RunFilter(fx.Db, Fx.Principal("u-none", AppRole.Dealer), PagePermission.GuestHouseBooking);
		Check.Equal("designation without the permission is refused on booking endpoints (403)", 403, noBook == null ? 0 : Status(noBook));

		// Another user's booking cannot be read, paid for, cancelled or invoiced.
		var owner = fx.Booking("u-a");
		var mine = await Book(owner, Req(1, method: GuestHousePaymentMethod.PayAfterStay, guest: "Owner"));
		var intruder = fx.Booking("u-b");
		var before = await fx.Db.GuestHouseBookings.AsNoTracking().Include(x => x.Payments).SingleAsync(x => x.Id == mine.BookingId);

		Check.Equal("intruder cannot read booking details (404)", 404, Status(await intruder.GetBookingDetails(mine.BookingId)));
		Check.Equal("intruder cannot download the invoice (404)", 404, Status(await intruder.DownloadInvoice(mine.BookingId)));
		Check.Equal("intruder cannot cancel it (404)", 404, Status(await intruder.RequestCancellation(mine.BookingId, new CreateCancellationRequest { Reason = "x" })));
		Check.Equal("intruder cannot read its refund status (404)", 404, Status(await intruder.GetRefundStatus(mine.BookingId)));
		Check.Equal("intruder cannot create a payment order (403)", 403, Status(await intruder.CreatePaymentOrder(mine.BookingId)));
		Check.Equal("intruder cannot verify a payment (403)", 403, Status(await intruder.VerifyPayment(mine.BookingId, new VerifyPaymentRequest { RazorpayOrderId = "o", RazorpayPaymentId = "p", RazorpaySignature = "good-signature" })));
		Check.Equal("no Razorpay order was created for the intruder", 0, fx.Razor.OrdersCreated);
		Check.That("no cancellation was recorded", !await fx.Db.Set<GuestHouseBookingCancellation>().AnyAsync());

		var after = await fx.Db.GuestHouseBookings.AsNoTracking().Include(x => x.Payments).SingleAsync(x => x.Id == mine.BookingId);
		Check.That("the owner's booking is unchanged by the intruder",
			after.BookingStatus == before.BookingStatus && after.PaymentStatus == before.PaymentStatus && after.TotalAmount == before.TotalAmount
			&& after.Payments.Single().PaymentStatus == before.Payments.Single().PaymentStatus);

		var list = await intruder.GetMyBookings();
		Check.Equal("the intruder's My Bookings call succeeds", 200, Status(list));
		var listed = ((ObjectResult)list).Value as MyBookingsResponse;
		Check.That("and does not list the owner's booking", listed != null && listed.Bookings.All(b => b.BookingId != mine.BookingId));

		// The owner still has full access.
		Check.Equal("the owner can read their booking", 200, Status(await owner.GetBookingDetails(mine.BookingId)));
		Check.Equal("the owner can request cancellation", 200, Status(await owner.RequestCancellation(mine.BookingId, new CreateCancellationRequest { Reason = "Other" })));
	}

	// ---- 9. Employee ID / Dealer Code binding -----------------------------------------------------------------

	private static async Task<string?> SavedCode(Fx fx, int bookingId) =>
		(await fx.Db.GuestHouseBookingGuests.AsNoTracking().SingleAsync(g => g.GuestHouseBookingId == bookingId)).EmployeeOrDealerCode;

	private static async Task CodeBinding()
	{
		using var fx = new Fx();

		// What the Guest Details page pre-fills: the signed-in user's own code from the masters.
		var emp = await fx.Booking("u-emp", AppRole.RM).GetBookerIdentity();
		Check.Equal("employee: identity lookup succeeds", 200, Status(emp));
		Check.That("employee: bound to the Employee ID from EmployeeInformation", Prop<bool>(emp, "Bound") && Prop<string>(emp, "Kind") == "Employee" && Prop<string>(emp, "Code") == "EMP-0050");

		var dealer = await fx.Booking("u-dealer", AppRole.Dealer).GetBookerIdentity();
		Check.That("dealer: bound to the Dealer Code from DealerRegistration", Prop<bool>(dealer, "Bound") && Prop<string>(dealer, "Kind") == "Dealer" && Prop<string>(dealer, "Code") == "D-1234");
		Check.That("dealer: no employee code is used for a dealer", Prop<string>(dealer, "Code") != "EMP-0050");

		var emp2 = await fx.Booking("u-emp2", AppRole.RM).GetBookerIdentity();
		Check.Equal("employee with a stale inactive login: the active row's code is used (shared Employeelogin rule)", "EMP-0052", Prop<string>(emp2, "Code") ?? "");

		var admin = await fx.Booking("u-admin", AppRole.Admin).GetBookerIdentity();
		Check.That("Admin keeps the manual field (not bound)", !Prop<bool>(admin, "Bound") && Prop<string>(admin, "Code") == null);

		// The pre-filled code can be edited for this booking's guest (scenario 15 covers what it can NOT affect).
		var asEmployee = await Book(fx.Booking("u-emp", AppRole.RM), Req(1, guest: "Emp Guest", code: "EMP-9999"));
		Check.Equal("employee booking saves the guest code edited on the form", "EMP-9999", await SavedCode(fx, asEmployee.BookingId) ?? "");

		var asDealer = await Book(fx.Booking("u-dealer", AppRole.Dealer), Req(1, guest: "Dealer Guest", code: "D-5678"));
		Check.Equal("dealer booking saves the guest code edited on the form", "D-5678", await SavedCode(fx, asDealer.BookingId) ?? "");

		var noCode = await Book(fx.Booking("u-dealer", AppRole.Dealer), Req(2, guest: "Dealer Guest 2", code: null));
		Check.Equal("dealer booking with no code typed still gets the dealer's code", "D-1234", await SavedCode(fx, noCode.BookingId) ?? "");

		var manual = await Book(fx.Booking("u-admin", AppRole.Admin), Req(1, guest: "Admin Guest", code: "MANUAL-7"));
		Check.Equal("Admin booking keeps the code entered on the form (unchanged behaviour)", "MANUAL-7", await SavedCode(fx, manual.BookingId) ?? "");

		var bookingsBefore = await fx.Db.GuestHouseBookings.CountAsync();

		// A required code that cannot be found refuses the booking instead of saving a wrong value.
		var noLogin = await fx.Booking("u-nologin", AppRole.RM).CreateBooking(Req(1, guest: "X", code: "EMP-0050"));
		Check.Equal("employee with no employee login is refused (400)", 400, Status(noLogin));
		Check.That("the message says the Employee ID could not be found", (Prop<string>(noLogin, "Message") ?? "").Contains("Employee ID"));

		var blank = await fx.Booking("u-nocode", AppRole.RM).CreateBooking(Req(1, guest: "X", code: "EMP-0050"));
		Check.Equal("employee whose master record has a blank code is refused (400)", 400, Status(blank));

		var newDealer = await fx.Booking("u-newdealer", AppRole.Dealer).CreateBooking(Req(1, guest: "X", code: "D-1234"));
		Check.Equal("dealer without an approved Dealer Code is refused (400)", 400, Status(newDealer));
		var noDealer = await fx.Booking("u-nodealer", AppRole.Dealer).CreateBooking(Req(1, guest: "X", code: "D-1234"));
		Check.Equal("dealer with no dealer registration is refused (400)", 400, Status(noDealer));
		Check.That("the message says the Dealer Code could not be found", (Prop<string>(noDealer, "Message") ?? "").Contains("Dealer Code"));

		var identityMissing = await fx.Booking("u-nodealer", AppRole.Dealer).GetBookerIdentity();
		Check.That("identity lookup reports bound with no code and a message", Prop<bool>(identityMissing, "Bound") && Prop<string>(identityMissing, "Code") == null && !string.IsNullOrWhiteSpace(Prop<string>(identityMissing, "Message")));

		Check.Equal("none of the refused requests created a booking", bookingsBefore, await fx.Db.GuestHouseBookings.CountAsync());

		var endpoint = typeof(GuestHouseBookingController).GetMethod(nameof(GuestHouseBookingController.GetBookerIdentity));
		Check.Equal("the identity endpoint requires the GuestHouseBooking permission", (PagePermission?)PagePermission.GuestHouseBooking,
			endpoint?.GetCustomAttributes(typeof(GuestHouseBookingAccessAttribute), false).Cast<GuestHouseBookingAccessAttribute>().FirstOrDefault()?.Permission);
	}

	// ---- 15. Edited guest code ------------------------------------------------------------------------------
	// The Employee ID / Dealer Code pre-filled on Guest Details can be edited for the booking's guest. The edit
	// is stored on the guest row only: the booker, eligibility, masters and invoice GSTIN must not follow it.

	private static async Task EditedGuestCode()
	{
		using var fx = new Fx();
		// Real roles on the user rows (the invoice GSTIN rule reads the booker's role from here).
		fx.Db.Users.Add(new UserInfo { Id = "u-dealer", UserName = "user.dealer", DesignationId = 1, Name = "Dealer", Password = "x", UpdatedBy = "test", Role = AppRole.Dealer });
		fx.Db.Users.Add(new UserInfo { Id = "u-emp", UserName = "user.emp", DesignationId = 1, Name = "Emp", Password = "x", UpdatedBy = "test", Role = AppRole.RM });
		fx.Db.Users.Add(new UserInfo { Id = "u-admin", UserName = "user.admin", DesignationId = 1, Name = "Admin", Password = "x", UpdatedBy = "test", Role = AppRole.Admin });
		(await fx.Db.DealerRegistrations.SingleAsync(d => d.DealerCode == "D-1234")).GSTNumber = "33OWNGST0000Z1";
		(await fx.Db.DealerRegistrations.SingleAsync(d => d.DealerCode == "D-5678")).GSTNumber = "33OTHERGST000Z1";
		await fx.Db.SaveChangesAsync();

		async Task<string?> Gst(int bookingId) => (await GuestHouseBillPdfHelper.ResolveGuestCompanyAndGstAsync(fx.Db, bookingId)).GstNumber;

		// Dealer keeps the pre-filled own code: invoice GSTIN resolves exactly as before.
		var own = await Book(fx.Booking("u-dealer", AppRole.Dealer), Req(1, guest: "Own", code: "D-1234"));
		Check.Equal("dealer with own code: GSTIN from own dealer record (unchanged)", "33OWNGST0000Z1", await Gst(own.BookingId) ?? "");

		// Dealer edits the code to another dealer's code.
		var edited = await Book(fx.Booking("u-dealer", AppRole.Dealer), Req(2, guest: "Edited", code: "  D-5678 "));
		Check.Equal("edited code is saved (trimmed) on the guest row", "D-5678", await SavedCode(fx, edited.BookingId) ?? "");
		var editedRow = await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(b => b.Id == edited.BookingId);
		Check.Equal("booking owner is still the signed-in dealer", "u-dealer", editedRow.CreatedBy ?? "");
		Check.That("edited dealer booking is still a paying booking", !edited.IsFreeBooking);
		Check.That("another dealer's GSTIN is NOT put on the invoice", await Gst(edited.BookingId) == null);

		// Employee edits the code to a dealer's code: still free (role-based), no dealer GSTIN.
		var empEdited = await Book(fx.Booking("u-emp", AppRole.RM), Req(1, guest: "Emp Edited", code: "D-5678"));
		Check.That("employee with edited code is still an Employee Stay (role-based)", empEdited.IsFreeBooking);
		Check.That("employee's edited dealer code does not pull a dealer GSTIN", await Gst(empEdited.BookingId) == null);

		// Roles without a pre-filled code keep the previous behaviour (manual code -> dealer GSTIN lookup).
		var admin = await Book(fx.Booking("u-admin", AppRole.Admin), Req(2, guest: "Admin", code: "D-5678"));
		Check.Equal("Admin-entered code keeps the previous GSTIN lookup", "33OTHERGST000Z1", await Gst(admin.BookingId) ?? "");

		// Blank code from a bound user falls back to their own code.
		var blank = await Book(fx.Booking("u-dealer", AppRole.Dealer), Req(3, guest: "Blank", code: "   "));
		Check.Equal("blank code falls back to the booker's own code", "D-1234", await SavedCode(fx, blank.BookingId) ?? "");

		// Masters and identity are untouched.
		Check.Equal("dealer master code unchanged", "D-1234", (await fx.Db.DealerRegistrations.AsNoTracking().SingleAsync(d => d.UserTableId == "u-dealer")).DealerCode ?? "");
		Check.Equal("employee master code unchanged", "EMP-0050", (await fx.Db.EmployeeInformation.AsNoTracking().SingleAsync(e => e.Id == 50)).EmployeeCode ?? "");
		Check.Equal("identity endpoint still pre-fills the account's own code", "D-1234", Prop<string>(await fx.Booking("u-dealer", AppRole.Dealer).GetBookerIdentity(), "Code") ?? "");

		// The edited code cannot be used to obtain a free booking.
		var cheat = await fx.Booking("u-dealer", AppRole.Dealer).CreateBooking(Req(1, method: GuestHousePaymentMethod.Complimentary, guest: "Cheat", code: "EMP-0050"));
		Check.Equal("dealer typing an employee code still cannot get Complimentary", 400, Status(cheat));
	}

	// ---- 10. Complimentary eligibility drives the binding ----------------------------------------------------

	private static async Task CodeEligibility()
	{
		using var fx = new Fx();

		// Pin the binding to the EXISTING Complimentary policy: every role that books free is bound to an
		// Employee ID, Dealer is bound to a Dealer Code, and nothing else is bound. A user id with no
		// master data is used so a bound role reports "no code found" rather than a value.
		foreach (var role in Enum.GetValues<AppRole>())
		{
			var free = GuestHouseBookingPolicy.IsEmployeeFreeBooking(role);
			var expectedBound = free || role == AppRole.Dealer;
			var r = await fx.Booking("u-ghost", role).GetBookerIdentity();
			var kind = Prop<string>(r, "Kind");
			Check.That($"{role}: bound = {expectedBound} ({(free ? "Complimentary employee" : role == AppRole.Dealer ? "dealer" : "not eligible")})",
				Prop<bool>(r, "Bound") == expectedBound && kind == (free ? "Employee" : role == AppRole.Dealer ? "Dealer" : "NotBound"), kind);
		}

		// Eligible employee: free booking AND their own code. Ineligible: pays, and is not given an employee code.
		var eligible = await Book(fx.Booking("u-emp", AppRole.RM), Req(1, guest: "Eligible"));
		Check.That("eligible employee: booked free with their own Employee ID", eligible.IsFreeBooking && await SavedCode(fx, eligible.BookingId) == "EMP-0050");

		var dealerBooking = await Book(fx.Booking("u-dealer", AppRole.Dealer), Req(2, guest: "Dealer"));
		Check.That("dealer: not free (pays), saved with the Dealer Code", !dealerBooking.IsFreeBooking && await SavedCode(fx, dealerBooking.BookingId) == "D-1234");

		var farmer = await Book(fx.Booking("u-a", AppRole.Farmer), Req(2, guest: "Farmer", code: "ANY"));
		Check.That("farmer: not free and not bound (manual code kept)", !farmer.IsFreeBooking && await SavedCode(fx, farmer.BookingId) == "ANY");

		// A paying role cannot reach Complimentary just by naming an employee code.
		var cheat = await fx.Booking("u-a", AppRole.Dealer).CreateBooking(Req(1, method: GuestHousePaymentMethod.Complimentary, guest: "Cheat", code: "EMP-0050"));
		Check.Equal("a dealer cannot obtain Complimentary by using an employee code", 400, Status(cheat));
	}

	// ---- 11. persons / adults / children rule -----------------------------------------------------------------

	private static Task GuestCountRule()
	{
		string? V(int? p, int? a, int? c) => GuestHouseGuestCounts.Validate(p, a, c);
		const string over = GuestHouseGuestCounts.TotalExceedsPersonsMessage;

		Check.That("5 persons, 3 adults, 2 children (total 5) is valid", V(5, 3, 2) == null);
		Check.Equal("5 persons, 3 adults, 3 children (total 6) is invalid, with the required message", over, V(5, 3, 3) ?? "");
		Check.That("the message reads as specified", over == "Total adults and children cannot exceed the selected number of persons.");
		Check.That("total equal to persons is allowed (3 + 2 = 5)", V(5, 3, 2) == null && V(2, 2, 0) == null);
		Check.That("children left empty count as 0", V(5, 3, null) == null);
		Check.That("fewer guests than persons is allowed (rule is <=)", V(5, 1, 0) == null);
		Check.Equal("6 adults for 5 persons is invalid", over, V(5, 6, 0) ?? "");
		Check.That("empty persons is rejected", V(null, 1, 0) != null);
		Check.That("zero persons is rejected", V(0, 0, 0) != null);
		Check.That("negative persons is rejected", V(-1, 1, 0) != null);
		Check.That("empty adults is rejected", V(5, null, 0) != null);
		Check.That("zero adults is rejected (children cannot stay alone)", V(5, 0, 2) != null);
		Check.That("negative adults is rejected", V(5, -1, 0) != null);
		Check.That("negative children is rejected (cannot offset the total)", V(5, 3, -1) != null);
		Check.That("a large child count cannot overflow past the rule", V(5, 1, int.MaxValue - 1) != null);

		Check.That("non-numeric page values parse as 'not provided'", GuestHouseGuestCounts.Parse("abc") == null && GuestHouseGuestCounts.Parse("") == null && GuestHouseGuestCounts.Parse(null) == null);
		Check.Equal("numeric page values parse", 3, GuestHouseGuestCounts.Parse("3") ?? -1);
		Check.That("a non-numeric persons value is therefore rejected", V(GuestHouseGuestCounts.Parse("five"), 1, 0) != null);
		return Task.CompletedTask;
	}

	// ---- 12. guest counts through the booking API --------------------------------------------------------------

	private static async Task GuestCountsApi()
	{
		using var fx = new Fx();
		var c = fx.Booking("u-a");

		// Room 3 (capacity 6) so only the persons/adults/children rule is under test.
		var ok = await Book(c, Req(3, persons: 5, adults: 3, children: 2, guest: "Valid Party"));
		var row = await fx.Db.GuestHouseBookings.AsNoTracking().Include(b => b.Guests).SingleAsync(b => b.Id == ok.BookingId);
		Check.That("valid 5 / 3 / 2 is saved on the booking", row.NumberOfPersons == 5 && row.NumberOfAdults == 3 && row.NumberOfChildren == 2);
		Check.That("and on the guest row", row.Guests.Single().NumberOfPersons == 5 && row.Guests.Single().NumberOfAdults == 3 && row.Guests.Single().NumberOfChildren == 2);

		var before = await fx.Db.GuestHouseBookings.CountAsync();
		var over = await c.CreateBooking(Req(3, persons: 5, adults: 3, children: 3, guest: "Too Many"));
		Check.Equal("5 / 3 / 3 is refused by the API (400)", 400, Status(over));
		Check.Equal("with the specified message", GuestHouseGuestCounts.TotalExceedsPersonsMessage, Prop<string>(over, "Message") ?? "");

		Check.Equal("missing persons is refused", 400, Status(await c.CreateBooking(Req(3, persons: null, adults: 2, children: 0, guest: "A"))));
		Check.Equal("zero persons is refused", 400, Status(await c.CreateBooking(Req(3, persons: 0, adults: 0, children: 0, guest: "B"))));
		Check.Equal("missing adults is refused", 400, Status(await c.CreateBooking(Req(3, persons: 4, adults: null, children: 2, guest: "C"))));
		Check.Equal("zero adults is refused", 400, Status(await c.CreateBooking(Req(3, persons: 4, adults: 0, children: 2, guest: "D"))));
		Check.Equal("negative children is refused", 400, Status(await c.CreateBooking(Req(3, persons: 4, adults: 3, children: -1, guest: "E"))));
		Check.Equal("no booking was created by any refused request", before, await fx.Db.GuestHouseBookings.CountAsync());

		var noChildren = await Book(c, Req(3, persons: 4, adults: 3, children: null, guest: "No Children"));
		Check.Equal("children omitted is accepted and saved as 0", 0, (await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(b => b.Id == noChildren.BookingId)).NumberOfChildren ?? -1);

		// The room-capacity rule still applies on top (room 1: capacity 2).
		Check.Equal("room capacity is still enforced after the count rule passes", 400, Status(await c.CreateBooking(Req(1, persons: 4, adults: 4, children: 0, guest: "Over Capacity"))));
		Check.Equal("room capacity plus one extra bed is still allowed", 200, Status(await c.CreateBooking(Req(1, persons: 3, adults: 2, children: 1, extraBeds: 1, guest: "With Bed"))));
	}

	// ---- 14. "Complimentary" is now shown as "Employee Stay" (display only) -----------------------------------------

	private static async Task EmployeeStayWording()
	{
		// Internal identifiers are untouched: stored values, enum member names and the server marker.
		Check.Equal("enum member is still named Complimentary", "Complimentary", Enum.GetName(GuestHousePaymentMethod.Complimentary) ?? "");
		Check.Equal("and still stores 5", 5, (int)GuestHousePaymentMethod.Complimentary);
		Check.That("the other payment methods keep their stored values",
			(int)GuestHousePaymentMethod.Razorpay == 0 && (int)GuestHousePaymentMethod.UPI == 1 && (int)GuestHousePaymentMethod.Card == 2
			&& (int)GuestHousePaymentMethod.NetBanking == 3 && (int)GuestHousePaymentMethod.PayAfterStay == 4);

		using var fx = new Fx();

		// An eligible employee still books free; the record keeps the internal value, the screens say Employee Stay.
		var emp = await Book(fx.Booking("u-emp", AppRole.RM), Req(1, guest: "Employee Stay Guest"));
		var stored = await fx.Db.GuestHouseBookings.AsNoTracking().Include(b => b.Payments).SingleAsync(b => b.Id == emp.BookingId);
		Check.That("eligible employee: booked with no payment (total 0, Confirmed, Paid)", emp.IsFreeBooking && stored.TotalAmount == 0m && stored.BookingStatus == GuestHouseBookingStatus.Confirmed && stored.PaymentStatus == GuestHousePaymentStatus.Paid);
		Check.That("stored payment method is still the internal Complimentary value (5)", (int)stored.Payments.Single().PaymentMethod == 5 && stored.Payments.Single().TransactionId == "EMPLOYEE-FREE");

		var owner = fx.Booking("u-emp", AppRole.RM);
		var details = (BookingDetailsDto)((ObjectResult)await owner.GetBookingDetails(emp.BookingId)).Value!;
		Check.Equal("Booking Details shows the payment method as Employee Stay", "Employee Stay", details.PaymentMethod);
		Check.Equal("Booking Details amount is 0", 0m, details.TotalAmount ?? -1m);

		// A booking saved before the rename (old internal value) shows the new wording, with its bill and invoice intact.
		fx.Db.GuestHouseBookings.Add(new GuestHouseBooking
		{
			Id = 800, BookingReference = "BKOLDFREE", GuestHouseId = 1, GuestHouseRoomId = 1, CheckInDate = CheckIn, CheckOutDate = CheckOut,
			NumberOfNights = 2, NumberOfRooms = 1, RoomPrice = 0m, SubTotal = 0m, TaxAmount = 0m, TotalAmount = 0m,
			BookingStatus = GuestHouseBookingStatus.Completed, PaymentStatus = GuestHousePaymentStatus.Paid,
			CreatedBy = "u-emp", CreatedAt = DateTime.Now.AddDays(-20), UpdatedBy = "u-emp", UpdatedAt = DateTime.Now.AddDays(-20),
			Guests = { new GuestHouseBookingGuest { GuestName = "Old Free Guest", PhoneNumber = "7777777777", EmployeeOrDealerCode = "EMP-0050" } },
			Payments = { new GuestHouseBookingPayment { PaymentMethod = GuestHousePaymentMethod.Complimentary, PaymentStatus = GuestHousePaymentStatus.Paid, Amount = 0m, TransactionId = "EMPLOYEE-FREE", PaymentDate = DateTime.Now.AddDays(-20) } }
		});
		fx.Db.GuestHouseBills.Add(new GuestHouseBill
		{
			Id = 800, BillNumber = "BILL-OLDFREE", GuestHouseBookingId = 800, BookingReference = "BKOLDFREE", GuestName = "Old Free Guest",
			GuestHouseName = "Test Guest House", RoomType = "AC Deluxe", NumberOfNights = 2, NumberOfRooms = 1, PaymentMethod = GuestHousePaymentMethod.Complimentary,
			PaymentStatus = GuestHousePaymentStatus.Paid, CreatedBy = "u-emp", UpdatedBy = "u-emp",
			LineItems = { new GuestHouseBillLineItem { Description = "AC Deluxe - Room Charge", Quantity = 2m, Rate = 0m } }
		});
		await fx.Db.SaveChangesAsync();

		var old = await owner.GetBookingDetails(800);
		Check.Equal("an old booking (stored value 5) displays as Employee Stay", "Employee Stay", ((BookingDetailsDto)((ObjectResult)old).Value!).PaymentMethod);
		Check.Equal("its invoice still downloads", 200, Status(await owner.DownloadInvoice(800)));
		var mine = ((ObjectResult)await owner.GetMyBookings()).Value as MyBookingsResponse;
		Check.That("My Bookings lists both employee bookings", mine != null && mine.Bookings.Any(b => b.BookingId == 800) && mine.Bookings.Any(b => b.BookingId == emp.BookingId));
		Check.Equal("cancellation of an Employee Stay booking still works", 200, Status(await owner.RequestCancellation(emp.BookingId, new CreateCancellationRequest { Reason = "Other" })));

		// The invoice's payment label (private helper) uses the new wording and leaves the other methods alone.
		var label = typeof(SpicAPI.Services.GuestHouseInvoicePdfBuilder).GetMethod("PaymentMethodLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		string? Label(GuestHousePaymentMethod m) => label?.Invoke(null, new object[] { (int)m }) as string;
		Check.Equal("invoice label for the employee option is Employee Stay", "Employee Stay", Label(GuestHousePaymentMethod.Complimentary) ?? "");
		Check.That("invoice labels for the other methods are unchanged", Label(GuestHousePaymentMethod.PayAfterStay) == "Credit" && Label(GuestHousePaymentMethod.Razorpay) == "Razorpay"
			&& Label(GuestHousePaymentMethod.UPI) == "UPI" && Label(GuestHousePaymentMethod.Card) == "Card" && Label(GuestHousePaymentMethod.NetBanking) == "Net Banking");

		// Other payment methods and their display names are exactly as before.
		var online = await Book(fx.Booking("u-a", AppRole.Dealer), Req(2, guest: "Online Guest"));
		var payLater = await Book(fx.Booking("u-a", AppRole.Dealer), Req(1, method: GuestHousePaymentMethod.PayAfterStay, guest: "Later Guest"));
		string Method(int id) => ((BookingDetailsDto)((ObjectResult)fx.Booking("u-a", AppRole.Dealer).GetBookingDetails(id).GetAwaiter().GetResult()).Value!).PaymentMethod;
		Check.Equal("Online (Razorpay) still displays as Razorpay", "Razorpay", Method(online.BookingId));
		Check.Equal("Pay After Stay still displays as Pay After Stay", "Pay After Stay", Method(payLater.BookingId));
		Check.That("the dealer's bookings are not free and not Employee Stay", !online.IsFreeBooking && !payLater.IsFreeBooking);

		// Ineligible roles still cannot reach it.
		Check.Equal("a Dealer cannot request the employee option (400)", 400, Status(await fx.Booking("u-a", AppRole.Dealer).CreateBooking(Req(2, method: GuestHousePaymentMethod.Complimentary, guest: "Nope"))));
		Check.Equal("a Farmer cannot request the employee option (400)", 400, Status(await fx.Booking("u-a", AppRole.Farmer).CreateBooking(Req(2, method: GuestHousePaymentMethod.Complimentary, guest: "Nope 2"))));
		Check.Equal("an Admin cannot request the employee option (400)", 400, Status(await fx.Booking("u-admin", AppRole.Admin).CreateBooking(Req(2, method: GuestHousePaymentMethod.Complimentary, guest: "Nope 3"))));
		Check.That("only roles in the existing policy are eligible (Dealer, Admin, SuperAdmin, Farmer are not)",
			!GuestHouseBookingPolicy.IsEmployeeFreeBooking(AppRole.Dealer) && !GuestHouseBookingPolicy.IsEmployeeFreeBooking(AppRole.Admin)
			&& !GuestHouseBookingPolicy.IsEmployeeFreeBooking(AppRole.SuperAdmin) && !GuestHouseBookingPolicy.IsEmployeeFreeBooking(AppRole.Farmer)
			&& GuestHouseBookingPolicy.IsEmployeeFreeBooking(AppRole.RM));

		// No user-facing "Complimentary" text is left on the Guest House screens (identifiers and comments excluded).
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln"))) dir = dir.Parent;
		var pages = Path.Combine(dir!.FullName, "SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages");
		foreach (var file in new[] { "Payment.razor", "BookingPreview.razor", "BookingDetails.razor", "MyBookings.razor", "BillList.razor", "FrontOffice.razor", "GenerateBill.razor" })
		{
			var src = File.ReadAllText(Path.Combine(pages, file));
			var shown = System.Text.RegularExpressions.Regex.Matches(src, "(\"[^\"\\n]*(?<![A-Za-z])Complimentary(?![A-Za-z])[^\"\\n]*\"|>[^<>@\\n]*(?<![A-Za-z])Complimentary(?![A-Za-z])[^<>\\n]*<|\\(Complimentary\\))");
			Check.Equal($"{file}: no user-facing 'Complimentary' text", 0, shown.Count);
		}
		var payment = File.ReadAllText(Path.Combine(pages, "Payment.razor"));
		Check.That("the Payment page shows the Employee Stay label, summary pill and confirmation", payment.Contains(">Employee Stay</div>") && payment.Contains(">Employee Stay</h3>") && payment.Contains(">Employee Stay</span>") && payment.Contains("Employee Stay booking - payment can be made after your stay."));
		Check.That("no 'free' / 'no payment required' wording on the employee booking screens",
			!System.Text.RegularExpressions.Regex.IsMatch(payment + File.ReadAllText(Path.Combine(pages, "BookingPreview.razor")),
				"(?i)(free of charge|no payment (is )?required|Free booking for|\\(Employee Stay\\))"));
	}

	// ---- 13. Guest Details page wiring ---------------------------------------------------------------------------

	// The page is a Razor component and the harness has no component host, so these are STRUCTURAL checks of
	// the markup/code (that each rule is wired in), not a behavioural run of the page. The behaviour they
	// rely on is covered by scenarios 9 to 12, which exercise the same shared rule and the same API.
	private static Task GuestDetailsWiring()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		var page = File.ReadAllText(Path.Combine(dir!.FullName, "SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", "GuestDetails.razor"));

		int Count(string needle) => System.Text.RegularExpressions.Regex.Matches(page, System.Text.RegularExpressions.Regex.Escape(needle)).Count;

		Check.Equal("persons, adults and children each revalidate when changed", 3, Count("@bind:after=\"ValidateGuestCounts\""));
		Check.That("Continue is disabled while the guest-count rule fails", page.Contains("@onclick=\"ContinueToPreview\" disabled=\"@(_guestCountError != null)\""));
		Check.That("the shared rule is used (page and API apply the same check)", page.Contains("SPIC.Core.Entities.GuestHouseGuestCounts.Validate("));
		Check.That("the rule is re-run in ValidateForm before navigating to the preview", page.Contains("_guestCountError = SPIC.Core.Entities.GuestHouseGuestCounts.Validate("));
		Check.That("the bound code is shown read-only until Edit", page.Contains("@if (_codeLocked)") && System.Text.RegularExpressions.Regex.IsMatch(page, "@bind=\"_form\\.EmployeeOrDealerCode\"\\s+readonly"));
		Check.That("Edit / Save / Cancel are wired", page.Contains("@onclick=\"StartCodeEdit\"") && page.Contains("@onclick=\"SaveCodeEdit\"") && page.Contains("@onclick=\"CancelCodeEdit\""));
		Check.That("Edit works on a draft; Save copies it into the form", page.Contains("@bind=\"_codeDraft\"") && page.Contains("_form.EmployeeOrDealerCode = value;"));
		Check.That("Cancel discards the draft without touching the form value", System.Text.RegularExpressions.Regex.IsMatch(page, @"void CancelCodeEdit\(\)\s*\{(?:(?!_form\.EmployeeOrDealerCode)[^}])*\}"));
		Check.That("Save keeps the required-field rule", page.Contains("_codeEditError = \"Employee ID / Dealer Code is required.\""));
		Check.That("Continue is blocked while an edit is unsaved", page.Contains("if (_codeEditing) return"));
		Check.That("the code comes from the booker-identity endpoint", page.Contains("api/GuestHouseBooking/booker-identity"));
		Check.That("a missing required code blocks Continue", page.Contains("if (_identityError != null) return _identityError;"));
		Check.That("a failed identity lookup is retried before continuing", page.Contains("if (!_identityLoaded)"));
		Check.That("the counts saved for the preview use the validated values", page.Contains("BookingState.NumberOfChildren = SPIC.Core.Entities.GuestHouseGuestCounts.Parse(_form.NumberOfChildren) ?? 0;"));
		return Task.CompletedTask;
	}
}

