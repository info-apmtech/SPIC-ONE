---
module: schemes
title: स्कीम और लकी ड्रॉ
icon: bi-gift
order: 104
language: hi
category: बिक्री
note: नई गाइड
meta-source-hash: b8a21e045ebf822e
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 01c42ebca4d8bbab
confidence: ok

SPIC के प्रोडक्ट खरीदने वाले डीलरों और किसानों को स्कीम से इनाम मिलता है। हर खरीद पर एक लकी ड्रॉ एंट्री मिलती है। ड्रॉ की तारीख पर विजेता चुने जाते हैं और उनका इनाम ऐप में दिखता है।

- create | icon: bi-pencil-square | color: #d97706
  title: स्कीम टीम इसे तैयार करती है
  text: नाम, जगहें, प्रोडक्ट के नियम, इनाम और ड्रॉ की तारीखें।
- join | icon: bi-qr-code-scan | color: #0d9488
  title: डीलर और किसान हिस्सा लेते हैं
  text: प्रोडक्ट का QR कोड स्कैन करें। हर खरीद पर 1 एंट्री मिलती है।
- win | icon: bi-trophy | color: #7c3aed
  title: लकी ड्रॉ से विजेता चुने जाते हैं
  text: विजेताओं को मैसेज, इनाम और उसे पाने का तरीका दिखता है।
:::

::: section flow
title: स्कीम कैसे आगे बढ़ती है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 801d9ad88aaa016d
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- team | type: lane | color: #d97706 | roles: Admin, CorporateAdmin
  title: स्कीम टीम
  sub: ऑफ़िस स्टाफ़
- people | type: lane | color: #0d9488 | roles: Dealer, Farmer
  title: डीलर और किसान
  sub: स्कीम के सदस्य
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: review | screen: add-scheme
  title: स्कीम बनाएँ
  status: Draft
  text: **Create Scheme** फ़ॉर्म में जगहें, प्रोडक्ट के नियम, इनाम और ड्रॉ की सेटिंग भरें, फिर **Next** दबाएँ।
- review | lane: team | col: 2 | icon: bi-clipboard-check | color: #d97706 | next: scan | screen: review-scheme
  title: जाँचें और शुरू करें
  status: Active
  text: **Review Your Scheme** पर हर जानकारी जाँचें, फिर स्कीम शुरू करने के लिए **Active Scheme** दबाएँ।
- scan | lane: people | col: 3 | icon: bi-qr-code-scan | color: #0d9488 | next: draw | screen: lucky-draw
  title: खरीदें और स्कैन करें
  status: Ticket Activated
  text: प्रोडक्ट का QR कोड स्कैन करें और खरीदे गए प्रोडक्ट चुनें। हर खरीद पर 1 लकी ड्रॉ एंट्री मिलती है।
- draw | lane: team | col: 4 | icon: bi-ticket-perforated | color: #2563eb | next: announce | screen: scheme-overview
  title: लकी ड्रॉ
  status: Selected
  text: ड्रॉ की तारीख पर सभी एंट्री में से विजेता चुने जाते हैं। ड्रॉ **Automatic** या **Manual** होता है, जैसा स्कीम में तय किया गया हो।
- announce | lane: people | col: 5 | icon: bi-megaphone | color: #7c3aed | next: prize | screen: winner-announcement
  title: विजेता को बताया जाता है
  status: Notified
  text: विजेता को इनाम और स्कीम के नाम के साथ **Congratulations!** दिखता है।
- prize | lane: people | col: 6 | icon: bi-gift | color: #16a34a | screen: winner-details
  title: इनाम पाएँ
  status: Prize Distributed
  text: **Winner Details** पर इनाम, उसका स्टेटस और उसे देने की तारीख दिखती है।
- not-won | lane: people | col: 4 | icon: bi-emoji-smile | color: #ea580c | kind: exception | from: draw | to: scan
  title: इस बार नहीं चुने गए
  status: Better Luck Next Time!
  text: खरीदते और स्कैन करते रहें। ज़्यादा एंट्री से अगले ड्रॉ में ज़्यादा मौके मिलते हैं।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 663b2eff26f57f93
