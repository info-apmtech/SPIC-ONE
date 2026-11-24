namespace SPIC.Core.Entities;

// One row = "this specific task instance currently depends on this tracking session." Deleted the
// moment the task no longer needs it (JmdoTaskAllocationController.ReleaseTaskTrackingAsync), or
// whenever the session itself stops being Active through any other path (JMDOTrackingController.Stop,
// CloseStaleSessionsAsync) - a link must never outlive its session.
//
// Keyed by (AllocationId, TaskKey) rather than (UserId, TaskKey): a user's SPCM/Soil Sample/PoS
// task key ("spcm", "soilsample", "posliquidation") recurs identically across every weekly
// JmdoTaskAllocation row they're ever assigned, so AllocationId is what actually disambiguates one
// real task instance from another with the same key in a different week. There is no unified
// "Task" entity in this codebase - tasks are string TaskKeys scoped to one JmdoTaskAllocation row,
// which is exactly what (AllocationId, TaskKey) captures.
public class JmdoTaskTrackingLink
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int AllocationId { get; set; }
    public string TaskKey { get; set; } = string.Empty;
    public int TrackingSessionId { get; set; }
    public JMDOTrackingSession? TrackingSession { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
