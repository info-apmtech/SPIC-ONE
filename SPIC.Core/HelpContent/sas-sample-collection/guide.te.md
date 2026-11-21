---
module: sas-sample-collection
title: మట్టి శాంపిల్ సేకరణ
icon: bi-droplet
order: 50
language: te
category: మట్టి పరీక్ష
note: శాంపిల్ ప్రయాణం, స్టేటస్‌లు, గైడెడ్ టూర్‌లతో కొత్త రూపం
meta-source-hash: 91cc9c6288b84a36
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 722480765e5f4cad
confidence: ok

SPIC రైతు పొలంలోని మట్టిని, నీటిని జిల్లా ల్యాబ్‌లో పరీక్షిస్తుంది. పొలం నుంచి రైతు రిపోర్ట్ వరకు ప్రతి శాంపిల్‌ను యాప్ ఫాలో చేస్తుంది. కాబట్టి అది ఎక్కడ ఉందో అందరూ చూడగలరు.

- collect | icon: bi-droplet | color: #0d9488
  title: పొలంలో సేకరణ
  text: MDO లేదా JMDO రైతు వివరాలు, శాంపిల్స్ నమోదు చేస్తారు.
- pay | icon: bi-qr-code | color: #d97706
  title: చెల్లింపు, ఆమోదం
  text: పెయిడ్ శాంపిల్‌కి UPI లేదా వేరే పద్ధతిలో డబ్బు చెల్లిస్తారు. తర్వాత దానికి ఆమోదం వస్తుంది, Finance దాన్ని చెక్ చేస్తుంది.
- report | icon: bi-file-earmark-text | color: #7c3aed
  title: రైతుకు ల్యాబ్ రిపోర్ట్
  text: ల్యాబ్ శాంపిల్స్‌ను పరీక్షిస్తుంది. రైతు రిపోర్ట్‌ను తమిళం లేదా ఇంగ్లీష్‌లో డౌన్‌లోడ్ చేసుకుంటారు.
:::

::: section flow
title: శాంపిల్ ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 8e557635907c2edf
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. ఫ్రీ శాంపిల్‌కి చెల్లింపు దశలు ఉండవు. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- officer | type: lane | color: #0d9488 | roles: MDO, JMDO
  title: ఫీల్డ్ ఆఫీసర్
  sub: MDO · JMDO
- approver | type: lane | color: #ea580c | roles: Admin, CorporateAdmin
  title: ఆమోదించేవారు
  sub: చెల్లింపు ఆమోదం
- finance | type: lane | color: #2563eb
  title: Finance
  sub: చెల్లింపు నిర్ధారణ
- lab | type: lane | color: #7c3aed
  title: ల్యాబ్
  sub: జిల్లా ల్యాబ్
- farmer | type: lane | color: #16a34a | roles: Farmer
  title: రైతు
  sub: శాంపిల్ యజమాని
- collect | lane: officer | col: 1 | icon: bi-droplet | color: #64748b | next: pay | screen: collect-sample
  title: శాంపిల్స్ సేకరించడం
  status: Draft
  text: రైతును ఎంచుకుని, ప్రతి మట్టి లేదా నీటి శాంపిల్ జోడించి, సేవ్ చేయండి. ఫ్రీ శాంపిల్ నేరుగా కన్సైన్‌మెంట్‌కి వెళ్తుంది.
- pay | lane: officer | col: 2 | icon: bi-qr-code | color: #d97706 | next: approve | screen: record-payment
  title: చెల్లింపు నమోదు చేయడం
  status: Pending Payment
  text: రైతు QR కోడ్ స్కాన్ చేసి డబ్బు చెల్లిస్తారు. ఆఫీసర్ ట్రాన్సాక్షన్ వివరాలు ఎంటర్ చేసి, రసీదు అప్‌లోడ్ చేస్తారు.
- approve | lane: approver | col: 3 | icon: bi-patch-check | color: #ea580c | next: send, verify | return: pay | screen: approve-payments
  title: చెల్లింపు ఆమోదించడం
  status: Pending Approval
  text: ఆమోదించేవారు అమౌంట్, రసీదు చెక్ చేస్తారు. తర్వాత ఆమోదించి Finance కి పంపుతారు, లేదా తిరిగి పంపుతారు.
- send | lane: officer | col: 4 | icon: bi-truck | color: #0d9488 | next: test | screen: send-consignment
  title: ల్యాబ్‌కి పంపడం
  status: Dispatched
  text: ఆఫీసర్ శాంపిల్స్‌ను కొరియర్‌కి ఇచ్చి, ట్రాకింగ్ నంబర్ నమోదు చేస్తారు. దీన్నే కన్సైన్‌మెంట్ అంటారు.
- verify | lane: finance | col: 4 | icon: bi-bank | color: #2563eb | screen: verify-payments
  title: డబ్బు అందిందని నిర్ధారించడం
  status: Finance Verified
  text: డబ్బు అకౌంట్‌లోకి వచ్చిందని Finance నిర్ధారిస్తుంది, లేదా సరిపోలలేదు (mismatch) అని గుర్తిస్తుంది.
- test | lane: lab | col: 5 | icon: bi-eyedropper | color: #7c3aed | next: report
  title: శాంపిల్స్ పరీక్షించడం
  status: In Progress
  text: ల్యాబ్ పార్సిల్ అందుకుని, ప్రతి శాంపిల్‌ను పరీక్షించి, ఫలితాలు ప్రచురిస్తుంది.
- report | lane: farmer | col: 6 | icon: bi-file-earmark-arrow-down | color: #16a34a | screen: farmer-my-reports
  title: రిపోర్ట్ డౌన్‌లోడ్ చేయడం
  status: Completed
  text: రైతు యాప్‌లో రిపోర్ట్ చూసి, దాన్ని తమిళం లేదా ఇంగ్లీష్‌లో డౌన్‌లోడ్ చేసుకుంటారు.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 0771c49c0ffc8c78
confidence: ok

- draft | color: #64748b
  label: Draft
  text: సేవ్ అయ్యింది, ఇంకా సబ్మిట్ కాలేదు.
- pending-payment | color: #d97706
  label: Pending Payment
  text: చెల్లింపు నమోదు కోసం ఎదురుచూస్తోంది.
- pending-approval | color: #ea580c
  label: Pending Approval
  text: చెల్లింపు నమోదైంది, ఆమోదం కోసం ఎదురుచూస్తోంది.
