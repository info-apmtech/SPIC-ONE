---
module: logistics
title: लॉजिस्टिक्स
icon: bi-truck
order: 113
language: hi
category: सुविधाएँ
note: नई गाइड
meta-source-hash: a51949592e8c1854
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 616b9e8fbedcbace
confidence: ok

SPIC और GREEN STAR जिन गोदामों (warehouse), रेक पॉइंट और बंदरगाहों (port) का इस्तेमाल करते हैं, उनकी एक ही लिस्ट रखें। फ़ील्ड स्टाफ़ हर जगह को उसकी लोकेशन और कागज़ात के साथ जोड़ता है, और RM, SMM और AVP बारी-बारी से उसे मंज़ूर करते हैं।

- add | icon: bi-building-add | color: #475569
  title: फ़ील्ड स्टाफ़ इसे जोड़ता है
  text: MO, MDO या JMDO गोदाम या रेक पॉइंट को उसके SAP Code, पते, नक्शे पर जगह और कागज़ात के साथ जोड़ते हैं।
- approve | icon: bi-shield-check | color: #d97706
  title: तीन मंज़ूरियाँ
  text: पहले RM, फिर SMM, फिर AVP रिकॉर्ड जाँचते हैं। हर कोई इसे मंज़ूर कर सकता है या टिप्पणी लिखकर लौटा सकता है।
- report | icon: bi-bar-chart-line | color: #2563eb
  title: कुल गिनती देखें
  text: रिपोर्ट में राज्य के हिसाब से दिखता है कि कितने रिकॉर्ड बाकी हैं और कितने पूरे हो गए।
:::

::: section flow
title: रिकॉर्ड कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: caf925a727761bcf
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- admin | type: lane | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  sub: SAP Code की लिस्ट
- maker | type: lane | color: #475569 | roles: MO, MDO, JMDO
  title: MO / MDO / JMDO
  sub: फ़ील्ड स्टाफ़
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: आपका रीजन
- smm | type: lane | color: #c026d3 | roles: SMM, SMD
  title: SMM
  sub: आपका राज्य
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin
  title: AVP
  sub: आख़िरी मंज़ूरी
- sap | lane: admin | col: 1 | icon: bi-upc-scan | color: #64748b | next: add | screen: sap-master
  title: SAP Code लोड करें
  text: Admin PVT Master और Rake Point Master की Excel फ़ाइलें अपलोड करता है और हर SAP Code को एक राज्य से जोड़ता है।
- add | lane: maker | col: 2 | icon: bi-building-add | color: #475569 | next: rm-check | screen: add-record
  title: रिकॉर्ड जोड़ें
  text: SAP Code चुनें, जानकारी, लोकेशन और कागज़ात भरें, और **Save** दबाएँ। रिकॉर्ड RM के पास जाता है।
- rm-check | lane: rm | col: 3 | icon: bi-hourglass-split | color: #d97706 | next: smm-check | return: add | screen: approve-records
  title: RM इसे जाँचता है
  status: Pending RM
  text: RM **Approve to SMM** दबाता है, या MO, MDO या JMDO को लौटाने के लिए **Send Back** दबाता है।
- smm-check | lane: smm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: final | return: rm-check | screen: approve-records
  title: SMM इसे जाँचता है
  status: Pending SMM
  text: SMM **Approve to AVP** दबाता है, या RM को लौटाने के लिए **Send Back** दबाता है।
- final | lane: avp | col: 5 | icon: bi-patch-check | color: #16a34a | return: smm-check | screen: approve-records
  title: आख़िरी मंज़ूरी
  status: Approved
  text: AVP **Final Approve** दबाता है, या SMM को लौटाने के लिए **Send Back** दबाता है। मंज़ूरी मिलते ही रिकॉर्ड पूरा हो जाता है।
- sentback | lane: maker | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: add | screen: manage-records
  title: ठीक करें और फिर से सेव करें
  status: Sent Back to MO
  text: **Remarks** पढ़ें, पेंसिल दबाएँ, रिकॉर्ड ठीक करें और **Update** दबाएँ। यह फिर से RM के पास जाता है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c35bc2efe348d9c7
confidence: ok

**Approval Status** कॉलम में दिखता है कि गोदाम या रेक पॉइंट अभी कहाँ है। **Status** कॉलम में **Active** या **Inactive** दिखता है।

- draft | color: #64748b
  label: Draft
  text: किसी ऐसे व्यक्ति ने सेव किया है जो MO, MDO या JMDO नहीं है। यह मंज़ूरी की कड़ी में नहीं है।
- pending-rm | color: #d97706
  label: Pending RM
  text: रीजन के RM का इंतज़ार है।
