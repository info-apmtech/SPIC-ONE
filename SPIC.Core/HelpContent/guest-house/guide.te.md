---
module: guest-house
title: గెస్ట్ హౌస్
icon: bi-house-door
order: 112
language: te
category: సౌకర్యాలు
note: కొత్త గైడ్
meta-source-hash: a51949592e8c1854
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 1b36f90eb8520b48
confidence: ok

కంపెనీ గెస్ట్ హౌస్‌లో రూమ్ బుక్ చేయండి, అవసరమైతే దానికి పేమెంట్ చేయండి, మీ బుకింగ్‌లు అన్నీ ఒకే చోట చూడండి. గెస్ట్ హౌస్ దగ్గర Front Office మిమ్మల్ని check-in చేసి, రూమ్ నంబర్ ఇచ్చి, check-out చేసి, బిల్లు తయారు చేస్తుంది.

- book | icon: bi-calendar-check | color: #b45309
  title: 5 దశల్లో బుక్ చేయండి
  text: గెస్ట్ హౌస్, తేదీలు, రూమ్, గెస్ట్ వివరాలు ఎంచుకుని, తర్వాత కన్ఫర్మ్ చేయండి.
- pay | icon: bi-wallet2 | color: #0d9488
  title: ఉద్యోగులకు ఉచితం
  text: కంపెనీ ఉద్యోగులు ఉచితంగా బుక్ చేసుకోవచ్చు. డీలర్లు, ఇతరులు Razorpay ద్వారా ఆన్‌లైన్‌లో పేమెంట్ చేస్తారు.
- stay | icon: bi-door-open | color: #2563eb
  title: మిగతాది Front Office చూసుకుంటుంది
  text: రూమ్ నంబర్లతో check-in, check-out, పేమెంట్, బిల్లు.
:::

::: section flow
title: బుకింగ్ ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: f66f0d447db14906
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- booker | type: lane | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: ఉద్యోగి లేదా డీలర్
  sub: బుక్ చేసే వ్యక్తి
- frontoffice | type: lane | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: Front Office
  sub: Receptionist · Admin
- admin | type: lane | color: #dc2626 | roles: Admin, CorporateAdmin
  title: గెస్ట్ హౌస్ admin
  sub: క్యాన్సిలేషన్‌లు, మాస్టర్‌లు
- book | lane: booker | col: 1 | icon: bi-house-door | color: #b45309 | next: pay | screen: choose-guest-house
  title: ఎంచుకుని నింపడం
  status: Pending Payment
  text: గెస్ట్ హౌస్, తేదీలు, రూమ్ ఎంచుకుని, గెస్ట్ వివరాలు నింపండి.
- pay | lane: booker | col: 2 | icon: bi-wallet2 | color: #0d9488 | next: checkin | return: book | screen: review-pay
  title: చెక్ చేసి పేమెంట్ చేయడం
  status: Confirmed
  text: అన్నీ చెక్ చేసి **Complete Booking** నొక్కండి. ఉద్యోగులు ఏమీ కట్టనక్కర్లేదు. మిగతావారు ఆన్‌లైన్‌లో కడతారు. పేమెంట్ ఫెయిల్ అయితే, కొత్త బుకింగ్ మొదలుపెట్టండి.
- checkin | lane: frontoffice | col: 3 | icon: bi-box-arrow-in-right | color: #2563eb | next: checkout | screen: check-in
  title: Check-in చేయడం
  status: CheckedIn
  text: Front Office ID ప్రూఫ్ చెక్ చేసి రూమ్ నంబర్లు ఇస్తుంది.
- checkout | lane: frontoffice | col: 4 | icon: bi-box-arrow-right | color: #7c3aed | next: bill | screen: check-out
  title: Check-out చేయడం
  status: Completed
  text: Front Office బాకీ ఉన్న డబ్బు తీసుకుని, గెస్ట్‌ను check-out చేస్తుంది. రూమ్ ఖాళీ అవుతుంది.
- bill | lane: frontoffice | col: 5 | icon: bi-receipt | color: #16a34a | screen: generate-bill
  title: బిల్లు తయారు చేయడం
  status: Billed
  text: Front Office భోజనం, ఇతర ఛార్జీలు కలిపి tax invoice తయారు చేస్తుంది.
- cancel | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: pay | screen: cancellation-requests
  title: క్యాన్సిల్, రీఫండ్
  status: Cancelled
  text: బుక్ చేసినవారు **My Bookings** నుంచి క్యాన్సిల్ చేయమని అడుగుతారు. Admin ఆమోదిస్తారు లేదా తిరస్కరిస్తారు. ఆమోదించిన రీఫండ్‌ను, మొదట పేమెంట్ చేసిన అదే మార్గానికి చేతితో (manual గా) పంపుతారు.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 41c01f8852ce2a28
confidence: ok

- pending-payment | color: #d97706
  label: Pending Payment
  text: బుకింగ్ సేవ్ అయ్యింది, ఆన్‌లైన్ పేమెంట్ కోసం ఎదురుచూస్తోంది.
- confirmed | color: #16a34a
  label: Confirmed
  text: పేమెంట్ అయ్యింది, లేదా ఉద్యోగికి ఉచితం. రూమ్ మీ కోసం ఉంచబడింది.
- booking-checked-in | color: #2563eb
  label: CheckedIn
  text: గెస్ట్ ఇప్పుడు రూమ్‌లో ఉంటున్నారు.
- completed | color: #0d9488
  label: Completed
  text: గెస్ట్ check-out అయ్యారు.
- cancelled | color: #dc2626
  label: Cancelled
  text: బుకింగ్ క్యాన్సిల్ అయ్యింది, లేదా దాని పేమెంట్ జరగలేదు.
- paid | color: #16a34a
  label: Paid
  text: డబ్బు అందింది.
- pending | color: #d97706
  label: Pending
  text: డబ్బు ఇంకా అందలేదు.
- failed | color: #dc2626
  label: Failed
  text: ఆన్‌లైన్ పేమెంట్ జరగలేదు. కొత్త బుకింగ్ మొదలుపెట్టండి.
- complimentary | color: #0d9488
  label: Complimentary
  text: కంపెనీ ఉద్యోగికి ఉచిత బుకింగ్. పేమెంట్ లేదు.
- pending-approval | color: #d97706
  label: Pending Approval
  text: మీ క్యాన్సిలేషన్ రిక్వెస్ట్ admin కోసం ఎదురుచూస్తోంది.
- approved | color: #16a34a
  label: Approved
  text: Admin క్యాన్సిలేషన్‌ను ఆమోదించారు. రూమ్ ఖాళీ చేయబడింది.
