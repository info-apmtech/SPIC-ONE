---
module: reports
title: రిపోర్టులు
icon: bi-bar-chart
order: 116
language: te
category: రిపోర్టులు
note: కొత్త గైడ్
meta-source-hash: c3d58c99d0724041
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 0cd39d62cdd793e3
confidence: ok

ముఖ్యమైన లెక్కలు ఒకే చోట చూడండి. **Admin Dashboard** సంక్షేమ దరఖాస్తులు, డీలర్లు, గెస్ట్ హౌస్ బుకింగ్‌లను చూపిస్తుంది. **Reports Center** రిపోర్టుల లిస్ట్ చూపిస్తుంది.

- dashboard | icon: bi-speedometer2 | color: #1d4ed8
  title: Admin Dashboard
  text: సంక్షేమ దరఖాస్తులు, డీలర్లు, గెస్ట్ హౌస్ లెక్కలు ఒకే పేజీలో.
- queue | icon: bi-list-check | color: #d97706
  title: పెండింగ్ క్యూ
  text: ఏ దరఖాస్తులు ఎదురుచూస్తున్నాయి, ఏ దశలో, ఎన్ని రోజులుగా అని చూడండి.
- reports | icon: bi-file-earmark-bar-graph | color: #7c3aed
  title: Reports Center
  text: రిపోర్టుల లిస్ట్. ఇది ఇంకా తయారవుతోంది, కాబట్టి కనిపించే రిపోర్టులు ఉదాహరణలు మాత్రమే.
:::

::: section flow
title: ఈ స్క్రీన్‌లను ఎలా వాడాలి
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 7963de51aaa7ddbb
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- office | type: lane | color: #1d4ed8 | roles: Admin, CorporateAdmin, Director
  title: SDWA Admin, ఆఫీసు
  sub: SDWA Admin · Admin · Director
- approvers | type: lane | color: #d97706 | roles: MO, RM, SMM
  title: ఆమోదించేవారు
  sub: MO · RM · SM · SDWA · SDWA Admin
- open | lane: office | col: 1 | icon: bi-speedometer2 | color: #1d4ed8 | next: check | screen: admin-dashboard
  title: Admin Dashboard తెరవడం
  text: మెనూలో **SDWA** తెరిచి **Admin Dashboard** ఎంచుకోండి.
- check | lane: office | col: 2 | icon: bi-list-check | color: #d97706 | next: act, reports | screen: admin-dashboard
  title: లెక్కలు, క్యూ చెక్ చేయడం
  text: కార్డులు, **Approval Workflow Overview**, **Pending Action Queue (SDWA)** చూడండి.
- act | lane: approvers | col: 3 | icon: bi-patch-check | color: #16a34a
  title: Scheme Approval స్క్రీన్‌లో ఆమోదించడం
  text: **Quick actions** కింద ఉన్న **Review Pending Approvals** నొక్కండి. ఆమోదించడం **Scheme Approval** స్క్రీన్‌లోనే జరుగుతుంది, డాష్‌బోర్డ్‌లో కాదు.
- reports | lane: office | col: 3 | icon: bi-file-earmark-bar-graph | color: #7c3aed | screen: reports-center
  title: రిపోర్టుల లిస్ట్ చూడటం
  text: రిపోర్టులు చూడటానికి **Reports Center** తెరవండి. ఇది ఇంకా తయారవుతోంది.
- no-data | lane: office | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: check | to: open | screen: admin-dashboard
  title: డేటా లోడ్ కాలేదు
  text: పైన ఎర్ర బాక్స్‌లో **Could not load** అని వస్తుంది, బాక్స్‌లలో **—** కనిపిస్తుంది. డాష్‌బోర్డ్‌ను కాసేపటి తర్వాత మళ్లీ తెరవండి.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9575d21878652ad1
confidence: ok

- pending-mo | color: #d97706
  label: Pending MO
  text: చెక్ చేయడానికి MO దగ్గర ఉంది.
