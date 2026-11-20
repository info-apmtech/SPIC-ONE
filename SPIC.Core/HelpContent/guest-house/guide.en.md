---
module: guest-house
title: Guest house
icon: bi-house-door
order: 112
category: Facilities
color: #b45309
example: DEMO-GH01 · AC Deluxe Room
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

Book a room in a company guest house, pay for it if needed, and see all your bookings in one place. At the guest house, the Front Office checks you in, gives you a room number, checks you out and makes the bill.

- book | icon: bi-calendar-check | color: #b45309
  title: Book in 5 steps
  text: Choose the guest house, dates, room and guest details, then confirm.
- pay | icon: bi-wallet2 | color: #0d9488
  title: Free for employees
  text: Company employees book free. Dealers and others pay online with Razorpay.
- stay | icon: bi-door-open | color: #2563eb
  title: Front Office does the rest
  text: Check-in with room numbers, check-out, payment and the bill.
:::

::: section flow
title: How a booking moves, and who acts
kind: flow

Each row is a role and each box is a step. Your row is marked "You". Tap a step to see what happens there.

- booker | type: lane | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: Employee or dealer
  sub: The person who books
- frontoffice | type: lane | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: Front Office
  sub: Receptionist · Admin
- admin | type: lane | color: #dc2626 | roles: Admin, CorporateAdmin
  title: Guest house admin
  sub: Cancellations and masters
- book | lane: booker | col: 1 | icon: bi-house-door | color: #b45309 | next: pay | screen: choose-guest-house
  title: Choose and fill
  status: Pending Payment
  text: Choose the guest house, dates and room, and fill the guest details.
- pay | lane: booker | col: 2 | icon: bi-wallet2 | color: #0d9488 | next: checkin | return: book | screen: review-pay
  title: Review and pay
  status: Confirmed
  text: Check everything and press **Complete Booking**. Employees pay nothing. Others pay online. If the payment fails, start a new booking.
- checkin | lane: frontoffice | col: 3 | icon: bi-box-arrow-in-right | color: #2563eb | next: checkout | screen: check-in
  title: Check in
  status: CheckedIn
  text: The Front Office checks the ID proof and gives the room numbers.
- checkout | lane: frontoffice | col: 4 | icon: bi-box-arrow-right | color: #7c3aed | next: bill | screen: check-out
  title: Check out
  status: Completed
  text: The Front Office collects any payment due and checks the guest out. The room becomes free.
- bill | lane: frontoffice | col: 5 | icon: bi-receipt | color: #16a34a | screen: generate-bill
  title: Make the bill
  status: Billed
  text: The Front Office adds food and other charges and generates the tax invoice.
- cancel | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: pay | screen: cancellation-requests
  title: Cancel and refund
  status: Cancelled
  text: The booker asks to cancel from **My Bookings**. The admin approves or rejects. An approved refund is paid by hand to the original payment source.
:::

::: section statuses
title: Every status you can see
kind: statuses

- pending-payment | color: #d97706
  label: Pending Payment
  text: The booking is saved and waiting for the online payment.
- confirmed | color: #16a34a
  label: Confirmed
  text: Paid, or free for an employee. The room is held for you.
- booking-checked-in | color: #2563eb
  label: CheckedIn
  text: The guest is staying in the room now.
- completed | color: #0d9488
  label: Completed
  text: The guest has checked out.
- cancelled | color: #dc2626
  label: Cancelled
  text: The booking is cancelled, or its payment did not go through.
- paid | color: #16a34a
  label: Paid
  text: The money is received.
- pending | color: #d97706
  label: Pending
  text: The money is not received yet.
- failed | color: #dc2626
  label: Failed
  text: The online payment did not go through. Start a new booking.
- complimentary | color: #0d9488
  label: Complimentary
  text: Free booking for a company employee. No payment.
- pending-approval | color: #d97706
  label: Pending Approval
  text: Your cancellation request is waiting for the admin.
- approved | color: #16a34a
  label: Approved
  text: The admin approved the cancellation. The room is released.
- rejected | color: #475569
  label: Rejected
  text: The admin did not accept the cancellation. Your booking stays active.
- refund-manual | color: #7c3aed
  label: Cancelled - Refund to be Processed Manually
  text: The refund will be paid by hand to your original payment source.
