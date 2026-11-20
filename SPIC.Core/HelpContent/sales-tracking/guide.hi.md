---
module: sales-tracking
title: सेल्स प्लान और ऑडिट
icon: bi-graph-up-arrow
order: 105
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
source-hash: 1976de717daddf89
confidence: ok

CSR-2 प्रोग्राम को आख़िरी मंज़ूरी मिलने के बाद उसकी बिक्री का प्लान बनाना और उसे जाँचना होता है। प्रोडक्ट के हिसाब से सेल्स प्लान बनाएँ, उसे कन्फ़र्म करें, SAP से आई डीलर बिक्री को उस प्लान के सामने सेव करें, और स्कीम खत्म होने पर देखें कि टारगेट पूरा हुआ या नहीं।

- plan | icon: bi-pencil-square | color: #2563eb
  title: प्लान बनाएँ
  text: स्कीम की तारीखें और जो प्रोडक्ट, मात्रा और कीमत आप बेचना चाहते हैं, वह भरें।
- track | icon: bi-graph-up-arrow | color: #0d9488
  title: बिक्री सेव करें और ट्रैक करें
  text: SAP के डीलर इनवॉइस अपलोड करें और हर दिन उन्हें प्लान के सामने सेव करें।
- review | icon: bi-flag | color: #d97706
  title: आख़िर में जाँच करें
  text: देखें कि Required Turnover पूरा हुआ या नहीं। अगर नहीं हुआ, तो SMM और समय माँग सकता है।
:::

::: section flow
title: सेल्स ऑडिट कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 63b369579a95c59f
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- owner | type: lane | color: #2563eb
  title: प्रोग्राम का ज़िम्मेदार
  sub: CSR-1 पर लिखा ज़िम्मेदार व्यक्ति (Responsible Person)
- data | type: lane | color: #64748b
  title: बिक्री का डेटा
  sub: SAP इनवॉइस की Excel फ़ाइल
- managers | type: lane | color: #0d9488 | roles: RM, SMM, SMD, Admin, SuperAdmin
  title: मैनेजर
  sub: RM · SMM · SMD
- approvers | type: lane | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: एक्सटेंशन मंज़ूर करने वाले
  sub: AVP · Admin
- start | lane: owner | col: 1 | icon: bi-play-circle | color: #64748b | next: plan | screen: audit-overview
  title: ऑडिट शुरू करें
  status: Not Started
  text: **Sales Audit** पर **Start Audit** दबाएँ। प्रोग्राम और बजट देखें, फिर **Continue to sales plan** दबाएँ।
- plan | lane: owner | col: 2 | icon: bi-pencil-square | color: #94a3b8 | next: dealers | screen: sales-plan
  title: सेल्स प्लान बनाएँ
  status: Draft
  text: स्कीम की तारीखें और प्रोडक्ट भरें। **Save Draft** या **Save & Continue** दबाएँ।
- dealers | lane: owner | col: 3 | icon: bi-people | color: #0891b2 | next: confirm
  title: डीलर जोड़ें
  status: Draft
  text: **Dealers** स्टेप पर वे डीलर चुनें जिनकी बिक्री इस प्रोग्राम में गिनी जाएगी।
- confirm | lane: owner | col: 4 | icon: bi-check2-square | color: #2563eb | next: upload | return: plan | screen: plan-review
  title: प्लान कन्फ़र्म करें
  status: Plan Confirmed
  text: **Review & Confirm Plan** पर सब कुछ जाँचें और **Confirm Sales Plan** दबाएँ। इसके बाद प्लान लॉक हो जाता है। उससे पहले आप **Edit Sales Plan** से वापस जा सकते हैं।
- upload | lane: data | col: 5 | icon: bi-cloud-arrow-up | color: #64748b | next: save | screen: dealer-sales-upload
  title: SAP बिक्री अपलोड करें
  status: New
  text: SAP के डीलर इनवॉइस की Excel फ़ाइल **Dealer Sales List** पर **Bulk Upload** से अपलोड की जाती है।
- save | lane: owner | col: 6 | icon: bi-save | color: #d97706 | next: track | screen: daily-sales
  title: रोज़ की बिक्री सेव करें
  status: In Progress
  text: **Review & Confirm Plan** पर **Refresh Sales** दबाएँ और फिर **Save Daily Sales** दबाएँ। नई इनवॉइस लाइनें **Old** हो जाती हैं और अचीवमेंट में जुड़ जाती हैं।
- track | lane: managers | col: 7 | icon: bi-graph-up-arrow | color: #0d9488 | next: review | screen: tracking-list
  title: प्रगति देखें
  status: In Progress
  text: RM, SMM और SMD अपने इलाके की बिक्री **Sales Tracking** पर देखते हैं।
- review | lane: owner | col: 8 | icon: bi-flag | color: #16a34a | screen: period-end-review
  title: स्कीम खत्म होने पर जाँच
  status: Completed
  text: जब सेव की गई बिक्री **Required Turnover** तक पहुँच जाती है, तो ऑडिट पर **Completed** दिखता है। **Period-End Review** पर आख़िरी नतीजा दिखता है।