confidence: ok

- draft | color: #64748b
  label: Draft
  text: स्कीम सेव है पर पब्लिश नहीं हुई। डीलर और किसान इसे नहीं देख सकते।
- active-draft | color: #d97706
  label: ACTIVE DRAFT
  text: स्कीम शुरू करने से पहले जाँच वाली स्क्रीन पर दिखता है।
- active | color: #16a34a
  label: Active
  text: स्कीम चल रही है। किसी एंट्री पर दिखे तो इसका मतलब है कि वह एंट्री इस राउंड में अभी जीत सकती है।
- participating | color: #0d9488
  label: Participating
  text: डीलर या किसान स्कीम में हिस्सा ले रहा है।
- verified | color: #0891b2
  label: Verified
  text: खरीद या डीलर की जाँच हो चुकी है।
- ticket | color: #2563eb
  label: Ticket Activated
  text: आपकी खरीद के लिए एक नई लकी ड्रॉ एंट्री जुड़ गई है।
- used | color: #64748b
  label: Used
  text: यह एंट्री पहले ही एक ड्रॉ में जा चुकी है।
- expired | color: #475569
  label: Expired
  text: यह एंट्री अब नहीं जीत सकती।
- selected | color: #7c3aed
  label: Selected
  text: ड्रॉ में विजेता के रूप में चुना गया।
- notified | color: #d97706
  label: Notified
  text: विजेता को बता दिया गया है।
- claimed | color: #16a34a
  label: Claimed
  text: विजेता ने इनाम ले लिया है।
- distributed | color: #16a34a
  label: Prize Distributed
  text: इनाम विजेता को दे दिया गया है।
- completed | color: #16a34a
  label: Completed
  text: इस तारीख का ड्रॉ पूरा हो गया है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 89f40baf961ca43b
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- team | color: #d97706 | roles: Admin, CorporateAdmin
  title: स्कीम टीम
  - स्कीम बनाना और जाँचना
  - स्कीम शुरू करना
  - डीलर, एंट्री और विजेताओं पर नज़र रखना
- dealer | color: #0d9488 | roles: Dealer
  title: डीलर
  - प्रोडक्ट स्कैन करना और एंट्री जोड़ना
  - अपनी एंट्री और प्रगति देखना
  - विजेता और अपना इनाम देखना
- farmer | color: #7c3aed | roles: Farmer
  title: किसान
  - खरीदे गए प्रोडक्ट स्कैन करना
  - अपनी एंट्री देखना
  - विजेता और अपना इनाम देखना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9ef969d93e7ac18c
confidence: ok

- how-to-win | popular: true
  q: मुझे जीतने का मौका कैसे मिलेगा?
  a: हर खरीद पर आपको 1 एंट्री मिलती है। एंट्री 100 के राउंड में गिनी जाती हैं। **Grand Draw** खोलने के लिए 100 एंट्री पूरी करें। लकी ड्रॉ पेज पर **How It Works** देखें।
- entries-count | popular: true
  q: मेरे पास कितनी एंट्री हैं?
  a: **Lucky Draw** खोलें और **View All Entries** दबाएँ (फ़ोन पर **View Entries**)। **TOTAL ENTRIES** कार्ड पर इस राउंड की आपकी चालू एंट्री दिखती हैं।
- not-won
  q: मुझे "Better Luck Next Time!" दिख रहा है
  a: इस ड्रॉ में आप नहीं चुने गए। अपनी खरीद स्कैन करते रहें। ज़्यादा एंट्री से अगली बार ज़्यादा मौके मिलते हैं।
- prize
  q: मैं जीत गया। मुझे इनाम कैसे मिलेगा?
  a: **Winner Details** खोलें। **Prize Information** में इनाम और उसका **Status** दिखता है। मदद के लिए **Need Help?** बॉक्स में **Contact Support** दबाएँ।
- change-scheme
  q: क्या स्कीम शुरू होने से पहले मैं उसे बदल सकता हूँ?
  a: हाँ। **Review Scheme** पर प्रोडक्ट का नियम बदलने के लिए पेंसिल, या मिटाने के लिए डस्टबिन इस्तेमाल करें। स्कीम तैयार होने तक उसे पब्लिश किए बिना रखने के लिए **Save Draft** दबाएँ।
