using SPIC.Core.Help;

namespace SPIC.RegressionHarness;

/// <summary>
/// v2 guide markup: the fields table and the Tip / Careful callouts, as HTML for the page and as
/// text for the voice; tour steps are still only the numbered list.
/// </summary>
internal static class HelpMarkdownTests
{
	public static void Run()
	{
		Console.WriteLine("Help markdown v2");

		TableToHtml();
		CalloutsToHtml();
		TableAndCalloutToSpeech();
		StepTextForTour();
	}

	private const string Body = """
		1. Tap a card.
		2. Search.

		> Tip: Use **Export** for Excel.

		> Careful: Deleting a Draft cannot be undone.

		| Field | What to enter |
		|---|---|
		| Date Range | Two dates. |
		| Status | One or more statuses. |
		""";

	private static void TableToHtml()
	{
		var html = HelpMarkdown.ToHtml(Body, "/help/x/images");
		Check.That("table rendered with class", html.Contains("<table class=\"hc-fields\">"), html);
		Check.That("header cells", html.Contains("<th>Field</th><th>What to enter</th>"), html);
		Check.That("body row", html.Contains("<tr><td>Date Range</td><td>Two dates.</td></tr>"), html);
		Check.Equal("two body rows", 2, html.Split("<tr><td>").Length - 1);
		Check.That("separator row not rendered", !html.Contains("---"), html);
	}

	private static void CalloutsToHtml()
	{
		var html = HelpMarkdown.ToHtml(Body, "/help/x/images");
		Check.That("tip callout", html.Contains("<div class=\"hc-callout hc-tip\"><b>Tip</b> Use <strong>Export</strong> for Excel.</div>"), html);
		Check.That("careful callout", html.Contains("<div class=\"hc-callout hc-warn\"><b>Careful</b> Deleting a Draft cannot be undone.</div>"), html);
		Check.That("plain note unchanged", HelpMarkdown.ToHtml("> Just a note", "").Contains("<div class=\"help-note\">"));
	}

	private static void TableAndCalloutToSpeech()
	{
		var speech = HelpMarkdown.ToSpeech("Title", Body);
		Check.That("table row read as label and text", speech.Contains("Date Range: Two dates."), speech);
		Check.That("header row not read", !speech.Contains("Field: What to enter"), speech);
		Check.That("no pipes read", !speech.Contains('|'), speech);
		Check.That("tip read as a sentence", speech.Contains("Tip. Use Export for Excel."), speech);
	}

	private static void StepTextForTour()
	{
		Check.Equal("steps ignore callouts and tables", "Tap a card.,Search.", string.Join(",", HelpSteps.Of(Body)));
	}
}
