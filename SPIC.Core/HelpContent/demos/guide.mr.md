---
module: demos
title: पीक प्रात्यक्षिक आणि ट्रीटमेंट
icon: bi-flower1
order: 108
language: mr
category: फील्डचे काम
note: नवीन मार्गदर्शक
meta-source-hash: c6011d305dce737c
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b0aaf57f4576cdfb
confidence: ok

शेतकऱ्याच्या शेतातले पीक प्रात्यक्षिक (डेमो) सुरुवातीपासून शेवटपर्यंत नोंदवा. शेतकरी आणि पिकाचे तपशील एकदाच सेव्ह करा, मग प्रत्येक शेत भेट निरीक्षणे आणि फोटोसह ट्रीटमेंट म्हणून नोंदवा, आणि प्रत्येक भेटीची तुलना आधीच्या भेटीशी करा.

- start | icon: bi-pencil-square | color: #65a30d
  title: डेमो सुरू करा
  text: मंजूर झालेल्या CSR-2 कार्यक्रमासाठी शेतकरी, ठिकाण आणि पिकाचे तपशील भरा.
- treat | icon: bi-camera | color: #d97706
  title: प्रत्येक भेट नोंदवा
  text: प्रॉडक्ट, डोस, निरीक्षणे आणि कंट्रोल व ट्रीटेड फोटो सेव्ह करा.
- compare | icon: bi-arrow-left-right | color: #16a34a
  title: तुलना करा आणि बंद करा
  text: पुढची ट्रीटमेंट नोंदवताना आधीची ट्रीटमेंट पाहा. कोणतीही अडचण उरली नाही की डेमो बंद करा.
:::

::: section flow
title: डेमो कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 672fd04ad6279036
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- csr | type: lane | color: #0891b2
  title: CSR टीम
  sub: CSR-1 आणि CSR-2
- field | type: lane | color: #65a30d | roles: MDO, JMDO
  title: जबाबदार व्यक्ती
  sub: CSR-1 वर नाव असलेले फील्ड कर्मचारी
- eligible | lane: csr | col: 1 | icon: bi-patch-check | color: #64748b | next: start | screen: demo-list
  title: CSR-2 मंजूर
  status: Not Started
  text: CSR-1 मध्ये तुमचे नाव **Responsible Person** म्हणून असते. त्याचा CSR-2 अंतिम मंजूर झाला की डेमो तुमच्या **Demo Documentation** लिस्टमध्ये दिसतो.
- start | lane: field | col: 2 | icon: bi-pencil-square | color: #d97706 | next: treat | screen: start-documentation
  title: डॉक्युमेंटेशन सुरू करा
  status: In Progress
  text: शेतकरी, ठिकाण आणि पिकाचे तपशील भरा आणि **Start Documentation** दाबा.
- treat | lane: field | col: 3 | icon: bi-camera | color: #64748b | next: close, followup | screen: record-treatment
  title: ट्रीटमेंट नोंदवा
  status: Draft
  text: प्रत्येक शेत भेटीत ट्रीटमेंट, निरीक्षणे आणि फोटो नोंदवा. नंतर पूर्ण करायचे असेल तर **Save Draft** दाबा, नाहीतर **Complete Treatment** दाबा.
- followup | lane: field | col: 4 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: treat | to: treat | screen: compare-treatments
  title: अडचण दिसली, परत भेट द्या
  status: In Progress
  text: **Problem Observed?** मध्ये **Yes** ठेवून ट्रीटमेंट पूर्ण केली तर डेमो उघडाच राहतो. पुढच्या भेटीसाठी **Start Next Treatment** दाबा.
