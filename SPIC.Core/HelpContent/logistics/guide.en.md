---
module: logistics
title: Logistics
icon: bi-truck
order: 113
category: Facilities
color: #475569
example: DEMO-WH-01 · Demo Warehouse
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Keep one list of the warehouses, rake points and ports that SPIC and GREEN STAR use. Field staff add each place with its location and documents, and the RM, SMM and AVP approve it in turn.

- add | icon: bi-building-add | color: #475569
  title: Field staff add it
  text: MO, MDO or JMDO add a warehouse or rake point with its SAP Code, address, map point and documents.
- approve | icon: bi-shield-check | color: #d97706
  title: Three approvals
  text: RM, then SMM, then AVP check the record. Each can approve it or send it back with remarks.
- report | icon: bi-bar-chart-line | color: #2563eb
  title: See the totals
  text: The report shows, state by state, how many records are pending and how many are completed.
:::

::: section flow
title: How a record moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- admin | type: lane | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  sub: SAP Code list
- maker | type: lane | color: #475569 | roles: MO, MDO, JMDO
  title: MO / MDO / JMDO
  sub: Field staff
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: Your region
- smm | type: lane | color: #c026d3 | roles: SMM, SMD
  title: SMM
  sub: Your state
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin
  title: AVP
  sub: Final approval
- sap | lane: admin | col: 1 | icon: bi-upc-scan | color: #64748b | next: add | screen: sap-master
  title: Load the SAP Codes
  text: An admin uploads the PVT Master and Rake Point Master Excel files and maps each SAP Code to a State.
- add | lane: maker | col: 2 | icon: bi-building-add | color: #475569 | next: rm-check | screen: add-record
  title: Add the record
  text: Choose the SAP Code, fill the details, location and documents, and press **Save**. The record goes to the RM.
- rm-check | lane: rm | col: 3 | icon: bi-hourglass-split | color: #d97706 | next: smm-check | return: add | screen: approve-records
  title: RM checks it
  status: Pending RM
  text: The RM presses **Approve to SMM**, or **Send Back** to return it to the MO, MDO or JMDO.
- smm-check | lane: smm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: final | return: rm-check | screen: approve-records
  title: SMM checks it
  status: Pending SMM
  text: The SMM presses **Approve to AVP**, or **Send Back** to return it to the RM.
- final | lane: avp | col: 5 | icon: bi-patch-check | color: #16a34a | return: smm-check | screen: approve-records
  title: Final approval
  status: Approved
  text: The AVP presses **Final Approve**, or **Send Back** to return it to the SMM. Once approved, the record is complete.
- sentback | lane: maker | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: add | screen: manage-records
  title: Correct and save again
  status: Sent Back to MO
  text: Read the **Remarks**, press the pencil, correct the record and press **Update**. It goes back to the RM.
:::

::: section statuses
title: Every status you can see
kind: statuses

The **Approval Status** column shows where a warehouse or rake point is. The **Status** column shows **Active** or **Inactive**.

- draft | color: #64748b
  label: Draft
  text: Saved by someone who is not an MO, MDO or JMDO. It is not in the approval chain.
- pending-rm | color: #d97706
  label: Pending RM
  text: Waiting for the RM of the region.
- pending-smm | color: #d97706
  label: Pending SMM
  text: The RM approved it. Waiting for the SMM of the state.
- pending-avp | color: #d97706
  label: Pending AVP
  text: The SMM approved it. Waiting for the final approval.
- approved | color: #16a34a
  label: Approved
  text: All three approvals are done.
- back-mo | color: #dc2626
  label: Sent Back to MO
  text: The RM sent it back. The MO, MDO or JMDO who made it must correct it.
- back-rm | color: #dc2626
  label: Sent Back to RM
  text: The SMM sent it back to the RM.
- back-smm | color: #dc2626
  label: Sent Back to SMM
  text: The AVP sent it back to the SMM.
- active | color: #0d9488
  label: Active
  text: The place is in use. Location details and the listed documents are required.
- inactive | color: #475569
  label: Inactive
  text: The place is not in use now. Fewer details are required.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- maker | color: #475569 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - Add warehouses and rake points
  - Upload GST, insurance and licence documents
  - Correct records that were sent back
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - Check records for your region
  - Approve to SMM or send back to the maker
- smm | color: #c026d3 | roles: SMM, SMD
  title: SMM
  - Check records for your state
  - Approve to AVP or send back to the RM
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP, Director
  - Give the final approval or send back to the SMM
  - Add and change ports
- admin | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  - Upload the SAP Code lists and map them to states
  - Give the final approval
  - See the logistics report
:::

::: section faq
title: Problems and answers
kind: faq

- no-sap-code | popular: true
  q: My SAP Code is not in the list
  a: The list shows only codes mapped to the **State** you chose. Ask an admin to upload the code or map it to your state on the **PVT Master Management** screen.
- sent-back | popular: true
  q: My warehouse shows "Sent Back to MO". What do I do?
  a: Read the **Remarks** column. Press the pencil on the row, correct the record and press **Update**. It goes back to the RM as **Pending RM**.
