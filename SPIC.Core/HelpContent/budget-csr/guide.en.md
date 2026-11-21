---
module: budget-csr
title: Budget and CSR
icon: bi-wallet2
order: 110
category: Marketing
color: #0891b2
example: DEMO CSR-1 · Farmer Meeting
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Plan the yearly marketing budget, share it down to states, regions and headquarters (HQs), and spend it through programs. Every program has two forms: CSR-1 before the event (the plan) and CSR-2 after the event (the report). Each form is checked and approved.

- plan | icon: bi-pie-chart | color: #0891b2
  title: Plan the budget
  text: Head office sets the yearly budget. It is shared to states, regions and HQs, then split across programs, month by month.
- csr1 | icon: bi-file-earmark-text | color: #d97706
  title: CSR-1 is the plan
  text: Before a program, apply CSR-1 with the date, place, focus crops and planned expenses.
- csr2 | icon: bi-camera | color: #16a34a
  title: CSR-2 is the report
  text: After the program, report CSR-2 with photos, participants, actual expenses and proofs.
:::

::: section flow
title: How the budget and a program move, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- office | type: lane | color: #0891b2 | roles: Admin, CorporateAdmin
  title: Head office
  sub: Admin
- field | type: lane | color: #d97706 | roles: MDO, JMDO, MO, RMD, SMD
  title: Field officers
  sub: MDO · MO · RMDO · SMDO
- approvers | type: lane | color: #0d9488 | roles: RM, SMM
  title: Approvers
  sub: RM · SMM
- md | type: lane | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: Senior approvers
  sub: MD Admin · MD HOD · AVP
- budget | lane: office | col: 1 | icon: bi-wallet2 | color: #0891b2 | next: plan | screen: state-allocation
  title: Set and share the budget
  status: Active
  text: Admin enters the yearly budget and shares it to states. SMD or SMM shares the state budget to regions, and RM or RMDO shares the region budget to HQs.
- plan | lane: field | col: 2 | icon: bi-calendar3 | color: #64748b | next: plan-ok | screen: program-budget
  title: Plan program budgets
  status: Submitted
  text: MO, RMDO and SMDO split their budget across programs and months, then press **Submit**.
- plan-ok | lane: approvers | col: 3 | icon: bi-check2-square | color: #2563eb | next: csr1 | screen: program-budget
  title: Validate and approve the plan
  status: Approved
  text: RMDO or SMDO validates the program budget. Then RM or SMM approves it.
- csr1 | lane: field | col: 4 | icon: bi-file-earmark-text | color: #d97706 | next: csr1-ok | screen: create-csr1
  title: Apply CSR-1
  status: Submitted
  text: For an approved program, fill the CSR-1 form with the date, HQ, location, responsible person, focus crops and expenses, then press **Submit for Validation**.
- csr1-ok | lane: approvers | col: 5 | icon: bi-patch-check | color: #2563eb | next: csr2 | screen: csr1-list
  title: Validate and approve CSR-1
  status: Approved
  text: MO validates HQ plans and SMD validates state plans. Then RM or SMM approves. The CSR-1 gets a sanction number and becomes eligible for CSR-2.
- csr2 | lane: field | col: 6 | icon: bi-camera | color: #16a34a | next: csr2-ok | screen: csr2-list
  title: Run the program and report CSR-2
  status: Submitted
  text: The responsible person runs the program and reports the date, photos, participants, actual expenses and proofs.
- csr2-ok | lane: approvers | col: 7 | icon: bi-check2-all | color: #16a34a | next: final | screen: csr2-list
  title: Validate and approve CSR-2
  status: Approved
  text: MO or SMD validates the report. Then RM or SMM approves it.
- final | lane: office | col: 8 | icon: bi-clipboard-data | color: #0891b2 | screen: final-report
  title: Final report
  text: Head office sees every program with its CSR-1 status, CSR-2 status, expenses and remaining budget.
- late | lane: md | col: 6 | icon: bi-clock-history | color: #dc2626 | kind: exception | from: csr1 | to: csr1-ok | screen: csr1-list
  title: Late forms need three approvals
  status: Level1Approved
  text: A CSR-1 or CSR-2 sent after the cut-off day needs three approvals: RM or SMM first, then MD Admin, then MD HOD.
