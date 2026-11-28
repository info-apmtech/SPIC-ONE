using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Farmer portal Task 7: the harvest yield forecast as pure functions (pinned by the regression harness). No AI call:
/// the page is titled "AI Harvest Yield Prediction" after the owner's sketch, but the estimate is this documented
/// rule model, and the screen says it is a model estimate from crop norms and the farmer's inputs.
///
///   expected t/ha = norm x f_ndvi x f_rain x f_npk x f_irrigation
///   total t       = t/ha x acres x 0.4047
///   range         = total x (1 -/+ w), w = 12 % (NDVI value) or 20 % (NDVI picked by look), +4 % rain outside the
///                   crop's band, +3 % NPK under 70 %, at most 25 %
///
/// The norm (<see cref="CropYieldNorm"/>, seeded with sources) is the yield of good management in Tamil Nadu for the
/// crop and irrigation class, inside its rainfall band. Each factor is 1.0 at the best input, so the norm is what a
/// farmer gets with a full canopy, rain inside the band and the full recommended fertilizer.
/// </summary>
public static class YieldEstimator
{
    public const string Irrigated = "Irrigated";
    public const string Rainfed = "Rainfed";

    /// <summary>1 acre = 0.4047 ha (the brief's factor; 1 / 2.47105 = 0.40469).</summary>
    public const double HectaresPerAcre = 0.4047;
    public const double QuintalsPerTonne = 10;
    public const int HistorySize = 20;

    // ---- the owner's sketch: slider ranges and defaults
    public const double NdviMin = 0.35, NdviMax = 0.92, NdviStep = 0.01, NdviDefault = 0.72;
    public const int RainMin = 400, RainMax = 1400, RainStep = 10, RainDefault = 850;
    public const int NpkMin = 50, NpkMax = 120, NpkStep = 5, NpkDefault = 90;
    public const decimal MinAcres = 0.1m, MaxAcres = 1000m, DefaultAcres = 3.0m;
    public const string DefaultCrop = "Paddy";

    /// <summary>NDVI from which the canopy counts as full (f_ndvi = 1.0) and above which lodging risk starts.</summary>
    public const double NdviFullFrom = 0.72, NdviLodgingAbove = 0.88;

    public const string MethodCanalBorewell = "canal-borewell";
    public const string MethodRainfed = "rainfed";
    public const string MethodDrip = "drip";
    public const string MethodSprinkler = "sprinkler";

    public const string SourceValue = "value";
    public const string SourceLook = "look";

    public sealed record Method(string Code, string Label, string IrrigationClass, string Icon);

    /// <summary>The sketch's irrigation choices, in its order. Canal / borewell, drip and sprinkler use the crop's
    /// Irrigated norm, rainfed its Rainfed norm.</summary>
    public static readonly IReadOnlyList<Method> Methods = new[]
    {
        new Method(MethodCanalBorewell, "Canal / Borewell Irrigated", Irrigated, "bi-water"),
        new Method(MethodRainfed, "Rainfed", Rainfed, "bi-cloud-rain"),
        new Method(MethodDrip, "Drip", Irrigated, "bi-droplet"),
        new Method(MethodSprinkler, "Sprinkler", Irrigated, "bi-cloud-drizzle")
    };

    public sealed record Look(string Key, string Label, string Hint, double Ndvi);

    /// <summary>"Not sure? Pick how your crop looks." Each look stands for a typical NDVI of that canopy (sparse
    /// cover reads 0.4-0.5, a closing canopy about 0.6, a full green canopy 0.75-0.85). A look widens the range.</summary>
    public static readonly IReadOnlyList<Look> Looks = new[]
    {
        new Look("sparse", "Sparse", "Gaps, soil shows between plants", 0.45),
        new Look("medium", "Medium", "Mostly covered, a few thin patches", 0.62),
        new Look("dense", "Dense", "Thick and dark green, ground hidden", 0.78)
    };

