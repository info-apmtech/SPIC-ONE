---
module: dealer-review
title: Dealer review and approval
icon: bi-person-check
order: 102
category: Dealers
color: #6d28d9
example: APP-2026-001 · Demo Agro Traders
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

After the MO submits a dealer application, it is checked by the RM, then the SM, and then the AVP. Each one can approve it and suggest a credit limit, or send it back for correction. This module also has the approval screens for sub dealers and employees, and a few dealer reports.

- check | icon: bi-search | color: #6d28d9
  title: Three levels check it
  text: RM, then SM, then AVP open the application and check every part.
- limit | icon: bi-cash-coin | color: #d97706
  title: Credit limit decision
  text: RM and SM recommend a credit limit for SPIC and GFL. The AVP gives the final limit.
- back | icon: bi-arrow-counterclockwise | color: #dc2626
  title: Send back to correct
  text: Any level can send the application one step back with a reason and the parts to fix.
:::

::: section flow
title: How an application moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- mo | type: lane | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  sub: Fills the application
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: Regional Manager
- sm | type: lane | color: #c026d3 | roles: SMD, SMM
  title: SM
  sub: State Marketing Manager
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin, SuperAdmin
  title: AVP
  sub: Final approval
- office | type: lane | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: New dealers only
- submit | lane: mo | col: 1 | icon: bi-send | color: #64748b | next: rm-check
  title: Submit the application
  status: Submitted
  text: The MO fills every part of the dealer application and submits it. Only complete applications reach the review list.
- rm-check | lane: rm | col: 2 | icon: bi-person-check | color: #0891b2 | next: sm-check | return: submit | screen: review-approve
  title: RM checks it
  status: In RM
  text: The RM checks the application, enters the RM credit limit and presses **Proceed to SM**, or sends it back to the MO.
- sent-back | lane: mo | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: submit | screen: send-back
  title: Correct and submit again
  status: Sent Back
  text: A reviewer sent the application back with a reason and the parts to fix. The previous level corrects them and sends it on again.
- sm-check | lane: sm | col: 3 | icon: bi-clipboard-check | color: #c026d3 | next: avp-check | return: rm-check | screen: review-approve
  title: SM checks it
  status: In SMM
  text: The SM sees the RM credit limit, enters the SM credit limit and presses **Proceed to AVP**, or sends it back to the RM.
- avp-check | lane: avp | col: 4 | icon: bi-patch-check | color: #7c3aed | next: office-ids | return: sm-check | screen: review-approve
  title: AVP approves it
  status: In AVP
  text: The AVP sees both earlier limits, enters the final credit limit and presses **Approve**. The dealer becomes active.
- office-ids | lane: office | col: 5 | icon: bi-upc-scan | color: #16a34a | screen: avp-office-ids
  title: Enter the Dealer IDs
  status: Verified
  text: For a new dealer, AVP Office enters the SPIC and GFL Dealer IDs. An existing dealer is complete after the AVP approval.
:::

::: section statuses
title: Every status you can see
kind: statuses

- submitted | color: #64748b
  label: Submitted
  text: The MO has submitted the application.
- in-rm | color: #0891b2
  label: In RM
  text: Waiting for the Regional Manager.
- in-smm | color: #c026d3
  label: In SMM
  text: Waiting for the State Marketing Manager.
- in-avp | color: #7c3aed
  label: In AVP
  text: Waiting for the AVP's final approval.
- pending | color: #d97706
  label: Pending
  text: On the review list: still with RM, SMM, AVP or AVP Office. The **Current Stage** column shows who has it.
- verified | color: #16a34a
  label: Verified
  text: On the review list: fully approved.
- approved | color: #16a34a
  label: Approved
  text: On the application: the AVP has approved it.
- sent-back | color: #dc2626
  label: Sent Back
  text: On the review list: a reviewer sent it back for correction.
- rejected | color: #dc2626
  label: Rejected
  text: On the application: a reviewer sent it back. It is not closed; it comes back after correction.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- mo | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  - Submit the dealer application
  - Correct it when it is sent back
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - Check applications from your region
  - Recommend the RM credit limit
  - Send back to the MO
- sm | color: #c026d3 | roles: SMD, SMM
  title: SM
  - Check applications from your state
  - Recommend the SM credit limit
  - Send back to the RM
  - Approve sub dealers and employees first
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP
  - Give the final approval and credit limit
  - Send back to the SM
  - Give the final approval for sub dealers and employees
