---
module: admin-masters
title: प्रशासन और मास्टर लिस्ट
icon: bi-gear
order: 117
language: hi
category: प्रशासन
note: नई गाइड
meta-source-hash: 2b15dc6f86ab1703
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 09f2a6bb7f506c56
confidence: ok

इन स्क्रीन पर वे बुनियादी लिस्ट रखी जाती हैं जिन्हें SPIC ONE का बाकी हिस्सा इस्तेमाल करता है: कर्मचारी और उनके लॉगिन, डेज़िग्नेशन (कौन व्यक्ति कौन-से पेज खोल सकता है), लोकेशन, फ़सलें और प्रोडक्ट, बैंक, और कुछ सेटिंग्स। इनमें से ज़्यादातर सीधी-सादी लिस्ट हैं: रिकॉर्ड जोड़ें, उसे बदलें, चालू या बंद करें।

- people | icon: bi-people | color: #374151
  title: लोग और ऐक्सेस
  text: कर्मचारियों को रजिस्टर करें, हर एक को रोल और लोकेशन दें, और पेज ऐक्सेस के लिए डेज़िग्नेशन चुनें।
- masters | icon: bi-list-check | color: #0d9488
  title: मास्टर लिस्ट
  text: लोकेशन, फ़सलें, प्रोडक्ट, बैंक, प्लांट और बाकी लिस्ट, जिन्हें दूसरी स्क्रीन इस्तेमाल करती हैं।
- tools | icon: bi-sliders | color: #2563eb
  title: सेटिंग्स और टूल
  text: GPS इंटरवल, CSR कट-ऑफ़ दिन, ऐप के इस्तेमाल के आँकड़े (metrics), IFMS इंपोर्ट और Data Explorer।
:::

::: section flow
title: नया कर्मचारी कैसे सेट किया जाता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: e8d287d4ff418bfb
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- admin | type: lane | color: #374151 | roles: Admin, SuperAdmin, CorporateAdmin
  title: एडमिनिस्ट्रेटर
  sub: Admin · SuperAdmin · CorporateAdmin
- staff | type: lane | color: #0d9488
  title: कर्मचारी
  sub: फ़ील्ड और ऑफ़िस के कर्मचारी
- locations | lane: admin | col: 1 | icon: bi-geo-alt | color: #0d9488 | next: designation | screen: location-master
  title: लोकेशन जोड़ें
  status: Active
  text: **Location Master** में वह ज़ोन, राज्य, रीजन और हेडक्वार्टर जोड़ें जहाँ कर्मचारी काम करेगा।
- designation | lane: admin | col: 2 | icon: bi-shield-lock | color: #7c3aed | next: register | screen: designation
  title: पेज ऐक्सेस चुनें
  status: Active
  text: **Designation Master** में एक डेज़िग्नेशन बनाएँ और टिक करें कि वह किन पेजों पर **View**, **Entry**, **Update** या **Delete** कर सकता है।
- register | lane: admin | col: 3 | icon: bi-person-plus | color: #2563eb | next: signin | screen: register-employee
  title: कर्मचारी को रजिस्टर करें
  status: Active
  text: रोल, डेज़िग्नेशन और लोकेशन चुनें, फिर लॉगिन की जानकारी भरें और **Save Employee** दबाएँ।
- signin | lane: staff | col: 4 | icon: bi-box-arrow-in-right | color: #16a34a
  title: लॉगिन करें और काम करें
  text: कर्मचारी यूज़र नेम और पासवर्ड से लॉगिन करता है। मेन्यू में सिर्फ़ वही पेज दिखते हैं जिनकी डेज़िग्नेशन इजाज़त देता है।
- upload-stopped | lane: admin | col: 3 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: register | to: locations | screen: employee-list
  title: अपलोड रुक गया
  text: अगर फ़ाइल में दिया कोई ज़ोन, राज्य, रीजन या HQ मौजूद नहीं है, तो कर्मचारियों का बल्क अपलोड रुक जाता है। उसे **Location Master** में जोड़ें और वही फ़ाइल फिर से अपलोड करें।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: d29ecb092c153613
confidence: ok

- active | color: #16a34a
  label: Active
  text: रिकॉर्ड इस्तेमाल में है और दूसरी स्क्रीन की लिस्ट में दिखता है।
- inactive | color: #64748b
  label: Inactive
  text: रिकॉर्ड रखा हुआ है पर बंद है। नई एंट्री करते समय यह नहीं दिखता।
- open | color: #dc2626
  label: Open
  text: सिर्फ़ Metrics में। एरर का ऐसा ग्रुप जिसे अभी तक किसी ने ठीक हुआ (resolved) मार्क नहीं किया।
- resolved | color: #16a34a
  label: Resolved
  text: सिर्फ़ Metrics में। किसी ने इस एरर ग्रुप को ठीक हुआ मार्क कर दिया।
- pending | color: #2563eb
  label: Pending
  text: सिर्फ़ IFMS इंपोर्ट में। रन शुरू होने का इंतज़ार कर रहा है।
- running | color: #2563eb
  label: Running
  text: सिर्फ़ IFMS इंपोर्ट में। डाउनलोड और इंपोर्ट अभी चल रहा है।
- succeeded | color: #16a34a
  label: Succeeded
  text: सिर्फ़ IFMS इंपोर्ट में। हर रिपोर्ट इंपोर्ट हो गई।
- partial | color: #d97706
  label: PartiallySucceeded
  text: सिर्फ़ IFMS इंपोर्ट में। कुछ रिपोर्ट इंपोर्ट हुईं और कुछ नहीं हुईं।
