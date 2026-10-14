using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{
    // One row for each State (RegionId = null), and one for each Region.
    // The state counter must NOT be duplicated across regional rows.
    public class SanctionNumberSequence
    {
        [Key]
        public int Id { get; set; }
        public int StateId { get; set; }
        public int? RegionId { get; set; }

        [Required, MaxLength(20)]
        public string StateSAPCode { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? RegionSAPCode { get; set; }

        // These values are managed by the sanction-issuing service after setup.
        // Confirm whether imported CSV values are NEXT or LAST issued before seeding.
        public int SanctionNumber { get; set; }
        public int SanctionNumberState { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
