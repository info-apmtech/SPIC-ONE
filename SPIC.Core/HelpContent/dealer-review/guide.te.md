---
module: dealer-review
title: డీలర్ రివ్యూ, ఆమోదం
icon: bi-person-check
order: 102
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
source-hash: c1f3fc37e023de12
confidence: ok

MO డీలర్ అప్లికేషన్ సబ్మిట్ చేశాక, ముందు RM, తర్వాత SM, తర్వాత AVP దాన్ని చెక్ చేస్తారు. ప్రతి ఒక్కరూ దాన్ని ఆమోదించి ఒక క్రెడిట్ లిమిట్ సూచించవచ్చు, లేదా సరిచేయడానికి తిరిగి పంపవచ్చు. సబ్ డీలర్లు, ఉద్యోగుల ఆమోదం స్క్రీన్‌లు, కొన్ని డీలర్ రిపోర్ట్‌లు కూడా ఈ మాడ్యూల్‌లో ఉన్నాయి.

- check | icon: bi-search | color: #6d28d9
  title: మూడు స్థాయిల్లో చెకింగ్
  text: ముందు RM, తర్వాత SM, తర్వాత AVP అప్లికేషన్ తెరిచి ప్రతి భాగాన్ని చెక్ చేస్తారు.
- limit | icon: bi-cash-coin | color: #d97706
  title: క్రెడిట్ లిమిట్ నిర్ణయం
  text: RM, SM కలిసి SPIC, GFL కోసం క్రెడిట్ లిమిట్ సూచిస్తారు. AVP చివరి లిమిట్ ఇస్తారు.
- back | icon: bi-arrow-counterclockwise | color: #dc2626
  title: సరిచేయడానికి తిరిగి పంపడం
  text: ఏ స్థాయిలో అయినా, కారణం, సరిచేయాల్సిన భాగాలు చెప్పి అప్లికేషన్‌ను ఒక అడుగు వెనక్కి పంపవచ్చు.
:::

::: section flow
title: అప్లికేషన్ ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 1f194f956d1d33e7
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- mo | type: lane | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  sub: అప్లికేషన్ నింపుతారు
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: రీజినల్ మేనేజర్
- sm | type: lane | color: #c026d3 | roles: SMD, SMM
  title: SM
  sub: స్టేట్ మార్కెటింగ్ మేనేజర్
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin, SuperAdmin
  title: AVP
  sub: చివరి ఆమోదం
- office | type: lane | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: కొత్త డీలర్లకు మాత్రమే
- submit | lane: mo | col: 1 | icon: bi-send | color: #64748b | next: rm-check
  title: అప్లికేషన్ సబ్మిట్ చేయడం
  status: Submitted
  text: MO డీలర్ అప్లికేషన్‌లోని ప్రతి భాగం నింపి సబ్మిట్ చేస్తారు. పూర్తిగా నింపిన అప్లికేషన్‌లు మాత్రమే రివ్యూ లిస్ట్‌కు వస్తాయి.
- rm-check | lane: rm | col: 2 | icon: bi-person-check | color: #0891b2 | next: sm-check | return: submit | screen: review-approve
  title: RM చెక్ చేయడం
  status: In RM
  text: RM అప్లికేషన్ చెక్ చేసి, RM క్రెడిట్ లిమిట్ ఎంటర్ చేసి **Proceed to SM** నొక్కుతారు, లేదా MO కి తిరిగి పంపుతారు.
- sent-back | lane: mo | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: submit | screen: send-back
  title: సరిచేసి మళ్లీ సబ్మిట్ చేయడం
  status: Sent Back
  text: ఒక రివ్యూయర్ కారణం, సరిచేయాల్సిన భాగాలు చెప్పి అప్లికేషన్‌ను తిరిగి పంపారు. ముందు స్థాయి వారు వాటిని సరిచేసి మళ్లీ ముందుకు పంపుతారు.
- sm-check | lane: sm | col: 3 | icon: bi-clipboard-check | color: #c026d3 | next: avp-check | return: rm-check | screen: review-approve
  title: SM చెక్ చేయడం
  status: In SMM
  text: SM, RM క్రెడిట్ లిమిట్ చూసి, SM క్రెడిట్ లిమిట్ ఎంటర్ చేసి **Proceed to AVP** నొక్కుతారు, లేదా RM కి తిరిగి పంపుతారు.
- avp-check | lane: avp | col: 4 | icon: bi-patch-check | color: #7c3aed | next: office-ids | return: sm-check | screen: review-approve
  title: AVP ఆమోదించడం
  status: In AVP
  text: AVP ముందు ఇచ్చిన రెండు లిమిట్‌లూ చూసి, చివరి క్రెడిట్ లిమిట్ ఎంటర్ చేసి **Approve** నొక్కుతారు. డీలర్ యాక్టివ్ అవుతారు.
