---
module: welfare
title: कल्याण योजनाएँ (SDWA)
icon: bi-heart
order: 111
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
source-hash: fa298dd497c945c5
confidence: ok

डीलर ऐप से SDWA कल्याण योजनाओं के लिए आवेदन कर सकते हैं, जैसे शादी का तोहफ़ा, इलाज में मदद या पढ़ाई में मदद। अफ़सर हर आवेदन को एक-एक स्टेप में जाँचते हैं, और डीलर कभी भी देख सकता है कि आवेदन कहाँ है।

- apply | icon: bi-pencil-square | color: #be123c
  title: डीलर आवेदन करता है
  text: अपनी पात्रता जाँचें, योजना चुनें, फ़ॉर्म भरें और दस्तावेज़ अपलोड करें।
- approve | icon: bi-shield-check | color: #d97706
  title: पाँच जाँच
  text: MO, RM, SM, SDWA और SDWA Admin बारी-बारी से आवेदन जाँचते हैं।
- benefit | icon: bi-cash-coin | color: #16a34a
  title: आख़िरी मंज़ूरी
  text: SDWA Admin आख़िरी मंज़ूरी देता है और चेक की जानकारी दर्ज करता है।
:::

::: section flow
title: आवेदन कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 942cc75f4b898ba1
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- dealer | type: lane | color: #be123c | roles: Dealer
  title: डीलर
  sub: आवेदन करता है और देखता है कि वह कहाँ है
- mo | type: lane | color: #d97706 | roles: MO
  title: MO
  sub: HQ की जाँच करता है
- rm | type: lane | color: #0d9488 | roles: RM
  title: RM
  sub: रीजन की जाँच करता है
- sm | type: lane | color: #2563eb | roles: SMM
  title: SM
  sub: राज्य की जाँच करता है
- sdwa | type: lane | color: #7c3aed
  title: SDWA
  sub: SDWA · SDWA Admin
- choose | lane: dealer | col: 1 | icon: bi-ui-checks | color: #64748b | next: apply | screen: dashboard
  title: पात्रता जाँचें
  text: डैशबोर्ड पर **Apply Welfare** के नीचे **Apply Now** दबाएँ। अगर आप पात्र हैं, तो योजना चुनें।
- apply | lane: dealer | col: 2 | icon: bi-pencil-square | color: #64748b | next: mo-check | screen: apply-scheme
  title: फ़ॉर्म भरें और सबमिट करें
  status: Draft
  text: फ़ॉर्म भरें, दस्तावेज़ अपलोड करें और सबमिट करें। तब तक यह सिर्फ़ एक ड्राफ़्ट है।
- mo-check | lane: mo | col: 3 | icon: bi-search | color: #d97706 | next: rm-check | screen: scheme-approval
  title: MO की जाँच
  status: Pending MO
  text: MO अपने HQ के डीलरों की जानकारी, बिक्री और दस्तावेज़ जाँचता है।
- rm-check | lane: rm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: sm-check | return: mo-check | screen: scheme-approval
  title: RM की जाँच
  status: Pending RM
  text: RM इसे रीजन के लिए जाँचता है। रिजेक्ट करने पर आवेदन MO के पास लौट जाता है।
- sm-check | lane: sm | col: 5 | icon: bi-hourglass-split | color: #d97706 | next: sdwa-check | return: rm-check | screen: scheme-approval
  title: SM की जाँच
  status: Pending SM
  text: SM इसे राज्य के लिए जाँचता है। रिजेक्ट करने पर आवेदन RM के पास लौट जाता है।
- sdwa-check | lane: sdwa | col: 6 | icon: bi-patch-check | color: #7c3aed | next: sdwaadmin | return: sm-check | screen: scheme-approval
  title: SDWA की मंज़ूरी
  status: Pending SDWA
  text: SDWA इसे मंज़ूर करता है। रिजेक्ट करने पर आवेदन SM के पास लौट जाता है।
- sdwaadmin | lane: sdwa | col: 7 | icon: bi-cash-coin | color: #7c3aed | next: approved | return: sdwa-check | screen: scheme-approval
  title: SDWA Admin की आख़िरी मंज़ूरी
  status: Pending SDWA Admin
  text: SDWA Admin आख़िरी मंज़ूरी देता है और चेक नंबर और रक़म भरता है।
- approved | lane: dealer | col: 8 | icon: bi-check-circle | color: #16a34a | screen: my-applications
  title: मंज़ूर
  status: Approved
  text: आवेदन मंज़ूर हो गया। डीलर को **My Applications** में **Approved** दिखता है।
