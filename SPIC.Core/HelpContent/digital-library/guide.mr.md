---
module: digital-library
title: डिजिटल लायब्ररी
icon: bi-book
order: 114
language: mr
category: ज्ञान
note: नवीन मार्गदर्शक
meta-source-hash: 1b034d33bc7629a5
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 3269461e350cadf4
confidence: ok

डिजिटल लायब्ररीमध्ये SPIC च्या प्रॉडक्टची माहिती, AI व्हिडिओ आणि प्रॉडक्ट ब्रोशर एकाच ठिकाणी असतात. कोणीही त्यात शोधू शकतो, वाचू शकतो आणि त्याबद्दल SPIC AI ला प्रश्न विचारू शकतो. एक छोटी टीम यात माहिती टाकते आणि ती प्रकाशित (publish) करते.

- find | icon: bi-search | color: #0e7490
  title: तुम्हाला हवे ते शोधा
  text: AI व्हिडिओ, प्रॉडक्टची माहिती आणि ब्रोशर पाहा, किंवा नावाने शोधा.
- ask | icon: bi-stars | color: #7c3aed
  title: SPIC AI ला विचारा
  text: प्रश्न टाइप करा आणि लायब्ररीतल्या माहितीवरून उत्तर मिळवा.
- manage | icon: bi-pencil-square | color: #d97706
  title: लायब्ररी टीम माहिती टाकते
  text: ते नवीन आयटम टाकतात, ते प्रकाशित करतात आणि जुने लपवतात.
:::

::: section flow
title: माहिती तुमच्यापर्यंत कशी पोहोचते, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 7a29b652c1da6588
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- team | type: lane | color: #d97706 | roles: Admin
  title: लायब्ररी टीम
  sub: लायब्ररी सांभाळणारे कर्मचारी
- readers | type: lane | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: सगळे
  sub: लॉगिन केलेले सगळे यूजर
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: publish | screen: add-content
  title: माहिती टाका
  status: Draft
  text: माहिती टाकण्याच्या फॉर्मचे तीन टप्पे भरा. **Save as Draft** केल्यावर ती वाचणाऱ्यांना दिसत नाही.
- publish | lane: team | col: 2 | icon: bi-broadcast | color: #16a34a | next: read | return: create | screen: manage-content
  title: प्रकाशित करा
  status: Published
  text: **Save & Publish** किंवा **Publish** दाबा. **Unpublish** केल्यावर ती परत Drafts मध्ये जाते.
- read | lane: readers | col: 3 | icon: bi-book | color: #0e7490 | next: ask | screen: library-home
  title: शोधा आणि वाचा
  status: Published
  text: प्रकाशित केलेले व्हिडिओ, प्रॉडक्ट पेज आणि ब्रोशर सगळे उघडू शकतात.
- ask | lane: readers | col: 4 | icon: bi-stars | color: #7c3aed | screen: ask-ai
  title: SPIC AI ला विचारा
  text: चॅटमध्ये प्रश्न विचारा. उत्तरात त्यासाठी वापरलेल्या लायब्ररीतल्या आयटमची यादी असते.
- archive | lane: team | col: 3 | icon: bi-archive | color: #475569 | kind: exception | from: publish | to: create | screen: manage-content
  title: जुनी माहिती लपवा
  status: Archived
  text: जुनी किंवा चुकीची माहिती archive केली जाते आणि मग ती वाचणाऱ्यांना दिसत नाही. **Restore** केल्यावर ती परत ड्राफ्ट म्हणून येते.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c0a33e46c0f720af
confidence: ok

- published | color: #16a34a
  label: Published
  text: सगळे ते पाहू आणि उघडू शकतात.
- draft | color: #d97706
  label: Draft
  text: सेव्ह केले आहे पण वाचणाऱ्यांपासून लपवलेले आहे. फक्त लायब्ररी टीमला दिसते.
- archived | color: #475569
  label: Archived
  text: वाचणाऱ्यांपासून लपवलेले आहे. लायब्ररी टीम ते कधीही परत आणू शकते.