- rejected | color: #475569
  label: Rejected
  text: Admin క్యాన్సిలేషన్‌ను ఒప్పుకోలేదు. మీ బుకింగ్ అలాగే కొనసాగుతుంది.
- refund-manual | color: #7c3aed
  label: Cancelled - Refund to be Processed Manually
  text: రీఫండ్‌ను మీరు మొదట పేమెంట్ చేసిన అదే మార్గానికి చేతితో పంపుతారు.
- refund-completed | color: #16a34a
  label: Refund Completed
  text: రీఫండ్ పంపబడింది. మీ బ్యాంక్ లేదా కార్డు స్టేట్‌మెంట్ చూడండి.
- no-refund | color: #64748b
  label: Cancelled - No Refund Due
  text: క్యాన్సిలేషన్ నియమం ప్రకారం రీఫండ్ ఏమీ రాదు.
- refund-failed | color: #dc2626
  label: Refund Failed
  text: రీఫండ్ పూర్తి కాలేదు. టీమ్ మిమ్మల్ని సంప్రదిస్తుంది.
- available | color: #16a34a
  label: Available
  text: Front Office: రూమ్ ఖాళీగా ఉంది.
- reserved | color: #d97706
  label: Reserved / Booked
  text: Front Office: ఒక గెస్ట్ రూమ్ బుక్ చేశారు, ఇంకా check-in కాలేదు.
- checked-in | color: #2563eb
  label: Checked-In
  text: Front Office: ఒక గెస్ట్ ఇప్పుడు రూమ్‌లో ఉన్నారు.
- not-billed | color: #64748b
  label: Not Billed
  text: గెస్ట్ check-out అయ్యారు, కానీ ఇంకా బిల్లు తయారు కాలేదు.
- billed | color: #16a34a
  label: Billed
  text: ఈ బసకు బిల్లు తయారైంది.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 0b5537897c5b9427
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- booker | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: ఉద్యోగి లేదా డీలర్
  - రూమ్ బుక్ చేయడం
  - మీ బుకింగ్‌లు చూడటం, invoice డౌన్‌లోడ్ చేయడం
  - క్యాన్సిల్ చేయమని అడగడం, రీఫండ్ ట్రాక్ చేయడం
- frontoffice | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: Front Office
  - గెస్ట్‌లను check-in చేసి రూమ్ నంబర్లు ఇవ్వడం
  - పేమెంట్ తీసుకుని గెస్ట్‌లను check-out చేయడం
  - బిల్లులు తయారు చేసి ప్రింట్ చేయడం
- admin | color: #dc2626 | roles: Admin, CorporateAdmin
  title: గెస్ట్ హౌస్ admin
  - క్యాన్సిలేషన్ రిక్వెస్ట్‌లను ఆమోదించడం లేదా తిరస్కరించడం
  - గెస్ట్ హౌస్‌లు, రూమ్ రకాలు, రూమ్‌లు జోడించడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: c456cc4a4b5c2181
confidence: ok

- free-booking | popular: true
  q: నా మొత్తం ₹0.00 (Complimentary) అని ఎందుకు చూపిస్తోంది?
  a: కంపెనీ ఉద్యోగులు గెస్ట్ హౌస్ రూమ్‌లను ఉచితంగా బుక్ చేసుకోవచ్చు. డీలర్లు, ఇతరులు బుక్ చేసేటప్పుడు ఆన్‌లైన్‌లో పేమెంట్ చేస్తారు.
- payment-failed | popular: true
  q: నా పేమెంట్ ఫెయిల్ అయ్యింది, లేదా పేమెంట్ విండో మూసేశాను
  a: ఆ బుకింగ్ ముందుకు వెళ్లదు, రూమ్ ఖాళీ అవుతుంది. **Book a Room** నుంచి కొత్త బుకింగ్ మొదలుపెట్టండి. మీ అకౌంట్ నుంచి డబ్బు కట్ అయి ఉంటే, అది ఆటోమేటిక్‌గా తిరిగి వస్తుంది.
- no-cancel
  q: నాకు Cancel Booking బటన్ కనిపించడం లేదు
  a: అది రాబోయే బుకింగ్‌లపై మాత్రమే కనిపిస్తుంది. Check-in అయిన బసను ఆన్‌లైన్‌లో క్యాన్సిల్ చేయలేరు. దయచేసి Front Office ని సంప్రదించండి.
- refund-when
  q: నా రీఫండ్ ఎప్పుడు వస్తుంది?
  a: Admin మీ క్యాన్సిలేషన్‌ను ఆమోదించిన తర్వాత, 2 పని దినాల్లో రీఫండ్‌ను మీరు మొదట పేమెంట్ చేసిన మార్గానికే చేతితో పంపుతారు. దాన్ని ట్రాక్ చేయడానికి **Refund Status** తెరవండి.
- no-rooms
  q: "No rooms available" అని కనిపిస్తోంది
  a: ఆ తేదీలకు రూమ్‌లు అన్నీ బుక్ అయిపోయాయి. **Change dates** నొక్కి వేరే తేదీలు ప్రయత్నించండి, లేదా వేరే గెస్ట్ హౌస్ చూడండి.
- early-checkin
  q: Front Office నన్ను check-in చేయలేకపోతోంది
  a: Check-in తేదీ నుంచి బస ముగిసే వరకు మాత్రమే check-in చేయవచ్చు, అది కూడా **Confirmed** బుకింగ్‌కు మాత్రమే.
:::

::: section choose-guest-house
title: గెస్ట్ హౌస్, మీ తేదీలు ఎంచుకోవడం
routes: /GuestHouse, /GuestHouseBooking
for: 
roles: 
designations: 
minutes: 2
source-hash: 3d1914b34d0d67da
confidence: ok

**ఈ స్క్రీన్ దేనికి**
బుకింగ్ ఇక్కడే మొదలవుతుంది. ముందు **Choose Your Guest House** లో గెస్ట్ హౌస్ ఎంచుకోండి, తర్వాత **Guest House Booking** లో (5 లో దశ 1) మీ తేదీలు ఎంచుకోండి.

1. మెనూలో **SDWA** తెరిచి **Book a Room** నొక్కండి. ప్రతి కార్డులో ఒక గెస్ట్ హౌస్, దాని అడ్రస్, ఫోన్ నంబర్ కనిపిస్తాయి.
2. మీకు కావాల్సిన గెస్ట్ హౌస్ కార్డుపై **Select** నొక్కండి. బటన్‌పై గెస్ట్ హౌస్ పేరు ఉంటుంది.
3. **Guest House Booking** లో, **Check-in Date**, **Check-in Time** ఎంచుకోండి.
4. **Check-out Date**, **Check-out Time** ఎంచుకోండి.
5. **Important Information** బాక్స్ చదివి, తర్వాత **Check Availability** నొక్కండి.

