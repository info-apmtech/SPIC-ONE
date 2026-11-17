---
module: demos
title: డెమోలు, ట్రీట్‌మెంట్‌లు
icon: bi-flower1
order: 108
language: te
category: ఫీల్డ్ పని
note: కొత్త గైడ్
meta-source-hash: c6011d305dce737c
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b0aaf57f4576cdfb
confidence: ok

రైతు పొలంలో జరిగే పంట డెమోను (ప్రదర్శన) మొదటి నుంచి చివరి వరకు నమోదు చేయండి. రైతు, పంట వివరాలు ఒక్కసారే సేవ్ చేస్తే చాలు. తర్వాత ప్రతి పొలం విజిట్‌ను ఒక ట్రీట్‌మెంట్‌గా, మీరు గమనించినవి, ఫోటోలతో నమోదు చేసి, ప్రతి విజిట్‌ను ముందు విజిట్‌తో పోల్చి చూడండి.

- start | icon: bi-pencil-square | color: #65a30d
  title: డెమో మొదలుపెట్టండి
  text: ఆమోదం వచ్చిన CSR-2 ప్రోగ్రామ్ కోసం రైతు, ప్రదేశం, పంట వివరాలు ఎంటర్ చేయండి.
- treat | icon: bi-camera | color: #d97706
  title: ప్రతి విజిట్ నమోదు చేయండి
  text: ప్రొడక్ట్, మోతాదు (dosage), గమనించినవి, control, treated ఫోటోలు సేవ్ చేయండి.
- compare | icon: bi-arrow-left-right | color: #16a34a
  title: పోల్చి చూసి ముగించండి
  text: తర్వాతి ట్రీట్‌మెంట్ నమోదు చేసేటప్పుడు ముందు ట్రీట్‌మెంట్ కనిపిస్తుంది. ఏ సమస్యా మిగలనప్పుడు డెమోను ముగించండి.
:::

::: section flow
title: డెమో ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 672fd04ad6279036
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- csr | type: lane | color: #0891b2
  title: CSR టీమ్
  sub: CSR-1, CSR-2
- field | type: lane | color: #65a30d | roles: MDO, JMDO
  title: బాధ్యత ఉన్న వ్యక్తి
  sub: CSR-1 లో పేరు ఉన్న ఫీల్డ్ సిబ్బంది
- eligible | lane: csr | col: 1 | icon: bi-patch-check | color: #64748b | next: start | screen: demo-list
  title: CSR-2 ఆమోదం వచ్చింది
  status: Not Started
  text: CSR-1 లో మిమ్మల్ని **Responsible Person** (బాధ్యత ఉన్న వ్యక్తి) గా పెడతారు. దాని CSR-2 కి చివరి ఆమోదం వచ్చాక, ఆ డెమో మీ **Demo Documentation** లిస్ట్‌లో కనిపిస్తుంది.
- start | lane: field | col: 2 | icon: bi-pencil-square | color: #d97706 | next: treat | screen: start-documentation
  title: నమోదు మొదలుపెట్టడం
  status: In Progress
  text: రైతు, ప్రదేశం, పంట వివరాలు ఎంటర్ చేసి **Start Documentation** నొక్కండి.
- treat | lane: field | col: 3 | icon: bi-camera | color: #64748b | next: close, followup | screen: record-treatment
  title: ట్రీట్‌మెంట్ నమోదు చేయడం
  status: Draft
  text: ప్రతి పొలం విజిట్‌లో ట్రీట్‌మెంట్, గమనించినవి, ఫోటోలు నమోదు చేయండి. తర్వాత పూర్తి చేయడానికి **Save Draft** నొక్కండి, లేదా **Complete Treatment** నొక్కండి.
- followup | lane: field | col: 4 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: treat | to: treat | screen: compare-treatments
  title: సమస్య కనిపించింది, మళ్లీ విజిట్
  status: In Progress
  text: **Problem Observed?** లో **Yes** పెట్టి ట్రీట్‌మెంట్ పూర్తి చేస్తే, డెమో తెరిచే ఉంటుంది. తర్వాతి విజిట్ కోసం **Start Next Treatment** నొక్కండి.
