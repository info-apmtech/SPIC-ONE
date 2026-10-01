using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class SaveBudgetBatchRequest
    {
        public string FinancialYear { get; set; } = "";
        public decimal ApprovedAmount { get; set; }

        public decimal SIDAmount { get; set; }

        public List<BudgetProgram> Programs { get; set; }
            = new List<BudgetProgram>();
    }
}