> Tip: సరైన ID ప్రూఫ్ (Aadhaar లేదా Passport) తీసుకురండి. Check-in దగ్గర అది కావాలి.

## సాధారణ సమస్యలు

- **"Check-out must be after Check-in."** (Check-out, check-in తర్వాతే ఉండాలి) తర్వాతి check-out తేదీ లేదా సమయం ఎంచుకోండి.
- **"No guest houses available".** (గెస్ట్ హౌస్‌లు ఏవీ లేవు) **Retry** నొక్కండి. అయినా అలాగే కనిపిస్తే, SDWA ఆఫీసును సంప్రదించండి.
- **నన్ను మళ్లీ Choose Your Guest House కి పంపేస్తోంది.** ముందు **Select** తో గెస్ట్ హౌస్ ఎంచుకుని, తర్వాత తేదీలు ఎంచుకోండి.
:::

::: section choose-room
title: రూమ్ ఎంచుకోవడం
routes: /Rooms
for: 
roles: 
designations: 
minutes: 2
source-hash: 9eefa1291c16d092
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Available Rooms** స్క్రీన్ (5 లో దశ 2) మీ తేదీలకు ఖాళీగా ఉన్న రూమ్‌లను చూపిస్తుంది.

1. రూమ్ కార్డులు చూడండి. ప్రతి దానిలో రూమ్ రకం, ఒక రాత్రికి ధర, ఎన్ని రూమ్‌లు ఖాళీగా ఉన్నాయి, ఎంత మంది గెస్ట్‌లు ఉండవచ్చు అనేది కనిపిస్తుంది.
2. ఆ రూమ్‌లు మాత్రమే చూడటానికి, పైన ఒక రూమ్ రకాన్ని నొక్కండి, లేదా **Search** లో టైప్ చేయండి. **All** అన్ని రూమ్‌లను చూపిస్తుంది.
3. మీకు కావాల్సిన **Number of Rooms** ఎంచుకోండి.
4. అదనపు బెడ్ కావాలంటే, **Additional Bed** టిక్ చేసి **No. of Additional Beds** ఎంచుకోండి. అదనపు బెడ్‌కు ఒక రాత్రికి వేరే ధర ఉంటుంది.
5. **Book Now** నొక్కండి.

> Tip: **View Details** బటన్ ఇంకా పని చేయదు. రూమ్ వివరాలు అన్నీ కార్డులోనే ఉన్నాయి.

## సాధారణ సమస్యలు

- **"No rooms available".** (రూమ్‌లు లేవు) ఆ తేదీలకు ఏ రూమ్ ఖాళీగా లేదు. **Change dates** నొక్కండి.
- **"No matching rooms".** (సరిపోయే రూమ్‌లు లేవు) **Clear filters** నొక్కండి.
- **"Up to ... rooms can be booked at a time for this room type."** (ఈ రూమ్ రకంలో ఒకసారికి ... రూమ్‌ల వరకు మాత్రమే బుక్ చేయవచ్చు) మిగతావి రెండో బుకింగ్‌లో బుక్ చేయండి.
:::

::: section guest-details
title: గెస్ట్ వివరాలు నింపడం
routes: /GuestDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: eca96eb90f508346
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Guest Details** (5 లో దశ 3) లో ఎవరు ఉండబోతున్నారో చెప్పండి. కుడి వైపు ఉన్న **Booking Summary** ధర చూపిస్తుంది.

1. **Employee ID / Dealer Code**, **Guest Name** ఎంటర్ చేయండి.
2. **Company Name** లిస్ట్‌లో ఉంటే దాన్ని ఎంచుకుని, **Phone Number**, **Email ID** ఎంటర్ చేయండి. డీలర్ అయితే, ఫోన్, ఈమెయిల్ డీలర్ రిజిస్ట్రేషన్ నుంచి ఆటోమేటిక్‌గా నిండుతాయి.
3. **Aadhaar / Passport Number** ఎంటర్ చేసి **Nationality** ఎంచుకోండి.
4. **Number of Persons**, **Number of Adults**, **Number of Children (below 12 years)** ఎంచుకోండి.
5. కావాలంటే, **Upload ID Proof (Optional)** లో ID ప్రూఫ్ అప్‌లోడ్ చేయండి.
6. **Booking Summary** చెక్ చేయండి: రూమ్ ఖర్చు, **GST (5%)**, **Total**. తర్వాత **Continue Review** నొక్కండి.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Employee ID / Dealer Code** | మీ ఉద్యోగి ID లేదా డీలర్ కోడ్. తప్పనిసరి. |
| **Guest Name** | ఉండబోయే వ్యక్తి పేరు. తప్పనిసరి. |
| **Phone Number**, **Email ID** | గెస్ట్ కాంటాక్ట్ వివరాలు. తప్పనిసరి. |
| **Nationality** | Indian, NRI లేదా Foreigner. తప్పనిసరి. |
| **Company Name** | బిల్లు కోసం కంపెనీ, దాని GSTIN. తప్పనిసరి కాదు. |
| **Upload ID Proof (Optional)** | ఒకే PDF, JPG లేదా PNG ఫైల్, 5 MB వరకు. |

## సాధారణ సమస్యలు

- **"Employee ID / Dealer Code is required to continue."** (ముందుకు వెళ్లడానికి Employee ID / Dealer Code తప్పనిసరి) తప్పనిసరి ఫీల్డ్‌లు అన్నీ నింపండి. ఏది మిస్ అయిందో మెసేజ్‌లోనే ఉంటుంది.
- **"Only PDF, JPG, or PNG files are allowed."** లేదా **"File must be 5 MB or less."** (PDF, JPG, PNG ఫైల్‌లు మాత్రమే, 5 MB లేదా అంతకంటే తక్కువ) వేరే ఫైల్ ఎంచుకోండి, లేదా చిన్న ఫోటో తీయండి.
- **నేను రూమ్ మార్చాలి.** **Available Rooms** కి తిరిగి వెళ్లడానికి **Back** నొక్కండి.
:::

::: section review-pay
title: బుకింగ్ చెక్ చేసి పూర్తి చేయడం
routes: /BookingPreview, /Payment
for: 
roles: 
designations: 
minutes: 3
source-hash: 0ae6b596dd70c921
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Review Your Booking** (5 లో దశ 4) లో మీరు అన్నీ చెక్ చేస్తారు. **Payment** (5 లో దశ 5) లో బుకింగ్ కన్ఫర్మ్ చేస్తారు. కంపెనీ ఉద్యోగులు ఏమీ కట్టనక్కర్లేదు. డీలర్లు, ఇతరులు Razorpay ద్వారా ఆన్‌లైన్‌లో పేమెంట్ చేస్తారు.

