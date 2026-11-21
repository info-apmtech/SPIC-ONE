---
module: digital-library
title: डिजिटल लाइब्रेरी
icon: bi-book
order: 114
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
source-hash: 3269461e350cadf4
confidence: ok

डिजिटल लाइब्रेरी में SPIC के प्रोडक्ट की जानकारी, AI वीडियो और प्रोडक्ट ब्रोशर एक ही जगह रहते हैं। कोई भी इसमें खोज सकता है, पढ़ सकता है और इसके बारे में SPIC AI से सवाल पूछ सकता है। एक छोटी टीम इसमें जानकारी डालती है और उसे पब्लिश करती है।

- find | icon: bi-search | color: #0e7490
  title: जो चाहिए वह ढूँढें
  text: AI वीडियो, प्रोडक्ट की जानकारी और ब्रोशर देखें, या नाम से खोजें।
- ask | icon: bi-stars | color: #7c3aed
  title: SPIC AI से पूछें
  text: सवाल टाइप करें और लाइब्रेरी की जानकारी से जवाब पाएँ।
- manage | icon: bi-pencil-square | color: #d97706
  title: लाइब्रेरी टीम जानकारी डालती है
  text: वे नए आइटम डालते हैं, उन्हें पब्लिश करते हैं और पुराने छिपा देते हैं।
:::

::: section flow
title: जानकारी आप तक कैसे पहुँचती है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 7a29b652c1da6588
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- team | type: lane | color: #d97706 | roles: Admin
  title: लाइब्रेरी टीम
  sub: लाइब्रेरी संभालने वाले कर्मचारी
- readers | type: lane | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: सभी
  sub: लॉगिन किए हुए सभी यूज़र
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: publish | screen: add-content
  title: जानकारी डालें
  status: Draft
  text: जानकारी डालने वाले फ़ॉर्म के तीनों स्टेप भरें। **Save as Draft** करने पर यह पढ़ने वालों को नहीं दिखती।
- publish | lane: team | col: 2 | icon: bi-broadcast | color: #16a34a | next: read | return: create | screen: manage-content
  title: पब्लिश करें
  status: Published
  text: **Save & Publish** या **Publish** दबाएँ। **Unpublish** करने पर यह वापस Drafts में चली जाती है।
- read | lane: readers | col: 3 | icon: bi-book | color: #0e7490 | next: ask | screen: library-home
  title: ढूँढें और पढ़ें
  status: Published
  text: पब्लिश किए गए वीडियो, प्रोडक्ट पेज और ब्रोशर सभी खोल सकते हैं।
- ask | lane: readers | col: 4 | icon: bi-stars | color: #7c3aed | screen: ask-ai
  title: SPIC AI से पूछें
  text: चैट में सवाल पूछें। जवाब में उन लाइब्रेरी आइटम की लिस्ट होती है जिनसे जवाब बना।
- archive | lane: team | col: 3 | icon: bi-archive | color: #475569 | kind: exception | from: publish | to: create | screen: manage-content
  title: पुरानी जानकारी छिपाएँ
  status: Archived
  text: पुरानी या गलत जानकारी archive कर दी जाती है और फिर पढ़ने वालों को नहीं दिखती। **Restore** करने पर यह फिर से ड्राफ़्ट बनकर वापस आती है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c0a33e46c0f720af
confidence: ok

- published | color: #16a34a
  label: Published
  text: सभी इसे देख और खोल सकते हैं।
- draft | color: #d97706
  label: Draft
  text: सेव है पर पढ़ने वालों से छिपा है। सिर्फ़ लाइब्रेरी टीम को दिखता है।
- archived | color: #475569
  label: Archived
  text: पढ़ने वालों से छिपा है। लाइब्रेरी टीम इसे कभी भी वापस ला सकती है।
- active | color: #0e7490
  label: Active
  text: ऐसी कैटेगरी जो जानकारी डालने वाले फ़ॉर्म पर चुनी जा सकती है।
