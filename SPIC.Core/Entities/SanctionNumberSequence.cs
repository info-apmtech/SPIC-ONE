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

        // Codes are read from the State and Region masters.
        // These counters are managed by the sanction-issuing service after setup.
        // Preserve the existing NEXT/LAST-issued convention when importing values.
        public int SanctionNumber { get; set; }
        public int SanctionNumberState { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