1. **Review Your Booking** లో, **Booking Information**, **Booking Summary**, **Guest Information** చెక్ చేయండి.
2. ఏదైనా మార్చాలంటే **Edit Details** నొక్కండి. అంతా సరిగ్గా ఉంటే, **Proceed to Payment** నొక్కండి.
3. **Payment** లో, **Booking Summary** చెక్ చేయండి. ఉద్యోగులకు **Complimentary (Employee)**, మొత్తం ₹0.00 కనిపిస్తాయి. ఇతరులకు **Online Payment** కనిపిస్తుంది.
4. **Complete Booking** నొక్కండి. ఆన్‌లైన్ పేమెంట్ అయితే, Razorpay విండో తెరుచుకుంటుంది. UPI, కార్డు, నెట్ బ్యాంకింగ్ లేదా వాలెట్‌తో కట్టండి.
5. **Booking Confirmed!** బాక్స్‌లో మీ **Booking ID**, **Payment Status** కనిపిస్తాయి. **View My Bookings** నొక్కండి, లేదా మళ్లీ బుక్ చేయడానికి **New Booking** నొక్కండి.

> Careful: పేమెంట్ చేస్తున్నప్పుడు పేమెంట్ విండో మూసేయకండి, పేజీ నుంచి బయటకు వెళ్లకండి. పేమెంట్ ఫెయిల్ అయినా, మీరు దాన్ని మూసేసినా, ఈ బుకింగ్ ముందుకు వెళ్లదు. మీరు కొత్త బుకింగ్ మొదలుపెట్టాలి.

## సాధారణ సమస్యలు

- **"No booking details found".** (బుకింగ్ వివరాలు లేవు) బుకింగ్ మొదలవలేదు, లేదా దాని సమయం అయిపోయింది. **Book a Room** నుంచి మళ్లీ మొదలుపెట్టండి.
- **బటన్‌పై "Payment Failed" అని చూపిస్తోంది.** కొత్త బుకింగ్ మొదలుపెట్టండి. డబ్బు కట్ అయి ఉంటే, అది ఆటోమేటిక్‌గా తిరిగి వస్తుంది.
- **"This room was just booked by someone else for the selected dates. Please try again."** (ఈ రూమ్‌ను ఇప్పుడే వేరేవారు బుక్ చేశారు) రూమ్ లేదా తేదీలు మళ్లీ ఎంచుకోండి.
- **"Your room hold has expired..."** (మీ కోసం ఉంచిన రూమ్ సమయం అయిపోయింది) పేమెంట్ చేయడానికి చాలా సమయం తీసుకున్నారు. కొత్త బుకింగ్ మొదలుపెట్టండి.
:::

::: section my-bookings
title: నా బుకింగ్‌లు చూడటం
routes: /MyBookings
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-mb-cards] | 2 > [data-help=gh-mb-tabs] | 3 > [data-help=gh-mb-list] | 4 > [data-help=gh-mb-list] | 5 > [data-help=gh-mb-new]
minutes: 2
source-hash: eddb90512855180d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**My Bookings** మీరు చేసిన ప్రతి రూమ్ బుకింగ్‌ను, పైన లెక్కలతో చూపిస్తుంది. మెనూ నుంచి తెరవండి: **SDWA**, తర్వాత **My Bookings / Cancellation**.

1. పైన ఉన్న కార్డులు చూడండి: **Total Bookings**, **Upcoming**, **Completed**, **Cancelled**.
2. ఒక ట్యాబ్ నొక్కండి: **Upcoming**, **Completed** లేదా **Cancelled**.
3. ప్రతి బుకింగ్ కార్డులో రూమ్ రకం, బుకింగ్ స్టేటస్, పేమెంట్ స్టేటస్, **Booking ID**, గెస్ట్ హౌస్, తేదీలు, **Guests**, **Rooms**, **Total Amount** కనిపిస్తాయి.
4. బుకింగ్ తెరవడానికి **View Details** నొక్కండి. రాబోయే బుకింగ్‌ను క్యాన్సిల్ చేయమని అడగడానికి **Cancel Booking** నొక్కండి. క్యాన్సిల్ అయిన బుకింగ్‌పై **Refund Status** నొక్కండి.
5. ఇంకో రూమ్ బుక్ చేయడానికి **New Booking** నొక్కండి.

## సాధారణ సమస్యలు

- **"No upcoming bookings".** (రాబోయే బుకింగ్‌లు లేవు) ఈ రోజు లేదా తర్వాత మీకు ఏ బుకింగ్ లేదు. **Book a room** నొక్కండి.
- **"Could not load bookings".** (బుకింగ్‌లు లోడ్ కాలేదు) మీ ఇంటర్నెట్ చెక్ చేసి **Retry** నొక్కండి.
- **ఒక బుకింగ్ Cancelled, Failed అని చూపిస్తోంది.** దాని ఆన్‌లైన్ పేమెంట్ జరగలేదు. మళ్లీ బుక్ చేయండి.
:::

::: section booking-details
title: ఒక బుకింగ్ చూసి invoice డౌన్‌లోడ్ చేయడం
routes: /BookingDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 4c0fd3b25f0fb3dd
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Booking Details** ఒక బుకింగ్ గురించి అన్నీ చూపిస్తుంది: రూమ్, తేదీలు, పేమెంట్, స్టేటస్, గెస్ట్.

1. **My Bookings** లో, ఒక బుకింగ్‌పై **View Details** నొక్కండి.
2. **Room Details**, **Check-in & Check-out** చూడండి. అందులో ఎన్ని రాత్రులు, రూమ్‌లు, అదనపు బెడ్‌లు ఉంటాయి. **Room Number** తెలిసినప్పుడు అది కనిపిస్తుంది.
3. **Payment Details** చూడండి: రూమ్ ఖర్చు, **GST (5%)**, **Total Amount**, **Payment Method**, **Payment Status**.
4. **Booking Status** చూడండి. క్యాన్సిల్ అయిన బుకింగ్‌లో **Cancellation Status** కూడా కనిపిస్తుంది.
5. **Guest Details** చూడండి. మీరు ID ప్రూఫ్ అప్‌లోడ్ చేసి ఉంటే, **View** లేదా **Download** నొక్కండి.
6. Tax invoice ను PDF గా సేవ్ చేయడానికి **Download Invoice** నొక్కండి.
7. తిరిగి వెళ్లడానికి **Back to My Bookings** నొక్కండి.

## సాధారణ సమస్యలు

