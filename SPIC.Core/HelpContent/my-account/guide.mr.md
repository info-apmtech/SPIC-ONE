---
module: my-account
title: माझे अकाउंट आणि डॅशबोर्ड
icon: bi-person-circle
order: 118
language: mr
category: सामान्य
note: नवीन मार्गदर्शक
meta-source-hash: a6f45f8833df3868
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 4b8137c0f3f7283d
confidence: ok

या स्क्रीन तुमच्याबद्दल आहेत: डीलर नोंदणीचा डॅशबोर्ड, तुमचे स्वतःचे प्रोफाइल, तुमची वाट पाहणाऱ्या कामांची लिस्ट, आणि SPIC शी संपर्क कसा करायचा.

- dashboard | icon: bi-speedometer2 | color: #0b6b3a
  title: डीलर नोंदणी
  text: तुमच्या भागातला प्रत्येक डीलर अर्ज त्याच्या स्थितीसह पाहा, आणि तुमची वाट पाहणाऱ्या अर्जांवर काम करा.
- profile | icon: bi-person-badge | color: #2563eb
  title: तुमचे प्रोफाइल
  text: तुमचे नाव, पद (designation), ठिकाण आणि स्थिती तपासा. डीलरना त्यांच्या नोंदणीचा तपशील पण दिसतो.
- help | icon: bi-bell | color: #d97706
  title: सूचना आणि मदत
  text: तुमच्यासाठी काय बाकी आहे ते पाहा, तुमचे रोजचे काम शोधा, आणि SPIC ला मेसेज पाठवा.
:::

::: section flow
title: डॅशबोर्डवर डीलर नोंदणी कशी पुढे जाते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 6696a0bfbcbd48ab
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. डीलरच्या डॅशबोर्ड कार्डवर तो कोणत्या टप्प्यावर पोहोचला ते दिसते.

- creator | type: lane | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: तुमचे HQ
- rm-lane | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: तुमचा रिजन
- smm-lane | type: lane | color: #7c3aed | roles: SMD, SMM
  title: SMM
  sub: तुमचे राज्य
- avp-lane | type: lane | color: #2563eb | roles: AVP, Admin
  title: AVP
  sub: अंतिम मंजुरी
- draft | lane: creator | col: 1 | icon: bi-pencil-square | color: #a16207 | next: rm | screen: dashboard
  title: भरा आणि सबमिट करा
  status: Draft
  text: अर्ज सबमिट होईपर्यंत कार्डवर **Draft** दिसते. तो पुढे भरण्यासाठी **Edit** दाबा.
- rm | lane: rm-lane | col: 2 | icon: bi-hourglass-split | color: #1d4ed8 | next: smm | return: draft | screen: dashboard
  title: RM तपासतात
  status: In RM
  text: RM **Review** दाबतात. RM ने अर्ज परत पाठवला तर कार्डवर **Rejected by RM** दिसते आणि MO तो परत बदलू शकतात.
- smm | lane: smm-lane | col: 3 | icon: bi-hourglass-split | color: #7c3aed | next: avp | return: rm | screen: dashboard
  title: SMM तपासतात
  status: In SMM
  text: SMM **Review** दाबतात. SMM ने अर्ज परत पाठवला तर **Rejected by SMM** दिसते आणि तो RM कडे परत जातो.
- avp | lane: avp-lane | col: 4 | icon: bi-patch-check | color: #4338ca | next: approved | return: smm | screen: dashboard
  title: AVP मंजूर करतात
  status: In AVP
  text: AVP अंतिम मंजुरी देतात. AVP ने अर्ज परत पाठवला तर **Rejected by AVP** दिसते आणि तो SMM कडे परत जातो.
- approved | lane: avp-lane | col: 5 | icon: bi-check-circle | color: #16a34a | screen: dashboard
  title: मंजूर
  status: Approved
  text: कार्डवर **Approved** दिसते. नवीन डीलरसाठी आधी **In AVP Office** दिसते, जोपर्यंत AVP Office कंपनीचे Dealer ID सेव्ह करत नाही.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 196e7278f9089805
confidence: ok

- draft | color: #a16207
  label: Draft
  text: अजून सबमिट केलेला नाही. MO, MDO किंवा JMDO अजून त्यात बदल करू शकतात.
- in-rm | color: #1d4ed8
  label: In RM
  text: RM च्या तपासणीची वाट पाहत आहे.
