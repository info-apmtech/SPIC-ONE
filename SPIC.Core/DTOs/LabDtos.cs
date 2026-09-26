using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// SAS Lab portal API contracts (version 2, 2026-09-27; docs/sas-lab-portal-plan.md).
/// Controller "Lab", all [Authorize]. Access is by PAGE, resolved on the server from the user's
/// designation RoleAccess exactly like LibraryController does (RoleAccessPermissions.HasPage):
///   COORDINATOR pages : LabDashboard, LabConsignments, LabAnalysis, LabReports
///   ANALYST page      : LabTestEntry (sees only batches assigned to them)
///   Admin / CorporateAdmin bypass the page checks (same as everywhere else).
/// Every list is paged (PageResult&lt;T&gt;, default 16, max 50) and takes q= (search), from= / to=
/// (dates, inclusive) and the filters named per route. Enums serialize as integers.
/// Dates are local (DateTime.Now convention of the rest of the API).
///
///   GET    api/Lab/me                                       -> LabMeDto (which lab pages the caller holds; drives the dashboard variant)
///   GET    api/Lab/dashboard                                -> LabDashboardDto (coordinator KPIs + recent consignments and batches, 5 each)
///   GET    api/Lab/analyst/dashboard                        -> LabAnalystDashboardDto (analyst KPIs + work priority counts)
///   GET    api/Lab/analysts                                 -> List&lt;LabUserDto&gt; (users whose designation grants LabTestEntry, plus admins)
///   GET    api/Lab/consignments/stats                       -> LabConsignmentStatsDto
///   GET    api/Lab/consignments?status=&amp;stateId=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize=  -> PageResult&lt;LabConsignmentRowDto&gt;
///                                                            status = LabConsignmentStatus name (InTransit, BatchPending, BatchCreated, Completed) or empty
///   GET    api/Lab/consignments/{id}                        -> LabConsignmentDetailDto
///   POST   api/Lab/consignments/{id}/receive                -> LabConsignmentRowDto (Dispatched / InTransit -> Delivered; writes v1 status events)
///   GET    api/Lab/batches/stats                            -> LabBatchStatsDto (Analysis Tracking KPIs)
///   GET    api/Lab/batches?status=&amp;priority=&amp;assignedTo=&amp;sampleType=&amp;financialYear=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize=
///                                                           -> PageResult&lt;LabBatchRowDto&gt; (analyst: only their own; financialYear = start year, e.g. 2026)
///   POST   api/Lab/batches                                  -> LabBatchDetailDto (body: LabBatchCreateDto; consignments must be Delivered and unbatched)
///   GET    api/Lab/batches/{id}                             -> LabBatchDetailDto (header + overview + info + timeline)
///   PATCH  api/Lab/batches/{id}/assign                      -> LabBatchDetailDto (body: LabBatchAssignDto)
///   PATCH  api/Lab/batches/{id}/status?status=TakenForAnalysis|Completed&amp;remarks=
///                                                           -> LabBatchDetailDto (Completed needs every sample Completed; creates the LabReport; InProgress and AnalysisCompleted are set by the system on value entry)
///   GET    api/Lab/batches/{id}/samples/stats               -> LabSampleStatsDto
///   GET    api/Lab/batches/{id}/samples?status=&amp;sampleType=&amp;parameter=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize= -> PageResult&lt;LabSampleRowDto&gt;
///   GET    api/Lab/batches/{id}/samples/export              -> .xlsx (same filters)
///   GET    api/Lab/batches/{id}/parameters/stats            -> LabParameterStatsDto
///   GET    api/Lab/batches/{id}/parameters?status=&amp;sampleType=&amp;parameter=&amp;q=&amp;page=&amp;pageSize= -> PageResult&lt;LabSampleParametersDto&gt; (one row per SAMPLE, each with its parameter rows)
///   GET    api/Lab/batches/{id}/parameters/export           -> .xlsx
///   GET    api/Lab/batches/{id}/documents?kind=&amp;uploader=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize= -> PageResult&lt;LabDocumentDto&gt;
///   POST   api/Lab/batches/{id}/documents?kind=Batch|Sample|Reference&amp;description=  (multipart "files"; pdf, images, xlsx, docx; &lt;= 20 MB each) -> List&lt;LabDocumentDto&gt;
///   DELETE api/Lab/documents/{docId}
///   GET    api/Lab/batches/{id}/activities?kind=&amp;by=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize= -> PageResult&lt;LabActivityDto&gt;
///   GET    api/Lab/samples/{itemId}                         -> LabSampleEntryDto (sample, farmer, parameter rows with current values, auto summary, recommendations)
///   PUT    api/Lab/samples/{itemId}/values                  -> LabSampleEntryDto (body: LabSampleValuesDto; Submit=false saves a draft -> InProgress, Submit=true locks -> Completed; rejected once the batch is Completed)
///   POST   api/Lab/samples/preview                          -> LabAutoResultDto (body: LabSampleValuesDto with SampleItemId; computes without saving)
///   GET    api/Lab/reports/stats?financialYear=             -> LabReportStatsDto (batch groups, sample reports, generated today, downloads)
///   GET    api/Lab/reports/batches?stateId=&amp;regionId=&amp;hqId=&amp;sampleType=&amp;financialYear=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize=
///                                                           -> PageResult&lt;LabReportBatchRowDto&gt; (batch-wise summary; analyst: own batches; coordinator drawer uses /batches/{id}/report)
///   GET    api/Lab/batches/{id}/report                      -> LabBatchReportDto (coordinator "Lab Report Details" drawer + admin Batch Report Details: batch info, KPIs, sample reports)
///   GET    api/Lab/batches/{id}/report/download?lang=&amp;format=pdf|zip -> all sample reports of the batch (one merged PDF, or a zip of PDFs)
///   GET    api/Lab/reports?batchId=&amp;sampleType=&amp;status=&amp;crop=&amp;village=&amp;financialYear=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize=
///                                                           -> PageResult&lt;LabReportRowDto&gt; (sample-wise reports; farmer: own samples)
///   GET    api/Lab/reports/{id}                             -> LabReportDetailDto (one sample report: header, results, recommendations, timeline)
///   GET    api/Lab/reports/{id}/pdf?lang=en|ta|te|mr        -> application/pdf  (also ?access_token=; counts a download, status Downloaded)
///   GET    api/Lab/reports/{id}/xlsx?lang=                  -> .xlsx            (also ?access_token=)
///   POST   api/Lab/reports/{id}/printed                     -> LabReportRowDto  (marks Printed)
///   GET    api/Lab/languages                                -> List&lt;LabLanguageDto&gt; (configured report languages; farmers get the farmer subset)
///   GET    api/Lab/financial-years                          -> List&lt;int&gt; (start years that have batches, newest first)
/// Lab Tracking (admin) reuses GET api/Lab/batches with batchId=&amp;consignmentId=&amp;stateId=&amp;regionId=&amp;stage= and
/// GET api/Lab/batches/{id} (its Batch View Details adds the six-step timeline: Consignment Delivered to Lab,
/// Lab Entry Created, Batch Created, Taken for Analysis, Analysis Completed, Report Generated).
/// Files: documents are stored under Uploads/Sas/lab/{batchId}/ and served by the existing
/// GET api/Sas/file/{*path}. Codes: BAT-SAS-{yyyy}-{001}, REP-SAS-{yyyy}-{001} (yyyy = calendar year).
/// Analysis days: whole days from TakenForAnalysisAt to AnalysisCompletedAt (or now); "Pending"
/// while null. Delayed: not complete and analysis days &gt; Sas:Lab:DelayedAfterDays (5).
/// </summary>
public enum LabConsignmentStatus { InTransit = 0, BatchPending = 1, BatchCreated = 2, Completed = 3 }
public enum LabOverallStatus { Good = 0, NeedsImprovement = 1, Poor = 2 }

