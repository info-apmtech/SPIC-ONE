namespace SPIC.Core.FarmerPortal;

/// <summary>Where a farmer author lives: their farmer profile, else their linked SAS farmer record.</summary>
public sealed record NearFarmerAuthor(string UserId, int? DistrictId, int? StateId);

/// <summary>Where a SPIC staff author works: their login's state, region(s) and headquarter(s).</summary>
public sealed record NearStaffAuthor(string UserId, int StateId, int RegionId, int HeadquartersId, int[]? RegionIds, int[]? HeadquartersIds);

/// <summary>The community authors near an area: those of the district and those of the state (which includes the district).</summary>
public sealed record NearAuthors(IReadOnlySet<string> District, IReadOnlySet<string> State);

/// <summary>
/// Farmer portal Task 3: "Near me" in the Knowledge Community. Discussions carry no location column;
/// a discussion is near when its AUTHOR is: a farmer whose profile (else linked SAS record) is in the
/// district, or a SPIC staff member whose headquarter serves the district (the headquarter / region of
/// the SPIC dealers registered in the district, the mapping Task 2's "My SPIC team" uses; a login with
/// no headquarter matches by region). The state fallback adds every farmer and staff login of the state.
/// Dealer accounts are not linked to a dealer registration, so their posts are never "near".
/// Pinned by SPIC.RegressionHarness (FarmerPortalTrendsTests).
/// </summary>
public static class CommunityNearRules
{
    public static NearAuthors Authors(
        int districtId,
        int stateId,
        IEnumerable<NearFarmerAuthor> farmers,
        IEnumerable<NearStaffAuthor> staff,
        IReadOnlySet<int> districtHqIds,
        IReadOnlySet<int> districtRegionIds)
    {
        var district = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var state = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var f in farmers)
        {
            if (string.IsNullOrWhiteSpace(f.UserId)) continue;
            if (districtId > 0 && f.DistrictId == districtId) district.Add(f.UserId);
            if (stateId > 0 && f.StateId == stateId) state.Add(f.UserId);
        }

        foreach (var s in staff)
        {
            if (string.IsNullOrWhiteSpace(s.UserId)) continue;
            if (districtId > 0 && ServesDistrict(s, districtHqIds, districtRegionIds)) district.Add(s.UserId);
            if (stateId > 0 && s.StateId == stateId) state.Add(s.UserId);
        }

        state.UnionWith(district);
        return new NearAuthors(district, state);
    }

    /// <summary>A headquarter of the login serves the district; with no headquarter at all, a region does.</summary>
    public static bool ServesDistrict(NearStaffAuthor s, IReadOnlySet<int> districtHqIds, IReadOnlySet<int> districtRegionIds)
    {
        var hqs = Ids(s.HeadquartersId, s.HeadquartersIds);
        if (hqs.Count > 0) return hqs.Any(districtHqIds.Contains);
        return Ids(s.RegionId, s.RegionIds).Any(districtRegionIds.Contains);
    }

    /// <summary>District when its authors wrote anything, else the state, else None.</summary>
    public static SPIC.Core.DTOs.FarmerAreaScope ChooseScope(NearAuthors authors) =>
        authors.District.Count > 0 ? SPIC.Core.DTOs.FarmerAreaScope.District
        : authors.State.Count > 0 ? SPIC.Core.DTOs.FarmerAreaScope.State
        : SPIC.Core.DTOs.FarmerAreaScope.None;

    /// <summary>The authors for a scope (empty for None).</summary>
    public static IReadOnlySet<string> For(NearAuthors authors, SPIC.Core.DTOs.FarmerAreaScope scope) => scope switch
    {
        SPIC.Core.DTOs.FarmerAreaScope.District => authors.District,
        SPIC.Core.DTOs.FarmerAreaScope.State => authors.State,
        _ => new HashSet<string>()
    };

    private static HashSet<int> Ids(int single, int[]? many)
    {
        var set = new HashSet<int>();
        if (single > 0) set.Add(single);
        if (many is not null) foreach (var id in many) if (id > 0) set.Add(id);
        return set;
    }
}
