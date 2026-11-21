---
module: welfare
title: సంక్షేమ పథకాలు (SDWA)
icon: bi-heart
order: 111
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
source-hash: fa298dd497c945c5
confidence: ok

డీలర్లు పెళ్లి కానుక, వైద్య సహాయం, చదువు సహాయం లాంటి SDWA సంక్షేమ పథకాలకు యాప్ నుంచే అప్లై చేయవచ్చు. అధికారులు ప్రతి అప్లికేషన్‌ను దశల వారీగా చెక్ చేస్తారు. అప్లికేషన్ ఎక్కడ ఉందో డీలర్ ఎప్పుడైనా చూడవచ్చు.

- apply | icon: bi-pencil-square | color: #be123c
  title: డీలర్ అప్లై చేస్తారు
  text: అర్హత చెక్ చేసి, ఒక పథకం ఎంచుకుని, ఫారం నింపి, డాక్యుమెంట్లు అప్‌లోడ్ చేయండి.
- approve | icon: bi-shield-check | color: #d97706
  title: ఐదు చెక్‌లు
  text: MO, RM, SM, SDWA, SDWA Admin ఒకరి తర్వాత ఒకరు దాన్ని చెక్ చేస్తారు.
- benefit | icon: bi-cash-coin | color: #16a34a
  title: చివరి ఆమోదం
  text: SDWA Admin చివరి ఆమోదం ఇచ్చి, చెక్ (cheque) వివరాలు నమోదు చేస్తారు.
:::

::: section flow
title: అప్లికేషన్ ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 942cc75f4b898ba1
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- dealer | type: lane | color: #be123c | roles: Dealer
  title: డీలర్
  sub: అప్లై చేసి ట్రాక్ చేస్తారు
- mo | type: lane | color: #d97706 | roles: MO
  title: MO
  sub: HQ ని చెక్ చేస్తారు
- rm | type: lane | color: #0d9488 | roles: RM
  title: RM
  sub: రీజియన్‌ను చెక్ చేస్తారు
- sm | type: lane | color: #2563eb | roles: SMM
  title: SM
  sub: రాష్ట్రాన్ని చెక్ చేస్తారు
- sdwa | type: lane | color: #7c3aed
  title: SDWA
  sub: SDWA · SDWA Admin
- choose | lane: dealer | col: 1 | icon: bi-ui-checks | color: #64748b | next: apply | screen: dashboard
  title: అర్హత చెక్ చేయడం
  text: డాష్‌బోర్డ్‌లో **Apply Welfare** కింద ఉన్న **Apply Now** నొక్కండి. మీకు అర్హత ఉంటే, ఒక పథకం ఎంచుకోండి.
- apply | lane: dealer | col: 2 | icon: bi-pencil-square | color: #64748b | next: mo-check | screen: apply-scheme
  title: నింపి సబ్మిట్ చేయడం
  status: Draft
  text: ఫారం నింపి, డాక్యుమెంట్లు అప్‌లోడ్ చేసి, సబ్మిట్ చేయండి. అప్పటివరకు అది కేవలం డ్రాఫ్ట్ మాత్రమే.
- mo-check | lane: mo | col: 3 | icon: bi-search | color: #d97706 | next: rm-check | screen: scheme-approval
  title: MO చెక్
  status: Pending MO
  text: HQ లోని డీలర్ల వివరాలు, అమ్మకాలు, డాక్యుమెంట్లు MO చెక్ చేస్తారు.
- rm-check | lane: rm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: sm-check | return: mo-check | screen: scheme-approval
  title: RM చెక్
  status: Pending RM
  text: RM రీజియన్ కోసం చెక్ చేస్తారు. తిరస్కరిస్తే అది MO కి తిరిగి వెళ్తుంది.
