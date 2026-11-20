---
module: sas-sample-collection
title: मातीचे नमुने गोळा करणे
icon: bi-droplet
order: 50
language: mr
category: माती परीक्षण
note: नमुन्याचा प्रवास, स्थिती आणि मार्गदर्शक टूरसह नवीन रूप
meta-source-hash: 91cc9c6288b84a36
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 722480765e5f4cad
confidence: ok

SPIC शेतकऱ्याच्या शेतातली माती आणि पाणी जिल्ह्याच्या लॅबमध्ये तपासते. ॲप प्रत्येक नमुन्याचा मागोवा शेतापासून शेतकऱ्याच्या रिपोर्टपर्यंत ठेवते, त्यामुळे नमुना कुठे आहे हे सगळ्यांना दिसते.

- collect | icon: bi-droplet | color: #0d9488
  title: शेतात नमुना गोळा करा
  text: MDO किंवा JMDO शेतकरी आणि नमुन्यांची नोंद करतात.
- pay | icon: bi-qr-code | color: #d97706
  title: पैसे भरणे आणि मंजुरी
  text: पैसे घेण्याच्या नमुन्याचे पैसे UPI किंवा दुसऱ्या पद्धतीने भरले जातात. मग त्याला मंजुरी मिळते आणि Finance ते तपासते.
- report | icon: bi-file-earmark-text | color: #7c3aed
  title: शेतकऱ्यासाठी लॅब रिपोर्ट
  text: लॅब नमुने तपासते आणि शेतकरी रिपोर्ट तमिळ किंवा इंग्रजीमध्ये डाउनलोड करतो.
:::

::: section flow
title: नमुना कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 8e557635907c2edf
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. मोफत नमुन्यासाठी पैसे भरण्याचे टप्पे नसतात. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- officer | type: lane | color: #0d9488 | roles: MDO, JMDO
  title: फील्ड ऑफिसर
  sub: MDO · JMDO
- approver | type: lane | color: #ea580c | roles: Admin, CorporateAdmin
  title: मंजुरी देणारे
  sub: पेमेंटची मंजुरी
- finance | type: lane | color: #2563eb
  title: Finance
  sub: पेमेंटची खात्री
- lab | type: lane | color: #7c3aed
  title: लॅब
  sub: जिल्ह्याची लॅब
- farmer | type: lane | color: #16a34a | roles: Farmer
  title: शेतकरी
  sub: नमुन्याचे मालक
- collect | lane: officer | col: 1 | icon: bi-droplet | color: #64748b | next: pay | screen: collect-sample
  title: नमुने गोळा करा
  status: Draft
  text: शेतकरी निवडा, मातीचा किंवा पाण्याचा प्रत्येक नमुना जोडा आणि सेव्ह करा. मोफत नमुना थेट consignment कडे जातो.
- pay | lane: officer | col: 2 | icon: bi-qr-code | color: #d97706 | next: approve | screen: record-payment
  title: पेमेंटची नोंद करा
  status: Pending Payment
  text: शेतकरी QR कोड स्कॅन करून पैसे भरतो. ऑफिसर व्यवहाराचा तपशील टाकतात आणि पावती अपलोड करतात.
- approve | lane: approver | col: 3 | icon: bi-patch-check | color: #ea580c | next: send, verify | return: pay | screen: approve-payments
  title: पेमेंट मंजूर करा
  status: Pending Approval
  text: मंजुरी देणारे रक्कम आणि पावती तपासतात, मग मंजूर करून Finance कडे पाठवतात, किंवा परत पाठवतात.
- send | lane: officer | col: 4 | icon: bi-truck | color: #0d9488 | next: test | screen: send-consignment
  title: लॅबला पाठवा
  status: Dispatched
  text: ऑफिसर नमुने कुरिअरकडे देतात आणि ट्रॅकिंग नंबरची नोंद करतात. याला consignment (पार्सल) म्हणतात.
- verify | lane: finance | col: 4 | icon: bi-bank | color: #2563eb | screen: verify-payments
  title: पैसे आले का ते तपासा
  status: Finance Verified
  text: Finance पैसे खात्यात आले याची खात्री करते, किंवा रक्कम जुळत नाही (mismatch) अशी नोंद करते.
- test | lane: lab | col: 5 | icon: bi-eyedropper | color: #7c3aed | next: report
  title: नमुने तपासा
  status: In Progress
  text: लॅबला पार्सल मिळते, लॅब प्रत्येक नमुना तपासते आणि निकाल प्रसिद्ध करते.
- report | lane: farmer | col: 6 | icon: bi-file-earmark-arrow-down | color: #16a34a | screen: farmer-my-reports
  title: रिपोर्ट डाउनलोड करा
  status: Completed
  text: शेतकऱ्याला ॲपमध्ये रिपोर्ट दिसतो आणि तो तमिळ किंवा इंग्रजीमध्ये डाउनलोड करता येतो.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 0771c49c0ffc8c78
confidence: ok

- draft | color: #64748b
  label: Draft
  text: सेव्ह केले आहे, अजून सबमिट केलेले नाही.
- pending-payment | color: #d97706
  label: Pending Payment
  text: पेमेंटची नोंद होण्याची वाट पाहत आहे.
- pending-approval | color: #ea580c
  label: Pending Approval
  text: पेमेंटची नोंद झाली आहे, मंजुरीची वाट पाहत आहे.
