---
module: publicity
title: Publicity Material Tracking
icon: bi-megaphone
order: 40
category: Marketing
color: #0b6b3a
example: IND-1042 · Banner 6x3
updated: 2026-11-16
note: New look with the request flow, statuses and guided tours
---

::: section overview
title: What this module does for you
kind: overview

Ask for publicity material (posters, banners and similar items) for your HQ, get it approved, receive it and confirm that it arrived. Every step is recorded, so everyone can see where a request is.

- start | icon: bi-pencil-square | color: #0b6b3a
  title: The RMDO starts it
  text: Raise a request with the material, product and quantity.
- approve | icon: bi-shield-check | color: #d97706
  title: Five approvals
  text: RM, SMD, SMM, ADMIN-MD CS and DGM-MD CS check it in turn.
- close | icon: bi-box-seam | color: #7c3aed
  title: The RMDO closes it
  text: Confirm what arrived, including short or damaged pieces.
:::

::: section flow
title: How a request moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- rmdo | type: lane | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  sub: Your HQ
- approvers | type: lane | color: #0d9488 | roles: RM, SMD, SMM
  title: Approvers
  sub: RM · SMD · SMM
- mdcs | type: lane | color: #ea580c | roles: MDAdmin, MDHOD | designations: ADMIN-MD CS, ADMIN MD CS, DGM-MD CS, DGM MD CS
  title: MD CS
  sub: ADMIN-MD CS · DGM-MD CS
- vendor | type: lane | color: #64748b
  title: Vendor
  sub: Courier
- raise | lane: rmdo | col: 1 | icon: bi-pencil-square | color: #64748b | next: approve | screen: raise-request
  title: Raise and submit
  status: Draft
  text: Fill the request form and press **Submit Request**. Until then it is a draft only you can see.
- approve | lane: approvers | col: 2 | icon: bi-hourglass-split | color: #d97706 | next: final | return: raise | screen: review-approve
  title: Check and approve
  status: Pending Approval
  text: RM, then SMD, then SMM check the request. Each can change it, approve and forward it, return it to the RMDO, or reject it.
- final | lane: mdcs | col: 3 | icon: bi-patch-check | color: #2563eb | next: dispatch | screen: review-approve
  title: Final approval
  status: Final Approved
  text: ADMIN-MD CS approves and forwards it, then DGM-MD CS gives the final approval.
- dispatch | lane: mdcs | col: 4 | icon: bi-truck | color: #7c3aed | next: ship | screen: create-dispatch
  title: Create the dispatch
  status: In Dispatch
  text: ADMIN-MD CS or DGM-MD CS records the dispatch from the vendor with the courier details.
- ship | lane: vendor | col: 5 | icon: bi-box-seam | color: #0d9488 | next: ack | screen: update-delivery
  title: Ship to the HQ
  status: Delivered
  text: The material travels from the vendor. MD CS marks it **In Transit** and then **Delivered**.
- ack | lane: rmdo | col: 6 | icon: bi-clipboard-check | color: #16a34a | screen: acknowledge-delivery
  title: Acknowledge
  status: Completed
  text: The RMDO counts the material and submits the **Acknowledgement**. If everything is right, the request is completed.
- exception | lane: mdcs | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: ack | to: dispatch | screen: resolve-exception
  title: Solve a shortage or damage
  status: Exception Raised
  text: If pieces are short or damaged, MD CS sends the rest from the vendor or moves extra stock from another HQ.
:::

::: section statuses
title: Every status you can see
kind: statuses

- draft | color: #64748b
  label: Draft
  text: Saved but not sent. Only you can see it.
- pending | color: #d97706
  label: Pending Approval
  text: With an approver now.
- pending-final | color: #d97706
  label: Pending Final Approval
  text: With DGM-MD CS for the last approval.
- returned | color: #ea580c
  label: Returned
  text: Sent back to the RMDO. Edit and submit it again.
