using System.Globalization;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Pages.Sas.Payments;

/// <summary>
/// Wording and pill tones of the Payment Approval pages (screens 23-30, 34-35). Tones are the
/// <c>LabPill</c> tones (green, orange, red, violet, blue, teal, pink, yellow, grey).
/// </summary>
public static class PaymentLabels
{
    private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");

    public static string ModeName(SamplePaymentMode mode) => mode switch
    {
        SamplePaymentMode.Upi => "UPI",
        SamplePaymentMode.BankTransfer => "Bank Transfer",
        SamplePaymentMode.Cash => "Cash",
        _ => mode.ToString()
    };

    public static readonly SamplePaymentMode[] Modes =
        { SamplePaymentMode.Upi, SamplePaymentMode.BankTransfer, SamplePaymentMode.Cash };

    // ---------------------------------------------------------------- admin status

    public static string AdminStatusName(SamplePaymentStatus status) => status switch
    {
        SamplePaymentStatus.Pending => "Pending Review",
        SamplePaymentStatus.Approved => "Admin Approved",
        SamplePaymentStatus.Rejected => "Returned to MO",
        _ => status.ToString()
    };

    public static string AdminStatusTone(SamplePaymentStatus status) => status switch
    {
        SamplePaymentStatus.Pending => "orange",
        SamplePaymentStatus.Approved => "green",
        SamplePaymentStatus.Rejected => "red",
        _ => "grey"
    };

    // ---------------------------------------------------------------- finance status

    /// <summary>Amount Mismatch: Finance received less than the paid amount (FinanceVerifiedAmount &lt; Amount).</summary>
    public static bool IsShort(SasPaymentRowDto row) =>
        row.FinanceStatus == SampleFinanceStatus.Mismatch &&
        row.FinanceVerifiedAmount is { } received && received < row.PaidAmount;

    /// <summary>Failed: Finance marked the payment through "Mark Mismatch" (no amount received).</summary>
    public static bool IsFailed(SasPaymentRowDto row) =>
        row.FinanceStatus == SampleFinanceStatus.Mismatch && row.FinanceVerifiedAmount is null;

    /// <summary>Finance wording of a Mismatch: Amount Mismatch / Failed (Mismatch otherwise).</summary>
    public static string MismatchName(SasPaymentRowDto row) =>
        IsShort(row) ? "Amount Mismatch" : IsFailed(row) ? "Failed" : "Mismatch";

    /// <summary>Admin list wording: Not Forwarded / Pending / Verified / Mismatch.</summary>
    public static string FinanceStatusName(SampleFinanceStatus status) => status switch
    {
        SampleFinanceStatus.NotForwarded => "Not Forwarded",
        SampleFinanceStatus.AwaitingVerification => "Pending",
        SampleFinanceStatus.Verified => "Verified",
        SampleFinanceStatus.Mismatch => "Mismatch",
        _ => status.ToString()
    };

    public static string FinanceStatusTone(SampleFinanceStatus status) => status switch
    {
        SampleFinanceStatus.NotForwarded => "grey",
        SampleFinanceStatus.AwaitingVerification => "orange",
        SampleFinanceStatus.Verified => "green",
        SampleFinanceStatus.Mismatch => "red",
        _ => "grey"
    };

    /// <summary>History wording: Payment Verified / Amount Mismatch / Failed.</summary>
    public static string HistoryStatusName(SasPaymentRowDto row) => row.FinanceStatus switch
    {
        SampleFinanceStatus.Verified => "Payment Verified",
        SampleFinanceStatus.Mismatch => MismatchName(row),
        SampleFinanceStatus.AwaitingVerification => "Pending",
        _ => "Not Forwarded"
    };

    public static string HistoryStatusTone(SasPaymentRowDto row) => row.FinanceStatus switch
    {
        SampleFinanceStatus.Verified => "green",
        SampleFinanceStatus.Mismatch => IsShort(row) ? "orange" : "red",   // Amount Mismatch / Failed
        SampleFinanceStatus.AwaitingVerification => "yellow",
        _ => "grey"
    };

    // ---------------------------------------------------------------- farmer

    /// <summary>Farmer wording (screen 34): Finance Verified / Payment Issue / Admin Approval Pending
    /// (awaiting the admin) / Admin Approved (approved, not yet processed by Finance, including v1
    /// approvals from before Finance verification). The "Approval Pending" KPI
    /// and filter (SasPaymentStatsDto.ApprovalPending, tab approvalPending) count both of the last two.</summary>
    public static string FarmerStatusName(SamplePaymentStatus admin, SampleFinanceStatus finance) =>
        finance == SampleFinanceStatus.Verified ? "Finance Verified"
        : admin == SamplePaymentStatus.Rejected || finance == SampleFinanceStatus.Mismatch ? "Payment Issue"
        : admin == SamplePaymentStatus.Pending ? "Admin Approval Pending"
        : "Admin Approved";

    public static string FarmerStatusTone(SamplePaymentStatus admin, SampleFinanceStatus finance) =>
        finance == SampleFinanceStatus.Verified ? "green"
        : admin == SamplePaymentStatus.Rejected || finance == SampleFinanceStatus.Mismatch ? "red"
        : admin == SamplePaymentStatus.Pending ? "orange"
        : "blue";

    // ---------------------------------------------------------------- formatting

    public static string Money(decimal amount) => "₹" + amount.ToString("N2", India);

    /// <summary>"₹0" in green when equal; "-₹100.00" when short; "+₹50.00" when over.</summary>
    public static string Difference(decimal difference) =>
        difference == 0 ? "₹0" : (difference < 0 ? "-" : "+") + Money(Math.Abs(difference));

    public static string Date(DateTime? value) => value?.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? "-";

    public static string Time(DateTime? value) => value?.ToString("hh:mm tt", CultureInfo.InvariantCulture) ?? "";

    public static string DateTime(DateTime? value) =>
        value is { } v ? v.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) : "-";

    public static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value!;

    /// <summary>Two-letter initials for the avatar circles.</summary>
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(new[] { ' ', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    /// <summary>Stable avatar colour per name (one of six soft tones).</summary>
    public static string AvatarTone(string? name)
    {
        var tones = new[] { "violet", "blue", "green", "orange", "pink", "teal" };
        var hash = 0;
        foreach (var ch in name ?? "") hash = unchecked(hash * 31 + ch);
        return tones[Math.Abs(hash % tones.Length)];
    }
}
