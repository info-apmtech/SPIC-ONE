---
module: sales-tracking
title: సేల్స్ ప్లాన్, ఆడిట్
icon: bi-graph-up-arrow
order: 105
language: te
category: అమ్మకాలు
note: కొత్త గైడ్
meta-source-hash: b8a21e045ebf822e
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 1976de717daddf89
confidence: ok

ఒక CSR-2 ప్రోగ్రామ్‌కు చివరి ఆమోదం వచ్చాక, దాని సేల్స్‌ను ప్లాన్ చేసి చెక్ చేయాలి. ప్రొడక్ట్ వారీగా సేల్స్ ప్లాన్ తయారు చేయండి, దాన్ని నిర్ధారించండి, SAP నుంచి వచ్చే డీలర్ సేల్స్‌ను ఆ ప్లాన్ కింద సేవ్ చేయండి, స్కీమ్ చివర్లో టార్గెట్ చేరుకున్నారో లేదో చూడండి.

- plan | icon: bi-pencil-square | color: #2563eb
  title: ప్లాన్ తయారు చేయండి
  text: స్కీమ్ తేదీలు, మీరు అమ్మాలనుకునే ప్రొడక్ట్‌లు, సంఖ్య, విలువ పెట్టండి.
- track | icon: bi-graph-up-arrow | color: #0d9488
  title: సేల్స్ సేవ్ చేసి ట్రాక్ చేయండి
  text: SAP డీలర్ ఇన్‌వాయిస్‌లు అప్‌లోడ్ చేసి, ప్రతి రోజు వాటిని ప్లాన్ కింద సేవ్ చేయండి.
- review | icon: bi-flag | color: #d97706
  title: చివర్లో చెక్ చేయండి
  text: Required Turnover చేరిందో లేదో చూడండి. చేరకపోతే, SMM ఇంకా సమయం అడగవచ్చు.
:::

::: section flow
title: సేల్స్ ఆడిట్ ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 63b369579a95c59f
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- owner | type: lane | color: #2563eb
  title: ప్రోగ్రామ్ బాధ్యులు
  sub: CSR-1 లో ఉన్న Responsible Person
- data | type: lane | color: #64748b
  title: సేల్స్ డేటా
  sub: SAP ఇన్‌వాయిస్ Excel
- managers | type: lane | color: #0d9488 | roles: RM, SMM, SMD, Admin, SuperAdmin
  title: మేనేజర్లు
  sub: RM · SMM · SMD
- approvers | type: lane | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: పొడిగింపు ఆమోదించేవారు
  sub: AVP · Admin
- start | lane: owner | col: 1 | icon: bi-play-circle | color: #64748b | next: plan | screen: audit-overview
  title: ఆడిట్ మొదలుపెట్టడం
  status: Not Started
  text: **Sales Audit** లో **Start Audit** నొక్కండి. ప్రోగ్రామ్, బడ్జెట్ చెక్ చేసి, **Continue to sales plan** నొక్కండి.
- plan | lane: owner | col: 2 | icon: bi-pencil-square | color: #94a3b8 | next: dealers | screen: sales-plan
  title: సేల్స్ ప్లాన్ తయారు చేయడం
  status: Draft
  text: స్కీమ్ తేదీలు, ప్రొడక్ట్‌లు ఎంటర్ చేయండి. **Save Draft** లేదా **Save & Continue** నొక్కండి.
- dealers | lane: owner | col: 3 | icon: bi-people | color: #0891b2 | next: confirm
  title: డీలర్లను జత చేయడం
  status: Draft
  text: **Dealers** దశలో, ఈ ప్రోగ్రామ్‌కు ఏ డీలర్ల సేల్స్ లెక్కలోకి వస్తాయో ఎంచుకోండి.
- confirm | lane: owner | col: 4 | icon: bi-check2-square | color: #2563eb | next: upload | return: plan | screen: plan-review
  title: ప్లాన్ నిర్ధారించడం
  status: Plan Confirmed
  text: **Review & Confirm Plan** లో అన్నీ చెక్ చేసి **Confirm Sales Plan** నొక్కండి. తర్వాత ప్లాన్ లాక్ అవుతుంది. అంతకు ముందు **Edit Sales Plan** తో వెనక్కి వెళ్లవచ్చు.
- upload | lane: data | col: 5 | icon: bi-cloud-arrow-up | color: #64748b | next: save | screen: dealer-sales-upload
  title: SAP సేల్స్ అప్‌లోడ్ చేయడం
  status: New
  text: SAP డీలర్ ఇన్‌వాయిస్ Excel ను **Dealer Sales List** లో **Bulk Upload** తో అప్‌లోడ్ చేస్తారు.
- save | lane: owner | col: 6 | icon: bi-save | color: #d97706 | next: track | screen: daily-sales
  title: రోజువారీ సేల్స్ సేవ్ చేయడం
  status: In Progress
  text: **Review & Confirm Plan** లో **Refresh Sales** నొక్కి, తర్వాత **Save Daily Sales** నొక్కండి. కొత్త ఇన్‌వాయిస్ లైన్లు **Old** అవుతాయి, అచీవ్‌మెంట్‌లో కలుస్తాయి.
- track | lane: managers | col: 7 | icon: bi-graph-up-arrow | color: #0d9488 | next: review | screen: tracking-list
  title: ప్రోగ్రెస్ గమనించడం
  status: In Progress
  text: RM, SMM, SMD తమ ప్రాంతం సేల్స్‌ను **Sales Tracking** లో చూస్తారు.
- review | lane: owner | col: 8 | icon: bi-flag | color: #16a34a | screen: period-end-review
  title: పీరియడ్ చివరి రివ్యూ
  status: Completed
  text: సేవ్ చేసిన సేల్స్ **Required Turnover** కు చేరగానే, ఆడిట్ **Completed** అని చూపిస్తుంది. చివరి ఫలితం **Period-End Review** లో కనిపిస్తుంది.
