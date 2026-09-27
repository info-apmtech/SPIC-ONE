namespace Spic.Infrastructure.Services.Payments;

/// <summary>
/// Bound from the "Razorpay" configuration section. KeySecret is never committed —
/// in Azure it is supplied as the environment variable Razorpay__KeySecret (Key Vault
/// secret), the same convention as Assistant__AnthropicApiKey. KeyId is not secret
/// (it is handed to the client to open Checkout) but is kept out of source control
/// for the same reason; it can be supplied the same way (Razorpay__KeyId).
/// </summary>
public class RazorpayOptions
{
    public const string SectionName = "Razorpay";

    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Secret entered when creating the webhook in the Razorpay Dashboard; used to validate
    /// X-Razorpay-Signature. Never committed - supplied as Razorpay__WebhookSecret.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(KeyId) && !string.IsNullOrWhiteSpace(KeySecret);

    public bool IsWebhookConfigured => !string.IsNullOrWhiteSpace(WebhookSecret);
}
