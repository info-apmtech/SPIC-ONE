---
module: farm-operations
title: ఫార్మ్ కార్యకలాపాలు
icon: bi-tree
order: 109
language: te
category: మట్టి పరీక్ష
note: కొత్త గైడ్
meta-source-hash: 4c57dbd8a4469315
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 62c60a0ea650a93e
confidence: ok

SPIC ఫార్మ్ లెక్కలన్నీ ఒకే చోట ఉంచుకోండి. ప్రొడక్ట్‌లు, వాటి రేట్లు సెట్ చేయండి, ప్రతి 15 రోజులకు ఒకసారి స్టాక్, అమ్మకాలు, ఖర్చులు నమోదు చేయండి, ఏడాది మొత్తం ఆదాయం, ఖర్చు, లాభం, స్టాక్ చూడండి.

- products | icon: bi-tags | color: #15803d
  title: ప్రొడక్ట్‌లు, రేట్లు
  text: Farm Office కొత్త ప్రొడక్ట్ లేదా కొత్త రేట్ అడుగుతుంది. Farm Admin దానికి ఆమోదం ఇస్తారు.
- report | icon: bi-journal-text | color: #d97706
  title: ప్రతి 15 రోజులకు ఒక రిపోర్ట్
  text: 1 నుంచి 15 వరకు, 16 నుంచి నెలాఖరు వరకు, ఇన్వెంటరీ (స్టాక్), ఆదాయం, ఖర్చు ఎంటర్ చేసి, సబ్మిట్ చేయండి.
- summaries | icon: bi-graph-up-arrow | color: #2563eb
  title: ఏడాది సమ్మరీలు
  text: ఫైనాన్స్, ఇన్వెంటరీ, ఆదాయం, ఖర్చు సమ్మరీలు చూసి, వాటిని Excel గా డౌన్‌లోడ్ చేయండి.
:::

::: section flow
title: ఫార్మ్ లెక్కలు ఎలా ముందుకు వెళ్తాయి, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 4948b5894215c5ba
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- office | type: lane | color: #15803d
  title: Farm Office
  sub: రిక్వెస్ట్‌లు, 15 రోజుల రిపోర్ట్‌లు
- admin | type: lane | color: #ea580c
  title: Farm Admin
  sub: ఆమోదాలు, Product Master
- request | lane: office | col: 1 | icon: bi-tags | color: #d97706 | next: approve | screen: product-rates
  title: ప్రొడక్ట్ లేదా రేట్ అడగడం
  status: Pending
  text: **Product & Rate Management** లో, కొత్త ప్రొడక్ట్ లేదా రేట్ మార్పు అడగండి. అది Farm Admin కోసం ఎదురుచూస్తుంది.
- approve | lane: admin | col: 2 | icon: bi-patch-check | color: #16a34a | next: entry | return: request | screen: rate-approvals
  title: రేట్‌కు ఆమోదం ఇవ్వడం
  status: Approved
  text: Farm Admin ధర, యూనిట్ చెక్ చేసి ఆమోదిస్తారు. తర్వాత ఆ ప్రొడక్ట్, రేట్ 15 రోజుల రిపోర్ట్‌లో వాడబడతాయి.
- rejected | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: approve | to: request | screen: rate-approvals
  title: రిక్వెస్ట్ తిరస్కరించబడింది
  status: Rejected
  text: Farm Admin కారణం రాసి దాన్ని తిరస్కరిస్తారు. Farm Office రిక్వెస్ట్‌ను మార్చి **Resubmit Request** నొక్కవచ్చు.
- entry | lane: office | col: 3 | icon: bi-journal-text | color: #64748b | next: submit | screen: fifteen-day-entry
  title: 15 రోజుల రిపోర్ట్ నింపడం
  status: Draft
  text: ఆ కాలానికి ఇన్వెంటరీ, ఆదాయం, ఖర్చు ఎంటర్ చేయండి. ప్రతి దశ డ్రాఫ్ట్‌గా సేవ్ అవుతుంది.
- submit | lane: office | col: 4 | icon: bi-send-check | color: #15803d | next: summaries | screen: report-review
  title: చెక్ చేసి సబ్మిట్ చేయడం
  status: Submitted
  text: రివ్యూ పేజీ చెక్ చేసి **Submit 15-Day Report** నొక్కండి. సబ్మిట్ చేసిన రిపోర్ట్‌ను ఎప్పుడైనా తెరవవచ్చు.