- inactive | color: #64748b
  label: Inactive
  text: ऐसी कैटेगरी जो अब जानकारी डालने वाले फ़ॉर्म पर नहीं दिखती।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 7b1e4cbe0e13d1bb
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- readers | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: सभी
  - लाइब्रेरी देखना और उसमें खोजना
  - वीडियो देखना और ब्रोशर खोलना
  - SPIC AI से सवाल पूछना
- team | color: #d97706 | roles: Admin
  title: लाइब्रेरी टीम
  - प्रोडक्ट, AI वीडियो और ब्रोशर डालना
  - जानकारी को publish, unpublish, archive और डिलीट करना
  - कैटेगरी की लिस्ट अपडेट रखना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 93226ef665c87ba7
confidence: ok

- open-brochure | popular: true
  q: प्रोडक्ट ब्रोशर कैसे पढ़ूँ?
  a: **Product Brochures** खोलें, ब्रोशर पर टैप करें, फिर कवर की तस्वीर के नीचे **Open brochure** दबाएँ। PDF खुल जाती है या आपके फ़ोन में सेव हो जाती है।
- ai-answer | popular: true
  q: क्या SPIC AI के हर जवाब पर भरोसा कर सकते हैं?
  a: SPIC AI लाइब्रेरी की जानकारी से जवाब देता है, पर उससे गलती हो सकती है। किसी किसान या डीलर को सलाह देने से पहले जवाब के नीचे दिए गए आइटम खोलें और बातें जाँच लें।
- no-add
  q: मुझे Add Content बटन नहीं दिख रहा
  a: सिर्फ़ लाइब्रेरी टीम ही जानकारी डाल सकती है। आपके पद (designation) को Digital Library में जानकारी डालने की अनुमति चाहिए। ज़रूरत हो तो अपने एडमिनिस्ट्रेटर से पूछें।
- only-published
  q: मुझे "Only published Digital Library content is available to you" दिख रहा है
  a: आप सिर्फ़ पब्लिश की गई जानकारी देख सकते हैं। स्टेटस लिस्ट में **All Status** या **Published** चुनें, या पहले तीन कार्ड में से किसी एक पर टैप करें।
- category-in-use
  q: मैं कैटेगरी डिलीट नहीं कर पा रहा
  a: लाइब्रेरी के कुछ आइटम अभी भी इसे इस्तेमाल कर रहे हैं। इसकी जगह टॉगल से इसे बंद कर दें। फिर यह जानकारी डालने वाले फ़ॉर्म पर नहीं दिखेगी।
:::

::: section library-home
title: लाइब्रेरी में जानकारी ढूँढें
routes: /DigitalLibrary
for: 
roles: 
designations: 
tour: 1 > [data-help=dl-actions] | 2 > [data-help=dl-search] | 3 > [data-help=dl-categories] | 4 > [data-help=dl-featured]
minutes: 2
source-hash: 96a094bb4b59554c
confidence: ok

**यह स्क्रीन किस काम की है**
**Digital Library Assistant** स्क्रीन लाइब्रेरी का पहला पेज है। यहाँ से आप SPIC AI से पूछ सकते हैं, किसी तरह की जानकारी खोल सकते हैं, या कोई featured आइटम खोल सकते हैं।

1. SPIC AI से सवाल पूछने के लिए **Start Chat** दबाएँ। अगर आप लाइब्रेरी टीम में हैं, तो यहाँ आपको **Add Content** भी दिखता है।
2. सर्च बॉक्स में प्रोडक्ट का नाम या सवाल टाइप करें और तीर दबाएँ। आप **Find Product Information** या **Show latest brochures** जैसे सुझाव पर भी टैप कर सकते हैं। इससे आपके सवाल के साथ SPIC AI चैट खुलती है।
3. **Digital Library** के नीचे **AI Videos**, **Product Information** या **Product Brochures** पर टैप करें। हर कार्ड पर दिखता है कि उसमें कितने आइटम हैं।
4. **Featured Content** के नीचे किसी आइटम पर **Open** (या **Explore Videos**) टैप करें। पूरी लिस्ट देखने के लिए **View All Content** दबाएँ।

