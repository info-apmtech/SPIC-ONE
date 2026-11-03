using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{
    public enum DemoTreatmentStatus { Draft = 0, Completed = 1 }

    public class DemoTreatment
    {
        [Key] public int Id { get; set; }
        [Required] public int CSR2Id { get; set; }
        [ForeignKey(nameof(CSR2Id))] public CSR2? CSR2 { get; set; }
        [Required] public int DemoDocumentationId { get; set; }
        [ForeignKey(nameof(DemoDocumentationId))] public DemoDocumentation? DemoDocumentation { get; set; }
        public int TreatmentNumber { get; set; } = 1;
        public DemoTreatmentStatus Status { get; set; } = DemoTreatmentStatus.Draft;
        public DateTime? VisitDate { get; set; }
        [MaxLength(100)] public string? FieldOfficerUserId { get; set; }
        public int? CropStageId { get; set; }
        [ForeignKey(nameof(CropStageId))] public CropStage? CropStage { get; set; }
        [MaxLength(50)] public string? PlantCondition { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey(nameof(ProductId))] public Product? Product { get; set; }
        [MaxLength(100)] public string? Dosage { get; set; }
        [MaxLength(100)] public string? ApplicationMethod { get; set; }
        public DateTime? TreatmentDate { get; set; }
        [MaxLength(100)] public string? ApplicationStage { get; set; }
        [MaxLength(2000)] public string? TreatmentRemarks { get; set; }
        [MaxLength(4000)] public string? PhysicalParameters { get; set; }
        [MaxLength(4000)] public string? YieldParameters { get; set; }
        [MaxLength(4000)] public string? ObservationNotes { get; set; }
        public bool HasProblem { get; set; }
        [MaxLength(100)] public string? ProblemCategory { get; set; }
        [MaxLength(2000)] public string? ProblemDescription { get; set; }
        [MaxLength(50)] public string? Severity { get; set; }
        [MaxLength(2000)] public string? Recommendation { get; set; }
        [MaxLength(2000)] public string? ActionTaken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(100)] public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [MaxLength(100)] public string? UpdatedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
        [MaxLength(100)] public string? CompletedBy { get; set; }
        public List<DemoTreatmentObservation> Observations { get; set; } = new();
        public List<DemoTreatmentPhoto> Photos { get; set; } = new();
    }

    public class DemoTreatmentObservation
    {
        [Key] public int Id { get; set; }
        public int DemoTreatmentId { get; set; }
        [ForeignKey(nameof(DemoTreatmentId))] public DemoTreatment? DemoTreatment { get; set; }
        [Required, MaxLength(200)] public string Observation { get; set; } = "";
    }

    public class DemoTreatmentPhoto
    {
        [Key] public int Id { get; set; }
        public int DemoTreatmentId { get; set; }
        [ForeignKey(nameof(DemoTreatmentId))] public DemoTreatment? DemoTreatment { get; set; }
        [Required, MaxLength(10)] public string PhotoType { get; set; } = ""; // Control / Treated
        [Required, MaxLength(255)] public string FileName { get; set; } = "";
        [Required, MaxLength(50)] public string ContentType { get; set; } = "";
        [Required] public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
