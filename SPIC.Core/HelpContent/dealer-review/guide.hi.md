---
module: dealer-review
title: डीलर जाँच और मंज़ूरी
icon: bi-person-check
order: 102
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
source-hash: c1f3fc37e023de12
confidence: ok

MO के डीलर एप्लिकेशन सबमिट करने के बाद पहले RM, फिर SM और फिर AVP उसे जाँचते हैं। हर कोई उसे मंज़ूर करके क्रेडिट लिमिट सुझा सकता है, या सुधार के लिए लौटा सकता है। इस मॉड्यूल में सब डीलर और कर्मचारियों की मंज़ूरी वाली स्क्रीन और डीलर की कुछ रिपोर्ट भी हैं।

- check | icon: bi-search | color: #6d28d9
  title: तीन लेवल जाँचते हैं
  text: पहले RM, फिर SM, फिर AVP एप्लिकेशन खोलकर उसका हर हिस्सा जाँचते हैं।
- limit | icon: bi-cash-coin | color: #d97706
  title: क्रेडिट लिमिट का फ़ैसला
  text: RM और SM, SPIC और GFL के लिए क्रेडिट लिमिट सुझाते हैं। AVP आख़िरी लिमिट देता है।
- back | icon: bi-arrow-counterclockwise | color: #dc2626
  title: सुधार के लिए लौटाएँ
  text: कोई भी लेवल कारण और ठीक करने वाले हिस्से लिखकर एप्लिकेशन को एक स्टेप पीछे भेज सकता है।
:::

::: section flow
title: एप्लिकेशन कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 1f194f956d1d33e7
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- mo | type: lane | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  sub: एप्लिकेशन भरता है
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: रीजनल मैनेजर
- sm | type: lane | color: #c026d3 | roles: SMD, SMM
  title: SM
  sub: स्टेट मार्केटिंग मैनेजर
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin, SuperAdmin
  title: AVP
  sub: आख़िरी मंज़ूरी
- office | type: lane | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: सिर्फ़ नए डीलर के लिए
- submit | lane: mo | col: 1 | icon: bi-send | color: #64748b | next: rm-check
  title: एप्लिकेशन सबमिट करें
  status: Submitted
  text: MO डीलर एप्लिकेशन का हर हिस्सा भरकर उसे सबमिट करता है। सिर्फ़ पूरे भरे हुए एप्लिकेशन ही जाँच वाली लिस्ट में आते हैं।
- rm-check | lane: rm | col: 2 | icon: bi-person-check | color: #0891b2 | next: sm-check | return: submit | screen: review-approve
  title: RM जाँचता है
  status: In RM
  text: RM एप्लिकेशन जाँचता है, RM क्रेडिट लिमिट भरता है और **Proceed to SM** दबाता है, या उसे MO को लौटा देता है।
- sent-back | lane: mo | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: submit | screen: send-back
  title: ठीक करके फिर से सबमिट करें
  status: Sent Back
  text: जाँचने वाले ने कारण और ठीक करने वाले हिस्से लिखकर एप्लिकेशन लौटा दिया है। पिछला लेवल उन्हें ठीक करके एप्लिकेशन फिर से आगे भेजता है।
- sm-check | lane: sm | col: 3 | icon: bi-clipboard-check | color: #c026d3 | next: avp-check | return: rm-check | screen: review-approve
  title: SM जाँचता है
  status: In SMM
  text: SM को RM क्रेडिट लिमिट दिखती है। वह SM क्रेडिट लिमिट भरता है और **Proceed to AVP** दबाता है, या एप्लिकेशन RM को लौटा देता है।
- avp-check | lane: avp | col: 4 | icon: bi-patch-check | color: #7c3aed | next: office-ids | return: sm-check | screen: review-approve
  title: AVP मंज़ूर करता है
  status: In AVP
  text: AVP को पहले की दोनों लिमिट दिखती हैं। वह आख़िरी क्रेडिट लिमिट भरता है और **Approve** दबाता है। डीलर एक्टिव हो जाता है।
