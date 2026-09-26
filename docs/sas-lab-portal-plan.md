# SAS Portal, version 2: Lab Coordinator portal

Owner: product owner (deploys) and the coordinating agent (builds). Started 2026-09-27 from Figma
screens supplied as screenshots. Builds on version 1 (docs/sas-sample-collection-plan.md): field
staff collect samples, pay, and dispatch consignments; the lab now receives them, batches the
samples, runs the analysis and publishes reports.

## 1. Screens received so far

| # | Screen | Notes |
|---|---|---|
| 1 | Lab Coordinator Dashboard | 7 KPI cards (Total Consignments Received, Pending Batch Creation, Total Batches Created, Taken for Analysis, Analysis Completed, Completed Batches, Reports Generated); Recent Consignments table (Consignment No., Shipment Date, Sample Type Paid/Free, No. of Samples, State, Region, Status Batch Pending / Batch Created, view); Recently Created Batches table (Batch No., Consignment No., No. of Samples, Assigned To with avatar, Current Status Taken for Analysis / Analysis Completed / Completed, Analysis Days, view); "View All" on both |
| 2 | Consignment Details (list) | 5 KPI cards (Total Consignments, Pending Batch Creation, Batched Consignments, Water Samples, Soil Samples); Consignment List (Consignment No., Shipment Date, Sample Type, Soil Samples, Water Samples, Total Samples, State, Status, view) with search, status filter, items per page, pager |

| 3 | Create Batch (dialog) | Batch No (auto-generated), Consignment No., Batch Created Date, Assign To (user), Remarks; Cancel / Confirm. Product owner: a batch takes SEVERAL consignments, so the Consignment List gets checkboxes on Batch Pending rows and a "Create Batch (n)" button; the dialog shows the picked consignments as removable chips in a searchable multi-select |

| 4 | Batch Created Successfully (dialog) | Green tick, "Batch has been created successfully and moved to Analysis Tracking"; Batch No, Consignment No. (list when several), Batch Created Date, Assigned To; buttons View Batch Details, Go To Analysis Tracking |

| 5 | Batch Details (Analysis Tracking) | Header strip: Batch No., Consignment No., Total Samples, Assigned Date, Current Status, Analysis Days. Tabs: Overview, Samples (n), Parameters (n), Documents, Activity Log. Overview: Batch Progress stepper (Taken for Analysis, In Progress, Analysis Completed, Completed, each with its date); Sample Type Summary table (per sample type: Total, Completed n (%), Not Started n (%), Total row); Recent Samples table (Sample ID, Sample Code, Soil/Water counts, Parameter, Status Completed / In Progress / Not Started, Progress bar %) with View All; right column Batch Information (Batch No, Consignment No., Batch Created Date, Assigned To) and Timeline (each step with description, date-time and "By: name"; pending steps greyed) |

| 6 | Batch Details, Samples tab | 3 KPI cards (Total Samples n 100%, Not Started n %, Completed n %); Samples List with search, All Statuses, All Types, All Parameters, Date Range, Export; columns: checkbox, Sample ID (SAS-SOIL-001 / SAS-WATER-001), Sample Code (SS-001 / WS-001), Sample Type (Paid/Free), Soil Samples, Water Samples, Parameter (current/last parameter), Status (Completed / In Progress / Not Started), Progress bar %; items per page + pager. Row opens the sample's result entry (screen still to come) |
| 7 | Batch Details, Documents tab (n) | Document List with search, All Types, All Uploaders, Date Range, Upload; columns: checkbox, Document Name (file icon, name, "PDF • description"), Document Type (Batch Document / Sample Document / Reference Document), Uploaded By (avatar + name), Uploaded On (date + time), Full Size, status pill (Completed / Not Started), Actions (download, view, delete); pager |
| 8 | Batch Details, Activity Log tab | Vertical timeline with an icon per activity kind; columns Date & Time, Activity pill (Batch Created, Sample Received, Sample Logged, Parameter Assigned, Analysis Started, Analysis In Progress, Document Uploaded, Status Updated), Description, Performed By (avatar, name, role); filters search / All Types / All Uploaders / Date Range; pager. The Upload button in the mock is the Documents tab's; the log is system-written |

