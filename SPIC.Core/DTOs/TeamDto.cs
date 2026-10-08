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
}
