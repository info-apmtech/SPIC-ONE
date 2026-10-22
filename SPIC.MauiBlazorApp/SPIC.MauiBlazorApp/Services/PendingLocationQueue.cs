using System.Text.Json;

namespace SPIC.MauiBlazorApp.Services
{
    /// <summary>
    /// A GPS fix the background tracking service captured but could not upload (no connectivity, or
    /// the server rejected it after a token refresh attempt also failed). Modeled directly on
    /// SPIC.Ifms.Relay's PendingSmsQueue - same Preferences-backed JSON list, same "drain on every
    /// tick" shape - adapted for location points instead of SMS payloads.
    ///
    /// DeviceCapturedAt is stamped at the moment of the GPS fix, not at upload time, so a point that
    /// sat in this queue for an hour still reports truthfully when it was actually captured once
    /// flushed (JMDOTrackingPoint.DeviceCapturedAt / the points/batch endpoint).
    /// </summary>
    public sealed class PendingLocationPoint
    {
        public int SessionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public DateTime DeviceCapturedAt { get; set; }
    }

    public static class PendingLocationQueue
    {
        private const string Key = "spic_jmdo_pending_location_points";

        // Points this old are almost certainly from an abandoned/auto-ended session by the time
        // connectivity returns - no point uploading a decayed trail, and this keeps the queue from
        // growing unbounded if the device is offline for days.
        private static readonly TimeSpan MaxAge = TimeSpan.FromHours(26);

        // JMDOTrackingController.AddPointsBatch caps a single batch at 500 - the queue itself allows
        // a bit more headroom locally so a short network outage can't silently start dropping fixes
        // before the next successful flush gets a chance to catch up.
        private const int MaxQueueSize = 2000;

        private static readonly object Gate = new();

        public static int Count
        {
            get { lock (Gate) return Load().Count; }
        }

        public static void Enqueue(PendingLocationPoint point)
        {
            lock (Gate)
            {
                var items = Load();
                items.Add(point);

                if (items.Count > MaxQueueSize)
                {
                    // Drop the oldest first - a recent fix is more useful than an ancient one once
                    // the queue is this backed up.
                    items = items
                        .OrderByDescending(p => p.DeviceCapturedAt)
                        .Take(MaxQueueSize)
                        .ToList();
                }

                Save(items);
            }
        }

        /// <summary>Every point still queued for the given session, oldest first - the caller is
        /// responsible for calling <see cref="RemoveUpTo"/> once the upload actually succeeds (this
        /// type never assumes success on its own).</summary>
        public static List<PendingLocationPoint> PeekForSession(int sessionId)
        {
            lock (Gate)
            {
                return Load()
                    .Where(p => p.SessionId == sessionId
                        && DateTime.UtcNow - p.DeviceCapturedAt <= MaxAge)
                    .OrderBy(p => p.DeviceCapturedAt)
                    .ToList();
            }
        }

        /// <summary>Removes the given points (by reference identity of DeviceCapturedAt+session -
        /// points are never mutated after Enqueue, so this is a safe-enough match) after a
        /// successful upload, and drops anything that's aged out while it waited.</summary>
        public static void RemoveUpTo(int sessionId, IReadOnlyCollection<PendingLocationPoint> uploaded)
        {
            lock (Gate)
            {
                var items = Load();
                var uploadedSet = uploaded.Select(p => p.DeviceCapturedAt).ToHashSet();

                var remaining = items
                    .Where(p => !(p.SessionId == sessionId && uploadedSet.Contains(p.DeviceCapturedAt)))
                    .Where(p => DateTime.UtcNow - p.DeviceCapturedAt <= MaxAge)
                    .ToList();

                Save(remaining);
            }
        }

        private static List<PendingLocationPoint> Load()
        {
            try
            {
                var json = Preferences.Default.Get(Key, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<PendingLocationPoint>();

                return JsonSerializer.Deserialize<List<PendingLocationPoint>>(json) ?? new List<PendingLocationPoint>();
            }
            catch
            {
                return new List<PendingLocationPoint>();
            }
        }

        private static void Save(List<PendingLocationPoint> items)
        {
            try
            {
                if (items.Count == 0)
                    Preferences.Default.Remove(Key);
                else
                    Preferences.Default.Set(Key, JsonSerializer.Serialize(items));
            }
            catch
            {
                // Losing the queue is no worse than not having one.
            }
        }
    }
}
