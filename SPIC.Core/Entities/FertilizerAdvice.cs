namespace SPIC.Core.Entities;

// Farmer Portal Task 4 (docs/farmer-portal.md): fertilizer recommendation. Crop norms and soil tips are
// seeded in migration V36_FertilizerAdvice from TNAU Agritech / ICAR sources (every row names its source);
// the farmer's runs are saved so later tasks (chatbot, yield forecast) can read the latest one.

/// <summary>PerHectare: N / P2O5 / K2O in kg per hectare. PerPlant: grams per plant (banana) or palm
/// (coconut) for the season / year, with <see cref="FertilizerCropNorm.PlantsPerHectare"/>.</summary>
public enum FertilizerDoseBasis
{
    PerHectare = 0,
    PerPlant = 1
}

/// <summary>How a split's day numbers are counted.</summary>
public enum FertilizerDayBasis
{
    /// <summary>Days after sowing.</summary>
    Sowing = 0,
    /// <summary>Days after planting / transplanting.</summary>
    Planting = 1,
    /// <summary>A named stage or month ("At tasselling", "3rd month after planting", coconut "June - July");
    /// no day numbers, the stage text says when.</summary>
    Calendar = 2
}

/// <summary>Watered crop, rain-fed crop, or either (a tip that applies to both).</summary>
public enum FertilizerWaterRegime
{
    Any = 0,
    Irrigated = 1,
    Rainfed = 2
}

/// <summary>
/// The blanket (general) recommendation for one crop and variant, as published by TNAU / ICAR. N, P2O5 and
/// K2O are the season totals (kg/ha, or g/plant for <see cref="FertilizerDoseBasis.PerPlant"/>); the
/// split rows add up to them exactly (pinned by the regression harness). A norm with a <see cref="SoilType"/>
/// applies only to that soil (TNAU rainfed maize: Vertisols vs Alfisols); null = every soil.
/// </summary>
public class FertilizerCropNorm
{
    public int Id { get; set; }
    /// <summary>Crop name as the farmer sees it ("Cotton", "Paddy", "Black gram").</summary>
    public string Crop { get; set; } = string.Empty;
    /// <summary>"Irrigated varieties", "Irrigated hybrids", "Rainfed", "Medium duration (135 days)" ...</summary>
    public string Variant { get; set; } = string.Empty;
    public FertilizerWaterRegime WaterRegime { get; set; }
    /// <summary>One of <see cref="SPIC.Core.FarmerPortal.FertilizerAdvisor.SoilTypes"/>, or null for every soil.</summary>
    public string? SoilType { get; set; }
    public FertilizerDoseBasis Basis { get; set; }
    public decimal N { get; set; }
    public decimal P2O5 { get; set; }
    public decimal K2O { get; set; }
    /// <summary>Per-plant crops only: plants (palms) per hectare at the source's spacing.</summary>
    public int? PlantsPerHectare { get; set; }
    /// <summary>Spacing text from the source ("1.8 x 1.8 m"), per-plant crops only.</summary>
    public string? Spacing { get; set; }
    /// <summary>Crop duration in days when the source states it; null otherwise.</summary>
    public int? DurationDays { get; set; }
    /// <summary>Short plain-words notes from the source (tissue culture banana +50 %, ...).</summary>
    public string? Notes { get; set; }
    /// <summary>Title of the source page.</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>URL of the source page.</summary>
    public string SourceUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FertilizerNormSplit> Splits { get; set; } = new List<FertilizerNormSplit>();
}

/// <summary>One application of a norm (basal or a top dressing): the nutrient amounts in the norm's
/// unit, applied on <see cref="DayFrom"/>-<see cref="DayTo"/> (null = at sowing / planting).</summary>
public class FertilizerNormSplit
{
    public int Id { get; set; }
    public int NormId { get; set; }
    public FertilizerCropNorm? Norm { get; set; }
    /// <summary>Order within the norm (1 = basal).</summary>
    public int Seq { get; set; }
    /// <summary>"Basal (at sowing)", "Top dressing", "Panicle initiation", "June - July" ...</summary>
    public string Stage { get; set; } = string.Empty;
    public int? DayFrom { get; set; }
    public int? DayTo { get; set; }
    public FertilizerDayBasis DayBasis { get; set; }
    public decimal N { get; set; }
    public decimal P2O5 { get; set; }
    public decimal K2O { get; set; }
}

/// <summary>Water / soil / nutrient management advice. Crop null = every crop; SoilType null = every soil;
/// WaterRegime Irrigated = only for watered crops (irrigation schedules); PhMin / PhMax limit a tip to soils with
/// PhMin &lt;= pH &lt; PhMax (lime tips only on acid soil).</summary>
public class CropSoilTip
{
    public int Id { get; set; }
    public string? Crop { get; set; }
    public string? SoilType { get; set; }
    public FertilizerWaterRegime WaterRegime { get; set; }
    /// <summary>Water, Soil or Nutrient (<see cref="SPIC.Core.FarmerPortal.FertilizerAdvisor.TipCategories"/>).</summary>
    public string Category { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Shown only when the soil pH is at least this (null = no lower limit).</summary>
    public decimal? PhMin { get; set; }
    /// <summary>Shown only when the soil pH is below this (null = no upper limit).</summary>
    public decimal? PhMax { get; set; }
}

/// <summary>
/// One fertilizer recommendation a farmer asked for. The summary columns are what later tasks read (the
/// chatbot's context, the yield forecast's NPK prefill); <see cref="InputsJson"/> /
/// <see cref="ResultJson"/> keep the full request and answer (FertilizerRecommendRequest /
/// FertilizerRecommendationDto). Timestamps are UTC.
/// </summary>
public class FertilizerRecommendationRun
{
    public long Id { get; set; }
    public string FarmerUserId { get; set; } = string.Empty;
    public int NormId { get; set; }
    public string Crop { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public string SoilType { get; set; } = string.Empty;
    public decimal AreaAcres { get; set; }
    /// <summary>Soil test values as used (kg/ha on the elemental basis: P as P, K as K; pH).</summary>
    public decimal SoilN { get; set; }
    public decimal SoilP { get; set; }
    public decimal SoilK { get; set; }
    public decimal SoilPh { get; set; }
    /// <summary>Low / Medium / High (the lab's wording), and the pH band key.</summary>
    public string NStatus { get; set; } = string.Empty;
    public string PStatus { get; set; } = string.Empty;
    public string KStatus { get; set; } = string.Empty;
    public string PhBand { get; set; } = string.Empty;
    /// <summary>Recommended nutrients after the soil test adjustment, kg per acre (season / year total).</summary>
    public decimal NeedNKgPerAcre { get; set; }
    public decimal NeedPKgPerAcre { get; set; }
    public decimal NeedKKgPerAcre { get; set; }
    /// <summary>True when N / P / K / pH came from the farmer's SPIC soil report.</summary>
    public bool FromSoilReport { get; set; }
    public int? SoilReportId { get; set; }
    /// <summary>One plain-words line ("Cotton, irrigated hybrids on Black Cotton Soil, 2 acres: N Low, P Low, K Low; ...").</summary>
    public string Summary { get; set; } = string.Empty;
    public string InputsJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
}
