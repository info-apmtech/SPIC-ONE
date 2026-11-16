---
module: logistics
title: లాజిస్టిక్స్
icon: bi-truck
order: 113
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
source-hash: 616b9e8fbedcbace
confidence: ok

SPIC, GREEN STAR వాడే గోడౌన్‌లు, రేక్ పాయింట్‌లు, పోర్టులు అన్నీ ఒకే లిస్ట్‌లో ఉంచండి. ఫీల్డ్ సిబ్బంది ప్రతి చోటును దాని లొకేషన్, డాక్యుమెంట్లతో జోడిస్తారు. తర్వాత RM, SMM, AVP ఒకరి తర్వాత ఒకరు దాన్ని ఆమోదిస్తారు.

- add | icon: bi-building-add | color: #475569
  title: ఫీల్డ్ సిబ్బంది జోడిస్తారు
  text: MO, MDO లేదా JMDO ఒక గోడౌన్ లేదా రేక్ పాయింట్‌ను దాని SAP Code, అడ్రస్, మ్యాప్ పాయింట్, డాక్యుమెంట్లతో జోడిస్తారు.
- approve | icon: bi-shield-check | color: #d97706
  title: మూడు ఆమోదాలు
  text: ముందు RM, తర్వాత SMM, తర్వాత AVP రికార్డును చెక్ చేస్తారు. ప్రతి ఒక్కరూ ఆమోదించవచ్చు, లేదా రిమార్క్‌లతో తిరిగి పంపవచ్చు.
- report | icon: bi-bar-chart-line | color: #2563eb
  title: మొత్తం లెక్కలు చూడండి
  text: ప్రతి రాష్ట్రంలో ఎన్ని రికార్డులు పెండింగ్‌లో ఉన్నాయి, ఎన్ని పూర్తయ్యాయి అని రిపోర్ట్ చూపిస్తుంది.
:::

::: section flow
title: రికార్డు ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: caf925a727761bcf
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- admin | type: lane | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  sub: SAP Code లిస్ట్
- maker | type: lane | color: #475569 | roles: MO, MDO, JMDO
  title: MO / MDO / JMDO
  sub: ఫీల్డ్ సిబ్బంది
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: మీ రీజియన్
- smm | type: lane | color: #c026d3 | roles: SMM, SMD
  title: SMM
  sub: మీ రాష్ట్రం
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin
  title: AVP
  sub: చివరి ఆమోదం
- sap | lane: admin | col: 1 | icon: bi-upc-scan | color: #64748b | next: add | screen: sap-master
  title: SAP Code లు లోడ్ చేయడం
  text: Admin, PVT Master మరియు Rake Point Master Excel ఫైల్‌లు అప్‌లోడ్ చేసి, ప్రతి SAP Code ను ఒక రాష్ట్రానికి కలుపుతారు.
- add | lane: maker | col: 2 | icon: bi-building-add | color: #475569 | next: rm-check | screen: add-record
  title: రికార్డు జోడించడం
  text: SAP Code ఎంచుకుని, వివరాలు, లొకేషన్, డాక్యుమెంట్లు నింపి **Save** నొక్కండి. రికార్డు RM కి వెళ్తుంది.
- rm-check | lane: rm | col: 3 | icon: bi-hourglass-split | color: #d97706 | next: smm-check | return: add | screen: approve-records
  title: RM చెక్ చేస్తారు
  status: Pending RM
  text: RM **Approve to SMM** నొక్కుతారు, లేదా MO, MDO, JMDO కి తిరిగి పంపడానికి **Send Back** నొక్కుతారు.
- smm-check | lane: smm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: final | return: rm-check | screen: approve-records
  title: SMM చెక్ చేస్తారు
  status: Pending SMM
  text: SMM **Approve to AVP** నొక్కుతారు, లేదా RM కి తిరిగి పంపడానికి **Send Back** నొక్కుతారు.