- office-ids | lane: office | col: 5 | icon: bi-upc-scan | color: #16a34a | screen: avp-office-ids
  title: Dealer ID లు ఎంటర్ చేయడం
  status: Verified
  text: కొత్త డీలర్ అయితే, AVP Office SPIC, GFL Dealer ID లు ఎంటర్ చేస్తుంది. ఇప్పటికే ఉన్న డీలర్ అయితే, AVP ఆమోదంతోనే పూర్తవుతుంది.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 3678b5e01ec8dd65
confidence: ok

- submitted | color: #64748b
  label: Submitted
  text: MO అప్లికేషన్ సబ్మిట్ చేశారు.
- in-rm | color: #0891b2
  label: In RM
  text: రీజినల్ మేనేజర్ (RM) కోసం ఎదురుచూస్తోంది.
- in-smm | color: #c026d3
  label: In SMM
  text: స్టేట్ మార్కెటింగ్ మేనేజర్ (SMM) కోసం ఎదురుచూస్తోంది.
- in-avp | color: #7c3aed
  label: In AVP
  text: AVP చివరి ఆమోదం కోసం ఎదురుచూస్తోంది.
- pending | color: #d97706
  label: Pending
  text: రివ్యూ లిస్ట్‌లో: ఇంకా RM, SMM, AVP లేదా AVP Office దగ్గర ఉంది. ఇప్పుడు ఎవరి దగ్గర ఉందో **Current Stage** కాలమ్ చూపిస్తుంది.
- verified | color: #16a34a
  label: Verified
  text: రివ్యూ లిస్ట్‌లో: పూర్తిగా ఆమోదం వచ్చింది.
- approved | color: #16a34a
  label: Approved
  text: అప్లికేషన్‌లో: AVP ఆమోదించారు.
- sent-back | color: #dc2626
  label: Sent Back
  text: రివ్యూ లిస్ట్‌లో: ఒక రివ్యూయర్ సరిచేయడానికి తిరిగి పంపారు.
- rejected | color: #dc2626
  label: Rejected
  text: అప్లికేషన్‌లో: ఒక రివ్యూయర్ దాన్ని తిరిగి పంపారు. ఇది మూసివేయబడలేదు; సరిచేశాక మళ్లీ వస్తుంది.
:::

::: section roles
title: ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 06465e79ce0431ec
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- mo | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  - డీలర్ అప్లికేషన్ సబ్మిట్ చేయడం
  - తిరిగి వచ్చినప్పుడు దాన్ని సరిచేయడం
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - మీ రీజియన్ అప్లికేషన్‌లు చెక్ చేయడం
  - RM క్రెడిట్ లిమిట్ సూచించడం
  - MO కి తిరిగి పంపడం
- sm | color: #c026d3 | roles: SMD, SMM
  title: SM
  - మీ రాష్ట్రం అప్లికేషన్‌లు చెక్ చేయడం
  - SM క్రెడిట్ లిమిట్ సూచించడం
  - RM కి తిరిగి పంపడం
  - సబ్ డీలర్లు, ఉద్యోగులకు మొదటి ఆమోదం ఇవ్వడం
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP
  - చివరి ఆమోదం, క్రెడిట్ లిమిట్ ఇవ్వడం
  - SM కి తిరిగి పంపడం
  - సబ్ డీలర్లు, ఉద్యోగులకు చివరి ఆమోదం ఇవ్వడం
- admin | color: #374151 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - AVP లాగానే చివరి ఆమోదం ఇవ్వగలరు
  - Credit Limit Sales లెక్కలు నిర్వహించడం
  - రాష్ట్రాల వారీ డీలర్ సారాంశం చూడటం
- office | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - కొత్త డీలర్లకు SPIC, GFL Dealer ID లు ఎంటర్ చేయడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 724bb434f0c07f99
confidence: ok

- no-button | popular: true
  q: నేను అప్లికేషన్ తెరిచాను, కానీ Proceed లేదా Send Back బటన్ లేదు
  a: అప్లికేషన్ ఇప్పుడు మీ స్థాయిలో లేదు. అది మీ కోసం ఎదురుచూస్తున్నప్పుడు మాత్రమే బటన్లు కనిపిస్తాయి. పైన ఉన్న **Status** కార్డు, ప్రోగ్రెస్ బార్ చెక్ చేయండి.
- no-limit-boxes | popular: true
  q: Confirm Proceed విండోలో క్రెడిట్ లిమిట్ బాక్స్‌లు ఎందుకు లేవు?
  a: కొత్త డీలర్ మొదటి ఆమోదానికి, యాక్టివ్‌గా లేని డీలర్‌కు, లేదా క్రెడిట్ లిమిట్ లేకుండా అంగీకారం (consent) తో అప్లై చేసిన డీలర్‌కు (అప్పుడు లిమిట్ 0) ఇవి కనిపించవు. డీలర్ SPIC తో ఉంటేనే SPIC బాక్స్‌లు, GFL తో ఉంటేనే GFL బాక్స్‌లు కనిపిస్తాయి.