- ready | color: #0891b2
  label: Ready consignment
  text: ల్యాబ్‌కి పంపడానికి సిద్ధంగా ఉంది.
- dispatched | color: #0d9488
  label: Dispatched
  text: కొరియర్‌కి ఇచ్చారు.
- in-transit | color: #0d9488
  label: In Transit
  text: ల్యాబ్‌కి వెళ్తున్న దారిలో ఉంది.
- delivered | color: #2563eb
  label: Delivered to Lab
  text: పార్సిల్ ల్యాబ్‌కి చేరింది.
- in-progress | color: #7c3aed
  label: In Progress
  text: ల్యాబ్ పరీక్షిస్తోంది.
- completed | color: #16a34a
  label: Completed
  text: ఫలితాలు వచ్చాయి.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 9c070a1e6fc377cf
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- officer | color: #0d9488 | roles: MDO, JMDO
  title: MDO, JMDO
  - శాంపిల్స్ సేకరించడం
  - చెల్లింపులు నమోదు చేయడం
  - కన్సైన్‌మెంట్‌లను ల్యాబ్‌కి పంపడం
- approver | color: #ea580c | roles: Admin, CorporateAdmin
  title: ఆమోదించేవారు
  - చెల్లింపులను ఆమోదించడం లేదా తిరిగి పంపడం
  - కన్సైన్‌మెంట్‌లను తర్వాతి దశకు మార్చడం
  - ల్యాబ్ ఫలితాలు ఎంటర్ చేయడం
- finance | color: #2563eb
  title: Finance
  - అందిన డబ్బును నిర్ధారించడం
  - సరిపోలని (mismatch) చెల్లింపులను గుర్తించడం
- farmer | color: #16a34a | roles: Farmer
  title: రైతు
  - మీ శాంపిల్స్ చూడటం
  - మీ రిపోర్ట్‌లు డౌన్‌లోడ్ చేయడం
  - మీ చెల్లింపులు చెక్ చేయడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: b121292d9fb622a2
confidence: ok

- no-menu | popular: true
  q: మెనూలో Sample Collection కనిపించడం లేదు
  a: మీ హోదాకు (designation) ఈ పేజీ లేదు. మీ అడ్మినిస్ట్రేటర్‌ని అడగండి. JMDO వాళ్లు బదులుగా **Soil Sample** టాస్క్ నుంచి దీన్ని తెరవాలి.
- payment-rejected | popular: true
  q: నా చెల్లింపు తిరస్కరించబడింది. ఇప్పుడు ఏమి చేయాలి?
  a: వివరాల పేజీలో కారణంతో పాటు **Payment rejected** అని కనిపిస్తుంది. సరిచేసి మళ్లీ **Record payment** నొక్కండి.
- not-ready
  q: "This collection is not ready to send" అని వస్తోంది
  a: చెల్లింపు ఇంకా నమోదు కాలేదు, లేదా ఆమోదం రాలేదు. ఫ్రీ శాంపిల్స్‌ను వెంటనే పంపవచ్చు.
- report-language
  q: రిపోర్ట్‌ను తమిళంలో ఎలా పొందాలి?
  a: **Download PDF** పక్కన ఉన్న చిన్న బాణం నొక్కి, తమిళం లేదా ఇంగ్లీష్ ఎంచుకోండి.
:::

::: section collection-list
title: మీ శాంపిల్ సేకరణలను కనుక్కోవడం, ట్రాక్ చేయడం
routes: /SampleCollection
for: 
roles: 
designations: 
screenshot: collection-list.png
hotspots: 1 @ 2.2,68.5,95.5,15.1 | 2 @ 73.7,88,22.4,4.8 | 3 @ 2.2,30.7,95.5,35.4 | 4 @ 3.8,94.7,92.3,5.3 | 5 @ 62.1,1.9,8.1,4.8 | 6 @ 79.2,1.9,18.5,4.8
screenshot-phone: collection-list.phone.jpg
hotspots-phone: 1 @ 3.1,40.6,93.8,24.4 | 2 @ 6.4,69.4,87.2,3.2 | 3 @ 3.1,36,93.8,3.2 | 4 @ 2,2,8,4 | 5 @ 15.3,4.3,10.2,3 | 6 @ 39.7,4.3,48.8,3
tour: 1 > [data-help=sc-tiles] | 2 > [data-help=sc-search] | 3 > [data-help=sc-filters] | 4 > [data-help=sc-table] | 5 > [data-help=sc-excel] | 6 > [data-help=sc-new]
minutes: 2
source-hash: 01315bd944ae112a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sample Collection** పేజీ మీరు చూడగలిగే ప్రతి సేకరణను, దాని చెల్లింపు, ల్యాబ్ స్టేటస్‌తో చూపిస్తుంది. ఒక సేకరణను కనుక్కుని, దాని వివరాలు తెరవడానికి దీన్ని వాడండి.

పైన ఉన్న కార్డులు లెక్కలు చూపిస్తాయి: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level**, **Ready to Submit**.

1. **Pending Payment** లేదా **Draft** లాంటి రంగు టైల్ నొక్కితే ఆ సేకరణలు మాత్రమే కనిపిస్తాయి. మళ్లీ నొక్కితే అన్నీ కనిపిస్తాయి.
2. వెతకడానికి, **Search collections...** లో రైతు పేరు లేదా సేకరణ ID టైప్ చేయండి.
3. ఇంకా ఆప్షన్‌ల కోసం, **Filters** తెరిచి, **Date Range**, **Location**, **Payment Type**, **Payment Status** లేదా **Status** సెట్ చేసి, **Apply Filters** నొక్కండి. వాటిని తీసేయడానికి **Clear All** నొక్కండి.
4. సేకరణ తెరవడానికి కన్ను ఐకాన్ నొక్కండి (ఫోన్‌లో అయితే కార్డు నొక్కండి).
5. స్క్రీన్‌పై కనిపిస్తున్న లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** లేదా **PDF** నొక్కండి. తాజా స్టేటస్ లోడ్ చేయడానికి **Refresh Data** నొక్కండి.
6. శాంపిల్స్ సేకరించడం మొదలుపెట్టడానికి **New Sample Collection** నొక్కండి.

## సాధారణ సమస్యలు

