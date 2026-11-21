---
module: budget-csr
title: बजट और CSR
icon: bi-wallet2
order: 110
language: hi
category: मार्केटिंग
note: नई गाइड
meta-source-hash: ed22bedc41464619
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 6fab7f9ab7aa88c7
confidence: ok

साल का मार्केटिंग बजट तय करें, उसे राज्यों, रीजन और हेडक्वार्टर (HQ) में बाँटें, और प्रोग्रामों के ज़रिए खर्च करें। हर प्रोग्राम के दो फ़ॉर्म होते हैं: प्रोग्राम से पहले CSR-1 (प्लान) और प्रोग्राम के बाद CSR-2 (रिपोर्ट)। हर फ़ॉर्म जाँचा जाता है और मंज़ूर किया जाता है।

- plan | icon: bi-pie-chart | color: #0891b2
  title: बजट तय करें
  text: हेड ऑफ़िस साल का बजट तय करता है। यह राज्यों, रीजन और HQ में बाँटा जाता है, फिर महीने के हिसाब से प्रोग्रामों में बाँटा जाता है।
- csr1 | icon: bi-file-earmark-text | color: #d97706
  title: CSR-1 यानी प्लान
  text: प्रोग्राम से पहले तारीख, जगह, मुख्य फ़सलें और अनुमानित खर्च भरकर CSR-1 डालें।
- csr2 | icon: bi-camera | color: #16a34a
  title: CSR-2 यानी रिपोर्ट
  text: प्रोग्राम के बाद फ़ोटो, आए हुए लोग, असली खर्च और सबूत के साथ CSR-2 रिपोर्ट दें।
:::

::: section flow
title: बजट और प्रोग्राम कैसे आगे बढ़ते हैं, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: a83847a70449e86b
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- office | type: lane | color: #0891b2 | roles: Admin, CorporateAdmin
  title: हेड ऑफ़िस
  sub: Admin
- field | type: lane | color: #d97706 | roles: MDO, JMDO, MO, RMD, SMD
  title: फ़ील्ड अधिकारी
  sub: MDO · MO · RMDO · SMDO
- approvers | type: lane | color: #0d9488 | roles: RM, SMM
  title: मंज़ूरी देने वाले
  sub: RM · SMM
- md | type: lane | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: सीनियर मंज़ूरी देने वाले
  sub: MD Admin · MD HOD · AVP
- budget | lane: office | col: 1 | icon: bi-wallet2 | color: #0891b2 | next: plan | screen: state-allocation
  title: बजट तय करें और बाँटें
  status: Active
  text: Admin साल का बजट डालता है और उसे राज्यों में बाँटता है। SMD या SMM राज्य का बजट रीजन में बाँटता है, और RM या RMDO रीजन का बजट HQ में बाँटता है।
- plan | lane: field | col: 2 | icon: bi-calendar3 | color: #64748b | next: plan-ok | screen: program-budget
  title: प्रोग्राम का बजट प्लान करें
  status: Submitted
  text: MO, RMDO और SMDO अपना बजट प्रोग्रामों और महीनों में बाँटते हैं, फिर **Submit** दबाते हैं।
- plan-ok | lane: approvers | col: 3 | icon: bi-check2-square | color: #2563eb | next: csr1 | screen: program-budget
  title: प्लान वैलिडेट और मंज़ूर करें
  status: Approved
  text: RMDO या SMDO प्रोग्राम बजट को वैलिडेट करता है (जाँचता है)। फिर RM या SMM उसे मंज़ूर करता है।
- csr1 | lane: field | col: 4 | icon: bi-file-earmark-text | color: #d97706 | next: csr1-ok | screen: create-csr1
  title: CSR-1 डालें
  status: Submitted
  text: मंज़ूर हुए प्रोग्राम के लिए CSR-1 फ़ॉर्म में तारीख, HQ, जगह, ज़िम्मेदार व्यक्ति, मुख्य फ़सलें और खर्च भरें, फिर **Submit for Validation** दबाएँ।
- csr1-ok | lane: approvers | col: 5 | icon: bi-patch-check | color: #2563eb | next: csr2 | screen: csr1-list
  title: CSR-1 वैलिडेट और मंज़ूर करें
  status: Approved
  text: HQ के प्लान MO वैलिडेट करता है और राज्य के प्लान SMD वैलिडेट करता है। फिर RM या SMM मंज़ूर करता है। CSR-1 को सैंक्शन नंबर मिलता है और वह CSR-2 के लायक हो जाता है।
- csr2 | lane: field | col: 6 | icon: bi-camera | color: #16a34a | next: csr2-ok | screen: csr2-list
  title: प्रोग्राम करें और CSR-2 रिपोर्ट दें
  status: Submitted
  text: ज़िम्मेदार व्यक्ति प्रोग्राम करता है और तारीख, फ़ोटो, आए हुए लोग, असली खर्च और सबूत की रिपोर्ट देता है।
- csr2-ok | lane: approvers | col: 7 | icon: bi-check2-all | color: #16a34a | next: final | screen: csr2-list
  title: CSR-2 वैलिडेट और मंज़ूर करें
  status: Approved
  text: MO या SMD रिपोर्ट वैलिडेट करता है। फिर RM या SMM उसे मंज़ूर करता है।
- final | lane: office | col: 8 | icon: bi-clipboard-data | color: #0891b2 | screen: final-report
  title: फ़ाइनल रिपोर्ट
  text: हेड ऑफ़िस को हर प्रोग्राम उसके CSR-1 स्टेटस, CSR-2 स्टेटस, खर्च और बचे हुए बजट के साथ दिखता है।
- late | lane: md | col: 6 | icon: bi-clock-history | color: #dc2626 | kind: exception | from: csr1 | to: csr1-ok | screen: csr1-list
  title: देर से आए फ़ॉर्म को तीन मंज़ूरियाँ चाहिए
  status: Level1Approved
  text: कट-ऑफ़ दिन के बाद भेजे गए CSR-1 या CSR-2 को तीन मंज़ूरियाँ चाहिए: पहले RM या SMM, फिर MD Admin, फिर MD HOD।
