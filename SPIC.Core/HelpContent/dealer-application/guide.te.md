---
module: dealer-application
title: డీలర్ దరఖాస్తు
icon: bi-shop
order: 101
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
source-hash: b3660f0cee5decd6
confidence: ok

13 చిన్న దశల్లో ఒక డీలర్‌ను నమోదు చేయండి: డీలర్ ఎవరు, వ్యాపారం, యజమానులు, డబ్బు, డాక్యుమెంట్లు. తర్వాత దరఖాస్తును సబ్మిట్ చేయండి. డీలర్‌కు ఆమోదం రాకముందు RM, SMM, AVP దాన్ని చెక్ చేస్తారు.

- steps | icon: bi-list-ol | color: #7c3aed
  title: 13 దశలు, ఒక్కొక్కటిగా
  text: Next నొక్కినప్పుడు ప్రతి దశ సేవ్ అవుతుంది. తర్వాత మళ్లీ వచ్చి పూర్తి చేయవచ్చు.
- check | icon: bi-clipboard-check | color: #d97706
  title: ఒక చివరి చెక్
  text: సబ్మిట్ చేసే ముందు ఏమి మిగిలి ఉందో చివరి దశ చూపిస్తుంది.
- approve | icon: bi-shield-check | color: #16a34a
  title: మూడు ఆమోదాలు
  text: ముందు RM, తర్వాత SMM, తర్వాత AVP దరఖాస్తును ఆమోదిస్తారు.
:::

::: section flow
title: దరఖాస్తు ఎలా ముందుకు వెళ్తుంది, ఎవరు ఏమి చేస్తారు
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2413a14b313769d3
confidence: ok

ప్రతి వరుస ఒక రోల్, ప్రతి బాక్స్ ఒక దశ. మీ వరుసపై "You" (మీరు) అని గుర్తు ఉంటుంది. అక్కడ ఏమి జరుగుతుందో చూడటానికి ఒక దశను నొక్కండి.

- applicant | type: lane | color: #7c3aed | roles: MO, MDO, JMDO, Admin
  title: దరఖాస్తుదారు
  sub: MO · MDO · JMDO (కొత్త డీలర్: Admin · AVP)
- rm-team | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: రీజియన్
- smm-team | type: lane | color: #d97706 | roles: SMD, SMM
  title: SMM
  sub: రాష్ట్రం
- avp-team | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: చివరి ఆమోదం
- office-team | type: lane | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: కొత్త డీలర్లకు మాత్రమే
- register | lane: applicant | col: 1 | icon: bi-person-plus | color: #64748b | next: steps | screen: register
  title: నమోదు మొదలుపెట్టడం
  status: Draft
  text: **Create Existing Dealer** లేదా **Create New Dealer** నొక్కి, దశ 1 **Dealer Registration** నింపండి.
- steps | lane: applicant | col: 2 | icon: bi-list-ol | color: #7c3aed | next: documents | screen: experience
  title: వ్యాపార దశలు నింపడం
  status: Registration In Progress
  text: అనుభవం, అమ్మకాలు, గోడౌన్లు, మార్కెట్, కంపెనీలు, యజమానులు, అమ్మకాల ప్లాన్, ఆస్తులు, క్రెడిట్ లిమిట్ నింపండి. ప్రతి దశలో **Next** నొక్కండి.
- documents | lane: applicant | col: 3 | icon: bi-paperclip | color: #7c3aed | next: submit | screen: enclosures
  title: డాక్యుమెంట్లు అప్‌లోడ్ చేయడం
  status: Registration In Progress
  text: **Enclosures** దశలో **Required** అని గుర్తు ఉన్న ప్రతి డాక్యుమెంట్‌ను అప్‌లోడ్ చేయండి.
- submit | lane: applicant | col: 4 | icon: bi-send | color: #0b6b3a | next: rm | screen: final-submission
  title: చెక్ చేసి సబ్మిట్ చేయడం
  status: Application Ready for Submission
  text: ప్రతి సెక్షన్ పూర్తయిందో లేదో చెక్ చేసి **Submit Registration** నొక్కండి.
- incomplete | lane: applicant | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: submit | to: steps | screen: final-submission
  title: ఏదో మిగిలిపోయింది
  text: ఒక సెక్షన్ లేదా డాక్యుమెంట్ మిగిలిపోతే, యాప్ సబ్మిట్ చేయదు. పెన్సిల్‌తో ఆ సెక్షన్ తెరిచి, సరిచేసి, మళ్లీ సబ్మిట్ చేయండి.
- rm | lane: rm-team | col: 5 | icon: bi-hourglass-split | color: #0d9488 | next: smm | return: submit
  title: RM చెక్ చేస్తారు
  status: In RM
  text: RM దరఖాస్తును ఆమోదిస్తారు, లేదా తిరస్కరించి దరఖాస్తుదారుకు తిరిగి పంపుతారు.
- smm | lane: smm-team | col: 6 | icon: bi-hourglass-split | color: #d97706 | next: avp | return: rm
  title: SMM చెక్ చేస్తారు
  status: In SMM
  text: SMM ఆమోదిస్తారు, లేదా తిరస్కరించి RM కి తిరిగి పంపుతారు.
- avp | lane: avp-team | col: 7 | icon: bi-patch-check | color: #2563eb | next: office | return: smm
  title: AVP ఆమోదిస్తారు
  status: In AVP
  text: AVP చివరి ఆమోదం ఇస్తారు, లేదా తిరస్కరించి SMM కి తిరిగి పంపుతారు.
- office | lane: office-team | col: 8 | icon: bi-123 | color: #64748b | next: done
  title: కొత్త డీలర్‌కు Dealer ID లు
  status: In AVP Office
  text: కొత్త డీలర్‌కు మాత్రమే, AVP Office కంపెనీ Dealer ID లను సేవ్ చేస్తారు. ఇప్పటికే ఉన్న డీలర్‌కు ఈ దశ ఉండదు.
- done | lane: applicant | col: 9 | icon: bi-check-circle | color: #16a34a | screen: application-pdf
  title: డీలర్ ఆమోదం పొందారు
  status: Approved
  text: దరఖాస్తు పూర్తయింది. డీలర్ కార్డు నుంచి డీలర్‌షిప్ దరఖాస్తు ఫారం తెరవవచ్చు.
:::

::: section statuses
title: మీకు కనిపించే ప్రతి స్టేటస్
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 329a69141c3bcda8
confidence: ok

