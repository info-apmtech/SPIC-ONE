---
module: publicity
title: प्रचार सामग्री ट्रैकिंग
icon: bi-megaphone
order: 40
language: hi
---

::: section overview
title: प्रचार सामग्री ट्रैकिंग कैसे काम करती है
routes: /PublicityRequest, /PublicityReview
for: 
roles: 
designations: 
source-hash: f087f488522bb49d
confidence: ok

**यह मॉड्यूल किस काम का है**
इस मॉड्यूल से आप अपने HQ के लिए प्रचार सामग्री (पोस्टर, बैनर और ऐसी ही चीज़ें) माँग सकते हैं, उसकी मंज़ूरी ले सकते हैं, सामग्री ले सकते हैं और पक्का कर सकते हैं कि वह पहुँच गई। हर स्टेप रिकॉर्ड होता है, इसलिए सबको दिखता है कि रिक्वेस्ट अभी कहाँ है।

## एक रिक्वेस्ट का सफ़र

1. RMDO रिक्वेस्ट भरता है और **Submit Request** दबाता है।
2. रिक्वेस्ट इस क्रम में मंज़ूरी के लिए जाती है: पहले RM, फिर SMD, फिर SMM, फिर ADMIN-MD CS, फिर DGM-MD CS (आख़िरी मंज़ूरी)।
3. मंज़ूरी देने वाला कोई भी व्यक्ति (DGM-MD CS को छोड़कर) आगे भेजने से पहले जानकारी बदल सकता है। मंज़ूरी देने वाला रिक्वेस्ट को **Reject** भी कर सकता है, इससे रिक्वेस्ट बंद हो जाती है।
4. आख़िरी मंज़ूरी के बाद ADMIN-MD CS (या DGM-MD CS) वेंडर से डिस्पैच बनाता है।
5. सामग्री रास्ते में इस तरह आगे बढ़ती है: **Dispatched**, फिर **In Transit**, फिर **Delivered**।
6. RMDO सामग्री जाँचता है और मिली हुई मात्रा और उसकी हालत के साथ **Acknowledgement** सबमिट करता है।
7. अगर सब कुछ सही है, तो रिक्वेस्ट **Completed** हो जाती है।
8. अगर कुछ चीज़ें कम हैं या खराब हैं, तो **Exception Raised** स्टेटस आता है। ADMIN-MD CS इसे सुलझाता है: वेंडर से नया डिस्पैच भेजकर, या किसी दूसरे HQ का ज़्यादा (बचा हुआ) स्टॉक भिजवाकर।

## कौन क्या करता है

- **RMDO**: रिक्वेस्ट बनाता है, डिलीवरी मिलने की पुष्टि करता है, ज़्यादा स्टॉक भेजने को कहा जाए तो कूरियर का सबूत भेजता है।
- **RM, SMD, SMM**: अपने रीजन या राज्य की रिक्वेस्ट जाँचते हैं और मंज़ूर करते हैं।
- **ADMIN-MD CS**: मंज़ूरी देता है, डिस्पैच बनाता है, डिलीवरी अपडेट करता है, एक्सेप्शन (समस्या) सुलझाता है।
- **DGM-MD CS**: आख़िरी मंज़ूरी देता है और डिस्पैच भी बना सकता है।

> नई मंज़ूरियाँ, डिस्पैच और ट्रांसफ़र ऑर्डर देखने के लिए लिस्ट पेज पर घंटी वाला बटन **Notifications** टैप करें।
:::

::: section requester-list
title: मेरी रिक्वेस्ट देखें और संभालें
routes: /PublicityRequest
for: 
roles: 
designations: 
source-hash: f32b121b5900212e
confidence: ok

**यह स्क्रीन किस काम की है**
**My Publicity Material Request** स्क्रीन पर आपके HQ की हर रिक्वेस्ट दिखती है: ऊपर गिनती और नीचे टेबल।

## स्टेप्स

