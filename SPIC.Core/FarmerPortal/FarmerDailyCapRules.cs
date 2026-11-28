using System.Globalization;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Per-farmer caps per IST day (final fix wave, docs/farmer-portal.md "Hardening"): one self-registered account must
/// not grow a table without bound or flood staff. Counted and inserted under a per-farmer PostgreSQL advisory lock
/// (the call-back pattern), so parallel taps and both API replicas cannot pass a cap together. A capped farmer gets
/// 429 with one of the plain messages below (the farmer screens show the server message). Days are IST
/// (<see cref="FarmerAssistantRules.IstDayStartUtc"/>).
/// </summary>
public static class FarmerDailyCapRules
{
    /// <summary>FarmerPortal:FertilizerDailyLimit: fertilizer recommendations per farmer per IST day.</summary>
    public const int FertilizerRunsDefault = 50;

    /// <summary>FarmerPortal:YieldDailyLimit: harvest forecasts per farmer per IST day.</summary>
    public const int YieldForecastsDefault = 50;

    /// <summary>Call-back requests per farmer per IST day, across every officer (the per-officer cap of 3 open stays).</summary>
    public const int CallBacksPerDay = 10;

    /// <summary>FarmerPortal:CommunityDailyPosts: discussions a farmer may start per IST day.</summary>
    public const int CommunityPostsDefault = 5;

    /// <summary>FarmerPortal:CommunityDailyReplies: replies a farmer may post per IST day.</summary>
    public const int CommunityRepliesDefault = 30;

    /// <summary>FarmerPortal:CommunityDailyAttachmentMb: attachment megabytes a farmer may upload per IST day.</summary>
    public const int CommunityAttachmentMbDefault = 20;

    /// <summary>Farmers attach photos and PDFs only (staff keep DOC / DOCX).</summary>
    public static readonly IReadOnlySet<string> FarmerAttachmentExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

    /// <summary>A positive whole number from config, else <paramref name="fallback"/>.</summary>
    public static int Limit(string? configured, int fallback) =>
        int.TryParse(configured?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : fallback;

    public static bool Allows(int usedToday, int limit) => usedToday < limit;

    public static bool AllowsBytes(long usedToday, long adding, long limitBytes) => usedToday + adding <= limitBytes;

    public static long MegaBytes(int mb) => mb * 1024L * 1024L;

    /// <summary>
    /// The start of the current IST day in the server's local clock. Community rows are stamped with DateTime.Now
    /// (server local: UTC in the container, IST on a developer PC), so their cap is counted on that clock.
    /// </summary>
    public static DateTime IstDayStartServerLocal(DateTime utcNow, TimeZoneInfo serverZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(FarmerAssistantRules.IstDayStartUtc(utcNow), serverZone);

    public static string FertilizerMessage(int limit) =>
        $"You have made {limit} fertilizer plans today. You can make more after midnight.";

    public static string YieldMessage(int limit) =>
        $"You have made {limit} harvest forecasts today. You can make more after midnight.";

    public static string CallBackMessage(int limit) =>
        $"You have sent {limit} call-back requests today. Your SPIC team will call you. You can send more after midnight.";

    public static string CommunityPostMessage(int limit) =>
        $"You can start {limit} discussions a day. You can post again after midnight.";

    public static string CommunityReplyMessage(int limit) =>
        $"You can send {limit} replies a day. You can reply again after midnight.";

    public static string CommunityAttachmentMessage(int limitMb) =>
        $"You can add up to {limitMb} MB of photos and files a day. You can add more after midnight.";

    public const string CommunityFarmerFileTypes = "You can attach photos (JPG, PNG, WEBP) and PDF files.";
}
