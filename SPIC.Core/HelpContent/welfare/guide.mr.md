---
module: welfare
title: कल्याण योजना (SDWA)
icon: bi-heart
order: 111
language: mr
category: डीलर
note: नवीन मार्गदर्शक
meta-source-hash: 95f84553d50218f7
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: fa298dd497c945c5
confidence: ok

डीलर ॲपमधून SDWA कल्याण योजनांसाठी अर्ज करू शकतात, जसे लग्नाची भेट, वैद्यकीय मदत किंवा शिक्षणासाठी मदत. अधिकारी प्रत्येक अर्ज टप्प्याटप्प्याने तपासतात, आणि अर्ज कुठे आहे ते डीलरला कधीही दिसते.

- apply | icon: bi-pencil-square | color: #be123c
  title: डीलर अर्ज करतात
  text: पात्रता तपासा, योजना निवडा, फॉर्म भरा आणि कागदपत्रे अपलोड करा.
- approve | icon: bi-shield-check | color: #d97706
  title: पाच तपासण्या
  text: MO, RM, SM, SDWA आणि SDWA Admin एकामागून एक अर्ज तपासतात.
- benefit | icon: bi-cash-coin | color: #16a34a
  title: अंतिम मंजुरी
  text: SDWA Admin अंतिम मंजुरी देतात आणि चेकचा तपशील नोंदवतात.
:::

::: section flow
title: अर्ज कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 942cc75f4b898ba1
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- dealer | type: lane | color: #be123c | roles: Dealer
  title: डीलर
  sub: अर्ज करतात आणि मागोवा घेतात
- mo | type: lane | color: #d97706 | roles: MO
  title: MO
  sub: HQ तपासतात
- rm | type: lane | color: #0d9488 | roles: RM
  title: RM
  sub: रिजन तपासतात
- sm | type: lane | color: #2563eb | roles: SMM
  title: SM
  sub: राज्य तपासतात
- sdwa | type: lane | color: #7c3aed
  title: SDWA
  sub: SDWA · SDWA Admin
- choose | lane: dealer | col: 1 | icon: bi-ui-checks | color: #64748b | next: apply | screen: dashboard
  title: पात्रता तपासा
  text: डॅशबोर्डवर **Apply Welfare** खाली **Apply Now** दाबा. तुम्ही पात्र असाल तर योजना निवडा.
- apply | lane: dealer | col: 2 | icon: bi-pencil-square | color: #64748b | next: mo-check | screen: apply-scheme
  title: फॉर्म भरा आणि सबमिट करा
  status: Draft
  text: फॉर्म भरा, कागदपत्रे अपलोड करा आणि सबमिट करा. तोपर्यंत तो फक्त ड्राफ्ट असतो.
- mo-check | lane: mo | col: 3 | icon: bi-search | color: #d97706 | next: rm-check | screen: scheme-approval
  title: MO ची तपासणी
  status: Pending MO
  text: MO आपल्या HQ मधल्या डीलरचा तपशील, विक्री आणि कागदपत्रे तपासतात.
- rm-check | lane: rm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: sm-check | return: mo-check | screen: scheme-approval
  title: RM ची तपासणी
  status: Pending RM
  text: RM रिजनसाठी अर्ज तपासतात. त्यांनी नाकारला तर अर्ज MO कडे परत जातो.
- sm-check | lane: sm | col: 5 | icon: bi-hourglass-split | color: #d97706 | next: sdwa-check | return: rm-check | screen: scheme-approval
  title: SM ची तपासणी
  status: Pending SM
  text: SM राज्यासाठी अर्ज तपासतात. त्यांनी नाकारला तर अर्ज RM कडे परत जातो.
- sdwa-check | lane: sdwa | col: 6 | icon: bi-patch-check | color: #7c3aed | next: sdwaadmin | return: sm-check | screen: scheme-approval
  title: SDWA ची मंजुरी
  status: Pending SDWA
  text: SDWA अर्ज मंजूर करतात. त्यांनी नाकारला तर अर्ज SM कडे परत जातो.
