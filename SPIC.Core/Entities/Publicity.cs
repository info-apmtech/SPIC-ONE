using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.Entities
{
    // Publicity Material Tracking (2026-11-08, docs/publicity-tracking-plan.md).
    // One request (indent) for one material of one product, approved RM -> SMD -> SMM ->
    // ADMIN-MD CS -> DGM-MD CS, then shipped in one or more dispatches (original vendor dispatch,
    // re-initiation from the vendor, or an inter-HQ stock transfer) that the RMDO acknowledges.
    // Everything that is a number on the screens (shortage, excess, usable, outstanding) is
    // computed from the dispatch + acknowledgement rows, never stored twice.

    public enum PublicityStage
    {
        IndentCreation = 0, RmReview = 1, SmdReview = 2, SmmReview = 3, AdminMdCsReview = 4, DgmMdCsReview = 5,
        Dispatch = 6, Delivery = 7, Acknowledgement = 8, ExceptionHandling = 9, ReInitiation = 10,
        InterHqTransfer = 11, TransferCreation = 12, Completed = 13
    }

    public enum PublicityStatus
    {
        Draft = 0, PendingApproval = 1, PendingFinalApproval = 2, FinalApproved = 3, InDispatch = 4,
        Delivered = 5, AcknowledgementPending = 6, ExceptionRaised = 7, ReInitiationInProgress = 8,
        TransferInProgress = 9, ExceptionResolved = 10, Completed = 11, Rejected = 12, TransferOrderReceived = 13, Returned = 14
    }

    public enum PublicityDispatchKind { Original = 0, ReInitiation = 1, Transfer = 2 }

    public enum PublicityDispatchStatus
    {
        TransferRequested = 0, ProofSubmitted = 1, Dispatched = 2, InTransit = 3, Delivered = 4, Acknowledged = 5
    }

    public enum PublicityFileKind
    {
        Design = 0, DispatchDocument = 1, TransportSlip = 2, EWayBill = 3, AckPhoto = 4, CourierProof = 5, Other = 6, NoteApproval = 7
    }

    public enum PublicityNotificationType { StockTransfer = 0, Exception = 1, Approval = 2, Dispatch = 3, General = 4 }

    // A Region as a whole is signed off by its state-level approver (SMD today) independently of
    // any single request's own Stage/Status - neither enum above has a "this whole region is done"
    // member, and none should: Stage/Status are per-REQUEST concepts. ZeroConfirmed exists
    // specifically for a Region that genuinely has no eligible requests this round - see
    // PublicityController.ConfirmZeroRegion's own doc comment for why this can never be inferred
    // automatically from "no rows appeared".
    // RmZeroSubmitted is when the Region's RM explicitly submitted Zero Requirement, awaiting SMD finalization.
    public enum PublicityRegionFinalizationStatus { Approved = 0, ZeroConfirmed = 1, RmZeroSubmitted = 2 }

    public class PublicityMaterialType
    {
        public int Id { get; set; }
        [MaxLength(100)] public string Name { get; set; } = "";
        [MaxLength(30)] public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class PublicityRequest
    {
        public int Id { get; set; }
        [MaxLength(30)] public string IndentNo { get; set; } = "";
        public int MaterialTypeId { get; set; }
        public PublicityMaterialType? MaterialType { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int OriginalQuantity { get; set; }
        [MaxLength(100)] public string? RequestedByUserId { get; set; }
        [MaxLength(200)] public string? RequestedByName { get; set; }
        public int StateId { get; set; }
        public int RegionId { get; set; }
        public int HeadquarterId { get; set; }
        public string? Remarks { get; set; }
        public PublicityStage Stage { get; set; }
        public PublicityStatus Status { get; set; }
        [MaxLength(100)] public string CreatedByUserId { get; set; } = "";
        [MaxLength(200)] public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        // Request-level review sign-off at the SMD stage - distinct from Region-level approval
        // (PublicityRegionFinalization below): a Region can only be approved once every one of its
        // currently-eligible requests has this set. Cleared automatically whenever the request is
        // edited afterward (see PublicityController.ReviewOneAsync's "edit" branch), so a changed
        // request always requires a fresh look before the Region can be (re-)approved.
        public DateTime? SmdReviewedAt { get; set; }
        [MaxLength(200)] public string? SmdReviewedByName { get; set; }
        public List<PublicityDispatch> Dispatches { get; set; } = new();
        public List<PublicityEvent> Events { get; set; } = new();
    }

    /// <summary>One Region's sign-off for its state-level approver's review pass - either every
    /// currently-eligible request in the Region has been reviewed and the Region itself approved as
    /// a block, or the Region has been explicitly confirmed to have zero requirements this round.
    /// One row per (StateId, RegionId) - re-finalizing overwrites the previous row (see
    /// PublicityController's ApproveRegion/ConfirmZeroRegion), so this always reflects the CURRENT
    /// finalization, not a history of every past one; PublicityEvent already owns per-request audit
    /// history, this is deliberately not a duplicate of it.
    ///
    /// Staleness (a Region finalized earlier, then new/unreviewed work appeared for it since) is
    /// never computed from this row directly - PublicityController's Consolidation() always
    /// re-derives "is this Region's finalization still accurate" from live request data (any
    /// currently-eligible request with SmdReviewedAt == null, or anything still short of the SMD
    /// stage, invalidates it) every time the page loads. There is no "cycle"/"batch" concept
    /// anywhere in this module (confirmed - no such field exists on PublicityRequest), so this
    /// row's validity is always judged against present-moment data, not a closed period.
    ///
    /// SubmittedAt marks a DIFFERENT thing from staleness: once PublicityController.ReviewBulk
    /// successfully forwards this Region's work to SMM, every one of its once-eligible requests has
    /// moved past SmdReview - so a fresh page load would otherwise see 0 currently-eligible requests
    /// and, for an Approved row, wrongly treat that as "nothing happened yet" rather than "already
    /// submitted". SubmittedAt records that this row's Approved/ZeroConfirmed decision was already
    /// acted on, without deleting the row (FinalizedByName/FinalizedAt stay as the only record of who
    /// reviewed/declared this Region, since neither ApproveRegion, ConfirmZeroRegion nor
    /// FinalizeZeroRegion write a PublicityEvent of their own). Any later explicit SMD/RM action on
    /// this Region (a fresh ApproveRegion/ConfirmZeroRegion/FinalizeZeroRegion call, meaning a
    /// genuinely new round) resets it back to null - see UpsertRegionFinalizationAsync.</summary>
    public class PublicityRegionFinalization
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public int RegionId { get; set; }
        public PublicityRegionFinalizationStatus Status { get; set; }
        [MaxLength(100)] public string FinalizedByUserId { get; set; } = "";
        [MaxLength(200)] public string? FinalizedByName { get; set; }
        public DateTime FinalizedAt { get; set; } = DateTime.Now;
        public DateTime? SubmittedAt { get; set; }
    }

    /// <summary>One shipment towards <see cref="RequestId"/>. A Transfer is sourced from another
    /// HQ's excess (<see cref="SourceRequestId"/>) and needs that HQ's courier proof first.</summary>
    public class PublicityDispatch
    {
        public int Id { get; set; }
        [MaxLength(30)] public string DispatchNo { get; set; } = "";
        public int RequestId { get; set; }
        public PublicityRequest? Request { get; set; }
        public PublicityDispatchKind Kind { get; set; }
        public PublicityDispatchStatus Status { get; set; }
        [MaxLength(200)] public string? VendorName { get; set; }
        public int? SourceHqId { get; set; }
        public int? SourceRequestId { get; set; }
        [MaxLength(500)] public string? DestinationAddress { get; set; }
        public int Quantity { get; set; }
        [MaxLength(150)] public string? Transporter { get; set; }
        [MaxLength(100)] public string? TrackingNo { get; set; }
        [MaxLength(50)] public string? VehicleNo { get; set; }
        [MaxLength(100)] public string? ResponsibleUserId { get; set; }
        [MaxLength(200)] public string? ResponsibleName { get; set; }
        [MaxLength(60)] public string? ResponsibleRole { get; set; }
        public DateTime? DispatchDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        [MaxLength(200)] public string? CurrentLocation { get; set; }
        public DateTime? InTransitAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? Remarks { get; set; }
        [MaxLength(100)] public string? CreatedByUserId { get; set; }
        [MaxLength(200)] public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(200)] public string? ProofByName { get; set; }
        public DateTime? ProofAt { get; set; }
        public PublicityAcknowledgement? Acknowledgement { get; set; }
    }

    public class PublicityAcknowledgement
    {
        public int Id { get; set; }
        public int DispatchId { get; set; }
        public PublicityDispatch? Dispatch { get; set; }
        public int RequestId { get; set; }
        public int ReceivedQty { get; set; }
        public bool IsDamaged { get; set; }
        public int DamagedQty { get; set; }
        [MaxLength(100)] public string? AcknowledgedByUserId { get; set; }
        [MaxLength(200)] public string? AcknowledgedByName { get; set; }
        public string? Remarks { get; set; }
        public bool IsDraft { get; set; } = true;
        public DateTime? SubmittedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Every file of the module: design file, dispatch documents, ack photos, courier proof.</summary>
    public class PublicityFile
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public int? DispatchId { get; set; }
        public PublicityFileKind Kind { get; set; }
        [MaxLength(260)] public string FileName { get; set; } = "";
        [MaxLength(500)] public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        [MaxLength(200)] public string? UploadedByName { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Approval history + timeline: one row per action.</summary>
    public class PublicityEvent
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public PublicityRequest? Request { get; set; }
        public int? DispatchId { get; set; }
        public PublicityStage Stage { get; set; }
        [MaxLength(40)] public string Action { get; set; } = "";
        [MaxLength(100)] public string? ActorUserId { get; set; }
        [MaxLength(200)] public string? ActorName { get; set; }
        [MaxLength(60)] public string? ActorRole { get; set; }
        public string? Remarks { get; set; }
        public DateTime At { get; set; } = DateTime.UtcNow;
    }

    /// <summary>In-app notice. Delivered to a user, or to a role (optionally limited to one HQ).</summary>
    public class PublicityNotification
    {
        public int Id { get; set; }
        public PublicityNotificationType Type { get; set; }
        [MaxLength(100)] public string? TargetUserId { get; set; }
        public AppRole? TargetRole { get; set; }
        public int? TargetHqId { get; set; }
        [MaxLength(200)] public string Title { get; set; } = "";
        [MaxLength(500)] public string Message { get; set; } = "";
        [MaxLength(300)] public string? SubText { get; set; }
        public int RequestId { get; set; }
        public int? DispatchId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}
