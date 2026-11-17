---
module: lab
title: SAS मिट्टी जाँच लैब
icon: bi-eyedropper
order: 60
language: hi
category: मिट्टी की जाँच
note: नया रूप, साथ में लैब का सफ़र, स्टेटस और गाइडेड टूर
meta-source-hash: 3e2288cb66a7506f
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: f1de7a25d0e196a5
confidence: ok

लैब पोर्टल मिट्टी और पानी के हर सैंपल को उस दिन से ट्रैक करता है जिस दिन वह लैब पहुँचता है, तब तक जब तक किसान को रिपोर्ट नहीं मिल जाती। हर व्यक्ति को सिर्फ़ अपने काम की स्क्रीन दिखती हैं।

- receive | icon: bi-box-arrow-in-down | color: #0d9488
  title: पार्सल लेना और बैच बनाना
  text: कोऑर्डिनेटर हर पार्सल लेता है और सैंपल को एनालिस्ट के लिए बैच में डालता है।
- test | icon: bi-eyedropper | color: #7c3aed
  title: जाँच करना और वैल्यू भरना
  text: एनालिस्ट टेस्ट वैल्यू टाइप करता है। नतीजे और सलाह सिस्टम खुद निकालता है।
- report | icon: bi-file-earmark-pdf | color: #16a34a
  title: कई भाषाओं में रिपोर्ट
  text: पूरे हुए बैच से हर सैंपल की एक रिपोर्ट बनती है, अंग्रेज़ी और भारतीय भाषाओं में।
:::

::: section flow
title: कंसाइनमेंट लैब में कैसे आगे बढ़ता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 998b226207ae8a8b
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- field | type: lane | color: #64748b | roles: MDO, JMDO
  title: फ़ील्ड स्टाफ़
  sub: MDO · JMDO
- coordinator | type: lane | color: #0d9488
  title: लैब कोऑर्डिनेटर
  sub: पार्सल लेता है और बैच बनाता है
- analyst | type: lane | color: #7c3aed
  title: लैब एनालिस्ट
  sub: टेस्ट वैल्यू भरता है
- farmer | type: lane | color: #16a34a | roles: Farmer
  title: किसान
  sub: रिपोर्ट पाता है
- send | lane: field | col: 1 | icon: bi-truck | color: #64748b | next: receive
  title: पार्सल भेजें
  status: In Transit
  text: फ़ील्ड स्टाफ़ सैंपल को कंसाइनमेंट के रूप में कूरियर से लैब भेजते हैं।
- receive | lane: coordinator | col: 2 | icon: bi-box-arrow-in-down | color: #0d9488 | next: batch | screen: receive-consignments
  title: मिला हुआ मार्क करें
  status: Batch Pending
  text: पार्सल पहुँचने पर कोऑर्डिनेटर **Mark received** दबाता है।
- batch | lane: coordinator | col: 3 | icon: bi-collection | color: #ea580c | next: test | screen: create-batch
  title: बैच बनाएँ
  status: Taken for Analysis
  text: कोऑर्डिनेटर कंसाइनमेंट को एक बैच में डालता है और उसे किसी एनालिस्ट को सौंपता है।
- test | lane: analyst | col: 4 | icon: bi-eyedropper | color: #7c3aed | next: complete | screen: enter-test-values
  title: टेस्ट वैल्यू भरें
  status: Auto Result Ready
  text: एनालिस्ट हर सैंपल की वैल्यू टाइप करता है और **Submit Values** दबाता है। नतीजा, स्टेटस और सलाह अपने-आप भर जाते हैं।
- complete | lane: coordinator | col: 5 | icon: bi-patch-check | color: #2563eb | next: report | screen: batch-details
  title: बैच पूरा करें
  status: Report Generated
  text: कोऑर्डिनेटर **Complete Batch** दबाता है। वैल्यू लॉक हो जाती हैं और हर सैंपल की एक रिपोर्ट बनती है।
- report | lane: farmer | col: 6 | icon: bi-file-earmark-arrow-down | color: #16a34a
  title: किसान को रिपोर्ट
  status: Completed
  text: किसान के लिए रिपोर्ट PDF या Excel में डाउनलोड की जाती है। किसान ऐप में भी इसे डाउनलोड कर सकता है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 670249fe2e3bdb4d
confidence: ok

- in-transit | color: #64748b
  label: In Transit
  text: पार्सल लैब के रास्ते में है।
- batch-pending | color: #d97706
  label: Batch Pending
  text: मिल गया है, बैच में डाले जाने का इंतज़ार है।
- batch-created | color: #0891b2
  label: Batch Created
  text: बैच में है, अभी कोई एनालिस्ट नहीं सौंपा गया।
- taken | color: #ea580c
  label: Taken for Analysis
  text: किसी एनालिस्ट को सौंप दिया गया है।
- pending-entry | color: #d97706
  label: Pending Entry
  text: एनालिस्ट ने अभी वैल्यू नहीं भरी हैं।
- in-progress | color: #7c3aed
  label: In Progress
  text: कुछ वैल्यू सेव हो चुकी हैं।
- auto-result | color: #2563eb
  label: Auto Result Ready
  text: सारी वैल्यू सबमिट हो गईं; नतीजे निकल गए हैं।
