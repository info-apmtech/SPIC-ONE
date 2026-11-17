---
module: sub-dealers
title: सब डीलर
icon: bi-people
order: 103
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
source-hash: 0365bef89cdeeedd
confidence: ok

प्रत्येक सब डीलरची बरोबर नोंद ठेवा: दुकान कुठे आहे, त्यांच्याशी संपर्क कसा करायचा, आणि त्यांचे GST, PAN आणि mFMS तपशील. याच मॉड्युलमध्ये सब डीलरचे आणि डीलरच्या कर्मचाऱ्यांचे कल्याण योजनेचे लाभार्थी पण ठेवले जातात.

- import | icon: bi-file-earmark-excel | color: #8b5cf6
  title: Admin मास्टर अपलोड करतात
  text: Admin प्रत्येक State, Region आणि HQ साठी सब डीलर कोडची मंजूर Excel लिस्ट इम्पोर्ट करतात.
- maintain | icon: bi-pencil-square | color: #0d9488
  title: MO तपशील भरतात
  text: MO आपल्या HQ चा प्रत्येक सब डीलर निवडतात आणि ठिकाण, संपर्क, GST, PAN आणि mFMS भरतात.
- welfare | icon: bi-heart | color: #d97706
  title: कल्याण योजनेचे लाभार्थी
  text: सब डीलर आणि कर्मचाऱ्यांच्या लाभार्थ्यांना आधी SM आणि मग AVP मंजुरी देतात.
:::

::: section flow
title: सब डीलरची नोंद कशी पुढे जाते, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 0b02b6ac4a67732f
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- admin | type: lane | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin आणि ऑफिस
  sub: मुख्य कार्यालय
- field | type: lane | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: तुमचे HQ
- sm | type: lane | color: #d97706 | roles: SMD, SMM
  title: SM
  sub: SMD · SMM
- avp | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: अंतिम मंजुरी
- import | lane: admin | col: 1 | icon: bi-file-earmark-excel | color: #8b5cf6 | next: maintain | screen: import-master
  title: मास्टर इम्पोर्ट करा
  text: Admin मंजूर Excel लिस्ट अपलोड करतात. कोणी तपशील सेव्ह करेपर्यंत नवीन सब डीलर लिस्टमध्ये दिसत नाहीत.
- maintain | lane: field | col: 2 | icon: bi-pencil-square | color: #16a34a | next: beneficiary | screen: maintain-sub-dealer
  title: तपशील भरा
  status: Active
  text: MO सब डीलर निवडतात, ठिकाण, संपर्क, GST, PAN आणि mFMS भरतात, आणि **Submit** दाबतात. आता तो **Sub Dealer Master** लिस्टमध्ये दिसतो.
- beneficiary | lane: admin | col: 3 | icon: bi-person-plus | color: #d97706 | next: sm-approve | screen: beneficiary-add
  title: लाभार्थी जोडा
  status: Pending
  text: **Sub Dealer & Employee** वर ऑफिसचे कर्मचारी सब डीलरचा किंवा डीलरच्या कर्मचाऱ्याचा कल्याण योजनेचा लाभार्थी जोडतात.
- sm-approve | lane: sm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: avp-approve | screen: beneficiary-approve
  title: SM ची मंजुरी
  status: Pending
  text: SMD किंवा SMM नोंद तपासतात आणि ती मंजूर करतात किंवा नाकारतात.
- avp-approve | lane: avp | col: 5 | icon: bi-patch-check | color: #2563eb | screen: beneficiary-approve
  title: AVP ची मंजुरी
  status: Approved
  text: AVP अंतिम मंजुरी देतात. आता कल्याण योजनेसाठी अर्ज करताना डीलर या व्यक्तीला निवडू शकतो.
- rejected | lane: sm | col: 5 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: sm-approve | screen: beneficiary-approve
  title: नाकारले
  status: SM Rejected
  text: SM किंवा AVP ने नोंद नाकारली तर ती तिथेच थांबते आणि डीलरला निवडायला मिळत नाही.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9462538c97a0b6f7
confidence: ok

- active | color: #16a34a
  label: Active
  text: सब डीलर किंवा लाभार्थी वापरात आहे. Active सब डीलरसाठी * खूण असलेले प्रत्येक फील्ड भरणे आवश्यक आहे.
- inactive | color: #64748b
  label: Inactive
  text: आता वापरात नाही. Inactive सब डीलरसाठी फक्त State, Region, HQ आणि Firm Name लागतात.
