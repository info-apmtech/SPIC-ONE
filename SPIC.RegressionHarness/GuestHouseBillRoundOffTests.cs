using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Billing;
using SPIC.Core.Entities;
using SpicAPI.Controllers;
using SpicAPI.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Generate Bill: automatic nearest-rupee Round Off and the server-verified, read-only Advance Payment.
/// Real controller actions on a private InMemory database; no real payments.
/// </summary>
internal static partial class GuestHouseLateCheckOutTests
{
	private static void RunBilling()
	{
		Console.WriteLine("GuestHouseBillRoundOff");
		Step("11. nearest-rupee round off (rule + examples)", RoundOffRule);
		Step("12. advance comes only from verified payments", VerifiedAdvanceRule);
		Step("13. generate bill: automatic round off + verified advance", GenerateRoundedBill);
		Step("14. tampered advance / round off cannot change records", TamperedRequests);
		Step("15. discount interaction and negative balance", DiscountRules);
		Step("16. saved values, page and PDF agree; history unchanged", PdfAndHistory);
		Step("17. Generate Bill page wiring (source checks)", PageWiring);
	}

	private static Task RoundOffRule()
	{
		void Row(string label, decimal gross, decimal discount, decimal advance, decimal roundOff, decimal total, decimal balance)
		{
			var r = GuestHouseBillMath.Compute(gross, discount, advance);
			Check.Equal($"{label}: round off", roundOff, r.RoundOff);
			Check.Equal($"{label}: rounded total", total, r.RoundedPayable);
			Check.Equal($"{label}: balance", balance, r.Balance);
		}
		Row("102.80", 102.80m, 0, 0, 0.20m, 103m, 103m);
		Row("102.50 (exact 50 paise rounds up)", 102.50m, 0, 0, 0.50m, 103m, 103m);
		Row("102.49", 102.49m, 0, 0, -0.49m, 102m, 102m);
		Row("101.20", 101.20m, 0, 0, -0.20m, 101m, 101m);
		Row("100.75", 100.75m, 0, 0, 0.25m, 101m, 101m);
		Row("100.25", 100.25m, 0, 0, -0.25m, 100m, 100m);
		Row("100.00 needs none", 100m, 0, 0, 0m, 100m, 100m);
		Row("spec example, advance 50", 102.80m, 0, 50m, 0.20m, 103m, 53m);
		Row("advance does not move the rounding (102.49, advance 40.10)", 102.49m, 0, 40.10m, -0.49m, 102m, 61.90m);
		Row("discount first: 102.80 - 0.30 = 102.50 -> +0.50", 102.80m, 0.30m, 0, 0.50m, 103m, 103m);
		Row("discount first: 118.60 - 10.20 = 108.40 -> -0.40", 118.60m, 10.20m, 0, -0.40m, 108m, 108m);
		Row("sample 118.29", 118.29m, 0, 0, -0.29m, 118m, 118m);
		Row("sample 118.60", 118.60m, 0, 0, 0.40m, 119m, 119m);
		Row("sample 118.89", 118.89m, 0, 0, 0.11m, 119m, 119m);
		Row("sample 4.20", 4.20m, 0, 0, -0.20m, 4m, 4m);
		Check.Equal("signed positive round off keeps its sign", "+0.20", GuestHouseBillMath.Compute(102.80m, 0, 0).RoundOff.ToString("+0.00;-0.00"));
		Check.Equal("signed negative round off keeps its sign", "-0.49", GuestHouseBillMath.Compute(102.49m, 0, 0).RoundOff.ToString("+0.00;-0.00"));
		return Task.CompletedTask;
	}

	private static GuestHouseBookingPayment Pay(GuestHousePaymentMethod m, GuestHousePaymentStatus s, decimal amt, DateTime? at, string? txn = null, string? reference = null) =>
		new() { PaymentMethod = m, PaymentStatus = s, Amount = amt, PaymentDate = at, TransactionId = txn, PaymentReference = reference };

