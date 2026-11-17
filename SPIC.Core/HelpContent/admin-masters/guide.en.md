---
module: admin-masters
title: Administration and masters
icon: bi-gear
order: 117
category: Administration
color: #374151
example: DEMO-EMP-01 · MDO
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

These screens keep the basic lists that the rest of SPIC ONE uses: employees and their logins, designations (which pages each person can open), locations, crops and products, banks, and a few settings. Most of them are simple lists: add a record, edit it, switch it on or off.

- people | icon: bi-people | color: #374151
  title: People and access
  text: Register employees, give each one a role and a location, and choose a designation for page access.
- masters | icon: bi-list-check | color: #0d9488
  title: Master lists
  text: Locations, crops, products, banks, plants and other lists used by the other screens.
- tools | icon: bi-sliders | color: #2563eb
  title: Settings and tools
  text: GPS interval, CSR cut-off days, usage metrics, the IFMS import and the Data Explorer.
:::

::: section flow
title: How a new employee is set up
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- admin | type: lane | color: #374151 | roles: Admin, SuperAdmin, CorporateAdmin
  title: Administrator
  sub: Admin · SuperAdmin · CorporateAdmin
- staff | type: lane | color: #0d9488
  title: Employee
  sub: Field and office staff
- locations | lane: admin | col: 1 | icon: bi-geo-alt | color: #0d9488 | next: designation | screen: location-master
  title: Add the locations
  status: Active
  text: In **Location Master**, add the zone, state, region and headquarter the employee will work in.
- designation | lane: admin | col: 2 | icon: bi-shield-lock | color: #7c3aed | next: register | screen: designation
  title: Choose page access
  status: Active
  text: In **Designation Master**, make a designation and tick the pages it can **View**, **Entry**, **Update** or **Delete**.
- register | lane: admin | col: 3 | icon: bi-person-plus | color: #2563eb | next: signin | screen: register-employee
  title: Register the employee
  status: Active
  text: Choose the role, designation and location, then enter the login details and press **Save Employee**.
- signin | lane: staff | col: 4 | icon: bi-box-arrow-in-right | color: #16a34a
  title: Sign in and work
  text: The employee signs in with the user name and password. The menu shows only the pages the designation allows.
- upload-stopped | lane: admin | col: 3 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: register | to: locations | screen: employee-list
  title: Upload stopped
  text: A bulk upload of employees stops if a zone, state, region or HQ in the file does not exist. Add it in **Location Master** and upload the same file again.
:::

::: section statuses
title: Every status you can see
kind: statuses

- active | color: #16a34a
  label: Active
  text: The record is in use and shows in the lists of other screens.
- inactive | color: #64748b
  label: Inactive
  text: The record is kept but switched off. It is hidden from new entries.
- open | color: #dc2626
  label: Open
  text: Metrics only. An error group that nobody has marked resolved yet.
- resolved | color: #16a34a
  label: Resolved
  text: Metrics only. Someone marked this error group as fixed.
- pending | color: #2563eb
  label: Pending
  text: IFMS import only. The run is waiting to start.
- running | color: #2563eb
  label: Running
  text: IFMS import only. The download and import are going on now.
- succeeded | color: #16a34a
  label: Succeeded
  text: IFMS import only. Every report was imported.
- partial | color: #d97706
  label: PartiallySucceeded
  text: IFMS import only. Some reports were imported and some were not.
- failed | color: #dc2626
  label: Failed
  text: IFMS import only. The run or report did not work. Read the red message.
- skipped | color: #dc2626
  label: Skipped
  text: IFMS import only. The report was not run.
- disabled | color: #64748b
  label: Disabled
  text: IFMS Logins only. The automation does not use this login.
- no-password | color: #dc2626
  label: No password set
  text: IFMS Logins only. Enter a password before the next run.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role or designation allows.

- admin | color: #374151 | roles: Admin, SuperAdmin
  title: Admin and SuperAdmin
  - Open every screen in this module, except the Data Explorer for Admin
  - Register employees and choose their designations
  - Keep the master lists up to date
  - Watch the IFMS import and the metrics
- superadmin | color: #7c3aed | roles: SuperAdmin
  title: SuperAdmin only
  - Use the Data Explorer
  - Update the region and HQ of existing employees from Excel
- corporate | color: #2563eb | roles: CorporateAdmin
  title: CorporateAdmin
  - See the Metrics screen
  - Open other screens here when your designation gives them
  - Use the Data Explorer only if you are the authorized person
