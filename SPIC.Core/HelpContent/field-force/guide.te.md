---
module: field-force
title: ఫీల్డ్ సిబ్బంది
icon: bi-person-walking
order: 107
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
source-hash: d557872762e3ea47
confidence: ok

ప్రతి వారం ఫీల్డ్ పనిని ప్లాన్ చేయండి, దానికి ఆమోదం పొందండి, అటెండెన్స్ వేయండి, ఫీల్డ్‌లో టాస్క్‌లు చేయండి. ఎవరు పని చేస్తున్నారు, ఎక్కడ ఉన్నారు, టార్గెట్‌తో పోలిస్తే ఒక్కొక్కరు ఎంత చేశారో మేనేజర్లు చూస్తారు.

- plan | icon: bi-calendar-week | color: #ea580c
  title: JMDO వారాన్ని ప్లాన్ చేస్తారు
  text: ప్రతి రోజుకు డీలర్లను ఎంచుకుని, SPCM, Soil Sample, PoS liquidation టార్గెట్‌లు పెట్టి, ప్లాన్ పంపండి.
- approve | icon: bi-person-check | color: #2563eb
  title: MDO ఆమోదిస్తారు
  text: MDO ప్లాన్‌ను చెక్ చేసి, అవసరమైతే డీలర్లు, తేదీలు లేదా టార్గెట్‌లు మార్చి, ఆమోదిస్తారు.
- work | icon: bi-geo-alt | color: #16a34a
  title: ఫీల్డ్‌లో పని, లైవ్‌గా గమనిస్తూ
  text: JMDO అటెండెన్స్ వేసి, ప్రతి టాస్క్‌ను మొదలుపెట్టి ముగిస్తారు. మేనేజర్లు టాస్క్‌లు, లొకేషన్, రిపోర్ట్‌లను ఫాలో అవుతారు.
:::

::: section flow
title: ఒక వారం ఫీల్డ్ పని ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2462b99188302817
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- jmdo | type: lane | color: #ea580c | roles: JMDO
  title: JMDO
  sub: ఫీల్డ్ సిబ్బంది
- mdo | type: lane | color: #2563eb | roles: MDO, MO
  title: MDO
  sub: మీ HQ లో టీమ్ మేనేజర్
- plan | lane: jmdo | col: 1 | icon: bi-calendar-week | color: #d97706 | next: approve | screen: plan-week
  title: వారాన్ని ప్లాన్ చేయడం
  status: Pending
  text: ప్రతి రోజుకు డీలర్లను ఎంచుకుని, వారపు టార్గెట్‌లు ఎంటర్ చేసి, **Request** నొక్కండి. ప్లాన్ MDO కోసం ఎదురుచూస్తుంది.
- approve | lane: mdo | col: 2 | icon: bi-person-check | color: #2563eb | next: attend | screen: review-allocation
  title: చెక్ చేసి ఆమోదించడం
  status: Approved
  text: MDO డీలర్ టాస్క్‌లు కలపవచ్చు లేదా తీసేయవచ్చు, తేదీలు, ప్రాధాన్యత, టార్గెట్‌లు మార్చవచ్చు, తర్వాత **Approve Allocation** నొక్కుతారు.
- attend | lane: jmdo | col: 3 | icon: bi-calendar-check | color: #0d9488 | next: work | screen: mark-attendance
  title: అటెండెన్స్ వేయడం
  status: Present
  text: ప్రతి పని రోజు, ఏ టాస్క్ మొదలుపెట్టే ముందైనా SPIC ONE మొబైల్ యాప్‌లో అటెండెన్స్ వేయండి.
- work | lane: jmdo | col: 4 | icon: bi-play-circle | color: #7c3aed | next: close | screen: todays-tasks
  title: టాస్క్ మొదలుపెట్టడం
  status: In Progress
  text: **Today's Allocation** లో **Start** నొక్కి, తర్వాత **Start Task** నొక్కండి. సమయం, లొకేషన్ నమోదు అవుతాయి.
- close | lane: jmdo | col: 5 | icon: bi-check-circle | color: #16a34a | next: monitor | screen: todays-tasks
  title: సేవ్ చేసి ముగించడం
  status: Completed
  text: **Close-out form** నింపి, **Save Record** నొక్కి, తర్వాత టాస్క్ ముగించడానికి **Stop Task** నొక్కండి.
- monitor | lane: mdo | col: 6 | icon: bi-list-check | color: #475569 | screen: task-monitoring
  title: ఫాలో అప్
  status: Completed
  text: MDO ప్రతి టాస్క్‌ను **Task Monitoring** లో, టీమ్ లొకేషన్‌ను **Team Tracking** లో, పురోగతిని **Reports** లో చూస్తారు.
- no-attendance | lane: jmdo | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: work | to: attend | screen: mark-attendance
  title: అటెండెన్స్ వేయలేదు
  status: Yet to Start
  text: ఈరోజు అటెండెన్స్ వేయకముందు టాస్క్ మొదలుపెట్టలేరు. యాప్ మీ కోసం **Mark Attendance** తెరుస్తుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 80c93f090c1c74d0
confidence: ok

- pending | color: #d97706
  label: Pending
  text: వారపు ప్లాన్ పంపబడింది, MDO ఆమోదం కోసం ఎదురుచూస్తోంది. JMDO కి "awaiting MDO approval" అని కనిపిస్తుంది.
- approved | color: #16a34a
  label: Approved
  text: MDO వారపు ప్లాన్‌ను ఆమోదించారు. దాని టాస్క్‌లను ఆ రోజున మొదలుపెట్టవచ్చు.
- rejected | color: #dc2626
  label: Rejected
  text: వారపు ప్లాన్ ఒప్పుకోలేదు. ప్లాన్ సరిచేసి మళ్లీ పంపండి.
- requested | color: #d97706
  label: Requested
  text: Task Monitoring లో, వారపు ప్లాన్ ఇంకా ఆమోదం కోసం ఎదురుచూస్తున్న టాస్క్.
- yet-to-start | color: #64748b
  label: Yet to Start
  text: టాస్క్ ఆమోదం పొందింది కానీ ఇంకా మొదలుపెట్టలేదు.
- not-started | color: #64748b
  label: Not Started
  text: Reports లో, ఇది Yet to Start తో సమానం.
- in-progress | color: #2563eb
  label: In Progress
  text: JMDO Start Task నొక్కి, ఆ పని చేస్తున్నారు.
- completed | color: #16a34a
  label: Completed
  text: టాస్క్ ముగిసింది.
