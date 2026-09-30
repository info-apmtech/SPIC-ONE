using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Entities
{
    public class StateBudgetAllocation
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public decimal BudgetAmount { get; set; }
        public string FinancialYear { get; set; } = "";
        public string Status { get; set; } = "Draft";
    }

}