- pending | color: #d97706
  label: Pending
  text: लाभार्थी SM किंवा AVP च्या मंजुरीची वाट पाहत आहे.
- approved | color: #2563eb
  label: Approved
  text: या लेव्हलवर मंजूर झाले. Overall कॉलममध्ये याचा अर्थ SM आणि AVP दोघांनी मंजुरी दिली.
- rejected | color: #dc2626
  label: Rejected
  text: या लेव्हलवर नाकारले. हे SM Status किंवा AVP Status कॉलममध्ये दिसते.
- sm-rejected | color: #dc2626
  label: SM Rejected
  text: SM ने लाभार्थी नाकारला. तो AVP कडे जात नाही.
- avp-rejected | color: #b91c1c
  label: AVP Rejected
  text: SM ने मंजूर केले, पण AVP ने लाभार्थी नाकारला.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e89557ba6534118
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- admin | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - Excel मधून सब डीलर मास्टर इम्पोर्ट करा
  - कोणत्याही सब डीलरचे तपशील भरा किंवा दुरुस्त करा
  - सब डीलर आणि कर्मचाऱ्यांचे लाभार्थी जोडा
- field | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - तुमच्या HQ च्या सब डीलरचे तपशील भरा आणि अपडेट करा
  - सेव्ह केलेले तपशील चुकीचे असतील तर डिलीट करा
- rm | color: #64748b | roles: RM, RMD
  title: RM, RMD
  - तुमच्या रिजनचे सब डीलर पाहा
  - लिस्ट Excel मध्ये एक्सपोर्ट करा
- sm | color: #d97706 | roles: SMD, SMM
  title: SMD, SMM
  - तुमच्या राज्याचे सब डीलर पाहा
  - लाभार्थ्यांना पहिली मंजुरी द्या किंवा नाकारा
- avp | color: #2563eb | roles: AVP, Director
  title: AVP आणि Director
  - तुमच्या भागातल्या सब डीलरचे तपशील भरा किंवा अपडेट करा
  - AVP लाभार्थ्यांना अंतिम मंजुरी देतात
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 3f822e638d6491b2
confidence: ok

- not-in-dropdown | popular: true
  q: Select Imported Sub Dealer मध्ये मला सब डीलर सापडत नाही
  a: फॉर्मवर फक्त Admin ने तुमच्या State, Region आणि HQ साठी इम्पोर्ट केलेले सब डीलरच दिसतात. फॉर्मवर नवीन कोड तयार करता येत नाही. तो मास्टरमध्ये जोडायला Admin ला सांगा.
- list-empty | popular: true
  q: Admin ने सब डीलर अपलोड केले, पण Sub Dealer Master लिस्ट रिकामी आहे
  a: लिस्टमध्ये फक्त ज्यांचे तपशील सेव्ह झाले आहेत तेच सब डीलर दिसतात. **Maintain Sub Dealer** दाबा, सब डीलर निवडा, तपशील भरा आणि **Submit** दाबा.
- submit-grey
  q: Submit बटण राखाडी आहे
  a: आधी सब डीलर निवडा आणि त्याचे तपशील लोड होईपर्यंत थांबा. GST PDF, ठिकाण किंवा पिनकोड वाचले जात असताना पण थांबा.
- edit-grey
  q: लिस्टवरचे पेन्सिल आणि डस्टबिन आयकॉन राखाडी आहेत
  a: तुमचा रोल फक्त सब डीलर पाहू शकतो. MO, MDO, JMDO, Admin, AVP आणि Director ते बदलू शकतात.
- no-approve
  q: मी मंजुरी देणारा आहे, पण मला लाभार्थ्यावर टिक किंवा क्रॉस दिसत नाही
  a: **SM Status** **Pending** असेल तेव्हाच SM ला ते दिसतात. SM ने मंजूर केल्यावर आणि **AVP Status** **Pending** असेल तेव्हाच AVP ला ते दिसतात.
:::

::: section sub-dealer-list
title: सब डीलरची लिस्ट पाहा
routes: /SubDealerList
for: 
roles: 
designations: 
tour: 1 > [data-help=sdl-cards] | 2 > [data-help=sdl-filters] | 3 > [data-help=sdl-table] | 4 > [data-help=sdl-actions] | 5 > [data-help=sdl-summary]
minutes: 2
source-hash: eb1071bb27a63554
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sub Dealer Master** स्क्रीनवर ज्या सब डीलरचे तपशील सब डीलर फॉर्ममधून सेव्ह झाले आहेत ते दिसतात. MO ला HQ दिसते, RM ला रिजन आणि SMM किंवा SMD ला राज्य दिसते. तुम्हाला कोणता भाग दिसतो ते वरच्या नोटमध्ये लिहिलेले असते.

