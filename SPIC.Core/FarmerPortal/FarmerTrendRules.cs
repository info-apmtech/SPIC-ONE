using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.Core.FarmerPortal;

/// <summary>One farmer's crops for "What farmers near you grow": a farmer profile or a SAS farmer.</summary>
/// <param name="FarmerKey">"u:{userId}" for an account, "s:{sasFarmerId}" for a SAS-only farmer, so one
/// person is counted once.</param>
public sealed record TrendCropFarmer(string FarmerKey, int? DistrictId, IReadOnlyList<string> Crops);

/// <summary>
/// Recheck fix B8: farmer profiles counted in SQL, one row per (district, stored crop list) with the number of
/// profiles that have exactly that list, so a cache miss no longer reads every profile of the state.
/// </summary>
public sealed record TrendCropGroup(int? DistrictId, string? PrimaryCrops, int Farmers);

/// <summary>One lab result for "Soil problems near you" (the lab's own status, one row per sample + parameter).</summary>
public sealed record TrendSoilResult(int SampleId, int? DistrictId, string Parameter, LabResultStatus Status);

/// <summary>One community discussion for "Talked about near you".</summary>
public sealed record TrendPost(int Id, string Title, string? Crop, string? Category, CommunityDiscussionStatus Status,
    int ReplyCount, int RecentReplies, int Views, int LikeCount, DateTime LastActivityAt);

/// <summary>
/// Farmer portal Task 3: the area numbers on "Trends in my area". Only totals for an area ever leave
/// these rules, never a single farmer's row, and an area with fewer than <see cref="MinSamples"/> soil
/// samples (or <see cref="MinFarmers"/> farmers) shows nothing. Each card uses the farmer's district,
/// else the state, else it is hidden (<see cref="FarmerAreaScope"/>). Pinned by
/// SPIC.RegressionHarness (FarmerPortalTrendsTests).
/// </summary>
public static class FarmerTrendRules
{
    /// <summary>Days of SPIC ONE data the area numbers look back.</summary>
    public const int WindowDays = 90;

    /// <summary>A soil statistic needs at least this many tested samples, else it is hidden.</summary>
    public const int MinSamples = 5;

    /// <summary>The crop card needs at least this many farmers in the area.</summary>
    public const int MinFarmers = 5;

    /// <summary>A crop is listed only when at least this many farmers grow it.</summary>
    public const int MinFarmersPerCrop = 2;

    public const int MaxCrops = 6;
    public const int MaxSoilProblems = 6;
    public const int MaxTalked = 5;

    /// <summary>The soil nutrients the card reports (lab parameter name -> short label), in display order.</summary>
    public static readonly IReadOnlyList<(string Parameter, string Short, string Plain)> Nutrients = new[]
    {
        ("Nitrogen", "N", "Nitrogen (N)"),
        ("Phosphorus", "P", "Phosphorus (P)"),
        ("Potassium", "K", "Potassium (K)"),
        ("Zinc", "Zn", "Zinc (Zn)"),
        ("Organic Carbon", "OC", "Organic carbon"),
        ("Sulphur", "S", "Sulphur (S)"),
        ("Iron", "Fe", "Iron (Fe)"),
        ("Boron", "B", "Boron (B)"),
        ("Manganese", "Mn", "Manganese (Mn)"),
        ("Copper", "Cu", "Copper (Cu)")
    };

    /// <summary>The lab parameter names the soil card reads.</summary>
    public static IReadOnlyList<string> NutrientParameters => Nutrients.Select(n => n.Parameter).ToList();

    /// <summary>District when it has enough, else the state when it has enough, else None.</summary>
    public static FarmerAreaScope ChooseScope(int districtCount, int stateCount, int minimum) =>
        districtCount >= minimum ? FarmerAreaScope.District
        : stateCount >= minimum ? FarmerAreaScope.State
        : FarmerAreaScope.None;

    /// <summary>"paddy " -> "Paddy". Empty for blanks.</summary>
    public static string NormaliseCrop(string? crop)
    {
        var c = (crop ?? "").Trim();
        if (c.Length == 0) return "";
        return char.ToUpperInvariant(c[0]) + c[1..].ToLowerInvariant();
    }

