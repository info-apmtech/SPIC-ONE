---
module: demos
title: फ़सल डेमो और ट्रीटमेंट
icon: bi-flower1
order: 108
language: hi
category: फ़ील्ड का काम
note: नई गाइड
meta-source-hash: c6011d305dce737c
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b0aaf57f4576cdfb
confidence: ok

किसान के खेत में फ़सल डेमो (प्रदर्शन) को शुरू से आख़िर तक दर्ज करें। किसान और फ़सल की जानकारी एक बार सेव करें, फिर हर खेत विज़िट को ऑब्ज़र्वेशन और फ़ोटो के साथ एक ट्रीटमेंट के रूप में दर्ज करें, और हर विज़िट की तुलना पिछली विज़िट से करें।

- start | icon: bi-pencil-square | color: #65a30d
  title: डेमो शुरू करें
  text: मंज़ूर हुए CSR-2 प्रोग्राम के लिए किसान, जगह और फ़सल की जानकारी भरें।
- treat | icon: bi-camera | color: #d97706
  title: हर विज़िट दर्ज करें
  text: प्रोडक्ट, डोज़, ऑब्ज़र्वेशन और कंट्रोल व ट्रीटेड फ़ोटो सेव करें।
- compare | icon: bi-arrow-left-right | color: #16a34a
  title: तुलना करें और बंद करें
  text: अगला ट्रीटमेंट दर्ज करते समय पिछला ट्रीटमेंट देखें। जब कोई दिक्कत न बचे, तो डेमो बंद करें।
:::

::: section flow
title: डेमो कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 672fd04ad6279036
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- csr | type: lane | color: #0891b2
  title: CSR टीम
  sub: CSR-1 और CSR-2
- field | type: lane | color: #65a30d | roles: MDO, JMDO
  title: ज़िम्मेदार व्यक्ति
  sub: CSR-1 में जिनका नाम है, वे फ़ील्ड स्टाफ़
- eligible | lane: csr | col: 1 | icon: bi-patch-check | color: #64748b | next: start | screen: demo-list
  title: CSR-2 मंज़ूर
  status: Not Started
  text: CSR-1 में आपका नाम **Responsible Person** के रूप में होता है। जब उसका CSR-2 आख़िरी तौर पर मंज़ूर हो जाता है, तो डेमो आपकी **Demo Documentation** लिस्ट में आ जाता है।
- start | lane: field | col: 2 | icon: bi-pencil-square | color: #d97706 | next: treat | screen: start-documentation
  title: डॉक्यूमेंटेशन शुरू करें
  status: In Progress
  text: किसान, जगह और फ़सल की जानकारी भरें और **Start Documentation** दबाएँ।
- treat | lane: field | col: 3 | icon: bi-camera | color: #64748b | next: close, followup | screen: record-treatment
  title: ट्रीटमेंट दर्ज करें
  status: Draft
  text: हर खेत विज़िट पर ट्रीटमेंट, ऑब्ज़र्वेशन और फ़ोटो दर्ज करें। बाद में पूरा करना हो तो **Save Draft** दबाएँ, नहीं तो **Complete Treatment** दबाएँ।
- followup | lane: field | col: 4 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: treat | to: treat | screen: compare-treatments
  title: दिक्कत मिली, फिर से विज़िट करें
  status: In Progress
  text: अगर आपने **Problem Observed?** में **Yes** रखकर ट्रीटमेंट पूरा किया, तो डेमो खुला रहता है। अगली विज़िट के लिए **Start Next Treatment** दबाएँ।
- close | lane: field | col: 5 | icon: bi-clipboard-check | color: #16a34a | screen: demo-details
  title: डेमो पूरा हुआ
  status: Completed
  text: जब आप **Problem Observed?** में **No** रखकर ट्रीटमेंट पूरा करते हैं, तो पूरा डेमो पूरा हो जाता है और फिर उसे सिर्फ़ देखा जा सकता है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c13ab7e2ba0afb47
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: डेमो अपनी पहली विज़िट का इंतज़ार कर रहा है। शुरुआती फ़ॉर्म ड्राफ़्ट में सेव हो, तब भी यही दिखता है।
- in-progress | color: #d97706
  label: In Progress
  text: डॉक्यूमेंटेशन शुरू हो गया है। ट्रीटमेंट दर्ज हो रहे हैं।
- draft | color: #64748b
  label: Draft
  text: ट्रीटमेंट सेव है पर पूरा नहीं हुआ। आप उसे अभी भी बदल सकते हैं।