- others | color: #0d9488
  title: Other staff
  - See a screen here only when your designation gives it
  - You may only view, or also add, change or delete, as your designation allows
:::

::: section faq
title: Problems and answers
kind: faq

- no-pages | popular: true
  q: A new employee signs in but cannot see the pages they need
  a: Page access comes from the designation. Open **Designation Master**, edit the employee's designation and tick **View** (and **Entry** or **Update** if needed) for those pages. Then check the employee has that designation in **Employee Registration**.
- upload-missing | popular: true
  q: The employee upload says "Missing master data — nothing was uploaded"
  a: A zone, state, region or HQ in your Excel file does not exist yet. Add it in **Location Master**, then choose the same file again.
- cannot-delete
  q: I cannot delete a location or a crop. It says "Cannot Delete"
  a: The record is used by other records, which the box lists. Remove or change those first, or just switch the record to **Inactive**.
- no-save
  q: I can see a master screen but there is no Save button
  a: Your designation gives you **View** only. Ask an administrator to add **Entry** (to add) or **Update** (to edit) for that page.
- ifms-password
  q: The IFMS Logins screen says "A password needs changing"
  a: The IFMS portal password expires every 80 days. Change it on the portal first, then paste the new one into **New password** and press **Update password**.
:::

::: section employee-list
title: See and manage employees
routes: /EmployeeManagement
minutes: 2

**What this screen is for**
The **Employee Management** screen lists every employee login with their role and state. From here you register new employees, upload many at once, or edit and delete a login.

1. Use the search box, **All Role** and **All States** to find an employee in the **Employee List**.
2. Read the columns: **Employee ID**, **Name**, **User Name**, **Password**, **Role** and **State**.
3. Press the pencil to edit an employee. It opens the employee form with their details filled in.
4. Press the bin to delete the login, then press **Delete** in the **Delete this role?** box.
5. Press **Register Employee** to add one employee.
6. To add many, press **Sample Format** to get the Excel file, fill it, press **Bulk Upload** and choose the file. The box shows how many rows were **Inserted** and **Skipped**.
7. Use **Copy**, **CSV**, **Excel**, **PDF** or **Print** above the table to take the list out.

> Careful: Deleting a login cannot be undone.
> Tip: In the upload file, the phone number becomes the login password. Leave **Designation** blank only if the person needs full access.

## Bulk upload columns

| Field | What to enter |
|---|---|
| **EmployeeID**, **EmpName**, **UserName** | The employee's ID, name and login name. |
| **Permission** | The role. Allowed: AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO. |
| **Zone**, **State**, **Region**, **HQ** | AVP needs a zone. SMD and SMM need a state. RM and RMD need a state and region. MDO, MO and JMDO need state, region and HQ. They must already exist. |
| **PhoneNumber**, **EmailID** | Contact details. The phone number is the password. |
| **Designation** | Must match an active designation name. |

## Common problems

- **"Missing master data — nothing was uploaded".** Add the missing zone, state, region or HQ in **Location Master**, then choose the same file again.
- **"Issues found:"** Some rows were skipped. Read the reason under each group, correct those rows and upload them again.
- **No Register Employee or Bulk Upload button.** Your designation does not give **Entry** on **Employee Registration**.
- **Update Region / HQ.** This button is shown to SuperAdmin only. It changes only the region and HQ of existing logins from an Excel file, after a preview.
:::

::: section register-employee
title: Register or edit an employee
routes: /EmployeeRegistration
minutes: 3
tour: 1 > [data-help=am-emp-role] | 2 > [data-help=am-emp-location] | 3 > [data-help=am-emp-login] | 4 > [data-help=am-emp-details] | 5 > [data-help=am-emp-save]
screenshot: register-employee.png
hotspots: 1 @ 2.2,13.6,95.5,24.1 | 2 @ 2.2,40.1,95.5,19.9 | 3 @ 2.2,62.4,95.5,24.1 | 4 @ 2.2,88.9,95.5,11.1 | 5 @ -
screenshot-phone: register-employee.phone.jpg
hotspots-phone: 1 @ 3.1,9.8,93.8,20.4 | 2 @ 3.1,31.5,93.8,11.2 | 3 @ 3.1,44.1,93.8,25.7 | 4 @ 3.1,71.3,93.8,28.7 | 5 @ 43.8,92.1,52.1,3

