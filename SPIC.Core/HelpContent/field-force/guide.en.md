---
module: field-force
title: Field force
icon: bi-person-walking
order: 107
category: Field work
color: #ea580c
example: DEMO-JMDO · Week plan 5 dealers
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Plan the field work for each week, get it approved, mark attendance and do the tasks in the field. Managers see who is working, where they are and how far each person is against the targets.

- plan | icon: bi-calendar-week | color: #ea580c
  title: The JMDO plans the week
  text: Pick the dealers for each day, set the SPCM, Soil Sample and PoS liquidation targets, and send the plan.
- approve | icon: bi-person-check | color: #2563eb
  title: The MDO approves it
  text: The MDO checks the plan, changes dealers, dates or targets if needed, and approves it.
- work | icon: bi-geo-alt | color: #16a34a
  title: Work in the field, watched live
  text: The JMDO marks attendance, starts and closes each task. Managers follow tasks, location and reports.
:::

::: section flow
title: How a week of field work moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- jmdo | type: lane | color: #ea580c | roles: JMDO
  title: JMDO
  sub: Field staff
- mdo | type: lane | color: #2563eb | roles: MDO, MO
  title: MDO
  sub: Team manager at your HQ
- plan | lane: jmdo | col: 1 | icon: bi-calendar-week | color: #d97706 | next: approve | screen: plan-week
  title: Plan the week
  status: Pending
  text: Choose dealers for each day, enter the weekly targets and press **Request**. The plan waits for the MDO.
- approve | lane: mdo | col: 2 | icon: bi-person-check | color: #2563eb | next: attend | screen: review-allocation
  title: Check and approve
  status: Approved
  text: The MDO can add or remove dealer tasks, change dates, priority and targets, then press **Approve Allocation**.
- attend | lane: jmdo | col: 3 | icon: bi-calendar-check | color: #0d9488 | next: work | screen: mark-attendance
  title: Mark attendance
  status: Present
  text: Each working day, mark attendance in the SPIC ONE mobile app before starting any task.
- work | lane: jmdo | col: 4 | icon: bi-play-circle | color: #7c3aed | next: close | screen: todays-tasks
  title: Start a task
  status: In Progress
  text: On **Today's Allocation**, press **Start**, then **Start Task**. Time and location are recorded.
- close | lane: jmdo | col: 5 | icon: bi-check-circle | color: #16a34a | next: monitor | screen: todays-tasks
  title: Save and close
  status: Completed
  text: Fill the **Close-out form**, press **Save Record**, then **Stop Task** to close it.
- monitor | lane: mdo | col: 6 | icon: bi-list-check | color: #475569 | screen: task-monitoring
  title: Follow up
  status: Completed
  text: The MDO watches every task on **Task Monitoring**, team location on **Team Tracking**, and progress on **Reports**.
- no-attendance | lane: jmdo | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: work | to: attend | screen: mark-attendance
  title: Attendance not marked
  status: Yet to Start
  text: A task cannot start before today's attendance is marked. The app opens **Mark Attendance** for you.
:::

::: section statuses
title: Every status you can see
kind: statuses

- pending | color: #d97706
  label: Pending
  text: The weekly plan is sent and waits for the MDO's approval. The JMDO sees "awaiting MDO approval".
- approved | color: #16a34a
  label: Approved
  text: The MDO approved the weekly plan. Its tasks can be started on their day.
- rejected | color: #dc2626
  label: Rejected
  text: The weekly plan was not accepted. Update the plan and send it again.
- requested | color: #d97706
  label: Requested
  text: On Task Monitoring, a task whose weekly plan is still waiting for approval.
- yet-to-start | color: #64748b
  label: Yet to Start
  text: The task is approved but not started.
- not-started | color: #64748b
  label: Not Started
  text: On Reports, the same as Yet to Start.
- in-progress | color: #2563eb
  label: In Progress
  text: The JMDO pressed Start Task and is working on it.
- completed | color: #16a34a
  label: Completed
  text: The task is closed.
- fully | color: #16a34a
  label: Fully Completed
  text: Close-out outcome: all the work was done.
- partially | color: #d97706
  label: Partially Completed
  text: Close-out outcome: only part of the work was done.
