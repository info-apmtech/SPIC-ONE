---
module: sas-sample-collection
title: Soil Sample Collection
icon: bi-droplet
order: 50
category: Soil testing
color: #0d9488
example: SC-2026-0142 · 3 samples
updated: 2026-11-16
note: New look with the sample journey, statuses and guided tours
---

::: section overview
title: What this module does for you
kind: overview

SPIC tests soil and water from a farmer's land in a district lab. The app follows each sample from the field to the farmer's report, so everyone can see where it is.

- collect | icon: bi-droplet | color: #0d9488
  title: Collect in the field
  text: The MDO or JMDO records the farmer and the samples.
- pay | icon: bi-qr-code | color: #d97706
  title: Pay and approve
  text: A paid sample is paid by UPI or another mode, then approved and checked by Finance.
- report | icon: bi-file-earmark-text | color: #7c3aed
  title: Lab report for the farmer
  text: The lab tests the samples and the farmer downloads the report in Tamil or English.
:::

::: section flow
title: How a sample moves, and who acts
kind: flow

Each row is a role and each box is a step. A free sample skips the payment steps. Tap a step to see what happens there.

- officer | type: lane | color: #0d9488 | roles: MDO, JMDO
  title: Field officer
  sub: MDO · JMDO
- approver | type: lane | color: #ea580c | roles: Admin, CorporateAdmin
  title: Approver
  sub: Payment approval
- finance | type: lane | color: #2563eb
  title: Finance
  sub: Payment verification
- lab | type: lane | color: #7c3aed
  title: Lab
  sub: District lab
- farmer | type: lane | color: #16a34a | roles: Farmer
  title: Farmer
  sub: Sample owner
- collect | lane: officer | col: 1 | icon: bi-droplet | color: #64748b | next: pay | screen: collect-sample
  title: Collect the samples
  status: Draft
  text: Pick the farmer, add each soil or water sample and save. A free sample goes straight to the consignment.
- pay | lane: officer | col: 2 | icon: bi-qr-code | color: #d97706 | next: approve | screen: record-payment
  title: Record the payment
  status: Pending Payment
  text: The farmer pays by scanning the QR code. The officer enters the transaction and uploads the receipt.
- approve | lane: approver | col: 3 | icon: bi-patch-check | color: #ea580c | next: send, verify | return: pay | screen: approve-payments
  title: Approve the payment
  status: Pending Approval
  text: The approver checks the amount and the receipt, then approves and forwards it to Finance, or sends it back.
- send | lane: officer | col: 4 | icon: bi-truck | color: #0d9488 | next: test | screen: send-consignment
  title: Send to the lab
  status: Dispatched
  text: The officer hands the samples to a courier and records the tracking number. This is the consignment.
- verify | lane: finance | col: 4 | icon: bi-bank | color: #2563eb | screen: verify-payments
  title: Verify the money
  status: Finance Verified
  text: Finance confirms that the money reached the account, or marks a mismatch.
- test | lane: lab | col: 5 | icon: bi-eyedropper | color: #7c3aed | next: report
  title: Test the samples
  status: In Progress
  text: The lab receives the parcel, tests each sample and publishes the results.
- report | lane: farmer | col: 6 | icon: bi-file-earmark-arrow-down | color: #16a34a | screen: farmer-my-reports
  title: Download the report
  status: Completed
  text: The farmer sees the report in the app and downloads it in Tamil or English.
:::

::: section statuses
title: Every status you can see
kind: statuses

- draft | color: #64748b
  label: Draft
  text: Saved, not yet submitted.
- pending-payment | color: #d97706
  label: Pending Payment
  text: Waiting for the payment to be recorded.
- pending-approval | color: #ea580c
  label: Pending Approval
  text: Payment recorded, waiting for approval.
- ready | color: #0891b2
  label: Ready consignment
  text: Ready to send to the lab.
- dispatched | color: #0d9488
  label: Dispatched
  text: Handed to the courier.
- in-transit | color: #0d9488
  label: In Transit
  text: On the way to the lab.
- delivered | color: #2563eb
  label: Delivered to Lab
  text: The lab has the parcel.
- in-progress | color: #7c3aed
  label: In Progress
  text: The lab is testing.
- completed | color: #16a34a
  label: Completed
  text: Results are available.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- officer | color: #0d9488 | roles: MDO, JMDO
  title: MDO, JMDO
  - Collect samples
  - Record payments
  - Send consignments to the lab