- sdwaadmin | lane: sdwa | col: 7 | icon: bi-cash-coin | color: #7c3aed | next: approved | return: sdwa-check | screen: scheme-approval
  title: SDWA Admin ची अंतिम मंजुरी
  status: Pending SDWA Admin
  text: SDWA Admin अंतिम मंजुरी देतात आणि चेक नंबर आणि रक्कम टाकतात.
- approved | lane: dealer | col: 8 | icon: bi-check-circle | color: #16a34a | screen: my-applications
  title: मंजूर
  status: Approved
  text: अर्ज मंजूर झाला. डीलरला **My Applications** मध्ये **Approved** दिसते.
- returned | lane: dealer | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: mo-check | to: apply | screen: my-applications
  title: दुरुस्त करा आणि परत सबमिट करा
  status: Rejected by MO – Resubmission Required
  text: MO ने नाकारला तर अर्ज डीलरकडे परत येतो. तो दुरुस्त करा आणि परत सबमिट करा. त्याचा Application ID तोच राहतो.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8a133f3149737003
confidence: ok

- draft | color: #64748b
  label: Draft
  text: सेव्ह केले आहे पण पाठवलेले नाही.
- pending-mo | color: #d97706
  label: Pending MO
  text: तपासणीसाठी MO कडे आहे.
- pending-rm | color: #d97706
  label: Pending RM
  text: तपासणीसाठी RM कडे आहे.
- pending-sm | color: #d97706
  label: Pending SM
  text: तपासणीसाठी SM कडे आहे.
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: मंजुरीसाठी SDWA कडे आहे.
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: अंतिम मंजुरीसाठी SDWA Admin कडे आहे.
- approved | color: #16a34a
  label: Approved
  text: अंतिम मंजुरी मिळाली.
- resubmit | color: #dc2626
  label: Rejected by MO – Resubmission Required
  text: हे डीलरला दिसते. अर्ज दुरुस्त करा आणि परत सबमिट करा.
- returned | color: #ea580c
  label: Returned to Dealer
  text: हे अधिकाऱ्यांना दिसते. अर्ज दुरुस्तीसाठी डीलरकडे परत गेला आहे.
- rejected | color: #475569
  label: Rejected
  text: अर्ज बंद झाला. गरज असेल तर परत अर्ज करा.
- cancelled | color: #94a3b8
  label: Cancelled
  text: अर्ज रद्द केला गेला.
- awaiting-mo | color: #d97706
  label: Awaiting MO Review
  text: MO ची वाट पाहणाऱ्या नवीन अर्जासाठी डॅशबोर्डवरचे नाव.
- under-mo | color: #d97706
  label: Under MO Review
  text: MO कडे परत आलेल्या अर्जासाठी डॅशबोर्डवरचे नाव.
- under-rm | color: #d97706
  label: Under RM Review
  text: Pending RM साठी डॅशबोर्डवरचे नाव.
- under-smm | color: #d97706
  label: Under SMM Review
  text: Pending SM साठी डॅशबोर्डवरचे नाव.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 69fdf9c12e670358
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- dealer | color: #be123c | roles: Dealer
  title: डीलर
  - पात्रता तपासा आणि अर्ज करा
  - अर्जांचा मागोवा घ्या आणि PDF डाउनलोड करा
  - परत आलेला अर्ज दुरुस्त करून परत सबमिट करा
- mo | color: #d97706 | roles: MO
  title: MO
  - तुमच्या HQ मधले अर्ज तपासा
  - चुकीचे कागदपत्र बदला किंवा डिलीट करा
  - मंजूर करा, किंवा डीलरकडे परत पाठवा
