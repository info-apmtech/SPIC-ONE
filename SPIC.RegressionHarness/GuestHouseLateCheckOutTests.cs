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
using SpicAPI.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Late check-out grace period + additional room charge, and overdue-guest room protection.
///
/// The calculator is pure, so it is tested directly on exact timestamps. Everything else calls the REAL
/// GuestHouseFrontOfficeController actions against a private EF Core InMemory database per scenario
/// (no PostgreSQL, no production data, no Razorpay - nothing is ever charged). The controller's server
/// clock is pinned through its Clock property. InMemory ignores transactions, so the serializable race
/// itself is not exercised; duplicate submission is covered by the sequential retry.
/// </summary>
internal static partial class GuestHouseLateCheckOutTests
{
	// Scheduled check-out of the policy examples: 19 Nov 2026, 10:00 AM, Rs 1,500 per day.
	private static readonly DateTime Scheduled = new(2026, 11, 19, 10, 0, 0);
	private static readonly DateTime Today = DateTime.Today;

	public static void Run()
	{
		Console.WriteLine("GuestHouseLateCheckOut");

		Step("1. policy table (client examples)", PolicyTable);
		Step("2. boundaries, midnight, month/year, early", Boundaries);
		Step("3. room rate snapshot + GST", RateAndGst);
		Step("4. summary, payment gate and check-out", PaymentGate);
		Step("5. duplicate submissions", Duplicates);
		Step("6. invoice integration", Invoice);
		Step("7. existing / historical bookings unchanged", Historical);
		Step("8. Razorpay, Pay After Stay, Employee Stay", PaymentMethods);
		Step("9. overdue guest stays visible", OverdueVisibility);
		Step("10. no double allocation of an occupied room", NoDoubleAllocation);
		Console.WriteLine();
		RunBilling();
	}

	private static void Step(string name, Func<Task> body)
	{
		Console.WriteLine($" {name}");
		try { body().GetAwaiter().GetResult(); }
		catch (Exception ex) { Check.That($"{name} ran to completion", false, $"{ex.GetType().Name}: {ex.Message}"); }
	}

	// ---- pure calculator ---------------------------------------------------------------------------

	private static GuestHouseLateCheckOut.Result Calc(DateTime actual, decimal rate = 1500m, decimal gst = 0m) =>
		GuestHouseLateCheckOut.Calculate(Scheduled, actual, rate, gst);

	private static Task PolicyTable()
	{
		void Row(string label, DateTime actual, decimal expected) =>
			Check.Equal(label, expected, Calc(actual).Amount);

		Row("on time -> 0", Scheduled, 0m);
		Row("30 minutes late (10:30) -> 0", Scheduled.AddMinutes(30), 0m);
		Row("exactly 1 hour (11:00) -> 0", Scheduled.AddHours(1), 0m);
		Row("1h 1m (11:01) -> 1,500", Scheduled.AddMinutes(61), 1500m);
		Row("20 Nov 10:00 (24h) -> 1,500", Scheduled.AddHours(24), 1500m);
		Row("20 Nov 11:00 (25h) -> 1,500", Scheduled.AddHours(25), 1500m);
		Row("20 Nov 11:01 (25h 1m) -> 3,000", Scheduled.AddHours(25).AddMinutes(1), 3000m);
		Row("21 Nov 11:00 (49h) -> 3,000", Scheduled.AddHours(49), 3000m);
		Row("21 Nov 11:01 (49h 1m) -> 4,500", Scheduled.AddHours(49).AddMinutes(1), 4500m);
		Row("121h (5th day grace ended at 97h, 6th ends at 121h) -> 7,500", Scheduled.AddHours(121), 7500m);
		Row("121h 1m -> 9,000", Scheduled.AddHours(121).AddMinutes(1), 9000m);

		// Seconds count: 1 hour and one second is beyond the grace.
		Check.Equal("1h 0m 1s -> 1 day", 1, Calc(Scheduled.AddHours(1).AddSeconds(1)).AdditionalDays);
		Check.That("within grace is flagged", Calc(Scheduled.AddMinutes(30)).WithinGrace);
		Check.That("beyond grace is not flagged", !Calc(Scheduled.AddMinutes(90)).WithinGrace);
		return Task.CompletedTask;
	}

