using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// One tile on the farmer home (/farmer): a feature that EXISTS. <see cref="Visible"/> decides
/// whether this signed-in user sees it (e.g. only farmers with soil tests).
/// </summary>
public sealed record FarmerFeatureTile(
    string Id,
    string Title,
    string Description,
    string Icon,
    FpHue Hue,
    string Route,
    Func<LoginState, bool> Visible,
    string? Badge = null);

/// <summary>
/// The farmer home tile registry (docs/farmer-portal.md, "Design system"). The home shows only
/// features that exist: a task adds its entry here in the same change that ships its page (never
/// before), in the order it should appear. Pure data: the harness checks every route is a real page
/// a farmer can open.
/// </summary>
public static class FarmerFeatureTiles
{
    private static bool Always(LoginState _) => true;

    /// <summary>A farmer linked to a SAS soil-sample record, or granted Sample Collection.</summary>
    public static bool HasSoilTests(LoginState login) =>
        login.FarmerSasLinked || login.CanSeeMenu(nameof(PagePermission.SampleCollection));

    public static readonly IReadOnlyList<FarmerFeatureTile> All = new List<FarmerFeatureTile>
    {
        // Task 4: the main tool, first on the home.
        new("fertilizer", "Fertilizer advice", "What to apply for your crop and soil",
            "bi-flower2", FpHue.Green, "/farmer/fertilizer", Always),
        // Task 5
        new("assistant", "Ask SPIC AI", "Farming questions in your language",
            "bi-stars", FpHue.Rose, "/farmer/assistant", Always),
        // Task 2
        new("dealers", "Dealers near me", "Call or visit a SPIC dealer",
            "bi-shop", FpHue.Gold, "/farmer/dealers", Always),
        new("team", "My SPIC team", "Your local officers call you back",
            "bi-people-fill", FpHue.Teal, "/farmer/team", Always),
        // Task 3
        new("trends", "Trends in my area", "Weather, crops and soil near you",
            "bi-cloud-sun", FpHue.Sky, "/farmer/trends", Always),
        new("community", "Community near me", "Ask farmers and SPIC experts",
            "bi-chat-square-text", FpHue.Violet, "/farmer/community", Always),
        // SAS v1: shown only to farmers with soil tests.
        new("soil-reports", "My soil reports", "Download your soil test reports",
            "bi-file-earmark-text", FpHue.Green, "/Sas/MyReports", HasSoilTests),
        new("soil-samples", "My soil samples", "Samples your SPIC officer collected",
            "bi-moisture", FpHue.Soil, "/Sas/MySamples", HasSoilTests),
        // Task 1
        new("profile", "My profile", "Your farm, village, crops and language",
            "bi-person-circle", FpHue.Sky, "/farmer/profile", Always)
    };

    /// <summary>The tiles this user sees, in registry order.</summary>
    public static IReadOnlyList<FarmerFeatureTile> For(LoginState login) =>
        All.Where(t => t.Visible(login)).ToList();
}
