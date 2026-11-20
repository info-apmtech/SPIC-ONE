using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// Guide format v2: module meta in the front matter, section kinds (screen / overview / flow /
/// statuses / roles / faq) and screen meta (screenshots, hotspots, tour steps, minutes). Mistakes
/// are reported in Problems, never thrown.
/// </summary>
internal static class HelpParserV2Tests
{
	public static void Run()
	{
		Console.WriteLine("Help guide format v2");

		ParsesFrontMatterV2();
		ParsesScreenMeta();
		KindDefaultsToScreen();
		ReportsMetaProblems();
	}

	private const string ScreenGuide = """
		---
		module: publicity
		title: Publicity
		category: Marketing
		color: #0d9488
		example: IND-1042 · Banner 6x3
		updated: 2026-11-14
		note: Transfer added
		---

		::: section requests-list
		title: My requests
		routes: /PublicityRequest
		screenshot: list.png
		hotspots: 1 @ 2,4,96,18 | 2 @ 2,26,96,9
		screenshot-phone: list.phone.webp
		hotspots-phone: 1 @ 4,10,92,20 | 2 @ 4,34,92,8
		tour: 1 > [data-help=pub-status-cards] | 2 > [data-help=pub-search]
		minutes: 2

		**What this screen is for**
		Intro.

		1. A
		2. B

		> Tip: something
		:::
		""";

	private static void ParsesFrontMatterV2()
	{
		var g = HelpGuideParser.Parse("publicity", "en", ScreenGuide);
		Check.Equal("category", "Marketing", g.Category);
		Check.Equal("color", "#0d9488", g.Color);
		Check.Equal("example", "IND-1042 · Banner 6x3", g.Example);
		Check.Equal("updated", new DateOnly(2026, 11, 14), g.Updated);
		Check.Equal("note", "Transfer added", g.Note);
		Check.Equal("v2 guide parses cleanly", "", string.Join(" | ", g.Problems));
	}

	private static void ParsesScreenMeta()
	{
		var s = HelpGuideParser.Parse("publicity", "en", ScreenGuide).Sections[0];
		Check.Equal("screen kind", "screen", s.Kind);
		Check.That("screen meta present", s.Screen is not null);
		Check.Equal("desktop image", "list.png", s.Screen!.Image);
		Check.Equal("second hotspot", new HelpHotspot(2, 2, 26, 96, 9), s.Screen.Hotspots[1]);
		Check.Equal("phone image", "list.phone.webp", s.Screen.PhoneImage);
		Check.Equal("phone hotspots", 2, s.Screen.PhoneHotspots.Count);
		Check.Equal("tour target", "[data-help=pub-status-cards]", s.Screen.Tour[0].Target);
		Check.Equal("tour steps", 2, s.Screen.Tour.Count);
		Check.Equal("minutes", 2, s.Screen.Minutes);
		Check.Equal("steps are the numbered list", "A,B", string.Join(",", HelpSteps.Of(s.Body)));
	}

	private static void KindDefaultsToScreen()
	{
		var s = HelpGuideParser.Parse("publicity", "en", HelpParserTests.SampleGuide).Sections[0];
		Check.Equal("v1 section is a screen", "screen", s.Kind);
		Check.That("v1 screen meta present", s.Screen is not null);
		Check.Equal("v1 has no hotspots", 0, s.Screen!.Hotspots.Count);
		Check.Equal("v1 has no image", null, s.Screen.Image);
	}

	private static string Section(string headers) => $"""
		---
		module: publicity
		title: Publicity
		---

		::: section one
		title: One
		routes: /PublicityRequest
		{headers}

		1. A
		2. B
		:::
		""";

	private static void OneProblem(string name, string headers, string expect)
	{
		var g = HelpGuideParser.Parse("publicity", "en", Section(headers));
		Check.Equal(name + ": exactly one problem", 1, g.Problems.Count);
		Check.That(name + ": names the section and '" + expect + "'",
			g.Problems.Count == 1 && g.Problems[0].Contains("one") && g.Problems[0].Contains(expect));
	}

	private static void ReportsMetaProblems()
	{
		OneProblem("too many hotspots", "hotspots: 1 @ 1,1,1,1 | 2 @ 1,1,1,1 | 3 @ 1,1,1,1", "hotspot");
		OneProblem("tour count", "tour: 1 > [data-help=a]", "tour");
		OneProblem("malformed hotspot", "hotspots: 1 @ x | 2 @ 1,1,1,1", "hotspot");
		OneProblem("hotspot out of range", "hotspots: 1 @ 2,4,120,18 | 2 @ 1,1,1,1", "0–100");
		OneProblem("unknown kind", "kind: chart", "kind");

		var g = HelpGuideParser.Parse("publicity", "en", Section("kind: chart"));
		Check.Equal("unknown kind kept as screen", "screen", g.Sections[0].Kind);
	}
}
