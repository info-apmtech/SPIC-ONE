using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// JMDO Attendance API contracts (shared by SpicAPI and the Blazor client).
/// Controller "JMDOAttendance", [Authorize], write restricted to AppRole.JMDO.
///
///   GET  api/JMDOAttendance/today            -> JMDOAttendanceDto? (the caller's own row for today, or null)
///   POST api/JMDOAttendance                  -> JMDOAttendanceDto (body: JMDOAttendanceUpsertDto; upserts by UserId+Date)
///   GET  api/JMDOAttendance/week             -> JMDOWeeklySummaryDto (the caller's own current Sunday-Saturday week)
///
/// Enums serialize as integers (no string converter in the API).
/// </summary>
public class JMDOAttendanceUpsertDto
{
    public DateTime Date { get; set; }
    public string? VisitArea { get; set; }
    public JMDOAttendanceStatus AttendanceStatus { get; set; }
    /// <summary>GPS fix captured at the moment Update Attendance was clicked. Nullable if location unavailable.</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class JMDOAttendanceDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string? UserName { get; set; }
    public DateTime Date { get; set; }
    public string? VisitArea { get; set; }
    public JMDOAttendanceStatus AttendanceStatus { get; set; }
    public JMDODutyStatus DutyStatus { get; set; }
    public DateTime? AttendanceUpdatedAt { get; set; }
    public double? AttendanceLatitude { get; set; }
    public double? AttendanceLongitude { get; set; }
}

/// <summary>
/// The caller's current Sunday-Saturday week. WeekStart is Sunday 00:00 and WeekEnd is the Saturday
/// (date only, inclusive) - records are queried with the half-open range [WeekStart, WeekStart + 7 days).
/// Always exactly 7 Days, Sunday first, including days with no attendance.
/// </summary>
public class JMDOWeeklySummaryDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public List<JMDOWeeklyDayDto> Days { get; set; } = new();
    public TimeSpan TotalWorkingDuration { get; set; }
    public int TotalTasksStarted { get; set; }
    public double TotalDistanceKm { get; set; }
}

public class JMDOWeeklyDayDto
{
    public DateTime Date { get; set; }
    /// <summary>Null when no attendance was marked for this day.</summary>
    public JMDOAttendanceStatus? AttendanceStatus { get; set; }
    public JMDODutyStatus? DutyStatus { get; set; }
    public string? VisitArea { get; set; }
    /// <summary>Sum of this day's tracking sessions (an Active session counts up to "now").</summary>
    public TimeSpan WorkingDuration { get; set; }
    /// <summary>Task-allocation tasks (dealer, MD program, SPCM, soil, PoS) whose StartedAt falls on this day.</summary>
    public int TasksStarted { get; set; }
    /// <summary>Estimate: straight-line distance between consecutive recorded GPS points per session - not road distance.</summary>
    public double DistanceKm { get; set; }
}
