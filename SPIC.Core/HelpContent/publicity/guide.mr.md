---
module: publicity
title: प्रचार साहित्य ट्रॅकिंग
icon: bi-megaphone
order: 40
language: mr
---

::: section overview
title: प्रचार साहित्य ट्रॅकिंग कसे चालते
routes: /PublicityRequest, /PublicityReview
for: 
roles: 
designations: 
source-hash: f087f488522bb49d
confidence: ok

**हा मॉड्युल कशासाठी आहे**
तुमच्या HQ साठी प्रचार साहित्य (पोस्टर, बॅनर आणि अशा वस्तू) मागवण्यासाठी हा मॉड्युल वापरा. त्याला मंजुरी घ्या, साहित्य घ्या आणि ते पोहोचले हे कळवा. प्रत्येक टप्प्याची नोंद होते. त्यामुळे विनंती कुठे आहे हे सगळ्यांना दिसते.

## एका विनंतीचा प्रवास

1. RMDO विनंती भरतात आणि **Submit Request** दाबतात.
2. विनंती या क्रमाने मंजुरीसाठी जाते: RM, मग SMD, मग SMM, मग ADMIN-MD CS, मग DGM-MD CS (अंतिम मंजुरी).
3. कोणताही मंजुरी देणारा (DGM-MD CS सोडून) पुढे पाठवण्याआधी तपशील बदलू शकतो. मंजुरी देणारा विनंती **Reject** पण करू शकतो. त्याने विनंती बंद होते.
4. अंतिम मंजुरीनंतर ADMIN-MD CS (किंवा DGM-MD CS) व्हेंडरकडून पाठवणी (dispatch) तयार करतात.
5. साहित्य असे पुढे जाते: **Dispatched**, मग **In Transit**, मग **Delivered**.
6. RMDO साहित्य तपासतात आणि मिळालेली संख्या व साहित्याची स्थिती लिहून **Acknowledgement** सबमिट करतात.
7. सगळे बरोबर असेल तर विनंती **Completed** होते.
8. काही नग कमी आले किंवा खराब असतील तर **Exception Raised** स्थिती दिसते. ADMIN-MD CS व्हेंडरकडून नवीन पाठवणी करून, किंवा दुसऱ्या HQ मधला जास्तीचा साठा हलवून ही अडचण सोडवतात.

## कोण काय करते

- **RMDO**: विनंती तयार करतात, साहित्य मिळाल्याची पोच देतात, जास्तीचा साठा पाठवायला सांगितल्यावर कुरिअरचा पुरावा पाठवतात.
- **RM, SMD, SMM**: आपल्या रिजन किंवा राज्याच्या विनंत्या तपासतात आणि मंजूर करतात.
- **ADMIN-MD CS**: मंजुरी देतात, पाठवणी तयार करतात, डिलिव्हरी अपडेट करतात, अडचणी (exception) सोडवतात.
- **DGM-MD CS**: अंतिम मंजुरी देतात आणि पाठवणी पण तयार करू शकतात.

> नवीन मंजुऱ्या, पाठवण्या आणि ट्रान्सफर ऑर्डर पाहण्यासाठी लिस्ट पेजवरचे घंटीचे बटण **Notifications** टॅप करा.
:::

::: section requester-list
title: माझ्या विनंत्या पाहा आणि सांभाळा
routes: /PublicityRequest
for: 
roles: 
designations: 
source-hash: f32b121b5900212e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Publicity Material Request** स्क्रीनवर तुमच्या HQ च्या सगळ्या विनंत्या दिसतात. वर आकडे असतात आणि खाली टेबल असते.

## पायऱ्या

