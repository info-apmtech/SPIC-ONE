---
module: sub-dealers
title: సబ్ డీలర్లు
icon: bi-people
order: 103
language: te
category: డీలర్లు
note: కొత్త గైడ్
meta-source-hash: 95f84553d50218f7
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 0365bef89cdeeedd
confidence: ok

ప్రతి సబ్ డీలర్ గురించి సరైన రికార్డు ఉంచండి: షాప్ ఎక్కడ ఉంది, వారిని ఎలా సంప్రదించాలి, వారి GST, PAN, mFMS వివరాలు. సబ్ డీలర్లు, డీలర్ల ఉద్యోగుల సంక్షేమ లబ్ధిదారులను కూడా ఇదే మాడ్యూల్ ఉంచుతుంది.

- import | icon: bi-file-earmark-excel | color: #8b5cf6
  title: Admin మాస్టర్‌ను అప్‌లోడ్ చేస్తారు
  text: ప్రతి State, Region, HQ కోసం ఆమోదించిన సబ్ డీలర్ కోడ్‌ల Excel లిస్ట్‌ను Admin ఇంపోర్ట్ చేస్తారు.
- maintain | icon: bi-pencil-square | color: #0d9488
  title: MO వివరాలు నింపుతారు
  text: MO తమ HQ లోని ప్రతి సబ్ డీలర్‌ను ఎంచుకుని, లొకేషన్, కాంటాక్ట్ నంబర్లు, GST, PAN, mFMS నింపుతారు.
- welfare | icon: bi-heart | color: #d97706
  title: సంక్షేమ లబ్ధిదారులు
  text: సబ్ డీలర్, ఉద్యోగి లబ్ధిదారులను ముందు SM, తర్వాత AVP ఆమోదిస్తారు.
:::

::: section flow
title: సబ్ డీలర్ రికార్డు ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 0b02b6ac4a67732f
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- admin | type: lane | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin, ఆఫీస్
  sub: హెడ్ ఆఫీస్
- field | type: lane | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: మీ HQ
- sm | type: lane | color: #d97706 | roles: SMD, SMM
  title: SM
  sub: SMD · SMM
- avp | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: చివరి ఆమోదం
- import | lane: admin | col: 1 | icon: bi-file-earmark-excel | color: #8b5cf6 | next: maintain | screen: import-master
  title: మాస్టర్ ఇంపోర్ట్ చేయడం
  text: ఆమోదించిన Excel లిస్ట్‌ను Admin అప్‌లోడ్ చేస్తారు. ఎవరైనా వివరాలు సేవ్ చేసే వరకు కొత్త సబ్ డీలర్లు లిస్ట్‌లో కనిపించరు.
- maintain | lane: field | col: 2 | icon: bi-pencil-square | color: #16a34a | next: beneficiary | screen: maintain-sub-dealer
  title: వివరాలు నింపడం
  status: Active
  text: MO సబ్ డీలర్‌ను ఎంచుకుని, లొకేషన్, కాంటాక్ట్ నంబర్లు, GST, PAN, mFMS నింపి **Submit** నొక్కుతారు. ఇప్పుడు అది **Sub Dealer Master** లిస్ట్‌లో కనిపిస్తుంది.
- beneficiary | lane: admin | col: 3 | icon: bi-person-plus | color: #d97706 | next: sm-approve | screen: beneficiary-add
  title: లబ్ధిదారుని చేర్చడం
  status: Pending
  text: **Sub Dealer & Employee** స్క్రీన్‌లో, ఆఫీస్ సిబ్బంది ఒక సబ్ డీలర్ లేదా ఒక డీలర్ ఉద్యోగి యొక్క సంక్షేమ లబ్ధిదారుని చేరుస్తారు.
- sm-approve | lane: sm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: avp-approve | screen: beneficiary-approve
  title: SM ఆమోదం
  status: Pending
  text: SMD లేదా SMM రికార్డును చెక్ చేసి, ఆమోదిస్తారు లేదా తిరస్కరిస్తారు.
