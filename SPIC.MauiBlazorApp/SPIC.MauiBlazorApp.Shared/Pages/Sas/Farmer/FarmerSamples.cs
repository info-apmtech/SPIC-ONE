using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Shared.Pages.Sas.Farmer;

/// <summary>One SAMPLE of the signed-in farmer (My Samples cards, My Reports "Under Process").</summary>
public sealed class FarmerSampleRow
{
    public int CollectionId { get; init; }
    public int ItemId { get; init; }
    public string SampleCode { get; init; } = "";
    public string CollectionCode { get; init; } = "";
    public string? ConsignmentCode { get; init; }
    public string CollectedByName { get; init; } = "";
    public DateTime CollectionDate { get; init; }
    public string? Crop { get; init; }
    public SampleType SampleType { get; init; }
    public SamplePaymentType PaymentType { get; init; }
    public SampleCollectionStatus CollectionStatus { get; init; }
    /// <summary>The sample's reports, newest first (a Soil + Water sample gets a soil and a water report).</summary>
    public List<LabReportRowDto> Reports { get; set; } = new();
    public LabReportRowDto? Report => Reports.FirstOrDefault();

    public FarmerStatus Status => FarmerSamples.StatusOf(CollectionStatus, Report is not null);
}

public enum FarmerStatus { Collected, PaymentPending, AwaitingDispatch, InTransit, UnderAnalysis, AnalysisCompleted, ReportReady }

/// <summary>
/// Loads the farmer's samples from the v1 SAS API (the API scopes collections to the farmer's
/// own SasFarmer rows) and joins the farmer's sample reports (api/Lab/reports, also scoped).
/// A collection may carry other farmers' samples; the rows keep only the items whose farmer
/// record is linked to this login (SasFarmer.UserId), falling back to every item when none is
/// linked (farmers matched by mobile number).
/// </summary>
public static class FarmerSamples
{
    private const int PageSize = 50;
    private const int MaxPages = 10;

    public static string Text(FarmerStatus s) => s switch
    {
        FarmerStatus.Collected => "Collected",
        FarmerStatus.PaymentPending => "Payment Pending",
        FarmerStatus.AwaitingDispatch => "Awaiting Dispatch",
        FarmerStatus.InTransit => "In Transit",
        FarmerStatus.UnderAnalysis => "Under Analysis",
        FarmerStatus.AnalysisCompleted => "Analysis Completed",
        FarmerStatus.ReportReady => "Report Ready",
        _ => s.ToString()
    };

    public static string Tone(FarmerStatus s) => s switch
    {
        FarmerStatus.Collected => "grey",
        FarmerStatus.PaymentPending => "orange",
        FarmerStatus.AwaitingDispatch => "yellow",
        FarmerStatus.InTransit => "blue",
        FarmerStatus.UnderAnalysis => "violet",
        FarmerStatus.AnalysisCompleted => "teal",
        FarmerStatus.ReportReady => "green",
        _ => "grey"
    };

    public static FarmerStatus StatusOf(SampleCollectionStatus status, bool hasReport) => status switch
    {
        SampleCollectionStatus.Completed => hasReport ? FarmerStatus.ReportReady : FarmerStatus.AnalysisCompleted,
        SampleCollectionStatus.TestInProgress or SampleCollectionStatus.DeliveredToLab => FarmerStatus.UnderAnalysis,
        SampleCollectionStatus.Dispatched or SampleCollectionStatus.InTransit => FarmerStatus.InTransit,
        SampleCollectionStatus.ReadyForConsignment => FarmerStatus.AwaitingDispatch,
        SampleCollectionStatus.PendingPayment or SampleCollectionStatus.PendingApproval => FarmerStatus.PaymentPending,
        _ => FarmerStatus.Collected
    };

    public static string ReportStatusText(LabReportStatus s) => s switch
    {
        LabReportStatus.Generated => "Report Ready",
        LabReportStatus.Downloaded => "Downloaded",
        LabReportStatus.Printed => "Printed",
        _ => s.ToString()
    };

    public static string ReportStatusTone(LabReportStatus s) => s switch
    {
        LabReportStatus.Generated => "green",
        LabReportStatus.Downloaded => "blue",
        LabReportStatus.Printed => "violet",
        _ => "grey"
    };