1. वरची कार्डे पाहा: **Total Request**, **Draft**, **Pending Approval**, **Final Approved**, **In Dispatch**, **Acknowledgement Pending**, **Completed** आणि **Exception**.
2. एखाद्या स्थितीतल्या विनंत्याच पाहण्यासाठी ते कार्ड टॅप करा. सगळ्या पाहण्यासाठी तेच कार्ड परत टॅप करा.
3. जास्त बारकाईने शोधायचे असेल तर **Filters** उघडा. **Date Range**, **Product**, **Publicity Material** किंवा **Status** निवडा, मग **Apply Filters** दाबा. फिल्टर काढण्यासाठी **Clear All** दाबा.
4. विनंती पटकन शोधण्यासाठी सर्च बॉक्समध्ये इंडेंट नंबर, प्रॉडक्ट किंवा साहित्य टाइप करा.
5. **Action** कॉलममध्ये डोळ्याचा आयकॉन टॅप करून विनंती उघडा.
6. **Draft** किंवा **Returned** विनंती बदलण्यासाठी पेन्सिल आयकॉन टॅप करा.
7. फक्त **Draft** विनंती डिलीट करता येते: त्यासाठी डस्टबिन आयकॉन टॅप करा. डिलीट केलेली विनंती परत आणता येत नाही.
8. लिस्ट Excel फाइल म्हणून डाउनलोड करण्यासाठी **Export** दाबा.
9. नवीन विनंती सुरू करण्यासाठी **Create New Request** दाबा.

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
source-hash: f4c944427bc4be47
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुमच्या HQ साठी प्रचार साहित्य मागवायला **Create Publicity Material Request** स्क्रीन वापरा. तुम्ही विनंती ड्राफ्ट म्हणून ठेवून नंतर पाठवू शकता.

## पायऱ्या

1. लिस्ट पेजवर **Create New Request** दाबा.
2. **Request Details** खाली **Type of Publicity Material** आणि **Product** निवडा.
3. तुम्हाला लागणारी **Quantity** टाका.
4. **Requested By** तपासा. त्यात तुमचे नाव आधीच असते.
5. तुमच्याकडे डिझाइन असेल तर फाइल अपलोड बॉक्समध्ये टाका किंवा **or click to browse** टॅप करा.
6. गरज असेल तर **Remarks** लिहा.
7. **Location** खाली **State**, **Region** आणि **Headquarter (HQ)** तपासा. हे तुमच्या प्रोफाइलमधून भरलेले असतात.
8. नंतरसाठी ठेवायचे असेल तर **Save as Draft** दाबा, किंवा RM कडे पाठवण्यासाठी **Submit Request** दाबा.
9. खात्रीच्या बॉक्समध्ये **Submit** दाबा.

## फील्डची माहिती

- **Type of Publicity Material**: तुम्हाला हवी असलेली वस्तू, उदा. पोस्टर किंवा बॅनर. भरणे आवश्यक.
- **Product**: हे साहित्य कोणत्या प्रॉडक्टसाठी आहे. टाइप करून शोधता येते. भरणे आवश्यक.
- **Quantity**: किती नग हवेत. भरणे आवश्यक.
- **Requested By**: साहित्य मागणारी व्यक्ती.
- **Design file**: एकच फाइल, PDF, JPG, PNG, DOC किंवा DOCX, 10 MB पर्यंत.
- **State**, **Region**, **Headquarter (HQ)**: साहित्य कुठे पोहोचवायचे. भरणे आवश्यक.

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
source-hash: 2d926f137adb7d61
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Publicity Material Request Details** स्क्रीनवर एका विनंतीची सगळी माहिती दिसते: ती आता कुठे आहे, कोणी मंजूर केली, पाठवणी आणि कागदपत्रे.

## पायऱ्या

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
source-hash: 3642d2f43fd54ffa
confidence: ok

**ही स्क्रीन कशासाठी आहे**
साहित्य तुमच्या HQ ला पोहोचल्यावर तुम्हाला किती नग मिळाले आणि ते कोणत्या स्थितीत आहेत हे नक्की कळवावे लागते. यामुळे विनंती बंद होते, किंवा अडचण ADMIN-MD CS ला कळवली जाते.

## पायऱ्या

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

