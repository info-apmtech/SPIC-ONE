---
module: sales-tracking
title: सेल्स प्लॅन आणि ऑडिट
icon: bi-graph-up-arrow
order: 105
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
source-hash: 1976de717daddf89
confidence: ok

CSR-2 प्रोग्रामला अंतिम मंजुरी मिळाल्यावर त्याच्या विक्रीचा प्लॅन करावा लागतो आणि ती तपासावी लागते. प्रॉडक्टनुसार सेल्स प्लॅन बनवा, त्याची खात्री करा, SAP मधली डीलर विक्री त्या प्लॅनसमोर सेव्ह करा, आणि स्कीम संपल्यावर टार्गेट पूर्ण झाले का ते पाहा.

- plan | icon: bi-pencil-square | color: #2563eb
  title: प्लॅन बनवा
  text: स्कीमच्या तारखा आणि तुम्ही विकणार असलेले प्रॉडक्ट, संख्या आणि किंमत भरा.
- track | icon: bi-graph-up-arrow | color: #0d9488
  title: विक्री सेव्ह करा आणि तिचा मागोवा घ्या
  text: SAP मधले डीलर इनव्हॉइस अपलोड करा आणि रोज ते प्लॅनसमोर सेव्ह करा.
- review | icon: bi-flag | color: #d97706
  title: शेवटी तपासणी करा
  text: Required Turnover पूर्ण झाला का ते पाहा. झाला नसेल तर SMM जास्त वेळ मागू शकतात.
:::

::: section flow
title: सेल्स ऑडिट कसे पुढे जाते, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 63b369579a95c59f
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- owner | type: lane | color: #2563eb
  title: प्रोग्रामचे जबाबदार
  sub: CSR-1 वरची जबाबदार व्यक्ती (Responsible Person)
- data | type: lane | color: #64748b
  title: विक्रीचा डेटा
  sub: SAP इनव्हॉइसची Excel फाइल
- managers | type: lane | color: #0d9488 | roles: RM, SMM, SMD, Admin, SuperAdmin
  title: मॅनेजर
  sub: RM · SMM · SMD
- approvers | type: lane | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: मुदतवाढ मंजूर करणारे
  sub: AVP · Admin
- start | lane: owner | col: 1 | icon: bi-play-circle | color: #64748b | next: plan | screen: audit-overview
  title: ऑडिट सुरू करा
  status: Not Started
  text: **Sales Audit** वर **Start Audit** दाबा. प्रोग्राम आणि बजेट तपासा, मग **Continue to sales plan** दाबा.
- plan | lane: owner | col: 2 | icon: bi-pencil-square | color: #94a3b8 | next: dealers | screen: sales-plan
  title: सेल्स प्लॅन बनवा
  status: Draft
  text: स्कीमच्या तारखा आणि प्रॉडक्ट भरा. **Save Draft** किंवा **Save & Continue** दाबा.
- dealers | lane: owner | col: 3 | icon: bi-people | color: #0891b2 | next: confirm
  title: डीलर जोडा
  status: Draft
  text: **Dealers** टप्प्यावर ज्या डीलरची विक्री या प्रोग्राममध्ये मोजायची आहे ते डीलर निवडा.
- confirm | lane: owner | col: 4 | icon: bi-check2-square | color: #2563eb | next: upload | return: plan | screen: plan-review
  title: प्लॅनची खात्री करा
  status: Plan Confirmed
  text: **Review & Confirm Plan** वर सगळे तपासा आणि **Confirm Sales Plan** दाबा. त्यानंतर प्लॅन लॉक होतो. त्याआधी तुम्ही **Edit Sales Plan** ने मागे जाऊ शकता.
- upload | lane: data | col: 5 | icon: bi-cloud-arrow-up | color: #64748b | next: save | screen: dealer-sales-upload
  title: SAP विक्री अपलोड करा
  status: New
  text: SAP मधली डीलर इनव्हॉइसची Excel फाइल **Dealer Sales List** वर **Bulk Upload** ने अपलोड केली जाते.
- save | lane: owner | col: 6 | icon: bi-save | color: #d97706 | next: track | screen: daily-sales
  title: रोजची विक्री सेव्ह करा
  status: In Progress
  text: **Review & Confirm Plan** वर **Refresh Sales** दाबा आणि मग **Save Daily Sales** दाबा. नवीन इनव्हॉइस ओळी **Old** होतात आणि अचिव्हमेंटमध्ये जोडल्या जातात.
- track | lane: managers | col: 7 | icon: bi-graph-up-arrow | color: #0d9488 | next: review | screen: tracking-list
  title: प्रगती पाहा
  status: In Progress
  text: RM, SMM आणि SMD आपल्या भागातली विक्री **Sales Tracking** वर पाहतात.
- review | lane: owner | col: 8 | icon: bi-flag | color: #16a34a | screen: period-end-review
  title: स्कीम संपल्यावरची तपासणी
  status: Completed
  text: सेव्ह केलेली विक्री **Required Turnover** पर्यंत पोहोचली की ऑडिटवर **Completed** दिसते. **Period-End Review** वर अंतिम निकाल दिसतो.
