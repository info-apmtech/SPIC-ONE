using SPIC.Core.Entities;

namespace SPIC.Core.FarmerPortal;

/// <summary>How the farmer's SPIC team was found (shown to the farmer in plain words).</summary>
public enum FarmerTeamBasis
{
    /// <summary>From the SPIC dealers in the farmer's district.</summary>
    District,
    /// <summary>No dealer in the district: from the nearest dealer (farmer GPS, within 100 km).</summary>
    NearestDealer,
    /// <summary>No mapped dealer / staff: the Regional Managers of the farmer's state.</summary>
    State,
    /// <summary>Nobody found.</summary>
    None
}

/// <summary>An active dealer as the team resolver needs it. RegionId / HqId 0 = not set.</summary>
public sealed record FarmerTeamDealer(int Id, int? DistrictId, int StateId, int RegionId, int HqId, double? Latitude, double? Longitude);

/// <summary>An active staff login (Employeelogins row) with its display name.</summary>
public sealed record FarmerTeamStaff(
    string UserId,
    AppRole Role,
    int StateId,
    int RegionId,
    int HqId,
    IReadOnlyList<int>? RegionIds,
    IReadOnlyList<int>? HqIds,
    string Name,
    string? OfficialPhone = null);

/// <summary>One person on the farmer's team, with the headquarter / region that linked them.</summary>
public sealed record FarmerTeamMember(FarmerTeamStaff Staff, int? AreaHqId, int? AreaRegionId);

public sealed record FarmerTeamResult(FarmerTeamBasis Basis, IReadOnlyList<FarmerTeamMember> Members, double? NearestDealerKm);

/// <summary>
/// "My SPIC team" resolution (docs/farmer-portal.md, Task 2; design call "area mapping"):
/// farmer district -> active dealers in that district -> their Region and Headquarter ids ->
/// active JMDO / MDO logins in those headquarters and RM / RMD logins in those regions. When the
/// district has no dealer, the nearest dealer by the farmer's GPS (within 100 km) is used; when that
/// still finds nobody, the RM / RMD logins of the farmer's state. A login matches through its
/// primary id or any of its multi-assignment ids (RegionIds / HeadquartersIds). Pure, pinned by
/// the harness; the controller feeds it database rows.
/// </summary>
public static class FarmerTeamRules
{
    /// <summary>The nearest-dealer fallback looks this far at most.</summary>
    public const double NearestDealerMaxKm = 100;

    /// <summary>At most this many people are shown (and may receive requests).</summary>
    public const int MaxMembers = 8;

    public static readonly IReadOnlySet<AppRole> HqRoles = new HashSet<AppRole> { AppRole.JMDO, AppRole.MDO };
    public static readonly IReadOnlySet<AppRole> RegionRoles = new HashSet<AppRole> { AppRole.RM, AppRole.RMD };

    /// <summary>Every role that can be on a farmer's team (and handles call-back requests).</summary>
    public static readonly IReadOnlySet<AppRole> TeamRoles = new HashSet<AppRole> { AppRole.JMDO, AppRole.MDO, AppRole.RM, AppRole.RMD };

    /// <summary>Plain-words role names for farmers.</summary>
    public static string RoleLabel(AppRole role) => role switch
    {
        AppRole.JMDO => "Junior Market Development Officer",
        AppRole.MDO => "Market Development Officer",
        AppRole.RM => "Regional Manager",
        AppRole.RMD => "Deputy Regional Manager",
        _ => "SPIC officer"
    };

    private static int RoleOrder(AppRole role) => role switch
    {
        AppRole.JMDO => 0,
        AppRole.MDO => 1,
        AppRole.RM => 2,
        AppRole.RMD => 3,
        _ => 9
    };

