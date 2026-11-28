namespace SPIC.Core.DTOs;

// Farmer portal Task 7 (docs/farmer-portal.md): the harvest yield forecast, api/Farmer/yield.
//   GET  api/Farmer/yield/options        crops (norm per irrigation class), methods, slider ranges, prefill
//   POST api/Farmer/yield/predict        YieldPredictRequest -> YieldPredictionDto (saved); 400 { message, errors }
//   GET  api/Farmer/yield/history        latest 20 YieldPredictionSummaryDto
//   GET  api/Farmer/yield/history/{id}   one saved forecast in full (404 for anyone else's)

public class YieldOptionsDto
{
    public List<YieldCropOptionDto> Crops { get; set; } = new();
    public List<YieldMethodOptionDto> Methods { get; set; } = new();
    public YieldSliderDto Ndvi { get; set; } = new();
    /// <summary>"Not sure? pick how your crop looks": sparse / medium / dense with the NDVI each stands for.</summary>
    public List<YieldLookOptionDto> Looks { get; set; } = new();
    /// <summary>Rainfall slider; its zones come from the chosen crop's band (<see cref="YieldNormOptionDto"/>).</summary>
    public YieldSliderDto Rainfall { get; set; } = new();
    public YieldSliderDto Npk { get; set; } = new();
    public YieldPrefillDto Prefill { get; set; } = new();
}

public class YieldCropOptionDto
{
    public string Crop { get; set; } = "";
    public string Product { get; set; } = "";
    /// <summary>One per irrigation class the crop has a norm for (Irrigated first).</summary>
    public List<YieldNormOptionDto> Norms { get; set; } = new();
}

public class YieldNormOptionDto
{
    public int NormId { get; set; }
    /// <summary>"Irrigated" | "Rainfed".</summary>
    public string IrrigationClass { get; set; } = "";
    public double TypicalTPerHa { get; set; }
    public double LowTPerHa { get; set; }
    public double HighTPerHa { get; set; }
    public int RainLowMm { get; set; }
    public int RainHighMm { get; set; }
}

public class YieldMethodOptionDto
{
    /// <summary>canal-borewell | rainfed | drip | sprinkler.</summary>
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>"Irrigated" | "Rainfed".</summary>
    public string IrrigationClass { get; set; } = "";
    public string Icon { get; set; } = "";
}

public class YieldSliderDto
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Step { get; set; }
    public double Default { get; set; }
    /// <summary>Zone thresholds for the slider colours (below = low zone, above = high zone).</summary>
    public double LowBelow { get; set; }
    public double HighAbove { get; set; }
    public string LowLabel { get; set; } = "";
    public string MediumLabel { get; set; } = "";
    public string HighLabel { get; set; } = "";
}

public class YieldLookOptionDto
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Hint { get; set; } = "";
    public double Ndvi { get; set; }
}

public class YieldPrefillDto
{
    public string? Crop { get; set; }
    public decimal AreaAcres { get; set; }
    public string Method { get; set; } = "";
    public int NpkPercent { get; set; }
    /// <summary>True when the crop / area / irrigation came from the farmer's profile.</summary>
    public bool CropFromProfile { get; set; }
    public bool AreaFromProfile { get; set; }
    public bool MethodFromProfile { get; set; }
    /// <summary>The farmer's latest fertilizer advice (crop, when), shown beside the NPK slider; null when none.</summary>
    public string? LastFertilizerCrop { get; set; }
    public DateTime? LastFertilizerAtUtc { get; set; }
}

public class YieldPredictRequest
{
    public string Crop { get; set; } = "";
    public string Method { get; set; } = "";
    public decimal AreaAcres { get; set; }
    public double Ndvi { get; set; }
    /// <summary>"value" (slider / number) or "look" (sparse / medium / dense): a look widens the range.</summary>
    public string NdviSource { get; set; } = "value";
    public int RainfallMm { get; set; }
    public int NpkPercent { get; set; }
}

public class YieldFactorDto
{
    /// <summary>ndvi | rain | npk | irrigation.</summary>
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>The multiplier (1.0 = no loss).</summary>
    public double Value { get; set; }
    /// <summary>Tonnes this factor costs on the whole area (0 when it costs nothing, negative when it adds).</summary>
    public double LossT { get; set; }
    /// <summary>ok | low | high (above the best value).</summary>
    public string Status { get; set; } = "ok";
    public string Note { get; set; } = "";
}

public class YieldTipDto
{
    public string Code { get; set; } = "";
    public string Text { get; set; } = "";
    /// <summary>About how many tonnes acting on it could add (null for warnings).</summary>
    public double? GainT { get; set; }
    public string? Href { get; set; }
    public string? LinkText { get; set; }
}

public class YieldPredictionDto
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int NormId { get; set; }
    public string Crop { get; set; } = "";
    public string Product { get; set; } = "";
    public string Method { get; set; } = "";
    public string MethodLabel { get; set; } = "";
    public string IrrigationClass { get; set; } = "";
    public decimal AreaAcres { get; set; }
    public double AreaHectares { get; set; }
    public double Ndvi { get; set; }
    public string NdviSource { get; set; } = "value";
    public int RainfallMm { get; set; }
    public int NpkPercent { get; set; }

    public double NormTPerHa { get; set; }
    public double NormLowTPerHa { get; set; }
    public double NormHighTPerHa { get; set; }
    public double? StateAverageTPerHa { get; set; }
    public int RainLowMm { get; set; }
    public int RainHighMm { get; set; }

    public double PerHectareT { get; set; }
    public double PerAcreT { get; set; }
    /// <summary>Expected harvest on the whole area, tonnes (2 decimals).</summary>
    public double ExpectedT { get; set; }
    public double LowT { get; set; }
    public double HighT { get; set; }
    public double ExpectedQuintals { get; set; }
    /// <summary>Half width of the range, percent (12-25).</summary>
    public int RangePercent { get; set; }
    /// <summary>0-100 for the ring.</summary>
    public int ConfidenceScore { get; set; }
    /// <summary>High / Medium / Low confidence.</summary>
    public string ConfidenceLabel { get; set; } = "";
    public List<string> ConfidenceReasons { get; set; } = new();

    public List<YieldFactorDto> Factors { get; set; } = new();
    /// <summary>Code of the factor that costs the most yield; null when none costs anything.</summary>
    public string? BiggestLoss { get; set; }
    public List<YieldTipDto> Tips { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string Source { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}

public class YieldPredictionSummaryDto
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Crop { get; set; } = "";
    public string Method { get; set; } = "";
    public decimal AreaAcres { get; set; }
    public decimal ExpectedT { get; set; }
    public decimal LowT { get; set; }
    public decimal HighT { get; set; }
    public string ConfidenceLabel { get; set; } = "";
}

public class YieldErrorDto
{
    public string Message { get; set; } = "";
    public Dictionary<string, string> Errors { get; set; } = new();
}