// ---------------------------------------------------------------- me / users

public class LabMeDto
{
    public bool IsCoordinator { get; set; }   // holds LabDashboard / LabConsignments / LabAnalysis / LabReports (any) or admin
    public bool IsAnalyst { get; set; }       // holds LabTestEntry
    public bool CanWrite { get; set; }        // coordinator pages or admin (create batches, receive, documents, complete)
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? DesignationName { get; set; }
}

public class LabUserDto
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string? DesignationName { get; set; }
    public string? AvatarUrl { get; set; }     // null -> initial avatar
    public int OpenBatches { get; set; }       // batches assigned and not Completed
}

// ---------------------------------------------------------------- dashboards

public class LabDashboardDto
{
    public int TotalConsignmentsReceived { get; set; }
    public int PendingBatchCreation { get; set; }
    public int TotalBatchesCreated { get; set; }
    public int TakenForAnalysis { get; set; }
    public int AnalysisCompleted { get; set; }
    public int CompletedBatches { get; set; }
    public int ReportsGenerated { get; set; }
    public List<LabConsignmentRowDto> RecentConsignments { get; set; } = new();
    public List<LabBatchRowDto> RecentBatches { get; set; } = new();
}

public class LabAnalystDashboardDto
{
    public int AssignedBatches { get; set; }
    public int PendingValueEntry { get; set; }      // samples NotStarted in my open batches
    public int AutoResultReady { get; set; }        // samples Completed in my batches not yet Completed
    public int ReportsGeneratedThisFy { get; set; }
    public int PreviousFyReports { get; set; }
    public int PendingEntryBatches { get; set; }    // "Pending value entry batches"
    public int AutoResultReadyBatches { get; set; } // "Auto result ready for review"
    public int ReportsToDownload { get; set; }      // "Generated reports for download"
    public int? ContinueBatchId { get; set; }       // most recent InProgress batch for "Continue Test Entry"
}

