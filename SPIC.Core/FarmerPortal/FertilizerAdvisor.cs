using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Farmer portal Task 4: the fertilizer recommendation, as pure functions (pinned by the regression harness).
///
///  1. Rating: each soil value is rated exactly as the SPIC lab rates it on the soil report
///     (LabAutoResultEngine.Classify): value &lt; RangeMin = Low (Deficient); value &gt; RangeMax = High (Excess);
///     RangeMin &lt;= value &lt; ModerateFrom (when the master sets it) = Medium (Moderate); else Medium (Normal).
///     The thresholds come from the lab parameter master (S-N, S-P, S-K, S-PH); <see cref="DefaultN"/> ...
///     apply only when a master row is missing.
///  2. Adjustment: the crop norm's N, P2O5 and K2O are multiplied by the lab's dose factor for the status
///     (Sas:Lab:DoseFactors, the same table that scales the soil report's fertilizer schedule; defaults
///     Deficient 1.25, Moderate 1.10, Normal 1.00, Excess 0.75), i.e. the soil-test rating approach: low
///     +25 %, medium as recommended, high -25 % (ICAR, Indian Farming 70(3), 2020). Every split is scaled.
///  3. Products: per split, DAP carries the phosphorus (its 18 % N counted) as long as its nitrogen does not
///     exceed the split's N; any phosphorus left goes as SSP; urea gives the rest of the N; MOP the K2O. The
///     SSP route (SSP + urea + MOP) is offered as the alternative.
///  4. pH band and amendments, then the water / soil / nutrient tips of the crop and soil type.
/// </summary>
public static class FertilizerAdvisor
{
    /// <summary>1 hectare = 2.47105 acres.</summary>
    public const decimal AcresPerHectare = 2.47105m;

    /// <summary>1 ppm (mg/kg) of a nutrient in the 0-15 cm plough layer = 2.24 kg/ha (2.24 million kg of soil per hectare).</summary>
    public const decimal KgPerHaPerPpm = 2.24m;

    public const decimal MinAcres = 0.1m;
    public const decimal MaxAcres = 1000m;
    public const int MaxPlants = 500000;
    public const int HistorySize = 20;

    public const string StronglyAcidic = "StronglyAcidic";
    public const string Acidic = "Acidic";
    public const string Neutral = "Neutral";
    public const string Alkaline = "Alkaline";
    public const string StronglyAlkaline = "StronglyAlkaline";

    /// <summary>Below this pH the soil needs lime (brief band); TNAU rates soil below pH 6.0 as acidic.</summary>
    public const decimal StrongAcidBelow = 5.5m;

    /// <summary>Above this pH a soil may be sodic (TNAU: sodic soils have pH &gt; 8.5 and ESP &gt; 15).</summary>
    public const decimal StrongAlkaliAbove = 8.5m;

    public static readonly IReadOnlyList<string> SoilTypes = new[]
    {
        "Black Cotton Soil", "Red Soil", "Alluvial Soil", "Laterite Soil", "Sandy / Sandy Loam", "Clay Loam", "Coastal Saline"
    };

    public static readonly IReadOnlyList<string> TipCategories = new[] { "Water", "Soil", "Nutrient" };

    public const string Disclaimer = "General guidance from TNAU/ICAR norms. For exact advice take a SPIC soil test or ask your MDO.";

    public const string AdjustmentSource = "ICAR, Indian Farming 70(3): 18-21 (2020): in low or high soil test categories the general recommended dose is raised or lowered by 25 %. SPIC lab dose factors (Sas:Lab:DoseFactors).";

    public const string SoilConstraintsSource = "TNAU Agritech: Soil related constraints and their management";
    public const string SoilConstraintsUrl = "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html";

    // ------------------------------------------------------------------ thresholds

    /// <summary>A rating rule: Low below <see cref="LowBelow"/>, High above <see cref="HighAbove"/>.</summary>
    public sealed record Threshold(string Code, string Label, string Unit, decimal LowBelow, decimal HighAbove, decimal? ModerateFrom,
        string LowLabel, string NormalLabel, string HighLabel, bool FromLabMaster);