- fully | color: #16a34a
  label: Fully Completed
  text: క్లోజ్-అవుట్ ఫలితం: పని మొత్తం పూర్తయింది.
- partially | color: #d97706
  label: Partially Completed
  text: క్లోజ్-అవుట్ ఫలితం: పనిలో కొంత భాగం మాత్రమే పూర్తయింది.
- not-completed | color: #ea580c
  label: Not Completed
  text: క్లోజ్-అవుట్ ఫలితం: పని చేయలేకపోయారు.
- present | color: #16a34a
  label: Present
  text: పూర్తి పని రోజుకు అటెండెన్స్. మిమ్మల్ని On Duty చేస్తుంది.
- half-day | color: #0d9488
  label: Half Day
  text: సగం రోజుకు అటెండెన్స్. మిమ్మల్ని On Duty చేస్తుంది.
- on-leave | color: #7c3aed
  label: On Leave
  text: ఈరోజు మీరు సెలవులో ఉన్నారు. మీరు Off Duty.
- absent | color: #dc2626
  label: Absent
  text: గైర్హాజరు (absent) అని గుర్తించిన రోజుకు వారపు సారాంశంలో ఇది కనిపిస్తుంది.
- active | color: #16a34a
  label: Active
  text: టీమ్ డాష్‌బోర్డ్‌లో, ఈరోజు సెలవులో లేని JMDO.
- on-duty | color: #16a34a
  label: On Duty
  text: ఈరోజు అటెండెన్స్ Present లేదా Half Day గా వేశారు.
- off-duty | color: #64748b
  label: Off Duty
  text: ఈరోజు ఇంకా అటెండెన్స్ వేయలేదు, లేదా On Leave.
- live | color: #16a34a
  label: Live
  text: Live Tracking ఆన్‌లో ఉంది, లొకేషన్ ఇప్పుడు వస్తోంది.
- stale | color: #d97706
  label: Stale
  text: Live Tracking ఆన్‌లో ఉంది, కానీ గత కొన్ని నిమిషాల్లో కొత్త లొకేషన్ రాలేదు.
- offline | color: #dc2626
  label: Offline
  text: Live Tracking ఆన్‌లో ఉంది, కానీ 15 నిమిషాలకు పైగా లొకేషన్ ఏదీ రాలేదు.
- auto-ended | color: #475569
  label: Ended automatically
  text: చాలా సేపు ట్రాకింగ్ నుంచి ఏమీ రాలేదు, అందుకే యాప్ దాన్ని ఆపేసింది.
- stopped | color: #64748b
  label: Stopped
  text: JMDO Stop Tracking నొక్కారు.
- never | color: #94a3b8
  label: Never started
  text: JMDO ఇంకా Live Tracking మొదలుపెట్టలేదు.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: ca8f05faf80da07f
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- jmdo | color: #ea580c | roles: JMDO
  title: JMDO (ఫీల్డ్ సిబ్బంది)
  - వారాన్ని ప్లాన్ చేసి Request నొక్కడం
  - ప్రతి పని రోజు అటెండెన్స్ వేయడం
  - ప్రతి టాస్క్‌ను మొదలుపెట్టడం, సేవ్ చేయడం, ముగించడం
  - Live Tracking మొదలుపెట్టడం, ఆపడం
  - My Reports చూడటం
- mdo | color: #2563eb | roles: MDO, MO
  title: MDO
  - మీ JMDO ల వారపు ప్లాన్‌లను ఆమోదించడం
  - ప్రతి JMDO కి సంవత్సర టార్గెట్‌లు పెట్టడం
  - టాస్క్‌లు, టీమ్ లొకేషన్, రిపోర్ట్‌లను ఫాలో అవ్వడం
- admin | color: #475569 | roles: Admin
  title: Admin
  - అన్ని HQ ల ప్లాన్‌లు చూడటం
  - ఆమోదం వచ్చిన తర్వాత కూడా ప్లాన్‌ను సరిచేయడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e1bd21cb8311eb35
confidence: ok

- cannot-start | popular: true
  q: నా టాస్క్ మొదలుపెట్టలేకపోతున్నాను
  a: రెండు విషయాలు సరిగ్గా ఉండాలి. మీ వారపు ప్లాన్‌ను MDO **Approved** చేసి ఉండాలి, మీరు ఈరోజు అటెండెన్స్ వేసి ఉండాలి. అటెండెన్స్ లేకపోతే, యాప్ మీ కోసం **Mark Attendance** తెరుస్తుంది.
- web-attendance | popular: true
  q: మొబైల్ యాప్ నుంచి మాత్రమే వేయగలరు అని Mark Attendance చెబుతోంది
  a: Attendance, Live Tracking, Track Playback మీ ఫోన్‌లోని SPIC ONE యాప్‌లో మాత్రమే పని చేస్తాయి. ఫోన్‌లో యాప్ తెరిచి మళ్లీ ప్రయత్నించండి.
- already-sent
  q: "You've already submitted an allocation for that week." (ఆ వారానికి మీరు ఇప్పటికే ప్లాన్ పంపారు)
  a: వారానికి ఒక ప్లాన్ మాత్రమే పంపగలరు. డీలర్లను కలపడానికి లేదా తీసేయడానికి, లేదా తేదీలు, టార్గెట్‌లు మార్చడానికి, **Allocation Requests** స్క్రీన్‌లో చేయమని మీ MDO ని అడగండి.
- spcm-not-closed
  q: టాస్క్ ముగించబోతే "SPCM target not yet reached" (SPCM టార్గెట్ ఇంకా చేరలేదు) అని వస్తోంది
  a: SPCM, PoS liquidation టాస్క్‌లు వారపు టార్గెట్ చేరినప్పుడే ముగుస్తాయి. వేరే రోజుల్లో కూడా రికార్డులు సేవ్ చేస్తూ ఉండండి. టార్గెట్ చేరగానే టాస్క్ దానంతట అదే ముగుస్తుంది.
- no-jmdos
  q: నాకు "No JMDOs found for your headquarters" (మీ HQ లో JMDO లు లేరు) అని కనిపిస్తోంది
  a: మీ HQ లోనే ఉన్న JMDO లే మీ టీమ్. ఎవరైనా కనిపించకపోతే, వారి ప్రొఫైల్‌లో HQ చెక్ చేయమని అడ్మిన్‌ని అడగండి.
