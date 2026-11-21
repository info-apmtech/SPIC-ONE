---
module: stock-liquidation
title: स्टॉक और लिक्विडेशन
icon: bi-boxes
order: 106
language: hi
category: बिक्री
note: नई गाइड
meta-source-hash: b8a21e045ebf822e
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: fb227391bc0718ee
confidence: ok

SPIC का खाद कंपनी से निकलने के बाद उस पर नज़र रखें। देखें कि डीलरों ने अभी किन बिक्रियों की पावती (acknowledgement) नहीं दी है, होलसेलर और रिटेलर के पास कितना स्टॉक पड़ा है, वह कितना पुराना है, और वह कितनी जल्दी आगे बिकता है। ये आंकड़े IFMS से आते हैं, जो सरकार का खाद सिस्टम है।

- ack | icon: bi-hourglass-split | color: #db2777
  title: बाकी पावती
  text: कंपनी और डीलर की वे बिक्रियाँ देखें जिनकी खरीदार ने अभी IFMS में पुष्टि नहीं की है।
- stock | icon: bi-boxes | color: #2563eb
  title: स्टॉक और उसकी उम्र (ageing)
  text: देखें कि हर डीलर के पास कितना स्टॉक है और वह कितने दिनों से वहाँ पड़ा है।
- liquidation | icon: bi-graph-up-arrow | color: #16a34a
  title: लिक्विडेशन
  text: देखें कि डीलर अपना स्टॉक कितनी जल्दी बेचते हैं, और कौन से डीलर धीमे हैं।
:::

::: section flow
title: स्टॉक कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 12cbf705ed2b7495
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- company | type: lane | color: #db2777
  title: SPIC
  sub: कंपनी की बिक्री
- dealers | type: lane | color: #7c3aed
  title: डीलर
  sub: होलसेलर और रिटेलर, IFMS में
- admin | type: lane | color: #374151 | roles: Admin
  title: Admin
  sub: IFMS की फ़ाइलें लोड करता है
- staff | type: lane | color: #0d9488 | roles: Director, AVP, SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: सेल्स स्टाफ़
  sub: अपने इलाके की रिपोर्ट देखते हैं
- sale | lane: company | col: 1 | icon: bi-receipt | color: #db2777 | next: ack | screen: pending-ack
  title: कंपनी की बिक्री
  status: New
  text: SPIC होलसेलर को खाद बेचता है। उसका इनवॉइस IFMS में डाला जाता है। होलसेलर भी इसी तरह रिटेलर को बेचते हैं।
- ack | lane: dealers | col: 2 | icon: bi-clipboard-check | color: #16a34a | next: upload | screen: ack-cycle
  title: डीलर पावती देता है
  status: Ack
  text: खरीदार IFMS में पुष्टि करता है कि स्टॉक पहुँच गया। इनवॉइस से माल मिलने तक के दिन पावती का चक्र (acknowledgement cycle) कहलाते हैं।
- upload | lane: admin | col: 3 | icon: bi-file-earmark-arrow-up | color: #374151 | next: stock | screen: file-upload
  title: IFMS की रिपोर्ट लोड करें
  text: IFMS की रिपोर्ट हर दिन SPIC ONE में आती हैं, रात के अपने-आप होने वाले इम्पोर्ट से या Admin के Excel अपलोड से।
- stock | lane: dealers | col: 4 | icon: bi-boxes | color: #2563eb | next: sell | screen: stock-report
  title: डीलर के पास स्टॉक
  status: Fresh
  text: पावती दिया हुआ स्टॉक होलसेलर या रिटेलर के पास रहता है। उसकी उम्र पावती वाले दिन से गिनी जाती है।
- sell | lane: dealers | col: 5 | icon: bi-cart-check | color: #16a34a | next: review | screen: liquidation-cycle
  title: आगे बिक गया (लिक्विडेट हुआ)
  status: Active
  text: डीलर स्टॉक आगे रिटेलर और किसानों को बेचता है। बिक्री से बचा हुआ स्टॉक कम होता है।