- extension | lane: approvers | col: 8 | icon: bi-calendar-plus | color: #dc2626 | kind: exception | from: review | to: track | screen: extension-review
  title: जास्त वेळ मागा
  status: Pending
  text: टार्गेट पूर्ण झाले नसेल तर SMM **Request Extension** दाबतात. AVP किंवा Admin ती मंजूर करतात किंवा नाकारतात. मंजूर झाली तर स्कीमची शेवटची तारीख पुढे जाते.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 154f433d358852fc
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: प्रोग्राम मंजूर झाला आहे आणि वाट पाहत आहे. **Start Audit** दाबा.
- draft | color: #94a3b8
  label: Draft
  text: ऑडिट सुरू झाले आहे. प्लॅन सेव्ह केला आहे पण त्याची खात्री केलेली नाही.
- plan-confirmed | color: #2563eb
  label: Plan Confirmed
  text: प्लॅन लॉक झाला आहे. आता त्यासमोर विक्री सेव्ह करता येते.
- in-progress | color: #d97706
  label: In Progress
  text: विक्री सेव्ह होत आहे. टार्गेट अजून पूर्ण झालेले नाही.
- sales-tracking | color: #0d9488
  label: Sales Tracking
  text: स्कीमचा कालावधी चालू आहे.
- period-expired | color: #ea580c
  label: Period Expired
  text: टार्गेट पूर्ण होण्याआधीच स्कीमचा कालावधी संपला.
- extension-request | color: #7c3aed
  label: Extension Request
  text: जास्त वेळ मागितला आहे. निर्णयाची वाट पाहत आहे.
- completed | color: #16a34a
  label: Completed
  text: सेव्ह केलेली विक्री Required Turnover पर्यंत पोहोचली. ऑडिट बंद झाले आहे.
- ext-pending | color: #d97706
  label: Pending
  text: मुदतवाढीची विनंती AVP किंवा Admin कडे बाकी आहे.
- ext-pending-admin | color: #d97706
  label: Pending Admin Review
  text: तुमची मुदतवाढीची विनंती निर्णयाची वाट पाहत असताना ट्रॅकिंग स्क्रीनवर हे दिसते.
- ext-approved | color: #16a34a
  label: Approved
  text: मुदतवाढ मंजूर झाली आणि स्कीमची शेवटची तारीख बदलली.
- ext-rejected | color: #dc2626
  label: Rejected
  text: मुदतवाढ नाकारली. स्कीमची शेवटची तारीख बदलली नाही.
- invoice-new | color: #0891b2
  label: New
  text: SAP इनव्हॉइसची अशी ओळ जी अजून ऑडिटमध्ये सेव्ह केलेली नाही.
- invoice-old | color: #475569
  label: Old
  text: आधीच सेव्ह केलेली SAP इनव्हॉइसची ओळ. ती कधीही दोनदा मोजली जात नाही.
- met | color: #16a34a
  label: Met
  text: प्लॅन केलेली किंमत किमान Required Turnover इतकी आहे.
- not-met | color: #dc2626
  label: Not Met
  text: प्लॅन केलेली किंमत Required Turnover पेक्षा कमी आहे. तुम्ही अजून प्लॅनची खात्री करू शकत नाही.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: f676ec8395aa4ec8
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- owner | color: #2563eb
  title: प्रोग्रामचे जबाबदार
  - ऑडिट सुरू करा आणि सेल्स प्लॅन बनवा
  - डीलर जोडा आणि प्लॅनची खात्री करा
  - रोजची विक्री सेव्ह करा
- data | color: #64748b
  title: विक्रीचा डेटा अपलोड
  - SAP मधली डीलर विक्रीची Excel फाइल अपलोड करा
- managers | color: #0d9488 | roles: RM, SMM, SMD
  title: RM, SMM, SMD
  - तुमच्या भागातल्या विक्रीचे ट्रॅकिंग पाहा
  - SMM: टार्गेट पूर्ण झाले नसेल तर मुदतवाढ मागा
- approvers | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: AVP आणि Admin
  - मुदतवाढीच्या विनंत्या मंजूर करा किंवा नाकारा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e0470c8f452a2a10
confidence: ok

- no-programme | popular: true
  q: माझा प्रोग्राम Sales Audit लिस्टमध्ये दिसत नाही
  a: लिस्टमध्ये फक्त अंतिम मंजुरी मिळालेले असे CSR-2 प्रोग्राम दिसतात ज्यांच्या CSR-1 वर तुम्ही जबाबदार व्यक्ती (Responsible Person) आहात. CSR-2 ची स्थिती आणि जबाबदार व्यक्ती तपासा.
- confirm-grey | popular: true
  q: Confirm Sales Plan बटण राखाडी आहे
  a: स्कीमचा कालावधी, किमान एक प्रॉडक्ट, **Met** दाखवणारे **Requirement Status**, आणि किमान एक जोडलेला डीलर लागतो. जे कमी आहे ते **Edit Sales Plan** किंवा **Edit Dealer Mapping** ने दुरुस्त करा.
