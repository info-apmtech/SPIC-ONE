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
