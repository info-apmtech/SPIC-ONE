using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class StateBudgetDto
    {
        public int StateId { get; set; }
        public string StateName { get; set; } = "";
        public decimal BudgetAmount { get; set; }
    }
}