- offline
  q: Team Tracking లో ఒక JMDO కి Stale లేదా Offline అని చూపిస్తోంది
  a: ఆ JMDO ఫోన్ నుంచి కొంత సేపటిగా లొకేషన్ రాలేదు. సిగ్నల్ లేకపోవచ్చు, లేదా యాప్ మూసి ఉండవచ్చు. JMDO కి ఫోన్ చేసి యాప్ తెరవమని చెప్పండి.
:::

::: section field-dashboard
title: నా ఫీల్డ్ డాష్‌బోర్డ్
routes: /FieldDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: ad0c345e7985b577
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**My Dashboard** అనేది JMDO హోమ్ స్క్రీన్. మీరు డ్యూటీలో ఉన్నారా, ఈరోజు ఎన్ని టాస్క్‌లు ఉన్నాయి, Live Tracking, మీరు పూర్తి చేయని టాస్క్‌లు ఇందులో కనిపిస్తాయి.

1. పైన మీ పేరు, **On Duty** లేదా **Off Duty** చూడండి. అటెండెన్స్ వేయకపోతే, **Mark Attendance** నొక్కండి.
2. కార్డులు చూడండి: **Today's Tasks**, **Yet to Start**, **In Progress**, **Completed**.
3. Live Tracking ఆన్ చేయడానికి, **Start Live Tracking** నొక్కండి. **Background location access** మెసేజ్ చదివి **Continue** నొక్కండి.
4. ట్రాకింగ్ ఆన్‌లో ఉన్నప్పుడు, మీ రూట్ చూడటానికి **View Route** నొక్కండి. ఆపడానికి **Stop Tracking**, తర్వాత మళ్లీ **Stop Tracking** నొక్కండి. **Keep Tracking** రద్దు చేస్తుంది.
5. **Continue where you left off** కింద, జరుగుతున్న టాస్క్‌కు తిరిగి వెళ్లడానికి **view** నొక్కండి. **View All** నొక్కితే **Tasks Allocation** తెరుచుకుంటుంది.

> Tip: మీరు బటన్ నొక్కినప్పుడు మాత్రమే Live Tracking మొదలవుతుంది. అటెండెన్స్‌తో లేదా టాస్క్‌లతో అది దానంతట అదే మొదలవ్వదు.

## సాధారణ సమస్యలు

- **"Mark today's attendance first to enable Start Live Tracking."** (Start Live Tracking కోసం ముందు ఈరోజు అటెండెన్స్ వేయండి) అటెండెన్స్ వేసి, తర్వాత తిరిగి రండి.
- **"Mobile app only".** (మొబైల్ యాప్‌లో మాత్రమే) మీ ఫోన్‌లోని SPIC ONE యాప్ నుంచి మాత్రమే Live Tracking మొదలుపెట్టగలరు.
- **"Nothing in progress right now".** (ఇప్పుడు ఏదీ జరగడం లేదు) మీరు ఏ టాస్క్ మొదలుపెట్టలేదు. ఒకటి మొదలుపెట్టడానికి **Tasks Allocation** తెరవండి.
:::

::: section plan-week
title: నా వారపు టాస్క్‌లను ప్లాన్ చేసి రిక్వెస్ట్ పంపడం
routes: /TasksAllocation
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-week] | 2 > [data-help=ff-steps] | 3 > [data-help=ff-days] | 4 > [data-help=ff-filters] | 5 > [data-help=ff-dealers] | 6 > [data-help=ff-next]
minutes: 4
source-hash: 3effab3c5a7b4e53
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Tasks Allocation** లోని **All Allocations** ట్యాబ్‌లో, JMDO ఒక వారాన్ని (ఆదివారం నుంచి శనివారం వరకు) ఐదు దశల్లో ప్లాన్ చేసి, ఆమోదం కోసం MDO కి పంపుతారు.

1. మీరు ప్లాన్ చేయాల్సిన వారాన్ని ఎంచుకోవడానికి **Previous Week**, **Next Week** వాడండి.
2. ఎడమ వైపు ఉన్న దశలు: **Dealer**, **SPCM**, **Soil Sample**, **MD Program**, **PoS liquidation**. ఒక దశను తెరవడానికి దాన్ని నొక్కండి.
3. **Dealer** దశలో, మీరు ప్లాన్ చేస్తున్న రోజును ఎంచుకోవడానికి ఒక రోజు ట్యాబ్ నొక్కండి.
4. సెర్చ్ బాక్స్ (**Search dealer name or code...**) లేదా **All Districts**, **All Sub Districts**, **All Dealers** లిస్ట్‌లతో డీలర్లను కనుక్కోండి.
5. ఆ రోజుకు డీలర్‌ను ఎంచుకోవడానికి వారి కార్డు నొక్కండి. తీసేయడానికి మళ్లీ నొక్కండి. మీరు ఎంత మంది డీలర్లను ఎంచుకున్నారో రోజు ట్యాబ్‌పై కనిపిస్తుంది.
6. **Next: SPCM Target** నొక్కండి. SPCM కి, Soil Sample కి **This Week's Target** ఎంటర్ చేయండి, MD programs చెక్ చేయండి, PoS liquidation సంఖ్యలు (**Urea**, **DAP**, **NPKS**, **Others**) ఎంటర్ చేయండి, తర్వాత **Request** నొక్కండి.

> Tip: CSR1 లో మీకు ఇచ్చిన MD programs దానంతట అవే కలుస్తాయి. వాటిని ఇక్కడ కలపలేరు, తీసేయలేరు.
> Careful: వారానికి ఒక ప్లాన్ మాత్రమే పంపగలరు. **Request** తర్వాత, MDO మాత్రమే దాన్ని మార్చగలరు.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **This Week's Target** (SPCM) | ఈ వారం ఎన్ని SPCM సాధించాలని ప్లాన్ చేస్తున్నారు. |
| **This Week's Target** (Soil Sample) | ఈ వారం ఎన్ని మట్టి నమూనాలు సేకరించాలని ప్లాన్ చేస్తున్నారు. |
| **Urea**, **DAP**, **NPKS**, **Others** | ఈ వారం ప్రతి ప్రొడక్ట్‌కు మీరు ప్లాన్ చేసే PoS liquidation సంఖ్య. |

## సాధారణ సమస్యలు