- missing
  q: ఒక అప్లికేషన్ నా రివ్యూ లిస్ట్‌లో కనిపించడం లేదు
  a: లిస్ట్‌లో మీ ప్రాంతం నుంచి పూర్తిగా నింపిన అప్లికేషన్‌లు మాత్రమే కనిపిస్తాయి. అప్లికేషన్‌లో కొన్ని భాగాలు ఇంకా ఖాళీగా ఉంటే, అది రాదు. అన్ని భాగాలు పూర్తి చేయమని MO కి చెప్పండి.
- remarks
  q: "Remarks are required for New Dealer approval." అని కనిపిస్తోంది
  a: కొత్త డీలర్ అయితే, ప్రతి స్థాయిలో రిమార్క్స్ రాయాలి. **Remarks** బాక్స్‌లో టైప్ చేసి, మళ్లీ **Confirm & Proceed** నొక్కండి.
- after-send-back
  q: నేను తిరిగి పంపితే అప్లికేషన్ ఎక్కడికి వెళ్తుంది?
  a: ఒక స్థాయి వెనక్కి వెళ్తుంది. RM, MO కి పంపుతారు; SM, RM కి; AVP, SM కి. వారు సరిచేశాక అది మళ్లీ మీ దగ్గరికి వస్తుంది.
:::

::: section review-list
title: రివ్యూ కోసం ఎదురుచూస్తున్న అప్లికేషన్‌లు చూడటం
routes: /DealerReviewList
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-list-cards] | 2 > [data-help=dr-list-search] | 3 > [data-help=dr-list-status] | 4 > [data-help=dr-list-table] | 5 > [data-help=dr-list-pages]
minutes: 2
source-hash: d6627d7c7d23093d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Dealer Applications Review** స్క్రీన్ మీ ప్రాంతం నుంచి సబ్మిట్ అయిన డీలర్ అప్లికేషన్‌లను చూపిస్తుంది. మీ కోసం ఎదురుచూస్తున్నవి పైన ఉంటాయి.

1. పైన ఉన్న కార్డులు చూడండి: **Total Applications**, **Pending Review**, **Verified**, **Sent Back**.
2. అప్లికేషన్‌ను త్వరగా కనుక్కోవడానికి **Search applications...** లో అప్లికేషన్ ID లేదా డీలర్ పేరు టైప్ చేయండి.
3. అవి మాత్రమే చూడటానికి, స్టేటస్ బాక్స్‌లో **Pending**, **Verified** లేదా **Sent Back** ఎంచుకోండి. **All Status** అన్నీ చూపిస్తుంది.
4. టేబుల్‌లో, అప్లికేషన్ ఇప్పుడు ఎవరి దగ్గర ఉందో **Current Stage** చూపిస్తుంది, ఆమోదించిన లిమిట్‌ను **Credit Limit** చూపిస్తుంది. తెరవడానికి **Action** లో కన్ను ఐకాన్ నొక్కండి.
5. కింద, ఎన్ని వరుసలు చూపించాలో **Items Per Page** లో ఎంచుకోండి, పేజీల మధ్య వెళ్లడానికి బాణం గుర్తులు వాడండి.

> Tip: RM తన రీజియన్‌ను, SM రాష్ట్రాన్ని, AVP అన్ని అప్లికేషన్‌లను చూస్తారు.

## సాధారణ సమస్యలు

- **"No applications found."** (అప్లికేషన్‌లు ఏవీ లేవు) **All Status** ఎంచుకుని, సెర్చ్ బాక్స్ ఖాళీ చేయండి.
- **ఒక అప్లికేషన్ లిస్ట్‌లో లేదు.** పూర్తిగా నింపిన అప్లికేషన్‌లు మాత్రమే లిస్ట్‌లో వస్తాయి. ప్రతి భాగం నింపమని MO కి చెప్పండి.
:::

::: section review-approve
title: అప్లికేషన్ చెక్ చేసి ఆమోదించడం
routes: /SavedDealerReview
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-rev-cards] | 2 > [data-help=dr-rev-progress] | 3 > [data-help=dr-rev-timeline] | 4 > [data-help=dr-rev-details] | 5 > [data-help=dr-rev-sendback] | 6 > [data-help=dr-rev-approve]
minutes: 4
source-hash: 337aec253f3ff75e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Review - Dealer Application** స్క్రీన్ ఒక అప్లికేషన్‌ను పూర్తిగా చూపిస్తుంది. అది మీ కోసం ఎదురుచూస్తున్నప్పుడు, ఇక్కడ ఆమోదించి మీ క్రెడిట్ లిమిట్ ఇవ్వవచ్చు, లేదా తిరిగి పంపవచ్చు.