> Tip: यहाँ का सर्च बॉक्स SPIC AI से पूछता है। किसी आइटम को उसके नाम से ढूँढना हो, तो **View All Content** दबाएँ और वहाँ का सर्च बॉक्स इस्तेमाल करें।

## आम दिक्कतें

- **"Nothing in the library yet".** (लाइब्रेरी में अभी कुछ नहीं है) अभी कोई जानकारी पब्लिश नहीं हुई है। बाद में फिर देखें।
- **"The library is not available".** (लाइब्रेरी अभी उपलब्ध नहीं है) कनेक्शन नहीं हो पाया। **Try again** दबाएँ।
- **मेरे सर्च से चैट खुल गई।** यह सामान्य है। इस पेज का सर्च बॉक्स SPIC AI से पूछता है।
:::

::: section all-content
title: सारी जानकारी देखें और खोजें
routes: /DigitalLibrary/content
for: 
roles: 
designations: 
minutes: 2
source-hash: e3b9101791400b7f
confidence: ok

**यह स्क्रीन किस काम की है**
**All Content** स्क्रीन पर लाइब्रेरी के सारे आइटम कार्ड के रूप में दिखते हैं। टाइटल के नीचे की लाइन बताती है कि कितने आइटम मेल खाते हैं।

1. सिर्फ़ एक तरह के आइटम देखने के लिए ऊपर **Product Information**, **AI Videos** या **Product Brochures** जैसे किसी कार्ड पर टैप करें। उस पर लिखा नंबर बताता है कि उस तरह के कितने आइटम हैं।
2. आप **All Types**, **AI Videos**, **Products** और **Brochures** टैब भी इस्तेमाल कर सकते हैं।
3. **Search content...** बॉक्स में टाइप करें। यह टाइटल, विवरण, कीवर्ड, टैग और कैटेगरी में शब्द ढूँढता है।
4. आइटम खोलने के लिए कार्ड की तस्वीर या टाइटल पर टैप करें।
5. और आइटम देखने के लिए नीचे **Load More** दबाएँ।

> Tip: ऊपर वाला पीछे का तीर आपको लाइब्रेरी के पहले पेज पर वापस ले जाता है।

## आम दिक्कतें

- **"No content found".** (कोई जानकारी नहीं मिली) आपका चुना हुआ टाइप या सर्च बहुत सीमित है। **Clear filters** दबाएँ।
- **"Only published Digital Library content is available to you".** (आप सिर्फ़ पब्लिश की गई जानकारी देख सकते हैं) स्टेटस लिस्ट में **All Status** या **Published** चुनें।
- **"The library is not available".** (लाइब्रेरी अभी उपलब्ध नहीं है) कनेक्शन नहीं हो पाया। **Try again** दबाएँ।
:::

::: section view-item
title: प्रोडक्ट, वीडियो या ब्रोशर पढ़ें
routes: /DigitalLibrary/view/{Id:int}
for: 
roles: 
designations: 
minutes: 2
source-hash: f49b1d4b29efe757
confidence: ok

**यह स्क्रीन किस काम की है**
यह स्क्रीन लाइब्रेरी का एक आइटम दिखाती है: प्रोडक्ट पेज, AI वीडियो या ब्रोशर। कौन से हिस्से दिखें और किस क्रम में, यह लाइब्रेरी टीम तय करती है, इसलिए हो सकता है कुछ हिस्से न दिखें।

