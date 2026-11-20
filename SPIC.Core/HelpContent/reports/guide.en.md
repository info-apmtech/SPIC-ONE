---
module: reports
title: Reports
icon: bi-bar-chart
order: 116
category: Reports
color: #1d4ed8
example: DEMO-WF-01 · Pending SDWA
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

See the main numbers in one place. The **Admin Dashboard** shows welfare applications, dealers and guest house bookings. The **Reports Center** shows a list of reports.

- dashboard | icon: bi-speedometer2 | color: #1d4ed8
  title: Admin Dashboard
  text: Welfare applications, dealers and guest house numbers on one page.
- queue | icon: bi-list-check | color: #d97706
  title: Pending queue
  text: See which applications are waiting, at which stage and for how many days.
- reports | icon: bi-file-earmark-bar-graph | color: #7c3aed
  title: Reports Center
  text: A list of reports. It is still being built, so the reports shown are examples.
:::

::: section flow
title: How you use these screens
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- office | type: lane | color: #1d4ed8 | roles: Admin, CorporateAdmin, Director
  title: SDWA Admin and office
  sub: SDWA Admin · Admin · Director
- approvers | type: lane | color: #d97706 | roles: MO, RM, SMM
  title: Approvers
  sub: MO · RM · SM · SDWA · SDWA Admin
- open | lane: office | col: 1 | icon: bi-speedometer2 | color: #1d4ed8 | next: check | screen: admin-dashboard
  title: Open the Admin Dashboard
  text: Open **SDWA** in the menu and choose **Admin Dashboard**.
- check | lane: office | col: 2 | icon: bi-list-check | color: #d97706 | next: act, reports | screen: admin-dashboard
  title: Check the numbers and the queue
  text: See the cards, the **Approval Workflow Overview** and the **Pending Action Queue (SDWA)**.
- act | lane: approvers | col: 3 | icon: bi-patch-check | color: #16a34a
  title: Approve on the Scheme Approval screen
  text: Press **Review Pending Approvals** under **Quick actions**. Approving is done on the **Scheme Approval** screen, not on the dashboard.
- reports | lane: office | col: 3 | icon: bi-file-earmark-bar-graph | color: #7c3aed | screen: reports-center
  title: Look at the report list
  text: Open the **Reports Center** to see the reports. It is still being built.
- no-data | lane: office | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: check | to: open | screen: admin-dashboard
  title: Data could not load
  text: A red box at the top says **Could not load** and the boxes show **—**. Open the dashboard again later.
:::

::: section statuses
title: Every status you can see
kind: statuses

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
- high | color: #dc2626
  label: High
  text: Priority in the queue. The dealer applied 5 or more days ago.
- medium | color: #ea580c
  label: Medium
  text: Priority in the queue. The dealer applied 2 to 4 days ago.
- low | color: #16a34a
  label: Low
  text: Priority in the queue. The dealer applied today or yesterday.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your access allows.

- sdwa-admin | color: #1d4ed8
  title: SDWA Admin
  - Watch all welfare applications on the Admin Dashboard
  - Open pending approvals from **Quick actions**
  - Check guest house bookings
- office | color: #0d9488 | roles: Admin, CorporateAdmin, Director
  title: Admin, Corporate Admin, Director
  - See every application still being checked
  - See dealer and guest house numbers
  - Open the Reports Center
- approvers | color: #d97706 | roles: MO, RM, SMM
  title: MO, RM, SM
  - If you can open the Admin Dashboard, the queue shows the applications at your stage
  - You see only your HQ, region or state
:::

::: section faq
title: Problems and answers
kind: faq

- dash | popular: true
  q: Some boxes show — instead of a number
  a: Either the data could not load (the red box at the top says which part), or the box has no data yet. **Active Schemes**, **Benefit Released**, **SLA Delayed**, **Upcoming Check-ins**, **Cancelled**, **Revenue Collected** and the **Finance & Benefit Release** boxes always show — for now.
- download | popular: true
  q: I press Download in the Reports Center and nothing happens
  a: The Reports Center is still being built. **Download** does not make a file yet, and the reports and numbers on it are examples, not your data.
- icons
  q: The tick and cross icons in the queue do nothing
  a: These icons do not work yet. Press **Review Pending Approvals** under **Quick actions** and approve or reject on the **Scheme Approval** screen.
