---
module: sas-sample-collection
title: Soil Sample Collection
icon: bi-droplet
order: 50
---

::: section overview
title: How soil sample collection works
routes: /SampleCollection

**What this screen is for**
SPIC tests soil and water from a farmer's land in a district lab. The app follows each sample from the field to the farmer's report. Every collection moves through the same steps.

## Steps

1. **Collect.** The field officer (MDO or JMDO) opens **New Sample Collection**, picks the farmer and adds one or more samples.
2. **Pay.** A **Paid Sample** needs a payment. The officer records the UPI or other payment and uploads a photo of the receipt. A **Free Sample** skips this step.
3. **Approve.** An approver checks the payment and the receipt. Finance then checks that the money was received.
4. **Send to the lab.** The officer sends the samples by courier and enters the tracking number. This is called a consignment.
5. **Test.** The lab receives the samples and tests them.
6. **Report.** The farmer sees the report in the app and can download it in Tamil or English.

## What the status means

- **Draft**: saved, not yet submitted.
- **Pending Payment**: waiting for the payment to be recorded.
- **Pending Approval**: payment recorded, waiting for approval.
- **Ready consignment**: ready to send to the lab.
- **Dispatched**, **In Transit**, **Delivered to Lab**: on the way to the lab.
- **In Progress**: the lab is testing.
- **Completed**: results are available.

## Common problems

- You cannot see **Sample Collection** in the menu: your designation does not include this page. Ask your administrator.
- JMDO users do not see it in the menu. Open it from your **Soil Sample** task instead.
:::

::: section collection-list
title: Find and track your sample collections
routes: /SampleCollection

**What this screen is for**
The **Sample Collection** page lists every collection you can see, with its payment and lab status. Use it to find a collection and open its details.

## Steps

1. Open **Sample Collection** from the menu.
2. The cards at the top show counts: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level** and **Ready to Submit**.
3. Tap a coloured tile such as **Pending Payment** or **Draft** to show only those collections. Tap it again to show all.
4. To search, type a farmer name or collection ID in **Search collections...**.
5. For more choices, open **Filters**, set **Date Range**, **Location**, **Payment Type**, **Payment Status** or **Status**, then tap **Apply Filters**. Tap **Clear All** to remove them.
6. Tap the eye icon (or the card on a phone) to open the collection.
7. Tap **Excel** or **PDF** to download the list shown on the screen.
8. Tap **Refresh Data** to load the latest status.

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

**What this screen is for**
Use **New Sample Collection** to record the samples you take from farmers. One collection can hold samples from several farmers.

## Steps

1. On **Sample Collection**, tap **New Sample Collection**. JMDO: tap **Start** on the **Soil Sample** card in **Tasks Allocation**, or **Soil Sample Collection** in a task's **Quick actions**.
2. At the top, choose **Free Sample** or **Paid Sample**.
3. In **Farmer Name**, type at least two letters and pick the farmer. If the farmer is new, tap **New Farmer**.
4. For a paid sample, choose **Farmer** or **NGO** under **Paid Sample Category**.
5. Set **Soil Sample** and **Water Sample** to **Yes** or **No**.
6. Pick **Crop 1**, **Crop 2** and add **Remarks** if needed.
7. Tap **Add Another Sample**. The sample appears in **Sample Added (Preview)** with its amount.
8. Repeat for every sample. Use the pencil to change a row or the bin to remove it.
9. Tap **Save Draft** to finish later, or **Save and Continue** to see the **Sample Summary**.
10. Check the summary and tap **Continue to payment** (paid) or **Continue to consignment** (free).

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

**What this screen is for**
A collection saved with **Save Draft** stays as a **Draft**. Nothing goes to payment or the lab until you submit it. You can open it again and finish it.

## Steps

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

**What this screen is for**
The **Payments** screen shows how much to collect, a UPI QR code for the farmer to scan, and a form for the payment details and receipt.

## Steps

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

**What this screen is for**
When a collection is free or its payment is approved, you pack the samples and hand them to a courier. **Upload Consignment** records the courier details and photos so the lab knows what is coming.

## Steps

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

**What this screen is for**
**Sample Collection Details** shows everything about one collection: payment, courier, lab results and history. Field staff, dealers and managers can follow progress here.

## Steps

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

**What this screen is for**
Administrators can review a payment on the collection details page, move a consignment to its next stage, and type lab results.

## Steps

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

**What this screen is for**
**Consignment History** lists every parcel of samples sent to the lab, with courier, tracking number and current status.

## Steps

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

**What this screen is for**
**My Samples** shows every soil or water sample your MO officer collected from you, and where each one is now.

## Steps

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

**What this screen is for**
**My Reports** lists the test reports the lab made for your samples. You can download each report in Tamil or English.

## Steps

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

**What this screen is for**
**Payments** shows the payments made for your paid soil samples and whether SPIC has approved and received them. You only view here; nothing needs to be filled in.

## Steps

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

**What this screen is for**
**Payment Approval** lists the payments field officers recorded for paid samples. You check each receipt and amount, then approve and forward it to Finance, or send it back to the officer.

## Steps

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

**What this screen is for**
**Payment Verification** lists payments the approver has forwarded to Finance. You confirm that the money reached the account. **Payment History** keeps every payment you have finished.

## Steps

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
