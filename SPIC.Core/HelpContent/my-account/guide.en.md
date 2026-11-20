---
module: my-account
title: My account and dashboard
icon: bi-person-circle
order: 118
category: General
color: #0b6b3a
example: DEMO-DL-001 · Demo Agro Traders
updated: 2026-11-17
note: New guide
---

::: section overview
title: What this module does for you
kind: overview

These are the screens about you: the dealer registration dashboard, your own profile, the list of things waiting for you, and how to contact SPIC.

- dashboard | icon: bi-speedometer2 | color: #0b6b3a
  title: Dealer registrations
  text: See every dealer application in your area, with its status, and act on the ones waiting for you.
- profile | icon: bi-person-badge | color: #2563eb
  title: Your profile
  text: Check your name, designation, location and status. Dealers also see their registration details.
- help | icon: bi-bell | color: #d97706
  title: Alerts and help
  text: See what is waiting for you, find your daily work, and send a message to SPIC.
:::

::: section flow
title: How a dealer registration moves on the dashboard
kind: flow

Each row is a role and each box is a step. Your row is marked "You". The dashboard card of a dealer shows the step it has reached.

- creator | type: lane | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: Your HQ
- rm-lane | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: Your region
- smm-lane | type: lane | color: #7c3aed | roles: SMD, SMM
  title: SMM
  sub: Your state
- avp-lane | type: lane | color: #2563eb | roles: AVP, Admin
  title: AVP
  sub: Final approval
- draft | lane: creator | col: 1 | icon: bi-pencil-square | color: #a16207 | next: rm | screen: dashboard
  title: Fill and submit
  status: Draft
  text: The card shows **Draft** until the application is submitted. Press **Edit** to continue it.
- rm | lane: rm-lane | col: 2 | icon: bi-hourglass-split | color: #1d4ed8 | next: smm | return: draft | screen: dashboard
  title: RM checks it
  status: In RM
  text: The RM presses **Review**. If the RM sends it back, the card shows **Rejected by RM** and the MO can edit it again.
- smm | lane: smm-lane | col: 3 | icon: bi-hourglass-split | color: #7c3aed | next: avp | return: rm | screen: dashboard
  title: SMM checks it
  status: In SMM
  text: The SMM presses **Review**. If the SMM sends it back, it shows **Rejected by SMM** and goes back to the RM.
- avp | lane: avp-lane | col: 4 | icon: bi-patch-check | color: #4338ca | next: approved | return: smm | screen: dashboard
  title: AVP approves
  status: In AVP
  text: The AVP gives the final approval. If the AVP sends it back, it shows **Rejected by AVP** and goes back to the SMM.
- approved | lane: avp-lane | col: 5 | icon: bi-check-circle | color: #16a34a | screen: dashboard
  title: Approved
  status: Approved
  text: The card shows **Approved**. A new dealer first shows **In AVP Office** until the AVP Office saves the company Dealer IDs.
:::

::: section statuses
title: Every status you can see
kind: statuses

- draft | color: #a16207
  label: Draft
  text: Not submitted yet. The MO, MDO or JMDO can still edit it.
- in-rm | color: #1d4ed8
  label: In RM
  text: Waiting for the RM to review it.
- in-smm | color: #7c3aed
  label: In SMM
  text: Waiting for the SMM to review it.
- in-avp | color: #4338ca
  label: In AVP
  text: Waiting for the AVP's final approval.
- in-avp-office | color: #0e7490
  label: In AVP Office
  text: A new dealer is approved. The AVP Office must still save the company Dealer IDs.
- approved | color: #047857
  label: Approved
  text: The application is fully approved.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: Sent back to the MO, MDO or JMDO to correct.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: Sent back to the RM.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: Sent back to the SMM.
- action | color: #2563eb
  label: Action Required
  text: It is your turn to act on this card.
- active | color: #047857
  label: Active
  text: The dealer, or your own account on the profile, is active.
- inactive | color: #c2410c
  label: Inactive
  text: The dealer, or your own account, is not active now.