- **Quantity Received**: प्रत्यक्ष मोजलेले नग. भरणे आवश्यक.
- **Material Condition**: चांगले किंवा खराब. भरणे आवश्यक.
- **Damaged Quantity**: 1 पासून मिळालेल्या संख्येपर्यंत.
- **Acknowledged By**: साहित्य घेणारी व्यक्ती. भरणे आवश्यक.
- **Photos**: मिळालेल्या साहित्याचे फोटो, प्रत्येकी 10 MB पर्यंत.

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
source-hash: 5b649ad478721aa8
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुमच्या HQ ला गरजेपेक्षा जास्त साहित्य मिळाले असेल, तर ADMIN-MD CS तुम्हाला त्यातले काही साहित्य कमी पडलेल्या दुसऱ्या HQ ला पाठवायला सांगू शकतात. तुम्ही ते कुरिअरने पाठवता आणि कुरिअरची पावती इथे अपलोड करता.

## पायऱ्या

1. **Notifications** उघडा, स्टॉक ट्रान्सफर ऑर्डर टॅप करा, मग **View Transfer** दाबा. किंवा तुमची विनंती उघडा, **Tracking & Delivery** वर जा आणि **Stock Transfer Order** कार्डवर **Create Transfer** दाबा.
2. **Read Information (Read Only)** भाग तपासा: **Destination HQ** आणि **Transfer Quantity**.
3. साहित्य पॅक करा आणि कुरिअरने पाठवा.
4. **Upload Courier / Dispatch Proof** खाली कुरिअर पावतीचा स्पष्ट फोटो अपलोड करा.
5. ॲप पावती वाचते आणि **Courier / Transporter**, **Tracking No. / AWB** आणि **Dispatch Date** भरते. ते तपासा आणि गरज असेल तर दुरुस्त करा.
6. तुमच्याकडे असतील तर **Vehicle No** आणि **Remarks (Optional)** टाका.
7. **Submit Proof to Admin** दाबा.
8. **Dispatch Proof Submitted Successfully!** स्क्रीन उघडते. **Go to Tracking & Delivery** किंवा **Back to List** दाबा.

## फील्डची माहिती

- **Courier / Transporter**: कुरिअर कंपनीचे नाव. भरणे आवश्यक.
- **Tracking No. / AWB**: कुरिअर पावतीवरचा नंबर. भरणे आवश्यक.
- **Dispatch Date**: तुम्ही पाठवले ती तारीख. भरणे आवश्यक.

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
source-hash: 3f380e63b29f62fe
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Notifications** स्क्रीनवर तुमच्या विनंत्यांबद्दलच्या सूचना दिसतात: परत पाठवलेल्या विनंत्या, पाठवण्या, अडचणी (exception) आणि स्टॉक ट्रान्सफर ऑर्डर.

## पायऱ्या

1. लिस्ट पेजवर **Notifications** दाबा.
2. एक टॅब निवडा: **All**, **Stock Transfer**, **Exception** किंवा **Dispatch & Approvals**.
3. न वाचलेल्या सूचनांवर एक ठिपका असतो. तपशील उघडण्यासाठी सूचना टॅप करा.
4. ट्रान्सफर ऑर्डरसाठी **Transfer Order Details**, **Source Excess Reference** आणि **Reason for Transfer** पाहा.
5. कुरिअरचा पुरावा अपलोड करण्यासाठी **View Transfer** दाबा, किंवा विनंती उघडण्यासाठी **View Details** दाबा.
6. एका सूचनेसाठी **Mark As Read** दाबा, किंवा सगळ्यांसाठी **Mark All As Read** दाबा.
7. परत जाण्यासाठी **Back to List Page** दाबा.

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
source-hash: 7ebd8194aa3f5ebd
confidence: ok

**ही स्क्रीन कशासाठी आहे**
मंजुरी देणारे आणि MD CS कर्मचारी आपल्या भागातल्या विनंत्या पाहण्यासाठी ही लिस्ट वापरतात. स्क्रीनचे नाव **Submitted Requests** असते (RM साठी ते **My Publicity Material Request** असते).

## पायऱ्या

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
source-hash: c7fff9b7b3926213
confidence: ok

**ही स्क्रीन कशासाठी आहे**
विनंती तपासण्यासाठी ती उघडा आणि मंजूर करा, बदला किंवा नाकारा. विनंती तुमची वाट पाहत असेल तेव्हाच बटणे दिसतात.

## पायऱ्या