- completed | color: #16a34a
  label: Completed
  text: ट्रीटमेंट के लिए: वह पूरा हो गया है और बदला नहीं जा सकता। डेमो के लिए: वह बंद हो गया है और सिर्फ़ देखा जा सकता है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 94984548531519c1
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही डेमो दिखते हैं जिनमें आप ज़िम्मेदार व्यक्ति हैं।

- field | color: #65a30d | roles: MDO, JMDO
  title: ज़िम्मेदार व्यक्ति
  - हर मंज़ूर डेमो का डॉक्यूमेंटेशन शुरू करना
  - हर खेत विज़िट को ट्रीटमेंट के रूप में दर्ज करना
  - कंट्रोल और ट्रीटेड फ़ोटो अपलोड करना
  - जब कोई दिक्कत न बचे, तो डेमो बंद करना
- csr | color: #0891b2
  title: CSR टीम
  - CSR-1 बनाना और Responsible Person चुनना
  - CSR-2 मंज़ूर करना, ताकि डेमो का डॉक्यूमेंटेशन हो सके
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 03c835ce85e68364
confidence: ok

- not-listed | popular: true
  q: मेरा डेमो लिस्ट में नहीं है
  a: लिस्ट में सिर्फ़ वे प्रोग्राम दिखते हैं जिनका CSR-2 आख़िरी तौर पर मंज़ूर हो चुका है और जिनके CSR-1 में आप **Responsible Person** हैं। जिसने CSR-1 बनाया था, उससे इसे जाँचने को कहें।
- no-next | popular: true
  q: मुझे Start Next Treatment नहीं दिख रहा
  a: यह तभी दिखता है जब आपका आख़िरी ट्रीटमेंट **Completed** हो और डेमो बंद न हुआ हो। अगर आख़िरी ट्रीटमेंट **Draft** है, तो उसे **View Details** से खोलें और पहले उसे पूरा करें।
- closed-early
  q: ट्रीटमेंट पूरा करने के बाद डेमो बंद हो गया
  a: **Problem Observed?** में **No** रखकर ट्रीटमेंट पूरा करने से पूरा डेमो पूरा हो जाता है। पूरा हुआ डेमो सिर्फ़ देखा जा सकता है, इसलिए **Complete Treatment** दबाने से पहले यह जवाब जाँच लें।
- photos
  q: मेरी फ़ोटो सेव नहीं हो रहीं
  a: फ़ोटो तभी अपलोड होती हैं जब आप **Save Draft** या **Complete Treatment** दबाते हैं। JPG या PNG फ़ाइलें इस्तेमाल करें, हर एक 10 MB या उससे कम, एक बार में 20 तक।
- missing-id
  q: मुझे "Missing or invalid csr2Id in URL" दिख रहा है
  a: स्क्रीन बिना डेमो चुने खोली गई। **Demo Documentation** खोलें और उस डेमो के **Action** कॉलम का बटन दबाएँ।
:::

::: section demo-list
title: मेरे डेमो देखें
routes: /DemoDocumentation
for: 
roles: 
designations: 
minutes: 2
source-hash: 4947901ab12cde8b
confidence: ok

**यह स्क्रीन किस काम की है**
**Demo Documentation** स्क्रीन पर आपको दिए गए हर मंज़ूर CSR-2 प्रोग्राम की लिस्ट होती है, साथ में उसका स्टेज और कितने ट्रीटमेंट हो चुके हैं।

1. ऊपर के कार्ड देखें: **Total Demonstrations**, **Not Started**, **In Progress** और **Completed**।
2. **Filters** में **Sanction Number**, **Program Name**, **Product**, **Crop**, **District**, **Stage** या **Status** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
3. सैंक्शन नंबर, प्रोग्राम, प्रोडक्ट, फ़सल या जगह से डेमो ढूँढने के लिए **Search demos...** बॉक्स में टाइप करें।
4. टेबल में हर डेमो का **Current Stage**, **Date of Sowing**, **Treatments**, **Last Treatment** और **Document Status** देखें।
5. **Action** कॉलम में नए डेमो के लिए **Start Documentation**, चल रहे डेमो के लिए **View / Continue**, या पूरे हुए डेमो के लिए **View** दबाएँ।
6. और लाइनें देखने के लिए नीचे **Items Per Page** और पेज नंबर इस्तेमाल करें।

> Tip: सिर्फ़ आपको दिए गए, मंज़ूर CSR-2 प्रोग्राम ही लिस्ट में दिखते हैं।

## आम दिक्कतें