- review | lane: staff | col: 6 | icon: bi-bar-chart | color: #0d9488 | screen: stock-details
  title: नज़र रखें और फ़ॉलो-अप करें
  text: स्टाफ़ अपने इलाके का स्टॉक, बिक्री और उम्र देखते हैं और धीमे डीलरों से फ़ॉलो-अप करते हैं।
- pending | lane: staff | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: sale | to: ack | screen: pending-ack
  title: समय पर पावती नहीं दी
  status: Overdue
  text: अगर खरीदार पावती नहीं देता, तो बिक्री बाकी (pending) रहती है और पुरानी होती जाती है। स्टाफ़ डीलर से IFMS में पावती देने को कहते हैं।
- aged | lane: staff | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: stock | to: sell | screen: ageing-report
  title: पुराना स्टॉक नहीं बिक रहा
  status: Long Aged
  text: जो स्टॉक नहीं बिकता वह पुराना होता जाता है और उस पर Long Aged या Critical दिखता है। स्टाफ़ डीलर को उसे पहले बेचने में मदद करते हैं।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 5b6bc08644fdb0cc
confidence: ok

- new | color: #64748b
  label: New
  text: IFMS का स्टेटस। बिक्री डाली गई है पर खरीदार ने अभी पावती नहीं दी है।
- ack | color: #16a34a
  label: Ack
  text: IFMS का स्टेटस। खरीदार ने पावती दे दी है कि स्टॉक पहुँच गया।
- completed | color: #16a34a
  label: Completed
  text: बिक्री की पावती मिल गई है। अब यह बाकी नहीं है।
- latest | color: #2563eb
  label: Latest
  text: 0 से 10 दिन से पावती का इंतज़ार है।
- overdue | color: #dc2626
  label: Overdue
  text: 21 दिन या उससे ज़्यादा से पावती का इंतज़ार है।
- consent | color: #9333ea
  label: Consent of Buyer
  text: IFMS में दिखता है कि बिक्री खरीदार की सहमति का इंतज़ार कर रही है।
- fast | color: #16a34a
  label: Fast
  text: इनवॉइस के 0 से 2 दिन के अंदर पावती दी।
- normal | color: #d97706
  label: Normal
  text: इनवॉइस के 3 से 5 दिन के अंदर पावती दी।
- delayed | color: #ea580c
  label: Delayed
  text: इनवॉइस के 6 से 10 दिन बाद पावती दी।
- pending-ack | color: #64748b
  label: Pending ACK
  text: स्टॉक रिपोर्ट। इस स्टॉक की IFMS में अभी पावती नहीं है, इसलिए इसकी कोई उम्र नहीं है।
- fresh | color: #059669
  label: Fresh
  text: 0 से 30 दिन पुराना स्टॉक।
- medium | color: #34d399
  label: Medium
  text: 31 से 90 दिन पुराना स्टॉक।
- slow | color: #f59e0b
  label: Slow Moving
  text: 91 से 180 दिन पुराना स्टॉक।
- long | color: #ef4444
  label: Long Aged
  text: 181 से 364 दिन पुराना स्टॉक।
- active | color: #16a34a
  label: Active
  text: लिक्विडेशन। डीलर ने यह स्टॉक पिछले 45 दिनों में बेचा है।
- monitoring | color: #d97706
  label: Monitoring
  text: लिक्विडेशन। 46 से 75 दिन से कोई बिक्री नहीं। नज़र रखें।
- critical | color: #b91c1c
  label: Critical
  text: हर स्क्रीन पर सबसे खराब ग्रुप। बाकी पावती: 11 से 20 दिन। पावती का चक्र: 10 दिन से ज़्यादा। स्टॉक की उम्र: 365 दिन या ज़्यादा। लिक्विडेशन: 75 दिन से ज़्यादा से कोई बिक्री नहीं।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e4dca540aba6f06
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- staff | color: #0d9488 | roles: SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: सेल्स स्टाफ़
  - अपने राज्य, रीजन या HQ की बाकी पावती, स्टॉक, उम्र और लिक्विडेशन देखें
  - जो डीलर पावती देने या बेचने में धीमे हैं, उनसे फ़ॉलो-अप करें
  - लिस्ट Excel में डाउनलोड करें
