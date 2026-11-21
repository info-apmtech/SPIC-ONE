---
module: budget-csr
title: బడ్జెట్, CSR
icon: bi-wallet2
order: 110
language: te
category: మార్కెటింగ్
note: కొత్త గైడ్
meta-source-hash: ed22bedc41464619
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 6fab7f9ab7aa88c7
confidence: ok

సంవత్సరపు మార్కెటింగ్ బడ్జెట్‌ను ప్లాన్ చేయండి, దాన్ని రాష్ట్రాలకు, రీజియన్‌లకు, HQ లకు పంచండి, ప్రోగ్రామ్‌ల ద్వారా ఖర్చు చేయండి. ప్రతి ప్రోగ్రామ్‌కు రెండు ఫారాలు ఉంటాయి: ప్రోగ్రామ్‌కు ముందు CSR-1 (ప్లాన్), ప్రోగ్రామ్ తర్వాత CSR-2 (రిపోర్ట్). ప్రతి ఫారాన్ని చెక్ చేసి ఆమోదిస్తారు.

- plan | icon: bi-pie-chart | color: #0891b2
  title: బడ్జెట్ ప్లాన్ చేయండి
  text: హెడ్ ఆఫీస్ సంవత్సరపు బడ్జెట్ నిర్ణయిస్తుంది. అది రాష్ట్రాలకు, రీజియన్‌లకు, HQ లకు పంచబడి, తర్వాత నెల నెలా ప్రోగ్రామ్‌లకు విభజించబడుతుంది.
- csr1 | icon: bi-file-earmark-text | color: #d97706
  title: CSR-1 అంటే ప్లాన్
  text: ప్రోగ్రామ్‌కు ముందు, తేదీ, స్థలం, ముఖ్య పంటలు, ప్లాన్ చేసిన ఖర్చులతో CSR-1 పెట్టండి.
- csr2 | icon: bi-camera | color: #16a34a
  title: CSR-2 అంటే రిపోర్ట్
  text: ప్రోగ్రామ్ అయ్యాక, ఫోటోలు, పాల్గొన్నవారు, అసలు ఖర్చులు, రుజువులతో CSR-2 రిపోర్ట్ పంపండి.
:::

::: section flow
title: బడ్జెట్, ప్రోగ్రామ్ ఎలా ముందుకు వెళ్తాయి, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: a83847a70449e86b
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- office | type: lane | color: #0891b2 | roles: Admin, CorporateAdmin
  title: హెడ్ ఆఫీస్
  sub: Admin
- field | type: lane | color: #d97706 | roles: MDO, JMDO, MO, RMD, SMD
  title: ఫీల్డ్ ఆఫీసర్లు
  sub: MDO · MO · RMDO · SMDO
- approvers | type: lane | color: #0d9488 | roles: RM, SMM
  title: ఆమోదించేవారు
  sub: RM · SMM
- md | type: lane | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: సీనియర్ ఆమోదించేవారు
  sub: MD Admin · MD HOD · AVP
- budget | lane: office | col: 1 | icon: bi-wallet2 | color: #0891b2 | next: plan | screen: state-allocation
  title: బడ్జెట్ నిర్ణయించి పంచడం
  status: Active
  text: Admin సంవత్సరపు బడ్జెట్ ఎంటర్ చేసి రాష్ట్రాలకు పంచుతారు. SMD లేదా SMM రాష్ట్ర బడ్జెట్‌ను రీజియన్‌లకు పంచుతారు, RM లేదా RMDO రీజియన్ బడ్జెట్‌ను HQ లకు పంచుతారు.
- plan | lane: field | col: 2 | icon: bi-calendar3 | color: #64748b | next: plan-ok | screen: program-budget
  title: ప్రోగ్రామ్ బడ్జెట్ ప్లాన్ చేయడం
  status: Submitted
  text: MO, RMDO, SMDO తమ బడ్జెట్‌ను ప్రోగ్రామ్‌లకు, నెలలకు విభజించి, **Submit** నొక్కుతారు.
- plan-ok | lane: approvers | col: 3 | icon: bi-check2-square | color: #2563eb | next: csr1 | screen: program-budget
  title: ప్లాన్‌ను వెరిఫై చేసి ఆమోదించడం
  status: Approved
  text: RMDO లేదా SMDO ప్రోగ్రామ్ బడ్జెట్‌ను వెరిఫై చేస్తారు. తర్వాత RM లేదా SMM దాన్ని ఆమోదిస్తారు.
- csr1 | lane: field | col: 4 | icon: bi-file-earmark-text | color: #d97706 | next: csr1-ok | screen: create-csr1
  title: CSR-1 పెట్టడం
  status: Submitted
  text: ఆమోదం వచ్చిన ప్రోగ్రామ్ కోసం, తేదీ, HQ, స్థలం, బాధ్యత వహించే వ్యక్తి, ముఖ్య పంటలు, ఖర్చులతో CSR-1 ఫారం నింపి, **Submit for Validation** నొక్కండి.
- csr1-ok | lane: approvers | col: 5 | icon: bi-patch-check | color: #2563eb | next: csr2 | screen: csr1-list
  title: CSR-1 ని వెరిఫై చేసి ఆమోదించడం
  status: Approved
  text: HQ ప్లాన్‌లను MO వెరిఫై చేస్తారు, రాష్ట్ర ప్లాన్‌లను SMD వెరిఫై చేస్తారు. తర్వాత RM లేదా SMM ఆమోదిస్తారు. CSR-1 కి ఒక sanction నంబర్ వస్తుంది, అది CSR-2 కి అర్హత పొందుతుంది.
- csr2 | lane: field | col: 6 | icon: bi-camera | color: #16a34a | next: csr2-ok | screen: csr2-list
  title: ప్రోగ్రామ్ నిర్వహించి CSR-2 రిపోర్ట్ పంపడం
  status: Submitted
  text: బాధ్యత వహించే వ్యక్తి ప్రోగ్రామ్ నిర్వహించి, తేదీ, ఫోటోలు, పాల్గొన్నవారు, అసలు ఖర్చులు, రుజువులు నమోదు చేస్తారు.
- csr2-ok | lane: approvers | col: 7 | icon: bi-check2-all | color: #16a34a | next: final | screen: csr2-list
  title: CSR-2 ని వెరిఫై చేసి ఆమోదించడం
  status: Approved
  text: MO లేదా SMD రిపోర్ట్‌ను వెరిఫై చేస్తారు. తర్వాత RM లేదా SMM దాన్ని ఆమోదిస్తారు.
- final | lane: office | col: 8 | icon: bi-clipboard-data | color: #0891b2 | screen: final-report
  title: చివరి రిపోర్ట్
  text: హెడ్ ఆఫీస్ ప్రతి ప్రోగ్రామ్‌ను దాని CSR-1 స్టేటస్, CSR-2 స్టేటస్, ఖర్చులు, మిగిలిన బడ్జెట్‌తో చూస్తుంది.
- late | lane: md | col: 6 | icon: bi-clock-history | color: #dc2626 | kind: exception | from: csr1 | to: csr1-ok | screen: csr1-list
  title: ఆలస్యమైన ఫారాలకు మూడు ఆమోదాలు
  status: Level1Approved
  text: చివరి తేదీ (cut-off) తర్వాత పంపిన CSR-1 లేదా CSR-2 కి మూడు ఆమోదాలు కావాలి: ముందు RM లేదా SMM, తర్వాత MD Admin, తర్వాత MD HOD.
