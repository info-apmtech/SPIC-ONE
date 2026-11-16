---
module: sas-sample-collection
title: मातीचे नमुने गोळा करणे
icon: bi-droplet
order: 50
language: mr
---

::: section overview
title: मातीचे नमुने गोळा करण्याचे काम कसे चालते
routes: /SampleCollection
for: 
roles: 
designations: 
source-hash: 690edb1b53c94744
confidence: ok

**ही स्क्रीन कशासाठी आहे**
SPIC शेतकऱ्याच्या शेतातली माती आणि पाणी जिल्ह्याच्या लॅबमध्ये तपासते. ॲप प्रत्येक नमुन्याचा मागोवा शेतापासून शेतकऱ्याच्या रिपोर्टपर्यंत ठेवते. प्रत्येक संकलन (collection) याच टप्प्यांमधून जाते.

## पायऱ्या

1. **Collect.** (नमुना गोळा करणे) फील्ड ऑफिसर (MDO किंवा JMDO) **New Sample Collection** उघडतात, शेतकरी निवडतात आणि एक किंवा जास्त नमुने जोडतात.
2. **Pay.** (पैसे भरणे) **Paid Sample** साठी पैसे भरावे लागतात. ऑफिसर UPI किंवा दुसऱ्या पद्धतीने झालेल्या पेमेंटची नोंद करतात आणि पावतीचा फोटो अपलोड करतात. **Free Sample** साठी हा टप्पा नसतो.
3. **Approve.** (मंजुरी) मंजुरी देणारे पेमेंट आणि पावती तपासतात. मग Finance पैसे मिळाले आहेत का ते तपासते.
4. **लॅबला पाठवणे.** ऑफिसर नमुने कुरिअरने पाठवतात आणि ट्रॅकिंग नंबर टाकतात. याला consignment (पार्सल) म्हणतात.
5. **Test.** (तपासणी) लॅबला नमुने मिळतात आणि लॅब ते तपासते.
6. **Report.** (रिपोर्ट) शेतकऱ्याला ॲपमध्ये रिपोर्ट दिसतो आणि तो तमिळ किंवा इंग्रजीमध्ये डाउनलोड करता येतो.

## स्थितीचा अर्थ

- **Draft**: सेव्ह केले आहे, अजून सबमिट केलेले नाही.
- **Pending Payment**: पेमेंटची नोंद होण्याची वाट पाहत आहे.
- **Pending Approval**: पेमेंटची नोंद झाली आहे, मंजुरीची वाट पाहत आहे.
- **Ready consignment**: लॅबला पाठवायला तयार.
- **Dispatched**, **In Transit**, **Delivered to Lab**: लॅबकडे जात आहे.
- **In Progress**: लॅबमध्ये तपासणी चालू आहे.
- **Completed**: निकाल उपलब्ध आहेत.

## नेहमीच्या अडचणी

- मेनूमध्ये **Sample Collection** दिसत नाही: तुमच्या पदामध्ये (designation) हे पेज नाही. तुमच्या ॲडमिनिस्ट्रेटरला विचारा.
- JMDO वापरकर्त्यांना हे मेनूमध्ये दिसत नाही. त्याऐवजी तुमच्या **Soil Sample** टास्कमधून ते उघडा.
:::

::: section collection-list
title: तुमची नमुना संकलने शोधा आणि त्यांचा मागोवा घ्या
routes: /SampleCollection
for: 
roles: 
designations: 
source-hash: 53f4f23e337f18d1
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sample Collection** पेजवर तुम्हाला दिसणारी सगळी संकलने, त्यांच्या पेमेंट आणि लॅबच्या स्थितीसह दिसतात. संकलन शोधण्यासाठी आणि त्याचा तपशील उघडण्यासाठी हे पेज वापरा.

## पायऱ्या

1. मेनूमधून **Sample Collection** उघडा.
2. वरच्या कार्डांवर आकडे दिसतात: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level** आणि **Ready to Submit**.
3. **Pending Payment** किंवा **Draft** सारखी रंगीत टाइल टॅप केली की फक्त तीच संकलने दिसतात. सगळी पाहण्यासाठी ती परत टॅप करा.
4. शोधण्यासाठी **Search collections...** मध्ये शेतकऱ्याचे नाव किंवा collection ID टाइप करा.
5. आणखी पर्यायांसाठी **Filters** उघडा, **Date Range**, **Location**, **Payment Type**, **Payment Status** किंवा **Status** निवडा, मग **Apply Filters** टॅप करा. ते काढण्यासाठी **Clear All** टॅप करा.
6. संकलन उघडण्यासाठी डोळ्याचा आयकॉन (फोनवर कार्ड) टॅप करा.
7. स्क्रीनवर दिसणारी लिस्ट डाउनलोड करण्यासाठी **Excel** किंवा **PDF** टॅप करा.
8. नवीन स्थिती पाहण्यासाठी **Refresh Data** टॅप करा.

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
source-hash: 4807aa811490f90c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
तुम्ही शेतकऱ्यांकडून घेतलेल्या नमुन्यांची नोंद करण्यासाठी **New Sample Collection** वापरा. एका संकलनात अनेक शेतकऱ्यांचे नमुने असू शकतात.

## पायऱ्या

