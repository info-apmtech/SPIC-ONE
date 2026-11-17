---
module: my-account
title: मेरा अकाउंट और डैशबोर्ड
icon: bi-person-circle
order: 118
language: hi
category: सामान्य
note: नई गाइड
meta-source-hash: a6f45f8833df3868
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 4b8137c0f3f7283d
confidence: ok

ये स्क्रीन आपसे जुड़ी हैं: डीलर रजिस्ट्रेशन का डैशबोर्ड, आपकी अपनी प्रोफ़ाइल, आपके लिए बाकी कामों की लिस्ट, और SPIC से संपर्क कैसे करें।

- dashboard | icon: bi-speedometer2 | color: #0b6b3a
  title: डीलर रजिस्ट्रेशन
  text: अपने इलाके की हर डीलर एप्लिकेशन उसके स्टेटस के साथ देखें, और जो आपके पास बाकी हैं उन पर काम करें।
- profile | icon: bi-person-badge | color: #2563eb
  title: आपकी प्रोफ़ाइल
  text: अपना नाम, पद (designation), जगह और स्टेटस देखें। डीलर को अपने रजिस्ट्रेशन की जानकारी भी दिखती है।
- help | icon: bi-bell | color: #d97706
  title: अलर्ट और मदद
  text: देखें कि आपके लिए क्या बाकी है, अपना रोज़ का काम ढूँढें, और SPIC को मैसेज भेजें।
:::

::: section flow
title: डैशबोर्ड पर डीलर रजिस्ट्रेशन कैसे आगे बढ़ता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 6696a0bfbcbd48ab
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। डीलर के डैशबोर्ड कार्ड पर दिखता है कि वह किस स्टेप तक पहुँचा है।

- creator | type: lane | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: आपका HQ
- rm-lane | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: आपका रीजन
- smm-lane | type: lane | color: #7c3aed | roles: SMD, SMM
  title: SMM
  sub: आपका राज्य
- avp-lane | type: lane | color: #2563eb | roles: AVP, Admin
  title: AVP
  sub: आख़िरी मंज़ूरी
- draft | lane: creator | col: 1 | icon: bi-pencil-square | color: #a16207 | next: rm | screen: dashboard
  title: भरें और सबमिट करें
  status: Draft
  text: जब तक एप्लिकेशन सबमिट नहीं होती, कार्ड पर **Draft** दिखता है। उसे आगे भरने के लिए **Edit** दबाएँ।
- rm | lane: rm-lane | col: 2 | icon: bi-hourglass-split | color: #1d4ed8 | next: smm | return: draft | screen: dashboard
  title: RM जाँचता है
  status: In RM
  text: RM **Review** दबाता है। अगर RM इसे लौटा देता है, तो कार्ड पर **Rejected by RM** दिखता है और MO इसे फिर से बदल सकता है।
- smm | lane: smm-lane | col: 3 | icon: bi-hourglass-split | color: #7c3aed | next: avp | return: rm | screen: dashboard
  title: SMM जाँचता है
  status: In SMM
  text: SMM **Review** दबाता है। अगर SMM इसे लौटा देता है, तो **Rejected by SMM** दिखता है और यह RM के पास वापस जाती है।
- avp | lane: avp-lane | col: 4 | icon: bi-patch-check | color: #4338ca | next: approved | return: smm | screen: dashboard
  title: AVP मंज़ूर करता है
  status: In AVP
  text: AVP आख़िरी मंज़ूरी देता है। अगर AVP इसे लौटा देता है, तो **Rejected by AVP** दिखता है और यह SMM के पास वापस जाती है।
- approved | lane: avp-lane | col: 5 | icon: bi-check-circle | color: #16a34a | screen: dashboard
  title: मंज़ूर
  status: Approved
  text: कार्ड पर **Approved** दिखता है। नए डीलर पर पहले **In AVP Office** दिखता है, जब तक AVP Office कंपनी के Dealer ID सेव नहीं कर देता।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 196e7278f9089805
confidence: ok

- draft | color: #a16207
  label: Draft
  text: अभी सबमिट नहीं हुई। MO, MDO या JMDO अभी भी इसे बदल सकते हैं।
- in-rm | color: #1d4ed8
  label: In RM
  text: RM के जाँचने का इंतज़ार है।
