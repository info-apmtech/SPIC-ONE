using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class ProgramWiseBudgetDto
    {
        public int Id { get; set; }
        public int ProgramId { get; set; }

        public string ProgramType { get; set; } = "";

        public string ProgramName { get; set; } = "";

        public decimal BudgetAmount { get; set; }

        public bool IsChangeAmount { get; set; }
        public decimal TotalBudget { get; set; }


        // Counts (User Entry)
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



        // Calculated Budget (Readonly)
        public decimal AprilBudget { get; set; }
        public decimal MayBudget { get; set; }
        public decimal JuneBudget { get; set; }
        public decimal JulyBudget { get; set; }
        public decimal AugustBudget { get; set; }
        public decimal SeptemberBudget { get; set; }
        public decimal OctoberBudget { get; set; }
        public decimal NovemberBudget { get; set; }
        public decimal DecemberBudget { get; set; }
        public decimal JanuaryBudget { get; set; }
        public decimal FebruaryBudget { get; set; }
        public decimal MarchBudget { get; set; }
    }
}