- pending-smm | color: #d97706
  label: Pending SMM
  text: RM ने मंज़ूर कर दिया। राज्य के SMM का इंतज़ार है।
- pending-avp | color: #d97706
  label: Pending AVP
  text: SMM ने मंज़ूर कर दिया। आख़िरी मंज़ूरी का इंतज़ार है।
- approved | color: #16a34a
  label: Approved
  text: तीनों मंज़ूरियाँ हो गई हैं।
- back-mo | color: #dc2626
  label: Sent Back to MO
  text: RM ने इसे लौटा दिया। जिस MO, MDO या JMDO ने इसे बनाया था, उसे इसे ठीक करना है।
- back-rm | color: #dc2626
  label: Sent Back to RM
  text: SMM ने इसे RM को लौटा दिया।
- back-smm | color: #dc2626
  label: Sent Back to SMM
  text: AVP ने इसे SMM को लौटा दिया।
- active | color: #0d9488
  label: Active
  text: जगह इस्तेमाल में है। लोकेशन की जानकारी और बताए गए कागज़ात ज़रूरी हैं।
- inactive | color: #475569
  label: Inactive
  text: जगह अभी इस्तेमाल में नहीं है। कम जानकारी चाहिए।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 8acdb7d7714a0307
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- maker | color: #475569 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - गोदाम और रेक पॉइंट जोड़ना
  - GST, बीमा और लाइसेंस के कागज़ात अपलोड करना
  - लौटाए गए रिकॉर्ड ठीक करना
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - अपने रीजन के रिकॉर्ड जाँचना
  - SMM को मंज़ूर करना या बनाने वाले को लौटाना
- smm | color: #c026d3 | roles: SMM, SMD
  title: SMM
  - अपने राज्य के रिकॉर्ड जाँचना
  - AVP को मंज़ूर करना या RM को लौटाना
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP, Director
  - आख़िरी मंज़ूरी देना या SMM को लौटाना
  - बंदरगाह (port) जोड़ना और बदलना
- admin | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  - SAP Code की लिस्ट अपलोड करना और उन्हें राज्यों से जोड़ना
  - आख़िरी मंज़ूरी देना
  - लॉजिस्टिक्स रिपोर्ट देखना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7662caacafa8b468
confidence: ok

- no-sap-code | popular: true
  q: मेरा SAP Code लिस्ट में नहीं है
  a: लिस्ट में सिर्फ़ वही कोड दिखते हैं जो आपके चुने हुए **State** से जुड़े हैं। Admin से कहें कि वह कोड अपलोड करे या **PVT Master Management** स्क्रीन पर उसे आपके राज्य से जोड़े।
- sent-back | popular: true
  q: मेरे गोदाम पर "Sent Back to MO" दिख रहा है। मैं क्या करूँ?
  a: **Remarks** कॉलम पढ़ें। उस लाइन पर पेंसिल दबाएँ, रिकॉर्ड ठीक करें और **Update** दबाएँ। यह **Pending RM** बनकर फिर से RM के पास जाता है।
- no-approve
  q: मैं RM हूँ पर मुझे Approve बटन नहीं दिख रहा
  a: बटन सिर्फ़ उन रिकॉर्ड पर दिखता है जो आपकी लेवल पर रुके हैं। मंज़ूरी वाले फ़िल्टर में अपनी लेवल चुनें, जैसे **Pending RM**। RM को सिर्फ़ अपने रीजन के रिकॉर्ड दिखते हैं।
- save-error
  q: Save करने पर फ़ील्ड के नीचे लाल मैसेज दिखते हैं
  a: **Active** रिकॉर्ड के लिए पूरी लोकेशन, संपर्क नंबर और ज़रूरी कागज़ात चाहिए। लाल निशान वाला हर फ़ील्ड भरें, या अगर जगह इस्तेमाल में नहीं है तो **Status** को **Inactive** करें।
- duplicate
  q: लिखा आता है कि SAP Code पहले से है
  a: कोई दूसरा गोदाम या रेक पॉइंट वह कोड पहले से इस्तेमाल कर रहा है। लिस्ट में उसे ढूँढें। एक ही जगह को दो बार न जोड़ें।
:::

::: section add-record
title: गोदाम या रेक पॉइंट जोड़ें
routes: /Logistics
for: 
roles: 
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-basic] | 3 > [data-help=lg-location] | 4 > [data-help=lg-docs] | 5 > [data-help=lg-save]
minutes: 5
source-hash: c9ec25daaeef4964
confidence: ok