- sm-check | lane: sm | col: 5 | icon: bi-hourglass-split | color: #d97706 | next: sdwa-check | return: rm-check | screen: scheme-approval
  title: SM చెక్
  status: Pending SM
  text: SM రాష్ట్రం కోసం చెక్ చేస్తారు. తిరస్కరిస్తే అది RM కి తిరిగి వెళ్తుంది.
- sdwa-check | lane: sdwa | col: 6 | icon: bi-patch-check | color: #7c3aed | next: sdwaadmin | return: sm-check | screen: scheme-approval
  title: SDWA ఆమోదం
  status: Pending SDWA
  text: SDWA ఆమోదిస్తుంది. తిరస్కరిస్తే అది SM కి తిరిగి వెళ్తుంది.
- sdwaadmin | lane: sdwa | col: 7 | icon: bi-cash-coin | color: #7c3aed | next: approved | return: sdwa-check | screen: scheme-approval
  title: SDWA Admin చివరి ఆమోదం
  status: Pending SDWA Admin
  text: SDWA Admin చివరి ఆమోదం ఇచ్చి, చెక్ నంబర్, మొత్తం ఎంటర్ చేస్తారు.
- approved | lane: dealer | col: 8 | icon: bi-check-circle | color: #16a34a | screen: my-applications
  title: ఆమోదం వచ్చింది
  status: Approved
  text: అప్లికేషన్‌కు ఆమోదం వచ్చింది. డీలర్ **My Applications** లో **Approved** అని చూస్తారు.
- returned | lane: dealer | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: mo-check | to: apply | screen: my-applications
  title: సరిచేసి మళ్లీ సబ్మిట్ చేయడం
  status: Rejected by MO – Resubmission Required
  text: MO తిరస్కరిస్తే, అది డీలర్‌కు తిరిగి వస్తుంది. సరిచేసి మళ్లీ సబ్మిట్ చేయండి. అదే Application ID ఉంటుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8a133f3149737003
confidence: ok

- draft | color: #64748b
  label: Draft
  text: సేవ్ అయ్యింది కానీ పంపలేదు.
- pending-mo | color: #d97706
  label: Pending MO
  text: చెక్ చేయడం కోసం MO దగ్గర ఉంది.
- pending-rm | color: #d97706
  label: Pending RM
  text: చెక్ చేయడం కోసం RM దగ్గర ఉంది.
- pending-sm | color: #d97706
  label: Pending SM
  text: చెక్ చేయడం కోసం SM దగ్గర ఉంది.
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: ఆమోదం కోసం SDWA దగ్గర ఉంది.
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: చివరి ఆమోదం కోసం SDWA Admin దగ్గర ఉంది.
- approved | color: #16a34a
  label: Approved
  text: చివరి ఆమోదం వచ్చింది.
- resubmit | color: #dc2626
  label: Rejected by MO – Resubmission Required
  text: ఇది డీలర్‌కు కనిపిస్తుంది. అప్లికేషన్ సరిచేసి మళ్లీ సబ్మిట్ చేయండి.
- returned | color: #ea580c
  label: Returned to Dealer
  text: ఇది అధికారులకు కనిపిస్తుంది. సరిచేయడం కోసం అప్లికేషన్ డీలర్ దగ్గరకు తిరిగి వెళ్లింది.
- rejected | color: #475569
  label: Rejected
  text: మూసివేయబడింది. అవసరమైతే మళ్లీ అప్లై చేయండి.
- cancelled | color: #94a3b8
  label: Cancelled
  text: అప్లికేషన్ రద్దు చేయబడింది.
- awaiting-mo | color: #d97706
  label: Awaiting MO Review
  text: MO కోసం ఎదురుచూస్తున్న కొత్త అప్లికేషన్‌కు డాష్‌బోర్డ్‌లో కనిపించే పేరు.
- under-mo | color: #d97706
  label: Under MO Review
  text: MO దగ్గరకు తిరిగి వచ్చిన అప్లికేషన్‌కు డాష్‌బోర్డ్‌లో కనిపించే పేరు.