- **Download Invoice బటన్ లేదు.** పేమెంట్ **Paid** అయి, invoice రెడీ అయినప్పుడు మాత్రమే అది కనిపిస్తుంది.
- **"Booking not found."** (బుకింగ్ దొరకలేదు) మీరు చేసిన బుకింగ్‌లను మాత్రమే తెరవగలరు.
:::

::: section cancel-booking
title: బుకింగ్ క్యాన్సిల్ చేయడం
routes: /CancelBooking
for: 
roles: 
designations: 
minutes: 3
source-hash: 58fac8bc78e4e38a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
క్యాన్సిల్ చేయమని అడగడానికి **Cancel Booking** వాడండి. Admin మీ రిక్వెస్ట్ చెక్ చేస్తారు. Admin ఆమోదించే వరకు మీ బుకింగ్ అలాగే కొనసాగుతుంది.

1. **My Bookings** లో, రాబోయే బుకింగ్‌పై **Cancel Booking** నొక్కండి.
2. **Important Notice** చదవండి. Check-in కి ఇంకా ఎంత సమయం ఉందో దాని బట్టి, మీకు ఎంత శాతం రీఫండ్ వస్తుందో అది చూపిస్తుంది.
3. **Cancellation Reason** కింద, ఒక కారణం ఎంచుకోండి.
4. **Refund Calculation** చెక్ చేయండి: **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)**, **Refund Amount**, **Refund Method**, **Estimated Time**.
5. **Continue Cancellation** నొక్కండి. **Confirm Cancellation?** బాక్స్‌లో, **Cancel Booking** నొక్కండి. మనసు మారితే **Keep My Booking** నొక్కండి.
6. **Cancellation Request Submitted** లో మీ **Cancellation ID**, **Pending Approval** కనిపిస్తాయి. **Refund Status**, **View Booking** లేదా **Go to My Bookings** నొక్కండి.

> Careful: Check-in దగ్గర పడే కొద్దీ రీఫండ్ తక్కువ వస్తుంది. ముందు **Cancellation Policy** బాక్స్ చదవండి.

## సాధారణ సమస్యలు

- **"Please select a reason for cancellation."** (క్యాన్సిల్ చేయడానికి కారణం ఎంచుకోండి) ముందు ఒక కారణం ఎంచుకోండి.
- **"This booking is already checked in and cannot be cancelled online. Please contact the Front Office."** (ఈ బుకింగ్ ఇప్పటికే check-in అయ్యింది, ఆన్‌లైన్‌లో క్యాన్సిల్ చేయలేరు) Front Office తో మాట్లాడండి.
- **"A cancellation request for this booking has already been submitted".** (ఈ బుకింగ్‌కు క్యాన్సిలేషన్ రిక్వెస్ట్ ఇప్పటికే పంపారు) మీరు ఇప్పటికే అడిగారు. **View Refund Status** నొక్కండి.
:::

::: section refund-status
title: నా రీఫండ్ ట్రాక్ చేయడం
routes: /RefundStatus
for: 
roles: 
designations: 
minutes: 2
source-hash: f4d86371946e0403
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ క్యాన్సిలేషన్, రీఫండ్ ఇప్పుడు ఎక్కడ ఉన్నాయో **Refund Status** చూపిస్తుంది.

1. **My Bookings** లో, **Cancelled** ట్యాబ్ తెరిచి **Refund Status** నొక్కండి. రిక్వెస్ట్ పంపిన తర్వాత **Cancel Booking** నుంచి కూడా దీన్ని తెరవవచ్చు.
2. పైన, **Total Refund Amount**, **Cancellation ID**, **Refund Method**, **Target Refund Date**, **Transaction Ref.**, **Current Status** చూడండి.
3. **Refund Breakdown** కింద, **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)**, **Refund Amount** చూడండి.
4. **Refund Tracking** కింద, దశలను గమనించండి: **Cancellation Requested**, తర్వాత **Pending Admin Approval**, **Approved by Admin** లేదా **Cancellation Rejected**, తర్వాత రీఫండ్.
5. టార్గెట్ తేదీ లోపు డబ్బు మీకు అందకపోతే, ఆరెంజ్ బాక్స్‌లో ఉన్న రిఫరెన్స్‌తో సపోర్ట్‌ను సంప్రదించండి.
6. **My Bookings** కి తిరిగి వెళ్లడానికి **Back** నొక్కండి.

## సాధారణ సమస్యలు

- **నా బుకింగ్ Cancelled ట్యాబ్‌లో లేదు.** Admin ఆమోదించిన తర్వాతే అది అక్కడికి వెళ్తుంది. అప్పటివరకు, ఆ బుకింగ్ **Cancel Booking** స్క్రీన్ తెరిచి **View Refund Status** నొక్కండి.
- **"Cancellation Rejected".** (క్యాన్సిలేషన్ తిరస్కరించబడింది) కారణం చదవండి. మీ బుకింగ్ అలాగే కొనసాగుతుంది.
- **"No Refund Due".** (రీఫండ్ రాదు) క్యాన్సిలేషన్ నియమం ప్రకారం డబ్బు తిరిగి రాదు, ఉదాహరణకు ఉద్యోగి ఉచిత బుకింగ్‌కు.
:::

::: section front-office
title: Front Office లో అన్ని రూమ్‌లు చూడటం
routes: /FrontOffice
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-fo-cards] | 2 > [data-help=gh-fo-tabs] | 3 > [data-help=gh-fo-search] | 4 > [data-help=gh-fo-bill]
minutes: 2
source-hash: c62b602840a09360
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Front Office** స్క్రీన్ ప్రతి రూమ్‌ను చూపిస్తుంది: అది ఖాళీగా ఉందా, బుక్ అయిందా, ఎవరైనా ఉన్నారా, ఎవరి దగ్గర ఉంది అని. మెనూ నుంచి తెరవండి: **SDWA**, తర్వాత **Front Office**. Receptionist తమ సొంత రాష్ట్రం గెస్ట్ హౌస్‌ను మాత్రమే చూస్తారు.

1. పైన ఉన్న కార్డులు చూడండి: **Total Rooms**, **Available**, **Reserved**, **Checked-In**.
2. ఒక ట్యాబ్ నొక్కండి: **All**, **Available**, **Reserved / Booked** లేదా **Checked-In**.
3. **Search rooms...** లో రూమ్ నంబర్, booking ID లేదా గెస్ట్ పేరు టైప్ చేయండి, లేదా **All Guest Houses** లో ఒక గెస్ట్ హౌస్ ఎంచుకోండి. ప్రతి వరుసలో రూమ్, దాని స్టేటస్, బుకింగ్, గెస్ట్, తేదీలు, చర్యలు కనిపిస్తాయి.
4. Check-out అయిన గెస్ట్‌కు బిల్లు తయారు చేయడానికి **Generate Bill** నొక్కండి. రూమ్‌లను మళ్లీ లోడ్ చేయడానికి **Refresh** నొక్కండి.

