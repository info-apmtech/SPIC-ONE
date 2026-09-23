using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// View-model helpers for the SAS (Sample Collection) module: the labels, pill classes and
/// small client-only models the pages share. Nothing here talks to the network - that is
/// <see cref="SasApi"/>'s job.
///
/// The pill classes are plain names ("sas-pill sas-pill--green"); every SAS page defines
/// them in its own scoped stylesheet, so the labels stay markup-free and reusable.
/// Owned by Client-Collect; the consignment and details pages read from it as well.
/// </summary>
public static class SasLabels
{
    // ---------------------------------------------------------------- collection status

    /// <summary>Pill text for a collection status (the wording on the Figma list).</summary>
    public static string Status(SampleCollectionStatus status) => status switch
    {
        SampleCollectionStatus.Draft => "Draft",
        SampleCollectionStatus.Collected => "Sample Collected",
        SampleCollectionStatus.PendingPayment => "Pending Payment",
        SampleCollectionStatus.PendingApproval => "Pending Approval",
        SampleCollectionStatus.ReadyForConsignment => "Ready consignment",
        SampleCollectionStatus.Dispatched => "Dispatched",
        SampleCollectionStatus.InTransit => "In Transit",
        SampleCollectionStatus.DeliveredToLab => "Delivered to Lab",
        SampleCollectionStatus.TestInProgress => "In Progress",
        SampleCollectionStatus.Completed => "Completed",
        _ => status.ToString()
    };

    /// <summary>The small grey line under the status pill in the list.</summary>
    public static string StatusHint(SampleCollectionStatus status) => status switch
    {
        SampleCollectionStatus.Draft => "Pending validation",
        SampleCollectionStatus.Collected => "Collection completed",
        SampleCollectionStatus.PendingPayment => "Payment pending",
        SampleCollectionStatus.PendingApproval => "Awaiting approval",
        SampleCollectionStatus.ReadyForConsignment => "Ready to dispatch",
        SampleCollectionStatus.Dispatched => "Handed to courier",
        SampleCollectionStatus.InTransit => "On the way to the lab",
        SampleCollectionStatus.DeliveredToLab => "At the district lab",
        SampleCollectionStatus.TestInProgress => "Testing in progress",
        SampleCollectionStatus.Completed => "Results available",
        _ => ""
    };

    public static string StatusPillClass(SampleCollectionStatus status) => "sas-pill " + (status switch
    {
        SampleCollectionStatus.Draft => "sas-pill--grey",
        SampleCollectionStatus.Collected => "sas-pill--green",
        SampleCollectionStatus.PendingPayment => "sas-pill--amber",
        SampleCollectionStatus.PendingApproval => "sas-pill--amber",
        SampleCollectionStatus.ReadyForConsignment => "sas-pill--blue",
        SampleCollectionStatus.Dispatched => "sas-pill--blue",
        SampleCollectionStatus.InTransit => "sas-pill--blue",
        SampleCollectionStatus.DeliveredToLab => "sas-pill--violet",
        SampleCollectionStatus.TestInProgress => "sas-pill--violet",
        SampleCollectionStatus.Completed => "sas-pill--green",
        _ => "sas-pill--grey"
    });

    /// <summary>Status values offered in the list filter, in lifecycle order.</summary>
    public static readonly (SampleCollectionStatus Value, string Label)[] StatusOptions =
    {
        (SampleCollectionStatus.Draft, "Draft"),
        (SampleCollectionStatus.Collected, "Sample Collected"),
        (SampleCollectionStatus.PendingPayment, "Pending Payment"),
        (SampleCollectionStatus.PendingApproval, "Pending Approval"),
        (SampleCollectionStatus.ReadyForConsignment, "Ready for Consignment"),
        (SampleCollectionStatus.Dispatched, "Dispatched"),
        (SampleCollectionStatus.InTransit, "In Transit"),
        (SampleCollectionStatus.DeliveredToLab, "Delivered to Lab"),
        (SampleCollectionStatus.TestInProgress, "Test in Progress"),
        (SampleCollectionStatus.Completed, "Completed"),
    };

    // ---------------------------------------------------------------- sample type

    public static string SampleTypeName(SampleType type) => type switch
    {
        SampleType.Soil => "Soil",
        SampleType.Water => "Water",
        SampleType.SoilAndWater => "Soil + Water",
        _ => type.ToString()
    };

