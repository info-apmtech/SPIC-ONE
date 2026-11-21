---
module: budget-csr
title: बजेट आणि CSR
icon: bi-wallet2
order: 110
language: mr
category: मार्केटिंग
note: नवीन मार्गदर्शक
meta-source-hash: ed22bedc41464619
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 6fab7f9ab7aa88c7
confidence: ok

वर्षाचे मार्केटिंग बजेट ठरवा, ते राज्ये, रिजन आणि मुख्यालये (HQ) यांना वाटून द्या, आणि कार्यक्रमांमधून ते खर्च करा. प्रत्येक कार्यक्रमाचे दोन फॉर्म असतात: कार्यक्रमाआधी CSR-1 (योजना) आणि कार्यक्रमानंतर CSR-2 (अहवाल). प्रत्येक फॉर्म तपासला जातो आणि मंजूर केला जातो.

- plan | icon: bi-pie-chart | color: #0891b2
  title: बजेट ठरवा
  text: मुख्य कार्यालय वर्षाचे बजेट ठरवते. ते राज्ये, रिजन आणि HQ यांना वाटले जाते, मग महिन्यानुसार कार्यक्रमांमध्ये विभागले जाते.
- csr1 | icon: bi-file-earmark-text | color: #d97706
  title: CSR-1 म्हणजे योजना
  text: कार्यक्रमाआधी तारीख, ठिकाण, मुख्य पिके आणि अंदाजे खर्च भरून CSR-1 करा.
- csr2 | icon: bi-camera | color: #16a34a
  title: CSR-2 म्हणजे अहवाल
  text: कार्यक्रमानंतर फोटो, सहभागी लोक, प्रत्यक्ष खर्च आणि पुरावे देऊन CSR-2 अहवाल द्या.
:::

::: section flow
title: बजेट आणि कार्यक्रम कसे पुढे जातात, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: a83847a70449e86b
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- office | type: lane | color: #0891b2 | roles: Admin, CorporateAdmin
  title: मुख्य कार्यालय
  sub: Admin
- field | type: lane | color: #d97706 | roles: MDO, JMDO, MO, RMD, SMD
  title: फील्ड अधिकारी
  sub: MDO · MO · RMDO · SMDO
- approvers | type: lane | color: #0d9488 | roles: RM, SMM
  title: मंजुरी देणारे
  sub: RM · SMM
- md | type: lane | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: वरिष्ठ मंजुरी देणारे
  sub: MD Admin · MD HOD · AVP
- budget | lane: office | col: 1 | icon: bi-wallet2 | color: #0891b2 | next: plan | screen: state-allocation
  title: बजेट ठरवा आणि वाटून द्या
  status: Active
  text: Admin वर्षाचे बजेट टाकतात आणि ते राज्यांना वाटून देतात. SMD किंवा SMM राज्याचे बजेट रिजनना वाटतात, आणि RM किंवा RMDO रिजनचे बजेट HQ ना वाटतात.
- plan | lane: field | col: 2 | icon: bi-calendar3 | color: #64748b | next: plan-ok | screen: program-budget
  title: कार्यक्रमांचे बजेट ठरवा
  status: Submitted
  text: MO, RMDO आणि SMDO आपले बजेट कार्यक्रम आणि महिन्यांमध्ये विभागतात, मग **Submit** दाबतात.
- plan-ok | lane: approvers | col: 3 | icon: bi-check2-square | color: #2563eb | next: csr1 | screen: program-budget
  title: योजना व्हॅलिडेट करा आणि मंजूर करा
  status: Approved
  text: RMDO किंवा SMDO कार्यक्रमाचे बजेट व्हॅलिडेट करतात (तपासतात). मग RM किंवा SMM ते मंजूर करतात.
- csr1 | lane: field | col: 4 | icon: bi-file-earmark-text | color: #d97706 | next: csr1-ok | screen: create-csr1
  title: CSR-1 करा
  status: Submitted
  text: मंजूर झालेल्या कार्यक्रमासाठी CSR-1 फॉर्ममध्ये तारीख, HQ, ठिकाण, जबाबदार व्यक्ती, मुख्य पिके आणि खर्च भरा, मग **Submit for Validation** दाबा.
- csr1-ok | lane: approvers | col: 5 | icon: bi-patch-check | color: #2563eb | next: csr2 | screen: csr1-list
  title: CSR-1 व्हॅलिडेट करा आणि मंजूर करा
  status: Approved
  text: HQ च्या योजना MO व्हॅलिडेट करतात आणि राज्याच्या योजना SMD व्हॅलिडेट करतात. मग RM किंवा SMM मंजूर करतात. CSR-1 ला सँक्शन नंबर मिळतो आणि तो CSR-2 साठी पात्र होतो.
- csr2 | lane: field | col: 6 | icon: bi-camera | color: #16a34a | next: csr2-ok | screen: csr2-list
  title: कार्यक्रम घ्या आणि CSR-2 अहवाल द्या
  status: Submitted
  text: जबाबदार व्यक्ती कार्यक्रम घेते आणि तारीख, फोटो, सहभागी लोक, प्रत्यक्ष खर्च आणि पुरावे कळवते.
- csr2-ok | lane: approvers | col: 7 | icon: bi-check2-all | color: #16a34a | next: final | screen: csr2-list
  title: CSR-2 व्हॅलिडेट करा आणि मंजूर करा
  status: Approved
  text: MO किंवा SMD अहवाल व्हॅलिडेट करतात. मग RM किंवा SMM तो मंजूर करतात.
- final | lane: office | col: 8 | icon: bi-clipboard-data | color: #0891b2 | screen: final-report
  title: अंतिम अहवाल
  text: मुख्य कार्यालयाला प्रत्येक कार्यक्रम त्याची CSR-1 स्थिती, CSR-2 स्थिती, खर्च आणि बाकी बजेटसह दिसतो.
- late | lane: md | col: 6 | icon: bi-clock-history | color: #dc2626 | kind: exception | from: csr1 | to: csr1-ok | screen: csr1-list
  title: उशिरा आलेल्या फॉर्मना तीन मंजुऱ्या लागतात
  status: Level1Approved
  text: कट-ऑफ दिवसानंतर पाठवलेल्या CSR-1 किंवा CSR-2 ला तीन मंजुऱ्या लागतात: आधी RM किंवा SMM, मग MD Admin, मग MD HOD.
