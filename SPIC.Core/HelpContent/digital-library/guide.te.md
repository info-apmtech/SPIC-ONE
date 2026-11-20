---
module: digital-library
title: డిజిటల్ లైబ్రరీ
icon: bi-book
order: 114
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
source-hash: 3269461e350cadf4
confidence: ok

డిజిటల్ లైబ్రరీ SPIC ప్రొడక్ట్ సమాచారం, AI వీడియోలు, ప్రొడక్ట్ బ్రోచర్లు అన్నింటినీ ఒకే చోట ఉంచుతుంది. ఎవరైనా ఇందులో వెతకవచ్చు, చదవవచ్చు, దీని గురించి SPIC AI ని ప్రశ్నలు అడగవచ్చు. ఒక చిన్న టీమ్ కంటెంట్ చేర్చి పబ్లిష్ చేస్తుంది.

- find | icon: bi-search | color: #0e7490
  title: మీకు కావాల్సింది కనుక్కోండి
  text: AI వీడియోలు, ప్రొడక్ట్ సమాచారం, బ్రోచర్లు చూడండి, లేదా పేరుతో వెతకండి.
- ask | icon: bi-stars | color: #7c3aed
  title: SPIC AI ని అడగండి
  text: ఒక ప్రశ్న టైప్ చేయండి, లైబ్రరీ కంటెంట్ నుంచి జవాబు వస్తుంది.
- manage | icon: bi-pencil-square | color: #d97706
  title: లైబ్రరీ టీమ్ కంటెంట్ చేరుస్తుంది
  text: వారు కొత్తవి చేరుస్తారు, పబ్లిష్ చేస్తారు, పాతవి దాచేస్తారు.
:::

::: section flow
title: కంటెంట్ మీ దగ్గరికి ఎలా వస్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 7a29b652c1da6588
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- team | type: lane | color: #d97706 | roles: Admin
  title: లైబ్రరీ టీమ్
  sub: లైబ్రరీని చూసుకునే సిబ్బంది
- readers | type: lane | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: అందరూ
  sub: login అయిన అందరు యూజర్లు
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: publish | screen: add-content
  title: కంటెంట్ చేర్చడం
  status: Draft
  text: చేర్చే ఫారంలోని మూడు దశలు నింపండి. **Save as Draft** దాన్ని చదివేవారికి కనిపించకుండా ఉంచుతుంది.
- publish | lane: team | col: 2 | icon: bi-broadcast | color: #16a34a | next: read | return: create | screen: manage-content
  title: పబ్లిష్ చేయడం
  status: Published
  text: **Save & Publish** లేదా **Publish** నొక్కండి. **Unpublish** దాన్ని మళ్లీ డ్రాఫ్ట్‌లలోకి పంపుతుంది.
- read | lane: readers | col: 3 | icon: bi-book | color: #0e7490 | next: ask | screen: library-home
  title: కనుక్కుని చదవడం
  status: Published
  text: పబ్లిష్ అయిన వీడియోలు, ప్రొడక్ట్ పేజీలు, బ్రోచర్లను అందరూ తెరవవచ్చు.
- ask | lane: readers | col: 4 | icon: bi-stars | color: #7c3aed | screen: ask-ai
  title: SPIC AI ని అడగడం
  text: చాట్‌లో ఒక ప్రశ్న అడగండి. జవాబు కోసం వాడిన లైబ్రరీ ఐటమ్‌లను కూడా జవాబు చూపిస్తుంది.
- archive | lane: team | col: 3 | icon: bi-archive | color: #475569 | kind: exception | from: publish | to: create | screen: manage-content
  title: పాత కంటెంట్ దాచడం
  status: Archived
  text: పాత లేదా తప్పు కంటెంట్ ఆర్కైవ్ అవుతుంది, చదివేవారికి ఇక కనిపించదు. **Restore** దాన్ని మళ్లీ డ్రాఫ్ట్‌గా తీసుకొస్తుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c0a33e46c0f720af
confidence: ok

- published | color: #16a34a
  label: Published
  text: అందరూ చూడవచ్చు, తెరవవచ్చు.
- draft | color: #d97706
  label: Draft
  text: సేవ్ అయ్యింది కానీ చదివేవారికి కనిపించదు. లైబ్రరీ టీమ్‌కు మాత్రమే కనిపిస్తుంది.