**यह स्क्रीन किस काम की है**
**Logistics Master** स्क्रीन पर ऊपर एक फ़ॉर्म है और नीचे सेव किए गए रिकॉर्ड की लिस्ट है। गोदाम या रेक पॉइंट जोड़ने के लिए फ़ॉर्म का इस्तेमाल करें।

1. टैब चुनें: **Warehouse** या **Rake Point**।
2. **Basic Information** में **SPIC**, **GREEN STAR** या दोनों पर टैप करें। **State** चुनें, फिर **SAP Code** चुनें; **Name** अपने आप भर जाता है। **Operated By** चुनें। गोदाम के लिए **Warehouse Category** (**PVT WH**, **C&F WH** या **Both**) और **WH Type** चुनें, और **Reservation Quantity** बॉक्स भरें।
3. **Primary Location** में **Use Current Location** दबाएँ, नक्शे पर टैप करें, या **Google Map URL** पेस्ट करें। फिर पता, **Pincode** और संपर्क नंबर भरें।
4. **Documents** में हर **Choose ... File** बटन दबाएँ और फ़ाइल चुनें। जाँचने के लिए आँख वाला आइकन इस्तेमाल करें। **Active** रिकॉर्ड के लिए * वाले फ़ील्ड ज़रूरी हैं।
5. **Save** दबाएँ। जब MO, MDO या JMDO सेव करता है, तो रिकॉर्ड **Pending RM** बनकर RM के पास जाता है।

> Tip: कागज़ स्कैन करके अपलोड करने से पहले उस पर गोदाम का पता हाइलाइटर से रंग दें।
> Careful: **Cancel** तभी दबाएँ जब फ़ॉर्म खाली करना हो। कुछ भी सेव नहीं होता।

| फ़ील्ड | क्या भरें |
|---|---|
| **Status** | जगह अभी इस्तेमाल में है तो **Active**, नहीं तो **Inactive**। |
| **Operated By** | **Dealer** या **Others**। |
| **WH Type** | **Field WH** या **Rake Point WH** (सिर्फ़ PVT WH और Both के लिए)। |
| **Approved Reservation Quantity (MT)** | कंपनी के लिए रखी गई जगह, टन में। |
| **Latitude & Longitude** | कॉमा से अलग दो नंबर, जैसे 10.7905, 78.7047। |
| **Pincode** | 6 अंक। **State** और **District** अपने आप भर सकते हैं। |

## आम दिक्कतें

- **SAP Code की लिस्ट खाली है।** चुने हुए **State** से अभी कोई कोड नहीं जुड़ा है। Admin से पूछें।
- **"Primary Location is required".** (Primary Location ज़रूरी है) **Active** रिकॉर्ड के लिए नक्शे पर जगह या Google Map URL और पूरा पता चाहिए।
- **"... saved, but one or more documents failed to upload".** (रिकॉर्ड सेव हो गया, पर एक या ज़्यादा कागज़ात अपलोड नहीं हुए) रिकॉर्ड सेव हो गया है। उस पर पेंसिल दबाएँ और कागज़ात फिर से अपलोड करें।
- **State, Region या HQ ग्रे है।** ये आपके लॉगिन से भरे जाते हैं और बदले नहीं जा सकते।
:::

::: section approve-records
title: रिकॉर्ड मंज़ूर करें या लौटाएँ
routes: /Logistics
for: 
roles: RM, RMD, SMM, SMD, AVP, Director, Admin, CorporateAdmin
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-approval-filter] | 3 > [data-help=lg-search] | 4 > [data-help=lg-list]
minutes: 2
source-hash: 5b300e3e278e2af7
confidence: ok

**यह स्क्रीन किस काम की है**
फ़ॉर्म के नीचे की लिस्ट में आपके इलाके के गोदाम और रेक पॉइंट दिखते हैं। जो रिकॉर्ड आपके इंतज़ार में हैं, उनके **Action** कॉलम में मंज़ूरी और **Send Back** के बटन होते हैं।

1. **Warehouse** या **Rake Point** टैब चुनें।
2. मंज़ूरी वाले फ़िल्टर में अपनी लेवल चुनें: **Pending RM**, **Pending SMM** या **Pending AVP**।
3. रिकॉर्ड ढूँढने के लिए सर्च बॉक्स में नाम या SAP Code टाइप करें। उसे जाँचने के लिए **Docs** कॉलम का **View** और PDF बटन इस्तेमाल करें।
4. **Approve to SMM**, **Approve to AVP** या **Final Approve** दबाएँ (आपकी लेवल का बटन दिखता है)। **Approval Remarks** लिखें और **Confirm & Proceed** दबाएँ। लौटाने के लिए **Send Back** दबाएँ, **Send Back Remarks** लिखें और **Confirm Send Back** दबाएँ।

