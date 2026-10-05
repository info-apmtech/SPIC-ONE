using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;

namespace Spic.Infrastructure.Services.MasterData;

/// <summary>A master value that a file needs but the database does not have.</summary>
public sealed class MissingMaster
{
    private readonly List<int> _rowNumbers = new();

    public string Kind { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;

    /// <summary>Rows in the uploaded file that referenced this value, ascending.</summary>
    public IReadOnlyList<int> RowNumbers => _rowNumbers;

    internal void AddRow(int rowNumber) => _rowNumbers.Add(rowNumber);

    public string Display => $"{Kind}: {Value} (row{(RowNumbers.Count == 1 ? "" : "s")} {string.Join(", ", RowNumbers)})";
}

/// <summary>Everything an importer needs to resolve master references.</summary>
public sealed class MasterLookup
{
    private readonly Dictionary<string, int> _zones = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _states = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _districts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _subDistricts = new(StringComparer.Ordinal);

    /// <summary>Region key is Scoped(RegionName, StateId).</summary>
    private readonly Dictionary<string, int> _regions = new(StringComparer.Ordinal);

    /// <summary>Headquarter key is Scoped(HeadquarterName, RegionId).</summary>
    private readonly Dictionary<string, int> _headquarters = new(StringComparer.Ordinal);

    /// <summary>Unscoped child name to all candidates, so a name under the wrong
    /// parent can be reported as missing rather than silently mismatched.</summary>
    private readonly Dictionary<string, List<int>> _regionIdsByName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<int>> _headquarterIdsByName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<int>> _districtIdsByName = new(StringComparer.Ordinal);

    public bool TryGetZone(string? name, out int id) => _zones.TryGetValue(MasterNormalizer.Normalize(name), out id);
    public bool TryGetState(string? name, out int id) => _states.TryGetValue(MasterNormalizer.Normalize(name), out id);
    public bool TryGetDistrict(string? name, int stateId, out int id) =>
        _districts.TryGetValue(MasterNormalizer.Scoped(name, stateId), out id);
    public bool TryGetSubDistrict(string? name, int districtId, out int id) =>
        _subDistricts.TryGetValue(MasterNormalizer.Scoped(name, districtId), out id);
    public bool TryGetRegion(string? name, int stateId, out int id) =>
        _regions.TryGetValue(MasterNormalizer.Scoped(name, stateId), out id);
    public bool TryGetHeadquarter(string? name, int regionId, out int id) =>
        _headquarters.TryGetValue(MasterNormalizer.Scoped(name, regionId), out id);

    /// <summary>True when the name does not exist at all, anywhere in the table.</summary>
    public bool RegionNameExistsAnywhere(string? name) =>
        _regionIdsByName.ContainsKey(MasterNormalizer.Normalize(name));

    public bool HeadquarterNameExistsAnywhere(string? name) =>
        _headquarterIdsByName.ContainsKey(MasterNormalizer.Normalize(name));

    public bool DistrictNameExistsAnywhere(string? name) =>
        _districtIdsByName.ContainsKey(MasterNormalizer.Normalize(name));