- close | lane: field | col: 5 | icon: bi-clipboard-check | color: #16a34a | screen: demo-details
  title: डेमो पूर्ण झाला
  status: Completed
  text: **Problem Observed?** मध्ये **No** निवडून ट्रीटमेंट पूर्ण केली की संपूर्ण डेमो पूर्ण होतो आणि तो फक्त पाहता येतो.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c13ab7e2ba0afb47
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: डेमो पहिल्या भेटीची वाट पाहत आहे. सुरुवातीचा फॉर्म ड्राफ्ट म्हणून सेव्ह केला असेल तरी हेच दिसते.
- in-progress | color: #d97706
  label: In Progress
  text: डॉक्युमेंटेशन सुरू झाले आहे. ट्रीटमेंट नोंदवल्या जात आहेत.
- draft | color: #64748b
  label: Draft
  text: ट्रीटमेंट सेव्ह केली आहे पण पूर्ण केलेली नाही. तुम्ही ती अजून बदलू शकता.
- completed | color: #16a34a
  label: Completed
  text: ट्रीटमेंटसाठी: ती पूर्ण झाली आहे आणि बदलता येत नाही. डेमोसाठी: तो बंद झाला आहे आणि फक्त पाहता येतो.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 94984548531519c1
confidence: ok

तुमचे कार्ड ठळक दिसते. ज्या डेमोमध्ये तुम्ही जबाबदार व्यक्ती आहात, फक्त तेच डेमो तुम्हाला दिसतात.

- field | color: #65a30d | roles: MDO, JMDO
  title: जबाबदार व्यक्ती
  - प्रत्येक मंजूर डेमोचे डॉक्युमेंटेशन सुरू करा
  - प्रत्येक शेत भेट ट्रीटमेंट म्हणून नोंदवा
  - कंट्रोल आणि ट्रीटेड फोटो अपलोड करा
  - कोणतीही अडचण उरली नाही की डेमो बंद करा
- csr | color: #0891b2
  title: CSR टीम
  - CSR-1 तयार करा आणि Responsible Person निवडा
  - CSR-2 मंजूर करा, म्हणजे डेमोचे डॉक्युमेंटेशन करता येईल
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 03c835ce85e68364
confidence: ok

- not-listed | popular: true
  q: माझा डेमो लिस्टमध्ये दिसत नाही
  a: लिस्टमध्ये फक्त असेच कार्यक्रम दिसतात ज्यांचा CSR-2 अंतिम मंजूर झाला आहे आणि CSR-1 वर तुमचे नाव **Responsible Person** म्हणून आहे. ज्यांनी CSR-1 तयार केला त्यांना तो तपासायला सांगा.
- no-next | popular: true
  q: मला Start Next Treatment दिसत नाही
  a: तुमची शेवटची ट्रीटमेंट **Completed** असेल आणि डेमो बंद झालेला नसेल तरच हे दिसते. शेवटची ट्रीटमेंट **Draft** असेल तर ती **View Details** ने उघडा आणि आधी पूर्ण करा.
- closed-early
  q: ट्रीटमेंट पूर्ण केल्यावर डेमो बंद झाला
  a: **Problem Observed?** मध्ये **No** ठेवून ट्रीटमेंट पूर्ण केली की संपूर्ण डेमो पूर्ण होतो. पूर्ण झालेला डेमो फक्त पाहता येतो, म्हणून **Complete Treatment** दाबण्याआधी हे उत्तर तपासा.
- photos
  q: माझे फोटो सेव्ह होत नाहीत
  a: **Save Draft** किंवा **Complete Treatment** दाबल्यावरच फोटो अपलोड होतात. JPG किंवा PNG फाइल वापरा, प्रत्येकी 10 MB किंवा कमी, एका वेळी 20 पर्यंत.
- missing-id
  q: मला "Missing or invalid csr2Id in URL" दिसते
  a: डेमो न निवडता स्क्रीन उघडली गेली. **Demo Documentation** उघडा आणि त्या डेमोच्या **Action** कॉलममधले बटण दाबा.
:::

