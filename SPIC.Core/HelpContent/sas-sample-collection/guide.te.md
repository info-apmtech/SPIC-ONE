---
module: sas-sample-collection
title: మట్టి శాంపిల్ సేకరణ
icon: bi-droplet
order: 50
language: te
---

::: section overview
title: మట్టి శాంపిల్ సేకరణ ఎలా పనిచేస్తుంది
routes: /SampleCollection
for: 
roles: 
designations: 
source-hash: 690edb1b53c94744
confidence: ok

**ఈ స్క్రీన్ దేనికి**
SPIC రైతు పొలంలోని మట్టిని, నీటిని జిల్లా ల్యాబ్‌లో పరీక్షిస్తుంది. పొలం నుంచి రైతు రిపోర్ట్ వరకు ప్రతి శాంపిల్‌ను యాప్ ఫాలో చేస్తుంది. ప్రతి సేకరణ ఇవే దశల గుండా వెళ్తుంది.

## దశలు

1. **Collect.** (సేకరణ) ఫీల్డ్ ఆఫీసర్ (MDO లేదా JMDO) **New Sample Collection** తెరిచి, రైతును ఎంచుకుని, ఒకటి లేదా ఎక్కువ శాంపిల్స్ జోడిస్తారు.
2. **Pay.** (చెల్లింపు) **Paid Sample** కి చెల్లింపు కావాలి. ఆఫీసర్ UPI లేదా ఇతర చెల్లింపును నమోదు చేసి, రసీదు ఫోటో అప్‌లోడ్ చేస్తారు. **Free Sample** కి ఈ దశ ఉండదు.
3. **Approve.** (ఆమోదం) ఆమోదించేవారు చెల్లింపును, రసీదును చెక్ చేస్తారు. తర్వాత డబ్బు అందిందో లేదో Finance చెక్ చేస్తుంది.
4. **ల్యాబ్‌కి పంపండి.** ఆఫీసర్ శాంపిల్స్‌ను కొరియర్‌లో పంపి, ట్రాకింగ్ నంబర్ ఎంటర్ చేస్తారు. దీన్ని కన్సైన్‌మెంట్ అంటారు.
5. **Test.** (పరీక్ష) ల్యాబ్ శాంపిల్స్ అందుకుని వాటిని పరీక్షిస్తుంది.
6. **Report.** (రిపోర్ట్) రైతు యాప్‌లో రిపోర్ట్ చూస్తారు, దాన్ని తమిళం లేదా ఇంగ్లీష్‌లో డౌన్‌లోడ్ చేసుకోవచ్చు.

## స్టేటస్ అర్థం

- **Draft**: సేవ్ అయ్యింది, ఇంకా సబ్మిట్ కాలేదు.
- **Pending Payment**: చెల్లింపు నమోదు కోసం ఎదురుచూస్తోంది.
- **Pending Approval**: చెల్లింపు నమోదైంది, ఆమోదం కోసం ఎదురుచూస్తోంది.
- **Ready consignment**: ల్యాబ్‌కి పంపడానికి సిద్ధంగా ఉంది.
- **Dispatched**, **In Transit**, **Delivered to Lab**: ల్యాబ్‌కి వెళ్తున్న దారిలో ఉంది.
- **In Progress**: ల్యాబ్ పరీక్షిస్తోంది.
- **Completed**: ఫలితాలు వచ్చాయి.

## సాధారణ సమస్యలు

- మెనూలో **Sample Collection** కనిపించడం లేదు: మీ హోదాకు (designation) ఈ పేజీ లేదు. మీ అడ్మినిస్ట్రేటర్‌ని అడగండి.
- JMDO వాళ్లకు ఇది మెనూలో కనిపించదు. బదులుగా మీ **Soil Sample** టాస్క్ నుంచి తెరవండి.
:::

::: section collection-list
title: మీ శాంపిల్ సేకరణలను కనుక్కోవడం, ట్రాక్ చేయడం
routes: /SampleCollection
for: 
roles: 
designations: 
source-hash: 53f4f23e337f18d1
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sample Collection** పేజీ మీరు చూడగలిగే ప్రతి సేకరణను, దాని చెల్లింపు, ల్యాబ్ స్టేటస్‌తో చూపిస్తుంది. ఒక సేకరణను కనుక్కుని, దాని వివరాలు తెరవడానికి దీన్ని వాడండి.

## దశలు

1. మెనూ నుంచి **Sample Collection** తెరవండి.
2. పైన ఉన్న కార్డులు లెక్కలు చూపిస్తాయి: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level**, **Ready to Submit**.
3. **Pending Payment** లేదా **Draft** లాంటి రంగు టైల్ నొక్కితే ఆ సేకరణలు మాత్రమే కనిపిస్తాయి. మళ్లీ నొక్కితే అన్నీ కనిపిస్తాయి.
4. వెతకడానికి, **Search collections...** లో రైతు పేరు లేదా సేకరణ ID టైప్ చేయండి.
5. ఇంకా ఆప్షన్‌ల కోసం, **Filters** తెరిచి, **Date Range**, **Location**, **Payment Type**, **Payment Status** లేదా **Status** సెట్ చేసి, **Apply Filters** నొక్కండి. వాటిని తీసేయడానికి **Clear All** నొక్కండి.
6. సేకరణ తెరవడానికి కన్ను ఐకాన్ నొక్కండి (ఫోన్‌లో అయితే కార్డు నొక్కండి).
7. స్క్రీన్‌పై కనిపిస్తున్న లిస్ట్ డౌన్‌లోడ్ చేయడానికి **Excel** లేదా **PDF** నొక్కండి.
8. తాజా స్టేటస్ లోడ్ చేయడానికి **Refresh Data** నొక్కండి.