- pending-rm | color: #d97706
  label: Pending RM
  text: చెక్ చేయడానికి RM దగ్గర ఉంది.
- pending-sm | color: #d97706
  label: Pending SM
  text: చెక్ చేయడానికి SM దగ్గర ఉంది.
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: ఆమోదం కోసం SDWA దగ్గర ఉంది.
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: చివరి ఆమోదం కోసం SDWA Admin దగ్గర ఉంది.
- high | color: #dc2626
  label: High
  text: క్యూలో ప్రాధాన్యత. డీలర్ 5 లేదా అంతకంటే ఎక్కువ రోజుల క్రితం దరఖాస్తు చేశారు.
- medium | color: #ea580c
  label: Medium
  text: క్యూలో ప్రాధాన్యత. డీలర్ 2 నుంచి 4 రోజుల క్రితం దరఖాస్తు చేశారు.
- low | color: #16a34a
  label: Low
  text: క్యూలో ప్రాధాన్యత. డీలర్ ఈ రోజు లేదా నిన్న దరఖాస్తు చేశారు.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 029f192b703c661f
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీకు అనుమతి ఉన్న స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- sdwa-admin | color: #1d4ed8
  title: SDWA Admin
  - Admin Dashboard లో అన్ని సంక్షేమ దరఖాస్తులను గమనించడం
  - **Quick actions** నుంచి పెండింగ్ ఆమోదాలను తెరవడం
  - గెస్ట్ హౌస్ బుకింగ్‌లు చెక్ చేయడం
- office | color: #0d9488 | roles: Admin, CorporateAdmin, Director
  title: Admin, Corporate Admin, Director
  - ఇంకా చెక్ అవుతున్న ప్రతి దరఖాస్తును చూడటం
  - డీలర్, గెస్ట్ హౌస్ లెక్కలు చూడటం
  - Reports Center తెరవడం
- approvers | color: #d97706 | roles: MO, RM, SMM
  title: MO, RM, SM
  - మీరు Admin Dashboard తెరవగలిగితే, మీ దశలో ఉన్న దరఖాస్తులు క్యూలో కనిపిస్తాయి
  - మీ HQ, రీజియన్ లేదా రాష్ట్రం మాత్రమే మీకు కనిపిస్తుంది
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7222902e690b16b2
confidence: ok

- dash | popular: true
  q: కొన్ని బాక్స్‌లలో నంబర్‌కు బదులు — కనిపిస్తోంది
  a: డేటా లోడ్ కాకపోయి ఉండొచ్చు (ఏ భాగమో పైన ఉన్న ఎర్ర బాక్స్ చెబుతుంది), లేదా ఆ బాక్స్‌కు ఇంకా డేటా లేకపోవచ్చు. **Active Schemes**, **Benefit Released**, **SLA Delayed**, **Upcoming Check-ins**, **Cancelled**, **Revenue Collected**, **Finance & Benefit Release** బాక్స్‌లు ప్రస్తుతానికి ఎప్పుడూ — అనే చూపిస్తాయి.
- download | popular: true
  q: Reports Center లో Download నొక్కితే ఏమీ జరగడం లేదు
  a: Reports Center ఇంకా తయారవుతోంది. **Download** ఇంకా ఫైల్ తయారు చేయదు. అందులో ఉన్న రిపోర్టులు, లెక్కలు ఉదాహరణలు మాత్రమే, మీ డేటా కాదు.
- icons
  q: క్యూలో టిక్, X ఐకాన్‌లు నొక్కితే ఏమీ జరగడం లేదు
  a: ఈ ఐకాన్‌లు ఇంకా పని చేయవు. **Quick actions** కింద ఉన్న **Review Pending Approvals** నొక్కి, **Scheme Approval** స్క్రీన్‌లో ఆమోదించండి లేదా తిరస్కరించండి.
- nothing-pending
  q: క్యూలో "Nothing pending" అని కనిపిస్తోంది
  a: మీ దశలో ఏ దరఖాస్తూ ఎదురుచూడటం లేదు. MO, RM, SM తమ సొంత HQ, రీజియన్ లేదా రాష్ట్రం మాత్రమే చూస్తారు.