- in-smm | color: #7c3aed
  label: In SMM
  text: SMM च्या तपासणीची वाट पाहत आहे.
- in-avp | color: #4338ca
  label: In AVP
  text: AVP च्या अंतिम मंजुरीची वाट पाहत आहे.
- in-avp-office | color: #0e7490
  label: In AVP Office
  text: नवीन डीलर मंजूर झाला आहे. AVP Office ला अजून कंपनीचे Dealer ID सेव्ह करायचे आहेत.
- approved | color: #047857
  label: Approved
  text: अर्ज पूर्णपणे मंजूर झाला आहे.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: दुरुस्तीसाठी MO, MDO किंवा JMDO कडे परत पाठवला आहे.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: RM कडे परत पाठवला आहे.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: SMM कडे परत पाठवला आहे.
- action | color: #2563eb
  label: Action Required
  text: या कार्डवर काम करायची आता तुमची पाळी आहे.
- active | color: #047857
  label: Active
  text: डीलर, किंवा प्रोफाइलवर तुमचे स्वतःचे अकाउंट, चालू आहे.
- inactive | color: #c2410c
  label: Inactive
  text: डीलर, किंवा तुमचे अकाउंट, आता चालू नाही.
- terminated | color: #dc2626
  label: Terminated
  text: डीलरशिप संपली आहे.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5025b20d273029c0
confidence: ok

तुमचे कार्ड ठळक दिसते. प्रत्येक जण आपले स्वतःचे प्रोफाइल उघडू शकतो.

- creator | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - डीलर अर्ज तयार करा आणि बदला
  - RM ने परत पाठवलेले अर्ज दुरुस्त करा
- approvers | color: #0d9488 | roles: RM, RMD, SMD, SMM
  title: RM, SMM
  - Action Required असलेली कार्डे तपासा
  - डीलर लिस्ट डाउनलोड करा
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - अंतिम मंजुरी द्या
  - डीलर इम्पोर्ट करा किंवा जोडा
  - Admin आपले स्वतःचे प्रोफाइल बदलू शकतात
- dealer | color: #7c3aed | roles: Dealer
  title: डीलर
  - तुमच्या प्रोफाइलवर तुमच्या नोंदणीचा तपशील तपासा
  - तुमचे कल्याणकारी योजनेचे (welfare scheme) अर्ज पाहा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 40062c69f8f437fc
confidence: ok

- no-edit-button | popular: true
  q: मला डीलर कार्डवर Edit किंवा Review दिसत नाही
  a: ज्याची पाळी आहे त्याच व्यक्तीला ही बटणे दिसतात. अर्ज आता कोणाकडे आहे ते पाहण्यासाठी कार्डवरची स्थिती पाहा.
- wrong-profile | popular: true
  q: माझ्या प्रोफाइलवर माझे नाव, पद किंवा ठिकाण चुकीचे आहे
  a: तुम्ही ते **Profile** स्क्रीनवर बदलू शकत नाही. तुमच्या ॲडमिनिस्ट्रेटरला ते दुरुस्त करायला सांगा.
- nothing-waiting
  q: Alerts मध्ये "Nothing waiting for you" दिसते
  a: आता कोणतीही मंजुरी, बुकिंग किंवा अर्ज तुमची वाट पाहत नाही. नंतर परत तपासा.
- dealer-buttons
  q: माझ्या डीलर प्रोफाइलवर Download PDF आणि Change Password दाबून काहीच होत नाही
  a: ही दोन बटणे अजून काम करत नाहीत.
- no-directions
  q: Get Directions दाबल्यावर "Location permission is required" दिसते
  a: ॲप किंवा ब्राउझरला लोकेशनची परवानगी द्या, मग परत **Get Directions** दाबा.
:::

::: section dashboard
title: डीलर नोंदणी पाहा आणि त्यावर काम करा
routes: /Dashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-dash-kpis] | 2 > [data-help=ma-dash-filters] | 3 > [data-help=ma-dash-cards] | 4 > [data-help=ma-dash-cards] | 5 > [data-help=ma-dash-actions]
minutes: 3
source-hash: 1b2061e620bf738f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Dealer Registrations** स्क्रीनवर तुमच्या भागातला प्रत्येक डीलर अर्ज एका कार्डवर दिसतो, आणि वर आकडे असतात. बहुतेक रोलना फक्त त्यांच्या स्वतःच्या State, Region किंवा HQ चे डीलर दिसतात.

