---
module: publicity
title: Publicity Material Tracking
icon: bi-megaphone
order: 40
---

::: section overview
title: How publicity material tracking works
routes: /PublicityRequest, /PublicityReview

**What this module is for**
Use this module to ask for publicity material (posters, banners and similar items) for your HQ, get it approved, receive it and confirm that it arrived. Every step is recorded, so everyone can see where a request is.

## The journey of one request

1. The RMDO fills a request and presses **Submit Request**.
2. The request goes for approval in this order: RM, then SMD, then SMM, then ADMIN-MD CS, then DGM-MD CS (final approval).
3. Any approver (except DGM-MD CS) can change the details before forwarding. An approver can also **Reject** the request, which closes it.
4. After final approval, ADMIN-MD CS (or DGM-MD CS) creates the dispatch from the vendor.
5. The material travels: **Dispatched**, then **In Transit**, then **Delivered**.
6. The RMDO checks the material and submits the **Acknowledgement** with the quantity received and its condition.
7. If everything is correct, the request becomes **Completed**.
8. If some items are short or damaged, an **Exception Raised** status appears. ADMIN-MD CS solves it by sending a new dispatch from the vendor, or by moving extra stock from another HQ.

## Who does what

- **RMDO**: creates requests, acknowledges delivery, sends courier proof when asked to transfer extra stock.
- **RM, SMD, SMM**: check and approve requests for their region or state.
- **ADMIN-MD CS**: approves, creates dispatches, updates delivery, solves exceptions.
- **DGM-MD CS**: gives final approval and may also create dispatches.

> Tap the bell button **Notifications** on the list page to see new approvals, dispatches and transfer orders.
:::

::: section requester-list
title: See and manage my requests
routes: /PublicityRequest

**What this screen is for**
The **My Publicity Material Request** screen shows every request for your HQ, with counts at the top and a table below.

## Steps

1. Look at the cards at the top: **Total Request**, **Draft**, **Pending Approval**, **Final Approved**, **In Dispatch**, **Acknowledgement Pending**, **Completed** and **Exception**.
2. Tap a card to show only the requests in that status. Tap it again to show all.
3. To search with more detail, open **Filters**, choose **Date Range**, **Product**, **Publicity Material** or **Status**, then press **Apply Filters**. Press **Clear All** to remove the filters.
4. Type an indent number, product or material in the search box to find a request quickly.
5. In the **Action** column, tap the eye icon to open a request.
6. For a **Draft** or **Returned** request, tap the pencil icon to change it.
7. For a **Draft** only, tap the bin icon to delete it. This cannot be undone.
8. Press **Export** to download the list as an Excel file.
9. Press **Create New Request** to start a new request.

## Common problems

- **I see "No requests found".** Your filters may be too narrow. Press **Clear All**.
- **There is no pencil icon.** After you submit, you cannot edit the request unless an approver returns it.
- **The Export button is grey.** There are no rows in the list to export.
:::

::: section raise-request
title: Raise a publicity request
routes: /PublicityRequest/new, /PublicityRequest/edit/{id}

**What this screen is for**
Use the **Create Publicity Material Request** screen to ask for publicity material for your HQ. You can save it as a draft and send it later.

## Steps

1. On the list page, press **Create New Request**.
2. Under **Request Details**, choose the **Type of Publicity Material** and the **Product**.
3. Enter the **Quantity** you need.
4. Check **Requested By**. It shows your name already.
5. If you have a design, drop the file in the upload box or tap **or click to browse**.
6. Add **Remarks** if needed.
7. Under **Location**, check the **State**, **Region** and **Headquarter (HQ)**. They are filled from your profile.
8. Press **Save as Draft** to keep it for later, or **Submit Request** to send it to the RM.
9. Press **Submit** in the confirm box.

## Fields explained

- **Type of Publicity Material**: the item you need, for example a poster or banner. Required.
- **Product**: the product the material is for. You can type to search. Required.
- **Quantity**: number of pieces needed. Required.
- **Requested By**: the person asking for the material.
- **Design file**: one file, PDF, JPG, PNG, DOC or DOCX, up to 10 MB.
- **State**, **Region**, **Headquarter (HQ)**: where the material must be delivered. Required.

## Common problems

- **"Select the state, region and headquarter."** Choose the State first, then the Region, then the HQ.
- **"is larger than 10 MB"** Make the file smaller or take a new photo, then upload again.
- **I cannot edit after submitting.** This is normal. Only a returned request can be edited again.
:::

::: section track-request
title: Track my request
routes: /PublicityRequest/{id}

**What this screen is for**
The **My Publicity Material Request Details** screen shows everything about one request: where it is now, who approved it, the dispatch and the documents.

## Steps

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

**What this screen is for**
When the material reaches your HQ, you must confirm how many pieces you received and their condition. This closes the request or reports a problem to ADMIN-MD CS.

