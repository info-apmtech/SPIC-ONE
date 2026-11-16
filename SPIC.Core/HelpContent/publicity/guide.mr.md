---
module: publicity
title: प्रचार साहित्य ट्रॅकिंग
icon: bi-megaphone
order: 40
language: mr
category: मार्केटिंग
note: विनंतीचा प्रवाह, स्थिती आणि मार्गदर्शक टूरसह नवीन रूप
meta-source-hash: 2abd562a1fb0baf2
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b68fc648b74d721b
confidence: ok

तुमच्या HQ साठी प्रचार साहित्य (पोस्टर, बॅनर आणि अशा वस्तू) मागवा, त्याला मंजुरी घ्या, साहित्य घ्या आणि ते पोहोचले याची खात्री करा. प्रत्येक टप्प्याची नोंद होते, त्यामुळे विनंती कुठे आहे हे सगळ्यांना दिसते.

- start | icon: bi-pencil-square | color: #0b6b3a
  title: RMDO सुरुवात करतात
  text: साहित्य, प्रॉडक्ट आणि संख्या भरून विनंती करा.
- approve | icon: bi-shield-check | color: #d97706
  title: पाच मंजुऱ्या
  text: RM, SMD, SMM, ADMIN-MD CS आणि DGM-MD CS एकामागून एक ती तपासतात.
- close | icon: bi-box-seam | color: #7c3aed
  title: RMDO विनंती बंद करतात
  text: काय पोहोचले ते कळवा, कमी किंवा खराब नगांसह.
:::

::: section flow
title: विनंती कशी पुढे जाते, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 922a2e0da6a7ddbc
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- rmdo | type: lane | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  sub: तुमचे HQ
- approvers | type: lane | color: #0d9488 | roles: RM, SMD, SMM
  title: मंजुरी देणारे
  sub: RM · SMD · SMM
- mdcs | type: lane | color: #ea580c | roles: MDAdmin, MDHOD | designations: ADMIN-MD CS, ADMIN MD CS, DGM-MD CS, DGM MD CS
  title: MD CS
  sub: ADMIN-MD CS · DGM-MD CS
- vendor | type: lane | color: #64748b
  title: व्हेंडर
  sub: कुरिअर
- raise | lane: rmdo | col: 1 | icon: bi-pencil-square | color: #64748b | next: approve | screen: raise-request
  title: विनंती भरा आणि पाठवा
  status: Draft
  text: विनंतीचा फॉर्म भरा आणि **Submit Request** दाबा. तोपर्यंत तो फक्त तुम्हाला दिसणारा ड्राफ्ट असतो.
- approve | lane: approvers | col: 2 | icon: bi-hourglass-split | color: #d97706 | next: final | return: raise | screen: review-approve
  title: तपासणी आणि मंजुरी
  status: Pending Approval
  text: आधी RM, मग SMD, मग SMM विनंती तपासतात. प्रत्येक जण ती बदलू शकतो, मंजूर करून पुढे पाठवू शकतो, RMDO कडे परत पाठवू शकतो, किंवा नाकारू शकतो.
- final | lane: mdcs | col: 3 | icon: bi-patch-check | color: #2563eb | next: dispatch | screen: review-approve
  title: अंतिम मंजुरी
  status: Final Approved
  text: ADMIN-MD CS मंजूर करून पुढे पाठवतात, मग DGM-MD CS अंतिम मंजुरी देतात.
- dispatch | lane: mdcs | col: 4 | icon: bi-truck | color: #7c3aed | next: ship | screen: create-dispatch
  title: पाठवणी तयार करा
  status: In Dispatch
  text: ADMIN-MD CS किंवा DGM-MD CS कुरिअरच्या तपशीलासह व्हेंडरकडून झालेल्या पाठवणीची नोंद करतात.
- ship | lane: vendor | col: 5 | icon: bi-box-seam | color: #0d9488 | next: ack | screen: update-delivery
  title: HQ ला पाठवणे
  status: Delivered
  text: साहित्य व्हेंडरकडून निघते. MD CS आधी **In Transit** आणि मग **Delivered** अशी नोंद करतात.
- ack | lane: rmdo | col: 6 | icon: bi-clipboard-check | color: #16a34a | screen: acknowledge-delivery
  title: पोच द्या
  status: Completed
  text: RMDO साहित्य मोजतात आणि **Acknowledgement** सबमिट करतात. सगळे बरोबर असेल तर विनंती पूर्ण होते.
- exception | lane: mdcs | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: ack | to: dispatch | screen: resolve-exception
  title: कमी किंवा खराब नगांची अडचण सोडवा
  status: Exception Raised
  text: नग कमी असतील किंवा खराब असतील, तर MD CS राहिलेले नग व्हेंडरकडून पाठवतात, किंवा दुसऱ्या HQ मधला जास्तीचा साठा हलवतात.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 06524fbf96639e47
confidence: ok