- under-rm | color: #d97706
  label: Under RM Review
  text: Pending RM కి డాష్‌బోర్డ్‌లో కనిపించే పేరు.
- under-smm | color: #d97706
  label: Under SMM Review
  text: Pending SM కి డాష్‌బోర్డ్‌లో కనిపించే పేరు.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 69fdf9c12e670358
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- dealer | color: #be123c | roles: Dealer
  title: డీలర్
  - అర్హత చెక్ చేసి అప్లై చేయడం
  - అప్లికేషన్‌లు ట్రాక్ చేయడం, PDF డౌన్‌లోడ్ చేయడం
  - తిరిగి వచ్చిన అప్లికేషన్‌ను సరిచేసి మళ్లీ సబ్మిట్ చేయడం
- mo | color: #d97706 | roles: MO
  title: MO
  - మీ HQ అప్లికేషన్‌లు చెక్ చేయడం
  - తప్పు డాక్యుమెంట్‌ను మార్చడం లేదా డిలీట్ చేయడం
  - ఆమోదించడం, లేదా డీలర్‌కు తిరిగి పంపడం
- rm-sm | color: #0d9488 | roles: RM, SMM
  title: RM, SM
  - మీ రీజియన్ లేదా రాష్ట్రం అప్లికేషన్‌లు చెక్ చేయడం
  - ఆమోదించడం, లేదా ఒక దశ వెనక్కి పంపడం
- sdwa | color: #7c3aed
  title: SDWA, SDWA Admin
  - అన్ని ప్రాంతాల అప్లికేషన్‌లు చూడటం
  - చివరి రెండు ఆమోదాలు ఇవ్వడం
  - SDWA Admin చెక్ వివరాలు ఎంటర్ చేస్తారు
- director | color: #475569 | roles: Director
  title: Director
  - అన్ని అప్లికేషన్‌లు చూడటం
  - SDWA ఆమోదించాక, కొన్ని పథకాలకు కావాలంటే ఒక రివ్యూ ఇవ్వడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: cd3161af8efa4aae
confidence: ok

- returned | popular: true
  q: నా అప్లికేషన్‌లో "Rejected by MO – Resubmission Required" అని ఉంది
  a: **My Applications** తెరిచి, ఎరుపు బాక్స్‌లో ఉన్న కారణం చదవండి. **Correct & Resubmit** నొక్కి, వివరాలు లేదా డాక్యుమెంట్లు సరిచేసి, **Resubmit Application** నొక్కండి. అదే Application ID ఉంటుంది.
- not-eligible | popular: true
  q: Welfare Eligibility విండోలో నాకు అర్హత లేదని వస్తోంది
  a: ఆ విండోలో ప్రతి నియమం **Required** (కావాల్సింది), **Your dealership** (మీ డీలర్‌షిప్) తో కనిపిస్తుంది. ఎరుపులో ఉన్న నియమాలు ఇంకా పూర్తి కాలేదు. ఏదైనా విలువ తప్పు అనిపిస్తే మీ MO తో మాట్లాడండి.
- seven-days
  q: "Invitation must be sent at least 7 days in advance" అని వస్తోంది
  a: Wedding Gift, Sathabhishekam, Grahapravesam కోసం, వేడుక తేదీ ఈరోజు నుంచి కనీసం 7 రోజుల తర్వాత ఉండాలి.
- cheque
  q: ఫారంలో "Cancelled Cheque / Bank Passbook is required" అని వస్తోంది
  a: రద్దు చేసిన చెక్ లేదా బ్యాంక్ పాస్‌బుక్ పేజీ అప్‌లోడ్ చేయండి. దానిపై ప్రింట్ అయినట్టే **Beneficiary Name** టైప్ చేయండి.
- where-is-it
  q: నా అప్లికేషన్ ఇప్పుడు ఎవరి దగ్గర ఉంది?
  a: **My Applications** లో ఉన్న వృత్తాల వరుస ప్రతి దశను చూపిస్తుంది. ఆకుపచ్చ అంటే పూర్తయింది, ఆరెంజ్ అంటే ఇప్పుడు అప్లికేషన్ ఉన్న దశ.
