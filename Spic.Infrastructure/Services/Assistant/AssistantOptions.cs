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

    public bool HasAnthropicKey => !string.IsNullOrWhiteSpace(AnthropicApiKey);
}