- final | lane: avp | col: 5 | icon: bi-patch-check | color: #16a34a | return: smm-check | screen: approve-records
  title: చివరి ఆమోదం
  status: Approved
  text: AVP **Final Approve** నొక్కుతారు, లేదా SMM కి తిరిగి పంపడానికి **Send Back** నొక్కుతారు. ఆమోదం వచ్చాక రికార్డు పూర్తవుతుంది.
- sentback | lane: maker | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: add | screen: manage-records
  title: సరిచేసి మళ్లీ సేవ్ చేయడం
  status: Sent Back to MO
  text: **Remarks** చదివి, పెన్సిల్ నొక్కి, రికార్డును సరిచేసి **Update** నొక్కండి. అది మళ్లీ RM కి వెళ్తుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c35bc2efe348d9c7
confidence: ok

ఒక గోడౌన్ లేదా రేక్ పాయింట్ ఇప్పుడు ఏ దశలో ఉందో **Approval Status** కాలమ్ చూపిస్తుంది. **Status** కాలమ్ **Active** లేదా **Inactive** అని చూపిస్తుంది.

- draft | color: #64748b
  label: Draft
  text: MO, MDO లేదా JMDO కాని వారు సేవ్ చేసింది. ఇది ఆమోదం వరుసలో లేదు.
- pending-rm | color: #d97706
  label: Pending RM
  text: రీజియన్ RM కోసం ఎదురుచూస్తోంది.
- pending-smm | color: #d97706
  label: Pending SMM
  text: RM ఆమోదించారు. రాష్ట్రం SMM కోసం ఎదురుచూస్తోంది.
- pending-avp | color: #d97706
  label: Pending AVP
  text: SMM ఆమోదించారు. చివరి ఆమోదం కోసం ఎదురుచూస్తోంది.
- approved | color: #16a34a
  label: Approved
  text: మూడు ఆమోదాలూ పూర్తయ్యాయి.
- back-mo | color: #dc2626
  label: Sent Back to MO
  text: RM తిరిగి పంపారు. దాన్ని తయారు చేసిన MO, MDO లేదా JMDO సరిచేయాలి.
- back-rm | color: #dc2626
  label: Sent Back to RM
  text: SMM దాన్ని RM కి తిరిగి పంపారు.
- back-smm | color: #dc2626
  label: Sent Back to SMM
  text: AVP దాన్ని SMM కి తిరిగి పంపారు.
- active | color: #0d9488
  label: Active
  text: ఈ చోటు వాడుకలో ఉంది. లొకేషన్ వివరాలు, లిస్ట్‌లో ఉన్న డాక్యుమెంట్లు తప్పనిసరి.
- inactive | color: #475569
  label: Inactive
  text: ఈ చోటు ఇప్పుడు వాడుకలో లేదు. తక్కువ వివరాలు సరిపోతాయి.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 8acdb7d7714a0307
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- maker | color: #475569 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - గోడౌన్‌లు, రేక్ పాయింట్‌లు జోడించడం
  - GST, ఇన్సూరెన్స్, లైసెన్స్ డాక్యుమెంట్లు అప్‌లోడ్ చేయడం
  - తిరిగి వచ్చిన రికార్డులు సరిచేయడం
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - మీ రీజియన్ రికార్డులు చెక్ చేయడం
  - SMM కి ఆమోదించడం, లేదా తయారు చేసినవారికి తిరిగి పంపడం
- smm | color: #c026d3 | roles: SMM, SMD
  title: SMM
  - మీ రాష్ట్రం రికార్డులు చెక్ చేయడం
  - AVP కి ఆమోదించడం, లేదా RM కి తిరిగి పంపడం
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP, Director
  - చివరి ఆమోదం ఇవ్వడం, లేదా SMM కి తిరిగి పంపడం
  - పోర్టులు జోడించడం, మార్చడం
