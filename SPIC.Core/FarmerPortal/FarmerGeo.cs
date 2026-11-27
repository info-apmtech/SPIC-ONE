namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Distances for the farmer portal (dealer locator, nearest dealer for "My SPIC team"). Pure maths,
/// pinned by SPIC.RegressionHarness (FarmerPortalConnectTests).
/// </summary>
public static class FarmerGeo
{
    /// <summary>Mean Earth radius used by the haversine formula.</summary>
    public const double EarthRadiusKm = 6371.0088;

    /// <summary>Kilometres per degree of latitude (and of longitude at the equator).</summary>
    public const double KmPerDegree = 111.32;

    /// <summary>
    /// A usable GPS point: finite, inside the lat / lng ranges and not the 0,0 "no fix" value a
    /// device or a blank form sends.
    /// </summary>
    public static bool IsValid(double? lat, double? lng) =>
        lat is { } a && lng is { } b
        && double.IsFinite(a) && double.IsFinite(b)
        && a is >= -90 and <= 90 && b is >= -180 and <= 180
        && !(Math.Abs(a) < 1e-9 && Math.Abs(b) < 1e-9);

    /// <summary>Great-circle distance in km between two points (haversine).</summary>
    public static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        static double Rad(double deg) => deg * Math.PI / 180.0;
        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>
    /// A lat / lng box that contains every point within <paramref name="radiusKm"/> of the centre, so
    /// the database can pre-filter before the exact haversine check. Longitude span grows towards the
    /// poles; it is capped at the whole range.
    /// </summary>
    public static (double MinLat, double MaxLat, double MinLng, double MaxLng) BoundingBox(double lat, double lng, double radiusKm)
    {
        var dLat = radiusKm / KmPerDegree;
        var cos = Math.Cos(lat * Math.PI / 180.0);
        var dLng = cos < 1e-6 ? 180 : Math.Min(180, radiusKm / (KmPerDegree * cos));
        return (lat - dLat, lat + dLat, lng - dLng, lng + dLng);
    }

    /// <summary>
    /// Rounds a coordinate to 3 decimals (about 110 m) before the app sends it to the API: precise enough
    /// to sort dealers by distance, without sending the farmer's exact position.
    /// </summary>
    public static double Coarse(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