- failed | color: #dc2626
  label: Failed
  text: सिर्फ़ IFMS इंपोर्ट में। रन या रिपोर्ट नहीं चला। लाल मैसेज पढ़ें।
- skipped | color: #dc2626
  label: Skipped
  text: सिर्फ़ IFMS इंपोर्ट में। रिपोर्ट चलाई नहीं गई।
- disabled | color: #64748b
  label: Disabled
  text: सिर्फ़ IFMS Logins में। ऑटोमेशन यह लॉगिन इस्तेमाल नहीं करता।
- no-password | color: #dc2626
  label: No password set
  text: सिर्फ़ IFMS Logins में। अगले रन से पहले पासवर्ड भरें।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: fb58f5d6b0867738
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जिनकी आपका रोल या डेज़िग्नेशन इजाज़त देता है।

- admin | color: #374151 | roles: Admin, SuperAdmin
  title: Admin और SuperAdmin
  - इस मॉड्यूल की हर स्क्रीन खोलना, बस Admin के लिए Data Explorer को छोड़कर
  - कर्मचारियों को रजिस्टर करना और उनके डेज़िग्नेशन चुनना
  - मास्टर लिस्ट को अप-टू-डेट रखना
  - IFMS इंपोर्ट और metrics पर नज़र रखना
- superadmin | color: #7c3aed | roles: SuperAdmin
  title: सिर्फ़ SuperAdmin
  - Data Explorer इस्तेमाल करना
  - पहले से मौजूद कर्मचारियों का रीजन और HQ Excel से अपडेट करना
- corporate | color: #2563eb | roles: CorporateAdmin
  title: CorporateAdmin
  - Metrics स्क्रीन देखना
  - अगर आपका डेज़िग्नेशन देता है, तो यहाँ की दूसरी स्क्रीन खोलना
  - Data Explorer सिर्फ़ तभी इस्तेमाल करना जब आप अधिकृत (authorized) व्यक्ति हों
- others | color: #0d9488
  title: बाकी कर्मचारी
  - यहाँ की कोई स्क्रीन तभी दिखती है जब आपका डेज़िग्नेशन उसे देता है
  - आपका डेज़िग्नेशन जितनी इजाज़त देता है, उसके हिसाब से आप सिर्फ़ देख सकते हैं, या जोड़, बदल या डिलीट भी कर सकते हैं
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 6e458e262a2cfdf7
confidence: ok

- no-pages | popular: true
  q: नया कर्मचारी लॉगिन करता है पर उसे अपने काम के पेज नहीं दिखते
  a: पेज ऐक्सेस डेज़िग्नेशन से मिलता है। **Designation Master** खोलें, कर्मचारी का डेज़िग्नेशन एडिट करें और उन पेजों के लिए **View** टिक करें (ज़रूरत हो तो **Entry** या **Update** भी)। फिर **Employee Registration** में देखें कि कर्मचारी को वही डेज़िग्नेशन दिया गया है।
- upload-missing | popular: true
  q: कर्मचारी अपलोड करने पर "Missing master data — nothing was uploaded" आता है
  a: आपकी Excel फ़ाइल का कोई ज़ोन, राज्य, रीजन या HQ अभी मौजूद नहीं है। उसे **Location Master** में जोड़ें, फिर वही फ़ाइल दोबारा चुनें।
- cannot-delete
  q: मैं लोकेशन या फ़सल डिलीट नहीं कर पा रहा। "Cannot Delete" आता है
  a: यह रिकॉर्ड दूसरे रिकॉर्ड में इस्तेमाल हुआ है, जो बॉक्स में दिखते हैं। पहले उन्हें हटाएँ या बदलें, या बस इस रिकॉर्ड को **Inactive** कर दें।
- no-save
  q: मुझे मास्टर स्क्रीन दिखती है पर Save बटन नहीं है
  a: आपका डेज़िग्नेशन आपको सिर्फ़ **View** देता है। एडमिनिस्ट्रेटर से कहें कि उस पेज के लिए **Entry** (जोड़ने के लिए) या **Update** (बदलने के लिए) जोड़ दें।
- ifms-password
  q: IFMS Logins स्क्रीन पर "A password needs changing" आता है
  a: IFMS पोर्टल का पासवर्ड हर 80 दिन में खत्म (expire) हो जाता है। पहले पोर्टल पर उसे बदलें, फिर नया पासवर्ड **New password** में पेस्ट करें और **Update password** दबाएँ।
:::

::: section employee-list
title: कर्मचारियों को देखें और संभालें
routes: /EmployeeManagement
for: 
roles: 
designations: 
minutes: 2
source-hash: 4cbca19ed0bdeb5d
confidence: ok

**यह स्क्रीन किस काम की है**
**Employee Management** स्क्रीन पर हर कर्मचारी का लॉगिन उसके रोल और राज्य के साथ दिखता है। यहाँ से आप नए कर्मचारी रजिस्टर कर सकते हैं, एक साथ कई कर्मचारी अपलोड कर सकते हैं, या लॉगिन एडिट या डिलीट कर सकते हैं।