- report | color: #16a34a
  label: Report Generated
  text: बैच पूरा हो गया है और रिपोर्ट तैयार हैं।
- delayed | color: #dc2626
  label: Delayed
  text: 5 दिन से ज़्यादा हो गए और जाँच पूरी नहीं हुई।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: a82220da9f08df6e
confidence: ok

जब आपका रोल मेल खाता है, तो आपका कार्ड हाइलाइट होता है।

- coordinator | color: #0d9488
  title: लैब कोऑर्डिनेटर
  - कंसाइनमेंट लेना
  - बैच बनाना और एनालिस्ट को सौंपना
  - बैच पूरे करना और रिपोर्ट डाउनलोड करना
- analyst | color: #7c3aed
  title: लैब एनालिस्ट
  - अपने बैच की टेस्ट वैल्यू भरना
  - सैंपल रिपोर्ट डाउनलोड करना
- admin | color: #2563eb | roles: Admin
  title: लैब एडमिन
  - Lab Tracking पर सारे बैच देखना
  - सिर्फ़ देखने के लिए
- field | color: #64748b | roles: MDO, JMDO
  title: फ़ील्ड स्टाफ़
  - सैंपल लैब को भेजना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: d8c921211d44aab6
confidence: ok

- no-lab-menu | popular: true
  q: मुझे लैब मेन्यू नहीं दिख रहा
  a: लैब की स्क्रीन तभी खुलती हैं जब आपके पद (designation) में लैब पेज हों। Admin के लिए भी यह ज़रूरी है। अपने एडमिनिस्ट्रेटर से पूछें।
- no-receive
  q: Mark received बटन नहीं है
  a: सिर्फ़ कोऑर्डिनेटर ही कंसाइनमेंट ले सकते हैं, और सिर्फ़ वही कंसाइनमेंट जो **In Transit** हैं।
- delayed | popular: true
  q: बैच पर Delayed दिख रहा है, पर एनालिस्ट ने काम पूरा कर दिया
  a: यह तब तक Delayed रहता है जब तक सारे सैंपल सबमिट होकर जाँच पूरी नहीं हो जाती।
- edit-values
  q: क्या सबमिट करने के बाद मैं कोई वैल्यू बदल सकता हूँ?
  a: हाँ। सैंपल पर **Edit Values** दबाएँ। यह सिर्फ़ तब तक चलता है जब तक बैच पूरा नहीं हो जाता।
:::

::: section coordinator-dashboard
title: लैब कोऑर्डिनेटर डैशबोर्ड
routes: /Lab
for: 
roles: 
designations: 
screenshot: coordinator-dashboard.png
screenshot-phone: coordinator-dashboard.phone.jpg
minutes: 1
source-hash: edeeb0d1e27dd744
confidence: ok

**यह स्क्रीन किस काम की है**
**Lab Coordinator Dashboard** से लैब की झलक जल्दी मिल जाती है: कितने कंसाइनमेंट आए, कितने अभी बैच का इंतज़ार कर रहे हैं और जाँच कहाँ तक पहुँची। जब आप **Lab** खोलते हैं, तो ऐप कोऑर्डिनेटर को यही डैशबोर्ड दिखाता है। एनालिस्ट इसकी जगह अपने अलग डैशबोर्ड पर पहुँचते हैं।

1. ऊपर के कार्ड देखें: **Total Consignments Received**, **Pending Batch Creation**, **Total Batches Created**, **Taken for Analysis**, **Analysis Completed**, **Completed Batches** और **Reports Generated**।
2. **Recent Consignments** टेबल में आख़िरी पाँच कंसाइनमेंट उनके **Status** (**Batch Pending** या **Batch Created**) के साथ दिखते हैं।
3. वह कंसाइनमेंट खोलने के लिए कंसाइनमेंट नंबर या आँख वाले आइकन पर टैप करें।
4. पूरी **Consignment Details** लिस्ट खोलने के लिए **Recent Consignments** के बगल में **View All** दबाएँ।
5. **Recently Created Batches** टेबल में आख़िरी पाँच बैच, **Assigned To** में एनालिस्ट, **Current Status** और **Analysis Days** दिखते हैं।
6. **Analysis Tracking** खोलने के लिए **Recently Created Batches** के बगल में **View All** दबाएँ।

## आम दिक्कतें

- **मुझे "No batches yet" दिख रहा है।** अभी कोई बैच नहीं बना है। Batch Pending कंसाइनमेंट देखने के लिए **Go to consignments** दबाएँ।
- **मुझे "The dashboard could not be loaded" दिख रहा है।** अपना इंटरनेट कनेक्शन जाँचें और **Try again** दबाएँ।
- **मैं मेन Dashboard पर पहुँच गया।** आपके पद में कोऑर्डिनेटर या एनालिस्ट वाले लैब पेज नहीं हैं। अपने एडमिनिस्ट्रेटर से पूछें।
:::

::: section receive-consignments
title: लैब में कंसाइनमेंट लें
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
screenshot: receive-consignments.png
screenshot-phone: receive-consignments.phone.jpg
minutes: 2
source-hash: 93cb533d61543543
confidence: ok

