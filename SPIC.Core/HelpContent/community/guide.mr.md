---
module: community
title: कम्युनिटी
icon: bi-chat-square-text
order: 115
language: mr
category: ज्ञान
note: नवीन गाइड
meta-source-hash: 1b034d33bc7629a5
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 59061c3615515658
confidence: ok

**SPIC Knowledge Community** ही प्रॉडक्ट आणि शेतीबद्दल प्रश्न विचारण्याची, शेतात तुम्ही जे पाहिले ते सांगण्याची, आणि SPIC तज्ज्ञ व इतर शेतकऱ्यांकडून उत्तरे मिळवण्याची जागा आहे.

- ask | icon: bi-patch-question | color: #0d9488
  title: प्रश्न विचारा
  text: तुमची अडचण तुमच्या शब्दांत लिहा. मदत होत असेल तर फोटो किंवा कागदपत्रे जोडा.
- answer | icon: bi-patch-check | color: #16a34a
  title: तज्ज्ञांची उत्तरे मिळवा
  text: SPIC कर्मचारी **SPIC Expert** म्हणून उत्तर देतात. इतर शेतकरी आणि डीलरसुद्धा उत्तर देऊ शकतात.
- learn | icon: bi-people | color: #2563eb
  title: इतरांकडून शिका
  text: जुन्या चर्चा शोधा, आवडलेल्या चर्चा फॉलो करा आणि प्रॉडक्ट ग्रुपमध्ये सामील व्हा.
:::

::: section flow
title: प्रश्न कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 8b3d8c2b9b5b6d84
confidence: ok

प्रत्येक ओळ लोकांचा एक गट आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- members | type: lane | color: #0d9488 | roles: Farmer, Dealer
  title: शेतकरी आणि डीलर
  sub: विचारा, उत्तर द्या, शेअर करा
- experts | type: lane | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC तज्ज्ञ
  sub: SPIC कर्मचारी
- ask | lane: members | col: 1 | icon: bi-pencil-square | color: #d97706 | next: reply, expert | screen: ask-question
  title: प्रश्न विचारा
  status: Waiting For Reply
  text: **Create a Discussion** फॉर्म भरा आणि **Post Discussion** दाबा. प्रश्न पहिल्या उत्तराची वाट पाहतो.
- similar | lane: members | col: 2 | icon: bi-search | color: #ea580c | kind: exception | from: ask | to: ask | screen: ask-question
  title: मिळताजुळता प्रश्न सापडला
  text: तुमच्यासारख्या दिसणाऱ्या चर्चा ॲप दाखवते. उत्तर वाचण्यासाठी त्यातली एक उघडा, **Back to edit** दाबा, किंवा **Post Anyway** दाबा.
- reply | lane: members | col: 3 | icon: bi-chat-dots | color: #2563eb | next: resolve | screen: discussion-details
  title: एखादा सदस्य उत्तर देतो
  status: Open
  text: दुसरा शेतकरी किंवा डीलर उत्तर देतो. वाट पाहणारा प्रश्न **Open** होतो.
- expert | lane: experts | col: 3 | icon: bi-patch-check | color: #16a34a | next: resolve | screen: discussion-details
  title: तज्ज्ञ उत्तर देतात
  status: Expert Answer
  text: SPIC कर्मचाऱ्यांनी उत्तर दिले की चर्चेवर **Expert Answer** दिसते.
- resolve | lane: members | col: 4 | icon: bi-check2-circle | color: #64748b | return: reply | screen: discussion-details
  title: अडचण सुटली अशी नोंद करा
  status: Resolved
  text: अडचण सुटली की प्रश्न विचारणारी व्यक्ती **Mark as Resolved** दाबते. **Reopen** दाबल्यावर चर्चा परत **Open** होते.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: af226e0559d41a5d
confidence: ok

- waiting | color: #d97706
  label: Waiting For Reply
  text: पोस्ट केले आहे, पण अजून कोणीही उत्तर दिलेले नाही.
- waiting-answers | color: #d97706
  label: Waiting for Answers
  text: प्रश्न विचारल्यानंतर लगेच "posted" स्क्रीनवर दिसते. याचा अर्थ Waiting For Reply सारखाच आहे.
- open | color: #2563eb
  label: Open
  text: सदस्यांनी उत्तरे दिली आहेत. चर्चा अजून चालू आहे.