- leaders | color: #db2777 | roles: Director, AVP
  title: Director और AVP
  - सभी राज्यों के आंकड़े देखें
  - राज्यों, डीलरों और प्रोडक्ट की तुलना करें
- admin | color: #374151 | roles: Admin
  title: Admin
  - IFMS की Excel रिपोर्ट हर दिन अपलोड करें
  - सभी राज्यों के आंकड़े देखें
  - State-wise Stock Reconciliation ईमेल से भेजें
- dealers | color: #7c3aed | roles: Dealer
  title: होलसेलर और रिटेलर
  - मिले हुए स्टॉक की IFMS में पावती दें
  - स्टॉक जल्दी आगे बेचें
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 12f38854e10b0f55
confidence: ok

- overdue | popular: true
  q: बिक्री पर Overdue दिख रहा है। मैं क्या करूँ?
  a: खरीदार ने 21 दिन या उससे ज़्यादा से IFMS में स्टॉक की पावती नहीं दी है। डीलर से IFMS में पावती देने को कहें। अगले IFMS अपडेट के बाद उस पर **Completed** दिखेगा।
- old-figures | popular: true
  q: आंकड़े पुराने लग रहे हैं या रिपोर्ट खाली है
  a: रिपोर्ट में वही IFMS डेटा दिखता है जो SPIC ONE में आख़िरी बार लोड हुआ था। अगर आज की फ़ाइलें अभी लोड नहीं हुई हैं, तो Admin से उन्हें **File Upload** पर अपलोड करने को कहें, फिर **Refresh Data** दबाएँ।
- my-area
  q: मैं दूसरा राज्य या रीजन नहीं चुन पा रहा
  a: ज़्यादातर स्टाफ़ को सिर्फ़ अपना राज्य, रीजन या HQ दिखता है। Director, AVP और Admin को सभी राज्य दिखते हैं।
- no-rows
  q: मुझे "No records found." या "No Data Available" दिख रहा है
  a: हो सकता है आपके फ़िल्टर बहुत कड़े हों। **Clear All** दबाएँ, फिर **Search** दबाएँ।
- pdf
  q: PDF और Send Mail बटन ग्रे हैं
  a: ये अभी तैयार नहीं हैं। लिस्ट डाउनलोड करने के लिए **Excel** इस्तेमाल करें।
:::

::: section pending-ack
title: बाकी पावती देखें
routes: /CompanySales
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-pa-filters] | 2 > [data-help=sl-pa-cards] | 3 > [data-help=sl-pa-tabs] | 4 > [data-help=sl-pa-table] | 5 > [data-help=sl-pa-export]
minutes: 3
source-hash: bc4973981c2b913d
confidence: ok

**यह स्क्रीन किस काम की है**
**Pending Acknowledgement** स्क्रीन पर कंपनी, होलसेलर और रिटेलर की बिक्री दिखती है, और यह भी कि खरीदार ने IFMS में उसकी पावती दी है या नहीं। मेन्यू में **Pending Acknowledgement** दबाकर इसे खोलें।

1. **Filters** खोलें और **Date Range**, **State**, **District**, **Dealer Type**, **Dealer / Agency Name**, **Product** या **Status (By Age)** चुनें। **Search** दबाएँ। **Clear All** से फ़िल्टर हट जाते हैं।
2. तीनों कार्ड पढ़ें: कुल, **Company Sales** और **Wholesaler Sales**। हर कार्ड में **Completed**, **Latest**, **Critical**, **Overdue** और **Consent of Buyer** दिखते हैं। नीचे का चार्ट राज्यों की तुलना करता है।
3. एक टैब चुनें: **All**, **Company Sales**, **Wholesaler Sales** या **Retailer Sales**। कोई बिक्री ढूँढने के लिए सर्च बॉक्स में इनवॉइस या डीलर का नाम टाइप करें।
4. टेबल में **Age Status** और **Pending Ack Age (Days)** देखें। पूरे **Record Details** देखने के लिए **View** दबाएँ।
5. लिस्ट डाउनलोड करने के लिए **Excel** दबाएँ।