- shortage | lane: md | col: 7 | icon: bi-arrow-left-right | color: #dc2626 | kind: exception | from: csr2-ok | to: csr2-ok | screen: reallocation
  title: బడ్జెట్ సరిపోలేదు
  status: PendingMDHOD
  text: బడ్జెట్ తక్కువగా ఉండటం వల్ల CSR-2 ఆమోదించలేకపోతే, RM, SMM లేదా Admin బడ్జెట్ తిరిగి కేటాయింపు (reallocation) అడుగుతారు. MD HOD చివరి ఆమోదం ఇచ్చాక, ఆమోదాన్ని మళ్లీ ప్రయత్నించవచ్చు.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8d3c0359eda444f2
confidence: ok

- active | color: #16a34a
  label: Active
  text: ఈ సంవత్సరపు బడ్జెట్ రికార్డ్ వాడుకలో ఉంది.
- inactive | color: #64748b
  label: Inactive
  text: ఈ సంవత్సరపు బడ్జెట్ రికార్డ్ ఆపివేయబడింది. దాన్ని మళ్లీ యాక్టివ్ చేయవచ్చు.
- draft | color: #64748b
  label: Draft
  text: సేవ్ అయ్యింది కానీ పంపలేదు. ఇంకా మార్చవచ్చు.
- submitted | color: #d97706
  label: Submitted
  text: పంపబడింది, వెరిఫికేషన్ కోసం ఎదురుచూస్తోంది.
- pending | color: #d97706
  label: Pending
  text: తర్వాతి చెకింగ్ కోసం ఎదురుచూస్తోంది.
- validated | color: #2563eb
  label: Validated
  text: వెరిఫై చేసేవారు చెక్ చేశారు. ఆమోదం కోసం ఎదురుచూస్తోంది.
- level1 | color: #7c3aed
  label: Level1Approved
  text: ఆలస్యమైన ఫారానికి మొదటి ఆమోదం వచ్చింది. MD Admin కోసం ఎదురుచూస్తోంది.
- level2 | color: #7c3aed
  label: Level2Approved
  text: ఆలస్యమైన ఫారానికి రెండో ఆమోదం వచ్చింది. MD HOD కోసం ఎదురుచూస్తోంది.
- approved | color: #16a34a
  label: Approved
  text: పూర్తిగా ఆమోదం వచ్చింది. ఆమోదం వచ్చిన CSR-1 తర్వాత CSR-2 కి వెళ్లవచ్చు.
- rejected | color: #dc2626
  label: Rejected
  text: తిరస్కరించబడింది. తిరస్కరణ కారణం చదవండి.
- pending-smm | color: #d97706
  label: PendingSMM
  text: బడ్జెట్ తిరిగి కేటాయింపు SMM కోసం ఎదురుచూస్తోంది.
- pending-avp | color: #d97706
  label: PendingAVP
  text: బడ్జెట్ తిరిగి కేటాయింపు AVP కోసం ఎదురుచూస్తోంది.
- pending-mdhod | color: #d97706
  label: PendingMDHOD
  text: బడ్జెట్ తిరిగి కేటాయింపు చివరి ఆమోదించేవారైన MD HOD కోసం ఎదురుచూస్తోంది.
- healthy | color: #16a34a
  label: Healthy
  text: HQ దగ్గర సరిపడా బడ్జెట్ మిగిలి ఉంది.
- near-limit | color: #d97706
  label: Near Limit
  text: HQ తన బడ్జెట్‌లో ఎక్కువ భాగం వాడేసింది.
- exceeded | color: #dc2626
  label: Exceeded
  text: HQ తన బడ్జెట్ కంటే ఎక్కువ ఖర్చు చేసింది.
- ext-pending | color: #d97706
  label: Pending Approval
  text: గడువు పొడిగింపు రిక్వెస్ట్ నిర్ణయం కోసం ఎదురుచూస్తోంది.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5e86c95e22cb73c4
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- admin | color: #0891b2 | roles: Admin, CorporateAdmin
  title: Admin
  - ప్రతి ఆర్థిక సంవత్సరానికి సంవత్సరపు బడ్జెట్ ఎంటర్ చేయడం
  - సంవత్సరపు బడ్జెట్‌ను రాష్ట్రాలకు పంచడం
  - ప్రోగ్రామ్ లిస్ట్, sanction నంబర్లు, RMD HQ మ్యాపింగ్ చూసుకోవడం
  - రాష్ట్రాల మధ్య బడ్జెట్ తిరిగి కేటాయింపు అడగడం
- smm | color: #0d9488 | roles: SMM
  title: SMM
  - రాష్ట్ర బడ్జెట్‌ను రీజియన్‌లకు పంచడం
  - ప్రోగ్రామ్ బడ్జెట్‌లు, రాష్ట్ర CSR-1, రాష్ట్ర CSR-2 ఆమోదించడం
  - రీజియన్‌ల మధ్య బడ్జెట్ తిరిగి కేటాయింపు అడగడం
  - RM నుంచి వచ్చే తిరిగి కేటాయింపు రిక్వెస్ట్‌లు ఆమోదించడం
- smd | color: #ea580c | roles: SMD
  title: SMDO
  - ప్రోగ్రామ్ బడ్జెట్‌లు ప్లాన్ చేసి వెరిఫై చేయడం
  - రాష్ట్ర బడ్జెట్‌ను రీజియన్‌లకు పంచడం
  - రాష్ట్ర ప్రోగ్రామ్‌లకు CSR-1 పెట్టడం
  - రాష్ట్ర CSR-1, CSR-2 వెరిఫై చేయడం
- rm | color: #2563eb | roles: RM
  title: RM
  - రీజియన్ బడ్జెట్‌ను HQ లకు పంచడం
  - ప్రోగ్రామ్ బడ్జెట్‌లు, HQ CSR-1, HQ CSR-2 ఆమోదించడం
  - HQ ల మధ్య బడ్జెట్ తిరిగి కేటాయింపు అడగడం
- rmd | color: #9333ea | roles: RMD
  title: RMDO
  - ప్రోగ్రామ్ బడ్జెట్‌లు ప్లాన్ చేసి వెరిఫై చేయడం
  - రీజియన్ బడ్జెట్‌ను HQ లకు పంచడం
  - HQ ప్రోగ్రామ్‌లకు CSR-1 పెట్టడం
- mo | color: #d97706 | roles: MO
  title: MO
  - ప్రోగ్రామ్ బడ్జెట్‌లు ప్లాన్ చేయడం
  - CSR-1 పెట్టడం
  - HQ CSR-1, CSR-2 వెరిఫై చేయడం
- mdo | color: #65a30d | roles: MDO, JMDO
  title: MDO, JMDO
  - HQ ప్రోగ్రామ్‌లకు CSR-1 పెట్టడం
  - మీరు బాధ్యత వహించే వ్యక్తి అయితే, ప్రోగ్రామ్ నిర్వహించి CSR-2 రిపోర్ట్ పంపడం
- senior | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: MD Admin, MD HOD, AVP
  - ఆలస్యమైన ఫారాలకు MD Admin రెండో ఆమోదం, MD HOD మూడో ఆమోదం ఇస్తారు
  - బడ్జెట్ తిరిగి కేటాయింపులను AVP, MD HOD ఆమోదిస్తారు
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 471a0e1b0196bf4a
confidence: ok

- csr2-grey | popular: true
  q: CSR-2 బటన్ బూడిద రంగులో ఉంది
  a: CSR-1 **Approved** అయ్యాకే CSR-2 తెరుచుకుంటుంది. అప్పుడు **CSR-2 Eligibility** కాలమ్‌లో **Eligible for CSR-2** అని చూపిస్తుంది, బటన్ పనిచేస్తుంది.