- active | color: #0e7490
  label: Active
  text: माहिती टाकण्याच्या फॉर्मवर निवडता येणारी कॅटेगरी.
- inactive | color: #64748b
  label: Inactive
  text: माहिती टाकण्याच्या फॉर्मवर आता निवडता न येणारी कॅटेगरी.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 7b1e4cbe0e13d1bb
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- readers | color: #0e7490 | roles: CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, MDAdmin, MDHOD, Dealer, Farmer
  title: सगळे
  - लायब्ररीत पाहा आणि शोधा
  - व्हिडिओ पाहा आणि ब्रोशर उघडा
  - SPIC AI ला प्रश्न विचारा
- team | color: #d97706 | roles: Admin
  title: लायब्ररी टीम
  - प्रॉडक्ट, AI व्हिडिओ आणि ब्रोशर टाका
  - माहिती publish, unpublish, archive आणि डिलीट करा
  - कॅटेगरीची लिस्ट नेहमी अपडेट ठेवा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 93226ef665c87ba7
confidence: ok

- open-brochure | popular: true
  q: प्रॉडक्ट ब्रोशर कसे वाचायचे?
  a: **Product Brochures** उघडा, ब्रोशर टॅप करा, मग कव्हरच्या चित्राखाली **Open brochure** दाबा. PDF उघडते किंवा तुमच्या फोनमध्ये सेव्ह होते.
- ai-answer | popular: true
  q: SPIC AI चे प्रत्येक उत्तर खरे मानू का?
  a: SPIC AI लायब्ररीतल्या माहितीवरून उत्तर देते, पण त्याच्याकडून चुका होऊ शकतात. शेतकऱ्याला किंवा डीलरला सल्ला देण्याआधी उत्तराखाली दिलेले आयटम उघडा आणि माहिती तपासा.
- no-add
  q: मला Add Content बटण दिसत नाही
  a: फक्त लायब्ररी टीमच माहिती टाकू शकते. तुमच्या पदाला (designation) Digital Library मध्ये माहिती टाकण्याची परवानगी असावी लागते. गरज असेल तर तुमच्या ॲडमिनिस्ट्रेटरला विचारा.
- only-published
  q: मला "Only published Digital Library content is available to you" दिसते
  a: तुम्हाला फक्त प्रकाशित माहितीच दिसू शकते. स्टेटसच्या लिस्टमध्ये **All Status** किंवा **Published** निवडा, किंवा पहिल्या तीनपैकी एक कार्ड टॅप करा.
- category-in-use
  q: मला कॅटेगरी डिलीट करता येत नाही
  a: लायब्ररीतले काही आयटम अजून ती वापरतात. त्याऐवजी टॉगलने ती बंद करा. मग ती माहिती टाकण्याच्या फॉर्मवर दिसत नाही.
:::

::: section library-home
title: लायब्ररीत माहिती शोधा
routes: /DigitalLibrary
for: 
roles: 
designations: 
tour: 1 > [data-help=dl-actions] | 2 > [data-help=dl-search] | 3 > [data-help=dl-categories] | 4 > [data-help=dl-featured]
minutes: 2
source-hash: 96a094bb4b59554c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Digital Library Assistant** स्क्रीन हे लायब्ररीचे पहिले पेज आहे. इथून तुम्ही SPIC AI ला विचारू शकता, एखाद्या प्रकारची माहिती उघडू शकता, किंवा featured आयटम उघडू शकता.

1. SPIC AI ला प्रश्न विचारण्यासाठी **Start Chat** दाबा. तुम्ही लायब्ररी टीममध्ये असाल तर इथे तुम्हाला **Add Content** पण दिसते.
2. सर्च बॉक्समध्ये प्रॉडक्टचे नाव किंवा प्रश्न टाइप करा आणि बाण दाबा. तुम्ही **Find Product Information** किंवा **Show latest brochures** सारखी सूचना पण टॅप करू शकता. यामुळे तुमच्या प्रश्नासह SPIC AI चॅट उघडते.
3. **Digital Library** खाली **AI Videos**, **Product Information** किंवा **Product Brochures** टॅप करा. प्रत्येक कार्डवर त्यात किती आयटम आहेत ते दिसते.
4. **Featured Content** खाली एखाद्या आयटमवर **Open** (किंवा **Explore Videos**) टॅप करा. पूर्ण लिस्ट पाहण्यासाठी **View All Content** दाबा.

