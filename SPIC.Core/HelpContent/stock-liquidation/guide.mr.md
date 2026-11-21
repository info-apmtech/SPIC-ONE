---
module: stock-liquidation
title: स्टॉक आणि लिक्विडेशन
icon: bi-boxes
order: 106
language: mr
category: विक्री
note: नवीन मार्गदर्शक
meta-source-hash: b8a21e045ebf822e
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: fb227391bc0718ee
confidence: ok

SPIC चे खत कंपनीतून बाहेर पडल्यानंतर त्याचा मागोवा घ्या. डीलरनी अजून कोणत्या विक्रीची पोच (acknowledgement) दिलेली नाही, होलसेलर आणि रिटेलरकडे किती स्टॉक पडून आहे, तो किती जुना आहे आणि तो किती लवकर पुढे विकला जातो, हे पाहा. हे आकडे IFMS या सरकारी खत प्रणालीमधून येतात.

- ack | icon: bi-hourglass-split | color: #db2777
  title: बाकी असलेली पोच
  text: कंपनी आणि डीलरच्या ज्या विक्रीची खरेदीदाराने अजून IFMS मध्ये खात्री केलेली नाही, त्या पाहा.
- stock | icon: bi-boxes | color: #2563eb
  title: स्टॉक आणि त्याचे वय (ageing)
  text: प्रत्येक डीलरकडे किती स्टॉक आहे आणि तो किती दिवसांपासून तिथे आहे ते पाहा.
- liquidation | icon: bi-graph-up-arrow | color: #16a34a
  title: लिक्विडेशन
  text: डीलर त्यांचा स्टॉक किती लवकर विकतात आणि कोणते डीलर हळू आहेत ते पाहा.
:::

::: section flow
title: स्टॉक कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 12cbf705ed2b7495
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- company | type: lane | color: #db2777
  title: SPIC
  sub: कंपनीची विक्री
- dealers | type: lane | color: #7c3aed
  title: डीलर
  sub: होलसेलर आणि रिटेलर, IFMS मध्ये
- admin | type: lane | color: #374151 | roles: Admin
  title: Admin
  sub: IFMS च्या फाइल लोड करतात
- staff | type: lane | color: #0d9488 | roles: Director, AVP, SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: विक्री कर्मचारी
  sub: आपल्या भागाचे रिपोर्ट पाहतात
- sale | lane: company | col: 1 | icon: bi-receipt | color: #db2777 | next: ack | screen: pending-ack
  title: कंपनीची विक्री
  status: New
  text: SPIC होलसेलरला खत विकते. त्याचे इनव्हॉइस IFMS मध्ये नोंदवले जाते. होलसेलरही याच पद्धतीने रिटेलरला विकतात.
- ack | lane: dealers | col: 2 | icon: bi-clipboard-check | color: #16a34a | next: upload | screen: ack-cycle
  title: डीलर पोच देतात
  status: Ack
  text: स्टॉक पोहोचला याची खरेदीदार IFMS मध्ये खात्री करतो. इनव्हॉइसपासून माल मिळेपर्यंतचे दिवस म्हणजे पोचीचे चक्र (acknowledgement cycle).
- upload | lane: admin | col: 3 | icon: bi-file-earmark-arrow-up | color: #374151 | next: stock | screen: file-upload
  title: IFMS चे रिपोर्ट लोड करा
  text: IFMS चे रिपोर्ट रोज SPIC ONE मध्ये येतात, रात्रीच्या आपोआप इम्पोर्टने किंवा Admin च्या Excel अपलोडने.
- stock | lane: dealers | col: 4 | icon: bi-boxes | color: #2563eb | next: sell | screen: stock-report
  title: डीलरकडचा स्टॉक
  status: Fresh
  text: पोच दिलेला स्टॉक होलसेलर किंवा रिटेलरकडे असतो. त्याचे वय पोच दिलेल्या दिवसापासून मोजले जाते.