- no-submit
  q: నేను అధికారిని, అప్లికేషన్‌ను ఆమోదించలేకపోతున్నాను
  a: అప్లికేషన్ మీ దశలో ఉండి, మీ ప్రాంతానిదైతేనే మీరు చర్య తీసుకోగలరు. లిస్ట్‌లో దాని స్టేటస్ చెక్ చేయండి.
:::

::: section dashboard
title: డీలర్ డాష్‌బోర్డ్ వాడటం
routes: /SDWADashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: 44c37577fe4705d0
confidence: ok

**ఈ స్క్రీన్ దేనికి**
సంక్షేమ అప్లికేషన్‌లు, గెస్ట్ హౌస్ బుకింగ్‌ల కోసం **Dashboard** డీలర్ మొదటి పేజీ. ఇక్కడ కొత్త అప్లికేషన్ మొదలుపెట్టవచ్చు, మీ అప్లికేషన్‌లు ఎలా ఉన్నాయో చూడవచ్చు.

1. పైన మీ పేరు, **Dealer Code**, **Region**, **Profile Completion** చెక్ చేయండి.
2. **Quick Actions** కింద, **Apply Welfare** కార్డులో **Apply Now** నొక్కండి. యాప్ ముందు మీ అర్హత చెక్ చేస్తుంది.
3. **Welfare Eligibility** విండో మీ **State**, **State Group**, **Dealer Type**, ప్రతి **Eligibility Criteria** నియమం చూపిస్తుంది. అర్హత ఉంటే **Proceed to Apply** నొక్కండి. లేకపోతే **Close** నొక్కండి.
4. **Application Status Overview** చూడండి: **Pending Applications**, **Approved Applications**, **Rejected Applications**, **Processing Applications**.
5. **Recent Applications** కింద, ఒకటి తెరవడానికి **View Details** నొక్కండి, లేదా **My Applications** తెరవడానికి **View All** నొక్కండి.
6. తాజా మార్పుల కోసం **Notifications**, **Recent Activity** చదవండి.

> Tip: ఈ స్క్రీన్‌లోని **Book Room**, **My Bookings** గెస్ట్ హౌస్ కోసం. Guest house గైడ్ చూడండి.

## సాధారణ సమస్యలు

- **"Unable to verify eligibility. Please try again later."** (అర్హత చెక్ చేయలేకపోయాం, తర్వాత ప్రయత్నించండి) మీ ఇంటర్నెట్ చెక్ చేసి, మళ్లీ **Apply Now** నొక్కండి.
- **"No applications yet".** (ఇంకా అప్లికేషన్‌లు లేవు) మీరు ఇంకా అప్లై చేయలేదు. **Apply Welfare** నొక్కండి.
- **నాకు Apply Welfare కనిపించడం లేదు.** SDWA Admin యూజర్లు ఈ స్క్రీన్ నుంచి అప్లై చేయరు.
:::

::: section choose-scheme
title: సంక్షేమ పథకం ఎంచుకోవడం
routes: /WelfareSchemes
for: 
roles: 
designations: 
minutes: 1
source-hash: 71f07f60f43b7d0c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Apply for Welfare Schemes** స్క్రీన్ ప్రతి SDWA పథకాన్ని ఒక కార్డుగా చూపిస్తుంది. మీకు కావాల్సింది ఎంచుకోండి.

1. కార్డులు చూడండి: **Medical Assistance**, **Wedding Gift**, **Grahapravesam**, **Educational Assistance**, **Sathabhishekam**, **Death Relief Assistance**, **Merit Award**, **Distinction Award**.
2. ప్రతి కార్డులో **Documents** (ఎన్ని కావాలి), **Eligibility** (ఎవరు అప్లై చేయవచ్చు) చూడండి.
3. మీకు కావాల్సిన పథకంలో **Apply Now** నొక్కండి. ఆ పథకం కోసం అప్లికేషన్ ఫారం తెరుచుకుంటుంది.

