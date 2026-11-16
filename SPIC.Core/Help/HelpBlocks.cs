using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SPIC.Core.Help
{
    /// <summary>One item of a block: id and layout on its header line, translatable text below it.</summary>
    public sealed record HelpBlockItem(
        string Id,
        string Header,
        IReadOnlyDictionary<string, string> Layout,
        IReadOnlyDictionary<string, string> Text,
        IReadOnlyList<string> Bullets);

    public sealed record HelpOverviewCard(string Id, string Icon, string Color, string Title, string Text);

    public sealed record HelpLane(string Id, string Color, IReadOnlyList<string> Roles, IReadOnlyList<string> Designations, string Title, string Sub);

    public sealed record HelpStage(
        string Id, string Lane, int Col, string Icon, string Color, string? Screen,
        IReadOnlyList<string> Next, string? Return, bool Exception, string? From, string? To,
        string Title, string Text, string Status);

    public sealed record HelpFlowView(IReadOnlyList<HelpLane> Lanes, IReadOnlyList<HelpStage> Stages);

    public sealed record HelpStatusTag(string Id, string Color, string Label, string Text);

    public sealed record HelpRoleCard(string Id, string Color, IReadOnlyList<string> Roles, IReadOnlyList<string> Designations, string Title, IReadOnlyList<string> Tasks);

    public sealed record HelpFaqItem(string Id, bool Popular, string Question, string Answer);

    /// <summary>
    /// Module-level blocks of a guide (overview, flow, statuses, roles, faq). A block body is
    /// optional lead paragraphs, then items:
    /// <code>
    /// - raise | lane: rmdo | col: 1 | color: #64748b      ← id + layout (English only)
    ///   title: Raise and submit                           ← text (translated)
    ///   text: Fill the form ...
    ///   - a bullet                                        ← bullets (translated)
    /// </code>
    /// Translations may change text only: <see cref="MergeLayout"/> rebuilds every header line from
    /// English, so a translator can never move a stage, recolour a tag or re-point a link.
    /// </summary>
    public static class HelpBlocks
    {
        private sealed class Draft
        {
            public string Id = string.Empty;
            public string Header = string.Empty;
            public readonly Dictionary<string, string> Layout = new(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, string> Text = new(StringComparer.OrdinalIgnoreCase);
            public readonly List<string> Bullets = new();
        }

        private static string[] Lines(string body) => (body ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        private static bool IsHeader(string line) => line.StartsWith("- ", StringComparison.Ordinal) || line == "-";

        /// <summary>The paragraphs before the first item.</summary>
        public static string Lead(string body)
        {
            var lead = new List<string>();
            foreach (var line in Lines(body))
            {
                if (IsHeader(line)) break;
                lead.Add(line.TrimEnd());
            }
            return string.Join("\n", lead).Trim();
        }

        public static IReadOnlyList<HelpBlockItem> Items(string body, List<string>? problems = null)
        {
            var items = new List<Draft>();
            Draft? current = null;
            var inItems = false;

            foreach (var raw in Lines(body))
            {
                var line = raw.TrimEnd();
                if (line.Trim().Length == 0) continue;

                if (IsHeader(line))
                {
                    inItems = true;
                    var parts = line.Substring(1).Split('|').Select(p => p.Trim()).ToList();
                    var id = parts[0];
                    if (id.Length == 0)
                    {
                        problems?.Add($"An item has no id: '{line}'.");
                        current = null;
                        continue;
                    }
                    current = new Draft { Id = id, Header = line.Trim() };
                    foreach (var kv in parts.Skip(1))
                    {
                        var colon = kv.IndexOf(':');
                        if (colon <= 0) { problems?.Add($"Item '{id}': '{kv}' is not 'key: value'."); continue; }
                        current.Layout[kv.Substring(0, colon).Trim()] = kv.Substring(colon + 1).Trim();
                    }
                    items.Add(current);
                    continue;
                }

                var indented = line.StartsWith("  ", StringComparison.Ordinal);
                if (!indented)
                {
                    if (inItems) problems?.Add($"Line '{line.Trim()}' is outside any item.");
                    continue; // lead text
                }
                if (current is null)
                {
                    problems?.Add($"Line '{line.Trim()}' is outside any item.");
                    continue;
                }

                var t = line.Trim();
                if (t.StartsWith("- ", StringComparison.Ordinal))
                {
                    current.Bullets.Add(t.Substring(2).Trim());
                    continue;
                }
                var c = t.IndexOf(':');
                if (c <= 0 || t.Substring(0, c).Contains(' '))
                {
                    problems?.Add($"Item '{current.Id}': '{t}' is not 'field: text'.");
                    continue;
                }
                current.Text[t.Substring(0, c).Trim()] = t.Substring(c + 1).Trim();
            }

            return items.Select(d => new HelpBlockItem(d.Id, d.Header, d.Layout, d.Text, d.Bullets)).ToList();
        }

        /// <summary>
        /// English item order and header lines; text, bullets and lead from the translation where it
        /// has them, English otherwise. Items only the translation has are dropped.
        /// </summary>
        public static string MergeLayout(string englishBody, string translatedBody)
        {
            var en = Items(englishBody);
            var tr = Items(translatedBody).GroupBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            var lead = Lead(translatedBody);
            if (lead.Length == 0) lead = Lead(englishBody);
            if (lead.Length > 0) sb.Append(lead).Append("\n\n");

            foreach (var item in en)
            {
                tr.TryGetValue(item.Id, out var t);
                sb.Append(item.Header).Append('\n');
                foreach (var (field, value) in item.Text)
                {
                    var text = t is not null && t.Text.TryGetValue(field, out var tv) && tv.Trim().Length > 0 ? tv : value;
                    sb.Append("  ").Append(field).Append(": ").Append(OneLine(text)).Append('\n');
                }
                var bullets = t is not null && t.Bullets.Count > 0 ? t.Bullets : item.Bullets;
                foreach (var b in bullets) sb.Append("  - ").Append(OneLine(b)).Append('\n');
            }
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>What Listen reads: a screen's markdown speech, or a block's text in reading order.</summary>
        public static string SpeechText(HelpSection s)
        {
            if (s.Kind == "screen") return HelpMarkdown.ToSpeech(s.Title, s.Body);

            var parts = new List<string> { s.Title };
            var lead = Lead(s.Body);
            if (lead.Length > 0) parts.Add(HelpMarkdown.ToSpeech(string.Empty, lead));

            IEnumerable<HelpBlockItem> items = Items(s.Body);
            if (s.Kind == "flow")
            {
                var all = items.ToList();
                items = all.Where(i => !IsLane(i))
                    .Select((i, index) => (i, index))
                    .OrderBy(x => IsException(x.i) ? 1 : 0)
                    .ThenBy(x => Col(x.i) ?? int.MaxValue)
                    .ThenBy(x => x.index)
                    .Select(x => x.i);
            }

            foreach (var item in items)
            {
                foreach (var field in new[] { "title", "label", "status", "q", "text", "a" })
                    if (item.Text.TryGetValue(field, out var v) && v.Trim().Length > 0)
                        parts.Add(HelpMarkdown.ToSpeech(string.Empty, v));
                parts.AddRange(item.Bullets.Select(b => HelpMarkdown.ToSpeech(string.Empty, b)));
            }

            return string.Join(" ", parts.Select(Sentence).Where(p => p.Length > 0));
        }

        /// <summary>Every translatable line: the lead, field values and bullets (no layout). A screen gives its body lines.</summary>
        public static IReadOnlyList<string> TextLines(HelpSection s)
        {
            if (s.Kind == "screen")
                return Lines(s.Body).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

            var lines = Lines(Lead(s.Body)).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            foreach (var item in Items(s.Body))
            {
                lines.AddRange(item.Text.Values.Where(v => v.Trim().Length > 0));
                lines.AddRange(item.Bullets);
            }
            return lines;
        }

        // ---------- typed views ----------

        public static IReadOnlyList<HelpOverviewCard> Overview(HelpSection s, List<string> problems) =>
            Items(s.Body, problems).Select(i => new HelpOverviewCard(i.Id, L(i, "icon", "bi-check2-circle"), L(i, "color", "#0b6b3a"), T(i, "title"), T(i, "text"))).ToList();

        public static HelpFlowView Flow(HelpSection s, List<string> problems)
        {
            var items = Items(s.Body, problems);
            var lanes = items.Where(IsLane)
                .Select(i => new HelpLane(i.Id, L(i, "color", "#64748b"), List(i, "roles"), List(i, "designations"), T(i, "title"), T(i, "sub")))
                .ToList();
            var stages = items.Where(i => !IsLane(i)).Select(i => new HelpStage(
                Id: i.Id,
                Lane: L(i, "lane", string.Empty),
                Col: Col(i) ?? 0,
                Icon: L(i, "icon", "bi-circle"),
                Color: L(i, "color", "#64748b"),
                Screen: NullIfEmpty(L(i, "screen", string.Empty)),
                Next: List(i, "next"),
                Return: NullIfEmpty(L(i, "return", string.Empty)),
                Exception: IsException(i),
                From: NullIfEmpty(L(i, "from", string.Empty)),
                To: NullIfEmpty(L(i, "to", string.Empty)),
                Title: T(i, "title"),
                Text: T(i, "text"),
                Status: T(i, "status"))).ToList();
            return new HelpFlowView(lanes, stages);
        }

        public static IReadOnlyList<HelpStatusTag> Statuses(HelpSection s, List<string> problems) =>
            Items(s.Body, problems).Select(i => new HelpStatusTag(i.Id, L(i, "color", "#64748b"), T(i, "label"), T(i, "text"))).ToList();

        public static IReadOnlyList<HelpRoleCard> Roles(HelpSection s, List<string> problems) =>
            Items(s.Body, problems).Select(i => new HelpRoleCard(i.Id, L(i, "color", "#0b6b3a"), List(i, "roles"), List(i, "designations"), T(i, "title"), i.Bullets)).ToList();

        public static IReadOnlyList<HelpFaqItem> Faq(HelpSection s, List<string> problems) =>
            Items(s.Body, problems).Select(i => new HelpFaqItem(i.Id, string.Equals(L(i, "popular", "false"), "true", StringComparison.OrdinalIgnoreCase), T(i, "q"), T(i, "a"))).ToList();

        /// <summary>Content mistakes in an English guide's blocks. Never throws.</summary>
        public static IReadOnlyList<string> Validate(HelpGuide english)
        {
            var problems = new List<string>();
            var screens = new HashSet<string>(english.Sections.Where(x => x.Kind == "screen").Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

            foreach (var group in english.Sections.Where(x => x.Kind != "screen").GroupBy(x => x.Kind))
                if (group.Count() > 1)
                    problems.Add($"There is more than one '{group.Key}' block ({string.Join(", ", group.Select(x => x.Id))}).");

            foreach (var s in english.Sections.Where(x => x.Kind != "screen"))
            {
                var local = new List<string>();
                var items = Items(s.Body, local);
                foreach (var dup in items.GroupBy(i => i.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                    local.Add($"Item id '{dup.Key}' is used more than once.");

                if (s.Kind == "flow")
                {
                    var flow = Flow(s, new List<string>());
                    var lanes = new HashSet<string>(flow.Lanes.Select(l => l.Id), StringComparer.OrdinalIgnoreCase);
                    var stages = new HashSet<string>(flow.Stages.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                    if (lanes.Count == 0) local.Add("The flow has no lanes (items with 'type: lane').");
                    foreach (var item in items.Where(i => !IsLane(i)))
                    {
                        if (Col(item) is not > 0) local.Add($"Stage '{item.Id}': col must be a positive number.");
                        var lane = L(item, "lane", string.Empty);
                        if (!lanes.Contains(lane)) local.Add($"Stage '{item.Id}': lane '{lane}' is not a lane.");
                    }
                    foreach (var st in flow.Stages)
                    {
                        foreach (var n in st.Next.Where(n => !stages.Contains(n))) local.Add($"Stage '{st.Id}': next '{n}' is not a stage.");
                        if (st.Return is { } r && !stages.Contains(r)) local.Add($"Stage '{st.Id}': return '{r}' is not a stage.");
                        if (st.From is { } f && !stages.Contains(f)) local.Add($"Stage '{st.Id}': from '{f}' is not a stage.");
                        if (st.To is { } t && !stages.Contains(t)) local.Add($"Stage '{st.Id}': to '{t}' is not a stage.");
                        if (st.Screen is { } sc && !screens.Contains(sc)) local.Add($"Stage '{st.Id}': screen '{sc}' is not a screen section.");
                    }
                }

                problems.AddRange(local.Select(p => $"Block '{s.Id}': {p}"));
            }
            return problems;
        }

        // ---------- helpers ----------

        private static bool IsLane(HelpBlockItem i) => string.Equals(L(i, "type", string.Empty), "lane", StringComparison.OrdinalIgnoreCase);

        private static bool IsException(HelpBlockItem i) => string.Equals(L(i, "kind", string.Empty), "exception", StringComparison.OrdinalIgnoreCase);

        private static int? Col(HelpBlockItem i) =>
            int.TryParse(L(i, "col", string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) ? c : null;

        private static string L(HelpBlockItem i, string key, string fallback) =>
            i.Layout.TryGetValue(key, out var v) && v.Length > 0 ? v : fallback;

        private static string T(HelpBlockItem i, string key) =>
            i.Text.TryGetValue(key, out var v) ? v : string.Empty;

        private static IReadOnlyList<string> List(HelpBlockItem i, string key) =>
            L(i, key, string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

        private static string OneLine(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();

        private static string Sentence(string s)
        {
            var t = s.Trim();
            if (t.Length == 0) return t;
            var last = t[t.Length - 1];
            return last is '.' or '?' or '!' or '।' or ':' ? t : t + ".";
        }
    }
}
