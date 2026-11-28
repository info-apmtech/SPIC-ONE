using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

/// <summary>Pure helpers of the fertilizer advice screens (farmer portal Task 4; checked by the regression harness).</summary>
public static class FertilizerUi
{
    public static readonly IReadOnlyList<string> StepNames = new[] { "Soil", "Crop", "Advice" };

    /// <summary>"Low" / "Medium" / "High" from the API -> the status colour.</summary>
    public static FpLevel Level(string? level) => (level ?? "").Trim().ToLowerInvariant() switch
    {
        "low" => FpLevel.Low,
        "medium" => FpLevel.Medium,
        "high" => FpLevel.High,
        _ => FpLevel.Unknown
    };

    /// <summary>The variant names of a crop, in master order, each once ("Rainfed" maize has one norm per soil).</summary>
    public static List<string> VariantNames(FertilizerCropOptionDto? crop) =>
        crop?.Variants.Select(v => v.Variant).Distinct(StringComparer.Ordinal).ToList() ?? new List<string>();

    /// <summary>The norm for a crop, variant and soil: the one made for that soil, else the one for every soil, else null.</summary>
    public static FertilizerVariantOptionDto? Resolve(FertilizerCropOptionDto? crop, string? variant, string? soil)
    {
        if (crop is null || string.IsNullOrEmpty(variant)) return null;
        var rows = crop.Variants.Where(v => v.Variant == variant).ToList();
        return rows.FirstOrDefault(v => v.SoilType is not null && v.SoilType == soil)
               ?? rows.FirstOrDefault(v => v.SoilType is null);
    }

    /// <summary>The soils a soil-specific variant covers ("Black Cotton Soil or Red Soil"), or null when it covers every soil.</summary>
    public static string? SoilsFor(FertilizerCropOptionDto? crop, string? variant)
    {
        if (crop is null || string.IsNullOrEmpty(variant)) return null;
        var rows = crop.Variants.Where(v => v.Variant == variant).ToList();
        if (rows.Count == 0 || rows.Any(v => v.SoilType is null)) return null;
        var soils = rows.Select(v => v.SoilType!).Distinct().ToList();
        return soils.Count == 1 ? soils[0] : string.Join(", ", soils.Take(soils.Count - 1)) + " or " + soils[^1];
    }

    /// <summary>The first crop of the profile that has advice ("paddy" matches "Paddy").</summary>
    public static string? ProfileCrop(IEnumerable<string>? profileCrops, IEnumerable<FertilizerCropOptionDto> crops)
    {
        var names = crops.Select(c => c.Crop).ToList();
        foreach (var p in profileCrops ?? Enumerable.Empty<string>())
        {
            var hit = names.FirstOrDefault(n => string.Equals(n, p?.Trim(), StringComparison.OrdinalIgnoreCase)
                                                || (string.Equals(p?.Trim(), "Rice", StringComparison.OrdinalIgnoreCase) && n == "Paddy"));
            if (hit is not null) return hit;
        }
        return null;
    }

    /// <summary>A slider as shown on a basis: thresholds, max and step scaled by the oxide factor (P2O5 2.29, K2O 1.2).</summary>
    public sealed record ShownRange(string Label, string Unit, double Min, double Max, double Step, double LowBelow, double HighAbove,
        string LowLabel, string MediumLabel, string HighLabel);

    public const string MissingHint = "Not in your report, enter it from your soil card.";

    /// <summary>The ending of the API's note for a value missing from the report (the slider shows <see cref="MissingHint"/> instead).</summary>
    public const string MissingNoteEnd = "enter it from your soil card.";

    public static double Factor(FertilizerRangeDto r, string? basis) =>
        r.AltBasis is not null && string.Equals(basis, r.AltBasis, StringComparison.Ordinal) && r.AltFactor is { } f ? f : 1.0;

    /// <summary>The range's thresholds as the shared rating rule's threshold (elemental basis).</summary>
    public static SPIC.Core.FarmerPortal.FertilizerAdvisor.Threshold ThresholdOf(FertilizerRangeDto r) =>
        new(r.Code, r.Label, r.Unit, (decimal)r.LowBelow, (decimal)r.HighAbove, r.ModerateFrom is { } m ? (decimal)m : null,
            r.LowLabel, r.MediumLabel, r.HighLabel, r.FromLabMaster);

