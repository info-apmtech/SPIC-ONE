using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class StateBudgetAllocationDto
    {
        public int StateId { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal AllocatedAmount { get; set; }

        public decimal RemainingAmount { get; set; }
    }
}
