using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPIC.Core.Help
{
    /// <summary>Which sections of a translated guide need (re)translating.</summary>
    public static class HelpTranslationPlanner
    {
        /// <summary>Ids of English sections whose translation is missing or was made from older English.</summary>
        public static IReadOnlyList<string> SectionsToTranslate(HelpGuide english, HelpGuide? translated)
        {
            var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (translated is not null)
            {
                foreach (var s in english.Sections)
                {
                    var t = translated.Sections.FirstOrDefault(x => string.Equals(x.Id, s.Id, StringComparison.OrdinalIgnoreCase));
                    if (t is not null && string.Equals(t.SourceHash, HelpHash.Of(s.Title, s.Body), StringComparison.OrdinalIgnoreCase))
                        current.Add(s.Id);
                }
            }
            return english.Sections.Where(s => !current.Contains(s.Id)).Select(s => s.Id).ToList();
        }
    }

    /// <summary>
    /// Writes a translated guide file. Front matter and every section header (routes, for, roles,
    /// designations) are copied from the English guide, so a translation can never change who sees
    /// a section; only the title, body and confidence come from the translation. Sections not in
    /// <c>translated</c> are left out (the library shows English for them). The key "_title" carries
    /// the translated guide title.
    /// </summary>
    public static class HelpGuideWriter
    {
        public const string GuideTitleKey = "_title";

        /// <param name="sourceHashes">Section id → the source-hash to keep for sections copied unchanged
        /// from an existing file (so a stale section stays visibly stale). Sections not listed get the
        /// current English hash - only pass new or corrected translations without one.</param>
        public static string Write(
            HelpGuide english,
            string language,
            IReadOnlyDictionary<string, (string Title, string Body, HelpConfidence Confidence)> translated,
            IReadOnlyDictionary<string, string?>? sourceHashes = null)
        {
            var sb = new StringBuilder();
            var title = translated.TryGetValue(GuideTitleKey, out var gt) && !string.IsNullOrWhiteSpace(gt.Title) ? gt.Title : english.Title;

            sb.Append("---\n");
            sb.Append("module: ").Append(english.Module).Append('\n');
            sb.Append("title: ").Append(OneLine(title)).Append('\n');
            sb.Append("icon: ").Append(english.Icon).Append('\n');
            sb.Append("order: ").Append(english.Order).Append('\n');
            sb.Append("language: ").Append(language).Append('\n');
            sb.Append("---\n");

            foreach (var s in english.Sections)
            {
                if (!translated.TryGetValue(s.Id, out var t)) continue;

                sb.Append('\n');
                sb.Append("::: section ").Append(s.Id).Append('\n');
                sb.Append("title: ").Append(OneLine(string.IsNullOrWhiteSpace(t.Title) ? s.Title : t.Title)).Append('\n');
                sb.Append("routes: ").Append(string.Join(", ", s.Routes)).Append('\n');
                sb.Append("for: ").Append(string.Join(", ", s.For)).Append('\n');
                sb.Append("roles: ").Append(string.Join(", ", s.Roles)).Append('\n');
                sb.Append("designations: ").Append(string.Join(", ", s.Designations)).Append('\n');
                var hash = sourceHashes is not null && sourceHashes.TryGetValue(s.Id, out var kept) && !string.IsNullOrWhiteSpace(kept)
                    ? kept
                    : HelpHash.Of(s.Title, s.Body);
                sb.Append("source-hash: ").Append(hash).Append('\n');
                sb.Append("confidence: ").Append(t.Confidence == HelpConfidence.Low ? "low" : "ok").Append('\n');
                sb.Append('\n');
                sb.Append(SafeBody(t.Body)).Append('\n');
                sb.Append(":::\n");
            }

            return sb.ToString();
        }

        private static string OneLine(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();

        // A body line that looks like a section marker would end or split the section when parsed.
        private static string SafeBody(string body) =>
            string.Join("\n", (body ?? string.Empty).Replace("\r\n", "\n").Split('\n')
                .Where(l => l.Trim() != ":::" && !l.Trim().StartsWith("::: section", StringComparison.Ordinal))
                .Select(l => l.TrimEnd()))
                .Trim('\n');
    }
}