- in-smm | color: #7c3aed
  label: In SMM
  text: SMM के जाँचने का इंतज़ार है।
- in-avp | color: #4338ca
  label: In AVP
  text: AVP की आख़िरी मंज़ूरी का इंतज़ार है।
- in-avp-office | color: #0e7490
  label: In AVP Office
  text: नया डीलर मंज़ूर हो गया है। AVP Office को अभी कंपनी के Dealer ID सेव करने हैं।
- approved | color: #047857
  label: Approved
  text: एप्लिकेशन पूरी तरह मंज़ूर हो गई है।
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: ठीक करने के लिए MO, MDO या JMDO को लौटा दी गई है।
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: RM को लौटा दी गई है।
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: SMM को लौटा दी गई है।
- action | color: #2563eb
  label: Action Required
  text: इस कार्ड पर काम करने की अब आपकी बारी है।
- active | color: #047857
  label: Active
  text: डीलर, या प्रोफ़ाइल पर आपका अपना अकाउंट, चालू है।
- inactive | color: #c2410c
  label: Inactive
  text: डीलर, या आपका अकाउंट, अभी चालू नहीं है।
- terminated | color: #dc2626
  label: Terminated
  text: डीलरशिप ख़त्म हो गई है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5025b20d273029c0
confidence: ok

आपका कार्ड हाइलाइट होता है। हर कोई अपनी प्रोफ़ाइल खोल सकता है।

- creator | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - डीलर एप्लिकेशन बनाना और बदलना
  - RM की लौटाई हुई एप्लिकेशन ठीक करना
- approvers | color: #0d9488 | roles: RM, RMD, SMD, SMM
  title: RM, SMM
  - Action Required वाले कार्ड जाँचना
  - डीलर लिस्ट डाउनलोड करना
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - आख़िरी मंज़ूरी देना
  - डीलर इम्पोर्ट करना या जोड़ना
  - Admin अपनी प्रोफ़ाइल बदल सकता है
- dealer | color: #7c3aed | roles: Dealer
  title: डीलर
  - अपनी प्रोफ़ाइल पर अपने रजिस्ट्रेशन की जानकारी देखना
  - अपनी कल्याण योजना (welfare scheme) की एप्लिकेशन देखना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 40062c69f8f437fc
confidence: ok

- no-edit-button | popular: true
  q: मुझे डीलर कार्ड पर Edit या Review नहीं दिख रहा
  a: ये बटन सिर्फ़ उसी को दिखते हैं जिसकी बारी है। अभी यह किसके पास है, यह जानने के लिए कार्ड पर स्टेटस देखें।
- wrong-profile | popular: true
  q: मेरी प्रोफ़ाइल पर मेरा नाम, पद या जगह ग़लत है
  a: आप इसे **Profile** स्क्रीन पर नहीं बदल सकते। अपने एडमिनिस्ट्रेटर से इसे ठीक करने को कहें।
- nothing-waiting
  q: Alerts में "Nothing waiting for you" दिख रहा है
  a: अभी कोई मंज़ूरी, बुकिंग या एप्लिकेशन आपके पास बाकी नहीं है। बाद में फिर देखें।
- dealer-buttons
  q: मेरी डीलर प्रोफ़ाइल पर Download PDF और Change Password दबाने से कुछ नहीं होता
  a: ये दोनों बटन अभी काम नहीं करते।
- no-directions
  q: Get Directions दबाने पर "Location permission is required" दिखता है
  a: ऐप या ब्राउज़र को लोकेशन की इजाज़त दें, फिर से **Get Directions** दबाएँ।
:::

::: section dashboard
title: डीलर रजिस्ट्रेशन देखें और उन पर काम करें
routes: /Dashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-dash-kpis] | 2 > [data-help=ma-dash-filters] | 3 > [data-help=ma-dash-cards] | 4 > [data-help=ma-dash-cards] | 5 > [data-help=ma-dash-actions]
minutes: 3
source-hash: 1b2061e620bf738f
confidence: ok

**यह स्क्रीन किस काम की है**
**Dealer Registrations** स्क्रीन पर आपके इलाके की हर डीलर एप्लिकेशन एक कार्ड के रूप में दिखती है, और ऊपर गिनती होती है। ज़्यादातर रोल को सिर्फ़ अपने State, Region या HQ के डीलर दिखते हैं।

