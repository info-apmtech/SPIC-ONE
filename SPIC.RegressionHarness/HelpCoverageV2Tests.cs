using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// help-tools check for v2 guides: block mistakes, missing screenshots (phone only warns), tour
/// targets that no page carries, and role paths that point at nothing.
/// </summary>
internal static class HelpCoverageV2Tests
{
	public static void Run()
	{
		Console.WriteLine("Help coverage v2");

		BlockProblemReported();
		MissingScreenshotReported();
		PhoneImageIsWarning();
		TourTargetReported();
		PathReported();
		CleanGuideHasNoIssues();
	}

	private const string Guide = """
		---
		module: publicity
		title: Publicity
		---

		::: section flow
		title: Flow
		kind: flow

		- rmdo | type: lane
		  title: RMDO
		- raise | lane: rmdo | col: 1 | screen: requests-list
		  title: Raise
		:::

		::: section requests-list
		title: My requests
		routes: /PublicityRequest
		screenshot: list.png
		screenshot-phone: list.phone.webp
		tour: 1 > [data-help=pub-cards] | 2 > [data-help="pub-search"]

		1. A
		2. B
		:::
		""";

	private const string Paths = """
		{ "paths": [ { "id": "rmdo", "designations": ["RMDO"], "title": { "en": "Path" },
		  "steps": [ { "module": "publicity", "section": "requests-list", "minutes": 2 } ] } ] }
		""";

	private static HelpLibrary Lib(string guide, string paths = Paths) =>
		new(new[] { ("publicity/guide.en.md", guide), ("paths.json", paths) });

	private static readonly IReadOnlySet<string> AllIds = new HashSet<string> { "pub-cards", "pub-search" };

	private static IReadOnlyList<CoverageIssue> Run(HelpLibrary lib, Func<string, bool>? exists = null, IReadOnlySet<string>? ids = null) =>
		HelpCoverage.Check(lib, new[] { "/PublicityRequest" }, Array.Empty<string>(), exists ?? (_ => true), Array.Empty<string>(), ids ?? AllIds);

	private static void BlockProblemReported()
	{
		var issues = Run(Lib(Guide.Replace("lane: rmdo | col: 1", "lane: ghost | col: 1")));
		Check.That("block problem reported", issues.Any(i => i.Kind == "block" && i.Detail.Contains("ghost") && !i.Warning));
	}

	private static void MissingScreenshotReported()
	{
		var issues = Run(Lib(Guide), p => !p.EndsWith("list.png"));
		Check.That("missing desktop screenshot is an error", issues.Any(i => i.Kind == "missing-image" && i.Detail.Contains("list.png") && !i.Warning));
	}

	private static void PhoneImageIsWarning()
	{
		var missingFile = Run(Lib(Guide), p => !p.EndsWith(".webp"));
		Check.That("missing phone file is a warning", missingFile.Any(i => i.Kind == "missing-phone-image" && i.Warning));
		var noKey = Run(Lib(Guide.Replace("screenshot-phone: list.phone.webp\n", "").Replace("screenshot-phone: list.phone.webp\r\n", "")));
		Check.That("no phone picture at all is a warning", noKey.Any(i => i.Kind == "missing-phone-image" && i.Warning));
	}

	private static void TourTargetReported()
	{
		var issues = Run(Lib(Guide), ids: new HashSet<string> { "pub-cards" });
		Check.Equal("one tour target missing", 1, issues.Count(i => i.Kind == "tour-target"));
		Check.That("names the quoted target", issues.Any(i => i.Kind == "tour-target" && i.Detail.Contains("pub-search")));
	}

	private static void PathReported()
	{
		var bad = Paths.Replace("\"section\": \"requests-list\"", "\"section\": \"nowhere\"");
		var issues = Run(Lib(Guide, bad));
		Check.That("path to a missing section reported", issues.Any(i => i.Kind == "path" && i.Detail.Contains("nowhere")));
		var flowStep = Paths.Replace("\"section\": \"requests-list\"", "\"section\": \"flow\"");
		Check.That("path to a block reported", Run(Lib(Guide, flowStep)).Any(i => i.Kind == "path" && i.Detail.Contains("flow")));
	}

	private static void CleanGuideHasNoIssues()
	{
		var issues = Run(Lib(Guide)).Where(i => i.Kind != "missing-translation").ToList();
		Check.Equal("clean v2 guide has no issues", "", string.Join(" | ", issues.Select(i => $"{i.Kind}: {i.Detail}")));
	}
}
