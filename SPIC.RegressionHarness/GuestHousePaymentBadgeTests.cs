using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Payments;
using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// The My Bookings / Booking Details payment badge. An Employee Stay (Complimentary) booking is stored
/// Confirmed + Paid (Check-Out, billing and invoices depend on that) but displays "Booked" through Confirmed / CheckedIn / Completed and "Paid" only once its bill is generated; everything else
/// keeps its stored PaymentStatus text. Runs the REAL GuestHouseBookingController list / details endpoints
/// and the same GuestHousePaymentBadge rule the two pages call.
/// </summary>
internal static class GuestHousePaymentBadgeTests
{
	public static void Run()
	{
		Console.WriteLine("GuestHousePaymentBadge");
		Step("1. display rule", Rule);
		Step("2. My Bookings API: stored status unchanged, method returned", MyBookingsApi);
		Step("3. Booking Details API: stored status unchanged, invoice rule untouched", DetailsApi);
		Step("4. pages use the rule", PagesUseTheRule);
		Step("5. Front Office list: method returned, stored status unchanged", FrontOfficeList);
		Step("6. checked-in and completed employee stays on My Bookings / Booking Details", CheckedInAndCompleted);
	}

	private static async Task FrontOfficeList()
	{
		using var fx = new Fx();
		var fo = fx.FrontOffice();
		var rows = (List<FrontOfficeActiveBookingDto>)((ObjectResult)await fo.GetActiveBookings()).Value!;
		string Shown(int id)
		{
			var r = rows.Single(b => b.BookingId == id);
			return GuestHousePaymentBadge.Text(r.PaymentMethod.HasValue ? MethodName(r.PaymentMethod.Value) : null, r.BookingStatus.ToString(), r.PaymentStatus.ToString(), false);
		}

		Check.Equal("Front Office: employee Confirmed shows Booked", "Booked", Shown(Fx.EmpId));
		Check.Equal("Front Office: employee CheckedIn shows Booked", "Booked", Shown(Fx.CheckedInId));
		Check.Equal("Front Office: dealer Razorpay Paid stays Paid", "Paid", Shown(Fx.DealerPaidId));
		Check.Equal("Front Office: dealer Pay After Stay stays Pending", "Pending", Shown(Fx.DealerPasId));
		Check.Equal("Front Office: API still returns PaymentStatus Paid for the employee booking", GuestHousePaymentStatus.Paid, rows.Single(b => b.BookingId == Fx.EmpId).PaymentStatus);
		Check.Equal("Front Office: method comes from the booking's payment row", GuestHousePaymentMethod.Complimentary, rows.Single(b => b.BookingId == Fx.EmpId).PaymentMethod);
	}

	private static async Task CheckedInAndCompleted()
	{
		using var fx = new Fx();
		var c = fx.Controller("u-x");
		var list = ((MyBookingsResponse)((ObjectResult)await c.GetMyBookings()).Value!).Bookings;
		string Mine(int id) { var b = list.Single(x => x.BookingId == id); return GuestHousePaymentBadge.Text(b.PaymentMethod, b.BookingStatus, b.PaymentStatus, b.HasInvoice); }
		async Task<string> Details(int id) { var d = (BookingDetailsDto)((ObjectResult)await c.GetBookingDetails(id)).Value!; return GuestHousePaymentBadge.Text(d.PaymentMethod, d.BookingStatus, d.PaymentStatus, d.HasInvoice); }

		Check.Equal("My Bookings: employee Confirmed shows Booked", "Booked", Mine(Fx.EmpId));
		Check.Equal("My Bookings: employee CheckedIn shows Booked", "Booked", Mine(Fx.CheckedInId));
		Check.Equal("My Bookings: employee Completed (no bill) shows Booked", "Booked", Mine(Fx.CompletedId));
		Check.Equal("Booking Details: employee Confirmed shows Booked", "Booked", await Details(Fx.EmpId));
		Check.Equal("Booking Details: employee CheckedIn shows Booked", "Booked", await Details(Fx.CheckedInId));
		Check.Equal("Booking Details: employee Completed (no bill) shows Booked", "Booked", await Details(Fx.CompletedId));

		// Generate Bill through the real endpoint, then the same bookings re-read.
		var fo = fx.FrontOffice();
		var gen = await fo.GenerateBill(new GenerateBillRequest
		{
			BookingId = Fx.CompletedId, PaymentReceived = true, PaymentMethod = 5,
			LineItems = { new BillLineItemDto { Description = "AC - Room Charge", Quantity = 1m, Rate = 0m } }
		});
		Check.That("Generate Bill succeeded", gen is OkObjectResult || (gen is ObjectResult { StatusCode: 200 }), (gen as ObjectResult)?.Value?.ToString());
		Check.That("a GuestHouseBill now exists", await fx.Db.GuestHouseBills.AnyAsync(b => b.GuestHouseBookingId == Fx.CompletedId));
		list = ((MyBookingsResponse)((ObjectResult)await fx.Controller("u-x").GetMyBookings()).Value!).Bookings;
		Check.Equal("My Bookings: employee after Generate Bill shows Paid", "Paid", Mine(Fx.CompletedId));
		Check.Equal("Booking Details: employee after Generate Bill shows Paid", "Paid", await Details(Fx.CompletedId));
		Check.Equal("other employee bookings are unaffected by that bill", "Booked", Mine(Fx.EmpId));

		var stored = await fx.Db.GuestHouseBookings.Where(b => b.Id == Fx.EmpId || b.Id == Fx.CheckedInId || b.Id == Fx.CompletedId).ToListAsync();
		Check.That("all three employee bookings are still stored Paid", stored.Count == 3 && stored.All(b => b.PaymentStatus == GuestHousePaymentStatus.Paid));
	}