**What this screen is for**
Use the **Employee Registration** screen to create a login for one employee, give them a role and a location, and choose the designation that decides which pages they can open. When you edit an employee, the title is **Edit Employee**.

1. Under **Role & Authority**, choose the **Role** and the **Designation**.
2. Under **Role Specification**, choose the location the role needs, for example **State**, **Regions** or **Headquarters**.
3. Under **Login Details**, enter the **User Name** and **Password**. Set the **Status** switch to active or inactive.
4. Under **Employee Details**, enter the **Employee Name**, **Employee ID**, **Personal Phone Number**, **Official Phone Number** and **Email ID**.
5. Press **Save Employee** (or **Update Employee** when editing). **Cancel** goes back without saving.

> Tip: The location fields change with the role. Some roles, such as Director, need no location.

## Fields explained

| Field | What to enter |
|---|---|
| **Role** | The person's job role. Required. |
| **Designation** | Controls page access. Required for CommonRole. |
| **Zone** | For AVP. |
| **State** | For SMD, SMM, MDAdmin, MDHOD, RM, RMD, MO, MDO and JMDO. |
| **Regions** | One or more regions, for RM and RMD. |
| **Region** and **Headquarters** | One region and one or more headquarters, for MO, MDO and JMDO. |
| **User Name** | The login name. No spaces. Required. |
| **Password** | Required for a new employee. |
| **Personal Phone Number** | 10 digits. Required. |
| **Email ID** | A valid email address. Required. |

## Common problems

- **"Please correct the highlighted fields and try again."** Read the red message under each field and fix it.
- **"Username must not contain spaces or invisible characters."** Type the user name again without spaces.
- **"A user or employee with these details already exists."** Another login has the same **User Name**, **Employee ID** or **Email ID**.
- **"Selected Region does not belong to the selected State."** Choose the state first, then a region inside it.
- **No Save Employee button.** Your designation does not give **Entry** (or **Update** for editing) on **Employee Registration**.
:::

::: section designation
title: Create a designation and its page access
routes: /Designation
minutes: 3
tour: 1 > [data-help=am-des-name] | 2 > [data-help=am-des-pagesearch] | 3 > [data-help=am-des-grid] | 4 > [data-help=am-des-save] | 5 > [data-help=am-des-search]
screenshot: designation.png
hotspots: 1 @ 4.6,18.5,90.8,7.8 | 2 @ 30.2,30.8,39.3,5.1 | 3 @ 6.5,37.3,86.9,55 | 4 @ 87,96.7,8.4,3.3 | 5 @ -

**What this screen is for**
The **Designation Master** screen decides which pages a person can open and what they can do there. Each employee gets one designation.

1. Under **Add New Designation**, type the **Designation Name**. Tap the switch to set **Active** or **Inactive**.
2. In **Role Access**, type in **Search PageName...** to find a page quickly.
3. Tick **View**, **Entry**, **Update** or **Delete** for each page. **ALL** ticks all four for that page. **Select All** ticks every page.
4. Press **Save** (**Update** when editing). **Cancel** clears the form.
5. In **Existing Designations**, search the list. The pencil edits a designation. The slash circle deactivates it and the arrow activates it again.

> Tip: **View** lets a person open the page. **Entry** lets them add, **Update** lets them change, and **Delete** lets them remove records.
> Careful: A deactivated designation stays with the users who have it, but it is hidden from active lists.

## Common problems

- **"Select at least one permission".** Tick at least one box before saving.
- **"Designation '...' already exists".** Use a different name, or edit the existing one.
- **I do not see the Add New Designation form.** Your designation gives you **View** only.
:::

::: section location-master
title: Keep the location list
routes: /LocationMaster
minutes: 3
screenshot: location-master.png
screenshot-phone: location-master.phone.jpg

**What this screen is for**
The **Location Master** screen holds zones, states, districts, sub-districts, regions and headquarters. Employees, dealers and reports use these lists.

1. Choose a tab: **Zones**, **States**, **Districts**, **Sub-Districts**, **Regions** or **Headquarters**.
2. Fill the form, for example **Zone Name**, **State Name** and **Zone**, or **Headquarter Name** and **Region**.
3. Set the switch to **Active** or **Inactive** and press **Save**. **Cancel** clears the form.
4. To add many records, press **Sample Format**, fill the Excel file, press **Bulk Upload** and choose the file.
5. In the table, search or choose a status. Tap the status to switch it, the pencil to edit, or the bin to delete.