## Steps

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

- **Quantity Received**: pieces actually counted. Required.
- **Material Condition**: good or damaged. Required.
- **Damaged Quantity**: from 1 up to the received quantity.
- **Acknowledged By**: the person who received the material. Required.
- **Photos**: pictures of the received material, up to 10 MB each.

## Common problems

- **"Damaged quantity must be between 1 and the received quantity."** Check the numbers again.
- **Shortage or damage found.** Submit anyway. An exception goes to ADMIN-MD CS, who will send the missing pieces.
- **No Acknowledge button.** The material is not dispatched yet, or it is already acknowledged.
:::

::: section transfer-proof
title: Send extra stock to another HQ
routes: /PublicityRequest/{id}/transfer/{dispatchId}, /PublicityRequest/done

**What this screen is for**
If your HQ received more material than needed, ADMIN-MD CS can ask you to send some of it to another HQ that is short. You send it by courier and upload the courier receipt here.

## Steps

1. Open **Notifications** and tap the stock transfer order, then press **View Transfer**. You can also open your request, go to **Tracking & Delivery** and press **Create Transfer** on the **Stock Transfer Order** card.
2. Check the **Read Information (Read Only)** part: **Destination HQ** and **Transfer Quantity**.
3. Pack and send the material by courier.
4. Under **Upload Courier / Dispatch Proof**, upload a clear photo of the courier receipt.
5. The app reads the receipt and fills **Courier / Transporter**, **Tracking No. / AWB** and **Dispatch Date**. Check them and correct if needed.
6. Add **Vehicle No** and **Remarks (Optional)** if you have them.
7. Press **Submit Proof to Admin**.
8. The **Dispatch Proof Submitted Successfully!** screen opens. Press **Go to Tracking & Delivery** or **Back to List**.

## Fields explained

- **Courier / Transporter**: courier company name. Required.
- **Tracking No. / AWB**: number on the courier receipt. Required.
- **Dispatch Date**: date you sent it. Required.

## Common problems

- **"Upload the courier / dispatch proof."** Upload the receipt before submitting.
- **Details were not filled in.** The photo may be blurred or a PDF. Type the details yourself.
- **"Transfer order not found".** The proof was already submitted. To change it, use **Update Proof** on the **Tracking & Delivery** tab.
:::

::: section requester-notifications
title: My publicity notifications
routes: /PublicityRequest/notifications

**What this screen is for**
The **Notifications** screen lists alerts about your requests: returns, dispatches, exceptions and stock transfer orders.

## Steps

1. On the list page, press **Notifications**.
2. Choose a tab: **All**, **Stock Transfer**, **Exception** or **Dispatch & Approvals**.
3. Unread items have a dot. Tap an item to open its details.
4. For a transfer order, see **Transfer Order Details**, **Source Excess Reference** and **Reason for Transfer**.
5. Press **View Transfer** to upload the courier proof, or **View Details** to open the request.
6. Press **Mark As Read** for one item, or **Mark All As Read** for all.
7. Press **Back to List Page** to return.

## Common problems

- **"No notifications".** Nothing new has happened on your requests yet.
- **Mark All As Read is grey.** All items are already read.
:::

::: section reviewer-list
title: See requests sent for my review
routes: /PublicityReview

**What this screen is for**
Approvers and MD CS staff use this list to see requests from their area. The title is **Submitted Requests** (for RM it is **My Publicity Material Request**).

## Steps

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

**What this screen is for**
Open a request to check it and approve it, change it, or reject it. The buttons appear only when the request is waiting for you.

## Steps

1. Open the request from the list or the consolidation screen.
2. Check the top strip, the **Request Timeline** and the **Approval History**.
3. Type any note in **Additional Remarks (if any)**.
4. To change the material, quantity or location first, press **Edit**.
5. To approve, press **Approve & Forward to** the next level (for example **Approve & Forward to SMD**), then confirm with **Approve & Forward**.
6. To refuse, press **Reject**, write the **Reason for rejection**, and press **Reject** again.

## Approval order

- RM forwards to SMD, SMD to SMM, SMM to ADMIN - MD CS, ADMIN-MD CS to DGM - MD CS.
- DGM-MD CS gives final approval. There is no **Edit** at this stage.

## Common problems

- **No Approve button, title says "Request Details".** The request is not at your level now. You can only view, **Print** or **Download Report**.
- **"Enter the reason for rejection."** The reason is required.
- **Reject closes the request for good.** It cannot be undone. The RMDO must raise a new request.
:::

::: section reviewer-edit
title: Edit a request before forwarding
routes: /PublicityReview/edit/{id}

**What this screen is for**
The **Edit Publicity Material Request** screen lets the approver whose turn it is correct a request. Saving the change also approves and forwards it.

## Steps

