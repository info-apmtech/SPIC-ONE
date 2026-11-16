using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SPIC.Core.Help
{
    public sealed record HelpImportResult(string Text, IReadOnlyList<string> Problems);

    /// <summary>
    /// Checks a translation written outside the translate command (by hand, by an agency or by an
    /// assistant) and writes it in the stamped format: routes and filters from English, the current
    /// English source-hash, confidence "ok". Each section must exist in English and keep every
    /// on-screen name in **bold** in English, so readers can find it on screen.
    /// </summary>
    public static class HelpTranslationImport
    {
        private static readonly Regex Bold = new(@"\*\*([^*\n]+)\*\*", RegexOptions.Compiled);

        /// <summary>
        /// The **bold** phrases in an English body that name something on screen and must stay English.
        /// Not screen names: a bold that is a whole line (a lead-in such as "What this screen is for")
        /// and a bold sentence of three or more words ending in . ? or ! (a problem statement).
        /// </summary>
        public static IReadOnlyList<string> ScreenNames(string body)
        {
            var names = new List<string>();
            foreach (var line in (body ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                var trimmed = line.Trim();
                var matches = Bold.Matches(trimmed);
                if (matches.Count == 1 && matches[0].Length == trimmed.Length) continue; // whole-line lead-in
                foreach (Match m in matches)
                {
                    var text = m.Groups[1].Value.Trim();
                    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    var sentence = words >= 3 && (text.EndsWith(".") || text.EndsWith("?") || text.EndsWith("!"));
                    if (!sentence && !names.Contains(text)) names.Add(text);
                }
            }
            return names;
        }

        public static HelpImportResult Build(HelpGuide english, HelpGuide draft, string language)
        {
            var problems = new List<string>();
            problems.AddRange(draft.Problems.Select(p => "draft: " + p));

            var translated = new Dictionary<string, (string Title, string Body, HelpConfidence Confidence)>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(draft.Title) && !string.Equals(draft.Title, draft.Module, StringComparison.OrdinalIgnoreCase))
                translated[HelpGuideWriter.GuideTitleKey] = (draft.Title, string.Empty, HelpConfidence.Ok);
            if (!string.IsNullOrWhiteSpace(draft.Category))
                translated[HelpGuideWriter.CategoryKey] = (draft.Category, string.Empty, HelpConfidence.Ok);
            if (!string.IsNullOrWhiteSpace(draft.Note))
                translated[HelpGuideWriter.NoteKey] = (draft.Note, string.Empty, HelpConfidence.Ok);

            foreach (var s in english.Sections)
            {
                var d = draft.Sections.FirstOrDefault(x => string.Equals(x.Id, s.Id, StringComparison.OrdinalIgnoreCase));
                if (d is null) { problems.Add($"{s.Id}: section missing"); continue; }
                if (string.IsNullOrWhiteSpace(d.Title) || string.IsNullOrWhiteSpace(d.Body)) { problems.Add($"{s.Id}: empty title or body"); continue; }

                var isBlock = s.Kind != "screen";
                if (isBlock)
                {
                    var enIds = HelpBlocks.Items(s.Body).Select(i => i.Id).ToList();
                    var draftIds = HelpBlocks.Items(d.Body).Select(i => i.Id).ToList();
                    foreach (var missing in enIds.Where(id => !draftIds.Contains(id, StringComparer.OrdinalIgnoreCase)))
                        problems.Add($"{s.Id}: item {missing} missing");
                    foreach (var extra in draftIds.Where(id => !enIds.Contains(id, StringComparer.OrdinalIgnoreCase)))
                        problems.Add($"{s.Id}: item {extra} is not in English");
                }

                // Blocks: check only their text (lead, fields, bullets), never their layout lines.
                var draftText = isBlock ? string.Join("\n", HelpBlocks.TextLines(d with { Kind = s.Kind })) : d.Body;
                var englishText = isBlock ? string.Join("\n", HelpBlocks.TextLines(s)) : s.Body;
                var draftBolds = new HashSet<string>(Bold.Matches(draftText).Select(m => m.Groups[1].Value.Trim()), StringComparer.Ordinal);
                foreach (var name in ScreenNames(englishText).Where(n => !draftBolds.Contains(n)))
                    problems.Add($"{s.Id}: **{name}** must stay in English");

                translated[s.Id] = (d.Title, isBlock ? HelpBlocks.MergeLayout(s.Body, d.Body) : d.Body, HelpConfidence.Ok);
            }

            foreach (var extra in draft.Sections.Where(d => !english.Sections.Any(s => string.Equals(s.Id, d.Id, StringComparison.OrdinalIgnoreCase))))
                problems.Add($"{extra.Id}: not a section of the English guide");

            return new HelpImportResult(HelpGuideWriter.Write(english, language, translated), problems);
        }
    }
}