:::

::: section scheme-list
title: सभी स्कीम देखें
routes: /Schemes
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-stats] | 2 > [data-help=sch-filters] | 3 > [data-help=sch-grid] | 4 > [data-help=sch-view] | 5 > [data-help=sch-create]
minutes: 2
source-hash: 799c692a3e1f6262
confidence: ok

**यह स्क्रीन किस काम की है**
**Schemes** स्क्रीन पर हर लकी ड्रॉ स्कीम दिखती है, और ऊपर कुल गिनती होती है। साइड मेनू में **Scheme List** दबाकर इसे खोलें।

1. ऊपर के कार्ड देखें: **Total Schemes**, **Overall Dealer**, **Participating dealer** और **Winners**।
2. स्कीम ढूँढने के लिए **Search Schemes....** में टाइप करें। लिस्ट से प्रकार (**Both**, किसान या डीलर), स्टेटस (**All Status**) और राज्य (**All States**) चुनें।
3. हर स्कीम कार्ड पर नाम, वह किसके लिए है, स्टेटस (जैसे **Active**), तारीखें, राज्य और ज़िला, **Participating dealer** और **Winners** की गिनती और **THE GRAND REWARD** दिखता है।
4. किसी कार्ड का **Scheme Overview** खोलने के लिए उस पर **View Details** दबाएँ।
5. नई स्कीम बनाने के लिए **Create Scheme** दबाएँ।

## आम दिक्कतें

- **मुझे कोई स्कीम नहीं मिल रही।** सर्च बॉक्स खाली करें और लिस्ट को फिर से **Both**, **All Status** और **All States** पर रखें।
- **मेरे मेनू में Scheme List नहीं है।** आपके पद को स्कीम का एक्सेस नहीं है। अपने एडमिन से पूछें।
:::

::: section add-scheme
title: स्कीम बनाएँ
routes: /AddScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-add-type] | 2 > [data-help=sch-add-basic] | 3 > [data-help=sch-add-rules] | 4 > [data-help=sch-add-prize] | 5 > [data-help=sch-add-draw] | 6 > [data-help=sch-add-next]
minutes: 5
source-hash: 6e0920ccd77cef79
confidence: ok

**यह स्क्रीन किस काम की है**
नई लकी ड्रॉ स्कीम तैयार करने के लिए **Create Scheme** स्क्रीन का इस्तेमाल करें: यह किसके लिए है, कहाँ चलेगी, कौन से प्रोडक्ट गिने जाएँगे, इनाम क्या है और ड्रॉ कब होगा।

1. ऊपर चुनें कि स्कीम किसके लिए है: **Both**, **Farmer** या **Dealer**।
2. **Scheme Type & Basic Info** में **Scheme Name** और **Draw points** भरें, और **State**, **District** और **Dealer** चुनें। **Start Date** और **End Date** चुनें। **Scheme Status** बॉक्स में **Draft - Scheme is not published** दिखता है।
3. **Add Product Rule** में **Category** और **Product** चुनें, **Dealer Quantity** और **Farmer Quantity** भरें, और **+ Add Rule** दबाएँ। हर नियम **Product Rules List** में दिखता है। नियम मिटाने के लिए डस्टबिन या बदलने के लिए पेंसिल इस्तेमाल करें।
4. **Winner Prize Details** में **Prize Name** और **Prize Description** भरें, और इनाम की फ़ोटो जोड़ने के लिए **Click to upload image** पर टैप करें।
5. **Lucky Draw Configuration** में **Automatic** या **Manual** चुनें, **Frequency** चुनें (**Daily**, **Weekly**, **Monthly** या **Custom**), और **Date** और **Draw Time** तय करें।
6. बाद के लिए रखने को **Save Draft** दबाएँ, या स्कीम जाँचने के लिए **Next** दबाएँ।

