---
module: publicity
title: ప్రచార సామగ్రి ట్రాకింగ్
icon: bi-megaphone
order: 40
language: te
---

::: section overview
title: ప్రచార సామగ్రి ట్రాకింగ్ ఎలా పనిచేస్తుంది
routes: /PublicityRequest, /PublicityReview
for: 
roles: 
designations: 
source-hash: f087f488522bb49d
confidence: ok

**ఈ మాడ్యూల్ దేనికి**
మీ HQ కోసం ప్రచార సామగ్రి (పోస్టర్లు, బ్యానర్లు లాంటివి) అడగడానికి, దానికి ఆమోదం పొందడానికి, దాన్ని అందుకోవడానికి, అది చేరిందని నిర్ధారించడానికి ఈ మాడ్యూల్ వాడండి. ప్రతి దశ నమోదు అవుతుంది. కాబట్టి రిక్వెస్ట్ ఎక్కడ ఉందో అందరూ చూడగలరు.

## ఒక రిక్వెస్ట్ ప్రయాణం

1. RMDO రిక్వెస్ట్ నింపి **Submit Request** నొక్కుతారు.
2. రిక్వెస్ట్ ఈ వరుసలో ఆమోదానికి వెళ్తుంది: RM, తర్వాత SMD, తర్వాత SMM, తర్వాత ADMIN-MD CS, తర్వాత DGM-MD CS (చివరి ఆమోదం).
3. ఆమోదించేవారు ఎవరైనా (DGM-MD CS తప్ప) ముందుకు పంపే ముందు వివరాలు మార్చవచ్చు. ఆమోదించేవారు రిక్వెస్ట్‌ను **Reject** కూడా చేయవచ్చు. అప్పుడు అది మూసివేయబడుతుంది.
4. చివరి ఆమోదం తర్వాత, ADMIN-MD CS (లేదా DGM-MD CS) వెండర్ నుంచి డిస్పాచ్ తయారు చేస్తారు.
5. సామగ్రి ఇలా ప్రయాణిస్తుంది: **Dispatched**, తర్వాత **In Transit**, తర్వాత **Delivered**.
6. RMDO సామగ్రిని చెక్ చేసి, ఎన్ని అందాయో, అవి ఎలా ఉన్నాయో రాసి **Acknowledgement** సబ్మిట్ చేస్తారు.
7. అంతా సరిగ్గా ఉంటే, రిక్వెస్ట్ **Completed** అవుతుంది.
8. కొన్ని వస్తువులు తక్కువగా వచ్చినా లేదా పాడైనా, **Exception Raised** స్టేటస్ కనిపిస్తుంది. ADMIN-MD CS దాన్ని పరిష్కరిస్తారు: వెండర్ నుంచి కొత్త డిస్పాచ్ పంపి, లేదా వేరే HQ దగ్గర ఉన్న ఎక్కువ స్టాక్‌ను తరలించి.

## ఎవరు ఏమి చేస్తారు

- **RMDO**: రిక్వెస్ట్‌లు తయారు చేస్తారు, డెలివరీ అందినట్టు నిర్ధారిస్తారు, ఎక్కువ స్టాక్ పంపమని అడిగినప్పుడు కొరియర్ రుజువు పంపుతారు.
- **RM, SMD, SMM**: తమ రీజియన్ లేదా రాష్ట్రం రిక్వెస్ట్‌లను చెక్ చేసి ఆమోదిస్తారు.
- **ADMIN-MD CS**: ఆమోదిస్తారు, డిస్పాచ్‌లు తయారు చేస్తారు, డెలివరీ అప్‌డేట్ చేస్తారు, ఎక్సెప్షన్‌లను పరిష్కరిస్తారు.
- **DGM-MD CS**: చివరి ఆమోదం ఇస్తారు, డిస్పాచ్‌లు కూడా తయారు చేయవచ్చు.

> కొత్త ఆమోదాలు, డిస్పాచ్‌లు, ట్రాన్స్‌ఫర్ ఆర్డర్‌లు చూడటానికి లిస్ట్ పేజీలో గంట బటన్ **Notifications** నొక్కండి.
:::

::: section requester-list
title: నా రిక్వెస్ట్‌లు చూడటం, నిర్వహించడం
routes: /PublicityRequest
for: 
roles: 
designations: 
source-hash: f32b121b5900212e
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**My Publicity Material Request** స్క్రీన్ మీ HQ యొక్క ప్రతి రిక్వెస్ట్‌ను చూపిస్తుంది. పైన లెక్కలు, కింద టేబుల్ ఉంటాయి.

## దశలు

