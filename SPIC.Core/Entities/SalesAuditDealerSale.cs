using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities;

// Standalone SAP sales invoice table: no CSR1/CSR2 relationship.
public sealed class SalesAuditDealerSale
{
    [Key]
    public long Id { get; set; }
    public Guid ImportBatchId { get; set; }
    [MaxLength(250)] public string UploadFileName { get; set; } = "";
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string ImportedBy { get; set; } = "";
    [Column(TypeName = "jsonb")]
    public string SourceRowJson { get; set; } = "{}";
}