- ready | color: #0891b2
  label: Ready consignment
  text: लॅबला पाठवायला तयार.
- dispatched | color: #0d9488
  label: Dispatched
  text: कुरिअरकडे दिले आहे.
- in-transit | color: #0d9488
  label: In Transit
  text: लॅबकडे जात आहे.
- delivered | color: #2563eb
  label: Delivered to Lab
  text: पार्सल लॅबला मिळाले आहे.
- in-progress | color: #7c3aed
  label: In Progress
  text: लॅबमध्ये तपासणी चालू आहे.
- completed | color: #16a34a
  label: Completed
  text: निकाल उपलब्ध आहेत.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 9c070a1e6fc377cf
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- officer | color: #0d9488 | roles: MDO, JMDO
  title: MDO, JMDO
  - नमुने गोळा करा
  - पेमेंटची नोंद करा
  - consignment लॅबला पाठवा
- approver | color: #ea580c | roles: Admin, CorporateAdmin
  title: मंजुरी देणारे
  - पेमेंट मंजूर करा किंवा परत पाठवा
  - consignment पुढे न्या
  - लॅबचे निकाल भरा
- finance | color: #2563eb
  title: Finance
  - मिळालेल्या पैशांची खात्री करा
  - न जुळणाऱ्या रकमेची (mismatch) नोंद करा
- farmer | color: #16a34a | roles: Farmer
  title: शेतकरी
  - तुमचे नमुने पाहा
  - तुमचे रिपोर्ट डाउनलोड करा
  - तुमचे पेमेंट तपासा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: b121292d9fb622a2
confidence: ok

- no-menu | popular: true
  q: मला मेनूमध्ये Sample Collection दिसत नाही
  a: तुमच्या पदामध्ये (designation) हे पेज नाही. तुमच्या ॲडमिनिस्ट्रेटरला विचारा. JMDO वापरकर्ते त्याऐवजी **Soil Sample** टास्कमधून ते उघडतात.
- payment-rejected | popular: true
  q: माझे पेमेंट नाकारले गेले. आता काय करू?
  a: तपशील पेजवर कारणासह **Payment rejected** दिसते. ते दुरुस्त करा आणि परत **Record payment** टॅप करा.
- not-ready
  q: "This collection is not ready to send" (हे संकलन पाठवायला तयार नाही) असे दिसते
  a: पेमेंटची अजून नोंद झालेली नाही किंवा ते मंजूर झालेले नाही. मोफत नमुने लगेच पाठवता येतात.
- report-language
  q: मला रिपोर्ट तमिळमध्ये कसा मिळेल?
  a: **Download PDF** च्या बाजूचा छोटा बाण टॅप करा आणि तमिळ किंवा इंग्रजी निवडा.
:::

::: section collection-list
title: तुमची नमुना संकलने शोधा आणि त्यांचा मागोवा घ्या
routes: /SampleCollection
for: 
roles: 
designations: 
screenshot: collection-list.png
hotspots: 1 @ 2.2,68.5,95.5,15.1 | 2 @ 73.7,88,22.4,4.8 | 3 @ 2.2,30.7,95.5,35.4 | 4 @ 3.8,94.7,92.3,5.3 | 5 @ 62.1,1.9,8.1,4.8 | 6 @ 79.2,1.9,18.5,4.8
screenshot-phone: collection-list.phone.jpg
hotspots-phone: 1 @ 3.1,40.6,93.8,24.4 | 2 @ 6.4,69.4,87.2,3.2 | 3 @ 3.1,36,93.8,3.2 | 4 @ 2,2,8,4 | 5 @ 15.3,4.3,10.2,3 | 6 @ 39.7,4.3,48.8,3
tour: 1 > [data-help=sc-tiles] | 2 > [data-help=sc-search] | 3 > [data-help=sc-filters] | 4 > [data-help=sc-table] | 5 > [data-help=sc-excel] | 6 > [data-help=sc-new]
minutes: 2
source-hash: 01315bd944ae112a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sample Collection** पेजवर तुम्हाला दिसणारी सगळी संकलने, त्यांच्या पेमेंट आणि लॅबच्या स्थितीसह दिसतात. संकलन शोधण्यासाठी आणि त्याचा तपशील उघडण्यासाठी हे पेज वापरा.

वरच्या कार्डांवर आकडे दिसतात: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level** आणि **Ready to Submit**.

1. **Pending Payment** किंवा **Draft** सारखी रंगीत टाइल टॅप केली की फक्त तीच संकलने दिसतात. सगळी पाहण्यासाठी ती परत टॅप करा.
2. शोधण्यासाठी **Search collections...** मध्ये शेतकऱ्याचे नाव किंवा collection ID टाइप करा.
3. आणखी पर्यायांसाठी **Filters** उघडा, **Date Range**, **Location**, **Payment Type**, **Payment Status** किंवा **Status** निवडा, मग **Apply Filters** टॅप करा. ते काढण्यासाठी **Clear All** टॅप करा.
4. संकलन उघडण्यासाठी डोळ्याचा आयकॉन (फोनवर कार्ड) टॅप करा.
5. स्क्रीनवर दिसणारी लिस्ट डाउनलोड करण्यासाठी **Excel** किंवा **PDF** टॅप करा. नवीन स्थिती पाहण्यासाठी **Refresh Data** टॅप करा.
6. नमुने गोळा करायला सुरुवात करण्यासाठी **New Sample Collection** टॅप करा.

