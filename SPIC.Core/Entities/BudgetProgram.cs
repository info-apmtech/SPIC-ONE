using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{
    public enum BudgetProgramStatus
    {
        Draft = 0,
        Submitted = 1,
        Validated = 2,
        Approved = 3,
        Rejected = 4
    }

    public class BudgetProgram
    {
        [Key]
        public int Id { get; set; }

        public int BudgetProgramMainId { get; set; }

        [ForeignKey(nameof(BudgetProgramMainId))]
        public BudgetProgramMains? BudgetProgramMain { get; set; }

        public int ProgramId { get; set; }

        [ForeignKey(nameof(ProgramId))]
        public ProgramMaster? Program { get; set; }

        // Persisted base budget for one occurrence of this program.
        public decimal ProgramBudget { get; set; }

        public decimal AprilCount { get; set; }
        public decimal MayCount { get; set; }
        public decimal JuneCount { get; set; }
        public decimal JulyCount { get; set; }
        public decimal AugustCount { get; set; }
        public decimal SeptemberCount { get; set; }
        public decimal OctoberCount { get; set; }
        public decimal NovemberCount { get; set; }
        public decimal DecemberCount { get; set; }
        public decimal JanuaryCount { get; set; }
        public decimal FebruaryCount { get; set; }
        public decimal MarchCount { get; set; }

        // Compatibility members used by the existing API / Razor DTO mapping.
        // They are calculated and are NOT database columns.
        [NotMapped]
        public decimal TotalBudget
        {
            get => ProgramBudget;
            set => ProgramBudget = value;
        }

        [NotMapped] public decimal April { get => ProgramBudget * AprilCount; set { } }
        [NotMapped] public decimal May { get => ProgramBudget * MayCount; set { } }
        [NotMapped] public decimal June { get => ProgramBudget * JuneCount; set { } }
        [NotMapped] public decimal July { get => ProgramBudget * JulyCount; set { } }
        [NotMapped] public decimal August { get => ProgramBudget * AugustCount; set { } }
        [NotMapped] public decimal September { get => ProgramBudget * SeptemberCount; set { } }
        [NotMapped] public decimal October { get => ProgramBudget * OctoberCount; set { } }
        [NotMapped] public decimal November { get => ProgramBudget * NovemberCount; set { } }
        [NotMapped] public decimal December { get => ProgramBudget * DecemberCount; set { } }
        [NotMapped] public decimal January { get => ProgramBudget * JanuaryCount; set { } }
        [NotMapped] public decimal February { get => ProgramBudget * FebruaryCount; set { } }
        [NotMapped] public decimal March { get => ProgramBudget * MarchCount; set { } }

        [NotMapped]
        public decimal TotalAllocatedBudget =>
            April + May + June + July + August + September +
            October + November + December + January + February + March;
    }

    public class BudgetProgramMains
    {
        [Key]
        public int Id { get; set; }

        public int StateId { get; set; }
        public int? RegionId { get; set; }
        public int? HeadQuartersId { get; set; }

        // Financial year is now stored as FinancialYears.Id.
        public int FinancialYearId { get; set; }

        [ForeignKey(nameof(FinancialYearId))]
        public FinancialYear? FinancialYear { get; set; }

        public BudgetProgramStatus Status { get; set; } = BudgetProgramStatus.Draft;

        public decimal ApprovedAmount { get; set; }
        public decimal AllocatedAmount { get; set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? ValidateBy { get; set; }
        public DateTime? ValidateAt { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<BudgetProgram> Programs { get; set; } = new List<BudgetProgram>();
        public ICollection<BudgetProgramHistory> Histories { get; set; } = new List<BudgetProgramHistory>();

        // SIDAmount column is removed. Remaining is always derived.
        [NotMapped]
        public decimal RemainingAmount => ApprovedAmount - AllocatedAmount;

        // Temporary source compatibility for existing controller code; not persisted.
        [NotMapped]
        public decimal SIDAmount
        {
            get => RemainingAmount;
            set { }
        }
    }

    // One immutable snapshot per program for each workflow transition.
    [Table("BudgetProgramHistory")]
    public class BudgetProgramHistory
    {
        [Key]
        public int Id { get; set; }

        public int BudgetProgramMainId { get; set; }
           
        [ForeignKey(nameof(BudgetProgramMainId))]
        public BudgetProgramMains? BudgetProgramMain { get; set; }

        public int? BudgetProgramId { get; set; }

        public int ProgramId { get; set; }

        public decimal ProgramBudget { get; set; }

        public decimal AprilCount { get; set; }
        public decimal MayCount { get; set; }
        public decimal JuneCount { get; set; }
        public decimal JulyCount { get; set; }
        public decimal AugustCount { get; set; }
        public decimal SeptemberCount { get; set; }
        public decimal OctoberCount { get; set; }
        public decimal NovemberCount { get; set; }
        public decimal DecemberCount { get; set; }
        public decimal JanuaryCount { get; set; }
        public decimal FebruaryCount { get; set; }
        public decimal MarchCount { get; set; }

        public BudgetProgramStatus Status { get; set; }

        [MaxLength(450)]
        public string? ActionBy { get; set; }

        public DateTime ActionAt { get; set; }
    }
}
