---
module: stock-liquidation
title: స్టాక్, అమ్మకం (liquidation)
icon: bi-boxes
order: 106
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
source-hash: fb227391bc0718ee
confidence: ok

SPIC ఎరువు కంపెనీ నుంచి బయటికి వెళ్లిన తర్వాత దాన్ని ఫాలో చేయండి. డీలర్లు ఇంకా ఏ అమ్మకాలను acknowledge చేయలేదో, హోల్‌సేలర్లు, రిటైలర్ల దగ్గర ఎంత స్టాక్ ఉందో, అది ఎన్ని రోజుల పాతదో, అది ఎంత వేగంగా అమ్ముడవుతోందో చూడండి. ఈ లెక్కలు ప్రభుత్వ ఎరువుల సిస్టమ్ అయిన IFMS నుంచి వస్తాయి.

- ack | icon: bi-hourglass-split | color: #db2777
  title: పెండింగ్ acknowledgement
  text: కొన్నవారు ఇంకా IFMS లో నిర్ధారించని కంపెనీ, డీలర్ అమ్మకాలు చూడండి.
- stock | icon: bi-boxes | color: #2563eb
  title: స్టాక్, దాని వయసు
  text: ప్రతి డీలర్ దగ్గర ఎంత స్టాక్ ఉందో, అది ఎన్ని రోజులుగా అక్కడ ఉందో చూడండి.
- liquidation | icon: bi-graph-up-arrow | color: #16a34a
  title: అమ్మకం (Liquidation)
  text: డీలర్లు తమ స్టాక్‌ను ఎంత వేగంగా అమ్ముతున్నారో, ఏ డీలర్లు నెమ్మదిగా ఉన్నారో చూడండి.
:::

::: section flow
title: స్టాక్ ఎలా కదులుతుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 12cbf705ed2b7495
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- company | type: lane | color: #db2777
  title: SPIC
  sub: కంపెనీ అమ్మకాలు
- dealers | type: lane | color: #7c3aed
  title: డీలర్లు
  sub: IFMS లో ఉన్న హోల్‌సేలర్లు, రిటైలర్లు
- admin | type: lane | color: #374151 | roles: Admin
  title: Admin
  sub: IFMS ఫైల్‌లు లోడ్ చేస్తారు
- staff | type: lane | color: #0d9488 | roles: Director, AVP, SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: సేల్స్ సిబ్బంది
  sub: తమ ప్రాంతం రిపోర్ట్‌లు గమనిస్తారు
- sale | lane: company | col: 1 | icon: bi-receipt | color: #db2777 | next: ack | screen: pending-ack
  title: కంపెనీ అమ్మకం
  status: New
  text: SPIC ఒక హోల్‌సేలర్‌కు ఎరువు అమ్ముతుంది. ఇన్‌వాయిస్ IFMS లో నమోదు అవుతుంది. హోల్‌సేలర్లు కూడా ఇదే విధంగా రిటైలర్లకు అమ్ముతారు.
- ack | lane: dealers | col: 2 | icon: bi-clipboard-check | color: #16a34a | next: upload | screen: ack-cycle
  title: డీలర్ acknowledge చేస్తారు
  status: Ack
  text: స్టాక్ వచ్చిందని కొన్నవారు IFMS లో నిర్ధారిస్తారు. ఇన్‌వాయిస్ తేదీ నుంచి అందుకున్న రోజు వరకు ఉన్న రోజులే acknowledgement సైకిల్.
- upload | lane: admin | col: 3 | icon: bi-file-earmark-arrow-up | color: #374151 | next: stock | screen: file-upload
  title: IFMS రిపోర్ట్‌లు లోడ్ చేయడం
  text: IFMS రిపోర్ట్‌లు ప్రతిరోజూ SPIC ONE లోకి వస్తాయి, రాత్రి జరిగే import ద్వారా లేదా Admin చేసే Excel అప్‌లోడ్ ద్వారా.
- stock | lane: dealers | col: 4 | icon: bi-boxes | color: #2563eb | next: sell | screen: stock-report
  title: డీలర్ దగ్గర ఉన్న స్టాక్
  status: Fresh
  text: Acknowledge చేసిన స్టాక్ హోల్‌సేలర్ లేదా రిటైలర్ దగ్గర ఉంటుంది. దాని వయసు acknowledge చేసిన రోజు నుంచి లెక్కిస్తారు.
- sell | lane: dealers | col: 5 | icon: bi-cart-check | color: #16a34a | next: review | screen: liquidation-cycle
  title: అమ్ముడైంది (liquidated)
  status: Active
  text: డీలర్ స్టాక్‌ను రిటైలర్లకు, రైతులకు అమ్ముతారు. అమ్మకాలతో మిగిలిన స్టాక్ తగ్గుతుంది.
