---
module: sales-tracking
title: Sales plan and audit
icon: bi-graph-up-arrow
order: 105
category: Sales
color: #2563eb
example: DEMO-0425 · Dealer meet programme
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

After a CSR-2 programme is finally approved, its sales must be planned and checked. Make a product-wise sales plan, confirm it, save the dealer sales from SAP against it, and see at the end of the scheme if the target was met.

- plan | icon: bi-pencil-square | color: #2563eb
  title: Make the plan
  text: Set the scheme dates and the products, quantities and values you plan to sell.
- track | icon: bi-graph-up-arrow | color: #0d9488
  title: Save and track sales
  text: Upload SAP dealer invoices and save them against the plan every day.
- review | icon: bi-flag | color: #d97706
  title: Review at the end
  text: See if the Required Turnover was reached. If not, SMM can ask for more time.
:::

::: section flow
title: How a sales audit moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- owner | type: lane | color: #2563eb
  title: Programme owner
  sub: Responsible Person on the CSR-1
- data | type: lane | color: #64748b
  title: Sales data
  sub: SAP invoice Excel
- managers | type: lane | color: #0d9488 | roles: RM, SMM, SMD, Admin, SuperAdmin
  title: Managers
  sub: RM · SMM · SMD
- approvers | type: lane | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: Extension approvers
  sub: AVP · Admin
- start | lane: owner | col: 1 | icon: bi-play-circle | color: #64748b | next: plan | screen: audit-overview
  title: Start the audit
  status: Not Started
  text: On **Sales Audit**, press **Start Audit**. Check the programme and the budget, then press **Continue to sales plan**.
- plan | lane: owner | col: 2 | icon: bi-pencil-square | color: #94a3b8 | next: dealers | screen: sales-plan
  title: Make the sales plan
  status: Draft
  text: Enter the scheme dates and the products. Press **Save Draft** or **Save & Continue**.
- dealers | lane: owner | col: 3 | icon: bi-people | color: #0891b2 | next: confirm
  title: Map the dealers
  status: Draft
  text: On the **Dealers** step, choose the dealers whose sales count for this programme.
- confirm | lane: owner | col: 4 | icon: bi-check2-square | color: #2563eb | next: upload | return: plan | screen: plan-review
  title: Confirm the plan
  status: Plan Confirmed
  text: On **Review & Confirm Plan**, check everything and press **Confirm Sales Plan**. The plan is then locked. Before that you can go back with **Edit Sales Plan**.
- upload | lane: data | col: 5 | icon: bi-cloud-arrow-up | color: #64748b | next: save | screen: dealer-sales-upload
  title: Upload SAP sales
  status: New
  text: The SAP dealer invoice Excel is uploaded on **Dealer Sales List** with **Bulk Upload**.
- save | lane: owner | col: 6 | icon: bi-save | color: #d97706 | next: track | screen: daily-sales
  title: Save daily sales
  status: In Progress
  text: On **Review & Confirm Plan**, press **Refresh Sales** and then **Save Daily Sales**. New invoice lines become **Old** and add to the achievement.
- track | lane: managers | col: 7 | icon: bi-graph-up-arrow | color: #0d9488 | next: review | screen: tracking-list
  title: Follow the progress
  status: In Progress
  text: RM, SMM and SMD watch the sales of their area on **Sales Tracking**.
- review | lane: owner | col: 8 | icon: bi-flag | color: #16a34a | screen: period-end-review
  title: Period-end review
  status: Completed
  text: When the saved sales reach the **Required Turnover**, the audit shows **Completed**. **Period-End Review** shows the final result.
- extension | lane: approvers | col: 8 | icon: bi-calendar-plus | color: #dc2626 | kind: exception | from: review | to: track | screen: extension-review
  title: Ask for more time
  status: Pending
  text: If the target is not met, SMM presses **Request Extension**. AVP or Admin approves or rejects it. If approved, the scheme end date moves.
:::

::: section statuses
title: Every status you can see
kind: statuses

- not-started | color: #64748b
  label: Not Started
  text: The programme is approved and waiting. Press **Start Audit**.
- draft | color: #94a3b8
  label: Draft
  text: The audit is started. The plan is saved but not confirmed.
- plan-confirmed | color: #2563eb
  label: Plan Confirmed
  text: The plan is locked. Sales can now be saved against it.
