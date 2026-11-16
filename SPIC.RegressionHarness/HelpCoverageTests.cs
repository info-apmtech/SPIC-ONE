using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// The hand-run coverage check (help-tools check): screens with no guide, guide filters naming a
/// page that does not exist, missing screenshots, and translations left behind by an English edit.
/// It is the main guard against guides drifting from the screens, so each kind of issue is pinned.
/// </summary>
internal static class HelpCoverageTests
{
	private const string Lab = """
		---
		module: lab
		title: SAS Lab
		---

		::: section entry
		title: Enter test results
		routes: /Lab/TestEntry/{id}
		for: LabTestEntry.Entry

		![Entry form](images/entry.png)
		:::
		""";

	private static readonly string[] Permissions = { "LabTestEntry", "PublicityRequest" };

	public static void Run()
	{
		Console.WriteLine("Help coverage check");

		UncoveredRouteReported();
		ParameterRoutesMatchByShape();
		IgnoredRouteNotReported();
		UnknownPermissionReported();
		BareForTokenReported();
		StaleTranslationReported();
		MissingImageReported();
		ParseProblemReported();
	}

	private static IReadOnlyList<CoverageIssue> Run(HelpLibrary lib, string[] pages, string[]? ignored = null, Func<string, bool>? imageExists = null) =>
		HelpCoverage.Check(lib, pages, ignored ?? Array.Empty<string>(), imageExists ?? (_ => true), Permissions);

	private static int Count(IReadOnlyList<CoverageIssue> issues, string kind) => issues.Count(i => i.Kind == kind);

	private static void UncoveredRouteReported()
	{
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", Lab)), new[] { "/Lab/Reports" });
		Check.That("uncovered page reported", issues.Any(i => i.Kind == "uncovered-route" && i.Detail == "/Lab/Reports"));
	}

	private static void ParameterRoutesMatchByShape()
	{
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", Lab)), new[] { "/Lab/TestEntry/{batchId:int}" });
		Check.Equal("{batchId:int} page is covered by {id} section route", 0, Count(issues, "uncovered-route"));
	}

	private static void IgnoredRouteNotReported()
	{
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", Lab)), new[] { "/login", "/Welcome/" }, new[] { "/login", "/welcome" });
		Check.Equal("ignored pages not reported", 0, Count(issues, "uncovered-route"));
	}

	private static void UnknownPermissionReported()
	{
		var bad = Lab.Replace("for: LabTestEntry.Entry", "for: NotAPage.View");
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", bad)), Array.Empty<string>());
		Check.That("unknown permission reported", issues.Any(i => i.Kind == "unknown-permission" && i.Detail.Contains("NotAPage")));
	}

	// Final review (minor 14, promoted): "for: LabTestEntry" without ".Action" hides the section from
	// everyone; the check must say so instead of passing.
	private static void BareForTokenReported()
	{
		var bad = Lab.Replace("for: LabTestEntry.Entry", "for: LabTestEntry");
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", bad)), Array.Empty<string>());
		Check.That("for: token without .Action reported", issues.Any(i => i.Kind == "unknown-permission" && i.Detail.Contains("Page.Action")));
	}

	private static void StaleTranslationReported()
	{
		var tamil = """
			::: section entry
			title: முடிவுகள்
			source-hash: 0000000000000000

			பழையது.
			:::
			""";
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", Lab), ("help/lab/guide.ta.md", tamil)), Array.Empty<string>());
		Check.That("stale Tamil section reported", issues.Any(i => i.Kind == "stale-translation" && i.Detail.Contains("ta") && i.Detail.Contains("entry")));
		Check.That("Telugu with no file reported as missing", issues.Any(i => i.Kind == "missing-translation" && i.Detail.StartsWith("te", StringComparison.Ordinal)));
	}

	private static void MissingImageReported()
	{
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", Lab)), Array.Empty<string>(),
			imageExists: path => path != "lab/images/entry.png");
		Check.That("missing screenshot reported", issues.Any(i => i.Kind == "missing-image" && i.Detail.Contains("images/entry.png")));
	}

	private static void ParseProblemReported()
	{
		var issues = Run(HelpLibraryTests.Library(("help/lab/guide.en.md", Lab + "\n::: section open\ntitle: X\n\nno end")), Array.Empty<string>());
		Check.Equal("parse problem reported", 1, Count(issues, "parse-problem"));
	}
}