- **"No eligible demonstration records found".** (कोई सही डेमो नहीं मिला) अभी किसी मंज़ूर CSR-2 में आपका नाम ज़िम्मेदार व्यक्ति के रूप में नहीं है, या आपके फ़िल्टर बहुत कड़े हैं। **Clear All** दबाएँ।
- **"Unable to load approved CSR-2 demonstrations".** (मंज़ूर CSR-2 डेमो लोड नहीं हो सके) ऐप सर्वर तक नहीं पहुँच पाया। अपना इंटरनेट देखें और स्क्रीन फिर से खोलें।
:::

::: section start-documentation
title: डेमो का डॉक्यूमेंटेशन शुरू करें
routes: /StartDocumentation
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-sd-program] | 2 > [data-help=dm-sd-farmer] | 3 > [data-help=dm-sd-crop] | 4 > [data-help=dm-sd-actions]
minutes: 4
source-hash: a86f4d3929f78e21
confidence: ok

**यह स्क्रीन किस काम की है**
पहली विज़िट पर डेमो के किसान, जगह और फ़सल की जानकारी सेव करने के लिए **Start Documentation** स्क्रीन का इस्तेमाल करें। इसे लिस्ट से **Start Documentation** दबाकर खोलें।

1. **Program Details (From CSR-2)** देखें: **Program Type**, **Program Name**, **Sanction Number**, **Actual Event Location** और **Focus Products**।
2. **Farmer & Location Details** में **Farmer Name**, **Mobile Number**, **Place / Village** और **Farmer Address** भरें। **Crop** और **Crop Type** चुनें, **Pin Code** भरें, फिर **State** और **District** चुनें।
3. **Crop Information** में **Date Of Sowing** भरें, **Current Crop Stage** चुनें, **Expected Harvest Date** भरें और ज़रूरत हो तो **Remarks** लिखें।
4. बाद में पूरा करना हो तो **Save Draft** दबाएँ, या शुरू करने के लिए **Start Documentation** दबाएँ। **Start Documentation** दबाने पर **Demo Details** स्क्रीन खुलती है।

> Careful: **Start Documentation** दबाने के बाद लिस्ट से यह फ़ॉर्म नहीं, बल्कि **Demo Details** खुलता है। शुरू करने से पहले जानकारी ठीक से जाँच लें।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Farmer Name** | वह किसान जिसके खेत में डेमो है। ज़रूरी है। |
| **Mobile Number** | किसान का 10 अंकों का मोबाइल नंबर। ज़रूरी है। |
| **Crop** | सिर्फ़ इस CSR-2 के फ़ोकस प्रोडक्ट वाली फ़सलें दिखती हैं। ज़रूरी है। |
| **Crop Type** और **Current Crop Stage** | फ़सल चुनने के बाद लोड होते हैं। ज़रूरी है। |
| **Pin Code** | 6 अंक। ज़रूरी है। |
| **State** और **District** | पहले State चुनें, फिर District। ज़रूरी है। |

## आम दिक्कतें

- **"Select State, District, Crop, Crop Type and Current Crop Stage."** (State, District, Crop, Crop Type और Current Crop Stage चुनें) इनमें से कोई लिस्ट खाली है। हर एक चुनें।
- **"Enter farmer name, a 10-digit mobile number and a 6-digit PIN code."** (किसान का नाम, 10 अंकों का मोबाइल नंबर और 6 अंकों का PIN कोड भरें) मोबाइल नंबर और PIN कोड जाँचें।
- **"This CSR-2 is not finally approved or is outside your permitted scope."** (यह CSR-2 आख़िरी तौर पर मंज़ूर नहीं है या आपकी अनुमति के दायरे से बाहर है) यह डेमो आपको नहीं दिया गया है। लिस्ट पर वापस जाएँ।
:::

::: section demo-details
title: डेमो और उसके ट्रीटमेंट देखें
routes: /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 7530f32fb5322456
confidence: ok

**यह स्क्रीन किस काम की है**
**Demo Details** स्क्रीन पर डेमो का सारांश, किसान की जानकारी और **Treatment Journey** दिखती है, यानी दर्ज किए गए हर ट्रीटमेंट की लिस्ट।

