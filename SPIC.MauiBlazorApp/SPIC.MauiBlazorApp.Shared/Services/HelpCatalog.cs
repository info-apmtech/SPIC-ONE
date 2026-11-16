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

    public sealed record HelpModuleView(string Module, string Title, string Icon, int Order, IReadOnlyList<HelpSectionView> Sections)
    {
        /// <summary>The localized guide (category, colour, example, updated, note).</summary>
        public HelpGuide Guide { get; init; } = null!;

        /// <summary>The screen sections the user can open, in guide order.</summary>
        public IReadOnlyList<HelpSectionView> Screens => Sections.Where(s => s.Section.Kind == "screen").ToList();

        /// <summary>The module's block of a kind (overview, flow, statuses, roles, faq), or null.</summary>
        public HelpSectionView? Block(string kind) => Sections.FirstOrDefault(s => s.Section.Kind == kind);

        /// <summary>The overview's lead paragraph: the text under the banner title.</summary>
        public string Lead => Block("overview") is { } o ? HelpBlocks.Lead(o.Section.Body) : string.Empty;
    }

    /// <summary>"Your path" for the signed-in user: steps limited to screens they can open.</summary>
    public sealed record HelpPathView(string Title, IReadOnlyList<(HelpPathStep Step, HelpSectionView Section)> Steps);

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

        public static string ApprovalKey(string module, string sectionId, string translationHash) =>
            HelpReviewStatus.ApprovalKey(module, sectionId, translationHash);

        /// <summary>Approval keys (<see cref="ApprovalKey"/>: module/sectionId/translationHash) a reviewer
        /// has approved for a language (set from the API).</summary>
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
            // Blocks (overview, flow, roles…) follow the module: shown only when a screen is.
            var anyScreen = guide.Sections.Any(s => s.Kind == "screen" && IsVisible(s));
            if (!anyScreen) return null;
            var sections = guide.Sections
                .Where(s => s.Kind != "screen" || IsVisible(s))
                .Select(s =>
                {
                    var fallback = !english && s.Confidence == HelpConfidence.Source;
                    // Approved only for the exact translated text shown here (an older app build may
                    // carry older text than the one a reviewer approved).
                    var machine = !english && !fallback
                                  && !approved.Contains(ApprovalKey(guide.Module, s.Id, HelpHash.Of(s.Title, s.Body)));
                    return new HelpSectionView(guide.Module, s, lang, fallback, machine);
                })
                .ToList();

            return new HelpModuleView(guide.Module, guide.Title, guide.Icon, guide.Order, sections) { Guide = guide };
        }

        /// <summary>
        /// The section the "?" button opens for <paramref name="path"/>: the visible section whose
        /// route matches; else the first visible section of a module with a route under the same
        /// first path segment; else null (the button then offers the Help Desk home).
        /// </summary>
        public HelpSectionView? ForPath(string path, string lang)
        {
            var modules = VisibleModules(lang);

            // The most specific match wins: /PublicityReview/consolidation beats /PublicityReview/{id}.
            var best = modules
                .SelectMany(m => m.Sections)
                .Select(s => (Section: s, Score: s.Section.Routes.Where(r => HelpRoutes.Matches(r, path)).Select(HelpRoutes.LiteralSegments).DefaultIfEmpty(-1).Max()))
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Section)
                .FirstOrDefault();
            if (best is not null) return best;

            var first = FirstSegment(path);
            if (first.Length == 0) return null;
            return modules
                .FirstOrDefault(m => m.Screens.Any(s => s.Section.Routes.Any(r => FirstSegment(r) == first)))
                ?.Screens.FirstOrDefault();
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
                    var text = HelpBlocks.SpeechText(s.Section).Replace('\n', ' ');
                    var at = text.IndexOf(q, StringComparison.CurrentCultureIgnoreCase);
                    if (at < 0) continue;
                    hits.Add(new HelpSearchHit(s, Snippet(text, at)));
                    if (hits.Count == 20) return hits;
                }
            }
            return hits;
        }

        /// <summary>FAQ items marked popular, from modules the user can see.</summary>
        public IReadOnlyList<(string Module, HelpFaqItem Item)> PopularFaq(string lang, int max = 6) =>
            VisibleModules(lang)
                .SelectMany(m => m.Block("faq") is { } faq
                    ? HelpBlocks.Faq(faq.Section, new List<string>()).Where(f => f.Popular).Select(f => (m.Module, f))
                    : Enumerable.Empty<(string, HelpFaqItem)>())
                .Take(max)
                .ToList();

        /// <summary>The first path for the user's designation or role, keeping only steps they can open; null if none.</summary>
        public HelpPathView? PathFor(string lang)
        {
            if (!_login.IsLoggedIn) return null;
            var path = _library.Paths.FirstOrDefault(p =>
                p.Designations.Any(d => SameName(d, _login.DesignationName)) || p.Roles.Any(r => SameName(r, _login.UserRole?.ToString())));
            if (path is null) return null;

            var modules = VisibleModules(lang).ToDictionary(m => m.Module, StringComparer.OrdinalIgnoreCase);
            var steps = new List<(HelpPathStep, HelpSectionView)>();
            foreach (var step in path.Steps)
                if (modules.TryGetValue(step.Module, out var m) && m.Screens.FirstOrDefault(s => SameName(s.Section.Id, step.Section)) is { } view)
                    steps.Add((step, view));
            return steps.Count == 0 ? null : new HelpPathView(path.TitleIn(lang), steps);
        }

        /// <summary>Latest guide updates among the modules the user can see.</summary>
        public IReadOnlyList<HelpWhatsNewItem> WhatsNew(string lang, int max = 5) =>
            HelpWhatsNew.From(VisibleModules(lang).Select(m => m.Guide), max);

        /// <summary>The roles-block card for the user: designation match first, then role. Null when none.</summary>
        public string? MyRoleCard(HelpModuleView module)
        {
            if (module.Block("roles") is not { } roles) return null;
            var cards = HelpBlocks.Roles(roles.Section, new List<string>());
            return (cards.FirstOrDefault(c => c.Designations.Any(d => SameName(d, _login.DesignationName)))
                    ?? cards.FirstOrDefault(c => c.Roles.Any(r => SameName(r, _login.UserRole?.ToString()))))?.Id;
        }

        /// <summary>The flow lane for the user: designation match first, then role. Null when none.</summary>
        public string? MyLane(HelpModuleView module)
        {
            if (module.Block("flow") is not { } flow) return null;
            var lanes = HelpBlocks.Flow(flow.Section, new List<string>()).Lanes;
            return (lanes.FirstOrDefault(l => l.Designations.Any(d => SameName(d, _login.DesignationName)))
                    ?? lanes.FirstOrDefault(l => l.Roles.Any(r => SameName(r, _login.UserRole?.ToString()))))?.Id;
        }

        private static bool SameName(string? a, string? b) =>
            !string.IsNullOrWhiteSpace(a) && string.Equals(a.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

        private bool IsVisible(HelpSection s)
        {
            if (!_login.IsLoggedIn) return false; // RouteAccess allows everything when signed out
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