- shortage | lane: md | col: 7 | icon: bi-arrow-left-right | color: #dc2626 | kind: exception | from: csr2-ok | to: csr2-ok | screen: reallocation
  title: Not enough budget
  status: PendingMDHOD
  text: If a CSR-2 cannot be approved because the budget is short, RM, SMM or Admin asks for a budget reallocation. After the final approval by MD HOD, the approval is tried again.
:::

::: section statuses
title: Every status you can see
kind: statuses

- active | color: #16a34a
  label: Active
  text: The annual budget record is in use.
- inactive | color: #64748b
  label: Inactive
  text: The annual budget record is switched off. It can be activated again.
- draft | color: #64748b
  label: Draft
  text: Saved but not sent. You can still change it.
- submitted | color: #d97706
  label: Submitted
  text: Sent and waiting for validation.
- pending | color: #d97706
  label: Pending
  text: Waiting for the next check.
- validated | color: #2563eb
  label: Validated
  text: Checked by the validator. Waiting for approval.
- level1 | color: #7c3aed
  label: Level1Approved
  text: A late form passed the first approval. Waiting for MD Admin.
- level2 | color: #7c3aed
  label: Level2Approved
  text: A late form passed the second approval. Waiting for MD HOD.
- approved | color: #16a34a
  label: Approved
  text: Fully approved. An approved CSR-1 can go on to CSR-2.
- rejected | color: #dc2626
  label: Rejected
  text: Refused. Read the rejection reason.
- pending-smm | color: #d97706
  label: PendingSMM
  text: A budget reallocation is waiting for SMM.
- pending-avp | color: #d97706
  label: PendingAVP
  text: A budget reallocation is waiting for AVP.
- pending-mdhod | color: #d97706
  label: PendingMDHOD
  text: A budget reallocation is waiting for MD HOD, the last approver.
- healthy | color: #16a34a
  label: Healthy
  text: The HQ has enough budget left.
- near-limit | color: #d97706
  label: Near Limit
  text: The HQ has used most of its budget.
- exceeded | color: #dc2626
  label: Exceeded
  text: The HQ has spent more than its budget.
- ext-pending | color: #d97706
  label: Pending Approval
  text: An extension request is waiting for a decision.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- admin | color: #0891b2 | roles: Admin, CorporateAdmin
  title: Admin
  - Enter the yearly budget for each financial year
  - Share the yearly budget to states
  - Keep the program list, sanction numbers and RMD HQ mapping
  - Ask for a budget reallocation between states
- smm | color: #0d9488 | roles: SMM
  title: SMM
  - Share the state budget to regions
  - Approve program budgets, state CSR-1 and state CSR-2
  - Ask for a budget reallocation between regions
  - Approve reallocation requests from RM
- smd | color: #ea580c | roles: SMD
  title: SMDO
  - Plan program budgets and validate them
  - Share the state budget to regions
  - Apply CSR-1 for state programs
  - Validate state CSR-1 and CSR-2
- rm | color: #2563eb | roles: RM
  title: RM
  - Share the region budget to HQs
  - Approve program budgets, HQ CSR-1 and HQ CSR-2
  - Ask for a budget reallocation between HQs
- rmd | color: #9333ea | roles: RMD
  title: RMDO
  - Plan program budgets and validate them
  - Share the region budget to HQs
  - Apply CSR-1 for HQ programs
- mo | color: #d97706 | roles: MO
  title: MO
  - Plan program budgets
  - Apply CSR-1
  - Validate HQ CSR-1 and CSR-2
- mdo | color: #65a30d | roles: MDO, JMDO
  title: MDO and JMDO
  - Apply CSR-1 for HQ programs
  - Run the program and report CSR-2 when you are the responsible person
- senior | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: MD Admin, MD HOD and AVP
  - MD Admin and MD HOD give the second and third approval of late forms
  - AVP and MD HOD approve budget reallocations
:::

::: section faq
title: Problems and answers
kind: faq

- csr2-grey | popular: true
  q: The CSR-2 button is grey
  a: CSR-2 opens only after the CSR-1 is **Approved**. Then the **CSR-2 Eligibility** column shows **Eligible for CSR-2** and the button works.