- extension | lane: approvers | col: 8 | icon: bi-calendar-plus | color: #dc2626 | kind: exception | from: review | to: track | screen: extension-review
  title: और समय माँगें
  status: Pending
  text: अगर टारगेट पूरा नहीं हुआ, तो SMM **Request Extension** दबाता है। AVP या Admin उसे मंज़ूर या रिजेक्ट करता है। मंज़ूर होने पर स्कीम की आख़िरी तारीख आगे बढ़ जाती है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 154f433d358852fc
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: प्रोग्राम मंज़ूर है और इंतज़ार में है। **Start Audit** दबाएँ।
- draft | color: #94a3b8
  label: Draft
  text: ऑडिट शुरू हो गया है। प्लान सेव है पर कन्फ़र्म नहीं हुआ।
- plan-confirmed | color: #2563eb
  label: Plan Confirmed
  text: प्लान लॉक हो गया है। अब उसके सामने बिक्री सेव की जा सकती है।
- in-progress | color: #d97706
  label: In Progress
  text: बिक्री सेव हो रही है। टारगेट अभी पूरा नहीं हुआ।
- sales-tracking | color: #0d9488
  label: Sales Tracking
  text: स्कीम का समय चल रहा है।
- period-expired | color: #ea580c
  label: Period Expired
  text: टारगेट पूरा होने से पहले ही स्कीम का समय खत्म हो गया।
- extension-request | color: #7c3aed
  label: Extension Request
  text: और समय माँगा गया है। फ़ैसले का इंतज़ार है।
- completed | color: #16a34a
  label: Completed
  text: सेव की गई बिक्री Required Turnover तक पहुँच गई। ऑडिट बंद हो गया है।
- ext-pending | color: #d97706
  label: Pending
  text: एक्सटेंशन की रिक्वेस्ट AVP या Admin के पास रुकी है।
- ext-pending-admin | color: #d97706
  label: Pending Admin Review
  text: जब तक आपकी एक्सटेंशन रिक्वेस्ट पर फ़ैसला नहीं होता, ट्रैकिंग स्क्रीन पर यह दिखता है।
- ext-approved | color: #16a34a
  label: Approved
  text: एक्सटेंशन मंज़ूर हो गया और स्कीम की आख़िरी तारीख बदल दी गई।
- ext-rejected | color: #dc2626
  label: Rejected
  text: एक्सटेंशन रिजेक्ट हो गया। स्कीम की आख़िरी तारीख नहीं बदली।
- invoice-new | color: #0891b2
  label: New
  text: SAP इनवॉइस की ऐसी लाइन जो अभी ऑडिट में सेव नहीं हुई।
- invoice-old | color: #475569
  label: Old
  text: SAP इनवॉइस की लाइन जो पहले ही सेव हो चुकी है। यह कभी दो बार नहीं गिनी जाती।
- met | color: #16a34a
  label: Met
  text: प्लान की गई कीमत कम से कम Required Turnover के बराबर है।
- not-met | color: #dc2626
  label: Not Met
  text: प्लान की गई कीमत Required Turnover से कम है। आप अभी प्लान कन्फ़र्म नहीं कर सकते।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: f676ec8395aa4ec8
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- owner | color: #2563eb
  title: प्रोग्राम का ज़िम्मेदार
  - ऑडिट शुरू करना और सेल्स प्लान बनाना
  - डीलर जोड़ना और प्लान कन्फ़र्म करना
  - रोज़ की बिक्री सेव करना
- data | color: #64748b
  title: बिक्री का डेटा अपलोड
  - SAP की डीलर बिक्री वाली Excel फ़ाइल अपलोड करना
- managers | color: #0d9488 | roles: RM, SMM, SMD
  title: RM, SMM, SMD
  - अपने इलाके की बिक्री की ट्रैकिंग देखना
  - SMM: टारगेट पूरा न होने पर एक्सटेंशन माँगना
- approvers | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: AVP और Admin
  - एक्सटेंशन की रिक्वेस्ट मंज़ूर या रिजेक्ट करना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e0470c8f452a2a10
confidence: ok

- no-programme | popular: true
  q: मेरा प्रोग्राम Sales Audit लिस्ट में नहीं है
  a: लिस्ट में सिर्फ़ आख़िरी मंज़ूरी पाए वही CSR-2 प्रोग्राम दिखते हैं जिनके CSR-1 पर आप ज़िम्मेदार व्यक्ति (Responsible Person) हैं। CSR-2 का स्टेटस और ज़िम्मेदार व्यक्ति देखें।
- confirm-grey | popular: true
  q: Confirm Sales Plan बटन ग्रे है
  a: आपको स्कीम का समय, कम से कम एक प्रोडक्ट, **Met** दिखाता हुआ **Requirement Status**, और कम से कम एक जुड़ा हुआ डीलर चाहिए। जो कमी है उसे **Edit Sales Plan** या **Edit Dealer Mapping** से ठीक करें।