- avp-approve | lane: avp | col: 5 | icon: bi-patch-check | color: #2563eb | screen: beneficiary-approve
  title: AVP ఆమోదం
  status: Approved
  text: AVP చివరి ఆమోదం ఇస్తారు. ఇక సంక్షేమ పథకానికి అప్లై చేసేటప్పుడు డీలర్ ఈ వ్యక్తిని ఎంచుకోవచ్చు.
- rejected | lane: sm | col: 5 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: sm-approve | screen: beneficiary-approve
  title: తిరస్కరించబడింది
  status: SM Rejected
  text: SM లేదా AVP రికార్డును తిరస్కరిస్తే, అది అక్కడే ఆగిపోతుంది. డీలర్‌కు అది చూపించబడదు.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9462538c97a0b6f7
confidence: ok

- active | color: #16a34a
  label: Active
  text: సబ్ డీలర్ లేదా లబ్ధిదారు వాడకంలో ఉన్నారు. Active సబ్ డీలర్‌కు * గుర్తు ఉన్న ప్రతి ఫీల్డ్ అవసరం.
- inactive | color: #64748b
  label: Inactive
  text: ఇప్పుడు వాడకంలో లేదు. Inactive సబ్ డీలర్‌కు State, Region, HQ, Firm Name మాత్రమే చాలు.
- pending | color: #d97706
  label: Pending
  text: లబ్ధిదారు SM లేదా AVP ఆమోదం కోసం ఎదురుచూస్తున్నారు.
- approved | color: #2563eb
  label: Approved
  text: ఈ స్థాయిలో ఆమోదం వచ్చింది. Overall కాలమ్‌లో ఉంటే, SM, AVP ఇద్దరూ ఆమోదించారని అర్థం.
- rejected | color: #dc2626
  label: Rejected
  text: ఈ స్థాయిలో తిరస్కరించబడింది. SM Status లేదా AVP Status కాలమ్‌లో కనిపిస్తుంది.
- sm-rejected | color: #dc2626
  label: SM Rejected
  text: SM లబ్ధిదారుని తిరస్కరించారు. అది AVP కి వెళ్లదు.
- avp-rejected | color: #b91c1c
  label: AVP Rejected
  text: SM ఆమోదించారు, కానీ AVP లబ్ధిదారుని తిరస్కరించారు.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e89557ba6534118
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- admin | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - సబ్ డీలర్ మాస్టర్‌ను Excel నుంచి ఇంపోర్ట్ చేయడం
  - ఏ సబ్ డీలర్ వివరాలైనా నింపడం లేదా సరిచేయడం
  - సబ్ డీలర్, ఉద్యోగి లబ్ధిదారులను చేర్చడం
- field | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - మీ HQ సబ్ డీలర్ల వివరాలు నింపడం, అప్‌డేట్ చేయడం
  - తప్పుగా సేవ్ అయిన వివరాలను డిలీట్ చేయడం
- rm | color: #64748b | roles: RM, RMD
  title: RM, RMD
  - మీ రీజియన్ సబ్ డీలర్లను చూడటం
  - లిస్ట్‌ను Excel గా డౌన్‌లోడ్ చేయడం
- sm | color: #d97706 | roles: SMD, SMM
  title: SMD, SMM
  - మీ రాష్ట్రం సబ్ డీలర్లను చూడటం
  - లబ్ధిదారులను ముందుగా ఆమోదించడం లేదా తిరస్కరించడం
- avp | color: #2563eb | roles: AVP, Director
  title: AVP, Director
  - మీ ప్రాంతం సబ్ డీలర్ల వివరాలు నింపడం లేదా అప్‌డేట్ చేయడం
  - లబ్ధిదారులకు AVP చివరి ఆమోదం ఇస్తారు
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 3f822e638d6491b2
confidence: ok