1. वरची कार्डे पाहा: **Total Sub Dealers**, **Active** आणि **Inactive**.
2. **Search** मध्ये कोड, नाव, मोबाइल, GST, PAN किंवा mFMS ID टाइप करा. लिस्ट कमी करण्यासाठी **State**, **Region**, **HQ** किंवा **Status** निवडा. तुमचा स्वतःचा भाग आधीच निवडलेला आणि लॉक केलेला असतो.
3. **Action** कॉलममध्ये डोळ्याचा आयकॉन सब डीलर पाहण्यासाठी उघडतो. पेन्सिल तो बदलण्यासाठी उघडते. डस्टबिन त्याचे सेव्ह केलेले तपशील डिलीट करते.
4. वरची बटणे वापरा. **Refresh** लिस्ट परत लोड करते. **Export All Saved** सेव्ह केलेले सगळे सब डीलर Excel म्हणून डाउनलोड करते. **Maintain Sub Dealer** फॉर्म उघडते.
5. खाली **State-wise Sub Dealer Summary** पर्यंत स्क्रोल करा. त्यात प्रत्येक राज्याच्या मास्टर नोंदींची तुलना सेव्ह केलेल्या **Active** आणि **Inactive** सब डीलरशी केलेली असते.

> Careful: डस्टबिन फक्त सेव्ह केलेले तपशील काढते, सब डीलरला नाही. कोणी परत तपशील भरेपर्यंत तो या लिस्टमधून निघून जातो.

## नेहमीच्या अडचणी

- **"No saved Sub Dealers found".** (सेव्ह केलेला एकही सब डीलर सापडला नाही) इथे फक्त सेव्ह केलेले तपशील असलेले सब डीलर दिसतात. Admin च्या अपलोडमधल्या ज्या ओळी कोणी भरल्या नाहीत त्या लपवलेल्या असतात.
- **पेन्सिल आणि डस्टबिन राखाडी आहेत.** तुमचा रोल फक्त पाहू शकतो. MO, MDO, JMDO, Admin, AVP आणि Director सब डीलर बदलू शकतात.
- **Maintain Sub Dealer बटण दिसत नाही.** तुमचा रोल फक्त लिस्ट पाहू शकतो.
- **"No saved Sub Dealer records are available to export."** (एक्सपोर्ट करायला सेव्ह केलेली एकही नोंद नाही) तुमच्या भागात अजून काहीही सेव्ह झालेले नाही.
:::

::: section maintain-sub-dealer
title: सब डीलरचे तपशील भरा किंवा अपडेट करा
routes: /SubDealerRegistration
for: 
roles: 
designations: 
tour: 1 > [data-help=sdr-business] | 2 > [data-help=sdr-basic] | 3 > [data-help=sdr-location] | 4 > [data-help=sdr-tax] | 5 > [data-help=sdr-mfms] | 6 > [data-help=sdr-submit]
minutes: 5
source-hash: e410f07c765072a5
confidence: ok

**ही स्क्रीन कशासाठी आहे**
Admin ने आधीच इम्पोर्ट केलेल्या सब डीलरचे तपशील भरण्यासाठी **Sub Dealer Maintenance** स्क्रीन वापरा. इथे नवीन सब डीलर कोड तयार करता येत नाही. लिस्टवरच्या **Maintain Sub Dealer** ने, किंवा ओळीवरच्या पेन्सिलने ही स्क्रीन उघडा.

1. **Business Information** खाली **Select Imported Sub Dealer** टॅप करा, कोड किंवा नाव टाइप करा आणि सब डीलर निवडा. त्याचे सेव्ह केलेले तपशील लोड होतात. **Firm Name** (GST वरच्या ट्रेड नावाप्रमाणे) तपासा.
2. **Basic Information** खाली **State**, **Region** आणि **HQ** तपासा, आणि **Status** निवडा: **Active** किंवा **Inactive**.
3. **Primary Location** खाली **Use Current Location** दाबा, नकाशावर टॅप करा, किंवा Google Maps ची लिंक पेस्ट करा. पत्ता भरा. 6 अंकी **Pincode** टाइप केल्यावर State, District, Block, Taluk आणि Village आपोआप भरतात. **Official Contact Number**, **WhatsApp Number** आणि **Alternative Number** टाका.
4. **Tax & Compliance** खाली **Upload GST Certificate (PDF)** टॅप करा. ॲप PDF वाचते आणि **GST Number**, **Legal Name**, **Trade Name** आणि **Constitution of Business** भरते. ते तपासा, मग **PAN Number** टाका.
5. **mFMS Information** खाली **Retail mFMS ID** टाका. **Wholesale mFMS ID** भरणे गरजेचे नाही.
6. **Submit** दाबा. सब डीलर सेव्ह झाल्याचा हिरवा मेसेज दिसतो, आणि पुढच्या सब डीलरसाठी फॉर्म रिकामा होतो.