    public static Method? FindMethod(string? code) =>
        Methods.FirstOrDefault(m => string.Equals(m.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Profile irrigation to the sketch's method: canal, borewell and mixed are "Canal / Borewell
    /// Irrigated"; no answer is treated as canal / borewell (most Tamil Nadu farms are irrigated).</summary>
    public static string MethodForProfile(FarmerIrrigationType? type) => type switch
    {
        FarmerIrrigationType.Rainfed => MethodRainfed,
        FarmerIrrigationType.Drip => MethodDrip,
        FarmerIrrigationType.Sprinkler => MethodSprinkler,
        _ => MethodCanalBorewell
    };

    /// <summary>The first profile crop that has a yield norm (names compared without case), else null.</summary>
    public static string? ProfileCrop(IEnumerable<string>? profileCrops, IEnumerable<string> cropsWithNorm)
    {
        var known = cropsWithNorm.ToList();
        foreach (var c in profileCrops ?? Array.Empty<string>())
        {
            var match = known.FirstOrDefault(k => string.Equals(k, c?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }
        return null;
    }

    /// <summary>The norm for a crop and method, or null when the crop has none for that irrigation class.</summary>
    public static CropYieldNorm? PickNorm(IEnumerable<CropYieldNorm> norms, string? crop, string? method)
    {
        var m = FindMethod(method);
        if (m is null || string.IsNullOrWhiteSpace(crop)) return null;
        return norms.FirstOrDefault(n => n.IsActive
            && string.Equals(n.Crop, crop.Trim(), StringComparison.OrdinalIgnoreCase)
            && n.IrrigationClass == m.IrrigationClass);
    }

    // ------------------------------------------------------------------ the factor curves

    /// <summary>
    /// f_ndvi, canopy vigour, piecewise linear through (0.35, 0.45), (0.60, 0.85), (0.72, 1.00), (0.88, 1.00),
    /// (0.92, 0.96). A sparse canopy (0.35) intercepts too little light for more than about half the yield; the
    /// curve rises steeply to a closing canopy (0.60) and is flat over a full canopy (0.72-0.88). Above 0.88 a
    /// very lush crop risks lodging (falling over) and disease, so it drops slightly. Values outside the slider
    /// are clamped.
    /// </summary>
    public static double NdviFactor(double ndvi) => Interpolate(Math.Clamp(ndvi, NdviMin, NdviMax), NdviCurve);

    private static readonly (double X, double Y)[] NdviCurve =
    {
        (0.35, 0.45), (0.60, 0.85), (0.72, 1.00), (0.88, 1.00), (0.92, 0.96)
    };

    /// <summary>Share of a rain deficit an irrigated crop still feels (the brief: deficits weigh 30 %).</summary>
    public const double IrrigatedDeficitWeight = 0.30;

    /// <summary>Yield lost per relative excess of rain above the band (waterlogging, leached nitrogen, pests and
    /// disease, harvest losses).</summary>
    public const double ExcessSlope = 0.35;
    public const double RainDeficitFloor = 0.30, RainExcessFloor = 0.60;

    /// <summary>
    /// f_rain, season rainfall against the crop's band [RainLowMm, RainHighMm]:
    ///  - inside the band: 1.0;
    ///  - below: the FAO water-production rule 1 - Ya/Ym = Ky (1 - ETa/ETm) (Irrigation and Drainage Paper 33), with the
    ///    relative deficit d = (low - rain) / low standing in for 1 - ETa/ETm: f = 1 - Ky d for a rainfed crop (at least
    ///    0.30); an irrigated crop is buffered, only 30 % of that loss counts (irrigation makes up the rest);
    ///  - above: f = 1 - 0.35 (rain - high) / high, at least 0.60, for both classes (too much rain hurts irrigated
    ///    fields too).
    /// </summary>
    public static double RainFactor(int rainfallMm, CropYieldNorm norm)
    {
        double rain = rainfallMm, low = norm.RainLowMm, high = norm.RainHighMm;
        if (rain >= low && rain <= high) return 1.0;
        if (rain < low)
        {
            var deficit = (low - rain) / low;
            var loss = (double)norm.YieldResponseFactor * deficit;
            if (norm.IrrigationClass == Irrigated) loss *= IrrigatedDeficitWeight;
            return Math.Max(RainDeficitFloor, 1 - loss);
        }
        var excess = (rain - high) / high;
        return Math.Max(RainExcessFloor, 1 - ExcessSlope * excess);
    }

    /// <summary>
    /// f_npk, share of the recommended N-P-K actually applied, with diminishing returns:
    /// f = 1 - 0.20 ((100 - p) / 50)^1.5 for p up to 100 (50 % -> 0.80, 75 % -> 0.93, 90 % -> 0.98, 100 % -> 1.0),
    /// and 1.0 above 100 % (more fertilizer gives no more yield; the result warns instead).
    /// </summary>
    public static double NpkFactor(int npkPercent)
    {
        var p = Math.Clamp(npkPercent, NpkMin, NpkMax);
        if (p >= 100) return 1.0;
        return 1 - 0.20 * Math.Pow((100.0 - p) / 50.0, 1.5);
    }

    /// <summary>Drip adds a little over canal / borewell (even wetting, fertigation), sprinkler less so. Paddy norms
    /// are for flooded fields: paddy under drip or sprinkler is aerobic rice, which yields less.</summary>
    public const double DripFactor = 1.05, SprinklerFactor = 1.02, AerobicPaddyFactor = 0.90;

    /// <summary>
    /// f_irrigation, the method within its class: canal / borewell 1.00 (the Irrigated norm), rainfed 1.00 (the
    /// Rainfed norm already carries it), drip 1.05, sprinkler 1.02, paddy under drip or sprinkler 0.90 (not flooded).
    /// Model assumptions, to be confirmed by an agronomist (docs/farmer-portal.md, Task 7).
    /// </summary>
    public static double IrrigationFactor(string method, string crop)
    {
        var paddy = string.Equals(crop, "Paddy", StringComparison.OrdinalIgnoreCase);
        return method switch
        {
            MethodDrip => paddy ? AerobicPaddyFactor : DripFactor,
            MethodSprinkler => paddy ? AerobicPaddyFactor : SprinklerFactor,
            _ => 1.0
        };
    }

    // ------------------------------------------------------------------ confidence

    public const int RangeValue = 12, RangeLook = 20, RangeRainOutside = 4, RangeLowNpk = 3, RangeMax = 25;

    /// <summary>Half width of the range in percent, and why it is that wide.</summary>
    public static (int Percent, List<string> Reasons) RangeWidth(YieldPredictRequest r, CropYieldNorm norm)
    {
        var reasons = new List<string>();
        var w = RangeValue;
        if (r.NdviSource == SourceLook)
        {
            w = RangeLook;
            reasons.Add("Crop vigour was picked by look, not measured.");
        }
        else reasons.Add("Crop vigour was entered as an NDVI value.");
        if (r.RainfallMm < norm.RainLowMm || r.RainfallMm > norm.RainHighMm)
        {
            w += RangeRainOutside;
            reasons.Add("Rainfall is outside the usual band for this crop, so the season is harder to call.");
        }
        if (r.NpkPercent < 70)
        {
            w += RangeLowNpk;
            reasons.Add("A low fertilizer dose makes the crop's response less certain.");
        }
        return (Math.Min(RangeMax, w), reasons);
    }

    /// <summary>Ring value: 100 - 2.5 x range percent (12 % -> 70, 20 % -> 50, 25 % -> 38).</summary>
    public static int ConfidenceScore(int rangePercent) => (int)Math.Round(100 - 2.5 * rangePercent, MidpointRounding.AwayFromZero);

    /// <summary>The same bands as FpBigNumber's ring: 70+ High, 40-69 Medium, under 40 Low.</summary>
    public static string ConfidenceLabel(int score) => score >= 70 ? "High confidence" : score >= 40 ? "Medium confidence" : "Low confidence";

    // ------------------------------------------------------------------ validation

    public static Dictionary<string, string> Validate(YieldPredictRequest? r, CropYieldNorm? norm, bool cropKnown)
    {
        var errors = new Dictionary<string, string>();
        if (r is null)
        {
            errors["crop"] = "Please fill in the form.";
            return errors;
        }
        var method = FindMethod(r.Method);
        if (string.IsNullOrWhiteSpace(r.Crop) || !cropKnown) errors["crop"] = "Please choose your crop.";
        if (method is null) errors["method"] = "Please choose how you water the crop.";
        else if (cropKnown && norm is null)
            errors["method"] = method.IrrigationClass == Rainfed
                ? $"{r.Crop.Trim()} is grown with irrigation here, so there is no rainfed forecast. Choose how you water it."
                : $"There is no irrigated forecast for {r.Crop.Trim()}. Choose Rainfed.";
        if (r.AreaAcres < MinAcres || r.AreaAcres > MaxAcres) errors["areaAcres"] = "Enter your area in acres (0.1 to 1000).";
        if (double.IsNaN(r.Ndvi) || r.Ndvi < NdviMin - 1e-9 || r.Ndvi > NdviMax + 1e-9)
            errors["ndvi"] = $"Crop vigour (NDVI) must be between {NdviMin:0.00} and {NdviMax:0.00}.";
        if (r.NdviSource != SourceValue && r.NdviSource != SourceLook) errors["ndviSource"] = "Please set the crop vigour.";
        if (r.RainfallMm < RainMin || r.RainfallMm > RainMax) errors["rainfallMm"] = $"Season rainfall must be between {RainMin} and {RainMax} mm.";
        if (r.NpkPercent < NpkMin || r.NpkPercent > NpkMax) errors["npkPercent"] = $"Fertilizer applied must be between {NpkMin} % and {NpkMax} %.";
        return errors;
    }

    // ------------------------------------------------------------------ the estimate

    /// <summary>The forecast for a validated request (<see cref="Validate"/> returned no errors).</summary>
    public static YieldPredictionDto Estimate(CropYieldNorm norm, YieldPredictRequest r)
    {
        var method = FindMethod(r.Method) ?? Methods[0];
        var ndvi = Math.Round(Math.Clamp(r.Ndvi, NdviMin, NdviMax), 2);
        var fNdvi = NdviFactor(ndvi);
        var fRain = RainFactor(r.RainfallMm, norm);
        var fNpk = NpkFactor(r.NpkPercent);
        var fIrr = IrrigationFactor(method.Code, norm.Crop);

        var normT = (double)norm.TypicalTPerHa;
        var perHa = normT * fNdvi * fRain * fNpk * fIrr;
        var hectares = (double)r.AreaAcres * HectaresPerAcre;
        var total = perHa * hectares;
        var (w, reasons) = RangeWidth(r, norm);
        var score = ConfidenceScore(w);

        var dto = new YieldPredictionDto
        {
            NormId = norm.Id,
            Crop = norm.Crop,
            Product = norm.Product,
            Method = method.Code,
            MethodLabel = method.Label,
            IrrigationClass = norm.IrrigationClass,
            AreaAcres = r.AreaAcres,
            AreaHectares = Math.Round(hectares, 3),
            Ndvi = ndvi,
            NdviSource = r.NdviSource,
            RainfallMm = r.RainfallMm,
            NpkPercent = r.NpkPercent,
            NormTPerHa = normT,
            NormLowTPerHa = (double)norm.LowTPerHa,
            NormHighTPerHa = (double)norm.HighTPerHa,
            StateAverageTPerHa = norm.StateAverageTPerHa is { } avg ? (double)avg : null,
            RainLowMm = norm.RainLowMm,
            RainHighMm = norm.RainHighMm,
            PerHectareT = Round2(perHa),
            PerAcreT = Round2(perHa * HectaresPerAcre),
            ExpectedT = Round2(total),
            LowT = Round2(total * (1 - w / 100.0)),
            HighT = Round2(total * (1 + w / 100.0)),
            ExpectedQuintals = Math.Round(total * QuintalsPerTonne, 1, MidpointRounding.AwayFromZero),
            RangePercent = w,
            ConfidenceScore = score,
            ConfidenceLabel = ConfidenceLabel(score),
            ConfidenceReasons = reasons,
            Source = norm.Source,
            SourceUrl = norm.SourceUrl
        };

        dto.Factors = new List<YieldFactorDto>
        {
            Factor("ndvi", "Crop vigour (NDVI)", fNdvi, total,
                ndvi < NdviFullFrom ? "low" : ndvi > NdviLodgingAbove ? "high" : "ok", NdviNote(ndvi)),
            Factor("rain", "Season rainfall", fRain, total,
                r.RainfallMm < norm.RainLowMm ? "low" : r.RainfallMm > norm.RainHighMm ? "high" : "ok", RainNote(r.RainfallMm, norm)),
            Factor("npk", "Fertilizer applied", fNpk, total,
                r.NpkPercent < 100 ? "low" : r.NpkPercent > 100 ? "high" : "ok", NpkNote(r.NpkPercent)),
            Factor("irrigation", "Irrigation", fIrr, total,
                fIrr < 1 ? "low" : fIrr > 1 ? "high" : "ok", IrrigationNote(method, norm.Crop, fIrr))
        };
        var biggest = dto.Factors.Where(f => f.LossT > 0).OrderByDescending(f => f.LossT).FirstOrDefault();
        dto.BiggestLoss = biggest?.Code;
        dto.Tips = Tips(dto, norm, method, fNdvi, fRain, fNpk, total);
        if (r.NpkPercent > 100)
            dto.Warnings.Add("More than the recommended fertilizer adds cost, not yield, and can harm the soil and water. Follow your fertilizer advice.");
        if (ndvi > NdviLodgingAbove)
            dto.Warnings.Add("A very dense, lush crop can lodge (fall over) and catch disease. Avoid extra nitrogen now and keep drains open.");
        return dto;
    }

    private static YieldFactorDto Factor(string code, string label, double f, double total, string status, string note) => new()
    {
        Code = code,
        Label = label,
        Value = Math.Round(f, 3),
        LossT = f > 0 ? Round2(total / f - total) : 0,
        Status = status,
        Note = note
    };

    private static string NdviNote(double ndvi) => ndvi switch
    {
        < 0.5 => $"NDVI {Num(ndvi, "0.00")}: a sparse crop with gaps",
        < NdviFullFrom => $"NDVI {Num(ndvi, "0.00")}: thinner than a full, healthy crop (0.72 or more)",
        > NdviLodgingAbove => $"NDVI {Num(ndvi, "0.00")}: very dense, some risk of lodging",
        _ => $"NDVI {Num(ndvi, "0.00")}: a full, healthy crop"
    };

    private static string RainNote(int rain, CropYieldNorm n)
    {
        var band = $"{n.RainLowMm}-{n.RainHighMm} mm";
        if (rain < n.RainLowMm)
            return n.IrrigationClass == Irrigated
                ? $"{rain} mm is below the {band} band; irrigation makes up most of it"
                : $"{rain} mm is below the {band} this crop needs";
        if (rain > n.RainHighMm) return $"{rain} mm is above the {band} band: waterlogging and disease risk";
        return $"{rain} mm is inside the {band} band for this crop";
    }

    private static string NpkNote(int p) => p switch
    {
        < 100 => $"{p} % of the recommended fertilizer",
        100 => "The full recommended fertilizer",
        _ => $"{p} %: more than recommended gives no extra yield"
    };

    private static string IrrigationNote(Method m, string crop, double f)
    {
        if (m.Code == MethodRainfed) return "Rainfed: the rainfed yield of this crop";
        if (f < 1) return $"{m.Label} on {crop.ToLowerInvariant()}: fields are not flooded (aerobic), about {Pct(1 - f)} less";
        if (f > 1) return $"{m.Label}: about {Pct(f - 1)} more than canal / borewell";
        return "Canal / borewell: the usual irrigated yield";
    }

    private static List<YieldTipDto> Tips(YieldPredictionDto dto, CropYieldNorm norm, Method method, double fNdvi, double fRain, double fNpk, double total)
    {
        var tips = new List<YieldTipDto>();
        if (fNpk < 1)
        {
            var gain = total / fNpk - total;
            tips.Add(new YieldTipDto
            {
                Code = "npk",
                GainT = Round2(gain),
                Text = $"Applying the full recommended fertilizer (100 %) could add {Amount(gain)}.",
                Href = "/farmer/fertilizer",
                LinkText = "Get fertilizer advice"
            });
        }
        if (fNdvi < 1 && dto.Ndvi < NdviFullFrom)
        {
            var gain = total / fNdvi - total;
            tips.Add(new YieldTipDto
            {
                Code = "ndvi",
                GainT = Round2(gain),
                Text = $"A fuller, greener crop could add up to {Amount(gain)}. Fill gaps, weed on time and check the leaves for a nutrient shortage.",
                Href = "/farmer/leaf-check",
                LinkText = "Check a leaf"
            });
        }
        if (fRain < 1)
        {
            var gain = total / fRain - total;
            string text;
            if (dto.RainfallMm > norm.RainHighMm)
                text = $"Heavy rain can cost about {Amount(gain)}: keep field drains open and watch for pests and disease.";
            else if (norm.IrrigationClass == Rainfed)
                text = $"Low rain costs about {Amount(gain)}. One protective irrigation at flowering, mulching or a farm pond helps most.";
            else
                text = $"Rain is below what {norm.Crop.ToLowerInvariant()} needs: keep irrigations on time, mainly at flowering and grain or fruit filling ({Amount(gain)} at stake).";
            tips.Add(new YieldTipDto { Code = "rain", GainT = Round2(gain), Text = text });
        }
        if (method.Code == MethodCanalBorewell && !string.Equals(norm.Crop, "Paddy", StringComparison.OrdinalIgnoreCase))
        {
            var gain = total * (DripFactor - 1);
            tips.Add(new YieldTipDto
            {
                Code = "drip",
                GainT = Round2(gain),
                Text = $"Drip irrigation usually gives about {Pct(DripFactor - 1)} more ({Amount(gain)}) and saves water."
            });
        }
        var top = tips.Where(t => t.GainT >= 0.005).OrderByDescending(t => t.GainT).Take(3).ToList();
        if (top.All(t => t.Code == "drip"))
            top.Insert(0, new YieldTipDto
            {
                Code = "keep",
                Text = "Your inputs are close to the best for this crop. Keep weeding, pest checks and irrigation on time to hold this yield."
            });
        return top.Take(3).ToList();
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Piecewise-linear interpolation through points sorted by X (clamped at both ends).</summary>
    public static double Interpolate(double x, IReadOnlyList<(double X, double Y)> points)
    {
        if (x <= points[0].X) return points[0].Y;
        for (var i = 1; i < points.Count; i++)
        {
            if (x > points[i].X) continue;
            var (x0, y0) = points[i - 1];
            var (x1, y1) = points[i];
            return y0 + (y1 - y0) * (x - x0) / (x1 - x0);
        }
        return points[^1].Y;
    }

    /// <summary>"about 40 kg" under a tonne, else "about 1.4 t (14 quintals)".</summary>
    public static string Amount(double tonnes)
    {
        if (tonnes < 1)
        {
            var kg = Math.Max(10, Math.Round(tonnes * 1000 / 10, MidpointRounding.AwayFromZero) * 10);
            return $"about {Num(kg, "0")} kg";
        }
        var q = tonnes * QuintalsPerTonne;
        return $"about {Tonnes(tonnes)} t ({Num(q, q < 100 ? "0.#" : "0")} quintals)";
    }

    /// <summary>Tonnes as shown: 2 decimals under 10 t, 1 under 100 t, whole tonnes above.</summary>
    public static string Tonnes(double t) => Num(t, t < 10 ? "0.##" : t < 100 ? "0.#" : "0");

    private static string Pct(double fraction) => $"{Num(fraction * 100, "0")} %";

    private static string Num(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture);

    private static double Round2(double v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