    /// <summary>Soil / Water yes-no answers -> the sample type, or null when both are "No".</summary>
    public static SampleType? TypeFrom(bool soil, bool water) =>
        soil && water ? SampleType.SoilAndWater
        : soil ? SampleType.Soil
        : water ? SampleType.Water
        : null;

    // ---------------------------------------------------------------- payment type / status

    public static string PaymentTypeName(SamplePaymentType type) =>
        type == SamplePaymentType.Paid ? "Paid" : "Free";

    public static string PaymentTypePillClass(SamplePaymentType type) =>
        "sas-pill " + (type == SamplePaymentType.Paid ? "sas-pill--blue" : "sas-pill--green");

    public static string PaidCategoryName(SamplePaidCategory? category) => category switch
    {
        SamplePaidCategory.Farmer => "Farmer",
        SamplePaidCategory.Ngo => "NGO",
        _ => ""
    };

    public static string PaymentStatusName(SamplePaymentStatus? status) => status switch
    {
        SamplePaymentStatus.Pending => "Payment Pending",
        SamplePaymentStatus.Approved => "Payment Approved",
        SamplePaymentStatus.Rejected => "Payment Rejected",
        _ => ""
    };

    public static string PaymentStatusPillClass(SamplePaymentStatus? status) => "sas-pill " + (status switch
    {
        SamplePaymentStatus.Approved => "sas-pill--green",
        SamplePaymentStatus.Rejected => "sas-pill--red",
        SamplePaymentStatus.Pending => "sas-pill--amber",
        _ => "sas-pill--grey"
    });

    public static readonly (SamplePaymentStatus Value, string Label)[] PaymentStatusOptions =
    {
        (SamplePaymentStatus.Pending, "Pending"),
        (SamplePaymentStatus.Approved, "Approved"),
        (SamplePaymentStatus.Rejected, "Rejected"),
    };

    public static string ConsignmentStatusName(ConsignmentStatus status) => status switch
    {
        ConsignmentStatus.PendingPickup => "Pending Pickup",
        ConsignmentStatus.Dispatched => "Dispatched",
        ConsignmentStatus.InTransit => "In Transit",
        ConsignmentStatus.Delivered => "Delivered",
        ConsignmentStatus.Completed => "Completed",
        _ => status.ToString()
    };

    // ---------------------------------------------------------------- formatting

    /// <summary>Rupee amount, e.g. "Rs 200.00". Free collections print "N/A" via the caller.</summary>
    public static string Money(decimal amount) => "₹" + amount.ToString("N2");

    public static string DateOnly(DateTime value) => value.ToString("dd/MM/yyyy");

    public static string TimeOnly(DateTime value) => value.ToString("hh:mm tt");

    public static string LongDate(DateTime value) => value.ToString("dd MMM yyyy");

    /// <summary>"31/05/2026 09:15 AM" for the single-line table cells and exports.</summary>
    public static string DateTimeText(DateTime value) => $"{DateOnly(value)} {TimeOnly(value)}";
}

// ---------------------------------------------------------------------------------- queries

/// <summary>
/// Everything the list page can filter on. Mirrors the URL query of
/// <c>/SampleCollection</c> one for one, so back / refresh / share all agree.
/// </summary>
public sealed class SasCollectionQuery
{
    public SampleCollectionStatus? Status { get; set; }
    public SamplePaymentType? PaymentType { get; set; }
    public SamplePaymentStatus? PaymentStatus { get; set; }
    public string? Location { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 16;
}

// ---------------------------------------------------------------------------------- new page

/// <summary>
/// One line of the "Sample Added (Preview)" table on the New Sample Collection page: a farmer
/// plus what was taken from them. Held only in the browser until Save Draft / Save and Continue
/// turns the list into <see cref="SampleItemUpsertDto"/>s.
/// </summary>
public sealed class SampleDraftRow
{
    /// <summary>Existing SampleItem id when a draft is being edited; null for a new line.</summary>
    public int? ItemId { get; set; }

    public SasFarmerDto Farmer { get; set; } = new();
    public SampleType SampleType { get; set; }
    public string? Crop1 { get; set; }
    public string? Crop2 { get; set; }
    public string? Remarks { get; set; }

    /// <summary>Price for this line, from the charge master (0 for a free collection).</summary>
    public decimal Amount { get; set; }

    public int FarmerId => Farmer.Id;

    public bool Soil => SampleType is SampleType.Soil or SampleType.SoilAndWater;
    public bool Water => SampleType is SampleType.Water or SampleType.SoilAndWater;

