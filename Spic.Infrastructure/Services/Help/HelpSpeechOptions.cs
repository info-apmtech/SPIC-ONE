namespace Spic.Infrastructure.Services.Help;

/// <summary>
/// "Help" section: Azure AI Speech for the Help Desk Listen button. In Azure the key comes from
/// the Help__SpeechKey env var (Key Vault secret help-speech-key) and the region from
/// Help__SpeechRegion; never from appsettings.json. With no key the voice is simply off.
/// </summary>
public sealed class HelpSpeechOptions
{
    public const string SectionName = "Help";

    public string? SpeechKey { get; set; }
    public string? SpeechRegion { get; set; }

    /// <summary>Folder for saved clips; default {ContentRoot}/Uploads/Help/audio (Azure Files in production).</summary>
    public string? AudioCacheRoot { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(SpeechKey) && !string.IsNullOrWhiteSpace(SpeechRegion);
}