- one-month | popular: true
  q: CSR-1 Create shows plans for only one month
  a: This is normal. The blue box says which month, for example "Showing plans for December 2026 only". On or before the CSR-1 cut-off day you plan next month; after it, the month after next.
- must-equal
  q: Submit says the total must equal the allotted budget
  a: On **Budgeting Management** you must spread your whole budget across programs. The message shows how much is still left to allocate. Add program counts until the total matches.
- no-money
  q: A CSR-2 cannot be approved because the budget is short
  a: RM, SMM or Admin raises a request on **Budget Reallocations** to move money from another state, region or HQ. After MD HOD approves it, try the approval again.
- three-levels
  q: Why does my CSR-1 or CSR-2 need three approvals?
  a: It was sent after the cut-off day. Late forms go to RM or SMM, then MD Admin, then MD HOD.
- save-state-first
  q: I see "Save this state's allocation above before distributing it to regions."
  a: The state has no budget yet. Admin must first save the state allocation for that financial year.
:::

::: section annual-budget
title: Enter the yearly budget
routes: /AnnualBudgeting
minutes: 2
screenshot: annual-budget.png
screenshot-phone: annual-budget.phone.jpg

**What this screen is for**
Admin uses the **Annual Budgeting** screen to enter the total budget for each financial year. All other budget screens start from this amount.

1. Under **Add Annual Budgeting**, choose the **Financial Year (FY)**.
2. Enter the **Amount**.
3. Leave the switch on **Active**, or tap it to make the record **Inactive**.
4. Press **Save**. Press **Cancel** to clear the form.
5. In **Existing Annual Budgeting Records**, search by FY or pick **Active** or **Inactive** to filter the list.
6. Tap the pencil to edit a record, then press **Update**. Tap the round slash icon to deactivate it, or the arrow icon to activate it again.

> Tip: Each financial year can have only one budget record, active or not.

## Common problems

- **"Please select a Financial Year".** Choose a year before saving.
- **"Amount must be greater than zero".** Enter an amount above 0.
- **"An Annual Budgeting record for ... already exists".** Edit the existing record instead of adding a new one.
- **The Save button is grey.** No financial year exists yet. Add one in the Financial Master first.
:::

::: section state-allocation
title: Share the budget to states, regions and HQs
routes: /StateBudgetManagement
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD
minutes: 3
tour: 1 > [data-help=bc-sb-tabs] | 2 > [data-help=bc-sb-select] | 3 > [data-help=bc-sb-cards] | 4 > [data-help=bc-sb-table] | 5 > [data-help=bc-sb-save]
screenshot: state-allocation.png
hotspots: 1 @ 2.2,11.4,95.5,8.4 | 2 @ 2.2,22.2,95.5,14.4 | 3 @ 1.5,37.6,97,14.4 | 4 @ 2.2,54.9,95.5,25.3 | 5 @ 66.8,87,28.6,5.3
screenshot-phone: state-allocation.phone.jpg
hotspots-phone: 1 @ 3.1,7.2,93.8,5.5 | 2 @ 3.1,14.3,93.8,9.3 | 3 @ 1,24.2,97.9,35.4 | 4 @ 3.1,61.4,93.8,16.3 | 5 @ 55.1,81.5,44.9,7.2

**What this screen is for**
The **State Budgeting Management** screen shares the yearly budget down, one level at a time. Admin shares it to states, SMD or SMM shares a state's budget to its regions, and RM or RMDO shares a region's budget to its HQs.

1. Your tab opens for your role: **State Allocation** (Admin), **Region Allocation** (SMD, SMM) or **Headquarters Allocation** (RM, RMDO).
2. Choose the **Financial Year**. On the region tab also choose the **State**. On the HQ tab choose the **State** and the **Region**.
3. Check the cards, such as **Annual Budget**, **Allocated Budget** and **Remaining Budget**, or **State Allocated Budget** and **State MDO Budget**.
4. In the table, type the amount for each state, region or HQ. On the region and HQ tabs, first enter the **State MDO Amount** or **Region MDO Amount** at the top.
5. Save. On the state tab press **Save Draft**, then **Submit For Validation**. On the other tabs press **Save Region Allocation** or **Save Headquarters Allocation**.

> Careful: On the state tab, the whole annual budget must be shared before you can save.

