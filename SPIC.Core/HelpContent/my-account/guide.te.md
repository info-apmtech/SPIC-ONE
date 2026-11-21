---
module: my-account
title: నా ఖాతా, డాష్‌బోర్డ్
icon: bi-person-circle
order: 118
language: te
category: సాధారణం
note: కొత్త గైడ్
meta-source-hash: a6f45f8833df3868
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 4b8137c0f3f7283d
confidence: ok

ఇవి మీకు సంబంధించిన స్క్రీన్‌లు: డీలర్ రిజిస్ట్రేషన్ డాష్‌బోర్డ్, మీ సొంత ప్రొఫైల్, మీ కోసం ఎదురుచూస్తున్న పనుల లిస్ట్, SPIC ని ఎలా సంప్రదించాలో.

- dashboard | icon: bi-speedometer2 | color: #0b6b3a
  title: డీలర్ రిజిస్ట్రేషన్‌లు
  text: మీ ప్రాంతంలోని ప్రతి డీలర్ దరఖాస్తును దాని స్టేటస్‌తో చూడండి. మీ కోసం ఎదురుచూస్తున్న వాటిపై పని చేయండి.
- profile | icon: bi-person-badge | color: #2563eb
  title: మీ ప్రొఫైల్
  text: మీ పేరు, హోదా, ప్రాంతం, స్టేటస్ చెక్ చేయండి. డీలర్లకు వారి రిజిస్ట్రేషన్ వివరాలు కూడా కనిపిస్తాయి.
- help | icon: bi-bell | color: #d97706
  title: అలర్ట్‌లు, సహాయం
  text: మీ కోసం ఏమి ఎదురుచూస్తోందో చూడండి, మీ రోజువారీ పని కనుక్కోండి, SPIC కి మెసేజ్ పంపండి.
:::

::: section flow
title: డాష్‌బోర్డ్‌లో డీలర్ రిజిస్ట్రేషన్ ఎలా ముందుకు వెళ్తుంది
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 6696a0bfbcbd48ab
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. డీలర్ డాష్‌బోర్డ్ కార్డు అది ఏ దశకు చేరిందో చూపిస్తుంది.

- creator | type: lane | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: మీ HQ
- rm-lane | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: మీ రీజియన్
- smm-lane | type: lane | color: #7c3aed | roles: SMD, SMM
  title: SMM
  sub: మీ రాష్ట్రం
- avp-lane | type: lane | color: #2563eb | roles: AVP, Admin
  title: AVP
  sub: చివరి ఆమోదం
- draft | lane: creator | col: 1 | icon: bi-pencil-square | color: #a16207 | next: rm | screen: dashboard
  title: నింపి సబ్మిట్ చేయడం
  status: Draft
  text: దరఖాస్తు సబ్మిట్ అయ్యే వరకు కార్డు **Draft** అని చూపిస్తుంది. దాన్ని కొనసాగించడానికి **Edit** నొక్కండి.
- rm | lane: rm-lane | col: 2 | icon: bi-hourglass-split | color: #1d4ed8 | next: smm | return: draft | screen: dashboard
  title: RM చెక్ చేస్తారు
  status: In RM
  text: RM **Review** నొక్కుతారు. RM తిరిగి పంపితే, కార్డు **Rejected by RM** అని చూపిస్తుంది, MO దాన్ని మళ్లీ మార్చవచ్చు.
- smm | lane: smm-lane | col: 3 | icon: bi-hourglass-split | color: #7c3aed | next: avp | return: rm | screen: dashboard
  title: SMM చెక్ చేస్తారు
  status: In SMM
  text: SMM **Review** నొక్కుతారు. SMM తిరిగి పంపితే, అది **Rejected by SMM** అని చూపించి RM దగ్గరికి తిరిగి వెళ్తుంది.
- avp | lane: avp-lane | col: 4 | icon: bi-patch-check | color: #4338ca | next: approved | return: smm | screen: dashboard
  title: AVP ఆమోదిస్తారు
  status: In AVP
  text: AVP చివరి ఆమోదం ఇస్తారు. AVP తిరిగి పంపితే, అది **Rejected by AVP** అని చూపించి SMM దగ్గరికి తిరిగి వెళ్తుంది.