- approver | color: #ea580c | roles: Admin, CorporateAdmin
  title: Approver
  - Approve or return payments
  - Move consignments on
  - Enter lab results
- finance | color: #2563eb
  title: Finance
  - Verify money received
  - Mark mismatches
- farmer | color: #16a34a | roles: Farmer
  title: Farmer
  - See your samples
  - Download your reports
  - Check your payments
:::

::: section faq
title: Problems and answers
kind: faq

- no-menu | popular: true
  q: I cannot see Sample Collection in the menu
  a: Your designation does not include this page. Ask your administrator. JMDO users open it from the **Soil Sample** task instead.
- payment-rejected | popular: true
  q: My payment was rejected. What now?
  a: The details page shows **Payment rejected** with the reason. Correct it and tap **Record payment** again.
- not-ready
  q: It says "This collection is not ready to send"
  a: The payment is not recorded or approved yet. Free samples can be sent straight away.
- report-language
  q: How do I get the report in Tamil?
  a: Tap the small arrow next to **Download PDF** and choose Tamil or English.
:::

::: section collection-list
title: Find and track your sample collections
routes: /SampleCollection
minutes: 2
tour: 1 > [data-help=sc-tiles] | 2 > [data-help=sc-search] | 3 > [data-help=sc-filters] | 4 > [data-help=sc-table] | 5 > [data-help=sc-excel] | 6 > [data-help=sc-new]
screenshot: collection-list.png
hotspots: 1 @ 2.2,68.5,95.5,15.1 | 2 @ 73.7,88,22.4,4.8 | 3 @ 2.2,30.7,95.5,35.4 | 4 @ 3.8,94.7,92.3,5.3 | 5 @ 62.1,1.9,8.1,4.8 | 6 @ 79.2,1.9,18.5,4.8
screenshot-phone: collection-list.phone.jpg
hotspots-phone: 1 @ 3.1,40.6,93.8,24.4 | 2 @ 6.4,69.4,87.2,3.2 | 3 @ 3.1,36,93.8,3.2 | 4 @ 2,2,8,4 | 5 @ 15.3,4.3,10.2,3 | 6 @ 39.7,4.3,48.8,3

**What this screen is for**
The **Sample Collection** page lists every collection you can see, with its payment and lab status. Use it to find a collection and open its details.

The cards at the top show counts: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level** and **Ready to Submit**.

1. Tap a coloured tile such as **Pending Payment** or **Draft** to show only those collections. Tap it again to show all.
2. To search, type a farmer name or collection ID in **Search collections...**.
3. For more choices, open **Filters**, set **Date Range**, **Location**, **Payment Type**, **Payment Status** or **Status**, then tap **Apply Filters**. Tap **Clear All** to remove them.
4. Tap the eye icon (or the card on a phone) to open the collection.
5. Tap **Excel** or **PDF** to download the list shown on the screen. Tap **Refresh Data** to load the latest status.
6. Tap **New Sample Collection** to start collecting.

## Common problems

- **No sample collections found**: clear the filters, or check the date range.
- **Collections could not be loaded**: check your internet and tap **Try again**.
- The **New Sample Collection** button is missing: your account cannot create collections. Only MDO, JMDO and administrator accounts whose designation allows it can collect samples.
- The download has only some rows: **Excel** and **PDF** save the current page only. Change **Items Per Page** to include more.
:::

::: section collect-sample
title: Collect a new soil or water sample
routes: /SampleCollection/new
for: SampleCollection.Entry
roles: MDO, JMDO, Admin, CorporateAdmin
minutes: 4
tour: 1 > [data-help=sc-paytype] | 2 > [data-help=sc-farmer] | 3 > [data-help=sc-soil] | 4 > [data-help=sc-add] | 5 > [data-help=sc-save]
screenshot: collect-sample.png
hotspots: 1 @ 66,1.9,31.8,5 | 2 @ 4,34.7,29.7,7.8 | 3 @ 35.2,81.7,29.7,8 | 4 @ 78.7,100,17.2,0 | 5 @ 81.8,100,16,0
screenshot-phone: collect-sample.phone.jpg
hotspots-phone: 1 @ 3.1,4.6,93.8,3.2 | 2 @ 6.9,33.7,86.2,8.5 | 3 @ 6.9,100,86.2,0 | 4 @ 6.9,100,86.2,0 | 5 @ 43.8,92.1,52.1,3