## Common problems

- **"All budget must be allocated to states."** The message shows the amount still left. Share it to the states.
- **"Region allocation cannot exceed the remaining state budget"** or **"Headquarters allocation cannot exceed the remaining region budget".** Lower the amount.
- **"Save this region's allocation above before distributing it to headquarters."** The region has no budget yet. SMD or SMM must save the region allocation first.
- **"You do not have access to any budget allocation level."** Your role does not share budget. Ask your administrator.
:::

::: section program-budget
title: Plan, validate and approve program budgets
routes: /BudgetingManagements
minutes: 4
screenshot: program-budget.png
screenshot-phone: program-budget.phone.jpg

**What this screen is for**
On **Budgeting Management**, MO, RMDO and SMDO split their budget across programs and months. RMDO and SMDO validate the saved plans, and RM and SMM approve them.

1. Choose the financial year at the top of **Program-wise Budget Planning**.
2. Check the cards: your budget (for example **Available Budget**), **Allocated Budget**, **Remaining Budget** and **SMDO Budget**.
3. In **Budget plan list**, enter how many programs you will run each month, from **April** to **March**. If a program allows it, you can change its **Budget Amount**. **Total Budget** for each row updates.
4. Press **Submit**. The whole budget must be allocated.
5. To validate (RMDO or SMDO), press **Validate Budget**, choose a plan in **Saved program budgets awaiting validation**, correct the counts if needed and press **Validate**. Press **Create Budget** to go back to your own plan.
6. To approve (RM or SMM), choose a plan in **Validated program budgets awaiting approval**, correct the counts if needed and press **Approve**.
7. Use **Copy**, **CSV**, **Excel**, **PDF** or **Print** to share the table.

## Common problems

- **"Total allocated budget ... must equal allotted budget ..."** Some budget is still left. Add program counts until nothing is left.
- **"... exceeds allotted budget ..."** You planned too much. Lower the counts.
- **"No eligible budgets found for your assigned scope and selected FY."** Nothing is waiting for you in this year. Check the year.
- **"Only MO, RMDO and SMDO can save program budgets."** Your role can only view, validate or approve.
:::

::: section budget-submissions
title: Track my submitted budgets
routes: /BudgetSubmissions
minutes: 2
screenshot: budget-submissions.png
screenshot-phone: budget-submissions.phone.jpg

**What this screen is for**
The **Budget Submissions** screen lists the program budgets you have sent and shows where each one stands.

1. Look at the cards: **Program Budget**, **Approved**, **Pending validation**, **Rejected** and **Drafts**.
2. Set **Date Range**, **Program Type**, **Program Name** or **Status**, then press **Apply Filters**. **Clear All** removes them.
3. Type in **Search Program...** to find a program quickly.
4. Check **Program Budget**, **Submitted Date**, **Status** and **Validation Due** in the table.
5. Tap the eye icon to see the details. From there you can **Print** or **Export PDF**.

## Common problems

- **"No budget submission records found."** You have not sent a plan yet, or the filters are too narrow. Press **Clear All**.
:::

::: section hq-budget-overview
title: See HQ budget use
routes: /BudgetOverview
minutes: 1
screenshot: hq-budget-overview.png
screenshot-phone: hq-budget-overview.phone.jpg

**What this screen is for**
The **HQ Budget Allocation** screen shows how a region's budget is shared to its HQs and how much each HQ has used.

1. Look at the cards: **Total Regional Budget**, **Allocated to HQs**, **Remaining Budget** and **Utilizes Budget**.
2. In **Budget plan list**, check each HQ's **Allocated**, **Utilized**, **Remaining** and **Utilization %**, and its **Pending CSR-1** and **Pending CSR-2**.
3. The **Status** shows **Healthy**, **Near Limit** or **Exceeded**.
4. Tap the eye icon to open the HQ's budget use.
5. Press **Allocate Budget** to go to **Budgeting Management**.

## Common problems

- **The numbers do not change.** This screen is still being built. Use **Budgeting Management** and **State Budgeting Management** for real figures.
:::

::: section reallocation
title: Move budget with a reallocation
routes: /BudgetReallocations
minutes: 3