- archived | color: #475569
  label: Archived
  text: చదివేవారికి కనిపించదు. లైబ్రరీ టీమ్ ఎప్పుడైనా దాన్ని తిరిగి తీసుకురావచ్చు.
- active | color: #0e7490
  label: Active
  text: చేర్చే ఫారంలో ఎంచుకోవడానికి ఉన్న కేటగిరీ.
- inactive | color: #64748b
  label: Inactive
  text: చేర్చే ఫారంలో ఇక ఎంచుకోవడానికి లేని కేటగిరీ.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 7b1e4cbe0e13d1bb
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- readers | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: అందరూ
  - లైబ్రరీ చూడటం, వెతకడం
  - వీడియోలు చూడటం, బ్రోచర్లు తెరవడం
  - SPIC AI ని ప్రశ్నలు అడగడం
- team | color: #d97706 | roles: Admin
  title: లైబ్రరీ టీమ్
  - ప్రొడక్ట్‌లు, AI వీడియోలు, బ్రోచర్లు చేర్చడం
  - కంటెంట్‌ను పబ్లిష్, అన్‌పబ్లిష్, ఆర్కైవ్, డిలీట్ చేయడం
  - కేటగిరీల లిస్ట్‌ను అప్‌డేట్‌గా ఉంచడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 93226ef665c87ba7
confidence: ok

- open-brochure | popular: true
  q: ప్రొడక్ట్ బ్రోచర్ ఎలా చదవాలి?
  a: **Product Brochures** తెరిచి, బ్రోచర్‌ను నొక్కి, కవర్ ఫోటో కింద ఉన్న **Open brochure** నొక్కండి. PDF తెరుచుకుంటుంది, లేదా మీ ఫోన్‌లో సేవ్ అవుతుంది.
- ai-answer | popular: true
  q: SPIC AI ఇచ్చే ప్రతి జవాబును నమ్మవచ్చా?
  a: SPIC AI లైబ్రరీ కంటెంట్ నుంచి జవాబు ఇస్తుంది, కానీ అది కూడా తప్పులు చేయవచ్చు. రైతుకు లేదా డీలర్‌కు సలహా ఇచ్చే ముందు, జవాబు కింద ఉన్న ఐటమ్‌లు తెరిచి వివరాలు సరిచూసుకోండి.
- no-add
  q: నాకు Add Content బటన్ కనిపించడం లేదు
  a: లైబ్రరీ టీమ్ మాత్రమే కంటెంట్ చేర్చగలదు. డిజిటల్ లైబ్రరీ కంటెంట్ చేర్చడానికి మీ designation కు అనుమతి ఉండాలి. అవసరమైతే మీ అడ్మినిస్ట్రేటర్‌ను అడగండి.
- only-published
  q: "Only published Digital Library content is available to you" అని కనిపిస్తోంది
  a: పబ్లిష్ అయిన కంటెంట్ మాత్రమే మీరు చూడగలరు. స్టేటస్ లిస్ట్‌లో **All Status** లేదా **Published** ఎంచుకోండి, లేదా మొదటి మూడు కార్డులలో ఒకటి నొక్కండి.
- category-in-use
  q: ఒక కేటగిరీని డిలీట్ చేయలేకపోతున్నాను
  a: లైబ్రరీ ఐటమ్‌లు ఇంకా దాన్ని వాడుతున్నాయి. బదులుగా toggle తో దాన్ని ఆఫ్ చేయండి. తర్వాత అది చేర్చే ఫారంలో ఎంచుకోవడానికి కనిపించదు.
:::

::: section library-home
title: లైబ్రరీలో కంటెంట్ కనుక్కోవడం
routes: /DigitalLibrary
for: 
roles: 
designations: 
tour: 1 > [data-help=dl-actions] | 2 > [data-help=dl-search] | 3 > [data-help=dl-categories] | 4 > [data-help=dl-featured]
minutes: 2
source-hash: 96a094bb4b59554c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Digital Library Assistant** స్క్రీన్ లైబ్రరీ మొదటి పేజీ. ఇక్కడి నుంచి SPIC AI ని అడగవచ్చు, ఒక రకం కంటెంట్ తెరవవచ్చు, లేదా ప్రత్యేకంగా చూపిన (featured) ఐటమ్ తెరవవచ్చు.

