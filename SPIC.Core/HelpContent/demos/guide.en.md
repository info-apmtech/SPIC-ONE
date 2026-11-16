---
module: demos
title: Demonstrations and treatments
icon: bi-flower1
order: 108
category: Field work
color: #65a30d
example: DEMO-CSR2-0001 · Paddy
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Record a crop demonstration in the farmer's field from start to end. You save the farmer and crop details once, then record each field visit as a treatment with observations and photos, and compare each visit with the one before.

- start | icon: bi-pencil-square | color: #65a30d
  title: Start the demo
  text: Enter the farmer, location and crop details for an approved CSR-2 program.
- treat | icon: bi-camera | color: #d97706
  title: Record each visit
  text: Save the product, dosage, observations and control and treated photos.
- compare | icon: bi-arrow-left-right | color: #16a34a
  title: Compare and close
  text: See the previous treatment while you record the next one. Close the demo when no problem is left.
:::

::: section flow
title: How a demonstration moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- csr | type: lane | color: #0891b2
  title: CSR team
  sub: CSR-1 and CSR-2
- field | type: lane | color: #65a30d | roles: MDO, JMDO
  title: Responsible person
  sub: Field staff named on the CSR-1
- eligible | lane: csr | col: 1 | icon: bi-patch-check | color: #64748b | next: start | screen: demo-list
  title: CSR-2 approved
  status: Not Started
  text: The CSR-1 names you as the **Responsible Person**. When its CSR-2 is finally approved, the demo appears in your **Demo Documentation** list.
- start | lane: field | col: 2 | icon: bi-pencil-square | color: #d97706 | next: treat | screen: start-documentation
  title: Start documentation
  status: In Progress
  text: Enter the farmer, location and crop details and press **Start Documentation**.
- treat | lane: field | col: 3 | icon: bi-camera | color: #64748b | next: close, followup | screen: record-treatment
  title: Record a treatment
  status: Draft
  text: At each field visit, record the treatment, observations and photos. Press **Save Draft** to finish later, or **Complete Treatment**.
- followup | lane: field | col: 4 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: treat | to: treat | screen: compare-treatments
  title: Problem found, visit again
  status: In Progress
  text: If you completed the treatment with **Problem Observed?** set to **Yes**, the demo stays open. Press **Start Next Treatment** for the next visit.
- close | lane: field | col: 5 | icon: bi-clipboard-check | color: #16a34a | screen: demo-details
  title: Demo completed
  status: Completed
  text: When you complete a treatment with **Problem Observed?** set to **No**, the whole demo is completed and can only be viewed.
:::

::: section statuses
title: Every status you can see
kind: statuses

- not-started | color: #64748b
  label: Not Started
  text: The demo is waiting for its first visit. A saved draft of the start form also shows this.
- in-progress | color: #d97706
  label: In Progress
  text: Documentation is started. Treatments are being recorded.
- draft | color: #64748b
  label: Draft
  text: A treatment is saved but not completed. You can still change it.
- completed | color: #16a34a
  label: Completed
  text: For a treatment, it is finished and cannot be changed. For the demo, it is closed and can only be viewed.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the demos where you are the responsible person.

- field | color: #65a30d | roles: MDO, JMDO
  title: Responsible person
  - Start documentation for each approved demo
  - Record every field visit as a treatment
  - Upload control and treated photos
  - Close the demo when no problem is left
- csr | color: #0891b2
  title: CSR team
  - Create the CSR-1 and choose the Responsible Person
  - Approve the CSR-2 so the demo can be documented
:::

::: section faq
title: Problems and answers
kind: faq

- not-listed | popular: true
  q: My demonstration is not in the list
  a: The list shows only programs whose CSR-2 is finally approved and where you are the **Responsible Person** on the CSR-1. Ask the person who created the CSR-1 to check it.
- no-next | popular: true
  q: I do not see Start Next Treatment
  a: It appears only when your last treatment is **Completed** and the demo is not closed. If the last treatment is a **Draft**, open it with **View Details** and complete it first.
- closed-early
  q: The demo closed after I completed a treatment
  a: Completing a treatment with **Problem Observed?** set to **No** completes the whole demo. A completed demo can only be viewed, so check this answer before you press **Complete Treatment**.
- photos
  q: My photos are not saved
  a: Photos are uploaded when you press **Save Draft** or **Complete Treatment**. Use JPG or PNG files of 10 MB or less, up to 20 at a time.
- missing-id
  q: I see "Missing or invalid csr2Id in URL"
  a: The screen was opened without choosing a demo. Open **Demo Documentation** and press the button in the **Action** column of the demo.
:::

::: section demo-list
title: See my demonstrations
routes: /DemoDocumentation
minutes: 2

**What this screen is for**
The **Demo Documentation** screen lists every approved CSR-2 program assigned to you, with its stage and how many treatments are done.

