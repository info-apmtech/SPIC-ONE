using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Pages.Lab.Admin;

/// <summary>Wording and small calculations shared by the admin Lab Tracking list and Batch View (screens 21-22).</summary>
public static class LabAdminText
{
    public static string Status(SampleBatchStatus s) => s switch
    {
        SampleBatchStatus.Created => "Batch Created",
        SampleBatchStatus.TakenForAnalysis => "Taken for Analysis",
        SampleBatchStatus.InProgress => "In Progress",
        SampleBatchStatus.AnalysisCompleted => "Analysis Completed",
        SampleBatchStatus.Completed => "Completed",
        _ => s.ToString()
    };

    /// <summary>"Pending" (yellow) until analysis starts, "n Days" (green), red when delayed.</summary>
    public static (string Text, string Tone) AnalysisDays(LabBatchRowDto row) =>
        row.AnalysisDays is not { } d
            ? ("Pending", "yellow")
            : ($"{d} Day{(d == 1 ? "" : "s")}", row.IsDelayed ? "red" : "green");

    public static string SampleTypeName(SampleType t) => t switch
    {
        SampleType.Soil => "Soil",
        SampleType.Water => "Water",
        SampleType.SoilAndWater => "Soil + Water",
        _ => t.ToString()
    };

    public static string PaymentTypeName(SamplePaymentType t) => t == SamplePaymentType.Paid ? "Paid" : "Free";

    public static string Date(DateTime d) => d.ToString("dd MMM yyyy");
    public static string Time(DateTime d) => d.ToString("hh:mm tt");
    public static string Stamp(DateTime d) => d.ToString("dd MMM yyyy, hh:mm tt");
    public static string DateOrDash(DateTime? d) => d is { } v ? Date(v) : "-";
    public static string StampOrDash(DateTime? d) => d is { } v ? Stamp(v) : "-";

    public static string Dash(string? s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

    /// <summary>Latest moment anything visible happened to the batch (status steps, timeline, sample entry).</summary>
    public static DateTime LastUpdated(LabBatchDetailDto d)
    {
        var dates = new List<DateTime> { d.CreatedAt };
        void Add(DateTime? v) { if (v is { } x) dates.Add(x); }
        Add(d.Header.AssignedAt);
        Add(d.TakenForAnalysisAt);
        Add(d.AnalysisStartedAt);
        Add(d.AnalysisCompletedAt);
        Add(d.CompletedAt);
        foreach (var a in d.Timeline) dates.Add(a.At);
        foreach (var s in d.RecentSamples)
        {
            Add(s.AnalysisStartedAt);
            Add(s.AnalysisCompletedAt);
        }
        return dates.Max();
    }

    /// <summary>Soil / water sample counts of a batch (a Soil + Water sample counts in both, like the consignment rows).</summary>
    public static (int Soil, int Water) SoilWater(LabBatchDetailDto d)
    {
        if (d.Consignments.Count > 0)
            return (d.Consignments.Sum(c => c.SoilSamples), d.Consignments.Sum(c => c.WaterSamples));

        var soil = d.SampleTypeSummary.Where(s => s.SampleType != SampleType.Water).Sum(s => s.Total);
        var water = d.SampleTypeSummary.Where(s => s.SampleType != SampleType.Soil).Sum(s => s.Total);
        return (soil, water);
    }
}