- draft | color: #64748b
  label: Draft
  text: సేవ్ అయ్యింది కానీ ఇంకా సబ్మిట్ చేయలేదు. ఇప్పటికీ మార్చవచ్చు.
- in-progress | color: #7c3aed
  label: Registration In Progress
  text: మీరు దశలు నింపుతున్నారు.
- ready | color: #0b6b3a
  label: Application Ready for Submission
  text: అన్ని సెక్షన్లు, డాక్యుమెంట్లు పూర్తయ్యాయి. మీరు సబ్మిట్ చేయవచ్చు.
- in-rm | color: #0d9488
  label: In RM
  text: సబ్మిట్ అయ్యింది. RM చెక్ చేస్తున్నారు.
- in-smm | color: #d97706
  label: In SMM
  text: RM ఆమోదించారు. SMM చెక్ చేస్తున్నారు.
- in-avp | color: #2563eb
  label: In AVP
  text: SMM ఆమోదించారు. AVP కోసం ఎదురుచూస్తోంది.
- in-office | color: #64748b
  label: In AVP Office
  text: కొత్త డీలర్‌ను AVP ఆమోదించారు. కంపెనీ Dealer ID ల కోసం ఎదురుచూస్తోంది.
- approved | color: #16a34a
  label: Approved
  text: దరఖాస్తుకు పూర్తి ఆమోదం వచ్చింది.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: దరఖాస్తుదారుకు తిరిగి పంపబడింది. సరిచేసి మళ్లీ సబ్మిట్ చేయండి.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: సరిచేయడానికి RM కి తిరిగి పంపబడింది.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: సరిచేయడానికి SMM కి తిరిగి పంపబడింది.
- uploaded | color: #16a34a
  label: Uploaded
  text: డాక్యుమెంట్ జత చేయబడింది.
- missing | color: #ea580c
  label: Missing
  text: డాక్యుమెంట్ ఇంకా జత చేయలేదు.
- consent | color: #0891b2
  label: Consent Provided
  text: డీలర్ క్రెడిట్ లిమిట్ వివరాలు ఇవ్వలేదు. క్రెడిట్ లిమిట్ దశలు దాటవేయబడతాయి.
- active | color: #16a34a
  label: Active
  text: డీలర్ ఇప్పుడు వ్యాపారం చేస్తున్నారు.
- inactive | color: #d97706
  label: Inactive
  text: డీలర్ ఇప్పుడు వ్యాపారం చేయడం లేదు.
- terminated | color: #475569
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
source-hash: 17fdf789d027bf63
confidence: ok

మీ కార్డు హైలైట్ అయి కనిపిస్తుంది. మీ రోల్ వాడే స్క్రీన్‌లు మాత్రమే మీకు కనిపిస్తాయి.

- creator | color: #7c3aed | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - ఇప్పటికే ఉన్న డీలర్లను నమోదు చేయడం
  - అన్ని దశలు నింపి డాక్యుమెంట్లు అప్‌లోడ్ చేయడం
  - సబ్మిట్ చేయడం, RM తిరస్కరించిన దరఖాస్తును సరిచేయడం
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - Create New Dealer తో కొత్త డీలర్లను నమోదు చేయడం
  - AVP చివరి ఆమోదం ఇవ్వడం
- rm | color: #0d9488 | roles: RM, RMD
  title: RM
  - మీ రీజియన్ దరఖాస్తులు చెక్ చేయడం
  - ఆమోదించడం, లేదా తిరస్కరించి దరఖాస్తుదారుకు తిరిగి పంపడం
- smm | color: #d97706 | roles: SMD, SMM
  title: SMM
  - మీ రాష్ట్రం దరఖాస్తులు చెక్ చేయడం
  - ఆమోదించడం, లేదా తిరస్కరించి RM కి తిరిగి పంపడం
- office | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - ఆమోదం పొందిన కొత్త డీలర్లకు కంపెనీ Dealer ID లు సేవ్ చేయడం
:::

::: section faq
title: సమస్యలు, జవాబులు
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 30e6b2b2e39499dd
confidence: ok

- warehouse-stuck | popular: true
  q: Warehouse దశ దాటి ముందుకు వెళ్లలేకపోతున్నాను
  a: కనీసం రెండు గోడౌన్లు, రెండు రైల్ హెడ్‌లు, రెండు పోర్ట్‌లు కావాలి. ప్రతి దాన్ని దాని **Add** బటన్‌తో జోడించి, తర్వాత **Next** నొక్కండి.
- not-submitted | popular: true
  q: Submit Registration నొక్కితే కొన్ని సెక్షన్లు పూర్తి కాలేదని వస్తోంది
  a: **Final Submission** లో, **Section Completion Status**, **Enclosures** కింద ఆరెంజ్ గుర్తుల కోసం చూడండి. పెన్సిల్ నొక్కి, ఆ దశ పూర్తి చేసి, మళ్లీ సబ్మిట్ చేయండి.
- red-step
  q: పైన ఉన్న దశల బార్‌లో ఒక భాగం ఎరుపుగా ఉంది
  a: ఆ దశలో ఏదో సమస్య ఉంది లేదా వివరాలు మిగిలిపోయాయి. ఎరుపు భాగాన్ని నొక్కి ఆ దశ తెరిచి సరిచేయండి.
- ocr
  q: GST, PAN లేదా బ్యాంక్ నంబర్‌ను యాప్ తనంతట తానే నింపింది
  a: అప్‌లోడ్ చేసిన ఫైల్‌ను యాప్ చదువుతుంది. నంబర్‌ను ఎప్పుడూ చెక్ చేయండి. తప్పుగా ఉంటే, సరైన నంబర్ టైప్ చేయండి.
- skipped-credit
  q: Credit Limit దశలు దాటవేయబడ్డాయి
  a: **Investment** దశలో అంగీకారం (consent) ఇచ్చారు. క్రెడిట్ లిమిట్ నింపడానికి **Investment** తెరిచి **Remove Consent** నొక్కండి.
- no-code
  q: నా కొత్త డీలర్‌కు Dealer Code లేదు
  a: కొత్త డీలర్‌కు చివరి ఆమోదం వచ్చిన తర్వాతే Dealer Code వస్తుంది.
:::