- one-month | popular: true
  q: CSR-1 Create లో ఒక్క నెల ప్లాన్‌లు మాత్రమే కనిపిస్తున్నాయి
  a: ఇది మామూలే. ఏ నెలో నీలం బాక్స్ చెబుతుంది, ఉదాహరణకు "Showing plans for December 2026 only". CSR-1 చివరి తేదీ (cut-off) వరకు వచ్చే నెల కోసం ప్లాన్ చేస్తారు; ఆ తర్వాత, దాని తర్వాతి నెల కోసం.
- must-equal
  q: మొత్తం కేటాయించిన బడ్జెట్‌కు సమానంగా ఉండాలని Submit చెబుతోంది
  a: **Budgeting Management** లో మీ మొత్తం బడ్జెట్‌ను ప్రోగ్రామ్‌లకు పంచాలి. ఇంకా ఎంత కేటాయించాలో మెసేజ్ చూపిస్తుంది. మొత్తం సరిపోయే వరకు ప్రోగ్రామ్ సంఖ్యలు పెంచండి.
- no-money
  q: బడ్జెట్ తక్కువగా ఉండటం వల్ల CSR-2 ఆమోదించలేకపోతున్నారు
  a: వేరే రాష్ట్రం, రీజియన్ లేదా HQ నుంచి డబ్బు తరలించడానికి RM, SMM లేదా Admin **Budget Reallocations** లో రిక్వెస్ట్ పెడతారు. MD HOD ఆమోదించాక, ఆమోదాన్ని మళ్లీ ప్రయత్నించండి.
- three-levels
  q: నా CSR-1 లేదా CSR-2 కి మూడు ఆమోదాలు ఎందుకు కావాలి?
  a: అది చివరి తేదీ (cut-off) తర్వాత పంపబడింది. ఆలస్యమైన ఫారాలు ముందు RM లేదా SMM కి, తర్వాత MD Admin కి, తర్వాత MD HOD కి వెళ్తాయి.
- save-state-first
  q: "Save this state's allocation above before distributing it to regions." అని కనిపిస్తోంది
  a: ఆ రాష్ట్రానికి ఇంకా బడ్జెట్ లేదు. ఆ ఆర్థిక సంవత్సరానికి రాష్ట్ర కేటాయింపును Admin ముందు సేవ్ చేయాలి.
:::

::: section annual-budget
title: సంవత్సరపు బడ్జెట్ ఎంటర్ చేయడం
routes: /AnnualBudgeting
for: 
roles: 
designations: 
minutes: 2
source-hash: 97c15e0498b487cc
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ప్రతి ఆర్థిక సంవత్సరానికి మొత్తం బడ్జెట్ ఎంటర్ చేయడానికి Admin **Annual Budgeting** స్క్రీన్ వాడతారు. మిగతా అన్ని బడ్జెట్ స్క్రీన్‌లు ఈ మొత్తం నుంచే మొదలవుతాయి.

1. **Add Annual Budgeting** కింద, **Financial Year (FY)** ఎంచుకోండి.
2. **Amount** ఎంటర్ చేయండి.
3. స్విచ్‌ను **Active** మీదే ఉంచండి, లేదా రికార్డ్‌ను **Inactive** చేయడానికి దాన్ని నొక్కండి.
4. **Save** నొక్కండి. ఫారం ఖాళీ చేయడానికి **Cancel** నొక్కండి.
5. **Existing Annual Budgeting Records** లో, FY తో సెర్చ్ చేయండి, లేదా లిస్ట్‌ను ఫిల్టర్ చేయడానికి **Active** లేదా **Inactive** ఎంచుకోండి.
6. రికార్డ్ మార్చడానికి పెన్సిల్ నొక్కి, తర్వాత **Update** నొక్కండి. దాన్ని ఆపడానికి గుండ్రని గీత ఐకాన్, మళ్లీ యాక్టివ్ చేయడానికి బాణం ఐకాన్ నొక్కండి.

> Tip: ప్రతి ఆర్థిక సంవత్సరానికి ఒకే ఒక్క బడ్జెట్ రికార్డ్ ఉంటుంది, అది యాక్టివ్ అయినా కాకపోయినా.

## సాధారణ సమస్యలు

- **"Please select a Financial Year".** (ఆర్థిక సంవత్సరం ఎంచుకోండి) సేవ్ చేసే ముందు ఒక సంవత్సరం ఎంచుకోండి.
- **"Amount must be greater than zero".** (మొత్తం సున్నా కంటే ఎక్కువ ఉండాలి) 0 కంటే ఎక్కువ మొత్తం ఎంటర్ చేయండి.
- **"An Annual Budgeting record for ... already exists".** (ఈ సంవత్సరానికి రికార్డ్ ఇప్పటికే ఉంది) కొత్తది జోడించకుండా, ఉన్న రికార్డ్‌నే మార్చండి.
- **Save బటన్ బూడిద రంగులో ఉంది.** ఇంకా ఏ ఆర్థిక సంవత్సరమూ లేదు. ముందు Financial Master లో ఒకటి జోడించండి.
:::

::: section state-allocation
title: బడ్జెట్‌ను రాష్ట్రాలకు, రీజియన్‌లకు, HQ లకు పంచడం
routes: /StateBudgetManagement
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD
designations: 
tour: 1 > [data-help=bc-sb-tabs] | 2 > [data-help=bc-sb-select] | 3 > [data-help=bc-sb-cards] | 4 > [data-help=bc-sb-table] | 5 > [data-help=bc-sb-save]
minutes: 3
source-hash: 535c04c3b89862f4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**State Budgeting Management** స్క్రీన్ సంవత్సరపు బడ్జెట్‌ను ఒక్కో స్థాయి చొప్పున కిందికి పంచుతుంది. Admin దాన్ని రాష్ట్రాలకు పంచుతారు, SMD లేదా SMM ఒక రాష్ట్ర బడ్జెట్‌ను దాని రీజియన్‌లకు పంచుతారు, RM లేదా RMDO ఒక రీజియన్ బడ్జెట్‌ను దాని HQ లకు పంచుతారు.

1. మీ రోల్‌కు తగ్గ ట్యాబ్ తెరుచుకుంటుంది: **State Allocation** (Admin), **Region Allocation** (SMD, SMM) లేదా **Headquarters Allocation** (RM, RMDO).
2. **Financial Year** ఎంచుకోండి. రీజియన్ ట్యాబ్‌లో **State** కూడా ఎంచుకోండి. HQ ట్యాబ్‌లో **State**, **Region** ఎంచుకోండి.
3. **Annual Budget**, **Allocated Budget**, **Remaining Budget**, లేదా **State Allocated Budget**, **State MDO Budget** లాంటి కార్డులు చెక్ చేయండి.
4. టేబుల్‌లో, ప్రతి రాష్ట్రం, రీజియన్ లేదా HQ కి మొత్తం టైప్ చేయండి. రీజియన్, HQ ట్యాబ్‌లలో, ముందు పైన ఉన్న **State MDO Amount** లేదా **Region MDO Amount** ఎంటర్ చేయండి.
5. సేవ్ చేయండి. రాష్ట్ర ట్యాబ్‌లో **Save Draft** నొక్కి, తర్వాత **Submit For Validation** నొక్కండి. మిగతా ట్యాబ్‌లలో **Save Region Allocation** లేదా **Save Headquarters Allocation** నొక్కండి.

> Careful: రాష్ట్ర ట్యాబ్‌లో, సేవ్ చేసే ముందు మొత్తం సంవత్సరపు బడ్జెట్ పంచి ఉండాలి.

## సాధారణ సమస్యలు

