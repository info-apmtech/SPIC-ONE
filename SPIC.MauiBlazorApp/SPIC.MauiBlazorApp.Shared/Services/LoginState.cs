using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
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
        public string LandingPage => UserRole switch
        {
            // Admin bypasses page permissions entirely (PageAuthorization.RoleModels), so it needs no
            // designation. Lands on the same plain welcome screen as every other designation-driven
            // role - listed first so the no-designation arm below can never claim it: Admin has full
            // access with an empty RoleAccess.
            AppRole.Admin => DefaultLandingPath,
            AppRole.Dealer => "/SDWADashboard",
            AppRole.SpecialAdmin => CanAccess(PagePermission.Logistics) ? "/Logistics" : "/Welcome",
            // No designation assigned at all => /Welcome, the existing screen that tells the user to
            // contact an administrator. Same signal PageGuard uses for its own no-designation branch,
            // and keyed off the designation data rather than a role name.
            _ when AllowedPages.Count == 0 => "/Welcome",
            // Has a designation => the default landing page. It is NOT a PagePermission and is not in
            // anyone's RoleAccess: it is a common landing screen, so it must not depend on which pages
            // the designation happens to grant. A designation that omits Dashboard still lands here.
            _ => DefaultLandingPath
        };

        // Default landing page for every non-Admin role that has a designation: a plain
        // "Welcome to SPIC ONE" screen with no dashboard content. Shared with PageGuard's always-open
        // set so the route and the route that is permitted cannot drift apart.
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
        // NavMenu, MobileSidebar, PageGuard and ShellNavigation all funnel through CanAccess /
        // Can below, so they cannot drift apart. Re-resolving per call is cheap and correct even
        // though AllowedPages and UserRole both change over the session.
        private EffectivePagePermissions EffectivePermissions =>
            PageAuthorization.GetEffectivePagePermissions(UserRole, AllowedPages);

        // True when this user's role uses PageAccessModel.DesignationOnly - i.e. its effective page
        // permissions are EXACTLY the pages configured on its Designation.RoleAccess, with no
        // union with any default, role-wide or open-to-all page. Today that is AppRole.CommonRole.
        // Derived from the MODEL rather than hardcoding a role name, so a role added to
        // PageAuthorization.RoleModels as DesignationOnly is picked up here automatically, and
        // used wherever a designation-driven role needs different handling (LandingPage, and the
        // shell-hub / read-only-library short-circuits in PageGuard).
        public bool IsDesignationDrivenRole =>
            PageAuthorization.ModelFor(UserRole) == PageAccessModel.DesignationOnly;

        // Convenience alias for the one role that currently uses the designation-driven model.
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

        // Page-level: can the user REACH this page at all? Used by the route guard, menu
        // visibility and tab visibility.
        public bool CanAccess(PagePermission page) => CanAccess(page.ToString());

        public bool CanAccess(string pageKey) => EffectivePermissions.CanReach(pageKey);

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