- zero-new
  q: Save Daily Sales वर 0 New दिसते
  a: तुमचे डीलर, प्रॉडक्ट आणि स्कीमच्या तारखांशी जुळणारी एकही नवीन SAP इनव्हॉइस ओळ नाही. **Dealer Sales List** वर नवीन SAP विक्री अपलोड करा, मग **Refresh Sales** दाबा.
- missing-id
  q: मला "Missing csr2Id" दिसते
  a: या स्क्रीन **Sales Audit** किंवा **Sales Tracking** लिस्टमधूनच उघडाव्या लागतात, सेव्ह केलेल्या लिंकमधून नाही. लिस्टवर परत जा आणि प्रोग्राम परत उघडा.
- extension-grey
  q: Request Extension बटण राखाडी आहे
  a: फक्त SMM च **Period-End Review** वरून मुदतवाढ मागू शकतात.
- upload-columns
  q: माझी Excel अपलोड "Missing columns" दाखवून फेल होते
  a: Excel च्या पहिल्या ओळीत ही नावे असली पाहिजेत: Billing Document, Billing Date, Item, Customer Number, Customer Name, Product Code, Product Name, Quantity, Unit of Measure, Gross Amount.
:::

::: section audit-list
title: माझे सेल्स ऑडिट पाहा
routes: /SalesAudit
for: 
roles: 
designations: 
minutes: 2
source-hash: dc0f5ec250d5599d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sales Audit** स्क्रीनवर तुम्ही जबाबदार असलेले मंजूर CSR-2 प्रोग्राम दिसतात, त्यांचा स्कीम कालावधी, टर्नओव्हर आणि स्थितीसह. प्रत्येक ऑडिट इथूनच सुरू करा किंवा पुढे चालू ठेवा.

1. **Filters** उघडा आणि **Sanction Number**, **Program Name**, **Location**, **Date Range** किंवा **Status** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
2. वरची कार्डे पाहा, जसे **Total Programs**, **Not started**, **In Progress** आणि **Completed**. कोणत्या टप्प्यावर किती प्रोग्राम आहेत ते यात दिसते.
3. **Sales Audit List** मध्ये प्रोग्राम शोधण्यासाठी सर्च बॉक्समध्ये टाइप करा.
4. प्रत्येक ओळीत **Scheme Period**, **Required TO (Auto)**, **Actual Turnover** आणि **Achievement** तपासा.
5. **Status** कॉलममधले बटण दाबा: नवीन प्रोग्रामसाठी **Start Audit**, तुम्ही सुरू केलेल्या प्रोग्रामसाठी **View / Continue**, किंवा **View Achievement**, **Request Extension**, **View Request** किंवा **View Audit**.

## नेहमीच्या अडचणी

- **"No eligible sales audit records found".** (योग्य सेल्स ऑडिट सापडले नाहीत) इथे फक्त अंतिम मंजुरी मिळालेले असे CSR-2 प्रोग्राम दिसतात ज्यांचे तुम्ही जबाबदार व्यक्ती (Responsible Person) आहात.
- **तारखेच्या फिल्टरने काहीच होत नाही.** तारखा निवडा, तारखेच्या बॉक्समध्ये **Done** दाबा, मग **Apply Filters** दाबा.
- **Scheme Period मध्ये "Not Set" दिसते.** सेल्स प्लॅन अजून बनवलेला नाही. **Start Audit** किंवा **View / Continue** दाबा.
:::

::: section audit-overview
title: प्रोग्राम आणि बजेट तपासा
routes: /AuditOverview
for: 
roles: 
designations: 
minutes: 2
source-hash: c767964e677164ff
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Audit Overview** हा पहिला टप्पा (**Program**) आहे. यात मंजूर CSR-2 आणि त्याला जोडलेल्या CSR-1 मधली माहिती दिसते, आणि ते बजेट दिसते ज्यावरून **Required Turnover** ठरतो.

1. **Sales Audit** वर एखाद्या प्रोग्रामसाठी **Start Audit** किंवा **View / Continue** दाबा.
2. वरची पट्टी तपासा: **Program Type**, **Program Name**, **Sanction Number** आणि **CSR-2 Status**.
3. **Program Information** तपासा: State, Region, HQ, CSR-2 कार्यक्रमाची तारीख आणि स्कीमच्या तारखा.
4. **Program Budget & Turnover** तपासा. **Required Turnover** हा **Total Approved Budget** वरून काढला जातो.
5. ऑडिट सुरू करण्यासाठी **Save Draft** दाबा, किंवा दुसऱ्या टप्प्यावर जाण्यासाठी **Continue to sales plan** दाबा.

> Tip: नियम बजेट कार्डवर दिलेला असतो: प्रत्येक ₹1.50 लाख बजेटसाठी ₹1.00 कोटी टर्नओव्हर.

