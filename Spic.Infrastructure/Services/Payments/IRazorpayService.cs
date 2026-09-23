namespace Spic.Infrastructure.Services.Payments;

public interface IRazorpayService
{
    /// <summary>
    /// Creates a Razorpay Order for the given amount (already computed server-side —
    /// never trust an amount supplied by the browser). Amount must be in the smallest
    /// currency unit (paise for INR).
    /// </summary>
    Task<RazorpayOrderResult> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the HMAC-SHA256 signature Razorpay Checkout returns after a successful
    /// payment, per Razorpay's documented Orders API verification approach:
    /// signature == HMAC_SHA256(order_id + "|" + payment_id, key_secret).
    /// </summary>
    bool VerifySignature(string orderId, string paymentId, string signature);

    /// <summary>
    /// Refunds (fully or partially) an already-captured Razorpay payment via the Refunds
    /// API (POST /v1/payments/{payment_id}/refund). Amount must be in the smallest
    /// currency unit (paise for INR) and server-computed - never trust a client amount.
    /// </summary>
    Task<RazorpayRefundResult> CreateRefundAsync(string paymentId, long amountInPaise, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the current state of a refund already created via CreateRefundAsync
    /// (GET /v1/payments/{payment_id}/refunds/{refund_id}). Read-only - never creates a refund.
    /// Status is Razorpay's "pending", "processed" or "failed".
    /// </summary>
    Task<RazorpayRefundResult> GetRefundAsync(string paymentId, string refundId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a Razorpay webhook: X-Razorpay-Signature == HMAC_SHA256(raw request body,
    /// webhook secret). The body must be the exact raw bytes received, never re-serialized.
    /// </summary>
    bool VerifyWebhookSignature(string rawBody, string? signature);

    /// <summary>
    /// Lists every refund Razorpay holds for a payment (GET /v1/payments/{payment_id}/refunds).
    /// Read-only; used to reconcile an approval whose refund id was never saved locally.
    /// Success = false means the list could not be fetched (never "no refunds").
    /// </summary>
    Task<RazorpayRefundListResult> GetRefundsForPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
}

public class RazorpayOrderResult
{
    public bool Success { get; set; }
    public string? OrderId { get; set; }
    public long AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";

    /// <summary>Raw JSON response from Razorpay, kept for audit (GatewayResponse column).</summary>
    public string? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RazorpayRefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }

    /// <summary>Razorpay refund status - "processed" or "pending" per the Refunds API.</summary>
    public string? Status { get; set; }
    public long AmountInPaise { get; set; }

    /// <summary>Raw JSON response from Razorpay, kept for audit.</summary>
    public string? RawResponse { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// True when the gateway could not be reached or timed out, so Razorpay MAY have
    /// processed the refund anyway. Callers must not blindly retry in that case.
    /// </summary>
    public bool OutcomeUnknown { get; set; }

    /// <summary>Razorpay speed_processed: "instant" (fund transfer) or "normal" (via the payment partner, 5-7 working days per Razorpay).</summary>
    public string? SpeedProcessed { get; set; }

    /// <summary>
    /// Banking partner reference from acquirer_data (e.g. "ARN 1234..."), which the customer can
    /// use to trace the refund with their bank. It is a trace reference, NOT a bank-credit confirmation.
    /// </summary>
    public string? BankReference { get; set; }

    /// <summary>Razorpay payment the refund belongs to (payment_id).</summary>
    public string? PaymentId { get; set; }

    /// <summary>notes.reason sent when the refund was created (identifies which cancellation it was for).</summary>
    public string? NotesReason { get; set; }
}

public class RazorpayRefundListResult
{
    public bool Success { get; set; }
    public List<RazorpayRefundResult> Refunds { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