1. लाइब्रेरी के पहले पेज से, **All Content** से या किसी जुड़े हुए आइटम से कोई आइटम खोलें।
2. ऊपर टाइप (**Product**, **AI Video** या **Brochure**), टाइटल, छोटा विवरण और टैग देखें। लिस्ट पर वापस जाने के लिए **Back** दबाएँ।
3. overview वाले हिस्से में वीडियो चलाएँ, प्रोडक्ट की तस्वीर देखें, या PDF खोलने के लिए **Open brochure** दबाएँ।
4. इस आइटम के बारे में SPIC AI से पूछने के लिए **SPIC AI Chatbox** में **Start Conversation** दबाएँ।
5. **Details** बॉक्स में **Category**, **Published** तारीख, **Author**, **Status**, **Views** और टैग देखें।
6. जुड़े हुए वीडियो और ब्रोशर, मुख्य खूबियाँ और इस्तेमाल का तरीका देखने के लिए नीचे स्क्रॉल करें। कोई जुड़ा हुआ आइटम खोलने के लिए उस पर टैप करें।

## आम दिक्कतें

- **"Content not found".** (जानकारी नहीं मिली) वह आइटम डिलीट हो गया है या अभी पब्लिश नहीं है। **Back to All Content** दबाएँ।
- **"No video has been uploaded or linked yet."** (अभी कोई वीडियो अपलोड या लिंक नहीं किया गया) लाइब्रेरी टीम ने अभी वीडियो नहीं डाला है।
- **"No brochure file has been uploaded yet."** (अभी ब्रोशर की फ़ाइल अपलोड नहीं हुई) लाइब्रेरी टीम ने अभी PDF नहीं डाली है।
- **वीडियो ऐप के बाहर खुलता है।** कुछ वीडियो लिंक सिर्फ़ अपनी ही वेबसाइट पर खुलते हैं। **Open the video** दबाएँ।
:::

::: section ask-ai
title: SPIC AI से सवाल पूछें
routes: /DigitalLibrary/chat, /DigitalLibrary/chat/{ConversationId:int}
for: 
roles: 
designations: 
tour: 1 > [data-help=dl-chat-new] | 2 > [data-help=dl-chat-body] | 3 > [data-help=dl-chat-input] | 4 > [data-help=dl-chat-tools] | 5 > [data-help=dl-chat-list] | 6 > [data-help=dl-chat-share]
minutes: 2
source-hash: 93ef9dd3f309cfcd
confidence: ok

**यह स्क्रीन किस काम की है**
**Digital Library Assistant** चैट स्क्रीन पर आप SPIC AI से SPIC के प्रोडक्ट, वीडियो और ब्रोशर के बारे में पूछते हैं। जवाब लाइब्रेरी की जानकारी से आते हैं और उनमें इस्तेमाल हुए आइटम की लिस्ट होती है।

1. नई बातचीत शुरू करने के लिए **New Chat** दबाएँ।
2. **Tell me about a SPIC product** या **Recommend videos** जैसे किसी सुझाव वाले कार्ड पर टैप करें। जवाब यहाँ दिखते हैं। जवाब के नीचे दिया गया लाइब्रेरी आइटम खोलने के लिए उस पर टैप करें, या अगले सवाल पर टैप करें।
3. या **Ask anything about SPIC products, videos, brochures** बॉक्स में अपना सवाल टाइप करें।
4. सिर्फ़ **AI Videos**, **Product Information** या **Product Brochures** में खोजने के लिए **Select Source** चुनें। लाइब्रेरी के किसी एक आइटम के बारे में पूछने के लिए **Attach** दबाएँ, या बोलकर सवाल पूछने के लिए **Voice** दबाएँ। फिर **Send** दबाएँ।
5. आपकी पुरानी चैट बाईं ओर **Today**, **Yesterday** और **Earlier** के नीचे दिखती हैं। खोलने के लिए किसी एक पर टैप करें। डस्टबिन उसे डिलीट करता है।
6. बातचीत किसी को भेजने के लिए या कॉपी करने के लिए **Share** दबाएँ।

> Tip: **Attach** करने के बाद बॉक्स में आइटम के नाम के साथ **Asking about:** दिखता है। जब तक आप क्रॉस नहीं दबाते, हर सवाल का जवाब उसी आइटम से दिया जाता है।
> Careful: SPIC AI गलत जानकारी दिखा सकता है। उस पर कुछ करने से पहले जवाब जाँच लें।

## आम दिक्कतें

