---
module: dealer-application
title: डीलर अर्ज
icon: bi-shop
order: 101
language: mr
category: डीलर
note: नवीन मार्गदर्शक
meta-source-hash: 95f84553d50218f7
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b3660f0cee5decd6
confidence: ok

13 छोट्या टप्प्यांत डीलरची नोंदणी करा: डीलर कोण आहे, व्यवसाय, मालक, पैशांची माहिती आणि कागदपत्रे. मग अर्ज सबमिट करा. डीलर मंजूर होण्याआधी RM, SMM आणि AVP तो तपासतात.

- steps | icon: bi-list-ol | color: #7c3aed
  title: 13 टप्पे, एका वेळी एक
  text: Next दाबल्यावर प्रत्येक टप्पा सेव्ह होतो. तुम्ही नंतर परत येऊन तो पूर्ण करू शकता.
- check | icon: bi-clipboard-check | color: #d97706
  title: शेवटची एक तपासणी
  text: सबमिट करण्याआधी काय राहिले आहे ते शेवटचा टप्पा दाखवतो.
- approve | icon: bi-shield-check | color: #16a34a
  title: तीन मंजुऱ्या
  text: आधी RM, मग SMM, मग AVP अर्ज मंजूर करतात.
:::

::: section flow
title: अर्ज कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2413a14b313769d3
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- applicant | type: lane | color: #7c3aed | roles: MO, MDO, JMDO, Admin
  title: अर्जदार
  sub: MO · MDO · JMDO (नवीन डीलर: Admin · AVP)
- rm-team | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: रिजन
- smm-team | type: lane | color: #d97706 | roles: SMD, SMM
  title: SMM
  sub: राज्य
- avp-team | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: अंतिम मंजुरी
- office-team | type: lane | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: फक्त नवीन डीलरसाठी
- register | lane: applicant | col: 1 | icon: bi-person-plus | color: #64748b | next: steps | screen: register
  title: नोंदणी सुरू करा
  status: Draft
  text: **Create Existing Dealer** किंवा **Create New Dealer** दाबा आणि पहिला टप्पा, **Dealer Registration**, भरा.
- steps | lane: applicant | col: 2 | icon: bi-list-ol | color: #7c3aed | next: documents | screen: experience
  title: व्यवसायाचे टप्पे भरा
  status: Registration In Progress
  text: अनुभव, विक्री, गोदामे, बाजार, कंपन्या, मालक, विक्रीचे नियोजन, मालमत्ता आणि क्रेडिट लिमिट भरा. प्रत्येक टप्प्यावर **Next** दाबा.
- documents | lane: applicant | col: 3 | icon: bi-paperclip | color: #7c3aed | next: submit | screen: enclosures
  title: कागदपत्रे अपलोड करा
  status: Registration In Progress
  text: **Enclosures** टप्प्यावर **Required** असे लिहिलेले प्रत्येक कागदपत्र अपलोड करा.
- submit | lane: applicant | col: 4 | icon: bi-send | color: #0b6b3a | next: rm | screen: final-submission
  title: तपासा आणि सबमिट करा
  status: Application Ready for Submission
  text: प्रत्येक भाग पूर्ण आहे याची खात्री करा आणि **Submit Registration** दाबा.
- incomplete | lane: applicant | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: submit | to: steps | screen: final-submission
  title: काहीतरी राहिले आहे
  text: एखादा भाग किंवा कागदपत्र राहिले असेल तर ॲप अर्ज सबमिट करत नाही. पेन्सिलने तो भाग उघडा, दुरुस्त करा आणि परत सबमिट करा.
- rm | lane: rm-team | col: 5 | icon: bi-hourglass-split | color: #0d9488 | next: smm | return: submit
  title: RM तपासतात
  status: In RM
  text: RM अर्ज मंजूर करतात, किंवा नाकारून अर्जदाराकडे परत पाठवतात.
- smm | lane: smm-team | col: 6 | icon: bi-hourglass-split | color: #d97706 | next: avp | return: rm
  title: SMM तपासतात
  status: In SMM
  text: SMM अर्ज मंजूर करतात, किंवा नाकारून RM कडे परत पाठवतात.
- avp | lane: avp-team | col: 7 | icon: bi-patch-check | color: #2563eb | next: office | return: smm
  title: AVP मंजूर करतात
  status: In AVP
  text: AVP अंतिम मंजुरी देतात, किंवा नाकारून SMM कडे परत पाठवतात.
- office | lane: office-team | col: 8 | icon: bi-123 | color: #64748b | next: done
  title: नवीन डीलरचे डीलर ID
  status: In AVP Office
  text: फक्त नवीन डीलरसाठी AVP Office कंपनीचे डीलर ID सेव्ह करते. जुन्या डीलरसाठी हा टप्पा नसतो.
- done | lane: applicant | col: 9 | icon: bi-check-circle | color: #16a34a | screen: application-pdf
  title: डीलर मंजूर झाला
  status: Approved
  text: अर्ज पूर्ण झाला. डीलर कार्डवरून तुम्ही डीलरशिपचा अर्ज फॉर्म उघडू शकता.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 329a69141c3bcda8
confidence: ok

- draft | color: #64748b
  label: Draft
  text: सेव्ह केले आहे पण अजून सबमिट केलेले नाही. तुम्ही अजून बदल करू शकता.