1. SPIC AI ని ప్రశ్న అడగడానికి **Start Chat** నొక్కండి. మీరు లైబ్రరీ టీమ్‌లో ఉంటే, ఇక్కడ **Add Content** కూడా కనిపిస్తుంది.
2. సెర్చ్ బాక్స్‌లో ప్రొడక్ట్ పేరు లేదా ప్రశ్న టైప్ చేసి బాణం గుర్తు నొక్కండి. **Find Product Information** లేదా **Show latest brochures** లాంటి సూచనను కూడా నొక్కవచ్చు. ఇది మీ ప్రశ్నతో SPIC AI చాట్‌ను తెరుస్తుంది.
3. **Digital Library** కింద, **AI Videos**, **Product Information** లేదా **Product Brochures** నొక్కండి. ప్రతి కార్డు అందులో ఎన్ని ఐటమ్‌లు ఉన్నాయో చూపిస్తుంది.
4. **Featured Content** కింద, ఒక ఐటమ్‌పై **Open** (లేదా **Explore Videos**) నొక్కండి. పూర్తి లిస్ట్ చూడటానికి **View All Content** నొక్కండి.

> Tip: ఇక్కడి సెర్చ్ బాక్స్ SPIC AI ని అడుగుతుంది. ఒక ఐటమ్‌ను దాని పేరుతో కనుక్కోవడానికి, **View All Content** నొక్కి అక్కడి సెర్చ్ బాక్స్ వాడండి.

## సాధారణ సమస్యలు

- **"Nothing in the library yet".** (లైబ్రరీలో ఇంకా ఏమీ లేదు) ఇంకా ఏ కంటెంట్ పబ్లిష్ కాలేదు. తర్వాత మళ్లీ చూడండి.
- **"The library is not available".** (లైబ్రరీ అందుబాటులో లేదు) కనెక్షన్ ఫెయిల్ అయ్యింది. **Try again** నొక్కండి.
- **నా సెర్చ్ ఒక చాట్‌ను తెరిచింది.** ఇది మామూలే. ఈ పేజీలోని సెర్చ్ బాక్స్ SPIC AI ని అడుగుతుంది.
:::

::: section all-content
title: మొత్తం కంటెంట్ చూడటం, వెతకడం
routes: /DigitalLibrary/content
for: 
roles: 
designations: 
minutes: 2
source-hash: e3b9101791400b7f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**All Content** స్క్రీన్ లైబ్రరీలోని ప్రతి ఐటమ్‌ను కార్డులుగా చూపిస్తుంది. టైటిల్ కింద ఉన్న లైన్ ఎన్ని ఐటమ్‌లు సరిపోతున్నాయో చూపిస్తుంది.

1. పైన, ఒక రకాన్ని మాత్రమే చూడటానికి **Product Information**, **AI Videos** లేదా **Product Brochures** లాంటి కార్డు నొక్కండి. అందులోని నంబర్ ఆ రకంలో ఎన్ని ఐటమ్‌లు ఉన్నాయో చెబుతుంది.
2. **All Types**, **AI Videos**, **Products**, **Brochures** ట్యాబ్‌లు కూడా వాడవచ్చు.
3. **Search content...** బాక్స్‌లో టైప్ చేయండి. అది టైటిల్, వివరణ, keywords, tags, కేటగిరీలోని పదాలను కనుక్కుంటుంది.
4. ఐటమ్ తెరవడానికి కార్డు ఫోటో లేదా టైటిల్ నొక్కండి.
5. ఇంకా ఐటమ్‌లు చూడటానికి కింద ఉన్న **Load More** నొక్కండి.

> Tip: పైన ఉన్న వెనక్కి బాణం మిమ్మల్ని లైబ్రరీ మొదటి పేజీకి తీసుకెళ్తుంది.

## సాధారణ సమస్యలు