- close | lane: field | col: 5 | icon: bi-clipboard-check | color: #16a34a | screen: demo-details
  title: డెమో పూర్తయింది
  status: Completed
  text: **Problem Observed?** లో **No** పెట్టి ఒక ట్రీట్‌మెంట్ పూర్తి చేస్తే, మొత్తం డెమో పూర్తవుతుంది. తర్వాత దాన్ని చూడటం మాత్రమే కుదురుతుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c13ab7e2ba0afb47
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: డెమో మొదటి విజిట్ కోసం ఎదురుచూస్తోంది. మొదలుపెట్టే ఫారం డ్రాఫ్ట్‌గా సేవ్ చేసినా ఇదే కనిపిస్తుంది.
- in-progress | color: #d97706
  label: In Progress
  text: నమోదు మొదలైంది. ట్రీట్‌మెంట్‌లు నమోదు అవుతున్నాయి.
- draft | color: #64748b
  label: Draft
  text: ఒక ట్రీట్‌మెంట్ సేవ్ అయ్యింది కానీ పూర్తి కాలేదు. దాన్ని ఇంకా మార్చవచ్చు.
- completed | color: #16a34a
  label: Completed
  text: ట్రీట్‌మెంట్ అయితే, అది పూర్తయింది, మార్చలేరు. డెమో అయితే, అది మూసివేయబడింది, చూడటం మాత్రమే కుదురుతుంది.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 94984548531519c1
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీరు బాధ్యత ఉన్న వ్యక్తిగా ఉన్న డెమోలు మాత్రమే మీకు కనిపిస్తాయి.

- field | color: #65a30d | roles: MDO, JMDO
  title: బాధ్యత ఉన్న వ్యక్తి
  - ఆమోదం వచ్చిన ప్రతి డెమోకు నమోదు మొదలుపెట్టడం
  - ప్రతి పొలం విజిట్‌ను ట్రీట్‌మెంట్‌గా నమోదు చేయడం
  - control, treated ఫోటోలు అప్‌లోడ్ చేయడం
  - ఏ సమస్యా మిగలనప్పుడు డెమోను ముగించడం
- csr | color: #0891b2
  title: CSR టీమ్
  - CSR-1 తయారు చేసి, Responsible Person (బాధ్యత ఉన్న వ్యక్తి) ని ఎంచుకోవడం
  - డెమోను నమోదు చేయగలిగేలా CSR-2 ని ఆమోదించడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 03c835ce85e68364
confidence: ok

- not-listed | popular: true
  q: నా డెమో లిస్ట్‌లో లేదు
  a: CSR-2 కి చివరి ఆమోదం వచ్చిన, CSR-1 లో మీరు **Responsible Person** గా ఉన్న ప్రోగ్రామ్‌లు మాత్రమే లిస్ట్‌లో కనిపిస్తాయి. CSR-1 తయారు చేసిన వ్యక్తిని దాన్ని చెక్ చేయమని అడగండి.
- no-next | popular: true
  q: నాకు Start Next Treatment కనిపించడం లేదు
  a: మీ చివరి ట్రీట్‌మెంట్ **Completed** అయి, డెమో మూసివేయబడకపోతేనే అది కనిపిస్తుంది. చివరి ట్రీట్‌మెంట్ **Draft** అయితే, **View Details** తో దాన్ని తెరిచి ముందు దాన్ని పూర్తి చేయండి.
- closed-early
  q: ఒక ట్రీట్‌మెంట్ పూర్తి చేశాక డెమో మూసుకుపోయింది
  a: **Problem Observed?** లో **No** పెట్టి ట్రీట్‌మెంట్ పూర్తి చేస్తే, మొత్తం డెమో పూర్తవుతుంది. పూర్తయిన డెమోను చూడటం మాత్రమే కుదురుతుంది. కాబట్టి **Complete Treatment** నొక్కే ముందు ఈ జవాబు చెక్ చేయండి.
- photos
  q: నా ఫోటోలు సేవ్ కాలేదు
  a: **Save Draft** లేదా **Complete Treatment** నొక్కినప్పుడే ఫోటోలు అప్‌లోడ్ అవుతాయి. 10 MB లేదా అంతకంటే తక్కువ ఉన్న JPG లేదా PNG ఫైల్‌లు, ఒకేసారి 20 వరకు వాడండి.
- missing-id
  q: "Missing or invalid csr2Id in URL" అని కనిపిస్తోంది
  a: డెమోను ఎంచుకోకుండా స్క్రీన్ తెరిచారు. **Demo Documentation** తెరిచి, ఆ డెమో యొక్క **Action** కాలమ్‌లో ఉన్న బటన్ నొక్కండి.
:::