## नेहमीच्या अडचणी

- **"Missing csr2Id".** (csr2Id नाही) ही स्क्रीन **Sales Audit** लिस्टमधून **Start Audit** ने उघडा.
- **"Access denied (403)".** (परवानगी नाही) तुम्ही या प्रोग्रामचे जबाबदार व्यक्ती (Responsible Person) नाही.
- **Save Draft राखाडी आहे.** ऑडिट आधीच अंतिम सबमिट झाले आहे.
:::

::: section sales-plan
title: सेल्स प्लॅन बनवा
routes: /SalesPlan
for: 
roles: 
designations: 
tour: 1 > [data-help=st-plan-period] | 2 > [data-help=st-plan-add] | 3 > [data-help=st-plan-table] | 4 > [data-help=st-plan-summary] | 5 > [data-help=st-plan-actions]
minutes: 4
source-hash: 2570b597b9143eea
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sales Plan & Scheme Period** हा दुसरा टप्पा आहे. स्कीमच्या तारखा आणि तुम्ही विकणार असलेले प्रॉडक्ट, संख्या आणि किंमत इथे भरा.

1. **Scheme Period** खाली **From Date** आणि **To Date** निवडा. **Duration** मध्ये महिने आणि दिवस दिसतात.
2. नवीन ओळ जोडण्यासाठी **+ Add Product** दाबा.
3. प्रत्येक ओळीत **Product Group** आणि **Product** निवडा, मग **Planned Quantity** आणि **Planned Value (₹)** टाका. डस्टबिनचा आयकॉन ओळ काढतो.
4. **Plan Summary & Comparison** तपासा. **Requirement Status** मध्ये **Met** दिसले पाहिजे: **Total Planned Commitment** किमान **Required Turnover** इतकी असली पाहिजे.
5. काम ठेवण्यासाठी **Save Draft** दाबा, किंवा सेव्ह करून **Dealers** टप्प्यावर जाण्यासाठी **Save & Continue** दाबा.

> Careful: प्लॅनची खात्री केल्यावर ही स्क्रीन लॉक होते. मग बटणावर **Continue to Dealer Mapping** दिसते.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **From Date**, **To Date** | स्कीमची सुरुवात आणि शेवट. शेवटची तारीख सुरुवातीच्या आधीची असू शकत नाही. |
| **Product Group** | हे आधी निवडा. मग **Product** लिस्टमध्ये त्यातले प्रॉडक्ट दिसतात. |
| **Product** | प्रत्येक प्रॉडक्ट फक्त एकदाच. |
| **Unit** | नेहमी MT. |
| **Planned Quantity** | MT मध्ये प्लॅन केलेली विक्री. उणे (negative) नको. |
| **Planned Value (₹)** | रुपयांमध्ये प्लॅन केलेल्या विक्रीची किंमत. उणे (negative) नको. |

## नेहमीच्या अडचणी

- **"Select a product and enter non-negative quantities and values for every row."** (प्रत्येक ओळीत प्रॉडक्ट निवडा आणि उणे नसलेली संख्या व किंमत टाका) प्रत्येक ओळ भरा, किंवा रिकाम्या ओळी डस्टबिनच्या आयकॉनने काढा.
- **"The same product cannot appear more than once."** (एकच प्रॉडक्ट एकापेक्षा जास्त वेळा असू शकत नाही) पुन्हा आलेली ओळ काढा.
- **"Scheme end date must not precede the start date."** (स्कीमची शेवटची तारीख सुरुवातीच्या तारखेआधी असू नये) From Date च्या दिवशी किंवा त्यानंतरची To Date निवडा.
- **Requirement Status मध्ये Not Met दिसते.** एकूण किंमत Required Turnover पर्यंत पोहोचेपर्यंत प्रॉडक्ट जोडा किंवा प्लॅन केलेली किंमत वाढवा.
:::

::: section plan-review
title: प्लॅन तपासा आणि त्याची खात्री करा
routes: /PlanReview
for: 
roles: 
designations: 
tour: 1 > [data-help=st-review-summary] | 2 > [data-help=st-review-budget] | 3 > [data-help=st-review-plan] | 4 > [data-help=st-review-status] | 5 > [data-help=st-review-actions]
minutes: 3
source-hash: ee688fac1f288bce
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Review & Confirm Plan** हा चौथा टप्पा आहे. प्रोग्राम, बजेट, प्लॅन आणि डीलर तपासा, मग प्लॅनची खात्री करा. खात्री फक्त एकदाच करता येते; त्यानंतर सेल्स ट्रॅकिंग सुरू होते.