- final | color: #2563eb
  label: Final Approved
  text: Approved. Waiting for the dispatch.
- dispatch | color: #7c3aed
  label: In Dispatch
  text: The vendor has sent the material.
- delivered | color: #0d9488
  label: Delivered
  text: The material reached the HQ.
- ack-pending | color: #0891b2
  label: Acknowledgement Pending
  text: Waiting for the RMDO to confirm what arrived.
- completed | color: #16a34a
  label: Completed
  text: Everything arrived in good condition.
- exception | color: #dc2626
  label: Exception Raised
  text: Pieces were short or damaged. MD CS is solving it.
- transfer | color: #7c3aed
  label: Transfer Order Received
  text: Your HQ is asked to send extra stock to another HQ.
- rejected | color: #475569
  label: Rejected
  text: Closed by an approver. Raise a new request if needed.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- rmdo | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  - Raise requests
  - Acknowledge delivery
  - Send courier proof for a stock transfer
- approvers | color: #0d9488 | roles: RM, SMD, SMM
  title: RM, SMD, SMM
  - Check requests for your area
  - Approve, return or reject
- admin | color: #ea580c | roles: MDAdmin | designations: ADMIN-MD CS, ADMIN MD CS
  title: ADMIN-MD CS
  - Approve and forward
  - Create dispatches and update delivery
  - Solve shortages and damage
- dgm | color: #7c3aed | roles: MDHOD | designations: DGM-MD CS, DGM MD CS
  title: DGM-MD CS
  - Give the final approval
  - Create dispatches
:::

::: section faq
title: Problems and answers
kind: faq

- no-pencil | popular: true
  q: I submitted my request and now there is no pencil icon
  a: After you submit, only an approver can return the request to you. Once it shows **Returned**, the pencil appears again.
- damaged | popular: true
  q: Some pieces arrived damaged or short. What do I do?
  a: Enter the received and damaged quantities when you acknowledge and submit anyway. An exception goes to ADMIN-MD CS, who sends the missing pieces.
- who-has-it
  q: Who has my request right now?
  a: Open the request. The top shows **Current Stage** and **Pending With**, and the **Approval History** shows every action.
- no-rows
  q: I see "No requests found"
  a: Your filters may be too narrow. Open **Filters** and press **Clear All**.
:::

::: section requester-list
title: See and manage my requests
routes: /PublicityRequest
minutes: 2
tour: 1 > [data-help=pub-cards] | 2 > [data-help=pub-search] | 3 > [data-help=pub-filters] | 4 > [data-help=pub-table] | 5 > [data-help=pub-export] | 6 > [data-help=pub-new]
screenshot: requester-list.png
hotspots: 1 @ 2.2,12.2,95.5,33.6 | 2 @ 61.6,81.1,24.3,5.1 | 3 @ 2.2,48.2,95.5,27.9 | 4 @ 4.2,87.9,91.6,12.1 | 5 @ 86.9,81.2,8.9,4.8 | 6 @ 80.7,1.9,17,4.8
screenshot-phone: requester-list.phone.jpg
hotspots-phone: 1 @ 3.1,9.9,93.8,35.4 | 2 @ 6.9,86.1,59.1,3 | 3 @ 3.1,46.3,93.8,35.1 | 4 @ 6.9,90,86.2,10 | 5 @ 68.5,86.2,24.5,2.8 | 6 @ 17.4,5.4,46.7,2.8

**What this screen is for**
The **My Publicity Material Request** screen shows every request for your HQ, with counts at the top and a table below.

1. Look at the cards at the top, such as **Draft**, **Pending Approval** and **Completed**. Tap a card to show only those requests. Tap it again to show all.
2. Type an indent number, product or material in the search box to find a request quickly.
3. Open **Filters** to choose **Date Range**, **Product**, **Publicity Material** or **Status**, then press **Apply Filters**. **Clear All** removes them.
4. In the **Action** column, the eye opens a request. The pencil edits a **Draft** or **Returned** request. The bin deletes a **Draft**.
5. Press **Export** to download the list as an Excel file.
6. Press **Create New Request** to start a new request.

