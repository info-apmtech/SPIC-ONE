using SPIC.Core.DTOs;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.Assistant;

/// <summary>
/// The always-available SPIC AI provider. It uses no network: the answer is
/// composed from the library items the retriever ranked highest, one short
/// paragraph per item drawn from Overview / Usage / Features, with the items
/// cited as sources. When nothing in the library matches, it says so and points
/// at the three most viewed published items instead.
/// </summary>
public class KeywordAssistantProvider : IAssistantProvider
{
    private readonly LibraryRetriever _retriever;

    public KeywordAssistantProvider(LibraryRetriever retriever) => _retriever = retriever;

    public string Name => "keyword";

    private static readonly string[] Crops =
    {
        "paddy","rice","wheat","sugarcane","cotton","maize","groundnut","banana","tomato","chilli",
        "potato","onion","turmeric","coconut","tea","coffee","pulses","soybean","mustard","millet",
        "sunflower","gram","jute","rubber","cardamom","mango","grapes","brinjal","cabbage","carrot"
    };

    public async Task<AssistantAnswer> AnswerAsync(AssistantContext ctx, CancellationToken cancellationToken = default)
    {
        var answer = new AssistantAnswer { Provider = Name };
        var question = (ctx.Question ?? string.Empty).Trim();

        if (ctx.Items.Count == 0)
        {
            var popular = await _retriever.MostViewedAsync(3, cancellationToken);

            if (popular.Count == 0)
            {
                answer.Text =
                    "The Digital Library has no matching content yet. Once the SPIC team publishes product pages, " +
                    "videos or brochures I can answer from them.";
                return answer;
            }

            answer.Text =
                "The Digital Library has no matching content yet for that question. " +
                "Here is what is most read right now: " +
                string.Join(", ", popular.Select(p => p.Title)) +
                ". Ask me about any of them and I will answer from the published content.";
            answer.Sources = popular.Select(p => p.ToSource()).ToList();
            answer.Suggestions = popular.Take(3).Select(p => $"Tell me about {p.Title}").ToList();
            return answer;
        }

        var crop = DetectCrop(question) ?? "paddy";
        var lines = new List<string>();

        var lead = ctx.About != null
            ? $"Here is what the Digital Library says about {ctx.About.Title}."
            : "Here is what the Digital Library has on that.";
        lines.Add(lead);

        foreach (var item in ctx.Items)
        {
            var body = FirstNonEmpty(item.Overview, item.Usage, item.Features, item.ShortDescription);
            if (string.IsNullOrWhiteSpace(body))
                body = $"A {KindWord(item)} is published in the library; open it for the full details.";

            lines.Add($"{item.Title}: {Snippet(body)}");
        }

        lines.Add("These answers come from the published library content only — open the items below for the full pages.");

        answer.Text = string.Join("\n\n", lines);
        answer.Sources = ctx.Items.Select(i => i.ToSource()).ToList();
        answer.Suggestions = BuildSuggestions(ctx, crop);
        return answer;
    }

    private static List<string> BuildSuggestions(AssistantContext ctx, string crop)
    {
        var top = ctx.Items.FirstOrDefault();
        var second = ctx.Items.Skip(1).FirstOrDefault();
        var suggestions = new List<string>();

        if (top != null)
        {
            suggestions.Add($"How much {top.Title} per acre for {crop}?");
            suggestions.Add($"When should {top.Title} be applied?");
        }

        if (second != null)
            suggestions.Add($"Compare {top!.Title} and {second.Title}");
        else if (top != null)
            suggestions.Add($"Which crops benefit most from {top.Title}?");

        return suggestions.Take(3).ToList();
    }

    private static string? DetectCrop(string question)
    {
        var words = LibraryRetriever.Tokenize(question).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Crops.FirstOrDefault(c => words.Contains(c));
    }

    private static string KindWord(AssistantLibraryItem item) => item.Kind switch
    {
        SPIC.Core.Entities.LibraryContentKind.Video => "video",
        SPIC.Core.Entities.LibraryContentKind.Brochure => "brochure",
        _ => "product page"
    };

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>First two sentences (or ~320 characters) of a body of text.</summary>
    private static string Snippet(string text)
    {
        text = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");

        var sentences = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '.' or '!' or '?')
            {
                sentences++;
                if (sentences >= 2 && i + 1 < text.Length)
                    return text.Substring(0, i + 1);
            }
        }

        return text.Length <= 320 ? text : text.Substring(0, 320).TrimEnd() + "…";
    }
}
