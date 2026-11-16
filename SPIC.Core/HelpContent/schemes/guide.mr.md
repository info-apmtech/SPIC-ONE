---
module: schemes
title: स्कीम आणि लकी ड्रॉ
icon: bi-gift
order: 104
language: mr
category: विक्री
note: नवीन मार्गदर्शक
meta-source-hash: b8a21e045ebf822e
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 01c42ebca4d8bbab
confidence: ok

SPIC चे प्रॉडक्ट घेणाऱ्या डीलर आणि शेतकऱ्यांना स्कीममधून बक्षीस मिळते. प्रत्येक खरेदीवर एक लकी ड्रॉ एंट्री मिळते. ड्रॉच्या तारखेला विजेते निवडले जातात आणि त्यांचे बक्षीस ॲपमध्ये दिसते.

- create | icon: bi-pencil-square | color: #d97706
  title: स्कीम टीम ती तयार करते
  text: नाव, ठिकाणे, प्रॉडक्टचे नियम, बक्षीस आणि ड्रॉच्या तारखा.
- join | icon: bi-qr-code-scan | color: #0d9488
  title: डीलर आणि शेतकरी भाग घेतात
  text: प्रॉडक्टवरचा QR कोड स्कॅन करा. प्रत्येक खरेदीवर 1 एंट्री मिळते.
- win | icon: bi-trophy | color: #7c3aed
  title: लकी ड्रॉमध्ये विजेते निवडले जातात
  text: विजेत्यांना मेसेज, बक्षीस आणि ते कसे मिळवायचे ते दिसते.
:::

::: section flow
title: स्कीम कशी पुढे जाते, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 801d9ad88aaa016d
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- team | type: lane | color: #d97706 | roles: Admin, CorporateAdmin
  title: स्कीम टीम
  sub: ऑफिसचे कर्मचारी
- people | type: lane | color: #0d9488 | roles: Dealer, Farmer
  title: डीलर आणि शेतकरी
  sub: स्कीमचे सदस्य
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: review | screen: add-scheme
  title: स्कीम तयार करा
  status: Draft
  text: **Create Scheme** फॉर्ममध्ये ठिकाणे, प्रॉडक्टचे नियम, बक्षीस आणि ड्रॉची सेटिंग भरा, मग **Next** दाबा.
- review | lane: team | col: 2 | icon: bi-clipboard-check | color: #d97706 | next: scan | screen: review-scheme
  title: तपासा आणि सुरू करा
  status: Active
  text: **Review Your Scheme** वर प्रत्येक माहिती तपासा, मग स्कीम सुरू करण्यासाठी **Active Scheme** दाबा.
- scan | lane: people | col: 3 | icon: bi-qr-code-scan | color: #0d9488 | next: draw | screen: lucky-draw
  title: खरेदी करा आणि स्कॅन करा
  status: Ticket Activated
  text: प्रॉडक्टवरचा QR कोड स्कॅन करा आणि घेतलेले प्रॉडक्ट निवडा. प्रत्येक खरेदीवर 1 लकी ड्रॉ एंट्री मिळते.
- draw | lane: team | col: 4 | icon: bi-ticket-perforated | color: #2563eb | next: announce | screen: scheme-overview
  title: लकी ड्रॉ
  status: Selected
  text: ड्रॉच्या तारखेला सगळ्या एंट्रीमधून विजेते निवडले जातात. स्कीममध्ये ठरवल्याप्रमाणे ड्रॉ **Automatic** किंवा **Manual** असतो.
- announce | lane: people | col: 5 | icon: bi-megaphone | color: #7c3aed | next: prize | screen: winner-announcement
  title: विजेत्याला कळवले जाते
  status: Notified
  text: विजेत्याला बक्षीस आणि स्कीमच्या नावासह **Congratulations!** दिसते.
- prize | lane: people | col: 6 | icon: bi-gift | color: #16a34a | screen: winner-details
  title: बक्षीस मिळवा
  status: Prize Distributed
  text: **Winner Details** वर बक्षीस, त्याची स्थिती आणि ते दिल्याची तारीख दिसते.
- not-won | lane: people | col: 4 | icon: bi-emoji-smile | color: #ea580c | kind: exception | from: draw | to: scan
  title: या वेळी निवड झाली नाही
  status: Better Luck Next Time!
  text: खरेदी आणि स्कॅन करत राहा. जास्त एंट्री म्हणजे पुढच्या ड्रॉमध्ये जास्त संधी.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 663b2eff26f57f93