1. Look at the cards at the top: **Total Demonstrations**, **Not Started**, **In Progress** and **Completed**.
2. Under **Filters**, choose **Sanction Number**, **Program Name**, **Product**, **Crop**, **District**, **Stage** or **Status**, then press **Apply Filters**. **Clear All** removes them.
3. Type in the **Search demos...** box to find a demo by sanction number, program, product, crop or place.
4. In the table, read **Current Stage**, **Date of Sowing**, **Treatments**, **Last Treatment** and **Document Status** for each demo.
5. In the **Action** column, press **Start Documentation** for a new demo, **View / Continue** for a demo in progress, or **View** for a completed one.
6. Use **Items Per Page** and the page numbers at the bottom to see more rows.

> Tip: Only your assigned, approved CSR-2 programs are listed.

## Common problems

- **"No eligible demonstration records found".** No approved CSR-2 names you as the responsible person yet, or your filters are too narrow. Press **Clear All**.
- **"Unable to load approved CSR-2 demonstrations".** The app could not reach the server. Check your internet and open the screen again.
:::

::: section start-documentation
title: Start documentation for a demo
routes: /StartDocumentation
minutes: 4
tour: 1 > [data-help=dm-sd-program] | 2 > [data-help=dm-sd-farmer] | 3 > [data-help=dm-sd-crop] | 4 > [data-help=dm-sd-actions]

**What this screen is for**
Use the **Start Documentation** screen on the first visit to save the farmer, location and crop details of the demo. Open it from the list with **Start Documentation**.

1. Check **Program Details (From CSR-2)**: **Program Type**, **Program Name**, **Sanction Number**, **Actual Event Location** and **Focus Products**.
2. Under **Farmer & Location Details**, enter the **Farmer Name**, **Mobile Number**, **Place / Village** and **Farmer Address**. Choose the **Crop** and **Crop Type**, enter the **Pin Code**, then choose the **State** and **District**.
3. Under **Crop Information**, enter the **Date Of Sowing**, choose the **Current Crop Stage**, enter the **Expected Harvest Date** and add **Remarks** if needed.
4. Press **Save Draft** to finish later, or **Start Documentation** to begin. **Start Documentation** opens the **Demo Details** screen.

> Careful: After you press **Start Documentation**, the list opens **Demo Details** instead of this form. Check the details before you start.

## Fields explained

| Field | What to enter |
|---|---|
| **Farmer Name** | The farmer who owns the demo field. Required. |
| **Mobile Number** | The farmer's 10-digit mobile number. Required. |
| **Crop** | Only the crops in this CSR-2's focus products are shown. Required. |
| **Crop Type** and **Current Crop Stage** | Load after you choose the crop. Required. |
| **Pin Code** | 6 digits. Required. |
| **State** and **District** | Choose the State first, then the District. Required. |

## Common problems

- **"Select State, District, Crop, Crop Type and Current Crop Stage."** One of these lists is empty. Choose each one.
- **"Enter farmer name, a 10-digit mobile number and a 6-digit PIN code."** Check the mobile number and PIN code.
- **"This CSR-2 is not finally approved or is outside your permitted scope."** This demo is not assigned to you. Go back to the list.
:::

::: section demo-details
title: See a demo and its treatments
routes: /DemoDetails
minutes: 2

**What this screen is for**
The **Demo Details** screen shows the demo summary, the farmer details and the **Treatment Journey**, the list of every treatment recorded.

1. Open a demo from the list with **View / Continue** or **View**.
2. Check the top card: product, **Sanction Number**, **Crop**, **Date of Sowing:**, **Crop Stage:** and **Expected Harvest:**.
3. Check the farmer card: **Farmer**, **Mobile Number**, **District**, **State**, **Pin Code** and **Location**.
4. If no treatment is recorded yet, press **Start First Treatment**.
5. In the **Treatment Journey**, each treatment shows its status, **Visit Date**, **Plant Stage**, **Dosage** and **Remarks**. **Treatments Done** counts the completed ones.
6. Press **View Details** on a treatment to open it.
7. When the last treatment is **Completed**, press **Start Next Treatment** for the next visit.

## Common problems

- **No Start Next Treatment button.** The last treatment is still a **Draft**, or the demo is **Completed**.
- **"Farmer and crop documentation is not available yet."** The start form was not started. Open the demo from the list and press **Start Documentation**.
- **"CSR-2 not found, not finally approved, or outside your permitted scope."** This demo is not assigned to you.
:::