- in-progress | color: #7c3aed
  label: Registration In Progress
  text: तुम्ही टप्पे भरत आहात.
- ready | color: #0b6b3a
  label: Application Ready for Submission
  text: प्रत्येक भाग आणि कागदपत्र पूर्ण आहे. तुम्ही सबमिट करू शकता.
- in-rm | color: #0d9488
  label: In RM
  text: सबमिट झाला. RM तो तपासत आहेत.
- in-smm | color: #d97706
  label: In SMM
  text: RM ने मंजूर केला. SMM तो तपासत आहेत.
- in-avp | color: #2563eb
  label: In AVP
  text: SMM ने मंजूर केला. AVP ची वाट पाहत आहे.
- in-office | color: #64748b
  label: In AVP Office
  text: AVP ने नवीन डीलर मंजूर केला. कंपनीच्या डीलर ID ची वाट पाहत आहे.
- approved | color: #16a34a
  label: Approved
  text: अर्ज पूर्णपणे मंजूर झाला आहे.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: अर्जदाराकडे परत पाठवला. दुरुस्त करा आणि परत सबमिट करा.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: दुरुस्तीसाठी RM कडे परत पाठवला.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: दुरुस्तीसाठी SMM कडे परत पाठवला.
- uploaded | color: #16a34a
  label: Uploaded
  text: कागदपत्र जोडलेले आहे.
- missing | color: #ea580c
  label: Missing
  text: कागदपत्र अजून जोडलेले नाही.
- consent | color: #0891b2
  label: Consent Provided
  text: डीलरने क्रेडिट लिमिटची माहिती दिली नाही. क्रेडिट लिमिटचे टप्पे वगळले जातात.
- active | color: #16a34a
  label: Active
  text: डीलर सध्या व्यवसाय करत आहे.
- inactive | color: #d97706
  label: Inactive
  text: डीलर सध्या व्यवसाय करत नाही.
- terminated | color: #475569
  label: Terminated
  text: डीलरशिप संपली आहे.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 17fdf789d027bf63
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- creator | color: #7c3aed | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - जुन्या डीलरची नोंदणी करा
  - सगळे टप्पे भरा आणि कागदपत्रे अपलोड करा
  - सबमिट करा, आणि RM ने नाकारलेला अर्ज दुरुस्त करा
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - Create New Dealer ने नवीन डीलरची नोंदणी करा
  - AVP अंतिम मंजुरी देतात
- rm | color: #0d9488 | roles: RM, RMD
  title: RM
  - तुमच्या रिजनमधले अर्ज तपासा
  - मंजूर करा, किंवा नाकारून अर्जदाराकडे परत पाठवा
- smm | color: #d97706 | roles: SMD, SMM
  title: SMM
  - तुमच्या राज्यातले अर्ज तपासा
  - मंजूर करा, किंवा नाकारून RM कडे परत पाठवा
- office | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - मंजूर झालेल्या नवीन डीलरसाठी कंपनीचे डीलर ID सेव्ह करा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 30e6b2b2e39499dd
confidence: ok

- warehouse-stuck | popular: true
  q: मी Warehouse टप्प्याच्या पुढे जाऊ शकत नाही
  a: तुम्हाला किमान दोन गोदामे (warehouse), दोन रेल्वे हेड आणि दोन बंदरे (port) लागतात. प्रत्येक त्याच्या **Add** बटणाने जोडा, मग **Next** दाबा.
- not-submitted | popular: true
  q: Submit Registration दाबल्यावर काही भाग अपूर्ण आहेत असे दिसते
  a: **Final Submission** वर **Section Completion Status** आणि **Enclosures** खाली नारिंगी खुणा शोधा. पेन्सिल टॅप करा, तो टप्पा पूर्ण करा आणि परत सबमिट करा.
- red-step
  q: वरच्या टप्प्यांच्या पट्टीचा एक भाग लाल आहे
  a: त्या टप्प्यात काहीतरी चूक आहे किंवा माहिती राहिली आहे. तो टप्पा उघडून दुरुस्त करण्यासाठी लाल भाग टॅप करा.
- ocr
  q: ॲपने GST, PAN किंवा बँक नंबर आपोआप भरला
  a: ॲप अपलोड केलेली फाइल वाचते. नंबर नेहमी तपासा. चुकीचा असेल तर बरोबर नंबर टाइप करा.
- skipped-credit
  q: Credit Limit चे टप्पे वगळले गेले
  a: **Investment** टप्प्यावर संमती (consent) दिली होती. क्रेडिट लिमिट भरण्यासाठी **Investment** उघडा आणि **Remove Consent** दाबा.
- no-code
  q: माझ्या नवीन डीलरला Dealer Code नाही
  a: नवीन डीलरला Dealer Code फक्त अंतिम मंजुरीनंतरच मिळतो.
:::

