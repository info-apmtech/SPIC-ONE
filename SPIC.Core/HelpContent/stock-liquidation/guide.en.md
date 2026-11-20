---
module: stock-liquidation
title: Stock and liquidation
icon: bi-boxes
order: 106
category: Sales
color: #db2777
example: DEMO-INV-0420 · Urea 10 MT
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Follow SPIC fertiliser after it leaves the company. See which sales the dealers have not yet acknowledged, how much stock lies with wholesalers and retailers, how old it is, and how fast it is sold on. The figures come from IFMS, the government fertiliser system.

- ack | icon: bi-hourglass-split | color: #db2777
  title: Pending acknowledgement
  text: See company and dealer sales that the buyer has not yet confirmed in IFMS.
- stock | icon: bi-boxes | color: #2563eb
  title: Stock and ageing
  text: See how much stock lies with each dealer and how many days it has been there.
- liquidation | icon: bi-graph-up-arrow | color: #16a34a
  title: Liquidation
  text: See how fast dealers sell their stock, and which dealers are slow.
:::

::: section flow
title: How stock moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- company | type: lane | color: #db2777
  title: SPIC
  sub: Company sales
- dealers | type: lane | color: #7c3aed
  title: Dealers
  sub: Wholesalers and retailers, in IFMS
- admin | type: lane | color: #374151 | roles: Admin
  title: Admin
  sub: Loads the IFMS files
- staff | type: lane | color: #0d9488 | roles: Director, AVP, SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: Sales staff
  sub: Watch the reports for their area
- sale | lane: company | col: 1 | icon: bi-receipt | color: #db2777 | next: ack | screen: pending-ack
  title: Company sale
  status: New
  text: SPIC sells fertiliser to a wholesaler. The invoice is entered in IFMS. Wholesalers also sell to retailers in the same way.
- ack | lane: dealers | col: 2 | icon: bi-clipboard-check | color: #16a34a | next: upload | screen: ack-cycle
  title: Dealer acknowledges
  status: Ack
  text: The buyer confirms in IFMS that the stock arrived. The days from invoice to receipt are the acknowledgement cycle.
- upload | lane: admin | col: 3 | icon: bi-file-earmark-arrow-up | color: #374151 | next: stock | screen: file-upload
  title: Load the IFMS reports
  text: The IFMS reports come into SPIC ONE every day, by the nightly import or by the Admin's Excel upload.
- stock | lane: dealers | col: 4 | icon: bi-boxes | color: #2563eb | next: sell | screen: stock-report
  title: Stock with the dealer
  status: Fresh
  text: Acknowledged stock lies with the wholesaler or retailer. Its age counts from the day it was acknowledged.
- sell | lane: dealers | col: 5 | icon: bi-cart-check | color: #16a34a | next: review | screen: liquidation-cycle
  title: Sold on (liquidated)
  status: Active
  text: The dealer sells the stock on to retailers and farmers. Sales lower the balance stock.
- review | lane: staff | col: 6 | icon: bi-bar-chart | color: #0d9488 | screen: stock-details
  title: Watch and follow up
  text: Staff check stock, sales and ageing for their area and follow up with dealers who are slow.
- pending | lane: staff | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: sale | to: ack | screen: pending-ack
  title: Not acknowledged in time
  status: Overdue
  text: If the buyer does not acknowledge, the sale stays pending and gets older. Staff ask the dealer to acknowledge it in IFMS.
- aged | lane: staff | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: stock | to: sell | screen: ageing-report
  title: Old stock not moving
  status: Long Aged
  text: Stock that is not sold gets older and shows Long Aged or Critical. Staff help the dealer sell it first.
:::

::: section statuses
title: Every status you can see
kind: statuses

- new | color: #64748b
  label: New
  text: IFMS status. The sale is entered but the buyer has not acknowledged it yet.
- ack | color: #16a34a
  label: Ack
  text: IFMS status. The buyer has acknowledged that the stock arrived.
- completed | color: #16a34a
  label: Completed
  text: The sale is acknowledged. It is no longer pending.
- latest | color: #2563eb
  label: Latest
  text: Waiting for acknowledgement for 0 to 10 days.
- overdue | color: #dc2626
  label: Overdue
  text: Waiting for acknowledgement for 21 days or more.
- consent | color: #9333ea
  label: Consent of Buyer
  text: IFMS shows the sale is waiting for the buyer's consent.