- **"Fill in at least one step before submitting."** (పంపే ముందు కనీసం ఒక దశ నింపండి) కనీసం ఒక డీలర్‌ను ఎంచుకోండి, లేదా కనీసం ఒక టార్గెట్ ఎంటర్ చేయండి.
- **"You've already submitted an allocation for that week."** (ఆ వారానికి మీరు ఇప్పటికే ప్లాన్ పంపారు) **Allocation Requests** లో ప్లాన్ మార్చమని మీ MDO ని అడగండి.
- **"Allocation submitted — awaiting MDO approval".** (ప్లాన్ పంపబడింది, MDO ఆమోదం కోసం ఎదురుచూస్తోంది) ప్లాన్ వెళ్లిపోయింది. MDO ఆమోదించిన తర్వాత టాస్క్‌లు **Today's Allocation** లో తెరుచుకుంటాయి.
- **"Allocation Rejected".** (ప్లాన్ తిరస్కరించబడింది) **Update & Resubmit** నొక్కి, ప్లాన్ సరిచేసి, మళ్లీ **Request** నొక్కండి.
:::

::: section mark-attendance
title: ఈరోజు అటెండెన్స్ వేయడం
routes: /MarkAttendance
for: 
roles: 
designations: 
minutes: 2
source-hash: 8b14e6f020b3c82e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఏ టాస్క్ మొదలుపెట్టే ముందైనా, ప్రతి పని రోజు మీ అటెండెన్స్ వేయండి. మీరు ఎక్కడ ఉన్నారో ఇది నమోదు చేస్తుంది కాబట్టి, మీ ఫోన్‌లోని SPIC ONE యాప్‌లో మాత్రమే ఇది పని చేస్తుంది.

1. మెనూ నుంచి **Mark Attendance** తెరవండి, లేదా **My Dashboard** లో **Mark Attendance** నొక్కండి.
2. మీ లొకేషన్ వాడటానికి యాప్‌కు అనుమతి ఇవ్వండి. **Visit Area / Place** మీ లొకేషన్ నుంచి నిండుతుంది. అవసరమైతే సరిచేయండి, లొకేషన్ దొరకకపోతే టైప్ చేయండి.
3. **Date** చెక్ చేయండి.
4. **Attendance Status** ఎంచుకోండి: **Present**, **Half Day** లేదా **On Leave**.
5. **Update Attendance** నొక్కండి. "Attendance updated." అని కనిపిస్తుంది, తర్వాత **My Dashboard** కి తిరిగి వెళ్తారు.
6. అదే రోజు తర్వాత మార్చడానికి, **Attendance marked today** పక్కన ఉన్న **Edit** నొక్కండి.
7. కుడి వైపు, **Current Status** (**On Duty** లేదా **Off Duty**), **Weekly Summary** చూడండి: ప్రతి రోజు అటెండెన్స్, పని గంటలు, మొదలుపెట్టిన టాస్క్‌లు, ప్రయాణించిన దూరం.

> Careful: ఉదయం 10:00 తర్వాత స్క్రీన్‌పై "Submitted late for today" (ఈరోజు ఆలస్యంగా వేశారు) అని చూపిస్తుంది. అయినా మీరు సేవ్ చేయవచ్చు.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Date** | ఈరోజు తేదీ. తప్పనిసరి. |
| **Visit Area / Place** | మీరు పని చేస్తున్న గ్రామం లేదా ఊరు. లొకేషన్ దొరకనప్పుడు తప్పనిసరి. |
| **Attendance Status** | **Present**, **Half Day** లేదా **On Leave**. |

## సాధారణ సమస్యలు

- **"Attendance can only be marked from the SPIC mobile app".** (SPIC మొబైల్ యాప్ నుంచి మాత్రమే అటెండెన్స్ వేయగలరు) మీ ఫోన్‌లో SPIC ONE తెరవండి.
- **"Location could not be captured. Please type your Visit Area / Place to save attendance."** (లొకేషన్ దొరకలేదు. అటెండెన్స్ సేవ్ చేయడానికి ప్రదేశం టైప్ చేయండి) లొకేషన్ ఆన్ చేయండి, లేదా ప్రదేశాన్ని మీరే టైప్ చేయండి.
- **"Date is required."** (తేదీ తప్పనిసరి) **Update Attendance** నొక్కే ముందు తేదీ ఎంచుకోండి.
:::

::: section todays-tasks
title: ఈరోజు టాస్క్‌లను మొదలుపెట్టడం, సేవ్ చేయడం, ముగించడం
routes: /TaskDetail
for: 
roles: 
designations: 
minutes: 4
source-hash: 163e059a3e7fb61f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఆమోదం వచ్చిన ప్రతి టాస్క్ తన సొంత స్క్రీన్‌లో తెరుచుకుంటుంది. మీరు దాన్ని మొదలుపెట్టి, ఏమి చేశారో నమోదు చేసి, ముగిస్తారు. టాస్క్‌పై గడిపిన సమయం, మీరు ఎక్కడ మొదలుపెట్టారో యాప్ నమోదు చేస్తుంది.

1. **Tasks Allocation** లో, **Today's Allocation** తెరవండి. **Yet to Start**, **In Progress**, **Completed** లేదా **All** ఎంచుకుని, ఒక టాస్క్ కార్డుపై **Start** (లేదా **Continue**) నొక్కండి.
2. **Task Timeline** (**Assigned**, **Requested**, **Approved**, **Started**, **Closed**), టాస్క్ వివరాలు చెక్ చేయండి.
3. **Start Task** నొక్కండి. యాప్ మీ లొకేషన్ నమోదు చేసి **Time on task** గడియారం మొదలుపెడుతుంది.
4. **Close-out form** లో **Outcome** ఎంచుకోండి. డీలర్ అయితే, **+ Add another Product** నొక్కి **Product Name**, **Stock** కలపండి. SPCM అయితే, **Achieved Target** ఎంటర్ చేయండి. PoS liquidation అయితే, **Urea**, **NPKS**, **DAP**, **Other** ఎంటర్ చేయండి.
5. **Remarks** రాసి, **Photo Proof** (డీలర్) లేదా **Supporting Document** జత చేసి, **Save Record** నొక్కండి. సేవ్ చేసిన రికార్డులు కుడి వైపు ఒక టేబుల్‌లో కనిపిస్తాయి.
6. విజిట్‌కు అవసరమైతే, **Quick actions** వాడండి: **Soil Sample Collection** లేదా **Add Farmer Data**.
7. పని అయిపోయాక, **Stop Task** నొక్కి, తర్వాత **Close Task** నొక్కండి.

> Careful: ముగించిన టాస్క్‌ను మళ్లీ మొదలుపెట్టలేరు.
> Tip: వారపు టార్గెట్ చేరగానే SPCM, PoS liquidation దానంతట అవే ముగుస్తాయి ("Record saved - weekly target reached, task completed!").