- sell | lane: dealers | col: 5 | icon: bi-cart-check | color: #16a34a | next: review | screen: liquidation-cycle
  title: पुढे विकला (लिक्विडेट झाला)
  status: Active
  text: डीलर स्टॉक पुढे रिटेलर आणि शेतकऱ्यांना विकतो. विक्रीमुळे शिल्लक स्टॉक कमी होतो.
- review | lane: staff | col: 6 | icon: bi-bar-chart | color: #0d9488 | screen: stock-details
  title: लक्ष ठेवा आणि पाठपुरावा करा
  text: कर्मचारी आपल्या भागाचा स्टॉक, विक्री आणि वय तपासतात आणि हळू असलेल्या डीलरकडे पाठपुरावा करतात.
- pending | lane: staff | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: sale | to: ack | screen: pending-ack
  title: वेळेवर पोच दिली नाही
  status: Overdue
  text: खरेदीदाराने पोच दिली नाही तर विक्री बाकी (pending) राहते आणि जुनी होत जाते. कर्मचारी डीलरला IFMS मध्ये पोच द्यायला सांगतात.
- aged | lane: staff | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: stock | to: sell | screen: ageing-report
  title: जुना स्टॉक हलत नाही
  status: Long Aged
  text: न विकलेला स्टॉक जुना होत जातो आणि त्यावर Long Aged किंवा Critical दिसते. कर्मचारी डीलरला तो आधी विकायला मदत करतात.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 5b6bc08644fdb0cc
confidence: ok

- new | color: #64748b
  label: New
  text: IFMS ची स्थिती. विक्रीची नोंद झाली आहे पण खरेदीदाराने अजून पोच दिलेली नाही.
- ack | color: #16a34a
  label: Ack
  text: IFMS ची स्थिती. स्टॉक पोहोचल्याची खरेदीदाराने पोच दिली आहे.
- completed | color: #16a34a
  label: Completed
  text: विक्रीची पोच दिली आहे. ती आता बाकी नाही.
- latest | color: #2563eb
  label: Latest
  text: 0 ते 10 दिवसांपासून पोचीची वाट पाहत आहे.
- overdue | color: #dc2626
  label: Overdue
  text: 21 किंवा त्याहून जास्त दिवसांपासून पोचीची वाट पाहत आहे.
- consent | color: #9333ea
  label: Consent of Buyer
  text: विक्री खरेदीदाराच्या संमतीची वाट पाहत आहे असे IFMS मध्ये दिसते.
- fast | color: #16a34a
  label: Fast
  text: इनव्हॉइसनंतर 0 ते 2 दिवसांत पोच दिली.
- normal | color: #d97706
  label: Normal
  text: इनव्हॉइसनंतर 3 ते 5 दिवसांत पोच दिली.
- delayed | color: #ea580c
  label: Delayed
  text: इनव्हॉइसनंतर 6 ते 10 दिवसांनी पोच दिली.
- pending-ack | color: #64748b
  label: Pending ACK
  text: स्टॉक रिपोर्ट. या स्टॉकची IFMS मध्ये अजून पोच नाही, म्हणून त्याला वय नाही.
- fresh | color: #059669
  label: Fresh
  text: 0 ते 30 दिवस जुना स्टॉक.
- medium | color: #34d399
  label: Medium
  text: 31 ते 90 दिवस जुना स्टॉक.
- slow | color: #f59e0b
  label: Slow Moving
  text: 91 ते 180 दिवस जुना स्टॉक.
- long | color: #ef4444
  label: Long Aged
  text: 181 ते 364 दिवस जुना स्टॉक.
- active | color: #16a34a
  label: Active
  text: लिक्विडेशन. डीलरने हा स्टॉक मागच्या 45 दिवसांत विकला आहे.
- monitoring | color: #d97706
  label: Monitoring
  text: लिक्विडेशन. 46 ते 75 दिवस विक्री नाही. लक्ष ठेवा.
