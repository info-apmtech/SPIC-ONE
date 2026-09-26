using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// Loads sample reports into <see cref="LabReportModel"/> (the PDF / Excel input and the body of
/// LabReportDetailDto.Sample). The parameter rows come from SampleLabResult; overall status,
/// recommendation groups and the crop note are recomputed from the stored rows.
/// A SoilAndWater sample's result rows are split by LabParameter.AppliesTo (v1 rows without a
/// master parameter are matched to the master by name, else treated as soil rows).
/// </summary>
public sealed class LabReportReader
{
    /// <summary>Crop whose schedule is printed (as "General") when the sample's crop has none.</summary>
    public const string GeneralScheduleCrop = "Banana";

    private readonly AppDbContext _db;

    public LabReportReader(AppDbContext db) => _db = db;

    public async Task<LabReportModel?> LoadAsync(int reportId, CancellationToken ct = default) =>
        (await LoadManyAsync(new[] { reportId }, ct)).FirstOrDefault();

    /// <summary>Models in the order of <paramref name="reportIds"/> (missing ids are skipped).</summary>
    public async Task<List<LabReportModel>> LoadManyAsync(IReadOnlyCollection<int> reportIds, CancellationToken ct = default)
    {
        if (reportIds.Count == 0) return new List<LabReportModel>();

        var reports = await _db.LabReports.AsNoTracking()
            .Where(r => reportIds.Contains(r.Id))
            .Select(r => new
            {
                Report = r,
                BatchCode = r.Batch!.Code,
                Item = r.SampleItem!,
                Farmer = r.SampleItem!.Farmer,
                CollectionDate = r.SampleItem!.Collection!.CollectionDate,
                PaymentType = r.SampleItem!.Collection!.PaymentType,
                HeadquarterId = r.SampleItem!.Collection!.HeadquarterId
            })
            .ToListAsync(ct);

        if (reports.Count == 0) return new List<LabReportModel>();

        var itemIds = reports.Select(r => r.Item.Id).Distinct().ToList();
        var batchIds = reports.Select(r => r.Report.BatchId).Distinct().ToList();

        var results = await _db.SampleLabResults.AsNoTracking()
            .Where(x => itemIds.Contains(x.SampleItemId))
            .ToListAsync(ct);

        var parameters = await _db.LabParameters.AsNoTracking().ToListAsync(ct);
        var byId = parameters.ToDictionary(p => p.Id);

        var consignments = await _db.SampleConsignments.AsNoTracking()
            .Where(c => c.BatchId != null && batchIds.Contains(c.BatchId.Value) && !c.IsDeleted)
            .OrderBy(c => c.Id)
            .Select(c => new { BatchId = c.BatchId!.Value, c.Code })
            .ToListAsync(ct);

        var schedule = await _db.LabCropRecommendations.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Stage).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);

        var hqIds = reports.Where(r => r.HeadquarterId.HasValue).Select(r => r.HeadquarterId!.Value).Distinct().ToList();
        var states = await LocationNamesAsync(_db, hqIds, ct);

        var models = new List<LabReportModel>();
        foreach (var id in reportIds)
        {
            var src = reports.FirstOrDefault(r => r.Report.Id == id);
            if (src == null) continue;

            var r = src.Report;
            var item = src.Item;
            var farmer = src.Farmer;

            var rows = results
                .Where(x => x.SampleItemId == item.Id)
                .Select(x => ToRow(x, byId, parameters, item.SampleType))
                .Where(x => x.Layout == r.SampleType)
                .Select(x => x.Row)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .ToList();

            var model = new LabReportModel
            {
                ReportId = r.Id,
                ReportCode = r.Code,
                Layout = r.SampleType,
                Status = r.Status,
                GeneratedAt = r.GeneratedAt,
                FinancialYearStart = r.FinancialYearStart,
                DownloadCount = r.DownloadCount,
                BatchId = r.BatchId,
                BatchCode = src.BatchCode,
                ConsignmentCodes = consignments.Where(c => c.BatchId == r.BatchId).Select(c => c.Code).ToList(),
                SampleItemId = item.Id,
                CollectionId = item.CollectionId,
                SampleNumber = item.Code,
                LabNumber = LabReportRules.SampleDisplayId(item.Id, r.SampleType),
                ItemType = item.SampleType,
                PaymentType = src.PaymentType,
                AnalysisStatus = item.AnalysisStatus,
                AnalysisStartedAt = item.AnalysisStartedAt,
                AnalysisCompletedAt = item.AnalysisCompletedAt,
                CollectedOn = src.CollectionDate,
                FarmerName = farmer?.Name ?? "",
                AddressLines = AddressLines(farmer),
                Mobile = farmer?.Mobile,
                SurveyNumber = farmer?.SurveyNumber,
                Village = farmer?.Village,
                District = farmer?.DistrictName,
                StateName = src.HeadquarterId.HasValue && states.TryGetValue(src.HeadquarterId.Value, out var loc) ? loc.State : farmer?.StateName,
                Crop1 = Clean(item.Crop1),
                Crop2 = Clean(item.Crop2),
                Rows = rows
            };

            model.OverallStatus = LabReportRules.Overall(rows);
            model.Recommendations = LabReportRules.Group(rows);
            model.CropSuitabilityNote = LabReportRules.CropNoteEnglish(model.Layout, model.Crop1, rows);
            if (model.Layout == SampleType.Soil) model.Schedule = BuildSchedule(model, schedule);

            models.Add(model);
        }

        return models;
    }

    public static LabSampleReportDto ToSampleReportDto(LabReportModel m, bool batchCompleted)
    {
        var entered = m.Rows.Count(r => r.HasValue);
        var last = m.Rows.Where(r => r.HasValue).OrderByDescending(r => r.EnteredAt).FirstOrDefault();
        return new LabSampleReportDto
        {
            Sample = new LabSampleRowDto
            {
                SampleItemId = m.SampleItemId,
                CollectionId = m.CollectionId,
                SampleId = m.LabNumber,
                SampleCode = m.SampleNumber,
                SampleType = m.Layout,
                PaymentType = m.PaymentType,
                FarmerName = m.FarmerName,
                Crop = m.Crop1,
                Village = m.Village,
                District = m.District,
                CurrentParameter = last?.Name,
                Status = m.AnalysisStatus,
                ReportGenerated = true,
                ParameterCount = m.Rows.Count,
                EnteredCount = entered,
                ProgressPercent = m.Rows.Count == 0 ? 0 : (int)Math.Round(entered * 100.0 / m.Rows.Count),
                AnalysisStartedAt = m.AnalysisStartedAt,
                AnalysisCompletedAt = m.AnalysisCompletedAt
            },
            FarmerMobile = m.Mobile,
            Parameters = m.Rows.Select(r => new LabParameterRowDto
            {
                LabParameterId = r.LabParameterId ?? 0,
                Code = r.Code,
                Name = r.Name,
                Unit = r.Unit,
                NormalRange = r.NormalRange,
                ReportingLimit = r.ReportingLimit,
                RecommendationGroup = r.Group,
                EnteredValue = r.Value,
                ResultLabel = r.ResultLabel,
                Status = r.HasValue && !r.IsText ? r.Status : null,
                Hint = r.Hint,
                RowStatus = m.AnalysisStatus == SampleAnalysisStatus.Completed || batchCompleted
                    ? SampleAnalysisStatus.Completed
                    : r.HasValue ? SampleAnalysisStatus.InProgress : SampleAnalysisStatus.NotStarted,
                EnteredAt = r.HasValue ? r.EnteredAt : null,
                EnteredByName = r.EnteredByName
            }).ToList(),
            OverallStatus = m.OverallStatus,
            OverallStatusText = LabReportRules.OverallText(m.OverallStatus),
            Recommendations = m.Recommendations,
            CropSuitabilityNote = m.CropSuitabilityNote
        };
    }

    // ------------------------------------------------------------------ helpers

    private static (SampleType Layout, LabReportRow Row) ToRow(SampleLabResult x, Dictionary<int, LabParameter> byId,
        List<LabParameter> all, SampleType itemType)
    {
        LabParameter? p = null;
        if (x.LabParameterId.HasValue) byId.TryGetValue(x.LabParameterId.Value, out p);
        if (p == null)
        {
            // v1 rows: match the master by name, preferring the item's own layout.
            var candidates = all.Where(m => string.Equals(m.Name, x.Parameter?.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            p = itemType == SampleType.Water
                ? candidates.FirstOrDefault(m => m.AppliesTo == SampleType.Water) ?? candidates.FirstOrDefault()
                : candidates.FirstOrDefault(m => m.AppliesTo != SampleType.Water) ?? candidates.FirstOrDefault();
        }

        SampleType layout = itemType switch
        {
            SampleType.Water => SampleType.Water,
            SampleType.Soil => SampleType.Soil,
            _ => p?.AppliesTo == SampleType.Water ? SampleType.Water : SampleType.Soil
        };

        var row = new LabReportRow
        {
            LabParameterId = x.LabParameterId ?? p?.Id,
            Code = p?.Code ?? "",
            Name = string.IsNullOrWhiteSpace(x.Parameter) ? p?.Name ?? "" : x.Parameter.Trim(),
            Unit = string.IsNullOrWhiteSpace(x.Unit) ? p?.Unit : x.Unit,
            NormalRange = string.IsNullOrWhiteSpace(x.NormalRange) ? p?.NormalRange : x.NormalRange,
            ReportingLimit = p?.ReportingLimit,
            Group = LabReportRules.NormalizeGroup(p?.RecommendationGroup),
            Value = x.EnteredValue,
            ResultLabel = x.ResultLabel,
            Status = x.Status,
            Hint = x.Hint,
            IsText = p?.ValueType == LabParameterValueType.Text,
            SortOrder = p?.SortOrder ?? x.SortOrder,
            EnteredAt = x.EnteredAt,
            EnteredByName = x.EnteredByName
        };
        return (layout, row);
    }

    private static List<LabScheduleColumn> BuildSchedule(LabReportModel m, List<LabCropRecommendation> all)
    {
        // Two crop columns like the reference (the farmer's first and second crop; the first
        // crop twice when there is only one).
        var crops = new List<string>();
        var c1 = m.Crop1 ?? "";
        crops.Add(c1);
        crops.Add(string.IsNullOrWhiteSpace(m.Crop2) ? c1 : m.Crop2!);

        var columns = new List<LabScheduleColumn>();
        foreach (var crop in crops)
        {
            var rows = all.Where(x => string.Equals(x.Crop, crop, StringComparison.OrdinalIgnoreCase)).ToList();
            var general = rows.Count == 0;
            if (general) rows = all.Where(x => string.Equals(x.Crop, GeneralScheduleCrop, StringComparison.OrdinalIgnoreCase)).ToList();
            columns.Add(new LabScheduleColumn
            {
                Crop = crop,
                ScheduleCrop = general ? GeneralScheduleCrop : crop,
                IsGeneral = general,
                Rows = rows
            });
        }
        return columns;
    }

    private static List<string> AddressLines(SasFarmer? f)
    {
        if (f == null) return new List<string>();
        var line1 = Join(", ", f.Address1, f.Address2);
        var line2 = Join(", ", f.Village, f.Taluk, f.DistrictName);
        if (!string.IsNullOrWhiteSpace(f.PinCode)) line2 = string.IsNullOrWhiteSpace(line2) ? f.PinCode!.Trim() : $"{line2} - {f.PinCode!.Trim()}";
        return new[] { line1, line2 }.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
    }

    private static string Join(string sep, params string?[] parts) =>
        string.Join(sep, parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    /// <summary>Headquarter id -> (headquarter, region, state) names.</summary>
    public static async Task<Dictionary<int, (string? Hq, string? Region, string? State, int? RegionId, int? StateId)>> LocationNamesAsync(
        AppDbContext db, IReadOnlyCollection<int> hqIds, CancellationToken ct = default)
    {
        if (hqIds.Count == 0) return new();
        var rows = await db.Headquarters.AsNoTracking()
            .Where(h => hqIds.Contains(h.Id))
            .Select(h => new
            {
                h.Id,
                h.HeadquarterName,
                h.RegionId,
                RegionName = h.Region != null ? h.Region.RegionName : null,
                StateId = h.Region != null ? (int?)h.Region.StateId : null,
                StateName = h.Region != null && h.Region.State != null ? h.Region.State.StateName : null
            })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => ((string?)r.HeadquarterName, r.RegionName, r.StateName, (int?)r.RegionId, r.StateId));
    }
}
