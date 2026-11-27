using System;
using System.Collections.Generic;
using System.Linq;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>The outcome of a route check: allowed, or where the guard sends the user instead.</summary>
    public sealed record RouteDecision(bool Allowed, string? RedirectTo);

    /// <summary>
    /// "May this signed-in user open this URL?" - the decision PageGuard enforces on every
    /// navigation, moved here VERBATIM (2026-11-16) so the Help Desk can ask the same question
    /// without a second copy of the rules. PageGuard now only applies the redirect this returns.
    /// <para>
    /// Behaviour is pinned by SPIC.RegressionHarness/RouteAccessTests. Change a rule here and
    /// you change both the route guard and which help guides a user sees.
    /// </para>
    /// </summary>
    public static class RouteAccess
    {
        private static readonly RouteDecision Allow = new(true, null);

        private static RouteDecision Redirect(string to) => new(false, to);

        internal static readonly HashSet<string> AlwaysOpen = new(StringComparer.OrdinalIgnoreCase)
        {
            "",
            "login",
            "AccountSelector",
            "error",
            "notfound",
            "forbidden",
            "Welcome",
            // Default landing page (LoginState). It is the destination of LandingPage for Admin /
            // SuperAdmin and for every non-Admin role that HAS a designation, so it must be open to
            // those users; otherwise the redirect at the bottom of Decide() sends the user back to
            // LandingPage - the very route - and loops. This is a common landing screen, NOT a
            // designation permission: it is not a PagePermission member and appears in no RoleAccess.
            // A user with no designation is redirected to /Welcome by the no-designation branch below.
            // Listed without the leading slash.
            LoginState.DefaultLandingPath.TrimStart('/'),
            // Help Desk (2026-11-16): every signed-in user with a designation, every role, including
            // the designation-only ones (Lab staff, CommonRole, SpecialAdmin) that skip the open-to-all
            // pages. The guides themselves are filtered per user by HelpCatalog. Users with no
            // designation are still sent to /Welcome (see HelpRoute in the no-designation branch).
            HelpRoute
        };

        private const string HelpRoute = "Help";

        // Shell hub pages (phone tab bar / top bar destinations). Each hub only LINKS to pages the
        // user can already open (every link is filtered by LoginState.CanAccess), so they are open to
        // every signed-in user who has a designation - EXCEPT a designation-driven role
        // (PageAccessModel.DesignationOnly: CommonRole, SpecialAdmin), for which the short-circuit
        // below is skipped and the hub itself must be on its designation.
        // Product decision 2026-09-20: the Knowledge Community is usable by everyone, and Digital
        // Library CONTENT is viewable by everyone; only adding/editing content (/DigitalLibrary/add/...)
        // needs the DigitalLibrary page in the user's designation (the API enforces the same rule).
        internal static readonly HashSet<string> ShellHubs = new(StringComparer.OrdinalIgnoreCase)
        {
            "Activities",
            "Farmers",
            "Alerts",
            "Community"
        };

        internal const string FarmerSegment = "farmer";

        internal const string CommunitySegment = "Community";

        /// <summary>The staff page for farmer call-back requests (/farmer-requests).</summary>
        public const string FarmerRequestsSegment = "farmer-requests";

        // Pages a farmer WITHOUT a designation may still open outside /farmer: the sign-in plumbing
        // and their own SAS samples / reports (the API scopes those to their linked farmer record).
        private static readonly HashSet<string> FarmerInfraRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "", "login", "AccountSelector", "error", "notfound", "forbidden"
        };

        private static RouteDecision? FarmerRoute(LoginState login, string firstSegment, string secondSegment)
        {
            if (firstSegment.Equals(FarmerSegment, StringComparison.OrdinalIgnoreCase))
            {
                if (secondSegment.Equals("login", StringComparison.OrdinalIgnoreCase)) return Allow;
                return login.UserRole is AppRole.Farmer or AppRole.Admin ? Allow : Redirect(login.LandingPage);
            }

            if (login.UserRole != AppRole.Farmer) return null;

            if (firstSegment.Equals("Sas", StringComparison.OrdinalIgnoreCase)
                && (secondSegment.Equals("MySamples", StringComparison.OrdinalIgnoreCase)
                    || secondSegment.Equals("MyReports", StringComparison.OrdinalIgnoreCase)))
                return Allow;

            // The Knowledge Community is open to every signed-in user (product decision 2026-09-20,
            // PageAuthorization.OpenAccessPages); a farmer reaches it from "Community near me"
            // (farmer portal Task 3): the list, a discussion, asking a question, the posted page.
            if (firstSegment.Equals(CommunitySegment, StringComparison.OrdinalIgnoreCase))
                return Allow;

            // A farmer with no designation never sees the staff Welcome / dashboard pages: anything
            // else goes to the farmer home. A farmer WITH a designation (older SAS accounts) keeps
            // the normal rules below; their LandingPage is the farmer home too.
            if (login.AllowedPages.Count == 0)
                return FarmerInfraRoutes.Contains(firstSegment) ? Allow : Redirect(LoginState.FarmerLandingPath);

            return null;
        }

        /// <summary>True when the user may open <paramref name="url"/> (absolute or relative).</summary>
        public static bool CanOpen(LoginState login, string url, string? dataExplorerAuthorizedId) =>
            Decide(login, url, dataExplorerAuthorizedId).Allowed;

        /// <summary>
        /// The route decision for <paramref name="url"/>. <paramref name="dataExplorerAuthorizedId"/>
        /// is the configured DataExplorer:AuthorizedEmployeeId.
        /// </summary>
        public static RouteDecision Decide(LoginState LoginState, string url, string? dataExplorerAuthorizedId)
        {
            if (!LoginState.IsLoggedIn) return Allow;

            var path = (new Uri(url, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                    ? new Uri(url).AbsolutePath
                    : url)
                .Split('?', '#')[0]
                .TrimStart('/')
                .TrimEnd('/');

            // Match on the FIRST path segment only, so routes WITH parameters
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var firstSegment = segments.FirstOrDefault() ?? "";
            var secondSegment = segments.Skip(1).FirstOrDefault() ?? "";

            // SuperAdmin bypasses everything else.
            if (LoginState.UserRole == AppRole.SuperAdmin) return Allow;

            // Farmer portal (docs/farmer-portal.md): gated by ROLE, not designation, so open sign-up
            // works without any designation data. /farmer/login is the anonymous sign-in page (a
            // signed-in user opening it is sent on by the page itself). Admin (and SuperAdmin above)
            // may open the portal for support; every staff role is sent back to its own home.
            var farmerRoute = FarmerRoute(LoginState, firstSegment, secondSegment);
            if (farmerRoute is not null) return farmerRoute;

            // Data Explorer: SuperAdmin or authorized CorporateAdmin only. Admin is strictly denied.
            bool isDataExplorer = firstSegment.Equals("DataExplorer", StringComparison.OrdinalIgnoreCase);
            bool isDataExplorerAuthorized = isDataExplorer
                && SPIC.Core.DTOs.DataExplorerAuthorization.IsAuthorized(LoginState.UserRole, LoginState.UserId, dataExplorerAuthorizedId);

            if (isDataExplorer && !isDataExplorerAuthorized)
            {
                return Redirect(LoginState.LandingPage);
            }

            // Admin bypasses everything EXCEPT the SAS Lab (/Lab/...), payment (/Sas/Payments...),
            // and Data Explorer (/DataExplorer) routes.
            bool designationOnlyRoute = firstSegment.Equals("Lab", StringComparison.OrdinalIgnoreCase)
                || (firstSegment.Equals("Sas", StringComparison.OrdinalIgnoreCase)
                    && secondSegment.Equals("Payments", StringComparison.OrdinalIgnoreCase))
                || isDataExplorer;
            if (LoginState.UserRole is AppRole.Admin && !designationOnlyRoute) return Allow;

            // Dealers can always access the SDWA Dashboard and SDWA welfare pages,
            // but must NEVER reach the SchemeApproval page.
            if (LoginState.UserRole == AppRole.Dealer)
            {
                if (firstSegment.Equals("SchemeApproval", StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect("/SDWADashboard");
                }

                if (firstSegment.Equals("SDWADashboard", StringComparison.OrdinalIgnoreCase)
                    || firstSegment.Equals("WelfareSchemes", StringComparison.OrdinalIgnoreCase)
                    || firstSegment.Equals("ApplyWelfareScheme", StringComparison.OrdinalIgnoreCase)
                    || firstSegment.Equals("WellfareApplication", StringComparison.OrdinalIgnoreCase))
                {
                    return Allow;
                }
            }

            // Director (existing AppRole) needs to reach SchemeApproval / ApprovalDetail
            // for the OPTIONAL post-SDWA Director review, independent of whatever
            // Designation.RoleAccess they may or may not hold - this is an AppRole-based
            // capability, not a Designation/PagePermission one (see WelfareSchemeApprovalController.
            // ViewerRoles, which already grants Director read access to every application).
            bool isDirectorSchemeApproval = LoginState.UserRole == AppRole.Director
                && (firstSegment.Equals("SchemeApproval", StringComparison.OrdinalIgnoreCase)
                    || firstSegment.Equals("ApprovalDetail", StringComparison.OrdinalIgnoreCase)
                    || firstSegment.Equals("MOApproval", StringComparison.OrdinalIgnoreCase));

            // Users with NO designation assigned (no allowed pages) can ONLY see the
            // Welcome page. Infrastructure pages (login / AccountSelector / error /
            // notfound / forbidden) stay reachable so the login and multi-role flows
            // still work, but Dashboard and every feature page are blocked.
            // Special exception: the authorized CorporateAdmin may reach DataExplorer.
            if (LoginState.AllowedPages.Count == 0)
            {
                bool isWelcome = firstSegment.Equals("Welcome", StringComparison.OrdinalIgnoreCase);
                bool isDealerDash = LoginState.UserRole == AppRole.Dealer
                                    && (firstSegment.Equals("SDWADashboard", StringComparison.OrdinalIgnoreCase)
                                        || firstSegment.Equals("WelfareSchemes", StringComparison.OrdinalIgnoreCase)
                                        || firstSegment.Equals("ApplyWelfareScheme", StringComparison.OrdinalIgnoreCase)
                                        || firstSegment.Equals("WellfareApplication", StringComparison.OrdinalIgnoreCase));
                // The default landing route is in AlwaysOpen so users WITH a designation (and Admin,
                // who bypasses this branch entirely) can reach it, so it is excluded here to keep this
                // branch's existing behaviour: a user with no designation still lands on /Welcome, the
                // screen telling them to contact an administrator. /Welcome is allowed, so this cannot loop.
                bool isDefaultLanding = firstSegment.Equals(
                    LoginState.DefaultLandingPath.TrimStart('/'), StringComparison.OrdinalIgnoreCase);

                bool isInfra = AlwaysOpen.Contains(firstSegment)
                               && !firstSegment.Equals("Dashboard", StringComparison.OrdinalIgnoreCase)
                               && !firstSegment.Equals(HelpRoute, StringComparison.OrdinalIgnoreCase)
                               && !isDefaultLanding;

                if (!isWelcome && !isDealerDash && !isInfra && !isDirectorSchemeApproval && !isDataExplorerAuthorized)
                {
                    return Redirect("/Welcome");
                }
                return Allow;
            }

            if (isDataExplorerAuthorized) return Allow;

            // Farmer call-back requests (farmer portal Task 2): a ROLE-gated staff page for the people
            // farmers can ask (JMDO / MDO / RM / RMD; Admin / SuperAdmin returned Allow above). No
            // PagePermission: the API answers by role and scopes every row to the caller.
            if (firstSegment.Equals(FarmerRequestsSegment, StringComparison.OrdinalIgnoreCase))
                return SPIC.Core.FarmerPortal.FarmerContactRequestRules.CanUseStaffPage(LoginState.UserRole)
                    ? Allow
                    : Redirect(LoginState.LandingPage);

            if (AlwaysOpen.Contains(firstSegment)) return Allow;
            // Shell hubs and the read-only Digital Library are open to every signed-in user for the
            // default employee roles. That grant now lives in PageAuthorization.OpenAccessPages
            // instead of in these two short-circuits, so a designation-driven role is NOT let through
            // here and falls through to the shared CanAccess check below - its hubs, like every other
            // page, come from its designation. No per-page or per-designation conditional: the model
            // is what PageGuard asks about.
            if (ShellHubs.Contains(firstSegment) && !LoginState.IsDesignationDrivenRole) return Allow;
            // /DigitalLibrary/add/... (content wizard) and /DigitalLibrary/categories (category master)
            // are admin pages: they fall through to the generic DigitalLibrary page check below.
            if (firstSegment.Equals("DigitalLibrary", StringComparison.OrdinalIgnoreCase)
                && !LoginState.IsDesignationDrivenRole
                && !secondSegment.Equals("add", StringComparison.OrdinalIgnoreCase)
                && !secondSegment.Equals("categories", StringComparison.OrdinalIgnoreCase))
            {
                return Allow;
            }

            // ApprovalDetail (and its legacy MOApproval alias) is a dedicated
            // full-page route for approval details shared by all roles;
            // it uses the same SchemeApproval permission.
            // SAS Lab portal and Payment Approval (2026-09-27): routes live under one prefix and map
            // to a page key by their second segment. Farmer pages under /Sas use the v1 keys.
            // The lab / payment keys are DESIGNATION only (HasPageStrict): the Admin / CorporateAdmin
            // roles do not bypass them (product decision 2026-09-27).
            if (firstSegment.Equals("Lab", StringComparison.OrdinalIgnoreCase))
            {
                firstSegment = secondSegment.ToLowerInvariant() switch
                {
                    "" => "LabDashboard",
                    "consignments" => "LabConsignments",
                    "analysis" => "LabAnalysis",
                    "reports" => "LabReports",
                    "testentry" => "LabTestEntry",
                    "tracking" => "LabTracking",
                    _ => "LabDashboard"
                };
                // Coordinators (LabAnalysis) may open a sample's entry page read-only from Batch Details.
                if (firstSegment == "LabTestEntry" && LoginState.HasPageStrict("LabAnalysis")) return Allow;
                // The lab dashboard opens to anyone holding any lab page (it picks the variant).
                if (firstSegment == "LabDashboard"
                    && (LoginState.HasPageStrict("LabTestEntry") || LoginState.HasPageStrict("LabConsignments")
                        || LoginState.HasPageStrict("LabAnalysis") || LoginState.HasPageStrict("LabReports")))
                {
                    return Allow;
                }
            }
            else if (firstSegment.Equals("Sas", StringComparison.OrdinalIgnoreCase))
            {
                if (secondSegment.Equals("Payments", StringComparison.OrdinalIgnoreCase))
                {
                    // Admin and Finance modes by their keys (designation only); farmers and the v1 write
                    // roles open the same page read-only through the SampleCollection key (the API decides
                    // the mode). Admin / CorporateAdmin never take the SampleCollection path: their
                    // payment access comes from the designation only.
                    if (LoginState.HasPageStrict("SasPaymentApproval") || LoginState.HasPageStrict("SasPaymentVerification")) return Allow;
                    if (LoginState.UserRole is not (AppRole.Admin or AppRole.CorporateAdmin)
                        && LoginState.CanAccess(PagePermission.SampleCollection)) return Allow;
                    firstSegment = "SasPaymentApproval";
                }
                else
                {
                    firstSegment = "SampleCollection";   // /Sas/MySamples, /Sas/MyReports, /Sas/MyPayments (farmer)
                }
            }

            if (firstSegment.Equals("MOApproval", StringComparison.OrdinalIgnoreCase)
                || firstSegment.Equals("ApprovalDetail", StringComparison.OrdinalIgnoreCase))
            {
                firstSegment = "SchemeApproval";
            }
            else if (firstSegment.Equals("TaskDetail", StringComparison.OrdinalIgnoreCase))
            {
                firstSegment = "TasksAllocation";
            }

            // /FarmOperations is the legacy second @page route of FarmDashboard.razor; without this
            // alias it resolves to the literal key "FarmOperations", which no Designation ever grants,
            // so a non-bypass user holding PagePermission.FarmDashboard would be bounced away.
            if (firstSegment.Equals("FarmOperations", StringComparison.OrdinalIgnoreCase))
            {
                firstSegment = "FarmDashboard";
            }
            else if (firstSegment.Equals("FifteenDayReportReview", StringComparison.OrdinalIgnoreCase)
                || firstSegment.Equals("15DayReportReview", StringComparison.OrdinalIgnoreCase)
                || firstSegment.Equals("15daysEntry", StringComparison.OrdinalIgnoreCase)
                || firstSegment.Equals("FinanceSummary", StringComparison.OrdinalIgnoreCase)
                || firstSegment.Equals("FinanceSummaryOverall", StringComparison.OrdinalIgnoreCase))
            {
                if (LoginState.CanAccess("FifteenDaysEntry") || LoginState.CanAccess("FarmDashboard"))
                {
                    return Allow;
                }
                firstSegment = "FifteenDaysEntry";
            }

            // CanAccess understands "Page.Action" tokens (page reachable if any action
            // is granted) as well as legacy bare page tokens. Dynamically registered
            // pages use the same generic check: Key == URL first segment.
            // The designation-only lab / payment keys are checked without the admin bypass.
            // Resolve the permission identity the SAME way the menus do: ShellNavigation maps
            // each route first segment to its PagePermission key (e.g. /CREATE-CSR-1Management
            // -> CSR1Create, /StockReport -> SalesReport, /qr-scanner -> QRScanner). Fall back
            // to the segment, which is the convention new pages follow (route prefix == enum
            // member name), so a new page / menu / enum member needs no wiring here.
            // Guest House Master and Cancellation Requests also open to a GHAdmin / SDWAAdmin designation
            // (the API's GuestHouseMasterAccess / GuestHouseFrontOfficeAccess filters grant the same).
            if ((firstSegment.Equals("GuestHouseMaster", StringComparison.OrdinalIgnoreCase)
                    || firstSegment.Equals("GuestHouseCancellations", StringComparison.OrdinalIgnoreCase))
                && LoginState.HasGuestHouseAdminPermission)
            {
                return Allow;
            }
            if (firstSegment.Equals("DealerManagementTrainingProgram", StringComparison.OrdinalIgnoreCase))
            {
                return Allow;
            }

            // ============================================================
            // CSR-2 LIST ACCESS
            // ============================================================
            // Allow signed-in MD Portal users to open the CSR-2 list.
            // Individual CSR-2 workflow actions are still controlled by
            // CSR2Controller and role/status checks.
            //
            // SMD can view the CSR-2 list.
            // HQ validation      -> MO
            // HQ approval        -> RM / RMM
            // State validation   -> SMDO
            // State approval     -> SMM
            // ============================================================

            if (firstSegment.Equals(
                    "CSR2List",
                    StringComparison.OrdinalIgnoreCase)
                ||
                firstSegment.Equals(
                    "CSR-2List",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Allow;
            }


            // ============================================================
            // PAGE PERMISSION RESOLUTION
            // ============================================================

            // Sales Audit child pages use the same page permission
            // as the parent Sales Audit page.
            //
            // IMPORTANT:
            // This does not bypass authorization.
            // The user must have access to SalesAudit.
            // API-level CSR-2 ownership and eligibility checks remain active.

            var permissionKey = firstSegment.ToLowerInvariant() switch
            {
                // Sales Audit workflow
                "auditoverview" => nameof(PagePermission.SalesAudit),
                "salesplan" => nameof(PagePermission.SalesAudit),
                "dealermapping" => nameof(PagePermission.SalesAudit),
                "salesauditdetails" => nameof(PagePermission.SalesAudit),
                "salestracking" => nameof(PagePermission.SalesAudit),
                "planreview" => nameof(PagePermission.SalesAudit),
                "salestrackinglist" => nameof(PagePermission.SalesAudit),
                "periodendreview" => nameof(PagePermission.SalesAudit),
                "requestextension" => nameof(PagePermission.SalesAudit),
                "extensionrequestdetails" => nameof(PagePermission.SalesAudit),
                "extensionrequests" => nameof(PagePermission.SalesAudit),
                "extensionstatusdrawer" => nameof(PagePermission.SalesAudit),
                // Existing permission resolution for all other pages
                _ => ShellNavigation.ResolvePermissionKey(firstSegment)
            };

            // Designation-only keys use the strict check; everything else CanAccess with the segment
            // fallback. Guest House booking / My Bookings routes answer only to their own key (see
            // ShellNavigation.IsRoutePermitted).
            bool allowed = ShellNavigation.IsRoutePermitted(LoginState, permissionKey, firstSegment);

            // Preserve existing authorization redirect.
            if (!allowed && !isDirectorSchemeApproval)
            {
                return Redirect(LoginState.LandingPage);
            }

            return Allow;
        }
    }
}
