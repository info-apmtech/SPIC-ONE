using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// One navigable destination used by the responsive shell (phone bottom tab bar,
/// "More" sheet and tablet rail).
/// </summary>
/// <param name="Key">Stable identifier; equals the first URL segment of <paramref name="Href"/> so the
/// active tab can be resolved from <c>NavigationManager.Uri</c>.</param>
/// <param name="Label">Full label as shown in the sidebar / More sheet.</param>
/// <param name="Icon">bootstrap-icons class, e.g. <c>bi-speedometer2</c>.</param>
/// <param name="Href">Absolute app path, e.g. <c>/Dashboard</c>.</param>
/// <param name="PermissionKey">Key passed to <see cref="LoginState.CanAccess(string)"/>. PageGuard gates a
/// route by its first URL segment, so this is normally the same as <paramref name="Key"/>; it differs
/// only where NavMenu.razor historically uses another key (e.g. StockReport → SalesReport).</param>
public sealed record ShellTab(string Key, string Label, string Icon, string Href, string PermissionKey)
{
    /// <summary>
    /// Optional extra visibility rule that REPLACES the plain <c>CanAccess(PermissionKey)</c> check.
    /// Mirrors the non-trivial conditions in NavMenu.razor (role-only items, Dealer / Director
    /// special cases). Admin and CorporateAdmin already bypass <c>CanAccess</c> inside LoginState.
    /// </summary>
    public Func<LoginState, bool>? Rule { get; init; }

    /// <summary>Group heading used by the More sheet (null = top-level item).</summary>
    public string? Group { get; init; }

    /// <summary>Short label (about 10 characters) for the tab bar / rail; falls back to <see cref="Label"/>.</summary>
    public string? ShortLabel { get; init; }

    public string TabLabel => ShortLabel ?? Label;
}

/// <summary>
/// Single source of truth for the responsive shell's navigation: which destinations exist,
/// who may see them, and which four belong in a role's phone tab bar.
///
/// NOTE ON DUPLICATION: NavMenu.razor (desktop sidebar + #mobileSidebar offcanvas) still carries
/// its own hard-coded link list with accordions, SVG icons and search-highlighting. Rewriting it to
/// consume this list was judged too risky for the live desktop UI, so the list below MIRRORS
/// NavMenu.razor. When a link is added to NavMenu.razor, add it here too (same href / label /
/// permission condition).
/// </summary>
public static class ShellNavigation
{
    public const int MaxPhoneTabs = 4;
    public const int MaxRailItems = 7;

    // ---------------------------------------------------------------------------------------------
    // Group headings (More sheet)
    // ---------------------------------------------------------------------------------------------
    public const string GroupSubsidy = "Subsidy Management System";
    public const string GroupMdPortal = "MD Portal";
    public const string GroupSdwa = "SDWA";
    public const string GroupSettings = "Settings";
    public const string GroupAdminTools = "Admin tools";
    public const string GroupGuestHouse = "Guest House";
    public const string GroupApprovals = "Approvals & Reports";

    // Convenience so the rules read like NavMenu.razor.
    private static bool IsAdminOrCorporate(LoginState s) => s.UserRole is AppRole.Admin or AppRole.CorporateAdmin;

    // Field staff: their phone bar is built around activities, dealer work and farmer work (product owner, 2026-09-20).
    private static bool IsFieldStaff(LoginState s) => s.UserRole is AppRole.MO or AppRole.MDO or AppRole.JMDO;