- admin | color: #374151 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - Can give the final approval like the AVP
  - Keep the Credit Limit Sales figures
  - See the state-wise dealer summary
- office | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - Enter the SPIC and GFL Dealer IDs for new dealers
:::

::: section faq
title: Problems and answers
kind: faq

- no-button | popular: true
  q: I opened an application but there is no Proceed or Send Back button
  a: The application is not at your level now. The buttons appear only when it is waiting for you. Check the **Status** card and the progress bar at the top.
- no-limit-boxes | popular: true
  q: Why are there no credit limit boxes in the Confirm Proceed window?
  a: They are not shown for a new dealer's first approval, for an inactive dealer, or when the dealer applied with consent and no credit limit (the limit is then 0). SPIC boxes appear only if the dealer is with SPIC, and GFL boxes only if the dealer is with GFL.
- missing
  q: An application is missing from my review list
  a: The list shows only complete applications from your area. If some parts of the application are still empty, it does not appear. Ask the MO to finish all parts.
- remarks
  q: I see "Remarks are required for New Dealer approval."
  a: For a new dealer, every level must write remarks. Type them in the **Remarks** box and press **Confirm & Proceed** again.
- after-send-back
  q: Where does the application go when I send it back?
  a: One level back. RM sends it to the MO, SM to the RM, and the AVP to the SM. They correct it and it comes to you again.
:::

::: section review-list
title: See applications waiting for review
routes: /DealerReviewList
minutes: 2
tour: 1 > [data-help=dr-list-cards] | 2 > [data-help=dr-list-search] | 3 > [data-help=dr-list-status] | 4 > [data-help=dr-list-table] | 5 > [data-help=dr-list-pages]
screenshot: review-list.png
hotspots: 1 @ 2.2,10.8,95.5,12.7 | 2 @ 61,28.8,24.3,4.3 | 3 @ 86.3,28.8,9.5,4.3 | 4 @ 4.2,35,91.6,24.5 | 5 @ 4.2,62,91.6,4.3
screenshot-phone: review-list.phone.jpg
hotspots-phone: 1 @ 3.1,7.3,93.8,42.2 | 2 @ 8.5,56.7,66.7,3.3 | 3 @ 8.5,60.9,26.2,3.3 | 4 @ 8.5,65.7,83.1,18.6 | 5 @ 8.5,86.1,83.1,2.8

**What this screen is for**
The **Dealer Applications Review** screen lists the submitted dealer applications from your area. Applications waiting for you are at the top.

1. Look at the cards at the top: **Total Applications**, **Pending Review**, **Verified** and **Sent Back**.
2. Type an application ID or dealer name in **Search applications...** to find one quickly.
3. Choose **Pending**, **Verified** or **Sent Back** in the status box to show only those. **All Status** shows everything.
4. In the table, **Current Stage** shows who has the application now and **Credit Limit** shows the approved limit. Tap the eye icon in **Action** to open it.
5. At the bottom, choose how many rows to show in **Items Per Page** and use the arrows to move between pages.

> Tip: RM sees the region, SM sees the state, and the AVP sees all applications.

## Common problems

- **"No applications found."** Choose **All Status** and clear the search box.
- **An application is not in the list.** Only complete applications are listed. Ask the MO to fill every part.
:::

::: section review-approve
title: Check and approve an application
routes: /SavedDealerReview
minutes: 4
tour: 1 > [data-help=dr-rev-cards] | 2 > [data-help=dr-rev-progress] | 3 > [data-help=dr-rev-timeline] | 4 > [data-help=dr-rev-details] | 5 > [data-help=dr-rev-sendback] | 6 > [data-help=dr-rev-approve]

**What this screen is for**
The **Review - Dealer Application** screen shows one full application. When it is waiting for you, you approve it here and give your credit limit, or send it back.

