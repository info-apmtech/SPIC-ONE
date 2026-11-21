---
module: publicity
title: प्रचार सामग्री ट्रैकिंग
icon: bi-megaphone
order: 40
language: hi
category: मार्केटिंग
note: नया रूप, साथ में रिक्वेस्ट का सफ़र, स्टेटस और गाइडेड टूर
meta-source-hash: 2abd562a1fb0baf2
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b68fc648b74d721b
confidence: ok

अपने HQ के लिए प्रचार सामग्री (पोस्टर, बैनर और ऐसी ही चीज़ें) माँगें, उसकी मंज़ूरी लें, सामग्री लें और पक्का करें कि वह पहुँच गई। हर स्टेप रिकॉर्ड होता है, इसलिए सबको दिखता है कि रिक्वेस्ट अभी कहाँ है।

- start | icon: bi-pencil-square | color: #0b6b3a
  title: RMDO इसे शुरू करता है
  text: सामग्री, प्रोडक्ट और मात्रा के साथ रिक्वेस्ट डालें।
- approve | icon: bi-shield-check | color: #d97706
  title: पाँच मंज़ूरियाँ
  text: RM, SMD, SMM, ADMIN-MD CS और DGM-MD CS बारी-बारी से इसे जाँचते हैं।
- close | icon: bi-box-seam | color: #7c3aed
  title: RMDO इसे बंद करता है
  text: जो सामग्री पहुँची, उसकी पुष्टि करें, कम या खराब पीस भी बताएँ।
:::

::: section flow
title: रिक्वेस्ट कैसे आगे बढ़ती है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 922a2e0da6a7ddbc
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- rmdo | type: lane | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  sub: आपका HQ
- approvers | type: lane | color: #0d9488 | roles: RM, SMD, SMM
  title: मंज़ूरी देने वाले
  sub: RM · SMD · SMM
- mdcs | type: lane | color: #ea580c | roles: MDAdmin, MDHOD | designations: ADMIN-MD CS, ADMIN MD CS, DGM-MD CS, DGM MD CS
  title: MD CS
  sub: ADMIN-MD CS · DGM-MD CS
- vendor | type: lane | color: #64748b
  title: वेंडर
  sub: कूरियर
- raise | lane: rmdo | col: 1 | icon: bi-pencil-square | color: #64748b | next: approve | screen: raise-request
  title: रिक्वेस्ट भरें और सबमिट करें
  status: Draft
  text: रिक्वेस्ट फ़ॉर्म भरें और **Submit Request** दबाएँ। तब तक यह एक ड्राफ़्ट है, जो सिर्फ़ आपको दिखता है।
- approve | lane: approvers | col: 2 | icon: bi-hourglass-split | color: #d97706 | next: final | return: raise | screen: review-approve
  title: जाँच और मंज़ूरी
  status: Pending Approval
  text: पहले RM, फिर SMD, फिर SMM रिक्वेस्ट जाँचते हैं। हर कोई इसे बदल सकता है, मंज़ूर करके आगे भेज सकता है, RMDO को लौटा सकता है, या रिजेक्ट कर सकता है।
- final | lane: mdcs | col: 3 | icon: bi-patch-check | color: #2563eb | next: dispatch | screen: review-approve
  title: आख़िरी मंज़ूरी
  status: Final Approved
  text: ADMIN-MD CS इसे मंज़ूर करके आगे भेजता है, फिर DGM-MD CS आख़िरी मंज़ूरी देता है।
- dispatch | lane: mdcs | col: 4 | icon: bi-truck | color: #7c3aed | next: ship | screen: create-dispatch
  title: डिस्पैच बनाएँ
  status: In Dispatch
  text: ADMIN-MD CS या DGM-MD CS कूरियर की जानकारी के साथ वेंडर से हुए डिस्पैच को दर्ज करता है।
- ship | lane: vendor | col: 5 | icon: bi-box-seam | color: #0d9488 | next: ack | screen: update-delivery
  title: HQ तक भेजना
  status: Delivered
  text: सामग्री वेंडर के यहाँ से चलती है। MD CS इसे पहले **In Transit** और फिर **Delivered** मार्क करता है।
- ack | lane: rmdo | col: 6 | icon: bi-clipboard-check | color: #16a34a | screen: acknowledge-delivery
  title: पुष्टि करें
  status: Completed
  text: RMDO सामग्री गिनता है और **Acknowledgement** सबमिट करता है। अगर सब सही है, तो रिक्वेस्ट पूरी हो जाती है।
- exception | lane: mdcs | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: ack | to: dispatch | screen: resolve-exception
  title: कमी या नुकसान सुलझाएँ
  status: Exception Raised
  text: अगर पीस कम हैं या खराब हैं, तो MD CS बाकी सामग्री वेंडर से भिजवाता है, या किसी दूसरे HQ का ज़्यादा स्टॉक भिजवाता है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 06524fbf96639e47
confidence: ok

- draft | color: #64748b
  label: Draft
  text: सेव है पर भेजा नहीं गया। सिर्फ़ आपको दिखता है।
- pending | color: #d97706
  label: Pending Approval
  text: अभी किसी मंज़ूरी देने वाले के पास है।
- pending-final | color: #d97706
  label: Pending Final Approval
  text: आख़िरी मंज़ूरी के लिए DGM-MD CS के पास है।
- returned | color: #ea580c
  label: Returned
  text: RMDO को लौटा दी गई है। इसे बदलें और फिर से सबमिट करें।