- refund-completed | color: #16a34a
  label: Refund Completed
  text: The refund has been paid. Check your bank or card statement.
- no-refund | color: #64748b
  label: Cancelled - No Refund Due
  text: No refund is due under the cancellation policy.
- refund-failed | color: #dc2626
  label: Refund Failed
  text: The refund could not be completed. The team will contact you.
- available | color: #16a34a
  label: Available
  text: Front Office: the room is free.
- reserved | color: #d97706
  label: Reserved / Booked
  text: Front Office: a guest has booked the room and has not checked in yet.
- checked-in | color: #2563eb
  label: Checked-In
  text: Front Office: a guest is in the room now.
- not-billed | color: #64748b
  label: Not Billed
  text: The guest checked out but no bill is made yet.
- billed | color: #16a34a
  label: Billed
  text: The bill for this stay is made.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. You only see the screens your role can use.

- booker | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: Employee or dealer
  - Book a room
  - See your bookings and download the invoice
  - Ask to cancel and track the refund
- frontoffice | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: Front Office
  - Check guests in and give room numbers
  - Collect payment and check guests out
  - Generate and print bills
- admin | color: #dc2626 | roles: Admin, CorporateAdmin
  title: Guest house admin
  - Approve or reject cancellation requests
  - Add guest houses, room types and rooms
:::

::: section faq
title: Problems and answers
kind: faq

- free-booking | popular: true
  q: Why does my total show ₹0.00 (Complimentary)?
  a: Company employees book guest house rooms free of charge. Dealers and others pay online when they book.
- payment-failed | popular: true
  q: My payment failed, or I closed the payment window
  a: That booking cannot go through, and the room is released. Start a new booking from **Book a Room**. If money was taken from your account, it is refunded automatically.
- no-cancel
  q: I do not see the Cancel Booking button
  a: It shows only on upcoming bookings. A stay that is checked in cannot be cancelled online. Please contact the Front Office.
- refund-when
  q: When will I get my refund?
  a: After the admin approves your cancellation, the refund is paid by hand to your original payment source within 2 working days. Open **Refund Status** to follow it.
- no-rooms
  q: I see "No rooms available"
  a: All rooms are booked for those dates. Press **Change dates** and try other dates, or another guest house.
- early-checkin
  q: The Front Office cannot check me in
  a: Check-in is possible only from the check-in date until the stay ends, and only for a **Confirmed** booking.
:::

::: section choose-guest-house
title: Choose a guest house and your dates
routes: /GuestHouse, /GuestHouseBooking
minutes: 2

**What this screen is for**
This is where a booking starts. First you pick the guest house on **Choose Your Guest House**, then you pick your dates on **Guest House Booking** (Step 1 of 5).

1. In the menu, open **SDWA** and tap **Book a Room**. Each card shows a guest house with its address and phone number.
2. Press **Select** on the card of the guest house you want. The button shows the guest house name.
3. On **Guest House Booking**, choose the **Check-in Date** and **Check-in Time**.
4. Choose the **Check-out Date** and **Check-out Time**.
5. Read the **Important Information** box, then press **Check Availability**.

> Tip: Carry a valid ID proof (Aadhaar or Passport). It is needed at check-in.

## Common problems

- **"Check-out must be after Check-in."** Choose a later check-out date or time.
- **"No guest houses available".** Press **Retry**. If it still shows, contact the SDWA office.
- **I am sent back to Choose Your Guest House.** Pick a guest house first with **Select**, then choose the dates.
:::

::: section choose-room
title: Choose a room
routes: /Rooms
minutes: 2

**What this screen is for**
The **Available Rooms** screen (Step 2 of 5) shows the rooms that are free for your dates.

1. Look at the room cards. Each shows the room type, the price per night, how many rooms are available and how many guests are allowed.
2. Tap a room type at the top, or type in **Search**, to see only those rooms. **All** shows every room.
3. Choose the **Number of Rooms** you need.
4. If you need an extra bed, tick **Additional Bed** and choose the **No. of Additional Beds**. The extra bed has its own price per night.
5. Press **Book Now**.

> Tip: The **View Details** button is not active yet. All room details are on the card.

## Common problems