- critical | color: #b91c1c
  label: Critical
  text: प्रत्येक स्क्रीनवरचा सगळ्यात वाईट गट. बाकी असलेली पोच: 11 ते 20 दिवस. पोचीचे चक्र: 10 दिवसांपेक्षा जास्त. स्टॉकचे वय: 365 किंवा त्याहून जास्त दिवस. लिक्विडेशन: 75 दिवसांपेक्षा जास्त काळ विक्री नाही.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e4dca540aba6f06
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- staff | color: #0d9488 | roles: SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: विक्री कर्मचारी
  - तुमच्या राज्याची, रिजनची किंवा HQ ची बाकी पोच, स्टॉक, वय आणि लिक्विडेशन पाहा
  - पोच देण्यात किंवा विकण्यात हळू असलेल्या डीलरकडे पाठपुरावा करा
  - लिस्ट Excel मध्ये डाउनलोड करा
- leaders | color: #db2777 | roles: Director, AVP
  title: Director आणि AVP
  - सगळ्या राज्यांचे आकडे पाहा
  - राज्ये, डीलर आणि प्रॉडक्ट यांची तुलना करा
- admin | color: #374151 | roles: Admin
  title: Admin
  - IFMS चे Excel रिपोर्ट रोज अपलोड करा
  - सगळ्या राज्यांचे आकडे पाहा
  - State-wise Stock Reconciliation ईमेलने पाठवा
- dealers | color: #7c3aed | roles: Dealer
  title: होलसेलर आणि रिटेलर
  - मिळालेल्या स्टॉकची IFMS मध्ये पोच द्या
  - स्टॉक लवकर पुढे विका
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 12f38854e10b0f55
confidence: ok

- overdue | popular: true
  q: विक्रीवर Overdue दिसते. मी काय करू?
  a: खरेदीदाराने 21 किंवा त्याहून जास्त दिवस IFMS मध्ये स्टॉकची पोच दिलेली नाही. डीलरला IFMS मध्ये पोच द्यायला सांगा. पुढच्या IFMS अपडेटनंतर त्यावर **Completed** दिसते.
- old-figures | popular: true
  q: आकडे जुने दिसतात किंवा रिपोर्ट रिकामा आहे
  a: रिपोर्टमध्ये SPIC ONE मध्ये शेवटचा लोड झालेला IFMS डेटा दिसतो. आजच्या फाइल अजून लोड झाल्या नसतील तर Admin ला त्या **File Upload** वर अपलोड करायला सांगा, मग **Refresh Data** दाबा.
- my-area
  q: मी दुसरे राज्य किंवा रिजन निवडू शकत नाही
  a: बहुतेक कर्मचाऱ्यांना फक्त त्यांचेच राज्य, रिजन किंवा HQ दिसते. Director, AVP आणि Admin यांना सगळी राज्ये दिसतात.
- no-rows
  q: मला "No records found." किंवा "No Data Available" दिसते
  a: तुमचे फिल्टर खूप मर्यादित असतील. **Clear All** दाबा, मग **Search** दाबा.
- pdf
  q: PDF आणि Send Mail बटणे राखाडी आहेत
  a: ही अजून तयार नाहीत. लिस्ट डाउनलोड करण्यासाठी **Excel** वापरा.
:::

::: section pending-ack
title: बाकी असलेली पोच पाहा
routes: /CompanySales
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-pa-filters] | 2 > [data-help=sl-pa-cards] | 3 > [data-help=sl-pa-tabs] | 4 > [data-help=sl-pa-table] | 5 > [data-help=sl-pa-export]
minutes: 3
source-hash: bc4973981c2b913d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Pending Acknowledgement** स्क्रीनवर कंपनी, होलसेलर आणि रिटेलरची विक्री दिसते, आणि खरेदीदाराने IFMS मध्ये त्याची पोच दिली आहे की नाही ते दिसते. मेन्यूमधून **Pending Acknowledgement** दाबून ही स्क्रीन उघडा.