**यह स्क्रीन किस काम की है**
**Consignment Details** स्क्रीन पर लैब को भेजा गया हर कंसाइनमेंट दिखता है। पार्सल को मिला हुआ मार्क करने और उसके अंदर क्या है यह देखने के लिए इसका इस्तेमाल करें।

1. ऊपर के कार्ड देखें: **Total Consignments**, **Pending Batch Creation**, **Batched Consignments**, **Water Samples** और **Soil Samples**।
2. कोई कंसाइनमेंट ढूँढने के लिए सर्च बॉक्स में कंसाइनमेंट नंबर या AWB नंबर टाइप करें, या **All Statuses** में कोई स्टेटस चुनें।
3. पार्सल पहुँचने पर **In Transit** स्टेटस वाली उसकी लाइन ढूँढें।
4. **Action** कॉलम में रिसीव आइकन पर टैप करें (फ़ोन पर **Mark received** दबाएँ)।
5. सवाल वाले बॉक्स में फिर से **Mark received** दबाएँ। स्टेटस **Batch Pending** हो जाता है और उसके सैंपल बैच के लिए तैयार हो जाते हैं।
6. पूरी जानकारी देखने के लिए कंसाइनमेंट नंबर या आँख वाले आइकन पर टैप करें।
7. जानकारी वाली स्क्रीन पर आपको सैंपल की गिनती, State, Region और **Head Quarters**, **History / Timeline**, **Collections**, **Samples** और **Courier & Package** की जानकारी दिखती है।
8. डिस्पैच के समय ली गई फ़ोटो देखने के लिए **Consignment Photos** दबाएँ। वह सैंपल कलेक्शन खोलने के लिए किसी **Collection No.** पर टैप करें।
9. अगर कंसाइनमेंट पहले से किसी बैच में है, तो बैच खोलने के लिए **View Batch Details** दबाएँ।

## आम दिक्कतें

- **Mark received बटन नहीं है।** सिर्फ़ कोऑर्डिनेटर ही कंसाइनमेंट ले सकते हैं, और सिर्फ़ वही कंसाइनमेंट जो **In Transit** हैं।
- **मुझे "No consignments found" दिख रहा है।** आपकी सर्च या स्टेटस फ़िल्टर बहुत कड़ा है। **Clear filters** दबाएँ।
- **Consignment Photos ग्रे है।** पार्सल भेजते समय कोई फ़ोटो नहीं जोड़ी गई थी।
:::

::: section create-batch
title: कंसाइनमेंट से बैच बनाएँ
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: dcc40dda2df92781
confidence: ok

**यह स्क्रीन किस काम की है**
बैच सैंपल का एक समूह है जिसकी जाँच एक एनालिस्ट एक साथ करता है। एक बैच में कई कंसाइनमेंट हो सकते हैं। आप इसे **Consignment Details** लिस्ट से बनाते हैं।

1. **Consignment Details** खोलें। सिर्फ़ बैच का इंतज़ार कर रहे कंसाइनमेंट देखने के लिए **All Statuses** में **Batch Pending** चुनें।
2. बैच में जो कंसाइनमेंट चाहिए, उनमें से हर एक की लाइन की शुरुआत वाले बॉक्स पर टिक करें। सबसे ऊपर वाला बॉक्स पेज की सारी Batch Pending लाइनों पर टिक कर देता है।
3. **Create Batch (n)** दबाएँ। नंबर बताता है कि आपने कितनों पर टिक किया। किसी कंसाइनमेंट की अपनी स्क्रीन पर इसकी जगह **Create Batch** दबाएँ।
4. **Create Batch** फ़ॉर्म भरें (नीचे देखें) और **Confirm** दबाएँ।
5. **Batch Created Successfully** बॉक्स में नया बैच नंबर दिखता है। बैच खोलने के लिए **View Batch Details** दबाएँ, या सारे बैच देखने के लिए **Go To Analysis Tracking** दबाएँ।

## फ़ील्ड का मतलब

- **Batch No**: Confirm दबाने के बाद सिस्टम खुद बनाता है।
- **Batch Created Date**: अपने-आप आज की तारीख रहती है। आगे की (भविष्य की) तारीख नहीं चलेगी।
- **Consignment No.**: टिक किए गए कंसाइनमेंट चिप के रूप में दिखते हैं। किसी चिप को हटाने के लिए उस पर छोटा क्रॉस दबाएँ, या कोई और Batch Pending कंसाइनमेंट जोड़ने के लिए बॉक्स में टाइप करें।
- **Assign To**: वह एनालिस्ट जो सैंपल की जाँच करेगा। अगर आप अभी चुनते हैं, तो बैच सीधे **Taken for Analysis** में चला जाता है। आप इसे खाली छोड़कर बाद में भी सौंप सकते हैं।
- **Priority**: **Low**, **Medium** (डिफ़ॉल्ट) या **High**। एनालिस्ट को यह प्राथमिकता दिखती है।
- **Remarks**: बैच के लिए कोई भी नोट।

## आम दिक्कतें

