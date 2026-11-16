using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// The translate tool's file handling: it re-translates only sections whose English changed (or
/// that were never translated), and the files it writes parse back with the English routes and
/// filters - a translation can never change who sees a section.
/// </summary>
internal static class HelpTranslationTests
{
	public static void Run()
	{
		Console.WriteLine("Help translation files");

		PlannerPicksMissingAndStaleOnly();
		WriterRoundTripsThroughParser();
		WriterNeverCopiesTranslatedRoutes();
	}

	private static HelpGuide English() => HelpGuideParser.Parse("publicity", "en", HelpLibraryTests.EnglishThree);

	private static void PlannerPicksMissingAndStaleOnly()
	{
		var en = English();
		var one = en.Sections[0];
		var tamil = HelpGuideParser.Parse("publicity", "ta", $"""
			::: section one
			title: ஒன்று
			source-hash: {HelpHash.Of(one.Title, one.Body)}

			முதல்.
			:::

			::: section two
			title: இரண்டு
			source-hash: 0000000000000000

			பழையது.
			:::
			""");
		Check.Equal("stale and missing sections planned", "two,three", string.Join(",", HelpTranslationPlanner.SectionsToTranslate(en, tamil)));
		Check.Equal("no file means every section", "one,two,three", string.Join(",", HelpTranslationPlanner.SectionsToTranslate(en, null)));
	}

	private static void WriterRoundTripsThroughParser()
	{
		var en = English();
		var text = HelpGuideWriter.Write(en, "ta", new Dictionary<string, (string Title, string Body, HelpConfidence Confidence)>
		{
			["_title"] = ("விளம்பரம்", string.Empty, HelpConfidence.Ok),
			["one"] = ("ஒன்று", "முதல் **பகுதி**.\n\n1. படி.", HelpConfidence.Ok),
			["three"] = ("மூன்று", "மூன்றாவது.", HelpConfidence.Low),
		});
		var back = HelpGuideParser.Parse("publicity", "ta", text);
		Check.Equal("written file parses cleanly", 0, back.Problems.Count);
		Check.Equal("only translated sections written, in English order", "one,three", string.Join(",", back.Sections.Select(s => s.Id)));
		Check.Equal("guide title translated", "விளம்பரம்", back.Title);
		Check.Equal("source hash is the English hash", HelpHash.Of(en.Sections[0].Title, en.Sections[0].Body), back.Sections[0].SourceHash);
		Check.Equal("confidence written", HelpConfidence.Low, back.Sections[1].Confidence);
		Check.Equal("body kept", "முதல் **பகுதி**.\n\n1. படி.", back.Sections[0].Body);

		var lib = HelpLibraryTests.Library(("help/publicity/guide.en.md", HelpLibraryTests.EnglishThree), ("help/publicity/guide.ta.md", text));
		Check.Equal("library uses the written translation", "ஒன்று", lib.Localized("publicity", "ta")!.Sections[0].Title);
	}

	private static void WriterNeverCopiesTranslatedRoutes()
	{
		var en = English();
		var text = HelpGuideWriter.Write(en, "ta", new Dictionary<string, (string Title, string Body, HelpConfidence Confidence)>
		{
			["two"] = ("இரண்டு", "routes: /Evil\n\nஉடல்.", HelpConfidence.Ok),
		});
		var back = HelpGuideParser.Parse("publicity", "ta", text);
		Check.Equal("routes header comes from English", "/PublicityReview", string.Join(",", back.Sections.Single().Routes));
	}
}
