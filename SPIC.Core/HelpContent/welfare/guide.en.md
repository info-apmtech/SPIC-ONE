---
module: welfare
title: Welfare schemes (SDWA)
icon: bi-heart
order: 111
category: Dealers
color: #be123c
example: DEMO-WF-01 · Wedding Gift
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Dealers can apply for SDWA welfare schemes, such as a wedding gift, medical help or education help, from the app. Officers check each application step by step, and the dealer can see where it is at any time.

- apply | icon: bi-pencil-square | color: #be123c
  title: The dealer applies
  text: Check eligibility, choose a scheme, fill the form and upload the documents.
- approve | icon: bi-shield-check | color: #d97706
  title: Five checks
  text: MO, RM, SM, SDWA and SDWA Admin check it in turn.
- benefit | icon: bi-cash-coin | color: #16a34a
  title: Final approval
  text: SDWA Admin gives the final approval and records the cheque details.
:::

::: section flow
title: How an application moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- dealer | type: lane | color: #be123c | roles: Dealer
  title: Dealer
  sub: Applies and tracks
- mo | type: lane | color: #d97706 | roles: MO
  title: MO
  sub: Checks the HQ
- rm | type: lane | color: #0d9488 | roles: RM
  title: RM
  sub: Checks the region
- sm | type: lane | color: #2563eb | roles: SMM
  title: SM
  sub: Checks the state
- sdwa | type: lane | color: #7c3aed
  title: SDWA
  sub: SDWA · SDWA Admin
- choose | lane: dealer | col: 1 | icon: bi-ui-checks | color: #64748b | next: apply | screen: dashboard
  title: Check eligibility
  text: On the dashboard, press **Apply Now** under **Apply Welfare**. If you are eligible, choose a scheme.
- apply | lane: dealer | col: 2 | icon: bi-pencil-square | color: #64748b | next: mo-check | screen: apply-scheme
  title: Fill and submit
  status: Draft
  text: Fill the form, upload the documents and submit. Until then it is only a draft.
- mo-check | lane: mo | col: 3 | icon: bi-search | color: #d97706 | next: rm-check | screen: scheme-approval
  title: MO check
  status: Pending MO
  text: The MO checks the details, sales and documents of dealers in the HQ.
- rm-check | lane: rm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: sm-check | return: mo-check | screen: scheme-approval
  title: RM check
  status: Pending RM
  text: The RM checks it for the region. A rejection sends it back to the MO.
- sm-check | lane: sm | col: 5 | icon: bi-hourglass-split | color: #d97706 | next: sdwa-check | return: rm-check | screen: scheme-approval
  title: SM check
  status: Pending SM
  text: The SM checks it for the state. A rejection sends it back to the RM.
- sdwa-check | lane: sdwa | col: 6 | icon: bi-patch-check | color: #7c3aed | next: sdwaadmin | return: sm-check | screen: scheme-approval
  title: SDWA approval
  status: Pending SDWA
  text: SDWA approves it. A rejection sends it back to the SM.
- sdwaadmin | lane: sdwa | col: 7 | icon: bi-cash-coin | color: #7c3aed | next: approved | return: sdwa-check | screen: scheme-approval
  title: SDWA Admin final approval
  status: Pending SDWA Admin
  text: SDWA Admin gives the final approval and enters the cheque number and amount.
- approved | lane: dealer | col: 8 | icon: bi-check-circle | color: #16a34a | screen: my-applications
  title: Approved
  status: Approved
  text: The application is approved. The dealer sees **Approved** in **My Applications**.
- returned | lane: dealer | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: mo-check | to: apply | screen: my-applications
  title: Correct and resubmit
  status: Rejected by MO – Resubmission Required
  text: If the MO rejects it, it comes back to the dealer. Correct it and resubmit. It keeps the same Application ID.
:::

::: section statuses
title: Every status you can see
kind: statuses

- draft | color: #64748b
  label: Draft
  text: Saved but not sent.
- pending-mo | color: #d97706
  label: Pending MO
  text: With the MO for checking.
- pending-rm | color: #d97706
  label: Pending RM
  text: With the RM for checking.
- pending-sm | color: #d97706
  label: Pending SM
  text: With the SM for checking.
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: With SDWA for approval.
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: With SDWA Admin for the final approval.
- approved | color: #16a34a
  label: Approved
  text: Final approval given.
- resubmit | color: #dc2626
  label: Rejected by MO – Resubmission Required
  text: The dealer sees this. Correct the application and resubmit it.
- returned | color: #ea580c
  label: Returned to Dealer
  text: Officers see this. The application is back with the dealer for correction.
- rejected | color: #475569
  label: Rejected
  text: Closed. Apply again if needed.
