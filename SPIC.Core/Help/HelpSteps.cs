using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SPIC.Core.Help
{
    /// <summary>
    /// The steps of a screen section: the items of the first numbered list in its body. Step N goes
    /// with hotspot N on the screenshot and with tour step N on the real page.
    /// </summary>
    public static class HelpSteps
    {
        private static readonly Regex Numbered = new(@"^\s*(\d+)\.\s+(.*)$", RegexOptions.Compiled);

        public static IReadOnlyList<string> Of(string body)
        {
            var steps = new List<string>();
            var started = false;
            foreach (var raw in (body ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
            {
                var m = Numbered.Match(raw);
                if (m.Success)
                {
                    started = true;
                    steps.Add(m.Groups[2].Value.Trim());
                    continue;
                }
                // The list ends at the first non-empty line that is not a list item.
                if (started && raw.Trim().Length > 0) break;
            }
            return steps;
        }
    }
}