- fast | color: #16a34a
  label: Fast
  text: Acknowledged within 0 to 2 days of the invoice.
- normal | color: #d97706
  label: Normal
  text: Acknowledged within 3 to 5 days of the invoice.
- delayed | color: #ea580c
  label: Delayed
  text: Acknowledged 6 to 10 days after the invoice.
- pending-ack | color: #64748b
  label: Pending ACK
  text: Stock report. The stock has no acknowledgement in IFMS yet, so it has no age.
- fresh | color: #059669
  label: Fresh
  text: Stock 0 to 30 days old.
- medium | color: #34d399
  label: Medium
  text: Stock 31 to 90 days old.
- slow | color: #f59e0b
  label: Slow Moving
  text: Stock 91 to 180 days old.
- long | color: #ef4444
  label: Long Aged
  text: Stock 181 to 364 days old.
- active | color: #16a34a
  label: Active
  text: Liquidation. The dealer sold this stock within the last 45 days.
- monitoring | color: #d97706
  label: Monitoring
  text: Liquidation. No sale for 46 to 75 days. Keep watching.
- critical | color: #b91c1c
  label: Critical
  text: The worst group on each screen. Pending acknowledgement: 11 to 20 days. Acknowledgement cycle: more than 10 days. Stock age: 365 days or more. Liquidation: no sale for more than 75 days.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- staff | color: #0d9488 | roles: SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: Sales staff
  - See pending acknowledgements, stock, ageing and liquidation for your own state, region or HQ
  - Follow up with dealers who are slow to acknowledge or sell
  - Download the lists to Excel
- leaders | color: #db2777 | roles: Director, AVP
  title: Director and AVP
  - See the figures for all states
  - Compare states, dealers and products
- admin | color: #374151 | roles: Admin
  title: Admin
  - Upload the IFMS Excel reports every day
  - See the figures for all states
  - Send the State-wise Stock Reconciliation by email
- dealers | color: #7c3aed | roles: Dealer
  title: Wholesalers and retailers
  - Acknowledge received stock in IFMS
  - Sell the stock on quickly
:::

::: section faq
title: Problems and answers
kind: faq

- overdue | popular: true
  q: A sale shows Overdue. What do I do?
  a: The buyer has not acknowledged the stock in IFMS for 21 days or more. Ask the dealer to acknowledge it in IFMS. After the next IFMS update it shows **Completed**.
- old-figures | popular: true
  q: The figures look old or a report is empty
  a: The reports show the last IFMS data loaded into SPIC ONE. If today's files are not loaded yet, ask the Admin to upload them on **File Upload**, then press **Refresh Data**.
- my-area
  q: I cannot choose another state or region
  a: Most staff see only their own state, region or HQ. Director, AVP and Admin see all states.
- no-rows
  q: I see "No records found." or "No Data Available"
  a: Your filters may be too narrow. Press **Clear All**, then **Search**.
- pdf
  q: The PDF and Send Mail buttons are grey
  a: These are not ready yet. Use **Excel** to download the list.
:::

::: section pending-ack
title: See pending acknowledgements
routes: /CompanySales
minutes: 3
tour: 1 > [data-help=sl-pa-filters] | 2 > [data-help=sl-pa-cards] | 3 > [data-help=sl-pa-tabs] | 4 > [data-help=sl-pa-table] | 5 > [data-help=sl-pa-export]
screenshot: pending-ack.png
hotspots: 1 @ 2.2,13.8,95.5,32.6 | 2 @ 2.2,49.3,95.5,42.5 | 3 @ - | 4 @ - | 5 @ 70,5.6,8,4.5

**What this screen is for**
The **Pending Acknowledgement** screen shows company, wholesaler and retailer sales and whether the buyer has acknowledged them in IFMS. Open it from the menu with **Pending Acknowledgement**.

1. Open **Filters** and choose a **Date Range**, **State**, **District**, **Dealer Type**, **Dealer / Agency Name**, **Product** or **Status (By Age)**. Press **Search**. **Clear All** removes them.
2. Read the three cards: the total, **Company Sales** and **Wholesaler Sales**. Each shows **Completed**, **Latest**, **Critical**, **Overdue** and **Consent of Buyer**. The chart below compares the states.
3. Choose a tab: **All**, **Company Sales**, **Wholesaler Sales** or **Retailer Sales**. Type an invoice or dealer name in the search box to find a sale.
4. In the table, see the **Age Status** and the **Pending Ack Age (Days)**. Press **View** to see the full **Record Details**.
5. Press **Excel** to download the list.