- **No sample collections found** (సేకరణలు ఏవీ దొరకలేదు): ఫిల్టర్‌లు తీసేయండి, లేదా తేదీల పరిధి చెక్ చేయండి.
- **Collections could not be loaded** (సేకరణలు లోడ్ కాలేదు): మీ ఇంటర్నెట్ చెక్ చేసి **Try again** నొక్కండి.
- **New Sample Collection** బటన్ కనిపించడం లేదు: మీ అకౌంట్‌తో సేకరణలు తయారు చేయలేరు. హోదా అనుమతించే MDO, JMDO, అడ్మినిస్ట్రేటర్ అకౌంట్‌లు మాత్రమే శాంపిల్స్ సేకరించగలవు.
- డౌన్‌లోడ్‌లో కొన్ని వరుసలు మాత్రమే ఉన్నాయి: **Excel**, **PDF** ఇప్పుడు ఉన్న పేజీని మాత్రమే సేవ్ చేస్తాయి. ఇంకా ఎక్కువ రావాలంటే **Items Per Page** మార్చండి.
:::

::: section collect-sample
title: కొత్త మట్టి లేదా నీటి శాంపిల్ సేకరించడం
routes: /SampleCollection/new
for: SampleCollection.Entry
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
screenshot: collect-sample.png
hotspots: 1 @ 66,1.9,31.8,5 | 2 @ 4,34.7,29.7,7.8 | 3 @ 35.2,81.7,29.7,8 | 4 @ 78.7,100,17.2,0 | 5 @ 81.8,100,16,0
screenshot-phone: collect-sample.phone.jpg
hotspots-phone: 1 @ 3.1,4.6,93.8,3.2 | 2 @ 6.9,33.7,86.2,8.5 | 3 @ 6.9,100,86.2,0 | 4 @ 6.9,100,86.2,0 | 5 @ 43.8,92.1,52.1,3
tour: 1 > [data-help=sc-paytype] | 2 > [data-help=sc-farmer] | 3 > [data-help=sc-soil] | 4 > [data-help=sc-add] | 5 > [data-help=sc-save]
minutes: 4
source-hash: 3ae93abc0337f008
confidence: ok

**ఈ స్క్రీన్ దేనికి**
రైతుల దగ్గర మీరు తీసుకున్న శాంపిల్స్ నమోదు చేయడానికి **New Sample Collection** వాడండి. ఒక సేకరణలో చాలా మంది రైతుల శాంపిల్స్ ఉండవచ్చు.

**Sample Collection** పేజీలో **New Sample Collection** నొక్కి దీన్ని తెరవండి. JMDO: **Tasks Allocation** లో **Soil Sample** కార్డుపై **Start** నొక్కండి, లేదా ఒక టాస్క్ యొక్క **Quick actions** లో **Soil Sample Collection** నొక్కండి.

1. పైన **Free Sample** లేదా **Paid Sample** ఎంచుకోండి.
2. **Farmer Name** లో కనీసం రెండు అక్షరాలు టైప్ చేసి రైతును ఎంచుకోండి. రైతు కొత్తవారైతే, **New Farmer** నొక్కండి. పెయిడ్ శాంపిల్ అయితే, **Paid Sample Category** కింద **Farmer** లేదా **NGO** ఎంచుకోండి.
3. **Soil Sample**, **Water Sample** ను **Yes** లేదా **No** గా సెట్ చేయండి. **Crop 1**, **Crop 2** ఎంచుకుని, అవసరమైతే **Remarks** రాయండి.
4. **Add Another Sample** నొక్కండి. శాంపిల్ దాని ధరతో **Sample Added (Preview)** లో కనిపిస్తుంది. ప్రతి శాంపిల్‌కి ఇలాగే చేయండి. పెన్సిల్ ఒక వరుసను మారుస్తుంది, బిన్ (చెత్తబుట్ట) దాన్ని తీసేస్తుంది.
5. తర్వాత పూర్తి చేయడానికి **Save Draft** నొక్కండి, లేదా **Save and Continue** నొక్కండి. **Sample Summary** చెక్ చేసి, **Continue to payment** (పెయిడ్) లేదా **Continue to consignment** (ఫ్రీ) నొక్కండి.

> Tip: ఒక సేకరణలో చాలా మంది రైతుల శాంపిల్స్ ఉండవచ్చు.

## ఫీల్డ్‌ల వివరాలు

- **Collection ID**: మీరు సేవ్ చేసినప్పుడు యాప్ తయారు చేస్తుంది.
- **Collected By**: మీ పేరు, ముందే నిండి ఉంటుంది.
- **Total Amount**: అన్ని పెయిడ్ శాంపిల్స్‌కి కలిపి ఛార్జీ. ఫ్రీ సేకరణకు **Free** అని కనిపిస్తుంది.
- **New Farmer**: **Farmer Name**, 10 అంకెల **Mobile Number** కావాలి. **Use my location** నొక్కితే **Latitude**, **Longitude** నిండుతాయి.

## సాధారణ సమస్యలు

- "Choose a farmer, or add a new one" (రైతును ఎంచుకోండి, లేదా కొత్తవారిని జోడించండి): టైప్ చేయడం మాత్రమే కాదు, లిస్ట్ నుంచి పేరు ఎంచుకోండి.
- "Choose at least one of Soil Sample or Water Sample" (Soil Sample లేదా Water Sample లో కనీసం ఒకటి ఎంచుకోండి): వాటిలో ఒకదాన్ని **Yes** గా సెట్ చేయండి.
- రైతు అడ్రస్ సరిచేయడానికి, **Edit farmer** నొక్కి, మార్చి, తర్వాత **Save farmer details** నొక్కండి.
:::

::: section edit-draft
title: డ్రాఫ్ట్ సేకరణను పూర్తి చేయడం లేదా మార్చడం
routes: /SampleCollection/new, /SampleCollection/view/{id}
for: SampleCollection.Update
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 2
source-hash: 2699d934a7f0674d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Save Draft** తో సేవ్ చేసిన సేకరణ **Draft** గానే ఉంటుంది. మీరు సబ్మిట్ చేసే వరకు ఏదీ చెల్లింపుకు గానీ ల్యాబ్‌కి గానీ వెళ్లదు. దాన్ని మళ్లీ తెరిచి పూర్తి చేయవచ్చు.