> Tip: मंज़ूर करने और लौटाने, दोनों के लिए टिप्पणी ज़रूरी है। यह **Remarks** कॉलम में दिखती है।
> Careful: **Send Back** से रिकॉर्ड मिटता नहीं है। यह सिर्फ़ एक लेवल पीछे जाता है।

## आम दिक्कतें

- **मंज़ूरी का बटन नहीं है।** रिकॉर्ड आपकी लेवल पर नहीं है। उसका **Approval Status** देखें।
- **"Approval remarks are required."** (मंज़ूरी की टिप्पणी ज़रूरी है) पक्का करने से पहले टिप्पणी टाइप करें।
- **मुझे कोई रिकॉर्ड नहीं दिख रहा।** RM को सिर्फ़ अपना रीजन, SMM को अपना राज्य और AVP को अपना ज़ोन दिखता है।
:::

::: section manage-records
title: रिकॉर्ड ढूँढें, ठीक करें और डाउनलोड करें
routes: /Logistics
for: 
roles: 
designations: 
minutes: 3
source-hash: 13e0dd985a45cb04
confidence: ok

**यह स्क्रीन किस काम की है**
फ़ॉर्म के नीचे की लिस्ट में सेव किए गए रिकॉर्ड दिखते हैं, और ऊपर गिनती होती है: **Total**, **Active** और **Inactive**।

1. टैब चुनें और लिस्ट के ऊपर की गिनती देखें।
2. नाम से खोजें, या लिस्ट छोटी करने के लिए **All Status** और मंज़ूरी वाले फ़िल्टर इस्तेमाल करें।
3. सेव किए गए कागज़ात देखने, डाउनलोड करने या खोलने के लिए **Docs** कॉलम में **View** दबाएँ। काम हो जाए तो **Close** दबाएँ।
4. रिकॉर्ड को PDF में खोलने के लिए **PDF** कॉलम में SPIC या GREEN STAR का लोगो दबाएँ।
5. रिकॉर्ड को फ़ॉर्म में लाने के लिए पेंसिल दबाएँ। बदलाव करें और **Update** दबाएँ।
6. **Status** कॉलम में **Active** या **Inactive** पर टैप करके उसे बदलें।
7. रिकॉर्ड मिटाने के लिए डस्टबिन दबाएँ, फिर पक्का करने के लिए **Delete** दबाएँ।
8. लिस्ट डाउनलोड करने के लिए लिस्ट के ऊपर **Excel** दबाएँ (फ़ोन पर **Export** और फिर **Excel**)।

> Careful: मिटाया गया रिकॉर्ड वापस नहीं लाया जा सकता।

## आम दिक्कतें

- **"Sent Back to MO".** (MO को लौटाया गया) **Remarks** पढ़ें, पेंसिल से रिकॉर्ड ठीक करें और **Update** दबाएँ। यह फिर से RM के पास जाता है।
- **Docs का View बटन ग्रे है।** उस रिकॉर्ड के लिए कोई कागज़ सेव नहीं है।
- **लिस्ट खाली है।** हो सकता है आपके फ़िल्टर बहुत कड़े हों। उन्हें फिर से **All Status** और **All Approval Status** पर रखें।
:::

::: section ports
title: बंदरगाह (port) जोड़ें
routes: /Logistics
for: 
roles: Admin, CorporateAdmin, AVP, Director
designations: 
minutes: 1
source-hash: 264444955d15cae7
confidence: ok

**यह स्क्रीन किस काम की है**
**Port** टैब में बंदरगाहों की लिस्ट रहती है। यह टैब सिर्फ़ Admin, AVP और Director को दिखता है। बंदरगाहों के लिए मंज़ूरी के स्टेप नहीं होते।

1. **Port** टैब खोलें।
2. **Port Name** भरें, और अगर आपके पास है तो **Google Map or Lat/Long URL** भी भरें।
3. **State** और **District** चुनें।
4. **Active** या **Inactive** चुनें और **Save** दबाएँ।

## आम दिक्कतें

- **"Port name is required".** (बंदरगाह का नाम ज़रूरी है) सेव करने से पहले नाम, **State** और **District** भरें।
:::

::: section reports
title: लॉजिस्टिक्स रिपोर्ट देखें
routes: /LogisticsReport
for: 
roles: 
designations: 
minutes: 1
source-hash: 206298ea363d8aeb
confidence: ok

**यह स्क्रीन किस काम की है**
**Logistics Reports** स्क्रीन पर राज्य के हिसाब से गोदामों और रेक पॉइंट की गिनती दिखती है, और सबसे नीचे **TOTAL** लाइन होती है।

