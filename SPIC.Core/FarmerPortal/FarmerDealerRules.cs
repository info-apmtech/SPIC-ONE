using System.Linq.Expressions;
using SPIC.Core.Entities;

namespace SPIC.Core.FarmerPortal;

/// <summary>Which search answered the dealer locator (docs/farmer-portal.md, Task 2).</summary>
public enum FarmerDealerScope
{
    /// <summary>Dealers within the radius of the farmer's GPS point, nearest first.</summary>
    Nearby,
    /// <summary>No dealer within the radius (or no GPS): dealers in the farmer's district.</summary>
    District,
    /// <summary>None in the district either: dealers in the farmer's state.</summary>
    State,
    /// <summary>Nothing found at all.</summary>
    None
}

/// <summary>One dealer as the locator ranks it: only what the ranking needs (no personal data).</summary>
public sealed record FarmerDealerPoint(int Id, string Name, int? DistrictId, int StateId, double? Latitude, double? Longitude);

/// <summary>A ranked dealer with its distance from the farmer (null when either side has no GPS).</summary>
public sealed record FarmerDealerHit(FarmerDealerPoint Dealer, double? DistanceKm);

/// <summary>
/// Dealer locator rules (api/Farmer/dealers). Which dealers a farmer may see, the radius and paging
/// limits, the nearby -> district -> state fallback and the ordering. Pure, pinned by the harness.
/// </summary>
public static class FarmerDealerRules
{
    public const int DefaultRadiusKm = 25;
    public const int MinRadiusKm = 1;
    public const int MaxRadiusKm = 100;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int SearchMax = 60;

    /// <summary>Radius in km: default 25 when missing, clamped to 1..100.</summary>
    public static int ClampRadius(int? radiusKm) =>
        radiusKm is null ? DefaultRadiusKm : Math.Clamp(radiusKm.Value, MinRadiusKm, MaxRadiusKm);