- review | lane: staff | col: 6 | icon: bi-bar-chart | color: #0d9488 | screen: stock-details
  title: గమనించి ఫాలో అప్ చేయడం
  text: సిబ్బంది తమ ప్రాంతం స్టాక్, అమ్మకాలు, స్టాక్ వయసు చెక్ చేసి, నెమ్మదిగా ఉన్న డీలర్లతో ఫాలో అప్ చేస్తారు.
- pending | lane: staff | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: sale | to: ack | screen: pending-ack
  title: సమయానికి acknowledge చేయలేదు
  status: Overdue
  text: కొన్నవారు acknowledge చేయకపోతే, అమ్మకం పెండింగ్‌లోనే ఉండి పాతబడుతుంది. దాన్ని IFMS లో acknowledge చేయమని సిబ్బంది డీలర్‌ను అడుగుతారు.
- aged | lane: staff | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: stock | to: sell | screen: ageing-report
  title: కదలని పాత స్టాక్
  status: Long Aged
  text: అమ్ముడుపోని స్టాక్ పాతబడి Long Aged లేదా Critical అని చూపిస్తుంది. దాన్ని ముందు అమ్మడానికి సిబ్బంది డీలర్‌కు సహాయం చేస్తారు.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 5b6bc08644fdb0cc
confidence: ok

- new | color: #64748b
  label: New
  text: IFMS స్టేటస్. అమ్మకం నమోదైంది కానీ కొన్నవారు ఇంకా acknowledge చేయలేదు.
- ack | color: #16a34a
  label: Ack
  text: IFMS స్టేటస్. స్టాక్ వచ్చిందని కొన్నవారు acknowledge చేశారు.
- completed | color: #16a34a
  label: Completed
  text: అమ్మకం acknowledge అయ్యింది. ఇక పెండింగ్‌లో లేదు.
- latest | color: #2563eb
  label: Latest
  text: 0 నుంచి 10 రోజులుగా acknowledgement కోసం ఎదురుచూస్తోంది.
- overdue | color: #dc2626
  label: Overdue
  text: 21 రోజులు లేదా అంతకంటే ఎక్కువగా acknowledgement కోసం ఎదురుచూస్తోంది.
- consent | color: #9333ea
  label: Consent of Buyer
  text: అమ్మకం కొన్నవారి అంగీకారం కోసం ఎదురుచూస్తోందని IFMS చూపిస్తోంది.
- fast | color: #16a34a
  label: Fast
  text: ఇన్‌వాయిస్ నుంచి 0 నుంచి 2 రోజుల్లో acknowledge అయ్యింది.
- normal | color: #d97706
  label: Normal
  text: ఇన్‌వాయిస్ నుంచి 3 నుంచి 5 రోజుల్లో acknowledge అయ్యింది.
- delayed | color: #ea580c
  label: Delayed
  text: ఇన్‌వాయిస్ తర్వాత 6 నుంచి 10 రోజులకు acknowledge అయ్యింది.
- pending-ack | color: #64748b
  label: Pending ACK
  text: స్టాక్ రిపోర్ట్. ఈ స్టాక్‌కు IFMS లో ఇంకా acknowledgement లేదు, కాబట్టి దానికి వయసు లేదు.
- fresh | color: #059669
  label: Fresh
  text: 0 నుంచి 30 రోజుల పాత స్టాక్.
- medium | color: #34d399
  label: Medium
  text: 31 నుంచి 90 రోజుల పాత స్టాక్.
- slow | color: #f59e0b
  label: Slow Moving
  text: 91 నుంచి 180 రోజుల పాత స్టాక్.
- long | color: #ef4444
  label: Long Aged
  text: 181 నుంచి 364 రోజుల పాత స్టాక్.
- active | color: #16a34a
  label: Active
  text: అమ్మకం (Liquidation). డీలర్ గత 45 రోజుల్లో ఈ స్టాక్ అమ్మారు.
- monitoring | color: #d97706
  label: Monitoring
  text: అమ్మకం (Liquidation). 46 నుంచి 75 రోజులుగా అమ్మకం లేదు. గమనిస్తూ ఉండండి.