- expert | color: #16a34a
  label: Expert Answer
  text: SPIC तज्ज्ञाने उत्तर दिले आहे.
- resolved | color: #64748b
  label: Resolved
  text: प्रश्न विचारणाऱ्याने अडचण सुटली अशी नोंद केली आहे. चर्चा परत उघडता येते.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: b913d791fc7175cc
confidence: ok

तुमचे कार्ड ठळक दिसते. साइन इन करणारे सगळे चर्चा वाचू शकतात आणि त्यात भाग घेऊ शकतात.

- members | color: #0d9488 | roles: Farmer, Dealer
  title: शेतकरी आणि डीलर
  - प्रश्न विचारा आणि फोटो किंवा फाइल जोडा
  - चर्चांना उत्तर द्या, लाइक करा, सेव्ह करा आणि फॉलो करा
  - तुमचे स्वतःचे प्रश्न बदला, डिलीट करा किंवा अडचण सुटली अशी नोंद करा
- experts | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC तज्ज्ञ (SPIC कर्मचारी)
  - प्रश्नांची उत्तरे द्या. तुमच्या उत्तरांवर **SPIC Expert** दिसते
  - इतर सदस्यांसारखेच प्रश्न विचारा आणि शेअर करा
- moderators | color: #475569 | roles: Admin, CorporateAdmin
  title: Admin आणि Corporate Admin
  - कोणतीही चर्चा सुटली अशी नोंद करा, किंवा ती परत उघडा
  - कोणतेही उत्तर डिलीट करा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 971404c02ac40d12
confidence: ok

- cannot-post | popular: true
  q: मी Post Discussion दाबतो आणि लाल संदेश दिसतात
  a: शीर्षक किमान 10 अक्षरांचे हवे, **Category** निवडलेली हवी, आणि **Description** किमान 20 अक्षरांचे हवे. लाल दिसणारे फील्ड दुरुस्त करा आणि परत **Post Discussion** दाबा.
- similar | popular: true
  q: ॲप "We found similar discussions" असे दाखवते. मी काय करू?
  a: तुमच्या प्रश्नाचे उत्तर आधीच असू शकते. ते वाचण्यासाठी **View Discussion** दाबा. तुमचा प्रश्न वेगळा असेल तर **Post Anyway** दाबा. तुमचा मजकूर बदलायचा असेल तर **Back to edit** दाबा.
- edit-delete
  q: माझा प्रश्न कसा बदलू किंवा डिलीट करू?
  a: **Discussions** उघडा, **My Discussions** टॅबवर जा आणि तुमच्या प्रश्नावरचे तीन ठिपके टॅप करा. **Edit** किंवा **Delete** निवडा.
- no-resolve
  q: मला Mark as Resolved दिसत नाही
  a: फक्त प्रश्न विचारणारी व्यक्ती किंवा Admin च चर्चा सुटली अशी नोंद करू शकतात किंवा ती परत उघडू शकतात.
- file-not-attached
  q: माझी फाइल किंवा फोटो जोडला गेला नाही
  a: प्रश्नावरची प्रत्येक फाइल 10 MB किंवा त्यापेक्षा लहान असावी, आणि एका प्रश्नावर जास्तीत जास्त 6 फाइल जोडता येतात. एका उत्तरावर प्रत्येकी 5 MB च्या जास्तीत जास्त 3 इमेज जोडता येतात.
- expert-answers
  q: SPIC तज्ज्ञांची उत्तरे कशी शोधू?
  a: **Discussions** उघडा आणि **Expert Answers** टॅब टॅप करा. चर्चेमध्ये तज्ज्ञांच्या उत्तरांवर **SPIC Expert** टॅग दिसतो.
:::

::: section community-home
title: Knowledge Community चे होम पेज वापरा
routes: /Community
for: 
roles: 
designations: 
minutes: 2
source-hash: ea1bb46391aec31c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**SPIC Knowledge Community** होम पेजवरून तुम्ही सुरुवात करता. इथे तुम्ही प्रश्न विचारू शकता, नवीन चर्चा पाहू शकता आणि एखाद्या प्रॉडक्टच्या चर्चा उघडू शकता.