- draft | color: #64748b
  label: Draft
  text: सेव्ह केले आहे पण पाठवलेले नाही. फक्त तुम्हाला दिसते.
- pending | color: #d97706
  label: Pending Approval
  text: आता मंजुरी देणाऱ्याकडे आहे.
- pending-final | color: #d97706
  label: Pending Final Approval
  text: शेवटच्या मंजुरीसाठी DGM-MD CS कडे आहे.
- returned | color: #ea580c
  label: Returned
  text: RMDO कडे परत पाठवली आहे. बदल करा आणि परत सबमिट करा.
- final | color: #2563eb
  label: Final Approved
  text: मंजूर झाली. पाठवणीची वाट पाहत आहे.
- dispatch | color: #7c3aed
  label: In Dispatch
  text: व्हेंडरने साहित्य पाठवले आहे.
- delivered | color: #0d9488
  label: Delivered
  text: साहित्य HQ ला पोहोचले.
- ack-pending | color: #0891b2
  label: Acknowledgement Pending
  text: काय पोहोचले ते RMDO ने कळवण्याची वाट पाहत आहे.
- completed | color: #16a34a
  label: Completed
  text: सगळे साहित्य चांगल्या स्थितीत पोहोचले.
- exception | color: #dc2626
  label: Exception Raised
  text: नग कमी होते किंवा खराब होते. MD CS ही अडचण सोडवत आहेत.
- transfer | color: #7c3aed
  label: Transfer Order Received
  text: तुमच्या HQ ला जास्तीचा साठा दुसऱ्या HQ ला पाठवायला सांगितले आहे.
- rejected | color: #475569
  label: Rejected
  text: मंजुरी देणाऱ्याने बंद केली. गरज असेल तर नवीन विनंती करा.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 3570dd53fa8c80d1
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- rmdo | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  - विनंती करा
  - साहित्य मिळाल्याची पोच द्या
  - स्टॉक ट्रान्सफरसाठी कुरिअरचा पुरावा पाठवा
- approvers | color: #0d9488 | roles: RM, SMD, SMM
  title: RM, SMD, SMM
  - तुमच्या भागातल्या विनंत्या तपासा
  - मंजूर करा, परत पाठवा किंवा नाकारा
- admin | color: #ea580c | roles: MDAdmin | designations: ADMIN-MD CS, ADMIN MD CS
  title: ADMIN-MD CS
  - मंजूर करून पुढे पाठवा
  - पाठवणी तयार करा आणि डिलिव्हरी अपडेट करा
  - कमी आणि खराब नगांची अडचण सोडवा
- dgm | color: #7c3aed | roles: MDHOD | designations: DGM-MD CS, DGM MD CS
  title: DGM-MD CS
  - अंतिम मंजुरी द्या
  - पाठवणी तयार करा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: a2321112e4f4d096
confidence: ok

- no-pencil | popular: true
  q: मी विनंती सबमिट केली आणि आता पेन्सिल आयकॉन दिसत नाही
  a: सबमिट केल्यावर फक्त मंजुरी देणारेच विनंती तुमच्याकडे परत पाठवू शकतात. त्यावर **Returned** दिसले की पेन्सिल परत दिसते.
- damaged | popular: true
  q: काही नग खराब आले किंवा कमी आले. मी काय करू?
  a: पोच देताना मिळालेले आणि खराब नग लिहा आणि तरीही सबमिट करा. ADMIN-MD CS कडे exception जाते, आणि ते राहिलेले नग पाठवतात.
- who-has-it
  q: माझी विनंती आता कोणाकडे आहे?
  a: विनंती उघडा. वर **Current Stage** आणि **Pending With** दिसतात, आणि **Approval History** मध्ये प्रत्येक कृती दिसते.
- no-rows
  q: मला "No requests found" दिसते
  a: तुमचे फिल्टर खूप मर्यादित असतील. **Filters** उघडा आणि **Clear All** दाबा.
:::

::: section requester-list
title: माझ्या विनंत्या पाहा आणि सांभाळा
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

**ही स्क्रीन कशासाठी आहे**
**My Publicity Material Request** स्क्रीनवर तुमच्या HQ च्या सगळ्या विनंत्या दिसतात. वर आकडे असतात आणि खाली टेबल असते.

1. वरची कार्डे पाहा, जसे **Draft**, **Pending Approval** आणि **Completed**. फक्त त्या विनंत्या पाहण्यासाठी कार्ड टॅप करा. सगळ्या पाहण्यासाठी ते परत टॅप करा.
2. विनंती पटकन शोधण्यासाठी सर्च बॉक्समध्ये इंडेंट नंबर, प्रॉडक्ट किंवा साहित्य टाइप करा.
3. **Filters** उघडा आणि **Date Range**, **Product**, **Publicity Material** किंवा **Status** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
4. **Action** कॉलममध्ये डोळ्याचा आयकॉन विनंती उघडतो. पेन्सिल **Draft** किंवा **Returned** विनंती बदलते. डस्टबिन **Draft** डिलीट करते.
5. लिस्ट Excel फाइल म्हणून डाउनलोड करण्यासाठी **Export** दाबा.
6. नवीन विनंती सुरू करण्यासाठी **Create New Request** दाबा.