1. ऊपर के कार्ड देखें: **Total Request**, **Draft**, **Pending Approval**, **Final Approved**, **In Dispatch**, **Acknowledgement Pending**, **Completed** और **Exception**।
2. किसी कार्ड पर टैप करें, तो सिर्फ़ उस स्टेटस वाली रिक्वेस्ट दिखेंगी। फिर से टैप करें, तो सब दिखेंगी।
3. और बारीकी से खोजने के लिए **Filters** खोलें, **Date Range**, **Product**, **Publicity Material** या **Status** चुनें, फिर **Apply Filters** दबाएँ। फ़िल्टर हटाने के लिए **Clear All** दबाएँ।
4. रिक्वेस्ट जल्दी ढूँढने के लिए सर्च बॉक्स में इंडेंट नंबर, प्रोडक्ट या सामग्री का नाम टाइप करें।
5. रिक्वेस्ट खोलने के लिए **Action** कॉलम में आँख वाले आइकन पर टैप करें।
6. **Draft** या **Returned** रिक्वेस्ट बदलने के लिए पेंसिल आइकन पर टैप करें।
7. सिर्फ़ **Draft** को मिटाने के लिए डस्टबिन आइकन पर टैप करें। मिटाने के बाद इसे वापस नहीं लाया जा सकता।
8. लिस्ट को Excel फ़ाइल में डाउनलोड करने के लिए **Export** दबाएँ।
9. नई रिक्वेस्ट शुरू करने के लिए **Create New Request** दबाएँ।

## आम दिक्कतें

- **मुझे "No requests found" दिख रहा है।** हो सकता है आपने बहुत ज़्यादा फ़िल्टर लगा रखे हों। **Clear All** दबाएँ।
- **पेंसिल आइकन नहीं है।** सबमिट करने के बाद आप रिक्वेस्ट नहीं बदल सकते, जब तक मंज़ूरी देने वाला उसे आपको लौटा न दे।
- **Export बटन ग्रे है।** लिस्ट में एक्सपोर्ट करने के लिए कोई लाइन नहीं है।
:::

::: section raise-request
title: प्रचार सामग्री की रिक्वेस्ट डालें
routes: /PublicityRequest/new, /PublicityRequest/edit/{id}
for: 
roles: 
designations: 
source-hash: f4c944427bc4be47
confidence: ok

**यह स्क्रीन किस काम की है**
अपने HQ के लिए प्रचार सामग्री माँगने के लिए **Create Publicity Material Request** स्क्रीन का इस्तेमाल करें। आप इसे ड्राफ़्ट में सेव करके बाद में भेज सकते हैं।

## स्टेप्स

1. लिस्ट पेज पर **Create New Request** दबाएँ।
2. **Request Details** में **Type of Publicity Material** और **Product** चुनें।
3. आपको जितनी **Quantity** चाहिए, वह भरें।
4. **Requested By** देख लें। इसमें आपका नाम पहले से भरा होता है।
5. अगर आपके पास डिज़ाइन है, तो फ़ाइल को अपलोड बॉक्स में डालें या **or click to browse** पर टैप करें।
6. ज़रूरत हो तो **Remarks** लिखें।
7. **Location** में **State**, **Region** और **Headquarter (HQ)** देख लें। ये आपकी प्रोफ़ाइल से भरे जाते हैं।
8. बाद के लिए रखने को **Save as Draft** दबाएँ, या RM को भेजने के लिए **Submit Request** दबाएँ।
9. पुष्टि वाले बॉक्स में **Submit** दबाएँ।

## फ़ील्ड का मतलब

- **Type of Publicity Material**: आपको जो चीज़ चाहिए, जैसे पोस्टर या बैनर। ज़रूरी है।
- **Product**: जिस प्रोडक्ट के लिए सामग्री है। आप टाइप करके खोज सकते हैं। ज़रूरी है।
- **Quantity**: कितने पीस चाहिए। ज़रूरी है।
- **Requested By**: वह व्यक्ति जो सामग्री माँग रहा है।
- **Design file**: एक फ़ाइल, PDF, JPG, PNG, DOC या DOCX, 10 MB तक।
- **State**, **Region**, **Headquarter (HQ)**: सामग्री कहाँ पहुँचानी है। ज़रूरी है।

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
source-hash: 2d926f137adb7d61
confidence: ok

**यह स्क्रीन किस काम की है**
**My Publicity Material Request Details** स्क्रीन पर एक रिक्वेस्ट की पूरी जानकारी दिखती है: वह अभी कहाँ है, किसने मंज़ूर किया, डिस्पैच और दस्तावेज़।

## स्टेप्स

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
source-hash: 3642d2f43fd54ffa
confidence: ok

**यह स्क्रीन किस काम की है**
जब सामग्री आपके HQ पहुँचे, तो आपको बताना होगा कि कितने पीस मिले और उनकी हालत कैसी है। इससे रिक्वेस्ट बंद हो जाती है, या ADMIN-MD CS को समस्या की खबर जाती है।