- **"SPIC AI could not answer just now."** (SPIC AI अभी जवाब नहीं दे पाया) थोड़ी देर रुकें और सवाल फिर से भेजें।
- **"Voice input is not available on this device."** (इस डिवाइस पर बोलकर सवाल नहीं पूछ सकते) इसकी जगह अपना सवाल टाइप करें।
- **"Start a conversation to share it."** (शेयर करने के लिए पहले बातचीत शुरू करें) पहले कोई सवाल पूछें, फिर **Share** दबाएँ।
- **जवाब के नीचे "Answers from library content" लिखा है।** यह जवाब पूरे AI के बिना, लाइब्रेरी आइटम के शब्दों को मिलाकर बनाया गया है।
:::

::: section add-content
title: लाइब्रेरी में जानकारी डालें या बदलें
routes: /DigitalLibrary/add/{KindRoute}
for: 
roles: 
designations: 
minutes: 5
source-hash: e5986f103fd5415d
confidence: ok

**यह स्क्रीन किस काम की है**
लाइब्रेरी टीम इस फ़ॉर्म से प्रोडक्ट पेज, AI वीडियो या ब्रोशर डालती है, या उसमें बदलाव करती है। फ़ॉर्म में तीन स्टेप हैं: **Content & Details**, **Related Content** और **Page Layout**।

1. लाइब्रेरी के पहले पेज पर या **All Content** पर **Add Content** दबाएँ। **Create New Content** में **AI Videos**, **Product Information** या **Product Brochures** चुनें। किसी आइटम को बदलना हो, तो इसकी जगह उसका पेंसिल आइकन या **Edit Content** दबाएँ।
2. **Basic Information** में कैटेगरी चुनें और टाइटल, टैग और **Short Description** भरें। प्रोडक्ट के लिए **Associated Program / Sub-Category**, **Resource Content Format** और **Visibility / Enable To** भी चुनें, फिर **Product Title** और **Keywords (Comma Separated)** भरें।
3. फ़ाइलें डालें: प्रोडक्ट की तस्वीर, वीडियो फ़ाइल या thumbnail के साथ **Video URL**, या ब्रोशर का कवर और **Brochure PDF**।
4. प्रोडक्ट या वीडियो के लिए overview, **Key Features / Benefits** और **Usage / Application** भरें। अपने टाइटल के साथ और लिखने के लिए **Add Section** दबाएँ।
5. **Next: Related Content** दबाएँ। आइटम पेज पर दिखाने के लिए पब्लिश किए गए वीडियो और ब्रोशर पर टिक करें। उनका क्रम बदलने के लिए तीर इस्तेमाल करें, और कितने दिखें यह तय करने के लिए **Max Item** इस्तेमाल करें।
6. **Next: Page Layout** दबाएँ। हिस्सों का क्रम तय करने के लिए उन्हें खींचें (drag), या तीर इस्तेमाल करें। किसी हिस्से को छिपाने के लिए स्विच और उसका नाम बदलने के लिए पेंसिल इस्तेमाल करें।
7. **Save & Publish** दबाएँ (या बदलाव करते समय **Save Changes**)। **Save as Draft** करने पर आइटम पढ़ने वालों को दिखाए बिना सेव हो जाता है।

> Tip: अगर कोई कैटेगरी नहीं है, तो लिस्ट में **+ Add new category…** चुनें। **Manage categories** से कैटेगरी की पूरी लिस्ट खुलती है।
> Careful: आइटम सेव होने के बाद फ़ाइलें अपलोड होती हैं। अपलोड के मैसेज खत्म होने तक इसी पेज पर रहें।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Product Title**, **Video Title** या **Brochure Title** | वह नाम जो पढ़ने वालों को दिखेगा। अगले स्टेप पर जाने से पहले ज़रूरी है। |
| **Tags** | सर्च में मदद करने वाले छोटे शब्द। एक टाइप करें और Enter दबाएँ, या **Used before:** के नीचे किसी टैग पर टैप करें। |
| **Product Image** या **Video Thumbnail** | PNG या JPG, 5 MB तक। सबसे अच्छा साइज़ 1280 x 720 px। |
| **Video File (Upload)** | MP4, WebM या MOV, 700 MB तक। या YouTube, Vimeo या सीधा **Video URL** दें। |
| **Duration** | ज़रूरी नहीं, hh:mm:ss के रूप में। आइटम पेज पर दिखता है। |
| **Brochure PDF** | एक PDF फ़ाइल, 25 MB तक। |

