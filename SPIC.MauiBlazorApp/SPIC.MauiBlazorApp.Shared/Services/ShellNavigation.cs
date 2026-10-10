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
    /// <param name="PermissionKey">Key passed to <see cref="LoginState.CanSeeMenu(string)"/>. PageGuard gates a
    /// route by its first URL segment, so this is normally the same as <paramref name="Key"/>; it differs
    /// only where NavMenu.razor historically uses another key (e.g. StockReport → SalesReport).</param>
    public sealed record ShellTab(string Key, string Label, string Icon, string Href, string PermissionKey)
{
    /// <summary>
    /// Optional extra visibility rule that REPLACES the plain <c>CanSeeMenu(PermissionKey)</c> check.
    /// Only use it to RESTRICT (a role that must never see the page - e.g. a Dealer on SchemeApproval,
    /// which PageGuard hard-blocks on the route too) or for pages that are not Designation-controlled
    /// at all (Admin / SuperAdmin-only features such as Data Explorer). Everything else must stay a
    /// plain entry with no Rule so menu visibility comes from the Designation alone.
    /// </summary>
    public Func<LoginState, bool>? Rule { get; init; }

    /// <summary>Group heading used by the More sheet (null = top-level item).</summary>
    public string? Group { get; init; }

    /// <summary>Short label (about 10 characters) for the tab bar / rail; falls back to <see cref="Label"/>.</summary>
    public string? ShortLabel { get; init; }

    /// <summary>
    /// More-sheet-only destination: accessible users find it in the "More" sheet and the
    /// tablet rail never lists it, and it is excluded from the phone bottom tab bar's
    /// priority/fallback fill. Used for pages grouped only for menu purposes (e.g. the
    /// Schemes parent) that must not steal a bottom-tab or rail slot by role priority.
    /// </summary>
    public bool MoreOnly { get; init; }

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
    public const string GroupSchemes = "Schemes";
    public const string GroupDemoDocumentation = "Demo Documentation";