## स्टेप्स

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

- **Quantity Received**: असल में गिने गए पीस। ज़रूरी है।
- **Material Condition**: अच्छी या खराब। ज़रूरी है।
- **Damaged Quantity**: 1 से लेकर मिली हुई मात्रा तक।
- **Acknowledged By**: वह व्यक्ति जिसने सामग्री ली। ज़रूरी है।
- **Photos**: मिली हुई सामग्री की तस्वीरें, हर एक 10 MB तक।

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
source-hash: 5b649ad478721aa8
confidence: ok

**यह स्क्रीन किस काम की है**
अगर आपके HQ को ज़रूरत से ज़्यादा सामग्री मिली है, तो ADMIN-MD CS आपसे उसमें से कुछ सामग्री किसी ऐसे दूसरे HQ को भेजने को कह सकता है जहाँ कमी है। आप इसे कूरियर से भेजते हैं और कूरियर की रसीद यहाँ अपलोड करते हैं।

## स्टेप्स

1. **Notifications** खोलें और स्टॉक ट्रांसफ़र ऑर्डर पर टैप करें, फिर **View Transfer** दबाएँ। आप अपनी रिक्वेस्ट खोलकर, **Tracking & Delivery** पर जाकर, **Stock Transfer Order** कार्ड पर **Create Transfer** भी दबा सकते हैं।
2. **Read Information (Read Only)** वाला हिस्सा देखें: **Destination HQ** और **Transfer Quantity**।
3. सामग्री पैक करें और कूरियर से भेजें।
4. **Upload Courier / Dispatch Proof** में कूरियर रसीद की साफ़ फ़ोटो अपलोड करें।
5. ऐप रसीद पढ़कर **Courier / Transporter**, **Tracking No. / AWB** और **Dispatch Date** भर देता है। इन्हें जाँचें और ज़रूरत हो तो ठीक करें।
6. अगर आपके पास हों, तो **Vehicle No** और **Remarks (Optional)** भरें।
7. **Submit Proof to Admin** दबाएँ।
8. **Dispatch Proof Submitted Successfully!** स्क्रीन खुलती है। **Go to Tracking & Delivery** या **Back to List** दबाएँ।

## फ़ील्ड का मतलब

- **Courier / Transporter**: कूरियर कंपनी का नाम। ज़रूरी है।
- **Tracking No. / AWB**: कूरियर रसीद पर लिखा नंबर। ज़रूरी है।
- **Dispatch Date**: जिस दिन आपने भेजा। ज़रूरी है।

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
source-hash: 3f380e63b29f62fe
confidence: ok

**यह स्क्रीन किस काम की है**
**Notifications** स्क्रीन पर आपकी रिक्वेस्ट से जुड़े अलर्ट दिखते हैं: लौटाई गई रिक्वेस्ट, डिस्पैच, एक्सेप्शन और स्टॉक ट्रांसफ़र ऑर्डर।

## स्टेप्स

1. लिस्ट पेज पर **Notifications** दबाएँ।
2. एक टैब चुनें: **All**, **Stock Transfer**, **Exception** या **Dispatch & Approvals**।
3. बिना पढ़े आइटम पर एक बिंदी होती है। किसी आइटम की जानकारी खोलने के लिए उस पर टैप करें।
4. ट्रांसफ़र ऑर्डर के लिए **Transfer Order Details**, **Source Excess Reference** और **Reason for Transfer** देखें।
5. कूरियर का सबूत अपलोड करने के लिए **View Transfer** दबाएँ, या रिक्वेस्ट खोलने के लिए **View Details** दबाएँ।
6. एक आइटम के लिए **Mark As Read** दबाएँ, या सबके लिए **Mark All As Read**।
7. वापस जाने के लिए **Back to List Page** दबाएँ।

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
source-hash: 7ebd8194aa3f5ebd
confidence: ok

**यह स्क्रीन किस काम की है**
मंज़ूरी देने वाले और MD CS स्टाफ़ इस लिस्ट में अपने इलाके की रिक्वेस्ट देखते हैं। इसका टाइटल **Submitted Requests** है (RM के लिए यह **My Publicity Material Request** है)।

## स्टेप्स

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
source-hash: c7fff9b7b3926213
confidence: ok

