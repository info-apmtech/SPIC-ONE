using System.Text.Json.Serialization;
using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

// Farmer Portal Task 4 contracts (docs/farmer-portal.md): fertilizer recommendation.
//   GET  api/Farmer/fertilizer/options        -> FertilizerOptionsDto (crops, variants, soil types, slider ranges)
//   GET  api/Farmer/fertilizer/soil-report    -> FertilizerSoilReportDto (N / P / K / pH of the latest SPIC soil report)
//   POST api/Farmer/fertilizer/recommend      FertilizerRecommendRequest -> FertilizerRecommendationDto (saved as a run)
//   GET  api/Farmer/fertilizer/history        -> List<FertilizerRunSummaryDto> (latest 20)
//   GET  api/Farmer/fertilizer/history/{id}   -> FertilizerRecommendationDto (one saved run, the caller's own)
//   GET  api/Farmer/fertilizer/latest         -> FertilizerRunSummaryDto, 204 when none (for the chatbot / yield forecast)
// Enums travel as numbers like the rest of the API.

public class FertilizerOptionsDto
{
    public List<FertilizerCropOptionDto> Crops { get; set; } = new();
    public List<string> SoilTypes { get; set; } = new();
    /// <summary>N, P, K and pH slider ranges and rating thresholds (the lab master's when present).</summary>
    public List<FertilizerRangeDto> Ranges { get; set; } = new();
    /// <summary>The products the schedule uses (generic grades; SPIC names where the lab master names them).</summary>
    public List<FertilizerProductDto> Products { get; set; } = new();
}

public class FertilizerCropOptionDto
{
    public string Crop { get; set; } = string.Empty;
    public List<FertilizerVariantOptionDto> Variants { get; set; } = new();
}

public class FertilizerVariantOptionDto
{
    public int NormId { get; set; }
    public string Variant { get; set; } = string.Empty;
    /// <summary>Null = every soil; else the norm applies only to this soil type.</summary>
    public string? SoilType { get; set; }
    public FertilizerWaterRegime WaterRegime { get; set; }
    public FertilizerDoseBasis Basis { get; set; }
    public int? PlantsPerHectare { get; set; }
    public string? Spacing { get; set; }
}

/// <summary>One soil value: slider range, default, and the rating thresholds.</summary>
public class FertilizerRangeDto
{
    /// <summary>"N", "P", "K" or "pH".</summary>
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Min { get; set; }
    public double Max { get; set; }
    public double Step { get; set; }
    public double Default { get; set; }
    /// <summary>Values below this are Low (pH: Acidic).</summary>
    public double LowBelow { get; set; }
    /// <summary>Values above this are High (pH: Alkaline).</summary>
    public double HighAbove { get; set; }
    public double? ModerateFrom { get; set; }
    public string LowLabel { get; set; } = "Low";
    public string MediumLabel { get; set; } = "Medium";
    public string HighLabel { get; set; } = "High";
    /// <summary>Always false since fix round 1: the farmer tool uses its own elemental ratings, not the lab master.</summary>
    public bool FromLabMaster { get; set; }
    /// <summary>The basis of Min / Max / thresholds: "N", "P", "K" (elemental) or "" for pH.</summary>
    public string Basis { get; set; } = string.Empty;
    /// <summary>The oxide basis the farmer may switch to ("P2O5", "K2O"), null when there is none.</summary>
    public string? AltBasis { get; set; }
    /// <summary>Oxide = elemental x AltFactor (P2O5 2.29, K2O 1.2).</summary>
    public double? AltFactor { get; set; }
}

public class FertilizerProductDto
{
    /// <summary>"Urea", "DAP", "MOP", "SSP".</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>"46% N", "18-46-0", "60% K2O", "16% P2O5".</summary>
    public string Grade { get; set; } = string.Empty;
    /// <summary>The SPIC product of this grade when the lab's schedule master names it ("SPIC Urea"), else null.</summary>
    public string? SpicName { get; set; }
    public decimal BagKg { get; set; }
}