::: section register
title: డీలర్ నమోదు మొదలుపెట్టడం (దశ 1)
routes: /Register
for: 
roles: 
designations: 
screenshot: register.png
hotspots: 1 @ 2.2,21.9,95.5,33.1 | 2 @ 2.2,57.9,95.5,32.4 | 3 @ 2.2,93.1,95.5,6.9 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: register.phone.jpg
hotspots-phone: 1 @ 3.1,14.7,93.8,42.9 | 2 @ 3.1,58.8,93.8,39.3 | 3 @ 3.1,99.2,93.8,0.8 | 4 @ - | 5 @ - | 6 @ -
tour: 1 > [data-help=dr-basic] | 2 > [data-help=dr-business] | 3 > [data-help=dr-location] | 4 > [data-help=dr-bank] | 5 > [data-help=dr-tax] | 6 > [data-help=dr-license]
minutes: 6
source-hash: 1268f37d3a5ce326
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Dealer Registration** స్క్రీన్ 13 దశల్లో దశ 1. డీలర్ ఎవరు, షాప్ ఎక్కడ ఉంది, బ్యాంక్ ఖాతా, పన్ను, లైసెన్స్ వివరాలు ఇది నమోదు చేస్తుంది. **Register** మెనూ తెరిచి **Create Existing Dealer** నొక్కండి. పూర్తిగా కొత్త డీలర్ కోసం Admin, AVP **Create New Dealer** వాడతారు.

1. **Basic Information** కింద, **Dealer**, **Department** లేదా **Institution** ఎంచుకుని, **SPIC**, **GREEN STAR** లేదా రెండింటినీ నొక్కండి. **State**, **Region**, **HQ** చెక్ చేయండి. ఇవి మీ ప్రొఫైల్ నుంచి నిండి ఉంటాయి. ఇప్పటికే ఉన్న డీలర్ అయితే, **Active**, **Inactive** లేదా **Terminated** కూడా ఎంచుకోండి.
2. **Business Information** కింద, **Dealer Code & Name**, **Parent Dealer Code & Name** ఎంచుకోండి (ఇప్పటికే ఉన్న డీలర్లకు మాత్రమే). ఎరువుల లైసెన్స్‌లో ఉన్నట్టు **Firm Name** టైప్ చేసి, **Date of Appointment for SPIC** లేదా **for Greenstar** ఎంచుకోండి.
3. **Primary Location** కింద, షాప్ దగ్గర నిలబడి **Use Current Location** నొక్కండి, లేదా **Google Map URL** పేస్ట్ చేయండి. తర్వాత చిరునామా, **Pincode**, **District**, **State**, మూడు ఫోన్ నంబర్లు నింపండి.
4. **Bank Details** కింద, **Upload Cheque (PDF or Image)** తో చెక్కును అప్‌లోడ్ చేయండి. యాప్ దాన్ని చదివి ఖాతా వివరాలు నింపుతుంది. **Account Holder Name**, **Account Number**, **Bank Name**, **Bank Branch**, **IFSC Code** చెక్ చేయండి.
5. **Tax & Compliance** కింద, GST సర్టిఫికెట్, PAN కార్డు, e-Aadhaar లను PDF ఫైల్‌లుగా అప్‌లోడ్ చేయండి. యాప్ ప్రతి ఫైల్‌ను చదివి నంబర్ నింపుతుంది. ప్రతి నంబర్ చెక్ చేయండి. **TAN Number** తప్పనిసరి కాదు.
6. **Wholesale Fertilizer Details (as per iFMS)** లేదా **Retail Fertilizer Details (as per iFMS)** నింపండి: లైసెన్స్ నంబర్, **Expiry Date**, **mFMS ID**, లైసెన్స్ ఫైల్. **Office Automation** కి జవాబు ఇచ్చి, తర్వాత **Next** నొక్కండి.

> Tip: ఇప్పటికే ఉన్న డీలర్ అయితే, **Trade Deposit Details for SPIC** లేదా **for Greenstar** కూడా నింపండి: మొత్తం, **Receipt No**, **Deposit Date**.
> Careful: **Department** లేదా **Terminated** స్టేటస్ ఎంచుకుంటే, ఈ ఒక్క పేజీ మాత్రమే ఉంటుంది. బటన్‌పై **Submit** అని ఉంటుంది, అది ఫారంను వెంటనే రివ్యూకి పంపుతుంది.

## ఫీల్డ్‌ల వివరాలు

| ఫీల్డ్ | ఏమి ఎంటర్ చేయాలి |
|---|---|
| **Firm Name** | ఎరువుల లైసెన్స్‌లో ఉన్న వ్యాపారం పేరు. తప్పనిసరి. |
| **Pincode** | 6 అంకెలు. యాప్ ఆ ప్రాంతాన్ని వెతుకుతుంది. |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 అంకెల మొబైల్ నంబర్లు. |
| **GST Number** | 15 అక్షరాలు, ఉదాహరణకు 22AAAAA0000A1Z5. |
| **PAN Number** | 10 అక్షరాలు, ఉదాహరణకు ABCDE1234F. |
| **Aadhaar Number** | సరిగ్గా 12 అంకెలు. |
| GST, PAN, Aadhaar, TAN ఫైల్‌లు | PDF మాత్రమే, 5 MB కంటే తక్కువ. |
| లైసెన్స్ | హోల్‌సేల్ లేదా రిటైల్, కనీసం ఒక లైసెన్స్, దాని సర్టిఫికెట్‌తో. |

## సాధారణ సమస్యలు

- **Next నొక్కినా పేజీ ముందుకు వెళ్లడం లేదు.** నక్షత్రం గుర్తు ఉన్న ఒక ఫీల్డ్ ఖాళీగా లేదా తప్పుగా ఉంది. ఫీల్డ్ కింద ఎరుపు మెసేజ్ చూడండి, ఉదాహరణకు "Pincode must be 6 digits".
- **"At least one Fertilizer License (Wholesale or Retail) is required"** (కనీసం ఒక ఎరువుల లైసెన్స్ కావాలి) ఒక లైసెన్స్ నింపి దాని సర్టిఫికెట్ అప్‌లోడ్ చేయండి.
- **"OCR could not extract the required information from this document."** (ఈ డాక్యుమెంట్ నుంచి కావాల్సిన వివరాలను యాప్ చదవలేకపోయింది) ఇంకా స్పష్టమైన ఫైల్ అప్‌లోడ్ చేయండి, లేదా వివరాలు మీరే టైప్ చేయండి.
- **"Only PDF files are allowed" or "File size must be less than 5 MB".** (PDF మాత్రమే వాడాలి, లేదా ఫైల్ 5 MB కంటే చిన్నదిగా ఉండాలి) డాక్యుమెంట్‌ను చిన్న PDF గా సేవ్ చేసి మళ్లీ అప్‌లోడ్ చేయండి.
:::

