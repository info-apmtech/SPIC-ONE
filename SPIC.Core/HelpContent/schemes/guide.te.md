---
module: schemes
title: స్కీమ్‌లు, లక్కీ డ్రా
icon: bi-gift
order: 104
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
source-hash: 01c42ebca4d8bbab
confidence: ok

SPIC ప్రొడక్ట్‌లు కొనే డీలర్లకు, రైతులకు స్కీమ్ బహుమతి ఇస్తుంది. ప్రతి కొనుగోలుకు ఒక లక్కీ డ్రా ఎంట్రీ వస్తుంది. డ్రా తేదీన విజేతలను ఎంచుకుంటారు, వారి బహుమతి యాప్‌లో కనిపిస్తుంది.

- create | icon: bi-pencil-square | color: #d97706
  title: స్కీమ్ టీమ్ దాన్ని సెట్ చేస్తుంది
  text: పేరు, ప్రాంతాలు, ప్రొడక్ట్ రూల్స్, బహుమతి, డ్రా తేదీలు.
- join | icon: bi-qr-code-scan | color: #0d9488
  title: డీలర్లు, రైతులు పాల్గొంటారు
  text: ప్రొడక్ట్ మీద ఉన్న QR కోడ్ స్కాన్ చేయండి. ప్రతి కొనుగోలుకు 1 ఎంట్రీ వస్తుంది.
- win | icon: bi-trophy | color: #7c3aed
  title: లక్కీ డ్రా విజేతలను ఎంచుకుంటుంది
  text: విజేతలకు ఒక మెసేజ్, బహుమతి, దాన్ని ఎలా పొందాలో కనిపిస్తాయి.
:::

::: section flow
title: స్కీమ్ ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 801d9ad88aaa016d
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- team | type: lane | color: #d97706 | roles: Admin, CorporateAdmin
  title: స్కీమ్ టీమ్
  sub: ఆఫీస్ సిబ్బంది
- people | type: lane | color: #0d9488 | roles: Dealer, Farmer
  title: డీలర్లు, రైతులు
  sub: స్కీమ్ సభ్యులు
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: review | screen: add-scheme
  title: స్కీమ్ తయారు చేయడం
  status: Draft
  text: **Create Scheme** ఫారంలో ప్రాంతాలు, ప్రొడక్ట్ రూల్స్, బహుమతి, డ్రా సెట్టింగ్‌లు నింపి, **Next** నొక్కండి.
- review | lane: team | col: 2 | icon: bi-clipboard-check | color: #d97706 | next: scan | screen: review-scheme
  title: చెక్ చేసి మొదలుపెట్టడం
  status: Active
  text: **Review Your Scheme** లో ప్రతి వివరం చెక్ చేసి, స్కీమ్ మొదలుపెట్టడానికి **Active Scheme** నొక్కండి.
- scan | lane: people | col: 3 | icon: bi-qr-code-scan | color: #0d9488 | next: draw | screen: lucky-draw
  title: కొని స్కాన్ చేయడం
  status: Ticket Activated
  text: ప్రొడక్ట్ మీద ఉన్న QR కోడ్ స్కాన్ చేసి, కొన్న ప్రొడక్ట్‌లు ఎంచుకోండి. ప్రతి కొనుగోలుకు 1 లక్కీ డ్రా ఎంట్రీ వస్తుంది.
- draw | lane: team | col: 4 | icon: bi-ticket-perforated | color: #2563eb | next: announce | screen: scheme-overview
  title: లక్కీ డ్రా
  status: Selected
  text: డ్రా తేదీన, అన్ని ఎంట్రీల నుంచి విజేతలను ఎంచుకుంటారు. స్కీమ్‌లో సెట్ చేసినట్టు డ్రా **Automatic** లేదా **Manual** గా ఉంటుంది.
- announce | lane: people | col: 5 | icon: bi-megaphone | color: #7c3aed | next: prize | screen: winner-announcement
  title: విజేతకు తెలియజేయడం
  status: Notified
  text: విజేతకు బహుమతి, స్కీమ్ పేరుతో **Congratulations!** అని కనిపిస్తుంది.
- prize | lane: people | col: 6 | icon: bi-gift | color: #16a34a | screen: winner-details
  title: బహుమతి పొందడం
  status: Prize Distributed
  text: **Winner Details** లో బహుమతి, దాని స్టేటస్, అది ఇచ్చిన తేదీ కనిపిస్తాయి.