/// <summary>The latest SPIC soil report of a farmer linked to a SAS farmer record, as kg/ha (and pH).</summary>
public class FertilizerSoilReportDto
{
    public bool Available { get; set; }
    /// <summary>Plain words when not available ("No soil report yet ...").</summary>
    public string? Message { get; set; }
    public int? ReportId { get; set; }
    public string? ReportCode { get; set; }
    /// <summary>When the report was made (server time, as the lab stores it).</summary>
    public DateTime? GeneratedAt { get; set; }
    public string? SampleCrop { get; set; }
    /// <summary>N, P (as P) and K (as K) in kg/ha, and pH; null when the report has no usable value.</summary>
    public double? N { get; set; }
    public double? P { get; set; }
    public double? K { get; set; }
    public double? Ph { get; set; }
    /// <summary>The basis the report printed P on ("P2O5" for kg/ha values of the SPIC lab, "P" for ppm), so the screen
    /// can show the farmer the same number as the report.</summary>
    public string? ReportPBasis { get; set; }
    /// <summary>P exactly as on the report, on <see cref="ReportPBasis"/> (kg/ha; a kg/acre value already x 2.47105), so the
    /// screen shows the report's own number without a round trip through P.</summary>
    public double? PAsReported { get; set; }
    /// <summary>"N", "P", "K", "pH" codes the report does not give (the farmer sets those).</summary>
    public List<string> Missing { get; set; } = new();
    /// <summary>Notes on unit conversions or values that could not be read.</summary>
    public List<string> Notes { get; set; } = new();
}

public class FertilizerRecommendRequest
{
    public int NormId { get; set; }
    public string SoilType { get; set; } = string.Empty;
    public decimal AreaAcres { get; set; }
    /// <summary>Per-plant crops: the farmer's own count; null = from the area at the source's spacing.</summary>
    public int? PlantCount { get; set; }
    /// <summary>Available N, P and K in kg/ha as entered (P on <see cref="PBasis"/>, K on <see cref="KBasis"/>), and soil pH.
    /// On the wire they are "n", "p", "k" and "ph" and may not be left out (final fix wave, #27): an omitted value stays
    /// null in <see cref="NInput"/> etc. and <c>FertilizerAdvisor.Validate</c> answers 400 instead of reading it as 0.
    /// Code uses N / P / K / Ph (0 when missing, but a request with a missing value never gets past Validate).</summary>
    [JsonIgnore] public decimal N { get => NInput ?? 0; set => NInput = value; }
    [JsonIgnore] public decimal P { get => PInput ?? 0; set => PInput = value; }
    [JsonIgnore] public decimal K { get => KInput ?? 0; set => KInput = value; }
    [JsonIgnore] public decimal Ph { get => PhInput ?? 0; set => PhInput = value; }
    [JsonPropertyName("n")] public decimal? NInput { get; set; }
    [JsonPropertyName("p")] public decimal? PInput { get; set; }
    [JsonPropertyName("k")] public decimal? KInput { get; set; }
    [JsonPropertyName("ph")] public decimal? PhInput { get; set; }
    /// <summary>"P" (default) or "P2O5": the basis <see cref="P"/> is entered on. Ratings always use P.</summary>
    public string? PBasis { get; set; }
    /// <summary>"K" (default) or "K2O": the basis <see cref="K"/> is entered on. Ratings always use K.</summary>
    public string? KBasis { get; set; }
    /// <summary>The report the values came from ("Use my soil report"); checked against the caller's own reports.</summary>
    public int? SoilReportId { get; set; }
}

