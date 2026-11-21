---
module: sub-dealers
title: सब डीलर
icon: bi-people
order: 103
language: hi
category: डीलर
note: नई गाइड
meta-source-hash: 95f84553d50218f7
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 0365bef89cdeeedd
confidence: ok

हर सब डीलर का सही रिकॉर्ड रखें: दुकान कहाँ है, उनसे कैसे संपर्क करें, और उनकी GST, PAN और mFMS की जानकारी। इसी मॉड्यूल में सब डीलर और डीलर के कर्मचारियों के कल्याण योजना वाले लाभार्थी भी रखे जाते हैं।

- import | icon: bi-file-earmark-excel | color: #8b5cf6
  title: Admin मास्टर अपलोड करता है
  text: Admin हर State, Region और HQ के लिए सब डीलर कोड की मंज़ूर Excel लिस्ट इम्पोर्ट करता है।
- maintain | icon: bi-pencil-square | color: #0d9488
  title: MO जानकारी भरता है
  text: MO अपने HQ का हर सब डीलर चुनता है और जगह, संपर्क, GST, PAN और mFMS भरता है।
- welfare | icon: bi-heart | color: #d97706
  title: कल्याण योजना के लाभार्थी
  text: सब डीलर और कर्मचारियों के लाभार्थियों को पहले SM और फिर AVP मंज़ूरी देते हैं।
:::

::: section flow
title: सब डीलर का रिकॉर्ड कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 0b02b6ac4a67732f
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- admin | type: lane | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin और ऑफ़िस
  sub: हेड ऑफ़िस
- field | type: lane | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: आपका HQ
- sm | type: lane | color: #d97706 | roles: SMD, SMM
  title: SM
  sub: SMD · SMM
- avp | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: आख़िरी मंज़ूरी
- import | lane: admin | col: 1 | icon: bi-file-earmark-excel | color: #8b5cf6 | next: maintain | screen: import-master
  title: मास्टर इम्पोर्ट करें
  text: Admin मंज़ूर Excel लिस्ट अपलोड करता है। जब तक कोई उनकी जानकारी सेव नहीं करता, नए सब डीलर लिस्ट में नहीं दिखते।
- maintain | lane: field | col: 2 | icon: bi-pencil-square | color: #16a34a | next: beneficiary | screen: maintain-sub-dealer
  title: जानकारी भरें
  status: Active
  text: MO सब डीलर चुनता है, जगह, संपर्क, GST, PAN और mFMS भरता है, और **Submit** दबाता है। अब यह **Sub Dealer Master** लिस्ट में दिखता है।
- beneficiary | lane: admin | col: 3 | icon: bi-person-plus | color: #d97706 | next: sm-approve | screen: beneficiary-add
  title: लाभार्थी जोड़ें
  status: Pending
  text: **Sub Dealer & Employee** पर ऑफ़िस के कर्मचारी सब डीलर या डीलर के कर्मचारी का कल्याण योजना वाला लाभार्थी जोड़ते हैं।
- sm-approve | lane: sm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: avp-approve | screen: beneficiary-approve
  title: SM की मंज़ूरी
  status: Pending
  text: SMD या SMM रिकॉर्ड जाँचता है और उसे मंज़ूर या रिजेक्ट करता है।
- avp-approve | lane: avp | col: 5 | icon: bi-patch-check | color: #2563eb | screen: beneficiary-approve
  title: AVP की मंज़ूरी
  status: Approved
  text: AVP आख़िरी मंज़ूरी देता है। अब कल्याण योजना के लिए आवेदन करते समय डीलर इस व्यक्ति को चुन सकता है।
- rejected | lane: sm | col: 5 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: sm-approve | screen: beneficiary-approve
  title: रिजेक्ट हुआ
  status: SM Rejected
  text: अगर SM या AVP रिकॉर्ड रिजेक्ट कर दे, तो वह वहीं रुक जाता है और डीलर को चुनने के लिए नहीं मिलता।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9462538c97a0b6f7
confidence: ok

- active | color: #16a34a
  label: Active
  text: सब डीलर या लाभार्थी इस्तेमाल में है। Active सब डीलर के लिए * वाला हर फ़ील्ड भरना ज़रूरी है।
- inactive | color: #64748b
  label: Inactive
  text: अभी इस्तेमाल में नहीं है। Inactive सब डीलर के लिए सिर्फ़ State, Region, HQ और Firm Name चाहिए।
