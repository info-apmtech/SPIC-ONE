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

    public class ApproveProgramBudgetRequest
    {
        public List<ApproveProgramAmountDto> Programs { get; set; } = new();
    }

    public class ApproveProgramAmountDto
    {
        public int ProgramId { get; set; }
        public decimal TotalBudget { get; set; }

        public decimal AprilCount { get; set; }
        public decimal MayCount { get; set; }
        public decimal JuneCount { get; set; }
        public decimal JulyCount { get; set; }
        public decimal AugustCount { get; set; }
        public decimal SeptemberCount { get; set; }
        public decimal OctoberCount { get; set; }
        public decimal NovemberCount { get; set; }
        public decimal DecemberCount { get; set; }
        public decimal JanuaryCount { get; set; }
        public decimal FebruaryCount { get; set; }
        public decimal MarchCount { get; set; }
    }
}