- zero-new
  q: Save Daily Sales पर 0 New दिख रहा है
  a: आपके डीलर, प्रोडक्ट और स्कीम की तारीखों से मिलती कोई नई SAP इनवॉइस लाइन नहीं है। **Dealer Sales List** पर नई SAP बिक्री अपलोड करें, फिर **Refresh Sales** दबाएँ।
- missing-id
  q: मुझे "Missing csr2Id" दिख रहा है
  a: ये स्क्रीन **Sales Audit** या **Sales Tracking** लिस्ट से ही खोलनी होती हैं, सेव किए हुए लिंक से नहीं। लिस्ट पर वापस जाएँ और प्रोग्राम फिर से खोलें।
- extension-grey
  q: Request Extension बटन ग्रे है
  a: सिर्फ़ SMM ही **Period-End Review** से एक्सटेंशन माँग सकता है।
- upload-columns
  q: मेरा Excel अपलोड "Missing columns" दिखाकर फ़ेल हो जाता है
  a: Excel की पहली लाइन में ये नाम होने चाहिए: Billing Document, Billing Date, Item, Customer Number, Customer Name, Product Code, Product Name, Quantity, Unit of Measure, Gross Amount.
:::

::: section audit-list
title: मेरे सेल्स ऑडिट देखें
routes: /SalesAudit
for: 
roles: 
designations: 
minutes: 2
source-hash: dc0f5ec250d5599d
confidence: ok

**यह स्क्रीन किस काम की है**
**Sales Audit** स्क्रीन पर वे मंज़ूर CSR-2 प्रोग्राम दिखते हैं जिनके आप ज़िम्मेदार हैं, उनके स्कीम के समय, टर्नओवर और स्टेटस के साथ। हर ऑडिट यहीं से शुरू करें या आगे बढ़ाएँ।

1. **Filters** खोलकर **Sanction Number**, **Program Name**, **Location**, **Date Range** या **Status** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
2. ऊपर के कार्ड देखें, जैसे **Total Programs**, **Not started**, **In Progress** और **Completed**। इनसे पता चलता है कि किस स्टेप पर कितने प्रोग्राम हैं।
3. **Sales Audit List** में प्रोग्राम ढूँढने के लिए सर्च बॉक्स में टाइप करें।
4. हर लाइन में **Scheme Period**, **Required TO (Auto)**, **Actual Turnover** और **Achievement** देखें।
5. **Status** कॉलम का बटन दबाएँ: नए प्रोग्राम के लिए **Start Audit**, जो आपने शुरू किया उसके लिए **View / Continue**, या **View Achievement**, **Request Extension**, **View Request** या **View Audit**।

## आम दिक्कतें

- **"No eligible sales audit records found".** (कोई सही सेल्स ऑडिट नहीं मिला) यहाँ सिर्फ़ आख़िरी मंज़ूरी पाए वही CSR-2 प्रोग्राम दिखते हैं जिनके आप ज़िम्मेदार व्यक्ति (Responsible Person) हैं।
- **तारीख का फ़िल्टर कुछ नहीं करता।** तारीखें चुनें, तारीख वाले बॉक्स में **Done** दबाएँ, फिर **Apply Filters** दबाएँ।
- **Scheme Period में "Not Set" दिख रहा है।** सेल्स प्लान अभी नहीं बना है। **Start Audit** या **View / Continue** दबाएँ।
:::

::: section audit-overview
title: प्रोग्राम और बजट जाँचें
routes: /AuditOverview
for: 
roles: 
designations: 
minutes: 2
source-hash: c767964e677164ff
confidence: ok

**यह स्क्रीन किस काम की है**
**Audit Overview** पहला स्टेप (**Program**) है। इसमें मंज़ूर CSR-2 और उससे जुड़े CSR-1 की जानकारी दिखती है, और वह बजट दिखता है जिससे **Required Turnover** तय होता है।

1. **Sales Audit** पर किसी प्रोग्राम के लिए **Start Audit** या **View / Continue** दबाएँ।
2. ऊपर की पट्टी देखें: **Program Type**, **Program Name**, **Sanction Number** और **CSR-2 Status**।
3. **Program Information** देखें: State, Region, HQ, CSR-2 कार्यक्रम की तारीख और स्कीम की तारीखें।
4. **Program Budget & Turnover** देखें। **Required Turnover** को **Total Approved Budget** से निकाला जाता है।
5. ऑडिट शुरू करने के लिए **Save Draft** दबाएँ, या दूसरे स्टेप पर जाने के लिए **Continue to sales plan** दबाएँ।

> Tip: नियम बजट कार्ड पर लिखा होता है: हर ₹1.50 लाख बजट पर ₹1.00 करोड़ टर्नओवर।

## आम दिक्कतें