    // Defaults from the task brief, used only when the lab master has no active row (the seeded master is
    // N 280-560, P 22-56, K 110-280 kg/ha, pH 6.5-7.5: those win, so the advice matches the soil report).
    public static readonly Threshold DefaultN = new("N", "Available Nitrogen", "kg/ha", 280m, 560m, null, "Low", "Medium", "High", false);
    public static readonly Threshold DefaultP = new("P", "Available Phosphorus", "kg/ha", 11m, 22m, null, "Low", "Medium", "High", false);
    public static readonly Threshold DefaultK = new("K", "Available Potassium", "kg/ha", 118m, 280m, null, "Low", "Medium", "High", false);
    public static readonly Threshold DefaultPh = new("pH", "Soil pH", "", 6.5m, 7.5m, null, "Acidic", "Neutral", "Alkaline", false);

    /// <summary>A threshold from a lab parameter master row; the default when the row is missing, inactive or has no range.</summary>
    public static Threshold FromLab(LabParameter? p, Threshold fallback)
    {
        if (p is null || !p.IsActive || p.RangeMin is null || p.RangeMax is null || p.RangeMax < p.RangeMin) return fallback;
        return fallback with
        {
            LowBelow = p.RangeMin.Value,
            HighAbove = p.RangeMax.Value,
            ModerateFrom = p.ModerateFrom,
            LowLabel = Text(p.LowLabel, fallback.LowLabel),
            NormalLabel = Text(p.NormalLabel, fallback.NormalLabel),
            HighLabel = Text(p.HighLabel, fallback.HighLabel),
            FromLabMaster = true
        };
    }

    /// <summary>The lab status of a value: Deficient / Moderate / Normal / Excess (LabAutoResultEngine.Classify).</summary>
    public static LabResultStatus Rate(decimal value, Threshold t)
    {
        if (value < t.LowBelow) return LabResultStatus.Deficient;
        if (value > t.HighAbove) return LabResultStatus.Excess;
        if (t.ModerateFrom is { } m && value < m) return LabResultStatus.Moderate;
        return LabResultStatus.Normal;
    }

    /// <summary>Low / Medium / High for a status (Moderate is Medium, like the report's "Medium" label).</summary>
    public static string Level(LabResultStatus status) => status switch
    {
        LabResultStatus.Deficient => "Low",
        LabResultStatus.Excess => "High",
        _ => "Medium"
    };

    /// <summary>The word the soil report prints for the status.</summary>
    public static string LevelLabel(LabResultStatus status, Threshold t) => status switch
    {
        LabResultStatus.Deficient => t.LowLabel,
        LabResultStatus.Excess => t.HighLabel,
        LabResultStatus.Moderate => "Medium",
        _ => t.NormalLabel
    };

    /// <summary>The lab's default dose factors (LabFertilizerSchedule.DefaultFactors); the API passes the configured ones.</summary>
    public static decimal DefaultFactor(LabResultStatus status) => status switch
    {
        LabResultStatus.Deficient => 1.25m,
        LabResultStatus.Moderate => 1.10m,
        LabResultStatus.Excess => 0.75m,
        _ => 1.00m
    };

    /// <summary>"+25 %", "-25 %", "as recommended".</summary>
    public static string AdjustmentText(decimal factor)
    {
        if (factor == 1m) return "as recommended";
        var pct = Math.Round((factor - 1m) * 100m, 0, MidpointRounding.AwayFromZero);
        return (pct > 0 ? "+" : "") + pct.ToString("0", CultureInfo.InvariantCulture) + " %";
    }

    // ------------------------------------------------------------------ units

    /// <summary>
    /// A soil report value in kg/ha: "kg/ha" (or blank) as it is, "kg/acre" x 2.47105, "ppm" / "mg/kg" x 2.24
    /// (0-15 cm plough layer). Null for any other unit (the value is not used, the farmer is told).
    /// </summary>
    public static decimal? ToKgPerHa(decimal value, string? unit, out string? note)
    {
        note = null;
        var u = (unit ?? "").Trim().ToLowerInvariant().Replace(" ", "");
        switch (u)
        {
            case "":
            case "-":
            case "kg/ha":
            case "kgha-1":
            case "kg/hectare":
                return value;
            case "kg/acre":
            case "kg/ac":
                note = "converted from kg/acre";
                return Math.Round(value * AcresPerHectare, 1, MidpointRounding.AwayFromZero);
            case "ppm":
            case "mg/kg":
                note = "converted from ppm";
                return Math.Round(value * KgPerHaPerPpm, 1, MidpointRounding.AwayFromZero);
            default:
                note = $"unit '{unit}' not understood";
                return null;
        }
    }

    public static decimal Hectares(decimal acres) => acres / AcresPerHectare;