- shortage | lane: md | col: 7 | icon: bi-arrow-left-right | color: #dc2626 | kind: exception | from: csr2-ok | to: csr2-ok | screen: reallocation
  title: बजट कम है
  status: PendingMDHOD
  text: अगर बजट कम होने की वजह से CSR-2 मंज़ूर नहीं हो पा रहा, तो RM, SMM या Admin बजट रीएलोकेशन (बजट एक जगह से दूसरी जगह ले जाना) की रिक्वेस्ट डालता है। MD HOD की आख़िरी मंज़ूरी के बाद मंज़ूरी फिर से की जाती है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8d3c0359eda444f2
confidence: ok

- active | color: #16a34a
  label: Active
  text: साल के बजट का यह रिकॉर्ड इस्तेमाल में है।
- inactive | color: #64748b
  label: Inactive
  text: साल के बजट का यह रिकॉर्ड बंद है। इसे फिर से चालू किया जा सकता है।
- draft | color: #64748b
  label: Draft
  text: सेव है पर भेजा नहीं गया। आप अभी भी इसे बदल सकते हैं।
- submitted | color: #d97706
  label: Submitted
  text: भेज दिया गया है और वैलिडेशन का इंतज़ार है।
- pending | color: #d97706
  label: Pending
  text: अगली जाँच का इंतज़ार है।
- validated | color: #2563eb
  label: Validated
  text: वैलिडेट करने वाले ने जाँच लिया है। मंज़ूरी का इंतज़ार है।
- level1 | color: #7c3aed
  label: Level1Approved
  text: देर से आए फ़ॉर्म को पहली मंज़ूरी मिल गई। MD Admin का इंतज़ार है।
- level2 | color: #7c3aed
  label: Level2Approved
  text: देर से आए फ़ॉर्म को दूसरी मंज़ूरी मिल गई। MD HOD का इंतज़ार है।
- approved | color: #16a34a
  label: Approved
  text: पूरी तरह मंज़ूर। मंज़ूर हुआ CSR-1 आगे CSR-2 के लिए जा सकता है।
- rejected | color: #dc2626
  label: Rejected
  text: रिजेक्ट हो गया। रिजेक्ट होने का कारण पढ़ें।
- pending-smm | color: #d97706
  label: PendingSMM
  text: बजट रीएलोकेशन SMM के पास बाकी है।
- pending-avp | color: #d97706
  label: PendingAVP
  text: बजट रीएलोकेशन AVP के पास बाकी है।
- pending-mdhod | color: #d97706
  label: PendingMDHOD
  text: बजट रीएलोकेशन आख़िरी मंज़ूरी देने वाले MD HOD के पास बाकी है।
- healthy | color: #16a34a
  label: Healthy
  text: HQ के पास काफ़ी बजट बचा है।
- near-limit | color: #d97706
  label: Near Limit
  text: HQ ने अपना ज़्यादातर बजट इस्तेमाल कर लिया है।
- exceeded | color: #dc2626
  label: Exceeded
  text: HQ ने बजट से ज़्यादा खर्च कर दिया है।
- ext-pending | color: #d97706
  label: Pending Approval
  text: समय बढ़ाने की रिक्वेस्ट पर फ़ैसले का इंतज़ार है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5e86c95e22cb73c4
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- admin | color: #0891b2 | roles: Admin, CorporateAdmin
  title: Admin
  - हर वित्त वर्ष के लिए साल का बजट डालना
  - साल का बजट राज्यों में बाँटना
  - प्रोग्राम की लिस्ट, सैंक्शन नंबर और RMD HQ मैपिंग संभालना
  - राज्यों के बीच बजट रीएलोकेशन की रिक्वेस्ट डालना
- smm | color: #0d9488 | roles: SMM
  title: SMM
  - राज्य का बजट रीजन में बाँटना
  - प्रोग्राम बजट, राज्य के CSR-1 और राज्य के CSR-2 मंज़ूर करना
  - रीजन के बीच बजट रीएलोकेशन की रिक्वेस्ट डालना
  - RM की रीएलोकेशन रिक्वेस्ट मंज़ूर करना
- smd | color: #ea580c | roles: SMD
  title: SMDO
  - प्रोग्राम बजट प्लान करना और उन्हें वैलिडेट करना
  - राज्य का बजट रीजन में बाँटना
  - राज्य के प्रोग्रामों के लिए CSR-1 डालना
  - राज्य के CSR-1 और CSR-2 वैलिडेट करना
- rm | color: #2563eb | roles: RM
  title: RM
  - रीजन का बजट HQ में बाँटना
  - प्रोग्राम बजट, HQ के CSR-1 और HQ के CSR-2 मंज़ूर करना
  - HQ के बीच बजट रीएलोकेशन की रिक्वेस्ट डालना
- rmd | color: #9333ea | roles: RMD
  title: RMDO
  - प्रोग्राम बजट प्लान करना और उन्हें वैलिडेट करना
  - रीजन का बजट HQ में बाँटना
  - HQ के प्रोग्रामों के लिए CSR-1 डालना
- mo | color: #d97706 | roles: MO
  title: MO
  - प्रोग्राम बजट प्लान करना
  - CSR-1 डालना
  - HQ के CSR-1 और CSR-2 वैलिडेट करना
- mdo | color: #65a30d | roles: MDO, JMDO
  title: MDO और JMDO
  - HQ के प्रोग्रामों के लिए CSR-1 डालना
  - जब आप ज़िम्मेदार व्यक्ति हों, तब प्रोग्राम करना और CSR-2 रिपोर्ट देना
- senior | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: MD Admin, MD HOD और AVP
  - देर से आए फ़ॉर्म को MD Admin और MD HOD दूसरी और तीसरी मंज़ूरी देते हैं
  - AVP और MD HOD बजट रीएलोकेशन मंज़ूर करते हैं
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 471a0e1b0196bf4a
confidence: ok

- csr2-grey | popular: true
  q: CSR-2 बटन ग्रे है
  a: CSR-2 तभी खुलता है जब CSR-1 **Approved** हो जाए। तब **CSR-2 Eligibility** कॉलम में **Eligible for CSR-2** दिखता है और बटन काम करता है।
- one-month | popular: true
  q: CSR-1 Create में सिर्फ़ एक महीने के प्लान दिख रहे हैं
  a: यह सामान्य है। नीले बॉक्स में लिखा होता है कि कौन सा महीना है, जैसे "Showing plans for December 2026 only"। CSR-1 के कट-ऑफ़ दिन तक आप अगले महीने का प्लान बनाते हैं; उसके बाद उसके भी अगले महीने का।
