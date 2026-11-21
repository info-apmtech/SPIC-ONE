---
module: sub-dealers
title: Sub dealers
icon: bi-people
order: 103
category: Dealers
color: #8b5cf6
example: DEMO-SD-001 · Demo Agro Traders
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Keep a correct record of every sub dealer: where the shop is, how to reach it, and its GST, PAN and mFMS details. The same module also keeps the welfare beneficiaries of sub dealers and of dealers' employees.

- import | icon: bi-file-earmark-excel | color: #8b5cf6
  title: Admin uploads the master
  text: Admin imports the approved Excel list of sub dealer codes for each State, Region and HQ.
- maintain | icon: bi-pencil-square | color: #0d9488
  title: MO fills the details
  text: The MO picks each sub dealer of the HQ and fills location, contacts, GST, PAN and mFMS.
- welfare | icon: bi-heart | color: #d97706
  title: Welfare beneficiaries
  text: Sub dealer and employee beneficiaries are approved by SM and then by AVP.
:::

::: section flow
title: How a sub dealer record moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- admin | type: lane | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin and office
  sub: Head office
- field | type: lane | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: Your HQ
- sm | type: lane | color: #d97706 | roles: SMD, SMM
  title: SM
  sub: SMD · SMM
- avp | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: Final approval
- import | lane: admin | col: 1 | icon: bi-file-earmark-excel | color: #8b5cf6 | next: maintain | screen: import-master
  title: Import the master
  text: Admin uploads the approved Excel list. New sub dealers stay hidden from the list until someone saves their details.
- maintain | lane: field | col: 2 | icon: bi-pencil-square | color: #16a34a | next: beneficiary | screen: maintain-sub-dealer
  title: Fill the details
  status: Active
  text: The MO picks the sub dealer, fills the location, contacts, GST, PAN and mFMS, and presses **Submit**. It now shows in the **Sub Dealer Master** list.
- beneficiary | lane: admin | col: 3 | icon: bi-person-plus | color: #d97706 | next: sm-approve | screen: beneficiary-add
  title: Add the beneficiary
  status: Pending
  text: On **Sub Dealer & Employee**, office staff add the welfare beneficiary of a sub dealer or of a dealer's employee.
- sm-approve | lane: sm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: avp-approve | screen: beneficiary-approve
  title: SM approval
  status: Pending
  text: SMD or SMM checks the record and approves or rejects it.
- avp-approve | lane: avp | col: 5 | icon: bi-patch-check | color: #2563eb | screen: beneficiary-approve
  title: AVP approval
  status: Approved
  text: The AVP gives the final approval. The dealer can now choose this person when applying for a welfare scheme.
- rejected | lane: sm | col: 5 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: sm-approve | screen: beneficiary-approve
  title: Rejected
  status: SM Rejected
  text: If SM or AVP rejects the record, it stops there and is not offered to the dealer.
:::

::: section statuses
title: Every status you can see
kind: statuses

- active | color: #16a34a
  label: Active
  text: The sub dealer or beneficiary is in use. An Active sub dealer needs every field marked *.
- inactive | color: #64748b
  label: Inactive
  text: Not in use now. An Inactive sub dealer needs only State, Region, HQ and Firm Name.
- pending | color: #d97706
  label: Pending
  text: The beneficiary is waiting for SM or AVP approval.
- approved | color: #2563eb
  label: Approved
  text: Approved at this level. In the Overall column it means SM and AVP both approved.
- rejected | color: #dc2626
  label: Rejected
  text: Refused at this level, shown in the SM Status or AVP Status column.
- sm-rejected | color: #dc2626
  label: SM Rejected
  text: SM refused the beneficiary. It does not go to AVP.
- avp-rejected | color: #b91c1c
  label: AVP Rejected
  text: SM approved, but AVP refused the beneficiary.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- admin | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - Import the sub dealer master from Excel
  - Fill or correct any sub dealer
  - Add sub dealer and employee beneficiaries
- field | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - Fill and update the sub dealers of your HQ
  - Delete saved details when they are wrong
- rm | color: #64748b | roles: RM, RMD
  title: RM, RMD
  - See the sub dealers of your region
  - Export the list to Excel
- sm | color: #d97706 | roles: SMD, SMM
  title: SMD, SMM
  - See the sub dealers of your state
  - Approve or reject beneficiaries first
- avp | color: #2563eb | roles: AVP, Director
  title: AVP and Director
  - Fill or update sub dealers in your area
  - AVP gives the final approval for beneficiaries