- pending | color: #d97706
  label: Pending
  text: लाभार्थी SM या AVP की मंज़ूरी का इंतज़ार कर रहा है।
- approved | color: #2563eb
  label: Approved
  text: इस लेवल पर मंज़ूर हो गया। Overall कॉलम में इसका मतलब है कि SM और AVP दोनों ने मंज़ूरी दे दी।
- rejected | color: #dc2626
  label: Rejected
  text: इस लेवल पर रिजेक्ट हुआ। यह SM Status या AVP Status कॉलम में दिखता है।
- sm-rejected | color: #dc2626
  label: SM Rejected
  text: SM ने लाभार्थी को रिजेक्ट कर दिया। यह AVP के पास नहीं जाता।
- avp-rejected | color: #b91c1c
  label: AVP Rejected
  text: SM ने मंज़ूर किया, लेकिन AVP ने लाभार्थी को रिजेक्ट कर दिया।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e89557ba6534118
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- admin | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - Excel से सब डीलर मास्टर इम्पोर्ट करना
  - किसी भी सब डीलर की जानकारी भरना या ठीक करना
  - सब डीलर और कर्मचारियों के लाभार्थी जोड़ना
- field | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - अपने HQ के सब डीलर की जानकारी भरना और अपडेट करना
  - सेव की गई जानकारी गलत हो तो उसे मिटाना
- rm | color: #64748b | roles: RM, RMD
  title: RM, RMD
  - अपने रीजन के सब डीलर देखना
  - लिस्ट को Excel में एक्सपोर्ट करना
- sm | color: #d97706 | roles: SMD, SMM
  title: SMD, SMM
  - अपने राज्य के सब डीलर देखना
  - लाभार्थियों को पहली मंज़ूरी देना या रिजेक्ट करना
- avp | color: #2563eb | roles: AVP, Director
  title: AVP और Director
  - अपने इलाके के सब डीलर की जानकारी भरना या अपडेट करना
  - AVP लाभार्थियों को आख़िरी मंज़ूरी देता है
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 3f822e638d6491b2
confidence: ok

- not-in-dropdown | popular: true
  q: Select Imported Sub Dealer में मुझे सब डीलर नहीं मिल रहा
  a: फ़ॉर्म में सिर्फ़ वही सब डीलर दिखते हैं जो Admin ने आपके State, Region और HQ के लिए इम्पोर्ट किए हैं। फ़ॉर्म पर नया कोड नहीं बनाया जा सकता। Admin से कहें कि उसे मास्टर में जोड़ दे।
- list-empty | popular: true
  q: Admin ने सब डीलर अपलोड कर दिए, फिर भी Sub Dealer Master लिस्ट खाली है
  a: लिस्ट में सिर्फ़ वही सब डीलर दिखते हैं जिनकी जानकारी सेव हो चुकी है। **Maintain Sub Dealer** दबाएँ, सब डीलर चुनें, जानकारी भरें और **Submit** दबाएँ।
- submit-grey
  q: Submit बटन ग्रे है
  a: पहले सब डीलर चुनें और उसकी जानकारी लोड होने तक रुकें। जब GST PDF, लोकेशन या पिनकोड पढ़ा जा रहा हो, तब भी रुकें।
- edit-grey
  q: लिस्ट पर पेंसिल और डस्टबिन आइकन ग्रे हैं
  a: आपका रोल सिर्फ़ सब डीलर देख सकता है। MO, MDO, JMDO, Admin, AVP और Director इन्हें बदल सकते हैं।
- no-approve
  q: मैं मंज़ूरी देने वाला हूँ, पर मुझे लाभार्थी पर टिक या क्रॉस नहीं दिख रहा
  a: SM को ये तभी दिखते हैं जब **SM Status** **Pending** हो। AVP को ये तभी दिखते हैं जब SM मंज़ूर कर चुका हो और **AVP Status** **Pending** हो।
:::

::: section sub-dealer-list
title: सब डीलर की लिस्ट देखें
routes: /SubDealerList
for: 
roles: 
designations: 
tour: 1 > [data-help=sdl-cards] | 2 > [data-help=sdl-filters] | 3 > [data-help=sdl-table] | 4 > [data-help=sdl-actions] | 5 > [data-help=sdl-summary]
minutes: 2
source-hash: eb1071bb27a63554
confidence: ok