- cancelled | color: #94a3b8
  label: Cancelled
  text: The application was cancelled.
- awaiting-mo | color: #d97706
  label: Awaiting MO Review
  text: Dashboard name for a new application waiting for the MO.
- under-mo | color: #d97706
  label: Under MO Review
  text: Dashboard name for an application back with the MO.
- under-rm | color: #d97706
  label: Under RM Review
  text: Dashboard name for Pending RM.
- under-smm | color: #d97706
  label: Under SMM Review
  text: Dashboard name for Pending SM.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- dealer | color: #be123c | roles: Dealer
  title: Dealer
  - Check eligibility and apply
  - Track applications and download the PDF
  - Correct and resubmit a returned application
- mo | color: #d97706 | roles: MO
  title: MO
  - Check applications from your HQ
  - Replace or delete a wrong document
  - Approve, or send back to the dealer
- rm-sm | color: #0d9488 | roles: RM, SMM
  title: RM and SM
  - Check applications from your region or state
  - Approve, or send back one step
- sdwa | color: #7c3aed
  title: SDWA and SDWA Admin
  - See applications from all areas
  - Give the last two approvals
  - SDWA Admin enters the cheque details
- director | color: #475569 | roles: Director
  title: Director
  - See all applications
  - Give an optional review for some schemes after SDWA approves
:::

::: section faq
title: Problems and answers
kind: faq

- returned | popular: true
  q: My application says "Rejected by MO – Resubmission Required"
  a: Open **My Applications**, read the reason in the red box, press **Correct & Resubmit**, fix the details or documents and press **Resubmit Application**. It keeps the same Application ID.
- not-eligible | popular: true
  q: The Welfare Eligibility window says I am not eligible
  a: The window lists each rule with **Required** and **Your dealership**. Rules in red are not met yet. Talk to your MO if you think a value is wrong.
- seven-days
  q: I get "Invitation must be sent at least 7 days in advance"
  a: For Wedding Gift, Sathabhishekam and Grahapravesam, the event date must be at least 7 days from today.
- cheque
  q: The form says "Cancelled Cheque / Bank Passbook is required"
  a: Upload a cancelled cheque or a bank passbook page, and type the **Beneficiary Name** exactly as it is printed there.
- where-is-it
  q: Who has my application now?
  a: In **My Applications**, the row of circles shows each step. Green is done and orange is the step that has it now.
- no-submit
  q: I am an officer and cannot approve an application
  a: You can act only when the application is at your stage and from your area. Check its status in the list.
:::

::: section dashboard
title: Use the dealer dashboard
routes: /SDWADashboard
minutes: 2
screenshot: dashboard.png

**What this screen is for**
The **Dashboard** is the dealer's home for welfare applications and guest house bookings. Start a new application here and see how your applications are doing.

1. At the top, check your name, **Dealer Code**, **Region** and **Profile Completion**.
2. Under **Quick Actions**, press **Apply Now** on the **Apply Welfare** card. The app checks your eligibility first.
3. The **Welfare Eligibility** window shows your **State**, **State Group**, **Dealer Type** and each **Eligibility Criteria** rule. If you are eligible, press **Proceed to Apply**. If not, press **Close**.
4. Look at **Application Status Overview**: **Pending Applications**, **Approved Applications**, **Rejected Applications** and **Processing Applications**.
5. Under **Recent Applications**, press **View Details** to open one, or **View All** to open **My Applications**.
6. Read **Notifications** and **Recent Activity** for the latest changes.

> Tip: **Book Room** and **My Bookings** on this screen are for the guest house. See the Guest house guide.

## Common problems

- **"Unable to verify eligibility. Please try again later."** Check your internet and press **Apply Now** again.
- **"No applications yet".** You have not applied yet. Press **Apply Welfare**.
- **I do not see Apply Welfare.** SDWA Admin users do not apply from this screen.
:::

::: section choose-scheme
title: Choose a welfare scheme
routes: /WelfareSchemes
minutes: 1
screenshot: choose-scheme.png
screenshot-phone: choose-scheme.phone.jpg

**What this screen is for**
The **Apply for Welfare Schemes** screen shows every SDWA scheme as a card. Pick the one you need.

1. Look at the cards: **Medical Assistance**, **Wedding Gift**, **Grahapravesam**, **Educational Assistance**, **Sathabhishekam**, **Death Relief Assistance**, **Merit Award** and **Distinction Award**.
2. On each card, check **Documents** (how many you need) and **Eligibility** (who can apply).
3. Press **Apply Now** on the scheme you want. The application form opens for that scheme.

> Careful: **Distinction Award** is only for dealers and employees in Tamil Nadu (TN).

## Common problems

