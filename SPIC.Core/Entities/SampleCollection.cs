namespace SPIC.Core.Entities;

// ------------------------------------------------------------------------------------------------
// SAS Portal: Soil / Water Sample Collection, payment, consignment and lab results.
// Built 2026-09-21 from the product owner's Figma screens (docs/sas-sample-collection-plan.md).
// ------------------------------------------------------------------------------------------------

public enum SamplePaymentType { Free = 0, Paid = 1 }
public enum SamplePaidCategory { Farmer = 0, Ngo = 1 }
public enum SampleType { Soil = 0, Water = 1, SoilAndWater = 2 }

/// <summary>Lifecycle of one collection (a visit that produced one or more samples).</summary>
public enum SampleCollectionStatus
{
    Draft = 0,               // saved, not submitted
    Collected = 1,           // submitted (free: goes straight to ReadyForConsignment)
    PendingPayment = 2,      // paid sample, payment not yet recorded
    PendingApproval = 3,     // payment recorded, awaiting approval
    ReadyForConsignment = 4, // payment approved or free sample
    Dispatched = 5,          // in a consignment
    InTransit = 6,
    DeliveredToLab = 7,
    TestInProgress = 8,
    Completed = 9
}

public enum SamplePaymentStatus { Pending = 0, Approved = 1, Rejected = 2 }   // admin: Pending Review / Admin Approved / Returned to MO
public enum SamplePaymentMode { Upi = 0, BankTransfer = 1, Cash = 2 }
public enum SampleFinanceStatus { NotForwarded = 0, AwaitingVerification = 1, Verified = 2, Mismatch = 3 }
public enum ConsignmentStatus { PendingPickup = 0, Dispatched = 1, InTransit = 2, Delivered = 3, Completed = 4 }
public enum ConsignmentPhotoKind { ShippingLabel = 0, Package = 1, Other = 2 }
public enum LabResultStatus { Normal = 0, Deficient = 1, Moderate = 2, Excess = 3 }

// ---- Lab portal (version 2, 2026-09-27, docs/sas-lab-portal-plan.md) ----
// Analyst wording: TakenForAnalysis = "Pending Entry", InProgress, AnalysisCompleted = "Auto Result
// Ready" (every value entered, range-based results generated), Completed = "Report Generated".
public enum SampleBatchStatus { Created = 0, TakenForAnalysis = 1, InProgress = 2, AnalysisCompleted = 3, Completed = 4 }
public enum SampleBatchPriority { Low = 0, Medium = 1, High = 2 }
public enum SampleAnalysisStatus { NotStarted = 0, InProgress = 1, Completed = 2 }
public enum LabDocumentKind { Batch = 0, Sample = 1, Reference = 2 }
public enum LabActivityKind
{
    BatchCreated = 0, SampleReceived = 1, SampleLogged = 2, ParameterAssigned = 3, AnalysisStarted = 4,
    AnalysisInProgress = 5, DocumentUploaded = 6, StatusUpdated = 7, ResultEntered = 8, ReportGenerated = 9,
    DocumentDeleted = 10, Assigned = 11
}
public enum LabReportStatus { Generated = 0, Downloaded = 1, Printed = 2 }
public enum LabParameterValueType { Numeric = 0, Text = 1 }
public enum LabCropStage { Basal = 0, TopDressing1 = 1, TopDressing2 = 2, TopDressing3 = 3 }

/// <summary>A farmer whose land/water is sampled (created inline from the collection form).</summary>
public class SasFarmer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Village { get; set; }
    public string? Taluk { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public int? StateId { get; set; }
    public string? StateName { get; set; }
    public string? PinCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? SurveyNumber { get; set; }
    /// <summary>Identity user id when the farmer has a login (AppRole.Farmer); null otherwise.</summary>
    public string? UserId { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }
}