- shortage | lane: md | col: 7 | icon: bi-arrow-left-right | color: #dc2626 | kind: exception | from: csr2-ok | to: csr2-ok | screen: reallocation
  title: बजेट पुरेसे नाही
  status: PendingMDHOD
  text: बजेट कमी असल्यामुळे CSR-2 मंजूर होत नसेल, तर RM, SMM किंवा Admin बजेट रीॲलोकेशनची (बजेट हलवण्याची) विनंती करतात. MD HOD ची अंतिम मंजुरी मिळाल्यावर मंजुरी परत करून पाहिली जाते.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8d3c0359eda444f2
confidence: ok

- active | color: #16a34a
  label: Active
  text: वर्षाच्या बजेटची ही नोंद वापरात आहे.
- inactive | color: #64748b
  label: Inactive
  text: वर्षाच्या बजेटची ही नोंद बंद केली आहे. ती परत चालू करता येते.
- draft | color: #64748b
  label: Draft
  text: सेव्ह केले आहे पण पाठवलेले नाही. तुम्ही अजून बदल करू शकता.
- submitted | color: #d97706
  label: Submitted
  text: पाठवले आहे आणि व्हॅलिडेशनची वाट पाहत आहे.
- pending | color: #d97706
  label: Pending
  text: पुढच्या तपासणीची वाट पाहत आहे.
- validated | color: #2563eb
  label: Validated
  text: व्हॅलिडेट करणाऱ्याने तपासले आहे. मंजुरीची वाट पाहत आहे.
- level1 | color: #7c3aed
  label: Level1Approved
  text: उशिरा आलेल्या फॉर्मला पहिली मंजुरी मिळाली. MD Admin ची वाट पाहत आहे.
- level2 | color: #7c3aed
  label: Level2Approved
  text: उशिरा आलेल्या फॉर्मला दुसरी मंजुरी मिळाली. MD HOD ची वाट पाहत आहे.
- approved | color: #16a34a
  label: Approved
  text: पूर्ण मंजूर. मंजूर झालेला CSR-1 पुढे CSR-2 साठी जाऊ शकतो.
- rejected | color: #dc2626
  label: Rejected
  text: नाकारले. नाकारण्याचे कारण वाचा.
- pending-smm | color: #d97706
  label: PendingSMM
  text: बजेट रीॲलोकेशन SMM ची वाट पाहत आहे.
- pending-avp | color: #d97706
  label: PendingAVP
  text: बजेट रीॲलोकेशन AVP ची वाट पाहत आहे.
- pending-mdhod | color: #d97706
  label: PendingMDHOD
  text: बजेट रीॲलोकेशन शेवटचे मंजुरी देणारे MD HOD यांची वाट पाहत आहे.
- healthy | color: #16a34a
  label: Healthy
  text: HQ कडे पुरेसे बजेट बाकी आहे.
- near-limit | color: #d97706
  label: Near Limit
  text: HQ ने आपले बहुतेक बजेट वापरले आहे.
- exceeded | color: #dc2626
  label: Exceeded
  text: HQ ने बजेटपेक्षा जास्त खर्च केला आहे.
- ext-pending | color: #d97706
  label: Pending Approval
  text: मुदतवाढीची विनंती निर्णयाची वाट पाहत आहे.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5e86c95e22cb73c4
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- admin | color: #0891b2 | roles: Admin, CorporateAdmin
  title: Admin
  - प्रत्येक आर्थिक वर्षासाठी वर्षाचे बजेट टाका
  - वर्षाचे बजेट राज्यांना वाटून द्या
  - कार्यक्रमांची लिस्ट, सँक्शन नंबर आणि RMD HQ मॅपिंग सांभाळा
  - राज्यांमध्ये बजेट रीॲलोकेशनची विनंती करा
- smm | color: #0d9488 | roles: SMM
  title: SMM
  - राज्याचे बजेट रिजनना वाटून द्या
  - कार्यक्रमांचे बजेट, राज्याचे CSR-1 आणि राज्याचे CSR-2 मंजूर करा
  - रिजनमध्ये बजेट रीॲलोकेशनची विनंती करा
  - RM कडून आलेल्या रीॲलोकेशनच्या विनंत्या मंजूर करा
- smd | color: #ea580c | roles: SMD
  title: SMDO
  - कार्यक्रमांचे बजेट ठरवा आणि ते व्हॅलिडेट करा
  - राज्याचे बजेट रिजनना वाटून द्या
  - राज्याच्या कार्यक्रमांसाठी CSR-1 करा
  - राज्याचे CSR-1 आणि CSR-2 व्हॅलिडेट करा
- rm | color: #2563eb | roles: RM
  title: RM
  - रिजनचे बजेट HQ ना वाटून द्या
  - कार्यक्रमांचे बजेट, HQ चे CSR-1 आणि HQ चे CSR-2 मंजूर करा
  - HQ मध्ये बजेट रीॲलोकेशनची विनंती करा
- rmd | color: #9333ea | roles: RMD
  title: RMDO
  - कार्यक्रमांचे बजेट ठरवा आणि ते व्हॅलिडेट करा
  - रिजनचे बजेट HQ ना वाटून द्या
  - HQ च्या कार्यक्रमांसाठी CSR-1 करा
- mo | color: #d97706 | roles: MO
  title: MO
  - कार्यक्रमांचे बजेट ठरवा
  - CSR-1 करा
  - HQ चे CSR-1 आणि CSR-2 व्हॅलिडेट करा
- mdo | color: #65a30d | roles: MDO, JMDO
  title: MDO आणि JMDO
  - HQ च्या कार्यक्रमांसाठी CSR-1 करा
  - तुम्ही जबाबदार व्यक्ती असाल तर कार्यक्रम घ्या आणि CSR-2 अहवाल द्या
- senior | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: MD Admin, MD HOD आणि AVP
  - उशिरा आलेल्या फॉर्मना MD Admin आणि MD HOD दुसरी आणि तिसरी मंजुरी देतात
  - AVP आणि MD HOD बजेट रीॲलोकेशन मंजूर करतात
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 471a0e1b0196bf4a
confidence: ok

- csr2-grey | popular: true
  q: CSR-2 बटण राखाडी आहे
  a: CSR-1 **Approved** झाल्यावरच CSR-2 उघडते. मग **CSR-2 Eligibility** कॉलममध्ये **Eligible for CSR-2** दिसते आणि बटण चालते.
