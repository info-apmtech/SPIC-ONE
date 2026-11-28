using System.Globalization;

namespace SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;

/// <summary>Pure helpers of the leaf-photo check screens (farmer portal Task 6), checked by the harness.</summary>
public static class LeafCheckUi
{
    /// <summary>What the farmer can say about the yellow leaves (value, chip text).</summary>
    public static readonly IReadOnlyList<(string Value, string Label)> LeafAges = new[]
    {
        ("older", "Older (lower) leaves"),
        ("younger", "Young (top) leaves"),
        ("all", "All leaves"),
        ("unknown", "Not sure")
    };

    /// <summary>Tips for a good photo (icon, text).</summary>
    public static readonly IReadOnlyList<(string Icon, string Title, string Text)> Tips = new[]
    {
        ("bi-flower1", "One leaf", "Fill the photo with a single leaf that shows the problem."),
        ("bi-brightness-high", "Daylight", "Take it in daylight, in shade. No flash, no strong sun glare."),
        ("bi-file-earmark", "Plain background", "Lay the leaf on a white paper or a plain cloth."),
        ("bi-camera", "Sharp", "Hold the phone still, about one hand away from the leaf.")
    };

    public static int Percent(double fraction) => (int)Math.Round(Math.Clamp(fraction, 0, 1) * 100);

    public static string ConfidenceWord(double confidence) =>
        confidence >= 0.75 ? "Quite sure" : confidence >= 0.5 ? "Fairly sure" : "Not sure";

    public static FpHue ConditionHue(string? condition, bool notALeaf) => notALeaf ? FpHue.Neutral : condition switch
    {
        "healthy" => FpHue.Green,
        "nutrient_deficiency" => FpHue.Gold,
        "disease" => FpHue.Rose,
        "pest" => FpHue.Violet,
        _ => FpHue.Neutral
    };

    public static string ConditionIcon(string? condition, bool notALeaf) => notALeaf ? "bi-image" : condition switch
    {
        "healthy" => "bi-check-circle-fill",
        "nutrient_deficiency" => "bi-droplet-half",
        "disease" => "bi-virus",
        "pest" => "bi-bug-fill",
        _ => "bi-question-circle-fill"
    };

    public static string MethodIcon(string? method) => method == "Foliar" ? "bi-droplet" : "bi-layers";

    public static string MethodLabel(string? method) => method == "Foliar" ? "Leaf spray" : "Soil";

    /// <summary>The crops with the farmer's profile crops first (in their order), each once.</summary>
    public static List<string> CropOrder(IEnumerable<string> crops, string? profileCrops) =>
        CropOrder(crops, (profileCrops ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>The crops with the farmer's profile crops first (in their order), each once.</summary>
    public static List<string> CropOrder(IEnumerable<string> crops, IEnumerable<string>? profileCrops)
    {
        var all = crops.ToList();
        var first = (profileCrops ?? Array.Empty<string>())
            .Select(p => all.FirstOrDefault(c => string.Equals(c, p, StringComparison.OrdinalIgnoreCase)))
            .Where(c => c is not null).Select(c => c!).Distinct().ToList();
        return first.Concat(all.Where(c => !first.Contains(c))).ToList();
    }

    public static string LeftText(int left, int limit) => left <= 0 ? "No checks left today" : $"{left} of {limit} checks left today";

    public static string Size(long bytes) =>
        bytes >= 1024 * 1024
            ? (bytes / 1024d / 1024d).ToString("0.0", CultureInfo.InvariantCulture) + " MB"
            : Math.Max(1, (int)Math.Round(bytes / 1024d)).ToString(CultureInfo.InvariantCulture) + " KB";

    public static string DataUrl(string contentType, byte[] bytes) => $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";

    public static string When(DateTime utc) => FertilizerUi.When(utc);
}