## आम दिक्कतें

- **"Enter a product title before continuing."** (आगे बढ़ने से पहले प्रोडक्ट का टाइटल भरें) पहले स्टेप 1 में टाइटल भरें।
- **"Image must be 5 MB or smaller."** (तस्वीर 5 MB या उससे छोटी होनी चाहिए) छोटी तस्वीर इस्तेमाल करें। वीडियो के लिए यही सीमा 700 MB और ब्रोशर के लिए 25 MB है।
- **"could not be uploaded. The item itself was saved."** (फ़ाइल अपलोड नहीं हुई, पर आइटम सेव हो गया) पेंसिल आइकन से आइटम फिर से खोलें और फ़ाइल दोबारा चुनें।
- **स्टेप 2 में "No published videos found" दिखता है।** सिर्फ़ पब्लिश किए गए आइटम ही जोड़े जा सकते हैं। पहले उन्हें पब्लिश करें।
- **"You do not have permission to add content".** (आपको जानकारी डालने की अनुमति नहीं है) सिर्फ़ लाइब्रेरी टीम ही जानकारी डाल या बदल सकती है।
:::

::: section manage-content
title: जानकारी पब्लिश करें, छिपाएँ या डिलीट करें
routes: /DigitalLibrary/content, /DigitalLibrary/view/{Id:int}
for: DigitalLibrary.Update
roles: 
designations: 
minutes: 3
source-hash: 483778c83ea27554
confidence: ok

**यह स्क्रीन किस काम की है**
पढ़ने वालों को क्या दिखेगा, यह लाइब्रेरी टीम तय करती है। हर कार्ड पर एक टैग होता है: **Published**, **Draft** या **Archived**। नीचे बताए गए बटन सिर्फ़ उन कर्मचारियों को दिखते हैं जिन्हें लाइब्रेरी की जानकारी बदलने की अनुमति है।

1. छिपे हुए आइटम देखने के लिए **All Content** पर **Draft Content** या **Archived** पर टैप करें, या **All Status** लिस्ट में **Published**, **Draft** या **Archived** चुनें।
2. ड्राफ़्ट कार्ड को सबको दिखाने के लिए उस पर **Publish** दबाएँ।
3. पब्लिश किए गए आइटम को छिपाने के लिए **Unpublish** दबाएँ। **Unpublish this content?** में **Unpublish** दबाएँ। यह Drafts में चला जाता है।
4. पुराने आइटम को archive करने के लिए archive आइकन दबाएँ। archive किए गए आइटम को फिर से ड्राफ़्ट के रूप में लाने के लिए **Restore** दबाएँ।
5. आइटम बदलने के लिए पेंसिल आइकन दबाएँ, या डिलीट करने के लिए डस्टबिन आइकन दबाएँ, फिर पक्का करने के लिए **Delete** दबाएँ।
6. आइटम पेज पर **Details** बॉक्स में **Edit Details** और एक पब्लिश बटन भी होता है, जैसे **Publish Product** या **Unpublish Video**।

> Careful: डिलीट की गई जानकारी वापस नहीं लाई जा सकती। अगर बाद में ज़रूरत पड़ सकती है, तो इसकी जगह archive करें।

## आम दिक्कतें

- **मुझे डस्टबिन आइकन नहीं दिख रहा।** डिलीट करने के लिए अलग अनुमति चाहिए। अपने एडमिनिस्ट्रेटर से पूछें।
- **कोई आइटम लिस्ट से गायब हो गया।** हो सकता है वह अब archive हो गया हो या ड्राफ़्ट में हो। स्टेटस लिस्ट में **All Status** चुनें।
:::

