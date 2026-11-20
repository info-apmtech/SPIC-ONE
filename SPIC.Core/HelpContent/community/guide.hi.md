---
module: community
title: कम्युनिटी
icon: bi-chat-square-text
order: 115
language: hi
category: ज्ञान
note: नई गाइड
meta-source-hash: 1b034d33bc7629a5
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 59061c3615515658
confidence: ok

**SPIC Knowledge Community** एक ऐसी जगह है जहाँ आप प्रोडक्ट और खेती के बारे में सवाल पूछ सकते हैं, खेत में जो देखा वह बता सकते हैं, और SPIC के एक्सपर्ट और दूसरे किसानों से जवाब पा सकते हैं।

- ask | icon: bi-patch-question | color: #0d9488
  title: सवाल पूछें
  text: अपनी दिक्कत अपने शब्दों में लिखें। अगर काम आए तो फ़ोटो या दस्तावेज़ भी जोड़ें।
- answer | icon: bi-patch-check | color: #16a34a
  title: एक्सपर्ट से जवाब पाएँ
  text: SPIC के कर्मचारी **SPIC Expert** के नाम से जवाब देते हैं। दूसरे किसान और डीलर भी जवाब दे सकते हैं।
- learn | icon: bi-people | color: #2563eb
  title: दूसरों से सीखें
  text: पुरानी चर्चाएँ खोजें, जो पसंद आएँ उन्हें फ़ॉलो करें और प्रोडक्ट ग्रुप से जुड़ें।
:::

::: section flow
title: सवाल कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 8b3d8c2b9b5b6d84
confidence: ok

हर लाइन लोगों का एक ग्रुप है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- members | type: lane | color: #0d9488 | roles: Farmer, Dealer
  title: किसान और डीलर
  sub: पूछें, जवाब दें, शेयर करें
- experts | type: lane | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC एक्सपर्ट
  sub: SPIC कर्मचारी
- ask | lane: members | col: 1 | icon: bi-pencil-square | color: #d97706 | next: reply, expert | screen: ask-question
  title: सवाल पूछें
  status: Waiting For Reply
  text: **Create a Discussion** फ़ॉर्म भरें और **Post Discussion** दबाएँ। सवाल अपने पहले जवाब का इंतज़ार करता है।
- similar | lane: members | col: 2 | icon: bi-search | color: #ea580c | kind: exception | from: ask | to: ask | screen: ask-question
  title: मिलता-जुलता सवाल मिला
  text: ऐप आपके सवाल जैसी दिखने वाली चर्चाएँ दिखाता है। जवाब पढ़ने के लिए उनमें से एक खोलें, **Back to edit** दबाएँ, या **Post Anyway** दबाएँ।
- reply | lane: members | col: 3 | icon: bi-chat-dots | color: #2563eb | next: resolve | screen: discussion-details
  title: कोई सदस्य जवाब देता है
  status: Open
  text: कोई दूसरा किसान या डीलर जवाब देता है। जो सवाल इंतज़ार में था, वह **Open** हो जाता है।
- expert | lane: experts | col: 3 | icon: bi-patch-check | color: #16a34a | next: resolve | screen: discussion-details
  title: एक्सपर्ट जवाब देता है
  status: Expert Answer
  text: जब SPIC के कर्मचारी जवाब देते हैं, तो चर्चा पर **Expert Answer** दिखता है।
- resolve | lane: members | col: 4 | icon: bi-check2-circle | color: #64748b | return: reply | screen: discussion-details
  title: हल हो गया, मार्क करें
  status: Resolved
  text: दिक्कत हल हो जाने पर सवाल पूछने वाला **Mark as Resolved** दबाता है। **Reopen** दबाने से चर्चा फिर से **Open** हो जाती है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: af226e0559d41a5d
confidence: ok

- waiting | color: #d97706
  label: Waiting For Reply
  text: पोस्ट हो गया है, पर अभी तक किसी ने जवाब नहीं दिया।
- waiting-answers | color: #d97706
  label: Waiting for Answers
  text: सवाल पूछने के ठीक बाद "posted" स्क्रीन पर दिखता है। इसका मतलब वही है जो Waiting For Reply का है।