::: section demo-list
title: माझे डेमो पाहा
routes: /DemoDocumentation
for: 
roles: 
designations: 
minutes: 2
source-hash: 4947901ab12cde8b
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Demo Documentation** स्क्रीनवर तुम्हाला दिलेले प्रत्येक मंजूर CSR-2 कार्यक्रम दिसतात, त्यांचा टप्पा आणि किती ट्रीटमेंट झाल्या ते पण दिसते.

1. वरची कार्डे पाहा: **Total Demonstrations**, **Not Started**, **In Progress** आणि **Completed**.
2. **Filters** खाली **Sanction Number**, **Program Name**, **Product**, **Crop**, **District**, **Stage** किंवा **Status** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
3. सँक्शन नंबर, कार्यक्रम, प्रॉडक्ट, पीक किंवा ठिकाणावरून डेमो शोधण्यासाठी **Search demos...** बॉक्समध्ये टाइप करा.
4. टेबलमध्ये प्रत्येक डेमोसाठी **Current Stage**, **Date of Sowing**, **Treatments**, **Last Treatment** आणि **Document Status** वाचा.
5. **Action** कॉलममध्ये नवीन डेमोसाठी **Start Documentation**, चालू डेमोसाठी **View / Continue**, किंवा पूर्ण झालेल्या डेमोसाठी **View** दाबा.
6. आणखी ओळी पाहण्यासाठी खालचे **Items Per Page** आणि पेज नंबर वापरा.

> Tip: फक्त तुम्हाला दिलेले, मंजूर झालेले CSR-2 कार्यक्रमच लिस्टमध्ये दिसतात.

## नेहमीच्या अडचणी

- **"No eligible demonstration records found".** (योग्य डेमो सापडले नाहीत) अजून कोणत्याही मंजूर CSR-2 मध्ये तुमचे नाव जबाबदार व्यक्ती म्हणून नाही, किंवा तुमचे फिल्टर खूप मर्यादित आहेत. **Clear All** दाबा.
- **"Unable to load approved CSR-2 demonstrations".** (मंजूर CSR-2 डेमो लोड करता आले नाहीत) ॲप सर्व्हरपर्यंत पोहोचू शकले नाही. इंटरनेट तपासा आणि स्क्रीन परत उघडा.
:::

::: section start-documentation
title: डेमोचे डॉक्युमेंटेशन सुरू करा
routes: /StartDocumentation
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-sd-program] | 2 > [data-help=dm-sd-farmer] | 3 > [data-help=dm-sd-crop] | 4 > [data-help=dm-sd-actions]
minutes: 4
source-hash: a86f4d3929f78e21
confidence: ok

**ही स्क्रीन कशासाठी आहे**
पहिल्या भेटीत डेमोचे शेतकरी, ठिकाण आणि पिकाचे तपशील सेव्ह करण्यासाठी **Start Documentation** स्क्रीन वापरा. लिस्टमधून **Start Documentation** दाबून ती उघडा.

1. **Program Details (From CSR-2)** तपासा: **Program Type**, **Program Name**, **Sanction Number**, **Actual Event Location** आणि **Focus Products**.
2. **Farmer & Location Details** खाली **Farmer Name**, **Mobile Number**, **Place / Village** आणि **Farmer Address** भरा. **Crop** आणि **Crop Type** निवडा, **Pin Code** टाका, मग **State** आणि **District** निवडा.
3. **Crop Information** खाली **Date Of Sowing** टाका, **Current Crop Stage** निवडा, **Expected Harvest Date** टाका आणि गरज असेल तर **Remarks** लिहा.
4. नंतर पूर्ण करायचे असेल तर **Save Draft** दाबा, किंवा सुरू करण्यासाठी **Start Documentation** दाबा. **Start Documentation** दाबल्यावर **Demo Details** स्क्रीन उघडते.

