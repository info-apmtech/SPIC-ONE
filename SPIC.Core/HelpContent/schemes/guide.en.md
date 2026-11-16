---
module: schemes
title: Schemes and lucky draw
icon: bi-gift
order: 104
category: Sales
color: #d97706
example: DEMO Scheme · 45 / 100 entries
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

A scheme rewards dealers and farmers who buy SPIC products. Each purchase gives a lucky draw entry. On the draw date, winners are picked and their prize is shown in the app.

- create | icon: bi-pencil-square | color: #d97706
  title: The scheme team sets it up
  text: Name, places, product rules, prize and draw dates.
- join | icon: bi-qr-code-scan | color: #0d9488
  title: Dealers and farmers take part
  text: Scan the product QR code. Each purchase gives 1 entry.
- win | icon: bi-trophy | color: #7c3aed
  title: The lucky draw picks winners
  text: Winners see a message, the prize and how to get it.
:::

::: section flow
title: How a scheme moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- team | type: lane | color: #d97706 | roles: Admin, CorporateAdmin
  title: Scheme team
  sub: Office staff
- people | type: lane | color: #0d9488 | roles: Dealer, Farmer
  title: Dealers and farmers
  sub: Scheme members
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: review | screen: add-scheme
  title: Create the scheme
  status: Draft
  text: Fill the **Create Scheme** form with the places, product rules, prize and draw settings, then press **Next**.
- review | lane: team | col: 2 | icon: bi-clipboard-check | color: #d97706 | next: scan | screen: review-scheme
  title: Review and start
  status: Active
  text: Check every detail on **Review Your Scheme**, then press **Active Scheme** to start it.
- scan | lane: people | col: 3 | icon: bi-qr-code-scan | color: #0d9488 | next: draw | screen: lucky-draw
  title: Buy and scan
  status: Ticket Activated
  text: Scan the product QR code and choose the products bought. Each purchase gives 1 lucky draw entry.
- draw | lane: team | col: 4 | icon: bi-ticket-perforated | color: #2563eb | next: announce | screen: scheme-overview
  title: Lucky draw
  status: Selected
  text: On the draw date, winners are picked from all entries. The draw is **Automatic** or **Manual**, as set in the scheme.
- announce | lane: people | col: 5 | icon: bi-megaphone | color: #7c3aed | next: prize | screen: winner-announcement
  title: Winner is told
  status: Notified
  text: The winner sees **Congratulations!** with the prize and the scheme name.
- prize | lane: people | col: 6 | icon: bi-gift | color: #16a34a | screen: winner-details
  title: Get the prize
  status: Prize Distributed
  text: **Winner Details** shows the prize, its status and the date it was given.
- not-won | lane: people | col: 4 | icon: bi-emoji-smile | color: #ea580c | kind: exception | from: draw | to: scan
  title: Not picked this time
  status: Better Luck Next Time!
  text: Keep buying and scanning. More entries give more chances in the next draw.
:::

::: section statuses
title: Every status you can see
kind: statuses

- draft | color: #64748b
  label: Draft
  text: The scheme is saved but not published. Dealers and farmers cannot see it.
- active-draft | color: #d97706
  label: ACTIVE DRAFT
  text: Shown on the review screen before you start the scheme.
- active | color: #16a34a
  label: Active
  text: The scheme is running. On an entry, it means the entry can still win in this cycle.
- participating | color: #0d9488
  label: Participating
  text: The dealer or farmer is taking part in the scheme.
- verified | color: #0891b2
  label: Verified
  text: The purchase or the dealer has been checked.
- ticket | color: #2563eb
  label: Ticket Activated
  text: A new lucky draw entry was added for your purchase.
- used | color: #64748b
  label: Used
  text: The entry has already gone into a draw.
- expired | color: #475569
  label: Expired
  text: The entry can no longer win.
- selected | color: #7c3aed
  label: Selected
  text: Picked as a winner in the draw.
- notified | color: #d97706
  label: Notified
  text: The winner has been told.
- claimed | color: #16a34a
  label: Claimed
  text: The winner has taken the prize.
- distributed | color: #16a34a
  label: Prize Distributed
  text: The prize has been given to the winner.