> Tip: Sort by **Pending Ack Age (Days)** to see the oldest pending sales first, and call those dealers.

| Field | What it shows |
|---|---|
| **Age Status** | **Latest** 0 to 10 days, **Critical** 11 to 20 days, **Overdue** 21 days or more, or **Consent of Buyer**. |
| **Workflow Status** | The status from IFMS, for example **New** or **Ack**. |
| **Quantity (MT)** | Quantity in metric tonnes. |

## Common problems

- **"No records found."** Press **Clear All** in Filters, or clear the search box.
- **The tabs do not change the cards.** This is normal. The tabs change only the table and the Excel file.
- **A sale is still pending after the dealer acknowledged.** The new IFMS data is not loaded yet. Check again after the next update.
:::

::: section ack-cycle
title: Check how fast dealers acknowledge
routes: /Acknowledgement
minutes: 2
screenshot: ack-cycle.png
screenshot-phone: ack-cycle.phone.jpg

**What this screen is for**
The **Acknowledgement Cycle** screen shows how many days dealers took to acknowledge stock after the invoice. It shows only acknowledged sales.

1. Open **Filters** and choose **State**, **Region**, **Head Quarters**, **District**, **Product**, **Dealer Name**, **Status**, **Cycle Bucket** or the **Invoice Date From** and **Invoice Date To**. Press **Search**.
2. Read the cards: **Total Transactions**, **Fast (0-2 Days)**, **Normal (3-5 Days)**, **Delayed (6-10 Days)** and **Critical (> 10 Days)**.
3. Choose how to group the two top 5 lists: **State**, **Region**, **Head Quarters**, **District**, **Product** or **Dealer Name**.
4. Compare the **Top 5 Fast Performing** list with the **Top 5 Delayed** list.
5. In the **Acknowledgement Cycle List**, see **Ack Cycle (Days)** and the **Status** of each sale. Press **View** for the **Acknowledgement Details**.
6. Press **Excel** to download the list.

## Common problems

- **A top 5 list says "No data for the current filters."** A group needs at least 50 acknowledged sales to be ranked. Widen the filters.
- **"No acknowledged transactions found."** Press **Clear All**, then **Search**.
:::

::: section stock-report
title: See the stock with dealers
routes: /StockReport
minutes: 3
tour: 1 > [data-help=sl-sr-filters] | 2 > [data-help=sl-sr-cards] | 3 > [data-help=sl-sr-charts] | 4 > [data-help=sl-sr-search] | 5 > [data-help=sl-sr-table] | 6 > [data-help=sl-sr-export]
screenshot: stock-report.png
hotspots: 1 @ 2.2,12.7,95.5,41.4 | 2 @ 2.2,56.9,95.5,17.6 | 3 @ 2.2,77.4,95.5,22.6 | 4 @ - | 5 @ - | 6 @ 69.3,4.8,8.2,4.7

**What this screen is for**
The **Stock Report** screen shows how much stock lies with each wholesaler and retailer now, and how old it is.

1. Open **Filters** and choose **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying With**, **Product** or **Ageing Range (Days)**. Press **Search**.
2. Read the cards: **Total Stock (Current Stock)**, **Dealers Count**, **Average Ageing** and **High Ageing Stock**.
3. See the charts: **State-wise Stock Availability** compares this year with last year, and **Product-wise Stock Distribution (MT)** shows each product's share.
4. Type a dealer, state or product in the search box above the **Stock Report List**.
5. In the table, see the **Quantity (MT)**, who it is **Lying With**, the **Ageing in Days** and the **Status**. Press **View** for the full **Stock Details**.
6. Press **Excel** to download the list.

> Tip: Choose **Long Aged (181-364)** and **Critical (365+)** in **Ageing Range (Days)** to find old stock quickly.

## Common problems

- **"No Data Available".** Press **Clear All** in Filters, or clear the search box.
- **Ageing shows "--" and the status is Pending ACK.** Ageing counts only from an acknowledgement in IFMS. Stock without one has no age yet.
:::

::: section ageing-report
title: Find old and slow-moving stock
routes: /AgeingReport
minutes: 2
screenshot: ageing-report.png
screenshot-phone: ageing-report.phone.jpg

**What this screen is for**
The **Ageing Report** screen shows how long stock has been lying with dealers, so you can find slow-moving stock.

