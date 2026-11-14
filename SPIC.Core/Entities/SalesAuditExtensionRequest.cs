using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Entities
{
    public sealed class SalesAuditExtensionRequest
    {
        public int Id { get; set; }
        public int CSRId { get; set; }
        public DateTime PreviousEndDate { get; set; }
        public DateTime NewEndDate { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public string Reason { get; set; } = "";
        public string? Remarks { get; set; } = "";
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public SalesAuditExtensionRequestStatus Status { get; set; }
            = SalesAuditExtensionRequestStatus.Submitted;
    }

    public enum SalesAuditExtensionRequestStatus
    {
        Submitted = 0,
        Approved = 1,
        Rejected = 2
    }
}