> Careful: Deleting a draft cannot be undone.

## Common problems

- **I see "No requests found".** Your filters may be too narrow. Press **Clear All**.
- **There is no pencil icon.** After you submit, you cannot edit the request unless an approver returns it.
- **The Export button is grey.** There are no rows in the list to export.
:::

::: section raise-request
title: Raise a publicity request
routes: /PublicityRequest/new, /PublicityRequest/edit/{id}
minutes: 3
tour: 1 > [data-help=pub-material] | 2 > [data-help=pub-qty] | 3 > [data-help=pub-design] | 4 > [data-help=pub-location] | 5 > [data-help=pub-submit]
screenshot: raise-request.png
hotspots: 1 @ 4.2,20.2,21.5,8.5 | 2 @ 50.9,20.2,21.5,8.5 | 3 @ 4.2,30.4,91.6,20.7 | 4 @ 2.2,73.6,95.5,19.1 | 5 @ 2.2,95,95.5,4.8
screenshot-phone: raise-request.phone.jpg
hotspots-phone: 1 @ 6.9,15.5,86.2,4.9 | 2 @ 6.9,27.4,86.2,4.9 | 3 @ 6.9,39.2,86.2,14.7 | 4 @ 3.1,66.1,93.8,23.6 | 5 @ 0,87.5,100,8.4

**What this screen is for**
Use the **Create Publicity Material Request** screen to ask for publicity material for your HQ. You can save it as a draft and send it later.

1. Under **Request Details**, choose the **Type of Publicity Material** and the **Product**.
2. Enter the **Quantity** you need. **Requested By** already shows your name.
3. If you have a design, drop the file in the upload box or tap **or click to browse**. Add **Remarks** if needed.
4. Under **Location**, check the **State**, **Region** and **Headquarter (HQ)**. They are filled from your profile.
5. Press **Save as Draft** to keep it for later, or **Submit Request** and then **Submit** to send it to the RM.

> Tip: You can come back to a draft from the list and finish it later.

## Fields explained

| Field | What to enter |
|---|---|
| **Type of Publicity Material** | The item you need, for example a poster or banner. Required. |
| **Product** | The product the material is for. You can type to search. Required. |
| **Quantity** | Number of pieces needed. Required. |
| **Design file** | One file, PDF, JPG, PNG, DOC or DOCX, up to 10 MB. |
| **State**, **Region**, **Headquarter (HQ)** | Where the material must be delivered. Required. |

## Common problems

- **"Select the state, region and headquarter."** Choose the State first, then the Region, then the HQ.
- **"is larger than 10 MB"** Make the file smaller or take a new photo, then upload again.
- **I cannot edit after submitting.** This is normal. Only a returned request can be edited again.
:::

::: section track-request
title: Track my request
routes: /PublicityRequest/{id}
minutes: 2
screenshot: track-request.png
screenshot-phone: track-request.phone.jpg

**What this screen is for**
The **My Publicity Material Request Details** screen shows everything about one request: where it is now, who approved it, the dispatch and the documents.

1. Open a request from the list with the eye icon.
2. At the top, see **Current Stage**, **Status** and who has the request now (**Pending With**).
3. On **Request Details & History**, see the **Request Timeline** and the **Approval History** table with every action and remark.
4. Open **Tracking & Delivery** to see each dispatch: vendor, transporter, tracking number and delivery status.
5. Open **Acknowledgement** to confirm receipt when the material arrives.
6. Open **Related Documents** to see all uploaded files.
7. Press **Download Report** for a PDF of the request, or **Print** to print it.
8. If the request was **Returned**, press **Edit & Resubmit**, correct it and submit again.

## Common problems

