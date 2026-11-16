---
module: lab
title: SAS Soil Testing Lab
icon: bi-eyedropper
order: 60
---

::: section overview
title: How the soil testing lab works
routes: /Lab

**What this module is for**
The lab portal follows every soil and water sample from the day it reaches the lab until the farmer gets the report. Each person sees only the screens of their own job.

## The journey of one consignment

1. Field staff collect samples from farmers and send them to the lab as a **consignment** by courier. On the way it shows **In Transit**.
2. When the parcel arrives, the lab coordinator presses **Mark received**. The consignment becomes **Batch Pending**.
3. The coordinator puts one or more Batch Pending consignments into a **batch** and assigns it to a lab analyst. The batch becomes **Taken for Analysis**.
4. The analyst opens the batch, enters the test values for each sample and presses **Submit Values**. The system works out the result, the status and the recommendation by itself.
5. When every sample is submitted, the coordinator presses **Complete Batch**. This locks the values and creates one report for each sample.
6. Reports can be downloaded as PDF or Excel in English and Indian languages and given to the farmer. The farmer can also download the report in the app.

## Who does what

- **Lab coordinator**: receives consignments, makes batches, assigns analysts, completes batches, downloads lab reports.
- **Lab analyst**: enters test values for the batches assigned to them and downloads sample reports.
- **Lab admin**: watches all batches on **Lab Tracking**. This screen is read only.

> Lab screens open only when your designation has the lab pages. Even an Admin needs this. Ask your administrator if a lab menu is missing.
:::

::: section coordinator-dashboard
title: Lab coordinator dashboard
routes: /Lab

**What this screen is for**
The **Lab Coordinator Dashboard** gives a quick picture of the lab: how many consignments arrived, how many still wait for a batch and how far the analysis has gone. When you open **Lab**, the app shows this dashboard to coordinators. Analysts are taken to their own dashboard instead.

## Steps

1. Look at the cards at the top: **Total Consignments Received**, **Pending Batch Creation**, **Total Batches Created**, **Taken for Analysis**, **Analysis Completed**, **Completed Batches** and **Reports Generated**.
2. The **Recent Consignments** table shows the last five consignments with their **Status** (**Batch Pending** or **Batch Created**).
3. Tap a consignment number, or the eye icon, to open that consignment.
4. Press **View All** next to **Recent Consignments** to open the full **Consignment Details** list.
5. The **Recently Created Batches** table shows the last five batches, the analyst in **Assigned To**, the **Current Status** and the **Analysis Days**.
6. Press **View All** next to **Recently Created Batches** to open **Analysis Tracking**.

## Common problems

- **I see "No batches yet".** No batch is made yet. Press **Go to consignments** to see the Batch Pending consignments.
- **I see "The dashboard could not be loaded".** Check your internet connection and press **Try again**.
- **I was sent to the main Dashboard.** Your designation does not have the coordinator or analyst lab pages. Ask your administrator.
:::

::: section receive-consignments
title: Receive consignments at the lab
routes: /Lab/Consignments, /Lab/Consignments/{id}

**What this screen is for**
The **Consignment Details** screen lists every consignment sent to the lab. Use it to mark a parcel as received and to check what is inside it.

## Steps

1. Look at the cards at the top: **Total Consignments**, **Pending Batch Creation**, **Batched Consignments**, **Water Samples** and **Soil Samples**.
2. To find a consignment, type the consignment number or AWB number in the search box, or choose a status in **All Statuses**.
3. When a parcel arrives, find its row with status **In Transit**.
4. Tap the receive icon in the **Action** column (on a phone, press **Mark received**).
5. Press **Mark received** again in the question box. The status changes to **Batch Pending** and its samples are ready for a batch.
6. To see full details, tap the consignment number or the eye icon.
7. On the details screen you see the sample counts, State, Region and **Head Quarters**, the **History / Timeline**, the **Collections**, the **Samples** and the **Courier & Package** details.
8. Press **Consignment Photos** to see the photos taken at dispatch. Tap a **Collection No.** to open that sample collection.
9. If the consignment is already in a batch, press **View Batch Details** to open the batch.

