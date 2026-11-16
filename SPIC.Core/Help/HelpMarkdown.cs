using System;
using System.Collections.Generic;
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
            void CloseNote()
            {
                if (!inNote) return;
                FlushParagraph();
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

            foreach (var raw in (body ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0)
                {
                    FlushParagraph();
                    CloseList();
                    CloseNote();
                    continue;
                }

                if (line.StartsWith("> ", StringComparison.Ordinal) || line == ">")
                {
                    if (!inNote) { FlushParagraph(); CloseList(); html.Append("<div class=\"help-note\">"); inNote = true; }
                    paragraph.Add(line.TrimStart('>').Trim());
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
                    paragraph.Add(line);
                }
            }

            FlushParagraph();
            CloseList();
            CloseNote();
            return html.ToString();
        }

        /// <summary>Plain text for the voice: title first, Markdown marks and images removed,
        /// numbered steps read as "Step 1.".</summary>
        public static string ToSpeech(string title, string body)
        {
            var lines = new List<string> { EndSentence(Plain(title ?? string.Empty)) };
            foreach (var raw in (body ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                var line = Image.Replace(raw.Trim(), string.Empty).Trim();
                if (line.Length == 0) continue;

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