- **"Pick at least one consignment."** (कम से कम एक कंसाइनमेंट चुनें) Confirm दबाने से पहले कम से कम एक कंसाइनमेंट जोड़ें।
- **टिक करने वाले बॉक्स नहीं हैं।** सिर्फ़ **Batch Pending** लाइनों पर टिक हो सकता है, और बॉक्स सिर्फ़ कोऑर्डिनेटर को दिखते हैं।
:::

::: section analysis-tracking
title: जाँच में चल रहे बैच ट्रैक करें
routes: /Lab/Analysis
for: 
roles: 
designations: 
screenshot: analysis-tracking.png
hotspots: 1 @ 3.7,13.6,92.5,21.5 | 2 @ 16.1,39.1,78.2,5.3 | 3 @ 76.5,39.1,17.8,5.3 | 4 @ 5.7,46,88.6,54 | 5 @ 5.7,46,88.6,54
screenshot-phone: analysis-tracking.phone.jpg
hotspots-phone: 1 @ 6.2,7.4,87.7,27.5 | 2 @ 10,39.1,80,6.8 | 3 @ 10,42.8,80,3 | 4 @ 2,2,8,4 | 5 @ 2,2,8,4
tour: 1 > [data-help=lab-at-cards] | 2 > [data-help=lab-at-search] | 3 > [data-help=lab-at-status] | 4 > [data-help=lab-at-table] | 5 > [data-help=lab-at-table]
minutes: 2
source-hash: 38d1f11b380940ea
confidence: ok

**यह स्क्रीन किस काम की है**
**Analysis Tracking** स्क्रीन पर हर बैच दिखता है, ताकि आप उसे बनने से लेकर जाँच पूरी होने तक देख सकें और देर से चल रहे बैच पहचान सकें।

1. ऊपर के कार्ड देखें: **Total Batches**, **Created**, **Taken for Analysis**, **Analysis Completed**, **Completed** और **Delayed Analysis**।
2. सर्च बॉक्स में बैच नंबर, कंसाइनमेंट नंबर या एनालिस्ट का नाम टाइप करें।
3. **All Statuses** में कोई स्टेटस चुनें। सिर्फ़ देर वाले बैच देखने के लिए **Delayed** चुनें।
4. कौन-से बैच समय पर हैं, यह देखने के लिए **Analysis Days** कॉलम पढ़ें (नीचे की टेबल देखें)।
5. **Batch Details** खोलने के लिए बैच नंबर या आँख वाले आइकन पर टैप करें। और बैच देखने के लिए नीचे के पेज नंबर इस्तेमाल करें।

## Analysis Days का मतलब

| क्या दिखता है | मतलब |
|---|---|
| **Pending** (पीला) | जाँच शुरू नहीं हुई है। |
| हरे रंग में दिनों की संख्या | जाँच समय पर चल रही है। |
| लाल रंग में दिनों की संख्या | बैच में देर हो रही है (आम तौर पर 5 दिन से ज़्यादा हो गए और काम पूरा नहीं हुआ)। |

## आम दिक्कतें

- **मुझे "No batches found" दिख रहा है।** स्टेटस या सर्च बदलें, या **Clear filters** दबाएँ। अगर कोई भी बैच नहीं है, तो **Go to consignments** दबाएँ और एक बैच बनाएँ।
- **बैच पर Delayed दिख रहा है, पर एनालिस्ट ने काम पूरा कर दिया।** यह तब तक Delayed रहता है जब तक सारे सैंपल सबमिट होकर जाँच पूरी नहीं हो जाती।
:::

::: section batch-details
title: बैच सौंपें, उस पर नज़र रखें और पूरा करें
routes: /Lab/Analysis/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: 16c6bd2291162e28
confidence: ok

**यह स्क्रीन किस काम की है**
**Batch Details** स्क्रीन पर एक बैच की पूरी जानकारी दिखती है: उसके सैंपल, टेस्ट पैरामीटर, दस्तावेज़ और इतिहास। कोऑर्डिनेटर यहीं एनालिस्ट को काम सौंपते हैं और बैच पूरा करते हैं।

1. ऊपर की पट्टी में **Batch No.**, **Consignment No.**, **Total Samples**, **Assigned Date**, **Current Status** और **Analysis Days** दिखते हैं।
2. टैब इस्तेमाल करें: **Overview** (प्रगति, सैंपल समरी, टाइमलाइन), **Samples**, **Parameters**, **Documents** और **Activity Log**।
3. बैच किसी एनालिस्ट को देने के लिए **Assign** दबाएँ (या एनालिस्ट बदलने के लिए **Reassign**)। **Assign To** और **Priority** चुनें, फिर **Save** दबाएँ।
4. अगर बैच में एनालिस्ट है पर काम शुरू नहीं हुआ, तो **Take for Analysis** दबाएँ और पुष्टि करें।
5. **Samples** टैब पर किसी सैंपल पर टैप करके उसकी टेस्ट वैल्यू देखें। लिस्ट डाउनलोड करने के लिए **Export** दबाएँ।
6. **Documents** टैब पर **Upload** दबाएँ, **Files**, **Document Type** और **Description** चुनें, फिर से **Upload** दबाएँ। किसी दस्तावेज़ को डाउनलोड करने, देखने या मिटाने के लिए आइकन इस्तेमाल करें।
7. जब हर सैंपल सबमिट हो जाए, तो एक नोट में "Every sample is completed" लिखा आता है। **Complete Batch** दबाएँ।
8. **Final Remarks** लिखें और **Complete Batch** दबाएँ। वैल्यू लॉक हो जाती हैं और रिपोर्ट बन जाती हैं।

