---
module: admin-masters
title: प्रशासन आणि मास्टर लिस्ट
icon: bi-gear
order: 117
language: mr
category: प्रशासन
note: नवीन मार्गदर्शक
meta-source-hash: 2b15dc6f86ab1703
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 09f2a6bb7f506c56
confidence: ok

या स्क्रीनवर SPIC ONE चे बाकीचे भाग वापरतात त्या मूळ लिस्ट ठेवल्या जातात: कर्मचारी आणि त्यांचे लॉगिन, डेझिग्नेशन (प्रत्येक व्यक्ती कोणती पेजेस उघडू शकते), लोकेशन, पिके आणि प्रॉडक्ट, बँका, आणि काही सेटिंग्ज. यातल्या बहुतेक साध्या लिस्ट आहेत: नोंद जोडा, ती बदला, चालू किंवा बंद करा.

- people | icon: bi-people | color: #374151
  title: लोक आणि ॲक्सेस
  text: कर्मचाऱ्यांची नोंदणी करा, प्रत्येकाला रोल आणि लोकेशन द्या, आणि पेज ॲक्सेससाठी डेझिग्नेशन निवडा.
- masters | icon: bi-list-check | color: #0d9488
  title: मास्टर लिस्ट
  text: लोकेशन, पिके, प्रॉडक्ट, बँका, प्लांट आणि इतर स्क्रीन वापरतात त्या बाकीच्या लिस्ट.
- tools | icon: bi-sliders | color: #2563eb
  title: सेटिंग्ज आणि टूल्स
  text: GPS इंटरव्हल, CSR कट-ऑफ दिवस, ॲपच्या वापराचे आकडे (metrics), IFMS इम्पोर्ट आणि Data Explorer.
:::

::: section flow
title: नवीन कर्मचाऱ्याची सुरुवात कशी होते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: e8d287d4ff418bfb
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- admin | type: lane | color: #374151 | roles: Admin, SuperAdmin, CorporateAdmin
  title: ॲडमिनिस्ट्रेटर
  sub: Admin · SuperAdmin · CorporateAdmin
- staff | type: lane | color: #0d9488
  title: कर्मचारी
  sub: फील्ड आणि ऑफिसमधले कर्मचारी
- locations | lane: admin | col: 1 | icon: bi-geo-alt | color: #0d9488 | next: designation | screen: location-master
  title: लोकेशन जोडा
  status: Active
  text: **Location Master** मध्ये कर्मचारी जिथे काम करणार तो झोन, राज्य, रिजन आणि हेडक्वार्टर जोडा.
- designation | lane: admin | col: 2 | icon: bi-shield-lock | color: #7c3aed | next: register | screen: designation
  title: पेज ॲक्सेस निवडा
  status: Active
  text: **Designation Master** मध्ये डेझिग्नेशन तयार करा आणि त्याला कोणत्या पेजवर **View**, **Entry**, **Update** किंवा **Delete** करता येईल ते टिक करा.
- register | lane: admin | col: 3 | icon: bi-person-plus | color: #2563eb | next: signin | screen: register-employee
  title: कर्मचाऱ्याची नोंदणी करा
  status: Active
  text: रोल, डेझिग्नेशन आणि लोकेशन निवडा, मग लॉगिनचे तपशील टाका आणि **Save Employee** दाबा.
- signin | lane: staff | col: 4 | icon: bi-box-arrow-in-right | color: #16a34a
  title: लॉगिन करा आणि काम करा
  text: कर्मचारी यूजर नेम आणि पासवर्डने लॉगिन करतो. मेन्यूमध्ये फक्त डेझिग्नेशनने परवानगी दिलेली पेजेसच दिसतात.
- upload-stopped | lane: admin | col: 3 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: register | to: locations | screen: employee-list
  title: अपलोड थांबले
  text: फाइलमधला झोन, राज्य, रिजन किंवा HQ अस्तित्वात नसेल तर कर्मचाऱ्यांचा बल्क अपलोड थांबतो. तो **Location Master** मध्ये जोडा आणि तीच फाइल परत अपलोड करा.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: d29ecb092c153613
confidence: ok

- active | color: #16a34a
  label: Active
  text: नोंद वापरात आहे आणि इतर स्क्रीनच्या लिस्टमध्ये दिसते.
- inactive | color: #64748b
  label: Inactive
  text: नोंद ठेवलेली आहे पण बंद केलेली आहे. नवीन नोंदी करताना ती दिसत नाही.
- open | color: #dc2626
  label: Open
  text: फक्त Metrics मध्ये. एररचा असा गट, जो अजून कोणीही सोडवला (resolved) म्हणून मार्क केलेला नाही.
- resolved | color: #16a34a
  label: Resolved
  text: फक्त Metrics मध्ये. कोणीतरी हा एररचा गट दुरुस्त झाला म्हणून मार्क केला.
- pending | color: #2563eb
  label: Pending
  text: फक्त IFMS इम्पोर्टमध्ये. रन सुरू होण्याची वाट पाहत आहे.
- running | color: #2563eb
  label: Running
  text: फक्त IFMS इम्पोर्टमध्ये. डाउनलोड आणि इम्पोर्ट आत्ता चालू आहे.
- succeeded | color: #16a34a
  label: Succeeded
  text: फक्त IFMS इम्पोर्टमध्ये. सगळे रिपोर्ट इम्पोर्ट झाले.
- partial | color: #d97706
  label: PartiallySucceeded
  text: फक्त IFMS इम्पोर्टमध्ये. काही रिपोर्ट इम्पोर्ट झाले आणि काही झाले नाहीत.