- **"Not dispatched yet" on Tracking & Delivery.** The material is still being approved or the dispatch is not made yet. Check the **Current Stage**.
- **Status shows "Rejected".** The request is closed. Read the reason in **Approval History** and raise a new request if needed.
- **I do not see Edit & Resubmit.** It appears only when the request is returned to you.
:::

::: section acknowledge-delivery
title: Acknowledge received material
routes: /PublicityRequest/{id}
minutes: 3

**What this screen is for**
When the material reaches your HQ, you must confirm how many pieces you received and their condition. This closes the request or reports a problem to ADMIN-MD CS.

1. Open the request and go to **Tracking & Delivery**.
2. In the **Awaiting Acknowledgement** box, press **Acknowledge**. This opens the **Acknowledgement** tab.
3. Check the **Dispatch Summary** (dispatch quantity and date).
4. Enter **Quantity Received**.
5. Choose **In Good Condition** or **In Damaged Condition**. If damaged, enter the **Damaged Quantity**.
6. Choose who checked the material in **Acknowledged By**.
7. Write **Remarks** and upload photos of the material.
8. Check the **Exception Details** box if it appears. It shows any shortage, excess or damage.
9. Press **Save as Draft** to finish later, or **Submit Acknowledgement**, then **Submit**.

## Fields explained

| Field | What to enter |
|---|---|
| **Quantity Received** | Pieces actually counted. Required. |
| **Material Condition** | Good or damaged. Required. |
| **Damaged Quantity** | From 1 up to the received quantity. |
| **Acknowledged By** | The person who received the material. Required. |
| **Photos** | Pictures of the received material, up to 10 MB each. |

## Common problems

- **"Damaged quantity must be between 1 and the received quantity."** Check the numbers again.
- **Shortage or damage found.** Submit anyway. An exception goes to ADMIN-MD CS, who will send the missing pieces.
- **No Acknowledge button.** The material is not dispatched yet, or it is already acknowledged.
:::

::: section transfer-proof
title: Send extra stock to another HQ
routes: /PublicityRequest/{id}/transfer/{dispatchId}, /PublicityRequest/done
minutes: 3

**What this screen is for**
If your HQ received more material than needed, ADMIN-MD CS can ask you to send some of it to another HQ that is short. You send it by courier and upload the courier receipt here.

1. Open **Notifications** and tap the stock transfer order, then press **View Transfer**. You can also open your request, go to **Tracking & Delivery** and press **Create Transfer** on the **Stock Transfer Order** card.
2. Check the **Read Information (Read Only)** part: **Destination HQ** and **Transfer Quantity**.
3. Pack and send the material by courier.
4. Under **Upload Courier / Dispatch Proof**, upload a clear photo of the courier receipt.
5. The app reads the receipt and fills **Courier / Transporter**, **Tracking No. / AWB** and **Dispatch Date**. Check them and correct if needed.
6. Add **Vehicle No** and **Remarks (Optional)** if you have them.
7. Press **Submit Proof to Admin**.
8. The **Dispatch Proof Submitted Successfully!** screen opens. Press **Go to Tracking & Delivery** or **Back to List**.

## Fields explained

| Field | What to enter |
|---|---|
| **Courier / Transporter** | Courier company name. Required. |
| **Tracking No. / AWB** | Number on the courier receipt. Required. |
| **Dispatch Date** | The date you sent it. Required. |

## Common problems

- **"Upload the courier / dispatch proof."** Upload the receipt before submitting.
- **Details were not filled in.** The photo may be blurred or a PDF. Type the details yourself.
- **"Transfer order not found".** The proof was already submitted. To change it, use **Update Proof** on the **Tracking & Delivery** tab.
:::