- not-in-dropdown | popular: true
  q: Select Imported Sub Dealer లో ఒక సబ్ డీలర్ కనిపించడం లేదు
  a: మీ State, Region, HQ కోసం Admin ఇంపోర్ట్ చేసిన సబ్ డీలర్లు మాత్రమే ఫారంలో కనిపిస్తారు. ఫారంలో కొత్త కోడ్ తయారు చేయలేరు. దాన్ని మాస్టర్‌లో చేర్చమని Admin ని అడగండి.
- list-empty | popular: true
  q: Admin సబ్ డీలర్లను అప్‌లోడ్ చేశారు, కానీ Sub Dealer Master లిస్ట్ ఖాళీగా ఉంది
  a: వివరాలు సేవ్ అయిన సబ్ డీలర్లు మాత్రమే లిస్ట్‌లో కనిపిస్తారు. **Maintain Sub Dealer** నొక్కి, సబ్ డీలర్‌ను ఎంచుకుని, వివరాలు నింపి **Submit** నొక్కండి.
- submit-grey
  q: Submit బటన్ బూడిద రంగులో ఉంది
  a: ముందు ఒక సబ్ డీలర్‌ను ఎంచుకుని, దాని వివరాలు లోడ్ అయ్యే వరకు ఆగండి. GST PDF, లొకేషన్ లేదా పిన్‌కోడ్ చదువుతున్నప్పుడు కూడా ఆగండి.
- edit-grey
  q: లిస్ట్‌లో పెన్సిల్, బిన్ ఐకాన్‌లు బూడిద రంగులో ఉన్నాయి
  a: మీ రోల్ సబ్ డీలర్లను చూడటానికి మాత్రమే. MO, MDO, JMDO, Admin, AVP, Director మాత్రమే వాటిని మార్చగలరు.
- no-approve
  q: నేను ఆమోదించేవాడిని, కానీ లబ్ధిదారు వరుసలో టిక్ లేదా క్రాస్ కనిపించడం లేదు
  a: **SM Status** **Pending** గా ఉన్నప్పుడు మాత్రమే SM కి అవి కనిపిస్తాయి. SM ఆమోదించాక, **AVP Status** **Pending** గా ఉన్నప్పుడు మాత్రమే AVP కి అవి కనిపిస్తాయి.
:::

::: section sub-dealer-list
title: సబ్ డీలర్ లిస్ట్ చూడటం
routes: /SubDealerList
for: 
roles: 
designations: 
tour: 1 > [data-help=sdl-cards] | 2 > [data-help=sdl-filters] | 3 > [data-help=sdl-table] | 4 > [data-help=sdl-actions] | 5 > [data-help=sdl-summary]
minutes: 2
source-hash: eb1071bb27a63554
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sub Dealer Master** స్క్రీన్, సబ్ డీలర్ ఫారం ద్వారా వివరాలు సేవ్ అయిన సబ్ డీలర్లను చూపిస్తుంది. MO తమ HQ ని, RM రీజియన్‌ను, SMM లేదా SMD రాష్ట్రాన్ని చూస్తారు. మీరు ఏ ప్రాంతాన్ని చూస్తున్నారో పైన ఒక నోట్ చెబుతుంది.

1. పైన ఉన్న కార్డులు చూడండి: **Total Sub Dealers**, **Active**, **Inactive**.
2. **Search** లో కోడ్, పేరు, మొబైల్, GST, PAN లేదా mFMS ID టైప్ చేయండి. లిస్ట్‌ను తగ్గించడానికి **State**, **Region**, **HQ** లేదా **Status** ఎంచుకోండి. మీ సొంత ప్రాంతం ముందే ఎంచుకుని లాక్ చేసి ఉంటుంది.
3. **Action** కాలమ్‌లో, కన్ను ఐకాన్ సబ్ డీలర్‌ను చూడటానికి తెరుస్తుంది. పెన్సిల్ మార్చడానికి తెరుస్తుంది. బిన్ (చెత్తబుట్ట) దాని సేవ్ అయిన వివరాలను డిలీట్ చేస్తుంది.
4. పైన ఉన్న బటన్లు వాడండి. **Refresh** లిస్ట్‌ను మళ్లీ లోడ్ చేస్తుంది. **Export All Saved** సేవ్ అయిన అన్ని సబ్ డీలర్లను Excel గా డౌన్‌లోడ్ చేస్తుంది. **Maintain Sub Dealer** ఫారం తెరుస్తుంది.
5. కిందకు **State-wise Sub Dealer Summary** వరకు స్క్రోల్ చేయండి. అది ప్రతి రాష్ట్రం మాస్టర్ రికార్డులను, సేవ్ అయిన **Active**, **Inactive** సబ్ డీలర్లతో పోల్చి చూపిస్తుంది.