1. लिस्ट से **View / Continue** या **View** दबाकर डेमो खोलें।
2. ऊपर का कार्ड देखें: प्रोडक्ट, **Sanction Number**, **Crop**, **Date of Sowing:**, **Crop Stage:** और **Expected Harvest:**।
3. किसान का कार्ड देखें: **Farmer**, **Mobile Number**, **District**, **State**, **Pin Code** और **Location**।
4. अगर अभी तक कोई ट्रीटमेंट दर्ज नहीं हुआ है, तो **Start First Treatment** दबाएँ।
5. **Treatment Journey** में हर ट्रीटमेंट का स्टेटस, **Visit Date**, **Plant Stage**, **Dosage** और **Remarks** दिखते हैं। **Treatments Done** में पूरे हुए ट्रीटमेंट की गिनती होती है।
6. किसी ट्रीटमेंट को खोलने के लिए उस पर **View Details** दबाएँ।
7. जब आख़िरी ट्रीटमेंट **Completed** हो जाए, तो अगली विज़िट के लिए **Start Next Treatment** दबाएँ।

## आम दिक्कतें

- **Start Next Treatment बटन नहीं है।** आख़िरी ट्रीटमेंट अभी भी **Draft** है, या डेमो **Completed** हो चुका है।
- **"Farmer and crop documentation is not available yet."** (किसान और फ़सल का डॉक्यूमेंटेशन अभी उपलब्ध नहीं है) शुरुआती फ़ॉर्म शुरू नहीं हुआ। लिस्ट से डेमो खोलें और **Start Documentation** दबाएँ।
- **"CSR-2 not found, not finally approved, or outside your permitted scope."** (CSR-2 नहीं मिला, आख़िरी तौर पर मंज़ूर नहीं है, या आपकी अनुमति के दायरे से बाहर है) यह डेमो आपको नहीं दिया गया है।
:::

::: section record-treatment
title: खेत विज़िट पर ट्रीटमेंट दर्ज करें
routes: /TreatmentDetails
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-td-visit] | 2 > [data-help=dm-td-treatment] | 3 > [data-help=dm-td-observations] | 4 > [data-help=dm-td-images] | 5 > [data-help=dm-td-problem] | 6 > [data-help=dm-td-actions]
minutes: 5
source-hash: d955aff37a8e757f
confidence: ok

**यह स्क्रीन किस काम की है**
हर खेत विज़िट पर क्या डाला गया, आपने क्या देखा, और कंट्रोल व ट्रीटेड पौधों की फ़ोटो दर्ज करने के लिए ट्रीटमेंट स्क्रीन (जैसे **Treatment 01**) का इस्तेमाल करें। इसे **Start First Treatment**, **Start Next Treatment** या **View Details** से खोलें।

1. **Visit Details** में **Visit Date** और **Field Officer / Visibility** देखें, फिर **Current Plant Stage** और **Plant Condition** चुनें।
2. **Treatment Applied** में **Product Used** चुनें, **Dosage** भरें, **Application Method** चुनें, **Treatment Date** भरें, **Stage of Application** चुनें और **Remarks** लिखें।
3. **Observations** में **Physical Parameters** और **Yield Parameters** लिखें, जो **Quick Observations** सही हों उन पर टैप करें, और **Observation Notes** लिखें।
4. **Control Images** और **Treated Images** में **Upload Image** दबाएँ और फ़ोटो चुनें। कंट्रोल फ़ोटो में बिना प्रोडक्ट वाले पौधे होते हैं, ट्रीटेड फ़ोटो में प्रोडक्ट डाले हुए पौधे होते हैं।
5. **Problem Observed?** में **Yes** या **No** चुनें। **Yes** होने पर **Problem Category**, **Problem Description**, **Severity**, **Recommendation / Monitoring Action** और **Action Taken** भरें।
6. बाद में पूरा करना हो तो **Save Draft** दबाएँ, या विज़िट पूरी होने पर **Complete Treatment** दबाएँ।

> Careful: **Problem Observed?** शुरू में **Yes** पर होता है। अगर आप **No** रखकर ट्रीटमेंट पूरा करते हैं, तो पूरा डेमो पूरा हो जाता है और फिर कोई ट्रीटमेंट नहीं जोड़ा जा सकता।

> Tip: दूसरे ट्रीटमेंट से, ऊपर **Previous Treatment Reference** बॉक्स में पिछली विज़िट दिखती है, ताकि आप तुलना कर सकें।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Visit Date** और **Treatment Date** | अपने आप आज की तारीख होती है। ट्रीटमेंट की तारीख विज़िट की तारीख से पहले की नहीं हो सकती। पूरा करने के लिए ज़रूरी है। |
| **Current Plant Stage** | आज दिखी फ़सल की अवस्था। पूरा करने के लिए ज़रूरी है। |
| **Product Used** | जो प्रोडक्ट डाला गया। पूरा करने के लिए ज़रूरी है। |
| **Dosage** | जैसे 2 ml/L। |
| **Control Images**, **Treated Images** | JPG या PNG, हर एक 10 MB या उससे कम, एक बार में 20 तक। |

