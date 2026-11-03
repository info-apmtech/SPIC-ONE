using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities;


public class SalesAuditPlanLine
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public int ProductId { get; set; }
    [Column(TypeName = "numeric(18,3)")] public decimal QuantityMT { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal ValueRs { get; set; }
}


public class SalesAuditAchievementLine
{
    [Key] public int Id { get; set; }
    public int CSR2Id { get; set; }
    public int ProductId { get; set; }
    [Column(TypeName = "numeric(18,3)")] public decimal QuantityMT { get; set; }
    [Column(TypeName = "numeric(18,2)")] public decimal ValueRs { get; set; }
}