- summaries | lane: office | col: 5 | icon: bi-graph-up-arrow | color: #2563eb | screen: finance-summary
  title: సమ్మరీలు చూడటం
  text: ఫైనాన్స్, ఇన్వెంటరీ, ఆదాయం, ఖర్చు సమ్మరీలు ఆ ఆర్థిక సంవత్సరంలోని ప్రతి రిపోర్ట్‌ను కలిపి చూపిస్తాయి.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: e9a76a64316c65fc
confidence: ok

- no-request | color: #64748b
  label: No Request
  text: ఈ ప్రొడక్ట్‌కు ఏ రిక్వెస్ట్ లేదు. ఇప్పుడున్న రేటే వాడబడుతుంది.
- pending | color: #d97706
  label: Pending
  text: రిక్వెస్ట్ Farm Admin కోసం ఎదురుచూస్తోంది.
- approved | color: #16a34a
  label: Approved
  text: Farm Admin ఆమోదించారు. కొత్త ప్రొడక్ట్ లేదా రేట్ వాడకంలో ఉంది.
- rejected | color: #dc2626
  label: Rejected
  text: Farm Admin తిరస్కరించారు. కారణం చదివి, మార్చి, మళ్లీ సబ్మిట్ చేయండి.
- active | color: #15803d
  label: Active
  text: ఈ ప్రొడక్ట్ Product Master లో వాడకంలో ఉంది.
- inactive | color: #475569
  label: Inactive
  text: ఈ ప్రొడక్ట్ ఆపివేయబడింది. కొత్త రిక్వెస్ట్‌లలో కనిపించదు.
- draft | color: #d97706
  label: Draft
  text: 15 రోజుల రిపోర్ట్ సేవ్ అయ్యింది కానీ సబ్మిట్ చేయలేదు. ఇంకా మార్చవచ్చు.
- submitted | color: #16a34a
  label: Submitted
  text: 15 రోజుల రిపోర్ట్ ఫైనల్ అయింది. ఒక admin మాత్రమే దాన్ని మళ్లీ తెరవగలరు.
- ready-to-submit | color: #2563eb
  label: Ready to submit
  text: రివ్యూ దశలో, రిపోర్ట్ సిద్ధంగా ఉంది. Finance Summary లో, కొన్ని రిపోర్ట్‌లు ఇంకా సబ్మిట్ కాలేదు.
- ready-update | color: #d97706
  label: Ready for Update
  text: ఇప్పటి కాలం ఎంట్రీ కోసం తెరిచి ఉంది.
- completed | color: #16a34a
  label: Completed
  text: ముందటి కాలం, ఇన్వెంటరీ దశలో కనిపిస్తుంది.
- upcoming | color: #7c3aed
  label: Upcoming
  text: తర్వాతి కాలం, ఇది అయిపోయాక తెరుచుకుంటుంది.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 6c6e07dec3a74424
confidence: ok

మీ హోదా (designation) కి అనుమతి ఉన్న స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- office | color: #15803d
  title: Farm Office
  - కొత్త ప్రొడక్ట్‌లు, రేట్ మార్పులు అడగడం
  - 15 రోజుల రిపోర్ట్ నింపి సబ్మిట్ చేయడం
  - ఫైనాన్స్, ఇన్వెంటరీ, ఆదాయం, ఖర్చు సమ్మరీలు చెక్ చేయడం
- admin | color: #ea580c
  title: Farm Admin
  - ప్రొడక్ట్, రేట్ రిక్వెస్ట్‌లను ఆమోదించడం లేదా తిరస్కరించడం
  - Product Master లో ప్రొడక్ట్‌లు, రేట్లు సరిచేయడం
  - ప్రతి ప్రొడక్ట్ రేట్ హిస్టరీ చూడటం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9b23a5f40e2ecf43
confidence: ok

- no-product | popular: true
  q: నా 15 రోజుల రిపోర్ట్‌లో ఒక ప్రొడక్ట్ కనిపించడం లేదు
  a: ఆమోదం వచ్చిన ప్రొడక్ట్‌లు మాత్రమే కనిపిస్తాయి. **Product & Rate Management** లో దాన్ని అడిగి, Farm Admin ఆమోదించే వరకు ఆగండి.
- change-submitted | popular: true
  q: రిపోర్ట్ సబ్మిట్ చేశాక ఒక తప్పు కనిపించింది
  a: సబ్మిట్ చేసిన రిపోర్ట్‌ను మార్చలేరు. దాన్ని మళ్లీ తెరవమని మీ admin ని అడగండి.
- sold-locked
  q: ఇన్వెంటరీ దశలో Sold కాలమ్‌లో టైప్ చేయలేకపోతున్నాను
  a: **Sold** అనేది **Income** దశలో మీరు ఎంటర్ చేసే **Sold Quantity** నుంచి వస్తుంది. అక్కడే మార్చండి.
