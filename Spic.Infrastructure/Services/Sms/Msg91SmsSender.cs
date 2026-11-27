using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.FarmerPortal;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.Sms;

/// <summary>
/// Sends the OTP through the MSG91 Flow API (POST https://control.msg91.com/api/v5/flow) with a
/// DLT-approved template whose variable is named "otp". The code travels in the JSON body, never in
/// the URL, so HttpClient's request logging cannot leak it; failures are logged with the masked
/// number and the provider's message only.
/// </summary>
public sealed class Msg91SmsSender : ISmsSender
{
    public const string HttpClientName = "Msg91";
    private const string Endpoint = "https://control.msg91.com/api/v5/flow";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Msg91Options _options;
    private readonly ILogger<Msg91SmsSender> _logger;

    public Msg91SmsSender(IHttpClientFactory httpClientFactory, IOptions<SmsOptions> options, ILogger<Msg91SmsSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value.Msg91;
        _logger = logger;
    }

    public string ProviderName => SmsOptions.Msg91Provider;

    public bool DeliversRealSms => true;

    public async Task<SmsSendResult> SendOtpAsync(string phone, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AuthKey) || string.IsNullOrWhiteSpace(_options.TemplateId))
        {
            _logger.LogError("MSG91 is selected (Sms:Provider) but Sms:Msg91:AuthKey or Sms:Msg91:TemplateId is empty.");
            return new SmsSendResult(false, "SMS is not set up yet.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["template_id"] = _options.TemplateId,
            ["short_url"] = "0",
            ["recipients"] = new[] { new Dictionary<string, string> { ["mobiles"] = "91" + phone, ["otp"] = code } }
        };
        if (!string.IsNullOrWhiteSpace(_options.SenderId)) payload["sender"] = _options.SenderId;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("authkey", _options.AuthKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode && IsSuccessBody(body))
                return SmsSendResult.Ok;

            _logger.LogWarning("MSG91 OTP send to {Phone} failed with {Status}: {Message}",
                FarmerPhone.Mask(phone), (int)response.StatusCode, MessageOf(body));
            return new SmsSendResult(false, "SMS could not be sent.");
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // TaskCanceledException (the 10 s timeout) derives from OperationCanceledException.
            _logger.LogWarning("MSG91 OTP send to {Phone} failed: {Error}", FarmerPhone.Mask(phone), ex.GetType().Name);
            return new SmsSendResult(false, "SMS could not be sent.");
        }
    }

    // MSG91 answers 200 with { "type": "success" | "error", "message": "..." }.
    private static bool IsSuccessBody(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("type", out var type)
                   && string.Equals(type.GetString(), "success", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string MessageOf(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return Trim(message.GetString());
        }
        catch (JsonException) { }
        return Trim(body);
    }

    private static string Trim(string? s) => s is null ? "" : s.Length <= 200 ? s : s[..200];
}