1. పైన ఉన్న కార్డులు చెక్ చేయండి: **Dealer Name**, **Dealer Code**, **Submitted By**, **Status**.
2. ప్రోగ్రెస్ బార్ ప్రతి స్థాయిని చూపిస్తుంది: **MO**, **RM**, **SM**, **AVP**, వాటితో పాటు **Under Review**, **Pending**, **Approved** లేదా **Rejected**.
3. ఎవరు ఏమి చేశారో, ఎప్పుడు, వారి రిమార్క్స్, వారు ఇచ్చిన క్రెడిట్ లిమిట్ **Timeline**, **Action History** లో కనిపిస్తాయి.
4. ప్రతి భాగాన్ని నొక్కి తెరిచి చెక్ చేయండి: **Company Details**, **Experience In Fertilizer Trade**, **Annual Sales Data**, **Proprietor Details**, **Investment**, **Credit Limit Proposal**, **Enclosures**, మిగతావి.
5. ఏదైనా తప్పుగా ఉంటే, **Send Back** నొక్కండి ("అప్లికేషన్‌ను తిరిగి పంపడం" భాగం చూడండి).
6. అంతా సరిగ్గా ఉంటే, **Proceed to SM** (RM), **Proceed to AVP** (SM) లేదా **Approve** (AVP) నొక్కండి. **Confirm Proceed** విండోలో మీ క్రెడిట్ లిమిట్, **Remarks** ఎంటర్ చేసి, **Confirm & Proceed** నొక్కండి.

> Careful: మీరు ముందుకు పంపాక, అప్లికేషన్ తర్వాతి స్థాయికి వెళ్లిపోతుంది. ఆ తర్వాత మీరు దాన్ని మార్చలేరు.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **RM Credit Limit (SPIC)**, **RM Credit Limit (GFL)** | RM సూచించే లిమిట్. RM కి కనిపిస్తుంది. |
| **SM Credit Limit (SPIC)**, **SM Credit Limit (GFL)** | SM సూచించే లిమిట్. RM లిమిట్‌లు పైన **(Approved)** అని కనిపిస్తాయి. |
| **AVP Credit Limit (SPIC)**, **AVP Credit Limit (GFL)** | చివరి లిమిట్. RM, SM లిమిట్‌లు పైన కనిపిస్తాయి. |
| **Remarks** | మీ కామెంట్లు. కొత్త డీలర్‌కు తప్పనిసరి, మిగతా వాటికి అవసరమైతేనే. |

## సాధారణ సమస్యలు

- **Proceed లేదా Send Back బటన్ లేదు.** అప్లికేషన్ ఇప్పుడు మీ కోసం ఎదురుచూడటం లేదు. మీరు చదవడం మాత్రమే చేయగలరు.
- **ఆకుపచ్చ రంగు "Credit Limit Consent" బాక్స్ కనిపిస్తోంది.** డీలర్ క్రెడిట్ లిమిట్ వివరాలు లేకుండా అప్లై చేశారు. అందుకే క్రెడిట్ లిమిట్ 0.
- **స్క్రీన్ Dashboard కి వెళ్లిపోతోంది.** ఇది మామూలే. మీరు ఆమోదించాక, యాప్ సక్సెస్ మెసేజ్ చూపించి Dashboard తెరుస్తుంది.
:::

::: section send-back
title: అప్లికేషన్‌ను తిరిగి పంపడం
routes: /SavedDealerReview
for: 
roles: RM, RMD, SMD, SMM, AVP, Director, Admin, CorporateAdmin, SuperAdmin
designations: 
minutes: 2
source-hash: b3b23aec15238c4a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
అప్లికేషన్‌లో తప్పులు ఉంటే, కారణంతో ఒక స్థాయి వెనక్కి పంపండి. RM, **Marketing Officer (MO)** కి పంపుతారు; SM, **Regional Manager (RM)** కి; AVP, **State Manager (SM)** కి.

1. అప్లికేషన్ తెరిచి కింద ఉన్న **Send Back** నొక్కండి.
2. **Send Back Application** విండోలో, **Application Summary**, **Send Back To** చెక్ చేయండి.
3. **Reason for Sending Back** రాయండి. ఇది తప్పనిసరి.
4. **Sections Requiring Correction** కింద, సరిచేయాల్సిన భాగాలకు టిక్ పెట్టండి, ఉదాహరణకు **Tax & Compliance**, **Investment** లేదా **Enclosures**. కొన్నింటికి ముందే టిక్ ఉంటుంది; సరిగ్గా ఉన్నవాటి టిక్ తీసేయండి.
5. **Priority Level** ఎంచుకోండి: **High**, **Medium** లేదా **Low**.
6. **Send Back to MO**, **Send Back to RM** లేదా **Send Back to SM** నొక్కండి (బటన్ మీ స్థాయిని బట్టి కనిపిస్తుంది), లేదా ఆపడానికి **Cancel** నొక్కండి.

## సాధారణ సమస్యలు

