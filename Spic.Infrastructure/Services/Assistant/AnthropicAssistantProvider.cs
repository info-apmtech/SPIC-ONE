using Microsoft.Extensions.Options;
using SPIC.Core.Interfaces;
using System.Text;

namespace Spic.Infrastructure.Services.Assistant;

/// <summary>
/// SPIC AI backed by the Anthropic Messages API. The retrieved library items are
/// the only knowledge the model is given; it is told to answer from them, to cite
/// the items it used by title and to end with a SUGGESTIONS line that we strip and
/// turn into follow-up chips. Any failure (no key, network, HTTP, refusal, parse) falls
/// back to the keyword provider, so the assistant always answers something.
/// Farmer portal Task 5: a context in <see cref="AssistantMode.Farmer"/> brings its own
/// system prompt and farmer context block; the HTTP call itself is
/// <see cref="AnthropicMessagesClient"/> (shared with the leaf-photo check).
/// </summary>
public class AnthropicAssistantProvider : IAssistantProvider
{
    private readonly AnthropicMessagesClient _client;
    private readonly AssistantOptions _options;
    private readonly KeywordAssistantProvider _fallback;

    public AnthropicAssistantProvider(
        AnthropicMessagesClient client,
        IOptions<AssistantOptions> options,
        KeywordAssistantProvider fallback)
    {
        _client = client;
        _options = options.Value;
        _fallback = fallback;
    }

    public string Name => "anthropic";

    public const string SystemPrompt =
        "You are SPIC AI, the assistant of SPIC's Digital Library for farmers and dealers. " +
        "Answer ONLY from the provided library content. Cite the items you used by title. " +
        "Say plainly when the library does not cover something instead of inventing an answer. " +
        "Keep answers short and practical, use metric units (kg, ha, litres), and write for a reader in the field. " +
        "End your reply with one final line in exactly this form: SUGGESTIONS: question one | question two | question three";

    public async Task<AssistantAnswer> AnswerAsync(AssistantContext ctx, CancellationToken cancellationToken = default)
    {
        if (!_client.IsConfigured)
            return await _fallback.AnswerAsync(ctx, cancellationToken);

        // AnthropicMessagesClient logs every failure (status, refusal, no text, exception) and never throws for
        // them; the keyword provider answers instead, so the assistant always answers something.
        var result = await _client.SendAsync(BuildRequest(ctx, _options), cancellationToken);
        if (!result.Ok || string.IsNullOrWhiteSpace(result.Text))
            return await _fallback.AnswerAsync(ctx, cancellationToken);

        var (answerText, suggestions) = SplitSuggestions(result.Text!);

        var cited = ctx.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Title) &&
                        answerText.Contains(i.Title, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (cited.Count == 0)
            cited = ctx.Items.Take(2).ToList();

        // Farmer mode: an item the library holds twice is linked once (the library chat is unchanged).
        if (ctx.Mode == AssistantMode.Farmer)
            cited = SPIC.Core.FarmerPortal.FarmerAssistantRules.DistinctByTitle(cited).ToList();

        return new AssistantAnswer
        {
            Provider = Name,
            Text = answerText,
            Sources = cited.Select(i => i.ToSource()).ToList(),
            Suggestions = suggestions
        };
    }

    // ---------------------------------------------------------------- request

    /// <summary>
    /// The Messages API request for one question: the context's own system prompt (farmer mode) or the library
    /// prompt, the last 10 earlier messages (starting with a question; back-to-back same-role messages joined,
    /// which the library never produces), then the question block. Public for the harness.
    /// </summary>
    public static AnthropicRequest BuildRequest(AssistantContext ctx, AssistantOptions options)
    {
        // (role, text) pairs first; back-to-back same-role messages are joined.
        var turns = new List<(string Role, string Text)>();
        void Add(string role, string text)
        {
            if (turns.Count > 0 && turns[^1].Role == role) turns[^1] = (role, turns[^1].Text + "\n\n" + text);
            else turns.Add((role, text));
        }

        foreach (var turn in ctx.History.TakeLast(10))
        {
            if (string.IsNullOrWhiteSpace(turn.Text)) continue;
            Add(string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user", turn.Text);
        }

        // The Messages API requires the first message to be from the user.
        while (turns.Count > 0 && turns[0].Role != "user")
            turns.RemoveAt(0);

        // The question block is a user message (a trailing unanswered question is joined to it).
        Add("user", BuildQuestionBlock(ctx));

        var maxTokens = ctx.MaxTokens is > 0 ? ctx.MaxTokens.Value : (options.MaxTokens <= 0 ? 800 : options.MaxTokens);
        return new AnthropicRequest(
            string.IsNullOrWhiteSpace(ctx.SystemPrompt) ? SystemPrompt : ctx.SystemPrompt!,
            turns.Select(t => AnthropicMessage.Text(t.Role, t.Text)).ToList(),
            maxTokens);
    }

    public static string BuildQuestionBlock(AssistantContext ctx)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(ctx.ContextBlock))
        {
            sb.AppendLine(ctx.ContextBlock!.Trim());
            sb.AppendLine();
        }

        sb.AppendLine(ctx.Mode == AssistantMode.Farmer ? "SPIC library content available to you:" : "Library content available to you:");
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

    /// <summary>Pulls the trailing "SUGGESTIONS: a | b | c" line out of the reply.</summary>
    public static (string Text, List<string> Suggestions) SplitSuggestions(string text)
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
}