1. **Sample Collection** తెరిచి **Draft** టైల్ నొక్కండి.
2. కన్ను ఐకాన్‌తో సేకరణ తెరవండి.
3. **Sample Collection Details** పైన ఉన్న **Edit** నొక్కండి.
4. కొత్త సేకరణలో లాగానే శాంపిల్స్ జోడించండి, మార్చండి లేదా తీసేయండి.
5. మళ్లీ డ్రాఫ్ట్‌గా ఉంచడానికి **Save Draft** నొక్కండి, లేదా **Save and Continue** నొక్కండి.
6. **Sample Summary** లో శాంపిల్స్ చెక్ చేసి **Continue to payment** లేదా **Continue to consignment** నొక్కండి.

> సబ్మిట్ చేశాక, సేకరణను ఇక మార్చలేరు.

## సాధారణ సమస్యలు

- **Edit** బటన్ కనిపించడం లేదు: సేకరణ ఇప్పటికే సబ్మిట్ అయ్యింది, లేదా మీ హోదా మార్పులకు అనుమతించదు.
- "This collection has already been submitted" (ఈ సేకరణ ఇప్పటికే సబ్మిట్ అయ్యింది): డ్రాఫ్ట్ ముందే సబ్మిట్ అయ్యింది. స్టేటస్ ఫాలో చేయడానికి దాని వివరాలు తెరవండి.
- "That draft no longer exists" (ఆ డ్రాఫ్ట్ ఇప్పుడు లేదు): అది తీసేయబడింది. కొత్త సేకరణ మొదలుపెట్టండి.
:::

::: section record-payment
title: పెయిడ్ శాంపిల్ చెల్లింపు నమోదు చేయడం
routes: /SampleCollection/payment/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: c5b827bf8559be23
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Payments** స్క్రీన్ ఎంత వసూలు చేయాలో, రైతు స్కాన్ చేయడానికి UPI QR కోడ్, చెల్లింపు వివరాలు, రసీదు కోసం ఒక ఫారం చూపిస్తుంది.

1. **Continue to payment** తర్వాత, **Payments** స్క్రీన్ తెరుచుకుంటుంది. సేకరణ వివరాల్లో **Record payment** నొక్కి కూడా తెరవవచ్చు.
2. **Total Amount to Collect** చెక్ చేయండి. శాంపిల్స్ చూడటానికి **View Sample Summary** నొక్కండి.
3. **Scan & Pay** కింద ఉన్న QR కోడ్‌ను ఏదైనా UPI యాప్‌తో స్కాన్ చేయమని రైతును అడగండి. దాన్ని సేవ్ చేయడానికి **Download QR Code** నొక్కండి.
4. **Payment Information** నింపండి.
5. **Payment Proof** కింద, **Click to Upload or Drag and Drop** నొక్కి రసీదు లేదా స్క్రీన్‌షాట్ ఎంచుకోండి.
6. **Save Payment** నొక్కండి. ఇప్పుడు చెల్లింపు ఆమోదం కోసం ఎదురుచూస్తుంది.

## ఫీల్డ్‌ల వివరాలు

- **Transaction ID** (తప్పనిసరి): UPI ట్రాన్సాక్షన్ నంబర్.
- **Paid By** (తప్పనిసరి): ఎవరు చెల్లించారు. సేకరణలోని రైతుల పేర్లు ఎంచుకోవడానికి కనిపిస్తాయి.
- **Payment Mode**: **UPI**, **Bank Transfer** లేదా **Cash**.
- **Transaction Date**, **Bank / Gateway**, **Payment Gateway / UTR No.**, **Bank Name**, **Contact Number**, **Email (optional)**: మీ దగ్గర ఉన్నవి నింపండి.
- **Remarks**: ఆమోదించేవారి కోసం ఒక నోట్.
- **Payment Proof**: PNG లేదా JPG ఫోటో, 5 MB వరకు.

## సాధారణ సమస్యలు

- "Please fill in the Transaction ID and Paid By" (Transaction ID, Paid By నింపండి): రెండూ తప్పనిసరి.
- "Pick a PNG or JPG image of 5 MB or less" (5 MB లేదా అంతకంటే తక్కువ ఉన్న PNG లేదా JPG ఫోటో ఎంచుకోండి): చిన్న ఫోటో తీయండి, లేదా స్క్రీన్‌షాట్ తీయండి.
- **Save Draft** టైప్ చేసిన వివరాలను ఈ ఫోన్‌లో మాత్రమే ఉంచుతుంది. **Save Payment** తో పూర్తి చేయండి.
- "Payment already recorded" (చెల్లింపు ఇప్పటికే నమోదైంది): అది ఆమోదం కోసం ఎదురుచూస్తోంది.
- చెల్లింపు తిరస్కరిస్తే, వివరాల పేజీలో కారణంతో పాటు **Payment rejected** అని కనిపిస్తుంది. సరిచేసి మళ్లీ **Record payment** నొక్కండి.
:::

::: section send-consignment
title: శాంపిల్స్‌ను ల్యాబ్‌కి పంపడం (కన్సైన్‌మెంట్)
routes: /SampleCollection/consignment/{id}, /SampleCollection/submitted/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: de0b21e8f58ece6a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
సేకరణ ఫ్రీ అయినప్పుడు లేదా దాని చెల్లింపుకు ఆమోదం వచ్చినప్పుడు, మీరు శాంపిల్స్ ప్యాక్ చేసి కొరియర్‌కి ఇస్తారు. ఏమి వస్తోందో ల్యాబ్‌కి తెలిసేలా **Upload Consignment** కొరియర్ వివరాలు, ఫోటోలు నమోదు చేస్తుంది.