1. పైన ఉన్న కార్డులు చూడండి: **Total Request**, **Draft**, **Pending Approval**, **Final Approved**, **In Dispatch**, **Acknowledgement Pending**, **Completed**, **Exception**.
2. ఒక కార్డు నొక్కితే ఆ స్టేటస్‌లో ఉన్న రిక్వెస్ట్‌లు మాత్రమే కనిపిస్తాయి. మళ్లీ నొక్కితే అన్నీ కనిపిస్తాయి.
3. ఇంకా వివరంగా వెతకాలంటే, **Filters** తెరిచి, **Date Range**, **Product**, **Publicity Material** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి. ఫిల్టర్‌లు తీసేయడానికి **Clear All** నొక్కండి.
4. రిక్వెస్ట్‌ను త్వరగా కనుక్కోవడానికి సెర్చ్ బాక్స్‌లో ఇండెంట్ నంబర్, ప్రొడక్ట్ లేదా సామగ్రి పేరు టైప్ చేయండి.
5. **Action** కాలమ్‌లో, రిక్వెస్ట్ తెరవడానికి కన్ను ఐకాన్ నొక్కండి.
6. **Draft** లేదా **Returned** రిక్వెస్ట్‌ను మార్చడానికి పెన్సిల్ ఐకాన్ నొక్కండి.
7. **Draft** ను మాత్రమే డిలీట్ చేయవచ్చు: బిన్ (చెత్తబుట్ట) ఐకాన్ నొక్కండి. డిలీట్ చేసింది మళ్లీ తిరిగి రాదు.
8. లిస్ట్‌ను Excel ఫైల్‌గా డౌన్‌లోడ్ చేయడానికి **Export** నొక్కండి.
9. కొత్త రిక్వెస్ట్ మొదలుపెట్టడానికి **Create New Request** నొక్కండి.

## సాధారణ సమస్యలు

- **"No requests found" అని కనిపిస్తోంది.** మీరు పెట్టిన ఫిల్టర్‌లు మరీ ఎక్కువగా ఉండొచ్చు. **Clear All** నొక్కండి.
- **పెన్సిల్ ఐకాన్ లేదు.** సబ్మిట్ చేశాక, ఆమోదించేవారు తిరిగి పంపితే తప్ప రిక్వెస్ట్‌ను మార్చలేరు.
- **Export బటన్ బూడిద రంగులో ఉంది.** ఎక్స్‌పోర్ట్ చేయడానికి లిస్ట్‌లో వరుసలు ఏవీ లేవు.
:::

::: section raise-request
title: ప్రచార సామగ్రి రిక్వెస్ట్ పెట్టడం
routes: /PublicityRequest/new, /PublicityRequest/edit/{id}
for: 
roles: 
designations: 
source-hash: f4c944427bc4be47
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ HQ కోసం ప్రచార సామగ్రి అడగడానికి **Create Publicity Material Request** స్క్రీన్ వాడండి. దాన్ని డ్రాఫ్ట్‌గా సేవ్ చేసి తర్వాత పంపవచ్చు.

## దశలు

1. లిస్ట్ పేజీలో **Create New Request** నొక్కండి.
2. **Request Details** కింద, **Type of Publicity Material**, **Product** ఎంచుకోండి.
3. మీకు కావాల్సిన **Quantity** ఎంటర్ చేయండి.
4. **Requested By** చెక్ చేయండి. అందులో మీ పేరు ముందే ఉంటుంది.
5. మీ దగ్గర డిజైన్ ఉంటే, ఫైల్‌ను అప్‌లోడ్ బాక్స్‌లో వేయండి లేదా **or click to browse** నొక్కండి.
6. అవసరమైతే **Remarks** రాయండి.
7. **Location** కింద, **State**, **Region**, **Headquarter (HQ)** చెక్ చేయండి. ఇవి మీ ప్రొఫైల్ నుంచి నిండి ఉంటాయి.
8. తర్వాత కోసం ఉంచాలంటే **Save as Draft** నొక్కండి, లేదా RM కి పంపడానికి **Submit Request** నొక్కండి.
9. నిర్ధారణ బాక్స్‌లో **Submit** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Type of Publicity Material**: మీకు కావాల్సిన వస్తువు, ఉదాహరణకు పోస్టర్ లేదా బ్యానర్. తప్పనిసరి.
- **Product**: ఈ సామగ్రి ఏ ప్రొడక్ట్ కోసమో అది. టైప్ చేసి వెతకవచ్చు. తప్పనిసరి.
- **Quantity**: కావాల్సిన ముక్కల సంఖ్య. తప్పనిసరి.
- **Requested By**: సామగ్రి అడుగుతున్న వ్యక్తి.
- **Design file**: ఒకే ఫైల్, PDF, JPG, PNG, DOC లేదా DOCX, 10 MB వరకు.
- **State**, **Region**, **Headquarter (HQ)**: సామగ్రి ఎక్కడ డెలివరీ కావాలో అది. తప్పనిసరి.

## సాధారణ సమస్యలు

- **"Select the state, region and headquarter."** (రాష్ట్రం, రీజియన్, హెడ్‌క్వార్టర్ ఎంచుకోండి) ముందు State, తర్వాత Region, తర్వాత HQ ఎంచుకోండి.
- **"is larger than 10 MB"** (ఫైల్ 10 MB కంటే పెద్దది) ఫైల్ సైజు తగ్గించండి లేదా కొత్త ఫోటో తీయండి, తర్వాత మళ్లీ అప్‌లోడ్ చేయండి.
- **సబ్మిట్ చేశాక మార్చలేకపోతున్నాను.** ఇది మామూలే. తిరిగి పంపిన (returned) రిక్వెస్ట్‌ను మాత్రమే మళ్లీ మార్చవచ్చు.
:::

::: section track-request
title: నా రిక్వెస్ట్‌ను ట్రాక్ చేయడం
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
source-hash: 2d926f137adb7d61
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**My Publicity Material Request Details** స్క్రీన్ ఒక రిక్వెస్ట్ గురించి అన్నీ చూపిస్తుంది: అది ఇప్పుడు ఎక్కడ ఉంది, ఎవరు ఆమోదించారు, డిస్పాచ్, డాక్యుమెంట్లు.

