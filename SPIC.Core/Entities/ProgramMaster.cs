using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SPIC.Core.Entities
{
    public class ProgramMaster
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Display(Name = "Program Name")]
        public string Name { get; set; } = "";
        public int ProgramTypeId { get; set; }
        [ForeignKey(nameof(ProgramTypeId))]

        [Display(Name = "Budget Amount")]
        public decimal BudgetAmount { get; set; }
        public ProgramType? ProgramType { get; set; }
        public string CreatedBy { get; set; } = "";
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