## సాధారణ సమస్యలు

- **"Allocation Awaiting MDO Approval".** (ప్లాన్ MDO ఆమోదం కోసం ఎదురుచూస్తోంది) MDO మీ వారపు ప్లాన్‌ను ఆమోదించే వరకు **Start Task** బటన్ బూడిద రంగులో ఉంటుంది.
- **"Mark today's attendance before starting a task."** (టాస్క్ మొదలుపెట్టే ముందు ఈరోజు అటెండెన్స్ వేయండి) యాప్ **Mark Attendance** తెరుస్తుంది. అటెండెన్స్ వేసి, తర్వాత టాస్క్ మొదలుపెట్టండి.
- **"Please start the task before saving records."** (రికార్డులు సేవ్ చేసే ముందు టాస్క్ మొదలుపెట్టండి) ముందు **Start Task** నొక్కండి.
- **Soil Sample కార్డు Sample Collection ని తెరుస్తోంది.** ఇది మామూలే. మట్టి నమూనాలు sample collection స్క్రీన్‌లలో నమోదు అవుతాయి.
:::

::: section task-history
title: నా టాస్క్ చరిత్ర, వారపు సారాంశం చూడటం
routes: /TasksAllocation
for: 
roles: 
designations: 
minutes: 2
source-hash: 5bb6d9cb3b6d9f29
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Tasks Allocation** లోని **Task History**, **Task Summary** ట్యాబ్‌లు ప్రతి వారం మీ టాస్క్‌లను, ప్రతి వారపు టార్గెట్‌లో ఎంత పూర్తయిందో చూపిస్తాయి.

1. **Tasks Allocation** లో, **Task History** తెరవండి. అది ఈరోజు టాస్క్‌లతో తెరుచుకుంటుంది.
2. **From**, **To**, **Dealer**, **Status** ఎంచుకుని, **Search** నొక్కండి. **Today** నొక్కితే ఈరోజుకు తిరిగి వస్తుంది.
3. ప్రతి కార్డులో టాస్క్, దాని స్టేటస్ కనిపిస్తాయి. దాన్ని తెరవడానికి **View Details** లేదా **Continue** నొక్కండి.
4. **Task Summary** తెరవండి. కార్డుల్లో **Total Assigned**, **Yet to Start**, **In Progress**, **Completed** కనిపిస్తాయి.
5. ప్రతి వారంలో దాని ప్లాన్ స్టేటస్, **Complete** శాతం, **Dealer Visits**, **MD Programs**, ప్రతి టార్గెట్‌కు **Target**, **Achieved**, **Pending** కనిపిస్తాయి.
6. **Day-wise Dealer Visits** కింద, ప్రతి రోజు ఎన్ని డీలర్ విజిట్‌లు అయ్యాయి, ఎన్ని మిగిలి ఉన్నాయో చూడండి.

## సాధారణ సమస్యలు

- **"No tasks match this filter".** (ఈ ఫిల్టర్‌కు సరిపడే టాస్క్‌లు లేవు) తేదీల పరిధి పెంచండి, లేదా **Status** ని **All** గా పెట్టండి.
- **"Detailed view for this task type isn't built yet."** (ఈ టాస్క్ రకానికి వివరాల స్క్రీన్ ఇంకా తయారు కాలేదు) Soil Sample టాస్క్‌లకు ఇక్కడ వివరాల స్క్రీన్ లేదు.
:::

::: section my-reports
title: నా రిపోర్ట్‌లు
routes: /FieldReports
for: 
roles: 
designations: 
minutes: 1
source-hash: 1d05a6505b4982e6
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఈ నెల మీరు ఎన్ని టాస్క్‌లు ముగించారు, ఎంత త్వరగా ముగించారో **My Reports** చూపిస్తుంది.

1. కార్డులు చూడండి: **Completed**, **Avg. Close Time**, **Soil Samples**, **Farmers Added**.
2. **Task closures** టేబుల్‌లో ముగిసిన ప్రతి టాస్క్ **Date**, **Task**, **Type**, **Outcome** తో ఉంటుంది.
3. ఒక టాస్క్ కనుక్కోవడానికి **Search...** లో టైప్ చేయండి.
4. లిస్ట్‌ను సేవ్ చేయడానికి లేదా ప్రింట్ చేయడానికి **Copy**, **CSV**, **Excel**, **PDF** లేదా **Print** వాడండి.

## సాధారణ సమస్యలు

- **"Nothing closed out this month yet".** (ఈ నెల ఇంకా ఏదీ ముగించలేదు) ఈ నెల మీరు ఏ టాస్క్ ముగించలేదు.
:::

::: section track-playback
title: నా ట్రాకింగ్ రూట్‌ను మళ్లీ చూడటం
routes: /TrackPlayback
for: 
roles: 
designations: 
minutes: 2
source-hash: cd8704c91e7b24e5
confidence: ok

**ఈ స్క్రీన్ దేనికి**
గత 7 రోజుల మీ Live Tracking రూట్‌ను **Track Playback** మ్యాప్‌పై చూపిస్తుంది. మీ ఫోన్‌లోని SPIC ONE యాప్‌లో మాత్రమే ఇది పని చేస్తుంది.

1. మెనూ నుంచి **Track Playback** తెరవండి, లేదా **My Dashboard** లో **View Route**, లేదా **Mark Attendance** లో **View Tracking Details** నొక్కండి.
2. ట్రాకింగ్ ఆన్‌లో ఉంటే, **Tracking Active** కార్డులో సమయం కనిపిస్తుంది. ఆపడానికి **Stop Tracking** నొక్కండి, లేదా దారి చూపించడానికి **Directions to live location** నొక్కండి.
3. **Other playbacks available** కింద, ఒక రోజు రూట్ తీసుకురావడానికి ఆ రోజును నొక్కండి.
4. మ్యాప్‌పై play నొక్కండి, రూట్ వెంబడి కదలడానికి స్లైడర్‌ను లాగండి, వేగం ఎంచుకోండి (**1x**, **2x** లేదా **4x**).

## సాధారణ సమస్యలు

- **"No tracking sessions in the last 7 days".** (గత 7 రోజుల్లో ట్రాకింగ్ లేదు) రూట్ నమోదు చేయడానికి **My Dashboard** నుంచి Live Tracking మొదలుపెట్టండి.
- **"Track Playback can only be used from the SPIC mobile app".** (Track Playback ని SPIC మొబైల్ యాప్ నుంచి మాత్రమే వాడగలరు) మీ ఫోన్‌లో SPIC ONE తెరవండి.
:::