- failed | color: #dc2626
  label: Failed
  text: फक्त IFMS इम्पोर्टमध्ये. रन किंवा रिपोर्ट चालला नाही. लाल मेसेज वाचा.
- skipped | color: #dc2626
  label: Skipped
  text: फक्त IFMS इम्पोर्टमध्ये. रिपोर्ट चालवला गेला नाही.
- disabled | color: #64748b
  label: Disabled
  text: फक्त IFMS Logins मध्ये. ऑटोमेशन हे लॉगिन वापरत नाही.
- no-password | color: #dc2626
  label: No password set
  text: फक्त IFMS Logins मध्ये. पुढच्या रनआधी पासवर्ड टाका.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: fb58f5d6b0867738
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल किंवा डेझिग्नेशन परवानगी देते त्याच स्क्रीन तुम्हाला दिसतात.

- admin | color: #374151 | roles: Admin, SuperAdmin
  title: Admin आणि SuperAdmin
  - या मॉड्युलमधली प्रत्येक स्क्रीन उघडा, फक्त Admin साठी Data Explorer सोडून
  - कर्मचाऱ्यांची नोंदणी करा आणि त्यांचे डेझिग्नेशन निवडा
  - मास्टर लिस्ट नेहमी अपडेट ठेवा
  - IFMS इम्पोर्ट आणि metrics वर लक्ष ठेवा
- superadmin | color: #7c3aed | roles: SuperAdmin
  title: फक्त SuperAdmin
  - Data Explorer वापरा
  - आधीपासून असलेल्या कर्मचाऱ्यांचा रिजन आणि HQ Excel मधून अपडेट करा
- corporate | color: #2563eb | roles: CorporateAdmin
  title: CorporateAdmin
  - Metrics स्क्रीन पाहा
  - तुमचे डेझिग्नेशन देत असेल तर इथल्या इतर स्क्रीन उघडा
  - तुम्ही अधिकृत (authorized) व्यक्ती असाल तरच Data Explorer वापरा
- others | color: #0d9488
  title: इतर कर्मचारी
  - तुमचे डेझिग्नेशन देत असेल तरच इथली स्क्रीन दिसते
  - तुमचे डेझिग्नेशन परवानगी देईल तसे तुम्ही फक्त पाहू शकता, किंवा जोडू, बदलू किंवा डिलीटही करू शकता
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 6e458e262a2cfdf7
confidence: ok

- no-pages | popular: true
  q: नवीन कर्मचारी लॉगिन करतो पण त्याला लागणारी पेजेस दिसत नाहीत
  a: पेज ॲक्सेस डेझिग्नेशनमधून मिळतो. **Designation Master** उघडा, कर्मचाऱ्याचे डेझिग्नेशन एडिट करा आणि त्या पेजेससाठी **View** टिक करा (गरज असेल तर **Entry** किंवा **Update** पण). मग **Employee Registration** मध्ये कर्मचाऱ्याला तेच डेझिग्नेशन दिले आहे का ते तपासा.
- upload-missing | popular: true
  q: कर्मचारी अपलोड करताना "Missing master data — nothing was uploaded" असे येते
  a: तुमच्या Excel फाइलमधला एखादा झोन, राज्य, रिजन किंवा HQ अजून अस्तित्वात नाही. तो **Location Master** मध्ये जोडा, मग तीच फाइल परत निवडा.
- cannot-delete
  q: मी लोकेशन किंवा पीक डिलीट करू शकत नाही. "Cannot Delete" असे येते
  a: ही नोंद इतर नोंदींमध्ये वापरली आहे, त्या नोंदी बॉक्समध्ये दिसतात. आधी त्या काढा किंवा बदला, किंवा फक्त ही नोंद **Inactive** करा.
- no-save
  q: मला मास्टर स्क्रीन दिसते पण Save बटण नाही
  a: तुमचे डेझिग्नेशन तुम्हाला फक्त **View** देते. त्या पेजसाठी **Entry** (जोडण्यासाठी) किंवा **Update** (बदलण्यासाठी) द्यायला ॲडमिनिस्ट्रेटरला सांगा.
- ifms-password
  q: IFMS Logins स्क्रीनवर "A password needs changing" असे येते
  a: IFMS पोर्टलचा पासवर्ड दर 80 दिवसांनी संपतो (expire होतो). आधी पोर्टलवर तो बदला, मग नवीन पासवर्ड **New password** मध्ये पेस्ट करा आणि **Update password** दाबा.
:::

::: section employee-list
title: कर्मचारी पाहा आणि सांभाळा
routes: /EmployeeManagement
for: 
roles: 
designations: 
minutes: 2
source-hash: 4cbca19ed0bdeb5d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Employee Management** स्क्रीनवर प्रत्येक कर्मचाऱ्याचे लॉगिन, त्याचा रोल आणि राज्य दिसते. इथून तुम्ही नवीन कर्मचाऱ्यांची नोंदणी करू शकता, अनेक कर्मचारी एकदम अपलोड करू शकता, किंवा लॉगिन एडिट किंवा डिलीट करू शकता.

