using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    public class LoginState
    {
        public bool IsBusy { get; set; }
        public string? ErrorMessage { get; set; }

        private string? _token;
        public DateTime Expiration { get; set; }
        public bool IsTokenExpired => Expiration != default && DateTime.UtcNow >= Expiration.ToUniversalTime();

        /// <summary>Where a signed-in user lands: used after login and when a stored session is restored.</summary>
        public string LandingPage
        {
            get
            {
                // Admin and SuperAdmin do NOT use Designation-based landing-page logic: they
                // always land on DefaultWelcome and keep their existing authorized pages.
                if (UserRole is AppRole.Admin or AppRole.SuperAdmin)
                    return DefaultLandingPath;

                // Every other role lands on DefaultWelcome ONLY when it actually holds a
                // Designation. "Has a designation" is the same signal PageGuard uses to decide
                // that a user has no designation (AllowedPages == the Designation.RoleAccess
                // snapshot), so the landing route and the route authorization can never disagree.
                // Without a designation the user keeps the existing Welcome page, and
                // DefaultWelcome is never shown.
                return AllowedPages.Count > 0 ? DefaultLandingPath : "/Welcome";
            }
        }

        // Default landing page for Admin / SuperAdmin and for every non-Admin role that has a
        // designation: a plain "Welcome to SPIC ONE" screen with no dashboard content. Shared with
        // PageGuard's always-open set so the route and the route that is permitted cannot drift apart.
        public const string DefaultLandingPath = "/DefaultWelcome";

        public event Action? OnChange;

        public string? Token
        {
            get => _token;
            set
            {
                _token = value;
                ParseClaims(value);
                OnChange?.Invoke();
            }
        }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

        public AppRole? UserRole { get; private set; }
        public int StateId { get; private set; }
        public int RegionId { get; private set; }
        public int HQId { get; private set; }
        public int EmployeeId { get; private set; }

        // SpecialAdmin-only multi-location scope. Populated from the
        // spic:assigned_* claims (database-backed at login). For every other role
        // these stay empty and existing single-location properties are unchanged.
        public List<int> AssignedStateIds { get; private set; } = new();
        public List<int> AssignedRegionIds { get; private set; } = new();
        public List<int> AssignedHeadquarterIds { get; private set; } = new();

        // True when the user is a SpecialAdmin with at least one assigned state.
        public bool IsSpecialAdminWithAssignments =>
            UserRole == AppRole.SpecialAdmin && AssignedStateIds.Count > 0;

        // Current authenticated user id (from token claims)
        public string? UserId { get; private set; }

        // ---------------- Designation (separate from AppRole) ----------------
        // Resolved server-side from UserInfo.DesignationId -> Designation.Name at
        // login (AuthenticationController). Kept independent of AppRole: a user's
        // AppRole (e.g. CorporateAdmin) never changes based on their Designation.
        public string? DesignationName { get; private set; }

        public void SetDesignationName(string? designationName)
        {
            DesignationName = string.IsNullOrWhiteSpace(designationName) ? null : designationName.Trim();
            OnChange?.Invoke();
        }

        public bool IsAdmin => UserRole is AppRole.Admin or AppRole.SuperAdmin or AppRole.CorporateAdmin or AppRole.Director or AppRole.AVP;

        // Single source of truth for the whole application: SPIC.Core's PageAuthorization.
        // PageGuard, the shell's CanSeeMenu and the page-level Can checks all funnel through this
        // one resolution, so they cannot drift apart. Re-resolving per call is cheap and correct
        // even though AllowedPages and UserRole both change over the session.
        private EffectivePagePermissions EffectivePermissions =>
            PageAuthorization.GetEffectivePagePermissions(UserRole, AllowedPages);

        // True when this user's role uses PageAccessModel.DesignationOnly - i.e. its effective page
        // permissions are EXACTLY the pages configured on its Designation.RoleAccess, with no
        // union with any default, role-wide or open-to-all page. Today that is AppRole.CommonRole
        // and AppRole.SpecialAdmin. Derived from the MODEL rather than hardcoding a role name, so a
        // role added to PageAuthorization.RoleModels as DesignationOnly is picked up here
        // automatically, and used wherever a designation-driven role needs different handling
        // (the shell-hub / read-only-library short-circuits in PageGuard).
        public bool IsDesignationDrivenRole =>
            PageAuthorization.ModelFor(UserRole) == PageAccessModel.DesignationOnly;

        // Legacy name for the same concept; kept because the phrase reads better in existing call
        // sites. It means "designation-driven role", not "AppRole.CommonRole".
        public bool IsCommonRole => IsDesignationDrivenRole;

        // SAS Lab / payment pages resolved through the DESIGNATION only (product decision 2026-09-27):
        // the Admin / CorporateAdmin roles do NOT bypass these. Owned by PageAuthorization now;
        // exposed here for the existing callers (PageGuard, and any page).
        public static IReadOnlySet<string> DesignationOnlyPages => PageAuthorization.DesignationOnlyPages;

        public bool IsStateRole => UserRole is AppRole.SMD or AppRole.SMM;
        public bool IsRegionRole => UserRole is AppRole.RM or AppRole.RMD;
        public bool IsHQRole => UserRole is AppRole.MO or AppRole.MDO or AppRole.JMDO;

        // Specific role group helpers
        public bool IsHQCreatorRole => UserRole is AppRole.MO or AppRole.MDO or AppRole.JMDO;
        public bool IsRMGroup => UserRole is AppRole.RM or AppRole.RMD;
        public bool IsSMGroup => UserRole is AppRole.SMD or AppRole.SMM;
        public bool IsDirectorOrAVP => UserRole is AppRole.Director or AppRole.AVP;
        public bool IsReviewerRole => UserRole is AppRole.Admin or AppRole.CorporateAdmin or AppRole.Director or AppRole.AVP or AppRole.SMD or AppRole.SMM or AppRole.RM or AppRole.RMD;

        // ---------------- Page-level permissions (from Designation.RoleAccess) ----------------

        // Empty set => user has no designation => Welcome page only (no feature pages).
        public HashSet<string> AllowedPages { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public void SetAllowedPages(string? roleAccessCsv)
        {
            AllowedPages = string.IsNullOrWhiteSpace(roleAccessCsv)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : roleAccessCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);
            OnChange?.Invoke();
        }

        public void ClearAllowedPages()
        {
            AllowedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            OnChange?.Invoke();
        }

        public void Logout()
        {
            ClearAllowedPages();
            SetDesignationName(null);
            Token = null; // triggers ParseClaims(null) + OnChange
        }

        // Returns the page part of a token: "Register.View" -> "Register".
        // A bare legacy token ("Register") returns itself.
        private static string PagePart(string token)
        {
            var dot = token.IndexOf('.');
            return dot < 0 ? token : token.Substring(0, dot);
        }

        private static List<int> ParseCsv(string? csv)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(csv)) return result;
            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var id) && id > 0 && !result.Contains(id))
                    result.Add(id);
            }
            return result;
        }

        // ---- Single permission decision (SPIC.Core.Entities.PageAuthorization) -------------
        // These four members are the ONLY entry points used by NavMenu, MobileSidebar, PageGuard,
        // ShellNavigation and individual pages. All policy lives in PageAuthorization, so adding a
        // designation or assigning pages to one needs no change in any of those components.

        // Raw designation matching: any token for the page, no role bypass, no open-to-all grant,
        // no key normalization. Drives the SAS Lab / payment pages today; semantics unchanged
        // from the pre-existing implementation.
        public bool HasPageStrict(PagePermission page) => HasPageStrict(page.ToString());

        public bool HasPageStrict(string pageKey) =>
            PageAuthorization.HasPageStrict(EffectivePermissions, pageKey);

        // Page-level: can the user REACH this page at all? Used by the route guard (PageGuard),
        // page-internal feature checks and the API. Menu / tab visibility uses CanSeeMenu below.
        public bool CanAccess(PagePermission page) => CanAccess(page.ToString());

        public bool CanAccess(string pageKey) => EffectivePermissions.CanReach(pageKey);

        // MENU VISIBILITY ONLY. Never used by PageGuard, the API, or action checks - those keep
        // using CanAccess / HasPageStrict exactly as before.
        //
        // CanAccess answers "may this user REACH this page?". PageAuthorization.OpenAccessRoutes
        // and the enum's [OpenToAll] grant deliberately open a few keys for every signed-in user,
        // which is correct for the route but wrong for a menu entry: a route-open key is ALSO an
        // assignable PagePermission member in the Designation grid, so the open grant would light
        // up a menu an administrator can uncheck. CanSeeMenu asks only "should this menu render?",
        // so the designation decides instead of the open grant or the role.
        //
        // The key MUST be normalized first: HasPageStrict compares the RAW token page part
        // against the key, and Designation.RoleAccess stores QRScanner as "qr-scanner". Passing
        // the raw enum name would hide the QR Scanner menu from every designation that grants it.
        //
        // The SAS Lab / SAS payment keys are checked BEFORE the Admin / SuperAdmin arm because
        // they are designation-only for every role (product decision 2026-09-27) and PageGuard
        // enforces the same rule on the route - returning true here would render a menu the guard
        // then bounces away from.
        public bool CanSeeMenu(PagePermission page) => CanSeeMenu(RoleAccessPermissions.KeyFor(page));

        public bool CanSeeMenu(string pageKey)
        {
            var key = RoleAccessPermissions.NormalizePageKey(pageKey);

            if (PageAuthorization.DesignationOnlyPages.Contains(key))
                return HasPageStrict(key);

            if (UserRole is AppRole.Admin or AppRole.SuperAdmin)
                return true;

            return HasPageStrict(key);
        }

        // Action-level: can the user perform a specific action on a page?
        // Use inside pages to show/hide Add / Edit / Delete buttons.
        // e.g. LoginState.Can("Register", "Update")
        public bool Can(PagePermission page, string action) => Can(RoleAccessPermissions.KeyFor(page), action);

        public bool Can(string pageKey, string action) => EffectivePermissions.CanPerformAction(UserRole, pageKey, action);

        // -------------------------------------------------------------------------------------

        private void ParseClaims(string? token)
        {
            UserRole = null;
            StateId = 0;
            RegionId = 0;
            HQId = 0;
            AssignedStateIds = new List<int>();
            AssignedRegionIds = new List<int>();
            AssignedHeadquarterIds = new List<int>();

            if (string.IsNullOrWhiteSpace(token)) return;

            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return;

                var payload = parts[1];
                // Fix base64url padding
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                payload = payload.Replace('-', '+').Replace('_', '/');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Role claim (may be "role" or the long ClaimTypes.Role URI)
                string? roleClaim = null;
                if (root.TryGetProperty("role", out var rp)) roleClaim = rp.GetString();
                else if (root.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var rp2)) roleClaim = rp2.GetString();

                if (roleClaim != null && Enum.TryParse<AppRole>(roleClaim, out var role))
                    UserRole = role;

                if (root.TryGetProperty("spic:state_id", out var sp) && int.TryParse(sp.GetString(), out var sid))
                    StateId = sid;
                if (root.TryGetProperty("spic:region_id", out var rip) && int.TryParse(rip.GetString(), out var rid))
                    RegionId = rid;
                if (root.TryGetProperty("spic:hq_id", out var hp) && int.TryParse(hp.GetString(), out var hid))
                    HQId = hid;

                // SpecialAdmin multi-location scope (comma-separated claim values)
                if (root.TryGetProperty("spic:assigned_state_ids", out var sasp))
                    AssignedStateIds = ParseCsv(sasp.GetString());
                if (root.TryGetProperty("spic:assigned_region_ids", out var rasp))
                    AssignedRegionIds = ParseCsv(rasp.GetString());
                if (root.TryGetProperty("spic:assigned_hq_ids", out var hasp))
                    AssignedHeadquarterIds = ParseCsv(hasp.GetString());

                // Try to parse user identifier from common claim names
                string? uid = null;
                if (root.TryGetProperty("sub", out var sub)) uid = sub.GetString();
                if (string.IsNullOrEmpty(uid) && root.TryGetProperty("nameid", out var nameid)) uid = nameid.GetString();
                if (string.IsNullOrEmpty(uid) && root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out var nid)) uid = nid.GetString();
                if (string.IsNullOrEmpty(uid) && root.TryGetProperty("spic:user_id", out var sup)) uid = sup.GetString();
                UserId = uid;
            }
            catch { }
        }
    }
}