- office-ids | lane: office | col: 5 | icon: bi-upc-scan | color: #16a34a | screen: avp-office-ids
  title: डीलर ID भरें
  status: Verified
  text: नए डीलर के लिए AVP Office, SPIC और GFL के डीलर ID भरता है। पुराने डीलर का एप्लिकेशन AVP की मंज़ूरी के बाद ही पूरा हो जाता है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 3678b5e01ec8dd65
confidence: ok

- submitted | color: #64748b
  label: Submitted
  text: MO ने एप्लिकेशन सबमिट कर दिया है।
- in-rm | color: #0891b2
  label: In RM
  text: रीजनल मैनेजर का इंतज़ार है।
- in-smm | color: #c026d3
  label: In SMM
  text: स्टेट मार्केटिंग मैनेजर का इंतज़ार है।
- in-avp | color: #7c3aed
  label: In AVP
  text: AVP की आख़िरी मंज़ूरी का इंतज़ार है।
- pending | color: #d97706
  label: Pending
  text: जाँच वाली लिस्ट पर: अभी RM, SMM, AVP या AVP Office के पास है। **Current Stage** कॉलम में दिखता है कि किसके पास है।
- verified | color: #16a34a
  label: Verified
  text: जाँच वाली लिस्ट पर: पूरी तरह मंज़ूर हो गया है।
- approved | color: #16a34a
  label: Approved
  text: एप्लिकेशन पर: AVP ने इसे मंज़ूर कर दिया है।
- sent-back | color: #dc2626
  label: Sent Back
  text: जाँच वाली लिस्ट पर: जाँचने वाले ने इसे सुधार के लिए लौटा दिया है।
- rejected | color: #dc2626
  label: Rejected
  text: एप्लिकेशन पर: जाँचने वाले ने इसे लौटा दिया है। यह बंद नहीं हुआ है; सुधार के बाद यह फिर से आता है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 06465e79ce0431ec
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- mo | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  - डीलर एप्लिकेशन सबमिट करना
  - लौटाए जाने पर उसे ठीक करना
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - अपने रीजन के एप्लिकेशन जाँचना
  - RM क्रेडिट लिमिट सुझाना
  - MO को लौटाना
- sm | color: #c026d3 | roles: SMD, SMM
  title: SM
  - अपने राज्य के एप्लिकेशन जाँचना
  - SM क्रेडिट लिमिट सुझाना
  - RM को लौटाना
  - सब डीलर और कर्मचारियों को पहली मंज़ूरी देना
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP
  - आख़िरी मंज़ूरी और क्रेडिट लिमिट देना
  - SM को लौटाना
  - सब डीलर और कर्मचारियों को आख़िरी मंज़ूरी देना
- admin | color: #374151 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - AVP की तरह आख़िरी मंज़ूरी दे सकता है
  - Credit Limit Sales के आँकड़े संभालना
  - राज्य के हिसाब से डीलर का सारांश देखना
- office | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - नए डीलर के लिए SPIC और GFL के डीलर ID भरना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 724bb434f0c07f99
confidence: ok

- no-button | popular: true
  q: मैंने एप्लिकेशन खोला पर Proceed या Send Back बटन नहीं दिख रहा
  a: एप्लिकेशन अभी आपके लेवल पर नहीं है। बटन तभी दिखते हैं जब वह आपका इंतज़ार कर रहा हो। ऊपर का **Status** कार्ड और प्रोग्रेस बार देखें।
- no-limit-boxes | popular: true
  q: Confirm Proceed विंडो में क्रेडिट लिमिट के बॉक्स क्यों नहीं हैं?
  a: नए डीलर की पहली मंज़ूरी में, बंद (inactive) डीलर के लिए, या अगर डीलर ने सहमति देकर बिना क्रेडिट लिमिट के आवेदन किया है (तब लिमिट 0 होती है), ये बॉक्स नहीं दिखते। SPIC के बॉक्स तभी दिखते हैं जब डीलर SPIC के साथ हो, और GFL के बॉक्स तभी जब डीलर GFL के साथ हो।
- missing
  q: मेरी जाँच वाली लिस्ट में एक एप्लिकेशन नहीं दिख रहा
  a: लिस्ट में सिर्फ़ आपके इलाके के पूरे भरे हुए एप्लिकेशन दिखते हैं। अगर एप्लिकेशन के कुछ हिस्से अभी खाली हैं, तो वह नहीं दिखता। MO से सारे हिस्से पूरे करने को कहें।
