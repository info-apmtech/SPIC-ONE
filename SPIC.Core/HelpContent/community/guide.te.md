---
module: community
title: కమ్యూనిటీ
icon: bi-chat-square-text
order: 115
language: te
category: జ్ఞానం
note: కొత్త గైడ్
meta-source-hash: 1b034d33bc7629a5
---

::: section overview
title: ఈ మాడ్యూల్ మీకు ఏమి చేస్తుంది
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 59061c3615515658
confidence: ok

**SPIC Knowledge Community** లో ప్రొడక్ట్‌లు, వ్యవసాయం గురించి ప్రశ్నలు అడగవచ్చు, పొలంలో మీరు చూసినది పంచుకోవచ్చు, SPIC నిపుణుల నుంచి, ఇతర రైతుల నుంచి జవాబులు పొందవచ్చు.

- ask | icon: bi-patch-question | color: #0d9488
  title: ప్రశ్న అడగండి
  text: మీ సమస్యను మీ సొంత మాటల్లో రాయండి. ఉపయోగపడితే ఫోటోలు లేదా డాక్యుమెంట్లు జత చేయండి.
- answer | icon: bi-patch-check | color: #16a34a
  title: నిపుణుల జవాబులు పొందండి
  text: SPIC సిబ్బంది **SPIC Expert** గుర్తుతో జవాబు ఇస్తారు. ఇతర రైతులు, డీలర్లు కూడా జవాబు ఇవ్వవచ్చు.
- learn | icon: bi-people | color: #2563eb
  title: ఇతరుల నుంచి నేర్చుకోండి
  text: పాత చర్చలు వెతకండి, నచ్చినవి ఫాలో చేయండి, ప్రొడక్ట్ గ్రూపుల్లో చేరండి.
:::

::: section flow
title: ప్రశ్న ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 8b3d8c2b9b5b6d84
confidence: ok

ప్రతి వరుస ఒక గుంపు, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- members | type: lane | color: #0d9488 | roles: Farmer, Dealer
  title: రైతులు, డీలర్లు
  sub: అడగడం, జవాబు ఇవ్వడం, పంచుకోవడం
- experts | type: lane | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC నిపుణులు
  sub: SPIC సిబ్బంది
- ask | lane: members | col: 1 | icon: bi-pencil-square | color: #d97706 | next: reply, expert | screen: ask-question
  title: ప్రశ్న అడగడం
  status: Waiting For Reply
  text: **Create a Discussion** ఫారం నింపి **Post Discussion** నొక్కండి. ప్రశ్న తన మొదటి జవాబు కోసం ఎదురుచూస్తుంది.
- similar | lane: members | col: 2 | icon: bi-search | color: #ea580c | kind: exception | from: ask | to: ask | screen: ask-question
  title: ఇలాంటి ప్రశ్న దొరికింది
  text: మీ ప్రశ్న లాగానే ఉన్న చర్చలను యాప్ చూపిస్తుంది. జవాబు చదవడానికి ఒకదాన్ని తెరవండి, లేదా **Back to edit** నొక్కండి, లేదా **Post Anyway** నొక్కండి.
- reply | lane: members | col: 3 | icon: bi-chat-dots | color: #2563eb | next: resolve | screen: discussion-details
  title: ఒక సభ్యుడు జవాబు ఇస్తారు
  status: Open
  text: ఇంకో రైతు లేదా డీలర్ జవాబు ఇస్తారు. ఎదురుచూస్తున్న ప్రశ్న **Open** అవుతుంది.
- expert | lane: experts | col: 3 | icon: bi-patch-check | color: #16a34a | next: resolve | screen: discussion-details
  title: ఒక నిపుణుడు జవాబు ఇస్తారు
  status: Expert Answer
  text: SPIC సిబ్బంది జవాబు ఇచ్చినప్పుడు, చర్చలో **Expert Answer** అని కనిపిస్తుంది.
- resolve | lane: members | col: 4 | icon: bi-check2-circle | color: #64748b | return: reply | screen: discussion-details
  title: పరిష్కారమైందని గుర్తించడం
  status: Resolved
  text: సమస్య తీరాక, ప్రశ్న అడిగిన వ్యక్తి **Mark as Resolved** నొక్కుతారు. **Reopen** నొక్కితే అది మళ్లీ **Open** అవుతుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: af226e0559d41a5d
