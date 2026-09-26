using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// SAS Sample Collection API contracts (shared by SpicAPI and the Blazor client).
/// Controller "Sas", all [Authorize]. WRITE roles (create/submit/pay/consign): MDO, JMDO, Admin,
/// CorporateAdmin. REVIEW roles (approve payments, consignment status, lab results): Admin,
/// CorporateAdmin. Everyone else with the SampleCollection / ConsignmentHistory page reads only;
/// Dealer and Farmer see status and results (Farmer: only collections whose farmer UserId is theirs).
///
///   GET    api/Sas/lookups                                  -> SasLookupsDto
///   GET    api/Sas/farmers?q=&page=&pageSize=               -> PageResult&lt;SasFarmerDto&gt; (name/mobile search)
///   GET    api/Sas/farmers/{id}                             -> SasFarmerDto
///   POST   api/Sas/farmers                                  -> SasFarmerDto (body: SasFarmerUpsertDto)
///   PUT    api/Sas/farmers/{id}                             -> SasFarmerDto
///   PATCH  api/Sas/farmers/{id}/link?userId=                -> SasFarmerDto (review roles; empty userId unlinks)
///   GET    api/Sas/collections/stats                        -> SampleCollectionStatsDto
///   GET    api/Sas/collections?status=&paymentType=&paymentStatus=&location=&from=&to=&q=&sort=&page=&pageSize=
///                                                           -> PageResult&lt;SampleCollectionSummaryDto&gt;
///   GET    api/Sas/collections/ready?paymentType=free|paid|both -> List&lt;SampleCollectionSummaryDto&gt; (ReadyForConsignment, mine)
///   GET    api/Sas/collections/{id}                         -> SampleCollectionDetailDto
///   POST   api/Sas/collections                              -> SampleCollectionDetailDto (body: SampleCollectionUpsertDto; Draft or submitted)
///   PUT    api/Sas/collections/{id}                         -> SampleCollectionDetailDto (Draft only)
///   DELETE api/Sas/collections/{id}                         (Draft only, soft)
///   POST   api/Sas/collections/{id}/submit                  -> SampleCollectionDetailDto (Free -> ReadyForConsignment, Paid -> PendingPayment)
///   POST   api/Sas/collections/{id}/payment                 -> SamplePaymentDto (body: SamplePaymentUpsertDto; status PendingApproval)
///   POST   api/Sas/collections/{id}/payment/proof           (multipart "file", image <= 5 MB) -> SasFileDto
///   PATCH  api/Sas/payments/{id}/status?status=Approved|Rejected&reason= -> SamplePaymentDto (review roles)
///   GET    api/Sas/consignments/stats                       -> ConsignmentStatsDto
///   GET    api/Sas/consignments?status=&courier=&paymentType=&from=&to=&q=&page=&pageSize= -> PageResult&lt;ConsignmentSummaryDto&gt;
///   GET    api/Sas/consignments/{id}                        -> ConsignmentDetailDto
///   POST   api/Sas/consignments                             -> ConsignmentDetailDto (body: ConsignmentUpsertDto; collections -> Dispatched)
///   POST   api/Sas/consignments/{id}/photos?kind=ShippingLabel|Package|Other&title= (multipart "files", images <= 5 MB) -> List&lt;ConsignmentPhotoDto&gt;
///   DELETE api/Sas/consignments/photos/{photoId}
///   PATCH  api/Sas/consignments/{id}/status?status=InTransit|Delivered|Completed -> ConsignmentDetailDto (review roles; cascades to collections)
///   GET    api/Sas/samples/{itemId}/results                 -> List&lt;LabResultDto&gt;
///   PUT    api/Sas/samples/{itemId}/results                 -> List&lt;LabResultDto&gt; (review roles; body: List&lt;LabResultUpsertDto&gt;; sets TestInProgress, or Completed when every item of the collection has results)
///   GET    api/Sas/file/{*path}                             (also accepts ?access_token=)
/// SasFarmer.UserId links a farmer record to a Farmer login and is what the Farmer read
/// scoping matches on; only review roles may set it (on create, update or the link route).
/// Codes: SMP-{yyyy}-{000001} per collection, "{code}-{n}" per sample, CONS-{yyyy}-{000001}.
/// Amounts: Free -> 0; Paid -> sum of SasSampleCharge(sampleType, paidCategory).AmountPerSample per item.
/// Enums serialize as integers (no string converter in the API).
/// </summary>
public class SasLookupsDto
{
    public List<SasCourierDto> Couriers { get; set; } = new();
    public List<SasChargeDto> Charges { get; set; } = new();
    public List<string> Crops { get; set; } = new();
    public List<SasStateDto> States { get; set; } = new();
    public List<string> PackagingTypes { get; set; } = new();
    public string UpiId { get; set; } = "";
    public string MerchantName { get; set; } = "";
    /// <summary>Parameters the lab result form shows by default (pH, EC, ...).</summary>
    public List<LabParameterDto> LabParameters { get; set; } = new();
}

