using Spic.Infrastructure.Services.MasterData;

namespace SPIC.RegressionHarness;

/// <summary>
/// Pins the canonical master-name rules that every bulk upload now shares.
///
/// These are the cases that used to differ between importers, which is how the same
/// logical master could read as "missing" in one flow and present in another.
/// </summary>
internal static class MasterNormalizerTests
{
	// Built in code rather than written as an escape in source: a literal BEL byte
	// would make this file read as binary.
	private const char Bell = (char)7;
	private const char ZeroWidthSpace = '\u200B';

	public static void Run()
	{
		Console.WriteLine("MasterNormalizer");

		// Internal spaces are meaningful. "North Zone" and "NorthZone" are different
		// master names and must never collapse onto each other.
		Check.That(
			"internal spaces are preserved, not deleted",
			MasterNormalizer.Normalize("North Zone") != MasterNormalizer.Normalize("NorthZone"));

		Check.Equal(
			"casing is ignored",
			MasterNormalizer.Normalize("TAMIL NADU"),
			MasterNormalizer.Normalize("tamil nadu"));

		Check.Equal(
			"surrounding whitespace is trimmed",
			MasterNormalizer.Normalize("   Tamil Nadu   "),
			MasterNormalizer.Normalize("Tamil Nadu"));

		Check.Equal(
			"whitespace runs collapse to a single space",
			MasterNormalizer.Normalize("North    Zone"),
			MasterNormalizer.Normalize("North Zone"));

		Check.Equal(
			"tabs and newlines are whitespace, so they collapse to one space",
			MasterNormalizer.Normalize("North\t\nZone"),
			MasterNormalizer.Normalize("North Zone"));

		// A control or zero-width character sitting *between* two words is noise, so it
		// disappears entirely rather than leaving a space behind. That is the difference
		// between removing a character and replacing it with a separator.
		Check.Equal(
			"control characters are removed without leaving a space",
			MasterNormalizer.Normalize($"Tamil{Bell}Nadu"),
			MasterNormalizer.Normalize("TamilNadu"));

		Check.Equal(
			"zero-width characters are removed without leaving a space",
			MasterNormalizer.Normalize($"Tamil{ZeroWidthSpace}Nadu"),
			MasterNormalizer.Normalize("TamilNadu"));

		Check.Equal(
			"byte-order marks are removed",
			MasterNormalizer.Normalize("\uFEFFTamil Nadu"),
			MasterNormalizer.Normalize("Tamil Nadu"));

		Check.Equal(
			"edge punctuation is trimmed",
			MasterNormalizer.Normalize("--Tamil Nadu--"),
			MasterNormalizer.Normalize("Tamil Nadu"));

		Check.Equal(
			"normalization is idempotent",
			MasterNormalizer.Normalize(MasterNormalizer.Normalize("  north   zone ")),
			MasterNormalizer.Normalize("North Zone"));

		Check.Equal("null is blank", MasterNormalizer.Normalize(null), string.Empty);
		Check.Equal("empty is blank", MasterNormalizer.Normalize(string.Empty), string.Empty);
		Check.Equal("whitespace only is blank", MasterNormalizer.Normalize("   "), string.Empty);
		Check.That("IsBlank agrees with Normalize", MasterNormalizer.IsBlank("  \t "));
		Check.That("IsBlank rejects real content", !MasterNormalizer.IsBlank(" Tamil Nadu "));

		// Header names have no meaningful internal spacing, so unlike master names they
		// lose all whitespace. "Product Name" and "ProductName" are the same column.
		Check.Equal(
			"header matching ignores all whitespace",
			MasterNormalizer.NormalizeHeader("Product Name"),
			MasterNormalizer.NormalizeHeader("ProductName"));

		Check.Equal(
			"header matching ignores underscores and hyphens",
			MasterNormalizer.NormalizeHeader("Product_Group-Name"),
			MasterNormalizer.NormalizeHeader("Product Group Name"));

		Check.Equal(
			"header matching is case insensitive",
			MasterNormalizer.NormalizeHeader("ISACTIVE"),
			MasterNormalizer.NormalizeHeader("isactive"));

		// Parent scoping keeps the same child name under two parents distinct.
		Check.That(
			"the same child name under different parents is a different key",
			MasterNormalizer.Scoped("Chennai", 1) != MasterNormalizer.Scoped("Chennai", 2));

		Check.Equal(
			"scoped keys ignore casing and padding",
			MasterNormalizer.Scoped("  chennai ", 7),
			MasterNormalizer.Scoped("CHENNAI", 7));
	}
}