::: section demo-list
title: నా డెమోలు చూడటం
routes: /DemoDocumentation
for: 
roles: 
designations: 
minutes: 2
source-hash: 4947901ab12cde8b
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Demo Documentation** స్క్రీన్ మీకు కేటాయించిన, ఆమోదం వచ్చిన ప్రతి CSR-2 ప్రోగ్రామ్‌ను చూపిస్తుంది. ప్రతి దాని దశ, ఎన్ని ట్రీట్‌మెంట్‌లు పూర్తయ్యాయో కూడా చూపిస్తుంది.

1. పైన ఉన్న కార్డులు చూడండి: **Total Demonstrations**, **Not Started**, **In Progress**, **Completed**.
2. **Filters** కింద, **Sanction Number**, **Program Name**, **Product**, **Crop**, **District**, **Stage** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
3. శాంక్షన్ నంబర్, ప్రోగ్రామ్, ప్రొడక్ట్, పంట లేదా ప్రదేశంతో డెమోను కనుక్కోవడానికి **Search demos...** బాక్స్‌లో టైప్ చేయండి.
4. టేబుల్‌లో, ప్రతి డెమోకు **Current Stage**, **Date of Sowing**, **Treatments**, **Last Treatment**, **Document Status** చదవండి.
5. **Action** కాలమ్‌లో, కొత్త డెమోకు **Start Documentation**, జరుగుతున్న డెమోకు **View / Continue**, పూర్తయిన డెమోకు **View** నొక్కండి.
6. ఇంకా వరుసలు చూడటానికి కింద ఉన్న **Items Per Page**, పేజీ నంబర్లు వాడండి.

> Tip: మీకు కేటాయించిన, ఆమోదం వచ్చిన CSR-2 ప్రోగ్రామ్‌లు మాత్రమే లిస్ట్‌లో ఉంటాయి.

## సాధారణ సమస్యలు

- **"No eligible demonstration records found".** (అర్హత ఉన్న డెమోలు ఏవీ లేవు) ఆమోదం వచ్చిన ఏ CSR-2 లోనూ ఇంకా మీరు బాధ్యత ఉన్న వ్యక్తిగా లేరు, లేదా మీరు పెట్టిన ఫిల్టర్‌లు మరీ ఎక్కువగా ఉన్నాయి. **Clear All** నొక్కండి.
- **"Unable to load approved CSR-2 demonstrations".** (ఆమోదం వచ్చిన CSR-2 డెమోలను చూపించలేకపోయింది) యాప్ సర్వర్‌ను చేరలేకపోయింది. మీ ఇంటర్నెట్ చెక్ చేసి, స్క్రీన్‌ను మళ్లీ తెరవండి.
:::

::: section start-documentation
title: డెమోకు నమోదు మొదలుపెట్టడం
routes: /StartDocumentation
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-sd-program] | 2 > [data-help=dm-sd-farmer] | 3 > [data-help=dm-sd-crop] | 4 > [data-help=dm-sd-actions]
minutes: 4
source-hash: a86f4d3929f78e21
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మొదటి విజిట్‌లో డెమో యొక్క రైతు, ప్రదేశం, పంట వివరాలు సేవ్ చేయడానికి **Start Documentation** స్క్రీన్ వాడండి. లిస్ట్‌లో **Start Documentation** నొక్కి దీన్ని తెరవండి.

1. **Program Details (From CSR-2)** చెక్ చేయండి: **Program Type**, **Program Name**, **Sanction Number**, **Actual Event Location**, **Focus Products**.
2. **Farmer & Location Details** కింద, **Farmer Name**, **Mobile Number**, **Place / Village**, **Farmer Address** ఎంటర్ చేయండి. **Crop**, **Crop Type** ఎంచుకుని, **Pin Code** ఎంటర్ చేసి, తర్వాత **State**, **District** ఎంచుకోండి.
3. **Crop Information** కింద, **Date Of Sowing** ఎంటర్ చేసి, **Current Crop Stage** ఎంచుకుని, **Expected Harvest Date** ఎంటర్ చేసి, అవసరమైతే **Remarks** రాయండి.
4. తర్వాత పూర్తి చేయడానికి **Save Draft** నొక్కండి, లేదా మొదలుపెట్టడానికి **Start Documentation** నొక్కండి. **Start Documentation** నొక్కితే **Demo Details** స్క్రీన్ తెరుచుకుంటుంది.