- **"No content found".** (కంటెంట్ ఏదీ దొరకలేదు) మీరు ఎంచుకున్న రకం లేదా సెర్చ్ మరీ పరిమితంగా ఉంది. **Clear filters** నొక్కండి.
- **"Only published Digital Library content is available to you".** (పబ్లిష్ అయిన కంటెంట్ మాత్రమే మీకు అందుబాటులో ఉంది) స్టేటస్ లిస్ట్‌లో **All Status** లేదా **Published** ఎంచుకోండి.
- **"The library is not available".** (లైబ్రరీ అందుబాటులో లేదు) కనెక్షన్ ఫెయిల్ అయ్యింది. **Try again** నొక్కండి.
:::

::: section view-item
title: ప్రొడక్ట్, వీడియో లేదా బ్రోచర్ చదవడం
routes: /DigitalLibrary/view/{Id:int}
for: 
roles: 
designations: 
minutes: 2
source-hash: f49b1d4b29efe757
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఈ స్క్రీన్ ఒక లైబ్రరీ ఐటమ్‌ను చూపిస్తుంది: ప్రొడక్ట్ పేజీ, AI వీడియో లేదా బ్రోచర్. ఏ భాగాలు ఏ వరుసలో కనిపించాలో లైబ్రరీ టీమ్ నిర్ణయిస్తుంది. కాబట్టి కొన్ని భాగాలు లేకపోవచ్చు.

1. లైబ్రరీ మొదటి పేజీ, **All Content** లేదా సంబంధిత ఐటమ్ నుంచి ఒక ఐటమ్ తెరవండి.
2. పైన రకం (**Product**, **AI Video** లేదా **Brochure**), టైటిల్, చిన్న వివరణ, tags చూడండి. లిస్ట్‌కు తిరిగి వెళ్లడానికి **Back** నొక్కండి.
3. ఓవర్‌వ్యూ భాగంలో, వీడియో ప్లే చేయండి, ప్రొడక్ట్ ఫోటో చూడండి, లేదా PDF తెరవడానికి **Open brochure** నొక్కండి.
4. **SPIC AI Chatbox** లో, ఈ ఐటమ్ గురించి SPIC AI ని అడగడానికి **Start Conversation** నొక్కండి.
5. **Details** బాక్స్‌లో **Category**, **Published** తేదీ, **Author**, **Status**, **Views**, tags చూడండి.
6. సంబంధిత వీడియోలు, బ్రోచర్లు, ముఖ్య ఫీచర్లు, వాడకం కోసం కిందికి స్క్రోల్ చేయండి. సంబంధిత ఐటమ్‌ను తెరవడానికి దాన్ని నొక్కండి.

## సాధారణ సమస్యలు

- **"Content not found".** (కంటెంట్ దొరకలేదు) ఆ ఐటమ్ డిలీట్ అయ్యింది, లేదా ఇప్పుడు పబ్లిష్‌లో లేదు. **Back to All Content** నొక్కండి.
- **"No video has been uploaded or linked yet."** (ఇంకా ఏ వీడియో అప్‌లోడ్ లేదా లింక్ కాలేదు) లైబ్రరీ టీమ్ ఇంకా వీడియో చేర్చలేదు.
- **"No brochure file has been uploaded yet."** (ఇంకా ఏ బ్రోచర్ ఫైల్ అప్‌లోడ్ కాలేదు) లైబ్రరీ టీమ్ ఇంకా PDF చేర్చలేదు.
- **వీడియో యాప్ బయట తెరుచుకుంటోంది.** కొన్ని వీడియో లింక్‌లు వాటి సొంత వెబ్‌సైట్‌లో మాత్రమే తెరుచుకుంటాయి. **Open the video** నొక్కండి.
:::

::: section ask-ai
title: SPIC AI ని ప్రశ్న అడగడం
routes: /DigitalLibrary/chat, /DigitalLibrary/chat/{ConversationId:int}
for: 
roles: 
designations: 
tour: 1 > [data-help=dl-chat-new] | 2 > [data-help=dl-chat-body] | 3 > [data-help=dl-chat-input] | 4 > [data-help=dl-chat-tools] | 5 > [data-help=dl-chat-list] | 6 > [data-help=dl-chat-share]
minutes: 2
source-hash: 93ef9dd3f309cfcd
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Digital Library Assistant** చాట్ స్క్రీన్‌లో SPIC ప్రొడక్ట్‌లు, వీడియోలు, బ్రోచర్ల గురించి SPIC AI ని అడగవచ్చు. జవాబులు లైబ్రరీ కంటెంట్ నుంచి వస్తాయి, అవి వాడిన ఐటమ్‌లను కూడా చూపిస్తాయి.

