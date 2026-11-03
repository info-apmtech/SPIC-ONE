using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities;

// Add to AppDbContext model and create an EF Core migration before enabling the API.

public class SalesAuditRecord
{
    [Key] public int Id { get; set; }
    [Required] public int CSR2Id { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal? AdditionalApprovedBudgetRs { get; set; }
    public bool IsFinalSubmitted { get; set; }
    public DateTime? FinalSubmittedAt { get; set; }
    public DateTime? SchemeFrom { get; set; }
    public DateTime? SchemeTo { get; set; }
    // INR crores; do not derive a target until a signed business rule is provided.
    [Column(TypeName = "numeric(18,4)")] public decimal? RequiredTurnover { get; set; }
    [Column(TypeName = "numeric(18,4)")] public decimal? ActualTurnover { get; set; }
    [Required, MaxLength(40)] public string DocumentStatus { get; set; } = "Not Started";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(100)] public string? UpdatedBy { get; set; }
}