> Tip: इनाम का नाम, जानकारी और फ़ोटो डीलरों को स्कीम पेज पर, विजेता के मैसेज में और ड्रॉ की घोषणा में दिखते हैं।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Scheme Name** | एक छोटा नाम जो सबको दिखेगा। |
| **Draw points** | ड्रॉ में शामिल होने के लिए सदस्य को जितने पॉइंट चाहिए। |
| **State**, **District**, **Dealer** | स्कीम कहाँ चलेगी। आप एक से ज़्यादा चुन सकते हैं। |
| **Dealer Quantity**, **Farmer Quantity** | इस नियम के लिए डीलर या किसान को प्रोडक्ट कितना खरीदना होगा। |
| **Prize Image** | एक JPG या PNG फ़ोटो, 5 MB तक। |
| **Draw Time** | दिन का वह समय जब ड्रॉ होता है। |

## आम दिक्कतें

- **फ़ोटो अपलोड नहीं हो रही।** 5 MB से छोटी JPG या PNG इस्तेमाल करें।
- **मुझे एक स्टेप पीछे जाना है।** **Previous Step** दबाएँ।
:::

::: section review-scheme
title: स्कीम जाँचें और शुरू करें
routes: /ReviewScheme
for: 
roles: 
designations: 
minutes: 3
source-hash: 5c87d7eaecfc2110
confidence: ok

**यह स्क्रीन किस काम की है**
बनाने वाले फ़ॉर्म पर **Next** दबाने के बाद **Review Scheme** स्क्रीन पर नई स्कीम की हर जानकारी एक जगह दिखती है, ताकि शुरू होने से पहले आप उसे जाँच सकें।

1. **Review Your Scheme** बैनर पढ़ें।
2. **Scheme Details** में **APPLICABLE FOR:**, स्टेटस (**ACTIVE DRAFT**), स्कीम का नाम, बनाने की तारीख और जानकारी जाँचें।
3. **Campine Time Line** में **Start Date** और **End Date** जाँचें, और साथ वाला इनाम का कार्ड देखें।
4. **Product Rules** टेबल जाँचें। नियम बदलने के लिए पेंसिल या मिटाने के लिए डस्टबिन इस्तेमाल करें।
5. **Target Segment** (**State**, **District**, **Dealers**) और **Review Checklist** (**Draw Points**, **Product Points**, **Draw Mode**, **Frequency**, **Draw Date** और **Draw Time**) जाँचें।
6. पब्लिश किए बिना रखने के लिए **Save Draft** दबाएँ, या स्कीम शुरू करने के लिए **Active Scheme** दबाएँ।

> Careful: **Active Scheme** दबाने के बाद डीलर और किसान स्कीम देख सकते हैं और एंट्री जोड़ना शुरू कर देते हैं।

## आम दिक्कतें

- **कोई जानकारी गलत है।** **Active Scheme** दबाने से पहले बनाने वाले फ़ॉर्म पर वापस जाएँ और उसे ठीक करें।
- **मैं अभी शुरू करने के लिए तैयार नहीं हूँ।** **Save Draft** दबाएँ। स्कीम ड्राफ़्ट ही रहेगी।
:::

::: section scheme-overview
title: स्कीम पर नज़र रखें
routes: /SchemeOverview
for: 
roles: 
designations: 
minutes: 3
source-hash: 55ace08ba0ddb992
confidence: ok

**यह स्क्रीन किस काम की है**
**Scheme Overview** स्क्रीन पर दिखता है कि एक स्कीम कैसी चल रही है: कौन हिस्सा ले रहा है, कितनी एंट्री हैं, कौन जीता और पिछले सभी ड्रॉ। स्कीम लिस्ट पर **View Details** दबाकर इसे खोलें।

1. ऊपर स्कीम का नाम, वह किसके लिए है, स्टेटस (जैसे **Active**), **States** और **Duration** देखें।
2. कार्ड देखें: **Participating dealer**, **Total Entries** और **Winners**।
3. **Overview** टैब पर बड़ा इनाम और **Draw Setting** देखें: **Draw Mode**, **Frequency**, **Next Draw Date** और **Last Draw Date**।
4. **Participating dealer** टैब पर हर सदस्य को उसके **Points** और **Entries** के साथ देखने के लिए खोजें या राज्य चुनें। ज़्यादा जानकारी के लिए कार्ड पर टैप करें।
5. **Winners** टैब पर हर विजेता को स्टेटस (**Selected**, **Notified** या **Claimed**), **Prize** और **Draw Date** के साथ देखें। ज़्यादा जानकारी के लिए कार्ड पर टैप करें।
6. **Draw History** टैब पर हर पिछला ड्रॉ उसकी तारीख, **Completed**, इनाम और कितने विजेता चुने गए, इसके साथ देखें।

