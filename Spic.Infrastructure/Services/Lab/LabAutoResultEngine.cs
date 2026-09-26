using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// Range-based auto result for one sample (screen 15; rules in docs/sas-lab-portal-plan.md).
/// The analyst enters only values; everything else comes from the LabParameter master:
///
///   numeric, value &lt; RangeMin                        -> LowLabel    / Deficient / LowHint
///   numeric, RangeMin &lt;= value &lt; ModerateFrom (set)   -> "Medium"    / Moderate  / NormalHint
///   numeric, inside the range                         -> NormalLabel / Normal    / NormalHint
///   numeric, value &gt; RangeMax                        -> HighLabel   / Excess    / HighHint
///   text (Texture)                                    -> value stored, no label, no status
///   derived (Organic Matter = Organic Carbon x factor) -> computed from its source, never entered
///
/// Overall status counts the Deficient / Excess parameters that were entered (derived rows are
/// left out so a low Organic Carbon is not counted twice through Organic Matter):
/// 0 -> Good, 1-2 -> Needs Improvement, 3+ -> Poor. Recommendations are the hints of the
/// non-Normal entered parameters grouped by RecommendationGroup (Fertilizer, Organic,
/// Micronutrient, General) and phrased "{hint} because {parameter} is below normal range.".
/// </summary>
public sealed class LabAutoResultEngine
{
    public static readonly string[] GroupOrder = { "Fertilizer", "Organic", "Micronutrient", "General" };

    /// <summary>The active parameters that apply to a sample type, in report order: the soil set
    /// (by SortOrder) then the water set; SoilAndWater samples get both sets.</summary>
    public IReadOnlyList<LabParameter> ParametersFor(SampleType type, IEnumerable<LabParameter> active)
    {
        var list = active.Where(p => p.IsActive).ToList();

        IEnumerable<LabParameter> Set(SampleType setType) => list
            .Where(p => p.AppliesTo == setType || p.AppliesTo == SampleType.SoilAndWater)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id);

