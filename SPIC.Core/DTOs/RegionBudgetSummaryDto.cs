using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class RegionBudgetSummaryDto
    {
        public int RegionId { get; set; }
        public decimal AllottedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
