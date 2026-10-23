using System;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.DTOs
{
    public sealed class SanctionNumberSequenceDto
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public int? RegionId { get; set; }

        // Display values read from the State and Region masters.
        public string StateName { get; set; } = "";
        public string? RegionName { get; set; }
        public string? StateCode { get; set; }
        public string? RegionCode { get; set; }

        public int SanctionNumber { get; set; }
        public int SanctionNumberState { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Only used during initial creation. Existing counters are not writable
    // through this management API, to prevent accidental reuse.
    public sealed class CreateSanctionNumberSequenceDto
    {
        [Range(1, int.MaxValue)]
        public int StateId { get; set; }

        [Range(1, int.MaxValue)]
        public int? RegionId { get; set; }

        [Range(0, int.MaxValue)]
        public int SanctionNumber { get; set; }

        [Range(0, int.MaxValue)]
        public int SanctionNumberState { get; set; }
    }
}
