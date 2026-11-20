---
module: dealer-application
title: Dealer application
icon: bi-shop
order: 101
category: Dealers
color: #7c3aed
example: DEMO Agro Traders · New dealer
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Register a dealer in 13 short steps: who the dealer is, the business, the owners, the money and the documents. Then submit the application. RM, SMM and AVP check it before the dealer is approved.

- steps | icon: bi-list-ol | color: #7c3aed
  title: 13 steps, one at a time
  text: Each step saves when you press Next. You can come back later and finish it.
- check | icon: bi-clipboard-check | color: #d97706
  title: One final check
  text: The last step shows what is missing before you submit.
- approve | icon: bi-shield-check | color: #16a34a
  title: Three approvals
  text: RM, then SMM, then AVP approve the application.
:::

::: section flow
title: How an application moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- applicant | type: lane | color: #7c3aed | roles: MO, MDO, JMDO, Admin
  title: Applicant
  sub: MO · MDO · JMDO (new dealer: Admin · AVP)
- rm-team | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: Region
- smm-team | type: lane | color: #d97706 | roles: SMD, SMM
  title: SMM
  sub: State
- avp-team | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: Final approval
- office-team | type: lane | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: New dealers only
- register | lane: applicant | col: 1 | icon: bi-person-plus | color: #64748b | next: steps | screen: register
  title: Start the registration
  status: Draft
  text: Press **Create Existing Dealer** or **Create New Dealer** and fill step 1, **Dealer Registration**.
- steps | lane: applicant | col: 2 | icon: bi-list-ol | color: #7c3aed | next: documents | screen: experience
  title: Fill the business steps
  status: Registration In Progress
  text: Fill experience, sales, warehouses, market, companies, owners, sales plan, assets and credit limit. Press **Next** on each step.
- documents | lane: applicant | col: 3 | icon: bi-paperclip | color: #7c3aed | next: submit | screen: enclosures
  title: Upload the documents
  status: Registration In Progress
  text: Upload every document marked **Required** on the **Enclosures** step.
- submit | lane: applicant | col: 4 | icon: bi-send | color: #0b6b3a | next: rm | screen: final-submission
  title: Check and submit
  status: Application Ready for Submission
  text: Check that every section is complete and press **Submit Registration**.
- incomplete | lane: applicant | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: submit | to: steps | screen: final-submission
  title: Something is missing
  text: If a section or document is missing, the app does not submit. Open the section with the pencil, fix it and submit again.
- rm | lane: rm-team | col: 5 | icon: bi-hourglass-split | color: #0d9488 | next: smm | return: submit
  title: RM checks it
  status: In RM
  text: The RM approves the application, or rejects it back to the applicant.
- smm | lane: smm-team | col: 6 | icon: bi-hourglass-split | color: #d97706 | next: avp | return: rm
  title: SMM checks it
  status: In SMM
  text: The SMM approves it, or rejects it back to the RM.
- avp | lane: avp-team | col: 7 | icon: bi-patch-check | color: #2563eb | next: office | return: smm
  title: AVP approves
  status: In AVP
  text: The AVP gives the final approval, or rejects it back to the SMM.
- office | lane: office-team | col: 8 | icon: bi-123 | color: #64748b | next: done
  title: Dealer IDs for a new dealer
  status: In AVP Office
  text: For a new dealer only, the AVP Office saves the company Dealer IDs. An existing dealer skips this step.
- done | lane: applicant | col: 9 | icon: bi-check-circle | color: #16a34a | screen: application-pdf
  title: Dealer approved
  status: Approved
  text: The application is complete. You can open the dealership application form from the dealer card.
:::

::: section statuses
title: Every status you can see
kind: statuses

- draft | color: #64748b
  label: Draft
  text: Saved but not submitted yet. You can still change it.
- in-progress | color: #7c3aed
  label: Registration In Progress
  text: You are filling the steps.
- ready | color: #0b6b3a
  label: Application Ready for Submission
  text: Every section and document is done. You can submit.
- in-rm | color: #0d9488
  label: In RM
  text: Submitted. The RM is checking it.
- in-smm | color: #d97706
  label: In SMM
  text: The RM approved it. The SMM is checking it.
- in-avp | color: #2563eb
  label: In AVP
  text: The SMM approved it. Waiting for the AVP.
- in-office | color: #64748b
  label: In AVP Office
  text: New dealer approved by the AVP. Waiting for the company Dealer IDs.