**What this screen is for**
Use **New Sample Collection** to record the samples you take from farmers. One collection can hold samples from several farmers.

Open it with **New Sample Collection** on the **Sample Collection** page. JMDO: tap **Start** on the **Soil Sample** card in **Tasks Allocation**, or **Soil Sample Collection** in a task's **Quick actions**.

1. At the top, choose **Free Sample** or **Paid Sample**.
2. In **Farmer Name**, type at least two letters and pick the farmer. If the farmer is new, tap **New Farmer**. For a paid sample, choose **Farmer** or **NGO** under **Paid Sample Category**.
3. Set **Soil Sample** and **Water Sample** to **Yes** or **No**. Pick **Crop 1**, **Crop 2** and add **Remarks** if needed.
4. Tap **Add Another Sample**. The sample appears in **Sample Added (Preview)** with its amount. Repeat for every sample; the pencil changes a row and the bin removes it.
5. Tap **Save Draft** to finish later, or **Save and Continue**. Check the **Sample Summary** and tap **Continue to payment** (paid) or **Continue to consignment** (free).

> Tip: One collection can hold samples from several farmers.

## Fields explained

- **Collection ID**: made by the app when you save.
- **Collected By**: your name, filled in for you.
- **Total Amount**: the charge for all paid samples. A free collection shows **Free**.
- **New Farmer**: needs **Farmer Name** and a 10-digit **Mobile Number**. **Use my location** fills **Latitude** and **Longitude**.

## Common problems

- "Choose a farmer, or add a new one": pick a name from the list, not just typed text.
- "Choose at least one of Soil Sample or Water Sample": set one of them to **Yes**.
- To correct a farmer's address, tap **Edit farmer**, change it, then tap **Save farmer details**.
:::

::: section edit-draft
title: Finish or change a draft collection
routes: /SampleCollection/new, /SampleCollection/view/{id}
for: SampleCollection.Update
roles: MDO, JMDO, Admin, CorporateAdmin
minutes: 2

**What this screen is for**
A collection saved with **Save Draft** stays as a **Draft**. Nothing goes to payment or the lab until you submit it. You can open it again and finish it.

1. Open **Sample Collection** and tap the **Draft** tile.
2. Open the collection with the eye icon.
3. Tap **Edit** at the top of **Sample Collection Details**.
4. Add, change or remove samples, the same way as for a new collection.
5. Tap **Save Draft** to keep it as a draft again, or **Save and Continue**.
6. In the **Sample Summary**, check the samples and tap **Continue to payment** or **Continue to consignment**.

> After you submit, the collection can no longer be edited.

## Common problems

- The **Edit** button is missing: the collection is already submitted, or your designation does not allow changes.
- "This collection has already been submitted": the draft was submitted earlier. Open its details to follow the status.
- "That draft no longer exists": it was removed. Start a new collection.
:::

::: section record-payment
title: Record the payment for a paid sample
routes: /SampleCollection/payment/{id}
roles: MDO, JMDO, Admin, CorporateAdmin
minutes: 3

**What this screen is for**
The **Payments** screen shows how much to collect, a UPI QR code for the farmer to scan, and a form for the payment details and receipt.

1. After **Continue to payment**, the **Payments** screen opens. You can also tap **Record payment** on the collection details.
2. Check **Total Amount to Collect**. Tap **View Sample Summary** to see the samples.
3. Ask the farmer to scan the QR code under **Scan & Pay** with any UPI app. Tap **Download QR Code** to save it.
4. Fill in **Payment Information**.
5. Under **Payment Proof**, tap **Click to Upload or Drag and Drop** and choose the receipt or screenshot.
6. Tap **Save Payment**. The payment now waits for approval.

## Fields explained

- **Transaction ID** (required): the UPI transaction number.
- **Paid By** (required): who paid. Farmer names from the collection are offered.
- **Payment Mode**: **UPI**, **Bank Transfer** or **Cash**.
- **Transaction Date**, **Bank / Gateway**, **Payment Gateway / UTR No.**, **Bank Name**, **Contact Number**, **Email (optional)**: fill in what you have.
- **Remarks**: a note for the approver.
- **Payment Proof**: a PNG or JPG image, up to 5 MB.

## Common problems

- "Please fill in the Transaction ID and Paid By": both are required.
- "Pick a PNG or JPG image of 5 MB or less": take a smaller photo or a screenshot.
- **Save Draft** keeps the typed details on this phone only. Finish with **Save Payment**.
- "Payment already recorded": it is waiting for approval.
- If the payment is rejected, the details page shows **Payment rejected** with the reason. Correct it and tap **Record payment** again.
:::