- **"Missing csr2Id".** (csr2Id नहीं है) यह स्क्रीन **Sales Audit** लिस्ट से **Start Audit** दबाकर खोलें।
- **"Access denied (403)".** (इजाज़त नहीं है) आप इस प्रोग्राम के ज़िम्मेदार व्यक्ति (Responsible Person) नहीं हैं।
- **Save Draft ग्रे है।** ऑडिट पहले ही फ़ाइनल सबमिट हो चुका है।
:::

::: section sales-plan
title: सेल्स प्लान बनाएँ
routes: /SalesPlan
for: 
roles: 
designations: 
tour: 1 > [data-help=st-plan-period] | 2 > [data-help=st-plan-add] | 3 > [data-help=st-plan-table] | 4 > [data-help=st-plan-summary] | 5 > [data-help=st-plan-actions]
minutes: 4
source-hash: 2570b597b9143eea
confidence: ok

**यह स्क्रीन किस काम की है**
**Sales Plan & Scheme Period** दूसरा स्टेप है। यहाँ स्कीम की तारीखें और जो प्रोडक्ट, मात्रा और कीमत आप बेचना चाहते हैं, वह भरें।

1. **Scheme Period** में **From Date** और **To Date** चुनें। **Duration** में महीने और दिन दिखते हैं।
2. नई लाइन जोड़ने के लिए **+ Add Product** दबाएँ।
3. हर लाइन में **Product Group** और **Product** चुनें, फिर **Planned Quantity** और **Planned Value (₹)** भरें। डस्टबिन आइकन से लाइन हटती है।
4. **Plan Summary & Comparison** देखें। **Requirement Status** में **Met** दिखना चाहिए: **Total Planned Commitment** कम से कम **Required Turnover** के बराबर होनी चाहिए।
5. अपना काम रखने के लिए **Save Draft** दबाएँ, या सेव करके **Dealers** स्टेप पर जाने के लिए **Save & Continue** दबाएँ।

> Careful: प्लान कन्फ़र्म होने के बाद यह स्क्रीन लॉक हो जाती है। तब बटन पर **Continue to Dealer Mapping** लिखा आता है।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **From Date**, **To Date** | स्कीम की शुरुआत और अंत। अंत की तारीख शुरुआत से पहले की नहीं हो सकती। |
| **Product Group** | इसे पहले चुनें। फिर **Product** लिस्ट में उसके प्रोडक्ट दिखते हैं। |
| **Product** | हर प्रोडक्ट सिर्फ़ एक बार। |
| **Unit** | हमेशा MT। |
| **Planned Quantity** | MT में प्लान की गई बिक्री। माइनस (negative) नहीं। |
| **Planned Value (₹)** | रुपये में प्लान की गई बिक्री की कीमत। माइनस (negative) नहीं। |

## आम दिक्कतें

- **"Select a product and enter non-negative quantities and values for every row."** (हर लाइन में प्रोडक्ट चुनें और माइनस के बिना मात्रा और कीमत भरें) हर लाइन भरें, या खाली लाइनें डस्टबिन आइकन से हटा दें।
- **"The same product cannot appear more than once."** (एक ही प्रोडक्ट एक से ज़्यादा बार नहीं आ सकता) दोहराई गई लाइन हटा दें।
- **"Scheme end date must not precede the start date."** (स्कीम की आख़िरी तारीख शुरू की तारीख से पहले नहीं हो सकती) From Date वाले दिन या उसके बाद की To Date चुनें।
- **Requirement Status में Not Met दिख रहा है।** प्रोडक्ट जोड़ें या प्लान की गई कीमत बढ़ाएँ, जब तक कुल रकम Required Turnover तक न पहुँच जाए।
:::

::: section plan-review
title: प्लान जाँचें और कन्फ़र्म करें
routes: /PlanReview
for: 
roles: 
designations: 
tour: 1 > [data-help=st-review-summary] | 2 > [data-help=st-review-budget] | 3 > [data-help=st-review-plan] | 4 > [data-help=st-review-status] | 5 > [data-help=st-review-actions]
minutes: 3
source-hash: ee688fac1f288bce
confidence: ok

**यह स्क्रीन किस काम की है**
**Review & Confirm Plan** चौथा स्टेप है। प्रोग्राम, बजट, प्लान और डीलर जाँचें, फिर प्लान कन्फ़र्म करें। कन्फ़र्म सिर्फ़ एक बार होता है; उसके बाद सेल्स ट्रैकिंग शुरू हो जाती है।

1. ऊपर की पट्टी (**Program Type**, **Program Name**, **Scheme Period**, **Scheme Duration**) और उसके नीचे **Program Information** देखें।
2. **Budget Details** देखें: **CSR1 Approved Budget**, **Additional Approved Budget**, **Total Approved Budget** और **Required Turnover**।
3. **Product - wise Sales Plan & Summary** देखें। प्लान बदलना हो तो **Edit Sales Plan** दबाएँ।
4. **Sales Achievement & Dealer Allocation** में **Requirement Status** पर **Met** दिखना चाहिए और **Mapped Dealers** 0 से ज़्यादा होने चाहिए। डीलर बदलने हों तो **Edit Dealer Mapping** दबाएँ।
5. **Confirm Sales Plan** दबाएँ, फिर **Yes, Confirm & Save** दबाएँ। **Sales Plan Confirmed** बॉक्स में **View Tracking →** या **Back to Sales Audit** दबाएँ।