- critical | color: #b91c1c
  label: Critical
  text: ప్రతి స్క్రీన్‌లో అన్నిటికంటే చెడ్డ గ్రూప్. పెండింగ్ acknowledgement: 11 నుంచి 20 రోజులు. Acknowledgement సైకిల్: 10 రోజులకు పైగా. స్టాక్ వయసు: 365 రోజులు లేదా అంతకంటే ఎక్కువ. అమ్మకం (Liquidation): 75 రోజులకు పైగా అమ్మకం లేదు.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e4dca540aba6f06
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- staff | color: #0d9488 | roles: SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: సేల్స్ సిబ్బంది
  - మీ సొంత రాష్ట్రం, రీజియన్ లేదా HQ యొక్క పెండింగ్ acknowledgement, స్టాక్, స్టాక్ వయసు, అమ్మకం చూడటం
  - Acknowledge చేయడంలో లేదా అమ్మడంలో నెమ్మదిగా ఉన్న డీలర్లతో ఫాలో అప్ చేయడం
  - లిస్ట్‌లను Excel లో డౌన్‌లోడ్ చేయడం
- leaders | color: #db2777 | roles: Director, AVP
  title: Director, AVP
  - అన్ని రాష్ట్రాల లెక్కలు చూడటం
  - రాష్ట్రాలు, డీలర్లు, ప్రొడక్ట్‌లను పోల్చడం
- admin | color: #374151 | roles: Admin
  title: Admin
  - IFMS Excel రిపోర్ట్‌లను ప్రతిరోజూ అప్‌లోడ్ చేయడం
  - అన్ని రాష్ట్రాల లెక్కలు చూడటం
  - State-wise Stock Reconciliation ను ఈమెయిల్‌లో పంపడం
- dealers | color: #7c3aed | roles: Dealer
  title: హోల్‌సేలర్లు, రిటైలర్లు
  - అందుకున్న స్టాక్‌ను IFMS లో acknowledge చేయడం
  - స్టాక్‌ను త్వరగా అమ్మడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 12f38854e10b0f55
confidence: ok

- overdue | popular: true
  q: ఒక అమ్మకం Overdue అని చూపిస్తోంది. నేను ఏమి చేయాలి?
  a: కొన్నవారు 21 రోజులు లేదా అంతకంటే ఎక్కువగా IFMS లో స్టాక్‌ను acknowledge చేయలేదు. దాన్ని IFMS లో acknowledge చేయమని డీలర్‌ను అడగండి. తర్వాతి IFMS అప్‌డేట్ తర్వాత అది **Completed** అని చూపిస్తుంది.
- old-figures | popular: true
  q: లెక్కలు పాతవిగా కనిపిస్తున్నాయి లేదా రిపోర్ట్ ఖాళీగా ఉంది
  a: SPIC ONE లోకి చివరగా లోడ్ అయిన IFMS డేటానే రిపోర్ట్‌లు చూపిస్తాయి. ఈరోజు ఫైల్‌లు ఇంకా లోడ్ కాకపోతే, **File Upload** లో వాటిని అప్‌లోడ్ చేయమని Admin ని అడగండి, తర్వాత **Refresh Data** నొక్కండి.
- my-area
  q: నేను వేరే రాష్ట్రం లేదా రీజియన్ ఎంచుకోలేకపోతున్నాను
  a: చాలా మంది సిబ్బంది తమ సొంత రాష్ట్రం, రీజియన్ లేదా HQ మాత్రమే చూస్తారు. Director, AVP, Admin అన్ని రాష్ట్రాలు చూస్తారు.
- no-rows
  q: "No records found." లేదా "No Data Available" అని కనిపిస్తోంది
  a: మీరు పెట్టిన ఫిల్టర్‌లు మరీ ఎక్కువగా ఉండొచ్చు. **Clear All** నొక్కి, తర్వాత **Search** నొక్కండి.
- pdf
  q: PDF, Send Mail బటన్‌లు బూడిద రంగులో ఉన్నాయి
  a: ఇవి ఇంకా సిద్ధం కాలేదు. లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** వాడండి.
:::

::: section pending-ack
title: పెండింగ్ acknowledgement లు చూడటం
routes: /CompanySales
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-pa-filters] | 2 > [data-help=sl-pa-cards] | 3 > [data-help=sl-pa-tabs] | 4 > [data-help=sl-pa-table] | 5 > [data-help=sl-pa-export]
minutes: 3
source-hash: bc4973981c2b913d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Pending Acknowledgement** స్క్రీన్ కంపెనీ, హోల్‌సేలర్, రిటైలర్ అమ్మకాలను, కొన్నవారు వాటిని IFMS లో acknowledge చేశారో లేదో చూపిస్తుంది. మెనూలో **Pending Acknowledgement** నొక్కి దీన్ని తెరవండి.