    // ---------------------------------------------------------------------------------------------
    // CANDIDATE LIST (ordered). Order matters twice:
    //   1. It is the fall-through order used by GetPhoneTabs when a role-priority item is not
    //      accessible to the current user.
    //   2. It is the display order inside the More sheet (grouped by Group, groups in first-seen order).
    // ---------------------------------------------------------------------------------------------
    public static readonly IReadOnlyList<ShellTab> Candidates = new List<ShellTab>
    {
        // ---- top level (mirrors NavMenu.razor) ----
        new("Dashboard", "Dashboard", "bi-speedometer2", "/Dashboard", nameof(PagePermission.Dashboard))
        {
            Rule = s => s.UserRole != AppRole.Dealer && s.CanAccess(nameof(PagePermission.Dashboard))
        },
        new("SubDealerList", "Sub Dealer Master", "bi-people-fill", "/SubDealerList", nameof(PagePermission.SubDealerList))
        {
            ShortLabel = "Dealers"
        },
        new("DigitalLibrary", "Digital Library", "bi-collection-play", "/DigitalLibrary", "DigitalLibrary")
        {
            ShortLabel = "Library",
            // New module without a PagePermission key yet: admins only (mirrors NavMenu).
            Rule = s => s.UserRole is AppRole.Admin or AppRole.CorporateAdmin
        },

        new("Community", "Knowledge Community", "bi-people-fill", "/Community", "Community")
        {
            ShortLabel = "Community",
            // New module without a PagePermission key yet: admins only (mirrors NavMenu).
            Rule = s => s.UserRole is AppRole.Admin or AppRole.CorporateAdmin
        },

        // ---- shell hubs (phone destinations; PageGuard opens them to every signed-in user with a
        //      designation because each hub only LINKS to pages the user can already open) ----
        new("Activities", "My Activities", "bi-clipboard2-pulse-fill", "/Activities", "Activities")
        {
            ShortLabel = "Activities", Rule = IsFieldStaff
        },
        new("Farmers", "Farmers", "bi-flower2", "/Farmers", "Farmers")
        {
            Rule = IsFieldStaff
        },
        new("Alerts", "Alerts", "bi-bell-fill", "/Alerts", "Alerts")
        {
            // Also reachable from the bell in the phone/tablet top bar.
            Rule = s => s.IsLoggedIn
        },
        new("AskAI", "Ask SPIC AI", "bi-stars", "/DigitalLibrary/chat", "DigitalLibrary")
        {
            // Also reachable from the sparkle icon in the phone/tablet top bar. The chat is open to
            // every role; the rest of the Digital Library stays behind CanAccess("DigitalLibrary").
            ShortLabel = "Ask AI", Rule = s => s.IsLoggedIn
        },

        // ---- role-specific quick destinations (pages reachable today but not listed in NavMenu) ----
        new("SDWADashboard", "Dealer Dashboard", "bi-house-door-fill", "/SDWADashboard", nameof(PagePermission.SDWADashboard))
        {
            ShortLabel = "Home", Group = GroupSdwa,
            // PageGuard always lets a Dealer reach SDWADashboard, even with no designation.
            Rule = s => s.UserRole == AppRole.Dealer || s.CanAccess(nameof(PagePermission.SDWADashboard))
        },
        new("WelfareSchemes", "Welfare Schemes", "bi-gift-fill", "/WelfareSchemes", nameof(PagePermission.WelfareSchemes))
        {
            ShortLabel = "Schemes", Group = GroupSdwa,
            Rule = s => s.UserRole == AppRole.Dealer || s.CanAccess(nameof(PagePermission.WelfareSchemes))
        },
        new("GuestHouse", "Guest House", "bi-building", "/GuestHouse", nameof(PagePermission.GuestHouse))
        {
            ShortLabel = "Guest House", Group = GroupGuestHouse
        },
        new("MyBookings", "My Bookings", "bi-calendar-check-fill", "/MyBookings", nameof(PagePermission.MyBookings))
        {
            ShortLabel = "Bookings", Group = GroupGuestHouse
        },
        new("SchemeApproval", "Scheme Approval", "bi-check2-square", "/SchemeApproval", nameof(PagePermission.SchemeApproval))
        {
            ShortLabel = "Approvals", Group = GroupSdwa,
            Rule = s => s.UserRole != AppRole.Dealer
                        && (s.CanAccess(nameof(PagePermission.SchemeApproval)) || s.UserRole == AppRole.Director)
        },
        new("SMMApprovals", "SMM Approvals", "bi-clipboard2-check-fill", "/SMMApprovals", "SMMApprovals")
        {
            ShortLabel = "Approvals", Group = GroupApprovals
        },
        new("AVPApprovals", "AVP Approvals", "bi-patch-check-fill", "/AVPApprovals", "AVPApprovals")
        {
            ShortLabel = "Approvals", Group = GroupApprovals
        },
        new("RMDValidationQueue", "RMD Validation Queue", "bi-list-check", "/RMDValidationQueue", "RMDValidationQueue")
        {
            ShortLabel = "Queue", Group = GroupMdPortal
        },
        new("ReportsCenter", "Reports Center", "bi-bar-chart-fill", "/ReportsCenter", "ReportsCenter")
        {
            ShortLabel = "Reports", Group = GroupApprovals
        },
        new("Logistics", "Logistics", "bi-truck", "/Logistics", nameof(PagePermission.Logistics))
        {
            ShortLabel = "Logistics", Group = GroupSettings
        },
        new("LogisticsMaster", "Logistics Master", "bi-box-seam-fill", "/LogisticsMaster", "LogisticsMaster")
        {
            ShortLabel = "Master", Group = GroupSettings
        },
        new("LogisticsReport", "Logistics Report", "bi-file-earmark-spreadsheet", "/LogisticsReport", nameof(PagePermission.LogisticsReport))
        {
            ShortLabel = "Reports", Group = GroupApprovals
        },
        new("UserProfile", "User Profile", "bi-person-fill", "/UserProfile", "UserProfile")
        {
            ShortLabel = "Profile"
        },
        new("Designation", "Designation", "bi-gear-fill", "/Designation", nameof(PagePermission.Designation))
        {
            ShortLabel = "Settings", Group = GroupSettings
        },

        // ---- admin tools (mirrors NavMenu.razor) ----
        new("IfmsAutoImport", "IFMS Auto Import", "bi-cloud-upload-fill", "/IfmsAutoImport", "IfmsAutoImport")
        {
            Group = GroupAdminTools, Rule = s => s.UserRole == AppRole.Admin
        },
        new("DataExplorer", "Data Explorer", "bi-database-fill", "/DataExplorer", "DataExplorer")
        {
            Group = GroupAdminTools, Rule = s => s.UserRole is AppRole.Admin or AppRole.SpecialAdmin
        },
        // NavMenu.razor gates the IFMS Logins link on the dynamically registered "IfmsRelaySetup" key.
        new("IfmsLogins", "IFMS Logins", "bi-sim-fill", "/IfmsLogins", "IfmsRelaySetup")
        {
            Group = GroupAdminTools
        },

        // ---- Subsidy Management System accordion ----
        new("StockReport", "Stock Report", "bi-table", "/StockReport", nameof(PagePermission.SalesReport)) { Group = GroupSubsidy },
        new("CompanySales", "Pending Acknowledgement", "bi-hourglass-split", "/CompanySales", nameof(PagePermission.CompanySales)) { Group = GroupSubsidy },
        new("AgeingReport", "Ageing Report", "bi-clipboard-data-fill", "/AgeingReport", nameof(PagePermission.AgeingReport)) { Group = GroupSubsidy },
        new("Acknowledgement", "Acknowledgement", "bi-clipboard-check", "/Acknowledgement", nameof(PagePermission.Acknowledgement)) { Group = GroupSubsidy },
        new("LiquidationCycle", "Liquidation Cycle", "bi-graph-up-arrow", "/LiquidationCycle", nameof(PagePermission.LiquidationCycle)) { Group = GroupSubsidy },
        new("TopRankingDistrict", "Top Ranking District", "bi-trophy-fill", "/TopRankingDistrict", nameof(PagePermission.TopRankingDistrict)) { Group = GroupSubsidy },
        new("TopRankingWholesalers", "Top Ranking Wholesaler", "bi-award-fill", "/TopRankingWholesalers", nameof(PagePermission.TopRankingWholesalers)) { Group = GroupSubsidy },
        new("TopRankingRetailers", "Top Ranking Retailers", "bi-shop", "/TopRankingRetailers", nameof(PagePermission.TopRankingRetailers)) { Group = GroupSubsidy },
        new("ProductWiseStockAvailability", "Productwise Stock Availability", "bi-columns-gap", "/ProductWiseStockAvailability", nameof(PagePermission.ProductWiseStockAvailability)) { Group = GroupSubsidy },
        new("StockDetails", "Stock Details", "bi-inboxes-fill", "/StockDetails", nameof(PagePermission.StockDetails)) { Group = GroupSubsidy },
        new("ExcelFormatFileUpload", "File Upload", "bi-file-earmark-arrow-up-fill", "/ExcelFormatFileUpload", "ExcelFormatFileUpload")
        {
            Group = GroupSubsidy, Rule = s => s.UserRole == AppRole.Admin
        },

        // ---- MD Portal accordion ----
        new("BudgetOverview", "Budget Overview", "bi-wallet2", "/BudgetOverview", "BudgetOverview") { Group = GroupMdPortal },
        new("BudgetingManagements", "Budgeting Management", "bi-cash-stack", "/BudgetingManagements", "BudgetingManagements") { Group = GroupMdPortal },
        new("BudgetSubmissions", "Budget Submissions", "bi-journal-text", "/BudgetSubmissions", nameof(PagePermission.BudgetSubmissions)) { Group = GroupMdPortal },
        new("CREATE-CSR-1Management", "CSR-1 Create", "bi-file-earmark-text", "/CREATE-CSR-1Management", nameof(PagePermission.CSR1Create)) { Group = GroupMdPortal },
        new("CSR-1List", "CSR-1 Management", "bi-kanban-fill", "/CSR-1List", nameof(PagePermission.CSR1Management)) { Group = GroupMdPortal },
        new("CSR2", "CSR-2", "bi-layout-text-window-reverse", "/CSR2", "CSR2") { Group = GroupMdPortal },
        new("FinalReportCSRView", "Final Report CSR", "bi-file-earmark-text", "/FinalReportCSRView", "FinalReportCSRView") { Group = GroupMdPortal },
        new("MOSubmissionValidation", "MO Submission Validation", "bi-ui-checks-grid", "/MOSubmissionValidation", "MOSubmissionValidation") { Group = GroupMdPortal },
        new("RMApprovalStatus", "RM Approval Status", "bi-diagram-3", "/RMApprovalStatus", "RMApprovalStatus") { Group = GroupMdPortal },

        // ---- top level, continued (mirrors NavMenu.razor) ----
        new("Profile", "Profile", "bi-person-circle", "/Profile", nameof(PagePermission.Profile)),
        new("EmployeeManagement", "Employee", "bi-person-badge-fill", "/EmployeeManagement", nameof(PagePermission.EmployeeManagement)),
        new("DealerReviewList", "Dealer Application Review", "bi-person-vcard-fill", "/DealerReviewList", nameof(PagePermission.dealerreviewlist)),
        new("CreditLimitSales", "Financial Year Sales Data", "bi-currency-rupee", "/CreditLimitSales", nameof(PagePermission.CreditLimitSales)),

        // ---- SDWA accordion (remaining items) ----
        new("ReportDashboard", "Admin Dashboard", "bi-grid-fill", "/ReportDashboard", "ReportDashboard") { Group = GroupSdwa },
        new("SubDealerEmployeeMaster", "Sub Dealer & Employee", "bi-people-fill", "/SubDealerEmployeeMaster", nameof(PagePermission.SubDealerEmployeeMaster)) { Group = GroupSdwa },
        new("GuestHouseMaster", "Guest House Master", "bi-building-fill", "/GuestHouseMaster", "GuestHouseMaster")
        {
            Group = GroupSdwa, Rule = IsAdminOrCorporate
        },
        new("SdwaCompanyMaster", "Company Details", "bi-briefcase-fill", "/SdwaCompanyMaster", "SdwaCompanyMaster")
        {
            Group = GroupSdwa, Rule = IsAdminOrCorporate
        },
        new("FrontOffice", "Front Office", "bi-door-open-fill", "/FrontOffice", nameof(PagePermission.FrontOffice)) { Group = GroupSdwa },
        new("GenerateBill", "Generate Bill", "bi-receipt", "/GenerateBill", nameof(PagePermission.GenerateBill)) { Group = GroupSdwa },

        // ---- Settings accordion (remaining items) ----
        new("LocationMaster", "Location Master", "bi-geo-alt-fill", "/LocationMaster", nameof(PagePermission.LocationMaster)) { Group = GroupSettings },
        new("Agriculture", "Agriculture & Products", "bi-flower1", "/Agriculture", nameof(PagePermission.Agriculture)) { Group = GroupSettings },
        new("Financial", "Financial Master", "bi-bank", "/Financial", nameof(PagePermission.Financial)) { Group = GroupSettings },
        new("Relationship", "Relationship Master", "bi-link-45deg", "/Relationship", nameof(PagePermission.Relationship)) { Group = GroupSettings },
        new("PageManagement", "Page Management", "bi-sliders", "/PageManagement", "PageManagement")
        {
            Group = GroupSettings, Rule = IsAdminOrCorporate
        },

        // ---- Contact ----
        new("ContactUs", "Contact Us", "bi-headset", "/ContactUs", "ContactUs"),
    };