    /// <summary>"Download PDF" for a single report, "Soil PDF" / "Water PDF" when a sample has several.</summary>
    public static string ReportLabel(LabReportRowDto r, int count) => count > 1 ? $"{SampleTypeName(r.SampleType)} PDF" : "Download PDF";

    public static string SampleTypeName(SampleType t) => t switch
    {
        SampleType.Soil => "Soil",
        SampleType.Water => "Water",
        SampleType.SoilAndWater => "Soil + Water",
        _ => t.ToString()
    };

    public static string Crop(SampleItemDto item) =>
        string.Join(", ", new[] { item.Crop1, item.Crop2 }.Where(c => !string.IsNullOrWhiteSpace(c)));

    /// <summary>The farmer's own items of a collection (linked by UserId), or all when none is linked.</summary>
    public static List<SampleItemDto> OwnItems(SampleCollectionDetailDto d, string? userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var own = d.Items.Where(i => string.Equals(i.Farmer.UserId, userId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (own.Count > 0) return own;
        }
        return d.Items;
    }

    /// <summary>Every page of the farmer's collections (API max 50 per page).</summary>
    public static async Task<List<SampleCollectionSummaryDto>?> CollectionsAsync(SasApi sas, CancellationToken ct)
    {
        var all = new List<SampleCollectionSummaryDto>();
        for (var page = 1; page <= MaxPages; page++)
        {
            var result = await sas.GetCollectionsAsync(new SasCollectionQuery { Page = page, PageSize = PageSize }, ct);
            if (result is null) return page == 1 ? null : all;
            all.AddRange(result.Items);
            if (!result.HasMore) break;
        }
        return all;
    }

    /// <summary>Every page of the farmer's sample reports; null when the reports route failed.</summary>
    public static async Task<List<LabReportRowDto>?> ReportsAsync(LabApi lab, CancellationToken ct)
    {
        var all = new List<LabReportRowDto>();
        for (var page = 1; page <= MaxPages; page++)
        {
            var result = await lab.GetReportsAsync(page: page, pageSize: PageSize, ct: ct);
            if (result is null) return page == 1 ? null : all;
            all.AddRange(result.Items);
            if (!result.HasMore) break;
        }
        return all;
    }

    /// <summary>
    /// The sample rows of the given collections: reads each collection's detail (items, crops,
    /// farmer links) four at a time. Drafts are skipped (the farmer has nothing to see yet).
    /// </summary>
    public static async Task<List<FarmerSampleRow>> SamplesAsync(SasApi sas, IEnumerable<SampleCollectionSummaryDto> collections,
        string? userId, CancellationToken ct)
    {
        var list = collections.Where(c => c.Status != SampleCollectionStatus.Draft).ToList();
        var details = new SampleCollectionDetailDto?[list.Count];
        using var gate = new SemaphoreSlim(4);
        await Task.WhenAll(list.Select(async (c, index) =>
        {
            try
            {
                await gate.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            try
            {
                details[index] = (await sas.GetCollectionAsync(c.Id, ct)).Item;
            }
            catch (OperationCanceledException)
            {
                // page left
            }
            finally
            {
                gate.Release();
            }
        }));

        var rows = new List<FarmerSampleRow>();
        for (var i = 0; i < list.Count; i++)
        {
            var d = details[i];
            if (d is null) continue;
            foreach (var item in OwnItems(d, userId))
            {
                rows.Add(new FarmerSampleRow
                {
                    CollectionId = d.Id,
                    ItemId = item.Id,
                    SampleCode = item.Code,
                    CollectionCode = d.Code,
                    ConsignmentCode = d.ConsignmentCode,
                    CollectedByName = d.CollectedByName,
                    CollectionDate = d.CollectionDate,
                    Crop = Crop(item),
                    SampleType = item.SampleType,
                    PaymentType = d.PaymentType,
                    CollectionStatus = d.Status
                });
            }
        }

        return rows.OrderByDescending(r => r.CollectionDate).ThenByDescending(r => r.ItemId).ToList();
    }
}