## नेहमीच्या अडचणी

- **No sample collections found**: फिल्टर काढा, किंवा तारखांची रेंज तपासा.
- **Collections could not be loaded**: तुमचे इंटरनेट तपासा आणि **Try again** टॅप करा.
- **New Sample Collection** बटण दिसत नाही: तुमच्या अकाउंटवरून संकलन तयार करता येत नाही. फक्त MDO, JMDO आणि ज्यांच्या पदाला परवानगी आहे अशी ॲडमिनिस्ट्रेटर अकाउंटच नमुने गोळा करू शकतात.
- डाउनलोडमध्ये काहीच ओळी आहेत: **Excel** आणि **PDF** फक्त सध्याचे पेज सेव्ह करतात. जास्त ओळी घेण्यासाठी **Items Per Page** बदला.
:::

::: section collect-sample
title: मातीचा किंवा पाण्याचा नवीन नमुना गोळा करा
routes: /SampleCollection/new
for: SampleCollection.Entry
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
screenshot: collect-sample.png
hotspots: 1 @ 66,1.9,31.8,5 | 2 @ 4,34.7,29.7,7.8 | 3 @ 35.2,81.7,29.7,8 | 4 @ 78.7,100,17.2,0 | 5 @ 81.8,100,16,0
screenshot-phone: collect-sample.phone.jpg
hotspots-phone: 1 @ 3.1,4.6,93.8,3.2 | 2 @ 6.9,33.7,86.2,8.5 | 3 @ 6.9,100,86.2,0 | 4 @ 6.9,100,86.2,0 | 5 @ 43.8,92.1,52.1,3
tour: 1 > [data-help=sc-paytype] | 2 > [data-help=sc-farmer] | 3 > [data-help=sc-soil] | 4 > [data-help=sc-add] | 5 > [data-help=sc-save]
minutes: 4
source-hash: 3ae93abc0337f008
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुम्ही शेतकऱ्यांकडून घेतलेल्या नमुन्यांची नोंद करण्यासाठी **New Sample Collection** वापरा. एका संकलनात अनेक शेतकऱ्यांचे नमुने असू शकतात.

**Sample Collection** पेजवर **New Sample Collection** टॅप करून ही स्क्रीन उघडा. JMDO: **Tasks Allocation** मधल्या **Soil Sample** कार्डवर **Start** टॅप करा, किंवा टास्कच्या **Quick actions** मधले **Soil Sample Collection** टॅप करा.

1. वर **Free Sample** किंवा **Paid Sample** निवडा.
2. **Farmer Name** मध्ये किमान दोन अक्षरे टाइप करा आणि शेतकरी निवडा. शेतकरी नवीन असेल तर **New Farmer** टॅप करा. पैसे घेण्याच्या नमुन्यासाठी **Paid Sample Category** खाली **Farmer** किंवा **NGO** निवडा.
3. **Soil Sample** आणि **Water Sample** साठी **Yes** किंवा **No** निवडा. **Crop 1**, **Crop 2** निवडा आणि गरज असेल तर **Remarks** टाका.
4. **Add Another Sample** टॅप करा. नमुना त्याच्या रकमेसह **Sample Added (Preview)** मध्ये दिसतो. प्रत्येक नमुन्यासाठी हेच करा; ओळ बदलण्यासाठी पेन्सिल, आणि काढण्यासाठी डस्टबिन वापरा.
5. नंतर पूर्ण करायचे असेल तर **Save Draft** टॅप करा, किंवा **Save and Continue** टॅप करा. **Sample Summary** तपासा आणि **Continue to payment** (पैसे घेण्याचा नमुना) किंवा **Continue to consignment** (मोफत नमुना) टॅप करा.

> Tip: एका संकलनात अनेक शेतकऱ्यांचे नमुने असू शकतात.

## फील्डची माहिती

- **Collection ID**: तुम्ही सेव्ह केल्यावर ॲप तयार करते.
- **Collected By**: तुमचे नाव, आपोआप भरलेले.
- **Total Amount**: पैसे घेण्याच्या सगळ्या नमुन्यांची एकूण रक्कम. मोफत संकलनावर **Free** दिसते.
- **New Farmer**: यासाठी **Farmer Name** आणि 10 अंकी **Mobile Number** लागतो. **Use my location** टॅप केल्यावर **Latitude** आणि **Longitude** भरले जातात.

## नेहमीच्या अडचणी

- "Choose a farmer, or add a new one" (शेतकरी निवडा किंवा नवीन जोडा): फक्त नाव टाइप करू नका, लिस्टमधून नाव निवडा.
- "Choose at least one of Soil Sample or Water Sample" (Soil Sample किंवा Water Sample पैकी किमान एक निवडा): त्यापैकी एक **Yes** करा.
- शेतकऱ्याचा पत्ता दुरुस्त करण्यासाठी **Edit farmer** टॅप करा, तो बदला, मग **Save farmer details** टॅप करा.
:::

::: section edit-draft
title: ड्राफ्ट संकलन पूर्ण करा किंवा बदला
routes: /SampleCollection/new, /SampleCollection/view/{id}
for: SampleCollection.Update
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 2
source-hash: 2699d934a7f0674d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Save Draft** ने सेव्ह केलेले संकलन **Draft** म्हणूनच राहते. तुम्ही सबमिट करेपर्यंत काहीही पेमेंटला किंवा लॅबला जात नाही. तुम्ही ते परत उघडून पूर्ण करू शकता.