- rm-sm | color: #0d9488 | roles: RM, SMM
  title: RM आणि SM
  - तुमच्या रिजन किंवा राज्यातले अर्ज तपासा
  - मंजूर करा, किंवा एक टप्पा मागे पाठवा
- sdwa | color: #7c3aed
  title: SDWA आणि SDWA Admin
  - सगळ्या भागांमधले अर्ज पाहा
  - शेवटच्या दोन मंजुऱ्या द्या
  - SDWA Admin चेकचा तपशील टाकतात
- director | color: #475569 | roles: Director
  title: डायरेक्टर
  - सगळे अर्ज पाहा
  - SDWA च्या मंजुरीनंतर काही योजनांसाठी हवे असल्यास रिव्ह्यू द्या
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: cd3161af8efa4aae
confidence: ok

- returned | popular: true
  q: माझ्या अर्जावर "Rejected by MO – Resubmission Required" दिसते
  a: **My Applications** उघडा, लाल बॉक्समधले कारण वाचा, **Correct & Resubmit** दाबा, तपशील किंवा कागदपत्रे दुरुस्त करा आणि **Resubmit Application** दाबा. त्याचा Application ID तोच राहतो.
- not-eligible | popular: true
  q: Welfare Eligibility विंडोमध्ये मी पात्र नाही असे दिसते
  a: विंडोमध्ये प्रत्येक नियम **Required** आणि **Your dealership** सोबत दिसतो. लाल रंगातले नियम अजून पूर्ण झालेले नाहीत. एखादा आकडा चुकीचा वाटत असेल तर तुमच्या MO शी बोला.
- seven-days
  q: मला "Invitation must be sent at least 7 days in advance" दिसते
  a: Wedding Gift, Sathabhishekam आणि Grahapravesam साठी कार्यक्रमाची तारीख आजपासून किमान 7 दिवसांनंतरची असावी.
- cheque
  q: फॉर्मवर "Cancelled Cheque / Bank Passbook is required" दिसते
  a: रद्द केलेला चेक किंवा बँक पासबुकचे पान अपलोड करा, आणि त्यावर जसे छापलेले आहे अगदी तसेच **Beneficiary Name** टाइप करा.
- where-is-it
  q: माझा अर्ज आता कोणाकडे आहे?
  a: **My Applications** मध्ये गोलांच्या ओळीत प्रत्येक टप्पा दिसतो. हिरवा म्हणजे पूर्ण झाला, आणि नारिंगी म्हणजे अर्ज आता त्या टप्प्यावर आहे.
- no-submit
  q: मी अधिकारी आहे आणि मला अर्ज मंजूर करता येत नाही
  a: अर्ज तुमच्या टप्प्यावर असेल आणि तुमच्या भागातला असेल तरच तुम्ही त्यावर काम करू शकता. लिस्टमध्ये त्याची स्थिती तपासा.
:::

::: section dashboard
title: डीलर डॅशबोर्ड वापरा
routes: /SDWADashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: 44c37577fe4705d0
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Dashboard** हे डीलरसाठी कल्याण योजनांचे अर्ज आणि गेस्ट हाउस बुकिंगचे मुख्य पान आहे. इथे नवीन अर्ज सुरू करा आणि तुमचे अर्ज कुठपर्यंत आले ते पाहा.

1. वर तुमचे नाव, **Dealer Code**, **Region** आणि **Profile Completion** तपासा.
2. **Quick Actions** खाली **Apply Welfare** कार्डवर **Apply Now** दाबा. ॲप आधी तुमची पात्रता तपासते.
3. **Welfare Eligibility** विंडोमध्ये तुमचे **State**, **State Group**, **Dealer Type** आणि **Eligibility Criteria** चा प्रत्येक नियम दिसतो. तुम्ही पात्र असाल तर **Proceed to Apply** दाबा. नसाल तर **Close** दाबा.
4. **Application Status Overview** पाहा: **Pending Applications**, **Approved Applications**, **Rejected Applications** आणि **Processing Applications**.
5. **Recent Applications** खाली एखादा अर्ज उघडायला **View Details** दाबा, किंवा **My Applications** उघडायला **View All** दाबा.
6. ताजे बदल पाहण्यासाठी **Notifications** आणि **Recent Activity** वाचा.