| 9 | Lab Reports (list, partly visible behind the drawer) | KPI card Total Reports; Filters card (Date Range, State, ...); Reports table (Batch No., Consignment No., ... with a view action); items per page + pager |
| 10 | Lab Report Details (right-side drawer) | Header "Lab Report Details", Report No. (REP-SAS-2026-001), close. Sections of label/value tiles: Report Summary (Report No., Batch No., Consignment No., Report Generated Date, Report Status pill "Report Ready"); Batch Details (Batch Created Date, Created By, No. of Samples, Current Status); Consignment Details (Shipment Date, Sample Type, State, Region, Head Quarters); Analysis Details (Assigned To, Assigned Date, Analysis Completed Date, Analysis Days, Final Completed Date, Final Remarks); Status Timeline (Created, Taken for Analysis, Analysis Completed, Completed, each with description, date-time, By). Footer: Close, Download PDF, Download Excel |

| 11 | Analysis Tracking (list) | 6 KPI cards with icon above the label (Total Batches, Created, Taken for Analysis, Analysis Completed, Completed, Delayed Analysis); Analysis List with search and All Statuses; columns Batch No., Consignment No., Total Samples, Assigned To (avatar), Assigned Date, Current Status pill, Analysis Days pill (Pending yellow, n Days green, red when delayed), view; items per page + pager. Delayed = in analysis longer than `Sas:Lab:DelayedAfterDays` (default 5) and not complete |

| 12 | Batch Details, Parameters tab | 3 KPI cards (Total Parameters, Completed, Not Started); Parameters List with search, All Statuses, All Types, All Parameters, Date Range, Export; columns in the mock: checkbox, Parameter Code (N-001, WTDS-001, MB-001), Parameter Name, Sample Type, Soil/Water counts, Unit, Reporting Limit, Status, No. of Samples. **Product owner: parameters are shown per sample, one sample at a time, not as one flat list** -> the tab lists the batch's samples; each expands to its parameter rows (code, name, unit, reporting limit, entered value, status). KPI cards count parameter rows across the whole batch |

Sidebar of the lab portal: Dashboard, Consignment Details, Analysis Tracking, Lab Reports.
### Lab Analyst role (sidebar: Dashboard; Test Value Entry > Batch List, Sample-wise Entry; Reports > ...)

| # | Screen | Notes |
|---|---|---|
| 13 | Analyst Dashboard | Subtitle "Manage assigned lab batches, enter test values, preview results, and generate reports." 5 KPI cards (Assigned Batches, Pending Value Entry = samples waiting for value entry, Auto Result Ready = samples with range-based result generated, Reports Generated this FY, Previous FY Reports); Today's Work Priority list (Pending value entry batches [High] n items -> Entry Now; Auto result ready for review [Medium] -> Preview; Generated reports for download [Low] -> View Reports); Quick Actions tiles (Go to Batch List, Continue Test Entry, View Test Reports, View Previous FY Reports) |
| 14 | Test Value Entry, Batch List | Subtitle "View assigned lab batches and start sample-wise test value entry." 4 KPI cards (Assigned Batches, Pending Value Entry, Auto Result Ready = batches ready for result preview, Report Generated); collapsible Filters card (Date Range, Sample Type, Status, Priority, Financial Year, Assigned By; Clear All / Apply Filters); Assigned Batch List: Batch No., Consignment No., Sample Type, No. of Samples, Pending Entries, Completed Entries, Assigned On (date + time), Priority pill (High / Medium / Low), Status pill (Pending Entry / In Progress / Auto Result Ready / Report Generated); row opens sample-wise entry |

| 15 | Test Value Entry, Sample-wise Entry | Subtitle "Enter sample test values, compare with normal range, and preview recommendations." Batch Summary tiles (Batch No., Consignment No., Sample Type, Total Samples, Assigned On; Pending, Completed, Priority, Status); Samples in this Batch table (Sample No., Farmer Name with avatar, Crop, Village, Entry Status Pending Entry / In Progress / Auto Result Ready / Report Generated, action icon: edit / continue / preview / view); Selected Sample tiles (Farmer Name, Mobile No, District, Village, Crop, Sample Type, Collected On, Entry Status); note "Enter only the test values. Normal range, result, status, and recommendation will be auto-calculated by the system."; Test Value Entry Table (Test Parameter, Normal Range, Test Value input, Unit, Result word e.g. Neutral / Safe / Low / Medium / High, Status pill Normal / Deficient / Moderate / Excess, Recommendation Hint); Auto Result Summary (per parameter result word + Overall Soil Status pill e.g. Needs Improvement); Recommendation Preview (Fertilizer / Organic / Micronutrient recommendation bullet groups + Crop Suitability Note); footer Back to Batch List, Save Draft, Preview Result, Submit Values |
| 16 | Submit Test Values? (dialog) | "Once submitted, test values will be locked for report preview. You can edit only before report generation."; Sample No., Farmer Name, Sample Type, Overall Soil Status; Cancel / Submit |
| 17 | Submitted Successfully (dialog) | "Values are locked and ready for report preview."; same four rows; Cancel / Preview Report |