- one-month | popular: true
  q: CSR-1 Create मध्ये फक्त एकाच महिन्याच्या योजना दिसतात
  a: हे नेहमीचेच आहे. निळ्या बॉक्समध्ये कोणता महिना ते लिहिलेले असते, उदा. "Showing plans for December 2026 only". CSR-1 च्या कट-ऑफ दिवसापर्यंत तुम्ही पुढच्या महिन्याची योजना करता; त्यानंतर त्याच्या पुढच्या महिन्याची.
- must-equal
  q: Submit केल्यावर एकूण रक्कम दिलेल्या बजेटएवढी हवी असे दिसते
  a: **Budgeting Management** वर तुम्हाला तुमचे संपूर्ण बजेट कार्यक्रमांमध्ये वाटावे लागते. मेसेजमध्ये अजून किती वाटायचे बाकी आहे ते दिसते. एकूण रक्कम जुळेपर्यंत कार्यक्रमांची संख्या वाढवा.
- no-money
  q: बजेट कमी असल्यामुळे CSR-2 मंजूर होत नाही
  a: दुसऱ्या राज्यातून, रिजनमधून किंवा HQ मधून पैसे हलवण्यासाठी RM, SMM किंवा Admin **Budget Reallocations** वर विनंती करतात. MD HOD ने ती मंजूर केल्यावर परत मंजुरी करून पाहा.
- three-levels
  q: माझ्या CSR-1 किंवा CSR-2 ला तीन मंजुऱ्या का लागतात?
  a: तो कट-ऑफ दिवसानंतर पाठवला होता. उशिरा आलेले फॉर्म आधी RM किंवा SMM, मग MD Admin, मग MD HOD यांच्याकडे जातात.
- save-state-first
  q: मला "Save this state's allocation above before distributing it to regions." (रिजनना वाटण्याआधी वर राज्याचे वाटप सेव्ह करा) दिसते
  a: त्या राज्याला अजून बजेट मिळालेले नाही. Admin ने आधी त्या आर्थिक वर्षासाठी राज्याचे वाटप सेव्ह करायला हवे.
:::

::: section annual-budget
title: वर्षाचे बजेट टाका
routes: /AnnualBudgeting
for: 
roles: 
designations: 
minutes: 2
source-hash: 97c15e0498b487cc
confidence: ok

**ही स्क्रीन कशासाठी आहे**
Admin प्रत्येक आर्थिक वर्षाचे एकूण बजेट टाकण्यासाठी **Annual Budgeting** स्क्रीन वापरतात. बजेटच्या बाकी सगळ्या स्क्रीन याच रकमेपासून सुरू होतात.

1. **Add Annual Budgeting** खाली **Financial Year (FY)** निवडा.
2. **Amount** टाका.
3. स्विच **Active** वर राहू द्या, किंवा नोंद **Inactive** करण्यासाठी तो टॅप करा.
4. **Save** दाबा. फॉर्म रिकामा करण्यासाठी **Cancel** दाबा.
5. **Existing Annual Budgeting Records** मध्ये FY ने शोधा, किंवा लिस्ट फिल्टर करण्यासाठी **Active** किंवा **Inactive** निवडा.
6. नोंद बदलण्यासाठी पेन्सिल टॅप करा, मग **Update** दाबा. नोंद बंद करण्यासाठी तिरकी रेघ असलेला गोल आयकॉन टॅप करा, किंवा ती परत चालू करण्यासाठी बाणाचा आयकॉन टॅप करा.

> Tip: प्रत्येक आर्थिक वर्षासाठी बजेटची एकच नोंद असू शकते, ती चालू असो वा बंद.

## नेहमीच्या अडचणी

- **"Please select a Financial Year".** (आर्थिक वर्ष निवडा) सेव्ह करण्याआधी वर्ष निवडा.
- **"Amount must be greater than zero".** (रक्कम शून्यापेक्षा जास्त हवी) 0 पेक्षा जास्त रक्कम टाका.
- **"An Annual Budgeting record for ... already exists".** (या वर्षाची नोंद आधीच आहे) नवीन नोंद जोडण्याऐवजी आधीची नोंद बदला.
- **Save बटण राखाडी आहे.** अजून एकही आर्थिक वर्ष तयार केलेले नाही. आधी Financial Master मध्ये ते जोडा.
:::

::: section state-allocation
title: बजेट राज्ये, रिजन आणि HQ यांना वाटून द्या
routes: /StateBudgetManagement
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD
designations: 
tour: 1 > [data-help=bc-sb-tabs] | 2 > [data-help=bc-sb-select] | 3 > [data-help=bc-sb-cards] | 4 > [data-help=bc-sb-table] | 5 > [data-help=bc-sb-save]
minutes: 3
source-hash: 535c04c3b89862f4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**State Budgeting Management** स्क्रीनवर वर्षाचे बजेट एका वेळी एक पातळी खाली वाटले जाते. Admin ते राज्यांना वाटतात, SMD किंवा SMM राज्याचे बजेट त्याच्या रिजनना वाटतात, आणि RM किंवा RMDO रिजनचे बजेट त्याच्या HQ ना वाटतात.

1. तुमच्या रोलनुसार तुमचा टॅब उघडतो: **State Allocation** (Admin), **Region Allocation** (SMD, SMM) किंवा **Headquarters Allocation** (RM, RMDO).
2. **Financial Year** निवडा. रिजन टॅबवर **State** पण निवडा. HQ टॅबवर **State** आणि **Region** निवडा.
3. कार्डे तपासा, जसे **Annual Budget**, **Allocated Budget** आणि **Remaining Budget**, किंवा **State Allocated Budget** आणि **State MDO Budget**.
4. टेबलमध्ये प्रत्येक राज्य, रिजन किंवा HQ साठी रक्कम टाइप करा. रिजन आणि HQ टॅबवर आधी वर **State MDO Amount** किंवा **Region MDO Amount** टाका.
5. सेव्ह करा. राज्य टॅबवर **Save Draft** दाबा, मग **Submit For Validation** दाबा. बाकीच्या टॅबवर **Save Region Allocation** किंवा **Save Headquarters Allocation** दाबा.

> Careful: राज्य टॅबवर सेव्ह करण्याआधी वर्षाचे संपूर्ण बजेट वाटून झालेले असले पाहिजे.

## नेहमीच्या अडचणी

