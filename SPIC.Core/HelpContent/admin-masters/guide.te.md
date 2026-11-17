---
module: admin-masters
title: నిర్వహణ, మాస్టర్ లిస్ట్‌లు
icon: bi-gear
order: 117
language: te
category: నిర్వహణ
note: కొత్త గైడ్
meta-source-hash: 2b15dc6f86ab1703
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 09f2a6bb7f506c56
confidence: ok

SPIC ONE లోని మిగతా స్క్రీన్‌లు వాడే ప్రాథమిక లిస్ట్‌లను ఈ స్క్రీన్‌లు ఉంచుతాయి: ఉద్యోగులు, వారి లాగిన్‌లు, డెసిగ్నేషన్‌లు (ఎవరు ఏ పేజీలు తెరవగలరు), లొకేషన్‌లు, పంటలు, ప్రొడక్ట్‌లు, బ్యాంకులు, ఇంకా కొన్ని సెట్టింగ్‌లు. వీటిలో చాలావరకు సాధారణ లిస్ట్‌లే: ఒక రికార్డు చేర్చండి, మార్చండి, ఆన్ లేదా ఆఫ్ చేయండి.

- people | icon: bi-people | color: #374151
  title: ఉద్యోగులు, యాక్సెస్
  text: ఉద్యోగులను రిజిస్టర్ చేయండి, ప్రతి ఒక్కరికీ ఒక రోల్, ఒక లొకేషన్ ఇవ్వండి, పేజీ యాక్సెస్ కోసం ఒక డెసిగ్నేషన్ ఎంచుకోండి.
- masters | icon: bi-list-check | color: #0d9488
  title: మాస్టర్ లిస్ట్‌లు
  text: లొకేషన్‌లు, పంటలు, ప్రొడక్ట్‌లు, బ్యాంకులు, ప్లాంట్‌లు, ఇంకా మిగతా స్క్రీన్‌లు వాడే ఇతర లిస్ట్‌లు.
- tools | icon: bi-sliders | color: #2563eb
  title: సెట్టింగ్‌లు, టూల్స్
  text: GPS ఇంటర్వెల్, CSR చివరి రోజులు, యాప్ వాడకం లెక్కలు, IFMS ఇంపోర్ట్, Data Explorer.
:::

::: section flow
title: కొత్త ఉద్యోగిని ఎలా సెటప్ చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: e8d287d4ff418bfb
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- admin | type: lane | color: #374151 | roles: Admin, SuperAdmin, CorporateAdmin
  title: అడ్మినిస్ట్రేటర్
  sub: Admin · SuperAdmin · CorporateAdmin
- staff | type: lane | color: #0d9488
  title: ఉద్యోగి
  sub: ఫీల్డ్ సిబ్బంది, ఆఫీసు సిబ్బంది
- locations | lane: admin | col: 1 | icon: bi-geo-alt | color: #0d9488 | next: designation | screen: location-master
  title: లొకేషన్‌లు చేర్చడం
  status: Active
  text: **Location Master** లో, ఉద్యోగి పని చేయబోయే జోన్, రాష్ట్రం, రీజియన్, హెడ్‌క్వార్టర్ చేర్చండి.
- designation | lane: admin | col: 2 | icon: bi-shield-lock | color: #7c3aed | next: register | screen: designation
  title: పేజీ యాక్సెస్ ఎంచుకోవడం
  status: Active
  text: **Designation Master** లో ఒక డెసిగ్నేషన్ తయారు చేసి, అది ఏ పేజీల్లో **View**, **Entry**, **Update** లేదా **Delete** చేయగలదో టిక్ చేయండి.
- register | lane: admin | col: 3 | icon: bi-person-plus | color: #2563eb | next: signin | screen: register-employee
  title: ఉద్యోగిని రిజిస్టర్ చేయడం
  status: Active
  text: రోల్, డెసిగ్నేషన్, లొకేషన్ ఎంచుకుని, లాగిన్ వివరాలు ఎంటర్ చేసి **Save Employee** నొక్కండి.
- signin | lane: staff | col: 4 | icon: bi-box-arrow-in-right | color: #16a34a
  title: లాగిన్ అయి పని చేయడం
  text: ఉద్యోగి యూజర్ నేమ్, పాస్‌వర్డ్‌తో లాగిన్ అవుతారు. డెసిగ్నేషన్ అనుమతించే పేజీలు మాత్రమే మెనూలో కనిపిస్తాయి.
- upload-stopped | lane: admin | col: 3 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: register | to: locations | screen: employee-list
  title: అప్‌లోడ్ ఆగిపోయింది
  text: ఫైల్‌లోని జోన్, రాష్ట్రం, రీజియన్ లేదా HQ యాప్‌లో లేకపోతే, ఉద్యోగుల బల్క్ అప్‌లోడ్ ఆగిపోతుంది. దాన్ని **Location Master** లో చేర్చి, అదే ఫైల్‌ను మళ్లీ అప్‌లోడ్ చేయండి.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: d29ecb092c153613
confidence: ok

- active | color: #16a34a
  label: Active
  text: రికార్డు వాడకంలో ఉంది, మిగతా స్క్రీన్‌ల లిస్ట్‌లలో కనిపిస్తుంది.
- inactive | color: #64748b
  label: Inactive
  text: రికార్డు ఉంచబడింది కానీ ఆఫ్ చేయబడింది. కొత్త ఎంట్రీలలో కనిపించదు.
- open | color: #dc2626
  label: Open
  text: Metrics లో మాత్రమే. ఇంకా ఎవరూ సరిచేసినట్టు గుర్తించని ఎర్రర్ గ్రూప్.
- resolved | color: #16a34a
  label: Resolved
  text: Metrics లో మాత్రమే. ఈ ఎర్రర్ గ్రూప్ సరిచేయబడిందని ఎవరో గుర్తించారు.
- pending | color: #2563eb
  label: Pending
  text: IFMS ఇంపోర్ట్‌లో మాత్రమే. రన్ మొదలవడానికి ఎదురుచూస్తోంది.
- running | color: #2563eb
  label: Running
  text: IFMS ఇంపోర్ట్‌లో మాత్రమే. డౌన్‌లోడ్, ఇంపోర్ట్ ఇప్పుడు జరుగుతున్నాయి.
- succeeded | color: #16a34a
  label: Succeeded
  text: IFMS ఇంపోర్ట్‌లో మాత్రమే. అన్ని రిపోర్టులు ఇంపోర్ట్ అయ్యాయి.