## Common problems

- **There is no Mark received button.** Only coordinators can receive, and only consignments that are **In Transit**.
- **I see "No consignments found".** Your search or status filter is too narrow. Press **Clear filters**.
- **Consignment Photos is grey.** No photos were added when the parcel was sent.
:::

::: section create-batch
title: Make a batch from consignments
routes: /Lab/Consignments, /Lab/Consignments/{id}

**What this screen is for**
A batch is a group of samples that one analyst tests together. One batch can hold several consignments. You make it from the **Consignment Details** list.

## Steps

1. Open **Consignment Details**. Choose **Batch Pending** in **All Statuses** to see only the consignments that wait for a batch.
2. Tick the box at the start of each consignment you want in the batch. The top box ticks all Batch Pending rows on the page.
3. Press **Create Batch (n)**. The number shows how many you ticked. On a consignment's own screen, press **Create Batch** instead.
4. Fill the **Create Batch** form (see below) and press **Confirm**.
5. The **Batch Created Successfully** box shows the new batch number. Press **View Batch Details** to open the batch, or **Go To Analysis Tracking** to see all batches.

## Fields explained

- **Batch No**: made by the system after you press Confirm.
- **Batch Created Date**: today by default. A future date is not allowed.
- **Consignment No.**: the ticked consignments are shown as chips. Press the small cross on a chip to remove it, or type in the box to add another Batch Pending consignment.
- **Assign To**: the analyst who will test the samples. If you choose one now, the batch goes straight to **Taken for Analysis**. You can also leave it empty and assign later.
- **Priority**: **Low**, **Medium** (default) or **High**. The analyst sees this priority.
- **Remarks**: any note for the batch.

## Common problems

- **"Pick at least one consignment."** Add at least one consignment before you press Confirm.
- **There are no tick boxes.** Only **Batch Pending** rows can be ticked, and only coordinators see the boxes.
:::

::: section analysis-tracking
title: Track batches under analysis
routes: /Lab/Analysis

**What this screen is for**
The **Analysis Tracking** screen lists every batch so you can follow it from creation to completed analysis and spot batches that are late.

## Steps

1. Look at the cards at the top: **Total Batches**, **Created**, **Taken for Analysis**, **Analysis Completed**, **Completed** and **Delayed Analysis**.
2. Type a batch number, consignment number or analyst name in the search box.
3. Choose a status in **All Statuses**. Choose **Delayed** to see only the late batches.
4. Read the **Analysis Days** column:
- **Pending** (yellow): analysis has not started.
- A green number of days: analysis is going on in time.
- A red number of days: the batch is delayed (normally more than 5 days without completing).
5. Tap the batch number or the eye icon to open **Batch Details**.
6. Use the page numbers at the bottom to see more batches, or change how many rows show on each page.

## Common problems

- **I see "No batches found".** Change the status or search, or press **Clear filters**. If there are no batches at all, press **Go to consignments** and make one.
- **A batch shows Delayed but the analyst finished.** It stays delayed until all samples are submitted and the analysis is completed.
:::

::: section batch-details
title: Assign, follow and complete a batch
routes: /Lab/Analysis/{id}

**What this screen is for**
The **Batch Details** screen shows everything about one batch: its samples, test parameters, documents and history. Coordinators also assign the analyst and complete the batch here.

## Steps

1. The strip at the top shows **Batch No.**, **Consignment No.**, **Total Samples**, **Assigned Date**, **Current Status** and **Analysis Days**.
2. Use the tabs: **Overview** (progress, sample summary, timeline), **Samples**, **Parameters**, **Documents** and **Activity Log**.
3. To give the batch to an analyst, press **Assign** (or **Reassign** to change the analyst). Choose **Assign To** and **Priority**, then press **Save**.
4. If the batch has an analyst but has not started, press **Take for Analysis** and confirm.
5. On the **Samples** tab, tap a sample to see its test values. Press **Export** to download the list.
6. On the **Documents** tab, press **Upload**, choose the **Files**, the **Document Type** and a **Description**, then press **Upload** again. Use the icons to download, view or delete a document.
7. When every sample is submitted, a note says "Every sample is completed". Press **Complete Batch**.
8. Type the **Final Remarks** and press **Complete Batch**. The values are locked and the reports are made.