    // ---------------------------------------------------------------------------------------------
    // PER-ROLE PRIORITY for the phone tab bar. Keys reference Candidates[].Key. Missing / inaccessible
    // entries fall through to the next accessible Candidate, so there are always up to 4 tabs.
    //
    // Approved by the product owner on 2026-09-20: the bar is Home | three role destinations | More.
    // "Ask SPIC AI" and "Alerts" live in the phone/tablet TOP bar (sparkle + bell) for every role, so
    // they do not take a bottom slot; they are also listed in the More sheet. Field staff get
    // Activities (SAS field work + approvals + guest house), Dealers and Farmers. Guest House stays
    // OUT of the staff bars (More sheet only): few employees book rooms (product owner, 2026-09-20).
    // Edit ONLY this table to change the bar.
    // ---------------------------------------------------------------------------------------------
    private static readonly string[] DealerPriority = { "SDWADashboard", "WelfareSchemes", "GuestHouse", "MyBookings" };
    private static readonly string[] FieldStaffPriority = { "Dashboard", "Activities", "SubDealerList", "Farmers" };            // MO / MDO / JMDO
    private static readonly string[] RegionPriority = { "Dashboard", "SubDealerList", "RMDValidationQueue", "ReportsCenter" };  // RM / RMD
    private static readonly string[] StatePriority = { "Dashboard", "SMMApprovals", "SubDealerList", "ReportsCenter" };         // SMD / SMM
    private static readonly string[] AdminPriority = { "Dashboard", "AVPApprovals", "ReportsCenter", "DigitalLibrary" };        // Admin / CorporateAdmin / Director / AVP (Library falls through for Director / AVP)
    private static readonly string[] SpecialAdminPriority = { "Logistics", "LogisticsMaster", "LogisticsReport", "UserProfile" };
    private static readonly string[] DefaultPriority = FieldStaffPriority;