- extension | lane: approvers | col: 8 | icon: bi-calendar-plus | color: #dc2626 | kind: exception | from: review | to: track | screen: extension-review
  title: ఇంకా సమయం అడగడం
  status: Pending
  text: టార్గెట్ చేరకపోతే, SMM **Request Extension** నొక్కుతారు. AVP లేదా Admin దాన్ని ఆమోదిస్తారు లేదా తిరస్కరిస్తారు. ఆమోదిస్తే, స్కీమ్ ముగింపు తేదీ మారుతుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 154f433d358852fc
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: ప్రోగ్రామ్‌కు ఆమోదం వచ్చింది, ఎదురుచూస్తోంది. **Start Audit** నొక్కండి.
- draft | color: #94a3b8
  label: Draft
  text: ఆడిట్ మొదలైంది. ప్లాన్ సేవ్ అయ్యింది కానీ నిర్ధారించలేదు.
- plan-confirmed | color: #2563eb
  label: Plan Confirmed
  text: ప్లాన్ లాక్ అయ్యింది. ఇప్పుడు దాని కింద సేల్స్ సేవ్ చేయవచ్చు.
- in-progress | color: #d97706
  label: In Progress
  text: సేల్స్ సేవ్ అవుతున్నాయి. టార్గెట్ ఇంకా చేరలేదు.
- sales-tracking | color: #0d9488
  label: Sales Tracking
  text: స్కీమ్ పీరియడ్ నడుస్తోంది.
- period-expired | color: #ea580c
  label: Period Expired
  text: టార్గెట్ చేరకముందే స్కీమ్ పీరియడ్ ముగిసింది.
- extension-request | color: #7c3aed
  label: Extension Request
  text: ఇంకా సమయం అడిగారు. నిర్ణయం కోసం ఎదురుచూస్తోంది.
- completed | color: #16a34a
  label: Completed
  text: సేవ్ చేసిన సేల్స్ Required Turnover కు చేరాయి. ఆడిట్ ముగిసింది.
- ext-pending | color: #d97706
  label: Pending
  text: ఒక పొడిగింపు రిక్వెస్ట్ AVP లేదా Admin కోసం ఎదురుచూస్తోంది.
- ext-pending-admin | color: #d97706
  label: Pending Admin Review
  text: మీ పొడిగింపు రిక్వెస్ట్ నిర్ణయం కోసం ఎదురుచూస్తున్నప్పుడు ట్రాకింగ్ స్క్రీన్‌పై ఇది కనిపిస్తుంది.
- ext-approved | color: #16a34a
  label: Approved
  text: పొడిగింపు ఆమోదించబడింది, స్కీమ్ ముగింపు తేదీ మారింది.
- ext-rejected | color: #dc2626
  label: Rejected
  text: పొడిగింపు తిరస్కరించబడింది. స్కీమ్ ముగింపు తేదీ మారలేదు.
- invoice-new | color: #0891b2
  label: New
  text: ఆడిట్‌లో ఇంకా సేవ్ చేయని SAP ఇన్‌వాయిస్ లైన్.
- invoice-old | color: #475569
  label: Old
  text: ఇప్పటికే సేవ్ చేసిన SAP ఇన్‌వాయిస్ లైన్. ఇది ఎప్పుడూ రెండుసార్లు లెక్కలోకి రాదు.
- met | color: #16a34a
  label: Met
  text: ప్లాన్ చేసిన విలువ కనీసం Required Turnover అంత ఉంది.
- not-met | color: #dc2626
  label: Not Met
  text: ప్లాన్ చేసిన విలువ Required Turnover కంటే తక్కువ ఉంది. ఇప్పుడే ప్లాన్ నిర్ధారించలేరు.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: f676ec8395aa4ec8
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- owner | color: #2563eb
  title: ప్రోగ్రామ్ బాధ్యులు
  - ఆడిట్ మొదలుపెట్టి సేల్స్ ప్లాన్ తయారు చేయడం
  - డీలర్లను జత చేసి ప్లాన్ నిర్ధారించడం
  - రోజువారీ సేల్స్ సేవ్ చేయడం
- data | color: #64748b
  title: సేల్స్ డేటా అప్‌లోడ్
  - SAP డీలర్ సేల్స్ Excel అప్‌లోడ్ చేయడం
- managers | color: #0d9488 | roles: RM, SMM, SMD
  title: RM, SMM, SMD
  - మీ ప్రాంతం సేల్స్ ట్రాకింగ్ చూడటం
  - SMM: టార్గెట్ చేరనప్పుడు పొడిగింపు అడగడం
- approvers | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: AVP, Admin
  - పొడిగింపు రిక్వెస్ట్‌లను ఆమోదించడం లేదా తిరస్కరించడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e0470c8f452a2a10
confidence: ok

- no-programme | popular: true
  q: నా ప్రోగ్రామ్ Sales Audit లిస్ట్‌లో లేదు
  a: చివరి ఆమోదం వచ్చిన CSR-2 ప్రోగ్రామ్‌లలో, CSR-1 లో మీరు Responsible Person గా ఉన్నవి మాత్రమే ఈ లిస్ట్‌లో కనిపిస్తాయి. CSR-2 స్టేటస్, Responsible Person చెక్ చేయండి.
- confirm-grey | popular: true
  q: Confirm Sales Plan బటన్ బూడిద రంగులో ఉంది
  a: ఒక స్కీమ్ పీరియడ్, కనీసం ఒక ప్రొడక్ట్, **Met** అని చూపించే **Requirement Status**, కనీసం ఒక జత చేసిన డీలర్ కావాలి. లేనిదాన్ని **Edit Sales Plan** లేదా **Edit Dealer Mapping** తో సరిచేయండి.