## దశలు

1. లిస్ట్‌లో కన్ను ఐకాన్‌తో రిక్వెస్ట్ తెరవండి.
2. పైన **Current Stage**, **Status**, ఇప్పుడు రిక్వెస్ట్ ఎవరి దగ్గర ఉందో (**Pending With**) చూడండి.
3. **Request Details & History** లో, **Request Timeline**, ప్రతి చర్య, రిమార్క్‌తో ఉన్న **Approval History** టేబుల్ చూడండి.
4. ప్రతి డిస్పాచ్ చూడటానికి **Tracking & Delivery** తెరవండి: వెండర్, ట్రాన్స్‌పోర్టర్, ట్రాకింగ్ నంబర్, డెలివరీ స్టేటస్.
5. సామగ్రి వచ్చినప్పుడు, అందినట్టు నిర్ధారించడానికి **Acknowledgement** తెరవండి.
6. అప్‌లోడ్ చేసిన అన్ని ఫైల్‌లు చూడటానికి **Related Documents** తెరవండి.
7. రిక్వెస్ట్ PDF కోసం **Download Report** నొక్కండి, లేదా ప్రింట్ చేయడానికి **Print** నొక్కండి.
8. రిక్వెస్ట్ **Returned** అయితే, **Edit & Resubmit** నొక్కి, సరిచేసి మళ్లీ సబ్మిట్ చేయండి.

## సాధారణ సమస్యలు

- **Tracking & Delivery లో "Not dispatched yet" అని ఉంది.** సామగ్రి ఇంకా ఆమోదం దశలో ఉంది, లేదా డిస్పాచ్ ఇంకా తయారు కాలేదు. **Current Stage** చెక్ చేయండి.
- **స్టేటస్ "Rejected" అని చూపిస్తోంది.** రిక్వెస్ట్ మూసివేయబడింది. **Approval History** లో కారణం చదవండి. అవసరమైతే కొత్త రిక్వెస్ట్ పెట్టండి.
- **Edit & Resubmit కనిపించడం లేదు.** రిక్వెస్ట్ మీకు తిరిగి వచ్చినప్పుడు మాత్రమే అది కనిపిస్తుంది.
:::

::: section acknowledge-delivery
title: అందిన సామగ్రిని నిర్ధారించడం
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
source-hash: 3642d2f43fd54ffa
confidence: ok

**ఈ స్క్రీన్ దేనికి**
సామగ్రి మీ HQ కి చేరినప్పుడు, ఎన్ని ముక్కలు అందాయో, అవి ఎలా ఉన్నాయో మీరు నిర్ధారించాలి. దీనితో రిక్వెస్ట్ మూసివేయబడుతుంది, లేదా సమస్య ఉంటే ADMIN-MD CS కి తెలుస్తుంది.

## దశలు

1. రిక్వెస్ట్ తెరిచి **Tracking & Delivery** కి వెళ్లండి.
2. **Awaiting Acknowledgement** బాక్స్‌లో **Acknowledge** నొక్కండి. ఇది **Acknowledgement** ట్యాబ్ తెరుస్తుంది.
3. **Dispatch Summary** (డిస్పాచ్ చేసిన సంఖ్య, తేదీ) చెక్ చేయండి.
4. **Quantity Received** ఎంటర్ చేయండి.
5. **In Good Condition** లేదా **In Damaged Condition** ఎంచుకోండి. పాడైతే, **Damaged Quantity** ఎంటర్ చేయండి.
6. సామగ్రిని ఎవరు చెక్ చేశారో **Acknowledged By** లో ఎంచుకోండి.
7. **Remarks** రాసి, సామగ్రి ఫోటోలు అప్‌లోడ్ చేయండి.
8. **Exception Details** బాక్స్ కనిపిస్తే చెక్ చేయండి. తక్కువ వచ్చినా, ఎక్కువ వచ్చినా, పాడైనా అది చూపిస్తుంది.
9. తర్వాత పూర్తి చేయడానికి **Save as Draft** నొక్కండి, లేదా **Submit Acknowledgement** నొక్కి, తర్వాత **Submit** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Quantity Received**: నిజంగా లెక్కపెట్టిన ముక్కలు. తప్పనిసరి.
- **Material Condition**: బాగుంది లేదా పాడైంది. తప్పనిసరి.
- **Damaged Quantity**: 1 నుంచి అందిన సంఖ్య వరకు.
- **Acknowledged By**: సామగ్రిని అందుకున్న వ్యక్తి. తప్పనిసరి.
- **Photos**: అందిన సామగ్రి ఫోటోలు, ఒక్కొక్కటి 10 MB వరకు.

## సాధారణ సమస్యలు

- **"Damaged quantity must be between 1 and the received quantity."** (పాడైన సంఖ్య 1 నుంచి అందిన సంఖ్య మధ్యలో ఉండాలి) నంబర్లు మళ్లీ చెక్ చేయండి.
- **తక్కువ వచ్చాయి లేదా పాడైనవి ఉన్నాయి.** అయినా సబ్మిట్ చేయండి. ఒక ఎక్సెప్షన్ ADMIN-MD CS కి వెళ్తుంది. వారు తక్కువైన ముక్కలు పంపుతారు.
- **Acknowledge బటన్ లేదు.** సామగ్రి ఇంకా డిస్పాచ్ కాలేదు, లేదా అందినట్టు ఇప్పటికే నిర్ధారించారు.
:::