- in-progress | color: #d97706
  label: In Progress
  text: Sales are being saved. The target is not reached yet.
- sales-tracking | color: #0d9488
  label: Sales Tracking
  text: The scheme period is running.
- period-expired | color: #ea580c
  label: Period Expired
  text: The scheme period ended before the target was reached.
- extension-request | color: #7c3aed
  label: Extension Request
  text: More time was asked for. Waiting for a decision.
- completed | color: #16a34a
  label: Completed
  text: The saved sales reached the Required Turnover. The audit is closed.
- ext-pending | color: #d97706
  label: Pending
  text: An extension request is waiting for AVP or Admin.
- ext-pending-admin | color: #d97706
  label: Pending Admin Review
  text: Shown on the tracking screen while your extension request waits for a decision.
- ext-approved | color: #16a34a
  label: Approved
  text: The extension was approved and the scheme end date was changed.
- ext-rejected | color: #dc2626
  label: Rejected
  text: The extension was refused. The scheme end date did not change.
- invoice-new | color: #0891b2
  label: New
  text: An SAP invoice line that is not saved to the audit yet.
- invoice-old | color: #475569
  label: Old
  text: An SAP invoice line already saved. It is never counted twice.
- met | color: #16a34a
  label: Met
  text: The planned value is at least the Required Turnover.
- not-met | color: #dc2626
  label: Not Met
  text: The planned value is below the Required Turnover. You cannot confirm the plan yet.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- owner | color: #2563eb
  title: Programme owner
  - Start the audit and make the sales plan
  - Map the dealers and confirm the plan
  - Save the daily sales
- data | color: #64748b
  title: Sales data upload
  - Upload the SAP dealer sales Excel
- managers | color: #0d9488 | roles: RM, SMM, SMD
  title: RM, SMM, SMD
  - See the sales tracking of your area
  - SMM: request an extension when the target is not met
- approvers | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: AVP and Admin
  - Approve or reject extension requests
:::

::: section faq
title: Problems and answers
kind: faq

- no-programme | popular: true
  q: My programme is not in the Sales Audit list
  a: The list shows only finally approved CSR-2 programmes where you are the Responsible Person on the CSR-1. Check the CSR-2 status and the Responsible Person.
- confirm-grey | popular: true
  q: The Confirm Sales Plan button is grey
  a: You need a scheme period, at least one product, **Requirement Status** showing **Met**, and at least one mapped dealer. Fix the missing part with **Edit Sales Plan** or **Edit Dealer Mapping**.
- zero-new
  q: Save Daily Sales shows 0 New
  a: No new SAP invoice lines match your dealers, products and scheme dates. Upload the latest SAP sales on **Dealer Sales List**, then press **Refresh Sales**.
- missing-id
  q: I see "Missing csr2Id"
  a: These screens must be opened from the **Sales Audit** or **Sales Tracking** list, not from a saved link. Go back to the list and open the programme again.
- extension-grey
  q: The Request Extension button is grey
  a: Only SMM can request an extension, from **Period-End Review**.
- upload-columns
  q: My Excel upload fails with "Missing columns"
  a: The first row of the Excel must have these names: Billing Document, Billing Date, Item, Customer Number, Customer Name, Product Code, Product Name, Quantity, Unit of Measure, Gross Amount.
:::

::: section audit-list
title: See my sales audits
routes: /SalesAudit
minutes: 2

**What this screen is for**
The **Sales Audit** screen lists the approved CSR-2 programmes you are responsible for, with their scheme period, turnover and status. Start or continue each audit from here.

1. Open **Filters** to choose **Sanction Number**, **Program Name**, **Location**, **Date Range** or **Status**, then press **Apply Filters**. **Clear All** removes them.
2. Look at the cards, such as **Total Programs**, **Not started**, **In Progress** and **Completed**, to see how many programmes are at each step.
3. In **Sales Audit List**, type in the search box to find a programme.
4. For each row, check **Scheme Period**, **Required TO (Auto)**, **Actual Turnover** and **Achievement**.
5. Press the button in the **Status** column: **Start Audit** for a new programme, **View / Continue** for one you started, or **View Achievement**, **Request Extension**, **View Request** or **View Audit**.

## Common problems