1. **Sample Collection** उघडा आणि **Draft** टाइल टॅप करा.
2. डोळ्याच्या आयकॉनने संकलन उघडा.
3. **Sample Collection Details** च्या वर **Edit** टॅप करा.
4. नवीन संकलनासारखेच नमुने जोडा, बदला किंवा काढा.
5. परत ड्राफ्ट म्हणून ठेवण्यासाठी **Save Draft** टॅप करा, किंवा **Save and Continue** टॅप करा.
6. **Sample Summary** मध्ये नमुने तपासा आणि **Continue to payment** किंवा **Continue to consignment** टॅप करा.

> सबमिट केल्यानंतर संकलन बदलता येत नाही.

## नेहमीच्या अडचणी

- **Edit** बटण दिसत नाही: संकलन आधीच सबमिट झाले आहे, किंवा तुमच्या पदाला बदल करण्याची परवानगी नाही.
- "This collection has already been submitted" (हे संकलन आधीच सबमिट झाले आहे): ड्राफ्ट आधीच सबमिट झाला होता. स्थिती पाहण्यासाठी त्याचा तपशील उघडा.
- "That draft no longer exists" (तो ड्राफ्ट आता नाही): तो काढून टाकला गेला आहे. नवीन संकलन सुरू करा.
:::

::: section record-payment
title: पैसे घेण्याच्या नमुन्याच्या पेमेंटची नोंद करा
routes: /SampleCollection/payment/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: c5b827bf8559be23
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payments** स्क्रीनवर किती पैसे घ्यायचे ते दिसते, शेतकऱ्याला स्कॅन करण्यासाठी UPI QR कोड दिसतो, आणि पेमेंटचा तपशील व पावतीसाठी फॉर्म असतो.

1. **Continue to payment** नंतर **Payments** स्क्रीन उघडते. संकलनाच्या तपशीलावर **Record payment** टॅप करूनही ती उघडता येते.
2. **Total Amount to Collect** तपासा. नमुने पाहण्यासाठी **View Sample Summary** टॅप करा.
3. शेतकऱ्याला **Scan & Pay** खालचा QR कोड कोणत्याही UPI ॲपने स्कॅन करायला सांगा. तो सेव्ह करण्यासाठी **Download QR Code** टॅप करा.
4. **Payment Information** भरा.
5. **Payment Proof** खाली **Click to Upload or Drag and Drop** टॅप करा आणि पावती किंवा स्क्रीनशॉट निवडा.
6. **Save Payment** टॅप करा. आता पेमेंट मंजुरीची वाट पाहते.

## फील्डची माहिती

- **Transaction ID** (आवश्यक): UPI व्यवहाराचा नंबर.
- **Paid By** (आवश्यक): कोणी पैसे भरले. संकलनातल्या शेतकऱ्यांची नावे पर्याय म्हणून दिसतात.
- **Payment Mode**: **UPI**, **Bank Transfer** किंवा **Cash**.
- **Transaction Date**, **Bank / Gateway**, **Payment Gateway / UTR No.**, **Bank Name**, **Contact Number**, **Email (optional)**: तुमच्याकडे असेल ती माहिती भरा.
- **Remarks**: मंजुरी देणाऱ्यासाठी नोंद.
- **Payment Proof**: PNG किंवा JPG फोटो, 5 MB पर्यंत.

## नेहमीच्या अडचणी

- "Please fill in the Transaction ID and Paid By" (Transaction ID आणि Paid By भरा): दोन्ही भरणे आवश्यक आहे.
- "Pick a PNG or JPG image of 5 MB or less" (5 MB किंवा त्यापेक्षा लहान PNG किंवा JPG फोटो निवडा): लहान फोटो किंवा स्क्रीनशॉट घ्या.
- **Save Draft** टाइप केलेला तपशील फक्त याच फोनवर ठेवते. शेवटी **Save Payment** करा.
- "Payment already recorded" (पेमेंटची नोंद आधीच झाली आहे): ते मंजुरीची वाट पाहत आहे.
- पेमेंट नाकारले गेले तर तपशील पेजवर कारणासह **Payment rejected** दिसते. ते दुरुस्त करा आणि परत **Record payment** टॅप करा.
:::

::: section send-consignment
title: नमुने लॅबला पाठवा (consignment)
routes: /SampleCollection/consignment/{id}, /SampleCollection/submitted/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: de0b21e8f58ece6a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
संकलन मोफत असेल किंवा त्याचे पेमेंट मंजूर झाले असेल, तेव्हा तुम्ही नमुने पॅक करून कुरिअरकडे देता. **Upload Consignment** कुरिअरचा तपशील आणि फोटो नोंदवते, त्यामुळे लॅबला काय येत आहे ते कळते.

