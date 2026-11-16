using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// Guide blocks (overview, flow, statuses, roles, faq): the item grammar, the typed views the
/// pages draw, validation, the merge that keeps English layout under translated text, and the text
/// spoken by Listen.
/// </summary>
internal static class HelpBlocksTests
{
	public static void Run()
	{
		Console.WriteLine("Help guide blocks");

		ParsesItems();
		MergeLayoutKeepsEnglishLayout();
		MergeLayoutFallsBackPerField();
		FlowViewAndValidation();
		SpeechTextOfFlow();
		TextLinesOfFaq();
		OtherViews();
	}

	private const string RolesBody = """
		Your card is highlighted.

		- rmdo | color: #0b6b3a | designations: RMDO
		  title: RMDO
		  - Raise requests
		  - Acknowledge delivery
		  - Send courier proof
		- approver | color: #0d9488 | designations: RM, SMD, SMM
		  title: RM, SMD, SMM
		  - Check requests
		""";

	private static void ParsesItems()
	{
		var problems = new List<string>();
		var items = HelpBlocks.Items(RolesBody, problems);
		Check.Equal("no item problems", "", string.Join(" | ", problems));
		Check.Equal("two items", "rmdo,approver", string.Join(",", items.Select(i => i.Id)));
		Check.Equal("layout colour", "#0b6b3a", items[0].Layout["color"]);
		Check.Equal("text title", "RMDO", items[0].Text["title"]);
		Check.Equal("three bullets", 3, items[0].Bullets.Count);
		Check.Equal("lead before items", "Your card is highlighted.", HelpBlocks.Lead(RolesBody));

		var bad = new List<string>();
		HelpBlocks.Items("  title: orphan\n- | color: red", bad);
		Check.Equal("orphan line and empty id reported", 2, bad.Count);
	}

	private const string FlowEn = """
		Each row is a role.

		- rmdo | type: lane | color: #0b6b3a | designations: RMDO
		  title: RMDO
		  sub: Your HQ
		- approver | type: lane | color: #0d9488 | designations: RM, SMD, SMM
		  title: Approvers
		  sub: RM · SMD · SMM
		- raise | lane: rmdo | col: 1 | color: #64748b | icon: bi-pencil-square | next: approve
		  title: Raise and submit
		  status: Draft
		  text: Fill the form and press **Submit Request**.
		- approve | lane: approver | col: 2 | color: #d97706 | icon: bi-hourglass-split | return: raise | screen: requests-list
		  title: Check and approve
		  status: Pending approval
		  text: Approve, return or reject.
		""";

	private static void MergeLayoutKeepsEnglishLayout()
	{
		var ta = """
			ஒவ்வொரு வரியும் ஒரு பங்கு.

			- rmdo | type: lane | color: red
			  title: RMDO
			  sub: உங்கள் HQ
			- approver | type: lane
			  title: அனுமதிப்பவர்கள்
			  sub: RM · SMD · SMM
			- raise | lane: vendor | col: 9 | color: red
			  title: உருவாக்கு
			  status: Draft
			  text: படிவத்தை நிரப்பி **Submit Request** அழுத்தவும்.
			- approve | lane: approver
			  title: சரிபார்த்து அனுமதி
			  status: Pending approval
			  text: அனுமதி, திருப்பி அனுப்பு அல்லது நிராகரி.
			""";
		var merged = HelpBlocks.MergeLayout(FlowEn, ta);
		var items = HelpBlocks.Items(merged);
		Check.Equal("English header line kept", "- raise | lane: rmdo | col: 1 | color: #64748b | icon: bi-pencil-square | next: approve", items[2].Header);
		Check.Equal("Tamil title kept", "உருவாக்கு", items[2].Text["title"]);
		Check.Equal("Tamil lead kept", "ஒவ்வொரு வரியும் ஒரு பங்கு.", HelpBlocks.Lead(merged));
		Check.Equal("lane colour from English", "#0b6b3a", items[0].Layout["color"]);
	}

	private static void MergeLayoutFallsBackPerField()
	{
		var ta = """
			- rmdo | type: lane
			  title: RMDO
			- raise | lane: rmdo
			  title: உருவாக்கு
			- bogus | lane: rmdo
			  title: தவறு
			""";
		var items = HelpBlocks.Items(HelpBlocks.MergeLayout(FlowEn, ta));
		Check.Equal("items in English order, extra dropped", "rmdo,approver,raise,approve", string.Join(",", items.Select(i => i.Id)));
		Check.Equal("missing field falls back to English", "Fill the form and press **Submit Request**.", items[2].Text["text"]);
		Check.Equal("missing item falls back to English", "Check and approve", items[3].Text["title"]);
		Check.Equal("missing lead falls back to English", "Each row is a role.", HelpBlocks.Lead(HelpBlocks.MergeLayout(FlowEn, ta)));
	}

	private static HelpGuide Guide(string flowBody) => HelpGuideParser.Parse("publicity", "en", $"""
		---
		module: publicity
		title: Publicity
		---

		::: section flow
		title: How a request moves
		kind: flow

		{flowBody}
		:::

		::: section requests-list
		title: My requests
		routes: /PublicityRequest

		1. A
		:::
		""");


