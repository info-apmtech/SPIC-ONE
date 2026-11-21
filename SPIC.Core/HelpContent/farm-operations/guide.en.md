---
module: farm-operations
title: Farm operations
icon: bi-tree
order: 109
category: Soil testing
color: #15803d
example: DEMO · 01 - 15 Nov 2026 report
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Keep the accounts of the SPIC farm in one place. Set the products and their rates, record stock, sales and expenses every 15 days, and see the income, expense, profit and stock for the whole year.

- products | icon: bi-tags | color: #15803d
  title: Products and rates
  text: The Farm Office asks for a new product or a new rate. The Farm Admin approves it.
- report | icon: bi-journal-text | color: #d97706
  title: A report every 15 days
  text: For 1 to 15 and 16 to the month end, enter the inventory, income and expense, then submit.
- summaries | icon: bi-graph-up-arrow | color: #2563eb
  title: Summaries for the year
  text: See the finance, inventory, income and expense summaries, and download them as Excel.
:::

::: section flow
title: How the farm accounts move, and who acts
kind: flow

Each row is a role and each box is a step. Tap a step to see what happens there.

- office | type: lane | color: #15803d
  title: Farm Office
  sub: Requests and 15-day reports
- admin | type: lane | color: #ea580c
  title: Farm Admin
  sub: Approvals and Product Master
- request | lane: office | col: 1 | icon: bi-tags | color: #d97706 | next: approve | screen: product-rates
  title: Ask for a product or rate
  status: Pending
  text: On **Product & Rate Management**, request a new product or a rate change. It waits for the Farm Admin.
- approve | lane: admin | col: 2 | icon: bi-patch-check | color: #16a34a | next: entry | return: request | screen: rate-approvals
  title: Approve the rate
  status: Approved
  text: The Farm Admin checks the price and unit and approves it. The product and rate are then used in the 15-day report.
- rejected | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: approve | to: request | screen: rate-approvals
  title: Request rejected
  status: Rejected
  text: The Farm Admin rejects it with a reason. The Farm Office can edit the request and press **Resubmit Request**.
- entry | lane: office | col: 3 | icon: bi-journal-text | color: #64748b | next: submit | screen: fifteen-day-entry
  title: Fill the 15-day report
  status: Draft
  text: Enter the inventory, income and expense for the period. Each step is saved as a draft.
- submit | lane: office | col: 4 | icon: bi-send-check | color: #15803d | next: summaries | screen: report-review
  title: Review and submit
  status: Submitted
  text: Check the review page and press **Submit 15-Day Report**. The submitted report can be opened at any time.
- summaries | lane: office | col: 5 | icon: bi-graph-up-arrow | color: #2563eb | screen: finance-summary
  title: See the summaries
  text: The finance, inventory, income and expense summaries add up every report of the financial year.
:::

::: section statuses
title: Every status you can see
kind: statuses

- no-request | color: #64748b
  label: No Request
  text: The product has no request. It uses its current rate.
- pending | color: #d97706
  label: Pending
  text: The request is waiting for the Farm Admin.
- approved | color: #16a34a
  label: Approved
  text: The Farm Admin approved it. The new product or rate is in use.
- rejected | color: #dc2626
  label: Rejected
  text: The Farm Admin refused it. Read the reason, edit and resubmit.
- active | color: #15803d
  label: Active
  text: The product is in use in the Product Master.
- inactive | color: #475569
  label: Inactive
  text: The product is switched off. It is hidden from new requests.
- draft | color: #d97706
  label: Draft
  text: The 15-day report is saved but not submitted. You can still change it.
- submitted | color: #16a34a
  label: Submitted
  text: The 15-day report is final. Only an admin can reopen it.
- ready-to-submit | color: #2563eb
  label: Ready to submit
  text: On the review step, the report is ready. On Finance Summary, some reports are not submitted yet.
- ready-update | color: #d97706
  label: Ready for Update
  text: The current period is open for entry.