## సాధారణ సమస్యలు

- **No sample collections found** (సేకరణలు ఏవీ దొరకలేదు): ఫిల్టర్‌లు తీసేయండి, లేదా తేదీల పరిధి చెక్ చేయండి.
- **Collections could not be loaded** (సేకరణలు లోడ్ కాలేదు): మీ ఇంటర్నెట్ చెక్ చేసి **Try again** నొక్కండి.
- **New Sample Collection** బటన్ కనిపించడం లేదు: మీ అకౌంట్‌తో సేకరణలు తయారు చేయలేరు. హోదా అనుమతించే MDO, JMDO, అడ్మినిస్ట్రేటర్ అకౌంట్‌లు మాత్రమే శాంపిల్స్ సేకరించగలవు.
- డౌన్‌లోడ్‌లో కొన్ని వరుసలు మాత్రమే ఉన్నాయి: **Excel**, **PDF** ఇప్పుడు ఉన్న పేజీని మాత్రమే సేవ్ చేస్తాయి. ఇంకా ఎక్కువ రావాలంటే **Items Per Page** మార్చండి.
:::

::: section collect-sample
title: కొత్త మట్టి లేదా నీటి శాంపిల్ సేకరించడం
routes: /SampleCollection/new
for: SampleCollection.Entry
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 4807aa811490f90c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
రైతుల దగ్గర మీరు తీసుకున్న శాంపిల్స్ నమోదు చేయడానికి **New Sample Collection** వాడండి. ఒక సేకరణలో చాలా మంది రైతుల శాంపిల్స్ ఉండవచ్చు.

## దశలు

1. **Sample Collection** లో **New Sample Collection** నొక్కండి. JMDO: **Tasks Allocation** లో **Soil Sample** కార్డుపై **Start** నొక్కండి, లేదా ఒక టాస్క్ యొక్క **Quick actions** లో **Soil Sample Collection** నొక్కండి.
2. పైన **Free Sample** లేదా **Paid Sample** ఎంచుకోండి.
3. **Farmer Name** లో కనీసం రెండు అక్షరాలు టైప్ చేసి రైతును ఎంచుకోండి. రైతు కొత్తవారైతే, **New Farmer** నొక్కండి.
4. పెయిడ్ శాంపిల్ అయితే, **Paid Sample Category** కింద **Farmer** లేదా **NGO** ఎంచుకోండి.
5. **Soil Sample**, **Water Sample** ను **Yes** లేదా **No** గా సెట్ చేయండి.
6. **Crop 1**, **Crop 2** ఎంచుకుని, అవసరమైతే **Remarks** రాయండి.
7. **Add Another Sample** నొక్కండి. శాంపిల్ దాని ధరతో **Sample Added (Preview)** లో కనిపిస్తుంది.
8. ప్రతి శాంపిల్‌కి ఇలాగే చేయండి. ఒక వరుస మార్చడానికి పెన్సిల్, తీసేయడానికి బిన్ (చెత్తబుట్ట) వాడండి.
9. తర్వాత పూర్తి చేయడానికి **Save Draft** నొక్కండి, లేదా **Sample Summary** చూడటానికి **Save and Continue** నొక్కండి.
10. సమ్మరీ చెక్ చేసి, **Continue to payment** (పెయిడ్) లేదా **Continue to consignment** (ఫ్రీ) నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Collection ID**: మీరు సేవ్ చేసినప్పుడు యాప్ తయారు చేస్తుంది.
- **Collected By**: మీ పేరు, ముందే నిండి ఉంటుంది.
- **Total Amount**: అన్ని పెయిడ్ శాంపిల్స్‌కి కలిపి ఛార్జీ. ఫ్రీ సేకరణకు **Free** అని కనిపిస్తుంది.
- **New Farmer**: **Farmer Name**, 10 అంకెల **Mobile Number** కావాలి. **Use my location** నొక్కితే **Latitude**, **Longitude** నిండుతాయి.

## సాధారణ సమస్యలు

- "Choose a farmer, or add a new one" (రైతును ఎంచుకోండి, లేదా కొత్తవారిని జోడించండి): టైప్ చేయడం మాత్రమే కాదు, లిస్ట్ నుంచి పేరు ఎంచుకోండి.
- "Choose at least one of Soil Sample or Water Sample" (Soil Sample లేదా Water Sample లో కనీసం ఒకటి ఎంచుకోండి): వాటిలో ఒకదాన్ని **Yes** గా సెట్ చేయండి.
- రైతు అడ్రస్ సరిచేయడానికి, **Edit farmer** నొక్కి, మార్చి, తర్వాత **Save farmer details** నొక్కండి.
:::

::: section edit-draft
title: డ్రాఫ్ట్ సేకరణను పూర్తి చేయడం లేదా మార్చడం
routes: /SampleCollection/new, /SampleCollection/view/{id}
for: SampleCollection.Update
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 1e935425e9054d62
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Save Draft** తో సేవ్ చేసిన సేకరణ **Draft** గానే ఉంటుంది. మీరు సబ్మిట్ చేసే వరకు ఏదీ చెల్లింపుకు గానీ ల్యాబ్‌కి గానీ వెళ్లదు. దాన్ని మళ్లీ తెరిచి పూర్తి చేయవచ్చు.