## आम दिक्कतें

- **"Enter Visit Date, Treatment Date, Current Plant Stage and Product Used before completing the treatment."** (ट्रीटमेंट पूरा करने से पहले Visit Date, Treatment Date, Current Plant Stage और Product Used भरें) ये चारों फ़ील्ड भरें, फिर से **Complete Treatment** दबाएँ।
- **"Treatment date cannot precede visit date."** (ट्रीटमेंट की तारीख विज़िट की तारीख से पहले की नहीं हो सकती) **Treatment Date** को विज़िट की तारीख या उसके बाद की करें।
- **"Only JPG and PNG images are allowed."** (सिर्फ़ JPG और PNG फ़ोटो चलती हैं) या **"Each image must be 10MB or less."** (हर फ़ोटो 10MB या उससे कम होनी चाहिए) दूसरी फ़ोटो चुनें या उन्हें छोटा करें।
- **"This treatment has already been completed."** (यह ट्रीटमेंट पहले ही पूरा हो चुका है) पूरा हुआ ट्रीटमेंट सिर्फ़ देखा जा सकता है।
:::

::: section compare-treatments
title: ट्रीटमेंट की तुलना करें और फ़ॉलो-अप करें
routes: /TreatmentDetails, /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 393196d831ee40ab
confidence: ok

**यह स्क्रीन किस काम की है**
जब कोई दिक्कत मिलती है, तो डेमो खुला रहता है और आप फिर से खेत पर जाते हैं। हर नए ट्रीटमेंट में पिछला ट्रीटमेंट दिखता है, ताकि आप देख सकें कि फ़सल सुधरी या नहीं।

1. **Demo Details** पर **Start Next Treatment** दबाएँ। अगले नंबर का ट्रीटमेंट खुलता है।
2. **Previous Treatment Reference** बॉक्स पढ़ें: **Visit Date**, **Current Plant Stage**, **Dosage Applied**, **Method**, **Field Observation** और **Yield Parameters**।
3. पिछले ट्रीटमेंट को उसकी फ़ोटो के साथ खोलने के लिए **View Full Details** दबाएँ।
4. आज की विज़िट और फ़ोटो उसी तरह दर्ज करें, और जो बदला उसे **Observation Notes** में लिखें।
5. **Treatment Journey** में सारे ट्रीटमेंट देखने के लिए **Back to Demo Details** दबाएँ।

## आम दिक्कतें

- **Previous Treatment Reference बॉक्स नहीं है।** यह सिर्फ़ दूसरे ट्रीटमेंट से आगे दिखता है।
- **मुझे पुराना ट्रीटमेंट बदलना है।** **Completed** ट्रीटमेंट बदला नहीं जा सकता। सुधार अगले ट्रीटमेंट में लिखें।
:::

::: section sample-screens
title: ट्रीटमेंट की सैंपल स्क्रीन
routes: /TreatmentDemoDetails, /Treatment01, /Treatment02
for: 
roles: 
designations: 
minutes: 1
source-hash: 13af1fa77b22fd32
confidence: ok

**यह स्क्रीन किस काम की है**
**Treatment Demo Details**, **Treatment 01** और **Treatment 02** मेन्यू आइटम तय उदाहरण वाली सैंपल स्क्रीन खोलते हैं। इनसे दिखता है कि डेमो कैसा दिखता है। वहाँ आप जो भी टाइप करते हैं, वह सेव नहीं होता।

1. **Treatment Demo Details** में तीन ट्रीटमेंट की **Treatment Journey** के साथ **Demo Details** स्क्रीन का उदाहरण दिखता है।
2. **Treatment 01** में पूरे हुए ट्रीटमेंट का उदाहरण दिखता है, जिसमें **Visit Details**, **Treatment Applied**, **Observations**, फ़ोटो और **Problem Identification & Recommendation** हैं।
3. **Treatment 02** में दूसरे ट्रीटमेंट का उदाहरण दिखता है, ऊपर **Previous Treatment Reference - Treatment 01** बॉक्स के साथ।
4. असली काम दर्ज करने के लिए **Demo Documentation** खोलें और अपना डेमो चुनें।

> Careful: इन सैंपल स्क्रीन के बटन कुछ भी सेव नहीं करते।

## आम दिक्कतें

- **मेरी भरी हुई जानकारी गायब हो गई।** ये सैंपल स्क्रीन हैं। असली डेमो दर्ज करने के लिए **Demo Documentation** इस्तेमाल करें।
:::