> Tip: **Active** सब डीलरसाठी * खूण असलेले प्रत्येक फील्ड भरणे आवश्यक आहे. **Inactive** सब डीलरसाठी फक्त State, Region, HQ आणि Firm Name लागतात.
> Careful: डोळ्याच्या आयकॉनने सब डीलर उघडला तर प्रत्येक फील्ड राखाडी असते आणि **Submit** बटण नसते. बदल करण्यासाठी पेन्सिल वापरा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Select Imported Sub Dealer** | Admin च्या अपलोडमधला सब डीलर. भरणे आवश्यक. |
| **Status** | सब डीलर आता काम करत असेल तर **Active**, नसेल तर **Inactive**. |
| **Google Map URL** आणि **Latitude & Longitude** | दुकानाचे ठिकाण. Active साठी आवश्यक. |
| **Pincode** | 6 अंक. यामुळे गावाची आणि भागाची नावे आपोआप भरतात. |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 अंकी मोबाइल नंबर. Active साठी आवश्यक. |
| **GST Number** | 15 अक्षरे, उदा. 22AAAAA0000A1Z5. Active साठी आवश्यक. |
| **Upload GST Certificate (PDF)** | एकच PDF फाइल, 5 MB पर्यंत. Active साठी आवश्यक. |
| **PAN Number** | 10 अक्षरे, उदा. ABCDE1234F. Active साठी आवश्यक. |
| **Retail mFMS ID** | Active साठी आवश्यक. |

## नेहमीच्या अडचणी

- **"Please correct the highlighted fields."** (लाल खूण केलेली फील्ड दुरुस्त करा) प्रत्येक फील्डखालचा लाल मजकूर वाचा आणि ते दुरुस्त करा.
- **Submit बटण राखाडी आहे.** आधी सब डीलर निवडा आणि तो लोड होईपर्यंत थांबा. GST PDF, ठिकाण किंवा पिनकोड वाचले जात असताना पण थांबा.
- **"Shortened URL detected."** (छोटी केलेली लिंक सापडली) लिंक ब्राउझरमध्ये उघडा, ॲड्रेस बारमधला पूर्ण पत्ता कॉपी करा आणि तो पेस्ट करा.
- **"Choose a non-empty GST PDF of 5 MB or smaller."** (5 MB किंवा त्यापेक्षा लहान, रिकामी नसलेली GST PDF निवडा) 5 MB किंवा त्यापेक्षा लहान PDF फाइल अपलोड करा.
- **"GST could not be extracted."** (GST चे तपशील वाचता आले नाहीत) GST चे तपशील स्वतः टाइप करा आणि ते तपासा.
- **"The selected Sub Dealer does not have a valid Sub Dealer Code. Contact Admin."** (निवडलेल्या सब डीलरचा कोड बरोबर नाही. Admin शी संपर्क करा) मास्टरमधली नोंद चुकीची आहे. ती दुरुस्त करायला Admin ला सांगा.
:::

::: section import-master
title: सब डीलर मास्टर इम्पोर्ट करा
routes: /SubDealerList
for: 
roles: Admin, SuperAdmin, CorporateAdmin
designations: 
minutes: 3
source-hash: c6fda3f4afafbb6e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
Admin सब डीलरची मंजूर Excel लिस्ट अपलोड करतात. इम्पोर्ट आधी प्रत्येक ओळीची तपासणी दाखवते, आणि तुम्ही **Proceed Import** दाबेपर्यंत काहीही सेव्ह करत नाही.