## దశలు

1. **Sample Collection** తెరిచి **Draft** టైల్ నొక్కండి.
2. కన్ను ఐకాన్‌తో సేకరణ తెరవండి.
3. **Sample Collection Details** పైన ఉన్న **Edit** నొక్కండి.
4. కొత్త సేకరణలో లాగానే శాంపిల్స్ జోడించండి, మార్చండి లేదా తీసేయండి.
5. మళ్లీ డ్రాఫ్ట్‌గా ఉంచడానికి **Save Draft** నొక్కండి, లేదా **Save and Continue** నొక్కండి.
6. **Sample Summary** లో శాంపిల్స్ చెక్ చేసి **Continue to payment** లేదా **Continue to consignment** నొక్కండి.

> సబ్మిట్ చేశాక, సేకరణను ఇక మార్చలేరు.

## సాధారణ సమస్యలు

- **Edit** బటన్ కనిపించడం లేదు: సేకరణ ఇప్పటికే సబ్మిట్ అయ్యింది, లేదా మీ హోదా మార్పులకు అనుమతించదు.
- "This collection has already been submitted" (ఈ సేకరణ ఇప్పటికే సబ్మిట్ అయ్యింది): డ్రాఫ్ట్ ముందే సబ్మిట్ అయ్యింది. స్టేటస్ ఫాలో చేయడానికి దాని వివరాలు తెరవండి.
- "That draft no longer exists" (ఆ డ్రాఫ్ట్ ఇప్పుడు లేదు): అది తీసేయబడింది. కొత్త సేకరణ మొదలుపెట్టండి.
:::

::: section record-payment
title: పెయిడ్ శాంపిల్ చెల్లింపు నమోదు చేయడం
routes: /SampleCollection/payment/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 67de9290d8256a0c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Payments** స్క్రీన్ ఎంత వసూలు చేయాలో, రైతు స్కాన్ చేయడానికి UPI QR కోడ్, చెల్లింపు వివరాలు, రసీదు కోసం ఒక ఫారం చూపిస్తుంది.

## దశలు

1. **Continue to payment** తర్వాత, **Payments** స్క్రీన్ తెరుచుకుంటుంది. సేకరణ వివరాల్లో **Record payment** నొక్కి కూడా తెరవవచ్చు.
2. **Total Amount to Collect** చెక్ చేయండి. శాంపిల్స్ చూడటానికి **View Sample Summary** నొక్కండి.
3. **Scan & Pay** కింద ఉన్న QR కోడ్‌ను ఏదైనా UPI యాప్‌తో స్కాన్ చేయమని రైతును అడగండి. దాన్ని సేవ్ చేయడానికి **Download QR Code** నొక్కండి.
4. **Payment Information** నింపండి.
5. **Payment Proof** కింద, **Click to Upload or Drag and Drop** నొక్కి రసీదు లేదా స్క్రీన్‌షాట్ ఎంచుకోండి.
6. **Save Payment** నొక్కండి. ఇప్పుడు చెల్లింపు ఆమోదం కోసం ఎదురుచూస్తుంది.

## ఫీల్డ్‌ల వివరాలు

- **Transaction ID** (తప్పనిసరి): UPI ట్రాన్సాక్షన్ నంబర్.
- **Paid By** (తప్పనిసరి): ఎవరు చెల్లించారు. సేకరణలోని రైతుల పేర్లు ఎంచుకోవడానికి కనిపిస్తాయి.
- **Payment Mode**: **UPI**, **Bank Transfer** లేదా **Cash**.
- **Transaction Date**, **Bank / Gateway**, **Payment Gateway / UTR No.**, **Bank Name**, **Contact Number**, **Email (optional)**: మీ దగ్గర ఉన్నవి నింపండి.
- **Remarks**: ఆమోదించేవారి కోసం ఒక నోట్.
- **Payment Proof**: PNG లేదా JPG ఫోటో, 5 MB వరకు.

## సాధారణ సమస్యలు

- "Please fill in the Transaction ID and Paid By" (Transaction ID, Paid By నింపండి): రెండూ తప్పనిసరి.
- "Pick a PNG or JPG image of 5 MB or less" (5 MB లేదా అంతకంటే తక్కువ ఉన్న PNG లేదా JPG ఫోటో ఎంచుకోండి): చిన్న ఫోటో తీయండి, లేదా స్క్రీన్‌షాట్ తీయండి.
- **Save Draft** టైప్ చేసిన వివరాలను ఈ ఫోన్‌లో మాత్రమే ఉంచుతుంది. **Save Payment** తో పూర్తి చేయండి.
- "Payment already recorded" (చెల్లింపు ఇప్పటికే నమోదైంది): అది ఆమోదం కోసం ఎదురుచూస్తోంది.
- చెల్లింపు తిరస్కరిస్తే, వివరాల పేజీలో కారణంతో పాటు **Payment rejected** అని కనిపిస్తుంది. సరిచేసి మళ్లీ **Record payment** నొక్కండి.
:::

::: section send-consignment
title: శాంపిల్స్‌ను ల్యాబ్‌కి పంపడం (కన్సైన్‌మెంట్)
routes: /SampleCollection/consignment/{id}, /SampleCollection/submitted/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 1820125bf239140d
confidence: ok

