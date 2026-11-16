using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// Translations of v2 guides: blocks keep the English layout under translated text, screen meta and
/// kinds always come from English, old v1 translation files fall back to English instead of
/// breaking, and the import checks block items and on-screen names inside block text.
/// </summary>
internal static class HelpV2TranslationTests
{
	public static void Run()
	{
		Console.WriteLine("Help v2 translations");

		LocalizedMergesBlockLayout();
		V1TranslationAgainstV2EnglishFallsBack();
		WriterKeepsKindAndScreenMeta();
		CategoryAndNoteFollowTheirHash();
		ImportReportsMissingAndExtraBlockItems();
		ImportChecksBoldInBlockText();
	}

	internal const string English = """
		---
		module: publicity
		title: Publicity
		category: Marketing
		note: Transfer added
		color: #0b6b3a
		---

		::: section flow
		title: How a request moves
		kind: flow

		Each row is a role.

		- rmdo | type: lane | color: #0b6b3a | designations: RMDO
		  title: RMDO
		- raise | lane: rmdo | col: 1 | color: #64748b | next: approve
		  title: Raise and submit
		  text: Press **Submit Request**.
		- approve | lane: rmdo | col: 2 | color: #d97706
		  title: Check and approve
		:::

		::: section faq
		title: Problems and answers
		kind: faq

		- no-rows | popular: true
		  q: I see no rows
		  a: Press **Clear All**.
		:::

		::: section requests-list
		title: My requests
		routes: /PublicityRequest
		screenshot: list.png
		hotspots: 1 @ 2,4,96,18 | 2 @ 2,26,96,9.5
		tour: 1 > [data-help=pub-status-cards] | 2 > [data-help=pub-search]
		minutes: 2

		1. Tap a card.
		2. Search.
		:::
		""";

	private static HelpGuide En() => HelpGuideParser.Parse("publicity", "en", English);

	private static string Hash(string id)
	{
		var s = En().Sections.First(x => x.Id == id);
		return HelpHash.Of(s.Title, s.Body);
	}

	private static HelpLibrary Library(string tamil) =>
		new(new[] { ("publicity/guide.en.md", English), ("publicity/guide.ta.md", tamil) });

	private static void LocalizedMergesBlockLayout()
	{
		var ta = $"""
			---
			module: publicity
			title: விளம்பரம்
			---

			::: section flow
			title: கோரிக்கை எப்படி நகரும்
			kind: flow
			source-hash: {Hash("flow")}

			- rmdo | type: lane | color: red
			  title: RMDO
			- raise | lane: elsewhere | col: 9
			  title: உருவாக்கு
			  text: **Submit Request** அழுத்தவும்.
			- approve | lane: rmdo
			  title: சரிபார்
			:::
			""";
		var flow = Library(ta).Localized("publicity", "ta")!.Sections.First(s => s.Id == "flow");
		var items = HelpBlocks.Items(flow.Body);
		Check.Equal("flow keeps its kind", "flow", flow.Kind);
		Check.Equal("translated title used", "கோரிக்கை எப்படி நகரும்", flow.Title);
		Check.Equal("English layout under Tamil text", "- raise | lane: rmdo | col: 1 | color: #64748b | next: approve", items[1].Header);
		Check.Equal("Tamil item text", "உருவாக்கு", items[1].Text["title"]);
		Check.Equal("English lead when the translation has none", "Each row is a role.", HelpBlocks.Lead(flow.Body));
		var screen = Library(ta).Localized("publicity", "ta")!.Sections.First(s => s.Id == "requests-list");
		Check.Equal("screen meta from English", 2, screen.Screen!.Hotspots.Count);
	}

	private static void V1TranslationAgainstV2EnglishFallsBack()
	{
		var ta = """
			---
			module: publicity
			title: விளம்பரம்
			---

			::: section raise-request
			title: பழைய பகுதி
			routes: /PublicityRequest
			source-hash: 0000000000000000
			confidence: ok

			பழைய உரை.
			:::
			""";
		var lib = Library(ta);
		Check.Equal("old Tamil file parses cleanly", 0, lib.Raw("publicity", "ta")!.Problems.Count);
		var localized = lib.Localized("publicity", "ta")!;
		Check.That("every section falls back to English",
			localized.Sections.Zip(En().Sections).All(p => p.First.Body == p.Second.Body && p.First.Title == p.Second.Title));
	}

