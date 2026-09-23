using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Spic.Infrastructure.Services.Payments;

/// <summary>
/// Talks to the Razorpay Orders API directly over HttpClient (same "no SDK" convention
/// as AnthropicAssistantProvider) and verifies the Checkout callback signature locally
/// with HMACSHA256 — no third-party package required for either.
///
/// The Key Secret is read only from configuration/environment (RazorpayOptions) and is
/// never logged, never returned to the client, and never persisted anywhere.
/// </summary>
public class RazorpayService : IRazorpayService
{
    private const string OrdersEndpoint = "https://api.razorpay.com/v1/orders";
    private const string PaymentsEndpoint = "https://api.razorpay.com/v1/payments";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayService> _logger;

    public RazorpayService(
        IHttpClientFactory httpClientFactory,
        IOptions<RazorpayOptions> options,
        ILogger<RazorpayService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RazorpayOrderResult> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("Razorpay order creation attempted but Razorpay:KeyId/KeySecret are not configured.");
            return new RazorpayOrderResult { Success = false, ErrorMessage = "Online payment is not configured." };
        }

        if (amountInPaise <= 0)
            return new RazorpayOrderResult { Success = false, ErrorMessage = "Invalid amount." };

        try
        {
            var payload = new
            {
                amount = amountInPaise,
                currency = "INR",
                receipt,
                payment_capture = 1
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, OrdersEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuthValue());

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Razorpay order creation failed with {Status}: {Body}", (int)response.StatusCode, Trim(body, 500));
                return new RazorpayOrderResult { Success = false, ErrorMessage = "The payment gateway could not start this payment.", RawResponse = body };
            }

