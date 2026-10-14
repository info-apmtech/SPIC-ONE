using System;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.DTOs
{
    public sealed class SanctionNumberSequenceDto
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public int? RegionId { get; set; }
        public string StateSAPCode { get; set; } = "";
        public string? RegionSAPCode { get; set; }
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
        public int? RegionId { get; set; }
        [Required, MaxLength(20)]
        public string StateSAPCode { get; set; } = "";
        [MaxLength(20)]
        public string? RegionSAPCode { get; set; }
        [Range(0, int.MaxValue)]
        public int SanctionNumber { get; set; }
        [Range(0, int.MaxValue)]
        public int SanctionNumberState { get; set; }
    }

    // Metadata-only edits: sequence counters cannot be reset by this endpoint.
    public sealed class UpdateSanctionNumberSequenceDto
    {
        [Required, MaxLength(20)]
        public string StateSAPCode { get; set; } = "";
        [MaxLength(20)]
        public string? RegionSAPCode { get; set; }
    }
}