1. ऊपर की गिनती देखें: **Total Registrations**, **Draft**, **In RM**, **In SMM**, **In AVP**, **Approved** और **Rejected / Returned**। **Status** बॉक्स में चालू (active), बंद (inactive) और ख़त्म (terminated) डीलर गिने जाते हैं।
2. सर्च बॉक्स में डीलर कोड या फ़र्म का नाम टाइप करें, या कंपनी, डीलर का प्रकार, स्टेटस, रीजन, मंज़ूरी का स्टेटस या क्रम (sort order) चुनें। कार्ड तुरंत फिर से लोड हो जाते हैं।
3. हर कार्ड पर फ़र्म का नाम, **Region**, **HQ**, मंज़ूरी का स्टेटस और डीलर का स्टेटस दिखता है। नीले **Action Required** टैग का मतलब है कि आपकी बारी है। और कार्ड देखने के लिए नीचे **Show more** दबाएँ।
4. ड्राफ़्ट या लौटाई गई एप्लिकेशन को आगे भरने के लिए **Edit** दबाएँ, या आपके पास आई एप्लिकेशन जाँचने के लिए **Review** दबाएँ। आँख वाला आइकन एप्लिकेशन को पढ़ने के लिए खोलता है। **SPIC** और **GFL** डीलरशिप की PDF खोलते हैं।
5. अपने फ़िल्टर के हिसाब से लिस्ट को Excel फ़ाइल में डाउनलोड करने के लिए **Dealer Master** या **Credit limit status** दबाएँ। MO, MDO और JMDO को **Create Existing Dealer** भी दिखता है, जो डीलर एप्लिकेशन फ़ॉर्म खोलता है।

> Tip: **Credit Limit** कार्ड पर सिर्फ़ Admin और MO को दिखता है, वह भी आख़िरी मंज़ूरी के बाद और जब डीलर का Dealer Code हो।
> Careful: Admin डस्टबिन आइकन से रजिस्ट्रेशन मिटा सकता है। इसे वापस नहीं लाया जा सकता।

## आम दिक्कतें

- **"No dealer registrations found."** (कोई डीलर रजिस्ट्रेशन नहीं मिला) हो सकता है आपकी सर्च या फ़िल्टर बहुत कड़े हों। सर्च बॉक्स ख़ाली करें और फ़िल्टर फिर से "All" पर कर दें।
- **Edit या Review बटन नहीं है।** एप्लिकेशन अभी किसी दूसरे लेवल पर है। कार्ड पर स्टेटस देखें।
- **ऊपर की गिनती मेरे रीजन से मेल नहीं खाती।** गिनती पर भी वही फ़िल्टर लगते हैं जो कार्ड पर लगे हैं। फ़िल्टर फिर से "All" पर कर दें।
:::

::: section dashboard-add-dealers
title: डीलर इम्पोर्ट करें या जोड़ें
routes: /Dashboard
for: 
roles: Admin, AVP
designations: 
minutes: 3
source-hash: 1bfd6d4d63fce849
confidence: ok

**यह स्क्रीन किस काम की है**
Admin और AVP पहले से मौजूद डीलरों को Excel फ़ाइल से ऐप में ला सकते हैं, एक डीलर हाथ से जोड़ सकते हैं, या नए डीलर की एप्लिकेशन शुरू कर सकते हैं।

1. Excel टेम्पलेट डाउनलोड करने के लिए **Sample Format** दबाएँ।
2. हर डीलर के लिए एक लाइन भरें: **CUSTOMER**, **CUSTOMER NAME** और **State**। CUSTOMER कोड के पहले अक्षर से पता चलता है कि यह किस कंपनी का कोड है, जैसे SPIC के लिए **D**।
3. **Import from Excel** दबाएँ और फ़ाइल (.xlsx या .xls) चुनें। जब तक "Importing, please wait..." दिखे, इंतज़ार करें।
4. नतीजा पढ़ें। जो लाइनें इम्पोर्ट नहीं हुईं, वे कारण के साथ **Skipped Groups** में दिखती हैं। **Close** दबाएँ।
5. सिर्फ़ एक डीलर जोड़ने के लिए **Add Dealer** दबाएँ, **CUSTOMER Code**, **CUSTOMER Name** और **State** भरें, और **Save Dealer** दबाएँ।
6. नए डीलर की पूरी एप्लिकेशन शुरू करने के लिए **Create New Dealer** दबाएँ।