**ఈ స్క్రీన్ దేనికి**
సేకరణ ఫ్రీ అయినప్పుడు లేదా దాని చెల్లింపుకు ఆమోదం వచ్చినప్పుడు, మీరు శాంపిల్స్ ప్యాక్ చేసి కొరియర్‌కి ఇస్తారు. ఏమి వస్తోందో ల్యాబ్‌కి తెలిసేలా **Upload Consignment** కొరియర్ వివరాలు, ఫోటోలు నమోదు చేస్తుంది.

## దశలు

1. సేకరణ తెరిచి **Upload consignment** నొక్కండి. ఫ్రీ శాంపిల్ అయితే, **Continue to consignment** నేరుగా దీన్ని తెరుస్తుంది.
2. **Courier Details** కింద, **Courier Service** ఎంచుకుని **Tracking Number (AWB)** టైప్ చేయండి.
3. సిద్ధంగా ఉన్న వేరే సేకరణలు కూడా అదే పార్సిల్‌లో పంపాలంటే, **Include other ready collections** తెరిచి వాటిని టిక్ చేయండి. పైన ఉన్న **Both**, **Free Sample**, **Paid Sample** నొక్కితే లిస్ట్‌లో ఏవి కనిపించాలో మారుతుంది.
4. **Upload Photos and Documents** కింద, **Shipping Label / Waybill** లో ఫోటో జోడించండి. ఫోన్‌లో **Take a photo** నొక్కవచ్చు.
5. కావాలంటే **Package and contact details** తెరవండి.
6. **Submit Consignment** నొక్కండి.
7. **Consignment Submitted Successfully!** ట్రాకింగ్ నంబర్ చూపిస్తుంది. దాన్ని కాపీ చేయడానికి కాపీ ఐకాన్ నొక్కండి, వివరాలు చూడటానికి **View Consignment** నొక్కండి, లేదా **Go to Dashboard** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Tracking Link (Optional)**, **Expected Delivery Date**: పార్సిల్‌ను ఫాలో చేయడానికి ల్యాబ్‌కి సహాయపడతాయి.
- **Package Photo**, **Any other Document**: కావాలంటే మాత్రమే జోడించే ఫోటోలు, PNG లేదా JPG, ఒక్కొక్కటి 5 MB వరకు.
- **Packaging Type**, **No. of Packages**, **Package Weight (kg)**, **Contact Person**, **Mobile**, **Email**, **Collection Notes**: తప్పనిసరి కాదు.

## సాధారణ సమస్యలు

- "Please fill the courier details and add the shipping label photo" (కొరియర్ వివరాలు నింపి, షిప్పింగ్ లేబుల్ ఫోటో జోడించండి): ఈ మూడూ తప్పనిసరి.
- "This collection is not ready to send" (ఈ సేకరణ పంపడానికి ఇంకా సిద్ధంగా లేదు): చెల్లింపు ఇంకా నమోదు కాలేదు, లేదా ఆమోదం రాలేదు.
- **Save Draft** టైప్ చేసిన ఫీల్డ్‌లను మాత్రమే ఉంచుతుంది. ఫోటోలు తర్వాత మళ్లీ జోడించండి.
- JMDO: కన్సైన్‌మెంట్ సబ్మిట్ చేస్తే ఈరోజు **Soil Sample** టాస్క్ పూర్తయినట్టు నమోదవుతుంది.
:::

::: section collection-details
title: శాంపిల్ సేకరణ వివరాలు చదవడం
routes: /SampleCollection/view/{id}
for: 
roles: 
designations: 
source-hash: c0ff9ecbae88fe84
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Sample Collection Details** ఒక సేకరణ గురించి అన్నీ చూపిస్తుంది: చెల్లింపు, కొరియర్, ల్యాబ్ ఫలితాలు, చరిత్ర. ఫీల్డ్ సిబ్బంది, డీలర్లు, మేనేజర్లు ఇక్కడ పురోగతిని ఫాలో చేయవచ్చు.

## దశలు

1. **Sample Collection** తెరిచి ఒక సేకరణపై కన్ను ఐకాన్ నొక్కండి.
2. పై లైన్‌లో సేకరణ ID, **Paid Sample** లేదా **Free Sample**, ప్రస్తుత స్టేటస్ కనిపిస్తాయి.
3. పట్టీలో **Consignment ID**, **Transaction ID**, **Courier Partner**, **Samples**, **No. of Tests**, **Payment Status** కనిపిస్తాయి. కన్సైన్‌మెంట్ తెరవడానికి కన్సైన్‌మెంట్ ID నొక్కండి.
4. శాంపిల్స్ ఎవరు సేకరించారో, ఒక్కో శాంపిల్‌కి ఎన్ని పరీక్షలు కావాలో **Collection Information**, **Sample Summary** చూపిస్తాయి.
5. ల్యాబ్ విలువలు సిద్ధమయ్యాక **Result Comparison** వాటిని చూపిస్తుంది. చాలా శాంపిల్స్ ఉంటే, ఒక శాంపిల్ ఫలితాలు చూడటానికి పైన ఆ శాంపిల్ నొక్కండి.
6. **Consignments Details**, **Amount Details**, **History / Timeline** లో కొరియర్, ఛార్జీలు, ఇప్పటివరకు జరిగిన ప్రతి దశ కనిపిస్తాయి.
7. ప్రింట్ లేదా డౌన్‌లోడ్ చేయడానికి **Export** నొక్కండి.