- not-won | lane: people | col: 4 | icon: bi-emoji-smile | color: #ea580c | kind: exception | from: draw | to: scan
  title: ఈసారి ఎంపిక కాలేదు
  status: Better Luck Next Time!
  text: కొంటూ, స్కాన్ చేస్తూ ఉండండి. ఎంట్రీలు ఎక్కువైతే తర్వాతి డ్రాలో అవకాశాలు కూడా ఎక్కువ.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 663b2eff26f57f93
confidence: ok

- draft | color: #64748b
  label: Draft
  text: స్కీమ్ సేవ్ అయ్యింది కానీ పబ్లిష్ కాలేదు. డీలర్లకు, రైతులకు ఇది కనిపించదు.
- active-draft | color: #d97706
  label: ACTIVE DRAFT
  text: స్కీమ్ మొదలుపెట్టే ముందు రివ్యూ స్క్రీన్‌లో కనిపిస్తుంది.
- active | color: #16a34a
  label: Active
  text: స్కీమ్ నడుస్తోంది. ఒక ఎంట్రీ మీద ఇది ఉంటే, ఆ ఎంట్రీ ఈ రౌండ్‌లో ఇంకా గెలవచ్చు.
- participating | color: #0d9488
  label: Participating
  text: డీలర్ లేదా రైతు స్కీమ్‌లో పాల్గొంటున్నారు.
- verified | color: #0891b2
  label: Verified
  text: కొనుగోలు లేదా డీలర్ చెక్ చేయబడింది.
- ticket | color: #2563eb
  label: Ticket Activated
  text: మీ కొనుగోలుకు ఒక కొత్త లక్కీ డ్రా ఎంట్రీ చేరింది.
- used | color: #64748b
  label: Used
  text: ఈ ఎంట్రీ ఇప్పటికే ఒక డ్రాలో వెళ్లిపోయింది.
- expired | color: #475569
  label: Expired
  text: ఈ ఎంట్రీ ఇక గెలవలేదు.
- selected | color: #7c3aed
  label: Selected
  text: డ్రాలో విజేతగా ఎంపికైంది.
- notified | color: #d97706
  label: Notified
  text: విజేతకు తెలియజేశారు.
- claimed | color: #16a34a
  label: Claimed
  text: విజేత బహుమతి తీసుకున్నారు.
- distributed | color: #16a34a
  label: Prize Distributed
  text: బహుమతి విజేతకు ఇచ్చేశారు.
- completed | color: #16a34a
  label: Completed
  text: ఈ తేదీ డ్రా పూర్తయింది.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 89f40baf961ca43b
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- team | color: #d97706 | roles: Admin, CorporateAdmin
  title: స్కీమ్ టీమ్
  - స్కీమ్‌లు తయారు చేయడం, చెక్ చేయడం
  - స్కీమ్ మొదలుపెట్టడం
  - డీలర్లు, ఎంట్రీలు, విజేతలను గమనించడం
- dealer | color: #0d9488 | roles: Dealer
  title: డీలర్
  - ప్రొడక్ట్‌లు స్కాన్ చేసి ఎంట్రీలు చేర్చడం
  - మీ ఎంట్రీలు, ప్రోగ్రెస్ చూడటం
  - విజేతలను, మీ బహుమతిని చూడటం
- farmer | color: #7c3aed | roles: Farmer
  title: రైతు
  - మీరు కొన్న ప్రొడక్ట్‌లు స్కాన్ చేయడం
  - మీ ఎంట్రీలు చూడటం
  - విజేతలను, మీ బహుమతిని చూడటం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9ef969d93e7ac18c
confidence: ok

- how-to-win | popular: true
  q: నాకు గెలిచే అవకాశం ఎలా వస్తుంది?
  a: ప్రతి కొనుగోలుకు మీకు 1 ఎంట్రీ వస్తుంది. ఎంట్రీలను 100 చొప్పున రౌండ్లుగా లెక్కిస్తారు. **Grand Draw** అన్‌లాక్ చేయడానికి 100 ఎంట్రీలు చేరుకోండి. లక్కీ డ్రా పేజీలో **How It Works** చూడండి.