::: section experience
title: ఎరువుల వ్యాపారంలో అనుభవం (దశ 2)
routes: /Experience
for: 
roles: 
designations: 
screenshot: experience.png
screenshot-phone: experience.phone.jpg
minutes: 3
source-hash: 4c4087f2c0c5036d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
డీలర్ ఎన్ని సంవత్సరాలుగా ఎరువులు అమ్ముతున్నారు, ఏ కంపెనీలకు అని **Experience In Fertilizer Trade** స్క్రీన్ నమోదు చేస్తుంది.

1. **Year of Experience** లో, డీలర్ ఎన్ని సంవత్సరాలుగా ఎరువుల వ్యాపారంలో ఉన్నారో ఎంటర్ చేయండి.
2. **Add Experience** కింద, **Company Name** ఎంచుకుని **Number of the years** ఎంటర్ చేయండి.
3. ఆ కంపెనీకి **Quantity (MT)**, **Turnover (Lakhs)** ఎంటర్ చేయండి.
4. డీలర్ ఇప్పుడు ఆ కంపెనీతో పని చేయకపోతే **Still Active** ను ఆఫ్ చేయండి.
5. **Add Experience** నొక్కండి. కంపెనీ **Saved Experience** కింద కనిపిస్తుంది. ప్రతి కంపెనీకి ఇలాగే చేయండి.
6. ఒక రికార్డు మార్చడానికి పెన్సిల్ వాడి **Update Experience** నొక్కండి, లేదా డిలీట్ చేయడానికి బిన్ నొక్కండి. తర్వాత **Next** నొక్కండి.

## సాధారణ సమస్యలు

- **"Please select a company."** (ఒక కంపెనీని ఎంచుకోండి) **Add Experience** నొక్కే ముందు **Company Name** ఎంచుకోండి.
- **దశల బార్‌లో దశ 2 ఎరుపుగా మారింది.** **Year of Experience** ఎంటర్ చేసి, కనీసం ఒక కంపెనీని జోడించండి.
:::

::: section annual-sales
title: గత సంవత్సరం అమ్మకాలు (దశ 3)
routes: /AnnualSales
for: 
roles: 
designations: 
screenshot: annual-sales.png
screenshot-phone: annual-sales.phone.jpg
minutes: 4
source-hash: 3d5342d2000c7789
confidence: ok

**ఈ స్క్రీన్ దేనికి**
డీలర్ **Last Financial Year** లో అన్ని కంపెనీలకు కలిపి ఏమి అమ్మారో **Annual Sales Data For All Companies** స్క్రీన్ నమోదు చేస్తుంది.

1. టేబుల్ పైన ఉన్న ఒక కేటగిరీ ట్యాబ్ నొక్కండి.
2. ఒక ప్రొడక్ట్ గ్రూప్‌ను నొక్కి తెరిచి దాని ప్రొడక్ట్‌లు చూడండి.
3. ప్రతి ప్రొడక్ట్‌కు **Own Retail Qty** ఎంటర్ చేయండి. డీలర్‌కు హోల్‌సేల్ లైసెన్స్ ఉంటే, **Sale to Dealer Qty** కూడా ఎంటర్ చేయండి.
4. టర్నోవర్ (**TO (₹)in Lakhs**), మొత్తాలు యాప్ తానే లెక్కిస్తుంది. టేబుల్ కింద ఉన్న సారాంశ కార్డులు, **Grand Total** చెక్ చేయండి.
5. ఏదైనా ప్రొడక్ట్ లేకపోతే, **Add Product** నొక్కి, **Category**, **Product Group**, **Product Name**, **Rate Per Unit (RPU)** నింపి, **Save** నొక్కండి.
6. **Next** నొక్కండి.

## సాధారణ సమస్యలు

- **Sale to Dealer Qty కాలమ్ లేదు.** దశ 1 లో డీలర్‌కు హోల్‌సేల్ లైసెన్స్ ఉంటేనే అది కనిపిస్తుంది.
- **దశల బార్‌లో దశ 3 ఎరుపుగా మారింది.** కనీసం ఒక ప్రొడక్ట్‌కు పరిమాణం ఎంటర్ చేయండి.
- **"A Product with the name ... already exists in this Category."** (ఈ పేరుతో ప్రొడక్ట్ ఈ కేటగిరీలో ఇప్పటికే ఉంది) ఆ ప్రొడక్ట్ ఇప్పటికే లిస్ట్‌లో ఉంది. దాని ప్రొడక్ట్ గ్రూప్‌లో వెతకండి.
:::

::: section warehouse
title: దగ్గరలోని గోడౌన్లు, రైల్ హెడ్‌లు, పోర్ట్‌లు (దశ 4)
routes: /Warehouse
for: 
roles: 
designations: 
screenshot: warehouse.png
screenshot-phone: warehouse.phone.jpg
minutes: 4
source-hash: bd545fbcf4350bec
confidence: ok

**ఈ స్క్రీన్ దేనికి**
డీలర్‌కు దగ్గరలో ఉన్న సౌకర్యాలను **Warehouse & Logistics** స్క్రీన్ నమోదు చేస్తుంది. దశ 1 లో మీరు ఇచ్చిన షాప్ లొకేషన్ నుంచి దూరం లెక్కించబడుతుంది.

1. **Logistics Note Information** చదవండి. కనీసం రెండు గోడౌన్లు, రెండు రైల్ హెడ్‌లు, రెండు పోర్ట్‌లు కావాలి.
2. **Warehouse** కార్డులో, **Facility Name** వెతికి ఎంచుకోండి. **Distance (KM)** తానే నిండుతుంది.
3. **Freight (₹/ton/Km)** ఎంటర్ చేసి **Add Warehouse** నొక్కండి.
4. **Rail Head**, **Port** కార్డులలో కూడా ఇలాగే చేయండి.
5. **Selected Nearest Facilities** కింద ఉన్న లిస్ట్ చెక్ చేయండి. మార్చడానికి పెన్సిల్, డిలీట్ చేయడానికి బిన్ వాడండి.
6. **Next** నొక్కండి.

## సాధారణ సమస్యలు