1. संकलन उघडा आणि **Upload consignment** टॅप करा. मोफत नमुन्यासाठी **Continue to consignment** ने ते थेट उघडते.
2. **Courier Details** खाली **Courier Service** निवडा आणि **Tracking Number (AWB)** टाइप करा.
3. पाठवायला तयार असलेली इतर संकलने याच पार्सलमध्ये पाठवायची असतील तर **Include other ready collections** उघडा आणि त्यांच्यावर टिक करा. वरचे **Both**, **Free Sample** आणि **Paid Sample** यानुसार कोणती संकलने दिसतील ते बदलते.
4. **Upload Photos and Documents** खाली **Shipping Label / Waybill** मध्ये फोटो जोडा. फोनवर तुम्ही **Take a photo** टॅप करू शकता.
5. हवे असल्यास **Package and contact details** उघडा.
6. **Submit Consignment** टॅप करा.
7. **Consignment Submitted Successfully!** वर ट्रॅकिंग नंबर दिसतो. तो कॉपी करण्यासाठी कॉपी आयकॉन टॅप करा, तपशील पाहण्यासाठी **View Consignment** टॅप करा, किंवा **Go to Dashboard** टॅप करा.

## फील्डची माहिती

- **Tracking Link (Optional)**, **Expected Delivery Date**: यामुळे लॅबला पार्सलचा मागोवा घेता येतो.
- **Package Photo**, **Any other Document**: हवे असल्यास फोटो, PNG किंवा JPG, प्रत्येकी 5 MB पर्यंत.
- **Packaging Type**, **No. of Packages**, **Package Weight (kg)**, **Contact Person**, **Mobile**, **Email**, **Collection Notes**: हवे असल्यास भरा.

## नेहमीच्या अडचणी

- "Please fill the courier details and add the shipping label photo" (कुरिअरचा तपशील भरा आणि शिपिंग लेबलचा फोटो जोडा): तिन्ही गोष्टी आवश्यक आहेत.
- "This collection is not ready to send" (हे संकलन पाठवायला तयार नाही): पेमेंटची अजून नोंद झालेली नाही किंवा ते मंजूर झालेले नाही.
- **Save Draft** फक्त टाइप केलेली फील्ड ठेवते. फोटो नंतर परत जोडा.
- JMDO: consignment सबमिट केल्यावर आजचा **Soil Sample** टास्क पूर्ण झाला अशी नोंद होते.
:::

::: section collection-details
title: नमुना संकलनाचा तपशील वाचा
routes: /SampleCollection/view/{id}
for: 
roles: 
designations: 
screenshot: collection-details.png
screenshot-phone: collection-details.phone.jpg
minutes: 2
source-hash: 0b6c9585e9bd5ccc
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sample Collection Details** मध्ये एका संकलनाची सगळी माहिती दिसते: पेमेंट, कुरिअर, लॅबचे निकाल आणि इतिहास. फील्ड कर्मचारी, डीलर आणि मॅनेजर इथे प्रगती पाहू शकतात.

1. **Sample Collection** उघडा आणि संकलनावरचा डोळ्याचा आयकॉन टॅप करा.
2. वरच्या ओळीत collection ID, **Paid Sample** किंवा **Free Sample**, आणि सध्याची स्थिती दिसते.
3. पट्टीवर **Consignment ID**, **Transaction ID**, **Courier Partner**, **Samples**, **No. of Tests** आणि **Payment Status** दिसतात. consignment उघडण्यासाठी consignment ID टॅप करा.
4. **Collection Information** आणि **Sample Summary** मध्ये नमुने कोणी गोळा केले आणि प्रत्येक नमुन्यासाठी किती तपासण्या लागतात ते दिसते.
5. लॅबचे आकडे तयार झाल्यावर **Result Comparison** मध्ये दिसतात. अनेक नमुने असतील तर एखाद्या नमुन्याचे निकाल पाहण्यासाठी वर तो नमुना टॅप करा.
6. **Consignments Details**, **Amount Details** आणि **History / Timeline** मध्ये कुरिअर, रक्कम आणि आतापर्यंतचा प्रत्येक टप्पा दिसतो.
7. प्रिंट किंवा डाउनलोड करण्यासाठी **Export** टॅप करा.

## तुम्हाला दिसू शकणारी बटणे

- **Edit**: संकलन अजून ड्राफ्ट आहे.
- **Record payment**: पेमेंट अजून बाकी आहे.
- **Upload consignment**: लॅबला पाठवायला तयार आहे.

## नेहमीच्या अडचणी

- "The lab has not published the results yet" (लॅबने अजून निकाल दिलेले नाहीत): नमुना अजून रस्त्यात आहे किंवा त्याची तपासणी चालू आहे.
- consignment खाली **Not dispatched yet**: नमुने अजून पाठवलेले नाहीत.
- "Collection not found" (संकलन सापडले नाही): ते काढून टाकले गेले आहे, किंवा तुम्हाला ते पाहण्याची परवानगी नाही.
:::

::: section admin-review
title: पेमेंट मंजूर करा, consignment पुढे न्या आणि निकाल भरा
routes: /SampleCollection/view/{id}, /ConsignmentHistory/view/{id}
for: 
roles: Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: 16838640fb4dadb0
confidence: ok

**ही स्क्रीन कशासाठी आहे**
ॲडमिनिस्ट्रेटर संकलनाच्या तपशील पेजवर पेमेंट तपासू शकतात, consignment पुढच्या टप्प्यावर नेऊ शकतात, आणि लॅबचे निकाल टाइप करू शकतात.

### संकलनावरचे पेमेंट मंजूर करा

1. **Pending Approval** स्थितीतले संकलन उघडा.
2. **Payment Approval** मध्ये **Amount**, **Transaction ID** आणि पावती जुळवून पाहा. पावती मोठी करून पाहण्यासाठी **View proof** टॅप करा.
3. **Approve payment** टॅप करा आणि खात्री करा. संकलन पाठवायला तयार होते.
4. नाकारण्यासाठी **Reject** टॅप करा, **Reason for rejection** लिहा, मग **Reject payment** टॅप करा.