> Tip: या स्क्रीनवरचे **Book Room** आणि **My Bookings** गेस्ट हाउससाठी आहेत. त्यासाठी Guest house मार्गदर्शक पाहा.

## नेहमीच्या अडचणी

- **"Unable to verify eligibility. Please try again later."** (पात्रता तपासता आली नाही. नंतर परत प्रयत्न करा.) तुमचे इंटरनेट तपासा आणि परत **Apply Now** दाबा.
- **"No applications yet".** (अजून एकही अर्ज नाही) तुम्ही अजून अर्ज केलेला नाही. **Apply Welfare** दाबा.
- **मला Apply Welfare दिसत नाही.** SDWA Admin वापरकर्ते या स्क्रीनवरून अर्ज करत नाहीत.
:::

::: section choose-scheme
title: कल्याण योजना निवडा
routes: /WelfareSchemes
for: 
roles: 
designations: 
minutes: 1
source-hash: 71f07f60f43b7d0c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Apply for Welfare Schemes** स्क्रीनवर प्रत्येक SDWA योजना एका कार्डवर दिसते. तुम्हाला हवी ती योजना निवडा.

1. कार्डे पाहा: **Medical Assistance**, **Wedding Gift**, **Grahapravesam**, **Educational Assistance**, **Sathabhishekam**, **Death Relief Assistance**, **Merit Award** आणि **Distinction Award**.
2. प्रत्येक कार्डवर **Documents** (किती कागदपत्रे लागतात) आणि **Eligibility** (कोण अर्ज करू शकतो) तपासा.
3. तुम्हाला हव्या असलेल्या योजनेवर **Apply Now** दाबा. त्या योजनेचा अर्जाचा फॉर्म उघडतो.

> Careful: **Distinction Award** फक्त तमिळनाडू (TN) मधल्या डीलर आणि कर्मचाऱ्यांसाठी आहे.

## नेहमीच्या अडचणी

- **मला ही स्क्रीन उघडता येत नाही.** तुमच्या लॉगिनला कल्याण योजनांचा ॲक्सेस नसेल. तुमच्या MO किंवा admin ला विचारा.
:::

::: section grahapravesam
title: गृहप्रवेश (Grahapravesam) योजनेबद्दल वाचा
routes: /GrahapravesamScheme
for: 
roles: 
designations: 
minutes: 1
source-hash: a035ff18db9c4a2f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
अर्ज करण्याआधी **Grahapravesam** स्क्रीनवर गृहप्रवेशाच्या भेटीची योजना समजावून सांगितली आहे.

1. वरचे कार्ड आणि **About the Scheme** वाचा.
2. **Eligibility Criteria** आणि **Required Documents** तपासा.
3. **Frequently Asked Questions** आणि **Important Notes** वाचा.
4. फॉर्म उघडायला **Apply Now** दाबा, किंवा दुसरी योजना निवडायला **Back to Schemes** दाबा.

## नेहमीच्या अडचणी

- **फॉर्म माझी गृहप्रवेशाची तारीख घेत नाही.** फॉर्मला आजपासून किमान 7 दिवसांनंतरची तारीख लागते.
:::

::: section apply-scheme
title: कल्याण योजनेसाठी अर्ज करा
routes: /ApplyWelfareScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-dealer] | 2 > [data-help=wf-quantity] | 3 > [data-help=wf-details] | 4 > [data-help=wf-documents] | 5 > [data-help=wf-declaration] | 6 > [data-help=wf-actions]
minutes: 5
source-hash: 55566f3677387604
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुमचा अर्ज भरून पाठवण्यासाठी **Apply for** स्क्रीन वापरा (उदा. **Apply for Wedding**). प्रत्येक योजनेसाठी फॉर्म थोडा वेगळा असतो.