> Tip: सबसे पुरानी बाकी बिक्रियाँ पहले देखने के लिए **Pending Ack Age (Days)** से क्रम लगाएँ, और उन डीलरों को फ़ोन करें।

| फ़ील्ड | क्या दिखाता है |
|---|---|
| **Age Status** | **Latest** 0 से 10 दिन, **Critical** 11 से 20 दिन, **Overdue** 21 दिन या ज़्यादा, या **Consent of Buyer**। |
| **Workflow Status** | IFMS का स्टेटस, जैसे **New** या **Ack**। |
| **Quantity (MT)** | मीट्रिक टन में मात्रा। |

## आम दिक्कतें

- **"No records found."** (कोई रिकॉर्ड नहीं मिला) Filters में **Clear All** दबाएँ, या सर्च बॉक्स खाली करें।
- **टैब बदलने पर कार्ड नहीं बदलते।** यह सामान्य है। टैब से सिर्फ़ टेबल और Excel फ़ाइल बदलती है।
- **डीलर के पावती देने के बाद भी बिक्री बाकी दिख रही है।** नया IFMS डेटा अभी लोड नहीं हुआ है। अगले अपडेट के बाद फिर से देखें।
:::

::: section ack-cycle
title: देखें कि डीलर कितनी जल्दी पावती देते हैं
routes: /Acknowledgement
for: 
roles: 
designations: 
minutes: 2
source-hash: 751788d0db9a139a
confidence: ok

**यह स्क्रीन किस काम की है**
**Acknowledgement Cycle** स्क्रीन दिखाती है कि इनवॉइस के बाद डीलरों ने स्टॉक की पावती देने में कितने दिन लगाए। इसमें सिर्फ़ वही बिक्रियाँ दिखती हैं जिनकी पावती मिल चुकी है।

1. **Filters** खोलें और **State**, **Region**, **Head Quarters**, **District**, **Product**, **Dealer Name**, **Status**, **Cycle Bucket** या **Invoice Date From** और **Invoice Date To** चुनें। **Search** दबाएँ।
2. कार्ड पढ़ें: **Total Transactions**, **Fast (0-2 Days)**, **Normal (3-5 Days)**, **Delayed (6-10 Days)** और **Critical (> 10 Days)**।
3. ऊपर की दो टॉप 5 लिस्ट किस हिसाब से ग्रुप हों, यह चुनें: **State**, **Region**, **Head Quarters**, **District**, **Product** या **Dealer Name**।
4. **Top 5 Fast Performing** लिस्ट की तुलना **Top 5 Delayed** लिस्ट से करें।
5. **Acknowledgement Cycle List** में हर बिक्री का **Ack Cycle (Days)** और **Status** देखें। **Acknowledgement Details** के लिए **View** दबाएँ।
6. लिस्ट डाउनलोड करने के लिए **Excel** दबाएँ।

## आम दिक्कतें

- **A top 5 list says "No data for the current filters."** (टॉप 5 लिस्ट में "इन फ़िल्टर के लिए कोई डेटा नहीं" दिख रहा है) किसी ग्रुप को रैंक मिलने के लिए उसमें कम से कम 50 पावती वाली बिक्रियाँ होनी चाहिए। फ़िल्टर थोड़े बड़े करें।
- **"No acknowledged transactions found."** (पावती वाली कोई बिक्री नहीं मिली) **Clear All** दबाएँ, फिर **Search** दबाएँ।
:::