- terminated | color: #dc2626
  label: Terminated
  text: The dealership has ended.
:::

::: section roles
title: Who does what
kind: roles

Your card is highlighted. Everyone can open their own profile.

- creator | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - Create and edit dealer applications
  - Correct applications sent back by the RM
- approvers | color: #0d9488 | roles: RM, RMD, SMD, SMM
  title: RM, SMM
  - Review the cards marked Action Required
  - Download dealer lists
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - Give the final approval
  - Import or add dealers
  - Admin can edit their own profile
- dealer | color: #7c3aed | roles: Dealer
  title: Dealer
  - Check your registration details on your profile
  - See your welfare scheme applications
:::

::: section faq
title: Problems and answers
kind: faq

- no-edit-button | popular: true
  q: I do not see Edit or Review on a dealer card
  a: Only the person whose turn it is sees these buttons. Look at the status on the card to see who has it now.
- wrong-profile | popular: true
  q: My name, designation or location on my profile is wrong
  a: You cannot change it on the **Profile** screen. Ask your administrator to correct it.
- nothing-waiting
  q: Alerts says "Nothing waiting for you"
  a: No approval, booking or application needs you now. Check again later.
- dealer-buttons
  q: Download PDF and Change Password on my dealer profile do nothing
  a: These two buttons do not work yet.
- no-directions
  q: Get Directions says "Location permission is required"
  a: Allow location for the app or browser, then press **Get Directions** again.
:::

::: section dashboard
title: See and act on dealer registrations
routes: /Dashboard
minutes: 3
tour: 1 > [data-help=ma-dash-kpis] | 2 > [data-help=ma-dash-filters] | 3 > [data-help=ma-dash-cards] | 4 > [data-help=ma-dash-cards] | 5 > [data-help=ma-dash-actions]

**What this screen is for**
The **Dealer Registrations** screen shows every dealer application in your area as a card, with counts at the top. Most roles see only the dealers of their own State, Region or HQ.

1. Look at the counts at the top: **Total Registrations**, **Draft**, **In RM**, **In SMM**, **In AVP**, **Approved** and **Rejected / Returned**. The **Status** box counts active, inactive and terminated dealers.
2. Type a dealer code or firm name in the search box, or choose a company, dealer type, status, region, approval status or sort order. The cards reload at once.
3. Each card shows the firm name, **Region**, **HQ**, the approval status and the dealer status. A blue **Action Required** tag means it is your turn. Press **Show more** at the bottom to see more cards.
4. Press **Edit** to continue a draft or a returned application, or **Review** to check one sent to you. The eye icon opens the application to read. **SPIC** and **GFL** open the dealership PDF.
5. Press **Dealer Master** or **Credit limit status** to download the list with your filters as an Excel file. MO, MDO and JMDO also see **Create Existing Dealer**, which opens the dealer application form.

> Tip: **Credit Limit** appears on a card only for Admin and MO, after the final approval, when the dealer has a Dealer Code.
> Careful: Admin can delete a registration with the bin icon. This cannot be undone.

## Common problems

- **"No dealer registrations found."** Your search or filters may be too narrow. Clear the search box and set the filters back to "All".
- **There is no Edit or Review button.** The application is with another level now. Check the status on the card.
- **The numbers at the top do not match my region.** The counts follow the same filters as the cards. Change the filters back to "All".
:::

::: section dashboard-add-dealers
title: Import or add dealers
routes: /Dashboard
roles: Admin, AVP
minutes: 3
screenshot: dashboard-add-dealers.png
screenshot-phone: dashboard-add-dealers.phone.jpg

**What this screen is for**
Admin and AVP can bring existing dealers into the app from an Excel file, add one dealer by hand, or start a new dealer application.

