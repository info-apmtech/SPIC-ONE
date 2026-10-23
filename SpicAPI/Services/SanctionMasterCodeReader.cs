using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpicAPI.Services
{
    // Reads master codes only. The existing issuing service owns authorization,
    // its approval transaction, formatting and atomic counter allocation.
    public static class SanctionMasterCodeReader
    {
        public static async Task<(string StateCode, string? RegionCode)> ReadAsync(
            AppDbContext db,
            int stateId,
            int? regionId,
            CancellationToken ct = default)
        {
            if (stateId <= 0 || (regionId.HasValue && regionId.Value <= 0))
            {
                throw new InvalidOperationException("Invalid sanction sequence scope.");
            }

            var stateCode = await db.States
                .AsNoTracking()
                .Where(x => x.Id == stateId)
                .Select(x => x.StateCode)
                .SingleOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(stateCode))
            {
                throw new InvalidOperationException(
                    "State Code is missing in the State master.");
            }

            if (!regionId.HasValue)
            {
                return (stateCode.Trim(), null);
            }

            var regionCode = await db.Regions
                .AsNoTracking()
                .Where(x => x.Id == regionId.Value && x.StateId == stateId)
                .Select(x => x.RegionCode)
                .SingleOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(regionCode))
            {
                throw new InvalidOperationException(
                    "Region Code is missing, or the Region does not belong to the selected State.");
            }

            return (stateCode.Trim(), regionCode.Trim());
        }
    }
}
