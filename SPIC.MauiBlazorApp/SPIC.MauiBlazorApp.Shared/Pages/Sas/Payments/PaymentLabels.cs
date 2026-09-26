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

    /// <summary>Is a Mismatch a short amount (Amount Mismatch) rather than a failed payment?</summary>
    public static bool IsShort(SasPaymentRowDto row, decimal? verifiedAmount = null) =>
        row.FinanceStatus == SampleFinanceStatus.Mismatch &&
        (verifiedAmount ?? ShortFromRemarks(row)) is { } v && v < row.PaidAmount;

    // The list row has no verified amount; a Finance "₹n short" remark (or any remark with
    // "short") marks the amount mismatch, everything else reads as Failed.
    private static decimal? ShortFromRemarks(SasPaymentRowDto row) =>
        row.FinanceRemarks?.Contains("short", StringComparison.OrdinalIgnoreCase) == true ? 0m : null;

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
        SampleFinanceStatus.Mismatch => IsShort(row) ? "Amount Mismatch" : "Failed",
        SampleFinanceStatus.AwaitingVerification => "Pending",
        _ => "Not Forwarded"
    };

    public static string HistoryStatusTone(SasPaymentRowDto row) => row.FinanceStatus switch
    {
        SampleFinanceStatus.Verified => "green",
        SampleFinanceStatus.Mismatch => IsShort(row) ? "orange" : "red",
        SampleFinanceStatus.AwaitingVerification => "yellow",
        _ => "grey"
    };

    // ---------------------------------------------------------------- farmer

    /// <summary>Farmer wording (screen 34): Finance Verified / Admin Approval Pending / Payment Issue.</summary>
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