1. On the request, press **Edit**. On the consolidation screen, tap the pencil icon.
2. Change the **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** or **Remarks**.
3. Add or remove the design file if needed.
4. Check **State**, **Region** and **Headquarter (HQ)**.
5. Press **Save & Forward to** the next level (for example **Save & Forward to SMM**).
6. The change is saved, written in the **Approval History**, and the request moves to the next level.

## Fields explained

- **Quantity**: the approved number of pieces. Required.
- **Type of Publicity Material**, **Product**, **Headquarter (HQ)**: required.

## Common problems

- **The edit page opens the details page instead.** The request is not waiting for you now.
- **I only wanted to save, not forward.** Saving here always forwards. Use **Back to List** if you do not want to forward yet.
:::

::: section consolidation
title: Approve many requests together
routes: /PublicityReview/consolidation

**What this screen is for**
The consolidation screen shows every request waiting at your level, grouped by region, so you can approve them all in one step. RM sees the region, SMD and SMM see the state, ADMIN-MD CS sees all India. DGM-MD CS does not use this screen.

## Steps

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

**What this screen is for**
After final approval, ADMIN-MD CS or DGM-MD CS records the dispatch from the vendor on the **Create Dispatch** screen. The same screen is used for a replacement (re-initiation) dispatch.

## Steps

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

- **Dispatch Quantity**: pieces sent now. For a replacement, it starts at the outstanding quantity.
- **Vendor**: pick from the list or type a new name. Required.
- **Responsible SMDO / RM / RMDO / MO**: the officer who will receive the material. Required.
- **Dispatch / Note Approval Document**: required for the first dispatch.
- **Delivery Address**: tap a saved address or type one. Required.

## Common problems

- **"Upload the dispatch / note approval document."** Upload it before confirming.
- **No Create Dispatch button.** The request is not **Final Approved** yet.
:::

::: section update-delivery
title: Update dispatch and delivery status
routes: /PublicityReview/{id}
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS

**What this screen is for**
After a dispatch is made, MD CS updates its progress so the RMDO and approvers can see where the material is.

## Steps

1. Open the request and go to **Tracking & Delivery**.
2. Find the dispatch card (**Dispatch 1**, **Dispatch 2** and so on).
3. When the material leaves the vendor, press **Mark In Transit**. Add the **Expected Delivery Date** and **Current Location / Hub**, then press **Save**.
4. When it reaches the HQ, press **Mark Delivered**, add the **Current Location / Hub** if needed, and press **Save**.
5. For a stock transfer, when the source RMDO has uploaded the courier proof, press **Update Dispatch Details**. Check **Transporter / Courier** and **Tracking No. (AWB)**, then press **Save**. The transfer then shows **Transfer Created**.

## Fields explained

- **Transporter / Courier** and **Tracking No. (AWB)**: required when confirming a transfer.
- **Expected Delivery Date**: when the material should arrive.
- **Current Location / Hub**: where the material is now, for example a courier hub.

## Common problems

- **No buttons on the dispatch card.** The dispatch is already acknowledged, or the transfer proof is still pending from the source RMDO ("Courier proof is pending from the source RMDO").
- **The RMDO cannot acknowledge.** The material must be dispatched first.
:::

::: section resolve-exception
title: Solve a shortage or damage exception
routes: /PublicityReview/{id}, /PublicityReview/done
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS

**What this screen is for**
When the RMDO reports fewer pieces or damaged pieces, the request shows **Exception Raised**. You decide how to send the missing quantity.

## Steps

1. Open the request (the **Exceptions** card on the list helps you find them) and go to **Tracking & Delivery**.
2. In the red **Exception Raised** box, read the **Exception Type**, **Raised By** and **Remarks**.
3. Press **Resolve Exception**. The box shows the outstanding quantity.
4. Choose one option:
5. **Re-initiate dispatch from the vendor**: press **Continue to Dispatch** and fill the dispatch form for the replacement.
6. **Stock transfer from another HQ's excess**: choose the **Source Request (Excess)**, check the **Transfer Quantity**, add **Remarks**, then press **Send Transfer Order**.
7. For a transfer, the **Transfer Order Sent Successfully** screen opens. The source HQ RMDO is told to send the stock and upload courier proof.
8. When the proof arrives, confirm it with **Update Dispatch Details** on the transfer card.

## Common problems

- **The stock transfer option is grey ("no excess available").** No other HQ has extra stock. Use the vendor option.
- **"Select the source request and quantity."** Choose a source and enter a quantity above zero.
- **Excess only.** If the RMDO received extra pieces, the request is completed. The extra stock can later be used for another HQ.
:::

::: section reviewer-notifications
title: Approver and MD CS notifications
routes: /PublicityReview/notifications

**What this screen is for**
The **Notifications** screen lists alerts about requests in your area: new approvals, dispatches, exceptions and stock transfers.

## Steps

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
