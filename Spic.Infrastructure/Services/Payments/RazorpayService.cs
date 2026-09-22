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

    private string BasicAuthValue() =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));

    private static string Trim(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
}