- zero-new
  q: Save Daily Sales లో 0 New అని చూపిస్తోంది
  a: మీ డీలర్లు, ప్రొడక్ట్‌లు, స్కీమ్ తేదీలకు సరిపోయే కొత్త SAP ఇన్‌వాయిస్ లైన్లు ఏవీ లేవు. తాజా SAP సేల్స్‌ను **Dealer Sales List** లో అప్‌లోడ్ చేసి, తర్వాత **Refresh Sales** నొక్కండి.
- missing-id
  q: "Missing csr2Id" అని కనిపిస్తోంది
  a: ఈ స్క్రీన్‌లను **Sales Audit** లేదా **Sales Tracking** లిస్ట్ నుంచే తెరవాలి, సేవ్ చేసిన లింక్ నుంచి కాదు. లిస్ట్‌కు వెనక్కి వెళ్లి ప్రోగ్రామ్‌ను మళ్లీ తెరవండి.
- extension-grey
  q: Request Extension బటన్ బూడిద రంగులో ఉంది
  a: SMM మాత్రమే **Period-End Review** నుంచి పొడిగింపు అడగగలరు.
- upload-columns
  q: నా Excel అప్‌లోడ్ "Missing columns" అని ఫెయిల్ అవుతోంది
  a: Excel మొదటి వరుసలో ఈ పేర్లు ఉండాలి: Billing Document, Billing Date, Item, Customer Number, Customer Name, Product Code, Product Name, Quantity, Unit of Measure, Gross Amount.
:::

::: section audit-list
title: నా సేల్స్ ఆడిట్‌లు చూడటం
routes: /SalesAudit
for: 
roles: 
designations: 
minutes: 2
source-hash: dc0f5ec250d5599d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sales Audit** స్క్రీన్ మీరు బాధ్యులుగా ఉన్న, ఆమోదం వచ్చిన CSR-2 ప్రోగ్రామ్‌లను వాటి స్కీమ్ పీరియడ్, టర్నోవర్, స్టేటస్‌తో చూపిస్తుంది. ప్రతి ఆడిట్‌ను ఇక్కడి నుంచే మొదలుపెట్టండి లేదా కొనసాగించండి.

1. **Filters** తెరిచి **Sanction Number**, **Program Name**, **Location**, **Date Range** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
2. ప్రతి దశలో ఎన్ని ప్రోగ్రామ్‌లు ఉన్నాయో చూడటానికి **Total Programs**, **Not started**, **In Progress**, **Completed** లాంటి కార్డులు చూడండి.
3. **Sales Audit List** లో, ప్రోగ్రామ్‌ను కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో టైప్ చేయండి.
4. ప్రతి వరుసలో **Scheme Period**, **Required TO (Auto)**, **Actual Turnover**, **Achievement** చెక్ చేయండి.
5. **Status** కాలమ్‌లోని బటన్ నొక్కండి: కొత్త ప్రోగ్రామ్‌కు **Start Audit**, మీరు మొదలుపెట్టినదానికి **View / Continue**, లేదా **View Achievement**, **Request Extension**, **View Request**, **View Audit**.

## సాధారణ సమస్యలు

- **"No eligible sales audit records found".** (అర్హత ఉన్న సేల్స్ ఆడిట్ రికార్డులు లేవు) చివరి ఆమోదం వచ్చిన CSR-2 ప్రోగ్రామ్‌లలో, మీరు Responsible Person గా ఉన్నవి మాత్రమే ఇక్కడ కనిపిస్తాయి.
- **తేదీ ఫిల్టర్ పని చేయడం లేదు.** తేదీలు ఎంచుకుని, తేదీ బాక్స్‌లో **Done** నొక్కి, తర్వాత **Apply Filters** నొక్కండి.
- **Scheme Period లో "Not Set" అని ఉంది.** సేల్స్ ప్లాన్ ఇంకా తయారు కాలేదు. **Start Audit** లేదా **View / Continue** నొక్కండి.
:::

::: section audit-overview
title: ప్రోగ్రామ్, బడ్జెట్ చెక్ చేయడం
routes: /AuditOverview
for: 
roles: 
designations: 
minutes: 2
source-hash: c767964e677164ff
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Audit Overview** అనేది దశ 1 (**Program**). ఆమోదం వచ్చిన CSR-2, దానికి జత అయిన CSR-1 వివరాలు, **Required Turnover** ను నిర్ణయించే బడ్జెట్ ఇది చూపిస్తుంది.

1. **Sales Audit** లో ఒక ప్రోగ్రామ్‌పై **Start Audit** లేదా **View / Continue** నొక్కండి.
2. పైన ఉన్న పట్టీ చెక్ చేయండి: **Program Type**, **Program Name**, **Sanction Number**, **CSR-2 Status**.
3. **Program Information** చెక్ చేయండి: రాష్ట్రం, రీజియన్, HQ, CSR-2 ఈవెంట్ తేదీ, స్కీమ్ తేదీలు.
4. **Program Budget & Turnover** చెక్ చేయండి. **Required Turnover** ను **Total Approved Budget** నుంచి లెక్కిస్తారు.
5. ఆడిట్ మొదలుపెట్టడానికి **Save Draft** నొక్కండి, లేదా దశ 2 కి వెళ్లడానికి **Continue to sales plan** నొక్కండి.

> Tip: నియమం బడ్జెట్ కార్డుపై చూపిస్తారు: ప్రతి ₹1.50 Lakh బడ్జెట్‌కు ₹1.00 Cr టర్నోవర్.

## సాధారణ సమస్యలు

