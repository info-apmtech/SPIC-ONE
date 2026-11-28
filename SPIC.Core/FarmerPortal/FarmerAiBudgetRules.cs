using System.Globalization;

namespace SPIC.Core.FarmerPortal;

/// <summary>What a farmer AI call is for. Every kind shares one daily budget; each kind may also have its own sub-limit.</summary>
public enum FarmerAiKind
{
    /// <summary>Ask SPIC AI (Task 5).</summary>
    Chat = 0,
    /// <summary>The leaf-photo check (Task 6).</summary>
    LeafCheck = 1
}

/// <summary>The budget's answer for one AI call.</summary>
public enum FarmerAiDecision
{
    /// <summary>Call the AI (one unit of today's budget is used).</summary>
    Allowed = 0,
    /// <summary>No Anthropic key: the basic answer, nothing counted.</summary>
    NoKey = 1,
    /// <summary>FarmerPortal:AssistantEnabled = false (kill switch): the basic answer.</summary>
    Disabled = 2,
    /// <summary>Today's AI answers across all farmers are used up: the basic answer.</summary>
    GlobalLimit = 3,
    /// <summary>Today's AI answers of this kind are used up: the basic answer.</summary>
    KindLimit = 4
}

/// <summary>
/// The daily AI budget across ALL farmers (farmer portal Task 5 fix round 1): a spend ceiling on top of the per-farmer
/// cap, with a kill switch. Counted on the IST day (<see cref="FarmerAssistantRules.IstDayStartUtc"/>). Pure; the
/// race-safe counting is SpicAPI/Services/FarmerAiBudget.cs. Pinned by the harness.
/// </summary>
public static class FarmerAiBudgetRules
{
    /// <summary>Default AI answers per IST day across all farmers and kinds (FarmerPortal:AssistantGlobalDailyLimit).</summary>
    public const int DefaultGlobalDailyLimit = 2000;

    /// <summary>FarmerPortal:AssistantEnabled: empty / unreadable = on; "false" / "0" / "off" / "no" = off.</summary>
    public static bool Enabled(string? configured)
    {
        var v = (configured ?? "").Trim().ToLowerInvariant();
        return v is not ("false" or "0" or "off" or "no");
    }

    /// <summary>FarmerPortal:AssistantGlobalDailyLimit; empty / unreadable / negative = 2000. 0 is allowed: no AI answers.</summary>
    public static int GlobalLimit(string? configured) =>
        int.TryParse(configured?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= 0 ? v : DefaultGlobalDailyLimit;

    /// <summary>A kind's sub-limit (FarmerPortal:AiKindDailyLimit:{Kind}); empty / unreadable / negative = none.</summary>
    public static int? KindLimit(string? configured) =>
        int.TryParse(configured?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= 0 ? v : null;

    /// <summary>The decision for one call, given what is already used today (all kinds, and this kind).</summary>
    public static FarmerAiDecision Decide(bool enabled, bool hasKey, int usedToday, int globalLimit, int usedKindToday, int? kindLimit)
    {
        if (!hasKey) return FarmerAiDecision.NoKey;
        if (!enabled) return FarmerAiDecision.Disabled;
        if (usedToday >= globalLimit) return FarmerAiDecision.GlobalLimit;
        if (kindLimit is { } k && usedKindToday >= k) return FarmerAiDecision.KindLimit;
        return FarmerAiDecision.Allowed;
    }

    /// <summary>How many of <paramref name="usedAtUtc"/> fall on the IST day of <paramref name="utcNow"/> (what the
    /// service counts in SQL).</summary>
    public static int UsedToday(IEnumerable<DateTime> usedAtUtc, DateTime utcNow) =>
        usedAtUtc.Count(t => FarmerAssistantRules.CountsToday(t, utcNow));

    /// <summary>The gentle line shown with a basic answer the budget chose (null when the AI was allowed or there is no key).</summary>
    public static string? Notice(FarmerAiDecision decision) => decision switch
    {
        FarmerAiDecision.Disabled => "Full SPIC AI answers are paused just now, so this is a basic answer from the SPIC library.",
        FarmerAiDecision.GlobalLimit or FarmerAiDecision.KindLimit =>
            "Many farmers are asking SPIC AI today, so this is a basic answer from the SPIC library. Full answers are back after 12 midnight.",
        _ => null
    };
}
