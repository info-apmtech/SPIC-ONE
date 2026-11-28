namespace Spic.Infrastructure.Services.Assistant;

/// <summary>
/// Bound from the "Assistant" configuration section. In Azure the key is supplied
/// as the environment variable Assistant__AnthropicApiKey (Key Vault secret), never
/// from appsettings.json. An empty key means the keyword provider answers.
/// </summary>
public class AssistantOptions
{
    public const string SectionName = "Assistant";

    public string AnthropicApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-5";
    public int MaxTokens { get; set; } = 800;

    /// <summary>Answer length for SPIC AI for farmers (farmer portal Task 5): longer than the library's
    /// because answers in Tamil / Hindi script take more tokens. 0 = 1500.</summary>
    public int FarmerMaxTokens { get; set; } = 1500;

    /// <summary>output_config.effort for farmer chat answers (fix round 1, M7): "low" keeps adaptive thinking short so
    /// it does not eat the 1500-token answer budget. Empty = not sent (use for a model without effort support).</summary>
    public string FarmerEffort { get; set; } = "low";

    /// <summary>Optional Messages API URL (a gateway / proxy, or a local stub in development). Empty = the
    /// Anthropic API (https://api.anthropic.com/v1/messages). The key is sent to this URL.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public bool HasAnthropicKey => !string.IsNullOrWhiteSpace(AnthropicApiKey);
}
