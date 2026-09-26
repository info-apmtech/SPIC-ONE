using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Shared.Pages.Lab.Analyst;

/// <summary>
/// Wording, formatting and small rules shared by the Lab Analyst pages (screens 13-19).
/// Analyst wording of the batch status (docs/sas-lab-portal-plan.md): TakenForAnalysis = Pending
/// Entry, InProgress = In Progress, AnalysisCompleted = Auto Result Ready, Completed = Report
/// Generated. A sample reads Pending Entry / In Progress / Auto Result Ready, and Report Generated
/// once its batch is Completed.
/// </summary>
public static class AnalystText
{
    public static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // ---------------------------------------------------------------- batch status

    public static string BatchStatusText(SampleBatchStatus s) => s switch
    {
        SampleBatchStatus.Created => "Created",
        SampleBatchStatus.TakenForAnalysis => "Pending Entry",
        SampleBatchStatus.InProgress => "In Progress",
        SampleBatchStatus.AnalysisCompleted => "Auto Result Ready",
        SampleBatchStatus.Completed => "Report Generated",
        _ => s.ToString()
    };

    /// <summary>Status filter options of the Batch List (value = SampleBatchStatus name the API accepts).</summary>
    public static readonly (string Value, string Label)[] BatchStatusOptions =
    {
        (nameof(SampleBatchStatus.TakenForAnalysis), "Pending Entry"),
        (nameof(SampleBatchStatus.InProgress), "In Progress"),
        (nameof(SampleBatchStatus.AnalysisCompleted), "Auto Result Ready"),
        (nameof(SampleBatchStatus.Completed), "Report Generated")
    };

    // ---------------------------------------------------------------- sample status

    public static string SampleStatusText(LabSampleRowDto s) =>
        s.ReportGenerated ? "Report Generated" : SampleStatusText(s.Status);

    public static string SampleStatusText(SampleAnalysisStatus s) => s switch
    {
        SampleAnalysisStatus.NotStarted => "Pending Entry",
        SampleAnalysisStatus.InProgress => "In Progress",
        SampleAnalysisStatus.Completed => "Auto Result Ready",
        _ => s.ToString()
    };

    public static string SampleTone(LabSampleRowDto s) =>
        s.ReportGenerated ? "teal" : SPIC.MauiBlazorApp.Shared.Components.Lab.LabPill.ToneFor(s.Status);

    /// <summary>Row action of "Samples in this Batch": edit / continue / preview / view.</summary>
    public static (string Icon, string Label) SampleAction(LabSampleRowDto s) =>
        s.ReportGenerated ? ("bi-eye", "View")
        : s.Status switch
        {
            SampleAnalysisStatus.NotStarted => ("bi-pencil-square", "Enter values"),
            SampleAnalysisStatus.InProgress => ("bi-arrow-right-circle", "Continue entry"),
            _ => ("bi-clipboard-check", "Preview result")
        };

    // ---------------------------------------------------------------- types, priority, result

    public static string PriorityText(SampleBatchPriority p) => p.ToString();

    public static string PaymentTypeText(SamplePaymentType t) => t == SamplePaymentType.Paid ? "Paid" : "Free";

    public static string SampleTypeText(SampleType t) => t switch
    {
        SPIC.Core.Entities.SampleType.Soil => "Soil",
        SPIC.Core.Entities.SampleType.Water => "Water",
        _ => "Soil & Water"
    };

    public static string ResultStatusText(LabResultStatus? s) => s switch
    {
        LabResultStatus.Normal => "Normal",
        LabResultStatus.Deficient => "Deficient",
        LabResultStatus.Moderate => "Moderate",
        LabResultStatus.Excess => "Excess",
        _ => "-"
    };

    public static string OverallTone(LabOverallStatus s) => s switch
    {
        LabOverallStatus.Good => "green",
        LabOverallStatus.NeedsImprovement => "orange",
        _ => "red"
    };

