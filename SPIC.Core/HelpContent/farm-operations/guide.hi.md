---
module: farm-operations
title: फ़ार्म का कामकाज
icon: bi-tree
order: 109
language: hi
category: मिट्टी की जाँच
note: नई गाइड
meta-source-hash: 4c57dbd8a4469315
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 62c60a0ea650a93e
confidence: ok

SPIC फ़ार्म का पूरा हिसाब-किताब एक ही जगह रखें। प्रोडक्ट और उनके रेट तय करें, हर 15 दिन में स्टॉक, बिक्री और खर्च दर्ज करें, और पूरे साल की कमाई, खर्च, मुनाफ़ा और स्टॉक देखें।

- products | icon: bi-tags | color: #15803d
  title: प्रोडक्ट और रेट
  text: Farm Office नया प्रोडक्ट या नया रेट माँगता है। Farm Admin उसे मंज़ूरी देता है।
- report | icon: bi-journal-text | color: #d97706
  title: हर 15 दिन में एक रिपोर्ट
  text: 1 से 15 और 16 से महीने के आख़िर तक, इन्वेंटरी, कमाई और खर्च भरें, फिर सबमिट करें।
- summaries | icon: bi-graph-up-arrow | color: #2563eb
  title: साल भर के सारांश
  text: फ़ाइनेंस, इन्वेंटरी, कमाई और खर्च के सारांश देखें, और उन्हें Excel में डाउनलोड करें।
:::

::: section flow
title: फ़ार्म का हिसाब कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 4948b5894215c5ba
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- office | type: lane | color: #15803d
  title: Farm Office
  sub: रिक्वेस्ट और 15 दिन की रिपोर्ट
- admin | type: lane | color: #ea580c
  title: Farm Admin
  sub: मंज़ूरियाँ और Product Master
- request | lane: office | col: 1 | icon: bi-tags | color: #d97706 | next: approve | screen: product-rates
  title: प्रोडक्ट या रेट माँगें
  status: Pending
  text: **Product & Rate Management** पर नया प्रोडक्ट या रेट में बदलाव माँगें। रिक्वेस्ट Farm Admin के पास रुकी रहती है।
- approve | lane: admin | col: 2 | icon: bi-patch-check | color: #16a34a | next: entry | return: request | screen: rate-approvals
  title: रेट को मंज़ूरी दें
  status: Approved
  text: Farm Admin कीमत और यूनिट जाँचकर मंज़ूरी देता है। फिर वह प्रोडक्ट और रेट 15 दिन की रिपोर्ट में इस्तेमाल होते हैं।
- rejected | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: approve | to: request | screen: rate-approvals
  title: रिक्वेस्ट रिजेक्ट हुई
  status: Rejected
  text: Farm Admin कारण लिखकर इसे रिजेक्ट करता है। Farm Office रिक्वेस्ट बदलकर **Resubmit Request** दबा सकता है।
- entry | lane: office | col: 3 | icon: bi-journal-text | color: #64748b | next: submit | screen: fifteen-day-entry
  title: 15 दिन की रिपोर्ट भरें
  status: Draft
  text: उस अवधि की इन्वेंटरी, कमाई और खर्च भरें। हर स्टेप ड्राफ़्ट में सेव होता जाता है।
- submit | lane: office | col: 4 | icon: bi-send-check | color: #15803d | next: summaries | screen: report-review
  title: जाँचें और सबमिट करें
  status: Submitted
  text: रिव्यू पेज जाँचें और **Submit 15-Day Report** दबाएँ। सबमिट की गई रिपोर्ट कभी भी खोली जा सकती है।
- summaries | lane: office | col: 5 | icon: bi-graph-up-arrow | color: #2563eb | screen: finance-summary
  title: सारांश देखें
  text: फ़ाइनेंस, इन्वेंटरी, कमाई और खर्च के सारांश में वित्तीय वर्ष की हर रिपोर्ट जुड़ जाती है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: e9a76a64316c65fc