    public static IReadOnlyList<string> GetRolePriority(AppRole? role) => role switch
    {
        AppRole.Dealer => DealerPriority,
        AppRole.MO or AppRole.MDO or AppRole.JMDO => FieldStaffPriority,
        AppRole.RM or AppRole.RMD => RegionPriority,
        AppRole.SMD or AppRole.SMM => StatePriority,
        AppRole.Admin or AppRole.CorporateAdmin or AppRole.Director or AppRole.AVP => AdminPriority,
        AppRole.SpecialAdmin => SpecialAdminPriority,
        _ => DefaultPriority
    };

    // ---------------------------------------------------------------------------------------------
    // Accessibility of a single destination for the signed-in user.
    // ---------------------------------------------------------------------------------------------
    public static bool IsAccessible(LoginState state, ShellTab tab)
    {
        if (state is null || !state.IsLoggedIn) return false;
        return tab.Rule is not null ? tab.Rule(state) : state.CanAccess(tab.PermissionKey);
    }

    public static ShellTab? Find(string key) =>
        Candidates.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Up to <see cref="MaxPhoneTabs"/> accessible destinations for the phone bottom tab bar:
    /// the role's priority list first, then the remaining Candidates in order.
    /// </summary>
    public static IReadOnlyList<ShellTab> GetPhoneTabs(LoginState state) => Pick(state, MaxPhoneTabs);