::: section register
title: डीलरची नोंदणी सुरू करा (टप्पा 1)
routes: /Register
for: 
roles: 
designations: 
screenshot: register.png
hotspots: 1 @ 2.2,21.9,95.5,33.1 | 2 @ 2.2,57.9,95.5,32.4 | 3 @ 2.2,93.1,95.5,6.9 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: register.phone.jpg
hotspots-phone: 1 @ 3.1,14.7,93.8,42.9 | 2 @ 3.1,58.8,93.8,39.3 | 3 @ 3.1,99.2,93.8,0.8 | 4 @ - | 5 @ - | 6 @ -
tour: 1 > [data-help=dr-basic] | 2 > [data-help=dr-business] | 3 > [data-help=dr-location] | 4 > [data-help=dr-bank] | 5 > [data-help=dr-tax] | 6 > [data-help=dr-license]
minutes: 6
source-hash: 1268f37d3a5ce326
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Dealer Registration** स्क्रीन हा 13 पैकी पहिला टप्पा आहे. यात डीलर कोण आहे, दुकान कुठे आहे, बँक खाते, आणि टॅक्स व लायसन्सची माहिती नोंदवली जाते. **Register** मेनू उघडा आणि **Create Existing Dealer** दाबा. अगदी नवीन डीलरसाठी Admin आणि AVP **Create New Dealer** वापरतात.

1. **Basic Information** खाली **Dealer**, **Department** किंवा **Institution** निवडा, आणि **SPIC**, **GREEN STAR** किंवा दोन्ही टॅप करा. **State**, **Region** आणि **HQ** तपासा. हे तुमच्या प्रोफाइलमधून भरलेले असतात. जुन्या डीलरसाठी **Active**, **Inactive** किंवा **Terminated** पण निवडा.
2. **Business Information** खाली **Dealer Code & Name** आणि **Parent Dealer Code & Name** निवडा (फक्त जुन्या डीलरसाठी). खत लायसन्सवर आहे तसेच **Firm Name** टाइप करा आणि **Date of Appointment for SPIC** किंवा **for Greenstar** निवडा.
3. **Primary Location** खाली दुकानात उभे राहून **Use Current Location** दाबा, किंवा **Google Map URL** पेस्ट करा. मग पत्ता, **Pincode**, **District**, **State** आणि तिन्ही फोन नंबर भरा.
4. **Bank Details** खाली **Upload Cheque (PDF or Image)** ने चेक अपलोड करा. ॲप तो वाचून खात्याची माहिती भरते. **Account Holder Name**, **Account Number**, **Bank Name**, **Bank Branch** आणि **IFSC Code** तपासा.
5. **Tax & Compliance** खाली GST सर्टिफिकेट, PAN कार्ड आणि e-Aadhaar PDF फाइल म्हणून अपलोड करा. ॲप प्रत्येक फाइल वाचून नंबर भरते. प्रत्येक नंबर तपासा. **TAN Number** भरणे गरजेचे नाही.
6. **Wholesale Fertilizer Details (as per iFMS)** किंवा **Retail Fertilizer Details (as per iFMS)** भरा: लायसन्स नंबर, **Expiry Date**, **mFMS ID** आणि लायसन्सची फाइल. **Office Automation** चे उत्तर द्या, मग **Next** दाबा.

> Tip: जुन्या डीलरसाठी **Trade Deposit Details for SPIC** किंवा **for Greenstar** पण भरा: रक्कम, **Receipt No** आणि **Deposit Date**.
> Careful: तुम्ही **Department** किंवा **Terminated** ही स्थिती निवडली तर हे एकच पेज असते. बटणावर **Submit** असे लिहिलेले असते आणि ते फॉर्म लगेच तपासणीसाठी पाठवते.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Firm Name** | खत लायसन्सवर आहे तसे व्यवसायाचे नाव. भरणे आवश्यक. |
| **Pincode** | 6 अंक. ॲप त्यावरून भाग शोधते. |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 अंकी मोबाइल नंबर. |
| **GST Number** | 15 अक्षरे, उदा. 22AAAAA0000A1Z5. |
| **PAN Number** | 10 अक्षरे, उदा. ABCDE1234F. |
| **Aadhaar Number** | बरोबर 12 अंक. |
| GST, PAN, Aadhaar आणि TAN च्या फाइल | फक्त PDF, 5 MB पेक्षा कमी. |
| लायसन्स | किमान एक लायसन्स, होलसेल किंवा रिटेल, त्याच्या सर्टिफिकेटसह. |

## नेहमीच्या अडचणी

- **Next दाबल्यावर पेज पुढे जात नाही.** स्टार असलेले एखादे फील्ड रिकामे आहे किंवा चुकीचे भरले आहे. फील्डखाली लाल मेसेज शोधा, उदा. "Pincode must be 6 digits".
- **"At least one Fertilizer License (Wholesale or Retail) is required"** (किमान एक खत लायसन्स, होलसेल किंवा रिटेल, आवश्यक आहे) एक लायसन्स भरा आणि त्याचे सर्टिफिकेट अपलोड करा.
- **"OCR could not extract the required information from this document."** (ॲपला या कागदपत्रातून माहिती वाचता आली नाही) जास्त स्पष्ट फाइल अपलोड करा, किंवा माहिती स्वतः टाइप करा.
- **"Only PDF files are allowed" or "File size must be less than 5 MB".** (फक्त PDF फाइल चालतात, किंवा फाइल 5 MB पेक्षा लहान असावी) कागदपत्र लहान PDF म्हणून सेव्ह करा आणि परत अपलोड करा.
:::

::: section experience
title: खत व्यवसायातला अनुभव (टप्पा 2)
routes: /Experience
for: 
roles: 
designations: 
screenshot: experience.png
screenshot-phone: experience.phone.jpg
minutes: 3
source-hash: 4c4087f2c0c5036d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Experience In Fertilizer Trade** स्क्रीनवर डीलर किती वर्षांपासून खत विकतो आहे, आणि कोणत्या कंपन्यांसाठी, याची नोंद होते.