::: section transfer-proof
title: ఎక్కువ స్టాక్‌ను వేరే HQ కి పంపడం
routes: /PublicityRequest/{id}/transfer/{dispatchId}, /PublicityRequest/done
for: 
roles: 
designations: 
source-hash: 5b649ad478721aa8
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ HQ కి అవసరమైన దానికంటే ఎక్కువ సామగ్రి వస్తే, అందులో కొంత భాగాన్ని తక్కువ ఉన్న వేరే HQ కి పంపమని ADMIN-MD CS మిమ్మల్ని అడగవచ్చు. మీరు దాన్ని కొరియర్‌లో పంపి, కొరియర్ రసీదును ఇక్కడ అప్‌లోడ్ చేస్తారు.

## దశలు

1. **Notifications** తెరిచి స్టాక్ ట్రాన్స్‌ఫర్ ఆర్డర్ నొక్కండి, తర్వాత **View Transfer** నొక్కండి. లేదా మీ రిక్వెస్ట్ తెరిచి, **Tracking & Delivery** కి వెళ్లి, **Stock Transfer Order** కార్డులో **Create Transfer** నొక్కవచ్చు.
2. **Read Information (Read Only)** భాగం చెక్ చేయండి: **Destination HQ**, **Transfer Quantity**.
3. సామగ్రిని ప్యాక్ చేసి కొరియర్‌లో పంపండి.
4. **Upload Courier / Dispatch Proof** కింద, కొరియర్ రసీదు యొక్క స్పష్టమైన ఫోటో అప్‌లోడ్ చేయండి.
5. యాప్ రసీదును చదివి **Courier / Transporter**, **Tracking No. / AWB**, **Dispatch Date** నింపుతుంది. వాటిని చెక్ చేసి, అవసరమైతే సరిచేయండి.
6. మీ దగ్గర ఉంటే **Vehicle No**, **Remarks (Optional)** జోడించండి.
7. **Submit Proof to Admin** నొక్కండి.
8. **Dispatch Proof Submitted Successfully!** స్క్రీన్ తెరుచుకుంటుంది. **Go to Tracking & Delivery** లేదా **Back to List** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Courier / Transporter**: కొరియర్ కంపెనీ పేరు. తప్పనిసరి.
- **Tracking No. / AWB**: కొరియర్ రసీదుపై ఉన్న నంబర్. తప్పనిసరి.
- **Dispatch Date**: మీరు పంపిన తేదీ. తప్పనిసరి.

## సాధారణ సమస్యలు

- **"Upload the courier / dispatch proof."** (కొరియర్ / డిస్పాచ్ రుజువు అప్‌లోడ్ చేయండి) సబ్మిట్ చేసే ముందు రసీదు అప్‌లోడ్ చేయండి.
- **వివరాలు వాటంతట అవే నిండలేదు.** ఫోటో మసకగా ఉండొచ్చు, లేదా అది PDF అయి ఉండొచ్చు. వివరాలు మీరే టైప్ చేయండి.
- **"Transfer order not found" అని వస్తోంది.** రుజువు ఇప్పటికే సబ్మిట్ అయ్యింది. మార్చాలంటే, **Tracking & Delivery** ట్యాబ్‌లో **Update Proof** వాడండి.
:::

::: section requester-notifications
title: నా ప్రచార సామగ్రి నోటిఫికేషన్‌లు
routes: /PublicityRequest/notifications
for: 
roles: 
designations: 
source-hash: 3f380e63b29f62fe
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Notifications** స్క్రీన్ మీ రిక్వెస్ట్‌ల గురించి అలర్ట్‌లు చూపిస్తుంది: తిరిగి పంపినవి, డిస్పాచ్‌లు, ఎక్సెప్షన్‌లు, స్టాక్ ట్రాన్స్‌ఫర్ ఆర్డర్‌లు.

## దశలు

1. లిస్ట్ పేజీలో **Notifications** నొక్కండి.
2. ఒక ట్యాబ్ ఎంచుకోండి: **All**, **Stock Transfer**, **Exception** లేదా **Dispatch & Approvals**.
3. చదవని వాటికి ఒక చుక్క ఉంటుంది. వివరాలు తెరవడానికి దాన్ని నొక్కండి.
4. ట్రాన్స్‌ఫర్ ఆర్డర్ అయితే, **Transfer Order Details**, **Source Excess Reference**, **Reason for Transfer** చూడండి.
5. కొరియర్ రుజువు అప్‌లోడ్ చేయడానికి **View Transfer** నొక్కండి, లేదా రిక్వెస్ట్ తెరవడానికి **View Details** నొక్కండి.
6. ఒక దానికి **Mark As Read** నొక్కండి, లేదా అన్నింటికీ **Mark All As Read** నొక్కండి.
7. వెనక్కి వెళ్లడానికి **Back to List Page** నొక్కండి.

## సాధారణ సమస్యలు

- **"No notifications".** (నోటిఫికేషన్‌లు లేవు) మీ రిక్వెస్ట్‌లపై ఇంకా కొత్తగా ఏమీ జరగలేదు.
- **Mark All As Read బూడిద రంగులో ఉంది.** అన్నీ ఇప్పటికే చదివారు.
:::