- **"No rooms available".** No room is free for those dates. Press **Change dates**.
- **"No matching rooms".** Press **Clear filters**.
- **"Up to ... rooms can be booked at a time for this room type."** Book the rest in a second booking.
:::

::: section guest-details
title: Fill the guest details
routes: /GuestDetails
minutes: 3

**What this screen is for**
On **Guest Details** (Step 3 of 5) you tell us who will stay. The **Booking Summary** on the right shows the price.

1. Enter the **Employee ID / Dealer Code** and the **Guest Name**.
2. Choose the **Company Name** if it is in the list, and enter the **Phone Number** and **Email ID**. For a dealer, the phone and email are filled from the dealer registration.
3. Enter the **Aadhaar / Passport Number** and choose the **Nationality**.
4. Choose the **Number of Persons**, **Number of Adults** and **Number of Children (below 12 years)**.
5. If you want, upload the ID proof in **Upload ID Proof (Optional)**.
6. Check the **Booking Summary**: room cost, **GST (5%)** and **Total**. Then press **Continue Review**.

| Field | What to enter |
|---|---|
| **Employee ID / Dealer Code** | Your employee ID or dealer code. Required. |
| **Guest Name** | The name of the person who will stay. Required. |
| **Phone Number** and **Email ID** | Contact details of the guest. Required. |
| **Nationality** | Indian, NRI or Foreigner. Required. |
| **Company Name** | The company for the bill and its GSTIN. Optional. |
| **Upload ID Proof (Optional)** | One PDF, JPG or PNG file, up to 5 MB. |

## Common problems

- **"Employee ID / Dealer Code is required to continue."** Fill every required field. The message names the missing one.
- **"Only PDF, JPG, or PNG files are allowed."** or **"File must be 5 MB or less."** Choose another file or take a smaller photo.
- **I want to change the room.** Press **Back** to return to **Available Rooms**.
:::

::: section review-pay
title: Review and complete the booking
routes: /BookingPreview, /Payment
minutes: 3

**What this screen is for**
On **Review Your Booking** (Step 4 of 5) you check everything. On **Payment** (Step 5 of 5) you confirm the booking. Company employees pay nothing. Dealers and others pay online with Razorpay.

1. On **Review Your Booking**, check the **Booking Information**, **Booking Summary** and **Guest Information**.
2. To change something, press **Edit Details**. If all is right, press **Proceed to Payment**.
3. On **Payment**, check the **Booking Summary**. Employees see **Complimentary (Employee)** and a total of ₹0.00. Others see **Online Payment**.
4. Press **Complete Booking**. For an online payment, the Razorpay window opens. Pay by UPI, card, net banking or wallet.
5. The **Booking Confirmed!** box shows your **Booking ID** and **Payment Status**. Press **View My Bookings**, or **New Booking** to book again.

> Careful: Do not close the payment window or leave the page while paying. If the payment fails or you close it, this booking cannot go through and you must start a new booking.

## Common problems

- **"No booking details found".** The booking was not started or has expired. Start again from **Book a Room**.
- **The button shows "Payment Failed".** Start a new booking. If money was taken, it is refunded automatically.
- **"This room was just booked by someone else for the selected dates. Please try again."** Choose the room or dates again.
- **"Your room hold has expired..."** You took too long to pay. Start a new booking.
:::

::: section my-bookings
title: See my bookings
routes: /MyBookings
minutes: 2
tour: 1 > [data-help=gh-mb-cards] | 2 > [data-help=gh-mb-tabs] | 3 > [data-help=gh-mb-list] | 4 > [data-help=gh-mb-list] | 5 > [data-help=gh-mb-new]
screenshot: my-bookings.png
hotspots: 1 @ 2.2,16.5,95.5,13.5 | 2 @ 2.2,32.8,33.1,6.3 | 3 @ 2.2,32.8,95.5,40.9 | 4 @ 2.2,32.8,95.5,40.9 | 5 @ 84.4,4.8,13.4,4.7
screenshot-phone: my-bookings.phone.jpg
hotspots-phone: 1 @ 3.1,18.3,93.8,24.4 | 2 @ 3.1,45.6,93.8,6.2 | 3 @ 3.1,45.6,93.8,40 | 4 @ 3.1,45.6,93.8,40 | 5 @ 3.1,10.1,35.7,5.3