confidence: ok

- draft | color: #64748b
  label: Draft
  text: स्कीम सेव्ह केली आहे पण प्रसिद्ध केलेली नाही. डीलर आणि शेतकऱ्यांना ती दिसत नाही.
- active-draft | color: #d97706
  label: ACTIVE DRAFT
  text: स्कीम सुरू करण्याआधी तपासणीच्या स्क्रीनवर दिसते.
- active | color: #16a34a
  label: Active
  text: स्कीम चालू आहे. एंट्रीवर दिसले तर याचा अर्थ ही एंट्री या फेरीत अजून जिंकू शकते.
- participating | color: #0d9488
  label: Participating
  text: डीलर किंवा शेतकरी स्कीममध्ये भाग घेत आहे.
- verified | color: #0891b2
  label: Verified
  text: खरेदी किंवा डीलर तपासलेला आहे.
- ticket | color: #2563eb
  label: Ticket Activated
  text: तुमच्या खरेदीसाठी एक नवीन लकी ड्रॉ एंट्री जोडली गेली.
- used | color: #64748b
  label: Used
  text: ही एंट्री आधीच एका ड्रॉमध्ये गेली आहे.
- expired | color: #475569
  label: Expired
  text: ही एंट्री आता जिंकू शकत नाही.
- selected | color: #7c3aed
  label: Selected
  text: ड्रॉमध्ये विजेता म्हणून निवड झाली.
- notified | color: #d97706
  label: Notified
  text: विजेत्याला कळवले आहे.
- claimed | color: #16a34a
  label: Claimed
  text: विजेत्याने बक्षीस घेतले आहे.
- distributed | color: #16a34a
  label: Prize Distributed
  text: बक्षीस विजेत्याला दिले आहे.
- completed | color: #16a34a
  label: Completed
  text: या तारखेचा ड्रॉ पूर्ण झाला आहे.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 89f40baf961ca43b
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- team | color: #d97706 | roles: Admin, CorporateAdmin
  title: स्कीम टीम
  - स्कीम तयार करा आणि तपासा
  - स्कीम सुरू करा
  - डीलर, एंट्री आणि विजेते यांच्यावर लक्ष ठेवा
- dealer | color: #0d9488 | roles: Dealer
  title: डीलर
  - प्रॉडक्ट स्कॅन करा आणि एंट्री जोडा
  - तुमच्या एंट्री आणि प्रगती पाहा
  - विजेते आणि तुमचे बक्षीस पाहा
- farmer | color: #7c3aed | roles: Farmer
  title: शेतकरी
  - तुम्ही घेतलेले प्रॉडक्ट स्कॅन करा
  - तुमच्या एंट्री पाहा
  - विजेते आणि तुमचे बक्षीस पाहा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9ef969d93e7ac18c
confidence: ok

- how-to-win | popular: true
  q: मला जिंकण्याची संधी कशी मिळेल?
  a: प्रत्येक खरेदीवर तुम्हाला 1 एंट्री मिळते. एंट्री 100 च्या फेरीत मोजल्या जातात. **Grand Draw** उघडण्यासाठी 100 एंट्री पूर्ण करा. लकी ड्रॉ पेजवरचे **How It Works** पाहा.
- entries-count | popular: true
  q: माझ्याकडे किती एंट्री आहेत?
  a: **Lucky Draw** उघडा आणि **View All Entries** दाबा (फोनवर **View Entries**). **TOTAL ENTRIES** कार्डवर या फेरीतल्या तुमच्या चालू एंट्री दिसतात.
- not-won
  q: मला "Better Luck Next Time!" दिसते
  a: या ड्रॉमध्ये तुमची निवड झाली नाही. तुमच्या खरेदी स्कॅन करत राहा. जास्त एंट्री म्हणजे पुढच्या वेळी जास्त संधी.
- prize
  q: मी जिंकलो. माझे बक्षीस कसे मिळेल?
  a: **Winner Details** उघडा. **Prize Information** मध्ये बक्षीस आणि त्याचे **Status** दिसते. मदत हवी असेल तर **Need Help?** बॉक्समध्ये **Contact Support** दाबा.