- final | color: #2563eb
  label: Final Approved
  text: मंज़ूर हो गई। डिस्पैच का इंतज़ार है।
- dispatch | color: #7c3aed
  label: In Dispatch
  text: वेंडर ने सामग्री भेज दी है।
- delivered | color: #0d9488
  label: Delivered
  text: सामग्री HQ पहुँच गई है।
- ack-pending | color: #0891b2
  label: Acknowledgement Pending
  text: RMDO के यह बताने का इंतज़ार है कि क्या पहुँचा।
- completed | color: #16a34a
  label: Completed
  text: सब कुछ अच्छी हालत में पहुँच गया।
- exception | color: #dc2626
  label: Exception Raised
  text: पीस कम थे या खराब थे। MD CS इसे सुलझा रहा है।
- transfer | color: #7c3aed
  label: Transfer Order Received
  text: आपके HQ से कहा गया है कि ज़्यादा स्टॉक किसी दूसरे HQ को भेजें।
- rejected | color: #475569
  label: Rejected
  text: मंज़ूरी देने वाले ने इसे बंद कर दिया। ज़रूरत हो तो नई रिक्वेस्ट डालें।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 3570dd53fa8c80d1
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- rmdo | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  - रिक्वेस्ट डालना
  - डिलीवरी की पुष्टि करना
  - स्टॉक ट्रांसफ़र के लिए कूरियर का सबूत भेजना
- approvers | color: #0d9488 | roles: RM, SMD, SMM
  title: RM, SMD, SMM
  - अपने इलाके की रिक्वेस्ट जाँचना
  - मंज़ूर करना, लौटाना या रिजेक्ट करना
- admin | color: #ea580c | roles: MDAdmin | designations: ADMIN-MD CS, ADMIN MD CS
  title: ADMIN-MD CS
  - मंज़ूर करके आगे भेजना
  - डिस्पैच बनाना और डिलीवरी अपडेट करना
  - कमी और नुकसान सुलझाना
- dgm | color: #7c3aed | roles: MDHOD | designations: DGM-MD CS, DGM MD CS
  title: DGM-MD CS
  - आख़िरी मंज़ूरी देना
  - डिस्पैच बनाना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: a2321112e4f4d096
confidence: ok

- no-pencil | popular: true
  q: मैंने अपनी रिक्वेस्ट सबमिट कर दी और अब पेंसिल आइकन नहीं दिख रहा
  a: सबमिट करने के बाद सिर्फ़ मंज़ूरी देने वाला ही रिक्वेस्ट आपको लौटा सकता है। जब उस पर **Returned** दिखेगा, तब पेंसिल फिर से आ जाएगी।
- damaged | popular: true
  q: कुछ पीस खराब या कम पहुँचे। मैं क्या करूँ?
  a: पुष्टि करते समय मिली हुई और खराब मात्रा भरें और फिर भी सबमिट करें। एक एक्सेप्शन ADMIN-MD CS के पास जाता है, जो कम पड़े पीस भेजता है।
- who-has-it
  q: मेरी रिक्वेस्ट अभी किसके पास है?
  a: रिक्वेस्ट खोलें। ऊपर **Current Stage** और **Pending With** दिखते हैं, और **Approval History** में हर कार्रवाई दिखती है।
- no-rows
  q: मुझे "No requests found" दिख रहा है
  a: हो सकता है आपके फ़िल्टर बहुत कड़े हों। **Filters** खोलें और **Clear All** दबाएँ।
:::

::: section requester-list
title: मेरी रिक्वेस्ट देखें और संभालें
routes: /PublicityRequest
for: 
roles: 
designations: 
screenshot: requester-list.png
hotspots: 1 @ 2.2,12.2,95.5,33.6 | 2 @ 61.6,81.1,24.3,5.1 | 3 @ 2.2,48.2,95.5,27.9 | 4 @ 4.2,87.9,91.6,12.1 | 5 @ 86.9,81.2,8.9,4.8 | 6 @ 80.7,1.9,17,4.8
screenshot-phone: requester-list.phone.jpg
hotspots-phone: 1 @ 3.1,9.9,93.8,35.4 | 2 @ 6.9,86.1,59.1,3 | 3 @ 3.1,46.3,93.8,35.1 | 4 @ 6.9,90,86.2,10 | 5 @ 68.5,86.2,24.5,2.8 | 6 @ 17.4,5.4,46.7,2.8
tour: 1 > [data-help=pub-cards] | 2 > [data-help=pub-search] | 3 > [data-help=pub-filters] | 4 > [data-help=pub-table] | 5 > [data-help=pub-export] | 6 > [data-help=pub-new]
minutes: 2
source-hash: 08092b10939e59ef
confidence: ok

**यह स्क्रीन किस काम की है**
**My Publicity Material Request** स्क्रीन पर आपके HQ की हर रिक्वेस्ट दिखती है: ऊपर गिनती और नीचे टेबल।