**What this screen is for**
**My Bookings** lists every room booking you made, with counts at the top. Open it from the menu: **SDWA**, then **My Bookings / Cancellation**.

1. Look at the cards at the top: **Total Bookings**, **Upcoming**, **Completed** and **Cancelled**.
2. Tap a tab: **Upcoming**, **Completed** or **Cancelled**.
3. Each booking card shows the room type, the booking status, the payment status, the **Booking ID**, the guest house, the dates, **Guests**, **Rooms** and **Total Amount**.
4. Press **View Details** to open a booking. On an upcoming booking, press **Cancel Booking** to ask to cancel. On a cancelled booking, press **Refund Status**.
5. Press **New Booking** to book another room.

## Common problems

- **"No upcoming bookings".** You have no booking for today or later. Press **Book a room**.
- **"Could not load bookings".** Check your internet and press **Retry**.
- **A booking shows Cancelled and Failed.** Its online payment did not go through. Book again.
:::

::: section booking-details
title: See one booking and download the invoice
routes: /BookingDetails
minutes: 2

**What this screen is for**
**Booking Details** shows everything about one booking: the room, the dates, the payment, the status and the guest.

1. On **My Bookings**, press **View Details** on a booking.
2. See **Room Details** and **Check-in & Check-out**, with the number of nights, rooms and extra beds. The **Room Number** shows when it is known.
3. See **Payment Details**: room cost, **GST (5%)**, **Total Amount**, **Payment Method** and **Payment Status**.
4. See **Booking Status**. A cancelled booking also shows the **Cancellation Status**.
5. See **Guest Details**. If you uploaded an ID proof, press **View** or **Download**.
6. Press **Download Invoice** to save the tax invoice as a PDF.
7. Press **Back to My Bookings** to return.

## Common problems

- **There is no Download Invoice button.** It appears only when the payment is **Paid** and the invoice is ready.
- **"Booking not found."** You can open only bookings that you made.
:::

::: section cancel-booking
title: Cancel a booking
routes: /CancelBooking
minutes: 3

**What this screen is for**
Use **Cancel Booking** to ask for a cancellation. The admin checks your request. Your booking stays active until the admin approves it.

1. On **My Bookings**, press **Cancel Booking** on an upcoming booking.
2. Read the **Important Notice**. It shows the refund percentage you will get, based on the time left before check-in.
3. Under **Cancellation Reason**, choose a reason.
4. Check the **Refund Calculation**: **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)**, **Refund Amount**, **Refund Method** and **Estimated Time**.
5. Press **Continue Cancellation**. In **Confirm Cancellation?**, press **Cancel Booking**. Press **Keep My Booking** if you changed your mind.
6. **Cancellation Request Submitted** shows your **Cancellation ID** and **Pending Approval**. Press **Refund Status**, **View Booking** or **Go to My Bookings**.

> Careful: The refund is smaller the closer you are to check-in. Read the **Cancellation Policy** box first.

## Common problems

- **"Please select a reason for cancellation."** Choose a reason first.
- **"This booking is already checked in and cannot be cancelled online. Please contact the Front Office."** Speak to the Front Office.
- **"A cancellation request for this booking has already been submitted".** You asked already. Press **View Refund Status**.
:::

::: section refund-status
title: Track my refund
routes: /RefundStatus
minutes: 2

**What this screen is for**
**Refund Status** shows where your cancellation and refund are now.

1. On **My Bookings**, open the **Cancelled** tab and press **Refund Status**. You can also open it from **Cancel Booking** after you send the request.
2. At the top, see the **Total Refund Amount**, **Cancellation ID**, **Refund Method**, **Target Refund Date**, **Transaction Ref.** and **Current Status**.
3. Under **Refund Breakdown**, see **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)** and **Refund Amount**.
4. Under **Refund Tracking**, follow the steps: **Cancellation Requested**, then **Pending Admin Approval**, **Approved by Admin** or **Cancellation Rejected**, then the refund.
5. If the money has not reached you by the target date, contact support with the reference shown in the orange box.
6. Press **Back** to return to **My Bookings**.

## Common problems