- must-equal
  q: Submit करने पर लिखा आता है कि कुल रकम दिए गए बजट के बराबर होनी चाहिए
  a: **Budgeting Management** पर आपको अपना पूरा बजट प्रोग्रामों में बाँटना होता है। मैसेज में दिखता है कि अभी कितना बाँटना बाकी है। कुल रकम बराबर होने तक प्रोग्रामों की गिनती बढ़ाएँ।
- no-money
  q: बजट कम होने की वजह से CSR-2 मंज़ूर नहीं हो रहा
  a: RM, SMM या Admin किसी दूसरे राज्य, रीजन या HQ से पैसा लाने के लिए **Budget Reallocations** पर रिक्वेस्ट डालता है। MD HOD के मंज़ूर करने के बाद फिर से मंज़ूरी करके देखें।
- three-levels
  q: मेरे CSR-1 या CSR-2 को तीन मंज़ूरियाँ क्यों चाहिए?
  a: यह कट-ऑफ़ दिन के बाद भेजा गया था। देर से आए फ़ॉर्म पहले RM या SMM, फिर MD Admin, फिर MD HOD के पास जाते हैं।
- save-state-first
  q: मुझे "Save this state's allocation above before distributing it to regions." (रीजन में बाँटने से पहले ऊपर राज्य का बँटवारा सेव करें) दिख रहा है
  a: उस राज्य को अभी तक बजट नहीं मिला है। Admin को पहले उस वित्त वर्ष के लिए राज्य का बँटवारा सेव करना होगा।
:::

::: section annual-budget
title: साल का बजट डालें
routes: /AnnualBudgeting
for: 
roles: 
designations: 
minutes: 2
source-hash: 97c15e0498b487cc
confidence: ok

**यह स्क्रीन किस काम की है**
Admin हर वित्त वर्ष का कुल बजट डालने के लिए **Annual Budgeting** स्क्रीन का इस्तेमाल करता है। बजट की बाकी सारी स्क्रीन इसी रकम से शुरू होती हैं।

1. **Add Annual Budgeting** में **Financial Year (FY)** चुनें।
2. **Amount** भरें।
3. स्विच को **Active** पर रहने दें, या रिकॉर्ड को **Inactive** करने के लिए उस पर टैप करें।
4. **Save** दबाएँ। फ़ॉर्म खाली करने के लिए **Cancel** दबाएँ।
5. **Existing Annual Budgeting Records** में FY से खोजें, या लिस्ट छाँटने के लिए **Active** या **Inactive** चुनें।
6. रिकॉर्ड बदलने के लिए पेंसिल पर टैप करें, फिर **Update** दबाएँ। रिकॉर्ड बंद करने के लिए तिरछी लाइन वाले गोल आइकन पर टैप करें, या उसे फिर से चालू करने के लिए तीर वाले आइकन पर टैप करें।

> Tip: हर वित्त वर्ष का बजट का एक ही रिकॉर्ड हो सकता है, चाहे वह चालू हो या बंद।

## आम दिक्कतें

- **"Please select a Financial Year".** (वित्त वर्ष चुनें) सेव करने से पहले साल चुनें।
- **"Amount must be greater than zero".** (रकम शून्य से ज़्यादा होनी चाहिए) 0 से ज़्यादा रकम भरें।
- **"An Annual Budgeting record for ... already exists".** (इस साल का रिकॉर्ड पहले से है) नया रिकॉर्ड जोड़ने की जगह पुराने रिकॉर्ड को बदलें।
- **Save बटन ग्रे है।** अभी तक कोई वित्त वर्ष नहीं बना है। पहले Financial Master में उसे जोड़ें।
:::

::: section state-allocation
title: बजट राज्यों, रीजन और HQ में बाँटें
routes: /StateBudgetManagement
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD
designations: 
tour: 1 > [data-help=bc-sb-tabs] | 2 > [data-help=bc-sb-select] | 3 > [data-help=bc-sb-cards] | 4 > [data-help=bc-sb-table] | 5 > [data-help=bc-sb-save]
minutes: 3
source-hash: 535c04c3b89862f4
confidence: ok

**यह स्क्रीन किस काम की है**
**State Budgeting Management** स्क्रीन पर साल का बजट एक बार में एक लेवल नीचे बाँटा जाता है। Admin इसे राज्यों में बाँटता है, SMD या SMM राज्य का बजट उसके रीजन में बाँटता है, और RM या RMDO रीजन का बजट उसके HQ में बाँटता है।

1. आपके रोल के हिसाब से आपका टैब खुलता है: **State Allocation** (Admin), **Region Allocation** (SMD, SMM) या **Headquarters Allocation** (RM, RMDO)।
2. **Financial Year** चुनें। रीजन वाले टैब पर **State** भी चुनें। HQ वाले टैब पर **State** और **Region** चुनें।
3. कार्ड देखें, जैसे **Annual Budget**, **Allocated Budget** और **Remaining Budget**, या **State Allocated Budget** और **State MDO Budget**।
4. टेबल में हर राज्य, रीजन या HQ की रकम टाइप करें। रीजन और HQ वाले टैब पर पहले ऊपर **State MDO Amount** या **Region MDO Amount** भरें।
5. सेव करें। राज्य वाले टैब पर **Save Draft** दबाएँ, फिर **Submit For Validation** दबाएँ। बाकी टैब पर **Save Region Allocation** या **Save Headquarters Allocation** दबाएँ।

> Careful: राज्य वाले टैब पर सेव करने से पहले साल का पूरा बजट बँट जाना चाहिए।

## आम दिक्कतें

- **"All budget must be allocated to states."** (पूरा बजट राज्यों में बँटना चाहिए) मैसेज में बची हुई रकम दिखती है। उसे राज्यों में बाँटें।
- **"Region allocation cannot exceed the remaining state budget"** या **"Headquarters allocation cannot exceed the remaining region budget".** (बँटवारा बचे हुए बजट से ज़्यादा नहीं हो सकता) रकम कम करें।
- **"Save this region's allocation above before distributing it to headquarters."** (HQ में बाँटने से पहले ऊपर रीजन का बँटवारा सेव करें) रीजन को अभी बजट नहीं मिला है। SMD या SMM को पहले रीजन का बँटवारा सेव करना होगा।
- **"You do not have access to any budget allocation level."** (आपको बजट बाँटने का कोई एक्सेस नहीं है) आपका रोल बजट नहीं बाँटता। अपने एडमिनिस्ट्रेटर से पूछें।
:::