1. ऊपर के कार्ड देखें, जैसे **Draft**, **Pending Approval** और **Completed**। सिर्फ़ वही रिक्वेस्ट देखने के लिए किसी कार्ड पर टैप करें। सब देखने के लिए उस पर फिर से टैप करें।
2. रिक्वेस्ट जल्दी ढूँढने के लिए सर्च बॉक्स में इंडेंट नंबर, प्रोडक्ट या सामग्री का नाम टाइप करें।
3. **Filters** खोलकर **Date Range**, **Product**, **Publicity Material** या **Status** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
4. **Action** कॉलम में आँख वाला आइकन रिक्वेस्ट खोलता है। पेंसिल से **Draft** या **Returned** रिक्वेस्ट बदली जाती है। डस्टबिन **Draft** को मिटाता है।
5. लिस्ट को Excel फ़ाइल में डाउनलोड करने के लिए **Export** दबाएँ।
6. नई रिक्वेस्ट शुरू करने के लिए **Create New Request** दबाएँ।

> Careful: ड्राफ़्ट मिटाने के बाद उसे वापस नहीं लाया जा सकता।

## आम दिक्कतें

- **मुझे "No requests found" दिख रहा है।** हो सकता है आपके फ़िल्टर बहुत कड़े हों। **Clear All** दबाएँ।
- **पेंसिल आइकन नहीं है।** सबमिट करने के बाद आप रिक्वेस्ट नहीं बदल सकते, जब तक मंज़ूरी देने वाला उसे आपको लौटा न दे।
- **Export बटन ग्रे है।** लिस्ट में एक्सपोर्ट करने के लिए कोई लाइन नहीं है।
:::

::: section raise-request
title: प्रचार सामग्री की रिक्वेस्ट डालें
routes: /PublicityRequest/new, /PublicityRequest/edit/{id}
for: 
roles: 
designations: 
screenshot: raise-request.png
hotspots: 1 @ 4.2,20.2,21.5,8.5 | 2 @ 50.9,20.2,21.5,8.5 | 3 @ 4.2,30.4,91.6,20.7 | 4 @ 2.2,73.6,95.5,19.1 | 5 @ 2.2,95,95.5,4.8
screenshot-phone: raise-request.phone.jpg
hotspots-phone: 1 @ 6.9,15.5,86.2,4.9 | 2 @ 6.9,27.4,86.2,4.9 | 3 @ 6.9,39.2,86.2,14.7 | 4 @ 3.1,66.1,93.8,23.6 | 5 @ 0,87.5,100,8.4
tour: 1 > [data-help=pub-material] | 2 > [data-help=pub-qty] | 3 > [data-help=pub-design] | 4 > [data-help=pub-location] | 5 > [data-help=pub-submit]
minutes: 3
source-hash: 74e6a60af7702e79
confidence: ok

**यह स्क्रीन किस काम की है**
अपने HQ के लिए प्रचार सामग्री माँगने के लिए **Create Publicity Material Request** स्क्रीन का इस्तेमाल करें। आप इसे ड्राफ़्ट में सेव करके बाद में भेज सकते हैं।

1. **Request Details** में **Type of Publicity Material** और **Product** चुनें।
2. आपको जितनी **Quantity** चाहिए, वह भरें। **Requested By** में आपका नाम पहले से भरा होता है।
3. अगर आपके पास डिज़ाइन है, तो फ़ाइल को अपलोड बॉक्स में डालें या **or click to browse** पर टैप करें। ज़रूरत हो तो **Remarks** लिखें।
4. **Location** में **State**, **Region** और **Headquarter (HQ)** देख लें। ये आपकी प्रोफ़ाइल से भरे जाते हैं।
5. बाद के लिए रखने को **Save as Draft** दबाएँ, या RM को भेजने के लिए **Submit Request** और फिर **Submit** दबाएँ।

> Tip: आप लिस्ट से ड्राफ़्ट पर वापस आकर उसे बाद में पूरा कर सकते हैं।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Type of Publicity Material** | आपको जो चीज़ चाहिए, जैसे पोस्टर या बैनर। ज़रूरी है। |
| **Product** | जिस प्रोडक्ट के लिए सामग्री है। आप टाइप करके खोज सकते हैं। ज़रूरी है। |
| **Quantity** | कितने पीस चाहिए। ज़रूरी है। |
| **Design file** | एक फ़ाइल, PDF, JPG, PNG, DOC या DOCX, 10 MB तक। |
| **State**, **Region**, **Headquarter (HQ)** | सामग्री कहाँ पहुँचानी है। ज़रूरी है। |

## आम दिक्कतें

- **"Select the state, region and headquarter."** (राज्य, रीजन और HQ चुनें) पहले State चुनें, फिर Region, फिर HQ।
- **"is larger than 10 MB"** (फ़ाइल 10 MB से बड़ी है) फ़ाइल छोटी करें या नई फ़ोटो लें, फिर से अपलोड करें।
- **सबमिट करने के बाद मैं बदलाव नहीं कर पा रहा।** यह सामान्य है। सिर्फ़ लौटाई गई रिक्वेस्ट ही फिर से बदली जा सकती है।
:::

::: section track-request
title: मेरी रिक्वेस्ट ट्रैक करें
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
screenshot: track-request.png
screenshot-phone: track-request.phone.jpg
minutes: 2
source-hash: 533996d4d473e58d
confidence: ok

**यह स्क्रीन किस काम की है**
**My Publicity Material Request Details** स्क्रीन पर एक रिक्वेस्ट की पूरी जानकारी दिखती है: वह अभी कहाँ है, किसने मंज़ूर किया, डिस्पैच और दस्तावेज़।