- **"All budget must be allocated to states."** (सगळे बजेट राज्यांना वाटायला हवे) मेसेजमध्ये अजून बाकी असलेली रक्कम दिसते. ती राज्यांना वाटून द्या.
- **"Region allocation cannot exceed the remaining state budget"** किंवा **"Headquarters allocation cannot exceed the remaining region budget".** (वाटप बाकी बजेटपेक्षा जास्त असू शकत नाही) रक्कम कमी करा.
- **"Save this region's allocation above before distributing it to headquarters."** (HQ ना वाटण्याआधी वर रिजनचे वाटप सेव्ह करा) रिजनला अजून बजेट मिळालेले नाही. SMD किंवा SMM ने आधी रिजनचे वाटप सेव्ह करायला हवे.
- **"You do not have access to any budget allocation level."** (तुम्हाला बजेट वाटपाचा ॲक्सेस नाही) तुमचा रोल बजेट वाटत नाही. तुमच्या ॲडमिनिस्ट्रेटरला विचारा.
:::

::: section program-budget
title: कार्यक्रमांचे बजेट ठरवा, व्हॅलिडेट करा आणि मंजूर करा
routes: /BudgetingManagements
for: 
roles: 
designations: 
minutes: 4
source-hash: 9d6de3d0ef847b35
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Budgeting Management** वर MO, RMDO आणि SMDO आपले बजेट कार्यक्रम आणि महिन्यांमध्ये विभागतात. RMDO आणि SMDO सेव्ह केलेल्या योजना व्हॅलिडेट करतात, आणि RM आणि SMM त्या मंजूर करतात.

1. **Program-wise Budget Planning** च्या वर आर्थिक वर्ष निवडा.
2. कार्डे तपासा: तुमचे बजेट (उदा. **Available Budget**), **Allocated Budget**, **Remaining Budget** आणि **SMDO Budget**.
3. **Budget plan list** मध्ये **April** ते **March** प्रत्येक महिन्यात किती कार्यक्रम घेणार ते टाका. कार्यक्रमाला परवानगी असेल तर तुम्ही त्याची **Budget Amount** बदलू शकता. प्रत्येक ओळीचे **Total Budget** आपोआप बदलते.
4. **Submit** दाबा. संपूर्ण बजेट वाटलेले असले पाहिजे.
5. व्हॅलिडेट करण्यासाठी (RMDO किंवा SMDO) **Validate Budget** दाबा, **Saved program budgets awaiting validation** मधून एक योजना निवडा, गरज असेल तर संख्या दुरुस्त करा आणि **Validate** दाबा. तुमच्या स्वतःच्या योजनेवर परत जाण्यासाठी **Create Budget** दाबा.
6. मंजूर करण्यासाठी (RM किंवा SMM) **Validated program budgets awaiting approval** मधून एक योजना निवडा, गरज असेल तर संख्या दुरुस्त करा आणि **Approve** दाबा.
7. टेबल शेअर करण्यासाठी **Copy**, **CSV**, **Excel**, **PDF** किंवा **Print** वापरा.

## नेहमीच्या अडचणी

- **"Total allocated budget ... must equal allotted budget ..."** (वाटलेले एकूण बजेट दिलेल्या बजेटएवढे हवे) काही बजेट अजून बाकी आहे. काहीही बाकी राहणार नाही इतकी कार्यक्रमांची संख्या वाढवा.
- **"... exceeds allotted budget ..."** (दिलेल्या बजेटपेक्षा जास्त) तुम्ही जास्त योजना केली आहे. संख्या कमी करा.
- **"No eligible budgets found for your assigned scope and selected FY."** (तुमच्या भागासाठी आणि निवडलेल्या वर्षासाठी बजेट सापडले नाही) या वर्षात तुमच्याकडे काहीही बाकी नाही. वर्ष तपासा.
- **"Only MO, RMDO and SMDO can save program budgets."** (फक्त MO, RMDO आणि SMDO कार्यक्रमांचे बजेट सेव्ह करू शकतात) तुमचा रोल फक्त पाहू, व्हॅलिडेट किंवा मंजूर करू शकतो.
:::

::: section budget-submissions
title: मी पाठवलेल्या बजेटचा मागोवा घ्या
routes: /BudgetSubmissions
for: 
roles: 
designations: 
minutes: 2
source-hash: 3e92ca02097adb1c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Budget Submissions** स्क्रीनवर तुम्ही पाठवलेल्या कार्यक्रमांच्या बजेटची लिस्ट असते आणि प्रत्येकाची स्थिती दिसते.

1. कार्डे पाहा: **Program Budget**, **Approved**, **Pending validation**, **Rejected** आणि **Drafts**.
2. **Date Range**, **Program Type**, **Program Name** किंवा **Status** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
3. कार्यक्रम पटकन शोधण्यासाठी **Search Program...** मध्ये टाइप करा.
4. टेबलमध्ये **Program Budget**, **Submitted Date**, **Status** आणि **Validation Due** तपासा.
5. तपशील पाहण्यासाठी डोळ्याचा आयकॉन टॅप करा. तिथून तुम्ही **Print** किंवा **Export PDF** करू शकता.

## नेहमीच्या अडचणी

- **"No budget submission records found."** (एकही नोंद सापडली नाही) तुम्ही अजून योजना पाठवलेली नाही, किंवा फिल्टर खूप मर्यादित आहेत. **Clear All** दाबा.
:::

::: section hq-budget-overview
title: HQ चा बजेट वापर पाहा
routes: /BudgetOverview
for: 
roles: 
designations: 
minutes: 1
source-hash: 6911fbefa137aa04
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**HQ Budget Allocation** स्क्रीनवर रिजनचे बजेट त्याच्या HQ ना कसे वाटले आहे आणि प्रत्येक HQ ने किती वापरले ते दिसते.

1. कार्डे पाहा: **Total Regional Budget**, **Allocated to HQs**, **Remaining Budget** आणि **Utilizes Budget**.
2. **Budget plan list** मध्ये प्रत्येक HQ चे **Allocated**, **Utilized**, **Remaining** आणि **Utilization %** तपासा, तसेच त्याचे **Pending CSR-1** आणि **Pending CSR-2**.
3. **Status** मध्ये **Healthy**, **Near Limit** किंवा **Exceeded** दिसते.
4. HQ चा बजेट वापर उघडण्यासाठी डोळ्याचा आयकॉन टॅप करा.
5. **Budgeting Management** वर जाण्यासाठी **Allocate Budget** दाबा.

## नेहमीच्या अडचणी

- **आकडे बदलत नाहीत.** ही स्क्रीन अजून तयार होत आहे. खरे आकडे पाहण्यासाठी **Budgeting Management** आणि **State Budgeting Management** वापरा.
:::

