using System;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.DTOs
{
    public class GpsSettingDto
    {
        public int Id { get; set; }
        public int IntervalSeconds { get; set; } = 20;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class UpdateGpsSettingDto
    {
        [Range(1, 100, ErrorMessage = "Tracking interval must be between 1 and 100 seconds.")]
        public int IntervalSeconds { get; set; } = 20;
    }
}