confidence: ok

- no-request | color: #64748b
  label: No Request
  text: इस प्रोडक्ट की कोई रिक्वेस्ट नहीं है। इसका मौजूदा रेट ही चलता है।
- pending | color: #d97706
  label: Pending
  text: रिक्वेस्ट Farm Admin के पास रुकी है।
- approved | color: #16a34a
  label: Approved
  text: Farm Admin ने मंज़ूरी दे दी। नया प्रोडक्ट या रेट इस्तेमाल में है।
- rejected | color: #dc2626
  label: Rejected
  text: Farm Admin ने मना कर दिया। कारण पढ़ें, बदलाव करें और फिर से सबमिट करें।
- active | color: #15803d
  label: Active
  text: यह प्रोडक्ट Product Master में इस्तेमाल में है।
- inactive | color: #475569
  label: Inactive
  text: यह प्रोडक्ट बंद कर दिया गया है। नई रिक्वेस्ट में यह नहीं दिखता।
- draft | color: #d97706
  label: Draft
  text: 15 दिन की रिपोर्ट सेव है पर सबमिट नहीं हुई। आप अभी भी इसे बदल सकते हैं।
- submitted | color: #16a34a
  label: Submitted
  text: 15 दिन की रिपोर्ट पक्की हो गई है। सिर्फ़ एडमिन ही इसे दोबारा खोल सकता है।
- ready-to-submit | color: #2563eb
  label: Ready to submit
  text: रिव्यू स्टेप पर इसका मतलब है कि रिपोर्ट तैयार है। Finance Summary पर इसका मतलब है कि कुछ रिपोर्ट अभी सबमिट नहीं हुई हैं।
- ready-update | color: #d97706
  label: Ready for Update
  text: मौजूदा अवधि एंट्री के लिए खुली है।
- completed | color: #16a34a
  label: Completed
  text: पिछली अवधि, जो इन्वेंटरी स्टेप पर दिखती है।
- upcoming | color: #7c3aed
  label: Upcoming
  text: अगली अवधि, जो यह अवधि खत्म होने के बाद खुलती है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 6c6e07dec3a74424
confidence: ok

आपको सिर्फ़ वही स्क्रीन दिखती हैं जिनकी आपका पद (डेज़िग्नेशन) इजाज़त देता है।

- office | color: #15803d
  title: Farm Office
  - नए प्रोडक्ट और रेट में बदलाव माँगना
  - 15 दिन की रिपोर्ट भरना और सबमिट करना
  - फ़ाइनेंस, इन्वेंटरी, कमाई और खर्च के सारांश देखना
- admin | color: #ea580c
  title: Farm Admin
  - प्रोडक्ट और रेट की रिक्वेस्ट मंज़ूर या रिजेक्ट करना
  - Product Master में प्रोडक्ट और रेट ठीक करना
  - हर प्रोडक्ट के रेट का इतिहास देखना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9b23a5f40e2ecf43
confidence: ok

- no-product | popular: true
  q: मेरी 15 दिन की रिपोर्ट में एक प्रोडक्ट नहीं दिख रहा
  a: सिर्फ़ मंज़ूर हुए प्रोडक्ट ही दिखते हैं। **Product & Rate Management** पर उसे माँगें और Farm Admin की मंज़ूरी तक इंतज़ार करें।
- change-submitted | popular: true
  q: मैंने रिपोर्ट सबमिट कर दी और बाद में उसमें गलती मिली
  a: सबमिट की गई रिपोर्ट बदली नहीं जा सकती। अपने एडमिन से उसे दोबारा खोलने को कहें।
- sold-locked
  q: इन्वेंटरी स्टेप पर मैं Sold कॉलम में टाइप नहीं कर पा रहा
  a: **Sold** का आँकड़ा उस **Sold Quantity** से आता है जो आप **Income** स्टेप पर भरते हैं। उसे वहीं बदलें।