- **"All budget must be allocated to states."** (మొత్తం బడ్జెట్ రాష్ట్రాలకు కేటాయించాలి) ఇంకా మిగిలిన మొత్తాన్ని మెసేజ్ చూపిస్తుంది. దాన్ని రాష్ట్రాలకు పంచండి.
- **"Region allocation cannot exceed the remaining state budget"** లేదా **"Headquarters allocation cannot exceed the remaining region budget".** (కేటాయింపు మిగిలిన బడ్జెట్ కంటే ఎక్కువ ఉండకూడదు) మొత్తం తగ్గించండి.
- **"Save this region's allocation above before distributing it to headquarters."** (HQ లకు పంచే ముందు ఈ రీజియన్ కేటాయింపు సేవ్ చేయండి) ఆ రీజియన్‌కు ఇంకా బడ్జెట్ లేదు. SMD లేదా SMM ముందు రీజియన్ కేటాయింపు సేవ్ చేయాలి.
- **"You do not have access to any budget allocation level."** (ఏ బడ్జెట్ కేటాయింపు స్థాయికీ మీకు యాక్సెస్ లేదు) మీ రోల్ బడ్జెట్ పంచదు. మీ అడ్మినిస్ట్రేటర్‌ను అడగండి.
:::

::: section program-budget
title: ప్రోగ్రామ్ బడ్జెట్‌లు ప్లాన్ చేయడం, వెరిఫై చేయడం, ఆమోదించడం
routes: /BudgetingManagements
for: 
roles: 
designations: 
minutes: 4
source-hash: 9d6de3d0ef847b35
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Budgeting Management** లో, MO, RMDO, SMDO తమ బడ్జెట్‌ను ప్రోగ్రామ్‌లకు, నెలలకు విభజిస్తారు. సేవ్ చేసిన ప్లాన్‌లను RMDO, SMDO వెరిఫై చేస్తారు, RM, SMM ఆమోదిస్తారు.

1. **Program-wise Budget Planning** పైన ఆర్థిక సంవత్సరం ఎంచుకోండి.
2. కార్డులు చెక్ చేయండి: మీ బడ్జెట్ (ఉదాహరణకు **Available Budget**), **Allocated Budget**, **Remaining Budget**, **SMDO Budget**.
3. **Budget plan list** లో, **April** నుంచి **March** వరకు ప్రతి నెలా ఎన్ని ప్రోగ్రామ్‌లు నిర్వహిస్తారో ఎంటర్ చేయండి. ప్రోగ్రామ్ అనుమతిస్తే, దాని **Budget Amount** మార్చవచ్చు. ప్రతి వరుస **Total Budget** తనంతట అదే మారుతుంది.
4. **Submit** నొక్కండి. మొత్తం బడ్జెట్ కేటాయించి ఉండాలి.
5. వెరిఫై చేయడానికి (RMDO లేదా SMDO), **Validate Budget** నొక్కి, **Saved program budgets awaiting validation** లో ఒక ప్లాన్ ఎంచుకుని, అవసరమైతే సంఖ్యలు సరిచేసి, **Validate** నొక్కండి. మీ సొంత ప్లాన్‌కు తిరిగి వెళ్లడానికి **Create Budget** నొక్కండి.
6. ఆమోదించడానికి (RM లేదా SMM), **Validated program budgets awaiting approval** లో ఒక ప్లాన్ ఎంచుకుని, అవసరమైతే సంఖ్యలు సరిచేసి, **Approve** నొక్కండి.
7. టేబుల్‌ను షేర్ చేయడానికి **Copy**, **CSV**, **Excel**, **PDF** లేదా **Print** వాడండి.

## సాధారణ సమస్యలు

- **"Total allocated budget ... must equal allotted budget ..."** (మొత్తం కేటాయింపు ఇచ్చిన బడ్జెట్‌కు సమానంగా ఉండాలి) ఇంకా కొంత బడ్జెట్ మిగిలి ఉంది. ఏమీ మిగలని వరకు ప్రోగ్రామ్ సంఖ్యలు పెంచండి.
- **"... exceeds allotted budget ..."** (ఇచ్చిన బడ్జెట్ కంటే ఎక్కువ) మీరు ఎక్కువ ప్లాన్ చేశారు. సంఖ్యలు తగ్గించండి.
- **"No eligible budgets found for your assigned scope and selected FY."** (మీ పరిధికి, ఎంచుకున్న సంవత్సరానికి బడ్జెట్ ఏదీ లేదు) ఈ సంవత్సరంలో మీ కోసం ఏదీ ఎదురుచూడటం లేదు. సంవత్సరం చెక్ చేయండి.
- **"Only MO, RMDO and SMDO can save program budgets."** (MO, RMDO, SMDO మాత్రమే ప్రోగ్రామ్ బడ్జెట్ సేవ్ చేయగలరు) మీ రోల్ చూడటం, వెరిఫై చేయడం లేదా ఆమోదించడం మాత్రమే చేయగలదు.
:::

::: section budget-submissions
title: నేను పంపిన బడ్జెట్‌లను ట్రాక్ చేయడం
routes: /BudgetSubmissions
for: 
roles: 
designations: 
minutes: 2
source-hash: 3e92ca02097adb1c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Budget Submissions** స్క్రీన్ మీరు పంపిన ప్రోగ్రామ్ బడ్జెట్‌ల లిస్ట్ చూపిస్తుంది, ప్రతిదీ ఇప్పుడు ఎక్కడ ఉందో కూడా చూపిస్తుంది.

1. కార్డులు చూడండి: **Program Budget**, **Approved**, **Pending validation**, **Rejected**, **Drafts**.
2. **Date Range**, **Program Type**, **Program Name** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
3. ప్రోగ్రామ్‌ను త్వరగా కనుక్కోవడానికి **Search Program...** లో టైప్ చేయండి.
4. టేబుల్‌లో **Program Budget**, **Submitted Date**, **Status**, **Validation Due** చెక్ చేయండి.
5. వివరాలు చూడటానికి కన్ను ఐకాన్ నొక్కండి. అక్కడి నుంచి **Print** లేదా **Export PDF** చేయవచ్చు.

## సాధారణ సమస్యలు

- **"No budget submission records found."** (పంపిన బడ్జెట్ రికార్డ్‌లు ఏవీ లేవు) మీరు ఇంకా ప్లాన్ పంపలేదు, లేదా ఫిల్టర్‌లు మరీ ఎక్కువగా ఉన్నాయి. **Clear All** నొక్కండి.
:::

::: section hq-budget-overview
title: HQ బడ్జెట్ వాడకం చూడటం
routes: /BudgetOverview
for: 
roles: 
designations: 
minutes: 1
source-hash: 6911fbefa137aa04
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**HQ Budget Allocation** స్క్రీన్ ఒక రీజియన్ బడ్జెట్ దాని HQ లకు ఎలా పంచబడిందో, ప్రతి HQ ఎంత వాడిందో చూపిస్తుంది.

1. కార్డులు చూడండి: **Total Regional Budget**, **Allocated to HQs**, **Remaining Budget**, **Utilizes Budget**.
2. **Budget plan list** లో, ప్రతి HQ యొక్క **Allocated**, **Utilized**, **Remaining**, **Utilization %**, దాని **Pending CSR-1**, **Pending CSR-2** చెక్ చేయండి.
3. **Status** లో **Healthy**, **Near Limit** లేదా **Exceeded** అని చూపిస్తుంది.
4. HQ బడ్జెట్ వాడకం తెరవడానికి కన్ను ఐకాన్ నొక్కండి.
5. **Budgeting Management** కి వెళ్లడానికి **Allocate Budget** నొక్కండి.