::: section send-consignment
title: Send samples to the lab (consignment)
routes: /SampleCollection/consignment/{id}, /SampleCollection/submitted/{id}
roles: MDO, JMDO, Admin, CorporateAdmin
minutes: 3

**What this screen is for**
When a collection is free or its payment is approved, you pack the samples and hand them to a courier. **Upload Consignment** records the courier details and photos so the lab knows what is coming.

1. Open the collection and tap **Upload consignment**. For a free sample, **Continue to consignment** opens it directly.
2. Under **Courier Details**, choose the **Courier Service** and type the **Tracking Number (AWB)**.
3. To send other ready collections in the same parcel, open **Include other ready collections** and tick them. **Both**, **Free Sample** and **Paid Sample** at the top change which ones are listed.
4. Under **Upload Photos and Documents**, add a photo in **Shipping Label / Waybill**. On a phone you can tap **Take a photo**.
5. Optionally open **Package and contact details**.
6. Tap **Submit Consignment**.
7. **Consignment Submitted Successfully!** shows the tracking number. Tap the copy icon to copy it, **View Consignment** to see the details, or **Go to Dashboard**.

## Fields explained

- **Tracking Link (Optional)**, **Expected Delivery Date**: help the lab follow the parcel.
- **Package Photo**, **Any other Document**: optional photos, PNG or JPG up to 5 MB each.
- **Packaging Type**, **No. of Packages**, **Package Weight (kg)**, **Contact Person**, **Mobile**, **Email**, **Collection Notes**: optional.

## Common problems

- "Please fill the courier details and add the shipping label photo": all three are required.
- "This collection is not ready to send": the payment is not recorded or approved yet.
- **Save Draft** keeps the typed fields only. Add the photos again later.
- JMDO: submitting the consignment marks today's **Soil Sample** task as completed.
:::

::: section collection-details
title: Read a sample collection's details
routes: /SampleCollection/view/{id}
minutes: 2
screenshot: collection-details.png
screenshot-phone: collection-details.phone.jpg

**What this screen is for**
**Sample Collection Details** shows everything about one collection: payment, courier, lab results and history. Field staff, dealers and managers can follow progress here.

1. Open **Sample Collection** and tap the eye icon on a collection.
2. The top line shows the collection ID, **Paid Sample** or **Free Sample**, and the current status.
3. The strip shows **Consignment ID**, **Transaction ID**, **Courier Partner**, **Samples**, **No. of Tests** and **Payment Status**. Tap the consignment ID to open the consignment.
4. **Collection Information** and **Sample Summary** list who collected the samples and how many tests each needs.
5. **Result Comparison** shows the lab values once they are ready. If there are several samples, tap a sample at the top to see its results.
6. **Consignments Details**, **Amount Details** and **History / Timeline** show the courier, the charges and every step so far.
7. Tap **Export** to print or download.

## Buttons you may see

- **Edit**: the collection is still a draft.
- **Record payment**: the payment is still pending.
- **Upload consignment**: ready to send to the lab.

## Common problems

- "The lab has not published the results yet": the sample is still on its way or being tested.
- **Not dispatched yet** under consignment: the samples have not been sent.
- "Collection not found": it was removed, or you do not have access.
:::

::: section admin-review
title: Approve payments, move consignments and enter results
routes: /SampleCollection/view/{id}, /ConsignmentHistory/view/{id}
roles: Admin, CorporateAdmin
minutes: 3

**What this screen is for**
Administrators can review a payment on the collection details page, move a consignment to its next stage, and type lab results.

### Approve a payment on the collection

1. Open a collection with status **Pending Approval**.
2. In **Payment Approval**, compare **Amount**, **Transaction ID** and the receipt. Tap **View proof** to enlarge it.
3. Tap **Approve payment** and confirm. The collection becomes ready to send.
4. To refuse, tap **Reject**, write the **Reason for rejection**, then tap **Reject payment**.

### Move a consignment on

1. Open **Consignment History** and open a consignment.
2. Under **Update status**, tap the next stage, such as **In Transit**, **Delivered** or **Completed**.

> Moving the consignment on also moves every sample inside it.

### Enter lab results

1. On the collection details, go to **Result Comparison**, choose the sample and tap **Enter results** (or **Edit results**).
2. Fill **Test Parameter**, **Entered Value**, **Unit**, **Normal Range** and **Result**. Tap **Add parameter** for more rows.
3. Tap **Save results**.