public class FertilizerRecommendationDto
{
    public long RunId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int NormId { get; set; }
    public string Crop { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public string SoilType { get; set; } = string.Empty;
    public FertilizerWaterRegime WaterRegime { get; set; }
    public FertilizerDoseBasis Basis { get; set; }
    public decimal AreaAcres { get; set; }
    public decimal AreaHectares { get; set; }
    /// <summary>Per-plant crops: the plants (palms) the totals are for.</summary>
    public int? PlantCount { get; set; }
    public bool PlantCountFromSpacing { get; set; }
    public string? Spacing { get; set; }
    public int? DurationDays { get; set; }
    public bool FromSoilReport { get; set; }
    public int? SoilReportId { get; set; }

    public List<FertilizerNutrientResultDto> Nutrients { get; set; } = new();
    public FertilizerPhResultDto Ph { get; set; } = new();
    /// <summary>The schedule: one row per application, products for the whole area.</summary>
    public List<FertilizerStageDto> Stages { get; set; } = new();
    /// <summary>Season totals per product: the DAP route (main) and the SSP route (alternative).</summary>
    public List<FertilizerProductLineDto> Totals { get; set; } = new();
    public List<FertilizerProductLineDto> SspTotals { get; set; } = new();

    public List<FertilizerTipDto> WaterTips { get; set; } = new();
    public List<FertilizerTipDto> SoilTips { get; set; } = new();
    public List<FertilizerTipDto> NutrientTips { get; set; } = new();

    public string? NormNotes { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    /// <summary>How the soil test changed the dose (the rule and where it comes from).</summary>
    public string AdjustmentRule { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = string.Empty;
}

public class FertilizerNutrientResultDto
{
    /// <summary>"N", "P" or "K".</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>"Nitrogen (N)" ...</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>"N", "P2O5", "K2O": what the dose is counted as.</summary>
    public string DoseAs { get; set; } = string.Empty;
    /// <summary>The soil value the rating used, kg/ha on the elemental basis (P as P, K as K).</summary>
    public decimal SoilValue { get; set; }
    /// <summary>The value as the farmer entered it and its basis ("N", "P", "P2O5", "K", "K2O").</summary>
    public decimal SoilValueEntered { get; set; }
    public string SoilBasis { get; set; } = string.Empty;
    /// <summary>"Low", "Medium" or "High".</summary>
    public string Level { get; set; } = string.Empty;
    /// <summary>The lab's label for the level (the soil report's word).</summary>
    public string LevelLabel { get; set; } = string.Empty;
    /// <summary>The lab status name: Deficient / Moderate / Normal / Excess.</summary>
    public string LabStatus { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    /// <summary>"+25 %", "as recommended", "-25 %".</summary>
    public string AdjustmentText { get; set; } = string.Empty;
    /// <summary>The source's dose (kg/ha, or g/plant for per-plant crops).</summary>
    public decimal NormDose { get; set; }
    public decimal AdjustedDose { get; set; }
    public decimal NeedKgPerAcre { get; set; }
    /// <summary>Total nutrient for the whole area (kg).</summary>
    public decimal TotalKg { get; set; }
}

/// <summary>pH band keys: StronglyAcidic (&lt; 5.5), Acidic (to the lab's neutral range), Neutral,
/// Alkaline (to 8.5), StronglyAlkaline (&gt; 8.5).</summary>
public class FertilizerPhResultDto
{
    public decimal Value { get; set; }
    public string Band { get; set; } = string.Empty;
    /// <summary>"Low" (acidic), "Medium" (neutral) or "High" (alkaline), for the status colour.</summary>
    public string Level { get; set; } = string.Empty;
    /// <summary>The lab's label: Acidic / Neutral / Alkaline.</summary>
    public string LevelLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> Advice { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

public class FertilizerStageDto
{
    public int Seq { get; set; }
    public string Stage { get; set; } = string.Empty;
    /// <summary>"At sowing", "40-45 days after sowing", "June - July" ...</summary>
    public string When { get; set; } = string.Empty;
    public int? DayFrom { get; set; }
    public int? DayTo { get; set; }
    /// <summary>Nutrients for the whole area, kg.</summary>
    public decimal NKg { get; set; }
    public decimal PKg { get; set; }
    public decimal KKg { get; set; }
    /// <summary>The DAP route: DAP for phosphorus (its nitrogen counted), urea for the rest of the N, MOP for K,
    /// SSP only for phosphorus the DAP cannot carry without too much nitrogen.</summary>
    public List<FertilizerProductLineDto> Products { get; set; } = new();
    /// <summary>The SSP route: SSP for phosphorus, urea for N, MOP for K.</summary>
    public List<FertilizerProductLineDto> SspProducts { get; set; } = new();
}

public class FertilizerProductLineDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string? SpicName { get; set; }
    /// <summary>Kilograms for the whole area (whole kg).</summary>
    public decimal Kg { get; set; }
    public decimal BagKg { get; set; }
    /// <summary>Kg / bag size, 1 decimal.</summary>
    public decimal Bags { get; set; }
    /// <summary>Per-plant crops: grams per plant (whole grams).</summary>
    public decimal? GramsPerPlant { get; set; }
}

public class FertilizerTipDto
{
    public string Category { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

/// <summary>A saved run in short: what the history list, the chatbot and the yield forecast read.</summary>
public class FertilizerRunSummaryDto
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int NormId { get; set; }
    public string Crop { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public string SoilType { get; set; } = string.Empty;
    public decimal AreaAcres { get; set; }
    public decimal SoilN { get; set; }
    public decimal SoilP { get; set; }
    public decimal SoilK { get; set; }
    public decimal SoilPh { get; set; }
    /// <summary>Low / Medium / High.</summary>
    public string NStatus { get; set; } = string.Empty;
    public string PStatus { get; set; } = string.Empty;
    public string KStatus { get; set; } = string.Empty;
    public string PhBand { get; set; } = string.Empty;
    public decimal NeedNKgPerAcre { get; set; }
    public decimal NeedPKgPerAcre { get; set; }
    public decimal NeedKKgPerAcre { get; set; }
    public bool FromSoilReport { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class FertilizerErrorDto
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> Errors { get; set; } = new();
}