> Careful: బిన్ సేవ్ అయిన వివరాలను మాత్రమే తీసేస్తుంది, సబ్ డీలర్‌ను కాదు. ఎవరైనా మళ్లీ వివరాలు నింపే వరకు అది ఈ లిస్ట్‌లో కనిపించదు.

## సాధారణ సమస్యలు

- **"No saved Sub Dealers found".** (సేవ్ అయిన సబ్ డీలర్లు లేరు) వివరాలు సేవ్ అయిన సబ్ డీలర్లు మాత్రమే ఇక్కడ కనిపిస్తారు. Admin అప్‌లోడ్ చేసి, ఎవరూ నింపని వరుసలు దాచబడి ఉంటాయి.
- **పెన్సిల్, బిన్ బూడిద రంగులో ఉన్నాయి.** మీ రోల్ చూడటానికి మాత్రమే. MO, MDO, JMDO, Admin, AVP, Director మాత్రమే సబ్ డీలర్లను మార్చగలరు.
- **Maintain Sub Dealer బటన్ లేదు.** మీ రోల్ లిస్ట్‌ను చూడటానికి మాత్రమే.
- **"No saved Sub Dealer records are available to export."** (ఎక్స్‌పోర్ట్ చేయడానికి సేవ్ అయిన సబ్ డీలర్ రికార్డులు లేవు) మీ ప్రాంతంలో ఇంకా ఏదీ సేవ్ కాలేదు.
:::

::: section maintain-sub-dealer
title: సబ్ డీలర్ వివరాలు నింపడం లేదా అప్‌డేట్ చేయడం
routes: /SubDealerRegistration
for: 
roles: 
designations: 
tour: 1 > [data-help=sdr-business] | 2 > [data-help=sdr-basic] | 3 > [data-help=sdr-location] | 4 > [data-help=sdr-tax] | 5 > [data-help=sdr-mfms] | 6 > [data-help=sdr-submit]
minutes: 5
source-hash: e410f07c765072a5
confidence: ok

**ఈ స్క్రీన్ దేనికి**
Admin ఇప్పటికే ఇంపోర్ట్ చేసిన సబ్ డీలర్ వివరాలు నింపడానికి **Sub Dealer Maintenance** స్క్రీన్ వాడండి. ఇక్కడ కొత్త సబ్ డీలర్ కోడ్ తయారు చేయలేరు. లిస్ట్‌లోని **Maintain Sub Dealer** తో గానీ, ఒక వరుసలోని పెన్సిల్‌తో గానీ దీన్ని తెరవండి.