- change-scheme
  q: स्कीम सुरू होण्याआधी मी ती बदलू शकतो का?
  a: हो. **Review Scheme** वर प्रॉडक्टचा नियम बदलण्यासाठी पेन्सिल, किंवा डिलीट करण्यासाठी डस्टबिन वापरा. स्कीम तयार होईपर्यंत प्रसिद्ध न करता ठेवण्यासाठी **Save Draft** दाबा.
:::

::: section scheme-list
title: सगळ्या स्कीम पाहा
routes: /Schemes
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-stats] | 2 > [data-help=sch-filters] | 3 > [data-help=sch-grid] | 4 > [data-help=sch-view] | 5 > [data-help=sch-create]
minutes: 2
source-hash: 799c692a3e1f6262
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Schemes** स्क्रीनवर प्रत्येक लकी ड्रॉ स्कीम दिसते, आणि वर एकूण आकडे असतात. बाजूच्या मेनूमधून **Scheme List** दाबून ही स्क्रीन उघडा.

1. वरची कार्डे पाहा: **Total Schemes**, **Overall Dealer**, **Participating dealer** आणि **Winners**.
2. स्कीम शोधण्यासाठी **Search Schemes....** मध्ये टाइप करा. लिस्टमधून प्रकार (**Both**, शेतकरी किंवा डीलर), स्थिती (**All Status**) आणि राज्य (**All States**) निवडा.
3. प्रत्येक स्कीम कार्डवर नाव, ती कोणासाठी आहे, स्थिती (उदा. **Active**), तारखा, राज्य आणि जिल्हा, **Participating dealer** आणि **Winners** चे आकडे आणि **THE GRAND REWARD** दिसते.
4. स्कीमचे **Scheme Overview** उघडण्यासाठी कार्डवर **View Details** दाबा.
5. नवीन स्कीम तयार करण्यासाठी **Create Scheme** दाबा.

## नेहमीच्या अडचणी

- **मला स्कीम सापडत नाही.** सर्च बॉक्स रिकामा करा आणि लिस्ट परत **Both**, **All Status** आणि **All States** वर ठेवा.
- **माझ्या मेनूमध्ये Scheme List नाही.** तुमच्या पदाला स्कीमचा ॲक्सेस नाही. तुमच्या ॲडमिनला विचारा.
:::

::: section add-scheme
title: स्कीम तयार करा
routes: /AddScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-add-type] | 2 > [data-help=sch-add-basic] | 3 > [data-help=sch-add-rules] | 4 > [data-help=sch-add-prize] | 5 > [data-help=sch-add-draw] | 6 > [data-help=sch-add-next]
minutes: 5
source-hash: 6e0920ccd77cef79
confidence: ok

**ही स्क्रीन कशासाठी आहे**
नवीन लकी ड्रॉ स्कीम तयार करण्यासाठी **Create Scheme** स्क्रीन वापरा: ती कोणासाठी आहे, कुठे चालते, कोणते प्रॉडक्ट मोजले जातात, बक्षीस काय आहे आणि ड्रॉ कधी होतो.

1. वर, स्कीम कोणासाठी आहे ते निवडा: **Both**, **Farmer** किंवा **Dealer**.
2. **Scheme Type & Basic Info** खाली **Scheme Name** आणि **Draw points** टाका, आणि **State**, **District** आणि **Dealer** निवडा. **Start Date** आणि **End Date** निवडा. **Scheme Status** बॉक्समध्ये **Draft - Scheme is not published** दिसते.
3. **Add Product Rule** खाली **Category** आणि **Product** निवडा, **Dealer Quantity** आणि **Farmer Quantity** टाका, आणि **+ Add Rule** दाबा. प्रत्येक नियम **Product Rules List** मध्ये दिसतो. नियम डिलीट करण्यासाठी डस्टबिन किंवा बदलण्यासाठी पेन्सिल वापरा.
4. **Winner Prize Details** खाली **Prize Name** आणि **Prize Description** टाका, आणि बक्षिसाचा फोटो जोडण्यासाठी **Click to upload image** टॅप करा.
5. **Lucky Draw Configuration** खाली **Automatic** किंवा **Manual** निवडा, **Frequency** निवडा (**Daily**, **Weekly**, **Monthly** किंवा **Custom**), आणि **Date** आणि **Draw Time** ठरवा.
6. नंतरसाठी ठेवायचे असेल तर **Save Draft** दाबा, किंवा स्कीम तपासण्यासाठी **Next** दाबा.