**यह स्क्रीन किस काम की है**
रिक्वेस्ट खोलकर उसे जाँचें, और फिर मंज़ूर करें, बदलें या रिजेक्ट करें। बटन तभी दिखते हैं जब रिक्वेस्ट आपके पास इंतज़ार कर रही हो।

## स्टेप्स

1. लिस्ट या कंसॉलिडेशन स्क्रीन से रिक्वेस्ट खोलें।
2. ऊपर की पट्टी, **Request Timeline** और **Approval History** देखें।
3. **Additional Remarks (if any)** में कोई भी नोट लिखें।
4. पहले सामग्री, मात्रा या जगह बदलनी हो, तो **Edit** दबाएँ।
5. मंज़ूर करने के लिए अगले लेवल वाला **Approve & Forward to** दबाएँ (जैसे **Approve & Forward to SMD**), फिर **Approve & Forward** से पुष्टि करें।
6. मना करने के लिए **Reject** दबाएँ, **Reason for rejection** लिखें, और फिर से **Reject** दबाएँ।

## मंज़ूरी का क्रम

- RM, SMD को भेजता है; SMD, SMM को; SMM, ADMIN - MD CS को; ADMIN-MD CS, DGM - MD CS को।
- DGM-MD CS आख़िरी मंज़ूरी देता है। इस स्टेज पर **Edit** नहीं होता।

## आम दिक्कतें

- **Approve बटन नहीं है, टाइटल में "Request Details" लिखा है।** रिक्वेस्ट अभी आपके लेवल पर नहीं है। आप सिर्फ़ देख सकते हैं, **Print** कर सकते हैं या **Download Report** कर सकते हैं।
- **"Enter the reason for rejection."** (रिजेक्ट करने का कारण लिखें) कारण लिखना ज़रूरी है।
- **Reject करने से रिक्वेस्ट हमेशा के लिए बंद हो जाती है।** इसे वापस नहीं किया जा सकता। RMDO को नई रिक्वेस्ट डालनी होगी।
:::

::: section reviewer-edit
title: आगे भेजने से पहले रिक्वेस्ट बदलें
routes: /PublicityReview/edit/{id}
for: 
roles: 
designations: 
source-hash: a0de34efb439462f
confidence: ok

**यह स्क्रीन किस काम की है**
**Edit Publicity Material Request** स्क्रीन पर वह मंज़ूरी देने वाला, जिसकी अभी बारी है, रिक्वेस्ट ठीक कर सकता है। बदलाव सेव करने से रिक्वेस्ट मंज़ूर होकर आगे भी चली जाती है।

## स्टेप्स

1. रिक्वेस्ट पर **Edit** दबाएँ। कंसॉलिडेशन स्क्रीन पर पेंसिल आइकन पर टैप करें।
2. **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** या **Remarks** बदलें।
3. ज़रूरत हो तो डिज़ाइन फ़ाइल जोड़ें या हटाएँ।
4. **State**, **Region** और **Headquarter (HQ)** देख लें।
5. अगले लेवल वाला **Save & Forward to** दबाएँ (जैसे **Save & Forward to SMM**)।
6. बदलाव सेव हो जाता है, **Approval History** में लिखा जाता है, और रिक्वेस्ट अगले लेवल पर चली जाती है।

## फ़ील्ड का मतलब

- **Quantity**: मंज़ूर किए गए पीस की संख्या। ज़रूरी है।
- **Type of Publicity Material**, **Product**, **Headquarter (HQ)**: ज़रूरी हैं।

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
source-hash: 92970d521f5da24b
confidence: ok

**यह स्क्रीन किस काम की है**
कंसॉलिडेशन स्क्रीन पर आपके लेवल पर इंतज़ार कर रही हर रिक्वेस्ट रीजन के हिसाब से दिखती है, ताकि आप सबको एक ही बार में मंज़ूर कर सकें। RM को अपना रीजन दिखता है, SMD और SMM को राज्य, और ADMIN-MD CS को पूरा भारत। DGM-MD CS यह स्क्रीन इस्तेमाल नहीं करता।

## स्टेप्स

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
source-hash: afa4604b42d3cf80
confidence: ok

**यह स्क्रीन किस काम की है**
आख़िरी मंज़ूरी के बाद ADMIN-MD CS या DGM-MD CS **Create Dispatch** स्क्रीन पर वेंडर से हुए डिस्पैच को दर्ज करता है। यही स्क्रीन बदले में भेजे जाने वाले (re-initiation) डिस्पैच के लिए भी इस्तेमाल होती है।