### consignment पुढे न्या

1. **Consignment History** उघडा आणि एक consignment उघडा.
2. **Update status** खाली पुढचा टप्पा टॅप करा, जसे **In Transit**, **Delivered** किंवा **Completed**.

> consignment पुढे नेल्यावर त्यातले सगळे नमुनेही पुढे जातात.

### लॅबचे निकाल भरा

1. संकलनाच्या तपशीलावर **Result Comparison** वर जा, नमुना निवडा आणि **Enter results** (किंवा **Edit results**) टॅप करा.
2. **Test Parameter**, **Entered Value**, **Unit**, **Normal Range** आणि **Result** भरा. जास्त ओळींसाठी **Add parameter** टॅप करा.
3. **Save results** टॅप करा.

## नेहमीच्या अडचणी

- "Add a reason so the collector knows what to correct" (कारण लिहा, म्हणजे नमुना गोळा करणाऱ्याला काय दुरुस्त करायचे ते कळेल): नाकारण्यासाठी कारण लागते.
- **Update status** दिसत नाही: consignment आधीच **Completed** झाले आहे.
:::

::: section consignment-history
title: लॅबला पाठवलेल्या consignment चा मागोवा घ्या
routes: /ConsignmentHistory, /ConsignmentHistory/view/{id}
for: 
roles: 
designations: 
screenshot: consignment-history.png
screenshot-phone: consignment-history.phone.jpg
minutes: 2
source-hash: c4455c0c6e2c1ab2
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Consignment History** मध्ये लॅबला पाठवलेले नमुन्यांचे प्रत्येक पार्सल, कुरिअर, ट्रॅकिंग नंबर आणि सध्याच्या स्थितीसह दिसते.

1. मेनूमधून **Consignment History** उघडा.
2. कार्डांवर **Total Consignments**, **Total Consignment Sent**, **Total Samples**, **Delivered** आणि **Pending** दिसतात.
3. एखादे शोधण्यासाठी **Search consignment, AWB, transaction...** मध्ये टाइप करा.
4. **Filters** उघडून **From**, **To**, **Status**, **Courier Partner** किंवा **Payment Type** निवडा, मग **Apply Filters** टॅप करा.
5. कुरिअरचे ट्रॅकिंग पेज उघडण्यासाठी ट्रॅकिंग नंबर टॅप करा.
6. **Consignment Details** उघडण्यासाठी consignment ID किंवा डोळ्याचा आयकॉन टॅप करा.
7. तपशील पेजवर टाइमलाइन, आतले नमुने आणि रक्कम पाहा. लेबल आणि पॅकेजचे फोटो पाहण्यासाठी **Click and View Photos and Files** टॅप करा, आणि ते सेव्ह करण्यासाठी **Download All** टॅप करा.
8. डाउनलोड करण्यासाठी **Excel** किंवा **PDF** वापरा, आणि अपडेट करण्यासाठी **Refresh Data** वापरा.

## फील्डची माहिती

- **Current Status**: **Pending Pickup**, **Dispatched**, **In Transit**, **Delivered** किंवा **Completed**.
- **AWB/Tracking No.**: कुरिअरचा ट्रॅकिंग नंबर.
- **Total samples**: फोनवर लिस्ट पाहण्यासाठी नमुन्यांच्या संख्येवर टॅप करा.

## नेहमीच्या अडचणी

- **No consignments found**: दुसरी तारीख रेंज, स्थिती किंवा कुरिअर निवडून पाहा, किंवा **Clear filters** टॅप करा.
- **No photos yet**: consignment पाठवताना काहीही जोडलेले नव्हते.
- JMDO वापरकर्त्यांना हे पेज मेनूमध्ये दिसत नाही.
:::

::: section farmer-my-samples
title: तुमचे मातीचे नमुने पाहा (शेतकरी)
routes: /Sas/MySamples, /SampleCollection/view/{id}
for: 
roles: Farmer
designations: 
screenshot: farmer-my-samples.png
screenshot-phone: farmer-my-samples.phone.jpg
minutes: 2
source-hash: eac7ad7b8677316c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Samples** मध्ये तुमच्या MO ऑफिसरने तुमच्याकडून घेतलेले मातीचे किंवा पाण्याचे सगळे नमुने, आणि प्रत्येक नमुना आता कुठे आहे ते दिसते.

1. मेनूमधून **My Samples** उघडा.
2. प्रत्येक कार्डवर नमुन्याचा नंबर, consignment, तो गोळा करणारे ऑफिसर, तारीख आणि पीक दिसते.
3. रंगीत लेबलवर **Paid** किंवा **Free**, नमुन्याची स्थिती, आणि **Report: Ready** किंवा **Report: Not Ready** दिसते.
4. नमुना शोधण्यासाठी **Search sample, consignment, crop** मध्ये टाइप करा, तारखा निवडा, किंवा प्रकार, स्थिती किंवा पीक निवडा.
5. नमुना उघडण्यासाठी **View Details** टॅप करा.
6. तपशील पेजवर **Sample Details**, **Farmer Details**, **Payment Details** आणि **Sample Collected by MO** पासून **Report Ready for Download** पर्यंतचे **Sample Journey** टप्पे पाहा.
7. रिपोर्ट तयार झाल्यावर **Download PDF** टॅप करा. तमिळ किंवा इंग्रजी निवडण्यासाठी त्याच्या बाजूचा छोटा बाण टॅप करा.

