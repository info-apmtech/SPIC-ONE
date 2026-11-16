# Help Centre — module map

Status: Draft, waiting for the user to confirm
Used by: stage 4 of `docs/superpowers/specs/2026-11-16-help-centre-redesign-design.md`, the guides for the remaining modules.

Every screen (`@page` route) belongs to exactly one guide module. The groups follow the side menu
(`Layout/NavMenu.razor`). Screens that need no guide (sign-in, error, legal and the Help pages)
are listed in `tools/help-tools/coverage-ignore.txt`.

**Done**, in format v2: `publicity`, `sas-sample-collection`, `lab`.

## Remaining modules (proposed order)

| # | Module id | Title | Category | Colour | Icon | Screens |
|---|---|---|---|---|---|---|
| 1 | `dealer-application` | Dealer application | Dealers | #7c3aed | bi-shop | /Register, /Experience, /AnnualSales, /Warehouse, /MarketDetails, /Companies, /Proprietor, /SalesPlaning, /Investment, /CreditLimit, /CreditLimitForGreenStar, /Enclosures, /FinalSubmission, /DealershipPDF/{dealerId:int?} |
| 2 | `dealer-review` | Dealer review and approval | Dealers | #6d28d9 | bi-person-check | /DealerReviewList, /ReviewDealer, /SavedDealerReview, /CreditLimitSales, /AVPApprovals, /SMMApprovals, /DealerDirectory, /DealerMapping, /DealerStateSummary, /DealerManagementTrainingProgram |
| 3 | `sub-dealers` | Sub dealers | Dealers | #8b5cf6 | bi-people | /SubDealerList, /SubDealerRegistration, /SubDealerEmployeeMaster |
| 4 | `schemes` | Schemes and lucky draw | Sales | #d97706 | bi-gift | /SchemeOverview, /AddScheme, /Schemes, /ReviewScheme, /WinnerPopUp, /WinnerDetails, /Luckydraw, /LuckyDrawList, /SelectPurchasedProducts, /Scanproduct, /qr-scanner, /CelebrationWinners |
| 5 | `sales-tracking` | Sales plan and audit | Sales | #2563eb | bi-graph-up-arrow | /SalesPlan, /PlanReview, /PeriodEndReview, /AuditOverview, /SalesAudit, /SalesAuditDetails, /DealerSalesUpload, /SalesTracking, /SalesTrackingList, /ExtensionRequests, /ExtensionRequestDetails |
| 6 | `stock-liquidation` | Stock and liquidation | Sales | #db2777 | bi-boxes | /StockReport, /CompanySales, /AgeingReport, /Acknowledgement, /LiquidationCycle, /TopRankingDistrict, /TopRankingWholesalers, /TopRankingRetailers, /ProductWiseStockAvailability, /StateWiseGlobalStockReconciliation, /StockDetails, /ExcelFormatFileUpload |
| 7 | `field-force` | Field force | Field work | #ea580c | bi-person-walking | /FieldDashboard, /FieldReports, /RMDDashboard, /MdoDashboard, /MdoReports, /AllocationRequests, /AllocationRequestReview, /TaskMonitoring, /TasksAllocation, /TaskDetail, /PerformanceTracker, /TargetAchievement, /FarmerData, /Farmers, /MarkAttendance, /TrackPlayback, /TeamTracking |
| 8 | `demos` | Demonstrations and treatments | Field work | #65a30d | bi-flower1 | /DemoDocumentation, /DemoDetails, /StartDocumentation, /Treatment01, /Treatment02, /TreatmentDemoDetails, /TreatmentDetails |
| 9 | `farm-operations` | Farm operations | Soil testing | #15803d | bi-tree | /FarmDashboard, /FarmOperations, /ProductRateManagement, /ProductRateApprovals, /FarmProductMaster, /FifteenDaysEntry, /15daysEntry, /FifteenDayReportReview, /15DayReportReview, /FinanceSummary, /FinanceSummaryOverall, /InventorySummary, /IncomeSummary, /ExpenseSummary |
| 10 | `budget-csr` | Budget and CSR | Marketing | #0891b2 | bi-wallet2 | /AnnualBudgeting, /BudgetingManagements, /BudgetSubmissions, /BudgetOverview, /StateBudgetManagement, /BudgetReallocations, /RMDHQMapping, /ProgramMaster, /SanctionNumberSequence, /CREATE-CSR-1Management, /CSR-1List, /CSR2, /CSR-2List, /CSR-2ProgramExecution, /csrplandetails2, /FinalReportCSRView, /RMDValidationQueue, /MOSubmissionValidation, /RMApprovalStatus, /RequestExtension/{CSRId:int}, /ApprovalDetail/{Id:int}, /MOApproval/{Id:int} |
| 11 | `welfare` | Welfare schemes (SDWA) | Dealers | #be123c | bi-heart | /SDWADashboard, /WelfareSchemes, /ApplyWelfareScheme, /WellfareApplication, /GrahapravesamScheme, /SchemeApproval, /SdwaCompanyMaster |
| 12 | `guest-house` | Guest house | Facilities | #b45309 | bi-house-door | /GuestHouse, /GuestHouseBooking, /GuestBooking, /GuestDetails, /BookingPreview, /BookingDetails, /Payment, /MyBookings, /CancelBooking, /CancelledBookingDetails, /RefundStatus, /GuestHouseMaster, /Rooms, /RoomDetails, /GuestHouseCancellations, /FrontOffice, /GenerateBill, /BillList, /PaymentManagement |
| 13 | `logistics` | Logistics | Facilities | #475569 | bi-truck | /Logistics, /LogisticsMaster, /LogisticsReport, /PVTMaster |
| 14 | `digital-library` | Digital library | Knowledge | #0e7490 | bi-book | /DigitalLibrary, /DigitalLibrary/{Section}, /DigitalLibrary/categories, /DigitalLibrary/content, /DigitalLibrary/add/{KindRoute}, /DigitalLibrary/view/{Id:int}, /DigitalLibrary/chat, /DigitalLibrary/chat/{ConversationId:int} |
| 15 | `community` | Community | Knowledge | #0d9488 | bi-chat-square-text | /Community, /Community/discussions, /Community/new, /Community/discussion/{Id:int}, /Community/posted/{Id:int} |
| 16 | `reports` | Reports | Reports | #1d4ed8 | bi-bar-chart | /ReportDashboard, /ReportsCenter |
| 17 | `admin-masters` | Administration and masters | Administration | #374151 | bi-gear | /EmployeeManagement, /EmployeeRegistration, /Designation, /LocationMaster, /PlantDealerNatureMaster, /GpsSettings, /ExpenseInformation, /CSRExceedingDaySetting, /Metrics, /IfmsAutoImport, /IfmsLogins, /DataExplorer |
| 18 | `my-account` | My account and dashboard | General | #0b6b3a | bi-person-circle | /Dashboard, /Profile, /UserProfile, /ContactUs, /Alerts, /Activities, /Gallery, /Album, /ViewVideo |

## To confirm with the user

These screens sit in the menu next to the administration pages, but their names also fit the dealer
application. They are placed provisionally and need a decision:

- `/Agriculture`, `/Financial`, `/Relationship`: dealer application steps, or admin masters? Proposed: `admin-masters`.
- `/EntryInfo`: proposed: `dealer-application`.
- `/Register` is also the new-dealer sign-up. Proposed: guide it as step 1 of the dealer application, not ignored.

## How each batch is done

For each module: write `guide.en.md` (format v2, see `docs/help-desk.md`), add `data-help` markers
for its main-task tours, add demo data and run `help-tools shoot`, translate with `help-tools import`,
run `help-tools check`, then open one PR per batch of about 4 modules.
