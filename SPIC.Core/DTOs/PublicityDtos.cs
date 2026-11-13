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
    }

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

    /// <summary>Approver consolidation: requests waiting at my stage, grouped by region.</summary>
    public sealed class PubConsolidationDto
    {
        public string Scope { get; set; } = "";
        public string StageLabel { get; set; } = "";
        public string NextLabel { get; set; } = "";
        public string? SubmissionId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int Regions { get; set; }
        public int Hqs { get; set; }
        public int Requests { get; set; }
        public int Quantity { get; set; }
        public int ReadyRegions { get; set; }
        public List<PubRegionDto> Groups { get; set; } = new();
    }

    public sealed class PubRegionDto
    {
        public string State { get; set; } = "";
        public string Region { get; set; } = "";
        public int Hqs { get; set; }
        public int Quantity { get; set; }
        public int Pending { get; set; }
        public int Total { get; set; }
        public DateTime? LastUpdated { get; set; }
        public List<PubRowDto> Rows { get; set; } = new();
    }

    public sealed class PubBulkReviewDto
    {
        public List<int> Ids { get; set; } = new();
        public string? Remarks { get; set; }
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