- approved | color: #16a34a
  label: Approved
  text: The application is fully approved.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: Sent back to the applicant. Correct it and submit again.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: Sent back to the RM to correct.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: Sent back to the SMM to correct.
- uploaded | color: #16a34a
  label: Uploaded
  text: The document is attached.
- missing | color: #ea580c
  label: Missing
  text: The document is not attached yet.
- consent | color: #0891b2
  label: Consent Provided
  text: The dealer gave no credit limit details. The credit limit steps are skipped.
- active | color: #16a34a
  label: Active
  text: The dealer is trading now.
- inactive | color: #d97706
  label: Inactive
  text: The dealer is not trading now.
- terminated | color: #475569
  label: Terminated
  text: The dealership has ended.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- creator | color: #7c3aed | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - Register existing dealers
  - Fill all steps and upload documents
  - Submit, and correct an application rejected by the RM
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - Register new dealers with Create New Dealer
  - AVP gives the final approval
- rm | color: #0d9488 | roles: RM, RMD
  title: RM
  - Check applications from your region
  - Approve, or reject back to the applicant
- smm | color: #d97706 | roles: SMD, SMM
  title: SMM
  - Check applications from your state
  - Approve, or reject back to the RM
- office | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - Save the company Dealer IDs for approved new dealers
:::

::: section faq
title: Problems and answers
kind: faq

- warehouse-stuck | popular: true
  q: I cannot go past the Warehouse step
  a: You need at least two warehouses, two rail heads and two ports. Add each one with its **Add** button, then press **Next**.
- not-submitted | popular: true
  q: Submit Registration says some sections are incomplete
  a: On **Final Submission**, look for the orange marks under **Section Completion Status** and **Enclosures**. Tap the pencil, finish that step, and submit again.
- red-step
  q: A part of the step bar at the top is red
  a: That step has a problem or missing data. Tap the red part to open the step and fix it.
- ocr
  q: The app filled the GST, PAN or bank number by itself
  a: The app reads the uploaded file. Always check the number. If it is wrong, type the correct one.
- skipped-credit
  q: The Credit Limit steps were skipped
  a: Consent was given on the **Investment** step. Open **Investment** and press **Remove Consent** to fill the credit limit.
- no-code
  q: My new dealer has no Dealer Code
  a: A new dealer gets the Dealer Code only after the final approval.
:::

::: section register
title: Start a dealer registration (step 1)
routes: /Register
minutes: 6
tour: 1 > [data-help=dr-basic] | 2 > [data-help=dr-business] | 3 > [data-help=dr-location] | 4 > [data-help=dr-bank] | 5 > [data-help=dr-tax] | 6 > [data-help=dr-license]
screenshot: register.png
hotspots: 1 @ 2.2,21.9,95.5,33.1 | 2 @ 2.2,57.9,95.5,32.4 | 3 @ 2.2,93.1,95.5,6.9 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: register.phone.jpg
hotspots-phone: 1 @ 3.1,14.7,93.8,42.9 | 2 @ 3.1,58.8,93.8,39.3 | 3 @ 3.1,99.2,93.8,0.8 | 4 @ - | 5 @ - | 6 @ -

**What this screen is for**
The **Dealer Registration** screen is step 1 of 13. It records who the dealer is, where the shop is, the bank account and the tax and licence details. Open the **Register** menu and press **Create Existing Dealer**. Admin and AVP use **Create New Dealer** for a brand-new dealer.

1. Under **Basic Information**, choose **Dealer**, **Department** or **Institution**, and tap **SPIC**, **GREEN STAR** or both. Check **State**, **Region** and **HQ**. They are filled from your profile. For an existing dealer, also choose **Active**, **Inactive** or **Terminated**.
2. Under **Business Information**, choose the **Dealer Code & Name** and the **Parent Dealer Code & Name** (existing dealers only). Type the **Firm Name** as on the fertilizer licence and choose the **Date of Appointment for SPIC** or **for Greenstar**.
3. Under **Primary Location**, stand at the shop and press **Use Current Location**, or paste the **Google Map URL**. Then fill the address, **Pincode**, **District**, **State** and the three phone numbers.
4. Under **Bank Details**, upload the cheque with **Upload Cheque (PDF or Image)**. The app reads it and fills the account details. Check **Account Holder Name**, **Account Number**, **Bank Name**, **Bank Branch** and **IFSC Code**.
5. Under **Tax & Compliance**, upload the GST certificate, PAN card and e-Aadhaar as PDF files. The app reads each file and fills the number. Check every number. **TAN Number** is optional.
6. Fill **Wholesale Fertilizer Details (as per iFMS)** or **Retail Fertilizer Details (as per iFMS)**: licence number, **Expiry Date**, **mFMS ID** and the licence file. Answer **Office Automation**, then press **Next**.