	private static async Task VerifiedAdvanceRule()
	{
		using var fx = new Fx();
		var b0 = fx.AddCheckedInBooking(1);
		var checkIn = b0.ActualCheckInAt!.Value;
		fx.Db.GuestHouseBookingPayments.RemoveRange(fx.Db.GuestHouseBookingPayments);
		await fx.Db.SaveChangesAsync();
		var b = await fx.Db.GuestHouseBookings.Include(x => x.Payments).SingleAsync();

		Check.Equal("no payment received -> 0.00", 0m, GuestHouseVerifiedAdvance.Compute(b));

		b.Payments.Add(Pay(GuestHousePaymentMethod.Razorpay, GuestHousePaymentStatus.Pending, 500m, checkIn.AddHours(-5), "pay_P"));
		b.Payments.Add(Pay(GuestHousePaymentMethod.Razorpay, GuestHousePaymentStatus.Failed, 700m, checkIn.AddHours(-4), "pay_F"));
		b.Payments.Add(Pay(GuestHousePaymentMethod.UPI, GuestHousePaymentStatus.Refunded, 900m, checkIn.AddHours(-3), "pay_R"));
		await fx.Db.SaveChangesAsync();
		Check.Equal("pending, failed and refunded payments are not counted", 0m, GuestHouseVerifiedAdvance.Compute(b));

		b.Payments.Add(Pay(GuestHousePaymentMethod.Razorpay, GuestHousePaymentStatus.Paid, 50m, checkIn.AddHours(-2), "pay_1"));
		await fx.Db.SaveChangesAsync();
		Check.Equal("one verified payment", 50m, GuestHouseVerifiedAdvance.Compute(b));

		b.Payments.Add(Pay(GuestHousePaymentMethod.UPI, GuestHousePaymentStatus.Paid, 25.50m, checkIn.AddHours(-1), "pay_2"));
		await fx.Db.SaveChangesAsync();
		Check.Equal("an additional verified payment is added to the total", 75.50m, GuestHouseVerifiedAdvance.Compute(b));

		b.Payments.Add(Pay(GuestHousePaymentMethod.Razorpay, GuestHousePaymentStatus.Paid, 50m, checkIn.AddHours(-2), "pay_1"));
		await fx.Db.SaveChangesAsync();
		Check.Equal("the same transaction id twice is counted once", 75.50m, GuestHouseVerifiedAdvance.Compute(b));

		b.Payments.Add(Pay(GuestHousePaymentMethod.PayAfterStay, GuestHousePaymentStatus.Paid, 1000m, checkIn.AddHours(1)));
		b.Payments.Add(Pay(GuestHousePaymentMethod.PayAfterStay, GuestHousePaymentStatus.Paid, 200m, checkIn.AddHours(-1), null, "LATE-CHECKOUT-1"));
		b.Payments.Add(Pay(GuestHousePaymentMethod.UPI, GuestHousePaymentStatus.Paid, 300m, checkIn.AddHours(2), "pay_desk"));
		await fx.Db.SaveChangesAsync();
		Check.Equal("desk collections / late settlement / post-check-in payments are not an advance", 75.50m, GuestHouseVerifiedAdvance.Compute(b));

		b.BookingStatus = GuestHouseBookingStatus.Completed;
		b.ActualCheckOutAt = Scheduled;
		await fx.Db.SaveChangesAsync();
		var draft = await DraftOrNull(fx.FrontOffice(DateTime.Now), b.Id);
		Check.Equal("bill draft carries the verified advance", 75.50m, draft?.AdvancePayment ?? -1m);
	}

	private static async Task<BillDraftDto?> DraftOrNull(GuestHouseFrontOfficeController c, int id)
	{
		var r = await c.GetBillDraft(id);
		return Status(r) == 200 ? (BillDraftDto)((ObjectResult)r).Value! : null;
	}

	/// <summary>A Completed booking whose only advance is a verified Razorpay payment of <paramref name="advance"/>.</summary>
	private static async Task<GuestHouseBooking> CompletedWithAdvance(Fx fx, decimal advance)
	{
		var b = fx.AddCheckedInBooking(1);
		var pay = fx.Db.GuestHouseBookingPayments.Single();
		pay.Amount = advance;
		pay.TransactionId = "pay_adv";
		b.BookingStatus = GuestHouseBookingStatus.Completed;
		b.ActualCheckOutAt = Scheduled.AddMinutes(-30);
		fx.Db.GuestHouseRoomAllocations.RemoveRange(fx.Db.GuestHouseRoomAllocations);
		await fx.Db.SaveChangesAsync();
		return b;
	}