- approved | lane: avp-lane | col: 5 | icon: bi-check-circle | color: #16a34a | screen: dashboard
  title: ఆమోదం వచ్చింది
  status: Approved
  text: కార్డు **Approved** అని చూపిస్తుంది. కొత్త డీలర్ అయితే, AVP Office కంపెనీ Dealer ID లు సేవ్ చేసే వరకు ముందు **In AVP Office** అని చూపిస్తుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 196e7278f9089805
confidence: ok

- draft | color: #a16207
  label: Draft
  text: ఇంకా సబ్మిట్ చేయలేదు. MO, MDO లేదా JMDO ఇంకా దాన్ని మార్చవచ్చు.
- in-rm | color: #1d4ed8
  label: In RM
  text: RM చెక్ చేయడం కోసం ఎదురుచూస్తోంది.
- in-smm | color: #7c3aed
  label: In SMM
  text: SMM చెక్ చేయడం కోసం ఎదురుచూస్తోంది.
- in-avp | color: #4338ca
  label: In AVP
  text: AVP చివరి ఆమోదం కోసం ఎదురుచూస్తోంది.
- in-avp-office | color: #0e7490
  label: In AVP Office
  text: కొత్త డీలర్‌కు ఆమోదం వచ్చింది. AVP Office ఇంకా కంపెనీ Dealer ID లు సేవ్ చేయాలి.
- approved | color: #047857
  label: Approved
  text: దరఖాస్తుకు పూర్తి ఆమోదం వచ్చింది.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: సరిచేయడానికి MO, MDO లేదా JMDO కి తిరిగి పంపబడింది.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: RM కి తిరిగి పంపబడింది.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: SMM కి తిరిగి పంపబడింది.
- action | color: #2563eb
  label: Action Required
  text: ఈ కార్డుపై ఇప్పుడు మీరు పని చేయాల్సిన వంతు.
- active | color: #047857
  label: Active
  text: డీలర్, లేదా ప్రొఫైల్‌లో మీ సొంత ఖాతా, యాక్టివ్‌గా ఉంది.
- inactive | color: #c2410c
  label: Inactive
  text: డీలర్, లేదా మీ సొంత ఖాతా, ఇప్పుడు యాక్టివ్‌గా లేదు.
- terminated | color: #dc2626
  label: Terminated
  text: డీలర్‌షిప్ ముగిసింది.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5025b20d273029c0
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. ప్రతి ఒక్కరూ తమ సొంత ప్రొఫైల్ తెరవవచ్చు.

- creator | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - డీలర్ దరఖాస్తులు తయారు చేయడం, మార్చడం
  - RM తిరిగి పంపిన దరఖాస్తులు సరిచేయడం
- approvers | color: #0d9488 | roles: RM, RMD, SMD, SMM
  title: RM, SMM
  - Action Required అని గుర్తు ఉన్న కార్డులు చెక్ చేయడం
  - డీలర్ లిస్ట్‌లు డౌన్‌లోడ్ చేయడం
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - చివరి ఆమోదం ఇవ్వడం
  - డీలర్లను ఇంపోర్ట్ చేయడం లేదా జోడించడం
  - Admin తమ సొంత ప్రొఫైల్ మార్చవచ్చు
- dealer | color: #7c3aed | roles: Dealer
  title: డీలర్
  - మీ ప్రొఫైల్‌లో రిజిస్ట్రేషన్ వివరాలు చెక్ చేయడం
  - మీ సంక్షేమ పథకం దరఖాస్తులు చూడటం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 40062c69f8f437fc
confidence: ok

- no-edit-button | popular: true
  q: డీలర్ కార్డుపై నాకు Edit లేదా Review కనిపించడం లేదు
  a: ఎవరి వంతు ఉందో వారికి మాత్రమే ఈ బటన్‌లు కనిపిస్తాయి. ఇప్పుడు అది ఎవరి దగ్గర ఉందో తెలుసుకోవడానికి కార్డుపై ఉన్న స్టేటస్ చూడండి.
- wrong-profile | popular: true
  q: నా ప్రొఫైల్‌లో నా పేరు, హోదా లేదా ప్రాంతం తప్పుగా ఉంది
  a: **Profile** స్క్రీన్‌లో మీరు దాన్ని మార్చలేరు. దాన్ని సరిచేయమని మీ అడ్మినిస్ట్రేటర్‌ను అడగండి.