> Careful: डिलीट केलेला ड्राफ्ट परत आणता येत नाही.

## नेहमीच्या अडचणी

- **मला "No requests found" दिसते.** तुमचे फिल्टर खूप मर्यादित असतील. **Clear All** दाबा.
- **पेन्सिल आयकॉन दिसत नाही.** सबमिट केल्यावर विनंती बदलता येत नाही. मंजुरी देणाऱ्याने ती परत पाठवली तरच बदलता येते.
- **Export बटण राखाडी आहे.** लिस्टमध्ये एक्सपोर्ट करायला एकही ओळ नाही.
:::

::: section raise-request
title: प्रचार साहित्याची विनंती करा
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

**ही स्क्रीन कशासाठी आहे**
तुमच्या HQ साठी प्रचार साहित्य मागवायला **Create Publicity Material Request** स्क्रीन वापरा. तुम्ही विनंती ड्राफ्ट म्हणून ठेवून नंतर पाठवू शकता.

1. **Request Details** खाली **Type of Publicity Material** आणि **Product** निवडा.
2. तुम्हाला लागणारी **Quantity** टाका. **Requested By** मध्ये तुमचे नाव आधीच असते.
3. तुमच्याकडे डिझाइन असेल तर फाइल अपलोड बॉक्समध्ये टाका किंवा **or click to browse** टॅप करा. गरज असेल तर **Remarks** लिहा.
4. **Location** खाली **State**, **Region** आणि **Headquarter (HQ)** तपासा. हे तुमच्या प्रोफाइलमधून भरलेले असतात.
5. नंतरसाठी ठेवायचे असेल तर **Save as Draft** दाबा, किंवा RM कडे पाठवण्यासाठी **Submit Request** दाबा आणि मग **Submit** दाबा.

> Tip: ड्राफ्ट तुम्ही लिस्टमधून परत उघडून नंतर पूर्ण करू शकता.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Type of Publicity Material** | तुम्हाला हवी असलेली वस्तू, उदा. पोस्टर किंवा बॅनर. भरणे आवश्यक. |
| **Product** | हे साहित्य कोणत्या प्रॉडक्टसाठी आहे. टाइप करून शोधता येते. भरणे आवश्यक. |
| **Quantity** | किती नग हवेत. भरणे आवश्यक. |
| **Design file** | एकच फाइल, PDF, JPG, PNG, DOC किंवा DOCX, 10 MB पर्यंत. |
| **State**, **Region**, **Headquarter (HQ)** | साहित्य कुठे पोहोचवायचे. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Select the state, region and headquarter."** (राज्य, रिजन आणि HQ निवडा) आधी State निवडा, मग Region, मग HQ.
- **"is larger than 10 MB"** (फाइल 10 MB पेक्षा मोठी आहे) फाइल लहान करा किंवा नवीन फोटो काढा, मग परत अपलोड करा.
- **सबमिट केल्यावर मी बदल करू शकत नाही.** हे नेहमीचेच आहे. फक्त परत पाठवलेली (returned) विनंतीच परत बदलता येते.
:::

::: section track-request
title: माझ्या विनंतीचा मागोवा घ्या
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
screenshot: track-request.png
screenshot-phone: track-request.phone.jpg
minutes: 2
source-hash: 533996d4d473e58d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Publicity Material Request Details** स्क्रीनवर एका विनंतीची सगळी माहिती दिसते: ती आता कुठे आहे, कोणी मंजूर केली, पाठवणी आणि कागदपत्रे.

1. लिस्टमधून डोळ्याचा आयकॉन वापरून विनंती उघडा.
2. वर **Current Stage**, **Status** आणि विनंती आता कोणाकडे आहे (**Pending With**) ते पाहा.
3. **Request Details & History** वर **Request Timeline** आणि **Approval History** टेबल पाहा. त्यात प्रत्येक कृती आणि शेरा असतो.
4. प्रत्येक पाठवणी पाहण्यासाठी **Tracking & Delivery** उघडा: व्हेंडर, ट्रान्सपोर्टर, ट्रॅकिंग नंबर आणि डिलिव्हरीची स्थिती.
5. साहित्य पोहोचल्यावर ते मिळाल्याची खात्री देण्यासाठी **Acknowledgement** उघडा.
6. अपलोड केलेल्या सगळ्या फाइल पाहण्यासाठी **Related Documents** उघडा.
7. विनंतीची PDF हवी असेल तर **Download Report** दाबा, किंवा प्रिंट करण्यासाठी **Print** दाबा.
8. विनंती **Returned** झाली असेल तर **Edit & Resubmit** दाबा, दुरुस्त करा आणि परत सबमिट करा.