	private static Task Boundaries()
	{
		Check.Equal("early check-out -> 0", 0m, Calc(Scheduled.AddHours(-5)).Amount);
		Check.Equal("early check-out late duration is zero", TimeSpan.Zero, Calc(Scheduled.AddHours(-5)).LateDuration);

		// Midnight scheduled check-out.
		var midnight = new DateTime(2026, 11, 20, 0, 0, 0);
		Check.Equal("midnight: 00:59 -> 0", 0, GuestHouseLateCheckOut.Calculate(midnight, midnight.AddMinutes(59), 1000m, 0m).AdditionalDays);
		Check.Equal("midnight: 01:01 -> 1 day", 1, GuestHouseLateCheckOut.Calculate(midnight, midnight.AddMinutes(61), 1000m, 0m).AdditionalDays);

		// Month / year change: 31 Dec 2026 10:00 -> 1 Jan 2027 10:00 is 24h; 11:00 is 25h; 11:01 is 25h 1m.
		var dec = new DateTime(2026, 12, 31, 10, 0, 0);
		Check.Equal("year change 24h -> 1 day", 1, GuestHouseLateCheckOut.Calculate(dec, new DateTime(2027, 1, 1, 10, 0, 0), 1000m, 0m).AdditionalDays);
		Check.Equal("year change 25h -> 1 day", 1, GuestHouseLateCheckOut.Calculate(dec, new DateTime(2027, 1, 1, 11, 0, 0), 1000m, 0m).AdditionalDays);
		Check.Equal("year change 25h 1m -> 2 days", 2, GuestHouseLateCheckOut.Calculate(dec, new DateTime(2027, 1, 1, 11, 1, 0), 1000m, 0m).AdditionalDays);

		// 28 Feb 2027 (non leap year) -> 1 Mar 2027.
		var feb = new DateTime(2027, 2, 28, 12, 0, 0);
		Check.Equal("February to March 24h -> 1 day", 1, GuestHouseLateCheckOut.Calculate(feb, new DateTime(2027, 3, 1, 12, 0, 0), 1000m, 0m).AdditionalDays);

		// Default check-out time when the booking has none: 12:00.
		var b = new GuestHouseBooking { CheckOutDate = new DateTime(2026, 11, 19), CheckOutTime = null };
		Check.Equal("missing CheckOutTime falls back to 12:00", new DateTime(2026, 11, 19, 12, 0, 0), GuestHouseLateCheckOut.ScheduledCheckOutAt(b));
		b.CheckOutTime = new TimeSpan(10, 0, 0);
		Check.Equal("CheckOutDate + CheckOutTime combine", Scheduled, GuestHouseLateCheckOut.ScheduledCheckOutAt(b));
		return Task.CompletedTask;
	}

	private static async Task RateAndGst()
	{
		using var fx = new Fx();
		var booking = fx.AddCheckedInBooking(1);
		fx.Db.GuestHouseRooms.Find(1)!.PricePerNight = 2500m; // room master repriced after the booking
		await fx.Db.SaveChangesAsync();
		var loaded = await fx.Db.GuestHouseBookings.Include(x => x.Payments).SingleAsync(x => x.Id == booking.Id);

		var r = GuestHouseLateCheckOut.Calculate(loaded, Scheduled.AddMinutes(61));
		Check.Equal("uses the booking's saved rate, not the new room master rate", 1500m, r.Amount);

		// Booking GST is 5% (150 on 3,000): CGST 2.5% + SGST 2.5%.
		Check.Equal("CGST", 37.50m, r.CgstAmount);
		Check.Equal("SGST", 37.50m, r.SgstAmount);
		Check.Equal("total with GST", 1575m, r.Total);

		var two = GuestHouseLateCheckOut.Calculate(loaded, Scheduled.AddHours(25).AddMinutes(1));
		Check.Equal("2 days = 3,000 + 150 GST", 3150m, two.Total);
	}

	// ---- controller flows ---------------------------------------------------------------------------

	private static async Task<CheckOutSummaryDto> Summary(GuestHouseFrontOfficeController c, int id)
	{
		var r = await c.GetCheckOutSummary(id);
		Check.Equal("summary HTTP 200", 200, Status(r));
		return (CheckOutSummaryDto)((ObjectResult)r).Value!;
	}