1. **Logistics Master** स्क्रीन पर **Reports** दबाएँ।
2. **Warehouse Report** या **Rakepoint Report** चुनें।
3. सिर्फ़ एक राज्य देखने के लिए **Search by state...** में राज्य का नाम टाइप करें।
4. कॉलम पढ़ें: **Active**, **Inactive**, **Total**, **As per SAP**, **Pending with MO**, **Pending RM**, **Pending SMM**, **Pending with AVP** और **Completed**।
5. नई गिनती लाने के लिए **Refresh** दबाएँ। वापस जाने के लिए **Back to Logistics** दबाएँ।

> Tip: **As per SAP** यानी SAP Code लिस्ट में कितने कोड हैं। **Pending with MO** यानी वे कोड जो अभी जोड़े नहीं गए या अभी मंज़ूरी में नहीं गए।

## आम दिक्कतें

- **"No data available".** (कोई जानकारी नहीं है) आपकी खोज से कुछ नहीं मिलता। सर्च बॉक्स खाली करें या **Refresh** दबाएँ।
:::

::: section sap-master
title: SAP Code अपलोड करें और राज्य जोड़ें
routes: /PVTMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 2bc3660a46728597
confidence: ok

**यह स्क्रीन किस काम की है**
**PVT Master Management** स्क्रीन पर SAP Code की लिस्ट रहती हैं। इन्हीं लिस्ट से Logistics फ़ॉर्म के **SAP Code** बॉक्स में कोड आते हैं। Admin इसे **Logistics Master** स्क्रीन पर **SAP Code** बटन से खोलते हैं।

1. **PVT Master** (गोदाम) या **Rake Point** चुनें।
2. Excel का नमूना डाउनलोड करने के लिए **Sample Format** दबाएँ और उसे भरें।
3. **Click to upload or drag and drop** वाली जगह दबाएँ और अपनी .xlsx फ़ाइल चुनें।
4. **Upload Result** पढ़ें: **Total Records**, **Successfully Inserted** और **Duplicate/Skipped**, और नीचे छोड़ी गई लाइनें।
5. लिस्ट में किसी लाइन पर **State** दबाएँ, **State** चुनें और **Save State Mapping** दबाएँ।
6. Logistics पर लौटने के लिए **Back** दबाएँ।

> Careful: 10 MB या उससे छोटी .xlsx फ़ाइल इस्तेमाल करें। जो कोड पहले से हैं, वे छोड़ दिए जाते हैं।

## आम दिक्कतें

- **"Please select a valid .xlsx Excel file."** (सही .xlsx Excel फ़ाइल चुनें) फ़ाइल को .xlsx में सेव करें और फिर से कोशिश करें।
- **किसी कोड पर "Not Mapped" दिख रहा है।** उस लाइन पर **State** दबाएँ और उसका राज्य चुनें। तब तक फ़ील्ड स्टाफ़ राज्य चुनने के बाद उस कोड को नहीं चुन पाएगा।
- **State बटन नहीं है।** सिर्फ़ Admin और CorporateAdmin ही यह जोड़ बदल सकते हैं।
:::

::: section old-master
title: पुरानी Logistics Master स्क्रीन
routes: /LogisticsMaster
for: 
roles: 
designations: 
minutes: 1
source-hash: 2bc6b07f78791b25
confidence: ok

**यह स्क्रीन किस काम की है**
यह Logistics फ़ॉर्म का पुराना रूप है, जिसमें **PVT WH**, **C&F WH** और **Rake Point** टैब हैं। नए काम के लिए साइड मेन्यू में मुख्य **Logistics Master** स्क्रीन इस्तेमाल करें।

1. टैब चुनें: **PVT WH**, **C&F WH** या **Rake Point**।
2. मुख्य स्क्रीन की तरह **Basic Information**, **Reservation Quantity**, **Primary Location** और **Documents** भरें।
3. **Save** दबाएँ, या रिकॉर्ड बदलने या मिटाने के लिए लिस्ट में पेंसिल और डस्टबिन इस्तेमाल करें।

## आम दिक्कतें

- **"Failed to load PVT WH" या "Failed to load C&F WH".** (PVT WH / C&F WH लोड नहीं हुआ) ये टैब इस स्क्रीन पर काम नहीं करते। मुख्य **Logistics Master** स्क्रीन इस्तेमाल करें।
- **मंज़ूरी के बटन नहीं हैं।** मंज़ूरी सिर्फ़ मुख्य **Logistics Master** स्क्रीन पर होती है।
:::