confidence: ok

- waiting | color: #d97706
  label: Waiting For Reply
  text: పోస్ట్ అయ్యింది, కానీ ఇంకా ఎవరూ జవాబు ఇవ్వలేదు.
- waiting-answers | color: #d97706
  label: Waiting for Answers
  text: మీరు ప్రశ్న అడిగిన వెంటనే వచ్చే "posted" స్క్రీన్‌లో ఇది కనిపిస్తుంది. దీని అర్థం కూడా Waiting For Reply లాంటిదే.
- open | color: #2563eb
  label: Open
  text: సభ్యులు జవాబు ఇచ్చారు. చర్చ ఇంకా కొనసాగుతోంది.
- expert | color: #16a34a
  label: Expert Answer
  text: ఒక SPIC నిపుణుడు జవాబు ఇచ్చారు.
- resolved | color: #64748b
  label: Resolved
  text: సమస్య తీరిందని ప్రశ్న అడిగిన వ్యక్తి గుర్తించారు. దీన్ని మళ్లీ తెరవవచ్చు.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: b913d791fc7175cc
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. లాగిన్ అయ్యే ప్రతి ఒక్కరూ చర్చలు చదవవచ్చు, వాటిలో పాల్గొనవచ్చు.

- members | color: #0d9488 | roles: Farmer, Dealer
  title: రైతులు, డీలర్లు
  - ప్రశ్నలు అడగడం, ఫోటోలు లేదా ఫైల్‌లు జత చేయడం
  - జవాబు ఇవ్వడం, లైక్ చేయడం, సేవ్ చేయడం, చర్చలను ఫాలో చేయడం
  - తమ సొంత ప్రశ్నలను మార్చడం, డిలీట్ చేయడం లేదా పరిష్కారమైందని గుర్తించడం
- experts | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC నిపుణులు (SPIC సిబ్బంది)
  - ప్రశ్నలకు జవాబు ఇవ్వడం. మీ జవాబులపై **SPIC Expert** అని కనిపిస్తుంది
  - ఇతర సభ్యుల లాగానే అడగడం, పంచుకోవడం
- moderators | color: #475569 | roles: Admin, CorporateAdmin
  title: Admin, Corporate Admin
  - ఏ చర్చనైనా పరిష్కారమైందని గుర్తించడం, లేదా మళ్లీ తెరవడం
  - ఏ జవాబునైనా డిలీట్ చేయడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 971404c02ac40d12
confidence: ok

- cannot-post | popular: true
  q: Post Discussion నొక్కితే ఎరుపు రంగు మెసేజ్‌లు వస్తున్నాయి
  a: టైటిల్‌లో కనీసం 10 అక్షరాలు ఉండాలి, ఒక **Category** ఎంచుకోవాలి, **Description** లో కనీసం 20 అక్షరాలు ఉండాలి. ఎరుపులో ఉన్న ఫీల్డ్‌ను సరిచేసి మళ్లీ **Post Discussion** నొక్కండి.
- similar | popular: true
  q: "We found similar discussions" అని యాప్ చెబుతోంది. నేను ఏమి చేయాలి?
  a: మీ ప్రశ్నకు ఇప్పటికే జవాబు ఉండొచ్చు. దాన్ని చదవడానికి **View Discussion** నొక్కండి. మీ ప్రశ్న వేరే అయితే, **Post Anyway** నొక్కండి. మీరు రాసినది మార్చడానికి **Back to edit** నొక్కండి.
- edit-delete
  q: నా ప్రశ్నను ఎలా మార్చాలి లేదా డిలీట్ చేయాలి?
  a: **Discussions** తెరిచి, **My Discussions** ట్యాబ్‌కు వెళ్లి, మీ ప్రశ్నపై ఉన్న మూడు చుక్కలు నొక్కండి. **Edit** లేదా **Delete** ఎంచుకోండి.
- no-resolve
  q: నాకు Mark as Resolved కనిపించడం లేదు
  a: ప్రశ్న అడిగిన వ్యక్తి లేదా Admin మాత్రమే చర్చను పరిష్కారమైందని గుర్తించగలరు లేదా మళ్లీ తెరవగలరు.