1. **Sub Dealer Master** वर **Import Sub Dealers** दाबा.
2. **Download Sample Format** दाबा. **Sub Dealer Code**, **Sub Dealer Name**, **HQ**, **Region** आणि **State** हे कॉलम भरा. State, Region आणि HQ ची नावे ॲपमध्ये आहेत तशीच वापरा.
3. **Choose Excel file** खाली .xlsx फाइल निवडा.
4. **Check Import** दाबा. **Import summary** मध्ये **New**, **Already same** आणि **Rejected** ओळी दिसतात. अजून काहीही सेव्ह झालेले नाही.
5. नाकारलेल्या प्रत्येक ओळीचे **Reason** (कारण) वाचा. गरज असेल तर Excel दुरुस्त करा आणि परत तपासा.
6. नवीन ओळी सेव्ह करण्यासाठी **Proceed Import** दाबा.
7. आता नवीन सब डीलर फॉर्मवर निवडता येतात. त्यांचे तपशील सेव्ह झाल्यावरच ते लिस्टमध्ये दिसतात.

> Careful: आधीपासून असलेल्या सब डीलरची फर्म, State, Region किंवा HQ इम्पोर्टने बदलता येत नाही.
> Tip: SuperAdmin ला चुकीचे किंवा रिकामे कोड दुरुस्त करण्यासाठी **Correct Sub Dealer Codes** आणि **Repair Missing Codes** पण दिसतात. दोन्ही काहीही सेव्ह करण्याआधी प्रिव्ह्यू दाखवतात.

## नेहमीच्या अडचणी

- **"Warning: Existing Sub Dealer master conflict detected!"** (आधीच्या सब डीलर मास्टरशी विसंगती सापडली) आधीपासून असलेल्या कोडसाठी फाइलमध्ये वेगळी फर्म, State, Region किंवा HQ दिलेले आहे. ते बदलण्यासाठी Admin शी संपर्क करा.
- **काही ओळींमध्ये Sub Dealer Code रिकामे आहेत किंवा दिसत नाहीत.** त्या ओळी नाकारल्या गेल्या आणि सेव्ह झाल्या नाहीत. दाखवलेले Excel ओळ नंबर तपासा.
- **Check Import राखाडी आहे.** आधी .xlsx फाइल निवडा. फाइल 10 MB पर्यंत असू शकते.
:::

::: section beneficiary-list
title: सब डीलर आणि कर्मचाऱ्यांचे लाभार्थी पाहा
routes: /SubDealerEmployeeMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: dfcd6ed3ad5ab51a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**SDWA** मेनूमधल्या **Sub Dealer & Employee** स्क्रीनवर सब डीलरचे आणि डीलरच्या कर्मचाऱ्यांचे कल्याण योजनेचे लाभार्थी ठेवले जातात. प्रत्येक नोंदीला आधी SM ची आणि मग AVP ची मंजुरी लागते.

1. एक टॅब निवडा: **Sub Dealer** किंवा **Employee**.
2. कार्डे पाहा: एकूण, **Active** आणि **Inactive**.
3. सर्च बॉक्समध्ये टाइप करा. **All Status**, **Active** किंवा **Inactive** निवडा, आणि **All Approval**, **Pending SM**, **Pending AVP**, **Approved** किंवा **Rejected** निवडा.
4. टेबलमध्ये प्रत्येक नोंदीचे **SM Status**, **AVP Status** आणि **Overall** वाचा.
5. तुमच्या रोलला नोंदी बदलता येत असतील तर **Status** कॉलममध्ये **Active** किंवा **Inactive** टॅप करून ते बदला.

## नेहमीच्या अडचणी

- **"You do not have permission to view this data."** (हा डेटा पाहण्याची तुम्हाला परवानगी नाही) Sub Dealer & Employee पाहण्याची परवानगी तुमच्या admin कडे मागा.
- **वर फॉर्म दिसत नाही.** तुमचे पद फक्त नोंदी पाहू शकते.
- **एखाद्या नोंदीवर खूप वेळ Pending दिसते.** ती आधी SM ची, मग AVP ची वाट पाहते. ती कुठे आहे ते पाहण्यासाठी **Pending SM** किंवा **Pending AVP** फिल्टर वापरा.
:::

::: section beneficiary-add
title: लाभार्थी जोडा किंवा बदला
routes: /SubDealerEmployeeMaster
for: SubDealerEmployeeMaster.Entry
roles: 
designations: 
minutes: 3
source-hash: c972236359c62bca
confidence: ok

**ही स्क्रीन कशासाठी आहे**
सब डीलरचा किंवा डीलरच्या कर्मचाऱ्याचा कल्याण योजनेचा लाभार्थी जोडा, एकेक करून किंवा Excel फाइलमधून. प्रत्येक नवीन नोंद SM च्या मंजुरीसाठी **Pending** म्हणून सुरू होते.

