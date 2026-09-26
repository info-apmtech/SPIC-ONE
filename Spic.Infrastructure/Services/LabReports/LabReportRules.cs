using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// Pure rules shared by report generation, the report DTOs and the PDF / Excel layouts
/// (docs/sas-lab-portal-plan.md, "Reports model" and the auto-result paragraph):
///   * Financial year = April to March, identified by its start year (2026 = FY 2026-27).
///   * Overall status: Good when no parameter is Deficient / Excess, Needs Improvement when
///     one or two are, Poor when three or more are.
///   * Recommendations: the hint of every out-of-range row (Deficient / Moderate / Excess),
///     grouped by LabParameter.RecommendationGroup (Fertilizer, Organic, Micronutrient, General).
///   * Crop suitability note: composed from the crop and the out-of-range parameters.
/// </summary>
public static class LabReportRules
{
    public static readonly string[] GroupOrder = { "Fertilizer", "Organic", "Micronutrient", "General" };

    public static int FinancialYearStart(DateTime date) => date.Month >= 4 ? date.Year : date.Year - 1;

    /// <summary>[from, to) of a financial year.</summary>
    public static (DateTime From, DateTime To) FinancialYearRange(int startYear) =>
        (new DateTime(startYear, 4, 1), new DateTime(startYear + 1, 4, 1));

    public static string FinancialYearLabel(int startYear) => $"{startYear}-{(startYear + 1) % 100:00}";

    /// <summary>Display id of a sample inside the lab ("Lab Number"): SAS-SOIL-001 / SAS-WATER-001.</summary>
    public static string SampleDisplayId(int sampleItemId, SampleType type) =>
        $"SAS-{(type == SampleType.Water ? "WATER" : "SOIL")}-{sampleItemId:000}";

    /// <summary>The layouts a sample needs: SoilAndWater gets a Soil and a Water report.</summary>
    public static IEnumerable<SampleType> ReportTypesFor(SampleType type) => type switch
    {
        SampleType.SoilAndWater => new[] { SampleType.Soil, SampleType.Water },
        SampleType.Water => new[] { SampleType.Water },
        _ => new[] { SampleType.Soil }
    };

    public static bool IsOutOfRange(LabResultStatus? status) =>
        status == LabResultStatus.Deficient || status == LabResultStatus.Excess;

    public static bool NeedsAction(LabResultStatus? status) =>
        status == LabResultStatus.Deficient || status == LabResultStatus.Excess || status == LabResultStatus.Moderate;

    public static LabOverallStatus Overall(IEnumerable<LabReportRow> rows)
    {
        var count = rows.Count(r => !r.IsText && r.HasValue && IsOutOfRange(r.Status));
        return count == 0 ? LabOverallStatus.Good : count <= 2 ? LabOverallStatus.NeedsImprovement : LabOverallStatus.Poor;
    }

    public static string OverallText(LabOverallStatus status) => status switch
    {
        LabOverallStatus.Good => "Good",
        LabOverallStatus.NeedsImprovement => "Needs Improvement",
        _ => "Poor"
    };

    public static List<LabRecommendationGroupDto> Group(IEnumerable<LabReportRow> rows)
    {
        var groups = new List<LabRecommendationGroupDto>();
        foreach (var row in rows.Where(r => !r.IsText && r.HasValue && NeedsAction(r.Status) && !string.IsNullOrWhiteSpace(r.Hint)))
        {
            var name = NormalizeGroup(row.Group);
            var group = groups.FirstOrDefault(g => g.Group == name);
            if (group == null)
            {
                group = new LabRecommendationGroupDto { Group = name };
                groups.Add(group);
            }
            var line = row.Hint!.Trim();
            if (!group.Lines.Contains(line, StringComparer.OrdinalIgnoreCase)) group.Lines.Add(line);
        }

        return groups
            .OrderBy(g => Array.IndexOf(GroupOrder, g.Group) is var i && i >= 0 ? i : GroupOrder.Length)
            .ThenBy(g => g.Group)
            .ToList();
    }

