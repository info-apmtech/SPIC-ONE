using Microsoft.Extensions.Options;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Services.Assistant;

namespace SpicAPI.Services
{
    /// <summary>The vision answer, or why there is none (the caller then gives the basic colour check).</summary>
    public sealed record LeafVisionOutcome(LeafDiagnosisResult? Result, string? Failure);

    /// <summary>
    /// Farmer portal Task 6: the leaf photo to Claude vision through the shared <see cref="AnthropicMessagesClient"/>
    /// (same key, model, endpoint and effort as SPIC AI for farmers: Assistant:AnthropicApiKey / Model / Endpoint /
    /// FarmerEffort). Sends only the re-encoded photo (no EXIF), the crop, the farmer's note on which leaves and the
    /// colour metrics; asks for structured JSON (<see cref="LeafVision.Schema"/>) and validates it with
    /// <see cref="LeafVision.Parse"/>. Never throws for an API problem. The caller must have taken one unit of the
    /// daily AI budget (<see cref="IFarmerAiBudget"/>, kind LeafCheck) first.
    /// </summary>
    public class LeafVisionService
    {
        private readonly AnthropicMessagesClient _client;
        private readonly AssistantOptions _options;
        private readonly ILogger<LeafVisionService> _log;

        public LeafVisionService(AnthropicMessagesClient client, IOptions<AssistantOptions> options, ILogger<LeafVisionService> log)
        {
            _client = client;
            _options = options.Value;
            _log = log;
        }

        /// <summary>The request the service sends (public for the harness).</summary>
        public static AnthropicRequest BuildRequest(byte[] jpeg, string crop, string? leafAgeHint, LeafMetrics metrics, string languageName, string? effort) =>
            new(
                LeafVision.SystemPrompt(languageName),
                new[]
                {
                    new AnthropicMessage("user", new object[]
                    {
                        AnthropicContent.ImageBase64("image/jpeg", Convert.ToBase64String(jpeg)),
                        AnthropicContent.Text(LeafVision.UserText(crop, leafAgeHint, metrics))
                    })
                },
                LeafVision.MaxTokens,
                TimeoutSeconds: 60,
                Effort: string.IsNullOrWhiteSpace(effort) ? null : effort,
                OutputSchema: LeafVision.Schema);

        public async Task<LeafVisionOutcome> DiagnoseAsync(byte[] jpeg, string crop, string? leafAgeHint, LeafMetrics metrics, string languageName, CancellationToken ct)
        {
            if (!_client.IsConfigured) return new(null, "not configured");
            var reply = await _client.SendAsync(BuildRequest(jpeg, crop, leafAgeHint, metrics, languageName, _options.FarmerEffort), ct);
            if (!reply.Ok) return new(null, reply.Error ?? "failed");
            var parsed = LeafVision.Parse(reply.Text);
            if (parsed is null)
            {
                _log.LogWarning("Leaf check: the vision answer was not valid JSON for the schema (stop_reason {StopReason}); basic colour check used.", reply.StopReason);
                return new(null, "unreadable");
            }
            return new(parsed, null);
        }
    }
}
