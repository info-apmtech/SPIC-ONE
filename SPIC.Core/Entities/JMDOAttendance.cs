namespace SPIC.Core.Entities;

// ------------------------------------------------------------------------------------------------
// JMDO Attendance and Tracking: daily attendance for a JMDO user plus the GPS tracking sessions
// and points recorded against that attendance.
// ------------------------------------------------------------------------------------------------

public enum JMDOAttendanceStatus { Present = 0, Absent = 1, HalfDay = 2, OnLeave = 3 }
public enum JMDODutyStatus { OnDuty = 0, OffDuty = 1, OnBreak = 2 }
public enum JMDOTrackingSessionStatus { Active = 0, Completed = 1, Cancelled = 2 }

/// <summary>One day's attendance record for a JMDO user.</summary>
public class JMDOAttendance
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime Date { get; set; }
    public string? VisitArea { get; set; }
    public JMDOAttendanceStatus AttendanceStatus { get; set; }
    public JMDODutyStatus DutyStatus { get; set; }
    public DateTime? AttendanceUpdatedAt { get; set; }
    /// <summary>GPS fix captured at the moment attendance was marked (device geolocation, not reverse-geocoded).</summary>
    public double? AttendanceLatitude { get; set; }
    public double? AttendanceLongitude { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<JMDOTrackingSession> TrackingSessions { get; set; } = new List<JMDOTrackingSession>();
}

/// <summary>One continuous GPS tracking session recorded against a JMDOAttendance day.</summary>
public class JMDOTrackingSession
{
    public int Id { get; set; }
    public int AttendanceId { get; set; }
    public JMDOAttendance? Attendance { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public double? EndLatitude { get; set; }
    public double? EndLongitude { get; set; }
    public JMDOTrackingSessionStatus Status { get; set; } = JMDOTrackingSessionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<JMDOTrackingPoint> Points { get; set; } = new List<JMDOTrackingPoint>();
}

/// <summary>One GPS point recorded within a JMDOTrackingSession.</summary>
public class JMDOTrackingPoint
{
    public int Id { get; set; }
    public int TrackingSessionId { get; set; }
    public JMDOTrackingSession? TrackingSession { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime RecordedAt { get; set; }
    public double? Accuracy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