- no-approvals
  q: Farm Office రిక్వెస్ట్‌లు పంపినా, Pending Approvals లిస్ట్ ఖాళీగా ఉంది
  a: **From Date**, **To Date** ఫిల్టర్‌లు ఈరోజు తేదీ నుంచి మొదలవుతాయి. ముందటి తేదీలు ఎంచుకుని **Apply Filters** నొక్కండి.
- cannot-edit
  q: "This product is still awaiting approval - you can't edit it yet." అని కనిపిస్తోంది
  a: Farm Admin ఆమోదించే వరకు కొత్త ప్రొడక్ట్‌ను మార్చలేరు. వారి నిర్ణయం కోసం ఆగండి.
:::

::: section farm-dashboard
title: ఫార్మ్‌ను ఒక్క చూపులో చూడటం
routes: /FarmDashboard, /FarmOperations
for: 
roles: 
designations: 
minutes: 2
source-hash: 46e18c8ee7fa0ff8
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఫార్మ్ డాష్‌బోర్డ్ (మెనూలో **Farm Operations**) ఎంచుకున్న ఆర్థిక సంవత్సరానికి డబ్బు, స్టాక్ చూపిస్తుంది. ఇక్కడి నుంచే మిగతా ఫార్మ్ స్క్రీన్‌లన్నింటికీ వెళ్లవచ్చు.

1. పైన కుడివైపు **Financial Year** ఎంచుకోండి.
2. కార్డులు చదవండి: **Total Budget**, **Total Income Till Date**, **Total Expense Till Date**, **Net Profit Till Date**, **ROI Till Date**.
3. **Quick Action** కింద, **Product Management**, **Inventory Summary**, **Income Summary**, **Expense Summary** లేదా **Finance Summary** నొక్కి ఆ స్క్రీన్ తెరవండి.
4. **Next 15-day Period** లో **Start 15 Days Entry** నొక్కండి. మీరు డ్రాఫ్ట్ సేవ్ చేసి ఉంటే, బటన్‌పై **Continue 15 Days Entry** అని ఉంటుంది.
5. **Income, Expense & Profit Trends** లో **Last 6 Periods**, **Last 3 Periods** లేదా **All Periods** ఎంచుకోండి. ఒక బార్ నంబర్లు చూడటానికి దానిపై కర్సర్ పెట్టండి లేదా నొక్కండి.
6. **Latest Stock Position** ప్రతి ప్రొడక్ట్ చివరి క్లోజింగ్ స్టాక్ చూపిస్తుంది. ఒక ప్రొడక్ట్ వెతకండి, లేదా ఇన్వెంటరీ సమ్మరీ తెరవడానికి **View All** నొక్కండి.
7. **Income Overview**, **Top Expense Categories** డబ్బు ఎక్కడి నుంచి వచ్చిందో, ఎక్కడికి వెళ్లిందో చూపిస్తాయి. సమ్మరీ తెరవడానికి **View Details** నొక్కండి.

> Tip: ROI అంటే నికర లాభాన్ని బడ్జెట్‌తో భాగించగా వచ్చేది.

## సాధారణ సమస్యలు

- **"No report history yet".** (ఇంకా రిపోర్ట్ హిస్టరీ లేదు) ముందు ఒక 15 రోజుల రిపోర్ట్ సబ్మిట్ చేయండి. ఆ తర్వాత చార్ట్ నిండుతుంది.
- **"Could not load dashboard data".** (డాష్‌బోర్డ్ డేటా లోడ్ కాలేదు) మీ ఇంటర్నెట్ చెక్ చేసి **Retry** నొక్కండి.
- **నంబర్లన్నీ సున్నా చూపిస్తున్నాయి.** సరైన **Financial Year** ఎంచుకున్నారో లేదో చూడండి.
:::

::: section product-rates
title: కొత్త ప్రొడక్ట్ లేదా రేట్ మార్పు అడగడం
routes: /ProductRateManagement
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-prm-tabs] | 2 > [data-help=fo-prm-status] | 3 > [data-help=fo-prm-search] | 4 > [data-help=fo-prm-actions] | 5 > [data-help=fo-prm-new]
minutes: 3
source-hash: e137e022776b0912
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Product & Rate Management** ప్రతి ఫార్మ్ ప్రొడక్ట్‌ను, దాని ఇప్పటి రేట్‌ను, చివరి రిక్వెస్ట్‌ను చూపిస్తుంది. Farm Admin ను కొత్త ప్రొడక్ట్ లేదా కొత్త రేట్ అడగడానికి దీన్ని వాడండి.

