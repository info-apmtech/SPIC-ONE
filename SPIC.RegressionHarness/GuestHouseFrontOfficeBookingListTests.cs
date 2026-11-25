using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Payments;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// Front Office Booking Details list (GET bookings/active): one row per booking for every current and
/// future Confirmed / CheckedIn stay, split into Today / Upcoming on the India business date.
///
/// Calls the REAL GuestHouseFrontOfficeController against a private EF Core InMemory database per
/// scenario. The business date is pinned to the machine's date so it agrees with GetRooms, which still
/// uses the server clock and must be unaffected.
/// </summary>
internal static class GuestHouseFrontOfficeBookingListTests
{
	private static readonly DateTime T = DateTime.Today;

	public static void Run()
	{
		Console.WriteLine("GuestHouseFrontOfficeBookingList");

		Step("1. status / date inclusion and exclusion", Inclusion);
		Step("2. Today / Upcoming periods and boundaries", Periods);
		Step("3. one row per booking, real room numbers only", RowsAndRooms);
		Step("4. room grid (GetRooms) unchanged by future bookings", RoomGridUnchanged);
		Step("5. check-in / check-out / Generate Bill flow preserved", Workflow);
		Step("6. India business date", IndiaDate);
	}

	private static void Step(string name, Func<Task> body)
	{
		Console.WriteLine($" {name}");
		try { body().GetAwaiter().GetResult(); }
		catch (Exception ex) { Check.That($"{name} ran to completion", false, $"{ex.GetType().Name}: {ex.Message}"); }
	}

	private static async Task<List<FrontOfficeActiveBookingDto>> List(GuestHouseFrontOfficeController c) =>
		(List<FrontOfficeActiveBookingDto>)((ObjectResult)await c.GetActiveBookings()).Value!;

	// ---- scenarios -------------------------------------------------------------------------------------

