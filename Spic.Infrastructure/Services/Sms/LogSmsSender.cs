using Microsoft.Extensions.Logging;
using SPIC.Core.FarmerPortal;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.Sms;

/// <summary>
/// Development SMS sender: writes the code to the API log instead of sending an SMS. Never used to
/// reach real farmers - in Production FarmerAuthController refuses to send codes with this provider.
/// </summary>
public sealed class LogSmsSender : ISmsSender
{
    private readonly ILogger<LogSmsSender> _logger;

    public LogSmsSender(ILogger<LogSmsSender> logger) => _logger = logger;

    public string ProviderName => SmsOptions.LogProvider;

    public bool DeliversRealSms => false;

    public Task<SmsSendResult> SendOtpAsync(string phone, string code, CancellationToken ct = default)
    {
        _logger.LogInformation("OTP for {Phone}: {Code}", FarmerPhone.Mask(phone), code);
        return Task.FromResult(SmsSendResult.Ok);
    }
}