## సాధారణ సమస్యలు

- **సంఖ్యలు మారడం లేదు.** ఈ స్క్రీన్ ఇంకా తయారవుతోంది. అసలు లెక్కల కోసం **Budgeting Management**, **State Budgeting Management** వాడండి.
:::

::: section reallocation
title: తిరిగి కేటాయింపుతో బడ్జెట్ తరలించడం
routes: /BudgetReallocations
for: 
roles: 
designations: 
minutes: 3
source-hash: 61df1dfc270033ec
confidence: ok

**ఈ స్క్రీన్ దేనికి**
బడ్జెట్ తక్కువగా ఉండటం వల్ల CSR-2 ఆమోదించలేకపోతే, **Budget Reallocations** స్క్రీన్ అదే స్థాయిలో ఉన్న ఒక బడ్జెట్ నుంచి మరో బడ్జెట్‌కు డబ్బు తరలిస్తుంది. RM, HQ ల మధ్య డబ్బు తరలిస్తారు, SMM రీజియన్‌ల మధ్య, Admin రాష్ట్రాల మధ్య.

1. **New Reallocation Request** కింద, **Initiated by**, స్థాయి బ్యాడ్జ్ చెక్ చేయండి.
2. **Financial Year** ఎంచుకోండి. అడిగితే, **State**, **Region** ఎంచుకోండి.
3. **From** (డబ్బు ఇచ్చే బడ్జెట్), **To** (డబ్బు కావాల్సిన బడ్జెట్) ఎంచుకోండి. **Source Available Budget**, **Selected destination** చెక్ చేయండి.
4. **Reallocation Amount (₹)** ఎంటర్ చేసి, **Reason for Reallocation** రాయండి.
5. **Approval route** చెక్ చేయండి, ఉదాహరణకు RM, SMM, AVP, MD HOD. **Apply Reallocation** నొక్కండి.
6. ఆమోదించేవారు: **Reallocation Requests** లో, **Approval / Rejection Remarks** రాసి, ఆ వరుసలో **Approve** లేదా **Reject** నొక్కండి.
7. తాజా స్టేటస్ చూడటానికి **Refresh** నొక్కండి.

## సాధారణ సమస్యలు

- **"Source budget shortage".** (డబ్బు ఇచ్చే బడ్జెట్‌లో సరిపడా లేదు) దానిలో మిగిలిన దానికంటే మొత్తం ఎక్కువ. దాన్ని తగ్గించండి, లేదా వేరే source ఎంచుకోండి.
- **"No eligible budgets found."** (అర్హత ఉన్న బడ్జెట్ ఏదీ లేదు) సంవత్సరం, అలాగే మీ రాష్ట్రం లేదా రీజియన్ కేటాయింపులు ఆమోదం పొందాయో లేదో చెక్ చేయండి.
- **"Apply permissions are unavailable for this login."** (ఈ లాగిన్‌కు రిక్వెస్ట్ పెట్టే అనుమతి లేదు) RM, SMM, Admin మాత్రమే తిరిగి కేటాయింపు అడగగలరు.
:::

::: section program-master
title: ప్రోగ్రామ్ లిస్ట్ చూసుకోవడం
routes: /ProgramMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 800f0d1d4704db06
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Program Master** స్క్రీన్ ప్రోగ్రామ్ రకాలు, ప్రోగ్రామ్‌ల లిస్ట్, వాటిని ఎవరు ప్లాన్ చేయవచ్చు, ప్రతి రాష్ట్రంలో ప్రోగ్రామ్ మొత్తం ఎంత అనేవి ఉంచుతుంది.

1. **Program Types** తెరవండి. **Program Type Name** ఎంటర్ చేసి, **Is Amount Change** ను **Allowed** లేదా **Not Allowed** గా పెట్టి, **Save** నొక్కండి.
2. **Program Master** తెరవండి. **Program Type** ఎంచుకుని, **Program Name** ఎంటర్ చేసి, **MO**, **RMDO**, **SMDO** ను **Eligible** లేదా **Not Eligible** గా పెట్టండి. **Save** నొక్కండి.
3. ఒక వరుసను మార్చడానికి దానిపై పెన్సిల్ నొక్కండి.
4. **State Mapping** తెరవండి (లేదా ప్రోగ్రామ్‌పై మ్యాప్ పిన్ నొక్కండి). **Program** ఎంచుకోండి.
5. ప్రతి రాష్ట్రానికి, **Applicable** ను ఆన్ లేదా ఆఫ్ చేయండి. వర్తించే రాష్ట్రానికి, **Program Amount (₹)** ఎంటర్ చేసి ఆ వరుసలో **Save** నొక్కండి.

> Tip: **Budgeting Management** లో ప్లాన్ చేసేవారు ప్రోగ్రామ్ బడ్జెట్ మొత్తాన్ని మార్చవచ్చో లేదో **Is Amount Change** నిర్ణయిస్తుంది.

## సాధారణ సమస్యలు

- **"View only - you do not have update permission."** (చూడటం మాత్రమే, మార్చే అనుమతి లేదు) మార్చే హక్కుల కోసం మీ అడ్మినిస్ట్రేటర్‌ను అడగండి.
:::

::: section rmd-hq-mapping
title: RMDO కి HQ లు ఇవ్వడం
routes: /RMDHQMapping
for: 
roles: 
designations: 
minutes: 2
source-hash: 1c5c04d12fa1b412
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**RMD Headquarters Mapping** స్క్రీన్ ఒక RMDO ని ఒకటి లేదా ఎక్కువ HQ లతో ఒకే రికార్డ్‌లో కలుపుతుంది.

1. **Assign Headquarters** కింద, **Employee (RMD)** ఎంచుకోండి.
2. **State** ఎంచుకుని, తర్వాత **Region** ఎంచుకోండి.
3. **Select Headquarters** నొక్కి HQ లను టిక్ చేయండి, లేదా **Select All**. **Done** నొక్కండి.
4. **Save Mapping** నొక్కండి. **Clear** నొక్కితే ఫారం ఖాళీ అవుతుంది.
5. **Existing RMD HQ Mappings** లో, మార్చడానికి పెన్సిల్ నొక్కండి (తర్వాత **Update Mapping**), డిలీట్ చేయడానికి క్రాస్ నొక్కండి (తర్వాత **Confirm**).

## సాధారణ సమస్యలు

- **"No headquarters found."** (HQ ఏదీ లేదు) ముందు రీజియన్ ఎంచుకోండి, లేదా ఆ రీజియన్‌లో HQ లు లేవు.
:::

::: section sanction-sequence
title: Sanction నంబర్ల మొదటి విలువ పెట్టడం
routes: /SanctionNumberSequence
for: 
roles: 
designations: 
minutes: 2
source-hash: 7e920cca79cfe7a9
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఆమోదం వచ్చిన CSR-1 కి ఒక sanction నంబర్ వస్తుంది. **Sanction Number Sequence** స్క్రీన్ ప్రతి రాష్ట్రానికి లేదా రీజియన్‌కు ఈ నంబర్ల మొదటి విలువను పెడుతుంది.

1. **Sequence Scope** ఎంచుకోండి: **State (SMM)** లేదా **Region (RM / RMM)**.
2. **State** ఎంచుకోండి, రీజియన్ scope అయితే **Region** కూడా ఎంచుకోండి. **State Code**, **Region Code** మాస్టర్‌ల నుంచి తనంతట అవే నిండుతాయి.
3. మొదటి విలువ ఎంటర్ చేయండి.
4. **Save Sequence** నొక్కండి. **Clear** నొక్కితే ఫారం ఖాళీ అవుతుంది.
5. **Existing Sanction Number Sequences** లిస్ట్ చెక్ చేయండి. రాష్ట్రం, రీజియన్ లేదా కోడ్‌తో సెర్చ్ చేయవచ్చు.