> Careful: **Start Documentation** दाबल्यावर लिस्टमधून हा फॉर्म न उघडता **Demo Details** उघडते. सुरू करण्याआधी तपशील नीट तपासा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Farmer Name** | ज्या शेतकऱ्याच्या शेतात डेमो आहे तो शेतकरी. भरणे आवश्यक. |
| **Mobile Number** | शेतकऱ्याचा 10 अंकी मोबाइल नंबर. भरणे आवश्यक. |
| **Crop** | या CSR-2 च्या फोकस प्रॉडक्टमधली पिकेच दिसतात. भरणे आवश्यक. |
| **Crop Type** आणि **Current Crop Stage** | पीक निवडल्यावर लोड होतात. भरणे आवश्यक. |
| **Pin Code** | 6 अंक. भरणे आवश्यक. |
| **State** आणि **District** | आधी State निवडा, मग District. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Select State, District, Crop, Crop Type and Current Crop Stage."** (State, District, Crop, Crop Type आणि Current Crop Stage निवडा) यापैकी एखादी लिस्ट रिकामी आहे. प्रत्येक निवडा.
- **"Enter farmer name, a 10-digit mobile number and a 6-digit PIN code."** (शेतकऱ्याचे नाव, 10 अंकी मोबाइल नंबर आणि 6 अंकी PIN कोड टाका) मोबाइल नंबर आणि PIN कोड तपासा.
- **"This CSR-2 is not finally approved or is outside your permitted scope."** (हा CSR-2 अंतिम मंजूर नाही किंवा तुमच्या परवानगीच्या बाहेर आहे) हा डेमो तुम्हाला दिलेला नाही. लिस्टवर परत जा.
:::

::: section demo-details
title: डेमो आणि त्याच्या ट्रीटमेंट पाहा
routes: /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 7530f32fb5322456
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Demo Details** स्क्रीनवर डेमोचा सारांश, शेतकऱ्याचे तपशील आणि **Treatment Journey** दिसते, म्हणजे नोंदवलेल्या प्रत्येक ट्रीटमेंटची लिस्ट.

1. लिस्टमधून **View / Continue** किंवा **View** दाबून डेमो उघडा.
2. वरचे कार्ड तपासा: प्रॉडक्ट, **Sanction Number**, **Crop**, **Date of Sowing:**, **Crop Stage:** आणि **Expected Harvest:**.
3. शेतकऱ्याचे कार्ड तपासा: **Farmer**, **Mobile Number**, **District**, **State**, **Pin Code** आणि **Location**.
4. अजून एकही ट्रीटमेंट नोंदवली नसेल तर **Start First Treatment** दाबा.
5. **Treatment Journey** मध्ये प्रत्येक ट्रीटमेंटची स्थिती, **Visit Date**, **Plant Stage**, **Dosage** आणि **Remarks** दिसतात. **Treatments Done** मध्ये पूर्ण झालेल्या ट्रीटमेंटची संख्या दिसते.
6. ट्रीटमेंट उघडण्यासाठी तिच्यावरचे **View Details** दाबा.
7. शेवटची ट्रीटमेंट **Completed** झाली की पुढच्या भेटीसाठी **Start Next Treatment** दाबा.

## नेहमीच्या अडचणी

- **Start Next Treatment बटण दिसत नाही.** शेवटची ट्रीटमेंट अजून **Draft** आहे, किंवा डेमो **Completed** झाला आहे.
- **"Farmer and crop documentation is not available yet."** (शेतकरी आणि पिकाचे डॉक्युमेंटेशन अजून उपलब्ध नाही) सुरुवातीचा फॉर्म सुरू केलेला नाही. लिस्टमधून डेमो उघडा आणि **Start Documentation** दाबा.
- **"CSR-2 not found, not finally approved, or outside your permitted scope."** (CSR-2 सापडला नाही, अंतिम मंजूर नाही, किंवा तुमच्या परवानगीच्या बाहेर आहे) हा डेमो तुम्हाला दिलेला नाही.
:::