**यह स्क्रीन किस काम की है**
**Sub Dealer Master** स्क्रीन पर वे सब डीलर दिखते हैं जिनकी जानकारी सब डीलर फ़ॉर्म से सेव हो चुकी है। MO को अपना HQ दिखता है, RM को रीजन और SMM या SMD को राज्य। ऊपर एक नोट बताता है कि आपको कौन सा इलाका दिख रहा है।

1. ऊपर के कार्ड देखें: **Total Sub Dealers**, **Active** और **Inactive**।
2. **Search** में कोड, नाम, मोबाइल, GST, PAN या mFMS ID टाइप करें। लिस्ट छोटी करने के लिए **State**, **Region**, **HQ** या **Status** चुनें। आपका अपना इलाका पहले से चुना और लॉक होता है।
3. **Action** कॉलम में आँख वाला आइकन सब डीलर को देखने के लिए खोलता है। पेंसिल उसे बदलने के लिए खोलती है। डस्टबिन उसकी सेव की गई जानकारी मिटाता है।
4. ऊपर के बटन इस्तेमाल करें। **Refresh** लिस्ट फिर से लोड करता है। **Export All Saved** सेव किए गए सारे सब डीलर Excel में डाउनलोड करता है। **Maintain Sub Dealer** फ़ॉर्म खोलता है।
5. नीचे **State-wise Sub Dealer Summary** तक स्क्रोल करें। इसमें हर राज्य के मास्टर रिकॉर्ड की तुलना उसके सेव किए गए **Active** और **Inactive** सब डीलर से होती है।

> Careful: डस्टबिन सिर्फ़ सेव की गई जानकारी हटाता है, सब डीलर को नहीं। जब तक कोई फिर से जानकारी नहीं भरता, वह इस लिस्ट से हट जाता है।

## आम दिक्कतें

- **"No saved Sub Dealers found".** (सेव किया हुआ कोई सब डीलर नहीं मिला) यहाँ सिर्फ़ वही सब डीलर दिखते हैं जिनकी जानकारी सेव है। Admin के अपलोड की जो लाइनें किसी ने नहीं भरीं, वे छिपी रहती हैं।
- **पेंसिल और डस्टबिन ग्रे हैं।** आपका रोल सिर्फ़ देख सकता है। MO, MDO, JMDO, Admin, AVP और Director सब डीलर बदल सकते हैं।
- **Maintain Sub Dealer बटन नहीं है।** आपका रोल सिर्फ़ लिस्ट देख सकता है।
- **"No saved Sub Dealer records are available to export."** (एक्सपोर्ट करने के लिए सेव किया हुआ कोई रिकॉर्ड नहीं है) आपके इलाके में अभी कुछ भी सेव नहीं हुआ है।
:::

::: section maintain-sub-dealer
title: सब डीलर की जानकारी भरें या अपडेट करें
routes: /SubDealerRegistration
for: 
roles: 
designations: 
tour: 1 > [data-help=sdr-business] | 2 > [data-help=sdr-basic] | 3 > [data-help=sdr-location] | 4 > [data-help=sdr-tax] | 5 > [data-help=sdr-mfms] | 6 > [data-help=sdr-submit]
minutes: 5
source-hash: e410f07c765072a5
confidence: ok

**यह स्क्रीन किस काम की है**
जिस सब डीलर को Admin पहले ही इम्पोर्ट कर चुका है, उसकी जानकारी भरने के लिए **Sub Dealer Maintenance** स्क्रीन का इस्तेमाल करें। यहाँ नया सब डीलर कोड नहीं बनाया जा सकता। इसे लिस्ट पर **Maintain Sub Dealer** से, या किसी लाइन की पेंसिल से खोलें।