## Common problems

- **There is no Complete Batch button.** Some samples are not submitted yet. Check the **Samples** tab for **Not Started** or **In Progress**.
- **The buttons are missing.** The batch is already **Completed**, or you do not have coordinator rights.
- **A file was not added.** Files must be PDF, JPG, PNG, WEBP, XLSX or DOCX, up to 20 MB each and 10 at a time.
:::

::: section lab-reports
title: Download lab reports
routes: /Lab/Reports

**What this screen is for**
The **Lab Reports** screen lists the reports made when a batch is completed. From here you can open a report's details and download it as PDF or Excel in the language the farmer reads.

## Steps

1. Look at the cards at the top: **Total Reports**, **Batch Reports**, **Sample Reports**, **Generated Today** and **Downloads Completed**.
2. To narrow the list, press **Filters**. Choose **From** and **To** dates, **State**, **Sample Type** or **Financial Year**, then press **Apply Filters**. Press **Clear All** to remove them.
3. Type a batch, consignment or report number in the search box.
4. Tap the eye icon (on a phone, **View Report**) to open **Lab Report Details**.
5. The details show the **Report Summary**, **Batch Details**, **Consignment Details**, **Analysis Details** and the **Status Timeline**.
6. Press **Download PDF** to get the batch report in English. To get another language, press the small arrow next to it and choose **Tamil**, **Telugu**, **Marathi** or another language in the list.
7. Press **Download Excel** in the same way. You get one Excel file for each sample report.
8. Press **Close** to go back to the list.

## Common problems

- **I see "No reports found".** Reports appear only after a batch is completed. Check your filters too.
- **The download buttons are grey.** The report is not ready yet.
- **The PDF came in English.** That language may not be ready for this report yet, so the English copy is given.
:::

::: section analyst-dashboard
title: Lab analyst dashboard
routes: /Lab/Analyst

**What this screen is for**
The **Lab Analyst Dashboard** shows the work assigned to you and what to do first. When an analyst opens **Lab**, the app shows this screen.

## Steps

1. Look at the cards at the top: **Assigned Batches**, **Pending Value Entry** (samples waiting for values), **Auto Result Ready** (samples whose result is worked out), **Reports Generated** (this financial year) and **Previous FY Reports**.
2. Check **Today's Work Priority**:
- **Pending value entry batches** (High): press **Entry Now** to see the batches waiting for values.
- **Auto result ready for review** (Medium): press **Preview** to check the results.
- **Generated reports for download** (Low): press **View Reports**.
3. Or use **Quick Actions**: **Go to Batch List**, **Continue Test Entry** (opens the batch you worked on last), **View Test Reports** and **View Previous FY Reports**.
4. Press **Refresh** to load the latest numbers.

## Common problems

- **All numbers are zero.** No batch is assigned to you yet. Ask your lab coordinator.
- **I see "Your dashboard could not be loaded".** Check your connection and press **Try again**.
:::

::: section analyst-batch-list
title: Find the batches assigned to me
routes: /Lab/TestEntry

**What this screen is for**
The **Test Value Entry** screen lists the batches assigned to you. Start or continue entering test values from here.

## Steps