::: section stock-report
title: डीलरों के पास स्टॉक देखें
routes: /StockReport
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-sr-filters] | 2 > [data-help=sl-sr-cards] | 3 > [data-help=sl-sr-charts] | 4 > [data-help=sl-sr-search] | 5 > [data-help=sl-sr-table] | 6 > [data-help=sl-sr-export]
minutes: 3
source-hash: 5c6d4945a1c3da6d
confidence: ok

**यह स्क्रीन किस काम की है**
**Stock Report** स्क्रीन दिखाती है कि हर होलसेलर और रिटेलर के पास अभी कितना स्टॉक है, और वह कितना पुराना है।

1. **Filters** खोलें और **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying With**, **Product** या **Ageing Range (Days)** चुनें। **Search** दबाएँ।
2. कार्ड पढ़ें: **Total Stock (Current Stock)**, **Dealers Count**, **Average Ageing** और **High Ageing Stock**।
3. चार्ट देखें: **State-wise Stock Availability** इस साल की तुलना पिछले साल से करता है, और **Product-wise Stock Distribution (MT)** हर प्रोडक्ट का हिस्सा दिखाता है।
4. **Stock Report List** के ऊपर वाले सर्च बॉक्स में डीलर, राज्य या प्रोडक्ट टाइप करें।
5. टेबल में **Quantity (MT)**, स्टॉक किसके पास है (**Lying With**), **Ageing in Days** और **Status** देखें। पूरे **Stock Details** के लिए **View** दबाएँ।
6. लिस्ट डाउनलोड करने के लिए **Excel** दबाएँ।

> Tip: पुराना स्टॉक जल्दी ढूँढने के लिए **Ageing Range (Days)** में **Long Aged (181-364)** और **Critical (365+)** चुनें।

## आम दिक्कतें

- **"No Data Available".** (कोई डेटा नहीं है) Filters में **Clear All** दबाएँ, या सर्च बॉक्स खाली करें।
- **Ageing में "--" दिख रहा है और स्टेटस Pending ACK है।** उम्र सिर्फ़ IFMS में पावती से गिनी जाती है। जिस स्टॉक की पावती नहीं है, उसकी अभी कोई उम्र नहीं होती।
:::

::: section ageing-report
title: पुराना और धीरे बिकने वाला स्टॉक ढूँढें
routes: /AgeingReport
for: 
roles: 
designations: 
minutes: 2
source-hash: 366d05a43d5fbe3a
confidence: ok

**यह स्क्रीन किस काम की है**
**Ageing Report** स्क्रीन दिखाती है कि स्टॉक डीलरों के पास कितने दिनों से पड़ा है, ताकि आप धीरे बिकने वाला स्टॉक ढूँढ सकें।

1. **Filters** खोलें और **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying with**, **Product** या **Ageing Range (Days)** चुनें। **Search** दबाएँ।
2. कार्ड पढ़ें: **Total Stock (MT)**, **Average Ageing (Days)**, **30-60 Days Stock (MT)** और **60+ Days Stock (MT)**।
3. **State-wise Stock by Ageing (MT)** में किसी बार पर उंगली रखें, उस राज्य का हर उम्र-ग्रुप दिखेगा।
4. **Ageing by Days (MT)** में देखें कि हर उम्र-ग्रुप में कितना स्टॉक है।
5. **Ageing Report List** में हर डीलर का **Ageing in Days** और **Status** देखें। **Stock Ageing Details** के लिए **View** दबाएँ।
6. लिस्ट डाउनलोड करने के लिए **Excel** दबाएँ।

## आम दिक्कतें

- **"No records found."** (कोई रिकॉर्ड नहीं मिला) **Clear All** दबाएँ, फिर **Search** दबाएँ।
- **कुछ पुराना स्टॉक नहीं दिख रहा।** उम्र सिर्फ़ IFMS में पावती से गिनी जाती है।
:::

::: section liquidation-cycle
title: देखें कि स्टॉक कितनी जल्दी बिकता है
routes: /LiquidationCycle
for: 
roles: 
designations: 
minutes: 3
source-hash: f76600523961f075
confidence: ok

