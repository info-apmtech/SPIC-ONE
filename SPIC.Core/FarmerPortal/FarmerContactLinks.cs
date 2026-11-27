using System.Globalization;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Call / WhatsApp / Directions links on the farmer dealer and team cards. Dealer phone numbers and
/// map URLs are typed by staff, so everything is normalised here and anything odd gives no link
/// rather than a broken (or unsafe) one. Pure, pinned by the harness.
/// </summary>
public static class FarmerContactLinks
{
    /// <summary>
    /// The number to dial: an Indian mobile as 10 digits; a landline (STD code + number, 10 or 11
    /// digits with the leading 0) kept as typed digits. Null when it is not a usable number.
    /// </summary>
    public static string? DialNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        // Several numbers in one field ("94430 12345 / 0461 2345678"): use the first.
        var first = raw.Split(new[] { '/', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? raw;
        var mobile = FarmerPhone.Normalise(first);
        if (FarmerPhone.IsValid(mobile)) return mobile;

        var digits = new string(first.Where(c => c is >= '0' and <= '9').ToArray());
        if (digits.StartsWith("91", StringComparison.Ordinal) && digits.Length == 12) digits = "0" + digits[2..];
        // Landline: 0 + STD code + subscriber number = 11 digits (or 10 without the 0).
        if (digits.Length == 11 && digits[0] == '0' && digits[1] != '0') return digits;
        if (digits.Length == 10 && digits[0] is >= '1' and <= '5') return "0" + digits;
        return null;
    }

    /// <summary>tel: link for <see cref="DialNumber"/>, or null.</summary>
    public static string? TelHref(string? raw) => DialNumber(raw) is { } n ? "tel:" + (n.Length == 10 ? "+91" + n : n) : null;

    /// <summary>The 10-digit Indian mobile for WhatsApp, or null (landlines have no WhatsApp).</summary>
    public static string? WhatsAppNumber(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var first = raw.Split(new[] { '/', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? raw;
        var mobile = FarmerPhone.Normalise(first);
        return FarmerPhone.IsValid(mobile) ? mobile : null;
    }

    /// <summary>https://wa.me/91XXXXXXXXXX, or null.</summary>
    public static string? WhatsAppHref(string? raw) => WhatsAppNumber(raw) is { } n ? "https://wa.me/91" + n : null;

    /// <summary>
    /// A staff-typed Google map URL, only when it is an absolute http(s) URL (never javascript:, data:
    /// or a relative path). Null otherwise.
    /// </summary>
    public static string? SafeMapUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var t = raw.Trim();
        if (t.Length > 2000) return null;
        return Uri.TryCreate(t, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
               && !string.IsNullOrEmpty(uri.Host)
            ? uri.AbsoluteUri
            : null;
    }

    /// <summary>
    /// Directions: Google Maps directions to the dealer's GPS point when known, else the dealer's own
    /// map link, else a Google Maps search for the firm and its village / district.
    /// </summary>
    public static string? DirectionsUrl(double? lat, double? lng, string? mapUrl, string? searchText)
    {
        if (FarmerGeo.IsValid(lat, lng))
            return "https://www.google.com/maps/dir/?api=1&destination="
                   + lat!.Value.ToString("0.######", CultureInfo.InvariantCulture) + ","
                   + lng!.Value.ToString("0.######", CultureInfo.InvariantCulture);
        if (SafeMapUrl(mapUrl) is { } safe) return safe;
        return string.IsNullOrWhiteSpace(searchText)
            ? null
            : "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(searchText.Trim());
    }

    /// <summary>"98765 43210" for a 10-digit mobile, the digits otherwise.</summary>
    public static string Display(string? number) =>
        number is { Length: 10 } n && FarmerPhone.IsValid(n) ? n[..5] + " " + n[5..] : number ?? string.Empty;
}