> Tip: इथला सर्च बॉक्स SPIC AI ला विचारतो. एखादा आयटम त्याच्या नावाने शोधायचा असेल तर **View All Content** दाबा आणि तिथला सर्च बॉक्स वापरा.

## नेहमीच्या अडचणी

- **"Nothing in the library yet".** (लायब्ररीत अजून काहीही नाही) अजून कोणतीही माहिती प्रकाशित झालेली नाही. नंतर परत पाहा.
- **"The library is not available".** (लायब्ररी उपलब्ध नाही) कनेक्शन जुळले नाही. **Try again** दाबा.
- **माझ्या सर्चने चॅट उघडले.** हे नेहमीचेच आहे. या पेजवरचा सर्च बॉक्स SPIC AI ला विचारतो.
:::

::: section all-content
title: सगळी माहिती पाहा आणि शोधा
routes: /DigitalLibrary/content
for: 
roles: 
designations: 
minutes: 2
source-hash: e3b9101791400b7f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**All Content** स्क्रीनवर लायब्ररीतले सगळे आयटम कार्डच्या रूपात दिसतात. टायटलखालच्या ओळीत किती आयटम जुळतात ते दिसते.

1. फक्त एकाच प्रकारचे आयटम पाहण्यासाठी वर **Product Information**, **AI Videos** किंवा **Product Brochures** सारखे कार्ड टॅप करा. त्यावरचा आकडा त्या प्रकारचे किती आयटम आहेत ते सांगतो.
2. तुम्ही **All Types**, **AI Videos**, **Products** आणि **Brochures** हे टॅब पण वापरू शकता.
3. **Search content...** बॉक्समध्ये टाइप करा. ते टायटल, वर्णन, कीवर्ड, टॅग आणि कॅटेगरीमधले शब्द शोधते.
4. आयटम उघडण्यासाठी कार्डवरचे चित्र किंवा टायटल टॅप करा.
5. आणखी आयटम पाहण्यासाठी खाली **Load More** दाबा.

> Tip: वरचा मागे जाण्याचा बाण तुम्हाला लायब्ररीच्या पहिल्या पेजवर परत नेतो.

## नेहमीच्या अडचणी

- **"No content found".** (कोणतीही माहिती सापडली नाही) तुम्ही निवडलेला प्रकार किंवा सर्च खूप मर्यादित आहे. **Clear filters** दाबा.
- **"Only published Digital Library content is available to you".** (तुम्हाला फक्त प्रकाशित माहितीच दिसू शकते) स्टेटसच्या लिस्टमध्ये **All Status** किंवा **Published** निवडा.
- **"The library is not available".** (लायब्ररी उपलब्ध नाही) कनेक्शन जुळले नाही. **Try again** दाबा.
:::

::: section view-item
title: प्रॉडक्ट, व्हिडिओ किंवा ब्रोशर वाचा
routes: /DigitalLibrary/view/{Id:int}
for: 
roles: 
designations: 
minutes: 2
source-hash: f49b1d4b29efe757
confidence: ok

**ही स्क्रीन कशासाठी आहे**
ही स्क्रीन लायब्ररीतला एक आयटम दाखवते: प्रॉडक्ट पेज, AI व्हिडिओ किंवा ब्रोशर. कोणते भाग दाखवायचे आणि कोणत्या क्रमाने ते लायब्ररी टीम ठरवते, त्यामुळे काही भाग नसू शकतात.