- nothing-waiting
  q: Alerts లో "Nothing waiting for you" అని కనిపిస్తోంది
  a: ఇప్పుడు ఏ ఆమోదం, బుకింగ్ లేదా దరఖాస్తు మీ కోసం ఎదురుచూడటం లేదు. తర్వాత మళ్లీ చూడండి.
- dealer-buttons
  q: నా డీలర్ ప్రొఫైల్‌లో Download PDF, Change Password ఏమీ చేయడం లేదు
  a: ఈ రెండు బటన్‌లు ఇంకా పని చేయడం లేదు.
- no-directions
  q: Get Directions నొక్కితే "Location permission is required" అని వస్తోంది
  a: యాప్ లేదా బ్రౌజర్‌కు లొకేషన్ అనుమతి ఇచ్చి, మళ్లీ **Get Directions** నొక్కండి.
:::

::: section dashboard
title: డీలర్ రిజిస్ట్రేషన్‌లు చూసి పని చేయడం
routes: /Dashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-dash-kpis] | 2 > [data-help=ma-dash-filters] | 3 > [data-help=ma-dash-cards] | 4 > [data-help=ma-dash-cards] | 5 > [data-help=ma-dash-actions]
minutes: 3
source-hash: 1b2061e620bf738f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Dealer Registrations** స్క్రీన్ మీ ప్రాంతంలోని ప్రతి డీలర్ దరఖాస్తును ఒక కార్డుగా చూపిస్తుంది, పైన లెక్కలు ఉంటాయి. చాలా రోల్స్‌కు వారి సొంత State, Region లేదా HQ డీలర్లు మాత్రమే కనిపిస్తారు.

1. పైన ఉన్న లెక్కలు చూడండి: **Total Registrations**, **Draft**, **In RM**, **In SMM**, **In AVP**, **Approved**, **Rejected / Returned**. **Status** బాక్స్ active, inactive, terminated డీలర్లను లెక్కపెడుతుంది.
2. సెర్చ్ బాక్స్‌లో డీలర్ కోడ్ లేదా సంస్థ పేరు టైప్ చేయండి, లేదా కంపెనీ, డీలర్ రకం, స్టేటస్, రీజియన్, ఆమోదం స్టేటస్ లేదా క్రమం ఎంచుకోండి. కార్డులు వెంటనే మారతాయి.
3. ప్రతి కార్డు సంస్థ పేరు, **Region**, **HQ**, ఆమోదం స్టేటస్, డీలర్ స్టేటస్ చూపిస్తుంది. నీలం రంగు **Action Required** ట్యాగ్ ఉంటే ఇప్పుడు మీ వంతు. ఇంకా కార్డులు చూడటానికి కింద **Show more** నొక్కండి.
4. డ్రాఫ్ట్ లేదా తిరిగి వచ్చిన దరఖాస్తును కొనసాగించడానికి **Edit** నొక్కండి, మీకు పంపిన దాన్ని చెక్ చేయడానికి **Review** నొక్కండి. కన్ను ఐకాన్ దరఖాస్తును చదవడానికి తెరుస్తుంది. **SPIC**, **GFL** డీలర్‌షిప్ PDF ను తెరుస్తాయి.
5. మీ ఫిల్టర్‌లతో లిస్ట్‌ను Excel ఫైల్‌గా డౌన్‌లోడ్ చేయడానికి **Dealer Master** లేదా **Credit limit status** నొక్కండి. MO, MDO, JMDO కి **Create Existing Dealer** కూడా కనిపిస్తుంది, అది డీలర్ దరఖాస్తు ఫారం తెరుస్తుంది.

> Tip: చివరి ఆమోదం తర్వాత, డీలర్‌కు Dealer Code ఉన్నప్పుడు, Admin, MO కి మాత్రమే కార్డుపై **Credit Limit** కనిపిస్తుంది.
> Careful: Admin బిన్ (చెత్తబుట్ట) ఐకాన్‌తో రిజిస్ట్రేషన్‌ను డిలీట్ చేయవచ్చు. అది మళ్లీ తిరిగి రాదు.

## సాధారణ సమస్యలు

