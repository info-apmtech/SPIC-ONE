using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{
    public enum DemoDocumentationStatus
    {
        Draft = 0,
        Started = 1
    }

    public class DemoDocumentation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CSR2Id { get; set; }

        [ForeignKey(nameof(CSR2Id))]
        public CSR2? CSR2 { get; set; }

        [Required, MaxLength(200)]
        public string FarmerName { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        [RegularExpression(@"^[0-9]{10}$")]
        public string MobileNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Village { get; set; }

        [MaxLength(1000)]
        public string? FarmerAddress { get; set; }

        [Required]
        public int StateId { get; set; }

        [Required]
        public int DistrictId { get; set; }

        [Required, MaxLength(6)]
        [RegularExpression(@"^[0-9]{6}$")]
        public string PinCode { get; set; } = string.Empty;

        [Required]
        public int CropId { get; set; }

        [Required]
        public int CropTypeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? SowingDate { get; set; }

        public int? CropStageId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? HarvestDate { get; set; }

        [MaxLength(2000)]
        public string? Remarks { get; set; }

        public DemoDocumentationStatus Status { get; set; } = DemoDocumentationStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? StartedAt { get; set; }

        [MaxLength(100)]
        public string? StartedBy { get; set; }
    }
}
