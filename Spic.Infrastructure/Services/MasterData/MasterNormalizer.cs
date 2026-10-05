using System.Text;

namespace Spic.Infrastructure.Services.MasterData;

/// <summary>
/// The one canonical way a master-data name is turned into a comparison key.
///
/// Every bulk upload in the application routes its master matching through this
/// type, so a name that resolves in one importer resolves identically in all of
/// them. Before this existed the solution carried five mutually incompatible
/// schemes (raw <c>NormalizeKey</c>, <c>.Trim()</c> + <c>OrdinalIgnoreCase</c>,
/// <c>.Trim().ToUpperInvariant()</c>, culture-sensitive <c>.ToLower()</c>, and a
/// <c>MasterKey</c> that deleted every whitespace character), which is how the
/// same logical master could read as "missing" in one flow and present in another.
///
/// Normalization rules:
///   * control, zero-width and BOM characters are removed;
///   * every run of whitespace collapses to a single space;
///   * leading and trailing whitespace is removed;
///   * leading and trailing punctuation is removed;
///   * the result is upper-cased with the invariant culture.
///
/// Internal spaces are deliberately preserved: "North Zone" and "NorthZone" are
/// different master names and must never collapse onto each other.
/// </summary>
public static class MasterNormalizer
{
    /// <summary>
    /// Unit Separator (U+001F). It cannot occur inside a master name, so a
    /// composite key can never collide with a differently scoped one.
    /// </summary>
    private const char ParentKeySeparator = '\u001F';

    private static readonly char[] EdgePunctuation =
    {
        '.', ',', '-', '_', '/', '\\', ':', ';', '"', '\'', '(', ')', '[', ']', '*', '#'
    };

    /// <summary>
    /// Characters that carry no meaning inside a name and vary between keyboard
    /// layouts, spreadsheets and copy/paste.
    /// </summary>
    private static bool IsIgnorable(char c) =>
        char.IsControl(c) || c == '\uFEFF' || c == '\u200B' || c == '\u200C' || c == '\u200D';

    /// <summary>Canonical comparison key, or "" when there is nothing to compare.</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var builder = new StringBuilder(raw.Length);
        var pendingSpace = false;

        foreach (var c in raw)
        {
            // Whitespace is checked before ignorable characters on purpose. Tab,
            // carriage return and newline all satisfy char.IsControl, so testing
            // IsIgnorable first would delete them outright — and "North\tZone"
            // would then stop matching "North Zone", which is exactly the kind of
            // invisible mismatch this type exists to prevent.
            if (char.IsWhiteSpace(c))
            {
                // Remember the space; whether it survives depends on a non-space
                // character following it, which also trims leading whitespace.
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (IsIgnorable(c))
                continue;

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(c);
        }

        var collapsed = builder.ToString().Trim(EdgePunctuation).Trim();

        return collapsed.Length == 0 ? string.Empty : collapsed.ToUpperInvariant();
    }

    /// <summary>
    /// Canonical key for a child master, scoped by its parent id, so the same
    /// child name under two different parents stays two different masters.
    /// </summary>
    public static string Scoped(string? name, int parentId) =>
        Normalize(name) + ParentKeySeparator +
        parentId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>True when the value carries nothing that could identify a master.</summary>
    public static bool IsBlank(string? raw) => Normalize(raw).Length == 0;

    /// <summary>
    /// Header-name normalization. Headers have no meaningful internal spacing
    /// ("Product Name" and "ProductName" are the same column), so unlike
    /// <see cref="Normalize"/> this removes all whitespace.
    /// </summary>
    public static string NormalizeHeader(string? raw) =>
        string.Concat((raw ?? string.Empty)
            .Where(c => !char.IsWhiteSpace(c) && !IsIgnorable(c) && c != '_' && c != '-')
            .Select(char.ToLowerInvariant));
}