- remarks
  q: मुझे "Remarks are required for New Dealer approval." दिख रहा है
  a: नए डीलर के लिए हर लेवल को रिमार्क लिखना ज़रूरी है। **Remarks** बॉक्स में रिमार्क टाइप करें और फिर से **Confirm & Proceed** दबाएँ।
- after-send-back
  q: जब मैं एप्लिकेशन लौटाता हूँ तो वह कहाँ जाता है?
  a: एक लेवल पीछे। RM उसे MO को भेजता है, SM उसे RM को, और AVP उसे SM को। वे उसे ठीक करते हैं और एप्लिकेशन फिर से आपके पास आता है।
:::

::: section review-list
title: जाँच के लिए इंतज़ार कर रहे एप्लिकेशन देखें
routes: /DealerReviewList
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-list-cards] | 2 > [data-help=dr-list-search] | 3 > [data-help=dr-list-status] | 4 > [data-help=dr-list-table] | 5 > [data-help=dr-list-pages]
minutes: 2
source-hash: d6627d7c7d23093d
confidence: ok

**यह स्क्रीन किस काम की है**
**Dealer Applications Review** स्क्रीन पर आपके इलाके के सबमिट हुए डीलर एप्लिकेशन दिखते हैं। जो एप्लिकेशन आपका इंतज़ार कर रहे हैं, वे सबसे ऊपर होते हैं।

1. ऊपर के कार्ड देखें: **Total Applications**, **Pending Review**, **Verified** और **Sent Back**।
2. एप्लिकेशन जल्दी ढूँढने के लिए **Search applications...** में एप्लिकेशन ID या डीलर का नाम टाइप करें।
3. सिर्फ़ वही एप्लिकेशन देखने के लिए स्टेटस वाले बॉक्स में **Pending**, **Verified** या **Sent Back** चुनें। **All Status** से सब दिखते हैं।
4. टेबल में **Current Stage** बताता है कि एप्लिकेशन अभी किसके पास है और **Credit Limit** में मंज़ूर लिमिट दिखती है। एप्लिकेशन खोलने के लिए **Action** में आँख वाले आइकन पर टैप करें।
5. नीचे **Items Per Page** में चुनें कि कितनी लाइनें दिखानी हैं, और पेज बदलने के लिए तीर इस्तेमाल करें।

> Tip: RM को अपना रीजन दिखता है, SM को अपना राज्य, और AVP को सारे एप्लिकेशन दिखते हैं।

## आम दिक्कतें

- **"No applications found."** (कोई एप्लिकेशन नहीं मिला) **All Status** चुनें और सर्च बॉक्स खाली करें।
- **कोई एप्लिकेशन लिस्ट में नहीं है।** सिर्फ़ पूरे भरे हुए एप्लिकेशन ही लिस्ट में आते हैं। MO से हर हिस्सा भरने को कहें।
:::

::: section review-approve
title: एप्लिकेशन जाँचें और मंज़ूर करें
routes: /SavedDealerReview
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-rev-cards] | 2 > [data-help=dr-rev-progress] | 3 > [data-help=dr-rev-timeline] | 4 > [data-help=dr-rev-details] | 5 > [data-help=dr-rev-sendback] | 6 > [data-help=dr-rev-approve]
minutes: 4
source-hash: 337aec253f3ff75e
confidence: ok

**यह स्क्रीन किस काम की है**
**Review - Dealer Application** स्क्रीन पर एक पूरा एप्लिकेशन दिखता है। जब वह आपका इंतज़ार कर रहा हो, तो आप यहीं उसे मंज़ूर करके अपनी क्रेडिट लिमिट देते हैं, या उसे लौटा देते हैं।

