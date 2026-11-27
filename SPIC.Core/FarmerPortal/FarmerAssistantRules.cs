using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace SPIC.Core.FarmerPortal;

/// <summary>What SPIC AI is told about the farmer (Task 5). Only what helps the answer: no name, phone,
/// village, GPS or user id ever leaves the API in a prompt.</summary>
public sealed class FarmerAssistantFacts
{
    public IReadOnlyList<string> Crops { get; init; } = Array.Empty<string>();
    public string? District { get; init; }
    public string? State { get; init; }
    public decimal? LandAcres { get; init; }
    public string? Irrigation { get; init; }
    /// <summary>When the latest SPIC soil report was made (UTC or local, shown as a date only).</summary>
    public DateTime? SoilReportDate { get; init; }
    /// <summary>Parameter name and rating word ("Nitrogen", "Low") of the latest soil report. Ratings only, no values.</summary>
    public IReadOnlyList<(string Name, string Rating)> SoilRatings { get; init; } = Array.Empty<(string, string)>();
    /// <summary>The one-line summary of the latest fertilizer advice run (FertilizerAdvisor.Summary).</summary>
    public string? FertilizerSummary { get; init; }
    public DateTime? FertilizerDateUtc { get; init; }
}

/// <summary>
/// SPIC AI for farmers (farmer portal Task 5, docs/farmer-portal.md): the daily cap on the IST day, question
/// validation, the system prompt and farmer context, the history window, the basic (keyword) answer and the
/// starter questions. Pure logic, pinned by SPIC.RegressionHarness/FarmerPortalAssistantTests.
/// </summary>
public static class FarmerAssistantRules
{
    /// <summary>Default questions per farmer per IST day (FarmerPortal:AssistantDailyLimit overrides).</summary>
    public const int DailyLimit = 30;
    public const int MaxQuestionChars = 1000;
    /// <summary>Earlier messages sent with a question (questions and answers, oldest first).</summary>
    public const int HistoryMessages = 10;
    public const int TitleLength = 60;
    /// <summary>Default answer length for farmer answers (Assistant:FarmerMaxTokens overrides).</summary>
    public const int DefaultMaxTokens = 1500;

    public const string Disclaimer =
        "SPIC AI can make mistakes. Check doses on the product label or with your SPIC team before you use them.";

    /// <summary>The first words of every basic (keyword) answer, so it can never pass for a full AI answer.</summary>
    public const string FallbackLabel = "Basic answer from the SPIC library";

    public static readonly TimeSpan IstOffset = TimeSpan.FromMinutes(330);

    // ------------------------------------------------------------------ daily cap

    /// <summary>The UTC instant at which the IST day containing <paramref name="utcNow"/> began (IST midnight).</summary>
    public static DateTime IstDayStartUtc(DateTime utcNow)
    {
        var utc = utcNow.Kind == DateTimeKind.Local ? utcNow.ToUniversalTime() : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var istMidnight = (utc + IstOffset).Date;
        return DateTime.SpecifyKind(istMidnight - IstOffset, DateTimeKind.Utc);
    }

    /// <summary>The next IST midnight in UTC: when the count starts again.</summary>
    public static DateTime NextResetUtc(DateTime utcNow) => IstDayStartUtc(utcNow).AddDays(1);

    /// <summary>True when a question asked at <paramref name="askedUtc"/> counts for the IST day of <paramref name="utcNow"/>.</summary>
    public static bool CountsToday(DateTime askedUtc, DateTime utcNow)
    {
        var start = IstDayStartUtc(utcNow);
        var at = DateTime.SpecifyKind(askedUtc, DateTimeKind.Utc);
        return at >= start && at < start.AddDays(1);
    }

    /// <summary>The configured daily limit (FarmerPortal:AssistantDailyLimit); empty, unreadable or below 1 = 30.</summary>
    public static int Limit(string? configured) =>
        int.TryParse(configured?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : DailyLimit;

    public static bool IsCapped(int usedToday, int limit) => usedToday >= limit;

    public static int Left(int usedToday, int limit) => Math.Max(0, limit - usedToday);

    public static int SecondsUntilReset(DateTime utcNow) =>
        (int)Math.Ceiling((NextResetUtc(utcNow) - DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)).TotalSeconds);

    public static string CapMessage(int limit) =>
        $"You have asked {limit} questions today. That is the most SPIC AI can answer in one day, so please ask again after 12 midnight. " +
        "For urgent help, ask your SPIC team.";

    // ------------------------------------------------------------------ question

    /// <summary>The problem with a question in plain words, or null when it can be asked.</summary>
    public static string? Validate(string? question)
    {
        var q = (question ?? "").Trim();
        if (q.Length == 0) return "Please type your question.";
        if (q.Length > MaxQuestionChars)
            return $"Please keep your question under {MaxQuestionChars} letters. You can ask the rest in a second question.";
        return null;
    }