- entries-count | popular: true
  q: నా దగ్గర ఎన్ని ఎంట్రీలు ఉన్నాయి?
  a: **Lucky Draw** తెరిచి **View All Entries** నొక్కండి (ఫోన్‌లో **View Entries**). ఈ రౌండ్‌లో మీ యాక్టివ్ ఎంట్రీలను **TOTAL ENTRIES** కార్డు చూపిస్తుంది.
- not-won
  q: నాకు "Better Luck Next Time!" అని కనిపిస్తోంది
  a: ఈ డ్రాలో మీరు ఎంపిక కాలేదు. మీ కొనుగోళ్లను స్కాన్ చేస్తూ ఉండండి. ఎంట్రీలు ఎక్కువైతే తర్వాతిసారి అవకాశాలు కూడా ఎక్కువ.
- prize
  q: నేను గెలిచాను. నా బహుమతి ఎలా పొందాలి?
  a: **Winner Details** తెరవండి. **Prize Information** లో బహుమతి, దాని **Status** కనిపిస్తాయి. సహాయం కోసం, **Need Help?** బాక్స్‌లో **Contact Support** నొక్కండి.
- change-scheme
  q: స్కీమ్ మొదలయ్యే ముందు దాన్ని మార్చవచ్చా?
  a: మార్చవచ్చు. **Review Scheme** లో, ప్రొడక్ట్ రూల్ మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ (చెత్తబుట్ట) వాడండి. సిద్ధమయ్యే వరకు పబ్లిష్ చేయకుండా ఉంచడానికి **Save Draft** నొక్కండి.
:::

::: section scheme-list
title: అన్ని స్కీమ్‌లు చూడటం
routes: /Schemes
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-stats] | 2 > [data-help=sch-filters] | 3 > [data-help=sch-grid] | 4 > [data-help=sch-view] | 5 > [data-help=sch-create]
minutes: 2
source-hash: 799c692a3e1f6262
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Schemes** స్క్రీన్ ప్రతి లక్కీ డ్రా స్కీమ్‌ను చూపిస్తుంది, పైన మొత్తం లెక్కలు ఉంటాయి. సైడ్ మెనూలో **Scheme List** నొక్కి దీన్ని తెరవండి.

1. పైన ఉన్న కార్డులు చూడండి: **Total Schemes**, **Overall Dealer**, **Participating dealer**, **Winners**.
2. ఒక స్కీమ్‌ను కనుక్కోవడానికి **Search Schemes....** బాక్స్‌లో టైప్ చేయండి. రకం (**Both**, రైతు లేదా డీలర్), స్టేటస్ (**All Status**), రాష్ట్రం (**All States**) ఎంచుకోవడానికి లిస్ట్‌లు వాడండి.
3. ప్రతి స్కీమ్ కార్డులో పేరు, ఎవరి కోసమో, స్టేటస్ (ఉదాహరణకు **Active**), తేదీలు, రాష్ట్రం, జిల్లా, **Participating dealer**, **Winners** లెక్కలు, **THE GRAND REWARD** కనిపిస్తాయి.
4. ఒక కార్డు మీద **View Details** నొక్కి దాని **Scheme Overview** తెరవండి.
5. కొత్త స్కీమ్ తయారు చేయడానికి **Create Scheme** నొక్కండి.

## సాధారణ సమస్యలు

- **స్కీమ్ కనిపించడం లేదు.** సెర్చ్ బాక్స్ ఖాళీ చేసి, లిస్ట్‌లను మళ్లీ **Both**, **All Status**, **All States** కి పెట్టండి.
- **నా మెనూలో Scheme List లేదు.** మీ డెసిగ్నేషన్‌కు స్కీమ్‌ల యాక్సెస్ లేదు. మీ అడ్మిన్‌ను అడగండి.
:::

::: section add-scheme
title: స్కీమ్ తయారు చేయడం
routes: /AddScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-add-type] | 2 > [data-help=sch-add-basic] | 3 > [data-help=sch-add-rules] | 4 > [data-help=sch-add-prize] | 5 > [data-help=sch-add-draw] | 6 > [data-help=sch-add-next]
minutes: 5
source-hash: 6e0920ccd77cef79
confidence: ok

**ఈ స్క్రీన్ దేనికి**
కొత్త లక్కీ డ్రా స్కీమ్ సెట్ చేయడానికి **Create Scheme** స్క్రీన్ వాడండి: ఎవరి కోసం, ఎక్కడ నడుస్తుంది, ఏ ప్రొడక్ట్‌లు లెక్కలోకి వస్తాయి, బహుమతి ఏమిటి, డ్రా ఎప్పుడు.