::: section requester-notifications
title: My publicity notifications
routes: /PublicityRequest/notifications
minutes: 1
tour: 1 > [data-help=pub-notif-tabs] | 2 > [data-help=pub-notif-list] | 3 > [data-help=pub-notif-list] | 4 > [data-help=pub-notif-readall] | 5 > [data-help=pub-notif-back]
screenshot: requester-notifications.png
hotspots: 1 @ 4.2,14.7,91.6,7 | 2 @ 4.2,23.6,91.6,76.4 | 3 @ 4.2,23.6,91.6,76.4 | 4 @ 84.5,1.9,13.3,4.8 | 5 @ 68.7,1.9,15.1,4.8
screenshot-phone: requester-notifications.phone.jpg
hotspots-phone: 1 @ 6.9,11,86.2,3.7 | 2 @ 6.9,15.8,86.2,84.2 | 3 @ 6.9,15.8,86.2,84.2 | 4 @ 46.4,5.4,36.4,2.8 | 5 @ 3.1,5.4,41.3,2.8

**What this screen is for**
The **Notifications** screen lists alerts about your requests: returns, dispatches, exceptions and stock transfer orders. Open it from the list page with **Notifications**.

1. Choose a tab: **All**, **Stock Transfer**, **Exception** or **Dispatch & Approvals**.
2. Unread items have a dot. Tap an item to open its details.
3. For a transfer order, press **View Transfer** to upload the courier proof. Press **View Details** to open the request.
4. Press **Mark As Read** for one item, or **Mark All As Read** for all.
5. Press **Back to List Page** to return.

## Common problems

- **"No notifications".** Nothing new has happened on your requests yet.
- **Mark All As Read is grey.** All items are already read.
:::

::: section reviewer-list
title: See requests sent for my review
routes: /PublicityReview
minutes: 2
screenshot: reviewer-list.png
screenshot-phone: reviewer-list.phone.jpg

**What this screen is for**
Approvers and MD CS staff use this list to see requests from their area. The title is **Submitted Requests** (for RM it is **My Publicity Material Request**).

1. Look at the cards at the top. RM sees **Pending for My Approval**, **Approved by RM**, **Dispatch & Delivery**, **Completed** and **Returned**. Others see cards such as **Total Submitted**, **Pending Review**, **Dispatch Pending**, **Exceptions** and **Completed**.
2. If a red **New Request for Review** box appears, press **View New Requests**. For RM, SMD, SMM and ADMIN-MD CS this opens the consolidation screen.
3. Open **Filters** to choose **Date Range**, **Product**, **Publicity Material**, **State**, **Region**, **HQ**, **Current Stage** or **Status**, then press **Apply Filters**.
4. Rows waiting for you show **Pending** in the **Status** column. Rows you already forwarded show **Approved**.
5. Tap the eye icon to open a request.
6. Press **Export** to download the list as Excel.
7. Press **Consolidation** to see all requests waiting at your level, grouped by region.

## Common problems

- **I do not see State or Region filters.** RM, SMD and SMM see only their own area, so some filters are hidden.
- **The list is empty.** Press **Clear All** in Filters. No request may have reached you yet.
:::

::: section review-approve
title: Review and approve a request
routes: /PublicityReview/{id}
minutes: 2

**What this screen is for**
Open a request to check it and approve it, change it, or reject it. The buttons appear only when the request is waiting for you.

1. Open the request from the list or the consolidation screen.
2. Check the top strip, the **Request Timeline** and the **Approval History**.
3. Type any note in **Additional Remarks (if any)**.
4. To change the material, quantity or location first, press **Edit**.
5. To approve, press **Approve & Forward to** the next level (for example **Approve & Forward to SMD**), then confirm with **Approve & Forward**.
6. To refuse, press **Reject**, write the **Reason for rejection**, and press **Reject** again.

> Careful: Reject closes the request for good. The RMDO must raise a new request.

## Approval order

- RM forwards to SMD, SMD to SMM, SMM to ADMIN - MD CS, ADMIN-MD CS to DGM - MD CS.
- DGM-MD CS gives final approval. There is no **Edit** at this stage.