	private static void Step(string name, Func<Task> body)
	{
		Console.WriteLine($" {name}");
		try { body().GetAwaiter().GetResult(); }
		catch (Exception ex) { Check.That($"{name} ran to completion", false, $"{ex.GetType().Name}: {ex.Message}"); }
	}

	private static string Text(GuestHousePaymentMethod method, GuestHouseBookingStatus booking, GuestHousePaymentStatus payment, bool billGenerated = false) =>
		GuestHousePaymentBadge.Text(MethodName(method), booking == GuestHouseBookingStatus.PendingPayment ? "Pending Payment" : booking.ToString(), payment switch
		{
			GuestHousePaymentStatus.PartiallyRefunded => "Partially Refunded",
			_ => payment.ToString()
		}, billGenerated);

	private static string MethodName(GuestHousePaymentMethod m) => m switch
	{
		GuestHousePaymentMethod.NetBanking => "Net Banking",
		GuestHousePaymentMethod.PayAfterStay => "Pay After Stay",
		GuestHousePaymentMethod.Complimentary => "Employee Stay",
		_ => m.ToString()
	};

	private static Task Rule()
	{
		var emp = GuestHousePaymentMethod.Complimentary;
		Check.Equal("Complimentary + Confirmed + Paid", "Booked", Text(emp, GuestHouseBookingStatus.Confirmed, GuestHousePaymentStatus.Paid));
		Check.Equal("Complimentary + CheckedIn + Paid", "Booked", Text(emp, GuestHouseBookingStatus.CheckedIn, GuestHousePaymentStatus.Paid));
		Check.Equal("Complimentary + Completed + Paid, no bill yet", "Booked", Text(emp, GuestHouseBookingStatus.Completed, GuestHousePaymentStatus.Paid));
		Check.Equal("Complimentary + Completed + Paid, bill generated", "Paid", Text(emp, GuestHouseBookingStatus.Completed, GuestHousePaymentStatus.Paid, billGenerated: true));
		Check.Equal("Dealer Razorpay + Confirmed + Paid", "Paid", Text(GuestHousePaymentMethod.Razorpay, GuestHouseBookingStatus.Confirmed, GuestHousePaymentStatus.Paid));
		Check.Equal("Dealer Pay After Stay + Confirmed + Pending", "Pending", Text(GuestHousePaymentMethod.PayAfterStay, GuestHouseBookingStatus.Confirmed, GuestHousePaymentStatus.Pending));
		Check.Equal("Dealer Pay After Stay + CheckedIn + Paid is a real payment", "Paid", Text(GuestHousePaymentMethod.PayAfterStay, GuestHouseBookingStatus.CheckedIn, GuestHousePaymentStatus.Paid));
		Check.Equal("Razorpay + PendingPayment + Pending", "Pending", Text(GuestHousePaymentMethod.Razorpay, GuestHouseBookingStatus.PendingPayment, GuestHousePaymentStatus.Pending));
		Check.Equal("Razorpay + Failed", "Failed", Text(GuestHousePaymentMethod.Razorpay, GuestHouseBookingStatus.Cancelled, GuestHousePaymentStatus.Failed));
		Check.Equal("Razorpay + Refunded", "Refunded", Text(GuestHousePaymentMethod.Razorpay, GuestHouseBookingStatus.Cancelled, GuestHousePaymentStatus.Refunded));
		Check.Equal("Razorpay + Partially Refunded", "Partially Refunded", Text(GuestHousePaymentMethod.Razorpay, GuestHouseBookingStatus.Cancelled, GuestHousePaymentStatus.PartiallyRefunded));
		Check.Equal("Complimentary + Cancelled keeps its stored text", "Paid", Text(emp, GuestHouseBookingStatus.Cancelled, GuestHousePaymentStatus.Paid));
		Check.Equal("Complimentary + Refunded keeps its stored text", "Refunded", Text(emp, GuestHouseBookingStatus.Cancelled, GuestHousePaymentStatus.Refunded));
		Check.Equal("no payment method: unchanged", "Paid", GuestHousePaymentBadge.Text(null, "Confirmed", "Paid", false));
		Check.Equal("empty payment method: unchanged", "Paid", GuestHousePaymentBadge.Text("", "Confirmed", "Paid", false));
		return Task.CompletedTask;
	}