    public string CropText =>
        string.Join(", ", new[] { Crop1, Crop2 }.Where(c => !string.IsNullOrWhiteSpace(c)));

    public SampleItemUpsertDto ToUpsert() => new()
    {
        Id = ItemId,
        FarmerId = Farmer.Id,
        SampleType = SampleType,
        Crop1 = string.IsNullOrWhiteSpace(Crop1) ? null : Crop1!.Trim(),
        Crop2 = string.IsNullOrWhiteSpace(Crop2) ? null : Crop2!.Trim(),
        Remarks = string.IsNullOrWhiteSpace(Remarks) ? null : Remarks!.Trim()
    };

    /// <summary>Reloads a saved draft's items into editable preview rows.</summary>
    public static SampleDraftRow FromDto(SampleItemDto dto) => new()
    {
        ItemId = dto.Id,
        Farmer = dto.Farmer ?? new SasFarmerDto(),
        SampleType = dto.SampleType,
        Crop1 = dto.Crop1,
        Crop2 = dto.Crop2,
        Remarks = dto.Remarks,
        Amount = dto.Amount
    };
}

/// <summary>
/// What the Sample Summary drawer shows. Built either from the preview rows on the New page
/// (before anything is saved) or from a saved <see cref="SampleCollectionDetailDto"/>.
/// </summary>
public sealed class SasSummary
{
    public int CollectionId { get; set; }
    public string Code { get; set; } = "";
    public DateTime CollectionDate { get; set; } = DateTime.Now;
    public string CollectedByName { get; set; } = "";
    public SamplePaymentType PaymentType { get; set; }
    public SamplePaidCategory? PaidCategory { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SampleDraftRow> Rows { get; set; } = new();

    public int TotalSamples => Rows.Count;

    /// <summary>Distinct sample types in the collection (the "Sample Types" stat card).</summary>
    public int SampleTypeCount => Rows.Select(r => r.SampleType).Distinct().Count();

    public int TotalFarmers => Rows.Select(r => r.FarmerId).Distinct().Count();

    public int VillagesCovered => Rows
        .Select(r => r.Farmer.Village?.Trim())
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();

    public string Districts => Join(Rows.Select(r => r.Farmer.DistrictName));

    public string States => Join(Rows.Select(r => r.Farmer.StateName));

    /// <summary>"PHC Anna Nagar" style origin for the drawer's From Location column.</summary>
    public string FromLocation(SampleDraftRow row) =>
        Join(new[] { row.Farmer.Village, row.Farmer.Taluk ?? row.Farmer.DistrictName });

    private static string Join(IEnumerable<string?> values)
    {
        var list = values.Where(v => !string.IsNullOrWhiteSpace(v))
                         .Select(v => v!.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();
        return list.Count == 0 ? "-" : string.Join(", ", list);
    }

    public static SasSummary FromDto(SampleCollectionDetailDto dto) => new()
    {
        CollectionId = dto.Id,
        Code = dto.Code,
        CollectionDate = dto.CollectionDate,
        CollectedByName = dto.CollectedByName,
        PaymentType = dto.PaymentType,
        PaidCategory = dto.PaidCategory,
        TotalAmount = dto.TotalAmount,
        Rows = (dto.Items ?? new List<SampleItemDto>()).Select(SampleDraftRow.FromDto).ToList()
    };
}

/// <summary>The payment form, kept apart from the DTO so a draft can be stashed in sessionStorage.</summary>
public sealed class SasPaymentForm
{
    public string TransactionId { get; set; } = "";
    public string? BankGateway { get; set; }
    public string? UtrNumber { get; set; }
    public string PaidByName { get; set; } = "";
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }

    public SamplePaymentUpsertDto ToDto() => new()
    {
        TransactionId = TransactionId.Trim(),
        BankGateway = Blank(BankGateway),
        UtrNumber = Blank(UtrNumber),
        PaidByName = PaidByName.Trim(),
        ContactNumber = Blank(ContactNumber),
        Email = Blank(Email)
    };

    public static SasPaymentForm FromDto(SamplePaymentDto dto) => new()
    {
        TransactionId = dto.TransactionId,
        BankGateway = dto.BankGateway,
        UtrNumber = dto.UtrNumber,
        PaidByName = dto.PaidByName,
        ContactNumber = dto.ContactNumber,
        Email = dto.Email
    };

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