- no-approvals
  q: Pending Approvals की लिस्ट खाली है, जबकि Farm Office ने रिक्वेस्ट भेजी हैं
  a: **From Date** और **To Date** फ़िल्टर आज की तारीख से शुरू होते हैं। पहले की तारीखें चुनें और **Apply Filters** दबाएँ।
- cannot-edit
  q: "This product is still awaiting approval - you can't edit it yet." (यह प्रोडक्ट अभी मंज़ूरी के इंतज़ार में है, अभी बदला नहीं जा सकता) दिख रहा है
  a: Farm Admin की मंज़ूरी से पहले नया प्रोडक्ट बदला नहीं जा सकता। उनके फ़ैसले का इंतज़ार करें।
:::

::: section farm-dashboard
title: फ़ार्म का पूरा हाल एक नज़र में देखें
routes: /FarmDashboard, /FarmOperations
for: 
roles: 
designations: 
minutes: 2
source-hash: 46e18c8ee7fa0ff8
confidence: ok

**यह स्क्रीन किस काम की है**
फ़ार्म डैशबोर्ड (मेन्यू में **Farm Operations**) पर चुने गए वित्तीय वर्ष का पैसा और स्टॉक दिखता है, और यहीं से आप फ़ार्म की बाकी हर स्क्रीन पर जा सकते हैं।

1. ऊपर दाईं ओर **Financial Year** चुनें।
2. कार्ड पढ़ें: **Total Budget**, **Total Income Till Date**, **Total Expense Till Date**, **Net Profit Till Date** और **ROI Till Date**।
3. **Quick Action** में वह स्क्रीन खोलने के लिए **Product Management**, **Inventory Summary**, **Income Summary**, **Expense Summary** या **Finance Summary** पर टैप करें।
4. **Next 15-day Period** में **Start 15 Days Entry** दबाएँ। अगर आपने ड्राफ़्ट सेव किया है, तो बटन पर **Continue 15 Days Entry** लिखा होता है।
5. **Income, Expense & Profit Trends** में **Last 6 Periods**, **Last 3 Periods** या **All Periods** चुनें। किसी बार के आँकड़े देखने के लिए उस पर पॉइंटर ले जाएँ।
6. **Latest Stock Position** में हर प्रोडक्ट का आख़िरी क्लोज़िंग स्टॉक दिखता है। कोई प्रोडक्ट खोजें, या इन्वेंटरी सारांश खोलने के लिए **View All** दबाएँ।
7. **Income Overview** और **Top Expense Categories** में दिखता है कि पैसा कहाँ से आया और कहाँ गया। सारांश खोलने के लिए **View Details** दबाएँ।

> Tip: ROI यानी शुद्ध मुनाफ़ा भाग बजट।

## आम दिक्कतें

- **"No report history yet".** (अभी कोई रिपोर्ट नहीं है) पहले 15 दिन की एक रिपोर्ट सबमिट करें। उसके बाद चार्ट भरने लगता है।
- **"Could not load dashboard data".** (डैशबोर्ड का डेटा लोड नहीं हुआ) अपना इंटरनेट जाँचें और **Retry** दबाएँ।
- **सारे आँकड़े ज़ीरो हैं।** देख लें कि सही **Financial Year** चुना गया है।
:::

::: section product-rates
title: नया प्रोडक्ट या रेट में बदलाव माँगें
routes: /ProductRateManagement
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-prm-tabs] | 2 > [data-help=fo-prm-status] | 3 > [data-help=fo-prm-search] | 4 > [data-help=fo-prm-actions] | 5 > [data-help=fo-prm-new]
minutes: 3
source-hash: e137e022776b0912
confidence: ok

**यह स्क्रीन किस काम की है**
**Product & Rate Management** पर फ़ार्म का हर प्रोडक्ट अपने मौजूदा रेट और अपनी आख़िरी रिक्वेस्ट के साथ दिखता है। Farm Admin से नया प्रोडक्ट या नया रेट माँगने के लिए इसका इस्तेमाल करें।

