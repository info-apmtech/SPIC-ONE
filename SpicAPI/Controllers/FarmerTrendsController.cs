using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using SpicAPI.Services;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Farmer portal Task 3 (docs/farmer-portal.md): "Trends in my area" and the home weather strip.
    /// Farmer role only; the area is the caller's own profile district (state as the fallback).
    ///
    ///   GET api/Farmer/trends   -> FarmerTrendsDto (weather + crops + soil problems + talked about + dealers)
    ///   GET api/Farmer/weather  -> FarmerWeatherDto (the home strip; no area numbers)
    ///
    /// Area numbers are aggregates of SPIC ONE data over the last <see cref="FarmerTrendRules.WindowDays"/>
    /// days (<see cref="FarmerTrendRules"/>): never a single farmer's row, soil stats hidden below 5
    /// samples. They are cached for one hour per district + state (in-memory, per API replica). Weather
    /// comes from <see cref="IFarmerWeatherService"/> (Open-Meteo, the GPS of the profile rounded to 0.1
    /// degree; no GPS = no weather, the app asks the farmer to add the location).
    /// </summary>
    [FarmerAccess]
    [Authorize(Roles = nameof(AppRole.Farmer))]
    [ApiController]
    [Route("api/Farmer")]
    public class FarmerTrendsController : ControllerBase
    {
        private const string NoProfileMessage = "Please finish your profile first.";
        public static readonly TimeSpan AreaCacheFor = TimeSpan.FromHours(1);

        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IFarmerWeatherService _weather;
        private readonly CommunityNearResolver _near;

        public FarmerTrendsController(AppDbContext db, IMemoryCache cache, IFarmerWeatherService weather, CommunityNearResolver near)
        {
            _db = db;
            _cache = cache;
            _weather = weather;
            _near = near;
        }

        private string? CallerId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private sealed record AreaTrends(FarmerCropTrendDto Crops, FarmerSoilTrendDto Soil, FarmerTalkedTrendDto Talked,
            FarmerDealerCountDto Dealers, DateTime ComputedAtUtc);

        [HttpGet("weather")]
        public async Task<IActionResult> Weather(CancellationToken ct)
        {
            var profile = await ProfileAsync(ct);
            if (profile is null) return NotFound(new FarmerAuthError { Message = NoProfileMessage });
            return Ok(await _weather.ForecastAsync(profile.Latitude, profile.Longitude, ct));
        }

        [HttpGet("trends")]
        public async Task<IActionResult> Trends(CancellationToken ct)
        {
            var profile = await ProfileAsync(ct);
            if (profile is null) return NotFound(new FarmerAuthError { Message = NoProfileMessage });

            // The forecast does not touch the database, so it runs while the area numbers load.
            var weatherTask = _weather.ForecastAsync(profile.Latitude, profile.Longitude, ct);

            var area = await _near.NamedAreaAsync(profile.DistrictId, profile.StateId, ct);
            var key = $"fp:trends:area:{area.DistrictId}:{area.StateId}";
            if (!_cache.TryGetValue(key, out AreaTrends? numbers) || numbers is null)
            {
                numbers = await ComputeAreaAsync(area, ct);
                _cache.Set(key, numbers, AreaCacheFor);
            }

            return Ok(new FarmerTrendsDto
            {
                DistrictName = area.DistrictName,
                StateName = area.StateName,
                WindowDays = FarmerTrendRules.WindowDays,
                AreaComputedAtUtc = numbers.ComputedAtUtc,
                Weather = await weatherTask,
                Crops = numbers.Crops,
                Soil = numbers.Soil,
                Talked = numbers.Talked,
                Dealers = numbers.Dealers
            });
        }

        private Task<FarmerProfile?> ProfileAsync(CancellationToken ct)
        {
            var userId = CallerId;
            if (string.IsNullOrEmpty(userId)) return Task.FromResult<FarmerProfile?>(null);
            return _db.FarmerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        }

        // ------------------------------------------------------------------ area numbers

        private async Task<AreaTrends> ComputeAreaAsync(CommunityArea area, CancellationToken ct)
        {
            // Community / SAS rows carry server-local timestamps (DateTime.Now); the window is 90 days,
            // so the few hours between IST and UTC do not matter.
            var since = DateTime.Now.AddDays(-FarmerTrendRules.WindowDays);
            var stateDistricts = area.StateId > 0
                ? await _db.Districts.AsNoTracking().Where(d => d.StateId == area.StateId).Select(d => d.Id).ToListAsync(ct)
                : new List<int>();
            if (area.DistrictId > 0 && !stateDistricts.Contains(area.DistrictId)) stateDistricts.Add(area.DistrictId);

            var crops = FarmerTrendRules.Crops(await CropFarmersAsync(area, stateDistricts, since, ct), area.DistrictId, area.DistrictName, area.StateName);
            var soil = FarmerTrendRules.SoilProblems(await SoilResultsAsync(area, stateDistricts, since, ct), area.DistrictId, area.DistrictName, area.StateName);
            var talked = await TalkedAsync(area, since, ct);
            var dealers = await DealerCountAsync(area, ct);
            return new AreaTrends(crops, soil, talked, dealers, DateTime.UtcNow);
        }

        /// <summary>Farmer profiles of the state (all: they hold the farmer's current crops) and SAS farmers
        /// whose samples were collected in the window (their sample crops).</summary>
        private async Task<List<TrendCropFarmer>> CropFarmersAsync(CommunityArea area, List<int> stateDistricts, DateTime since, CancellationToken ct)
        {
            var list = new List<TrendCropFarmer>();

            var profiles = await _db.FarmerProfiles.AsNoTracking()
                .Where(p => (area.StateId > 0 && p.StateId == area.StateId) || p.DistrictId == area.DistrictId)
                .Select(p => new { p.UserId, p.DistrictId, p.PrimaryCrops })
                .ToListAsync(ct);
            list.AddRange(profiles.Select(p => new TrendCropFarmer("u:" + p.UserId, p.DistrictId,
                (p.PrimaryCrops ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))));

            var samples = await (
                from item in _db.SampleItems.AsNoTracking()
                join collection in _db.SampleCollections.AsNoTracking() on item.CollectionId equals collection.Id
                join farmer in _db.SasFarmers.AsNoTracking() on item.FarmerId equals farmer.Id
                where !item.IsDeleted && !collection.IsDeleted && !farmer.IsDeleted
                      && collection.Status != SampleCollectionStatus.Draft
                      && collection.CollectionDate >= since
                      && ((area.StateId > 0 && farmer.StateId == area.StateId)
                          || (farmer.DistrictId != null && stateDistricts.Contains(farmer.DistrictId.Value)))
                select new { farmer.Id, farmer.UserId, farmer.DistrictId, item.Crop1, item.Crop2 })
                .ToListAsync(ct);
            list.AddRange(samples.Select(s => new TrendCropFarmer(
                string.IsNullOrWhiteSpace(s.UserId) ? "s:" + s.Id : "u:" + s.UserId,
                s.DistrictId,
                new[] { s.Crop1 ?? "", s.Crop2 ?? "" })));

            return list;
        }

        /// <summary>The lab's own status for the soil nutrients of every soil report generated in the window, in the state.</summary>
        private async Task<List<TrendSoilResult>> SoilResultsAsync(CommunityArea area, List<int> stateDistricts, DateTime since, CancellationToken ct)
        {
            var nutrients = FarmerTrendRules.NutrientParameters.ToList();
            // Deleted samples, collections, batches and farmers never count (LabReport and
            // SampleLabResult have no IsDeleted flag of their own).
            var rows = await (
                from report in _db.LabReports.AsNoTracking()
                join batch in _db.SampleBatches.AsNoTracking() on report.BatchId equals batch.Id
                join item in _db.SampleItems.AsNoTracking() on report.SampleItemId equals item.Id
                join collection in _db.SampleCollections.AsNoTracking() on item.CollectionId equals collection.Id
                join farmer in _db.SasFarmers.AsNoTracking() on item.FarmerId equals farmer.Id
                join result in _db.SampleLabResults.AsNoTracking() on item.Id equals result.SampleItemId
                where report.SampleType == SampleType.Soil
                      && report.GeneratedAt >= since
                      && !batch.IsDeleted
                      && !item.IsDeleted
                      && !collection.IsDeleted
                      && !farmer.IsDeleted
                      && nutrients.Contains(result.Parameter)
                      && result.EnteredValue != null && result.EnteredValue != ""
                      && ((area.StateId > 0 && farmer.StateId == area.StateId)
                          || (farmer.DistrictId != null && stateDistricts.Contains(farmer.DistrictId.Value)))
                orderby result.Id
                select new { SampleId = item.Id, farmer.DistrictId, result.Parameter, result.Status })
                .ToListAsync(ct);
            return rows.Select(r => new TrendSoilResult(r.SampleId, r.DistrictId, r.Parameter, r.Status)).ToList();
        }

        /// <summary>
        /// "Talked about near you": discussions active in the window by authors of the district (when it has
        /// at least <see cref="CommunityNearRules.MinDistrictPosts"/> of them), else of the state. Scored,
        /// ordered and limited in SQL (recent replies are a correlated COUNT); only 20 rows are read.
        /// </summary>
        private async Task<FarmerTalkedTrendDto> TalkedAsync(CommunityArea area, DateTime since, CancellationToken ct)
        {
            var (scope, near, _) = await _near.NearPostsAsync(area, p => p.LastActivityAt >= since, ct);
            if (scope == FarmerAreaScope.None) return new FarmerTalkedTrendDto();

            var top = await TopTalkedAsync(near, since, ct);
            // Final fix wave (#22): the district had enough posts but none scored; the state may have some.
            if (top.Count == 0 && scope == FarmerAreaScope.District)
            {
                var (statePosts, stateCount) = await _near.StatePostsAsync(area, p => p.LastActivityAt >= since, ct);
                if (stateCount > 0)
                {
                    top = await TopTalkedAsync(statePosts, since, ct);
                    scope = FarmerAreaScope.State;
                }
            }
            return top.Count == 0
                ? new FarmerTalkedTrendDto()
                : new FarmerTalkedTrendDto
                {
                    Scope = scope,
                    AreaName = FarmerTrendRules.AreaName(scope, area.DistrictName, area.StateName),
                    Items = top
                };
        }

        private async Task<List<FarmerTalkedDto>> TopTalkedAsync(IQueryable<CommunityPost> near, DateTime since, CancellationToken ct)
        {
            var rows = await near
                .Select(p => new
                {
                    p.Id, p.Title, p.Crop, p.Category, p.Status, p.ReplyCount, p.Views, p.LikeCount, p.LastActivityAt,
                    Recent = _db.CommunityPostReplies.Count(r => r.PostId == p.Id && !r.IsDeleted && r.CreatedAt >= since)
                })
                .OrderByDescending(x => x.Recent * 3 + x.LikeCount + x.Views / 10)
                .ThenByDescending(x => x.LastActivityAt)
                .Take(20)
                .ToListAsync(ct);

            return FarmerTrendRules.TopTalked(rows.Select(p =>
                new TrendPost(p.Id, p.Title, p.Crop, p.Category, p.Status, p.ReplyCount, p.Recent, p.Views, p.LikeCount, p.LastActivityAt)));
        }

        /// <summary>Listable SPIC dealers (<see cref="FarmerDealerRules.ListableForFarmers"/>) in the district, else the state.</summary>
        private async Task<FarmerDealerCountDto> DealerCountAsync(CommunityArea area, CancellationToken ct)
        {
            var listable = _db.DealerRegistrations.AsNoTracking().Where(FarmerDealerRules.ListableForFarmers);
            if (area.DistrictId > 0)
            {
                var inDistrict = await listable.CountAsync(d => d.DistrictId == area.DistrictId, ct);
                if (inDistrict > 0)
                    return new FarmerDealerCountDto { Scope = FarmerAreaScope.District, AreaName = area.DistrictName, Count = inDistrict };
            }
            if (area.StateId > 0)
            {
                var inState = await listable.CountAsync(d => (d.DealerStateId ?? d.StateId) == area.StateId, ct);
                if (inState > 0)
                    return new FarmerDealerCountDto { Scope = FarmerAreaScope.State, AreaName = area.StateName, Count = inState };
            }
            return new FarmerDealerCountDto();
        }
    }
}