1. **Sample Collection** वर **New Sample Collection** टॅप करा. JMDO: **Tasks Allocation** मधल्या **Soil Sample** कार्डवर **Start** टॅप करा, किंवा टास्कच्या **Quick actions** मधले **Soil Sample Collection** टॅप करा.
2. वर **Free Sample** किंवा **Paid Sample** निवडा.
3. **Farmer Name** मध्ये किमान दोन अक्षरे टाइप करा आणि शेतकरी निवडा. शेतकरी नवीन असेल तर **New Farmer** टॅप करा.
4. पैसे घेण्याच्या नमुन्यासाठी **Paid Sample Category** खाली **Farmer** किंवा **NGO** निवडा.
5. **Soil Sample** आणि **Water Sample** साठी **Yes** किंवा **No** निवडा.
6. **Crop 1**, **Crop 2** निवडा आणि गरज असेल तर **Remarks** टाका.
7. **Add Another Sample** टॅप करा. नमुना त्याच्या रकमेसह **Sample Added (Preview)** मध्ये दिसतो.
8. प्रत्येक नमुन्यासाठी हेच करा. ओळ बदलण्यासाठी पेन्सिल, किंवा काढण्यासाठी डस्टबिन वापरा.
9. नंतर पूर्ण करायचे असेल तर **Save Draft** टॅप करा, किंवा **Sample Summary** पाहण्यासाठी **Save and Continue** टॅप करा.
10. सारांश तपासा आणि **Continue to payment** (पैसे घेण्याचा नमुना) किंवा **Continue to consignment** (मोफत नमुना) टॅप करा.

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
source-hash: 1e935425e9054d62
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Save Draft** ने सेव्ह केलेले संकलन **Draft** म्हणूनच राहते. तुम्ही सबमिट करेपर्यंत काहीही पेमेंटला किंवा लॅबला जात नाही. तुम्ही ते परत उघडून पूर्ण करू शकता.

## पायऱ्या

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
source-hash: 67de9290d8256a0c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payments** स्क्रीनवर किती पैसे घ्यायचे ते दिसते, शेतकऱ्याला स्कॅन करण्यासाठी UPI QR कोड दिसतो, आणि पेमेंटचा तपशील व पावतीसाठी फॉर्म असतो.

## पायऱ्या

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
source-hash: 1820125bf239140d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
संकलन मोफत असेल किंवा त्याचे पेमेंट मंजूर झाले असेल, तेव्हा तुम्ही नमुने पॅक करून कुरिअरकडे देता. **Upload Consignment** कुरिअरचा तपशील आणि फोटो नोंदवते, त्यामुळे लॅबला काय येत आहे ते कळते.

## पायऱ्या

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
source-hash: c0ff9ecbae88fe84
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sample Collection Details** मध्ये एका संकलनाची सगळी माहिती दिसते: पेमेंट, कुरिअर, लॅबचे निकाल आणि इतिहास. फील्ड कर्मचारी, डीलर आणि मॅनेजर इथे प्रगती पाहू शकतात.

## पायऱ्या

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
source-hash: 71850d4b14d9d073
confidence: ok

**ही स्क्रीन कशासाठी आहे**
ॲडमिनिस्ट्रेटर संकलनाच्या तपशील पेजवर पेमेंट तपासू शकतात, consignment पुढच्या टप्प्यावर नेऊ शकतात, आणि लॅबचे निकाल टाइप करू शकतात.

## पायऱ्या

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
source-hash: c0eb73648ebf3b77
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Consignment History** मध्ये लॅबला पाठवलेले नमुन्यांचे प्रत्येक पार्सल, कुरिअर, ट्रॅकिंग नंबर आणि सध्याच्या स्थितीसह दिसते.

## पायऱ्या

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
source-hash: fa5d1d2f177d288c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Samples** मध्ये तुमच्या MO ऑफिसरने तुमच्याकडून घेतलेले मातीचे किंवा पाण्याचे सगळे नमुने, आणि प्रत्येक नमुना आता कुठे आहे ते दिसते.

## पायऱ्या

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
source-hash: 810f0b093cade052
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Reports** मध्ये लॅबने तुमच्या नमुन्यांसाठी बनवलेले तपासणी रिपोर्ट दिसतात. प्रत्येक रिपोर्ट तुम्ही तमिळ किंवा इंग्रजीमध्ये डाउनलोड करू शकता.

## पायऱ्या

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
source-hash: 41753a10de9d41d6
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payments** मध्ये तुमच्या पैसे भरलेल्या मातीच्या नमुन्यांसाठी केलेली पेमेंट दिसतात, आणि SPIC ने ती मंजूर केली आणि मिळाली का ते दिसते. इथे तुम्ही फक्त पाहता; काहीही भरायचे नसते.

## पायऱ्या

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
source-hash: 4a8d8dd3f9015aa4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payment Approval** मध्ये फील्ड ऑफिसरनी पैसे घेण्याच्या नमुन्यांसाठी नोंदवलेली पेमेंट दिसतात. तुम्ही प्रत्येक पावती आणि रक्कम तपासता, मग ते मंजूर करून Finance कडे पाठवता, किंवा ऑफिसरकडे परत पाठवता.

## पायऱ्या

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
source-hash: ec36e4e58f4e4b58
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Payment Verification** मध्ये मंजुरी देणाऱ्याने Finance कडे पाठवलेली पेमेंट दिसतात. पैसे खात्यात आले याची तुम्ही खात्री करता. **Payment History** मध्ये तुम्ही पूर्ण केलेले प्रत्येक पेमेंट राहते.

## पायऱ्या

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