> Tip: Admin को **Total Dealer List** भी दिखता है, जो राज्य के हिसाब से डीलरों की गिनती खोलता है।

| फ़ील्ड | क्या भरें |
|---|---|
| **CUSTOMER Code** | कंपनी के रिकॉर्ड में डीलर का कोड। ज़रूरी है। |
| **CUSTOMER Name** | फ़र्म या डीलर का नाम। |
| **State** | डीलर का राज्य। |

## आम दिक्कतें

- **कुछ लाइनें छोड़ दी गईं।** **Skipped Groups** में कारण पढ़ें, फ़ाइल में वे लाइनें ठीक करें और फिर से इम्पोर्ट करें।
- **"Import failed."** (इम्पोर्ट नहीं हुआ) देख लें कि फ़ाइल वही Excel टेम्पलेट है जिसमें तीन कॉलम हैं, फिर दोबारा कोशिश करें।
:::

::: section profile
title: मेरी प्रोफ़ाइल देखें
routes: /Profile
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-prf-banner] | 2 > [data-help=ma-prf-tabs] | 3 > [data-help=ma-prf-details] | 4 > [data-help=ma-prf-details]
minutes: 2
source-hash: 854cdeee48d9aeee
confidence: ok

**यह स्क्रीन किस काम की है**
**Profile** स्क्रीन पर आपके अपने अकाउंट की जानकारी दिखती है। स्टाफ़ को अपनी कर्मचारी वाली जानकारी दिखती है। डीलर को अपने रजिस्ट्रेशन की जानकारी दिखती है।

1. हरी पट्टी देखें। उसमें आपका नाम, आपका रोल या कोड और आपका पद दिखता है। डीलर को **Profile Completion** भी दिखता है।
2. टैब इस्तेमाल करें। स्टाफ़ को एक टैब दिखता है (**Employee**, या Admin के लिए **Profile**)। डीलर को **Dealer Info**, **Proprietorship**, **Nominees**, **Trade Experience**, **Turnover**, **Assets** और **Welfare Scheme** दिखते हैं।
3. कार्ड में अपनी जानकारी पढ़ें, जैसे **Designation**, **Region**, **Headquarter** और **Status**। **Turnover** पर **SPIC** और **Green Star** के बीच बदलें।
4. अगर कुछ ग़लत है, तो अपने एडमिनिस्ट्रेटर से इसे ठीक करने को कहें। इस स्क्रीन पर आप अपनी जानकारी नहीं बदल सकते।

> Careful: डीलर प्रोफ़ाइल पर **Download PDF** और **Change Password** अभी काम नहीं करते।

## आम दिक्कतें

- **"No employee profile information available."** (कर्मचारी प्रोफ़ाइल की जानकारी नहीं है) आपका कर्मचारी रिकॉर्ड आपके लॉगिन से जुड़ा नहीं है। अपने एडमिनिस्ट्रेटर से कहें।
- **"No dealer registration data available."** (डीलर रजिस्ट्रेशन की जानकारी नहीं है) आपका डीलर रजिस्ट्रेशन अभी आपके लॉगिन से जुड़ा नहीं है। अपने MO से संपर्क करें।
- **"No welfare scheme applications found."** (कल्याण योजना की कोई एप्लिकेशन नहीं मिली) आपने अभी तक किसी कल्याण योजना के लिए आवेदन नहीं किया है।
:::

::: section profile-edit
title: मेरी प्रोफ़ाइल बदलें (Admin)
routes: /Profile
for: 
roles: Admin
designations: 
minutes: 1
source-hash: 50d60fcf9a6af1a1
confidence: ok

**यह स्क्रीन किस काम की है**
सिर्फ़ Admin रोल को **Edit Profile** बटन दिखता है। Admin अपना नाम, ईमेल और पद बदल सकता है।

1. **Admin Details** कार्ड पर **Edit Profile** दबाएँ।
2. **Name**, **Email** या **Designation** बदलें। Role, User Name और Status यहाँ नहीं बदले जा सकते।
3. **Save Changes** दबाएँ। "Profile updated successfully." (प्रोफ़ाइल अपडेट हो गई) मैसेज दिखता है।
4. बिना सेव किए बंद करने के लिए **Cancel** दबाएँ।

