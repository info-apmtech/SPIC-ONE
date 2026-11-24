using SPIC.Core.Entities;

namespace SpicAPI.Services
{
	/// <summary>
	/// The advance payment of a booking, derived ONLY from its recorded payment rows - never from a value
	/// typed or posted by a client. Same definition the bill draft has always used: money actually Paid
	/// by a non-desk method on or before check-in. Pending / Failed / Refunded rows are never counted, the
	/// Late Check-Out desk settlement and Pay After Stay desk collections are not an advance, and a
	/// payment that appears twice under one gateway transaction id is counted once.
	/// </summary>
	public static class GuestHouseVerifiedAdvance
	{
		public static decimal Compute(GuestHouseBooking booking)
		{
			if (!booking.ActualCheckInAt.HasValue)
				return 0m;

			var counted = booking.Payments
				.Where(GuestHouseLateCheckOut.IsOriginalPayment)
				.Where(p => p.PaymentMethod != GuestHousePaymentMethod.PayAfterStay
					&& p.PaymentStatus == GuestHousePaymentStatus.Paid
					&& p.PaymentDate.HasValue
					&& p.PaymentDate.Value <= booking.ActualCheckInAt.Value)
				.OrderBy(p => p.Id)
				.ToList();

			// De-duplicate rows that carry the same non-empty gateway transaction id.
			var unique = counted
				.GroupBy(p => string.IsNullOrWhiteSpace(p.TransactionId) ? $"row:{p.Id}:{p.GetHashCode()}" : $"txn:{p.TransactionId!.Trim()}")
				.Select(g => g.First());

			return Math.Max(0m, unique.Sum(p => p.Amount));
		}
	}
}