::: section reviewer-list
title: నా రివ్యూకి వచ్చిన రిక్వెస్ట్‌లు చూడటం
routes: /PublicityReview
for: 
roles: 
designations: 
source-hash: 7ebd8194aa3f5ebd
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఆమోదించేవారు, MD CS సిబ్బంది తమ ప్రాంతం నుంచి వచ్చిన రిక్వెస్ట్‌లు చూడటానికి ఈ లిస్ట్ వాడతారు. దీని టైటిల్ **Submitted Requests** (RM కి ఇది **My Publicity Material Request**).

## దశలు

1. పైన ఉన్న కార్డులు చూడండి. RM కి **Pending for My Approval**, **Approved by RM**, **Dispatch & Delivery**, **Completed**, **Returned** కనిపిస్తాయి. మిగతా వారికి **Total Submitted**, **Pending Review**, **Dispatch Pending**, **Exceptions**, **Completed** లాంటి కార్డులు కనిపిస్తాయి.
2. ఎరుపు రంగు **New Request for Review** బాక్స్ కనిపిస్తే, **View New Requests** నొక్కండి. RM, SMD, SMM, ADMIN-MD CS కి ఇది కన్సాలిడేషన్ స్క్రీన్ తెరుస్తుంది.
3. **Filters** తెరిచి **Date Range**, **Product**, **Publicity Material**, **State**, **Region**, **HQ**, **Current Stage** లేదా **Status** ఎంచుకుని, **Apply Filters** నొక్కండి.
4. మీ కోసం ఎదురుచూస్తున్న వరుసలకు **Status** కాలమ్‌లో **Pending** అని ఉంటుంది. మీరు ఇప్పటికే ముందుకు పంపిన వరుసలకు **Approved** అని ఉంటుంది.
5. రిక్వెస్ట్ తెరవడానికి కన్ను ఐకాన్ నొక్కండి.
6. లిస్ట్‌ను Excel గా డౌన్‌లోడ్ చేయడానికి **Export** నొక్కండి.
7. మీ స్థాయిలో ఎదురుచూస్తున్న అన్ని రిక్వెస్ట్‌లను రీజియన్ వారీగా చూడటానికి **Consolidation** నొక్కండి.

## సాధారణ సమస్యలు

- **State లేదా Region ఫిల్టర్‌లు కనిపించడం లేదు.** RM, SMD, SMM తమ సొంత ప్రాంతం మాత్రమే చూస్తారు. అందుకే కొన్ని ఫిల్టర్‌లు దాచి ఉంటాయి.
- **లిస్ట్ ఖాళీగా ఉంది.** Filters లో **Clear All** నొక్కండి. ఇంకా ఏ రిక్వెస్ట్ మీ దగ్గరికి రాకపోయి ఉండొచ్చు.
:::

::: section review-approve
title: రిక్వెస్ట్‌ను రివ్యూ చేసి ఆమోదించడం
routes: /PublicityReview/{id}
for: 
roles: 
designations: 
source-hash: c7fff9b7b3926213
confidence: ok

**ఈ స్క్రీన్ దేనికి**
రిక్వెస్ట్‌ను చెక్ చేసి ఆమోదించడానికి, మార్చడానికి లేదా తిరస్కరించడానికి దాన్ని తెరవండి. రిక్వెస్ట్ మీ కోసం ఎదురుచూస్తున్నప్పుడు మాత్రమే బటన్లు కనిపిస్తాయి.

## దశలు

1. లిస్ట్ నుంచి లేదా కన్సాలిడేషన్ స్క్రీన్ నుంచి రిక్వెస్ట్ తెరవండి.
2. పైన ఉన్న పట్టీ, **Request Timeline**, **Approval History** చెక్ చేయండి.
3. ఏదైనా నోట్ ఉంటే **Additional Remarks (if any)** లో టైప్ చేయండి.
4. ముందుగా సామగ్రి, సంఖ్య లేదా లొకేషన్ మార్చాలంటే, **Edit** నొక్కండి.
5. ఆమోదించడానికి, తర్వాతి స్థాయికి **Approve & Forward to** నొక్కండి (ఉదాహరణకు **Approve & Forward to SMD**), తర్వాత **Approve & Forward** తో నిర్ధారించండి.
6. తిరస్కరించడానికి, **Reject** నొక్కి, **Reason for rejection** రాసి, మళ్లీ **Reject** నొక్కండి.

## ఆమోదం వరుస

- RM నుంచి SMD కి, SMD నుంచి SMM కి, SMM నుంచి ADMIN - MD CS కి, ADMIN-MD CS నుంచి DGM - MD CS కి ముందుకు వెళ్తుంది.
- DGM-MD CS చివరి ఆమోదం ఇస్తారు. ఈ దశలో **Edit** ఉండదు.

## సాధారణ సమస్యలు

- **Approve బటన్ లేదు, టైటిల్ "Request Details" అని ఉంది.** రిక్వెస్ట్ ఇప్పుడు మీ స్థాయిలో లేదు. మీరు చూడటం, **Print** లేదా **Download Report** మాత్రమే చేయగలరు.
- **"Enter the reason for rejection."** (తిరస్కరణకు కారణం ఎంటర్ చేయండి) కారణం తప్పనిసరి.
- **Reject చేస్తే రిక్వెస్ట్ శాశ్వతంగా మూసివేయబడుతుంది.** దాన్ని వెనక్కి తీసుకోలేరు. RMDO కొత్త రిక్వెస్ట్ పెట్టాలి.
:::