::: section program-budget
title: प्रोग्राम बजट प्लान, वैलिडेट और मंज़ूर करें
routes: /BudgetingManagements
for: 
roles: 
designations: 
minutes: 4
source-hash: 9d6de3d0ef847b35
confidence: ok

**यह स्क्रीन किस काम की है**
**Budgeting Management** पर MO, RMDO और SMDO अपना बजट प्रोग्रामों और महीनों में बाँटते हैं। RMDO और SMDO सेव किए गए प्लान वैलिडेट करते हैं, और RM और SMM उन्हें मंज़ूर करते हैं।

1. **Program-wise Budget Planning** के ऊपर वित्त वर्ष चुनें।
2. कार्ड देखें: आपका बजट (जैसे **Available Budget**), **Allocated Budget**, **Remaining Budget** और **SMDO Budget**।
3. **Budget plan list** में **April** से **March** तक हर महीने कितने प्रोग्राम करेंगे, वह भरें। अगर प्रोग्राम में इजाज़त हो, तो आप उसकी **Budget Amount** बदल सकते हैं। हर लाइन का **Total Budget** अपने आप बदल जाता है।
4. **Submit** दबाएँ। पूरा बजट बँटा होना चाहिए।
5. वैलिडेट करने के लिए (RMDO या SMDO) **Validate Budget** दबाएँ, **Saved program budgets awaiting validation** में से एक प्लान चुनें, ज़रूरत हो तो गिनती ठीक करें और **Validate** दबाएँ। अपने प्लान पर वापस जाने के लिए **Create Budget** दबाएँ।
6. मंज़ूर करने के लिए (RM या SMM) **Validated program budgets awaiting approval** में से एक प्लान चुनें, ज़रूरत हो तो गिनती ठीक करें और **Approve** दबाएँ।
7. टेबल शेयर करने के लिए **Copy**, **CSV**, **Excel**, **PDF** या **Print** का इस्तेमाल करें।

## आम दिक्कतें

- **"Total allocated budget ... must equal allotted budget ..."** (कुल बाँटा गया बजट दिए गए बजट के बराबर होना चाहिए) कुछ बजट अभी बचा है। प्रोग्रामों की गिनती तब तक बढ़ाएँ जब तक कुछ न बचे।
- **"... exceeds allotted budget ..."** (दिए गए बजट से ज़्यादा) आपने ज़्यादा प्लान कर लिया है। गिनती कम करें।
- **"No eligible budgets found for your assigned scope and selected FY."** (आपके इलाके और चुने गए साल के लिए कोई बजट नहीं मिला) इस साल आपके पास कुछ भी बाकी नहीं है। साल देख लें।
- **"Only MO, RMDO and SMDO can save program budgets."** (सिर्फ़ MO, RMDO और SMDO प्रोग्राम बजट सेव कर सकते हैं) आपका रोल सिर्फ़ देख, वैलिडेट या मंज़ूर कर सकता है।
:::

::: section budget-submissions
title: मेरे भेजे गए बजट ट्रैक करें
routes: /BudgetSubmissions
for: 
roles: 
designations: 
minutes: 2
source-hash: 3e92ca02097adb1c
confidence: ok

**यह स्क्रीन किस काम की है**
**Budget Submissions** स्क्रीन पर आपके भेजे गए प्रोग्राम बजट की लिस्ट होती है और दिखता है कि हर एक अभी कहाँ है।

1. कार्ड देखें: **Program Budget**, **Approved**, **Pending validation**, **Rejected** और **Drafts**।
2. **Date Range**, **Program Type**, **Program Name** या **Status** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
3. प्रोग्राम जल्दी ढूँढने के लिए **Search Program...** में टाइप करें।
4. टेबल में **Program Budget**, **Submitted Date**, **Status** और **Validation Due** देखें।
5. पूरी जानकारी देखने के लिए आँख वाले आइकन पर टैप करें। वहाँ से आप **Print** या **Export PDF** कर सकते हैं।

## आम दिक्कतें

- **"No budget submission records found."** (कोई रिकॉर्ड नहीं मिला) आपने अभी तक कोई प्लान नहीं भेजा है, या फ़िल्टर बहुत कड़े हैं। **Clear All** दबाएँ।
:::

::: section hq-budget-overview
title: HQ का बजट इस्तेमाल देखें
routes: /BudgetOverview
for: 
roles: 
designations: 
minutes: 1
source-hash: 6911fbefa137aa04
confidence: ok

**यह स्क्रीन किस काम की है**
**HQ Budget Allocation** स्क्रीन पर दिखता है कि रीजन का बजट उसके HQ में कैसे बँटा है और हर HQ ने कितना इस्तेमाल किया है।

1. कार्ड देखें: **Total Regional Budget**, **Allocated to HQs**, **Remaining Budget** और **Utilizes Budget**।
2. **Budget plan list** में हर HQ का **Allocated**, **Utilized**, **Remaining** और **Utilization %** देखें, और उसके **Pending CSR-1** और **Pending CSR-2** भी।
3. **Status** में **Healthy**, **Near Limit** या **Exceeded** दिखता है।
4. HQ का बजट इस्तेमाल खोलने के लिए आँख वाले आइकन पर टैप करें।
5. **Budgeting Management** पर जाने के लिए **Allocate Budget** दबाएँ।

## आम दिक्कतें

- **आँकड़े नहीं बदलते।** यह स्क्रीन अभी बन रही है। असली आँकड़ों के लिए **Budgeting Management** और **State Budgeting Management** का इस्तेमाल करें।
:::

::: section reallocation
title: रीएलोकेशन से बजट दूसरी जगह ले जाएँ
routes: /BudgetReallocations
for: 
roles: 
designations: 
minutes: 3
source-hash: 61df1dfc270033ec
confidence: ok

