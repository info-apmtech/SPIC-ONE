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

    // One row on the MDO's "Allocation Requests" list (pending-approval queue).
    public class PendingAllocationRowDto
    {
        public int AllocationId { get; set; }
        public string JmdoName { get; set; } = "";
        public string? JmdoCode { get; set; }
        public string? Location { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int DealerCount { get; set; }
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public int ProgramCount { get; set; }
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }
        // How many of the 4 PoS liquidation target fields (Urea/DAP/NPS/Others)
        // were filled in - there's no single "liquidation quantity" field to sum.
        public int PosLiquidationFieldCount { get; set; }
    }

    // Full detail for the MDO's Review page - one allocation, editable before approval.
    public class JmdoAllocationDetailDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
        public string JmdoName { get; set; } = "";
        public string? JmdoCode { get; set; }
        public string? Location { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int? HeadquarterId { get; set; }
        public List<JmdoAllocationDealerRowDto> Dealers { get; set; } = new();
        public List<JmdoAllocationProgramRowDto> Programs { get; set; } = new();
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }
    }

    public class JmdoAllocationDealerRowDto
    {
        public int RowId { get; set; }
        public int SubDealerId { get; set; }
        public string DealerName { get; set; } = "";
        public string DealerCode { get; set; } = "";
        // Weekday the JMDO picked at submission (Step 1's Sun-Sat tabs).
        public DayOfWeek? Day { get; set; }
        // Exact calendar date the MDO pins this task to during review - set
        // separately from Day, which stays whatever the JMDO originally picked.
        public DateTime? PlannedDate { get; set; }
    }

    public class JmdoAllocationProgramRowDto
    {
        public int RowId { get; set; }
        public int Csr1Id { get; set; }
        public string ProgramName { get; set; } = "";
        public decimal Budget { get; set; }
    }

    public class AddDealerToAllocationDto
    {
        public int DealerId { get; set; }
        public DateTime? PlannedDate { get; set; }
    }

    public class UpdateDealerPlannedDateDto
    {
        public DateTime? PlannedDate { get; set; }
    }

    public class AddProgramToAllocationDto
    {
        public int Csr1Id { get; set; }
    }

    public class UpdateAllocationTargetsDto
    {
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }
    }
}