## సాధారణ సమస్యలు

- **"You do not have permission to access the Front Office."** (Front Office తెరవడానికి మీకు అనుమతి లేదు) Front Office అనుమతి కోసం మీ administrator ని అడగండి.
- **"No rooms found".** (రూమ్‌లు ఏవీ దొరకలేదు) **All**, **All Guest Houses** ఎంచుకుని, సెర్చ్ క్లియర్ చేయండి.
:::

::: section check-in
title: గెస్ట్‌ను check-in చేయడం
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 3
source-hash: 4d3a8c4aabfb6c8a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
గెస్ట్ వచ్చినప్పుడు, వారిని check-in చేసి వారి రూమ్ నంబర్లు ఇవ్వండి.

1. **Front Office** లో, **Reserved / Booked** నొక్కి బుకింగ్‌ను కనుక్కోండి.
2. **Actions** కాలమ్‌లో **Check-In** నొక్కండి.
3. **Check-In** బాక్స్‌లో, **Booking ID**, **Guest Name**, **Room Type**, **Guest House** చెక్ చేయండి. ID ప్రూఫ్ జత చేసి ఉంటే, దాన్ని చూడటానికి **View** నొక్కండి.
4. **Number of Persons**, **Number of Extra Beds** చెక్ చేసి, అవసరమైతే సరిచేయండి.
5. **Assign Physical Room(s)** కింద, ఇవ్వాల్సిన రూమ్ నంబర్లను నొక్కండి. బుక్ చేసినన్ని రూమ్‌లు సరిగ్గా అంతే ఎంచుకోండి.
6. **Confirm Check-In** నొక్కండి. ఇప్పుడు రూమ్‌పై **Checked-In** అని కనిపిస్తుంది.

> Careful: Check-in ముందు గెస్ట్ ID ప్రూఫ్ (Aadhaar లేదా Passport) చెక్ చేయండి.

## సాధారణ సమస్యలు

- **"This booking's check-in date has not arrived yet."** (ఈ బుకింగ్ check-in తేదీ ఇంకా రాలేదు) Check-in తేదీ రోజు లేదా ఆ తర్వాత check-in చేయండి.
- **"This booking's stay period has passed and it can no longer be checked in."** (బస సమయం అయిపోయింది, ఇక check-in చేయలేరు) బస తేదీలు అయిపోయాయి.
- **"Please select exactly ... physical room(s) before confirming check-in."** (check-in కన్ఫర్మ్ చేసే ముందు సరిగ్గా ... రూమ్(లు) ఎంచుకోండి) ఇంకా ఎక్కువ లేదా తక్కువ రూమ్ నంబర్లు నొక్కండి.
- **"No physical rooms are currently free for the booked stay period."** (బుక్ చేసిన సమయానికి ఇప్పుడు ఏ రూమ్ ఖాళీగా లేదు) బుక్ చేసినవారిని సంప్రదించండి, లేదా వేరే రూమ్ రకం ఎంచుకోండి.
- **"These rooms were just assigned to another overlapping stay. Please refresh and try again."** (ఈ రూమ్‌లు ఇప్పుడే వేరే బసకు ఇచ్చేశారు) **Refresh** నొక్కి వేరే రూమ్‌లు ఎంచుకోండి.
:::

::: section check-out
title: గెస్ట్‌ను check-out చేసి పేమెంట్ తీసుకోవడం
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 2
source-hash: cf102cb774da6ac0
confidence: ok

**ఈ స్క్రీన్ దేనికి**
గెస్ట్ వెళ్తున్నప్పుడు, పేమెంట్ అందిందని నిర్ధారించుకుని, తర్వాత వారిని check-out చేయండి. రూమ్ మళ్లీ ఖాళీ అవుతుంది.

1. **Front Office** లో, **Checked-In** నొక్కి గెస్ట్‌ను కనుక్కోండి.
2. **Actions** కాలమ్‌లో **Check-Out** నొక్కండి.
3. **Check-Out & Payment** లో, **Room Number**, తేదీలు, **Number of Persons**, **Extra Beds**, **Total Amount**, **Payment Method**, **Payment Status** చెక్ చేయండి.
4. **Payment Status** **Pending** అని ఉంటే, డబ్బు తీసుకుని **Confirm Payment Received** నొక్కండి.
5. **Paid** అని చూపించాక, **Confirm Check-Out** నొక్కండి. రూమ్ మళ్లీ **Available** అని చూపిస్తుంది.
6. ఇప్పుడు **Generate Bill** తో బిల్లు తయారు చేయండి.

## సాధారణ సమస్యలు

- **"Payment must be received before check-out."** (check-out ముందు పేమెంట్ అందాలి) ముందు **Confirm Payment Received** నొక్కండి.
- **"Check-Out is only allowed for a booking that is currently checked in."** (ఇప్పుడు check-in లో ఉన్న బుకింగ్‌కు మాత్రమే check-out చేయవచ్చు) గెస్ట్ check-in కాలేదు. **Status** చూడండి.
:::

::: section bills
title: పూర్తయిన బసలు, బిల్లులు కనుక్కోవడం
routes: /GenerateBill
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-gb-filters] | 2 > [data-help=gh-gb-go] | 3 > [data-help=gh-gb-tabs] | 4 > [data-help=gh-gb-find]
minutes: 2
source-hash: 5194cb8d5292259e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Generate Bill** స్క్రీన్ check-out అయిన బసలను, ఇప్పటికే తయారైన బిల్లులను చూపిస్తుంది. మెనూ నుంచి (**SDWA**, తర్వాత **Generate Bill**) లేదా **Front Office** నుంచి తెరవండి.

1. **Filters** కింద, **From Date**, **To Date** ఎంచుకోండి. బసలకు ఇవి check-out తేదీలు, బిల్లులకు బిల్లు తేదీలు.
2. **Search** నొక్కండి. **Today** ఈ రోజువి మాత్రమే చూపిస్తుంది. **Refresh** లిస్ట్‌ను మళ్లీ లోడ్ చేస్తుంది.
3. Check-out అయిన బసలు చూడటానికి **Completed Bookings** నొక్కండి, ఇప్పటికే తయారైన బిల్లులు చూడటానికి **Generated Bills** నొక్కండి.
4. సెర్చ్ బాక్స్‌లో booking ID లేదా గెస్ట్ పేరు టైప్ చేయండి. ఒక బసపై, **Bill Status** **Not Billed** లేదా **Billed** అని చూపిస్తుంది. **Generate Bill** లేదా **View Bill** నొక్కండి.