**यह स्क्रीन किस काम की है**
जब बजट कम होने की वजह से CSR-2 मंज़ूर नहीं हो पाता, तब **Budget Reallocations** स्क्रीन एक बजट से उसी लेवल के दूसरे बजट में पैसा ले जाती है। RM पैसा HQ के बीच ले जाता है, SMM रीजन के बीच, और Admin राज्यों के बीच।

1. **New Reallocation Request** में **Initiated by** और लेवल वाला बैज देखें।
2. **Financial Year** चुनें। अगर पूछा जाए, तो **State** और **Region** चुनें।
3. **From** (जो बजट पैसा देगा) और **To** (जिस बजट को पैसा चाहिए) चुनें। **Source Available Budget** और **Selected destination** देखें।
4. **Reallocation Amount (₹)** भरें और **Reason for Reallocation** लिखें।
5. **Approval route** देखें, जैसे RM, SMM, AVP, MD HOD। **Apply Reallocation** दबाएँ।
6. मंज़ूरी देने वाले: **Reallocation Requests** में **Approval / Rejection Remarks** लिखें, फिर उस लाइन पर **Approve** या **Reject** दबाएँ।
7. नया स्टेटस देखने के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **"Source budget shortage".** (देने वाले बजट में पैसा कम है) रकम देने वाले बजट में बची रकम से ज़्यादा है। उसे कम करें या कोई दूसरा बजट चुनें।
- **"No eligible budgets found."** (कोई बजट नहीं मिला) साल देखें, और देखें कि आपके राज्य या रीजन का बँटवारा मंज़ूर हो चुका है।
- **"Apply permissions are unavailable for this login."** (इस लॉगिन को रिक्वेस्ट डालने की इजाज़त नहीं है) सिर्फ़ RM, SMM और Admin रीएलोकेशन की रिक्वेस्ट डाल सकते हैं।
:::

::: section program-master
title: प्रोग्राम की लिस्ट संभालें
routes: /ProgramMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 800f0d1d4704db06
confidence: ok

**यह स्क्रीन किस काम की है**
**Program Master** स्क्रीन पर प्रोग्राम के टाइप और प्रोग्रामों की लिस्ट रखी जाती है, कौन उन्हें प्लान कर सकता है, और हर राज्य में प्रोग्राम की रकम कितनी है।

1. **Program Types** खोलें। **Program Type Name** भरें, **Is Amount Change** को **Allowed** या **Not Allowed** करें, और **Save** दबाएँ।
2. **Program Master** खोलें। **Program Type** चुनें, **Program Name** भरें, और **MO**, **RMDO** और **SMDO** को **Eligible** या **Not Eligible** करें। **Save** दबाएँ।
3. किसी लाइन को बदलने के लिए उस पर पेंसिल टैप करें।
4. **State Mapping** खोलें (या प्रोग्राम पर मैप पिन टैप करें)। **Program** चुनें।
5. हर राज्य के लिए **Applicable** चालू या बंद करें। जिस राज्य पर लागू है, उसके लिए **Program Amount (₹)** भरें और उस लाइन पर **Save** दबाएँ।

> Tip: **Is Amount Change** से तय होता है कि प्लान बनाने वाले **Budgeting Management** पर प्रोग्राम की बजट रकम बदल सकते हैं या नहीं।

## आम दिक्कतें

- **"View only - you do not have update permission."** (सिर्फ़ देख सकते हैं, बदलने की इजाज़त नहीं है) अपने एडमिनिस्ट्रेटर से बदलने की इजाज़त माँगें।
:::

::: section rmd-hq-mapping
title: RMDO को HQ दें
routes: /RMDHQMapping
for: 
roles: 
designations: 
minutes: 2
source-hash: 1c5c04d12fa1b412
confidence: ok

**यह स्क्रीन किस काम की है**
**RMD Headquarters Mapping** स्क्रीन एक ही रिकॉर्ड में एक RMDO को एक या ज़्यादा HQ से जोड़ती है।

1. **Assign Headquarters** में **Employee (RMD)** चुनें।
2. **State** चुनें, फिर **Region**।
3. **Select Headquarters** पर टैप करें और HQ पर टिक लगाएँ, या **Select All** पर टैप करें। **Done** दबाएँ।
4. **Save Mapping** दबाएँ। **Clear** दबाने से फ़ॉर्म खाली हो जाता है।
5. **Existing RMD HQ Mappings** में बदलने के लिए पेंसिल पर टैप करें (फिर **Update Mapping**), या मिटाने के लिए क्रॉस पर टैप करें (फिर **Confirm**)।

## आम दिक्कतें

- **"No headquarters found."** (कोई HQ नहीं मिला) पहले रीजन चुनें, या उस रीजन में कोई HQ नहीं है।
:::

::: section sanction-sequence
title: सैंक्शन नंबर की शुरुआती वैल्यू तय करें
routes: /SanctionNumberSequence
for: 
roles: 
designations: 
minutes: 2
source-hash: 7e920cca79cfe7a9
confidence: ok

**यह स्क्रीन किस काम की है**
मंज़ूर हुए CSR-1 को सैंक्शन नंबर मिलता है। **Sanction Number Sequence** स्क्रीन पर हर राज्य या रीजन के लिए इन नंबरों की शुरुआती वैल्यू तय की जाती है।

1. **Sequence Scope** चुनें: **State (SMM)** या **Region (RM / RMM)**।
2. **State** चुनें, और रीजन वाले scope के लिए **Region** भी चुनें। **State Code** और **Region Code** मास्टर से अपने आप भर जाते हैं।
3. शुरुआती वैल्यू भरें।
4. **Save Sequence** दबाएँ। **Clear** दबाने से फ़ॉर्म खाली हो जाता है।
5. **Existing Sanction Number Sequences** लिस्ट देखें। राज्य, रीजन या कोड से खोजें।

> Careful: एक बार बनने के बाद sequence बदला नहीं जा सकता।

## आम दिक्कतें

- **"Code not configured in State master".** (State master में कोड नहीं डाला गया) पहले उस राज्य या रीजन का कोड उसके मास्टर में डालें।
:::

::: section create-csr1
title: प्रोग्राम के लिए CSR-1 डालें
routes: /CREATE-CSR-1Management
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c1-banner] | 2 > [data-help=bc-c1-search] | 3 > [data-help=bc-c1-plan] | 4 > [data-help=bc-c1-focus] | 5 > [data-help=bc-c1-expense] | 6 > [data-help=bc-c1-actions]
minutes: 5
source-hash: 640ab3b5f3b1dedd
confidence: ok