- **"No dealer registrations found."** (డీలర్ రిజిస్ట్రేషన్‌లు ఏవీ లేవు) మీ సెర్చ్ లేదా ఫిల్టర్‌లు మరీ ఎక్కువగా ఉండొచ్చు. సెర్చ్ బాక్స్ ఖాళీ చేసి, ఫిల్టర్‌లను మళ్లీ "All" కి మార్చండి.
- **Edit లేదా Review బటన్ లేదు.** దరఖాస్తు ఇప్పుడు వేరే స్థాయిలో ఉంది. కార్డుపై స్టేటస్ చూడండి.
- **పైన ఉన్న సంఖ్యలు నా రీజియన్‌తో సరిపోలడం లేదు.** లెక్కలు కూడా కార్డుల లాగే అదే ఫిల్టర్‌లను అనుసరిస్తాయి. ఫిల్టర్‌లను మళ్లీ "All" కి మార్చండి.
:::

::: section dashboard-add-dealers
title: డీలర్లను ఇంపోర్ట్ చేయడం లేదా జోడించడం
routes: /Dashboard
for: 
roles: Admin, AVP
designations: 
minutes: 3
source-hash: 1bfd6d4d63fce849
confidence: ok

**ఈ స్క్రీన్ దేనికి**
Admin, AVP ఇప్పటికే ఉన్న డీలర్లను Excel ఫైల్ నుంచి యాప్‌లోకి తేవచ్చు, ఒక డీలర్‌ను చేత్తో జోడించవచ్చు, లేదా కొత్త డీలర్ దరఖాస్తు మొదలుపెట్టవచ్చు.

1. Excel టెంప్లేట్ డౌన్‌లోడ్ చేయడానికి **Sample Format** నొక్కండి.
2. ప్రతి డీలర్‌కు ఒక వరుసలో **CUSTOMER**, **CUSTOMER NAME**, **State** నింపండి. CUSTOMER కోడ్ మొదటి అక్షరం అది ఏ కంపెనీ కోడ్ అని చెబుతుంది, ఉదాహరణకు SPIC కి **D**.
3. **Import from Excel** నొక్కి ఫైల్ (.xlsx లేదా .xls) ఎంచుకోండి. "Importing, please wait..." అని చూపిస్తున్నప్పుడు ఆగండి.
4. ఫలితం చదవండి. ఇంపోర్ట్ కాని వరుసలు కారణంతో పాటు **Skipped Groups** కింద ఉంటాయి. **Close** నొక్కండి.
5. ఒక్క డీలర్‌ను మాత్రమే జోడించడానికి, **Add Dealer** నొక్కి, **CUSTOMER Code**, **CUSTOMER Name**, **State** ఎంటర్ చేసి, **Save Dealer** నొక్కండి.
6. కొత్త డీలర్ కోసం పూర్తి దరఖాస్తు మొదలుపెట్టడానికి **Create New Dealer** నొక్కండి.

> Tip: Admin కి **Total Dealer List** కూడా కనిపిస్తుంది, అది రాష్ట్రాల వారీగా డీలర్ల లెక్క తెరుస్తుంది.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **CUSTOMER Code** | కంపెనీ రికార్డుల్లో ఉన్న డీలర్ కోడ్. తప్పనిసరి. |
| **CUSTOMER Name** | సంస్థ లేదా డీలర్ పేరు. |
| **State** | డీలర్ రాష్ట్రం. |

## సాధారణ సమస్యలు

- **కొన్ని వరుసలు వదిలేయబడ్డాయి.** **Skipped Groups** కింద కారణం చదివి, ఫైల్‌లో ఆ వరుసలు సరిచేసి, మళ్లీ ఇంపోర్ట్ చేయండి.
- **"Import failed."** (ఇంపోర్ట్ విఫలమైంది) ఫైల్ మూడు కాలమ్‌లు ఉన్న Excel టెంప్లేట్ అవునో కాదో చూసి, మళ్లీ ప్రయత్నించండి.
:::

::: section profile
title: నా ప్రొఫైల్ చూడటం
routes: /Profile
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-prf-banner] | 2 > [data-help=ma-prf-tabs] | 3 > [data-help=ma-prf-details] | 4 > [data-help=ma-prf-details]
minutes: 2
source-hash: 854cdeee48d9aeee
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Profile** స్క్రీన్ మీ సొంత ఖాతా వివరాలు చూపిస్తుంది. ఉద్యోగులకు వారి ఉద్యోగి వివరాలు కనిపిస్తాయి. డీలర్లకు వారి రిజిస్ట్రేషన్ వివరాలు కనిపిస్తాయి.

