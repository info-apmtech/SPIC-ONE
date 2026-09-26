namespace SPIC.Core.Interfaces;

/// <summary>
/// Report generation for the SAS Lab portal (docs/sas-lab-portal-plan.md). Implemented by the
/// reports workstream (Spic.Infrastructure/Services/LabReports); called by LabController when a
/// batch is completed. One LabReport row per sample (two for SoilAndWater samples), codes
/// RPT-SAS-{yyyy}-{001}, batch ReportCode REP-SAS-{yyyy}-{001}; idempotent (re-running for a
/// batch that already has reports returns without creating duplicates).
/// </summary>
public interface ILabReportService
{
    Task GenerateForBatchAsync(int batchId, string? byUserId, string? byName, CancellationToken ct = default);
}