- returned | lane: dealer | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: mo-check | to: apply | screen: my-applications
  title: सुधारें और फिर से सबमिट करें
  status: Rejected by MO – Resubmission Required
  text: अगर MO इसे रिजेक्ट करता है, तो आवेदन डीलर के पास लौट आता है। इसे सुधारें और फिर से सबमिट करें। इसका Application ID वही रहता है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8a133f3149737003
confidence: ok

- draft | color: #64748b
  label: Draft
  text: सेव है पर भेजा नहीं गया।
- pending-mo | color: #d97706
  label: Pending MO
  text: जाँच के लिए MO के पास है।
- pending-rm | color: #d97706
  label: Pending RM
  text: जाँच के लिए RM के पास है।
- pending-sm | color: #d97706
  label: Pending SM
  text: जाँच के लिए SM के पास है।
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: मंज़ूरी के लिए SDWA के पास है।
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: आख़िरी मंज़ूरी के लिए SDWA Admin के पास है।
- approved | color: #16a34a
  label: Approved
  text: आख़िरी मंज़ूरी मिल गई।
- resubmit | color: #dc2626
  label: Rejected by MO – Resubmission Required
  text: यह डीलर को दिखता है। आवेदन सुधारें और फिर से सबमिट करें।
- returned | color: #ea580c
  label: Returned to Dealer
  text: यह अफ़सरों को दिखता है। आवेदन सुधार के लिए डीलर के पास वापस गया है।
- rejected | color: #475569
  label: Rejected
  text: बंद हो गया। ज़रूरत हो तो फिर से आवेदन करें।
- cancelled | color: #94a3b8
  label: Cancelled
  text: आवेदन रद्द कर दिया गया।
- awaiting-mo | color: #d97706
  label: Awaiting MO Review
  text: MO का इंतज़ार कर रहे नए आवेदन के लिए डैशबोर्ड पर दिखने वाला नाम।
- under-mo | color: #d97706
  label: Under MO Review
  text: MO के पास वापस आए आवेदन के लिए डैशबोर्ड पर दिखने वाला नाम।
- under-rm | color: #d97706
  label: Under RM Review
  text: Pending RM के लिए डैशबोर्ड पर दिखने वाला नाम।
- under-smm | color: #d97706
  label: Under SMM Review
  text: Pending SM के लिए डैशबोर्ड पर दिखने वाला नाम।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 69fdf9c12e670358
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- dealer | color: #be123c | roles: Dealer
  title: डीलर
  - पात्रता जाँचना और आवेदन करना
  - आवेदन पर नज़र रखना और PDF डाउनलोड करना
  - लौटाया गया आवेदन सुधारकर फिर से सबमिट करना
- mo | color: #d97706 | roles: MO
  title: MO
  - अपने HQ के आवेदन जाँचना
  - गलत दस्तावेज़ बदलना या मिटाना
  - मंज़ूर करना, या डीलर को लौटाना
- rm-sm | color: #0d9488 | roles: RM, SMM
  title: RM और SM
  - अपने रीजन या राज्य के आवेदन जाँचना
  - मंज़ूर करना, या एक स्टेप पीछे लौटाना
- sdwa | color: #7c3aed
  title: SDWA और SDWA Admin
  - सभी इलाकों के आवेदन देखना
  - आख़िरी दो मंज़ूरियाँ देना
  - SDWA Admin चेक की जानकारी भरता है
- director | color: #475569 | roles: Director
  title: डायरेक्टर
  - सभी आवेदन देखना
  - SDWA की मंज़ूरी के बाद कुछ योजनाओं के लिए चाहें तो अपनी राय (रिव्यू) देना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: cd3161af8efa4aae
confidence: ok

- returned | popular: true
  q: मेरे आवेदन पर "Rejected by MO – Resubmission Required" लिखा है
  a: **My Applications** खोलें, लाल बॉक्स में कारण पढ़ें, **Correct & Resubmit** दबाएँ, जानकारी या दस्तावेज़ ठीक करें और **Resubmit Application** दबाएँ। इसका Application ID वही रहता है।
- not-eligible | popular: true
  q: Welfare Eligibility विंडो कहती है कि मैं पात्र नहीं हूँ
  a: विंडो में हर नियम के साथ **Required** और **Your dealership** दिखते हैं। लाल रंग वाले नियम अभी पूरे नहीं हुए हैं। अगर आपको कोई आँकड़ा गलत लगे, तो अपने MO से बात करें।