1. **Employee List** मध्ये कर्मचारी शोधण्यासाठी सर्च बॉक्स, **All Role** आणि **All States** वापरा.
2. कॉलम वाचा: **Employee ID**, **Name**, **User Name**, **Password**, **Role** आणि **State**.
3. कर्मचारी एडिट करण्यासाठी पेन्सिल दाबा. त्याचे तपशील भरलेला कर्मचाऱ्याचा फॉर्म उघडतो.
4. लॉगिन डिलीट करण्यासाठी डस्टबिन दाबा, मग **Delete this role?** बॉक्समध्ये **Delete** दाबा.
5. एक कर्मचारी जोडण्यासाठी **Register Employee** दाबा.
6. अनेक कर्मचारी जोडायचे असतील तर Excel फाइल मिळवण्यासाठी **Sample Format** दाबा, ती भरा, **Bulk Upload** दाबा आणि फाइल निवडा. किती ओळी **Inserted** आणि **Skipped** झाल्या ते बॉक्समध्ये दिसते.
7. लिस्ट बाहेर काढण्यासाठी टेबलच्या वरची **Copy**, **CSV**, **Excel**, **PDF** किंवा **Print** बटणे वापरा.

> Careful: डिलीट केलेले लॉगिन परत आणता येत नाही.
> Tip: अपलोड फाइलमध्ये फोन नंबरच लॉगिनचा पासवर्ड बनतो. व्यक्तीला पूर्ण ॲक्सेस हवा असेल तरच **Designation** रिकामे ठेवा.

## बल्क अपलोडचे कॉलम

| फील्ड | काय भरायचे |
|---|---|
| **EmployeeID**, **EmpName**, **UserName** | कर्मचाऱ्याचा ID, नाव आणि लॉगिनचे नाव. |
| **Permission** | रोल. चालणारे रोल: AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO. |
| **Zone**, **State**, **Region**, **HQ** | AVP ला झोन लागतो. SMD आणि SMM ला राज्य लागते. RM आणि RMD ला राज्य आणि रिजन लागतात. MDO, MO आणि JMDO ला राज्य, रिजन आणि HQ लागतात. हे आधीपासून असले पाहिजेत. |
| **PhoneNumber**, **EmailID** | संपर्काचे तपशील. फोन नंबरच पासवर्ड असतो. |
| **Designation** | एखाद्या चालू (active) डेझिग्नेशनच्या नावाशी जुळले पाहिजे. |

## नेहमीच्या अडचणी

- **"Missing master data — nothing was uploaded".** (मास्टर डेटा नाही — काहीही अपलोड झाले नाही) राहिलेला झोन, राज्य, रिजन किंवा HQ **Location Master** मध्ये जोडा, मग तीच फाइल परत निवडा.
- **"Issues found:"** (अडचणी सापडल्या) काही ओळी सोडून दिल्या गेल्या. प्रत्येक गटाखालचे कारण वाचा, त्या ओळी दुरुस्त करा आणि परत अपलोड करा.
- **Register Employee किंवा Bulk Upload बटण नाही.** तुमचे डेझिग्नेशन **Employee Registration** वर **Entry** देत नाही.
- **Update Region / HQ.** हे बटण फक्त SuperAdmin ला दिसते. ते Excel फाइलमधून, आधी प्रिव्ह्यू दाखवून, आधीपासून असलेल्या लॉगिनचा फक्त रिजन आणि HQ बदलते.
:::

::: section register-employee
title: कर्मचाऱ्याची नोंदणी करा किंवा तपशील बदला
routes: /EmployeeRegistration
for: 
roles: 
designations: 
tour: 1 > [data-help=am-emp-role] | 2 > [data-help=am-emp-location] | 3 > [data-help=am-emp-login] | 4 > [data-help=am-emp-details] | 5 > [data-help=am-emp-save]
minutes: 3
source-hash: e7b641f98153cac9
confidence: ok

**ही स्क्रीन कशासाठी आहे**
एका कर्मचाऱ्यासाठी लॉगिन तयार करायला, त्याला रोल आणि लोकेशन द्यायला, आणि तो कोणती पेजेस उघडू शकेल हे ठरवणारे डेझिग्नेशन निवडायला **Employee Registration** स्क्रीन वापरा. कर्मचारी एडिट करताना स्क्रीनचे नाव **Edit Employee** असते.

1. **Role & Authority** खाली **Role** आणि **Designation** निवडा.
2. **Role Specification** खाली रोलला लागणारे लोकेशन निवडा, उदा. **State**, **Regions** किंवा **Headquarters**.
3. **Login Details** खाली **User Name** आणि **Password** टाका. **Status** स्विच चालू (active) किंवा बंद (inactive) करा.
4. **Employee Details** खाली **Employee Name**, **Employee ID**, **Personal Phone Number**, **Official Phone Number** आणि **Email ID** टाका.
5. **Save Employee** दाबा (एडिट करताना **Update Employee**). **Cancel** दाबल्यास सेव्ह न करता परत जाता येते.