	private static async Task PaymentGate()
	{
		// Within grace: no charge, existing flow untouched.
		using (var fx = new Fx())
		{
			var b = fx.AddCheckedInBooking(1);
			var c = fx.FrontOffice(Scheduled.AddMinutes(30));
			var s = await Summary(c, b.Id);
			Check.That("within grace flagged", s.WithinGracePeriod);
			Check.Equal("within grace: no additional days", 0, s.AdditionalDaysCharged);
			Check.Equal("within grace: final payable = booking amount", 3150m, s.FinalPayableAmount);
			Check.That("within grace: ready for check-out", s.ReadyForCheckOut);
			Check.Equal("within grace: check-out succeeds", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
			Check.Equal("within grace: no extra payment row", 1, await fx.Db.GuestHouseBookingPayments.CountAsync());
		}

		// Beyond grace: summary shows the charge, check-out is refused until it is collected.
		using (var fx = new Fx())
		{
			var b = fx.AddCheckedInBooking(1);
			var c = fx.FrontOffice(Scheduled.AddMinutes(61));
			var s = await Summary(c, b.Id);
			Check.Equal("summary: scheduled time", Scheduled, s.ScheduledCheckOutAt);
			Check.Equal("summary: actual time", Scheduled.AddMinutes(61), s.ActualCheckOutAt);
			Check.Equal("summary: grace minutes", 60, s.GracePeriodMinutes);
			Check.Equal("summary: late minutes", 61L, s.LateDurationMinutes);
			Check.Equal("summary: days charged", 1, s.AdditionalDaysCharged);
			Check.Equal("summary: charge before GST", 1500m, s.LateCheckOutCharge);
			Check.Equal("summary: GST", 75m, s.LateCheckOutGst);
			Check.Equal("summary: original booking amount", 3150m, s.TotalAmount);
			Check.Equal("summary: final payable", 3150m + 1575m, s.FinalPayableAmount);
			Check.That("summary: not ready (late charge outstanding)", !s.ReadyForCheckOut);

			var blocked = await c.CheckOut(new CheckOutRequest { BookingId = b.Id });
			Check.Equal("check-out refused while late charge unpaid", 400, Status(blocked));
			var after = await fx.Db.GuestHouseBookings.SingleAsync(x => x.Id == b.Id);
			Check.That("booking still CheckedIn", after.BookingStatus == GuestHouseBookingStatus.CheckedIn);
			Check.Equal("room allocation kept", 1, await fx.Db.GuestHouseRoomAllocations.CountAsync());

			Check.Equal("collecting payment succeeds", 200, Status(await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id })));
			var s2 = await Summary(c, b.Id);
			Check.Equal("nothing outstanding after payment", 0m, s2.LateChargeOutstanding);
			Check.That("ready for check-out", s2.ReadyForCheckOut);
			Check.Equal("check-out now succeeds", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));

