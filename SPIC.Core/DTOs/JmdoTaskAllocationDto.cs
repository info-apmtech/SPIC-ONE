using System;
using System.Collections.Generic;

namespace SPIC.Core.DTOs
{
    public class JmdoDealerDayAssignmentDto
    {
        public int DealerId { get; set; }
        public DayOfWeek Day { get; set; }
    }

    public class JmdoTaskAllocationCreateDto
    {
        public List<JmdoDealerDayAssignmentDto> DealerAssignments { get; set; } = new();
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public List<int> ProgramIds { get; set; } = new();
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }
    }

    public class JmdoDealerAssignmentResponseDto
    {
        public int DealerId { get; set; }
        public string DealerName { get; set; } = "";
        public string DealerCode { get; set; } = "";
        public DayOfWeek? Day { get; set; }
    }

    public class JmdoTaskAllocationResponseDto
    {
        public int Id { get; set; }
        public DateTime AllocationDate { get; set; }
        public string Status { get; set; } = "";
        public DateTime SubmittedAt { get; set; }

        public int DealerCount { get; set; }
        public int ProgramCount { get; set; }
        public List<JmdoDealerAssignmentResponseDto> DealerAssignments { get; set; } = new();
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }
    }
}