1. लिस्टमधून किंवा consolidation स्क्रीनवरून विनंती उघडा.
2. वरची पट्टी, **Request Timeline** आणि **Approval History** तपासा.
3. काही नोंद असेल तर **Additional Remarks (if any)** मध्ये टाइप करा.
4. आधी साहित्य, संख्या किंवा ठिकाण बदलायचे असेल तर **Edit** दाबा.
5. मंजूर करण्यासाठी पुढच्या लेव्हलचे **Approve & Forward to** बटण दाबा (उदा. **Approve & Forward to SMD**), मग **Approve & Forward** दाबून खात्री करा.
6. नाकारण्यासाठी **Reject** दाबा, **Reason for rejection** लिहा, आणि परत **Reject** दाबा.

## मंजुरीचा क्रम

- RM पुढे SMD कडे पाठवतात, SMD पुढे SMM कडे, SMM पुढे ADMIN - MD CS कडे, ADMIN-MD CS पुढे DGM - MD CS कडे.
- DGM-MD CS अंतिम मंजुरी देतात. या टप्प्यावर **Edit** नसते.

## नेहमीच्या अडचणी

- **Approve बटण नाही, आणि स्क्रीनचे नाव "Request Details" आहे.** विनंती आता तुमच्या लेव्हलवर नाही. तुम्ही फक्त पाहू शकता, **Print** करू शकता किंवा **Download Report** करू शकता.
- **"Enter the reason for rejection."** (नाकारण्याचे कारण लिहा) कारण लिहिणे आवश्यक आहे.
- **Reject केल्यावर विनंती कायमची बंद होते.** हे परत करता येत नाही. RMDO ला नवीन विनंती करावी लागेल.
:::

::: section reviewer-edit
title: पुढे पाठवण्याआधी विनंती बदला
routes: /PublicityReview/edit/{id}
for: 
roles: 
designations: 
source-hash: a0de34efb439462f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Edit Publicity Material Request** स्क्रीनवर, ज्या मंजुरी देणाऱ्याची पाळी आहे तो विनंती दुरुस्त करू शकतो. बदल सेव्ह केल्यावर विनंती मंजूरही होते आणि पुढे पाठवली जाते.

## पायऱ्या

1. विनंतीवर **Edit** दाबा. consolidation स्क्रीनवर पेन्सिल आयकॉन टॅप करा.
2. **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** किंवा **Remarks** बदला.
3. गरज असेल तर डिझाइन फाइल जोडा किंवा काढा.
4. **State**, **Region** आणि **Headquarter (HQ)** तपासा.
5. पुढच्या लेव्हलचे **Save & Forward to** बटण दाबा (उदा. **Save & Forward to SMM**).
6. बदल सेव्ह होतो, **Approval History** मध्ये नोंदवला जातो, आणि विनंती पुढच्या लेव्हलला जाते.

## फील्डची माहिती

- **Quantity**: मंजूर केलेले नग. भरणे आवश्यक.
- **Type of Publicity Material**, **Product**, **Headquarter (HQ)**: भरणे आवश्यक.

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
source-hash: 92970d521f5da24b
confidence: ok

**ही स्क्रीन कशासाठी आहे**
consolidation स्क्रीनवर तुमच्या लेव्हलवर वाट पाहणाऱ्या सगळ्या विनंत्या रिजननुसार गट करून दिसतात. त्यामुळे तुम्ही त्या सगळ्या एकाच वेळी मंजूर करू शकता. RM ला त्यांचा रिजन दिसतो, SMD आणि SMM यांना राज्य दिसते, ADMIN-MD CS यांना संपूर्ण भारत दिसतो. DGM-MD CS ही स्क्रीन वापरत नाहीत.

## पायऱ्या

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
source-hash: afa4604b42d3cf80
confidence: ok

**ही स्क्रीन कशासाठी आहे**
अंतिम मंजुरीनंतर ADMIN-MD CS किंवा DGM-MD CS व्हेंडरकडून झालेल्या पाठवणीची नोंद **Create Dispatch** स्क्रीनवर करतात. बदली (re-initiation) पाठवणीसाठी पण हीच स्क्रीन वापरली जाते.

## पायऱ्या

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