- not-completed | color: #ea580c
  label: Not Completed
  text: Close-out outcome: the work could not be done.
- present | color: #16a34a
  label: Present
  text: Attendance for a full working day. Puts you On Duty.
- half-day | color: #0d9488
  label: Half Day
  text: Attendance for half a day. Puts you On Duty.
- on-leave | color: #7c3aed
  label: On Leave
  text: You are on leave today. You are Off Duty.
- absent | color: #dc2626
  label: Absent
  text: Shown in the weekly summary for a day marked absent.
- active | color: #16a34a
  label: Active
  text: On the team dashboard, a JMDO who is not on leave today.
- on-duty | color: #16a34a
  label: On Duty
  text: Attendance is marked as Present or Half Day today.
- off-duty | color: #64748b
  label: Off Duty
  text: No attendance yet today, or On Leave.
- live | color: #16a34a
  label: Live
  text: Live tracking is on and the location is arriving now.
- stale | color: #d97706
  label: Stale
  text: Live tracking is on, but no new location came in the last few minutes.
- offline | color: #dc2626
  label: Offline
  text: Live tracking is on, but no location came for more than 15 minutes.
- auto-ended | color: #475569
  label: Ended automatically
  text: Tracking was silent for a long time, so the app ended it.
- stopped | color: #64748b
  label: Stopped
  text: The JMDO pressed Stop Tracking.
- never | color: #94a3b8
  label: Never started
  text: The JMDO has not started live tracking.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- jmdo | color: #ea580c | roles: JMDO
  title: JMDO (field staff)
  - Plan the week and press Request
  - Mark attendance every working day
  - Start, save and close each task
  - Start and stop Live Tracking
  - Check My Reports
- mdo | color: #2563eb | roles: MDO, MO
  title: MDO
  - Approve the weekly plans of your JMDOs
  - Set the yearly targets for each JMDO
  - Follow tasks, team location and reports
- admin | color: #475569 | roles: Admin
  title: Admin
  - See plans from every HQ
  - Correct a plan even after it is approved
:::

::: section faq
title: Problems and answers
kind: faq

- cannot-start | popular: true
  q: I cannot start my task
  a: Two things must be true. Your weekly plan must be **Approved** by the MDO, and you must mark today's attendance. If attendance is missing, the app opens **Mark Attendance** for you.
- web-attendance | popular: true
  q: Mark Attendance says it can only be marked from the mobile app
  a: Attendance, Live Tracking and Track Playback work only in the SPIC ONE app on your phone. Open the app on your phone and try again.
- already-sent
  q: "You've already submitted an allocation for that week."
  a: You can send only one plan per week. Ask your MDO to add or remove dealers, or change dates and targets, on the **Allocation Requests** screen.
- spcm-not-closed
  q: "SPCM target not yet reached" when I try to close the task
  a: SPCM and PoS liquidation close only when the weekly target is reached. Keep saving records on other days. The task closes by itself when the target is reached.
- no-jmdos
  q: I see "No JMDOs found for your headquarters"
  a: Your team is the JMDOs with the same HQ as you. If someone is missing, ask the admin to check the HQ on their profile.
- offline
  q: Team Tracking shows Stale or Offline for a JMDO
  a: The JMDO's phone has not sent a location for some time. It may have no signal or the app may be closed. Call the JMDO and ask them to open the app.
:::

::: section field-dashboard
title: My field dashboard
routes: /FieldDashboard
minutes: 2
screenshot: field-dashboard.png
screenshot-phone: field-dashboard.phone.jpg

**What this screen is for**
**My Dashboard** is the JMDO's home screen. It shows whether you are on duty, how many tasks you have today, Live Tracking, and the tasks you have not finished.

1. At the top, see your name and **On Duty** or **Off Duty**. If you have not marked attendance, press **Mark Attendance**.
2. Look at the cards: **Today's Tasks**, **Yet to Start**, **In Progress** and **Completed**.
3. To turn on Live Tracking, press **Start Live Tracking**. Read the **Background location access** message and press **Continue**.
4. While tracking is on, press **View Route** to see your route, or **Stop Tracking** and then **Stop Tracking** again to end it. **Keep Tracking** cancels.
5. Under **Continue where you left off**, press **view** to go back to a task in progress. **View All** opens **Tasks Allocation**.