1. **Year of Experience** मध्ये डीलर किती वर्षांपासून खताच्या व्यवसायात आहे ते टाका.
2. **Add Experience** खाली **Company Name** निवडा आणि **Number of the years** टाका.
3. त्या कंपनीसाठी **Quantity (MT)** आणि **Turnover (Lakhs)** टाका.
4. डीलर आता त्या कंपनीसोबत काम करत नसेल तर **Still Active** बंद करा.
5. **Add Experience** दाबा. कंपनी **Saved Experience** खाली दिसते. प्रत्येक कंपनीसाठी असेच करा.
6. रेकॉर्ड बदलण्यासाठी पेन्सिल वापरा आणि **Update Experience** दाबा, किंवा डिलीट करण्यासाठी डस्टबिन वापरा. मग **Next** दाबा.

## नेहमीच्या अडचणी

- **"Please select a company."** (कृपया कंपनी निवडा) **Add Experience** दाबण्याआधी **Company Name** निवडा.
- **टप्प्यांच्या पट्टीवर टप्पा 2 लाल होतो.** **Year of Experience** टाका आणि किमान एक कंपनी जोडा.
:::

::: section annual-sales
title: मागच्या वर्षीची विक्री (टप्पा 3)
routes: /AnnualSales
for: 
roles: 
designations: 
screenshot: annual-sales.png
screenshot-phone: annual-sales.phone.jpg
minutes: 4
source-hash: 3d5342d2000c7789
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Annual Sales Data For All Companies** स्क्रीनवर डीलरने **Last Financial Year** मध्ये सगळ्या कंपन्यांचे मिळून किती विकले याची नोंद होते.

1. टेबलच्या वरचा एक कॅटेगरी टॅब टॅप करा.
2. प्रॉडक्ट ग्रुप उघडून त्यातले प्रॉडक्ट पाहण्यासाठी तो टॅप करा.
3. प्रत्येक प्रॉडक्टसाठी **Own Retail Qty** टाका. डीलरकडे होलसेल लायसन्स असेल तर **Sale to Dealer Qty** पण टाका.
4. टर्नओव्हर (**TO (₹)in Lakhs**) आणि बेरीज ॲप आपोआप काढते. टेबलखालची सारांश कार्डे आणि **Grand Total** तपासा.
5. एखादा प्रॉडक्ट लिस्टमध्ये नसेल तर **Add Product** दाबा, **Category**, **Product Group**, **Product Name** आणि **Rate Per Unit (RPU)** भरा, आणि **Save** दाबा.
6. **Next** दाबा.

## नेहमीच्या अडचणी

- **Sale to Dealer Qty कॉलम दिसत नाही.** टप्पा 1 मध्ये डीलरचे होलसेल लायसन्स भरले असेल तरच तो दिसतो.
- **टप्प्यांच्या पट्टीवर टप्पा 3 लाल होतो.** किमान एका प्रॉडक्टसाठी संख्या टाका.
- **"A Product with the name ... already exists in this Category."** (या नावाचा प्रॉडक्ट या कॅटेगरीत आधीच आहे) प्रॉडक्ट लिस्टमध्ये आधीच आहे. तो त्याच्या प्रॉडक्ट ग्रुपमध्ये शोधा.
:::

::: section warehouse
title: जवळची गोदामे, रेल्वे हेड आणि बंदरे (टप्पा 4)
routes: /Warehouse
for: 
roles: 
designations: 
screenshot: warehouse.png
screenshot-phone: warehouse.phone.jpg
minutes: 4
source-hash: bd545fbcf4350bec
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Warehouse & Logistics** स्क्रीनवर डीलरच्या सगळ्यात जवळच्या सुविधांची नोंद होते. टप्पा 1 मध्ये दिलेल्या दुकानाच्या ठिकाणावरून अंतर काढले जाते.

1. **Logistics Note Information** वाचा. तुम्हाला किमान दोन गोदामे, दोन रेल्वे हेड आणि दोन बंदरे लागतात.
2. **Warehouse** कार्डमध्ये **Facility Name** शोधून निवडा. **Distance (KM)** आपोआप भरते.
3. **Freight (₹/ton/Km)** टाका आणि **Add Warehouse** दाबा.
4. **Rail Head** आणि **Port** कार्डमध्ये पण असेच करा.
5. **Selected Nearest Facilities** खालची लिस्ट तपासा. एखादी बदलण्यासाठी पेन्सिल, किंवा डिलीट करण्यासाठी डस्टबिन वापरा.
6. **Next** दाबा.

## नेहमीच्या अडचणी

- **"A minimum of two Warehouses, two Rail Heads, and two Ports must be configured to proceed further."** (पुढे जाण्यासाठी किमान दोन गोदामे, दोन रेल्वे हेड आणि दोन बंदरे जोडायला हवीत) आधी राहिलेल्या सुविधा जोडा.
- **"Dealer location (Latitude/Longitude) is not set."** (डीलरचे ठिकाण भरलेले नाही) टप्पा 1 वर परत जा आणि **Google Map URL** किंवा **Latitude & Longitude** भरा.
- **Add बटण राखाडी आहे.** आधी **Facility Name** निवडा आणि अंतर दिसेपर्यंत थांबा.
:::