- **"Missing csr2Id".** (csr2Id లేదు) **Sales Audit** లిస్ట్ నుంచి **Start Audit** తో ఈ స్క్రీన్ తెరవండి.
- **"Access denied (403)".** (అనుమతి లేదు) ఈ ప్రోగ్రామ్‌కు మీరు Responsible Person కాదు.
- **Save Draft బూడిద రంగులో ఉంది.** ఆడిట్ ఇప్పటికే చివరిగా సబ్మిట్ అయ్యింది.
:::

::: section sales-plan
title: సేల్స్ ప్లాన్ తయారు చేయడం
routes: /SalesPlan
for: 
roles: 
designations: 
tour: 1 > [data-help=st-plan-period] | 2 > [data-help=st-plan-add] | 3 > [data-help=st-plan-table] | 4 > [data-help=st-plan-summary] | 5 > [data-help=st-plan-actions]
minutes: 4
source-hash: 2570b597b9143eea
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sales Plan & Scheme Period** అనేది దశ 2. స్కీమ్ తేదీలు, మీరు అమ్మాలనుకునే ప్రొడక్ట్‌లు, సంఖ్య, విలువ ఇక్కడ పెట్టండి.

1. **Scheme Period** కింద, **From Date**, **To Date** ఎంచుకోండి. **Duration** నెలలు, రోజులు చూపిస్తుంది.
2. ఒక వరుస జోడించడానికి **+ Add Product** నొక్కండి.
3. ప్రతి వరుసలో **Product Group**, **Product** ఎంచుకుని, **Planned Quantity**, **Planned Value (₹)** ఎంటర్ చేయండి. బిన్ ఐకాన్ ఒక వరుసను తీసేస్తుంది.
4. **Plan Summary & Comparison** చెక్ చేయండి. **Requirement Status** లో **Met** అని చూపించాలి: **Total Planned Commitment** కనీసం **Required Turnover** అంత ఉండాలి.
5. మీ పని ఉంచుకోవడానికి **Save Draft** నొక్కండి, లేదా సేవ్ చేసి **Dealers** దశకు వెళ్లడానికి **Save & Continue** నొక్కండి.

> Careful: ప్లాన్ నిర్ధారించాక, ఈ స్క్రీన్ లాక్ అవుతుంది. అప్పుడు బటన్‌పై **Continue to Dealer Mapping** అని ఉంటుంది.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **From Date**, **To Date** | స్కీమ్ మొదలు, ముగింపు. ముగింపు తేదీ మొదలు తేదీ కంటే ముందు ఉండకూడదు. |
| **Product Group** | దీన్ని ముందు ఎంచుకోండి. తర్వాత **Product** లిస్ట్‌లో దాని ప్రొడక్ట్‌లు కనిపిస్తాయి. |
| **Product** | ప్రతి ప్రొడక్ట్ ఒక్కసారి మాత్రమే. |
| **Unit** | ఎప్పుడూ MT. |
| **Planned Quantity** | ప్లాన్ చేసిన సేల్స్, MT లో. మైనస్ కాకూడదు. |
| **Planned Value (₹)** | ప్లాన్ చేసిన సేల్స్ విలువ, రూపాయల్లో. మైనస్ కాకూడదు. |

## సాధారణ సమస్యలు

- **"Select a product and enter non-negative quantities and values for every row."** (ప్రతి వరుసలో ప్రొడక్ట్ ఎంచుకుని, మైనస్ కాని సంఖ్య, విలువ ఎంటర్ చేయండి) ప్రతి వరుస నింపండి, లేదా ఖాళీ వరుసలను బిన్ ఐకాన్‌తో తీసేయండి.
- **"The same product cannot appear more than once."** (ఒకే ప్రొడక్ట్ ఒకటి కంటే ఎక్కువసార్లు ఉండకూడదు) రెండోసారి వచ్చిన వరుసను తీసేయండి.
- **"Scheme end date must not precede the start date."** (స్కీమ్ ముగింపు తేదీ మొదలు తేదీ కంటే ముందు ఉండకూడదు) From Date రోజు లేదా ఆ తర్వాత వచ్చే To Date ఎంచుకోండి.
- **Requirement Status లో Not Met అని చూపిస్తోంది.** మొత్తం Required Turnover కు చేరే వరకు ప్రొడక్ట్‌లు జోడించండి లేదా ప్లాన్ చేసిన విలువలు పెంచండి.
:::

::: section plan-review
title: ప్లాన్ చెక్ చేసి నిర్ధారించడం
routes: /PlanReview
for: 
roles: 
designations: 
tour: 1 > [data-help=st-review-summary] | 2 > [data-help=st-review-budget] | 3 > [data-help=st-review-plan] | 4 > [data-help=st-review-status] | 5 > [data-help=st-review-actions]
minutes: 3
source-hash: ee688fac1f288bce
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Review & Confirm Plan** అనేది దశ 4. ప్రోగ్రామ్, బడ్జెట్, ప్లాన్, డీలర్లను చెక్ చేసి, తర్వాత ప్లాన్ నిర్ధారించండి. మీరు ఒక్కసారి మాత్రమే నిర్ధారిస్తారు; ఆ తర్వాత సేల్స్ ట్రాకింగ్ మొదలవుతుంది.

1. పైన ఉన్న పట్టీ (**Program Type**, **Program Name**, **Scheme Period**, **Scheme Duration**), దాని కింద ఉన్న **Program Information** చెక్ చేయండి.
2. **Budget Details** చెక్ చేయండి: **CSR1 Approved Budget**, **Additional Approved Budget**, **Total Approved Budget**, **Required Turnover**.
3. **Product - wise Sales Plan & Summary** చెక్ చేయండి. ప్లాన్ మార్చడానికి **Edit Sales Plan** నొక్కండి.
4. **Sales Achievement & Dealer Allocation** లో, **Requirement Status** లో **Met** అని చూపించాలి, **Mapped Dealers** 0 కంటే ఎక్కువ ఉండాలి. డీలర్లను మార్చడానికి **Edit Dealer Mapping** నొక్కండి.
5. **Confirm Sales Plan** నొక్కి, తర్వాత **Yes, Confirm & Save** నొక్కండి. **Sales Plan Confirmed** బాక్స్‌లో **View Tracking →** లేదా **Back to Sales Audit** నొక్కండి.

