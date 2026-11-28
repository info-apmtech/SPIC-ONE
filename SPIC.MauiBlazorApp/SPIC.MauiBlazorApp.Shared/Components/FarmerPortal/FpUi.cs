using System.Globalization;
using System.Text.RegularExpressions;

namespace SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

/// <summary>Nutrient / value zone used by FpStatus, FpZoneSlider and FpBigNumber.</summary>
public enum FpLevel
{
    Unknown,
    Low,
    Medium,
    High
}

/// <summary>Accent hue of an icon circle, tile, card header or chip (CSS class fp-hue-*).</summary>
public enum FpHue
{
    Green,
    Gold,
    Soil,
    Sky,
    Violet,
    Teal,
    Rose,
    Neutral
}

/// <summary>FpCard look: white (Default), warm gold (Highlight), green tint (Tinted), no padding (Flush).</summary>
public enum FpCardVariant
{
    Default,
    Highlight,
    Tinted,
    Flush
}

/// <summary>FpSkeleton shape.</summary>
public enum FpSkeletonVariant
{
    Home,
    Hero,
    Tiles,
    Cards,
    Form
}

/// <summary>Inline SVG illustrations drawn by FpArt (empty, error and brand panels).</summary>
public enum FpArtKind
{
    Sprout,
    Field,
    Offline,
    Search,
    Team
}

/// <summary>
/// Pure helpers behind the farmer portal design system (docs/farmer-portal.md, "Design system").
/// No Blazor or JS here, so SPIC.RegressionHarness checks them (FarmerPortalUiTests).
/// </summary>
public static partial class FpUi
{
    /// <summary>India Standard Time is UTC+05:30 all year (no daylight saving).</summary>
    public static readonly TimeSpan IstOffset = TimeSpan.FromMinutes(330);

    /// <summary>The IST wall-clock time for a UTC instant. A Local / Unspecified value is treated as UTC
    /// only when it is Unspecified; Local is converted to UTC first.</summary>
    public static DateTime ToIst(DateTime utc)
    {
        var u = utc.Kind == DateTimeKind.Local ? utc.ToUniversalTime() : utc;
        return DateTime.SpecifyKind(u, DateTimeKind.Unspecified) + IstOffset;
    }

    /// <summary>
    /// Time-of-day greeting in IST: 05:00-11:59 "Good morning", 12:00-16:59 "Good afternoon",
    /// 17:00-04:59 "Good evening".
    /// </summary>
    public static string Greeting(DateTime utcNow)
    {
        var hour = ToIst(utcNow).Hour;
        return hour switch
        {
            >= 5 and < 12 => "Good morning",
            >= 12 and < 17 => "Good afternoon",
            _ => "Good evening"
        };
    }

    /// <summary>"Saturday, 28 November" in IST, for the home hero.</summary>
    public static string IstDateLine(DateTime utcNow) =>
        ToIst(utcNow).ToString("dddd, d MMMM", CultureInfo.InvariantCulture);