## आम दिक्कतें

- **Complete Batch बटन नहीं है।** कुछ सैंपल अभी सबमिट नहीं हुए हैं। **Samples** टैब में **Not Started** या **In Progress** देखें।
- **बटन नहीं दिख रहे।** बैच पहले ही **Completed** है, या आपके पास कोऑर्डिनेटर के अधिकार नहीं हैं।
- **फ़ाइल नहीं जुड़ी।** फ़ाइलें PDF, JPG, PNG, WEBP, XLSX या DOCX होनी चाहिए, हर एक 20 MB तक, और एक बार में 10 तक।
:::

::: section lab-reports
title: लैब रिपोर्ट डाउनलोड करें
routes: /Lab/Reports
for: 
roles: 
designations: 
screenshot: lab-reports.png
screenshot-phone: lab-reports.phone.jpg
minutes: 2
source-hash: bbe4683cfa84f8af
confidence: ok

**यह स्क्रीन किस काम की है**
**Lab Reports** स्क्रीन पर वे रिपोर्ट दिखती हैं जो बैच पूरा होने पर बनती हैं। यहाँ से आप किसी रिपोर्ट की जानकारी खोल सकते हैं और उसे PDF या Excel में उस भाषा में डाउनलोड कर सकते हैं जो किसान पढ़ता है।

1. ऊपर के कार्ड देखें: **Total Reports**, **Batch Reports**, **Sample Reports**, **Generated Today** और **Downloads Completed**।
2. लिस्ट छोटी करने के लिए **Filters** दबाएँ। **From** और **To** तारीखें, **State**, **Sample Type** या **Financial Year** चुनें, फिर **Apply Filters** दबाएँ। इन्हें हटाने के लिए **Clear All** दबाएँ।
3. सर्च बॉक्स में बैच, कंसाइनमेंट या रिपोर्ट नंबर टाइप करें।
4. **Lab Report Details** खोलने के लिए आँख वाले आइकन (फ़ोन पर **View Report**) पर टैप करें।
5. जानकारी में **Report Summary**, **Batch Details**, **Consignment Details**, **Analysis Details** और **Status Timeline** दिखते हैं।
6. बैच रिपोर्ट अंग्रेज़ी में लेने के लिए **Download PDF** दबाएँ। दूसरी भाषा के लिए उसके बगल वाला छोटा तीर दबाएँ और लिस्ट से **Tamil**, **Telugu**, **Marathi** या कोई और भाषा चुनें।
7. इसी तरह **Download Excel** दबाएँ। आपको हर सैंपल रिपोर्ट की एक Excel फ़ाइल मिलती है।
8. लिस्ट पर वापस जाने के लिए **Close** दबाएँ।

## आम दिक्कतें

- **मुझे "No reports found" दिख रहा है।** रिपोर्ट बैच पूरा होने के बाद ही आती हैं। अपने फ़िल्टर भी जाँचें।
- **डाउनलोड बटन ग्रे हैं।** रिपोर्ट अभी तैयार नहीं है।
- **PDF अंग्रेज़ी में आई।** हो सकता है इस रिपोर्ट के लिए वह भाषा अभी तैयार न हो, इसलिए अंग्रेज़ी कॉपी दी गई।
:::

::: section analyst-dashboard
title: लैब एनालिस्ट डैशबोर्ड
routes: /Lab/Analyst
for: 
roles: 
designations: 
screenshot: analyst-dashboard.png
screenshot-phone: analyst-dashboard.phone.jpg
minutes: 1
source-hash: 84e531434d1c8d93
confidence: ok

**यह स्क्रीन किस काम की है**
**Lab Analyst Dashboard** पर आपको सौंपा गया काम दिखता है, और यह भी कि पहले क्या करना है। जब कोई एनालिस्ट **Lab** खोलता है, तो ऐप यही स्क्रीन दिखाता है।

1. ऊपर के कार्ड देखें: **Assigned Batches**, **Pending Value Entry** (वैल्यू का इंतज़ार कर रहे सैंपल), **Auto Result Ready** (जिन सैंपल का नतीजा निकल चुका है), **Reports Generated** (इस फ़ाइनेंशियल ईयर में) और **Previous FY Reports**।
2. **Today's Work Priority** देखें:
- **Pending value entry batches** (High): वैल्यू का इंतज़ार कर रहे बैच देखने के लिए **Entry Now** दबाएँ।
- **Auto result ready for review** (Medium): नतीजे जाँचने के लिए **Preview** दबाएँ।
- **Generated reports for download** (Low): **View Reports** दबाएँ।
3. या **Quick Actions** इस्तेमाल करें: **Go to Batch List**, **Continue Test Entry** (वह बैच खोलता है जिस पर आपने आख़िरी बार काम किया था), **View Test Reports** और **View Previous FY Reports**।
4. नए नंबर देखने के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **सारे नंबर शून्य हैं।** अभी आपको कोई बैच नहीं सौंपा गया है। अपने लैब कोऑर्डिनेटर से पूछें।
- **मुझे "Your dashboard could not be loaded" दिख रहा है।** अपना कनेक्शन जाँचें और **Try again** दबाएँ।
:::