public class SasCourierDto { public int Id { get; set; } public string Name { get; set; } = ""; public string? TrackingUrlTemplate { get; set; } }
public class SasChargeDto { public SampleType SampleType { get; set; } public SamplePaidCategory Category { get; set; } public decimal AmountPerSample { get; set; } public int NoOfTests { get; set; } }
public class SasStateDto { public int Id { get; set; } public string Name { get; set; } = ""; public List<SasDistrictDto> Districts { get; set; } = new(); }
public class SasDistrictDto { public int Id { get; set; } public string Name { get; set; } = ""; }
public class LabParameterDto { public string Parameter { get; set; } = ""; public string NormalRange { get; set; } = ""; public string Unit { get; set; } = ""; }

// ---------------------------------------------------------------- farmers

public class SasFarmerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Mobile { get; set; } = "";
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
    public string Location => string.Join(", ", new[] { Village, DistrictName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    /// <summary>Identity user id when the farmer has a login; null otherwise.</summary>
    public string? UserId { get; set; }
}

public class SasFarmerUpsertDto
{
    public string Name { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Village { get; set; }
    public string? Taluk { get; set; }
    public int? DistrictId { get; set; }
    public int? StateId { get; set; }
    public string? PinCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? SurveyNumber { get; set; }
    /// <summary>Identity user id of the farmer's login. Review roles only; ignored for everyone else.</summary>
    public string? UserId { get; set; }
}

// ---------------------------------------------------------------- collections

public class SampleCollectionStatsDto
{
    public int Total { get; set; }
    public int Free { get; set; }
    public int Paid { get; set; }
    /// <summary>Collections at the lab (DeliveredToLab or TestInProgress).</summary>
    public int LabLevel { get; set; }
    public int ReadyToSubmit { get; set; }     // ReadyForConsignment
    public int Completed { get; set; }
    public int PendingPayment { get; set; }
    public int PendingApproval { get; set; }
    public int ApprovedPayment { get; set; }   // = ReadyToSubmit (kept for the card label)
    public int Draft { get; set; }
}

public class SampleCollectionSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public DateTime CollectionDate { get; set; }
    public string CollectedByName { get; set; } = "";
    public string? LocationName { get; set; }
    public SamplePaymentType PaymentType { get; set; }
    public SamplePaidCategory? PaidCategory { get; set; }
    public SampleCollectionStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int SampleCount { get; set; }
    /// <summary>First farmer's name (list column) and the distinct sample types, e.g. "Soil, Water".</summary>
    public string FarmerName { get; set; } = "";
    public string SampleTypes { get; set; } = "";
    public SamplePaymentStatus? PaymentStatus { get; set; }
    public DateTime? PaymentReviewedAt { get; set; }
    public int? ConsignmentId { get; set; }
    public string? ConsignmentCode { get; set; }
    public bool IsMine { get; set; }
}

public class SampleCollectionDetailDto : SampleCollectionSummaryDto
{
    public string? Remarks { get; set; }
    public string? CollectedByRole { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<SampleItemDto> Items { get; set; } = new();
    public SamplePaymentDto? Payment { get; set; }
    public ConsignmentSummaryDto? Consignment { get; set; }
    public List<SasStatusEventDto> Timeline { get; set; } = new();
    /// <summary>What the caller may do (server-side truth for the buttons).</summary>
    public bool CanEdit { get; set; }
    public bool CanPay { get; set; }
    public bool CanConsign { get; set; }
    public bool CanReview { get; set; }
}

public class SampleItemDto
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string Code { get; set; } = "";
    public SasFarmerDto Farmer { get; set; } = new();
    public SampleType SampleType { get; set; }
    public string? Crop1 { get; set; }
    public string? Crop2 { get; set; }
    public string? Remarks { get; set; }
    public int NoOfTests { get; set; }
    public decimal Amount { get; set; }
    public bool HasResults { get; set; }
    public List<LabResultDto> Results { get; set; } = new();
}

public class SampleCollectionUpsertDto
{
    public DateTime CollectionDate { get; set; } = DateTime.Now;
    public SamplePaymentType PaymentType { get; set; }
    /// <summary>Required when PaymentType is Paid; ignored (null) for Free.</summary>
    public SamplePaidCategory? PaidCategory { get; set; }
    public string? Remarks { get; set; }
    public List<SampleItemUpsertDto> Items { get; set; } = new();
    /// <summary>true = keep as Draft; false = submit immediately (same as POST .../submit).</summary>
    public bool SaveAsDraft { get; set; } = true;
}

public class SampleItemUpsertDto
{
    public int? Id { get; set; }              // existing item when editing a draft
    public int FarmerId { get; set; }
    public SampleType SampleType { get; set; }
    public string? Crop1 { get; set; }
    public string? Crop2 { get; set; }
    public string? Remarks { get; set; }
}

// ---------------------------------------------------------------- payments

public class SamplePaymentDto
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string TransactionId { get; set; } = "";
    public string? BankGateway { get; set; }
    public string? UtrNumber { get; set; }
    public string PaidByName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? ProofPath { get; set; }
    public decimal Amount { get; set; }
    public SamplePaymentStatus Status { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SamplePaymentUpsertDto
{
    public string TransactionId { get; set; } = "";
    public string? BankGateway { get; set; }
    public string? UtrNumber { get; set; }
    public string PaidByName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    // Payment Approval module (2026-09-27): MO-entered details shown to the admin and Finance.
    public SamplePaymentMode PaymentMode { get; set; } = SamplePaymentMode.Upi;
    public DateTime? TransactionDate { get; set; }
    public string? BankName { get; set; }
    public string? MoRemarks { get; set; }
}

public class SasFileDto { public string Path { get; set; } = ""; public string FileName { get; set; } = ""; public long Size { get; set; } public string ContentType { get; set; } = ""; }

// ---------------------------------------------------------------- consignments

public class ConsignmentStatsDto
{
    public int Total { get; set; }
    public int WithMultipleCollections { get; set; }
    public int TotalSent { get; set; }        // = Total (label "Total Consignment Sent")
    public int TotalSamples { get; set; }
    public int Delivered { get; set; }
    public int Pending { get; set; }          // not yet delivered
}

public class ConsignmentSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string CourierService { get; set; } = "";
    public string TrackingNumber { get; set; } = "";
    public string? TrackingLink { get; set; }
    public DateTime DispatchedAt { get; set; }
    public string DispatchedByName { get; set; } = "";
    public ConsignmentStatus Status { get; set; }
    public int CollectionCount { get; set; }
    public int SampleCount { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Payment status across the bundled collections: Approved when all paid ones are approved.</summary>
    public SamplePaymentStatus? PaymentStatus { get; set; }
    public DateTime? PaymentReviewedAt { get; set; }
    public string? TransactionIds { get; set; }   // comma separated, for the list column
    public int PackageCount { get; set; }
    public List<ConsignmentSampleLineDto> SampleLines { get; set; } = new();   // per sample type: count / amount / status (expandable row)
}

public class ConsignmentSampleLineDto
{
    public SampleType SampleType { get; set; }
    public int SampleCount { get; set; }
    public int NoOfTests { get; set; }
    public string SampleCodeRange { get; set; } = "";
    public decimal Amount { get; set; }
    public SampleCollectionStatus Status { get; set; }
}

public class ConsignmentDetailDto : ConsignmentSummaryDto
{
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal? PackageWeightKg { get; set; }
    public string? PackagingType { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactMobile { get; set; }
    public string? ContactEmail { get; set; }
    public string? Notes { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public List<ConsignmentPhotoDto> Photos { get; set; } = new();
    public List<SampleCollectionSummaryDto> Collections { get; set; } = new();
    public List<SampleItemDto> Samples { get; set; } = new();
    public List<SasStatusEventDto> Timeline { get; set; } = new();
    public bool CanReview { get; set; }
}

public class ConsignmentUpsertDto
{
    public List<int> CollectionIds { get; set; } = new();
    public string CourierService { get; set; } = "";
    public string TrackingNumber { get; set; } = "";
    public string? TrackingLink { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public int PackageCount { get; set; } = 1;
    public decimal? PackageWeightKg { get; set; }
    public string? PackagingType { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactMobile { get; set; }
    public string? ContactEmail { get; set; }
    public string? Notes { get; set; }
}

public class ConsignmentPhotoDto
{
    public int Id { get; set; }
    public ConsignmentPhotoKind Kind { get; set; }
    public string? Title { get; set; }
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SasStatusEventDto
{
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public string? ByName { get; set; }
    public DateTime At { get; set; }
}

// ---------------------------------------------------------------- lab results

public class LabResultDto
{
    public int Id { get; set; }
    public string Parameter { get; set; } = "";
    public string? NormalRange { get; set; }
    public string? EnteredValue { get; set; }
    public string? Unit { get; set; }
    public string? ResultLabel { get; set; }
    public LabResultStatus Status { get; set; }
    public string? Hint { get; set; }
    public int SortOrder { get; set; }
    public string? EnteredByName { get; set; }
    public DateTime EnteredAt { get; set; }
}

public class LabResultUpsertDto
{
    public string Parameter { get; set; } = "";
    public string? NormalRange { get; set; }
    public string? EnteredValue { get; set; }
    public string? Unit { get; set; }
    public string? ResultLabel { get; set; }
    public LabResultStatus Status { get; set; }
    public string? Hint { get; set; }
    public int SortOrder { get; set; }
}
