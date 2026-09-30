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
        public ProgramType? ProgramType { get; set; }

        public ICollection<ProgramStateBudget> StateBudgets { get; set; }
            = new List<ProgramStateBudget>();
        public string CreatedBy { get; set; } = "";
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class ProgramStateBudget
    {
        [Key]
        public int Id { get; set; }
        public int ProgramId { get; set; }
        public ProgramMaster? Program { get; set; }
        public int StateId { get; set; }
        public State? State { get; set; }
        public decimal BudgetAmount { get; set; }
        public ICollection<ProgramRegionBudget> Regions { get; set; }
           = new List<ProgramRegionBudget>();
    }

    public class ProgramRegionBudget
    {
        [Key]
        public int Id { get; set; }
        public int ProgramStateBudgetId { get; set; }
        public ProgramStateBudget? ProgramStateBudget { get; set; }
        public int RegionId { get; set; }
        public Region? Region { get; set; }
        public decimal BudgetAmount { get; set; }
        public ICollection<ProgramHQBudget> Headquarters { get; set; }
           = new List<ProgramHQBudget>();

    }

    public class ProgramHQBudget
    {
        [Key]
        public int Id { get; set; }
        public int ProgramRegionBudgetId { get; set; }
        public ProgramRegionBudget? ProgramRegionBudget { get; set; }

        public int HQId { get; set; }
        public Headquarter? Headquarters { get; set; }

        public decimal BudgetAmount { get; set; }

    }

}
