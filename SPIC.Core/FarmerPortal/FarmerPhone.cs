using System.Text.RegularExpressions;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Indian mobile numbers as the farmer portal stores them: exactly 10 digits starting 6-9.
/// Shared by the API (OTP endpoints) and the app (login form) so both accept the same input.
/// </summary>
public static partial class FarmerPhone
{
    [GeneratedRegex(@"^[6-9][0-9]{9}$")]
    private static partial Regex ValidPattern();

    /// <summary>
    /// Strips spaces, dashes, brackets and dots, then a leading "+91" / "91" (only when 12 digits
    /// remain) or a leading "0". Returns the trimmed digits even when they are not valid; check
    /// with <see cref="IsValid"/>.
    /// </summary>
    public static string Normalise(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var s = new string(input.Where(c => !char.IsWhiteSpace(c) && c is not ('-' or '(' or ')' or '.')).ToArray());

        if (s.StartsWith("+91", StringComparison.Ordinal)) s = s[3..];
        else if (s.StartsWith("0091", StringComparison.Ordinal)) s = s[4..];
        else if (s.Length == 12 && s.StartsWith("91", StringComparison.Ordinal)) s = s[2..];

        if (s.Length == 11 && s.StartsWith('0')) s = s[1..];

        return s;
    }

    public static bool IsValid(string? normalised) =>
        !string.IsNullOrEmpty(normalised) && ValidPattern().IsMatch(normalised);

    /// <summary>For logs: "9876543221" -> "98xxxxxx21". Never log a full number.</summary>
    public static string Mask(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return "(none)";
        if (phone.Length < 4) return new string('x', phone.Length);
        return phone[..2] + new string('x', phone.Length - 4) + phone[^2..];
    }

    /// <summary>
    /// The spellings a staff record may hold for the same number (PhoneNumber / UserName), used to
    /// spot a mobile that already belongs to a non-farmer account.
    /// </summary>
    public static string[] StoredVariants(string normalised) => new[]
    {
        normalised,
        "0" + normalised,
        "91" + normalised,
        "+91" + normalised,
        "+91 " + normalised,
        "+91-" + normalised
    };

    /// <summary>
    /// True when <paramref name="stored"/> is another spelling (see <see cref="StoredVariants"/>) of the verified
    /// number <paramref name="normalised"/>, not the plain 10 digits (recheck fix B3: an admin-made Farmer account
    /// with "+91 98765 43210" is rewritten to the 10 digits at OTP sign-in).
    /// </summary>
    public static bool IsOtherSpelling(string? stored, string normalised) =>
        !string.IsNullOrEmpty(stored) && stored != normalised && StoredVariants(normalised).Contains(stored);

    /// <summary>
    /// The value for FarmerProfiles.Phone (varchar 10) from the account's PhoneNumber: the normalised 10 digits when
    /// that is a valid mobile, else the profile's current value (recheck fix B3; a "+91..." spelling used to overflow
    /// the column and every profile save failed).
    /// </summary>
    public static string ProfilePhone(string? accountPhone, string? current)
    {
        var n = Normalise(accountPhone);
        return IsValid(n) ? n : (current ?? string.Empty);
    }

    /// <summary>Identity user name of a self signed-up farmer.</summary>
    public static string UserNameFor(string normalised) => "f" + normalised;
}