1. పైన, స్కీమ్ ఎవరి కోసమో ఎంచుకోండి: **Both**, **Farmer** లేదా **Dealer**.
2. **Scheme Type & Basic Info** కింద, **Scheme Name**, **Draw points** ఎంటర్ చేసి, **State**, **District**, **Dealer** ఎంచుకోండి. **Start Date**, **End Date** ఎంచుకోండి. **Scheme Status** బాక్స్‌లో **Draft - Scheme is not published** అని కనిపిస్తుంది.
3. **Add Product Rule** కింద, **Category**, **Product** ఎంచుకుని, **Dealer Quantity**, **Farmer Quantity** ఎంటర్ చేసి, **+ Add Rule** నొక్కండి. ప్రతి రూల్ **Product Rules List** లో కనిపిస్తుంది. రూల్ డిలీట్ చేయడానికి బిన్, మార్చడానికి పెన్సిల్ వాడండి.
4. **Winner Prize Details** కింద, **Prize Name**, **Prize Description** ఎంటర్ చేసి, బహుమతి ఫోటో చేర్చడానికి **Click to upload image** నొక్కండి.
5. **Lucky Draw Configuration** కింద, **Automatic** లేదా **Manual** ఎంచుకుని, **Frequency** (**Daily**, **Weekly**, **Monthly** లేదా **Custom**) ఎంచుకుని, **Date**, **Draw Time** సెట్ చేయండి.
6. తర్వాత కోసం ఉంచాలంటే **Save Draft** నొక్కండి, లేదా స్కీమ్ చెక్ చేయడానికి **Next** నొక్కండి.

> Tip: బహుమతి పేరు, వివరణ, ఫోటో డీలర్లకు స్కీమ్ పేజీలో, విజేత మెసేజ్‌లో, డ్రా ప్రకటనలో కనిపిస్తాయి.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Scheme Name** | అందరికీ కనిపించే చిన్న పేరు. |
| **Draw points** | డ్రాకు అర్హత పొందడానికి సభ్యుడికి కావాల్సిన పాయింట్లు. |
| **State**, **District**, **Dealer** | స్కీమ్ ఎక్కడ నడుస్తుందో అది. ఒకటి కంటే ఎక్కువ ఎంచుకోవచ్చు. |
| **Dealer Quantity**, **Farmer Quantity** | ఈ రూల్ కోసం డీలర్ లేదా రైతు ఆ ప్రొడక్ట్ ఎంత కొనాలో అది. |
| **Prize Image** | ఒకే JPG లేదా PNG ఫోటో, 5 MB వరకు. |
| **Draw Time** | రోజులో డ్రా జరిగే సమయం. |

## సాధారణ సమస్యలు

- **ఫోటో అప్‌లోడ్ కావడం లేదు.** 5 MB కంటే తక్కువ ఉన్న JPG లేదా PNG వాడండి.
- **ఒక దశ వెనక్కి వెళ్లాలి.** **Previous Step** నొక్కండి.
:::

::: section review-scheme
title: స్కీమ్ చెక్ చేసి మొదలుపెట్టడం
routes: /ReviewScheme
for: 
roles: 
designations: 
minutes: 3
source-hash: 5c87d7eaecfc2110
confidence: ok

**ఈ స్క్రీన్ దేనికి**
తయారు చేసే ఫారంలో **Next** నొక్కిన తర్వాత, **Review Scheme** స్క్రీన్ కొత్త స్కీమ్ యొక్క ప్రతి వివరాన్ని ఒకే చోట చూపిస్తుంది. కాబట్టి మొదలుపెట్టే ముందు చెక్ చేసుకోవచ్చు.

1. **Review Your Scheme** బ్యానర్ చదవండి.
2. **Scheme Details** కింద, **APPLICABLE FOR:**, స్టేటస్ (**ACTIVE DRAFT**), స్కీమ్ పేరు, తయారు చేసిన తేదీ, వివరణ చెక్ చేయండి.
3. **Campine Time Line** కింద **Start Date**, **End Date**, దాని పక్కన ఉన్న బహుమతి కార్డు చెక్ చేయండి.
4. **Product Rules** టేబుల్ చెక్ చేయండి. రూల్ మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ వాడండి.
5. **Target Segment** (**State**, **District**, **Dealers**), **Review Checklist** (**Draw Points**, **Product Points**, **Draw Mode**, **Frequency**, **Draw Date**, **Draw Time**) చెక్ చేయండి.
6. పబ్లిష్ చేయకుండా ఉంచడానికి **Save Draft** నొక్కండి, లేదా స్కీమ్ మొదలుపెట్టడానికి **Active Scheme** నొక్కండి.