1. Open **Filters** and choose **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying with**, **Product** or **Ageing Range (Days)**. Press **Search**.
2. Read the cards: **Total Stock (MT)**, **Average Ageing (Days)**, **30-60 Days Stock (MT)** and **60+ Days Stock (MT)**.
3. In **State-wise Stock by Ageing (MT)**, point to a bar to see each age group for that state.
4. In **Ageing by Days (MT)**, see how much stock is in each age group.
5. In the **Ageing Report List**, see each dealer's **Ageing in Days** and **Status**. Press **View** for the **Stock Ageing Details**.
6. Press **Excel** to download the list.

## Common problems

- **"No records found."** Press **Clear All**, then **Search**.
- **Some old stock does not show.** Ageing counts only from an acknowledgement in IFMS.
:::

::: section liquidation-cycle
title: See how fast stock is sold
routes: /LiquidationCycle
minutes: 3
screenshot: liquidation-cycle.png
screenshot-phone: liquidation-cycle.phone.jpg

**What this screen is for**
The **Liquidation Cycle** screen shows how much stock each dealer had, how much they sold, and how long since their last sale.

1. Open **Filters** and choose **Date From**, **Date To**, **State**, **Region**, **HQ**, **District**, **Sub District**, **Dealer Name**, **Product** or **Financial Year**. Press **Apply Filters**.
2. Read the cards: **Total Stock (MT)**, **Liquidated (MT)** and **Balance Stock (MT)**.
3. Under **Dealer Liquidation Performance**, choose **Top 5 Performance**, **State-wise**, **All**, **Company Sales**, **Wholesalers** or **Retailers**.
4. Compare the fast list with the slow list, for example **Top 5 Fast Dealers** and **Top 5 Slow Dealers**.
5. In the **Liquidation Cycle List**, see **Stock (MT)**, **Ageing (Days)**, **Sales (MT)** and **Status**. Press **View** for the **Liquidation Details**.
6. Press **Excel** to download the list.

> Tip: Here, **Ageing (Days)** means the days since the dealer's last sale of that product.

## Common problems

- **"No records found."** Press **Clear All**, then **Apply Filters**.
- **I cannot choose another state.** Your area is fixed by your login. Director, AVP and Admin see all states.
:::

::: section stock-details
title: See daily stock and sales by state
routes: /StockDetails
minutes: 2
screenshot: stock-details.png
screenshot-phone: stock-details.phone.jpg

**What this screen is for**
The **Stock Details** screen gives one line per state with opening stock, supplies, sales and closing stock. It is updated every day.

1. At the top, see **Date as on** and the **Period**.
2. Open **Filters** and choose **From Date**, **To Date**, **Financial Year**, **State**, **Region**, **Head Quarters** or **Product**. Press **Search**.
3. Read the cards: **Total Stock**, **Total Sales**, **Closing Stock** and **Sales %**.
4. In the **Stock List**, read each state's **Opening Stock**, **Supplies**, **Total Stock**, **Sales**, **Total Sales**, **Closing Stock** and **Sales %**. The last line is the **Grand Total**.
5. Type a state name in the search box to find it.
6. Press **Excel** to download the list.

## Common problems

- **"No Data Available".** Press **Clear All**, then **Search**.
- **The column dates change.** The column headings show the dates of your chosen period.
:::

::: section product-stock
title: See stock of each product by state
routes: /ProductWiseStockAvailability
minutes: 2
screenshot: product-stock.png
screenshot-phone: product-stock.phone.jpg

**What this screen is for**
The **Product-wise Stock Availability** screen shows, for each state, how much of each product is in stock.

1. Open **Filters** and choose **State**, **Region**, **Head Quarters**, **Date From** or **Date To**. Press **Search**.
2. Read the cards: **Total States**, **Total Products**, **Total Quantity (MT)**, **Highest Stock State** and **Low Stock Alerts**.
3. In the **Stock Availability List**, each row is a state and each column is a product. **Total (MT)** is the state total.
4. Type a state name in the search box to find it. The last line is the **Grand Total**.
5. Press **Excel** to download the table.

## Common problems

- **"No Data Available".** Press **Clear All**, then **Search**.
- **Low Stock Alerts is high.** These states are below the stock limit. Plan supplies for them.
:::

::: section stock-reconciliation
title: Check the state-wise stock reconciliation
routes: /StateWiseGlobalStockReconciliation
minutes: 2
screenshot: stock-reconciliation.png
screenshot-phone: stock-reconciliation.phone.jpg