1. **Employee List** में कर्मचारी ढूँढने के लिए सर्च बॉक्स, **All Role** और **All States** का इस्तेमाल करें।
2. कॉलम पढ़ें: **Employee ID**, **Name**, **User Name**, **Password**, **Role** और **State**।
3. कर्मचारी को एडिट करने के लिए पेंसिल दबाएँ। उसकी जानकारी भरा हुआ कर्मचारी फ़ॉर्म खुल जाता है।
4. लॉगिन डिलीट करने के लिए डस्टबिन दबाएँ, फिर **Delete this role?** बॉक्स में **Delete** दबाएँ।
5. एक कर्मचारी जोड़ने के लिए **Register Employee** दबाएँ।
6. कई कर्मचारी जोड़ने हों तो Excel फ़ाइल लेने के लिए **Sample Format** दबाएँ, उसे भरें, **Bulk Upload** दबाएँ और फ़ाइल चुनें। बॉक्स में दिखता है कि कितनी लाइनें **Inserted** और कितनी **Skipped** हुईं।
7. लिस्ट बाहर निकालने के लिए टेबल के ऊपर **Copy**, **CSV**, **Excel**, **PDF** या **Print** का इस्तेमाल करें।

> Careful: डिलीट किया गया लॉगिन वापस नहीं लाया जा सकता।
> Tip: अपलोड फ़ाइल में फ़ोन नंबर ही लॉगिन का पासवर्ड बन जाता है। **Designation** खाली तभी छोड़ें जब व्यक्ति को पूरा ऐक्सेस चाहिए।

## बल्क अपलोड के कॉलम

| फ़ील्ड | क्या भरें |
|---|---|
| **EmployeeID**, **EmpName**, **UserName** | कर्मचारी की ID, नाम और लॉगिन का नाम। |
| **Permission** | रोल। ये रोल चलते हैं: AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO। |
| **Zone**, **State**, **Region**, **HQ** | AVP को ज़ोन चाहिए। SMD और SMM को राज्य चाहिए। RM और RMD को राज्य और रीजन चाहिए। MDO, MO और JMDO को राज्य, रीजन और HQ चाहिए। ये पहले से मौजूद होने चाहिए। |
| **PhoneNumber**, **EmailID** | संपर्क की जानकारी। फ़ोन नंबर ही पासवर्ड है। |
| **Designation** | किसी चालू (active) डेज़िग्नेशन के नाम से मेल खाना चाहिए। |

## आम दिक्कतें

- **"Missing master data — nothing was uploaded".** (मास्टर डेटा नहीं मिला — कुछ भी अपलोड नहीं हुआ) छूटा हुआ ज़ोन, राज्य, रीजन या HQ **Location Master** में जोड़ें, फिर वही फ़ाइल दोबारा चुनें।
- **"Issues found:"** (दिक्कतें मिलीं) कुछ लाइनें छोड़ दी गईं। हर ग्रुप के नीचे लिखी वजह पढ़ें, उन लाइनों को ठीक करें और फिर से अपलोड करें।
- **Register Employee या Bulk Upload बटन नहीं है।** आपका डेज़िग्नेशन **Employee Registration** पर **Entry** नहीं देता।
- **Update Region / HQ.** यह बटन सिर्फ़ SuperAdmin को दिखता है। यह Excel फ़ाइल से, पहले प्रीव्यू दिखाकर, पहले से मौजूद लॉगिन का सिर्फ़ रीजन और HQ बदलता है।
:::

::: section register-employee
title: कर्मचारी रजिस्टर करें या उसकी जानकारी बदलें
routes: /EmployeeRegistration
for: 
roles: 
designations: 
tour: 1 > [data-help=am-emp-role] | 2 > [data-help=am-emp-location] | 3 > [data-help=am-emp-login] | 4 > [data-help=am-emp-details] | 5 > [data-help=am-emp-save]
minutes: 3
source-hash: e7b641f98153cac9
confidence: ok

**यह स्क्रीन किस काम की है**
**Employee Registration** स्क्रीन से एक कर्मचारी का लॉगिन बनाएँ, उसे रोल और लोकेशन दें, और वह डेज़िग्नेशन चुनें जो तय करता है कि वह कौन-से पेज खोल सकता है। कर्मचारी को एडिट करते समय स्क्रीन का नाम **Edit Employee** होता है।

1. **Role & Authority** में **Role** और **Designation** चुनें।
2. **Role Specification** में वह लोकेशन चुनें जो रोल को चाहिए, जैसे **State**, **Regions** या **Headquarters**।
3. **Login Details** में **User Name** और **Password** भरें। **Status** स्विच को चालू (active) या बंद (inactive) करें।
4. **Employee Details** में **Employee Name**, **Employee ID**, **Personal Phone Number**, **Official Phone Number** और **Email ID** भरें।
5. **Save Employee** दबाएँ (एडिट करते समय **Update Employee**)। **Cancel** दबाने से बिना सेव किए वापस चले जाते हैं।

> Tip: लोकेशन वाले फ़ील्ड रोल के हिसाब से बदलते हैं। Director जैसे कुछ रोल को लोकेशन की ज़रूरत नहीं होती।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Role** | व्यक्ति का काम वाला रोल। ज़रूरी है। |
| **Designation** | पेज ऐक्सेस तय करता है। CommonRole के लिए ज़रूरी है। |
| **Zone** | AVP के लिए। |
| **State** | SMD, SMM, MDAdmin, MDHOD, RM, RMD, MO, MDO और JMDO के लिए। |
| **Regions** | एक या ज़्यादा रीजन, RM और RMD के लिए। |
| **Region** और **Headquarters** | एक रीजन और एक या ज़्यादा हेडक्वार्टर, MO, MDO और JMDO के लिए। |
| **User Name** | लॉगिन का नाम। स्पेस नहीं। ज़रूरी है। |
| **Password** | नए कर्मचारी के लिए ज़रूरी है। |
| **Personal Phone Number** | 10 अंक। ज़रूरी है। |
| **Email ID** | सही ईमेल पता। ज़रूरी है। |