> Careful: कन्फ़र्म करने के बाद आप प्लान, स्कीम की तारीखें या डीलर नहीं बदल सकते।

## आम दिक्कतें

- **Confirm Sales Plan ग्रे है।** स्कीम का समय सेव करें, पक्का करें कि **Requirement Status** पर **Met** दिख रहा है, और कम से कम एक डीलर जोड़ें।
- **"Missing csr2Id. Open Plan Review from Dealer Mapping."** (csr2Id नहीं है। Dealer Mapping से Plan Review खोलें) **Sales Audit** लिस्ट से प्रोग्राम फिर से खोलें।
- **मैं अभी रुककर बाद में पूरा करना चाहता हूँ।** **Save Draft** दबाएँ। आपका प्लान और डीलर जैसे हैं वैसे ही रहेंगे।
:::

::: section daily-sales
title: रोज़ की बिक्री ऑडिट में सेव करें
routes: /PlanReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 3c54cfd763cbb61e
confidence: ok

**यह स्क्रीन किस काम की है**
प्लान कन्फ़र्म होने के बाद, नई SAP बिक्री को अचीवमेंट में जोड़ने के लिए **Review & Confirm Plan** पर वापस आएँ। हर बार नई बिक्री अपलोड होने पर ऐसा करें।

1. पक्का करें कि नई SAP डीलर बिक्री **Dealer Sales List** पर अपलोड हो चुकी है।
2. प्रोग्राम के लिए **Review & Confirm Plan** खोलें। **Sales Tracking Dashboard** से आप **Back to Plan Review** दबा सकते हैं।
3. **Matched Dealer Invoice Details** में **Refresh Sales** दबाएँ। हर इनवॉइस लाइन पर **New** (अभी सेव नहीं) या **Old** (पहले से सेव) दिखता है।
4. नीचे **Save Daily Sales** (इस पर दिखता है कि कितनी New हैं) या ऊपर **Save Sales Progress** दबाएँ, फिर **Yes, Save Daily Sales** दबाएँ।
5. **Daily Sales Saved Successfully** बॉक्स में सेव हुए इनवॉइस और **Cumulative Achievement** दिखते हैं। **Continue** या **Back to Sales Audit** दबाएँ।
6. जब सेव की गई बिक्री Required Turnover तक पहुँच जाती है, तो बटन पर **Audit Completed** दिखता है और ऑडिट बंद हो जाता है।

> Tip: Old इनवॉइस लाइनें कभी दो बार नहीं गिनी जातीं, इसलिए फिर से सेव करना सुरक्षित है।

## आम दिक्कतें

- **Save Daily Sales ग्रे है या उस पर 0 New दिख रहा है।** कोई नई इनवॉइस लाइन नहीं है। अगली SAP बिक्री अपलोड करें और **Refresh Sales** दबाएँ।
- **"SAP products excluded because no unambiguous planned-product name matched".** (प्लान के प्रोडक्ट का नाम साफ़ नहीं मिला, इसलिए SAP प्रोडक्ट छोड़ दिए गए) SAP के प्रोडक्ट का नाम आपके प्लान के किसी प्रोडक्ट से मिलना चाहिए।
- **कुछ इनवॉइस गिने नहीं जा रहे।** स्कीम के समय से बाहर की लाइनें, या MT के अलावा किसी और यूनिट वाली लाइनें छोड़ दी जाती हैं।
:::

::: section dealer-sales-upload
title: SAP डीलर बिक्री अपलोड करें
routes: /DealerSalesUpload
for: 
roles: 
designations: 
tour: 1 > [data-help=st-upload-btn] | 2 > [data-help=st-upload-btn] | 3 > [data-help=st-upload-search] | 4 > [data-help=st-upload-table] | 5 > [data-help=st-upload-export] | 6 > [data-help=st-upload-footer]
minutes: 3
source-hash: 7a499220079c04f2
confidence: ok

**यह स्क्रीन किस काम की है**
**Dealer Sales List** में SAP से आई डीलर इनवॉइस लाइनें रहती हैं। SAP की Excel फ़ाइल यहाँ अपलोड करें; सेल्स ऑडिट अचीवमेंट गिनने के लिए इन्हीं लाइनों का इस्तेमाल करते हैं।