- **"A minimum of two Warehouses, two Rail Heads, and two Ports must be configured to proceed further."** (ముందుకు వెళ్లడానికి కనీసం రెండు గోడౌన్లు, రెండు రైల్ హెడ్‌లు, రెండు పోర్ట్‌లు కావాలి) ముందు మిగిలిన సౌకర్యాలను జోడించండి.
- **"Dealer location (Latitude/Longitude) is not set."** (డీలర్ లొకేషన్ సెట్ కాలేదు) దశ 1 కి తిరిగి వెళ్లి **Google Map URL** లేదా **Latitude & Longitude** నింపండి.
- **Add బటన్ బూడిద రంగులో ఉంది.** ముందు **Facility Name** ఎంచుకుని, దూరం కనిపించే వరకు ఆగండి.
:::

::: section market-details
title: మార్కెట్ వివరాలు (దశ 5)
routes: /MarketDetails
for: 
roles: 
designations: 
screenshot: market-details.png
screenshot-phone: market-details.phone.jpg
minutes: 3
source-hash: a7a15b3e04404881
confidence: ok

**ఈ స్క్రీన్ దేనికి**
డీలర్ సేవ చేసే బ్లాక్‌లు, గ్రామాలను, వాటి పంటలు, రైతులతో **Market Details** స్క్రీన్ నమోదు చేస్తుంది.

1. **Blocks / Villages** కింద, **Name of Block/ Village** టైప్ చేయండి.
2. **Major Crops** తెరిచి ముఖ్యమైన పంటలను టిక్ చేయండి. లిస్ట్‌లో వెతకవచ్చు.
3. **No of Farmer** ఎంటర్ చేయండి, ఆ బాక్స్ కనిపిస్తే **No of Dealer** కూడా ఎంటర్ చేయండి.
4. **Season From & To Month**, **Irrigation Type** ఎంచుకోండి: **CANAL**, **TANK**, **WELL** లేదా **RAINFED**.
5. **Add Blocks/ Villages** నొక్కండి. ఎంట్రీ **Added Market Details** కింద కనిపిస్తుంది. ప్రతి బ్లాక్ లేదా గ్రామానికి ఇలాగే చేయండి.
6. **Next** నొక్కండి.

## సాధారణ సమస్యలు

- **"No regional blocks added yet."** (ఇంకా బ్లాక్‌లు ఏవీ జోడించలేదు) కనీసం ఒక బ్లాక్ లేదా గ్రామాన్ని జోడించండి. లేకపోతే దశ 5 ఎరుపుగా కనిపిస్తుంది.
- **నాకు No of Dealer కనిపించడం లేదు.** డీలర్‌కు హోల్‌సేల్ లైసెన్స్ ఉంటేనే అది కనిపిస్తుంది.
:::

::: section companies
title: డీలర్ ప్రాంతంలోని కంపెనీలు (దశ 6)
routes: /Companies
for: 
roles: 
designations: 
screenshot: companies.png
screenshot-phone: companies.phone.jpg
minutes: 1
source-hash: 4709346d2ff331c4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
డీలర్ ప్రాంతంలో ఏ ఎరువుల కంపెనీలు అమ్ముతున్నాయో **Companies Operating In Your Place** స్క్రీన్ చూపిస్తుంది.

1. **SPIC**, **GREEN STAR** తో సహా, ఆ ప్రాంతంలో అమ్మే ప్రతి కంపెనీని నొక్కండి. టిక్ ఉంటే అది ఎంచుకున్నట్టు.
2. టిక్ తీసేయడానికి కంపెనీని మళ్లీ నొక్కండి.
3. **Next** నొక్కండి.

## సాధారణ సమస్యలు

- **దశల బార్‌లో దశ 6 ఎరుపుగా మారింది.** కనీసం ఒక కంపెనీని టిక్ చేయండి.
:::

::: section proprietor
title: యజమాని, భాగస్వాముల వివరాలు (దశ 7)
routes: /Proprietor
for: 
roles: 
designations: 
screenshot: proprietor.png
minutes: 6
source-hash: 02521efb7ec5b6b9
confidence: ok

**ఈ స్క్రీన్ దేనికి**
వ్యాపారం యొక్క చట్టపరమైన రకాన్ని, ప్రతి యజమాని లేదా భాగస్వామిని వారి కుటుంబంతో **Proprietor Details** స్క్రీన్ నమోదు చేస్తుంది. ఈ వివరాలు SDWA సంక్షేమ పథకానికి కూడా వాడతారు.

1. **Business Entity Type** ఎంచుకోండి: **Sole Proprietor**, **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** లేదా **Society**.
2. **Ownership Information** కింద, **Authorized Name**, **Son of / Daughter of**, **Phone Number**, **PAN number**, **Aadhaar Card Number** నింపండి.
3. **Proprietor / Managing Partner Image** అప్‌లోడ్ చేసి, **Residential Address Details** నింపండి.
4. **Family Members** కింద, ప్రతి సభ్యుడిని జోడించి **Add Details** నొక్కండి. **Other Occupation / Agencies** కింద, **Yes** లేదా **No** ఎంచుకుని, వేరే వ్యాపారం ఏదైనా ఉంటే జోడించండి.
5. **Save Details** నొక్కండి. ఆ వ్యక్తి **Saved Details** కింద కనిపిస్తారు. ప్రతి భాగస్వామిని ఇలాగే జోడించండి.
6. **Next** నొక్కండి.

| Business Entity Type | కావాల్సిన వ్యక్తులు |
|---|---|
| **Sole Proprietor** | సరిగ్గా 1 యజమాని. |
| **Society** | కనీసం 1 అధికారం ఉన్న వ్యక్తి. |
| **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** | కనీసం 2 భాగస్వాములు. |

## సాధారణ సమస్యలు

- **"Please upload Proprietor / Managing Partner Image before saving."** (సేవ్ చేసే ముందు యజమాని ఫోటో అప్‌లోడ్ చేయండి) ఫోటో అప్‌లోడ్ చేసి, మళ్లీ **Save Details** నొక్కండి.
- **"Please add at least 2 partners for the selected entity type."** (ఈ రకానికి కనీసం 2 భాగస్వాములను జోడించండి) **Next** నొక్కే ముందు రెండవ భాగస్వామిని సేవ్ చేయండి.
- **ఒక వ్యక్తిని సేవ్ చేశాక ఫారం కనిపించడం లేదు.** **Sole Proprietor** కి ఒక్కరే అనుమతి. దాన్ని మార్చడానికి **Saved Details** కింద పెన్సిల్ వాడండి.
:::

