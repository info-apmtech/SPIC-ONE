using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Components.Lab.Batch;

/// <summary>
/// Wording, pill tones and icons shared by the lab portal pages (coordinator first; the analyst
/// and admin pages can reuse it). Tones are LabPill tone names.
/// </summary>
public static class LabText
{
    // ---------------------------------------------------------------- consignments

    public static string ConsignmentStatus(LabConsignmentStatus s) => s switch
    {
        LabConsignmentStatus.InTransit => "In Transit",
        LabConsignmentStatus.BatchPending => "Batch Pending",
        LabConsignmentStatus.BatchCreated => "Batch Created",
        LabConsignmentStatus.Completed => "Completed",
        _ => s.ToString()
    };

    public static string ConsignmentTone(LabConsignmentStatus s) => s switch
    {
        LabConsignmentStatus.InTransit => "blue",
        LabConsignmentStatus.BatchPending => "orange",
        LabConsignmentStatus.BatchCreated => "green",
        LabConsignmentStatus.Completed => "teal",
        _ => "grey"
    };

    public static readonly LabConsignmentStatus[] ConsignmentStatuses =
    {
        LabConsignmentStatus.InTransit, LabConsignmentStatus.BatchPending,
        LabConsignmentStatus.BatchCreated, LabConsignmentStatus.Completed
    };

    // ---------------------------------------------------------------- batches

    public static string BatchStatus(SampleBatchStatus s) => s switch
    {
        SampleBatchStatus.Created => "Created",
        SampleBatchStatus.TakenForAnalysis => "Taken for Analysis",
        SampleBatchStatus.InProgress => "In Progress",
        SampleBatchStatus.AnalysisCompleted => "Analysis Completed",
        SampleBatchStatus.Completed => "Completed",
        _ => s.ToString()
    };

    public static readonly SampleBatchStatus[] BatchStatuses =
    {
        SampleBatchStatus.Created, SampleBatchStatus.TakenForAnalysis, SampleBatchStatus.InProgress,
        SampleBatchStatus.AnalysisCompleted, SampleBatchStatus.Completed
    };

    public static string Priority(SampleBatchPriority p) => p.ToString();

    /// <summary>"Pending" until analysis starts, "n Days" afterwards.</summary>
    public static string AnalysisDays(int? days) => days switch
    {
        null => "Pending",
        1 => "1 Day",
        _ => $"{days} Days"
    };

    public static string AnalysisDaysTone(LabBatchRowDto b) =>
        b.AnalysisDays is null ? "yellow" : b.IsDelayed ? "red" : "green";

    // ---------------------------------------------------------------- samples and parameters

    public static string SampleStatus(SampleAnalysisStatus s) => s switch
    {
        SampleAnalysisStatus.NotStarted => "Not Started",
        SampleAnalysisStatus.InProgress => "In Progress",
        SampleAnalysisStatus.Completed => "Completed",
        _ => s.ToString()
    };

    public static readonly SampleAnalysisStatus[] SampleStatuses =
    {
        SampleAnalysisStatus.NotStarted, SampleAnalysisStatus.InProgress, SampleAnalysisStatus.Completed
    };

    public static string SampleType(SampleType t) => t switch
    {
        SPIC.Core.Entities.SampleType.Soil => "Soil",
        SPIC.Core.Entities.SampleType.Water => "Water",
        SPIC.Core.Entities.SampleType.SoilAndWater => "Soil & Water",
        _ => t.ToString()
    };

    public static int SoilCount(SampleType t) => t is SPIC.Core.Entities.SampleType.Soil or SPIC.Core.Entities.SampleType.SoilAndWater ? 1 : 0;
    public static int WaterCount(SampleType t) => t is SPIC.Core.Entities.SampleType.Water or SPIC.Core.Entities.SampleType.SoilAndWater ? 1 : 0;

    public static string PaymentType(SamplePaymentType t) => t == SamplePaymentType.Paid ? "Paid" : "Free";
    public static string PaymentTone(SamplePaymentType t) => t == SamplePaymentType.Paid ? "violet" : "blue";

    public static string ResultStatus(LabResultStatus s) => s.ToString();

    // ---------------------------------------------------------------- documents

    public static string DocumentKind(LabDocumentKind k) => k switch
    {
        LabDocumentKind.Batch => "Batch Document",
        LabDocumentKind.Sample => "Sample Document",
        LabDocumentKind.Reference => "Reference Document",
        _ => k.ToString()
    };