1. वरचे आकडे पाहा: **Total Registrations**, **Draft**, **In RM**, **In SMM**, **In AVP**, **Approved** आणि **Rejected / Returned**. **Status** बॉक्समध्ये चालू (active), बंद (inactive) आणि संपलेल्या (terminated) डीलरची संख्या दिसते.
2. सर्च बॉक्समध्ये डीलर कोड किंवा फर्मचे नाव टाइप करा, किंवा कंपनी, डीलरचा प्रकार, स्थिती, रिजन, मंजुरीची स्थिती किंवा क्रम (sort order) निवडा. कार्डे लगेच परत लोड होतात.
3. प्रत्येक कार्डवर फर्मचे नाव, **Region**, **HQ**, मंजुरीची स्थिती आणि डीलरची स्थिती दिसते. निळा **Action Required** टॅग म्हणजे तुमची पाळी आहे. आणखी कार्डे पाहण्यासाठी खाली **Show more** दाबा.
4. ड्राफ्ट किंवा परत आलेला अर्ज पुढे भरण्यासाठी **Edit** दाबा, किंवा तुमच्याकडे आलेला अर्ज तपासण्यासाठी **Review** दाबा. डोळ्याचा आयकॉन अर्ज वाचण्यासाठी उघडतो. **SPIC** आणि **GFL** डीलरशिपची PDF उघडतात.
5. तुमच्या फिल्टरनुसार लिस्ट Excel फाइल म्हणून डाउनलोड करण्यासाठी **Dealer Master** किंवा **Credit limit status** दाबा. MO, MDO आणि JMDO यांना **Create Existing Dealer** पण दिसते, ते डीलर अर्जाचा फॉर्म उघडते.

> Tip: **Credit Limit** कार्डवर फक्त Admin आणि MO यांनाच दिसते, तेही अंतिम मंजुरीनंतर आणि डीलरला Dealer Code मिळाल्यावर.
> Careful: Admin डस्टबिन आयकॉनने नोंदणी डिलीट करू शकतात. ती परत आणता येत नाही.

## नेहमीच्या अडचणी

- **"No dealer registrations found."** (एकही डीलर नोंदणी सापडली नाही) तुमचा सर्च किंवा फिल्टर खूप मर्यादित असतील. सर्च बॉक्स रिकामा करा आणि फिल्टर परत "All" वर ठेवा.
- **Edit किंवा Review बटण दिसत नाही.** अर्ज आता दुसऱ्या लेव्हलकडे आहे. कार्डवरची स्थिती तपासा.
- **वरचे आकडे माझ्या रिजनशी जुळत नाहीत.** कार्डांना लावलेले फिल्टरच आकड्यांनाही लागतात. फिल्टर परत "All" वर ठेवा.
:::

::: section dashboard-add-dealers
title: डीलर इम्पोर्ट करा किंवा जोडा
routes: /Dashboard
for: 
roles: Admin, AVP
designations: 
minutes: 3
source-hash: 1bfd6d4d63fce849
confidence: ok

**ही स्क्रीन कशासाठी आहे**
Admin आणि AVP आधीपासूनच्या डीलरना Excel फाइलमधून ॲपमध्ये आणू शकतात, एक डीलर हाताने जोडू शकतात, किंवा नवीन डीलरचा अर्ज सुरू करू शकतात.

1. Excel टेम्प्लेट डाउनलोड करण्यासाठी **Sample Format** दाबा.
2. प्रत्येक डीलरसाठी एक ओळ भरा: **CUSTOMER**, **CUSTOMER NAME** आणि **State**. CUSTOMER कोडच्या पहिल्या अक्षरावरून तो कोणत्या कंपनीचा कोड आहे ते कळते, उदा. SPIC साठी **D**.
3. **Import from Excel** दाबा आणि फाइल (.xlsx किंवा .xls) निवडा. "Importing, please wait..." दिसत असेपर्यंत थांबा.
4. निकाल वाचा. इम्पोर्ट न झालेल्या ओळी कारणासह **Skipped Groups** खाली दिसतात. **Close** दाबा.
5. फक्त एक डीलर जोडण्यासाठी **Add Dealer** दाबा, **CUSTOMER Code**, **CUSTOMER Name** आणि **State** टाका, आणि **Save Dealer** दाबा.
6. नवीन डीलरचा पूर्ण अर्ज सुरू करण्यासाठी **Create New Dealer** दाबा.

