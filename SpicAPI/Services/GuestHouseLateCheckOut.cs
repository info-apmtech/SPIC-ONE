using SPIC.Core.Entities;

namespace SpicAPI.Services
{
	/// <summary>
	/// Client-approved late check-out policy (single source of truth, pure and clock-free):
	///
	///   * every scheduled check-out has a 1-hour free grace period;
	///   * once that hour is exceeded, one full additional day's room rate is charged;
	///   * every further 24-hour period gets its own 1-hour grace - exceeding it adds another full day.
	///
	/// With L = actual - scheduled, the number of charged days is the number of thresholds
	/// T(k) = (k-1) x 24h + 1h (k = 1, 2, 3 ...) that L strictly exceeds, i.e.
	/// ceil((L - 1h) / 24h) for L &gt; 1h, else 0. Computed on exact ticks - no rounded hours.
	/// </summary>
	public static class GuestHouseLateCheckOut
	{
		public static readonly TimeSpan GracePeriod = TimeSpan.FromHours(1);
		public static readonly TimeSpan Period = TimeSpan.FromHours(24);

		/// <summary>Booking creation falls back to 12:00 when no check-out time was given; so do we.</summary>
		public static readonly TimeSpan DefaultCheckOutTime = TimeSpan.FromHours(12);

		/// <summary>Reference prefix of the Late Check-Out settlement row in GuestHouseBookingPayments.</summary>
		public const string PaymentReferencePrefix = "LATE-CHECKOUT-";

		public const string BillLineDescription = "Late Check-Out Charge";

		public sealed record Result(
			DateTime ScheduledCheckOutAt,
			DateTime ActualCheckOutAt,
			TimeSpan GracePeriod,
			TimeSpan LateDuration,
			bool WithinGrace,
			int AdditionalDays,
			decimal DailyRate,
			decimal Amount,
			decimal CgstPercent,
			decimal SgstPercent,
			decimal CgstAmount,
			decimal SgstAmount,
			decimal GstAmount,
			decimal Total)
		{
			public bool HasCharge => AdditionalDays > 0;
		}

		public static DateTime ScheduledCheckOutAt(GuestHouseBooking booking) =>
			booking.CheckOutDate!.Value.Date.Add(booking.CheckOutTime ?? DefaultCheckOutTime);

		/// <summary>Number of additional days chargeable for a given lateness (exact, tick based).</summary>
		public static int AdditionalDays(TimeSpan late)
		{
			if (late <= GracePeriod) return 0;
			var excess = (late - GracePeriod).Ticks;
			return (int)((excess + Period.Ticks - 1) / Period.Ticks);
		}

		/// <summary>
		/// Late charge of a booking at <paramref name="actualCheckOutAt"/>. Always uses the room rate
		/// snapshotted on the booking (never the room master) and the booking's own persisted GST rate.
		/// Complimentary (free Employee Stay) bookings are never charged.
		/// </summary>
		public static Result Calculate(GuestHouseBooking booking, DateTime actualCheckOutAt)
		{
			var complimentary = booking.Payments.Any(p => p.PaymentMethod == GuestHousePaymentMethod.Complimentary);
			var rooms = Math.Max(1, booking.NumberOfRooms ?? 1);
			var subTotal = booking.SubTotal ?? 0m;
			var gstPercent = subTotal > 0m ? Math.Round(((booking.TaxAmount ?? 0m) / subTotal) * 100m, 2) : 0m;
			return Calculate(ScheduledCheckOutAt(booking), actualCheckOutAt, complimentary ? 0m : booking.RoomPrice * rooms, gstPercent);
		}

		/// <param name="dailyRate">Full room charge for one additional day (room rate x rooms).</param>
		/// <param name="gstPercent">Total GST %, split equally into CGST + SGST like the bill lines.</param>
		public static Result Calculate(DateTime scheduled, DateTime actual, decimal dailyRate, decimal gstPercent)
		{
			var late = actual > scheduled ? actual - scheduled : TimeSpan.Zero;
			var days = dailyRate > 0m ? AdditionalDays(late) : 0;

			var amount = Math.Round(days * dailyRate, 2, MidpointRounding.AwayFromZero);
			var half = Math.Round(gstPercent / 2m, 2);
			var cgst = Math.Round(amount * (half / 100m), 2, MidpointRounding.AwayFromZero);
			var sgst = Math.Round(amount * (half / 100m), 2, MidpointRounding.AwayFromZero);

			return new Result(scheduled, actual, GracePeriod, late, late <= GracePeriod, days, dailyRate,
				amount, half, half, cgst, sgst, cgst + sgst, amount + cgst + sgst);
		}

		/// <summary>The settlement row recorded for a booking's late charge, if any.</summary>
		public static GuestHouseBookingPayment? SettlementRow(GuestHouseBooking booking) =>
			booking.Payments.FirstOrDefault(p => p.PaymentReference != null
				&& p.PaymentReference.StartsWith(PaymentReferencePrefix, StringComparison.Ordinal));

		/// <summary>Late charge already collected (0 when none).</summary>
		public static decimal Settled(GuestHouseBooking booking)
		{
			var row = SettlementRow(booking);
			return row != null && row.PaymentStatus == GuestHousePaymentStatus.Paid ? row.Amount : 0m;
		}

		/// <summary>Rows that are the booking's real (original) payment - excludes the late-charge row.</summary>
		public static bool IsOriginalPayment(GuestHouseBookingPayment p) =>
			p.PaymentReference == null || !p.PaymentReference.StartsWith(PaymentReferencePrefix, StringComparison.Ordinal);
	}
}