    /// <summary>A login's headquarter ids (primary + multi-assignment), positive only.</summary>
    public static IEnumerable<int> HqsOf(FarmerTeamStaff s) =>
        new[] { s.HqId }.Concat(s.HqIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct();

    /// <summary>A login's region ids (primary + multi-assignment), positive only.</summary>
    public static IEnumerable<int> RegionsOf(FarmerTeamStaff s) =>
        new[] { s.RegionId }.Concat(s.RegionIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct();

    public static FarmerTeamResult Resolve(
        int districtId,
        int stateId,
        double? farmerLat,
        double? farmerLng,
        IReadOnlyCollection<FarmerTeamDealer> activeDealers,
        IReadOnlyCollection<FarmerTeamStaff> activeStaff)
    {
        var basis = FarmerTeamBasis.District;
        double? nearestKm = null;
        var area = activeDealers.Where(d => districtId > 0 && d.DistrictId == districtId).ToList();

        if (area.Count == 0 && FarmerGeo.IsValid(farmerLat, farmerLng))
        {
            var nearest = activeDealers
                .Where(d => FarmerGeo.IsValid(d.Latitude, d.Longitude))
                .Select(d => (Dealer: d, Km: FarmerGeo.HaversineKm(farmerLat!.Value, farmerLng!.Value, d.Latitude!.Value, d.Longitude!.Value)))
                .Where(x => x.Km <= NearestDealerMaxKm)
                .OrderBy(x => x.Km)
                .ThenBy(x => x.Dealer.Id)
                .FirstOrDefault();
            if (nearest.Dealer is not null)
            {
                area.Add(nearest.Dealer);
                basis = FarmerTeamBasis.NearestDealer;
                nearestKm = Math.Round(nearest.Km, 1);
            }
        }

        // Headquarters / regions weighted by how many of the area's dealers they serve: the one with
        // the most dealers comes first (its staff are listed first within a role).
        var hqWeight = area.Where(d => d.HqId > 0).GroupBy(d => d.HqId).ToDictionary(g => g.Key, g => g.Count());
        var regionWeight = area.Where(d => d.RegionId > 0).GroupBy(d => d.RegionId).ToDictionary(g => g.Key, g => g.Count());

        var candidates = new List<(FarmerTeamMember Member, int Weight)>();
        foreach (var s in activeStaff)
        {
            if (HqRoles.Contains(s.Role))
            {
                var best = HqsOf(s).Where(hqWeight.ContainsKey).OrderByDescending(h => hqWeight[h]).ThenBy(h => h).ToList();
                if (best.Count > 0) candidates.Add((new FarmerTeamMember(s, best[0], null), hqWeight[best[0]]));
            }
            else if (RegionRoles.Contains(s.Role))
            {
                var best = RegionsOf(s).Where(regionWeight.ContainsKey).OrderByDescending(r => regionWeight[r]).ThenBy(r => r).ToList();
                if (best.Count > 0) candidates.Add((new FarmerTeamMember(s, null, best[0]), regionWeight[best[0]]));
            }
        }

        if (candidates.Count == 0)
        {
            basis = FarmerTeamBasis.State;
            nearestKm = null;
            foreach (var s in activeStaff.Where(s => RegionRoles.Contains(s.Role) && stateId > 0 && s.StateId == stateId))
                candidates.Add((new FarmerTeamMember(s, null, s.RegionId > 0 ? s.RegionId : null), 0));
        }

        var members = candidates
            .OrderBy(c => RoleOrder(c.Member.Staff.Role))
            .ThenByDescending(c => c.Weight)
            .ThenBy(c => c.Member.Staff.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Member.Staff.UserId, StringComparer.Ordinal)
            .Select(c => c.Member)
            // One card per person even when they hold two logins (e.g. JMDO and MDO).
            .GroupBy(m => m.Staff.UserId, StringComparer.Ordinal)
            .Select(g => g.First())
            .Take(MaxMembers)
            .ToList();

        return new FarmerTeamResult(members.Count == 0 ? FarmerTeamBasis.None : basis, members, nearestKm);
    }
}