- no-approve
  q: I am an RM but I see no Approve button
  a: The button shows only for records waiting for your level. Choose your level, such as **Pending RM**, in the approval filter. RMs see records of their own region only.
- save-error
  q: Save shows red messages under the fields
  a: An **Active** record needs the full location, the contact number and the required documents. Fill each field marked in red, or set the **Status** to **Inactive** if the place is not in use.
- duplicate
  q: It says the SAP Code already exists
  a: Another warehouse or rake point already uses that code. Search the list for it. Do not add the same place twice.
:::

::: section add-record
title: Add a warehouse or rake point
routes: /Logistics
minutes: 5
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-basic] | 3 > [data-help=lg-location] | 4 > [data-help=lg-docs] | 5 > [data-help=lg-save]
screenshot: add-record.png
hotspots: 1 @ 2.2,13.6,95.5,8.4 | 2 @ 2.2,24.4,95.5,54 | 3 @ 2.2,99.3,95.5,0.7 | 4 @ - | 5 @ -
screenshot-phone: add-record.phone.jpg
hotspots-phone: 1 @ 3.1,10.5,93.8,5.4 | 2 @ 3.1,17.3,93.8,73 | 3 @ 3.1,99.6,93.8,0.4 | 4 @ - | 5 @ 43.8,92.1,52.1,3

**What this screen is for**
The **Logistics Master** screen has a form at the top and the list of saved records below. Use the form to add a warehouse or a rake point.

1. Choose the tab: **Warehouse** or **Rake Point**.
2. In **Basic Information**, tap **SPIC**, **GREEN STAR** or both. Choose the **State**, then the **SAP Code**; the **Name** fills in by itself. Choose **Operated By**. For a warehouse, choose the **Warehouse Category** (**PVT WH**, **C&F WH** or **Both**), the **WH Type**, and fill the **Reservation Quantity** box.
3. In **Primary Location**, press **Use Current Location**, tap the map, or paste the **Google Map URL**. Then fill the address, **Pincode** and contact number.
4. In **Documents**, press each **Choose ... File** button and pick the file. Use the eye to check it. Fields marked * are required for an **Active** record.
5. Press **Save**. When an MO, MDO or JMDO saves, the record goes to the RM as **Pending RM**.

> Tip: Highlight the godown address in the document with a highlighter before you scan and upload it.
> Careful: Press **Cancel** only if you want to clear the form. Nothing is saved.

| Field | What to enter |
|---|---|
| **Status** | **Active** if the place is in use now, else **Inactive**. |
| **Operated By** | **Dealer** or **Others**. |
| **WH Type** | **Field WH** or **Rake Point WH** (PVT WH and Both only). |
| **Approved Reservation Quantity (MT)** | Space reserved for the company, in tonnes. |
| **Latitude & Longitude** | Two numbers with a comma, for example 10.7905, 78.7047. |
| **Pincode** | 6 digits. **State** and **District** may fill in by themselves. |

## Common problems

- **The SAP Code list is empty.** No codes are mapped to the chosen **State** yet. Ask an admin.
- **"Primary Location is required".** An **Active** record needs a map point or Google Map URL and the full address.
- **"... saved, but one or more documents failed to upload".** The record is saved. Press the pencil on it and upload the documents again.
- **State, Region or HQ is grey.** They are set from your login and cannot be changed.
:::

::: section approve-records
title: Approve or send back a record
routes: /Logistics
roles: RM, RMD, SMM, SMD, AVP, Director, Admin, CorporateAdmin
minutes: 2
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-approval-filter] | 3 > [data-help=lg-search] | 4 > [data-help=lg-list]
screenshot: approve-records.png
hotspots: 1 @ 2.2,13.6,95.5,8.4 | 2 @ - | 3 @ - | 4 @ -
screenshot-phone: approve-records.phone.jpg
hotspots-phone: 1 @ 3.1,10.5,93.8,5.4 | 2 @ - | 3 @ - | 4 @ -

**What this screen is for**
Below the form, the list shows the warehouses and rake points of your area. Records that wait for you have approve and **Send Back** buttons in the **Action** column.

1. Choose the **Warehouse** or **Rake Point** tab.
2. In the approval filter, choose your level: **Pending RM**, **Pending SMM** or **Pending AVP**.
3. Type a name or SAP Code in the search box to find the record. Use **View** in the **Docs** column and the PDF button to check it.
4. Press **Approve to SMM**, **Approve to AVP** or **Final Approve** (the button shows your level). Enter **Approval Remarks** and press **Confirm & Proceed**. To return it, press **Send Back**, enter **Send Back Remarks** and press **Confirm Send Back**.

> Tip: Remarks are required for both approve and send back. They show in the **Remarks** column.
> Careful: **Send Back** does not delete the record. It moves it back one level only.

## Common problems

- **No approve button.** The record is not at your level. Check its **Approval Status**.
- **"Approval remarks are required."** Type a remark before you confirm.
- **I cannot see a record.** RMs see their region, SMMs their state and AVPs their zone only.
:::