1. तुमचे डीलर कार्ड (**Location Info** आणि **Personal Info**) तपासा आणि योजनेचा **Eligibility Conditions** बॉक्स वाचा.
2. **Quantity Lifted** खाली **Quantity Lifted during Last Year (MT)** निवडा. 3 वर्षांची एकूण संख्या आपोआप भरते. मग **Beneficiary Details** उघडते: **Select Beneficiary Group** निवडा आणि नॉमिनी आणि लाभार्थी भरा.
3. योजनेचा भाग भरा, उदा. **Wedding Details** मध्ये **Wedding Date**, किंवा **Medical Assistance Details** मध्ये **Treatment / Medical Condition**.
4. **Document Upload** खाली प्रत्येक कागदपत्रासाठी **Click to upload** टॅप करा. **Cancelled Cheque / Bank Passbook** साठी त्यावर छापलेले **Beneficiary Name** पण टाइप करा.
5. **Declaration** खाली मजकूर वाचा आणि बॉक्सवर टिक करा.
6. अर्ज ठेवायचा असेल तर **Save as Draft** दाबा, किंवा **Preview Application** दाबा. तो तपासा, **Submit Application** दाबा, मग **Confirm** दाबा. **Application ID** लिहून ठेवा आणि **Track Application** दाबा.

> Careful: **Confirm** दाबल्यावर अर्ज MO कडे जातो, आणि तो तुमच्याकडे परत पाठवला तरच तुम्ही तो बदलू शकता.
> Tip: सेव्ह केलेल्या ड्राफ्टला एक रेफरन्स नंबर मिळतो आणि तो **My Applications** मध्ये **Drafts** खाली मोजला जातो.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Quantity Lifted during Last Year (MT)** | मागच्या वर्षी तुम्ही उचललेल्या मालाची रेंज. भरणे आवश्यक. |
| **Select Beneficiary Group** | **Direct Dealer**, **Sub Dealer** किंवा **Approved Employee**. भरणे आवश्यक. |
| **Nominee Name** आणि **Nominee Relationship** | तुमच्या डीलर रेकॉर्डमधला नॉमिनी. भरणे आवश्यक. |
| **Select Beneficiary** | कुटुंबातली एक व्यक्ती, किंवा **Add new beneficiary**. डायरेक्ट डीलरसाठी भरणे आवश्यक. |
| कार्यक्रमाची तारीख | Wedding, Sathabhishekam आणि Grahapravesam: आजपासून किमान 7 दिवसांनंतरची. |
| **Date of Death** आणि **Legal Heir Relation** | फक्त Death Relief साठी. नाते **Others** असेल तर प्रतिज्ञापत्र (affidavit) अपलोड करा. |
| कागदपत्रे | PDF, JPG किंवा PNG, प्रत्येकी 5 MB पर्यंत. |

## नेहमीच्या अडचणी

- **"Please fix the following errors:"** (खालील चुका दुरुस्त करा) बटणांच्या वरची लिस्ट वाचा आणि प्रत्येक चूक दुरुस्त करा.
- **फाइल अपलोड केल्यावर काहीच होत नाही.** फाइल PDF, JPG किंवा PNG असावी आणि 5 MB पेक्षा लहान असावी.
- **"Minimum 80% marks is required for the Merit Award."** (Merit Award साठी किमान 80% गुण आवश्यक) Merit Award साठी किमान 80% गुण लागतात. Distinction Award साठी 80% पेक्षा जास्त गुण लागतात आणि एकही विषय राहिलेला (arrears) नसावा.
- **"A minimum average annual sales of 50,000 MT..."** (वर्षाला सरासरी किमान 50,000 MT विक्री...) सब डीलरसाठी अर्ज करायला तुमची विक्री खूप कमी आहे (कर्मचाऱ्यासाठी 5,000 MT).
:::