1. **Bulk Upload** दबाएँ और SAP की Excel फ़ाइल चुनें (.xlsx या .xls, 5 MB तक)।
2. मैसेज पढ़ें: **New records**, **Already imported** और **Repeated in Excel**। अगर **Upload errors** टेबल दिखे, तो Excel में वे लाइनें ठीक करें और फिर से अपलोड करें।
3. लाइनें ढूँढने के लिए सर्च बॉक्स में इनवॉइस, डीलर या प्रोडक्ट टाइप करें।
4. लाइनें देखें: **Billing Date**, **Billing Document**, **Dealer Code**, **Dealer Name**, **Product Name**, **Quantity**, **Unit** और **Gross Amount (₹)**।
5. लिस्ट बाहर निकालने के लिए **Copy**, **CSV**, **Excel**, **PDF** या **Print** इस्तेमाल करें।
6. नीचे **Items Per Page** चुनें, पेजों के बीच आगे-पीछे जाएँ, और कुल **Gross sales** देखें।

> Tip: वही फ़ाइल फिर से अपलोड करना सुरक्षित है। जो लाइनें पहले से इम्पोर्ट हैं, वे छोड़ दी जाती हैं।

## Excel के कॉलम

| फ़ील्ड | क्या भरें |
|---|---|
| **Billing Document**, **Item** | इनवॉइस नंबर और लाइन नंबर। ज़रूरी है। |
| **Billing Date** | इनवॉइस की तारीख। असली तारीख होनी चाहिए। |
| **Customer Number**, **Customer Name** | डीलर कोड और डीलर का नाम। ज़रूरी है। |
| **Product Code**, **Product Name** | ज़रूरी है। ऑडिट में गिनती के लिए नाम प्लान के प्रोडक्ट से मिलना चाहिए। |
| **Quantity**, **Unit of Measure**, **Gross Amount** | माइनस (negative) नहीं। ऑडिट में सिर्फ़ MT वाली लाइनें गिनी जाती हैं। |

## आम दिक्कतें

- **"Missing columns".** (कॉलम नहीं हैं) Excel की पहली लाइन में ऊपर दिए सारे कॉलम के नाम होने चाहिए।
- **"Excel validation failed; no rows saved."** (Excel की जाँच में गलती मिली; कोई लाइन सेव नहीं हुई) **Upload errors** टेबल पढ़ें (**Excel Row**, **Field**, **Reason**), वे लाइनें ठीक करें और फिर से अपलोड करें।
- **"Maximum 10,000 invoice rows allowed."** (ज़्यादा से ज़्यादा 10,000 इनवॉइस लाइनें चल सकती हैं) फ़ाइल को छोटी फ़ाइलों में बाँट दें।
:::

::: section sales-tracking
title: टारगेट के सामने बिक्री ट्रैक करें
routes: /SalesTracking
for: 
roles: 
designations: 
minutes: 3
source-hash: 409f69e798e447e9
confidence: ok

**यह स्क्रीन किस काम की है**
**Sales Tracking Dashboard** पर दिखता है कि एक प्रोग्राम का Required Turnover कितना पूरा हुआ। प्रोग्राम का ज़िम्मेदार प्लान कन्फ़र्म करने के बाद इसे खोलता है। मैनेजर इसे **Sales Tracking** लिस्ट से सिर्फ़ देखने के लिए खोलते हैं।

1. प्लान कन्फ़र्म करने के बाद **View Tracking →** से, या **Sales Tracking** लिस्ट पर **View** से इसे खोलें।
2. ऊपर की टाइलें पढ़ें: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %**, **Remaining to Achieve** और **Tracking Status**।
3. अगर **Extension Request** बॉक्स दिखे, तो उसका स्टेटस पढ़ें। कारण और रिमार्क देखने के लिए **View Request Status** दबाएँ।
4. **Turnover Achievement Overview** और **Monthly Progress Trend** देखें। स्कीम 7 दिन के अंदर खत्म होने वाली हो तो चेतावनी दिखती है।
5. प्रोडक्ट टेबल और डीलर टेबल देखें। **Dealer Sales Achievement - SAP Invoice Matches** में इनवॉइस लाइनें दिखाने या छिपाने के लिए **View Details** या **Hide Details** दबाएँ।
6. प्रोग्राम का ज़िम्मेदार **Import actual sales achievements (SAP invoice report)** में SAP इनवॉइस रिपोर्ट अपलोड कर सकता है। नया अपलोड पुराने अपलोड की जगह ले लेता है।
7. Excel कॉपी के लिए **Download Tracking Report** दबाएँ, या **Period-End Review** खोलने के लिए **Go To Final Review** दबाएँ।

## आम दिक्कतें

- **"Awaiting Sales Data".** (बिक्री के डेटा का इंतज़ार है) अभी कोई अचीवमेंट सेव नहीं हुआ। **Review & Confirm Plan** पर रोज़ की बिक्री सेव करें।
- **इम्पोर्ट वाला बॉक्स नहीं दिख रहा।** सिर्फ़ देखने वाले (read-only) व्यू में, प्लान कन्फ़र्म होने से पहले, और ऑडिट फ़ाइनल सबमिट होने के बाद यह छिपा रहता है।
- **"Upload an XLSX or XLS file up to 5 MB."** (5 MB तक की XLSX या XLS फ़ाइल अपलोड करें) छोटी Excel फ़ाइल चुनें।
- **Go To Final Review ग्रे है।** अभी किसी प्रोडक्ट का अचीवमेंट सेव नहीं हुआ।
:::