1. **Filters** उघडा आणि **Date Range**, **State**, **District**, **Dealer Type**, **Dealer / Agency Name**, **Product** किंवा **Status (By Age)** निवडा. **Search** दाबा. **Clear All** ने फिल्टर निघतात.
2. तीन कार्डे वाचा: एकूण, **Company Sales** आणि **Wholesaler Sales**. प्रत्येकावर **Completed**, **Latest**, **Critical**, **Overdue** आणि **Consent of Buyer** दिसतात. खालचा चार्ट राज्यांची तुलना करतो.
3. एक टॅब निवडा: **All**, **Company Sales**, **Wholesaler Sales** किंवा **Retailer Sales**. विक्री शोधण्यासाठी सर्च बॉक्समध्ये इनव्हॉइस किंवा डीलरचे नाव टाइप करा.
4. टेबलमध्ये **Age Status** आणि **Pending Ack Age (Days)** पाहा. पूर्ण **Record Details** पाहण्यासाठी **View** दाबा.
5. लिस्ट डाउनलोड करण्यासाठी **Excel** दाबा.

> Tip: सगळ्यात जुन्या बाकी विक्री आधी पाहण्यासाठी **Pending Ack Age (Days)** नुसार क्रम लावा, आणि त्या डीलरना फोन करा.

| फील्ड | काय दाखवते |
|---|---|
| **Age Status** | **Latest** 0 ते 10 दिवस, **Critical** 11 ते 20 दिवस, **Overdue** 21 किंवा जास्त दिवस, किंवा **Consent of Buyer**. |
| **Workflow Status** | IFMS मधली स्थिती, उदा. **New** किंवा **Ack**. |
| **Quantity (MT)** | मेट्रिक टनमध्ये संख्या. |

## नेहमीच्या अडचणी

- **"No records found."** (कोणतीही नोंद सापडली नाही) Filters मध्ये **Clear All** दाबा, किंवा सर्च बॉक्स रिकामा करा.
- **टॅब बदलले तरी कार्डे बदलत नाहीत.** हे नेहमीचेच आहे. टॅबमुळे फक्त टेबल आणि Excel फाइल बदलते.
- **डीलरने पोच दिल्यानंतरही विक्री बाकी दिसते.** नवीन IFMS डेटा अजून लोड झालेला नाही. पुढच्या अपडेटनंतर परत तपासा.
:::

::: section ack-cycle
title: डीलर किती लवकर पोच देतात ते तपासा
routes: /Acknowledgement
for: 
roles: 
designations: 
minutes: 2
source-hash: 751788d0db9a139a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Acknowledgement Cycle** स्क्रीनवर इनव्हॉइसनंतर डीलरनी स्टॉकची पोच द्यायला किती दिवस घेतले ते दिसते. यात फक्त पोच दिलेल्या विक्री दिसतात.

1. **Filters** उघडा आणि **State**, **Region**, **Head Quarters**, **District**, **Product**, **Dealer Name**, **Status**, **Cycle Bucket** किंवा **Invoice Date From** आणि **Invoice Date To** निवडा. **Search** दाबा.
2. कार्डे वाचा: **Total Transactions**, **Fast (0-2 Days)**, **Normal (3-5 Days)**, **Delayed (6-10 Days)** आणि **Critical (> 10 Days)**.
3. वरच्या दोन टॉप 5 लिस्ट कशानुसार गट करायच्या ते निवडा: **State**, **Region**, **Head Quarters**, **District**, **Product** किंवा **Dealer Name**.
4. **Top 5 Fast Performing** लिस्टची **Top 5 Delayed** लिस्टशी तुलना करा.
5. **Acknowledgement Cycle List** मध्ये प्रत्येक विक्रीचे **Ack Cycle (Days)** आणि **Status** पाहा. **Acknowledgement Details** पाहण्यासाठी **View** दाबा.
6. लिस्ट डाउनलोड करण्यासाठी **Excel** दाबा.

## नेहमीच्या अडचणी

- **A top 5 list says "No data for the current filters."** (टॉप 5 लिस्टवर "या फिल्टरसाठी डेटा नाही" दिसते) रँक मिळण्यासाठी एका गटात किमान 50 पोच दिलेल्या विक्री लागतात. फिल्टर थोडे मोठे करा.
- **"No acknowledged transactions found."** (पोच दिलेली कोणतीही विक्री सापडली नाही) **Clear All** दाबा, मग **Search** दाबा.
:::

