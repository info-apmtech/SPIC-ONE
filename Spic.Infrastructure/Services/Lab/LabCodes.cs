using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spic.Infrastructure.Data;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// Batch codes BAT-SAS-{yyyy}-{001}, numbered per calendar year from the highest existing code.
/// Several API instances share one database, so two batches created at the same moment can pick
/// the same number: the unique index on SampleBatch.Code rejects the second insert and the caller
/// retries (<see cref="IsUniqueViolation"/>, <see cref="MaxAttempts"/>).
/// </summary>
public static class LabCodes
{
    public const int MaxAttempts = 5;

    public static string BatchPrefix(int year) => $"BAT-SAS-{year}-";

    public static async Task<string> NextBatchCodeAsync(AppDbContext db, DateTime now, CancellationToken ct = default)
    {
        var head = BatchPrefix(now.Year);

        var codes = await db.SampleBatches.AsNoTracking()
            .Where(b => b.Code.StartsWith(head))
            .Select(b => b.Code)
            .ToListAsync(ct);

        var max = 0;
        foreach (var code in codes)
        {
            var tail = code.Length > head.Length ? code[head.Length..] : "";
            if (int.TryParse(tail, out var value) && value > max) max = value;
        }

        return head + (max + 1).ToString("000");
    }

    /// <summary>True when the save failed on a unique index (PostgreSQL 23505).</summary>
    public static bool IsUniqueViolation(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
        }
        return false;
    }
}