::: section my-applications
title: माझ्या अर्जांचा मागोवा घ्या
routes: /WellfareApplication
for: 
roles: 
designations: 
minutes: 2
source-hash: 62f3f5bcf69937c0
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Applications** स्क्रीनवर तुम्ही केलेला प्रत्येक कल्याण अर्ज दिसतो, त्याची स्थिती आणि तो कोणकोणत्या टप्प्यांतून गेला ते पण दिसते.

1. वरच्या टाइल्स पाहा: **Total**, **Pending**, **Approved**, **Rejected**, **Drafts** आणि **Cancelled**.
2. **Latest First** किंवा **Oldest First** निवडा, एखादी स्थिती निवडा, किंवा **Search by Application ID or Scheme** मध्ये टाइप करा.
3. प्रत्येक कार्डवर योजना, Application ID आणि स्थिती पाहा. गोलांच्या ओळीत **Dealer**, **MO**, **RM**, **SM/SMM**, **SDWA**, **SDWA Admin** आणि **Final** दिसतात. हिरवा म्हणजे पूर्ण झाला, नारिंगी म्हणजे अर्ज आता तिथे आहे.
4. सगळी माहिती पाहण्यासाठी **View Details** दाबा, किंवा प्रत सेव्ह करण्यासाठी **Download PDF** दाबा.
5. लाल बॉक्समध्ये **Rejected by** दिसले तर **Reason** वाचा आणि **Correct & Resubmit** दाबा. फॉर्म दुरुस्त करा, **Resubmit Application** दाबा, मग **Confirm Resubmission** दाबा.
6. नवीन अर्ज सुरू करण्यासाठी **Apply for a scheme** दाबा.

> Tip: परत सबमिट केलेल्या अर्जाचा Application ID तोच राहतो आणि तो परत MO पासून सुरू होतो.

## नेहमीच्या अडचणी

- **"No matching applications".** (जुळणारा एकही अर्ज नाही) **Clear filters** दाबा.
- **Download PDF वर "Failed – Tap to Retry" दिसते.** तुमचे इंटरनेट तपासा आणि परत टॅप करा.
- **Correct & Resubmit बटण दिसत नाही.** MO ने तुमच्याकडे परत पाठवलेला अर्जच परत सबमिट करता येतो.
:::

::: section scheme-approval
title: कल्याण अर्ज तपासा आणि मंजूर करा
routes: /SchemeApproval
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-ap-cards] | 2 > [data-help=wf-ap-tabs] | 3 > [data-help=wf-ap-search] | 4 > [data-help=wf-ap-table] | 5 > [data-help=wf-ap-table] | 6 > [data-help=wf-ap-pages]
minutes: 3
source-hash: 06b3fdb4303eee15
confidence: ok

**ही स्क्रीन कशासाठी आहे**
MO, RM, SM, SDWA आणि SDWA Admin आपल्या टप्प्यावर वाट पाहणारे कल्याण अर्ज तपासण्यासाठी **Approval** स्क्रीन वापरतात. MO ला HQ दिसतो, RM ला रिजन, SM ला राज्य. SDWA आणि SDWA Admin यांना सगळे भाग दिसतात.

1. कार्डे पाहा: **Total Applications**, तुमच्या टप्प्याचे **Pending** (उदा. **Pending MO**), **Validated by MO**, **Rejected** आणि **Completed**.
2. एक टॅब निवडा, जसे **Pending**, तुमच्या टप्प्याचे **Approved by**, **Validated by MO**, **Recommended by RM**, **Approved by SM**, **Rejected** किंवा **Completed**.
3. **Search dealer / application...** मध्ये टाइप करा किंवा **All Schemes** मध्ये योजना निवडा.
4. टेबलमध्ये **Status** आणि **Approval Flow (MO → RM → SM → SDWA → SDWA Admin)** पाहा. हिरवी टिक म्हणजे पूर्ण, निळा गोल म्हणजे अर्ज आता त्या टप्प्यावर आहे, लाल फुली म्हणजे नाकारला.
5. अर्ज उघडण्यासाठी डोळ्याचा आयकॉन टॅप करा. तपशील आणि कागदपत्रे तपासा, **Comments** निवडा, आणि **Recommendation** आपोआप भरते. **Submit** दाबा.
6. आणखी ओळी पाहण्यासाठी **Items Per Page** आणि बाण वापरा.