1. लायब्ररीच्या पहिल्या पेजवरून, **All Content** मधून किंवा संबंधित आयटमवरून एखादा आयटम उघडा.
2. वर प्रकार (**Product**, **AI Video** किंवा **Brochure**), टायटल, थोडक्यात वर्णन आणि टॅग पाहा. लिस्टवर परत जाण्यासाठी **Back** दाबा.
3. overview भागात व्हिडिओ चालू करा, प्रॉडक्टचे चित्र पाहा, किंवा PDF उघडण्यासाठी **Open brochure** दाबा.
4. या आयटमबद्दल SPIC AI ला विचारण्यासाठी **SPIC AI Chatbox** मध्ये **Start Conversation** दाबा.
5. **Details** बॉक्समध्ये **Category**, **Published** तारीख, **Author**, **Status**, **Views** आणि टॅग पाहा.
6. संबंधित व्हिडिओ आणि ब्रोशर, मुख्य वैशिष्ट्ये आणि वापर पाहण्यासाठी खाली स्क्रोल करा. संबंधित आयटम उघडण्यासाठी तो टॅप करा.

## नेहमीच्या अडचणी

- **"Content not found".** (माहिती सापडली नाही) तो आयटम डिलीट झाला आहे किंवा आता प्रकाशित नाही. **Back to All Content** दाबा.
- **"No video has been uploaded or linked yet."** (अजून व्हिडिओ अपलोड किंवा लिंक केलेला नाही) लायब्ररी टीमने अजून व्हिडिओ टाकलेला नाही.
- **"No brochure file has been uploaded yet."** (अजून ब्रोशरची फाइल अपलोड केलेली नाही) लायब्ररी टीमने अजून PDF टाकलेली नाही.
- **व्हिडिओ ॲपच्या बाहेर उघडतो.** काही व्हिडिओ लिंक फक्त त्यांच्या स्वतःच्या वेबसाइटवरच उघडतात. **Open the video** दाबा.
:::

::: section ask-ai
title: SPIC AI ला प्रश्न विचारा
routes: /DigitalLibrary/chat, /DigitalLibrary/chat/{ConversationId:int}
for: 
roles: 
designations: 
tour: 1 > [data-help=dl-chat-new] | 2 > [data-help=dl-chat-body] | 3 > [data-help=dl-chat-input] | 4 > [data-help=dl-chat-tools] | 5 > [data-help=dl-chat-list] | 6 > [data-help=dl-chat-share]
minutes: 2
source-hash: 93ef9dd3f309cfcd
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Digital Library Assistant** चॅट स्क्रीनवर तुम्ही SPIC AI ला SPIC प्रॉडक्ट, व्हिडिओ आणि ब्रोशरबद्दल विचारता. उत्तरे लायब्ररीतल्या माहितीवरून येतात आणि त्यात वापरलेल्या आयटमची यादी असते.

1. नवीन संभाषण सुरू करण्यासाठी **New Chat** दाबा.
2. **Tell me about a SPIC product** किंवा **Recommend videos** सारखे सूचनेचे कार्ड टॅप करा. उत्तरे इथे दिसतात. उत्तराखालचा लायब्ररीतला आयटम उघडण्यासाठी तो टॅप करा, किंवा पुढचा प्रश्न टॅप करा.
3. किंवा **Ask anything about SPIC products, videos, brochures** या बॉक्समध्ये तुमचा स्वतःचा प्रश्न टाइप करा.
4. फक्त **AI Videos**, **Product Information** किंवा **Product Brochures** मध्ये शोधण्यासाठी **Select Source** निवडा. लायब्ररीतल्या एका आयटमबद्दल विचारण्यासाठी **Attach** दाबा, किंवा प्रश्न बोलून विचारण्यासाठी **Voice** दाबा. मग **Send** दाबा.
5. तुमच्या आधीच्या चॅट डावीकडे **Today**, **Yesterday** आणि **Earlier** खाली दिसतात. उघडण्यासाठी एक चॅट टॅप करा. डस्टबिन ती चॅट डिलीट करते.
6. संभाषण कोणाला पाठवण्यासाठी किंवा कॉपी करण्यासाठी **Share** दाबा.

> Tip: **Attach** केल्यावर बॉक्समध्ये त्या आयटमच्या नावासह **Asking about:** दिसते. तुम्ही क्रॉस दाबेपर्यंत प्रत्येक प्रश्नाचे उत्तर त्याच आयटमवरून दिले जाते.
> Careful: SPIC AI चुकीची माहिती दाखवू शकते. त्यानुसार काही करण्याआधी उत्तर तपासा.