- **"Reason is required."** (కారణం తప్పనిసరి) ముందు కారణం రాయండి.
- **"Please select at least one correction section."** (కనీసం ఒక సరిచేయాల్సిన భాగం ఎంచుకోండి) కనీసం ఒక భాగానికి టిక్ పెట్టండి.
- **"Unable to send back dealer application."** (డీలర్ అప్లికేషన్‌ను తిరిగి పంపలేకపోయాం) మీ ఇంటర్నెట్ చెక్ చేసి మళ్లీ ప్రయత్నించండి.
:::

::: section avp-office-ids
title: కొత్త డీలర్ కోసం Dealer ID లు ఎంటర్ చేయడం
routes: /SavedDealerReview
for: 
roles: CommonRole
designations: AVP Office
minutes: 2
source-hash: e77b528ade25e538
confidence: ok

**ఈ స్క్రీన్ దేనికి**
AVP కొత్త డీలర్‌ను ఆమోదించాక, AVP Office కంపెనీ Dealer ID లు ఎంటర్ చేస్తుంది. దీనితో అప్లికేషన్ పూర్తవుతుంది.

1. రివ్యూ లిస్ట్ నుంచి అప్లికేషన్ తెరవండి. దాని **Current Stage** లో **AVP Office** అని ఉంటుంది.
2. **AVP Office - Dealer ID Submission** బాక్స్ వరకు కిందకి స్క్రోల్ చేయండి.
3. **SPIC Dealer ID** మరియు/లేదా **GFL Dealer ID** ఎంటర్ చేయండి. డీలర్ అప్లై చేసిన కంపెనీలు మాత్రమే కనిపిస్తాయి.
4. అవసరమైతే **Remarks (optional)** రాయండి.
5. **Submit Dealer IDs** నొక్కండి. యాప్ సక్సెస్ మెసేజ్ చూపించి Dashboard తెరుస్తుంది.

## సాధారణ సమస్యలు

- **"SPIC Dealer ID is required for this application."** (ఈ అప్లికేషన్‌కు SPIC Dealer ID తప్పనిసరి) డీలర్ SPIC తో ఉన్నారు. SPIC Dealer ID ఎంటర్ చేయండి.
- **"GFL Dealer ID is required for this application."** (ఈ అప్లికేషన్‌కు GFL Dealer ID తప్పనిసరి) డీలర్ GFL తో ఉన్నారు. GFL Dealer ID ఎంటర్ చేయండి.
- **నాకు ఆ బాక్స్ కనిపించడం లేదు.** AVP ఇంకా ఆమోదించలేదు, లేదా ID లు ఇప్పటికే ఎంటర్ చేశారు.
:::

::: section view-application
title: అప్లికేషన్ చూడటం (చదవడానికి మాత్రమే)
routes: /ReviewDealer
for: 
roles: 
designations: 
minutes: 1
source-hash: ee8404c8e5c3aad3
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఈ పాత **Review - Dealer Application** స్క్రీన్ ఒక అప్లికేషన్‌ను చూపిస్తుంది, కానీ ఆమోదం బటన్లు ఏవీ ఉండవు.

1. పైన ఉన్న కార్డులు చెక్ చేయండి: **Dealer Name**, **Dealer ID**, **Total Experience**, **Total Sales**, **Investment**, **Credit Limit**, **Status**.
2. **Company Details** లేదా **Investment** లాంటి ప్రతి భాగాన్ని నొక్కి తెరవండి.
3. **Enclosures** కింద, డాక్యుమెంట్ పేరు నొక్కి దాన్ని తెరవండి.

> Tip: ఆమోదించడానికి లేదా తిరిగి పంపడానికి, బదులుగా **Dealer Applications Review** లిస్ట్ నుంచి అప్లికేషన్ తెరవండి.

## సాధారణ సమస్యలు

- **స్క్రీన్ ఖాళీగా ఉంది.** ఒక అప్లికేషన్ ఎంచుకున్నప్పుడు మాత్రమే ఇది తెరుచుకుంటుంది. రివ్యూ లిస్ట్‌కు వెళ్లి కన్ను ఐకాన్ వాడండి.
:::

::: section sm-approvals
title: సబ్ డీలర్లు, ఉద్యోగులను ఆమోదించడం (SM)
routes: /SMMApprovals
for: 
roles: SMD, SMM
designations: 
tour: 1 > [data-help=dr-sm-tabs] | 2 > [data-help=dr-sm-cards] | 3 > [data-help=dr-sm-table] | 4 > [data-help=dr-sm-table]
minutes: 2
source-hash: e13ee4a27ffd7b42
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**SM Approval** స్క్రీన్ మీ రాష్ట్రంలోని డీలర్ల కోసం చేర్చిన సబ్ డీలర్లు, ఉద్యోగులను చూపిస్తుంది. ముందు SM చెక్ చేస్తారు; తర్వాత అవి AVP కి వెళ్తాయి.