> Careful: **Distinction Award** తమిళనాడు (TN) లోని డీలర్లు, ఉద్యోగులకు మాత్రమే.

## సాధారణ సమస్యలు

- **ఈ స్క్రీన్ తెరవలేకపోతున్నాను.** మీ లాగిన్‌కు సంక్షేమ పథకాల యాక్సెస్ లేకపోవచ్చు. మీ MO లేదా admin ని అడగండి.
:::

::: section grahapravesam
title: Grahapravesam పథకం గురించి చదవడం
routes: /GrahapravesamScheme
for: 
roles: 
designations: 
minutes: 1
source-hash: a035ff18db9c4a2f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Grahapravesam** స్క్రీన్ అప్లై చేసే ముందు గృహప్రవేశం కానుక పథకం గురించి వివరిస్తుంది.

1. పైన ఉన్న కార్డు, **About the Scheme** చదవండి.
2. **Eligibility Criteria**, **Required Documents** చెక్ చేయండి.
3. **Frequently Asked Questions**, **Important Notes** చదవండి.
4. ఫారం తెరవడానికి **Apply Now** నొక్కండి, లేదా వేరే పథకం ఎంచుకోవడానికి **Back to Schemes** నొక్కండి.

## సాధారణ సమస్యలు

- **ఫారం నా గృహప్రవేశం తేదీని తీసుకోవడం లేదు.** ఫారంకు ఈరోజు నుంచి కనీసం 7 రోజుల తర్వాతి తేదీ కావాలి.
:::

::: section apply-scheme
title: సంక్షేమ పథకానికి అప్లై చేయడం
routes: /ApplyWelfareScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-dealer] | 2 > [data-help=wf-quantity] | 3 > [data-help=wf-details] | 4 > [data-help=wf-documents] | 5 > [data-help=wf-declaration] | 6 > [data-help=wf-actions]
minutes: 5
source-hash: 55566f3677387604
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ అప్లికేషన్ నింపి పంపడానికి **Apply for** స్క్రీన్ (ఉదాహరణకు **Apply for Wedding**) వాడండి. ప్రతి పథకానికి ఫారం కొంచెం మారుతుంది.

1. మీ డీలర్ కార్డు (**Location Info**, **Personal Info**) చెక్ చేసి, ఆ పథకం కోసం ఉన్న **Eligibility Conditions** బాక్స్ చదవండి.
2. **Quantity Lifted** కింద, **Quantity Lifted during Last Year (MT)** ఎంచుకోండి. 3 సంవత్సరాల మొత్తాలు వాటంతట అవే నిండుతాయి. తర్వాత **Beneficiary Details** తెరుచుకుంటుంది: **Select Beneficiary Group** ఎంచుకుని, నామినీ, లబ్ధిదారు వివరాలు నింపండి.
3. పథకం భాగం నింపండి, ఉదాహరణకు **Wedding Date** తో **Wedding Details**, లేదా **Treatment / Medical Condition** తో **Medical Assistance Details**.
4. **Document Upload** కింద, ప్రతి డాక్యుమెంట్ కోసం **Click to upload** నొక్కండి. **Cancelled Cheque / Bank Passbook** కోసం, దానిపై ప్రింట్ అయినట్టే **Beneficiary Name** కూడా టైప్ చేయండి.
5. **Declaration** కింద, మాటలు చదివి బాక్స్‌లో టిక్ పెట్టండి.
6. దాచుకోవడానికి **Save as Draft** నొక్కండి, లేదా **Preview Application** నొక్కండి. చెక్ చేసి, **Submit Application** నొక్కి, తర్వాత **Confirm** నొక్కండి. **Application ID** రాసుకుని **Track Application** నొక్కండి.