> Careful: ఒక sequence తయారు చేశాక దాన్ని మార్చలేరు.

## సాధారణ సమస్యలు

- **"Code not configured in State master".** (State master లో కోడ్ సెట్ చేయలేదు) ముందు దాని మాస్టర్‌లో రాష్ట్రం లేదా రీజియన్ కోడ్ జోడించండి.
:::

::: section create-csr1
title: ప్రోగ్రామ్ కోసం CSR-1 పెట్టడం
routes: /CREATE-CSR-1Management
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c1-banner] | 2 > [data-help=bc-c1-search] | 3 > [data-help=bc-c1-plan] | 4 > [data-help=bc-c1-focus] | 5 > [data-help=bc-c1-expense] | 6 > [data-help=bc-c1-actions]
minutes: 5
source-hash: 640ab3b5f3b1dedd
confidence: ok

**ఈ స్క్రీన్ దేనికి**
CSR-1 అంటే ఒక ప్రోగ్రామ్‌కు ముందు వేసే ప్లాన్. **CSR-1 Program - Wise Budget Allocation** స్క్రీన్ ఆమోదం వచ్చిన మీ ప్రోగ్రామ్ బడ్జెట్‌లను చూపిస్తుంది. ఒకటి ఎంచుకుని CSR-1 ఫారం నింపండి.

1. నీలం బాక్స్ చదవండి. మీరు ఏ నెల కోసం ప్లాన్ చేస్తున్నారో అది చెబుతుంది.
2. **Budget plan list** లో ప్రోగ్రామ్ కనుక్కుని (**Search Program...** లేదా **Filters** వాడండి), **Remaining Programs** చెక్ చేసి, **Apply CSR 1** నొక్కండి.
3. **CSR-1 Plan Details** కింద, ప్రోగ్రామ్ చెక్ చేసి **Budget**, **HQ**, **Location**, **Responsible Person**, **Date** నింపండి.
4. **Focus Details** కింద, కనీసం ఒక **Focused Crop**, దానికి ఒక **Focused Product** ఎంచుకోండి. ప్రతి బాక్స్‌లో మూడు పంటలు, మూడు ప్రొడక్ట్‌ల వరకు జోడించవచ్చు.
5. **Expense Breakdown** కింద, ప్రతి ఖర్చుకు ప్లాన్ చేసిన మొత్తం ఎంటర్ చేయండి. **Total Expenses** బడ్జెట్‌లోపే ఉండాలి.
6. తర్వాత పూర్తి చేయడానికి **Save Draft** నొక్కండి, లేదా పంపడానికి **Submit for Validation** నొక్కండి.

> Tip: **Responsible Person** నే ప్రోగ్రామ్ నిర్వహించి CSR-2 రిపోర్ట్ పంపుతారు.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Budget** | ఈ ప్రోగ్రామ్ కోసం డబ్బు. తప్పనిసరి. |
| **HQ** మరియు **Location** | ప్రోగ్రామ్ ఎక్కడ జరుగుతుంది. తప్పనిసరి. |
| **Responsible Person** | దాన్ని నిర్వహించే ఆఫీసర్. తప్పనిసరి. |
| **Date** | ప్రోగ్రామ్ తేదీ, ప్లాన్ చేసిన నెలలోపే. తప్పనిసరి. |

## సాధారణ సమస్యలు

- **"Budget Exceeded"** లేదా **"Total Expense Breakdown ... exceeds the allocated Budget".** (ఖర్చు కేటాయించిన బడ్జెట్ కంటే ఎక్కువ) ఖర్చు మొత్తాలు తగ్గించండి.
- **"Please enter at least one expense amount."** (కనీసం ఒక ఖర్చు మొత్తం ఎంటర్ చేయండి) కనీసం ఒక ప్లాన్ చేసిన ఖర్చు ఎంటర్ చేయండి.
- **"Please select at least one Product for Focus Crop 1."** (Focus Crop 1 కి కనీసం ఒక Product ఎంచుకోండి) మీరు ఎంచుకున్న ప్రతి పంటకు ఒక ప్రొడక్ట్ ఎంచుకోండి.
- **"CSR-1 creation is not available for Admin role."** (Admin కి CSR-1 తయారు చేసే వీలు లేదు) Admin, **CSR-1 Management** లో CSR-1 చూడగలరు మాత్రమే.
:::

::: section csr1-list
title: CSR-1 ట్రాక్ చేయడం, వెరిఫై చేయడం, ఆమోదించడం
routes: /CSR-1List
for: 
roles: 
designations: 
minutes: 3
source-hash: 33a5bb3a053b06a1
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**CSR-1 Management** లో CSR-1 ప్లాన్‌ల లిస్ట్ ఉంటుంది. ఫీల్డ్ ఆఫీసర్లు తమవి ట్రాక్ చేస్తారు. MO, SMD వాటిని వెరిఫై చేస్తారు, RM, SMM ఆమోదిస్తారు.

1. కార్డులు చూడండి: **Total CSR -1**, **Approved**, **Pending**, **Rejected**, **Drafts**.
2. **Filters** కింద, **From Date**, **To Date**, **Program Type**, **Program Name** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి.
3. **CSR-1 plan list** లో, **Status**, **Sanction Number**, **CSR-2 Eligibility** చెక్ చేయండి. **State** లేదా **HQ** ట్యాగ్ ప్లాన్ ఏ స్థాయిదో చూపిస్తుంది.
4. **CSR-1 Program Details** తెరవడానికి కన్ను ఐకాన్ నొక్కండి.
5. అది మీ కోసం ఎదురుచూస్తుంటే, **Validate** నొక్కండి, లేదా ఆమోదం బటన్ నొక్కండి (**Approve**, లేదా ఆలస్యమైన ప్లాన్ అయితే స్థాయి పేరు). తిరస్కరించడానికి, **Reject** నొక్కి, రిమార్క్స్ రాసి, **Confirm Reject** నొక్కండి.
6. ప్లాన్ **Approved** అయ్యాక, ప్రోగ్రామ్ రిపోర్ట్ పంపడానికి వరుసలోని **CSR-2** నొక్కండి, లేదా వివరాలలోని **Open CSR-2** నొక్కండి.

> Careful: బిన్ ఐకాన్ మళ్లీ అడగకుండా CSR-1 ని వెంటనే డిలీట్ చేస్తుంది.

## సాధారణ సమస్యలు

- **వివరాలలో "Awaiting MO Validation" లేదా "Awaiting SMD Validation" అని కనిపిస్తోంది.** ఎవరైనా ఆమోదించే ముందు ప్లాన్ వెరిఫై అవ్వాలి.
- **CSR-2 బటన్ బూడిద రంగులో ఉంది.** CSR-1 ఇంకా **Approved** కాలేదు.
- **ఎరుపు రంగు "Rejection Reason" బాక్స్ కనిపిస్తోంది.** లిస్ట్ కింద ఉన్న కారణం చదవండి.
:::

::: section csr2-list
title: CSR-2 రిపోర్ట్ పంపడం, వెరిఫై చేయడం, ఆమోదించడం
routes: /CSR-2List
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c2-cards] | 2 > [data-help=bc-c2-filters] | 3 > [data-help=bc-c2-table] | 4 > [data-help=bc-c2-table] | 5 > [data-help=bc-c2-table]
minutes: 3
source-hash: 3490f090fad6d820
confidence: ok