::: section market-details
title: बाजाराची माहिती (टप्पा 5)
routes: /MarketDetails
for: 
roles: 
designations: 
screenshot: market-details.png
screenshot-phone: market-details.phone.jpg
minutes: 3
source-hash: a7a15b3e04404881
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Market Details** स्क्रीनवर डीलर ज्या ब्लॉक आणि गावांमध्ये माल देतो त्यांची, तिथल्या पिकांची आणि शेतकऱ्यांची नोंद होते.

1. **Blocks / Villages** खाली **Name of Block/ Village** टाइप करा.
2. **Major Crops** उघडा आणि मुख्य पिकांवर टिक करा. तुम्ही लिस्टमध्ये शोधू शकता.
3. **No of Farmer** टाका, आणि **No of Dealer** बॉक्स दिसत असेल तर तो पण भरा.
4. **Season From & To Month** आणि **Irrigation Type** निवडा: **CANAL**, **TANK**, **WELL** किंवा **RAINFED**.
5. **Add Blocks/ Villages** दाबा. ही नोंद **Added Market Details** खाली दिसते. प्रत्येक ब्लॉक किंवा गावासाठी असेच करा.
6. **Next** दाबा.

## नेहमीच्या अडचणी

- **"No regional blocks added yet."** (अजून एकही ब्लॉक जोडलेला नाही) किमान एक ब्लॉक किंवा गाव जोडा. नाहीतर टप्पा 5 लाल दिसतो.
- **मला No of Dealer दिसत नाही.** डीलरकडे होलसेल लायसन्स असेल तरच ते दिसते.
:::

::: section companies
title: डीलरच्या भागातल्या कंपन्या (टप्पा 6)
routes: /Companies
for: 
roles: 
designations: 
screenshot: companies.png
screenshot-phone: companies.phone.jpg
minutes: 1
source-hash: 4709346d2ff331c4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Companies Operating In Your Place** स्क्रीनवर डीलरच्या भागात कोणत्या खत कंपन्या विक्री करतात ते दिसते.

1. त्या भागात विक्री करणारी प्रत्येक कंपनी टॅप करा, **SPIC** आणि **GREEN STAR** सुद्धा. निवडलेल्या कंपनीवर टिक दिसते.
2. टिक काढण्यासाठी कंपनी परत टॅप करा.
3. **Next** दाबा.

## नेहमीच्या अडचणी

- **टप्प्यांच्या पट्टीवर टप्पा 6 लाल होतो.** किमान एका कंपनीवर टिक करा.
:::

::: section proprietor
title: मालक आणि भागीदारांची माहिती (टप्पा 7)
routes: /Proprietor
for: 
roles: 
designations: 
screenshot: proprietor.png
minutes: 6
source-hash: 02521efb7ec5b6b9
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Proprietor Details** स्क्रीनवर व्यवसायाचा कायदेशीर प्रकार आणि प्रत्येक मालक किंवा भागीदार, त्यांच्या कुटुंबासह, यांची नोंद होते. ही माहिती SDWA कल्याण योजनेसाठी पण वापरली जाते.

1. **Business Entity Type** निवडा: **Sole Proprietor**, **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** किंवा **Society**.
2. **Ownership Information** खाली **Authorized Name**, **Son of / Daughter of**, **Phone Number**, **PAN number** आणि **Aadhaar Card Number** भरा.
3. **Proprietor / Managing Partner Image** अपलोड करा आणि **Residential Address Details** भरा.
4. **Family Members** खाली प्रत्येक सदस्य जोडा आणि **Add Details** दाबा. **Other Occupation / Agencies** खाली **Yes** किंवा **No** निवडा आणि दुसरा कोणताही व्यवसाय असेल तर तो जोडा.
5. **Save Details** दाबा. ती व्यक्ती **Saved Details** खाली दिसते. प्रत्येक भागीदार असाच जोडा.
6. **Next** दाबा.

| Business Entity Type | किती व्यक्ती लागतात |
|---|---|
| **Sole Proprietor** | बरोबर 1 मालक. |
| **Society** | किमान 1 अधिकृत व्यक्ती. |
| **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** | किमान 2 भागीदार. |

## नेहमीच्या अडचणी

- **"Please upload Proprietor / Managing Partner Image before saving."** (सेव्ह करण्याआधी मालक / मॅनेजिंग पार्टनरचा फोटो अपलोड करा) फोटो अपलोड करा, मग परत **Save Details** दाबा.
- **"Please add at least 2 partners for the selected entity type."** (निवडलेल्या प्रकारासाठी किमान 2 भागीदार जोडा) **Next** दाबण्याआधी दुसरा भागीदार सेव्ह करा.
- **एक व्यक्ती सेव्ह केल्यावर फॉर्म दिसेनासा झाला.** **Sole Proprietor** साठी फक्त एकच व्यक्ती चालते. ती बदलण्यासाठी **Saved Details** खालची पेन्सिल वापरा.
:::

::: section sales-planning
title: पुढच्या वर्षाचे विक्रीचे नियोजन (टप्पा 8)
routes: /SalesPlaning
for: 
roles: 
designations: 
screenshot: sales-planning.png
screenshot-phone: sales-planning.phone.jpg
minutes: 4
source-hash: b908e1cc20e482d4
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sales Planning & Forecast** स्क्रीनवर पुढच्या आर्थिक वर्षात, एप्रिल ते मार्च, डीलर दर महिन्याला किती विकायचे ठरवतो याची नोंद होते.