- seven-days
  q: मुझे "Invitation must be sent at least 7 days in advance" दिख रहा है
  a: Wedding Gift, Sathabhishekam और Grahapravesam के लिए कार्यक्रम की तारीख आज से कम से कम 7 दिन बाद की होनी चाहिए।
- cheque
  q: फ़ॉर्म में "Cancelled Cheque / Bank Passbook is required" लिखा आ रहा है
  a: एक कैंसल किया हुआ चेक या बैंक पासबुक का पन्ना अपलोड करें, और **Beneficiary Name** ठीक वैसा ही टाइप करें जैसा उस पर छपा है।
- where-is-it
  q: मेरा आवेदन अभी किसके पास है?
  a: **My Applications** में गोलों की लाइन हर स्टेप दिखाती है। हरा मतलब पूरा हो गया, और नारंगी मतलब आवेदन अभी उसी स्टेप पर है।
- no-submit
  q: मैं अफ़सर हूँ और आवेदन मंज़ूर नहीं कर पा रहा
  a: आप आवेदन पर तभी काम कर सकते हैं जब वह आपके स्टेज पर हो और आपके इलाके का हो। लिस्ट में उसका स्टेटस देखें।
:::

::: section dashboard
title: डीलर डैशबोर्ड का इस्तेमाल करें
routes: /SDWADashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: 44c37577fe4705d0
confidence: ok

**यह स्क्रीन किस काम की है**
**Dashboard** डीलर के लिए कल्याण योजना के आवेदनों और गेस्ट हाउस बुकिंग का मुख्य पेज है। यहाँ से नया आवेदन शुरू करें और देखें कि आपके आवेदन कहाँ तक पहुँचे।

1. सबसे ऊपर अपना नाम, **Dealer Code**, **Region** और **Profile Completion** देख लें।
2. **Quick Actions** में **Apply Welfare** कार्ड पर **Apply Now** दबाएँ। ऐप पहले आपकी पात्रता जाँचता है।
3. **Welfare Eligibility** विंडो में आपका **State**, **State Group**, **Dealer Type** और **Eligibility Criteria** का हर नियम दिखता है। अगर आप पात्र हैं, तो **Proceed to Apply** दबाएँ। अगर नहीं, तो **Close** दबाएँ।
4. **Application Status Overview** देखें: **Pending Applications**, **Approved Applications**, **Rejected Applications** और **Processing Applications**।
5. **Recent Applications** में किसी आवेदन को खोलने के लिए **View Details** दबाएँ, या **My Applications** खोलने के लिए **View All** दबाएँ।
6. ताज़ा बदलाव जानने के लिए **Notifications** और **Recent Activity** पढ़ें।

> Tip: इस स्क्रीन पर **Book Room** और **My Bookings** गेस्ट हाउस के लिए हैं। उसके लिए Guest house गाइड देखें।

## आम दिक्कतें

- **"Unable to verify eligibility. Please try again later."** (पात्रता जाँची नहीं जा सकी। बाद में फिर से कोशिश करें।) अपना इंटरनेट देखें और फिर से **Apply Now** दबाएँ।
- **"No applications yet".** (अभी तक कोई आवेदन नहीं) आपने अभी तक आवेदन नहीं किया है। **Apply Welfare** दबाएँ।
- **मुझे Apply Welfare नहीं दिख रहा।** SDWA Admin यूज़र इस स्क्रीन से आवेदन नहीं करते।
:::

::: section choose-scheme
title: कल्याण योजना चुनें
routes: /WelfareSchemes
for: 
roles: 
designations: 
minutes: 1
source-hash: 71f07f60f43b7d0c
confidence: ok

**यह स्क्रीन किस काम की है**
**Apply for Welfare Schemes** स्क्रीन पर हर SDWA योजना एक कार्ड के रूप में दिखती है। जो योजना आपको चाहिए, उसे चुनें।

1. कार्ड देखें: **Medical Assistance**, **Wedding Gift**, **Grahapravesam**, **Educational Assistance**, **Sathabhishekam**, **Death Relief Assistance**, **Merit Award** और **Distinction Award**।
2. हर कार्ड पर **Documents** (कितने दस्तावेज़ चाहिए) और **Eligibility** (कौन आवेदन कर सकता है) देखें।
3. जो योजना आपको चाहिए, उस पर **Apply Now** दबाएँ। उस योजना का आवेदन फ़ॉर्म खुल जाता है।