**यह स्क्रीन किस काम की है**
**Liquidation Cycle** स्क्रीन दिखाती है कि हर डीलर के पास कितना स्टॉक था, उसने कितना बेचा, और उसकी आख़िरी बिक्री को कितने दिन हुए।

1. **Filters** खोलें और **Date From**, **Date To**, **State**, **Region**, **HQ**, **District**, **Sub District**, **Dealer Name**, **Product** या **Financial Year** चुनें। **Apply Filters** दबाएँ।
2. कार्ड पढ़ें: **Total Stock (MT)**, **Liquidated (MT)** और **Balance Stock (MT)**।
3. **Dealer Liquidation Performance** के नीचे **Top 5 Performance**, **State-wise**, **All**, **Company Sales**, **Wholesalers** या **Retailers** चुनें।
4. तेज़ लिस्ट की तुलना धीमी लिस्ट से करें, जैसे **Top 5 Fast Dealers** और **Top 5 Slow Dealers**।
5. **Liquidation Cycle List** में **Stock (MT)**, **Ageing (Days)**, **Sales (MT)** और **Status** देखें। **Liquidation Details** के लिए **View** दबाएँ।
6. लिस्ट डाउनलोड करने के लिए **Excel** दबाएँ।

> Tip: यहाँ **Ageing (Days)** का मतलब है डीलर ने उस प्रोडक्ट की आख़िरी बिक्री कितने दिन पहले की।

## आम दिक्कतें

- **"No records found."** (कोई रिकॉर्ड नहीं मिला) **Clear All** दबाएँ, फिर **Apply Filters** दबाएँ।
- **मैं दूसरा राज्य नहीं चुन पा रहा।** आपका इलाका आपके लॉगिन से तय होता है। Director, AVP और Admin को सभी राज्य दिखते हैं।
:::

::: section stock-details
title: राज्य के हिसाब से रोज़ का स्टॉक और बिक्री देखें
routes: /StockDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: b30cf269de71aa03
confidence: ok

**यह स्क्रीन किस काम की है**
**Stock Details** स्क्रीन पर हर राज्य की एक लाइन होती है, जिसमें शुरुआती स्टॉक, सप्लाई, बिक्री और आख़िरी स्टॉक दिखता है। यह हर दिन अपडेट होती है।

1. ऊपर **Date as on** और **Period** देखें।
2. **Filters** खोलें और **From Date**, **To Date**, **Financial Year**, **State**, **Region**, **Head Quarters** या **Product** चुनें। **Search** दबाएँ।
3. कार्ड पढ़ें: **Total Stock**, **Total Sales**, **Closing Stock** और **Sales %**।
4. **Stock List** में हर राज्य का **Opening Stock**, **Supplies**, **Total Stock**, **Sales**, **Total Sales**, **Closing Stock** और **Sales %** पढ़ें। आख़िरी लाइन **Grand Total** होती है।
5. किसी राज्य को ढूँढने के लिए सर्च बॉक्स में उसका नाम टाइप करें।
6. लिस्ट डाउनलोड करने के लिए **Excel** दबाएँ।

## आम दिक्कतें

- **"No Data Available".** (कोई डेटा नहीं है) **Clear All** दबाएँ, फिर **Search** दबाएँ।
- **कॉलम की तारीखें बदल जाती हैं।** कॉलम के हेडिंग में आपकी चुनी हुई अवधि की तारीखें दिखती हैं।
:::

::: section product-stock
title: राज्य के हिसाब से हर प्रोडक्ट का स्टॉक देखें
routes: /ProductWiseStockAvailability
for: 
roles: 
designations: 
minutes: 2
source-hash: 5858eb268707eedc
confidence: ok

**यह स्क्रीन किस काम की है**
**Product-wise Stock Availability** स्क्रीन दिखाती है कि हर राज्य में हर प्रोडक्ट का कितना स्टॉक है।

