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

public enum SamplePaymentStatus { Pending = 0, Approved = 1, Rejected = 2 }
public enum ConsignmentStatus { PendingPickup = 0, Dispatched = 1, InTransit = 2, Delivered = 3, Completed = 4 }
public enum ConsignmentPhotoKind { ShippingLabel = 0, Package = 1, Other = 2 }
public enum LabResultStatus { Normal = 0, Deficient = 1, Moderate = 2, Excess = 3 }

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