:::

::: section faq
title: Problems and answers
kind: faq

- not-in-dropdown | popular: true
  q: I cannot find a sub dealer in Select Imported Sub Dealer
  a: The form shows only sub dealers that Admin imported for your State, Region and HQ. A new code cannot be created on the form. Ask Admin to add it to the master.
- list-empty | popular: true
  q: The Sub Dealer Master list is empty, but Admin uploaded the sub dealers
  a: The list shows only sub dealers whose details were saved. Press **Maintain Sub Dealer**, pick the sub dealer, fill the details and press **Submit**.
- submit-grey
  q: The Submit button is grey
  a: Pick a sub dealer first and wait until its details load. Also wait while the GST PDF, the location or the pincode is being read.
- edit-grey
  q: The pencil and bin icons on the list are grey
  a: Your role can only view sub dealers. MO, MDO, JMDO, Admin, AVP and Director can change them.
- no-approve
  q: I am an approver but I see no tick or cross on a beneficiary
  a: SM sees them only while **SM Status** is **Pending**. AVP sees them only after SM approved and while **AVP Status** is **Pending**.
:::

::: section sub-dealer-list
title: See the sub dealer list
routes: /SubDealerList
minutes: 2
tour: 1 > [data-help=sdl-cards] | 2 > [data-help=sdl-filters] | 3 > [data-help=sdl-table] | 4 > [data-help=sdl-actions] | 5 > [data-help=sdl-summary]
screenshot: sub-dealer-list.png
hotspots: 1 @ 2.2,21,95.5,10.1 | 2 @ 2.2,33,95.5,11.6 | 3 @ 2.2,46.6,95.5,40.1 | 4 @ 55.3,4.8,42.4,4.5 | 5 @ 2.2,88.8,95.5,11.2
screenshot-phone: sub-dealer-list.phone.jpg
hotspots-phone: 1 @ 3.1,18.2,93.8,9.8 | 2 @ 3.1,29.1,93.8,29.3 | 3 @ 3.1,59.5,93.8,22.9 | 4 @ 3.1,5.9,93.8,6.6 | 5 @ 3.1,83.7,93.8,16.3

**What this screen is for**
The **Sub Dealer Master** screen lists the sub dealers whose details were saved through the sub dealer form. MO sees the HQ, RM the region and SMM or SMD the state. A note at the top says which area you see.

1. Look at the cards at the top: **Total Sub Dealers**, **Active** and **Inactive**.
2. Type a code, name, mobile, GST, PAN or mFMS ID in **Search**. Choose **State**, **Region**, **HQ** or **Status** to narrow the list. Your own area is already chosen and locked.
3. In the **Action** column, the eye opens a sub dealer to view. The pencil opens it to edit. The bin deletes its saved details.
4. Use the buttons at the top. **Refresh** reloads the list. **Export All Saved** downloads all saved sub dealers as Excel. **Maintain Sub Dealer** opens the form.
5. Scroll down to **State-wise Sub Dealer Summary**. It compares each state's master records with its saved **Active** and **Inactive** sub dealers.

> Careful: The bin removes only the saved details, not the sub dealer. It leaves this list until someone fills the details again.

## Common problems

- **"No saved Sub Dealers found".** Only sub dealers with saved details show here. Rows from the Admin upload that nobody filled are hidden.
- **The pencil and bin are grey.** Your role can only view. MO, MDO, JMDO, Admin, AVP and Director can change sub dealers.
- **There is no Maintain Sub Dealer button.** Your role can only view the list.
- **"No saved Sub Dealer records are available to export."** Nothing is saved in your area yet.
:::

::: section maintain-sub-dealer
title: Fill or update a sub dealer's details
routes: /SubDealerRegistration
minutes: 5
tour: 1 > [data-help=sdr-business] | 2 > [data-help=sdr-basic] | 3 > [data-help=sdr-location] | 4 > [data-help=sdr-tax] | 5 > [data-help=sdr-mfms] | 6 > [data-help=sdr-submit]
screenshot: maintain-sub-dealer.png
hotspots: 1 @ 2.2,34.4,95.5,32.4 | 2 @ 2.2,11.4,95.5,20.1 | 3 @ 2.2,69.6,95.5,30.4 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: maintain-sub-dealer.phone.jpg
hotspots-phone: 1 @ 3.1,40,93.8,24.8 | 2 @ 3.1,9.5,93.8,29.4 | 3 @ 3.1,65.9,93.8,34.1 | 4 @ - | 5 @ - | 6 @ 4.1,92.1,91.8,3