- partial | color: #d97706
  label: PartiallySucceeded
  text: IFMS ఇంపోర్ట్‌లో మాత్రమే. కొన్ని రిపోర్టులు ఇంపోర్ట్ అయ్యాయి, కొన్ని కాలేదు.
- failed | color: #dc2626
  label: Failed
  text: IFMS ఇంపోర్ట్‌లో మాత్రమే. రన్ లేదా రిపోర్ట్ పని చేయలేదు. ఎరుపు రంగు మెసేజ్ చదవండి.
- skipped | color: #dc2626
  label: Skipped
  text: IFMS ఇంపోర్ట్‌లో మాత్రమే. రిపోర్ట్ రన్ కాలేదు.
- disabled | color: #64748b
  label: Disabled
  text: IFMS Logins లో మాత్రమే. ఆటోమేషన్ ఈ లాగిన్‌ను వాడదు.
- no-password | color: #dc2626
  label: No password set
  text: IFMS Logins లో మాత్రమే. తర్వాతి రన్‌కు ముందు పాస్‌వర్డ్ ఎంటర్ చేయండి.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: fb58f5d6b0867738
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ లేదా డెసిగ్నేషన్ అనుమతించే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- admin | color: #374151 | roles: Admin, SuperAdmin
  title: Admin, SuperAdmin
  - ఈ మాడ్యూల్‌లోని ప్రతి స్క్రీన్ తెరవడం (Admin కి Data Explorer తప్ప)
  - ఉద్యోగులను రిజిస్టర్ చేసి వారి డెసిగ్నేషన్ ఎంచుకోవడం
  - మాస్టర్ లిస్ట్‌లను అప్‌డేట్‌గా ఉంచడం
  - IFMS ఇంపోర్ట్, metrics గమనించడం
- superadmin | color: #7c3aed | roles: SuperAdmin
  title: SuperAdmin మాత్రమే
  - Data Explorer వాడటం
  - ఇప్పటికే ఉన్న ఉద్యోగుల రీజియన్, HQ ని Excel ద్వారా మార్చడం
- corporate | color: #2563eb | roles: CorporateAdmin
  title: CorporateAdmin
  - Metrics స్క్రీన్ చూడటం
  - మీ డెసిగ్నేషన్ ఇస్తే ఇక్కడి ఇతర స్క్రీన్‌లు తెరవడం
  - మీరు అనుమతి ఉన్న వ్యక్తి అయితేనే Data Explorer వాడటం
- others | color: #0d9488
  title: ఇతర సిబ్బంది
  - మీ డెసిగ్నేషన్ ఇస్తేనే ఇక్కడి ఒక స్క్రీన్ కనిపిస్తుంది
  - మీ డెసిగ్నేషన్ అనుమతించినట్టు, చూడటం మాత్రమే, లేదా చేర్చడం, మార్చడం, డిలీట్ చేయడం కూడా చేయవచ్చు
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 6e458e262a2cfdf7
confidence: ok

- no-pages | popular: true
  q: కొత్త ఉద్యోగి లాగిన్ అవుతున్నారు, కానీ కావాల్సిన పేజీలు కనిపించడం లేదు
  a: పేజీ యాక్సెస్ డెసిగ్నేషన్ నుంచి వస్తుంది. **Designation Master** తెరిచి, ఉద్యోగి డెసిగ్నేషన్‌ను మార్చి, ఆ పేజీలకు **View** టిక్ చేయండి (అవసరమైతే **Entry** లేదా **Update** కూడా). తర్వాత **Employee Registration** లో ఉద్యోగికి ఆ డెసిగ్నేషన్ ఉందో లేదో చెక్ చేయండి.
- upload-missing | popular: true
  q: ఉద్యోగుల అప్‌లోడ్‌లో "Missing master data — nothing was uploaded" అని వస్తోంది
  a: మీ Excel ఫైల్‌లోని జోన్, రాష్ట్రం, రీజియన్ లేదా HQ ఇంకా యాప్‌లో లేదు. దాన్ని **Location Master** లో చేర్చి, అదే ఫైల్‌ను మళ్లీ ఎంచుకోండి.
- cannot-delete
  q: ఒక లొకేషన్ లేదా పంటను డిలీట్ చేయలేకపోతున్నాను. "Cannot Delete" అని వస్తోంది
  a: ఆ రికార్డును ఇతర రికార్డులు వాడుతున్నాయి, అవి బాక్స్‌లో కనిపిస్తాయి. ముందు వాటిని తీసేయండి లేదా మార్చండి, లేదా రికార్డును **Inactive** చేసేయండి.
- no-save
  q: మాస్టర్ స్క్రీన్ కనిపిస్తోంది కానీ Save బటన్ లేదు
  a: మీ డెసిగ్నేషన్ మీకు **View** మాత్రమే ఇస్తుంది. ఆ పేజీకి **Entry** (చేర్చడానికి) లేదా **Update** (మార్చడానికి) ఇవ్వమని అడ్మినిస్ట్రేటర్‌ను అడగండి.
- ifms-password
  q: IFMS Logins స్క్రీన్‌లో "A password needs changing" అని వస్తోంది
  a: IFMS పోర్టల్ పాస్‌వర్డ్ ప్రతి 80 రోజులకు ఎక్స్‌పైర్ అవుతుంది. ముందు పోర్టల్‌లో మార్చండి, తర్వాత కొత్తదాన్ని **New password** లో పేస్ట్ చేసి **Update password** నొక్కండి.
:::

::: section employee-list
title: ఉద్యోగులను చూడటం, నిర్వహించడం
routes: /EmployeeManagement
for: 
roles: 
designations: 
minutes: 2
source-hash: 4cbca19ed0bdeb5d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Employee Management** స్క్రీన్ ప్రతి ఉద్యోగి లాగిన్‌ను వారి రోల్, రాష్ట్రంతో చూపిస్తుంది. ఇక్కడి నుంచి కొత్త ఉద్యోగులను రిజిస్టర్ చేయవచ్చు, చాలా మందిని ఒకేసారి అప్‌లోడ్ చేయవచ్చు, లేదా ఒక లాగిన్‌ను మార్చవచ్చు, డిలీట్ చేయవచ్చు.