::: section reallocation
title: रीॲलोकेशनने बजेट हलवा
routes: /BudgetReallocations
for: 
roles: 
designations: 
minutes: 3
source-hash: 61df1dfc270033ec
confidence: ok

**ही स्क्रीन कशासाठी आहे**
बजेट कमी असल्यामुळे CSR-2 मंजूर होत नसेल, तेव्हा **Budget Reallocations** स्क्रीन एका बजेटमधून त्याच पातळीवरच्या दुसऱ्या बजेटमध्ये पैसे हलवते. RM पैसे HQ मध्ये हलवतात, SMM रिजनमध्ये, आणि Admin राज्यांमध्ये.

1. **New Reallocation Request** खाली **Initiated by** आणि पातळीचा बॅज तपासा.
2. **Financial Year** निवडा. विचारले तर **State** आणि **Region** निवडा.
3. **From** (पैसे देणारे बजेट) आणि **To** (ज्याला पैसे हवे आहेत ते बजेट) निवडा. **Source Available Budget** आणि **Selected destination** तपासा.
4. **Reallocation Amount (₹)** टाका आणि **Reason for Reallocation** लिहा.
5. **Approval route** तपासा, उदा. RM, SMM, AVP, MD HOD. **Apply Reallocation** दाबा.
6. मंजुरी देणारे: **Reallocation Requests** मध्ये **Approval / Rejection Remarks** लिहा, मग त्या ओळीवर **Approve** किंवा **Reject** दाबा.
7. नवीन स्थिती पाहण्यासाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **"Source budget shortage".** (देणाऱ्या बजेटमध्ये पैसे कमी आहेत) रक्कम देणाऱ्या बजेटमध्ये बाकी असलेल्या रकमेपेक्षा जास्त आहे. ती कमी करा किंवा दुसरे बजेट निवडा.
- **"No eligible budgets found."** (बजेट सापडले नाही) वर्ष तपासा, आणि तुमच्या राज्याचे किंवा रिजनचे वाटप मंजूर झाले आहे का ते तपासा.
- **"Apply permissions are unavailable for this login."** (या लॉगिनला विनंती करण्याची परवानगी नाही) फक्त RM, SMM आणि Admin रीॲलोकेशनची विनंती करू शकतात.
:::

::: section program-master
title: कार्यक्रमांची लिस्ट सांभाळा
routes: /ProgramMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 800f0d1d4704db06
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Program Master** स्क्रीनवर कार्यक्रमांचे प्रकार आणि कार्यक्रमांची लिस्ट, ते कोण ठरवू शकते, आणि प्रत्येक राज्यातली कार्यक्रमाची रक्कम ठेवली जाते.

1. **Program Types** उघडा. **Program Type Name** टाका, **Is Amount Change** ला **Allowed** किंवा **Not Allowed** करा, आणि **Save** दाबा.
2. **Program Master** उघडा. **Program Type** निवडा, **Program Name** टाका, आणि **MO**, **RMDO** आणि **SMDO** ला **Eligible** किंवा **Not Eligible** करा. **Save** दाबा.
3. ओळ बदलण्यासाठी त्यावरची पेन्सिल टॅप करा.
4. **State Mapping** उघडा (किंवा कार्यक्रमावरची मॅप पिन टॅप करा). **Program** निवडा.
5. प्रत्येक राज्यासाठी **Applicable** चालू किंवा बंद करा. लागू असलेल्या राज्यासाठी **Program Amount (₹)** टाका आणि त्या ओळीवर **Save** दाबा.

> Tip: **Budgeting Management** वर योजना करणारे कार्यक्रमाची बजेट रक्कम बदलू शकतात की नाही, हे **Is Amount Change** ठरवते.

## नेहमीच्या अडचणी

- **"View only - you do not have update permission."** (फक्त पाहता येते, बदल करण्याची परवानगी नाही) तुमच्या ॲडमिनिस्ट्रेटरकडे बदल करण्याची परवानगी मागा.
:::

::: section rmd-hq-mapping
title: RMDO ला HQ द्या
routes: /RMDHQMapping
for: 
roles: 
designations: 
minutes: 2
source-hash: 1c5c04d12fa1b412
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**RMD Headquarters Mapping** स्क्रीन एकाच नोंदीत एका RMDO ला एक किंवा अनेक HQ जोडते.

1. **Assign Headquarters** खाली **Employee (RMD)** निवडा.
2. **State** निवडा, मग **Region**.
3. **Select Headquarters** टॅप करा आणि HQ वर टिक करा, किंवा **Select All** टॅप करा. **Done** दाबा.
4. **Save Mapping** दाबा. **Clear** ने फॉर्म रिकामा होतो.
5. **Existing RMD HQ Mappings** मध्ये बदलण्यासाठी पेन्सिल टॅप करा (मग **Update Mapping**), किंवा डिलीट करण्यासाठी फुली टॅप करा (मग **Confirm**).

## नेहमीच्या अडचणी

- **"No headquarters found."** (एकही HQ सापडले नाही) आधी रिजन निवडा, किंवा त्या रिजनमध्ये एकही HQ नाही.
:::

::: section sanction-sequence
title: सँक्शन नंबरची सुरुवातीची किंमत ठरवा
routes: /SanctionNumberSequence
for: 
roles: 
designations: 
minutes: 2
source-hash: 7e920cca79cfe7a9
confidence: ok

**ही स्क्रीन कशासाठी आहे**
मंजूर झालेल्या CSR-1 ला सँक्शन नंबर मिळतो. **Sanction Number Sequence** स्क्रीनवर प्रत्येक राज्य किंवा रिजनसाठी या नंबरची सुरुवातीची किंमत ठरवली जाते.

1. **Sequence Scope** निवडा: **State (SMM)** किंवा **Region (RM / RMM)**.
2. **State** निवडा, आणि रिजनच्या scope साठी **Region** पण निवडा. **State Code** आणि **Region Code** मास्टरमधून आपोआप भरले जातात.
3. सुरुवातीची किंमत टाका.
4. **Save Sequence** दाबा. **Clear** ने फॉर्म रिकामा होतो.
5. **Existing Sanction Number Sequences** लिस्ट तपासा. राज्य, रिजन किंवा कोडने शोधा.

> Careful: एकदा तयार केलेला sequence नंतर बदलता येत नाही.

## नेहमीच्या अडचणी

- **"Code not configured in State master".** (State master मध्ये कोड टाकलेला नाही) आधी त्या राज्याचा किंवा रिजनचा कोड त्याच्या मास्टरमध्ये टाका.
:::