> Tip: Live Tracking starts only when you press the button. It does not start with attendance or tasks.

## Common problems

- **"Mark today's attendance first to enable Start Live Tracking."** Mark attendance, then come back.
- **"Mobile app only".** Live Tracking can be started only from the SPIC ONE app on your phone.
- **"Nothing in progress right now".** You have no started task. Open **Tasks Allocation** to start one.
:::

::: section plan-week
title: Plan and request my weekly tasks
routes: /TasksAllocation
minutes: 4
tour: 1 > [data-help=ff-week] | 2 > [data-help=ff-steps] | 3 > [data-help=ff-days] | 4 > [data-help=ff-filters] | 5 > [data-help=ff-dealers] | 6 > [data-help=ff-next]
screenshot: plan-week.png
hotspots: 1 @ 4.6,28.4,90.8,6.9 | 2 @ 4.6,37.8,26.2,37.3 | 3 @ 35.4,42.4,60,4.6 | 4 @ 35.4,49,60,11.9 | 5 @ 35.4,63.2,60,14.4 | 6 @ 78.4,80.6,17.1,5.1
screenshot-phone: plan-week.phone.jpg
hotspots-phone: 1 @ 6.8,18.8,86.4,15.1 | 2 @ 6.8,35.7,86.4,5.1 | 3 @ 6.8,46.5,86.4,3.7 | 4 @ 6.8,51.3,86.4,17.5 | 5 @ 6.8,70.2,86.4,10.6 | 6 @ 6.8,82.1,86.4,3.9

**What this screen is for**
On **Tasks Allocation**, the **All Allocations** tab is where the JMDO plans a week (Sunday to Saturday) in five steps and sends it to the MDO for approval.

1. Use **Previous Week** and **Next Week** to choose the week you want to plan.
2. The steps on the left are **Dealer**, **SPCM**, **Soil Sample**, **MD Program** and **PoS liquidation**. Tap a step to open it.
3. In the **Dealer** step, tap a day tab to choose the day you are planning.
4. Find dealers with the search box (**Search dealer name or code...**) or the **All Districts**, **All Sub Districts** and **All Dealers** lists.
5. Tap a dealer card to select it for that day. Tap it again to remove it. The day tab shows how many dealers you chose.
6. Press **Next: SPCM Target**. Enter **This Week's Target** for SPCM and for Soil Sample, check the MD programs, enter the PoS liquidation quantities (**Urea**, **DAP**, **NPKS**, **Others**), then press **Request**.

> Tip: MD programs given to you in CSR1 are added by themselves. You cannot add or remove them here.
> Careful: You can send only one plan per week. After **Request**, only the MDO can change it.

| Field | What to enter |
|---|---|
| **This Week's Target** (SPCM) | How many SPCM you plan to achieve this week. |
| **This Week's Target** (Soil Sample) | How many soil samples you plan to collect this week. |
| **Urea**, **DAP**, **NPKS**, **Others** | PoS liquidation quantity you plan for each product this week. |

## Common problems

- **"Fill in at least one step before submitting."** Choose at least one dealer or enter at least one target.
- **"You've already submitted an allocation for that week."** Ask your MDO to change the plan on **Allocation Requests**.
- **"Allocation submitted — awaiting MDO approval".** The plan is sent. Tasks open on **Today's Allocation** after the MDO approves it.
- **"Allocation Rejected".** Press **Update & Resubmit**, correct the plan and press **Request** again.
:::

::: section mark-attendance
title: Mark today's attendance
routes: /MarkAttendance
minutes: 2

**What this screen is for**
Mark your attendance every working day before you start any task. This works only in the SPIC ONE app on your phone, because it records where you are.

1. Open **Mark Attendance** from the menu, or press **Mark Attendance** on **My Dashboard**.
2. Allow the app to use your location. **Visit Area / Place** is filled from your location. Correct it if needed, or type it if no location is found.
3. Check the **Date**.
4. Choose the **Attendance Status**: **Present**, **Half Day** or **On Leave**.
5. Press **Update Attendance**. You see "Attendance updated." and go back to **My Dashboard**.
6. To change it later the same day, press **Edit** next to **Attendance marked today**.
7. On the right, see **Current Status** (**On Duty** or **Off Duty**) and the **Weekly Summary**: attendance, working hours, tasks started and distance for each day.