1. Look at the cards at the top: **Assigned Batches**, **Pending Value Entry**, **Auto Result Ready** and **Report Generated**.
2. To narrow the list, open **Filters** and choose **Date Range**, **Sample Type**, **Status**, **Priority**, **Financial Year** or **Assigned By**. Press **Apply Filters**. Press **Clear All** to remove them.
3. Type a batch or consignment number in the search box.
4. In the **Assigned Batch List**, check **Pending Entries**, **Completed Entries**, **Priority** and **Status**. Do **High** priority batches first.
5. Tap a row, or the icon in the **Action** column, to open the batch. The icon means:
- **Start entry**: status **Pending Entry**, nothing entered yet.
- **Continue entry**: status **In Progress**, some values saved.
- **Preview results**: status **Auto Result Ready**, all values submitted.
- **View**: status **Report Generated**, the batch is closed.

## Common problems

- **A batch is missing.** It may be assigned to another analyst, or a filter hides it. Press **Clear filters**.
- **I see "Batches could not be loaded".** Check your connection and press **Try again**.
:::

::: section enter-test-values
title: Enter test values for a sample
routes: /Lab/TestEntry/{id}

**What this screen is for**
On **Sample-wise Entry** you type the test values of each sample in a batch. The system fills the normal range, result, status and recommendation by itself.

## Steps

1. Check the **Batch Summary** at the top.
2. In **Samples in this Batch**, tap the icon of the sample to work on, or press **Next pending sample**.
3. Check the **Selected Sample** details (farmer, village, crop).
4. In the **Test Value Entry Table**, type each value in **Test Value**. For **Texture**, choose from the list. Rows marked **Auto-calculated** fill by themselves.
5. Watch **Result**, **Status** and **Recommendation Hint** change as you type. See the **Recommendation Preview** and **Fertilizer Schedule (Kg/acre)** below.
6. Press **Save Draft** to keep unfinished work.
7. When all values are in, press **Submit Values**, then **Submit** in the **Submit Test Values?** box.
8. In **Submitted Successfully**, press **Preview Report** or **Cancel**, then pick the next sample.
9. To correct a submitted sample, press **Edit Values**. This works only until the batch is completed.

## Fields explained

- **Normal Range**: the good range for this test.
- **Status**: **Normal**, **Deficient**, **Moderate** or **Excess**, from your value.
- **Overall Soil Status** (or **Overall Water Status**): **Good**, **Needs Improvement** or **Poor**, from all values.

## Common problems

- **"Enter every parameter before submitting."** Fill the parameters named in the message.
- **"Fix the values that are not numbers."** Use only digits and a dot.
- **"Leave this sample?"** Your values are not saved. Press **Stay**, then **Save Draft**.
- **Everything is locked.** The batch is completed. Press **View Report**.
:::

::: section analyst-test-reports
title: Test reports and previous year reports
routes: /Lab/TestEntry/Reports, /Lab/TestEntry/Reports/Previous

**What this screen is for**
The **Test Reports** screen shows a report summary for each of your batches in this financial year (April to March). **Previous FY Reports** shows the same for earlier years.

## Steps

1. Look at the cards at the top: **Total Batches**, **Total Sample Reports**, **Reports Generated Today** and **Downloads Completed**.
2. To see older reports, press **Previous FY Reports**. To come back, press **Current FY Reports**.
3. To narrow the list, open **Filters** and choose **Financial Year**, **Date Range**, **Sample Type**, **State**, **Region** or **Head Quarters**. Press **Apply Filters**. Press **Clear All** to remove them.
4. Type a batch or report number in the search box.
5. In **Batch-wise Report Summary**, check **Reports Generated** and **Pending Reports** for each batch.
6. Tap a row, or the **View reports** icon, to open **Batch Report Details** with the report of every sample.

## Common problems

- **I see "No batch reports found".** Reports appear only after the coordinator completes the batch. Check the financial year and filters too.
- **I see "Reports are not available".** The report service did not answer. Try again later.
- **An old report is missing.** Open **Previous FY Reports** and choose its year.
:::

::: section batch-report-details
title: Open and download sample reports of a batch
routes: /Lab/TestEntry/Reports/{id}

**What this screen is for**
**Batch Report Details** lists the report of each sample in one batch. Each sample has its own report for its farmer. Here you view, download and mark reports as printed.