    /// <summary>
    /// "What farmers near you grow". <paramref name="farmers"/> are the farmers of the state (the
    /// caller already filtered to the state); the district is used when it has <see cref="MinFarmers"/>.
    /// A farmer counts once per crop. Crops grown by fewer than <see cref="MinFarmersPerCrop"/> farmers
    /// are left out.
    /// </summary>
    public static FarmerCropTrendDto Crops(IEnumerable<TrendCropFarmer> farmers, int districtId, string? districtName, string? stateName) =>
        Crops(farmers, Array.Empty<TrendCropGroup>(), districtId, districtName, stateName);

    /// <summary>
    /// <see cref="Crops(IEnumerable{TrendCropFarmer}, int, string?, string?)"/> with part of the farmers already counted
    /// in SQL (<paramref name="groups"/>, recheck fix B8): each group counts as that many farmers with that district and
    /// those crops. The caller keeps the farmers that also have a SAS record in <paramref name="farmers"/>, so a person is
    /// still counted once.
    /// </summary>
    public static FarmerCropTrendDto Crops(IEnumerable<TrendCropFarmer> farmers, IEnumerable<TrendCropGroup> groups,
        int districtId, string? districtName, string? stateName)
    {
        // One entry per farmer (the same person may have a profile and a SAS record keyed alike).
        var merged = farmers
            .GroupBy(f => f.FarmerKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                DistrictId = g.Select(x => x.DistrictId).FirstOrDefault(d => d is > 0),
                Crops = g.SelectMany(x => x.Crops).Select(NormaliseCrop).Where(c => c.Length > 0)
                         .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Weight = 1
            })
            .Concat(groups.Where(g => g.Farmers > 0).Select(g => new
            {
                DistrictId = g.DistrictId is > 0 ? g.DistrictId : null,
                Crops = (g.PrimaryCrops ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                         .Select(NormaliseCrop).Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Weight = g.Farmers
            }))
            .Where(f => f.Crops.Count > 0)
            .ToList();

        var inDistrict = merged.Where(f => districtId > 0 && f.DistrictId == districtId).ToList();
        var scope = ChooseScope(inDistrict.Sum(f => f.Weight), merged.Sum(f => f.Weight), MinFarmers);
        var dto = new FarmerCropTrendDto
        {
            Scope = scope,
            AreaName = AreaName(scope, districtName, stateName),
            MinFarmers = MinFarmers
        };
        if (scope == FarmerAreaScope.None) return dto;

        var pool = scope == FarmerAreaScope.District ? inDistrict : merged;
        var poolCount = pool.Sum(f => f.Weight);
        dto.FarmerCount = poolCount;
        dto.Items = pool
            .SelectMany(f => f.Crops.Select(c => (Crop: c, f.Weight)))
            .GroupBy(x => x.Crop, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Crop = g.First().Crop, Farmers = g.Sum(x => x.Weight) })
            .Where(x => x.Farmers >= MinFarmersPerCrop)
            .OrderByDescending(x => x.Farmers).ThenBy(x => x.Crop, StringComparer.OrdinalIgnoreCase)
            .Take(MaxCrops)
            .Select(x => new FarmerCropShareDto { Crop = x.Crop, Farmers = x.Farmers, SharePct = Pct(x.Farmers, poolCount) })
            .ToList();
        return dto;
    }

    /// <summary>
    /// "Soil problems near you": per nutrient, the share of tested samples the lab marked Deficient
    /// (Low). The area needs <see cref="MinSamples"/> samples; a nutrient tested on fewer than
    /// <see cref="MinSamples"/> samples is hidden; nutrients with no Low sample are not listed.
    /// <paramref name="results"/> are the state's results (the caller filtered to the state).
    /// </summary>
    public static FarmerSoilTrendDto SoilProblems(IEnumerable<TrendSoilResult> results, int districtId, string? districtName, string? stateName)
    {
        var known = Nutrients.ToDictionary(n => n.Parameter, StringComparer.OrdinalIgnoreCase);
        // One result per sample and nutrient (a re-entered value must not count twice).
        var rows = results
            .Where(r => known.ContainsKey(r.Parameter?.Trim() ?? ""))
            .GroupBy(r => (r.SampleId, Parameter: r.Parameter.Trim().ToLowerInvariant()))
            .Select(g => g.Last())
            .ToList();

        var districtRows = rows.Where(r => districtId > 0 && r.DistrictId == districtId).ToList();
        var districtSamples = districtRows.Select(r => r.SampleId).Distinct().Count();
        var stateSamples = rows.Select(r => r.SampleId).Distinct().Count();
        var scope = ChooseScope(districtSamples, stateSamples, MinSamples);

        var dto = new FarmerSoilTrendDto
        {
            Scope = scope,
            AreaName = AreaName(scope, districtName, stateName),
            MinSamples = MinSamples
        };
        if (scope == FarmerAreaScope.None) return dto;

        var pool = scope == FarmerAreaScope.District ? districtRows : rows;
        dto.SampleCount = scope == FarmerAreaScope.District ? districtSamples : stateSamples;

        var items = new List<FarmerSoilProblemDto>();
        var hidden = 0;
        foreach (var (parameter, shortLabel, plain) in Nutrients)
        {
            var tested = pool.Where(r => r.Parameter.Trim().Equals(parameter, StringComparison.OrdinalIgnoreCase)).ToList();
            if (tested.Count == 0) continue;
            if (tested.Count < MinSamples)
            {
                hidden++;
                continue;
            }
            var low = tested.Count(r => r.Status == LabResultStatus.Deficient);
            if (low == 0) continue;
            items.Add(new FarmerSoilProblemDto
            {
                Parameter = parameter,
                Short = shortLabel,
                Label = plain,
                Samples = tested.Count,
                LowSamples = low,
                LowPct = Pct(low, tested.Count)
            });
        }

        dto.HiddenForFewSamples = hidden;
        dto.Items = items
            .OrderByDescending(i => i.LowPct).ThenByDescending(i => i.Samples)
            .Take(MaxSoilProblems)
            .ToList();
        return dto;
    }

    /// <summary>The "talked about" score: recent replies weigh most, then likes, then views.</summary>
    public static int TalkScore(TrendPost p) => p.RecentReplies * 3 + p.LikeCount + p.Views / 10;

    /// <summary>
    /// "Talked about near you": the top <see cref="MaxTalked"/> discussions by score, newest activity
    /// first on ties. Discussions with no reply, like or view at all are left out.
    /// </summary>
    public static List<FarmerTalkedDto> TopTalked(IEnumerable<TrendPost> posts) =>
        posts
            .Select(p => new { Post = p, Score = TalkScore(p) })
            .Where(x => x.Score > 0 || x.Post.ReplyCount > 0)
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Post.LastActivityAt).ThenByDescending(x => x.Post.Id)
            .Take(MaxTalked)
            .Select(x => new FarmerTalkedDto
            {
                Id = x.Post.Id,
                Title = x.Post.Title,
                Crop = x.Post.Crop,
                Category = x.Post.Category,
                Status = x.Post.Status,
                ReplyCount = x.Post.ReplyCount,
                RecentReplies = x.Post.RecentReplies,
                Views = x.Post.Views,
                LastActivityAt = x.Post.LastActivityAt
            })
            .ToList();

    /// <summary>District name for District, state name for State, null for None.</summary>
    public static string? AreaName(FarmerAreaScope scope, string? districtName, string? stateName) => scope switch
    {
        FarmerAreaScope.District => districtName,
        FarmerAreaScope.State => stateName,
        _ => null
    };

    /// <summary>Whole-number percentage, 0 when there is nothing to divide by.</summary>
    public static int Pct(int part, int whole) =>
        whole <= 0 ? 0 : (int)Math.Round(part * 100.0 / whole, MidpointRounding.AwayFromZero);
}
