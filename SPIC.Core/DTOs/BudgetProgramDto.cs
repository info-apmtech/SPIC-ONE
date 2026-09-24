using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class BudgetProgramDto
    {
        public int Id { get; set; }
        public string ProgramType { get; set; } = "";
        public string ProgramName { get; set; } = "";
        public decimal TotalBudget { get; set; }
        public decimal April { get; set; }
        public decimal May { get; set; }
        public decimal June { get; set; }
        public decimal July { get; set; }
        public decimal August { get; set; }
        public decimal September { get; set; }
        public decimal October { get; set; }
        public decimal November { get; set; }
        public decimal December { get; set; }
        public decimal January { get; set; }
        public decimal February { get; set; }
        public decimal March { get; set; }
    }
}