- admin | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  - SAP Code లిస్ట్‌లు అప్‌లోడ్ చేసి రాష్ట్రాలకు కలపడం
  - చివరి ఆమోదం ఇవ్వడం
  - లాజిస్టిక్స్ రిపోర్ట్ చూడటం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7662caacafa8b468
confidence: ok

- no-sap-code | popular: true
  q: నా SAP Code లిస్ట్‌లో లేదు
  a: మీరు ఎంచుకున్న **State** కి కలిపిన కోడ్‌లు మాత్రమే లిస్ట్‌లో కనిపిస్తాయి. ఆ కోడ్‌ను అప్‌లోడ్ చేయమని, లేదా **PVT Master Management** స్క్రీన్‌లో మీ రాష్ట్రానికి కలపమని Admin ని అడగండి.
- sent-back | popular: true
  q: నా గోడౌన్ "Sent Back to MO" అని చూపిస్తోంది. నేను ఏమి చేయాలి?
  a: **Remarks** కాలమ్ చదవండి. ఆ వరుసలో పెన్సిల్ నొక్కి, రికార్డును సరిచేసి **Update** నొక్కండి. అది **Pending RM** గా మళ్లీ RM కి వెళ్తుంది.
- no-approve
  q: నేను RM ని, కానీ నాకు Approve బటన్ కనిపించడం లేదు
  a: మీ దశలో ఎదురుచూస్తున్న రికార్డులకు మాత్రమే బటన్ కనిపిస్తుంది. ఆమోదం ఫిల్టర్‌లో మీ దశను (ఉదాహరణకు **Pending RM**) ఎంచుకోండి. RM లకు వారి సొంత రీజియన్ రికార్డులు మాత్రమే కనిపిస్తాయి.
- save-error
  q: Save నొక్కితే ఫీల్డ్‌ల కింద ఎర్రటి మెసేజ్‌లు వస్తున్నాయి
  a: **Active** రికార్డుకు పూర్తి లొకేషన్, కాంటాక్ట్ నంబర్, అవసరమైన డాక్యుమెంట్లు తప్పనిసరి. ఎరుపు రంగులో గుర్తు ఉన్న ప్రతి ఫీల్డ్ నింపండి. ఆ చోటు వాడుకలో లేకపోతే **Status** ను **Inactive** గా పెట్టండి.
- duplicate
  q: SAP Code ఇప్పటికే ఉంది అని చెబుతోంది
  a: వేరే గోడౌన్ లేదా రేక్ పాయింట్ ఇప్పటికే ఆ కోడ్ వాడుతోంది. లిస్ట్‌లో దాని కోసం వెతకండి. ఒకే చోటును రెండుసార్లు జోడించకండి.
:::

::: section add-record
title: గోడౌన్ లేదా రేక్ పాయింట్ జోడించడం
routes: /Logistics
for: 
roles: 
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-basic] | 3 > [data-help=lg-location] | 4 > [data-help=lg-docs] | 5 > [data-help=lg-save]
minutes: 5
source-hash: c9ec25daaeef4964
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Logistics Master** స్క్రీన్‌లో పైన ఒక ఫారం, కింద సేవ్ చేసిన రికార్డుల లిస్ట్ ఉంటాయి. గోడౌన్ లేదా రేక్ పాయింట్ జోడించడానికి ఈ ఫారం వాడండి.