1. ऊपर के कार्ड देखें: **Dealer Name**, **Dealer Code**, **Submitted By** और **Status**।
2. प्रोग्रेस बार पर हर लेवल दिखता है: **MO**, **RM**, **SM** और **AVP**, साथ में **Under Review**, **Pending**, **Approved** या **Rejected**।
3. **Timeline** और **Action History** में दिखता है कि किसने, कब क्या किया, उनके रिमार्क और उन्होंने कितनी क्रेडिट लिमिट दी।
4. हर हिस्से को खोलकर जाँचने के लिए उस पर टैप करें: **Company Details**, **Experience In Fertilizer Trade**, **Annual Sales Data**, **Proprietor Details**, **Investment**, **Credit Limit Proposal**, **Enclosures** और बाकी।
5. अगर कुछ गलत है, तो **Send Back** दबाएँ ("एप्लिकेशन लौटाएँ" देखें)।
6. अगर सब सही है, तो **Proceed to SM** (RM), **Proceed to AVP** (SM) या **Approve** (AVP) दबाएँ। **Confirm Proceed** विंडो में अपनी क्रेडिट लिमिट और **Remarks** भरें, फिर **Confirm & Proceed** दबाएँ।

> Careful: आगे भेजने के बाद एप्लिकेशन अगले लेवल पर चला जाता है और आप उसे बदल नहीं सकते।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **RM Credit Limit (SPIC)**, **RM Credit Limit (GFL)** | RM जो लिमिट सुझाता है। RM को दिखती है। |
| **SM Credit Limit (SPIC)**, **SM Credit Limit (GFL)** | SM जो लिमिट सुझाता है। RM की लिमिट ऊपर **(Approved)** के रूप में दिखती हैं। |
| **AVP Credit Limit (SPIC)**, **AVP Credit Limit (GFL)** | आख़िरी लिमिट। RM और SM की लिमिट ऊपर दिखती हैं। |
| **Remarks** | आपकी टिप्पणी। नए डीलर के लिए ज़रूरी, बाकी के लिए अपनी मर्ज़ी से। |

## आम दिक्कतें

- **Proceed या Send Back बटन नहीं है।** एप्लिकेशन अभी आपका इंतज़ार नहीं कर रहा। आप उसे सिर्फ़ पढ़ सकते हैं।
- **हरा "Credit Limit Consent" बॉक्स।** डीलर ने क्रेडिट लिमिट की जानकारी के बिना आवेदन किया है, इसलिए क्रेडिट लिमिट 0 है।
- **स्क्रीन Dashboard पर चली जाती है।** यह सामान्य है। मंज़ूर करने के बाद ऐप सफलता का मैसेज दिखाता है और Dashboard खोलता है।
:::

::: section send-back
title: एप्लिकेशन लौटाएँ
routes: /SavedDealerReview
for: 
roles: RM, RMD, SMD, SMM, AVP, Director, Admin, CorporateAdmin, SuperAdmin
designations: 
minutes: 2
source-hash: b3b23aec15238c4a
confidence: ok

**यह स्क्रीन किस काम की है**
अगर एप्लिकेशन में गलतियाँ हैं, तो कारण लिखकर उसे एक लेवल पीछे भेजें। RM उसे **Marketing Officer (MO)** को भेजता है, SM उसे **Regional Manager (RM)** को, और AVP उसे **State Manager (SM)** को।

1. एप्लिकेशन खोलें और नीचे **Send Back** दबाएँ।
2. **Send Back Application** विंडो में **Application Summary** और **Send Back To** देखें।
3. **Reason for Sending Back** लिखें। यह ज़रूरी है।
4. **Sections Requiring Correction** में ठीक करने वाले हिस्सों पर टिक करें, जैसे **Tax & Compliance**, **Investment** या **Enclosures**। कुछ पर पहले से टिक होता है; जो हिस्से सही हैं, उनका टिक हटा दें।
5. **Priority Level** चुनें: **High**, **Medium** या **Low**।
6. **Send Back to MO**, **Send Back to RM** या **Send Back to SM** दबाएँ (बटन पर आपका लेवल दिखता है), या रोकने के लिए **Cancel** दबाएँ।

## आम दिक्कतें

- **"Reason is required."** (कारण लिखना ज़रूरी है) पहले कारण लिखें।
- **"Please select at least one correction section."** (कम से कम एक सुधार वाला हिस्सा चुनें) कम से कम एक हिस्से पर टिक करें।
- **"Unable to send back dealer application."** (डीलर एप्लिकेशन लौटाया नहीं जा सका) अपना इंटरनेट देखें और फिर से कोशिश करें।
:::