    /// <summary>Plants on the area at the source's density (whole plants, at least 1).</summary>
    public static int PlantsFor(decimal acres, int plantsPerHectare) =>
        Math.Max(1, (int)Math.Round(Hectares(acres) * plantsPerHectare, 0, MidpointRounding.AwayFromZero));

    // ------------------------------------------------------------------ pH

    public sealed record PhBand(string Band, string Level, string LevelLabel, string Title, List<string> Advice);

    /// <summary>
    /// pH bands. The middle edges are the lab's neutral range (6.5 - 7.5 in the master) so the chip matches the
    /// soil report; 5.5 and 8.5 split strong from mild. Amendment doses are NOT computed: TNAU ties lime to a
    /// lime requirement test and gypsum to a gypsum requirement test of the soil.
    /// </summary>
    public static PhBand PhBandFor(decimal ph, Threshold t)
    {
        if (ph < StrongAcidBelow)
            return new(StronglyAcidic, "Low", t.LowLabel, "Strongly acidic soil: lime is needed", new List<string>
            {
                "Apply lime as per the lime requirement test of your soil, spread evenly and mixed into the soil (TNAU). Ask the SPIC soil lab for the lime requirement: the dose depends on the test, not on pH alone.",
                "Dolomite, basic slag or wood ash can be used instead of lime on a lime-equivalent basis (TNAU).",
                "Apply lime well before sowing and not together with fertilizer; add organic manure (FYM / compost) as well."
            });
        if (ph < t.LowBelow)
            return new(Acidic, "Low", t.LowLabel, "Slightly acidic soil", new List<string>
            {
                "Add organic manure (FYM / compost) every season.",
                "TNAU rates soil below pH 6.0 as acidic. If your pH is below 6.0, ask for a lime requirement test and lime as per the test (pulses: TNAU recommends lime below pH 6.0)."
            });
        if (ph <= t.HighAbove)
            return new(Neutral, "Medium", t.NormalLabel, "Neutral soil: no correction needed", new List<string>
            {
                "Your soil pH suits most crops. No lime or gypsum is needed for pH."
            });
        if (ph <= StrongAlkaliAbove)
            return new(Alkaline, "High", t.HighLabel, "Slightly alkaline soil", new List<string>
            {
                "Add organic manure or grow and plough in a green manure crop.",
                "Your SPIC soil report advises gypsum for alkaline soil. Gypsum is needed when the soil has too much sodium: check EC and sodium on your soil report and apply gypsum as per the gypsum requirement test (TNAU).",
                "Check Zinc and Iron on your soil report. If they are Low, follow the zinc advice for your crop below."
            });
        return new(StronglyAlkaline, "High", t.HighLabel, "Strongly alkaline soil: it may be sodic", new List<string>
        {
            "TNAU reclaims sodic soils (pH above 8.5) with gypsum as per the gypsum requirement test of the soil: plough at the right moisture, apply the gypsum, hold water in the field and drain it away to wash out the salts.",
            "Plough in a green manure crop at 5 t/ha (2 t/acre) (TNAU).",
            "Check Zinc and Iron on your soil report. If they are Low, follow the zinc advice for your crop below."
        });
    }

    // ------------------------------------------------------------------ products

    public sealed record Product(string Code, string Name, string Grade, string? SpicName, decimal BagKg, decimal NFraction, decimal PFraction, decimal KFraction);

    // Fertiliser (Control) Order grades. Urea is sold in 45 kg bags since March 2018 (Department of
    // Fertilizers); DAP, MOP and SSP in 50 kg bags. SPIC names only where the lab's schedule master
    // (LabCropRecommendation) names the product: "SPIC Urea" and "SPIC DAP".
    public static readonly Product Urea = new("Urea", "Urea", "46% N", "SPIC Urea", 45m, 0.46m, 0m, 0m);
    public static readonly Product Dap = new("DAP", "DAP", "18-46-0", "SPIC DAP", 50m, 0.18m, 0.46m, 0m);
    public static readonly Product Mop = new("MOP", "Muriate of Potash", "60% K2O", null, 50m, 0m, 0m, 0.60m);
    public static readonly Product Ssp = new("SSP", "Single Super Phosphate", "16% P2O5", null, 50m, 0m, 0.16m, 0m);

    public static readonly IReadOnlyList<Product> Products = new[] { Dap, Urea, Mop, Ssp };

