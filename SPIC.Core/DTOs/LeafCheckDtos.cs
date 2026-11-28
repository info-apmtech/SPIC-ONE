using SPIC.Core.FarmerPortal;

namespace SPIC.Core.DTOs;

// Farmer portal Task 6 (docs/farmer-portal.md): the leaf-photo deficiency check, api/Farmer/leaf-check.

/// <summary>GET api/Farmer/leaf-check/status.</summary>
public class LeafCheckStatusDto
{
    public int DailyLimit { get; set; }
    public int UsedToday { get; set; }
    public int LeftToday { get; set; }
    public DateTime ResetsAtUtc { get; set; }
    /// <summary>True when a full SPIC AI check is possible now (key set, switched on, today's AI budget not used up);
    /// false = the basic colour check.</summary>
    public bool FullCheck { get; set; }
    public List<string> Crops { get; set; } = new();
    public long MaxPhotoBytes { get; set; }
}

public class LeafNutrientDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public double Likelihood { get; set; }
    /// <summary>Likely / Possible / Less likely.</summary>
    public string LikelihoodLabel { get; set; } = "";
}

public class LeafCorrectiveDto
{
    public string Nutrient { get; set; } = "";
    public string NutrientName { get; set; } = "";
    public string Product { get; set; } = "";
    /// <summary>Soil | Foliar.</summary>
    public string Method { get; set; } = "";
    public string Dose { get; set; } = "";
    public string? Note { get; set; }
    public string Source { get; set; } = "";
    public string? SourceUrl { get; set; }
    /// <summary>True when the row is for this crop (false = the general row).</summary>
    public bool ForCrop { get; set; }
}

/// <summary>One leaf check in full (POST answer, GET {id}).</summary>
public class LeafCheckResultDto
{
    public long Id { get; set; }
    public string Crop { get; set; } = "";
    public string LeafAgeHint { get; set; } = "unknown";
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>"ai" | "basic".</summary>
    public string Provider { get; set; } = "basic";
    /// <summary>no-key | paused | budget | ai-failed | not-a-leaf (null for an AI answer).</summary>
    public string? BasicReason { get; set; }
    /// <summary>"Basic colour check" for a basic answer, else null.</summary>
    public string? BasicLabel { get; set; }
    /// <summary>Why this is a basic colour check, in plain words (null for an AI answer).</summary>
    public string? Notice { get; set; }
    public string Condition { get; set; } = "unclear";
    public string ConditionLabel { get; set; } = "";
    public double Confidence { get; set; }
    public bool NotALeaf { get; set; }
    public List<LeafNutrientDto> Nutrients { get; set; } = new();
    public List<string> Symptoms { get; set; } = new();
    public string LeafAge { get; set; } = "unknown";
    public List<string> Advice { get; set; } = new();
    public bool NeedsSoilTest { get; set; }
    public LeafMetrics Metrics { get; set; } = new();
    public string PatternLabel { get; set; } = "";
    public string LccLabel { get; set; } = "";
    public List<LeafCorrectiveDto> Correctives { get; set; } = new();
    public string Disclaimer { get; set; } = "";
    /// <summary>True while the photo is stored (GET {id}/image).</summary>
    public bool HasImage { get; set; }
}

/// <summary>One row of GET api/Farmer/leaf-check/history.</summary>
public class LeafCheckSummaryDto
{
    public long Id { get; set; }
    public string Crop { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string Condition { get; set; } = "unclear";
    public string ConditionLabel { get; set; } = "";
    public bool NotALeaf { get; set; }
    public string? TopNutrient { get; set; }
    public string? TopNutrientName { get; set; }
    public double Confidence { get; set; }
    public string Provider { get; set; } = "basic";
    public int LccScore { get; set; }
    public bool HasImage { get; set; }
}

/// <summary>400 / 404 / 429 body.</summary>
public class LeafCheckErrorDto
{
    public string Message { get; set; } = "";
    public Dictionary<string, string> Errors { get; set; } = new();
    public int? RetryAfterSeconds { get; set; }
    public DateTime? ResetsAtUtc { get; set; }
    public int? DailyLimit { get; set; }
}