1. కొత్త సంభాషణ మొదలుపెట్టడానికి **New Chat** నొక్కండి.
2. **Tell me about a SPIC product** లేదా **Recommend videos** లాంటి సూచన కార్డు నొక్కండి. జవాబులు ఇక్కడ కనిపిస్తాయి. జవాబు కింద ఉన్న లైబ్రరీ ఐటమ్‌ను తెరవడానికి దాన్ని నొక్కండి, లేదా తర్వాతి ప్రశ్నను నొక్కండి.
3. లేదా **Ask anything about SPIC products, videos, brochures** బాక్స్‌లో మీ సొంత ప్రశ్న టైప్ చేయండి.
4. **AI Videos**, **Product Information** లేదా **Product Brochures** లో మాత్రమే వెతకడానికి **Select Source** ఎంచుకోండి. ఒక లైబ్రరీ ఐటమ్ గురించి మాత్రమే అడగడానికి **Attach** నొక్కండి, లేదా మాట్లాడి అడగడానికి **Voice** నొక్కండి. తర్వాత **Send** నొక్కండి.
5. మీ పాత చాట్‌లు ఎడమ వైపు **Today**, **Yesterday**, **Earlier** కింద ఉంటాయి. ఒకదాన్ని తెరవడానికి దాన్ని నొక్కండి. బిన్ (చెత్తబుట్ట) దాన్ని డిలీట్ చేస్తుంది.
6. సంభాషణను ఎవరికైనా పంపడానికి లేదా కాపీ చేయడానికి **Share** నొక్కండి.

> Tip: **Attach** చేశాక, బాక్స్‌లో **Asking about:** తో పాటు ఐటమ్ పేరు కనిపిస్తుంది. మీరు అడ్డగీత (X) నొక్కే వరకు, ప్రతి ప్రశ్నకు ఆ ఐటమ్ నుంచే జవాబు వస్తుంది.
> Careful: SPIC AI తప్పు సమాచారం చూపించవచ్చు. జవాబు ప్రకారం చేసే ముందు దాన్ని సరిచూసుకోండి.

## సాధారణ సమస్యలు

- **"SPIC AI could not answer just now."** (SPIC AI ఇప్పుడు జవాబు ఇవ్వలేకపోయింది) కాసేపు ఆగి ప్రశ్నను మళ్లీ పంపండి.
- **"Voice input is not available on this device."** (ఈ పరికరంలో వాయిస్ ఇన్‌పుట్ లేదు) బదులుగా మీ ప్రశ్న టైప్ చేయండి.
- **"Start a conversation to share it."** (షేర్ చేయడానికి ముందు సంభాషణ మొదలుపెట్టండి) ముందు ఒక ప్రశ్న అడగండి, తర్వాత **Share** నొక్కండి.
- **జవాబు కింద "Answers from library content" అని ఉంది.** పూర్తి AI లేకుండా, లైబ్రరీ ఐటమ్‌లలోని పదాలను పోల్చి ఆ జవాబు తయారు చేయబడింది.
:::

::: section add-content
title: లైబ్రరీ కంటెంట్ చేర్చడం లేదా మార్చడం
routes: /DigitalLibrary/add/{KindRoute}
for: 
roles: 
designations: 
minutes: 5
source-hash: e5986f103fd5415d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ప్రొడక్ట్ పేజీ, AI వీడియో లేదా బ్రోచర్ చేర్చడానికి, లేదా ఒకదాన్ని మార్చడానికి లైబ్రరీ టీమ్ ఈ ఫారం వాడుతుంది. ఫారంలో మూడు దశలు ఉన్నాయి: **Content & Details**, **Related Content**, **Page Layout**.