## आम दिक्कतें

- **"Please correct the highlighted fields and try again."** (हाइलाइट किए गए फ़ील्ड ठीक करके फिर कोशिश करें) हर फ़ील्ड के नीचे लाल मैसेज पढ़ें और उसे ठीक करें।
- **"Username must not contain spaces or invisible characters."** (यूज़र नेम में स्पेस या न दिखने वाले अक्षर नहीं होने चाहिए) यूज़र नेम बिना स्पेस के फिर से टाइप करें।
- **"A user or employee with these details already exists."** (इस जानकारी वाला यूज़र या कर्मचारी पहले से है) किसी दूसरे लॉगिन का **User Name**, **Employee ID** या **Email ID** यही है।
- **"Selected Region does not belong to the selected State."** (चुना गया रीजन चुने गए राज्य का नहीं है) पहले राज्य चुनें, फिर उसके अंदर का रीजन चुनें।
- **Save Employee बटन नहीं है।** आपका डेज़िग्नेशन **Employee Registration** पर **Entry** (या एडिट के लिए **Update**) नहीं देता।
:::

::: section designation
title: डेज़िग्नेशन और उसका पेज ऐक्सेस बनाएँ
routes: /Designation
for: 
roles: 
designations: 
tour: 1 > [data-help=am-des-name] | 2 > [data-help=am-des-pagesearch] | 3 > [data-help=am-des-grid] | 4 > [data-help=am-des-save] | 5 > [data-help=am-des-search]
minutes: 3
source-hash: e6dc32d0910d8413
confidence: ok

**यह स्क्रीन किस काम की है**
**Designation Master** स्क्रीन तय करती है कि कोई व्यक्ति कौन-से पेज खोल सकता है और वहाँ क्या कर सकता है। हर कर्मचारी को एक डेज़िग्नेशन मिलता है।

1. **Add New Designation** में **Designation Name** टाइप करें। **Active** या **Inactive** करने के लिए स्विच पर टैप करें।
2. **Role Access** में पेज जल्दी ढूँढने के लिए **Search PageName...** में टाइप करें।
3. हर पेज के लिए **View**, **Entry**, **Update** या **Delete** टिक करें। **ALL** उस पेज के चारों बॉक्स टिक करता है। **Select All** हर पेज को टिक करता है।
4. **Save** दबाएँ (एडिट करते समय **Update**)। **Cancel** फ़ॉर्म खाली कर देता है।
5. **Existing Designations** में लिस्ट में खोजें। पेंसिल से डेज़िग्नेशन एडिट होता है। तिरछी लाइन वाला गोला उसे बंद (deactivate) करता है और तीर उसे फिर से चालू करता है।

> Tip: **View** से व्यक्ति पेज खोल सकता है। **Entry** से रिकॉर्ड जोड़ सकता है, **Update** से बदल सकता है, और **Delete** से हटा सकता है।
> Careful: बंद किया गया डेज़िग्नेशन उन यूज़र के पास बना रहता है जिनके पास वह है, पर चालू लिस्ट में नहीं दिखता।

## आम दिक्कतें

- **"Select at least one permission".** (कम से कम एक परमिशन चुनें) सेव करने से पहले कम से कम एक बॉक्स टिक करें।
- **"Designation '...' already exists".** (यह डेज़िग्नेशन पहले से है) दूसरा नाम रखें, या पहले वाले को एडिट करें।
- **मुझे Add New Designation फ़ॉर्म नहीं दिखता।** आपका डेज़िग्नेशन आपको सिर्फ़ **View** देता है।
:::

::: section location-master
title: लोकेशन की लिस्ट संभालें
routes: /LocationMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 1a434ac83feeecb3
confidence: ok

**यह स्क्रीन किस काम की है**
**Location Master** स्क्रीन में ज़ोन, राज्य, ज़िले, तहसील (sub-districts), रीजन और हेडक्वार्टर रखे जाते हैं। कर्मचारी, डीलर और रिपोर्ट इन लिस्ट का इस्तेमाल करते हैं।

1. एक टैब चुनें: **Zones**, **States**, **Districts**, **Sub-Districts**, **Regions** या **Headquarters**।
2. फ़ॉर्म भरें, जैसे **Zone Name**, **State Name** और **Zone**, या **Headquarter Name** और **Region**।
3. स्विच को **Active** या **Inactive** करें और **Save** दबाएँ। **Cancel** फ़ॉर्म खाली कर देता है।
4. कई रिकॉर्ड जोड़ने हों तो **Sample Format** दबाएँ, Excel फ़ाइल भरें, **Bulk Upload** दबाएँ और फ़ाइल चुनें।
5. टेबल में खोजें या स्टेटस चुनें। स्टेटस बदलने के लिए उस पर टैप करें, एडिट के लिए पेंसिल, या डिलीट के लिए डस्टबिन पर टैप करें।

> Tip: इस क्रम में जोड़ें: पहले ज़ोन, फिर राज्य, फिर रीजन, फिर हेडक्वार्टर। ज़िला किसी राज्य में आता है और तहसील किसी ज़िले में।
> Careful: डिलीट किया गया वापस नहीं आता। जो लोकेशन दूसरे रिकॉर्ड में इस्तेमाल हुई है, उसे डिलीट नहीं किया जा सकता।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Zone Name**, **Zone Code**, **Zone Color Code** | ज़ोन और नक्शे पर उसका रंग। नाम ज़रूरी है। |
| **State Name**, **State Code**, **Zone** | राज्य और वह ज़ोन जिसमें राज्य है। नाम और ज़ोन ज़रूरी हैं। |
| **District Name**, **State** | दोनों ज़रूरी हैं। |
| **Sub-District Name**, **District** | दोनों ज़रूरी हैं। **State** बस ज़िला ढूँढने में मदद करता है। |
| **Region Name**, **Region Code**, **State** | नाम और राज्य ज़रूरी हैं। |
| **Headquarter Name**, **Region** | दोनों ज़रूरी हैं। |

