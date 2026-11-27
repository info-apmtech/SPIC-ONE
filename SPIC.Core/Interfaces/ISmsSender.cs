namespace SPIC.Core.Interfaces;

/// <summary>
/// Sends the farmer portal's one-time sign-in codes. Chosen by Sms:Provider ("Log" | "Msg91",
/// default "Log"); implementations live in Spic.Infrastructure/Services/Sms. An implementation
/// never logs the code except <c>LogSmsSender</c>, which exists for development only.
/// </summary>
public interface ISmsSender
{
    /// <summary>"Log" or "Msg91".</summary>
    string ProviderName { get; }

    /// <summary>True when the provider really delivers SMS (false for the development log sender).</summary>
    bool DeliversRealSms { get; }

    /// <summary>Sends <paramref name="code"/> to the 10-digit Indian mobile <paramref name="phone"/>.</summary>
    Task<SmsSendResult> SendOtpAsync(string phone, string code, CancellationToken ct = default);
}

public sealed record SmsSendResult(bool Success, string? Error = null)
{
    public static SmsSendResult Ok { get; } = new(true);
}