1. ఒక ట్యాబ్ ఎంచుకోండి: **All Pending**, **Sub Dealers** లేదా **Employees**.
2. కార్డులు **Total Pending**, **Sub Dealers**, **Employees** చూపిస్తాయి. ఒక రకం మాత్రమే చూడటానికి **Filter** వాడండి.
3. **Pending SM Approvals** లో, **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary**, **Nominee** చెక్ చేయండి.
4. **Approve** లేదా **Reject** నొక్కి, **Remarks** రాసి, తర్వాత **Confirm Approve** లేదా **Confirm Reject** నొక్కండి.

> Tip: ఆమోదానికి రిమార్క్స్ తప్పనిసరి కాదు, కానీ తిరస్కరించేటప్పుడు కారణం రాయండి.

## సాధారణ సమస్యలు

- **"No Pending Approvals".** (పెండింగ్ ఆమోదాలు ఏవీ లేవు) ఇప్పుడు మీ కోసం ఏదీ ఎదురుచూడటం లేదు.
- **"Show more" అని కనిపిస్తోంది.** మొదట 50 వరుసలు మాత్రమే కనిపిస్తాయి. మిగతావి చూడటానికి **Show more** నొక్కండి.
:::

::: section avp-approvals
title: సబ్ డీలర్లు, ఉద్యోగులకు చివరి ఆమోదం (AVP)
routes: /AVPApprovals
for: 
roles: AVP
designations: 
minutes: 2
source-hash: 61b6fcc972ffa53e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**AVP Approval** స్క్రీన్ SM ఇప్పటికే ఆమోదించిన సబ్ డీలర్లు, ఉద్యోగులను చూపిస్తుంది. AVP చివరి ఆమోదం ఇస్తారు.

1. ఒక ట్యాబ్ ఎంచుకోండి: **All Pending**, **Sub Dealers** లేదా **Employees**.
2. **Total Pending**, **Sub Dealers**, **Employees** కార్డులు చెక్ చేయండి. ఒక రకం మాత్రమే చూడటానికి **Filter** వాడండి.
3. **Pending AVP Approvals** లో, **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary**, **Nominee** చెక్ చేయండి.
4. **Approve** లేదా **Reject** నొక్కండి. విండోలో **SM Status: Approved** అని కనిపిస్తుంది.
5. **Remarks** రాసి **Confirm Approve** లేదా **Confirm Reject** నొక్కండి.

## సాధారణ సమస్యలు

- **"No Pending AVP Approvals".** (పెండింగ్ AVP ఆమోదాలు ఏవీ లేవు) SM ఆమోదించిన ఏ రికార్డూ మీ కోసం ఎదురుచూడటం లేదు.
- **"Action failed".** (పని జరగలేదు) మళ్లీ ప్రయత్నించండి. మళ్లీ మళ్లీ ఫెయిల్ అయితే, మీ admin కి చెప్పండి.
:::

::: section credit-limit-sales
title: క్రెడిట్ లిమిట్ కోసం డీలర్ అమ్మకాలు నమోదు చేయడం
routes: /CreditLimitSales
for: 
roles: 
designations: 
minutes: 3
source-hash: 580721465ba2267a
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Credit Limit Sales** స్క్రీన్ ప్రతి డీలర్ గత అమ్మకాలను సంవత్సరం, ప్రొడక్ట్ వారీగా ఉంచుతుంది. రివ్యూయర్లు ఈ లెక్కలను అప్లికేషన్‌లోని **Credit Limit Proposal** లో చూస్తారు.

1. **Add New Credit Limit Sales** కింద, **State**, **Financial Year** ఎంచుకోండి.
2. **Dealer Code** లేదా **Dealer Name** తో డీలర్‌ను ఎంచుకోండి. రెండు బాక్స్‌లూ ఒకే డీలర్‌ను ఎంచుకుంటాయి.
3. **Product**, **Product Group**, **Category** ఎంచుకోండి.
4. **Quantity**, **Gross Amount** ఎంటర్ చేసి, **Save** నొక్కండి. **Cancel** ఫారం ఖాళీ చేస్తుంది.
5. ఒకేసారి చాలా వరుసలు చేర్చాలంటే, Excel ఫార్మాట్ కోసం **Sample Format** నొక్కి, దాన్ని నింపి, **Bulk Upload** నొక్కి, **Financial Year**, ఫైల్ ఎంచుకోండి.
6. **Existing Credit Limit Sales** లో, కోడ్ లేదా పేరుతో వెతకండి. పెన్సిల్ వరుసను మారుస్తుంది, బిన్ (చెత్తబుట్ట) దాన్ని డిలీట్ చేస్తుంది.

> Careful: వరుసను డిలీట్ చేస్తే అది మళ్లీ తిరిగి రాదు.

## సాధారణ సమస్యలు

