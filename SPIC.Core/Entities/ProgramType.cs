using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SPIC.Core.Entities
{
    public class ProgramType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Display(Name = "Program Type")]
        public string Name { get; set; } = "";       

        // Indicates whether user can modify/change the budget amount
        [Display(Name = "Is Change Amount")]
        public bool IsChangeAmount { get; set; } = false;
        public string CreatedBy { get; set; } = "";
        public string? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public ICollection<ProgramMaster> Programs { get; set; } = new List<ProgramMaster>();
    }
}
