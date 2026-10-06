using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SPIC.Core.Entities
{
    /// <summary>
    /// The authorization MODEL a role uses to resolve its page permissions.
    /// The role decides which model applies; the model decides where the answer comes from.
    /// </summary>
    public enum PageAccessModel
    {
        /// <summary>
        /// The default employee model: the pages the user's DESIGNATION grants, plus the pages
        /// the product opens to every signed-in user (<see cref="OpenToAllAttribute"/> /
        /// <see cref="PageAuthorization.OpenAccessRoutes"/>). This is the long-standing SPIC
        /// behaviour and is unchanged for every role that does not opt out of it.
        /// </summary>
        DesignationAndOpenPages = 0,

        /// <summary>
        /// The designation is the AUTHORITATIVE and ONLY source of page access. The open-to-all
        /// pages are NOT added. This is the
        /// <c>User -> Role -> Designation -> PagePermission</c> model: the effective permission
        /// set equals exactly the pages configured on the user's designation, so creating a new
        /// designation and assigning pages needs no code change at all.
        /// </summary>
        DesignationOnly = 1,

        /// <summary>
        /// The role bypasses page permissions (subject only to
        /// <see cref="PageAuthorization.DesignationOnlyPages"/>, which stay designation-only for
        /// everyone).
        /// </summary>
        RoleBypass = 2,
    }

    /// <summary>
    /// A user's RESOLVED effective page permissions for one authorization decision.
    /// Produced only by <see cref="PageAuthorization.GetEffectivePagePermissions(AppRole, IEnumerable{string})"/>,
    /// which is the single place in SPIC where "may this user reach this page?" is decided.
    /// Consumers (NavMenu, MobileSidebar, PageGuard, ShellNavigation, pages) ask this object
    /// instead of re-implementing permission logic.
    /// </summary>
    public sealed class EffectivePagePermissions
    {
        /// <summary>Which authorization model produced this set.</summary>
        public PageAccessModel Model { get; }

        /// <summary>The designation's raw "Page.Action" tokens (empty when no designation is assigned).</summary>
        public IReadOnlySet<string> Tokens { get; }

        /// <summary>Page keys granted by the designation (normalized, any action).</summary>
        public IReadOnlySet<string> DesignationPages { get; }

        internal EffectivePagePermissions(PageAccessModel model, IReadOnlySet<string> tokens)
        {
            Model = model;
            Tokens = tokens;

            var pages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
                pages.Add(RoleAccessPermissions.NormalizePageKey(RoleAccessPermissions.PagePart(token)));
            DesignationPages = pages;
        }

        /// <summary>True when the user has no designation assigned at all.</summary>
        public bool HasNoDesignation => Tokens.Count == 0;

        /// <summary>Can the user REACH this page (menu entry, route guard, tab)?</summary>
        public bool CanReach(string pageKey) => PageAuthorization.CanReach(this, pageKey);

        public bool CanReach(PagePermission page) => CanReach(RoleAccessPermissions.KeyFor(page));

        /// <summary>Can the user perform an ACTION on a page (Add / Edit / Delete buttons)?</summary>
        public bool CanPerformAction(AppRole? role, string pageKey, string action) =>
            PageAuthorization.CanPerformAction(this, role, pageKey, action);

        public bool CanPerformAction(AppRole? role, PagePermission page, string action) =>
            CanPerformAction(role, RoleAccessPermissions.KeyFor(page), action);
    }

    /// <summary>
    /// THE single, generic permission-resolution mechanism for SPIC.
    /// <para>
    /// It implements the required hierarchy <c>User -> Role -&gt; Designation -&gt; PagePermission</c>
    /// and is deliberately driven by data rather than by per-designation or per-page conditionals:
    /// <list type="number">
    /// <item>the ROLE selects the authorization MODEL (<see cref="RoleModels"/>);</item>
    /// <item>the DESIGNATION supplies the page permissions, from the existing
    /// Designation.RoleAccess mapping - no new table is required;</item>
    /// <item>the PAGE catalogue supplies each page's metadata, including which pages are open to
    /// all signed-in users (<see cref="OpenToAllAttribute"/>).</item>
    /// </list>
    /// There is deliberately no <c>if (designation == ...)</c> and no <c>if (role == X &amp;&amp; page == ...)</c>
    /// anywhere: adding a designation (DigitalLibrary, Finance, HR, ...) and assigning pages to it
    /// in Designation.razor changes the effective permission set immediately, with no change to
    /// NavMenu, PageGuard, ShellNavigation or any individual page.
    /// <para>
    /// Used by both the client (LoginState, which NavMenu / PageGuard / ShellNavigation call) and
    /// by server code via <see cref="RoleAccessPermissions"/> for the same designation token
    /// semantics, so there is one algorithm rather than two.
    /// </para>
    /// </summary>
    public static class PageAuthorization
    {
        /// <summary>Routes with no PagePermission member that every signed-in user may reach.</summary>
        public static IReadOnlySet<string> OpenAccessRoutes { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Shell hubs: a hub page only LINKS to pages the user can already open, so it is
                // safe to open to all signed-in users.
                "Activities",
                "Farmers",
                "Alerts",
                // Demo/documentation subtree. These were never designation-configurable (they have
                // no PagePermission member); give them one if they should become assignable and
                // they can leave this list.
                "DemoDocumentation",
                "StartDocumentation",
                "DemoDetails",
                "TreatmentDetails",
                "Treatment01",
                "TreatmentDemoDetails",
                "Treatment02",
                "SalesAudit",
                "ExtensionRequests",
                "FarmDashboard",
                "FarmOperations",
                "FinanceSummary",
                "FinanceSummaryOverall"
            };

        /// <summary>
        /// Pages that are ALWAYS resolved from the designation, for every role including
        /// SuperAdmin (SAS Lab and SAS payment approval - product decision 2026-09-27: an admin
        /// needs a designation that grants the key like everyone else).
        /// </summary>
        public static readonly IReadOnlySet<string> DesignationOnlyPages =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(PagePermission.LabDashboard),
                nameof(PagePermission.LabConsignments),
                nameof(PagePermission.LabAnalysis),
                nameof(PagePermission.LabReports),
                nameof(PagePermission.LabTestEntry),
                nameof(PagePermission.LabTracking),
                nameof(PagePermission.SasPaymentApproval),
                nameof(PagePermission.SasPaymentVerification)
            };

        /// <summary>All pages open to every signed-in user: enum [OpenToAll] + route-only entries.</summary>
        public static IReadOnlySet<string> OpenAccessPages { get; } = BuildOpenAccessPages();

        private static HashSet<string> BuildOpenAccessPages()
        {
            var set = new HashSet<string>(OpenAccessRoutes, StringComparer.OrdinalIgnoreCase);
            foreach (var page in Enum.GetValues<PagePermission>())
            {
                var field = typeof(PagePermission).GetField(page.ToString());
                if (field?.GetCustomAttribute<OpenToAllAttribute>() != null)
                    set.Add(RoleAccessPermissions.KeyFor(page));
            }
            return set;
        }

        /// <summary>
        /// The ONE place a role is mapped to its authorization model.
        /// <para>
        /// Roles absent from this table use the long-standing default model
        /// (<see cref="PageAccessModel.DesignationAndOpenPages"/>). To make any NEW role follow
        /// <c>Designation -> PagePermission</c> you add exactly one entry here; nothing else in the
        /// application changes.
        /// </para>
        /// </summary>
        public static IReadOnlyDictionary<AppRole, PageAccessModel> RoleModels { get; } =
            new Dictionary<AppRole, PageAccessModel>
            {
                // CommonRole: a role with no implicit page access of its own. Its effective
                // permission set is EXACTLY the pages on its designation - not a union with any
                // default, role-wide or open-to-all pages.
                [AppRole.CommonRole] = PageAccessModel.DesignationOnly,
                // Admin-family roles keep their existing bypass over page permissions.
                [AppRole.Admin] = PageAccessModel.RoleBypass,
                [AppRole.CorporateAdmin] = PageAccessModel.RoleBypass,
                [AppRole.SuperAdmin] = PageAccessModel.RoleBypass,
            };

        /// <summary>
        /// Roles that bypass ACTION-level permissions (Add / Edit / Delete buttons).
        /// <para>
        /// Deliberately NOT the same set as the page-level bypass: SuperAdmin bypasses page
        /// reachability but has NEVER bypassed actions - that asymmetry is existing SPIC
        /// behaviour and is preserved exactly. Listing it as its own table is what keeps the two
        /// decisions from drifting apart when a role is added.
        /// </para>
        /// </summary>
        public static IReadOnlySet<AppRole> ActionBypassRoles { get; } =
            new HashSet<AppRole> { AppRole.Admin, AppRole.CorporateAdmin };

        /// <summary>The authorization model that applies to a role (default when not listed).</summary>
        public static PageAccessModel ModelFor(AppRole? role) =>
            role is AppRole r && RoleModels.TryGetValue(r, out var model)
                ? model
                : PageAccessModel.DesignationAndOpenPages;

        /// <summary>True when the role bypasses action-level permissions (page bypass is separate).</summary>
        public static bool HasActionBypass(AppRole? role) =>
            role is AppRole r && ActionBypassRoles.Contains(r);

        /// <summary>
        /// Resolves a user's effective page permissions from the inputs that already exist:
        /// their ROLE (which selects the model) and their DESIGNATION's RoleAccess tokens.
        /// This is the single reusable resolution entry point for the whole application.
        /// </summary>
        public static EffectivePagePermissions GetEffectivePagePermissions(
            AppRole? role, IEnumerable<string>? designationTokens) =>
            new(ModelFor(role), new HashSet<string>(designationTokens ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Resolves a user's effective page permissions directly from a designation's RoleAccess
        /// CSV, the same way the server does, so client and server resolve identically.
        /// </summary>
        public static EffectivePagePermissions GetEffectivePagePermissions(
            AppRole? role, string? designationRoleAccessCsv) =>
            new(ModelFor(role), RoleAccessPermissions.ParseTokens(designationRoleAccessCsv));

        /// <summary>
        /// The single page-level decision. Callers should reach this via
        /// <see cref="EffectivePagePermissions.CanReach(string)"/>.
        /// </summary>
        public static bool CanReach(EffectivePagePermissions permissions, string pageKey)
        {
            if (permissions is null) return false;

            // 1. Designation-only pages (SAS Lab / payments): resolved from the designation for
            //    EVERY role, with no role bypass.
            if (DesignationOnlyPages.Contains(pageKey))
                return HasPageStrict(permissions, pageKey);

            // 2. Pages the product opens to every signed-in user - the long-standing behaviour,
            //    preserved for every role whose model includes them. A designation-driven role
            //    (CommonRole) skips this step, so these pages are reachable for it ONLY when its
            //    designation grants them. No per-page or per-designation conditional exists: the
            //    set comes from the PagePermission catalogue and PageAuthorization data.
            if (OpenAccessPages.Contains(pageKey) && permissions.Model != PageAccessModel.DesignationOnly)
                return true;

            // 3. Role bypass (Admin / CorporateAdmin / SuperAdmin) over remaining pages.
            if (permissions.Model == PageAccessModel.RoleBypass)
                return true;

            // 4. No designation assigned => the Welcome page only.
            if (permissions.HasNoDesignation)
                return IsWelcome(pageKey);

            // 5. Otherwise the designation decides.
            return permissions.DesignationPages.Contains(RoleAccessPermissions.NormalizePageKey(pageKey));
        }

        /// <summary>
        /// The single action-level decision. Note this is deliberately NOT affected by the
        /// open-to-all pages: those are page-reachability only, so an action still requires a
        /// real "Page.Action" token - preserving existing behaviour exactly.
        /// </summary>
        public static bool CanPerformAction(
            EffectivePagePermissions permissions, AppRole? role, string pageKey, string action)
        {
            if (permissions is null) return false;

            // Admin / CorporateAdmin bypass actions, except on the designation-only pages.
            // SuperAdmin is intentionally absent from ActionBypassRoles: it bypasses page
            // reachability but has never bypassed actions.
            if (HasActionBypass(role) && !DesignationOnlyPages.Contains(pageKey))
                return true;

            if (permissions.HasNoDesignation) return false;

            var wantedPage = RoleAccessPermissions.NormalizePageKey(pageKey);
            foreach (var token in permissions.Tokens)
            {
                var dot = token.IndexOf('.');
                var tokenPage = dot < 0 ? token : token.Substring(0, dot);
                if (!string.Equals(RoleAccessPermissions.NormalizePageKey(tokenPage), wantedPage, StringComparison.OrdinalIgnoreCase))
                    continue;
                // A bare legacy page token grants every action on that page (existing behaviour).
                if (dot < 0) return true;
                if (string.Equals(token.Substring(dot + 1), action, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// RAW designation matching for ANY page key: true when the user's designation holds any
        /// token for that page, with no role bypass and no open-to-all grant and WITHOUT key
        /// normalization.
        /// <para>
        /// This is the single implementation of what LoginState.HasPageStrict has always meant. It
        /// deliberately does NOT consult the authorization model, so it stays valid for every page
        /// key (not just the designation-only SAS Lab / payment pages that use it today) and it
        /// never changes meaning for an existing role.
        /// </para>
        /// </summary>
        public static bool HasPageStrict(EffectivePagePermissions permissions, string pageKey)
        {
            if (permissions is null) return false;
            foreach (var token in permissions.Tokens)
            {
                if (string.Equals(RoleAccessPermissions.PagePart(token), pageKey, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsWelcome(string pageKey) =>
            string.Equals(pageKey, "Welcome", StringComparison.OrdinalIgnoreCase);
    }
}