    public static string DocumentIcon(string? extension) => (extension ?? "").Trim('.').ToUpperInvariant() switch
    {
        "PDF" => "bi-file-earmark-pdf",
        "XLSX" or "XLS" or "CSV" => "bi-file-earmark-excel",
        "DOCX" or "DOC" => "bi-file-earmark-word",
        "JPG" or "JPEG" or "PNG" or "WEBP" or "GIF" => "bi-file-earmark-image",
        _ => "bi-file-earmark"
    };

    public static bool IsImage(LabDocumentDto d) => d.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public static bool IsPdf(LabDocumentDto d) => d.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || string.Equals(d.Extension.Trim('.'), "pdf", StringComparison.OrdinalIgnoreCase);

    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / (1024.0 * 1024.0):0.#} MB"
    };

    // ---------------------------------------------------------------- activity

    public static string ActivityKind(LabActivityKind k) => k switch
    {
        LabActivityKind.BatchCreated => "Batch Created",
        LabActivityKind.SampleReceived => "Sample Received",
        LabActivityKind.SampleLogged => "Sample Logged",
        LabActivityKind.ParameterAssigned => "Parameter Assigned",
        LabActivityKind.AnalysisStarted => "Analysis Started",
        LabActivityKind.AnalysisInProgress => "Analysis In Progress",
        LabActivityKind.DocumentUploaded => "Document Uploaded",
        LabActivityKind.StatusUpdated => "Status Updated",
        LabActivityKind.ResultEntered => "Result Entered",
        LabActivityKind.ReportGenerated => "Report Generated",
        LabActivityKind.DocumentDeleted => "Document Deleted",
        LabActivityKind.Assigned => "Assigned",
        _ => k.ToString()
    };

    public static string ActivityTone(LabActivityKind k) => k switch
    {
        LabActivityKind.BatchCreated => "green",
        LabActivityKind.SampleReceived => "blue",
        LabActivityKind.SampleLogged => "teal",
        LabActivityKind.ParameterAssigned => "violet",
        LabActivityKind.AnalysisStarted => "orange",
        LabActivityKind.AnalysisInProgress => "violet",
        LabActivityKind.DocumentUploaded => "pink",
        LabActivityKind.StatusUpdated => "blue",
        LabActivityKind.ResultEntered => "teal",
        LabActivityKind.ReportGenerated => "green",
        LabActivityKind.DocumentDeleted => "red",
        LabActivityKind.Assigned => "yellow",
        _ => "grey"
    };

    public static string ActivityIcon(LabActivityKind k) => k switch
    {
        LabActivityKind.BatchCreated => "bi-collection",
        LabActivityKind.SampleReceived => "bi-box-arrow-in-down",
        LabActivityKind.SampleLogged => "bi-journal-check",
        LabActivityKind.ParameterAssigned => "bi-list-check",
        LabActivityKind.AnalysisStarted => "bi-play-circle",
        LabActivityKind.AnalysisInProgress => "bi-hourglass-split",
        LabActivityKind.DocumentUploaded => "bi-cloud-arrow-up",
        LabActivityKind.StatusUpdated => "bi-arrow-repeat",
        LabActivityKind.ResultEntered => "bi-pencil-square",
        LabActivityKind.ReportGenerated => "bi-file-earmark-check",
        LabActivityKind.DocumentDeleted => "bi-trash",
        LabActivityKind.Assigned => "bi-person-check",
        _ => "bi-dot"
    };

    public static readonly LabActivityKind[] ActivityKinds = (LabActivityKind[])Enum.GetValues(typeof(LabActivityKind));

    // ---------------------------------------------------------------- formatting

    public static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value!;
    public static string Date(DateTime value) => value.ToString("dd MMM yyyy");
    public static string Date(DateTime? value) => value is { } v ? Date(v) : "-";
    public static string Time(DateTime value) => value.ToString("hh:mm tt");
    public static string Stamp(DateTime value) => value.ToString("dd MMM yyyy, hh:mm tt");
    public static string Stamp(DateTime? value) => value is { } v ? Stamp(v) : "-";

    public static int Percent(int part, int total) => total <= 0 ? 0 : (int)Math.Round(part * 100.0 / total);
}