	private static GenerateBillRequest MiscBill(int bookingId, decimal? advance, decimal? roundOff = null, decimal? discount = null) => new()
	{
		BookingId = bookingId,
		// 100.00 + CGST 1.50 + SGST 1.30 = 102.80 (the example in the requirement).
		LineItems = new() { new() { Description = "Misc Service", Quantity = 1, Rate = 100m, CgstPercent = 1.5m, SgstPercent = 1.3m } },
		AdvancePayment = advance,
		RoundOff = roundOff,
		Discount = discount,
		PaymentReceived = true,
		PaymentMethod = (int)GuestHousePaymentMethod.UPI
	};

	private static async Task GenerateRoundedBill()
	{
		using var fx = new Fx();
		var b = await CompletedWithAdvance(fx, 50m);
		var c = fx.FrontOffice(DateTime.Now);

		var draft = await DraftOrNull(c, b.Id);
		Check.Equal("page draft advance = verified payment", 50m, draft?.AdvancePayment ?? -1m);

		// The client even posts a made-up round off: it is ignored.
		var r = await c.GenerateBill(MiscBill(b.Id, 50m, roundOff: 7.77m));
		Check.Equal("bill generated", 200, Status(r));
		var bill = await fx.Db.GuestHouseBills.SingleAsync();
		Check.Equal("subtotal", 100m, bill.Subtotal);
		Check.Equal("CGST", 1.50m, bill.CgstAmount);
		Check.Equal("SGST", 1.30m, bill.SgstAmount);
		Check.Equal("gross", 102.80m, bill.TotalAmount);
		Check.Equal("automatic round off (+0.20, client value ignored)", 0.20m, bill.RoundOff);
		Check.Equal("verified advance saved", 50m, bill.AdvancePayment);
		Check.Equal("balance = 103.00 - 50.00", 53m, bill.BalanceAmount);
		Check.Equal("grand total after round off", 103m, bill.TotalAmount - bill.Discount + bill.RoundOff);
		Check.Equal("payment records untouched", 50m, (await fx.Db.GuestHouseBookingPayments.SingleAsync()).Amount);
	}

	private static async Task TamperedRequests()
	{
		using var fx = new Fx();
		var b = await CompletedWithAdvance(fx, 50m);
		var c = fx.FrontOffice(DateTime.Now);

		Check.Equal("advance forced to 0 is rejected", 409, Status(await c.GenerateBill(MiscBill(b.Id, 0m))));
		Check.Equal("advance inflated is rejected", 409, Status(await c.GenerateBill(MiscBill(b.Id, 9999m))));
		Check.Equal("advance raised by a paisa is rejected", 409, Status(await c.GenerateBill(MiscBill(b.Id, 50.01m))));
		Check.Equal("no bill was created by rejected requests", 0, await fx.Db.GuestHouseBills.CountAsync());
		var pay = await fx.Db.GuestHouseBookingPayments.SingleAsync();
		Check.Equal("payment amount unchanged", 50m, pay.Amount);
		Check.Equal("payment transaction reference unchanged", "pay_adv", pay.TransactionId ?? "");
		Check.That("payment status unchanged", pay.PaymentStatus == GuestHousePaymentStatus.Paid);
		Check.Equal("no payment rows added or removed", 1, await fx.Db.GuestHouseBookingPayments.CountAsync());

		// An older client that sends no advance at all still gets the verified one.
		Check.Equal("omitted advance uses the verified amount", 200, Status(await c.GenerateBill(MiscBill(b.Id, null))));
		Check.Equal("saved advance is the verified payment", 50m, (await fx.Db.GuestHouseBills.SingleAsync()).AdvancePayment);
	}

	private static async Task DiscountRules()
	{
		// Discount is applied before rounding: 102.80 - 0.30 = 102.50 -> +0.50 -> 103.00.
		using (var fx = new Fx())
		{
			var b = await CompletedWithAdvance(fx, 0m);
			Check.Equal("discounted bill generated", 200, Status(await fx.FrontOffice(DateTime.Now).GenerateBill(MiscBill(b.Id, null, discount: 0.30m))));
			var bill = await fx.Db.GuestHouseBills.SingleAsync();
			Check.Equal("round off after discount", 0.50m, bill.RoundOff);
			Check.Equal("balance", 103m, bill.BalanceAmount);
			Check.Equal("discount saved", 0.30m, bill.Discount);
		}
		using (var fx = new Fx())
		{
			var b = await CompletedWithAdvance(fx, 0m);
			Check.Equal("discount above total rejected", 400, Status(await fx.FrontOffice(DateTime.Now).GenerateBill(MiscBill(b.Id, null, discount: 500m))));
		}
		// Received money above the payable is never silently reduced.
		using (var fx = new Fx())
		{
			var b = await CompletedWithAdvance(fx, 3150m);
			Check.Equal("payable below received advance rejected", 400, Status(await fx.FrontOffice(DateTime.Now).GenerateBill(MiscBill(b.Id, 3150m))));
			Check.Equal("payment record untouched", 3150m, (await fx.Db.GuestHouseBookingPayments.SingleAsync()).Amount);
		}
	}