    /// <summary>The slider on a basis. The zones come from <c>FertilizerAdvisor.InBasis</c>, the same exact numbers the
    /// server rates against (P2O5 25.19 / 50.38), so the chip always matches the result.</summary>
    public static ShownRange Shown(FertilizerRangeDto r, string? basis)
    {
        var f = Factor(r, basis);
        var name = f == 1.0 ? (r.Basis.Length > 0 ? r.Basis : r.Code) : r.AltBasis!;
        var label = r.Code == "pH" ? r.Label : $"{r.Label} ({name})";
        var unit = "kg/ha";   // the basis is in the label ("Available Phosphorus (P2O5)") and on the unit chips
        var max = f == 1.0 ? r.Max : Math.Ceiling(r.Max * f / 10) * 10;
        var zones = SPIC.Core.FarmerPortal.FertilizerAdvisor.InBasis(ThresholdOf(r), f == 1.0 ? null : r.AltBasis);
        return new ShownRange(label, unit, r.Min, max, r.Step, (double)zones.LowBelow, (double)zones.HighAbove,
            r.LowLabel, r.MediumLabel, r.HighLabel);
    }

    /// <summary>The chip's level for a value on a basis: the shared server rule (FertilizerAdvisor.RateAs).</summary>
    public static FpLevel LevelAs(double? value, FertilizerRangeDto r, string? basis)
    {
        if (value is not { } v || double.IsNaN(v)) return FpLevel.Unknown;
        var b = Factor(r, basis) == 1.0 ? null : r.AltBasis;
        return Level(SPIC.Core.FarmerPortal.FertilizerAdvisor.Level(SPIC.Core.FarmerPortal.FertilizerAdvisor.RateAs((decimal)v, ThresholdOf(r), b)));
    }

    /// <summary>"Low below 25.19, Medium 25.19 - 50.38, High above 50.38" (exact thresholds, up to 2 decimals).</summary>
    public static string Thresholds(ShownRange v) =>
        $"{v.LowLabel} below {v.LowBelow:0.##}, {v.MediumLabel} {v.LowBelow:0.##} - {v.HighAbove:0.##}, {v.HighLabel} above {v.HighAbove:0.##}";

    /// <summary>A value moved between bases (P &lt;-&gt; P2O5, K &lt;-&gt; K2O), 2 decimals, keeping its Low / Medium / High
    /// (<c>FertilizerAdvisor.ConvertKeepingLevel</c>); null stays null.</summary>
    public static double? Convert(double? value, FertilizerRangeDto r, string? from, string? to)
    {
        if (value is not { } v) return null;
        return (double)SPIC.Core.FarmerPortal.FertilizerAdvisor.ConvertKeepingLevel((decimal)v, ThresholdOf(r), from, to);
    }

    /// <summary>The strip after "Use my soil report": which values came from it and which the farmer must set.</summary>
    public static string ReportLine(FertilizerSoilReportDto report)
    {
        var when = report.GeneratedAt is { } at ? $" of {at.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}" : "";
        if (report.Missing.Count == 0) return $"Filled from your soil report {report.ReportCode}{when}.";
        var names = new Dictionary<string, string> { ["N"] = "nitrogen", ["P"] = "phosphorus", ["K"] = "potassium", ["pH"] = "pH" };
        var filled = names.Keys.Where(k => !report.Missing.Contains(k)).Select(k => names[k]).ToList();
        var missing = report.Missing.Select(k => names.TryGetValue(k, out var n) ? n : k).ToList();
        return $"Filled {Join(filled)} from your soil report {report.ReportCode}{when}. Your report has no usable {Join(missing)}: set it from your soil card. This advice will count as your own numbers.";
    }