## మీకు కనిపించే బటన్లు

- **Edit**: సేకరణ ఇంకా డ్రాఫ్ట్‌గా ఉంది.
- **Record payment**: చెల్లింపు ఇంకా పెండింగ్‌లో ఉంది.
- **Upload consignment**: ల్యాబ్‌కి పంపడానికి సిద్ధంగా ఉంది.

## సాధారణ సమస్యలు

- "The lab has not published the results yet" (ల్యాబ్ ఇంకా ఫలితాలు ఇవ్వలేదు): శాంపిల్ ఇంకా దారిలో ఉంది, లేదా పరీక్ష జరుగుతోంది.
- కన్సైన్‌మెంట్ కింద **Not dispatched yet**: శాంపిల్స్ ఇంకా పంపలేదు.
- "Collection not found" (సేకరణ దొరకలేదు): అది తీసేయబడింది, లేదా మీకు యాక్సెస్ లేదు.
:::

::: section admin-review
title: చెల్లింపులు ఆమోదించడం, కన్సైన్‌మెంట్‌లు ముందుకు మార్చడం, ఫలితాలు ఎంటర్ చేయడం
routes: /SampleCollection/view/{id}, /ConsignmentHistory/view/{id}
for: 
roles: Admin, CorporateAdmin
designations: 
source-hash: 71850d4b14d9d073
confidence: ok

**ఈ స్క్రీన్ దేనికి**
అడ్మినిస్ట్రేటర్లు సేకరణ వివరాల పేజీలో చెల్లింపును రివ్యూ చేయవచ్చు, కన్సైన్‌మెంట్‌ను తర్వాతి దశకు మార్చవచ్చు, ల్యాబ్ ఫలితాలు టైప్ చేయవచ్చు.

## దశలు

### సేకరణలో చెల్లింపును ఆమోదించడం

1. **Pending Approval** స్టేటస్ ఉన్న సేకరణ తెరవండి.
2. **Payment Approval** లో **Amount**, **Transaction ID**, రసీదు పోల్చి చూడండి. రసీదు పెద్దగా చూడటానికి **View proof** నొక్కండి.
3. **Approve payment** నొక్కి నిర్ధారించండి. సేకరణ పంపడానికి సిద్ధమవుతుంది.
4. తిరస్కరించడానికి, **Reject** నొక్కి, **Reason for rejection** రాసి, తర్వాత **Reject payment** నొక్కండి.

### కన్సైన్‌మెంట్‌ను ముందుకు మార్చడం

1. **Consignment History** తెరిచి ఒక కన్సైన్‌మెంట్ తెరవండి.
2. **Update status** కింద, **In Transit**, **Delivered** లేదా **Completed** లాంటి తర్వాతి దశ నొక్కండి.

> కన్సైన్‌మెంట్‌ను ముందుకు మారిస్తే, దానిలోని ప్రతి శాంపిల్ కూడా ముందుకు మారుతుంది.

### ల్యాబ్ ఫలితాలు ఎంటర్ చేయడం

1. సేకరణ వివరాల్లో **Result Comparison** కి వెళ్లి, శాంపిల్ ఎంచుకుని **Enter results** (లేదా **Edit results**) నొక్కండి.
2. **Test Parameter**, **Entered Value**, **Unit**, **Normal Range**, **Result** నింపండి. ఇంకా వరుసలు కావాలంటే **Add parameter** నొక్కండి.
3. **Save results** నొక్కండి.

## సాధారణ సమస్యలు

- "Add a reason so the collector knows what to correct" (సేకరించినవారికి ఏమి సరిచేయాలో తెలిసేలా కారణం రాయండి): తిరస్కరణకు కారణం తప్పనిసరి.
- **Update status** కనిపించడం లేదు: కన్సైన్‌మెంట్ ఇప్పటికే **Completed** అయ్యింది.
:::

::: section consignment-history
title: ల్యాబ్‌కి పంపిన కన్సైన్‌మెంట్‌లను ట్రాక్ చేయడం
routes: /ConsignmentHistory, /ConsignmentHistory/view/{id}
for: 
roles: 
designations: 
source-hash: c0eb73648ebf3b77
confidence: ok

**ఈ స్క్రీన్ దేనికి**
**Consignment History** ల్యాబ్‌కి పంపిన ప్రతి శాంపిల్ పార్సిల్‌ను, కొరియర్, ట్రాకింగ్ నంబర్, ప్రస్తుత స్టేటస్‌తో చూపిస్తుంది.

## దశలు

1. మెనూ నుంచి **Consignment History** తెరవండి.
2. కార్డులు **Total Consignments**, **Total Consignment Sent**, **Total Samples**, **Delivered**, **Pending** చూపిస్తాయి.
3. ఒకదాన్ని కనుక్కోవడానికి **Search consignment, AWB, transaction...** లో టైప్ చేయండి.
4. **Filters** తెరిచి **From**, **To**, **Status**, **Courier Partner** లేదా **Payment Type** ఎంచుకుని, **Apply Filters** నొక్కండి.
5. కొరియర్ ట్రాకింగ్ పేజీ తెరవడానికి ట్రాకింగ్ నంబర్ నొక్కండి.
6. **Consignment Details** తెరవడానికి కన్సైన్‌మెంట్ ID లేదా కన్ను ఐకాన్ నొక్కండి.
7. వివరాల పేజీలో టైమ్‌లైన్, లోపల ఉన్న శాంపిల్స్, అమౌంట్ చూడండి. లేబుల్, ప్యాకేజీ ఫోటోలు చూడటానికి **Click and View Photos and Files** నొక్కండి, వాటిని సేవ్ చేయడానికి **Download All** నొక్కండి.
8. డౌన్‌లోడ్ చేయడానికి **Excel** లేదా **PDF** వాడండి, అప్‌డేట్ చేయడానికి **Refresh Data** వాడండి.