::: section sales-planning
title: రాబోయే సంవత్సరానికి అమ్మకాల ప్లాన్ (దశ 8)
routes: /SalesPlaning
for: 
roles: 
designations: 
screenshot: sales-planning.png
screenshot-phone: sales-planning.phone.jpg
minutes: 4
source-hash: b908e1cc20e482d4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
రాబోయే ఆర్థిక సంవత్సరంలో, ఏప్రిల్ నుంచి మార్చి వరకు ప్రతి నెలా డీలర్ ఎంత అమ్మాలని ప్లాన్ చేస్తున్నారో **Sales Planning & Forecast** స్క్రీన్ నమోదు చేస్తుంది.

1. **Business Plan** కింద, ఒక కేటగిరీ ట్యాబ్ నొక్కండి.
2. ఒక ప్రొడక్ట్ గ్రూప్‌ను నొక్కి తెరవండి.
3. ప్రతి ప్రొడక్ట్‌కు, **April** నుంచి **March** వరకు ప్రతి నెల కింద ప్లాన్ చేసిన పరిమాణం ఎంటర్ చేయండి. లక్షల్లో విలువ, **Total** యాప్ తానే లెక్కిస్తుంది.
4. **Monthly Total** వరుస, **Fertilizer Products Summary** లాంటి సారాంశ కార్డులు చెక్ చేయండి.
5. **Next** నొక్కండి.

## సాధారణ సమస్యలు

- **దశల బార్‌లో దశ 8 ఎరుపుగా మారింది.** కనీసం ఒక ప్రొడక్ట్‌కు ప్లాన్ ఎంటర్ చేయండి.
:::

::: section investment
title: ఆస్తులు, అప్పులు, క్రెడిట్ అంగీకారం (దశ 9)
routes: /Investment
for: 
roles: 
designations: 
screenshot: investment.png
screenshot-phone: investment.phone.jpg
minutes: 5
source-hash: 6476f16e152121ca
confidence: ok

**ఈ స్క్రీన్ దేనికి**
డీలర్‌కు ఏమి ఉంది, ఎంత అప్పు ఉంది అని **Investment** స్క్రీన్ నమోదు చేసి, **Net Financial Position** చూపిస్తుంది.

1. డీలర్ క్రెడిట్ లిమిట్ వివరాలు ఇవ్వకపోతే, **Give Consent** నొక్కండి. అప్పుడు క్రెడిట్ లిమిట్ దశలు దాటవేయబడి సున్నాగా నమోదవుతాయి. **Remove Consent** దాన్ని రద్దు చేస్తుంది.
2. **Assets (Banking & Properties)** కింద, **Banking**, **Land** లేదా **Building** లో కనీసం ఒకటి జోడించండి. కార్డు నింపి **Add Banking**, **Add Land** లేదా **Add Building** నొక్కండి.
3. భూమి, బిల్డింగ్‌లకు, మీ దగ్గర ఉంటే **EC (Encumbrance Certificate)**, **Property Document**, **Valuation Certificate** అప్‌లోడ్ చేయండి.
4. **Movable**, **Infrastructure** కింద, **Movable Assets Value**, **Own Godown Capacity**, **Rent Godown Capacity** ఎంటర్ చేయండి.
5. **Loan Liabilities List** కింద, ప్రతి లోన్‌కు **Loan Source / Institution**, **Loan Value (₹) In Lakhs** ఎంటర్ చేసి, **Add Loan** నొక్కండి.
6. **Net Financial Position** చెక్ చేయండి: **Fixed Assets** మైనస్ **Total Liabilities**. తర్వాత **Next** నొక్కండి.

## సాధారణ సమస్యలు

- **"Please add details for at least one of Banking, Land, or Building before proceeding."** (ముందుకు వెళ్లే ముందు Banking, Land, Building లో కనీసం ఒకటి జోడించండి) ముందు ఒక ఆస్తిని జోడించండి.
- **"OD Value is required when Banking details are provided."** (బ్యాంకింగ్ వివరాలు ఇస్తే OD విలువ తప్పనిసరి) **OD Limit (₹) Lakhs** ఎంటర్ చేయండి.
- **"Market Value is required when Land details are provided."** (భూమి వివరాలు ఇస్తే మార్కెట్ విలువ తప్పనిసరి) మార్కెట్ విలువ ఎంటర్ చేయండి. బిల్డింగ్‌కు కూడా ఇదే నియమం.
:::

::: section credit-limit
title: క్రెడిట్ లిమిట్ లేదా ట్రేడ్ డిపాజిట్ (దశలు 10, 11)
routes: /CreditLimit, /CreditLimitForGreenStar
for: 
roles: 
designations: 
screenshot: credit-limit.png
screenshot-phone: credit-limit.phone.jpg
minutes: 5
source-hash: d34b3a1b414aa494
confidence: ok

**ఈ స్క్రీన్ దేనికి**
దశ 10 SPIC కోసం, దశ 11 Greenstar కోసం. దశ 1 లో **GREEN STAR** ఎంచుకుంటేనే దశ 11 కనిపిస్తుంది. ఇప్పటికే ఉన్న డీలర్‌కు, ఈ స్క్రీన్‌లు (**Credit Limit — SPIC**, **Credit Limit — GreenStar**) సిఫార్సు చేసిన క్రెడిట్ లిమిట్‌ను లెక్కిస్తాయి. కొత్త డీలర్‌కు, ఇవి బదులుగా ట్రేడ్ డిపాజిట్ వివరాలు తీసుకుంటాయి.

1. పైన ఉన్న డీలర్ సారాంశం, **Existing Credit Limit** (**Valid From**, **Valid To**), అమ్మకాల ప్లాన్ కార్డులు చెక్ చేయండి.
2. **Details of Securities** కింద, **Fixed Deposit with SPIC** లేదా **BG in favour of SPIC** లాంటి ప్రతి సెక్యూరిటీకి **Number**, **Other Details**, **Amount (₹) In Lakhs** ఎంటర్ చేయండి.
3. **SPIC Valuation** లేదా **Greenstar Valuation** టేబుల్‌లో, చాలా విలువలు ముందు దశల నుంచి వస్తాయి. **Monthly Average Net Overdues during last year** మీరే ఎంటర్ చేయండి. ఇది తప్పనిసరి, 0 లేదా అంతకంటే ఎక్కువ.
4. **Grand Total** స్కోర్, **Recommended Credit Limit** బాక్స్ చెక్ చేయండి: **Proposed Limit**, **Credit Multiplier**, **MO Recommended Credit Limit**.
5. ఏదైనా **Additional Credit Limit (₹) In Lakhs** ఉంటే ఎంటర్ చేసి, **Next** నొక్కండి.

