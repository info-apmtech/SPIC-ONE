using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class BudgetDashboardDto
    {
        public decimal ApprovedBudget { get; set; }

        public decimal AllocatedBudget { get; set; }

        public decimal RemainingBudget { get; set; }

        public List<ProgramWiseBudgetDto> ProgramBudgets { get; set; } = new();
    }

}