## సాధారణ సమస్యలు

- **"From Date cannot be after To Date."** (From Date, To Date తర్వాత ఉండకూడదు) తేదీలు మార్చండి.
- **ఒక బస లిస్ట్‌లో లేదు.** Check-out అయిన బసలు మాత్రమే, వాటి check-out తేదీ ప్రకారం కనిపిస్తాయి. తేదీల పరిధి పెంచండి.
:::

::: section generate-bill
title: ఒక బసకు బిల్లు తయారు చేయడం
routes: /GenerateBill
for: 
roles: 
designations: 
minutes: 4
source-hash: 71ab93bc25bfd36f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
Check-out తర్వాత, ఆ బసకు tax invoice తయారు చేయండి. ప్రతి బసకు ఒక్కసారి మాత్రమే బిల్లు చేయగలరు.

1. **Completed Bookings** లో, **Not Billed** అని చూపించే బసపై **Generate Bill** నొక్కండి. **New Bill** స్క్రీన్ తెరుచుకుంటుంది.
2. **Booking Summary** చెక్ చేయండి: **Booking**, **Guest**, **Company Name**, **GSTIN**, **Guest House**, **Room**, **Stay**.
3. **Bill Items** కింద, రూమ్ లైన్ ముందే నిండి ఉంటుంది. ఉంటే **Food Bill**, **Additional Beds** మొత్తాలు ఎంటర్ చేయండి.
4. లాండ్రీ లేదా ఇతర ఛార్జీల కోసం, **Add Charge** నొక్కండి. వివరణ, **Qty**, **Rate**, **CGST %**, **SGST %** ఎంటర్ చేయండి. జోడించిన లైన్‌ను **Remove** తీసేస్తుంది.
5. **Totals, Discount & Payment** కింద, అవసరమైతే **Discount**, **Advance Payment**, **Round Off** ఎంటర్ చేయండి. **Payment Method** ఎంచుకుని, **Payment received** టిక్ చేసి, **Remarks** రాయండి.
6. **Balance Payable** చెక్ చేసి, తర్వాత **Generate Bill** నొక్కండి. కొత్త బిల్లు **Generated Bills** ట్యాబ్‌లో కనిపిస్తుంది.
7. Invoice చూడటానికి **View** నొక్కండి, తర్వాత **Download PDF** లేదా **Print** నొక్కండి. లిస్ట్‌లోని **PDF** దాన్ని నేరుగా డౌన్‌లోడ్ చేస్తుంది.

> Careful: **Generate Bill** నొక్కే ముందు ప్రతి మొత్తాన్ని చెక్ చేయండి. అదే బసకు మళ్లీ బిల్లు చేయలేరు.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Food Bill** | బస సమయంలో ఇచ్చిన భోజనం సంఖ్య, రేటు. |
| **Add Charge** | ఇంకేదైనా ఛార్జీ, దాని సొంత CGST %, SGST % తో. |
| **Advance Payment** | ఈ బసకు ఇప్పటికే కట్టిన డబ్బు. తెలిస్తే ముందే నిండి ఉంటుంది. |
| **Payment Method** | UPI, Card, Net Banking లేదా Cash. ఉద్యోగి ఉచిత బసకు Complimentary అని కూడా చూపిస్తుంది. |

## సాధారణ సమస్యలు

- **"Discount cannot exceed Grand Total."** లేదా **"Advance Payment cannot exceed Grand Total."** (Grand Total కంటే ఎక్కువ ఉండకూడదు) మొత్తం తగ్గించండి.
- **"Balance Payable cannot be negative."** (Balance Payable మైనస్‌లో ఉండకూడదు) **Discount**, **Advance Payment**, **Round Off** చెక్ చేయండి.
- **"A bill has already been generated for this booking".** (ఈ బుకింగ్‌కు ఇప్పటికే బిల్లు తయారైంది) బదులుగా **View Bill** తో దాన్ని తెరవండి.
:::

::: section bill-list
title: తయారైన అన్ని బిల్లులు చూడటం
routes: /BillList
for: 
roles: 
designations: 
minutes: 1
source-hash: 863b55025a37bbd6
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Bill List** స్క్రీన్ తయారైన ప్రతి invoice ను, కొత్తవి ముందుగా చూపిస్తుంది. ఇవే బిల్లులు **Generate Bill** లోని **Generated Bills** ట్యాబ్‌లో కూడా ఉంటాయి.

1. టేబుల్ చూడండి: **Bill #**, **Booking**, **Guest**, **Guest House**, **Room**, **Bill Date**, తేదీలు, **Total**, **Balance**, **Payment**.
2. Invoice తెరవడానికి **View** నొక్కండి. అందులో **Download PDF** లేదా **Print** నొక్కండి.
3. Invoice ను వెంటనే డౌన్‌లోడ్ చేయడానికి **PDF** నొక్కండి.
4. ఇంకా వరుసలు చూడటానికి **Show more** నొక్కండి.
5. కొత్త బిల్లు తయారు చేయడానికి **Generate Bill** నొక్కండి, లేదా మళ్లీ లోడ్ చేయడానికి **Refresh** నొక్కండి.

## సాధారణ సమస్యలు

- **"No bills yet".** (ఇంకా బిల్లులు లేవు) ఇప్పటివరకు ఏ బిల్లూ తయారు కాలేదు. **Generate Bill** నుంచి ఒకటి తయారు చేయండి.
:::

::: section cancellation-requests
title: క్యాన్సిలేషన్ రిక్వెస్ట్‌లను ఆమోదించడం లేదా తిరస్కరించడం
routes: /GuestHouseCancellations
for: 
roles: 
designations: 
minutes: 3
source-hash: abf61f8cc73c49f2
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఎవరైనా బుకింగ్ క్యాన్సిల్ చేయమని అడిగితే, ఆ రిక్వెస్ట్ ఇక్కడికి వస్తుంది. మెనూ నుంచి తెరవండి: **SDWA**, తర్వాత **Cancellation Requests**.

