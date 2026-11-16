using SPIC.Core.DTOs;
using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// Final-review fixes for the help centre: a hotspot can say "no marker", import reports block
/// text it would otherwise drop, review corrections keep the English layout of blocks, and
/// apply-reviews keeps a translation's category and note (and their staleness).
/// </summary>
internal static class HelpReviewFixTests
{
	public static void Run()
	{
		Console.WriteLine("Help final-review fixes");

		HotspotCanBeEmpty();
		ImportReportsDroppedBlockLines();
		ImportReportsMissingItemField();
		ApplierKeepsBlockLayout();
		ApplierKeepsCategoryAndNote();
		WriterWithoutMetaKeysLeavesMetaStale();
		ReviewMarkdownHidesLayout();
	}

	private static void ReviewMarkdownHidesLayout()
	{
		var flow = En().Sections.First(s => s.Id == "flow");
		var md = HelpBlocks.ReviewMarkdown(flow);
		Check.That("review text shows the step title", md.Contains("Raise and submit"), md);
		Check.That("review text hides layout lines", !md.Contains("lane:") && !md.Contains("|"), md);
	}

	private static void HotspotCanBeEmpty()
	{
		var g = HelpGuideParser.Parse("m", "en", """
			---
			module: m
			title: M
			---

			::: section one
			title: One
			routes: /X
			hotspots: 1 @ 2,4,96,18 | 2 @ -

			1. A
			2. B
			:::
			""");
		Check.Equal("'N @ -' is accepted", "", string.Join(" | ", g.Problems));
		Check.Equal("two hotspots kept", 2, g.Sections[0].Screen!.Hotspots.Count);
		Check.That("second hotspot has no marker", !g.Sections[0].Screen!.Hotspots[1].HasMarker);
		Check.That("first hotspot has a marker", g.Sections[0].Screen!.Hotspots[0].HasMarker);
	}

	private static HelpGuide En() => HelpGuideParser.Parse("publicity", "en", HelpV2TranslationTests.English);

	private static HelpGuide Draft(string flowItems) => HelpGuideParser.Parse("publicity", "ta", $"""
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
		  a: **Clear All** அழுத்தவும்.
		:::

		::: section requests-list
		title: என் கோரிக்கைகள்

		1. அட்டையைத் தட்டவும்.
		2. தேடவும்.
		:::
		""");

	private static void ImportReportsDroppedBlockLines()
	{
		var items = "- rmdo | type: lane\n  title: RMDO\n- raise | lane: rmdo\n  title: உருவாக்கு\n  text: **Submit Request**\nஅழுத்தவும்.\n- approve | lane: rmdo\n  title: சரிபார்";
		var r = HelpTranslationImport.Build(En(), Draft(items), "ta");
		Check.That("unindented continuation line reported", r.Problems.Any(p => p.Contains("flow") && p.Contains("அழுத்தவும்")), string.Join(" | ", r.Problems));
	}

	private static void ImportReportsMissingItemField()
	{
		var items = "- rmdo | type: lane\n  title: RMDO\n- raise | lane: rmdo\n  தலைப்பு: உருவாக்கு\n  text: **Submit Request** அழுத்தவும்.\n- approve | lane: rmdo\n  title: சரிபார்";
		var r = HelpTranslationImport.Build(En(), Draft(items), "ta");
		Check.That("translated field key reported as missing title", r.Problems.Any(p => p.Contains("raise") && p.Contains("title")), string.Join(" | ", r.Problems));
	}

	private static void ApplierKeepsBlockLayout()
	{
		var en = En();
		var flow = en.Sections.First(s => s.Id == "flow");
		var ta = HelpGuideWriter.Write(en, "ta", en.Sections.ToDictionary(s => s.Id, s => (s.Title, s.Body, HelpConfidence.Ok)));
		var correction = new HelpCorrectionExportDto("publicity", "flow", "ta", HelpHash.Of(flow.Title, flow.Body), "ஓட்டம்",
			"- rmdo | type: lane | color: red\n  title: RMDO\n- raise | lane: elsewhere | col: 9\n  title: உருவாக்கு\n- approve | lane: rmdo\n  title: சரிபார்",
			"Reviewer", DateTime.UtcNow);
		var (text, skipped) = HelpReviewApplier.Apply(en, HelpGuideParser.Parse("publicity", "ta", ta), new[] { correction });
		Check.Equal("block correction applied", 0, skipped.Count);
		var written = HelpGuideParser.Parse("publicity", "ta", text).Sections.First(s => s.Id == "flow");
		var items = HelpBlocks.Items(written.Body);
		Check.Equal("correction stored with English layout", "- raise | lane: rmdo | col: 1 | color: #64748b | next: approve", items[1].Header);
		Check.Equal("correction text kept", "உருவாக்கு", items[1].Text["title"]);
	}

	private static void ApplierKeepsCategoryAndNote()
	{
		var en = En();
		var dict = en.Sections.ToDictionary(s => s.Id, s => (s.Title, s.Body, HelpConfidence.Ok));
		dict[HelpGuideWriter.CategoryKey] = ("சந்தைப்படுத்தல்", "", HelpConfidence.Ok);
		dict[HelpGuideWriter.NoteKey] = ("மாற்றம்", "", HelpConfidence.Ok);
		var ta = HelpGuideParser.Parse("publicity", "ta", HelpGuideWriter.Write(en, "ta", dict));
		var (text, _) = HelpReviewApplier.Apply(en, ta, Array.Empty<HelpCorrectionExportDto>());
		var after = HelpGuideParser.Parse("publicity", "ta", text);
		Check.Equal("category survives apply-reviews", "சந்தைப்படுத்தல்", after.Category);
		Check.Equal("note survives apply-reviews", "மாற்றம்", after.Note);
		Check.Equal("meta hash kept", ta.MetaSourceHash, after.MetaSourceHash);
	}

	private static void WriterWithoutMetaKeysLeavesMetaStale()
	{
		var en = En();
		var text = HelpGuideWriter.Write(en, "ta", en.Sections.ToDictionary(s => s.Id, s => (s.Title, s.Body, HelpConfidence.Ok)));
		Check.Equal("no translated category/note: no meta hash", null, HelpGuideParser.Parse("publicity", "ta", text).MetaSourceHash);
	}
}