1. ఆకుపచ్చ బ్యానర్ చూడండి. అది మీ పేరు, మీ రోల్ లేదా కోడ్, మీ హోదా చూపిస్తుంది. డీలర్లకు **Profile Completion** కూడా కనిపిస్తుంది.
2. ట్యాబ్‌లు వాడండి. ఉద్యోగులకు ఒక ట్యాబ్ కనిపిస్తుంది (**Employee**, Admin కి **Profile**). డీలర్లకు **Dealer Info**, **Proprietorship**, **Nominees**, **Trade Experience**, **Turnover**, **Assets**, **Welfare Scheme** కనిపిస్తాయి.
3. కార్డులో మీ వివరాలు చదవండి, ఉదాహరణకు **Designation**, **Region**, **Headquarter**, **Status**. **Turnover** లో **SPIC**, **Green Star** మధ్య మారవచ్చు.
4. ఏదైనా తప్పుగా ఉంటే, సరిచేయమని మీ అడ్మినిస్ట్రేటర్‌ను అడగండి. ఈ స్క్రీన్‌లో మీ వివరాలు మీరు మార్చలేరు.

> Careful: డీలర్ ప్రొఫైల్‌లో **Download PDF**, **Change Password** ఇంకా పని చేయడం లేదు.

## సాధారణ సమస్యలు

- **"No employee profile information available."** (ఉద్యోగి ప్రొఫైల్ సమాచారం లేదు) మీ ఉద్యోగి రికార్డు మీ లాగిన్‌తో లింక్ కాలేదు. మీ అడ్మినిస్ట్రేటర్‌ను అడగండి.
- **"No dealer registration data available."** (డీలర్ రిజిస్ట్రేషన్ సమాచారం లేదు) మీ డీలర్ రిజిస్ట్రేషన్ ఇంకా మీ లాగిన్‌తో లింక్ కాలేదు. మీ MO ని సంప్రదించండి.
- **"No welfare scheme applications found."** (సంక్షేమ పథకం దరఖాస్తులు లేవు) మీరు ఇంకా ఏ సంక్షేమ పథకానికీ దరఖాస్తు చేయలేదు.
:::

::: section profile-edit
title: నా ప్రొఫైల్ మార్చడం (Admin)
routes: /Profile
for: 
roles: Admin
designations: 
minutes: 1
source-hash: 50d60fcf9a6af1a1
confidence: ok

**ఈ స్క్రీన్ దేనికి**
Admin రోల్‌కు మాత్రమే **Edit Profile** బటన్ కనిపిస్తుంది. Admin తమ సొంత పేరు, ఈమెయిల్, హోదా మార్చవచ్చు.

1. **Admin Details** కార్డుపై **Edit Profile** నొక్కండి.
2. **Name**, **Email** లేదా **Designation** మార్చండి. Role, User Name, Status ఇక్కడ మార్చలేరు.
3. **Save Changes** నొక్కండి. "Profile updated successfully." (ప్రొఫైల్ అప్‌డేట్ అయింది) అనే మెసేజ్ వస్తుంది.
4. సేవ్ చేయకుండా మూసివేయడానికి **Cancel** నొక్కండి.

## సాధారణ సమస్యలు

- **"Your profile could not be saved. Please try again."** (మీ ప్రొఫైల్ సేవ్ కాలేదు) మీ నెట్‌వర్క్ చెక్ చేసి, మళ్లీ **Save Changes** నొక్కండి.
- **ఫారం లాక్ అయి, మీ వివరాలు లోడ్ కాలేదని చూపిస్తోంది.** **Cancel** నొక్కి, మళ్లీ **Edit Profile** తెరవండి.
:::

::: section user-details
title: మీ ప్రొఫైల్ పూర్తి చేయడం (ప్రివ్యూ)
routes: /UserProfile
for: 
roles: 
designations: 
minutes: 1
source-hash: e4cca852b7305bb4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**User Details** స్క్రీన్ ఒక డిజైన్ ప్రివ్యూ మాత్రమే. అది మెనూలో లేదు, ఇంకా ఏదీ సేవ్ చేయదు.

1. స్క్రీన్ **Complete Your Profile** ని, **Full Name**, **Mobile Number**, **Address**, **District** ఫీల్డ్‌లను చూపిస్తుంది.
2. **Continue** బటన్ ఈ వివరాలను ఇంకా సేవ్ చేయదు.