1. **Business Information** కింద, **Select Imported Sub Dealer** నొక్కి, కోడ్ లేదా పేరు టైప్ చేసి సబ్ డీలర్‌ను ఎంచుకోండి. దాని సేవ్ అయిన వివరాలు లోడ్ అవుతాయి. **Firm Name** చెక్ చేయండి (GST లోని ట్రేడ్ నేమ్ ప్రకారం).
2. **Basic Information** కింద, **State**, **Region**, **HQ** చెక్ చేసి, **Status** ఎంచుకోండి: **Active** లేదా **Inactive**.
3. **Primary Location** కింద, **Use Current Location** నొక్కండి, లేదా మ్యాప్‌పై నొక్కండి, లేదా Google Maps లింక్ పేస్ట్ చేయండి. అడ్రస్ నింపండి. 6 అంకెల **Pincode** టైప్ చేస్తే State, District, Block, Taluk, Village వాటంతట అవే నిండుతాయి. **Official Contact Number**, **WhatsApp Number**, **Alternative Number** ఎంటర్ చేయండి.
4. **Tax & Compliance** కింద, **Upload GST Certificate (PDF)** నొక్కండి. యాప్ PDF ను చదివి **GST Number**, **Legal Name**, **Trade Name**, **Constitution of Business** నింపుతుంది. వాటిని చెక్ చేసి, తర్వాత **PAN Number** ఎంటర్ చేయండి.
5. **mFMS Information** కింద, **Retail mFMS ID** ఎంటర్ చేయండి. **Wholesale mFMS ID** తప్పనిసరి కాదు.
6. **Submit** నొక్కండి. సబ్ డీలర్ సేవ్ అయిందని ఆకుపచ్చ మెసేజ్ వస్తుంది, తర్వాతి దాని కోసం ఫారం ఖాళీ అవుతుంది.

> Tip: **Active** సబ్ డీలర్‌కు * గుర్తు ఉన్న ప్రతి ఫీల్డ్ అవసరం. **Inactive** సబ్ డీలర్‌కు State, Region, HQ, Firm Name మాత్రమే చాలు.
> Careful: కన్ను ఐకాన్‌తో సబ్ డీలర్‌ను తెరిస్తే, ప్రతి ఫీల్డ్ బూడిద రంగులో ఉంటుంది, **Submit** బటన్ ఉండదు. మార్చడానికి పెన్సిల్ వాడండి.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Select Imported Sub Dealer** | Admin అప్‌లోడ్ చేసిన దాంట్లో ఉన్న సబ్ డీలర్. తప్పనిసరి. |
| **Status** | సబ్ డీలర్ ఇప్పుడు పనిచేస్తుంటే **Active**, లేకపోతే **Inactive**. |
| **Google Map URL** మరియు **Latitude & Longitude** | షాప్ లొకేషన్. Active కి తప్పనిసరి. |
| **Pincode** | 6 అంకెలు. ఇది ఊరి పేర్లను మీ కోసం నింపుతుంది. |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 అంకెల మొబైల్ నంబర్లు. Active కి తప్పనిసరి. |
| **GST Number** | 15 అక్షరాలు, ఉదాహరణకు 22AAAAA0000A1Z5. Active కి తప్పనిసరి. |
| **Upload GST Certificate (PDF)** | ఒకే PDF ఫైల్, 5 MB వరకు. Active కి తప్పనిసరి. |
| **PAN Number** | 10 అక్షరాలు, ఉదాహరణకు ABCDE1234F. Active కి తప్పనిసరి. |
| **Retail mFMS ID** | Active కి తప్పనిసరి. |

## సాధారణ సమస్యలు

- **"Please correct the highlighted fields."** (గుర్తించిన ఫీల్డ్‌లను సరిచేయండి) ప్రతి ఫీల్డ్ కింద ఉన్న ఎరుపు అక్షరాలు చదివి సరిచేయండి.
- **Submit బటన్ బూడిద రంగులో ఉంది.** ముందు ఒక సబ్ డీలర్‌ను ఎంచుకుని, అది లోడ్ అయ్యే వరకు ఆగండి. GST PDF, లొకేషన్ లేదా పిన్‌కోడ్ చదువుతున్నప్పుడు కూడా ఆగండి.
- **"Shortened URL detected."** (చిన్న లింక్ గుర్తించబడింది) లింక్‌ను బ్రౌజర్‌లో తెరిచి, అడ్రస్ బార్ నుంచి పూర్తి అడ్రస్ కాపీ చేసి పేస్ట్ చేయండి.
- **"Choose a non-empty GST PDF of 5 MB or smaller."** (ఖాళీ కాని, 5 MB లేదా అంతకంటే చిన్న GST PDF ఎంచుకోండి) 5 MB లేదా అంతకంటే తక్కువ PDF ఫైల్ అప్‌లోడ్ చేయండి.
- **"GST could not be extracted."** (GST వివరాలు చదవలేకపోయింది) GST వివరాలు మీరే టైప్ చేసి చెక్ చేయండి.
- **"The selected Sub Dealer does not have a valid Sub Dealer Code. Contact Admin."** (ఎంచుకున్న సబ్ డీలర్‌కు సరైన కోడ్ లేదు. Admin ని సంప్రదించండి) మాస్టర్ రికార్డు తప్పుగా ఉంది. దాన్ని సరిచేయమని Admin ని అడగండి.
:::