    // Admin-only helper for the entries that are deliberately not Designation-controlled.
    private static bool IsAdmin(LoginState s) => s.UserRole is AppRole.Admin;

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
            // Designation-controlled like every other menu. The old `UserRole != AppRole.Dealer`
            // term was a MENU-only restriction: PageGuard has no Dealer rule for /Dashboard, so a
            // Dealer holding PagePermission.Dashboard has always been able to open the route.
            // Dropping it makes the entry obey business rule 3 (Designation decides) with no
            // change to route authorization.
            Rule = s => s.CanSeeMenu(PagePermission.Dashboard)
        },
        new("SubDealerList", "Sub Dealer Master", "bi-people-fill", "/SubDealerList", nameof(PagePermission.SubDealerList))
        {
            ShortLabel = "Dealers"
        },
        new("DigitalLibrary", "Digital Library", "bi-collection-play", "/DigitalLibrary", "DigitalLibrary")
        {
            ShortLabel = "Library",
            // Menu visibility goes through CanSeeMenu: the route stays open to every signed-in
            // user (PageAuthorization.OpenAccessPages) but the entry follows the Designation, so an
            // administrator can switch it off. Same decision NavMenu / MobileSidebar use; PageGuard
            // keeps using CanAccess for the route itself.
            Rule = s => s.CanSeeMenu(PagePermission.DigitalLibrary)
        },

        new("Community", "Knowledge Community", "bi-people-fill", "/Community", "Community")
        {
            ShortLabel = "Community",
            // Live module: the PagePermission key exists, so the designation decides the MENU entry
            // (CanSeeMenu, mirroring NavMenu). The route stays open to every signed-in user
            // (product decision 2026-09-20).
            Rule = s => s.CanSeeMenu(PagePermission.Community)
        },

        // ---- SAS portal (page-permission gated; write actions are checked inside the pages) ----
        new("SampleCollection", "Sample Collection", "bi-droplet-half", "/SampleCollection", nameof(PagePermission.SampleCollection))
        {
            ShortLabel = "Samples", Group = "SAS Portal"
        },
        new("ConsignmentHistory", "Consignment History", "bi-box-seam", "/ConsignmentHistory", nameof(PagePermission.ConsignmentHistory))
        {
            ShortLabel = "Consignments", Group = "SAS Portal"
        },

        // ---- SAS Lab portal (version 2, 2026-09-27) and payments: DESIGNATION only ----
        // Rule = HasPageStrict: the Admin / CorporateAdmin roles do NOT bypass these (product decision 2026-09-27).
        new("LabDashboard", "Lab Dashboard", "bi-speedometer", "/Lab", nameof(PagePermission.LabDashboard))
        {
            ShortLabel = "Lab", Group = "SAS Lab",
            Rule = s => s.HasPageStrict(PagePermission.LabDashboard)
        },
        new("LabConsignments", "Consignment Details", "bi-boxes", "/Lab/Consignments", nameof(PagePermission.LabConsignments))
        {
            ShortLabel = "Consignments", Group = "SAS Lab",
            Rule = s => s.HasPageStrict(PagePermission.LabConsignments)
        },
        new("LabAnalysis", "Analysis Tracking", "bi-clipboard2-pulse", "/Lab/Analysis", nameof(PagePermission.LabAnalysis))
        {
            ShortLabel = "Analysis", Group = "SAS Lab",
            Rule = s => s.HasPageStrict(PagePermission.LabAnalysis)
        },
        new("LabReports", "Lab Reports", "bi-file-earmark-bar-graph", "/Lab/Reports", nameof(PagePermission.LabReports))
        {
            ShortLabel = "Reports", Group = "SAS Lab",
            Rule = s => s.HasPageStrict(PagePermission.LabReports)
        },
        new("LabTestEntry", "Test Value Entry", "bi-pencil-square", "/Lab/TestEntry", nameof(PagePermission.LabTestEntry))
        {
            ShortLabel = "Test Entry", Group = "SAS Lab",
            Rule = s => s.HasPageStrict(PagePermission.LabTestEntry)
        },
        new("LabTracking", "Lab Tracking", "bi-binoculars", "/Lab/Tracking", nameof(PagePermission.LabTracking))
        {
            ShortLabel = "Lab Tracking", Group = "SAS Lab",
            Rule = s => s.HasPageStrict(PagePermission.LabTracking)
        },
        new("SasPaymentApproval", "Payment Approval", "bi-cash-coin", "/Sas/Payments", nameof(PagePermission.SasPaymentApproval))
        {
            ShortLabel = "Payments", Group = "SAS Portal",
            Rule = s => s.HasPageStrict(PagePermission.SasPaymentApproval)
        },
        new("SasPaymentVerification", "Payment Verification", "bi-patch-check", "/Sas/Payments", nameof(PagePermission.SasPaymentVerification))
        {
            ShortLabel = "Verification", Group = "SAS Portal",
            Rule = s => s.HasPageStrict(PagePermission.SasPaymentVerification)
        },
        new("SasPaymentHistory", "Payment History", "bi-clock-history", "/Sas/Payments/History", nameof(PagePermission.SasPaymentVerification))
        {
            ShortLabel = "History", Group = "SAS Portal",
            Rule = s => s.HasPageStrict(PagePermission.SasPaymentVerification)
        },
        // Farmer pages (v1 SampleCollection key; the API scopes to the farmer's own samples).
        // The `UserRole == AppRole.Farmer` term is KEPT deliberately: it is a true business
        // authorization, not a menu restriction - SasController rejects a non-Farmer outright
        // (account.Role != AppRole.Farmer), so these three destinations only exist for a Farmer.
        // The permission half of the condition already goes through CanSeeMenu.
        new("SasMySamples", "My Samples", "bi-droplet-half", "/Sas/MySamples", nameof(PagePermission.SampleCollection))
        {
            ShortLabel = "Samples", Group = "SAS Portal",
            Rule = s => s.UserRole == AppRole.Farmer && s.CanSeeMenu(PagePermission.SampleCollection)
        },
        new("SasMyReports", "My Reports", "bi-file-earmark-text", "/Sas/MyReports", nameof(PagePermission.SampleCollection))
        {
            ShortLabel = "Reports", Group = "SAS Portal",
            Rule = s => s.UserRole == AppRole.Farmer && s.CanSeeMenu(PagePermission.SampleCollection)
        },
        new("SasMyPayments", "Payments", "bi-wallet2", "/Sas/Payments", nameof(PagePermission.SampleCollection))
        {
            ShortLabel = "Payments", Group = "SAS Portal",
            Rule = s => s.UserRole == AppRole.Farmer && s.CanSeeMenu(PagePermission.SampleCollection)
        },

        // ---- shell hubs (phone destinations). Each hub only LINKS to pages the user can already
        //      open, so PageGuard keeps the ROUTE open for every signed-in user with a designation.
        //      The MENU entries are Designation-controlled through their own PagePermission
        //      members (added for the menu-authorization standard), so an administrator can now
        //      grant or revoke them like any other menu. ----
        new("Activities", "My Activities", "bi-clipboard2-pulse-fill", "/Activities", nameof(PagePermission.Activities))
        {
            ShortLabel = "Activities", Rule = s => s.CanSeeMenu(PagePermission.Activities)
        },
        new("Farmers", "Farmers", "bi-flower2", "/Farmers", nameof(PagePermission.Farmers))
        {
            Rule = s => s.CanSeeMenu(PagePermission.Farmers)
        },
        new("Alerts", "Alerts", "bi-bell-fill", "/Alerts", nameof(PagePermission.Alerts))
        {
            // Also reachable from the bell in the phone/tablet top bar. The route stays open to
            // every signed-in user (PageAuthorization.OpenAccessRoutes); the MENU entry follows
            // PagePermission.Alerts so the bell can actually be switched on or off per Designation.
            Rule = s => s.CanSeeMenu(PagePermission.Alerts)
        },
        new("AskAI", "Ask SPIC AI", "bi-stars", "/DigitalLibrary/chat", "DigitalLibrary")
        {
            // Also reachable from the sparkle icon in the phone/tablet top bar. The chat is part of
            // the DigitalLibrary page, so it follows the same CanSeeMenu decision as that page.
            ShortLabel = "Ask AI",
            Rule = s => s.CanSeeMenu(PagePermission.DigitalLibrary)
        },
        // Category master behind the content forms (admin page; PageGuard treats
        // /DigitalLibrary/categories like /DigitalLibrary/add). Own key so it is never confused
        // with the Library tab in Find / ActiveKey; the permission is the DigitalLibrary page.
        new("LibraryCategories", "Library Categories", "bi-tags", "/DigitalLibrary/categories", "DigitalLibrary")
        {
            ShortLabel = "Categories", Group = "Digital Library",
            Rule = s => s.CanSeeMenu(PagePermission.DigitalLibrary)
        },

        // ---- role-specific quick destinations (pages reachable today but not listed in NavMenu) ----
        new("SDWADashboard", "Dealer Dashboard", "bi-house-door-fill", "/SDWADashboard", nameof(PagePermission.SDWADashboard))
        {
            ShortLabel = "Home", Group = GroupSdwa,
            // Was `UserRole == AppRole.Dealer || CanAccess(...)`. The Dealer role bypass is
            // replaced by the PagePermission grant (see the Dealer designation backfill script):
            // every existing Dealer designation receives SDWADashboard before this rule ships.
            // PageGuard is untouched and still lets a Dealer reach the ROUTE (see its own Dealer
            // block), so the route stays open even for a designation that has not been backfilled.
            Rule = s => s.CanSeeMenu(PagePermission.SDWADashboard)
        },
        new("WelfareSchemes", "Welfare Schemes", "bi-gift-fill", "/WelfareSchemes", nameof(PagePermission.WelfareSchemes))
        {
            ShortLabel = "Schemes", Group = GroupSdwa,
            // Same conversion as SDWADashboard: Dealer role bypass -> PagePermission grant.
            Rule = s => s.CanSeeMenu(PagePermission.WelfareSchemes)
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
            // The Designation decides the entry (no Director term - that was a menu restriction
            // and PageGuard still lets a Director open the approval ROUTES as its own documented
            // server-side capability).
            //
            // The `UserRole != AppRole.Dealer` term is KEPT deliberately: it is a true business
            // authorization, not a menu restriction. PageGuard itself hard-redirects every Dealer
            // away from /SchemeApproval to /SDWADashboard ("must NEVER reach the SchemeApproval
            // page"), so a menu shown here would be a link the route guard rejects.
            Rule = s => s.UserRole != AppRole.Dealer
                        && s.CanSeeMenu(PagePermission.SchemeApproval)
        },
        new("SMMApprovals", "SMM Approvals", "bi-clipboard2-check-fill", "/SMMApprovals", nameof(PagePermission.SMMApprovals))
        {
            ShortLabel = "Approvals", Group = GroupApprovals
        },
        new("AVPApprovals", "AVP Approvals", "bi-patch-check-fill", "/AVPApprovals", nameof(PagePermission.AVPApprovals))
        {
            ShortLabel = "Approvals", Group = GroupApprovals
        },
        new("RMDValidationQueue", "RMD Validation Queue", "bi-list-check", "/RMDValidationQueue", nameof(PagePermission.RMDValidationQueue))
        {
            ShortLabel = "Queue", Group = GroupMdPortal
        },
        new("ReportsCenter", "Reports Center", "bi-bar-chart-fill", "/ReportsCenter", nameof(PagePermission.ReportsCenter))
        {
            ShortLabel = "Reports", Group = GroupApprovals
        },
        new("Logistics", "Logistics", "bi-truck", "/Logistics", nameof(PagePermission.Logistics))
        {
            ShortLabel = "Logistics", Group = GroupSettings
        },
        new("LogisticsMaster", "Logistics Master", "bi-box-seam-fill", "/LogisticsMaster", nameof(PagePermission.LogisticsMaster))
        {
            ShortLabel = "Master", Group = GroupSettings
        },
        new("LogisticsReport", "Logistics Report", "bi-file-earmark-spreadsheet", "/LogisticsReport", nameof(PagePermission.LogisticsReport))
        {
            ShortLabel = "Reports", Group = GroupApprovals
        },
        new("UserProfile", "User Profile", "bi-person-fill", "/UserProfile", nameof(PagePermission.UserProfile))
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
            // No Rule: Admin / SuperAdmin reach it through the role bypass, every other role
            // through its Designation (PagePermission.IfmsAutoImport).
            Group = GroupAdminTools
        },
        new("DataExplorer", "Data Explorer", "bi-database-fill", "/DataExplorer", "DataExplorer")
        {
            // Mirrors NavMenu.razor / MobileSidebar.razor: BOTH list Data Explorer for Admin or
            // SuperAdmin, and PageGuard lets those two roles through the role bypass - so the shell
            // rule must not be narrower than the sidebar or Admin loses the entry here.
            Group = GroupAdminTools, Rule = s => s.UserRole is AppRole.Admin or AppRole.SuperAdmin
        },
        new("IfmsLogins", "IFMS Logins", "bi-sim-fill", "/IfmsLogins", nameof(PagePermission.IfmsRelaySetup))
        {
            Group = GroupAdminTools
        },
        // Metrics (NavMenu: Settings accordion). MoreOnly: never takes a tab-bar / rail slot.
        new("Metrics", "Metrics", "bi-activity", "/Metrics", nameof(PagePermission.Metrics)) { Group = GroupAdminTools, MoreOnly = true },

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
        // Admin-only tool: no PagePermission exists for it on purpose (it is not a Designation page).
        new("ExcelFormatFileUpload", "File Upload", "bi-file-earmark-arrow-up-fill", "/ExcelFormatFileUpload", "ExcelFormatFileUpload")
        {
            Group = GroupSubsidy, Rule = IsAdmin
        },

        // ---- MD Portal accordion ----
        new("BudgetOverview", "Budget Overview", "bi-wallet2", "/BudgetOverview", nameof(PagePermission.BudgetOverview)) { Group = GroupMdPortal },
        new("BudgetingManagements", "Budgeting Management", "bi-cash-stack", "/BudgetingManagements", nameof(PagePermission.BudgetingManagements)) { Group = GroupMdPortal },
        new("BudgetSubmissions", "Budget Submissions", "bi-journal-text", "/BudgetSubmissions", nameof(PagePermission.BudgetSubmissions)) { Group = GroupMdPortal },
        new("AnnualBudgeting", "Annual Budgeting", "bi-wallet-fill", "/AnnualBudgeting", nameof(PagePermission.AnnualBudgeting)) { Group = GroupMdPortal },
        new("ProgramMaster", "Program Master", "bi-diagram-3", "/ProgramMaster", nameof(PagePermission.ProgramMaster)) { Group = GroupMdPortal },
        new("CREATE-CSR-1Management", "CSR-1 Create", "bi-file-earmark-text", "/CREATE-CSR-1Management", nameof(PagePermission.CSR1Create)) { Group = GroupMdPortal },
        new("CSR-1List", "CSR-1 Management", "bi-kanban-fill", "/CSR-1List", nameof(PagePermission.CSR1Management)) { Group = GroupMdPortal },
        new("CSR2", "CSR-2", "bi-layout-text-window-reverse", "/CSR2", nameof(PagePermission.CSR2)) { Group = GroupMdPortal },
        new("FinalReportCSRView", "Final Report CSR", "bi-file-earmark-text", "/FinalReportCSRView", nameof(PagePermission.FinalReportCSRView)) { Group = GroupMdPortal },
        new("MOSubmissionValidation", "MO Submission Validation", "bi-ui-checks-grid", "/MOSubmissionValidation", nameof(PagePermission.MOSubmissionValidation)) { Group = GroupMdPortal },
        new("RMApprovalStatus", "RM Approval Status", "bi-diagram-3", "/RMApprovalStatus", nameof(PagePermission.RMApprovalStatus)) { Group = GroupMdPortal },

        // ---- top level, continued (mirrors NavMenu.razor) ----
        new("Profile", "Profile", "bi-person-circle", "/Profile", nameof(PagePermission.Profile)),
        new("EmployeeManagement", "Employee", "bi-person-badge-fill", "/EmployeeManagement", nameof(PagePermission.EmployeeManagement)),
        new("DealerReviewList", "Dealer Application Review", "bi-person-vcard-fill", "/DealerReviewList", nameof(PagePermission.dealerreviewlist)),
        new("CreditLimitSales", "Financial Year Sales Data", "bi-currency-rupee", "/CreditLimitSales", nameof(PagePermission.CreditLimitSales)),

        // ---- Schemes parent menu (10 pages that previously had no menu entry). MoreOnly: these
        //      must never auto-fill the phone bottom tab bar or tablet rail, only the More sheet. ----
        new("SchemeOverview", "Scheme Overview", "bi-clipboard-data-fill", "/SchemeOverview", nameof(PagePermission.SchemeOverview)) { Group = GroupSchemes, MoreOnly = true },
        new("AddScheme", "Add Scheme", "bi-plus-square-fill", "/AddScheme", nameof(PagePermission.AddScheme)) { Group = GroupSchemes, MoreOnly = true },
        new("Schemes", "Scheme List", "bi-collection-fill", "/Schemes", nameof(PagePermission.Schemes)) { Group = GroupSchemes, MoreOnly = true },
        new("WinnerPopUp", "Winner PopUp", "bi-gift-fill", "/WinnerPopUp", nameof(PagePermission.WinnerPopUp)) { Group = GroupSchemes, MoreOnly = true },
        new("WinnerDetails", "Winner Details", "bi-trophy-fill", "/WinnerDetails", nameof(PagePermission.WinnerDetails)) { Group = GroupSchemes, MoreOnly = true },
        new("Luckydraw", "Lucky Draw", "bi-dice-6-fill", "/Luckydraw", nameof(PagePermission.Luckydraw)) { Group = GroupSchemes, MoreOnly = true },
        new("LuckyDrawList", "Lucky Draw List", "bi-file-earmark-text-fill", "/LuckyDrawList", nameof(PagePermission.LuckyDrawList)) { Group = GroupSchemes, MoreOnly = true },
        new("SelectPurchasedProducts", "Select Purchased Products", "bi-bag-check-fill", "/SelectPurchasedProducts", nameof(PagePermission.SelectPurchasedProducts)) { Group = GroupSchemes, MoreOnly = true },
        new("Scanproduct", "Scan Product", "bi-upc-scan", "/Scanproduct", nameof(PagePermission.Scanproduct)) { Group = GroupSchemes, MoreOnly = true },
        new("qr-scanner", "QR Scanner", "bi-qr-code-scan", "/qr-scanner", nameof(PagePermission.QRScanner)) { Group = GroupSchemes, MoreOnly = true },

        // ---- SDWA accordion (remaining items) ----
        new("ReportDashboard", "Admin Dashboard", "bi-grid-fill", "/ReportDashboard", nameof(PagePermission.ReportDashboard)) { Group = GroupSdwa },
        new("SubDealerEmployeeMaster", "Sub Dealer & Employee", "bi-people-fill", "/SubDealerEmployeeMaster", nameof(PagePermission.SubDealerEmployeeMaster)) { Group = GroupSdwa },
        // The three SDWA master pages are Designation-controlled, exactly like the desktop
        // sidebar entries for them (NavMenu / MobileSidebar use CanSeeMenu too), so no Rule.
        // Admin still reaches them through the role bypass; CorporateAdmin needs the grant.
        // Guest House Master shares the GuestHouse permission with its sidebar entry and
        // /GuestHouse, so its PermissionKey is the GuestHouse key rather than the URL segment.
        // Also shown to a GHAdmin / SDWAAdmin designation (PageGuard and the API allow the same).
        new("GuestHouseMaster", "Guest House Master", "bi-building-fill", "/GuestHouseMaster", nameof(PagePermission.GuestHouse))
        {
            Group = GroupSdwa,
            Rule = s => s.CanSeeMenu(PagePermission.GuestHouse) || s.HasGuestHouseAdminPermission
        },
        new("SdwaCompanyMaster", "Company Details", "bi-briefcase-fill", "/SdwaCompanyMaster", nameof(PagePermission.SdwaCompanyMaster))
        {
            Group = GroupSdwa
        },
        new("GuestHouseCancellations", "Cancellation Requests", "bi-x-octagon-fill", "/GuestHouseCancellations", nameof(PagePermission.GuestHouseCancellations))
        {
            Group = GroupSdwa,
            Rule = s => s.CanSeeMenu(PagePermission.GuestHouseCancellations) || s.HasGuestHouseAdminPermission
        },
        new("FrontOffice", "Front Office", "bi-door-open-fill", "/FrontOffice", nameof(PagePermission.FrontOffice)) { Group = GroupSdwa },
        new("GenerateBill", "Generate Bill", "bi-receipt", "/GenerateBill", nameof(PagePermission.GenerateBill)) { Group = GroupSdwa },

        // ---- Settings accordion (remaining items) ----
        new("LocationMaster", "Location Master", "bi-geo-alt-fill", "/LocationMaster", nameof(PagePermission.LocationMaster)) { Group = GroupSettings },
        new("Agriculture", "Agriculture & Products", "bi-flower1", "/Agriculture", nameof(PagePermission.Agriculture)) { Group = GroupSettings },
        new("Financial", "Financial Master", "bi-bank", "/Financial", nameof(PagePermission.Financial)) { Group = GroupSettings },
        new("Relationship", "Relationship Master", "bi-link-45deg", "/Relationship", nameof(PagePermission.Relationship)) { Group = GroupSettings },
        // ---- Contact ----
        new("ContactUs", "Contact Us", "bi-headset", "/ContactUs", nameof(PagePermission.ContactUs)),

        // ---- Demo documentation subtree (mirrors the NavMenu / MobileSidebar accordion).
        //      Appended at the END on purpose: every existing candidate keeps its position, so
        //      More-sheet order and the role tab/rail fill are unchanged.
        //      PermissionKey stays the route-only name for the six children: PageGuard resolves a
        //      route's permission identity from ShellNavigation.PermissionKey, so changing it would
        //      change ROUTE authorization (forbidden). They are Designation-controlled through the
        //      menu Rule only, using the PagePermission members added for the menu standard.
        //      MoreOnly: grouped menu entries only, never a tab or rail slot. ----
        new("DemoDocumentation", "Demo Documentation", "bi-journal-richtext", "/DemoDocumentation", nameof(PagePermission.DemoDocumentation)) { Group = GroupDemoDocumentation, MoreOnly = true },
        new("StartDocumentation", "Start Documentation", "bi-pencil-square", "/StartDocumentation", "StartDocumentation") { Group = GroupDemoDocumentation, MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.StartDocumentation) },
        new("DemoDetails", "Demo Details", "bi-clipboard2-data", "/DemoDetails", "DemoDetails") { Group = GroupDemoDocumentation, MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.DemoDetails) },
        new("TreatmentDetails", "Treatment Details", "bi-list-check", "/TreatmentDetails", "TreatmentDetails") { Group = GroupDemoDocumentation, MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.TreatmentDetails) },
        new("Treatment01", "Treatment 01", "bi-1-circle", "/Treatment01", "Treatment01") { Group = GroupDemoDocumentation, MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.Treatment01) },
        new("TreatmentDemoDetails", "Treatment Demo Details", "bi-easel", "/TreatmentDemoDetails", "TreatmentDemoDetails") { Group = GroupDemoDocumentation, MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.TreatmentDemoDetails) },
        new("Treatment02", "Treatment 02", "bi-2-circle", "/Treatment02", "Treatment02") { Group = GroupDemoDocumentation, MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.Treatment02) },

        // ---- sidebar entries that had no shell destination (desktop/mobile menu parity).
        //      Same PermissionKey / condition as NavMenu.razor, MoreOnly so the phone tab bar and
        //      tablet rail keep exactly the items they show today. ----
        // Menu visibility goes through LoginState.CanSeeMenu: these three keys are in
        // PageAuthorization.OpenAccessRoutes (so CanAccess lets the route open for every signed-in
        // user) but are ALSO assignable PagePermission members, so the menu must still follow the
        // designation. Admin / SuperAdmin are unaffected inside CanSeeMenu.
        new("SalesAudit", "Sales Audit", "bi-clipboard-data", "/SalesAudit", nameof(PagePermission.SalesAudit))
        { MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.SalesAudit) },
        new("ExtensionRequests", "Extension Requests", "bi-hourglass-split", "/ExtensionRequests", nameof(PagePermission.ExtensionRequests))
        { MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.ExtensionRequests) },
        new("FarmDashboard", "Farm Operations", "bi-speedometer2", "/FarmDashboard", nameof(PagePermission.FarmDashboard))
        { MoreOnly = true, Rule = s => s.CanSeeMenu(PagePermission.FarmDashboard) },
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
    //
    // Menu visibility only: CanSeeMenu answers "should this entry render?", never "may the user
    // open the route?" - PageGuard keeps using CanAccess for that. Admin / SuperAdmin return true
    // inside CanSeeMenu (except the designation-only SAS Lab / payment keys, which keep their
    // explicit HasPageStrict Rule below), and every other role is decided by its Designation alone.
    // ---------------------------------------------------------------------------------------------
    public static bool IsAccessible(LoginState state, ShellTab tab)
    {
        if (state is null || !state.IsLoggedIn) return false;
        return tab.Rule is not null ? tab.Rule(state) : state.CanSeeMenu(tab.PermissionKey);
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
            // More-only destinations (e.g. Schemes parent pages) never take a tab-bar / rail slot.
            if (tab.MoreOnly) continue;
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
        // task allocation
        ["TaskDetail"] = "TasksAllocation",
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

    /// <summary>
    /// Resolves the PagePermission key for a route's first URL segment.
    /// Checks Candidates directly first; if absent, resolves any child route alias
    /// (e.g. CancelBooking -> MyBookings, RefundStatus -> MyBookings) and returns that
    /// parent destination's PermissionKey, falling back to the segment itself.
    /// </summary>
    public static string ResolvePermissionKey(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment)) return string.Empty;

        var direct = Find(segment);
        if (direct != null) return direct.PermissionKey;

        if (RouteAliases.TryGetValue(segment, out var alias))
        {
            return Find(alias)?.PermissionKey ?? alias;
        }

        return segment;
    }
}