::: section categories
title: लाइब्रेरी की कैटेगरी संभालें
routes: /DigitalLibrary/categories
for: 
roles: 
designations: 
minutes: 3
source-hash: f17d81a735096b2f
confidence: ok

**यह स्क्रीन किस काम की है**
**Library Categories** स्क्रीन पर उन कैटेगरी और सब-कैटेगरी की लिस्ट रहती है जो जानकारी डालने वाले फ़ॉर्म पर दिखती हैं, जैसे एक कैटेगरी और उसके नीचे उसकी सब-कैटेगरी।

1. कैटेगरी ढूँढने के लिए सर्च बॉक्स में टाइप करें, या **All**, **Active** या **Inactive** चुनें।
2. किसी कैटेगरी की सब-कैटेगरी देखने के लिए उसके पास वाले तीर पर टैप करें।
3. **Add Category** दबाएँ, **Name** और चाहें तो **Sort order** टाइप करें, फिर **Save** दबाएँ।
4. **Add Sub-Category** दबाएँ (या किसी कैटेगरी के नीचे **Add sub-category to**), **Category** चुनें, **Name** टाइप करें और **Save** दबाएँ।
5. नाम या क्रम बदलने के लिए पेंसिल आइकन दबाएँ, फिर **Save** दबाएँ। नया नाम उसे इस्तेमाल करने वाले आइटम पर भी बदल जाता है।
6. कैटेगरी को **Inactive** या फिर से **Active** करने के लिए टॉगल इस्तेमाल करें। Inactive कैटेगरी जानकारी डालने वाले फ़ॉर्म पर नहीं दिखतीं।
7. जिस कैटेगरी को कोई आइटम इस्तेमाल नहीं करता, उसे हटाने के लिए डस्टबिन आइकन दबाएँ और **Delete** दबाएँ।

## आम दिक्कतें

- **"You do not have permission to manage categories".** (आपको कैटेगरी संभालने की अनुमति नहीं है) सिर्फ़ लाइब्रेरी टीम ही कैटेगरी बदल सकती है। **Back to the library** दबाएँ।
- **"is in use by ... so it cannot be deleted. Deactivate it instead."** (यह इस्तेमाल में है, इसलिए डिलीट नहीं हो सकती; इसकी जगह बंद करें) टॉगल से इसे बंद कर दें।
- **"has ... sub-categories. Delete or move them first".** (इसके नीचे सब-कैटेगरी हैं; पहले उन्हें डिलीट करें या हटाएँ) इसकी सब-कैटेगरी डिलीट करें या दूसरी जगह ले जाएँ, या कैटेगरी को बंद कर दें।
- **Add Sub-Category ग्रे है।** पहले एक कैटेगरी डालें।
- **"Sort order must be a whole number of 0 or more."** (Sort order 0 या उससे बड़ी पूरी संख्या होनी चाहिए) इसे खाली छोड़ें या 0, 1, 2 जैसे नंबर टाइप करें।
:::

::: section coming-soon
title: लाइब्रेरी का ऐसा पेज जो अभी तैयार नहीं है
routes: /DigitalLibrary/{Section}
for: 
roles: 
designations: 
minutes: 1
source-hash: b471b84357af366e
confidence: ok

**यह स्क्रीन किस काम की है**
Digital Library के कुछ पते ऐसा पेज खोलते हैं जिस पर लिखा होता है कि वह हिस्सा **is being built** (बन रहा है)। लाइब्रेरी का वह हिस्सा अभी तैयार नहीं है।

1. मैसेज पढ़ें। उसमें लिखा होता है कि यह हिस्सा आने वाले किसी अपडेट में मिलेगा।
2. पहले पेज पर वापस जाने के लिए **Back to the library** दबाएँ।

## आम दिक्कतें

- **मैं किसी सेव किए हुए लिंक से इस पेज पर आया।** मेन्यू से लाइब्रेरी खोलें और इसकी जगह **View All Content** इस्तेमाल करें।
:::