- **"All required fields must be filled."** (తప్పనిసరి ఫీల్డ్‌లు అన్నీ నింపాలి) State, Financial Year, డీలర్, Product, Product Group, Category ఎంచుకోండి.
- **"Please select a financial year before uploading."** (అప్‌లోడ్ చేసే ముందు ఆర్థిక సంవత్సరం ఎంచుకోండి) ముందు అప్‌లోడ్ విండోలో సంవత్సరం ఎంచుకోండి.
- **Save, Bulk Upload, పెన్సిల్ లేదా బిన్ కనిపించడం లేదు.** మీ డెసిగ్నేషన్‌కు చేర్చడానికి, మార్చడానికి లేదా డిలీట్ చేయడానికి అనుమతి లేదు. మీ admin ని అడగండి.
:::

::: section state-summary
title: రాష్ట్రాల వారీ డీలర్ సారాంశం
routes: /DealerStateSummary
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP
designations: 
minutes: 1
source-hash: d5685e7a98ccb663
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Total Dealers — State Wise** స్క్రీన్ ప్రతి రాష్ట్రంలో డీలర్లను లెక్కపెట్టి, SPIC, GFL కోసం వారి అప్లికేషన్‌లు ఎక్కడ ఉన్నాయో చూపిస్తుంది.

1. **TOTAL DEALERS**, **COMPLETED** కార్డులు చెక్ చేయండి.
2. **Dealer Summary** లో, ప్రతి వరుస ఒక రాష్ట్రం. కాలమ్‌లు **As Per SAP Data**, **Saved Dealers**, **MO**, **RM**, **SMM**, **AVP**, **Completed** చూపిస్తాయి, ప్రతిదీ **SPIC**, **GFL** కోసం.
3. కాలమ్ హెడింగ్ నొక్కితే దాని ప్రకారం వరుసగా పెడుతుంది. మళ్లీ నొక్కితే తిరగబడుతుంది.
4. కింద ఉన్న **Total** వరుస అన్ని రాష్ట్రాలను కలిపి చూపిస్తుంది.
5. తాజా లెక్కలు చూడటానికి **Refresh** నొక్కండి, లేదా **Back to Dashboard** నొక్కండి.

## సాధారణ సమస్యలు

- **"Access restricted".** (అనుమతి లేదు) ఈ రిపోర్ట్ Admin, Director, AVP యూజర్లకు మాత్రమే.
- **ఒక రాష్ట్రం "Unknown" అని చూపిస్తోంది.** రాష్ట్రాల లిస్ట్ లోడ్ కాలేదు. **Refresh** నొక్కండి.
:::

::: section dealer-directory
title: డీలర్ డైరెక్టరీ
routes: /DealerDirectory
for: 
roles: 
designations: 
minutes: 1
source-hash: db991abf0063ad82
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Dealer Directory** స్క్రీన్ రాబోయే డీలర్ లిస్ట్‌కు ఒక ముందస్తు నమూనా మాత్రమే. దీనిలోని లెక్కలు, వరుసలు ఉదాహరణలు మాత్రమే, దీని బటన్లు, ఫిల్టర్‌లు ఇంకా పని చేయవు.

1. ఆకారం చూడండి: **Total Dealers**, **Active Dealers**, **Regions Covered**, **Recently Added** కార్డులు.
2. **Filters** బాక్స్‌లో తర్వాత **State**, **Region**, **Head Quarters**, **Status** ఎంచుకోవచ్చు.
3. **Dealer List** టేబుల్ తర్వాత **Dealer Code**, **Firm Name**, **State**, **Head Quarters**, **Mobile No.**, **Email**, **Status** చూపిస్తుంది.

> Tip: నిజమైన అప్లికేషన్ కనుక్కోవడానికి, **Dealer Applications Review** లిస్ట్ వాడండి.

## సాధారణ సమస్యలు

- **Excel, PDF లేదా Print నొక్కితే ఏమీ జరగడం లేదు.** ఈ బటన్లు ఇంకా పని చేయడం లేదు.
:::

::: section dealer-mapping
title: సేల్స్ ఆడిట్ ప్రోగ్రామ్‌కు డీలర్లను జోడించడం
routes: /DealerMapping
for: 
roles: 
designations: 
minutes: 3
source-hash: 42aed46703ffd3d8
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Dealer Mapping** స్క్రీన్ సేల్స్ ఆడిట్ ప్రోగ్రామ్‌లో 3వ దశ, **Dealers**. ప్రోగ్రామ్‌లో ఉన్న డీలర్ల లిస్ట్ అప్‌లోడ్ చేసి, వారి SAP అమ్మకాలను ప్లాన్‌తో పోల్చి చూస్తారు.

