using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SPIC.Core.Entities;

public enum ReallocationLevel { State = 1, Region = 2, Headquarters = 3 }
public enum ReallocationStatus { Draft = 0, PendingSMM = 1, PendingAVP = 2, PendingMDHOD = 3, Approved = 4, Rejected = 5, Cancelled = 6 }
public enum ReallocationDecision { Submitted = 0, Approved = 1, Rejected = 2 }
public class BudgetReallocation
{
    public int Id { get; set; }
    public int FinancialYearId { get; set; }
    public ReallocationLevel SourceLevel { get; set; }
    public int SourceId { get; set; }
    public ReallocationLevel DestinationLevel { get; set; }
    public int DestinationId { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal Amount { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(30)] public string InitiatorRole { get; set; } = string.Empty;
    [MaxLength(128)] public string InitiatedBy { get; set; } = string.Empty;
    public ReallocationStatus Status { get; set; } = ReallocationStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<BudgetReallocationApproval> Approvals { get; set; } = new List<BudgetReallocationApproval>();
}
public class BudgetReallocationApproval
{
    public int Id { get; set; }
    public int BudgetReallocationId { get; set; }
    [ForeignKey(nameof(BudgetReallocationId))]
    public BudgetReallocation BudgetReallocation { get; set; } = null!;
    [MaxLength(30)] public string StageRole { get; set; } = string.Empty;
    public ReallocationDecision Decision { get; set; }
    [MaxLength(128)] public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? Remarks { get; set; }
}