- open | color: #2563eb
  label: Open
  text: सदस्यों ने जवाब दिए हैं। चर्चा अभी चल रही है।
- expert | color: #16a34a
  label: Expert Answer
  text: किसी SPIC एक्सपर्ट ने जवाब दिया है।
- resolved | color: #64748b
  label: Resolved
  text: सवाल पूछने वाले ने दिक्कत को हल मार्क कर दिया है। इसे फिर से खोला जा सकता है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: b913d791fc7175cc
confidence: ok

आपका कार्ड हाइलाइट होता है। जो भी साइन इन करता है, वह चर्चाएँ पढ़ सकता है और उनमें हिस्सा ले सकता है।

- members | color: #0d9488 | roles: Farmer, Dealer
  title: किसान और डीलर
  - सवाल पूछना और फ़ोटो या फ़ाइल जोड़ना
  - चर्चाओं में जवाब देना, लाइक करना, सेव करना और फ़ॉलो करना
  - अपने सवाल बदलना, मिटाना या हल मार्क करना
- experts | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC एक्सपर्ट (SPIC कर्मचारी)
  - सवालों के जवाब देना। आपके जवाबों पर **SPIC Expert** दिखता है
  - किसी भी सदस्य की तरह सवाल पूछना और शेयर करना
- moderators | color: #475569 | roles: Admin, CorporateAdmin
  title: Admin और Corporate Admin
  - किसी भी चर्चा को हल मार्क करना, या फिर से खोलना
  - किसी भी जवाब को मिटाना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 971404c02ac40d12
confidence: ok

- cannot-post | popular: true
  q: मैं Post Discussion दबाता हूँ और लाल मैसेज आ जाते हैं
  a: टाइटल कम से कम 10 अक्षर का होना चाहिए, एक **Category** चुनी होनी चाहिए, और **Description** कम से कम 20 अक्षर का होना चाहिए। लाल वाला फ़ील्ड ठीक करें और फिर से **Post Discussion** दबाएँ।
- similar | popular: true
  q: ऐप कहता है "We found similar discussions"। मैं क्या करूँ?
  a: हो सकता है आपके सवाल का जवाब पहले से हो। उसे पढ़ने के लिए **View Discussion** दबाएँ। अगर आपका सवाल अलग है, तो **Post Anyway** दबाएँ। अपना लिखा बदलने के लिए **Back to edit** दबाएँ।
- edit-delete
  q: मैं अपना सवाल कैसे बदलूँ या मिटाऊँ?
  a: **Discussions** खोलें, **My Discussions** टैब पर जाएँ और अपने सवाल पर तीन बिंदुओं पर टैप करें। **Edit** या **Delete** चुनें।
- no-resolve
  q: मुझे Mark as Resolved नहीं दिख रहा
  a: सिर्फ़ सवाल पूछने वाला या कोई Admin ही चर्चा को हल मार्क कर सकता है या फिर से खोल सकता है।
- file-not-attached
  q: मेरी फ़ाइल या फ़ोटो नहीं जुड़ी
  a: सवाल पर हर फ़ाइल 10 MB या उससे छोटी होनी चाहिए, और एक सवाल में ज़्यादा से ज़्यादा 6 फ़ाइलें जुड़ सकती हैं। एक जवाब में 5 MB तक की ज़्यादा से ज़्यादा 3 इमेज जुड़ सकती हैं।
- expert-answers
  q: SPIC एक्सपर्ट के जवाब कैसे ढूँढूँ?
  a: **Discussions** खोलें और **Expert Answers** टैब पर टैप करें। किसी चर्चा में एक्सपर्ट के जवाबों पर **SPIC Expert** टैग दिखता है।
:::

::: section community-home
title: Knowledge Community का होम पेज इस्तेमाल करें
routes: /Community
for: 
roles: 
designations: 
minutes: 2
source-hash: ea1bb46391aec31c
confidence: ok

**यह स्क्रीन किस काम की है**
**SPIC Knowledge Community** होम पेज से आप शुरुआत करते हैं। यहाँ आप सवाल पूछ सकते हैं, नई चर्चाएँ देख सकते हैं और किसी प्रोडक्ट की चर्चाएँ खोल सकते हैं।