::: section team-dashboard
title: టీమ్ డాష్‌బోర్డ్
routes: /MdoDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: f544adf24e2f0b9c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఒకే HQ లోని JMDO ల టాస్క్ పని మొత్తం ఒకే చూపులో **Team Dashboard** MDO కి చూపిస్తుంది.

1. కార్డులు చూడండి: **Total Tasks**, **Unassigned**, **Requested by JMDO**, **Approved · Today**, **In Progress**, **Completed**.
2. **JMDO-wise summary** లో, ప్రతి JMDO ని **Active** లేదా **On Leave** తో, **Assigned**, **In Progress**, **Completed** సంఖ్యలతో చూడండి.
3. వారపు ప్లాన్‌లను ఆమోదించడానికి **Allocation Requests** నొక్కండి.
4. తాజా సంఖ్యలు తీసుకురావడానికి **Refresh** నొక్కండి.

> Tip: ఏ వారపు ప్లాన్ పంపని JMDO లను **Unassigned** లెక్కపెడుతుంది. ఈరోజు డ్యూటీలో ఉన్న JMDO లను **In Progress** లెక్కపెడుతుంది.

## సాధారణ సమస్యలు

- **"No JMDOs found for your headquarters".** (మీ HQ లో JMDO లు లేరు) మీ HQ లోనే ఉన్న JMDO ఎవరూ లేరు. ప్రొఫైల్‌లలో HQ చెక్ చేయమని అడ్మిన్‌ని అడగండి.
:::

::: section allocation-requests
title: ఆమోదం కోసం ఎదురుచూస్తున్న వారపు ప్లాన్‌లు చూడటం
routes: /AllocationRequests
for: 
roles: 
designations: 
minutes: 2
source-hash: d233ce1bd4e689c1
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ JMDO లు ఆమోదం కోసం పంపిన వారపు ప్లాన్‌లను **Allocation Requests** లిస్ట్ చేస్తుంది. అడ్మిన్‌కు ఆమోదం వచ్చిన ప్లాన్‌లు, ఒక **Status** కాలమ్ కూడా కనిపిస్తాయి.

1. పైన ఉన్న నీలి బాక్స్ చదవండి. ఎంత మంది JMDO లు మీ ఆమోదం కోసం ఎదురుచూస్తున్నారో అది చెబుతుంది.
2. **Search...**, **All Districts**, **All Sub Districts**, **All JMDOs** తో లిస్ట్‌ను తగ్గించండి.
3. ప్రతి వరుసలో **Submitted**, **Dealer Tasks**, **SPCM Target**, **Soil Target**, **MD Programs**, **Pos Liquidation** కనిపిస్తాయి.
4. ఒక ప్లాన్ తెరవడానికి **Review** నొక్కండి. ఆమోదం వచ్చిన ప్లాన్‌కు, అడ్మిన్‌కు దాని బదులు **Edit** కనిపిస్తుంది.
5. లిస్ట్‌ను సేవ్ చేయడానికి లేదా ప్రింట్ చేయడానికి **Copy**, **CSV**, **Excel**, **PDF** లేదా **Print** వాడండి.

## సాధారణ సమస్యలు

- **"No pending allocation requests".** (పెండింగ్ ప్లాన్ రిక్వెస్ట్‌లు లేవు) ఇప్పుడు ఏ JMDO మీ కోసం ఎదురుచూడటం లేదు.
- **"No allocation requests match these filters".** (ఈ ఫిల్టర్‌లకు సరిపడే రిక్వెస్ట్‌లు లేవు) ఫిల్టర్‌లను మళ్లీ **All Districts**, **All Sub Districts**, **All JMDOs** గా పెట్టండి.
:::

::: section review-allocation
title: వారపు ప్లాన్‌ను చెక్ చేసి ఆమోదించడం
routes: /AllocationRequestReview
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-ar-dealers] | 2 > [data-help=ff-ar-add] | 3 > [data-help=ff-ar-targets] | 4 > [data-help=ff-ar-programs] | 5 > [data-help=ff-ar-approve]
minutes: 3
source-hash: 791e48a894306411
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Allocation Requests** నుంచి **Review** తో ఒక ప్లాన్ తెరవండి. ఆమోదించే ముందు దాన్ని సరిచేయవచ్చు. మీరు ఆమోదించగానే JMDO టాస్క్‌లు మొదలుపెట్టవచ్చు.

1. **Dealer Tasks** లో, ప్రతి డీలర్‌ను చెక్ చేయండి. **Date** (అది ప్లాన్ చేసిన వారంలోనే ఉండాలి) లేదా **Priority** (**Normal** లేదా **High Priority**) మార్చండి. బిన్ (చెత్తబుట్ట) నొక్కి, తర్వాత **Remove** నొక్కితే టాస్క్ తీసేయబడుతుంది.
2. డీలర్లను కలపడానికి, **Add dealer task(s)** కింద వారిని ఎంచుకుని, **Date**, **Priority** ఎంచుకుని, **Add** నొక్కండి.
3. **SPCM Target**, **Soil Sample Target**, **PoS Liquidation Targets** (**Urea**, **DAP**, **NPKS**, **Other**) చెక్ చేయండి. బాక్స్ నుంచి బయటకు రాగానే మార్పు సేవ్ అవుతుంది.
4. **MD Programs** చెక్ చేయడానికి మాత్రమే చూపిస్తారు. అవి CSR1 నుంచి వస్తాయి, ఇక్కడ మార్చలేరు.
5. కింద ఉన్న సారాంశం లైన్ చదివి, **Approve Allocation** నొక్కి, తర్వాత **Yes, Approve** నొక్కండి.

> Careful: డీలర్ టాస్క్‌ను తీసేస్తే మళ్లీ తిరిగి తేలేరు.

## సాధారణ సమస్యలు

- **"The date must fall within the JMDO's allocated week"** (తేదీ JMDO ప్లాన్ చేసిన వారంలోనే ఉండాలి) **Dealer Tasks** కింద చూపిన వారంలోని తేదీని ఎంచుకోండి.
- **"Already assigned on that date"** (ఆ తేదీకి ఇప్పటికే కేటాయించారు) ఆ డీలర్ ఆ రోజుకు ఇప్పటికే ప్లాన్‌లో ఉన్నారు.
- **"Couldn't load this allocation."** (ఈ ప్లాన్ తెరవలేకపోయాం) **Back to all requests** తో వెనక్కి వెళ్లి, ప్లాన్‌ను మళ్లీ తెరవండి.
:::