::: section tracking-list
title: मेरे इलाके की सेल्स ट्रैकिंग देखें
routes: /SalesTrackingList
for: 
roles: 
designations: 
minutes: 2
source-hash: e2e94d6371a53595
confidence: ok

**यह स्क्रीन किस काम की है**
**Sales Tracking** मेन्यू से आपके इलाके के प्रोग्राम की सिर्फ़ देखने वाली लिस्ट खुलती है। RM को रीजन, SMM को HQ, SMD को राज्य दिखता है, और Admin को सब कुछ दिखता है।

1. **Filters** खोलकर **Sanction Number**, **Program Name**, **Location**, **Date Range** या **Status** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** से फ़िल्टर फिर से पहले जैसे हो जाते हैं।
2. प्रोग्राम ढूँढने के लिए सर्च बॉक्स में टाइप करें।
3. हर लाइन में **Scheme Period**, **Required TO (Auto)**, **Actual Turnover**, **Achievement** और **Document Status** देखें।
4. उस प्रोग्राम का **Sales Tracking Dashboard** खोलने के लिए **View** दबाएँ।
5. पेजों के बीच आगे-पीछे जाने के लिए **Items Per Page** और तीर इस्तेमाल करें।

## आम दिक्कतें

- **गलती दिखती है कि रोल को इजाज़त नहीं है या कोई इलाका नहीं दिया गया।** आपके लॉगिन को रीजन, HQ या राज्य के साथ RM, SMM या SMD रोल चाहिए। एडमिनिस्ट्रेटर से पूछें।
- **"From date cannot be after To date."** (From date, To date के बाद की नहीं हो सकती) तारीखों की रेंज ठीक करें।
- **"No accessible Sales Audit records found."** (देखने लायक कोई सेल्स ऑडिट नहीं मिला) **Clear All** दबाएँ। हो सकता है आपके इलाके में अभी कोई मंज़ूर प्रोग्राम न हो।
:::

::: section period-end-review
title: स्कीम खत्म होने पर नतीजा देखें
routes: /PeriodEndReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 93280350556420ae
confidence: ok

**यह स्क्रीन किस काम की है**
**Period-End Review** ट्रैकिंग का आख़िरी स्टेप है। इसमें दिखता है कि प्रोग्राम ने **Required Turnover** पूरा किया या नहीं, और अगर नहीं किया तो SMM और समय माँग सकता है।

1. **Sales Tracking Dashboard** पर **Go To Final Review** या **View Final Review** दबाएँ।
2. **Program Summary** और **Scheme period ends on** में लिखी तारीख देखें।
3. **Final Program Summary** में **Required Turnover (TO)**, **Planned Commitment**, **Actual Turnover Achieved**, **Balance to Target** और **Achievement** पढ़ें।
4. **Target Outcome & Next Step** में **Target Achieved** या **Target Not Achieved** दिखता है।
5. अगर टारगेट पूरा नहीं हुआ, तो SMM **Request Extension** दबाता है। इससे **Request Extension** स्क्रीन खुलती है, जहाँ कारण और नई आख़िरी तारीख देनी होती है।
6. बाहर निकलने के लिए **Back to Sales Tracking**, **Close** या **Back to List** दबाएँ।

> Tip: यह स्क्रीन सिर्फ़ जाँच के लिए है। इससे स्कीम बंद नहीं होती।

## आम दिक्कतें

- **Request Extension ग्रे है।** सिर्फ़ SMM ही एक्सटेंशन माँग सकता है।
- **Proceed to Final Review और Go To Final Review ग्रे हैं।** आख़िरी जाँच वाला स्टेप अभी उपलब्ध नहीं है।
- **"Missing CSR2 ID".** (CSR2 ID नहीं है) यह स्क्रीन **Sales Tracking** से खोलें।
:::

::: section audit-details
title: प्लान और अचीवमेंट की शीट भरें
routes: /SalesAuditDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: 609a8329530a3911
confidence: ok

**यह स्क्रीन किस काम की है**
**Sales Audit — Plan & Achievement** एक ही शीट है, जिसमें स्कीम की तारीखें, बजट, प्लान और असली बिक्री होती है। **Sales Audit** लिस्ट इसे **View Achievement** या **Request Extension** से खोलती है।