> Careful: నిర్ధారించాక, ప్లాన్, స్కీమ్ తేదీలు, డీలర్లను మార్చలేరు.

## సాధారణ సమస్యలు

- **Confirm Sales Plan బూడిద రంగులో ఉంది.** ఒక స్కీమ్ పీరియడ్ సేవ్ చేయండి, **Requirement Status** లో **Met** అని ఉందో చూసుకోండి, కనీసం ఒక డీలర్‌ను జత చేయండి.
- **"Missing csr2Id. Open Plan Review from Dealer Mapping."** (csr2Id లేదు. Dealer Mapping నుంచి Plan Review తెరవండి) **Sales Audit** లిస్ట్ నుంచి ప్రోగ్రామ్‌ను మళ్లీ తెరవండి.
- **ఇప్పుడు ఆపి తర్వాత పూర్తి చేయాలనుకుంటున్నాను.** **Save Draft** నొక్కండి. మీ ప్లాన్, డీలర్లు అలాగే ఉంటాయి.
:::

::: section daily-sales
title: రోజువారీ సేల్స్‌ను ఆడిట్‌లో సేవ్ చేయడం
routes: /PlanReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 3c54cfd763cbb61e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ప్లాన్ నిర్ధారించాక, కొత్త SAP సేల్స్‌ను అచీవ్‌మెంట్‌లో కలపడానికి **Review & Confirm Plan** కు తిరిగి రండి. కొత్త సేల్స్ అప్‌లోడ్ అయిన ప్రతిసారీ ఇలా చేయండి.

1. తాజా SAP డీలర్ సేల్స్ **Dealer Sales List** లో అప్‌లోడ్ అయ్యాయో లేదో చూసుకోండి.
2. ఆ ప్రోగ్రామ్ యొక్క **Review & Confirm Plan** తెరవండి. **Sales Tracking Dashboard** నుంచి **Back to Plan Review** నొక్కవచ్చు.
3. **Matched Dealer Invoice Details** లో **Refresh Sales** నొక్కండి. ప్రతి ఇన్‌వాయిస్ లైన్ **New** (ఇంకా సేవ్ కాలేదు) లేదా **Old** (ఇప్పటికే సేవ్ అయ్యింది) అని చూపిస్తుంది.
4. కింద ఉన్న **Save Daily Sales** (ఎన్ని New ఉన్నాయో అది చూపిస్తుంది) లేదా పైన ఉన్న **Save Sales Progress** నొక్కి, తర్వాత **Yes, Save Daily Sales** నొక్కండి.
5. **Daily Sales Saved Successfully** బాక్స్ సేవ్ అయిన ఇన్‌వాయిస్‌లు, **Cumulative Achievement** చూపిస్తుంది. **Continue** లేదా **Back to Sales Audit** నొక్కండి.
6. సేవ్ చేసిన సేల్స్ Required Turnover కు చేరగానే, బటన్ **Audit Completed** అని చూపిస్తుంది, ఆడిట్ ముగుస్తుంది.

> Tip: Old ఇన్‌వాయిస్ లైన్లు ఎప్పుడూ రెండుసార్లు లెక్కలోకి రావు, కాబట్టి మళ్లీ సేవ్ చేయడం సురక్షితమే.

## సాధారణ సమస్యలు

- **Save Daily Sales బూడిద రంగులో ఉంది లేదా 0 New అని చూపిస్తోంది.** కొత్త ఇన్‌వాయిస్ లైన్లు ఏవీ లేవు. తర్వాతి SAP సేల్స్ అప్‌లోడ్ చేసి **Refresh Sales** నొక్కండి.
- **"SAP products excluded because no unambiguous planned-product name matched".** (ప్లాన్‌లోని ప్రొడక్ట్ పేరుతో స్పష్టంగా సరిపోలనందున SAP ప్రొడక్ట్‌లు వదిలేయబడ్డాయి) SAP ప్రొడక్ట్ పేరు మీ ప్లాన్‌లోని ఒక ప్రొడక్ట్‌తో సరిపోలాలి.
- **కొన్ని ఇన్‌వాయిస్‌లు లెక్కలోకి రాలేదు.** స్కీమ్ పీరియడ్ బయట ఉన్న లైన్లు, MT కాకుండా వేరే యూనిట్ ఉన్న లైన్లు వదిలేయబడతాయి.
:::

::: section dealer-sales-upload
title: SAP డీలర్ సేల్స్ అప్‌లోడ్ చేయడం
routes: /DealerSalesUpload
for: 
roles: 
designations: 
tour: 1 > [data-help=st-upload-btn] | 2 > [data-help=st-upload-btn] | 3 > [data-help=st-upload-search] | 4 > [data-help=st-upload-table] | 5 > [data-help=st-upload-export] | 6 > [data-help=st-upload-footer]
minutes: 3
source-hash: 7a499220079c04f2
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Dealer Sales List** లో SAP నుంచి వచ్చిన డీలర్ ఇన్‌వాయిస్ లైన్లు ఉంటాయి. SAP Excel ను ఇక్కడ అప్‌లోడ్ చేయండి; సేల్స్ ఆడిట్‌లు ఈ లైన్లతో అచీవ్‌మెంట్ లెక్కిస్తాయి.