> Careful: **Active Scheme** నొక్కిన తర్వాత, డీలర్లకు, రైతులకు స్కీమ్ కనిపిస్తుంది, వారు ఎంట్రీలు చేర్చడం మొదలుపెట్టవచ్చు.

## సాధారణ సమస్యలు

- **ఒక వివరం తప్పుగా ఉంది.** **Active Scheme** నొక్కే ముందు, తయారు చేసే ఫారంకు వెనక్కి వెళ్లి దాన్ని సరిచేయండి.
- **నేను ఇంకా మొదలుపెట్టడానికి సిద్ధంగా లేను.** **Save Draft** నొక్కండి. స్కీమ్ డ్రాఫ్ట్‌గానే ఉంటుంది.
:::

::: section scheme-overview
title: స్కీమ్‌ను గమనించడం
routes: /SchemeOverview
for: 
roles: 
designations: 
minutes: 3
source-hash: 55ace08ba0ddb992
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Scheme Overview** స్క్రీన్ ఒక స్కీమ్ ఎలా నడుస్తోందో చూపిస్తుంది: ఎవరు పాల్గొంటున్నారు, ఎన్ని ఎంట్రీలు ఉన్నాయి, ఎవరు గెలిచారు, గతంలో జరిగిన ప్రతి డ్రా. స్కీమ్ లిస్ట్‌లో **View Details** నొక్కి దీన్ని తెరవండి.

1. పైన, స్కీమ్ పేరు, ఎవరి కోసమో, స్టేటస్ (ఉదాహరణకు **Active**), **States**, **Duration** చూడండి.
2. కార్డులు చూడండి: **Participating dealer**, **Total Entries**, **Winners**.
3. **Overview** ట్యాబ్‌లో, గ్రాండ్ రివార్డ్, **Draw Setting** చూడండి: **Draw Mode**, **Frequency**, **Next Draw Date**, **Last Draw Date**.
4. **Participating dealer** ట్యాబ్‌లో, వెతకండి లేదా ఒక రాష్ట్రం ఎంచుకోండి. ప్రతి సభ్యుడు తమ **Points**, **Entries** తో కనిపిస్తారు. ఇంకా చూడటానికి ఒక కార్డు నొక్కండి.
5. **Winners** ట్యాబ్‌లో, ప్రతి విజేతను స్టేటస్ (**Selected**, **Notified** లేదా **Claimed**), **Prize**, **Draw Date** తో చూడండి. ఇంకా చూడటానికి ఒక కార్డు నొక్కండి.
6. **Draw History** ట్యాబ్‌లో, గతంలో జరిగిన ప్రతి డ్రాను దాని తేదీ, **Completed**, బహుమతి, ఎంతమంది విజేతలు ఎంపికయ్యారో దానితో చూడండి.

## సాధారణ సమస్యలు

- **Winners ట్యాబ్ ఖాళీగా ఉంది.** ఈ స్కీమ్‌కు ఇంకా ఏ డ్రా జరగలేదు. **Overview** ట్యాబ్‌లో **Next Draw Date** చెక్ చేయండి.
- **ఒక సభ్యుడు కనిపించడం లేదు.** **All States** ఎంచుకుని, సెర్చ్ బాక్స్ ఖాళీ చేయండి.
:::

::: section scan-qr
title: ప్రొడక్ట్ QR కోడ్ స్కాన్ చేయడం
routes: /qr-scanner
for: 
roles: 
designations: 
minutes: 1
source-hash: 4ad0b8818ad680ef
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీరు కొన్న SPIC ప్రొడక్ట్ మీద ఉన్న QR కోడ్ స్కాన్ చేయడానికి **QR Scanner** వాడండి. ఇలాగే ఒక కొనుగోలు లక్కీ డ్రా కోసం లెక్కలోకి వస్తుంది.

