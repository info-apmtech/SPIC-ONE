using SPIC.Core.Entities;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal Task 1b (design system): the pure helpers behind the Fp components (IST greeting,
/// OTP box normalisation, zones, names) and the home tile registry.
/// </summary>
internal static class FarmerPortalUiTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal: design system helpers and home tiles");

		// IST greeting from UTC instants. IST = UTC + 05:30, so 04:59 IST = 23:29 UTC the day before.
		Greet("04:59 IST is evening", 2026, 11, 27, 23, 29, "Good evening");
		Greet("05:00 IST is morning", 2026, 11, 27, 23, 30, "Good morning");
		Greet("11:59 IST is morning", 2026, 11, 28, 6, 29, "Good morning");
		Greet("12:00 IST is afternoon", 2026, 11, 28, 6, 30, "Good afternoon");
		Greet("16:59 IST is afternoon", 2026, 11, 28, 11, 29, "Good afternoon");
		Greet("17:00 IST is evening", 2026, 11, 28, 11, 30, "Good evening");
		Greet("23:59 IST is evening", 2026, 11, 28, 18, 29, "Good evening");
		Greet("00:00 IST is evening", 2026, 11, 28, 18, 30, "Good evening");
		Check.Equal("greeting: an Unspecified kind is read as UTC", "Good morning",
			FpUi.Greeting(new DateTime(2026, 11, 28, 2, 0, 0, DateTimeKind.Unspecified)));
		Check.Equal("greeting: a Local kind is converted to UTC first", FpUi.Greeting(new DateTime(2026, 11, 28, 2, 0, 0, DateTimeKind.Utc).ToLocalTime()),
			FpUi.Greeting(new DateTime(2026, 11, 28, 2, 0, 0, DateTimeKind.Utc)));
		Check.Equal("IST date line rolls over at 18:30 UTC", "Sunday, 29 November",
			FpUi.IstDateLine(new DateTime(2026, 11, 28, 18, 30, 0, DateTimeKind.Utc)));
		Check.Equal("IST date line before 18:30 UTC", "Saturday, 28 November",
			FpUi.IstDateLine(new DateTime(2026, 11, 28, 18, 29, 0, DateTimeKind.Utc)));

		// OTP boxes: what typing / pasting leaves in the six boxes.
		Check.Equal("otp: plain six digits", "482913", FpUi.NormaliseOtp("482913"));
		Check.Equal("otp: spaces from a paste", "482913", FpUi.NormaliseOtp("482 913"));
		Check.Equal("otp: dashes and tabs", "482913", FpUi.NormaliseOtp("48-29\t13"));
		Check.Equal("otp: a whole SMS gives the code", "482913", FpUi.NormaliseOtp("Your SPIC ONE code is 482913. Valid for 5 min."));
		Check.Equal("otp: SMS with the code and another number", "482913", FpUi.NormaliseOtp("482913 is your code. Do not share. 5 min"));
		Check.Equal("otp: seven digits keep the first six", "123456", FpUi.NormaliseOtp("1234567"));
		Check.Equal("otp: two six-digit runs fall back to all digits (first six)", "111111", FpUi.NormaliseOtp("111111 or 222222"));
		Check.Equal("otp: partial", "48", FpUi.NormaliseOtp("4a8"));
		Check.Equal("otp: Devanagari digits are dropped", "", FpUi.NormaliseOtp("४८२९१३"));
		Check.Equal("otp: Tamil digits are dropped", "12", FpUi.NormaliseOtp("௧௨1 2"));
		Check.Equal("otp: null", "", FpUi.NormaliseOtp(null));
		Check.Equal("otp: empty", "", FpUi.NormaliseOtp(""));
		Check.Equal("otp: four-box length", "1234", FpUi.NormaliseOtp("12 34 56", 4));

		// Zones (FpZoneSlider / FpStatus): below LowBelow Low, above HighAbove High, thresholds Medium.
		Check.Equal("zone: null is Unknown", FpLevel.Unknown, FpUi.ZoneLevel(null, 280, 560));
		Check.Equal("zone: NaN is Unknown", FpLevel.Unknown, FpUi.ZoneLevel(double.NaN, 280, 560));
		Check.Equal("zone: just below low", FpLevel.Low, FpUi.ZoneLevel(279.9, 280, 560));
		Check.Equal("zone: low threshold is Medium", FpLevel.Medium, FpUi.ZoneLevel(280, 280, 560));
		Check.Equal("zone: high threshold is Medium", FpLevel.Medium, FpUi.ZoneLevel(560, 280, 560));
		Check.Equal("zone: above high", FpLevel.High, FpUi.ZoneLevel(560.1, 280, 560));
		Check.Equal("fraction: middle", 0.5, FpUi.Fraction(50, 0, 100));
		Check.Equal("fraction: clamped low", 0.0, FpUi.Fraction(-5, 0, 100));
		Check.Equal("fraction: clamped high", 1.0, FpUi.Fraction(500, 0, 100));
		Check.Equal("fraction: empty range", 0.0, FpUi.Fraction(5, 10, 10));
		Check.Equal("pct: dot decimal separator", "37.5%", FpUi.Pct(0.375));
		Check.Equal("level labels", "Low|Medium|High|Not set",
			string.Join("|", new[] { FpLevel.Low, FpLevel.Medium, FpLevel.High, FpLevel.Unknown }.Select(FpUi.LevelLabel)));

		// Names and countdown.
		Check.Equal("short name: first word", "Murugan", FpUi.ShortName("Murugan K"));
		Check.Equal("short name: skips a leading initial", "Murugan", FpUi.ShortName("K. Murugan"));
		Check.Equal("short name: skips an initial without a dot", "Selvi", FpUi.ShortName("R Selvi"));
		Check.Equal("short name: all initials keeps the name", "K. R.", FpUi.ShortName("K. R."));
		Check.Equal("short name: empty", "", FpUi.ShortName("  "));
		Check.Equal("initials: two words", "MK", FpUi.Initials("murugan k"));
		Check.Equal("initials: dotted initial", "KM", FpUi.Initials("K. Murugan"));
		Check.Equal("initials: empty", "?", FpUi.Initials(null));
		Check.Equal("countdown 75 s", "1:15", FpUi.Countdown(75));
		Check.Equal("countdown 9 s", "0:09", FpUi.Countdown(9));
		Check.Equal("countdown never negative", "0:00", FpUi.Countdown(-3));
		Check.Equal("crop emoji: paddy", "\U0001F33E", FpUi.CropEmoji("Paddy"));
		Check.That("crop emoji: unknown crop has none (leaf icon instead)", FpUi.CropEmoji("Turmeric") is null);

		// Home tile registry.
		var ids = FarmerFeatureTiles.All.Select(t => t.Id).ToList();
		Check.That("tiles: ids are unique", ids.Distinct().Count() == ids.Count);
		Check.That("tiles: every tile has a title, description, bi- icon and route",
			FarmerFeatureTiles.All.All(t => t.Title.Length > 0 && t.Description.Length > 0 && t.Icon.StartsWith("bi-") && t.Route.StartsWith('/')));
		Check.Equal("hue css class", "fp-hue-soil", FpUi.HueCss(FpHue.Soil));
		var farmer = TestLogin.As(AppRole.Farmer, "");
		Check.Equal("tiles: farmer without soil tests sees only existing features", "profile",
			string.Join(",", FarmerFeatureTiles.For(farmer).Select(t => t.Id)));
		var linked = TestLogin.As(AppRole.Farmer, "");
		linked.SetFarmerSasLinked(true);
		Check.Equal("tiles: linked farmer also sees the soil report and sample tiles", "soil-reports,soil-samples,profile",
			string.Join(",", FarmerFeatureTiles.For(linked).Select(t => t.Id)));
		Check.That("tiles: every tile's route opens for a (linked) farmer",
			FarmerFeatureTiles.All.All(t => RouteAccess.CanOpen(linked, t.Route, null)));

		// No placeholder tiles: every route must be a real @page in the app (an unbuilt feature has no tile).
		var pages = typeof(FpUi).Assembly.GetTypes()
			.SelectMany(t => t.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), false))
			.Cast<Microsoft.AspNetCore.Components.RouteAttribute>()
			.Select(r => r.Template.TrimEnd('/'))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		foreach (var tile in FarmerFeatureTiles.All)
			Check.That($"tiles: {tile.Id} route {tile.Route} is a real page", pages.Contains(tile.Route.TrimEnd('/')));
	}

	private static void Greet(string name, int y, int m, int d, int hh, int mm, string expected) =>
		Check.Equal("greeting: " + name, expected, FpUi.Greeting(new DateTime(y, m, d, hh, mm, 0, DateTimeKind.Utc)));
}