::: section create-csr1
title: कार्यक्रमासाठी CSR-1 करा
routes: /CREATE-CSR-1Management
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c1-banner] | 2 > [data-help=bc-c1-search] | 3 > [data-help=bc-c1-plan] | 4 > [data-help=bc-c1-focus] | 5 > [data-help=bc-c1-expense] | 6 > [data-help=bc-c1-actions]
minutes: 5
source-hash: 640ab3b5f3b1dedd
confidence: ok

**ही स्क्रीन कशासाठी आहे**
CSR-1 म्हणजे एका कार्यक्रमाची योजना, जी कार्यक्रमाआधी केली जाते. **CSR-1 Program - Wise Budget Allocation** स्क्रीनवर तुमच्या मंजूर झालेल्या कार्यक्रम बजेटची लिस्ट असते. त्यातला एक निवडा आणि CSR-1 फॉर्म भरा.

1. निळा बॉक्स वाचा. तुम्ही कोणत्या महिन्याची योजना करत आहात ते त्यात लिहिलेले असते.
2. **Budget plan list** मध्ये कार्यक्रम शोधा (**Search Program...** किंवा **Filters** वापरा), **Remaining Programs** तपासा, आणि **Apply CSR 1** दाबा.
3. **CSR-1 Plan Details** खाली कार्यक्रम तपासा आणि **Budget**, **HQ**, **Location**, **Responsible Person** आणि **Date** भरा.
4. **Focus Details** खाली किमान एक **Focused Crop** आणि त्यासाठी एक **Focused Product** निवडा. प्रत्येक बॉक्समध्ये तुम्ही तीन पिके आणि तीन प्रॉडक्टपर्यंत जोडू शकता.
5. **Expense Breakdown** खाली प्रत्येक खर्चाची अंदाजे रक्कम टाका. **Total Expenses** बजेटच्या आतच राहायला हवा.
6. नंतर पूर्ण करायचे असेल तर **Save Draft** दाबा, किंवा पाठवण्यासाठी **Submit for Validation** दाबा.

> Tip: **Responsible Person** कार्यक्रम घेतील आणि CSR-2 अहवाल देतील.

| फील्ड | काय भरायचे |
|---|---|
| **Budget** | या कार्यक्रमासाठीचे पैसे. भरणे आवश्यक. |
| **HQ** आणि **Location** | कार्यक्रम कुठे होणार. भरणे आवश्यक. |
| **Responsible Person** | कार्यक्रम घेणारा अधिकारी. भरणे आवश्यक. |
| **Date** | कार्यक्रमाची तारीख, ठरवलेल्या महिन्यातलीच. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Budget Exceeded"** किंवा **"Total Expense Breakdown ... exceeds the allocated Budget".** (खर्च बजेटपेक्षा जास्त आहे) खर्चाच्या रकमा कमी करा.
- **"Please enter at least one expense amount."** (किमान एका खर्चाची रक्कम टाका) किमान एक अंदाजे खर्च टाका.
- **"Please select at least one Product for Focus Crop 1."** (Focus Crop 1 साठी किमान एक प्रॉडक्ट निवडा) तुम्ही निवडलेल्या प्रत्येक पिकासाठी एक प्रॉडक्ट निवडा.
- **"CSR-1 creation is not available for Admin role."** (Admin रोलला CSR-1 करता येत नाही) Admin फक्त **CSR-1 Management** वर CSR-1 पाहू शकतात.
:::

::: section csr1-list
title: CSR-1 चा मागोवा घ्या, व्हॅलिडेट करा आणि मंजूर करा
routes: /CSR-1List
for: 
roles: 
designations: 
minutes: 3
source-hash: 33a5bb3a053b06a1
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**CSR-1 Management** मध्ये CSR-1 योजनांची लिस्ट असते. फील्ड अधिकारी आपल्या योजनांचा मागोवा घेतात. MO आणि SMD त्या व्हॅलिडेट करतात, आणि RM आणि SMM त्या मंजूर करतात.

1. कार्डे पाहा: **Total CSR -1**, **Approved**, **Pending**, **Rejected** आणि **Drafts**.
2. **Filters** खाली **From Date**, **To Date**, **Program Type**, **Program Name** किंवा **Status** निवडा, मग **Apply Filters** दाबा.
3. **CSR-1 plan list** मध्ये **Status**, **Sanction Number** आणि **CSR-2 Eligibility** तपासा. **State** किंवा **HQ** टॅगवरून योजनेची पातळी कळते.
4. **CSR-1 Program Details** उघडण्यासाठी डोळ्याचा आयकॉन टॅप करा.
5. योजना तुमची वाट पाहत असेल तर **Validate** दाबा, किंवा मंजुरीचे बटण दाबा (**Approve**, किंवा उशिरा आलेल्या योजनेसाठी पातळीचे नाव). नाकारायचे असेल तर **Reject** दाबा, शेरा लिहा आणि **Confirm Reject** दाबा.
6. योजना **Approved** झाल्यावर कार्यक्रमाचा अहवाल देण्यासाठी ओळीवर **CSR-2** दाबा, किंवा तपशीलामध्ये **Open CSR-2** दाबा.

> Careful: डस्टबिनचा आयकॉन CSR-1 लगेच डिलीट करतो, परत विचारत नाही.

## नेहमीच्या अडचणी

- **तपशीलामध्ये "Awaiting MO Validation" किंवा "Awaiting SMD Validation" दिसते.** कोणी मंजूर करण्याआधी योजना व्हॅलिडेट व्हायला हवी.
- **CSR-2 बटण राखाडी आहे.** CSR-1 अजून **Approved** झालेला नाही.
- **लाल "Rejection Reason" बॉक्स दिसतो.** लिस्टच्या खाली कारण वाचा.
:::

::: section csr2-list
title: CSR-2 अहवाल द्या, व्हॅलिडेट करा आणि मंजूर करा
routes: /CSR-2List
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c2-cards] | 2 > [data-help=bc-c2-filters] | 3 > [data-help=bc-c2-table] | 4 > [data-help=bc-c2-table] | 5 > [data-help=bc-c2-table]
minutes: 3
source-hash: 3490f090fad6d820
confidence: ok

**ही स्क्रीन कशासाठी आहे**
CSR-2 म्हणजे कार्यक्रम झाल्यानंतरचा अहवाल. **CSR-2 Program Execution** लिस्टमध्ये तुमचे कार्यक्रम उपस्थिती, फोटो-व्हिडिओ (media), खर्च आणि प्रगतीसह दिसतात.