	private static void FlowViewAndValidation()
	{
		var guide = Guide(FlowEn);
		var flowSection = guide.Sections[0];
		Check.Equal("flow kind parsed", "flow", flowSection.Kind);

		var problems = new List<string>();
		var flow = HelpBlocks.Flow(flowSection, problems);
		Check.Equal("two lanes", "rmdo,approver", string.Join(",", flow.Lanes.Select(l => l.Id)));
		Check.Equal("lane designations", "RM,SMD,SMM", string.Join(",", flow.Lanes[1].Designations));
		Check.Equal("two stages", "raise,approve", string.Join(",", flow.Stages.Select(s => s.Id)));
		Check.Equal("stage next", "approve", flow.Stages[0].Next.Single());
		Check.Equal("stage return", "raise", flow.Stages[1].Return);
		Check.Equal("stage column", 2, flow.Stages[1].Col);
		Check.Equal("stage status", "Pending approval", flow.Stages[1].Status);
		Check.Equal("stage screen", "requests-list", flow.Stages[1].Screen);
		Check.Equal("valid flow has no problems", "", string.Join(" | ", HelpBlocks.Validate(guide)));

		var broken = Guide(FlowEn.Replace("next: approve", "next: nowhere"));
		var issues = HelpBlocks.Validate(broken);
		Check.Equal("one problem for an unknown next", 1, issues.Count);
		Check.That("problem names the missing stage", issues.Count == 1 && issues[0].Contains("nowhere"));

		var badScreen = HelpBlocks.Validate(Guide(FlowEn.Replace("screen: requests-list", "screen: ghost")));
		Check.That("unknown screen reported", badScreen.Count == 1 && badScreen[0].Contains("ghost"));
		var badLane = HelpBlocks.Validate(Guide(FlowEn.Replace("lane: approver | col: 2", "lane: nobody | col: 2")));
		Check.That("unknown lane reported", badLane.Count == 1 && badLane[0].Contains("nobody"));
		var badCol = HelpBlocks.Validate(Guide(FlowEn.Replace("col: 2", "col: x")));
		Check.That("bad column reported", badCol.Count == 1 && badCol[0].Contains("col"));
	}

	private static void SpeechTextOfFlow()
	{
		var speech = HelpBlocks.SpeechText(Guide(FlowEn).Sections[0]);
		var a = speech.IndexOf("Raise and submit.", StringComparison.Ordinal);
		var b = speech.IndexOf("Check and approve.", StringComparison.Ordinal);
		Check.That("stages spoken in order", a >= 0 && b > a, speech);
		Check.That("no layout spoken", !speech.Contains('|') && !speech.Contains("color") && !speech.Contains("#"), speech);
		Check.That("bold markup stripped", !speech.Contains("**"), speech);
		Check.That("lanes not spoken", !speech.Contains("Your HQ"), speech);

		var screen = Guide(FlowEn).Sections[1];
		Check.Equal("screen speech is the markdown speech", HelpMarkdown.ToSpeech(screen.Title, screen.Body), HelpBlocks.SpeechText(screen));
	}

	private const string FaqBody = """
		- no-pencil | popular: true
		  q: I submitted, and now there is no pencil icon
		  a: After you submit, only an approver can return it. Press **Clear All** to reset filters.
		- no-rows
		  q: I see "No requests found"
		  a: Your filters may be too narrow.
		""";

	private static void TextLinesOfFaq()
	{
		var section = new HelpSection("faq", "Problems", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), FaqBody, null, HelpConfidence.Source) { Kind = "faq" };
		var lines = HelpBlocks.TextLines(section);
		Check.That("question included", lines.Contains("I submitted, and now there is no pencil icon"));
		Check.That("answer included", lines.Any(l => l.StartsWith("After you submit")));
		Check.That("no header lines", !lines.Any(l => l.Contains("popular")));

		var faq = HelpBlocks.Faq(section, new List<string>());
		Check.Equal("two FAQ items", 2, faq.Count);
		Check.That("first is popular", faq[0].Popular && !faq[1].Popular);
		Check.Equal("answer text", "Your filters may be too narrow.", faq[1].Answer);
	}

	private static void OtherViews()
	{
		var roles = new HelpSection("roles", "Who does what", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), RolesBody, null, HelpConfidence.Source) { Kind = "roles" };
		var cards = HelpBlocks.Roles(roles, new List<string>());
		Check.Equal("role cards", 2, cards.Count);
		Check.Equal("role tasks", 3, cards[0].Tasks.Count);
		Check.Equal("role designations", "RMDO", cards[0].Designations.Single());

		var statuses = new HelpSection("statuses", "Statuses", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
			"- draft | color: #64748b\n  label: Draft\n  text: Saved, not sent.", null, HelpConfidence.Source) { Kind = "statuses" };
		var tags = HelpBlocks.Statuses(statuses, new List<string>());
		Check.Equal("status label", "Draft", tags.Single().Label);
		Check.Equal("status colour", "#64748b", tags.Single().Color);

		var overview = new HelpSection("overview", "What this module does", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
			"Lead text.\n\n- start | icon: bi-pencil-square | color: #0b6b3a\n  title: You start it\n  text: Raise a request.", null, HelpConfidence.Source) { Kind = "overview" };
		var ov = HelpBlocks.Overview(overview, new List<string>());
		Check.Equal("overview card", "You start it", ov.Single().Title);
		Check.Equal("overview icon", "bi-pencil-square", ov.Single().Icon);
	}
}
