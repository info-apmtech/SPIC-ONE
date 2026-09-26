using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// SAS Payment Approval / Verification API contracts (2026-09-27; docs/sas-lab-portal-plan.md,
/// screens 23-30 and 34-35). Lives in the existing "Sas" controller (region "payment approval").
/// Modes are resolved on the server from the caller's pages, like LibraryController does:
///   ADMIN   : SasPaymentApproval (or Admin / CorporateAdmin)  -> review, approve &amp; forward, reject
///   FINANCE : SasPaymentVerification                            -> verify, mismatch
///   FARMER  : role Farmer                                       -> own samples only, read-only
///   MO/JMDO : v1 write roles                                    -> own submissions, read-only here
/// Every list is paged (PageResult&lt;T&gt;, default 16, max 50); enums serialize as integers.
///
///   GET    api/Sas/payments/me                                 -> SasPaymentModeDto (which mode the caller gets)
///   GET    api/Sas/payments/stats                              -> SasPaymentStatsDto (all KPIs; the page shows the ones for its mode)
///   GET    api/Sas/payments?tab=&amp;stateId=&amp;regionId=&amp;mode=&amp;adminStatus=&amp;financeStatus=&amp;crop=&amp;q=&amp;from=&amp;to=&amp;page=&amp;pageSize=
///                                                              -> PageResult&lt;SasPaymentRowDto&gt;
///                                                              tab = all | pendingAdmin | forwarded | returned | financeStatus | pendingVerification | verified | failed | mismatch | history
///   GET    api/Sas/payments/{id}                               -> SasPaymentDetailDto
///   POST   api/Sas/payments/{id}/approve                       -> SasPaymentDetailDto (admin; body: SasPaymentApproveDto; Status Approved, FinanceStatus AwaitingVerification, ForwardedAt now)
///   POST   api/Sas/payments/{id}/reject                        -> SasPaymentDetailDto (admin; body: SasPaymentRejectDto; Status Rejected = Returned to MO; collection back to PendingPayment)
///   POST   api/Sas/payments/{id}/verify                        -> SasPaymentDetailDto (finance; body: SasPaymentVerifyDto; VerifiedAmount &lt; Amount -> Mismatch, else Verified)
///   POST   api/Sas/payments/{id}/mismatch                      -> SasPaymentDetailDto (finance; body: SasPaymentRejectDto; FinanceStatus Mismatch)
///   GET    api/Sas/payments/{id}/proof                         -> the receipt image (also ?access_token=; same file as api/Sas/file/{ProofPath})
/// The v1 PATCH api/Sas/payments/{id}/status keeps working and maps to approve / reject.
/// Codes: PAY-{yyyy}-{00001}; v1 payments without a code get one lazily when first listed.
/// </summary>
public enum SasPaymentPageMode { Admin = 0, Finance = 1, Farmer = 2, ReadOnly = 3 }

public class SasPaymentModeDto
{
    public SasPaymentPageMode Mode { get; set; }
    public bool CanApprove { get; set; }
    public bool CanVerify { get; set; }
}

public class SasPaymentStatsDto
{
    // admin
    public int PendingAdminApproval { get; set; }
    public int ForwardedToFinance { get; set; }
    public int ReturnedOrRejected { get; set; }
    public int FinanceVerified { get; set; }
    // finance
    public int AllPayments { get; set; }
    public int PendingVerification { get; set; }
    public int VerifiedPayments { get; set; }
    public int FailedOrMismatch { get; set; }
    public int Mismatch { get; set; }
    public int Failed { get; set; }
    // farmer
    public int TotalPaidSamples { get; set; }
    public int ApprovalPending { get; set; }
    public int PaymentIssues { get; set; }
}

public class SasPaymentRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";                     // PAY-2026-00124
    public int CollectionId { get; set; }
    public string CollectionCode { get; set; } = "";           // SMP-2026-000157
    public int? ConsignmentId { get; set; }
    public string? ConsignmentCode { get; set; }
    public string SubmittedByName { get; set; } = "";          // MO
    public string? SubmittedByRole { get; set; }
    public string? SubmittedByAvatarUrl { get; set; }
    public string FarmerName { get; set; } = "";               // first farmer of the collection
    public string? FarmerAvatarUrl { get; set; }
    public string? Crop { get; set; }
    public SamplePaymentMode PaymentMode { get; set; }
    public string TransactionId { get; set; } = "";
    public decimal ExpectedAmount { get; set; }                // collection TotalAmount
    public decimal PaidAmount { get; set; }                    // payment Amount
    public decimal Difference => PaidAmount - ExpectedAmount;
    public SamplePaymentStatus AdminStatus { get; set; }
    public SampleFinanceStatus FinanceStatus { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? FinanceVerifiedByName { get; set; }
    public DateTime? FinanceVerifiedAt { get; set; }
    public string? FinanceRemarks { get; set; }
    public string? StateName { get; set; }
    public string? RegionName { get; set; }
}

public class SasPaymentTimelineStepDto
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? At { get; set; }
    public string? ByName { get; set; }
    public bool IsDone { get; set; }
    public bool IsCurrent { get; set; }
}

public class SasPaymentDetailDto
{
    public SasPaymentRowDto Summary { get; set; } = new();
    // farmer details
    public string? FarmerMobile { get; set; }
    public string? FarmerDistrict { get; set; }
    public string? FarmerVillage { get; set; }
    public string? FarmerAddress { get; set; }
    public string? HeadquarterName { get; set; }
    // sample details
    public int TotalSamples { get; set; }
    public int SoilSamples { get; set; }
    public int WaterSamples { get; set; }
    public decimal RatePerSample { get; set; }
    public string CollectedByName { get; set; } = "";
    public string? CollectedByRole { get; set; }
    public DateTime CollectionDate { get; set; }
    public DateTime? ConsignmentCreatedAt { get; set; }
    public string? TrackingNumber { get; set; }
    // payment details
    public DateTime? TransactionDate { get; set; }
    public string? BankName { get; set; }
    public string? UtrNumber { get; set; }
    public string? MoRemarks { get; set; }
    public string? ProofUrl { get; set; }                      // api/Sas/payments/{id}/proof
    public string? ProofContentType { get; set; }
    // admin review
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminRemarks { get; set; }
    public string? RejectReason { get; set; }
    public decimal? VerifiedAmount { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ForwardedToName { get; set; }
    public DateTime? ForwardedAt { get; set; }
    // finance
    public DateTime? FinanceReceivedDate { get; set; }
    public List<SasPaymentTimelineStepDto> Timeline { get; set; } = new();
    public SasPaymentPageMode Mode { get; set; }
    public bool CanApprove { get; set; }
    public bool CanVerify { get; set; }
}

public class SasPaymentApproveDto
{
    public decimal VerifiedAmount { get; set; }
    public string ForwardTo { get; set; } = "Finance Team";
    public DateTime? ApprovedDate { get; set; }
    public string? Remarks { get; set; }
    public bool Confirmed { get; set; }                        // the checkbox; must be true
}

public class SasPaymentRejectDto { public string Reason { get; set; } = ""; }

public class SasPaymentVerifyDto
{
    public decimal VerifiedAmount { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Remarks { get; set; }
    public bool Confirmed { get; set; }
}