1. कार्डे पाहा: **Total CSR-2**, **Approved**, **Submitted** आणि **Drafts**. फक्त ते कार्यक्रम पाहण्यासाठी कार्ड टॅप करा.
2. **Filters** खाली तारखा, **Program Type**, **Program Name**, **Location** किंवा **Status** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
3. **Assigned Programs** मध्ये **Approved Budget**, **Attendance**, **Media**, **Actual Expense**, **Report Completion** आणि **Status** तपासा.
4. CSR-2 फॉर्म उघडण्यासाठी **View Details** दाबा. कार्यक्रमाची तारीख, फोटो, सहभागी लोक आणि प्रत्येक प्रत्यक्ष खर्च त्याच्या पुराव्यासह भरा, मग पाठवा.
5. अहवाल तुमची वाट पाहत असेल तर **Validate** (MO किंवा SMD) दाबा, किंवा मंजुरीचे बटण दाबा, जसे **Approve CSR-2** किंवा **Approve Level 1**.

> Tip: अहवाल **Draft** पासून **Approved** पर्यंत पुढे जाईल तसे **Report Completion** वाढत जाते.

## नेहमीच्या अडचणी

- **"No CSR-2 records found."** (CSR-2 ची एकही नोंद सापडली नाही) तुम्हाला एकही मंजूर CSR-1 दिलेला नाही, किंवा फिल्टर खूप मर्यादित आहेत.
- **"You are not authorized to validate this CSR-2."** (तुम्हाला हा CSR-2 व्हॅलिडेट करण्याची परवानगी नाही) दुसऱ्या रोलने तो व्हॅलिडेट करायला हवा.
- **बजेटमुळे मंजुरी होत नाही.** RM, SMM किंवा Admin कडे बजेट रीॲलोकेशन मागा.
:::

::: section csr2-assigned
title: मला दिलेले कार्यक्रम उघडा
routes: /CSR-2ProgramExecution
for: 
roles: 
designations: 
minutes: 1
source-hash: 6d55229f3d01c986
confidence: ok

**ही स्क्रीन कशासाठी आहे**
ही **CSR-2 Program Execution** स्क्रीन तुम्हाला दिलेल्या मंजूर CSR-1 कार्यक्रमांची लिस्ट दाखवते, म्हणजे तुम्ही त्यांचे CSR-2 अहवाल सुरू करू शकता.

1. वर **Total CSR -2** पाहा.
2. **Assigned Programs** मध्ये कार्यक्रम त्याच्या **Sanction Number**, **Program Type** किंवा **Program** वरून शोधा.
3. **Approved Budget** आणि **Status** तपासा.
4. त्या कार्यक्रमाचा CSR-2 फॉर्म उघडण्यासाठी **View Program** दाबा.
5. नवीन लिस्ट आणण्यासाठी **Refresh Data** दाबा.

## नेहमीच्या अडचणी

- **"No approved CSR-1 programs found."** (मंजूर CSR-1 कार्यक्रम सापडले नाहीत) तुमची एकही CSR-1 योजना अजून मंजूर झालेली नाही.
:::

::: section csr2-form-preview
title: CSR-2 Management फॉर्म
routes: /CSR2, /csrplandetails2
for: 
roles: 
designations: 
minutes: 1
source-hash: 7593c2c21789d74d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**CSR-2 Management** स्क्रीनवर CSR-2 अहवालाचे भाग दिसतात: बजेट कार्डे, योजनेचा तपशील, मुख्य पिकांचा तपशील आणि पुराव्यांसह खर्चाचा तपशील. खऱ्या कार्यक्रमाचा अहवाल देण्यासाठी **CSR-2 List** उघडा आणि **View Details** दाबा.

1. कार्डे तपासा: **Approved Budget**, **Actual Expense** आणि **Difference**.
2. **CSR-2 Plan Details** पाहा: कार्यक्रम, कार्यक्रमांची संख्या, बजेट, HQ आणि ठिकाण.
3. **Focus Details** पाहा: मुख्य पिके आणि प्रॉडक्ट.
4. **Expense Breakdown** पाहा: प्रत्येक खर्चाला रक्कम, **Date** आणि **Upload proof** असते.

## नेहमीच्या अडचणी

- **इथे Save Draft किंवा Submit CSR-2 दाबून काहीही होत नाही.** अहवाल पाठवण्यासाठी **CSR-2 List** आणि **View Details** वापरा.
:::

::: section final-report
title: CSR चा अंतिम अहवाल पाहा
routes: /FinalReportCSRView
for: 
roles: 
designations: 
minutes: 1
source-hash: d8668b0d71867c79
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Final Report - CSR Program Tracking** वर प्रत्येक कार्यक्रम योजनेपासून अहवालापर्यंत दिसतो: CSR-1 स्थिती, CSR-2 स्थिती, सँक्शन नंबर, खर्च आणि बाकी बजेट.

1. कार्डे पाहा: **Total Programs**, **Approved**, **Pending**, **Rejected** आणि **Drafts**.
2. **Filters** वापरून तारखा, **Program Type**, **Program Name**, **CSR Type** किंवा **Status** निवडा, मग **Apply Filters** दाबा.
3. **CSR-2 plan list** मध्ये **Total Expenses**, **Approved Budget** आणि **Remaining Budget** यांची तुलना करा. **Report Status** मध्ये **Completed** किंवा **Pending** दिसते.
4. कार्यक्रमाचा तपशील पाहण्यासाठी डोळ्याचा आयकॉन टॅप करा, मग **Print** किंवा **Export PDF** करा.

## नेहमीच्या अडचणी

- **आकडे बदलत नाहीत.** ही स्क्रीन अजून तयार होत आहे. खऱ्या नोंदींसाठी **CSR-1 Management** आणि **CSR-2 List** वापरा.
:::

::: section rmd-queue
title: RMD व्हॅलिडेशनची रांग
routes: /RMDValidationQueue
for: 
roles: 
designations: 
minutes: 2
source-hash: 641c748d235d2f14
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**RMD Validation Queue** मध्ये मार्केट ऑफिसरकडून (MO) आलेल्या CSR-1 आणि CSR-2 ची लिस्ट असते.

1. टॅब निवडा: **CSR-1 Submissions** किंवा **CSR-2 Reports**. बॅजवर त्यांची संख्या दिसते.
2. **Filters** उघडून **Date Range**, **Program Type**, **Head Quarters** किंवा **Status** निवडा, मग **Apply Filters** दाबा.
3. एखादी नोंद शोधण्यासाठी सर्च बॉक्समध्ये टाइप करा.
4. **MO Name**, **Program Name**, **Head Quarters**, **Total Budget**, **Validation Due** आणि **Status** तपासा.
5. नवीन लिस्ट आणण्यासाठी **Refresh Data** दाबा.

