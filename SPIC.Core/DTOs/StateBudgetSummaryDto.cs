using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class StateBudgetSummaryDto
    {
        public int StateId { get; set; }
        public decimal AllottedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