**ఈ స్క్రీన్ దేనికి**
CSR-2 అంటే ప్రోగ్రామ్ జరిగిన తర్వాత పంపే రిపోర్ట్. **CSR-2 Program Execution** లిస్ట్ మీ ప్రోగ్రామ్‌లను హాజరు, మీడియా, ఖర్చులు, ప్రోగ్రెస్‌తో చూపిస్తుంది.

1. కార్డులు చూడండి: **Total CSR-2**, **Approved**, **Submitted**, **Drafts**. ఒక కార్డు నొక్కితే ఆ ప్రోగ్రామ్‌లు మాత్రమే కనిపిస్తాయి.
2. **Filters** కింద, తేదీలు, **Program Type**, **Program Name**, **Location** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
3. **Assigned Programs** లో, **Approved Budget**, **Attendance**, **Media**, **Actual Expense**, **Report Completion**, **Status** చెక్ చేయండి.
4. CSR-2 ఫారం తెరవడానికి **View Details** నొక్కండి. ప్రోగ్రామ్ తేదీ, ఫోటోలు, పాల్గొన్నవారు, ప్రతి అసలు ఖర్చు దాని రుజువుతో నింపి, పంపండి.
5. ఒక రిపోర్ట్ మీ కోసం ఎదురుచూస్తుంటే, **Validate** (MO లేదా SMD) నొక్కండి, లేదా **Approve CSR-2** లేదా **Approve Level 1** లాంటి ఆమోదం బటన్ నొక్కండి.

> Tip: రిపోర్ట్ **Draft** నుంచి **Approved** వైపు వెళ్లే కొద్దీ **Report Completion** పెరుగుతుంది.

## సాధారణ సమస్యలు

- **"No CSR-2 records found."** (CSR-2 రికార్డ్‌లు ఏవీ లేవు) ఆమోదం వచ్చిన ఏ CSR-1 మీకు కేటాయించబడలేదు, లేదా ఫిల్టర్‌లు మరీ ఎక్కువగా ఉన్నాయి.
- **"You are not authorized to validate this CSR-2."** (ఈ CSR-2 ని వెరిఫై చేసే అనుమతి మీకు లేదు) వేరే రోల్ ఉన్నవారు దాన్ని వెరిఫై చేయాలి.
- **బడ్జెట్ వల్ల ఆమోదం విఫలమవుతోంది.** RM, SMM లేదా Admin ను బడ్జెట్ తిరిగి కేటాయింపు అడగండి.
:::

::: section csr2-assigned
title: నాకు కేటాయించిన ప్రోగ్రామ్‌లు తెరవడం
routes: /CSR-2ProgramExecution
for: 
roles: 
designations: 
minutes: 1
source-hash: 6d55229f3d01c986
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఈ **CSR-2 Program Execution** స్క్రీన్ మీకు కేటాయించిన, ఆమోదం వచ్చిన CSR-1 ప్రోగ్రామ్‌లను చూపిస్తుంది. కాబట్టి వాటి CSR-2 రిపోర్ట్‌లు మొదలుపెట్టవచ్చు.

1. పైన ఉన్న **Total CSR -2** చూడండి.
2. **Assigned Programs** లో, ప్రోగ్రామ్‌ను దాని **Sanction Number**, **Program Type** లేదా **Program** తో కనుక్కోండి.
3. **Approved Budget**, **Status** చెక్ చేయండి.
4. ఆ ప్రోగ్రామ్ CSR-2 ఫారం తెరవడానికి **View Program** నొక్కండి.
5. తాజా లిస్ట్ కోసం **Refresh Data** నొక్కండి.

## సాధారణ సమస్యలు

- **"No approved CSR-1 programs found."** (ఆమోదం వచ్చిన CSR-1 ప్రోగ్రామ్‌లు ఏవీ లేవు) మీ CSR-1 ప్లాన్‌లలో దేనికీ ఇంకా ఆమోదం రాలేదు.
:::

::: section csr2-form-preview
title: CSR-2 Management ఫారం
routes: /CSR2, /csrplandetails2
for: 
roles: 
designations: 
minutes: 1
source-hash: 7593c2c21789d74d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**CSR-2 Management** స్క్రీన్ CSR-2 రిపోర్ట్ భాగాలను చూపిస్తుంది: బడ్జెట్ కార్డులు, ప్లాన్ వివరాలు, ముఖ్య పంటల వివరాలు, రుజువులతో ఖర్చుల విభజన. అసలు ప్రోగ్రామ్ రిపోర్ట్ పంపడానికి, **CSR-2 List** తెరిచి **View Details** నొక్కండి.

1. కార్డులు చెక్ చేయండి: **Approved Budget**, **Actual Expense**, **Difference**.
2. **CSR-2 Plan Details** చూడండి: ప్రోగ్రామ్, ప్రోగ్రామ్‌ల సంఖ్య, బడ్జెట్, HQ, స్థలం.
3. **Focus Details** చూడండి: ముఖ్య పంటలు, ప్రొడక్ట్‌లు.
4. **Expense Breakdown** చూడండి: ప్రతి ఖర్చుకు ఒక మొత్తం, ఒక **Date**, **Upload proof** ఉంటాయి.

## సాధారణ సమస్యలు

- **ఇక్కడ Save Draft లేదా Submit CSR-2 ఏమీ చేయడం లేదు.** రిపోర్ట్ పంపడానికి **CSR-2 List**, **View Details** వాడండి.
:::

::: section final-report
title: చివరి CSR రిపోర్ట్ చూడటం
routes: /FinalReportCSRView
for: 
roles: 
designations: 
minutes: 1
source-hash: d8668b0d71867c79
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Final Report - CSR Program Tracking** ప్రతి ప్రోగ్రామ్‌ను ప్లాన్ నుంచి రిపోర్ట్ వరకు చూపిస్తుంది: CSR-1 స్టేటస్, CSR-2 స్టేటస్, sanction నంబర్, ఖర్చులు, మిగిలిన బడ్జెట్.

1. కార్డులు చూడండి: **Total Programs**, **Approved**, **Pending**, **Rejected**, **Drafts**.
2. తేదీలు, **Program Type**, **Program Name**, **CSR Type** లేదా **Status** ఎంచుకోవడానికి **Filters** వాడి, **Apply Filters** నొక్కండి.
3. **CSR-2 plan list** లో, **Total Expenses**, **Approved Budget**, **Remaining Budget** పోల్చి చూడండి. **Report Status** లో **Completed** లేదా **Pending** అని చూపిస్తుంది.
4. ప్రోగ్రామ్ వివరాలు చూడటానికి కన్ను ఐకాన్ నొక్కి, తర్వాత **Print** లేదా **Export PDF** చేయండి.

## సాధారణ సమస్యలు

- **సంఖ్యలు మారడం లేదు.** ఈ స్క్రీన్ ఇంకా తయారవుతోంది. అసలు రికార్డ్‌ల కోసం **CSR-1 Management**, **CSR-2 List** వాడండి.
:::

::: section rmd-queue
title: RMD వెరిఫికేషన్ క్యూ
routes: /RMDValidationQueue
for: 
roles: 
designations: 
minutes: 2
source-hash: 641c748d235d2f14
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**RMD Validation Queue** లో Market Officer ల నుంచి వచ్చిన CSR-1, CSR-2 సబ్మిషన్‌ల లిస్ట్ ఉంటుంది.