- file-not-attached
  q: నా ఫైల్ లేదా ఫోటో జత కాలేదు
  a: ప్రశ్నలోని ప్రతి ఫైల్ 10 MB లేదా అంతకంటే తక్కువ ఉండాలి. ఒక ప్రశ్నకు 6 ఫైల్‌ల వరకు జత చేయవచ్చు. ఒక జవాబుకు, ఒక్కొక్కటి 5 MB వరకు ఉన్న 3 ఫోటోల వరకు జత చేయవచ్చు.
- expert-answers
  q: SPIC నిపుణుల జవాబులు ఎలా కనుక్కోవాలి?
  a: **Discussions** తెరిచి **Expert Answers** ట్యాబ్ నొక్కండి. చర్చలో, నిపుణుల జవాబులపై **SPIC Expert** గుర్తు ఉంటుంది.
:::

::: section community-home
title: Knowledge Community హోమ్ పేజీ వాడటం
routes: /Community
for: 
roles: 
designations: 
minutes: 2
source-hash: ea1bb46391aec31c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**SPIC Knowledge Community** హోమ్ పేజీ నుంచే మీరు మొదలుపెడతారు. ఇక్కడ ప్రశ్న అడగవచ్చు, కొత్త చర్చలు చూడవచ్చు, ఒక ప్రొడక్ట్ చర్చలు తెరవవచ్చు.

1. సెర్చ్ బాక్స్‌లో మీ ప్రశ్న లేదా ప్రొడక్ట్ పేరు టైప్ చేసి **Ask a Question** నొక్కండి. మీరు టైప్ చేసిన మాటలే టైటిల్‌గా ప్రశ్న ఫారం తెరుచుకుంటుంది.
2. సెర్చ్ బాక్స్ కింద ఉన్న సంఖ్యలు చూడండి: **Community Members**, **Discussions**, **Expert Answer**.
3. **Recent Discussions** కింద, ఒక చర్చను తెరవడానికి దాన్ని నొక్కండి. లిస్ట్‌ను గడులుగా (grid) లేదా వరుసలుగా చూపించడానికి రెండు చిన్న బటన్‌లు వాడండి.
4. ట్యాబ్‌లు, ఫిల్టర్‌లతో పూర్తి లిస్ట్ చూడటానికి **View All Discussions** నొక్కండి.
5. **Popular Product** కింద, ఒక ప్రొడక్ట్ గ్రూపులో చేరడానికి **Join** నొక్కండి. అది **Joined** అని మారుతుంది. బయటకు రావడానికి మళ్లీ నొక్కండి.
6. ఆ ప్రొడక్ట్ గురించిన చర్చలు మాత్రమే చూడటానికి, ప్రొడక్ట్ కార్డుపై ఉన్న **Discussions** నొక్కండి.
7. **New Here?** బాక్స్‌లో, కొత్త ప్రశ్న మొదలుపెట్టడానికి **Ask a Question** నొక్కండి.

> Tip: మీరు గడుల రూపం ఎంచుకున్నారా, వరుసల రూపం ఎంచుకున్నారా అనేది యాప్ గుర్తుపెట్టుకుంటుంది.

## ప్రొడక్ట్ ఫోటో మార్చడం

- Digital Library ని మార్చగలిగే Admin, సిబ్బందికి ప్రతి ప్రొడక్ట్ ఫోటోపై ఒక కెమెరా ఐకాన్ కనిపిస్తుంది.
- కెమెరా నొక్కి, 3 MB వరకు ఉన్న JPG, PNG లేదా WEBP ఫోటో ఎంచుకోండి.

## సాధారణ సమస్యలు

- **"Discussions could not be loaded".** (చర్చలు లోడ్ కాలేదు) కనెక్షన్ ఫెయిల్ అయ్యింది. **Try again** నొక్కండి.
- **"No discussions yet".** (ఇంకా చర్చలు లేవు) ఇంకా ఎవరూ ప్రశ్న అడగలేదు. మొదట అడిగేవారు మీరే కావడానికి **Ask a Question** నొక్కండి.
- **"That picture is larger than 3 MB."** (ఆ ఫోటో 3 MB కంటే పెద్దది) చిన్న ప్రొడక్ట్ ఫోటో ఎంచుకోండి.
:::

