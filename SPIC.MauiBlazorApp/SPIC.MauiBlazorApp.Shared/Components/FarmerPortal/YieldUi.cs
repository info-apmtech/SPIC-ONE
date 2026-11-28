using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.FarmerPortal;

namespace SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

/// <summary>Pure helpers of the harvest yield forecast screens (farmer portal Task 7), checked by the harness.</summary>
public static class YieldUi
{
    public const string Disclaimer =
        "Estimate from crop norms and your inputs, not a guarantee. This is a model estimate, not a field visit or a satellite reading: weather, pests and disease can change the harvest. Talk to your MDO before you commit a sale.";

    /// <summary>Fix round 1: said under "How we worked this out", so the rain band and curves are not read as sourced.</summary>
    public const string AssumptionsLine =
        "The rain band, the factor curves and the irrigation factors are our model's assumptions, built on the sources below; they are not published figures or measured on your field.";

    /// <summary>Tonnes as shown (2 decimals under 10 t, 1 under 100 t, whole above).</summary>
    public static string Tonnes(double t) => YieldEstimator.Tonnes(t);

    public static string Tonnes(decimal t) => YieldEstimator.Tonnes((double)t);

    /// <summary>Quintals (1 t = 10 q): one decimal under 100, whole above.</summary>
    public static string Quintals(double t)
    {
        var q = t * YieldEstimator.QuintalsPerTonne;
        return q.ToString(q < 100 ? "0.#" : "#,0", CultureInfo.InvariantCulture);
    }

    public static string Acres(decimal acres) =>
        $"{acres.ToString("0.##", CultureInfo.InvariantCulture)} {(acres == 1m ? "acre" : "acres")}";

    /// <summary>"= 1.21 ha" under the area box.</summary>
    public static string Hectares(decimal? acres) => acres is { } a && a > 0
        ? $"= {((double)a * YieldEstimator.HectaresPerAcre).ToString("0.##", CultureInfo.InvariantCulture)} ha"
        : "";

    /// <summary>A crop chip icon for crops without an emoji.</summary>
    public static string CropIcon(string crop) => crop switch
    {
        "Cotton" => "bi-cloud",
        "Sugarcane" => "bi-bar-chart-steps",
        _ => "bi-flower1"
    };

    public static string MethodLabel(string? code) => YieldEstimator.FindMethod(code)?.Label ?? code ?? "";

    /// <summary>The norm of the chosen crop for the chosen method's irrigation class, or null.</summary>
    public static YieldNormOptionDto? NormFor(YieldCropOptionDto? crop, string? method)
    {
        var cls = YieldEstimator.FindMethod(method)?.IrrigationClass;
        return crop?.Norms.FirstOrDefault(n => n.IrrigationClass == cls);
    }

    /// <summary>True when the crop has a norm for the method's class (rainfed sugarcane / banana are not offered).</summary>
    public static bool Offers(YieldCropOptionDto? crop, string method) => crop is null || NormFor(crop, method) is not null;

    /// <summary>"Typical with good management: 5.8 t/ha" for the crop card.</summary>
    public static string TypicalLine(YieldCropOptionDto crop, YieldNormOptionDto norm) =>
        $"Good management in Tamil Nadu: about {Tonnes(norm.TypicalTPerHa)} t/ha of {crop.Product} ({norm.IrrigationClass.ToLowerInvariant()}).";

    public static string RainHint(string crop, YieldNormOptionDto? norm) => norm is null
        ? "Rain expected from sowing to harvest."
        : $"Rain expected from sowing to harvest. Best for {crop.ToLowerInvariant()}: {norm.RainLowMm}-{norm.RainHighMm} mm.";

    /// <summary>The NPK slider hint, with the farmer's latest fertilizer advice when there is one.</summary>
    public static string NpkHint(string? lastCrop, DateTime? lastAtUtc)
    {
        const string basic = "How much of the recommended fertilizer you applied or will apply. 100 % = the full dose.";
        if (string.IsNullOrWhiteSpace(lastCrop) || lastAtUtc is not { } at) return basic;
        var day = FpUi.ToIst(DateTime.SpecifyKind(at, DateTimeKind.Utc)).ToString("d MMM", CultureInfo.InvariantCulture);
        return $"{basic} Your last fertilizer advice: {lastCrop}, {day}. Applied all of it? Set 100 %.";
    }