- could-not-load
  q: "Could not load: welfare approvals" అని కనిపిస్తోంది
  a: సంక్షేమ డేటా MO, RM, SM, SDWA, SDWA Admin, Admin, Corporate Admin, Director కి మాత్రమే కనిపిస్తుంది. మీరు దీన్ని చూడాల్సినవారైతే, మీ యాక్సెస్ చెక్ చేయమని మీ admin ని అడగండి, తర్వాత పేజీని మళ్లీ తెరవండి.
:::

::: section admin-dashboard
title: Admin Dashboard చదవడం
routes: /ReportDashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=rpt-kpis] | 2 > [data-help=rpt-workflow] | 3 > [data-help=rpt-queue] | 4 > [data-help=rpt-charts] | 5 > [data-help=rpt-guesthouse] | 6 > [data-help=rpt-quick]
minutes: 3
source-hash: c608b65421e5a6ab
confidence: ok

**ఈ స్క్రీన్ దేనికి**
SDWA Admin కోసం ఉన్న **Dashboard** సంక్షేమ దరఖాస్తులు, డీలర్లు, గెస్ట్ హౌస్ బుకింగ్‌లను ఒకే పేజీలో చూపిస్తుంది. మెనూలో **SDWA** > **Admin Dashboard** ద్వారా దీన్ని తెరవండి. ఇక్కడ మీరు చూడటం మాత్రమే చేస్తారు. ఆమోదించడం **Scheme Approval** స్క్రీన్‌లో జరుగుతుంది.

1. పైన ఉన్న కార్డులు చూడండి: **Total Welfare Applications**, **Pending Final Approval**, **Approved Applications**, **Rejected Applications**, **Total Dealers**, **Guest House Bookings**.
2. **Approval Workflow Overview** లో, **Dealer** నుంచి **Completed** వరకు ఉన్న దశలు, ఎన్ని దరఖాస్తులు **Pending at MO**, **Pending at RM**, **Pending at SMM**, **Pending at SDWA**, **Rejected** లో ఉన్నాయో చూడండి.
3. **Pending Action Queue (SDWA)** లో, ఎదురుచూస్తున్న ప్రతి దరఖాస్తును దాని **Current Stage**, **Pending Since**, **Priority** తో చూడండి. ఇంకా వరుసలు చూడటానికి **Show more** నొక్కండి.
4. ప్రతి స్కీమ్‌కు ఎన్ని దరఖాస్తులు ఉన్నాయో **Scheme Distribution** చూపిస్తుంది. నెలవారీ దరఖాస్తులను **Monthly Application Trend** చూపిస్తుంది. వాటి కింద డీలర్ లెక్కలు, **Regional Distribution** టేబుల్ ఉంటాయి.
5. బుకింగ్‌లను, ప్రతి గెస్ట్ హౌస్ ఎంత నిండి ఉందో **Guest House Overview** చూపిస్తుంది. ఫ్రంట్ ఆఫీస్ తెరవడానికి **View All Bookings** నొక్కండి.
6. **Quick actions** కింద, దరఖాస్తులను ఆమోదించడానికి **Review Pending Approvals** నొక్కండి. **Dealer Directory**, **Manage Schemes**, **Guest House Bookings** ఆ స్క్రీన్‌లను తెరుస్తాయి.

> Careful: క్యూలో ఉన్న కన్ను, టిక్, X, డౌన్‌లోడ్ ఐకాన్‌లు, **Search zones...** బాక్స్‌లు, **Generate Reports**, **Gallery** కార్డులు ఇంకా పని చేయవు.

## స్క్రీన్‌లోని ఇతర భాగాలు

- **Recent Activities** చివరి 5 సంఘటనలను చూపిస్తుంది, ఉదాహరణకు కొత్త దరఖాస్తు, ఒక ఆమోదం లేదా గెస్ట్ హౌస్ బిల్లు.
- **Finance & Benefit Release** లో ఇంకా డేటా లేదు. అది — అని, "No payment releases" అని చూపిస్తుంది.

