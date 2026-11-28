using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Spic.Infrastructure.Data;

namespace SpicAPI.Services
{
    /// <summary>
    /// The per-farmer advisory lock the daily caps count under (SPIC.Core FarmerDailyCapRules; the same pattern as the
    /// call-back cap): a transaction holding <c>pg_advisory_xact_lock(hashtextextended(key, 0))</c>, released on commit
    /// or dispose. Null on a non-PostgreSQL provider (the harness's InMemory database), where there is nothing to lock.
    /// The lock is never held during an external call.
    /// </summary>
    public static class FarmerDailyLock
    {
        public static async Task<IDbContextTransaction?> AcquireAsync(AppDbContext db, string key, CancellationToken ct)
        {
            if (!db.Database.IsNpgsql()) return null;
            var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
            return tx;
        }
    }
}
