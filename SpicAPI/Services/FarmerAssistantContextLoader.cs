using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;

namespace SpicAPI.Services
{
    /// <summary>
    /// What SPIC AI for farmers (farmer portal Task 5) and, next, the leaf-photo check are told about the
    /// farmer: crops, district / state, land and water from the profile; the RATINGS (no values) of the latest
    /// completed SPIC soil report; the one-line summary of the latest fertilizer advice run. Never the name,
    /// phone, village, GPS or user id (<see cref="FarmerAssistantFacts"/> has no field for them).
    /// </summary>
    public class FarmerAssistantContextLoader
    {
        private const int MaxSoilRows = 14;

        private readonly AppDbContext _db;

        public FarmerAssistantContextLoader(AppDbContext db) => _db = db;

        public sealed record Loaded(FarmerAssistantFacts Facts, string Language, bool HasProfile);

        public async Task<Loaded> LoadAsync(string userId, CancellationToken ct)
        {
            var profile = await _db.FarmerProfiles.AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new { p.StateId, p.DistrictId, p.PrimaryCrops, p.LandAcres, p.IrrigationType, p.PreferredLanguage })
                .FirstOrDefaultAsync(ct);

            string? district = null, state = null;
            if (profile is not null)
            {
                district = await _db.Districts.AsNoTracking().Where(d => d.Id == profile.DistrictId).Select(d => d.DistrictName).FirstOrDefaultAsync(ct);
                state = await _db.States.AsNoTracking().Where(s => s.Id == profile.StateId).Select(s => s.StateName).FirstOrDefaultAsync(ct);
            }

            var (soilDate, ratings) = await SoilRatingsAsync(userId, ct);

            var run = await _db.FertilizerRecommendationRuns.AsNoTracking()
                .Where(r => r.FarmerUserId == userId)
                .OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
                .Select(r => new { r.Summary, r.CreatedAtUtc })
                .FirstOrDefaultAsync(ct);

            var crops = (profile?.PrimaryCrops ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var facts = new FarmerAssistantFacts
            {
                Crops = crops,
                District = district,
                State = state,
                LandAcres = profile?.LandAcres,
                Irrigation = profile?.IrrigationType?.ToString(),
                SoilReportDate = soilDate,
                SoilRatings = ratings,
                FertilizerSummary = run?.Summary,
                FertilizerDateUtc = run?.CreatedAtUtc
            };
            return new Loaded(facts, FarmerAssistantRules.LanguageCode(profile?.PreferredLanguage), profile is not null);
        }

        /// <summary>The ratings of the caller's latest SPIC soil report (same report rule as the fertilizer
        /// controller: soil, not deleted, the caller's SAS farmer records by user id or mobile).</summary>
        private async Task<(DateTime? Date, IReadOnlyList<(string Name, string Rating)> Ratings)> SoilRatingsAsync(string userId, CancellationToken ct)
        {
            var account = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.UserName, u.PhoneNumber })
                .FirstOrDefaultAsync(ct);
            var userName = account?.UserName ?? "";
            var phone = account?.PhoneNumber ?? "";
            var farmerIds = await _db.SasFarmers.AsNoTracking()
                .Where(f => !f.IsDeleted && (
                    f.UserId == userId ||
                    (userName != "" && f.Mobile == userName) ||
                    (phone != "" && f.Mobile == phone)))
                .Select(f => f.Id)
                .ToListAsync(ct);
            if (farmerIds.Count == 0) return (null, Array.Empty<(string, string)>());

            var report = await _db.LabReports.AsNoTracking()
                .Where(r => r.SampleType == SampleType.Soil
                            && !r.Batch!.IsDeleted && !r.SampleItem!.IsDeleted && !r.SampleItem.Collection!.IsDeleted
                            && farmerIds.Contains(r.SampleItem.FarmerId))
                .OrderByDescending(r => r.GeneratedAt).ThenByDescending(r => r.Id)
                .Select(r => new { r.SampleItemId, r.GeneratedAt })
                .FirstOrDefaultAsync(ct);
            if (report is null) return (null, Array.Empty<(string, string)>());

            var rows = await _db.SampleLabResults.AsNoTracking()
                .Where(x => x.SampleItemId == report.SampleItemId && x.EnteredValue != null && x.EnteredValue != "")
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .Select(x => new { x.Parameter, x.ResultLabel, x.Status })
                .Take(MaxSoilRows)
                .ToListAsync(ct);

            var ratings = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Parameter))
                .Select(r => (r.Parameter.Trim(), FarmerAssistantRules.SoilRating(r.ResultLabel, r.Status)))
                .ToList();
            return (report.GeneratedAt, ratings);
        }
    }
}