1. ఒక ట్యాబ్ ఎంచుకోండి: **All Product**, **Livestock** లేదా **Orchard**.
2. ఆ రిక్వెస్ట్‌లు మాత్రమే చూడటానికి **Pending** లేదా **Approved** నొక్కండి. **All** అన్ని ప్రొడక్ట్‌లు చూపిస్తుంది.
3. ఒక ప్రొడక్ట్‌ను త్వరగా కనుక్కోవడానికి **Search products...** లో టైప్ చేయండి.
4. **Action** కాలమ్‌లో, కన్ను ఐకాన్ **Request Details** చూపిస్తుంది. పెన్సిల్ రేట్ మార్పు అడగడానికి, లేదా మీ **Pending** లేదా **Rejected** రిక్వెస్ట్‌ను మార్చడానికి. బిన్ (చెత్తబుట్ట) **Pending** లేదా **Rejected** రిక్వెస్ట్‌ను డిలీట్ చేస్తుంది.
5. ఒక ప్రొడక్ట్ చేర్చడానికి **Request New Product** నొక్కండి. **Live Stock** లేదా **Orchard** ఎంచుకుని, **Product**, **UOM**, **Price** ఎంటర్ చేసి, కావాలంటే **Reason** రాసి, **Request Product** నొక్కండి.

> Tip: ఆమోదం వచ్చాక, ఆ ప్రొడక్ట్, రేట్ ఇన్వెంటరీ, ఆదాయం ఎంట్రీలో కనిపిస్తాయి.
> Careful: రిక్వెస్ట్‌ను డిలీట్ చేస్తే అది మళ్లీ తిరిగి రాదు.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Live Stock** / **Orchard** | ప్రొడక్ట్ గ్రూప్ (పశువులు లేదా తోట). |
| **Product** | ప్రొడక్ట్ పేరు, ఉదాహరణకు Sheep (గొర్రె). తప్పనిసరి. |
| **UOM** | Kg, Unit, Litre లేదా Nos. తప్పనిసరి. |
| **Price** | ఒక యూనిట్‌కు రేట్, రూపాయల్లో. సున్నా కంటే ఎక్కువ ఉండాలి. |
| **Reason (Optional)** | మీకు ఎందుకు కావాలో. Farm Admin నిర్ణయం తీసుకోవడానికి సాయపడుతుంది. |

## సాధారణ సమస్యలు

- **"Enter a valid rate."** (సరైన రేట్ ఎంటర్ చేయండి) సున్నా కంటే ఎక్కువ ధర ఎంటర్ చేయండి.
- **"This product is still awaiting approval - you can't edit it yet."** (ఈ ప్రొడక్ట్ ఇంకా ఆమోదం కోసం ఎదురుచూస్తోంది, ఇప్పుడే మార్చలేరు) Farm Admin కొత్త ప్రొడక్ట్‌ను ఆమోదించే వరకు ఆగండి.
- **నా రిక్వెస్ట్ Rejected అని చూపిస్తోంది.** **Rejection Reason** చదవడానికి కన్ను ఐకాన్‌తో దాన్ని తెరవండి. పెన్సిల్ నొక్కి, సరిచేసి, **Resubmit Request** నొక్కండి.
- **నాకు Request New Product కనిపించడం లేదు.** మీ హోదాకు ఈ స్క్రీన్ చూసే అనుమతి మాత్రమే ఉంది.
:::

::: section rate-approvals
title: ప్రొడక్ట్, రేట్ రిక్వెస్ట్‌లను ఆమోదించడం లేదా తిరస్కరించడం
routes: /ProductRateApprovals
for: 
roles: 
designations: 
minutes: 3
source-hash: 0eac09c794e59c9c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Pending Approvals** లో Farm Office నుంచి వచ్చి నిర్ణయం కోసం ఎదురుచూస్తున్న కొత్త ప్రొడక్ట్, రేట్ మార్పు రిక్వెస్ట్‌లు కనిపిస్తాయి. నిర్ణయం తీసుకున్న రిక్వెస్ట్ ఈ లిస్ట్ నుంచి వెళ్లిపోతుంది.

