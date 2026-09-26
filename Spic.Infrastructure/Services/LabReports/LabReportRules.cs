using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using Spic.Infrastructure.Services.Lab;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// Report helpers the lab's result engine does not cover (docs/sas-lab-portal-plan.md, "Reports
/// model"): the financial year (April to March, identified by its start year: 2026 = FY 2026-27),
/// the layouts a sample needs, the translation template of the crop note and value / range
/// formatting for the layouts. Overall status, recommendations and the crop suitability note
/// come from <see cref="LabAutoResultEngine"/> (LabReportReader), and the Lab Number from
/// <see cref="LabSampleIds"/>, so the entry page, the stored results and the report agree.
/// </summary>
public static class LabReportRules
{
    public static int FinancialYearStart(DateTime date) => date.Month >= 4 ? date.Year : date.Year - 1;

    /// <summary>[from, to) of a financial year.</summary>
    public static (DateTime From, DateTime To) FinancialYearRange(int startYear) =>
        (new DateTime(startYear, 4, 1), new DateTime(startYear + 1, 4, 1));

    public static string FinancialYearLabel(int startYear) => $"{startYear}-{(startYear + 1) % 100:00}";

    /// <summary>The layouts a sample needs: SoilAndWater gets a Soil and a Water report.</summary>
    public static IEnumerable<SampleType> ReportTypesFor(SampleType type) => type switch
    {
        SampleType.SoilAndWater => new[] { SampleType.Soil, SampleType.Water },
        SampleType.Water => new[] { SampleType.Water },
        _ => new[] { SampleType.Soil }
    };

    /// <summary>Translation template of the crop note (note.* keys): the translated layouts fill
    /// {crop} and {params}; English prints the engine's sentence as it is. SoilPoor is kept for
    /// its seed rows only (the engine has one "after correcting" sentence).</summary>
    public enum CropNoteKind { SoilGood, SoilCorrect, SoilPoor, WaterGood, WaterCaution }

    /// <summary>The template kind of the engine's note for a layout, with the parameters it names.</summary>
    public static (CropNoteKind Kind, List<LabSuitabilityProblem> Problems) CropNote(SampleType layout, LabSuitability? note)
    {
        if (layout == SampleType.Water)
        {
            var water = note?.Water ?? new List<LabSuitabilityProblem>();
            return (water.Count == 0 ? CropNoteKind.WaterGood : CropNoteKind.WaterCaution, water);
        }
        var soil = note?.Soil ?? new List<LabSuitabilityProblem>();
        return (soil.Count == 0 ? CropNoteKind.SoilGood : CropNoteKind.SoilCorrect, soil);
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
    public string LabNumber { get; set; } = "";        // SAS-SOIL-001 (LabSampleIds, numbered per batch)
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
    /// <summary>The engine's recommendation lines in parts (for the translated layouts).</summary>
    public List<LabRecommendationLine> RecommendationLines { get; set; } = new();
    /// <summary>The engine's crop note in parts (for the translated layouts).</summary>
    public LabSuitability? Suitability { get; set; }

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