- **"No eligible sales audit records found".** Only finally approved CSR-2 programmes where you are the Responsible Person appear here.
- **The date filter does nothing.** Choose the dates, press **Done** in the date box, then press **Apply Filters**.
- **"Not Set" in Scheme Period.** The sales plan is not made yet. Press **Start Audit** or **View / Continue**.
:::

::: section audit-overview
title: Check the programme and budget
routes: /AuditOverview
minutes: 2

**What this screen is for**
**Audit Overview** is step 1 (**Program**). It shows the details from the approved CSR-2 and the linked CSR-1, and the budget that decides the **Required Turnover**.

1. On **Sales Audit**, press **Start Audit** or **View / Continue** on a programme.
2. Check the top strip: **Program Type**, **Program Name**, **Sanction Number** and **CSR-2 Status**.
3. Check **Program Information**: State, Region, HQ, the CSR-2 event date and the scheme dates.
4. Check **Program Budget & Turnover**. The **Required Turnover** is worked out from the **Total Approved Budget**.
5. Press **Save Draft** to start the audit, or **Continue to sales plan** to go to step 2.

> Tip: The rule is shown on the budget card: ₹1.00 Cr turnover for every ₹1.50 Lakh budget.

## Common problems

- **"Missing csr2Id".** Open this screen with **Start Audit** from the **Sales Audit** list.
- **"Access denied (403)".** You are not the Responsible Person for this programme.
- **Save Draft is grey.** The audit is already final-submitted.
:::

::: section sales-plan
title: Make the sales plan
routes: /SalesPlan
minutes: 4
tour: 1 > [data-help=st-plan-period] | 2 > [data-help=st-plan-add] | 3 > [data-help=st-plan-table] | 4 > [data-help=st-plan-summary] | 5 > [data-help=st-plan-actions]

**What this screen is for**
**Sales Plan & Scheme Period** is step 2. Set the dates of the scheme and the products, quantities and values you plan to sell.

1. Under **Scheme Period**, choose the **From Date** and **To Date**. **Duration** shows the months and days.
2. Press **+ Add Product** to add a row.
3. In each row, choose the **Product Group** and the **Product**, then enter the **Planned Quantity** and **Planned Value (₹)**. The bin icon removes a row.
4. Check **Plan Summary & Comparison**. **Requirement Status** must show **Met**: the **Total Planned Commitment** must be at least the **Required Turnover**.
5. Press **Save Draft** to keep your work, or **Save & Continue** to save and go to the **Dealers** step.

> Careful: After the plan is confirmed, this screen is locked. The button then says **Continue to Dealer Mapping**.

## Fields explained

| Field | What to enter |
|---|---|
| **From Date**, **To Date** | Start and end of the scheme. The end cannot be before the start. |
| **Product Group** | Choose it first. The **Product** list then shows its products. |
| **Product** | Each product only once. |
| **Unit** | Always MT. |
| **Planned Quantity** | Planned sales in MT. Not negative. |
| **Planned Value (₹)** | Planned sales value in rupees. Not negative. |

## Common problems

- **"Select a product and enter non-negative quantities and values for every row."** Fill every row, or remove empty rows with the bin icon.
- **"The same product cannot appear more than once."** Remove the repeated row.
- **"Scheme end date must not precede the start date."** Pick a To Date on or after the From Date.
- **Requirement Status shows Not Met.** Add products or raise the planned values until the total reaches the Required Turnover.
:::

::: section plan-review
title: Review and confirm the plan
routes: /PlanReview
minutes: 3
tour: 1 > [data-help=st-review-summary] | 2 > [data-help=st-review-budget] | 3 > [data-help=st-review-plan] | 4 > [data-help=st-review-status] | 5 > [data-help=st-review-actions]

**What this screen is for**
**Review & Confirm Plan** is step 4. Check the programme, budget, plan and dealers, then confirm the plan. You confirm only once; after that, sales tracking starts.

1. Check the top strip (**Program Type**, **Program Name**, **Scheme Period**, **Scheme Duration**) and the **Program Information** below it.
2. Check **Budget Details**: **CSR1 Approved Budget**, **Additional Approved Budget**, **Total Approved Budget** and **Required Turnover**.
3. Check **Product - wise Sales Plan & Summary**. To change the plan, press **Edit Sales Plan**.
4. In **Sales Achievement & Dealer Allocation**, **Requirement Status** must show **Met** and **Mapped Dealers** must be more than 0. To change the dealers, press **Edit Dealer Mapping**.
5. Press **Confirm Sales Plan**, then **Yes, Confirm & Save**. In the **Sales Plan Confirmed** box, press **View Tracking →** or **Back to Sales Audit**.

