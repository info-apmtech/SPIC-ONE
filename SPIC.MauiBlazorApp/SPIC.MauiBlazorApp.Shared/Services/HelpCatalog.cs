using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using SPIC.Core.Help;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>A guide section as one user sees it in one language.</summary>
    /// <param name="IsFallbackEnglish">A non-English view showing the English text (no current translation).</param>
    /// <param name="MachineTranslated">Translated text that no reviewer has approved yet.</param>
    public sealed record HelpSectionView(string Module, HelpSection Section, string Language, bool IsFallbackEnglish, bool MachineTranslated);

    public sealed record HelpModuleView(string Module, string Title, string Icon, int Order, IReadOnlyList<HelpSectionView> Sections);

    public sealed record HelpSearchHit(HelpSectionView Section, string Snippet);

    /// <summary>
    /// The Help Desk guides the signed-in user may see (spec §6.3).
    /// <para>
    /// A section is visible when the user can OPEN at least one of its routes - the same
    /// <see cref="RouteAccess"/> decision PageGuard enforces - and every narrowing filter passes:
    /// each <c>for</c> token "Page.Action" via <see cref="LoginState.Can(string, string)"/>, the
    /// <c>roles</c> list (AppRole names) and the <c>designations</c> list (designation names).
    /// A module with no visible section is not shown at all.
    /// </para>
    /// </summary>
    public sealed class HelpCatalog
    {
        private static readonly IReadOnlySet<string> NoneApproved = new HashSet<string>();

        private readonly HelpLibrary _library;
        private readonly LoginState _login;
        private readonly IConfiguration _config;

        public HelpCatalog(HelpLibrary library, LoginState login, IConfiguration config)
        {
            _library = library;
            _login = login;
            _config = config;
        }

        public HelpLibrary Library => _library;

        /// <summary>"module/sectionId" keys a reviewer has approved for a language (set from the API).</summary>
        public Func<string, IReadOnlySet<string>> ApprovedKeys { get; set; } = _ => NoneApproved;

        public IReadOnlyList<HelpModuleView> VisibleModules(string lang) =>
            _library.Modules
                .Select(m => Module(m, lang))
                .Where(m => m is not null)
                .Select(m => m!)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

        public HelpModuleView? Module(string module, string lang)
        {
            var guide = _library.Localized(module, lang);
            if (guide is null) return null;

            var approved = ApprovedKeys(lang) ?? NoneApproved;
            var english = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase);
            var sections = guide.Sections
                .Where(IsVisible)
                .Select(s =>
                {
                    var fallback = !english && s.Confidence == HelpConfidence.Source;
                    var machine = !english && !fallback && !approved.Contains($"{guide.Module}/{s.Id}");
                    return new HelpSectionView(guide.Module, s, lang, fallback, machine);
                })
                .ToList();

            return sections.Count == 0 ? null : new HelpModuleView(guide.Module, guide.Title, guide.Icon, guide.Order, sections);
        }

        /// <summary>
        /// The section the "?" button opens for <paramref name="path"/>: the visible section whose
        /// route matches; else the first visible section of a module with a route under the same
        /// first path segment; else null (the button then offers the Help Desk home).
        /// </summary>
        public HelpSectionView? ForPath(string path, string lang)
        {
            var modules = VisibleModules(lang);
            foreach (var module in modules)
                foreach (var s in module.Sections)
                    if (s.Section.Routes.Any(r => HelpRoutes.Matches(r, path)))
                        return s;

            var first = FirstSegment(path);
            if (first.Length == 0) return null;
            return modules
                .FirstOrDefault(m => m.Sections.Any(s => s.Section.Routes.Any(r => FirstSegment(r) == first)))
                ?.Sections.FirstOrDefault();
        }

        public IReadOnlyList<HelpSearchHit> Search(string query, string lang)
        {
            var q = (query ?? string.Empty).Trim();
            if (q.Length == 0) return Array.Empty<HelpSearchHit>();

            var hits = new List<HelpSearchHit>();
            foreach (var module in VisibleModules(lang))
            {
                foreach (var s in module.Sections)
                {
                    var text = HelpMarkdown.ToSpeech(s.Section.Title, s.Section.Body).Replace('\n', ' ');
                    var at = text.IndexOf(q, StringComparison.CurrentCultureIgnoreCase);
                    if (at < 0) continue;
                    hits.Add(new HelpSearchHit(s, Snippet(text, at)));
                    if (hits.Count == 20) return hits;
                }
            }
            return hits;
        }

        private bool IsVisible(HelpSection s)
        {
            var dataExplorerId = _config["DataExplorer:AuthorizedEmployeeId"];
            if (!s.Routes.Any(r => RouteAccess.CanOpen(_login, HelpRoutes.Sample(r), dataExplorerId))) return false;

            foreach (var token in s.For)
            {
                var dot = token.IndexOf('.');
                if (dot <= 0 || !_login.Can(token.Substring(0, dot), token.Substring(dot + 1))) return false;
            }

            if (s.Roles.Count > 0
                && !s.Roles.Any(r => string.Equals(r, _login.UserRole?.ToString(), StringComparison.OrdinalIgnoreCase)))
                return false;

            if (s.Designations.Count > 0
                && !s.Designations.Any(d => string.Equals(d.Trim(), _login.DesignationName?.Trim(), StringComparison.OrdinalIgnoreCase)))
                return false;

            return true;
        }

        private static string FirstSegment(string path) =>
            HelpRoutes.Normalize(path).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        private static string Snippet(string text, int at)
        {
            var start = Math.Max(0, at - 40);
            var length = Math.Min(120, text.Length - start);
            var snippet = text.Substring(start, length).Trim();
            return (start > 0 ? "…" : string.Empty) + snippet + (start + length < text.Length ? "…" : string.Empty);
        }
    }
}
