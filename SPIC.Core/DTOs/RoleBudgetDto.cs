using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class RoleBudgetDto
    {
        public decimal BudgetAmount { get; set; }
        public string BudgetLabel { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