## आम दिक्कतें

- **"Zone is required."** या **"Region is required."** (ज़ोन / रीजन ज़रूरी है) लिस्ट से ऊपर वाली (parent) लोकेशन चुनें।
- **"Cannot Delete".** (डिलीट नहीं हो सकता) यह रिकॉर्ड कहीं और इस्तेमाल हुआ है। पहले उन रिकॉर्ड को बदलें, या इसे **Inactive** कर दें।
- **बल्क अपलोड में "Skipped Groups:" या "Issues:" दिखता है।** वजहें पढ़ें, उन्हें रखने के लिए **Copy** दबाएँ, फ़ाइल ठीक करें और फिर से अपलोड करें।
:::

::: section agriculture-master
title: फ़सल और प्रोडक्ट की लिस्ट संभालें
routes: /Agriculture
for: 
roles: 
designations: 
minutes: 3
source-hash: 9d6cd17408623739
confidence: ok

**यह स्क्रीन किस काम की है**
**Agriculture & Product Master** स्क्रीन में फ़सलें, फ़सल के प्रकार, फ़सल की अवस्थाएँ, मुकाबले वाली कंपनियाँ, सेक्टर, यूनिट, कैटेगरी, प्रोडक्ट ग्रुप और प्रोडक्ट रखे जाते हैं। दूसरी स्क्रीन इन लिस्ट का इस्तेमाल करती हैं।

1. एक टैब चुनें: **Crops**, **Crop Types**, **Crop Stages**, **Competitors**, **Sector**, **Units**, **Categories**, **Product Groups** या **Products**।
2. फ़ॉर्म भरें। **Crop Types** और **Crop Stages** के लिए पहले **Crop** चुनें। **Products** के लिए **Category** चुनें, और अगर पता हो तो **Product Group** और **Rate Per Unit (RPU)** भरें।
3. स्विच को **Active** या **Inactive** करें और **Save** दबाएँ। **Cancel** फ़ॉर्म खाली कर देता है।
4. कई रिकॉर्ड जोड़ने हों तो **Sample Format** दबाएँ, उसे भरें, **Bulk Upload** दबाएँ और फ़ाइल चुनें। बॉक्स में जुड़ी, अपडेट हुई और रिजेक्ट हुई लाइनें दिखती हैं।
5. टेबल में खोजें या स्टेटस चुनें। स्टेटस बदलने के लिए उस पर टैप करें, एडिट के लिए पेंसिल, या डिलीट के लिए डस्टबिन पर टैप करें।

> Tip: पहले यूनिट बनाएँ, फिर कैटेगरी, और फिर प्रोडक्ट।
> Careful: डिलीट किया गया वापस नहीं आता। जो रिकॉर्ड कहीं और इस्तेमाल हुआ है, उसे डिलीट नहीं किया जा सकता।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Category Name** और **Unit** | दोनों ज़रूरी हैं। दूसरा स्विच कैटेगरी को **Speciality** या **Regular** मार्क करता है। |
| **Crop** | वह फ़सल जिसका यह फ़सल-प्रकार या फ़सल-अवस्था है। ज़रूरी है। |
| **Product Name** और **Category** | दोनों ज़रूरी हैं। एक कैटेगरी में एक प्रोडक्ट का नाम एक ही बार इस्तेमाल हो सकता है। |
| **Rate Per Unit (RPU)** | प्रोडक्ट का रेट। ज़रूरी नहीं। |

## आम दिक्कतें

- **"Please select an active crop."** (कोई चालू फ़सल चुनें) यह फ़सल बंद है। पहले **Crops** टैब में उसे चालू करें।
- **"... already exists".** (पहले से है) इस नाम का रिकॉर्ड पहले से है। उसकी जगह उसे एडिट करें।
- **Sample Format या Bulk Upload बटन नहीं है।** ये बटन **Crop Types** और **Crop Stages** पर नहीं होते, या आपके डेज़िग्नेशन में **Entry** नहीं है।
:::

::: section financial-master
title: बैंक और वित्त वर्ष की लिस्ट संभालें
routes: /Financial
for: 
roles: 
designations: 
minutes: 2
source-hash: 149843a1d578c0f3
confidence: ok

**यह स्क्रीन किस काम की है**
**Financial Master** स्क्रीन में वे बैंक और वित्त वर्ष रखे जाते हैं जो दूसरे फ़ॉर्म में इस्तेमाल होते हैं।

1. **Bank** या **Financial Year** टैब चुनें।
2. बैंक के लिए **Bank Name** और **IFSC Prefix** भरें, और अगर आपके पास हो तो **Bank Logo** जोड़ें। वित्त वर्ष के लिए **Financial Year Name**, **Start Date** और **End Date** भरें।
3. स्विच को **Active** या **Inactive** करें और **Save** दबाएँ। **Cancel** फ़ॉर्म खाली कर देता है।
4. बैंक कार्ड के रूप में दिखते हैं, और नीचे **Show more** होता है। वित्त वर्ष पेज वाली टेबल में दिखते हैं।
5. स्टेटस बदलने के लिए उस पर टैप करें, एडिट के लिए पेंसिल, या डिलीट के लिए डस्टबिन पर टैप करें।