1. **Filters** खोलें और **State**, **Region**, **Head Quarters**, **Date From** या **Date To** चुनें। **Search** दबाएँ।
2. कार्ड पढ़ें: **Total States**, **Total Products**, **Total Quantity (MT)**, **Highest Stock State** और **Low Stock Alerts**।
3. **Stock Availability List** में हर लाइन एक राज्य है और हर कॉलम एक प्रोडक्ट है। **Total (MT)** उस राज्य का कुल जोड़ है।
4. किसी राज्य को ढूँढने के लिए सर्च बॉक्स में उसका नाम टाइप करें। आख़िरी लाइन **Grand Total** होती है।
5. टेबल डाउनलोड करने के लिए **Excel** दबाएँ।

## आम दिक्कतें

- **"No Data Available".** (कोई डेटा नहीं है) **Clear All** दबाएँ, फिर **Search** दबाएँ।
- **Low Stock Alerts की गिनती ज़्यादा है।** इन राज्यों में स्टॉक तय सीमा से कम है। इनके लिए सप्लाई की योजना बनाएँ।
:::

::: section stock-reconciliation
title: राज्य के हिसाब से स्टॉक का मिलान देखें
routes: /StateWiseGlobalStockReconciliation
for: 
roles: 
designations: 
minutes: 2
source-hash: 50c58211c889d4c4
confidence: ok

**यह स्क्रीन किस काम की है**
**State-wise Stock Reconciliation** स्क्रीन एक दिन के लिए, राज्य-दर-राज्य, कंपनी के स्टॉक का मिलान होलसेलर और रिटेलर के पास पड़े स्टॉक से करती है।

1. **As On Date:** चुनें और **View** दबाएँ। वापस आज की तारीख पर जाने के लिए **Today** दबाएँ।
2. अगर **Partial Report - Pending Uploads** बॉक्स दिखे, तो उस दिन की कुछ रिपोर्ट अभी लोड नहीं हुई हैं।
3. **Urea Stock** टेबल में हर राज्य का **Company Stock**, **Wholesaler Stock**, **Retail Point Stock**, रास्ते में चल रहा स्टॉक (GIT) और **Overall Stock** पढ़ें।
4. प्रोडक्ट और राज्य के हिसाब से स्टॉक देखने के लिए नीचे **GFL Stock** टेबल तक स्क्रोल करें।
5. किसी राज्य को ढूँढने के लिए सर्च बॉक्स में उसका नाम टाइप करें।
6. डाउनलोड करने के लिए **Export Excel** दबाएँ, या रिपोर्ट भेजने के लिए **Send Email** दबाएँ। **To (recipient email)** डालें, ज़रूरत हो तो **CC (optional)** जोड़ें, और **Send Email** दबाएँ।

## आम दिक्कतें

- **"No Reconciliation Data Available".** (मिलान का कोई डेटा नहीं है) उस तारीख की रिपोर्ट अपलोड नहीं हुई हैं। दूसरी तारीख चुनें, या Admin से उन्हें अपलोड करने को कहें। Admin **Go to File Upload** दबा सकते हैं।
- **किसी कॉलम में आंकड़े नहीं हैं।** उस दिन की वह रिपोर्ट नहीं है। **Partial Report** बॉक्स देखें।
:::

::: section rankings
title: टॉप रैंकिंग ज़िले, होलसेलर और रिटेलर
routes: /TopRankingDistrict, /TopRankingWholesalers, /TopRankingRetailers
for: 
roles: 
designations: 
minutes: 1
source-hash: aa745aaa1c48ea10
confidence: ok

**यह स्क्रीन किस काम की है**
रैंकिंग स्क्रीन पर वे ज़िले, होलसेलर और रिटेलर दिखते हैं जिन्होंने सबसे कम समय में सबसे ज़्यादा स्टॉक बेचा। मेन्यू से **Top Ranking District**, **Top Ranking Wholesaler** या **Top Ranking Retailers** खोलें।