## स्थितीचा अर्थ

- **Payment Pending**: पेमेंट अजून पूर्ण झालेले नाही.
- **Awaiting Dispatch**: लॅबला पाठवण्याची वाट पाहत आहे.
- **In Transit**: लॅबकडे जात आहे.
- **Under Analysis**: लॅबमध्ये तपासणी चालू आहे.
- **Report Ready**: तुम्ही रिपोर्ट डाउनलोड करू शकता.

## नेहमीच्या अडचणी

- **No samples yet**: तुमच्या ऑफिसरने तुमच्या नावावर अजून नमुना जोडलेला नाही. तुमच्या MO ऑफिसरला विचारा.
- **Download PDF** दिसत नाही: लॅबने अजून रिपोर्ट पूर्ण केलेला नाही.
:::

::: section farmer-my-reports
title: तुमचा माती तपासणी रिपोर्ट डाउनलोड करा (शेतकरी)
routes: /Sas/MyReports
for: 
roles: Farmer
designations: 
screenshot: farmer-my-reports.png
screenshot-phone: farmer-my-reports.phone.jpg
minutes: 2
source-hash: 40b826ab2246cad3
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Reports** मध्ये लॅबने तुमच्या नमुन्यांसाठी बनवलेले तपासणी रिपोर्ट दिसतात. प्रत्येक रिपोर्ट तुम्ही तमिळ किंवा इंग्रजीमध्ये डाउनलोड करू शकता.

1. मेनूमधून **My Reports** उघडा.
2. कार्डांवर **Total Reports**, **Ready to Download**, **Downloaded** आणि **Under Process** दिसतात.
3. **Reports List** मध्ये तुमचा रिपोर्ट शोधा. लिस्ट छोटी करण्यासाठी **Search report, sample, crop**, तारखा, **All Statuses** किंवा **All Crops** वापरा.
4. रिपोर्ट सेव्ह करण्यासाठी **Download PDF** टॅप करा. तमिळ किंवा इंग्रजी निवडण्यासाठी त्याच्या बाजूचा छोटा बाण टॅप करा.
5. **Report Details** वाचण्यासाठी डोळ्याचा आयकॉन (फोनवर **View**) टॅप करा: तपासणीचे निकाल, शिफारसी आणि पिकाबद्दल सल्ला.

## फील्डची माहिती

- **Reports No.**: रिपोर्टचा नंबर.
- **Sample No.**: हा रिपोर्ट कोणत्या नमुन्याचा आहे.
- **Status**: **Report Ready** (अजून डाउनलोड केलेला नाही), **Downloaded** किंवा **Printed**.
- **Under Process**: लॅबमध्ये अजून तपासणी चालू असलेले नमुने.

## नेहमीच्या अडचणी

- **No reports yet**: लॅबचे काम अजून पूर्ण झालेले नाही. किती नमुन्यांची तपासणी चालू आहे ते संदेशात दिसते.
- **Reports are not available right now**: तुमचे इंटरनेट तपासा आणि **Try again** टॅप करा.
- फाइल उघडली नाही: तुमच्या फोनचे डाउनलोड्स फोल्डर तपासा.
:::

::: section farmer-payments
title: तुमच्या नमुन्यांचे पेमेंट तपासा (शेतकरी)
routes: /Sas/Payments, /Sas/Payments/{id}
for: 
roles: Farmer
designations: 
minutes: 2
source-hash: f07974a161df2664
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payments** मध्ये तुमच्या पैसे भरलेल्या मातीच्या नमुन्यांसाठी केलेली पेमेंट दिसतात, आणि SPIC ने ती मंजूर केली आणि मिळाली का ते दिसते. इथे तुम्ही फक्त पाहता; काहीही भरायचे नसते.

1. मेनूमधून **Payments** उघडा.
2. कार्डांवर **Total Paid Samples**, **Finance Verified**, **Approval Pending** आणि **Payment Issues** दिसतात.
3. **Search sample no., transaction ID...** ने शोधा, किंवा तारखा, स्थिती किंवा पीक निवडा.
4. **Payment Details** उघडण्यासाठी नमुन्याचा नंबर, डोळ्याचा आयकॉन किंवा **View Details** टॅप करा.
5. रक्कम, **Payment Mode**, **Transaction ID**, **Payment Proof** खालची पावती, आणि **Verification Journey** पाहा.

## स्थितीचा अर्थ

- **Admin Approval Pending**: तुमचे पेमेंट मंजुरीची वाट पाहत आहे.
- **Admin Approved**: मंजूर झाले, Finance ची वाट पाहत आहे.
- **Finance Verified**: पेमेंटची पूर्ण खात्री झाली आहे.
- **Payment Issue**: पेमेंट परत पाठवले गेले किंवा जुळले नाही. तुमचे MO ऑफिसर ते दुरुस्त करतील.

## नेहमीच्या अडचणी

- **No payments found**: तुमचे पैसे भरलेले नमुने नाहीत, किंवा तुमचे नमुने मोफत होते.
- **Payment Issue** खूप दिवस राहिले: **Transaction ID** सह तुमच्या MO ऑफिसरशी संपर्क करा.
:::