**What this screen is for**
Use the **Sub Dealer Maintenance** screen to fill the details of a sub dealer that Admin already imported. You cannot create a new sub dealer code here. Open it with **Maintain Sub Dealer** on the list, or with the pencil on a row.

1. Under **Business Information**, tap **Select Imported Sub Dealer**, type the code or name and pick the sub dealer. Its saved details load. Check the **Firm Name** (as per GST trade name).
2. Under **Basic Information**, check **State**, **Region** and **HQ**, and choose the **Status**: **Active** or **Inactive**.
3. Under **Primary Location**, press **Use Current Location**, tap the map, or paste a Google Maps link. Fill the address. Typing the 6-digit **Pincode** fills the State, District, Block, Taluk and Village. Enter the **Official Contact Number**, **WhatsApp Number** and **Alternative Number**.
4. Under **Tax & Compliance**, tap **Upload GST Certificate (PDF)**. The app reads the PDF and fills **GST Number**, **Legal Name**, **Trade Name** and **Constitution of Business**. Check them, then enter the **PAN Number**.
5. Under **mFMS Information**, enter the **Retail mFMS ID**. The **Wholesale mFMS ID** is optional.
6. Press **Submit**. A green message says the sub dealer was saved, and the form clears for the next one.

> Tip: An **Active** sub dealer needs every field marked *. An **Inactive** one needs only State, Region, HQ and Firm Name.
> Careful: When you open a sub dealer with the eye icon, every field is grey and there is no **Submit** button. Use the pencil to change it.

## Fields explained

| Field | What to enter |
|---|---|
| **Select Imported Sub Dealer** | The sub dealer from the Admin upload. Required. |
| **Status** | **Active** if the sub dealer is working now, **Inactive** if not. |
| **Google Map URL** and **Latitude & Longitude** | The shop location. Required for Active. |
| **Pincode** | 6 digits. It fills the place names for you. |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10-digit mobile numbers. Required for Active. |
| **GST Number** | 15 characters, for example 22AAAAA0000A1Z5. Required for Active. |
| **Upload GST Certificate (PDF)** | One PDF file, up to 5 MB. Required for Active. |
| **PAN Number** | 10 characters, for example ABCDE1234F. Required for Active. |
| **Retail mFMS ID** | Required for Active. |

## Common problems

- **"Please correct the highlighted fields."** Read the red text under each field and fix it.
- **The Submit button is grey.** Pick a sub dealer first and wait for it to load. Also wait while the GST PDF, location or pincode is being read.
- **"Shortened URL detected."** Open the link in a browser, copy the full address from the address bar and paste it.
- **"Choose a non-empty GST PDF of 5 MB or smaller."** Upload a PDF file of 5 MB or less.
- **"GST could not be extracted."** Type the GST details yourself and check them.
- **"The selected Sub Dealer does not have a valid Sub Dealer Code. Contact Admin."** The master record is wrong. Ask Admin to correct it.
:::

::: section import-master
title: Import the sub dealer master
routes: /SubDealerList
roles: Admin, SuperAdmin, CorporateAdmin
minutes: 3
screenshot: import-master.png
screenshot-phone: import-master.phone.jpg

**What this screen is for**
Admin uploads the approved Excel list of sub dealers. The import first shows a check of every row and saves nothing until you press **Proceed Import**.

1. On **Sub Dealer Master**, press **Import Sub Dealers**.
2. Press **Download Sample Format**. Fill the columns **Sub Dealer Code**, **Sub Dealer Name**, **HQ**, **Region** and **State**. Use the same State, Region and HQ names as in the app.
3. Under **Choose Excel file**, pick the .xlsx file.
4. Press **Check Import**. The **Import summary** shows **New**, **Already same** and **Rejected** rows. Nothing is saved yet.
5. Read the **Reason** for each rejected row. Fix the Excel and check it again if needed.
6. Press **Proceed Import** to save the new rows.
7. The new sub dealers can now be picked on the form. They show in the list only after their details are saved.

> Careful: An import cannot change the firm, State, Region or HQ of a sub dealer that already exists.
> Tip: SuperAdmin also sees **Correct Sub Dealer Codes** and **Repair Missing Codes** to fix wrong or blank codes. Both show a preview before anything is saved.

## Common problems

