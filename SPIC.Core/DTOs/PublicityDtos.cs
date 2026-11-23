using System;
using System.Collections.Generic;
using SPIC.Core.Entities;

namespace SPIC.Core.DTOs
{
    // Publicity Material Tracking contract (api/Publicity, PublicityController).
    //   GET  lookups                         materials, products, couriers, people, my location
    //   GET  requests/stats | requests       KPI cards | filtered, paged list
    //   GET  requests/{id}                   full details (timeline, history, dispatches, acks, files)
    //   POST requests  PUT requests/{id}     draft / submit (Submit = true)
    //   DELETE requests/{id}                 draft only
    //   POST requests/{id}/files?kind=       upload (design file, dispatch docs, ack photos, proof)
    //   DELETE files/{id}                    GET file/{*path} (access_token allowed)
    //   POST requests/{id}/review            approver action (RM, SMD, SMM, MD Admin, MD HOD)
    //   POST requests/{id}/dispatches        admin: vendor / re-initiation dispatch or HQ transfer order
    //   POST dispatches/{id}/status          admin: in transit / delivered / confirm a transfer
    //   POST dispatches/{id}/proof           source RMDO: courier proof for a transfer order
    //   POST dispatches/ocr                  courier receipt -> transporter, AWB, date
    //   PUT  dispatches/{id}/ack             destination RMDO: acknowledgement (draft / submit)
    //   GET  notifications | notifications/{id}   POST notifications/{id}/read

    public sealed class PubOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class PubPerson
    {
        public string UserId { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Role { get; set; }
    }

    public sealed class PubLookupsDto
    {
        public List<PubOption> Materials { get; set; } = new();
        public List<PubOption> Products { get; set; } = new();
        public List<string> Couriers { get; set; } = new();
        public List<string> Vendors { get; set; } = new();
        public List<PubPerson> People { get; set; } = new();
        public string? MyUserId { get; set; }
        public string? MyName { get; set; }
        public int MyStateId { get; set; }
        public int MyRegionId { get; set; }
        public int MyHqId { get; set; }
        public string? MyScope { get; set; }
    }

    public sealed class PubStatsDto
    {
        public int Total { get; set; }
        public int Draft { get; set; }
        public int PendingApproval { get; set; }
        public int FinalApproved { get; set; }
        public int InDispatch { get; set; }
        public int AckPending { get; set; }
        public int Completed { get; set; }
        public int Exception { get; set; }
        public int PendingMine { get; set; }
        public int ApprovedByMe { get; set; }
        public int Returned { get; set; }
        public int FinalApproval { get; set; }
        public int WithDgm { get; set; }
        public int DispatchPending { get; set; }
    }

    public sealed class PubRowDto
    {
        public int Id { get; set; }
        public string IndentNo { get; set; } = "";
        public string Product { get; set; } = "";
        public string Material { get; set; } = "";
        public int Quantity { get; set; }
        public string Hq { get; set; } = "";
        public PublicityStage Stage { get; set; }
        public PublicityStatus Status { get; set; }
        public string? StatusNote { get; set; }
        public string State { get; set; } = "";
        public string Region { get; set; } = "";
        public int OriginalQuantity { get; set; }
        public bool AwaitingMe { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        // Who actually performed the RM-stage "Approved" action on this request - resolved from the
        // existing PublicityEvent audit trail (Action="Approved", Stage=RmReview), never from the
        // Region's CURRENT RM assignment (that could be a different person than whoever approved
        // this specific request historically). Null if that event can't be found (e.g. a request
        // created directly past RmReview by a chain role) - the UI must show the plain "Approved by
        // RM" label in that case, never a fabricated name.
        public string? RmApprovedByName { get; set; }
        // Request-level sign-off at the SMD stage - see PublicityRequest.SmdReviewedAt's own doc
        // comment. Null means not yet reviewed.
        public DateTime? SmdReviewedAt { get; set; }
        public string? SmdReviewedByName { get; set; }
    }

