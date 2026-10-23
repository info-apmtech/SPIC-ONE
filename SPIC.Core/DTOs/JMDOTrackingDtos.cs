using SPIC.Core.Entities;

namespace SPIC.Core.DTOs;

/// <summary>
/// JMDO Tracking API contracts (shared by SpicAPI and the Blazor client).
/// Controller "JMDOTracking", [Authorize], restricted to AppRole.JMDO (api/JMDOTracking/team is the
/// one exception - Admin/MO/MDO, HQ-scoped like api/Team/jmdos).
///
///   POST api/JMDOTracking/start                       -> JMDOTrackingSessionDto
///         (body: JMDOTrackingStartDto; idempotent - creates today's Active session against the
///         caller's own JMDOAttendance row, or returns the caller's existing Active session.
///         Independent of DutyStatus - only requires today's attendance to exist.)
///   GET  api/JMDOTracking/mine/active                  -> JMDOTrackingSessionDto, or 204
///         (the caller's current Active session, if any, with its computed LiveStatus - lets any
///         page reflect real tracking state without starting a session as a side effect)
///   POST api/JMDOTracking/sessions/{sessionId}/points  -> 200
///         (body: JMDOTrackingPointUpsertDto; rejected unless sessionId is the caller's own
///         session and it is still Active)
///   POST api/JMDOTracking/sessions/{sessionId}/points/batch -> 200
///         (body: List&lt;JMDOTrackingPointUpsertDto&gt;, capped at 500; offline-queue flush-on-
///         reconnect - each point's own DeviceCapturedAt is preserved, inserted oldest first)
///   POST api/JMDOTracking/stop                        -> JMDOTrackingSessionDto
///         (body: JMDOTrackingStopDto; completes the caller's own Active session - no session id
///         is accepted from the client, it is resolved the same way Start resolves it; saves one
///         final JMDOTrackingPoint, sets EndTime/Duration/Status=Completed; 400 if the caller has
///         no Active session, e.g. already stopped. This is the ONLY path that produces
///         Status=Completed - never attendance, never a stale-session cleanup, never a task event.
///         Does not touch JMDOAttendance.DutyStatus.)
///   GET  api/JMDOTracking/team                         -> List&lt;JMDOTrackingTeamRowDto&gt;
///         (supervisor visibility - the caller's own HQ's JMDOs only)
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

/// <summary>Live/health view of a session, derived (never stored) from Status + the last point the
/// server actually received - see JMDOTrackingController.ResolveLiveStatus. Deliberately NOT based
/// on anything a device claims locally (e.g. an offline queue it's still holding), so a device that's
/// been offline for an hour correctly reads Offline here even while it keeps collecting points to
/// flush later.</summary>
public enum JmdoTrackingLiveStatus
{
    /// <summary>Active, and a point arrived within the last 2x the configured interval.</summary>
    Live = 0,
    /// <summary>Active, but the last point is older than that - a missed beat or two, not yet alarming.</summary>
    Stale = 1,
    /// <summary>Active, but nothing has arrived in 15+ minutes - likely out of signal, backgrounded
    /// past what the OS allows, or the device is off.</summary>
    Offline = 2,
    /// <summary>Server-side cleanup closed this session after a long silence (see
    /// JmdoTracking:AutoEndAfterHours) - distinct from Stopped on purpose: nobody pressed Stop.</summary>
    AutoEnded = 3,
    /// <summary>The JMDO explicitly pressed Stop Live Tracking. The only way to reach this state.</summary>
    Stopped = 4,
    /// <summary>Team view only - this JMDO has never started a tracking session at all (not even a
    /// past, now-ended one). Distinct from Stopped/AutoEnded, which both imply one existed once.</summary>
    NotStarted = 5
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

    public JmdoTrackingLiveStatus LiveStatus { get; set; }
    public DateTime? LastPointAt { get; set; }
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
}

public class JMDOTrackingPointUpsertDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Accuracy { get; set; }

    // Optional - the device's own capture time. Defaults to "now" server-side when omitted (older
    // clients / the normal single-point live ping), but is required in practice for a point coming
    // out of an offline queue, where it can be well before the moment it's finally uploaded.
    public DateTime? DeviceCapturedAt { get; set; }
}

// GET api/JMDOTracking/team row - one per JMDO on the caller's (Admin/MO/MDO) own HQ team.
public class JMDOTrackingTeamRowDto
{
    public string UserId { get; set; } = "";
    public string JmdoName { get; set; } = "";
    public JmdoTrackingLiveStatus LiveStatus { get; set; }
    public DateTime? LastPointAt { get; set; }
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public DateTime? SessionStartTime { get; set; }
}

// GET api/JMDOTracking/sessions/{sessionId}/points - one row per recorded GPS fix, oldest first, for
// drawing a session's route on a map. RecordedAt (server-received) is what orders/paginates the
// data server-side; DeviceCapturedAt is exposed too since it's what actually reflects real movement
// time for a point that was flushed from an offline queue well after it was captured.
public class JMDOTrackingPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime DeviceCapturedAt { get; set; }
    public double? Accuracy { get; set; }
}