## సాధారణ సమస్యలు

- **నా వివరాలు సేవ్ కాలేదు.** ఇది మామూలే. ఈ స్క్రీన్ ఒక ప్రివ్యూ మాత్రమే. మీ అసలు వివరాలు **Profile** లో ఉన్నాయి.
:::

::: section alerts
title: నా కోసం ఏమి ఎదురుచూస్తోందో చూడటం
routes: /Alerts
for: 
roles: 
designations: 
minutes: 1
source-hash: b3694f1c480ea2a4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Alerts** స్క్రీన్ మీరు పని చేయాల్సి రావచ్చే క్యూలను (queues) లిస్ట్ చేస్తుంది. ఫోన్ లేదా ట్యాబ్లెట్‌లో, యాప్ పైన ఉన్న గంట ఐకాన్‌తో దీన్ని తెరవండి. మీ రోల్ వాడగలిగే లింక్‌లు మాత్రమే ఇందులో కనిపిస్తాయి.

1. **Approvals waiting** కింద, ఒక క్యూ నొక్కండి, ఉదాహరణకు **Scheme Approval** లేదా **Dealer Application Review**.
2. **Bookings and applications** కింద, **My Bookings** లేదా **Welfare Schemes** నొక్కండి.
3. ఆ స్క్రీన్‌లోని పనులు పూర్తి చేసి, తర్వాత ఇక్కడికి తిరిగి రండి.

> Tip: యాప్ స్టోర్ విడుదలతో పుష్ నోటిఫికేషన్‌లు వస్తాయి. అప్పటివరకు ఈ స్క్రీన్ చూస్తూ ఉండండి.

## సాధారణ సమస్యలు

- **"Nothing waiting for you".** (మీ కోసం ఏమీ ఎదురుచూడటం లేదు) మీ రోల్‌కు ఇక్కడ ఆమోదం, బుకింగ్ లేదా దరఖాస్తు స్క్రీన్‌లు ఏవీ లేవు.
- **ఒక క్యూ తెరుచుకుంటుంది కానీ ఖాళీగా ఉంది.** మీ రోల్‌కు ఈ లింక్ ఎప్పుడూ కనిపిస్తుంది. ఏదో ఎదురుచూస్తోందని దాని అర్థం కాదు.
:::

::: section activities
title: నా రోజువారీ పని తెరవడం
routes: /Activities
for: 
roles: 
designations: 
minutes: 1
source-hash: 3155933a5c612823
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**My Activities** స్క్రీన్ మీ ఫీల్డ్ పని, ఆమోదాలు, గెస్ట్ హౌస్ బుకింగ్‌లు, రిపోర్టులు అన్నింటినీ ఒకే చోట చూపిస్తుంది. ఫోన్‌లో, MO సిబ్బంది దీన్ని కింద ఉన్న ట్యాబ్ బార్‌లో చూడవచ్చు.

1. **Field activities** కింద, మీకు ఉంటే **Sample Collection** లేదా **Consignment History** నొక్కండి.
2. **Pending on you** కింద, **Scheme Approval** లేదా **Pending Acknowledgement** లాంటి ఒక క్యూ నొక్కండి.
3. **Guest house** కింద, **Book a room** లేదా **My Bookings** నొక్కండి.
4. **Reports** కింద, **Reports Center** నొక్కండి.

> Tip: ఒక భాగంలోని లింక్‌లలో కనీసం ఒక్కటైనా మీ రోల్ వాడగలిగితేనే ఆ భాగం కనిపిస్తుంది.

## సాధారణ సమస్యలు

- **నాకు "More of the Sales Activity System is on its way" మాత్రమే కనిపిస్తోంది.** మీ రోల్‌కు ఇంకా ఫీల్డ్ పని స్క్రీన్‌లు లేవు. అవి తర్వాత జోడించబడతాయి.
:::

::: section contact-us
title: SPIC ని సంప్రదించడం
routes: /ContactUs
for: 
roles: 
designations: 
minutes: 2
source-hash: 6825b4b8292fb086
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Contact Us** స్క్రీన్ SPIC ఆఫీస్ చిరునామా, ఈమెయిల్, ఫోన్ నంబర్, ఒక మ్యాప్, మీ ప్రశ్న పంపడానికి ఒక ఫారం చూపిస్తుంది.

