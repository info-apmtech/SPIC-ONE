namespace SPIC.Core.DTOs
{
    public class TeamJmdoDto
    {
        public int EmployeeLoginId { get; set; }
        public string Name { get; set; } = "";
        public string? EmployeeCode { get; set; }
        public bool IsActive { get; set; }
        public string? HeadquarterName { get; set; }
        // Today's JMDOAttendance row, if any - "On Leave" when AttendanceStatus
        // is OnLeave for today, "Active" otherwise (including no record yet).
        public bool IsOnLeaveToday { get; set; }
        // Today's DutyStatus == OnDuty - used as the closest real proxy for
        // "being worked right now" (there's no per-task in-progress state).
        public bool IsOnDutyToday { get; set; }
        // Counted across all of this JMDO's JmdoTaskAllocation (weekly) submissions,
        // not individual tasks - there's no per-task progress tracking today.
        public int AssignedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        // Approved allocations whose ReviewedAt falls today.
        public int ApprovedTodayCount { get; set; }
    }

    // GET api/Team/yearly-targets row - one saved JmdoYearlyTarget for the current FY.
    public class JmdoYearlyTargetDto
    {
        public int EmployeeLoginId { get; set; }
        public string Name { get; set; } = "";
        public string? EmployeeCode { get; set; }
        public string? HeadquarterName { get; set; }
        public int FinancialYearStart { get; set; }
        public int SpcmTarget { get; set; }
        public int SoilSampleTarget { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // PUT api/Team/yearly-targets body. The financial year is decided server-side (current
    // April-March FY), never taken from the client.
    public class SaveJmdoYearlyTargetDto
    {
        public int EmployeeLoginId { get; set; }
        public int? SpcmTarget { get; set; }
        public int? SoilSampleTarget { get; set; }
    }

    // GET api/Team/reports - feeds MdoReports.razor ("Reports"). Same HQ-scoped team as
    // api/Team/jmdos, "this month" = the calendar month containing DateTime.Today (same
    // convention JmdoTaskAllocationController.GetMyReports uses for the per-JMDO "My Reports"
    // page this mirrors at team level).
    public class MdoTeamReportsDto
    {
        public int TasksClosedThisMonth { get; set; }
        // Null when nothing completed this month has a measurable assign-to-close gap.
        public double? AvgTurnaroundDays { get; set; }
        public int SoilSamplesThisMonth { get; set; }
        // All-time, not monthly - matches the "Region-wide" card label (distinct from the
        // JMDO-wise table's own "Farmers Added" column, which IS scoped to this month).
        public int FarmersCoveredAllTime { get; set; }

        public List<MdoReportJmdoSummaryRowDto> JmdoSummaries { get; set; } = new();
        public List<MdoReportDetailRowDto> DetailedTasks { get; set; } = new();
    }

    public class MdoReportJmdoSummaryRowDto
    {
        public string JmdoName { get; set; } = "";
        public int TasksClosedThisMonth { get; set; }
        // Null when this JMDO closed no dealer visit this month - On-time % is only
        // evaluable for dealer visits (the only task kind with a real due date, PlannedDate).
        // SPCM/Soil/PoS/MD Program have no per-task due date in this data model.
        public double? OnTimePercent { get; set; }
        public int SoilSamplesThisMonth { get; set; }
        public int FarmersAddedThisMonth { get; set; }
    }

    public class MdoReportDetailRowDto
    {
        public string JmdoName { get; set; } = "";
        public string Type { get; set; } = ""; // Dealer / SPCM / MD Program / Soil Sample / PoS Liquidation
        public string Summary { get; set; } = "";
        // The closeout's own Remarks, where one exists - "—" when the task hasn't been closed
        // out yet (YetToStart/InProgress), or for Soil Sample, which has no closeout at all.
        public string Resolution { get; set; } = "";
        // Not Started / In Progress / the closeout's own Outcome once Completed (Fully
        // Completed / Partially Completed / Not Completed) - falls back to "Completed" when
        // no closeout Outcome was recorded.
        public string Status { get; set; } = "";
        public DateTime TaskDate { get; set; }
        // Null (rendered as "—") when there's no StartedAt to measure from.
        public double? HoursTaken { get; set; }
    }
}