1. सर्च बॉक्स में अपना सवाल या प्रोडक्ट का नाम टाइप करें और **Ask a Question** दबाएँ। सवाल का फ़ॉर्म खुलता है, जिसमें आपके शब्द टाइटल बन जाते हैं।
2. सर्च बॉक्स के नीचे की गिनती देखें: **Community Members**, **Discussions** और **Expert Answer**।
3. **Recent Discussions** में किसी चर्चा पर टैप करके उसे खोलें। लिस्ट को ग्रिड में या लाइनों में दिखाने के लिए दो छोटे बटन इस्तेमाल करें।
4. टैब और फ़िल्टर के साथ पूरी लिस्ट देखने के लिए **View All Discussions** दबाएँ।
5. **Popular Product** में किसी प्रोडक्ट ग्रुप से जुड़ने के लिए **Join** दबाएँ। यह **Joined** हो जाता है। ग्रुप छोड़ने के लिए इसे फिर से दबाएँ।
6. सिर्फ़ उस प्रोडक्ट की चर्चाएँ देखने के लिए प्रोडक्ट कार्ड पर **Discussions** दबाएँ।
7. नया सवाल शुरू करने के लिए **New Here?** बॉक्स में **Ask a Question** दबाएँ।

> Tip: आपने ग्रिड चुना या लाइनें, ऐप यह याद रखता है।

## प्रोडक्ट की फ़ोटो बदलना

- जो Admin और कर्मचारी Digital Library बदल सकते हैं, उन्हें हर प्रोडक्ट फ़ोटो पर कैमरे का आइकन दिखता है।
- कैमरे पर टैप करें और 3 MB तक की JPG, PNG या WEBP फ़ोटो चुनें।

## आम दिक्कतें

- **"Discussions could not be loaded".** (चर्चाएँ लोड नहीं हो सकीं) कनेक्शन टूट गया। **Try again** दबाएँ।
- **"No discussions yet".** (अभी कोई चर्चा नहीं) अभी तक किसी ने सवाल नहीं पूछा। सबसे पहले पूछने के लिए **Ask a Question** दबाएँ।
- **"That picture is larger than 3 MB."** (फ़ोटो 3 MB से बड़ी है) प्रोडक्ट की छोटी फ़ोटो चुनें।
:::

::: section browse-discussions
title: चर्चाएँ ढूँढें और फ़ॉलो करें
routes: /Community/discussions
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-list-stats] | 2 > [data-help=cm-list-tabs] | 3 > [data-help=cm-list-filters] | 4 > [data-help=cm-list-tools] | 5 > [data-help=cm-list-card] | 6 > [data-help=cm-list-new]
minutes: 3
source-hash: 6111e390f7350721
confidence: ok

**यह स्क्रीन किस काम की है**
**Discussions** स्क्रीन पर कम्युनिटी का हर सवाल दिखता है। आप खोज सकते हैं, फ़िल्टर लगा सकते हैं, चर्चाएँ फ़ॉलो कर सकते हैं और अपने सवाल संभाल सकते हैं।

1. ऊपर की टाइलें देखें: **Total Discussions**, **Active Members**, **Expert Answers** और **New This Month**।
2. एक टैब चुनें: **All Discussion**, **My Discussions**, **Trending**, **Unanswered**, **Following** या **Expert Answers**।
3. **Filters** खोलकर **Date Range**, **Category**, **Product**, **Crop**, **Status** या **Tags** चुनें, फिर **Apply Filters** दबाएँ। **Clear All** दबाने से फ़िल्टर हट जाते हैं।
4. कोई विषय ढूँढने के लिए **Sort by:** (**Latest Activity**, **Most Replies**, **Most Views** या **Oldest**) इस्तेमाल करें और **Search discussions...** में टाइप करें।
5. चर्चा खोलने के लिए उस पर टैप करें। फ़ॉलो करने के लिए व्यक्ति की फ़ोटो के नीचे **Follow** दबाएँ। अपने सवालों पर **Edit** या **Delete** के लिए तीन बिंदुओं पर टैप करें।
6. नया सवाल पूछने के लिए **Start a Discussion** दबाएँ।

> Careful: चर्चा मिटाने पर वह और उसके सारे जवाब कम्युनिटी से हट जाते हैं।