    /// <summary>Up to <see cref="MaxRailItems"/> accessible destinations for the tablet rail (same ordering).</summary>
    public static IReadOnlyList<ShellTab> GetRailItems(LoginState state) => Pick(state, MaxRailItems);

    /// <summary>Every accessible destination NOT already in the phone tab bar, in Candidate order.</summary>
    public static IReadOnlyList<ShellTab> MoreItems(LoginState state)
    {
        var tabs = GetPhoneTabs(state);
        return Candidates
            .Where(c => !tabs.Contains(c) && IsAccessible(state, c))
            .ToList();
    }

    /// <summary>
    /// <see cref="MoreItems"/> grouped for display: ungrouped items first (null heading), then each
    /// group in first-seen Candidate order.
    /// </summary>
    public static IReadOnlyList<(string? Group, IReadOnlyList<ShellTab> Items)> MoreItemsGrouped(LoginState state)
    {
        var items = MoreItems(state);
        var result = new List<(string? Group, IReadOnlyList<ShellTab> Items)>();

        var ungrouped = items.Where(i => i.Group is null).ToList();
        if (ungrouped.Count > 0) result.Add((null, ungrouped));

        foreach (var g in items.Where(i => i.Group is not null).Select(i => i.Group!).Distinct())
            result.Add((g, items.Where(i => i.Group == g).ToList()));

        return result;
    }