- **I cannot open this screen.** Your login may not have welfare access. Ask your MO or admin.
:::

::: section grahapravesam
title: Read about the Grahapravesam scheme
routes: /GrahapravesamScheme
minutes: 1
screenshot: grahapravesam.png
screenshot-phone: grahapravesam.phone.jpg

**What this screen is for**
The **Grahapravesam** screen explains the housewarming gift scheme before you apply.

1. Read the top card and **About the Scheme**.
2. Check the **Eligibility Criteria** and the **Required Documents**.
3. Read the **Frequently Asked Questions** and the **Important Notes**.
4. Press **Apply Now** to open the form, or **Back to Schemes** to choose another scheme.

## Common problems

- **The form will not take my housewarming date.** The form needs a date at least 7 days from today.
:::

::: section apply-scheme
title: Apply for a welfare scheme
routes: /ApplyWelfareScheme
minutes: 5
tour: 1 > [data-help=wf-dealer] | 2 > [data-help=wf-quantity] | 3 > [data-help=wf-details] | 4 > [data-help=wf-documents] | 5 > [data-help=wf-declaration] | 6 > [data-help=wf-actions]
screenshot: apply-scheme.png
hotspots: 1 @ 2.2,44.3,95.5,38.7 | 2 @ 2.2,85.9,95.5,14.1 | 3 @ - | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: apply-scheme.phone.jpg
hotspots-phone: 1 @ 3.1,26.6,93.8,43.2 | 2 @ 3.1,71.5,93.8,28.5 | 3 @ - | 4 @ - | 5 @ - | 6 @ -

**What this screen is for**
Use the **Apply for** screen (for example **Apply for Wedding**) to fill and send your application. The form changes a little for each scheme.

1. Check your dealer card (**Location Info** and **Personal Info**) and read the **Eligibility Conditions** box for the scheme.
2. Under **Quantity Lifted**, choose **Quantity Lifted during Last Year (MT)**. The 3-year totals fill by themselves. **Beneficiary Details** then opens: choose **Select Beneficiary Group** and fill the nominee and beneficiary.
3. Fill the scheme part, for example **Wedding Details** with the **Wedding Date**, or **Medical Assistance Details** with the **Treatment / Medical Condition**.
4. Under **Document Upload**, tap **Click to upload** for each document. For **Cancelled Cheque / Bank Passbook**, also type the **Beneficiary Name** as printed on it.
5. Under **Declaration**, read the text and tick the box.
6. Press **Save as Draft** to keep it, or **Preview Application**. Check it, press **Submit Application**, then **Confirm**. Note the **Application ID** and press **Track Application**.

> Careful: After you press **Confirm**, the application goes to the MO and you cannot change it unless it is sent back to you.
> Tip: A saved draft gets a reference number and is counted under **Drafts** in **My Applications**.

## Fields explained

| Field | What to enter |
|---|---|
| **Quantity Lifted during Last Year (MT)** | Your lifting range last year. Required. |
| **Select Beneficiary Group** | **Direct Dealer**, **Sub Dealer** or **Approved Employee**. Required. |
| **Nominee Name** and **Nominee Relationship** | The nominee from your dealer records. Required. |
| **Select Beneficiary** | A family member, or **Add new beneficiary**. Required for a direct dealer. |
| Event date | Wedding, Sathabhishekam and Grahapravesam: at least 7 days from today. |
| **Date of Death** and **Legal Heir Relation** | Death Relief only. If the relation is **Others**, upload the affidavit. |
| Documents | PDF, JPG or PNG, up to 5 MB each. |

## Common problems

- **"Please fix the following errors:"** Read the list above the buttons and fix each item.
- **Nothing happens when I upload a file.** The file must be PDF, JPG or PNG and smaller than 5 MB.
- **"Minimum 80% marks is required for the Merit Award."** The Merit Award needs at least 80%. The Distinction Award needs more than 80% and no arrears.
- **"A minimum average annual sales of 50,000 MT..."** Your sales are too low to apply for a sub dealer (5,000 MT for an employee).
:::

::: section my-applications
title: Track my applications
routes: /WellfareApplication
minutes: 2

**What this screen is for**
The **My Applications** screen shows every welfare application you made, with its status and the steps it has passed.

1. Look at the tiles at the top: **Total**, **Pending**, **Approved**, **Rejected**, **Drafts** and **Cancelled**.
2. Choose **Latest First** or **Oldest First**, pick a status, or type in **Search by Application ID or Scheme**.
3. On each card, see the scheme, the Application ID and the status. The row of circles shows **Dealer**, **MO**, **RM**, **SM/SMM**, **SDWA**, **SDWA Admin** and **Final**. Green is done, orange is where it is now.
4. Press **View Details** to see everything, or **Download PDF** to save a copy.
5. If a red box says **Rejected by**, read the **Reason** and press **Correct & Resubmit**. Fix the form, press **Resubmit Application**, then **Confirm Resubmission**.
6. Press **Apply for a scheme** to start a new application.