1. एक टैब चुनें: **All Product**, **Livestock** या **Orchard**।
2. सिर्फ़ वही रिक्वेस्ट देखने के लिए **Pending** या **Approved** पर टैप करें। **All** में हर प्रोडक्ट दिखता है।
3. प्रोडक्ट जल्दी ढूँढने के लिए **Search products...** में टाइप करें।
4. **Action** कॉलम में आँख वाला आइकन **Request Details** दिखाता है। पेंसिल से रेट में बदलाव माँगा जाता है, या आपकी **Pending** या **Rejected** रिक्वेस्ट बदली जाती है। डस्टबिन **Pending** या **Rejected** रिक्वेस्ट को मिटाता है।
5. प्रोडक्ट जोड़ने के लिए **Request New Product** दबाएँ। **Live Stock** या **Orchard** चुनें, **Product**, **UOM** और **Price** भरें, चाहें तो **Reason** लिखें, और **Request Product** दबाएँ।

> Tip: मंज़ूरी मिलने के बाद वह प्रोडक्ट और रेट इन्वेंटरी और कमाई की एंट्री में दिखने लगते हैं।
> Careful: मिटाई गई रिक्वेस्ट वापस नहीं लाई जा सकती।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Live Stock** / **Orchard** | प्रोडक्ट का ग्रुप। |
| **Product** | प्रोडक्ट का नाम, जैसे भेड़। ज़रूरी है। |
| **UOM** | Kg, Unit, Litre या Nos। ज़रूरी है। |
| **Price** | एक यूनिट का रेट, रुपये में। ज़ीरो से ज़्यादा होना चाहिए। |
| **Reason (Optional)** | आपको इसकी ज़रूरत क्यों है। इससे Farm Admin को फ़ैसला लेने में मदद मिलती है। |

## आम दिक्कतें

- **"Enter a valid rate."** (सही रेट डालें) ज़ीरो से ज़्यादा कीमत डालें।
- **"This product is still awaiting approval - you can't edit it yet."** (यह प्रोडक्ट अभी मंज़ूरी के इंतज़ार में है) Farm Admin के नए प्रोडक्ट को मंज़ूरी देने तक इंतज़ार करें।
- **मेरी रिक्वेस्ट पर Rejected दिख रहा है।** **Rejection Reason** पढ़ने के लिए उसे आँख वाले आइकन से खोलें। पेंसिल पर टैप करें, गलती ठीक करें और **Resubmit Request** दबाएँ।
- **मुझे Request New Product नहीं दिख रहा।** आपका पद यह स्क्रीन सिर्फ़ देख सकता है।
:::

::: section rate-approvals
title: प्रोडक्ट और रेट की रिक्वेस्ट मंज़ूर या रिजेक्ट करें
routes: /ProductRateApprovals
for: 
roles: 
designations: 
minutes: 3
source-hash: 0eac09c794e59c9c
confidence: ok

**यह स्क्रीन किस काम की है**
**Pending Approvals** पर Farm Office से आई वे नए प्रोडक्ट और रेट बदलाव की रिक्वेस्ट दिखती हैं जो फ़ैसले का इंतज़ार कर रही हैं। जिस रिक्वेस्ट पर फ़ैसला हो जाता है, वह इस लिस्ट से हट जाती है।

