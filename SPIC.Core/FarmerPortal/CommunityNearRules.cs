using SPIC.Core.DTOs;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Farmer portal Task 3: "Near me" in the Knowledge Community. Discussions carry no location column;
/// a discussion is near when its AUTHOR is: a farmer whose profile (else linked SAS record) is in the
/// district, or a SPIC staff member whose headquarter serves the district (the headquarter / region of
/// the SPIC dealers registered in the district, the mapping Task 2's "My SPIC team" uses; a login with
/// no headquarter matches by region). The state level adds every farmer and staff login of the state.
/// Dealer accounts are not linked to a dealer registration, so their posts are never "near".
/// The author test itself is a SQL sub-query (SpicAPI/Services/CommunityNearResolver.NearPosts) so
/// counting and paging run in the database; this class holds the scope choice. Pinned by
/// SPIC.RegressionHarness (FarmerPortalTrendsTests).
/// </summary>
public static class CommunityNearRules
{
    /// <summary>The district is shown on its own only when it has at least this many discussions; fewer
    /// and the (richer) state list is shown instead.</summary>
    public const int MinDistrictPosts = 3;

    /// <summary>
    /// District when it has at least <see cref="MinDistrictPosts"/> discussions; else the state when it
    /// has any (it includes the district's); else the district if it has any; else None.
    /// </summary>
    public static FarmerAreaScope ChooseScope(int districtPosts, int statePosts) =>
        districtPosts >= MinDistrictPosts ? FarmerAreaScope.District
        : statePosts > 0 ? FarmerAreaScope.State
        : districtPosts > 0 ? FarmerAreaScope.District
        : FarmerAreaScope.None;
}
