using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SPIC.Core.Entities
{
    public class BudgetProgram
    {
        [Key]
        public int Id { get; set; }

        // FK -> BudgetProgramMains.Id
        public int BudgetProgramMainId { get; set; }

        [ForeignKey(nameof(BudgetProgramMainId))]
        public BudgetProgramMains? BudgetProgramMain { get; set; }

        public int ProgramId { get; set; }

        [ForeignKey(nameof(ProgramId))]
        public ProgramMaster? Program { get; set; }

        public decimal TotalBudget { get; set; }

        public decimal AprilCount { get; set; }
        public decimal April { get; set; }

        public decimal MayCount { get; set; }
        public decimal May { get; set; }

        public decimal JuneCount { get; set; }
        public decimal June { get; set; }

        public decimal JulyCount { get; set; }
        public decimal July { get; set; }

        public decimal AugustCount { get; set; }
        public decimal August { get; set; }

        public decimal SeptemberCount { get; set; }
        public decimal September { get; set; }

        public decimal OctoberCount { get; set; }
        public decimal October { get; set; }

        public decimal NovemberCount { get; set; }
        public decimal November { get; set; }

        public decimal DecemberCount { get; set; }
        public decimal December { get; set; }

        public decimal JanuaryCount { get; set; }
        public decimal January { get; set; }

        public decimal FebruaryCount { get; set; }
        public decimal February { get; set; }

        public decimal MarchCount { get; set; }
        public decimal March { get; set; }
    }

    public class BudgetProgramMains
    {
        public int Id { get; set; }

        public int StateId { get; set; }

        // Add this
        public int? RegionId { get; set; }

        public string FinancialYear { get; set; } = string.Empty;

        public string Status { get; set; } = "Draft";

        public decimal ApprovedAmount { get; set; }

        public decimal AllocatedAmount { get; set; }

        public decimal SIDAmount { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ValidateBy { get; set; }

        public DateTime? ValidateAt { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public ICollection<BudgetProgram>? Programs { get; set; }
    }
}