1. **Filters** खोलें। **From Date**, **To Date**, **Request Type**, **Product Group** या **Product** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
2. एक टैब चुनें: **All Request**, **New Product** या **Rate Change**। नंबर बताते हैं कि कितनी रिक्वेस्ट रुकी हैं।
3. रिक्वेस्ट देखने के लिए आँख वाले आइकन पर टैप करें: प्रोडक्ट, यूनिट, मौजूदा रेट, **Request Price**, किसने माँगा और **Reason**।
4. माँगी गई कीमत पर ही मंज़ूरी देनी हो तो उस लाइन में **Approve** दबाएँ और पक्का करें।
5. पहले कीमत या यूनिट बदलनी हो तो पेंसिल पर टैप करें। **Approval Details** में **UOM** या **Approved Price** बदलें, **Admin Remarks** लिखें, और **Approve** दबाएँ।
6. मना करने के लिए पेंसिल पर टैप करें, **Admin Remarks** में कारण लिखें और **Reject** दबाएँ, फिर पक्का करें।

> Careful: मंज़ूरी देते ही प्रोडक्ट का रेट तुरंत बदल जाता है। वह अगली 15 दिन की रिपोर्ट में इस्तेमाल होता है।

## आम दिक्कतें

- **लिस्ट खाली है।** तारीखें आज से शुरू होती हैं। पहले की **From Date** चुनें और **Apply Filters** दबाएँ।
- **"Enter a rejection reason in Admin Remarks."** (Admin Remarks में रिजेक्ट करने का कारण लिखें) रिजेक्ट करने के लिए कारण लिखना ज़रूरी है।
- **"Enter a valid approved price."** (सही मंज़ूर कीमत डालें) ज़ीरो से ज़्यादा कीमत डालें।
- **Approve बटन नहीं दिख रहा।** आपका पद रिक्वेस्ट सिर्फ़ देख सकता है।
:::

::: section product-master
title: Product Master संभालें
routes: /FarmProductMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: 858235a42d5c0ca1
confidence: ok

**यह स्क्रीन किस काम की है**
**Product Master** फ़ार्म के सभी प्रोडक्ट, उनकी यूनिट और मौजूदा रेट की लिस्ट है। मंज़ूर हुई रिक्वेस्ट यहाँ जुड़ जाती हैं। Farm Admin यहाँ सीधे किसी प्रोडक्ट को ठीक भी कर सकता है।

1. एक टैब चुनें: **All Product**, **Livestock** या **Orchard**, या कोई प्रोडक्ट खोजें।
2. **Current Rate** और **Status** (**Active** या **Inactive**) देखें।
3. हर मंज़ूर रेट उसकी तारीख के साथ देखने के लिए **View History** दबाएँ। सबसे नए रेट पर **Current** लिखा होता है।
4. किसी प्रोडक्ट को ठीक करने के लिए पेंसिल पर टैप करें। **Product Group**, **Product Name**, **UOM** या **Current Rate** बदलें।
5. किसी प्रोडक्ट का इस्तेमाल बंद करना हो तो **Active** का टिक हटा दें। वह अपने रेट के इतिहास के साथ लिस्ट में बना रहता है।
6. **Save Changes** दबाएँ।

> Careful: यहाँ किया गया बदलाव मंज़ूरी की प्रक्रिया से नहीं गुज़रता। वह तुरंत लागू हो जाता है।

## आम दिक्कतें

- **"Enter a product name and UOM."** (प्रोडक्ट का नाम और UOM डालें) दोनों भरना ज़रूरी है।
- **"No approved rate history yet."** (अभी मंज़ूर रेट का कोई इतिहास नहीं है) इस प्रोडक्ट की अभी तक कोई रेट रिक्वेस्ट मंज़ूर नहीं हुई है।
- **पेंसिल नहीं दिख रही।** आपका पद Product Master सिर्फ़ देख सकता है।
:::

::: section fifteen-day-entry
title: 15 दिन की रिपोर्ट भरें और सबमिट करें
routes: /FifteenDaysEntry, /15daysEntry
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-steps] | 2 > [data-help=fo-period] | 3 > [data-help=fo-inventory] | 4 > [data-help=fo-continue] | 5 > [data-help=fo-continue] | 6 > [data-help=fo-save]
minutes: 6
source-hash: 8adffbba9a65bc8e
confidence: ok

