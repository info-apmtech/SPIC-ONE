namespace SPIC.Core.Entities;

// Farmer Portal Task 6 (docs/farmer-portal.md, 2026-11-28): the leaf-photo deficiency check (/farmer/leaf-check).
// UTC times, no foreign key to the user (like the other farmer tables).

/// <summary>
/// One leaf-photo check of a farmer: the re-encoded photo (Uploads/FarmerLeaf/{userId}/{guid}.jpg), the colour
/// metrics, the diagnosis and who made it ("ai" = Claude vision, "basic" = the colour-check rules). Delete is soft:
/// the row stays (it counts against the daily cap) and the photo file is removed.
/// </summary>
public class LeafDiagnosis
{
    public long Id { get; set; }
    public string FarmerUserId { get; set; } = string.Empty;
    public string Crop { get; set; } = string.Empty;
    /// <summary>What the farmer said about the yellow leaves: "older" | "younger" | "all" | "unknown".</summary>
    public string LeafAgeHint { get; set; } = "unknown";
    /// <summary>Relative path under the API's Uploads root, e.g. "FarmerLeaf/{userId}/{guid}.jpg"; null once deleted.</summary>
    public string? ImagePath { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public int ImageBytes { get; set; }
    /// <summary>LeafMetrics as JSON.</summary>
    public string MetricsJson { get; set; } = "{}";
    /// <summary>LeafCheckResultDto (without the image) as JSON; null while the check is being made.</summary>
    public string? ResultJson { get; set; }
    /// <summary>"ai" | "basic".</summary>
    public string? Provider { get; set; }
    /// <summary>Why the basic colour check was used: no-key, paused, budget, ai-failed, not-a-leaf (null for ai).</summary>
    public string? BasicReason { get; set; }
    /// <summary>healthy | nutrient_deficiency | disease | pest | unclear.</summary>
    public string? Condition { get; set; }
    /// <summary>The most likely nutrient code (N, P, K, Mg, Zn, Fe, S, B, Ca, Mn), if any.</summary>
    public string? TopNutrient { get; set; }
    public double? Confidence { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}

/// <summary>
/// A corrective for one nutrient shortage (seeded, sources on every row: Spic.Infrastructure/Data/NutrientCorrectiveSeed.cs).
/// <see cref="Crop"/> null = the general row used when the crop has none of its own.
/// </summary>
public class NutrientCorrective
{
    public int Id { get; set; }
    /// <summary>N, P, K, Mg, Zn, Fe, S, B, Ca, Mn.</summary>
    public string Nutrient { get; set; } = string.Empty;
    public string? Crop { get; set; }
    public string Product { get; set; } = string.Empty;
    /// <summary>"Soil" | "Foliar".</summary>
    public string Method { get; set; } = "Soil";
    public string Dose { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