1. మూడు కార్డులపై ఉన్న ఆఫీస్ చిరునామా, ఈమెయిల్, ఫోన్ నంబర్ చదవండి. ఆఫీస్ సమయం సోమవారం నుంచి శుక్రవారం వరకు, ఉదయం 9:30 నుంచి సాయంత్రం 6:00 వరకు.
2. మీ మ్యాప్ యాప్‌లో ఆఫీస్ చూడటానికి **Open in Maps** నొక్కండి.
3. మీరు ఉన్న చోటు నుంచి రోడ్డు దారి, దూరం, ప్రయాణ సమయం చూడటానికి **Get Directions** నొక్కండి.
4. **Send Us a Message** కింద, **Full Name**, **Email Address**, **Enquiry Type**, **Subject**, **Message** నింపండి.
5. **Send Feedback** నొక్కండి. "Thank you! Your enquiry has been sent." (ధన్యవాదాలు! మీ ప్రశ్న పంపబడింది) అనే మెసేజ్ వస్తుంది.

> Careful: **Send Email**, **Call Office**, **WhatsApp Us** ఇంకా పని చేయడం లేదు. కార్డులపై ఉన్న ఈమెయిల్ లేదా ఫోన్ నంబర్ వాడండి.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Enquiry Type** | **Welfare Schemes**, **Room Bookings** లేదా **General Enquiry**. |
| **Email Address** | SPIC మీకు జవాబు పంపగలిగే ఈమెయిల్. |
| **Message** | మీ ప్రశ్న, కొన్ని లైన్లలో. |

## సాధారణ సమస్యలు

- **"Please fill in every field before sending."** (పంపే ముందు అన్ని ఫీల్డ్‌లు నింపండి) ఐదు ఫీల్డ్‌లూ తప్పనిసరి.
- **"Please enter a valid email address."** (సరైన ఈమెయిల్ అడ్రస్ ఎంటర్ చేయండి) ఈమెయిల్ చెక్ చేయండి, ఉదాహరణకు name@example.com.
- **"Location permission is required to calculate the distance to SPIC Office."** (SPIC ఆఫీస్‌కు దూరం లెక్కించడానికి లొకేషన్ అనుమతి కావాలి) యాప్‌కు లొకేషన్ అనుమతి ఇచ్చి, మళ్లీ **Get Directions** నొక్కండి.
- **"Your enquiry could not be sent."** (మీ ప్రశ్న పంపలేకపోయాము) మీ నెట్‌వర్క్ చెక్ చేసి, తర్వాత మళ్లీ ప్రయత్నించండి.
:::

::: section gallery
title: ఫోటోలు, వీడియోలు (ప్రివ్యూ)
routes: /Gallery, /Album, /ViewVideo
for: 
roles: 
designations: 
minutes: 1
source-hash: 00d0d57d14bc58ed
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Gallery** లో SDWA కార్యక్రమాలు, మీటింగ్‌లు, గెస్ట్ హౌస్ యొక్క నమూనా ఫోటోలు కనిపిస్తాయి. ఈ స్క్రీన్‌లు నమూనా ఫోటోలతో ఉన్న డిజైన్ ప్రివ్యూ మాత్రమే, ఇంకా అసలు ఆల్బమ్‌లు కావు.

1. **Gallery** లో, **Photos**, **Videos** స్క్రోల్ చేసి చూడండి.
2. **Guest House Rooms** ఆల్బమ్ తెరవడానికి, ఒక ఫోటోపై **View Album** నొక్కండి.
3. ఆల్బమ్‌లో, **All Media**, **Photos** లేదా **Videos** నొక్కండి, లేదా సెర్చ్ బాక్స్‌లో టైప్ చేయండి.
4. వీడియో కార్డు నొక్కండి. ఇంకా ఏ వీడియో పెట్టలేదు కాబట్టి, వీడియో స్క్రీన్ "Video will appear here" అని చూపిస్తుంది.

> Tip: **Gallery** లోని ట్యాబ్‌లు, సెర్చ్ బాక్స్, క్రమం లిస్ట్ ఇంకా పని చేయడం లేదు. ప్రతి **View Album** బటన్ అదే నమూనా ఆల్బమ్‌ను తెరుస్తుంది.

## సాధారణ సమస్యలు

- **వీడియో ప్లే కావడం లేదు.** ఇంకా ఏ వీడియో పెట్టలేదు.
:::
