using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Payments;
using SPIC.Core.Entities;

namespace SpicAPI.Services
{
	/// <summary>
	/// Keeps an approved Guest House cancellation's refund state in step with Razorpay.
	///
	/// Primary path: the Razorpay refund.* webhook (RazorpayWebhookController ->
	/// ApplyWebhookRefundAsync) updates the status automatically. Fallback: the refund is
	/// also polled through the Fetch Refund API (IRazorpayService.GetRefundAsync) whenever its
	/// status is looked at - the dealer's Refund Status page, the Admin cancellation list and
	/// the Admin "Check Refund Status" action. Only a refund Razorpay reports as "processed" is
	/// marked Completed - and only then is the booking/payment marked Refunded /
	/// PartiallyRefunded. "pending" stays Processing.
	///
	/// What "processed" means (Razorpay refund entity docs): it is Razorpay's FINAL state for
	/// the refund, i.e. Razorpay has completed its side. It is NOT a confirmation that the
	/// money has reached the customer's bank account - for speed_processed = "normal" Razorpay
	/// documents a further 5-7 working days, and no Razorpay API field or webhook reports the
	/// actual bank credit. acquirer_data (ARN/RRN/UTR) is only a reference the customer can use
	/// to trace the refund with their bank. So Completed here = "processed by Razorpay",
	/// never "bank credited".
	///
	/// "Within 2 working days after Admin approval" is the client's business requirement for
	/// the bank credit, not a Razorpay guarantee; it drives the displayed target date only.
	/// </summary>
	public static class GuestHouseRefundStatusSync
	{
		public const int RefundCreditWorkingDays = 2;
		public const string RefundTimelineMessage = "Refund will be credited to the customer's/dealer's original payment source within 2 working days after Admin approval.";

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

		/// <summary>
		/// Applies a Razorpay refund status to the tracked cancellation/refund/booking/payment.
		/// Returns true when anything changed. Does not call SaveChanges.
		/// </summary>
		public static bool ApplyGatewayStatus(
			GuestHouseBookingCancellation cancellation,
			GuestHouseBookingRefund refund,
			GuestHouseBooking booking,
			GuestHouseBookingPayment? payment,
			string? gatewayStatus,
			DateTime now,
			string? speedProcessed = null,
			string? bankReference = null)
		{
			if (string.Equals(gatewayStatus, "processed", StringComparison.OrdinalIgnoreCase))
			{
				cancellation.RefundStatus = GuestHouseRefundStatus.Completed;
				cancellation.Remarks = $"Razorpay refund {refund.RefundReference} processed by Razorpay"
					+ (string.IsNullOrWhiteSpace(speedProcessed) ? "" : $" ({speedProcessed} speed)")
					+ (string.IsNullOrWhiteSpace(bankReference) ? "; bank reference not yet provided." : $"; bank reference {bankReference}.")
					+ " Credit to the original payment source is not separately confirmed by Razorpay.";
				refund.RefundStatus = GuestHouseRefundStatus.Completed;
				refund.ProcessedAt = now;
				refund.UpdatedAt = now;

				var refundAmount = refund.RefundAmount ?? 0m;
				if (refundAmount > 0)
				{
					var paymentStatus = refundAmount >= (refund.OriginalAmount ?? 0m)
						? GuestHousePaymentStatus.Refunded
						: GuestHousePaymentStatus.PartiallyRefunded;
					booking.PaymentStatus = paymentStatus;
					booking.UpdatedAt = now;
					if (payment != null)
					{
						payment.PaymentStatus = paymentStatus;
						payment.UpdatedAt = now;
					}
				}
				return true;
			}

			if (string.Equals(gatewayStatus, "failed", StringComparison.OrdinalIgnoreCase))
			{
				cancellation.RefundStatus = GuestHouseRefundStatus.Failed;
				cancellation.Remarks = $"Razorpay reported refund {refund.RefundReference} as failed ({now:dd MMM yyyy HH:mm}). No refund is created again automatically: "
					+ "issue it from the Razorpay Dashboard, then use Reconcile Refund (or its webhook links it).";
				refund.RefundStatus = GuestHouseRefundStatus.Failed;
				refund.UpdatedAt = now;
				return true;
			}

			// "pending" (or anything unrecognised): leave it Processing. If Razorpay could not
			// refund instantly it falls back to normal speed - record that, since the 2-working-day
			// target is then unlikely to be met (Razorpay documents 5-7 working days for normal).
			if (string.Equals(speedProcessed, "normal", StringComparison.OrdinalIgnoreCase)
				&& cancellation.Remarks?.Contains("normal speed", StringComparison.OrdinalIgnoreCase) != true)
			{
				cancellation.Remarks = $"Razorpay refund {refund.RefundReference} is processing at normal speed (instant refund not possible); "
					+ "Razorpay documents 5-7 working days for normal refunds.";
				refund.UpdatedAt = now;
				return true;
			}
			return false;
		}