	private static async Task MyBookingsApi()
	{
		using var fx = new Fx();
		var list = ((MyBookingsResponse)((ObjectResult)await fx.Controller("u-x").GetMyBookings()).Value!).Bookings;

		var emp = list.Single(b => b.BookingId == Fx.EmpId);
		Check.Equal("employee booking still returns PaymentStatus Paid", "Paid", emp.PaymentStatus);
		Check.Equal("employee booking returns method Employee Stay", "Employee Stay", emp.PaymentMethod ?? "");
		Check.Equal("employee booking displays Booked", "Booked", GuestHousePaymentBadge.Text(emp.PaymentMethod, emp.BookingStatus, emp.PaymentStatus, emp.HasInvoice));

		var paid = list.Single(b => b.BookingId == Fx.DealerPaidId);
		Check.Equal("dealer booking displays Paid", "Paid", GuestHousePaymentBadge.Text(paid.PaymentMethod, paid.BookingStatus, paid.PaymentStatus, paid.HasInvoice));

		var pas = list.Single(b => b.BookingId == Fx.DealerPasId);
		Check.Equal("dealer Pay After Stay stays Pending", "Pending", GuestHousePaymentBadge.Text(pas.PaymentMethod, pas.BookingStatus, pas.PaymentStatus, pas.HasInvoice));

		var cancelled = list.Single(b => b.BookingId == Fx.CancelledId);
		Check.Equal("cancelled employee booking keeps its stored text", "Refunded", GuestHousePaymentBadge.Text(cancelled.PaymentMethod, cancelled.BookingStatus, cancelled.PaymentStatus, cancelled.HasInvoice));
	}

	private static async Task DetailsApi()
	{
		using var fx = new Fx();
		var c = fx.Controller("u-x");

		var emp = (BookingDetailsDto)((ObjectResult)await c.GetBookingDetails(Fx.EmpId)).Value!;
		Check.Equal("details still return PaymentStatus Paid", "Paid", emp.PaymentStatus);
		Check.Equal("details return Employee Stay", "Employee Stay", emp.PaymentMethod);
		Check.Equal("details display Booked", "Booked", GuestHousePaymentBadge.Text(emp.PaymentMethod, emp.BookingStatus, emp.PaymentStatus, emp.HasInvoice));

		var pas = (BookingDetailsDto)((ObjectResult)await c.GetBookingDetails(Fx.DealerPasId)).Value!;
		Check.Equal("details: dealer Pay After Stay stays Pending", "Pending", GuestHousePaymentBadge.Text(pas.PaymentMethod, pas.BookingStatus, pas.PaymentStatus, pas.HasInvoice));

		var paid = (BookingDetailsDto)((ObjectResult)await c.GetBookingDetails(Fx.DealerPaidId)).Value!;
		Check.Equal("details: dealer Paid stays Paid", "Paid", GuestHousePaymentBadge.Text(paid.PaymentMethod, paid.BookingStatus, paid.PaymentStatus, paid.HasInvoice));

		// Stored data untouched.
		var stored = await fx.Db.GuestHouseBookings.Include(b => b.Payments).SingleAsync(b => b.Id == Fx.EmpId);
		Check.That("employee booking is still stored Confirmed + Paid + Complimentary / EMPLOYEE-FREE / 0",
			stored.BookingStatus == GuestHouseBookingStatus.Confirmed && stored.PaymentStatus == GuestHousePaymentStatus.Paid
			&& stored.Payments.Single().PaymentMethod == GuestHousePaymentMethod.Complimentary
			&& stored.Payments.Single().TransactionId == "EMPLOYEE-FREE" && stored.Payments.Single().Amount == 0m);
	}

	private static Task PagesUseTheRule()
	{
		var pages = RepoFile("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages");
		var mine = File.ReadAllText(Path.Combine(pages, "MyBookings.razor"));
		var details = File.ReadAllText(Path.Combine(pages, "BookingDetails.razor"));
		Check.That("My Bookings uses GuestHousePaymentBadge", mine.Contains("GuestHousePaymentBadge.Text("));
		Check.That("Booking Details uses GuestHousePaymentBadge", details.Contains("GuestHousePaymentBadge.Text("));
		Check.That("Front Office uses GuestHousePaymentBadge", File.ReadAllText(Path.Combine(pages, "FrontOffice.razor")).Contains("GuestHousePaymentBadge.Text("));
		Check.That("invoice button still keys off the stored Paid status",
			details.Contains("_booking.HasInvoice && string.Equals(_booking.PaymentStatus, \"Paid\""));
		return Task.CompletedTask;
	}