> Tip: Admin ला **Total Dealer List** पण दिसते. ते राज्यनिहाय डीलरची संख्या उघडते.

| फील्ड | काय भरायचे |
|---|---|
| **CUSTOMER Code** | कंपनीच्या रेकॉर्डमधला डीलरचा कोड. भरणे आवश्यक. |
| **CUSTOMER Name** | फर्मचे किंवा डीलरचे नाव. |
| **State** | डीलरचे राज्य. |

## नेहमीच्या अडचणी

- **काही ओळी वगळल्या गेल्या.** **Skipped Groups** खालचे कारण वाचा, फाइलमध्ये त्या ओळी दुरुस्त करा आणि परत इम्पोर्ट करा.
- **"Import failed."** (इम्पोर्ट झाले नाही) फाइल तीन कॉलम असलेले Excel टेम्प्लेटच आहे ना ते तपासा, मग परत प्रयत्न करा.
:::

::: section profile
title: माझे प्रोफाइल पाहा
routes: /Profile
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-prf-banner] | 2 > [data-help=ma-prf-tabs] | 3 > [data-help=ma-prf-details] | 4 > [data-help=ma-prf-details]
minutes: 2
source-hash: 854cdeee48d9aeee
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Profile** स्क्रीनवर तुमच्या स्वतःच्या अकाउंटचा तपशील दिसतो. कर्मचाऱ्यांना त्यांचा कर्मचारी म्हणून तपशील दिसतो. डीलरना त्यांचा नोंदणी तपशील दिसतो.

1. हिरवी पट्टी पाहा. त्यात तुमचे नाव, तुमचा रोल किंवा कोड आणि तुमचे पद दिसते. डीलरना **Profile Completion** पण दिसते.
2. टॅब वापरा. कर्मचाऱ्यांना एक टॅब दिसतो (**Employee**, किंवा Admin साठी **Profile**). डीलरना **Dealer Info**, **Proprietorship**, **Nominees**, **Trade Experience**, **Turnover**, **Assets** आणि **Welfare Scheme** दिसतात.
3. कार्डमध्ये तुमचा तपशील वाचा, उदा. **Designation**, **Region**, **Headquarter** आणि **Status**. **Turnover** वर **SPIC** आणि **Green Star** यांच्यात बदलून पाहा.
4. काही चुकीचे असेल तर तुमच्या ॲडमिनिस्ट्रेटरला ते दुरुस्त करायला सांगा. या स्क्रीनवर तुम्ही तुमचा तपशील बदलू शकत नाही.

> Careful: डीलर प्रोफाइलवर **Download PDF** आणि **Change Password** अजून काम करत नाहीत.

## नेहमीच्या अडचणी

- **"No employee profile information available."** (कर्मचारी प्रोफाइलची माहिती उपलब्ध नाही) तुमचा कर्मचारी रेकॉर्ड तुमच्या लॉगिनशी जोडलेला नाही. तुमच्या ॲडमिनिस्ट्रेटरला सांगा.
- **"No dealer registration data available."** (डीलर नोंदणीची माहिती उपलब्ध नाही) तुमची डीलर नोंदणी अजून तुमच्या लॉगिनशी जोडलेली नाही. तुमच्या MO शी संपर्क करा.
- **"No welfare scheme applications found."** (कल्याणकारी योजनेचा एकही अर्ज सापडला नाही) तुम्ही अजून कोणत्याही कल्याणकारी योजनेसाठी अर्ज केलेला नाही.
:::

::: section profile-edit
title: माझे प्रोफाइल बदला (Admin)
routes: /Profile
for: 
roles: Admin
designations: 
minutes: 1
source-hash: 50d60fcf9a6af1a1
confidence: ok

**ही स्क्रीन कशासाठी आहे**
फक्त Admin रोलला **Edit Profile** बटण दिसते. Admin आपले स्वतःचे नाव, ईमेल आणि पद बदलू शकतात.

1. **Admin Details** कार्डवर **Edit Profile** दाबा.
2. **Name**, **Email** किंवा **Designation** बदला. Role, User Name आणि Status इथे बदलता येत नाहीत.
3. **Save Changes** दाबा. "Profile updated successfully." (प्रोफाइल अपडेट झाले) असा मेसेज दिसतो.
4. सेव्ह न करता बंद करण्यासाठी **Cancel** दाबा.