## टैब में क्या दिखता है

- **My Discussions**: आपके पूछे हुए सवाल।
- **Trending**: पिछले 30 दिनों के सबसे ज़्यादा चल रहे सवाल।
- **Unanswered**: जिन सवालों का अभी तक कोई जवाब नहीं है।
- **Following**: जिन सवालों को आप फ़ॉलो करते हैं।
- **Expert Answers**: जिन सवालों का जवाब किसी SPIC एक्सपर्ट ने दिया है।

## आम दिक्कतें

- **"No discussions found".** (कोई चर्चा नहीं मिली) कोई दूसरा टैब आज़माएँ या **Clear filters** दबाएँ।
- **चर्चा पर तीन बिंदु नहीं दिख रहे।** आप सिर्फ़ अपने ही सवाल बदल या मिटा सकते हैं।
- **Tags फ़िल्टर से कुछ नहीं होता।** **Apply Filters** दबाने से पहले टैग टाइप करें और Enter दबाएँ।
:::

::: section ask-question
title: सवाल पूछें
routes: /Community/new, /Community/posted/{id}
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-new-title] | 2 > [data-help=cm-new-choices] | 3 > [data-help=cm-new-tags] | 4 > [data-help=cm-new-desc] | 5 > [data-help=cm-new-files] | 6 > [data-help=cm-new-post]
minutes: 3
source-hash: a42a0e7fda40c257
confidence: ok

**यह स्क्रीन किस काम की है**
कम्युनिटी से सवाल पूछने या अपना अनुभव बताने के लिए **Create a Discussion** स्क्रीन का इस्तेमाल करें। फिर SPIC एक्सपर्ट और दूसरे किसान जवाब दे सकते हैं।

1. **Discussion Title** में अपना सवाल टाइप करें।
2. एक **Category** चुनें। अगर सवाल किसी प्रोडक्ट या फ़सल के बारे में है, तो **Related Product** और **Crop** भी चुनें।
3. **Tags** में कोई छोटा शब्द टाइप करें, जैसे कोई रोग या दिक्कत, और Enter या कॉमा (,) दबाएँ। आप 6 टैग तक जोड़ सकते हैं।
4. **Description** में अपना सवाल लिखें, आपने क्या करके देखा और आपको क्या दिखा।
5. **Attachments** में फ़ोटो या फ़ाइलें बॉक्स में डालें या **Browse Files** दबाएँ। फ़ाइल हटाने के लिए क्रॉस (×) पर टैप करें।
6. **Post Discussion** दबाएँ। अगर ऐप **We found similar discussions** दिखाए, तो वे चर्चाएँ पढ़ें या **Post Anyway** दबाएँ। अगर **Your question looks unique!** (आपका सवाल अलग लगता है) दिखे, तो फिर से **Post Discussion** दबाएँ।

> Tip: पत्ती, पौधे या प्रोडक्ट के लेबल की साफ़ फ़ोटो से एक्सपर्ट जल्दी जवाब दे पाते हैं।

## पोस्ट करने के बाद

- **Your question has been posted** स्क्रीन पर टाइटल, **Category**, **Posted** समय, **Status** और **Views** दिखते हैं।
- अपना सवाल खोलने के लिए **View Discussion** दबाएँ, या होम पेज पर जाने के लिए **Back to Community** दबाएँ।

## अपना सवाल बदलें

- **Discussions** पर अपने सवाल के तीन बिंदुओं पर टैप करें और **Edit** चुनें। वही फ़ॉर्म खुलता है।
- लिखा हुआ बदलें और **Save Changes** दबाएँ।
- आपका टाइप किया हुआ हटाने से पहले **Cancel** आपसे पूछता है। वहीं रहने के लिए **Keep editing** दबाएँ।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Discussion Title** | एक लाइन में आपका सवाल। कम से कम 10 अक्षर। ज़रूरी है। |
| **Category** | सवाल किस तरह का है। ज़रूरी है। |
| **Related Product** | जिस SPIC प्रोडक्ट के बारे में सवाल है, अगर कोई है। |
| **Crop** | फ़सल, जैसे धान या टमाटर। |
| **Tags** | 6 तक छोटे शब्द, जिनसे दूसरों को सवाल ढूँढने में मदद मिले। |
| **Description** | पूरा सवाल। कम से कम 20 अक्षर। ज़रूरी है। |
| **Attachments** | इमेज, PDF या Word फ़ाइलें, हर एक 10 MB तक। |

