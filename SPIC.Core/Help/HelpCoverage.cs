using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SPIC.Core.Help
{
    /// <summary>One finding of the coverage check. <see cref="Kind"/> is one of
    /// uncovered-route, unknown-permission, missing-image, stale-translation,
    /// missing-translation, parse-problem.</summary>
    public sealed record CoverageIssue(string Kind, string Module, string Detail);

    /// <summary>
    /// The checks behind <c>help-tools check</c>, kept here (pure, no file system) so they are tested
    /// in the regression harness. The tool supplies the page routes, the ignore list, an image
    /// existence probe and the PagePermission names.
    /// </summary>
    public static class HelpCoverage
    {
        private static readonly Regex Image = new(@"!\[[^\]]*\]\(([^)\s]+)\)", RegexOptions.Compiled);

        /// <param name="imageExists">Called with "module/relative/path.png".</param>
        public static IReadOnlyList<CoverageIssue> Check(
            HelpLibrary library,
            IEnumerable<string> pageRoutes,
            IEnumerable<string> ignoredRoutes,
            Func<string, bool> imageExists,
            IEnumerable<string> permissionNames)
        {
            var issues = new List<CoverageIssue>();
            var permissions = new HashSet<string>(permissionNames, StringComparer.OrdinalIgnoreCase);
            var ignored = new HashSet<string>(ignoredRoutes.Select(HelpRoutes.Shape), StringComparer.OrdinalIgnoreCase);
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var module in library.Modules)
            {
                var en = library.English(module)!;
                foreach (var problem in en.Problems)
                    issues.Add(new CoverageIssue("parse-problem", module, $"en: {problem}"));

                foreach (var s in en.Sections)
                {
                    foreach (var r in s.Routes) covered.Add(HelpRoutes.Shape(r));

                    foreach (var token in s.For)
                    {
                        var page = token.Split('.')[0];
                        if (!permissions.Contains(page))
                            issues.Add(new CoverageIssue("unknown-permission", module, $"section '{s.Id}': for '{token}' is not a PagePermission"));
                    }

                    CheckImages(module, "en", s, imageExists, issues);
                }

                foreach (var lang in library.Config.Languages.Select(l => l.Code).Where(c => !c.Equals("en", StringComparison.OrdinalIgnoreCase)))
                {
                    var translated = library.Raw(module, lang);
                    if (translated is null)
                    {
                        issues.Add(new CoverageIssue("missing-translation", module, $"{lang}: no guide.{lang}.md"));
                        continue;
                    }

                    foreach (var problem in translated.Problems)
                        issues.Add(new CoverageIssue("parse-problem", module, $"{lang}: {problem}"));

                    var byId = translated.Sections.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
                    foreach (var s in en.Sections)
                    {
                        if (!byId.TryGetValue(s.Id, out var t))
                        {
                            issues.Add(new CoverageIssue("missing-translation", module, $"{lang}: section '{s.Id}' not translated"));
                            continue;
                        }
                        if (!string.Equals(t.SourceHash, HelpHash.Of(s.Title, s.Body), StringComparison.OrdinalIgnoreCase))
                            issues.Add(new CoverageIssue("stale-translation", module, $"{lang}: section '{s.Id}' was translated from older English"));
                        CheckImages(module, lang, t, imageExists, issues);
                    }
                }
            }

            foreach (var route in pageRoutes.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(r => r, StringComparer.OrdinalIgnoreCase))
            {
                var shape = HelpRoutes.Shape(route);
                if (ignored.Contains(shape) || covered.Contains(shape)) continue;
                issues.Add(new CoverageIssue("uncovered-route", ModuleHint(route), route));
            }

            return issues;
        }

        private static void CheckImages(string module, string lang, HelpSection s, Func<string, bool> imageExists, List<CoverageIssue> issues)
        {
            foreach (Match m in Image.Matches(s.Body))
            {
                var path = m.Groups[1].Value;
                if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase) || path.StartsWith('/')) continue;
                var relative = $"{module}/{path.TrimStart('.', '/')}";
                if (!imageExists(relative))
                    issues.Add(new CoverageIssue("missing-image", module, $"{lang}: section '{s.Id}' links {path}, which does not exist"));
            }
        }

        // Groups an uncovered route under its first path segment in the report.
        private static string ModuleHint(string route) =>
            HelpRoutes.Normalize(route).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "(root)";
    }
}