    // Display-only status for a Region's current state in the SMD consolidation screen - computed
    // fresh from live request data (+ any PublicityRegionFinalization row) every time, never stored
    // as its own column anywhere. See PublicityController.Consolidation() for the derivation and
    // PublicityRegionFinalization's doc comment for why staleness is always re-checked live.
    // PendingReview specifically means "the responsible RM has not completed a submission" (no
    // eligible request has reached SMD yet, whether because nothing was ever raised or because some
    // of the Region's requests are still sitting at RM review) - distinct from Submitted, where the
    // RM's submission is complete and SMD simply hasn't started reviewing it yet. ZeroSubmittedByRm
    // means the RM has explicitly submitted Zero Requirement, awaiting SMD finalization. ZeroConfirmed
    // means SMD has finalized/acknowledged the zero requirement.
    // SubmittedToSmm means this Region's Approved/ZeroConfirmed decision has already been forwarded
    // to SMM (PublicityRegionFinalization.SubmittedAt is set) and nothing new has arrived for it
    // since - distinct from Approved/ZeroConfirmed so an already-submitted Region never keeps
    // counting toward RegionsFinalized or re-enabling "Approve & Submit to SMM" for a closed round.
    public enum PubRegionStatus { PendingReview = 0, InReview = 1, Reviewed = 2, Approved = 3, ZeroConfirmed = 4, Submitted = 5, ZeroSubmittedByRm = 6, SubmittedToSmm = 7 }

    public sealed class PubPageDto
    {
        public List<PubRowDto> Items { get; set; } = new();
        public int Total { get; set; }
    }

    public sealed class PubUpsertDto
    {
        public int MaterialTypeId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? RequestedByUserId { get; set; }
        public int StateId { get; set; }
        public int RegionId { get; set; }
        public int HeadquarterId { get; set; }
        public string? Remarks { get; set; }
        public bool Submit { get; set; }
    }

    /// <summary>One node of a stepper. State: 0 pending, 1 current, 2 done, 3 warning.</summary>
    public sealed class PubStepDto
    {
        public string Label { get; set; } = "";
        public string? Pill { get; set; }
        public string? Note { get; set; }
        public DateTime? At { get; set; }
        public int State { get; set; }
    }

    public sealed class PubHistoryDto
    {
        public string Stage { get; set; } = "";
        public string Action { get; set; } = "Not Started";
        public string? By { get; set; }
        public string? Role { get; set; }
        public DateTime? At { get; set; }
        public string? Remarks { get; set; }
        public int State { get; set; }
    }