## आम दिक्कतें

- **"Your profile could not be saved. Please try again."** (आपकी प्रोफ़ाइल सेव नहीं हो सकी, फिर से कोशिश करें) अपना नेटवर्क देखें और फिर से **Save Changes** दबाएँ।
- **फ़ॉर्म लॉक है और लिखा है कि आपकी जानकारी लोड नहीं हो सकी।** **Cancel** दबाएँ और फिर से **Edit Profile** खोलें।
:::

::: section user-details
title: अपनी प्रोफ़ाइल पूरी करें (प्रीव्यू)
routes: /UserProfile
for: 
roles: 
designations: 
minutes: 1
source-hash: e4cca852b7305bb4
confidence: ok

**यह स्क्रीन किस काम की है**
**User Details** स्क्रीन सिर्फ़ डिज़ाइन का नमूना (preview) है। यह मेन्यू में नहीं है, और अभी कुछ भी सेव नहीं करती।

1. स्क्रीन पर **Complete Your Profile** और **Full Name**, **Mobile Number**, **Address** और **District** फ़ील्ड दिखते हैं।
2. **Continue** बटन अभी यह जानकारी सेव नहीं करता।

## आम दिक्कतें

- **मेरी जानकारी सेव नहीं हुई।** यह सामान्य है। यह स्क्रीन सिर्फ़ प्रीव्यू है। आपकी असली जानकारी **Profile** पर है।
:::

::: section alerts
title: देखें कि मेरे लिए क्या बाकी है
routes: /Alerts
for: 
roles: 
designations: 
minutes: 1
source-hash: b3694f1c480ea2a4
confidence: ok

**यह स्क्रीन किस काम की है**
**Alerts** स्क्रीन पर वे कतारें (queues) दिखती हैं जिन पर आपको काम करना पड़ सकता है। फ़ोन या टैबलेट पर इसे ऐप में ऊपर वाली घंटी से खोलें। इसमें सिर्फ़ वही लिंक दिखते हैं जो आपका रोल इस्तेमाल कर सकता है।

1. **Approvals waiting** में कोई कतार टैप करें, जैसे **Scheme Approval** या **Dealer Application Review**।
2. **Bookings and applications** में **My Bookings** या **Welfare Schemes** टैप करें।
3. उस स्क्रीन पर काम करें, फिर यहाँ वापस आएँ।

> Tip: पुश नोटिफ़िकेशन ऐप स्टोर वाली रिलीज़ के साथ आएँगे। तब तक यह स्क्रीन देखते रहें।

## आम दिक्कतें

- **"Nothing waiting for you".** (आपके लिए कुछ बाकी नहीं) आपके रोल के लिए यहाँ मंज़ूरी, बुकिंग या एप्लिकेशन की कोई स्क्रीन नहीं है।
- **कतार खुलती है पर ख़ाली होती है।** आपके रोल के लिए यह लिंक हमेशा दिखता है। इसका मतलब यह नहीं कि कुछ बाकी है।
:::

::: section activities
title: मेरा रोज़ का काम खोलें
routes: /Activities
for: 
roles: 
designations: 
minutes: 1
source-hash: 3155933a5c612823
confidence: ok

**यह स्क्रीन किस काम की है**
**My Activities** स्क्रीन पर आपका फ़ील्ड का काम, मंज़ूरियाँ, गेस्ट हाउस बुकिंग और रिपोर्ट एक ही जगह मिलते हैं। फ़ोन पर MO स्टाफ़ को यह नीचे वाले टैब बार में मिलती है।

1. **Field activities** में, अगर आपके पास हों, तो **Sample Collection** या **Consignment History** टैप करें।
2. **Pending on you** में **Scheme Approval** या **Pending Acknowledgement** जैसी कोई कतार टैप करें।
3. **Guest house** में **Book a room** या **My Bookings** टैप करें।
4. **Reports** में **Reports Center** टैप करें।

> Tip: कोई हिस्सा तभी दिखता है जब आपका रोल उसके कम से कम एक लिंक का इस्तेमाल कर सकता हो।

## आम दिक्कतें