> Careful: डिलीट किया गया वापस नहीं आता।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **IFSC Prefix** | IFSC कोड के सिर्फ़ पहले 4 अक्षर, जैसे HDFC या SBIN। ज़रूरी है। |
| **Bank Logo** | 512 KB से छोटी फ़ोटो। ज़रूरी नहीं। |

## आम दिक्कतें

- **"IFSC Prefix is required."** (IFSC Prefix ज़रूरी है) IFSC कोड के पहले 4 अक्षर भरें।
- **"Logo file must be under 512 KB."** (लोगो फ़ाइल 512 KB से छोटी होनी चाहिए) छोटी फ़ोटो चुनें।
- **Bulk Upload दबाने पर कुछ नहीं होता।** इस स्क्रीन पर बल्क अपलोड अभी तैयार नहीं है। रिकॉर्ड एक-एक करके जोड़ें।
:::

::: section relationship-master
title: रिश्तों की लिस्ट संभालें
routes: /Relationship
for: 
roles: 
designations: 
minutes: 1
source-hash: dfed886a1ac19a35
confidence: ok

**यह स्क्रीन किस काम की है**
**Relationship Master** स्क्रीन में रिश्तों की लिस्ट रखी जाती है, जैसे डीलर की जानकारी में जिस व्यक्ति का नाम है, उससे रिश्ता।

1. **Add New Relationship** में **Relationship Name** टाइप करें।
2. स्विच को **Active** या **Inactive** करें और **Save** दबाएँ। **Cancel** फ़ॉर्म खाली कर देता है।
3. **Existing Relationships** में खोजें या स्टेटस चुनें।
4. स्टेटस बदलने के लिए उस पर टैप करें, एडिट के लिए पेंसिल, या डिलीट के लिए डस्टबिन पर टैप करें।

> Careful: डिलीट किया गया वापस नहीं आता।

## आम दिक्कतें

- **"Relationship name is required."** (रिश्ते का नाम ज़रूरी है) पहले नाम टाइप करें।
- **Bulk Upload दबाने पर कुछ नहीं होता।** इस स्क्रीन पर बल्क अपलोड अभी तैयार नहीं है। रिकॉर्ड एक-एक करके जोड़ें।
:::

::: section plant-dealer-nature
title: प्लांट और डीलर नेचर की लिस्ट संभालें
routes: /PlantDealerNatureMaster
for: 
roles: 
designations: 
minutes: 1
source-hash: 99cbbff4aa8ad460
confidence: ok

**यह स्क्रीन किस काम की है**
**Plant & Dealer Nature Master** स्क्रीन में वे प्लांट और डीलर नेचर (डीलर का प्रकार) रखे जाते हैं जो Excel अपलोड और डीलर के रिकॉर्ड में इस्तेमाल होते हैं।

1. **Plants** या **Dealer Natures** टैब चुनें।
2. **Plant Name** या **Dealer Nature Name** टाइप करें।
3. स्विच को **Active** या **Inactive** करें और **Save** दबाएँ। **Cancel** फ़ॉर्म खाली कर देता है।
4. टेबल में खोजें या स्टेटस चुनें। स्टेटस बदलने के लिए उस पर टैप करें, एडिट के लिए पेंसिल, या डिलीट के लिए डस्टबिन पर टैप करें।

> Careful: डिलीट किया गया वापस नहीं आता।

## आम दिक्कतें

- **"Plant name is required."** (प्लांट का नाम ज़रूरी है) पहले नाम टाइप करें।
- **स्टेटस पर टैप करने से वह नहीं बदलता।** आपके डेज़िग्नेशन में इस पेज पर **Update** नहीं है।
:::

::: section expense-master
title: खर्चों की लिस्ट संभालें
routes: /ExpenseInformation
for: 
roles: 
designations: 
minutes: 1
source-hash: cbd7b43eecb288ef
confidence: ok

**यह स्क्रीन किस काम की है**
**Expense Information Master** स्क्रीन में खर्चों के नामों की लिस्ट रखी जाती है।

1. **Add New Expense Information** में **Expense Name** टाइप करें।
2. स्विच को **Active** या **Inactive** करें और **Save** दबाएँ। **Cancel** फ़ॉर्म खाली कर देता है।
3. **Existing Expense Informations** में खोजें या स्टेटस चुनें।
4. स्टेटस बदलने के लिए उस पर टैप करें, एडिट के लिए पेंसिल, या डिलीट के लिए डस्टबिन पर टैप करें।

> Careful: डिलीट किया गया वापस नहीं आता।

## आम दिक्कतें

- **"Expense name is required."** (खर्च का नाम ज़रूरी है) पहले नाम टाइप करें।
:::

::: section gps-settings
title: GPS ट्रैकिंग का इंटरवल सेट करें
routes: /GpsSettings
for: 
roles: 
designations: 
minutes: 1
source-hash: 2b7d216628a0423f
confidence: ok

**यह स्क्रीन किस काम की है**
**GPS Settings** स्क्रीन तय करती है कि लाइव ट्रैकिंग के दौरान JMDO मोबाइल ऐप कितनी-कितनी देर में GPS लोकेशन रिकॉर्ड करता है।