## आम दिक्कतें

- **Winners टैब खाली है।** इस स्कीम का अभी तक कोई ड्रॉ नहीं हुआ है। **Overview** टैब पर **Next Draw Date** देखें।
- **कोई सदस्य नहीं दिख रहा।** **All States** चुनें और सर्च बॉक्स खाली करें।
:::

::: section scan-qr
title: प्रोडक्ट का QR कोड स्कैन करें
routes: /qr-scanner
for: 
roles: 
designations: 
minutes: 1
source-hash: 4ad0b8818ad680ef
confidence: ok

**यह स्क्रीन किस काम की है**
आपने जो SPIC प्रोडक्ट खरीदा है, उस पर लगा QR कोड स्कैन करने के लिए **QR Scanner** का इस्तेमाल करें। इसी से आपकी खरीद लकी ड्रॉ के लिए गिनी जाती है।

1. फ़ोन को प्रोडक्ट के QR कोड के ऊपर रखें। कोड को चौकोर फ़्रेम के अंदर रखें।
2. अँधेरा हो तो फ़्लैश के लिए बिजली वाले आइकन पर टैप करें। कैमरा बदलने के लिए कैमरा आइकन पर टैप करें।
3. अगर कोड छोटा है या दूर है, तो ज़ूम बार को **+** की तरफ़ खिसकाएँ।
4. अपने पास पहले से रखी फ़ोटो से कोड स्कैन करने के लिए फ़ोटो वाले आइकन पर टैप करें।
5. अपनी लकी ड्रॉ एंट्री देखने के लिए **View Entries** दबाएँ।

## आम दिक्कतें

- **कोड पढ़ा नहीं जा रहा।** कैमरा साफ़ करें, फ़्लैश चालू करें और फ़ोन को लगभग एक हाथ की दूरी पर स्थिर रखें।
- **कैमरा नहीं खुल रहा।** फ़ोन की सेटिंग में ऐप को कैमरा इस्तेमाल करने की इजाज़त दें।
:::

::: section scan-product
title: स्कैन किए गए प्रोडक्ट भरें
routes: /Scanproduct
for: 
roles: 
designations: 
minutes: 2
source-hash: c9f9991efd4bd6c1
confidence: ok

**यह स्क्रीन किस काम की है**
स्कैन के बाद इस स्क्रीन पर कंपनी और उसके प्रोडक्ट दिखते हैं। हर प्रोडक्ट आपने कितना खरीदा, वह भरें और सबमिट करें।

1. ऊपर कंपनी का कार्ड जाँचें: नाम, **VERIFIED**, पता, फ़ोन और ईमेल।
2. **Products** में अपना खरीदा हुआ हर प्रोडक्ट ढूँढें।
3. एक बढ़ाने के लिए **+** दबाएँ। एक घटाने के लिए **−** दबाएँ। बीच वाला नंबर मात्रा है।
4. **Submit** दबाएँ।
5. फिर से स्कैन करने के लिए **Back To Scan** के पास वाले पीछे के तीर पर टैप करें।

## आम दिक्कतें

- **मात्रा ज़ीरो से नीचे नहीं जाती।** यह सामान्य है। ज़ीरो का मतलब है कि आपने वह प्रोडक्ट नहीं खरीदा।
- **गलत कंपनी दिख रही है।** **Back To Scan** पर वापस जाएँ और सही QR कोड स्कैन करें।
:::

::: section select-products
title: डीलर से खरीदे गए प्रोडक्ट चुनें
routes: /SelectPurchasedProducts
for: 
roles: 
designations: 
minutes: 2
source-hash: 79749dd21c644750
confidence: ok