1. वरची पट्टी (**Program Type**, **Program Name**, **Scheme Period**, **Scheme Duration**) आणि त्याखालची **Program Information** तपासा.
2. **Budget Details** तपासा: **CSR1 Approved Budget**, **Additional Approved Budget**, **Total Approved Budget** आणि **Required Turnover**.
3. **Product - wise Sales Plan & Summary** तपासा. प्लॅन बदलायचा असेल तर **Edit Sales Plan** दाबा.
4. **Sales Achievement & Dealer Allocation** मध्ये **Requirement Status** वर **Met** दिसले पाहिजे आणि **Mapped Dealers** 0 पेक्षा जास्त असले पाहिजेत. डीलर बदलायचे असतील तर **Edit Dealer Mapping** दाबा.
5. **Confirm Sales Plan** दाबा, मग **Yes, Confirm & Save** दाबा. **Sales Plan Confirmed** बॉक्समध्ये **View Tracking →** किंवा **Back to Sales Audit** दाबा.

> Careful: खात्री केल्यावर तुम्ही प्लॅन, स्कीमच्या तारखा किंवा डीलर बदलू शकत नाही.

## नेहमीच्या अडचणी

- **Confirm Sales Plan राखाडी आहे.** स्कीमचा कालावधी सेव्ह करा, **Requirement Status** वर **Met** दिसते याची खात्री करा, आणि किमान एक डीलर जोडा.
- **"Missing csr2Id. Open Plan Review from Dealer Mapping."** (csr2Id नाही. Dealer Mapping मधून Plan Review उघडा) **Sales Audit** लिस्टमधून प्रोग्राम परत उघडा.
- **मला आता थांबून नंतर पूर्ण करायचे आहे.** **Save Draft** दाबा. तुमचा प्लॅन आणि डीलर जसेच्या तसे राहतात.
:::

::: section daily-sales
title: रोजची विक्री ऑडिटमध्ये सेव्ह करा
routes: /PlanReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 3c54cfd763cbb61e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
प्लॅनची खात्री झाल्यावर नवीन SAP विक्री अचिव्हमेंटमध्ये जोडण्यासाठी **Review & Confirm Plan** वर परत या. प्रत्येक वेळी नवीन विक्री अपलोड झाली की हे करा.

1. नवीन SAP डीलर विक्री **Dealer Sales List** वर अपलोड झाली आहे याची खात्री करा.
2. प्रोग्रामसाठी **Review & Confirm Plan** उघडा. **Sales Tracking Dashboard** वरून तुम्ही **Back to Plan Review** दाबू शकता.
3. **Matched Dealer Invoice Details** मध्ये **Refresh Sales** दाबा. प्रत्येक इनव्हॉइस ओळीवर **New** (अजून सेव्ह नाही) किंवा **Old** (आधीच सेव्ह) दिसते.
4. खालचे **Save Daily Sales** (त्यावर किती New आहेत ते दिसते) किंवा वरचे **Save Sales Progress** दाबा, मग **Yes, Save Daily Sales** दाबा.
5. **Daily Sales Saved Successfully** बॉक्समध्ये सेव्ह झालेले इनव्हॉइस आणि **Cumulative Achievement** दिसते. **Continue** किंवा **Back to Sales Audit** दाबा.
6. सेव्ह केलेली विक्री Required Turnover पर्यंत पोहोचली की बटणावर **Audit Completed** दिसते आणि ऑडिट बंद होते.

> Tip: Old इनव्हॉइस ओळी कधीही दोनदा मोजल्या जात नाहीत, त्यामुळे परत सेव्ह करणे सुरक्षित आहे.

## नेहमीच्या अडचणी

- **Save Daily Sales राखाडी आहे किंवा त्यावर 0 New दिसते.** नवीन इनव्हॉइस ओळी नाहीत. पुढची SAP विक्री अपलोड करा आणि **Refresh Sales** दाबा.
- **"SAP products excluded because no unambiguous planned-product name matched".** (प्लॅनमधल्या प्रॉडक्टचे नाव नीट जुळले नाही म्हणून SAP प्रॉडक्ट वगळले) SAP मधल्या प्रॉडक्टचे नाव तुमच्या प्लॅनमधल्या प्रॉडक्टशी जुळले पाहिजे.
- **काही इनव्हॉइस मोजले जात नाहीत.** स्कीमच्या कालावधीबाहेरच्या ओळी, किंवा MT सोडून दुसरे युनिट असलेल्या ओळी वगळल्या जातात.
:::

::: section dealer-sales-upload
title: SAP डीलर विक्री अपलोड करा
routes: /DealerSalesUpload
for: 
roles: 
designations: 
tour: 1 > [data-help=st-upload-btn] | 2 > [data-help=st-upload-btn] | 3 > [data-help=st-upload-search] | 4 > [data-help=st-upload-table] | 5 > [data-help=st-upload-export] | 6 > [data-help=st-upload-footer]
minutes: 3
source-hash: 7a499220079c04f2
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Dealer Sales List** मध्ये SAP मधल्या डीलर इनव्हॉइस ओळी असतात. SAP ची Excel फाइल इथे अपलोड करा; सेल्स ऑडिट अचिव्हमेंट मोजण्यासाठी याच ओळी वापरतात.