1. **Sales Audit** पर **Status** कॉलम में **View Achievement** या **Request Extension** दबाएँ।
2. **CSR-2 Auto-Filled Program Information** देखें।
3. **Scheme Period & Budget** में **Scheme From**, **Scheme To** और **Additional Approved Budget (₹)** देखें। उनके नीचे **Required Turnover (1.5×)** और **Audit Status** दिखते हैं।
4. **P — Sales Plan / Committee (Product-wise Breakup)** में **+ Add Product** दबाएँ और प्रोडक्ट, मात्रा और कीमत भरें। **Remove** से लाइन मिट जाती है।
5. **SP — Sales / Achievement** में इसी तरह असली बिक्री जोड़ें। **Actual** और **Achievement** में कुल जोड़ दिखता है।
6. अपना काम रखने के लिए **Save Draft** दबाएँ, या ऑडिट बंद करने के लिए **Final Submit** दबाएँ।

> Careful: **Final Submit** के बाद शीट सिर्फ़ देखी जा सकती है, बदली नहीं जा सकती।
> Careful: **Additional Approved Budget** उसकी अलग मंज़ूरी जाँचने के बाद ही भरें।

## आम दिक्कतें

- **"Complete scheme dates and product plan meeting the required target."** (स्कीम की तारीखें और टारगेट पूरा करने वाला प्रोडक्ट प्लान पूरा भरें) Final Submit के लिए दोनों तारीखें, कम से कम एक प्लान की लाइन, और प्लान व असली बिक्री का कुल कम से कम Required Turnover के बराबर होना चाहिए।
- **"Invalid scheme period."** (स्कीम का समय गलत है) Scheme To, Scheme From से पहले की नहीं होनी चाहिए।
- **"Invalid CSR-2 ID".** (CSR-2 ID गलत है) यह स्क्रीन **Sales Audit** लिस्ट से खोलें।
:::

::: section extension-list
title: एक्सटेंशन की रिक्वेस्ट देखें
routes: /ExtensionRequests
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: b342a221494b9432
confidence: ok

**यह स्क्रीन किस काम की है**
**Extension Request List** में वे रिक्वेस्ट दिखती हैं जो प्रोग्राम का टारगेट पूरा न होने पर और समय माँगने के लिए डाली गईं। AVP और Admin इन्हें यहाँ जाँचते हैं।

1. कार्ड देखें: **Total Request**, **Pending Request**, **Rejected** और **Approved**।
2. **Filters** खोलकर **Sanction Number**, **Program Type**, **Location**, **Request On** या **Status** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
3. ID, सैंक्शन नंबर, प्रोग्राम, जगह या नाम से रिक्वेस्ट ढूँढने के लिए सर्च बॉक्स में टाइप करें।
4. हर लाइन में **Requested By**, **Scheme Period**, **Request New Date** और **Achievement** देखें।
5. रिक्वेस्ट खोलने के लिए **View Request** दबाएँ।

## आम दिक्कतें

- **"You do not have permission to view extension requests."** (आपको एक्सटेंशन की रिक्वेस्ट देखने की इजाज़त नहीं है) सिर्फ़ AVP और Admin यह लिस्ट खोल सकते हैं।
- **"No records found".** (कुछ नहीं मिला) Filters में **Clear All** दबाएँ।
:::

::: section extension-review
title: एक्सटेंशन मंज़ूर या रिजेक्ट करें
routes: /ExtensionRequestDetails
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: 38c09035527b95d4
confidence: ok

**यह स्क्रीन किस काम की है**
**Extension Request Details** में एक रिक्वेस्ट, प्रोग्राम का प्रदर्शन और और समय माँगने का कारण दिखता है। इसे यहीं मंज़ूर या रिजेक्ट करें।

1. **Extension Request List** पर **View Request** दबाएँ।
2. ऊपर का हिस्सा देखें: स्टेटस, **Request ID** और **Request On**।
3. **Scheme Performance** पढ़ें: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %** और **Remaining to Achieve**।
4. **Extension Request** पढ़ें: **Current Scheme Period**, **Request New End Date**, **Requested By**, कारण और रिमार्क।
5. **Dealer-wise Actual Sales** देखें।
6. **Pending** रिक्वेस्ट के लिए **Approve Extension** या **Reject Request** दबाएँ।
7. **Extension Approved Successfully** या **Extension Rejected Successfully** खुलता है। लिस्ट पर वापस जाने के लिए **View Request Details** दबाएँ।

> Careful: बटन तुरंत काम करते हैं। पक्का करने के लिए दूसरा बॉक्स नहीं आता। मंज़ूर करने पर स्कीम की आख़िरी तारीख बदल जाती है।

## आम दिक्कतें

- **Approve या Reject बटन नहीं हैं।** रिक्वेस्ट पहले ही **Approved** या **Rejected** हो चुकी है।
- **"This request is already approved or the scheme end date has changed. Reload to review it."** (यह रिक्वेस्ट पहले ही मंज़ूर हो चुकी है या स्कीम की आख़िरी तारीख बदल गई है। जाँचने के लिए पेज फिर से लोड करें) पेज फिर से लोड करें और दोबारा देखें।
- **"Request not found".** (रिक्वेस्ट नहीं मिली) लिस्ट से रिक्वेस्ट फिर से खोलें।
:::