- **My booking is not in the Cancelled tab.** It moves there only after the admin approves. Until then, open the booking's **Cancel Booking** screen and press **View Refund Status**.
- **"Cancellation Rejected".** Read the reason. Your booking stays active.
- **"No Refund Due".** Under the cancellation policy no money is returned, for example for a free employee booking.
:::

::: section front-office
title: See all rooms at the Front Office
routes: /FrontOffice
minutes: 2
tour: 1 > [data-help=gh-fo-cards] | 2 > [data-help=gh-fo-tabs] | 3 > [data-help=gh-fo-search] | 4 > [data-help=gh-fo-bill]
screenshot: front-office.png
hotspots: 1 @ 2.2,15.5,95.5,10.6 | 2 @ 2.2,29.5,39.2,5.9 | 3 @ 64.5,42,19.5,5.1 | 4 @ 76,4.8,12,4.5
screenshot-phone: front-office.phone.jpg
hotspots-phone: 1 @ 3.1,14.6,93.8,16 | 2 @ 3.1,31.9,93.8,5.1 | 3 @ 7.4,46.5,38.2,4.3 | 4 @ 3.1,8.3,32.9,4.3

**What this screen is for**
The **Front Office** screen shows every room, whether it is free, booked or occupied, and who has it. Open it from the menu: **SDWA**, then **Front Office**. A receptionist sees only the guest house of their own state.

1. Look at the cards at the top: **Total Rooms**, **Available**, **Reserved** and **Checked-In**.
2. Tap a tab: **All**, **Available**, **Reserved / Booked** or **Checked-In**.
3. Type a room number, booking ID or guest name in **Search rooms...**, or choose a guest house in **All Guest Houses**. Each row shows the room, its status, the booking and guest, the dates and the actions.
4. Press **Generate Bill** to make the bill for a guest who has checked out. Press **Refresh** to reload the rooms.

## Common problems

- **"You do not have permission to access the Front Office."** Ask your administrator for the Front Office permission.
- **"No rooms found".** Choose **All** and **All Guest Houses**, and clear the search.
:::

::: section check-in
title: Check a guest in
routes: /FrontOffice
minutes: 3
screenshot: check-in.png
screenshot-phone: check-in.phone.jpg

**What this screen is for**
When a guest arrives, check them in and give them their room numbers.

1. On **Front Office**, tap **Reserved / Booked** and find the booking.
2. Press **Check-In** in the **Actions** column.
3. In the **Check-In** box, check the **Booking ID**, **Guest Name**, **Room Type** and **Guest House**. If an ID proof is attached, press **View** to see it.
4. Check the **Number of Persons** and **Number of Extra Beds**, and correct them if needed.
5. Under **Assign Physical Room(s)**, tap the room numbers to give. Pick exactly as many rooms as were booked.
6. Press **Confirm Check-In**. The room now shows **Checked-In**.

> Careful: Check the guest's ID proof (Aadhaar or Passport) before check-in.

## Common problems

- **"This booking's check-in date has not arrived yet."** Check in on or after the check-in date.
- **"This booking's stay period has passed and it can no longer be checked in."** The stay dates are over.
- **"Please select exactly ... physical room(s) before confirming check-in."** Tap more or fewer room numbers.
- **"No physical rooms are currently free for the booked stay period."** Contact the booker or choose another room type.
- **"These rooms were just assigned to another overlapping stay. Please refresh and try again."** Press **Refresh** and pick other rooms.
:::

::: section check-out
title: Check a guest out and take payment
routes: /FrontOffice
minutes: 2
screenshot: check-out.png
screenshot-phone: check-out.phone.jpg

**What this screen is for**
When the guest leaves, make sure the payment is received, then check them out. The room becomes free again.

1. On **Front Office**, tap **Checked-In** and find the guest.
2. Press **Check-Out** in the **Actions** column.
3. In **Check-Out & Payment**, check the **Room Number**, dates, **Number of Persons**, **Extra Beds**, **Total Amount**, **Payment Method** and **Payment Status**.
4. If the **Payment Status** is **Pending**, collect the money and press **Confirm Payment Received**.
5. When it shows **Paid**, press **Confirm Check-Out**. The room shows **Available** again.
6. Now make the bill with **Generate Bill**.

## Common problems

- **"Payment must be received before check-out."** Press **Confirm Payment Received** first.
- **"Check-Out is only allowed for a booking that is currently checked in."** The guest is not checked in. Check the **Status**.
:::

