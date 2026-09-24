namespace SPIC.MauiBlazorApp.Shared.Pages;

public enum ExtensionRequestState
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>One row of the admin Extension Request List (and the header of its details page).</summary>
public sealed record ExtensionRequestRow(
    string Id,
    string SanctionNumber,
    string ProgramName,
    string ProgramType,
    string Location,
    string RequestedBy,
    DateTime? SchemeFrom,
    DateTime? SchemeTo,
    DateTime RequestedOn,
    DateTime NewEndDate,
    decimal RequiredTurnover,
    decimal ActualTurnover,
    ExtensionRequestState State)
{
    /// <summary>"EXT-2026-00021" -> "EXT - 2026 - 00021", as the design displays it.</summary>
    public string DisplayId => Id.Replace("-", " - ");

    public decimal AchievementPct => RequiredTurnover == 0 ? 0 : ActualTurnover / RequiredTurnover * 100;

    public int ExtraDays => SchemeTo is null ? 0 : (NewEndDate.Date - SchemeTo.Value.Date).Days;
}

/// <summary>Sample requests until the API is wired; shared by the admin list and details pages.</summary>
public static class ExtensionRequestSamples
{
    public static readonly IReadOnlyList<ExtensionRequestRow> All = Build();

    public static ExtensionRequestRow? Find(string? id) =>
        All.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));

    private static List<ExtensionRequestRow> Build()
    {
        var from = new DateTime(2026, 8, 1);
        var to = new DateTime(2026, 10, 31);
        var newEnd = new DateTime(2026, 11, 30);
        const string program = "Dealers Meet - State Level";

        ExtensionRequestRow Row(int n, string location, string by, decimal required, decimal actual,
                                ExtensionRequestState state, bool periodSet = true) =>
            new($"EXT-2026-{n:00000}", $"SN-2026-{400 + n:0000}", program, "Dealer Program", location, by,
                periodSet ? from : null, periodSet ? to : null,
                new DateTime(2026, 10, 31, 12, 5, 0).AddHours(-n), newEnd,
                required, actual, state);

        return new()
        {
            Row(21, "Pune, Maharashtra", "Rakesh Kumar (MO)", 2_20_00_000, 1_35_00_000, ExtensionRequestState.Pending),
            Row(20, "Nashik, Maharashtra", "Priya Sharma (MO)", 1_50_00_000, 1_20_00_000, ExtensionRequestState.Pending),
            Row(19, "Madurai, Tamil Nadu", "Karthik R (MO)", 2_20_00_000, 1_95_00_000, ExtensionRequestState.Pending),
            Row(18, "Salem, Tamil Nadu", "Anitha S (MO)", 2_00_00_000, 1_10_00_000, ExtensionRequestState.Approved),
            Row(17, "Ludhiana, Punjab", "Gurpreet Singh (MO)", 2_20_00_000, 1_80_00_000, ExtensionRequestState.Pending),
            Row(16, "Nagpur, Maharashtra", "Suresh Patil (MO)", 1_80_00_000, 60_00_000, ExtensionRequestState.Rejected, periodSet: false),
            Row(15, "Coimbatore, Tamil Nadu", "Vignesh M (MO)", 2_20_00_000, 2_05_00_000, ExtensionRequestState.Approved),
            Row(14, "Pune, Maharashtra", "Rakesh Kumar (MO)", 1_60_00_000, 1_30_00_000, ExtensionRequestState.Pending),
        };
    }
}

/// <summary>An extension request on a Sales Audit scheme, shown on Sales Tracking and in its status drawer.</summary>
public sealed record ExtensionRequestInfo(
    string RequestId,
    ExtensionRequestState State,
    DateTime RequestedOn,
    string RequestedBy,
    DateTime OriginalEndDate,
    DateTime NewEndDate,
    string Remarks,
    DateTime? ApprovedOn = null,
    string? ApprovedBy = null)
{
    public int ExtraDays => (NewEndDate.Date - OriginalEndDate.Date).Days;

    /// <summary>"1 Month (30 Days)"; months are whole 30-day blocks, matching the design.</summary>
    public string ExtensionLength
    {
        get
        {
            var months = ExtraDays / 30;
            var days = $"{ExtraDays} {(ExtraDays == 1 ? "Day" : "Days")}";
            return months >= 1 ? $"{months} {(months == 1 ? "Month" : "Months")} ({days})" : days;
        }
    }

    /// <summary>Sample request until the API is wired (same scheme as the Sales Tracking sample data).</summary>
    public static ExtensionRequestInfo Sample(ExtensionRequestState state) => new(
        RequestId: "EXT - 2026 - 00021",
        State: state,
        RequestedOn: new DateTime(2026, 10, 31, 12, 5, 0),
        RequestedBy: "Rakesh Kumar (MO)",
        OriginalEndDate: new DateTime(2026, 10, 31),
        NewEndDate: new DateTime(2026, 11, 30),
        Remarks: "Additional time required to achieve the remaining turnover due to delayed dealer sales.",
        ApprovedOn: state == ExtensionRequestState.Approved ? new DateTime(2026, 11, 2, 10, 30, 0) : null,
        ApprovedBy: state == ExtensionRequestState.Approved ? "AVP" : null);
}