**यह स्क्रीन किस काम की है**
हर 15 दिन में (1 से 15, और 16 से महीने के आख़िर तक) फ़ार्म चार स्टेप वाली एक रिपोर्ट भेजता है: **Inventory**, **Income**, **Expense** और **Review**। यह स्क्रीन वह अगली अवधि खोलती है जो अभी सबमिट नहीं हुई है।

1. **Inventory**, **Income**, **Expense** और **Review** के बीच आने-जाने के लिए ऊपर के स्टेप इस्तेमाल करें। हर बार आगे-पीछे जाने पर आपका काम ड्राफ़्ट में सेव हो जाता है।
2. अवधि के कार्ड देखें: **Current Reporting**, **Previous Period** और **Next Reporting Period**।
3. **Inventory Entry** पर, **Livestock Sales Ledger** में हर प्रोडक्ट के लिए **Purchase / Old Quantity**, **Hatched / New Breed**, **Mortality** और **Others** भरें। **Opening Stock** और **Closing Stock** अपने-आप भर जाते हैं। ज़रूरत हो तो **Remarks** लिखें।
4. **Continue to Income** दबाएँ। **Income Entry** पर **Livestock Sales Ledger** और **Orchard Sales** में हर प्रोडक्ट की **Sold Quantity** भरें। रेट मंज़ूर रेट ही होता है, इसलिए **Total amount** अपने-आप निकल आता है। **Continue to Expense** दबाएँ।
5. **Expense Entry** पर **Expenditure Breakup** और **Miscellaneous Expenditure** में रकम भरें। अगर कोई लाइन नहीं है तो **Add New Category** या **Add New Sub Category** इस्तेमाल करें। **Continue to Review** दबाएँ, सब कुछ जाँचें, **Submit 15-Day Report** दबाएँ और फिर **Confirm Submit** दबाएँ।
6. काम रोककर बाद में लौटना हो तो कभी भी **Save as Draft** दबाएँ।

> Tip: ब्रेकअप में **Miscellaneous** सब कैटेगरी का कुल जोड़ होता है। इसमें टाइप नहीं किया जा सकता।
> Careful: एक बार सबमिट करने के बाद रिपोर्ट तब तक नहीं बदली जा सकती, जब तक कोई एडमिन उसे दोबारा न खोले।

## सबमिट करने के बाद

- **15 - Day Report Submitted Successfully!** स्क्रीन पर कुल आँकड़े दिखते हैं।
- रिपोर्ट खोलने के लिए **View Submitted Report** दबाएँ, साल भर का हिसाब देखने के लिए **Go to finance summary** दबाएँ, या अगली अवधि के लिए **Enter ... Report** दबाएँ।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Purchase / Old Quantity** | इस अवधि में खरीदे गए जानवर या सामान। |
| **Hatched / New Breed** | इस अवधि में पैदा हुए या अंडे से निकले नए जानवर। |
| **Mortality** | इस अवधि में मरे हुए जानवर। |
| **Others** | कोई और बदलाव। स्टॉक घटाने के लिए यह ज़ीरो से कम (माइनस) भी हो सकता है। |
| **Sold Quantity** | आपने कितना बेचा। इससे इन्वेंटरी स्टेप पर **Sold** भी भर जाता है। |
| **Amount (₹)** | इस अवधि में उस खर्च पर लगा पैसा। |

## आम दिक्कतें

- **"No products in this ledger".** (इस लेजर में कोई प्रोडक्ट नहीं है) अभी कोई प्रोडक्ट मंज़ूर नहीं हुआ है। **Product & Rate Management** पर उसे माँगें।
- **मैं Sold में टाइप नहीं कर पा रहा।** **Income** स्टेप पर **Sold Quantity** भरें।
- **रेट गलत है।** **Product & Rate Management** पर रेट में बदलाव माँगें। यहाँ इसे नहीं बदला जा सकता।
- **"Please enter a category name."** (कैटेगरी का नाम डालें) **Add Category** दबाने से पहले नाम टाइप करें।
:::