1. लिस्ट में आँख वाले आइकन से रिक्वेस्ट खोलें।
2. ऊपर **Current Stage**, **Status** और यह देखें कि रिक्वेस्ट अभी किसके पास है (**Pending With**)।
3. **Request Details & History** पर **Request Timeline** और **Approval History** टेबल देखें, जिसमें हर कार्रवाई और रिमार्क है।
4. हर डिस्पैच देखने के लिए **Tracking & Delivery** खोलें: वेंडर, ट्रांसपोर्टर, ट्रैकिंग नंबर और डिलीवरी स्टेटस।
5. सामग्री पहुँचने पर उसके मिलने की पुष्टि करने के लिए **Acknowledgement** खोलें।
6. अपलोड की गई सारी फ़ाइलें देखने के लिए **Related Documents** खोलें।
7. रिक्वेस्ट की PDF के लिए **Download Report** दबाएँ, या प्रिंट करने के लिए **Print** दबाएँ।
8. अगर रिक्वेस्ट **Returned** हुई है, तो **Edit & Resubmit** दबाएँ, उसे ठीक करें और फिर से सबमिट करें।

## आम दिक्कतें

- **Tracking & Delivery पर "Not dispatched yet" दिख रहा है।** सामग्री की मंज़ूरी अभी चल रही है या डिस्पैच अभी नहीं बना है। **Current Stage** देखें।
- **Status में "Rejected" दिख रहा है।** रिक्वेस्ट बंद हो गई है। **Approval History** में कारण पढ़ें और ज़रूरत हो तो नई रिक्वेस्ट डालें।
- **मुझे Edit & Resubmit नहीं दिख रहा।** यह तभी दिखता है जब रिक्वेस्ट आपको लौटाई जाती है।
:::

::: section acknowledge-delivery
title: मिली हुई सामग्री की पुष्टि करें
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
minutes: 3
source-hash: 2e6ce69978f16d26
confidence: ok

**यह स्क्रीन किस काम की है**
जब सामग्री आपके HQ पहुँचे, तो आपको बताना होगा कि कितने पीस मिले और उनकी हालत कैसी है। इससे रिक्वेस्ट बंद हो जाती है, या ADMIN-MD CS को समस्या की खबर जाती है।

1. रिक्वेस्ट खोलें और **Tracking & Delivery** पर जाएँ।
2. **Awaiting Acknowledgement** बॉक्स में **Acknowledge** दबाएँ। इससे **Acknowledgement** टैब खुलता है।
3. **Dispatch Summary** (डिस्पैच की मात्रा और तारीख) देखें।
4. **Quantity Received** भरें।
5. **In Good Condition** या **In Damaged Condition** चुनें। अगर सामग्री खराब है, तो **Damaged Quantity** भरें।
6. **Acknowledged By** में चुनें कि सामग्री किसने जाँची।
7. **Remarks** लिखें और सामग्री की फ़ोटो अपलोड करें।
8. अगर **Exception Details** बॉक्स दिखे, तो उसे देखें। उसमें कोई भी कमी, ज़्यादा माल या नुकसान दिखता है।
9. बाद में पूरा करने के लिए **Save as Draft** दबाएँ, या **Submit Acknowledgement** दबाएँ, फिर **Submit**।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Quantity Received** | असल में गिने गए पीस। ज़रूरी है। |
| **Material Condition** | अच्छी या खराब। ज़रूरी है। |
| **Damaged Quantity** | 1 से लेकर मिली हुई मात्रा तक। |
| **Acknowledged By** | वह व्यक्ति जिसने सामग्री ली। ज़रूरी है। |
| **Photos** | मिली हुई सामग्री की तस्वीरें, हर एक 10 MB तक। |

## आम दिक्कतें

- **"Damaged quantity must be between 1 and the received quantity."** (खराब मात्रा 1 और मिली हुई मात्रा के बीच होनी चाहिए) नंबर फिर से जाँचें।
- **कमी या नुकसान मिला।** फिर भी सबमिट करें। एक एक्सेप्शन ADMIN-MD CS के पास जाता है, जो कम पड़े पीस भेजेंगे।
- **Acknowledge बटन नहीं है।** सामग्री अभी डिस्पैच नहीं हुई है, या उसकी पुष्टि पहले ही हो चुकी है।
:::

::: section transfer-proof
title: ज़्यादा स्टॉक दूसरे HQ को भेजें
routes: /PublicityRequest/{id}/transfer/{dispatchId}, /PublicityRequest/done
for: 
roles: 
designations: 
minutes: 3
source-hash: e7a33ab19f8314b5
confidence: ok

**यह स्क्रीन किस काम की है**
अगर आपके HQ को ज़रूरत से ज़्यादा सामग्री मिली है, तो ADMIN-MD CS आपसे उसमें से कुछ सामग्री किसी ऐसे दूसरे HQ को भेजने को कह सकता है जहाँ कमी है। आप इसे कूरियर से भेजते हैं और कूरियर की रसीद यहाँ अपलोड करते हैं।