1. సేకరణ తెరిచి **Upload consignment** నొక్కండి. ఫ్రీ శాంపిల్ అయితే, **Continue to consignment** నేరుగా దీన్ని తెరుస్తుంది.
2. **Courier Details** కింద, **Courier Service** ఎంచుకుని **Tracking Number (AWB)** టైప్ చేయండి.
3. సిద్ధంగా ఉన్న వేరే సేకరణలు కూడా అదే పార్సిల్‌లో పంపాలంటే, **Include other ready collections** తెరిచి వాటిని టిక్ చేయండి. పైన ఉన్న **Both**, **Free Sample**, **Paid Sample** నొక్కితే లిస్ట్‌లో ఏవి కనిపించాలో మారుతుంది.
4. **Upload Photos and Documents** కింద, **Shipping Label / Waybill** లో ఫోటో జోడించండి. ఫోన్‌లో **Take a photo** నొక్కవచ్చు.
5. కావాలంటే **Package and contact details** తెరవండి.
6. **Submit Consignment** నొక్కండి.
7. **Consignment Submitted Successfully!** ట్రాకింగ్ నంబర్ చూపిస్తుంది. దాన్ని కాపీ చేయడానికి కాపీ ఐకాన్ నొక్కండి, వివరాలు చూడటానికి **View Consignment** నొక్కండి, లేదా **Go to Dashboard** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Tracking Link (Optional)**, **Expected Delivery Date**: పార్సిల్‌ను ఫాలో చేయడానికి ల్యాబ్‌కి సహాయపడతాయి.
- **Package Photo**, **Any other Document**: కావాలంటే మాత్రమే జోడించే ఫోటోలు, PNG లేదా JPG, ఒక్కొక్కటి 5 MB వరకు.
- **Packaging Type**, **No. of Packages**, **Package Weight (kg)**, **Contact Person**, **Mobile**, **Email**, **Collection Notes**: తప్పనిసరి కాదు.

## సాధారణ సమస్యలు

- "Please fill the courier details and add the shipping label photo" (కొరియర్ వివరాలు నింపి, షిప్పింగ్ లేబుల్ ఫోటో జోడించండి): ఈ మూడూ తప్పనిసరి.
- "This collection is not ready to send" (ఈ సేకరణ పంపడానికి ఇంకా సిద్ధంగా లేదు): చెల్లింపు ఇంకా నమోదు కాలేదు, లేదా ఆమోదం రాలేదు.
- **Save Draft** టైప్ చేసిన ఫీల్డ్‌లను మాత్రమే ఉంచుతుంది. ఫోటోలు తర్వాత మళ్లీ జోడించండి.
- JMDO: కన్సైన్‌మెంట్ సబ్మిట్ చేస్తే ఈరోజు **Soil Sample** టాస్క్ పూర్తయినట్టు నమోదవుతుంది.
:::

::: section collection-details
title: శాంపిల్ సేకరణ వివరాలు చదవడం
routes: /SampleCollection/view/{id}
for: 
roles: 
designations: 
screenshot: collection-details.png
screenshot-phone: collection-details.phone.jpg
minutes: 2
source-hash: 0b6c9585e9bd5ccc
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sample Collection Details** ఒక సేకరణ గురించి అన్నీ చూపిస్తుంది: చెల్లింపు, కొరియర్, ల్యాబ్ ఫలితాలు, చరిత్ర. ఫీల్డ్ సిబ్బంది, డీలర్లు, మేనేజర్లు ఇక్కడ పురోగతిని ఫాలో చేయవచ్చు.

1. **Sample Collection** తెరిచి ఒక సేకరణపై కన్ను ఐకాన్ నొక్కండి.
2. పై లైన్‌లో సేకరణ ID, **Paid Sample** లేదా **Free Sample**, ప్రస్తుత స్టేటస్ కనిపిస్తాయి.
3. పట్టీలో **Consignment ID**, **Transaction ID**, **Courier Partner**, **Samples**, **No. of Tests**, **Payment Status** కనిపిస్తాయి. కన్సైన్‌మెంట్ తెరవడానికి కన్సైన్‌మెంట్ ID నొక్కండి.
4. శాంపిల్స్ ఎవరు సేకరించారో, ఒక్కో శాంపిల్‌కి ఎన్ని పరీక్షలు కావాలో **Collection Information**, **Sample Summary** చూపిస్తాయి.
5. ల్యాబ్ విలువలు సిద్ధమయ్యాక **Result Comparison** వాటిని చూపిస్తుంది. చాలా శాంపిల్స్ ఉంటే, ఒక శాంపిల్ ఫలితాలు చూడటానికి పైన ఆ శాంపిల్ నొక్కండి.
6. **Consignments Details**, **Amount Details**, **History / Timeline** లో కొరియర్, ఛార్జీలు, ఇప్పటివరకు జరిగిన ప్రతి దశ కనిపిస్తాయి.
7. ప్రింట్ లేదా డౌన్‌లోడ్ చేయడానికి **Export** నొక్కండి.

## మీకు కనిపించే బటన్లు

- **Edit**: సేకరణ ఇంకా డ్రాఫ్ట్‌గా ఉంది.
- **Record payment**: చెల్లింపు ఇంకా పెండింగ్‌లో ఉంది.
- **Upload consignment**: ల్యాబ్‌కి పంపడానికి సిద్ధంగా ఉంది.

## సాధారణ సమస్యలు

- "The lab has not published the results yet" (ల్యాబ్ ఇంకా ఫలితాలు ఇవ్వలేదు): శాంపిల్ ఇంకా దారిలో ఉంది, లేదా పరీక్ష జరుగుతోంది.
- కన్సైన్‌మెంట్ కింద **Not dispatched yet**: శాంపిల్స్ ఇంకా పంపలేదు.
- "Collection not found" (సేకరణ దొరకలేదు): అది తీసేయబడింది, లేదా మీకు యాక్సెస్ లేదు.
:::

::: section admin-review
title: చెల్లింపులు ఆమోదించడం, కన్సైన్‌మెంట్‌లు ముందుకు మార్చడం, ఫలితాలు ఎంటర్ చేయడం
routes: /SampleCollection/view/{id}, /ConsignmentHistory/view/{id}
for: 
roles: Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: 16838640fb4dadb0
confidence: ok

**ఈ స్క్రీన్ దేనికి**
అడ్మినిస్ట్రేటర్లు సేకరణ వివరాల పేజీలో చెల్లింపును రివ్యూ చేయవచ్చు, కన్సైన్‌మెంట్‌ను తర్వాతి దశకు మార్చవచ్చు, ల్యాబ్ ఫలితాలు టైప్ చేయవచ్చు.

### సేకరణలో చెల్లింపును ఆమోదించడం

1. **Pending Approval** స్టేటస్ ఉన్న సేకరణ తెరవండి.
2. **Payment Approval** లో **Amount**, **Transaction ID**, రసీదు పోల్చి చూడండి. రసీదు పెద్దగా చూడటానికి **View proof** నొక్కండి.
3. **Approve payment** నొక్కి నిర్ధారించండి. సేకరణ పంపడానికి సిద్ధమవుతుంది.
4. తిరస్కరించడానికి, **Reject** నొక్కి, **Reason for rejection** రాసి, తర్వాత **Reject payment** నొక్కండి.