## Common problems

- **No Approve button, title says "Request Details".** The request is not at your level now. You can only view, **Print** or **Download Report**.
- **"Enter the reason for rejection."** The reason is required.
:::

::: section reviewer-edit
title: Edit a request before forwarding
routes: /PublicityReview/edit/{id}
minutes: 2

**What this screen is for**
The **Edit Publicity Material Request** screen lets the approver whose turn it is correct a request. Saving the change also approves and forwards it.

1. On the request, press **Edit**. On the consolidation screen, tap the pencil icon.
2. Change the **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** or **Remarks**.
3. Add or remove the design file if needed.
4. Check **State**, **Region** and **Headquarter (HQ)**.
5. Press **Save & Forward to** the next level (for example **Save & Forward to SMM**).
6. The change is saved, written in the **Approval History**, and the request moves to the next level.

## Common problems

- **The edit page opens the details page instead.** The request is not waiting for you now.
- **I only wanted to save, not forward.** Saving here always forwards. Use **Back to List** if you do not want to forward yet.
:::

::: section consolidation
title: Approve many requests together
routes: /PublicityReview/consolidation
minutes: 3
screenshot: consolidation.png
screenshot-phone: consolidation.phone.jpg

**What this screen is for**
The consolidation screen shows every request waiting at your level, grouped by region, so you can approve them all in one step. RM sees the region, SMD and SMM see the state, ADMIN-MD CS sees all India. DGM-MD CS does not use this screen.

1. On the list page, press **Consolidation**.
2. Check the cards: **Submission ID**, **Region** or **State**, **Total Region**, **Total Requests** and **Total Requirement**.
3. In the table, tap the arrow in **Action** to open a region and see its HQ requests.
4. Tap the eye icon to view a request, or the pencil icon to edit it.
5. Read the box at the bottom. It shows how many regions have reached you.
6. When all regions have reached you, press **Approve & Submit to** the next level, then confirm with **Approve & Submit**.
7. Press **Export** to download the table, or **View Submitted Request** to go back to the list.

## Common problems

- **Approve & Submit is grey.** Some regions are still pending with the previous level. Wait until all regions reach you.
- **"Nothing waiting for you".** No request is at your level now.
- **Request Qty and Approved Qty differ.** An approver changed the quantity. See **Approval History** on the request.
:::

::: section create-dispatch
title: Create a dispatch
routes: /PublicityReview/{id}/dispatch, /PublicityReview/done
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3

**What this screen is for**
After final approval, ADMIN-MD CS or DGM-MD CS records the dispatch from the vendor on the **Create Dispatch** screen. The same screen is used for a replacement (re-initiation) dispatch.

1. Open a **Final Approved** request and press **Create Dispatch**.
2. Check the **Read Information (Read Only)** part.
3. Under **Feed Logistics & Dispatch Data**, fill **Dispatch Date**, **Dispatch Quantity**, **Vendor** and **Transporter / Courier**.
4. Add **Vehicle No**, **Tracking No.** and **Expected Delivery Date** if known.
5. Choose the **Responsible SMDO / RM / RMDO / MO**.
6. Upload the **Dispatch / Note Approval Document**. Add the transport slip or E-way bill if you have them.
7. Choose or type the **Delivery Address**.
8. Press **Confirm and Create Dispatch**.
9. The **Dispatch Created Successfully!** screen opens. Press **Go to Tracking & Delivery**.

## Fields explained

| Field | What to enter |
|---|---|
| **Dispatch Quantity** | Pieces sent now. For a replacement, it starts at the outstanding quantity. |
| **Vendor** | Pick from the list or type a new name. Required. |
| **Responsible SMDO / RM / RMDO / MO** | The officer who will receive the material. Required. |
| **Dispatch / Note Approval Document** | Required for the first dispatch. |
| **Delivery Address** | Tap a saved address or type one. Required. |