1. सर्च बॉक्समध्ये तुमचा प्रश्न किंवा प्रॉडक्टचे नाव टाइप करा आणि **Ask a Question** दाबा. तुमचे शब्द शीर्षक म्हणून घेऊन प्रश्नाचा फॉर्म उघडतो.
2. सर्च बॉक्सखालचे आकडे पाहा: **Community Members**, **Discussions** आणि **Expert Answer**.
3. **Recent Discussions** खाली एखादी चर्चा उघडण्यासाठी ती टॅप करा. लिस्ट ग्रिडमध्ये किंवा ओळींमध्ये दाखवण्यासाठी दोन लहान बटणे वापरा.
4. टॅब आणि फिल्टरसह पूर्ण लिस्ट पाहण्यासाठी **View All Discussions** दाबा.
5. **Popular Product** खाली प्रॉडक्ट ग्रुपमध्ये सामील होण्यासाठी **Join** दाबा. ते **Joined** होते. ग्रुप सोडण्यासाठी ते परत दाबा.
6. फक्त त्या प्रॉडक्टबद्दलच्या चर्चा पाहण्यासाठी प्रॉडक्ट कार्डवर **Discussions** दाबा.
7. नवीन प्रश्न सुरू करण्यासाठी **New Here?** बॉक्समध्ये **Ask a Question** दाबा.

> Tip: तुम्ही ग्रिड निवडले की ओळी, हे ॲप लक्षात ठेवते.

## प्रॉडक्टचा फोटो बदलणे

- Digital Library बदलू शकणाऱ्या Admin आणि कर्मचाऱ्यांना प्रत्येक प्रॉडक्ट फोटोवर कॅमेऱ्याचा आयकॉन दिसतो.
- कॅमेरा टॅप करा आणि 3 MB पर्यंतचा JPG, PNG किंवा WEBP फोटो निवडा.

## नेहमीच्या अडचणी

- **"Discussions could not be loaded".** (चर्चा लोड होऊ शकल्या नाहीत) कनेक्शन तुटले. **Try again** दाबा.
- **"No discussions yet".** (अजून एकही चर्चा नाही) अजून कोणीही प्रश्न विचारलेला नाही. पहिला प्रश्न विचारण्यासाठी **Ask a Question** दाबा.
- **"That picture is larger than 3 MB."** (फोटो 3 MB पेक्षा मोठा आहे) प्रॉडक्टचा लहान फोटो निवडा.
:::

::: section browse-discussions
title: चर्चा शोधा आणि फॉलो करा
routes: /Community/discussions
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-list-stats] | 2 > [data-help=cm-list-tabs] | 3 > [data-help=cm-list-filters] | 4 > [data-help=cm-list-tools] | 5 > [data-help=cm-list-card] | 6 > [data-help=cm-list-new]
minutes: 3
source-hash: 6111e390f7350721
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Discussions** स्क्रीनवर कम्युनिटीतले सगळे प्रश्न दिसतात. तुम्ही शोधू शकता, फिल्टर लावू शकता, चर्चा फॉलो करू शकता आणि तुमचे स्वतःचे प्रश्न सांभाळू शकता.

1. वरच्या टाइल पाहा: **Total Discussions**, **Active Members**, **Expert Answers** आणि **New This Month**.
2. एक टॅब निवडा: **All Discussion**, **My Discussions**, **Trending**, **Unanswered**, **Following** किंवा **Expert Answers**.
3. **Filters** उघडा आणि **Date Range**, **Category**, **Product**, **Crop**, **Status** किंवा **Tags** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
4. विषय शोधण्यासाठी **Sort by:** (**Latest Activity**, **Most Replies**, **Most Views** किंवा **Oldest**) वापरा आणि **Search discussions...** मध्ये टाइप करा.
5. चर्चा उघडण्यासाठी ती टॅप करा. चर्चा फॉलो करण्यासाठी व्यक्तीच्या फोटोखाली **Follow** दाबा. तुमच्या स्वतःच्या प्रश्नांवर **Edit** किंवा **Delete** करण्यासाठी तीन ठिपके टॅप करा.
6. नवीन प्रश्न विचारण्यासाठी **Start a Discussion** दाबा.

> Careful: चर्चा डिलीट केली की ती आणि तिची सगळी उत्तरे कम्युनिटीतून निघून जातात.

## टॅबमध्ये काय दिसते