**What this screen is for**
When a CSR-2 cannot be approved because the budget is short, the **Budget Reallocations** screen moves money from one budget to another at the same level. RM moves money between HQs, SMM between regions, and Admin between states.

1. Under **New Reallocation Request**, check **Initiated by** and the level badge.
2. Choose the **Financial Year**. If asked, choose the **State** and the **Region**.
3. Choose **From** (the budget that gives money) and **To** (the budget that needs it). Check **Source Available Budget** and the **Selected destination**.
4. Enter the **Reallocation Amount (₹)** and write the **Reason for Reallocation**.
5. Check the **Approval route**, for example RM, SMM, AVP, MD HOD. Press **Apply Reallocation**.
6. Approvers: in **Reallocation Requests**, write **Approval / Rejection Remarks**, then press **Approve** or **Reject** on the row.
7. Press **Refresh** to see the latest status.

## Common problems

- **"Source budget shortage".** The amount is more than the source has left. Lower it or choose another source.
- **"No eligible budgets found."** Check the year and that the allocations are approved for your state or region.
- **"Apply permissions are unavailable for this login."** Only RM, SMM and Admin can ask for a reallocation.
:::

::: section program-master
title: Keep the program list
routes: /ProgramMaster
minutes: 3
screenshot: program-master.png
screenshot-phone: program-master.phone.jpg

**What this screen is for**
The **Program Master** screen keeps the list of program types and programs, who can plan them, and the program amount in each state.

1. Open **Program Types**. Enter the **Program Type Name**, set **Is Amount Change** to **Allowed** or **Not Allowed**, and press **Save**.
2. Open **Program Master**. Choose the **Program Type**, enter the **Program Name**, and set **MO**, **RMDO** and **SMDO** to **Eligible** or **Not Eligible**. Press **Save**.
3. Tap the pencil on a row to edit it.
4. Open **State Mapping** (or tap the map pin on a program). Choose the **Program**.
5. For each state, turn **Applicable** on or off. For an applicable state, enter the **Program Amount (₹)** and press **Save** on that row.

> Tip: **Is Amount Change** decides if planners may change a program's budget amount on **Budgeting Management**.

## Common problems

- **"View only - you do not have update permission."** Ask your administrator for update rights.
:::

::: section rmd-hq-mapping
title: Give HQs to an RMDO
routes: /RMDHQMapping
minutes: 2
screenshot: rmd-hq-mapping.png
screenshot-phone: rmd-hq-mapping.phone.jpg

**What this screen is for**
The **RMD Headquarters Mapping** screen links an RMDO to one or more HQs in one record.

1. Under **Assign Headquarters**, choose the **Employee (RMD)**.
2. Choose the **State**, then the **Region**.
3. Tap **Select Headquarters** and tick the HQs, or **Select All**. Press **Done**.
4. Press **Save Mapping**. **Clear** empties the form.
5. In **Existing RMD HQ Mappings**, tap the pencil to edit (then **Update Mapping**), or the cross to delete (then **Confirm**).

## Common problems

- **"No headquarters found."** Choose the region first, or the region has no HQs.
:::

::: section sanction-sequence
title: Set sanction number starting values
routes: /SanctionNumberSequence
minutes: 2
screenshot: sanction-sequence.png
screenshot-phone: sanction-sequence.phone.jpg

**What this screen is for**
An approved CSR-1 gets a sanction number. The **Sanction Number Sequence** screen sets the starting value of these numbers for each state or region.

1. Choose the **Sequence Scope**: **State (SMM)** or **Region (RM / RMM)**.
2. Choose the **State**, and for a region scope also the **Region**. The **State Code** and **Region Code** fill in from the masters.
3. Enter the starting value.
4. Press **Save Sequence**. **Clear** empties the form.
5. Check the list **Existing Sanction Number Sequences**. Search by state, region or code.

> Careful: A sequence cannot be changed after it is created.

## Common problems

- **"Code not configured in State master".** Add the state or region code in its master first.
:::

::: section create-csr1
title: Apply CSR-1 for a program
routes: /CREATE-CSR-1Management
minutes: 5
tour: 1 > [data-help=bc-c1-banner] | 2 > [data-help=bc-c1-search] | 3 > [data-help=bc-c1-plan] | 4 > [data-help=bc-c1-focus] | 5 > [data-help=bc-c1-expense] | 6 > [data-help=bc-c1-actions]