    /// <summary>"Overall Soil Status" / "Overall Water Status" / "Overall Status".</summary>
    public static string OverallLabel(SampleType t) => t switch
    {
        SPIC.Core.Entities.SampleType.Soil => "Overall Soil Status",
        SPIC.Core.Entities.SampleType.Water => "Overall Water Status",
        _ => "Overall Status"
    };

    public static string ReportStatusText(LabReportStatus s) => s.ToString();

    public static string ReportTone(LabReportStatus s) => s switch
    {
        LabReportStatus.Generated => "green",
        LabReportStatus.Downloaded => "blue",
        LabReportStatus.Printed => "violet",
        _ => "grey"
    };

    public static string RecommendationTitle(string group) => group switch
    {
        "Fertilizer" => "Fertilizer Recommendation",
        "Organic" => "Organic Recommendation",
        "Micronutrient" => "Micronutrient Recommendation",
        "General" => "General Recommendation",
        _ => group
    };

    public static string RecommendationIcon(string group) => group switch
    {
        "Fertilizer" => "bi-bag",
        "Organic" => "bi-flower1",
        "Micronutrient" => "bi-droplet-half",
        _ => "bi-info-circle"
    };

    // ---------------------------------------------------------------- dates

    public static string Date(DateTime? d) => d is null ? "-" : d.Value.ToString("dd MMM yyyy", Invariant);
    public static string Time(DateTime? d) => d is null ? "" : d.Value.ToString("hh:mm tt", Invariant);
    public static string DateTimeText(DateTime? d) => d is null ? "-" : d.Value.ToString("dd MMM yyyy, hh:mm tt", Invariant);

    /// <summary>Financial year start (April to March).</summary>
    public static int FinancialYearStart(DateTime d) => d.Month >= 4 ? d.Year : d.Year - 1;
    public static int CurrentFinancialYear => FinancialYearStart(DateTime.Today);

    /// <summary>2025 -> "2025-26" with an en dash.</summary>
    public static string FinancialYear(int start) => $"{start}{EnDash}{(start + 1) % 100:00}";

    public const char EnDash = (char)0x2013;

    public static string Dash(string? s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

    public static CommunityMember Person(string? name, string? avatar = null) => new()
    {
        Name = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim(),
        Avatar = string.IsNullOrWhiteSpace(avatar) ? null : avatar
    };
}

/// <summary>
/// How a parameter row is entered. LabParameterRowDto (the fixed contract) does not carry the
/// master's ValueType / Options / DerivedFromCode, so the page recognises the two special rows of
/// the seeded master by code (S-TEX is a text parameter with options, S-OM = S-OC x 1.724 is
/// derived) and falls back to "numeric" for everything else. Any row the preview fills in that
/// the analyst did not send is treated as derived as well (see SampleEntry.razor).
/// </summary>
public static class AnalystParameters
{
    private static readonly Dictionary<string, string[]> TextOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S-TEX"] = new[] { "Sandy", "Loamy Sand", "Sandy Loam", "Loam", "Silt Loam", "Clay Loam", "Sandy Clay", "Silty Clay", "Clay", "Sandy Clay Silt" }
    };

    private static readonly Dictionary<string, string> Derived = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S-OM"] = "Organic Carbon x 1.724"
    };

    public static bool IsText(LabParameterRowDto p) => TextOptions.ContainsKey(p.Code);

    public static IReadOnlyList<string> OptionsFor(LabParameterRowDto p) =>
        TextOptions.TryGetValue(p.Code, out var o) ? o : Array.Empty<string>();

    public static bool IsDerivedCode(string code) => Derived.ContainsKey(code);

    public static string? DerivedNote(string code) => Derived.TryGetValue(code, out var n) ? n : null;

    public static bool TryNumber(string? value, out decimal number) =>
        decimal.TryParse((value ?? "").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out number);
}