> Careful: After 10:00 AM the screen shows "Submitted late for today". You can still save it.

| Field | What to enter |
|---|---|
| **Date** | Today's date. Required. |
| **Visit Area / Place** | The village or town where you are working. Required when the location is not captured. |
| **Attendance Status** | **Present**, **Half Day** or **On Leave**. |

## Common problems

- **"Attendance can only be marked from the SPIC mobile app".** Open SPIC ONE on your phone.
- **"Location could not be captured. Please type your Visit Area / Place to save attendance."** Turn on location, or type the place yourself.
- **"Date is required."** Choose the date before pressing **Update Attendance**.
:::

::: section todays-tasks
title: Start, save and close today's tasks
routes: /TaskDetail
minutes: 4
screenshot: todays-tasks.png
screenshot-phone: todays-tasks.phone.jpg

**What this screen is for**
Each approved task opens on its own screen. You start it, record what you did, and close it. The app records the time on the task and where you started it.

1. On **Tasks Allocation**, open **Today's Allocation**. Choose **Yet to Start**, **In Progress**, **Completed** or **All**, then press **Start** (or **Continue**) on a task card.
2. Check the **Task Timeline** (**Assigned**, **Requested**, **Approved**, **Started**, **Closed**) and the task details.
3. Press **Start Task**. The app captures your location and starts the **Time on task** clock.
4. In the **Close-out form**, choose the **Outcome**. For a dealer, press **+ Add another Product** and add the **Product Name** and **Stock**. For SPCM, enter **Achieved Target**. For PoS liquidation, enter **Urea**, **NPKS**, **DAP** and **Other**.
5. Write **Remarks**, attach the **Photo Proof** (dealer) or **Supporting Document**, and press **Save Record**. Saved records show in a table on the right.
6. If the visit needs it, use **Quick actions**: **Soil Sample Collection** or **Add Farmer Data**.
7. When the work is done, press **Stop Task**, then **Close Task**.

> Careful: A closed task cannot be restarted.
> Tip: SPCM and PoS liquidation close by themselves when the weekly target is reached ("Record saved - weekly target reached, task completed!").

## Common problems

- **"Allocation Awaiting MDO Approval".** The **Start Task** button stays grey until the MDO approves your weekly plan.
- **"Mark today's attendance before starting a task."** The app opens **Mark Attendance**. Mark it, then start the task.
- **"Please start the task before saving records."** Press **Start Task** first.
- **The Soil Sample card opens Sample Collection.** This is normal. Soil samples are recorded on the sample collection screens.
:::

::: section task-history
title: See my task history and weekly summary
routes: /TasksAllocation
minutes: 2
screenshot: task-history.png
screenshot-phone: task-history.phone.jpg

**What this screen is for**
The **Task History** and **Task Summary** tabs of **Tasks Allocation** show your tasks from every week, and how much of each week's target is done.

1. On **Tasks Allocation**, open **Task History**. It opens on today's tasks.
2. Choose **From**, **To**, **Dealer** and **Status**, then press **Search**. **Today** goes back to today.
3. Each card shows the task and its status. Press **View Details** or **Continue** to open it.
4. Open **Task Summary**. The cards show **Total Assigned**, **Yet to Start**, **In Progress** and **Completed**.
5. Each week shows its plan status, **Complete** percentage, **Dealer Visits**, **MD Programs** and each target with **Target**, **Achieved** and **Pending**.
6. Under **Day-wise Dealer Visits**, see how many dealer visits were done and pending each day.

## Common problems

- **"No tasks match this filter".** Widen the dates or set **Status** to **All**.
- **"Detailed view for this task type isn't built yet."** Soil Sample tasks have no detail screen here.
:::

::: section my-reports
title: My reports
routes: /FieldReports
minutes: 1
screenshot: my-reports.png
screenshot-phone: my-reports.phone.jpg

**What this screen is for**
**My Reports** shows how many tasks you closed this month and how fast.