1. ట్యాబ్ ఎంచుకోండి: **Warehouse** లేదా **Rake Point**.
2. **Basic Information** లో, **SPIC**, **GREEN STAR** లేదా రెండూ నొక్కండి. **State** ఎంచుకుని, తర్వాత **SAP Code** ఎంచుకోండి; **Name** దానంతట అదే నిండుతుంది. **Operated By** ఎంచుకోండి. గోడౌన్ అయితే, **Warehouse Category** (**PVT WH**, **C&F WH** లేదా **Both**), **WH Type** ఎంచుకుని, **Reservation Quantity** బాక్స్ నింపండి.
3. **Primary Location** లో, **Use Current Location** నొక్కండి, లేదా మ్యాప్‌పై నొక్కండి, లేదా **Google Map URL** పేస్ట్ చేయండి. తర్వాత అడ్రస్, **Pincode**, కాంటాక్ట్ నంబర్ నింపండి.
4. **Documents** లో, ప్రతి **Choose ... File** బటన్ నొక్కి ఫైల్ ఎంచుకోండి. కన్ను ఐకాన్‌తో దాన్ని చెక్ చేయండి. **Active** రికార్డుకు * గుర్తు ఉన్న ఫీల్డ్‌లు తప్పనిసరి.
5. **Save** నొక్కండి. MO, MDO లేదా JMDO సేవ్ చేస్తే, రికార్డు **Pending RM** గా RM కి వెళ్తుంది.

> Tip: డాక్యుమెంట్‌ను స్కాన్ చేసి అప్‌లోడ్ చేసే ముందు, అందులో గోడౌన్ అడ్రస్‌ను హైలైటర్‌తో గుర్తు పెట్టండి.
> Careful: ఫారం ఖాళీ చేయాలనుకుంటేనే **Cancel** నొక్కండి. ఏదీ సేవ్ అవ్వదు.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Status** | చోటు ఇప్పుడు వాడుకలో ఉంటే **Active**, లేకపోతే **Inactive**. |
| **Operated By** | **Dealer** లేదా **Others**. |
| **WH Type** | **Field WH** లేదా **Rake Point WH** (PVT WH, Both కి మాత్రమే). |
| **Approved Reservation Quantity (MT)** | కంపెనీ కోసం కేటాయించిన స్థలం, టన్నుల్లో. |
| **Latitude & Longitude** | కామాతో రెండు నంబర్లు, ఉదాహరణకు 10.7905, 78.7047. |
| **Pincode** | 6 అంకెలు. **State**, **District** దానంతట అవే నిండవచ్చు. |

## సాధారణ సమస్యలు

- **SAP Code లిస్ట్ ఖాళీగా ఉంది.** ఎంచుకున్న **State** కి ఇంకా ఏ కోడ్‌లూ కలపలేదు. Admin ని అడగండి.
- **"Primary Location is required".** (Primary Location తప్పనిసరి) **Active** రికార్డుకు మ్యాప్ పాయింట్ లేదా Google Map URL, పూర్తి అడ్రస్ కావాలి.
- **"... saved, but one or more documents failed to upload".** (సేవ్ అయ్యింది, కానీ కొన్ని డాక్యుమెంట్లు అప్‌లోడ్ కాలేదు) రికార్డు సేవ్ అయ్యింది. దానిపై పెన్సిల్ నొక్కి డాక్యుమెంట్లు మళ్లీ అప్‌లోడ్ చేయండి.
- **State, Region లేదా HQ బూడిద రంగులో ఉంది.** ఇవి మీ లాగిన్ నుంచి వస్తాయి, మార్చలేరు.
:::

::: section approve-records
title: రికార్డును ఆమోదించడం లేదా తిరిగి పంపడం
routes: /Logistics
for: 
roles: RM, RMD, SMM, SMD, AVP, Director, Admin, CorporateAdmin
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-approval-filter] | 3 > [data-help=lg-search] | 4 > [data-help=lg-list]
minutes: 2
source-hash: 5b300e3e278e2af7
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఫారం కింద ఉన్న లిస్ట్ మీ ప్రాంతం గోడౌన్‌లు, రేక్ పాయింట్‌లు చూపిస్తుంది. మీ కోసం ఎదురుచూస్తున్న రికార్డులకు **Action** కాలమ్‌లో ఆమోదం బటన్, **Send Back** బటన్ ఉంటాయి.