> Careful: नाकारणारी कमेंट अर्ज एक टप्पा मागे पाठवते. MO ने नाकारला तर अर्ज डीलरकडे परत जातो.
> Tip: SDWA Admin ला **Cheque Number** आणि **Cheque Amount** पण टाकावे लागतात, आणि ते **Cheque Image** जोडू शकतात.

## मंजुरीचा क्रम

- आधी MO, मग RM, मग SM, मग SDWA, आणि शेवटी SDWA Admin अंतिम मंजुरी देतात.
- Admin, Corporate Admin आणि Director लिस्ट उघडू शकतात पण मंजुरी देत नाहीत. SDWA ने मंजूर केल्यावर Director हे Death Relief, Educational Assistance, Merit Award आणि Medical Assistance साठी हवे असल्यास रिव्ह्यू जोडू शकतात.

## नेहमीच्या अडचणी

- **"No applications found for this view."** (या व्ह्यूमध्ये एकही अर्ज नाही) या टॅबमध्ये काहीही बाकी नाही. दुसरा टॅब पाहा.
- **"Unable to load the approval queue. Please try again."** (मंजुरीची लिस्ट उघडता आली नाही. परत प्रयत्न करा.) **Retry** दाबा.
- **Submit बटण दिसत नाही.** अर्ज आता तुमच्या टप्प्यावर नाही.
- **"Please select a comment."** (कमेंट निवडा) **Submit** दाबण्याआधी कमेंट निवडा.
:::

::: section company-master
title: SDWA कंपन्या सांभाळा
routes: /SdwaCompanyMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: de3ec8b1ed8fc6f6
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**SDWA Company Details** स्क्रीनवर बिलिंगसाठी गेस्ट हाउसशी जोडलेल्या कंपन्यांची लिस्ट ठेवली जाते.

1. कार्डे पाहा: **Total Companies**, **Active** आणि **Inactive**.
2. **Add New Company** खाली **Company Name**, **Short Name / Code** आणि **GSTIN** टाका.
3. **Guest House(s)** मध्ये प्रत्येक गेस्ट हाउसचे नाव टाइप करा आणि टॅप करा. नाव काढायला त्यावरची छोटी x टॅप करा.
4. स्विच **Active** किंवा **Inactive** वर ठेवा, मग **Save** दाबा. **Cancel** दाबल्यावर फॉर्म रिकामा होतो.
5. **Existing Companies** मध्ये सर्च करा किंवा **All Status**, **Active** किंवा **Inactive** निवडा. बदलण्यासाठी पेन्सिल वापरा, स्थिती बदलण्यासाठी स्थितीवर टॅप करा, किंवा डिलीट करण्यासाठी डस्टबिन वापरा.

> Careful: डिलीट केलेले परत आणता येत नाही. बुकिंगशी जोडलेली कंपनी डिलीट करता येत नाही.

## नेहमीच्या अडचणी

- **"GSTIN is required."** किंवा **"Company Name is required."** (GSTIN / कंपनीचे नाव भरणे आवश्यक) दोन्ही फील्ड भरा.
- **"At least one Guest House must be selected."** (किमान एक गेस्ट हाउस निवडा) किमान एक गेस्ट हाउस निवडा.
- **मला Add New Company फॉर्म दिसत नाही.** तुमचा लॉगिन फक्त ही लिस्ट पाहू शकतो.
:::