> Careful: **Confirm** నొక్కాక, అప్లికేషన్ MO కి వెళ్తుంది. మీకు తిరిగి పంపితే తప్ప దాన్ని మార్చలేరు.
> Tip: సేవ్ చేసిన డ్రాఫ్ట్‌కు ఒక రిఫరెన్స్ నంబర్ వస్తుంది. అది **My Applications** లో **Drafts** కింద లెక్కించబడుతుంది.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Quantity Lifted during Last Year (MT)** | గత సంవత్సరం మీరు లిఫ్ట్ చేసిన పరిమాణం శ్రేణి. తప్పనిసరి. |
| **Select Beneficiary Group** | **Direct Dealer**, **Sub Dealer** లేదా **Approved Employee**. తప్పనిసరి. |
| **Nominee Name**, **Nominee Relationship** | మీ డీలర్ రికార్డుల్లో ఉన్న నామినీ. తప్పనిసరి. |
| **Select Beneficiary** | కుటుంబ సభ్యులలో ఒకరు, లేదా **Add new beneficiary**. డైరెక్ట్ డీలర్‌కు తప్పనిసరి. |
| వేడుక తేదీ | Wedding, Sathabhishekam, Grahapravesam: ఈరోజు నుంచి కనీసం 7 రోజుల తర్వాత. |
| **Date of Death**, **Legal Heir Relation** | Death Relief కి మాత్రమే. బంధుత్వం **Others** అయితే, అఫిడవిట్ అప్‌లోడ్ చేయండి. |
| డాక్యుమెంట్లు | PDF, JPG లేదా PNG, ఒక్కొక్కటి 5 MB వరకు. |

## సాధారణ సమస్యలు

- **"Please fix the following errors:"** (కింది తప్పులు సరిచేయండి) బటన్‌ల పైన ఉన్న లిస్ట్ చదివి, ప్రతి దాన్ని సరిచేయండి.
- **ఫైల్ అప్‌లోడ్ చేస్తే ఏమీ జరగడం లేదు.** ఫైల్ PDF, JPG లేదా PNG అయి, 5 MB కంటే చిన్నదిగా ఉండాలి.
- **"Minimum 80% marks is required for the Merit Award."** (Merit Award కి కనీసం 80% మార్కులు కావాలి) Merit Award కి కనీసం 80% కావాలి. Distinction Award కి 80% కంటే ఎక్కువ, బ్యాక్‌లాగ్‌లు (arrears) లేకుండా కావాలి.
- **"A minimum average annual sales of 50,000 MT..."** (కనీస సగటు వార్షిక అమ్మకం 50,000 MT) సబ్ డీలర్ కోసం అప్లై చేయడానికి మీ అమ్మకాలు చాలా తక్కువ (ఉద్యోగి కోసం 5,000 MT).
:::

::: section my-applications
title: నా అప్లికేషన్‌లు ట్రాక్ చేయడం
routes: /WellfareApplication
for: 
roles: 
designations: 
minutes: 2
source-hash: 62f3f5bcf69937c0
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**My Applications** స్క్రీన్ మీరు పెట్టిన ప్రతి సంక్షేమ అప్లికేషన్‌ను, దాని స్టేటస్‌ను, అది దాటిన దశలను చూపిస్తుంది.