    /// <summary>(page >= 1, pageSize 1..50, default 20).</summary>
    public static (int Page, int PageSize) ClampPaging(int? page, int? pageSize) =>
        (Math.Max(1, page ?? 1), pageSize is null or < 1 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize));

    /// <summary>The name / village search text, trimmed and cut to 60 characters; null when blank.</summary>
    public static string? CleanSearch(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return null;
        var t = q.Trim();
        return t.Length > SearchMax ? t[..SearchMax] : t;
    }

    /// <summary>
    /// Dealers a farmer may be sent to: Status Active (never InActive / Terminated), not marked
    /// "to be terminated" or "not traceable" (InactiveProposal), a real dealer (not a Department or
    /// Institution registration), with a firm name. A registration made through the Create New Dealer
    /// flow counts only after its FINAL (AVP) approval gave it a dealer code: until then its Status
    /// is the default Active although it is not trading yet (DealerRegistrationsController sets Active
    /// at the final AVP approval). Existing / bulk-loaded dealers are trading dealers already, so their
    /// Status alone decides (their approval flags describe a later credit-limit review).
    /// EF-translatable; <see cref="IsListable"/> is the compiled form.
    /// </summary>
    public static readonly Expression<Func<DealerRegistration, bool>> ListableForFarmers = d =>
        d.Status == DealerStatus.Active
        && (d.InactiveProposal == null
            || (d.InactiveProposal != FutureBusinessProposal.Terminated && d.InactiveProposal != FutureBusinessProposal.NotTraceable))
        && d.IsDealer
        && (d.DealerType == null || d.DealerType == RegistrationDealerType.Dealer)
        && d.FirmName != null && d.FirmName != ""
        && (!d.IsNewDealerRegistration || (d.AVPApproved == true && d.DealerCode != null && d.DealerCode != ""));

    private static readonly Func<DealerRegistration, bool> ListableCompiled = ListableForFarmers.Compile();

    public static bool IsListable(DealerRegistration dealer) => ListableCompiled(dealer);

    /// <summary>Dealers within <paramref name="radiusKm"/> of the farmer, nearest first (then by name).</summary>
    public static List<FarmerDealerHit> Nearby(IEnumerable<FarmerDealerPoint> dealers, double lat, double lng, int radiusKm) =>
        dealers
            .Where(d => FarmerGeo.IsValid(d.Latitude, d.Longitude))
            .Select(d => new FarmerDealerHit(d, FarmerGeo.HaversineKm(lat, lng, d.Latitude!.Value, d.Longitude!.Value)))
            .Where(h => h.DistanceKm <= radiusKm)
            .OrderBy(h => h.DistanceKm)
            .ThenBy(h => h.Dealer.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Dealer.Id)
            .ToList();

    /// <summary>
    /// District / state lists: nearest first when the farmer has GPS (dealers without GPS after them),
    /// otherwise by name.
    /// </summary>
    public static List<FarmerDealerHit> Ordered(IEnumerable<FarmerDealerPoint> dealers, double? lat, double? lng)
    {
        var gps = FarmerGeo.IsValid(lat, lng);
        return dealers
            .Select(d => new FarmerDealerHit(d,
                gps && FarmerGeo.IsValid(d.Latitude, d.Longitude)
                    ? FarmerGeo.HaversineKm(lat!.Value, lng!.Value, d.Latitude!.Value, d.Longitude!.Value)
                    : null))
            .OrderBy(h => h.DistanceKm is null ? 1 : 0)
            .ThenBy(h => h.DistanceKm ?? 0)
            .ThenBy(h => h.Dealer.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(h => h.Dealer.Id)
            .ToList();
    }

    /// <summary>
    /// The fallback chain: GPS and someone within the radius -> Nearby; else anyone in the district ->
    /// District; else anyone in the state -> State; else None.
    /// </summary>
    public static FarmerDealerScope ChooseScope(bool hasGps, int nearbyCount, int districtCount, int stateCount) =>
        hasGps && nearbyCount > 0 ? FarmerDealerScope.Nearby
        : districtCount > 0 ? FarmerDealerScope.District
        : stateCount > 0 ? FarmerDealerScope.State
        : FarmerDealerScope.None;

    /// <summary>
    /// The whole selection over in-memory dealers (the controller runs the same steps against the
    /// database): returns the scope and the ranked list for it.
    /// </summary>
    public static (FarmerDealerScope Scope, List<FarmerDealerHit> Hits) Select(
        IReadOnlyCollection<FarmerDealerPoint> listable, double? lat, double? lng, int radiusKm, int? districtId, int? stateId)
    {
        var gps = FarmerGeo.IsValid(lat, lng);
        var nearby = gps ? Nearby(listable, lat!.Value, lng!.Value, radiusKm) : new List<FarmerDealerHit>();
        var district = districtId is > 0 ? listable.Where(d => d.DistrictId == districtId).ToList() : new List<FarmerDealerPoint>();
        var state = stateId is > 0 ? listable.Where(d => d.StateId == stateId).ToList() : new List<FarmerDealerPoint>();

        var scope = ChooseScope(gps, nearby.Count, district.Count, state.Count);
        return scope switch
        {
            FarmerDealerScope.Nearby => (scope, nearby),
            FarmerDealerScope.District => (scope, Ordered(district, lat, lng)),
            FarmerDealerScope.State => (scope, Ordered(state, lat, lng)),
            _ => (scope, new List<FarmerDealerHit>())
        };
    }

    /// <summary>Distance for a card: "850 m" below 1 km, "4.2 km" below 10 km, "27 km" above.</summary>
    public static string DistanceText(double km) =>
        km < 1 ? $"{Math.Max(50, Math.Round(km * 1000 / 50) * 50):0} m"
        : km < 10 ? km.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " km"
        : Math.Round(km).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " km";

    /// <summary>Non-empty parts joined with ", " (address lines).</summary>
    public static string JoinParts(params string?[] parts) =>
        string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));

    /// <summary>
    /// Like <see cref="JoinParts"/> but each part only once, ignoring case ("Kovilpatti, Kovilpatti" when the
    /// village and the taluk are the same place).
    /// </summary>
    public static string JoinDistinct(params string?[] parts) =>
        string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// The street address for a dealer card: the address lines and pincode without the parts the card
    /// already shows in its place line (village, district) and without repeats.
    /// </summary>
    public static string CardAddress(string? line1, string? line2, string? pinCode, params string?[] alreadyShown)
    {
        var shown = new HashSet<string>(alreadyShown.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()), StringComparer.OrdinalIgnoreCase);
        var parts = new[] { line1, line2 }
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .SelectMany(l => l!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(part => !shown.Contains(part))
            .Append(pinCode)
            .ToArray();
        return JoinDistinct(parts);
    }
}