- **मुझे सिर्फ़ "More of the Sales Activity System is on its way" दिख रहा है।** आपके रोल के लिए अभी फ़ील्ड के काम की कोई स्क्रीन नहीं है। ये बाद में जोड़ी जाएँगी।
:::

::: section contact-us
title: SPIC से संपर्क करें
routes: /ContactUs
for: 
roles: 
designations: 
minutes: 2
source-hash: 6825b4b8292fb086
confidence: ok

**यह स्क्रीन किस काम की है**
**Contact Us** स्क्रीन पर SPIC ऑफ़िस का पता, ईमेल और फ़ोन नंबर, एक नक्शा, और अपना सवाल भेजने के लिए एक फ़ॉर्म दिखता है।

1. तीन कार्ड पर ऑफ़िस का पता, ईमेल और फ़ोन नंबर पढ़ें। ऑफ़िस का समय सोमवार से शुक्रवार, सुबह 9:30 से शाम 6:00 बजे तक है।
2. अपने मैप ऐप में ऑफ़िस देखने के लिए **Open in Maps** दबाएँ।
3. आप जहाँ हैं वहाँ से रास्ता, दूरी और लगने वाला समय देखने के लिए **Get Directions** दबाएँ।
4. **Send Us a Message** में **Full Name**, **Email Address**, **Enquiry Type**, **Subject** और **Message** भरें।
5. **Send Feedback** दबाएँ। "Thank you! Your enquiry has been sent." (धन्यवाद! आपकी पूछताछ भेज दी गई है) मैसेज दिखता है।

> Careful: **Send Email**, **Call Office** और **WhatsApp Us** अभी काम नहीं करते। कार्ड पर दिया ईमेल या फ़ोन नंबर इस्तेमाल करें।

| फ़ील्ड | क्या भरें |
|---|---|
| **Enquiry Type** | **Welfare Schemes**, **Room Bookings** या **General Enquiry**। |
| **Email Address** | ऐसा ईमेल जिस पर SPIC आपको जवाब दे सके। |
| **Message** | अपना सवाल कुछ लाइनों में। |

## आम दिक्कतें

- **"Please fill in every field before sending."** (भेजने से पहले हर फ़ील्ड भरें) पाँचों फ़ील्ड भरना ज़रूरी है।
- **"Please enter a valid email address."** (सही ईमेल पता डालें) ईमेल देख लें, जैसे name@example.com।
- **"Location permission is required to calculate the distance to SPIC Office."** (SPIC ऑफ़िस की दूरी निकालने के लिए लोकेशन की इजाज़त चाहिए) ऐप को लोकेशन की इजाज़त दें, फिर से **Get Directions** दबाएँ।
- **"Your enquiry could not be sent."** (आपकी पूछताछ नहीं भेजी जा सकी) अपना नेटवर्क देखें और बाद में फिर कोशिश करें।
:::

::: section gallery
title: फ़ोटो और वीडियो (प्रीव्यू)
routes: /Gallery, /Album, /ViewVideo
for: 
roles: 
designations: 
minutes: 1
source-hash: 00d0d57d14bc58ed
confidence: ok

**यह स्क्रीन किस काम की है**
**Gallery** में SDWA कार्यक्रमों, मीटिंग और गेस्ट हाउस की नमूना फ़ोटो दिखती हैं। ये स्क्रीन नमूना फ़ोटो वाला डिज़ाइन प्रीव्यू हैं, अभी असली एल्बम नहीं हैं।

1. **Gallery** पर **Photos** और **Videos** स्क्रॉल करके देखें।
2. **Guest House Rooms** एल्बम खोलने के लिए किसी फ़ोटो पर **View Album** दबाएँ।
3. एल्बम में **All Media**, **Photos** या **Videos** टैप करें, या सर्च बॉक्स में टाइप करें।
4. वीडियो कार्ड टैप करें। वीडियो स्क्रीन पर "Video will appear here" दिखता है, क्योंकि अभी कोई वीडियो डाला नहीं गया है।

> Tip: **Gallery** पर टैब, सर्च बॉक्स और क्रम (sort) वाली लिस्ट अभी काम नहीं करते। हर **View Album** बटन वही नमूना एल्बम खोलता है।

## आम दिक्कतें

- **वीडियो नहीं चलता।** अभी कोई वीडियो डाला नहीं गया है।
:::
