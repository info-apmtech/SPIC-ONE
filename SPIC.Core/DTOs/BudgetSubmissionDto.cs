using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class BudgetSubmissionDto
    {
        public int Id { get; set; }
        public string ProgramType { get; set; } = "";
        public string ProgramName { get; set; } = "";
        public decimal TotalBudget { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; } = "";
        public DateTime? ValidationDue { get; set; }
    }

    public class BudgetSubmissionSummaryDto
    {
        public int Total { get; set; }
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int Draft { get; set; }
    }
}
