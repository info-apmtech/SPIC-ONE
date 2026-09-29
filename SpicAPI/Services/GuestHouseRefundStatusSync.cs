using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
	/// <summary>
	/// Finalises an Admin-approved Guest House cancellation: approval audit, refund record,
	/// booking Cancelled and room allocations released.
	///
	/// Client requirement: booking cancellation refunds are NOT automatic. The refund is
	/// processed manually outside the application, so nothing here calls Razorpay - no refund
	/// is created, polled, reconciled or updated from a webhook. An approved cancellation with
	/// a refund due is recorded with RefundStatus Pending (to be refunded manually).
	///
	/// "Within 2 working days after Admin approval" is the client's business requirement for
	/// the refund; it drives the displayed target date only.
	/// </summary>
	public static class GuestHouseRefundStatusSync
	{
		public const int RefundCreditWorkingDays = 2;
		public const string RefundTimelineMessage = "Refund will be processed manually to the customer's/dealer's original payment source within 2 working days after Admin approval.";
		public const string ManualRefundMethod = "Manual (processed outside the application)";

		/// <summary>Adds working days, skipping Saturdays and Sundays (public holidays are not known to the system).</summary>
		public static DateTime AddWorkingDays(DateTime start, int workingDays)
		{
			var date = start;
			var added = 0;
			while (added < workingDays)
			{
				date = date.AddDays(1);
				if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
					added++;
			}
			return date;
		}

		public sealed record RefundComputation(bool IsPaid, GuestHouseBookingPayment? Payment, decimal PaidAmount, decimal RefundAmount);

		/// <summary>
		/// The amount to be refunded: the snapshot the dealer was shown at request time (from
		/// GuestHouseCancellationHelper), capped at what was captured. Nothing is refundable if no
		/// payment was captured (e.g. Pay After Stay).
		/// </summary>
		public static RefundComputation ComputeRefund(GuestHouseBooking booking, GuestHouseBookingCancellation cancellation)
		{
			var isPaid = booking.PaymentStatus == GuestHousePaymentStatus.Paid;
			var payment = booking.Payments.FirstOrDefault(p => p.PaymentStatus == GuestHousePaymentStatus.Paid)
				?? booking.Payments.FirstOrDefault();
			var paidAmount = isPaid ? (payment?.Amount > 0 ? payment!.Amount : booking.TotalAmount ?? 0) : 0m;
			var refundAmount = isPaid ? Math.Min(Math.Max(0m, cancellation.RefundAmount ?? 0m), paidAmount) : 0m;
			return new RefundComputation(isPaid, payment, paidAmount, refundAmount);
		}

		public enum FinalizeOutcome
		{
			Finalized,         // this call finalised the approval
			AlreadyFinalized   // someone else already approved or rejected it
		}

		/// <summary>
		/// Approves a PendingApproval cancellation: Approved + decision audit,
		/// GuestHouseBookingRefund upsert (manual refund, Pending), booking Cancelled and room
		/// allocations released. Exactly one caller can win: the PendingApproval -> Approved
		/// transition is a conditional UPDATE inside the same transaction as the rest, so a
		/// concurrent approve/reject affects 0 rows. Never calls Razorpay.
		/// </summary>
		public static async Task<FinalizeOutcome> FinalizeApprovalAsync(AppDbContext db, int cancellationId, string? decidedBy)
		{
			var now = DateTime.Now;
			await using var tx = await db.Database.BeginTransactionAsync();

			var won = await db.Set<GuestHouseBookingCancellation>()
				.Where(c => c.Id == cancellationId
					&& c.ApprovalStatus == GuestHouseCancellationApprovalStatus.PendingApproval)
				.ExecuteUpdateAsync(s => s.SetProperty(c => c.ApprovalStatus, GuestHouseCancellationApprovalStatus.Approved));
			if (won == 0)
			{
				await tx.RollbackAsync();
				return FinalizeOutcome.AlreadyFinalized;
			}

			var cancellation = await db.Set<GuestHouseBookingCancellation>()
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.Payments)
				.FirstAsync(c => c.Id == cancellationId);
			var booking = cancellation.GuestHouseBooking!;
			var calc = ComputeRefund(booking, cancellation);
			var refundDue = calc.RefundAmount > 0;
			var refundStatus = refundDue ? GuestHouseRefundStatus.Pending : GuestHouseRefundStatus.Completed;

			cancellation.ApprovalStatus = GuestHouseCancellationApprovalStatus.Approved;
			cancellation.AdminDecisionBy = decidedBy;
			cancellation.AdminDecisionAt = now;
			cancellation.RefundStatus = refundStatus;
			cancellation.RefundAmount = calc.RefundAmount;
			// Client requirement: refunded within 2 working days of Admin approval (display only).
			cancellation.EstimatedRefundDate = refundDue ? AddWorkingDays(now, RefundCreditWorkingDays) : null;
			cancellation.Remarks = refundDue
				? $"Refund of Rs. {calc.RefundAmount:0.00} to be processed manually outside the application."
				: calc.IsPaid ? "No refund due under the cancellation policy." : "No payment was captured for this booking; no refund due.";

			if (calc.IsPaid)
			{
				var refund = await db.Set<GuestHouseBookingRefund>().FirstOrDefaultAsync(r => r.GuestHouseBookingId == booking.Id);
				if (refund == null)
				{
					refund = new GuestHouseBookingRefund { GuestHouseBookingId = booking.Id, CreatedAt = now };
					db.Set<GuestHouseBookingRefund>().Add(refund);
				}
				refund.GuestHouseBookingCancellationId = cancellation.Id;
				refund.OriginalAmount = calc.PaidAmount;
				refund.CancellationCharge = cancellation.CancellationCharge;
				refund.TaxAdjustment = cancellation.TaxAdjustment;
				refund.RefundAmount = calc.RefundAmount;
				refund.RefundMethod = refundDue ? ManualRefundMethod : cancellation.RefundMethod;
				refund.RefundStatus = refundStatus;
				refund.ProcessedAt = refundStatus == GuestHouseRefundStatus.Completed ? now : null;
				refund.UpdatedAt = now;
				// Payment stays Paid: the manual refund happens outside the application.
			}

			// Room release: the booking is cancelled - GetCommittedRoomsByRoomAsync and the Front
			// Office grid exclude Cancelled bookings - and physical allocations are dropped, as
			// Check-Out does.
			if (booking.BookingStatus is GuestHouseBookingStatus.CheckedIn or GuestHouseBookingStatus.Completed)
			{
				// Only reachable if the guest was checked in between the Admin's checks and this save.
				cancellation.Remarks += $" Booking was already {booking.BookingStatus}, so it was not cancelled - review manually.";
			}
			else
			{
				booking.BookingStatus = GuestHouseBookingStatus.Cancelled;
				booking.UpdatedAt = now;
				booking.UpdatedBy = decidedBy;

				var allocations = await db.GuestHouseRoomAllocations
					.Where(a => a.GuestHouseBookingId == booking.Id)
					.ToListAsync();
				db.GuestHouseRoomAllocations.RemoveRange(allocations);
			}

			await db.SaveChangesAsync();
			await tx.CommitAsync();
			return FinalizeOutcome.Finalized;
		}
	}
}