1. Look at the cards: **Completed**, **Avg. Close Time**, **Soil Samples** and **Farmers Added**.
2. The **Task closures** table lists each closed task with **Date**, **Task**, **Type** and **Outcome**.
3. Type in **Search...** to find a task.
4. Use **Copy**, **CSV**, **Excel**, **PDF** or **Print** to save or print the list.

## Common problems

- **"Nothing closed out this month yet".** You have not closed a task this month.
:::

::: section track-playback
title: Replay my tracking route
routes: /TrackPlayback
minutes: 2

**What this screen is for**
**Track Playback** shows the route of your Live Tracking for the last 7 days on a map. It works only in the SPIC ONE app on your phone.

1. Open **Track Playback** from the menu, **View Route** on **My Dashboard**, or **View Tracking Details** on **Mark Attendance**.
2. If tracking is on, the **Tracking Active** card shows the time. Press **Stop Tracking** to end it, or **Directions to live location** to open directions.
3. Under **Other playbacks available**, tap a day to load its route.
4. On the map, press play, drag the slider to move along the route, and choose a speed (**1x**, **2x** or **4x**).

## Common problems

- **"No tracking sessions in the last 7 days".** Start Live Tracking from **My Dashboard** to record a route.
- **"Track Playback can only be used from the SPIC mobile app".** Open SPIC ONE on your phone.
:::

::: section team-dashboard
title: Team dashboard
routes: /MdoDashboard
minutes: 2
screenshot: team-dashboard.png
screenshot-phone: team-dashboard.phone.jpg

**What this screen is for**
**Team Dashboard** gives the MDO an overview of task work across the JMDOs at the same HQ.

1. Look at the cards: **Total Tasks**, **Unassigned**, **Requested by JMDO**, **Approved · Today**, **In Progress** and **Completed**.
2. In **JMDO-wise summary**, see each JMDO with **Active** or **On Leave**, and the **Assigned**, **In Progress** and **Completed** counts.
3. Press **Allocation Requests** to approve weekly plans.
4. Press **Refresh** to load the latest numbers.

> Tip: **Unassigned** counts JMDOs who have not sent any weekly plan. **In Progress** counts JMDOs who are on duty today.

## Common problems

- **"No JMDOs found for your headquarters".** No JMDO has the same HQ as you. Ask the admin to check the HQ on the profiles.
:::

::: section allocation-requests
title: See weekly plans waiting for approval
routes: /AllocationRequests
minutes: 2
screenshot: allocation-requests.png
screenshot-phone: allocation-requests.phone.jpg

**What this screen is for**
**Allocation Requests** lists the weekly plans your JMDOs have sent for approval. Admin also sees approved plans and a **Status** column.

1. Read the blue box at the top. It says how many JMDOs are waiting for your approval.
2. Narrow the list with **Search...**, **All Districts**, **All Sub Districts** and **All JMDOs**.
3. Each row shows **Submitted**, **Dealer Tasks**, **SPCM Target**, **Soil Target**, **MD Programs** and **Pos Liquidation**.
4. Press **Review** to open a plan. For an approved plan, Admin sees **Edit** instead.
5. Use **Copy**, **CSV**, **Excel**, **PDF** or **Print** to save or print the list.

## Common problems

- **"No pending allocation requests".** No JMDO is waiting for you now.
- **"No allocation requests match these filters".** Set the filters back to **All Districts**, **All Sub Districts** and **All JMDOs**.
:::

::: section review-allocation
title: Review and approve a weekly plan
routes: /AllocationRequestReview
minutes: 3
tour: 1 > [data-help=ff-ar-dealers] | 2 > [data-help=ff-ar-add] | 3 > [data-help=ff-ar-targets] | 4 > [data-help=ff-ar-programs] | 5 > [data-help=ff-ar-approve]

**What this screen is for**
Open a plan from **Allocation Requests** with **Review**. You can correct it before approving. The JMDO can start the tasks as soon as you approve.