1. **Warehouse** లేదా **Rake Point** ట్యాబ్ ఎంచుకోండి.
2. ఆమోదం ఫిల్టర్‌లో మీ దశ ఎంచుకోండి: **Pending RM**, **Pending SMM** లేదా **Pending AVP**.
3. రికార్డు కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో పేరు లేదా SAP Code టైప్ చేయండి. దాన్ని చెక్ చేయడానికి **Docs** కాలమ్‌లో **View**, PDF బటన్ వాడండి.
4. **Approve to SMM**, **Approve to AVP** లేదా **Final Approve** నొక్కండి (మీ దశకు సంబంధించిన బటన్ కనిపిస్తుంది). **Approval Remarks** రాసి **Confirm & Proceed** నొక్కండి. తిరిగి పంపాలంటే, **Send Back** నొక్కి, **Send Back Remarks** రాసి, **Confirm Send Back** నొక్కండి.

> Tip: ఆమోదించడానికీ, తిరిగి పంపడానికీ రిమార్క్‌లు తప్పనిసరి. అవి **Remarks** కాలమ్‌లో కనిపిస్తాయి.
> Careful: **Send Back** రికార్డును డిలీట్ చేయదు. అది ఒక దశ మాత్రమే వెనక్కి పంపుతుంది.

## సాధారణ సమస్యలు

- **ఆమోదం బటన్ లేదు.** రికార్డు మీ దశలో లేదు. దాని **Approval Status** చెక్ చేయండి.
- **"Approval remarks are required."** (ఆమోదం రిమార్క్‌లు తప్పనిసరి) నిర్ధారించే ముందు ఒక రిమార్క్ టైప్ చేయండి.
- **నాకు ఒక రికార్డు కనిపించడం లేదు.** RM లకు వారి రీజియన్, SMM లకు వారి రాష్ట్రం, AVP లకు వారి జోన్ మాత్రమే కనిపిస్తాయి.
:::

::: section manage-records
title: రికార్డులు వెతకడం, సరిచేయడం, డౌన్‌లోడ్ చేయడం
routes: /Logistics
for: 
roles: 
designations: 
minutes: 3
source-hash: 13e0dd985a45cb04
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఫారం కింద ఉన్న లిస్ట్ సేవ్ చేసిన రికార్డులను చూపిస్తుంది. పైన లెక్కలు ఉంటాయి: **Total**, **Active**, **Inactive**.

1. ట్యాబ్ ఎంచుకుని, లిస్ట్ పైన ఉన్న లెక్కలు చూడండి.
2. పేరుతో వెతకండి, లేదా లిస్ట్ తగ్గించడానికి **All Status**, ఆమోదం ఫిల్టర్‌లు వాడండి.
3. సేవ్ చేసిన డాక్యుమెంట్లు చూడటానికి, డౌన్‌లోడ్ చేయడానికి లేదా తెరవడానికి **Docs** కాలమ్‌లో **View** నొక్కండి. అయ్యాక **Close** నొక్కండి.
4. రికార్డును PDF గా తెరవడానికి **PDF** కాలమ్‌లో SPIC లేదా GREEN STAR లోగో నొక్కండి.
5. రికార్డును ఫారంలోకి తేవడానికి పెన్సిల్ నొక్కండి. మార్చి **Update** నొక్కండి.
6. **Status** కాలమ్‌లో **Active** లేదా **Inactive** నొక్కి దాన్ని మార్చండి.
7. రికార్డు డిలీట్ చేయడానికి బిన్ (చెత్తబుట్ట) నొక్కి, నిర్ధారించడానికి **Delete** నొక్కండి.
8. లిస్ట్ డౌన్‌లోడ్ చేయడానికి లిస్ట్ పైన ఉన్న **Excel** నొక్కండి (ఫోన్‌లో, **Export** తర్వాత **Excel**).

> Careful: డిలీట్ చేసిన రికార్డు మళ్లీ తిరిగి రాదు.

## సాధారణ సమస్యలు

