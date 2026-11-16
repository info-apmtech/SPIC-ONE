using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using SPIC.Core.Help;

namespace SPIC.HelpTools;

/// <summary>
/// Translates one guide section with Claude, then checks it: the translation is translated back
/// to English and compared with the original, and sections whose meaning drifted are marked LOW
/// so reviewers see them first. A refused or cut-off answer returns null and the section stays
/// English (the library falls back per section).
/// </summary>
internal sealed class Translator
{
	private readonly AnthropicClient _client = new();
	private readonly string _model;

	public Translator(string model) => _model = model;

	public async Task<(string Title, string Body, HelpConfidence Confidence)?> TranslateAsync(
		HelpLanguage lang, IReadOnlyList<HelpGlossaryTerm> glossary, string title, string body)
	{
		var translated = await AskAsync(TranslateSystem(lang, glossary), Envelope(title, body));
		if (translated is null) return null;
		var (tTitle, tBody) = Split(translated);
		if (string.IsNullOrWhiteSpace(tTitle)) return null;

		// A title-only request (the guide title) needs no meaning check.
		if (string.IsNullOrWhiteSpace(body)) return (tTitle, string.Empty, HelpConfidence.Ok);

		var back = await AskAsync(BackSystem(lang), Envelope(tTitle, tBody));
		if (back is null) return (tTitle, tBody, HelpConfidence.Low);

		var verdict = await AskAsync(CompareSystem(lang),
			$"<original>\n{title}\n\n{body}\n</original>\n<back_translation>\n{back}\n</back_translation>");
		var confidence = verdict is not null && verdict.TrimStart().StartsWith("OK", StringComparison.OrdinalIgnoreCase)
			? HelpConfidence.Ok
			: HelpConfidence.Low;
		if (confidence == HelpConfidence.Low && verdict is not null)
			Console.WriteLine($"      low confidence: {verdict.Trim().Split('\n')[0]}");
		return (tTitle, tBody, confidence);
	}

	private async Task<string?> AskAsync(string system, string user)
	{
		var message = await _client.Messages.Create(new MessageCreateParams
		{
			Model = _model,
			MaxTokens = 16000,
			System = system,
			OutputConfig = new OutputConfig { Effort = Effort.Medium },
			Messages = [new MessageParam { Role = Role.User, Content = user }],
		});

		var stop = message.StopReason?.ToString() ?? string.Empty;
		if (stop.Contains("refusal", StringComparison.OrdinalIgnoreCase) || stop.Contains("max_tokens", StringComparison.OrdinalIgnoreCase) || stop.Contains("MaxTokens", StringComparison.OrdinalIgnoreCase))
		{
			Console.WriteLine($"      model stopped with {stop}; section left in English");
			return null;
		}

		var text = new StringBuilder();
		foreach (var block in message.Content)
			if (block.Value is TextBlock t) text.Append(t.Text);
		return text.Length == 0 ? null : text.ToString();
	}

	private static string Envelope(string title, string body) =>
		$"<title>\n{title}\n</title>\n<body>\n{body}\n</body>";

	// First line = title, rest (after one blank line) = body.
	private static (string Title, string Body) Split(string reply)
	{
		var text = reply.Replace("\r\n", "\n").Trim();
		var nl = text.IndexOf('\n');
		return nl < 0 ? (text, string.Empty) : (text[..nl].Trim(), text[(nl + 1)..].Trim('\n'));
	}

	private static string TranslateSystem(HelpLanguage lang, IReadOnlyList<HelpGlossaryTerm> glossary)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"You translate help guides for SPIC ONE, the work app of an agricultural company in India, into {lang.Name} ({lang.Native}).");
		sb.AppendLine($"Readers are field staff, dealers and farmers. Use simple, everyday spoken {lang.Name}, not formal or literary words.");
		sb.AppendLine("Keep the Markdown structure exactly: ## headings, numbered and bulleted lists, **bold**, lines starting with > and image links ![..](..) unchanged.");
		sb.AppendLine("Text in **bold** is a button, tab or field name shown on screen in English: keep it in English exactly as written, inside the same ** marks, so readers can find it on screen.");
		sb.AppendLine("Keep role names, module names, codes and numbers (RMDO, MDO, JMDO, SAS, IDs) in English.");
		var terms = glossary.Where(g => g.Keep || !string.IsNullOrWhiteSpace(g.Write)).ToList();
		if (terms.Count > 0)
		{
			sb.AppendLine("Glossary (follow exactly):");
			foreach (var g in terms)
				sb.AppendLine(g.Keep ? $"- {g.Term}: keep in English" : $"- {g.Term}: write as {g.Write}");
		}
		sb.AppendLine("Reply with the translated title on the first line, then one blank line, then the translated body. Nothing else - no notes, no tags.");
		return sb.ToString();
	}

	private static string BackSystem(HelpLanguage lang) =>
		$"Translate this {lang.Name} help text into plain English, as literally as reads naturally. Keep the Markdown. " +
		"Reply with the title on the first line, one blank line, then the body. Nothing else.";

	private static string CompareSystem(HelpLanguage lang) =>
		$"You check {lang.Name} translations of app help text. You get the ORIGINAL English and a BACK-TRANSLATION of the {lang.Name} version into English. " +
		"Reply OK if every step, button name, role name and number in the original is still present with the same meaning. " +
		"Otherwise reply LOW: followed by a short reason. One line only.";
}