1. **Notifications** खोलें और स्टॉक ट्रांसफ़र ऑर्डर पर टैप करें, फिर **View Transfer** दबाएँ। आप अपनी रिक्वेस्ट खोलकर, **Tracking & Delivery** पर जाकर, **Stock Transfer Order** कार्ड पर **Create Transfer** भी दबा सकते हैं।
2. **Read Information (Read Only)** वाला हिस्सा देखें: **Destination HQ** और **Transfer Quantity**।
3. सामग्री पैक करें और कूरियर से भेजें।
4. **Upload Courier / Dispatch Proof** में कूरियर रसीद की साफ़ फ़ोटो अपलोड करें।
5. ऐप रसीद पढ़कर **Courier / Transporter**, **Tracking No. / AWB** और **Dispatch Date** भर देता है। इन्हें जाँचें और ज़रूरत हो तो ठीक करें।
6. अगर आपके पास हों, तो **Vehicle No** और **Remarks (Optional)** भरें।
7. **Submit Proof to Admin** दबाएँ।
8. **Dispatch Proof Submitted Successfully!** स्क्रीन खुलती है। **Go to Tracking & Delivery** या **Back to List** दबाएँ।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Courier / Transporter** | कूरियर कंपनी का नाम। ज़रूरी है। |
| **Tracking No. / AWB** | कूरियर रसीद पर लिखा नंबर। ज़रूरी है। |
| **Dispatch Date** | जिस दिन आपने भेजा। ज़रूरी है। |

## आम दिक्कतें

- **"Upload the courier / dispatch proof."** (कूरियर / डिस्पैच का सबूत अपलोड करें) सबमिट करने से पहले रसीद अपलोड करें।
- **जानकारी अपने-आप नहीं भरी।** हो सकता है फ़ोटो धुँधली हो या फ़ाइल PDF हो। जानकारी खुद टाइप करें।
- **"Transfer order not found".** (ट्रांसफ़र ऑर्डर नहीं मिला) सबूत पहले ही सबमिट हो चुका है। उसे बदलने के लिए **Tracking & Delivery** टैब पर **Update Proof** इस्तेमाल करें।
:::

::: section requester-notifications
title: मेरे प्रचार सामग्री नोटिफ़िकेशन
routes: /PublicityRequest/notifications
for: 
roles: 
designations: 
screenshot: requester-notifications.png
hotspots: 1 @ 4.2,14.7,91.6,7 | 2 @ 4.2,23.6,91.6,76.4 | 3 @ 4.2,23.6,91.6,76.4 | 4 @ 84.5,1.9,13.3,4.8 | 5 @ 68.7,1.9,15.1,4.8
screenshot-phone: requester-notifications.phone.jpg
hotspots-phone: 1 @ 6.9,11,86.2,3.7 | 2 @ 6.9,15.8,86.2,84.2 | 3 @ 6.9,15.8,86.2,84.2 | 4 @ 46.4,5.4,36.4,2.8 | 5 @ 3.1,5.4,41.3,2.8
tour: 1 > [data-help=pub-notif-tabs] | 2 > [data-help=pub-notif-list] | 3 > [data-help=pub-notif-list] | 4 > [data-help=pub-notif-readall] | 5 > [data-help=pub-notif-back]
minutes: 1
source-hash: 4cd27f470cab35ac
confidence: ok

**यह स्क्रीन किस काम की है**
**Notifications** स्क्रीन पर आपकी रिक्वेस्ट से जुड़े अलर्ट दिखते हैं: लौटाई गई रिक्वेस्ट, डिस्पैच, एक्सेप्शन और स्टॉक ट्रांसफ़र ऑर्डर। इसे लिस्ट पेज पर **Notifications** दबाकर खोलें।

1. एक टैब चुनें: **All**, **Stock Transfer**, **Exception** या **Dispatch & Approvals**।
2. बिना पढ़े आइटम पर एक बिंदी होती है। किसी आइटम की जानकारी खोलने के लिए उस पर टैप करें।
3. ट्रांसफ़र ऑर्डर में कूरियर का सबूत अपलोड करने के लिए **View Transfer** दबाएँ। रिक्वेस्ट खोलने के लिए **View Details** दबाएँ।
4. एक आइटम के लिए **Mark As Read** दबाएँ, या सबके लिए **Mark All As Read**।
5. वापस जाने के लिए **Back to List Page** दबाएँ।

## आम दिक्कतें

- **"No notifications".** (कोई नोटिफ़िकेशन नहीं) आपकी रिक्वेस्ट पर अभी कुछ नया नहीं हुआ है।
- **Mark All As Read ग्रे है।** सारे आइटम पहले ही पढ़े जा चुके हैं।
:::

::: section reviewer-list
title: मेरी जाँच के लिए आई रिक्वेस्ट देखें
routes: /PublicityReview
for: 
roles: 
designations: 
screenshot: reviewer-list.png
screenshot-phone: reviewer-list.phone.jpg
minutes: 2
source-hash: 7eaa380f878c5c45
confidence: ok

**यह स्क्रीन किस काम की है**
मंज़ूरी देने वाले और MD CS स्टाफ़ इस लिस्ट में अपने इलाके की रिक्वेस्ट देखते हैं। इसका टाइटल **Submitted Requests** है (RM के लिए यह **My Publicity Material Request** है)।

1. ऊपर के कार्ड देखें। RM को **Pending for My Approval**, **Approved by RM**, **Dispatch & Delivery**, **Completed** और **Returned** दिखते हैं। बाकी लोगों को **Total Submitted**, **Pending Review**, **Dispatch Pending**, **Exceptions** और **Completed** जैसे कार्ड दिखते हैं।
2. अगर लाल रंग का **New Request for Review** बॉक्स दिखे, तो **View New Requests** दबाएँ। RM, SMD, SMM और ADMIN-MD CS के लिए इससे कंसॉलिडेशन स्क्रीन खुलती है।
3. **Filters** खोलकर **Date Range**, **Product**, **Publicity Material**, **State**, **Region**, **HQ**, **Current Stage** या **Status** चुनें, फिर **Apply Filters** दबाएँ।
4. जो लाइनें आपका इंतज़ार कर रही हैं, उनके **Status** कॉलम में **Pending** दिखता है। जिन्हें आप आगे भेज चुके हैं, उनमें **Approved** दिखता है।
5. रिक्वेस्ट खोलने के लिए आँख वाले आइकन पर टैप करें।
6. लिस्ट को Excel में डाउनलोड करने के लिए **Export** दबाएँ।
7. आपके लेवल पर इंतज़ार कर रही सारी रिक्वेस्ट रीजन के हिसाब से देखने के लिए **Consolidation** दबाएँ।