        return type switch
        {
            SampleType.Soil => Set(SampleType.Soil).ToList(),
            SampleType.Water => Set(SampleType.Water).ToList(),
            _ => Set(SampleType.Soil).Concat(Set(SampleType.Water)).DistinctBy(p => p.Id).ToList()
        };
    }

    /// <summary>Parameters the analyst types a value for (every applicable one except derived).</summary>
    public static bool IsEnterable(LabParameter p) => string.IsNullOrWhiteSpace(p.DerivedFromCode);

    public LabEvaluation Evaluate(SampleType sampleType, string? crop, IReadOnlyList<LabParameter> parameters,
        IReadOnlyDictionary<int, string?> values)
    {
        var evaluation = new LabEvaluation();
        var numeric = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);   // code -> parsed value
        var rows = new List<(LabParameter P, LabParameterRowDto Row)>();

        // Pass 1: entered values (derived parameters are ignored even when a value is posted).
        foreach (var p in parameters)
        {
            var row = NewRow(p);
            rows.Add((p, row));
            if (!IsEnterable(p)) continue;

            values.TryGetValue(p.Id, out var raw);
            var value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            if (value == null) continue;

            if (p.ValueType == LabParameterValueType.Text)
            {
                row.EnteredValue = NormalizeOption(p, value);
                continue;
            }

            if (!TryParseNumber(value, out var number))
            {
                evaluation.Errors.Add($"{p.Name}: \"{value}\" is not a number.");
                continue;
            }

            row.EnteredValue = value;
            numeric[p.Code] = number;
            Classify(p, number, row);
        }

        // Pass 2: derived parameters from their source value.
        foreach (var (p, row) in rows.Where(r => !IsEnterable(r.P)))
        {
            if (!numeric.TryGetValue(p.DerivedFromCode!, out var source)) continue;
            var derived = Math.Round(source * (p.DerivedFactor ?? 1m), 2, MidpointRounding.AwayFromZero);
            row.EnteredValue = derived.ToString("0.##", CultureInfo.InvariantCulture);
            Classify(p, derived, row);
        }

        var result = evaluation.Result;
        result.Parameters = rows.Select(r => r.Row).ToList();

        var counted = rows.Where(r => IsEnterable(r.P) && r.Row.Status.HasValue).ToList();
        var problems = counted.Count(r => r.Row.Status == LabResultStatus.Deficient || r.Row.Status == LabResultStatus.Excess);

        result.OverallStatus = problems == 0 ? LabOverallStatus.Good
            : problems <= 2 ? LabOverallStatus.NeedsImprovement
            : LabOverallStatus.Poor;
        result.OverallStatusText = OverallText(result.OverallStatus);

        // Recommendation groups, in the fixed order, then any other group name.
        var lines = counted
            .Where(r => r.Row.Status != LabResultStatus.Normal && !string.IsNullOrWhiteSpace(r.Row.Hint))
            .Select(r => (Group: string.IsNullOrWhiteSpace(r.P.RecommendationGroup) ? "General" : r.P.RecommendationGroup.Trim(),
                          Line: Phrase(r.Row.Hint!, r.P.Name, r.Row.Status!.Value)))
            .ToList();

        var groups = GroupOrder.Concat(lines.Select(l => l.Group).Where(g => !GroupOrder.Contains(g, StringComparer.OrdinalIgnoreCase)).Distinct());
        foreach (var group in groups)
        {
            var groupLines = lines.Where(l => string.Equals(l.Group, group, StringComparison.OrdinalIgnoreCase))
                .Select(l => l.Line).Distinct().ToList();
            if (groupLines.Count > 0)
                result.Recommendations.Add(new LabRecommendationGroupDto { Group = group, Lines = groupLines });
        }

        result.CropSuitabilityNote = SuitabilityNote(sampleType, crop, rows.Where(r => IsEnterable(r.P)).ToList());
        return evaluation;
    }

    public static string OverallText(LabOverallStatus status) => status switch
    {
        LabOverallStatus.Good => "Good",
        LabOverallStatus.NeedsImprovement => "Needs Improvement",
        _ => "Poor"
    };

    // ---------------------------------------------------------------- rules

    private static void Classify(LabParameter p, decimal value, LabParameterRowDto row)
    {
        if (p.RangeMin.HasValue && value < p.RangeMin.Value)
        {
            row.ResultLabel = p.LowLabel;
            row.Status = LabResultStatus.Deficient;
            row.Hint = p.LowHint;
        }
        else if (p.RangeMax.HasValue && value > p.RangeMax.Value)
        {
            row.ResultLabel = p.HighLabel;
            row.Status = LabResultStatus.Excess;
            row.Hint = p.HighHint;
        }
        else if (p.ModerateFrom.HasValue && value < p.ModerateFrom.Value)
        {
            row.ResultLabel = "Medium";
            row.Status = LabResultStatus.Moderate;
            row.Hint = p.NormalHint;
        }
        else
        {
            row.ResultLabel = p.NormalLabel;
            row.Status = LabResultStatus.Normal;
            row.Hint = p.NormalHint;
        }
    }

    private static string Phrase(string hint, string name, LabResultStatus status)
    {
        var clean = hint.Trim().TrimEnd('.');
        var subject = Lower(name);
        var reason = status switch
        {
            LabResultStatus.Deficient => $"{subject} is below normal range",
            LabResultStatus.Excess => $"{subject} is above normal range",
            _ => $"{subject} is only moderate"
        };
        return $"{clean} because {reason}.";
    }

    /// <summary>Soil: "Soil is suitable for {crop} after correcting low nitrogen, low organic
    /// carbon, and low zinc." / "Soil is suitable for {crop}.". Water: "Suitable for irrigation" /
    /// "Use with caution: high sodium and high chloride." SoilAndWater: both sentences.</summary>
    private static string? SuitabilityNote(SampleType type, string? crop, List<(LabParameter P, LabParameterRowDto Row)> rows)
    {
        if (!rows.Any(r => r.Row.Status.HasValue)) return null;

        string Problems(IEnumerable<(LabParameter P, LabParameterRowDto Row)> set) => JoinList(set
            .Where(r => r.Row.Status == LabResultStatus.Deficient || r.Row.Status == LabResultStatus.Excess)
            .Select(r => $"{(r.Row.Status == LabResultStatus.Deficient ? "low" : "high")} {Lower(r.P.Name)}")
            .ToList());

        string SoilNote()
        {
            var soil = rows.Where(r => r.P.AppliesTo != SampleType.Water).ToList();
            var cropName = string.IsNullOrWhiteSpace(crop) ? "the selected crop" : crop.Trim();
            var problems = Problems(soil);
            return problems.Length == 0
                ? $"Soil is suitable for {cropName}."
                : $"Soil is suitable for {cropName} after correcting {problems}.";
        }

        string WaterNote()
        {
            var water = rows.Where(r => r.P.AppliesTo != SampleType.Soil).ToList();
            var problems = Problems(water);
            return problems.Length == 0 ? "Suitable for irrigation" : $"Use with caution: {problems}.";
        }

        return type switch
        {
            SampleType.Soil => SoilNote(),
            SampleType.Water => WaterNote(),
            _ => $"{SoilNote()} Water: {WaterNote()}"
        };
    }

    private static string JoinList(List<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => string.Join(", ", items.Take(items.Count - 1)) + ", and " + items[^1]
    };

    /// <summary>"Organic Carbon" -> "organic carbon"; acronyms and mixed case (pH, EC) stay.</summary>
    private static string Lower(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(w => w.Length > 1 && char.IsUpper(w[0]) && w.Skip(1).All(c => !char.IsLetter(c) || char.IsLower(c))
            ? char.ToLowerInvariant(w[0]) + w[1..]
            : w));

    private static string NormalizeOption(LabParameter p, string value)
    {
        if (string.IsNullOrWhiteSpace(p.Options)) return value;
        var match = p.Options.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase));
        return match ?? value;
    }

    public static bool TryParseNumber(string value, out decimal number) =>
        decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out number);

    private static LabParameterRowDto NewRow(LabParameter p) => new()
    {
        LabParameterId = p.Id,
        Code = p.Code,
        Name = p.Name,
        Unit = p.Unit,
        NormalRange = p.NormalRange,
        ReportingLimit = p.ReportingLimit,
        RecommendationGroup = string.IsNullOrWhiteSpace(p.RecommendationGroup) ? "General" : p.RecommendationGroup,
        RowStatus = SampleAnalysisStatus.NotStarted
    };
}

public sealed class LabEvaluation
{
    public LabAutoResultDto Result { get; } = new();
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;
}
