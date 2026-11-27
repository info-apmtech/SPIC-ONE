using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// One tile on the farmer home (/farmer). <see cref="Ready"/> false shows the tile with a "Soon"
/// badge and no link, so the home always shows the whole portal. <see cref="Hue"/> is one of the
/// FpTile hues: green, gold, soil, sky, violet, teal, rose.
/// </summary>
public sealed record FarmerFeatureTile(
    string Id,
    string Title,
    string Description,
    string Icon,
    string Hue,
    string Route,
    bool Ready,
    Func<LoginState, bool> Visible,
    string? Badge = null);

/// <summary>
/// The farmer home tile registry (docs/farmer-portal.md, "Design system"). Each feature task flips
/// its own entry to <c>Ready: true</c> (and adjusts the text) when its page ships; a new feature
/// appends an entry. Order here is the order on the home grid. Pure data: the harness checks it.
/// </summary>
public static class FarmerFeatureTiles
{
    private static bool Always(LoginState _) => true;

    /// <summary>A farmer linked to a SAS soil-sample record, or granted Sample Collection.</summary>
    public static bool HasSoilTests(LoginState login) =>
        login.FarmerSasLinked || login.CanSeeMenu(nameof(PagePermission.SampleCollection));

    public static readonly IReadOnlyList<FarmerFeatureTile> All = new List<FarmerFeatureTile>
    {
        // Task 4
        new("fertilizer", "Fertilizer advice", "What to apply, and how much, for your crop",
            "bi-droplet-half", "green", "/farmer/fertilizer", Ready: false, Always),
        // Task 2
        new("dealers", "Dealers near you", "SPIC dealers with stock, call or get directions",
            "bi-shop", "soil", "/farmer/dealers", Ready: false, Always),
        // Task 5
        new("assistant", "Ask SPIC AI", "Crop and fertilizer questions in your language",
            "bi-chat-dots", "violet", "/farmer/assistant", Ready: false, Always),
        // Task 6
        new("leaf-check", "Leaf check", "Photo of a leaf to spot a nutrient problem",
            "bi-camera", "teal", "/farmer/leaf-check", Ready: false, Always),
        // Task 7
        new("yield", "Harvest forecast", "Expected harvest for your field this season",
            "bi-graph-up-arrow", "gold", "/farmer/yield", Ready: false, Always),
        // Task 2
        new("team", "My SPIC team", "Your SPIC field officer and how to reach them",
            "bi-person-badge", "sky", "/farmer/team", Ready: false, Always),
        // Task 3
        new("community", "Farmers near you", "What farmers in your area grow and ask",
            "bi-people", "rose", "/farmer/community", Ready: false, Always),
        // Task 1 (SAS v1): shown only to farmers with soil tests.
        new("soil-samples", "My soil samples", "Samples your SPIC officer collected",
            "bi-moisture", "soil", "/Sas/MySamples", Ready: true, HasSoilTests),
        new("soil-reports", "My soil reports", "Download your soil test reports",
            "bi-file-earmark-text", "green", "/Sas/MyReports", Ready: true, HasSoilTests),
        // Task 1
        new("profile", "My profile", "Your farm, village, crops and language",
            "bi-person-circle", "sky", "/farmer/profile", Ready: true, Always)
    };

    /// <summary>The tiles this user sees, in registry order.</summary>
    public static IReadOnlyList<FarmerFeatureTile> For(LoginState login) =>
        All.Where(t => t.Visible(login)).ToList();
}