	private static async Task PdfAndHistory()
	{
		using var fx = new Fx();
		var b = await CompletedWithAdvance(fx, 50m);
		var c = fx.FrontOffice(DateTime.Now);
		await c.GenerateBill(MiscBill(b.Id, 50m));
		var bill = await fx.Db.GuestHouseBills.Include(x => x.LineItems).SingleAsync();

		var view = GuestHouseBillPdfHelper.ToBillViewDto(bill);
		Check.Equal("PDF/page view: round off = saved", bill.RoundOff, view.RoundOff);
		Check.Equal("PDF/page view: advance = saved", bill.AdvancePayment, view.AdvancePayment);
		Check.Equal("PDF/page view: balance = saved", bill.BalanceAmount, view.BalanceAmount);
		Check.Equal("PDF/page grand total = saved rounded total", 103m, view.TotalAmount - view.Discount + view.RoundOff);
		Check.Equal("balance = grand total - advance (saved values)", view.TotalAmount - view.Discount + view.RoundOff - view.AdvancePayment, view.BalanceAmount);

		var pdfSource = File.ReadAllText(RepoFile("SpicAPI", "Services", "GuestHouseInvoicePdfBuilder.cs"));
		Check.That("PDF prints a signed Round Off", pdfSource.Contains("+0.00;-0.00"));
		Check.That("PDF prints the Grand Total from saved values", pdfSource.Contains("bill.TotalAmount - bill.Discount + bill.RoundOff"));
		Check.That("PDF does not recompute the round off", !pdfSource.Contains("GuestHouseBillMath"));
		try { Check.That("PDF still renders", GuestHouseInvoicePdfBuilder.Build(view).Length > 1000); }
		catch (Exception ex) { Check.That("PDF still renders", false, ex.Message); }

		// Historical invoice with an old MANUAL round off: stored values are read as saved and still add up.
		var old = new GuestHouseBill { BillNumber = "BILL-OLD", GuestHouseBookingId = b.Id, TotalAmount = 118.60m, Discount = 0, AdvancePayment = 0, RoundOff = -0.60m, BalanceAmount = 118m };
		var oldView = GuestHouseBillPdfHelper.ToBillViewDto(old);
		Check.Equal("historical round off unchanged", -0.60m, oldView.RoundOff);
		Check.Equal("historical balance unchanged", 118m, oldView.BalanceAmount);
		Check.Equal("historical grand total derivation still holds", oldView.BalanceAmount + oldView.AdvancePayment, oldView.TotalAmount - oldView.Discount + oldView.RoundOff);
	}

	private static string RepoFile(params string[] parts)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		return Path.Combine(new[] { dir!.FullName }.Concat(parts).ToArray());
	}

	private static Task PageWiring()
	{
		var page = File.ReadAllText(RepoFile("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", "GenerateBill.razor"));
		Check.That("page uses the shared GuestHouseBillMath calculation", page.Contains("GuestHouseBillMath.Compute(GrandTotal, EffectiveDiscount, EffectiveAdvance)"));
		Check.That("Advance Payment input is read-only", page.Contains("readonly value=\"@FormatMoney(EffectiveAdvance)\""));
		Check.That("Round Off input is read-only", page.Contains("readonly value=\"@SignedMoney(EffectiveRoundOff)\""));
		Check.That("no editable binding left for advance", !page.Contains("@bind=\"_advance\""));
		Check.That("no editable binding left for round off", !page.Contains("@bind=\"_roundOff\""));
		Check.That("page sends the calculated round off, not a typed one", page.Contains("RoundOff = EffectiveRoundOff"));
		return Task.CompletedTask;
	}
}