- completed | color: #16a34a
  label: Completed
  text: The previous period, shown on the inventory step.
- upcoming | color: #7c3aed
  label: Upcoming
  text: The next period, which opens after this one.
:::

::: section roles
title: Who does what
kind: roles

You only see the screens your designation allows.

- office | color: #15803d
  title: Farm Office
  - Request new products and rate changes
  - Fill and submit the 15-day report
  - Check the finance, inventory, income and expense summaries
- admin | color: #ea580c
  title: Farm Admin
  - Approve or reject product and rate requests
  - Correct products and rates in the Product Master
  - See the rate history of each product
:::

::: section faq
title: Problems and answers
kind: faq

- no-product | popular: true
  q: A product is missing in my 15-day report
  a: Only approved products appear. Request it on **Product & Rate Management** and wait until the Farm Admin approves it.
- change-submitted | popular: true
  q: I submitted the report and found a mistake
  a: A submitted report cannot be edited. Ask your admin to reopen it.
- sold-locked
  q: I cannot type in the Sold column on the inventory step
  a: **Sold** comes from the **Sold Quantity** you enter on the **Income** step. Change it there.
- no-approvals
  q: The Pending Approvals list is empty, but the Farm Office sent requests
  a: The **From Date** and **To Date** filters start at today. Choose earlier dates and press **Apply Filters**.
- cannot-edit
  q: "This product is still awaiting approval - you can't edit it yet."
  a: A new product cannot be changed until the Farm Admin approves it. Wait for the decision.
:::

::: section farm-dashboard
title: See the farm at a glance
routes: /FarmDashboard, /FarmOperations
minutes: 2
screenshot: farm-dashboard.png
screenshot-phone: farm-dashboard.phone.jpg

**What this screen is for**
The farm dashboard (**Farm Operations** in the menu) shows the money and stock for the selected financial year, and takes you to every other farm screen.

1. Choose the **Financial Year** at the top right.
2. Read the cards: **Total Budget**, **Total Income Till Date**, **Total Expense Till Date**, **Net Profit Till Date** and **ROI Till Date**.
3. Under **Quick Action**, tap **Product Management**, **Inventory Summary**, **Income Summary**, **Expense Summary** or **Finance Summary** to open that screen.
4. In **Next 15-day Period**, press **Start 15 Days Entry**. If you saved a draft, the button says **Continue 15 Days Entry**.
5. In **Income, Expense & Profit Trends**, choose **Last 6 Periods**, **Last 3 Periods** or **All Periods**. Point at a bar to see its numbers.
6. **Latest Stock Position** shows the last closing stock of each product. Search a product, or press **View All** to open the inventory summary.
7. **Income Overview** and **Top Expense Categories** show where money came from and went. Press **View Details** to open the summary.

> Tip: ROI is the net profit divided by the budget.

## Common problems

- **"No report history yet".** Submit a 15-day report first. The chart fills after that.
- **"Could not load dashboard data".** Check your internet and press **Retry**.
- **The numbers are zero.** Check that the right **Financial Year** is chosen.
:::

::: section product-rates
title: Request a new product or a rate change
routes: /ProductRateManagement
minutes: 3
tour: 1 > [data-help=fo-prm-tabs] | 2 > [data-help=fo-prm-status] | 3 > [data-help=fo-prm-search] | 4 > [data-help=fo-prm-actions] | 5 > [data-help=fo-prm-new]

**What this screen is for**
**Product & Rate Management** lists every farm product with its current rate and its latest request. Use it to ask the Farm Admin for a new product or a new rate.

1. Choose a tab: **All Product**, **Livestock** or **Orchard**.
2. Tap **Pending** or **Approved** to see only those requests. **All** shows every product.
3. Type in **Search products...** to find a product quickly.
4. In the **Action** column, the eye shows the **Request Details**. The pencil asks for a rate change, or edits your **Pending** or **Rejected** request. The bin deletes a **Pending** or **Rejected** request.
5. To add a product, press **Request New Product**. Choose **Live Stock** or **Orchard**, enter the **Product**, **UOM** and **Price**, add a **Reason** if you like, and press **Request Product**.