## नेहमीच्या अडचणी

- **"SPIC AI could not answer just now."** (SPIC AI आत्ता उत्तर देऊ शकले नाही) थोडा वेळ थांबा आणि प्रश्न परत पाठवा.
- **"Voice input is not available on this device."** (या डिव्हाइसवर बोलून प्रश्न विचारता येत नाही) त्याऐवजी प्रश्न टाइप करा.
- **"Start a conversation to share it."** (शेअर करण्यासाठी आधी संभाषण सुरू करा) आधी प्रश्न विचारा, मग **Share** दाबा.
- **उत्तराखाली "Answers from library content" दिसते.** हे उत्तर पूर्ण AI न वापरता, लायब्ररीतल्या आयटममधले शब्द जुळवून तयार केले आहे.
:::

::: section add-content
title: लायब्ररीत माहिती टाका किंवा बदला
routes: /DigitalLibrary/add/{KindRoute}
for: 
roles: 
designations: 
minutes: 5
source-hash: e5986f103fd5415d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
लायब्ररी टीम हा फॉर्म प्रॉडक्ट पेज, AI व्हिडिओ किंवा ब्रोशर टाकण्यासाठी, किंवा त्यात बदल करण्यासाठी वापरते. फॉर्ममध्ये तीन टप्पे आहेत: **Content & Details**, **Related Content** आणि **Page Layout**.

1. लायब्ररीच्या पहिल्या पेजवर किंवा **All Content** वर **Add Content** दाबा. **Create New Content** मध्ये **AI Videos**, **Product Information** किंवा **Product Brochures** निवडा. एखादा आयटम बदलायचा असेल तर त्याऐवजी त्याचा पेन्सिल आयकॉन किंवा **Edit Content** दाबा.
2. **Basic Information** मध्ये कॅटेगरी निवडा आणि टायटल, टॅग आणि **Short Description** भरा. प्रॉडक्टसाठी **Associated Program / Sub-Category**, **Resource Content Format** आणि **Visibility / Enable To** पण निवडा, मग **Product Title** आणि **Keywords (Comma Separated)** भरा.
3. फाइल टाका: प्रॉडक्टचे चित्र, व्हिडिओ फाइल किंवा thumbnail सह **Video URL**, किंवा ब्रोशरचे कव्हर आणि **Brochure PDF**.
4. प्रॉडक्ट किंवा व्हिडिओसाठी overview, **Key Features / Benefits** आणि **Usage / Application** भरा. स्वतःच्या टायटलसह आणखी मजकूर टाकण्यासाठी **Add Section** दाबा.
5. **Next: Related Content** दाबा. आयटम पेजवर दाखवायचे प्रकाशित व्हिडिओ आणि ब्रोशर टिक करा. त्यांचा क्रम बदलण्यासाठी बाण वापरा, आणि किती दाखवायचे ते ठरवण्यासाठी **Max Item** वापरा.
6. **Next: Page Layout** दाबा. भागांचा क्रम ठरवण्यासाठी ते ओढा (drag), किंवा बाण वापरा. एखादा भाग लपवण्यासाठी स्विच वापरा आणि त्याचे नाव बदलण्यासाठी पेन्सिल वापरा.
7. **Save & Publish** दाबा (किंवा बदल करताना **Save Changes**). **Save as Draft** केल्यावर आयटम वाचणाऱ्यांना न दाखवता सेव्ह होतो.