::: section reviewer-edit
title: ముందుకు పంపే ముందు రిక్వెస్ట్‌ను మార్చడం
routes: /PublicityReview/edit/{id}
for: 
roles: 
designations: 
source-hash: a0de34efb439462f
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Edit Publicity Material Request** స్క్రీన్‌లో, ఎవరి వంతు ఉందో ఆ ఆమోదించేవారు రిక్వెస్ట్‌ను సరిచేయవచ్చు. మార్పు సేవ్ చేస్తే, రిక్వెస్ట్ ఆమోదించబడి ముందుకు కూడా వెళ్తుంది.

## దశలు

1. రిక్వెస్ట్‌లో **Edit** నొక్కండి. కన్సాలిడేషన్ స్క్రీన్‌లో అయితే, పెన్సిల్ ఐకాన్ నొక్కండి.
2. **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** లేదా **Remarks** మార్చండి.
3. అవసరమైతే డిజైన్ ఫైల్ జోడించండి లేదా తీసేయండి.
4. **State**, **Region**, **Headquarter (HQ)** చెక్ చేయండి.
5. తర్వాతి స్థాయికి **Save & Forward to** నొక్కండి (ఉదాహరణకు **Save & Forward to SMM**).
6. మార్పు సేవ్ అవుతుంది, **Approval History** లో నమోదవుతుంది, రిక్వెస్ట్ తర్వాతి స్థాయికి వెళ్తుంది.

## ఫీల్డ్‌ల వివరాలు

- **Quantity**: ఆమోదించిన ముక్కల సంఖ్య. తప్పనిసరి.
- **Type of Publicity Material**, **Product**, **Headquarter (HQ)**: తప్పనిసరి.

## సాధారణ సమస్యలు

- **ఎడిట్ పేజీకి బదులు వివరాల పేజీ తెరుచుకుంటోంది.** రిక్వెస్ట్ ఇప్పుడు మీ కోసం ఎదురుచూడటం లేదు.
- **నేను సేవ్ మాత్రమే చేయాలనుకున్నాను, ముందుకు పంపాలని కాదు.** ఇక్కడ సేవ్ చేస్తే ఎప్పుడూ ముందుకు వెళ్తుంది. ఇప్పుడే ముందుకు పంపకూడదనుకుంటే **Back to List** వాడండి.
:::

::: section consolidation
title: చాలా రిక్వెస్ట్‌లను కలిపి ఆమోదించడం
routes: /PublicityReview/consolidation
for: 
roles: 
designations: 
source-hash: 92970d521f5da24b
confidence: ok

**ఈ స్క్రీన్ దేనికి**
కన్సాలిడేషన్ స్క్రీన్ మీ స్థాయిలో ఎదురుచూస్తున్న ప్రతి రిక్వెస్ట్‌ను రీజియన్ వారీగా చూపిస్తుంది. దీనితో అన్నింటినీ ఒకేసారి ఆమోదించవచ్చు. RM కి రీజియన్, SMD, SMM కి రాష్ట్రం, ADMIN-MD CS కి మొత్తం భారతదేశం కనిపిస్తాయి. DGM-MD CS ఈ స్క్రీన్ వాడరు.

## దశలు

1. లిస్ట్ పేజీలో **Consolidation** నొక్కండి.
2. కార్డులు చెక్ చేయండి: **Submission ID**, **Region** లేదా **State**, **Total Region**, **Total Requests**, **Total Requirement**.
3. టేబుల్‌లో, ఒక రీజియన్ తెరిచి దాని HQ రిక్వెస్ట్‌లు చూడటానికి **Action** లో బాణం గుర్తు నొక్కండి.
4. రిక్వెస్ట్ చూడటానికి కన్ను ఐకాన్ నొక్కండి, లేదా మార్చడానికి పెన్సిల్ ఐకాన్ నొక్కండి.
5. కింద ఉన్న బాక్స్ చదవండి. ఎన్ని రీజియన్‌లు మీ దగ్గరికి వచ్చాయో అది చూపిస్తుంది.
6. అన్ని రీజియన్‌లు మీ దగ్గరికి వచ్చాక, తర్వాతి స్థాయికి **Approve & Submit to** నొక్కి, **Approve & Submit** తో నిర్ధారించండి.
7. టేబుల్ డౌన్‌లోడ్ చేయడానికి **Export** నొక్కండి, లేదా లిస్ట్‌కి తిరిగి వెళ్లడానికి **View Submitted Request** నొక్కండి.

## సాధారణ సమస్యలు

- **Approve & Submit బూడిద రంగులో ఉంది.** కొన్ని రీజియన్‌లు ఇంకా ముందు స్థాయి దగ్గర పెండింగ్‌లో ఉన్నాయి. అన్ని రీజియన్‌లు మీ దగ్గరికి వచ్చే వరకు ఆగండి.
- **"Nothing waiting for you" అని వస్తోంది.** ఇప్పుడు మీ స్థాయిలో ఏ రిక్వెస్ట్ లేదు.
- **Request Qty, Approved Qty వేరువేరుగా ఉన్నాయి.** ఒక ఆమోదించేవారు సంఖ్య మార్చారు. రిక్వెస్ట్‌లో **Approval History** చూడండి.
:::

::: section create-dispatch
title: డిస్పాచ్ తయారు చేయడం
routes: /PublicityReview/{id}/dispatch, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
source-hash: afa4604b42d3cf80
confidence: ok