> Careful: **Distinction Award** सिर्फ़ तमिलनाडु (TN) के डीलरों और कर्मचारियों के लिए है।

## आम दिक्कतें

- **मैं यह स्क्रीन नहीं खोल पा रहा।** हो सकता है आपके लॉगिन को कल्याण योजना का एक्सेस न हो। अपने MO या admin से पूछें।
:::

::: section grahapravesam
title: गृहप्रवेश (Grahapravesam) योजना के बारे में पढ़ें
routes: /GrahapravesamScheme
for: 
roles: 
designations: 
minutes: 1
source-hash: a035ff18db9c4a2f
confidence: ok

**यह स्क्रीन किस काम की है**
आवेदन करने से पहले **Grahapravesam** स्क्रीन पर गृहप्रवेश के तोहफ़े वाली योजना समझाई गई है।

1. ऊपर का कार्ड और **About the Scheme** पढ़ें।
2. **Eligibility Criteria** और **Required Documents** देखें।
3. **Frequently Asked Questions** और **Important Notes** पढ़ें।
4. फ़ॉर्म खोलने के लिए **Apply Now** दबाएँ, या दूसरी योजना चुनने के लिए **Back to Schemes** दबाएँ।

## आम दिक्कतें

- **फ़ॉर्म मेरी गृहप्रवेश की तारीख नहीं ले रहा।** फ़ॉर्म में तारीख आज से कम से कम 7 दिन बाद की होनी चाहिए।
:::

::: section apply-scheme
title: कल्याण योजना के लिए आवेदन करें
routes: /ApplyWelfareScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-dealer] | 2 > [data-help=wf-quantity] | 3 > [data-help=wf-details] | 4 > [data-help=wf-documents] | 5 > [data-help=wf-declaration] | 6 > [data-help=wf-actions]
minutes: 5
source-hash: 55566f3677387604
confidence: ok

**यह स्क्रीन किस काम की है**
अपना आवेदन भरकर भेजने के लिए **Apply for** स्क्रीन (जैसे **Apply for Wedding**) का इस्तेमाल करें। हर योजना के लिए फ़ॉर्म थोड़ा अलग होता है।

1. अपना डीलर कार्ड (**Location Info** और **Personal Info**) देखें और योजना का **Eligibility Conditions** बॉक्स पढ़ें।
2. **Quantity Lifted** में **Quantity Lifted during Last Year (MT)** चुनें। 3 साल का कुल अपने-आप भर जाता है। फिर **Beneficiary Details** खुलता है: **Select Beneficiary Group** चुनें और नॉमिनी और लाभार्थी की जानकारी भरें।
3. योजना वाला हिस्सा भरें, जैसे **Wedding Details** में **Wedding Date**, या **Medical Assistance Details** में **Treatment / Medical Condition**।
4. **Document Upload** में हर दस्तावेज़ के लिए **Click to upload** पर टैप करें। **Cancelled Cheque / Bank Passbook** के लिए उस पर छपा **Beneficiary Name** भी टाइप करें।
5. **Declaration** में लिखी बात पढ़ें और बॉक्स पर टिक करें।
6. आवेदन रखने के लिए **Save as Draft** दबाएँ, या **Preview Application** दबाएँ। उसे जाँचें, **Submit Application** दबाएँ, फिर **Confirm** दबाएँ। **Application ID** लिख लें और **Track Application** दबाएँ।

> Careful: **Confirm** दबाने के बाद आवेदन MO के पास चला जाता है, और जब तक वह आपको लौटाया न जाए, आप उसे बदल नहीं सकते।
> Tip: सेव किए गए ड्राफ़्ट को एक रेफ़रेंस नंबर मिलता है और वह **My Applications** में **Drafts** में गिना जाता है।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Quantity Lifted during Last Year (MT)** | पिछले साल आपने कितना माल उठाया, उसकी रेंज। ज़रूरी है। |
| **Select Beneficiary Group** | **Direct Dealer**, **Sub Dealer** या **Approved Employee**। ज़रूरी है। |
| **Nominee Name** और **Nominee Relationship** | आपके डीलर रिकॉर्ड में दर्ज नॉमिनी। ज़रूरी है। |
| **Select Beneficiary** | परिवार का कोई सदस्य, या **Add new beneficiary**। डायरेक्ट डीलर के लिए ज़रूरी है। |
| कार्यक्रम की तारीख | Wedding, Sathabhishekam और Grahapravesam: आज से कम से कम 7 दिन बाद की। |
| **Date of Death** और **Legal Heir Relation** | सिर्फ़ Death Relief के लिए। अगर रिश्ता **Others** है, तो हलफ़नामा (affidavit) अपलोड करें। |
| दस्तावेज़ | PDF, JPG या PNG, हर फ़ाइल 5 MB तक। |