::: section browse-discussions
title: చర్చలు కనుక్కోవడం, ఫాలో చేయడం
routes: /Community/discussions
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-list-stats] | 2 > [data-help=cm-list-tabs] | 3 > [data-help=cm-list-filters] | 4 > [data-help=cm-list-tools] | 5 > [data-help=cm-list-card] | 6 > [data-help=cm-list-new]
minutes: 3
source-hash: 6111e390f7350721
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Discussions** స్క్రీన్ కమ్యూనిటీలోని ప్రతి ప్రశ్నను లిస్ట్‌గా చూపిస్తుంది. ఇక్కడ వెతకవచ్చు, ఫిల్టర్ చేయవచ్చు, చర్చలను ఫాలో చేయవచ్చు, మీ సొంత ప్రశ్నలను నిర్వహించవచ్చు.

1. పైన ఉన్న టైల్స్ చూడండి: **Total Discussions**, **Active Members**, **Expert Answers**, **New This Month**.
2. ఒక ట్యాబ్ ఎంచుకోండి: **All Discussion**, **My Discussions**, **Trending**, **Unanswered**, **Following** లేదా **Expert Answers**.
3. **Filters** తెరిచి **Date Range**, **Category**, **Product**, **Crop**, **Status** లేదా **Tags** ఎంచుకుని, **Apply Filters** నొక్కండి. **Clear All** నొక్కితే అవి తీసేయబడతాయి.
4. ఒక విషయం కనుక్కోవడానికి **Sort by:** (**Latest Activity**, **Most Replies**, **Most Views** లేదా **Oldest**) వాడండి, **Search discussions...** లో టైప్ చేయండి.
5. ఒక చర్చను తెరవడానికి దాన్ని నొక్కండి. దాన్ని ఫాలో చేయడానికి, ఆ వ్యక్తి ఫోటో కింద ఉన్న **Follow** నొక్కండి. మీ సొంత ప్రశ్నలపై, **Edit** లేదా **Delete** చేయడానికి మూడు చుక్కలు నొక్కండి.
6. కొత్త ప్రశ్న అడగడానికి **Start a Discussion** నొక్కండి.

> Careful: చర్చను డిలీట్ చేస్తే, అది, దాని జవాబులు కమ్యూనిటీ నుంచి పూర్తిగా పోతాయి.

## ట్యాబ్‌లు ఏమి చూపిస్తాయి

- **My Discussions**: మీరు అడిగిన ప్రశ్నలు.
- **Trending**: గత 30 రోజుల్లో ఎక్కువగా చర్చ జరిగిన ప్రశ్నలు.
- **Unanswered**: ఇంకా జవాబు రాని ప్రశ్నలు.
- **Following**: మీరు ఫాలో చేస్తున్న ప్రశ్నలు.
- **Expert Answers**: SPIC నిపుణుడు జవాబు ఇచ్చిన ప్రశ్నలు.

## సాధారణ సమస్యలు

- **"No discussions found".** (చర్చలు ఏవీ దొరకలేదు) వేరే ట్యాబ్ చూడండి, లేదా **Clear filters** నొక్కండి.
- **చర్చపై మూడు చుక్కలు లేవు.** మీ సొంత ప్రశ్నలను మాత్రమే మార్చగలరు లేదా డిలీట్ చేయగలరు.
- **Tags ఫిల్టర్ పనిచేయడం లేదు.** **Apply Filters** నొక్కే ముందు, ఒక ట్యాగ్ టైప్ చేసి Enter నొక్కండి.
:::

::: section ask-question
title: ప్రశ్న అడగడం
routes: /Community/new, /Community/posted/{id}
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-new-title] | 2 > [data-help=cm-new-choices] | 3 > [data-help=cm-new-tags] | 4 > [data-help=cm-new-desc] | 5 > [data-help=cm-new-files] | 6 > [data-help=cm-new-post]
minutes: 3
source-hash: a42a0e7fda40c257
confidence: ok

