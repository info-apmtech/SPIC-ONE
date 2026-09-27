using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// Soil-test-based fertilizer schedule (phase 2b; product owner: "based on the sample, and for
/// crop as well"). For each crop column (Crop1, and Crop2 or Crop1 again, like the reference
/// report) the crop's active LabCropRecommendation rows are taken; a crop without rows uses the
/// rows of crop "General" (IsGeneral, printed under the heading "General"). Every row's
/// KgPerAcre is multiplied by a dose factor and rounded to 2 decimals (a 0 base stays 0):
///
///   Nutrient codes: LabCropRecommendation.Nutrient, comma separated ("N,P").
///   Code -> parameter: Sas:Lab:NutrientParameters (N = S-N, P = S-P, K = S-K, Zn = S-ZN,
///     Fe = S-FE, Mn = S-MN, Cu = S-CU, S = S-S, B = S-B, Organic = S-OC, Gypsum = S-PH).
///   Status: the parameter's auto-result status on this sample (Deficient / Moderate / Normal /
///     Excess), NotTested when it has no value (or the code is not mapped).
///   Factor: Sas:Lab:NutrientDoseFactors:{code}:{status} when the nutrient has its own table
///     (Organic: Deficient 1.25, else 1.00; Gypsum: Excess (alkaline) 1.25, Normal 1.00,
///     Deficient (acidic) 0.00 = "not required"), else Sas:Lab:DoseFactors:{status}
///     (Deficient 1.25, Moderate 1.10, Normal 1.00, Excess 0.75, NotTested 1.00).
///   Several nutrients: the highest factor wins (DAP "N,P" follows the scarcer of N and P).
///   No nutrient: factor 1.00, printed unchanged.
///
/// Every number lives in configuration; the defaults below apply only when a key is missing.
/// Water samples have no schedule. One engine for the entry preview (LabController) and the
/// soil report (LabReportReader), so the analyst sees what the PDF prints.
/// </summary>
public sealed class LabFertilizerSchedule
{
    public const string GeneralCrop = "General";
    public const string NotTested = "NotTested";