## नेहमीच्या अडचणी

- **"Your profile could not be saved. Please try again."** (तुमचे प्रोफाइल सेव्ह झाले नाही, परत प्रयत्न करा) तुमचे नेटवर्क तपासा आणि परत **Save Changes** दाबा.
- **फॉर्म लॉक आहे आणि तुमचा तपशील लोड झाला नाही असे लिहिलेले आहे.** **Cancel** दाबा आणि परत **Edit Profile** उघडा.
:::

::: section user-details
title: तुमचे प्रोफाइल पूर्ण करा (प्रिव्ह्यू)
routes: /UserProfile
for: 
roles: 
designations: 
minutes: 1
source-hash: e4cca852b7305bb4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**User Details** स्क्रीन फक्त डिझाइनचा नमुना (preview) आहे. ती मेनूमध्ये नाही, आणि अजून काहीही सेव्ह करत नाही.

1. स्क्रीनवर **Complete Your Profile** आणि **Full Name**, **Mobile Number**, **Address** आणि **District** ही फील्ड दिसतात.
2. **Continue** बटण अजून हा तपशील सेव्ह करत नाही.

## नेहमीच्या अडचणी

- **माझा तपशील सेव्ह झाला नाही.** हे नेहमीचेच आहे. ही स्क्रीन फक्त प्रिव्ह्यू आहे. तुमचा खरा तपशील **Profile** वर आहे.
:::

::: section alerts
title: माझ्यासाठी काय बाकी आहे ते पाहा
routes: /Alerts
for: 
roles: 
designations: 
minutes: 1
source-hash: b3694f1c480ea2a4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Alerts** स्क्रीनवर अशा रांगा (queues) दिसतात ज्यांवर तुम्हाला काम करावे लागू शकते. फोन किंवा टॅबलेटवर ॲपच्या वरच्या घंटीच्या आयकॉनने ही स्क्रीन उघडा. तुमचा रोल वापरू शकतो त्याच लिंक यात दिसतात.

1. **Approvals waiting** खाली एक रांग टॅप करा, उदा. **Scheme Approval** किंवा **Dealer Application Review**.
2. **Bookings and applications** खाली **My Bookings** किंवा **Welfare Schemes** टॅप करा.
3. त्या स्क्रीनवरचे काम करा, मग इथे परत या.

> Tip: पुश नोटिफिकेशन ॲप स्टोअरवरच्या रिलीजसोबत येतील. तोपर्यंत ही स्क्रीन तपासत राहा.

## नेहमीच्या अडचणी

- **"Nothing waiting for you".** (तुमच्यासाठी काहीही बाकी नाही) तुमच्या रोलसाठी इथे मंजुरी, बुकिंग किंवा अर्जाची एकही स्क्रीन नाही.
- **रांग उघडते पण रिकामी असते.** तुमच्या रोलसाठी ही लिंक नेहमी दिसते. याचा अर्थ काही बाकी आहे असा नाही.
:::

::: section activities
title: माझे रोजचे काम उघडा
routes: /Activities
for: 
roles: 
designations: 
minutes: 1
source-hash: 3155933a5c612823
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Activities** स्क्रीनवर तुमचे फील्डचे काम, मंजुऱ्या, गेस्ट हाऊस बुकिंग आणि रिपोर्ट एकाच ठिकाणी दिसतात. फोनवर MO कर्मचाऱ्यांना ही स्क्रीन खालच्या टॅब बारमध्ये मिळते.

1. **Field activities** खाली, तुमच्याकडे असतील तर **Sample Collection** किंवा **Consignment History** टॅप करा.
2. **Pending on you** खाली **Scheme Approval** किंवा **Pending Acknowledgement** सारखी रांग टॅप करा.
3. **Guest house** खाली **Book a room** किंवा **My Bookings** टॅप करा.
4. **Reports** खाली **Reports Center** टॅप करा.

> Tip: तुमचा रोल त्यातली किमान एक लिंक वापरू शकत असेल तरच तो भाग दिसतो.

## नेहमीच्या अडचणी

- **मला फक्त "More of the Sales Activity System is on its way" दिसते.** तुमच्या रोलसाठी अजून फील्डच्या कामाची एकही स्क्रीन नाही. त्या नंतर जोडल्या जातील.
:::