		public enum WebhookOutcome
		{
			Applied,     // state changed and saved
			NoChange,    // known refund, nothing to change (duplicate / out-of-order / already final)
			Ignored,     // not a Guest House refund
			RetryLater   // refund belongs to an approval claimed within ApprovalInFlightWindow - let Razorpay redeliver
		}

		/// <summary>
		/// Applies a refund.* webhook's refund entity. Idempotent by state: a refund is only ever
		/// moved out of Processing once, so duplicate or out-of-order deliveries are no-ops.
		/// For an already finalised approval, booking cancellation / room state is never touched
		/// (refund.processed does not release the room again). Only reconciling an interrupted
		/// approval finalises it - which is when the booking is cancelled and the room released.
		/// </summary>
		public static async Task<WebhookOutcome> ApplyWebhookRefundAsync(AppDbContext db, RazorpayRefundResult entity, string? paymentId)
		{
			if (string.IsNullOrWhiteSpace(entity.RefundId))
				return WebhookOutcome.Ignored;

			var refund = await db.Set<GuestHouseBookingRefund>()
				.FirstOrDefaultAsync(r => r.RefundReference == entity.RefundId);

			if (refund == null)
			{
				// The refund id is only saved when the approval is finalised. An approval that was
				// claimed (PendingApproval + Processing) but never finalised - still running, or its
				// create call timed out / its save failed after Razorpay accepted the refund - is
				// reconciled here from the webhook's own payment id + refund id.
				if (string.IsNullOrWhiteSpace(paymentId))
					return WebhookOutcome.Ignored;

				var stuck = await FindClaimedApprovalAsync(db, paymentId);
				if (stuck == null)
				{
					// A refund issued from the Razorpay Dashboard to replace one that failed after approval.
					var failedApproval = await FindFailedApprovedRefundAsync(db, paymentId);
					if (failedApproval == null)
						return WebhookOutcome.Ignored;
					entity.PaymentId ??= paymentId;
					return await LinkReplacementRefundAsync(db, failedApproval.Id, entity, "Razorpay webhook")
						? WebhookOutcome.Applied
						: WebhookOutcome.NoChange;
				}

				// The approve request may still be between Razorpay's response and its own save;
				// let Razorpay redeliver rather than race it.
				if (stuck.AdminDecisionAt.HasValue && stuck.AdminDecisionAt.Value > DateTime.Now - ApprovalInFlightWindow)
					return WebhookOutcome.RetryLater;

				var result = await ReconcileClaimedApprovalAsync(db, stuck.Id, entity, "Razorpay webhook");
				return result is ReconcileOutcome.Finalized or ReconcileOutcome.MarkedFailed
					? WebhookOutcome.Applied
					: WebhookOutcome.NoChange;
			}

			var cancellation = await db.Set<GuestHouseBookingCancellation>()
				.FirstOrDefaultAsync(c => c.GuestHouseBookingId == refund.GuestHouseBookingId);
			if (cancellation == null
				|| cancellation.ApprovalStatus != GuestHouseCancellationApprovalStatus.Approved
				|| cancellation.RefundStatus != GuestHouseRefundStatus.Processing)
			{
				return WebhookOutcome.NoChange;
			}

			var booking = await db.GuestHouseBookings
				.Include(b => b.Payments)
				.FirstAsync(b => b.Id == refund.GuestHouseBookingId);
			var payment = booking.Payments.FirstOrDefault(p => p.TransactionId == paymentId)
				?? booking.Payments.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.TransactionId));