> Tip: For an existing dealer, also fill **Trade Deposit Details for SPIC** or **for Greenstar**: amount, **Receipt No** and **Deposit Date**.
> Careful: If you choose **Department** or the status **Terminated**, this is the only page. The button says **Submit** and sends the form for review at once.

## Fields explained

| Field | What to enter |
|---|---|
| **Firm Name** | The business name as on the fertilizer licence. Required. |
| **Pincode** | 6 digits. The app looks up the area. |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10-digit mobile numbers. |
| **GST Number** | 15 characters, for example 22AAAAA0000A1Z5. |
| **PAN Number** | 10 characters, for example ABCDE1234F. |
| **Aadhaar Number** | Exactly 12 digits. |
| GST, PAN, Aadhaar and TAN files | PDF only, less than 5 MB. |
| Licence | At least one licence, wholesale or retail, with its certificate. |

## Common problems

- **The page does not move when I press Next.** A starred field is empty or wrong. Look for the red message under the field, for example "Pincode must be 6 digits".
- **"At least one Fertilizer License (Wholesale or Retail) is required"** Fill one licence and upload its certificate.
- **"OCR could not extract the required information from this document."** Upload a clearer file, or type the details yourself.
- **"Only PDF files are allowed" or "File size must be less than 5 MB".** Save the document as a smaller PDF and upload it again.
:::

::: section experience
title: Experience in the fertilizer trade (step 2)
routes: /Experience
minutes: 3
screenshot: experience.png
screenshot-phone: experience.phone.jpg

**What this screen is for**
The **Experience In Fertilizer Trade** screen records how long the dealer has sold fertilizer, and for which companies.

1. In **Year of Experience**, enter how many years the dealer has been in the fertilizer business.
2. Under **Add Experience**, choose the **Company Name** and enter the **Number of the years**.
3. Enter the **Quantity (MT)** and the **Turnover (Lakhs)** for that company.
4. Turn **Still Active** off if the dealer no longer works with that company.
5. Press **Add Experience**. The company appears under **Saved Experience**. Repeat for each company.
6. Use the pencil to change a record and press **Update Experience**, or the bin to delete it. Then press **Next**.

## Common problems

- **"Please select a company."** Choose the **Company Name** before pressing **Add Experience**.
- **Step 2 turns red on the step bar.** Enter the **Year of Experience** and add at least one company.
:::

::: section annual-sales
title: Sales in the last year (step 3)
routes: /AnnualSales
minutes: 4
screenshot: annual-sales.png
screenshot-phone: annual-sales.phone.jpg

**What this screen is for**
The **Annual Sales Data For All Companies** screen records what the dealer sold in the **Last Financial Year**, for all companies together.

1. Tap a category tab at the top of the table.
2. Tap a product group to open it and see its products.
3. For each product, enter **Own Retail Qty**. If the dealer has a wholesale licence, also enter **Sale to Dealer Qty**.
4. The turnover (**TO (₹)in Lakhs**) and the totals are worked out for you. Check the summary cards and **Grand Total** below the table.
5. If a product is missing, press **Add Product**, fill **Category**, **Product Group**, **Product Name** and **Rate Per Unit (RPU)**, and press **Save**.
6. Press **Next**.

## Common problems

- **There is no Sale to Dealer Qty column.** It appears only when the dealer has a wholesale licence in step 1.
- **Step 3 turns red on the step bar.** Enter a quantity for at least one product.
- **"A Product with the name ... already exists in this Category."** The product is already in the list. Look for it in its product group.
:::

::: section warehouse
title: Nearest warehouses, rail heads and ports (step 4)
routes: /Warehouse
minutes: 4
screenshot: warehouse.png
screenshot-phone: warehouse.phone.jpg

**What this screen is for**
The **Warehouse & Logistics** screen records the facilities nearest to the dealer. The distance is worked out from the shop location you gave in step 1.

1. Read the **Logistics Note Information**. You need at least two warehouses, two rail heads and two ports.
2. In the **Warehouse** card, search and choose the **Facility Name**. **Distance (KM)** fills in by itself.
3. Enter the **Freight (₹/ton/Km)** and press **Add Warehouse**.
4. Do the same in the **Rail Head** and **Port** cards.
5. Check the list under **Selected Nearest Facilities**. Use the pencil to change one or the bin to delete it.
6. Press **Next**.

