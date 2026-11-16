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

                var kind = Value(header, "kind").ToLowerInvariant();
                if (kind.Length == 0) kind = "screen";
                if (Array.IndexOf(HelpKinds, kind) < 0)
                {
                    problems.Add($"Section '{id}' has an unknown kind '{kind}'; it is shown as a screen.");
                    kind = "screen";
                }

                var joined = JoinBody(body);
                sections.Add(new HelpSection(
                    Id: id,
                    Title: Value(header, "title"),
                    Routes: List(header, "routes"),
                    For: List(header, "for"),
                    Roles: List(header, "roles"),
                    Designations: List(header, "designations"),
                    Body: joined,
                    SourceHash: NullIfEmpty(Value(header, "source-hash")),
                    Confidence: ConfidenceOf(language, Value(header, "confidence")))
                {
                    Kind = kind,
                    Screen = kind == "screen" ? ScreenMeta(id, header, joined, problems) : null,
                });
            }

            return new HelpGuide(
                Module: NullIfEmpty(Value(front, "module")) ?? module,
                Language: language,
                Title: NullIfEmpty(Value(front, "title")) ?? module,
                Icon: NullIfEmpty(Value(front, "icon")) ?? "bi-question-circle",
                Order: int.TryParse(Value(front, "order"), out var order) ? order : 1000,
                Sections: sections,
                Problems: problems)
            {
                Category = Value(front, "category"),
                Color = NullIfEmpty(Value(front, "color")) ?? "#0b6b3a",
                Example = Value(front, "example"),
                Updated = DateOnly.TryParseExact(Value(front, "updated"), "yyyy-MM-dd", out var updated) ? updated : null,
                Note = Value(front, "note"),
                MetaSourceHash = NullIfEmpty(Value(front, "meta-source-hash")),
            };
        }

        /// <summary>Section kinds: a screen guide, or one of the module-level blocks.</summary>
        public static readonly string[] HelpKinds = { "screen", "overview", "flow", "statuses", "roles", "faq" };

        private static HelpScreenMeta ScreenMeta(string id, Dictionary<string, string> header, string body, List<string> problems)
        {
            var steps = HelpSteps.Of(body).Count;
            var hotspots = Hotspots(id, "hotspots", Value(header, "hotspots"), steps, problems);
            var phone = Hotspots(id, "hotspots-phone", Value(header, "hotspots-phone"), steps, problems);

            var tour = new List<HelpTourStep>();
            var tourText = Value(header, "tour");
            if (tourText.Length > 0)
            {
                foreach (var part in tourText.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var gt = part.IndexOf('>');
                    if (gt <= 0 || !int.TryParse(part.Substring(0, gt).Trim(), out var n) || part.Substring(gt + 1).Trim().Length == 0)
                    {
                        problems.Add($"Section '{id}': tour step '{part}' is not 'N > selector'.");
                        continue;
                    }
                    tour.Add(new HelpTourStep(n, part.Substring(gt + 1).Trim()));
                }
                if (tour.Count != steps)
                    problems.Add($"Section '{id}': the tour has {tour.Count} step(s) but the body has {steps}.");
                else if (!InOrder(tour.Select(t => t.N)))
                    problems.Add($"Section '{id}': tour steps must be numbered 1..{steps} in order.");
            }

            return new HelpScreenMeta(
                Image: NullIfEmpty(Value(header, "screenshot")),
                Hotspots: hotspots,
                PhoneImage: NullIfEmpty(Value(header, "screenshot-phone")),
                PhoneHotspots: phone,
                Tour: tour,
                Minutes: int.TryParse(Value(header, "minutes"), out var minutes) && minutes > 0 ? minutes : 0);
        }

        /// <summary>"1 @ x,y,w,h | 2 @ ..." with every value a % from 0 to 100.</summary>
        private static IReadOnlyList<HelpHotspot> Hotspots(string id, string key, string text, int steps, List<string> problems)
        {
            var list = new List<HelpHotspot>();
            if (text.Length == 0) return list;

            var bad = false;
            foreach (var part in text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var at = part.IndexOf('@');
                var nums = at > 0
                    ? part.Substring(at + 1).Split(',', StringSplitOptions.TrimEntries)
                    : Array.Empty<string>();
                var values = nums.Select(v => double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : double.NaN).ToArray();
                if (at <= 0 || !int.TryParse(part.Substring(0, at).Trim(), out var n) || values.Length != 4 || values.Any(double.IsNaN))
                {
                    if (!bad) problems.Add($"Section '{id}': {key} entry '{part}' is not 'N @ x,y,w,h'.");
                    bad = true;
                    continue;
                }
                if (values.Any(v => v < 0 || v > 100))
                {
                    if (!bad) problems.Add($"Section '{id}': {key} entry '{part}' must use % values 0–100.");
                    bad = true;
                    continue;
                }
                list.Add(new HelpHotspot(n, values[0], values[1], values[2], values[3]));
            }

            if (!bad && list.Count != steps)
                problems.Add($"Section '{id}': {key} has {list.Count} hotspot(s) but the body has {steps} step(s).");
            else if (!bad && !InOrder(list.Select(h => h.N)))
                problems.Add($"Section '{id}': {key} hotspots must be numbered 1..{steps} in order.");
            return list;
        }

        private static bool InOrder(IEnumerable<int> numbers) =>
            numbers.Select((n, i) => n == i + 1).All(ok => ok);

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