> Tip: Add in this order: zone, then state, then region, then headquarter. Districts belong to a state and sub-districts to a district.
> Careful: Delete cannot be undone. A location used by other records cannot be deleted.

## Fields explained

| Field | What to enter |
|---|---|
| **Zone Name**, **Zone Code**, **Zone Color Code** | The zone and its colour on maps. The name is required. |
| **State Name**, **State Code**, **Zone** | The state and the zone it is in. Name and zone are required. |
| **District Name**, **State** | Both required. |
| **Sub-District Name**, **District** | Both required. **State** only helps you find the district. |
| **Region Name**, **Region Code**, **State** | Name and state are required. |
| **Headquarter Name**, **Region** | Both required. |

## Common problems

- **"Zone is required."** or **"Region is required."** Choose the parent location from the list.
- **"Cannot Delete".** The record is used elsewhere. Change those records first, or switch it to **Inactive**.
- **Bulk upload shows "Skipped Groups:" or "Issues:".** Read the reasons, press **Copy** to keep them, correct the file and upload again.
:::

::: section agriculture-master
title: Keep the crop and product lists
routes: /Agriculture
minutes: 3
screenshot: agriculture-master.png
screenshot-phone: agriculture-master.phone.jpg

**What this screen is for**
The **Agriculture & Product Master** screen holds crops, crop types, crop stages, competitors, sectors, units, categories, product groups and products. Other screens use these lists.

1. Choose a tab: **Crops**, **Crop Types**, **Crop Stages**, **Competitors**, **Sector**, **Units**, **Categories**, **Product Groups** or **Products**.
2. Fill the form. For **Crop Types** and **Crop Stages**, choose the **Crop** first. For **Products**, choose the **Category** and add the **Product Group** and **Rate Per Unit (RPU)** if known.
3. Set the switch to **Active** or **Inactive** and press **Save**. **Cancel** clears the form.
4. To add many records, press **Sample Format**, fill it, press **Bulk Upload** and choose the file. The box shows the inserted, updated and rejected rows.
5. In the table, search or choose a status. Tap the status to switch it, the pencil to edit, or the bin to delete.

> Tip: Make the units before the categories, and the categories before the products.
> Careful: Delete cannot be undone. A record used elsewhere cannot be deleted.

## Fields explained

| Field | What to enter |
|---|---|
| **Category Name** and **Unit** | Both required. The second switch marks the category **Speciality** or **Regular**. |
| **Crop** | The crop a crop type or crop stage belongs to. Required. |
| **Product Name** and **Category** | Both required. A product name can be used once in each category. |
| **Rate Per Unit (RPU)** | The product's rate. Optional. |

## Common problems

- **"Please select an active crop."** The crop is switched off. Switch it on in the **Crops** tab first.
- **"... already exists".** A record with this name is already there. Edit it instead.
- **No Sample Format or Bulk Upload button.** These are not offered on **Crop Types** and **Crop Stages**, or your designation has no **Entry**.
:::

::: section financial-master
title: Keep the bank and financial year lists
routes: /Financial
minutes: 2
screenshot: financial-master.png
screenshot-phone: financial-master.phone.jpg

**What this screen is for**
The **Financial Master** screen holds the banks and the financial years used in other forms.

1. Choose the **Bank** or **Financial Year** tab.
2. For a bank, enter the **Bank Name** and **IFSC Prefix**, and add the **Bank Logo** if you have it. For a financial year, enter the **Financial Year Name**, **Start Date** and **End Date**.
3. Set the switch to **Active** or **Inactive** and press **Save**. **Cancel** clears the form.
4. Banks show as cards, with **Show more** at the bottom. Financial years show in a table with pages.
5. Tap the status to switch it, the pencil to edit, or the bin to delete.

> Careful: Delete cannot be undone.

## Fields explained

| Field | What to enter |
|---|---|
| **IFSC Prefix** | Only the first 4 letters of the IFSC code, for example HDFC or SBIN. Required. |
| **Bank Logo** | A picture under 512 KB. Optional. |

## Common problems

- **"IFSC Prefix is required."** Enter the first 4 letters of the IFSC code.
- **"Logo file must be under 512 KB."** Choose a smaller picture.
- **Bulk Upload does nothing.** Bulk upload is not ready on this screen yet. Add records one by one.
:::