> Tip: A resubmitted application keeps the same Application ID and starts again at the MO.

## Common problems

- **"No matching applications".** Press **Clear filters**.
- **"Failed – Tap to Retry" on Download PDF.** Check your internet and tap it again.
- **No Correct & Resubmit button.** Only an application sent back to you by the MO can be resubmitted.
:::

::: section scheme-approval
title: Check and approve welfare applications
routes: /SchemeApproval
minutes: 3
tour: 1 > [data-help=wf-ap-cards] | 2 > [data-help=wf-ap-tabs] | 3 > [data-help=wf-ap-search] | 4 > [data-help=wf-ap-table] | 5 > [data-help=wf-ap-table] | 6 > [data-help=wf-ap-pages]
screenshot: scheme-approval.png
hotspots: 1 @ 2.2,13.5,95.5,23.4 | 2 @ 2.2,39.4,97.8,5.9 | 3 @ 61.8,50.7,33.6,4.3 | 4 @ 4.6,57.1,90.8,18.7 | 5 @ 4.6,57.1,90.8,18.7 | 6 @ 4.6,78,90.8,5.9
screenshot-phone: scheme-approval.phone.jpg
hotspots-phone: 1 @ 3.1,10.4,93.8,21.3 | 2 @ 3.1,33,93.8,14.5 | 3 @ 6.9,52.9,86.2,9.3 | 4 @ - | 5 @ - | 6 @ 6.9,84.3,86.2,4.5

**What this screen is for**
MO, RM, SM, SDWA and SDWA Admin use the **Approval** screen to check welfare applications waiting at their stage. MO sees the HQ, RM the region, SM the state. SDWA and SDWA Admin see all areas.

1. Look at the cards: **Total Applications**, **Pending** for your stage (for example **Pending MO**), **Validated by MO**, **Rejected** and **Completed**.
2. Choose a tab, such as **Pending**, **Approved by** your stage, **Validated by MO**, **Recommended by RM**, **Approved by SM**, **Rejected** or **Completed**.
3. Type in **Search dealer / application...** or choose a scheme in **All Schemes**.
4. In the table, see the **Status** and the **Approval Flow (MO → RM → SM → SDWA → SDWA Admin)**. A green tick is done, the blue circle is the stage that has it now, a red cross is a rejection.
5. Tap the eye icon to open an application. Check the details and documents, choose **Comments**, and the **Recommendation** fills by itself. Press **Submit**.
6. Use **Items Per Page** and the arrows to see more rows.

> Careful: A comment that rejects sends the application one step back. A rejection by the MO sends it back to the dealer.
> Tip: SDWA Admin must also enter the **Cheque Number** and **Cheque Amount**, and can add a **Cheque Image**.

## Approval order

- MO, then RM, then SM, then SDWA, then SDWA Admin gives the final approval.
- Admin, Corporate Admin and Director can open the list but do not approve. Director can add an optional review for Death Relief, Educational Assistance, Merit Award and Medical Assistance after SDWA approves.

## Common problems

- **"No applications found for this view."** Nothing is waiting in this tab. Try another tab.
- **"Unable to load the approval queue. Please try again."** Press **Retry**.
- **No Submit button.** The application is not at your stage now.
- **"Please select a comment."** Choose a comment before you press **Submit**.
:::

::: section company-master
title: Manage SDWA companies
routes: /SdwaCompanyMaster
minutes: 2
screenshot: company-master.png
screenshot-phone: company-master.phone.jpg

**What this screen is for**
The **SDWA Company Details** screen keeps the list of companies linked to the guest houses for billing.

1. Look at the cards: **Total Companies**, **Active** and **Inactive**.
2. Under **Add New Company**, enter the **Company Name**, **Short Name / Code** and **GSTIN**.
3. In **Guest House(s)**, type and tap each guest house. Tap the small x on a name to remove it.
4. Set the switch to **Active** or **Inactive**, then press **Save**. **Cancel** clears the form.
5. In **Existing Companies**, search or choose **All Status**, **Active** or **Inactive**. Use the pencil to edit, tap the status to switch it, or the bin to delete.

> Careful: Delete cannot be undone. A company linked to bookings cannot be deleted.

## Common problems

- **"GSTIN is required."** or **"Company Name is required."** Fill both fields.
- **"At least one Guest House must be selected."** Pick at least one guest house.
- **I do not see the Add New Company form.** Your login can only view this list.
:::