**What this screen is for**
The **State-wise Stock Reconciliation** screen matches the company's stock with the stock lying with wholesalers and retailers, state by state, for one day.

1. Choose the **As On Date:** and press **View**. Press **Today** to go back to today.
2. If a **Partial Report - Pending Uploads** box appears, some reports for that day are not loaded yet.
3. In the **Urea Stock** table, read each state's **Company Stock**, **Wholesaler Stock**, **Retail Point Stock**, stock in transit (GIT) and **Overall Stock**.
4. Scroll down to the **GFL Stock** table for stock by product and state.
5. Type a state name in the search box to find it.
6. Press **Export Excel** to download, or **Send Email** to send the report. Enter **To (recipient email)**, add **CC (optional)** if needed, and press **Send Email**.

## Common problems

- **"No Reconciliation Data Available".** The reports for that date are not uploaded. Choose another date, or ask the Admin to upload them. Admins can press **Go to File Upload**.
- **A column shows no figures.** That report is missing for the day. See the **Partial Report** box.
:::

::: section rankings
title: Top ranking districts, wholesalers and retailers
routes: /TopRankingDistrict, /TopRankingWholesalers, /TopRankingRetailers
minutes: 1
screenshot: rankings.png
screenshot-phone: rankings.phone.jpg

**What this screen is for**
The ranking screens list the districts, wholesalers and retailers that sold the most stock in the shortest time. Open **Top Ranking District**, **Top Ranking Wholesaler** or **Top Ranking Retailers** from the menu.

1. Open the ranking you need from the menu.
2. Read the cards at the top, such as the totals and **Avg Liquidation Cycle**.
3. In the list, rank 1 is the best. See the **State**, **Region**, **HQ**, **District** and **Sub-District** of each.

> Careful: These ranking screens are still being connected to live data. The figures and filters on them now are only examples. Use **Liquidation Cycle** for real figures.

## Common problems

- **The list does not change when I filter.** The screen is not connected to live data yet. Use **Top 5 Performance** on **Liquidation Cycle**.
:::

::: section file-upload
title: Upload the IFMS Excel reports
routes: /ExcelFormatFileUpload
roles: Admin
minutes: 3
tour: 1 > [data-help=sl-fu-grid] | 2 > [data-help=sl-fu-file] | 3 > [data-help=sl-fu-date] | 4 > [data-help=sl-fu-upload]
screenshot: file-upload.png
hotspots: 1 @ 2.2,12.7,95.5,87.3 | 2 @ 4.2,23.4,22.7,8.4 | 3 @ 28.4,24.1,18.7,7.7 | 4 @ 4.2,33.8,9.4,5.4
screenshot-phone: file-upload.phone.jpg
hotspots-phone: 1 @ 3.1,7.4,93.8,92.6 | 2 @ 8.5,13.7,83.1,4.9 | 3 @ 8.5,19.7,51.3,4.5 | 4 @ 63.8,21,25.7,3.1

**What this screen is for**
The Admin uses the **Excel Format File Upload** screen to load the daily IFMS reports. All the stock and liquidation screens read this data. Open it from the menu with **File Upload**.

1. Find the card for your report: **Retail Stocks**, **Company Sales**, **Sales and Receipt**, **Wholesale Sales**, **Wholesale Stock As On Today**, **State Wise Global Stock Reconciliation** or **Warehouse and RakePoint Global Stock**.
2. Under **Excel File**, choose the file you downloaded from IFMS.
3. If the card has a **Report Date**, check that it is the date of the report. It starts at today.
4. Press **Upload** and wait while it shows **Uploading...**. A message then shows the total rows and how many were inserted, updated and skipped.

> Tip: Each card is for one report. Choose the card whose name matches your IFMS report.

| Field | What to enter |
|---|---|
| **Excel File** | One .xlsx or .csv file, up to 100 MB. |
| **Report Date** | The date the report is for. Needed for Retail Stocks, Sales and Receipt, State Wise Global Stock Reconciliation and Warehouse and RakePoint Global Stock. |

## Common problems

- **"Please select a file first."** Choose the file on the same card, then press **Upload**.
- **"Only .xlsx and .csv files are supported."** Save the file as .xlsx or .csv and try again.
- **"The selected file exceeds the 100 MB upload limit."** Split the file into smaller files.
- **Upload Failed.** Read the message. Check that the file is the right report for that card, then try again.
:::