1. **Bulk Upload** నొక్కి SAP Excel ఫైల్ ఎంచుకోండి (.xlsx లేదా .xls, 5 MB వరకు).
2. మెసేజ్ చదవండి: **New records**, **Already imported**, **Repeated in Excel**. **Upload errors** టేబుల్ కనిపిస్తే, ఆ వరుసలను Excel లో సరిచేసి మళ్లీ అప్‌లోడ్ చేయండి.
3. లైన్లను కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో ఇన్‌వాయిస్, డీలర్ లేదా ప్రొడక్ట్ టైప్ చేయండి.
4. వరుసలను చెక్ చేయండి: **Billing Date**, **Billing Document**, **Dealer Code**, **Dealer Name**, **Product Name**, **Quantity**, **Unit**, **Gross Amount (₹)**.
5. లిస్ట్‌ను బయటకు తీయడానికి **Copy**, **CSV**, **Excel**, **PDF** లేదా **Print** వాడండి.
6. కింద **Items Per Page** ఎంచుకోండి, పేజీల మధ్య మారండి, మొత్తం **Gross sales** చూడండి.

> Tip: అదే ఫైల్‌ను మళ్లీ అప్‌లోడ్ చేయడం సురక్షితమే. ఇప్పటికే ఇంపోర్ట్ అయిన లైన్లు వదిలేయబడతాయి.

## Excel కాలమ్‌లు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Billing Document**, **Item** | ఇన్‌వాయిస్ నంబర్, లైన్ నంబర్. తప్పనిసరి. |
| **Billing Date** | ఇన్‌వాయిస్ తేదీ. సరైన తేదీ అయి ఉండాలి. |
| **Customer Number**, **Customer Name** | డీలర్ కోడ్, డీలర్ పేరు. తప్పనిసరి. |
| **Product Code**, **Product Name** | తప్పనిసరి. ఆడిట్‌లో లెక్కలోకి రావాలంటే, పేరు ప్లాన్ చేసిన ప్రొడక్ట్‌తో సరిపోలాలి. |
| **Quantity**, **Unit of Measure**, **Gross Amount** | మైనస్ కాకూడదు. MT లైన్లు మాత్రమే ఆడిట్‌లో లెక్కలోకి వస్తాయి. |

## సాధారణ సమస్యలు

- **"Missing columns".** (కాలమ్‌లు లేవు) Excel మొదటి వరుసలో పైన ఉన్న అన్ని కాలమ్ పేర్లు ఉండాలి.
- **"Excel validation failed; no rows saved."** (Excel చెకింగ్ ఫెయిల్ అయ్యింది; ఏ వరుసా సేవ్ కాలేదు) **Upload errors** టేబుల్ (**Excel Row**, **Field**, **Reason**) చదివి, ఆ వరుసలను సరిచేసి మళ్లీ అప్‌లోడ్ చేయండి.
- **"Maximum 10,000 invoice rows allowed."** (గరిష్ఠంగా 10,000 ఇన్‌వాయిస్ వరుసలు మాత్రమే) ఫైల్‌ను చిన్న ఫైల్‌లుగా విడదీయండి.
:::

::: section sales-tracking
title: టార్గెట్‌తో పోల్చి సేల్స్ గమనించడం
routes: /SalesTracking
for: 
roles: 
designations: 
minutes: 3
source-hash: 409f69e798e447e9
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఒక ప్రోగ్రామ్ Required Turnover లో ఎంత చేరిందో **Sales Tracking Dashboard** చూపిస్తుంది. ప్రోగ్రామ్ బాధ్యులు ప్లాన్ నిర్ధారించాక దీన్ని తెరుస్తారు. మేనేజర్లు **Sales Tracking** లిస్ట్ నుంచి దీన్ని చూడటానికి మాత్రమే (read-only) తెరుస్తారు.

1. ప్లాన్ నిర్ధారించాక **View Tracking →** తో, లేదా **Sales Tracking** లిస్ట్‌లో **View** తో దీన్ని తెరవండి.
2. పైన ఉన్న టైల్స్ చదవండి: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %**, **Remaining to Achieve**, **Tracking Status**.
3. **Extension Request** బాక్స్ కనిపిస్తే, దాని స్టేటస్ చదవండి. కారణం, రిమార్క్‌లు చూడటానికి **View Request Status** నొక్కండి.
4. **Turnover Achievement Overview**, **Monthly Progress Trend** చూడండి. స్కీమ్ 7 రోజుల్లో ముగుస్తుంటే ఒక హెచ్చరిక కనిపిస్తుంది.
5. ప్రొడక్ట్ టేబుల్, డీలర్ టేబుల్స్ చెక్ చేయండి. **Dealer Sales Achievement - SAP Invoice Matches** లో, ఇన్‌వాయిస్ లైన్లు చూపించడానికి లేదా దాచడానికి **View Details** లేదా **Hide Details** నొక్కండి.
6. ప్రోగ్రామ్ బాధ్యులు **Import actual sales achievements (SAP invoice report)** కింద SAP ఇన్‌వాయిస్ రిపోర్ట్ అప్‌లోడ్ చేయవచ్చు. కొత్త అప్‌లోడ్ పాతదాని స్థానంలో వస్తుంది.
7. Excel కాపీ కోసం **Download Tracking Report** నొక్కండి, లేదా **Period-End Review** తెరవడానికి **Go To Final Review** నొక్కండి.

## సాధారణ సమస్యలు