- completed | color: #16a34a
  label: Completed
  text: The draw on this date is finished.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role uses.

- team | color: #d97706 | roles: Admin, CorporateAdmin
  title: Scheme team
  - Create and review schemes
  - Start a scheme
  - Follow dealers, entries and winners
- dealer | color: #0d9488 | roles: Dealer
  title: Dealer
  - Scan products and add entries
  - See your entries and progress
  - See winners and your prize
- farmer | color: #7c3aed | roles: Farmer
  title: Farmer
  - Scan products you bought
  - See your entries
  - See winners and your prize
:::

::: section faq
title: Problems and answers
kind: faq

- how-to-win | popular: true
  q: How do I get a chance to win?
  a: Each purchase gives you 1 entry. Entries are counted in cycles of 100. Reach 100 entries to unlock the **Grand Draw**. See **How It Works** on the lucky draw page.
- entries-count | popular: true
  q: How many entries do I have?
  a: Open **Lucky Draw** and press **View All Entries** (on a phone, **View Entries**). The **TOTAL ENTRIES** card shows your active entries in this cycle.
- not-won
  q: I see "Better Luck Next Time!"
  a: You were not picked in this draw. Keep scanning your purchases. More entries give you more chances next time.
- prize
  q: I won. How do I get my prize?
  a: Open **Winner Details**. **Prize Information** shows the prize and its **Status**. For help, press **Contact Support** in the **Need Help?** box.
- change-scheme
  q: Can I change a scheme before it starts?
  a: Yes. On **Review Scheme**, use the pencil to edit a product rule or the bin to delete it. Press **Save Draft** to keep it unpublished until it is ready.
:::

::: section scheme-list
title: See all schemes
routes: /Schemes
minutes: 2
tour: 1 > [data-help=sch-stats] | 2 > [data-help=sch-filters] | 3 > [data-help=sch-grid] | 4 > [data-help=sch-view] | 5 > [data-help=sch-create]
screenshot: scheme-list.png
hotspots: 1 @ 2.2,14.4,95.5,11.5 | 2 @ 2.2,28.7,95.5,12.2 | 3 @ 2.2,43.8,95.5,56.2 | 4 @ 4.2,85.6,42.7,4.8 | 5 @ 81.5,5.5,16.3,5.3
screenshot-phone: scheme-list.phone.jpg
hotspots-phone: 1 @ 3.1,10.1,93.8,13.4 | 2 @ 3.1,25.2,93.8,21.2 | 3 @ 3.1,48.1,93.8,51.9 | 4 @ 8.5,94.7,83.1,3 | 5 @ 3.1,6,93.8,3

**What this screen is for**
The **Schemes** screen lists every lucky draw scheme, with totals at the top. Open it from the side menu with **Scheme List**.

1. Look at the cards at the top: **Total Schemes**, **Overall Dealer**, **Participating dealer** and **Winners**.
2. Type in **Search Schemes....** to find a scheme. Use the lists to choose the type (**Both**, farmer or dealer), the status (**All Status**) and the state (**All States**).
3. Each scheme card shows the name, who it is for, the status (for example **Active**), the dates, the state and district, the **Participating dealer** and **Winners** counts and **THE GRAND REWARD**.
4. Press **View Details** on a card to open its **Scheme Overview**.
5. Press **Create Scheme** to make a new scheme.

## Common problems

- **I cannot find a scheme.** Clear the search box and set the lists back to **Both**, **All Status** and **All States**.
- **Scheme List is not in my menu.** Your designation does not have access to schemes. Ask your admin.
:::

::: section add-scheme
title: Create a scheme
routes: /AddScheme
minutes: 5
tour: 1 > [data-help=sch-add-type] | 2 > [data-help=sch-add-basic] | 3 > [data-help=sch-add-rules] | 4 > [data-help=sch-add-prize] | 5 > [data-help=sch-add-draw] | 6 > [data-help=sch-add-next]
screenshot: add-scheme.png
hotspots: 1 @ 75.8,4.8,22,4.9 | 2 @ 2.2,12.6,95.5,35.9 | 3 @ 2.2,56.2,95.5,43.8 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: add-scheme.phone.jpg
hotspots-phone: 1 @ 3.1,6,93.8,3.6 | 2 @ 3.1,10.7,93.8,55 | 3 @ 3.1,70.1,93.8,29.9 | 4 @ - | 5 @ - | 6 @ 71.2,92.1,24.7,3