    /// <summary>Product kg (unrounded) for N / P2O5 / K2O kg by the DAP route.</summary>
    public static Dictionary<string, decimal> DapRoute(decimal n, decimal p, decimal k)
    {
        var result = new Dictionary<string, decimal>();
        n = Math.Max(0, n);
        p = Math.Max(0, p);
        k = Math.Max(0, k);

        // DAP for P2O5, but never more N from DAP than the split needs.
        var dap = Math.Min(p / Dap.PFraction, n / Dap.NFraction);
        if (dap < 0) dap = 0;
        var pLeft = p - dap * Dap.PFraction;
        var nLeft = n - dap * Dap.NFraction;
        if (pLeft < 0.0001m) pLeft = 0;
        if (nLeft < 0.0001m) nLeft = 0;

        if (dap > 0) result[Dap.Code] = dap;
        if (pLeft > 0) result[Ssp.Code] = pLeft / Ssp.PFraction;
        if (nLeft > 0) result[Urea.Code] = nLeft / Urea.NFraction;
        if (k > 0) result[Mop.Code] = k / Mop.KFraction;
        return result;
    }

    /// <summary>Product kg (unrounded) by the SSP route: SSP for P2O5, urea for N, MOP for K2O.</summary>
    public static Dictionary<string, decimal> SspRoute(decimal n, decimal p, decimal k)
    {
        var result = new Dictionary<string, decimal>();
        if (p > 0) result[Ssp.Code] = p / Ssp.PFraction;
        if (n > 0) result[Urea.Code] = n / Urea.NFraction;
        if (k > 0) result[Mop.Code] = k / Mop.KFraction;
        return result;
    }

    /// <summary>Product lines (DAP, SSP, urea, MOP order), whole kg, bags to 1 decimal; grams per plant when given.</summary>
    public static List<FertilizerProductLineDto> Lines(Dictionary<string, decimal> kg, int? plants)
    {
        var lines = new List<FertilizerProductLineDto>();
        foreach (var product in new[] { Dap, Ssp, Urea, Mop })
        {
            if (!kg.TryGetValue(product.Code, out var amount) || amount <= 0) continue;
            var whole = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
            if (whole == 0 && amount > 0) whole = Math.Round(amount, 1, MidpointRounding.AwayFromZero);
            lines.Add(new FertilizerProductLineDto
            {
                Code = product.Code,
                Name = product.Name,
                Grade = product.Grade,
                SpicName = product.SpicName,
                Kg = whole,
                BagKg = product.BagKg,
                Bags = Math.Round(amount / product.BagKg, 1, MidpointRounding.AwayFromZero),
                GramsPerPlant = plants is > 0 ? Math.Round(amount * 1000m / plants.Value, 0, MidpointRounding.AwayFromZero) : null
            });
        }
        return lines;
    }

    // ------------------------------------------------------------------ the recommendation

    public sealed record Inputs(FertilizerCropNorm Norm, IReadOnlyList<FertilizerNormSplit> Splits, IReadOnlyList<CropSoilTip> Tips,
        Threshold N, Threshold P, Threshold K, Threshold Ph, Func<string, LabResultStatus, decimal> Factor, FertilizerRecommendRequest Request);

    /// <summary>Field errors (key = request field) for a request against the chosen norm (null when the norm is unknown).</summary>
    public static Dictionary<string, string> Validate(FertilizerRecommendRequest r, FertilizerCropNorm? norm)
    {
        var errors = new Dictionary<string, string>();
        if (norm is null || !norm.IsActive) errors["normId"] = "Please choose your crop.";
        if (!SoilTypes.Contains(r.SoilType ?? "")) errors["soilType"] = "Please choose your soil type.";
        else if (norm?.SoilType is { } only && !string.Equals(only, r.SoilType, StringComparison.Ordinal))
            errors["soilType"] = $"This recommendation is for {only} only.";
        if (r.AreaAcres < MinAcres || r.AreaAcres > MaxAcres) errors["areaAcres"] = $"Enter your area in acres ({MinAcres.ToString(CultureInfo.InvariantCulture)} to {MaxAcres.ToString("0", CultureInfo.InvariantCulture)}).";
        if (r.PlantCount is { } pc && (pc < 1 || pc > MaxPlants)) errors["plantCount"] = "Enter the number of plants (1 to 5,00,000).";
        if (r.N < 0 || r.N > 2000) errors["n"] = "Nitrogen must be between 0 and 2000 kg/ha.";
        if (r.P < 0 || r.P > 500) errors["p"] = "Phosphorus must be between 0 and 500 kg/ha.";
        if (r.K < 0 || r.K > 2000) errors["k"] = "Potassium must be between 0 and 2000 kg/ha.";
        if (r.Ph < 3m || r.Ph > 11m) errors["ph"] = "Soil pH must be between 3 and 11.";
        return errors;
    }

