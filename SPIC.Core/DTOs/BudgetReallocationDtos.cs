using System;
using System.ComponentModel.DataAnnotations;
using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

public sealed class CreateBudgetReallocationDto
{
    [Range(1, int.MaxValue)] public int FinancialYearId { get; set; }
    public ReallocationLevel SourceLevel { get; set; }
    [Range(1, int.MaxValue)] public int SourceId { get; set; }
    public ReallocationLevel DestinationLevel { get; set; }
    [Range(1, int.MaxValue)] public int DestinationId { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] public decimal Amount { get; set; }
    [Required, StringLength(1000, MinimumLength = 5)] public string Reason { get; set; } = string.Empty;
}
public sealed class ReallocationDecisionDto
{
    public bool Approve { get; set; }
    [StringLength(1000)] public string? Remarks { get; set; }
}
public sealed record BudgetPoolKey(int FinancialYearId, ReallocationLevel Level, int EntityId);
public sealed record BudgetAvailability(decimal Allocated, decimal Used, decimal PendingCommitments, decimal TransferAdjustment)
{
    public decimal Available => Allocated + TransferAdjustment - Used - PendingCommitments;
}