1. ఒక ట్యాబ్ నొక్కండి: **Pending Approval**, **Approved**, **Rejected** లేదా **All**.
2. **Search requests...** లో టైప్ చేయండి, లేదా **All Guest Houses** లో ఒక గెస్ట్ హౌస్ ఎంచుకోండి.
3. వరుస చదవండి: బుకింగ్, గెస్ట్, బస, కట్టిన మొత్తం (**Paid**), **Refund**, **Reason**, ఎప్పుడు అడిగారు (**Requested**).
4. ఒప్పుకోవడానికి, **Approve** నొక్కి, బాక్స్ చదివి, **Approve Cancellation** నొక్కండి. బుకింగ్ క్యాన్సిల్ అవుతుంది, రూమ్ ఖాళీ అవుతుంది.
5. తిరస్కరించడానికి, **Reject** నొక్కి, **Reason for rejection** రాసి, **Reject Request** నొక్కండి. బుకింగ్ అలాగే కొనసాగుతుంది.
6. రీఫండ్ ఆమోదించిన తర్వాత, 2 పని దినాల్లో దాన్ని మొదట పేమెంట్ చేసిన మార్గానికే చేతితో పంపండి.

> Careful: యాప్ రీఫండ్‌ను సొంతంగా పంపదు. ఎవరో ఒకరు దాన్ని యాప్ బయట పంపాలి.

## సాధారణ సమస్యలు

- **"You do not have permission to review cancellation requests."** (క్యాన్సిలేషన్ రిక్వెస్ట్‌లు చూడటానికి మీకు అనుమతి లేదు) ఈ అనుమతి కోసం మీ administrator ని అడగండి.
- **"The guest is checked in. Check the guest out instead of cancelling."** (గెస్ట్ check-in అయ్యారు, క్యాన్సిల్ బదులు check-out చేయండి) **Front Office** లో **Check-Out** వాడండి.
- **Reject బటన్ లేదు.** ఈ రిక్వెస్ట్ రీఫండ్ ఇప్పటికే జరుగుతోంది.
:::

::: section guest-house-master
title: గెస్ట్ హౌస్‌లు, రూమ్ రకాలు, రూమ్‌లు జోడించడం
routes: /GuestHouseMaster
for: 
roles: 
designations: 
minutes: 4
source-hash: 7f6f2fee8bd4e156
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ప్రజలు ఏమి బుక్ చేయవచ్చో సెట్ చేయడానికి admin లు **Guest House & Room Master** వాడతారు. మెనూ నుంచి తెరవండి: **SDWA**, తర్వాత **Guest House**. ముందు గెస్ట్ హౌస్, తర్వాత రూమ్ రకాలు, తర్వాత రూమ్‌లు జోడించండి.

1. **Guest House** ట్యాబ్‌లో, **Name**, తెలిస్తే **Address**, **Phone Number**, **State**, **Description** ఎంటర్ చేయండి. ఫోటో జోడించడానికి **Upload Image** నొక్కండి.
2. **Room Type** ట్యాబ్‌లో, **Room Type Name** ఎంటర్ చేయండి, ఉదాహరణకు AC Deluxe Room.
3. **Rooms** ట్యాబ్‌లో, **Guest House**, **Room Type (Master)** ఎంచుకుని, తర్వాత **Room Type**, **Room Number**, **No. of Rooms**, **Rate (₹)** ఎంటర్ చేయండి.
4. స్విచ్‌ను **Active** లేదా **Inactive** కి పెట్టి **Save** నొక్కండి. **Cancel** ఫారం క్లియర్ చేస్తుంది.
5. **Guest House** లేదా **Rooms** ట్యాబ్‌లో చాలా ఒకేసారి జోడించడానికి, **Download Template** నొక్కి, దాన్ని నింపి, తర్వాత **Upload File** నొక్కండి.
6. టేబుల్‌లో, సెర్చ్ బాక్స్, **All Status** వాడండి. మార్చడానికి **Active** లేదా **Inactive** నొక్కండి, ఎడిట్ చేయడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Careful: డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు. కొంత కాలం బుకింగ్‌లు ఆపాలంటే, బదులుగా **Inactive** కి పెట్టండి. Inactive గెస్ట్ హౌస్‌లు బుకింగ్‌కు కనిపించవు.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Name** | గెస్ట్ హౌస్ పేరు. తప్పనిసరి. |
| **State** | గెస్ట్ హౌస్ ఉన్న రాష్ట్రం. ఆ రాష్ట్రం receptionist లు దాన్ని చూసుకుంటారు. |
| **No. of Rooms** | ఈ రకం రూమ్‌లు ఎన్ని. 1 లేదా అంతకంటే ఎక్కువ పూర్ణ సంఖ్య. తప్పనిసరి. |
| **Rate (₹)** | ఒక రాత్రికి ధర. తప్పనిసరి. |

## సాధారణ సమస్యలు

- **"No. of Rooms must be a positive whole number."** (No. of Rooms 1 లేదా అంతకంటే ఎక్కువ పూర్ణ సంఖ్య ఉండాలి) 1 లేదా అంతకంటే ఎక్కువ ఎంటర్ చేయండి.
- **"Rate must be a valid amount."** (Rate సరైన మొత్తం ఉండాలి) ఒక సంఖ్య ఎంటర్ చేయండి, ఉదాహరణకు 1200.
- **నాకు టేబుల్‌లు కనిపిస్తున్నాయి, కానీ ఫారం లేదు.** మీరు చూడటానికి మాత్రమే అనుమతి ఉంది. జోడించే లేదా మార్చే హక్కుల కోసం మీ administrator ని అడగండి.
:::

::: section design-previews
title: ఇంకా తయారవుతున్న స్క్రీన్‌లు
routes: /GuestBooking, /RoomDetails, /CancelledBookingDetails, /PaymentManagement
for: 
roles: 
designations: 
minutes: 1
source-hash: 0ad754e112fa7bee
confidence: ok

**ఈ స్క్రీన్ దేనికి**
కొన్ని స్క్రీన్‌లు ఇంకా మొదటి డిజైన్‌లు మాత్రమే. అవి మీ డేటా కాకుండా, మారని నమూనా సమాచారం చూపిస్తాయి. వాటి బటన్‌లు ఏదీ సేవ్ చేయవు.

1. నమూనా గెస్ట్ వివరాలతో **Review Your Booking**, **AC Deluxe Room** రూమ్ వివరాలు, **Cancelled Booking Details** లేదా **Payment Management** తెరిచి ఉంటే, మీరు ఈ డిజైన్ స్క్రీన్‌లలో ఒకదానిలో ఉన్నారు.
2. అక్కడ ఏ డేటా ఎంటర్ చేయకండి, నమ్మకండి.
3. రూమ్ బుక్ చేయడానికి **Book a Room** వాడండి. మీ బుకింగ్‌లు చూడటానికి, క్యాన్సిల్ చేయడానికి లేదా ట్రాక్ చేయడానికి **My Bookings / Cancellation** వాడండి.

## సాధారణ సమస్యలు

- **సంఖ్యలు ఎప్పుడూ మారవు.** ఇది నమూనా సమాచారం. పైన చెప్పిన అసలు స్క్రీన్‌లు వాడండి.
:::