::: section task-monitoring
title: నా టీమ్ ప్రతి టాస్క్‌ను ఫాలో అవ్వడం
routes: /TaskMonitoring
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-tm-filters] | 2 > [data-help=ff-tm-apply] | 3 > [data-help=ff-tm-search] | 4 > [data-help=ff-tm-refresh]
minutes: 2
source-hash: 0c1b0fa3ecdc07f2
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ JMDO ల ప్రతి టాస్క్‌ను **Task Monitoring** లిస్ట్ చేస్తుంది: ఎవరి దగ్గర ఉంది, దాని రకం, ఎక్కడ, ఎప్పటికి పూర్తి కావాలి, దాని స్టేటస్. ఇది ఈరోజు పూర్తి కావాల్సిన టాస్క్‌లతో తెరుచుకుంటుంది.

1. **Filters** కింద, ఒకటి లేదా ఎక్కువ **JMDO**, **Task Type**, **Status**, **State** లేదా **Region** ఎంచుకుని, **Date Range** పెట్టండి.
2. సరిపడే టాస్క్‌లు చూపించడానికి **Apply Filters** నొక్కండి. **Clear All** తేదీలతో సహా అన్ని ఫిల్టర్‌లు తీసేస్తుంది.
3. **Tasks** టేబుల్‌లో, లిస్ట్ తగ్గించడానికి **Search...** లో టైప్ చేయండి. **Requested** టాస్క్‌కు ఒక కన్ను ఐకాన్ ఉంటుంది, అది ఆమోదం కోసం దాని వారపు ప్లాన్‌ను తెరుస్తుంది.
4. తాజా స్టేటస్ తీసుకురావడానికి **Refresh** నొక్కండి.

## సాధారణ సమస్యలు

- **"No tasks match these filters".** (ఈ ఫిల్టర్‌లకు సరిపడే టాస్క్‌లు లేవు) అన్ని తేదీల టాస్క్‌లు చూడటానికి **Clear All** నొక్కండి.
- **"No tasks yet".** (ఇంకా టాస్క్‌లు లేవు) మీ HQ లోని ఏ JMDO వారపు ప్లాన్ పంపలేదు.
:::

::: section team-tracking
title: నా టీమ్ ఎక్కడ ఉందో చూడటం
routes: /TeamTracking
for: 
roles: 
designations: 
minutes: 1
source-hash: 158915a8c6a5812a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ HQ లోని JMDO ల Live Tracking స్టేటస్‌ను **Team Tracking** చూపిస్తుంది.

1. **JMDO tracking status** టేబుల్‌లో, ప్రతి JMDO **Status** చూడండి: **Live**, **Stale**, **Offline**, **Stopped**, **Ended automatically** లేదా **Never started**.
2. చివరగా వచ్చిన లొకేషన్ సమయం, ప్రదేశం **Last Seen**, **Last Location** లో కనిపిస్తాయి.
3. JMDO ఎప్పుడు ట్రాకింగ్ మొదలుపెట్టారో **Session Started** చూపిస్తుంది.
4. ఒక JMDO ని కనుక్కోవడానికి **Search...** లో టైప్ చేయండి, తాజా స్టేటస్ కోసం **Refresh** నొక్కండి.

## సాధారణ సమస్యలు

- **ఒక JMDO కి "Never started" అని చూపిస్తోంది.** ఆ JMDO యాప్‌లో Live Tracking మొదలుపెట్టలేదు.
- **Stale లేదా Offline.** ఫోన్ నుంచి కొంత సేపటిగా లొకేషన్ రాలేదు. JMDO కి ఫోన్ చేయండి.
:::

::: section yearly-targets
title: JMDO లకు సంవత్సర టార్గెట్‌లు పెట్టడం
routes: /TargetAchievement
for: 
roles: 
designations: 
minutes: 2
source-hash: e1cdc1ea7b68c98e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Yearly Target Allocation** లో, ఆర్థిక సంవత్సరానికి (ఏప్రిల్ నుంచి మార్చి వరకు) ప్రతి JMDO SPCM, Soil Sample టార్గెట్‌ను MDO పెడతారు.

1. **Allocate target to a JMDO** కింద, **JMDO** ని ఎంచుకోండి. ఇప్పటికే సేవ్ చేసిన టార్గెట్ దానంతట అదే నిండుతుంది.
2. **SPCM Target (yearly)**, **Soil Sample Target (yearly)** ఎంటర్ చేయండి.
3. **Save Target Allocation** నొక్కండి.
4. **Current yearly targets** టేబుల్‌లో ప్రతి JMDO టార్గెట్ కనిపిస్తుంది. ఒక వరుసను ఫారంలోకి తెచ్చి మార్చడానికి పెన్సిల్ నొక్కండి.

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **JMDO** | ఫీల్డ్ ఆఫీసర్. తప్పనిసరి. |
| **SPCM Target (yearly)** | పూర్ణ సంఖ్య, సున్నా లేదా ఎక్కువ. తప్పనిసరి. |
| **Soil Sample Target (yearly)** | పూర్ణ సంఖ్య, సున్నా లేదా ఎక్కువ. తప్పనిసరి. |

## సాధారణ సమస్యలు

- **"Enter valid, non-negative SPCM and Soil Sample targets."** (సరైన, మైనస్ కాని SPCM, Soil Sample టార్గెట్‌లు ఎంటర్ చేయండి) సున్నా లేదా ఎక్కువ ఉన్న పూర్ణ సంఖ్యలు వాడండి.
- **Save బటన్ బూడిద రంగులో ఉంది.** ఒక JMDO ని ఎంచుకుని, రెండు టార్గెట్‌లూ నింపండి.
:::

::: section team-reports
title: టీమ్ రిపోర్ట్‌లు
routes: /MdoReports
for: 
roles: 
designations: 
minutes: 2
source-hash: 4f488c0a407fcae5
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఈ నెల మీ JMDO లు ఎలా పని చేస్తున్నారో **Reports** చూపిస్తుంది: ముగిసిన టాస్క్‌లు, పట్టిన సమయం, మట్టి నమూనాలు, రైతులు.