            using var doc = JsonDocument.Parse(body);
            var orderId = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("Razorpay order response had no order id: {Body}", Trim(body, 500));
                return new RazorpayOrderResult { Success = false, ErrorMessage = "The payment gateway returned an unexpected response.", RawResponse = body };
            }

            return new RazorpayOrderResult
            {
                Success = true,
                OrderId = orderId,
                AmountInPaise = amountInPaise,
                Currency = "INR",
                RawResponse = body
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Razorpay order creation threw.");
            return new RazorpayOrderResult { Success = false, ErrorMessage = "Could not reach the payment gateway. Please try again." };
        }
    }

    public async Task<RazorpayRefundResult> CreateRefundAsync(string paymentId, long amountInPaise, string? notes, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning("Razorpay refund attempted but Razorpay:KeyId/KeySecret are not configured.");
            return new RazorpayRefundResult { Success = false, ErrorMessage = "Online refund is not configured." };
        }

        if (string.IsNullOrWhiteSpace(paymentId))
            return new RazorpayRefundResult { Success = false, ErrorMessage = "Invalid payment reference." };

        if (amountInPaise <= 0)
            return new RazorpayRefundResult { Success = false, ErrorMessage = "Invalid refund amount." };

        try
        {
            var payload = new Dictionary<string, object?>
            {
                ["amount"] = amountInPaise,
                // Instant Refund: Razorpay refunds instantly where it can and otherwise falls
                // back to normal speed by itself (speed_processed in the response/webhooks says which).
                ["speed"] = "optimum"
            };
            if (!string.IsNullOrWhiteSpace(notes))
                payload["notes"] = new { reason = notes };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{PaymentsEndpoint}/{paymentId}/refund")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuthValue());

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Razorpay refund failed for payment {PaymentId} with {Status}: {Body}", paymentId, (int)response.StatusCode, Trim(body, 500));
                // A 4xx is a definite rejection; a 5xx gives no guarantee the refund was not created.
                return new RazorpayRefundResult
                {
                    Success = false,
                    OutcomeUnknown = (int)response.StatusCode >= 500,
                    ErrorMessage = "The payment gateway could not process this refund.",
                    RawResponse = body
                };
            }

            using var doc = JsonDocument.Parse(body);
            var refundId = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var status = doc.RootElement.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(refundId))
            {
                _logger.LogWarning("Razorpay refund response had no refund id for payment {PaymentId}: {Body}", paymentId, Trim(body, 500));
                return new RazorpayRefundResult { Success = false, ErrorMessage = "The payment gateway returned an unexpected response.", RawResponse = body };
            }

            return new RazorpayRefundResult
            {
                Success = true,
                RefundId = refundId,
                Status = status,
                AmountInPaise = amountInPaise,
                SpeedProcessed = ReadString(doc.RootElement, "speed_processed"),
                BankReference = ReadBankReference(doc.RootElement),
                RawResponse = body
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Razorpay refund threw for payment {PaymentId}.", paymentId);
            return new RazorpayRefundResult { Success = false, OutcomeUnknown = true, ErrorMessage = "Could not reach the payment gateway." };
        }
    }

    public async Task<RazorpayRefundResult> GetRefundAsync(string paymentId, string refundId, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            return new RazorpayRefundResult { Success = false, ErrorMessage = "Online refund is not configured." };

        if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(refundId))
            return new RazorpayRefundResult { Success = false, ErrorMessage = "Invalid refund reference." };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{PaymentsEndpoint}/{Uri.EscapeDataString(paymentId)}/refunds/{Uri.EscapeDataString(refundId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuthValue());

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Razorpay refund fetch failed for refund {RefundId} with {Status}: {Body}", refundId, (int)response.StatusCode, Trim(body, 500));
                return new RazorpayRefundResult { Success = false, ErrorMessage = "Could not fetch the refund status.", RawResponse = body };
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            return new RazorpayRefundResult
            {
                Success = true,
                RefundId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : refundId,
                Status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null,
                AmountInPaise = root.TryGetProperty("amount", out var amountProp) && amountProp.TryGetInt64(out var amount) ? amount : 0,
                SpeedProcessed = root.TryGetProperty("speed_processed", out var speedProp) && speedProp.ValueKind == JsonValueKind.String ? speedProp.GetString() : null,
                BankReference = ReadBankReference(root),
                RawResponse = body
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Razorpay refund fetch threw for refund {RefundId}.", refundId);
            return new RazorpayRefundResult { Success = false, OutcomeUnknown = true, ErrorMessage = "Could not reach the payment gateway." };
        }
    }

    // acquirer_data holds the banking partner's reference (ARN, RRN or UTR, depending on the
    // payment method). Often null at first - Razorpay can mark a refund processed before it arrives.
    private static string? ReadBankReference(JsonElement root)
    {
        if (!root.TryGetProperty("acquirer_data", out var acquirer) || acquirer.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var key in new[] { "arn", "rrn", "utr" })
        {
            if (acquirer.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return $"{key.ToUpperInvariant()} {value.GetString()}";
            }
        }
        return null;
    }

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature))
            return false;

        if (!_options.IsConfigured)
        {
            _logger.LogWarning("Razorpay signature verification attempted but Razorpay:KeySecret is not configured.");
            return false;
        }

        var payload = Encoding.UTF8.GetBytes($"{orderId}|{paymentId}");
        var key = Encoding.UTF8.GetBytes(_options.KeySecret);

        using var hmac = new HMACSHA256(key);
        var computedHash = hmac.ComputeHash(payload);
        var computedSignature = Convert.ToHexStringLower(computedHash);

        byte[] expected, actual;
        try
        {
            expected = Encoding.UTF8.GetBytes(computedSignature);
            actual = Encoding.UTF8.GetBytes(signature.Trim());
        }
        catch
        {
            return false;
        }

        // Constant-time comparison: this is a security boundary, never a plain "==".
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public bool VerifyWebhookSignature(string rawBody, string? signature)
    {
        if (string.IsNullOrEmpty(rawBody) || string.IsNullOrWhiteSpace(signature))
            return false;

        if (!_options.IsWebhookConfigured)
        {
            _logger.LogWarning("Razorpay webhook received but Razorpay:WebhookSecret is not configured.");
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var expected = Encoding.UTF8.GetBytes(Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))));
        var actual = Encoding.UTF8.GetBytes(signature.Trim());

        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public static RazorpayRefundResult ParseRefundEntity(JsonElement refund) => new RazorpayRefundResult
    {
        Success = true,
        RefundId = ReadString(refund, "id"),
        Status = ReadString(refund, "status"),
        AmountInPaise = refund.TryGetProperty("amount", out var amountProp) && amountProp.TryGetInt64(out var amount) ? amount : 0,
        SpeedProcessed = ReadString(refund, "speed_processed"),
        BankReference = ReadBankReference(refund),
        PaymentId = ReadString(refund, "payment_id"),
        NotesReason = refund.TryGetProperty("notes", out var notes) && notes.ValueKind == JsonValueKind.Object
            ? ReadString(notes, "reason")
            : null
    };

    public async Task<RazorpayRefundListResult> GetRefundsForPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            return new RazorpayRefundListResult { Success = false, ErrorMessage = "Online refund is not configured." };

        if (string.IsNullOrWhiteSpace(paymentId))
            return new RazorpayRefundListResult { Success = false, ErrorMessage = "Invalid payment reference." };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{PaymentsEndpoint}/{Uri.EscapeDataString(paymentId)}/refunds?count=100");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicAuthValue());

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Razorpay refund list failed for payment {PaymentId} with {Status}: {Body}", paymentId, (int)response.StatusCode, Trim(body, 500));
                return new RazorpayRefundListResult { Success = false, ErrorMessage = "Could not fetch refunds for this payment." };
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return new RazorpayRefundListResult { Success = false, ErrorMessage = "The payment gateway returned an unexpected response." };

            return new RazorpayRefundListResult
            {
                Success = true,
                Refunds = items.EnumerateArray().Select(ParseRefundEntity).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Razorpay refund list threw for payment {PaymentId}.", paymentId);
            return new RazorpayRefundListResult { Success = false, ErrorMessage = "Could not reach the payment gateway." };
        }
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;

    private string BasicAuthValue() =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));

    private static string Trim(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
}