## नेहमीच्या अडचणी

- **Tracking & Delivery वर "Not dispatched yet" दिसते.** साहित्याची मंजुरी अजून चालू आहे किंवा पाठवणी अजून झालेली नाही. **Current Stage** तपासा.
- **Status मध्ये "Rejected" दिसते.** विनंती बंद झाली आहे. **Approval History** मध्ये कारण वाचा आणि गरज असेल तर नवीन विनंती करा.
- **मला Edit & Resubmit दिसत नाही.** विनंती तुमच्याकडे परत पाठवली गेली तरच हे दिसते.
:::

::: section acknowledge-delivery
title: मिळालेल्या साहित्याची पोच द्या
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
minutes: 3
source-hash: 2e6ce69978f16d26
confidence: ok

**ही स्क्रीन कशासाठी आहे**
साहित्य तुमच्या HQ ला पोहोचल्यावर तुम्हाला किती नग मिळाले आणि ते कोणत्या स्थितीत आहेत हे नक्की कळवावे लागते. यामुळे विनंती बंद होते, किंवा अडचण ADMIN-MD CS ला कळवली जाते.

1. विनंती उघडा आणि **Tracking & Delivery** वर जा.
2. **Awaiting Acknowledgement** बॉक्समध्ये **Acknowledge** दाबा. यामुळे **Acknowledgement** टॅब उघडतो.
3. **Dispatch Summary** तपासा (पाठवलेली संख्या आणि तारीख).
4. **Quantity Received** टाका.
5. **In Good Condition** किंवा **In Damaged Condition** निवडा. खराब असेल तर **Damaged Quantity** टाका.
6. साहित्य कोणी तपासले ते **Acknowledged By** मध्ये निवडा.
7. **Remarks** लिहा आणि साहित्याचे फोटो अपलोड करा.
8. **Exception Details** बॉक्स दिसला तर तो तपासा. त्यात कमी, जास्त किंवा खराब नग दिसतात.
9. नंतर पूर्ण करायचे असेल तर **Save as Draft** दाबा, किंवा **Submit Acknowledgement** दाबा, मग **Submit** दाबा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Quantity Received** | प्रत्यक्ष मोजलेले नग. भरणे आवश्यक. |
| **Material Condition** | चांगले किंवा खराब. भरणे आवश्यक. |
| **Damaged Quantity** | 1 पासून मिळालेल्या संख्येपर्यंत. |
| **Acknowledged By** | साहित्य घेणारी व्यक्ती. भरणे आवश्यक. |
| **Photos** | मिळालेल्या साहित्याचे फोटो, प्रत्येकी 10 MB पर्यंत. |

## नेहमीच्या अडचणी

- **"Damaged quantity must be between 1 and the received quantity."** (खराब नग 1 ते मिळालेल्या संख्येच्या दरम्यान असावेत) आकडे परत तपासा.
- **नग कमी आहेत किंवा खराब आहेत.** तरीही सबमिट करा. ADMIN-MD CS कडे exception जाते, आणि ते राहिलेले नग पाठवतील.
- **Acknowledge बटण दिसत नाही.** साहित्य अजून पाठवलेले नाही, किंवा त्याची पोच आधीच दिलेली आहे.
:::

::: section transfer-proof
title: जास्तीचा साठा दुसऱ्या HQ ला पाठवा
routes: /PublicityRequest/{id}/transfer/{dispatchId}, /PublicityRequest/done
for: 
roles: 
designations: 
minutes: 3
source-hash: e7a33ab19f8314b5
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुमच्या HQ ला गरजेपेक्षा जास्त साहित्य मिळाले असेल, तर ADMIN-MD CS तुम्हाला त्यातले काही साहित्य कमी पडलेल्या दुसऱ्या HQ ला पाठवायला सांगू शकतात. तुम्ही ते कुरिअरने पाठवता आणि कुरिअरची पावती इथे अपलोड करता.

1. **Notifications** उघडा, स्टॉक ट्रान्सफर ऑर्डर टॅप करा, मग **View Transfer** दाबा. किंवा तुमची विनंती उघडा, **Tracking & Delivery** वर जा आणि **Stock Transfer Order** कार्डवर **Create Transfer** दाबा.
2. **Read Information (Read Only)** भाग तपासा: **Destination HQ** आणि **Transfer Quantity**.
3. साहित्य पॅक करा आणि कुरिअरने पाठवा.
4. **Upload Courier / Dispatch Proof** खाली कुरिअर पावतीचा स्पष्ट फोटो अपलोड करा.
5. ॲप पावती वाचते आणि **Courier / Transporter**, **Tracking No. / AWB** आणि **Dispatch Date** भरते. ते तपासा आणि गरज असेल तर दुरुस्त करा.
6. तुमच्याकडे असतील तर **Vehicle No** आणि **Remarks (Optional)** टाका.
7. **Submit Proof to Admin** दाबा.
8. **Dispatch Proof Submitted Successfully!** स्क्रीन उघडते. **Go to Tracking & Delivery** किंवा **Back to List** दाबा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Courier / Transporter** | कुरिअर कंपनीचे नाव. भरणे आवश्यक. |
| **Tracking No. / AWB** | कुरिअर पावतीवरचा नंबर. भरणे आवश्यक. |
| **Dispatch Date** | तुम्ही पाठवले ती तारीख. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Upload the courier / dispatch proof."** (कुरिअर / पाठवणीचा पुरावा अपलोड करा) सबमिट करण्याआधी पावती अपलोड करा.
- **तपशील आपोआप भरले गेले नाहीत.** फोटो धूसर असेल किंवा फाइल PDF असेल. तपशील स्वतः टाइप करा.
- **"Transfer order not found".** (ट्रान्सफर ऑर्डर सापडली नाही) पुरावा आधीच सबमिट झाला आहे. तो बदलायचा असेल तर **Tracking & Delivery** टॅबवर **Update Proof** वापरा.
:::