**ఈ స్క్రీన్ దేనికి**
చివరి ఆమోదం తర్వాత, ADMIN-MD CS లేదా DGM-MD CS వెండర్ నుంచి వెళ్లే డిస్పాచ్‌ను **Create Dispatch** స్క్రీన్‌లో నమోదు చేస్తారు. రీప్లేస్‌మెంట్ (re-initiation, మళ్లీ పంపే) డిస్పాచ్‌కి కూడా ఇదే స్క్రీన్ వాడతారు.

## దశలు

1. **Final Approved** రిక్వెస్ట్ తెరిచి **Create Dispatch** నొక్కండి.
2. **Read Information (Read Only)** భాగం చెక్ చేయండి.
3. **Feed Logistics & Dispatch Data** కింద, **Dispatch Date**, **Dispatch Quantity**, **Vendor**, **Transporter / Courier** నింపండి.
4. తెలిస్తే **Vehicle No**, **Tracking No.**, **Expected Delivery Date** జోడించండి.
5. **Responsible SMDO / RM / RMDO / MO** ఎంచుకోండి.
6. **Dispatch / Note Approval Document** అప్‌లోడ్ చేయండి. మీ దగ్గర ఉంటే ట్రాన్స్‌పోర్ట్ స్లిప్ లేదా E-way bill కూడా జోడించండి.
7. **Delivery Address** ఎంచుకోండి లేదా టైప్ చేయండి.
8. **Confirm and Create Dispatch** నొక్కండి.
9. **Dispatch Created Successfully!** స్క్రీన్ తెరుచుకుంటుంది. **Go to Tracking & Delivery** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Dispatch Quantity**: ఇప్పుడు పంపుతున్న ముక్కలు. రీప్లేస్‌మెంట్ అయితే, ఇంకా రావాల్సిన సంఖ్యతో మొదలవుతుంది.
- **Vendor**: లిస్ట్ నుంచి ఎంచుకోండి లేదా కొత్త పేరు టైప్ చేయండి. తప్పనిసరి.
- **Responsible SMDO / RM / RMDO / MO**: సామగ్రి అందుకునే అధికారి. తప్పనిసరి.
- **Dispatch / Note Approval Document**: మొదటి డిస్పాచ్‌కి తప్పనిసరి.
- **Delivery Address**: సేవ్ చేసిన అడ్రస్ నొక్కండి లేదా అడ్రస్ టైప్ చేయండి. తప్పనిసరి.

## సాధారణ సమస్యలు

- **"Upload the dispatch / note approval document."** (డిస్పాచ్ / నోట్ అప్రూవల్ డాక్యుమెంట్ అప్‌లోడ్ చేయండి) నిర్ధారించే ముందు దాన్ని అప్‌లోడ్ చేయండి.
- **Create Dispatch బటన్ లేదు.** రిక్వెస్ట్ ఇంకా **Final Approved** కాలేదు.
:::

::: section update-delivery
title: డిస్పాచ్, డెలివరీ స్టేటస్ అప్‌డేట్ చేయడం
routes: /PublicityReview/{id}
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
source-hash: 377dc5ed807e6b17
confidence: ok

**ఈ స్క్రీన్ దేనికి**
డిస్పాచ్ అయ్యాక, సామగ్రి ఎక్కడ ఉందో RMDO, ఆమోదించేవారు చూడగలిగేలా MD CS దాని పురోగతిని అప్‌డేట్ చేస్తారు.

## దశలు

1. రిక్వెస్ట్ తెరిచి **Tracking & Delivery** కి వెళ్లండి.
2. డిస్పాచ్ కార్డు కనుక్కోండి (**Dispatch 1**, **Dispatch 2** అలా).
3. సామగ్రి వెండర్ దగ్గర నుంచి బయలుదేరినప్పుడు, **Mark In Transit** నొక్కండి. **Expected Delivery Date**, **Current Location / Hub** జోడించి, **Save** నొక్కండి.
4. అది HQ కి చేరినప్పుడు, **Mark Delivered** నొక్కి, అవసరమైతే **Current Location / Hub** జోడించి, **Save** నొక్కండి.
5. స్టాక్ ట్రాన్స్‌ఫర్ అయితే, సోర్స్ RMDO కొరియర్ రుజువు అప్‌లోడ్ చేశాక, **Update Dispatch Details** నొక్కండి. **Transporter / Courier**, **Tracking No. (AWB)** చెక్ చేసి, **Save** నొక్కండి. అప్పుడు ట్రాన్స్‌ఫర్‌కి **Transfer Created** అని కనిపిస్తుంది.

## ఫీల్డ్‌ల వివరాలు

- **Transporter / Courier**, **Tracking No. (AWB)**: ట్రాన్స్‌ఫర్ నిర్ధారించేటప్పుడు తప్పనిసరి.
- **Expected Delivery Date**: సామగ్రి ఎప్పుడు రావాలో ఆ తేదీ.
- **Current Location / Hub**: సామగ్రి ఇప్పుడు ఎక్కడ ఉందో అది, ఉదాహరణకు కొరియర్ హబ్.

## సాధారణ సమస్యలు

- **డిస్పాచ్ కార్డులో బటన్లు లేవు.** డిస్పాచ్ అందినట్టు ఇప్పటికే నిర్ధారించారు, లేదా ట్రాన్స్‌ఫర్ రుజువు ఇంకా సోర్స్ RMDO దగ్గర పెండింగ్‌లో ఉంది ("Courier proof is pending from the source RMDO").
- **RMDO అందినట్టు నిర్ధారించలేకపోతున్నారు.** ముందు సామగ్రి డిస్పాచ్ కావాలి.
:::