::: section avp-office-ids
title: नए डीलर के लिए डीलर ID भरें
routes: /SavedDealerReview
for: 
roles: CommonRole
designations: AVP Office
minutes: 2
source-hash: e77b528ade25e538
confidence: ok

**यह स्क्रीन किस काम की है**
AVP के नए डीलर को मंज़ूर करने के बाद AVP Office कंपनी के डीलर ID भरता है। इससे एप्लिकेशन पूरा हो जाता है।

1. जाँच वाली लिस्ट से एप्लिकेशन खोलें। उसके **Current Stage** में **AVP Office** दिखता है।
2. नीचे **AVP Office - Dealer ID Submission** बॉक्स तक स्क्रॉल करें।
3. **SPIC Dealer ID** और/या **GFL Dealer ID** भरें। सिर्फ़ वही कंपनियाँ दिखती हैं जिनके लिए डीलर ने आवेदन किया है।
4. ज़रूरत हो तो **Remarks (optional)** लिखें।
5. **Submit Dealer IDs** दबाएँ। ऐप सफलता का मैसेज दिखाता है और Dashboard खोलता है।

## आम दिक्कतें

- **"SPIC Dealer ID is required for this application."** (इस एप्लिकेशन के लिए SPIC डीलर ID ज़रूरी है) डीलर SPIC के साथ है। SPIC डीलर ID भरें।
- **"GFL Dealer ID is required for this application."** (इस एप्लिकेशन के लिए GFL डीलर ID ज़रूरी है) डीलर GFL के साथ है। GFL डीलर ID भरें।
- **मुझे बॉक्स नहीं दिख रहा।** AVP ने अभी इसे मंज़ूर नहीं किया है, या ID पहले ही भरे जा चुके हैं।
:::

::: section view-application
title: एप्लिकेशन देखें (सिर्फ़ पढ़ने के लिए)
routes: /ReviewDealer
for: 
roles: 
designations: 
minutes: 1
source-hash: ee8404c8e5c3aad3
confidence: ok

**यह स्क्रीन किस काम की है**
यह पुरानी **Review - Dealer Application** स्क्रीन बिना किसी मंज़ूरी वाले बटन के एक एप्लिकेशन दिखाती है।

1. ऊपर के कार्ड देखें: **Dealer Name**, **Dealer ID**, **Total Experience**, **Total Sales**, **Investment**, **Credit Limit** और **Status**।
2. हर हिस्सा खोलने के लिए उस पर टैप करें, जैसे **Company Details** या **Investment**।
3. **Enclosures** में कोई दस्तावेज़ खोलने के लिए उसके नाम पर टैप करें।

> Tip: मंज़ूर करना या लौटाना हो, तो इसकी जगह **Dealer Applications Review** लिस्ट से एप्लिकेशन खोलें।

## आम दिक्कतें

- **स्क्रीन खाली है।** यह तभी खुलती है जब कोई एप्लिकेशन चुना गया हो। जाँच वाली लिस्ट पर जाएँ और आँख वाला आइकन इस्तेमाल करें।
:::

::: section sm-approvals
title: सब डीलर और कर्मचारियों को मंज़ूरी दें (SM)
routes: /SMMApprovals
for: 
roles: SMD, SMM
designations: 
tour: 1 > [data-help=dr-sm-tabs] | 2 > [data-help=dr-sm-cards] | 3 > [data-help=dr-sm-table] | 4 > [data-help=dr-sm-table]
minutes: 2
source-hash: e13ee4a27ffd7b42
confidence: ok

**यह स्क्रीन किस काम की है**
**SM Approval** स्क्रीन पर आपके राज्य के डीलरों के लिए जोड़े गए सब डीलर और कर्मचारी दिखते हैं। पहले SM उन्हें जाँचता है; फिर वे AVP के पास जाते हैं।

