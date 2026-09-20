# SPIC ONE: screen checklist and native-experience programme

Generated 2026-09-19 from the page inventory (120 routed pages in `SPIC.MauiBlazorApp.Shared/Pages`). Tick the four surface columns per page as they pass; nothing ships to the live web until every row in a workstream is green.

## 1. What "passes" means for one page

Checked on **Phone** (360 to 430 px, portrait), **Tablet** (768 to 1199 px, both orientations), **Windows** (WebView2 window, 1024 px and up, mouse and keyboard) and **Web desktop** (Chrome/Edge 1280 px and up, the live users' view, which must look the same as today).

| # | Check | How to verify |
|---|---|---|
| 1 | Renders with data, no blank body, no error boundary | Sweep script plus screenshot |
| 2 | No horizontal scroll, no clipped content, nothing under system bars | `scrollWidth <= innerWidth` in sweep; visual |
| 3 | Navigation: reachable from menu or tab bar, back returns to previous screen, deep link opens | Manual plus sweep redirect check |
| 4 | Loading state while fetching; empty state when no rows; error state on API failure | Throttle network or stop API on staging |
| 5 | Every primary action works: save, submit, approve, reject, send back, delete (with confirm), export Excel/PDF, upload, download, print, map pick, scan | Against the **staging** API only |
| 6 | Forms: validation messages visible, keyboard does not hide the active field or the action bar, date and select pickers open natively on phone | Manual on device |
| 7 | Tables: card mode on phone, sticky header with inner horizontal scroll on tablet, full table on desktop | Visual at three widths |
| 8 | Drawers and modals: bottom sheet on phone, side drawer on tablet and desktop, close with back gesture or Escape | Manual |
| 9 | Touch targets at least 44 px; no hover-only controls on touch surfaces | Visual plus DOM audit |
| 10 | Role checks: page hidden and blocked for roles without access; visible for roles with access | Two test logins per role group |
| 11 | Session: survives app restart, expiry shows a warning, 401 returns to login | Manual |
| 12 | Desktop web unchanged for existing users (same layout at 1280 px and up) | Before and after screenshot diff |


## 2. Page inventory by workstream

Counts are occurrences in the page source: Table = `<table>` or `SpicDataTable`, Form = `EditForm`, Upload = `InputFile`, Export = export or download calls, Sheet = drawer, modal or offcanvas, Map/Chart = Leaflet or Chart.js, Loading = loading-state code. "In menu" means linked from NavMenu; pages not in the menu are reached from other pages or are orphans to confirm with SPIC.


### Workstream A  Dealer onboarding and credit

| Page | Routes | Lines | Table | Form | Upload | Export | Sheet | Map/Chart | Loading | In menu | Phone | Tablet | Windows | Web |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| _ProprietorDetails | /ProprietorDetails | 147 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Agriculture | /Agriculture | 1020 | 2 | 0 | 1 | 13 | 23 | 0 | 6 | yes | [ ] | [ ] | [ ] | [ ] |
| AnnualSales | /AnnualSales | 942 | 1 | 0 | 0 | 0 | 8 | 0 | 6 | no | [ ] | [ ] | [ ] | [ ] |
| CompaniesOperating | /Companies | 247 | 0 | 0 | 0 | 0 | 0 | 0 | 6 | no | [ ] | [ ] | [ ] | [ ] |
| CreditLimit | /CreditLimit | 1921 | 2 | 0 | 4 | 0 | 0 | 0 | 4 | no | [ ] | [ ] | [ ] | [ ] |
| CreditLimitForGreenStar | /CreditLimitForGreenStar | 2295 | 2 | 0 | 2 | 0 | 0 | 0 | 4 | no | [ ] | [ ] | [ ] | [ ] |
| CreditLimitSales | /CreditLimitSales | 885 | 1 | 0 | 1 | 13 | 30 | 0 | 5 | yes | [ ] | [ ] | [ ] | [ ] |
| Dashboard | /Dashboard | 1871 | 0 | 0 | 1 | 23 | 37 | 0 | 8 | yes | [ ] | [ ] | [ ] | [ ] |
| DealerDirectory | /DealerDirectory | 248 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| DealerReviewList | /DealerReviewList | 941 | 1 | 0 | 0 | 0 | 0 | 0 | 4 | yes | [ ] | [ ] | [ ] | [ ] |
| DEALERSHIPPDF | /DealershipPDF/{dealerId:int?} | 4815 | 20 | 0 | 0 | 0 | 0 | 0 | 5 | no | [ ] | [ ] | [ ] | [ ] |
| DealerStateSummary | /DealerStateSummary | 844 | 1 | 0 | 0 | 0 | 0 | 0 | 7 | no | [ ] | [ ] | [ ] | [ ] |
| EmployeeManagementList | /EmployeeManagement | 590 | 1 | 0 | 1 | 38 | 15 | 0 | 5 | yes | [ ] | [ ] | [ ] | [ ] |
| EmployeeRegistration | /EmployeeRegistration | 1077 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | no | [ ] | [ ] | [ ] | [ ] |
| Enclosures | /Enclosures | 3795 | 2 | 0 | 62 | 0 | 44 | 0 | 4 | no | [ ] | [ ] | [ ] | [ ] |
| ExcelFormatFileUpload | /ExcelFormatFileUpload | 446 | 0 | 0 | 1 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| Experience | /Experience | 557 | 0 | 0 | 0 | 0 | 7 | 0 | 5 | no | [ ] | [ ] | [ ] | [ ] |
| FinalSubmission | /FinalSubmission | 1240 | 0 | 0 | 0 | 0 | 0 | 0 | 3 | no | [ ] | [ ] | [ ] | [ ] |
| Financial | /Financial | 829 | 1 | 0 | 1 | 0 | 8 | 0 | 7 | yes | [ ] | [ ] | [ ] | [ ] |
| Investment | /Investment | 1724 | 0 | 0 | 6 | 0 | 32 | 0 | 4 | no | [ ] | [ ] | [ ] | [ ] |
| MarketDetails | /MarketDetails | 820 | 0 | 0 | 0 | 0 | 5 | 0 | 6 | no | [ ] | [ ] | [ ] | [ ] |
| Proprietor | /Proprietor | 2164 | 0 | 0 | 4 | 0 | 4 | 0 | 5 | no | [ ] | [ ] | [ ] | [ ] |
| Register | /Register | 3817 | 0 | 1 | 7 | 0 | 19 | 3 | 6 | no | [ ] | [ ] | [ ] | [ ] |
| Relationship | /Relationship | 468 | 1 | 0 | 0 | 7 | 8 | 0 | 4 | yes | [ ] | [ ] | [ ] | [ ] |
| ReviewDeale | /ReviewDealer | 1781 | 4 | 0 | 0 | 0 | 0 | 1 | 6 | no | [ ] | [ ] | [ ] | [ ] |
| SalesPlaning | /SalesPlaning | 1031 | 1 | 0 | 0 | 0 | 0 | 0 | 6 | no | [ ] | [ ] | [ ] | [ ] |
| SavedDealerReview | /SavedDealerReview | 4032 | 5 | 0 | 0 | 0 | 16 | 1 | 6 | no | [ ] | [ ] | [ ] | [ ] |
| SubDealerEmployeeMaster | /SubDealerEmployeeMaster | 1310 | 1 | 0 | 1 | 25 | 40 | 0 | 5 | yes | [ ] | [ ] | [ ] | [ ] |
| SubDealerList | /SubDealerList | 3312 | 2 | 0 | 1 | 38 | 27 | 2 | 8 | yes | [ ] | [ ] | [ ] | [ ] |
| SubDealerRegistration | /SubDealerRegistration | 3161 | 0 | 1 | 2 | 0 | 19 | 4 | 3 | no | [ ] | [ ] | [ ] | [ ] |
| Warehouse | /Warehouse | 1238 | 0 | 0 | 0 | 0 | 5 | 0 | 6 | no | [ ] | [ ] | [ ] | [ ] |

### Workstream B  Reports and stock

| Page | Routes | Lines | Table | Form | Upload | Export | Sheet | Map/Chart | Loading | In menu | Phone | Tablet | Windows | Web |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Acknowledgement | /Acknowledgement | 1221 | 1 | 0 | 0 | 4 | 1 | 16 | 11 | yes | [ ] | [ ] | [ ] | [ ] |
| AgeingReport | /AgeingReport | 1260 | 2 | 0 | 0 | 4 | 1 | 16 | 11 | yes | [ ] | [ ] | [ ] | [ ] |
| CompanySales | /CompanySales | 1200 | 1 | 0 | 0 | 7 | 1 | 7 | 11 | yes | [ ] | [ ] | [ ] | [ ] |
| IfmsAutoImport | /IfmsAutoImport | 427 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| IfmsLogins | /IfmsLogins | 339 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| LiquidationCycle | /LiquidationCycle | 1428 | 1 | 0 | 0 | 3 | 3 | 35 | 17 | yes | [ ] | [ ] | [ ] | [ ] |
| MOSubmissionValidation | /MOSubmissionValidation | 594 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| ProductWiseStockAvailability | /ProductWiseStockAvailability | 1013 | 1 | 0 | 0 | 7 | 0 | 0 | 18 | yes | [ ] | [ ] | [ ] | [ ] |
| ReportDashboard | /ReportDashboard | 996 | 3 | 0 | 0 | 1 | 0 | 2 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| ReportsCenter | /ReportsCenter | 400 | 1 | 0 | 0 | 0 | 21 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| RMDDashboard | /RMDDashboard | 713 | 1 | 0 | 0 | 0 | 0 | 23 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| RMDValidationQueue | /RMDValidationQueue | 1154 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| StockDetails | /StockDetails | 999 | 1 | 0 | 0 | 4 | 0 | 0 | 11 | yes | [ ] | [ ] | [ ] | [ ] |
| StockReport | /StockReport | 1395 | 1 | 0 | 0 | 7 | 2 | 27 | 11 | yes | [ ] | [ ] | [ ] | [ ] |
| TopRankingDistrict | /TopRankingDistrict | 255 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| TopRankingRetailers | /TopRankingRetailers | 297 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| TopRankingWholesalers | /TopRankingWholesalers | 275 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |

### Workstream C  Guest house, welfare and SDWA

| Page | Routes | Lines | Table | Form | Upload | Export | Sheet | Map/Chart | Loading | In menu | Phone | Tablet | Windows | Web |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AccountSelect | /AccountSelector | 130 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| ApplyWelfareScheme | /ApplyWelfareScheme | 2868 | 0 | 0 | 1 | 0 | 148 | 0 | 25 | no | [ ] | [ ] | [ ] | [ ] |
| BillList | /BillList | 546 | 5 | 0 | 0 | 1 | 9 | 0 | 4 | no | [ ] | [ ] | [ ] | [ ] |
| BookingDetails | /BookingDetails | 756 | 0 | 0 | 0 | 2 | 0 | 0 | 5 | no | [ ] | [ ] | [ ] | [ ] |
| BookingPreview | /BookingPreview | 410 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| CancelBooking | /CancelBooking | 291 | 0 | 0 | 0 | 0 | 14 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| CancelledBookingDetails | /CancelledBookingDetails | 217 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| CelebrationWinners | /CelebrationWinners | 677 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| ContactUs | /ContactUs | 180 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| FrontOffice | /FrontOffice | 776 | 1 | 0 | 0 | 4 | 24 | 0 | 4 | yes | [ ] | [ ] | [ ] | [ ] |
| Gallery | /Gallery | 362 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| GenerateBill | /GenerateBill | 1505 | 7 | 0 | 0 | 10 | 13 | 0 | 2 | yes | [ ] | [ ] | [ ] | [ ] |
| GrahapravesamScheme | /GrahapravesamScheme | 510 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| GuestBooking | /GuestBooking | 418 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| GuestDetails | /GuestDetails | 932 | 0 | 0 | 1 | 0 | 0 | 0 | 5 | no | [ ] | [ ] | [ ] | [ ] |
| GuestHouse | /GuestHouse | 109 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| GuestHouseBooking | /GuestHouseBooking | 180 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| GuestHouseMaster | /GuestHouseMaster | 1253 | 2 | 0 | 2 | 17 | 22 | 0 | 6 | yes | [ ] | [ ] | [ ] | [ ] |
| LuckyDraw | /Luckydraw | 199 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| LuckyDrawList | /LuckyDrawList | 115 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| MyBookings | /MyBookings | 413 | 0 | 0 | 0 | 0 | 0 | 0 | 4 | no | [ ] | [ ] | [ ] | [ ] |
| Payment | /Payment | 926 | 0 | 0 | 0 | 0 | 31 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| PaymentManagement | /PaymentManagement | 1028 | 5 | 0 | 0 | 0 | 95 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| PurchasedProducts | /SelectPurchasedProducts | 99 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| QRScanner | /qr-scanner | 251 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| RefundStatus | /RefundStatus | 428 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| RoomDetails | /RoomDetails | 217 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Rooms | /Rooms | 366 | 0 | 0 | 0 | 0 | 0 | 0 | 3 | no | [ ] | [ ] | [ ] | [ ] |
| ScanProduct | /Scanproduct | 562 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| SdwaCompanyMaster | /SdwaCompanyMaster | 593 | 1 | 0 | 0 | 3 | 8 | 0 | 4 | yes | [ ] | [ ] | [ ] | [ ] |
| SDWADashboard | /SDWADashboard | 996 | 0 | 0 | 0 | 1 | 13 | 0 | 20 | yes | [ ] | [ ] | [ ] | [ ] |
| Verification | /verifymobile | 236 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| ViewAlbum | /Album | 142 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| ViewVideo | /ViewVideo | 34 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| WelfareSchemes | /WelfareSchemes | 293 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| WellfareApplication | /WellfareApplication | 504 | 0 | 0 | 0 | 1 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| WinnerDetails | /WinnerDetails | 315 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Winnerpopup | /WinnerPopUp | 359 | 0 | 0 | 0 | 0 | 15 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |

### Workstream D  Approvals, schemes, CSR and budget

| Page | Routes | Lines | Table | Form | Upload | Export | Sheet | Map/Chart | Loading | In menu | Phone | Tablet | Windows | Web |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AddScheme | /AddScheme | 527 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| ApprovalDetail | /ApprovalDetail/{Id:int} /MOApproval/{Id:int} | 430 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| BudgetingManagements | /BudgetingManagements | 500 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| BudgetOverview | /BudgetOverview | 886 | 1 | 0 | 0 | 0 | 5 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| BudgetSubmissions | /BudgetSubmissions | 352 | 1 | 0 | 0 | 0 | 9 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| CREATE-CSR-1Management | /CREATE-CSR-1Management | 996 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| CSR-1Management | /CSR-1List | 397 | 1 | 0 | 0 | 0 | 9 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| Csr2 | /CSR2 | 815 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| CsrPlandetails2 | /csrplandetails2 | 232 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| EntryInfo | /EntryInfo | 189 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| FinalReportCSR | /FinalReportCSRView | 352 | 1 | 0 | 0 | 1 | 33 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| ReviewScheme | /ReviewScheme | 331 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| RMApprovalStatus | /RMApprovalStatus | 642 | 1 | 0 | 0 | 0 | 9 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| SchemeApproval | /SchemeApproval | 619 | 1 | 0 | 0 | 0 | 1 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| SchemeOverview | /SchemeOverview | 334 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Schemes | /Schemes | 405 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| SMMApprovals | /SMMApprovals | 276 | 1 | 0 | 0 | 0 | 20 | 0 | 4 | no | [ ] | [ ] | [ ] | [ ] |

### Workstream E  Settings, masters, logistics and profile

| Page | Routes | Lines | Table | Form | Upload | Export | Sheet | Map/Chart | Loading | In menu | Phone | Tablet | Windows | Web |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DataExplorer | /DataExplorer | 537 | 3 | 0 | 0 | 10 | 0 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| Designation | /Designation | 853 | 2 | 0 | 0 | 0 | 23 | 0 | 3 | yes | [ ] | [ ] | [ ] | [ ] |
| Home | /home | 16 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| LocationMaster | /LocationMaster | 1468 | 1 | 0 | 1 | 15 | 20 | 0 | 6 | yes | [ ] | [ ] | [ ] | [ ] |
| Login | / /login | 109 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Logistics | /Logistics | 7253 | 2 | 1 | 14 | 80 | 119 | 4 | 4 | yes | [ ] | [ ] | [ ] | [ ] |
| LogisticsMaster | /LogisticsMaster | 2298 | 1 | 1 | 5 | 9 | 8 | 3 | 4 | no | [ ] | [ ] | [ ] | [ ] |
| LogisticsReport | /LogisticsReport | 587 | 1 | 0 | 0 | 0 | 0 | 1 | 5 | no | [ ] | [ ] | [ ] | [ ] |
| NotFound | /not-found | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| PageManagement | /PageManagement | 553 | 1 | 0 | 0 | 0 | 23 | 0 | 0 | yes | [ ] | [ ] | [ ] | [ ] |
| PrivacyPolicy | /privacy /privacy-policy | 180 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Profile | /Profile | 1365 | 1 | 0 | 0 | 0 | 0 | 0 | 10 | yes | [ ] | [ ] | [ ] | [ ] |
| PVTMaster | /PVTMaster | 958 | 4 | 0 | 2 | 8 | 0 | 0 | 2 | no | [ ] | [ ] | [ ] | [ ] |
| UserProfile | /UserProfile | 77 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Welcome | /Welcome | 160 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | no | [ ] | [ ] | [ ] | [ ] |
| Activities (hub) | /Activities | new | 0 | 0 | 0 | 0 | 0 | 0 | 0 | tab bar (MO/MDO/JMDO) | [ ] | [ ] | [ ] | [ ] |
| Farmers (hub) | /Farmers | new | 0 | 0 | 0 | 0 | 0 | 0 | 0 | tab bar (MO/MDO/JMDO) | [ ] | [ ] | [ ] | [ ] |
| Alerts (hub) | /Alerts | new | 0 | 0 | 0 | 0 | 0 | 0 | 0 | top bar bell + More | [ ] | [ ] | [ ] | [ ] |
| Community | /Community | new | 0 | 0 | 0 | 0 | 0 | 0 | 0 | admin (sidebar + More) | [ ] | [ ] | [ ] | [ ] |
| CommunityDiscussions | /Community/discussions | new | 0 | 0 | 0 | 0 | 0 | 0 | 0 | via Community | [ ] | [ ] | [ ] | [ ] |
| CommunityDiscussion | /Community/discussion/{id} | new | 0 | 0 | 0 | 0 | 0 | 0 | 0 | via Community | [ ] | [ ] | [ ] | [ ] |
| CommunityNew | /Community/new | new | 0 | 1 | 1 | 0 | 2 | 0 | 0 | via Community | [ ] | [ ] | [ ] | [ ] |
| CommunityPosted | /Community/posted/{id} | new | 0 | 0 | 0 | 0 | 0 | 0 | 0 | via Community | [ ] | [ ] | [ ] | [ ] |

### Workstream sizes

| Workstream | Pages |
|---|---|
| A  Dealer onboarding and credit | 31 |
| B  Reports and stock | 17 |
| C  Guest house, welfare and SDWA | 38 |
| D  Approvals, schemes, CSR and budget | 17 |
| E  Settings, masters, logistics and profile | 23 |


## 3. Programme plan

### Phase 0: release what is done (this week)
1. Deploy the current `azure-deploy` head to the live web with `deploy/azure/deploy.ps1`. It carries the authorization fixes, local assets, session store and the renderer crash fix; the desktop layout is unchanged.
2. Provision the **staging** environment from `infra/azure/staging.parameters.json` and load it with `copy-db.ps1`, so functional checks never touch live data. Test builds point at it with `-p:SpicApiBaseUrl=https://<staging-api>/`.
3. Play Console internal testing track with the current `.aab`; TestFlight internal build from the Mac.

### Phase 1: foundation (one agent, sequential, about a week)
Shared building blocks that every page then consumes. Nothing page-specific is touched, and every block switches on by breakpoint, so desktop web is unaffected:
- Responsive tokens: one breakpoint scale (`--bp-phone: 767px`, `--bp-tablet: 1199px`) replacing the 17 ad-hoc values.
- App shell in MainLayout: role-based **bottom tab bar** plus a "More" grid on phone, **navigation rail** on tablet, the existing sidebar on desktop.
- `PageHeader` (title, back, actions) and `<PageTitle>` on every page.
- `BottomSheet` wrapper used by the existing drawers; `ActionBar` (sticky Save and Submit) for forms.
- `SpicDataTable` card mode below 768 px, sticky header with inner scroll on tablet.
- `Skeleton`, `EmptyState`, `Toast` service, offline banner, session-expiry warning dialog.
- Test harness: the device sweep extended to click primary actions on staging, plus browser sweeps at 375, 768, 1280 and 1920 px.

### Phase 2: pages in parallel (five agents, one per workstream, two to three weeks)
Each agent owns only the files in its workstream table, works in its own git worktree and branch (`native/<workstream>`), and may not edit shared components; requests for shared changes go to the coordinator. Per page the agent adopts PageHeader, ActionBar, card mode and bottom sheets, fixes loading, empty and error states, verifies checks 1 to 9 on the four surfaces, and ticks the row. A sixth **QA agent** runs the sweeps on every merged branch and reopens rows that regress. The coordinator merges one workstream at a time into `azure-deploy` and re-runs the full sweep.

### Phase 3: device features and new pages (overlapping late Phase 2)
Biometric unlock, camera capture, QR scanner, push notifications, Windows context menus and notifications. **New pages requested by SPIC are built directly on the Phase 1 components**, so they are native-ready from the first commit.

### Release rule
Each merged workstream goes to staging, then to a **zero-traffic revision** of the live web app (Container Apps traffic split), is smoke-tested at the revision URL, and only then receives full traffic. The mobile app follows through the internal testing tracks. Rollback is a traffic switch back to the previous revision.

## 4. Status log

### 2026-09-20 (overnight run)
- Phase 1 foundation merged: app shell (bottom tab bar + More sheet on phone, nav rail on tablet, PageHeader, tokens.css), data display (SpicDataTable card mode, Skeleton, EmptyState, StatCard), feedback (BottomSheet, ActionBar, Toast/Confirm, OfflineBanner, SessionExpiryDialog), `tools/ui-sweep` harness. Android hardware back walks the WebView history.
- Phase 2 merged for all five workstreams (A 31 pages, B 17, C 38, D 17, E 15): PageHeader everywhere, ActionBar on forms and wizards, card mode or `data-label` cards for tables on phone, drawers as bottom sheets, `alert`/`confirm` replaced by toasts and confirm dialogs, loading skeletons and empty states, 44 px targets, third-party placeholder images replaced by local assets. All layout rules are under phone/tablet media queries; desktop is pinned to the previous look.
- Verified so far (checks 1 and 2, read-only, no data created): every route renders on the Xiaomi test phone with no horizontal overflow, in both portrait (392 px) and landscape/tablet (832 px) layouts; Web, Windows and Android Release builds pass. Checks 3 to 11 need the staging environment and one login per role group.
- Known follow-ups: IFMS Auto Import API returns a non-JSON error body (now shown as a toast; root cause on the API/IFMS DB side); RMDValidationQueue still calls a hard-coded Supabase project (business decision); `RMDValidationQueueDrawer`, `MOSubmissionValidationDrawer`, `ApprovalPopup`, `EducationAssistance`, `SchemeApprovalDrawer` are unreferenced files; two CSR-1 mock-ups still coexist; role mapping for the bottom tabs awaits the product owner's correction in `Services/ShellNavigation.cs`.
- Bottom menu approved and applied (see docs/modules.md, "Phone bottom menu"): Home | role slots | More; Ask SPIC AI and Alerts moved to the phone/tablet top bar; new hub pages `/Activities`, `/Farmers`, `/Alerts`; `/DigitalLibrary/chat` opened to every role (library management stays admin-only).
- Community and Digital Library wired to the new API (checks 1-11 run against the local `spicone_dev` database on web at 392 px and 1366 px: create/edit/publish, uploads, replies, reactions, filters, paging, assistant). Phone verification of the API-backed pages waits for the production API deploy (the Android app targets api.spicone.in and blocks plain HTTP to a local API).
