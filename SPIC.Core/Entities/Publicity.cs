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
        public List<PublicityDispatch> Dispatches { get; set; } = new();
        public List<PublicityEvent> Events { get; set; } = new();
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