	private static string RepoFile(params string[] parts)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		return Path.Combine(new[] { dir?.FullName ?? throw new InvalidOperationException("SpicOne.sln not found") }.Concat(parts).ToArray());
	}

	private sealed class StubRazorpay : IRazorpayService
	{
		public Task<RazorpayOrderResult> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken cancellationToken = default) =>
			Task.FromResult(new RazorpayOrderResult { Success = true, OrderId = "order_test", AmountInPaise = amountInPaise, RawResponse = "{}" });
		public bool VerifySignature(string orderId, string paymentId, string signature) => false;
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
		public const int EmpId = 1, DealerPaidId = 2, DealerPasId = 3, CancelledId = 4, CheckedInId = 5, CompletedId = 6;
		public readonly AppDbContext Db;

		public Fx()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
				.Options;
			Db = new AppDbContext(options);
			Db.GuestHouses.Add(new GuestHouse { Id = 1, Name = "Test Guest House", IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 1, GuestHouseId = 1, RoomType = "AC", RoomNumber = "101", PricePerNight = 1000m, Capacity = 2, AvailableQuantity = 3, IsActive = true });

			Add(EmpId, GuestHouseBookingStatus.Confirmed, GuestHousePaymentStatus.Paid, GuestHousePaymentMethod.Complimentary, 0m, "EMPLOYEE-FREE");
			Add(DealerPaidId, GuestHouseBookingStatus.Confirmed, GuestHousePaymentStatus.Paid, GuestHousePaymentMethod.Razorpay, 1050m, "pay_1");
			Add(DealerPasId, GuestHouseBookingStatus.Confirmed, GuestHousePaymentStatus.Pending, GuestHousePaymentMethod.PayAfterStay, 1050m, null);
			Add(CancelledId, GuestHouseBookingStatus.Cancelled, GuestHousePaymentStatus.Refunded, GuestHousePaymentMethod.Complimentary, 0m, "EMPLOYEE-FREE");
			Add(CheckedInId, GuestHouseBookingStatus.CheckedIn, GuestHousePaymentStatus.Paid, GuestHousePaymentMethod.Complimentary, 0m, "EMPLOYEE-FREE");
			Add(CompletedId, GuestHouseBookingStatus.Completed, GuestHousePaymentStatus.Paid, GuestHousePaymentMethod.Complimentary, 0m, "EMPLOYEE-FREE");
			Db.SaveChanges();
		}

		private void Add(int id, GuestHouseBookingStatus status, GuestHousePaymentStatus payment, GuestHousePaymentMethod method, decimal amount, string? txn)
		{
			var b = new GuestHouseBooking
			{
				Id = id, BookingReference = $"BKB{id:000}", GuestHouseId = 1, GuestHouseRoomId = 1,
				CheckInDate = DateTime.Today.AddDays(5), CheckOutDate = DateTime.Today.AddDays(6),
				NumberOfNights = 1, NumberOfRooms = 1, NumberOfPersons = 1, RoomPrice = amount, SubTotal = amount, TaxAmount = 0m, TotalAmount = amount,
				BookingStatus = status, PaymentStatus = payment, CreatedBy = "u-x", CreatedAt = DateTime.Now.AddDays(-1)
			};
			b.Guests.Add(new GuestHouseBookingGuest { GuestName = "Guest", PhoneNumber = "9999999999", NumberOfPersons = 1 });
			b.Payments.Add(new GuestHouseBookingPayment { PaymentMethod = method, PaymentStatus = payment, Amount = amount, TransactionId = txn });
			Db.GuestHouseBookings.Add(b);
		}

		public GuestHouseBookingController Controller(string userId)
		{
			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, userId), new Claim(ClaimTypes.Role, nameof(AppRole.Dealer))
			};
			return new GuestHouseBookingController(Db, new StubEnv(), new StubRazorpay(), Options.Create(new RazorpayOptions()),
				Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseBookingController>.Instance)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", ClaimTypes.Name, ClaimTypes.Role)) }
				}
			};
		}

		public GuestHouseFrontOfficeController FrontOffice() =>
			new(Db, Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseFrontOfficeController>.Instance)
			{
				BusinessDate = () => DateTime.Today,
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext
					{
						User = new ClaimsPrincipal(new ClaimsIdentity(new[]
						{
							new Claim(ClaimTypes.NameIdentifier, "u-admin"), new Claim(ClaimTypes.Name, "u-admin"), new Claim(ClaimTypes.Role, nameof(AppRole.Admin))
						}, "test", ClaimTypes.Name, ClaimTypes.Role))
					}
				}
			};

		public void Dispose() => Db.Dispose();
	}
}