> Tip: बक्षिसाचे नाव, माहिती आणि फोटो डीलरना स्कीम पेजवर, विजेत्याच्या मेसेजमध्ये आणि ड्रॉच्या घोषणेत दिसतात.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Scheme Name** | सगळ्यांना दिसणारे छोटे नाव. |
| **Draw points** | ड्रॉसाठी पात्र होण्यासाठी सदस्याला लागणारे पॉइंट. |
| **State**, **District**, **Dealer** | स्कीम कुठे चालते. एकापेक्षा जास्त निवडता येतात. |
| **Dealer Quantity**, **Farmer Quantity** | या नियमासाठी डीलर किंवा शेतकऱ्याने प्रॉडक्ट किती घ्यायला हवा. |
| **Prize Image** | एक JPG किंवा PNG फोटो, 5 MB पर्यंत. |
| **Draw Time** | दिवसातली ड्रॉची वेळ. |

## नेहमीच्या अडचणी

- **फोटो अपलोड होत नाही.** 5 MB पेक्षा लहान JPG किंवा PNG वापरा.
- **मला एक टप्पा मागे जायचे आहे.** **Previous Step** दाबा.
:::

::: section review-scheme
title: स्कीम तपासा आणि सुरू करा
routes: /ReviewScheme
for: 
roles: 
designations: 
minutes: 3
source-hash: 5c87d7eaecfc2110
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तयार करण्याच्या फॉर्मवर **Next** दाबल्यावर **Review Scheme** स्क्रीनवर नवीन स्कीमची प्रत्येक माहिती एकाच ठिकाणी दिसते, म्हणजे स्कीम सुरू होण्याआधी तुम्ही ती तपासू शकता.

1. **Review Your Scheme** हा बॅनर वाचा.
2. **Scheme Details** खाली **APPLICABLE FOR:**, स्थिती (**ACTIVE DRAFT**), स्कीमचे नाव, ती तयार केल्याची तारीख आणि माहिती तपासा.
3. **Campine Time Line** खाली **Start Date** आणि **End Date** तपासा, आणि शेजारचे बक्षिसाचे कार्ड तपासा.
4. **Product Rules** टेबल तपासा. नियम बदलण्यासाठी पेन्सिल किंवा डिलीट करण्यासाठी डस्टबिन वापरा.
5. **Target Segment** (**State**, **District**, **Dealers**) आणि **Review Checklist** (**Draw Points**, **Product Points**, **Draw Mode**, **Frequency**, **Draw Date** आणि **Draw Time**) तपासा.
6. प्रसिद्ध न करता ठेवण्यासाठी **Save Draft** दाबा, किंवा स्कीम सुरू करण्यासाठी **Active Scheme** दाबा.

> Careful: **Active Scheme** दाबल्यावर डीलर आणि शेतकऱ्यांना स्कीम दिसते आणि ते एंट्री जोडायला सुरुवात करतात.

## नेहमीच्या अडचणी

- **एखादी माहिती चुकीची आहे.** **Active Scheme** दाबण्याआधी तयार करण्याच्या फॉर्मवर परत जा आणि ती दुरुस्त करा.
- **मी अजून सुरू करायला तयार नाही.** **Save Draft** दाबा. स्कीम ड्राफ्टच राहते.
:::

::: section scheme-overview
title: स्कीमवर लक्ष ठेवा
routes: /SchemeOverview
for: 
roles: 
designations: 
minutes: 3
source-hash: 55ace08ba0ddb992
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Scheme Overview** स्क्रीनवर एक स्कीम कशी चालली आहे ते दिसते: कोण भाग घेतो, किती एंट्री आहेत, कोण जिंकले आणि आधीचे सगळे ड्रॉ. स्कीम लिस्टवर **View Details** दाबून ही स्क्रीन उघडा.