## आम दिक्कतें

- **"Please enter a discussion title."** या **"The title needs at least 10 characters."** (टाइटल लिखें / टाइटल कम से कम 10 अक्षर का हो) लंबा टाइटल लिखें।
- **"Please add a little more detail (at least 20 characters)."** (थोड़ी और जानकारी लिखें, कम से कम 20 अक्षर) **Description** में और लिखें।
- **"That file is larger than 10 MB and was not attached."** (फ़ाइल 10 MB से बड़ी है, इसलिए नहीं जुड़ी) फ़ाइल छोटी करें या नई फ़ोटो लें।
- **"Your question was posted, but the attachments could not be uploaded."** (सवाल पोस्ट हो गया, पर फ़ाइलें अपलोड नहीं हुईं) सवाल सेव हो गया है। अपना कनेक्शन देखें।
:::

::: section discussion-details
title: चर्चा पढ़ें और जवाब दें
routes: /Community/discussion/{id}
for: 
roles: 
designations: 
minutes: 3
source-hash: 59af4bc85c32cb5c
confidence: ok

**यह स्क्रीन किस काम की है**
**Discussion Details** स्क्रीन पर एक सवाल, उसकी फ़ोटो और फ़ाइलें, और सारे जवाब दिखते हैं। यहाँ आप जवाब देते हैं, लाइक करते हैं, सेव करते हैं, फ़ॉलो करते हैं और शेयर करते हैं।

1. ऊपर देखें कि सवाल किसने पूछा, उनका टैग (**Farmer**, **Dealer** या **SPIC Expert**), तारीख और स्टेटस।
2. सवाल पढ़ें। फ़ोटो बड़ी देखने के लिए उस पर टैप करें। फ़ाइल खोलने के लिए उस पर टैप करें।
3. ऊपर के **Save**, **Share** और **Follow** इस्तेमाल करें, और सवाल को लाइक करने के लिए अंगूठे वाला बटन दबाएँ।
4. **Replied** में जवाब पढ़ें। **Latest**, **Oldest** या **Most Liked** चुनने के लिए **Sort by:** इस्तेमाल करें।
5. किसी एक जवाब का जवाब देने के लिए उसके **reply** बटन पर टैप करें। उस व्यक्ति के नाम के साथ एक बॉक्स खुलता है। अपना जवाब लिखें और **Reply** दबाएँ।
6. सवाल का जवाब देने के लिए नीचे **Write a reply...** में टाइप करें। ज़रूरत हो तो 3 इमेज तक या एक लिंक जोड़ें, फिर भेजने वाला बटन दबाएँ।
7. जब आपकी दिक्कत हल हो जाए, तो **Mark as Resolved** दबाएँ और पक्का करें। और मदद चाहिए तो **Reopen** दबाएँ।

> Tip: कंप्यूटर पर Enter दबाने से जवाब चला जाता है। Shift और Enter साथ दबाने से नई लाइन शुरू होती है।
> Careful: किसी जवाब पर डस्टबिन आइकन उस जवाब को मिटा देता है। आप सिर्फ़ अपने ही जवाब मिटा सकते हैं। Admin कोई भी जवाब मिटा सकता है।

## आम दिक्कतें

- **"Discussion not found".** (चर्चा नहीं मिली) सवाल मिटा दिया गया है। **Back to Discussions** दबाएँ।
- **"Please write a few words with your images."** (इमेज के साथ कुछ शब्द लिखें) जवाब में सिर्फ़ इमेज नहीं चलेंगी, कुछ लिखना ज़रूरी है।
- **"A reply can carry up to 3 images."** (एक जवाब में 3 इमेज तक जुड़ सकती हैं) एक इमेज हटाएँ या बाकी इमेज दूसरे जवाब में पोस्ट करें।
- **Mark as Resolved बटन नहीं दिख रहा।** सिर्फ़ सवाल पूछने वाला या कोई Admin ही इसे हल मार्क कर सकता है या फिर से खोल सकता है।
:::