> Tip: लोकेशनची फील्ड रोलनुसार बदलतात. Director सारख्या काही रोलना लोकेशन लागत नाही.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Role** | व्यक्तीचा कामाचा रोल. भरणे आवश्यक. |
| **Designation** | पेज ॲक्सेस ठरवते. CommonRole साठी भरणे आवश्यक. |
| **Zone** | AVP साठी. |
| **State** | SMD, SMM, MDAdmin, MDHOD, RM, RMD, MO, MDO आणि JMDO साठी. |
| **Regions** | एक किंवा अनेक रिजन, RM आणि RMD साठी. |
| **Region** आणि **Headquarters** | एक रिजन आणि एक किंवा अनेक हेडक्वार्टर, MO, MDO आणि JMDO साठी. |
| **User Name** | लॉगिनचे नाव. स्पेस नको. भरणे आवश्यक. |
| **Password** | नवीन कर्मचाऱ्यासाठी भरणे आवश्यक. |
| **Personal Phone Number** | 10 अंक. भरणे आवश्यक. |
| **Email ID** | बरोबर ईमेल पत्ता. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Please correct the highlighted fields and try again."** (ठळक केलेली फील्ड दुरुस्त करून परत प्रयत्न करा) प्रत्येक फील्डखालचा लाल मेसेज वाचा आणि ते फील्ड दुरुस्त करा.
- **"Username must not contain spaces or invisible characters."** (यूजर नेममध्ये स्पेस किंवा न दिसणारी अक्षरे नकोत) यूजर नेम स्पेसशिवाय परत टाइप करा.
- **"A user or employee with these details already exists."** (हे तपशील असलेला यूजर किंवा कर्मचारी आधीच आहे) दुसऱ्या लॉगिनचे **User Name**, **Employee ID** किंवा **Email ID** हेच आहे.
- **"Selected Region does not belong to the selected State."** (निवडलेला रिजन निवडलेल्या राज्यातला नाही) आधी राज्य निवडा, मग त्यातला रिजन निवडा.
- **Save Employee बटण नाही.** तुमचे डेझिग्नेशन **Employee Registration** वर **Entry** (किंवा एडिटसाठी **Update**) देत नाही.
:::

::: section designation
title: डेझिग्नेशन आणि त्याचा पेज ॲक्सेस तयार करा
routes: /Designation
for: 
roles: 
designations: 
tour: 1 > [data-help=am-des-name] | 2 > [data-help=am-des-pagesearch] | 3 > [data-help=am-des-grid] | 4 > [data-help=am-des-save] | 5 > [data-help=am-des-search]
minutes: 3
source-hash: e6dc32d0910d8413
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Designation Master** स्क्रीन ठरवते की व्यक्ती कोणती पेजेस उघडू शकते आणि तिथे काय करू शकते. प्रत्येक कर्मचाऱ्याला एक डेझिग्नेशन मिळते.

1. **Add New Designation** खाली **Designation Name** टाइप करा. **Active** किंवा **Inactive** करण्यासाठी स्विच टॅप करा.
2. **Role Access** मध्ये पेज पटकन शोधण्यासाठी **Search PageName...** मध्ये टाइप करा.
3. प्रत्येक पेजसाठी **View**, **Entry**, **Update** किंवा **Delete** टिक करा. **ALL** त्या पेजचे चारही बॉक्स टिक करते. **Select All** सगळी पेजेस टिक करते.
4. **Save** दाबा (एडिट करताना **Update**). **Cancel** फॉर्म रिकामा करते.
5. **Existing Designations** मध्ये लिस्टमध्ये शोधा. पेन्सिल डेझिग्नेशन एडिट करते. तिरकी रेघ असलेले वर्तुळ ते बंद (deactivate) करते आणि बाण ते परत चालू करतो.

> Tip: **View** ने व्यक्ती पेज उघडू शकते. **Entry** ने नोंदी जोडू शकते, **Update** ने बदलू शकते, आणि **Delete** ने काढू शकते.
> Careful: बंद केलेले डेझिग्नेशन ज्या यूजरकडे आहे त्यांच्याकडे राहते, पण चालू लिस्टमध्ये दिसत नाही.

## नेहमीच्या अडचणी

- **"Select at least one permission".** (किमान एक परवानगी निवडा) सेव्ह करण्याआधी किमान एक बॉक्स टिक करा.
- **"Designation '...' already exists".** (हे डेझिग्नेशन आधीच आहे) वेगळे नाव वापरा, किंवा आधीचे डेझिग्नेशन एडिट करा.
- **मला Add New Designation फॉर्म दिसत नाही.** तुमचे डेझिग्नेशन तुम्हाला फक्त **View** देते.
:::

::: section location-master
title: लोकेशनची लिस्ट सांभाळा
routes: /LocationMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 1a434ac83feeecb3
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Location Master** स्क्रीनवर झोन, राज्ये, जिल्हे, तालुके (sub-districts), रिजन आणि हेडक्वार्टर असतात. कर्मचारी, डीलर आणि रिपोर्ट या लिस्ट वापरतात.

1. टॅब निवडा: **Zones**, **States**, **Districts**, **Sub-Districts**, **Regions** किंवा **Headquarters**.
2. फॉर्म भरा, उदा. **Zone Name**, **State Name** आणि **Zone**, किंवा **Headquarter Name** आणि **Region**.
3. स्विच **Active** किंवा **Inactive** करा आणि **Save** दाबा. **Cancel** फॉर्म रिकामा करते.
4. अनेक नोंदी जोडायच्या असतील तर **Sample Format** दाबा, Excel फाइल भरा, **Bulk Upload** दाबा आणि फाइल निवडा.
5. टेबलमध्ये शोधा किंवा स्थिती निवडा. स्थिती बदलण्यासाठी ती टॅप करा, एडिटसाठी पेन्सिल, किंवा डिलीटसाठी डस्टबिन टॅप करा.