1. वर स्कीमचे नाव, ती कोणासाठी आहे, स्थिती (उदा. **Active**), **States** आणि **Duration** पाहा.
2. कार्डे पाहा: **Participating dealer**, **Total Entries** आणि **Winners**.
3. **Overview** टॅबवर मोठे बक्षीस आणि **Draw Setting** पाहा: **Draw Mode**, **Frequency**, **Next Draw Date** आणि **Last Draw Date**.
4. **Participating dealer** टॅबवर प्रत्येक सदस्य त्याच्या **Points** आणि **Entries** सह पाहण्यासाठी शोधा किंवा राज्य निवडा. जास्त माहितीसाठी कार्ड टॅप करा.
5. **Winners** टॅबवर प्रत्येक विजेता स्थिती (**Selected**, **Notified** किंवा **Claimed**), **Prize** आणि **Draw Date** सह पाहा. जास्त माहितीसाठी कार्ड टॅप करा.
6. **Draw History** टॅबवर आधीचा प्रत्येक ड्रॉ त्याची तारीख, **Completed**, बक्षीस आणि किती विजेते निवडले ते पाहा.

## नेहमीच्या अडचणी

- **Winners टॅब रिकामा आहे.** या स्कीमचा अजून एकही ड्रॉ झालेला नाही. **Overview** टॅबवर **Next Draw Date** तपासा.
- **एखादा सदस्य दिसत नाही.** **All States** निवडा आणि सर्च बॉक्स रिकामा करा.
:::

::: section scan-qr
title: प्रॉडक्टचा QR कोड स्कॅन करा
routes: /qr-scanner
for: 
roles: 
designations: 
minutes: 1
source-hash: 4ad0b8818ad680ef
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुम्ही घेतलेल्या SPIC प्रॉडक्टवरचा QR कोड स्कॅन करण्यासाठी **QR Scanner** वापरा. अशा प्रकारे तुमची खरेदी लकी ड्रॉसाठी मोजली जाते.

1. फोन प्रॉडक्टवरच्या QR कोडवर धरा. कोड चौकोनी चौकटीच्या आत ठेवा.
2. अंधार असेल तर फ्लॅशसाठी विजेचा आयकॉन टॅप करा. कॅमेरा बदलण्यासाठी कॅमेऱ्याचा आयकॉन टॅप करा.
3. कोड लहान असेल किंवा लांब असेल तर झूमची पट्टी **+** च्या दिशेने सरकवा.
4. तुमच्याकडे आधीच असलेल्या फोटोमधला कोड स्कॅन करायचा असेल तर फोटोचा आयकॉन टॅप करा.
5. तुमच्या लकी ड्रॉ एंट्री पाहण्यासाठी **View Entries** दाबा.

## नेहमीच्या अडचणी

- **कोड वाचला जात नाही.** कॅमेरा स्वच्छ करा, फ्लॅश चालू करा आणि फोन साधारण एक हात अंतरावर स्थिर धरा.
- **कॅमेरा उघडत नाही.** फोनच्या सेटिंगमध्ये ॲपला कॅमेरा वापरण्याची परवानगी द्या.
:::

::: section scan-product
title: स्कॅन केलेले प्रॉडक्ट भरा
routes: /Scanproduct
for: 
roles: 
designations: 
minutes: 2
source-hash: c9f9991efd4bd6c1
confidence: ok

**ही स्क्रीन कशासाठी आहे**
स्कॅन केल्यावर या स्क्रीनवर कंपनी आणि तिचे प्रॉडक्ट दिसतात. प्रत्येक प्रॉडक्ट तुम्ही किती घेतला ते भरा आणि सबमिट करा.

1. वरचे कंपनीचे कार्ड तपासा: नाव, **VERIFIED**, पत्ता, फोन आणि ईमेल.
2. **Products** खाली तुम्ही घेतलेला प्रत्येक प्रॉडक्ट शोधा.
3. एक वाढवण्यासाठी **+** दाबा. एक कमी करण्यासाठी **−** दाबा. मधला आकडा म्हणजे संख्या.
4. **Submit** दाबा.
5. परत स्कॅन करण्यासाठी **Back To Scan** शेजारचा मागचा बाण टॅप करा.

## नेहमीच्या अडचणी

- **संख्या शून्याच्या खाली जात नाही.** हे नेहमीचेच आहे. शून्य म्हणजे तुम्ही तो प्रॉडक्ट घेतला नाही.
- **चुकीची कंपनी दिसते.** **Back To Scan** वर परत जा आणि योग्य QR कोड स्कॅन करा.
:::