			var done = await fx.Db.GuestHouseBookings.Include(x => x.Payments).SingleAsync(x => x.Id == b.Id);
			Check.That("completed", done.BookingStatus == GuestHouseBookingStatus.Completed);
			Check.Equal("server clock recorded as actual check-out", Scheduled.AddMinutes(61), done.ActualCheckOutAt ?? default);
			Check.Equal("original booking amount untouched", 3150m, done.TotalAmount ?? 0m);
			Check.Equal("allocation released only now", 0, await fx.Db.GuestHouseRoomAllocations.CountAsync());
		}

		// The grace boundary can be crossed between "payment recorded" and "check-out": the new
		// charge must be collected too.
		using (var fx = new Fx())
		{
			var b = fx.AddCheckedInBooking(1);
			var c = fx.FrontOffice(Scheduled.AddMinutes(61));
			await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id });
			c.Clock = () => Scheduled.AddHours(25).AddMinutes(1); // now a second day applies
			Check.Equal("charge grew after payment -> check-out refused", 400, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
			await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id });
			Check.Equal("then succeeds", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
			var rows = await fx.Db.GuestHouseBookingPayments.Where(p => p.PaymentReference != null && p.PaymentReference.StartsWith("LATE-CHECKOUT-")).ToListAsync();
			Check.Equal("still a single late-charge row", 1, rows.Count);
			Check.Equal("late row holds 2 days incl. GST", 3150m, rows.Single().Amount);
		}

		// Original booking unpaid: the existing rule still wins.
		using (var fx = new Fx())
		{
			var b = fx.AddCheckedInBooking(1, paid: false, method: GuestHousePaymentMethod.PayAfterStay);
			var c = fx.FrontOffice(Scheduled.AddMinutes(10));
			Check.Equal("unpaid original -> 400", 400, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
		}
	}

	private static async Task Duplicates()
	{
		using var fx = new Fx();
		var b = fx.AddCheckedInBooking(1);
		var c = fx.FrontOffice(Scheduled.AddHours(2));

		await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id });
		await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id });
		await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id });
		Check.Equal("repeated payment confirmation: one original + one late row", 2, await fx.Db.GuestHouseBookingPayments.CountAsync());

		Check.Equal("first check-out", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
		Check.Equal("duplicate check-out refused", 400, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
		Check.Equal("no extra payment row from the retry", 2, await fx.Db.GuestHouseBookingPayments.CountAsync());
		Check.Equal("check-out time not overwritten", Scheduled.AddHours(2), (await fx.Db.GuestHouseBookings.SingleAsync()).ActualCheckOutAt ?? default);

		var draft = await Draft(c, b.Id);
		Check.Equal("one late line on the draft", 1, draft.LineItems.Count(l => l.Description == "Late Check-Out Charge"));
	}

	private static async Task<BillDraftDto> Draft(GuestHouseFrontOfficeController c, int id)
	{
		var r = await c.GetBillDraft(id);
		Check.Equal("bill draft HTTP 200", 200, Status(r));
		return (BillDraftDto)((ObjectResult)r).Value!;
	}

	private static async Task Invoice()
	{
		using var fx = new Fx();
		var b = fx.AddCheckedInBooking(1);
		var c = fx.FrontOffice(Scheduled.AddHours(25).AddMinutes(1)); // 2 extra days
		await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id });
		await c.CheckOut(new CheckOutRequest { BookingId = b.Id });

		var draft = await Draft(c, b.Id);
		var late = draft.LineItems.Single(l => l.Description == "Late Check-Out Charge");
		Check.Equal("late line quantity = rooms x days", 2m, late.Quantity);
		Check.Equal("late line rate = booking's saved rate", 1500m, late.Rate);
		Check.Equal("late line CGST %", 2.5m, late.CgstPercent);
		Check.Equal("late line SGST %", 2.5m, late.SgstPercent);

		// The client tries to forge / duplicate / alter the late line: the server's line wins.
		var room = draft.LineItems.First(l => (l.Description ?? "").Contains("Room Charge"));
		var req = new GenerateBillRequest
		{
			BookingId = b.Id,
			PaymentReceived = true,
			PaymentMethod = (int)GuestHousePaymentMethod.PayAfterStay,
			LineItems = new List<BillLineItemDto>
			{
				room,
				new() { Description = "Late Check-Out Charge", Quantity = 1, Rate = 1m, CgstPercent = 0, SgstPercent = 0 },
				new() { Description = "late check-out charge", Quantity = 9, Rate = 9m }
			}
		};
		var gen = await c.GenerateBill(req);
		Check.Equal("bill generated", 200, Status(gen));
		var bill = await fx.Db.GuestHouseBills.Include(x => x.LineItems).SingleAsync();
		var lateLines = bill.LineItems.Where(l => l.Description == "Late Check-Out Charge").ToList();
		Check.Equal("exactly one late line on the invoice", 1, lateLines.Count);
		Check.Equal("late line amount = 2 x 1,500", 3000m, lateLines[0].Amount);
		Check.Equal("late line CGST", 75m, lateLines[0].CgstAmount);
		Check.Equal("late line SGST", 75m, lateLines[0].SgstAmount);
		// Room 2 nights x 1,500 = 3,000 (+150 GST) plus late 3,000 (+150 GST).
		Check.Equal("subtotal = room + late", 6000m, bill.Subtotal);
		Check.Equal("CGST total", 150m, bill.CgstAmount);
		Check.Equal("SGST total", 150m, bill.SgstAmount);
		Check.Equal("grand total", 6300m, bill.TotalAmount);

		Check.Equal("second invoice for the same booking refused", 400, Status(await c.GenerateBill(req)));
		Check.Equal("still exactly one bill", 1, await fx.Db.GuestHouseBills.CountAsync());
	}

	private static async Task Historical()
	{
		using var fx = new Fx();

		// A booking completed BEFORE this feature: checked out 3 days late, no settlement row.
		var old = fx.AddCheckedInBooking(1);
		old.BookingStatus = GuestHouseBookingStatus.Completed;
		old.ActualCheckOutAt = Scheduled.AddDays(3);
		fx.Db.GuestHouseRoomAllocations.RemoveRange(fx.Db.GuestHouseRoomAllocations);
		await fx.Db.SaveChangesAsync();

		var c = fx.FrontOffice(Scheduled.AddDays(10));
		var draft = await Draft(c, old.Id);
		Check.That("no late line is invented for a historical completed booking", draft.LineItems.All(l => l.Description != "Late Check-Out Charge"));
		Check.Equal("historical booking amount unchanged", 3150m, (await fx.Db.GuestHouseBookings.SingleAsync()).TotalAmount ?? 0m);
		Check.Equal("historical payments unchanged", 1, await fx.Db.GuestHouseBookingPayments.CountAsync());

		// A historical invoice is a stored snapshot: it is read, never recomputed.
		fx.Db.GuestHouseBills.Add(new GuestHouseBill { BillNumber = "BILL-OLD-0001", GuestHouseBookingId = old.Id, Subtotal = 3000m, CgstAmount = 75m, SgstAmount = 75m, TotalAmount = 3150m, BalanceAmount = 3150m });
		await fx.Db.SaveChangesAsync();
		var bill = await fx.Db.GuestHouseBills.SingleAsync();
		Check.Equal("existing invoice total unchanged", 3150m, bill.TotalAmount);
		Check.Equal("existing invoice cannot be duplicated", 400, Status(await c.GenerateBill(new GenerateBillRequest { BookingId = old.Id, LineItems = new() { new() { Description = "Room Charge", Quantity = 2, Rate = 1500 } } })));

		// On-time stay: payment + billing identical to before.
		using var fx2 = new Fx();
		var b = fx2.AddCheckedInBooking(1);
		var c2 = fx2.FrontOffice(Scheduled.AddMinutes(-30));
		Check.Equal("on-time check-out", 200, Status(await c2.CheckOut(new CheckOutRequest { BookingId = b.Id })));
		Check.Equal("on-time: no extra payment row", 1, await fx2.Db.GuestHouseBookingPayments.CountAsync());
		var d2 = await Draft(c2, b.Id);
		Check.That("on-time: no late line", d2.LineItems.All(l => l.Description != "Late Check-Out Charge"));
	}

	private static async Task PaymentMethods()
	{
		// Razorpay (paid online at booking): original stays Razorpay/Paid, late is collected separately.
		using (var fx = new Fx())
		{
			var b = fx.AddCheckedInBooking(1, method: GuestHousePaymentMethod.Razorpay);
			var c = fx.FrontOffice(Scheduled.AddHours(3));
			await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id });
			var rows = await fx.Db.GuestHouseBookingPayments.OrderBy(p => p.Id).ToListAsync();
			Check.Equal("Razorpay original row kept first", GuestHousePaymentMethod.Razorpay, rows[0].PaymentMethod);
			Check.Equal("Razorpay original amount unchanged", 3150m, rows[0].Amount);
			Check.Equal("Razorpay: check-out after collecting late charge", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
		}

		// Pay After Stay: still unpaid at the desk; one confirmation settles original + late.
		using (var fx = new Fx())
		{
			var b = fx.AddCheckedInBooking(1, paid: false, method: GuestHousePaymentMethod.PayAfterStay);
			var c = fx.FrontOffice(Scheduled.AddHours(3));
			Check.Equal("Pay After Stay: refused before payment", 400, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
			Check.Equal("Pay After Stay: confirm payment", 200, Status(await c.RecordPayment(new RecordPaymentRequest { BookingId = b.Id })));
			var rows = await fx.Db.GuestHouseBookingPayments.OrderBy(p => p.Id).ToListAsync();
			Check.Equal("Pay After Stay: original row paid", GuestHousePaymentStatus.Paid, rows[0].PaymentStatus);
			Check.Equal("Pay After Stay: original + late rows", 2, rows.Count);
			Check.Equal("Pay After Stay: check-out", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
		}

		// Employee Stay (Complimentary, Rs 0): never charged a late fee, rules unchanged.
		using (var fx = new Fx())
		{
			var b = fx.AddCheckedInBooking(1, method: GuestHousePaymentMethod.Complimentary, price: 0m);
			var c = fx.FrontOffice(Scheduled.AddDays(4));
			var s = await Summary(c, b.Id);
			Check.Equal("Employee Stay: no late days charged", 0, s.AdditionalDaysCharged);
			Check.Equal("Employee Stay: check-out needs no extra payment", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = b.Id })));
			Check.Equal("Employee Stay: no extra payment row", 1, await fx.Db.GuestHouseBookingPayments.CountAsync());
		}
	}

	// ---- room availability ---------------------------------------------------------------------------

	private static async Task OverdueVisibility()
	{
		using var fx = new Fx();
		// Scheduled to leave two days ago (overdue) - and another leaving today.
		var overdue = fx.AddCheckedInBooking(1, checkIn: Today.AddDays(-4), checkOut: Today.AddDays(-2), allocated: "101");
		var leavingToday = fx.AddCheckedInBooking(1, checkIn: Today.AddDays(-1), checkOut: Today, allocated: "102");

		var c = fx.FrontOffice(DateTime.Now);
		var ok = (List<FrontOfficeHouseDto>)((ObjectResult)await c.GetRooms()).Value!;
		var rooms = ok.SelectMany(h => h.Rooms).ToList();
		var r101 = rooms.Single(r => r.RoomNumber == "101");
		var r102 = rooms.Single(r => r.RoomNumber == "102");
		Check.That("overdue guest's room is not Available", r101.RoomStatus == FrontOfficeRoomStatus.CheckedIn);
		Check.Equal("overdue room shows its booking (Check-Out action)", overdue.Id, r101.CurrentBookingId ?? 0);
		Check.That("guest leaving today stays visible as CheckedIn", r102.RoomStatus == FrontOfficeRoomStatus.CheckedIn && r102.CurrentBookingId == leavingToday.Id);
		Check.That("an untouched room is still Available", rooms.Single(r => r.RoomNumber == "103").RoomStatus == FrontOfficeRoomStatus.Available);

		// Overdue guest can still be checked out (late charge collected), then the room frees up.
		var s = await Summary(c, overdue.Id);
		Check.That("overdue summary has additional days", s.AdditionalDaysCharged >= 2);
		await c.RecordPayment(new RecordPaymentRequest { BookingId = overdue.Id });
		Check.Equal("overdue guest checks out", 200, Status(await c.CheckOut(new CheckOutRequest { BookingId = overdue.Id })));
		var after = (List<FrontOfficeHouseDto>)((ObjectResult)await c.GetRooms()).Value!;
		Check.That("room is Available only after actual check-out", after.SelectMany(h => h.Rooms).Single(r => r.RoomNumber == "101").RoomStatus == FrontOfficeRoomStatus.Available);
	}

	private static async Task NoDoubleAllocation()
	{
		using var fx = new Fx(quantity: 1); // a single physical room, 101
		var overdue = fx.AddCheckedInBooking(1, checkIn: Today.AddDays(-4), checkOut: Today.AddDays(-1), allocated: "101");

		var free = await GuestHouseRoomInventory.GetFreePhysicalRoomsAsync(fx.Db, 1, Today, Today.AddDays(1), DateTime.Now.AddMinutes(-30));
		Check.That("overdue guest's room is not free today", !free.Free.Contains("101") && free.Occupied.Contains("101"));

		// A new confirmed booking arrives today for the same room type.
		var next = new GuestHouseBooking
		{
			BookingReference = "BKNEXT", GuestHouseId = 1, GuestHouseRoomId = 1, NumberOfRooms = 1, NumberOfPersons = 1,
			CheckInDate = Today, CheckInTime = new TimeSpan(14, 0, 0), CheckOutDate = Today.AddDays(1), CheckOutTime = new TimeSpan(10, 0, 0),
			RoomPrice = 1500m, BookingStatus = GuestHouseBookingStatus.Confirmed, PaymentStatus = GuestHousePaymentStatus.Paid
		};
		fx.Db.GuestHouseBookings.Add(next);
		await fx.Db.SaveChangesAsync();

		var c = fx.FrontOffice(DateTime.Now);
		var result = await c.CheckIn(new CheckInRequest { BookingId = next.Id, SelectedRoomNumbers = new List<string> { "101" } });
		Check.Equal("the occupied room cannot be assigned to another guest", 409, Status(result));
		Check.That("next booking not checked in", (await fx.Db.GuestHouseBookings.SingleAsync(x => x.Id == next.Id)).BookingStatus == GuestHouseBookingStatus.Confirmed);
		Check.Equal("no second allocation for 101", 1, await fx.Db.GuestHouseRoomAllocations.CountAsync(a => a.RoomNumber == "101"));

		// A booking for a LATER date is not affected (the room can be re-let once it is actually free).
		var later = await GuestHouseRoomInventory.GetFreePhysicalRoomsAsync(fx.Db, 1, Today.AddDays(3), Today.AddDays(4), DateTime.Now.AddMinutes(-30));
		Check.That("future dates are unaffected", later.Free.Contains("101"));

		// After actual check-out the room can be allocated.
		var oc = fx.FrontOffice(Today.AddDays(-1).AddHours(10).AddMinutes(30)); // within grace -> no late charge
		Check.Equal("overdue guest checks out", 200, Status(await oc.CheckOut(new CheckOutRequest { BookingId = overdue.Id })));
		Check.Equal("room now assignable", 200, Status(await c.CheckIn(new CheckInRequest { BookingId = next.Id, SelectedRoomNumbers = new List<string> { "101" } })));
	}

	// ---- fixture ---------------------------------------------------------------------------------------

	private static int Status(IActionResult r) => r switch
	{
		ObjectResult o => o.StatusCode ?? 200,
		StatusCodeResult s => s.StatusCode,
		ForbidResult => 403,
		_ => -1
	};

	private sealed class Fx : IDisposable
	{
		public readonly AppDbContext Db;
		private int _bookingSeq;

		public Fx(int quantity = 3)
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
				.Options;
			Db = new AppDbContext(options);
			Db.GuestHouses.Add(new GuestHouse { Id = 1, Name = "Test Guest House", IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = 1, GuestHouseId = 1, RoomType = "AC Deluxe", RoomNumber = "101", PricePerNight = 1500m, Capacity = 2, NumberOfAdults = 2, AvailableQuantity = quantity, IsActive = true });
			Db.SaveChanges();
		}

		/// <summary>
		/// A CheckedIn booking: 2 nights at Rs 1,500 + 5% GST = 3,150, scheduled to check out 19 Nov 2026 10:00
		/// unless dates are given. Original payment is Paid (online) unless <paramref name="paid"/> is false.
		/// </summary>
		public GuestHouseBooking AddCheckedInBooking(int roomId, bool paid = true, GuestHousePaymentMethod method = GuestHousePaymentMethod.Razorpay,
			decimal price = 1500m, DateTime? checkIn = null, DateTime? checkOut = null, string allocated = "101")
		{
			var co = checkOut ?? Scheduled.Date;
			var ci = checkIn ?? co.AddDays(-2);
			var sub = price * 2;
			var booking = new GuestHouseBooking
			{
				BookingReference = $"BKT{++_bookingSeq:00000}",
				GuestHouseId = 1, GuestHouseRoomId = roomId, NumberOfRooms = 1, NumberOfNights = 2, NumberOfPersons = 2,
				CheckInDate = ci, CheckInTime = new TimeSpan(14, 0, 0),
				CheckOutDate = co, CheckOutTime = new TimeSpan(10, 0, 0),
				ActualCheckInAt = ci.AddHours(14),
				RoomPrice = price, SubTotal = sub, TaxAmount = sub * 0.05m, TotalAmount = sub * 1.05m,
				BookingStatus = GuestHouseBookingStatus.CheckedIn,
				PaymentStatus = paid ? GuestHousePaymentStatus.Paid : GuestHousePaymentStatus.Pending
			};
			booking.Guests.Add(new GuestHouseBookingGuest { GuestName = "Guest One", PhoneNumber = "9999999999", NumberOfPersons = 2 });
			booking.Payments.Add(new GuestHouseBookingPayment
			{
				PaymentReference = method == GuestHousePaymentMethod.Razorpay ? "order_test_1" : null,
				PaymentMethod = method,
				PaymentStatus = paid ? GuestHousePaymentStatus.Paid : GuestHousePaymentStatus.Pending,
				Amount = sub * 1.05m,
				PaymentDate = paid ? ci.AddHours(-24) : null
			});
			booking.RoomAllocations.Add(new GuestHouseRoomAllocation { GuestHouseId = 1, GuestHouseRoomId = roomId, RoomNumber = allocated, CheckInDate = ci, CheckOutDate = co });
			Db.GuestHouseBookings.Add(booking);
			Db.SaveChanges();
			return booking;
		}

		public GuestHouseFrontOfficeController FrontOffice(DateTime now) =>
			new(Db, Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseFrontOfficeController>.Instance)
			{
				Clock = () => now,
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