::: section import-master
title: సబ్ డీలర్ మాస్టర్ ఇంపోర్ట్ చేయడం
routes: /SubDealerList
for: 
roles: Admin, SuperAdmin, CorporateAdmin
designations: 
minutes: 3
source-hash: c6fda3f4afafbb6e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఆమోదించిన సబ్ డీలర్ల Excel లిస్ట్‌ను Admin అప్‌లోడ్ చేస్తారు. ఇంపోర్ట్ ముందుగా ప్రతి వరుసను చెక్ చేసి చూపిస్తుంది. **Proceed Import** నొక్కే వరకు ఏదీ సేవ్ కాదు.

1. **Sub Dealer Master** లో, **Import Sub Dealers** నొక్కండి.
2. **Download Sample Format** నొక్కండి. **Sub Dealer Code**, **Sub Dealer Name**, **HQ**, **Region**, **State** కాలమ్‌లు నింపండి. యాప్‌లో ఉన్న అవే State, Region, HQ పేర్లు వాడండి.
3. **Choose Excel file** కింద, .xlsx ఫైల్ ఎంచుకోండి.
4. **Check Import** నొక్కండి. **Import summary** లో **New**, **Already same**, **Rejected** వరుసలు కనిపిస్తాయి. ఇంకా ఏదీ సేవ్ కాలేదు.
5. తిరస్కరించిన ప్రతి వరుసకు **Reason** చదవండి. అవసరమైతే Excel సరిచేసి మళ్లీ చెక్ చేయండి.
6. కొత్త వరుసలు సేవ్ చేయడానికి **Proceed Import** నొక్కండి.
7. ఇప్పుడు కొత్త సబ్ డీలర్లను ఫారంలో ఎంచుకోవచ్చు. వారి వివరాలు సేవ్ అయ్యాకే వారు లిస్ట్‌లో కనిపిస్తారు.

> Careful: ఇప్పటికే ఉన్న సబ్ డీలర్ యొక్క ఫర్మ్, State, Region లేదా HQ ను ఇంపోర్ట్ ద్వారా మార్చలేరు.
> Tip: తప్పు లేదా ఖాళీ కోడ్‌లను సరిచేయడానికి SuperAdmin కి **Correct Sub Dealer Codes**, **Repair Missing Codes** కూడా కనిపిస్తాయి. రెండూ ఏదైనా సేవ్ చేసే ముందు ప్రివ్యూ చూపిస్తాయి.

## సాధారణ సమస్యలు

- **"Warning: Existing Sub Dealer master conflict detected!"** (ఇప్పటికే ఉన్న సబ్ డీలర్ మాస్టర్‌తో తేడా ఉంది) ఇప్పటికే ఉన్న ఒక కోడ్‌కు ఫైల్‌లో వేరే ఫర్మ్, State, Region లేదా HQ ఇచ్చారు. దాన్ని మార్చడానికి Admin ని సంప్రదించండి.
- **కొన్ని వరుసల్లో Sub Dealer Code ఖాళీగా లేదా కనిపించకుండా ఉంది.** ఆ వరుసలు తిరస్కరించబడ్డాయి, సేవ్ కాలేదు. చూపించిన Excel వరుస నంబర్లు చెక్ చేయండి.
- **Check Import బూడిద రంగులో ఉంది.** ముందు ఒక .xlsx ఫైల్ ఎంచుకోండి. ఫైల్ 10 MB వరకు ఉండవచ్చు.
:::