| 18 | Reports, Test Reports (analyst) | Subtitle "View batch-wise report summaries and open sample-wise reports." 4 KPI cards (Total Batches = batch-wise report groups, Total Sample Reports, Reports Generated Today, Downloads Completed = PDF downloads); collapsible Filters (Financial Year, Date Range, Sample Type, State, Region, Head Quarters; Clear All / Apply); Batch-wise Report Summary table: Batch No., Soil Type (Paid/Free), Soil Samples, Water Samples, Total Samples, Reports Generated, Pending Reports, State, Region; row opens Batch Report Details |
| 19 | Batch Report Details (analyst) | Title "Batch Report Details – BAT-…", subtitle "View all sample-wise reports generated inside this selected batch.", button Download All Reports (split button: language choice); 5 KPI cards with icon above (Total Samples, Soil Samples, Water Samples, Reports Generated, Downloads Completed); Batch Information tiles (Batch No., Consignment No., State, Region, Head Quarters, Date of Dispatch, Financial Year 2025–26); info banner "Each sample in this batch has its own individual report…"; Filters (Sample Type, Status, Crops, Villages); Sample-wise Reports table: Sample No., Farmer Name (avatar), Crop, Village, Sample Type, Reports No. (RPT-SAS-2026-001), Generated Date, Report Status pill (Generated / Downloaded / Printed), Action: View, Download PDF split button (language) |
| 20 | Reference: current lab report PDF (`report (12).pdf`, 6 pages) | Soil report in English, Tamil, Marathi: header "SPIC AGRICULTURE SERVICES, SPIC Ltd, Muthaiyahpuram, Tuticorin - 628005, Phone…" with SPIC and GreenStar logos; title "SOIL SAMPLE ANALYTICAL REPORT"; farmer block (Farmer Name, Address, Mobile Number | Survey Number, Sample Number, Lab Number, Batch Number); parameter table Parameters / Result / Response / Optimum Range (EC, pH, Texture (text), Organic Carbon, Organic Matter, Nitrogen, Phosphorous, Potash, Calcium, Manganese, Sulphur, Copper, Zinc, Iron); Recommendations bullets; "Recommendations (Kg/acre)" schedule: Basal Application per crop (SPIC Jyoti, SPIC Gypsum, SPIC Sangamam, SPIC DAP, SPIC Urea, Potash, SPIC Zinc Sulphate, Ferrous Sulphate, Manganese Sulphate, Copper Sulphate) and Top Dressing (1st Application 90th day, 2nd 150th day, 3rd 210th day: SPIC DAP, SPIC Urea, Potash); footer "!! Healthy Soil. Wealthy Farmer. !!", signature, "OFFICER, SPIC AGRICULTURE SERVICES". Irrigation water report in English, Tamil, Telugu: farmer name & address, Date, Lab No; title "IRRIGATION WATER ANALYTICAL REPORT"; table S.No / Parameters / Result / Remarks (pH, EC (dS/cm), Bicarbonates, Chlorides, Calcium, Magnesium, Sodium, Potassium (m.eq/l), Sodium absorption ratio, Residual sodium carbonate, Total Hardness); Recommendations; Note ("Suitable for irrigation"); "AUTHORIZED SIGNATORY, SPIC SOIL TESTING LAB" |

### Admin role (main app sidebar)