## Common problems

- **"A minimum of two Warehouses, two Rail Heads, and two Ports must be configured to proceed further."** Add the missing facilities first.
- **"Dealer location (Latitude/Longitude) is not set."** Go back to step 1 and fill the **Google Map URL** or **Latitude & Longitude**.
- **The Add button is grey.** Choose a **Facility Name** first, and wait for the distance to appear.
:::

::: section market-details
title: Market details (step 5)
routes: /MarketDetails
minutes: 3
screenshot: market-details.png
screenshot-phone: market-details.phone.jpg

**What this screen is for**
The **Market Details** screen records the blocks and villages the dealer serves, with their crops and farmers.

1. Under **Blocks / Villages**, type the **Name of Block/ Village**.
2. Open **Major Crops** and tick the main crops. You can search the list.
3. Enter the **No of Farmer**, and the **No of Dealer** if that box shows.
4. Choose the **Season From & To Month** and the **Irrigation Type**: **CANAL**, **TANK**, **WELL** or **RAINFED**.
5. Press **Add Blocks/ Villages**. The entry appears under **Added Market Details**. Repeat for each block or village.
6. Press **Next**.

## Common problems

- **"No regional blocks added yet."** Add at least one block or village. Otherwise step 5 shows red.
- **I cannot see No of Dealer.** It appears only when the dealer has a wholesale licence.
:::

::: section companies
title: Companies in the dealer's area (step 6)
routes: /Companies
minutes: 1
screenshot: companies.png
screenshot-phone: companies.phone.jpg

**What this screen is for**
The **Companies Operating In Your Place** screen shows which fertilizer companies sell in the dealer's area.

1. Tap each company that sells in the area, including **SPIC** and **GREEN STAR**. A tick shows it is chosen.
2. Tap a company again to remove the tick.
3. Press **Next**.

## Common problems

- **Step 6 turns red on the step bar.** Tick at least one company.
:::

::: section proprietor
title: Owner and partner details (step 7)
routes: /Proprietor
minutes: 6
screenshot: proprietor.png

**What this screen is for**
The **Proprietor Details** screen records the legal type of the business and every owner or partner, with their family. These details are also used for the SDWA welfare scheme.

1. Choose the **Business Entity Type**: **Sole Proprietor**, **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** or **Society**.
2. Under **Ownership Information**, fill **Authorized Name**, **Son of / Daughter of**, **Phone Number**, **PAN number** and the **Aadhaar Card Number**.
3. Upload the **Proprietor / Managing Partner Image** and fill the **Residential Address Details**.
4. Under **Family Members**, add each member and press **Add Details**. Under **Other Occupation / Agencies**, choose **Yes** or **No** and add any other business.
5. Press **Save Details**. The person appears under **Saved Details**. Add each partner the same way.
6. Press **Next**.

| Business Entity Type | People needed |
|---|---|
| **Sole Proprietor** | Exactly 1 proprietor. |
| **Society** | At least 1 authorized person. |
| **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** | At least 2 partners. |

## Common problems

- **"Please upload Proprietor / Managing Partner Image before saving."** Upload the photo, then press **Save Details** again.
- **"Please add at least 2 partners for the selected entity type."** Save a second partner before pressing **Next**.
- **The form is gone after I saved one person.** For a **Sole Proprietor**, only one person is allowed. Use the pencil under **Saved Details** to change it.
:::

::: section sales-planning
title: Sales plan for the coming year (step 8)
routes: /SalesPlaning
minutes: 4
screenshot: sales-planning.png
screenshot-phone: sales-planning.phone.jpg

**What this screen is for**
The **Sales Planning & Forecast** screen records how much the dealer plans to sell in each month of the coming financial year, from April to March.

1. Under **Business Plan**, tap a category tab.
2. Tap a product group to open it.
3. For each product, enter the planned quantity under each month from **April** to **March**. The value in lakhs and the **Total** are worked out for you.
4. Check the **Monthly Total** row and the summary cards, such as **Fertilizer Products Summary**.
5. Press **Next**.

## Common problems

- **Step 8 turns red on the step bar.** Enter a plan for at least one product.
:::

::: section investment
title: Assets, loans and credit consent (step 9)
routes: /Investment
minutes: 5
screenshot: investment.png
screenshot-phone: investment.phone.jpg