::: section analyst-batch-list
title: मुझे सौंपे गए बैच ढूँढें
routes: /Lab/TestEntry
for: 
roles: 
designations: 
screenshot: analyst-batch-list.png
hotspots: 1 @ 2.2,9.8,95.5,17.9 | 2 @ 2.2,30.1,95.5,34.9 | 3 @ 75.2,69.6,20.6,4.8 | 4 @ 4.2,76.1,91.6,23.9
screenshot-phone: analyst-batch-list.phone.jpg
hotspots-phone: 1 @ 3.1,5.8,93.8,16.9 | 2 @ 3.1,23.7,93.8,3.5 | 3 @ 6.9,31.2,86.2,3 | 4 @ 2,2,8,4
tour: 1 > [data-help=lab-bl-cards] | 2 > [data-help=lab-bl-filters] | 3 > [data-help=lab-bl-search] | 4 > [data-help=lab-bl-table]
minutes: 2
source-hash: 42bf99c9e09c8175
confidence: ok

**यह स्क्रीन किस काम की है**
**Test Value Entry** स्क्रीन पर आपको सौंपे गए बैच दिखते हैं। टेस्ट वैल्यू भरना यहीं से शुरू करें या जारी रखें।

1. ऊपर के कार्ड देखें: **Assigned Batches**, **Pending Value Entry**, **Auto Result Ready** और **Report Generated**।
2. लिस्ट छोटी करने के लिए **Filters** खोलें और **Date Range**, **Sample Type**, **Status**, **Priority**, **Financial Year** या **Assigned By** चुनें। **Apply Filters** दबाएँ। इन्हें हटाने के लिए **Clear All** दबाएँ।
3. सर्च बॉक्स में बैच या कंसाइनमेंट नंबर टाइप करें।
4. **Assigned Batch List** में **Pending Entries**, **Completed Entries**, **Priority** और **Status** देखें। **High** प्राथमिकता वाले बैच पहले करें। बैच खोलने के लिए किसी लाइन पर, या **Action** कॉलम के आइकन पर टैप करें।

## Action आइकन का मतलब

| आइकन | यह कब दिखता है |
|---|---|
| **Start entry** | स्टेटस **Pending Entry**, अभी कुछ नहीं भरा गया। |
| **Continue entry** | स्टेटस **In Progress**, कुछ वैल्यू सेव हैं। |
| **Preview results** | स्टेटस **Auto Result Ready**, सारी वैल्यू सबमिट हो गई हैं। |
| **View** | स्टेटस **Report Generated**, बैच बंद हो गया है। |

## आम दिक्कतें

- **एक बैच नहीं दिख रहा।** हो सकता है वह किसी दूसरे एनालिस्ट को सौंपा गया हो, या कोई फ़िल्टर उसे छिपा रहा हो। **Clear filters** दबाएँ।
- **मुझे "Batches could not be loaded" दिख रहा है।** अपना कनेक्शन जाँचें और **Try again** दबाएँ।
:::

::: section enter-test-values
title: सैंपल की टेस्ट वैल्यू भरें
routes: /Lab/TestEntry/{id}
for: 
roles: 
designations: 
minutes: 4
source-hash: d752cb5b39f927f4
confidence: ok

**यह स्क्रीन किस काम की है**
**Sample-wise Entry** पर आप बैच के हर सैंपल की टेस्ट वैल्यू टाइप करते हैं। सिस्टम नॉर्मल रेंज, नतीजा, स्टेटस और सुझाव खुद भर देता है।

1. ऊपर **Batch Summary** देखें।
2. **Samples in this Batch** में, जिस सैंपल पर काम करना है उसके आइकन पर टैप करें, या **Next pending sample** दबाएँ।
3. **Selected Sample** की जानकारी (किसान, गाँव, फ़सल) देखें।
4. **Test Value Entry Table** में हर वैल्यू **Test Value** में टाइप करें। **Texture** के लिए लिस्ट से चुनें। **Auto-calculated** वाली लाइनें खुद भर जाती हैं।
5. टाइप करते समय **Result**, **Status** और **Recommendation Hint** को बदलते हुए देखें। नीचे **Recommendation Preview** और **Fertilizer Schedule (Kg/acre)** देखें।
6. अधूरा काम रखने के लिए **Save Draft** दबाएँ।
7. सारी वैल्यू भरने के बाद **Submit Values** दबाएँ, फिर **Submit Test Values?** (टेस्ट वैल्यू सबमिट करें?) बॉक्स में **Submit** दबाएँ।
8. **Submitted Successfully** में **Preview Report** या **Cancel** दबाएँ, फिर अगला सैंपल चुनें।
9. सबमिट किया हुआ सैंपल ठीक करने के लिए **Edit Values** दबाएँ। यह सिर्फ़ तब तक चलता है जब तक बैच पूरा नहीं हो जाता।

