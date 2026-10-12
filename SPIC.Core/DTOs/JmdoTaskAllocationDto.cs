using System;
using System.Collections.Generic;
using SPIC.Core.Entities;

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
        public AllocTaskStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? StartPlaceName { get; set; }
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
        public List<JmdoAllocationProgramRowDto> Programs { get; set; } = new();
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
        public int? UreaTarget { get; set; }
        public int? DapTarget { get; set; }
        public int? NpsTarget { get; set; }
        public int? OthersTarget { get; set; }

        // Field-work progress - distinct from the "Status" above, which is the MDO's
        // approval status of the whole week's submission.
        public AllocTaskStatus SpcmStatus { get; set; }
        public AllocTaskStatus SoilSampleStatus { get; set; }
        public AllocTaskStatus PosLiquidationStatus { get; set; }
        public DateTime? SpcmStartedAt { get; set; }
        public DateTime? SoilSampleStartedAt { get; set; }
        public DateTime? PosLiquidationStartedAt { get; set; }
        public DateTime? SpcmCompletedAt { get; set; }
        public DateTime? SoilSampleCompletedAt { get; set; }
        public DateTime? PosLiquidationCompletedAt { get; set; }
        public string? SpcmStartPlaceName { get; set; }
        public string? SoilSampleStartPlaceName { get; set; }
        public string? PosLiquidationStartPlaceName { get; set; }

        public List<JmdoTaskCloseoutDto> Closeouts { get; set; } = new();
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
        public AllocTaskStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? StartPlaceName { get; set; }
        public string? SanctionNo { get; set; }
        public DateTime? SanctionDate { get; set; }
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

    // Sent from TaskDetail's Start/Stop buttons. TaskKey reuses the exact same string
    // convention the client already builds for itself ("dealer_{subDealerId}",
    // "prog_{csr1Id}", "spcm", "soil", "pos") so the server and client never need two
    // separate ideas of "which task is this".
    public class UpdateTaskStatusDto
    {
        public string TaskKey { get; set; } = "";
        public AllocTaskStatus Status { get; set; }
        // Reverse-geocoded on the client at the moment of capture (Start) and sent up alongside
        // the status change, so the server can cache it next to StartedAt - only meaningful when
        // Status is InProgress.
        public string? PlaceName { get; set; }
    }

    public class ProductLineDto
    {
        public string ProductGroupName { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string StockText { get; set; } = "";
    }

    // One "Save Record" submission from TaskDetail's close-out form.
    public class JmdoTaskCloseoutDto
    {
        public int Id { get; set; }
        public JmdoCloseoutKind Kind { get; set; }
        public int? SubDealerId { get; set; }
        public int? Csr1Id { get; set; }
        public string Outcome { get; set; } = "";
        public string? Remarks { get; set; }
        public int? AchievedTarget { get; set; }
        public int? Urea { get; set; }
        public int? Nps { get; set; }
        public int? Dap { get; set; }
        public int? Other { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductLineDto> Products { get; set; } = new();
    }

    public class CreateJmdoTaskCloseoutDto
    {
        public JmdoCloseoutKind Kind { get; set; }
        public int? SubDealerId { get; set; }
        public int? Csr1Id { get; set; }
        public string Outcome { get; set; } = "";
        public string? Remarks { get; set; }
        public int? AchievedTarget { get; set; }
        public int? Urea { get; set; }
        public int? Nps { get; set; }
        public int? Dap { get; set; }
        public int? Other { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public List<ProductLineDto> Products { get; set; } = new();
    }

    public class CloseoutFileUploadResponse
    {
        public string FilePath { get; set; } = "";
    }
}
