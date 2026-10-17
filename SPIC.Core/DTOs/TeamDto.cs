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
}