> Careful: **Start Documentation** నొక్కిన తర్వాత, లిస్ట్ నుంచి ఈ ఫారం బదులు **Demo Details** తెరుచుకుంటుంది. మొదలుపెట్టే ముందు వివరాలు చెక్ చేయండి.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Farmer Name** | డెమో పొలం యజమాని అయిన రైతు. తప్పనిసరి. |
| **Mobile Number** | రైతు యొక్క 10 అంకెల మొబైల్ నంబర్. తప్పనిసరి. |
| **Crop** | ఈ CSR-2 యొక్క focus products లో ఉన్న పంటలు మాత్రమే కనిపిస్తాయి. తప్పనిసరి. |
| **Crop Type** మరియు **Current Crop Stage** | పంట ఎంచుకున్న తర్వాత ఇవి వస్తాయి. తప్పనిసరి. |
| **Pin Code** | 6 అంకెలు. తప్పనిసరి. |
| **State** మరియు **District** | ముందు State, తర్వాత District ఎంచుకోండి. తప్పనిసరి. |

## సాధారణ సమస్యలు

- **"Select State, District, Crop, Crop Type and Current Crop Stage."** (State, District, Crop, Crop Type, Current Crop Stage ఎంచుకోండి) వీటిలో ఏదో ఒక లిస్ట్ ఖాళీగా ఉంది. ప్రతి దాన్నీ ఎంచుకోండి.
- **"Enter farmer name, a 10-digit mobile number and a 6-digit PIN code."** (రైతు పేరు, 10 అంకెల మొబైల్ నంబర్, 6 అంకెల PIN code ఎంటర్ చేయండి) మొబైల్ నంబర్, PIN code చెక్ చేయండి.
- **"This CSR-2 is not finally approved or is outside your permitted scope."** (ఈ CSR-2 కి చివరి ఆమోదం లేదు, లేదా అది మీకు అనుమతి ఉన్న పరిధి బయట ఉంది) ఈ డెమో మీకు కేటాయించలేదు. లిస్ట్‌కు తిరిగి వెళ్లండి.
:::

::: section demo-details
title: ఒక డెమోను, దాని ట్రీట్‌మెంట్‌లను చూడటం
routes: /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 7530f32fb5322456
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Demo Details** స్క్రీన్ డెమో సారాంశం, రైతు వివరాలు, **Treatment Journey** (నమోదైన అన్ని ట్రీట్‌మెంట్‌ల లిస్ట్) చూపిస్తుంది.

1. లిస్ట్‌లో **View / Continue** లేదా **View** నొక్కి డెమో తెరవండి.
2. పైన ఉన్న కార్డు చెక్ చేయండి: ప్రొడక్ట్, **Sanction Number**, **Crop**, **Date of Sowing:**, **Crop Stage:**, **Expected Harvest:**.
3. రైతు కార్డు చెక్ చేయండి: **Farmer**, **Mobile Number**, **District**, **State**, **Pin Code**, **Location**.
4. ఇంకా ఏ ట్రీట్‌మెంట్ నమోదు కాకపోతే, **Start First Treatment** నొక్కండి.
5. **Treatment Journey** లో, ప్రతి ట్రీట్‌మెంట్ దాని స్టేటస్, **Visit Date**, **Plant Stage**, **Dosage**, **Remarks** చూపిస్తుంది. పూర్తయినవి ఎన్నో **Treatments Done** లెక్క చూపిస్తుంది.
6. ఒక ట్రీట్‌మెంట్‌ను తెరవడానికి దాని మీద **View Details** నొక్కండి.
7. చివరి ట్రీట్‌మెంట్ **Completed** అయ్యాక, తర్వాతి విజిట్ కోసం **Start Next Treatment** నొక్కండి.

## సాధారణ సమస్యలు

- **Start Next Treatment బటన్ లేదు.** చివరి ట్రీట్‌మెంట్ ఇంకా **Draft** గా ఉంది, లేదా డెమో **Completed** అయిపోయింది.
- **"Farmer and crop documentation is not available yet."** (రైతు, పంట నమోదు ఇంకా లేదు) మొదలుపెట్టే ఫారం ఇంకా మొదలుపెట్టలేదు. లిస్ట్ నుంచి డెమో తెరిచి **Start Documentation** నొక్కండి.
- **"CSR-2 not found, not finally approved, or outside your permitted scope."** (CSR-2 దొరకలేదు, చివరి ఆమోదం లేదు, లేదా మీకు అనుమతి ఉన్న పరిధి బయట ఉంది) ఈ డెమో మీకు కేటాయించలేదు.
:::