1. **Business Plan** खाली एक कॅटेगरी टॅब टॅप करा.
2. प्रॉडक्ट ग्रुप उघडण्यासाठी तो टॅप करा.
3. प्रत्येक प्रॉडक्टसाठी **April** ते **March** प्रत्येक महिन्याखाली ठरवलेली संख्या टाका. लाखांमधली किंमत आणि **Total** ॲप आपोआप काढते.
4. **Monthly Total** ओळ आणि सारांश कार्डे तपासा, जसे **Fertilizer Products Summary**.
5. **Next** दाबा.

## नेहमीच्या अडचणी

- **टप्प्यांच्या पट्टीवर टप्पा 8 लाल होतो.** किमान एका प्रॉडक्टसाठी नियोजन टाका.
:::

::: section investment
title: मालमत्ता, कर्जे आणि क्रेडिटसाठी संमती (टप्पा 9)
routes: /Investment
for: 
roles: 
designations: 
screenshot: investment.png
screenshot-phone: investment.phone.jpg
minutes: 5
source-hash: 6476f16e152121ca
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Investment** स्क्रीनवर डीलरकडे काय आहे आणि त्याच्यावर किती देणे आहे याची नोंद होते, आणि **Net Financial Position** दिसते.

1. डीलर क्रेडिट लिमिटची माहिती देणार नसेल तर **Give Consent** दाबा. मग क्रेडिट लिमिटचे टप्पे वगळले जातात आणि शून्य म्हणून नोंदवले जातात. **Remove Consent** ने हे रद्द होते.
2. **Assets (Banking & Properties)** खाली **Banking**, **Land** किंवा **Building** यापैकी किमान एक जोडा. कार्ड भरा आणि **Add Banking**, **Add Land** किंवा **Add Building** दाबा.
3. जमीन आणि इमारतीसाठी तुमच्याकडे असतील तर **EC (Encumbrance Certificate)**, **Property Document** आणि **Valuation Certificate** अपलोड करा.
4. **Movable** आणि **Infrastructure** खाली **Movable Assets Value**, **Own Godown Capacity** आणि **Rent Godown Capacity** टाका.
5. **Loan Liabilities List** खाली प्रत्येक कर्जाचा **Loan Source / Institution** आणि **Loan Value (₹) In Lakhs** टाका, आणि **Add Loan** दाबा.
6. **Net Financial Position** तपासा: **Fixed Assets** वजा **Total Liabilities**. मग **Next** दाबा.

## नेहमीच्या अडचणी

- **"Please add details for at least one of Banking, Land, or Building before proceeding."** (पुढे जाण्याआधी Banking, Land किंवा Building पैकी किमान एकाची माहिती जोडा) आधी एक मालमत्ता जोडा.
- **"OD Value is required when Banking details are provided."** (Banking ची माहिती दिली असेल तर OD Value आवश्यक आहे) **OD Limit (₹) Lakhs** टाका.
- **"Market Value is required when Land details are provided."** (Land ची माहिती दिली असेल तर Market Value आवश्यक आहे) बाजारभाव टाका. इमारतीसाठी पण हेच लागू आहे.
:::

::: section credit-limit
title: क्रेडिट लिमिट किंवा ट्रेड डिपॉझिट (टप्पे 10 आणि 11)
routes: /CreditLimit, /CreditLimitForGreenStar
for: 
roles: 
designations: 
screenshot: credit-limit.png
screenshot-phone: credit-limit.phone.jpg
minutes: 5
source-hash: d34b3a1b414aa494
confidence: ok

**ही स्क्रीन कशासाठी आहे**
टप्पा 10 SPIC साठी आहे आणि टप्पा 11 Greenstar साठी आहे. टप्पा 1 मध्ये **GREEN STAR** निवडले असेल तरच टप्पा 11 दिसतो. जुन्या डीलरसाठी या स्क्रीन (**Credit Limit — SPIC** आणि **Credit Limit — GreenStar**) सुचवलेली क्रेडिट लिमिट काढतात. नवीन डीलरसाठी त्याऐवजी इथे ट्रेड डिपॉझिट भरले जाते.

1. वरचा डीलरचा सारांश, **Existing Credit Limit** (**Valid From** आणि **Valid To**) आणि विक्री नियोजनाची कार्डे तपासा.
2. **Details of Securities** खाली प्रत्येक सिक्युरिटीसाठी **Number**, **Other Details** आणि **Amount (₹) In Lakhs** टाका, जसे **Fixed Deposit with SPIC** किंवा **BG in favour of SPIC**.
3. **SPIC Valuation** किंवा **Greenstar Valuation** टेबलमध्ये बहुतेक आकडे आधीच्या टप्प्यांमधून येतात. **Monthly Average Net Overdues during last year** तुम्ही स्वतः टाका. हे भरणे आवश्यक आहे, 0 किंवा जास्त.
4. **Grand Total** स्कोअर आणि **Recommended Credit Limit** बॉक्स तपासा: **Proposed Limit**, **Credit Multiplier** आणि **MO Recommended Credit Limit**.
5. काही **Additional Credit Limit (₹) In Lakhs** असेल तर टाका, मग **Next** दाबा.

## नवीन डीलरसाठी