1. **Business Information** में **Select Imported Sub Dealer** पर टैप करें, कोड या नाम टाइप करें और सब डीलर चुनें। उसकी सेव की गई जानकारी लोड हो जाती है। **Firm Name** (GST के ट्रेड नाम के हिसाब से) जाँच लें।
2. **Basic Information** में **State**, **Region** और **HQ** जाँचें, और **Status** चुनें: **Active** या **Inactive**।
3. **Primary Location** में **Use Current Location** दबाएँ, मैप पर टैप करें, या Google Maps का लिंक पेस्ट करें। पता भरें। 6 अंकों का **Pincode** टाइप करने पर State, District, Block, Taluk और Village अपने आप भर जाते हैं। **Official Contact Number**, **WhatsApp Number** और **Alternative Number** भरें।
4. **Tax & Compliance** में **Upload GST Certificate (PDF)** पर टैप करें। ऐप PDF पढ़कर **GST Number**, **Legal Name**, **Trade Name** और **Constitution of Business** भर देता है। इन्हें जाँचें, फिर **PAN Number** भरें।
5. **mFMS Information** में **Retail mFMS ID** भरें। **Wholesale mFMS ID** भरना ज़रूरी नहीं है।
6. **Submit** दबाएँ। एक हरा मैसेज बताता है कि सब डीलर सेव हो गया, और अगले के लिए फ़ॉर्म खाली हो जाता है।

> Tip: **Active** सब डीलर के लिए * वाला हर फ़ील्ड भरना ज़रूरी है। **Inactive** के लिए सिर्फ़ State, Region, HQ और Firm Name चाहिए।
> Careful: आँख वाले आइकन से सब डीलर खोलने पर हर फ़ील्ड ग्रे होता है और **Submit** बटन नहीं होता। बदलने के लिए पेंसिल का इस्तेमाल करें।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Select Imported Sub Dealer** | Admin के अपलोड वाला सब डीलर। ज़रूरी है। |
| **Status** | अगर सब डीलर अभी काम कर रहा है तो **Active**, नहीं तो **Inactive**। |
| **Google Map URL** और **Latitude & Longitude** | दुकान की जगह। Active के लिए ज़रूरी। |
| **Pincode** | 6 अंक। इससे जगहों के नाम अपने आप भर जाते हैं। |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 अंकों के मोबाइल नंबर। Active के लिए ज़रूरी। |
| **GST Number** | 15 अक्षर, जैसे 22AAAAA0000A1Z5। Active के लिए ज़रूरी। |
| **Upload GST Certificate (PDF)** | एक PDF फ़ाइल, 5 MB तक। Active के लिए ज़रूरी। |
| **PAN Number** | 10 अक्षर, जैसे ABCDE1234F। Active के लिए ज़रूरी। |
| **Retail mFMS ID** | Active के लिए ज़रूरी। |

## आम दिक्कतें

- **"Please correct the highlighted fields."** (हाइलाइट किए गए फ़ील्ड ठीक करें) हर फ़ील्ड के नीचे लाल लिखावट पढ़ें और उसे ठीक करें।
- **Submit बटन ग्रे है।** पहले सब डीलर चुनें और उसके लोड होने तक रुकें। जब GST PDF, लोकेशन या पिनकोड पढ़ा जा रहा हो, तब भी रुकें।
- **"Shortened URL detected."** (छोटा किया हुआ लिंक मिला) लिंक को ब्राउज़र में खोलें, एड्रेस बार से पूरा पता कॉपी करें और उसे पेस्ट करें।
- **"Choose a non-empty GST PDF of 5 MB or smaller."** (5 MB या उससे छोटी, खाली न हो ऐसी GST PDF चुनें) 5 MB या उससे छोटी PDF फ़ाइल अपलोड करें।
- **"GST could not be extracted."** (GST की जानकारी पढ़ी नहीं जा सकी) GST की जानकारी खुद टाइप करें और उसे जाँच लें।
- **"The selected Sub Dealer does not have a valid Sub Dealer Code. Contact Admin."** (चुने गए सब डीलर का कोड सही नहीं है। Admin से संपर्क करें) मास्टर रिकॉर्ड गलत है। Admin से उसे ठीक करने को कहें।
:::

::: section import-master
title: सब डीलर मास्टर इम्पोर्ट करें
routes: /SubDealerList
for: 
roles: Admin, SuperAdmin, CorporateAdmin
designations: 
minutes: 3
source-hash: c6fda3f4afafbb6e
confidence: ok

**यह स्क्रीन किस काम की है**
Admin सब डीलर की मंज़ूर Excel लिस्ट अपलोड करता है। इम्पोर्ट पहले हर लाइन की जाँच दिखाता है, और जब तक आप **Proceed Import** नहीं दबाते, कुछ भी सेव नहीं करता।

