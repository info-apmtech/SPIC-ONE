using System;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.Entities
{
    public class GpsSetting
    {
        [Key]
        public int Id { get; set; }

        public int IntervalSeconds { get; set; } = 20;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