    private static IReadOnlyList<ShellTab> Pick(LoginState state, int max)
    {
        var result = new List<ShellTab>(max);
        if (state is null || !state.IsLoggedIn) return result;

        foreach (var key in GetRolePriority(state.UserRole))
        {
            if (result.Count >= max) break;
            var tab = Find(key);
            if (tab is not null && !result.Contains(tab) && IsAccessible(state, tab))
                result.Add(tab);
        }

        foreach (var tab in Candidates)
        {
            if (result.Count >= max) break;
            if (!result.Contains(tab) && IsAccessible(state, tab))
                result.Add(tab);
        }

        return result;
    }

    // ---------------------------------------------------------------------------------------------
    // ACTIVE TAB RESOLUTION
    // Child routes that belong to a tab's section, so the tab stays highlighted while the user is
    // inside a flow (e.g. booking a room keeps "Guest House" active).
    // ---------------------------------------------------------------------------------------------
    private static readonly Dictionary<string, string> RouteAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // approvals
        ["ApprovalDetail"] = "SchemeApproval",
        ["MOApproval"] = "SchemeApproval",
        // welfare schemes
        ["ApplyWelfareScheme"] = "WelfareSchemes",
        ["WellfareApplication"] = "WelfareSchemes",
        ["SchemeOverview"] = "WelfareSchemes",
        ["Schemes"] = "WelfareSchemes",
        // guest house booking flow
        ["GuestHouseBooking"] = "GuestHouse",
        ["Rooms"] = "GuestHouse",
        ["RoomDetails"] = "GuestHouse",
        ["GuestDetails"] = "GuestHouse",
        ["GuestBooking"] = "GuestHouse",
        ["Payment"] = "GuestHouse",
        ["BookingPreview"] = "GuestHouse",
        // my bookings
        ["BookingDetails"] = "MyBookings",
        ["CancelBooking"] = "MyBookings",
        ["CancelledBookingDetails"] = "MyBookings",
        ["RefundStatus"] = "MyBookings",
        // dealer registration wizard lives under Dashboard
        ["Register"] = "Dashboard",
        ["Experience"] = "Dashboard",
        ["AnnualSales"] = "Dashboard",
        ["Warehouse"] = "Dashboard",
        ["MarketDetails"] = "Dashboard",
        ["Companies"] = "Dashboard",
        ["Proprietor"] = "Dashboard",
        ["SalesPlaning"] = "Dashboard",
        ["Investment"] = "Dashboard",
        ["CreditLimit"] = "Dashboard",
        ["CreditLimitForGreenStar"] = "Dashboard",
        ["Enclosures"] = "Dashboard",
        ["FinalSubmission"] = "Dashboard",
        ["SavedDealerReview"] = "Dashboard",
        ["ReviewDealer"] = "Dashboard",
        ["DealershipPDF"] = "Dashboard",
        // sub dealers
        ["SubDealerRegistration"] = "SubDealerList",
        // employees
        ["EmployeeRegistration"] = "EmployeeManagement",
        // MD portal
        ["csrplandetails2"] = "CSR2",
        ["EntryInfo"] = "CSR2",
    };

    /// <summary>First path segment of an absolute or base-relative URI, without query/fragment.</summary>
    public static string FirstSegment(string uriOrPath, string? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(uriOrPath)) return string.Empty;

        var path = uriOrPath;
        if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var abs))
        {
            path = abs.AbsolutePath;
            if (!string.IsNullOrEmpty(baseUri) && Uri.TryCreate(baseUri, UriKind.Absolute, out var b)
                && path.StartsWith(b.AbsolutePath, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(b.AbsolutePath.Length);
            }
        }

        path = path.Split('?', '#')[0].Trim('/');
        return path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    /// <summary>Resolves the Candidate key that should be highlighted for the given URI (null if none).</summary>
    public static string? ActiveKey(string uri, string? baseUri = null)
    {
        var seg = FirstSegment(uri, baseUri);
        if (seg.Length == 0) return null;
        if (RouteAliases.TryGetValue(seg, out var alias)) seg = alias;
        return Find(seg)?.Key;
    }

    public static bool IsActive(ShellTab tab, string uri, string? baseUri = null) =>
        string.Equals(ActiveKey(uri, baseUri), tab.Key, StringComparison.OrdinalIgnoreCase);
}