1. **Sub Dealer Master** पर **Import Sub Dealers** दबाएँ।
2. **Download Sample Format** दबाएँ। **Sub Dealer Code**, **Sub Dealer Name**, **HQ**, **Region** और **State** कॉलम भरें। State, Region और HQ के नाम वैसे ही लिखें जैसे ऐप में हैं।
3. **Choose Excel file** में .xlsx फ़ाइल चुनें।
4. **Check Import** दबाएँ। **Import summary** में **New**, **Already same** और **Rejected** लाइनें दिखती हैं। अभी कुछ भी सेव नहीं हुआ है।
5. हर रिजेक्ट हुई लाइन का **Reason** (कारण) पढ़ें। ज़रूरत हो तो Excel ठीक करें और फिर से जाँचें।
6. नई लाइनें सेव करने के लिए **Proceed Import** दबाएँ।
7. अब नए सब डीलर फ़ॉर्म में चुने जा सकते हैं। उनकी जानकारी सेव होने के बाद ही वे लिस्ट में दिखते हैं।

> Careful: पहले से मौजूद सब डीलर की फ़र्म, State, Region या HQ इम्पोर्ट से नहीं बदले जा सकते।
> Tip: SuperAdmin को गलत या खाली कोड ठीक करने के लिए **Correct Sub Dealer Codes** और **Repair Missing Codes** भी दिखते हैं। दोनों कुछ भी सेव करने से पहले प्रीव्यू दिखाते हैं।

## आम दिक्कतें

- **"Warning: Existing Sub Dealer master conflict detected!"** (पहले से मौजूद सब डीलर मास्टर से टकराव मिला) फ़ाइल में किसी पहले से मौजूद कोड के लिए अलग फ़र्म, State, Region या HQ दिया गया है। इसे बदलने के लिए Admin से संपर्क करें।
- **कुछ लाइनों में Sub Dealer Code खाली हैं या दिखते नहीं।** वे लाइनें रिजेक्ट हो गईं और सेव नहीं हुईं। दिखाए गए Excel लाइन नंबर जाँचें।
- **Check Import ग्रे है।** पहले .xlsx फ़ाइल चुनें। फ़ाइल 10 MB तक हो सकती है।
:::

::: section beneficiary-list
title: सब डीलर और कर्मचारियों के लाभार्थी देखें
routes: /SubDealerEmployeeMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: dfcd6ed3ad5ab51a
confidence: ok

**यह स्क्रीन किस काम की है**
**SDWA** मेन्यू की **Sub Dealer & Employee** स्क्रीन पर सब डीलर और डीलर के कर्मचारियों के कल्याण योजना वाले लाभार्थी रखे जाते हैं। हर रिकॉर्ड को पहले SM की और फिर AVP की मंज़ूरी चाहिए।

1. एक टैब चुनें: **Sub Dealer** या **Employee**।
2. कार्ड देखें: कुल, **Active** और **Inactive**।
3. सर्च बॉक्स में टाइप करें। **All Status**, **Active** या **Inactive** चुनें, और **All Approval**, **Pending SM**, **Pending AVP**, **Approved** या **Rejected** चुनें।
4. टेबल में हर रिकॉर्ड का **SM Status**, **AVP Status** और **Overall** पढ़ें।
5. अगर आपका रोल रिकॉर्ड बदल सकता है, तो **Status** कॉलम में **Active** या **Inactive** पर टैप करके उसे बदलें।

## आम दिक्कतें

- **"You do not have permission to view this data."** (आपको यह डेटा देखने की अनुमति नहीं है) अपने admin से Sub Dealer & Employee देखने की अनुमति माँगें।
- **ऊपर कोई फ़ॉर्म नहीं है।** आपका पद सिर्फ़ रिकॉर्ड देख सकता है।
- **किसी रिकॉर्ड पर बहुत देर से Pending दिख रहा है।** वह पहले SM का, फिर AVP का इंतज़ार करता है। वह कहाँ है यह देखने के लिए **Pending SM** या **Pending AVP** फ़िल्टर लगाएँ।
:::

::: section beneficiary-add
title: लाभार्थी जोड़ें या बदलें
routes: /SubDealerEmployeeMaster
for: SubDealerEmployeeMaster.Entry
roles: 
designations: 
minutes: 3
source-hash: c972236359c62bca
confidence: ok

**यह स्क्रीन किस काम की है**
सब डीलर या डीलर के कर्मचारी का कल्याण योजना वाला लाभार्थी जोड़ें, एक-एक करके या Excel फ़ाइल से। हर नया रिकॉर्ड SM की मंज़ूरी के लिए **Pending** के रूप में शुरू होता है।

