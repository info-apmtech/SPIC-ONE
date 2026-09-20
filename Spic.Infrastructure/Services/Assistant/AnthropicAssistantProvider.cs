using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Core.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Spic.Infrastructure.Services.Assistant;

/// <summary>
/// SPIC AI backed by the Anthropic Messages API. The retrieved library items are
/// the only knowledge the model is given; it is told to answer from them, to cite
/// the items it used by title and to end with a SUGGESTIONS line that we strip and
/// turn into follow-up chips. Any failure (no key, network, HTTP, parse) falls back
/// to the keyword provider, so the assistant always answers something.
/// </summary>
public class AnthropicAssistantProvider : IAssistantProvider
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AssistantOptions _options;
    private readonly KeywordAssistantProvider _fallback;
    private readonly ILogger<AnthropicAssistantProvider> _logger;

    public AnthropicAssistantProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AssistantOptions> options,
        KeywordAssistantProvider fallback,
        ILogger<AnthropicAssistantProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _fallback = fallback;
        _logger = logger;
    }

    public string Name => "anthropic";

    private const string SystemPrompt =
        "You are SPIC AI, the assistant of SPIC's Digital Library for farmers and dealers. " +
        "Answer ONLY from the provided library content. Cite the items you used by title. " +
        "Say plainly when the library does not cover something instead of inventing an answer. " +
        "Keep answers short and practical, use metric units (kg, ha, litres), and write for a reader in the field. " +
        "End your reply with one final line in exactly this form: SUGGESTIONS: question one | question two | question three";

    public async Task<AssistantAnswer> AnswerAsync(AssistantContext ctx, CancellationToken cancellationToken = default)
    {
        if (!_options.HasAnthropicKey)
            return await _fallback.AnswerAsync(ctx, cancellationToken);

        try
        {
            var payload = BuildPayload(ctx);

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("x-api-key", _options.AnthropicApiKey);
            request.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anthropic assistant call failed with {Status}: {Body}", (int)response.StatusCode, Trim(body, 500));
                return await _fallback.AnswerAsync(ctx, cancellationToken);
            }

            var text = ExtractText(body);
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Anthropic assistant returned no text content.");
                return await _fallback.AnswerAsync(ctx, cancellationToken);
            }

            var (answerText, suggestions) = SplitSuggestions(text!);

            var cited = ctx.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Title) &&
                            answerText.Contains(i.Title, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (cited.Count == 0)
                cited = ctx.Items.Take(2).ToList();

            return new AssistantAnswer
            {
                Provider = Name,
                Text = answerText,
                Sources = cited.Select(i => i.ToSource()).ToList(),
                Suggestions = suggestions
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic assistant call failed; falling back to the keyword provider.");
            return await _fallback.AnswerAsync(ctx, cancellationToken);
        }
    }

    // ---------------------------------------------------------------- request

    private sealed record AnthropicMessage(string role, string content);

    private object BuildPayload(AssistantContext ctx)
    {
        var messages = new List<AnthropicMessage>();

        foreach (var turn in ctx.History.TakeLast(10))
        {
            if (string.IsNullOrWhiteSpace(turn.Text)) continue;
            var role = string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
            messages.Add(new AnthropicMessage(role, turn.Text));
        }

        // The Messages API requires the first message to be from the user.
        while (messages.Count > 0 && messages[0].role != "user")
            messages.RemoveAt(0);

        messages.Add(new AnthropicMessage("user", BuildQuestionBlock(ctx)));

        return new
        {
            model = string.IsNullOrWhiteSpace(_options.Model) ? "claude-sonnet-5" : _options.Model,
            max_tokens = _options.MaxTokens <= 0 ? 800 : _options.MaxTokens,
            system = SystemPrompt,
            messages
        };
    }

    private static string BuildQuestionBlock(AssistantContext ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Library content available to you:");
        sb.AppendLine();

        if (ctx.Items.Count == 0)
        {
            sb.AppendLine("(none — the library has nothing matching this question)");
        }
        else
        {
            foreach (var item in ctx.Items)
            {
                sb.AppendLine($"### {item.Title} ({item.Kind})");
                if (!string.IsNullOrWhiteSpace(item.Tags)) sb.AppendLine($"Tags: {item.Tags}");
                if (!string.IsNullOrWhiteSpace(item.ShortDescription)) sb.AppendLine($"Summary: {item.ShortDescription}");
                if (!string.IsNullOrWhiteSpace(item.Overview)) sb.AppendLine($"Overview: {item.Overview}");
                if (!string.IsNullOrWhiteSpace(item.Features)) sb.AppendLine($"Features: {item.Features}");
                if (!string.IsNullOrWhiteSpace(item.Usage)) sb.AppendLine($"Usage: {item.Usage}");
                sb.AppendLine();
            }
        }

        if (ctx.About != null)
            sb.AppendLine($"The user is reading \"{ctx.About.Title}\" — assume the question is about it unless they say otherwise.").AppendLine();

        sb.AppendLine("Question:");
        sb.Append(ctx.Question);
        return sb.ToString();
    }

    // --------------------------------------------------------------- response

    private static string? ExtractText(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return null;

        var sb = new StringBuilder();
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) &&
                type.GetString() == "text" &&
                block.TryGetProperty("text", out var text))
            {
                sb.Append(text.GetString());
            }
        }

        return sb.Length == 0 ? null : sb.ToString().Trim();
    }

    /// <summary>Pulls the trailing "SUGGESTIONS: a | b | c" line out of the reply.</summary>
    private static (string Text, List<string> Suggestions) SplitSuggestions(string text)
    {
        var suggestions = new List<string>();
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var line = lines[i].Trim().TrimStart('*', '_', '-', ' ');
            if (!line.StartsWith("SUGGESTIONS:", StringComparison.OrdinalIgnoreCase)) continue;

            suggestions = line.Substring("SUGGESTIONS:".Length)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.Trim('*', '_', '"', ' '))
                .Where(s => s.Length > 0)
                .Take(3)
                .ToList();

            lines.RemoveAt(i);
            break;
        }

        return (string.Join("\n", lines).Trim(), suggestions);
    }

    private static string Trim(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
}