1. Check the cards at the top: **Dealer Name**, **Dealer Code**, **Submitted By** and **Status**.
2. The progress bar shows each level: **MO**, **RM**, **SM** and **AVP**, with **Under Review**, **Pending**, **Approved** or **Rejected**.
3. The **Timeline** and **Action History** show who acted, when, their remarks and the credit limit they gave.
4. Tap each part to open it and check it: **Company Details**, **Experience In Fertilizer Trade**, **Annual Sales Data**, **Proprietor Details**, **Investment**, **Credit Limit Proposal**, **Enclosures** and the others.
5. If something is wrong, press **Send Back** (see "Send an application back").
6. If all is correct, press **Proceed to SM** (RM), **Proceed to AVP** (SM) or **Approve** (AVP). In the **Confirm Proceed** window, enter your credit limit and **Remarks**, then press **Confirm & Proceed**.

> Careful: After you proceed, the application moves to the next level and you cannot change it.

## Fields explained

| Field | What to enter |
|---|---|
| **RM Credit Limit (SPIC)**, **RM Credit Limit (GFL)** | The limit the RM recommends. Shown to the RM. |
| **SM Credit Limit (SPIC)**, **SM Credit Limit (GFL)** | The limit the SM recommends. The RM limits are shown above as **(Approved)**. |
| **AVP Credit Limit (SPIC)**, **AVP Credit Limit (GFL)** | The final limit. The RM and SM limits are shown above. |
| **Remarks** | Your comments. Required for a new dealer, optional otherwise. |

## Common problems

- **No Proceed or Send Back button.** The application is not waiting for you now. You can only read it.
- **A green "Credit Limit Consent" box.** The dealer applied without credit limit details, so the credit limit is 0.
- **The screen goes to the Dashboard.** This is normal. After you approve, the app shows a success message and opens the Dashboard.
:::

::: section send-back
title: Send an application back
routes: /SavedDealerReview
roles: RM, RMD, SMD, SMM, AVP, Director, Admin, CorporateAdmin, SuperAdmin
minutes: 2
screenshot: send-back.png
screenshot-phone: send-back.phone.jpg

**What this screen is for**
If an application has mistakes, send it one level back with a reason. RM sends it to the **Marketing Officer (MO)**, SM to the **Regional Manager (RM)**, and the AVP to the **State Manager (SM)**.

1. Open the application and press **Send Back** at the bottom.
2. In the **Send Back Application** window, check the **Application Summary** and **Send Back To**.
3. Write the **Reason for Sending Back**. It is required.
4. Under **Sections Requiring Correction**, tick the parts to fix, for example **Tax & Compliance**, **Investment** or **Enclosures**. Some are ticked already; untick those that are fine.
5. Choose the **Priority Level**: **High**, **Medium** or **Low**.
6. Press **Send Back to MO**, **Send Back to RM** or **Send Back to SM** (the button shows your level), or **Cancel** to stop.

## Common problems

- **"Reason is required."** Write the reason first.
- **"Please select at least one correction section."** Tick at least one part.
- **"Unable to send back dealer application."** Check your internet and try again.
:::

::: section avp-office-ids
title: Enter the Dealer IDs for a new dealer
routes: /SavedDealerReview
roles: CommonRole
designations: AVP Office
minutes: 2
screenshot: avp-office-ids.png
screenshot-phone: avp-office-ids.phone.jpg

**What this screen is for**
After the AVP approves a new dealer, AVP Office enters the company Dealer IDs. This finishes the application.

1. Open the application from the review list. Its **Current Stage** shows **AVP Office**.
2. Scroll to the **AVP Office - Dealer ID Submission** box.
3. Enter the **SPIC Dealer ID** and/or the **GFL Dealer ID**. Only the companies the dealer applied for are shown.
4. Add **Remarks (optional)** if needed.
5. Press **Submit Dealer IDs**. The app shows a success message and opens the Dashboard.

## Common problems

- **"SPIC Dealer ID is required for this application."** The dealer is with SPIC. Enter the SPIC Dealer ID.
- **"GFL Dealer ID is required for this application."** The dealer is with GFL. Enter the GFL Dealer ID.
- **I do not see the box.** The AVP has not approved it yet, or the IDs are already entered.
:::

::: section view-application
title: View an application (read only)
routes: /ReviewDealer
minutes: 1
screenshot: view-application.png
screenshot-phone: view-application.phone.jpg

**What this screen is for**
This older **Review - Dealer Application** screen shows one application without any approval buttons.

1. Check the cards at the top: **Dealer Name**, **Dealer ID**, **Total Experience**, **Total Sales**, **Investment**, **Credit Limit** and **Status**.
2. Tap each part, such as **Company Details** or **Investment**, to open it.
3. Under **Enclosures**, tap a document name to open it.