> Tip: या क्रमाने जोडा: आधी झोन, मग राज्य, मग रिजन, मग हेडक्वार्टर. जिल्हा राज्याखाली असतो आणि तालुका जिल्ह्याखाली.
> Careful: डिलीट केलेले परत आणता येत नाही. इतर नोंदींमध्ये वापरलेले लोकेशन डिलीट करता येत नाही.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Zone Name**, **Zone Code**, **Zone Color Code** | झोन आणि नकाशावर त्याचा रंग. नाव भरणे आवश्यक. |
| **State Name**, **State Code**, **Zone** | राज्य आणि ते ज्या झोनमध्ये आहे तो झोन. नाव आणि झोन भरणे आवश्यक. |
| **District Name**, **State** | दोन्ही भरणे आवश्यक. |
| **Sub-District Name**, **District** | दोन्ही भरणे आवश्यक. **State** फक्त जिल्हा शोधायला मदत करते. |
| **Region Name**, **Region Code**, **State** | नाव आणि राज्य भरणे आवश्यक. |
| **Headquarter Name**, **Region** | दोन्ही भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Zone is required."** किंवा **"Region is required."** (झोन / रिजन भरणे आवश्यक) लिस्टमधून वरचे (parent) लोकेशन निवडा.
- **"Cannot Delete".** (डिलीट करता येत नाही) ही नोंद दुसरीकडे वापरली आहे. आधी त्या नोंदी बदला, किंवा ही नोंद **Inactive** करा.
- **बल्क अपलोडमध्ये "Skipped Groups:" किंवा "Issues:" दिसते.** कारणे वाचा, ती जपून ठेवण्यासाठी **Copy** दाबा, फाइल दुरुस्त करा आणि परत अपलोड करा.
:::

::: section agriculture-master
title: पिके आणि प्रॉडक्टच्या लिस्ट सांभाळा
routes: /Agriculture
for: 
roles: 
designations: 
minutes: 3
source-hash: 9d6cd17408623739
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Agriculture & Product Master** स्क्रीनवर पिके, पिकांचे प्रकार, पिकांचे टप्पे, स्पर्धक कंपन्या, सेक्टर, युनिट, कॅटेगरी, प्रॉडक्ट गट आणि प्रॉडक्ट असतात. इतर स्क्रीन या लिस्ट वापरतात.

1. टॅब निवडा: **Crops**, **Crop Types**, **Crop Stages**, **Competitors**, **Sector**, **Units**, **Categories**, **Product Groups** किंवा **Products**.
2. फॉर्म भरा. **Crop Types** आणि **Crop Stages** साठी आधी **Crop** निवडा. **Products** साठी **Category** निवडा, आणि माहीत असतील तर **Product Group** आणि **Rate Per Unit (RPU)** टाका.
3. स्विच **Active** किंवा **Inactive** करा आणि **Save** दाबा. **Cancel** फॉर्म रिकामा करते.
4. अनेक नोंदी जोडायच्या असतील तर **Sample Format** दाबा, ती फाइल भरा, **Bulk Upload** दाबा आणि फाइल निवडा. जोडलेल्या, अपडेट झालेल्या आणि नाकारलेल्या ओळी बॉक्समध्ये दिसतात.
5. टेबलमध्ये शोधा किंवा स्थिती निवडा. स्थिती बदलण्यासाठी ती टॅप करा, एडिटसाठी पेन्सिल, किंवा डिलीटसाठी डस्टबिन टॅप करा.

> Tip: आधी युनिट तयार करा, मग कॅटेगरी, आणि मग प्रॉडक्ट.
> Careful: डिलीट केलेले परत आणता येत नाही. दुसरीकडे वापरलेली नोंद डिलीट करता येत नाही.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Category Name** आणि **Unit** | दोन्ही भरणे आवश्यक. दुसरा स्विच कॅटेगरीला **Speciality** किंवा **Regular** म्हणून मार्क करतो. |
| **Crop** | पिकाचा प्रकार किंवा पिकाचा टप्पा ज्या पिकाचा आहे ते पीक. भरणे आवश्यक. |
| **Product Name** आणि **Category** | दोन्ही भरणे आवश्यक. एका कॅटेगरीत एका प्रॉडक्टचे नाव एकदाच वापरता येते. |
| **Rate Per Unit (RPU)** | प्रॉडक्टचा दर. भरणे गरजेचे नाही. |

## नेहमीच्या अडचणी

- **"Please select an active crop."** (चालू असलेले पीक निवडा) हे पीक बंद केलेले आहे. आधी **Crops** टॅबमध्ये ते चालू करा.
- **"... already exists".** (आधीच आहे) या नावाची नोंद आधीच आहे. त्याऐवजी ती एडिट करा.
- **Sample Format किंवा Bulk Upload बटण नाही.** ही बटणे **Crop Types** आणि **Crop Stages** वर नसतात, किंवा तुमच्या डेझिग्नेशनला **Entry** नाही.
:::

::: section financial-master
title: बँका आणि आर्थिक वर्षांच्या लिस्ट सांभाळा
routes: /Financial
for: 
roles: 
designations: 
minutes: 2
source-hash: 149843a1d578c0f3
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Financial Master** स्क्रीनवर इतर फॉर्ममध्ये वापरल्या जाणाऱ्या बँका आणि आर्थिक वर्षे असतात.

1. **Bank** किंवा **Financial Year** टॅब निवडा.
2. बँकेसाठी **Bank Name** आणि **IFSC Prefix** टाका, आणि तुमच्याकडे असेल तर **Bank Logo** जोडा. आर्थिक वर्षासाठी **Financial Year Name**, **Start Date** आणि **End Date** टाका.
3. स्विच **Active** किंवा **Inactive** करा आणि **Save** दाबा. **Cancel** फॉर्म रिकामा करते.
4. बँका कार्डच्या रूपात दिसतात, आणि खाली **Show more** असते. आर्थिक वर्षे पानांमध्ये विभागलेल्या टेबलमध्ये दिसतात.
5. स्थिती बदलण्यासाठी ती टॅप करा, एडिटसाठी पेन्सिल, किंवा डिलीटसाठी डस्टबिन टॅप करा.