## नेहमीच्या अडचणी

- **"Failed to load data".** (माहिती लोड झाली नाही) तुमचे इंटरनेट तपासा आणि **Try Again** दाबा.
- **"No submissions found".** (एकही नोंद सापडली नाही) Filters मध्ये **Clear All** दाबा.
:::

::: section mo-validation
title: MO च्या नोंदींचे व्हॅलिडेशन
routes: /MOSubmissionValidation
for: 
roles: 
designations: 
minutes: 1
source-hash: fbb06076726e4306
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**MO Submission Validation** स्क्रीनवर मार्केट ऑफिसरकडून आलेल्या CSR-1 आणि CSR-2 ची लिस्ट, ठरवलेल्या आणि प्रत्यक्ष बजेटसह दिसते.

1. टॅब निवडा: **CSR-1 Submissions** किंवा **CSR-2 Reports**.
2. **Filters** वापरून **Date Range**, **Program Type**, **Head Quatres** किंवा **Status** निवडा.
3. **CSR-2 Expense Report Validation List** मध्ये **Planned Budget**, **Actual Budget** आणि **Variance** यांची तुलना करा.
4. **Validation Due** आणि **CSR - 1 Status** तपासा.

## नेहमीच्या अडचणी

- **लिस्ट बदलत नाही.** ही स्क्रीन अजून तयार होत आहे. CSR-1 **CSR-1 Management** वर आणि CSR-2 **CSR-2 List** वर व्हॅलिडेट करा.
:::

::: section rm-approval-status
title: RM मंजुरीची स्थिती
routes: /RMApprovalStatus
for: 
roles: 
designations: 
minutes: 1
source-hash: dd292461c2960406
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**RM Approval Status** स्क्रीनवर RMD ने व्हॅलिडेट करून RM कडे पाठवलेल्या CSR-1 आणि CSR-2 चा मागोवा घेतला जातो.

1. टॅब निवडा: **CSR-1 Submissions** किंवा **CSR-2 Report**.
2. कार्डे पाहा: **Sent to RM**, **Pending RM Approval**, **Approved by RM** आणि **Rejected / Correction**.
3. **CSR-1 RM Approval Status List** मध्ये **Mo Submitted**, **RMD Validated** आणि **Status** तपासा.
4. कार्यक्रमाचा तपशील आणि **Process Timeline** पाहण्यासाठी डोळ्याचा आयकॉन टॅप करा.

## नेहमीच्या अडचणी

- **लिस्ट बदलत नाही.** ही स्क्रीन अजून तयार होत आहे. CSR-1 **CSR-1 Management** वर आणि CSR-2 **CSR-2 List** वर मंजूर करा.
:::

::: section request-extension
title: स्कीमची मुदत वाढवून मागा
routes: /RequestExtension/{CSRId:int}
for: 
roles: 
designations: 
minutes: 2
source-hash: c4d4cc4e073e1bda
confidence: ok

**ही स्क्रीन कशासाठी आहे**
सेल्स ऑडिट स्कीमचा आवश्यक टर्नओव्हर वेळेत पूर्ण झाला नाही, तर **Request Extension** स्क्रीनवरून नंतरची शेवटची तारीख मागता येते. ही स्क्रीन **Period-End Review** मधून उघडा.

1. **Scheme Information (Read Only)** तपासा: **Sanction Number**, **Current Scheme Period**, **Required Turnover (TO)**, **Actual Turnover Achieved** आणि **Balance to Target**.
2. **Requested New End Date** निवडा. ती आताच्या शेवटच्या तारखेनंतरची असायला हवी.
3. **Reason for Extension** निवडा.
4. **Remarks / Explanation (Optional)** लिहा, 500 अक्षरांपर्यंत.
5. **Submit Extension Request** दाबा.
6. **Extension Request Submitted** बॉक्समध्ये **Request ID** आणि **Pending Approval** दिसते. **View My Request** किंवा **Go to Sales Audit List** दाबा.

> Tip: विनंती मंजूर होईपर्यंत स्कीमची शेवटची तारीख बदलत नाही.

## नेहमीच्या अडचणी

- **"Select the new end date."** किंवा **"The new end date must be after ..."** (नवीन शेवटची तारीख निवडा, ती आधीच्या तारखेनंतरची हवी) नंतरची तारीख निवडा.
- **"A request is already awaiting approval for this CSR."** (या CSR साठी एक विनंती आधीच मंजुरीसाठी बाकी आहे) पहिल्या विनंतीवर निर्णय होईपर्यंत थांबा.
:::

::: section welfare-application
title: वेल्फेअर अर्ज तपासा
routes: /ApprovalDetail/{Id:int}, /MOApproval/{Id:int}
for: 
roles: 
designations: 
minutes: 3
source-hash: a63f984a39e5444b
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Application Details** स्क्रीनवर डीलरचा एक वेल्फेअर (SDWA) अर्ज दिसतो, म्हणजे सध्याच्या पातळीवरचा मंजुरी देणारा तो तपासून पुढची कृती करू शकतो. ही स्क्रीन **Scheme Approval** मधून उघडा.

1. वर **Approval Workflow** तपासा. त्यात MO पासून SDWA Admin पर्यंत प्रत्येक पातळी दिसते.
2. **Applicant Information**, **Nominee & Beneficiary Information** आणि **Eligibility Information** वाचा.
3. **Uploaded Documents** खाली प्रत्येक फाइल तपासण्यासाठी **View** किंवा **Download** दाबा.
4. आधीच्या पातळ्यांचे **Previous Remarks** वाचा.
5. तुमची पाळी असेल तर **Comments** निवडा. **Recommendation** आपोआप भरले जाते. SDWA Admin **Cheque Number** आणि **Cheque Amount** पण टाकतात.
6. **Submit** दाबा. लिस्टवर परत जाण्यासाठी **Back to Approvals** दाबा.

## नेहमीच्या अडचणी

- **Submit बटण नाही.** अर्ज आता तुमच्या पातळीवर नाही.
- **लाल बॉक्समध्ये अर्ज नाकारल्याचे लिहिले आहे.** तो दुरुस्तीसाठी डीलरकडे परत पाठवला आहे, किंवा बंद केला आहे.
:::