### కన్సైన్‌మెంట్‌ను ముందుకు మార్చడం

1. **Consignment History** తెరిచి ఒక కన్సైన్‌మెంట్ తెరవండి.
2. **Update status** కింద, **In Transit**, **Delivered** లేదా **Completed** లాంటి తర్వాతి దశ నొక్కండి.

> కన్సైన్‌మెంట్‌ను ముందుకు మారిస్తే, దానిలోని ప్రతి శాంపిల్ కూడా ముందుకు మారుతుంది.

### ల్యాబ్ ఫలితాలు ఎంటర్ చేయడం

1. సేకరణ వివరాల్లో **Result Comparison** కి వెళ్లి, శాంపిల్ ఎంచుకుని **Enter results** (లేదా **Edit results**) నొక్కండి.
2. **Test Parameter**, **Entered Value**, **Unit**, **Normal Range**, **Result** నింపండి. ఇంకా వరుసలు కావాలంటే **Add parameter** నొక్కండి.
3. **Save results** నొక్కండి.

## సాధారణ సమస్యలు

- "Add a reason so the collector knows what to correct" (సేకరించినవారికి ఏమి సరిచేయాలో తెలిసేలా కారణం రాయండి): తిరస్కరణకు కారణం తప్పనిసరి.
- **Update status** కనిపించడం లేదు: కన్సైన్‌మెంట్ ఇప్పటికే **Completed** అయ్యింది.
:::

::: section consignment-history
title: ల్యాబ్‌కి పంపిన కన్సైన్‌మెంట్‌లను ట్రాక్ చేయడం
routes: /ConsignmentHistory, /ConsignmentHistory/view/{id}
for: 
roles: 
designations: 
screenshot: consignment-history.png
screenshot-phone: consignment-history.phone.jpg
minutes: 2
source-hash: c4455c0c6e2c1ab2
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Consignment History** ల్యాబ్‌కి పంపిన ప్రతి శాంపిల్ పార్సిల్‌ను, కొరియర్, ట్రాకింగ్ నంబర్, ప్రస్తుత స్టేటస్‌తో చూపిస్తుంది.

1. మెనూ నుంచి **Consignment History** తెరవండి.
2. కార్డులు **Total Consignments**, **Total Consignment Sent**, **Total Samples**, **Delivered**, **Pending** చూపిస్తాయి.
3. ఒకదాన్ని కనుక్కోవడానికి **Search consignment, AWB, transaction...** లో టైప్ చేయండి.
4. **Filters** తెరిచి **From**, **To**, **Status**, **Courier Partner** లేదా **Payment Type** ఎంచుకుని, **Apply Filters** నొక్కండి.
5. కొరియర్ ట్రాకింగ్ పేజీ తెరవడానికి ట్రాకింగ్ నంబర్ నొక్కండి.
6. **Consignment Details** తెరవడానికి కన్సైన్‌మెంట్ ID లేదా కన్ను ఐకాన్ నొక్కండి.
7. వివరాల పేజీలో టైమ్‌లైన్, లోపల ఉన్న శాంపిల్స్, అమౌంట్ చూడండి. లేబుల్, ప్యాకేజీ ఫోటోలు చూడటానికి **Click and View Photos and Files** నొక్కండి, వాటిని సేవ్ చేయడానికి **Download All** నొక్కండి.
8. డౌన్‌లోడ్ చేయడానికి **Excel** లేదా **PDF** వాడండి, అప్‌డేట్ చేయడానికి **Refresh Data** వాడండి.

## ఫీల్డ్‌ల వివరాలు

- **Current Status**: **Pending Pickup**, **Dispatched**, **In Transit**, **Delivered** లేదా **Completed**.
- **AWB/Tracking No.**: కొరియర్ ట్రాకింగ్ నంబర్.
- **Total samples**: ఫోన్‌లో, లిస్ట్ చూడటానికి శాంపిల్స్ సంఖ్యపై నొక్కండి.

## సాధారణ సమస్యలు

- **No consignments found** (కన్సైన్‌మెంట్‌లు ఏవీ దొరకలేదు): వేరే తేదీల పరిధి, స్టేటస్ లేదా కొరియర్ ప్రయత్నించండి, లేదా **Clear filters** నొక్కండి.
- **No photos yet** (ఇంకా ఫోటోలు లేవు): కన్సైన్‌మెంట్ పంపినప్పుడు ఏమీ జోడించలేదు.
- JMDO వాళ్లకు ఈ పేజీ మెనూలో కనిపించదు.
:::

::: section farmer-my-samples
title: మీ మట్టి శాంపిల్స్ చూడటం (రైతు)
routes: /Sas/MySamples, /SampleCollection/view/{id}
for: 
roles: Farmer
designations: 
screenshot: farmer-my-samples.png
screenshot-phone: farmer-my-samples.phone.jpg
minutes: 2
source-hash: eac7ad7b8677316c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ MO ఆఫీసర్ మీ దగ్గర సేకరించిన ప్రతి మట్టి లేదా నీటి శాంపిల్‌ను, ఒక్కొక్కటి ఇప్పుడు ఎక్కడ ఉందో **My Samples** చూపిస్తుంది.

1. మెనూ నుంచి **My Samples** తెరవండి.
2. ప్రతి కార్డులో శాంపిల్ నంబర్, కన్సైన్‌మెంట్, సేకరించిన ఆఫీసర్, తేదీ, పంట కనిపిస్తాయి.
3. రంగు లేబుల్స్ **Paid** లేదా **Free**, శాంపిల్ స్టేటస్, **Report: Ready** లేదా **Report: Not Ready** చూపిస్తాయి.
4. ఒక శాంపిల్ కనుక్కోవడానికి, **Search sample, consignment, crop** లో టైప్ చేయండి, తేదీలు ఎంచుకోండి, లేదా రకం, స్టేటస్ లేదా పంట ఎంచుకోండి.
5. శాంపిల్ తెరవడానికి **View Details** నొక్కండి.
6. వివరాల పేజీలో **Sample Details**, **Farmer Details**, **Payment Details**, ఇంకా **Sample Collected by MO** నుంచి **Report Ready for Download** వరకు ఉన్న **Sample Journey** దశలు చూడండి.
7. రిపోర్ట్ సిద్ధమైనప్పుడు, **Download PDF** నొక్కండి. తమిళం లేదా ఇంగ్లీష్ ఎంచుకోవడానికి దాని పక్కన ఉన్న చిన్న బాణం నొక్కండి.