::: section report-review
title: सबमिट की गई 15 दिन की रिपोर्ट खोलें
routes: /FifteenDayReportReview, /15DayReportReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 1abbfd7769868ce9
confidence: ok

**यह स्क्रीन किस काम की है**
**15 - Day Report Review** स्क्रीन पर सबमिट की गई एक रिपोर्ट पूरी दिखती है और उसकी तुलना पिछली अवधि से होती है।

1. सबमिट करने के बाद **View Submitted Report** से, या **Finance Summary** पर आँख वाले आइकन से इसे खोलें।
2. ऊपर अवधि, स्टेटस और किसने सबमिट किया, यह देखें।
3. कार्ड पढ़ें: **Total Income**, **Total Expense**, **Net Profit** और **Total Products**। तीर पिछले 15 दिनों से तुलना दिखाते हैं।
4. हर हिस्सा खोलने या बंद करने के लिए **Inventory Summary**, **Income Summary** या **Expense Summary** पर टैप करें।
5. नीचे **Financial Summary** और **Comparison with Previous** देखें।
6. वापस जाने के लिए **Back to Finance Summary List** दबाएँ।

## आम दिक्कतें

- **"No previous period to compare".** (तुलना के लिए कोई पिछली अवधि नहीं है) यह वित्तीय वर्ष की पहली रिपोर्ट है।
- **"No inventory records found for this period."** (इस अवधि का कोई इन्वेंटरी रिकॉर्ड नहीं मिला) उस रिपोर्ट में कोई स्टॉक नहीं भरा गया था।
:::

::: section finance-summary
title: साल भर का फ़ाइनेंस सारांश देखें
routes: /FinanceSummary, /FinanceSummaryOverall
for: 
roles: 
designations: 
minutes: 2
source-hash: d2b850b02c31e1d6
confidence: ok

**यह स्क्रीन किस काम की है**
**Finance Summary (Overall)** में एक वित्तीय वर्ष की सभी 15 दिन की रिपोर्ट जोड़ी जाती हैं और हर रिपोर्ट की लिस्ट दिखती है।

1. **Financial Year** और **View** चुनें: **15-Day View**, **Monthly View** या **Quarterly View**।
2. कार्ड पढ़ें: **Annual Budget**, **Total Income**, **Total Expense**, **Net Profit** और **ROI**।
3. **Analytics Validation Trend** में **Last 6 Months**, **Last 3 Months** या **Last 1 Year** चुनें।
4. रिपोर्ट के इतिहास में लिस्ट छोटी करने के लिए **From Date** और **To Date** चुनें।
5. **Status** कॉलम में **Submitted** या **Draft** दिखता है। रिपोर्ट खोलने के लिए आँख वाले आइकन पर टैप करें।
6. लिस्ट सेव करने के लिए **Download Excel** दबाएँ।

## आम दिक्कतें

- **"No report records found for the selected filter."** (चुने गए फ़िल्टर के लिए कोई रिपोर्ट नहीं मिली) तारीखें बढ़ाएँ या कोई दूसरा वित्तीय वर्ष चुनें।
- **ऊपर के बैज पर Ready to submit लिखा है।** साल की कम से कम एक रिपोर्ट अभी ड्राफ़्ट में है।
:::

::: section inventory-summary
title: हर प्रोडक्ट के स्टॉक का आना-जाना देखें
routes: /InventorySummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 7b39d227504cc4c7
confidence: ok

**यह स्क्रीन किस काम की है**
**Inventory Summary** में दिखता है कि हर 15 दिन की अवधि में हर प्रोडक्ट का स्टॉक कैसे बदला।

