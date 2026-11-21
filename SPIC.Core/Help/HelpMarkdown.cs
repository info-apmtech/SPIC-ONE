using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SPIC.Core.Help
{
    /// <summary>
    /// The small Markdown subset guides use, rendered without a third-party package (the API and
    /// the offline MAUI app both use it): <c>##</c>/<c>###</c> headings, paragraphs,
    /// <c>**bold**</c>, <c>1.</c> and <c>- </c> lists, <c>&gt; </c> notes and
    /// <c>![alt](images/file.png)</c>. Everything is HTML-encoded first, so guide text can never
    /// inject markup.
    /// </summary>
    public static class HelpMarkdown
    {
        private static readonly Regex Ordered = new(@"^(\d+)\.\s+(.*)$", RegexOptions.Compiled);
        private static readonly Regex Image = new(@"!\[([^\]]*)\]\(([^)\s]+)\)", RegexOptions.Compiled);
        private static readonly Regex Bold = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex LabelLine = new(@"^\*\*[^*]+\*\*$", RegexOptions.Compiled);

        public static string ToHtml(string body, string imageBase)
        {
            var html = new StringBuilder();
            var paragraph = new List<string>();
            string? openList = null; // "ol" | "ul"
            var inNote = false;

            void FlushParagraph()
            {
                if (paragraph.Count == 0) return;
                html.Append("<p>").Append(Inline(string.Join(" ", paragraph), imageBase)).Append("</p>");
                paragraph.Clear();
            }
            void CloseList()
            {
                if (openList == null) return;
                html.Append("</").Append(openList).Append('>');
                openList = null;
            }
            var calloutOpen = false;
            void CloseNote()
            {
                if (!inNote) return;
                if (calloutOpen)
                {
                    // A callout's text sits inline after its label, not in a <p>.
                    if (paragraph.Count > 0) html.Append(' ').Append(Inline(string.Join(" ", paragraph), imageBase));
                    paragraph.Clear();
                    calloutOpen = false;
                }
                else FlushParagraph();
                html.Append("</div>");
                inNote = false;
            }
            void OpenList(string kind)
            {
                if (openList == kind) return;
                CloseList();
                html.Append('<').Append(kind).Append('>');
                openList = kind;
            }

            var table = new List<string>();
            void FlushTable()
            {
                if (table.Count == 0) return;
                var rows = table.Where(r => !IsTableSeparator(r)).Select(Cells).ToList();
                var hasHeader = table.Count > 1 && IsTableSeparator(table[1]);
                html.Append("<table class=\"hc-fields\">");
                for (var r = 0; r < rows.Count; r++)
                {
                    var cell = hasHeader && r == 0 ? "th" : "td";
                    html.Append("<tr>");
                    foreach (var c in rows[r]) html.Append('<').Append(cell).Append('>').Append(Inline(c, imageBase)).Append("</").Append(cell).Append('>');
                    html.Append("</tr>");
                }
                html.Append("</table>");
                table.Clear();
            }

            foreach (var raw in (body ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.StartsWith('|'))
                {
                    if (table.Count == 0) { FlushParagraph(); CloseList(); CloseNote(); }
                    table.Add(line);
                    continue;
                }
                FlushTable();

                if (line.Length == 0)
                {
                    if (inNote) CloseNote(); else FlushParagraph();
                    CloseList();
                    continue;
                }

                if (line.StartsWith("> ", StringComparison.Ordinal) || line == ">")
                {
                    var text = line.TrimStart('>').Trim();
                    if (!inNote)
                    {
                        FlushParagraph(); CloseList();
                        var callout = Callout(text);
                        if (callout is { } c)
                        {
                            html.Append("<div class=\"hc-callout ").Append(c.Css).Append("\"><b>").Append(c.Label).Append("</b>");
                            text = c.Text;
                            paragraph.Add(text);
                            inNote = true;
                            calloutOpen = true;
                            continue;
                        }
                        html.Append("<div class=\"help-note\">");
                        inNote = true;
                    }
                    paragraph.Add(text);
                    continue;
                }
                CloseNote();

                if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    FlushParagraph(); CloseList();
                    html.Append("<h4>").Append(Inline(line[4..], imageBase)).Append("</h4>");
                }
                else if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    FlushParagraph(); CloseList();
                    html.Append("<h3>").Append(Inline(line[3..], imageBase)).Append("</h3>");
                }
                else if (Ordered.Match(line) is { Success: true } m)
                {
                    FlushParagraph(); OpenList("ol");
                    html.Append("<li>").Append(Inline(m.Groups[2].Value, imageBase)).Append("</li>");
                }
                else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
                {
                    FlushParagraph(); OpenList("ul");
                    html.Append("<li>").Append(Inline(line[2..], imageBase)).Append("</li>");
                }
                else if (Image.Match(line) is { Success: true } img && img.Length == line.Length)
                {
                    FlushParagraph(); CloseList();
                    html.Append("<figure class=\"help-figure\">").Append(Inline(line, imageBase)).Append("</figure>");
                }
                else
                {
                    CloseList();
                    // A line that is only **bold**, with text under it, is a label for that text
                    // ("What this screen is for"): it gets its own line instead of running into it.
                    if (!inNote && paragraph.Count == 1 && LabelLine.IsMatch(paragraph[0]))
                    {
                        html.Append("<p class=\"help-label\">").Append(Inline(paragraph[0], imageBase)).Append("</p>");
                        paragraph.Clear();
                    }
                    paragraph.Add(line);
                }
            }

            FlushTable();
            if (inNote) CloseNote(); else FlushParagraph();
            CloseList();
            return html.ToString();
        }

        /// <summary>"Tip: …" and "Careful: …" (fixed English keywords, also in translations).</summary>
        private static (string Css, string Label, string Text)? Callout(string text)
        {
            if (text.StartsWith("Tip:", StringComparison.Ordinal)) return ("hc-tip", "Tip", text[4..].Trim());
            if (text.StartsWith("Careful:", StringComparison.Ordinal)) return ("hc-warn", "Careful", text[8..].Trim());
            return null;
        }

        private static bool IsTableSeparator(string line) =>
            line.Trim().Trim('|').Split('|').All(c => c.Trim().Length > 0 && c.Trim().All(ch => ch is '-' or ':'));

        private static List<string> Cells(string line) =>
            line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();

        /// <summary>Plain text for the voice: title first, Markdown marks and images removed,
        /// numbered steps read as "Step 1.".</summary>
        public static string ToSpeech(string title, string body)
        {
            var lines = new List<string> { EndSentence(Plain(title ?? string.Empty)) };
            var all = (body ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < all.Length; i++)
            {
                var line = Image.Replace(all[i].Trim(), string.Empty).Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith('|'))
                {
                    // Table: skip the separator and the header row; read "label: text." per row.
                    if (IsTableSeparator(line)) continue;
                    if (i + 1 < all.Length && all[i + 1].Trim().StartsWith('|') && IsTableSeparator(all[i + 1])) continue;
                    var cells = Cells(line).Select(Plain).Where(c => c.Length > 0).ToList();
                    if (cells.Count > 0) lines.Add(EndSentence(cells.Count == 1 ? cells[0] : cells[0] + ": " + string.Join(", ", cells.Skip(1))));
                    continue;
                }

                if (line.StartsWith('>') && Callout(line.TrimStart('>').Trim()) is { } callout)
                {
                    lines.Add(callout.Label + ". " + EndSentence(Plain(callout.Text)));
                    continue;
                }

                if (Ordered.Match(line) is { Success: true } m)
                    line = $"Step {m.Groups[1].Value}. {m.Groups[2].Value}";
                else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
                    line = line[2..];
                else if (line.StartsWith('#'))
                    line = EndSentence(line.TrimStart('#').Trim());

                line = Plain(line.TrimStart('>').Trim());
                if (line.Length > 0) lines.Add(line);
            }
            return string.Join("\n", lines);
        }

        private static string Inline(string text, string imageBase)
        {
            var encoded = WebUtility.HtmlEncode(text);
            encoded = Image.Replace(encoded, m =>
                $"<img src=\"{ImageSrc(m.Groups[2].Value, imageBase)}\" alt=\"{m.Groups[1].Value}\" loading=\"lazy\" />");
            return Bold.Replace(encoded, "<strong>$1</strong>");
        }

        private static string ImageSrc(string path, string imageBase)
        {
            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase) || path.StartsWith('/')) return path;
            return (imageBase ?? string.Empty).TrimEnd('/') + "/" + path.TrimStart('.', '/');
        }

        private static string Plain(string text) => Bold.Replace(text, "$1").Replace("**", string.Empty);

        private static string EndSentence(string text)
        {
            var t = text.Trim();
            if (t.Length == 0) return t;
            return char.IsPunctuation(t[^1]) ? t : t + ".";
        }
    }
}