## కొత్త డీలర్ కోసం

- దశ 10 లో, **Application Fee** (**Bank**, **DD Number**, **DD Date**, **Amount (₹)**, **Payable At**), **Trade Deposit Details** నింపండి. రెండు అటాచ్‌మెంట్లను PDF, JPG లేదా PNG గా అప్‌లోడ్ చేయండి.
- దశ 11 లో, **GFL Trade Deposit Details** నింపి అటాచ్‌మెంట్ అప్‌లోడ్ చేయండి.

> Tip: ఆమోదం పొందిన డీలర్‌కు, డీలర్ కార్డులోని **Credit Limit** బటన్ ఈ స్క్రీన్‌ను విడిగా తెరుస్తుంది. అప్పుడు కింద బటన్‌పై **Submit** అని ఉంటుంది.

## సాధారణ సమస్యలు

- **"Monthly Average Net Overdues during last year is required and must be 0 or greater."** (ఈ విలువ తప్పనిసరి, 0 లేదా అంతకంటే ఎక్కువ ఉండాలి) విలువ 0 అయినా ఎంటర్ చేయండి.
- **"Complete all SPIC Application Fee and Trade Deposit fields, and upload both attachments."** (అన్ని ఫీల్డ్‌లు నింపి, రెండు అటాచ్‌మెంట్లు అప్‌లోడ్ చేయండి) ప్రతి బాక్స్ నింపి రెండు ఫైల్‌లు అప్‌లోడ్ చేయండి.
- **ఈ దశలు తెరుచుకోవడం లేదు.** **Investment** దశలో అంగీకారం (consent) ఇచ్చారు.
:::

::: section enclosures
title: డాక్యుమెంట్లు అప్‌లోడ్ చేయడం (దశ 12)
routes: /Enclosures
for: 
roles: 
designations: 
screenshot: enclosures.png
minutes: 5
source-hash: f0405468232d5c91
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Enclosures** స్క్రీన్ అన్ని డాక్యుమెంట్లను ఒకే చోట సేకరిస్తుంది. ఏ డాక్యుమెంట్లు **Uploaded**, ఏవి **Missing** అని ఇది చూపిస్తుంది.

1. పైన ఉన్న **Document Completion Progress** చెక్ చేయండి. ఎన్ని డాక్యుమెంట్లు అప్‌లోడ్ అయ్యాయి, ఎన్ని తప్పనిసరి డాక్యుమెంట్లు పెండింగ్‌లో ఉన్నాయో అది చూపిస్తుంది.
2. **Documents already added in previous steps** కింద, GST, PAN, Aadhaar, లైసెన్స్ ఫైల్‌లు చెక్ చేయండి. **Missing** గా ఉన్నదాన్ని **Upload Document** తో అప్‌లోడ్ చేయండి.
3. **Partner / Proprietor Documents**, **Property Documents (Land & Building)** చెక్ చేయండి. మిగిలిపోయిన ఫోటో, Aadhaar, PAN, EC, ఆస్తి డాక్యుమెంట్ లేదా వాల్యుయేషన్ సర్టిఫికెట్ అప్‌లోడ్ చేయండి.
4. **Additional Documents** కింద, సంవత్సరం ఎంచుకుని **IT Returns for Last Year**, **IT Returns for Last 2nd Year**, ఇంకా **Required** అని గుర్తు ఉన్న మిగతా అన్ని డాక్యుమెంట్లు అప్‌లోడ్ చేయండి.
5. స్పెసిమెన్ సంతకం కోసం, **Download Template** నొక్కి, ప్రింట్ తీసి, సంతకం చేసే అందరితో సంతకం పెట్టించి, **Spic Specimen Signature** లేదా **Greenstar Specimen Signature** కింద అప్‌లోడ్ చేయండి.
6. చివరి చెక్‌కు వెళ్లడానికి **Next** నొక్కండి.

> Careful: డాక్యుమెంట్‌ను డిలీట్ చేస్తే అది మళ్లీ తిరిగి రాదు. పొరపాటున డిలీట్ చేస్తే, సరైన ఫైల్‌ను మళ్లీ అప్‌లోడ్ చేయండి.

| డాక్యుమెంట్ | ఏమి అప్‌లోడ్ చేయాలి |
|---|---|
| చాలా డాక్యుమెంట్లు | PDF, JPG లేదా PNG, 10 MB వరకు. |
| **List of Retailers with Complete Details** | అడిగినప్పుడు Excel లేదా CSV ఫైల్. ఫార్మాట్ కోసం **Download Sample Template** నొక్కండి. |
| **Request Letter for Dealership** | కొత్త డీలర్లకు మాత్రమే. సంస్థ లెటర్‌హెడ్‌పై, ముద్ర, సంతకంతో. |

## సాధారణ సమస్యలు

- **"You still have ... required document(s) pending."** (ఇంకా కొన్ని తప్పనిసరి డాక్యుమెంట్లు పెండింగ్‌లో ఉన్నాయి) **Required**, **Missing** అని గుర్తు ఉన్న ప్రతి డాక్యుమెంట్‌ను అప్‌లోడ్ చేయండి.
- **"No partners found. Add partners in Proprietor step first."** (భాగస్వాములు లేరు. ముందు Proprietor దశలో జోడించండి) దశ 7 కి తిరిగి వెళ్లి యజమానులు లేదా భాగస్వాములను సేవ్ చేయండి.
:::

::: section final-submission
title: దరఖాస్తును చెక్ చేసి సబ్మిట్ చేయడం (దశ 13)
routes: /FinalSubmission
for: 
roles: 
designations: 
screenshot: final-submission.png
hotspots: 1 @ 2.2,21.9,95.5,13 | 2 @ 2.2,37.8,95.5,31.3 | 3 @ 2.2,72,95.5,28 | 4 @ - | 5 @ -
screenshot-phone: final-submission.phone.jpg
hotspots-phone: 1 @ 3.1,13.3,93.8,7.5 | 2 @ 3.1,22.5,93.8,38.9 | 3 @ 3.1,63.1,93.8,36.9 | 4 @ - | 5 @ -
tour: 1 > [data-help=dr-fs-progress] | 2 > [data-help=dr-fs-sections] | 3 > [data-help=dr-fs-enclosures] | 4 > [data-help=dr-fs-terms] | 5 > [data-help=dr-fs-result]
minutes: 3
source-hash: ec4c07bdc1524795
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Final Submission** స్క్రీనే చివరి దశ. ఏది పూర్తయింది, ఏది మిగిలి ఉంది అని చూపించి, దరఖాస్తును ఆమోదానికి పంపుతుంది.