::: section bills
title: Find completed stays and bills
routes: /GenerateBill
minutes: 2
tour: 1 > [data-help=gh-gb-filters] | 2 > [data-help=gh-gb-go] | 3 > [data-help=gh-gb-tabs] | 4 > [data-help=gh-gb-find]
screenshot: bills.png
hotspots: 1 @ 4.2,24.4,91.6,7.2 | 2 @ 40.1,27.2,8.3,4.4 | 3 @ 2.2,40.9,95.5,5.1 | 4 @ 77.3,52.6,18.1,5.1
screenshot-phone: bills.phone.jpg
hotspots-phone: 1 @ 6.9,18.7,86.2,23.7 | 2 @ 6.9,30.8,86.2,3.3 | 3 @ 3.1,47.8,93.8,3.3 | 4 @ 7.4,59.1,85.1,3.3

**What this screen is for**
The **Generate Bill** screen lists the stays that have checked out and the bills already made. Open it from the menu (**SDWA**, then **Generate Bill**) or from **Front Office**.

1. Under **Filters**, choose the **From Date** and **To Date**. The dates are check-out dates for stays and bill dates for bills.
2. Press **Search**. **Today** shows today only. **Refresh** reloads the list.
3. Tap **Completed Bookings** to see checked-out stays, or **Generated Bills** to see bills already made.
4. Type a booking ID or guest name in the search box. On a stay, the **Bill Status** shows **Not Billed** or **Billed**. Press **Generate Bill** or **View Bill**.

## Common problems

- **"From Date cannot be after To Date."** Change the dates.
- **A stay is not in the list.** Only checked-out stays appear, by their check-out date. Widen the dates.
:::

::: section generate-bill
title: Make a bill for a stay
routes: /GenerateBill
minutes: 4
screenshot: generate-bill.png
screenshot-phone: generate-bill.phone.jpg

**What this screen is for**
After check-out, make the tax invoice for the stay. Each stay can be billed only once.

1. On **Completed Bookings**, press **Generate Bill** on a stay that shows **Not Billed**. The **New Bill** screen opens.
2. Check the **Booking Summary**: **Booking**, **Guest**, **Company Name**, **GSTIN**, **Guest House**, **Room** and **Stay**.
3. Under **Bill Items**, the room line is filled for you. Enter the **Food Bill** and **Additional Beds** amounts if any.
4. For laundry or other charges, press **Add Charge**. Enter the description, **Qty**, **Rate**, **CGST %** and **SGST %**. **Remove** deletes an added line.
5. Under **Totals, Discount & Payment**, enter **Discount**, **Advance Payment** and **Round Off** if needed. Choose the **Payment Method**, tick **Payment received** and add **Remarks**.
6. Check **Balance Payable**, then press **Generate Bill**. The new bill shows on the **Generated Bills** tab.
7. Press **View** to see the invoice, then **Download PDF** or **Print**. **PDF** on the list downloads it directly.

> Careful: Check every amount before you press **Generate Bill**. A bill cannot be made again for the same stay.

| Field | What to enter |
|---|---|
| **Food Bill** | Quantity and rate of food served during the stay. |
| **Add Charge** | Any other charge, with its own CGST % and SGST %. |
| **Advance Payment** | Money already paid for this stay. It is filled in for you when known. |
| **Payment Method** | UPI, Card, Net Banking or Cash. A free employee stay also shows Complimentary. |

## Common problems

- **"Discount cannot exceed Grand Total."** or **"Advance Payment cannot exceed Grand Total."** Lower the amount.
- **"Balance Payable cannot be negative."** Check **Discount**, **Advance Payment** and **Round Off**.
- **"A bill has already been generated for this booking".** Open it with **View Bill** instead.
:::

::: section bill-list
title: See all generated bills
routes: /BillList
minutes: 1
screenshot: bill-list.png
screenshot-phone: bill-list.phone.jpg

**What this screen is for**
The **Bill List** screen shows every invoice made, newest first. The same bills are also on the **Generated Bills** tab of **Generate Bill**.