    /// <summary>Conversation title: the first 60 characters of the first question, spaces collapsed.</summary>
    public static string MakeTitle(string question)
    {
        var cleaned = Regex.Replace((question ?? "").Trim(), @"\s+", " ");
        if (cleaned.Length <= TitleLength) return cleaned;
        return cleaned[..TitleLength].TrimEnd() + "…";
    }

    // ------------------------------------------------------------------ language

    /// <summary>The language name in English for a profile code ("ta" -> "Tamil"); English for unknown codes.</summary>
    public static string LanguageName(string? code) => (code ?? "").Trim().ToLowerInvariant() switch
    {
        "ta" => "Tamil",
        "te" => "Telugu",
        "hi" => "Hindi",
        "mr" => "Marathi",
        "kn" => "Kannada",
        "ml" => "Malayalam",
        _ => "English"
    };

    /// <summary>A known profile language code, else "en".</summary>
    public static string LanguageCode(string? code) =>
        FarmerProfileRules.IsKnownLanguage(code?.Trim().ToLowerInvariant()) ? code!.Trim().ToLowerInvariant() : FarmerProfileRules.DefaultLanguage;

    private static string Script(string language) => language switch
    {
        "Tamil" => " (Tamil script)",
        "Telugu" => " (Telugu script)",
        "Hindi" => " (Devanagari script)",
        "Marathi" => " (Devanagari script)",
        "Kannada" => " (Kannada script)",
        "Malayalam" => " (Malayalam script)",
        _ => ""
    };

    // ------------------------------------------------------------------ prompt

    /// <summary>The system prompt for farmer answers in the farmer's language.</summary>
    public static string SystemPrompt(string? languageCode)
    {
        var language = LanguageName(languageCode);
        var script = Script(language);
        return
            "You are SPIC AI, the farming helper of SPIC, an Indian fertilizer company, inside the SPIC ONE app. " +
            "You answer questions from farmers in India.\n\n" +
            "How to answer:\n" +
            $"- Write your whole answer in {language}{script}, even when the question is in another language. Product names and units may stay as they are written on the pack.\n" +
            "- Use simple, everyday words a farmer understands. If you must use a technical word, explain it in a few words.\n" +
            "- Keep it short: one line that answers the question, then at most 5 short numbered steps.\n" +
            "- Write plain text for a phone chat: no markdown, no tables, no headings.\n" +
            "- Use metric units: kg, g, litre, ml, kg per acre (per hectare too when it helps), degrees Celsius.\n" +
            "- Use the farmer's details (crops, district, soil test ratings, last fertilizer advice) when they help the answer.\n\n" +
            "Safety and honesty:\n" +
            "- Recommend a SPIC product only when it appears in the SPIC library content you are given, by the name written there. Do not name other companies' brands.\n" +
            "- Never invent prices or subsidies. Give a dose only when it is in the library content, the farmer's own fertilizer advice or the product label norms; otherwise say the dose depends on the label or a soil test.\n" +
            "- For any pesticide, fungicide, herbicide or other chemical: tell the farmer to follow the label dose exactly, wear gloves and a mask, not to spray in strong wind or before rain, keep children and animals away, and keep the waiting days before harvest written on the label.\n" +
            "- When you are not sure, or the problem needs someone to see the field (a disease you cannot name with confidence, a big crop loss, a soil problem), say so plainly and suggest the farmer asks their SPIC officer (MDO) through \"My SPIC team\" in the app for a field visit.\n" +
            "- When the library content does not cover the question, you may give general good farming practice, but say that it is general advice.\n" +
            "- Answer only about farming: crops, soil, water, fertilizer, pests, weather and SPIC products. Politely say no to anything else.\n" +
            "- Never ask for phone numbers, Aadhaar, bank details or passwords.\n\n" +
            "End your reply with one final line that starts with the English word SUGGESTIONS: followed by three short questions " +
            $"the farmer might ask next, written in {language}, separated by |";
    }

