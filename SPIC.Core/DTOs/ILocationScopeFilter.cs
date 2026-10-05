using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
    /// <summary>
    /// Common location-scope surface implemented by every report/filter DTO that
    /// is filtered by State/Region/HeadQuarter. Used by the server-side
    /// SpecialAdmin scope enforcement to restrict a request to the assigned
    /// locations (from the JWT) for the SpecialAdmin role only.
    /// </summary>
    public interface ILocationScopeFilter
    {
        List<int> StateIds { get; set; }
        List<int> RegionIds { get; set; }
        List<int> HeadQuarterIds { get; set; }
    }
}