**What this screen is for**
The **Investment** screen records what the dealer owns and owes, and shows the **Net Financial Position**.

1. If the dealer will not give credit limit details, press **Give Consent**. The credit limit steps are then skipped and recorded as zero. **Remove Consent** undoes it.
2. Under **Assets (Banking & Properties)**, add at least one of **Banking**, **Land** or **Building**. Fill the card and press **Add Banking**, **Add Land** or **Add Building**.
3. For land and buildings, upload the **EC (Encumbrance Certificate)**, **Property Document** and **Valuation Certificate** if you have them.
4. Under **Movable** and **Infrastructure**, enter the **Movable Assets Value**, **Own Godown Capacity** and **Rent Godown Capacity**.
5. Under **Loan Liabilities List**, enter each loan's **Loan Source / Institution** and **Loan Value (₹) In Lakhs**, and press **Add Loan**.
6. Check the **Net Financial Position**: **Fixed Assets** minus **Total Liabilities**. Then press **Next**.

## Common problems

- **"Please add details for at least one of Banking, Land, or Building before proceeding."** Add one asset first.
- **"OD Value is required when Banking details are provided."** Enter the **OD Limit (₹) Lakhs**.
- **"Market Value is required when Land details are provided."** Enter the market value. The same applies to a building.
:::

::: section credit-limit
title: Credit limit or trade deposit (steps 10 and 11)
routes: /CreditLimit, /CreditLimitForGreenStar
minutes: 5
screenshot: credit-limit.png
screenshot-phone: credit-limit.phone.jpg

**What this screen is for**
Step 10 is for SPIC and step 11 is for Greenstar. Step 11 shows only when **GREEN STAR** was chosen in step 1. For an existing dealer, these screens (**Credit Limit — SPIC** and **Credit Limit — GreenStar**) work out a recommended credit limit. For a new dealer, they collect the trade deposit instead.

1. Check the dealer summary, the **Existing Credit Limit** (**Valid From** and **Valid To**) and the sales plan cards at the top.
2. Under **Details of Securities**, enter the **Number**, **Other Details** and **Amount (₹) In Lakhs** for each security, such as **Fixed Deposit with SPIC** or **BG in favour of SPIC**.
3. In the **SPIC Valuation** or **Greenstar Valuation** table, most values come from earlier steps. Enter **Monthly Average Net Overdues during last year** yourself. It is required, 0 or more.
4. Check the **Grand Total** score and the **Recommended Credit Limit** box: **Proposed Limit**, **Credit Multiplier** and **MO Recommended Credit Limit**.
5. Enter any **Additional Credit Limit (₹) In Lakhs**, then press **Next**.

## For a new dealer

- On step 10, fill the **Application Fee** (**Bank**, **DD Number**, **DD Date**, **Amount (₹)**, **Payable At**) and the **Trade Deposit Details**. Upload both attachments as PDF, JPG or PNG.
- On step 11, fill the **GFL Trade Deposit Details** and upload the attachment.

> Tip: For an approved dealer, the **Credit Limit** button on the dealer card opens this screen on its own. The button at the bottom then says **Submit**.

## Common problems

- **"Monthly Average Net Overdues during last year is required and must be 0 or greater."** Enter the value, even if it is 0.
- **"Complete all SPIC Application Fee and Trade Deposit fields, and upload both attachments."** Fill every box and upload both files.
- **These steps do not open.** Consent was given on the **Investment** step.
:::

::: section enclosures
title: Upload the documents (step 12)
routes: /Enclosures
minutes: 5
screenshot: enclosures.png

**What this screen is for**
The **Enclosures** screen collects every document in one place. It shows which documents are **Uploaded** and which are **Missing**.

1. Check **Document Completion Progress** at the top. It shows how many documents are uploaded and how many required ones are pending.
2. Under **Documents already added in previous steps**, check the GST, PAN, Aadhaar and licence files. Upload any **Missing** one with **Upload Document**.
3. Check **Partner / Proprietor Documents** and **Property Documents (Land & Building)**. Upload any missing photo, Aadhaar, PAN, EC, property document or valuation certificate.
4. Under **Additional Documents**, choose the year and upload **IT Returns for Last Year** and **IT Returns for Last 2nd Year**, and every other document marked **Required**.
5. For the specimen signature, press **Download Template**, print it, get it signed by all signatories, and upload it under **Spic Specimen Signature** or **Greenstar Specimen Signature**.
6. Press **Next** to go to the final check.