## फ़ील्ड का मतलब

- **Normal Range**: इस टेस्ट के लिए सही रेंज।
- **Status**: आपकी वैल्यू के हिसाब से **Normal**, **Deficient**, **Moderate** या **Excess**।
- **Overall Soil Status** (या **Overall Water Status**): सारी वैल्यू के हिसाब से **Good**, **Needs Improvement** या **Poor**।

## आम दिक्कतें

- **"Enter every parameter before submitting."** (सबमिट करने से पहले हर पैरामीटर भरें) मैसेज में जिन पैरामीटर के नाम हैं, उन्हें भरें।
- **"Fix the values that are not numbers."** (जो वैल्यू नंबर नहीं हैं, उन्हें ठीक करें) सिर्फ़ अंक और दशमलव का बिंदु (.) इस्तेमाल करें।
- **"Leave this sample?"** (यह सैंपल छोड़ना है?) आपकी वैल्यू सेव नहीं हुई हैं। **Stay** दबाएँ, फिर **Save Draft**।
- **सब कुछ लॉक है।** बैच पूरा हो चुका है। **View Report** दबाएँ।
:::

::: section analyst-test-reports
title: टेस्ट रिपोर्ट और पिछले साल की रिपोर्ट
routes: /Lab/TestEntry/Reports, /Lab/TestEntry/Reports/Previous
for: 
roles: 
designations: 
minutes: 2
source-hash: f0ab8dbc77d485c5
confidence: ok

**यह स्क्रीन किस काम की है**
**Test Reports** स्क्रीन पर इस फ़ाइनेंशियल ईयर (अप्रैल से मार्च) के आपके हर बैच की रिपोर्ट समरी दिखती है। **Previous FY Reports** में पिछले सालों की यही जानकारी दिखती है।

1. ऊपर के कार्ड देखें: **Total Batches**, **Total Sample Reports**, **Reports Generated Today** और **Downloads Completed**।
2. पुरानी रिपोर्ट देखने के लिए **Previous FY Reports** दबाएँ। वापस आने के लिए **Current FY Reports** दबाएँ।
3. लिस्ट छोटी करने के लिए **Filters** खोलें और **Financial Year**, **Date Range**, **Sample Type**, **State**, **Region** या **Head Quarters** चुनें। **Apply Filters** दबाएँ। इन्हें हटाने के लिए **Clear All** दबाएँ।
4. सर्च बॉक्स में बैच या रिपोर्ट नंबर टाइप करें।
5. **Batch-wise Report Summary** में हर बैच के **Reports Generated** और **Pending Reports** देखें।
6. हर सैंपल की रिपोर्ट के साथ **Batch Report Details** खोलने के लिए किसी लाइन पर, या **View reports** आइकन पर टैप करें।

## आम दिक्कतें

- **मुझे "No batch reports found" दिख रहा है।** रिपोर्ट तभी आती हैं जब कोऑर्डिनेटर बैच पूरा कर देता है। फ़ाइनेंशियल ईयर और फ़िल्टर भी जाँचें।
- **मुझे "Reports are not available" दिख रहा है।** रिपोर्ट सर्विस से जवाब नहीं आया। थोड़ी देर बाद फिर कोशिश करें।
- **एक पुरानी रिपोर्ट नहीं दिख रही।** **Previous FY Reports** खोलें और उसका साल चुनें।
:::

::: section batch-report-details
title: बैच की सैंपल रिपोर्ट खोलें और डाउनलोड करें
routes: /Lab/TestEntry/Reports/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: 8c8efa3d44f215ee
confidence: ok

**यह स्क्रीन किस काम की है**
**Batch Report Details** में एक बैच के हर सैंपल की रिपोर्ट दिखती है। हर सैंपल की, उसके किसान के लिए, अपनी अलग रिपोर्ट होती है। यहाँ आप रिपोर्ट देखते हैं, डाउनलोड करते हैं और उन्हें प्रिंट हो चुका मार्क करते हैं।

1. कार्ड देखें: **Total Samples**, **Soil Samples**, **Water Samples**, **Reports Generated** और **Downloads Completed**।
2. **Batch Information** देखें: बैच, कंसाइनमेंट, State, Region, **Head Quarters**, **Date of Dispatch** और **Financial Year**।
3. कोई सैंपल ढूँढने के लिए **Sample Type**, **Status**, **Crops** और **Villages** फ़िल्टर इस्तेमाल करें।
4. सैंपल रिपोर्ट स्क्रीन पर खोलने के लिए **View** दबाएँ। इसमें नतीजे, सुझाव और खाद का शेड्यूल दिखता है।
5. एक रिपोर्ट अंग्रेज़ी में सेव करने के लिए **Download PDF** दबाएँ। दूसरी भाषा चुनने के लिए, जैसे **Tamil**, **Telugu** या **Marathi**, उसके बगल वाला छोटा तीर दबाएँ।
6. बैच की सारी रिपोर्ट एक साथ सेव करने के लिए **Download All Reports** दबाएँ। भाषा चुनने के लिए उसका तीर इस्तेमाल करें।
7. किसान के लिए रिपोर्ट प्रिंट करने के बाद **Mark printed** दबाएँ। उसका **Report Status** बदलकर **Printed** हो जाता है।