::: section beneficiary-list
title: సబ్ డీలర్, ఉద్యోగి లబ్ధిదారులను చూడటం
routes: /SubDealerEmployeeMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: dfcd6ed3ad5ab51a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**SDWA** మెనూలో ఉన్న **Sub Dealer & Employee** స్క్రీన్, సబ్ డీలర్లు, డీలర్ల ఉద్యోగుల సంక్షేమ లబ్ధిదారులను ఉంచుతుంది. ప్రతి రికార్డుకు ముందు SM ఆమోదం, తర్వాత AVP ఆమోదం కావాలి.

1. ఒక ట్యాబ్ ఎంచుకోండి: **Sub Dealer** లేదా **Employee**.
2. కార్డులు చూడండి: మొత్తం, **Active**, **Inactive**.
3. సెర్చ్ బాక్స్‌లో టైప్ చేయండి. **All Status**, **Active** లేదా **Inactive**, మరియు **All Approval**, **Pending SM**, **Pending AVP**, **Approved** లేదా **Rejected** ఎంచుకోండి.
4. టేబుల్‌లో, ప్రతి రికార్డుకు **SM Status**, **AVP Status**, **Overall** చదవండి.
5. మీ రోల్ రికార్డులను మార్చగలిగితే, **Status** కాలమ్‌లో **Active** లేదా **Inactive** నొక్కి దాన్ని మార్చవచ్చు.

## సాధారణ సమస్యలు

- **"You do not have permission to view this data."** (ఈ డేటా చూడటానికి మీకు అనుమతి లేదు) Sub Dealer & Employee కి యాక్సెస్ ఇవ్వమని మీ admin ని అడగండి.
- **పైన ఫారం లేదు.** మీ హోదా రికార్డులను చూడటానికి మాత్రమే.
- **ఒక రికార్డు చాలా కాలంగా Pending అని చూపిస్తోంది.** అది SM కోసం, తర్వాత AVP కోసం ఎదురుచూస్తోంది. అది ఎక్కడ ఉందో చూడటానికి **Pending SM** లేదా **Pending AVP** ఫిల్టర్ వాడండి.
:::

::: section beneficiary-add
title: లబ్ధిదారుని చేర్చడం లేదా మార్చడం
routes: /SubDealerEmployeeMaster
for: SubDealerEmployeeMaster.Entry
roles: 
designations: 
minutes: 3
source-hash: c972236359c62bca
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఒక సబ్ డీలర్ లేదా ఒక డీలర్ ఉద్యోగి యొక్క సంక్షేమ లబ్ధిదారుని, ఒక్కొక్కరిగా గానీ Excel ఫైల్ నుంచి గానీ చేర్చండి. ప్రతి కొత్త రికార్డు SM ఆమోదం కోసం **Pending** గా మొదలవుతుంది.

1. **Sub Dealer** లేదా **Employee** ట్యాబ్ ఎంచుకోండి. పైన ఉన్న ఫారంలో **Add New Sub Dealer** లేదా **Add New Employee** అని ఉంటుంది.
2. సబ్ డీలర్ అయితే, **Main Dealer Code**, **Main Dealer / Firm Name**, **Sub Dealer Code**, **Sub Dealer Name**, **Beneficiary Name** నింపండి. ఉద్యోగి అయితే, **Dealer Code**, **Employee Name**, **Beneficiary Name** నింపండి.
3. మీ దగ్గర ఉన్న మిగతా వివరాలు కూడా చేర్చండి, ఉదాహరణకు **Beneficiary ID**, **Nominee Name**, **Date of Birth**, **Relationship**.
4. స్విచ్‌ను **Active** లోనే ఉంచి **Save** నొక్కండి.
5. చాలా మందిని ఒకేసారి చేర్చడానికి, **Download Template** నొక్కి, దాన్ని నింపి, తర్వాత **Upload File** నొక్కి Excel ఫైల్ ఎంచుకోండి.
6. ఒక రికార్డు మార్చడానికి, **Action** కాలమ్‌లోని పెన్సిల్ నొక్కి, సరిచేసి **Save** నొక్కండి. బిన్ రికార్డును డిలీట్ చేస్తుంది.