- **"Sent Back to MO".** (MO కి తిరిగి పంపబడింది) **Remarks** చదివి, పెన్సిల్‌తో రికార్డును సరిచేసి **Update** నొక్కండి. అది మళ్లీ RM కి వెళ్తుంది.
- **Docs లోని View బటన్ బూడిద రంగులో ఉంది.** ఆ రికార్డుకు డాక్యుమెంట్లు ఏవీ సేవ్ కాలేదు.
- **లిస్ట్ ఖాళీగా ఉంది.** మీరు పెట్టిన ఫిల్టర్‌లు మరీ ఎక్కువగా ఉండొచ్చు. వాటిని మళ్లీ **All Status**, **All Approval Status** కి మార్చండి.
:::

::: section ports
title: పోర్టు జోడించడం
routes: /Logistics
for: 
roles: Admin, CorporateAdmin, AVP, Director
designations: 
minutes: 1
source-hash: 264444955d15cae7
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Port** ట్యాబ్‌లో పోర్టుల లిస్ట్ ఉంటుంది. Admin, AVP, Director కి మాత్రమే ఈ ట్యాబ్ కనిపిస్తుంది. పోర్టులకు ఆమోదం దశలు లేవు.

1. **Port** ట్యాబ్ తెరవండి.
2. **Port Name** ఎంటర్ చేయండి. మీ దగ్గర ఉంటే **Google Map or Lat/Long URL** కూడా ఎంటర్ చేయండి.
3. **State**, **District** ఎంచుకోండి.
4. **Active** లేదా **Inactive** ఎంచుకుని **Save** నొక్కండి.

## సాధారణ సమస్యలు

- **"Port name is required".** (పోర్టు పేరు తప్పనిసరి) సేవ్ చేసే ముందు పేరు, **State**, **District** ఎంటర్ చేయండి.
:::

::: section reports
title: లాజిస్టిక్స్ రిపోర్ట్ చూడటం
routes: /LogisticsReport
for: 
roles: 
designations: 
minutes: 1
source-hash: 206298ea363d8aeb
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Logistics Reports** స్క్రీన్ గోడౌన్‌లు, రేక్ పాయింట్‌లను రాష్ట్రాల వారీగా లెక్కపెట్టి చూపిస్తుంది. కింద **TOTAL** వరుస ఉంటుంది.

1. **Logistics Master** స్క్రీన్‌లో **Reports** నొక్కండి.
2. **Warehouse Report** లేదా **Rakepoint Report** ఎంచుకోండి.
3. ఒక రాష్ట్రాన్ని మాత్రమే చూడటానికి, **Search by state...** లో రాష్ట్రం పేరు టైప్ చేయండి.
4. కాలమ్‌లు చదవండి: **Active**, **Inactive**, **Total**, **As per SAP**, **Pending with MO**, **Pending RM**, **Pending SMM**, **Pending with AVP**, **Completed**.
5. కొత్త లెక్కలు తేవడానికి **Refresh** నొక్కండి. వెనక్కి వెళ్లడానికి **Back to Logistics** నొక్కండి.

> Tip: **As per SAP** అంటే SAP Code లిస్ట్‌లో ఉన్న కోడ్‌ల సంఖ్య. **Pending with MO** అంటే ఇంకా జోడించని లేదా ఇంకా ఆమోదానికి రాని కోడ్‌లు.

## సాధారణ సమస్యలు

- **"No data available".** (డేటా లేదు) మీ సెర్చ్‌కు సరిపోయేది ఏదీ లేదు. సెర్చ్ బాక్స్ ఖాళీ చేయండి, లేదా **Refresh** నొక్కండి.
:::