**यह स्क्रीन किस काम की है**
CSR-1 एक प्रोग्राम का प्लान है, जो प्रोग्राम से पहले बनाया जाता है। **CSR-1 Program - Wise Budget Allocation** स्क्रीन पर आपके मंज़ूर हुए प्रोग्राम बजट की लिस्ट होती है। उनमें से एक चुनें और CSR-1 फ़ॉर्म भरें।

1. नीला बॉक्स पढ़ें। उसमें लिखा होता है कि आप किस महीने का प्लान बना रहे हैं।
2. **Budget plan list** में प्रोग्राम ढूँढें (**Search Program...** या **Filters** का इस्तेमाल करें), **Remaining Programs** देखें, और **Apply CSR 1** दबाएँ।
3. **CSR-1 Plan Details** में प्रोग्राम देखें और **Budget**, **HQ**, **Location**, **Responsible Person** और **Date** भरें।
4. **Focus Details** में कम से कम एक **Focused Crop** और उसके लिए एक **Focused Product** चुनें। हर बॉक्स में आप तीन फ़सलें और तीन प्रोडक्ट तक जोड़ सकते हैं।
5. **Expense Breakdown** में हर खर्च की अनुमानित रकम भरें। **Total Expenses** बजट के अंदर ही रहना चाहिए।
6. बाद में पूरा करने के लिए **Save Draft** दबाएँ, या भेजने के लिए **Submit for Validation** दबाएँ।

> Tip: **Responsible Person** ही प्रोग्राम करेगा और CSR-2 रिपोर्ट देगा।

| फ़ील्ड | क्या भरें |
|---|---|
| **Budget** | इस प्रोग्राम का पैसा। ज़रूरी है। |
| **HQ** और **Location** | प्रोग्राम कहाँ होगा। ज़रूरी है। |
| **Responsible Person** | जो अधिकारी प्रोग्राम करेगा। ज़रूरी है। |
| **Date** | प्रोग्राम की तारीख, प्लान वाले महीने के अंदर। ज़रूरी है। |

## आम दिक्कतें

- **"Budget Exceeded"** या **"Total Expense Breakdown ... exceeds the allocated Budget".** (खर्च बजट से ज़्यादा है) खर्च की रकम कम करें।
- **"Please enter at least one expense amount."** (कम से कम एक खर्च की रकम भरें) कम से कम एक अनुमानित खर्च भरें।
- **"Please select at least one Product for Focus Crop 1."** (Focus Crop 1 के लिए कम से कम एक प्रोडक्ट चुनें) आपने जो भी फ़सल चुनी है, हर एक के लिए एक प्रोडक्ट चुनें।
- **"CSR-1 creation is not available for Admin role."** (Admin रोल CSR-1 नहीं बना सकता) Admin सिर्फ़ **CSR-1 Management** पर CSR-1 देख सकता है।
:::

::: section csr1-list
title: CSR-1 ट्रैक, वैलिडेट और मंज़ूर करें
routes: /CSR-1List
for: 
roles: 
designations: 
minutes: 3
source-hash: 33a5bb3a053b06a1
confidence: ok

**यह स्क्रीन किस काम की है**
**CSR-1 Management** में CSR-1 प्लान की लिस्ट होती है। फ़ील्ड अधिकारी अपने प्लान ट्रैक करते हैं। MO और SMD उन्हें वैलिडेट करते हैं, और RM और SMM उन्हें मंज़ूर करते हैं।

1. कार्ड देखें: **Total CSR -1**, **Approved**, **Pending**, **Rejected** और **Drafts**।
2. **Filters** में **From Date**, **To Date**, **Program Type**, **Program Name** या **Status** चुनें, फिर **Apply Filters** दबाएँ।
3. **CSR-1 plan list** में **Status**, **Sanction Number** और **CSR-2 Eligibility** देखें। **State** या **HQ** टैग से प्लान का लेवल पता चलता है।
4. **CSR-1 Program Details** खोलने के लिए आँख वाले आइकन पर टैप करें।
5. अगर प्लान आपके पास बाकी है, तो **Validate** दबाएँ, या मंज़ूरी वाला बटन दबाएँ (**Approve**, या देर से आए प्लान के लिए लेवल का नाम)। रिजेक्ट करने के लिए **Reject** दबाएँ, रिमार्क लिखें और **Confirm Reject** दबाएँ।
6. प्लान **Approved** होने पर प्रोग्राम की रिपोर्ट देने के लिए लाइन पर **CSR-2** दबाएँ, या जानकारी वाले पेज में **Open CSR-2** दबाएँ।

> Careful: डस्टबिन वाला आइकन CSR-1 को तुरंत मिटा देता है, दोबारा नहीं पूछता।

## आम दिक्कतें

- **जानकारी में "Awaiting MO Validation" या "Awaiting SMD Validation" दिख रहा है।** किसी के मंज़ूर करने से पहले प्लान का वैलिडेट होना ज़रूरी है।
- **CSR-2 बटन ग्रे है।** CSR-1 अभी **Approved** नहीं हुआ है।
- **लाल "Rejection Reason" बॉक्स दिख रहा है।** लिस्ट के नीचे कारण पढ़ें।
:::

::: section csr2-list
title: CSR-2 रिपोर्ट दें, वैलिडेट और मंज़ूर करें
routes: /CSR-2List
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c2-cards] | 2 > [data-help=bc-c2-filters] | 3 > [data-help=bc-c2-table] | 4 > [data-help=bc-c2-table] | 5 > [data-help=bc-c2-table]
minutes: 3
source-hash: 3490f090fad6d820
confidence: ok

**यह स्क्रीन किस काम की है**
CSR-2 प्रोग्राम हो जाने के बाद की रिपोर्ट है। **CSR-2 Program Execution** लिस्ट में आपके प्रोग्राम हाज़िरी, फ़ोटो-वीडियो (media), खर्च और प्रगति के साथ दिखते हैं।