1. एक टैब चुनें: **All Pending**, **Sub Dealers** या **Employees**।
2. कार्ड पर **Total Pending**, **Sub Dealers** और **Employees** दिखते हैं। एक ही तरह के देखने के लिए **Filter** इस्तेमाल करें।
3. **Pending SM Approvals** में **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** और **Nominee** देखें।
4. **Approve** या **Reject** दबाएँ, **Remarks** लिखें, फिर **Confirm Approve** या **Confirm Reject** दबाएँ।

> Tip: मंज़ूरी के लिए रिमार्क लिखना ज़रूरी नहीं है, पर रिजेक्ट करते समय कारण ज़रूर लिखें।

## आम दिक्कतें

- **"No Pending Approvals".** (कोई मंज़ूरी बाकी नहीं) अभी आपके पास कुछ भी बाकी नहीं है।
- **मुझे "Show more" दिख रहा है।** शुरू में सिर्फ़ 50 लाइनें दिखती हैं। बाकी देखने के लिए **Show more** दबाएँ।
:::

::: section avp-approvals
title: सब डीलर और कर्मचारियों की आख़िरी मंज़ूरी (AVP)
routes: /AVPApprovals
for: 
roles: AVP
designations: 
minutes: 2
source-hash: 61b6fcc972ffa53e
confidence: ok

**यह स्क्रीन किस काम की है**
**AVP Approval** स्क्रीन पर वे सब डीलर और कर्मचारी दिखते हैं जिन्हें SM पहले ही मंज़ूर कर चुका है। AVP आख़िरी मंज़ूरी देता है।

1. एक टैब चुनें: **All Pending**, **Sub Dealers** या **Employees**।
2. **Total Pending**, **Sub Dealers** और **Employees** कार्ड देखें। एक ही तरह के देखने के लिए **Filter** इस्तेमाल करें।
3. **Pending AVP Approvals** में **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** और **Nominee** देखें।
4. **Approve** या **Reject** दबाएँ। विंडो में **SM Status: Approved** दिखता है।
5. **Remarks** लिखें और **Confirm Approve** या **Confirm Reject** दबाएँ।

## आम दिक्कतें

- **"No Pending AVP Approvals".** (AVP की कोई मंज़ूरी बाकी नहीं) SM से मंज़ूर हुआ कोई रिकॉर्ड आपका इंतज़ार नहीं कर रहा।
- **"Action failed".** (काम नहीं हो पाया) फिर से कोशिश करें। अगर बार-बार ऐसा हो, तो अपने admin को बताएँ।
:::

::: section credit-limit-sales
title: क्रेडिट लिमिट के लिए डीलर की बिक्री दर्ज करें
routes: /CreditLimitSales
for: 
roles: 
designations: 
minutes: 3
source-hash: 580721465ba2267a
confidence: ok

**यह स्क्रीन किस काम की है**
**Credit Limit Sales** स्क्रीन पर हर डीलर की पिछली बिक्री साल और प्रोडक्ट के हिसाब से रखी जाती है। जाँचने वाले ये आँकड़े एप्लिकेशन के **Credit Limit Proposal** में देखते हैं।

1. **Add New Credit Limit Sales** में **State** और **Financial Year** चुनें।
2. **Dealer Code** या **Dealer Name** से डीलर चुनें। दोनों बॉक्स एक ही डीलर चुनते हैं।
3. **Product**, **Product Group** और **Category** चुनें।
4. **Quantity** और **Gross Amount** भरें, फिर **Save** दबाएँ। **Cancel** से फ़ॉर्म खाली हो जाता है।
5. एक साथ कई लाइनें जोड़नी हों, तो Excel का नमूना पाने के लिए **Sample Format** दबाएँ, उसे भरें, **Bulk Upload** दबाएँ, फिर **Financial Year** और फ़ाइल चुनें।
6. **Existing Credit Limit Sales** में कोड या नाम से खोजें। पेंसिल से लाइन बदली जाती है और डस्टबिन उसे मिटाता है।

> Careful: मिटाई गई लाइन वापस नहीं लाई जा सकती।

## आम दिक्कतें

- **"All required fields must be filled."** (सारे ज़रूरी फ़ील्ड भरने होंगे) State, Financial Year, डीलर, Product, Product Group और Category चुनें।
- **"Please select a financial year before uploading."** (अपलोड करने से पहले वित्तीय वर्ष चुनें) पहले अपलोड विंडो में साल चुनें।
- **Save, Bulk Upload, पेंसिल या डस्टबिन नहीं दिख रहा।** आपके पद को जोड़ने, बदलने या मिटाने की इजाज़त नहीं है। अपने admin से पूछें।
:::