## ఫీల్డ్‌ల వివరాలు

- **Current Status**: **Pending Pickup**, **Dispatched**, **In Transit**, **Delivered** లేదా **Completed**.
- **AWB/Tracking No.**: కొరియర్ ట్రాకింగ్ నంబర్.
- **Total samples**: ఫోన్‌లో, లిస్ట్ చూడటానికి శాంపిల్స్ సంఖ్యపై నొక్కండి.

## సాధారణ సమస్యలు

- **No consignments found** (కన్సైన్‌మెంట్‌లు ఏవీ దొరకలేదు): వేరే తేదీల పరిధి, స్టేటస్ లేదా కొరియర్ ప్రయత్నించండి, లేదా **Clear filters** నొక్కండి.
- **No photos yet** (ఇంకా ఫోటోలు లేవు): కన్సైన్‌మెంట్ పంపినప్పుడు ఏమీ జోడించలేదు.
- JMDO వాళ్లకు ఈ పేజీ మెనూలో కనిపించదు.
:::

::: section farmer-my-samples
title: మీ మట్టి శాంపిల్స్ చూడటం (రైతు)
routes: /Sas/MySamples, /SampleCollection/view/{id}
for: 
roles: Farmer
designations: 
source-hash: fa5d1d2f177d288c
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ MO ఆఫీసర్ మీ దగ్గర సేకరించిన ప్రతి మట్టి లేదా నీటి శాంపిల్‌ను, ఒక్కొక్కటి ఇప్పుడు ఎక్కడ ఉందో **My Samples** చూపిస్తుంది.

## దశలు

1. మెనూ నుంచి **My Samples** తెరవండి.
2. ప్రతి కార్డులో శాంపిల్ నంబర్, కన్సైన్‌మెంట్, సేకరించిన ఆఫీసర్, తేదీ, పంట కనిపిస్తాయి.
3. రంగు లేబుల్స్ **Paid** లేదా **Free**, శాంపిల్ స్టేటస్, **Report: Ready** లేదా **Report: Not Ready** చూపిస్తాయి.
4. ఒక శాంపిల్ కనుక్కోవడానికి, **Search sample, consignment, crop** లో టైప్ చేయండి, తేదీలు ఎంచుకోండి, లేదా రకం, స్టేటస్ లేదా పంట ఎంచుకోండి.
5. శాంపిల్ తెరవడానికి **View Details** నొక్కండి.
6. వివరాల పేజీలో **Sample Details**, **Farmer Details**, **Payment Details**, ఇంకా **Sample Collected by MO** నుంచి **Report Ready for Download** వరకు ఉన్న **Sample Journey** దశలు చూడండి.
7. రిపోర్ట్ సిద్ధమైనప్పుడు, **Download PDF** నొక్కండి. తమిళం లేదా ఇంగ్లీష్ ఎంచుకోవడానికి దాని పక్కన ఉన్న చిన్న బాణం నొక్కండి.

## స్టేటస్ అర్థం

- **Payment Pending**: చెల్లింపు ఇంకా పూర్తి కాలేదు.
- **Awaiting Dispatch**: ల్యాబ్‌కి పంపడానికి ఎదురుచూస్తోంది.
- **In Transit**: ల్యాబ్‌కి వెళ్తున్న దారిలో ఉంది.
- **Under Analysis**: ల్యాబ్ దాన్ని పరీక్షిస్తోంది.
- **Report Ready**: మీరు రిపోర్ట్ డౌన్‌లోడ్ చేసుకోవచ్చు.

## సాధారణ సమస్యలు

- **No samples yet** (ఇంకా శాంపిల్స్ లేవు): మీ ఆఫీసర్ మీ పేరు మీద ఇంకా శాంపిల్ జోడించలేదు. మీ MO ఆఫీసర్‌ని అడగండి.
- **Download PDF** కనిపించడం లేదు: ల్యాబ్ ఇంకా రిపోర్ట్ పూర్తి చేయలేదు.
:::

::: section farmer-my-reports
title: మీ మట్టి పరీక్ష రిపోర్ట్ డౌన్‌లోడ్ చేయడం (రైతు)
routes: /Sas/MyReports
for: 
roles: Farmer
designations: 
source-hash: 810f0b093cade052
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ శాంపిల్స్ కోసం ల్యాబ్ తయారు చేసిన పరీక్ష రిపోర్ట్‌లను **My Reports** చూపిస్తుంది. ప్రతి రిపోర్ట్‌ను తమిళం లేదా ఇంగ్లీష్‌లో డౌన్‌లోడ్ చేసుకోవచ్చు.

## దశలు