**What this screen is for**
Use the **Create Scheme** screen to set up a new lucky draw scheme: who it is for, where it runs, which products count, the prize and when the draw happens.

1. At the top, choose who the scheme is for: **Both**, **Farmer** or **Dealer**.
2. Under **Scheme Type & Basic Info**, enter the **Scheme Name** and **Draw points**, and choose the **State**, **District** and **Dealer**. Pick the **Start Date** and **End Date**. The **Scheme Status** box shows **Draft - Scheme is not published**.
3. Under **Add Product Rule**, choose the **Category** and **Product**, enter the **Dealer Quantity** and **Farmer Quantity**, and press **+ Add Rule**. Each rule appears in the **Product Rules List**. Use the bin to delete a rule or the pencil to edit it.
4. Under **Winner Prize Details**, enter the **Prize Name** and **Prize Description**, and tap **Click to upload image** to add a prize photo.
5. Under **Lucky Draw Configuration**, choose **Automatic** or **Manual**, pick the **Frequency** (**Daily**, **Weekly**, **Monthly** or **Custom**), and set the **Date** and **Draw Time**.
6. Press **Save Draft** to keep it for later, or **Next** to review the scheme.

> Tip: The prize name, description and photo are shown to dealers on the scheme page, in the winner message and in the draw announcement.

## Fields explained

| Field | What to enter |
|---|---|
| **Scheme Name** | A short name everyone will see. |
| **Draw points** | Points a member needs to qualify for the draw. |
| **State**, **District**, **Dealer** | Where the scheme runs. You can choose more than one. |
| **Dealer Quantity**, **Farmer Quantity** | How much of the product a dealer or a farmer must buy for the rule. |
| **Prize Image** | One JPG or PNG photo, up to 5 MB. |
| **Draw Time** | The time of day the draw happens. |

## Common problems

- **The photo does not upload.** Use a JPG or PNG under 5 MB.
- **I want to go back a step.** Press **Previous Step**.
:::

::: section review-scheme
title: Review and start a scheme
routes: /ReviewScheme
minutes: 3
screenshot: review-scheme.png
screenshot-phone: review-scheme.phone.jpg

**What this screen is for**
After **Next** on the create form, the **Review Scheme** screen shows every detail of the new scheme in one place, so you can check it before it starts.

1. Read the banner **Review Your Scheme**.
2. Under **Scheme Details**, check **APPLICABLE FOR:**, the status (**ACTIVE DRAFT**), the scheme name, the date it was created and the description.
3. Check the **Start Date** and **End Date** under **Campine Time Line**, and the prize card next to it.
4. Check the **Product Rules** table. Use the pencil to edit a rule or the bin to delete it.
5. Check **Target Segment** (**State**, **District**, **Dealers**) and the **Review Checklist** (**Draw Points**, **Product Points**, **Draw Mode**, **Frequency**, **Draw Date** and **Draw Time**).
6. Press **Save Draft** to keep it unpublished, or **Active Scheme** to start the scheme.

> Careful: After you press **Active Scheme**, dealers and farmers can see the scheme and start adding entries.

## Common problems

- **A detail is wrong.** Go back to the create form and correct it before you press **Active Scheme**.
- **I am not ready to start.** Press **Save Draft**. The scheme stays a draft.
:::

::: section scheme-overview
title: Follow a scheme
routes: /SchemeOverview
minutes: 3
screenshot: scheme-overview.png
screenshot-phone: scheme-overview.phone.jpg

**What this screen is for**
The **Scheme Overview** screen shows how one scheme is doing: who takes part, how many entries there are, who won and every past draw. Open it with **View Details** on the scheme list.