// ---------------------------------------------------------------- consignments

public class LabConsignmentStatsDto
{
    public int TotalConsignments { get; set; }
    public int PendingBatchCreation { get; set; }
    public int BatchedConsignments { get; set; }
    public int WaterSamples { get; set; }
    public int SoilSamples { get; set; }
}

public class LabConsignmentRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public DateTime ShipmentDate { get; set; }              // DispatchedAt
    public DateTime? DeliveredAt { get; set; }
    public SamplePaymentType SampleType { get; set; }       // Paid when any collection is paid
    public int SoilSamples { get; set; }
    public int WaterSamples { get; set; }
    public int TotalSamples { get; set; }
    public string? StateName { get; set; }
    public string? RegionName { get; set; }
    public string? HeadquarterName { get; set; }
    public LabConsignmentStatus LabStatus { get; set; }
    public ConsignmentStatus Status { get; set; }           // v1 status
    public int? BatchId { get; set; }
    public string? BatchCode { get; set; }
    public string CourierService { get; set; } = "";
    public string TrackingNumber { get; set; } = "";
    public string DispatchedByName { get; set; } = "";
}

public class LabConsignmentDetailDto
{
    public LabConsignmentRowDto Summary { get; set; } = new();
    public ConsignmentDetailDto Consignment { get; set; } = new();   // v1 detail (collections, photos, timeline)
    public LabBatchRowDto? Batch { get; set; }
}

// ---------------------------------------------------------------- batches

public class LabBatchStatsDto
{
    public int TotalBatches { get; set; }
    public int Created { get; set; }
    public int TakenForAnalysis { get; set; }
    public int AnalysisCompleted { get; set; }
    public int Completed { get; set; }
    public int DelayedAnalysis { get; set; }
}

public class LabBatchCreateDto
{
    public List<int> ConsignmentIds { get; set; } = new();
    public DateTime? BatchDate { get; set; }               // default today
    public string? AssignedToUserId { get; set; }
    public SampleBatchPriority Priority { get; set; } = SampleBatchPriority.Medium;
    public string? Remarks { get; set; }
}

public class LabBatchAssignDto
{
    public string? AssignedToUserId { get; set; }
    public SampleBatchPriority? Priority { get; set; }
}