> Tip: To approve or send back, open the application from the **Dealer Applications Review** list instead.

## Common problems

- **The screen is empty.** It opens only with an application chosen. Go to the review list and use the eye icon.
:::

::: section sm-approvals
title: Approve sub dealers and employees (SM)
routes: /SMMApprovals
roles: SMD, SMM
minutes: 2
tour: 1 > [data-help=dr-sm-tabs] | 2 > [data-help=dr-sm-cards] | 3 > [data-help=dr-sm-table] | 4 > [data-help=dr-sm-table]

**What this screen is for**
The **SM Approval** screen lists sub dealers and employees added for dealers in your state. The SM checks them first; then they go to the AVP.

1. Choose a tab: **All Pending**, **Sub Dealers** or **Employees**.
2. The cards show **Total Pending**, **Sub Dealers** and **Employees**. Use **Filter** to show one type.
3. In **Pending SM Approvals**, check the **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** and **Nominee**.
4. Press **Approve** or **Reject**, write **Remarks**, then press **Confirm Approve** or **Confirm Reject**.

> Tip: Remarks are optional for approval, but write a reason when you reject.

## Common problems

- **"No Pending Approvals".** Nothing is waiting for you now.
- **I see "Show more".** Only 50 rows are shown at first. Press **Show more** to see the rest.
:::

::: section avp-approvals
title: Final approval of sub dealers and employees (AVP)
routes: /AVPApprovals
roles: AVP
minutes: 2
screenshot: avp-approvals.png
screenshot-phone: avp-approvals.phone.jpg

**What this screen is for**
The **AVP Approval** screen lists sub dealers and employees that the SM has already approved. The AVP gives the final approval.

1. Choose a tab: **All Pending**, **Sub Dealers** or **Employees**.
2. Check the cards **Total Pending**, **Sub Dealers** and **Employees**. Use **Filter** to show one type.
3. In **Pending AVP Approvals**, check the **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** and **Nominee**.
4. Press **Approve** or **Reject**. The window shows **SM Status: Approved**.
5. Write **Remarks** and press **Confirm Approve** or **Confirm Reject**.

## Common problems

- **"No Pending AVP Approvals".** No SM-approved record is waiting for you.
- **"Action failed".** Try again. If it keeps failing, tell your admin.
:::

::: section credit-limit-sales
title: Keep dealer sales for the credit limit
routes: /CreditLimitSales
minutes: 3
screenshot: credit-limit-sales.png
screenshot-phone: credit-limit-sales.phone.jpg

**What this screen is for**
The **Credit Limit Sales** screen keeps each dealer's past sales by year and product. Reviewers see these figures in the **Credit Limit Proposal** of an application.

1. Under **Add New Credit Limit Sales**, choose the **State** and **Financial Year**.
2. Choose the dealer by **Dealer Code** or **Dealer Name**. Both boxes pick the same dealer.
3. Choose the **Product**, **Product Group** and **Category**.
4. Enter the **Quantity** and **Gross Amount**, then press **Save**. **Cancel** clears the form.
5. To add many rows at once, press **Sample Format** to get the Excel layout, fill it, press **Bulk Upload**, choose the **Financial Year** and the file.
6. In **Existing Credit Limit Sales**, search by code or name. The pencil edits a row and the bin deletes it.

> Careful: Deleting a row cannot be undone.

## Common problems

- **"All required fields must be filled."** Choose State, Financial Year, dealer, Product, Product Group and Category.
- **"Please select a financial year before uploading."** Choose the year in the upload window first.
- **No Save, Bulk Upload, pencil or bin.** Your designation does not allow adding, changing or deleting. Ask your admin.
:::

::: section state-summary
title: State-wise dealer summary
routes: /DealerStateSummary
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP
minutes: 1
screenshot: state-summary.png
screenshot-phone: state-summary.phone.jpg

**What this screen is for**
The **Total Dealers — State Wise** screen counts dealers in each state and shows where their applications are, for SPIC and GFL.

