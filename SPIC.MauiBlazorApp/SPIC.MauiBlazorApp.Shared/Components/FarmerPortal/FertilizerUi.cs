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