| # | Screen | Notes |
|---|---|---|
| 21 | Lab Tracking (admin) | Subtitle "Admin view to monitor consignments, batches and analysis progress across the lab workflow." 4 KPI cards (Total Batches = all batches in system, Batch Created = batch created in lab, Taken for analysis = analysis in progress, Analysis Completed = completed batches); status tabs with counts (All Batches, Batch Created, Taken for Analysis, Analysis Completed); collapsible Filters (Date Range, Batch No., Consignment No., Assigned To, State, Region, Current Stage; Clear All / Apply); Lab Tracking List with search: Batch No., Consignment No., Sample Type, No. of Samples, Soil, Water, Assigned To (avatar), Current Status pill, Analysis Days, Last Updated (date + time), view (opens the coordinator's Batch Details read-only); items per page + pager. Page key `LabTracking` (module "SAS Lab"), route `/Lab/Tracking`; it reads `api/Lab/batches` with `batchId`, `consignmentId`, `stateId`, `regionId` filters and `api/Lab/batches/stats` |

| 22 | Batch View Details (admin, "old design", product owner is fine with it for admin batch review) | Title, "Batch Created on …", Export button. Header strip: Batch No., Consignment No. (with Delivered On), Test Samples (count, Soil:n Water:n), Samples (n tests), Assigned To (Analyst), Batch Created On (date + time), Last Updated. Three cards: Source Details (Collected By (role), Mobile No., Location, Collection Date), Consignment Details (Consignment No., Dispatch Date, AWB / Tracking No (link), Expected Delivery Date), Batch Details (Batch No., Total Samples, Soil Sample, Water Sample, Batch Created By (Lab Coordinator), Batch Created Date, Assigned Analyst). Analysis Process table (S.No, Sample Type, Total Samples, Completed, Not Started, Completed % bar, Total row). Horizontal Timeline: Consignment Delivered to Lab, Lab Entry Created (consignment received in the lab), Batch Created, Taken for Analysis, Analysis Completed, Report Generated, each with date-time and "by name"; pending steps grey with a dashed line. Route `/Lab/Tracking/{batchId}`; read-only |

| 23 | Payment Approval (admin list) | Subtitle "Admin reviews payment proofs for paid samples and forwards approved payments to Finance." 4 KPI cards (Pending Admin Approval, Forward to Finance, Returned / Rejected, Finance Verified); tabs with counts (All Payments, Pending Admin Approval, Forwarded to Finance, Returned / Rejected, Finance Status); Filters (Date Range, State, Region, Payment Mode, Status); Payment Approval List with search: Payment Ref No. (PAY-2026-00124), Collection No., Consignment No., MO Name (avatar), Farmer Name (avatar), Mode (UPI / Bank Transfer), Expected Amount, Paid Amount, Difference (green 0 / red), Admin Status pill (Pending Review / Returned to MO / Admin Approved), Finance Status pill (Not Forwarded / Pending / Verified), Submitted date, view; pager |
| 24 | Payment Approval Details (admin) | Header: Payment Ref No., Submitted date, pills "MO Submitted" and "Pending Admin Approval". Cards: Source Details (Payment Ref No., Consignment No., Sample Collection No., Payment Status, Submitted Date, Submitted By (role)); Farmer Details (Name, Mobile, State, Region, District, Village, Address); Sample Details (Total Samples, Soil Sample, Rate per Sample, Total Payable Amount, Collected By, Collection Date, Consignment Created); Payment Details (Payment Mode, Transaction ID, Transaction Date, Bank Name, UTR No., MO Remarks; tiles Expected / Paid / Difference); Admin Review Status (Current Status, Reviewed By, Verification Note, Forward To); Status Timeline (MO Submitted Payment, Admin Review Pending, Finance Verification Pending, Finance Payment Verified). **Payment Proof Preview: the receipt image uploaded by the MO (v1 `SamplePayment.ProofPath`), shown inline with a full-size viewer.** Footer: Reject Payment, Approve & Forward to Finance |
| 25 | Approve & Forward to Finance (dialog) | Summary rows (Payment Ref No., Farmer Name, Consignment No., Expected Amount, Paid Amount, Transaction ID, Payment Mode); Verified Amount (select), Forward To (Finance Team), Approved Date, Remarks, checkbox "I confirm that the payment proof, transaction details and amount are verified."; Cancel / Approve & Forward |
| 26 | Approve & Forwarded Successfully (dialog) | Rows: Payment Ref No., Farmer Name, Consignment No., Approved Amount, Forwarded To, Admin Status pill, Finance Status pill (Awaiting Verification), Forwarded On; Cancel / Go to Payment Approval List |

| 27 | Payment Verification (Finance list) | **Same page as 23 in Finance mode (product owner: do not create it again).** Subtitle "Admin-approved paid sample payments awaiting Finance verification." KPIs (All Payments, Pending Verification, Verified Payments, Failed/Mismatch Payments); tabs (All Payments, Pending Verification, Verified Payments, Failed Payments); same Filters; list columns Payment Ref No., Consignment No., Farmer Name, Mode, Amount, Admin Status, Finance Status (Pending / Mismatch / Verified), Submitted, view. Finance sidebar: Dashboard, Payment Verification, Payment History, Reports |
| 28 | Payment Proof Details (Finance) | **Same details page as 24 in Finance mode.** Header pills Admin Approved / Verified; cards with icons: Payment Summary (adds Admin Status, Finance Status), Farmer Details, Sample Details, Payment Details (adds Admin Remarks), Admin Verification (Admin Status, Verified By, Verified Date & Time, Admin Remarks, Forwarded to Finance), Status Timeline (MO Submitted Payment, Admin Reviewed Payment, Admin Approved & Forwarded to Finance, Finance Verification Pending, Finance — Payment Verified); Payment Proof Preview (uploaded receipt). Finance actions: Verify Payment, Mark Mismatch (with remarks) |

| 29 | Confirm Payment Received (Finance dialog) | Summary rows (Payment Ref No., Farmer Name, Consignment No., Expected Amount, Paid Amount, Transaction ID, Payment Mode); Verified Amount (select), Received Date, Remarks, checkbox "I have verified the payment proof, transaction ID, and amount."; Cancel / Confirm. A verified amount below the expected amount records Mismatch with the shortfall as the remark ("₹100 short") |
| 30 | Payment History (Finance) | Subtitle "Full record of all Finance-processed payments." Same KPIs; tabs All / Verified / Failed / Mismatch; Filters (Date Range, Payment Mode, Status); list: Payment Ref No., Consignment No., Farmer Name, Mode, Amount, Finance Status (Payment Verified / Amount Mismatch / Failed), Verified By (avatar, "Vue (FO)"), Verified Date, Remarks, view. Route `/Sas/Payments/History` (same page component, history mode) |

Page keys: `SasPaymentApproval` (admin mode) and `SasPaymentVerification` (finance mode); one
page pair `/Sas/Payments` and `/Sas/Payments/{id}` renders the mode from the key the user holds
(admins hold both and see the admin actions until forwarded, then the finance status read-only).
Payment History = the same list filtered to Verified / Rejected; Finance Reports = later.

### Farmer role (sidebar: Dashboard, My Samples, My Reports, Payments)

| # | Screen | Notes |
|---|---|---|
| 31 | My Samples (farmer) | Subtitle "All soil samples submitted through your MO officer." Toolbar: search, date range, All Types, All Statuses, All Crops. Card per sample: Sample No. (SMP-2026-00124), "Consignment: CON-… · Collected by MO name · date", crop pill on the right; status pills Paid / Free, Under Analysis / Report Ready, "Report: Not Ready"; actions View Details and, when the report is ready, Download PDF split button (language). Route `/Sas/MySamples`; uses the v1 farmer scoping (`SasFarmer.UserId`) and `api/Lab/reports/{id}/pdf?lang=`. My Reports = the same list filtered to Report Ready; Payments = the farmer's own payment records (read-only) |

| 32 | Sample Details (farmer) | **Product owner: use the existing v1 Sample Collection Details page (`/SampleCollection/view/{id}`), do not create a new one.** Additions to that page for the farmer view: header pills (Under Analysis / Paid Sample / Report: Not Ready), cards with icons Sample Details (Total Samples, Soil Sample, Rate per Sample, Total Payable Amount, Collected By, Collection Date, Consignment Created, AWB / Tracking No), Farmer Details, Payment Details (Payment Amount, Mode, Transaction ID, Transaction Date, Admin Status, Finance Status); Payment Proof Preview (receipt image); farmer-worded timeline: Sample Collected by MO, Sent to Lab (courier + AWB), Lab Received, Assigned for Testing, Under Lab Analysis ("Results expected within n days"), Report Generated, Report Ready for Download |
| 33 | My Reports (farmer) | Subtitle "Download your soil test reports in Tamil or English." KPI cards (Total Reports, Ready to Download, Downloaded, Under Process); Reports List with search, Date Range, All Statuses, All Crops: Reports No., Sample No., Crop, Sample Type, Generated Date, Status (Report Ready / Downloaded), Download PDF split button (Tamil / English), view; pager. Route `/Sas/MyReports` |

| 34 | Payments (farmer) | **Same payment list page in farmer mode.** Subtitle "Track payment status for all your paid soil samples." KPI cards (Total Paid Samples, Finance Verified, Approval Pending, Payment Issues); Payment List with search, Date Range, All Statuses, All Crops: Sample No., Crop, Amount, Mode, Transaction ID, Submitted Date, Status (Finance Verified / Admin Approval Pending / Payment Issue), view (same details page, read-only). Route `/Sas/Payments` resolves the mode from the user: farmer (own samples), finance, admin |

| 35 | Payment Details (farmer drawer) | **Same payment details in farmer mode**, as a right-side drawer over the list: header "Payment Details" + status pill, Sample No.; Payment Details card (Sample ID, Crop Name, Payment Amount, Payment Mode, Transaction ID, Payment Date, Submitted By, Admin Status); Payment Proof (receipt image with Download and Print); Verification Journey timeline (Payment Submitted, Admin Approved, Finance Verification, Payment Confirmed with descriptions and date-times). Read-only |

Farmer downloads offer Tamil and English; the lab and analyst downloads offer every configured
language (`Sas:Lab:ReportLanguages`, default en, ta, te, mr; extendable once fonts are in).

**Payment model (extends v1 `SamplePayment`):** `Code` (`PAY-{yyyy}-{00001}`), `PaymentMode`
(UPI / BankTransfer / Cash), `TransactionDate`, `BankName`, `MoRemarks`; admin side keeps v1
`Status` (Pending = Pending Review, Approved = Admin Approved, Rejected = Returned to MO) plus
`VerifiedAmount`, `ForwardedToName` ("Finance Team"), `ForwardedAt`, `ApprovedDate`,
`AdminRemarks`; finance side `FinanceStatus` (NotForwarded, AwaitingVerification, Verified,
Rejected), `FinanceVerifiedAt`, `FinanceVerifiedByName`, `FinanceRemarks`. Page key
`SasPaymentApproval` (module "SAS"), routes `/Sas/Payments` and `/Sas/Payments/{id}`. The v1
approve/reject on the collection details page keeps working and now also stamps these fields.

**Reports model (revised after the reference PDF):** `LabReport` is per SAMPLE (Code
`RPT-SAS-{yyyy}-{001}`, BatchId, SampleItemId, GeneratedAt, Status Generated / Downloaded /
Printed, DownloadCount, LastDownloadedAt, PrintedAt); the batch carries `ReportCode`
(`REP-SAS-…`) and `ReportGeneratedAt` for the coordinator drawer. PDF/Excel endpoints take
`lang=` (English plus the Indian languages the product owner confirms); labels, response words
and recommendation lines come from a `LabTranslation` table (Key, Lang, Text) seeded from the
reference PDF (English and Tamil first). Soil and water use two QuestPDF layouts that copy the
reference. The fertilizer schedule comes from an admin-editable `LabCropRecommendation` master
(Crop, Stage: Basal / TopDressing1 / 2 / 3 with DayNumber, Product, KgPerAcre), seeded with the
Banana example; result-dependent adjustments wait for the product owner's rules. `LabParameter`
gains `ValueType` (Numeric / Text with `Options`, e.g. Texture) and derived parameters (Organic
Matter = Organic Carbon x 1.724). Fonts for Indian scripts: Noto Sans (pending approval to add).

Auto-result rules live on `LabParameter` (RangeMin/RangeMax, Low/Normal/High labels and hints,
optional ModerateFrom, RecommendationGroup). Sample statuses: NotStarted = Pending Entry,
InProgress = draft saved, Completed = values submitted (Auto Result Ready; still editable until the
batch report is generated), "Report Generated" is shown once the batch is Completed. Overall Soil
Status: Good when no parameter is Deficient/Excess, Needs Improvement when at most two are,
Poor otherwise. Crop Suitability Note is composed from the crop and the deficient/excess
parameters.

Model consequences: `SampleBatch.Priority` (Low / Medium / High, chosen in the coordinator's Create
Batch dialog, default Medium) and `AssignedByName`; analyst status wording maps onto
`SampleBatchStatus` (TakenForAnalysis = Pending Entry, InProgress, AnalysisCompleted = Auto Result
Ready, Completed = Report Generated); the auto result is computed from the parameter's numeric
range (`LabParameter.RangeMin/RangeMax`) when a value is entered (below = Deficient, inside =
Normal, above = Excess); Financial Year = April to March.
Screens still to come: Analysis Tracking, Lab Reports, the consignment view, batch creation and
the pages behind "View All".

## 2. Model (draft, to be confirmed against the remaining screens)

- `SampleBatch`: Id, Code (`BAT-SAS-yyyy-nnn`), BatchDate, SampleCount (sum over its
  consignments), AssignedToUserId, AssignedToName, Remarks, Status (Created, TakenForAnalysis,
  AnalysisCompleted, Completed), CreatedAt, AnalysisStartedAt, AnalysisCompletedAt, CompletedAt,
  ReportGeneratedAt, CreatedBy/UpdatedAt. "Analysis Days" = AnalysisCompletedAt (or now) minus
  AnalysisStartedAt; "Pending" until started.
- `SampleConsignment.BatchId` (nullable): a consignment belongs to at most one batch; a batch has
  one or more consignments. Tables that show one Consignment No. per batch show the first and
  "+n"; the batch view lists them all.
- Batch status gains `InProgress` (first parameter result entered) between TakenForAnalysis and
  AnalysisCompleted (all samples completed); `Completed` when the coordinator closes the batch
  (report generated).
- `LabParameter` (master, seeded): Code (N-001, WTDS-001, MB-001 ...), Name, Unit, NormalRange,
  ReportingLimit, applies to Soil / Water / Both, SortOrder, IsActive. "Parameters (26)" on a
  batch = the parameter rows across its samples (each sample carries the parameters that apply
  to its type).
- Per-sample analysis: `SampleItem` gets AnalysisStatus (NotStarted, InProgress, Completed),
  AnalysisStartedAt, AnalysisCompletedAt; progress % = entered results / applicable parameters;
  results stay in `SampleLabResult` (v1), one row per sample and parameter.
- `LabActivity` (Activity Log / Timeline): BatchId, Kind, Title, Description, ByName, At; written
  on every transition (reuses the `SasStatusEvent` pattern).
- `LabDocument`: BatchId, Kind (Batch / Sample / Reference), FileName, Description, StoredPath,
  ContentType, Size, UploadedByUserId/Name, CreatedAt (Documents tab; files under
  `Uploads/Sas/Lab/...` served by the existing `api/Sas/file`).
- `LabReport`: Code (`REP-SAS-yyyy-nnn`), BatchId, GeneratedAt, GeneratedByName, Status
  (Ready), FinalRemarks, FinalCompletedAt. Generated when the coordinator completes a batch
  (with the final remarks entered then); one report per batch. PDF and Excel are rendered on
  the API from the report, batch, consignment and results (`api/Lab/reports/{id}/pdf|xlsx`),
  reusing the v1 result rows; the v1 Sample Collection Details page for dealers and farmers
  keeps reading `SampleLabResult`, so field users see the same results.
- Consignment lab status is derived, not stored: Delivered with no batch = Batch Pending; with a
  batch = Batch Created; all batches completed = Completed. The existing `ConsignmentStatus` and
  `SampleCollectionStatus` (DeliveredToLab, TestInProgress, Completed) are advanced by the batch
  transitions so the field staff's timeline and the dealer/farmer details page keep working.
- Lab results stay in `SampleLabResult` (v1); Lab Reports will read them per batch.

## 3. Access

New page keys appended to `PagePermission`, module "SAS Lab": `LabDashboard`, `LabConsignments`,
`LabAnalysis`, `LabReports`. Granted through Designation (a "Lab Coordinator" designation); admins
see everything. Routes: `/Lab` (dashboard), `/Lab/Consignments`, `/Lab/Consignments/{id}`,
`/Lab/Analysis`, `/Lab/Reports`. API: `api/Lab/...` (contract to be fixed in
`SPIC.Core/DTOs/LabDtos.cs` before the agents start).

## 4. Build plan (phase 0 done by the coordinator on 2026-09-27; phases 1a-1f in parallel)

Fixed contracts (do not change without the coordinator): `SPIC.Core/Entities/SampleCollection.cs`
(lab section, payment fields), `SPIC.Core/DTOs/LabDtos.cs`, `SPIC.Core/DTOs/SasPaymentDtos.cs`,
migration `V5o_SasLabPortal` (applied to the local database; NOT to prod yet), page keys in
`PagePermission`, routes in `ShellNavigation.cs` / `NavMenu.razor` / `PageGuard.razor`, config
`Sas:Lab` in `SpicAPI/appsettings.json`, the client wrapper `Shared/Services/LabApi.cs` and the
shared components under `Shared/Components/Lab/`.

| Phase | Agent (Opus) | Owns (exclusively) | Delivers |
|---|---|---|---|
| 1a | API-Lab | `SpicAPI/Controllers/LabController.cs`, `Spic.Infrastructure/Services/Lab/*` (auto-result engine, activity writer, code generators) | every `api/Lab/...` route except reports: me, dashboards, analysts, consignments (list/detail/receive), batches (stats/list/create/detail/assign/status), samples (stats/list/export), parameters (stats/list/export), documents, activities, sample entry (get/values/preview), financial years, languages |
| 1b | API-Reports | `SpicAPI/Controllers/LabReportsController.cs` (route prefix `api/Lab/reports`, `api/Lab/batches/{id}/report*`), `Spic.Infrastructure/Services/LabReports/*` (QuestPDF soil + water layouts, ClosedXML, translations), `SpicAPI/Fonts/*`, `LabTranslation` seed script | report creation on batch completion (called from 1a through a shared service interface defined by 1b in `SPIC.Core/Interfaces/ILabReportService.cs`), report lists/detail, PDF/XLSX per language, batch download, printed |
| 1c | API+Client-Payments | `SpicAPI/Controllers/SasPaymentsController.cs` (`api/Sas/payments/...`), `Shared/Services/SasPaymentApi.cs`, `Shared/Pages/Sas/Payments/*` | payment approval / verification / history / farmer payments (one list page and one details page with modes, approve, reject, verify, mismatch dialogs, proof preview) |
| 1d | Client-Coordinator | `Shared/Pages/Lab/Coordinator/*`, `Shared/Components/Lab/Batch/*` | Lab Dashboard (coordinator variant), Consignment Details list + Create Batch + success dialog, Analysis Tracking list, Batch Details tabs (Overview, Samples, Parameters per sample, Documents, Activity Log), Lab Reports list + report drawer |
| 1e | Client-Analyst | `Shared/Pages/Lab/Analyst/*` | Analyst Dashboard, Batch List, Sample-wise Entry (+ Submit / Submitted dialogs), Test Reports, Batch Report Details, Previous FY Reports |
| 1f | Client-Admin+Farmer | `Shared/Pages/Lab/Admin/*`, `Shared/Pages/Sas/Farmer/*`, farmer additions to `Shared/Pages/Sas/SampleCollectionDetails.razor` (+css) | Lab Tracking list + Batch View Details; farmer My Samples, My Reports, v1 details page additions |
| 2 | coordinator | merges, `Lab.razor` dashboard switch (coordinator / analyst variant by `api/Lab/me`), role sweeps (admin, coordinator, analyst, finance, farmer, MDO at 375 / 1280), phone check, docs | release on Satham |
| 3 | product owner | `migrate.ps1`, `deploy.ps1`, Designation grants (Lab Coordinator: LabDashboard, LabConsignments, LabAnalysis, LabReports; Lab Analyst: LabDashboard, LabTestEntry, LabReports; Finance: SasPaymentVerification; admins: SasPaymentApproval, LabTracking), fonts, fertilizer rules | production |

Phase 2 reconciliation list (coordinator), collected from the workstream reports:
- Sample display id: LabController numbers samples per batch (`SAS-SOIL-001`), the report renderer
  uses the item id (`SAS-SOIL-043`); make the PDF's Lab Number use the batch numbering.
- Crop suitability note wording differs between the entry preview (LabAutoResultEngine) and the
  report detail (LabReportRules); the report must reuse the engine.
- CORS: expose `X-Report-Language-Fallback` so the web client can tell the user a PDF fell back
  to English.
- `reports/stats.BatchGroups` counts every batch in scope; the analyst screen wants batches with
  reports only (decide with the product owner).
- Translations marked `// review` in `LabTranslationSeed.cs` need a native speaker (Telugu soil
  labels, Marathi "Recommendations", texture terms).
- Fonts: `SpicAPI/Fonts/` is empty until the Noto Sans files are approved and added.
- Payments: Finance's verified amount overwrites the admin's (one `VerifiedAmount` column) and the
  received date is folded into `FinanceVerifiedAt`; add `FinanceVerifiedAmount` and
  `FinanceReceivedDate` in a follow-up migration (V5p) and split them in the controller/timeline.
  "Failed" and "Amount Mismatch" both store `FinanceStatus.Mismatch`; the History "All" tab
  guesses from the remark. Farmer status for admin-approved-but-unverified reads "Admin Approved".

Rules for every agent: local database only (`spicone_dev`, local ports assigned per agent), QA
accounts, no live API; phone-first checks at 375 px; sweeps clean; commit in the worktree; report.

## 5. Status log

- 2026-09-27: phase 0 (7d89acf, b945f00); API-Lab merged (b1227c7, 486 checks); API-Reports merged
  (4b4fd3a, 48 + 21 checks, PDFs checked against the reference); Payments merged (59657be; guard
  fix for farmers / MOs); admin + farmer pages (b688dcb); analyst pages (fa5a487, report
  follow-up 8ff3d21 with `ValueType` / `Options` / `IsDerived` on parameter rows); coordinator
  pages (ed0b681, report follow-up e319ab8; coordinators may open the sample entry page
  read-only, 10ee406 / 62e0473). Reconciliation agent running (items above); farmer verification
  round running. Local QA users: qa.labcoord (designation 6), qa.analyst / qa.analyst2 (7),
  qa.finance (8). Migration V5o applied to the local database only.

## 6. Principles

Same as v1: no live data (local PostgreSQL only), one additive migration, product owner runs
`migrate.ps1` then `deploy.ps1`, phone <= 767.98px checked for every page, sweeps per role.