**ఈ స్క్రీన్ దేనికి**
కమ్యూనిటీని ప్రశ్న అడగడానికి లేదా మీ అనుభవం పంచుకోవడానికి **Create a Discussion** స్క్రీన్ వాడండి. తర్వాత SPIC నిపుణులు, ఇతర రైతులు జవాబు ఇవ్వగలరు.

1. మీ ప్రశ్నను **Discussion Title** లో టైప్ చేయండి.
2. ఒక **Category** ఎంచుకోండి. అది ఏదైనా ప్రొడక్ట్ లేదా పంట గురించి అయితే, **Related Product**, **Crop** కూడా ఎంచుకోండి.
3. **Tags** కింద, ఏదైనా తెగులు లేదా సమస్య లాంటి ఒక చిన్న మాట టైప్ చేసి Enter లేదా కామా (comma) నొక్కండి. 6 ట్యాగ్‌ల వరకు జత చేయవచ్చు.
4. **Description** లో మీ ప్రశ్న, మీరు ఏమి ప్రయత్నించారు, ఏమి చూశారు రాయండి.
5. **Attachments** కింద, ఫోటోలు లేదా ఫైల్‌లను బాక్స్‌లో వేయండి లేదా **Browse Files** నొక్కండి. ఫైల్ తీసేయడానికి దాని అడ్డగీత (X) నొక్కండి.
6. **Post Discussion** నొక్కండి. యాప్ **We found similar discussions** అని చూపిస్తే, వాటిని చదవండి లేదా **Post Anyway** నొక్కండి. **Your question looks unique!** అని చూపిస్తే, మళ్లీ **Post Discussion** నొక్కండి.

> Tip: ఆకు, మొక్క లేదా ప్రొడక్ట్ లేబుల్ యొక్క స్పష్టమైన ఫోటో ఉంటే నిపుణులు త్వరగా జవాబు ఇస్తారు.

## పోస్ట్ చేశాక

- **Your question has been posted** స్క్రీన్‌లో టైటిల్, **Category**, **Posted** సమయం, **Status**, **Views** కనిపిస్తాయి.
- మీ ప్రశ్నను తెరవడానికి **View Discussion** నొక్కండి, లేదా హోమ్ పేజీకి వెళ్లడానికి **Back to Community** నొక్కండి.

## మీ ప్రశ్నను మార్చడం

- **Discussions** లో, మీ ప్రశ్నపై మూడు చుక్కలు నొక్కి **Edit** ఎంచుకోండి. అదే ఫారం తెరుచుకుంటుంది.
- రాసినది మార్చి **Save Changes** నొక్కండి.
- మీరు టైప్ చేసినది తీసేసే ముందు **Cancel** మిమ్మల్ని అడుగుతుంది. మార్చడం కొనసాగించడానికి **Keep editing** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Discussion Title** | మీ ప్రశ్న ఒకే లైన్‌లో. కనీసం 10 అక్షరాలు. తప్పనిసరి. |
| **Category** | ప్రశ్న రకం. తప్పనిసరి. |
| **Related Product** | ఇది ఏ SPIC ప్రొడక్ట్ గురించో అది, ఉంటే. |
| **Crop** | పంట, ఉదాహరణకు వరి లేదా టమాటా. |
| **Tags** | ఇతరులు దీన్ని కనుక్కోవడానికి సాయపడే చిన్న మాటలు, 6 వరకు. |
| **Description** | పూర్తి ప్రశ్న. కనీసం 20 అక్షరాలు. తప్పనిసరి. |
| **Attachments** | ఫోటోలు, PDF లేదా Word ఫైల్‌లు, ఒక్కొక్కటి 10 MB వరకు. |

## సాధారణ సమస్యలు

- **"Please enter a discussion title."** లేదా **"The title needs at least 10 characters."** (టైటిల్ రాయండి / టైటిల్‌లో కనీసం 10 అక్షరాలు ఉండాలి) ఇంకా పొడవైన టైటిల్ రాయండి.
- **"Please add a little more detail (at least 20 characters)."** (ఇంకొంచెం వివరం రాయండి, కనీసం 20 అక్షరాలు) **Description** లో ఇంకా ఎక్కువ రాయండి.
- **"That file is larger than 10 MB and was not attached."** (ఆ ఫైల్ 10 MB కంటే పెద్దది, అందుకే జత కాలేదు) ఫైల్ సైజు తగ్గించండి లేదా కొత్త ఫోటో తీయండి.
- **"Your question was posted, but the attachments could not be uploaded."** (మీ ప్రశ్న పోస్ట్ అయ్యింది, కానీ జత చేసినవి అప్‌లోడ్ కాలేదు) ప్రశ్న సేవ్ అయ్యింది. మీ ఇంటర్నెట్ కనెక్షన్ చెక్ చేయండి.
:::