- **"Awaiting Sales Data".** (సేల్స్ డేటా కోసం ఎదురుచూస్తోంది) ఇంకా ఏ అచీవ్‌మెంట్ సేవ్ కాలేదు. **Review & Confirm Plan** లో రోజువారీ సేల్స్ సేవ్ చేయండి.
- **ఇంపోర్ట్ బాక్స్ కనిపించడం లేదు.** read-only వ్యూలో, ప్లాన్ నిర్ధారించకముందు, ఆడిట్ చివరిగా సబ్మిట్ అయ్యాక అది దాచబడుతుంది.
- **"Upload an XLSX or XLS file up to 5 MB."** (5 MB వరకు ఉన్న XLSX లేదా XLS ఫైల్ అప్‌లోడ్ చేయండి) చిన్న Excel ఫైల్ ఎంచుకోండి.
- **Go To Final Review బూడిద రంగులో ఉంది.** ఇంకా ఏ ప్రొడక్ట్ అచీవ్‌మెంట్ సేవ్ కాలేదు.
:::

::: section tracking-list
title: నా ప్రాంతం సేల్స్ ట్రాకింగ్ చూడటం
routes: /SalesTrackingList
for: 
roles: 
designations: 
minutes: 2
source-hash: e2e94d6371a53595
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sales Tracking** మెనూ మీ ప్రాంతంలోని ప్రోగ్రామ్‌ల లిస్ట్‌ను చూడటానికి మాత్రమే (read-only) తెరుస్తుంది. RM రీజియన్‌ను, SMM HQ ను, SMD రాష్ట్రాన్ని చూస్తారు, Admin అన్నీ చూస్తారు.

1. **Filters** తెరిచి **Sanction Number**, **Program Name**, **Location**, **Date Range** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** వాటిని తీసేస్తుంది.
2. ప్రోగ్రామ్‌ను కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో టైప్ చేయండి.
3. ప్రతి వరుసలో **Scheme Period**, **Required TO (Auto)**, **Actual Turnover**, **Achievement**, **Document Status** చెక్ చేయండి.
4. ఆ ప్రోగ్రామ్ యొక్క **Sales Tracking Dashboard** తెరవడానికి **View** నొక్కండి.
5. పేజీల మధ్య మారడానికి **Items Per Page**, బాణం గుర్తులు వాడండి.

## సాధారణ సమస్యలు

- **రోల్‌కు అనుమతి లేదని లేదా ప్రాంతం కేటాయించలేదని ఎర్రర్ వస్తోంది.** మీ లాగిన్‌కు రీజియన్, HQ లేదా రాష్ట్రంతో RM, SMM లేదా SMD రోల్ ఉండాలి. అడ్మినిస్ట్రేటర్‌ను అడగండి.
- **"From date cannot be after To date."** (From తేదీ To తేదీ తర్వాత ఉండకూడదు) తేదీ రేంజ్ సరిచేయండి.
- **"No accessible Sales Audit records found."** (మీరు చూడగలిగే Sales Audit రికార్డులు లేవు) **Clear All** నొక్కండి. మీ ప్రాంతంలో ఇంకా ఆమోదం వచ్చిన ప్రోగ్రామ్ ఏదీ ఉండకపోవచ్చు.
:::

::: section period-end-review
title: స్కీమ్ చివర్లో ఫలితం చెక్ చేయడం
routes: /PeriodEndReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 93280350556420ae
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Period-End Review** ట్రాకింగ్‌లో చివరి దశ. ప్రోగ్రామ్ తన **Required Turnover** కు చేరిందో లేదో చూపిస్తుంది, చేరకపోతే SMM ఇంకా సమయం అడగడానికి వీలు ఇస్తుంది.

1. **Sales Tracking Dashboard** లో **Go To Final Review** లేదా **View Final Review** నొక్కండి.
2. **Program Summary**, **Scheme period ends on** లోని తేదీ చెక్ చేయండి.
3. **Final Program Summary** లో **Required Turnover (TO)**, **Planned Commitment**, **Actual Turnover Achieved**, **Balance to Target**, **Achievement** చదవండి.
4. **Target Outcome & Next Step** లో **Target Achieved** లేదా **Target Not Achieved** అని చూపిస్తుంది.
5. టార్గెట్ చేరకపోతే, SMM **Request Extension** నొక్కుతారు. ఇది కారణం, కొత్త ముగింపు తేదీ ఇవ్వడానికి **Request Extension** స్క్రీన్ తెరుస్తుంది.
6. బయటకు వెళ్లడానికి **Back to Sales Tracking**, **Close** లేదా **Back to List** నొక్కండి.

> Tip: ఈ స్క్రీన్ చూడటానికి మాత్రమే. ఇది స్కీమ్‌ను ముగించదు.

## సాధారణ సమస్యలు

- **Request Extension బూడిద రంగులో ఉంది.** SMM మాత్రమే పొడిగింపు అడగగలరు.
- **Proceed to Final Review, Go To Final Review బూడిద రంగులో ఉన్నాయి.** చివరి రివ్యూ దశ ఇంకా అందుబాటులో లేదు.
- **"Missing CSR2 ID".** (CSR2 ID లేదు) ఈ స్క్రీన్‌ను **Sales Tracking** నుంచి తెరవండి.
:::

::: section audit-details
title: ప్లాన్, అచీవ్‌మెంట్ షీట్ నింపడం
routes: /SalesAuditDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: 609a8329530a3911
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sales Audit — Plan & Achievement** అనేది స్కీమ్ తేదీలు, బడ్జెట్, ప్లాన్, నిజమైన సేల్స్ అన్నీ ఉన్న ఒకే షీట్. **Sales Audit** లిస్ట్ దీన్ని **View Achievement** లేదా **Request Extension** తో తెరుస్తుంది.