1. At the top, see the scheme name, who it is for, the status (for example **Active**), the **States** and the **Duration**.
2. Look at the cards: **Participating dealer**, **Total Entries** and **Winners**.
3. On the **Overview** tab, see the grand reward and the **Draw Setting**: **Draw Mode**, **Frequency**, **Next Draw Date** and **Last Draw Date**.
4. On the **Participating dealer** tab, search or choose a state to see each member with their **Points** and **Entries**. Tap a card to see more.
5. On the **Winners** tab, see each winner with the status (**Selected**, **Notified** or **Claimed**), the **Prize** and the **Draw Date**. Tap a card to see more.
6. On the **Draw History** tab, see every past draw with its date, **Completed**, the prize and how many winners were selected.

## Common problems

- **The Winners tab is empty.** No draw has been held for this scheme yet. Check the **Next Draw Date** on the **Overview** tab.
- **A member is missing.** Choose **All States** and clear the search box.
:::

::: section scan-qr
title: Scan a product QR code
routes: /qr-scanner
minutes: 1
screenshot: scan-qr.png
screenshot-phone: scan-qr.phone.jpg

**What this screen is for**
Use the **QR Scanner** to scan the QR code on a SPIC product you bought. This is how a purchase is counted for the lucky draw.

1. Hold the phone over the QR code on the product. Keep the code inside the square frame.
2. If it is dark, tap the lightning icon for the flash. Tap the camera icon to switch cameras.
3. If the code is small or far away, slide the zoom bar towards **+**.
4. To scan a code from a photo you already have, tap the picture icon.
5. Press **View Entries** to see your lucky draw entries.

## Common problems

- **The code is not read.** Clean the camera, turn on the flash and hold the phone still, about a hand's length away.
- **The camera does not open.** Allow the app to use the camera in your phone settings.
:::

::: section scan-product
title: Enter the products you scanned
routes: /Scanproduct
minutes: 2
screenshot: scan-product.png
screenshot-phone: scan-product.phone.jpg

**What this screen is for**
After a scan, this screen shows the company and its products. Enter how many of each product you bought and submit.

1. Check the company card at the top: name, **VERIFIED**, address, phone and email.
2. Under **Products**, find each product you bought.
3. Press **+** to add one. Press **−** to remove one. The number between them is the quantity.
4. Press **Submit**.
5. To scan again, tap the back arrow next to **Back To Scan**.

## Common problems

- **The quantity does not go below zero.** That is normal. Zero means you did not buy that product.
- **The wrong company is shown.** Go **Back To Scan** and scan the right QR code.
:::

::: section select-products
title: Choose purchased products from a dealer
routes: /SelectPurchasedProducts
minutes: 2

**What this screen is for**
The **Select Purchased Products** screen shows the dealer you bought from, your lucky draw progress and the products the dealer sells. Choose the products you bought.

1. Check the dealer card: name, **VERIFIED**, address, phone and email.
2. See **Current Progress**: your **Total Lucky Draw Entries** and how many more you need for the grand draw.
3. Under **Available Products**, find each product you bought.
4. Use **+** and **−** next to **Qty** to set how many you bought.

## Common problems

- **I do not see my product.** The dealer may not sell it under this scheme. Ask the dealer.
- **This is the wrong dealer.** Go back and scan the right dealer's QR code.
:::

::: section lucky-draw
title: My lucky draw
routes: /Luckydraw
minutes: 2
tour: 1 > [data-help=sch-ld-entry] | 2 > [data-help=sch-ld-prize] | 3 > [data-help=sch-ld-progress] | 4 > [data-help=sch-ld-how] | 5 > [data-help=sch-ld-actions]
screenshot: lucky-draw.png
hotspots: 1 @ 4.8,17.9,90.5,16.1 | 2 @ 4.8,36.4,90.5,18.8 | 3 @ 4.8,57.6,90.5,35.2 | 4 @ 4.8,95.2,90.5,4.8 | 5 @ -
screenshot-phone: lucky-draw.phone.jpg
hotspots-phone: 1 @ - | 2 @ 7.2,27.3,85.6,25.3 | 3 @ 7.2,53.8,85.6,20.8 | 4 @ 7.2,75.7,85.6,7.8 | 5 @ -

**What this screen is for**
After your purchase is verified, the **Lucky Draw** page shows **You're in the Lucky Draw!**, the prize you can win and how close you are to the grand draw.