- **"Warning: Existing Sub Dealer master conflict detected!"** The file gives a different firm, State, Region or HQ for a code that already exists. Contact Admin to change it.
- **Some rows have empty or invisible Sub Dealer Codes.** Those rows were rejected and not saved. Check the Excel row numbers shown.
- **Check Import is grey.** Choose an .xlsx file first. The file can be up to 10 MB.
:::

::: section beneficiary-list
title: See sub dealer and employee beneficiaries
routes: /SubDealerEmployeeMaster
minutes: 2
screenshot: beneficiary-list.png
screenshot-phone: beneficiary-list.phone.jpg

**What this screen is for**
The **Sub Dealer & Employee** screen, in the **SDWA** menu, keeps the welfare beneficiaries of sub dealers and of dealers' employees. Each record needs SM approval and then AVP approval.

1. Choose a tab: **Sub Dealer** or **Employee**.
2. Look at the cards: the total, **Active** and **Inactive**.
3. Type in the search box. Choose **All Status**, **Active** or **Inactive**, and **All Approval**, **Pending SM**, **Pending AVP**, **Approved** or **Rejected**.
4. In the table, read **SM Status**, **AVP Status** and **Overall** for each record.
5. If your role may change records, tap **Active** or **Inactive** in the **Status** column to switch it.

## Common problems

- **"You do not have permission to view this data."** Ask your admin for access to Sub Dealer & Employee.
- **There is no form at the top.** Your designation can only view records.
- **A record shows Pending for a long time.** It waits for SM, then for AVP. Use the **Pending SM** or **Pending AVP** filter to see where it is.
:::

::: section beneficiary-add
title: Add or change a beneficiary
routes: /SubDealerEmployeeMaster
for: SubDealerEmployeeMaster.Entry
minutes: 3
screenshot: beneficiary-add.png
screenshot-phone: beneficiary-add.phone.jpg

**What this screen is for**
Add the welfare beneficiary of a sub dealer or of a dealer's employee, one at a time or from an Excel file. Every new record starts as **Pending** for SM approval.

1. Choose the **Sub Dealer** or **Employee** tab. The form at the top says **Add New Sub Dealer** or **Add New Employee**.
2. For a sub dealer, fill **Main Dealer Code**, **Main Dealer / Firm Name**, **Sub Dealer Code**, **Sub Dealer Name** and **Beneficiary Name**. For an employee, fill **Dealer Code**, **Employee Name** and **Beneficiary Name**.
3. Add the other details you have, such as **Beneficiary ID**, **Nominee Name**, **Date of Birth** and **Relationship**.
4. Leave the switch on **Active** and press **Save**.
5. To add many at once, press **Download Template**, fill it, then press **Upload File** and choose the Excel file.
6. To change a record, tap the pencil in the **Action** column, correct it and press **Save**. The bin deletes a record.

> Careful: Deleting a record cannot be undone. Changing a record does not reset its SM and AVP approval.

## Common problems

- **"Main Dealer Code is required."** Fill every field marked *. The message names the missing field.
- **The save fails with "already exists".** A beneficiary with the same Dealer Code, Sub Dealer Code, Beneficiary Name and DOB is already in the list. Search for it.
- **"Upload completed with ... skipped row(s)".** Read the skipped rows shown below the message. Press **Copy** to copy them, fix the file and upload those rows again.
:::

::: section beneficiary-approve
title: Approve or reject a beneficiary
routes: /SubDealerEmployeeMaster
roles: SMD, SMM, AVP
minutes: 2
screenshot: beneficiary-approve.png
screenshot-phone: beneficiary-approve.phone.jpg

**What this screen is for**
SMD or SMM approves each beneficiary first. The AVP gives the final approval. Only approved, active beneficiaries can be chosen by the dealer in a welfare scheme application.

1. Choose the **Sub Dealer** or **Employee** tab.
2. Set the approval filter to **Pending SM** (SMD, SMM) or **Pending AVP** (AVP).
3. Check the details of the record: dealer code, names, date of birth and relationship.
4. In the **Action** column, press the green tick to approve or the red cross to reject. On a phone, press **Approve** or **Reject**.
5. Type **Remarks (optional)** and press **Approve** or **Reject**.
6. The **SM Status** or **AVP Status** column changes. AVP sees a record only after SM approves it.

> Careful: A rejected record stops there. It is not offered to the dealer for welfare schemes.

## Common problems

- **No tick or cross on a row.** SM sees them only while **SM Status** is **Pending**. AVP sees them only after SM approved and while **AVP Status** is **Pending**.
- **The Overall column says SM Rejected.** SM refused the record, so it never reaches AVP.
:::
