using System;
using System.Collections.Generic;
using System.Linq;

namespace SPIC.Core.Help
{
    /// <summary>
    /// Reads a guide file:
    /// <code>
    /// ---
    /// module: publicity
    /// title: Publicity Material Tracking
    /// icon: bi-megaphone
    /// order: 40
    /// ---
    ///
    /// ::: section raise-request
    /// title: Raise a publicity request
    /// routes: /PublicityRequest, /PublicityRequest/new
    /// for: PublicityRequest.Entry
    ///
    /// Body markdown...
    /// :::
    /// </code>
    /// Header lines run until the first blank line; list values are comma-separated. A section
    /// without its closing ":::" or with a repeated id is skipped and reported in
    /// <see cref="HelpGuide.Problems"/> - a content mistake must never take the Help Desk down.
    /// </summary>
    public static class HelpGuideParser
    {
        private const string SectionMarker = "::: section";
        private const string CloseMarker = ":::";

        public static HelpGuide Parse(string module, string language, string text)
        {
            var lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var problems = new List<string>();
            var front = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var i = 0;

            while (i < lines.Length && lines[i].Trim().Length == 0) i++;
            if (i < lines.Length && lines[i].Trim() == "---")
            {
                i++;
                while (i < lines.Length && lines[i].Trim() != "---")
                {
                    AddHeader(front, lines[i]);
                    i++;
                }
                i++; // closing ---
            }

            var sections = new List<HelpSection>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (i < lines.Length)
            {
                var trimmed = lines[i].Trim();
                if (!trimmed.StartsWith(SectionMarker, StringComparison.Ordinal))
                {
                    i++;
                    continue;
                }

                var id = trimmed.Substring(SectionMarker.Length).Trim();
                i++;

                var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (i < lines.Length && lines[i].Trim().Length > 0 && !IsMarker(lines[i]))
                {
                    AddHeader(header, lines[i]);
                    i++;
                }

                var body = new List<string>();
                var closed = false;
                while (i < lines.Length)
                {
                    var t = lines[i].Trim();
                    if (t == CloseMarker) { closed = true; i++; break; }
                    if (t.StartsWith(SectionMarker, StringComparison.Ordinal)) break; // next section began
                    body.Add(lines[i]);
                    i++;
                }

                if (id.Length == 0)
                {
                    problems.Add("A section has no id after '::: section'.");
                    continue;
                }
                if (!closed)
                {
                    problems.Add($"Section '{id}' has no closing ':::' and was skipped.");
                    continue;
                }
                if (!seen.Add(id))
                {
                    problems.Add($"Section id '{id}' is used more than once; only the first is kept.");
                    continue;
                }

                sections.Add(new HelpSection(
                    Id: id,
                    Title: Value(header, "title"),
                    Routes: List(header, "routes"),
                    For: List(header, "for"),
                    Roles: List(header, "roles"),
                    Designations: List(header, "designations"),
                    Body: JoinBody(body),
                    SourceHash: NullIfEmpty(Value(header, "source-hash")),
                    Confidence: ConfidenceOf(language, Value(header, "confidence"))));
            }

            return new HelpGuide(
                Module: NullIfEmpty(Value(front, "module")) ?? module,
                Language: language,
                Title: NullIfEmpty(Value(front, "title")) ?? module,
                Icon: NullIfEmpty(Value(front, "icon")) ?? "bi-question-circle",
                Order: int.TryParse(Value(front, "order"), out var order) ? order : 1000,
                Sections: sections,
                Problems: problems);
        }

        private static bool IsMarker(string line)
        {
            var t = line.Trim();
            return t == CloseMarker || t.StartsWith(SectionMarker, StringComparison.Ordinal);
        }

        private static void AddHeader(Dictionary<string, string> into, string line)
        {
            var colon = line.IndexOf(':');
            if (colon <= 0) return;
            into[line.Substring(0, colon).Trim()] = line.Substring(colon + 1).Trim();
        }

        private static string Value(Dictionary<string, string> header, string key) =>
            header.TryGetValue(key, out var v) ? v : string.Empty;

        private static IReadOnlyList<string> List(Dictionary<string, string> header, string key) =>
            Value(header, key)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

        private static HelpConfidence ConfidenceOf(string language, string value)
        {
            if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)) return HelpConfidence.Source;
            return string.Equals(value, "low", StringComparison.OrdinalIgnoreCase) ? HelpConfidence.Low : HelpConfidence.Ok;
        }

        private static string JoinBody(List<string> body)
        {
            var start = 0;
            var end = body.Count;
            while (start < end && body[start].Trim().Length == 0) start++;
            while (end > start && body[end - 1].Trim().Length == 0) end--;
            return string.Join("\n", body.Skip(start).Take(end - start).Select(l => l.TrimEnd()));
        }
    }
}