## आम दिक्कतें

- **मुझे State या Region फ़िल्टर नहीं दिख रहे।** RM, SMD और SMM को सिर्फ़ अपना इलाका दिखता है, इसलिए कुछ फ़िल्टर छिपे रहते हैं।
- **लिस्ट खाली है।** Filters में **Clear All** दबाएँ। हो सकता है अभी तक कोई रिक्वेस्ट आप तक न पहुँची हो।
:::

::: section review-approve
title: रिक्वेस्ट जाँचें और मंज़ूर करें
routes: /PublicityReview/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: 3d13e1d79363e813
confidence: ok

**यह स्क्रीन किस काम की है**
रिक्वेस्ट खोलकर उसे जाँचें, और फिर मंज़ूर करें, बदलें या रिजेक्ट करें। बटन तभी दिखते हैं जब रिक्वेस्ट आपके पास इंतज़ार कर रही हो।

1. लिस्ट या कंसॉलिडेशन स्क्रीन से रिक्वेस्ट खोलें।
2. ऊपर की पट्टी, **Request Timeline** और **Approval History** देखें।
3. **Additional Remarks (if any)** में कोई भी नोट लिखें।
4. पहले सामग्री, मात्रा या जगह बदलनी हो, तो **Edit** दबाएँ।
5. मंज़ूर करने के लिए अगले लेवल वाला **Approve & Forward to** दबाएँ (जैसे **Approve & Forward to SMD**), फिर **Approve & Forward** से पुष्टि करें।
6. मना करने के लिए **Reject** दबाएँ, **Reason for rejection** लिखें, और फिर से **Reject** दबाएँ।

> Careful: Reject करने से रिक्वेस्ट हमेशा के लिए बंद हो जाती है। RMDO को नई रिक्वेस्ट डालनी होगी।

## मंज़ूरी का क्रम

- RM, SMD को भेजता है; SMD, SMM को; SMM, ADMIN - MD CS को; ADMIN-MD CS, DGM - MD CS को।
- DGM-MD CS आख़िरी मंज़ूरी देता है। इस स्टेज पर **Edit** नहीं होता।

## आम दिक्कतें

- **Approve बटन नहीं है, टाइटल में "Request Details" लिखा है।** रिक्वेस्ट अभी आपके लेवल पर नहीं है। आप सिर्फ़ देख सकते हैं, **Print** कर सकते हैं या **Download Report** कर सकते हैं।
- **"Enter the reason for rejection."** (रिजेक्ट करने का कारण लिखें) कारण लिखना ज़रूरी है।
:::

::: section reviewer-edit
title: आगे भेजने से पहले रिक्वेस्ट बदलें
routes: /PublicityReview/edit/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: e76c7e9756536fde
confidence: ok

**यह स्क्रीन किस काम की है**
**Edit Publicity Material Request** स्क्रीन पर वह मंज़ूरी देने वाला, जिसकी अभी बारी है, रिक्वेस्ट ठीक कर सकता है। बदलाव सेव करने से रिक्वेस्ट मंज़ूर होकर आगे भी चली जाती है।

1. रिक्वेस्ट पर **Edit** दबाएँ। कंसॉलिडेशन स्क्रीन पर पेंसिल आइकन पर टैप करें।
2. **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** या **Remarks** बदलें।
3. ज़रूरत हो तो डिज़ाइन फ़ाइल जोड़ें या हटाएँ।
4. **State**, **Region** और **Headquarter (HQ)** देख लें।
5. अगले लेवल वाला **Save & Forward to** दबाएँ (जैसे **Save & Forward to SMM**)।
6. बदलाव सेव हो जाता है, **Approval History** में लिखा जाता है, और रिक्वेस्ट अगले लेवल पर चली जाती है।

## आम दिक्कतें

- **एडिट पेज की जगह डिटेल्स पेज खुल जाता है।** रिक्वेस्ट अभी आपके पास इंतज़ार नहीं कर रही।
- **मैं सिर्फ़ सेव करना चाहता था, आगे नहीं भेजना था।** यहाँ सेव करने से रिक्वेस्ट हमेशा आगे जाती है। अगर अभी आगे नहीं भेजना है, तो **Back to List** इस्तेमाल करें।
:::

::: section consolidation
title: कई रिक्वेस्ट एक साथ मंज़ूर करें
routes: /PublicityReview/consolidation
for: 
roles: 
designations: 
screenshot: consolidation.png
screenshot-phone: consolidation.phone.jpg
minutes: 3
source-hash: abde9049ef5f5976
confidence: ok

**यह स्क्रीन किस काम की है**
कंसॉलिडेशन स्क्रीन पर आपके लेवल पर इंतज़ार कर रही हर रिक्वेस्ट रीजन के हिसाब से दिखती है, ताकि आप सबको एक ही बार में मंज़ूर कर सकें। RM को अपना रीजन दिखता है, SMD और SMM को राज्य, और ADMIN-MD CS को पूरा भारत। DGM-MD CS यह स्क्रीन इस्तेमाल नहीं करता।

