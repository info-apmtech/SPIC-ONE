using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
    public class JmdoTaskAllocationCreateDto
    {
        public List<int> DealerIds { get; set; } = new();
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public List<int> ProgramIds { get; set; } = new();
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }
    }

    public class JmdoTaskAllocationResponseDto
    {
        public int Id { get; set; }
        public DateTime AllocationDate { get; set; }
        public string Status { get; set; } = "";
        public DateTime SubmittedAt { get; set; }

        public int DealerCount { get; set; }
        public int ProgramCount { get; set; }
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }
    }
}