1. **Sales Plan** నుంచి **Save & Continue** లేదా **Continue to Dealer Mapping** తో దీన్ని తెరవండి.
2. **Upload Dealer List** కింద, Excel ఫైల్‌ను వేయండి లేదా **Upload File** నొక్కండి. ఫైల్‌లో **Dealer Code** కాలమ్ ఉండాలి.
3. మెసేజ్ చదవండి. కొన్ని వరుసలు వదిలేస్తే, **Row No.**, **Dealer Code**, **Reason** చూడటానికి **View Errors** నొక్కండి.
4. **Dealer List** లో, కోడ్ లేదా పేరుతో వెతకండి. బిన్ (చెత్తబుట్ట) డీలర్‌ను ప్రోగ్రామ్ నుంచి తీసేస్తుంది.
5. **Dealer Sales Achievement - SAP Invoice Matches** లో, ప్లాన్ చేసిన అమ్మకాలు, నిజమైన అమ్మకాలు చెక్ చేయండి. వాటిని మళ్లీ లోడ్ చేయడానికి **Refresh Matches** నొక్కండి.
6. **Plan Summary & Comparison** చెక్ చేయండి: **Required Turnover**, **Achievement Amount**, **Difference**, **Progress %**.
7. **Continue to Plan Review** నొక్కండి, లేదా **Back to Sales Plan** నొక్కండి.

## సాధారణ సమస్యలు

- **"Missing CSR-2 ID. Open this page from Sales Plan."** (CSR-2 ID లేదు. ఈ పేజీని Sales Plan నుంచి తెరవండి) సేవ్ చేసిన లింక్ నుంచి కాకుండా, Sales Plan నుంచి స్క్రీన్ తెరవండి.
- **"Upload an .xlsx or .xls file up to 5 MB."** (5 MB వరకు ఉన్న .xlsx లేదా .xls ఫైల్ అప్‌లోడ్ చేయండి) 5 MB కంటే చిన్న Excel ఫైల్ వాడండి.
- **Continue to Plan Review బూడిద రంగులో ఉంది.** ముందు కనీసం ఒక డీలర్‌ను అప్‌లోడ్ చేయండి.
:::

::: section training-program
title: డీలర్ ట్రైనింగ్ ప్రోగ్రామ్ నివేదిక (CSR-2)
routes: /DealerManagementTrainingProgram
for: 
roles: 
designations: 
minutes: 5
source-hash: f2c7c96961e19cd8
confidence: ok

**ఈ స్క్రీన్ దేనికి**
CSR-1 ప్లాన్ ఆమోదం పొందాక, ప్రోగ్రామ్ నిజంగా ఎలా జరిగిందో నమోదు చేయడానికి ఈ స్క్రీన్ వాడండి: తేదీ, ఫోటోలు, పాల్గొన్నవారు, ఖర్చులు. **CSR-2 Program Execution** లిస్ట్‌లో **View Program** తో ఇది తెరుచుకుంటుంది.

1. **Plan & Start** లో, **Approved CSR-1 Plan** చెక్ చేయండి. **Focus Details**, **Actual Event Date**, **Start Time**, **End Time**, **Actual Location** నింపండి. ఈవెంట్ ఫోటోలు, వీడియోల కోసం **Take Photo**, **Record Video** వాడండి. **Start Event** నొక్కి, నిర్ధారించడానికి మళ్లీ **Start Event** నొక్కండి.
2. **Participants & Employees** లో, **Phone Number** తో పాల్గొనేవారిని వెతికి, **Role** ఎంచుకుని **Add Participant** నొక్కండి. ఇంకా రిజిస్టర్ కాని వారి కోసం **New Farmer** వాడండి. సిబ్బందిని **Add Employee** తో చేర్చండి.
3. **Expense & Proof** లో, ప్రతి ఖర్చుకు **Actual amount (₹)**, **Bill No.**, **Date** ఎంటర్ చేసి, **Upload proofs** నొక్కండి. కార్డులు **Approved Budget**, **Actual Expense** ను పోల్చి చూపిస్తాయి.
4. **Review & Submit** లో, అన్నీ చెక్ చేయండి. ఒక కార్డుపై **Edit** నొక్కితే వెనక్కి వెళ్లి మార్చవచ్చు.
5. తర్వాత పూర్తి చేయడానికి **Save Draft** నొక్కండి. పంపడానికి **Apply CSR-2** నొక్కండి, లేదా అది మీ కోసం ఎదురుచూస్తుంటే **Validate CSR-2** లేదా ఆమోదం బటన్ నొక్కండి.

> Tip: ఫోటోలు, వీడియోలపై GPS లొకేషన్, తేదీ, సమయం నమోదవుతాయి.

## సాధారణ సమస్యలు

- **"CSR-2 can be opened only for an approved CSR-1."** (ఆమోదం పొందిన CSR-1 కి మాత్రమే CSR-2 తెరుచుకుంటుంది) CSR-1 ప్లాన్ ఆమోదం వచ్చేవరకు ఆగండి.
- **నాకు రెండు దశలు మాత్రమే కనిపిస్తున్నాయి.** కొన్ని రోల్స్ **Plan & Start**, **Participants & Employees** మాత్రమే నింపి, తర్వాత **Apply CSR-2** నొక్కుతారు.
- **"Enter a valid 10-digit mobile number."** (సరైన 10 అంకెల మొబైల్ నంబర్ ఎంటర్ చేయండి) ఫోన్ నంబర్ చెక్ చేయండి.
:::