::: section requester-notifications
title: माझ्या प्रचार साहित्याच्या सूचना
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

**ही स्क्रीन कशासाठी आहे**
**Notifications** स्क्रीनवर तुमच्या विनंत्यांबद्दलच्या सूचना दिसतात: परत पाठवलेल्या विनंत्या, पाठवण्या, अडचणी (exception) आणि स्टॉक ट्रान्सफर ऑर्डर. लिस्ट पेजवर **Notifications** दाबून ही स्क्रीन उघडा.

1. एक टॅब निवडा: **All**, **Stock Transfer**, **Exception** किंवा **Dispatch & Approvals**.
2. न वाचलेल्या सूचनांवर एक ठिपका असतो. तपशील उघडण्यासाठी सूचना टॅप करा.
3. ट्रान्सफर ऑर्डरसाठी कुरिअरचा पुरावा अपलोड करायला **View Transfer** दाबा. विनंती उघडण्यासाठी **View Details** दाबा.
4. एका सूचनेसाठी **Mark As Read** दाबा, किंवा सगळ्यांसाठी **Mark All As Read** दाबा.
5. परत जाण्यासाठी **Back to List Page** दाबा.

## नेहमीच्या अडचणी

- **"No notifications".** (कोणतीही सूचना नाही) तुमच्या विनंत्यांवर अजून काहीही नवीन घडलेले नाही.
- **Mark All As Read राखाडी आहे.** सगळ्या सूचना आधीच वाचलेल्या आहेत.
:::

::: section reviewer-list
title: माझ्याकडे तपासणीसाठी आलेल्या विनंत्या पाहा
routes: /PublicityReview
for: 
roles: 
designations: 
screenshot: reviewer-list.png
screenshot-phone: reviewer-list.phone.jpg
minutes: 2
source-hash: 7eaa380f878c5c45
confidence: ok

**ही स्क्रीन कशासाठी आहे**
मंजुरी देणारे आणि MD CS कर्मचारी आपल्या भागातल्या विनंत्या पाहण्यासाठी ही लिस्ट वापरतात. स्क्रीनचे नाव **Submitted Requests** असते (RM साठी ते **My Publicity Material Request** असते).

1. वरची कार्डे पाहा. RM ला **Pending for My Approval**, **Approved by RM**, **Dispatch & Delivery**, **Completed** आणि **Returned** दिसतात. इतरांना **Total Submitted**, **Pending Review**, **Dispatch Pending**, **Exceptions** आणि **Completed** अशी कार्डे दिसतात.
2. लाल रंगाचा **New Request for Review** बॉक्स दिसला तर **View New Requests** दाबा. RM, SMD, SMM आणि ADMIN-MD CS साठी यामुळे consolidation स्क्रीन उघडते.
3. **Filters** उघडून **Date Range**, **Product**, **Publicity Material**, **State**, **Region**, **HQ**, **Current Stage** किंवा **Status** निवडा, मग **Apply Filters** दाबा.
4. तुमची वाट पाहणाऱ्या ओळींच्या **Status** कॉलममध्ये **Pending** दिसते. तुम्ही आधीच पुढे पाठवलेल्या ओळींमध्ये **Approved** दिसते.
5. विनंती उघडण्यासाठी डोळ्याचा आयकॉन टॅप करा.
6. लिस्ट Excel म्हणून डाउनलोड करण्यासाठी **Export** दाबा.
7. तुमच्या लेव्हलवर वाट पाहणाऱ्या सगळ्या विनंत्या रिजननुसार गट करून पाहण्यासाठी **Consolidation** दाबा.

## नेहमीच्या अडचणी

- **मला State किंवा Region फिल्टर दिसत नाहीत.** RM, SMD आणि SMM यांना फक्त त्यांचाच भाग दिसतो, त्यामुळे काही फिल्टर लपवलेले असतात.
- **लिस्ट रिकामी आहे.** Filters मध्ये **Clear All** दाबा. कदाचित अजून एकही विनंती तुमच्याकडे आलेली नसेल.
:::