    public sealed class PubFileDto
    {
        public int Id { get; set; }
        public int? DispatchId { get; set; }
        public PublicityFileKind Kind { get; set; }
        public string FileName { get; set; } = "";
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public sealed class PubAckDto
    {
        public int Id { get; set; }
        public int ReceivedQty { get; set; }
        public bool IsDamaged { get; set; }
        public int DamagedQty { get; set; }
        public int ShortageQty { get; set; }
        public int ExcessQty { get; set; }
        public string? AcknowledgedByUserId { get; set; }
        public string? AcknowledgedByName { get; set; }
        public string? Remarks { get; set; }
        public bool IsDraft { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string ExceptionType { get; set; } = "None";
    }

    public sealed class PubDispatchDto
    {
        public int Id { get; set; }
        public string DispatchNo { get; set; } = "";
        public int RequestId { get; set; }
        public string RequestIndentNo { get; set; } = "";
        public PublicityDispatchKind Kind { get; set; }
        public PublicityDispatchStatus Status { get; set; }
        public string SourceName { get; set; } = "";
        public int? SourceHqId { get; set; }
        public int? SourceRequestId { get; set; }
        public string? SourceIndentNo { get; set; }
        public string DestinationHq { get; set; } = "";
        public string? DestinationAddress { get; set; }
        public int Quantity { get; set; }
        public string? Transporter { get; set; }
        public string? TrackingNo { get; set; }
        public string? VehicleNo { get; set; }
        public string? ResponsibleName { get; set; }
        public string? ResponsibleRole { get; set; }
        public DateTime? DispatchDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? CurrentLocation { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProofByName { get; set; }
        public DateTime? ProofAt { get; set; }
        public PubAckDto? Ack { get; set; }
        public List<PubStepDto> Steps { get; set; } = new();
        public List<PubFileDto> Files { get; set; } = new();
        public bool CanAcknowledge { get; set; }
        public bool CanSubmitProof { get; set; }
    }

    public sealed class PubSummaryDto
    {
        public int Requested { get; set; }
        public int Dispatched { get; set; }
        public int OriginalDispatched { get; set; }
        public int OriginalReceived { get; set; }
        public int ReplacementDispatched { get; set; }
        public int ReplacementReceived { get; set; }
        public int Received { get; set; }
        public int Damaged { get; set; }
        public int Shortage { get; set; }
        public int Excess { get; set; }
        public int Usable { get; set; }
        public int Outstanding { get; set; }
        public int TransferredOut { get; set; }
        public string ExceptionType { get; set; } = "None";
        public string ExceptionStatus { get; set; } = "None";
        public string MaterialCondition { get; set; } = "-";
    }

    public sealed class PubDetailDto
    {
        public int Id { get; set; }
        public string IndentNo { get; set; } = "";
        public PublicityStage Stage { get; set; }
        public PublicityStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MaterialTypeId { get; set; }
        public string Material { get; set; } = "";
        public int ProductId { get; set; }
        public string Product { get; set; } = "";
        public int Quantity { get; set; }
        public string? RequestedByUserId { get; set; }
        public string? RequestedByName { get; set; }
        public string? RequestedByRole { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerRole { get; set; }
        public string OwnerCaption { get; set; } = "Current Approver";
        public int StateId { get; set; }
        public int RegionId { get; set; }
        public int HeadquarterId { get; set; }
        public string State { get; set; } = "";
        public string Region { get; set; } = "";
        public string Hq { get; set; } = "";
        public string? Remarks { get; set; }
        public bool CanEdit { get; set; }
        public bool CanReview { get; set; }
        public bool CanDispatch { get; set; }
        public bool CanResolve { get; set; }
        public DateTime? SmdReviewedAt { get; set; }
        public string? SmdReviewedByName { get; set; }
        public string? ExceptionRaisedBy { get; set; }
        public DateTime? ExceptionRaisedAt { get; set; }
        public string? ExceptionRemarks { get; set; }
        /// <summary>Latest remarks an approver entered (the "Additional Remarks" card).</summary>
        public string? ApproverRemarks { get; set; }
        public List<string> KnownAddresses { get; set; } = new();
        /// <summary>Other HQs' requests for the same product + material that still hold excess (transfer sources).</summary>
        public List<PubExcessDto> ExcessSources { get; set; } = new();
        public List<PubStepDto> Timeline { get; set; } = new();
        public List<PubHistoryDto> History { get; set; } = new();
        public List<PubFileDto> Files { get; set; } = new();
        public List<PubDispatchDto> Dispatches { get; set; } = new();
        public List<PubDispatchDto> OutgoingTransfers { get; set; } = new();
        public PubSummaryDto Summary { get; set; } = new();
    }

    public sealed class PubExcessDto
    {
        public int RequestId { get; set; }
        public string IndentNo { get; set; } = "";
        public string Hq { get; set; } = "";
        public int Remaining { get; set; }
    }

    /// <summary>Approver consolidation: requests waiting at my stage, grouped by region. At the SMD
    /// stage, Groups always includes EVERY Region in the state (active, or with any request that
    /// hasn't fully passed the SMD stage yet) - not just ones with currently-pending requests - see
    /// PublicityController.Consolidation()'s own comment on why that distinction matters.</summary>
    public sealed class PubConsolidationDto
    {
        public string Scope { get; set; } = "";
        public string StageLabel { get; set; } = "";
        public string NextLabel { get; set; } = "";
        public string? SubmissionId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int Regions { get; set; }
        public int Hqs { get; set; }
        /// <summary>Sum of each Region's EligibleCount only - requests still short of my stage
        /// (StillAtRmCount) are real but not yet mine to act on, so they're deliberately excluded
        /// from this total (see PubRegionDto.StillAtRmCount's own comment).</summary>
        public int Requests { get; set; }
        public int Quantity { get; set; }
        public int RegionsFinalized { get; set; }
        public int ApprovedRegions { get; set; }
        public int ZeroConfirmedRegions { get; set; }
        public List<PubRegionDto> Groups { get; set; } = new();
    }

    public sealed class PubRegionDto
    {
        public int StateId { get; set; }
        public int RegionId { get; set; }
        public string State { get; set; } = "";
        public string Region { get; set; } = "";
        public int Hqs { get; set; }
        public int Quantity { get; set; }
        /// <summary>Requests currently AT my stage (SmdReview), i.e. actually eligible for me to
        /// review right now. This is what "Total Requests"/the review-progress denominator should
        /// be built from - NOT StillAtRmCount, which is a different, earlier-stage concept.</summary>
        public int EligibleCount { get; set; }
        /// <summary>Of EligibleCount, how many already have SmdReviewedAt set.</summary>
        public int ReviewedCount { get; set; }
        /// <summary>Requests from this Region that exist but haven't reached my stage yet (still
        /// with RM or earlier) - real, but not "mine" to review yet. Approving/zero-confirming a
        /// Region requires this to be 0 first (see ApproveRegion/ConfirmZeroRegion), so these can't
        /// be silently skipped by forwarding before RM has finished its own part.</summary>
        public int StillAtRmCount { get; set; }
        public PubRegionStatus Status { get; set; }
        public string? FinalizedByName { get; set; }
        public DateTime? FinalizedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<PubRowDto> Rows { get; set; } = new();
    }

    /// <summary>Body for POST regions/{stateId}/{regionId}/approve and .../confirm-zero - both take
    /// only an optional remark, every other input is resolved server-side from the authenticated
    /// caller's own state/role claim and the live request data (see PublicityController).</summary>
    public sealed class PubRegionActionDto
    {
        public string? Remarks { get; set; }
    }

    /// <summary>Body for POST requests/{id}/mark-reviewed - a toggle, not a one-way action, so SMD
    /// can un-review a request to look again before approving the Region.</summary>
    public sealed class PubMarkReviewedDto
    {
        public bool Reviewed { get; set; } = true;
    }

    public sealed class PubBulkReviewDto
    {
        public List<int> Ids { get; set; } = new();
        public string? Remarks { get; set; }
    }

    /// <summary>ReviewBulk's actual response shape - Approved is the server-confirmed count, which
    /// can be less than the number of ids submitted (already-forwarded/out-of-scope ids are silently
    /// skipped, never retried) - callers must report THIS count, not their own request-list length,
    /// so a partial failure is never shown as "every request processed".</summary>
    public sealed class PubBulkResultDto
    {
        public bool Success { get; set; }
        public int Approved { get; set; }
    }

    public sealed class PubReviewDto
    {
        /// <summary>Approve, Return, Reject or Edit (Edit changes the quantity and stays on the stage).</summary>
        public string Action { get; set; } = "Approve";
        public int? Quantity { get; set; }
        public string? Remarks { get; set; }
    }

    public sealed class PubDispatchCreateDto
    {
        public PublicityDispatchKind Kind { get; set; }
        public string? VendorName { get; set; }
        public int? SourceRequestId { get; set; }
        public int Quantity { get; set; }
        public string? Transporter { get; set; }
        public string? TrackingNo { get; set; }
        public string? VehicleNo { get; set; }
        public string? ResponsibleUserId { get; set; }
        public DateTime? DispatchDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? DestinationAddress { get; set; }
        public string? Remarks { get; set; }
    }

    public sealed class PubDispatchStatusDto
    {
        public PublicityDispatchStatus Status { get; set; }
        public string? CurrentLocation { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Transporter { get; set; }
        public string? TrackingNo { get; set; }
        public DateTime? DispatchDate { get; set; }
    }

    public sealed class PubProofDto
    {
        public string? Transporter { get; set; }
        public string? TrackingNo { get; set; }
        public DateTime? DispatchDate { get; set; }
        public string? VehicleNo { get; set; }
        public string? Remarks { get; set; }
    }

    public sealed class PubOcrDto
    {
        public string? Transporter { get; set; }
        public string? TrackingNo { get; set; }
        public DateTime? DispatchDate { get; set; }
        public string? VehicleNo { get; set; }
        public bool Found { get; set; }
    }

    public sealed class PubAckUpsertDto
    {
        public int ReceivedQty { get; set; }
        public bool IsDamaged { get; set; }
        public int DamagedQty { get; set; }
        public string? AcknowledgedByUserId { get; set; }
        public string? Remarks { get; set; }
        public bool Submit { get; set; }
    }

    public sealed class PubNotificationDto
    {
        public int Id { get; set; }
        public PublicityNotificationType Type { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string? SubText { get; set; }
        public int RequestId { get; set; }
        public int? DispatchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    /// <summary>The notification drawer: notice + the transfer order it points at.</summary>
    public sealed class PubNotificationDetailDto
    {
        public PubNotificationDto Notice { get; set; } = new();
        public string? DestinationIndentNo { get; set; }
        public string? SourceIndentNo { get; set; }
        public string? SourceHq { get; set; }
        public string? DestinationHq { get; set; }
        public string? Product { get; set; }
        public string? Material { get; set; }
        public int TransferQty { get; set; }
        public int ReportedExcess { get; set; }
        public int AlreadyTransferred { get; set; }
        public int RemainingExcess { get; set; }
        public string? Reason { get; set; }
    }
}
