using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// The lab's display id of a sample ("Lab Number"): SAS-SOIL-001, SAS-WATER-001, SAS-SW-001.
/// Numbered per batch and per sample type, in collection-code order then item id, so the
/// analyst / coordinator pages (LabController), the report list and detail (LabReportReader)
/// and the PDF / Excel layouts all print the same id. A SoilAndWater sample keeps its one id
/// (SAS-SW-nnn) on both of its reports.
/// </summary>
public static class LabSampleIds
{
    public static string TypeToken(SampleType type) => type switch
    {
        SampleType.Soil => "SOIL",
        SampleType.Water => "WATER",
        _ => "SW"
    };

    public static string Format(SampleType type, int number) => $"SAS-{TypeToken(type)}-{Math.Max(number, 1):000}";

    /// <summary>Display ids of the samples of ONE batch: item id -> SAS-{TYPE}-{nnn}.</summary>
    public static Dictionary<int, string> Number(IEnumerable<(int ItemId, SampleType Type, string? CollectionCode)> items)
    {
        var ids = new Dictionary<int, string>();
        var counters = new Dictionary<SampleType, int>();
        foreach (var i in items.OrderBy(i => i.CollectionCode ?? "", StringComparer.OrdinalIgnoreCase).ThenBy(i => i.ItemId))
        {
            if (ids.ContainsKey(i.ItemId)) continue;
            counters[i.Type] = counters.TryGetValue(i.Type, out var n) ? n + 1 : 1;
            ids[i.ItemId] = Format(i.Type, counters[i.Type]);
        }
        return ids;
    }

    /// <summary>The live samples of the given batches (through their consignments).</summary>
    public static IQueryable<SampleItem> ItemsOfBatches(AppDbContext db, IReadOnlyCollection<int> batchIds) =>
        db.SampleItems.AsNoTracking().Where(i => !i.IsDeleted && !i.Collection!.IsDeleted &&
            i.Collection.Consignment != null && !i.Collection.Consignment.IsDeleted &&
            i.Collection.Consignment.BatchId != null && batchIds.Contains(i.Collection.Consignment.BatchId.Value));

    /// <summary>Display ids of every sample of the given batches, numbered per batch: item id -> id.</summary>
    public static async Task<Dictionary<int, string>> ForBatchesAsync(AppDbContext db, IReadOnlyCollection<int> batchIds,
        CancellationToken ct = default)
    {
        if (batchIds.Count == 0) return new Dictionary<int, string>();

        var rows = await ItemsOfBatches(db, batchIds)
            .Select(i => new { BatchId = i.Collection!.Consignment!.BatchId!.Value, i.Id, i.SampleType, CollectionCode = i.Collection.Code })
            .ToListAsync(ct);

        var ids = new Dictionary<int, string>();
        foreach (var batch in rows.GroupBy(r => r.BatchId))
            foreach (var pair in Number(batch.Select(r => (r.Id, r.SampleType, (string?)r.CollectionCode))))
                ids[pair.Key] = pair.Value;
        return ids;
    }

    /// <summary>Display id of one sample inside its batch.</summary>
    public static async Task<string> ForItemAsync(AppDbContext db, int batchId, int itemId, SampleType type, CancellationToken ct = default)
    {
        var ids = await ForBatchesAsync(db, new[] { batchId }, ct);
        return ids.TryGetValue(itemId, out var id) ? id : Format(type, 1);
    }
}