## आम दिक्कतें

- **"Please fix the following errors:"** (नीचे दी गई गलतियाँ ठीक करें) बटनों के ऊपर की लिस्ट पढ़ें और हर गलती ठीक करें।
- **फ़ाइल अपलोड करने पर कुछ नहीं होता।** फ़ाइल PDF, JPG या PNG होनी चाहिए और 5 MB से छोटी होनी चाहिए।
- **"Minimum 80% marks is required for the Merit Award."** (Merit Award के लिए कम से कम 80% अंक ज़रूरी हैं) Merit Award के लिए कम से कम 80% अंक चाहिए। Distinction Award के लिए 80% से ज़्यादा अंक चाहिए और कोई विषय बाकी (arrears) नहीं होना चाहिए।
- **"A minimum average annual sales of 50,000 MT..."** (हर साल औसतन कम से कम 50,000 MT बिक्री...) सब डीलर के लिए आवेदन करने के हिसाब से आपकी बिक्री बहुत कम है (कर्मचारी के लिए 5,000 MT)।
:::

::: section my-applications
title: अपने आवेदनों पर नज़र रखें
routes: /WellfareApplication
for: 
roles: 
designations: 
minutes: 2
source-hash: 62f3f5bcf69937c0
confidence: ok

**यह स्क्रीन किस काम की है**
**My Applications** स्क्रीन पर आपका हर कल्याण आवेदन दिखता है, उसके स्टेटस के साथ, और यह भी कि वह किन-किन स्टेप से गुज़र चुका है।

1. ऊपर की टाइलें देखें: **Total**, **Pending**, **Approved**, **Rejected**, **Drafts** और **Cancelled**।
2. **Latest First** या **Oldest First** चुनें, कोई स्टेटस चुनें, या **Search by Application ID or Scheme** में टाइप करें।
3. हर कार्ड पर योजना, Application ID और स्टेटस देखें। गोलों की लाइन में **Dealer**, **MO**, **RM**, **SM/SMM**, **SDWA**, **SDWA Admin** और **Final** दिखते हैं। हरा मतलब पूरा हो गया, नारंगी मतलब आवेदन अभी वहीं है।
4. पूरी जानकारी देखने के लिए **View Details** दबाएँ, या एक कॉपी सेव करने के लिए **Download PDF** दबाएँ।
5. अगर लाल बॉक्स में **Rejected by** लिखा है, तो **Reason** पढ़ें और **Correct & Resubmit** दबाएँ। फ़ॉर्म ठीक करें, **Resubmit Application** दबाएँ, फिर **Confirm Resubmission** दबाएँ।
6. नया आवेदन शुरू करने के लिए **Apply for a scheme** दबाएँ।

> Tip: फिर से सबमिट किए गए आवेदन का Application ID वही रहता है और वह फिर से MO से शुरू होता है।

## आम दिक्कतें

- **"No matching applications".** (कोई मिलता-जुलता आवेदन नहीं) **Clear filters** दबाएँ।
- **Download PDF पर "Failed – Tap to Retry" दिख रहा है।** अपना इंटरनेट देखें और फिर से टैप करें।
- **Correct & Resubmit बटन नहीं है।** सिर्फ़ वही आवेदन फिर से सबमिट हो सकता है जिसे MO ने आपको लौटाया हो।
:::

::: section scheme-approval
title: कल्याण आवेदन जाँचें और मंज़ूर करें
routes: /SchemeApproval
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-ap-cards] | 2 > [data-help=wf-ap-tabs] | 3 > [data-help=wf-ap-search] | 4 > [data-help=wf-ap-table] | 5 > [data-help=wf-ap-table] | 6 > [data-help=wf-ap-pages]
minutes: 3
source-hash: 06b3fdb4303eee15
confidence: ok

**यह स्क्रीन किस काम की है**
MO, RM, SM, SDWA और SDWA Admin अपने स्टेज पर रुके कल्याण आवेदनों को जाँचने के लिए **Approval** स्क्रीन का इस्तेमाल करते हैं। MO को HQ दिखता है, RM को रीजन, SM को राज्य। SDWA और SDWA Admin को सभी इलाके दिखते हैं।