## స్టేటస్ అర్థం

- **Payment Pending**: చెల్లింపు ఇంకా పూర్తి కాలేదు.
- **Awaiting Dispatch**: ల్యాబ్‌కి పంపడానికి ఎదురుచూస్తోంది.
- **In Transit**: ల్యాబ్‌కి వెళ్తున్న దారిలో ఉంది.
- **Under Analysis**: ల్యాబ్ దాన్ని పరీక్షిస్తోంది.
- **Report Ready**: మీరు రిపోర్ట్ డౌన్‌లోడ్ చేసుకోవచ్చు.

## సాధారణ సమస్యలు

- **No samples yet** (ఇంకా శాంపిల్స్ లేవు): మీ ఆఫీసర్ మీ పేరు మీద ఇంకా శాంపిల్ జోడించలేదు. మీ MO ఆఫీసర్‌ని అడగండి.
- **Download PDF** కనిపించడం లేదు: ల్యాబ్ ఇంకా రిపోర్ట్ పూర్తి చేయలేదు.
:::

::: section farmer-my-reports
title: మీ మట్టి పరీక్ష రిపోర్ట్ డౌన్‌లోడ్ చేయడం (రైతు)
routes: /Sas/MyReports
for: 
roles: Farmer
designations: 
screenshot: farmer-my-reports.png
screenshot-phone: farmer-my-reports.phone.jpg
minutes: 2
source-hash: 40b826ab2246cad3
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ శాంపిల్స్ కోసం ల్యాబ్ తయారు చేసిన పరీక్ష రిపోర్ట్‌లను **My Reports** చూపిస్తుంది. ప్రతి రిపోర్ట్‌ను తమిళం లేదా ఇంగ్లీష్‌లో డౌన్‌లోడ్ చేసుకోవచ్చు.

1. మెనూ నుంచి **My Reports** తెరవండి.
2. కార్డులు **Total Reports**, **Ready to Download**, **Downloaded**, **Under Process** చూపిస్తాయి.
3. **Reports List** లో మీ రిపోర్ట్ కనుక్కోండి. లిస్ట్ తగ్గించడానికి **Search report, sample, crop**, తేదీలు, **All Statuses** లేదా **All Crops** వాడండి.
4. రిపోర్ట్ సేవ్ చేయడానికి **Download PDF** నొక్కండి. తమిళం లేదా ఇంగ్లీష్ ఎంచుకోవడానికి దాని పక్కన ఉన్న చిన్న బాణం నొక్కండి.
5. **Report Details** చదవడానికి కన్ను ఐకాన్ (ఫోన్‌లో అయితే **View**) నొక్కండి: పరీక్ష ఫలితాలు, సిఫార్సులు, పంట సలహా.

## ఫీల్డ్‌ల వివరాలు

- **Reports No.**: రిపోర్ట్ నంబర్.
- **Sample No.**: ఈ రిపోర్ట్ ఏ శాంపిల్‌దో ఆ నంబర్.
- **Status**: **Report Ready** (ఇంకా డౌన్‌లోడ్ చేయలేదు), **Downloaded** లేదా **Printed**.
- **Under Process**: ఇంకా ల్యాబ్‌లో పరీక్ష జరుగుతున్న శాంపిల్స్.

## సాధారణ సమస్యలు

- **No reports yet** (ఇంకా రిపోర్ట్‌లు లేవు): ల్యాబ్ ఇంకా పూర్తి చేయలేదు. ఎన్ని శాంపిల్స్ పరీక్షలో ఉన్నాయో మెసేజ్ చెబుతుంది.
- **Reports are not available right now** (ఇప్పుడు రిపోర్ట్‌లు అందుబాటులో లేవు): మీ ఇంటర్నెట్ చెక్ చేసి **Try again** నొక్కండి.
- ఫైల్ తెరుచుకోలేదు: మీ ఫోన్‌లోని డౌన్‌లోడ్స్ ఫోల్డర్ చెక్ చేయండి.
:::

::: section farmer-payments
title: మీ శాంపిల్ చెల్లింపులు చెక్ చేయడం (రైతు)
routes: /Sas/Payments, /Sas/Payments/{id}
for: 
roles: Farmer
designations: 
minutes: 2
source-hash: f07974a161df2664
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ పెయిడ్ మట్టి శాంపిల్స్ కోసం చేసిన చెల్లింపులను, వాటిని SPIC ఆమోదించిందా, అందుకుందా అనేది **Payments** చూపిస్తుంది. ఇక్కడ మీరు చూడటం మాత్రమే; ఏమీ నింపాల్సిన అవసరం లేదు.

1. మెనూ నుంచి **Payments** తెరవండి.
2. కార్డులు **Total Paid Samples**, **Finance Verified**, **Approval Pending**, **Payment Issues** చూపిస్తాయి.
3. **Search sample no., transaction ID...** తో వెతకండి, లేదా తేదీలు, స్టేటస్ లేదా పంట ఎంచుకోండి.
4. **Payment Details** తెరవడానికి శాంపిల్ నంబర్, కన్ను ఐకాన్ లేదా **View Details** నొక్కండి.
5. అమౌంట్, **Payment Mode**, **Transaction ID**, **Payment Proof** కింద ఉన్న రసీదు, **Verification Journey** చూడండి.

## స్టేటస్ అర్థం

- **Admin Approval Pending**: మీ చెల్లింపు ఆమోదం కోసం ఎదురుచూస్తోంది.
- **Admin Approved**: ఆమోదించారు, Finance కోసం ఎదురుచూస్తోంది.
- **Finance Verified**: చెల్లింపు పూర్తిగా నిర్ధారించబడింది.
- **Payment Issue**: చెల్లింపు తిరిగి పంపబడింది, లేదా సరిపోలలేదు. మీ MO ఆఫీసర్ దాన్ని సరిచేస్తారు.

## సాధారణ సమస్యలు

- **No payments found** (చెల్లింపులు ఏవీ దొరకలేదు): మీకు పెయిడ్ శాంపిల్స్ లేవు, లేదా మీ శాంపిల్స్ ఫ్రీ.
- **Payment Issue** చాలా రోజులు అలాగే ఉంటే: **Transaction ID** తో మీ MO ఆఫీసర్‌ని సంప్రదించండి.
:::