1. **Filters** తెరిచి **Date Range**, **State**, **District**, **Dealer Type**, **Dealer / Agency Name**, **Product** లేదా **Status (By Age)** ఎంచుకోండి. **Search** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
2. మూడు కార్డులు చదవండి: మొత్తం, **Company Sales**, **Wholesaler Sales**. ప్రతి దానిలో **Completed**, **Latest**, **Critical**, **Overdue**, **Consent of Buyer** కనిపిస్తాయి. కింద ఉన్న చార్ట్ రాష్ట్రాలను పోలుస్తుంది.
3. ఒక ట్యాబ్ ఎంచుకోండి: **All**, **Company Sales**, **Wholesaler Sales** లేదా **Retailer Sales**. ఒక అమ్మకాన్ని కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో ఇన్‌వాయిస్ నంబర్ లేదా డీలర్ పేరు టైప్ చేయండి.
4. టేబుల్‌లో **Age Status**, **Pending Ack Age (Days)** చూడండి. పూర్తి **Record Details** చూడటానికి **View** నొక్కండి.
5. లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** నొక్కండి.

> Tip: అన్నిటికంటే పాత పెండింగ్ అమ్మకాలు ముందు కనిపించేలా **Pending Ack Age (Days)** తో సార్ట్ చేయండి, ఆ డీలర్లకు ఫోన్ చేయండి.

| ఫీల్డ్ | ఏమి చూపిస్తుంది |
|---|---|
| **Age Status** | **Latest** 0 నుంచి 10 రోజులు, **Critical** 11 నుంచి 20 రోజులు, **Overdue** 21 రోజులు లేదా ఎక్కువ, లేదా **Consent of Buyer**. |
| **Workflow Status** | IFMS నుంచి వచ్చే స్టేటస్, ఉదాహరణకు **New** లేదా **Ack**. |
| **Quantity (MT)** | మెట్రిక్ టన్నుల్లో పరిమాణం. |

## సాధారణ సమస్యలు

- **"No records found."** (రికార్డులు ఏవీ లేవు) Filters లో **Clear All** నొక్కండి, లేదా సెర్చ్ బాక్స్ ఖాళీ చేయండి.
- **ట్యాబ్‌లు మార్చినా కార్డులు మారడం లేదు.** ఇది మామూలే. ట్యాబ్‌లు టేబుల్‌ను, Excel ఫైల్‌ను మాత్రమే మారుస్తాయి.
- **డీలర్ acknowledge చేసినా అమ్మకం ఇంకా పెండింగ్‌లోనే ఉంది.** కొత్త IFMS డేటా ఇంకా లోడ్ కాలేదు. తర్వాతి అప్‌డేట్ అయ్యాక మళ్లీ చెక్ చేయండి.
:::

::: section ack-cycle
title: డీలర్లు ఎంత వేగంగా acknowledge చేస్తున్నారో చెక్ చేయడం
routes: /Acknowledgement
for: 
roles: 
designations: 
minutes: 2
source-hash: 751788d0db9a139a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Acknowledgement Cycle** స్క్రీన్ ఇన్‌వాయిస్ తర్వాత డీలర్లు స్టాక్‌ను acknowledge చేయడానికి ఎన్ని రోజులు తీసుకున్నారో చూపిస్తుంది. ఇందులో acknowledge అయిన అమ్మకాలు మాత్రమే కనిపిస్తాయి.

1. **Filters** తెరిచి **State**, **Region**, **Head Quarters**, **District**, **Product**, **Dealer Name**, **Status**, **Cycle Bucket** లేదా **Invoice Date From**, **Invoice Date To** ఎంచుకోండి. **Search** నొక్కండి.
2. కార్డులు చదవండి: **Total Transactions**, **Fast (0-2 Days)**, **Normal (3-5 Days)**, **Delayed (6-10 Days)**, **Critical (> 10 Days)**.
3. రెండు top 5 లిస్ట్‌లను దేని ప్రకారం విభజించాలో ఎంచుకోండి: **State**, **Region**, **Head Quarters**, **District**, **Product** లేదా **Dealer Name**.
4. **Top 5 Fast Performing** లిస్ట్‌ను **Top 5 Delayed** లిస్ట్‌తో పోల్చండి.
5. **Acknowledgement Cycle List** లో ప్రతి అమ్మకం యొక్క **Ack Cycle (Days)**, **Status** చూడండి. **Acknowledgement Details** కోసం **View** నొక్కండి.
6. లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** నొక్కండి.

## సాధారణ సమస్యలు

- **A top 5 list says "No data for the current filters."** (ఒక top 5 లిస్ట్‌లో "ప్రస్తుత ఫిల్టర్‌లకు డేటా లేదు" అని ఉంది) ఒక గ్రూప్‌కు ర్యాంక్ రావాలంటే కనీసం 50 acknowledge అయిన అమ్మకాలు ఉండాలి. కొన్ని ఫిల్టర్‌లు తీసేసి మళ్లీ చూడండి.
- **"No acknowledged transactions found."** (acknowledge అయిన లావాదేవీలు ఏవీ లేవు) **Clear All** నొక్కి, తర్వాత **Search** నొక్కండి.
:::