public class SampleCollection
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;          // SMP-2026-000001
    public DateTime CollectionDate { get; set; } = DateTime.Now;
    public string CollectedByUserId { get; set; } = string.Empty;
    public string CollectedByName { get; set; } = string.Empty;
    public string? CollectedByRole { get; set; }                // MDO / JMDO / ...
    public int? HeadquarterId { get; set; }                     // from the collector's claims, for location filters
    public string? LocationName { get; set; }                   // "Headquarter, State" at creation time
    public SamplePaymentType PaymentType { get; set; }
    public SamplePaidCategory? PaidCategory { get; set; }       // null for free samples
    public SampleCollectionStatus Status { get; set; } = SampleCollectionStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }
    public int? ConsignmentId { get; set; }
    public SampleConsignment? Consignment { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<SampleItem> Items { get; set; } = new List<SampleItem>();
    public ICollection<SamplePayment> Payments { get; set; } = new List<SamplePayment>();
}

public class SampleItem
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public SampleCollection? Collection { get; set; }
    public string Code { get; set; } = string.Empty;           // SMP-2026-000001-1
    public int FarmerId { get; set; }
    public SasFarmer? Farmer { get; set; }
    public SampleType SampleType { get; set; }
    public string? Crop1 { get; set; }
    public string? Crop2 { get; set; }
    public string? Remarks { get; set; }
    public int NoOfTests { get; set; } = 1;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }
    // Lab portal: per-sample analysis progress (values and results stay in SampleLabResult).
    public SampleAnalysisStatus AnalysisStatus { get; set; } = SampleAnalysisStatus.NotStarted;
    public DateTime? AnalysisStartedAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }

    public ICollection<SampleLabResult> Results { get; set; } = new List<SampleLabResult>();
}