> Careful: डिलीट केलेले परत आणता येत नाही.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **IFSC Prefix** | IFSC कोडची फक्त पहिली 4 अक्षरे, उदा. HDFC किंवा SBIN. भरणे आवश्यक. |
| **Bank Logo** | 512 KB पेक्षा लहान फोटो. भरणे गरजेचे नाही. |

## नेहमीच्या अडचणी

- **"IFSC Prefix is required."** (IFSC Prefix भरणे आवश्यक) IFSC कोडची पहिली 4 अक्षरे टाका.
- **"Logo file must be under 512 KB."** (लोगोची फाइल 512 KB पेक्षा लहान असावी) लहान फोटो निवडा.
- **Bulk Upload दाबून काहीच होत नाही.** या स्क्रीनवर बल्क अपलोड अजून तयार नाही. नोंदी एक एक करून जोडा.
:::

::: section relationship-master
title: नात्यांची लिस्ट सांभाळा
routes: /Relationship
for: 
roles: 
designations: 
minutes: 1
source-hash: dfed886a1ac19a35
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Relationship Master** स्क्रीनवर नात्यांची लिस्ट असते, उदा. डीलरच्या तपशिलात नाव दिलेल्या व्यक्तीचे नाते.

1. **Add New Relationship** खाली **Relationship Name** टाइप करा.
2. स्विच **Active** किंवा **Inactive** करा आणि **Save** दाबा. **Cancel** फॉर्म रिकामा करते.
3. **Existing Relationships** मध्ये शोधा किंवा स्थिती निवडा.
4. स्थिती बदलण्यासाठी ती टॅप करा, एडिटसाठी पेन्सिल, किंवा डिलीटसाठी डस्टबिन टॅप करा.

> Careful: डिलीट केलेले परत आणता येत नाही.

## नेहमीच्या अडचणी

- **"Relationship name is required."** (नात्याचे नाव भरणे आवश्यक) आधी नाव टाइप करा.
- **Bulk Upload दाबून काहीच होत नाही.** या स्क्रीनवर बल्क अपलोड अजून तयार नाही. नोंदी एक एक करून जोडा.
:::

::: section plant-dealer-nature
title: प्लांट आणि डीलर नेचरच्या लिस्ट सांभाळा
routes: /PlantDealerNatureMaster
for: 
roles: 
designations: 
minutes: 1
source-hash: 99cbbff4aa8ad460
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Plant & Dealer Nature Master** स्क्रीनवर Excel अपलोड आणि डीलरच्या नोंदींमध्ये वापरले जाणारे प्लांट आणि डीलर नेचर (डीलरचा प्रकार) असतात.

1. **Plants** किंवा **Dealer Natures** टॅब निवडा.
2. **Plant Name** किंवा **Dealer Nature Name** टाइप करा.
3. स्विच **Active** किंवा **Inactive** करा आणि **Save** दाबा. **Cancel** फॉर्म रिकामा करते.
4. टेबलमध्ये शोधा किंवा स्थिती निवडा. स्थिती बदलण्यासाठी ती टॅप करा, एडिटसाठी पेन्सिल, किंवा डिलीटसाठी डस्टबिन टॅप करा.

> Careful: डिलीट केलेले परत आणता येत नाही.

## नेहमीच्या अडचणी

- **"Plant name is required."** (प्लांटचे नाव भरणे आवश्यक) आधी नाव टाइप करा.
- **मी स्थिती टॅप केल्यावर ती बदलत नाही.** तुमच्या डेझिग्नेशनला या पेजवर **Update** नाही.
:::

::: section expense-master
title: खर्चांची लिस्ट सांभाळा
routes: /ExpenseInformation
for: 
roles: 
designations: 
minutes: 1
source-hash: cbd7b43eecb288ef
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Expense Information Master** स्क्रीनवर खर्चांच्या नावांची लिस्ट असते.

1. **Add New Expense Information** खाली **Expense Name** टाइप करा.
2. स्विच **Active** किंवा **Inactive** करा आणि **Save** दाबा. **Cancel** फॉर्म रिकामा करते.
3. **Existing Expense Informations** मध्ये शोधा किंवा स्थिती निवडा.
4. स्थिती बदलण्यासाठी ती टॅप करा, एडिटसाठी पेन्सिल, किंवा डिलीटसाठी डस्टबिन टॅप करा.

> Careful: डिलीट केलेले परत आणता येत नाही.

## नेहमीच्या अडचणी

- **"Expense name is required."** (खर्चाचे नाव भरणे आवश्यक) आधी नाव टाइप करा.
:::

::: section gps-settings
title: GPS ट्रॅकिंगचा इंटरव्हल सेट करा
routes: /GpsSettings
for: 
roles: 
designations: 
minutes: 1
source-hash: 2b7d216628a0423f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**GPS Settings** स्क्रीनवर ठरवले जाते की लाइव्ह ट्रॅकिंगच्या वेळी JMDO मोबाइल ॲप किती वेळाने GPS लोकेशन नोंदवते.

1. **Current Saved Interval**, **System Default** (20 s) आणि **Allowed Range** (1 ते 100 सेकंद) पाहा.
2. स्लायडर सरकवा, किंवा बॉक्समध्ये सेकंदांची संख्या टाइप करा.
3. **Save Settings** दाबा.
4. परत 20 सेकंदांवर जायचे असेल तर **Reset to Default (20s)** दाबा आणि मग **Save Settings** दाबा.