**What this screen is for**
CSR-1 is the plan for one program, made before the event. The **CSR-1 Program - Wise Budget Allocation** screen lists your approved program budgets. Pick one and fill the CSR-1 form.

1. Read the blue box. It says which month you are planning for.
2. Find the program in **Budget plan list** (use **Search Program...** or **Filters**), check **Remaining Programs**, and press **Apply CSR 1**.
3. Under **CSR-1 Plan Details**, check the program and fill **Budget**, **HQ**, **Location**, **Responsible Person** and **Date**.
4. Under **Focus Details**, choose at least one **Focused Crop** and a **Focused Product** for it. You can add up to three crops and three products in each box.
5. Under **Expense Breakdown**, enter the planned amount for each expense. **Total Expenses** must stay within the budget.
6. Press **Save Draft** to finish later, or **Submit for Validation** to send it.

> Tip: The **Responsible Person** will run the program and report CSR-2.

| Field | What to enter |
|---|---|
| **Budget** | Money for this program. Required. |
| **HQ** and **Location** | Where the program happens. Required. |
| **Responsible Person** | The officer who runs it. Required. |
| **Date** | The program date, inside the planned month. Required. |

## Common problems

- **"Budget Exceeded"** or **"Total Expense Breakdown ... exceeds the allocated Budget".** Lower the expense amounts.
- **"Please enter at least one expense amount."** Enter at least one planned expense.
- **"Please select at least one Product for Focus Crop 1."** Choose a product for each crop you picked.
- **"CSR-1 creation is not available for Admin role."** Admin can only view CSR-1 on **CSR-1 Management**.
:::

::: section csr1-list
title: Track, validate and approve CSR-1
routes: /CSR-1List
minutes: 3
screenshot: csr1-list.png
screenshot-phone: csr1-list.phone.jpg

**What this screen is for**
**CSR-1 Management** lists CSR-1 plans. Field officers track their own. MO and SMD validate them, and RM and SMM approve them.

1. Look at the cards: **Total CSR -1**, **Approved**, **Pending**, **Rejected** and **Drafts**.
2. Under **Filters**, set **From Date**, **To Date**, **Program Type**, **Program Name** or **Status**, then press **Apply Filters**.
3. In **CSR-1 plan list**, check the **Status**, **Sanction Number** and **CSR-2 Eligibility**. A **State** or **HQ** tag shows the level of the plan.
4. Tap the eye icon to open **CSR-1 Program Details**.
5. If it is waiting for you, press **Validate**, or the approve button (**Approve**, or the level name for a late plan). To refuse, press **Reject**, write the remarks and press **Confirm Reject**.
6. When the plan is **Approved**, press **CSR-2** on the row, or **Open CSR-2** in the details, to report the program.

> Careful: The bin icon deletes a CSR-1 at once, without asking again.

## Common problems

- **The details show "Awaiting MO Validation" or "Awaiting SMD Validation".** The plan must be validated before anyone can approve it.
- **The CSR-2 button is grey.** The CSR-1 is not **Approved** yet.
- **A red "Rejection Reason" box appears.** Read the reason under the list.
:::

::: section csr2-list
title: Report, validate and approve CSR-2
routes: /CSR-2List
minutes: 3
tour: 1 > [data-help=bc-c2-cards] | 2 > [data-help=bc-c2-filters] | 3 > [data-help=bc-c2-table] | 4 > [data-help=bc-c2-table] | 5 > [data-help=bc-c2-table]
screenshot: csr2-list.png
hotspots: 1 @ 3.9,11.8,92.1,11.7 | 2 @ 3.9,25.2,92.1,30.3 | 3 @ 3.9,57.1,92.1,27.8 | 4 @ 3.9,57.1,92.1,27.8 | 5 @ 3.9,57.1,92.1,27.8
screenshot-phone: csr2-list.phone.jpg
hotspots-phone: 1 @ 5.6,7.2,88.7,29.6 | 2 @ 5.6,37.8,88.7,36.9 | 3 @ 5.6,75.7,88.7,24.3 | 4 @ 5.6,75.7,88.7,24.3 | 5 @ 5.6,75.7,88.7,24.3