1. जो रैंकिंग चाहिए, उसे मेन्यू से खोलें।
2. ऊपर के कार्ड पढ़ें, जैसे कुल आंकड़े और **Avg Liquidation Cycle**।
3. लिस्ट में रैंक 1 सबसे अच्छा है। हर एक का **State**, **Region**, **HQ**, **District** और **Sub-District** देखें।

> Careful: ये रैंकिंग स्क्रीन अभी लाइव डेटा से जोड़ी जा रही हैं। इन पर अभी के आंकड़े और फ़िल्टर सिर्फ़ उदाहरण हैं। असली आंकड़ों के लिए **Liquidation Cycle** इस्तेमाल करें।

## आम दिक्कतें

- **फ़िल्टर लगाने पर लिस्ट नहीं बदलती।** यह स्क्रीन अभी लाइव डेटा से नहीं जुड़ी है। **Liquidation Cycle** पर **Top 5 Performance** इस्तेमाल करें।
:::

::: section file-upload
title: IFMS की Excel रिपोर्ट अपलोड करें
routes: /ExcelFormatFileUpload
for: 
roles: Admin
designations: 
tour: 1 > [data-help=sl-fu-grid] | 2 > [data-help=sl-fu-file] | 3 > [data-help=sl-fu-date] | 4 > [data-help=sl-fu-upload]
minutes: 3
source-hash: 3c1d667ca6804537
confidence: ok

**यह स्क्रीन किस काम की है**
Admin रोज़ की IFMS रिपोर्ट लोड करने के लिए **Excel Format File Upload** स्क्रीन इस्तेमाल करता है। स्टॉक और लिक्विडेशन की सारी स्क्रीन यही डेटा पढ़ती हैं। मेन्यू में **File Upload** दबाकर इसे खोलें।

1. अपनी रिपोर्ट का कार्ड ढूँढें: **Retail Stocks**, **Company Sales**, **Sales and Receipt**, **Wholesale Sales**, **Wholesale Stock As On Today**, **State Wise Global Stock Reconciliation** या **Warehouse and RakePoint Global Stock**।
2. **Excel File** के नीचे वह फ़ाइल चुनें जो आपने IFMS से डाउनलोड की है।
3. अगर कार्ड पर **Report Date** है, तो देखें कि वह रिपोर्ट की ही तारीख है। यह आज की तारीख से शुरू होती है।
4. **Upload** दबाएँ और जब तक **Uploading...** दिखे, इंतज़ार करें। फिर एक मैसेज में कुल लाइनें और कितनी डाली गईं, अपडेट हुईं और छोड़ी गईं, यह दिखता है।

> Tip: हर कार्ड एक रिपोर्ट के लिए है। वह कार्ड चुनें जिसका नाम आपकी IFMS रिपोर्ट से मिलता है।

| फ़ील्ड | क्या भरें |
|---|---|
| **Excel File** | एक .xlsx या .csv फ़ाइल, 100 MB तक। |
| **Report Date** | रिपोर्ट किस तारीख की है। Retail Stocks, Sales and Receipt, State Wise Global Stock Reconciliation और Warehouse and RakePoint Global Stock के लिए ज़रूरी। |

## आम दिक्कतें

- **"Please select a file first."** (पहले फ़ाइल चुनें) उसी कार्ड पर फ़ाइल चुनें, फिर **Upload** दबाएँ।
- **"Only .xlsx and .csv files are supported."** (सिर्फ़ .xlsx और .csv फ़ाइलें चलती हैं) फ़ाइल को .xlsx या .csv में सेव करें और फिर से कोशिश करें।
- **"The selected file exceeds the 100 MB upload limit."** (चुनी गई फ़ाइल 100 MB से बड़ी है) फ़ाइल को छोटी-छोटी फ़ाइलों में बाँटें।
- **Upload Failed.** (अपलोड नहीं हुआ) मैसेज पढ़ें। देखें कि फ़ाइल उस कार्ड के लिए सही रिपोर्ट है, फिर से कोशिश करें।
:::