> Careful: డిలీట్ చేసిన రికార్డు మళ్లీ తిరిగి రాదు. రికార్డును మార్చినా దాని SM, AVP ఆమోదం మారదు.

## సాధారణ సమస్యలు

- **"Main Dealer Code is required."** (Main Dealer Code తప్పనిసరి) * గుర్తు ఉన్న ప్రతి ఫీల్డ్ నింపండి. ఏ ఫీల్డ్ మిస్ అయిందో మెసేజ్ చెబుతుంది.
- **"already exists" అని సేవ్ ఫెయిల్ అవుతోంది.** అదే Dealer Code, Sub Dealer Code, Beneficiary Name, DOB ఉన్న లబ్ధిదారు ఇప్పటికే లిస్ట్‌లో ఉన్నారు. దాని కోసం సెర్చ్ చేయండి.
- **"Upload completed with ... skipped row(s)".** (అప్‌లోడ్ పూర్తయింది, కొన్ని వరుసలు వదిలేయబడ్డాయి) మెసేజ్ కింద చూపించిన వదిలేసిన వరుసలు చదవండి. వాటిని కాపీ చేయడానికి **Copy** నొక్కి, ఫైల్ సరిచేసి, ఆ వరుసలను మళ్లీ అప్‌లోడ్ చేయండి.
:::

::: section beneficiary-approve
title: లబ్ధిదారుని ఆమోదించడం లేదా తిరస్కరించడం
routes: /SubDealerEmployeeMaster
for: 
roles: SMD, SMM, AVP
designations: 
minutes: 2
source-hash: c429d512dd70afcb
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ప్రతి లబ్ధిదారుని ముందు SMD లేదా SMM ఆమోదిస్తారు. AVP చివరి ఆమోదం ఇస్తారు. ఆమోదం పొంది Active గా ఉన్న లబ్ధిదారులను మాత్రమే సంక్షేమ పథకం అప్లికేషన్‌లో డీలర్ ఎంచుకోగలరు.

1. **Sub Dealer** లేదా **Employee** ట్యాబ్ ఎంచుకోండి.
2. ఆమోదం ఫిల్టర్‌ను **Pending SM** (SMD, SMM) లేదా **Pending AVP** (AVP) గా పెట్టండి.
3. రికార్డు వివరాలు చెక్ చేయండి: డీలర్ కోడ్, పేర్లు, పుట్టిన తేదీ, బంధుత్వం.
4. **Action** కాలమ్‌లో, ఆమోదించడానికి ఆకుపచ్చ టిక్, తిరస్కరించడానికి ఎరుపు క్రాస్ నొక్కండి. ఫోన్‌లో **Approve** లేదా **Reject** నొక్కండి.
5. **Remarks (optional)** టైప్ చేసి **Approve** లేదా **Reject** నొక్కండి.
6. **SM Status** లేదా **AVP Status** కాలమ్ మారుతుంది. SM ఆమోదించాకే AVP కి ఆ రికార్డు కనిపిస్తుంది.

> Careful: తిరస్కరించిన రికార్డు అక్కడే ఆగిపోతుంది. సంక్షేమ పథకాల కోసం అది డీలర్‌కు చూపించబడదు.

## సాధారణ సమస్యలు

- **వరుసలో టిక్ లేదా క్రాస్ లేదు.** **SM Status** **Pending** గా ఉన్నప్పుడు మాత్రమే SM కి అవి కనిపిస్తాయి. SM ఆమోదించాక, **AVP Status** **Pending** గా ఉన్నప్పుడు మాత్రమే AVP కి అవి కనిపిస్తాయి.
- **Overall కాలమ్‌లో SM Rejected అని ఉంది.** SM రికార్డును తిరస్కరించారు, కాబట్టి అది AVP కి వెళ్లదు.
:::
