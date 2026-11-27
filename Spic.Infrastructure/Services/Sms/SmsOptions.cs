namespace Spic.Infrastructure.Services.Sms;

/// <summary>
/// "Sms" configuration section. Provider = "Log" (default, development: the code is written to the
/// API log) or "Msg91". The MSG91 auth key is a secret: supply it as the environment variable
/// Sms__Msg91__AuthKey (Key Vault in Azure), never in appsettings.json.
/// </summary>
public sealed class SmsOptions
{
    public const string SectionName = "Sms";
    public const string LogProvider = "Log";
    public const string Msg91Provider = "Msg91";

    public string? Provider { get; set; }
    public Msg91Options Msg91 { get; set; } = new();

    /// <summary>The configured provider, "Log" when empty or unknown.</summary>
    public string EffectiveProvider =>
        string.Equals(Provider?.Trim(), Msg91Provider, StringComparison.OrdinalIgnoreCase) ? Msg91Provider : LogProvider;
}

public sealed class Msg91Options
{
    public string? AuthKey { get; set; }
    /// <summary>The DLT-approved flow template; its OTP variable must be named "otp" (##otp##).</summary>
    public string? TemplateId { get; set; }
    /// <summary>Optional 6-letter DLT sender id; the template's own sender is used when empty.</summary>
    public string? SenderId { get; set; }
}