public class SamplePayment
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public SampleCollection? Collection { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? BankGateway { get; set; }
    public string? UtrNumber { get; set; }
    public string PaidByName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? ProofPath { get; set; }                     // Sas/collections/{id}/payment_....jpg
    public decimal Amount { get; set; }
    public SamplePaymentStatus Status { get; set; } = SamplePaymentStatus.Pending;
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    // Payment Approval module (2026-09-27, docs/sas-lab-portal-plan.md): admin review then Finance verification.
    public string? Code { get; set; }                          // PAY-2026-00125 (assigned on creation from v2 on)
    public SamplePaymentMode PaymentMode { get; set; } = SamplePaymentMode.Upi;
    public DateTime? TransactionDate { get; set; }
    public string? BankName { get; set; }
    public string? MoRemarks { get; set; }
    public decimal? VerifiedAmount { get; set; }
    public string? AdminRemarks { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ForwardedToName { get; set; }               // "Finance Team"
    public DateTime? ForwardedAt { get; set; }
    public SampleFinanceStatus FinanceStatus { get; set; } = SampleFinanceStatus.NotForwarded;
    public DateTime? FinanceVerifiedAt { get; set; }
    public string? FinanceVerifiedByName { get; set; }
    public string? FinanceRemarks { get; set; }
    // V5p (2026-09-27): Finance's own figures. VerifiedAmount stays the admin's approved amount;
    // FinanceVerifiedAmount is what Finance confirmed as received (null when Finance marked the
    // payment failed through "mismatch"), FinanceReceivedDate the date Finance entered.
    public decimal? FinanceVerifiedAmount { get; set; }
    public DateTime? FinanceReceivedDate { get; set; }
}

public class SampleConsignment
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;           // CONS-2026-000001
    public string CourierService { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string? TrackingLink { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public int PackageCount { get; set; } = 1;
    public decimal? PackageWeightKg { get; set; }
    public string? PackagingType { get; set; }                 // Ice Box ...
    public string? ContactPerson { get; set; }
    public string? ContactMobile { get; set; }
    public string? ContactEmail { get; set; }
    public string? Notes { get; set; }
    public ConsignmentStatus Status { get; set; } = ConsignmentStatus.Dispatched;
    public DateTime DispatchedAt { get; set; } = DateTime.Now;
    public string DispatchedByUserId { get; set; } = string.Empty;
    public string DispatchedByName { get; set; } = string.Empty;
    public DateTime? DeliveredAt { get; set; }
    /// <summary>Lab portal: the batch this consignment was put into (at most one).</summary>
    public int? BatchId { get; set; }
    public SampleBatch? Batch { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }

    public ICollection<SampleCollection> Collections { get; set; } = new List<SampleCollection>();
    public ICollection<ConsignmentPhoto> Photos { get; set; } = new List<ConsignmentPhoto>();
}

public class ConsignmentPhoto
{
    public int Id { get; set; }
    public int ConsignmentId { get; set; }
    public SampleConsignment? Consignment { get; set; }
    public ConsignmentPhotoKind Kind { get; set; }
    public string? Title { get; set; }                         // Front View, Top View ...
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;    // Sas/consignments/{id}/...
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Timeline rows for the collection and consignment detail pages.</summary>
public class SasStatusEvent
{
    public int Id { get; set; }
    public int? CollectionId { get; set; }
    public int? ConsignmentId { get; set; }
    public string Status { get; set; } = string.Empty;         // enum name of the new status
    public string? Note { get; set; }
    public string? ByName { get; set; }
    public DateTime At { get; set; } = DateTime.Now;
}

/// <summary>One lab parameter for one sample (pH, EC, Organic Carbon, N, P, K, Zn ...).</summary>
public class SampleLabResult
{
    public int Id { get; set; }
    public int SampleItemId { get; set; }
    public SampleItem? SampleItem { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string? NormalRange { get; set; }
    public string? EnteredValue { get; set; }
    public string? Unit { get; set; }
    public string? ResultLabel { get; set; }                  // Neutral / Safe / Low / Medium / High
    public LabResultStatus Status { get; set; }
    public string? Hint { get; set; }                          // recommendation
    public int SortOrder { get; set; }
    /// <summary>Lab portal: the master parameter this row was created from (null for v1 rows).</summary>
    public int? LabParameterId { get; set; }
    public string? EnteredByName { get; set; }
    public DateTime EnteredAt { get; set; } = DateTime.Now;
}

/// <summary>Price per sample by type and paid category (admin-editable master, seeded).</summary>
public class SasSampleCharge
{
    public int Id { get; set; }
    public SampleType SampleType { get; set; }
    public SamplePaidCategory Category { get; set; }
    public decimal AmountPerSample { get; set; }
    public int NoOfTests { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class SasCourier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Optional, e.g. https://www.bluedart.com/tracking?awb={0}</summary>
    public string? TrackingUrlTemplate { get; set; }
    public bool IsActive { get; set; } = true;
}

// ------------------------------------------------------------------------------------------------
// Lab portal (version 2): batches of consignments, parameter master, documents, activity, reports.
// ------------------------------------------------------------------------------------------------

/// <summary>A batch groups one or more delivered consignments for analysis by one analyst.</summary>
public class SampleBatch
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;           // BAT-SAS-2026-001
    public DateTime BatchDate { get; set; } = DateTime.Today;
    public int SampleCount { get; set; }                        // sum over its consignments
    public SampleBatchPriority Priority { get; set; } = SampleBatchPriority.Medium;
    public string? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? AssignedByName { get; set; }
    public string? Remarks { get; set; }
    public SampleBatchStatus Status { get; set; } = SampleBatchStatus.Created;
    public DateTime? TakenForAnalysisAt { get; set; }
    public DateTime? AnalysisStartedAt { get; set; }            // first value entered
    public DateTime? AnalysisCompletedAt { get; set; }          // every sample completed (auto results ready)
    public DateTime? CompletedAt { get; set; }                  // report generated
    public string? FinalRemarks { get; set; }
    public string? ReportCode { get; set; }                    // REP-SAS-2026-001 (batch-level summary)
    public DateTime? ReportGeneratedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }

    public ICollection<SampleConsignment> Consignments { get; set; } = new List<SampleConsignment>();
    public ICollection<LabDocument> Documents { get; set; } = new List<LabDocument>();
    public ICollection<LabActivity> Activities { get; set; } = new List<LabActivity>();
    public ICollection<LabReport> Reports { get; set; } = new List<LabReport>();   // one per sample
}

/// <summary>Parameter master (seeded; admin-editable later). AppliesTo SoilAndWater = both.</summary>
public class LabParameter
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;           // N-001, WTDS-001 ...
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? NormalRange { get; set; }                   // "6.5 - 7.5": drives the auto result
    public decimal? RangeMin { get; set; }
    public decimal? RangeMax { get; set; }
    public string? ReportingLimit { get; set; }
    public SampleType AppliesTo { get; set; } = SampleType.SoilAndWater;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Text parameters (Texture) offer Options ("Sandy|Clay|Loam|Sandy Clay Silt") and get no auto status.</summary>
    public LabParameterValueType ValueType { get; set; } = LabParameterValueType.Numeric;
    public string? Options { get; set; }
    /// <summary>Derived value: DerivedFromCode x DerivedFactor (Organic Matter = Organic Carbon x 1.724); not entered.</summary>
    public string? DerivedFromCode { get; set; }
    public decimal? DerivedFactor { get; set; }
    // Auto-result rules (analyst enters only the value; see docs/sas-lab-portal-plan.md):
    // below RangeMin -> LowLabel / Deficient / LowHint; inside -> NormalLabel / Normal / NormalHint;
    // above RangeMax -> HighLabel / Excess / HighHint. Moderate is used when a parameter sets
    // ModerateFrom (values between RangeMin and ModerateFrom read "Medium" / Moderate).
    public string LowLabel { get; set; } = "Low";              // pH: Acidic, EC: Safe ...
    public string NormalLabel { get; set; } = "Medium";        // pH: Neutral, EC: Safe
    public string HighLabel { get; set; } = "High";            // pH: Alkaline, EC: Saline
    public decimal? ModerateFrom { get; set; }
    public string? LowHint { get; set; }
    public string? NormalHint { get; set; }
    public string? HighHint { get; set; }
    /// <summary>Recommendation group on the preview: Fertilizer, Organic, Micronutrient, General.</summary>
    public string RecommendationGroup { get; set; } = "General";
}

public class LabDocument
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public SampleBatch? Batch { get; set; }
    public LabDocumentKind Kind { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StoredPath { get; set; } = string.Empty;    // Sas/lab/{batchId}/...
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? UploadedByUserId { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; }
}

/// <summary>Activity Log / timeline rows of a batch, written by the API on every action.</summary>
public class LabActivity
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public SampleBatch? Batch { get; set; }
    public LabActivityKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;          // "Batch Created"
    public string? Description { get; set; }                  // "Batch BAT-SAS-2026-001 has been created"
    public string? ByUserId { get; set; }
    public string? ByName { get; set; }
    public string? ByRole { get; set; }                        // "Lab Coordinator" / designation name
    public DateTime At { get; set; } = DateTime.Now;
}

/// <summary>One report per SAMPLE, created when the batch is completed (PDF / Excel rendered on demand).</summary>
public class LabReport
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;           // RPT-SAS-2026-001
    public int BatchId { get; set; }
    public SampleBatch? Batch { get; set; }
    public int SampleItemId { get; set; }
    public SampleItem? SampleItem { get; set; }
    public SampleType SampleType { get; set; }                 // Soil or Water layout (SoilAndWater -> two reports)
    public LabReportStatus Status { get; set; } = LabReportStatus.Generated;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string? GeneratedByName { get; set; }
    public int DownloadCount { get; set; }
    public DateTime? LastDownloadedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public int FinancialYearStart { get; set; }                // 2026 for FY 2026-27 (April to March)
}

/// <summary>Fertilizer schedule master printed on the soil report (admin-editable; seeded with the Banana example).</summary>
public class LabCropRecommendation
{
    public int Id { get; set; }
    public string Crop { get; set; } = string.Empty;
    public LabCropStage Stage { get; set; }
    public int? DayNumber { get; set; }                        // 90 / 150 / 210 for top dressings
    public string Product { get; set; } = string.Empty;        // SPIC Jyoti, SPIC Gypsum, SPIC DAP ...
    public decimal KgPerAcre { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Report labels per language (Key + Lang unique). Missing keys fall back to English.</summary>
public class LabTranslation
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;            // report.title.soil, param.S-PH.name, response.Low ...
    public string Lang { get; set; } = "en";                    // en, ta, te, kn, ml, hi, mr
    public string Text { get; set; } = string.Empty;
}