1. **Current Saved Interval**, **System Default** (20 s) और **Allowed Range** (1 से 100 सेकंड) देखें।
2. स्लाइडर खिसकाएँ, या बॉक्स में सेकंड की संख्या टाइप करें।
3. **Save Settings** दबाएँ।
4. वापस 20 सेकंड पर जाने के लिए **Reset to Default (20s)** दबाएँ और फिर **Save Settings** दबाएँ।

> Tip: छोटी संख्या रखने पर लोकेशन ज़्यादा बार रिकॉर्ड होती है पर बैटरी ज़्यादा खर्च होती है। बड़ी संख्या रखने पर बैटरी बचती है।

## आम दिक्कतें

- **"Tracking interval must be an integer between 1 and 100 seconds."** (इंटरवल 1 से 100 सेकंड के बीच पूरी संख्या होनी चाहिए) 1 से 100 तक की पूरी संख्या भरें।
- **Save Settings बटन नहीं है।** आपके डेज़िग्नेशन में **GPS Settings** पर **Update** नहीं है।
:::

::: section csr-exceeding-day
title: CSR कट-ऑफ़ दिन सेट करें
routes: /CSRExceedingDaySetting
for: 
roles: 
designations: 
minutes: 2
source-hash: 94dea6d5ea8fe4d7
confidence: ok

**यह स्क्रीन किस काम की है**
**CSR Exceeding Day Setting** स्क्रीन पर महीने का वह दिन तय होता है जिसके बाद CSR-1 और CSR-2 को मंज़ूरी के लंबे रास्ते से जाना पड़ता है। एक समय में सिर्फ़ एक सेटिंग चालू रहती है।

1. **CSR1 Day** और **CSR2 Day** भरें (1 से 31)।
2. **Status** स्विच को **Active** करें और **Save** दबाएँ। **Cancel** फ़ॉर्म खाली कर देता है।
3. **CSR Exceeding Day Settings** में सारी सेटिंग्स देखें। स्टेटस बदलने के लिए उस पर टैप करें, एडिट के लिए पेंसिल, या डिलीट के लिए डस्टबिन पर टैप करें।

> Tip: महीने के **CSR1 Day** के बाद बना CSR-1, या **CSR2 Day** के बाद सबमिट हुआ CSR-2, लंबी मंज़ूरी से गुज़रता है।
> Careful: कोई चालू सेटिंग सेव करने पर पुरानी चालू सेटिंग बंद हो जाती है।

## आम दिक्कतें

- **"CSR1 Day must be between 1 and 31."** (CSR1 Day 1 से 31 के बीच होना चाहिए) 1 से 31 तक का दिन भरें।
- **"You do not have permission to save this setting."** (आपको यह सेटिंग सेव करने की इजाज़त नहीं है) आपके डेज़िग्नेशन में इस पेज पर **Entry** या **Update** नहीं है।
:::

::: section metrics
title: देखें कि ऐप कैसे इस्तेमाल हो रहा है
routes: /Metrics
for: 
roles: 
designations: 
minutes: 2
source-hash: 42e4f35d4ca253b9
confidence: ok

**यह स्क्रीन किस काम की है**
**Metrics** स्क्रीन दिखाती है कि SPIC ONE कौन इस्तेमाल कर रहा है, किस ऐप पर, कौन-से पेज और सर्विस इस्तेमाल हो रहे हैं, और क्या गड़बड़ हुई। यहाँ सिर्फ़ आँकड़े दिखते हैं; एरर मार्क करने के अलावा यहाँ से कुछ नहीं बदलता।

1. समय चुनें: **7 days**, **30 days** या **90 days**। कोई ऐप चुनें, या **All apps** ही रहने दें।
2. ऊपर की लाइव लाइन पढ़ें: पिछले 15 मिनट में ऑनलाइन लोग, और पिछले एक घंटे की रिक्वेस्ट और एरर।
3. एक टैब खोलें: **Overview**, **Users**, **Pages**, **API** या **Errors**।
4. **Errors** पर खोजने के लिए, या **Source** और **Status** (**Open**, **Resolved** या **All**) चुनने के लिए **Filters** खोलें।
5. किसी एरर की जानकारी देखने के लिए उस पर टैप करें। ठीक हो जाने पर **Mark resolved** दबाएँ, या फिर से खोलने के लिए **Reopen** दबाएँ।

## आम दिक्कतें

- **"You do not have access to Metrics".** (आपको Metrics का ऐक्सेस नहीं है) एडमिनिस्ट्रेटर से कहें कि Metrics पेज आपके डेज़िग्नेशन में जोड़ दें।
- **"The metrics could not be loaded".** (metrics लोड नहीं हो सके) कनेक्शन देखें और **Try again** दबाएँ।
- **"Live numbers unavailable".** (लाइव आँकड़े नहीं मिले) लाइव आँकड़े पढ़े नहीं जा सके। ये हर मिनट रिफ़्रेश होते हैं।
:::

::: section ifms-auto-import
title: रात वाले IFMS इंपोर्ट पर नज़र रखें
routes: /IfmsAutoImport
for: 
roles: 
designations: 
minutes: 2
source-hash: cc12211b4d92d31d
confidence: ok

**यह स्क्रीन किस काम की है**
**IFMS Auto Import** स्क्रीन पर IFMS पोर्टल (dbtfert.nic.in) से हर रात होने वाला रिपोर्ट का डाउनलोड और SPIC ONE में उनका इंपोर्ट दिखता है। यह 04:05 पर अपने-आप चलता है।

