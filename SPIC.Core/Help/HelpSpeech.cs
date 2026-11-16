using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SPIC.Core.Help
{
    /// <summary>
    /// Builds the SSML request for one guide section's voice. The text is XML-escaped first, so
    /// guide content can never break the request; glossary terms with a "say" entry are wrapped in
    /// <c>&lt;sub alias="..."&gt;</c> so English codes inside Indian-language text (RMDO, SAS) are
    /// read the way the glossary says.
    /// </summary>
    public static class HelpSpeech
    {
        public static string BuildSsml(string text, HelpLanguage language, IReadOnlyList<HelpGlossaryTerm> glossary)
        {
            var escaped = Escape(text ?? string.Empty);

            var sayAs = glossary
                .Where(g => !string.IsNullOrWhiteSpace(g.Term) && !string.IsNullOrWhiteSpace(g.Say))
                .GroupBy(g => Escape(g.Term), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Say!, StringComparer.Ordinal);

            if (sayAs.Count > 0)
            {
                // One pass, longest term first, whole words only.
                var pattern = string.Join("|", sayAs.Keys.OrderByDescending(k => k.Length).Select(Regex.Escape));
                escaped = Regex.Replace(escaped, $@"(?<![\p{{L}}\p{{N}}])(?:{pattern})(?![\p{{L}}\p{{N}}])",
                    m => $"<sub alias=\"{Escape(sayAs[m.Value])}\">{m.Value}</sub>");
            }

            var locale = LocaleOf(language.Voice);
            return $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"{locale}\">" +
                   $"<voice name=\"{Escape(language.Voice)}\">{escaped}</voice></speak>";
        }

        /// <summary>"ta-IN-PallaviNeural" → "ta-IN".</summary>
        public static string LocaleOf(string voice)
        {
            var parts = (voice ?? string.Empty).Split('-');
            return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : "en-IN";
        }

        private static string Escape(string s) => WebUtility.HtmlEncode(s).Replace("&#39;", "&apos;");
    }
}