1. **Employee List** లో ఉద్యోగిని కనుక్కోవడానికి సెర్చ్ బాక్స్, **All Role**, **All States** వాడండి.
2. కాలమ్‌లు చదవండి: **Employee ID**, **Name**, **User Name**, **Password**, **Role**, **State**.
3. ఉద్యోగిని మార్చడానికి పెన్సిల్ నొక్కండి. వారి వివరాలు నిండి ఉన్న ఉద్యోగి ఫారం తెరుచుకుంటుంది.
4. లాగిన్ డిలీట్ చేయడానికి బిన్ (చెత్తబుట్ట) నొక్కి, **Delete this role?** బాక్స్‌లో **Delete** నొక్కండి.
5. ఒక ఉద్యోగిని చేర్చడానికి **Register Employee** నొక్కండి.
6. చాలా మందిని చేర్చడానికి, Excel ఫైల్ కోసం **Sample Format** నొక్కి, దాన్ని నింపి, **Bulk Upload** నొక్కి ఫైల్ ఎంచుకోండి. ఎన్ని వరుసలు **Inserted**, ఎన్ని **Skipped** అయ్యాయో బాక్స్ చూపిస్తుంది.
7. లిస్ట్‌ను బయటకు తీయడానికి టేబుల్ పైన ఉన్న **Copy**, **CSV**, **Excel**, **PDF** లేదా **Print** వాడండి.

> Careful: లాగిన్ డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు.
> Tip: అప్‌లోడ్ ఫైల్‌లో, ఫోన్ నంబరే లాగిన్ పాస్‌వర్డ్ అవుతుంది. ఆ వ్యక్తికి పూర్తి యాక్సెస్ కావాలంటేనే **Designation** ఖాళీగా వదలండి.

## Bulk upload కాలమ్‌లు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **EmployeeID**, **EmpName**, **UserName** | ఉద్యోగి ID, పేరు, లాగిన్ పేరు. |
| **Permission** | రోల్. అనుమతి ఉన్నవి: AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO. |
| **Zone**, **State**, **Region**, **HQ** | AVP కి జోన్ కావాలి. SMD, SMM కి రాష్ట్రం కావాలి. RM, RMD కి రాష్ట్రం, రీజియన్ కావాలి. MDO, MO, JMDO కి రాష్ట్రం, రీజియన్, HQ కావాలి. ఇవి ముందే యాప్‌లో ఉండాలి. |
| **PhoneNumber**, **EmailID** | కాంటాక్ట్ వివరాలు. ఫోన్ నంబరే పాస్‌వర్డ్. |
| **Designation** | Active గా ఉన్న ఒక డెసిగ్నేషన్ పేరుతో సరిగ్గా సరిపోవాలి. |

## సాధారణ సమస్యలు

- **"Missing master data — nothing was uploaded".** (మాస్టర్ డేటా లేదు — ఏదీ అప్‌లోడ్ కాలేదు) లేని జోన్, రాష్ట్రం, రీజియన్ లేదా HQ ని **Location Master** లో చేర్చి, అదే ఫైల్‌ను మళ్లీ ఎంచుకోండి.
- **"Issues found:"** (సమస్యలు కనిపించాయి) కొన్ని వరుసలు వదిలేయబడ్డాయి. ప్రతి గ్రూప్ కింద ఉన్న కారణం చదివి, ఆ వరుసలు సరిచేసి, మళ్లీ అప్‌లోడ్ చేయండి.
- **Register Employee లేదా Bulk Upload బటన్ లేదు.** మీ డెసిగ్నేషన్ **Employee Registration** లో **Entry** ఇవ్వడం లేదు.
- **Update Region / HQ.** ఈ బటన్ SuperAdmin కి మాత్రమే కనిపిస్తుంది. ఇది ఇప్పటికే ఉన్న లాగిన్‌ల రీజియన్, HQ ని మాత్రమే Excel ఫైల్ నుంచి మారుస్తుంది, ముందు ఒక ప్రివ్యూ చూపించిన తర్వాత.
:::

::: section register-employee
title: ఉద్యోగిని రిజిస్టర్ చేయడం లేదా మార్చడం
routes: /EmployeeRegistration
for: 
roles: 
designations: 
tour: 1 > [data-help=am-emp-role] | 2 > [data-help=am-emp-location] | 3 > [data-help=am-emp-login] | 4 > [data-help=am-emp-details] | 5 > [data-help=am-emp-save]
minutes: 3
source-hash: e7b641f98153cac9
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఒక ఉద్యోగికి లాగిన్ తయారు చేయడానికి, వారికి ఒక రోల్, ఒక లొకేషన్ ఇవ్వడానికి, వారు ఏ పేజీలు తెరవగలరో నిర్ణయించే డెసిగ్నేషన్ ఎంచుకోవడానికి **Employee Registration** స్క్రీన్ వాడండి. ఉద్యోగిని మార్చేటప్పుడు, టైటిల్ **Edit Employee** అని ఉంటుంది.

1. **Role & Authority** కింద, **Role**, **Designation** ఎంచుకోండి.
2. **Role Specification** కింద, ఆ రోల్‌కు కావాల్సిన లొకేషన్ ఎంచుకోండి, ఉదాహరణకు **State**, **Regions** లేదా **Headquarters**.
3. **Login Details** కింద, **User Name**, **Password** ఎంటర్ చేయండి. **Status** స్విచ్‌ను active లేదా inactive గా పెట్టండి.
4. **Employee Details** కింద, **Employee Name**, **Employee ID**, **Personal Phone Number**, **Official Phone Number**, **Email ID** ఎంటర్ చేయండి.
5. **Save Employee** నొక్కండి (మార్చేటప్పుడు **Update Employee**). **Cancel** సేవ్ చేయకుండా వెనక్కి వెళ్తుంది.

> Tip: రోల్‌ను బట్టి లొకేషన్ ఫీల్డ్‌లు మారతాయి. Director లాంటి కొన్ని రోల్‌లకు లొకేషన్ అవసరం లేదు.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Role** | ఆ వ్యక్తి ఉద్యోగ రోల్. తప్పనిసరి. |
| **Designation** | పేజీ యాక్సెస్‌ను నిర్ణయిస్తుంది. CommonRole కి తప్పనిసరి. |
| **Zone** | AVP కోసం. |
| **State** | SMD, SMM, MDAdmin, MDHOD, RM, RMD, MO, MDO, JMDO కోసం. |
| **Regions** | ఒకటి లేదా ఎక్కువ రీజియన్‌లు, RM, RMD కోసం. |
| **Region** మరియు **Headquarters** | ఒక రీజియన్, ఒకటి లేదా ఎక్కువ హెడ్‌క్వార్టర్‌లు, MO, MDO, JMDO కోసం. |
| **User Name** | లాగిన్ పేరు. స్పేస్‌లు ఉండకూడదు. తప్పనిసరి. |
| **Password** | కొత్త ఉద్యోగికి తప్పనిసరి. |
| **Personal Phone Number** | 10 అంకెలు. తప్పనిసరి. |
| **Email ID** | సరైన ఈమెయిల్ అడ్రస్. తప్పనిసరి. |