- nothing-pending
  q: The queue says "Nothing pending"
  a: No application is waiting at your stage. MO, RM and SM see only their own HQ, region or state.
- could-not-load
  q: I see "Could not load: welfare approvals"
  a: Welfare data is shown only to MO, RM, SM, SDWA, SDWA Admin, Admin, Corporate Admin and Director. If you should see it, ask your admin to check your access, then open the page again.
:::

::: section admin-dashboard
title: Read the Admin Dashboard
routes: /ReportDashboard
minutes: 3
tour: 1 > [data-help=rpt-kpis] | 2 > [data-help=rpt-workflow] | 3 > [data-help=rpt-queue] | 4 > [data-help=rpt-charts] | 5 > [data-help=rpt-guesthouse] | 6 > [data-help=rpt-quick]

**What this screen is for**
The **Dashboard** for SDWA Admin shows welfare applications, dealers and guest house bookings on one page. Open it from **SDWA** > **Admin Dashboard** in the menu. You only look here. Approving is done on the **Scheme Approval** screen.

1. Look at the cards at the top: **Total Welfare Applications**, **Pending Final Approval**, **Approved Applications**, **Rejected Applications**, **Total Dealers** and **Guest House Bookings**.
2. In **Approval Workflow Overview**, see the stages from **Dealer** to **Completed**, and how many applications are **Pending at MO**, **Pending at RM**, **Pending at SMM**, **Pending at SDWA** and **Rejected**.
3. In **Pending Action Queue (SDWA)**, see each waiting application with its **Current Stage**, **Pending Since** and **Priority**. Press **Show more** to see more rows.
4. **Scheme Distribution** shows how many applications each scheme has. **Monthly Application Trend** shows applications per month. Below them are the dealer counts and the **Regional Distribution** table.
5. **Guest House Overview** shows bookings and how full each guest house is. Press **View All Bookings** to open the front office.
6. Under **Quick actions**, press **Review Pending Approvals** to approve applications. **Dealer Directory**, **Manage Schemes** and **Guest House Bookings** open those screens.

> Careful: The eye, tick, cross and download icons in the queue, the **Search zones...** boxes, and the **Generate Reports** and **Gallery** cards do not work yet.

## Other parts of the screen

- **Recent Activities** shows the last 5 events, such as a new application, an approval or a guest house bill.
- **Finance & Benefit Release** has no data yet. It shows — and "No payment releases".

## Common problems

- **The red box says "Could not load".** That part of the data did not load. Its boxes show —. Open the page again later.
- **Pending at MO looks high.** It also counts applications sent back to the dealer for correction.
- **Pending Since looks too long.** It counts the days since the dealer applied, not since the application reached this stage.
- **"Nothing pending" in the queue.** No application is waiting at your stage now.
:::

::: section reports-center
title: Look at the Reports Center
routes: /ReportsCenter
minutes: 1
screenshot: reports-center.png
screenshot-phone: reports-center.phone.jpg

**What this screen is for**
The **Reports Center** will let you see and download booking, billing, approval and dealer reports. It is still being built: the numbers, reports and dates on it are examples, and the filters and downloads do not work yet.

1. Look at the cards at the top: **Total Reports**, **Booking Reports**, **Billing Reports** and **Approval Reports**.
2. The tabs (**All Reports**, **Booking Reports**, **Billing Reports**, **Approval Reports**), the **Search** box and the **All Categories** and **All Formats** lists are shown, but they do not change the list yet.
3. Each report card shows the report name, what it contains, **Available Formats** and a date. The cards are **Dealer Information**, **Tirupathi Bookings**, **T-Nagar Bills**, **MO Reports**, **RM Reports** and **Tirupathi Bills**.
4. Press **Preview** to open a sample with **Category**, **Records**, **File Types**, **Generated By** and a small table. Press **Close** or the X to close it.
5. **Download**, **Download PDF** and **Download Excel** do not make a file yet.

> Careful: Do not use the numbers on this screen. They are examples, not your real data.

## Common problems

- **Nothing happens when I press Download.** Downloads are not ready yet.
- **Every Preview shows Dealer Information.** All Preview buttons open the same sample for now.
- **The tabs and search do not change the list.** They are not working yet.
:::