::: section stock-report
title: డీలర్ల దగ్గర ఉన్న స్టాక్ చూడటం
routes: /StockReport
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-sr-filters] | 2 > [data-help=sl-sr-cards] | 3 > [data-help=sl-sr-charts] | 4 > [data-help=sl-sr-search] | 5 > [data-help=sl-sr-table] | 6 > [data-help=sl-sr-export]
minutes: 3
source-hash: 5c6d4945a1c3da6d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Stock Report** స్క్రీన్ ఇప్పుడు ప్రతి హోల్‌సేలర్, రిటైలర్ దగ్గర ఎంత స్టాక్ ఉందో, అది ఎన్ని రోజుల పాతదో చూపిస్తుంది.

1. **Filters** తెరిచి **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying With**, **Product** లేదా **Ageing Range (Days)** ఎంచుకోండి. **Search** నొక్కండి.
2. కార్డులు చదవండి: **Total Stock (Current Stock)**, **Dealers Count**, **Average Ageing**, **High Ageing Stock**.
3. చార్ట్‌లు చూడండి: **State-wise Stock Availability** ఈ సంవత్సరాన్ని గత సంవత్సరంతో పోలుస్తుంది, **Product-wise Stock Distribution (MT)** ప్రతి ప్రొడక్ట్ వాటాను చూపిస్తుంది.
4. **Stock Report List** పైన ఉన్న సెర్చ్ బాక్స్‌లో డీలర్, రాష్ట్రం లేదా ప్రొడక్ట్ పేరు టైప్ చేయండి.
5. టేబుల్‌లో **Quantity (MT)**, స్టాక్ ఎవరి దగ్గర ఉందో (**Lying With**), **Ageing in Days**, **Status** చూడండి. పూర్తి **Stock Details** కోసం **View** నొక్కండి.
6. లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** నొక్కండి.

> Tip: పాత స్టాక్‌ను త్వరగా కనుక్కోవడానికి **Ageing Range (Days)** లో **Long Aged (181-364)**, **Critical (365+)** ఎంచుకోండి.

## సాధారణ సమస్యలు

- **"No Data Available".** (డేటా లేదు) Filters లో **Clear All** నొక్కండి, లేదా సెర్చ్ బాక్స్ ఖాళీ చేయండి.
- **Ageing లో "--" అని, స్టేటస్ Pending ACK అని చూపిస్తోంది.** IFMS లో acknowledge చేసిన రోజు నుంచే వయసు లెక్కిస్తారు. Acknowledgement లేని స్టాక్‌కు ఇంకా వయసు లేదు.
:::

::: section ageing-report
title: పాత, నెమ్మదిగా కదిలే స్టాక్‌ను కనుక్కోవడం
routes: /AgeingReport
for: 
roles: 
designations: 
minutes: 2
source-hash: 366d05a43d5fbe3a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Ageing Report** స్క్రీన్ స్టాక్ డీలర్ల దగ్గర ఎన్ని రోజులుగా ఉందో చూపిస్తుంది. దీనితో నెమ్మదిగా కదిలే స్టాక్‌ను కనుక్కోవచ్చు.

1. **Filters** తెరిచి **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying with**, **Product** లేదా **Ageing Range (Days)** ఎంచుకోండి. **Search** నొక్కండి.
2. కార్డులు చదవండి: **Total Stock (MT)**, **Average Ageing (Days)**, **30-60 Days Stock (MT)**, **60+ Days Stock (MT)**.
3. **State-wise Stock by Ageing (MT)** లో, ఒక రాష్ట్రం యొక్క ప్రతి వయసు గ్రూప్ చూడటానికి దాని బార్ మీద పాయింటర్ ఉంచండి.
4. **Ageing by Days (MT)** లో, ప్రతి వయసు గ్రూప్‌లో ఎంత స్టాక్ ఉందో చూడండి.
5. **Ageing Report List** లో ప్రతి డీలర్ యొక్క **Ageing in Days**, **Status** చూడండి. **Stock Ageing Details** కోసం **View** నొక్కండి.
6. లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** నొక్కండి.

## సాధారణ సమస్యలు

- **"No records found."** (రికార్డులు ఏవీ లేవు) **Clear All** నొక్కి, తర్వాత **Search** నొక్కండి.
- **కొంత పాత స్టాక్ కనిపించడం లేదు.** IFMS లో acknowledge చేసిన రోజు నుంచే వయసు లెక్కిస్తారు.
:::