::: section record-treatment
title: పొలం విజిట్‌లో ట్రీట్‌మెంట్ నమోదు చేయడం
routes: /TreatmentDetails
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-td-visit] | 2 > [data-help=dm-td-treatment] | 3 > [data-help=dm-td-observations] | 4 > [data-help=dm-td-images] | 5 > [data-help=dm-td-problem] | 6 > [data-help=dm-td-actions]
minutes: 5
source-hash: d955aff37a8e757f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ప్రతి పొలం విజిట్‌లో ఏమి వేశారో, మీరు ఏమి చూశారో, control, treated మొక్కల ఫోటోలు నమోదు చేయడానికి ట్రీట్‌మెంట్ స్క్రీన్ (ఉదాహరణకు **Treatment 01**) వాడండి. **Start First Treatment**, **Start Next Treatment** లేదా **View Details** తో దీన్ని తెరవండి.

1. **Visit Details** కింద, **Visit Date**, **Field Officer / Visibility** చెక్ చేసి, తర్వాత **Current Plant Stage**, **Plant Condition** ఎంచుకోండి.
2. **Treatment Applied** కింద, **Product Used** ఎంచుకుని, **Dosage** ఎంటర్ చేసి, **Application Method** ఎంచుకుని, **Treatment Date** ఎంటర్ చేసి, **Stage of Application** ఎంచుకుని, **Remarks** రాయండి.
3. **Observations** కింద, **Physical Parameters**, **Yield Parameters** రాసి, సరిపోయే **Quick Observations** నొక్కి, **Observation Notes** రాయండి.
4. **Control Images**, **Treated Images** కింద, **Upload Image** నొక్కి ఫోటోలు ఎంచుకోండి. Control ఫోటోలు ప్రొడక్ట్ వేయని మొక్కలను, treated ఫోటోలు ప్రొడక్ట్ వేసిన మొక్కలను చూపిస్తాయి.
5. **Problem Observed?** కింద, **Yes** లేదా **No** ఎంచుకోండి. **Yes** అయితే, **Problem Category**, **Problem Description**, **Severity**, **Recommendation / Monitoring Action**, **Action Taken** నింపండి.
6. తర్వాత పూర్తి చేయడానికి **Save Draft** నొక్కండి, లేదా విజిట్ అయిపోయాక **Complete Treatment** నొక్కండి.

> Careful: **Problem Observed?** మొదట్లో **Yes** అనే ఉంటుంది. **No** పెట్టి ట్రీట్‌మెంట్ పూర్తి చేస్తే, మొత్తం డెమో పూర్తవుతుంది, ఇక కొత్త ట్రీట్‌మెంట్‌లు జోడించలేరు.

> Tip: రెండో ట్రీట్‌మెంట్ నుంచి, పైన ఉన్న **Previous Treatment Reference** బాక్స్ ముందు విజిట్‌ను చూపిస్తుంది. కాబట్టి పోల్చి చూడవచ్చు.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Visit Date** మరియు **Treatment Date** | ముందే ఈరోజు తేదీ ఉంటుంది. ట్రీట్‌మెంట్ తేదీ విజిట్ తేదీ కంటే ముందు ఉండకూడదు. పూర్తి చేయడానికి తప్పనిసరి. |
| **Current Plant Stage** | ఈరోజు చూసిన పంట దశ. పూర్తి చేయడానికి తప్పనిసరి. |
| **Product Used** | వేసిన ప్రొడక్ట్. పూర్తి చేయడానికి తప్పనిసరి. |
| **Dosage** | ఉదాహరణకు 2 ml/L. |
| **Control Images**, **Treated Images** | JPG లేదా PNG, ఒక్కొక్కటి 10 MB లేదా అంతకంటే తక్కువ, ఒకేసారి 20 వరకు. |

## సాధారణ సమస్యలు

- **"Enter Visit Date, Treatment Date, Current Plant Stage and Product Used before completing the treatment."** (ట్రీట్‌మెంట్ పూర్తి చేసే ముందు ఈ నాలుగూ ఎంటర్ చేయండి) ఈ నాలుగు ఫీల్డ్‌లు నింపి, మళ్లీ **Complete Treatment** నొక్కండి.
- **"Treatment date cannot precede visit date."** (ట్రీట్‌మెంట్ తేదీ విజిట్ తేదీ కంటే ముందు ఉండకూడదు) **Treatment Date** ని విజిట్ తేదీ లేదా దాని తర్వాతి తేదీకి మార్చండి.
- **"Only JPG and PNG images are allowed."** లేదా **"Each image must be 10MB or less."** (JPG, PNG మాత్రమే; ప్రతి ఫోటో 10MB లేదా అంతకంటే తక్కువ) వేరే ఫోటోలు ఎంచుకోండి, లేదా వాటి సైజు తగ్గించండి.
- **"This treatment has already been completed."** (ఈ ట్రీట్‌మెంట్ ఇప్పటికే పూర్తయింది) పూర్తయిన ట్రీట్‌మెంట్‌ను చూడటం మాత్రమే కుదురుతుంది.
:::