> Careful: After confirming, you cannot change the plan, the scheme dates or the dealers.

## Common problems

- **Confirm Sales Plan is grey.** Save a scheme period, make sure **Requirement Status** shows **Met**, and map at least one dealer.
- **"Missing csr2Id. Open Plan Review from Dealer Mapping."** Open the programme again from the **Sales Audit** list.
- **I want to stop and finish later.** Press **Save Draft**. Your plan and dealers stay as they are.
:::

::: section daily-sales
title: Save daily sales to the audit
routes: /PlanReview
minutes: 2

**What this screen is for**
After the plan is confirmed, come back to **Review & Confirm Plan** to add the new SAP sales to the achievement. Do this each time new sales are uploaded.

1. Make sure the latest SAP dealer sales are uploaded on **Dealer Sales List**.
2. Open **Review & Confirm Plan** for the programme. From **Sales Tracking Dashboard** you can press **Back to Plan Review**.
3. In **Matched Dealer Invoice Details**, press **Refresh Sales**. Each invoice line shows **New** (not saved yet) or **Old** (already saved).
4. Press **Save Daily Sales** at the bottom (it shows how many are New) or **Save Sales Progress** at the top, then press **Yes, Save Daily Sales**.
5. The **Daily Sales Saved Successfully** box shows the invoices saved and the **Cumulative Achievement**. Press **Continue** or **Back to Sales Audit**.
6. When the saved sales reach the Required Turnover, the button shows **Audit Completed** and the audit is closed.

> Tip: Old invoice lines are never counted twice, so saving again is safe.

## Common problems

- **Save Daily Sales is grey or shows 0 New.** There are no new invoice lines. Upload the next SAP sales and press **Refresh Sales**.
- **"SAP products excluded because no unambiguous planned-product name matched".** The SAP product name must match a product in your plan.
- **Some invoices are not counted.** Lines outside the scheme period, or with a unit other than MT, are left out.
:::

::: section dealer-sales-upload
title: Upload SAP dealer sales
routes: /DealerSalesUpload
minutes: 3
tour: 1 > [data-help=st-upload-btn] | 2 > [data-help=st-upload-btn] | 3 > [data-help=st-upload-search] | 4 > [data-help=st-upload-table] | 5 > [data-help=st-upload-export] | 6 > [data-help=st-upload-footer]
screenshot: dealer-sales-upload.png
hotspots: 1 @ 86.8,2.9,10.3,3.5 | 2 @ 86.8,2.9,10.3,3.5 | 3 @ 61.6,3,24.3,3.2 | 4 @ 2.9,11.4,94.2,12.3 | 5 @ 2.9,7.8,24.3,2.6 | 6 @ 2.9,24.9,94.2,3.1
screenshot-phone: dealer-sales-upload.phone.jpg
hotspots-phone: 1 @ 66.8,11.9,28.3,7.1 | 2 @ 66.8,11.9,28.3,7.1 | 3 @ 4.9,12.1,59.4,6.6 | 4 @ 4.9,29.1,90.3,25 | 5 @ 4.9,21.8,66.7,5.3 | 6 @ 4.9,56.6,90.3,23

**What this screen is for**
**Dealer Sales List** holds the dealer invoice lines from SAP. Upload the SAP Excel here; the sales audits use these lines to count the achievement.

1. Press **Bulk Upload** and choose the SAP Excel file (.xlsx or .xls, up to 5 MB).
2. Read the message: **New records**, **Already imported** and **Repeated in Excel**. If an **Upload errors** table appears, correct those rows in Excel and upload again.
3. Type an invoice, dealer or product in the search box to find lines.
4. Check the rows: **Billing Date**, **Billing Document**, **Dealer Code**, **Dealer Name**, **Product Name**, **Quantity**, **Unit** and **Gross Amount (₹)**.
5. Use **Copy**, **CSV**, **Excel**, **PDF** or **Print** to take the list out.
6. At the bottom, choose **Items Per Page**, move between pages, and see the total **Gross sales**.