1. **Sub Dealer** या **Employee** टैब चुनें। ऊपर के फ़ॉर्म पर **Add New Sub Dealer** या **Add New Employee** लिखा होता है।
2. सब डीलर के लिए **Main Dealer Code**, **Main Dealer / Firm Name**, **Sub Dealer Code**, **Sub Dealer Name** और **Beneficiary Name** भरें। कर्मचारी के लिए **Dealer Code**, **Employee Name** और **Beneficiary Name** भरें।
3. आपके पास जो बाकी जानकारी है वह भी भरें, जैसे **Beneficiary ID**, **Nominee Name**, **Date of Birth** और **Relationship**।
4. स्विच को **Active** पर ही रहने दें और **Save** दबाएँ।
5. एक साथ कई रिकॉर्ड जोड़ने के लिए **Download Template** दबाएँ, उसे भरें, फिर **Upload File** दबाएँ और Excel फ़ाइल चुनें।
6. रिकॉर्ड बदलने के लिए **Action** कॉलम में पेंसिल पर टैप करें, ठीक करें और **Save** दबाएँ। डस्टबिन रिकॉर्ड मिटा देता है।

> Careful: मिटाया गया रिकॉर्ड वापस नहीं लाया जा सकता। रिकॉर्ड बदलने से उसकी SM और AVP मंज़ूरी फिर से शुरू नहीं होती।

## आम दिक्कतें

- **"Main Dealer Code is required."** (Main Dealer Code भरना ज़रूरी है) * वाला हर फ़ील्ड भरें। मैसेज में छूटे हुए फ़ील्ड का नाम होता है।
- **सेव करते समय "already exists" आता है।** उसी Dealer Code, Sub Dealer Code, Beneficiary Name और DOB वाला लाभार्थी लिस्ट में पहले से है। उसे खोजें।
- **"Upload completed with ... skipped row(s)".** (अपलोड हो गया, पर कुछ लाइनें छूट गईं) मैसेज के नीचे दिखाई गई छूटी हुई लाइनें पढ़ें। उन्हें कॉपी करने के लिए **Copy** दबाएँ, फ़ाइल ठीक करें और वे लाइनें फिर से अपलोड करें।
:::

::: section beneficiary-approve
title: लाभार्थी को मंज़ूर या रिजेक्ट करें
routes: /SubDealerEmployeeMaster
for: 
roles: SMD, SMM, AVP
designations: 
minutes: 2
source-hash: c429d512dd70afcb
confidence: ok

**यह स्क्रीन किस काम की है**
SMD या SMM हर लाभार्थी को पहले मंज़ूरी देता है। AVP आख़िरी मंज़ूरी देता है। कल्याण योजना के आवेदन में डीलर सिर्फ़ मंज़ूर और Active लाभार्थी ही चुन सकता है।

1. **Sub Dealer** या **Employee** टैब चुनें।
2. मंज़ूरी वाला फ़िल्टर **Pending SM** (SMD, SMM) या **Pending AVP** (AVP) पर लगाएँ।
3. रिकॉर्ड की जानकारी जाँचें: डीलर कोड, नाम, जन्म की तारीख और रिश्ता।
4. **Action** कॉलम में मंज़ूर करने के लिए हरा टिक, या रिजेक्ट करने के लिए लाल क्रॉस दबाएँ। फ़ोन पर **Approve** या **Reject** दबाएँ।
5. **Remarks (optional)** टाइप करें और **Approve** या **Reject** दबाएँ।
6. **SM Status** या **AVP Status** कॉलम बदल जाता है। AVP को रिकॉर्ड तभी दिखता है जब SM उसे मंज़ूर कर दे।

> Careful: रिजेक्ट हुआ रिकॉर्ड वहीं रुक जाता है। कल्याण योजनाओं के लिए वह डीलर को नहीं दिखाया जाता।

## आम दिक्कतें

- **लाइन पर टिक या क्रॉस नहीं है।** SM को ये तभी दिखते हैं जब **SM Status** **Pending** हो। AVP को ये तभी दिखते हैं जब SM मंज़ूर कर चुका हो और **AVP Status** **Pending** हो।
- **Overall कॉलम में SM Rejected लिखा है।** SM ने रिकॉर्ड रिजेक्ट कर दिया, इसलिए वह AVP तक कभी नहीं पहुँचता।
:::