public class LabBatchRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public DateTime BatchDate { get; set; }
    public List<string> ConsignmentCodes { get; set; } = new();
    public string ConsignmentCode => ConsignmentCodes.Count == 0 ? "" : ConsignmentCodes.Count == 1 ? ConsignmentCodes[0] : $"{ConsignmentCodes[0]} +{ConsignmentCodes.Count - 1}";
    public int SampleCount { get; set; }
    public SamplePaymentType SampleType { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public string? AssignedToAvatarUrl { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? AssignedByName { get; set; }
    public SampleBatchPriority Priority { get; set; }
    public SampleBatchStatus Status { get; set; }
    public int? AnalysisDays { get; set; }                 // null = Pending
    public bool IsDelayed { get; set; }
    public int PendingEntries { get; set; }                // samples NotStarted + InProgress
    public int CompletedEntries { get; set; }              // samples Completed
    public int? ReportId { get; set; }
    public string? ReportCode { get; set; }
}

public class LabBatchStepDto
{
    public SampleBatchStatus Status { get; set; }
    public string Title { get; set; } = "";                // Taken for Analysis / In Progress / ...
    public string? Description { get; set; }
    public DateTime? At { get; set; }
    public string? ByName { get; set; }
    public bool IsDone { get; set; }
    public bool IsCurrent { get; set; }
}

public class LabSampleTypeSummaryDto
{
    public SampleType SampleType { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int NotStarted { get; set; }
}

public class LabBatchDetailDto
{
    public LabBatchRowDto Header { get; set; } = new();
    public string? Remarks { get; set; }
    public string? FinalRemarks { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? TakenForAnalysisAt { get; set; }
    public DateTime? AnalysisStartedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ParameterRowCount { get; set; }            // "Parameters (26)"
    public int DocumentCount { get; set; }
    public List<LabBatchStepDto> Progress { get; set; } = new();       // 4 steps
    public List<LabSampleTypeSummaryDto> SampleTypeSummary { get; set; } = new();
    public List<LabSampleRowDto> RecentSamples { get; set; } = new();  // 5
    public List<LabActivityDto> Timeline { get; set; } = new();        // status transitions only
    public List<LabConsignmentRowDto> Consignments { get; set; } = new();
}

// ---------------------------------------------------------------- samples and parameters

public class LabSampleStatsDto { public int Total { get; set; } public int NotStarted { get; set; } public int InProgress { get; set; } public int Completed { get; set; } }
public class LabParameterStatsDto { public int TotalParameters { get; set; } public int Completed { get; set; } public int InProgress { get; set; } public int NotStarted { get; set; } }

public class LabSampleRowDto
{
    public int SampleItemId { get; set; }
    public int CollectionId { get; set; }
    public string SampleId { get; set; } = "";              // SAS-SOIL-001 style display id (from Code + type)
    public string SampleCode { get; set; } = "";            // v1 item code
    public SampleType SampleType { get; set; }
    public SamplePaymentType PaymentType { get; set; }
    public string FarmerName { get; set; } = "";
    public string? Crop { get; set; }
    public string? Village { get; set; }
    public string? District { get; set; }
    public string? CurrentParameter { get; set; }           // last parameter with a value
    public SampleAnalysisStatus Status { get; set; }
    public bool ReportGenerated { get; set; }               // batch Completed
    public int ParameterCount { get; set; }
    public int EnteredCount { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime? AnalysisStartedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
}

public partial class LabParameterRowDto   // partial: entry-form fields appended at the end of this file (phase 1e)
{
    public int LabParameterId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Unit { get; set; }
    public string? NormalRange { get; set; }
    public string? ReportingLimit { get; set; }
    public string RecommendationGroup { get; set; } = "General";
    public string? EnteredValue { get; set; }
    public string? ResultLabel { get; set; }                // Neutral / Safe / Low / Medium / High
    public LabResultStatus? Status { get; set; }            // null until a value is entered
    public string? Hint { get; set; }
    public SampleAnalysisStatus RowStatus { get; set; }     // NotStarted / InProgress (draft) / Completed (submitted)
    public DateTime? EnteredAt { get; set; }
    public string? EnteredByName { get; set; }
}

/// <summary>One sample with its parameter rows (Parameters tab shows samples one by one).</summary>
public class LabSampleParametersDto
{
    public LabSampleRowDto Sample { get; set; } = new();
    public List<LabParameterRowDto> Parameters { get; set; } = new();
}

public class LabSampleValueDto { public int LabParameterId { get; set; } public string? Value { get; set; } }

public class LabSampleValuesDto
{
    public int SampleItemId { get; set; }
    public List<LabSampleValueDto> Values { get; set; } = new();
    public bool Submit { get; set; }                       // false = Save Draft, true = Submit Values (locks)
}

public class LabRecommendationGroupDto { public string Group { get; set; } = ""; public List<string> Lines { get; set; } = new(); }

public class LabAutoResultDto
{
    public List<LabParameterRowDto> Parameters { get; set; } = new();
    public LabOverallStatus OverallStatus { get; set; }
    public string OverallStatusText { get; set; } = "";     // Good / Needs Improvement / Poor
    public List<LabRecommendationGroupDto> Recommendations { get; set; } = new();
    public string? CropSuitabilityNote { get; set; }
}

public class LabSampleEntryDto
{
    public LabBatchRowDto Batch { get; set; } = new();
    public int PendingInBatch { get; set; }
    public int CompletedInBatch { get; set; }
    public List<LabSampleRowDto> SamplesInBatch { get; set; } = new();
    public LabSampleRowDto Sample { get; set; } = new();
    public string? FarmerMobile { get; set; }
    public DateTime CollectedOn { get; set; }
    public bool IsLocked { get; set; }                     // batch Completed: read-only
    public LabAutoResultDto Result { get; set; } = new();  // parameters with current values + summary
}

// ---------------------------------------------------------------- documents and activity

public class LabDocumentDto
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public LabDocumentKind Kind { get; set; }
    public string FileName { get; set; } = "";
    public string? Description { get; set; }
    public string Extension { get; set; } = "";            // PDF / XLSX ...
    public string Url { get; set; } = "";                  // api/Sas/file/{StoredPath}
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string? UploadedByName { get; set; }
    public string? UploadedByAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LabActivityDto
{
    public int Id { get; set; }
    public LabActivityKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ByName { get; set; }
    public string? ByRole { get; set; }
    public string? ByAvatarUrl { get; set; }
    public DateTime At { get; set; }
}

// ---------------------------------------------------------------- reports

public class LabReportStatsDto { public int TotalReports { get; set; } public int ThisFinancialYear { get; set; } public int PreviousFinancialYears { get; set; }
    // Added by the reports workstream (analyst Test Reports KPIs; scoped like the lists, financialYear= applies):
    public int BatchGroups { get; set; }          // batches in scope with at least one report (batch-wise report groups)
    public int SampleReports { get; set; }        // sample reports (LabReport rows)
    public int GeneratedToday { get; set; }       // sample reports generated today
    public int DownloadsCompleted { get; set; }   // PDF / Excel downloads (sum of DownloadCount)
}

/// <summary>One sample report (RPT-SAS-2026-001).</summary>
public class LabReportRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = "";
    public List<string> ConsignmentCodes { get; set; } = new();
    public string ConsignmentCode => ConsignmentCodes.Count == 0 ? "" : ConsignmentCodes.Count == 1 ? ConsignmentCodes[0] : $"{ConsignmentCodes[0]} +{ConsignmentCodes.Count - 1}";
    public int SampleItemId { get; set; }
    public string SampleCode { get; set; } = "";            // SMP-001 / v1 item code
    public string FarmerName { get; set; } = "";
    public string? FarmerAvatarUrl { get; set; }
    public string? Crop { get; set; }
    public string? Village { get; set; }
    public SampleType SampleType { get; set; }              // Soil or Water layout
    public SamplePaymentType PaymentType { get; set; }
    public string? StateName { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime GeneratedAt { get; set; }
    public LabReportStatus Status { get; set; }             // Generated / Downloaded / Printed
    public int DownloadCount { get; set; }
    public int FinancialYearStart { get; set; }
    public string SampleId { get; set; } = "";              // lab display id SAS-SOIL-001 (same as the batch's samples and the PDF's Lab Number)
}

public class LabReportDetailDto
{
    public LabReportRowDto Summary { get; set; } = new();
    // Batch Details
    public DateTime BatchCreatedAt { get; set; }
    public string? BatchCreatedByName { get; set; }
    public SampleBatchStatus BatchStatus { get; set; }
    // Consignment Details
    public DateTime ShipmentDate { get; set; }
    public string? RegionName { get; set; }
    public string? HeadquarterName { get; set; }
    // Analysis Details
    public DateTime? AssignedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
    public int? AnalysisDays { get; set; }
    public DateTime? FinalCompletedAt { get; set; }
    public string? FinalRemarks { get; set; }
    public List<LabBatchStepDto> Timeline { get; set; } = new();
    public LabSampleReportDto Sample { get; set; } = new();          // this sample's results, recommendations and note (drawer body and PDF)
    public List<string> Languages { get; set; } = new();              // download languages offered to the caller
}

public class LabLanguageDto { public string Code { get; set; } = "en"; public string Name { get; set; } = "English"; public string NativeName { get; set; } = "English"; }

/// <summary>Batch-wise report summary row (analyst Test Reports list; coordinator Lab Reports list).</summary>
public class LabReportBatchRowDto
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = "";
    public string? ReportCode { get; set; }                // REP-SAS-2026-001 once generated
    public List<string> ConsignmentCodes { get; set; } = new();
    public string ConsignmentCode => ConsignmentCodes.Count == 0 ? "" : ConsignmentCodes.Count == 1 ? ConsignmentCodes[0] : $"{ConsignmentCodes[0]} +{ConsignmentCodes.Count - 1}";
    public SamplePaymentType SampleType { get; set; }      // "Soil Type" column: Paid / Free
    public int SoilSamples { get; set; }
    public int WaterSamples { get; set; }
    public int TotalSamples { get; set; }
    public int ReportsGenerated { get; set; }
    public int PendingReports { get; set; }
    public int DownloadsCompleted { get; set; }
    public string? StateName { get; set; }
    public string? RegionName { get; set; }
    public string? HeadquarterName { get; set; }
    public DateTime BatchDate { get; set; }
    public DateTime? ReportGeneratedAt { get; set; }
    public SampleBatchStatus BatchStatus { get; set; }
    public int FinancialYearStart { get; set; }
    public string? AssignedToName { get; set; }
}

/// <summary>Coordinator "Lab Report Details" drawer and analyst / admin Batch Report Details page.</summary>
public class LabBatchReportDto
{
    public LabReportBatchRowDto Summary { get; set; } = new();
    public LabBatchRowDto Batch { get; set; } = new();
    public string? BatchCreatedByName { get; set; }
    public DateTime BatchCreatedAt { get; set; }
    public DateTime ShipmentDate { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
    public int? AnalysisDays { get; set; }
    public DateTime? FinalCompletedAt { get; set; }
    public string? FinalRemarks { get; set; }
    public List<LabBatchStepDto> Timeline { get; set; } = new();
    public List<LabReportRowDto> SampleReports { get; set; } = new();
}

public class LabSampleReportDto
{
    public LabSampleRowDto Sample { get; set; } = new();
    public string? FarmerMobile { get; set; }
    public List<LabParameterRowDto> Parameters { get; set; } = new();
    public LabOverallStatus OverallStatus { get; set; }
    public string OverallStatusText { get; set; } = "";
    public List<LabRecommendationGroupDto> Recommendations { get; set; } = new();
    public string? CropSuitabilityNote { get; set; }
}

// ---------------------------------------------------------------- entry-form fields (phase 1e, analyst Sample-wise Entry)

/// <summary>How the analyst enters a parameter row (from the LabParameter master; filled by LabController).</summary>
public partial class LabParameterRowDto
{
    public LabParameterValueType ValueType { get; set; } = LabParameterValueType.Numeric;   // Text rows (Texture) get a select
    public List<string> Options { get; set; } = new();                                     // choices of a Text row, in master order
    public bool IsDerived { get; set; }                                                    // computed (Organic Matter = Organic Carbon x factor); read-only
}