::: section sap-master
title: SAP Code లు అప్‌లోడ్ చేసి రాష్ట్రాలకు కలపడం
routes: /PVTMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 2bc3660a46728597
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**PVT Master Management** స్క్రీన్‌లో SAP Code లిస్ట్‌లు ఉంటాయి. లాజిస్టిక్స్ ఫారంలోని **SAP Code** బాక్స్‌కు ఇవే వస్తాయి. **Logistics Master** స్క్రీన్‌లోని **SAP Code** బటన్‌తో Admin దీన్ని తెరుస్తారు.

1. **PVT Master** (గోడౌన్‌లు) లేదా **Rake Point** ఎంచుకోండి.
2. Excel ఫార్మాట్ డౌన్‌లోడ్ చేయడానికి **Sample Format** నొక్కి, దాన్ని నింపండి.
3. **Click to upload or drag and drop** భాగం నొక్కి, మీ .xlsx ఫైల్ ఎంచుకోండి.
4. **Upload Result** చదవండి: **Total Records**, **Successfully Inserted**, **Duplicate/Skipped**, వదిలేసిన వరుసల లిస్ట్‌తో పాటు.
5. లిస్ట్‌లో, ఒక వరుసపై **State** నొక్కి, **State** ఎంచుకుని **Save State Mapping** నొక్కండి.
6. లాజిస్టిక్స్‌కు తిరిగి వెళ్లడానికి **Back** నొక్కండి.

> Careful: 10 MB లేదా అంతకంటే తక్కువ ఉన్న .xlsx ఫైల్ వాడండి. ఇప్పటికే ఉన్న కోడ్‌లు వదిలేయబడతాయి.

## సాధారణ సమస్యలు

- **"Please select a valid .xlsx Excel file."** (సరైన .xlsx Excel ఫైల్ ఎంచుకోండి) ఫైల్‌ను .xlsx గా సేవ్ చేసి మళ్లీ ప్రయత్నించండి.
- **ఒక కోడ్ "Not Mapped" అని చూపిస్తోంది.** ఆ వరుసపై **State** నొక్కి దాని రాష్ట్రం ఎంచుకోండి. అప్పటివరకు, ఫీల్డ్ సిబ్బంది రాష్ట్రం ఎంచుకున్నాక ఆ కోడ్‌ను ఎంచుకోలేరు.
- **State బటన్ లేదు.** Admin, CorporateAdmin మాత్రమే కలపడాన్ని (mapping) మార్చగలరు.
:::

::: section old-master
title: పాత Logistics Master స్క్రీన్
routes: /LogisticsMaster
for: 
roles: 
designations: 
minutes: 1
source-hash: 2bc6b07f78791b25
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఇది లాజిస్టిక్స్ ఫారం యొక్క పాత వెర్షన్. ఇందులో **PVT WH**, **C&F WH**, **Rake Point** అనే ట్యాబ్‌లు ఉంటాయి. కొత్త పని కోసం, సైడ్ మెనూలోని ప్రధాన **Logistics Master** స్క్రీన్ వాడండి.

1. ఒక ట్యాబ్ ఎంచుకోండి: **PVT WH**, **C&F WH** లేదా **Rake Point**.
2. ప్రధాన స్క్రీన్‌లో లాగానే **Basic Information**, **Reservation Quantity**, **Primary Location**, **Documents** నింపండి.
3. **Save** నొక్కండి. రికార్డును మార్చడానికి లేదా డిలీట్ చేయడానికి లిస్ట్‌లోని పెన్సిల్, బిన్ వాడండి.

## సాధారణ సమస్యలు

- **"Failed to load PVT WH" or "Failed to load C&F WH".** (PVT WH / C&F WH లోడ్ కాలేదు) ఈ స్క్రీన్‌లో ఈ ట్యాబ్‌లు పనిచేయవు. ప్రధాన **Logistics Master** స్క్రీన్ వాడండి.
- **ఆమోదం బటన్‌లు లేవు.** ఆమోదాలు ప్రధాన **Logistics Master** స్క్రీన్‌లో మాత్రమే జరుగుతాయి.
:::