## సాధారణ సమస్యలు

- **"Please correct the highlighted fields and try again."** (హైలైట్ అయిన ఫీల్డ్‌లు సరిచేసి మళ్లీ ప్రయత్నించండి) ప్రతి ఫీల్డ్ కింద ఉన్న ఎరుపు మెసేజ్ చదివి సరిచేయండి.
- **"Username must not contain spaces or invisible characters."** (యూజర్ నేమ్‌లో స్పేస్‌లు లేదా కనిపించని అక్షరాలు ఉండకూడదు) స్పేస్‌లు లేకుండా యూజర్ నేమ్ మళ్లీ టైప్ చేయండి.
- **"A user or employee with these details already exists."** (ఈ వివరాలతో యూజర్ లేదా ఉద్యోగి ఇప్పటికే ఉన్నారు) వేరే లాగిన్‌కు అదే **User Name**, **Employee ID** లేదా **Email ID** ఉంది.
- **"Selected Region does not belong to the selected State."** (ఎంచుకున్న రీజియన్ ఎంచుకున్న రాష్ట్రంలో లేదు) ముందు రాష్ట్రం ఎంచుకుని, తర్వాత దానిలోని రీజియన్ ఎంచుకోండి.
- **Save Employee బటన్ లేదు.** మీ డెసిగ్నేషన్ **Employee Registration** లో **Entry** ఇవ్వడం లేదు (మార్చడానికి **Update**).
:::

::: section designation
title: డెసిగ్నేషన్, దాని పేజీ యాక్సెస్ తయారు చేయడం
routes: /Designation
for: 
roles: 
designations: 
tour: 1 > [data-help=am-des-name] | 2 > [data-help=am-des-pagesearch] | 3 > [data-help=am-des-grid] | 4 > [data-help=am-des-save] | 5 > [data-help=am-des-search]
minutes: 3
source-hash: e6dc32d0910d8413
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఒక వ్యక్తి ఏ పేజీలు తెరవగలరు, అక్కడ ఏమి చేయగలరు అనేది **Designation Master** స్క్రీన్ నిర్ణయిస్తుంది. ప్రతి ఉద్యోగికి ఒక డెసిగ్నేషన్ ఉంటుంది.

1. **Add New Designation** కింద, **Designation Name** టైప్ చేయండి. **Active** లేదా **Inactive** చేయడానికి స్విచ్ నొక్కండి.
2. **Role Access** లో, ఒక పేజీని త్వరగా కనుక్కోవడానికి **Search PageName...** లో టైప్ చేయండి.
3. ప్రతి పేజీకి **View**, **Entry**, **Update** లేదా **Delete** టిక్ చేయండి. **ALL** ఆ పేజీకి నాలుగూ టిక్ చేస్తుంది. **Select All** ప్రతి పేజీని టిక్ చేస్తుంది.
4. **Save** నొక్కండి (మార్చేటప్పుడు **Update**). **Cancel** ఫారం ఖాళీ చేస్తుంది.
5. **Existing Designations** లో లిస్ట్ వెతకండి. పెన్సిల్ డెసిగ్నేషన్‌ను మారుస్తుంది. అడ్డగీత ఉన్న వృత్తం దాన్ని Inactive చేస్తుంది, బాణం దాన్ని మళ్లీ Active చేస్తుంది.

> Tip: **View** ఉంటే పేజీ తెరవవచ్చు. **Entry** ఉంటే చేర్చవచ్చు, **Update** ఉంటే మార్చవచ్చు, **Delete** ఉంటే రికార్డులు తీసేయవచ్చు.
> Careful: Inactive చేసిన డెసిగ్నేషన్ అది ఉన్న యూజర్‌ల దగ్గర అలాగే ఉంటుంది, కానీ active లిస్ట్‌లలో కనిపించదు.

## సాధారణ సమస్యలు

- **"Select at least one permission".** (కనీసం ఒక అనుమతి ఎంచుకోండి) సేవ్ చేసే ముందు కనీసం ఒక బాక్స్ టిక్ చేయండి.
- **"Designation '...' already exists".** (ఈ డెసిగ్నేషన్ ఇప్పటికే ఉంది) వేరే పేరు వాడండి, లేదా ఉన్నదాన్నే మార్చండి.
- **నాకు Add New Designation ఫారం కనిపించడం లేదు.** మీ డెసిగ్నేషన్ మీకు **View** మాత్రమే ఇస్తుంది.
:::

::: section location-master
title: లొకేషన్ లిస్ట్ ఉంచడం
routes: /LocationMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 1a434ac83feeecb3
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Location Master** స్క్రీన్‌లో జోన్‌లు, రాష్ట్రాలు, జిల్లాలు, మండలాలు (sub-districts), రీజియన్‌లు, హెడ్‌క్వార్టర్‌లు ఉంటాయి. ఉద్యోగులు, డీలర్లు, రిపోర్టులు ఈ లిస్ట్‌లను వాడతాయి.