**What this screen is for**
CSR-2 is the report of a program after it happens. The **CSR-2 Program Execution** list shows your programs with attendance, media, expenses and progress.

1. Look at the cards: **Total CSR-2**, **Approved**, **Submitted** and **Drafts**. Tap a card to show only those programs.
2. Under **Filters**, set the dates, **Program Type**, **Program Name**, **Location** or **Status**, then press **Apply Filters**. **Clear All** removes them.
3. In **Assigned Programs**, check **Approved Budget**, **Attendance**, **Media**, **Actual Expense**, **Report Completion** and **Status**.
4. Press **View Details** to open the CSR-2 form. Fill the event date, photos, participants and each actual expense with its proof, then send it.
5. If a report is waiting for you, press **Validate** (MO or SMD) or the approve button, such as **Approve CSR-2** or **Approve Level 1**.

> Tip: **Report Completion** rises as the report moves from **Draft** to **Approved**.

## Common problems

- **"No CSR-2 records found."** No approved CSR-1 is assigned to you, or the filters are too narrow.
- **"You are not authorized to validate this CSR-2."** Another role must validate it.
- **The approval fails because of budget.** Ask RM, SMM or Admin for a budget reallocation.
:::

::: section csr2-assigned
title: Open my assigned programs
routes: /CSR-2ProgramExecution
minutes: 1

**What this screen is for**
This **CSR-2 Program Execution** screen lists the approved CSR-1 programs assigned to you, so you can start their CSR-2 reports.

1. Look at **Total CSR -2** at the top.
2. In **Assigned Programs**, find the program by its **Sanction Number**, **Program Type** or **Program**.
3. Check the **Approved Budget** and **Status**.
4. Press **View Program** to open the CSR-2 form for that program.
5. Press **Refresh Data** to load the latest list.

## Common problems

- **"No approved CSR-1 programs found."** None of your CSR-1 plans is approved yet.
:::

::: section csr2-form-preview
title: CSR-2 Management form
routes: /CSR2, /csrplandetails2
minutes: 1
screenshot: csr2-form-preview.png
screenshot-phone: csr2-form-preview.phone.jpg

**What this screen is for**
The **CSR-2 Management** screen shows the parts of a CSR-2 report: budget cards, plan details, focus details and the expense breakdown with proofs. To report a real program, open **CSR-2 List** and press **View Details**.

1. Check the cards: **Approved Budget**, **Actual Expense** and **Difference**.
2. See **CSR-2 Plan Details**: program, number of programs, budget, HQ and location.
3. See **Focus Details**: the focused crops and products.
4. See **Expense Breakdown**: each expense has an amount, a **Date** and **Upload proof**.

## Common problems

- **Save Draft or Submit CSR-2 does nothing here.** Use **CSR-2 List** and **View Details** to send a report.
:::

::: section final-report
title: See the final CSR report
routes: /FinalReportCSRView
minutes: 1
screenshot: final-report.png
screenshot-phone: final-report.phone.jpg

**What this screen is for**
**Final Report - CSR Program Tracking** shows each program from plan to report: the CSR-1 status, CSR-2 status, sanction number, expenses and remaining budget.

1. Look at the cards: **Total Programs**, **Approved**, **Pending**, **Rejected** and **Drafts**.
2. Use **Filters** to choose dates, **Program Type**, **Program Name**, **CSR Type** or **Status**, then press **Apply Filters**.
3. In **CSR-2 plan list**, compare **Total Expenses**, **Approved Budget** and **Remaining Budget**. **Report Status** shows **Completed** or **Pending**.
4. Tap the eye icon to see the program details, then **Print** or **Export PDF**.

## Common problems

- **The numbers do not change.** This screen is still being built. Use **CSR-1 Management** and **CSR-2 List** for real records.
:::

::: section rmd-queue
title: RMD validation queue
routes: /RMDValidationQueue
minutes: 2

**What this screen is for**
The **RMD Validation Queue** lists CSR-1 and CSR-2 submissions received from Market Officers.

1. Choose the tab: **CSR-1 Submissions** or **CSR-2 Reports**. The badge shows how many there are.
2. Open **Filters** to choose **Date Range**, **Program Type**, **Head Quarters** or **Status**, then press **Apply Filters**.
3. Type in the search box to find a submission.
4. Check **MO Name**, **Program Name**, **Head Quarters**, **Total Budget**, **Validation Due** and **Status**.
5. Press **Refresh Data** to load the latest list.