::: section record-treatment
title: शेत भेटीत ट्रीटमेंट नोंदवा
routes: /TreatmentDetails
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-td-visit] | 2 > [data-help=dm-td-treatment] | 3 > [data-help=dm-td-observations] | 4 > [data-help=dm-td-images] | 5 > [data-help=dm-td-problem] | 6 > [data-help=dm-td-actions]
minutes: 5
source-hash: d955aff37a8e757f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
प्रत्येक शेत भेटीत काय वापरले, तुम्हाला काय दिसले, आणि कंट्रोल व ट्रीटेड झाडांचे फोटो नोंदवण्यासाठी ट्रीटमेंट स्क्रीन (उदा. **Treatment 01**) वापरा. ती **Start First Treatment**, **Start Next Treatment** किंवा **View Details** ने उघडा.

1. **Visit Details** खाली **Visit Date** आणि **Field Officer / Visibility** तपासा, मग **Current Plant Stage** आणि **Plant Condition** निवडा.
2. **Treatment Applied** खाली **Product Used** निवडा, **Dosage** टाका, **Application Method** निवडा, **Treatment Date** टाका, **Stage of Application** निवडा आणि **Remarks** लिहा.
3. **Observations** खाली **Physical Parameters** आणि **Yield Parameters** लिहा, लागू असलेली **Quick Observations** टॅप करा, आणि **Observation Notes** लिहा.
4. **Control Images** आणि **Treated Images** खाली **Upload Image** दाबा आणि फोटो निवडा. कंट्रोल फोटोमध्ये प्रॉडक्ट न वापरलेली झाडे असतात, ट्रीटेड फोटोमध्ये प्रॉडक्ट वापरलेली झाडे असतात.
5. **Problem Observed?** खाली **Yes** किंवा **No** निवडा. **Yes** असेल तर **Problem Category**, **Problem Description**, **Severity**, **Recommendation / Monitoring Action** आणि **Action Taken** भरा.
6. नंतर पूर्ण करायचे असेल तर **Save Draft** दाबा, किंवा भेट संपल्यावर **Complete Treatment** दाबा.

> Careful: **Problem Observed?** सुरुवातीला **Yes** वर असते. **No** ठेवून ट्रीटमेंट पूर्ण केली तर संपूर्ण डेमो पूर्ण होतो आणि आणखी ट्रीटमेंट जोडता येत नाहीत.

> Tip: दुसऱ्या ट्रीटमेंटपासून वरच्या **Previous Treatment Reference** बॉक्समध्ये मागची भेट दिसते, त्यामुळे तुम्ही तुलना करू शकता.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Visit Date** आणि **Treatment Date** | आपोआप आजची तारीख असते. ट्रीटमेंटची तारीख भेटीच्या तारखेच्या आधीची असू शकत नाही. पूर्ण करण्यासाठी आवश्यक. |
| **Current Plant Stage** | आज दिसलेली पिकाची अवस्था. पूर्ण करण्यासाठी आवश्यक. |
| **Product Used** | वापरलेले प्रॉडक्ट. पूर्ण करण्यासाठी आवश्यक. |
| **Dosage** | उदा. 2 ml/L. |
| **Control Images**, **Treated Images** | JPG किंवा PNG, प्रत्येकी 10 MB किंवा कमी, एका वेळी 20 पर्यंत. |

## नेहमीच्या अडचणी

- **"Enter Visit Date, Treatment Date, Current Plant Stage and Product Used before completing the treatment."** (ट्रीटमेंट पूर्ण करण्याआधी Visit Date, Treatment Date, Current Plant Stage आणि Product Used भरा) ही चार फील्ड भरा, मग परत **Complete Treatment** दाबा.
- **"Treatment date cannot precede visit date."** (ट्रीटमेंटची तारीख भेटीच्या तारखेच्या आधीची असू शकत नाही) **Treatment Date** भेटीच्या तारखेला किंवा नंतरची करा.
- **"Only JPG and PNG images are allowed."** (फक्त JPG आणि PNG फोटो चालतात) किंवा **"Each image must be 10MB or less."** (प्रत्येक फोटो 10MB किंवा कमी असावा) दुसरे फोटो निवडा किंवा ते लहान करा.
- **"This treatment has already been completed."** (ही ट्रीटमेंट आधीच पूर्ण झाली आहे) पूर्ण झालेली ट्रीटमेंट फक्त पाहता येते.
:::