## Common problems

- "Add a reason so the collector knows what to correct": a rejection needs a reason.
- **Update status** is missing: the consignment is already **Completed**.
:::

::: section consignment-history
title: Track consignments sent to the lab
routes: /ConsignmentHistory, /ConsignmentHistory/view/{id}
minutes: 2
screenshot: consignment-history.png
screenshot-phone: consignment-history.phone.jpg

**What this screen is for**
**Consignment History** lists every parcel of samples sent to the lab, with courier, tracking number and current status.

1. Open **Consignment History** from the menu.
2. The cards show **Total Consignments**, **Total Consignment Sent**, **Total Samples**, **Delivered** and **Pending**.
3. Type in **Search consignment, AWB, transaction...** to find one.
4. Open **Filters** to choose **From**, **To**, **Status**, **Courier Partner** or **Payment Type**, then tap **Apply Filters**.
5. Tap the tracking number to open the courier's tracking page.
6. Tap the consignment ID or the eye icon to open **Consignment Details**.
7. On the details page, see the timeline, the samples inside and the amount. Tap **Click and View Photos and Files** to see the label and package photos, and **Download All** to save them.
8. Use **Excel** or **PDF** to download, and **Refresh Data** to update.

## Fields explained

- **Current Status**: **Pending Pickup**, **Dispatched**, **In Transit**, **Delivered** or **Completed**.
- **AWB/Tracking No.**: the courier's tracking number.
- **Total samples**: on a phone, tap the samples count to see the list.

## Common problems

- **No consignments found**: try another date range, status or courier, or tap **Clear filters**.
- **No photos yet**: nothing was attached when the consignment was sent.
- JMDO users do not see this page in the menu.
:::

::: section farmer-my-samples
title: See your soil samples (farmer)
routes: /Sas/MySamples, /SampleCollection/view/{id}
roles: Farmer
minutes: 2
screenshot: farmer-my-samples.png
screenshot-phone: farmer-my-samples.phone.jpg

**What this screen is for**
**My Samples** shows every soil or water sample your MO officer collected from you, and where each one is now.

1. Open **My Samples** from the menu.
2. Each card shows the sample number, the consignment, the officer who collected it, the date and the crop.
3. The coloured labels show **Paid** or **Free**, the sample status, and **Report: Ready** or **Report: Not Ready**.
4. To find a sample, type in **Search sample, consignment, crop**, pick dates, or choose a type, status or crop.
5. Tap **View Details** to open the sample.
6. On the details page, see **Sample Details**, **Farmer Details**, **Payment Details** and the **Sample Journey** steps from **Sample Collected by MO** to **Report Ready for Download**.
7. When the report is ready, tap **Download PDF**. Tap the small arrow next to it to choose Tamil or English.

## What the status means

- **Payment Pending**: the payment is not finished yet.
- **Awaiting Dispatch**: waiting to be sent to the lab.
- **In Transit**: on the way to the lab.
- **Under Analysis**: the lab is testing it.
- **Report Ready**: you can download the report.

## Common problems

- **No samples yet**: your officer has not added a sample under your name. Ask your MO officer.
- **Download PDF** is missing: the lab has not finished the report yet.
:::

::: section farmer-my-reports
title: Download your soil test report (farmer)
routes: /Sas/MyReports
roles: Farmer
minutes: 2
screenshot: farmer-my-reports.png
screenshot-phone: farmer-my-reports.phone.jpg

**What this screen is for**
**My Reports** lists the test reports the lab made for your samples. You can download each report in Tamil or English.

1. Open **My Reports** from the menu.
2. The cards show **Total Reports**, **Ready to Download**, **Downloaded** and **Under Process**.
3. In **Reports List**, find your report. Use **Search report, sample, crop**, dates, **All Statuses** or **All Crops** to narrow the list.
4. Tap **Download PDF** to save the report. Tap the small arrow next to it to choose Tamil or English.
5. Tap the eye icon (or **View** on a phone) to read the **Report Details**: test results, recommendations and crop advice.

## Fields explained

- **Reports No.**: the report's number.
- **Sample No.**: the sample it belongs to.
- **Status**: **Report Ready** (not downloaded yet), **Downloaded** or **Printed**.
- **Under Process**: samples still being tested at the lab.

## Common problems