1. పైన ఉన్న టైల్స్ చూడండి: **Total**, **Pending**, **Approved**, **Rejected**, **Drafts**, **Cancelled**.
2. **Latest First** లేదా **Oldest First** ఎంచుకోండి, ఒక స్టేటస్ ఎంచుకోండి, లేదా **Search by Application ID or Scheme** లో టైప్ చేయండి.
3. ప్రతి కార్డులో పథకం, Application ID, స్టేటస్ కనిపిస్తాయి. వృత్తాల వరుస **Dealer**, **MO**, **RM**, **SM/SMM**, **SDWA**, **SDWA Admin**, **Final** చూపిస్తుంది. ఆకుపచ్చ అంటే పూర్తయింది, ఆరెంజ్ అంటే ఇప్పుడు ఉన్న చోటు.
4. అన్నీ చూడటానికి **View Details** నొక్కండి, లేదా ఒక కాపీ సేవ్ చేయడానికి **Download PDF** నొక్కండి.
5. ఎరుపు బాక్స్‌లో **Rejected by** అని ఉంటే, **Reason** చదివి **Correct & Resubmit** నొక్కండి. ఫారం సరిచేసి, **Resubmit Application** నొక్కి, తర్వాత **Confirm Resubmission** నొక్కండి.
6. కొత్త అప్లికేషన్ మొదలుపెట్టడానికి **Apply for a scheme** నొక్కండి.

> Tip: మళ్లీ సబ్మిట్ చేసిన అప్లికేషన్‌కు అదే Application ID ఉంటుంది. అది మళ్లీ MO దగ్గర నుంచి మొదలవుతుంది.

## సాధారణ సమస్యలు

- **"No matching applications".** (సరిపోయే అప్లికేషన్‌లు లేవు) **Clear filters** నొక్కండి.
- **Download PDF పై "Failed – Tap to Retry" అని వస్తోంది.** మీ ఇంటర్నెట్ చెక్ చేసి మళ్లీ నొక్కండి.
- **Correct & Resubmit బటన్ లేదు.** MO మీకు తిరిగి పంపిన అప్లికేషన్‌ను మాత్రమే మళ్లీ సబ్మిట్ చేయవచ్చు.
:::

::: section scheme-approval
title: సంక్షేమ అప్లికేషన్‌లు చెక్ చేసి ఆమోదించడం
routes: /SchemeApproval
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-ap-cards] | 2 > [data-help=wf-ap-tabs] | 3 > [data-help=wf-ap-search] | 4 > [data-help=wf-ap-table] | 5 > [data-help=wf-ap-table] | 6 > [data-help=wf-ap-pages]
minutes: 3
source-hash: 06b3fdb4303eee15
confidence: ok

**ఈ స్క్రీన్ దేనికి**
MO, RM, SM, SDWA, SDWA Admin తమ దశలో ఎదురుచూస్తున్న సంక్షేమ అప్లికేషన్‌లు చెక్ చేయడానికి **Approval** స్క్రీన్ వాడతారు. MO తన HQ ని, RM రీజియన్‌ను, SM రాష్ట్రాన్ని చూస్తారు. SDWA, SDWA Admin అన్ని ప్రాంతాలను చూస్తారు.

1. కార్డులు చూడండి: **Total Applications**, మీ దశ కోసం **Pending** (ఉదాహరణకు **Pending MO**), **Validated by MO**, **Rejected**, **Completed**.
2. ఒక ట్యాబ్ ఎంచుకోండి, ఉదాహరణకు **Pending**, మీ దశ కోసం **Approved by**, **Validated by MO**, **Recommended by RM**, **Approved by SM**, **Rejected** లేదా **Completed**.
3. **Search dealer / application...** లో టైప్ చేయండి, లేదా **All Schemes** లో ఒక పథకం ఎంచుకోండి.
4. టేబుల్‌లో **Status**, **Approval Flow (MO → RM → SM → SDWA → SDWA Admin)** చూడండి. ఆకుపచ్చ టిక్ అంటే పూర్తయింది, నీలం వృత్తం అంటే ఇప్పుడు అప్లికేషన్ ఉన్న దశ, ఎరుపు క్రాస్ అంటే తిరస్కరణ.
5. అప్లికేషన్ తెరవడానికి కన్ను ఐకాన్ నొక్కండి. వివరాలు, డాక్యుమెంట్లు చెక్ చేసి, **Comments** ఎంచుకోండి. **Recommendation** దానంతట అదే నిండుతుంది. **Submit** నొక్కండి.
6. ఇంకా వరుసలు చూడటానికి **Items Per Page**, బాణం గుర్తులు వాడండి.

