namespace SPIC.Core.Utilities;

public interface IGpsPoint
{
    double Latitude { get; }
    double Longitude { get; }
    double? Accuracy { get; }
    DateTime RecordedAt { get; }
}

public class GpsTrajectoryPoint : IGpsPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime RecordedAt { get; set; }
    public int SegmentIndex { get; set; }
}

/// <summary>
/// Robust GPS filtering, outlier rejection, route segmentation, and travel distance calculation.
/// Prevents false GPS coordinate jumps, cellular triangulation bounces, and stationary jitter
/// from distorting tracking routes, marker animations, and travel distance summaries.
/// </summary>
public static class GpsTrajectoryFilter
{
    public const double DefaultMaxAccuracyMeters = 200.0;
    public const double DefaultMaxSpeedKmh = 140.0;
    public const double DefaultMinMovementMeters = 5.0;
    public const double MaxDiscontinuityGapSeconds = 900.0; // 15 minutes
    public const double MaxDiscontinuityGapMeters = 1500.0;

    private const double EarthRadiusMeters = 6371000.0;

    /// <summary>
    /// Rejects null, NaN, out-of-range, and near-(0,0) Null Island coordinates.
    /// </summary>
    public static bool IsValidCoordinate(double? lat, double? lng)
    {
        if (lat is null || lng is null) return false;
        if (double.IsNaN(lat.Value) || double.IsInfinity(lat.Value)) return false;
        if (double.IsNaN(lng.Value) || double.IsInfinity(lng.Value)) return false;
        return lat.Value is >= -90 and <= 90
            && lng.Value is >= -180 and <= 180
            && (Math.Abs(lat.Value) > 0.01 || Math.Abs(lng.Value) > 0.01);
    }

    /// <summary>
    /// Calculates Great-Circle distance between two coordinates in meters via the Haversine formula.
    /// </summary>
    public static double HaversineDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;
        var rLat1 = lat1 * Math.PI / 180.0;
        var rLat2 = lat2 * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0)
            + Math.Cos(rLat1) * Math.Cos(rLat2) * Math.Sin(dLng / 2.0) * Math.Sin(dLng / 2.0);
        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Filters out inaccurate fixes and transient kinematic speed-spike bounces (e.g. A -> B -> A).
    /// </summary>
    public static List<T> CleanPoints<T>(
        IEnumerable<T> points,
        double maxAccuracyMeters = DefaultMaxAccuracyMeters,
        double maxSpeedKmh = DefaultMaxSpeedKmh) where T : IGpsPoint
    {
        var valid = new List<T>();
        foreach (var p in points)
        {
            if (!IsValidCoordinate(p.Latitude, p.Longitude)) continue;
            if (p.Accuracy.HasValue && p.Accuracy.Value > maxAccuracyMeters) continue;
            valid.append_if_valid(p);
        }

        if (valid.Count <= 2) return valid;

        // Kinematic bounce outlier removal:
        // Detects when point B implies impossible speed from A, and is immediately followed
        // by point C returning back to the vicinity of A at plausible speed.
        var cleaned = new List<T>(valid.Count) { valid[0] };
        var i = 1;
        while (i < valid.Count)
        {
            var curr = valid[i];
            var prev = cleaned[^1];

            var distPrev = HaversineDistanceMeters(prev.Latitude, prev.Longitude, curr.Latitude, curr.Longitude);
            var dtPrev = (curr.RecordedAt - prev.RecordedAt).TotalSeconds;

            if (dtPrev <= 0)
            {
                if (distPrev < 20.0) cleaned.Add(curr);
                i++;
                continue;
            }

            var speedPrevKmh = (distPrev / dtPrev) * 3.6;

            if (speedPrevKmh > maxSpeedKmh && distPrev > 80.0)
            {
                var isBounce = false;
                var maxLookahead = Math.Min(i + 4, valid.Count);
                for (var k = i + 1; k < maxLookahead; k++)
                {
                    var nxt = valid[k];
                    var dtNxt = (nxt.RecordedAt - curr.RecordedAt).TotalSeconds;
                    if (dtNxt <= 0) continue;

                    var distNxt = HaversineDistanceMeters(curr.Latitude, curr.Longitude, nxt.Latitude, nxt.Longitude);
                    var distPrevNxt = HaversineDistanceMeters(prev.Latitude, prev.Longitude, nxt.Latitude, nxt.Longitude);
                    var dtPrevNxt = (nxt.RecordedAt - prev.RecordedAt).TotalSeconds;
                    var speedPrevNxt = dtPrevNxt > 0 ? (distPrevNxt / dtPrevNxt) * 3.6 : 0;

                    if (distNxt > 80.0 && speedPrevNxt <= maxSpeedKmh)
                    {
                        isBounce = true;
                        break;
                    }
                }

                if (isBounce)
                {
                    // Discard curr as a transient bounce outlier
                    i++;
                    continue;
                }
            }

            cleaned.Add(curr);
            i++;
        }

        return cleaned;
    }

    private static void append_if_valid<T>(this List<T> list, T item) => list.Add(item);

    /// <summary>
    /// Splits cleaned points into discrete route segments separated by time/distance discontinuities.
    /// Prevents straight polylines from being drawn across periods when tracking was paused or lost.
    /// </summary>
    public static List<List<T>> SegmentPoints<T>(
        IEnumerable<T> points,
        double maxGapSeconds = MaxDiscontinuityGapSeconds,
        double maxGapMeters = MaxDiscontinuityGapMeters) where T : IGpsPoint
    {
        var segments = new List<List<T>>();
        var currentSegment = new List<T>();

        T? prev = default;
        foreach (var curr in points)
        {
            if (prev is not null)
            {
                var dt = (curr.RecordedAt - prev.RecordedAt).TotalSeconds;
                var dist = HaversineDistanceMeters(prev.Latitude, prev.Longitude, curr.Latitude, curr.Longitude);

                if (dt > maxGapSeconds || (dt > 120.0 && dist > maxGapMeters))
                {
                    if (currentSegment.Count > 0)
                    {
                        segments.Add(currentSegment);
                        currentSegment = new List<T>();
                    }
                }
            }

            currentSegment.Add(curr);
            prev = curr;
        }

        if (currentSegment.Count > 0)
        {
            segments.Add(currentSegment);
        }

        return segments;
    }

    /// <summary>
    /// Calculates true travel distance in kilometers by cleaning raw points, segmenting discontinuities,
    /// and filtering out stationary GPS jitter (< minMovementMeters).
    /// </summary>
    public static double CalculateTravelDistanceKm<T>(
        IEnumerable<T> points,
        double maxAccuracyMeters = DefaultMaxAccuracyMeters,
        double maxSpeedKmh = DefaultMaxSpeedKmh,
        double minMovementMeters = DefaultMinMovementMeters) where T : IGpsPoint
    {
        var cleaned = CleanPoints(points, maxAccuracyMeters, maxSpeedKmh);
        if (cleaned.Count < 2) return 0.0;

        var segments = SegmentPoints(cleaned);
        double totalMeters = 0.0;

        foreach (var segment in segments)
        {
            if (segment.Count < 2) continue;

            T anchor = segment[0];
            for (var i = 1; i < segment.Count; i++)
            {
                var curr = segment[i];
                var dist = HaversineDistanceMeters(anchor.Latitude, anchor.Longitude, curr.Latitude, curr.Longitude);

                // Filter out small stationary jitter (< minMovementMeters)
                if (dist >= minMovementMeters)
                {
                    totalMeters += dist;
                    anchor = curr;
                }
            }
        }

        return Math.Round(totalMeters / 1000.0, 2);
    }
}