1. Press **Sample Format** to download the Excel template.
2. Fill one row per dealer with **CUSTOMER**, **CUSTOMER NAME** and **State**. The first letter of the CUSTOMER code tells which company code it is, for example **D** for SPIC.
3. Press **Import from Excel** and choose the file (.xlsx or .xls). Wait while it says "Importing, please wait...".
4. Read the result. Rows that were not imported are listed under **Skipped Groups** with the reason. Press **Close**.
5. To add only one dealer, press **Add Dealer**, enter the **CUSTOMER Code**, **CUSTOMER Name** and **State**, and press **Save Dealer**.
6. To start a full application for a new dealer, press **Create New Dealer**.

> Tip: Admin also sees **Total Dealer List**, which opens a state-wise count of dealers.

| Field | What to enter |
|---|---|
| **CUSTOMER Code** | The dealer's code from the company records. Required. |
| **CUSTOMER Name** | The firm or dealer name. |
| **State** | The dealer's state. |

## Common problems

- **Some rows were skipped.** Read the reason under **Skipped Groups**, correct those rows in the file and import it again.
- **"Import failed."** Check that the file is the Excel template with the three columns, then try again.
:::

::: section profile
title: See my profile
routes: /Profile
minutes: 2
tour: 1 > [data-help=ma-prf-banner] | 2 > [data-help=ma-prf-tabs] | 3 > [data-help=ma-prf-details] | 4 > [data-help=ma-prf-details]
screenshot: profile.png
hotspots: 1 @ 2.2,12.7,95.5,22 | 2 @ 2.2,37.6,95.5,6.3 | 3 @ 2.2,46.7,95.5,39.8 | 4 @ 2.2,46.7,95.5,39.8
screenshot-phone: profile.phone.jpg
hotspots-phone: 1 @ 3.1,10.2,93.8,33.7 | 2 @ 2.1,46.1,95.9,7 | 3 @ 3.1,55.2,93.8,33.5 | 4 @ 3.1,55.2,93.8,33.5

**What this screen is for**
The **Profile** screen shows your own account details. Staff see their employee details. Dealers see their registration details.

1. Look at the green banner. It shows your name, your role or code and your designation. Dealers also see **Profile Completion**.
2. Use the tabs. Staff see one tab (**Employee**, or **Profile** for Admin). Dealers see **Dealer Info**, **Proprietorship**, **Nominees**, **Trade Experience**, **Turnover**, **Assets** and **Welfare Scheme**.
3. Read your details in the card, for example **Designation**, **Region**, **Headquarter** and **Status**. On **Turnover**, switch between **SPIC** and **Green Star**.
4. If something is wrong, ask your administrator to correct it. You cannot change your details on this screen.

> Careful: On a dealer profile, **Download PDF** and **Change Password** do not work yet.

## Common problems

- **"No employee profile information available."** Your employee record is not linked to your login. Ask your administrator.
- **"No dealer registration data available."** Your dealer registration is not linked to your login yet. Contact your MO.
- **"No welfare scheme applications found."** You have not applied for a welfare scheme yet.
:::

::: section profile-edit
title: Edit my profile (Admin)
routes: /Profile
roles: Admin
minutes: 1
screenshot: profile-edit.png
screenshot-phone: profile-edit.phone.jpg

**What this screen is for**
Only the Admin role sees the **Edit Profile** button. Admin can change their own name, email and designation.

1. On the **Admin Details** card, press **Edit Profile**.
2. Change **Name**, **Email** or **Designation**. Role, User Name and Status cannot be changed here.
3. Press **Save Changes**. The message "Profile updated successfully." appears.
4. Press **Cancel** to close without saving.

## Common problems

- **"Your profile could not be saved. Please try again."** Check your network and press **Save Changes** again.
- **The form is locked and says your details could not be loaded.** Press **Cancel** and open **Edit Profile** again.
:::

::: section user-details
title: Complete your profile (preview)
routes: /UserProfile
minutes: 1
screenshot: user-details.png
screenshot-phone: user-details.phone.jpg

**What this screen is for**
The **User Details** screen is a design preview. It is not in the menu, and it does not save anything yet.

1. The screen shows **Complete Your Profile** with the fields **Full Name**, **Mobile Number**, **Address** and **District**.
2. The **Continue** button does not save these details yet.

## Common problems