> Tip: Uploading the same file again is safe. Lines already imported are skipped.

## Excel columns

| Field | What to enter |
|---|---|
| **Billing Document**, **Item** | Invoice number and line number. Required. |
| **Billing Date** | Invoice date. Must be a real date. |
| **Customer Number**, **Customer Name** | Dealer code and dealer name. Required. |
| **Product Code**, **Product Name** | Required. The name must match the planned product to count in an audit. |
| **Quantity**, **Unit of Measure**, **Gross Amount** | Not negative. Only MT lines count in an audit. |

## Common problems

- **"Missing columns".** The first row of the Excel must have all the column names above.
- **"Excel validation failed; no rows saved."** Read the **Upload errors** table (**Excel Row**, **Field**, **Reason**), fix those rows and upload again.
- **"Maximum 10,000 invoice rows allowed."** Split the file into smaller files.
:::

::: section sales-tracking
title: Follow sales against the target
routes: /SalesTracking
minutes: 3

**What this screen is for**
**Sales Tracking Dashboard** shows how much of one programme's Required Turnover is achieved. The programme owner opens it after confirming the plan. Managers open it read-only from the **Sales Tracking** list.

1. Open it with **View Tracking →** after confirming the plan, or with **View** on the **Sales Tracking** list.
2. Read the top tiles: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %**, **Remaining to Achieve** and **Tracking Status**.
3. If an **Extension Request** box appears, read its status. Press **View Request Status** to see the reason and remarks.
4. Look at **Turnover Achievement Overview** and **Monthly Progress Trend**. A warning appears when the scheme ends within 7 days.
5. Check the product table and the dealer tables. In **Dealer Sales Achievement - SAP Invoice Matches**, press **View Details** or **Hide Details** to show or hide the invoice lines.
6. The programme owner can upload an SAP invoice report under **Import actual sales achievements (SAP invoice report)**. A new upload replaces the earlier one.
7. Press **Download Tracking Report** for an Excel copy, or **Go To Final Review** to open the **Period-End Review**.

## Common problems

- **"Awaiting Sales Data".** No achievement is saved yet. Save the daily sales on **Review & Confirm Plan**.
- **The import box is missing.** It is hidden in the read-only view, before the plan is confirmed, and after the audit is final-submitted.
- **"Upload an XLSX or XLS file up to 5 MB."** Choose a smaller Excel file.
- **Go To Final Review is grey.** No product achievement is saved yet.
:::

::: section tracking-list
title: See sales tracking for my area
routes: /SalesTrackingList
minutes: 2
screenshot: tracking-list.png
screenshot-phone: tracking-list.phone.jpg

**What this screen is for**
The **Sales Tracking** menu opens a read-only list of programmes in your area. RM sees the region, SMM the HQ, SMD the state, and Admin sees all.

1. Open **Filters** to choose **Sanction Number**, **Program Name**, **Location**, **Date Range** or **Status**, then press **Apply Filters**. **Clear All** resets them.
2. Type in the search box to find a programme.
3. For each row, check **Scheme Period**, **Required TO (Auto)**, **Actual Turnover**, **Achievement** and **Document Status**.
4. Press **View** to open the **Sales Tracking Dashboard** of that programme.
5. Use **Items Per Page** and the arrows to move between pages.

## Common problems

- **An error says the role is not permitted or no area is assigned.** Your login needs the RM, SMM or SMD role with a region, HQ or state. Ask the administrator.
- **"From date cannot be after To date."** Correct the date range.
- **"No accessible Sales Audit records found."** Press **Clear All**. There may be no approved programme in your area yet.
:::

::: section period-end-review
title: Check the result at the end of the scheme
routes: /PeriodEndReview
minutes: 2

**What this screen is for**
**Period-End Review** is the last step of tracking. It shows whether the programme reached its **Required Turnover**, and lets SMM ask for more time if it did not.

1. On **Sales Tracking Dashboard**, press **Go To Final Review** or **View Final Review**.
2. Check **Program Summary** and the date in **Scheme period ends on**.
3. In **Final Program Summary**, read **Required Turnover (TO)**, **Planned Commitment**, **Actual Turnover Achieved**, **Balance to Target** and **Achievement**.
4. **Target Outcome & Next Step** shows **Target Achieved** or **Target Not Achieved**.
5. If the target is not achieved, SMM presses **Request Extension**. This opens the **Request Extension** screen to give the reason and the new end date.
6. Press **Back to Sales Tracking**, **Close** or **Back to List** to leave.