1. **Bulk Upload** दाबा आणि SAP ची Excel फाइल निवडा (.xlsx किंवा .xls, 5 MB पर्यंत).
2. मेसेज वाचा: **New records**, **Already imported** आणि **Repeated in Excel**. **Upload errors** टेबल दिसले तर Excel मध्ये त्या ओळी दुरुस्त करा आणि परत अपलोड करा.
3. ओळी शोधण्यासाठी सर्च बॉक्समध्ये इनव्हॉइस, डीलर किंवा प्रॉडक्ट टाइप करा.
4. ओळी तपासा: **Billing Date**, **Billing Document**, **Dealer Code**, **Dealer Name**, **Product Name**, **Quantity**, **Unit** आणि **Gross Amount (₹)**.
5. लिस्ट बाहेर काढण्यासाठी **Copy**, **CSV**, **Excel**, **PDF** किंवा **Print** वापरा.
6. खाली **Items Per Page** निवडा, पानांमध्ये पुढे-मागे जा, आणि एकूण **Gross sales** पाहा.

> Tip: तीच फाइल परत अपलोड करणे सुरक्षित आहे. आधीच इम्पोर्ट झालेल्या ओळी सोडून दिल्या जातात.

## Excel चे कॉलम

| फील्ड | काय भरायचे |
|---|---|
| **Billing Document**, **Item** | इनव्हॉइस नंबर आणि ओळ नंबर. भरणे आवश्यक. |
| **Billing Date** | इनव्हॉइसची तारीख. खरी तारीख असली पाहिजे. |
| **Customer Number**, **Customer Name** | डीलर कोड आणि डीलरचे नाव. भरणे आवश्यक. |
| **Product Code**, **Product Name** | भरणे आवश्यक. ऑडिटमध्ये मोजले जाण्यासाठी नाव प्लॅनमधल्या प्रॉडक्टशी जुळले पाहिजे. |
| **Quantity**, **Unit of Measure**, **Gross Amount** | उणे (negative) नको. ऑडिटमध्ये फक्त MT च्या ओळी मोजल्या जातात. |

## नेहमीच्या अडचणी

- **"Missing columns".** (कॉलम नाहीत) Excel च्या पहिल्या ओळीत वरच्या सगळ्या कॉलमची नावे असली पाहिजेत.
- **"Excel validation failed; no rows saved."** (Excel तपासणीत चूक आली; एकही ओळ सेव्ह झाली नाही) **Upload errors** टेबल वाचा (**Excel Row**, **Field**, **Reason**), त्या ओळी दुरुस्त करा आणि परत अपलोड करा.
- **"Maximum 10,000 invoice rows allowed."** (जास्तीत जास्त 10,000 इनव्हॉइस ओळी चालतात) फाइलचे छोटे भाग करा.
:::

::: section sales-tracking
title: टार्गेटसमोर विक्रीचा मागोवा घ्या
routes: /SalesTracking
for: 
roles: 
designations: 
minutes: 3
source-hash: 409f69e798e447e9
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sales Tracking Dashboard** वर एका प्रोग्रामचा Required Turnover किती पूर्ण झाला ते दिसते. प्रोग्रामचे जबाबदार प्लॅनची खात्री केल्यावर ही स्क्रीन उघडतात. मॅनेजर ती **Sales Tracking** लिस्टमधून फक्त पाहण्यासाठी उघडतात.

1. प्लॅनची खात्री केल्यावर **View Tracking →** ने, किंवा **Sales Tracking** लिस्टवर **View** ने ही स्क्रीन उघडा.
2. वरच्या टाइल वाचा: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %**, **Remaining to Achieve** आणि **Tracking Status**.
3. **Extension Request** बॉक्स दिसला तर त्याची स्थिती वाचा. कारण आणि शेरा पाहण्यासाठी **View Request Status** दाबा.
4. **Turnover Achievement Overview** आणि **Monthly Progress Trend** पाहा. स्कीम 7 दिवसांत संपणार असेल तर इशारा दिसतो.
5. प्रॉडक्टचे टेबल आणि डीलरची टेबल तपासा. **Dealer Sales Achievement - SAP Invoice Matches** मध्ये इनव्हॉइस ओळी दाखवण्यासाठी किंवा लपवण्यासाठी **View Details** किंवा **Hide Details** दाबा.
6. प्रोग्रामचे जबाबदार **Import actual sales achievements (SAP invoice report)** खाली SAP इनव्हॉइस रिपोर्ट अपलोड करू शकतात. नवीन अपलोड आधीच्या अपलोडची जागा घेतो.
7. Excel कॉपीसाठी **Download Tracking Report** दाबा, किंवा **Period-End Review** उघडण्यासाठी **Go To Final Review** दाबा.

## नेहमीच्या अडचणी