    /// <summary>
    /// The name to greet with: the first word that is not an initial ("K. Murugan" and "Murugan K" both
    /// give "Murugan"); the whole name when every word is an initial; "" for an empty name.
    /// </summary>
    public static string ShortName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";
        var words = fullName.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var first = words.FirstOrDefault(w => w.Length > 1);
        return first ?? fullName.Trim();
    }

    /// <summary>One or two upper-case initials for an avatar circle ("Murugan K" gives "MK").</summary>
    public static string Initials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var words = fullName.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var letters = words
            .Select(w => w.FirstOrDefault(char.IsLetterOrDigit))
            .Where(c => c != default)
            .Take(2)
            .Select(char.ToUpperInvariant)
            .ToArray();
        return letters.Length == 0 ? "?" : new string(letters);
    }

    [GeneratedRegex(@"(?<![0-9])[0-9]{6}(?![0-9])")]
    private static partial Regex SixDigitRun();

    /// <summary>
    /// What the six OTP boxes keep from typed or pasted text: ASCII digits only, at most
    /// <paramref name="length"/>. When pasted text holds a run of exactly <paramref name="length"/>
    /// digits ("Your SPIC ONE code is 482913. Valid 5 min"), that run wins over the other digits.
    /// Non-ASCII digits (Devanagari, Tamil) are dropped, matching FarmerOtpRules.
    /// </summary>
    public static string NormaliseOtp(string? raw, int length = 6)
    {
        if (string.IsNullOrEmpty(raw) || length <= 0) return "";
        if (length == 6)
        {
            var runs = SixDigitRun().Matches(raw);
            if (runs.Count == 1) return runs[0].Value;
        }
        var digits = new string(raw.Where(char.IsAsciiDigit).ToArray());
        return digits.Length <= length ? digits : digits[..length];
    }

    /// <summary>The zone for <paramref name="value"/>: below <paramref name="lowBelow"/> is Low, above
    /// <paramref name="highAbove"/> is High, otherwise Medium (both thresholds count as Medium).</summary>
    public static FpLevel ZoneLevel(double? value, double lowBelow, double highAbove)
    {
        if (value is not { } v || double.IsNaN(v)) return FpLevel.Unknown;
        if (v < lowBelow) return FpLevel.Low;
        if (v > highAbove) return FpLevel.High;
        return FpLevel.Medium;
    }

    /// <summary>
    /// The slider bubble's number (final fix wave, #29): <paramref name="digits"/> decimals, or up to two more when the
    /// rounded number would fall in another zone than the value itself (10.99 next to a "Low below 11" chip shows
    /// "10.99", not "11.0"), so the bubble and the status chip always agree.
    /// </summary>
    public static string ZoneNumber(double value, int digits, Func<double, FpLevel> levelOf)
    {
        var level = levelOf(value);
        for (var d = digits; d <= digits + 2; d++)
        {
            var rounded = Math.Round(value, d, MidpointRounding.AwayFromZero);
            if (levelOf(rounded) == level || d == digits + 2)
                return value.ToString("N" + d, CultureInfo.InvariantCulture);
        }
        return value.ToString("N" + digits, CultureInfo.InvariantCulture);
    }

    /// <summary>Where <paramref name="value"/> sits between min and max, 0..1 (clamped; 0 when the range is empty).</summary>
    public static double Fraction(double value, double min, double max)
    {
        if (!(max > min) || double.IsNaN(value)) return 0;
        return Math.Clamp((value - min) / (max - min), 0, 1);
    }

    /// <summary>A percentage for inline CSS ("37.5%"), always with a dot decimal separator.</summary>
    public static string Pct(double fraction) =>
        (Math.Round(Math.Clamp(fraction, 0, 1) * 100, 2)).ToString("0.##", CultureInfo.InvariantCulture) + "%";

    /// <summary>Plain-word label for a level.</summary>
    public static string LevelLabel(FpLevel level) => level switch
    {
        FpLevel.Low => "Low",
        FpLevel.Medium => "Medium",
        FpLevel.High => "High",
        _ => "Not set"
    };

    /// <summary>CSS class for a hue: FpHue.Soil gives "fp-hue-soil" (defined in farmer-portal.css).</summary>
    public static string HueCss(FpHue hue) => "fp-hue-" + hue.ToString().ToLowerInvariant();

    /// <summary>CSS modifier for a level (fp-status--low ...).</summary>
    public static string LevelCss(FpLevel level) => level.ToString().ToLowerInvariant();

    /// <summary>Resend countdown text: 75 gives "1:15", 9 gives "0:09".</summary>
    public static string Countdown(int seconds)
    {
        var s = Math.Max(0, seconds);
        return $"{s / 60}:{s % 60:00}";
    }

    /// <summary>
    /// A small picture for a crop chip. Emoji that every phone has had for years; null means the chip
    /// shows a leaf icon instead.
    /// </summary>
    public static string? CropEmoji(string? crop) => (crop ?? "").Trim().ToLowerInvariant() switch
    {
        "paddy" or "rice" => "\U0001F33E",      // sheaf of rice
        "wheat" or "millets" => "\U0001F33E",
        "banana" => "\U0001F34C",
        "coconut" => "\U0001F965",
        "groundnut" or "peanut" => "\U0001F95C",
        "maize" or "corn" => "\U0001F33D",
        "chilli" or "chili" => "\U0001F336️",
        "onion" => "\U0001F9C5",
        "tomato" => "\U0001F345",
        "vegetables" => "\U0001F955",           // carrot
        "mango" => "\U0001F96D",
        "grapes" => "\U0001F347",
        "potato" => "\U0001F954",
        _ => null
    };
}