    /// <summary>The look key whose NDVI equals the value (when the farmer picked a look), else null.</summary>
    public static string? LookKey(IEnumerable<YieldLookOptionDto> looks, double? ndvi) =>
        ndvi is { } v ? looks.FirstOrDefault(l => Math.Abs(l.Ndvi - v) < 0.0001)?.Key : null;

    /// <summary>
    /// Hides the slider's min / max scale label when a zone mark sits within 15 % of it (NDVI 0.88 next to 0.92,
    /// paddy's 1,300 mm next to 1,400), so the labels never print over each other. The value bubble and box stay.
    /// </summary>
    public static string? ScaleCss(double min, double max, double lowBelow, double highAbove)
    {
        var span = max - min;
        if (span <= 0) return null;
        var css = new List<string>();
        if (lowBelow > min && (lowBelow - min) / span < 0.15) css.Add("fpy-zone--nomin");
        if (highAbove < max && (max - highAbove) / span < 0.15) css.Add("fpy-zone--nomax");
        return css.Count == 0 ? null : string.Join(" ", css);
    }

    /// <summary>Factor as a whole percent (0.982 -> 98).</summary>
    public static int Percent(double factor) => (int)Math.Round(factor * 100, MidpointRounding.AwayFromZero);

    /// <summary>Bar width 0-100 % (a factor above 1 fills the bar).</summary>
    public static string BarWidth(double factor) =>
        Math.Clamp(factor * 100, 2, 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    /// <summary>Bar colour: good (no loss), mild (up to 10 % lost), poor (more), plus (adds yield).</summary>
    public static string FactorCss(double factor) => factor > 1.0005 ? "plus" : factor >= 0.9995 ? "good" : factor >= 0.9 ? "mild" : "poor";

    /// <summary>"costs about 120 kg", "adds about 0.35 t (3.5 quintals)" or "no loss".</summary>
    public static string LossText(double lossT) => lossT switch
    {
        >= 0.005 => "costs " + YieldEstimator.Amount(lossT),
        <= -0.005 => "adds " + YieldEstimator.Amount(-lossT),
        _ => "no loss"
    };

    public static string TipIcon(string code) => code switch
    {
        "npk" => "bi-flower2",
        "ndvi" => "bi-tree",
        "rain" => "bi-cloud-rain-heavy",
        "drip" => "bi-droplet-half",
        "canopy" => "bi-binoculars",
        "plan" => "bi-truck",
        "soil" => "bi-moisture",
        _ => "bi-hand-thumbs-up"
    };

    /// <summary>"Paddy · 3 acres · Canal / Borewell Irrigated".</summary>
    public static string Subtitle(YieldPredictionDto r) => $"{r.Crop} · {Acres(r.AreaAcres)} · {r.MethodLabel}";

    /// <summary>"About 69.2 quintals of paddy grain · 5.7 t/ha (2.31 t per acre)".</summary>
    public static string Caption(YieldPredictionDto r) =>
        $"About {Quintals(r.ExpectedT)} quintals of {r.Product} · {Tonnes(r.PerHectareT)} t/ha ({Tonnes(r.PerAcreT)} t per acre)";

    /// <summary>The worked-out line: "5.8 t/ha x 1.00 vigour x 1.00 rain x 0.98 fertilizer x 1.00 irrigation = 5.7 t/ha".</summary>
    public static string Formula(YieldPredictionDto r)
    {
        string F(string code) => (r.Factors.FirstOrDefault(f => f.Code == code)?.Value ?? 1).ToString("0.00", CultureInfo.InvariantCulture);
        return $"{Tonnes(r.NormTPerHa)} t/ha x {F("ndvi")} vigour x {F("rain")} rain x {F("npk")} fertilizer x {F("irrigation")} irrigation = {Tonnes(r.PerHectareT)} t/ha";
    }

    public static string AreaFormula(YieldPredictionDto r) =>
        $"{Tonnes(r.PerHectareT)} t/ha x {r.AreaHectares.ToString("0.###", CultureInfo.InvariantCulture)} ha ({Acres(r.AreaAcres)} x 0.4047) = {Tonnes(r.ExpectedT)} t";

    /// <summary>Hue of the confidence chip in the history list.</summary>
    public static FpHue ConfidenceHue(string label) => label switch
    {
        "High confidence" => FpHue.Green,
        "Medium confidence" => FpHue.Gold,
        _ => FpHue.Rose
    };

    /// <summary>History line date in IST: "28 Nov 2026, 3:05 pm".</summary>
    public static string When(DateTime utc) => FertilizerUi.When(utc);
}