    public static FertilizerRecommendationDto Build(Inputs input)
    {
        var norm = input.Norm;
        var r = input.Request;
        var perPlant = norm.Basis == FertilizerDoseBasis.PerPlant;
        var hectares = Hectares(r.AreaAcres);

        int? plants = null;
        var plantsFromSpacing = false;
        if (perPlant)
        {
            if (r.PlantCount is > 0) plants = r.PlantCount;
            else
            {
                plants = PlantsFor(r.AreaAcres, norm.PlantsPerHectare ?? 0);
                plantsFromSpacing = true;
            }
        }

        // Rating and factor per nutrient.
        var nStatus = Rate(r.N, input.N);
        var pStatus = Rate(r.P, input.P);
        var kStatus = Rate(r.K, input.K);
        var nFactor = input.Factor("N", nStatus);
        var pFactor = input.Factor("P", pStatus);
        var kFactor = input.Factor("K", kStatus);

        // Unit -> kg for the whole area: kg/ha x ha, or g/plant x plants / 1000.
        var toAreaKg = perPlant ? plants!.Value / 1000m : hectares;
        // Per-hectare equivalent (for "kg per acre"): per-plant doses at the source's density, or the farmer's own count.
        var perHaFactor = perPlant ? (plantsFromSpacing ? (norm.PlantsPerHectare ?? 0) / 1000m : plants!.Value / 1000m / hectares) : 1m;

        FertilizerNutrientResultDto Nutrient(string code, string label, string doseAs, decimal soil, LabResultStatus status, Threshold t, decimal factor, decimal normDose)
        {
            var adjusted = Math.Round(normDose * factor, 2, MidpointRounding.AwayFromZero);
            return new FertilizerNutrientResultDto
            {
                Code = code,
                Label = label,
                DoseAs = doseAs,
                SoilValue = soil,
                Level = Level(status),
                LevelLabel = LevelLabel(status, t),
                LabStatus = status.ToString(),
                Factor = factor,
                AdjustmentText = AdjustmentText(factor),
                NormDose = normDose,
                AdjustedDose = adjusted,
                NeedKgPerAcre = Math.Round(normDose * factor * perHaFactor / AcresPerHectare, 1, MidpointRounding.AwayFromZero),
                TotalKg = Math.Round(normDose * factor * toAreaKg, 1, MidpointRounding.AwayFromZero)
            };
        }

        var result = new FertilizerRecommendationDto
        {
            NormId = norm.Id,
            Crop = norm.Crop,
            Variant = norm.Variant,
            SoilType = r.SoilType,
            WaterRegime = norm.WaterRegime,
            Basis = norm.Basis,
            AreaAcres = r.AreaAcres,
            AreaHectares = Math.Round(hectares, 2, MidpointRounding.AwayFromZero),
            PlantCount = plants,
            PlantCountFromSpacing = plantsFromSpacing,
            Spacing = norm.Spacing,
            DurationDays = norm.DurationDays,
            FromSoilReport = r.SoilReportId is not null,
            SoilReportId = r.SoilReportId,
            NormNotes = norm.Notes,
            Source = norm.Source,
            SourceUrl = norm.SourceUrl,
            AdjustmentRule = AdjustmentSource,
            Disclaimer = Disclaimer
        };
        result.Nutrients.Add(Nutrient("N", "Nitrogen (N)", "N", r.N, nStatus, input.N, nFactor, norm.N));
        result.Nutrients.Add(Nutrient("P", "Phosphorus (P)", "P2O5", r.P, pStatus, input.P, pFactor, norm.P2O5));
        result.Nutrients.Add(Nutrient("K", "Potassium (K)", "K2O", r.K, kStatus, input.K, kFactor, norm.K2O));

        var band = PhBandFor(r.Ph, input.Ph);
        result.Ph = new FertilizerPhResultDto
        {
            Value = r.Ph,
            Band = band.Band,
            Level = band.Level,
            LevelLabel = band.LevelLabel,
            Title = band.Title,
            Advice = band.Advice,
            Source = SoilConstraintsSource,
            SourceUrl = SoilConstraintsUrl
        };

        // The schedule, split by split.
        var dapTotals = new Dictionary<string, decimal>();
        var sspTotals = new Dictionary<string, decimal>();
        foreach (var s in input.Splits.OrderBy(s => s.Seq))
        {
            var n = s.N * nFactor * toAreaKg;
            var p = s.P2O5 * pFactor * toAreaKg;
            var k = s.K2O * kFactor * toAreaKg;
            var dap = DapRoute(n, p, k);
            var ssp = SspRoute(n, p, k);
            Add(dapTotals, dap);
            Add(sspTotals, ssp);
            result.Stages.Add(new FertilizerStageDto
            {
                Seq = s.Seq,
                Stage = s.Stage,
                When = When(s),
                DayFrom = s.DayFrom,
                DayTo = s.DayTo,
                NKg = Math.Round(n, 1, MidpointRounding.AwayFromZero),
                PKg = Math.Round(p, 1, MidpointRounding.AwayFromZero),
                KKg = Math.Round(k, 1, MidpointRounding.AwayFromZero),
                Products = Lines(dap, plants),
                SspProducts = Lines(ssp, plants)
            });
        }
        result.Totals = Lines(dapTotals, plants);
        result.SspTotals = Lines(sspTotals, plants);

        // Tips of this crop (or every crop) and this soil (or every soil); irrigation tips only for watered crops.
        foreach (var tip in TipsFor(input.Tips, norm, r.SoilType))
        {
            var dto = new FertilizerTipDto { Category = tip.Category, Text = tip.Text, Source = tip.Source, SourceUrl = tip.SourceUrl };
            switch (tip.Category)
            {
                case "Water": result.WaterTips.Add(dto); break;
                case "Soil": result.SoilTips.Add(dto); break;
                default: result.NutrientTips.Add(dto); break;
            }
        }

        result.Summary = Summary(result);
        return result;
    }