- टप्पा 10 वर **Application Fee** (**Bank**, **DD Number**, **DD Date**, **Amount (₹)**, **Payable At**) आणि **Trade Deposit Details** भरा. दोन्ही अटॅचमेंट PDF, JPG किंवा PNG म्हणून अपलोड करा.
- टप्पा 11 वर **GFL Trade Deposit Details** भरा आणि अटॅचमेंट अपलोड करा.

> Tip: मंजूर झालेल्या डीलरसाठी, डीलर कार्डवरचे **Credit Limit** बटण ही स्क्रीन वेगळी उघडते. मग खालच्या बटणावर **Submit** असे लिहिलेले असते.

## नेहमीच्या अडचणी

- **"Monthly Average Net Overdues during last year is required and must be 0 or greater."** (हा आकडा भरणे आवश्यक आहे आणि तो 0 किंवा जास्त असावा) आकडा 0 असला तरी तो टाका.
- **"Complete all SPIC Application Fee and Trade Deposit fields, and upload both attachments."** (SPIC Application Fee आणि Trade Deposit ची सगळी फील्ड भरा, आणि दोन्ही अटॅचमेंट अपलोड करा) प्रत्येक बॉक्स भरा आणि दोन्ही फाइल अपलोड करा.
- **हे टप्पे उघडत नाहीत.** **Investment** टप्प्यावर संमती (consent) दिली होती.
:::

::: section enclosures
title: कागदपत्रे अपलोड करा (टप्पा 12)
routes: /Enclosures
for: 
roles: 
designations: 
screenshot: enclosures.png
minutes: 5
source-hash: f0405468232d5c91
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Enclosures** स्क्रीनवर सगळी कागदपत्रे एकाच ठिकाणी जमा होतात. कोणती कागदपत्रे **Uploaded** आहेत आणि कोणती **Missing** आहेत ते इथे दिसते.

1. वरचे **Document Completion Progress** तपासा. किती कागदपत्रे अपलोड झाली आणि किती आवश्यक कागदपत्रे बाकी आहेत ते त्यात दिसते.
2. **Documents already added in previous steps** खाली GST, PAN, Aadhaar आणि लायसन्सच्या फाइल तपासा. **Missing** असलेले कागदपत्र **Upload Document** ने अपलोड करा.
3. **Partner / Proprietor Documents** आणि **Property Documents (Land & Building)** तपासा. राहिलेला फोटो, Aadhaar, PAN, EC, मालमत्तेचे कागदपत्र किंवा व्हॅल्युएशन सर्टिफिकेट अपलोड करा.
4. **Additional Documents** खाली वर्ष निवडा आणि **IT Returns for Last Year** आणि **IT Returns for Last 2nd Year** अपलोड करा, तसेच **Required** असे लिहिलेले इतर प्रत्येक कागदपत्र अपलोड करा.
5. सहीच्या नमुन्यासाठी **Download Template** दाबा, तो प्रिंट करा, सगळ्या सही करणाऱ्यांच्या सह्या घ्या, आणि **Spic Specimen Signature** किंवा **Greenstar Specimen Signature** खाली अपलोड करा.
6. शेवटच्या तपासणीकडे जाण्यासाठी **Next** दाबा.

> Careful: डिलीट केलेले कागदपत्र परत आणता येत नाही. चुकून डिलीट झाले तर बरोबर फाइल परत अपलोड करा.

| कागदपत्र | काय अपलोड करायचे |
|---|---|
| बहुतेक कागदपत्रे | PDF, JPG किंवा PNG, 10 MB पर्यंत. |
| **List of Retailers with Complete Details** | मागितले असेल तेव्हा Excel किंवा CSV फाइल. फॉरमॅटसाठी **Download Sample Template** दाबा. |
| **Request Letter for Dealership** | फक्त नवीन डीलरसाठी. फर्मच्या लेटरहेडवर, शिक्का आणि सहीसह. |

## नेहमीच्या अडचणी

- **"You still have ... required document(s) pending."** (अजून ... आवश्यक कागदपत्रे बाकी आहेत) **Required** आणि **Missing** असे लिहिलेले प्रत्येक कागदपत्र अपलोड करा.
- **"No partners found. Add partners in Proprietor step first."** (एकही भागीदार सापडला नाही. आधी Proprietor टप्प्यात भागीदार जोडा) टप्पा 7 वर परत जा आणि मालक किंवा भागीदार सेव्ह करा.
:::

::: section final-submission
title: अर्ज तपासा आणि सबमिट करा (टप्पा 13)
routes: /FinalSubmission
for: 
roles: 
designations: 
screenshot: final-submission.png
hotspots: 1 @ 2.2,21.9,95.5,13 | 2 @ 2.2,37.8,95.5,31.3 | 3 @ 2.2,72,95.5,28 | 4 @ - | 5 @ -
screenshot-phone: final-submission.phone.jpg
hotspots-phone: 1 @ 3.1,13.3,93.8,7.5 | 2 @ 3.1,22.5,93.8,38.9 | 3 @ 3.1,63.1,93.8,36.9 | 4 @ - | 5 @ -
tour: 1 > [data-help=dr-fs-progress] | 2 > [data-help=dr-fs-sections] | 3 > [data-help=dr-fs-enclosures] | 4 > [data-help=dr-fs-terms] | 5 > [data-help=dr-fs-result]
minutes: 3
source-hash: ec4c07bdc1524795
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Final Submission** स्क्रीन हा शेवटचा टप्पा आहे. काय पूर्ण आहे आणि काय राहिले आहे ते इथे दिसते, आणि इथूनच अर्ज मंजुरीसाठी जातो.