- **"Awaiting Sales Data".** (विक्रीच्या डेटाची वाट पाहत आहे) अजून एकही अचिव्हमेंट सेव्ह झालेली नाही. **Review & Confirm Plan** वर रोजची विक्री सेव्ह करा.
- **इम्पोर्टचा बॉक्स दिसत नाही.** फक्त पाहण्याच्या (read-only) व्ह्यूमध्ये, प्लॅनची खात्री होण्याआधी, आणि ऑडिट अंतिम सबमिट झाल्यावर तो लपवलेला असतो.
- **"Upload an XLSX or XLS file up to 5 MB."** (5 MB पर्यंतची XLSX किंवा XLS फाइल अपलोड करा) लहान Excel फाइल निवडा.
- **Go To Final Review राखाडी आहे.** अजून एकाही प्रॉडक्टची अचिव्हमेंट सेव्ह झालेली नाही.
:::

::: section tracking-list
title: माझ्या भागातले सेल्स ट्रॅकिंग पाहा
routes: /SalesTrackingList
for: 
roles: 
designations: 
minutes: 2
source-hash: e2e94d6371a53595
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sales Tracking** मेनूमधून तुमच्या भागातल्या प्रोग्रामची फक्त पाहण्यासाठीची लिस्ट उघडते. RM ला रिजन, SMM ला HQ, SMD ला राज्य दिसते, आणि Admin ला सगळे दिसते.

1. **Filters** उघडा आणि **Sanction Number**, **Program Name**, **Location**, **Date Range** किंवा **Status** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर परत पहिल्यासारखे होतात.
2. प्रोग्राम शोधण्यासाठी सर्च बॉक्समध्ये टाइप करा.
3. प्रत्येक ओळीत **Scheme Period**, **Required TO (Auto)**, **Actual Turnover**, **Achievement** आणि **Document Status** तपासा.
4. त्या प्रोग्रामचा **Sales Tracking Dashboard** उघडण्यासाठी **View** दाबा.
5. पानांमध्ये पुढे-मागे जाण्यासाठी **Items Per Page** आणि बाण वापरा.

## नेहमीच्या अडचणी

- **रोलला परवानगी नाही किंवा भाग दिलेला नाही अशी चूक दिसते.** तुमच्या लॉगिनला रिजन, HQ किंवा राज्यासह RM, SMM किंवा SMD रोल लागतो. ॲडमिनिस्ट्रेटरला विचारा.
- **"From date cannot be after To date."** (From date ही To date नंतरची असू शकत नाही) तारखांची रेंज दुरुस्त करा.
- **"No accessible Sales Audit records found."** (पाहता येतील असे सेल्स ऑडिट सापडले नाहीत) **Clear All** दाबा. कदाचित तुमच्या भागात अजून एकही मंजूर प्रोग्राम नसेल.
:::

::: section period-end-review
title: स्कीम संपल्यावर निकाल तपासा
routes: /PeriodEndReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 93280350556420ae
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Period-End Review** हा ट्रॅकिंगचा शेवटचा टप्पा आहे. प्रोग्रामने **Required Turnover** गाठला का ते यात दिसते, आणि गाठला नसेल तर SMM ला जास्त वेळ मागता येतो.

1. **Sales Tracking Dashboard** वर **Go To Final Review** किंवा **View Final Review** दाबा.
2. **Program Summary** आणि **Scheme period ends on** मधली तारीख तपासा.
3. **Final Program Summary** मध्ये **Required Turnover (TO)**, **Planned Commitment**, **Actual Turnover Achieved**, **Balance to Target** आणि **Achievement** वाचा.
4. **Target Outcome & Next Step** मध्ये **Target Achieved** किंवा **Target Not Achieved** दिसते.
5. टार्गेट पूर्ण झाले नसेल तर SMM **Request Extension** दाबतात. यामुळे **Request Extension** स्क्रीन उघडते, तिथे कारण आणि नवीन शेवटची तारीख द्यायची असते.
6. बाहेर पडण्यासाठी **Back to Sales Tracking**, **Close** किंवा **Back to List** दाबा.

> Tip: ही स्क्रीन फक्त तपासणीसाठी आहे. यामुळे स्कीम बंद होत नाही.

## नेहमीच्या अडचणी

- **Request Extension राखाडी आहे.** फक्त SMM च मुदतवाढ मागू शकतात.
- **Proceed to Final Review आणि Go To Final Review राखाडी आहेत.** अंतिम तपासणीचा टप्पा अजून उपलब्ध नाही.
- **"Missing CSR2 ID".** (CSR2 ID नाही) ही स्क्रीन **Sales Tracking** मधून उघडा.
:::

::: section audit-details
title: प्लॅन आणि अचिव्हमेंटची शीट भरा
routes: /SalesAuditDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: 609a8329530a3911
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sales Audit — Plan & Achievement** ही एकच शीट आहे, ज्यात स्कीमच्या तारखा, बजेट, प्लॅन आणि प्रत्यक्ष विक्री असते. **Sales Audit** लिस्ट ती **View Achievement** किंवा **Request Extension** ने उघडते.