1. कार्ड देखें: **Total CSR-2**, **Approved**, **Submitted** और **Drafts**। सिर्फ़ वही प्रोग्राम देखने के लिए किसी कार्ड पर टैप करें।
2. **Filters** में तारीखें, **Program Type**, **Program Name**, **Location** या **Status** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
3. **Assigned Programs** में **Approved Budget**, **Attendance**, **Media**, **Actual Expense**, **Report Completion** और **Status** देखें।
4. CSR-2 फ़ॉर्म खोलने के लिए **View Details** दबाएँ। प्रोग्राम की तारीख, फ़ोटो, आए हुए लोग और हर असली खर्च उसके सबूत के साथ भरें, फिर भेज दें।
5. अगर कोई रिपोर्ट आपके पास बाकी है, तो **Validate** (MO या SMD) दबाएँ, या मंज़ूरी वाला बटन दबाएँ, जैसे **Approve CSR-2** या **Approve Level 1**।

> Tip: रिपोर्ट जैसे-जैसे **Draft** से **Approved** की तरफ़ बढ़ती है, **Report Completion** बढ़ता जाता है।

## आम दिक्कतें

- **"No CSR-2 records found."** (CSR-2 का कोई रिकॉर्ड नहीं मिला) आपको कोई मंज़ूर CSR-1 नहीं दिया गया है, या फ़िल्टर बहुत कड़े हैं।
- **"You are not authorized to validate this CSR-2."** (आपको यह CSR-2 वैलिडेट करने की इजाज़त नहीं है) इसे किसी दूसरे रोल को वैलिडेट करना है।
- **बजट की वजह से मंज़ूरी नहीं हो रही।** RM, SMM या Admin से बजट रीएलोकेशन के लिए कहें।
:::

::: section csr2-assigned
title: मुझे दिए गए प्रोग्राम खोलें
routes: /CSR-2ProgramExecution
for: 
roles: 
designations: 
minutes: 1
source-hash: 6d55229f3d01c986
confidence: ok

**यह स्क्रीन किस काम की है**
यह **CSR-2 Program Execution** स्क्रीन आपको दिए गए मंज़ूर CSR-1 प्रोग्रामों की लिस्ट दिखाती है, ताकि आप उनकी CSR-2 रिपोर्ट शुरू कर सकें।

1. ऊपर **Total CSR -2** देखें।
2. **Assigned Programs** में प्रोग्राम को उसके **Sanction Number**, **Program Type** या **Program** से ढूँढें।
3. **Approved Budget** और **Status** देखें।
4. उस प्रोग्राम का CSR-2 फ़ॉर्म खोलने के लिए **View Program** दबाएँ।
5. नई लिस्ट लाने के लिए **Refresh Data** दबाएँ।

## आम दिक्कतें

- **"No approved CSR-1 programs found."** (कोई मंज़ूर CSR-1 प्रोग्राम नहीं मिला) आपका कोई भी CSR-1 प्लान अभी मंज़ूर नहीं हुआ है।
:::

::: section csr2-form-preview
title: CSR-2 Management फ़ॉर्म
routes: /CSR2, /csrplandetails2
for: 
roles: 
designations: 
minutes: 1
source-hash: 7593c2c21789d74d
confidence: ok

**यह स्क्रीन किस काम की है**
**CSR-2 Management** स्क्रीन पर CSR-2 रिपोर्ट के हिस्से दिखते हैं: बजट कार्ड, प्लान की जानकारी, मुख्य फ़सलों की जानकारी और सबूत के साथ खर्च का ब्योरा। असली प्रोग्राम की रिपोर्ट देने के लिए **CSR-2 List** खोलें और **View Details** दबाएँ।

1. कार्ड देखें: **Approved Budget**, **Actual Expense** और **Difference**।
2. **CSR-2 Plan Details** देखें: प्रोग्राम, प्रोग्रामों की गिनती, बजट, HQ और जगह।
3. **Focus Details** देखें: मुख्य फ़सलें और प्रोडक्ट।
4. **Expense Breakdown** देखें: हर खर्च की एक रकम, **Date** और **Upload proof** होता है।

## आम दिक्कतें

- **यहाँ Save Draft या Submit CSR-2 दबाने से कुछ नहीं होता।** रिपोर्ट भेजने के लिए **CSR-2 List** और **View Details** का इस्तेमाल करें।
:::

::: section final-report
title: CSR की फ़ाइनल रिपोर्ट देखें
routes: /FinalReportCSRView
for: 
roles: 
designations: 
minutes: 1
source-hash: d8668b0d71867c79
confidence: ok

**यह स्क्रीन किस काम की है**
**Final Report - CSR Program Tracking** पर हर प्रोग्राम प्लान से लेकर रिपोर्ट तक दिखता है: CSR-1 स्टेटस, CSR-2 स्टेटस, सैंक्शन नंबर, खर्च और बचा हुआ बजट।

1. कार्ड देखें: **Total Programs**, **Approved**, **Pending**, **Rejected** और **Drafts**।
2. **Filters** से तारीखें, **Program Type**, **Program Name**, **CSR Type** या **Status** चुनें, फिर **Apply Filters** दबाएँ।
3. **CSR-2 plan list** में **Total Expenses**, **Approved Budget** और **Remaining Budget** की तुलना करें। **Report Status** में **Completed** या **Pending** दिखता है।
4. प्रोग्राम की जानकारी देखने के लिए आँख वाले आइकन पर टैप करें, फिर **Print** या **Export PDF** करें।

## आम दिक्कतें

- **आँकड़े नहीं बदलते।** यह स्क्रीन अभी बन रही है। असली रिकॉर्ड के लिए **CSR-1 Management** और **CSR-2 List** का इस्तेमाल करें।
:::

::: section rmd-queue
title: RMD वैलिडेशन की कतार
routes: /RMDValidationQueue
for: 
roles: 
designations: 
minutes: 2
source-hash: 641c748d235d2f14
confidence: ok

**यह स्क्रीन किस काम की है**
**RMD Validation Queue** में मार्केट ऑफ़िसर (MO) से आए CSR-1 और CSR-2 की लिस्ट होती है।

1. टैब चुनें: **CSR-1 Submissions** या **CSR-2 Reports**। बैज पर उनकी गिनती दिखती है।
2. **Filters** खोलकर **Date Range**, **Program Type**, **Head Quarters** या **Status** चुनें, फिर **Apply Filters** दबाएँ।
3. कोई रिकॉर्ड ढूँढने के लिए सर्च बॉक्स में टाइप करें।
4. **MO Name**, **Program Name**, **Head Quarters**, **Total Budget**, **Validation Due** और **Status** देखें।
5. नई लिस्ट लाने के लिए **Refresh Data** दबाएँ।