::: section liquidation-cycle
title: స్టాక్ ఎంత వేగంగా అమ్ముడవుతోందో చూడటం
routes: /LiquidationCycle
for: 
roles: 
designations: 
minutes: 3
source-hash: f76600523961f075
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Liquidation Cycle** స్క్రీన్ ప్రతి డీలర్ దగ్గర ఎంత స్టాక్ ఉందో, వారు ఎంత అమ్మారో, చివరి అమ్మకం తర్వాత ఎన్ని రోజులు అయ్యిందో చూపిస్తుంది.

1. **Filters** తెరిచి **Date From**, **Date To**, **State**, **Region**, **HQ**, **District**, **Sub District**, **Dealer Name**, **Product** లేదా **Financial Year** ఎంచుకోండి. **Apply Filters** నొక్కండి.
2. కార్డులు చదవండి: **Total Stock (MT)**, **Liquidated (MT)**, **Balance Stock (MT)**.
3. **Dealer Liquidation Performance** కింద **Top 5 Performance**, **State-wise**, **All**, **Company Sales**, **Wholesalers** లేదా **Retailers** ఎంచుకోండి.
4. వేగంగా అమ్మేవారి లిస్ట్‌ను నెమ్మదిగా అమ్మేవారి లిస్ట్‌తో పోల్చండి, ఉదాహరణకు **Top 5 Fast Dealers**, **Top 5 Slow Dealers**.
5. **Liquidation Cycle List** లో **Stock (MT)**, **Ageing (Days)**, **Sales (MT)**, **Status** చూడండి. **Liquidation Details** కోసం **View** నొక్కండి.
6. లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** నొక్కండి.

> Tip: ఇక్కడ **Ageing (Days)** అంటే ఆ ప్రొడక్ట్‌ను డీలర్ చివరిసారి అమ్మినప్పటి నుంచి అయిన రోజులు.

## సాధారణ సమస్యలు

- **"No records found."** (రికార్డులు ఏవీ లేవు) **Clear All** నొక్కి, తర్వాత **Apply Filters** నొక్కండి.
- **నేను వేరే రాష్ట్రం ఎంచుకోలేకపోతున్నాను.** మీ ప్రాంతం మీ login ద్వారా నిర్ణయించబడుతుంది. Director, AVP, Admin అన్ని రాష్ట్రాలు చూస్తారు.
:::

::: section stock-details
title: రాష్ట్రాల వారీగా రోజువారీ స్టాక్, అమ్మకాలు చూడటం
routes: /StockDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: b30cf269de71aa03
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Stock Details** స్క్రీన్ ప్రతి రాష్ట్రానికి ఒక లైన్ చూపిస్తుంది: ఓపెనింగ్ స్టాక్, సప్లైలు, అమ్మకాలు, క్లోజింగ్ స్టాక్. ఇది ప్రతిరోజూ అప్‌డేట్ అవుతుంది.

1. పైన **Date as on**, **Period** చూడండి.
2. **Filters** తెరిచి **From Date**, **To Date**, **Financial Year**, **State**, **Region**, **Head Quarters** లేదా **Product** ఎంచుకోండి. **Search** నొక్కండి.
3. కార్డులు చదవండి: **Total Stock**, **Total Sales**, **Closing Stock**, **Sales %**.
4. **Stock List** లో ప్రతి రాష్ట్రం యొక్క **Opening Stock**, **Supplies**, **Total Stock**, **Sales**, **Total Sales**, **Closing Stock**, **Sales %** చదవండి. చివరి లైన్ **Grand Total**.
5. ఒక రాష్ట్రాన్ని కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో దాని పేరు టైప్ చేయండి.
6. లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** నొక్కండి.

## సాధారణ సమస్యలు

- **"No Data Available".** (డేటా లేదు) **Clear All** నొక్కి, తర్వాత **Search** నొక్కండి.
- **కాలమ్‌ల తేదీలు మారుతున్నాయి.** కాలమ్ హెడింగ్‌లు మీరు ఎంచుకున్న కాలం యొక్క తేదీలను చూపిస్తాయి.
:::

::: section product-stock
title: రాష్ట్రాల వారీగా ప్రతి ప్రొడక్ట్ స్టాక్ చూడటం
routes: /ProductWiseStockAvailability
for: 
roles: 
designations: 
minutes: 2
source-hash: 5858eb268707eedc
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Product-wise Stock Availability** స్క్రీన్ ప్రతి రాష్ట్రంలో ప్రతి ప్రొడక్ట్ ఎంత స్టాక్‌లో ఉందో చూపిస్తుంది.