::: section state-summary
title: राज्य के हिसाब से डीलर का सारांश
routes: /DealerStateSummary
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP
designations: 
minutes: 1
source-hash: d5685e7a98ccb663
confidence: ok

**यह स्क्रीन किस काम की है**
**Total Dealers — State Wise** स्क्रीन हर राज्य के डीलर गिनती है और SPIC और GFL के लिए दिखाती है कि उनके एप्लिकेशन कहाँ हैं।

1. **TOTAL DEALERS** और **COMPLETED** कार्ड देखें।
2. **Dealer Summary** में हर लाइन एक राज्य है। कॉलम में **As Per SAP Data**, **Saved Dealers**, **MO**, **RM**, **SMM**, **AVP** और **Completed** दिखते हैं, हर एक **SPIC** और **GFL** के लिए।
3. किसी कॉलम के हिसाब से क्रम लगाने के लिए उसके नाम पर टैप करें। उल्टा क्रम लगाने के लिए फिर से टैप करें।
4. नीचे की **Total** लाइन में सारे राज्यों का जोड़ होता है।
5. नए आँकड़े लाने के लिए **Refresh** दबाएँ, या **Back to Dashboard** दबाएँ।

## आम दिक्कतें

- **"Access restricted".** (इजाज़त नहीं है) यह रिपोर्ट सिर्फ़ Admin, Director और AVP यूज़र के लिए है।
- **किसी राज्य के आगे "Unknown" दिख रहा है।** राज्यों की लिस्ट लोड नहीं हुई। **Refresh** दबाएँ।
:::

::: section dealer-directory
title: डीलर डायरेक्टरी
routes: /DealerDirectory
for: 
roles: 
designations: 
minutes: 1
source-hash: db991abf0063ad82
confidence: ok

**यह स्क्रीन किस काम की है**
**Dealer Directory** स्क्रीन आने वाली डीलर लिस्ट की एक झलक है। इस पर दिखने वाले आँकड़े और लाइनें सिर्फ़ उदाहरण हैं, और इसके बटन और फ़िल्टर अभी काम नहीं करते।

1. बनावट देखें: **Total Dealers**, **Active Dealers**, **Regions Covered** और **Recently Added** के कार्ड।
2. **Filters** बॉक्स में आप **State**, **Region**, **Head Quarters** और **Status** चुन सकेंगे।
3. **Dealer List** टेबल में **Dealer Code**, **Firm Name**, **State**, **Head Quarters**, **Mobile No.**, **Email** और **Status** दिखेंगे।

> Tip: असली एप्लिकेशन ढूँढना हो, तो **Dealer Applications Review** लिस्ट इस्तेमाल करें।

## आम दिक्कतें

- **Excel, PDF या Print कुछ नहीं करता।** ये बटन अभी काम नहीं करते।
:::

::: section dealer-mapping
title: डीलरों को सेल्स ऑडिट प्रोग्राम से जोड़ें
routes: /DealerMapping
for: 
roles: 
designations: 
minutes: 3
source-hash: 42aed46703ffd3d8
confidence: ok

**यह स्क्रीन किस काम की है**
**Dealer Mapping** स्क्रीन सेल्स ऑडिट प्रोग्राम का तीसरा स्टेप, **Dealers**, है। आप प्रोग्राम के डीलरों की लिस्ट अपलोड करते हैं और उनकी SAP बिक्री को प्लान से मिलाकर देखते हैं।