::: section contact-us
title: SPIC शी संपर्क करा
routes: /ContactUs
for: 
roles: 
designations: 
minutes: 2
source-hash: 6825b4b8292fb086
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Contact Us** स्क्रीनवर SPIC ऑफिसचा पत्ता, ईमेल आणि फोन नंबर, नकाशा, आणि तुमचा प्रश्न पाठवण्यासाठी एक फॉर्म दिसतो.

1. तीन कार्डांवर ऑफिसचा पत्ता, ईमेल आणि फोन नंबर वाचा. ऑफिसची वेळ सोमवार ते शुक्रवार, सकाळी 9:30 ते संध्याकाळी 6:00 आहे.
2. तुमच्या नकाशाच्या ॲपमध्ये ऑफिस पाहण्यासाठी **Open in Maps** दाबा.
3. तुम्ही जिथे आहात तिथून रस्ता, अंतर आणि प्रवासाला लागणारा वेळ पाहण्यासाठी **Get Directions** दाबा.
4. **Send Us a Message** खाली **Full Name**, **Email Address**, **Enquiry Type**, **Subject** आणि **Message** भरा.
5. **Send Feedback** दाबा. "Thank you! Your enquiry has been sent." (धन्यवाद! तुमची चौकशी पाठवली आहे) असा मेसेज दिसतो.

> Careful: **Send Email**, **Call Office** आणि **WhatsApp Us** अजून काम करत नाहीत. कार्डांवरचा ईमेल किंवा फोन नंबर वापरा.

| फील्ड | काय भरायचे |
|---|---|
| **Enquiry Type** | **Welfare Schemes**, **Room Bookings** किंवा **General Enquiry**. |
| **Email Address** | ज्यावर SPIC तुम्हाला उत्तर देऊ शकेल असा ईमेल. |
| **Message** | तुमचा प्रश्न थोडक्यात, काही ओळींमध्ये. |

## नेहमीच्या अडचणी

- **"Please fill in every field before sending."** (पाठवण्याआधी प्रत्येक फील्ड भरा) पाचही फील्ड भरणे आवश्यक आहे.
- **"Please enter a valid email address."** (बरोबर ईमेल पत्ता टाका) ईमेल तपासा, उदा. name@example.com.
- **"Location permission is required to calculate the distance to SPIC Office."** (SPIC ऑफिसपर्यंतचे अंतर मोजण्यासाठी लोकेशनची परवानगी लागते) ॲपला लोकेशनची परवानगी द्या, मग परत **Get Directions** दाबा.
- **"Your enquiry could not be sent."** (तुमची चौकशी पाठवता आली नाही) तुमचे नेटवर्क तपासा आणि नंतर परत प्रयत्न करा.
:::

::: section gallery
title: फोटो आणि व्हिडिओ (प्रिव्ह्यू)
routes: /Gallery, /Album, /ViewVideo
for: 
roles: 
designations: 
minutes: 1
source-hash: 00d0d57d14bc58ed
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Gallery** मध्ये SDWA कार्यक्रम, मीटिंग आणि गेस्ट हाऊसचे नमुना फोटो दिसतात. या स्क्रीन नमुना फोटो असलेला डिझाइनचा प्रिव्ह्यू आहेत, अजून खरे अल्बम नाहीत.

1. **Gallery** वर **Photos** आणि **Videos** स्क्रोल करून पाहा.
2. **Guest House Rooms** अल्बम उघडण्यासाठी फोटोवर **View Album** दाबा.
3. अल्बममध्ये **All Media**, **Photos** किंवा **Videos** टॅप करा, किंवा सर्च बॉक्समध्ये टाइप करा.
4. व्हिडिओ कार्ड टॅप करा. अजून एकही व्हिडिओ टाकलेला नसल्यामुळे व्हिडिओ स्क्रीनवर "Video will appear here" दिसते.

> Tip: **Gallery** वरचे टॅब, सर्च बॉक्स आणि क्रम लावण्याची (sort) लिस्ट अजून काम करत नाहीत. प्रत्येक **View Album** बटण तोच नमुना अल्बम उघडते.

## नेहमीच्या अडचणी

- **व्हिडिओ चालत नाही.** अजून एकही व्हिडिओ टाकलेला नाही.
:::