1. **Filters** తెరిచి **State**, **Region**, **Head Quarters**, **Date From** లేదా **Date To** ఎంచుకోండి. **Search** నొక్కండి.
2. కార్డులు చదవండి: **Total States**, **Total Products**, **Total Quantity (MT)**, **Highest Stock State**, **Low Stock Alerts**.
3. **Stock Availability List** లో, ప్రతి వరుస ఒక రాష్ట్రం, ప్రతి కాలమ్ ఒక ప్రొడక్ట్. **Total (MT)** అంటే ఆ రాష్ట్రం మొత్తం.
4. ఒక రాష్ట్రాన్ని కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో దాని పేరు టైప్ చేయండి. చివరి లైన్ **Grand Total**.
5. టేబుల్ డౌన్‌లోడ్ చేయడానికి **Excel** నొక్కండి.

## సాధారణ సమస్యలు

- **"No Data Available".** (డేటా లేదు) **Clear All** నొక్కి, తర్వాత **Search** నొక్కండి.
- **Low Stock Alerts ఎక్కువగా ఉంది.** ఈ రాష్ట్రాలు స్టాక్ పరిమితి కంటే తక్కువలో ఉన్నాయి. వాటికి సప్లైలు ప్లాన్ చేయండి.
:::

::: section stock-reconciliation
title: రాష్ట్రాల వారీ స్టాక్ సరిపోల్చడం (reconciliation) చెక్ చేయడం
routes: /StateWiseGlobalStockReconciliation
for: 
roles: 
designations: 
minutes: 2
source-hash: 50c58211c889d4c4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**State-wise Stock Reconciliation** స్క్రీన్ ఒక రోజుకు, రాష్ట్రం వారీగా, కంపెనీ స్టాక్‌ను హోల్‌సేలర్లు, రిటైలర్ల దగ్గర ఉన్న స్టాక్‌తో సరిపోలుస్తుంది.

1. **As On Date:** ఎంచుకుని **View** నొక్కండి. ఈరోజుకు తిరిగి వెళ్లడానికి **Today** నొక్కండి.
2. **Partial Report - Pending Uploads** బాక్స్ కనిపిస్తే, ఆ రోజు కొన్ని రిపోర్ట్‌లు ఇంకా లోడ్ కాలేదని అర్థం.
3. **Urea Stock** టేబుల్‌లో ప్రతి రాష్ట్రం యొక్క **Company Stock**, **Wholesaler Stock**, **Retail Point Stock**, దారిలో ఉన్న స్టాక్ (GIT), **Overall Stock** చదవండి.
4. ప్రొడక్ట్, రాష్ట్రం వారీ స్టాక్ కోసం కిందికి **GFL Stock** టేబుల్ వరకు స్క్రోల్ చేయండి.
5. ఒక రాష్ట్రాన్ని కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో దాని పేరు టైప్ చేయండి.
6. డౌన్‌లోడ్ చేయడానికి **Export Excel** నొక్కండి, లేదా రిపోర్ట్ పంపడానికి **Send Email** నొక్కండి. **To (recipient email)** ఎంటర్ చేసి, అవసరమైతే **CC (optional)** జోడించి, **Send Email** నొక్కండి.

## సాధారణ సమస్యలు

- **"No Reconciliation Data Available".** (సరిపోల్చే డేటా లేదు) ఆ తేదీ రిపోర్ట్‌లు అప్‌లోడ్ కాలేదు. వేరే తేదీ ఎంచుకోండి, లేదా వాటిని అప్‌లోడ్ చేయమని Admin ని అడగండి. Admin లు **Go to File Upload** నొక్కవచ్చు.
- **ఒక కాలమ్‌లో లెక్కలు లేవు.** ఆ రోజుకు ఆ రిపోర్ట్ లేదు. **Partial Report** బాక్స్ చూడండి.
:::

::: section rankings
title: టాప్ ర్యాంకింగ్ జిల్లాలు, హోల్‌సేలర్లు, రిటైలర్లు
routes: /TopRankingDistrict, /TopRankingWholesalers, /TopRankingRetailers
for: 
roles: 
designations: 
minutes: 1
source-hash: aa745aaa1c48ea10
confidence: ok

**ఈ స్క్రీన్ దేనికి**
తక్కువ సమయంలో ఎక్కువ స్టాక్ అమ్మిన జిల్లాలు, హోల్‌సేలర్లు, రిటైలర్లను ర్యాంకింగ్ స్క్రీన్‌లు లిస్ట్ చేస్తాయి. మెనూ నుంచి **Top Ranking District**, **Top Ranking Wholesaler** లేదా **Top Ranking Retailers** తెరవండి.