			if (!ApplyGatewayStatus(cancellation, refund, booking, payment, entity.Status, DateTime.Now, entity.SpeedProcessed, entity.BankReference))
				return WebhookOutcome.NoChange;

			await db.SaveChangesAsync();
			return WebhookOutcome.Applied;
		}

		// =====================================================================
		//  Approval finalisation + reconciliation of interrupted approvals
		// =====================================================================

		/// <summary>
		/// How long after the approval claim an approve request may legitimately still be running
		/// (Razorpay create call has a 30 s timeout). Within this window a webhook/reconcile never
		/// finalises the approval itself - it defers to the running request.
		/// </summary>
		public static readonly TimeSpan ApprovalInFlightWindow = TimeSpan.FromMinutes(2);

		/// <summary>notes.reason sent with the Razorpay refund; also used to recognise it again.</summary>
		public static string RefundNote(string? cancellationReference) => $"Guest House cancellation {cancellationReference}";

		public sealed record RefundComputation(bool IsPaid, GuestHouseBookingPayment? Payment, decimal PaidAmount, decimal RefundAmount);

		/// <summary>
		/// The amount actually refunded: the snapshot the dealer was shown at request time (from
		/// GuestHouseCancellationHelper), capped at what was captured. Nothing is refundable if no
		/// payment was captured (e.g. Pay After Stay). Used by approve and reconciliation alike.
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

		public static long ToPaise(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

		public enum FinalizeOutcome
		{
			Finalized,         // this call finalised the approval
			AlreadyFinalized   // someone else (approve / webhook / reconcile) already did
		}

		/// <summary>
		/// Finalises a CLAIMED approval (PendingApproval + RefundStatus Processing) once the
		/// Razorpay refund is known to have been accepted - or when no refund is due:
		/// Approved + decision audit, GuestHouseBookingRefund upsert, booking Cancelled and room
		/// allocations released. Exactly one caller can win: the PendingApproval -> Approved
		/// transition is a conditional UPDATE inside the same transaction as the rest.
		/// Never calls Razorpay, so it can never create a refund.
		/// </summary>
		public static async Task<FinalizeOutcome> FinalizeApprovalAsync(
			AppDbContext db, int cancellationId, RazorpayRefundResult? gateway, string? decidedBy, string? reconciledVia = null)
		{
			var now = DateTime.Now;
			await using var tx = await db.Database.BeginTransactionAsync();

			var won = await db.Set<GuestHouseBookingCancellation>()
				.Where(c => c.Id == cancellationId
					&& c.ApprovalStatus == GuestHouseCancellationApprovalStatus.PendingApproval
					&& c.RefundStatus == GuestHouseRefundStatus.Processing)
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
			var needsGatewayRefund = calc.RefundAmount > 0;

			if (needsGatewayRefund && string.IsNullOrWhiteSpace(gateway?.RefundId))
				throw new InvalidOperationException("A refund is due but no Razorpay refund id was supplied; the approval cannot be finalised.");

			// The decision time is when the Admin approved (the claim), even if finalised later.
			var decisionAt = cancellation.AdminDecisionAt ?? now;
			var refundStatus = needsGatewayRefund ? GuestHouseRefundStatus.Processing : GuestHouseRefundStatus.Completed;

			cancellation.ApprovalStatus = GuestHouseCancellationApprovalStatus.Approved;
			cancellation.AdminDecisionBy = cancellation.AdminDecisionBy ?? decidedBy;
			cancellation.AdminDecisionAt = decisionAt;
			cancellation.RefundStatus = refundStatus;
			cancellation.RefundAmount = calc.RefundAmount;
			// Client requirement: credited within 2 working days of Admin approval (display only).
			cancellation.EstimatedRefundDate = needsGatewayRefund ? AddWorkingDays(decisionAt, RefundCreditWorkingDays) : null;
			cancellation.Remarks = (needsGatewayRefund
					? $"Razorpay instant refund {gateway!.RefundId} initiated ({gateway.Status}"
						+ (string.IsNullOrWhiteSpace(gateway.SpeedProcessed) ? "" : $", {gateway.SpeedProcessed} speed") + ")."
					: calc.IsPaid ? "No refund due under the cancellation policy." : "No payment was captured for this booking; no refund due.")
				+ (reconciledVia == null ? "" : $" Reconciled via {reconciledVia} on {now:dd MMM yyyy HH:mm} after the approval was interrupted.");

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
				refund.RefundMethod = needsGatewayRefund ? "Razorpay (Original Payment Method)" : cancellation.RefundMethod;
				refund.RefundStatus = refundStatus;
				refund.RefundReference = gateway?.RefundId;
				refund.ProcessedAt = refundStatus == GuestHouseRefundStatus.Completed ? now : null;
				refund.UpdatedAt = now;

				// Payment stays Paid until Razorpay reports the refund as processed.
				if (needsGatewayRefund)
					ApplyGatewayStatus(cancellation, refund, booking, calc.Payment, gateway!.Status, now, gateway.SpeedProcessed, gateway.BankReference);
			}

			// Room release rule: the refund was accepted by Razorpay (or none is due), so the booking
			// is cancelled now - GetCommittedRoomsByRoomAsync and the Front Office grid exclude
			// Cancelled bookings - and physical allocations are dropped, as Check-Out does.
			if (booking.BookingStatus is GuestHouseBookingStatus.CheckedIn or GuestHouseBookingStatus.Completed)
			{
				// Only reachable when an interrupted approval is reconciled after the guest arrived.
				cancellation.Remarks += $" Booking was already {booking.BookingStatus} at reconciliation, so it was not cancelled - review manually.";
			}
			else
			{
				booking.BookingStatus = GuestHouseBookingStatus.Cancelled;
				booking.UpdatedAt = now;
				booking.UpdatedBy = cancellation.AdminDecisionBy ?? decidedBy;

				var allocations = await db.GuestHouseRoomAllocations
					.Where(a => a.GuestHouseBookingId == booking.Id)
					.ToListAsync();
				db.GuestHouseRoomAllocations.RemoveRange(allocations);
			}

			await db.SaveChangesAsync();
			await tx.CommitAsync();
			return FinalizeOutcome.Finalized;
		}

		/// <summary>A claimed-but-not-finalised approval for this Razorpay payment, if any.</summary>
		public static Task<GuestHouseBookingCancellation?> FindClaimedApprovalAsync(AppDbContext db, string paymentId) =>
			db.Set<GuestHouseBookingCancellation>()
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.ApprovalStatus == GuestHouseCancellationApprovalStatus.PendingApproval
					&& c.RefundStatus == GuestHouseRefundStatus.Processing
					&& c.GuestHouseBooking!.Payments.Any(p => p.TransactionId == paymentId));

		public enum ReconcileOutcome
		{
			Finalized,     // Razorpay had accepted the refund - approval finalised with its refund id
			MarkedFailed,  // no refund exists / the refund failed - released for an Admin retry
			NotMatched,    // the Razorpay refund does not belong to this approval (amount/note differ)
			NoChange       // already finalised elsewhere
		}

		/// <summary>
		/// Applies one Razorpay refund (from a webhook or from the payment's refund list) to a
		/// claimed approval, after checking it really is this approval's refund: same payment,
		/// same amount, and (when present) the same notes.reason we sent.
		///   pending/processed -> finalise (booking Cancelled, room released, refund row saved)
		///   failed            -> RefundStatus Failed; booking stays active; Admin may retry
		/// </summary>
		public static async Task<ReconcileOutcome> ReconcileClaimedApprovalAsync(
			AppDbContext db, int cancellationId, RazorpayRefundResult razorpayRefund, string via)
		{
			var cancellation = await db.Set<GuestHouseBookingCancellation>()
				.AsNoTracking()
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.Payments)
				.FirstAsync(c => c.Id == cancellationId);
			var calc = ComputeRefund(cancellation.GuestHouseBooking!, cancellation);

			var sameAmount = razorpayRefund.AmountInPaise == ToPaise(calc.RefundAmount);
			var sameNote = string.IsNullOrWhiteSpace(razorpayRefund.NotesReason)
				|| string.Equals(razorpayRefund.NotesReason, RefundNote(cancellation.CancellationReference), StringComparison.Ordinal);
			var samePayment = calc.Payment?.TransactionId != null
				&& string.Equals(razorpayRefund.PaymentId ?? calc.Payment.TransactionId, calc.Payment.TransactionId, StringComparison.Ordinal);
			if (!sameAmount || !sameNote || !samePayment)
				return ReconcileOutcome.NotMatched;

			if (string.Equals(razorpayRefund.Status, "failed", StringComparison.OrdinalIgnoreCase))
			{
				// Razorpay accepted and then failed the refund: no money moved, so this is the
				// same as a definite create failure - booking stays active, Admin can retry.
				var released = await db.Set<GuestHouseBookingCancellation>()
					.Where(c => c.Id == cancellationId
						&& c.ApprovalStatus == GuestHouseCancellationApprovalStatus.PendingApproval
						&& c.RefundStatus == GuestHouseRefundStatus.Processing)
					.ExecuteUpdateAsync(s => s
						.SetProperty(c => c.RefundStatus, GuestHouseRefundStatus.Failed)
						.SetProperty(c => c.AdminDecisionBy, (string?)null)
						.SetProperty(c => c.AdminDecisionAt, (DateTime?)null)
						.SetProperty(c => c.Remarks, $"Razorpay refund {razorpayRefund.RefundId} failed (reconciled via {via} on {DateTime.Now:dd MMM yyyy HH:mm}). Booking not cancelled; the approval can be retried."));
				return released == 1 ? ReconcileOutcome.MarkedFailed : ReconcileOutcome.NoChange;
			}

			var outcome = await FinalizeApprovalAsync(db, cancellationId, razorpayRefund, cancellation.AdminDecisionBy ?? via, via);
			return outcome == FinalizeOutcome.Finalized ? ReconcileOutcome.Finalized : ReconcileOutcome.NoChange;
		}

		/// <summary>An approved cancellation whose Razorpay refund failed, for this payment, if any.</summary>
		public static Task<GuestHouseBookingCancellation?> FindFailedApprovedRefundAsync(AppDbContext db, string paymentId) =>
			db.Set<GuestHouseBookingCancellation>()
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.ApprovalStatus == GuestHouseCancellationApprovalStatus.Approved
					&& c.RefundStatus == GuestHouseRefundStatus.Failed
					&& c.GuestHouseBooking!.Payments.Any(p => p.TransactionId == paymentId));

		/// <summary>
		/// refund.failed after approval: the booking is already cancelled and the app never refunds
		/// again by itself. The Admin issues the replacement refund from the Razorpay Dashboard;
		/// this links it (same payment + same amount, not the failed refund) to the existing
		/// refund record and resumes normal tracking. Conditional Failed -> Processing update, so it
		/// can only happen once. Never calls Razorpay.
		/// </summary>
		public static async Task<bool> LinkReplacementRefundAsync(AppDbContext db, int cancellationId, RazorpayRefundResult replacement, string via)
		{
			if (string.IsNullOrWhiteSpace(replacement.RefundId)
				|| string.Equals(replacement.Status, "failed", StringComparison.OrdinalIgnoreCase))
				return false;

			var cancellation = await db.Set<GuestHouseBookingCancellation>()
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.Payments)
				.FirstAsync(c => c.Id == cancellationId);
			var refund = await db.Set<GuestHouseBookingRefund>()
				.FirstOrDefaultAsync(r => r.GuestHouseBookingId == cancellation.GuestHouseBookingId);
			if (refund == null || refund.RefundReference == replacement.RefundId)
				return false;

			var booking = cancellation.GuestHouseBooking!;
			var payment = booking.Payments.FirstOrDefault(p => p.TransactionId == replacement.PaymentId)
				?? booking.Payments.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.TransactionId));
			if (payment?.TransactionId == null
				|| !string.Equals(replacement.PaymentId ?? payment.TransactionId, payment.TransactionId, StringComparison.Ordinal)
				|| replacement.AmountInPaise != ToPaise(refund.RefundAmount ?? 0m))
				return false;

			await using var tx = await db.Database.BeginTransactionAsync();
			var won = await db.Set<GuestHouseBookingCancellation>()
				.Where(c => c.Id == cancellationId
					&& c.ApprovalStatus == GuestHouseCancellationApprovalStatus.Approved
					&& c.RefundStatus == GuestHouseRefundStatus.Failed)
				.ExecuteUpdateAsync(s => s.SetProperty(c => c.RefundStatus, GuestHouseRefundStatus.Processing));
			if (won == 0)
			{
				await tx.RollbackAsync();
				return false;
			}

			var now = DateTime.Now;
			var failedRefundId = refund.RefundReference;
			cancellation.RefundStatus = GuestHouseRefundStatus.Processing;
			cancellation.Remarks = $"Replacement Razorpay refund {replacement.RefundId} linked via {via} on {now:dd MMM yyyy HH:mm} (failed refund: {failedRefundId}).";
			refund.RefundReference = replacement.RefundId;
			refund.RefundStatus = GuestHouseRefundStatus.Processing;
			refund.ProcessedAt = null;
			refund.UpdatedAt = now;
			ApplyGatewayStatus(cancellation, refund, booking, payment, replacement.Status, now, replacement.SpeedProcessed, replacement.BankReference);

			await db.SaveChangesAsync();
			await tx.CommitAsync();
			return true;
		}

		public sealed record ManualReconcileResult(bool Changed, string Message);

		/// <summary>
		/// Admin "Reconcile Refund": resolves a claimed-but-unfinalised approval by asking Razorpay
		/// which refunds exist for the payment. Read-only towards Razorpay - never creates a refund.
		/// </summary>
		public static async Task<ManualReconcileResult> ReconcileManuallyAsync(AppDbContext db, IRazorpayService razorpay, int cancellationId)
		{
			var cancellation = await db.Set<GuestHouseBookingCancellation>()
				.AsNoTracking()
				.Include(c => c.GuestHouseBooking!).ThenInclude(b => b.Payments)
				.FirstAsync(c => c.Id == cancellationId);

			if (cancellation.ApprovalStatus == GuestHouseCancellationApprovalStatus.Approved
				&& cancellation.RefundStatus == GuestHouseRefundStatus.Failed)
				return await ReconcileFailedApprovedRefundAsync(db, razorpay, cancellation);

			if (cancellation.ApprovalStatus != GuestHouseCancellationApprovalStatus.PendingApproval
				|| cancellation.RefundStatus != GuestHouseRefundStatus.Processing)
				return new ManualReconcileResult(false, "This request has no interrupted approval or failed refund to reconcile.");

			if (cancellation.AdminDecisionAt.HasValue && cancellation.AdminDecisionAt.Value > DateTime.Now - ApprovalInFlightWindow)
				return new ManualReconcileResult(false, "The approval is still being processed. Please try again in a couple of minutes.");

			var calc = ComputeRefund(cancellation.GuestHouseBooking!, cancellation);

			if (calc.RefundAmount <= 0)
			{
				// No refund was ever due, so nothing could have been created at Razorpay.
				var done = await FinalizeApprovalAsync(db, cancellationId, null, cancellation.AdminDecisionBy, "Admin reconcile");
				return new ManualReconcileResult(done == FinalizeOutcome.Finalized,
					done == FinalizeOutcome.Finalized ? "No refund was due. Cancellation approved and room released." : "Already resolved.");
			}

			var paymentId = calc.Payment?.TransactionId;
			if (string.IsNullOrWhiteSpace(paymentId))
				return new ManualReconcileResult(false, "No Razorpay payment reference is recorded for this booking. Reconcile manually in the Razorpay dashboard.");

			var list = await razorpay.GetRefundsForPaymentAsync(paymentId);
			if (!list.Success)
				return new ManualReconcileResult(false, (list.ErrorMessage ?? "Could not fetch refunds from Razorpay.") + " Nothing was changed.");

			var matches = new List<RazorpayRefundResult>();
			foreach (var r in list.Refunds)
			{
				r.PaymentId ??= paymentId;
				var sameAmount = r.AmountInPaise == ToPaise(calc.RefundAmount);
				var sameNote = string.IsNullOrWhiteSpace(r.NotesReason)
					|| string.Equals(r.NotesReason, RefundNote(cancellation.CancellationReference), StringComparison.Ordinal);
				if (sameAmount && sameNote)
					matches.Add(r);
			}

			// Prefer a live refund over a failed one (a failed attempt may sit beside a later retry).
			var live = matches.Where(r => !string.Equals(r.Status, "failed", StringComparison.OrdinalIgnoreCase)).ToList();
			if (live.Count > 1)
				return new ManualReconcileResult(false, $"Razorpay has {live.Count} matching refunds for this payment. Reconcile manually in the Razorpay dashboard.");

			if (live.Count == 1)
			{
				var outcome = await ReconcileClaimedApprovalAsync(db, cancellationId, live[0], "Admin reconcile");
				return outcome == ReconcileOutcome.Finalized
					? new ManualReconcileResult(true, $"Found Razorpay refund {live[0].RefundId} ({live[0].Status}). Cancellation approved and room released.")
					: new ManualReconcileResult(false, "Already resolved.");
			}

			if (matches.Count > 0)
			{
				var outcome = await ReconcileClaimedApprovalAsync(db, cancellationId, matches[0], "Admin reconcile");
				return new ManualReconcileResult(outcome == ReconcileOutcome.MarkedFailed,
					outcome == ReconcileOutcome.MarkedFailed ? "The Razorpay refund failed. The booking is still active; you can retry the approval." : "Already resolved.");
			}

			if (list.Refunds.Count > 0)
				return new ManualReconcileResult(false, "Razorpay has refunds on this payment that do not match this cancellation. Reconcile manually in the Razorpay dashboard.");

			// Razorpay confirms no refund exists for the payment: nothing was created, so the
			// claim can be released for a normal Admin retry without any risk of a duplicate.
			var releasedCount = await db.Set<GuestHouseBookingCancellation>()
				.Where(c => c.Id == cancellationId
					&& c.ApprovalStatus == GuestHouseCancellationApprovalStatus.PendingApproval
					&& c.RefundStatus == GuestHouseRefundStatus.Processing)
				.ExecuteUpdateAsync(s => s
					.SetProperty(c => c.RefundStatus, GuestHouseRefundStatus.Failed)
					.SetProperty(c => c.AdminDecisionBy, (string?)null)
					.SetProperty(c => c.AdminDecisionAt, (DateTime?)null)
					.SetProperty(c => c.Remarks, $"Razorpay has no refund for payment {paymentId} (checked {DateTime.Now:dd MMM yyyy HH:mm}). Booking not cancelled; the approval can be retried."));
			return new ManualReconcileResult(releasedCount == 1,
				releasedCount == 1 ? "Razorpay has no refund for this payment. The booking is still active; you can retry the approval." : "Already resolved.");
		}

		private static async Task<ManualReconcileResult> ReconcileFailedApprovedRefundAsync(
			AppDbContext db, IRazorpayService razorpay, GuestHouseBookingCancellation cancellation)
		{
			var refund = await db.Set<GuestHouseBookingRefund>()
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.GuestHouseBookingId == cancellation.GuestHouseBookingId);
			var paymentId = cancellation.GuestHouseBooking!.Payments
				.Select(p => p.TransactionId)
				.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
			if (refund == null || string.IsNullOrWhiteSpace(paymentId))
				return new ManualReconcileResult(false, "No Razorpay refund record exists for this booking. Reconcile manually in the Razorpay dashboard.");

			var list = await razorpay.GetRefundsForPaymentAsync(paymentId);
			if (!list.Success)
				return new ManualReconcileResult(false, (list.ErrorMessage ?? "Could not fetch refunds from Razorpay.") + " Nothing was changed.");

			var expectedPaise = ToPaise(refund.RefundAmount ?? 0m);
			var replacements = list.Refunds
				.Where(r => r.RefundId != refund.RefundReference
					&& r.AmountInPaise == expectedPaise
					&& !string.Equals(r.Status, "failed", StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (replacements.Count == 0)
				return new ManualReconcileResult(false,
					$"Razorpay refund {refund.RefundReference} failed and no replacement refund exists. The app does not refund again automatically: "
					+ "issue the refund from the Razorpay Dashboard, then use Reconcile Refund (or wait for its webhook) to link it.");
			if (replacements.Count > 1)
				return new ManualReconcileResult(false, $"Razorpay has {replacements.Count} possible replacement refunds for this payment. Reconcile manually in the Razorpay dashboard.");

			replacements[0].PaymentId ??= paymentId;
			var linked = await LinkReplacementRefundAsync(db, cancellation.Id, replacements[0], "Admin reconcile");
			return linked
				? new ManualReconcileResult(true, $"Linked replacement Razorpay refund {replacements[0].RefundId} ({replacements[0].Status}).")
				: new ManualReconcileResult(false, "Already resolved.");
		}

		/// <summary>
		/// Polls Razorpay for the booking's refund if it is an approved cancellation still in
		/// Processing. Never throws; a gateway error just leaves the stored status unchanged.
		/// </summary>
		public static async Task SyncAsync(AppDbContext db, IRazorpayService razorpay, ILogger logger, int bookingId)
		{
			try
			{
				var cancellation = await db.Set<GuestHouseBookingCancellation>()
					.FirstOrDefaultAsync(c => c.GuestHouseBookingId == bookingId
						&& c.ApprovalStatus == GuestHouseCancellationApprovalStatus.Approved
						&& c.RefundStatus == GuestHouseRefundStatus.Processing);
				if (cancellation == null)
					return;

				var refund = await db.Set<GuestHouseBookingRefund>()
					.FirstOrDefaultAsync(r => r.GuestHouseBookingId == bookingId);
				if (refund == null || string.IsNullOrWhiteSpace(refund.RefundReference))
					return;

				var booking = await db.GuestHouseBookings
					.Include(b => b.Payments)
					.FirstOrDefaultAsync(b => b.Id == bookingId);
				var payment = booking?.Payments.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.TransactionId));
				if (booking == null || payment == null)
					return;

				var result = await razorpay.GetRefundAsync(payment.TransactionId!, refund.RefundReference);
				if (!result.Success)
					return;

				if (ApplyGatewayStatus(cancellation, refund, booking, payment, result.Status, DateTime.Now, result.SpeedProcessed, result.BankReference))
					await db.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Refund status sync failed for booking {BookingId}.", bookingId);
				// Drop any half-applied changes so a later SaveChanges on this context cannot persist them.
				db.ChangeTracker.Clear();
			}
		}
	}
}