## Common problems

- **"Upload the dispatch / note approval document."** Upload it before confirming.
- **No Create Dispatch button.** The request is not **Final Approved** yet.
:::

::: section update-delivery
title: Update dispatch and delivery status
routes: /PublicityReview/{id}
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 2

**What this screen is for**
After a dispatch is made, MD CS updates its progress so the RMDO and approvers can see where the material is.

1. Open the request and go to **Tracking & Delivery**.
2. Find the dispatch card (**Dispatch 1**, **Dispatch 2** and so on).
3. When the material leaves the vendor, press **Mark In Transit**. Add the **Expected Delivery Date** and **Current Location / Hub**, then press **Save**.
4. When it reaches the HQ, press **Mark Delivered**, add the **Current Location / Hub** if needed, and press **Save**.
5. For a stock transfer, when the source RMDO has uploaded the courier proof, press **Update Dispatch Details**. Check **Transporter / Courier** and **Tracking No. (AWB)**, then press **Save**. The transfer then shows **Transfer Created**.

## Fields explained

| Field | What to enter |
|---|---|
| **Transporter / Courier** and **Tracking No. (AWB)** | Required when confirming a transfer. |
| **Expected Delivery Date** | When the material should arrive. |
| **Current Location / Hub** | Where the material is now, for example a courier hub. |

## Common problems

- **No buttons on the dispatch card.** The dispatch is already acknowledged, or the transfer proof is still pending from the source RMDO ("Courier proof is pending from the source RMDO").
- **The RMDO cannot acknowledge.** The material must be dispatched first.
:::

::: section resolve-exception
title: Solve a shortage or damage exception
routes: /PublicityReview/{id}, /PublicityReview/done
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3

**What this screen is for**
When the RMDO reports fewer pieces or damaged pieces, the request shows **Exception Raised**. You decide how to send the missing quantity.

1. Open the request (the **Exceptions** card on the list helps you find them) and go to **Tracking & Delivery**.
2. In the red **Exception Raised** box, read the **Exception Type**, **Raised By** and **Remarks**.
3. Press **Resolve Exception**. The box shows the outstanding quantity.
4. To send the rest from the vendor, choose **Re-initiate dispatch from the vendor**, press **Continue to Dispatch** and fill the dispatch form for the replacement.
5. To move extra stock from another HQ, choose **Stock transfer from another HQ's excess**, pick the **Source Request (Excess)**, check the **Transfer Quantity**, add **Remarks**, then press **Send Transfer Order**.
6. For a transfer, the **Transfer Order Sent Successfully** screen opens. The source HQ RMDO is told to send the stock and upload courier proof.
7. When the proof arrives, confirm it with **Update Dispatch Details** on the transfer card.

## Common problems

- **The stock transfer option is grey ("no excess available").** No other HQ has extra stock. Use the vendor option.
- **"Select the source request and quantity."** Choose a source and enter a quantity above zero.
- **Excess only.** If the RMDO received extra pieces, the request is completed. The extra stock can later be used for another HQ.
:::

::: section reviewer-notifications
title: Approver and MD CS notifications
routes: /PublicityReview/notifications
minutes: 1

**What this screen is for**
The **Notifications** screen lists alerts about requests in your area: new approvals, dispatches, exceptions and stock transfers.

1. On the list page, press **Notifications**.
2. Choose a tab: **All**, **Stock Transfer**, **Exception** or **Dispatch & Approvals**.
3. Unread items have a dot. Tap an item to read it.
4. Press **View Details** to open the request on the **Tracking & Delivery** tab.
5. Press **Mark As Read** for one item, or **Mark All As Read** for all.
6. Press **Back to List Page** to return.

## Common problems

- **"No notifications".** Nothing new has happened yet.
- **An exception notification.** Open the request and use **Resolve Exception** (ADMIN-MD CS and DGM-MD CS only).
:::