::: section review-approve
title: विनंती तपासा आणि मंजूर करा
routes: /PublicityReview/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: 3d13e1d79363e813
confidence: ok

**ही स्क्रीन कशासाठी आहे**
विनंती तपासण्यासाठी ती उघडा आणि मंजूर करा, बदला किंवा नाकारा. विनंती तुमची वाट पाहत असेल तेव्हाच बटणे दिसतात.

1. लिस्टमधून किंवा consolidation स्क्रीनवरून विनंती उघडा.
2. वरची पट्टी, **Request Timeline** आणि **Approval History** तपासा.
3. काही नोंद असेल तर **Additional Remarks (if any)** मध्ये टाइप करा.
4. आधी साहित्य, संख्या किंवा ठिकाण बदलायचे असेल तर **Edit** दाबा.
5. मंजूर करण्यासाठी पुढच्या लेव्हलचे **Approve & Forward to** बटण दाबा (उदा. **Approve & Forward to SMD**), मग **Approve & Forward** दाबून खात्री करा.
6. नाकारण्यासाठी **Reject** दाबा, **Reason for rejection** लिहा, आणि परत **Reject** दाबा.

> Careful: Reject केल्यावर विनंती कायमची बंद होते. RMDO ला नवीन विनंती करावी लागेल.

## मंजुरीचा क्रम

- RM पुढे SMD कडे पाठवतात, SMD पुढे SMM कडे, SMM पुढे ADMIN - MD CS कडे, ADMIN-MD CS पुढे DGM - MD CS कडे.
- DGM-MD CS अंतिम मंजुरी देतात. या टप्प्यावर **Edit** नसते.

## नेहमीच्या अडचणी

- **Approve बटण नाही, आणि स्क्रीनचे नाव "Request Details" आहे.** विनंती आता तुमच्या लेव्हलवर नाही. तुम्ही फक्त पाहू शकता, **Print** करू शकता किंवा **Download Report** करू शकता.
- **"Enter the reason for rejection."** (नाकारण्याचे कारण लिहा) कारण लिहिणे आवश्यक आहे.
:::

::: section reviewer-edit
title: पुढे पाठवण्याआधी विनंती बदला
routes: /PublicityReview/edit/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: e76c7e9756536fde
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Edit Publicity Material Request** स्क्रीनवर, ज्या मंजुरी देणाऱ्याची पाळी आहे तो विनंती दुरुस्त करू शकतो. बदल सेव्ह केल्यावर विनंती मंजूरही होते आणि पुढे पाठवली जाते.

1. विनंतीवर **Edit** दाबा. consolidation स्क्रीनवर पेन्सिल आयकॉन टॅप करा.
2. **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** किंवा **Remarks** बदला.
3. गरज असेल तर डिझाइन फाइल जोडा किंवा काढा.
4. **State**, **Region** आणि **Headquarter (HQ)** तपासा.
5. पुढच्या लेव्हलचे **Save & Forward to** बटण दाबा (उदा. **Save & Forward to SMM**).
6. बदल सेव्ह होतो, **Approval History** मध्ये नोंदवला जातो, आणि विनंती पुढच्या लेव्हलला जाते.

## नेहमीच्या अडचणी

- **Edit पेजऐवजी तपशील पेज उघडते.** विनंती आता तुमची वाट पाहत नाही.
- **मला फक्त सेव्ह करायचे होते, पुढे पाठवायचे नव्हते.** इथे सेव्ह केल्यावर विनंती नेहमी पुढे जाते. अजून पुढे पाठवायचे नसेल तर **Back to List** वापरा.
:::

::: section consolidation
title: अनेक विनंत्या एकत्र मंजूर करा
routes: /PublicityReview/consolidation
for: 
roles: 
designations: 
screenshot: consolidation.png
screenshot-phone: consolidation.phone.jpg
minutes: 3
source-hash: abde9049ef5f5976
confidence: ok

**ही स्क्रीन कशासाठी आहे**
consolidation स्क्रीनवर तुमच्या लेव्हलवर वाट पाहणाऱ्या सगळ्या विनंत्या रिजननुसार गट करून दिसतात. त्यामुळे तुम्ही त्या सगळ्या एकाच वेळी मंजूर करू शकता. RM ला त्यांचा रिजन दिसतो, SMD आणि SMM यांना राज्य दिसते, ADMIN-MD CS यांना संपूर्ण भारत दिसतो. DGM-MD CS ही स्क्रीन वापरत नाहीत.