1. మెనూ నుంచి **My Reports** తెరవండి.
2. కార్డులు **Total Reports**, **Ready to Download**, **Downloaded**, **Under Process** చూపిస్తాయి.
3. **Reports List** లో మీ రిపోర్ట్ కనుక్కోండి. లిస్ట్ తగ్గించడానికి **Search report, sample, crop**, తేదీలు, **All Statuses** లేదా **All Crops** వాడండి.
4. రిపోర్ట్ సేవ్ చేయడానికి **Download PDF** నొక్కండి. తమిళం లేదా ఇంగ్లీష్ ఎంచుకోవడానికి దాని పక్కన ఉన్న చిన్న బాణం నొక్కండి.
5. **Report Details** చదవడానికి కన్ను ఐకాన్ (ఫోన్‌లో అయితే **View**) నొక్కండి: పరీక్ష ఫలితాలు, సిఫార్సులు, పంట సలహా.

## ఫీల్డ్‌ల వివరాలు

- **Reports No.**: రిపోర్ట్ నంబర్.
- **Sample No.**: ఈ రిపోర్ట్ ఏ శాంపిల్‌దో ఆ నంబర్.
- **Status**: **Report Ready** (ఇంకా డౌన్‌లోడ్ చేయలేదు), **Downloaded** లేదా **Printed**.
- **Under Process**: ఇంకా ల్యాబ్‌లో పరీక్ష జరుగుతున్న శాంపిల్స్.

## సాధారణ సమస్యలు

- **No reports yet** (ఇంకా రిపోర్ట్‌లు లేవు): ల్యాబ్ ఇంకా పూర్తి చేయలేదు. ఎన్ని శాంపిల్స్ పరీక్షలో ఉన్నాయో మెసేజ్ చెబుతుంది.
- **Reports are not available right now** (ఇప్పుడు రిపోర్ట్‌లు అందుబాటులో లేవు): మీ ఇంటర్నెట్ చెక్ చేసి **Try again** నొక్కండి.
- ఫైల్ తెరుచుకోలేదు: మీ ఫోన్‌లోని డౌన్‌లోడ్స్ ఫోల్డర్ చెక్ చేయండి.
:::

::: section farmer-payments
title: మీ శాంపిల్ చెల్లింపులు చెక్ చేయడం (రైతు)
routes: /Sas/Payments, /Sas/Payments/{id}
for: 
roles: Farmer
designations: 
source-hash: 41753a10de9d41d6
confidence: ok

**ఈ స్క్రీన్ దేనికి**
మీ పెయిడ్ మట్టి శాంపిల్స్ కోసం చేసిన చెల్లింపులను, వాటిని SPIC ఆమోదించిందా, అందుకుందా అనేది **Payments** చూపిస్తుంది. ఇక్కడ మీరు చూడటం మాత్రమే; ఏమీ నింపాల్సిన అవసరం లేదు.

## దశలు

1. మెనూ నుంచి **Payments** తెరవండి.
2. కార్డులు **Total Paid Samples**, **Finance Verified**, **Approval Pending**, **Payment Issues** చూపిస్తాయి.
3. **Search sample no., transaction ID...** తో వెతకండి, లేదా తేదీలు, స్టేటస్ లేదా పంట ఎంచుకోండి.
4. **Payment Details** తెరవడానికి శాంపిల్ నంబర్, కన్ను ఐకాన్ లేదా **View Details** నొక్కండి.
5. అమౌంట్, **Payment Mode**, **Transaction ID**, **Payment Proof** కింద ఉన్న రసీదు, **Verification Journey** చూడండి.

## స్టేటస్ అర్థం

- **Admin Approval Pending**: మీ చెల్లింపు ఆమోదం కోసం ఎదురుచూస్తోంది.
- **Admin Approved**: ఆమోదించారు, Finance కోసం ఎదురుచూస్తోంది.
- **Finance Verified**: చెల్లింపు పూర్తిగా నిర్ధారించబడింది.
- **Payment Issue**: చెల్లింపు తిరిగి పంపబడింది, లేదా సరిపోలలేదు. మీ MO ఆఫీసర్ దాన్ని సరిచేస్తారు.

## సాధారణ సమస్యలు

- **No payments found** (చెల్లింపులు ఏవీ దొరకలేదు): మీకు పెయిడ్ శాంపిల్స్ లేవు, లేదా మీ శాంపిల్స్ ఫ్రీ.
- **Payment Issue** చాలా రోజులు అలాగే ఉంటే: **Transaction ID** తో మీ MO ఆఫీసర్‌ని సంప్రదించండి.
:::

::: section approve-payments
title: శాంపిల్ చెల్లింపులు ఆమోదించి Finance కి పంపడం
routes: /Sas/Payments, /Sas/Payments/{id}
for: SasPaymentApproval.View
roles: 
designations: 
source-hash: 4a8d8dd3f9015aa4
confidence: ok

**ఈ స్క్రీన్ దేనికి**
పెయిడ్ శాంపిల్స్ కోసం ఫీల్డ్ ఆఫీసర్లు నమోదు చేసిన చెల్లింపులను **Payment Approval** చూపిస్తుంది. మీరు ప్రతి రసీదు, అమౌంట్ చెక్ చేసి, ఆమోదించి Finance కి పంపుతారు, లేదా ఆఫీసర్‌కి తిరిగి పంపుతారు.

## దశలు