1. ఒక ట్యాబ్ ఎంచుకోండి: **Zones**, **States**, **Districts**, **Sub-Districts**, **Regions** లేదా **Headquarters**.
2. ఫారం నింపండి, ఉదాహరణకు **Zone Name**, **State Name**, **Zone**, లేదా **Headquarter Name**, **Region**.
3. స్విచ్‌ను **Active** లేదా **Inactive** గా పెట్టి **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
4. చాలా రికార్డులు చేర్చడానికి, **Sample Format** నొక్కి, Excel ఫైల్ నింపి, **Bulk Upload** నొక్కి ఫైల్ ఎంచుకోండి.
5. టేబుల్‌లో వెతకండి లేదా ఒక స్టేటస్ ఎంచుకోండి. స్టేటస్ మార్చడానికి దాన్ని నొక్కండి, మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Tip: ఈ క్రమంలో చేర్చండి: జోన్, తర్వాత రాష్ట్రం, తర్వాత రీజియన్, తర్వాత హెడ్‌క్వార్టర్. జిల్లాలు ఒక రాష్ట్రం కిందకు, మండలాలు ఒక జిల్లా కిందకు వస్తాయి.
> Careful: డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు. ఇతర రికార్డులు వాడుతున్న లొకేషన్‌ను డిలీట్ చేయలేరు.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Zone Name**, **Zone Code**, **Zone Color Code** | జోన్, మ్యాప్‌లలో దాని రంగు. పేరు తప్పనిసరి. |
| **State Name**, **State Code**, **Zone** | రాష్ట్రం, అది ఉన్న జోన్. పేరు, జోన్ తప్పనిసరి. |
| **District Name**, **State** | రెండూ తప్పనిసరి. |
| **Sub-District Name**, **District** | రెండూ తప్పనిసరి. **State** జిల్లాను కనుక్కోవడానికి మాత్రమే సాయం చేస్తుంది. |
| **Region Name**, **Region Code**, **State** | పేరు, రాష్ట్రం తప్పనిసరి. |
| **Headquarter Name**, **Region** | రెండూ తప్పనిసరి. |

## సాధారణ సమస్యలు

- **"Zone is required."** లేదా **"Region is required."** (జోన్ / రీజియన్ తప్పనిసరి) లిస్ట్ నుంచి పై స్థాయి లొకేషన్ ఎంచుకోండి.
- **"Cannot Delete".** (డిలీట్ చేయలేరు) ఆ రికార్డు వేరే చోట వాడబడుతోంది. ముందు ఆ రికార్డులను మార్చండి, లేదా దీన్ని **Inactive** చేయండి.
- **Bulk upload లో "Skipped Groups:" లేదా "Issues:" అని వస్తోంది.** కారణాలు చదవండి, వాటిని ఉంచుకోవడానికి **Copy** నొక్కండి, ఫైల్ సరిచేసి మళ్లీ అప్‌లోడ్ చేయండి.
:::

::: section agriculture-master
title: పంట, ప్రొడక్ట్ లిస్ట్‌లు ఉంచడం
routes: /Agriculture
for: 
roles: 
designations: 
minutes: 3
source-hash: 9d6cd17408623739
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Agriculture & Product Master** స్క్రీన్‌లో పంటలు, పంట రకాలు, పంట దశలు, పోటీదారులు, సెక్టార్‌లు, యూనిట్‌లు, కేటగిరీలు, ప్రొడక్ట్ గ్రూప్‌లు, ప్రొడక్ట్‌లు ఉంటాయి. మిగతా స్క్రీన్‌లు ఈ లిస్ట్‌లను వాడతాయి.

1. ఒక ట్యాబ్ ఎంచుకోండి: **Crops**, **Crop Types**, **Crop Stages**, **Competitors**, **Sector**, **Units**, **Categories**, **Product Groups** లేదా **Products**.
2. ఫారం నింపండి. **Crop Types**, **Crop Stages** కోసం, ముందు **Crop** ఎంచుకోండి. **Products** కోసం, **Category** ఎంచుకుని, తెలిస్తే **Product Group**, **Rate Per Unit (RPU)** చేర్చండి.
3. స్విచ్‌ను **Active** లేదా **Inactive** గా పెట్టి **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
4. చాలా రికార్డులు చేర్చడానికి, **Sample Format** నొక్కి, దాన్ని నింపి, **Bulk Upload** నొక్కి ఫైల్ ఎంచుకోండి. చేర్చిన, అప్‌డేట్ అయిన, తిరస్కరించిన వరుసలను బాక్స్ చూపిస్తుంది.
5. టేబుల్‌లో వెతకండి లేదా ఒక స్టేటస్ ఎంచుకోండి. స్టేటస్ మార్చడానికి దాన్ని నొక్కండి, మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Tip: కేటగిరీలకు ముందు యూనిట్‌లు, ప్రొడక్ట్‌లకు ముందు కేటగిరీలు తయారు చేయండి.
> Careful: డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు. వేరే చోట వాడబడుతున్న రికార్డును డిలీట్ చేయలేరు.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Category Name** మరియు **Unit** | రెండూ తప్పనిసరి. రెండో స్విచ్ కేటగిరీని **Speciality** లేదా **Regular** అని గుర్తిస్తుంది. |
| **Crop** | పంట రకం లేదా పంట దశ ఏ పంటకు చెందినదో అది. తప్పనిసరి. |
| **Product Name** మరియు **Category** | రెండూ తప్పనిసరి. ఒక ప్రొడక్ట్ పేరును ప్రతి కేటగిరీలో ఒక్కసారి మాత్రమే వాడవచ్చు. |
| **Rate Per Unit (RPU)** | ప్రొడక్ట్ రేటు. తప్పనిసరి కాదు. |

## సాధారణ సమస్యలు

- **"Please select an active crop."** (Active గా ఉన్న పంటను ఎంచుకోండి) ఆ పంట ఆఫ్ చేయబడింది. ముందు **Crops** ట్యాబ్‌లో దాన్ని ఆన్ చేయండి.
- **"... already exists".** (ఇప్పటికే ఉంది) ఈ పేరుతో ఒక రికార్డు ఇప్పటికే ఉంది. దాన్నే మార్చండి.
- **Sample Format లేదా Bulk Upload బటన్ లేదు.** ఇవి **Crop Types**, **Crop Stages** లో ఉండవు, లేదా మీ డెసిగ్నేషన్‌కు **Entry** లేదు.
:::

::: section financial-master
title: బ్యాంక్, ఆర్థిక సంవత్సరం లిస్ట్‌లు ఉంచడం
routes: /Financial
for: 
roles: 
designations: 
minutes: 2
source-hash: 149843a1d578c0f3
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Financial Master** స్క్రీన్‌లో మిగతా ఫారాలు వాడే బ్యాంకులు, ఆర్థిక సంవత్సరాలు ఉంటాయి.