1. लिस्ट पेजवर **Consolidation** दाबा.
2. कार्डे तपासा: **Submission ID**, **Region** किंवा **State**, **Total Region**, **Total Requests** आणि **Total Requirement**.
3. टेबलमध्ये एखाद्या रिजनच्या HQ विनंत्या पाहण्यासाठी **Action** मधला बाण टॅप करा.
4. विनंती पाहण्यासाठी डोळ्याचा आयकॉन, किंवा बदलण्यासाठी पेन्सिल आयकॉन टॅप करा.
5. खालचा बॉक्स वाचा. किती रिजन तुमच्याकडे पोहोचले ते त्यात दिसते.
6. सगळे रिजन तुमच्याकडे पोहोचल्यावर पुढच्या लेव्हलचे **Approve & Submit to** बटण दाबा, मग **Approve & Submit** दाबून खात्री करा.
7. टेबल डाउनलोड करण्यासाठी **Export** दाबा, किंवा लिस्टवर परत जाण्यासाठी **View Submitted Request** दाबा.

## नेहमीच्या अडचणी

- **Approve & Submit राखाडी आहे.** काही रिजन अजून आधीच्या लेव्हलकडे बाकी आहेत. सगळे रिजन तुमच्याकडे येईपर्यंत थांबा.
- **"Nothing waiting for you".** (तुमच्याकडे काहीही बाकी नाही) आता तुमच्या लेव्हलवर एकही विनंती नाही.
- **Request Qty आणि Approved Qty वेगळे आहेत.** एखाद्या मंजुरी देणाऱ्याने संख्या बदलली आहे. विनंतीवरची **Approval History** पाहा.
:::

::: section create-dispatch
title: पाठवणी तयार करा
routes: /PublicityReview/{id}/dispatch, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3
source-hash: 039c89aba91e084f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
अंतिम मंजुरीनंतर ADMIN-MD CS किंवा DGM-MD CS व्हेंडरकडून झालेल्या पाठवणीची नोंद **Create Dispatch** स्क्रीनवर करतात. बदली (re-initiation) पाठवणीसाठी पण हीच स्क्रीन वापरली जाते.

1. **Final Approved** विनंती उघडा आणि **Create Dispatch** दाबा.
2. **Read Information (Read Only)** भाग तपासा.
3. **Feed Logistics & Dispatch Data** खाली **Dispatch Date**, **Dispatch Quantity**, **Vendor** आणि **Transporter / Courier** भरा.
4. माहीत असतील तर **Vehicle No**, **Tracking No.** आणि **Expected Delivery Date** टाका.
5. **Responsible SMDO / RM / RMDO / MO** निवडा.
6. **Dispatch / Note Approval Document** अपलोड करा. तुमच्याकडे असतील तर ट्रान्सपोर्ट स्लिप किंवा E-way bill पण जोडा.
7. **Delivery Address** निवडा किंवा टाइप करा.
8. **Confirm and Create Dispatch** दाबा.
9. **Dispatch Created Successfully!** स्क्रीन उघडते. **Go to Tracking & Delivery** दाबा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Dispatch Quantity** | आता पाठवलेले नग. बदली पाठवणीसाठी ही संख्या बाकी असलेल्या नगांपासून सुरू होते. |
| **Vendor** | लिस्टमधून निवडा किंवा नवीन नाव टाइप करा. भरणे आवश्यक. |
| **Responsible SMDO / RM / RMDO / MO** | साहित्य घेणारा अधिकारी. भरणे आवश्यक. |
| **Dispatch / Note Approval Document** | पहिल्या पाठवणीसाठी आवश्यक. |
| **Delivery Address** | सेव्ह केलेला पत्ता टॅप करा किंवा पत्ता टाइप करा. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Upload the dispatch / note approval document."** (पाठवणी / नोट मंजुरीचे कागदपत्र अपलोड करा) खात्री करण्याआधी ते अपलोड करा.
- **Create Dispatch बटण दिसत नाही.** विनंती अजून **Final Approved** झालेली नाही.
:::

::: section update-delivery
title: पाठवणी आणि डिलिव्हरीची स्थिती अपडेट करा
routes: /PublicityReview/{id}
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 2
source-hash: 70a3a9c616c85eba
confidence: ok

**ही स्क्रीन कशासाठी आहे**
पाठवणी झाल्यावर MD CS तिची प्रगती अपडेट करतात. त्यामुळे RMDO आणि मंजुरी देणाऱ्यांना साहित्य कुठे आहे ते दिसते.

1. विनंती उघडा आणि **Tracking & Delivery** वर जा.
2. पाठवणीचे कार्ड शोधा (**Dispatch 1**, **Dispatch 2** वगैरे).
3. साहित्य व्हेंडरकडून निघाल्यावर **Mark In Transit** दाबा. **Expected Delivery Date** आणि **Current Location / Hub** टाका, मग **Save** दाबा.
4. साहित्य HQ ला पोहोचल्यावर **Mark Delivered** दाबा, गरज असेल तर **Current Location / Hub** टाका, आणि **Save** दाबा.
5. स्टॉक ट्रान्सफरसाठी: पाठवणाऱ्या (source) RMDO ने कुरिअरचा पुरावा अपलोड केल्यावर **Update Dispatch Details** दाबा. **Transporter / Courier** आणि **Tracking No. (AWB)** तपासा, मग **Save** दाबा. त्यानंतर ट्रान्सफरवर **Transfer Created** दिसते.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Transporter / Courier** आणि **Tracking No. (AWB)** | ट्रान्सफरची खात्री करताना भरणे आवश्यक. |
| **Expected Delivery Date** | साहित्य कधी पोहोचायला हवे. |
| **Current Location / Hub** | साहित्य आता कुठे आहे, उदा. कुरिअर हब. |

