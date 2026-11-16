using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// help-tools import: a translation written outside the translate command (by hand or by an
/// agency) is checked and written in the same stamped format. Every English section must be there,
/// and on-screen names in **bold** must stay English; bold sentences and lead-in lines translate.
/// </summary>
internal static class HelpImportTests
{
	public static void Run()
	{
		Console.WriteLine("Help translation import");

		ImportWritesStampedFile();
		ImportReportsMissingAndExtraSections();
		ImportReportsTranslatedScreenName();
		ImportAllowsTranslatedLeadInAndSentenceBold();
	}

	private const string English = """
		---
		module: publicity
		title: Publicity
		order: 40
		---

		::: section one
		title: One
		routes: /PublicityRequest
		for: PublicityRequest.View

		**What this screen is for**
		Press **Submit Request**.

		- **I see no rows here.** Press **Clear All**.
		:::

		::: section two
		title: Two
		routes: /PublicityReview

		Second body.
		:::
		""";

	private static HelpGuide En() => HelpGuideParser.Parse("publicity", "en", English);

	private static HelpGuide Draft(string text) => HelpGuideParser.Parse("publicity", "ta", text);

	private const string GoodDraft = """
		---
		module: publicity
		title: விளம்பரம்
		order: 40
		---

		::: section one
		title: ஒன்று
		routes: /SomethingElse

		**இந்தத் திரை எதற்கு**
		**Submit Request** அழுத்தவும்.

		- **இங்கே வரிசைகள் இல்லை.** **Clear All** அழுத்தவும்.
		:::

		::: section two
		title: இரண்டு
		routes: /PublicityReview

		இரண்டாவது.
		:::
		""";

	private static void ImportWritesStampedFile()
	{
		var en = En();
		var result = HelpTranslationImport.Build(en, Draft(GoodDraft), "ta");
		Check.Equal("good draft has no problems", "", string.Join(" | ", result.Problems));

		var back = HelpGuideParser.Parse("publicity", "ta", result.Text);
		Check.Equal("imported file parses cleanly", 0, back.Problems.Count);
		Check.Equal("guide title from draft", "விளம்பரம்", back.Title);
		Check.Equal("sections in English order", "one,two", string.Join(",", back.Sections.Select(s => s.Id)));
		Check.Equal("source hash stamped from English", HelpHash.Of(en.Sections[0].Title, en.Sections[0].Body), back.Sections[0].SourceHash);
		Check.Equal("routes come from English, not the draft", "/PublicityRequest", string.Join(",", back.Sections[0].Routes));
		Check.Equal("for comes from English", "PublicityRequest.View", string.Join(",", back.Sections[0].For));
		Check.Equal("imported sections are labelled ok", HelpConfidence.Ok, back.Sections[1].Confidence);
	}

	private static void ImportReportsMissingAndExtraSections()
	{
		var draft = Draft("""
			---
			module: publicity
			title: விளம்பரம்
			---

			::: section one
			title: ஒன்று

			**இந்தத் திரை எதற்கு**
			**Submit Request** அழுத்தவும்.

			- **இங்கே வரிசைகள் இல்லை.** **Clear All** அழுத்தவும்.
			:::

			::: section bogus
			title: தவறு

			உரை.
			:::
			""");
		var result = HelpTranslationImport.Build(En(), draft, "ta");
		Check.That("missing section reported", result.Problems.Any(p => p.Contains("two") && p.Contains("missing")));
		Check.That("extra section reported", result.Problems.Any(p => p.Contains("bogus")));
	}

	private static void ImportReportsTranslatedScreenName()
	{
		var draft = Draft(GoodDraft.Replace("**Submit Request** அழுத்தவும்", "**சமர்ப்பி** அழுத்தவும்"));
		var result = HelpTranslationImport.Build(En(), draft, "ta");
		Check.That("translated button name reported", result.Problems.Any(p => p.Contains("one") && p.Contains("**Submit Request**")));
		Check.Equal("only that one problem", 1, result.Problems.Count);
	}

	private static void ImportAllowsTranslatedLeadInAndSentenceBold()
	{
		var names = HelpTranslationImport.ScreenNames(En().Sections[0].Body);
		Check.Equal("screen names are the inline labels only", "Clear All,Submit Request", string.Join(",", names.OrderBy(n => n, StringComparer.Ordinal)));
		Check.Equal("short label ending in a full stop is a screen name", "Batch No.", string.Join(",", HelpTranslationImport.ScreenNames("Type the **Batch No.** here.")));
	}
}