> Tip: This screen is for review only. It does not close the scheme.

## Common problems

- **Request Extension is grey.** Only SMM can request an extension.
- **Proceed to Final Review and Go To Final Review are grey.** The final review step is not available yet.
- **"Missing CSR2 ID".** Open this screen from **Sales Tracking**.
:::

::: section audit-details
title: Fill the plan and achievement sheet
routes: /SalesAuditDetails
minutes: 3

**What this screen is for**
**Sales Audit — Plan & Achievement** is one sheet with the scheme dates, budget, plan and actual sales. The **Sales Audit** list opens it with **View Achievement** or **Request Extension**.

1. On **Sales Audit**, press **View Achievement** or **Request Extension** in the **Status** column.
2. Check **CSR-2 Auto-Filled Program Information**.
3. Under **Scheme Period & Budget**, check **Scheme From**, **Scheme To** and **Additional Approved Budget (₹)**. **Required Turnover (1.5×)** and **Audit Status** are shown below.
4. Under **P — Sales Plan / Committee (Product-wise Breakup)**, press **+ Add Product** and enter the product, quantity and value. **Remove** deletes a row.
5. Under **SP — Sales / Achievement**, add the actual sales the same way. **Actual** and **Achievement** show the totals.
6. Press **Save Draft** to keep your work, or **Final Submit** to close the audit.

> Careful: After **Final Submit** the sheet is read only.
> Careful: Enter an **Additional Approved Budget** only after checking its separate approval.

## Common problems

- **"Complete scheme dates and product plan meeting the required target."** Final Submit needs both dates, at least one plan row, and plan and actual totals at least equal to the Required Turnover.
- **"Invalid scheme period."** Scheme To must not be before Scheme From.
- **"Invalid CSR-2 ID".** Open this screen from the **Sales Audit** list.
:::

::: section extension-list
title: See extension requests
routes: /ExtensionRequests
roles: AVP, Admin, SuperAdmin
minutes: 2
screenshot: extension-list.png
screenshot-phone: extension-list.phone.jpg

**What this screen is for**
**Extension Request List** shows the requests for more time raised when a programme did not reach its target. AVP and Admin review them here.

1. Look at the cards: **Total Request**, **Pending Request**, **Rejected** and **Approved**.
2. Open **Filters** to choose **Sanction Number**, **Program Type**, **Location**, **Request On** or **Status**, then press **Apply Filters**. **Clear All** removes them.
3. Type in the search box to find a request by ID, sanction number, programme, location or name.
4. For each row, check **Requested By**, **Scheme Period**, **Request New Date** and **Achievement**.
5. Press **View Request** to open a request.

## Common problems

- **"You do not have permission to view extension requests."** Only AVP and Admin can open this list.
- **"No records found".** Press **Clear All** in Filters.
:::

::: section extension-review
title: Approve or reject an extension
routes: /ExtensionRequestDetails
roles: AVP, Admin, SuperAdmin
minutes: 2

**What this screen is for**
**Extension Request Details** shows one request with the programme's performance and the reason for more time. Approve or reject it here.

1. On **Extension Request List**, press **View Request**.
2. Check the header: the status, **Request ID** and **Request On**.
3. Read **Scheme Performance**: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %** and **Remaining to Achieve**.
4. Read **Extension Request**: **Current Scheme Period**, **Request New End Date**, **Requested By**, the reason and the remarks.
5. Check **Dealer-wise Actual Sales**.
6. For a **Pending** request, press **Approve Extension** or **Reject Request**.
7. **Extension Approved Successfully** or **Extension Rejected Successfully** opens. Press **View Request Details** to go back to the list.

> Careful: The buttons act at once. There is no second confirm box. Approving changes the scheme end date.

## Common problems

- **No Approve or Reject buttons.** The request is already **Approved** or **Rejected**.
- **"This request is already approved or the scheme end date has changed. Reload to review it."** Reload the page and check again.
- **"Request not found".** Open the request again from the list.
:::
