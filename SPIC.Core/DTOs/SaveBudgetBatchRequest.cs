using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class SaveBudgetBatchRequest
    {
        public string FinancialYear { get; set; } = string.Empty;

        // Amount provided to SMM
        public decimal ApprovedAmount { get; set; }

        // Balance left with SMM after program planning
        public decimal RemainingAmount { get; set; }

        public List<BudgetProgram> Programs { get; set; } = new();
    }
}
