using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// JMDO Tracking API contracts (shared by SpicAPI and the Blazor client).
/// Controller "JMDOTracking", [Authorize], restricted to AppRole.JMDO.
///
///   POST api/JMDOTracking/start                       -> JMDOTrackingSessionDto
///         (body: JMDOTrackingStartDto; idempotent - creates today's Active session against the
///         caller's own JMDOAttendance row, or returns the caller's existing Active session)
///   POST api/JMDOTracking/sessions/{sessionId}/points  -> 200
///         (body: JMDOTrackingPointUpsertDto; rejected unless sessionId is the caller's own
///         session and it is still Active)
///   POST api/JMDOTracking/stop                        -> JMDOTrackingSessionDto
///         (body: JMDOTrackingStopDto; completes the caller's own Active session - no session id
///         is accepted from the client, it is resolved the same way Start resolves it; saves one
///         final JMDOTrackingPoint, sets EndTime/Duration/Status=Completed; 400 if the caller has
///         no Active session, e.g. already stopped)
///
/// View Tracking Details is a later step - not part of this contract.
/// Enums serialize as integers (no string converter in the API).
/// </summary>
public class JMDOTrackingStartDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class JMDOTrackingStopDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }
}

public class JMDOTrackingSessionDto
{
    public int Id { get; set; }
    public int AttendanceId { get; set; }
    public string UserId { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public JMDOTrackingSessionStatus Status { get; set; }
}

public class JMDOTrackingPointUpsertDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }
}