1. आख़िरी रन के कार्ड देखें: **Last run**, **Reports**, **Rows added**, **Rows updated** और **Login**।
2. हर रन पर उसकी तारीख, कितनी रिपोर्ट इंपोर्ट हुईं, और **Succeeded** या **Failed** जैसा स्टेटस दिखता है।
3. हर रिपोर्ट को उसके **Status**, **Rows**, **New**, **Updated** और **Skipped** के साथ देखने के लिए किसी रन पर टैप करें।
4. अगर पीला CAPTCHA बॉक्स दिखे, तो जो अक्षर दिख रहे हैं उन्हें टाइप करें और **Send** दबाएँ। फिर डाउनलोड आगे चलता है।
5. तुरंत रन शुरू करने के लिए **Run now** दबाएँ, या नए नतीजे देखने के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **"No runs yet".** (अभी कोई रन नहीं हुआ) पहला रन 04:05 पर शुरू होता है, या **Run now** दबाएँ।
- **"Could not queue a run."** (रन शुरू नहीं हो सका) मैसेज पढ़ें और थोड़ी देर बाद फिर कोशिश करें।
- **CAPTCHA पर "expired" लिखा आता है।** नई तस्वीर अपने-आप आ जाती है। नई वाली टाइप करें।
- **Login कार्ड पर "failed" लिखा है।** हो सकता है IFMS पासवर्ड खत्म (expire) हो गया हो। **IFMS Logins** देखें।
:::

::: section ifms-logins
title: IFMS पोर्टल के लॉगिन संभालें
routes: /IfmsLogins
for: 
roles: 
designations: 
minutes: 2
source-hash: 7e0020b6083c5518
confidence: ok

**यह स्क्रीन किस काम की है**
**IFMS Logins** स्क्रीन पर हर कंपनी का वह पोर्टल लॉगिन रखा जाता है जिसे रात वाला IFMS इंपोर्ट इस्तेमाल करता है। पासवर्ड सुरक्षित रखे जाते हैं और फिर कभी दिखाए नहीं जाते।

1. हर कंपनी के कार्ड पर **Disabled**, **No password set** जैसा टैग, या पासवर्ड खत्म होने में कितने दिन बचे हैं, यह दिखता है।
2. **Password set**, **Expires**, **Last login** और **Result** पढ़ें।
3. जब पोर्टल पर पासवर्ड बदले, तो नया पासवर्ड **New password** में पेस्ट करें, **OTP goes to** देखें, और **Update password** दबाएँ।
4. किसी लॉगिन का इस्तेमाल रोकने के लिए **Disable** दबाएँ, या फिर से इस्तेमाल करने के लिए **Enable** दबाएँ।
5. कंपनी जोड़ने के लिए **Add a login** में **Key**, **Company**, **Username** और **Password** भरें और **Add** दबाएँ।

> Careful: पोर्टल का पासवर्ड हर 80 दिन में खत्म हो जाता है। पहले पोर्टल पर बदलें, फिर यहाँ, वरना रात का रन फ़ेल हो जाएगा।

## आम दिक्कतें

- **"Enter the new password first."** (पहले नया पासवर्ड भरें) **Update password** दबाने से पहले नया पासवर्ड पेस्ट करें।
- **"Key, username and password are all needed."** (Key, यूज़र नेम और पासवर्ड तीनों चाहिए) लॉगिन जोड़ने के लिए तीनों भरें।
:::

::: section data-explorer
title: डेटा को सीधे देखें और ठीक करें
routes: /DataExplorer
for: 
roles: SuperAdmin, CorporateAdmin
designations: 
minutes: 3
source-hash: fb55adf5f6eab06e
confidence: ok

**यह स्क्रीन किस काम की है**
**Data Explorer** स्क्रीन पर अधिकृत व्यक्ति डेटाबेस की किसी भी टेबल की लाइनें देख, फ़िल्टर, एक्सपोर्ट और ठीक कर सकता है। हर बदलाव आपके नाम के साथ ऑडिट लॉग में लिखा जाता है। इसके लिए अलग पासवर्ड चाहिए।

1. Data Explorer का पासवर्ड भरें और **Unlock** दबाएँ।
2. **Tables** पर खोजें और एक टेबल चुनें। **Filter (any column contains)** में टाइप करें और Enter दबाएँ। क्रम से लगाने (sort) के लिए कॉलम के नाम पर टैप करें।
3. किसी लाइन को एडिट करने के लिए उस पर टैप करें, फिर **Save** या **Delete** दबाएँ। नई लाइन जोड़ने के लिए **New row** और **Insert** दबाएँ। **Export CSV** लाइनें डाउनलोड करता है।
4. **Query** पर एक SQL स्टेटमेंट टाइप करें और **Run** दबाएँ। अगर कुछ बदलना है तो पहले उसका ट्रायल (dry run) दिखता है; उसे लागू करने के लिए **Confirm: change ... row(s)** दबाएँ।
5. हाल के बदलाव देखने के लिए **Audit log** खोलें। काम खत्म होने पर **Lock** दबाएँ।

> Careful: यहाँ के बदलाव सीधे लाइव डेटा में जाते हैं और वापस नहीं किए जा सकते। जहाँ हो सके, सामान्य स्क्रीन ही इस्तेमाल करें।

## आम दिक्कतें

- **"Data Explorer is available to SuperAdmin or the authorized CorporateAdmin only."** (Data Explorer सिर्फ़ SuperAdmin या अधिकृत CorporateAdmin के लिए है) आपके लॉगिन को यह स्क्रीन इस्तेमाल करने की इजाज़त नहीं है।
- **Save या Delete नहीं हो रहा।** key ठीक एक ही लाइन से मेल खानी चाहिए।
:::