- **No reports yet**: the lab has not finished. The message tells you how many samples are being tested.
- **Reports are not available right now**: check your internet and tap **Try again**.
- The file did not open: check your phone's downloads folder.
:::

::: section farmer-payments
title: Check your sample payments (farmer)
routes: /Sas/Payments, /Sas/Payments/{id}
roles: Farmer
minutes: 2

**What this screen is for**
**Payments** shows the payments made for your paid soil samples and whether SPIC has approved and received them. You only view here; nothing needs to be filled in.

1. Open **Payments** from the menu.
2. The cards show **Total Paid Samples**, **Finance Verified**, **Approval Pending** and **Payment Issues**.
3. Search with **Search sample no., transaction ID...**, or choose dates, a status or a crop.
4. Tap the sample number, the eye icon or **View Details** to open **Payment Details**.
5. See the amount, **Payment Mode**, **Transaction ID**, the receipt under **Payment Proof**, and the **Verification Journey**.

## What the status means

- **Admin Approval Pending**: your payment is waiting for approval.
- **Admin Approved**: approved, waiting for Finance.
- **Finance Verified**: the payment is fully confirmed.
- **Payment Issue**: the payment was returned or did not match. Your MO officer will correct it.

## Common problems

- **No payments found**: you have no paid samples, or your samples were free.
- A **Payment Issue** stays for long: contact your MO officer with the **Transaction ID**.
:::

::: section approve-payments
title: Approve sample payments and forward to Finance
routes: /Sas/Payments, /Sas/Payments/{id}
for: SasPaymentApproval.View
minutes: 3

**What this screen is for**
**Payment Approval** lists the payments field officers recorded for paid samples. You check each receipt and amount, then approve and forward it to Finance, or send it back to the officer.

1. Open **Payment Approval** from the menu.
2. Tap the **Pending Admin Approval** tab.
3. Tap a **Payment Ref No.** or the eye icon to open **Payment Approval Details**.
4. Compare **Expected Amount**, **Paid Amount** and **Difference**, and check the receipt under **Payment Proof Preview**.
5. Tap **Approve & Forward to Finance**.
6. Choose the **Verified Amount** (or **Other amount**), add **Remarks**, tick **I confirm that the payment proof, transaction details and amount are verified.**, then tap **Approve & Forward**.
7. To refuse, tap **Reject Payment**, write the **Reason for rejection** and tap **Reject Payment** again.

## Fields explained

- **Admin Status**: **Pending Review**, **Admin Approved** or **Returned to MO**.
- **Finance Status**: **Not Forwarded**, **Pending**, **Verified** or **Mismatch**.
- **Difference**: red when the paid amount is not the expected amount.

## Common problems

- "Tick the confirmation to continue": tick the box before approving.
- "Enter a verified amount greater than zero": choose or type the amount.
- No action buttons: the payment is already decided.
- Use **Filters** (dates, **State**, **Region**, **Payment Mode**, **Status**) to find older payments.
:::

::: section verify-payments
title: Verify payments received (Finance)
routes: /Sas/Payments, /Sas/Payments/{id}, /Sas/Payments/History
for: SasPaymentVerification.View
minutes: 3

**What this screen is for**
**Payment Verification** lists payments the approver has forwarded to Finance. You confirm that the money reached the account. **Payment History** keeps every payment you have finished.

1. Open **Payment Verification** from the menu.
2. Tap the **Pending Verification** tab.
3. Open a payment with its **Payment Ref No.** or the eye icon. **Payment Proof Details** opens.
4. Check the receipt under **Payment Proof Preview** against your bank statement.
5. If the money arrived, tap **Verify Payment**. Choose the **Verified Amount**, set the **Received Date**, add **Remarks**, tick **I have verified the payment proof, transaction ID, and amount.** and tap **Confirm**.
6. If you cannot find the payment, tap **Mark Mismatch**, write the **Mismatch remarks** and tap **Mark Mismatch** again.
7. Open **Payment History** to see finished payments. Use the **All**, **Verified**, **Failed** and **Mismatch** tabs.

## Fields explained

- **Verified Amount**: the amount actually received. Less than expected is saved as a mismatch.
- **Payment Verified**: the money was confirmed.
- **Amount Mismatch**: less money arrived than expected.
- **Failed**: marked with **Mark Mismatch**; no money was found.

## Common problems

- "Enter the amount received": choose or type the amount.
- "Tick the confirmation to continue": tick the box before confirming.
- No action buttons: the payment is already verified or marked.
:::