	private static async Task Inclusion()
	{
		using var fx = new Fx();
		var today = fx.Add(GuestHouseBookingStatus.Confirmed, T, T.AddDays(2));
		var arrivedNotIn = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(-1), T.AddDays(1));
		var future = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(3), T.AddDays(5));
		var inHouse = fx.Add(GuestHouseBookingStatus.CheckedIn, T.AddDays(-1), T.AddDays(1), allocated: "101");
		var overdue = fx.Add(GuestHouseBookingStatus.CheckedIn, T.AddDays(-4), T.AddDays(-2), allocated: "102");

		var noShow = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(-3), T.AddDays(-1));
		var draft = fx.Add(GuestHouseBookingStatus.Draft, T.AddDays(1), T.AddDays(2));
		var liveHold = fx.Add(GuestHouseBookingStatus.PendingPayment, T.AddDays(1), T.AddDays(2), createdAt: DateTime.Now);
		var cancelled = fx.Add(GuestHouseBookingStatus.Cancelled, T.AddDays(2), T.AddDays(3));
		var completed = fx.Add(GuestHouseBookingStatus.Completed, T.AddDays(-2), T);

		var ids = (await List(fx.FrontOffice())).Select(r => r.BookingId).ToHashSet();

		Check.That("Confirmed stay starting today is listed", ids.Contains(today.Id));
		Check.That("Confirmed stay already started (not checked in) is listed", ids.Contains(arrivedNotIn.Id));
		Check.That("future Confirmed booking is listed (was hidden before)", ids.Contains(future.Id));
		Check.That("checked-in guest is listed", ids.Contains(inHouse.Id));
		Check.That("overdue checked-in guest is listed", ids.Contains(overdue.Id));

		Check.That("no-show (check-out passed, never checked in) is excluded", !ids.Contains(noShow.Id));
		Check.That("Draft is excluded", !ids.Contains(draft.Id));
		Check.That("PendingPayment within its live hold is excluded", !ids.Contains(liveHold.Id));
		Check.That("Cancelled is excluded", !ids.Contains(cancelled.Id));
		Check.That("Completed is excluded", !ids.Contains(completed.Id));
		Check.Equal("exactly the five active bookings", 5, ids.Count);

		var stored = await fx.Db.GuestHouseBookings.AsNoTracking().SingleAsync(b => b.Id == noShow.Id);
		Check.That("no-show booking is left untouched (status/payment)",
			stored.BookingStatus == GuestHouseBookingStatus.Confirmed && stored.PaymentStatus == GuestHousePaymentStatus.Paid);
	}

	private static async Task Periods()
	{
		using var fx = new Fx();
		var startsToday = fx.Add(GuestHouseBookingStatus.Confirmed, T, T.AddDays(1));
		var startsTomorrow = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(1), T.AddDays(2));
		var checkOutToday = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(-2), T);
		var inHouse = fx.Add(GuestHouseBookingStatus.CheckedIn, T.AddDays(-1), T.AddDays(1), allocated: "101");

		var rows = await List(fx.FrontOffice());
		FrontOfficeBookingPeriod? P(GuestHouseBooking b) => rows.SingleOrDefault(r => r.BookingId == b.Id)?.Period;

		Check.That("check-in today => Today", P(startsToday) == FrontOfficeBookingPeriod.Today);
		Check.That("check-in tomorrow => Upcoming", P(startsTomorrow) == FrontOfficeBookingPeriod.Upcoming);
		Check.That("checked-in guest => Today", P(inHouse) == FrontOfficeBookingPeriod.Today);
		Check.That("Confirmed stay whose check-out date is today (not checked in) is excluded", P(checkOutToday) == null);
		Check.That("Today rows are ordered before Upcoming rows",
			rows.Select(r => r.Period).SequenceEqual(rows.Select(r => r.Period).OrderBy(p => p)));

		// The business date drives the split: move it forward one day and tomorrow's booking becomes Today.
		var c = fx.FrontOffice();
		c.BusinessDate = () => T.AddDays(1);
		var shifted = await List(c);
		Check.That("business date controls Today/Upcoming",
			shifted.Single(r => r.BookingId == startsTomorrow.Id).Period == FrontOfficeBookingPeriod.Today);
		Check.That("stay ending on the business date drops off",
			shifted.All(r => r.BookingId != startsToday.Id));
	}

	private static async Task RowsAndRooms()
	{
		using var fx = new Fx();
		// Same room type: one guest in house now plus two later reservations.
		var now = fx.Add(GuestHouseBookingStatus.CheckedIn, T.AddDays(-1), T.AddDays(2), allocated: "101");
		var later1 = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(3), T.AddDays(4));
		var later2 = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(10), T.AddDays(12), rooms: 2);
		// Multi-record booking: rooms reserved at booking time on record 2 (real allocation rows).
		var multi = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(5), T.AddDays(6), rooms: 2,
			reserved: new[] { (2, "201"), (2, "202") });

		var rows = await List(fx.FrontOffice());
		Check.Equal("one row per booking (no per-room duplicates)", 4, rows.Count);
		Check.Equal("booking ids are distinct", rows.Count, rows.Select(r => r.BookingId).Distinct().Count());

		var r1 = rows.Single(r => r.BookingId == now.Id);
		Check.That("checked-in booking shows its allocated room", r1.IsRoomAssigned && r1.RoomNumber == "101");

		var r2 = rows.Single(r => r.BookingId == later1.Id);
		Check.That("unallocated future booking has no guessed room number", !r2.IsRoomAssigned && r2.RoomNumber == null);

		var r3 = rows.Single(r => r.BookingId == later2.Id);
		Check.That("multi-room unallocated booking: one row, no room number", !r3.IsRoomAssigned && r3.NumberOfRooms == 2);

		var r4 = rows.Single(r => r.BookingId == multi.Id);
		Check.That("multi-record booking shows its reserved rooms", r4.IsRoomAssigned && r4.RoomNumber == "201, 202");
		Check.That("guest name and status text are populated", r2.GuestName == "Guest One" && r2.StatusText == "Confirmed" && r1.StatusText == "Checked-In");
	}

	private static async Task RoomGridUnchanged()
	{
		using var fx = new Fx();
		fx.Add(GuestHouseBookingStatus.CheckedIn, T.AddDays(-1), T.AddDays(1), allocated: "101");
		fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(3), T.AddDays(5));
		fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(7), T.AddDays(8));

		var houses = (List<FrontOfficeHouseDto>)((ObjectResult)await fx.FrontOffice().GetRooms()).Value!;
		var rooms = houses.SelectMany(h => h.Rooms).ToList();
		Check.Equal("room grid still lists one row per physical room", 5, rooms.Count);
		Check.Equal("room grid: only today's guest is Checked-In", 1, rooms.Count(r => r.RoomStatus == FrontOfficeRoomStatus.CheckedIn));
		Check.Equal("room grid: future bookings do not reserve rooms today", 0, rooms.Count(r => r.RoomStatus == FrontOfficeRoomStatus.Reserved));
		Check.Equal("room grid: Available count unaffected by future bookings", 4, rooms.Count(r => r.RoomStatus == FrontOfficeRoomStatus.Available));
	}

	private static async Task Workflow()
	{
		using var fx = new Fx();
		var today = fx.Add(GuestHouseBookingStatus.Confirmed, T, T.AddDays(2));
		var future = fx.Add(GuestHouseBookingStatus.Confirmed, T.AddDays(3), T.AddDays(4));
		var c = fx.FrontOffice();

		var futureCheckIn = (FrontOfficeBookingDto)((ObjectResult)await c.GetBookingForCheckIn(future.Id)).Value!;
		Check.That("upcoming booking still cannot be checked in early", !futureCheckIn.CanCheckIn);

		Check.Equal("today's booking checks in", 200,
			Status(await c.CheckIn(new CheckInRequest { BookingId = today.Id, SelectedRoomNumbers = new List<string> { "103" } })));
		var afterIn = (await List(c)).Single(r => r.BookingId == today.Id);
		Check.That("after check-in: Checked-In with the selected room",
			afterIn.BookingStatus == GuestHouseBookingStatus.CheckedIn && afterIn.RoomNumber == "103");

		Check.Equal("checked-in guest checks out", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = today.Id })));
		var afterOut = await List(c);
		Check.That("after check-out: booking leaves the Front Office list", afterOut.All(r => r.BookingId != today.Id));
		Check.That("future booking is still listed", afterOut.Any(r => r.BookingId == future.Id));

		var billing = ((ObjectResult)await c.GetCompletedBookingsForBilling(null, null, null)).Value!;
		var count = (int)billing.GetType().GetProperty("Count")!.GetValue(billing)!;
		Check.Equal("completed stay is available in Generate Bill", 1, count);
	}

	private static Task IndiaDate()
	{
		var ist = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(z => z.Id is "Asia/Kolkata" or "India Standard Time");
		var expected = ist != null
			? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist).Date
			: DateTime.UtcNow.AddHours(5.5).Date;
		var actual = GuestHouseFrontOfficeController.IndiaToday();
		Check.Equal("IndiaToday is the IST calendar date", expected, actual);
		Check.That("IndiaToday has no time component", actual.TimeOfDay == TimeSpan.Zero);
		return Task.CompletedTask;
	}

	// ---- fixture ---------------------------------------------------------------------------------------

	private static int Status(IActionResult r) => r switch
	{
		ObjectResult o => o.StatusCode ?? 200,
		StatusCodeResult s => s.StatusCode,
		_ => -1
	};

	private sealed class Fx : IDisposable
	{
		public readonly AppDbContext Db;
		private int _seq;

		public Fx()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
				.Options;
			Db = new AppDbContext(options);
			Db.GuestHouses.Add(new GuestHouse { Id = 1, Name = "Test Guest House", IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 1, GuestHouseId = 1, RoomType = "AC Deluxe", RoomNumber = "101", PricePerNight = 1500m, Capacity = 2, NumberOfAdults = 2, AvailableQuantity = 3, IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 2, GuestHouseId = 1, RoomType = "AC Deluxe", RoomNumber = "201", PricePerNight = 1500m, Capacity = 2, NumberOfAdults = 2, AvailableQuantity = 2, IsActive = true });
			Db.SaveChanges();
		}

		public GuestHouseBooking Add(GuestHouseBookingStatus status, DateTime checkIn, DateTime checkOut,
			string? allocated = null, int rooms = 1, (int RoomId, string Number)[]? reserved = null, DateTime? createdAt = null)
		{
			var sub = 1500m * Math.Max(1, (checkOut - checkIn).Days);
			var booking = new GuestHouseBooking
			{
				BookingReference = $"BKL{++_seq:00000}",
				GuestHouseId = 1, GuestHouseRoomId = 1, NumberOfRooms = rooms, NumberOfPersons = 2,
				CheckInDate = checkIn, CheckInTime = new TimeSpan(14, 0, 0),
				CheckOutDate = checkOut, CheckOutTime = new TimeSpan(10, 0, 0),
				ActualCheckInAt = status == GuestHouseBookingStatus.CheckedIn ? checkIn.AddHours(14) : null,
				RoomPrice = 1500m, SubTotal = sub, TaxAmount = 0m, TotalAmount = sub,
				BookingStatus = status,
				PaymentStatus = GuestHousePaymentStatus.Paid,
				CreatedAt = createdAt ?? DateTime.Now.AddDays(-7)
			};
			booking.Guests.Add(new GuestHouseBookingGuest { GuestName = "Guest One", PhoneNumber = "9999999999", NumberOfPersons = 2 });
			booking.Payments.Add(new GuestHouseBookingPayment
			{
				PaymentReference = "order_test_1", PaymentMethod = GuestHousePaymentMethod.Razorpay,
				PaymentStatus = GuestHousePaymentStatus.Paid, Amount = sub, PaymentDate = checkIn.AddDays(-1)
			});
			if (allocated != null)
				booking.RoomAllocations.Add(new GuestHouseRoomAllocation { GuestHouseId = 1, GuestHouseRoomId = 1, RoomNumber = allocated, CheckInDate = checkIn, CheckOutDate = checkOut });
			foreach (var (roomId, number) in reserved ?? Array.Empty<(int, string)>())
				booking.RoomAllocations.Add(new GuestHouseRoomAllocation { GuestHouseId = 1, GuestHouseRoomId = roomId, RoomNumber = number, CheckInDate = checkIn, CheckOutDate = checkOut });
			Db.GuestHouseBookings.Add(booking);
			Db.SaveChanges();
			return booking;
		}

		public GuestHouseFrontOfficeController FrontOffice() =>
			new(Db, Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseFrontOfficeController>.Instance)
			{
				BusinessDate = () => T,
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext
					{
						User = new ClaimsPrincipal(new ClaimsIdentity(new[]
						{
							new Claim(ClaimTypes.NameIdentifier, "u-admin"),
							new Claim(ClaimTypes.Name, "user.admin"),
							new Claim(ClaimTypes.Role, AppRole.Admin.ToString())
						}, "test", ClaimTypes.Name, ClaimTypes.Role))
					}
				}
			};

		public void Dispose() => Db.Dispose();
	}
}