::: section compare-treatments
title: ट्रीटमेंटची तुलना करा आणि पाठपुरावा करा
routes: /TreatmentDetails, /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 393196d831ee40ab
confidence: ok

**ही स्क्रीन कशासाठी आहे**
अडचण दिसली की डेमो उघडाच राहतो आणि तुम्ही परत शेताला भेट देता. प्रत्येक नवीन ट्रीटमेंटमध्ये आधीची ट्रीटमेंट दिसते, त्यामुळे पीक सुधारले का ते तुम्हाला कळते.

1. **Demo Details** वर **Start Next Treatment** दाबा. पुढच्या नंबरची ट्रीटमेंट उघडते.
2. **Previous Treatment Reference** बॉक्स वाचा: **Visit Date**, **Current Plant Stage**, **Dosage Applied**, **Method**, **Field Observation** आणि **Yield Parameters**.
3. आधीची ट्रीटमेंट तिच्या फोटोसह उघडण्यासाठी **View Full Details** दाबा.
4. आजची भेट आणि फोटो त्याच पद्धतीने नोंदवा, आणि काय बदलले ते **Observation Notes** मध्ये लिहा.
5. **Treatment Journey** मध्ये सगळ्या ट्रीटमेंट पाहण्यासाठी **Back to Demo Details** दाबा.

## नेहमीच्या अडचणी

- **Previous Treatment Reference बॉक्स दिसत नाही.** तो फक्त दुसऱ्या ट्रीटमेंटपासून पुढे दिसतो.
- **मला जुनी ट्रीटमेंट बदलायची आहे.** **Completed** ट्रीटमेंट बदलता येत नाही. दुरुस्ती पुढच्या ट्रीटमेंटमध्ये लिहा.
:::

::: section sample-screens
title: ट्रीटमेंटच्या नमुना स्क्रीन
routes: /TreatmentDemoDetails, /Treatment01, /Treatment02
for: 
roles: 
designations: 
minutes: 1
source-hash: 13af1fa77b22fd32
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Treatment Demo Details**, **Treatment 01** आणि **Treatment 02** हे मेन्यू ठरलेल्या उदाहरणाच्या माहितीसह नमुना स्क्रीन उघडतात. डेमो कसा दिसतो ते त्यात दिसते. तिथे तुम्ही जे टाइप करता ते सेव्ह होत नाही.

1. **Treatment Demo Details** मध्ये तीन ट्रीटमेंटच्या **Treatment Journey** सह **Demo Details** स्क्रीनचे उदाहरण दिसते.
2. **Treatment 01** मध्ये पूर्ण झालेल्या ट्रीटमेंटचे उदाहरण दिसते, त्यात **Visit Details**, **Treatment Applied**, **Observations**, फोटो आणि **Problem Identification & Recommendation** असतात.
3. **Treatment 02** मध्ये दुसऱ्या ट्रीटमेंटचे उदाहरण दिसते, वर **Previous Treatment Reference - Treatment 01** बॉक्स असतो.
4. खरे काम नोंदवण्यासाठी **Demo Documentation** उघडा आणि तुमचा डेमो निवडा.

> Careful: या नमुना स्क्रीनवरची बटणे काहीही सेव्ह करत नाहीत.

## नेहमीच्या अडचणी

- **मी भरलेली माहिती गायब झाली.** या नमुना स्क्रीन आहेत. खरा डेमो नोंदवण्यासाठी **Demo Documentation** वापरा.
:::
