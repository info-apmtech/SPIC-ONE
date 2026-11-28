using System.Globalization;
using SPIC.Core.Entities;

namespace SPIC.Core.FarmerPortal;

/// <summary>The photo formats the leaf check accepts, read from the file's first bytes (never from the client's content type).</summary>
public enum LeafImageFormat
{
    Unknown = 0,
    Jpeg = 1,
    Png = 2,
    Webp = 3,
    /// <summary>iPhone HEIC / HEIF: recognised only to give a clear message; not accepted.</summary>
    Heic = 4
}

/// <summary>One likely nutrient of a diagnosis (code N, P, K, Mg, Zn, Fe, S, B, Ca, Mn; likelihood 0-1).</summary>
public sealed record LeafNutrientGuess(string Code, double Likelihood);

/// <summary>
/// A diagnosis, from Claude vision (<see cref="LeafVision.Parse"/>) or the colour-check rules
/// (<see cref="LeafCheckRules.RuleDiagnosis"/>), in the shape the brief asks of the model.
/// </summary>
public sealed class LeafDiagnosisResult
{
    /// <summary>healthy | nutrient_deficiency | disease | pest | unclear.</summary>
    public string Condition { get; set; } = "unclear";
    public List<LeafNutrientGuess> Nutrients { get; set; } = new();
    public List<string> Symptoms { get; set; } = new();
    /// <summary>older | younger | all | unknown.</summary>
    public string LeafAge { get; set; } = "unknown";
    public double Confidence { get; set; }
    public List<string> Advice { get; set; } = new();
    public bool NeedsSoilTest { get; set; }
    public bool NotALeaf { get; set; }
}

/// <summary>
/// Farmer portal Task 6: the leaf-photo check. Upload validation (size, magic bytes), the daily cap on the IST day, the
/// colour-check rule fallback, nutrient names, the correctives choice and the screen words. Pure; pinned by the harness.
/// </summary>
public static class LeafCheckRules
{
    /// <summary>Largest photo accepted (8 MB).</summary>
    public const long MaxPhotoBytes = 8L * 1024 * 1024;
    /// <summary>Checks per farmer per IST day (FarmerPortal:LeafCheckDailyLimit).</summary>
    public const int DefaultDailyLimit = 10;
    /// <summary>The stored photo (and the one sent to Claude) is at most this many pixels on its long side.</summary>
    public const int MaxStoredSide = 1024;
    /// <summary>JPEG quality of the stored photo.</summary>
    public const int JpegQuality = 80;
    /// <summary>The colour metrics are measured on a copy at most this many pixels on its long side.</summary>
    public const int MetricsSide = 256;
    /// <summary>Smallest photo side accepted.</summary>
    public const int MinSide = 64;
    /// <summary>Largest photo accepted, in megapixels (a decompression-bomb guard).</summary>
    public const long MaxPixels = 60_000_000;
    /// <summary>History entries listed.</summary>
    public const int HistoryLimit = 30;

    public const string BasicLabel = "Basic colour check";

    public const string Disclaimer = "AI suggestion from a photo. Confirm with a soil test or your MDO.";

    public const string BasicDisclaimer =
        "Basic colour check from the leaf colours only, not an expert diagnosis. Confirm with a soil test or your MDO.";

    /// <summary>The crops offered (the fertilizer tool's crops and a few more grown in Tamil Nadu), then "Other".</summary>
    public static readonly IReadOnlyList<string> Crops = new[]
    {
        "Paddy", "Maize", "Cotton", "Groundnut", "Black gram", "Green gram", "Soybean", "Wheat", "Sugarcane",
        "Banana", "Coconut", "Chilli", "Tomato", "Brinjal", "Onion", "Turmeric", "Other"
    };

    public static readonly IReadOnlyList<string> LeafAges = new[] { "older", "younger", "all", "unknown" };

    public static readonly IReadOnlyList<string> Conditions = new[] { "healthy", "nutrient_deficiency", "disease", "pest", "unclear" };

    public static readonly IReadOnlyList<string> NutrientCodes = new[] { "N", "P", "K", "Mg", "Zn", "Fe", "S", "B", "Ca", "Mn" };