1. In **Dealer Tasks**, check each dealer. Change the **Date** (it must be inside the planned week) or the **Priority** (**Normal** or **High Priority**). The bin removes a task after you press **Remove**.
2. To add dealers, choose them under **Add dealer task(s)**, pick the **Date** and **Priority**, and press **Add**.
3. Check the **SPCM Target**, **Soil Sample Target** and **PoS Liquidation Targets** (**Urea**, **DAP**, **NPKS**, **Other**). A change is saved when you leave the box.
4. **MD Programs** are shown for checking only. They come from CSR1 and cannot be changed here.
5. Read the summary line at the bottom, press **Approve Allocation**, then **Yes, Approve**.

> Careful: Removing a dealer task cannot be undone.

## Common problems

- **"The date must fall within the JMDO's allocated week"** Choose a date inside the week shown under **Dealer Tasks**.
- **"Already assigned on that date"** That dealer is already planned for that day.
- **"Couldn't load this allocation."** Go back with **Back to all requests** and open the plan again.
:::

::: section task-monitoring
title: Follow every task of my team
routes: /TaskMonitoring
minutes: 2
tour: 1 > [data-help=ff-tm-filters] | 2 > [data-help=ff-tm-apply] | 3 > [data-help=ff-tm-search] | 4 > [data-help=ff-tm-refresh]
screenshot: task-monitoring.png
hotspots: 1 @ 4.7,22.3,90.5,19.1 | 2 @ 4.7,43.8,90.5,5.3 | 3 @ 71,57.1,24.3,5.4 | 4 @ 87.6,4.8,10.1,5.3
screenshot-phone: task-monitoring.phone.jpg
hotspots-phone: 1 @ 7.8,16.7,84.3,39.8 | 2 @ 7.8,57.9,84.3,6.8 | 3 @ 7.8,71.1,66.7,3.2 | 4 @ 3.1,7.9,93.8,3.1

**What this screen is for**
**Task Monitoring** lists every task of your JMDOs: who has it, its type, where, when it is due and its status. It opens on tasks due today.

1. Under **Filters**, choose one or more **JMDO**, **Task Type**, **Status**, **State** or **Region**, and set the **Date Range**.
2. Press **Apply Filters** to show the matching tasks. **Clear All** removes every filter, including the dates.
3. In the **Tasks** table, type in **Search...** to narrow the list. A **Requested** task has an eye icon that opens its weekly plan for approval.
4. Press **Refresh** to load the latest status.

## Common problems

- **"No tasks match these filters".** Press **Clear All** to see tasks of every date.
- **"No tasks yet".** No JMDO at your HQ has sent a weekly plan.
:::

::: section team-tracking
title: See where my team is
routes: /TeamTracking
minutes: 1
screenshot: team-tracking.png
screenshot-phone: team-tracking.phone.jpg

**What this screen is for**
**Team Tracking** shows the Live Tracking status of the JMDOs at your HQ.

1. In the **JMDO tracking status** table, see each JMDO's **Status**: **Live**, **Stale**, **Offline**, **Stopped**, **Ended automatically** or **Never started**.
2. **Last Seen** and **Last Location** show the time and place of the last location received.
3. **Session Started** shows when the JMDO started tracking.
4. Type in **Search...** to find a JMDO, and press **Refresh** for the latest status.

## Common problems

- **A JMDO shows "Never started".** That JMDO has not started Live Tracking in the app.
- **Stale or Offline.** The phone has not sent a location for some time. Call the JMDO.
:::

::: section yearly-targets
title: Set yearly targets for JMDOs
routes: /TargetAchievement
minutes: 2
screenshot: yearly-targets.png
screenshot-phone: yearly-targets.phone.jpg

**What this screen is for**
On **Yearly Target Allocation**, the MDO sets each JMDO's SPCM and Soil Sample target for the financial year (April to March).

1. Under **Allocate target to a JMDO**, choose the **JMDO**. A saved target fills in by itself.
2. Enter **SPCM Target (yearly)** and **Soil Sample Target (yearly)**.
3. Press **Save Target Allocation**.
4. The **Current yearly targets** table shows every JMDO's target. Press the pencil to load a row into the form and change it.

| Field | What to enter |
|---|---|
| **JMDO** | The field officer. Required. |
| **SPCM Target (yearly)** | Whole number, zero or more. Required. |
| **Soil Sample Target (yearly)** | Whole number, zero or more. Required. |

## Common problems