## स्टेप्स

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

- **Dispatch Quantity**: अभी भेजे जा रहे पीस। बदले वाले डिस्पैच में यह बाकी बची मात्रा से शुरू होती है।
- **Vendor**: लिस्ट से चुनें या नया नाम टाइप करें। ज़रूरी है।
- **Responsible SMDO / RM / RMDO / MO**: वह अधिकारी जो सामग्री लेगा। ज़रूरी है।
- **Dispatch / Note Approval Document**: पहले डिस्पैच के लिए ज़रूरी है।
- **Delivery Address**: सेव किए गए पते पर टैप करें या पता टाइप करें। ज़रूरी है।

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
source-hash: 377dc5ed807e6b17
confidence: ok

**यह स्क्रीन किस काम की है**
डिस्पैच बनने के बाद MD CS उसकी प्रगति अपडेट करता है, ताकि RMDO और मंज़ूरी देने वाले देख सकें कि सामग्री कहाँ है।

## स्टेप्स

1. रिक्वेस्ट खोलें और **Tracking & Delivery** पर जाएँ।
2. डिस्पैच कार्ड ढूँढें (**Dispatch 1**, **Dispatch 2** वगैरह)।
3. जब सामग्री वेंडर के यहाँ से निकले, तो **Mark In Transit** दबाएँ। **Expected Delivery Date** और **Current Location / Hub** भरें, फिर **Save** दबाएँ।
4. जब वह HQ पहुँच जाए, तो **Mark Delivered** दबाएँ, ज़रूरत हो तो **Current Location / Hub** भरें, और **Save** दबाएँ।
5. स्टॉक ट्रांसफ़र में, जब भेजने वाले (source) RMDO ने कूरियर का सबूत अपलोड कर दिया हो, तो **Update Dispatch Details** दबाएँ। **Transporter / Courier** और **Tracking No. (AWB)** जाँचें, फिर **Save** दबाएँ। इसके बाद ट्रांसफ़र पर **Transfer Created** दिखता है।

## फ़ील्ड का मतलब

- **Transporter / Courier** और **Tracking No. (AWB)**: ट्रांसफ़र की पुष्टि करते समय ज़रूरी हैं।
- **Expected Delivery Date**: सामग्री कब तक पहुँचनी चाहिए।
- **Current Location / Hub**: सामग्री अभी कहाँ है, जैसे कोई कूरियर हब।

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
source-hash: 2d04fb4f99911b79
confidence: ok

**यह स्क्रीन किस काम की है**
जब RMDO बताता है कि पीस कम मिले या खराब मिले, तो रिक्वेस्ट पर **Exception Raised** दिखता है। आप तय करते हैं कि कम पड़ी मात्रा कैसे भेजनी है।

## स्टेप्स

1. रिक्वेस्ट खोलें (लिस्ट पर **Exceptions** कार्ड से ऐसी रिक्वेस्ट ढूँढने में मदद मिलती है) और **Tracking & Delivery** पर जाएँ।
2. लाल **Exception Raised** बॉक्स में **Exception Type**, **Raised By** और **Remarks** पढ़ें।
3. **Resolve Exception** दबाएँ। बॉक्स में बाकी बची मात्रा दिखती है।
4. एक विकल्प चुनें:
5. **Re-initiate dispatch from the vendor**: **Continue to Dispatch** दबाएँ और बदले की सामग्री के लिए डिस्पैच फ़ॉर्म भरें।
6. **Stock transfer from another HQ's excess**: **Source Request (Excess)** चुनें, **Transfer Quantity** देखें, **Remarks** लिखें, फिर **Send Transfer Order** दबाएँ।
7. ट्रांसफ़र के लिए **Transfer Order Sent Successfully** स्क्रीन खुलती है। भेजने वाले HQ के RMDO को स्टॉक भेजने और कूरियर का सबूत अपलोड करने के लिए कहा जाता है।
8. सबूत आने पर ट्रांसफ़र कार्ड पर **Update Dispatch Details** से उसकी पुष्टि करें।

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
source-hash: d08258d028c354d8
confidence: ok

**यह स्क्रीन किस काम की है**
**Notifications** स्क्रीन पर आपके इलाके की रिक्वेस्ट से जुड़े अलर्ट दिखते हैं: नई मंज़ूरियाँ, डिस्पैच, एक्सेप्शन और स्टॉक ट्रांसफ़र।

## स्टेप्स

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
