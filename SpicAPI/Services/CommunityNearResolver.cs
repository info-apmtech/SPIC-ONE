using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;

namespace SpicAPI.Services
{
    /// <summary>The area a "near me" question is asked for (0 = unknown).</summary>
    public sealed record CommunityArea(int DistrictId, int StateId, string? DistrictName, string? StateName);

    /// <summary>
    /// Farmer portal Task 3: which Knowledge Community authors are near an area
    /// (<see cref="CommunityNearRules"/>). Discussions have no location column; the authors of the
    /// live discussions are looked up instead (their farmer profile / linked SAS farmer record, or their
    /// staff login's headquarter / region / state), so old discussions count too. Bounded by the number
    /// of distinct authors, which is small. Used by CommunityController (tab=near, api/Community/near) and
    /// by the "Talked about near you" card of api/Farmer/trends.
    /// </summary>
    public sealed class CommunityNearResolver
    {
        private readonly AppDbContext _db;

        public CommunityNearResolver(AppDbContext db) => _db = db;

        /// <summary>
        /// The caller's area: a farmer's profile district / state (else their linked SAS farmer record);
        /// a staff member's state from the JWT ("spic:state_id"), no district.
        /// </summary>
        public async Task<CommunityArea> CallerAreaAsync(string userId, AppRole? role, int stateClaim, CancellationToken ct)
        {
            int districtId = 0, stateId = 0;
            if (role == AppRole.Farmer && !string.IsNullOrWhiteSpace(userId))
            {
                var profile = await _db.FarmerProfiles.AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .Select(p => new { p.DistrictId, p.StateId })
                    .FirstOrDefaultAsync(ct);
                if (profile is not null)
                {
                    districtId = profile.DistrictId;
                    stateId = profile.StateId;
                }
                else
                {
                    var sas = await _db.SasFarmers.AsNoTracking()
                        .Where(f => f.UserId == userId && !f.IsDeleted)
                        .Select(f => new { f.DistrictId, f.StateId })
                        .FirstOrDefaultAsync(ct);
                    districtId = sas?.DistrictId ?? 0;
                    stateId = sas?.StateId ?? 0;
                }
            }
            else
            {
                stateId = stateClaim;
            }

            return await NamedAreaAsync(districtId, stateId, ct);
        }

        /// <summary>Fills the names (and the state from the district when it is missing).</summary>
        public async Task<CommunityArea> NamedAreaAsync(int districtId, int stateId, CancellationToken ct)
        {
            string? districtName = null, stateName = null;
            if (districtId > 0)
            {
                var d = await _db.Districts.AsNoTracking()
                    .Where(x => x.Id == districtId)
                    .Select(x => new { x.DistrictName, x.StateId })
                    .FirstOrDefaultAsync(ct);
                districtName = d?.DistrictName;
                if (stateId <= 0 && d is not null) stateId = d.StateId;
            }
            if (stateId > 0)
                stateName = await _db.States.AsNoTracking().Where(s => s.Id == stateId).Select(s => s.StateName).FirstOrDefaultAsync(ct);
            return new CommunityArea(districtId, stateId, districtName, stateName);
        }

        /// <summary>The authors of live (not deleted) discussions who are near <paramref name="area"/>.</summary>
        public async Task<NearAuthors> AuthorsAsync(CommunityArea area, CancellationToken ct)
        {
            var empty = new NearAuthors(new HashSet<string>(), new HashSet<string>());
            if (area.DistrictId <= 0 && area.StateId <= 0) return empty;

            var authorIds = await _db.CommunityPosts.AsNoTracking()
                .Where(p => !p.IsDeleted && p.AuthorUserId != "")
                .Select(p => p.AuthorUserId)
                .Distinct()
                .ToListAsync(ct);
            if (authorIds.Count == 0) return empty;

            var profiles = await _db.FarmerProfiles.AsNoTracking()
                .Where(p => authorIds.Contains(p.UserId))
                .Select(p => new NearFarmerAuthor(p.UserId, p.DistrictId, p.StateId))
                .ToListAsync(ct);
            var withProfile = profiles.Select(p => p.UserId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var sas = await _db.SasFarmers.AsNoTracking()
                .Where(f => f.UserId != null && !f.IsDeleted && authorIds.Contains(f.UserId))
                .Select(f => new NearFarmerAuthor(f.UserId!, f.DistrictId, f.StateId))
                .ToListAsync(ct);
            var farmers = profiles.Concat(sas.Where(s => !withProfile.Contains(s.UserId))).ToList();

            var staff = await _db.Employeelogins.AsNoTracking()
                .Where(l => l.IsActive && authorIds.Contains(l.UserId))
                .Select(l => new NearStaffAuthor(l.UserId, l.StateId, l.RegionId, l.HeadquartersId, l.RegionIds, l.HeadquartersIds))
                .ToListAsync(ct);

            // The headquarters / regions that serve the district: those of its SPIC dealers (a dealer
            // without a region takes its headquarter's region), as in "My SPIC team".
            var hqIds = new HashSet<int>();
            var regionIds = new HashSet<int>();
            if (area.DistrictId > 0 && staff.Count > 0)
            {
                var dealers = await _db.DealerRegistrations.AsNoTracking()
                    .Where(FarmerDealerRules.ListableForFarmers)
                    .Where(d => d.DistrictId == area.DistrictId)
                    .Select(d => new { d.HQ, d.Region })
                    .Distinct()
                    .ToListAsync(ct);
                foreach (var d in dealers)
                {
                    if (d.HQ > 0) hqIds.Add(d.HQ);
                    if (d.Region > 0) regionIds.Add(d.Region);
                }
                var hqsWithoutRegion = dealers.Where(d => d.HQ > 0 && d.Region <= 0).Select(d => d.HQ).Distinct().ToList();
                if (hqsWithoutRegion.Count > 0)
                {
                    var extra = await _db.Headquarters.AsNoTracking()
                        .Where(h => hqsWithoutRegion.Contains(h.Id))
                        .Select(h => h.RegionId)
                        .ToListAsync(ct);
                    foreach (var r in extra) if (r > 0) regionIds.Add(r);
                }
            }

            return CommunityNearRules.Authors(area.DistrictId, area.StateId, farmers, staff, hqIds, regionIds);
        }
    }
}