> Careful: తిరస్కరించే కామెంట్ అప్లికేషన్‌ను ఒక దశ వెనక్కి పంపుతుంది. MO తిరస్కరిస్తే, అది డీలర్‌కు తిరిగి వెళ్తుంది.
> Tip: SDWA Admin **Cheque Number**, **Cheque Amount** కూడా ఎంటర్ చేయాలి. **Cheque Image** కూడా జోడించవచ్చు.

## ఆమోదం క్రమం

- MO, తర్వాత RM, తర్వాత SM, తర్వాత SDWA, తర్వాత SDWA Admin చివరి ఆమోదం ఇస్తారు.
- Admin, Corporate Admin, Director లిస్ట్ తెరవగలరు కానీ ఆమోదించరు. SDWA ఆమోదించాక, Death Relief, Educational Assistance, Merit Award, Medical Assistance కోసం Director కావాలంటే ఒక రివ్యూ జోడించవచ్చు.

## సాధారణ సమస్యలు

- **"No applications found for this view."** (ఈ వ్యూలో అప్లికేషన్‌లు లేవు) ఈ ట్యాబ్‌లో ఏదీ ఎదురుచూడటం లేదు. వేరే ట్యాబ్ చూడండి.
- **"Unable to load the approval queue. Please try again."** (ఆమోదం లిస్ట్ లోడ్ కాలేదు) **Retry** నొక్కండి.
- **Submit బటన్ లేదు.** అప్లికేషన్ ఇప్పుడు మీ దశలో లేదు.
- **"Please select a comment."** (ఒక కామెంట్ ఎంచుకోండి) **Submit** నొక్కే ముందు ఒక కామెంట్ ఎంచుకోండి.
:::

::: section company-master
title: SDWA కంపెనీలను నిర్వహించడం
routes: /SdwaCompanyMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: de3ec8b1ed8fc6f6
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**SDWA Company Details** స్క్రీన్‌లో, బిల్లింగ్ కోసం గెస్ట్ హౌస్‌లకు లింక్ చేసిన కంపెనీల లిస్ట్ ఉంటుంది.

1. కార్డులు చూడండి: **Total Companies**, **Active**, **Inactive**.
2. **Add New Company** కింద, **Company Name**, **Short Name / Code**, **GSTIN** ఎంటర్ చేయండి.
3. **Guest House(s)** లో, ప్రతి గెస్ట్ హౌస్‌ను టైప్ చేసి నొక్కండి. ఒక పేరు తీసేయడానికి దానిపై ఉన్న చిన్న x నొక్కండి.
4. స్విచ్‌ను **Active** లేదా **Inactive** కి పెట్టి, **Save** నొక్కండి. **Cancel** ఫారంను ఖాళీ చేస్తుంది.
5. **Existing Companies** లో, వెతకండి లేదా **All Status**, **Active**, **Inactive** ఎంచుకోండి. మార్చడానికి పెన్సిల్, స్టేటస్ మార్చడానికి ఆ స్టేటస్, డిలీట్ చేయడానికి బిన్ నొక్కండి.

> Careful: డిలీట్ చేసింది మళ్లీ తిరిగి రాదు. బుకింగ్‌లకు లింక్ అయిన కంపెనీని డిలీట్ చేయలేరు.

## సాధారణ సమస్యలు

- **"GSTIN is required."** లేదా **"Company Name is required."** (GSTIN / కంపెనీ పేరు తప్పనిసరి) రెండు ఫీల్డ్‌లూ నింపండి.
- **"At least one Guest House must be selected."** (కనీసం ఒక గెస్ట్ హౌస్ ఎంచుకోవాలి) కనీసం ఒక గెస్ట్ హౌస్ ఎంచుకోండి.
- **నాకు Add New Company ఫారం కనిపించడం లేదు.** మీ లాగిన్ ఈ లిస్ట్‌ను చూడటానికి మాత్రమే.
:::