1. **Sales Audit** वर **Status** कॉलममध्ये **View Achievement** किंवा **Request Extension** दाबा.
2. **CSR-2 Auto-Filled Program Information** तपासा.
3. **Scheme Period & Budget** खाली **Scheme From**, **Scheme To** आणि **Additional Approved Budget (₹)** तपासा. त्याखाली **Required Turnover (1.5×)** आणि **Audit Status** दिसतात.
4. **P — Sales Plan / Committee (Product-wise Breakup)** खाली **+ Add Product** दाबा आणि प्रॉडक्ट, संख्या आणि किंमत टाका. **Remove** ने ओळ डिलीट होते.
5. **SP — Sales / Achievement** खाली त्याच पद्धतीने प्रत्यक्ष विक्री जोडा. **Actual** आणि **Achievement** मध्ये बेरीज दिसते.
6. काम ठेवण्यासाठी **Save Draft** दाबा, किंवा ऑडिट बंद करण्यासाठी **Final Submit** दाबा.

> Careful: **Final Submit** नंतर शीट फक्त पाहता येते, बदलता येत नाही.
> Careful: **Additional Approved Budget** ची वेगळी मंजुरी तपासल्यावरच तो टाका.

## नेहमीच्या अडचणी

- **"Complete scheme dates and product plan meeting the required target."** (स्कीमच्या तारखा आणि टार्गेट पूर्ण करणारा प्रॉडक्ट प्लॅन पूर्ण भरा) Final Submit साठी दोन्ही तारखा, किमान एक प्लॅनची ओळ, आणि प्लॅन व प्रत्यक्ष विक्रीची बेरीज किमान Required Turnover इतकी लागते.
- **"Invalid scheme period."** (स्कीमचा कालावधी चुकीचा आहे) Scheme To ही Scheme From च्या आधीची असू नये.
- **"Invalid CSR-2 ID".** (CSR-2 ID चुकीचा आहे) ही स्क्रीन **Sales Audit** लिस्टमधून उघडा.
:::

::: section extension-list
title: मुदतवाढीच्या विनंत्या पाहा
routes: /ExtensionRequests
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: b342a221494b9432
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Extension Request List** मध्ये, प्रोग्रामने टार्गेट गाठले नाही म्हणून जास्त वेळ मागणाऱ्या विनंत्या दिसतात. AVP आणि Admin त्या इथे तपासतात.

1. कार्डे पाहा: **Total Request**, **Pending Request**, **Rejected** आणि **Approved**.
2. **Filters** उघडा आणि **Sanction Number**, **Program Type**, **Location**, **Request On** किंवा **Status** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
3. ID, सँक्शन नंबर, प्रोग्राम, ठिकाण किंवा नावाने विनंती शोधण्यासाठी सर्च बॉक्समध्ये टाइप करा.
4. प्रत्येक ओळीत **Requested By**, **Scheme Period**, **Request New Date** आणि **Achievement** तपासा.
5. विनंती उघडण्यासाठी **View Request** दाबा.

## नेहमीच्या अडचणी

- **"You do not have permission to view extension requests."** (तुम्हाला मुदतवाढीच्या विनंत्या पाहण्याची परवानगी नाही) फक्त AVP आणि Admin ही लिस्ट उघडू शकतात.
- **"No records found".** (काहीही सापडले नाही) Filters मध्ये **Clear All** दाबा.
:::

::: section extension-review
title: मुदतवाढ मंजूर करा किंवा नाकारा
routes: /ExtensionRequestDetails
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: 38c09035527b95d4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Extension Request Details** मध्ये एक विनंती, प्रोग्रामची कामगिरी आणि जास्त वेळ मागण्याचे कारण दिसते. ती इथे मंजूर करा किंवा नाकारा.

1. **Extension Request List** वर **View Request** दाबा.
2. वरचा भाग तपासा: स्थिती, **Request ID** आणि **Request On**.
3. **Scheme Performance** वाचा: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %** आणि **Remaining to Achieve**.
4. **Extension Request** वाचा: **Current Scheme Period**, **Request New End Date**, **Requested By**, कारण आणि शेरा.
5. **Dealer-wise Actual Sales** तपासा.
6. **Pending** विनंतीसाठी **Approve Extension** किंवा **Reject Request** दाबा.
7. **Extension Approved Successfully** किंवा **Extension Rejected Successfully** उघडते. लिस्टवर परत जाण्यासाठी **View Request Details** दाबा.

> Careful: बटणे लगेच काम करतात. खात्रीसाठी दुसरा बॉक्स येत नाही. मंजूर केल्यावर स्कीमची शेवटची तारीख बदलते.

## नेहमीच्या अडचणी

- **Approve किंवा Reject बटणे नाहीत.** विनंती आधीच **Approved** किंवा **Rejected** झाली आहे.
- **"This request is already approved or the scheme end date has changed. Reload to review it."** (ही विनंती आधीच मंजूर झाली आहे किंवा स्कीमची शेवटची तारीख बदलली आहे. तपासण्यासाठी पेज परत लोड करा) पेज परत लोड करा आणि पुन्हा तपासा.
- **"Request not found".** (विनंती सापडली नाही) लिस्टमधून विनंती परत उघडा.
:::