1. **Filters** తెరవండి. **From Date**, **To Date**, **Request Type**, **Product Group** లేదా **Product** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
2. ఒక ట్యాబ్ ఎంచుకోండి: **All Request**, **New Product** లేదా **Rate Change**. ఎన్ని ఎదురుచూస్తున్నాయో నంబర్లు చూపిస్తాయి.
3. ఒక రిక్వెస్ట్ చూడటానికి కన్ను ఐకాన్ నొక్కండి: ప్రొడక్ట్, యూనిట్, ఇప్పటి రేట్, **Request Price**, ఎవరు అడిగారు, **Reason**.
4. అడిగిన ధరకే ఆమోదించడానికి, ఆ వరుసలో **Approve** నొక్కి నిర్ధారించండి.
5. ముందుగా ధర లేదా యూనిట్ మార్చాలంటే, పెన్సిల్ నొక్కండి. **Approval Details** కింద **UOM** లేదా **Approved Price** మార్చి, **Admin Remarks** రాసి, **Approve** నొక్కండి.
6. తిరస్కరించడానికి, పెన్సిల్ నొక్కి, **Admin Remarks** లో కారణం రాసి, **Reject** నొక్కి, తర్వాత నిర్ధారించండి.

> Careful: ఆమోదించిన వెంటనే ప్రొడక్ట్ రేట్ మారిపోతుంది. అది తర్వాతి 15 రోజుల రిపోర్ట్‌లో వాడబడుతుంది.

## సాధారణ సమస్యలు

- **లిస్ట్ ఖాళీగా ఉంది.** తేదీలు ఈరోజు నుంచి మొదలవుతాయి. ముందటి **From Date** ఎంచుకుని **Apply Filters** నొక్కండి.
- **"Enter a rejection reason in Admin Remarks."** (Admin Remarks లో తిరస్కరణ కారణం రాయండి) తిరస్కరించడానికి కారణం తప్పనిసరి.
- **"Enter a valid approved price."** (సరైన ఆమోద ధర ఎంటర్ చేయండి) సున్నా కంటే ఎక్కువ ధర ఎంటర్ చేయండి.
- **Approve బటన్ లేదు.** మీ హోదాకు రిక్వెస్ట్‌లు చూసే అనుమతి మాత్రమే ఉంది.
:::

::: section product-master
title: Product Master నిర్వహించడం
routes: /FarmProductMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: 858235a42d5c0ca1
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Product Master** అంటే అన్ని ఫార్మ్ ప్రొడక్ట్‌లు, వాటి యూనిట్‌లు, ఇప్పటి రేట్ల లిస్ట్. ఆమోదం వచ్చిన రిక్వెస్ట్‌లు ఇక్కడ చేరతాయి. Farm Admin ఒక ప్రొడక్ట్‌ను నేరుగా కూడా సరిచేయవచ్చు.

1. ఒక ట్యాబ్ ఎంచుకోండి: **All Product**, **Livestock** లేదా **Orchard**, లేదా ఒక ప్రొడక్ట్ వెతకండి.
2. **Current Rate**, **Status** (**Active** లేదా **Inactive**) చెక్ చేయండి.
3. ఆమోదం వచ్చిన ప్రతి రేట్‌ను దాని తేదీతో చూడటానికి **View History** నొక్కండి. చివరి రేట్‌పై **Current** అనే గుర్తు ఉంటుంది.
4. ఒక ప్రొడక్ట్‌ను సరిచేయడానికి, పెన్సిల్ నొక్కండి. **Product Group**, **Product Name**, **UOM** లేదా **Current Rate** మార్చండి.
5. ఒక ప్రొడక్ట్ వాడకం ఆపడానికి **Active** టిక్ తీసేయండి. అది దాని రేట్ హిస్టరీతో లిస్ట్‌లోనే ఉంటుంది.
6. **Save Changes** నొక్కండి.

> Careful: ఇక్కడ చేసే మార్పు ఆమోద ప్రక్రియ ద్వారా వెళ్లదు. మార్పు వెంటనే అమల్లోకి వస్తుంది.

## సాధారణ సమస్యలు

- **"Enter a product name and UOM."** (ప్రొడక్ట్ పేరు, UOM ఎంటర్ చేయండి) రెండూ తప్పనిసరి.
- **"No approved rate history yet."** (ఇంకా ఆమోదించిన రేట్ హిస్టరీ లేదు) ఈ ప్రొడక్ట్‌కు ఇంకా ఏ రేట్ రిక్వెస్ట్‌కూ ఆమోదం రాలేదు.
- **పెన్సిల్ ఐకాన్ లేదు.** మీ హోదాకు Product Master చూసే అనుమతి మాత్రమే ఉంది.
:::