## आम दिक्कतें

- **"Failed to load data".** (डेटा लोड नहीं हुआ) अपना इंटरनेट देखें और **Try Again** दबाएँ।
- **"No submissions found".** (कोई रिकॉर्ड नहीं मिला) Filters में **Clear All** दबाएँ।
:::

::: section mo-validation
title: MO की एंट्री का वैलिडेशन
routes: /MOSubmissionValidation
for: 
roles: 
designations: 
minutes: 1
source-hash: fbb06076726e4306
confidence: ok

**यह स्क्रीन किस काम की है**
**MO Submission Validation** स्क्रीन पर मार्केट ऑफ़िसर से आए CSR-1 और CSR-2 की लिस्ट, प्लान किए गए और असली बजट के साथ दिखती है।

1. टैब चुनें: **CSR-1 Submissions** या **CSR-2 Reports**।
2. **Filters** से **Date Range**, **Program Type**, **Head Quatres** या **Status** चुनें।
3. **CSR-2 Expense Report Validation List** में **Planned Budget**, **Actual Budget** और **Variance** की तुलना करें।
4. **Validation Due** और **CSR - 1 Status** देखें।

## आम दिक्कतें

- **लिस्ट नहीं बदलती।** यह स्क्रीन अभी बन रही है। CSR-1 को **CSR-1 Management** पर और CSR-2 को **CSR-2 List** पर वैलिडेट करें।
:::

::: section rm-approval-status
title: RM मंज़ूरी का स्टेटस
routes: /RMApprovalStatus
for: 
roles: 
designations: 
minutes: 1
source-hash: dd292461c2960406
confidence: ok

**यह स्क्रीन किस काम की है**
**RM Approval Status** स्क्रीन पर वे CSR-1 और CSR-2 ट्रैक होते हैं जिन्हें RMD ने वैलिडेट करके RM को भेजा है।

1. टैब चुनें: **CSR-1 Submissions** या **CSR-2 Report**।
2. कार्ड देखें: **Sent to RM**, **Pending RM Approval**, **Approved by RM** और **Rejected / Correction**।
3. **CSR-1 RM Approval Status List** में **Mo Submitted**, **RMD Validated** और **Status** देखें।
4. प्रोग्राम की जानकारी और **Process Timeline** देखने के लिए आँख वाले आइकन पर टैप करें।

## आम दिक्कतें

- **लिस्ट नहीं बदलती।** यह स्क्रीन अभी बन रही है। CSR-1 को **CSR-1 Management** पर और CSR-2 को **CSR-2 List** पर मंज़ूर करें।
:::

::: section request-extension
title: स्कीम का समय बढ़ाने की रिक्वेस्ट डालें
routes: /RequestExtension/{CSRId:int}
for: 
roles: 
designations: 
minutes: 2
source-hash: c4d4cc4e073e1bda
confidence: ok

**यह स्क्रीन किस काम की है**
अगर सेल्स ऑडिट स्कीम का ज़रूरी टर्नओवर समय पर पूरा नहीं होता, तो **Request Extension** स्क्रीन से आगे की आख़िरी तारीख माँगी जाती है। इसे **Period-End Review** से खोलें।

1. **Scheme Information (Read Only)** देखें: **Sanction Number**, **Current Scheme Period**, **Required Turnover (TO)**, **Actual Turnover Achieved** और **Balance to Target**।
2. **Requested New End Date** चुनें। यह अभी की आख़िरी तारीख के बाद की होनी चाहिए।
3. **Reason for Extension** चुनें।
4. **Remarks / Explanation (Optional)** लिखें, 500 अक्षरों तक।
5. **Submit Extension Request** दबाएँ।
6. **Extension Request Submitted** बॉक्स में **Request ID** और **Pending Approval** दिखता है। **View My Request** या **Go to Sales Audit List** दबाएँ।

> Tip: जब तक रिक्वेस्ट मंज़ूर नहीं होती, स्कीम की आख़िरी तारीख नहीं बदलती।

## आम दिक्कतें

- **"Select the new end date."** या **"The new end date must be after ..."** (नई आख़िरी तारीख चुनें, जो पुरानी तारीख के बाद की हो) बाद की तारीख चुनें।
- **"A request is already awaiting approval for this CSR."** (इस CSR की एक रिक्वेस्ट पहले से मंज़ूरी के लिए बाकी है) पहली रिक्वेस्ट पर फ़ैसला होने तक रुकें।
:::

::: section welfare-application
title: वेलफ़ेयर आवेदन जाँचें
routes: /ApprovalDetail/{Id:int}, /MOApproval/{Id:int}
for: 
roles: 
designations: 
minutes: 3
source-hash: a63f984a39e5444b
confidence: ok

**यह स्क्रीन किस काम की है**
**Application Details** स्क्रीन पर डीलर का एक वेलफ़ेयर (SDWA) आवेदन दिखता है, ताकि मौजूदा लेवल का मंज़ूरी देने वाला उसे जाँचकर आगे की कार्रवाई कर सके। इसे **Scheme Approval** से खोलें।

1. ऊपर **Approval Workflow** देखें। इसमें MO से SDWA Admin तक हर लेवल दिखता है।
2. **Applicant Information**, **Nominee & Beneficiary Information** और **Eligibility Information** पढ़ें।
3. **Uploaded Documents** में हर फ़ाइल जाँचने के लिए **View** या **Download** दबाएँ।
4. पिछले लेवल के **Previous Remarks** पढ़ें।
5. अगर आपकी बारी है, तो **Comments** चुनें। **Recommendation** अपने आप भर जाता है। SDWA Admin **Cheque Number** और **Cheque Amount** भी भरता है।
6. **Submit** दबाएँ। लिस्ट पर वापस जाने के लिए **Back to Approvals** दबाएँ।

## आम दिक्कतें

- **Submit बटन नहीं है।** आवेदन अभी आपके लेवल पर नहीं है।
- **लाल बॉक्स में लिखा है कि आवेदन रिजेक्ट हो गया।** उसे सुधार के लिए डीलर को लौटा दिया गया है, या बंद कर दिया गया है।
:::
