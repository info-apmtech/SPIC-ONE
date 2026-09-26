using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// Writes the batch Activity Log / Timeline rows (screens 5, 8 and 22). One method per
/// <see cref="LabActivityKind"/>; titles are the pills of screen 8. Rows are only ADDED to the
/// context: the caller saves them together with the change they describe. The actor is the
/// caller resolved by <see cref="LabAccess"/> (name, user id, designation as the role).
/// </summary>
public sealed class LabActivityWriter
{
    private readonly AppDbContext _db;
    private readonly LabAccess _access;

    public LabActivityWriter(AppDbContext db, LabAccess access)
    {
        _db = db;
        _access = access;
    }

    public static string Title(LabActivityKind kind) => kind switch
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
        _ => kind.ToString()
    };

    public static string StatusText(SampleBatchStatus status) => status switch
    {
        SampleBatchStatus.Created => "Created",
        SampleBatchStatus.TakenForAnalysis => "Taken for Analysis",
        SampleBatchStatus.InProgress => "In Progress",
        SampleBatchStatus.AnalysisCompleted => "Analysis Completed",
        SampleBatchStatus.Completed => "Completed",
        _ => status.ToString()
    };

    public static string DocumentKindText(LabDocumentKind kind) => kind switch
    {
        LabDocumentKind.Batch => "Batch Document",
        LabDocumentKind.Sample => "Sample Document",
        LabDocumentKind.Reference => "Reference Document",
        _ => kind.ToString()
    };

    public LabActivity BatchCreated(SampleBatch batch, DateTime at) =>
        Add(batch, LabActivityKind.BatchCreated, $"Batch {batch.Code} has been created", at);

    public LabActivity SampleReceived(SampleBatch batch, string consignmentCode, int samples, DateTime at) =>
        Add(batch, LabActivityKind.SampleReceived,
            $"Consignment {consignmentCode} received at the lab with {samples} sample{(samples == 1 ? "" : "s")}", at);

    public LabActivity SampleLogged(SampleBatch batch, int samples, DateTime at) =>
        Add(batch, LabActivityKind.SampleLogged, $"All {samples} samples logged into system", at);

    public LabActivity ParameterAssigned(SampleBatch batch, int parameterRows, int samples, DateTime at) =>
        Add(batch, LabActivityKind.ParameterAssigned, $"{parameterRows} parameters assigned to {samples} samples", at);

    public LabActivity Assigned(SampleBatch batch, string analystName, bool reassigned, DateTime at) =>
        Add(batch, LabActivityKind.Assigned,
            reassigned ? $"Batch reassigned to {analystName}" : $"Batch assigned to {analystName}", at);

    public LabActivity StatusUpdated(SampleBatch batch, SampleBatchStatus status, DateTime at, string? note = null) =>
        Add(batch, LabActivityKind.StatusUpdated,
            $"Status updated to {StatusText(status)}" + (string.IsNullOrWhiteSpace(note) ? "" : $": {note.Trim()}"), at);

    public LabActivity AnalysisStarted(SampleBatch batch, string sampleId, DateTime at) =>
        Add(batch, LabActivityKind.AnalysisStarted, $"Analysis started with sample {sampleId}", at);

    public LabActivity AnalysisInProgress(SampleBatch batch, string sampleId, int entered, int total, DateTime at) =>
        Add(batch, LabActivityKind.AnalysisInProgress,
            $"Draft saved for sample {sampleId} ({entered} of {total} parameters entered)", at);

    public LabActivity ResultEntered(SampleBatch batch, string sampleId, string overallStatus, DateTime at) =>
        Add(batch, LabActivityKind.ResultEntered,
            $"Test values submitted for sample {sampleId} (overall status: {overallStatus})", at);

    public LabActivity DocumentUploaded(SampleBatch batch, string fileName, LabDocumentKind kind, DateTime at) =>
        Add(batch, LabActivityKind.DocumentUploaded, $"{fileName} uploaded as {DocumentKindText(kind)}", at);

    public LabActivity DocumentDeleted(SampleBatch batch, string fileName, DateTime at) =>
        Add(batch, LabActivityKind.DocumentDeleted, $"{fileName} deleted", at);

    public LabActivity ReportGenerated(SampleBatch batch, string? reportCode, int sampleReports, DateTime at) =>
        Add(batch, LabActivityKind.ReportGenerated,
            string.IsNullOrWhiteSpace(reportCode)
                ? $"{sampleReports} sample report{(sampleReports == 1 ? "" : "s")} generated"
                : $"Report {reportCode} generated ({sampleReports} sample report{(sampleReports == 1 ? "" : "s")})", at);

    private LabActivity Add(SampleBatch batch, LabActivityKind kind, string description, DateTime at)
    {
        var row = new LabActivity
        {
            BatchId = batch.Id,
            Kind = kind,
            Title = Title(kind),
            Description = description,
            ByUserId = string.IsNullOrWhiteSpace(_access.UserId) ? null : _access.UserId,
            ByName = _access.Name,
            ByRole = _access.RoleLabel,
            At = at
        };

        _db.LabActivities.Add(row);
        return row;
    }
}