1. **Bank** లేదా **Financial Year** ట్యాబ్ ఎంచుకోండి.
2. బ్యాంక్ అయితే, **Bank Name**, **IFSC Prefix** ఎంటర్ చేసి, మీ దగ్గర ఉంటే **Bank Logo** చేర్చండి. ఆర్థిక సంవత్సరం అయితే, **Financial Year Name**, **Start Date**, **End Date** ఎంటర్ చేయండి.
3. స్విచ్‌ను **Active** లేదా **Inactive** గా పెట్టి **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
4. బ్యాంకులు కార్డులుగా కనిపిస్తాయి, కింద **Show more** ఉంటుంది. ఆర్థిక సంవత్సరాలు పేజీలు ఉన్న టేబుల్‌లో కనిపిస్తాయి.
5. స్టేటస్ మార్చడానికి దాన్ని నొక్కండి, మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Careful: డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **IFSC Prefix** | IFSC కోడ్‌లోని మొదటి 4 అక్షరాలు మాత్రమే, ఉదాహరణకు HDFC లేదా SBIN. తప్పనిసరి. |
| **Bank Logo** | 512 KB కంటే చిన్న ఫోటో. తప్పనిసరి కాదు. |

## సాధారణ సమస్యలు

- **"IFSC Prefix is required."** (IFSC Prefix తప్పనిసరి) IFSC కోడ్‌లోని మొదటి 4 అక్షరాలు ఎంటర్ చేయండి.
- **"Logo file must be under 512 KB."** (లోగో ఫైల్ 512 KB కంటే చిన్నదిగా ఉండాలి) చిన్న ఫోటో ఎంచుకోండి.
- **Bulk Upload నొక్కితే ఏమీ జరగడం లేదు.** ఈ స్క్రీన్‌లో bulk upload ఇంకా సిద్ధంగా లేదు. రికార్డులు ఒక్కొక్కటిగా చేర్చండి.
:::

::: section relationship-master
title: బంధుత్వం లిస్ట్ ఉంచడం
routes: /Relationship
for: 
roles: 
designations: 
minutes: 1
source-hash: dfed886a1ac19a35
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Relationship Master** స్క్రీన్‌లో బంధుత్వాల లిస్ట్ ఉంటుంది, ఉదాహరణకు డీలర్ వివరాలలో పేరు ఉన్న వ్యక్తితో ఉన్న సంబంధం.

1. **Add New Relationship** కింద, **Relationship Name** టైప్ చేయండి.
2. స్విచ్‌ను **Active** లేదా **Inactive** గా పెట్టి **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
3. **Existing Relationships** లో వెతకండి లేదా ఒక స్టేటస్ ఎంచుకోండి.
4. స్టేటస్ మార్చడానికి దాన్ని నొక్కండి, మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Careful: డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు.

## సాధారణ సమస్యలు

- **"Relationship name is required."** (బంధుత్వం పేరు తప్పనిసరి) ముందు ఒక పేరు టైప్ చేయండి.
- **Bulk Upload నొక్కితే ఏమీ జరగడం లేదు.** ఈ స్క్రీన్‌లో bulk upload ఇంకా సిద్ధంగా లేదు. రికార్డులు ఒక్కొక్కటిగా చేర్చండి.
:::

::: section plant-dealer-nature
title: ప్లాంట్, డీలర్ రకం లిస్ట్‌లు ఉంచడం
routes: /PlantDealerNatureMaster
for: 
roles: 
designations: 
minutes: 1
source-hash: 99cbbff4aa8ad460
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Plant & Dealer Nature Master** స్క్రీన్‌లో Excel అప్‌లోడ్‌లలో, డీలర్ రికార్డులలో వాడే ప్లాంట్‌లు, డీలర్ రకాలు ఉంటాయి.

1. **Plants** లేదా **Dealer Natures** ట్యాబ్ ఎంచుకోండి.
2. **Plant Name** లేదా **Dealer Nature Name** టైప్ చేయండి.
3. స్విచ్‌ను **Active** లేదా **Inactive** గా పెట్టి **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
4. టేబుల్‌లో వెతకండి లేదా ఒక స్టేటస్ ఎంచుకోండి. స్టేటస్ మార్చడానికి దాన్ని నొక్కండి, మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Careful: డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు.

## సాధారణ సమస్యలు

- **"Plant name is required."** (ప్లాంట్ పేరు తప్పనిసరి) ముందు ఒక పేరు టైప్ చేయండి.
- **స్టేటస్‌ను నొక్కినా అది మారడం లేదు.** మీ డెసిగ్నేషన్‌కు ఈ పేజీలో **Update** లేదు.
:::

::: section expense-master
title: ఖర్చుల లిస్ట్ ఉంచడం
routes: /ExpenseInformation
for: 
roles: 
designations: 
minutes: 1
source-hash: cbd7b43eecb288ef
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Expense Information Master** స్క్రీన్‌లో ఖర్చుల పేర్ల లిస్ట్ ఉంటుంది.

1. **Add New Expense Information** కింద, **Expense Name** టైప్ చేయండి.
2. స్విచ్‌ను **Active** లేదా **Inactive** గా పెట్టి **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
3. **Existing Expense Informations** లో వెతకండి లేదా ఒక స్టేటస్ ఎంచుకోండి.
4. స్టేటస్ మార్చడానికి దాన్ని నొక్కండి, మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Careful: డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు.

## సాధారణ సమస్యలు

- **"Expense name is required."** (ఖర్చు పేరు తప్పనిసరి) ముందు ఒక పేరు టైప్ చేయండి.
:::

::: section gps-settings
title: GPS ట్రాకింగ్ ఇంటర్వెల్ సెట్ చేయడం
routes: /GpsSettings
for: 
roles: 
designations: 
minutes: 1
source-hash: 2b7d216628a0423f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
లైవ్ ట్రాకింగ్ సమయంలో JMDO మొబైల్ యాప్ ఎంత తరచుగా GPS లొకేషన్‌ను రికార్డు చేయాలో **GPS Settings** స్క్రీన్ సెట్ చేస్తుంది.

1. **Current Saved Interval**, **System Default** (20 s), **Allowed Range** (1 నుంచి 100 సెకన్లు) చూడండి.
2. స్లైడర్‌ను జరపండి, లేదా బాక్స్‌లో సెకన్ల సంఖ్య టైప్ చేయండి.
3. **Save Settings** నొక్కండి.
4. మళ్లీ 20 సెకన్లకు వెళ్లడానికి, **Reset to Default (20s)** నొక్కి, తర్వాత **Save Settings** నొక్కండి.

> Tip: చిన్న సంఖ్య లొకేషన్‌ను ఎక్కువసార్లు రికార్డు చేస్తుంది కానీ బ్యాటరీ ఎక్కువ ఖర్చవుతుంది. పెద్ద సంఖ్య బ్యాటరీని ఆదా చేస్తుంది.

## సాధారణ సమస్యలు