1. Check the cards **TOTAL DEALERS** and **COMPLETED**.
2. In **Dealer Summary**, each row is a state. The columns show **As Per SAP Data**, **Saved Dealers**, **MO**, **RM**, **SMM**, **AVP** and **Completed**, each for **SPIC** and **GFL**.
3. Tap a column heading to sort by it. Tap again to reverse.
4. The **Total** row at the bottom adds up all states.
5. Press **Refresh** to load the latest numbers, or **Back to Dashboard**.

## Common problems

- **"Access restricted".** This report is only for Admin, Director and AVP users.
- **A state shows "Unknown".** The state list did not load. Press **Refresh**.
:::

::: section dealer-directory
title: Dealer directory
routes: /DealerDirectory
minutes: 1
screenshot: dealer-directory.png
screenshot-phone: dealer-directory.phone.jpg

**What this screen is for**
The **Dealer Directory** screen is a preview of a future dealer list. The numbers and rows on it are only examples, and its buttons and filters do not work yet.

1. Look at the layout: cards for **Total Dealers**, **Active Dealers**, **Regions Covered** and **Recently Added**.
2. The **Filters** box will let you choose **State**, **Region**, **Head Quarters** and **Status**.
3. The **Dealer List** table will show **Dealer Code**, **Firm Name**, **State**, **Head Quarters**, **Mobile No.**, **Email** and **Status**.

> Tip: To find a real application, use the **Dealer Applications Review** list.

## Common problems

- **Excel, PDF or Print does nothing.** These buttons are not working yet.
:::

::: section dealer-mapping
title: Map dealers to a sales audit program
routes: /DealerMapping
minutes: 3

**What this screen is for**
The **Dealer Mapping** screen is step 3, **Dealers**, of a sales audit program. You upload the list of dealers in the program and compare their SAP sales with the plan.

1. Open it from **Sales Plan** with **Save & Continue** or **Continue to Dealer Mapping**.
2. Under **Upload Dealer List**, drop the Excel file or press **Upload File**. The file must have a **Dealer Code** column.
3. Read the message. If rows were skipped, press **View Errors** to see the **Row No.**, **Dealer Code** and **Reason**.
4. In **Dealer List**, search by code or name. The bin removes a dealer from the program.
5. In **Dealer Sales Achievement - SAP Invoice Matches**, check planned and actual sales. Press **Refresh Matches** to load them again.
6. Check **Plan Summary & Comparison**: **Required Turnover**, **Achievement Amount**, **Difference** and **Progress %**.
7. Press **Continue to Plan Review**, or **Back to Sales Plan**.

## Common problems

- **"Missing CSR-2 ID. Open this page from Sales Plan."** Open the screen from the Sales Plan, not from a saved link.
- **"Upload an .xlsx or .xls file up to 5 MB."** Use an Excel file smaller than 5 MB.
- **Continue to Plan Review is grey.** Upload at least one dealer first.
:::

::: section training-program
title: Report a dealer training program (CSR-2)
routes: /DealerManagementTrainingProgram
minutes: 5

**What this screen is for**
After a CSR-1 plan is approved, use this screen to report how the program actually went: date, photos, participants and expenses. It opens with **View Program** from the **CSR-2 Program Execution** list.

1. On **Plan & Start**, check the **Approved CSR-1 Plan**. Fill **Focus Details**, **Actual Event Date**, **Start Time**, **End Time** and **Actual Location**. Use **Take Photo** and **Record Video** for event media. Press **Start Event**, then **Start Event** again to confirm.
2. On **Participants & Employees**, search a participant by **Phone Number**, choose the **Role** and press **Add Participant**. Use **New Farmer** for someone not yet registered. Add staff with **Add Employee**.
3. On **Expense & Proof**, enter the **Actual amount (₹)**, **Bill No.** and **Date** for each expense, and press **Upload proofs**. The cards compare **Approved Budget** and **Actual Expense**.
4. On **Review & Submit**, check everything. Press **Edit** on a card to go back and change it.
5. Press **Save Draft** to finish later. Press **Apply CSR-2** to send it, or **Validate CSR-2** or the approve button if it is waiting for you.

> Tip: Photos and videos are stamped with GPS position, date and time.

## Common problems

- **"CSR-2 can be opened only for an approved CSR-1."** Wait until the CSR-1 plan is approved.
- **I see only two steps.** Some roles fill only **Plan & Start** and **Participants & Employees**, then press **Apply CSR-2**.
- **"Enter a valid 10-digit mobile number."** Check the phone number.
:::