    public static int Limit(string? configured) =>
        int.TryParse(configured?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : DefaultDailyLimit;

    public static string CapMessage(int limit) =>
        $"You have used today's {limit} leaf checks. You can check more leaves after 12 midnight, or show the plant to your SPIC team.";

    /// <summary>The crop as listed (case-insensitive), or null when it is not one of <see cref="Crops"/>.</summary>
    public static string? NormaliseCrop(string? crop) =>
        Crops.FirstOrDefault(c => string.Equals(c, crop?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>older / younger / all / unknown; anything else (or nothing) = unknown.</summary>
    public static string NormaliseLeafAge(string? age)
    {
        var v = (age ?? "").Trim().ToLowerInvariant();
        return LeafAges.Contains(v) ? v : "unknown";
    }

    /// <summary>The format from the file's first bytes: JPEG FF D8 FF, PNG 89 50 4E 47 0D 0A 1A 0A, WebP "RIFF" ....
    /// "WEBP", HEIC "ftyp" + heic / heix / mif1 / msf1 / hevc at byte 4.</summary>
    public static LeafImageFormat DetectFormat(ReadOnlySpan<byte> head)
    {
        if (head.Length >= 3 && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF) return LeafImageFormat.Jpeg;
        if (head.Length >= 8 && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47
            && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A) return LeafImageFormat.Png;
        if (head.Length >= 12 && head[0] == (byte)'R' && head[1] == (byte)'I' && head[2] == (byte)'F' && head[3] == (byte)'F'
            && head[8] == (byte)'W' && head[9] == (byte)'E' && head[10] == (byte)'B' && head[11] == (byte)'P') return LeafImageFormat.Webp;
        if (head.Length >= 12 && head[4] == (byte)'f' && head[5] == (byte)'t' && head[6] == (byte)'y' && head[7] == (byte)'p')
        {
            var brand = System.Text.Encoding.ASCII.GetString(head.Slice(8, 4));
            if (brand is "heic" or "heix" or "heim" or "heis" or "hevc" or "hevx" or "mif1" or "msf1") return LeafImageFormat.Heic;
        }
        return LeafImageFormat.Unknown;
    }

    /// <summary>The farmer-facing problem with an upload, or null when it may be decoded. The content type the client
    /// claims is not looked at: only the size and the first bytes.</summary>
    public static string? ValidateUpload(long length, ReadOnlySpan<byte> head)
    {
        if (length <= 0) return "Please take a photo of the leaf or choose one from your gallery.";
        if (length > MaxPhotoBytes) return "This photo is too large (over 8 MB). Please take a new photo or choose a smaller one.";
        return DetectFormat(head) switch
        {
            LeafImageFormat.Jpeg or LeafImageFormat.Png or LeafImageFormat.Webp => null,
            LeafImageFormat.Heic => "This photo is in HEIC format. Please choose a JPEG photo, or set your camera to \"Most compatible\".",
            _ => "This file is not a photo we can read. Please use a JPEG, PNG or WebP photo."
        };
    }

    /// <summary>Validates the form fields; field errors keyed "crop" / "photo".</summary>
    public static Dictionary<string, string> ValidateForm(string? crop, string? leafAge)
    {
        var errors = new Dictionary<string, string>();
        if (NormaliseCrop(crop) is null) errors["crop"] = "Please choose your crop.";
        if (!string.IsNullOrWhiteSpace(leafAge) && !LeafAges.Contains(leafAge.Trim().ToLowerInvariant()))
            errors["leafAge"] = "Please choose which leaves are affected.";
        return errors;
    }

    // ------------------------------------------------------------------ words

    public static string NutrientName(string code) => code switch
    {
        "N" => "Nitrogen",
        "P" => "Phosphorus",
        "K" => "Potassium",
        "Mg" => "Magnesium",
        "Zn" => "Zinc",
        "Fe" => "Iron",
        "S" => "Sulphur",
        "B" => "Boron",
        "Ca" => "Calcium",
        "Mn" => "Manganese",
        _ => code
    };

    /// <summary>The code as listed ("zn" -> "Zn"), or null for anything else.</summary>
    public static string? NormaliseNutrient(string? code) =>
        NutrientCodes.FirstOrDefault(c => string.Equals(c, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string ConditionLabel(string? condition, bool notALeaf) => notALeaf ? "This does not look like a leaf" : condition switch
    {
        "healthy" => "Your leaf looks healthy",
        "nutrient_deficiency" => "Looks like a nutrient shortage",
        "disease" => "Looks like a plant disease",
        "pest" => "Looks like pest damage",
        _ => "We are not sure from this photo"
    };

    /// <summary>Likely (0.6+), Possible (0.35+), Less likely.</summary>
    public static string LikelihoodLabel(double likelihood) =>
        likelihood >= 0.6 ? "Likely" : likelihood >= 0.35 ? "Possible" : "Less likely";

    public static string PatternLabel(string? pattern) => pattern switch
    {
        "uniform" => "Yellowing over the whole leaf",
        "interveinal" => "Yellow between green veins (interveinal)",
        "patchy" => "Yellow patches on part of the leaf",
        "margin" => "Brown, dry leaf edges",
        "spots" => "Brown spots on the leaf",
        _ => "Even green colour"
    };

    public static string LeafAgeLabel(string? age) => age switch
    {
        "older" => "Older (lower) leaves",
        "younger" => "Young (top) leaves",
        "all" => "All leaves",
        _ => "Not sure"
    };

    public static string LccLabel(int score) => score switch
    {
        <= 2 => $"{score} of 6: pale",
        <= 4 => $"{score} of 6: medium green",
        _ => $"{score} of 6: dark green"
    };

    /// <summary>The notice for a basic colour check (why there was no AI answer), or null for an AI answer.</summary>
    public static string? BasicNotice(string? reason) => reason switch
    {
        "no-key" => "SPIC AI photo checks are not switched on yet, so this is a basic colour check of your leaf.",
        "paused" => "SPIC AI photo checks are paused just now, so this is a basic colour check of your leaf.",
        "budget" => "Many farmers are using SPIC AI today, so this is a basic colour check. Full checks are back after 12 midnight.",
        "ai-failed" => "SPIC AI could not check this photo just now, so this is a basic colour check of your leaf.",
        "not-a-leaf" => "We could not find a leaf in this photo, so SPIC AI did not check it.",
        _ => null
    };

    /// <summary>The budget's decision as the stored reason.</summary>
    public static string BasicReasonFor(FarmerAiDecision decision) => decision switch
    {
        FarmerAiDecision.NoKey => "no-key",
        FarmerAiDecision.Disabled => "paused",
        FarmerAiDecision.GlobalLimit or FarmerAiDecision.KindLimit => "budget",
        _ => "ai-failed"
    };

    // ------------------------------------------------------------------ rule fallback ("basic colour check")

    /// <summary>
    /// The basic colour check: a diagnosis from the colour metrics and what the farmer said about which leaves are
    /// yellow. Confidence is kept at or below 0.5: a colour check cannot see spots, insects or the plant.
    /// </summary>
    public static LeafDiagnosisResult RuleDiagnosis(LeafMetrics m, string? crop, string? leafAgeHint)
    {
        var age = NormaliseLeafAge(leafAgeHint);
        var paddy = string.Equals(crop, "Paddy", StringComparison.OrdinalIgnoreCase);
        var r = new LeafDiagnosisResult { LeafAge = age };

        if (m.LeafShare < LeafColorAnalyzer.MinLeafShare || m.LeafPixels < 200)
        {
            r.NotALeaf = true;
            r.Condition = "unclear";
            r.Confidence = 0.5;
            r.Symptoms.Add($"Only {Pct(m.LeafShare)} of the photo has leaf colours");
            r.Advice.Add("Take a new photo of one leaf, close up, so it fills most of the picture.");
            r.Advice.Add("Use daylight and a plain background (a white paper or a plain cloth).");
            return r;
        }

        r.Symptoms.Add($"Leaf colour score {LccLabel(m.LccScore)}");
        if (m.YellowShare >= 0.08) r.Symptoms.Add($"About {Pct(m.YellowShare)} of the leaf is yellow");
        if (m.BrownShare >= 0.05) r.Symptoms.Add($"About {Pct(m.BrownShare)} of the leaf is brown or dry");
        if (m.Pattern != "none") r.Symptoms.Add(PatternLabel(m.Pattern));

        void Add(string code, double likelihood) => r.Nutrients.Add(new LeafNutrientGuess(code, likelihood));

        switch (m.Pattern)
        {
            case "none" when m.LccScore >= 3:
                r.Condition = "healthy";
                r.Confidence = 0.45;
                r.Advice.Add("Keep following your fertilizer plan.");
                r.Advice.Add("Check a few more leaves from different parts of the field.");
                break;
            case "none":
                r.Condition = "nutrient_deficiency";
                r.Confidence = 0.3;
                Add("N", 0.4);
                r.Symptoms.Add("The leaf is pale green");
                r.Advice.Add("A pale leaf can mean the crop needs nitrogen. Compare it with leaves from a healthy part of the field.");
                break;
            case "uniform":
                r.Condition = "nutrient_deficiency";
                r.Confidence = 0.45;
                if (age == "younger") { Add("S", 0.5); Add("N", 0.3); }
                else { Add("N", 0.6); Add("S", 0.25); }
                r.Advice.Add(age == "younger"
                    ? "Yellow young leaves all over often mean sulphur is short. Nitrogen shortage starts on the older leaves."
                    : "Yellowing of the whole leaf, starting on the older leaves, usually means nitrogen is short.");
                break;
            case "interveinal":
                r.Condition = "nutrient_deficiency";
                r.Confidence = 0.4;
                if (age == "younger") { Add("Fe", 0.55); Add("Zn", 0.45); Add("Mn", 0.3); }
                else if (age == "older") { Add("Mg", 0.6); Add("K", 0.2); }
                else { Add("Mg", 0.4); Add("Fe", 0.4); Add("Zn", 0.35); }
                r.Advice.Add(age switch
                {
                    "younger" => "Yellow between green veins on young leaves often means iron or zinc is short.",
                    "older" => "Yellow between green veins on older leaves often means magnesium is short.",
                    _ => "Yellow between green veins: on older leaves it is usually magnesium, on young leaves iron or zinc. Look which leaves have it."
                });
                break;
            case "patchy":
                r.Condition = "unclear";
                r.Confidence = 0.3;
                Add("N", 0.3);
                Add("K", 0.25);
                r.Advice.Add("Yellow patches can be a nutrient shortage or a disease. Show the plant to your SPIC officer (MDO).");
                break;
            case "margin":
                r.Condition = "nutrient_deficiency";
                r.Confidence = 0.4;
                if (age == "younger") { Add("Ca", 0.35); Add("B", 0.3); }
                else { Add("K", 0.6); }
                r.Advice.Add(age == "younger"
                    ? "Dry tips and edges on young leaves can mean calcium or boron is short."
                    : "Brown, dry edges on older leaves usually mean potassium is short.");
                break;
            default: // spots
                r.Condition = "unclear";
                r.Confidence = 0.25;
                if (paddy) Add("Zn", 0.35);
                r.Advice.Add("Brown spots are often a disease or a pest. Show the plant to your SPIC officer (MDO) before you spray anything.");
                break;
        }

        if (paddy && m.LccScore < 4 && r.Nutrients.Any(n => n.Code == "N"))
            r.Advice.Add("For paddy, TNAU tops up nitrogen when the leaf colour chart reads below 4 (3 for White Ponni).");

        r.NeedsSoilTest = r.Condition is "nutrient_deficiency" or "unclear";
        r.Nutrients = r.Nutrients.OrderByDescending(n => n.Likelihood).ToList();
        return r;
    }

    // ------------------------------------------------------------------ correctives

    /// <summary>Nutrients shown with "What to do": likelihood at least this, the top three.</summary>
    public const double CorrectiveMinLikelihood = 0.25;

    /// <summary>
    /// The correctives for the likely nutrients (likelihood at least 0.25, the top three): the crop's own rows when it
    /// has any for that nutrient, else the general rows (Crop null); active rows only, in SortOrder.
    /// </summary>
    public static List<NutrientCorrective> SelectCorrectives(IEnumerable<NutrientCorrective> all, string? crop, IEnumerable<LeafNutrientGuess> nutrients)
    {
        var rows = all.Where(c => c.IsActive).ToList();
        var result = new List<NutrientCorrective>();
        foreach (var n in nutrients.Where(n => n.Likelihood >= CorrectiveMinLikelihood).OrderByDescending(n => n.Likelihood).Take(3))
        {
            var own = rows.Where(c => c.Nutrient == n.Code && c.Crop is not null && string.Equals(c.Crop, crop, StringComparison.OrdinalIgnoreCase)).ToList();
            var chosen = own.Count > 0 ? own : rows.Where(c => c.Nutrient == n.Code && c.Crop is null).ToList();
            result.AddRange(chosen.OrderBy(c => c.SortOrder).ThenBy(c => c.Id));
        }
        return result;
    }

    private static string Pct(double share) => $"{Math.Round(share * 100):0} %";
}