> Tip: Once approved, the product and rate appear in the inventory and income entry.
> Careful: Deleting a request cannot be undone.

## Fields explained

| Field | What to enter |
|---|---|
| **Live Stock** / **Orchard** | The product group. |
| **Product** | The product name, for example Sheep. Required. |
| **UOM** | Kg, Unit, Litre or Nos. Required. |
| **Price** | The rate per unit in rupees. Must be more than zero. |
| **Reason (Optional)** | Why you need it. Helps the Farm Admin decide. |

## Common problems

- **"Enter a valid rate."** Enter a price above zero.
- **"This product is still awaiting approval - you can't edit it yet."** Wait for the Farm Admin to approve the new product.
- **My request shows Rejected.** Open it with the eye to read the **Rejection Reason**. Tap the pencil, correct it and press **Resubmit Request**.
- **I do not see Request New Product.** Your designation can only view this screen.
:::

::: section rate-approvals
title: Approve or reject product and rate requests
routes: /ProductRateApprovals
minutes: 3
screenshot: rate-approvals.png

**What this screen is for**
**Pending Approvals** lists the new product and rate change requests from the Farm Office that are waiting for a decision. A decided request leaves this list.

1. Open **Filters**. Choose **From Date**, **To Date**, **Request Type**, **Product Group** or **Product**, then press **Apply Filters**. **Clear All** resets them.
2. Choose a tab: **All Request**, **New Product** or **Rate Change**. The numbers show how many are waiting.
3. Tap the eye to see a request: product, unit, current rate, **Request Price**, who asked and the **Reason**.
4. To approve at the asked price, press **Approve** in the row and confirm.
5. To change the price or unit first, tap the pencil. Under **Approval Details**, change the **UOM** or **Approved Price**, add **Admin Remarks**, and press **Approve**.
6. To refuse, tap the pencil, write the reason in **Admin Remarks** and press **Reject**, then confirm.

> Careful: Approving changes the product rate at once. It is used in the next 15-day report.

## Common problems

- **The list is empty.** The dates start at today. Choose an earlier **From Date** and press **Apply Filters**.
- **"Enter a rejection reason in Admin Remarks."** A reason is required to reject.
- **"Enter a valid approved price."** Enter a price above zero.
- **No Approve button.** Your designation can only view requests.
:::

::: section product-master
title: Manage the product master
routes: /FarmProductMaster
minutes: 2
screenshot: product-master.png

**What this screen is for**
**Product Master** is the list of all farm products, their units and current rates. Approved requests are added here. The Farm Admin can also correct a product directly.

1. Choose a tab: **All Product**, **Livestock** or **Orchard**, or search a product.
2. Check the **Current Rate** and the **Status** (**Active** or **Inactive**).
3. Press **View History** to see each approved rate with its date. The latest rate is tagged **Current**.
4. To correct a product, tap the pencil. Change the **Product Group**, **Product Name**, **UOM** or **Current Rate**.
5. Untick **Active** to stop using a product. It stays in the list with its rate history.
6. Press **Save Changes**.

> Careful: Edit here does not go through the approval flow. The change is used at once.

## Common problems

- **"Enter a product name and UOM."** Both are required.
- **"No approved rate history yet."** The product has no approved rate request yet.
- **There is no pencil.** Your designation can only view the Product Master.
:::

::: section fifteen-day-entry
title: Fill and submit the 15-day report
routes: /FifteenDaysEntry, /15daysEntry
minutes: 6
tour: 1 > [data-help=fo-steps] | 2 > [data-help=fo-period] | 3 > [data-help=fo-inventory] | 4 > [data-help=fo-continue] | 5 > [data-help=fo-continue] | 6 > [data-help=fo-save]
screenshot: fifteen-day-entry.png
hotspots: 1 @ 2.2,1.9,95.5,6.6 | 2 @ 2.2,19.2,95.5,21 | 3 @ 2.2,42.7,95.5,57.3 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: fifteen-day-entry.phone.jpg
hotspots-phone: 1 @ 3.1,0.8,93.8,6.4 | 2 @ 3.1,17.4,93.8,32.8 | 3 @ 3.1,51.6,93.8,48.4 | 4 @ - | 5 @ - | 6 @ -