::: section fifteen-day-entry
title: 15 రోజుల రిపోర్ట్ నింపి సబ్మిట్ చేయడం
routes: /FifteenDaysEntry, /15daysEntry
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-steps] | 2 > [data-help=fo-period] | 3 > [data-help=fo-inventory] | 4 > [data-help=fo-continue] | 5 > [data-help=fo-continue] | 6 > [data-help=fo-save]
minutes: 6
source-hash: 8adffbba9a65bc8e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ప్రతి 15 రోజులకు (1 నుంచి 15 వరకు, 16 నుంచి నెలాఖరు వరకు) ఫార్మ్ ఒక రిపోర్ట్ పంపుతుంది. అందులో నాలుగు దశలు ఉంటాయి: **Inventory**, **Income**, **Expense**, **Review**. ఇంకా సబ్మిట్ చేయని తర్వాతి కాలాన్ని ఈ స్క్రీన్ తెరుస్తుంది.

1. **Inventory**, **Income**, **Expense**, **Review** మధ్య వెళ్లడానికి పైన ఉన్న దశలను వాడండి. మీరు ప్రతిసారి కదిలినప్పుడు మీ పని డ్రాఫ్ట్‌గా సేవ్ అవుతుంది.
2. కాలం కార్డులు చెక్ చేయండి: **Current Reporting**, **Previous Period**, **Next Reporting Period**.
3. **Inventory Entry** లో, **Livestock Sales Ledger** లో ప్రతి ప్రొడక్ట్‌కు **Purchase / Old Quantity**, **Hatched / New Breed**, **Mortality**, **Others** ఎంటర్ చేయండి. **Opening Stock**, **Closing Stock** వాటంతట అవే నిండుతాయి. అవసరమైతే **Remarks** రాయండి.
4. **Continue to Income** నొక్కండి. **Income Entry** లో, **Livestock Sales Ledger**, **Orchard Sales** లో ప్రతి ప్రొడక్ట్‌కు **Sold Quantity** ఎంటర్ చేయండి. రేట్ అనేది ఆమోదించిన రేటే, కాబట్టి **Total amount** మీ కోసం లెక్కించబడుతుంది. **Continue to Expense** నొక్కండి.
5. **Expense Entry** లో, **Expenditure Breakup**, **Miscellaneous Expenditure** కింద మొత్తాలు ఎంటర్ చేయండి. ఏదైనా లైన్ లేకపోతే **Add New Category** లేదా **Add New Sub Category** వాడండి. **Continue to Review** నొక్కి, అన్నీ చెక్ చేసి, **Submit 15-Day Report** నొక్కి, తర్వాత **Confirm Submit** నొక్కండి.
6. ఆపి తర్వాత తిరిగి రావడానికి, ఎప్పుడైనా **Save as Draft** నొక్కండి.

> Tip: బ్రేకప్‌లోని **Miscellaneous** అనేది సబ్ కేటగిరీల మొత్తం. అందులో మీరు టైప్ చేయలేరు.
> Careful: సబ్మిట్ చేశాక, admin మళ్లీ తెరిస్తే తప్ప రిపోర్ట్‌ను మార్చలేరు.

## సబ్మిట్ చేశాక

- **15 - Day Report Submitted Successfully!** స్క్రీన్ మొత్తాలను చూపిస్తుంది.
- దాన్ని తెరవడానికి **View Submitted Report** నొక్కండి, ఏడాది లెక్కల కోసం **Go to finance summary** నొక్కండి, లేదా తర్వాతి కాలం కోసం **Enter ... Report** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Purchase / Old Quantity** | ఈ కాలంలో కొన్న జంతువులు లేదా వస్తువులు. |
| **Hatched / New Breed** | ఈ కాలంలో కొత్తగా పుట్టిన లేదా పొదిగిన జంతువులు. |
| **Mortality** | ఈ కాలంలో చనిపోయిన జంతువులు. |
| **Others** | వేరే ఏదైనా మార్పు. స్టాక్ తగ్గించడానికి ఇది సున్నా కంటే తక్కువ (మైనస్) కూడా ఉండొచ్చు. |
| **Sold Quantity** | ఎన్ని అమ్మారు. ఇది ఇన్వెంటరీ దశలోని **Sold** ను కూడా నింపుతుంది. |
| **Amount (₹)** | ఈ కాలంలో ఆ ఖర్చుకు అయిన డబ్బు. |

## సాధారణ సమస్యలు

- **"No products in this ledger".** (ఈ లెడ్జర్‌లో ప్రొడక్ట్‌లు లేవు) ఇంకా ఏ ప్రొడక్ట్‌కూ ఆమోదం రాలేదు. **Product & Rate Management** లో దాన్ని అడగండి.
- **Sold లో టైప్ చేయలేకపోతున్నాను.** **Income** దశలో **Sold Quantity** ఎంటర్ చేయండి.
- **రేట్ తప్పుగా ఉంది.** **Product & Rate Management** లో రేట్ మార్పు అడగండి. ఇక్కడ దాన్ని మార్చలేరు.
- **"Please enter a category name."** (కేటగిరీ పేరు ఎంటర్ చేయండి) **Add Category** నొక్కే ముందు ఒక పేరు టైప్ చేయండి.
:::