- **"Tracking interval must be an integer between 1 and 100 seconds."** (ఇంటర్వెల్ 1 నుంచి 100 సెకన్ల మధ్య పూర్తి సంఖ్య అయి ఉండాలి) 1 నుంచి 100 వరకు ఒక పూర్తి సంఖ్య ఎంటర్ చేయండి.
- **Save Settings బటన్ లేదు.** మీ డెసిగ్నేషన్‌కు **GPS Settings** లో **Update** లేదు.
:::

::: section csr-exceeding-day
title: CSR చివరి రోజులు సెట్ చేయడం
routes: /CSRExceedingDaySetting
for: 
roles: 
designations: 
minutes: 2
source-hash: 94dea6d5ea8fe4d7
confidence: ok

**ఈ స్క్రీన్ దేనికి**
నెలలో ఏ రోజు తర్వాత CSR-1, CSR-2 పొడవైన ఆమోద మార్గంలో వెళ్లాలో **CSR Exceeding Day Setting** స్క్రీన్ సెట్ చేస్తుంది. ఒక సమయంలో ఒక సెట్టింగ్ మాత్రమే active గా ఉంటుంది.

1. **CSR1 Day**, **CSR2 Day** ఎంటర్ చేయండి (1 నుంచి 31).
2. **Status** స్విచ్‌ను **Active** గా పెట్టి **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
3. **CSR Exceeding Day Settings** లో అన్ని సెట్టింగ్‌లు చూడండి. స్టేటస్ మార్చడానికి దాన్ని నొక్కండి, మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Tip: నెలలో **CSR1 Day** తర్వాత తయారైన CSR-1 కి, లేదా **CSR2 Day** తర్వాత సబ్మిట్ అయిన CSR-2 కి పొడవైన ఆమోదం కావాలి.
> Careful: ఒక active సెట్టింగ్ సేవ్ చేస్తే, పాత active సెట్టింగ్ ఆఫ్ అయిపోతుంది.

## సాధారణ సమస్యలు

- **"CSR1 Day must be between 1 and 31."** (CSR1 Day 1 నుంచి 31 మధ్య ఉండాలి) 1 నుంచి 31 వరకు ఒక రోజు ఎంటర్ చేయండి.
- **"You do not have permission to save this setting."** (ఈ సెట్టింగ్ సేవ్ చేయడానికి మీకు అనుమతి లేదు) మీ డెసిగ్నేషన్‌కు ఈ పేజీలో **Entry** లేదా **Update** లేదు.
:::

::: section metrics
title: యాప్ ఎలా వాడబడుతోందో చూడటం
routes: /Metrics
for: 
roles: 
designations: 
minutes: 2
source-hash: 42e4f35d4ca253b9
confidence: ok

**ఈ స్క్రీన్ దేనికి**
SPIC ONE ను ఎవరు వాడుతున్నారు, ఏ యాప్‌లో, ఏ పేజీలు, సర్వీసులు వాడబడుతున్నాయి, ఏమి తప్పు జరిగింది అనేది **Metrics** స్క్రీన్ చూపిస్తుంది. ఇది లెక్కలు మాత్రమే చూపిస్తుంది; ఎర్రర్‌లను గుర్తించడం తప్ప ఇక్కడి నుంచి ఏదీ మార్చలేరు.

1. కాలం ఎంచుకోండి: **7 days**, **30 days** లేదా **90 days**. ఒక యాప్ ఎంచుకోండి, లేదా **All apps** అలాగే ఉంచండి.
2. పైన ఉన్న లైవ్ లైన్ చదవండి: గత 15 నిమిషాల్లో ఆన్‌లైన్‌లో ఉన్నవారు, గత ఒక గంటలో రిక్వెస్ట్‌లు, ఎర్రర్‌లు.
3. ఒక ట్యాబ్ తెరవండి: **Overview**, **Users**, **Pages**, **API** లేదా **Errors**.
4. **Errors** లో, వెతకడానికి లేదా **Source**, **Status** (**Open**, **Resolved** లేదా **All**) ఎంచుకోవడానికి **Filters** తెరవండి.
5. ఒక ఎర్రర్ వివరాలు చూడటానికి దాన్ని నొక్కండి. అది సరిచేయబడితే **Mark resolved** నొక్కండి, లేదా **Reopen** నొక్కండి.

## సాధారణ సమస్యలు

- **"You do not have access to Metrics".** (Metrics చూడటానికి మీకు యాక్సెస్ లేదు) మీ డెసిగ్నేషన్‌కు Metrics పేజీ చేర్చమని అడ్మినిస్ట్రేటర్‌ను అడగండి.
- **"The metrics could not be loaded".** (metrics లోడ్ కాలేదు) ఇంటర్నెట్ కనెక్షన్ చెక్ చేసి **Try again** నొక్కండి.
- **"Live numbers unavailable".** (లైవ్ లెక్కలు అందుబాటులో లేవు) లైవ్ లెక్కలు చదవలేకపోయాము. అవి ప్రతి నిమిషం రిఫ్రెష్ అవుతాయి.
:::

::: section ifms-auto-import
title: రాత్రి జరిగే IFMS ఇంపోర్ట్‌ను గమనించడం
routes: /IfmsAutoImport
for: 
roles: 
designations: 
minutes: 2
source-hash: cc12211b4d92d31d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
IFMS పోర్టల్ (dbtfert.nic.in) నుంచి ప్రతి రాత్రి రిపోర్టులు డౌన్‌లోడ్ చేసి SPIC ONE లోకి ఇంపోర్ట్ చేయడాన్ని **IFMS Auto Import** స్క్రీన్ చూపిస్తుంది. ఇది 04:05 కి దానంతట అదే రన్ అవుతుంది.

1. చివరి రన్ కార్డులు చూడండి: **Last run**, **Reports**, **Rows added**, **Rows updated**, **Login**.
2. ప్రతి రన్ దాని తేదీ, ఎన్ని రిపోర్టులు ఇంపోర్ట్ అయ్యాయి, **Succeeded** లేదా **Failed** లాంటి స్టేటస్ చూపిస్తుంది.
3. ప్రతి రిపోర్ట్‌ను దాని **Status**, **Rows**, **New**, **Updated**, **Skipped** తో చూడటానికి ఒక రన్‌ను నొక్కండి.
4. పసుపు రంగు CAPTCHA బాక్స్ వస్తే, మీకు కనిపించే అక్షరాలు టైప్ చేసి **Send** నొక్కండి. తర్వాత డౌన్‌లోడ్ కొనసాగుతుంది.
5. వెంటనే రన్ మొదలుపెట్టడానికి **Run now** నొక్కండి, లేదా తాజా ఫలితాలు చూడటానికి **Refresh** నొక్కండి.