## Common problems

- **"Failed to load data".** Check your connection and press **Try Again**.
- **"No submissions found".** Press **Clear All** in Filters.
:::

::: section mo-validation
title: MO submission validation
routes: /MOSubmissionValidation
minutes: 1
screenshot: mo-validation.png
screenshot-phone: mo-validation.phone.jpg

**What this screen is for**
The **MO Submission Validation** screen lists CSR-1 and CSR-2 submissions from Market Officers, with planned and actual budgets.

1. Choose the tab: **CSR-1 Submissions** or **CSR-2 Reports**.
2. Use **Filters** to choose **Date Range**, **Program Type**, **Head Quatres** or **Status**.
3. In **CSR-2 Expense Report Validation List**, compare **Planned Budget**, **Actual Budget** and **Variance**.
4. Check **Validation Due** and **CSR - 1 Status**.

## Common problems

- **The list does not change.** This screen is still being built. Validate CSR-1 on **CSR-1 Management** and CSR-2 on **CSR-2 List**.
:::

::: section rm-approval-status
title: RM approval status
routes: /RMApprovalStatus
minutes: 1
screenshot: rm-approval-status.png
screenshot-phone: rm-approval-status.phone.jpg

**What this screen is for**
The **RM Approval Status** screen tracks CSR-1 and CSR-2 submissions that RMD validated and sent to RM.

1. Choose the tab: **CSR-1 Submissions** or **CSR-2 Report**.
2. Look at the cards: **Sent to RM**, **Pending RM Approval**, **Approved by RM** and **Rejected / Correction**.
3. In **CSR-1 RM Approval Status List**, check **Mo Submitted**, **RMD Validated** and **Status**.
4. Tap the eye icon to see the program details and the **Process Timeline**.

## Common problems

- **The list does not change.** This screen is still being built. Approve CSR-1 on **CSR-1 Management** and CSR-2 on **CSR-2 List**.
:::

::: section request-extension
title: Ask for a scheme extension
routes: /RequestExtension/{CSRId:int}
minutes: 2

**What this screen is for**
If the required turnover of a sales audit scheme is not reached in time, the **Request Extension** screen asks for a later end date. Open it from **Period-End Review**.

1. Check **Scheme Information (Read Only)**: **Sanction Number**, **Current Scheme Period**, **Required Turnover (TO)**, **Actual Turnover Achieved** and **Balance to Target**.
2. Choose the **Requested New End Date**. It must be after the current end date.
3. Choose the **Reason for Extension**.
4. Add **Remarks / Explanation (Optional)**, up to 500 characters.
5. Press **Submit Extension Request**.
6. The **Extension Request Submitted** box shows the **Request ID** and **Pending Approval**. Press **View My Request** or **Go to Sales Audit List**.

> Tip: The scheme end date does not change until the request is approved.

## Common problems

- **"Select the new end date."** or **"The new end date must be after ..."** Pick a later date.
- **"A request is already awaiting approval for this CSR."** Wait for the decision on the first request.
:::

::: section welfare-application
title: Review a welfare application
routes: /ApprovalDetail/{Id:int}, /MOApproval/{Id:int}
minutes: 3

**What this screen is for**
The **Application Details** screen shows one welfare (SDWA) application from a dealer, so the approver at the current level can check it and act. Open it from **Scheme Approval**.

1. Check the **Approval Workflow** at the top. It shows each level from MO to SDWA Admin.
2. Read **Applicant Information**, **Nominee & Beneficiary Information** and **Eligibility Information**.
3. Under **Uploaded Documents**, press **View** or **Download** to check each file.
4. Read **Previous Remarks** from earlier levels.
5. If it is your turn, choose the **Comments**. The **Recommendation** fills in. SDWA Admin also enters the **Cheque Number** and **Cheque Amount**.
6. Press **Submit**. Press **Back to Approvals** to return to the list.

## Common problems

- **There is no Submit button.** The application is not at your level now.
- **A red box says the application was rejected.** It was returned to the dealer for correction, or closed.
:::