1. పైన ఉన్న ప్రోగ్రెస్ కార్డు చూడండి. ఎన్ని సెక్షన్లు పూర్తయ్యాయో అది చూపిస్తుంది. 100% అయినప్పుడు **Application Ready for Submission** అని చూపిస్తుంది.
2. **Section Completion Status** కింద, పచ్చ టిక్ అంటే ఆ సెక్షన్ పూర్తయింది, ఆరెంజ్ గుర్తు అంటే ఏదో మిగిలి ఉంది. ఆ దశ తెరిచి సరిచేయడానికి పెన్సిల్ నొక్కండి.
3. **Enclosures** కార్డులో, **Documents Uploaded**, **Missing Documents** చెక్ చేయండి. మిగిలిపోయిన డాక్యుమెంట్‌కు ఆరెంజ్ రంగు అడ్డ గుర్తు (X) ఉంటుంది.
4. **Terms and Conditions**, **I hereby declare** పాయింట్లు చదవండి. సబ్మిట్ చేసినప్పుడు, మీరు వాటిని అంగీకరిస్తారు.
5. పచ్చ రంగు **All sections completed!** బాక్స్ కనిపించినప్పుడు, **Submit Registration** నొక్కండి. **Registration submitted successfully!** అని కనిపించి, డీలర్ లిస్ట్‌కు తిరిగి వెళ్తారు. అక్కడ దరఖాస్తు **In RM** అని చూపిస్తుంది.

> Tip: పైన ఉన్న దశల బార్‌లో ఏ భాగాన్ని నొక్కినా నేరుగా ఆ దశకు వెళ్లవచ్చు.

## సాధారణ సమస్యలు

- **"Please complete all ... incomplete section(s) before submitting."** (సబ్మిట్ చేసే ముందు పూర్తి కాని సెక్షన్లు పూర్తి చేయండి) ముందు ఆరెంజ్ గుర్తు ఉన్న ప్రతి సెక్షన్‌ను సరిచేయండి.
- **"Error submitting registration"** (నమోదు సబ్మిట్ చేయడంలో లోపం) మెసేజ్ చదవండి. సర్వర్ ఏమి మిగిలి ఉందని గుర్తించిందో అది చెబుతుంది. దాన్ని సరిచేసి మళ్లీ సబ్మిట్ చేయండి.
- **Submit Registration బటన్ లేదు.** దరఖాస్తు చూడటానికి మాత్రమే తెరవబడింది. డీలర్ కార్డులోని **Edit** తో దాన్ని తెరవండి. దరఖాస్తు **Draft** లేదా **Rejected by RM** గా ఉన్నప్పుడు మాత్రమే **Edit** కనిపిస్తుంది.
:::

::: section application-pdf
title: డీలర్‌షిప్ దరఖాస్తు ఫారం చూడటం
routes: /DealershipPDF/{dealerId:int?}
for: 
roles: 
designations: 
minutes: 2
source-hash: 916c338c0a45b3ba
confidence: ok

**ఈ స్క్రీన్ దేనికి**
SPIC కోసం లేదా Greenstar కోసం, మొత్తం దరఖాస్తును ఒకే **DEALERSHIP APPLICATION FORM** గా ఈ స్క్రీన్ చూపిస్తుంది. దరఖాస్తు చదవడానికి లేదా ప్రింట్ కాపీ ఉంచుకోవడానికి దీన్ని వాడండి.

1. డీలర్ లిస్ట్‌లో, డీలర్ కార్డు కనుక్కుని **SPIC** లేదా **GFL** నొక్కండి.
2. పైన కంపెనీ పేరు, లోగోతో ఫారం తెరుచుకుంటుంది.
3. ప్రతి భాగం చదవడానికి కిందకు స్క్రోల్ చేయండి: **Dealer Details**, **Business Information**, **Bank Details**, **Proprietor / Partner Details**, **Sales Planning & Forecast**, **Investment Details** ఇంకా మరిన్ని.
4. చివరి దగ్గర, క్రెడిట్ లిమిట్, **Recommended Credit Limit**, **Attached Documents** చూడండి.
5. కాపీ ఉంచుకోవడానికి, మీ బ్రౌజర్ లేదా పరికరంలోని ప్రింట్ ఆప్షన్ వాడండి.

## సాధారణ సమస్యలు

- **పైన "Credit Limit Consent: GIVEN" అని ఉంది.** డీలర్ క్రెడిట్ లిమిట్ వివరాలు ఇవ్వలేదు, కాబట్టి క్రెడిట్ లిమిట్ 0.
- **GFL బటన్ లేదు.** డీలర్ SPIC కి మాత్రమే నమోదు అయ్యారు.
:::

::: section entry-info
title: లక్కీ డ్రా ఎంట్రీ వివరాలు
routes: /EntryInfo
for: 
roles: 
designations: 
screenshot: entry-info.png
screenshot-phone: entry-info.phone.jpg
minutes: 1
source-hash: 24f199e7d7d10b2f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
లక్కీ డ్రా కోసం ఒక కొనుగోలు ఎంట్రీని **Entry Information** స్క్రీన్ చూపిస్తుంది. ఈ స్క్రీన్ ఇంకా తయారవుతోంది: ఇది నమూనా విలువలు చూపిస్తుంది, నిజమైన ఎంట్రీలు కాదు.

1. పైన, ఎంట్రీ నంబర్, దాని స్టేటస్ చూడండి.
2. ఎంట్రీ కార్డులో, **Date**, **Dealer Name**, **Source**, **Location**, **Product**, **Purchase Amount** చూడండి.
3. **LUCKY DRAW INFO** కింద, ఎంట్రీ అర్హత ఉందో లేదో, ఇప్పటి రౌండ్ ఎంతవరకు వచ్చిందో చూడండి.
4. **Verification** కింద, **Invoice Number**, ఎంట్రీ లెక్కలోకి వచ్చిందో లేదో చూడండి.
5. తిరిగి వెళ్లడానికి పైన ఉన్న వెనక్కి బాణం నొక్కండి.

## సాధారణ సమస్యలు

- **విలువలు ఎప్పుడూ మారడం లేదు.** ప్రస్తుతానికి ఈ స్క్రీన్ నమూనా విలువలు మాత్రమే చూపిస్తుంది.
:::