1. वरचे प्रगतीचे कार्ड पाहा. किती भाग पूर्ण झाले ते त्यात दिसते. 100% झाल्यावर त्यावर **Application Ready for Submission** दिसते.
2. **Section Completion Status** खाली हिरवी टिक म्हणजे तो भाग पूर्ण आहे, आणि नारिंगी खूण म्हणजे काहीतरी राहिले आहे. तो टप्पा उघडून दुरुस्त करण्यासाठी पेन्सिल टॅप करा.
3. **Enclosures** कार्डमध्ये **Documents Uploaded** आणि **Missing Documents** तपासा. राहिलेल्या कागदपत्रावर नारिंगी फुली असते.
4. **Terms and Conditions** आणि **I hereby declare** चे मुद्दे वाचा. सबमिट केल्यावर तुम्ही ते मान्य करता.
5. हिरवा **All sections completed!** बॉक्स दिसल्यावर **Submit Registration** दाबा. तुम्हाला **Registration submitted successfully!** दिसते आणि तुम्ही डीलर लिस्टवर परत जाता, जिथे अर्जावर **In RM** दिसते.

> Tip: वरच्या टप्प्यांच्या पट्टीचा कोणताही भाग टॅप करून तुम्ही थेट त्या टप्प्यावर जाऊ शकता.

## नेहमीच्या अडचणी

- **"Please complete all ... incomplete section(s) before submitting."** (सबमिट करण्याआधी सगळे ... अपूर्ण भाग पूर्ण करा) आधी नारिंगी खूण असलेला प्रत्येक भाग दुरुस्त करा.
- **"Error submitting registration"** (नोंदणी सबमिट करताना अडचण आली) मेसेज वाचा. सर्व्हरला काय कमी आढळले ते त्यात लिहिलेले असते. ते दुरुस्त करा आणि परत सबमिट करा.
- **Submit Registration बटण दिसत नाही.** अर्ज फक्त पाहण्यासाठी उघडला आहे. डीलर कार्डवरच्या **Edit** ने तो उघडा. अर्ज **Draft** किंवा **Rejected by RM** असेल तेव्हाच **Edit** दिसते.
:::

::: section application-pdf
title: डीलरशिपचा अर्ज फॉर्म पाहा
routes: /DealershipPDF/{dealerId:int?}
for: 
roles: 
designations: 
minutes: 2
source-hash: 916c338c0a45b3ba
confidence: ok

**ही स्क्रीन कशासाठी आहे**
ही स्क्रीन संपूर्ण अर्ज एकाच **DEALERSHIP APPLICATION FORM** म्हणून दाखवते, SPIC साठी किंवा Greenstar साठी. अर्ज वाचण्यासाठी किंवा त्याची छापील प्रत ठेवण्यासाठी ती वापरा.

1. डीलर लिस्टमध्ये डीलरचे कार्ड शोधा आणि **SPIC** किंवा **GFL** टॅप करा.
2. वर कंपनीचे नाव आणि लोगो असलेला फॉर्म उघडतो.
3. प्रत्येक भाग वाचण्यासाठी खाली स्क्रोल करा: **Dealer Details**, **Business Information**, **Bank Details**, **Proprietor / Partner Details**, **Sales Planning & Forecast**, **Investment Details** आणि इतर.
4. शेवटी क्रेडिट लिमिट, **Recommended Credit Limit** आणि **Attached Documents** पाहा.
5. प्रत ठेवण्यासाठी तुमच्या ब्राउझर किंवा फोनचा प्रिंट पर्याय वापरा.

## नेहमीच्या अडचणी

- **वर "Credit Limit Consent: GIVEN" दिसते.** डीलरने क्रेडिट लिमिटची माहिती दिली नाही, त्यामुळे क्रेडिट लिमिट 0 आहे.
- **GFL बटण दिसत नाही.** डीलरची नोंदणी फक्त SPIC साठी झाली आहे.
:::

::: section entry-info
title: लकी ड्रॉ एंट्रीची माहिती
routes: /EntryInfo
for: 
roles: 
designations: 
screenshot: entry-info.png
screenshot-phone: entry-info.phone.jpg
minutes: 1
source-hash: 24f199e7d7d10b2f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Entry Information** स्क्रीनवर लकी ड्रॉसाठी एका खरेदीची एंट्री दिसते. ही स्क्रीन अजून तयार होत आहे: त्यावर खऱ्या एंट्री नाहीत, फक्त नमुन्याचे आकडे दिसतात.

1. वर एंट्री नंबर आणि तिची स्थिती पाहा.
2. एंट्री कार्डमध्ये **Date**, **Dealer Name**, **Source**, **Location**, **Product** आणि **Purchase Amount** पाहा.
3. **LUCKY DRAW INFO** खाली एंट्री पात्र आहे का, आणि सध्याचे चक्र (cycle) कुठपर्यंत आले आहे ते पाहा.
4. **Verification** खाली **Invoice Number** पाहा, आणि एंट्री मोजली गेली आहे का ते पाहा.
5. मागे जाण्यासाठी वरचा मागचा बाण दाबा.

## नेहमीच्या अडचणी

- **आकडे कधीच बदलत नाहीत.** सध्या ही स्क्रीन फक्त नमुन्याचे आकडे दाखवते.
:::
