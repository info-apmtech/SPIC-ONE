using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SPIC.Core.Entities
{
    public class BudgetProgram
    {
        [Key] public int Id { get; set; }
        public int ProgramId { get; set; }
        [ForeignKey(nameof(ProgramId))] public ProgramMaster? Program { get; set; }
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
        public string FinancialYear { get; set; } = "";
        public string CreatedBy { get; set; } = ""; public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now; public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