::: section record-treatment
title: Record a treatment at a field visit
routes: /TreatmentDetails
minutes: 5
tour: 1 > [data-help=dm-td-visit] | 2 > [data-help=dm-td-treatment] | 3 > [data-help=dm-td-observations] | 4 > [data-help=dm-td-images] | 5 > [data-help=dm-td-problem] | 6 > [data-help=dm-td-actions]
screenshot: record-treatment.png
hotspots: 1 @ 3,15.3,94,23.2 | 2 @ 3,39.9,94,23.2 | 3 @ 3,64.5,94,35.5 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: record-treatment.phone.jpg
hotspots-phone: 1 @ 5.1,8.6,89.7,14.5 | 2 @ 5.1,23.8,89.7,15.5 | 3 @ 5.1,40.2,89.7,27.7 | 4 @ 5.1,68.7,89.7,22.1 | 5 @ 5.1,91.6,89.7,8.4 | 6 @ -

**What this screen is for**
Use the treatment screen (for example **Treatment 01**) at each field visit to record what was applied, what you saw and photos of the control and treated plants. Open it with **Start First Treatment**, **Start Next Treatment** or **View Details**.

1. Under **Visit Details**, check the **Visit Date** and **Field Officer / Visibility**, then choose the **Current Plant Stage** and **Plant Condition**.
2. Under **Treatment Applied**, choose the **Product Used**, enter the **Dosage**, choose the **Application Method**, enter the **Treatment Date**, choose the **Stage of Application** and add **Remarks**.
3. Under **Observations**, write the **Physical Parameters** and **Yield Parameters**, tap the **Quick Observations** that fit, and write **Observation Notes**.
4. Under **Control Images** and **Treated Images**, press **Upload Image** and choose the photos. Control photos show plants without the product, treated photos show plants with it.
5. Under **Problem Observed?**, choose **Yes** or **No**. For **Yes**, fill **Problem Category**, **Problem Description**, **Severity**, **Recommendation / Monitoring Action** and **Action Taken**.
6. Press **Save Draft** to finish later, or **Complete Treatment** when the visit is done.

> Careful: **Problem Observed?** starts on **Yes**. If you complete the treatment with **No**, the whole demo is completed and no more treatments can be added.

> Tip: From the second treatment, the **Previous Treatment Reference** box at the top shows the last visit, so you can compare.

## Fields explained

| Field | What to enter |
|---|---|
| **Visit Date** and **Treatment Date** | Today by default. The treatment date cannot be before the visit date. Required to complete. |
| **Current Plant Stage** | The crop stage seen today. Required to complete. |
| **Product Used** | The product applied. Required to complete. |
| **Dosage** | For example 2 ml/L. |
| **Control Images**, **Treated Images** | JPG or PNG, 10 MB or less each, up to 20 at a time. |

## Common problems

- **"Enter Visit Date, Treatment Date, Current Plant Stage and Product Used before completing the treatment."** Fill these four fields, then press **Complete Treatment** again.
- **"Treatment date cannot precede visit date."** Change the **Treatment Date** to the visit date or later.
- **"Only JPG and PNG images are allowed."** or **"Each image must be 10MB or less."** Choose other photos or make them smaller.
- **"This treatment has already been completed."** A completed treatment can only be viewed.
:::

::: section compare-treatments
title: Compare treatments and follow up
routes: /TreatmentDetails, /DemoDetails
minutes: 2
screenshot: compare-treatments.png
screenshot-phone: compare-treatments.phone.jpg

**What this screen is for**
When a problem is found, the demo stays open and you visit the field again. Each new treatment shows the previous one, so you can see whether the crop improved.

1. On **Demo Details**, press **Start Next Treatment**. The next treatment number opens.
2. Read the **Previous Treatment Reference** box: **Visit Date**, **Current Plant Stage**, **Dosage Applied**, **Method**, **Field Observation** and **Yield Parameters**.
3. Press **View Full Details** to open the previous treatment with its photos.
4. Record today's visit and photos the same way, and note what changed in **Observation Notes**.
5. Press **Back to Demo Details** to see all treatments in the **Treatment Journey**.

## Common problems

- **No Previous Treatment Reference box.** It shows only from the second treatment onward.
- **I want to change an old treatment.** A **Completed** treatment cannot be changed. Write the correction in the next treatment.
:::

::: section sample-screens
title: Sample treatment screens
routes: /TreatmentDemoDetails, /Treatment01, /Treatment02
minutes: 1

**What this screen is for**
The **Treatment Demo Details**, **Treatment 01** and **Treatment 02** menu items open sample screens with fixed example values. They show how a demo looks. Nothing you type there is saved.

1. **Treatment Demo Details** shows an example **Demo Details** screen with a **Treatment Journey** of three treatments.
2. **Treatment 01** shows an example of a completed treatment, with **Visit Details**, **Treatment Applied**, **Observations**, photos and **Problem Identification & Recommendation**.
3. **Treatment 02** shows an example second treatment, with the **Previous Treatment Reference - Treatment 01** box at the top.
4. To record real work, open **Demo Documentation** and choose your demo.

> Careful: The buttons on these sample screens do not save anything.

## Common problems

- **My entries disappeared.** These are sample screens. Use **Demo Documentation** to record a real demo.
:::