::: section approve-payments
title: नमुन्यांचे पेमेंट मंजूर करा आणि Finance कडे पाठवा
routes: /Sas/Payments, /Sas/Payments/{id}
for: SasPaymentApproval.View
roles: 
designations: 
minutes: 3
source-hash: 2c0a5e7b4b9a51d2
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payment Approval** मध्ये फील्ड ऑफिसरनी पैसे घेण्याच्या नमुन्यांसाठी नोंदवलेली पेमेंट दिसतात. तुम्ही प्रत्येक पावती आणि रक्कम तपासता, मग ते मंजूर करून Finance कडे पाठवता, किंवा ऑफिसरकडे परत पाठवता.

1. मेनूमधून **Payment Approval** उघडा.
2. **Pending Admin Approval** टॅब टॅप करा.
3. **Payment Approval Details** उघडण्यासाठी **Payment Ref No.** किंवा डोळ्याचा आयकॉन टॅप करा.
4. **Expected Amount**, **Paid Amount** आणि **Difference** जुळवून पाहा, आणि **Payment Proof Preview** खालची पावती तपासा.
5. **Approve & Forward to Finance** टॅप करा.
6. **Verified Amount** (किंवा **Other amount**) निवडा, **Remarks** टाका, **I confirm that the payment proof, transaction details and amount are verified.** (पेमेंटचा पुरावा, व्यवहाराचा तपशील आणि रक्कम तपासली आहे) वर टिक करा, मग **Approve & Forward** टॅप करा.
7. नाकारण्यासाठी **Reject Payment** टॅप करा, **Reason for rejection** लिहा आणि परत **Reject Payment** टॅप करा.

## फील्डची माहिती

- **Admin Status**: **Pending Review**, **Admin Approved** किंवा **Returned to MO**.
- **Finance Status**: **Not Forwarded**, **Pending**, **Verified** किंवा **Mismatch**.
- **Difference**: भरलेली रक्कम अपेक्षित रकमेइतकी नसेल तेव्हा लाल दिसते.

## नेहमीच्या अडचणी

- "Tick the confirmation to continue" (पुढे जाण्यासाठी खात्रीच्या बॉक्सवर टिक करा): मंजूर करण्याआधी बॉक्सवर टिक करा.
- "Enter a verified amount greater than zero" (शून्यापेक्षा जास्त तपासलेली रक्कम टाका): रक्कम निवडा किंवा टाइप करा.
- कृतीची बटणे दिसत नाहीत: पेमेंटबद्दल आधीच निर्णय झाला आहे.
- जुनी पेमेंट शोधण्यासाठी **Filters** (तारखा, **State**, **Region**, **Payment Mode**, **Status**) वापरा.
:::

::: section verify-payments
title: मिळालेल्या पेमेंटची खात्री करा (Finance)
routes: /Sas/Payments, /Sas/Payments/{id}, /Sas/Payments/History
for: SasPaymentVerification.View
roles: 
designations: 
minutes: 3
source-hash: 788890513e2baf14
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payment Verification** मध्ये मंजुरी देणाऱ्याने Finance कडे पाठवलेली पेमेंट दिसतात. पैसे खात्यात आले याची तुम्ही खात्री करता. **Payment History** मध्ये तुम्ही पूर्ण केलेले प्रत्येक पेमेंट राहते.

1. मेनूमधून **Payment Verification** उघडा.
2. **Pending Verification** टॅब टॅप करा.
3. **Payment Ref No.** किंवा डोळ्याच्या आयकॉनने पेमेंट उघडा. **Payment Proof Details** उघडते.
4. **Payment Proof Preview** खालची पावती तुमच्या बँक स्टेटमेंटशी जुळवून पाहा.
5. पैसे आले असतील तर **Verify Payment** टॅप करा. **Verified Amount** निवडा, **Received Date** टाका, **Remarks** टाका, **I have verified the payment proof, transaction ID, and amount.** (मी पेमेंटचा पुरावा, transaction ID आणि रक्कम तपासली आहे) वर टिक करा आणि **Confirm** टॅप करा.
6. पेमेंट सापडत नसेल तर **Mark Mismatch** टॅप करा, **Mismatch remarks** लिहा आणि परत **Mark Mismatch** टॅप करा.
7. पूर्ण झालेली पेमेंट पाहण्यासाठी **Payment History** उघडा. **All**, **Verified**, **Failed** आणि **Mismatch** टॅब वापरा.

## फील्डची माहिती

- **Verified Amount**: प्रत्यक्ष मिळालेली रक्कम. अपेक्षेपेक्षा कमी असेल तर mismatch म्हणून सेव्ह होते.
- **Payment Verified**: पैसे मिळाल्याची खात्री झाली.
- **Amount Mismatch**: अपेक्षेपेक्षा कमी पैसे आले.
- **Failed**: **Mark Mismatch** ने नोंद केलेले; पैसे सापडले नाहीत.

## नेहमीच्या अडचणी

- "Enter the amount received" (मिळालेली रक्कम टाका): रक्कम निवडा किंवा टाइप करा.
- "Tick the confirmation to continue" (पुढे जाण्यासाठी खात्रीच्या बॉक्सवर टिक करा): खात्री करण्याआधी बॉक्सवर टिक करा.
- कृतीची बटणे दिसत नाहीत: पेमेंटची आधीच खात्री झाली आहे किंवा नोंद झाली आहे.
:::