::: section relationship-master
title: Keep the relationship list
routes: /Relationship
minutes: 1
screenshot: relationship-master.png
screenshot-phone: relationship-master.phone.jpg

**What this screen is for**
The **Relationship Master** screen holds the list of relationships, for example the relation of a person named in a dealer's details.

1. Under **Add New Relationship**, type the **Relationship Name**.
2. Set the switch to **Active** or **Inactive** and press **Save**. **Cancel** clears the form.
3. In **Existing Relationships**, search or choose a status.
4. Tap the status to switch it, the pencil to edit, or the bin to delete.

> Careful: Delete cannot be undone.

## Common problems

- **"Relationship name is required."** Type a name first.
- **Bulk Upload does nothing.** Bulk upload is not ready on this screen yet. Add records one by one.
:::

::: section plant-dealer-nature
title: Keep the plant and dealer nature lists
routes: /PlantDealerNatureMaster
minutes: 1
screenshot: plant-dealer-nature.png
screenshot-phone: plant-dealer-nature.phone.jpg

**What this screen is for**
The **Plant & Dealer Nature Master** screen holds the plants and the dealer natures used in Excel uploads and dealer records.

1. Choose the **Plants** or **Dealer Natures** tab.
2. Type the **Plant Name** or **Dealer Nature Name**.
3. Set the switch to **Active** or **Inactive** and press **Save**. **Cancel** clears the form.
4. In the table, search or choose a status. Tap the status to switch it, the pencil to edit, or the bin to delete.

> Careful: Delete cannot be undone.

## Common problems

- **"Plant name is required."** Type a name first.
- **The status does not change when I tap it.** Your designation has no **Update** on this page.
:::

::: section expense-master
title: Keep the expense list
routes: /ExpenseInformation
minutes: 1
screenshot: expense-master.png
screenshot-phone: expense-master.phone.jpg

**What this screen is for**
The **Expense Information Master** screen holds the list of expense names.

1. Under **Add New Expense Information**, type the **Expense Name**.
2. Set the switch to **Active** or **Inactive** and press **Save**. **Cancel** clears the form.
3. In **Existing Expense Informations**, search or choose a status.
4. Tap the status to switch it, the pencil to edit, or the bin to delete.

> Careful: Delete cannot be undone.

## Common problems

- **"Expense name is required."** Type a name first.
:::

::: section gps-settings
title: Set the GPS tracking interval
routes: /GpsSettings
minutes: 1
screenshot: gps-settings.png
screenshot-phone: gps-settings.phone.jpg

**What this screen is for**
The **GPS Settings** screen sets how often the JMDO mobile app records the GPS location during live tracking.

1. Look at **Current Saved Interval**, **System Default** (20 s) and **Allowed Range** (1 to 100 seconds).
2. Move the slider, or type a number of seconds in the box.
3. Press **Save Settings**.
4. To go back to 20 seconds, press **Reset to Default (20s)** and then **Save Settings**.

> Tip: A small number records the location more often but uses more battery. A large number saves battery.

## Common problems

- **"Tracking interval must be an integer between 1 and 100 seconds."** Enter a whole number from 1 to 100.
- **No Save Settings button.** Your designation has no **Update** on **GPS Settings**.
:::

::: section csr-exceeding-day
title: Set the CSR cut-off days
routes: /CSRExceedingDaySetting
minutes: 2
screenshot: csr-exceeding-day.png
screenshot-phone: csr-exceeding-day.phone.jpg

**What this screen is for**
The **CSR Exceeding Day Setting** screen sets the day of the month after which CSR-1 and CSR-2 go through the longer approval route. Only one setting is active at a time.

1. Enter the **CSR1 Day** and **CSR2 Day** (1 to 31).
2. Set the **Status** switch to **Active** and press **Save**. **Cancel** clears the form.
3. In **CSR Exceeding Day Settings**, see all settings. Tap the status to switch it, the pencil to edit, or the bin to delete.

> Tip: A CSR-1 created after the **CSR1 Day** of the month, or a CSR-2 submitted after the **CSR2 Day**, needs the longer approval.
> Careful: Saving an active setting switches the old active setting off.

## Common problems

- **"CSR1 Day must be between 1 and 31."** Enter a day from 1 to 31.
- **"You do not have permission to save this setting."** Your designation has no **Entry** or **Update** on this page.
:::