::: section compare-treatments
title: ట్రీట్‌మెంట్‌లను పోల్చి చూసి ఫాలో-అప్ చేయడం
routes: /TreatmentDetails, /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 393196d831ee40ab
confidence: ok

**ఈ స్క్రీన్ దేనికి**
సమస్య కనిపిస్తే, డెమో తెరిచే ఉంటుంది, మీరు మళ్లీ పొలానికి వెళ్తారు. ప్రతి కొత్త ట్రీట్‌మెంట్ ముందు దాన్ని చూపిస్తుంది. కాబట్టి పంట మెరుగైందో లేదో చూడవచ్చు.

1. **Demo Details** లో, **Start Next Treatment** నొక్కండి. తర్వాతి ట్రీట్‌మెంట్ నంబర్ తెరుచుకుంటుంది.
2. **Previous Treatment Reference** బాక్స్ చదవండి: **Visit Date**, **Current Plant Stage**, **Dosage Applied**, **Method**, **Field Observation**, **Yield Parameters**.
3. ముందు ట్రీట్‌మెంట్‌ను దాని ఫోటోలతో తెరవడానికి **View Full Details** నొక్కండి.
4. ఈరోజు విజిట్, ఫోటోలు అదే విధంగా నమోదు చేసి, ఏమి మారిందో **Observation Notes** లో రాయండి.
5. **Treatment Journey** లో అన్ని ట్రీట్‌మెంట్‌లు చూడటానికి **Back to Demo Details** నొక్కండి.

## సాధారణ సమస్యలు

- **Previous Treatment Reference బాక్స్ లేదు.** ఇది రెండో ట్రీట్‌మెంట్ నుంచి మాత్రమే కనిపిస్తుంది.
- **పాత ట్రీట్‌మెంట్‌ను మార్చాలి.** **Completed** అయిన ట్రీట్‌మెంట్‌ను మార్చలేరు. సరిచేసిన విషయాన్ని తర్వాతి ట్రీట్‌మెంట్‌లో రాయండి.
:::

::: section sample-screens
title: నమూనా ట్రీట్‌మెంట్ స్క్రీన్‌లు
routes: /TreatmentDemoDetails, /Treatment01, /Treatment02
for: 
roles: 
designations: 
minutes: 1
source-hash: 13af1fa77b22fd32
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Treatment Demo Details**, **Treatment 01**, **Treatment 02** మెనూలు మారని ఉదాహరణ విలువలతో నమూనా స్క్రీన్‌లను తెరుస్తాయి. ఒక డెమో ఎలా ఉంటుందో అవి చూపిస్తాయి. అక్కడ మీరు టైప్ చేసేది ఏదీ సేవ్ అవ్వదు.

1. **Treatment Demo Details** మూడు ట్రీట్‌మెంట్‌లు ఉన్న **Treatment Journey** తో ఒక ఉదాహరణ **Demo Details** స్క్రీన్ చూపిస్తుంది.
2. **Treatment 01** పూర్తయిన ఒక ట్రీట్‌మెంట్ ఉదాహరణను చూపిస్తుంది: **Visit Details**, **Treatment Applied**, **Observations**, ఫోటోలు, **Problem Identification & Recommendation**.
3. **Treatment 02** పైన **Previous Treatment Reference - Treatment 01** బాక్స్‌తో రెండో ట్రీట్‌మెంట్ ఉదాహరణను చూపిస్తుంది.
4. నిజమైన పని నమోదు చేయడానికి, **Demo Documentation** తెరిచి మీ డెమో ఎంచుకోండి.

> Careful: ఈ నమూనా స్క్రీన్‌లలోని బటన్‌లు ఏదీ సేవ్ చేయవు.

## సాధారణ సమస్యలు

- **నేను ఎంటర్ చేసినవి కనిపించడం లేదు.** ఇవి నమూనా స్క్రీన్‌లు. నిజమైన డెమో నమోదు చేయడానికి **Demo Documentation** వాడండి.
:::