> Tip: लहान संख्या ठेवली तर लोकेशन जास्त वेळा नोंदवले जाते पण बॅटरी जास्त खर्च होते. मोठी संख्या ठेवली तर बॅटरी वाचते.

## नेहमीच्या अडचणी

- **"Tracking interval must be an integer between 1 and 100 seconds."** (इंटरव्हल 1 ते 100 सेकंदांमधली पूर्ण संख्या असावी) 1 ते 100 मधली पूर्ण संख्या टाका.
- **Save Settings बटण नाही.** तुमच्या डेझिग्नेशनला **GPS Settings** वर **Update** नाही.
:::

::: section csr-exceeding-day
title: CSR कट-ऑफ दिवस सेट करा
routes: /CSRExceedingDaySetting
for: 
roles: 
designations: 
minutes: 2
source-hash: 94dea6d5ea8fe4d7
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**CSR Exceeding Day Setting** स्क्रीनवर महिन्याचा तो दिवस ठरवला जातो ज्यानंतर CSR-1 आणि CSR-2 ला मंजुरीचा जास्त लांबचा मार्ग लागतो. एका वेळी एकच सेटिंग चालू असते.

1. **CSR1 Day** आणि **CSR2 Day** टाका (1 ते 31).
2. **Status** स्विच **Active** करा आणि **Save** दाबा. **Cancel** फॉर्म रिकामा करते.
3. **CSR Exceeding Day Settings** मध्ये सगळी सेटिंग्ज पाहा. स्थिती बदलण्यासाठी ती टॅप करा, एडिटसाठी पेन्सिल, किंवा डिलीटसाठी डस्टबिन टॅप करा.

> Tip: महिन्याच्या **CSR1 Day** नंतर तयार केलेल्या CSR-1 ला, किंवा **CSR2 Day** नंतर सबमिट केलेल्या CSR-2 ला, जास्त लांबची मंजुरी लागते.
> Careful: चालू सेटिंग सेव्ह केल्यावर आधीचे चालू सेटिंग बंद होते.

## नेहमीच्या अडचणी

- **"CSR1 Day must be between 1 and 31."** (CSR1 Day 1 ते 31 च्या दरम्यान असावा) 1 ते 31 मधला दिवस टाका.
- **"You do not have permission to save this setting."** (हे सेटिंग सेव्ह करण्याची तुम्हाला परवानगी नाही) तुमच्या डेझिग्नेशनला या पेजवर **Entry** किंवा **Update** नाही.
:::

::: section metrics
title: ॲप कसे वापरले जाते ते पाहा
routes: /Metrics
for: 
roles: 
designations: 
minutes: 2
source-hash: 42e4f35d4ca253b9
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Metrics** स्क्रीनवर दिसते की SPIC ONE कोण वापरत आहे, कोणत्या ॲपवर, कोणती पेजेस आणि सर्व्हिसेस वापरल्या जातात, आणि काय चुकले. इथे फक्त आकडे दिसतात; एरर मार्क करण्याशिवाय इथून काहीही बदलले जात नाही.

1. कालावधी निवडा: **7 days**, **30 days** किंवा **90 days**. एखादे ॲप निवडा, किंवा **All apps** तसेच ठेवा.
2. वरची लाइव्ह ओळ वाचा: गेल्या 15 मिनिटांत ऑनलाइन असलेले लोक, आणि गेल्या एका तासातल्या रिक्वेस्ट आणि एरर.
3. एक टॅब उघडा: **Overview**, **Users**, **Pages**, **API** किंवा **Errors**.
4. **Errors** वर शोधण्यासाठी, किंवा **Source** आणि **Status** (**Open**, **Resolved** किंवा **All**) निवडण्यासाठी **Filters** उघडा.
5. एररचे तपशील पाहण्यासाठी ती टॅप करा. ती दुरुस्त झाल्यावर **Mark resolved** दाबा, किंवा परत उघडण्यासाठी **Reopen** दाबा.

## नेहमीच्या अडचणी

- **"You do not have access to Metrics".** (तुम्हाला Metrics चा ॲक्सेस नाही) तुमच्या डेझिग्नेशनमध्ये Metrics पेज जोडायला ॲडमिनिस्ट्रेटरला सांगा.
- **"The metrics could not be loaded".** (metrics लोड होऊ शकले नाहीत) कनेक्शन तपासा आणि **Try again** दाबा.
- **"Live numbers unavailable".** (लाइव्ह आकडे मिळाले नाहीत) लाइव्ह आकडे वाचता आले नाहीत. ते दर मिनिटाला रिफ्रेश होतात.
:::

::: section ifms-auto-import
title: रात्रीचा IFMS इम्पोर्ट पाहा
routes: /IfmsAutoImport
for: 
roles: 
designations: 
minutes: 2
source-hash: cc12211b4d92d31d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**IFMS Auto Import** स्क्रीनवर IFMS पोर्टलवरून (dbtfert.nic.in) रोज रात्री होणारे रिपोर्टचे डाउनलोड आणि त्यांचा SPIC ONE मध्ये होणारा इम्पोर्ट दिसतो. हे 04:05 ला आपोआप चालते.