::: section report-review
title: సబ్మిట్ చేసిన 15 రోజుల రిపోర్ట్ తెరవడం
routes: /FifteenDayReportReview, /15DayReportReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 1abbfd7769868ce9
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**15 - Day Report Review** స్క్రీన్ సబ్మిట్ చేసిన ఒక రిపోర్ట్‌ను పూర్తిగా చూపించి, దాన్ని ముందటి కాలంతో పోలుస్తుంది.

1. సబ్మిట్ చేశాక **View Submitted Report** తో, లేదా **Finance Summary** లోని కన్ను ఐకాన్‌తో దీన్ని తెరవండి.
2. పైన కాలం, స్టేటస్, ఎవరు సబ్మిట్ చేశారో చూడండి.
3. కార్డులు చదవండి: **Total Income**, **Total Expense**, **Net Profit**, **Total Products**. బాణం గుర్తులు ముందటి 15 రోజులతో పోల్చి చూపిస్తాయి.
4. ప్రతి భాగాన్ని తెరవడానికి లేదా మూయడానికి **Inventory Summary**, **Income Summary** లేదా **Expense Summary** నొక్కండి.
5. కింద ఉన్న **Financial Summary**, **Comparison with Previous** చెక్ చేయండి.
6. వెనక్కి వెళ్లడానికి **Back to Finance Summary List** నొక్కండి.

## సాధారణ సమస్యలు

- **"No previous period to compare".** (పోల్చడానికి ముందటి కాలం లేదు) ఇది ఈ ఆర్థిక సంవత్సరంలో మొదటి రిపోర్ట్.
- **"No inventory records found for this period."** (ఈ కాలానికి ఇన్వెంటరీ రికార్డులు లేవు) ఆ రిపోర్ట్‌లో స్టాక్ ఏదీ ఎంటర్ చేయలేదు.
:::

::: section finance-summary
title: ఏడాది ఫైనాన్స్ సమ్మరీ చూడటం
routes: /FinanceSummary, /FinanceSummaryOverall
for: 
roles: 
designations: 
minutes: 2
source-hash: d2b850b02c31e1d6
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Finance Summary (Overall)** ఒక ఆర్థిక సంవత్సరంలోని అన్ని 15 రోజుల రిపోర్ట్‌లను కలిపి, ప్రతి రిపోర్ట్‌ను లిస్ట్ చేస్తుంది.

1. **Financial Year**, **View** ఎంచుకోండి: **15-Day View**, **Monthly View** లేదా **Quarterly View**.
2. కార్డులు చదవండి: **Annual Budget**, **Total Income**, **Total Expense**, **Net Profit**, **ROI**.
3. **Analytics Validation Trend** లో **Last 6 Months**, **Last 3 Months** లేదా **Last 1 Year** ఎంచుకోండి.
4. రిపోర్ట్ హిస్టరీలో, లిస్ట్ తగ్గించడానికి **From Date**, **To Date** సెట్ చేయండి.
5. **Status** కాలమ్‌లో **Submitted** లేదా **Draft** కనిపిస్తుంది. రిపోర్ట్ తెరవడానికి కన్ను ఐకాన్ నొక్కండి.
6. లిస్ట్ సేవ్ చేయడానికి **Download Excel** నొక్కండి.

## సాధారణ సమస్యలు

- **"No report records found for the selected filter."** (ఎంచుకున్న ఫిల్టర్‌కు రిపోర్ట్ రికార్డులు లేవు) తేదీలు పెంచండి లేదా వేరే ఆర్థిక సంవత్సరం ఎంచుకోండి.
- **పైన ఉన్న బ్యాడ్జ్ Ready to submit అని చూపిస్తోంది.** ఆ ఏడాదిలో కనీసం ఒక రిపోర్ట్ ఇంకా డ్రాఫ్ట్‌గా ఉంది.
:::

::: section inventory-summary
title: ప్రొడక్ట్ వారీగా స్టాక్ కదలిక చూడటం
routes: /InventorySummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 7b39d227504cc4c7
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Inventory Summary** ప్రతి 15 రోజుల కాలంలో ప్రతి ప్రొడక్ట్ స్టాక్ ఎలా మారిందో చూపిస్తుంది.