> Tip: कॅटेगरी नसेल तर लिस्टमध्ये **+ Add new category…** निवडा. **Manage categories** ने कॅटेगरीची पूर्ण लिस्ट उघडते.
> Careful: आयटम सेव्ह झाल्यावर फाइल अपलोड होतात. अपलोडचे मेसेज संपेपर्यंत पेजवरच थांबा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Product Title**, **Video Title** किंवा **Brochure Title** | वाचणाऱ्यांना दिसणारे नाव. पुढच्या टप्प्यावर जाण्याआधी भरणे आवश्यक. |
| **Tags** | सर्चला मदत करणारे छोटे शब्द. एक टाइप करा आणि Enter दाबा, किंवा **Used before:** खालचा टॅग टॅप करा. |
| **Product Image** किंवा **Video Thumbnail** | PNG किंवा JPG, 5 MB पर्यंत. सगळ्यात चांगला आकार 1280 x 720 px. |
| **Video File (Upload)** | MP4, WebM किंवा MOV, 700 MB पर्यंत. किंवा YouTube, Vimeo किंवा थेट **Video URL** द्या. |
| **Duration** | हवे असेल तर, hh:mm:ss अशा प्रकारे. आयटम पेजवर दिसते. |
| **Brochure PDF** | एकच PDF फाइल, 25 MB पर्यंत. |

## नेहमीच्या अडचणी

- **"Enter a product title before continuing."** (पुढे जाण्याआधी प्रॉडक्टचे टायटल टाका) आधी टप्पा 1 मध्ये टायटल भरा.
- **"Image must be 5 MB or smaller."** (चित्र 5 MB किंवा त्यापेक्षा लहान असावे) लहान चित्र वापरा. व्हिडिओसाठी हीच मर्यादा 700 MB आणि ब्रोशरसाठी 25 MB आहे.
- **"could not be uploaded. The item itself was saved."** (फाइल अपलोड झाली नाही, पण आयटम सेव्ह झाला आहे) पेन्सिल आयकॉनने तो आयटम परत उघडा आणि फाइल परत निवडा.
- **टप्पा 2 मध्ये "No published videos found" दिसते.** फक्त प्रकाशित आयटमच जोडता येतात. आधी ते प्रकाशित करा.
- **"You do not have permission to add content".** (तुम्हाला माहिती टाकण्याची परवानगी नाही) फक्त लायब्ररी टीमच माहिती टाकू किंवा बदलू शकते.
:::

::: section manage-content
title: माहिती प्रकाशित करा, लपवा किंवा डिलीट करा
routes: /DigitalLibrary/content, /DigitalLibrary/view/{Id:int}
for: DigitalLibrary.Update
roles: 
designations: 
minutes: 3
source-hash: 483778c83ea27554
confidence: ok

**ही स्क्रीन कशासाठी आहे**
वाचणाऱ्यांना काय दिसेल ते लायब्ररी टीम ठरवते. प्रत्येक कार्डवर एक टॅग असतो: **Published**, **Draft** किंवा **Archived**. खालची बटणे फक्त लायब्ररीतली माहिती बदलण्याची परवानगी असलेल्या कर्मचाऱ्यांनाच दिसतात.

1. लपवलेले आयटम पाहण्यासाठी **All Content** वर **Draft Content** किंवा **Archived** टॅप करा, किंवा **All Status** लिस्टमध्ये **Published**, **Draft** किंवा **Archived** निवडा.
2. ड्राफ्ट कार्ड सगळ्यांना दाखवण्यासाठी त्यावर **Publish** दाबा.
3. प्रकाशित आयटम लपवण्यासाठी **Unpublish** दाबा. **Unpublish this content?** मध्ये **Unpublish** दाबा. तो Drafts मध्ये जातो.
4. जुना आयटम archive करण्यासाठी archive आयकॉन दाबा. archive केलेला आयटम परत ड्राफ्ट म्हणून आणण्यासाठी **Restore** दाबा.
5. आयटम बदलण्यासाठी पेन्सिल आयकॉन दाबा, किंवा डिलीट करण्यासाठी डस्टबिन आयकॉन दाबा, मग खात्री करण्यासाठी **Delete** दाबा.
6. आयटम पेजवर **Details** बॉक्समध्ये **Edit Details** आणि **Publish Product** किंवा **Unpublish Video** सारखे publish बटण पण असते.

> Careful: डिलीट केलेली माहिती परत आणता येत नाही. परत लागण्याची शक्यता असेल तर त्याऐवजी archive करा.

## नेहमीच्या अडचणी