    /// <summary>The farmer's details as given to the model. Empty facts give a short "nothing on record" block.</summary>
    public static string ContextBlock(FarmerAssistantFacts? facts)
    {
        facts ??= new FarmerAssistantFacts();
        var sb = new StringBuilder();
        sb.AppendLine("About this farmer (use it when it helps; do not ask for more personal details):");

        var crops = facts.Crops.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
        sb.AppendLine(crops.Count > 0 ? $"- Crops: {string.Join(", ", crops)}" : "- Crops: not given");

        var place = string.Join(", ", new[] { string.IsNullOrWhiteSpace(facts.District) ? null : $"{facts.District.Trim()} district", facts.State?.Trim() }
            .Where(p => !string.IsNullOrWhiteSpace(p)));
        if (place.Length > 0) sb.AppendLine($"- Place: {place}, India");

        var land = new List<string>();
        if (facts.LandAcres is > 0) land.Add($"{facts.LandAcres.Value.ToString("0.##", CultureInfo.InvariantCulture)} acres");
        if (!string.IsNullOrWhiteSpace(facts.Irrigation)) land.Add($"water: {facts.Irrigation.Trim()}");
        if (land.Count > 0) sb.AppendLine($"- Farm: {string.Join(", ", land)}");

        var ratings = facts.SoilRatings.Where(r => !string.IsNullOrWhiteSpace(r.Name) && !string.IsNullOrWhiteSpace(r.Rating)).Take(14).ToList();
        if (ratings.Count > 0)
        {
            var when = facts.SoilReportDate is { } d ? $", {d.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}" : "";
            sb.AppendLine($"- Latest SPIC soil test (ratings only{when}): {string.Join(", ", ratings.Select(r => $"{r.Name.Trim()} {r.Rating.Trim()}"))}");
        }
        else
        {
            sb.AppendLine("- SPIC soil test: none on record");
        }

        if (!string.IsNullOrWhiteSpace(facts.FertilizerSummary))
        {
            var when = facts.FertilizerDateUtc is { } f ? $" ({(f + IstOffset).ToString("d MMM yyyy", CultureInfo.InvariantCulture)})" : "";
            sb.AppendLine($"- Last fertilizer advice in the app{when}: {facts.FertilizerSummary.Trim()}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>The rating word for a soil report row: the lab's own label when set, else from the status.</summary>
    public static string SoilRating(string? resultLabel, LabResultStatus status)
    {
        if (!string.IsNullOrWhiteSpace(resultLabel)) return resultLabel.Trim();
        return status switch
        {
            LabResultStatus.Deficient => "Low",
            LabResultStatus.Excess => "High",
            _ => "Medium"
        };
    }

    /// <summary>The last <see cref="HistoryMessages"/> earlier messages, oldest first, starting with a question,
    /// with back-to-back messages of the same role joined (an unanswered question left by a dropped request).</summary>
    public static List<AssistantTurn> History(IEnumerable<AssistantTurn> earlier)
    {
        var merged = new List<AssistantTurn>();
        foreach (var t in earlier)
        {
            if (string.IsNullOrWhiteSpace(t.Text)) continue;
            var role = string.Equals(t.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
            if (merged.Count > 0 && merged[^1].Role == role)
                merged[^1] = new AssistantTurn { Role = role, Text = merged[^1].Text + "\n\n" + t.Text };
            else
                merged.Add(new AssistantTurn { Role = role, Text = t.Text });
        }
        var window = merged.Skip(Math.Max(0, merged.Count - HistoryMessages)).ToList();
        while (window.Count > 0 && window[0].Role != "user") window.RemoveAt(0);
        return window;
    }

    // ------------------------------------------------------------------ basic (keyword) answer

    private static readonly string[] ChemicalWords =
    {
        "pesticide", "insecticide", "fungicide", "herbicide", "weedicide", "spray", "spraying", "pest", "pests",
        "insect", "insects", "worm", "bollworm", "borer", "aphid", "aphids", "mite", "mites", "chemical", "poison"
    };

    /// <summary>True when the question is about pesticides / spraying (the answer adds the label and safety line).</summary>
    public static bool IsChemicalQuestion(string? question)
    {
        var words = Regex.Split((question ?? "").ToLowerInvariant(), @"[^\p{L}\p{Nd}]+").Where(w => w.Length > 0).ToHashSet();
        return ChemicalWords.Any(words.Contains);
    }

    public const string SafetyLine =
        "For any pesticide: follow the label dose exactly, wear gloves and a mask, do not spray in strong wind or before rain, and keep the waiting days before harvest.";

    public const string TeamLine = "For advice for your own field, ask your SPIC team (My SPIC team in the app).";

    /// <summary>
    /// The basic answer used when the full AI answer is not available (no key, or the AI call failed): the
    /// matching library items in short, always starting with <see cref="FallbackLabel"/>.
    /// </summary>
    public static (string Text, List<string> Suggestions) FallbackAnswer(string? question, IReadOnlyList<AssistantLibraryItem> items, string? languageCode, IReadOnlyList<string>? crops = null)
    {
        var lines = new List<string>();
        var english = LanguageCode(languageCode) == "en";

        if (items.Count == 0)
        {
            lines.Add($"{FallbackLabel}: the SPIC library has nothing on this question yet.");
            lines.Add("Please ask your SPIC team (My SPIC team in the app), or ask other farmers in Community near me.");
        }
        else
        {
            lines.Add($"{FallbackLabel}: SPIC AI cannot give a full answer right now, so here is what the SPIC library says.");
            // The library may hold the same item twice (a re-upload): each title once.
            foreach (var item in DistinctByTitle(items).Take(3))
            {
                var body = new[] { item.Overview, item.Usage, item.Features, item.ShortDescription }.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
                lines.Add(string.IsNullOrWhiteSpace(body) ? $"{item.Title}: open it below for the details." : $"{item.Title}: {Snippet(body)}");
            }
            lines.Add(TeamLine);
        }

        if (IsChemicalQuestion(question)) lines.Add(SafetyLine);
        if (!english) lines.Add($"Basic answers are in English only. Full SPIC AI answers are given in {LanguageName(languageCode)}.");

        var suggestions = Starters(crops ?? Array.Empty<string>())
            .Where(s => !string.Equals(s, question?.Trim(), StringComparison.OrdinalIgnoreCase))
            .Take(3).ToList();
        return (string.Join("\n\n", lines), suggestions);
    }

    /// <summary>Library items with each title once, in rank order (sources and the basic answer).</summary>
    public static IEnumerable<AssistantLibraryItem> DistinctByTitle(IEnumerable<AssistantLibraryItem> items) =>
        items.GroupBy(i => (i.Title ?? "").Trim(), StringComparer.OrdinalIgnoreCase).Select(g => g.First());

    /// <summary>First two sentences (or about 320 characters) of a text.</summary>
    public static string Snippet(string text)
    {
        text = Regex.Replace(text.Trim(), @"\s+", " ");
        var sentences = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '.' or '!' or '?')
            {
                sentences++;
                if (sentences >= 2 && i + 1 < text.Length) return text[..(i + 1)];
            }
        }
        return text.Length <= 320 ? text : text[..320].TrimEnd() + "…";
    }

    // ------------------------------------------------------------------ starters

    private static readonly Dictionary<string, string[]> CropStarters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Paddy"] = new[] { "When should I give urea to my paddy?", "Why are my paddy leaves turning yellow?" },
        ["Rice"] = new[] { "When should I give urea to my paddy?", "Why are my paddy leaves turning yellow?" },
        ["Cotton"] = new[] { "How do I control pink bollworm in cotton safely?", "What fertilizer does cotton need at flowering?" },
        ["Maize"] = new[] { "What fertilizer does maize need at knee height?", "How do I control fall armyworm in maize safely?" },
        ["Groundnut"] = new[] { "When should I apply gypsum to groundnut?", "Why are my groundnut leaves turning yellow?" },
        ["Banana"] = new[] { "How much fertilizer does one banana plant need?", "How often should I water banana in summer?" },
        ["Coconut"] = new[] { "Why are my coconut nuts falling early?", "How much fertilizer does one coconut palm need?" },
        ["Sugarcane"] = new[] { "When should I top-dress sugarcane?", "How do I control early shoot borer in sugarcane safely?" },
        ["Chilli"] = new[] { "Why are my chilli leaves curling?", "What fertilizer does chilli need after transplanting?" },
        ["Tomato"] = new[] { "Why are my tomato fruits cracking?", "What fertilizer does tomato need at flowering?" },
        ["Onion"] = new[] { "When should I stop watering onion before harvest?", "What fertilizer does onion need?" },
        ["Turmeric"] = new[] { "When should I earth up turmeric?", "What fertilizer does turmeric need?" },
        ["Black gram"] = new[] { "Does black gram need nitrogen fertilizer?", "How do I control yellow mosaic in black gram?" },
        ["Green gram"] = new[] { "Does green gram need nitrogen fertilizer?", "How do I control yellow mosaic in green gram?" },
        ["Wheat"] = new[] { "When should I give the first irrigation to wheat?", "What fertilizer does wheat need?" }
    };

    private static readonly string[] GeneralStarters =
    {
        "How do I read my soil test report?",
        "How should I apply DAP correctly?",
        "How can I make my soil healthier?",
        "How do I spray pesticide safely?"
    };

    /// <summary>Up to four starter questions: two for each of the farmer's first crops, then general ones.</summary>
    public static List<string> Starters(IEnumerable<string> crops)
    {
        var result = new List<string>();
        foreach (var crop in crops.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(2))
        {
            if (CropStarters.TryGetValue(crop, out var list)) result.AddRange(list);
            else result.Add($"What fertilizer does my {crop.ToLowerInvariant()} crop need?");
        }
        foreach (var g in GeneralStarters)
        {
            if (result.Count >= 4) break;
            result.Add(g);
        }
        return result.Distinct().Take(4).ToList();
    }
}