- **My details were not saved.** This is normal. The screen is only a preview. Your real details are on **Profile**.
:::

::: section alerts
title: See what is waiting for me
routes: /Alerts
minutes: 1
screenshot: alerts.png
screenshot-phone: alerts.phone.jpg

**What this screen is for**
The **Alerts** screen lists the queues that may need your action. On a phone or tablet, open it with the bell at the top of the app. It shows only the links your role can use.

1. Under **Approvals waiting**, tap a queue, for example **Scheme Approval** or **Dealer Application Review**.
2. Under **Bookings and applications**, tap **My Bookings** or **Welfare Schemes**.
3. Work on the items on that screen, then come back here.

> Tip: Push notifications will come with the app store release. Until then, check this screen.

## Common problems

- **"Nothing waiting for you".** Your role has no approval, booking or application screens here.
- **A queue opens but is empty.** The link is always shown for your role. It does not mean something is waiting.
:::

::: section activities
title: Open my daily work
routes: /Activities
minutes: 1
screenshot: activities.png
screenshot-phone: activities.phone.jpg

**What this screen is for**
The **My Activities** screen gathers your field work, approvals, guest house bookings and reports in one place. On a phone, MO staff find it in the tab bar at the bottom.

1. Under **Field activities**, tap **Sample Collection** or **Consignment History** if you have them.
2. Under **Pending on you**, tap a queue such as **Scheme Approval** or **Pending Acknowledgement**.
3. Under **Guest house**, tap **Book a room** or **My Bookings**.
4. Under **Reports**, tap **Reports Center**.

> Tip: A part appears only when your role can use at least one of its links.

## Common problems

- **I only see "More of the Sales Activity System is on its way".** Your role has no field activity screens yet. These will be added later.
:::

::: section contact-us
title: Contact SPIC
routes: /ContactUs
minutes: 2
screenshot: contact-us.png
screenshot-phone: contact-us.phone.jpg

**What this screen is for**
The **Contact Us** screen shows the SPIC office address, email and phone number, a map, and a form to send your question.

1. Read the office address, email and phone number on the three cards. Office hours are Monday to Friday, 9:30 AM to 6:00 PM.
2. Press **Open in Maps** to see the office in your map app.
3. Press **Get Directions** to see the road route, distance and travel time from where you are.
4. Under **Send Us a Message**, fill **Full Name**, **Email Address**, **Enquiry Type**, **Subject** and **Message**.
5. Press **Send Feedback**. The message "Thank you! Your enquiry has been sent." appears.

> Careful: **Send Email**, **Call Office** and **WhatsApp Us** do not work yet. Use the email or phone number on the cards.

| Field | What to enter |
|---|---|
| **Enquiry Type** | **Welfare Schemes**, **Room Bookings** or **General Enquiry**. |
| **Email Address** | An email where SPIC can reply to you. |
| **Message** | Your question in a few lines. |

## Common problems

- **"Please fill in every field before sending."** All five fields are required.
- **"Please enter a valid email address."** Check the email, for example name@example.com.
- **"Location permission is required to calculate the distance to SPIC Office."** Allow location for the app, then press **Get Directions** again.
- **"Your enquiry could not be sent."** Check your network and try again later.
:::

::: section gallery
title: Photos and videos (preview)
routes: /Gallery, /Album, /ViewVideo
minutes: 1
screenshot: gallery.png
screenshot-phone: gallery.phone.jpg

**What this screen is for**
The **Gallery** shows sample pictures of SDWA events, meetings and the guest house. These screens are a design preview with sample pictures, not real albums yet.

1. On **Gallery**, scroll through **Photos** and **Videos**.
2. Press **View Album** on a photo to open the **Guest House Rooms** album.
3. In the album, tap **All Media**, **Photos** or **Videos**, or type in the search box.
4. Tap the video card. The video screen says "Video will appear here" because no video is published yet.

> Tip: The tabs, search box and sort list on **Gallery** do not work yet. Every **View Album** button opens the same sample album.

## Common problems

- **The video does not play.** No video has been published yet.
:::