1. Look at the table: **Bill #**, **Booking**, **Guest**, **Guest House**, **Room**, **Bill Date**, dates, **Total**, **Balance** and **Payment**.
2. Press **View** to open an invoice. In it, press **Download PDF** or **Print**.
3. Press **PDF** to download an invoice straight away.
4. Press **Show more** to see more rows.
5. Press **Generate Bill** to make a new bill, or **Refresh** to reload.

## Common problems

- **"No bills yet".** No bill has been made. Make one from **Generate Bill**.
:::

::: section cancellation-requests
title: Approve or reject cancellation requests
routes: /GuestHouseCancellations
minutes: 3
screenshot: cancellation-requests.png
screenshot-phone: cancellation-requests.phone.jpg

**What this screen is for**
When someone asks to cancel a booking, the request comes here. Open it from the menu: **SDWA**, then **Cancellation Requests**.

1. Tap a tab: **Pending Approval**, **Approved**, **Rejected** or **All**.
2. Type in **Search requests...**, or choose a guest house in **All Guest Houses**.
3. Read the row: the booking, the guest, the stay, the amount **Paid**, the **Refund**, the **Reason** and when it was **Requested**.
4. To accept, press **Approve**, read the box and press **Approve Cancellation**. The booking is cancelled and the room is released.
5. To refuse, press **Reject**, write the **Reason for rejection** and press **Reject Request**. The booking stays active.
6. After you approve a refund, pay it by hand to the original payment source within 2 working days.

> Careful: The app does not send the refund itself. Someone must pay it outside the app.

## Common problems

- **"You do not have permission to review cancellation requests."** Ask your administrator for this permission.
- **"The guest is checked in. Check the guest out instead of cancelling."** Use **Check-Out** on **Front Office**.
- **There is no Reject button.** A refund for this request is already in progress.
:::

::: section guest-house-master
title: Add guest houses, room types and rooms
routes: /GuestHouseMaster
minutes: 4
screenshot: guest-house-master.png
screenshot-phone: guest-house-master.phone.jpg

**What this screen is for**
Admins use **Guest House & Room Master** to set up what people can book. Open it from the menu: **SDWA**, then **Guest House**. Add the guest house first, then room types, then rooms.

1. On the **Guest House** tab, enter the **Name** and, if known, **Address**, **Phone Number**, **State** and **Description**. Press **Upload Image** to add a photo.
2. On the **Room Type** tab, enter a **Room Type Name**, for example AC Deluxe Room.
3. On the **Rooms** tab, choose the **Guest House** and **Room Type (Master)**, then enter the **Room Type**, **Room Number**, **No. of Rooms** and **Rate (₹)**.
4. Set the switch to **Active** or **Inactive** and press **Save**. **Cancel** clears the form.
5. To add many at once on the **Guest House** or **Rooms** tab, press **Download Template**, fill it in, then press **Upload File**.
6. In the table, use the search box and **All Status**. Tap **Active** or **Inactive** to switch it, the pencil to edit, or the bin to delete.

> Careful: Delete cannot be undone. To stop bookings for a while, set it to **Inactive** instead. Inactive guest houses are not shown for booking.

| Field | What to enter |
|---|---|
| **Name** | The guest house name. Required. |
| **State** | The state of the guest house. Receptionists of that state manage it. |
| **No. of Rooms** | How many rooms of this type. A whole number, 1 or more. Required. |
| **Rate (₹)** | Price per night. Required. |

## Common problems

- **"No. of Rooms must be a positive whole number."** Enter 1 or more.
- **"Rate must be a valid amount."** Enter a number, for example 1200.
- **I see the tables but no form.** You can only view. Ask your administrator for add or update rights.
:::

::: section design-previews
title: Screens that are still being built
routes: /GuestBooking, /RoomDetails, /CancelledBookingDetails, /PaymentManagement
minutes: 1

**What this screen is for**
A few screens are early designs. They show fixed sample content, not your data, and their buttons do not save anything.

1. If you opened **Review Your Booking** with sample guest details, **AC Deluxe Room** room details, **Cancelled Booking Details** or **Payment Management**, you are on one of these design screens.
2. Do not enter or trust any data there.
3. To book a room, use **Book a Room**. To see, cancel or track your bookings, use **My Bookings / Cancellation**.

## Common problems

- **The numbers never change.** This is sample content. Use the real screens named above.
:::
