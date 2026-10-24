using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SPIC.Core.Entities
{
    public class CSRExceedingDaySetting
    {
        [Key]
        public int Id { get; set; }

        public int CSR1Day { get; set; }

        public int CSR2Day { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