- **Dispatch Quantity**: आता पाठवलेले नग. बदली पाठवणीसाठी ही संख्या बाकी असलेल्या नगांपासून सुरू होते.
- **Vendor**: लिस्टमधून निवडा किंवा नवीन नाव टाइप करा. भरणे आवश्यक.
- **Responsible SMDO / RM / RMDO / MO**: साहित्य घेणारा अधिकारी. भरणे आवश्यक.
- **Dispatch / Note Approval Document**: पहिल्या पाठवणीसाठी आवश्यक.
- **Delivery Address**: सेव्ह केलेला पत्ता टॅप करा किंवा पत्ता टाइप करा. भरणे आवश्यक.

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
source-hash: 377dc5ed807e6b17
confidence: ok

**ही स्क्रीन कशासाठी आहे**
पाठवणी झाल्यावर MD CS तिची प्रगती अपडेट करतात. त्यामुळे RMDO आणि मंजुरी देणाऱ्यांना साहित्य कुठे आहे ते दिसते.

## पायऱ्या

1. विनंती उघडा आणि **Tracking & Delivery** वर जा.
2. पाठवणीचे कार्ड शोधा (**Dispatch 1**, **Dispatch 2** वगैरे).
3. साहित्य व्हेंडरकडून निघाल्यावर **Mark In Transit** दाबा. **Expected Delivery Date** आणि **Current Location / Hub** टाका, मग **Save** दाबा.
4. साहित्य HQ ला पोहोचल्यावर **Mark Delivered** दाबा, गरज असेल तर **Current Location / Hub** टाका, आणि **Save** दाबा.
5. स्टॉक ट्रान्सफरसाठी: पाठवणाऱ्या (source) RMDO ने कुरिअरचा पुरावा अपलोड केल्यावर **Update Dispatch Details** दाबा. **Transporter / Courier** आणि **Tracking No. (AWB)** तपासा, मग **Save** दाबा. त्यानंतर ट्रान्सफरवर **Transfer Created** दिसते.

## फील्डची माहिती

- **Transporter / Courier** आणि **Tracking No. (AWB)**: ट्रान्सफरची खात्री करताना भरणे आवश्यक.
- **Expected Delivery Date**: साहित्य कधी पोहोचायला हवे.
- **Current Location / Hub**: साहित्य आता कुठे आहे, उदा. कुरिअर हब.

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
source-hash: 2d04fb4f99911b79
confidence: ok

**ही स्क्रीन कशासाठी आहे**
RMDO ने कमी नग किंवा खराब नग कळवले की विनंतीवर **Exception Raised** दिसते. राहिलेली संख्या कशी पाठवायची ते तुम्ही ठरवता.

## पायऱ्या

1. विनंती उघडा (लिस्टवरचे **Exceptions** कार्ड अशा विनंत्या शोधायला मदत करते) आणि **Tracking & Delivery** वर जा.
2. लाल **Exception Raised** बॉक्समध्ये **Exception Type**, **Raised By** आणि **Remarks** वाचा.
3. **Resolve Exception** दाबा. बॉक्समध्ये बाकी असलेली संख्या दिसते.
4. एक पर्याय निवडा:
5. **Re-initiate dispatch from the vendor**: **Continue to Dispatch** दाबा आणि बदली साहित्यासाठी पाठवणीचा फॉर्म भरा.
6. **Stock transfer from another HQ's excess**: **Source Request (Excess)** निवडा, **Transfer Quantity** तपासा, **Remarks** टाका, मग **Send Transfer Order** दाबा.
7. ट्रान्सफरसाठी **Transfer Order Sent Successfully** स्क्रीन उघडते. source HQ च्या RMDO ला साठा पाठवून कुरिअरचा पुरावा अपलोड करायला सांगितले जाते.
8. पुरावा आल्यावर ट्रान्सफर कार्डवरच्या **Update Dispatch Details** ने त्याची खात्री करा.

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
source-hash: d08258d028c354d8
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Notifications** स्क्रीनवर तुमच्या भागातल्या विनंत्यांबद्दलच्या सूचना दिसतात: नवीन मंजुऱ्या, पाठवण्या, अडचणी (exception) आणि स्टॉक ट्रान्सफर.

## पायऱ्या

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