    /// <summary>Tips for the crop (or every crop) and the soil (or every soil), water regime matching; crop-specific first.</summary>
    public static List<CropSoilTip> TipsFor(IEnumerable<CropSoilTip> tips, FertilizerCropNorm norm, string soilType) =>
        tips.Where(t => t.IsActive
                        && (t.Crop is null || string.Equals(t.Crop, norm.Crop, StringComparison.OrdinalIgnoreCase))
                        && (t.SoilType is null || string.Equals(t.SoilType, soilType, StringComparison.Ordinal))
                        && (t.WaterRegime == FertilizerWaterRegime.Any || t.WaterRegime == norm.WaterRegime))
            .OrderBy(t => t.Crop is null ? 1 : 0)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .ToList();

    /// <summary>"At sowing", "40-45 days after sowing", "30 days after planting", "June - July".</summary>
    public static string When(FertilizerNormSplit s)
    {
        if (s.DayBasis == FertilizerDayBasis.Calendar) return s.Stage;
        var after = s.DayBasis == FertilizerDayBasis.Planting ? "planting" : "sowing";
        if (s.DayFrom is null) return s.DayBasis == FertilizerDayBasis.Planting ? "At planting" : "At sowing";
        var days = s.DayTo is { } to && to != s.DayFrom ? $"{s.DayFrom}-{to}" : $"{s.DayFrom}";
        return $"{days} days after {after}";
    }

    /// <summary>One line for history, the chatbot and the yield forecast.</summary>
    public static string Summary(FertilizerRecommendationDto r)
    {
        var area = r.AreaAcres.ToString("0.##", CultureInfo.InvariantCulture);
        var parts = string.Join(", ", r.Nutrients.Select(n => $"{n.Code} {n.Level}"));
        var need = string.Join(" : ", r.Nutrients.Select(n => n.NeedKgPerAcre.ToString("0.#", CultureInfo.InvariantCulture)));
        return $"{r.Crop} ({r.Variant}) on {r.SoilType}, {area} acre{(r.AreaAcres == 1m ? "" : "s")}: {parts}; pH {r.Ph.Value.ToString("0.0", CultureInfo.InvariantCulture)} {r.Ph.LevelLabel}. Need N : P2O5 : K2O = {need} kg/acre.";
    }

    private static void Add(Dictionary<string, decimal> into, Dictionary<string, decimal> add)
    {
        foreach (var (k, v) in add) into[k] = (into.TryGetValue(k, out var cur) ? cur : 0m) + v;
    }

    private static string Text(string? v, string fallback) => string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
}