**What this screen is for**
Every 15 days (1 to 15, and 16 to the month end) the farm sends one report with four steps: **Inventory**, **Income**, **Expense** and **Review**. The screen opens the next period that is not submitted yet.

1. Use the steps at the top to move between **Inventory**, **Income**, **Expense** and **Review**. Your work is saved as a draft each time you move.
2. Check the period cards: **Current Reporting**, **Previous Period** and **Next Reporting Period**.
3. On **Inventory Entry**, in the **Livestock Sales Ledger**, enter **Purchase / Old Quantity**, **Hatched / New Breed**, **Mortality** and **Others** for each product. **Opening Stock** and **Closing Stock** fill themselves. Add **Remarks** if needed.
4. Press **Continue to Income**. On **Income Entry**, enter the **Sold Quantity** for each product in the **Livestock Sales Ledger** and **Orchard Sales**. The rate is the approved rate, so the **Total amount** is worked out for you. Press **Continue to Expense**.
5. On **Expense Entry**, enter the amounts under **Expenditure Breakup** and **Miscellaneous Expenditure**. Use **Add New Category** or **Add New Sub Category** if a line is missing. Press **Continue to Review**, check everything, press **Submit 15-Day Report** and then **Confirm Submit**.
6. Press **Save as Draft** at any time to stop and come back later.

> Tip: **Miscellaneous** in the breakup is the total of the sub categories. You cannot type in it.
> Careful: Once submitted, the report cannot be edited unless an admin reopens it.

## After you submit

- The **15 - Day Report Submitted Successfully!** screen shows the totals.
- Press **View Submitted Report** to open it, **Go to finance summary** for the year, or **Enter ... Report** for the next period.

## Fields explained

| Field | What to enter |
|---|---|
| **Purchase / Old Quantity** | Animals or items bought in this period. |
| **Hatched / New Breed** | New animals born or hatched in this period. |
| **Mortality** | Animals that died in this period. |
| **Others** | Any other change. It can be below zero to reduce stock. |
| **Sold Quantity** | How many you sold. It also fills **Sold** on the inventory step. |
| **Amount (₹)** | Money spent on that expense in this period. |

## Common problems

- **"No products in this ledger".** No product is approved yet. Request it on **Product & Rate Management**.
- **I cannot type in Sold.** Enter the **Sold Quantity** on the **Income** step.
- **The rate is wrong.** Ask for a rate change on **Product & Rate Management**. You cannot change it here.
- **"Please enter a category name."** Type a name before pressing **Add Category**.
:::

::: section report-review
title: Open a submitted 15-day report
routes: /FifteenDayReportReview, /15DayReportReview
minutes: 2
screenshot: report-review.png
screenshot-phone: report-review.phone.jpg

**What this screen is for**
The **15 - Day Report Review** screen shows one submitted report in full and compares it with the period before.

1. Open it with **View Submitted Report** after submitting, or with the eye on **Finance Summary**.
2. At the top, see the period, the status and who submitted it.
3. Read the cards: **Total Income**, **Total Expense**, **Net Profit** and **Total Products**. The arrows compare with the previous 15 days.
4. Tap **Inventory Summary**, **Income Summary** or **Expense Summary** to open or close each part.
5. Check **Financial Summary** and **Comparison with Previous** at the bottom.
6. Press **Back to Finance Summary List** to go back.

## Common problems

- **"No previous period to compare".** This is the first report of the financial year.
- **"No inventory records found for this period."** No stock was entered for that report.
:::

::: section finance-summary
title: See the finance summary for the year
routes: /FinanceSummary, /FinanceSummaryOverall
minutes: 2
screenshot: finance-summary.png
screenshot-phone: finance-summary.phone.jpg