1. कार्ड देखें: **Total Applications**, आपके स्टेज का **Pending** (जैसे **Pending MO**), **Validated by MO**, **Rejected** और **Completed**।
2. कोई टैब चुनें, जैसे **Pending**, आपके स्टेज का **Approved by**, **Validated by MO**, **Recommended by RM**, **Approved by SM**, **Rejected** या **Completed**।
3. **Search dealer / application...** में टाइप करें या **All Schemes** में योजना चुनें।
4. टेबल में **Status** और **Approval Flow (MO → RM → SM → SDWA → SDWA Admin)** देखें। हरा टिक मतलब पूरा, नीला गोला मतलब आवेदन अभी उसी स्टेज पर है, लाल क्रॉस मतलब रिजेक्ट हुआ।
5. आवेदन खोलने के लिए आँख वाले आइकन पर टैप करें। जानकारी और दस्तावेज़ जाँचें, **Comments** चुनें, और **Recommendation** अपने-आप भर जाता है। **Submit** दबाएँ।
6. और लाइनें देखने के लिए **Items Per Page** और तीर वाले बटन इस्तेमाल करें।

> Careful: रिजेक्ट वाला कमेंट आवेदन को एक स्टेप पीछे भेज देता है। MO के रिजेक्ट करने पर आवेदन डीलर के पास लौट जाता है।
> Tip: SDWA Admin को **Cheque Number** और **Cheque Amount** भी भरना होता है, और वह **Cheque Image** भी जोड़ सकता है।

## मंज़ूरी का क्रम

- पहले MO, फिर RM, फिर SM, फिर SDWA, और आख़िर में SDWA Admin आख़िरी मंज़ूरी देता है।
- Admin, Corporate Admin और Director लिस्ट खोल सकते हैं, पर मंज़ूरी नहीं देते। SDWA की मंज़ूरी के बाद Director, Death Relief, Educational Assistance, Merit Award और Medical Assistance के लिए चाहें तो अपनी राय (रिव्यू) जोड़ सकता है।

## आम दिक्कतें

- **"No applications found for this view."** (इस व्यू में कोई आवेदन नहीं मिला) इस टैब में कुछ भी बाकी नहीं है। कोई दूसरा टैब देखें।
- **"Unable to load the approval queue. Please try again."** (मंज़ूरी की लिस्ट लोड नहीं हो सकी। फिर से कोशिश करें।) **Retry** दबाएँ।
- **Submit बटन नहीं है।** आवेदन अभी आपके स्टेज पर नहीं है।
- **"Please select a comment."** (कोई कमेंट चुनें) **Submit** दबाने से पहले कमेंट चुनें।
:::

::: section company-master
title: SDWA कंपनियाँ संभालें
routes: /SdwaCompanyMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: de3ec8b1ed8fc6f6
confidence: ok

**यह स्क्रीन किस काम की है**
**SDWA Company Details** स्क्रीन पर बिलिंग के लिए गेस्ट हाउस से जुड़ी कंपनियों की लिस्ट रखी जाती है।

1. कार्ड देखें: **Total Companies**, **Active** और **Inactive**।
2. **Add New Company** में **Company Name**, **Short Name / Code** और **GSTIN** भरें।
3. **Guest House(s)** में हर गेस्ट हाउस का नाम टाइप करें और उस पर टैप करें। किसी नाम को हटाने के लिए उस पर बने छोटे x पर टैप करें।
4. स्विच को **Active** या **Inactive** पर रखें, फिर **Save** दबाएँ। **Cancel** दबाने से फ़ॉर्म खाली हो जाता है।
5. **Existing Companies** में सर्च करें या **All Status**, **Active** या **Inactive** चुनें। बदलने के लिए पेंसिल, स्टेटस बदलने के लिए स्टेटस पर टैप, या मिटाने के लिए डस्टबिन इस्तेमाल करें।

> Careful: मिटाने के बाद वापस नहीं लाया जा सकता। जो कंपनी बुकिंग से जुड़ी है, उसे मिटाया नहीं जा सकता।

## आम दिक्कतें

- **"GSTIN is required."** या **"Company Name is required."** (GSTIN / कंपनी का नाम ज़रूरी है) दोनों फ़ील्ड भरें।
- **"At least one Guest House must be selected."** (कम से कम एक गेस्ट हाउस चुनें) कम से कम एक गेस्ट हाउस चुनें।
- **मुझे Add New Company फ़ॉर्म नहीं दिख रहा।** आपका लॉगिन सिर्फ़ यह लिस्ट देख सकता है।
:::