::: section metrics
title: See how the app is used
routes: /Metrics
minutes: 2
screenshot: metrics.png
screenshot-phone: metrics.phone.jpg

**What this screen is for**
The **Metrics** screen shows who is using SPIC ONE, on which app, which pages and services are used, and what went wrong. It only shows numbers; nothing is changed from here except marking errors.

1. Choose the period: **7 days**, **30 days** or **90 days**. Choose an app, or keep **All apps**.
2. Read the live line at the top: people online in the last 15 minutes, requests and errors in the last hour.
3. Open a tab: **Overview**, **Users**, **Pages**, **API** or **Errors**.
4. On **Errors**, open **Filters** to search or choose the **Source** and **Status** (**Open**, **Resolved** or **All**).
5. Tap an error to see its details. Press **Mark resolved** when it is fixed, or **Reopen**.

## Common problems

- **"You do not have access to Metrics".** Ask an administrator to add the Metrics page to your designation.
- **"The metrics could not be loaded".** Check the connection and press **Try again**.
- **"Live numbers unavailable".** The live numbers could not be read. They refresh every minute.
:::

::: section ifms-auto-import
title: Watch the nightly IFMS import
routes: /IfmsAutoImport
minutes: 2

**What this screen is for**
The **IFMS Auto Import** screen shows the nightly download of reports from the IFMS portal (dbtfert.nic.in) and their import into SPIC ONE. It runs on its own at 04:05.

1. Look at the cards for the latest run: **Last run**, **Reports**, **Rows added**, **Rows updated** and **Login**.
2. Each run shows its date, how many reports were imported and a status such as **Succeeded** or **Failed**.
3. Tap a run to see each report with its **Status**, **Rows**, **New**, **Updated** and **Skipped**.
4. If a yellow CAPTCHA box appears, type the letters you see and press **Send**. The download then continues.
5. Press **Run now** to start a run at once, or **Refresh** to load the latest results.

## Common problems

- **"No runs yet".** The first run starts at 04:05, or press **Run now**.
- **"Could not queue a run."** Read the message and try again later.
- **The CAPTCHA says "expired".** A new picture comes on its own. Type the new one.
- **The Login card says "failed".** The IFMS password may have expired. Check **IFMS Logins**.
:::

::: section ifms-logins
title: Keep the IFMS portal logins
routes: /IfmsLogins
minutes: 2

**What this screen is for**
The **IFMS Logins** screen keeps the portal login of each company that the nightly IFMS import uses. Passwords are stored safely and are never shown again.

1. Each company card shows a tag such as **Disabled**, **No password set**, or how many days are left before the password expires.
2. Read **Password set**, **Expires**, **Last login** and **Result**.
3. When the password changes on the portal, paste it in **New password**, check **OTP goes to**, and press **Update password**.
4. Press **Disable** to stop using a login, or **Enable** to use it again.
5. To add a company, fill **Key**, **Company**, **Username** and **Password** under **Add a login** and press **Add**.

> Careful: The portal password expires every 80 days. Change it on the portal first, then here, or the night run fails.

## Common problems

- **"Enter the new password first."** Paste the new password before pressing **Update password**.
- **"Key, username and password are all needed."** Fill all three to add a login.
:::

::: section data-explorer
title: Look at and correct data directly
routes: /DataExplorer
roles: SuperAdmin, CorporateAdmin
minutes: 3

**What this screen is for**
The **Data Explorer** screen lets an authorized person browse, filter, export and correct rows in any table of the database. Every change is written to the audit log with your name. It needs its own password.

1. Enter the Data Explorer password and press **Unlock**.
2. On **Tables**, search and pick a table. Type in **Filter (any column contains)** and press Enter. Tap a column name to sort.
3. Tap a row to edit it, then press **Save** or **Delete**. Press **New row** and **Insert** to add one. **Export CSV** downloads the rows.
4. On **Query**, type one SQL statement and press **Run**. A change shows a dry run first; press **Confirm: change ... row(s)** to apply it.
5. Open **Audit log** to see the last changes. Press **Lock** when you finish.

> Careful: Changes here go straight into the live data and cannot be undone. Use the normal screens whenever you can.

## Common problems

- **"Data Explorer is available to SuperAdmin or the authorized CorporateAdmin only."** Your login is not allowed to use this screen.
- **Save or Delete does not go through.** The key must match exactly one row.
:::
