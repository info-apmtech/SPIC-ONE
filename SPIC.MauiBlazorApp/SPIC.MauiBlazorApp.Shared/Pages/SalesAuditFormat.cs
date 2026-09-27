using System.Globalization;

namespace SPIC.MauiBlazorApp.Shared.Pages;

/// <summary>Number formatting shared by the Sales Audit flow pages.</summary>
public static class SalesAuditFormat
{
    private const decimal OneCrore = 1_00_00_000m;

    /// <summary>
    /// Indian digit grouping: 23000000 -> "2,30,00,000". Done by hand so it does not depend on
    /// the en-IN culture being available in the host (invariant globalization mode).
    /// </summary>
    public static string Indian(decimal value)
    {
        var digits = Math.Round(Math.Abs(value)).ToString("0", CultureInfo.InvariantCulture);
        if (digits.Length > 3)
        {
            var head = digits[..^3];
            var tail = digits[^3..];
            var groups = new List<string>();
            while (head.Length > 2)
            {
                groups.Insert(0, head[^2..]);
                head = head[..^2];
            }
            groups.Insert(0, head);
            digits = string.Join(",", groups) + "," + tail;
        }
        return value < 0 ? "-" + digits : digits;
    }

    /// <summary>"₹ 3,30,000"</summary>
    public static string Rupees(decimal value) => "₹ " + Indian(value);

    /// <summary>Rupees expressed in crores with two decimals: 23000000 -> "2.30".</summary>
    public static string Crore(decimal rupees) =>
        (rupees / OneCrore).ToString("0.00", CultureInfo.InvariantCulture);

    /// <summary>Accepts "₹ 80,00,000", "80,00,000" or "8000000"; returns the fallback on bad input.</summary>
    public static decimal ParseAmount(object? raw, decimal fallback)
    {
        var digits = new string((raw?.ToString() ?? "").Where(c => char.IsDigit(c) || c == '.').ToArray());
        return decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? Math.Max(0, value)
            : fallback;
    }
}