1. लिस्ट पेज पर **Consolidation** दबाएँ।
2. कार्ड देखें: **Submission ID**, **Region** या **State**, **Total Region**, **Total Requests** और **Total Requirement**।
3. टेबल में किसी रीजन को खोलकर उसकी HQ रिक्वेस्ट देखने के लिए **Action** में तीर पर टैप करें।
4. रिक्वेस्ट देखने के लिए आँख वाले आइकन पर, या बदलने के लिए पेंसिल आइकन पर टैप करें।
5. नीचे वाला बॉक्स पढ़ें। उसमें दिखता है कि कितने रीजन आप तक पहुँच चुके हैं।
6. जब सारे रीजन आप तक पहुँच जाएँ, तो अगले लेवल वाला **Approve & Submit to** दबाएँ, फिर **Approve & Submit** से पुष्टि करें।
7. टेबल डाउनलोड करने के लिए **Export** दबाएँ, या लिस्ट पर वापस जाने के लिए **View Submitted Request** दबाएँ।

## आम दिक्कतें

- **Approve & Submit ग्रे है।** कुछ रीजन अभी पिछले लेवल पर रुके हुए हैं। सारे रीजन आप तक पहुँचने तक इंतज़ार करें।
- **"Nothing waiting for you".** (आपके लिए कुछ बाकी नहीं) अभी कोई रिक्वेस्ट आपके लेवल पर नहीं है।
- **Request Qty और Approved Qty अलग-अलग हैं।** मंज़ूरी देने वाले किसी व्यक्ति ने मात्रा बदली है। रिक्वेस्ट पर **Approval History** देखें।
:::

::: section create-dispatch
title: डिस्पैच बनाएँ
routes: /PublicityReview/{id}/dispatch, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3
source-hash: 039c89aba91e084f
confidence: ok

**यह स्क्रीन किस काम की है**
आख़िरी मंज़ूरी के बाद ADMIN-MD CS या DGM-MD CS **Create Dispatch** स्क्रीन पर वेंडर से हुए डिस्पैच को दर्ज करता है। यही स्क्रीन बदले में भेजे जाने वाले (re-initiation) डिस्पैच के लिए भी इस्तेमाल होती है।

1. **Final Approved** रिक्वेस्ट खोलें और **Create Dispatch** दबाएँ।
2. **Read Information (Read Only)** वाला हिस्सा देखें।
3. **Feed Logistics & Dispatch Data** में **Dispatch Date**, **Dispatch Quantity**, **Vendor** और **Transporter / Courier** भरें।
4. पता हो तो **Vehicle No**, **Tracking No.** और **Expected Delivery Date** भरें।
5. **Responsible SMDO / RM / RMDO / MO** चुनें।
6. **Dispatch / Note Approval Document** अपलोड करें। अगर आपके पास ट्रांसपोर्ट स्लिप या E-way bill हो, तो वह भी जोड़ें।
7. **Delivery Address** चुनें या टाइप करें।
8. **Confirm and Create Dispatch** दबाएँ।
9. **Dispatch Created Successfully!** स्क्रीन खुलती है। **Go to Tracking & Delivery** दबाएँ।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Dispatch Quantity** | अभी भेजे जा रहे पीस। बदले वाले डिस्पैच में यह बाकी बची मात्रा से शुरू होती है। |
| **Vendor** | लिस्ट से चुनें या नया नाम टाइप करें। ज़रूरी है। |
| **Responsible SMDO / RM / RMDO / MO** | वह अधिकारी जो सामग्री लेगा। ज़रूरी है। |
| **Dispatch / Note Approval Document** | पहले डिस्पैच के लिए ज़रूरी है। |
| **Delivery Address** | सेव किए गए पते पर टैप करें या पता टाइप करें। ज़रूरी है। |

## आम दिक्कतें

- **"Upload the dispatch / note approval document."** (डिस्पैच / नोट अप्रूवल डॉक्यूमेंट अपलोड करें) पुष्टि करने से पहले इसे अपलोड करें।
- **Create Dispatch बटन नहीं है।** रिक्वेस्ट अभी **Final Approved** नहीं हुई है।
:::

::: section update-delivery
title: डिस्पैच और डिलीवरी का स्टेटस अपडेट करें
routes: /PublicityReview/{id}
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 2
source-hash: 70a3a9c616c85eba
confidence: ok

**यह स्क्रीन किस काम की है**
डिस्पैच बनने के बाद MD CS उसकी प्रगति अपडेट करता है, ताकि RMDO और मंज़ूरी देने वाले देख सकें कि सामग्री कहाँ है।