## సాధారణ సమస్యలు

- **ఎర్ర బాక్స్‌లో "Could not load" అని ఉంది.** డేటాలో ఆ భాగం లోడ్ కాలేదు. దాని బాక్స్‌లలో — కనిపిస్తుంది. కాసేపటి తర్వాత పేజీని మళ్లీ తెరవండి.
- **Pending at MO లెక్క ఎక్కువగా కనిపిస్తోంది.** సరిచేయడం కోసం డీలర్‌కు తిరిగి పంపిన దరఖాస్తులను కూడా అది లెక్కపెడుతుంది.
- **Pending Since మరీ ఎక్కువ రోజులు చూపిస్తోంది.** దరఖాస్తు ఈ దశకు వచ్చినప్పటి నుంచి కాదు, డీలర్ దరఖాస్తు చేసినప్పటి నుంచి అది రోజులను లెక్కపెడుతుంది.
- **క్యూలో "Nothing pending" అని ఉంది.** ఇప్పుడు మీ దశలో ఏ దరఖాస్తూ ఎదురుచూడటం లేదు.
:::

::: section reports-center
title: Reports Center చూడటం
routes: /ReportsCenter
for: 
roles: 
designations: 
minutes: 1
source-hash: fea64cc958f10b95
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Reports Center** లో బుకింగ్, బిల్లింగ్, ఆమోదం, డీలర్ రిపోర్టులను చూసి డౌన్‌లోడ్ చేసుకోగలుగుతారు. ఇది ఇంకా తయారవుతోంది: ఇందులో ఉన్న లెక్కలు, రిపోర్టులు, తేదీలు ఉదాహరణలు మాత్రమే, ఫిల్టర్‌లు, డౌన్‌లోడ్‌లు ఇంకా పని చేయవు.

1. పైన ఉన్న కార్డులు చూడండి: **Total Reports**, **Booking Reports**, **Billing Reports**, **Approval Reports**.
2. ట్యాబ్‌లు (**All Reports**, **Booking Reports**, **Billing Reports**, **Approval Reports**), **Search** బాక్స్, **All Categories**, **All Formats** లిస్ట్‌లు కనిపిస్తాయి, కానీ అవి ఇంకా లిస్ట్‌ను మార్చవు.
3. ప్రతి రిపోర్ట్ కార్డు రిపోర్ట్ పేరు, అందులో ఏమి ఉందో, **Available Formats**, ఒక తేదీ చూపిస్తుంది. కార్డులు: **Dealer Information**, **Tirupathi Bookings**, **T-Nagar Bills**, **MO Reports**, **RM Reports**, **Tirupathi Bills**.
4. **Category**, **Records**, **File Types**, **Generated By**, ఒక చిన్న టేబుల్‌తో ఉన్న ఉదాహరణను తెరవడానికి **Preview** నొక్కండి. మూసివేయడానికి **Close** లేదా X నొక్కండి.
5. **Download**, **Download PDF**, **Download Excel** ఇంకా ఫైల్ తయారు చేయవు.

> Careful: ఈ స్క్రీన్‌లోని లెక్కలను వాడకండి. అవి ఉదాహరణలు, మీ నిజమైన డేటా కాదు.

## సాధారణ సమస్యలు

- **Download నొక్కితే ఏమీ జరగడం లేదు.** డౌన్‌లోడ్‌లు ఇంకా సిద్ధంగా లేవు.
- **ప్రతి Preview లో Dealer Information నే కనిపిస్తోంది.** ప్రస్తుతానికి అన్ని Preview బటన్‌లు ఒకే ఉదాహరణను తెరుస్తాయి.
- **ట్యాబ్‌లు, సెర్చ్ లిస్ట్‌ను మార్చడం లేదు.** అవి ఇంకా పని చేయడం లేదు.
:::