::: section manage-records
title: Find, correct and download records
routes: /Logistics
minutes: 3
screenshot: manage-records.png
screenshot-phone: manage-records.phone.jpg

**What this screen is for**
The list below the form shows saved records with counts at the top: **Total**, **Active** and **Inactive**.

1. Choose the tab and look at the counts at the top of the list.
2. Search by name, or use the **All Status** and approval filters to narrow the list.
3. Press **View** in the **Docs** column to see, download or open the saved documents. Press **Close** when done.
4. Press the SPIC or GREEN STAR logo in the **PDF** column to open the record as a PDF.
5. Press the pencil to load a record into the form. Change it and press **Update**.
6. Tap **Active** or **Inactive** in the **Status** column to switch it.
7. Press the bin to delete a record, then **Delete** to confirm.
8. Press **Excel** above the list (on a phone, **Export** then **Excel**) to download the list.

> Careful: A deleted record cannot be brought back.

## Common problems

- **"Sent Back to MO".** Read the **Remarks**, correct the record with the pencil and press **Update**. It returns to the RM.
- **The Docs View button is grey.** No documents are saved for that record.
- **The list is empty.** Your filters may be too narrow. Set them back to **All Status** and **All Approval Status**.
:::

::: section ports
title: Add a port
routes: /Logistics
roles: Admin, CorporateAdmin, AVP, Director
minutes: 1
screenshot: ports.png
screenshot-phone: ports.phone.jpg

**What this screen is for**
The **Port** tab keeps the list of ports. Only admins, the AVP and the Director see this tab. Ports have no approval steps.

1. Open the **Port** tab.
2. Enter the **Port Name** and, if you have it, the **Google Map or Lat/Long URL**.
3. Choose the **State** and **District**.
4. Choose **Active** or **Inactive** and press **Save**.

## Common problems

- **"Port name is required".** Enter the name, **State** and **District** before you save.
:::

::: section reports
title: See the logistics report
routes: /LogisticsReport
minutes: 1
screenshot: reports.png
screenshot-phone: reports.phone.jpg

**What this screen is for**
The **Logistics Reports** screen counts warehouses and rake points state by state, with the **TOTAL** row at the bottom.

1. On the **Logistics Master** screen, press **Reports**.
2. Choose **Warehouse Report** or **Rakepoint Report**.
3. Type a state name in **Search by state...** to see one state.
4. Read the columns: **Active**, **Inactive**, **Total**, **As per SAP**, **Pending with MO**, **Pending RM**, **Pending SMM**, **Pending with AVP** and **Completed**.
5. Press **Refresh** to load the latest numbers. Press **Back to Logistics** to return.

> Tip: **As per SAP** is the number of codes in the SAP Code list. **Pending with MO** is the codes not yet added or not yet in approval.

## Common problems

- **"No data available".** Nothing matches your search. Clear the search box or press **Refresh**.
:::

::: section sap-master
title: Upload SAP Codes and map states
routes: /PVTMaster
minutes: 3
screenshot: sap-master.png
screenshot-phone: sap-master.phone.jpg

**What this screen is for**
The **PVT Master Management** screen holds the SAP Code lists that feed the **SAP Code** box on the Logistics form. Admins open it with the **SAP Code** button on the **Logistics Master** screen.

1. Choose **PVT Master** (warehouses) or **Rake Point**.
2. Press **Sample Format** to download the Excel layout and fill it.
3. Press the **Click to upload or drag and drop** area and pick your .xlsx file.
4. Read the **Upload Result**: **Total Records**, **Successfully Inserted** and **Duplicate/Skipped**, with the skipped rows listed.
5. In the list, press **State** on a row, choose the **State** and press **Save State Mapping**.
6. Press **Back** to return to Logistics.

> Careful: Use a .xlsx file of 10 MB or less. Codes that already exist are skipped.

## Common problems

- **"Please select a valid .xlsx Excel file."** Save the file as .xlsx and try again.
- **A code shows "Not Mapped".** Press **State** on that row and choose its state. Until then, field staff cannot pick it after choosing a state.
- **There is no State button.** Only Admin and CorporateAdmin can change the mapping.
:::

::: section old-master
title: The older Logistics Master screen
routes: /LogisticsMaster
minutes: 1

**What this screen is for**
This is an older version of the Logistics form, with the tabs **PVT WH**, **C&F WH** and **Rake Point**. Use the main **Logistics Master** screen in the side menu for new work.

1. Choose a tab: **PVT WH**, **C&F WH** or **Rake Point**.
2. Fill **Basic Information**, **Reservation Quantity**, **Primary Location** and **Documents**, as on the main screen.
3. Press **Save**, or use the pencil and bin in the list to change or delete a record.

## Common problems

- **"Failed to load PVT WH" or "Failed to load C&F WH".** These tabs do not work on this screen. Use the main **Logistics Master** screen.
- **No approval buttons.** Approvals are done on the main **Logistics Master** screen only.
:::