1. See the new entry: **+1 Lucky Draw Entry**, **Ticket Activated**, the **Entry ID** and the date.
2. See the **Grand Prize** with its estimated value, and the **Previous Winners**. Press **View All** to see more winners.
3. Under **CURRENT CYCLE PROGRESS**, see your entries out of 100, the **Next Milestone** and the **Grand Draw**.
4. Read **How It Works**: each purchase gives 1 entry, and 100 entries unlock the grand draw.
5. Press **Scan Another Product** to add more entries, or **View All Entries** to see the list. On a phone the buttons are **Scan QR** and **View Entries**.

## Common problems

- **My entry count did not go up.** The entry is added only after the purchase is verified. Check again later in **View All Entries**.
- **What is the GOLD MILESTONE?** It is the 100-entry mark that unlocks the **Grand Draw**.
:::

::: section my-entries
title: See all my entries
routes: /LuckyDrawList
minutes: 2
screenshot: my-entries.png
screenshot-phone: my-entries.phone.jpg

**What this screen is for**
The **Over All Entries** screen lists every lucky draw entry you have in this cycle, with the products and points for each.

1. The **TOTAL ENTRIES** card shows how many active entries you have, out of 100, with the 25, 50, 75 and **GOLD MILESTONE** marks.
2. Under **Your Entries List**, choose a tab: **All**, **Active**, **Used** or **Expired**.
3. Each card shows the entry number, its status, the date, the products with **No.of Products**, **Total Products** and **Earned Points**.
4. Press **View Details** on a card to see that entry.
5. Tap the back arrow to return to the lucky draw page.

## Common problems

- **The list is empty.** You have no entries in this tab. Choose **All**.
- **Some entries show Expired.** They can no longer win. Keep scanning new purchases to add entries.
:::

::: section winner-announcement
title: Winner message
routes: /WinnerPopUp
minutes: 1
screenshot: winner-announcement.png

**What this screen is for**
When you win, a **Congratulations!** message opens: **You are a Lucky Draw Winner**. It shows your prize and the scheme.

1. Read the prize name and the scheme name.
2. Press **View Details** to open **Winner Details**.
3. Close the message to go back to the lucky draw page.

## Common problems

- **I closed the message by mistake.** Open **Winner Details** from the side menu.
:::

::: section winner-details
title: My prize details
routes: /WinnerDetails
minutes: 2
screenshot: winner-details.png
screenshot-phone: winner-details.phone.jpg

**What this screen is for**
The **Winner Details** screen shows your lucky draw prize and how it will reach you.

1. At the top, see **You are a Winner** and the **Draw ID**.
2. The **Draw Summary** shows the **Participants**, **Winners** and **Entries** in that draw.
3. **Winner Information** shows the **Winner Name**, **Type**, **Dealer Code**, **Mobile Number** and **Points Earned**.
4. **Prize Information** shows the **Prize Name**, **Prize Value**, **Status** (for example **Prize Distributed**) and **Distributed Date**.
5. **Scheme Information** and **Activity Information** show the scheme, the **Draw Date**, who announced it and when.
6. If you see **Better Luck Next Time!**, press **Scan QR** to add entries or **View Entries** to see them.
7. For any question about your prize, press **Contact Support** in the **Need Help?** box.

## Common problems

- **The mobile number is wrong.** Press **Contact Support** so the team can reach you.
- **The prize is not given yet.** Check the **Status** under **Prize Information**, or press **Contact Support**.
:::

::: section scheme-winners
title: See all scheme winners
routes: /CelebrationWinners
minutes: 1
screenshot: scheme-winners.png
screenshot-phone: scheme-winners.phone.jpg

**What this screen is for**
The **Scheme Winners** screen shows the lucky draw winners from past announcements.

1. The banner **Celebrating Our Winners!** shows the **Total Winners Announced**.
2. Use the lists to choose a scheme (**All Schemes**), the year, the type (**All Types**) and the place (**All Locations**).
3. Choose a tab: **All Winners**, **Dealer Winners** or **Farmer Winners**.
4. Each card shows the winner, **Dealer** or **Farmer**, the scheme, the place, the **Prize** and the **Draw Date**.
5. Press **View Details** to see more about a winner.

## Common problems

- **I do not see a winner.** Choose **All Schemes**, **All Types** and **All Locations**, and check the year.
:::
