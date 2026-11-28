namespace SPIC.Core.Entities;

// Farmer Portal Task 7 (docs/farmer-portal.md, 2026-11-28): the harvest yield forecast (/farmer/yield).
// Crop norms are seeded in migration V41_YieldForecast (every row names its sources); the farmer's forecasts
// are saved (latest 20 shown). UTC times, no foreign key to the user (like the other farmer tables).

/// <summary>
/// The yield one crop gives under good management in Tamil Nadu, for one irrigation class ("Irrigated" or
/// "Rainfed"), with the season rainfall band in which that yield holds and the FAO water figures the rainfall
/// curve uses. Seed and sources: Spic.Infrastructure/Data/CropYieldNormSeed.cs. A crop without a Rainfed row is
/// not offered as rainfed (sugarcane, banana).
/// </summary>
public class CropYieldNorm
{
    public int Id { get; set; }
    /// <summary>Crop name as the farmer sees it ("Paddy", "Black gram"); the same names as the fertilizer tool.</summary>
    public string Crop { get; set; } = string.Empty;
    /// <summary>"Irrigated" | "Rainfed" (<see cref="SPIC.Core.FarmerPortal.YieldEstimator.Irrigated"/>).</summary>
    public string IrrigationClass { get; set; } = string.Empty;
    /// <summary>Typical yield with good management, tonnes per hectare of <see cref="Product"/>.</summary>
    public decimal TypicalTPerHa { get; set; }
    /// <summary>Realistic range of farm yields (poor to very good season), t/ha.</summary>
    public decimal LowTPerHa { get; set; }
    public decimal HighTPerHa { get; set; }
    /// <summary>Tamil Nadu state average (DES, Statistical Hand Book of Tamil Nadu 2022-23), t/ha of the same
    /// product; null when the state figure is not on this product's basis.</summary>
    public decimal? StateAverageTPerHa { get; set; }
    /// <summary>Season rainfall (mm) in which the typical yield holds; below it the deficit curve applies, above
    /// it the excess curve.</summary>
    public int RainLowMm { get; set; }
    public int RainHighMm { get; set; }
    /// <summary>Seasonal crop water need, mm per growing period (FAO Irrigation Water Management manual 3, table 5).</summary>
    public int WaterNeedMinMm { get; set; }
    public int WaterNeedMaxMm { get; set; }
    /// <summary>FAO yield response factor Ky for the whole season (Irrigation and Drainage Paper 33): relative yield
    /// loss per relative water deficit.</summary>
    public decimal YieldResponseFactor { get; set; }
    /// <summary>What is weighed at harvest ("paddy grain", "seed cotton (kapas)", "dry pods", "cane").</summary>
    public string Product { get; set; } = string.Empty;
    /// <summary>Always "t" (tonnes); quintals are shown beside it.</summary>
    public string Unit { get; set; } = "t";
    public string? Notes { get; set; }
    /// <summary>Plain names of the sources, e.g. "TNAU rice variety pages (CO 51 6,623 kg/ha ...); DES TN 2022-23".</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>The main source page.</summary>
    public string SourceUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>One forecast a farmer made: the inputs, the headline numbers and the full result as JSON.</summary>
public class YieldPrediction
{
    public long Id { get; set; }
    public string FarmerUserId { get; set; } = string.Empty;
    public int NormId { get; set; }
    public string Crop { get; set; } = string.Empty;
    /// <summary>canal-borewell | rainfed | drip | sprinkler.</summary>
    public string Method { get; set; } = string.Empty;
    public string IrrigationClass { get; set; } = string.Empty;
    public decimal AreaAcres { get; set; }
    public decimal Ndvi { get; set; }
    /// <summary>"value" (slider / number) | "look" (sparse / medium / dense).</summary>
    public string NdviSource { get; set; } = "value";
    public int RainfallMm { get; set; }
    public int NpkPercent { get; set; }
    public decimal ExpectedT { get; set; }
    public decimal LowT { get; set; }
    public decimal HighT { get; set; }
    public string ConfidenceLabel { get; set; } = string.Empty;
    /// <summary>YieldPredictionDto as JSON.</summary>
    public string ResultJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
}