1. **Report Filters** కింద, **Report Type**, **JMDO**, **On or after** (మొదలు తేదీ) ఎంచుకోండి.
2. **Generate Report** నొక్కండి.
3. కార్డులు చూడండి: **Tasks Closed**, **Avg. Turnaround**, **Soil Samples**, **Farmers Covered**.
4. **JMDO-wise summary** లో ప్రతి JMDO కి **Tasks Closed**, **On-time %**, **Soil Samples**, **Farmers Added** కనిపిస్తాయి.
5. **Detailed Task Report** లో ప్రతి టాస్క్ **Resolution**, **Status**, **Hours Taken** తో కనిపిస్తుంది.
6. ఒక టేబుల్‌పై **Copy**, **CSV**, **Excel**, **PDF** లేదా **Print** వాడండి, మళ్లీ లోడ్ చేయడానికి **Refresh** నొక్కండి.

## సాధారణ సమస్యలు

- **Sales Report లో ఏమీ కనిపించడం లేదు.** అమ్మకాల డేటా ఇంకా టాస్క్ సిస్టమ్‌లో భాగం కాలేదు.
- **Farmer Coverage కి వివరాల టేబుల్ లేదు.** రైతుల సంఖ్యలు కార్డుల్లో, సారాంశంలో మాత్రమే కనిపిస్తాయి.
:::

::: section performance-tracker
title: JMDO ల పనితీరును పోల్చడం
routes: /PerformanceTracker
for: 
roles: 
designations: 
minutes: 1
source-hash: 203ffe19ee1cb6d6
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Performance Tracker** JMDO లను బాగా చేసినవారి నుంచి తక్కువ చేసినవారి వరకు ర్యాంక్ చేస్తుంది. ప్రస్తుతానికి ఇది నమూనా సంఖ్యలు చూపిస్తుంది, నిజమైన డేటా కాదు.

1. **Filters** తెరిచి ఒక **JMDO** ని ఎంచుకోండి.
2. **Apply Filters** నొక్కండి. **Clear All** మళ్లీ అన్ని JMDO లను చూపిస్తుంది.
3. ప్రతి JMDO కి **Rank**, **SPCM Achv.**, **Soil Achv.**, **Attendance** చదవండి.

## సాధారణ సమస్యలు

- **"Detailed performance view ... isn't built yet."** (పనితీరు వివరాల స్క్రీన్ ఇంకా తయారు కాలేదు) కన్ను ఐకాన్‌కు ఇంకా వివరాల పేజీ లేదు.
:::

::: section farmer-data
title: రైతుల డేటా
routes: /FarmerData
for: 
roles: 
designations: 
minutes: 1
source-hash: 78a4629a8079544f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Farmer Data** అనేది మీ ప్రాంతంలోని రైతుల లిస్ట్, కొత్త రైతును కలపడానికి ఒక ఫారం కూడా ఉంది. ప్రస్తుతానికి ఈ స్క్రీన్ ఒక ప్రివ్యూ మాత్రమే: కొత్త ఎంట్రీలు సర్వర్‌లో సేవ్ కావు.

1. రైతులను **Contact**, **Location**, **Crop(s)**, **Acres** తో చూడండి.
2. **Add Farmer Entry** కింద, **MO H.Q.**, **Month**, **Programme** ఎంచుకోండి.
3. **Farmer Name**, **Contact Number**, **Location**, **Contact Address**, **Acres**, **Crop 1**, **Crop 2** ఎంటర్ చేయండి.
4. **Save farmer entry** నొక్కండి. ఒక వరుసపై ఉన్న పెన్సిల్ దాన్ని ఫారంలోకి కాపీ చేస్తుంది.

> Tip: మట్టి నమూనా కోసం రైతును నమోదు చేయడానికి, టాస్క్‌లోని **Add Farmer Data** వాడండి. అది sample collection స్క్రీన్‌ను తెరుస్తుంది.

## సాధారణ సమస్యలు

- **Save బటన్ బూడిద రంగులో ఉంది.** **Farmer Name** తప్పనిసరి.
- **పేజీ నుంచి బయటకు వెళ్లాక నా ఎంట్రీ పోయింది.** ఈ స్క్రీన్ ఇంకా సర్వర్‌లో సేవ్ చేయదు.
:::

::: section farmers-hub
title: రైతుల హబ్
routes: /Farmers
for: 
roles: 
designations: 
minutes: 1
source-hash: 6be3ac6e9f35012a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
రైతు ముందు ఫీల్డ్ ఆఫీసర్ వాడే టూల్స్‌ను **Farmers** ఒకే చోట ఇస్తుంది. **Farmer Portal is on its way** (రైతుల పోర్టల్ త్వరలో వస్తోంది) అని ఒక నోటీసు ఉంటుంది.

1. రాబోయే Farmer Portal గురించిన నోటీసు చదవండి.
2. **Help a farmer now** కింద, ప్రొడక్ట్‌లు, మోతాదు, వాడకం గురించి జవాబుల కోసం **Ask SPIC AI** నొక్కండి.
3. రైతులు, SPIC నిపుణుల ప్రశ్నలు జవాబుల కోసం **Knowledge Community** నొక్కండి.
4. ప్రొడక్ట్ వివరాలు చూపించడానికి **Product Information**, **Product Brochures** లేదా **AI Videos** నొక్కండి.

## సాధారణ సమస్యలు

- **Knowledge Community కనిపించడం లేదు.** మీ రోల్‌కు community యాక్సెస్ లేదు.
:::

::: section rmd-dashboard
title: RMD డాష్‌బోర్డ్
routes: /RMDDashboard
for: 
roles: 
designations: 
minutes: 1
source-hash: 4698a8dc0e852e00
confidence: ok

**ఈ స్క్రీన్ దేనికి**
RMD డాష్‌బోర్డ్ రీజియన్ యొక్క CSR, బడ్జెట్ మొత్తం ఒకే చూపులో చూపిస్తుంది. ప్రస్తుతానికి దీని సంఖ్యలు నమూనాలు మాత్రమే, నిజమైన డేటా కాదు. దీని బటన్‌లు ఏమీ సేవ్ చేయవు.

1. పైన **Budget Overview**, **CSR Workflow Summary** చూడండి.
2. **RM Approval Status**, **Budget Utilization** లాంటి చార్ట్‌లు చూడండి.
3. **Quick Actions**, **Recent Programs & Reports**, **Recent Activity**, **System Updates** చూడండి.

## సాధారణ సమస్యలు

- **సంఖ్యలు నా రీజియన్‌తో సరిపోవడం లేదు.** ప్రస్తుతానికి ఈ స్క్రీన్‌లోని సంఖ్యలు నమూనాలు మాత్రమే.
:::