1. **Filters** తెరవండి. **From Date**, **To Date**, **Category**, **Product** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
2. ఒక ప్రొడక్ట్ చూడటానికి దాని ట్యాబ్ నొక్కండి.
3. కార్డులు చదవండి: **Total Purchased**, **Total Hatched**, **Total Sold**, **Mortality**, **Closing Stock**.
4. **Stock Movement History** టేబుల్‌లో, వరుస క్రమంలో పెట్టడానికి ఒక కాలమ్ పేరు నొక్కండి. ఇంకా డ్రాఫ్ట్‌లో ఉన్న కాలానికి **Draft** అనే గుర్తు ఉంటుంది.
5. టేబుల్ సేవ్ చేయడానికి **Download Excel** లేదా **Download PDF** నొక్కండి.

## సాధారణ సమస్యలు

- **"From Date cannot be later than To Date."** (From Date, To Date తర్వాత ఉండకూడదు) తేదీలు మళ్లీ ఎంచుకోండి.
- **"No products found".** (ప్రొడక్ట్‌లు ఏవీ లేవు) ముందు Product Master లో ప్రొడక్ట్‌లు చేర్చాలి.
:::

::: section income-summary
title: ఆదాయం సమ్మరీ చూడటం
routes: /IncomeSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: d11284cff39730df
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Income Summary** ప్రతి రిపోర్టింగ్ కాలానికి ఫార్మ్ అమ్మకాల ఆదాయాన్ని, పశువులు (livestock), తోట (orchard) అని విడదీసి చూపిస్తుంది.

1. **Filters** తెరవండి. **Period Type** (**15 Days** లేదా **1 Month**), **From Date**, **To Date**, **Income Category** ఎంచుకుని, **Apply Filters** నొక్కండి.
2. కార్డులు చదవండి: **Total Income Till Date**, **Sales Records**, **Products Covered**, **Reporting Period**. చివరి కార్డు, చివరి కాలాన్ని అప్‌డేట్ చేయడానికి ఇంకా ఎన్ని రోజులు ఉన్నాయో చూపిస్తుంది.
3. **Livestock Sales Ledger** టేబుల్‌లో, ప్రతి కాలానికి **Livestock Income**, **Orchard Income**, **Total Income**, **Status** చూడండి.
4. ఆ కాలంలోని ప్రతి ప్రొడక్ట్, సంఖ్య, రేట్, ఆదాయం చూడటానికి **View** లోని కన్ను ఐకాన్ నొక్కండి.
5. ప్రతి ప్రొడక్ట్ వాటా చూడటానికి **Live Income Overview**, **Orchard Income Overview** చూడండి.
6. టేబుల్ సేవ్ చేయడానికి **Download Excel** నొక్కండి.

## సాధారణ సమస్యలు

- **"Editing window closed".** (మార్చే సమయం అయిపోయింది) చివరి కాలాన్ని అప్‌డేట్ చేసే సమయం ముగిసింది.
- **"Could not load income data".** (ఆదాయం డేటా లోడ్ కాలేదు) **Retry** నొక్కండి.
:::

::: section expense-summary
title: ఖర్చుల సమ్మరీ చూడటం
routes: /ExpenseSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 9200f1d0c1608503
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Expense Summary** ప్రతి రిపోర్టింగ్ కాలానికి ఫార్మ్ ఖర్చులను, ఎక్కువ ఖర్చైన కేటగిరీలను చూపిస్తుంది.

1. **Filters** తెరవండి. **Period Type**, **From Date**, **To Date**, **Expense Category** ఎంచుకుని, **Apply Filters** నొక్కండి.
2. కార్డులు చదవండి: **Total Expense**, **Highest Expense Category**, **Expense Categories**, **Summary Coverage**.
3. **15 - Day Expense Summary** లో, ప్రతి కాలం **Total Expense (₹)**, **Status** చూడండి.
4. ఆ కాలం **Expenditure Breakup (Farm Operations)**, **Miscellaneous Expenditure (Sub-Categories)** చూడటానికి **View** లోని కన్ను ఐకాన్ నొక్కండి.
5. పోల్చి చూడటానికి **Monthly Expense Trend**, **Expense Category Overview** చూడండి.
6. టేబుల్ సేవ్ చేయడానికి **Download Excel** నొక్కండి.

## సాధారణ సమస్యలు

- **"No expense data yet".** (ఇంకా ఖర్చు డేటా లేదు) ముందు ఖర్చులతో ఒక 15 రోజుల రిపోర్ట్ సబ్మిట్ చేయండి.
- **"Could not load expense data".** (ఖర్చు డేటా లోడ్ కాలేదు) **Retry** నొక్కండి.
:::
