using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Entities
{
    public class CropStage
    {
        public int Id { get; set; }
        public int CropId { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
}