1. ప్రొడక్ట్ మీద ఉన్న QR కోడ్ పైన ఫోన్ పట్టుకోండి. కోడ్ చతురస్రం ఫ్రేమ్ లోపల ఉండేలా చూడండి.
2. చీకటిగా ఉంటే, ఫ్లాష్ కోసం మెరుపు ఐకాన్ నొక్కండి. కెమెరా మార్చడానికి కెమెరా ఐకాన్ నొక్కండి.
3. కోడ్ చిన్నగా లేదా దూరంగా ఉంటే, జూమ్ బార్‌ను **+** వైపు జరపండి.
4. మీ దగ్గర ఇప్పటికే ఉన్న ఫోటో నుంచి కోడ్ స్కాన్ చేయడానికి, ఫోటో ఐకాన్ నొక్కండి.
5. మీ లక్కీ డ్రా ఎంట్రీలు చూడటానికి **View Entries** నొక్కండి.

## సాధారణ సమస్యలు

- **కోడ్ చదవడం లేదు.** కెమెరా తుడిచి, ఫ్లాష్ ఆన్ చేసి, ఒక చేయి దూరంలో ఫోన్‌ను కదలకుండా పట్టుకోండి.
- **కెమెరా తెరుచుకోవడం లేదు.** మీ ఫోన్ సెట్టింగ్‌లలో యాప్‌కు కెమెరా వాడే అనుమతి ఇవ్వండి.
:::

::: section scan-product
title: స్కాన్ చేసిన ప్రొడక్ట్‌లు ఎంటర్ చేయడం
routes: /Scanproduct
for: 
roles: 
designations: 
minutes: 2
source-hash: c9f9991efd4bd6c1
confidence: ok

**ఈ స్క్రీన్ దేనికి**
స్కాన్ చేసిన తర్వాత, ఈ స్క్రీన్ కంపెనీని, దాని ప్రొడక్ట్‌లను చూపిస్తుంది. ప్రతి ప్రొడక్ట్ ఎన్ని కొన్నారో ఎంటర్ చేసి సబ్మిట్ చేయండి.

1. పైన ఉన్న కంపెనీ కార్డు చెక్ చేయండి: పేరు, **VERIFIED**, అడ్రస్, ఫోన్, ఈమెయిల్.
2. **Products** కింద, మీరు కొన్న ప్రతి ప్రొడక్ట్‌ను కనుక్కోండి.
3. ఒకటి పెంచడానికి **+** నొక్కండి. ఒకటి తగ్గించడానికి **−** నొక్కండి. రెండింటి మధ్య ఉన్న నంబరే సంఖ్య.
4. **Submit** నొక్కండి.
5. మళ్లీ స్కాన్ చేయడానికి, **Back To Scan** పక్కన ఉన్న వెనక్కి బాణం నొక్కండి.

## సాధారణ సమస్యలు

- **సంఖ్య సున్నా కంటే కిందకు వెళ్లడం లేదు.** ఇది మామూలే. సున్నా అంటే మీరు ఆ ప్రొడక్ట్ కొనలేదు.
- **తప్పు కంపెనీ కనిపిస్తోంది.** **Back To Scan** కి వెళ్లి సరైన QR కోడ్ స్కాన్ చేయండి.
:::

::: section select-products
title: డీలర్ దగ్గర కొన్న ప్రొడక్ట్‌లు ఎంచుకోవడం
routes: /SelectPurchasedProducts
for: 
roles: 
designations: 
minutes: 2
source-hash: 79749dd21c644750
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Select Purchased Products** స్క్రీన్ మీరు కొన్న డీలర్‌ను, మీ లక్కీ డ్రా ప్రోగ్రెస్‌ను, డీలర్ అమ్మే ప్రొడక్ట్‌లను చూపిస్తుంది. మీరు కొన్న ప్రొడక్ట్‌లు ఎంచుకోండి.

1. డీలర్ కార్డు చెక్ చేయండి: పేరు, **VERIFIED**, అడ్రస్, ఫోన్, ఈమెయిల్.
2. **Current Progress** చూడండి: మీ **Total Lucky Draw Entries**, గ్రాండ్ డ్రా కోసం ఇంకా ఎన్ని కావాలో.
3. **Available Products** కింద, మీరు కొన్న ప్రతి ప్రొడక్ట్‌ను కనుక్కోండి.
4. ఎన్ని కొన్నారో సెట్ చేయడానికి **Qty** పక్కన ఉన్న **+**, **−** వాడండి.

## సాధారణ సమస్యలు