## సాధారణ సమస్యలు

- **"No runs yet".** (ఇంకా రన్‌లు లేవు) మొదటి రన్ 04:05 కి మొదలవుతుంది, లేదా **Run now** నొక్కండి.
- **"Could not queue a run."** (రన్ మొదలుపెట్టలేకపోయాము) మెసేజ్ చదివి, కాసేపటి తర్వాత మళ్లీ ప్రయత్నించండి.
- **CAPTCHA లో "expired" అని వస్తోంది.** కొత్త ఫోటో దానంతట అదే వస్తుంది. కొత్తది టైప్ చేయండి.
- **Login కార్డులో "failed" అని వస్తోంది.** IFMS పాస్‌వర్డ్ ఎక్స్‌పైర్ అయి ఉండొచ్చు. **IFMS Logins** చెక్ చేయండి.
:::

::: section ifms-logins
title: IFMS పోర్టల్ లాగిన్‌లు ఉంచడం
routes: /IfmsLogins
for: 
roles: 
designations: 
minutes: 2
source-hash: 7e0020b6083c5518
confidence: ok

**ఈ స్క్రీన్ దేనికి**
రాత్రి IFMS ఇంపోర్ట్ వాడే, ప్రతి కంపెనీ పోర్టల్ లాగిన్‌ను **IFMS Logins** స్క్రీన్ ఉంచుతుంది. పాస్‌వర్డ్‌లు సురక్షితంగా దాచబడతాయి, మళ్లీ ఎప్పుడూ చూపించబడవు.

1. ప్రతి కంపెనీ కార్డు మీద **Disabled**, **No password set** లాంటి ట్యాగ్, లేదా పాస్‌వర్డ్ ఎక్స్‌పైర్ అవడానికి ఇంకా ఎన్ని రోజులు ఉన్నాయో కనిపిస్తుంది.
2. **Password set**, **Expires**, **Last login**, **Result** చదవండి.
3. పోర్టల్‌లో పాస్‌వర్డ్ మారినప్పుడు, దాన్ని **New password** లో పేస్ట్ చేసి, **OTP goes to** చెక్ చేసి, **Update password** నొక్కండి.
4. ఒక లాగిన్ వాడటం ఆపడానికి **Disable** నొక్కండి, మళ్లీ వాడటానికి **Enable** నొక్కండి.
5. ఒక కంపెనీని చేర్చడానికి, **Add a login** కింద **Key**, **Company**, **Username**, **Password** నింపి **Add** నొక్కండి.

> Careful: పోర్టల్ పాస్‌వర్డ్ ప్రతి 80 రోజులకు ఎక్స్‌పైర్ అవుతుంది. ముందు పోర్టల్‌లో, తర్వాత ఇక్కడ మార్చండి, లేకపోతే రాత్రి రన్ ఫెయిల్ అవుతుంది.

## సాధారణ సమస్యలు

- **"Enter the new password first."** (ముందు కొత్త పాస్‌వర్డ్ ఎంటర్ చేయండి) **Update password** నొక్కే ముందు కొత్త పాస్‌వర్డ్ పేస్ట్ చేయండి.
- **"Key, username and password are all needed."** (Key, యూజర్ నేమ్, పాస్‌వర్డ్ మూడూ కావాలి) లాగిన్ చేర్చడానికి మూడూ నింపండి.
:::

::: section data-explorer
title: డేటాను నేరుగా చూడటం, సరిచేయడం
routes: /DataExplorer
for: 
roles: SuperAdmin, CorporateAdmin
designations: 
minutes: 3
source-hash: fb55adf5f6eab06e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
అనుమతి ఉన్న వ్యక్తి డేటాబేస్‌లోని ఏ టేబుల్‌లోని వరుసలనైనా చూడటానికి, ఫిల్టర్ చేయడానికి, ఎక్స్‌పోర్ట్ చేయడానికి, సరిచేయడానికి **Data Explorer** స్క్రీన్ సాయం చేస్తుంది. ప్రతి మార్పు మీ పేరుతో audit log లో రాయబడుతుంది. దీనికి వేరే పాస్‌వర్డ్ కావాలి.

1. Data Explorer పాస్‌వర్డ్ ఎంటర్ చేసి **Unlock** నొక్కండి.
2. **Tables** లో, ఒక టేబుల్ వెతికి ఎంచుకోండి. **Filter (any column contains)** లో టైప్ చేసి Enter నొక్కండి. క్రమంలో పెట్టడానికి ఒక కాలమ్ పేరు నొక్కండి.
3. ఒక వరుసను మార్చడానికి దాన్ని నొక్కి, తర్వాత **Save** లేదా **Delete** నొక్కండి. ఒక వరుస చేర్చడానికి **New row**, తర్వాత **Insert** నొక్కండి. **Export CSV** వరుసలను డౌన్‌లోడ్ చేస్తుంది.
4. **Query** లో, ఒక SQL స్టేట్‌మెంట్ టైప్ చేసి **Run** నొక్కండి. మార్పు అయితే ముందు ఒక ట్రయల్ రన్ (dry run) చూపిస్తుంది; దాన్ని అమలు చేయడానికి **Confirm: change ... row(s)** నొక్కండి.
5. చివరి మార్పులు చూడటానికి **Audit log** తెరవండి. పని అయిపోయాక **Lock** నొక్కండి.

> Careful: ఇక్కడ చేసే మార్పులు నేరుగా లైవ్ డేటాలోకి వెళ్తాయి, మళ్లీ వెనక్కి తీసుకోలేరు. వీలైనప్పుడల్లా మామూలు స్క్రీన్‌లే వాడండి.

## సాధారణ సమస్యలు

- **"Data Explorer is available to SuperAdmin or the authorized CorporateAdmin only."** (Data Explorer SuperAdmin కి లేదా అనుమతి ఉన్న CorporateAdmin కి మాత్రమే) మీ లాగిన్‌కు ఈ స్క్రీన్ వాడే అనుమతి లేదు.
- **Save లేదా Delete పని చేయడం లేదు.** key సరిగ్గా ఒకే ఒక్క వరుసతో సరిపోవాలి.
:::