## नेहमीच्या अडचणी

- **पाठवणीच्या कार्डवर बटणे नाहीत.** पाठवणीची पोच आधीच दिलेली आहे, किंवा ट्रान्सफरचा पुरावा अजून source RMDO कडून बाकी आहे ("Courier proof is pending from the source RMDO").
- **RMDO पोच देऊ शकत नाहीत.** आधी साहित्य पाठवलेले असले पाहिजे.
:::

::: section resolve-exception
title: कमी किंवा खराब नगांची अडचण सोडवा
routes: /PublicityReview/{id}, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3
source-hash: 981f49f742015d88
confidence: ok

**ही स्क्रीन कशासाठी आहे**
RMDO ने कमी नग किंवा खराब नग कळवले की विनंतीवर **Exception Raised** दिसते. राहिलेली संख्या कशी पाठवायची ते तुम्ही ठरवता.

1. विनंती उघडा (लिस्टवरचे **Exceptions** कार्ड अशा विनंत्या शोधायला मदत करते) आणि **Tracking & Delivery** वर जा.
2. लाल **Exception Raised** बॉक्समध्ये **Exception Type**, **Raised By** आणि **Remarks** वाचा.
3. **Resolve Exception** दाबा. बॉक्समध्ये बाकी असलेली संख्या दिसते.
4. राहिलेले नग व्हेंडरकडून पाठवायचे असतील तर **Re-initiate dispatch from the vendor** निवडा, **Continue to Dispatch** दाबा आणि बदली साहित्यासाठी पाठवणीचा फॉर्म भरा.
5. दुसऱ्या HQ मधला जास्तीचा साठा हलवायचा असेल तर **Stock transfer from another HQ's excess** निवडा, **Source Request (Excess)** निवडा, **Transfer Quantity** तपासा, **Remarks** टाका, मग **Send Transfer Order** दाबा.
6. ट्रान्सफरसाठी **Transfer Order Sent Successfully** स्क्रीन उघडते. source HQ च्या RMDO ला साठा पाठवून कुरिअरचा पुरावा अपलोड करायला सांगितले जाते.
7. पुरावा आल्यावर ट्रान्सफर कार्डवरच्या **Update Dispatch Details** ने त्याची खात्री करा.

## नेहमीच्या अडचणी

- **स्टॉक ट्रान्सफरचा पर्याय राखाडी आहे ("no excess available").** कोणत्याही दुसऱ्या HQ कडे जास्तीचा साठा नाही. व्हेंडरचा पर्याय वापरा.
- **"Select the source request and quantity."** (source विनंती आणि संख्या निवडा) एक source निवडा आणि शून्यापेक्षा जास्त संख्या टाका.
- **Excess only.** (फक्त जास्त नग) RMDO ला जास्त नग मिळाले असतील तर विनंती पूर्ण (completed) होते. तो जास्तीचा साठा नंतर दुसऱ्या HQ साठी वापरता येतो.
:::

::: section reviewer-notifications
title: मंजुरी देणारे आणि MD CS यांच्या सूचना
routes: /PublicityReview/notifications
for: 
roles: 
designations: 
minutes: 1
source-hash: 3fcc7d3b610404d1
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Notifications** स्क्रीनवर तुमच्या भागातल्या विनंत्यांबद्दलच्या सूचना दिसतात: नवीन मंजुऱ्या, पाठवण्या, अडचणी (exception) आणि स्टॉक ट्रान्सफर.

1. लिस्ट पेजवर **Notifications** दाबा.
2. एक टॅब निवडा: **All**, **Stock Transfer**, **Exception** किंवा **Dispatch & Approvals**.
3. न वाचलेल्या सूचनांवर एक ठिपका असतो. सूचना वाचण्यासाठी ती टॅप करा.
4. विनंती **Tracking & Delivery** टॅबवर उघडण्यासाठी **View Details** दाबा.
5. एका सूचनेसाठी **Mark As Read** दाबा, किंवा सगळ्यांसाठी **Mark All As Read** दाबा.
6. परत जाण्यासाठी **Back to List Page** दाबा.

## नेहमीच्या अडचणी

- **"No notifications".** (कोणतीही सूचना नाही) अजून काहीही नवीन घडलेले नाही.
- **exception ची सूचना आली आहे.** विनंती उघडा आणि **Resolve Exception** वापरा (फक्त ADMIN-MD CS आणि DGM-MD CS).
:::
