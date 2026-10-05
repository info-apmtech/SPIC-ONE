using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class DemoDocumentationDto
    {
        public int Id { get; set; }

        public string SeasonNumber { get; set; } = "";

        public string ProgramName { get; set; } = "";

        public string Product { get; set; } = "";

        public string Crop { get; set; } = "";

        public string Location { get; set; } = "";

        public string CurrentStage { get; set; } = "";

        public DateTime SowingDate { get; set; }

        public DateTime HarvestDate { get; set; }

        public int TotalTreatments { get; set; }

        public int CompletedTreatments { get; set; }

        public string DocumentationStatus { get; set; } = "";

        public string Status { get; set; } = "";
    }
}