    public static readonly IReadOnlyDictionary<string, decimal> DefaultFactors = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["Deficient"] = 1.25m,
        ["Moderate"] = 1.10m,
        ["Normal"] = 1.00m,
        ["Excess"] = 0.75m,
        [NotTested] = 1.00m
    };

    public static readonly IReadOnlyDictionary<string, string> DefaultNutrientParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["N"] = "S-N",
        ["P"] = "S-P",
        ["K"] = "S-K",
        ["Zn"] = "S-ZN",
        ["Fe"] = "S-FE",
        ["Mn"] = "S-MN",
        ["Cu"] = "S-CU",
        ["S"] = "S-S",
        ["B"] = "S-B",
        ["Organic"] = "S-OC",
        ["Gypsum"] = "S-PH"
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, decimal>> DefaultNutrientFactors =
        new Dictionary<string, IReadOnlyDictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Organic"] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Deficient"] = 1.25m, ["Moderate"] = 1.00m, ["Normal"] = 1.00m, ["Excess"] = 1.00m, [NotTested] = 1.00m
            },
            ["Gypsum"] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["Deficient"] = 0.00m, ["Moderate"] = 1.00m, ["Normal"] = 1.00m, ["Excess"] = 1.25m, [NotTested] = 1.00m
            }
        };

    private readonly IConfiguration? _config;

    public LabFertilizerSchedule(IConfiguration? config = null)
    {
        _config = config;
    }

    /// <summary>The active schedule master in print order (stage, sort order, id).</summary>
    public static Task<List<LabCropRecommendation>> LoadRowsAsync(AppDbContext db, CancellationToken ct = default) =>
        db.LabCropRecommendations.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Stage).ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);

    public static string StageName(LabCropStage stage) => stage switch
    {
        LabCropStage.Basal => "Basal Application",
        LabCropStage.TopDressing1 => "1st Application",
        LabCropStage.TopDressing2 => "2nd Application",
        _ => "3rd Application"
    };

    /// <summary>The schedule columns of a sample (empty for water samples). <paramref name="parameters"/>
    /// are the engine's evaluated rows (code + status) of the sample.</summary>
    public List<LabFertilizerScheduleDto> Build(SampleType sampleType, string? crop1, string? crop2,
        IEnumerable<LabParameterRowDto> parameters, IReadOnlyList<LabCropRecommendation> master)
    {
        var columns = new List<LabFertilizerScheduleDto>();
        if (sampleType == SampleType.Water) return columns;

        var rules = Rules();
        var byCode = new Dictionary<string, LabParameterRowDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in parameters)
        {
            if (!string.IsNullOrWhiteSpace(p.Code)) byCode[p.Code.Trim()] = p;
        }

        var c1 = Clean(crop1);
        var c2 = Clean(crop2);
        if (c2.Length == 0) c2 = c1;   // one crop: printed twice like the reference
        foreach (var crop in new[] { c1, c2 })
        {
            var rows = master.Where(x => x.IsActive && crop.Length > 0 && string.Equals(x.Crop?.Trim(), crop, StringComparison.OrdinalIgnoreCase)).ToList();
            var general = rows.Count == 0;
            if (general) rows = master.Where(x => x.IsActive && string.Equals(x.Crop?.Trim(), GeneralCrop, StringComparison.OrdinalIgnoreCase)).ToList();

            columns.Add(new LabFertilizerScheduleDto
            {
                Crop = crop,
                ScheduleCrop = general ? GeneralCrop : crop,
                IsGeneral = general,
                Rows = rows
                    .OrderBy(r => r.Stage).ThenBy(r => r.SortOrder).ThenBy(r => r.Id)
                    .Select(r => Adjust(r, byCode, rules))
                    .ToList()
            });
        }
        return columns;
    }

    private static LabFertilizerScheduleRowDto Adjust(LabCropRecommendation r, Dictionary<string, LabParameterRowDto> byCode, ScheduleRules rules)
    {
        var row = new LabFertilizerScheduleRowDto
        {
            Stage = r.Stage,
            StageName = StageName(r.Stage),
            DayNumber = r.DayNumber,
            Product = r.Product,
            Nutrient = string.IsNullOrWhiteSpace(r.Nutrient) ? null : r.Nutrient.Trim(),
            BaseKgPerAcre = r.KgPerAcre,
            Factor = 1m,
            SortOrder = r.SortOrder
        };

        var codes = (row.Nutrient ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var first = true;
        foreach (var code in codes)
        {
            LabParameterRowDto? p = null;
            if (rules.Parameters.TryGetValue(code, out var parameterCode)) byCode.TryGetValue(parameterCode, out p);
            var status = p?.Status?.ToString() ?? NotTested;
            var factor = rules.Factor(code, status);

            // the highest factor wins; the first nutrient on a tie
            if (!first && factor <= row.Factor) continue;
            first = false;
            row.Factor = factor;
            row.StatusUsed = status;
            row.StatusNutrient = code;
            row.ParameterCode = p?.Code ?? parameterCode;
            row.ParameterName = p?.Name;
            row.ResultLabel = p?.Status is null ? null : p.ResultLabel;
        }

        row.AdjustedKgPerAcre = row.BaseKgPerAcre == 0m ? 0m : Math.Round(row.BaseKgPerAcre * row.Factor, 2, MidpointRounding.AwayFromZero);
        row.NotRequired = codes.Length > 0 && row.Factor == 0m;
        return row;
    }

    // ---------------------------------------------------------------- configuration

    private sealed class ScheduleRules
    {
        public Dictionary<string, decimal> Factors { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Dictionary<string, decimal>> NutrientFactors { get; } = new(StringComparer.OrdinalIgnoreCase);

        public decimal Factor(string nutrient, string status)
        {
            if (NutrientFactors.TryGetValue(nutrient, out var own) && own.TryGetValue(status, out var f)) return f;
            if (Factors.TryGetValue(status, out f)) return f;
            return Factors.TryGetValue(NotTested, out f) ? f : 1m;
        }
    }

    /// <summary>Read on every call so an edited appsettings (reloadOnChange) applies without a restart.</summary>
    private ScheduleRules Rules()
    {
        var rules = new ScheduleRules();
        foreach (var (k, v) in DefaultFactors) rules.Factors[k] = v;
        foreach (var (k, v) in DefaultNutrientParameters) rules.Parameters[k] = v;
        foreach (var (k, v) in DefaultNutrientFactors) rules.NutrientFactors[k] = new Dictionary<string, decimal>(v, StringComparer.OrdinalIgnoreCase);

        var lab = _config?.GetSection("Sas:Lab");
        if (lab == null) return rules;

        foreach (var child in lab.GetSection("DoseFactors").GetChildren())
        {
            if (TryDecimal(child.Value, out var f)) rules.Factors[child.Key] = f;
        }
        foreach (var child in lab.GetSection("NutrientParameters").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(child.Value)) rules.Parameters[child.Key] = child.Value.Trim();
        }
        foreach (var nutrient in lab.GetSection("NutrientDoseFactors").GetChildren())
        {
            if (!rules.NutrientFactors.TryGetValue(nutrient.Key, out var own))
                rules.NutrientFactors[nutrient.Key] = own = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in nutrient.GetChildren())
            {
                if (TryDecimal(child.Value, out var f)) own[child.Key] = f;
            }
        }
        return rules;
    }

    private static bool TryDecimal(string? value, out decimal number) =>
        decimal.TryParse(value?.Trim(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out number);

    private static string Clean(string? v) => string.IsNullOrWhiteSpace(v) ? "" : v.Trim();
}