1. **Sub Dealer** किंवा **Employee** टॅब निवडा. वरच्या फॉर्मवर **Add New Sub Dealer** किंवा **Add New Employee** लिहिलेले असते.
2. सब डीलरसाठी **Main Dealer Code**, **Main Dealer / Firm Name**, **Sub Dealer Code**, **Sub Dealer Name** आणि **Beneficiary Name** भरा. कर्मचाऱ्यासाठी **Dealer Code**, **Employee Name** आणि **Beneficiary Name** भरा.
3. तुमच्याकडे असलेले इतर तपशील भरा, जसे **Beneficiary ID**, **Nominee Name**, **Date of Birth** आणि **Relationship**.
4. स्विच **Active** वरच ठेवा आणि **Save** दाबा.
5. एकदम अनेक नोंदी जोडायच्या असतील तर **Download Template** दाबा, ते भरा, मग **Upload File** दाबा आणि Excel फाइल निवडा.
6. नोंद बदलायची असेल तर **Action** कॉलममधली पेन्सिल टॅप करा, दुरुस्त करा आणि **Save** दाबा. डस्टबिन नोंद डिलीट करते.

> Careful: डिलीट केलेली नोंद परत आणता येत नाही. नोंद बदलल्याने तिची SM आणि AVP मंजुरी परत सुरुवातीपासून होत नाही.

## नेहमीच्या अडचणी

- **"Main Dealer Code is required."** (Main Dealer Code भरणे आवश्यक आहे) * खूण असलेले प्रत्येक फील्ड भरा. मेसेजमध्ये राहिलेल्या फील्डचे नाव असते.
- **सेव्ह करताना "already exists" येते.** त्याच Dealer Code, Sub Dealer Code, Beneficiary Name आणि DOB चा लाभार्थी लिस्टमध्ये आधीच आहे. तो शोधा.
- **"Upload completed with ... skipped row(s)".** (अपलोड झाले, पण काही ओळी सोडल्या) मेसेजखाली दाखवलेल्या सोडलेल्या ओळी वाचा. त्या कॉपी करण्यासाठी **Copy** दाबा, फाइल दुरुस्त करा आणि त्या ओळी परत अपलोड करा.
:::

::: section beneficiary-approve
title: लाभार्थी मंजूर करा किंवा नाकारा
routes: /SubDealerEmployeeMaster
for: 
roles: SMD, SMM, AVP
designations: 
minutes: 2
source-hash: c429d512dd70afcb
confidence: ok

**ही स्क्रीन कशासाठी आहे**
SMD किंवा SMM प्रत्येक लाभार्थ्याला आधी मंजुरी देतात. AVP अंतिम मंजुरी देतात. कल्याण योजनेच्या अर्जात डीलर फक्त मंजूर आणि Active लाभार्थीच निवडू शकतो.

1. **Sub Dealer** किंवा **Employee** टॅब निवडा.
2. मंजुरीचा फिल्टर **Pending SM** (SMD, SMM) किंवा **Pending AVP** (AVP) वर ठेवा.
3. नोंदीचे तपशील तपासा: डीलर कोड, नावे, जन्मतारीख आणि नाते.
4. **Action** कॉलममध्ये मंजूर करण्यासाठी हिरवी टिक, किंवा नाकारण्यासाठी लाल क्रॉस दाबा. फोनवर **Approve** किंवा **Reject** दाबा.
5. **Remarks (optional)** टाइप करा आणि **Approve** किंवा **Reject** दाबा.
6. **SM Status** किंवा **AVP Status** कॉलम बदलतो. SM ने मंजूर केल्यावरच AVP ला नोंद दिसते.

> Careful: नाकारलेली नोंद तिथेच थांबते. कल्याण योजनांसाठी ती डीलरला दिली जात नाही.

## नेहमीच्या अडचणी

- **ओळीवर टिक किंवा क्रॉस दिसत नाही.** **SM Status** **Pending** असेल तेव्हाच SM ला ते दिसतात. SM ने मंजूर केल्यावर आणि **AVP Status** **Pending** असेल तेव्हाच AVP ला ते दिसतात.
- **Overall कॉलममध्ये SM Rejected दिसते.** SM ने नोंद नाकारली, त्यामुळे ती AVP पर्यंत पोहोचत नाही.
:::