1. మీకు కావాల్సిన ర్యాంకింగ్‌ను మెనూ నుంచి తెరవండి.
2. పైన ఉన్న కార్డులు చదవండి, ఉదాహరణకు మొత్తాలు, **Avg Liquidation Cycle**.
3. లిస్ట్‌లో ర్యాంక్ 1 అన్నిటికంటే మంచిది. ప్రతి దాని **State**, **Region**, **HQ**, **District**, **Sub-District** చూడండి.

> Careful: ఈ ర్యాంకింగ్ స్క్రీన్‌లను ఇంకా లైవ్ డేటాకు కలుపుతున్నారు. ఇప్పుడు వాటిలో ఉన్న లెక్కలు, ఫిల్టర్‌లు ఉదాహరణలు మాత్రమే. నిజమైన లెక్కల కోసం **Liquidation Cycle** వాడండి.

## సాధారణ సమస్యలు

- **ఫిల్టర్ పెట్టినా లిస్ట్ మారడం లేదు.** ఈ స్క్రీన్ ఇంకా లైవ్ డేటాకు కలపబడలేదు. **Liquidation Cycle** లో ఉన్న **Top 5 Performance** వాడండి.
:::

::: section file-upload
title: IFMS Excel రిపోర్ట్‌లు అప్‌లోడ్ చేయడం
routes: /ExcelFormatFileUpload
for: 
roles: Admin
designations: 
tour: 1 > [data-help=sl-fu-grid] | 2 > [data-help=sl-fu-file] | 3 > [data-help=sl-fu-date] | 4 > [data-help=sl-fu-upload]
minutes: 3
source-hash: 3c1d667ca6804537
confidence: ok

**ఈ స్క్రీన్ దేనికి**
రోజువారీ IFMS రిపోర్ట్‌లు లోడ్ చేయడానికి Admin **Excel Format File Upload** స్క్రీన్ వాడతారు. అన్ని స్టాక్, అమ్మకం స్క్రీన్‌లు ఈ డేటానే చదువుతాయి. మెనూలో **File Upload** నొక్కి దీన్ని తెరవండి.

1. మీ రిపోర్ట్ కార్డు కనుక్కోండి: **Retail Stocks**, **Company Sales**, **Sales and Receipt**, **Wholesale Sales**, **Wholesale Stock As On Today**, **State Wise Global Stock Reconciliation** లేదా **Warehouse and RakePoint Global Stock**.
2. **Excel File** కింద, IFMS నుంచి మీరు డౌన్‌లోడ్ చేసిన ఫైల్ ఎంచుకోండి.
3. కార్డులో **Report Date** ఉంటే, అది రిపోర్ట్ తేదీయేనా అని చెక్ చేయండి. అది ఈరోజు తేదీతో మొదలవుతుంది.
4. **Upload** నొక్కి, **Uploading...** అని చూపిస్తున్నప్పుడు ఆగండి. తర్వాత మొత్తం వరుసలు, ఎన్ని చేర్చబడ్డాయి, ఎన్ని అప్‌డేట్ అయ్యాయి, ఎన్ని వదిలేయబడ్డాయి అని ఒక మెసేజ్ వస్తుంది.

> Tip: ప్రతి కార్డు ఒక రిపోర్ట్ కోసం. మీ IFMS రిపోర్ట్ పేరుతో సరిపోయే కార్డు ఎంచుకోండి.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Excel File** | ఒకే .xlsx లేదా .csv ఫైల్, 100 MB వరకు. |
| **Report Date** | రిపోర్ట్ ఏ తేదీకి సంబంధించినదో అది. Retail Stocks, Sales and Receipt, State Wise Global Stock Reconciliation, Warehouse and RakePoint Global Stock కు అవసరం. |

## సాధారణ సమస్యలు

- **"Please select a file first."** (ముందు ఒక ఫైల్ ఎంచుకోండి) అదే కార్డులో ఫైల్ ఎంచుకుని, తర్వాత **Upload** నొక్కండి.
- **"Only .xlsx and .csv files are supported."** (.xlsx, .csv ఫైల్‌లు మాత్రమే తీసుకుంటుంది) ఫైల్‌ను .xlsx లేదా .csv గా సేవ్ చేసి మళ్లీ ప్రయత్నించండి.
- **"The selected file exceeds the 100 MB upload limit."** (ఎంచుకున్న ఫైల్ 100 MB పరిమితి కంటే పెద్దది) ఫైల్‌ను చిన్న ఫైల్‌లుగా విభజించండి.
- **Upload Failed.** (అప్‌లోడ్ విఫలమైంది) మెసేజ్ చదవండి. ఆ కార్డుకు సరైన రిపోర్ట్ ఫైలేనా అని చెక్ చేసి, మళ్లీ ప్రయత్నించండి.
:::