::: section resolve-exception
title: తక్కువ లేదా పాడైన ఎక్సెప్షన్‌ను పరిష్కరించడం
routes: /PublicityReview/{id}, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
source-hash: 2d04fb4f99911b79
confidence: ok

**ఈ స్క్రీన్ దేనికి**
RMDO తక్కువ ముక్కలు లేదా పాడైన ముక్కలు వచ్చాయని చెప్పినప్పుడు, రిక్వెస్ట్‌కి **Exception Raised** అని కనిపిస్తుంది. తక్కువైన సంఖ్యను ఎలా పంపాలో మీరు నిర్ణయిస్తారు.

## దశలు

1. రిక్వెస్ట్ తెరిచి (లిస్ట్‌లోని **Exceptions** కార్డు వాటిని కనుక్కోవడానికి సహాయపడుతుంది) **Tracking & Delivery** కి వెళ్లండి.
2. ఎరుపు **Exception Raised** బాక్స్‌లో **Exception Type**, **Raised By**, **Remarks** చదవండి.
3. **Resolve Exception** నొక్కండి. ఇంకా రావాల్సిన సంఖ్యను బాక్స్ చూపిస్తుంది.
4. ఒక ఆప్షన్ ఎంచుకోండి:
5. **Re-initiate dispatch from the vendor** (వెండర్ నుంచి మళ్లీ డిస్పాచ్): **Continue to Dispatch** నొక్కి, రీప్లేస్‌మెంట్ కోసం డిస్పాచ్ ఫారం నింపండి.
6. **Stock transfer from another HQ's excess** (వేరే HQ దగ్గర ఉన్న ఎక్కువ స్టాక్ నుంచి ట్రాన్స్‌ఫర్): **Source Request (Excess)** ఎంచుకుని, **Transfer Quantity** చెక్ చేసి, **Remarks** జోడించి, **Send Transfer Order** నొక్కండి.
7. ట్రాన్స్‌ఫర్ అయితే, **Transfer Order Sent Successfully** స్క్రీన్ తెరుచుకుంటుంది. స్టాక్ పంపి కొరియర్ రుజువు అప్‌లోడ్ చేయమని సోర్స్ HQ RMDO కి తెలియజేయబడుతుంది.
8. రుజువు వచ్చాక, ట్రాన్స్‌ఫర్ కార్డులో **Update Dispatch Details** తో దాన్ని నిర్ధారించండి.

## సాధారణ సమస్యలు

- **స్టాక్ ట్రాన్స్‌ఫర్ ఆప్షన్ బూడిద రంగులో ఉంది ("no excess available").** వేరే ఏ HQ దగ్గరా ఎక్కువ స్టాక్ లేదు. వెండర్ ఆప్షన్ వాడండి.
- **"Select the source request and quantity."** (సోర్స్ రిక్వెస్ట్, సంఖ్య ఎంచుకోండి) ఒక సోర్స్ ఎంచుకుని, సున్నా కంటే ఎక్కువ సంఖ్య ఎంటర్ చేయండి.
- **Excess only.** (ఎక్కువ మాత్రమే వచ్చింది) RMDO కి ఎక్కువ ముక్కలు వస్తే, రిక్వెస్ట్ పూర్తవుతుంది. ఆ ఎక్కువ స్టాక్‌ను తర్వాత వేరే HQ కోసం వాడవచ్చు.
:::

::: section reviewer-notifications
title: ఆమోదించేవారు, MD CS నోటిఫికేషన్‌లు
routes: /PublicityReview/notifications
for: 
roles: 
designations: 
source-hash: d08258d028c354d8
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Notifications** స్క్రీన్ మీ ప్రాంతంలోని రిక్వెస్ట్‌ల గురించి అలర్ట్‌లు చూపిస్తుంది: కొత్త ఆమోదాలు, డిస్పాచ్‌లు, ఎక్సెప్షన్‌లు, స్టాక్ ట్రాన్స్‌ఫర్‌లు.

## దశలు

1. లిస్ట్ పేజీలో **Notifications** నొక్కండి.
2. ఒక ట్యాబ్ ఎంచుకోండి: **All**, **Stock Transfer**, **Exception** లేదా **Dispatch & Approvals**.
3. చదవని వాటికి ఒక చుక్క ఉంటుంది. చదవడానికి దాన్ని నొక్కండి.
4. రిక్వెస్ట్‌ను **Tracking & Delivery** ట్యాబ్‌లో తెరవడానికి **View Details** నొక్కండి.
5. ఒక దానికి **Mark As Read** నొక్కండి, లేదా అన్నింటికీ **Mark All As Read** నొక్కండి.
6. వెనక్కి వెళ్లడానికి **Back to List Page** నొక్కండి.

## సాధారణ సమస్యలు

- **"No notifications".** (నోటిఫికేషన్‌లు లేవు) ఇంకా కొత్తగా ఏమీ జరగలేదు.
- **ఎక్సెప్షన్ నోటిఫికేషన్ వచ్చింది.** రిక్వెస్ట్ తెరిచి **Resolve Exception** వాడండి (ADMIN-MD CS, DGM-MD CS మాత్రమే).
:::