::: section stock-report
title: डीलरकडचा स्टॉक पाहा
routes: /StockReport
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-sr-filters] | 2 > [data-help=sl-sr-cards] | 3 > [data-help=sl-sr-charts] | 4 > [data-help=sl-sr-search] | 5 > [data-help=sl-sr-table] | 6 > [data-help=sl-sr-export]
minutes: 3
source-hash: 5c6d4945a1c3da6d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Stock Report** स्क्रीनवर प्रत्येक होलसेलर आणि रिटेलरकडे आत्ता किती स्टॉक आहे आणि तो किती जुना आहे ते दिसते.

1. **Filters** उघडा आणि **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying With**, **Product** किंवा **Ageing Range (Days)** निवडा. **Search** दाबा.
2. कार्डे वाचा: **Total Stock (Current Stock)**, **Dealers Count**, **Average Ageing** आणि **High Ageing Stock**.
3. चार्ट पाहा: **State-wise Stock Availability** या वर्षाची मागच्या वर्षाशी तुलना करतो, आणि **Product-wise Stock Distribution (MT)** प्रत्येक प्रॉडक्टचा वाटा दाखवतो.
4. **Stock Report List** च्या वरच्या सर्च बॉक्समध्ये डीलर, राज्य किंवा प्रॉडक्ट टाइप करा.
5. टेबलमध्ये **Quantity (MT)**, स्टॉक कोणाकडे आहे (**Lying With**), **Ageing in Days** आणि **Status** पाहा. पूर्ण **Stock Details** पाहण्यासाठी **View** दाबा.
6. लिस्ट डाउनलोड करण्यासाठी **Excel** दाबा.

> Tip: जुना स्टॉक पटकन शोधण्यासाठी **Ageing Range (Days)** मध्ये **Long Aged (181-364)** आणि **Critical (365+)** निवडा.

## नेहमीच्या अडचणी

- **"No Data Available".** (डेटा उपलब्ध नाही) Filters मध्ये **Clear All** दाबा, किंवा सर्च बॉक्स रिकामा करा.
- **Ageing मध्ये "--" दिसते आणि स्थिती Pending ACK आहे.** वय फक्त IFMS मधल्या पोचीपासून मोजले जाते. पोच नसलेल्या स्टॉकला अजून वय नसते.
:::

::: section ageing-report
title: जुना आणि हळू विकला जाणारा स्टॉक शोधा
routes: /AgeingReport
for: 
roles: 
designations: 
minutes: 2
source-hash: 366d05a43d5fbe3a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Ageing Report** स्क्रीनवर स्टॉक डीलरकडे किती दिवसांपासून पडून आहे ते दिसते, त्यामुळे तुम्ही हळू विकला जाणारा स्टॉक शोधू शकता.

1. **Filters** उघडा आणि **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying with**, **Product** किंवा **Ageing Range (Days)** निवडा. **Search** दाबा.
2. कार्डे वाचा: **Total Stock (MT)**, **Average Ageing (Days)**, **30-60 Days Stock (MT)** आणि **60+ Days Stock (MT)**.
3. **State-wise Stock by Ageing (MT)** मध्ये एखाद्या बारवर बोट ठेवा, त्या राज्याचा प्रत्येक वयोगट दिसेल.
4. **Ageing by Days (MT)** मध्ये प्रत्येक वयोगटात किती स्टॉक आहे ते पाहा.
5. **Ageing Report List** मध्ये प्रत्येक डीलरचे **Ageing in Days** आणि **Status** पाहा. **Stock Ageing Details** पाहण्यासाठी **View** दाबा.
6. लिस्ट डाउनलोड करण्यासाठी **Excel** दाबा.

## नेहमीच्या अडचणी

- **"No records found."** (कोणतीही नोंद सापडली नाही) **Clear All** दाबा, मग **Search** दाबा.
- **काही जुना स्टॉक दिसत नाही.** वय फक्त IFMS मधल्या पोचीपासून मोजले जाते.
:::

