using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;

namespace SpicAPI.Services
{
    /// <summary>The area a "near me" question is asked for (0 = unknown).</summary>
    public sealed record CommunityArea(int DistrictId, int StateId, string? DistrictName, string? StateName);

    /// <summary>
    /// Farmer portal Task 3: which Knowledge Community discussions are near an area
    /// (<see cref="CommunityNearRules"/>). Discussions have no location column; the AUTHOR is matched
    /// instead (their farmer profile / linked SAS farmer record, or their staff login's headquarter /
    /// region / state), so old discussions count too. Each count and each page is ONE SQL query (author
    /// IN sub-queries, <see cref="NearPosts"/>): nothing grows in memory with the number of authors, and
    /// paging runs in the database. Used by CommunityController (tab=near, api/Community/near) and by the
    /// "Talked about near you" card of api/Farmer/trends.
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

        /// <summary>
        /// The live discussions near <paramref name="area"/> and which scope applied: the district when it
        /// has at least <see cref="CommunityNearRules.MinDistrictPosts"/> discussions, else the state
        /// (<see cref="CommunityNearRules.ChooseScope"/>). Returns an IQueryable: the caller counts, orders
        /// and pages it in SQL. Cost: one small dealer-headquarter lookup and at most two COUNT queries;
        /// Count is the chosen scope's total (with <paramref name="filter"/>).
        /// <paramref name="filter"/> (e.g. "active in the last 90 days") applies to the counts too.
        /// </summary>
        public async Task<(FarmerAreaScope Scope, IQueryable<CommunityPost> Posts, int Count)> NearPostsAsync(
            CommunityArea area, Expression<Func<CommunityPost, bool>>? filter, CancellationToken ct)
        {
            var (hqIds, regionIds) = await DistrictServiceAsync(area.DistrictId, ct);
            var district = NearPosts(Sources(), area, FarmerAreaScope.District, hqIds, regionIds);
            var state = NearPosts(Sources(), area, FarmerAreaScope.State, hqIds, regionIds);
            if (filter is not null)
            {
                district = district.Where(filter);
                state = state.Where(filter);
            }

            var districtCount = area.DistrictId > 0 ? await district.CountAsync(ct) : 0;
            var stateCount = districtCount >= CommunityNearRules.MinDistrictPosts || area.StateId <= 0
                ? 0
                : await state.CountAsync(ct);
            var scope = CommunityNearRules.ChooseScope(districtCount, stateCount);
            return scope switch
            {
                FarmerAreaScope.District => (scope, district, districtCount),
                FarmerAreaScope.State => (scope, state, stateCount),
                _ => (scope, _db.CommunityPosts.AsNoTracking().Where(p => false), 0)
            };
        }

        /// <summary>
        /// The state-scope posts only (final fix wave, deferred #22): used when the district scope was chosen but
        /// gave nothing to show (for example "Talked about near you" with no scored discussion).
        /// </summary>
        public async Task<(IQueryable<CommunityPost> Posts, int Count)> StatePostsAsync(
            CommunityArea area, Expression<Func<CommunityPost, bool>>? filter, CancellationToken ct)
        {
            if (area.StateId <= 0) return (_db.CommunityPosts.AsNoTracking().Where(p => false), 0);
            var (hqIds, regionIds) = await DistrictServiceAsync(area.DistrictId, ct);
            var state = NearPosts(Sources(), area, FarmerAreaScope.State, hqIds, regionIds);
            if (filter is not null) state = state.Where(filter);
            return (state, await state.CountAsync(ct));
        }

        /// <summary>
        /// The headquarters and regions that serve a district: those of its listable SPIC dealers (a dealer
        /// without a region takes its headquarter's region), as in "My SPIC team". Bounded by the dealers
        /// of one district.
        /// </summary>
        public async Task<(List<int> HqIds, List<int> RegionIds)> DistrictServiceAsync(int districtId, CancellationToken ct)
        {
            var hqIds = new HashSet<int>();
            var regionIds = new HashSet<int>();
            if (districtId <= 0) return (new List<int>(), new List<int>());

            var dealers = await _db.DealerRegistrations.AsNoTracking()
                .Where(FarmerDealerRules.ListableForFarmers)
                .Where(d => d.DistrictId == districtId)
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
            return (hqIds.ToList(), regionIds.ToList());
        }

        /// <summary>The tables <see cref="NearPosts"/> reads (the harness passes in-memory lists instead).</summary>
        public sealed record NearSources(
            IQueryable<CommunityPost> Posts,
            IQueryable<FarmerProfile> Profiles,
            IQueryable<SasFarmer> SasFarmers,
            IQueryable<EmployeeRegistration.Employeelogin> Logins);

        private NearSources Sources() => new(
            _db.CommunityPosts.AsNoTracking(), _db.FarmerProfiles, _db.SasFarmers, _db.Employeelogins);

        /// <summary>
        /// Live discussions whose author is near the area, as ONE query (author IN sub-queries):
        /// a farmer profile in the district (State: or in the state); a linked SAS farmer record in the
        /// district / state for accounts with no farmer profile; an active staff login whose headquarter
        /// (single or multi) serves the district or, with no headquarter at all, whose region does
        /// (State: or whose login state is the area's state). On PostgreSQL the array tests become
        /// `&&` / unnest sub-queries; the harness runs the same expression over in-memory lists.
        /// </summary>
        public static IQueryable<CommunityPost> NearPosts(NearSources db, CommunityArea area, FarmerAreaScope scope,
            IReadOnlyCollection<int> hqIds, IReadOnlyCollection<int> regionIds)
        {
            var posts = db.Posts.Where(p => !p.IsDeleted);
            var d = area.DistrictId;
            var st = scope == FarmerAreaScope.State ? area.StateId : 0;
            if (scope == FarmerAreaScope.None || (d <= 0 && st <= 0)) return posts.Where(p => false);

            var hq = hqIds.ToList();
            var rg = regionIds.ToList();

            var farmers = db.Profiles
                .Where(f => (d > 0 && f.DistrictId == d) || (st > 0 && f.StateId == st))
                .Select(f => f.UserId);
            var sas = db.SasFarmers
                .Where(f => f.UserId != null && !f.IsDeleted
                            && ((d > 0 && f.DistrictId == d) || (st > 0 && f.StateId == st))
                            && !db.Profiles.Any(p => p.UserId == f.UserId))
                .Select(f => f.UserId!);
            var staff = db.Logins
                .Where(l => l.IsActive
                            && ((st > 0 && l.StateId == st)
                                || hq.Contains(l.HeadquartersId)
                                || (l.HeadquartersIds != null && l.HeadquartersIds.Any(h => hq.Contains(h)))
                                || (l.HeadquartersId <= 0
                                    && (l.HeadquartersIds == null || !l.HeadquartersIds.Any(h => h > 0))
                                    && (rg.Contains(l.RegionId) || (l.RegionIds != null && l.RegionIds.Any(r => rg.Contains(r)))))))
                .Select(l => l.UserId);

            return posts.Where(p => farmers.Contains(p.AuthorUserId) || sas.Contains(p.AuthorUserId) || staff.Contains(p.AuthorUserId));
        }
    }
}