- **मला डस्टबिन आयकॉन दिसत नाही.** डिलीट करण्यासाठी वेगळी परवानगी लागते. तुमच्या ॲडमिनिस्ट्रेटरला विचारा.
- **लिस्टमधून एखादा आयटम गायब झाला.** तो आता archive झाला असेल किंवा ड्राफ्ट असेल. स्टेटसच्या लिस्टमध्ये **All Status** निवडा.
:::

::: section categories
title: लायब्ररीच्या कॅटेगरी सांभाळा
routes: /DigitalLibrary/categories
for: 
roles: 
designations: 
minutes: 3
source-hash: f17d81a735096b2f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Library Categories** स्क्रीनवर माहिती टाकण्याच्या फॉर्मवर दिसणाऱ्या कॅटेगरी आणि सब-कॅटेगरीची लिस्ट ठेवली जाते, उदा. एक कॅटेगरी आणि तिच्या खाली तिच्या सब-कॅटेगरी.

1. कॅटेगरी शोधण्यासाठी सर्च बॉक्समध्ये टाइप करा, किंवा **All**, **Active** किंवा **Inactive** निवडा.
2. कॅटेगरीच्या सब-कॅटेगरी पाहण्यासाठी तिच्या शेजारचा बाण टॅप करा.
3. **Add Category** दाबा, **Name** आणि हवा असेल तर **Sort order** टाइप करा, मग **Save** दाबा.
4. **Add Sub-Category** दाबा (किंवा कॅटेगरीखाली **Add sub-category to**), **Category** निवडा, **Name** टाइप करा आणि **Save** दाबा.
5. नाव किंवा क्रम बदलण्यासाठी पेन्सिल आयकॉन दाबा, मग **Save** दाबा. नवीन नाव ते वापरणाऱ्या आयटमवरही बदलते.
6. कॅटेगरी **Inactive** करण्यासाठी किंवा परत **Active** करण्यासाठी टॉगल वापरा. Inactive कॅटेगरी माहिती टाकण्याच्या फॉर्मवर दिसत नाहीत.
7. कोणताही आयटम वापरत नसलेली कॅटेगरी काढण्यासाठी डस्टबिन आयकॉन दाबा आणि **Delete** दाबा.

## नेहमीच्या अडचणी

- **"You do not have permission to manage categories".** (तुम्हाला कॅटेगरी सांभाळण्याची परवानगी नाही) फक्त लायब्ररी टीमच कॅटेगरी बदलू शकते. **Back to the library** दाबा.
- **"is in use by ... so it cannot be deleted. Deactivate it instead."** (ती वापरात आहे, त्यामुळे डिलीट करता येत नाही; त्याऐवजी बंद करा) टॉगलने ती बंद करा.
- **"has ... sub-categories. Delete or move them first".** (तिच्या खाली सब-कॅटेगरी आहेत; आधी त्या डिलीट करा किंवा हलवा) तिच्या सब-कॅटेगरी डिलीट करा किंवा हलवा, किंवा कॅटेगरी बंद करा.
- **Add Sub-Category राखाडी आहे.** आधी एक कॅटेगरी टाका.
- **"Sort order must be a whole number of 0 or more."** (Sort order 0 किंवा त्यापेक्षा मोठी पूर्ण संख्या असावी) रिकामे सोडा किंवा 0, 1, 2 असे टाइप करा.
:::

::: section coming-soon
title: अजून तयार नसलेले लायब्ररीचे पेज
routes: /DigitalLibrary/{Section}
for: 
roles: 
designations: 
minutes: 1
source-hash: b471b84357af366e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
Digital Library चे काही पत्ते असे पेज उघडतात ज्यावर तो भाग **is being built** (तयार होत आहे) असे लिहिलेले असते. लायब्ररीचा तो भाग अजून तयार नाही.

1. मेसेज वाचा. तो भाग पुढच्या रिलीजमध्ये उपलब्ध होईल असे त्यात लिहिलेले असते.
2. पहिल्या पेजवर परत जाण्यासाठी **Back to the library** दाबा.

## नेहमीच्या अडचणी

- **मी सेव्ह केलेल्या लिंकवरून या पेजवर आलो.** मेनूमधून लायब्ररी उघडा आणि त्याऐवजी **View All Content** वापरा.
:::