::: section liquidation-cycle
title: स्टॉक किती लवकर विकला जातो ते पाहा
routes: /LiquidationCycle
for: 
roles: 
designations: 
minutes: 3
source-hash: f76600523961f075
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Liquidation Cycle** स्क्रीनवर प्रत्येक डीलरकडे किती स्टॉक होता, त्यांनी किती विकला आणि शेवटच्या विक्रीला किती दिवस झाले ते दिसते.

1. **Filters** उघडा आणि **Date From**, **Date To**, **State**, **Region**, **HQ**, **District**, **Sub District**, **Dealer Name**, **Product** किंवा **Financial Year** निवडा. **Apply Filters** दाबा.
2. कार्डे वाचा: **Total Stock (MT)**, **Liquidated (MT)** आणि **Balance Stock (MT)**.
3. **Dealer Liquidation Performance** खाली **Top 5 Performance**, **State-wise**, **All**, **Company Sales**, **Wholesalers** किंवा **Retailers** निवडा.
4. जलद लिस्टची हळू लिस्टशी तुलना करा, उदा. **Top 5 Fast Dealers** आणि **Top 5 Slow Dealers**.
5. **Liquidation Cycle List** मध्ये **Stock (MT)**, **Ageing (Days)**, **Sales (MT)** आणि **Status** पाहा. **Liquidation Details** पाहण्यासाठी **View** दाबा.
6. लिस्ट डाउनलोड करण्यासाठी **Excel** दाबा.

> Tip: इथे **Ageing (Days)** म्हणजे डीलरने त्या प्रॉडक्टची शेवटची विक्री केल्यापासूनचे दिवस.

## नेहमीच्या अडचणी

- **"No records found."** (कोणतीही नोंद सापडली नाही) **Clear All** दाबा, मग **Apply Filters** दाबा.
- **मी दुसरे राज्य निवडू शकत नाही.** तुमचा भाग तुमच्या लॉगिननुसार ठरलेला असतो. Director, AVP आणि Admin यांना सगळी राज्ये दिसतात.
:::

::: section stock-details
title: राज्यानुसार रोजचा स्टॉक आणि विक्री पाहा
routes: /StockDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: b30cf269de71aa03
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Stock Details** स्क्रीनवर प्रत्येक राज्याची एक ओळ असते, त्यात सुरुवातीचा स्टॉक, पुरवठा, विक्री आणि शेवटचा स्टॉक दिसतो. ही रोज अपडेट होते.

1. वर **Date as on** आणि **Period** पाहा.
2. **Filters** उघडा आणि **From Date**, **To Date**, **Financial Year**, **State**, **Region**, **Head Quarters** किंवा **Product** निवडा. **Search** दाबा.
3. कार्डे वाचा: **Total Stock**, **Total Sales**, **Closing Stock** आणि **Sales %**.
4. **Stock List** मध्ये प्रत्येक राज्याचे **Opening Stock**, **Supplies**, **Total Stock**, **Sales**, **Total Sales**, **Closing Stock** आणि **Sales %** वाचा. शेवटची ओळ **Grand Total** असते.
5. राज्य शोधण्यासाठी सर्च बॉक्समध्ये त्याचे नाव टाइप करा.
6. लिस्ट डाउनलोड करण्यासाठी **Excel** दाबा.

## नेहमीच्या अडचणी

- **"No Data Available".** (डेटा उपलब्ध नाही) **Clear All** दाबा, मग **Search** दाबा.
- **कॉलमच्या तारखा बदलतात.** कॉलमच्या शीर्षकात तुम्ही निवडलेल्या कालावधीच्या तारखा दिसतात.
:::

::: section product-stock
title: राज्यानुसार प्रत्येक प्रॉडक्टचा स्टॉक पाहा
routes: /ProductWiseStockAvailability
for: 
roles: 
designations: 
minutes: 2
source-hash: 5858eb268707eedc
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Product-wise Stock Availability** स्क्रीनवर प्रत्येक राज्यात प्रत्येक प्रॉडक्टचा किती स्टॉक आहे ते दिसते.