    private static string Join(List<string> items) =>
        items.Count <= 1 ? string.Concat(items) : string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1];

    /// <summary>"Soil test 18 kg P2O5/ha (7.9 kg P/ha)" / "Soil test 100 kg/ha".</summary>
    public static string SoilLine(FertilizerNutrientResultDto n)
    {
        // Runs saved before fix round 1 have no entered value / basis: show the rating value.
        var value = string.IsNullOrEmpty(n.SoilBasis) ? n.SoilValue : n.SoilValueEntered;
        var entered = value.ToString("0.##", CultureInfo.InvariantCulture);
        var basis = string.IsNullOrEmpty(n.SoilBasis) ? n.Code : n.SoilBasis;
        if (n.Code == "N" || basis == n.Code) return n.Code == "N" ? $"Soil test {entered} kg/ha" : $"Soil test {entered} kg {basis}/ha";
        return $"Soil test {entered} kg {basis}/ha ({n.SoilValue.ToString("0.#", CultureInfo.InvariantCulture)} kg {n.Code}/ha)";
    }

    public static string Kg(decimal kg) =>
        kg >= 100 ? kg.ToString("N0", CultureInfo.InvariantCulture) : kg.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>"1.3 bags of 50 kg", "1 bag of 45 kg", "less than 0.1 bag".</summary>
    public static string Bags(decimal bags, decimal bagKg)
    {
        var size = bagKg.ToString("0", CultureInfo.InvariantCulture);
        if (bags <= 0) return $"less than 0.1 bag of {size} kg";
        var text = bags.ToString("0.#", CultureInfo.InvariantCulture);
        return $"{text} {(bags == 1m ? "bag" : "bags")} of {size} kg";
    }

    public static string Acres(decimal acres) =>
        $"{acres.ToString("0.##", CultureInfo.InvariantCulture)} {(acres == 1m ? "acre" : "acres")}";

    /// <summary>"N 150 : P2O5 75 : K2O 75 kg/ha" or "... g per plant".</summary>
    public static string Dose(FertilizerRecommendationDto r, Func<FertilizerNutrientResultDto, decimal> pick)
    {
        var unit = r.Basis == FertilizerDoseBasis.PerPlant ? (r.Crop == "Coconut" ? "g per palm a year" : "g per plant") : "kg/ha";
        var parts = r.Nutrients.Select(n => $"{n.DoseAs} {pick(n).ToString("0.##", CultureInfo.InvariantCulture)}");
        return string.Join(" : ", parts) + " " + unit;
    }

    /// <summary>"+25 % because your soil is Low" / "as recommended (Medium)" / "-25 % because your soil is High".</summary>
    public static string AdjustmentLine(FertilizerNutrientResultDto n) => n.Factor == 1m
        ? $"As recommended: your soil is {n.LevelLabel}"
        : $"{n.AdjustmentText} because your soil is {n.LevelLabel}";

    /// <summary>"Plants (palms)" label and the hint for per-plant crops.</summary>
    public static string PlantWord(string? crop) => string.Equals(crop, "Coconut", StringComparison.OrdinalIgnoreCase) ? "palms" : "plants";

    public static string PlantsHint(FertilizerVariantOptionDto v, decimal? acres, string? crop)
    {
        var word = PlantWord(crop);
        if (v.PlantsPerHectare is not { } perHa || acres is not > 0) return $"Leave empty to count {word} from your area.";
        var count = SPIC.Core.FarmerPortal.FertilizerAdvisor.PlantsFor(acres.Value, perHa);
        return $"Leave empty for about {count.ToString("N0", CultureInfo.InvariantCulture)} {word} ({v.Spacing} spacing).";
    }

    /// <summary>History line date in IST: "28 Nov 2026, 3:05 pm".</summary>
    public static string When(DateTime utc) =>
        FpUi.ToIst(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToString("d MMM yyyy, h:mm tt", CultureInfo.InvariantCulture).Replace("AM", "am").Replace("PM", "pm");

    /// <summary>Plain words for a pH band key.</summary>
    public static string PhWord(string? band) => band switch
    {
        SPIC.Core.FarmerPortal.FertilizerAdvisor.StronglyAcidic => "Strongly acidic",
        SPIC.Core.FarmerPortal.FertilizerAdvisor.Acidic => "Slightly acidic",
        SPIC.Core.FarmerPortal.FertilizerAdvisor.Neutral => "Neutral",
        SPIC.Core.FarmerPortal.FertilizerAdvisor.Alkaline => "Slightly alkaline",
        SPIC.Core.FarmerPortal.FertilizerAdvisor.StronglyAlkaline => "Strongly alkaline",
        _ => "Not known"
    };
}