1. మెనూ నుంచి **Payment Approval** తెరవండి.
2. **Pending Admin Approval** ట్యాబ్ నొక్కండి.
3. **Payment Approval Details** తెరవడానికి **Payment Ref No.** లేదా కన్ను ఐకాన్ నొక్కండి.
4. **Expected Amount**, **Paid Amount**, **Difference** పోల్చి చూడండి, **Payment Proof Preview** కింద రసీదు చెక్ చేయండి.
5. **Approve & Forward to Finance** నొక్కండి.
6. **Verified Amount** (లేదా **Other amount**) ఎంచుకుని, **Remarks** రాసి, **I confirm that the payment proof, transaction details and amount are verified.** (చెల్లింపు రుజువు, ట్రాన్సాక్షన్ వివరాలు, అమౌంట్ చెక్ చేశానని నిర్ధారిస్తున్నాను) టిక్ చేసి, తర్వాత **Approve & Forward** నొక్కండి.
7. తిరస్కరించడానికి, **Reject Payment** నొక్కి, **Reason for rejection** రాసి, మళ్లీ **Reject Payment** నొక్కండి.

## ఫీల్డ్‌ల వివరాలు

- **Admin Status**: **Pending Review**, **Admin Approved** లేదా **Returned to MO**.
- **Finance Status**: **Not Forwarded**, **Pending**, **Verified** లేదా **Mismatch**.
- **Difference**: చెల్లించిన అమౌంట్ రావాల్సిన అమౌంట్‌కి సమానం కాకపోతే ఎరుపు రంగులో ఉంటుంది.

## సాధారణ సమస్యలు

- "Tick the confirmation to continue" (ముందుకు వెళ్లడానికి నిర్ధారణ బాక్స్ టిక్ చేయండి): ఆమోదించే ముందు బాక్స్ టిక్ చేయండి.
- "Enter a verified amount greater than zero" (సున్నా కంటే ఎక్కువ verified amount ఎంటర్ చేయండి): అమౌంట్ ఎంచుకోండి లేదా టైప్ చేయండి.
- యాక్షన్ బటన్లు లేవు: ఈ చెల్లింపుపై ఇప్పటికే నిర్ణయం తీసుకున్నారు.
- పాత చెల్లింపులు కనుక్కోవడానికి **Filters** (తేదీలు, **State**, **Region**, **Payment Mode**, **Status**) వాడండి.
:::

::: section verify-payments
title: అందిన చెల్లింపులను నిర్ధారించడం (Finance)
routes: /Sas/Payments, /Sas/Payments/{id}, /Sas/Payments/History
for: SasPaymentVerification.View
roles: 
designations: 
source-hash: ec36e4e58f4e4b58
confidence: ok

**ఈ స్క్రీన్ దేనికి**
ఆమోదించేవారు Finance కి పంపిన చెల్లింపులను **Payment Verification** చూపిస్తుంది. డబ్బు అకౌంట్‌లోకి వచ్చిందని మీరు నిర్ధారిస్తారు. మీరు పూర్తి చేసిన ప్రతి చెల్లింపు **Payment History** లో ఉంటుంది.

## దశలు

1. మెనూ నుంచి **Payment Verification** తెరవండి.
2. **Pending Verification** ట్యాబ్ నొక్కండి.
3. **Payment Ref No.** లేదా కన్ను ఐకాన్‌తో ఒక చెల్లింపు తెరవండి. **Payment Proof Details** తెరుచుకుంటుంది.
4. **Payment Proof Preview** కింద ఉన్న రసీదును మీ బ్యాంక్ స్టేట్‌మెంట్‌తో పోల్చి చెక్ చేయండి.
5. డబ్బు వచ్చి ఉంటే, **Verify Payment** నొక్కండి. **Verified Amount** ఎంచుకుని, **Received Date** సెట్ చేసి, **Remarks** రాసి, **I have verified the payment proof, transaction ID, and amount.** (చెల్లింపు రుజువు, ట్రాన్సాక్షన్ ID, అమౌంట్ చెక్ చేశాను) టిక్ చేసి **Confirm** నొక్కండి.
6. చెల్లింపు కనిపించకపోతే, **Mark Mismatch** నొక్కి, **Mismatch remarks** రాసి, మళ్లీ **Mark Mismatch** నొక్కండి.
7. పూర్తయిన చెల్లింపులు చూడటానికి **Payment History** తెరవండి. **All**, **Verified**, **Failed**, **Mismatch** ట్యాబ్‌లు వాడండి.

## ఫీల్డ్‌ల వివరాలు

- **Verified Amount**: నిజంగా అందిన అమౌంట్. రావాల్సిన దానికంటే తక్కువ అయితే mismatch (సరిపోలలేదు) గా సేవ్ అవుతుంది.
- **Payment Verified**: డబ్బు అందినట్టు నిర్ధారించారు.
- **Amount Mismatch**: రావాల్సిన దానికంటే తక్కువ డబ్బు వచ్చింది.
- **Failed**: **Mark Mismatch** తో గుర్తించారు; డబ్బు ఏమీ కనిపించలేదు.

## సాధారణ సమస్యలు

- "Enter the amount received" (అందిన అమౌంట్ ఎంటర్ చేయండి): అమౌంట్ ఎంచుకోండి లేదా టైప్ చేయండి.
- "Tick the confirmation to continue" (ముందుకు వెళ్లడానికి నిర్ధారణ బాక్స్ టిక్ చేయండి): నిర్ధారించే ముందు బాక్స్ టిక్ చేయండి.
- యాక్షన్ బటన్లు లేవు: చెల్లింపు ఇప్పటికే నిర్ధారించారు, లేదా ఇప్పటికే గుర్తించారు.
:::