## फ़ील्ड का मतलब

- **Report Status**: **Generated** (तैयार), **Downloaded** (कम से कम एक बार सेव हुई) या **Printed**।
- **Reports No.**: किसान की रिपोर्ट पर छपा रिपोर्ट नंबर।

## आम दिक्कतें

- **मुझे "This batch report is not available" दिख रहा है।** बैच अभी पूरा नहीं हुआ है। सैंपल जाँचने के लिए **Open the batch** दबाएँ।
- **Mark printed ग्रे है।** रिपोर्ट पहले ही **Printed** मार्क हो चुकी है।
:::

::: section lab-tracking
title: एडमिन के लिए लैब ट्रैकिंग
routes: /Lab/Tracking
for: 
roles: 
designations: 
minutes: 2
source-hash: 4713e37bae159d20
confidence: ok

**यह स्क्रीन किस काम की है**
**Lab Tracking** लैब के सारे बैच का एडमिन व्यू है। कंसाइनमेंट, बैच और जाँच की प्रगति देखने के लिए इसका इस्तेमाल करें। यहाँ आप कुछ भी बदल नहीं सकते।

1. ऊपर के कार्ड देखें: **Total Batches**, **Batch Created**, **Taken for analysis** और **Analysis Completed**।
2. एक स्टेज दिखाने के लिए टैब इस्तेमाल करें: **All Batches**, **Batch Created**, **Taken for Analysis** या **Analysis Completed**। हर टैब पर उसकी गिनती दिखती है।
3. लिस्ट छोटी करने के लिए **Filters** खोलें और **Date Range**, **Batch No.**, **Consignment No.**, **Assigned To**, **State**, **Region** या **Current Stage** चुनें। **Apply Filters** दबाएँ। इन्हें हटाने के लिए **Clear All** दबाएँ।
4. **Lab Tracking List** के ऊपर वाले सर्च बॉक्स में बैच नंबर, कंसाइनमेंट नंबर या एनालिस्ट का नाम टाइप करें।
5. **Analysis Days** पढ़ें: **Pending** (पीला) का मतलब शुरू नहीं हुआ, हरा मतलब समय पर, लाल मतलब देर हो रही है।
6. बैच आख़िरी बार कब आगे बढ़ा, यह जानने के लिए **Last Updated** देखें।
7. बैच व्यू खोलने के लिए बैच नंबर या आँख वाले आइकन पर टैप करें।

## आम दिक्कतें

- **मुझे "No batches found" दिख रहा है।** **Clear filters** दबाएँ और **All Batches** टैब चुनें।
- **Lab Tracking मेरे मेन्यू में नहीं है।** आपके पद में Lab Tracking पेज होना ज़रूरी है। सिर्फ़ Admin रोल काफ़ी नहीं है। अपने एडमिनिस्ट्रेटर से पूछें।
:::

::: section batch-view
title: एडमिन के रूप में एक बैच देखें
routes: /Lab/Tracking/{id}
for: 
roles: 
designations: 
minutes: 1
source-hash: 56cb817a9b7e82f1
confidence: ok

**यह स्क्रीन किस काम की है**
बैच व्यू में एडमिन को एक ही पेज पर एक बैच की पूरी कहानी मिलती है: सैंपल कहाँ से आए, कूरियर, बैच, जाँच की प्रगति और टाइमलाइन। यह सिर्फ़ देखने के लिए है।

1. ऊपर की पट्टी में **Batch No.**, **Consignment No.**, **Test Samples**, **Samples**, **Assigned To (Analyst)**, **Batch Created On** और **Last Updated** दिखते हैं।
2. **Source Details** में दिखता है कि सैंपल किसने लिए, साथ में उनका **Mobile No.**, **Location** और **Collection Date**।
3. **Consignment Details** में **Dispatch Date**, **AWB / Tracking No** और **Expected Delivery Date** दिखते हैं। कूरियर की साइट पर पार्सल ट्रैक करने के लिए ट्रैकिंग नंबर पर टैप करें।
4. **Batch Details** में सैंपल की गिनती, **Batch Created By**, **Batch Created Date** और **Assigned Analyst** दिखते हैं।
5. **Analysis Process** में हर सैंपल टाइप के लिए दिखता है कि कितने सैंपल **Completed** हैं और कितने **Not Started**, साथ में **Completed %** बार।
6. **Timeline** में हर स्टेप तारीख और करने वाले के नाम के साथ दिखता है: **Consignment Delivered to Lab**, **Lab Entry Created**, **Batch Created**, **Taken for Analysis**, **Analysis Completed** और **Report Generated**। ग्रे स्टेप अभी नहीं हुए हैं।
7. बैच के सैंपल Excel फ़ाइल में डाउनलोड करने के लिए **Export** दबाएँ।
8. लिस्ट पर लौटने के लिए **Back to Lab Tracking** दबाएँ।

## आम दिक्कतें

- **मुझे "The batch could not be loaded" दिख रहा है।** हो सकता है बैच हटा दिया गया हो, या आपके पास Lab Tracking न हो। **Try again** दबाएँ।
:::