::: section select-products
title: डीलरकडून घेतलेले प्रॉडक्ट निवडा
routes: /SelectPurchasedProducts
for: 
roles: 
designations: 
minutes: 2
source-hash: 79749dd21c644750
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Select Purchased Products** स्क्रीनवर तुम्ही ज्या डीलरकडून खरेदी केली तो डीलर, तुमची लकी ड्रॉची प्रगती आणि तो डीलर विकतो ते प्रॉडक्ट दिसतात. तुम्ही घेतलेले प्रॉडक्ट निवडा.

1. डीलरचे कार्ड तपासा: नाव, **VERIFIED**, पत्ता, फोन आणि ईमेल.
2. **Current Progress** पाहा: तुमच्या **Total Lucky Draw Entries** आणि ग्रँड ड्रॉसाठी अजून किती लागतात.
3. **Available Products** खाली तुम्ही घेतलेला प्रत्येक प्रॉडक्ट शोधा.
4. तुम्ही किती घेतले ते ठरवण्यासाठी **Qty** शेजारचे **+** आणि **−** वापरा.

## नेहमीच्या अडचणी

- **मला माझा प्रॉडक्ट दिसत नाही.** डीलर तो या स्कीममध्ये विकत नसेल. डीलरला विचारा.
- **हा चुकीचा डीलर आहे.** परत जा आणि योग्य डीलरचा QR कोड स्कॅन करा.
:::

::: section lucky-draw
title: माझा लकी ड्रॉ
routes: /Luckydraw
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-ld-entry] | 2 > [data-help=sch-ld-prize] | 3 > [data-help=sch-ld-progress] | 4 > [data-help=sch-ld-how] | 5 > [data-help=sch-ld-actions]
minutes: 2
source-hash: f6f7b0a7b08535be
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुमची खरेदी तपासली गेल्यावर **Lucky Draw** पेजवर **You're in the Lucky Draw!**, तुम्ही जिंकू शकता ते बक्षीस आणि ग्रँड ड्रॉपासून तुम्ही किती जवळ आहात ते दिसते.

1. नवीन एंट्री पाहा: **+1 Lucky Draw Entry**, **Ticket Activated**, **Entry ID** आणि तारीख.
2. **Grand Prize** आणि त्याची अंदाजे किंमत, आणि **Previous Winners** पाहा. जास्त विजेते पाहण्यासाठी **View All** दाबा.
3. **CURRENT CYCLE PROGRESS** खाली 100 पैकी तुमच्या एंट्री, **Next Milestone** आणि **Grand Draw** पाहा.
4. **How It Works** वाचा: प्रत्येक खरेदीवर 1 एंट्री मिळते, आणि 100 एंट्री झाल्या की ग्रँड ड्रॉ उघडतो.
5. जास्त एंट्री जोडण्यासाठी **Scan Another Product** दाबा, किंवा लिस्ट पाहण्यासाठी **View All Entries** दाबा. फोनवर ही बटणे **Scan QR** आणि **View Entries** असतात.

## नेहमीच्या अडचणी

- **माझ्या एंट्रीचा आकडा वाढला नाही.** खरेदी तपासली गेल्यावरच एंट्री जोडली जाते. थोड्या वेळाने **View All Entries** मध्ये परत तपासा.
- **GOLD MILESTONE म्हणजे काय?** हा 100 एंट्रीचा टप्पा आहे, तो पूर्ण झाला की **Grand Draw** उघडतो.
:::

::: section my-entries
title: माझ्या सगळ्या एंट्री पाहा
routes: /LuckyDrawList
for: 
roles: 
designations: 
minutes: 2
source-hash: a976b5d2def67294
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Over All Entries** स्क्रीनवर या फेरीतली तुमची प्रत्येक लकी ड्रॉ एंट्री दिसते, प्रत्येकीचे प्रॉडक्ट आणि पॉइंटसह.

1. **TOTAL ENTRIES** कार्डवर 100 पैकी तुमच्या किती चालू एंट्री आहेत ते दिसते, 25, 50, 75 आणि **GOLD MILESTONE** या खुणांसह.
2. **Your Entries List** खाली एक टॅब निवडा: **All**, **Active**, **Used** किंवा **Expired**.
3. प्रत्येक कार्डवर एंट्रीचा नंबर, तिची स्थिती, तारीख, प्रॉडक्ट, **No.of Products**, **Total Products** आणि **Earned Points** दिसतात.
4. ती एंट्री पाहण्यासाठी कार्डवर **View Details** दाबा.
5. लकी ड्रॉ पेजवर परत जाण्यासाठी मागचा बाण टॅप करा.

