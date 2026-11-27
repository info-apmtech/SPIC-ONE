using System.Globalization;
using System.Text.RegularExpressions;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

/// <summary>Pure helpers of the SPIC AI chat screen (/farmer/assistant, farmer portal Task 5); pinned by the harness.</summary>
public static class FarmerAssistantUi
{
    /// <summary>The message time in IST: "3:05 pm".</summary>
    public static string Time(DateTime utc) =>
        FpUi.ToIst(utc).ToString("h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant();

    /// <summary>"Today" / "Yesterday" / "Earlier" for a conversation, on the IST calendar.</summary>
    public static string Group(DateTime updatedUtc, DateTime utcNow)
    {
        var day = FpUi.ToIst(updatedUtc).Date;
        var today = FpUi.ToIst(utcNow).Date;
        if (day == today) return "Today";
        if (day == today.AddDays(-1)) return "Yesterday";
        return "Earlier";
    }

    /// <summary>The list line under a conversation title: "3 questions · Today, 3:05 pm" / "· 26 Nov".</summary>
    public static string ListLine(int questions, DateTime updatedUtc, DateTime utcNow)
    {
        var count = questions == 1 ? "1 question" : $"{questions} questions";
        var group = Group(updatedUtc, utcNow);
        var when = group switch
        {
            "Today" => $"Today, {Time(updatedUtc)}",
            "Yesterday" => "Yesterday",
            _ => FpUi.ToIst(updatedUtc).ToString("d MMM", CultureInfo.InvariantCulture)
        };
        return $"{count} · {when}";
    }

    /// <summary>"28 of 30 questions left today" / "1 question left today" / "No questions left today".</summary>
    public static string LeftText(int left, int limit) => left switch
    {
        <= 0 => "No questions left today",
        1 => "1 question left today",
        _ when left >= limit => $"{limit} questions a day",
        _ => $"{left} of {limit} questions left today"
    };

    /// <summary>True for the basic (keyword) answer, which the screen labels.</summary>
    public static bool IsBasic(string? provider) => string.Equals(provider, "keyword", StringComparison.OrdinalIgnoreCase);

    /// <summary>The answer as paragraphs for the bubble: markdown marks the model may still add (**, #, leading
    /// "- " bullets as "• ") are cleaned; blank lines split paragraphs; single line breaks stay.</summary>
    public static List<string> Paragraphs(string? text)
    {
        var cleaned = (text ?? "").Replace("\r\n", "\n");
        cleaned = Regex.Replace(cleaned, @"\*\*(.+?)\*\*", "$1");
        cleaned = Regex.Replace(cleaned, @"(?m)^[ \t]{0,3}#{1,6}[ \t]+", "");
        cleaned = Regex.Replace(cleaned, @"(?m)^[ \t]*[-*][ \t]+", "• ");
        return Regex.Split(cleaned, @"\n\s*\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
    }

    /// <summary>The library page of a source.</summary>
    public static string SourceHref(int contentId) => $"/DigitalLibrary/view/{contentId}";

    public static string SourceIcon(LibraryContentKind kind) => kind switch
    {
        LibraryContentKind.Video => "bi-play-circle",
        LibraryContentKind.Brochure => "bi-file-earmark-pdf",
        _ => "bi-box-seam"
    };
}