1. ట్యాబ్ ఎంచుకోండి: **CSR-1 Submissions** లేదా **CSR-2 Reports**. ఎన్ని ఉన్నాయో బ్యాడ్జ్ చూపిస్తుంది.
2. **Date Range**, **Program Type**, **Head Quarters** లేదా **Status** ఎంచుకోవడానికి **Filters** తెరిచి, **Apply Filters** నొక్కండి.
3. ఒక సబ్మిషన్ కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో టైప్ చేయండి.
4. **MO Name**, **Program Name**, **Head Quarters**, **Total Budget**, **Validation Due**, **Status** చెక్ చేయండి.
5. తాజా లిస్ట్ కోసం **Refresh Data** నొక్కండి.

## సాధారణ సమస్యలు

- **"Failed to load data".** (డేటా లోడ్ కాలేదు) మీ ఇంటర్నెట్ కనెక్షన్ చెక్ చేసి **Try Again** నొక్కండి.
- **"No submissions found".** (సబ్మిషన్‌లు ఏవీ లేవు) Filters లో **Clear All** నొక్కండి.
:::

::: section mo-validation
title: MO సబ్మిషన్ వెరిఫికేషన్
routes: /MOSubmissionValidation
for: 
roles: 
designations: 
minutes: 1
source-hash: fbb06076726e4306
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**MO Submission Validation** స్క్రీన్ Market Officer ల నుంచి వచ్చిన CSR-1, CSR-2 సబ్మిషన్‌లను, ప్లాన్ చేసిన, అసలు బడ్జెట్‌లతో చూపిస్తుంది.

1. ట్యాబ్ ఎంచుకోండి: **CSR-1 Submissions** లేదా **CSR-2 Reports**.
2. **Date Range**, **Program Type**, **Head Quatres** లేదా **Status** ఎంచుకోవడానికి **Filters** వాడండి.
3. **CSR-2 Expense Report Validation List** లో, **Planned Budget**, **Actual Budget**, **Variance** పోల్చి చూడండి.
4. **Validation Due**, **CSR - 1 Status** చెక్ చేయండి.

## సాధారణ సమస్యలు

- **లిస్ట్ మారడం లేదు.** ఈ స్క్రీన్ ఇంకా తయారవుతోంది. CSR-1 ని **CSR-1 Management** లో, CSR-2 ని **CSR-2 List** లో వెరిఫై చేయండి.
:::

::: section rm-approval-status
title: RM ఆమోదం స్టేటస్
routes: /RMApprovalStatus
for: 
roles: 
designations: 
minutes: 1
source-hash: dd292461c2960406
confidence: ok

**ఈ స్క్రీన్ దేనికి**
RMD వెరిఫై చేసి RM కి పంపిన CSR-1, CSR-2 సబ్మిషన్‌లను **RM Approval Status** స్క్రీన్ ట్రాక్ చేస్తుంది.

1. ట్యాబ్ ఎంచుకోండి: **CSR-1 Submissions** లేదా **CSR-2 Report**.
2. కార్డులు చూడండి: **Sent to RM**, **Pending RM Approval**, **Approved by RM**, **Rejected / Correction**.
3. **CSR-1 RM Approval Status List** లో, **Mo Submitted**, **RMD Validated**, **Status** చెక్ చేయండి.
4. ప్రోగ్రామ్ వివరాలు, **Process Timeline** చూడటానికి కన్ను ఐకాన్ నొక్కండి.

## సాధారణ సమస్యలు

- **లిస్ట్ మారడం లేదు.** ఈ స్క్రీన్ ఇంకా తయారవుతోంది. CSR-1 ని **CSR-1 Management** లో, CSR-2 ని **CSR-2 List** లో ఆమోదించండి.
:::

::: section request-extension
title: స్కీమ్ గడువు పొడిగింపు అడగడం
routes: /RequestExtension/{CSRId:int}
for: 
roles: 
designations: 
minutes: 2
source-hash: c4d4cc4e073e1bda
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఒక sales audit స్కీమ్‌లో కావాల్సిన టర్నోవర్ (అమ్మకాలు) సమయానికి చేరకపోతే, **Request Extension** స్క్రీన్‌తో తర్వాతి ముగింపు తేదీ అడగవచ్చు. దీన్ని **Period-End Review** నుంచి తెరవండి.

1. **Scheme Information (Read Only)** చెక్ చేయండి: **Sanction Number**, **Current Scheme Period**, **Required Turnover (TO)**, **Actual Turnover Achieved**, **Balance to Target**.
2. **Requested New End Date** ఎంచుకోండి. అది ఇప్పటి ముగింపు తేదీ తర్వాత ఉండాలి.
3. **Reason for Extension** ఎంచుకోండి.
4. **Remarks / Explanation (Optional)** 500 అక్షరాల వరకు జోడించండి.
5. **Submit Extension Request** నొక్కండి.
6. **Extension Request Submitted** బాక్స్‌లో **Request ID**, **Pending Approval** కనిపిస్తాయి. **View My Request** లేదా **Go to Sales Audit List** నొక్కండి.

> Tip: రిక్వెస్ట్ ఆమోదం పొందే వరకు స్కీమ్ ముగింపు తేదీ మారదు.

## సాధారణ సమస్యలు

- **"Select the new end date."** లేదా **"The new end date must be after ..."** (కొత్త ముగింపు తేదీ తర్వాతిది అయి ఉండాలి) తర్వాతి తేదీ ఎంచుకోండి.
- **"A request is already awaiting approval for this CSR."** (ఈ CSR కి ఒక రిక్వెస్ట్ ఇప్పటికే ఆమోదం కోసం ఎదురుచూస్తోంది) మొదటి రిక్వెస్ట్ నిర్ణయం కోసం ఎదురుచూడండి.
:::

::: section welfare-application
title: సంక్షేమ దరఖాస్తు పరిశీలించడం
routes: /ApprovalDetail/{Id:int}, /MOApproval/{Id:int}
for: 
roles: 
designations: 
minutes: 3
source-hash: a63f984a39e5444b
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Application Details** స్క్రీన్ ఒక డీలర్ యొక్క ఒక సంక్షేమ (SDWA) దరఖాస్తును చూపిస్తుంది. కాబట్టి ఇప్పటి స్థాయిలో ఉన్న ఆమోదించేవారు దాన్ని చెక్ చేసి నిర్ణయం తీసుకోవచ్చు. దీన్ని **Scheme Approval** నుంచి తెరవండి.

1. పైన ఉన్న **Approval Workflow** చెక్ చేయండి. MO నుంచి SDWA Admin వరకు ప్రతి స్థాయి అందులో కనిపిస్తుంది.
2. **Applicant Information**, **Nominee & Beneficiary Information**, **Eligibility Information** చదవండి.
3. **Uploaded Documents** కింద, ప్రతి ఫైల్ చెక్ చేయడానికి **View** లేదా **Download** నొక్కండి.
4. ముందు స్థాయిల **Previous Remarks** చదవండి.
5. మీ వంతు అయితే, **Comments** ఎంచుకోండి. **Recommendation** తనంతట అదే నిండుతుంది. SDWA Admin **Cheque Number**, **Cheque Amount** కూడా ఎంటర్ చేస్తారు.
6. **Submit** నొక్కండి. లిస్ట్‌కు తిరిగి వెళ్లడానికి **Back to Approvals** నొక్కండి.

## సాధారణ సమస్యలు

- **Submit బటన్ లేదు.** దరఖాస్తు ఇప్పుడు మీ స్థాయిలో లేదు.
- **దరఖాస్తు తిరస్కరించబడిందని ఎరుపు బాక్స్ చెబుతోంది.** సరిచేయడానికి అది డీలర్‌కు తిరిగి పంపబడింది, లేదా మూసివేయబడింది.
:::