1. లైబ్రరీ మొదటి పేజీలో లేదా **All Content** లో **Add Content** నొక్కండి. **Create New Content** లో **AI Videos**, **Product Information** లేదా **Product Brochures** ఎంచుకోండి. ఒక ఐటమ్‌ను మార్చడానికి, బదులుగా దాని పెన్సిల్ ఐకాన్ లేదా **Edit Content** నొక్కండి.
2. **Basic Information** లో కేటగిరీ ఎంచుకుని, టైటిల్, tags, **Short Description** నింపండి. ప్రొడక్ట్ అయితే **Associated Program / Sub-Category**, **Resource Content Format**, **Visibility / Enable To** కూడా ఎంచుకుని, **Product Title**, **Keywords (Comma Separated)** నింపండి.
3. ఫైల్‌లు చేర్చండి: ప్రొడక్ట్ ఫోటో, వీడియో ఫైల్ లేదా thumbnail తో **Video URL**, లేదా బ్రోచర్ కవర్, **Brochure PDF**.
4. ప్రొడక్ట్ లేదా వీడియో అయితే, ఓవర్‌వ్యూ, **Key Features / Benefits**, **Usage / Application** నింపండి. సొంత టైటిల్‌తో ఇంకా టెక్స్ట్ చేర్చడానికి **Add Section** నొక్కండి.
5. **Next: Related Content** నొక్కండి. ఐటమ్ పేజీలో చూపించాల్సిన పబ్లిష్ అయిన వీడియోలు, బ్రోచర్లను టిక్ చేయండి. వాటి వరుస మార్చడానికి బాణాలు, ఎన్ని చూపించాలో పరిమితి పెట్టడానికి **Max Item** వాడండి.
6. **Next: Page Layout** నొక్కండి. భాగాల వరుస పెట్టడానికి వాటిని లాగండి, లేదా బాణాలు వాడండి. ఒక భాగాన్ని దాచడానికి switch, పేరు మార్చడానికి పెన్సిల్ వాడండి.
7. **Save & Publish** నొక్కండి (మార్చేటప్పుడు **Save Changes**). **Save as Draft** చదివేవారికి చూపించకుండా సేవ్ చేస్తుంది.

> Tip: కేటగిరీ లేకపోతే, లిస్ట్‌లో **+ Add new category…** ఎంచుకోండి. **Manage categories** పూర్తి కేటగిరీల లిస్ట్‌ను తెరుస్తుంది.
> Careful: ఐటమ్ సేవ్ అయిన తర్వాతే ఫైల్‌లు అప్‌లోడ్ అవుతాయి. అప్‌లోడ్ మెసేజ్‌లు పూర్తయ్యే వరకు పేజీలోనే ఉండండి.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Product Title**, **Video Title** లేదా **Brochure Title** | చదివేవారికి కనిపించే పేరు. తర్వాతి దశకు వెళ్లే ముందు తప్పనిసరి. |
| **Tags** | వెతకడానికి సహాయపడే చిన్న పదాలు. ఒకటి టైప్ చేసి Enter నొక్కండి, లేదా **Used before:** కింద ఉన్న tag నొక్కండి. |
| **Product Image** లేదా **Video Thumbnail** | PNG లేదా JPG, 5 MB వరకు. మంచి సైజు 1280 x 720 px. |
| **Video File (Upload)** | MP4, WebM లేదా MOV, 700 MB వరకు. లేదా YouTube, Vimeo లేదా నేరుగా **Video URL** ఇవ్వండి. |
| **Duration** | ఐచ్ఛికం, hh:mm:ss రూపంలో. ఐటమ్ పేజీలో కనిపిస్తుంది. |
| **Brochure PDF** | ఒకే PDF ఫైల్, 25 MB వరకు. |

## సాధారణ సమస్యలు

- **"Enter a product title before continuing."** (ముందుకు వెళ్లే ముందు ప్రొడక్ట్ టైటిల్ ఎంటర్ చేయండి) ముందు దశ 1 లో టైటిల్ నింపండి.
- **"Image must be 5 MB or smaller."** (ఫోటో 5 MB లేదా అంతకంటే తక్కువ ఉండాలి) చిన్న ఫోటో వాడండి. ఇదే నియమం వీడియోకు 700 MB, బ్రోచర్‌కు 25 MB.
- **"could not be uploaded. The item itself was saved."** (అప్‌లోడ్ కాలేదు, కానీ ఐటమ్ సేవ్ అయ్యింది) పెన్సిల్ ఐకాన్‌తో ఐటమ్‌ను మళ్లీ తెరిచి, ఫైల్‌ను మళ్లీ ఎంచుకోండి.
- **దశ 2 లో "No published videos found" అని ఉంది.** పబ్లిష్ అయిన ఐటమ్‌లను మాత్రమే లింక్ చేయవచ్చు. ముందు వాటిని పబ్లిష్ చేయండి.
- **"You do not have permission to add content".** (కంటెంట్ చేర్చడానికి మీకు అనుమతి లేదు) లైబ్రరీ టీమ్ మాత్రమే కంటెంట్ చేర్చగలదు లేదా మార్చగలదు.
:::

