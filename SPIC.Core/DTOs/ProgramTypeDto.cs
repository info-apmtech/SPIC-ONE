using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class ProgramTypeDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
