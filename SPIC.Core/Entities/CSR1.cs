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
        // Program type is derived from ProgramMaster.ProgramTypeId.
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
        // NotMapped, not columns: the expense breakdown moved to the normalized CSR1Expenses table
        // (one row per CSR1Id+ExpenseId, see CSR1Expenses below and ExpenseInformation for the
        // catalog of expense types) on 2026-10-24. These 11 properties are kept on CSR1 purely as a
        // stable, backward-compatible shape for request/response JSON (CSR1SaveRequest.CSR1 IS this
        // entity, and the UI still sends/expects these exact field names) - CSR1ExpenseMapper
        // converts between this shape and CSR1Expenses rows; nothing here is ever queried by EF.
        [NotMapped]
        public decimal PrintingAndStationery { get; set; }

        [NotMapped]
        public decimal PublicityMaterial { get; set; }

        [NotMapped]
        public decimal StageArrangements { get; set; }

        [NotMapped]
        public decimal ServiceChargesLCA { get; set; }

        [NotMapped]
        public decimal TransportRent { get; set; }

        [NotMapped]
        public decimal JeepRunningExpenses { get; set; }

        [NotMapped]
        public decimal Refreshments { get; set; }

        [NotMapped]
        public decimal Inputs { get; set; }

        [NotMapped]
        public decimal Photography { get; set; }

        [NotMapped]
        public decimal Compliments { get; set; }

        [NotMapped]
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


    // =========================================================
    // CSR-1 EXPENSES
    // Each expense is stored as a separate row.
    // ExpenseId = ExpenseInformations.Id
    // =========================================================
    public class CSR1Expenses
    {
        [Key]
        public int Id { get; set; }

        public int CSR1Id { get; set; }

        [ForeignKey(nameof(CSR1Id))]
        public CSR1? CSR1 { get; set; }

        // ExpenseInformations.Id
        public int? ExpenseId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpenseAmount { get; set; }

        public bool IsActive { get; set; } = true;
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