::: section manage-content
title: కంటెంట్ పబ్లిష్ చేయడం, దాచడం లేదా డిలీట్ చేయడం
routes: /DigitalLibrary/content, /DigitalLibrary/view/{Id:int}
for: DigitalLibrary.Update
roles: 
designations: 
minutes: 3
source-hash: 483778c83ea27554
confidence: ok

**ఈ స్క్రీన్ దేనికి**
చదివేవారు ఏమి చూడాలో లైబ్రరీ టీమ్ నిర్ణయిస్తుంది. ప్రతి కార్డుపై ఒక గుర్తు ఉంటుంది: **Published**, **Draft** లేదా **Archived**. కింద చెప్పిన బటన్లు లైబ్రరీ కంటెంట్ మార్చడానికి అనుమతి ఉన్న సిబ్బందికి మాత్రమే కనిపిస్తాయి.

1. **All Content** లో, దాచిన ఐటమ్‌లు చూడటానికి **Draft Content** లేదా **Archived** నొక్కండి, లేదా **All Status** లిస్ట్‌లో **Published**, **Draft** లేదా **Archived** ఎంచుకోండి.
2. డ్రాఫ్ట్ కార్డును అందరికీ చూపించడానికి దానిపై **Publish** నొక్కండి.
3. పబ్లిష్ అయిన ఐటమ్‌ను దాచడానికి **Unpublish** నొక్కండి. **Unpublish this content?** లో **Unpublish** నొక్కండి. అది డ్రాఫ్ట్‌లలోకి వెళ్తుంది.
4. పాత ఐటమ్‌ను ఆర్కైవ్ చేయడానికి ఆర్కైవ్ ఐకాన్ నొక్కండి. ఆర్కైవ్ అయిన ఐటమ్‌ను మళ్లీ డ్రాఫ్ట్‌గా తీసుకురావడానికి **Restore** నొక్కండి.
5. ఐటమ్‌ను మార్చడానికి పెన్సిల్ ఐకాన్ నొక్కండి, లేదా డిలీట్ చేయడానికి బిన్ ఐకాన్ నొక్కి, తర్వాత నిర్ధారించడానికి **Delete** నొక్కండి.
6. ఐటమ్ పేజీలో, **Details** బాక్స్‌లో **Edit Details**, **Publish Product** లేదా **Unpublish Video** లాంటి పబ్లిష్ బటన్ కూడా ఉంటాయి.

> Careful: కంటెంట్ డిలీట్ చేస్తే మళ్లీ తిరిగి రాదు. మళ్లీ అవసరం కావచ్చు అనుకుంటే, బదులుగా ఆర్కైవ్ చేయండి.

## సాధారణ సమస్యలు

- **నాకు బిన్ ఐకాన్ కనిపించడం లేదు.** డిలీట్ చేయడానికి వేరే అనుమతి కావాలి. మీ అడ్మినిస్ట్రేటర్‌ను అడగండి.
- **ఒక ఐటమ్ లిస్ట్ నుంచి కనిపించకుండా పోయింది.** అది ఇప్పుడు ఆర్కైవ్ అయి ఉండొచ్చు, లేదా డ్రాఫ్ట్‌గా ఉండొచ్చు. స్టేటస్ లిస్ట్‌లో **All Status** ఎంచుకోండి.
:::