1. इसे **Sales Plan** से **Save & Continue** या **Continue to Dealer Mapping** दबाकर खोलें।
2. **Upload Dealer List** में Excel फ़ाइल डालें या **Upload File** दबाएँ। फ़ाइल में **Dealer Code** कॉलम होना चाहिए।
3. मैसेज पढ़ें। अगर कुछ लाइनें छोड़ दी गईं, तो **Row No.**, **Dealer Code** और **Reason** देखने के लिए **View Errors** दबाएँ।
4. **Dealer List** में कोड या नाम से खोजें। डस्टबिन डीलर को प्रोग्राम से हटा देता है।
5. **Dealer Sales Achievement - SAP Invoice Matches** में तय की गई और असली बिक्री देखें। इन्हें फिर से लोड करने के लिए **Refresh Matches** दबाएँ।
6. **Plan Summary & Comparison** देखें: **Required Turnover**, **Achievement Amount**, **Difference** और **Progress %**।
7. **Continue to Plan Review** दबाएँ, या **Back to Sales Plan** दबाएँ।

## आम दिक्कतें

- **"Missing CSR-2 ID. Open this page from Sales Plan."** (CSR-2 ID नहीं है। यह पेज Sales Plan से खोलें) सेव किए गए लिंक से नहीं, बल्कि Sales Plan से यह स्क्रीन खोलें।
- **"Upload an .xlsx or .xls file up to 5 MB."** (5 MB तक की .xlsx या .xls फ़ाइल अपलोड करें) 5 MB से छोटी Excel फ़ाइल इस्तेमाल करें।
- **Continue to Plan Review ग्रे है।** पहले कम से कम एक डीलर अपलोड करें।
:::

::: section training-program
title: डीलर ट्रेनिंग प्रोग्राम की रिपोर्ट दें (CSR-2)
routes: /DealerManagementTrainingProgram
for: 
roles: 
designations: 
minutes: 5
source-hash: f2c7c96961e19cd8
confidence: ok

**यह स्क्रीन किस काम की है**
CSR-1 प्लान मंज़ूर होने के बाद, यह बताने के लिए इस स्क्रीन का इस्तेमाल करें कि प्रोग्राम असल में कैसा रहा: तारीख, फ़ोटो, शामिल लोग और खर्च। यह **CSR-2 Program Execution** लिस्ट से **View Program** दबाने पर खुलती है।

1. **Plan & Start** पर **Approved CSR-1 Plan** देखें। **Focus Details**, **Actual Event Date**, **Start Time**, **End Time** और **Actual Location** भरें। कार्यक्रम के फ़ोटो-वीडियो के लिए **Take Photo** और **Record Video** इस्तेमाल करें। **Start Event** दबाएँ, फिर पक्का करने के लिए दोबारा **Start Event** दबाएँ।
2. **Participants & Employees** पर किसी शामिल व्यक्ति को **Phone Number** से खोजें, **Role** चुनें और **Add Participant** दबाएँ। जिसका अभी रजिस्ट्रेशन नहीं हुआ है, उसके लिए **New Farmer** इस्तेमाल करें। कर्मचारियों को **Add Employee** से जोड़ें।
3. **Expense & Proof** पर हर खर्च के लिए **Actual amount (₹)**, **Bill No.** और **Date** भरें, और **Upload proofs** दबाएँ। कार्ड पर **Approved Budget** और **Actual Expense** की तुलना दिखती है।
4. **Review & Submit** पर सब कुछ देखें। पीछे जाकर बदलने के लिए किसी कार्ड पर **Edit** दबाएँ।
5. बाद में पूरा करना हो तो **Save Draft** दबाएँ। भेजने के लिए **Apply CSR-2** दबाएँ, या अगर यह आपका इंतज़ार कर रहा है तो **Validate CSR-2** या मंज़ूरी वाला बटन दबाएँ।

> Tip: फ़ोटो और वीडियो पर GPS जगह, तारीख और समय छपा होता है।

## आम दिक्कतें

- **"CSR-2 can be opened only for an approved CSR-1."** (CSR-2 सिर्फ़ मंज़ूर हुए CSR-1 के लिए खुलता है) CSR-1 प्लान मंज़ूर होने तक इंतज़ार करें।
- **मुझे सिर्फ़ दो स्टेप दिख रहे हैं।** कुछ रोल सिर्फ़ **Plan & Start** और **Participants & Employees** भरते हैं, फिर **Apply CSR-2** दबाते हैं।
- **"Enter a valid 10-digit mobile number."** (सही 10 अंकों का मोबाइल नंबर भरें) फ़ोन नंबर जाँचें।
:::