## Steps

1. Look at the cards: **Total Samples**, **Soil Samples**, **Water Samples**, **Reports Generated** and **Downloads Completed**.
2. Check **Batch Information**: batch, consignment, State, Region, **Head Quarters**, **Date of Dispatch** and **Financial Year**.
3. To find a sample, use the filters **Sample Type**, **Status**, **Crops** and **Villages**.
4. Press **View** to open a sample report on screen. It shows the results, the recommendations and the fertilizer schedule.
5. Press **Download PDF** to save one report in English. Press the small arrow next to it to choose another language, such as **Tamil**, **Telugu** or **Marathi**.
6. Press **Download All Reports** to save every report of the batch at once. Use its arrow to choose the language.
7. After you print a report for the farmer, press **Mark printed**. Its **Report Status** changes to **Printed**.

## Fields explained

- **Report Status**: **Generated** (ready), **Downloaded** (saved at least once) or **Printed**.
- **Reports No.**: the report number printed on the farmer's report.

## Common problems

- **I see "This batch report is not available".** The batch is not completed yet. Press **Open the batch** to check the samples.
- **Mark printed is grey.** The report is already marked **Printed**.
:::

::: section lab-tracking
title: Lab tracking for admins
routes: /Lab/Tracking

**What this screen is for**
**Lab Tracking** is the admin view of all batches in the lab. Use it to watch consignments, batches and analysis progress. You cannot change anything here.

## Steps

1. Look at the cards at the top: **Total Batches**, **Batch Created**, **Taken for analysis** and **Analysis Completed**.
2. Use the tabs to show one stage: **All Batches**, **Batch Created**, **Taken for Analysis** or **Analysis Completed**. Each tab shows its count.
3. To narrow the list, open **Filters** and choose **Date Range**, **Batch No.**, **Consignment No.**, **Assigned To**, **State**, **Region** or **Current Stage**. Press **Apply Filters**. Press **Clear All** to remove them.
4. Type a batch number, consignment number or analyst name in the search box above the **Lab Tracking List**.
5. Read **Analysis Days**: **Pending** (yellow) means not started, green means in time, red means delayed.
6. Check **Last Updated** to see when the batch last moved.
7. Tap the batch number or the eye icon to open the batch view.

## Common problems

- **I see "No batches found".** Press **Clear filters** and choose the **All Batches** tab.
- **Lab Tracking is not in my menu.** Your designation needs the Lab Tracking page. The Admin role alone is not enough. Ask your administrator.
:::

::: section batch-view
title: View one batch as admin
routes: /Lab/Tracking/{id}

**What this screen is for**
The batch view gives an admin the full story of one batch on one page: where the samples came from, the courier, the batch, the analysis progress and the timeline. It is read only.

## Steps

1. The strip at the top shows **Batch No.**, **Consignment No.**, **Test Samples**, **Samples**, **Assigned To (Analyst)**, **Batch Created On** and **Last Updated**.
2. **Source Details** shows who collected the samples, their **Mobile No.**, **Location** and **Collection Date**.
3. **Consignment Details** shows the **Dispatch Date**, the **AWB / Tracking No** and the **Expected Delivery Date**. Tap the tracking number to follow the parcel on the courier site.
4. **Batch Details** shows the sample counts, **Batch Created By**, **Batch Created Date** and **Assigned Analyst**.
5. **Analysis Process** shows, for each sample type, how many samples are **Completed** and **Not Started**, with a **Completed %** bar.
6. The **Timeline** shows each step: **Consignment Delivered to Lab**, **Lab Entry Created**, **Batch Created**, **Taken for Analysis**, **Analysis Completed** and **Report Generated**, with the date and who did it. Grey steps are not done yet.
7. Press **Export** to download the batch samples as an Excel file.
8. Press **Back to Lab Tracking** to return to the list.

## Common problems

- **I see "The batch could not be loaded".** The batch may be removed, or you do not have Lab Tracking. Press **Try again**.
:::