1. शेवटच्या रनची कार्डे पाहा: **Last run**, **Reports**, **Rows added**, **Rows updated** आणि **Login**.
2. प्रत्येक रनवर त्याची तारीख, किती रिपोर्ट इम्पोर्ट झाले, आणि **Succeeded** किंवा **Failed** सारखी स्थिती दिसते.
3. प्रत्येक रिपोर्ट त्याच्या **Status**, **Rows**, **New**, **Updated** आणि **Skipped** सह पाहण्यासाठी रन टॅप करा.
4. पिवळा CAPTCHA बॉक्स दिसला तर दिसणारी अक्षरे टाइप करा आणि **Send** दाबा. मग डाउनलोड पुढे चालू होते.
5. लगेच रन सुरू करण्यासाठी **Run now** दाबा, किंवा नवीन निकाल पाहण्यासाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **"No runs yet".** (अजून एकही रन नाही) पहिला रन 04:05 ला सुरू होतो, किंवा **Run now** दाबा.
- **"Could not queue a run."** (रन सुरू करता आला नाही) मेसेज वाचा आणि थोड्या वेळाने परत प्रयत्न करा.
- **CAPTCHA वर "expired" असे येते.** नवीन चित्र आपोआप येते. नवीन अक्षरे टाइप करा.
- **Login कार्डवर "failed" असे येते.** IFMS पासवर्ड संपला (expire झाला) असेल. **IFMS Logins** तपासा.
:::

::: section ifms-logins
title: IFMS पोर्टलचे लॉगिन सांभाळा
routes: /IfmsLogins
for: 
roles: 
designations: 
minutes: 2
source-hash: 7e0020b6083c5518
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**IFMS Logins** स्क्रीनवर प्रत्येक कंपनीचे पोर्टल लॉगिन ठेवले जाते, जे रात्रीचा IFMS इम्पोर्ट वापरतो. पासवर्ड सुरक्षित ठेवले जातात आणि परत कधीही दाखवले जात नाहीत.

1. प्रत्येक कंपनीच्या कार्डवर **Disabled**, **No password set** असा टॅग, किंवा पासवर्ड संपायला किती दिवस राहिले ते दिसते.
2. **Password set**, **Expires**, **Last login** आणि **Result** वाचा.
3. पोर्टलवर पासवर्ड बदलला की नवीन पासवर्ड **New password** मध्ये पेस्ट करा, **OTP goes to** तपासा, आणि **Update password** दाबा.
4. लॉगिन वापरणे थांबवण्यासाठी **Disable** दाबा, किंवा परत वापरण्यासाठी **Enable** दाबा.
5. कंपनी जोडण्यासाठी **Add a login** खाली **Key**, **Company**, **Username** आणि **Password** भरा आणि **Add** दाबा.

> Careful: पोर्टलचा पासवर्ड दर 80 दिवसांनी संपतो. आधी पोर्टलवर बदला, मग इथे, नाहीतर रात्रीचा रन फेल होतो.

## नेहमीच्या अडचणी

- **"Enter the new password first."** (आधी नवीन पासवर्ड टाका) **Update password** दाबण्याआधी नवीन पासवर्ड पेस्ट करा.
- **"Key, username and password are all needed."** (Key, यूजर नेम आणि पासवर्ड तिन्ही लागतात) लॉगिन जोडण्यासाठी तिन्ही भरा.
:::

::: section data-explorer
title: डेटा थेट पाहा आणि दुरुस्त करा
routes: /DataExplorer
for: 
roles: SuperAdmin, CorporateAdmin
designations: 
minutes: 3
source-hash: fb55adf5f6eab06e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Data Explorer** स्क्रीनवर अधिकृत व्यक्ती डेटाबेसच्या कोणत्याही टेबलमधल्या ओळी पाहू, फिल्टर करू, एक्सपोर्ट करू आणि दुरुस्त करू शकते. प्रत्येक बदल तुमच्या नावासह ऑडिट लॉगमध्ये लिहिला जातो. यासाठी वेगळा पासवर्ड लागतो.

1. Data Explorer चा पासवर्ड टाका आणि **Unlock** दाबा.
2. **Tables** वर शोधा आणि टेबल निवडा. **Filter (any column contains)** मध्ये टाइप करा आणि Enter दाबा. क्रमाने लावण्यासाठी (sort) कॉलमचे नाव टॅप करा.
3. ओळ एडिट करण्यासाठी ती टॅप करा, मग **Save** किंवा **Delete** दाबा. नवीन ओळ जोडण्यासाठी **New row** आणि **Insert** दाबा. **Export CSV** ओळी डाउनलोड करते.
4. **Query** वर एक SQL स्टेटमेंट टाइप करा आणि **Run** दाबा. बदल असेल तर आधी त्याची चाचणी (dry run) दिसते; तो लागू करण्यासाठी **Confirm: change ... row(s)** दाबा.
5. अलीकडचे बदल पाहण्यासाठी **Audit log** उघडा. काम झाल्यावर **Lock** दाबा.

> Careful: इथले बदल थेट लाइव्ह डेटामध्ये जातात आणि परत करता येत नाहीत. शक्य असेल तेव्हा नेहमीच्या स्क्रीनच वापरा.

## नेहमीच्या अडचणी

- **"Data Explorer is available to SuperAdmin or the authorized CorporateAdmin only."** (Data Explorer फक्त SuperAdmin किंवा अधिकृत CorporateAdmin साठी आहे) तुमच्या लॉगिनला ही स्क्रीन वापरण्याची परवानगी नाही.
- **Save किंवा Delete होत नाही.** की (key) नेमक्या एका ओळीशी जुळली पाहिजे.
:::