::: section discussion-details
title: చర్చ చదవడం, జవాబు ఇవ్వడం
routes: /Community/discussion/{id}
for: 
roles: 
designations: 
minutes: 3
source-hash: 59af4bc85c32cb5c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Discussion Details** స్క్రీన్ ఒక ప్రశ్నను దాని ఫోటోలు, ఫైల్‌లు, అన్ని జవాబులతో చూపిస్తుంది. ఇక్కడ జవాబు ఇవ్వవచ్చు, లైక్ చేయవచ్చు, సేవ్ చేయవచ్చు, ఫాలో చేయవచ్చు, షేర్ చేయవచ్చు.

1. పైన, ఎవరు అడిగారు, వారి గుర్తు (**Farmer**, **Dealer** లేదా **SPIC Expert**), తేదీ, స్టేటస్ చూడండి.
2. ప్రశ్న చదవండి. ఫోటోను పెద్దగా చూడటానికి దాన్ని నొక్కండి. ఫైల్‌ను తెరవడానికి దాన్ని నొక్కండి.
3. పైన ఉన్న **Save**, **Share**, **Follow** వాడండి. ప్రశ్నను లైక్ చేయడానికి బొటనవేలు బటన్ నొక్కండి.
4. **Replied** కింద, జవాబులు చదవండి. **Latest**, **Oldest** లేదా **Most Liked** ఎంచుకోవడానికి **Sort by:** వాడండి.
5. ఒక జవాబుకు మాత్రమే జవాబు ఇవ్వడానికి, దాని **reply** బటన్ నొక్కండి. ఆ వ్యక్తి పేరుతో ఒక బాక్స్ తెరుచుకుంటుంది. మీ జవాబు రాసి **Reply** నొక్కండి.
6. ప్రశ్నకు జవాబు ఇవ్వడానికి, కింద ఉన్న **Write a reply...** లో టైప్ చేయండి. అవసరమైతే 3 ఫోటోల వరకు లేదా ఒక లింక్ జత చేసి, పంపే బటన్ నొక్కండి.
7. మీ సమస్య తీరాక, **Mark as Resolved** నొక్కి నిర్ధారించండి. ఇంకా సాయం కావాలంటే **Reopen** నొక్కండి.

> Tip: కంప్యూటర్‌లో, Enter నొక్కితే జవాబు వెళ్లిపోతుంది. కొత్త లైన్ కోసం Shift తో పాటు Enter నొక్కండి.
> Careful: జవాబుపై ఉన్న బిన్ (చెత్తబుట్ట) ఐకాన్ ఆ జవాబును డిలీట్ చేస్తుంది. మీ సొంత జవాబులను మాత్రమే డిలీట్ చేయగలరు. Admin ఏ జవాబునైనా డిలీట్ చేయగలరు.

## సాధారణ సమస్యలు

- **"Discussion not found".** (చర్చ దొరకలేదు) ప్రశ్న డిలీట్ అయ్యింది. **Back to Discussions** నొక్కండి.
- **"Please write a few words with your images."** (ఫోటోలతో పాటు కొన్ని మాటలు రాయండి) జవాబులో ఫోటోలు మాత్రమే కాదు, కొంచెం రాత కూడా ఉండాలి.
- **"A reply can carry up to 3 images."** (ఒక జవాబుకు 3 ఫోటోల వరకు మాత్రమే జత చేయవచ్చు) ఒక ఫోటో తీసేయండి, లేదా మిగతావి ఇంకో జవాబులో పెట్టండి.
- **Mark as Resolved బటన్ లేదు.** ప్రశ్న అడిగిన వ్యక్తి లేదా Admin మాత్రమే పరిష్కారమైందని గుర్తించగలరు లేదా మళ్లీ తెరవగలరు.
:::