> Careful: Deleting a document cannot be undone. Upload the right file again if you delete one by mistake.

| Document | What to upload |
|---|---|
| Most documents | PDF, JPG or PNG, up to 10 MB. |
| **List of Retailers with Complete Details** | Excel or CSV file, when asked. Press **Download Sample Template** for the format. |
| **Request Letter for Dealership** | New dealers only. On the firm's letterhead with seal and signature. |

## Common problems

- **"You still have ... required document(s) pending."** Upload every document marked **Required** and **Missing**.
- **"No partners found. Add partners in Proprietor step first."** Go back to step 7 and save the owners or partners.
:::

::: section final-submission
title: Check and submit the application (step 13)
routes: /FinalSubmission
minutes: 3
tour: 1 > [data-help=dr-fs-progress] | 2 > [data-help=dr-fs-sections] | 3 > [data-help=dr-fs-enclosures] | 4 > [data-help=dr-fs-terms] | 5 > [data-help=dr-fs-result]
screenshot: final-submission.png
hotspots: 1 @ 2.2,21.9,95.5,13 | 2 @ 2.2,37.8,95.5,31.3 | 3 @ 2.2,72,95.5,28 | 4 @ - | 5 @ -
screenshot-phone: final-submission.phone.jpg
hotspots-phone: 1 @ 3.1,13.3,93.8,7.5 | 2 @ 3.1,22.5,93.8,38.9 | 3 @ 3.1,63.1,93.8,36.9 | 4 @ - | 5 @ -

**What this screen is for**
The **Final Submission** screen is the last step. It shows what is complete and what is missing, and sends the application for approval.

1. Look at the progress card at the top. It shows how many sections are complete. At 100% it says **Application Ready for Submission**.
2. Under **Section Completion Status**, a green tick means the section is complete and an orange mark means something is missing. Tap the pencil to open that step and fix it.
3. In the **Enclosures** card, check **Documents Uploaded** and **Missing Documents**. A missing document has an orange cross.
4. Read the **Terms and Conditions** and the **I hereby declare** points. When you submit, you accept them.
5. When the green **All sections completed!** box shows, press **Submit Registration**. You see **Registration submitted successfully!** and go back to the dealer list, where the application shows **In RM**.

> Tip: You can tap any part of the step bar at the top to go straight to that step.

## Common problems

- **"Please complete all ... incomplete section(s) before submitting."** Fix every section with an orange mark first.
- **"Error submitting registration"** Read the message. It names what the server found missing. Fix it and submit again.
- **There is no Submit Registration button.** The application was opened to view only. Open it with **Edit** on the dealer card. **Edit** shows only while the application is a **Draft** or **Rejected by RM**.
:::

::: section application-pdf
title: See the dealership application form
routes: /DealershipPDF/{dealerId:int?}
minutes: 2

**What this screen is for**
This screen shows the whole application as one **DEALERSHIP APPLICATION FORM**, for SPIC or for Greenstar. Use it to read the application or keep a printed copy.

1. On the dealer list, find the dealer card and tap **SPIC** or **GFL**.
2. The form opens with the company name and logo at the top.
3. Scroll down to read each part: **Dealer Details**, **Business Information**, **Bank Details**, **Proprietor / Partner Details**, **Sales Planning & Forecast**, **Investment Details** and more.
4. Near the end, see the credit limit, the **Recommended Credit Limit** and the **Attached Documents**.
5. To keep a copy, use the print option of your browser or device.

## Common problems

- **"Credit Limit Consent: GIVEN" at the top.** The dealer gave no credit limit details, so the credit limit is 0.
- **There is no GFL button.** The dealer is registered only for SPIC.
:::

::: section entry-info
title: Lucky draw entry details
routes: /EntryInfo
minutes: 1
screenshot: entry-info.png
screenshot-phone: entry-info.phone.jpg

**What this screen is for**
The **Entry Information** screen shows one purchase entry for the lucky draw. This screen is still being built: it shows sample values, not real entries.

1. At the top, see the entry number and its status.
2. In the entry card, see the **Date**, **Dealer Name**, **Source**, **Location**, **Product** and **Purchase Amount**.
3. Under **LUCKY DRAW INFO**, see if the entry is eligible and how far the current cycle has reached.
4. Under **Verification**, see the **Invoice Number** and whether the entry is counted.
5. Press the back arrow at the top to go back.

## Common problems

- **The values never change.** This screen shows sample values only for now.
:::