1. रिक्वेस्ट खोलें और **Tracking & Delivery** पर जाएँ।
2. डिस्पैच कार्ड ढूँढें (**Dispatch 1**, **Dispatch 2** वगैरह)।
3. जब सामग्री वेंडर के यहाँ से निकले, तो **Mark In Transit** दबाएँ। **Expected Delivery Date** और **Current Location / Hub** भरें, फिर **Save** दबाएँ।
4. जब वह HQ पहुँच जाए, तो **Mark Delivered** दबाएँ, ज़रूरत हो तो **Current Location / Hub** भरें, और **Save** दबाएँ।
5. स्टॉक ट्रांसफ़र में, जब भेजने वाले (source) RMDO ने कूरियर का सबूत अपलोड कर दिया हो, तो **Update Dispatch Details** दबाएँ। **Transporter / Courier** और **Tracking No. (AWB)** जाँचें, फिर **Save** दबाएँ। इसके बाद ट्रांसफ़र पर **Transfer Created** दिखता है।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Transporter / Courier** और **Tracking No. (AWB)** | ट्रांसफ़र की पुष्टि करते समय ज़रूरी हैं। |
| **Expected Delivery Date** | सामग्री कब तक पहुँचनी चाहिए। |
| **Current Location / Hub** | सामग्री अभी कहाँ है, जैसे कोई कूरियर हब। |

## आम दिक्कतें

- **डिस्पैच कार्ड पर कोई बटन नहीं है।** डिस्पैच की पुष्टि पहले ही हो चुकी है, या ट्रांसफ़र का सबूत अभी भेजने वाले RMDO से आना बाकी है ("Courier proof is pending from the source RMDO")।
- **RMDO पुष्टि नहीं कर पा रहा।** पहले सामग्री का डिस्पैच होना ज़रूरी है।
:::

::: section resolve-exception
title: कमी या नुकसान वाला एक्सेप्शन सुलझाएँ
routes: /PublicityReview/{id}, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3
source-hash: 981f49f742015d88
confidence: ok

**यह स्क्रीन किस काम की है**
जब RMDO बताता है कि पीस कम मिले या खराब मिले, तो रिक्वेस्ट पर **Exception Raised** दिखता है। आप तय करते हैं कि कम पड़ी मात्रा कैसे भेजनी है।

1. रिक्वेस्ट खोलें (लिस्ट पर **Exceptions** कार्ड से ऐसी रिक्वेस्ट ढूँढने में मदद मिलती है) और **Tracking & Delivery** पर जाएँ।
2. लाल **Exception Raised** बॉक्स में **Exception Type**, **Raised By** और **Remarks** पढ़ें।
3. **Resolve Exception** दबाएँ। बॉक्स में बाकी बची मात्रा दिखती है।
4. बाकी सामग्री वेंडर से भिजवाने के लिए **Re-initiate dispatch from the vendor** चुनें, **Continue to Dispatch** दबाएँ और बदले की सामग्री के लिए डिस्पैच फ़ॉर्म भरें।
5. किसी दूसरे HQ का ज़्यादा स्टॉक भिजवाने के लिए **Stock transfer from another HQ's excess** चुनें, **Source Request (Excess)** चुनें, **Transfer Quantity** देखें, **Remarks** लिखें, फिर **Send Transfer Order** दबाएँ।
6. ट्रांसफ़र के लिए **Transfer Order Sent Successfully** स्क्रीन खुलती है। भेजने वाले HQ के RMDO को स्टॉक भेजने और कूरियर का सबूत अपलोड करने के लिए कहा जाता है।
7. सबूत आने पर ट्रांसफ़र कार्ड पर **Update Dispatch Details** से उसकी पुष्टि करें।

## आम दिक्कतें

- **स्टॉक ट्रांसफ़र वाला विकल्प ग्रे है ("no excess available")।** किसी दूसरे HQ के पास ज़्यादा स्टॉक नहीं है। वेंडर वाला विकल्प इस्तेमाल करें।
- **"Select the source request and quantity."** (सोर्स रिक्वेस्ट और मात्रा चुनें) एक सोर्स चुनें और शून्य से ज़्यादा मात्रा भरें।
- **Excess only.** (सिर्फ़ ज़्यादा माल) अगर RMDO को ज़्यादा पीस मिले हैं, तो रिक्वेस्ट पूरी (completed) हो जाती है। ज़्यादा स्टॉक बाद में किसी दूसरे HQ के लिए इस्तेमाल हो सकता है।
:::

::: section reviewer-notifications
title: मंज़ूरी देने वालों और MD CS के नोटिफ़िकेशन
routes: /PublicityReview/notifications
for: 
roles: 
designations: 
minutes: 1
source-hash: 3fcc7d3b610404d1
confidence: ok

**यह स्क्रीन किस काम की है**
**Notifications** स्क्रीन पर आपके इलाके की रिक्वेस्ट से जुड़े अलर्ट दिखते हैं: नई मंज़ूरियाँ, डिस्पैच, एक्सेप्शन और स्टॉक ट्रांसफ़र।

1. लिस्ट पेज पर **Notifications** दबाएँ।
2. एक टैब चुनें: **All**, **Stock Transfer**, **Exception** या **Dispatch & Approvals**।
3. बिना पढ़े आइटम पर एक बिंदी होती है। पढ़ने के लिए आइटम पर टैप करें।
4. रिक्वेस्ट को **Tracking & Delivery** टैब पर खोलने के लिए **View Details** दबाएँ।
5. एक आइटम के लिए **Mark As Read** दबाएँ, या सबके लिए **Mark All As Read**।
6. वापस जाने के लिए **Back to List Page** दबाएँ।

## आम दिक्कतें

- **"No notifications".** (कोई नोटिफ़िकेशन नहीं) अभी कुछ नया नहीं हुआ है।
- **एक्सेप्शन का नोटिफ़िकेशन।** रिक्वेस्ट खोलें और **Resolve Exception** इस्तेमाल करें (सिर्फ़ ADMIN-MD CS और DGM-MD CS)।
:::
