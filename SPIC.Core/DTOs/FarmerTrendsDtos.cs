using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

// Farmer Portal Task 3 contracts (docs/farmer-portal.md): "Trends in my area", the home weather strip
// and "Community near me".
//   GET api/Farmer/trends                       -> FarmerTrendsDto   (Farmer; area numbers cached 1 h per district)
//   GET api/Farmer/weather                      -> FarmerWeatherDto  (Farmer; Open-Meteo, cached 1 h per 0.1 deg point)
//   GET api/Community/near?page&pageSize        -> CommunityNearPageDto (any signed-in user)
//   GET api/Community/discussions?tab=near&...  -> PageResult<DiscussionSummaryDto> (same "near" rule)
// Enums travel as numbers like the rest of the API.

/// <summary>Which area a card describes: the farmer's district, the state (fallback) or nothing.</summary>
public enum FarmerAreaScope
{
    District = 0,
    State = 1,
    None = 2
}

/// <summary>Ok = forecast shown; NoLocation = no GPS on the profile; Unavailable = Open-Meteo failed.</summary>
public enum FarmerWeatherStatus
{
    Ok = 0,
    NoLocation = 1,
    Unavailable = 2
}

/// <summary>Info (blue), Caution (amber) or Good (green).</summary>
public enum FarmerAdvisoryLevel
{
    Info = 0,
    Caution = 1,
    Good = 2
}

/// <summary>One forecast day (IST date).</summary>
public class FarmerWeatherDayDto
{
    /// <summary>"yyyy-MM-dd" in IST.</summary>
    public string Date { get; set; } = string.Empty;
    public int? WeatherCode { get; set; }
    public double? MaxTempC { get; set; }
    public double? MinTempC { get; set; }
    public double? RainMm { get; set; }
    public int? RainChancePct { get; set; }
    public double? WindKmh { get; set; }
}

/// <summary>A simple farm advisory from the forecast (<see cref="SPIC.Core.FarmerPortal.FarmerWeatherRules.Advisories"/>).</summary>
public class FarmerAdvisoryDto
{
    public string Code { get; set; } = string.Empty;
    public FarmerAdvisoryLevel Level { get; set; }
    public string Icon { get; set; } = "bi-info-circle";
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class FarmerWeatherDto
{
    public FarmerWeatherStatus Status { get; set; }
    public List<FarmerWeatherDayDto> Days { get; set; } = new();
    public List<FarmerAdvisoryDto> Advisories { get; set; } = new();
    /// <summary>When Open-Meteo answered (UTC); null unless Ok.</summary>
    public DateTime? FetchedAtUtc { get; set; }
    public string Source { get; set; } = "Open-Meteo";
}

public class FarmerCropShareDto
{
    public string Crop { get; set; } = string.Empty;
    public int Farmers { get; set; }
    public int SharePct { get; set; }
}

public class FarmerCropTrendDto
{
    public FarmerAreaScope Scope { get; set; } = FarmerAreaScope.None;
    public string? AreaName { get; set; }
    /// <summary>Farmers counted in the area (0 when hidden).</summary>
    public int FarmerCount { get; set; }
    public int MinFarmers { get; set; }
    public List<FarmerCropShareDto> Items { get; set; } = new();
}

public class FarmerSoilProblemDto
{
    /// <summary>Lab parameter name, e.g. "Nitrogen".</summary>
    public string Parameter { get; set; } = string.Empty;
    /// <summary>"N", "P", "K", "Zn" ...</summary>
    public string Short { get; set; } = string.Empty;
    /// <summary>Plain label, e.g. "Nitrogen (N)".</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Samples tested for this nutrient in the area (at least MinSamples).</summary>
    public int Samples { get; set; }
    public int LowSamples { get; set; }
    public int LowPct { get; set; }
}

public class FarmerSoilTrendDto
{
    public FarmerAreaScope Scope { get; set; } = FarmerAreaScope.None;
    public string? AreaName { get; set; }
    /// <summary>Soil samples with a lab report in the area and window (0 when hidden).</summary>
    public int SampleCount { get; set; }
    public int MinSamples { get; set; }
    /// <summary>Nutrients left out because fewer than MinSamples samples were tested for them.</summary>
    public int HiddenForFewSamples { get; set; }
    public List<FarmerSoilProblemDto> Items { get; set; } = new();
}

public class FarmerTalkedDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Crop { get; set; }
    public string? Category { get; set; }
    public CommunityDiscussionStatus Status { get; set; }
    public int ReplyCount { get; set; }
    /// <summary>Replies within the window.</summary>
    public int RecentReplies { get; set; }
    public int Views { get; set; }
    public DateTime LastActivityAt { get; set; }
}

public class FarmerTalkedTrendDto
{
    public FarmerAreaScope Scope { get; set; } = FarmerAreaScope.None;
    public string? AreaName { get; set; }
    public List<FarmerTalkedDto> Items { get; set; } = new();
}

public class FarmerDealerCountDto
{
    public FarmerAreaScope Scope { get; set; } = FarmerAreaScope.None;
    public string? AreaName { get; set; }
    public int Count { get; set; }
}

public class FarmerTrendsDto
{
    public string? DistrictName { get; set; }
    public string? StateName { get; set; }
    public int WindowDays { get; set; }
    /// <summary>When the area numbers were computed (UTC; they are cached for an hour).</summary>
    public DateTime AreaComputedAtUtc { get; set; }
    public FarmerWeatherDto Weather { get; set; } = new();
    public FarmerCropTrendDto Crops { get; set; } = new();
    public FarmerSoilTrendDto Soil { get; set; } = new();
    public FarmerTalkedTrendDto Talked { get; set; } = new();
    public FarmerDealerCountDto Dealers { get; set; } = new();
}

/// <summary>"Community near me": the discussions whose authors are near the caller (district, else state).</summary>
public class CommunityNearPageDto
{
    public FarmerAreaScope Scope { get; set; } = FarmerAreaScope.None;
    public string? AreaName { get; set; }
    public List<DiscussionSummaryDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool HasMore => Page * PageSize < Total;
}