::: section categories
title: లైబ్రరీ కేటగిరీలు నిర్వహించడం
routes: /DigitalLibrary/categories
for: 
roles: 
designations: 
minutes: 3
source-hash: f17d81a735096b2f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
చేర్చే ఫారంలో ఎంచుకోవడానికి ఉండే కేటగిరీలు, సబ్-కేటగిరీల లిస్ట్‌ను **Library Categories** స్క్రీన్ ఉంచుతుంది. ఉదాహరణకు, ఒక కేటగిరీ, దాని కింద దాని సబ్-కేటగిరీలు.

1. కేటగిరీని కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో టైప్ చేయండి, లేదా **All**, **Active** లేదా **Inactive** ఎంచుకోండి.
2. కేటగిరీ సబ్-కేటగిరీలు చూడటానికి దాని పక్కన ఉన్న బాణం నొక్కండి.
3. **Add Category** నొక్కి, **Name**, కావాలంటే **Sort order** టైప్ చేసి, **Save** నొక్కండి.
4. **Add Sub-Category** నొక్కండి (లేదా కేటగిరీ కింద ఉన్న **Add sub-category to**), **Category** ఎంచుకుని, **Name** టైప్ చేసి, **Save** నొక్కండి.
5. పేరు లేదా వరుస మార్చడానికి పెన్సిల్ ఐకాన్ నొక్కి, తర్వాత **Save** నొక్కండి. కొత్త పేరు దాన్ని వాడే ఐటమ్‌లలో కూడా మారుతుంది.
6. కేటగిరీని **Inactive** చేయడానికి లేదా మళ్లీ **Active** చేయడానికి toggle వాడండి. Inactive కేటగిరీలు చేర్చే ఫారంలో ఎంచుకోవడానికి కనిపించవు.
7. ఏ ఐటమ్ వాడని కేటగిరీని తీసేయడానికి బిన్ ఐకాన్, **Delete** నొక్కండి.

## సాధారణ సమస్యలు

- **"You do not have permission to manage categories".** (కేటగిరీలు నిర్వహించడానికి మీకు అనుమతి లేదు) లైబ్రరీ టీమ్ మాత్రమే కేటగిరీలు మార్చగలదు. **Back to the library** నొక్కండి.
- **"is in use by ... so it cannot be deleted. Deactivate it instead."** (వాడుకలో ఉంది కాబట్టి డిలీట్ చేయలేరు, బదులుగా ఆఫ్ చేయండి) toggle తో దాన్ని ఆఫ్ చేయండి.
- **"has ... sub-categories. Delete or move them first".** (ఇందులో సబ్-కేటగిరీలు ఉన్నాయి, ముందు వాటిని డిలీట్ చేయండి లేదా మార్చండి) దాని సబ్-కేటగిరీలను డిలీట్ చేయండి లేదా వేరే కేటగిరీకి మార్చండి, లేదా ఆ కేటగిరీని ఆఫ్ చేయండి.
- **Add Sub-Category బూడిద రంగులో ఉంది.** ముందు ఒక కేటగిరీ చేర్చండి.
- **"Sort order must be a whole number of 0 or more."** (Sort order 0 లేదా అంతకంటే ఎక్కువ పూర్ణ సంఖ్య కావాలి) దాన్ని ఖాళీగా వదిలేయండి, లేదా 0, 1, 2 అలా టైప్ చేయండి.
:::

::: section coming-soon
title: ఇంకా సిద్ధం కాని లైబ్రరీ పేజీ
routes: /DigitalLibrary/{Section}
for: 
roles: 
designations: 
minutes: 1
source-hash: b471b84357af366e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
కొన్ని డిజిటల్ లైబ్రరీ అడ్రస్‌లు, ఆ భాగం **is being built** (తయారవుతోంది) అని చెప్పే పేజీని తెరుస్తాయి. లైబ్రరీలోని ఆ భాగం ఇంకా సిద్ధం కాలేదు.

1. మెసేజ్ చదవండి. ఆ భాగం రాబోయే విడుదలలో అందుబాటులోకి వస్తుందని అది చెబుతుంది.
2. మొదటి పేజీకి తిరిగి వెళ్లడానికి **Back to the library** నొక్కండి.

## సాధారణ సమస్యలు

- **సేవ్ చేసిన లింక్ నుంచి ఈ పేజీకి వచ్చాను.** మెనూ నుంచి లైబ్రరీ తెరిచి, బదులుగా **View All Content** వాడండి.
:::
