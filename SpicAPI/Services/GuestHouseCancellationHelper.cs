using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
	/// <summary>
	/// Single source of truth for the Guest House booking cancellation refund calculation,
	/// shared by the dealer-facing CancelBooking preview/request endpoints
	/// (GuestHouseBookingController) and the Admin approval endpoints
	/// (GuestHouseFrontOfficeController), so the amount an Admin approves always matches
	/// the amount the dealer was shown - no separate/duplicated formula.
	///
	/// Reads GuestHouseCancellationPolicy (per Guest House, tiered by HoursBeforeCheckIn)
	/// rather than hard-coding a percentage. If a Guest House has no active policy rows
	/// configured, this falls back to a full refund (0% charge) rather than fabricating
	/// a charge percentage that was never configured anywhere.
	/// </summary>
	public static class GuestHouseCancellationHelper
	{
		public class RefundCalculation
		{
			public int? PolicyId { get; set; }
			public string? PolicyName { get; set; }
			public decimal RefundPercentage { get; set; } = 100m;
			public decimal CancellationChargePercentage { get; set; } = 0m;
			public decimal CancellationCharge { get; set; }
			public decimal TaxAdjustment { get; set; }
			public decimal RefundAmount { get; set; }
			public double HoursUntilCheckIn { get; set; }
			public bool PolicyConfigured { get; set; }
		}

		public static async Task<RefundCalculation> CalculateAsync(AppDbContext db, GuestHouseBooking booking)
		{
			var checkIn = booking.CheckInDate.HasValue
				? booking.CheckInDate.Value.Date.Add(booking.CheckInTime ?? TimeSpan.Zero)
				: (DateTime?)null;
			var hoursUntilCheckIn = checkIn.HasValue ? (checkIn.Value - DateTime.Now).TotalHours : 0;

			var policies = await db.Set<GuestHouseCancellationPolicy>()
				.AsNoTracking()
				.Where(p => p.GuestHouseId == booking.GuestHouseId && p.IsActive)
				.OrderByDescending(p => p.HoursBeforeCheckIn ?? 0)
				.ToListAsync();

			var tier = policies.FirstOrDefault(p => hoursUntilCheckIn >= (p.HoursBeforeCheckIn ?? 0));

			var refundPercent = tier?.RefundPercentage ?? 100m;
			var chargePercent = tier?.CancellationChargePercentage ?? 0m;

			var subTotal = booking.SubTotal ?? 0m;
			var tax = booking.TaxAmount ?? 0m;
			var total = booking.TotalAmount ?? (subTotal + tax);

			var baseCharge = Math.Round(subTotal * chargePercent / 100m, 2, MidpointRounding.AwayFromZero);
			var taxAdjustment = Math.Round(tax * chargePercent / 100m, 2, MidpointRounding.AwayFromZero);
			var refundAmount = Math.Max(0m, total - baseCharge - taxAdjustment);

			return new RefundCalculation
			{
				PolicyId = tier?.Id,
				PolicyName = tier?.PolicyName,
				RefundPercentage = refundPercent,
				CancellationChargePercentage = chargePercent,
				CancellationCharge = baseCharge,
				TaxAdjustment = taxAdjustment,
				RefundAmount = refundAmount,
				HoursUntilCheckIn = hoursUntilCheckIn,
				PolicyConfigured = tier != null
			};
		}
	}
}
