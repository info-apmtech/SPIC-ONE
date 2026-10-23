using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{
    public class CSR1
    {
        [Key]
        public int Id { get; set; }

        // -------------------------
        // CSR-1 Plan Details
        // -------------------------
        // ProgramTypeId removed. Program type is derived from ProgramMaster.ProgramTypeId.
        public int ProgramId { get; set; }
        public int NumberOfPrograms { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        public int? HeadquarterId { get; set; }
        public int? LocationId { get; set; }

        // Stores Employeelogins.UserId as text.
        [MaxLength(100)]
        public string? ResponsiblePersonId { get; set; }

        // -------------------------
        // Expense Breakdown
        // -------------------------
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrintingAndStationery { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PublicityMaterial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StageArrangements { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceChargesLCA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TransportRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal JeepRunningExpenses { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Refreshments { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Inputs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Photography { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Compliments { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Others { get; set; }

        // -------------------------
        // Workflow
        // -------------------------
        public CSR1Status Status { get; set; } = CSR1Status.Draft;
        public string? Remarks { get; set; }
        public string? ValidateBy { get; set; }
        public DateTime? ValidateAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // -------------------------
        // Audit
        // -------------------------
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CSR1Date { get; set; }
        public string? SanctionNo { get; set; }
        public int? BudgetProgramId { get; set; }
        public int? ProgramMonth { get; set; }
        public int? ProgramYear { get; set; }
    }

    public class CSR1Products1
    {
        [Key]
        public int Id { get; set; }
        public int CSR1Id { get; set; }

        [Column(TypeName = "integer[]")]
        public int[] CropId { get; set; } = Array.Empty<int>();

        [Column(TypeName = "integer[]")]
        public int[] ProductId { get; set; } = Array.Empty<int>();

        public bool IsActive { get; set; } = true;
    }

    public class CSR1Products2
    {
        [Key]
        public int Id { get; set; }
        public int CSR1Id { get; set; }

        [Column(TypeName = "integer[]")]
        public int[] CropId { get; set; } = Array.Empty<int>();

        [Column(TypeName = "integer[]")]
        public int[] ProductId { get; set; } = Array.Empty<int>();

        public bool IsActive { get; set; } = true;
    }

    public class CSR1Products3
    {
        [Key]
        public int Id { get; set; }
        public int CSR1Id { get; set; }

        [Column(TypeName = "integer[]")]
        public int[] CropId { get; set; } = Array.Empty<int>();

        [Column(TypeName = "integer[]")]
        public int[] ProductId { get; set; } = Array.Empty<int>();

        public bool IsActive { get; set; } = true;
    }

    public class CSR1ApprovalDetails
    {
        [Key]
        public int Id { get; set; }
        public int CSR1Id { get; set; }

        [ForeignKey(nameof(CSR1Id))]
        public CSR1? CSR1 { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApproverLevel { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public enum CSR1Status
    {
        Draft = 0,
        SubmittedForValidation = 1,
        Submitted = 2,
        Pending = 3,
        Validated = 4,
        Approved = 5,
        Rejected = 6,
        Level1Approved = 7,
        Level2Approved = 8
    }
}
