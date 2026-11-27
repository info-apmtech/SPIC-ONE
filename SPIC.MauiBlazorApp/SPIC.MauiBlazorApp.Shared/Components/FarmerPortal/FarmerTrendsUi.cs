using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;

namespace SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

/// <summary>
/// Plain-words text for farmer portal Task 3 (weather strip, "Trends in my area", "Community near me").
/// Pure, checked by SPIC.RegressionHarness (FarmerPortalTrendsTests).
/// </summary>
public static class FarmerTrendsUi
{
    /// <summary>Where "Add your location" sends the farmer: the profile, scrolled to "Use my location".</summary>
    public const string AddLocationHref = "/farmer/profile?focus=location";

    /// <summary>"Today", "Tomorrow", then the short weekday ("Mon"), from the forecast's IST date.</summary>
    public static string DayLabel(string isoDate, DateTime utcNow)
    {
        if (!DateTime.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return isoDate;
        var today = FpUi.ToIst(utcNow).Date;
        if (day.Date == today) return "Today";
        if (day.Date == today.AddDays(1)) return "Tomorrow";
        return day.ToString("ddd", CultureInfo.InvariantCulture);
    }

    /// <summary>Short label for the strip: "Today", "Tmrw" (fits a 72 px pill), "Mon".</summary>
    public static string ShortDayLabel(string isoDate, DateTime utcNow)
    {
        var label = DayLabel(isoDate, utcNow);
        return label == "Tomorrow" ? "Tmrw" : label;
    }

    /// <summary>"31°" (rounded), or a dash.</summary>
    public static string Temp(double? c) => c is { } t ? $"{Math.Round(t):0}°" : "–";

    /// <summary>"13 mm", "0.4 mm", "No rain" for 0 / missing.</summary>
    public static string Rain(double? mm) => mm switch
    {
        null or <= 0.05 => "No rain",
        < 1 => $"{mm.Value.ToString("0.0", CultureInfo.InvariantCulture)} mm",
        _ => $"{Math.Round(mm.Value):0} mm"
    };

    /// <summary>The strongest advisory to show on the home strip (a caution first), or null.</summary>
    public static FarmerAdvisoryDto? TopAdvice(FarmerWeatherDto? weather) =>
        weather?.Status != FarmerWeatherStatus.Ok ? null
        : weather.Advisories.FirstOrDefault(a => a.Level == FarmerAdvisoryLevel.Caution) ?? weather.Advisories.FirstOrDefault();

    /// <summary>CSS hue for an advisory level.</summary>
    public static string AdviceHue(FarmerAdvisoryLevel level) => level switch
    {
        FarmerAdvisoryLevel.Caution => "fp-hue-gold",
        FarmerAdvisoryLevel.Good => "fp-hue-green",
        _ => "fp-hue-sky"
    };

    /// <summary>
    /// A note when a card fell back from the district to the state (null when it shows the district).
    /// </summary>
    public static string? ScopeNote(FarmerAreaScope scope, string? districtName, string? stateName, string what) =>
        scope == FarmerAreaScope.State
            ? $"Not enough {what} from {Or(districtName, "your district")} yet, so this shows all of {Or(stateName, "your state")}."
            : null;

    /// <summary>"in Thoothukudi" / "in Tamil Nadu" / "near you".</summary>
    public static string InArea(string? areaName) => string.IsNullOrWhiteSpace(areaName) ? "near you" : "in " + areaName;

    /// <summary>"1 farmer", "12 farmers".</summary>
    public static string Count(int n, string one, string? many = null) =>
        n == 1 ? $"1 {one}" : $"{n.ToString("N0", CultureInfo.InvariantCulture)} {many ?? one + "s"}";

    /// <summary>Soil card row caption: "6 of 20 samples low".</summary>
    public static string SoilCaption(FarmerSoilProblemDto p) => $"{p.LowSamples} of {p.Samples} samples low";

    /// <summary>The community author's role as a short chip label.</summary>
    public static string RoleLabel(string? role) => role switch
    {
        "SPIC Expert" => "SPIC expert",
        "Dealer" => "Dealer",
        _ => "Farmer"
    };

    /// <summary>Hue for the author's avatar: farmers green, SPIC experts gold, dealers sky.</summary>
    public static FpHue RoleHue(string? role) => role switch
    {
        "SPIC Expert" => FpHue.Gold,
        "Dealer" => FpHue.Sky,
        _ => FpHue.Green
    };

    /// <summary>A discussion status in plain words (null for an ordinary open discussion).</summary>
    public static (string Text, FpHue Hue, string Icon)? StatusChip(CommunityDiscussionStatus status) => status switch
    {
        CommunityDiscussionStatus.ExpertAnswer => ("Expert answered", FpHue.Gold, "bi-patch-check-fill"),
        CommunityDiscussionStatus.Resolved => ("Solved", FpHue.Green, "bi-check-circle-fill"),
        CommunityDiscussionStatus.WaitingForReply => ("Waiting for an answer", FpHue.Neutral, "bi-hourglass-split"),
        _ => null
    };

    /// <summary>"12 replies · 40 views".</summary>
    public static string TalkLine(int replies, int views) =>
        $"{Count(replies, "reply", "replies")} · {Count(views, "view")}";

    /// <summary>"Active 28 Nov" from the discussion's last activity (stored as the API's local time).</summary>
    public static string Active(DateTime lastActivity) => "Active " + lastActivity.ToString("d MMM", CultureInfo.InvariantCulture);

    /// <summary>Bar width in percent for a share: at least 4 % so a small share still shows.</summary>
    public static int BarWidth(int pct) => pct <= 0 ? 0 : Math.Clamp(pct, 4, 100);

    /// <summary>The district / state line for the hero chip.</summary>
    public static string Place(string? districtName, string? stateName) => FarmerDealerRules.JoinParts(districtName, stateName);

    private static string Or(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