- **"Enter valid, non-negative SPCM and Soil Sample targets."** Use whole numbers, zero or more.
- **The Save button is grey.** Choose a JMDO and fill both targets.
:::

::: section team-reports
title: Team reports
routes: /MdoReports
minutes: 2
screenshot: team-reports.png
screenshot-phone: team-reports.phone.jpg

**What this screen is for**
**Reports** shows how your JMDOs are doing this month: tasks closed, time taken, soil samples and farmers.

1. Under **Report Filters**, choose the **Report Type**, the **JMDO** and **On or after** (a start date).
2. Press **Generate Report**.
3. Look at the cards: **Tasks Closed**, **Avg. Turnaround**, **Soil Samples** and **Farmers Covered**.
4. The **JMDO-wise summary** shows **Tasks Closed**, **On-time %**, **Soil Samples** and **Farmers Added** for each JMDO.
5. The **Detailed Task Report** shows each task with **Resolution**, **Status** and **Hours Taken**.
6. Use **Copy**, **CSV**, **Excel**, **PDF** or **Print** on a table, and **Refresh** to reload.

## Common problems

- **Sales Report shows nothing.** Sales data is not part of the task system yet.
- **Farmer Coverage has no detailed table.** Farmer numbers are shown in the cards and the summary only.
:::

::: section performance-tracker
title: Compare JMDO performance
routes: /PerformanceTracker
minutes: 1
screenshot: performance-tracker.png
screenshot-phone: performance-tracker.phone.jpg

**What this screen is for**
**Performance Tracker** ranks JMDOs from best to least performing. For now it shows sample figures, not live data.

1. Open **Filters** and choose a **JMDO**.
2. Press **Apply Filters**. **Clear All** shows all JMDOs again.
3. Read **Rank**, **SPCM Achv.**, **Soil Achv.** and **Attendance** for each JMDO.

## Common problems

- **"Detailed performance view ... isn't built yet."** The eye icon has no detail page yet.
:::

::: section farmer-data
title: Farmer data
routes: /FarmerData
minutes: 1

**What this screen is for**
**Farmer Data** is a list of farmers in your region with a form to add one. For now this screen is a preview: new entries are not saved on the server.

1. See the farmers with **Contact**, **Location**, **Crop(s)** and **Acres**.
2. Under **Add Farmer Entry**, choose **MO H.Q.**, **Month** and **Programme**.
3. Enter **Farmer Name**, **Contact Number**, **Location**, **Contact Address**, **Acres**, **Crop 1** and **Crop 2**.
4. Press **Save farmer entry**. The pencil on a row copies it into the form.

> Tip: To record a farmer for a soil sample, use **Add Farmer Data** on a task, which opens the sample collection screen.

## Common problems

- **The Save button is grey.** **Farmer Name** is required.
- **My entry is gone after I left the page.** This screen does not save to the server yet.
:::

::: section farmers-hub
title: Farmers hub
routes: /Farmers
minutes: 1
screenshot: farmers-hub.png
screenshot-phone: farmers-hub.phone.jpg

**What this screen is for**
**Farmers** gathers the tools a field officer uses in front of a farmer. A notice says the **Farmer Portal is on its way**.

1. Read the notice about the coming Farmer Portal.
2. Under **Help a farmer now**, tap **Ask SPIC AI** for answers about products, dosage and usage.
3. Tap **Knowledge Community** for questions and answers from farmers and SPIC experts.
4. Tap **Product Information**, **Product Brochures** or **AI Videos** to show product material.

## Common problems

- **Knowledge Community is missing.** Your role does not have access to the community.
:::

::: section rmd-dashboard
title: RMD dashboard
routes: /RMDDashboard
minutes: 1
screenshot: rmd-dashboard.png
screenshot-phone: rmd-dashboard.phone.jpg

**What this screen is for**
The RMD dashboard shows a CSR and budget overview for the region. For now its figures are samples, not live data, and its buttons do not save anything.

1. See **Budget Overview** and **CSR Workflow Summary** at the top.
2. Look at the charts, such as **RM Approval Status** and **Budget Utilization**.
3. See **Quick Actions**, **Recent Programs & Reports**, **Recent Activity** and **System Updates**.

## Common problems

- **The numbers do not match my region.** The figures on this screen are samples for now.
:::