    /// <summary>
    /// Loads every location master once. Built with GroupBy(...).First() rather
    /// than ToDictionary so that duplicate rows already in the database cannot
    /// throw and take the whole upload down with them.
    /// </summary>
    public static async Task<MasterLookup> LoadAsync(AppDbContext db, CancellationToken ct = default)
    {
        var lookup = new MasterLookup();

        var zones = await db.Zones.AsNoTracking().ToListAsync(ct);
        foreach (var g in zones.GroupBy(z => MasterNormalizer.Normalize(z.ZoneName)).Where(g => g.Key.Length > 0))
            lookup._zones.TryAdd(g.Key, g.First().Id);

        var states = await db.States.AsNoTracking().ToListAsync(ct);
        foreach (var g in states.GroupBy(s => MasterNormalizer.Normalize(s.StateName)).Where(g => g.Key.Length > 0))
            lookup._states.TryAdd(g.Key, g.First().Id);

        var districts = await db.Districts.AsNoTracking().ToListAsync(ct);
        foreach (var g in districts.GroupBy(d => MasterNormalizer.Scoped(d.DistrictName, d.StateId)).Where(g => g.Key.Length > 0))
            lookup._districts.TryAdd(g.Key, g.First().Id);
        foreach (var g in districts.GroupBy(d => MasterNormalizer.Normalize(d.DistrictName)).Where(g => g.Key.Length > 0))
        {
            if (!lookup._districtIdsByName.TryGetValue(g.Key, out var list))
                lookup._districtIdsByName[g.Key] = list = new List<int>();
            list.AddRange(g.Select(d => d.Id));
        }

        var subDistricts = await db.SubDistricts.AsNoTracking().ToListAsync(ct);
        foreach (var g in subDistricts.GroupBy(s => MasterNormalizer.Scoped(s.SubDistrictName, s.DistrictId)).Where(g => g.Key.Length > 0))
            lookup._subDistricts.TryAdd(g.Key, g.First().Id);

        var regions = await db.Regions.AsNoTracking().ToListAsync(ct);
        foreach (var g in regions.GroupBy(r => MasterNormalizer.Scoped(r.RegionName, r.StateId)).Where(g => g.Key.Length > 0))
            lookup._regions.TryAdd(g.Key, g.First().Id);
        foreach (var g in regions.GroupBy(r => MasterNormalizer.Normalize(r.RegionName)).Where(g => g.Key.Length > 0))
        {
            if (!lookup._regionIdsByName.TryGetValue(g.Key, out var list))
                lookup._regionIdsByName[g.Key] = list = new List<int>();
            list.AddRange(g.Select(r => r.Id));
        }

        var headquarters = await db.Headquarters.AsNoTracking().ToListAsync(ct);
        foreach (var g in headquarters.GroupBy(h => MasterNormalizer.Scoped(h.HeadquarterName, h.RegionId)).Where(g => g.Key.Length > 0))
            lookup._headquarters.TryAdd(g.Key, g.First().Id);
        foreach (var g in headquarters.GroupBy(h => MasterNormalizer.Normalize(h.HeadquarterName)).Where(g => g.Key.Length > 0))
        {
            if (!lookup._headquarterIdsByName.TryGetValue(g.Key, out var list))
                lookup._headquarterIdsByName[g.Key] = list = new List<int>();
            list.AddRange(g.Select(h => h.Id));
        }

        return lookup;
    }
}

/// <summary>
/// Collects every master an importer needs but cannot find, so the whole file is
/// reported in one response instead of failing one row at a time.
/// </summary>
public sealed class MissingMasterCollector
{
    private readonly Dictionary<string, MissingMaster> _byKey = new(StringComparer.Ordinal);
    private readonly List<MissingMaster> _entries = new();

    /// <summary>
    /// Records a missing value. Repeats of the same kind+value collapse into one
    /// entry that lists every affected row.
    /// </summary>
    public void Add(string kind, string? value, int rowNumber)
    {
        var display = (value ?? string.Empty).Trim();
        if (display.Length == 0)
            return;

        var key = kind + "|" + MasterNormalizer.Normalize(display);
        if (!_byKey.TryGetValue(key, out var entry))
        {
            entry = new MissingMaster { Kind = kind, Value = display };
            _byKey[key] = entry;
            _entries.Add(entry);
        }

        entry.AddRow(rowNumber);
    }

    public bool HasAny => _entries.Count > 0;

    public IReadOnlyList<MissingMaster> Entries => _entries;

    /// <summary>Message plus a per-master line, ready to hand straight to the UI.</summary>
    public (string Message, IReadOnlyList<string> Lines) Build(string entityLabel)
    {
        var ordered = _entries
            .OrderBy(e => e.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lines = ordered
            .Select(e => $"Missing {e.Kind}: {e.Value} — row{(e.RowNumbers.Count == 1 ? "" : "s")} {string.Join(", ", e.RowNumbers)}")
            .ToList();

        var message =
            $"Upload stopped. No {entityLabel} records were created and no master data was added. " +
            $"Create the following master record(s), then upload the same file again:" +
            Environment.NewLine + string.Join(Environment.NewLine, lines);

        return (message, lines);
    }
}
