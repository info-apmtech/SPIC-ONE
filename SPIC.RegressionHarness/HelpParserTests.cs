using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// The Help Desk guide format: front matter + "::: section" blocks, the section hash that ties a
/// translation (and its voice clip) to the English text it came from, route matching for the
/// "?" button, and the two renderers (HTML for the page, plain text for the voice).
/// </summary>
internal static class HelpParserTests
{
	internal const string SampleGuide = """
		---
		module: publicity
		title: Publicity Material Tracking
		icon: bi-megaphone
		order: 40
		---

		::: section raise-request
		title: Raise a publicity request
		routes: /PublicityRequest, /PublicityRequest/new, /PublicityRequest/edit/{id}
		for: PublicityRequest.Entry
		roles:
		designations:
		source-hash:
		confidence:

		**What this screen is for** - asking for publicity material.

		1. Tap **New request**.
		2. Fill the form.
		:::
		""";

	public static void Run()
	{
		Console.WriteLine("Help guide parser");

		ParsesFrontMatterAndSections();
		MalformedSectionIsReportedNotThrown();
		DuplicateSectionIdReported();
		HashIgnoresLineEndingsAndTrailingSpaces();
		RouteMatchingIgnoresCaseQueryAndTrailingSlash();
		HtmlIsEscaped();
		SpeechStripsMarkup();
	}

	private static void ParsesFrontMatterAndSections()
	{
		var guide = HelpGuideParser.Parse("publicity", "en", SampleGuide);
		Check.Equal("module read from front matter", "publicity", guide.Module);
		Check.Equal("order read from front matter", 40, guide.Order);
		Check.Equal("title read from front matter", "Publicity Material Tracking", guide.Title);
		Check.Equal("one section parsed", 1, guide.Sections.Count);
		var s = guide.Sections[0];
		Check.Equal("section id", "raise-request", s.Id);
		Check.Equal("three routes", 3, s.Routes.Count);
		Check.Equal("for token", "PublicityRequest.Entry", s.For[0]);
		Check.Equal("empty roles list", 0, s.Roles.Count);
		Check.Equal("English is the source", HelpConfidence.Source, s.Confidence);
		Check.That("body kept", s.Body.Contains("1. Tap **New request**."));
		Check.Equal("no problems", 0, guide.Problems.Count);
	}

	private static void MalformedSectionIsReportedNotThrown()
	{
		var text = SampleGuide + "\n::: section broken\ntitle: Broken\nroutes: /X\n\nno closing marker";
		HelpGuide? guide = null;
		try { guide = HelpGuideParser.Parse("publicity", "en", text); }
		catch (Exception ex) { Check.That("malformed guide does not throw", false, ex.Message); return; }
		Check.Equal("good section before it is kept", 1, guide.Sections.Count);
		Check.Equal("one problem reported", 1, guide.Problems.Count);
	}

	private static void DuplicateSectionIdReported()
	{
		var text = SampleGuide + "\n" + SampleGuide[SampleGuide.IndexOf("::: section", StringComparison.Ordinal)..];
		var guide = HelpGuideParser.Parse("publicity", "en", text);
		Check.Equal("duplicate keeps first only", 1, guide.Sections.Count);
		Check.That("duplicate id reported", guide.Problems.Count == 1 && guide.Problems[0].Contains("raise-request"));
	}

	private static void HashIgnoresLineEndingsAndTrailingSpaces()
	{
		Check.Equal("hash ignores CRLF and trailing spaces", HelpHash.Of("T", "a\nb"), HelpHash.Of("T", "a\r\nb  "));
		Check.That("hash changes with a word", HelpHash.Of("T", "a b") != HelpHash.Of("T", "a c"));
		Check.Equal("hash is 16 hex chars", 16, HelpHash.Of("T", "x").Length);
	}

	private static void RouteMatchingIgnoresCaseQueryAndTrailingSlash()
	{
		Check.That("absolute URL, case, query, fragment and trailing slash ignored",
			HelpRoutes.Matches("/PublicityRequest/edit/{id}", "https://x/publicityrequest/EDIT/12/?a=1#t"));
		Check.That("missing parameter segment does not match",
			!HelpRoutes.Matches("/PublicityRequest/edit/{id}", "/PublicityRequest/edit"));
		Check.That("longer path does not match a shorter route",
			!HelpRoutes.Matches("/PublicityRequest", "/PublicityRequest/new"));
		Check.Equal("sample fills parameters", "/PublicityRequest/edit/0", HelpRoutes.Sample("/PublicityRequest/edit/{id}"));
	}

	private static void HtmlIsEscaped()
	{
		var html = HelpMarkdown.ToHtml("<script>x</script> & **b**", "/img");
		Check.That("script tag escaped", html.Contains("&lt;script&gt;") && !html.Contains("<script>"));
		Check.That("bold rendered", html.Contains("<strong>b</strong>"));
		var img = HelpMarkdown.ToHtml("![Form](images/form.png)", "_content/x/help/publicity");
		Check.That("image path rewritten", img.Contains("src=\"_content/x/help/publicity/images/form.png\""));
	}

	private static void SpeechStripsMarkup()
	{
		var speech = HelpMarkdown.ToSpeech("Raise", "1. Tap **New**\n![a](images/x.png)");
		Check.That("title spoken first", speech.StartsWith("Raise", StringComparison.Ordinal));
		Check.That("step numbers spoken", speech.Contains("Step 1. Tap New"));
		Check.That("image markup removed", !speech.Contains("!["));
	}
}
