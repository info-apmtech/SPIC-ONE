using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// JMDO Attendance API contracts (shared by SpicAPI and the Blazor client).
/// Controller "JMDOAttendance", [Authorize], write restricted to AppRole.JMDO.
///
///   GET  api/JMDOAttendance/today            -> JMDOAttendanceDto? (the caller's own row for today, or null)
///   POST api/JMDOAttendance                  -> JMDOAttendanceDto (body: JMDOAttendanceUpsertDto; upserts by UserId+Date)
///
/// Enums serialize as integers (no string converter in the API).
/// </summary>
public class JMDOAttendanceUpsertDto
{
    public DateTime Date { get; set; }
    public string VisitArea { get; set; } = "";
    public JMDOAttendanceStatus AttendanceStatus { get; set; }
    /// <summary>GPS fix captured at the moment Update Attendance was clicked. Required; the server rejects a missing or out-of-range value.</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class JMDOAttendanceDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string? UserName { get; set; }
    public DateTime Date { get; set; }
    public string VisitArea { get; set; } = "";
    public JMDOAttendanceStatus AttendanceStatus { get; set; }
    public JMDODutyStatus DutyStatus { get; set; }
    public DateTime? AttendanceUpdatedAt { get; set; }
    public double? AttendanceLatitude { get; set; }
    public double? AttendanceLongitude { get; set; }
}