**What this screen is for**
**Finance Summary (Overall)** adds up all 15-day reports of a financial year and lists every report.

1. Choose the **Financial Year** and the **View**: **15-Day View**, **Monthly View** or **Quarterly View**.
2. Read the cards: **Annual Budget**, **Total Income**, **Total Expense**, **Net Profit** and **ROI**.
3. In **Analytics Validation Trend**, choose **Last 6 Months**, **Last 3 Months** or **Last 1 Year**.
4. In the report history, set **From Date** and **To Date** to narrow the list.
5. The **Status** column shows **Submitted** or **Draft**. Tap the eye to open the report.
6. Press **Download Excel** to save the list.

## Common problems

- **"No report records found for the selected filter."** Widen the dates or choose another financial year.
- **The badge at the top says Ready to submit.** At least one report in the year is still a draft.
:::

::: section inventory-summary
title: See stock movement by product
routes: /InventorySummary
minutes: 2
screenshot: inventory-summary.png
screenshot-phone: inventory-summary.phone.jpg

**What this screen is for**
**Inventory Summary** shows how the stock of each product moved in every 15-day period.

1. Open **Filters**. Choose **From Date**, **To Date**, **Category** and **Product**, then press **Apply Filters**. **Clear All** resets them.
2. Tap a product tab to see that product.
3. Read the cards: **Total Purchased**, **Total Hatched**, **Total Sold**, **Mortality** and **Closing Stock**.
4. In the **Stock Movement History** table, tap a column name to sort. A period still in draft has a **Draft** tag.
5. Press **Download Excel** or **Download PDF** to save the table.

## Common problems

- **"From Date cannot be later than To Date."** Choose the dates again.
- **"No products found".** Products must be added in the Product Master first.
:::

::: section income-summary
title: See the income summary
routes: /IncomeSummary
minutes: 2
screenshot: income-summary.png
screenshot-phone: income-summary.phone.jpg

**What this screen is for**
**Income Summary** shows the farm sales income for each reporting period, split into livestock and orchard.

1. Open **Filters**. Choose the **Period Type** (**15 Days** or **1 Month**), **From Date**, **To Date** and **Income Category**, then press **Apply Filters**.
2. Read the cards: **Total Income Till Date**, **Sales Records**, **Products Covered** and **Reporting Period**. The last card shows how many days are left to update the latest period.
3. In the **Livestock Sales Ledger** table, see **Livestock Income**, **Orchard Income**, **Total Income** and **Status** for each period.
4. Tap the eye in **View** to see each product, quantity, rate and income of that period.
5. See **Live Income Overview** and **Orchard Income Overview** for the share of each product.
6. Press **Download Excel** to save the table.

## Common problems

- **"Editing window closed".** The time to update the latest period is over.
- **"Could not load income data".** Press **Retry**.
:::

::: section expense-summary
title: See the expense summary
routes: /ExpenseSummary
minutes: 2
screenshot: expense-summary.png
screenshot-phone: expense-summary.phone.jpg

**What this screen is for**
**Expense Summary** shows the farm expenses for each reporting period and the biggest expense categories.

1. Open **Filters**. Choose the **Period Type**, **From Date**, **To Date** and **Expense Category**, then press **Apply Filters**.
2. Read the cards: **Total Expense**, **Highest Expense Category**, **Expense Categories** and **Summary Coverage**.
3. In **15 - Day Expense Summary**, see the **Total Expense (₹)** and **Status** of each period.
4. Tap the eye in **View** to see the **Expenditure Breakup (Farm Operations)** and **Miscellaneous Expenditure (Sub-Categories)** of that period.
5. See **Monthly Expense Trend** and **Expense Category Overview** to compare.
6. Press **Download Excel** to save the table.

## Common problems

- **"No expense data yet".** Submit a 15-day report with expenses first.
- **"Could not load expense data".** Press **Retry**.
:::