- **My Discussions**: तुम्ही विचारलेले प्रश्न.
- **Trending**: मागच्या 30 दिवसांतले सगळ्यात जास्त चर्चेतले प्रश्न.
- **Unanswered**: ज्यांना अजून एकही उत्तर नाही असे प्रश्न.
- **Following**: तुम्ही फॉलो करत असलेले प्रश्न.
- **Expert Answers**: SPIC तज्ज्ञाने उत्तर दिलेले प्रश्न.

## नेहमीच्या अडचणी

- **"No discussions found".** (एकही चर्चा सापडली नाही) दुसरा टॅब वापरून पाहा किंवा **Clear filters** दाबा.
- **चर्चेवर तीन ठिपके दिसत नाहीत.** तुम्ही फक्त तुमचेच प्रश्न बदलू किंवा डिलीट करू शकता.
- **Tags फिल्टरने काहीच होत नाही.** **Apply Filters** दाबण्याआधी टॅग टाइप करा आणि Enter दाबा.
:::

::: section ask-question
title: प्रश्न विचारा
routes: /Community/new, /Community/posted/{id}
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-new-title] | 2 > [data-help=cm-new-choices] | 3 > [data-help=cm-new-tags] | 4 > [data-help=cm-new-desc] | 5 > [data-help=cm-new-files] | 6 > [data-help=cm-new-post]
minutes: 3
source-hash: a42a0e7fda40c257
confidence: ok

**ही स्क्रीन कशासाठी आहे**
कम्युनिटीला प्रश्न विचारण्यासाठी किंवा तुमचा अनुभव सांगण्यासाठी **Create a Discussion** स्क्रीन वापरा. मग SPIC तज्ज्ञ आणि इतर शेतकरी उत्तर देऊ शकतात.

1. **Discussion Title** मध्ये तुमचा प्रश्न टाइप करा.
2. **Category** निवडा. प्रश्न एखाद्या प्रॉडक्ट किंवा पिकाबद्दल असेल तर **Related Product** आणि **Crop** सुद्धा निवडा.
3. **Tags** खाली रोग किंवा अडचणीसारखा एखादा छोटा शब्द टाइप करा आणि Enter किंवा स्वल्पविराम (,) दाबा. तुम्ही 6 पर्यंत टॅग जोडू शकता.
4. **Description** मध्ये तुमचा प्रश्न, तुम्ही काय करून पाहिले आणि तुम्हाला काय दिसले ते लिहा.
5. **Attachments** खाली फोटो किंवा फाइल बॉक्समध्ये टाका किंवा **Browse Files** दाबा. फाइल काढण्यासाठी फुली (×) टॅप करा.
6. **Post Discussion** दाबा. ॲपने **We found similar discussions** दाखवले तर त्या चर्चा वाचा किंवा **Post Anyway** दाबा. **Your question looks unique!** (तुमचा प्रश्न वेगळा दिसतो) असे दिसले तर परत **Post Discussion** दाबा.

> Tip: पानाचा, रोपाचा किंवा प्रॉडक्टच्या लेबलचा स्पष्ट फोटो असला की तज्ज्ञ लवकर उत्तर देऊ शकतात.

## पोस्ट केल्यानंतर

- **Your question has been posted** स्क्रीनवर शीर्षक, **Category**, **Posted** वेळ, **Status** आणि **Views** दिसतात.
- तुमचा प्रश्न उघडण्यासाठी **View Discussion** दाबा, किंवा होम पेजवर जाण्यासाठी **Back to Community** दाबा.

## तुमचा प्रश्न बदला

- **Discussions** वर तुमच्या प्रश्नावरचे तीन ठिपके टॅप करा आणि **Edit** निवडा. तोच फॉर्म उघडतो.
- मजकूर बदला आणि **Save Changes** दाबा.
- तुम्ही टाइप केलेले काढून टाकण्याआधी **Cancel** तुम्हाला विचारते. तिथेच राहण्यासाठी **Keep editing** दाबा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Discussion Title** | एका ओळीत तुमचा प्रश्न. किमान 10 अक्षरे. भरणे आवश्यक. |
| **Category** | प्रश्नाचा प्रकार. भरणे आवश्यक. |
| **Related Product** | प्रश्न ज्या SPIC प्रॉडक्टबद्दल आहे ते, असेल तर. |
| **Crop** | पीक, उदा. भात किंवा टोमॅटो. |
| **Tags** | इतरांना प्रश्न सापडायला मदत करणारे 6 पर्यंत छोटे शब्द. |
| **Description** | पूर्ण प्रश्न. किमान 20 अक्षरे. भरणे आवश्यक. |
| **Attachments** | इमेज, PDF किंवा Word फाइल, प्रत्येकी 10 MB पर्यंत. |

