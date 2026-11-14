using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities;

public class SalesAuditDealerMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CSR2Id { get; set; }

    [ForeignKey(nameof(CSR2Id))]
    public CSR2? CSR2 { get; set; }

    [Required]
    public int DealerRegistrationId { get; set; }

    [ForeignKey(nameof(DealerRegistrationId))]
    public DealerRegistration? DealerRegistration { get; set; }
}
