using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;

namespace SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

/// <summary>
/// Pure text / colour helpers for the Task 2 farmer screens (dealers, team, call-back requests) and
/// the staff call-back page. No Blazor or JS here, so SPIC.RegressionHarness checks them.
/// </summary>
public static class FarmerConnectUi
{
    /// <summary>Avatar / chip hue per staff role.</summary>
    public static FpHue RoleHue(AppRole role) => role switch
    {
        AppRole.JMDO => FpHue.Green,
        AppRole.MDO => FpHue.Sky,
        AppRole.RM => FpHue.Gold,
        AppRole.RMD => FpHue.Violet,
        _ => FpHue.Neutral
    };

    /// <summary>Short role tag shown next to the name ("JMDO").</summary>
    public static string RoleShort(AppRole role) => role switch
    {
        AppRole.JMDO or AppRole.MDO or AppRole.RM or AppRole.RMD => role.ToString(),
        _ => "SPIC"
    };

    public static FpHue StatusHue(FarmerContactRequestStatus status) => status switch
    {
        FarmerContactRequestStatus.Open => FpHue.Gold,
        FarmerContactRequestStatus.Contacted => FpHue.Green,
        _ => FpHue.Neutral
    };

    public static string StatusIcon(FarmerContactRequestStatus status) => status switch
    {
        FarmerContactRequestStatus.Open => "bi-hourglass-split",
        FarmerContactRequestStatus.Contacted => "bi-telephone-outbound-fill",
        _ => "bi-check-circle-fill"
    };

    /// <summary>What the status means for the farmer, in plain words.</summary>
    public static string StatusHelp(FarmerContactRequestStatus status) => status switch
    {
        FarmerContactRequestStatus.Open => "Waiting for a call back",
        FarmerContactRequestStatus.Contacted => "Your officer has called you",
        _ => "Done"
    };

    /// <summary>"28 Nov, 10:15 am" in IST (the year is added when it is not this year's).</summary>
    public static string IstWhen(DateTime utc, DateTime utcNow)
    {
        var ist = FpUi.ToIst(utc);
        var now = FpUi.ToIst(utcNow);
        var en = CultureInfo.InvariantCulture;
        var time = ist.ToString("h:mm", en) + (ist.Hour < 12 ? " am" : " pm");
        if (ist.Date == now.Date) return "Today, " + time;
        if (ist.Date == now.Date.AddDays(-1)) return "Yesterday, " + time;
        return ist.ToString(ist.Year == now.Year ? "d MMM" : "d MMM yyyy", en) + ", " + time;
    }

    /// <summary>The line above the dealer list saying which search answered.</summary>
    public static string ScopeTitle(FarmerDealerListDto list) => list.Scope switch
    {
        FarmerDealerScope.Nearby => $"{Count(list.Total, "dealer")} within {list.RadiusKm} km",
        FarmerDealerScope.District => $"{Count(list.Total, "dealer")} in {list.DistrictName ?? "your district"}",
        FarmerDealerScope.State => $"{Count(list.Total, "dealer")} in {list.StateName ?? "your state"}",
        _ => "No dealers found"
    };

    /// <summary>
    /// Why a fallback list is shown (null when the list is what was asked for).
    /// </summary>
    public static string? ScopeNote(FarmerDealerListDto list)
    {
        var district = list.DistrictName ?? "your district";
        return list.Scope switch
        {
            FarmerDealerScope.District when list.UsedLocation =>
                $"No SPIC dealer within {list.RadiusKm} km of you, so we show the dealers in {district}.",
            FarmerDealerScope.State when list.UsedLocation =>
                $"No SPIC dealer within {list.RadiusKm} km or in {district}, so we show the dealers in {list.StateName ?? "your state"}.",
            FarmerDealerScope.State =>
                $"No SPIC dealer in {district} yet, so we show the dealers in {list.StateName ?? "your state"}.",
            _ => null
        };
    }

    /// <summary>How the team was found, in plain words (null for the normal district case).</summary>
    public static string? BasisNote(FarmerTeamDto team) => team.Basis switch
    {
        FarmerTeamBasis.NearestDealer =>
            $"There is no SPIC dealer in {team.DistrictName ?? "your district"} yet, so this is the team of the nearest dealer"
            + (team.NearestDealerKm is { } km ? $" ({FarmerDealerRules.DistanceText(km)} away)." : "."),
        FarmerTeamBasis.State =>
            $"We could not find the officers for {team.DistrictName ?? "your district"} yet, so these are the Regional Managers for {team.StateName ?? "your state"}. They will connect you with your local officer.",
        _ => null
    };

    /// <summary>
    /// The home's "Your SPIC team" preview: the first person of each role (in team order, so JMDO,
    /// MDO, RM, RMD), then the next people in order, at most <paramref name="max"/>.
    /// </summary>
    public static List<FarmerContactDto> Preview(IReadOnlyList<FarmerContactDto> members, int max = 3)
    {
        var picked = members.GroupBy(m => m.Role).Select(g => g.First()).Take(max).ToList();
        foreach (var m in members)
        {
            if (picked.Count >= max) break;
            if (!picked.Contains(m)) picked.Add(m);
        }
        return members.Where(picked.Contains).ToList();   // keep the team order
    }

    public static string Count(int n, string noun) =>
        n.ToString("N0", CultureInfo.InvariantCulture) + " " + noun + (n == 1 ? "" : "s");
}
