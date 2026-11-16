using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// The guide library: which modules exist, the language list, and how a translated guide is put
/// together - a translated section is used only while it still matches the English it came from,
/// and a translation can never change which routes (and so which users) a section applies to.
/// </summary>
internal static class HelpLibraryTests
{
	internal const string Config = """
		{ "defaultLanguage": "en",
		  "languages": [
		    { "code": "en", "name": "English", "native": "English", "voice": "en-IN-NeerjaNeural" },
		    { "code": "ta", "name": "Tamil", "native": "தமிழ்", "voice": "ta-IN-PallaviNeural" },
		    { "code": "te", "name": "Telugu", "native": "తెలుగు", "voice": "te-IN-ShrutiNeural" },
		    { "code": "mr", "name": "Marathi", "native": "मराठी", "voice": "mr-IN-AarohiNeural" },
		    { "code": "hi", "name": "Hindi", "native": "हिन्दी", "voice": "hi-IN-SwaraNeural" } ] }
		""";

	internal const string Glossary = """
		{ "ta": { "RMDO": { "keep": true }, "Consignment": { "write": "கன்சைன்மென்ட்", "say": "கன்சைன்மென்ட்" } } }
		""";

	internal const string EnglishThree = """
		---
		module: publicity
		title: Publicity
		order: 40
		---

		::: section one
		title: One
		routes: /PublicityRequest

		First body.
		:::

		::: section two
		title: Two
		routes: /PublicityReview

		Second body.
		:::

		::: section three
		title: Three
		routes: /PublicityReview/{id}

		Third body.
		:::
		""";

	public static void Run()
	{
		Console.WriteLine("Help library");

		LoadsModulesAndConfig();
		MissingOrMalformedTranslatedSectionFallsBackToEnglish();
		TranslatedFileCannotChangeRoutes();
		GlossaryIsReadPerLanguage();
		EmbeddedContentLoads();
	}

	internal static HelpLibrary Library(params (string Path, string Text)[] guides)
	{
		var files = new List<(string, string)> { ("help/help.config.json", Config), ("help/glossary.json", Glossary) };
		files.AddRange(guides);
		return new HelpLibrary(files);
	}

	private static void LoadsModulesAndConfig()
	{
		var lib = Library(("help/publicity/guide.en.md", EnglishThree));
		Check.That("publicity module found", lib.Modules.Contains("publicity"));
		Check.Equal("five launch languages", 5, lib.Config.Languages.Count);
		Check.Equal("default language", "en", lib.Config.DefaultLanguage);
		Check.Equal("Hindi voice", "hi-IN-SwaraNeural", lib.Config.Languages.Single(l => l.Code == "hi").Voice);
	}

	private static void MissingOrMalformedTranslatedSectionFallsBackToEnglish()
	{
		var en = HelpGuideParser.Parse("publicity", "en", EnglishThree);
		var oneHash = HelpHash.Of(en.Sections[0].Title, en.Sections[0].Body);
		var tamil = $"""
			---
			title: விளம்பரம்
			---

			::: section one
			title: ஒன்று
			source-hash: {oneHash}
			confidence: ok

			முதல்.
			:::

			::: section three
			title: மூன்று
			source-hash: 0000000000000000
			confidence: ok

			பழையது.
			:::
			""";
		var lib = Library(("help/publicity/guide.en.md", EnglishThree), ("help/publicity/guide.ta.md", tamil));
		var ta = lib.Localized("publicity", "ta")!;
		Check.Equal("all English sections present, in order", "one,two,three", string.Join(",", ta.Sections.Select(s => s.Id)));
		Check.Equal("current translation used", "ஒன்று", ta.Sections[0].Title);
		Check.Equal("missing translation falls back to English", HelpConfidence.Source, ta.Sections[1].Confidence);
		Check.Equal("stale translation falls back to English", "Three", ta.Sections[2].Title);
		Check.Equal("translated guide title used", "விளம்பரம்", ta.Title);

		var noFile = lib.Localized("publicity", "te")!;
		Check.Equal("no Telugu file at all gives English", "One", noFile.Sections[0].Title);
		Check.That("unknown module gives null", lib.Localized("nope", "ta") is null);
	}

	private static void TranslatedFileCannotChangeRoutes()
	{
		var en = HelpGuideParser.Parse("publicity", "en", EnglishThree);
		var hash = HelpHash.Of(en.Sections[0].Title, en.Sections[0].Body);
		var tamil = $"""
			::: section one
			title: ஒன்று
			routes: /Evil
			for: Evil.Entry
			source-hash: {hash}

			முதல்.
			:::
			""";
		var lib = Library(("help/publicity/guide.en.md", EnglishThree), ("help/publicity/guide.ta.md", tamil));
		var section = lib.Localized("publicity", "ta")!.Sections[0];
		Check.Equal("routes always come from English", "/PublicityRequest", string.Join(",", section.Routes));
		Check.Equal("for tokens always come from English", 0, section.For.Count);
		Check.Equal("source hash kept for review status", hash, section.SourceHash);
	}

	private static void GlossaryIsReadPerLanguage()
	{
		var lib = Library(("help/publicity/guide.en.md", EnglishThree));
		var ta = lib.GlossaryFor("ta");
		Check.That("keep term read", ta.Any(t => t.Term == "RMDO" && t.Keep));
		Check.That("say-as read", ta.Any(t => t.Term == "Consignment" && t.Say == "கன்சைன்மென்ட்"));
		Check.Equal("language with no glossary is empty", 0, lib.GlossaryFor("mr").Count);
	}

	private static void EmbeddedContentLoads()
	{
		var lib = HelpLibrary.FromEmbedded();
		Check.Equal("embedded config lists five languages", 5, lib.Config.Languages.Count);
		foreach (var module in new[] { "publicity", "sas-sample-collection", "lab" })
			Check.That($"embedded pilot guide '{module}' loads", lib.Modules.Contains(module), string.Join(",", lib.Modules));
	}
}