1. **Filters** खोलें। **From Date**, **To Date**, **Category** और **Product** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
2. कोई प्रोडक्ट देखने के लिए उसके टैब पर टैप करें।
3. कार्ड पढ़ें: **Total Purchased**, **Total Hatched**, **Total Sold**, **Mortality** और **Closing Stock**।
4. **Stock Movement History** टेबल में क्रम बदलने के लिए कॉलम के नाम पर टैप करें। जो अवधि अभी ड्राफ़्ट में है, उस पर **Draft** टैग होता है।
5. टेबल सेव करने के लिए **Download Excel** या **Download PDF** दबाएँ।

## आम दिक्कतें

- **"From Date cannot be later than To Date."** (From Date, To Date के बाद की नहीं हो सकती) तारीखें फिर से चुनें।
- **"No products found".** (कोई प्रोडक्ट नहीं मिला) पहले Product Master में प्रोडक्ट जोड़े जाने चाहिए।
:::

::: section income-summary
title: कमाई का सारांश देखें
routes: /IncomeSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: d11284cff39730df
confidence: ok

**यह स्क्रीन किस काम की है**
**Income Summary** में हर रिपोर्टिंग अवधि की फ़ार्म की बिक्री से हुई कमाई दिखती है, पशुधन (livestock) और बाग (orchard) अलग-अलग।

1. **Filters** खोलें। **Period Type** (**15 Days** या **1 Month**), **From Date**, **To Date** और **Income Category** चुनें, फिर **Apply Filters** दबाएँ।
2. कार्ड पढ़ें: **Total Income Till Date**, **Sales Records**, **Products Covered** और **Reporting Period**। आख़िरी कार्ड बताता है कि आख़िरी अवधि अपडेट करने के लिए कितने दिन बचे हैं।
3. **Livestock Sales Ledger** टेबल में हर अवधि का **Livestock Income**, **Orchard Income**, **Total Income** और **Status** देखें।
4. उस अवधि का हर प्रोडक्ट, मात्रा, रेट और कमाई देखने के लिए **View** में आँख वाले आइकन पर टैप करें।
5. हर प्रोडक्ट का हिस्सा देखने के लिए **Live Income Overview** और **Orchard Income Overview** देखें।
6. टेबल सेव करने के लिए **Download Excel** दबाएँ।

## आम दिक्कतें

- **"Editing window closed".** (बदलाव का समय खत्म) आख़िरी अवधि अपडेट करने का समय निकल चुका है।
- **"Could not load income data".** (कमाई का डेटा लोड नहीं हुआ) **Retry** दबाएँ।
:::

::: section expense-summary
title: खर्च का सारांश देखें
routes: /ExpenseSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 9200f1d0c1608503
confidence: ok

**यह स्क्रीन किस काम की है**
**Expense Summary** में हर रिपोर्टिंग अवधि का फ़ार्म का खर्च और सबसे बड़े खर्च वाली कैटेगरी दिखती हैं।

1. **Filters** खोलें। **Period Type**, **From Date**, **To Date** और **Expense Category** चुनें, फिर **Apply Filters** दबाएँ।
2. कार्ड पढ़ें: **Total Expense**, **Highest Expense Category**, **Expense Categories** और **Summary Coverage**।
3. **15 - Day Expense Summary** में हर अवधि का **Total Expense (₹)** और **Status** देखें।
4. उस अवधि का **Expenditure Breakup (Farm Operations)** और **Miscellaneous Expenditure (Sub-Categories)** देखने के लिए **View** में आँख वाले आइकन पर टैप करें।
5. तुलना के लिए **Monthly Expense Trend** और **Expense Category Overview** देखें।
6. टेबल सेव करने के लिए **Download Excel** दबाएँ।

## आम दिक्कतें

- **"No expense data yet".** (अभी खर्च का कोई डेटा नहीं है) पहले खर्च के साथ 15 दिन की एक रिपोर्ट सबमिट करें।
- **"Could not load expense data".** (खर्च का डेटा लोड नहीं हुआ) **Retry** दबाएँ।
:::