**यह स्क्रीन किस काम की है**
**Select Purchased Products** स्क्रीन पर वह डीलर जिससे आपने खरीदा, आपकी लकी ड्रॉ की प्रगति और डीलर के बेचे जाने वाले प्रोडक्ट दिखते हैं। आपने जो प्रोडक्ट खरीदे, उन्हें चुनें।

1. डीलर का कार्ड जाँचें: नाम, **VERIFIED**, पता, फ़ोन और ईमेल।
2. **Current Progress** देखें: आपकी **Total Lucky Draw Entries** और ग्रैंड ड्रॉ के लिए अभी कितनी और चाहिए।
3. **Available Products** में अपना खरीदा हुआ हर प्रोडक्ट ढूँढें।
4. आपने कितना खरीदा, यह तय करने के लिए **Qty** के पास **+** और **−** इस्तेमाल करें।

## आम दिक्कतें

- **मुझे अपना प्रोडक्ट नहीं दिख रहा।** हो सकता है डीलर इसे इस स्कीम में न बेचता हो। डीलर से पूछें।
- **यह गलत डीलर है।** वापस जाएँ और सही डीलर का QR कोड स्कैन करें।
:::

::: section lucky-draw
title: मेरा लकी ड्रॉ
routes: /Luckydraw
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-ld-entry] | 2 > [data-help=sch-ld-prize] | 3 > [data-help=sch-ld-progress] | 4 > [data-help=sch-ld-how] | 5 > [data-help=sch-ld-actions]
minutes: 2
source-hash: f6f7b0a7b08535be
confidence: ok

**यह स्क्रीन किस काम की है**
आपकी खरीद की जाँच होने के बाद **Lucky Draw** पेज पर **You're in the Lucky Draw!**, आप जो इनाम जीत सकते हैं और ग्रैंड ड्रॉ से आप कितनी दूर हैं, यह दिखता है।

1. नई एंट्री देखें: **+1 Lucky Draw Entry**, **Ticket Activated**, **Entry ID** और तारीख।
2. **Grand Prize** और उसकी अनुमानित कीमत, और **Previous Winners** देखें। और विजेता देखने के लिए **View All** दबाएँ।
3. **CURRENT CYCLE PROGRESS** में 100 में से आपकी एंट्री, **Next Milestone** और **Grand Draw** देखें।
4. **How It Works** पढ़ें: हर खरीद पर 1 एंट्री मिलती है, और 100 एंट्री होने पर ग्रैंड ड्रॉ खुलता है।
5. और एंट्री जोड़ने के लिए **Scan Another Product** दबाएँ, या लिस्ट देखने के लिए **View All Entries** दबाएँ। फ़ोन पर ये बटन **Scan QR** और **View Entries** होते हैं।

## आम दिक्कतें

- **मेरी एंट्री की गिनती नहीं बढ़ी।** खरीद की जाँच होने के बाद ही एंट्री जुड़ती है। थोड़ी देर बाद **View All Entries** में फिर से देखें।
- **GOLD MILESTONE क्या है?** यह 100 एंट्री का पड़ाव है, जिसके पूरा होने पर **Grand Draw** खुलता है।
:::

::: section my-entries
title: मेरी सभी एंट्री देखें
routes: /LuckyDrawList
for: 
roles: 
designations: 
minutes: 2
source-hash: a976b5d2def67294
confidence: ok

**यह स्क्रीन किस काम की है**
**Over All Entries** स्क्रीन पर इस राउंड की आपकी हर लकी ड्रॉ एंट्री दिखती है, हर एक के प्रोडक्ट और पॉइंट के साथ।

1. **TOTAL ENTRIES** कार्ड पर दिखता है कि 100 में से आपकी कितनी चालू एंट्री हैं, साथ में 25, 50, 75 और **GOLD MILESTONE** के निशान।
2. **Your Entries List** में एक टैब चुनें: **All**, **Active**, **Used** या **Expired**।
3. हर कार्ड पर एंट्री नंबर, उसका स्टेटस, तारीख, प्रोडक्ट, **No.of Products**, **Total Products** और **Earned Points** दिखते हैं।
4. वह एंट्री देखने के लिए कार्ड पर **View Details** दबाएँ।
5. लकी ड्रॉ पेज पर वापस जाने के लिए पीछे के तीर पर टैप करें।