## नेहमीच्या अडचणी

- **लिस्ट रिकामी आहे.** या टॅबमध्ये तुमची एकही एंट्री नाही. **All** निवडा.
- **काही एंट्रीवर Expired दिसते.** त्या आता जिंकू शकत नाहीत. एंट्री जोडण्यासाठी नवीन खरेदी स्कॅन करत राहा.
:::

::: section winner-announcement
title: विजेत्याचा मेसेज
routes: /WinnerPopUp
for: 
roles: 
designations: 
minutes: 1
source-hash: a8394f0ccc4ee5a5
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुम्ही जिंकलात की **Congratulations!** मेसेज उघडतो: **You are a Lucky Draw Winner**. त्यात तुमचे बक्षीस आणि स्कीम दिसते.

1. बक्षिसाचे नाव आणि स्कीमचे नाव वाचा.
2. **Winner Details** उघडण्यासाठी **View Details** दाबा.
3. लकी ड्रॉ पेजवर परत जाण्यासाठी मेसेज बंद करा.

## नेहमीच्या अडचणी

- **मी चुकून मेसेज बंद केला.** बाजूच्या मेनूमधून **Winner Details** उघडा.
:::

::: section winner-details
title: माझ्या बक्षिसाची माहिती
routes: /WinnerDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: e8beec48b189b974
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Winner Details** स्क्रीनवर तुमचे लकी ड्रॉचे बक्षीस आणि ते तुमच्यापर्यंत कसे पोहोचेल ते दिसते.

1. वर **You are a Winner** आणि **Draw ID** पाहा.
2. **Draw Summary** मध्ये त्या ड्रॉचे **Participants**, **Winners** आणि **Entries** दिसतात.
3. **Winner Information** मध्ये **Winner Name**, **Type**, **Dealer Code**, **Mobile Number** आणि **Points Earned** दिसतात.
4. **Prize Information** मध्ये **Prize Name**, **Prize Value**, **Status** (उदा. **Prize Distributed**) आणि **Distributed Date** दिसतात.
5. **Scheme Information** आणि **Activity Information** मध्ये स्कीम, **Draw Date**, घोषणा कोणी केली आणि कधी ते दिसते.
6. तुम्हाला **Better Luck Next Time!** दिसले तर एंट्री जोडण्यासाठी **Scan QR** दाबा किंवा त्या पाहण्यासाठी **View Entries** दाबा.
7. तुमच्या बक्षिसाबद्दल काहीही प्रश्न असेल तर **Need Help?** बॉक्समध्ये **Contact Support** दाबा.

## नेहमीच्या अडचणी

- **मोबाइल नंबर चुकीचा आहे.** **Contact Support** दाबा, म्हणजे टीम तुमच्याशी संपर्क करू शकेल.
- **बक्षीस अजून मिळालेले नाही.** **Prize Information** खाली **Status** तपासा, किंवा **Contact Support** दाबा.
:::

::: section scheme-winners
title: स्कीमचे सगळे विजेते पाहा
routes: /CelebrationWinners
for: 
roles: 
designations: 
minutes: 1
source-hash: 633e72a075f230fb
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Scheme Winners** स्क्रीनवर आधीच्या घोषणांमधले लकी ड्रॉचे विजेते दिसतात.

1. **Celebrating Our Winners!** या बॅनरवर **Total Winners Announced** दिसते.
2. लिस्टमधून स्कीम (**All Schemes**), वर्ष, प्रकार (**All Types**) आणि ठिकाण (**All Locations**) निवडा.
3. एक टॅब निवडा: **All Winners**, **Dealer Winners** किंवा **Farmer Winners**.
4. प्रत्येक कार्डवर विजेता, **Dealer** किंवा **Farmer**, स्कीम, ठिकाण, **Prize** आणि **Draw Date** दिसते.
5. विजेत्याबद्दल जास्त माहिती पाहण्यासाठी **View Details** दाबा.

## नेहमीच्या अडचणी

- **मला एखादा विजेता दिसत नाही.** **All Schemes**, **All Types** आणि **All Locations** निवडा, आणि वर्ष तपासा.
:::