1. **Filters** उघडा आणि **State**, **Region**, **Head Quarters**, **Date From** किंवा **Date To** निवडा. **Search** दाबा.
2. कार्डे वाचा: **Total States**, **Total Products**, **Total Quantity (MT)**, **Highest Stock State** आणि **Low Stock Alerts**.
3. **Stock Availability List** मध्ये प्रत्येक ओळ एक राज्य आहे आणि प्रत्येक कॉलम एक प्रॉडक्ट आहे. **Total (MT)** म्हणजे त्या राज्याची एकूण बेरीज.
4. राज्य शोधण्यासाठी सर्च बॉक्समध्ये त्याचे नाव टाइप करा. शेवटची ओळ **Grand Total** असते.
5. टेबल डाउनलोड करण्यासाठी **Excel** दाबा.

## नेहमीच्या अडचणी

- **"No Data Available".** (डेटा उपलब्ध नाही) **Clear All** दाबा, मग **Search** दाबा.
- **Low Stock Alerts चा आकडा जास्त आहे.** या राज्यांमध्ये स्टॉक मर्यादेपेक्षा कमी आहे. त्यांच्यासाठी पुरवठ्याचे नियोजन करा.
:::

::: section stock-reconciliation
title: राज्यानुसार स्टॉकचा ताळमेळ तपासा
routes: /StateWiseGlobalStockReconciliation
for: 
roles: 
designations: 
minutes: 2
source-hash: 50c58211c889d4c4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**State-wise Stock Reconciliation** स्क्रीन एका दिवसासाठी, राज्यानुसार, कंपनीच्या स्टॉकचा होलसेलर आणि रिटेलरकडे असलेल्या स्टॉकशी ताळमेळ घालते.

1. **As On Date:** निवडा आणि **View** दाबा. परत आजच्या तारखेवर जाण्यासाठी **Today** दाबा.
2. **Partial Report - Pending Uploads** बॉक्स दिसला तर त्या दिवसाचे काही रिपोर्ट अजून लोड झालेले नाहीत.
3. **Urea Stock** टेबलमध्ये प्रत्येक राज्याचा **Company Stock**, **Wholesaler Stock**, **Retail Point Stock**, रस्त्यात असलेला स्टॉक (GIT) आणि **Overall Stock** वाचा.
4. प्रॉडक्ट आणि राज्यानुसार स्टॉक पाहण्यासाठी खाली **GFL Stock** टेबलपर्यंत स्क्रोल करा.
5. राज्य शोधण्यासाठी सर्च बॉक्समध्ये त्याचे नाव टाइप करा.
6. डाउनलोड करण्यासाठी **Export Excel** दाबा, किंवा रिपोर्ट पाठवण्यासाठी **Send Email** दाबा. **To (recipient email)** टाका, गरज असेल तर **CC (optional)** जोडा, आणि **Send Email** दाबा.

## नेहमीच्या अडचणी

- **"No Reconciliation Data Available".** (ताळमेळाचा डेटा उपलब्ध नाही) त्या तारखेचे रिपोर्ट अपलोड झालेले नाहीत. दुसरी तारीख निवडा, किंवा Admin ला ते अपलोड करायला सांगा. Admin **Go to File Upload** दाबू शकतात.
- **एका कॉलममध्ये आकडे नाहीत.** त्या दिवसाचा तो रिपोर्ट नाही. **Partial Report** बॉक्स पाहा.
:::

::: section rankings
title: टॉप रँकिंग जिल्हे, होलसेलर आणि रिटेलर
routes: /TopRankingDistrict, /TopRankingWholesalers, /TopRankingRetailers
for: 
roles: 
designations: 
minutes: 1
source-hash: aa745aaa1c48ea10
confidence: ok

**ही स्क्रीन कशासाठी आहे**
रँकिंग स्क्रीनवर सगळ्यात कमी वेळात सगळ्यात जास्त स्टॉक विकणारे जिल्हे, होलसेलर आणि रिटेलर दिसतात. मेन्यूमधून **Top Ranking District**, **Top Ranking Wholesaler** किंवा **Top Ranking Retailers** उघडा.