- **నా ప్రొడక్ట్ కనిపించడం లేదు.** ఈ స్కీమ్‌లో డీలర్ దాన్ని అమ్మకపోవచ్చు. డీలర్‌ను అడగండి.
- **ఇది తప్పు డీలర్.** వెనక్కి వెళ్లి సరైన డీలర్ QR కోడ్ స్కాన్ చేయండి.
:::

::: section lucky-draw
title: నా లక్కీ డ్రా
routes: /Luckydraw
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-ld-entry] | 2 > [data-help=sch-ld-prize] | 3 > [data-help=sch-ld-progress] | 4 > [data-help=sch-ld-how] | 5 > [data-help=sch-ld-actions]
minutes: 2
source-hash: f6f7b0a7b08535be
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ కొనుగోలు వెరిఫై అయిన తర్వాత, **Lucky Draw** పేజీ **You're in the Lucky Draw!** అని, మీరు గెలవగల బహుమతిని, గ్రాండ్ డ్రాకు మీరు ఎంత దగ్గరగా ఉన్నారో చూపిస్తుంది.

1. కొత్త ఎంట్రీ చూడండి: **+1 Lucky Draw Entry**, **Ticket Activated**, **Entry ID**, తేదీ.
2. **Grand Prize** ను దాని సుమారు విలువతో, **Previous Winners** ను చూడండి. ఇంకా విజేతలను చూడటానికి **View All** నొక్కండి.
3. **CURRENT CYCLE PROGRESS** కింద, 100 లో మీ ఎంట్రీలు, **Next Milestone**, **Grand Draw** చూడండి.
4. **How It Works** చదవండి: ప్రతి కొనుగోలుకు 1 ఎంట్రీ, 100 ఎంట్రీలు అయితే గ్రాండ్ డ్రా అన్‌లాక్ అవుతుంది.
5. ఇంకా ఎంట్రీలు చేర్చడానికి **Scan Another Product** నొక్కండి, లేదా లిస్ట్ చూడటానికి **View All Entries** నొక్కండి. ఫోన్‌లో ఈ బటన్‌లు **Scan QR**, **View Entries** అని ఉంటాయి.

## సాధారణ సమస్యలు

- **నా ఎంట్రీల సంఖ్య పెరగలేదు.** కొనుగోలు వెరిఫై అయిన తర్వాతే ఎంట్రీ చేరుతుంది. కాసేపటి తర్వాత **View All Entries** లో మళ్లీ చూడండి.
- **GOLD MILESTONE అంటే ఏమిటి?** **Grand Draw** ను అన్‌లాక్ చేసే 100 ఎంట్రీల గుర్తు అది.
:::

::: section my-entries
title: నా అన్ని ఎంట్రీలు చూడటం
routes: /LuckyDrawList
for: 
roles: 
designations: 
minutes: 2
source-hash: a976b5d2def67294
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Over All Entries** స్క్రీన్ ఈ రౌండ్‌లో మీకు ఉన్న ప్రతి లక్కీ డ్రా ఎంట్రీని, ప్రతి దాని ప్రొడక్ట్‌లు, పాయింట్లతో చూపిస్తుంది.

1. **TOTAL ENTRIES** కార్డు, 100 లో మీకు ఎన్ని యాక్టివ్ ఎంట్రీలు ఉన్నాయో 25, 50, 75, **GOLD MILESTONE** గుర్తులతో చూపిస్తుంది.
2. **Your Entries List** కింద, ఒక ట్యాబ్ ఎంచుకోండి: **All**, **Active**, **Used** లేదా **Expired**.
3. ప్రతి కార్డులో ఎంట్రీ నంబర్, దాని స్టేటస్, తేదీ, ప్రొడక్ట్‌లు, **No.of Products**, **Total Products**, **Earned Points** కనిపిస్తాయి.
4. ఆ ఎంట్రీ చూడటానికి కార్డు మీద **View Details** నొక్కండి.
5. లక్కీ డ్రా పేజీకి తిరిగి వెళ్లడానికి వెనక్కి బాణం నొక్కండి.

## సాధారణ సమస్యలు

- **లిస్ట్ ఖాళీగా ఉంది.** ఈ ట్యాబ్‌లో మీకు ఎంట్రీలు లేవు. **All** ఎంచుకోండి.
- **కొన్ని ఎంట్రీలు Expired అని చూపిస్తున్నాయి.** అవి ఇక గెలవలేవు. ఎంట్రీలు చేర్చడానికి కొత్త కొనుగోళ్లను స్కాన్ చేస్తూ ఉండండి.
:::