## आम दिक्कतें

- **लिस्ट खाली है।** इस टैब में आपकी कोई एंट्री नहीं है। **All** चुनें।
- **कुछ एंट्री पर Expired दिख रहा है।** वे अब नहीं जीत सकतीं। एंट्री जोड़ने के लिए नई खरीद स्कैन करते रहें।
:::

::: section winner-announcement
title: विजेता का मैसेज
routes: /WinnerPopUp
for: 
roles: 
designations: 
minutes: 1
source-hash: a8394f0ccc4ee5a5
confidence: ok

**यह स्क्रीन किस काम की है**
जब आप जीतते हैं, तो **Congratulations!** मैसेज खुलता है: **You are a Lucky Draw Winner**। इसमें आपका इनाम और स्कीम दिखती है।

1. इनाम का नाम और स्कीम का नाम पढ़ें।
2. **Winner Details** खोलने के लिए **View Details** दबाएँ।
3. लकी ड्रॉ पेज पर वापस जाने के लिए मैसेज बंद करें।

## आम दिक्कतें

- **मैंने गलती से मैसेज बंद कर दिया।** साइड मेनू से **Winner Details** खोलें।
:::

::: section winner-details
title: मेरे इनाम की जानकारी
routes: /WinnerDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: e8beec48b189b974
confidence: ok

**यह स्क्रीन किस काम की है**
**Winner Details** स्क्रीन पर आपका लकी ड्रॉ का इनाम और वह आप तक कैसे पहुँचेगा, यह दिखता है।

1. ऊपर **You are a Winner** और **Draw ID** देखें।
2. **Draw Summary** में उस ड्रॉ के **Participants**, **Winners** और **Entries** दिखते हैं।
3. **Winner Information** में **Winner Name**, **Type**, **Dealer Code**, **Mobile Number** और **Points Earned** दिखते हैं।
4. **Prize Information** में **Prize Name**, **Prize Value**, **Status** (जैसे **Prize Distributed**) और **Distributed Date** दिखते हैं।
5. **Scheme Information** और **Activity Information** में स्कीम, **Draw Date**, घोषणा किसने की और कब, यह दिखता है।
6. अगर आपको **Better Luck Next Time!** दिखे, तो एंट्री जोड़ने के लिए **Scan QR** दबाएँ या उन्हें देखने के लिए **View Entries** दबाएँ।
7. अपने इनाम के बारे में कोई भी सवाल हो तो **Need Help?** बॉक्स में **Contact Support** दबाएँ।

## आम दिक्कतें

- **मोबाइल नंबर गलत है।** **Contact Support** दबाएँ, ताकि टीम आपसे संपर्क कर सके।
- **इनाम अभी तक नहीं मिला।** **Prize Information** में **Status** देखें, या **Contact Support** दबाएँ।
:::

::: section scheme-winners
title: स्कीम के सभी विजेता देखें
routes: /CelebrationWinners
for: 
roles: 
designations: 
minutes: 1
source-hash: 633e72a075f230fb
confidence: ok

**यह स्क्रीन किस काम की है**
**Scheme Winners** स्क्रीन पर पिछली घोषणाओं के लकी ड्रॉ विजेता दिखते हैं।

1. **Celebrating Our Winners!** बैनर पर **Total Winners Announced** दिखता है।
2. लिस्ट से स्कीम (**All Schemes**), साल, प्रकार (**All Types**) और जगह (**All Locations**) चुनें।
3. एक टैब चुनें: **All Winners**, **Dealer Winners** या **Farmer Winners**।
4. हर कार्ड पर विजेता, **Dealer** या **Farmer**, स्कीम, जगह, **Prize** और **Draw Date** दिखता है।
5. किसी विजेता के बारे में और जानने के लिए **View Details** दबाएँ।

## आम दिक्कतें

- **मुझे कोई विजेता नहीं दिख रहा।** **All Schemes**, **All Types** और **All Locations** चुनें, और साल जाँचें।
:::