1. तुम्हाला हवी ती रँकिंग मेन्यूमधून उघडा.
2. वरची कार्डे वाचा, जसे एकूण आकडे आणि **Avg Liquidation Cycle**.
3. लिस्टमध्ये रँक 1 सगळ्यात चांगला असतो. प्रत्येकाचे **State**, **Region**, **HQ**, **District** आणि **Sub-District** पाहा.

> Careful: या रँकिंग स्क्रीन अजून लाइव्ह डेटाशी जोडल्या जात आहेत. त्यावरचे आकडे आणि फिल्टर सध्या फक्त उदाहरण आहेत. खऱ्या आकड्यांसाठी **Liquidation Cycle** वापरा.

## नेहमीच्या अडचणी

- **फिल्टर लावल्यावर लिस्ट बदलत नाही.** ही स्क्रीन अजून लाइव्ह डेटाशी जोडलेली नाही. **Liquidation Cycle** वरचे **Top 5 Performance** वापरा.
:::

::: section file-upload
title: IFMS चे Excel रिपोर्ट अपलोड करा
routes: /ExcelFormatFileUpload
for: 
roles: Admin
designations: 
tour: 1 > [data-help=sl-fu-grid] | 2 > [data-help=sl-fu-file] | 3 > [data-help=sl-fu-date] | 4 > [data-help=sl-fu-upload]
minutes: 3
source-hash: 3c1d667ca6804537
confidence: ok

**ही स्क्रीन कशासाठी आहे**
रोजचे IFMS रिपोर्ट लोड करण्यासाठी Admin **Excel Format File Upload** स्क्रीन वापरतात. स्टॉक आणि लिक्विडेशनच्या सगळ्या स्क्रीन हाच डेटा वाचतात. मेन्यूमधून **File Upload** दाबून ही स्क्रीन उघडा.

1. तुमच्या रिपोर्टचे कार्ड शोधा: **Retail Stocks**, **Company Sales**, **Sales and Receipt**, **Wholesale Sales**, **Wholesale Stock As On Today**, **State Wise Global Stock Reconciliation** किंवा **Warehouse and RakePoint Global Stock**.
2. **Excel File** खाली तुम्ही IFMS मधून डाउनलोड केलेली फाइल निवडा.
3. कार्डवर **Report Date** असेल तर ती रिपोर्टचीच तारीख आहे ना ते तपासा. ती आजच्या तारखेपासून सुरू होते.
4. **Upload** दाबा आणि **Uploading...** दिसत असेपर्यंत थांबा. मग एका मेसेजमध्ये एकूण ओळी आणि किती ओळी घातल्या, अपडेट झाल्या आणि सोडल्या ते दिसते.

> Tip: प्रत्येक कार्ड एका रिपोर्टसाठी आहे. तुमच्या IFMS रिपोर्टच्या नावाशी जुळणारे कार्ड निवडा.

| फील्ड | काय भरायचे |
|---|---|
| **Excel File** | एक .xlsx किंवा .csv फाइल, 100 MB पर्यंत. |
| **Report Date** | रिपोर्ट कोणत्या तारखेचा आहे. Retail Stocks, Sales and Receipt, State Wise Global Stock Reconciliation आणि Warehouse and RakePoint Global Stock साठी आवश्यक. |

## नेहमीच्या अडचणी

- **"Please select a file first."** (आधी फाइल निवडा) त्याच कार्डवर फाइल निवडा, मग **Upload** दाबा.
- **"Only .xlsx and .csv files are supported."** (फक्त .xlsx आणि .csv फाइल चालतात) फाइल .xlsx किंवा .csv म्हणून सेव्ह करा आणि परत प्रयत्न करा.
- **"The selected file exceeds the 100 MB upload limit."** (निवडलेली फाइल 100 MB पेक्षा मोठी आहे) फाइलचे लहान फाइलमध्ये तुकडे करा.
- **Upload Failed.** (अपलोड झाले नाही) मेसेज वाचा. फाइल त्या कार्डसाठी योग्य रिपोर्ट आहे ना ते तपासा, मग परत प्रयत्न करा.
:::
