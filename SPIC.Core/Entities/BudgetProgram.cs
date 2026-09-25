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


        public int ProgramId { get; set; }

        [ForeignKey(nameof(ProgramId))]
        public ProgramMaster? Program { get; set; }

        // Base / Total Budget Amount
        public decimal TotalBudget { get; set; }


        // April
        public decimal AprilCount { get; set; }
        public decimal April { get; set; }


        // May
        public decimal MayCount { get; set; }
        public decimal May { get; set; }


        // June
        public decimal JuneCount { get; set; }
        public decimal June { get; set; }


        // July
        public decimal JulyCount { get; set; }
        public decimal July { get; set; }


        // August
        public decimal AugustCount { get; set; }
        public decimal August { get; set; }


        // September
        public decimal SeptemberCount { get; set; }
        public decimal September { get; set; }


        // October
        public decimal OctoberCount { get; set; }
        public decimal October { get; set; }


        // November
        public decimal NovemberCount { get; set; }
        public decimal November { get; set; }


        // December
        public decimal DecemberCount { get; set; }
        public decimal December { get; set; }


        // January
        public decimal JanuaryCount { get; set; }
        public decimal January { get; set; }


        // February
        public decimal FebruaryCount { get; set; }
        public decimal February { get; set; }


        // March
        public decimal MarchCount { get; set; }
        public decimal March { get; set; }

        public string Status { get; set; } = "Draft";

        public string FinancialYear { get; set; } = "";

        public string CreatedBy { get; set; } = "";

        public string? UpdatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