## नेहमीच्या अडचणी

- **"Please enter a discussion title."** किंवा **"The title needs at least 10 characters."** (शीर्षक लिहा / शीर्षक किमान 10 अक्षरांचे हवे) मोठे शीर्षक लिहा.
- **"Please add a little more detail (at least 20 characters)."** (थोडी जास्त माहिती लिहा, किमान 20 अक्षरे) **Description** मध्ये जास्त लिहा.
- **"That file is larger than 10 MB and was not attached."** (फाइल 10 MB पेक्षा मोठी आहे, त्यामुळे जोडली गेली नाही) फाइल लहान करा किंवा नवीन फोटो काढा.
- **"Your question was posted, but the attachments could not be uploaded."** (प्रश्न पोस्ट झाला, पण फाइल अपलोड झाल्या नाहीत) प्रश्न सेव्ह झाला आहे. तुमचे कनेक्शन तपासा.
:::

::: section discussion-details
title: चर्चा वाचा आणि उत्तर द्या
routes: /Community/discussion/{id}
for: 
roles: 
designations: 
minutes: 3
source-hash: 59af4bc85c32cb5c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Discussion Details** स्क्रीनवर एक प्रश्न, त्याचे फोटो आणि फाइल, आणि सगळी उत्तरे दिसतात. इथे तुम्ही उत्तर देता, लाइक करता, सेव्ह करता, फॉलो करता आणि शेअर करता.

1. वर प्रश्न कोणी विचारला, त्यांचा टॅग (**Farmer**, **Dealer** किंवा **SPIC Expert**), तारीख आणि स्थिती पाहा.
2. प्रश्न वाचा. फोटो मोठा पाहण्यासाठी तो टॅप करा. फाइल उघडण्यासाठी ती टॅप करा.
3. वरची **Save**, **Share** आणि **Follow** बटणे वापरा, आणि प्रश्न लाइक करण्यासाठी अंगठ्याचे बटण दाबा.
4. **Replied** खाली उत्तरे वाचा. **Latest**, **Oldest** किंवा **Most Liked** निवडण्यासाठी **Sort by:** वापरा.
5. एखाद्या उत्तराला उत्तर द्यायचे असेल तर त्याचे **reply** बटण टॅप करा. त्या व्यक्तीच्या नावासह एक बॉक्स उघडतो. तुमचे उत्तर लिहा आणि **Reply** दाबा.
6. प्रश्नाला उत्तर द्यायचे असेल तर खाली **Write a reply...** मध्ये टाइप करा. गरज असेल तर 3 पर्यंत इमेज किंवा लिंक जोडा, मग सेंड बटण दाबा.
7. तुमची अडचण सुटली की **Mark as Resolved** दाबा आणि खात्री करा. आणखी मदत हवी असेल तर **Reopen** दाबा.

> Tip: कॉम्प्युटरवर Enter दाबले की उत्तर जाते. Shift आणि Enter एकत्र दाबले की नवीन ओळ सुरू होते.
> Careful: उत्तरावरचा डस्टबिन आयकॉन ते उत्तर डिलीट करतो. तुम्ही फक्त तुमचीच उत्तरे डिलीट करू शकता. Admin कोणतेही उत्तर डिलीट करू शकतात.

## नेहमीच्या अडचणी

- **"Discussion not found".** (चर्चा सापडली नाही) प्रश्न डिलीट झाला आहे. **Back to Discussions** दाबा.
- **"Please write a few words with your images."** (इमेजसोबत थोडे शब्द लिहा) उत्तरात फक्त इमेज चालत नाहीत, थोडा मजकूर हवा.
- **"A reply can carry up to 3 images."** (एका उत्तरात 3 पर्यंत इमेज जोडता येतात) एक इमेज काढा किंवा उरलेल्या इमेज दुसऱ्या उत्तरात पोस्ट करा.
- **Mark as Resolved बटण दिसत नाही.** फक्त प्रश्न विचारणारी व्यक्ती किंवा Admin च अडचण सुटली अशी नोंद करू शकतात किंवा चर्चा परत उघडू शकतात.
:::