1. **Sales Audit** లో, **Status** కాలమ్‌లో **View Achievement** లేదా **Request Extension** నొక్కండి.
2. **CSR-2 Auto-Filled Program Information** చెక్ చేయండి.
3. **Scheme Period & Budget** కింద, **Scheme From**, **Scheme To**, **Additional Approved Budget (₹)** చెక్ చేయండి. **Required Turnover (1.5×)**, **Audit Status** కింద చూపిస్తారు.
4. **P — Sales Plan / Committee (Product-wise Breakup)** కింద, **+ Add Product** నొక్కి ప్రొడక్ట్, సంఖ్య, విలువ ఎంటర్ చేయండి. **Remove** ఒక వరుసను తీసేస్తుంది.
5. **SP — Sales / Achievement** కింద, నిజమైన సేల్స్‌ను అదే విధంగా జోడించండి. **Actual**, **Achievement** మొత్తాలను చూపిస్తాయి.
6. మీ పని ఉంచుకోవడానికి **Save Draft** నొక్కండి, లేదా ఆడిట్ ముగించడానికి **Final Submit** నొక్కండి.

> Careful: **Final Submit** తర్వాత షీట్ చూడటానికి మాత్రమే ఉంటుంది.
> Careful: **Additional Approved Budget** ను దాని వేరే ఆమోదం చెక్ చేశాకే ఎంటర్ చేయండి.

## సాధారణ సమస్యలు

- **"Complete scheme dates and product plan meeting the required target."** (టార్గెట్‌కు సరిపోయే స్కీమ్ తేదీలు, ప్రొడక్ట్ ప్లాన్ పూర్తి చేయండి) Final Submit కు రెండు తేదీలు, కనీసం ఒక ప్లాన్ వరుస, Required Turnover కు కనీసం సమానమైన ప్లాన్, నిజమైన మొత్తాలు కావాలి.
- **"Invalid scheme period."** (స్కీమ్ పీరియడ్ తప్పు) Scheme To, Scheme From కంటే ముందు ఉండకూడదు.
- **"Invalid CSR-2 ID".** (CSR-2 ID తప్పు) ఈ స్క్రీన్‌ను **Sales Audit** లిస్ట్ నుంచి తెరవండి.
:::

::: section extension-list
title: పొడిగింపు రిక్వెస్ట్‌లు చూడటం
routes: /ExtensionRequests
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: b342a221494b9432
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఒక ప్రోగ్రామ్ టార్గెట్ చేరనప్పుడు ఇంకా సమయం కోసం వచ్చిన రిక్వెస్ట్‌లను **Extension Request List** చూపిస్తుంది. AVP, Admin వాటిని ఇక్కడ చెక్ చేస్తారు.

1. కార్డులు చూడండి: **Total Request**, **Pending Request**, **Rejected**, **Approved**.
2. **Filters** తెరిచి **Sanction Number**, **Program Type**, **Location**, **Request On** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
3. ID, శాంక్షన్ నంబర్, ప్రోగ్రామ్, లొకేషన్ లేదా పేరుతో రిక్వెస్ట్ కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో టైప్ చేయండి.
4. ప్రతి వరుసలో **Requested By**, **Scheme Period**, **Request New Date**, **Achievement** చెక్ చేయండి.
5. ఒక రిక్వెస్ట్ తెరవడానికి **View Request** నొక్కండి.

## సాధారణ సమస్యలు

- **"You do not have permission to view extension requests."** (పొడిగింపు రిక్వెస్ట్‌లు చూడటానికి మీకు అనుమతి లేదు) AVP, Admin మాత్రమే ఈ లిస్ట్ తెరవగలరు.
- **"No records found".** (రికార్డులు లేవు) Filters లో **Clear All** నొక్కండి.
:::

::: section extension-review
title: పొడిగింపును ఆమోదించడం లేదా తిరస్కరించడం
routes: /ExtensionRequestDetails
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: 38c09035527b95d4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Extension Request Details** ఒక రిక్వెస్ట్‌ను, ప్రోగ్రామ్ పనితీరు, ఇంకా సమయం కావడానికి కారణంతో చూపిస్తుంది. ఇక్కడ దాన్ని ఆమోదించండి లేదా తిరస్కరించండి.

1. **Extension Request List** లో **View Request** నొక్కండి.
2. పైభాగం చెక్ చేయండి: స్టేటస్, **Request ID**, **Request On**.
3. **Scheme Performance** చదవండి: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %**, **Remaining to Achieve**.
4. **Extension Request** చదవండి: **Current Scheme Period**, **Request New End Date**, **Requested By**, కారణం, రిమార్క్‌లు.
5. **Dealer-wise Actual Sales** చెక్ చేయండి.
6. **Pending** రిక్వెస్ట్ అయితే, **Approve Extension** లేదా **Reject Request** నొక్కండి.
7. **Extension Approved Successfully** లేదా **Extension Rejected Successfully** తెరుచుకుంటుంది. లిస్ట్‌కు తిరిగి వెళ్లడానికి **View Request Details** నొక్కండి.

> Careful: బటన్లు వెంటనే పని చేస్తాయి. రెండో నిర్ధారణ బాక్స్ ఉండదు. ఆమోదిస్తే స్కీమ్ ముగింపు తేదీ మారుతుంది.

## సాధారణ సమస్యలు

- **Approve లేదా Reject బటన్లు లేవు.** రిక్వెస్ట్ ఇప్పటికే **Approved** లేదా **Rejected** అయ్యింది.
- **"This request is already approved or the scheme end date has changed. Reload to review it."** (ఈ రిక్వెస్ట్ ఇప్పటికే ఆమోదించబడింది లేదా స్కీమ్ ముగింపు తేదీ మారింది. మళ్లీ లోడ్ చేసి చూడండి) పేజీని మళ్లీ లోడ్ చేసి మళ్లీ చెక్ చేయండి.
- **"Request not found".** (రిక్వెస్ట్ దొరకలేదు) లిస్ట్ నుంచి రిక్వెస్ట్‌ను మళ్లీ తెరవండి.
:::