::: section approve-payments
title: శాంపిల్ చెల్లింపులు ఆమోదించి Finance కి పంపడం
routes: /Sas/Payments, /Sas/Payments/{id}
for: SasPaymentApproval.View
roles: 
designations: 
minutes: 3
source-hash: 2c0a5e7b4b9a51d2
confidence: ok

**ఈ స్క్రీన్ దేనికి**
పెయిడ్ శాంపిల్స్ కోసం ఫీల్డ్ ఆఫీసర్లు నమోదు చేసిన చెల్లింపులను **Payment Approval** చూపిస్తుంది. మీరు ప్రతి రసీదు, అమౌంట్ చెక్ చేసి, ఆమోదించి Finance కి పంపుతారు, లేదా ఆఫీసర్‌కి తిరిగి పంపుతారు.

1. మెనూ నుంచి **Payment Approval** తెరవండి.
2. **Pending Admin Approval** ట్యాబ్ నొక్కండి.
3. **Payment Approval Details** తెరవడానికి **Payment Ref No.** లేదా కన్ను ఐకాన్ నొక్కండి.
4. **Expected Amount**, **Paid Amount**, **Difference** పోల్చి చూడండి, **Payment Proof Preview** కింద రసీదు చెక్ చేయండి.
5. **Approve & Forward to Finance** నొక్కండి.
6. **Verified Amount** (లేదా **Other amount**) ఎంచుకుని, **Remarks** రాసి, **I confirm that the payment proof, transaction details and amount are verified.** (చెల్లింపు రుజువు, ట్రాన్సాక్షన్ వివరాలు, అమౌంట్ చెక్ చేశానని నిర్ధారిస్తున్నాను) టిక్ చేసి, తర్వాత **Approve & Forward** నొక్కండి.
7. తిరస్కరించడానికి, **Reject Payment** నొక్కి, **Reason for rejection** రాసి, మళ్లీ **Reject Payment** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Admin Status**: **Pending Review**, **Admin Approved** లేదా **Returned to MO**.
- **Finance Status**: **Not Forwarded**, **Pending**, **Verified** లేదా **Mismatch**.
- **Difference**: చెల్లించిన అమౌంట్ రావాల్సిన అమౌంట్‌కి సమానం కాకపోతే ఎరుపు రంగులో ఉంటుంది.

## సాధారణ సమస్యలు

- "Tick the confirmation to continue" (ముందుకు వెళ్లడానికి నిర్ధారణ బాక్స్ టిక్ చేయండి): ఆమోదించే ముందు బాక్స్ టిక్ చేయండి.
- "Enter a verified amount greater than zero" (సున్నా కంటే ఎక్కువ verified amount ఎంటర్ చేయండి): అమౌంట్ ఎంచుకోండి లేదా టైప్ చేయండి.
- యాక్షన్ బటన్లు లేవు: ఈ చెల్లింపుపై ఇప్పటికే నిర్ణయం తీసుకున్నారు.
- పాత చెల్లింపులు కనుక్కోవడానికి **Filters** (తేదీలు, **State**, **Region**, **Payment Mode**, **Status**) వాడండి.
:::

::: section verify-payments
title: అందిన చెల్లింపులను నిర్ధారించడం (Finance)
routes: /Sas/Payments, /Sas/Payments/{id}, /Sas/Payments/History
for: SasPaymentVerification.View
roles: 
designations: 
minutes: 3
source-hash: 788890513e2baf14
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఆమోదించేవారు Finance కి పంపిన చెల్లింపులను **Payment Verification** చూపిస్తుంది. డబ్బు అకౌంట్‌లోకి వచ్చిందని మీరు నిర్ధారిస్తారు. మీరు పూర్తి చేసిన ప్రతి చెల్లింపు **Payment History** లో ఉంటుంది.

1. మెనూ నుంచి **Payment Verification** తెరవండి.
2. **Pending Verification** ట్యాబ్ నొక్కండి.
3. **Payment Ref No.** లేదా కన్ను ఐకాన్‌తో ఒక చెల్లింపు తెరవండి. **Payment Proof Details** తెరుచుకుంటుంది.
4. **Payment Proof Preview** కింద ఉన్న రసీదును మీ బ్యాంక్ స్టేట్‌మెంట్‌తో పోల్చి చెక్ చేయండి.
5. డబ్బు వచ్చి ఉంటే, **Verify Payment** నొక్కండి. **Verified Amount** ఎంచుకుని, **Received Date** సెట్ చేసి, **Remarks** రాసి, **I have verified the payment proof, transaction ID, and amount.** (చెల్లింపు రుజువు, ట్రాన్సాక్షన్ ID, అమౌంట్ చెక్ చేశాను) టిక్ చేసి **Confirm** నొక్కండి.
6. చెల్లింపు కనిపించకపోతే, **Mark Mismatch** నొక్కి, **Mismatch remarks** రాసి, మళ్లీ **Mark Mismatch** నొక్కండి.
7. పూర్తయిన చెల్లింపులు చూడటానికి **Payment History** తెరవండి. **All**, **Verified**, **Failed**, **Mismatch** ట్యాబ్‌లు వాడండి.

## ఫీల్డ్‌ల వివరాలు

- **Verified Amount**: నిజంగా అందిన అమౌంట్. రావాల్సిన దానికంటే తక్కువ అయితే mismatch (సరిపోలలేదు) గా సేవ్ అవుతుంది.
- **Payment Verified**: డబ్బు అందినట్టు నిర్ధారించారు.
- **Amount Mismatch**: రావాల్సిన దానికంటే తక్కువ డబ్బు వచ్చింది.
- **Failed**: **Mark Mismatch** తో గుర్తించారు; డబ్బు ఏమీ కనిపించలేదు.

## సాధారణ సమస్యలు

- "Enter the amount received" (అందిన అమౌంట్ ఎంటర్ చేయండి): అమౌంట్ ఎంచుకోండి లేదా టైప్ చేయండి.
- "Tick the confirmation to continue" (ముందుకు వెళ్లడానికి నిర్ధారణ బాక్స్ టిక్ చేయండి): నిర్ధారించే ముందు బాక్స్ టిక్ చేయండి.
- యాక్షన్ బటన్లు లేవు: చెల్లింపు ఇప్పటికే నిర్ధారించారు, లేదా ఇప్పటికే గుర్తించారు.
:::