    public static string NormalizeGroup(string? group)
    {
        if (string.IsNullOrWhiteSpace(group)) return "General";
        var match = GroupOrder.FirstOrDefault(g => string.Equals(g, group.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? group.Trim();
    }

    /// <summary>Kind of crop note, so the layouts can translate it: the caller formats it with the
    /// crop name and the (translated) parameter list.</summary>
    public enum CropNoteKind { SoilGood, SoilCorrect, SoilPoor, WaterGood, WaterCaution }

    public static (CropNoteKind Kind, List<LabReportRow> OutOfRange) CropNote(SampleType layout, IEnumerable<LabReportRow> rows)
    {
        var bad = rows.Where(r => !r.IsText && r.HasValue && IsOutOfRange(r.Status)).ToList();
        if (layout == SampleType.Water)
            return (bad.Count == 0 ? CropNoteKind.WaterGood : CropNoteKind.WaterCaution, bad);
        return (bad.Count == 0 ? CropNoteKind.SoilGood : bad.Count <= 2 ? CropNoteKind.SoilCorrect : CropNoteKind.SoilPoor, bad);
    }

    /// <summary>English crop note (the DTOs are English; the layouts use the translated keys).</summary>
    public static string CropNoteEnglish(SampleType layout, string? crop, IEnumerable<LabReportRow> rows)
    {
        var (kind, bad) = CropNote(layout, rows);
        var list = string.Join(", ", bad.Select(b => b.Name));
        var cropName = string.IsNullOrWhiteSpace(crop) ? "the proposed crop" : crop!;
        return LabTranslationSeed.CropNoteTemplate(kind)
            .Replace("{crop}", cropName)
            .Replace("{params}", list);
    }

    public static string FormatValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return value.Trim();
    }

    /// <summary>"0.87 - 1.29 (%)" like the reference; a "-" unit is dropped.</summary>
    public static string RangeWithUnit(string? range, string? unit)
    {
        var r = (range ?? "").Trim();
        var u = CleanUnit(unit);
        if (r.Length == 0) return "";
        return u == null ? r : $"{r}  ({u})";
    }

    public static string? CleanUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return null;
        var u = unit.Trim();
        return u == "-" ? null : u;
    }
}

/// <summary>One parameter row of a sample report (from SampleLabResult + its LabParameter).</summary>
public sealed class LabReportRow
{
    public int? LabParameterId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Unit { get; set; }
    public string? NormalRange { get; set; }
    public string? ReportingLimit { get; set; }
    public string Group { get; set; } = "General";
    public string? Value { get; set; }
    public string? ResultLabel { get; set; }
    public LabResultStatus? Status { get; set; }
    public string? Hint { get; set; }
    public bool IsText { get; set; }
    public int SortOrder { get; set; }
    public DateTime EnteredAt { get; set; }
    public string? EnteredByName { get; set; }
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
}

/// <summary>Everything one sample report page needs (English data; the layouts translate labels).</summary>
public sealed class LabReportModel
{
    public int ReportId { get; set; }
    public string ReportCode { get; set; } = "";
    public SampleType Layout { get; set; }
    public LabReportStatus Status { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int FinancialYearStart { get; set; }
    public int DownloadCount { get; set; }

    public int BatchId { get; set; }
    public string BatchCode { get; set; } = "";
    public List<string> ConsignmentCodes { get; set; } = new();

    public int SampleItemId { get; set; }
    public int CollectionId { get; set; }
    public string SampleNumber { get; set; } = "";     // v1 item code
    public string LabNumber { get; set; } = "";        // SAS-SOIL-001
    public SampleType ItemType { get; set; }
    public SamplePaymentType PaymentType { get; set; }
    public SampleAnalysisStatus AnalysisStatus { get; set; }
    public DateTime? AnalysisStartedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
    public DateTime CollectedOn { get; set; }

    public string FarmerName { get; set; } = "";
    public List<string> AddressLines { get; set; } = new();
    public string? Mobile { get; set; }
    public string? SurveyNumber { get; set; }
    public string? Village { get; set; }
    public string? District { get; set; }
    public string? StateName { get; set; }
    public string? Crop1 { get; set; }
    public string? Crop2 { get; set; }

    public List<LabReportRow> Rows { get; set; } = new();
    public LabOverallStatus OverallStatus { get; set; }
    public List<LabRecommendationGroupDto> Recommendations { get; set; } = new();
    public string CropSuitabilityNote { get; set; } = "";

    /// <summary>Fertilizer schedule columns (Crop1, Crop2 or Crop1 twice like the reference).</summary>
    public List<LabScheduleColumn> Schedule { get; set; } = new();
}

public sealed class LabScheduleColumn
{
    public string Crop { get; set; } = "";              // the crop the column is for (farmer's crop)
    public string ScheduleCrop { get; set; } = "";      // the crop whose rows are printed
    public bool IsGeneral { get; set; }                  // no rows for Crop -> Banana rows printed as "General"
    public List<LabCropRecommendation> Rows { get; set; } = new();
}