::: section winner-announcement
title: విజేత మెసేజ్
routes: /WinnerPopUp
for: 
roles: 
designations: 
minutes: 1
source-hash: a8394f0ccc4ee5a5
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీరు గెలిచినప్పుడు, **Congratulations!** మెసేజ్ తెరుచుకుంటుంది: **You are a Lucky Draw Winner**. అందులో మీ బహుమతి, స్కీమ్ కనిపిస్తాయి.

1. బహుమతి పేరు, స్కీమ్ పేరు చదవండి.
2. **Winner Details** తెరవడానికి **View Details** నొక్కండి.
3. లక్కీ డ్రా పేజీకి తిరిగి వెళ్లడానికి మెసేజ్ మూసివేయండి.

## సాధారణ సమస్యలు

- **పొరపాటున మెసేజ్ మూసేశాను.** సైడ్ మెనూ నుంచి **Winner Details** తెరవండి.
:::

::: section winner-details
title: నా బహుమతి వివరాలు
routes: /WinnerDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: e8beec48b189b974
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Winner Details** స్క్రీన్ మీ లక్కీ డ్రా బహుమతిని, అది మీకు ఎలా చేరుతుందో చూపిస్తుంది.

1. పైన, **You are a Winner**, **Draw ID** చూడండి.
2. **Draw Summary** ఆ డ్రాలోని **Participants**, **Winners**, **Entries** చూపిస్తుంది.
3. **Winner Information** లో **Winner Name**, **Type**, **Dealer Code**, **Mobile Number**, **Points Earned** కనిపిస్తాయి.
4. **Prize Information** లో **Prize Name**, **Prize Value**, **Status** (ఉదాహరణకు **Prize Distributed**), **Distributed Date** కనిపిస్తాయి.
5. **Scheme Information**, **Activity Information** లో స్కీమ్, **Draw Date**, ఎవరు ప్రకటించారు, ఎప్పుడు అనేవి కనిపిస్తాయి.
6. **Better Luck Next Time!** అని కనిపిస్తే, ఎంట్రీలు చేర్చడానికి **Scan QR** నొక్కండి, లేదా వాటిని చూడటానికి **View Entries** నొక్కండి.
7. మీ బహుమతి గురించి ఏ ప్రశ్న ఉన్నా, **Need Help?** బాక్స్‌లో **Contact Support** నొక్కండి.

## సాధారణ సమస్యలు

- **మొబైల్ నంబర్ తప్పుగా ఉంది.** టీమ్ మిమ్మల్ని సంప్రదించేలా **Contact Support** నొక్కండి.
- **బహుమతి ఇంకా ఇవ్వలేదు.** **Prize Information** కింద **Status** చెక్ చేయండి, లేదా **Contact Support** నొక్కండి.
:::

::: section scheme-winners
title: అన్ని స్కీమ్ విజేతలను చూడటం
routes: /CelebrationWinners
for: 
roles: 
designations: 
minutes: 1
source-hash: 633e72a075f230fb
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Scheme Winners** స్క్రీన్ గత ప్రకటనల్లోని లక్కీ డ్రా విజేతలను చూపిస్తుంది.

1. **Celebrating Our Winners!** బ్యానర్ **Total Winners Announced** చూపిస్తుంది.
2. స్కీమ్ (**All Schemes**), సంవత్సరం, రకం (**All Types**), ప్రాంతం (**All Locations**) ఎంచుకోవడానికి లిస్ట్‌లు వాడండి.
3. ఒక ట్యాబ్ ఎంచుకోండి: **All Winners**, **Dealer Winners** లేదా **Farmer Winners**.
4. ప్రతి కార్డులో విజేత, **Dealer** లేదా **Farmer**, స్కీమ్, ప్రాంతం, **Prize**, **Draw Date** కనిపిస్తాయి.
5. ఒక విజేత గురించి ఇంకా చూడటానికి **View Details** నొక్కండి.

## సాధారణ సమస్యలు

- **ఒక విజేత కనిపించడం లేదు.** **All Schemes**, **All Types**, **All Locations** ఎంచుకుని, సంవత్సరం చెక్ చేయండి.
:::