	private static void WriterKeepsKindAndScreenMeta()
	{
		var en = En();
		var dict = en.Sections.ToDictionary(s => s.Id, s => (s.Title + " (ta)", s.Body, HelpConfidence.Ok));
		var text = HelpGuideWriter.Write(en, "ta", dict);
		var back = HelpGuideParser.Parse("publicity", "ta", text);
		Check.Equal("written file parses cleanly", "", string.Join(" | ", back.Problems));
		Check.Equal("flow kind written", "flow", back.Sections.First(s => s.Id == "flow").Kind);
		Check.Equal("faq kind written", "faq", back.Sections.First(s => s.Id == "faq").Kind);
		var screen = back.Sections.First(s => s.Id == "requests-list").Screen!;
		Check.That("hotspots written from English", en.Sections[2].Screen!.Hotspots.SequenceEqual(screen.Hotspots));
		Check.Equal("tour written from English", "[data-help=pub-search]", screen.Tour[1].Target);
		Check.Equal("image written from English", "list.png", screen.Image);
		Check.Equal("minutes written", 2, screen.Minutes);
	}

	private static void CategoryAndNoteFollowTheirHash()
	{
		var en = En();
		var dict = new Dictionary<string, (string, string, HelpConfidence)>
		{
			[HelpGuideWriter.CategoryKey] = ("சந்தைப்படுத்தல்", "", HelpConfidence.Ok),
			[HelpGuideWriter.NoteKey] = ("மாற்றம் சேர்க்கப்பட்டது", "", HelpConfidence.Ok),
		};
		var ta = HelpGuideWriter.Write(en, "ta", dict);
		var localized = Library(ta).Localized("publicity", "ta")!;
		Check.Equal("translated category", "சந்தைப்படுத்தல்", localized.Category);
		Check.Equal("translated note", "மாற்றம் சேர்க்கப்பட்டது", localized.Note);

		var changedEnglish = English.Replace("note: Transfer added", "note: Something new");
		var lib2 = new HelpLibrary(new[] { ("publicity/guide.en.md", changedEnglish), ("publicity/guide.ta.md", ta) });
		Check.Equal("stale note falls back to English", "Something new", lib2.Localized("publicity", "ta")!.Note);
		Check.Equal("stale category falls back too", "Marketing", lib2.Localized("publicity", "ta")!.Category);
	}

	private static HelpGuide Draft(string flowItems, string faqAnswer = "**Clear All** அழுத்தவும்.") => HelpGuideParser.Parse("publicity", "ta", $"""
		---
		module: publicity
		title: விளம்பரம்
		---

		::: section flow
		title: ஓட்டம்
		kind: flow

		{flowItems}
		:::

		::: section faq
		title: கேள்விகள்
		kind: faq

		- no-rows | popular: true
		  q: வரிசைகள் இல்லை
		  a: {faqAnswer}
		:::

		::: section requests-list
		title: என் கோரிக்கைகள்

		1. அட்டையைத் தட்டவும்.
		2. தேடவும்.
		:::
		""");

	private const string AllItems = """
		- rmdo | type: lane
		  title: RMDO
		- raise | lane: rmdo
		  title: உருவாக்கு
		  text: **Submit Request** அழுத்தவும்.
		- approve | lane: rmdo
		  title: சரிபார்
		""";

	private static void ImportReportsMissingAndExtraBlockItems()
	{
		var good = HelpTranslationImport.Build(En(), Draft(AllItems), "ta");
		Check.Equal("complete draft imports", "", string.Join(" | ", good.Problems));
		var written = HelpGuideParser.Parse("publicity", "ta", good.Text).Sections.First(s => s.Id == "flow");
		Check.Equal("import stores English layout", "- raise | lane: rmdo | col: 1 | color: #64748b | next: approve", HelpBlocks.Items(written.Body)[1].Header);

		var missing = HelpTranslationImport.Build(En(), Draft(AllItems.Replace("\r\n", "\n").Replace("- approve | lane: rmdo\n  title: சரிபார்", "")), "ta");
		Check.That("missing item reported", missing.Problems.Any(p => p.Contains("flow") && p.Contains("approve") && p.Contains("missing")), string.Join(" | ", missing.Problems));

		var extra = HelpTranslationImport.Build(En(), Draft(AllItems + "\n- bogus | lane: rmdo\n  title: தவறு"), "ta");
		Check.That("extra item reported", extra.Problems.Any(p => p.Contains("bogus")), string.Join(" | ", extra.Problems));
	}

	private static void ImportChecksBoldInBlockText()
	{
		var result = HelpTranslationImport.Build(En(), Draft(AllItems, "**எல்லாம் அழி** அழுத்தவும்."), "ta");
		Check.Equal("one problem for the translated button name", 1, result.Problems.Count);
		Check.That("names Clear All", result.Problems.Count == 1 && result.Problems[0].Contains("Clear All"));
	}
}
