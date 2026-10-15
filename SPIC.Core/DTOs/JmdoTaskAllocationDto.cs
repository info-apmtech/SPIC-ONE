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
        // Which Sun-Sat week this submission plans for - any date within that week works, the
        // server only uses it to find that week's Sunday. Null means "this week" (the original,
        // only-ever-current-week behaviour), so existing callers need no change.
        public DateTime? WeekStart { get; set; }
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
        // Set by the MDO during review, not by the JMDO at submission - optional, defaults to Normal.
        public DealerTaskPriority Priority { get; set; }
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

    // One row on the MDO's "Allocation Requests" list (pending-approval queue). Admin also sees
    // already-approved rows here (so they have somewhere to reopen one for a post-approval edit),
    // hence Status is included - MDO/MO only ever see Pending rows, so it's always "Pending" for them.
    public class PendingAllocationRowDto
    {
        public int AllocationId { get; set; }
        public string Status { get; set; } = "";
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
        // Minimal per-dealer info so the client can filter rows by District/Taluk/Dealer the
        // same way TasksAllocation.razor's own Dealer tab filters its dealer grid - this list
        // only needs to be enough to drive those three dropdowns, not full dealer details.
        public List<PendingAllocationDealerFilterDto> Dealers { get; set; } = new();
    }

    public class PendingAllocationDealerFilterDto
    {
        public int SubDealerId { get; set; }
        public string DealerName { get; set; } = "";
        public string DealerCode { get; set; } = "";
        public int? DistrictId { get; set; }
        public string? Taluk { get; set; }
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
        // Set by the MDO during review, not by the JMDO at submission - optional, defaults to Normal.
        public DealerTaskPriority Priority { get; set; }
        // Resolved from the live SubDealerRegistration row (not snapshotted at submission like
        // DealerName/DealerCode) so the District/Taluk filters on this page reflect the dealer's
        // current location, not whatever it was when the JMDO submitted.
        public int? DistrictId { get; set; }
        public string? Taluk { get; set; }
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

    public class UpdateDealerPriorityDto
    {
        public DealerTaskPriority Priority { get; set; }
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

    // One flattened row per individual task (one dealer visit, one MD program, or one of the
    // Spcm/SoilSample/PosLiquidation targets) for the Task Monitoring report - GetPending shows
    // one row per ALLOCATION (a JMDO's whole week submission); this shows one row per task inside
    // it, which is what "who has this task, where it stands, since when" actually needs.
    public class TaskMonitoringRowDto
    {
        public int AllocationId { get; set; }
        public string JmdoName { get; set; } = "";
        public string? JmdoCode { get; set; }
        public string TaskType { get; set; } = ""; // Dealer / SPCM / Soil Sample / MD Program / PoS Liquidation
        public string? Location { get; set; } // Headquarter name, same source as PendingAllocationRowDto.Location
        public int? StateId { get; set; }
        public int? RegionId { get; set; }
        public DateTime? Due { get; set; }
        // Requested (allocation still Pending MDO approval) / Yet to Start / In Progress / Completed -
        // Rejected allocations are excluded entirely upstream (their tasks are dead, not "requested").
        public string Status { get; set; } = "";
    }

    // GET api/JmdoTaskAllocation/mine/summary - the signed-in JMDO's own allocation for the current
    // Sunday-Saturday week, counted with the same "what is a task" rules as the Monitoring report.
    // Allocated always equals Completed + InProgress + Pending (Pending = YetToStart only).
    public class JmdoTaskSummaryDto
    {
        public DateTime WeekStart { get; set; } // Sunday
        public DateTime WeekEnd { get; set; }   // Saturday (inclusive)

        public bool HasAllocation { get; set; }
        public JmdoTaskAllocationStatus? AllocationStatus { get; set; }

        public int Allocated { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Pending { get; set; }

        // Nearby covers dealer tasks only - no other task type has coordinates.
        public int PendingDealerTasks { get; set; }
        public int PendingDealerTasksWithCoordinates { get; set; }
        /// <summary>Null when no valid device location was supplied (not "zero nearby").</summary>
        public int? NearbyPendingDealerTasks { get; set; }
        public double NearbyRadiusKm { get; set; }

        // The same tasks as Allocated above, split so each is counted exactly once:
        //   Days            - dealer visits, under their planned day (PlannedDate, else PlannedDay), Sunday first, always 7;
        //   WeeklyTasks     - MD Program / SPCM / Soil Sample / PoS: no day exists for these in the data model;
        //   UnscheduledDealerTasks - dealer visits with no day, or a PlannedDate outside this week.
        // Sum of Days + WeeklyTasks + UnscheduledDealerTasks == the totals above.
        public List<JmdoTaskDayCountsDto> Days { get; set; } = new();
        public JmdoTaskCountsDto WeeklyTasks { get; set; } = new();
        public JmdoTaskCountsDto UnscheduledDealerTasks { get; set; } = new();
    }

    // Allocated == Completed + InProgress + Pending (Pending = YetToStart only).
    public class JmdoTaskCountsDto
    {
        public int Allocated { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Pending { get; set; }
    }

    public class JmdoTaskDayCountsDto : JmdoTaskCountsDto
    {
        public DateTime Date { get; set; }
    }
}
