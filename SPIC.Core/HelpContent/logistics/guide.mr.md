---
module: logistics
title: लॉजिस्टिक्स
icon: bi-truck
order: 113
language: mr
category: सुविधा
note: नवीन मार्गदर्शक
meta-source-hash: a51949592e8c1854
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 616b9e8fbedcbace
confidence: ok

SPIC आणि GREEN STAR वापरत असलेल्या गोदामांची (warehouse), रेक पॉइंटची आणि बंदरांची (port) एकच लिस्ट ठेवा. फील्ड स्टाफ प्रत्येक जागा तिच्या लोकेशन आणि कागदपत्रांसह जोडतात, आणि RM, SMM आणि AVP एकामागून एक ती मंजूर करतात.

- add | icon: bi-building-add | color: #475569
  title: फील्ड स्टाफ नोंद जोडतात
  text: MO, MDO किंवा JMDO गोदाम किंवा रेक पॉइंट त्याचा SAP Code, पत्ता, नकाशावरची जागा आणि कागदपत्रांसह जोडतात.
- approve | icon: bi-shield-check | color: #d97706
  title: तीन मंजुऱ्या
  text: आधी RM, मग SMM, मग AVP नोंद तपासतात. प्रत्येक जण ती मंजूर करू शकतो किंवा शेरा लिहून परत पाठवू शकतो.
- report | icon: bi-bar-chart-line | color: #2563eb
  title: एकूण आकडे पाहा
  text: रिपोर्टमध्ये राज्यानुसार किती नोंदी बाकी आहेत आणि किती पूर्ण झाल्या ते दिसते.
:::

::: section flow
title: नोंद कशी पुढे जाते, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: caf925a727761bcf
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- admin | type: lane | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  sub: SAP Code ची लिस्ट
- maker | type: lane | color: #475569 | roles: MO, MDO, JMDO
  title: MO / MDO / JMDO
  sub: फील्ड स्टाफ
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: तुमचा रिजन
- smm | type: lane | color: #c026d3 | roles: SMM, SMD
  title: SMM
  sub: तुमचे राज्य
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin
  title: AVP
  sub: अंतिम मंजुरी
- sap | lane: admin | col: 1 | icon: bi-upc-scan | color: #64748b | next: add | screen: sap-master
  title: SAP Code लोड करा
  text: Admin PVT Master आणि Rake Point Master च्या Excel फाइल अपलोड करतात आणि प्रत्येक SAP Code एका राज्याशी जोडतात.
- add | lane: maker | col: 2 | icon: bi-building-add | color: #475569 | next: rm-check | screen: add-record
  title: नोंद जोडा
  text: SAP Code निवडा, तपशील, लोकेशन आणि कागदपत्रे भरा, आणि **Save** दाबा. नोंद RM कडे जाते.
- rm-check | lane: rm | col: 3 | icon: bi-hourglass-split | color: #d97706 | next: smm-check | return: add | screen: approve-records
  title: RM तपासतात
  status: Pending RM
  text: RM **Approve to SMM** दाबतात, किंवा MO, MDO किंवा JMDO कडे परत पाठवण्यासाठी **Send Back** दाबतात.
- smm-check | lane: smm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: final | return: rm-check | screen: approve-records
  title: SMM तपासतात
  status: Pending SMM
  text: SMM **Approve to AVP** दाबतात, किंवा RM कडे परत पाठवण्यासाठी **Send Back** दाबतात.
- final | lane: avp | col: 5 | icon: bi-patch-check | color: #16a34a | return: smm-check | screen: approve-records
  title: अंतिम मंजुरी
  status: Approved
  text: AVP **Final Approve** दाबतात, किंवा SMM कडे परत पाठवण्यासाठी **Send Back** दाबतात. मंजुरी मिळाली की नोंद पूर्ण होते.
- sentback | lane: maker | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: add | screen: manage-records
  title: दुरुस्त करा आणि परत सेव्ह करा
  status: Sent Back to MO
  text: **Remarks** वाचा, पेन्सिल दाबा, नोंद दुरुस्त करा आणि **Update** दाबा. ती परत RM कडे जाते.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c35bc2efe348d9c7
confidence: ok

**Approval Status** कॉलममध्ये गोदाम किंवा रेक पॉइंट आता कुठे आहे ते दिसते. **Status** कॉलममध्ये **Active** किंवा **Inactive** दिसते.

- draft | color: #64748b
  label: Draft
  text: MO, MDO किंवा JMDO नसलेल्या व्यक्तीने सेव्ह केली आहे. ती मंजुरीच्या साखळीत नाही.
- pending-rm | color: #d97706
  label: Pending RM
  text: रिजनच्या RM ची वाट पाहत आहे.
- pending-smm | color: #d97706
  label: Pending SMM
  text: RM ने मंजूर केली. राज्याच्या SMM ची वाट पाहत आहे.
- pending-avp | color: #d97706
  label: Pending AVP
  text: SMM ने मंजूर केली. अंतिम मंजुरीची वाट पाहत आहे.
- approved | color: #16a34a
  label: Approved
  text: तिन्ही मंजुऱ्या झाल्या.
- back-mo | color: #dc2626
  label: Sent Back to MO
  text: RM ने परत पाठवली. ज्या MO, MDO किंवा JMDO ने ती बनवली त्यांनी ती दुरुस्त करायची आहे.
- back-rm | color: #dc2626
  label: Sent Back to RM
  text: SMM ने RM कडे परत पाठवली.
- back-smm | color: #dc2626
  label: Sent Back to SMM
  text: AVP ने SMM कडे परत पाठवली.
- active | color: #0d9488
  label: Active
  text: जागा वापरात आहे. लोकेशनचा तपशील आणि दिलेली कागदपत्रे आवश्यक आहेत.
- inactive | color: #475569
  label: Inactive
  text: जागा आता वापरात नाही. कमी तपशील लागतो.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 8acdb7d7714a0307
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- maker | color: #475569 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - गोदाम आणि रेक पॉइंट जोडा
  - GST, विमा आणि लायसन्सची कागदपत्रे अपलोड करा
  - परत पाठवलेल्या नोंदी दुरुस्त करा
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - तुमच्या रिजनच्या नोंदी तपासा
  - SMM कडे मंजूर करा किंवा बनवणाऱ्याकडे परत पाठवा
- smm | color: #c026d3 | roles: SMM, SMD
  title: SMM
  - तुमच्या राज्याच्या नोंदी तपासा
  - AVP कडे मंजूर करा किंवा RM कडे परत पाठवा
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP, Director
  - अंतिम मंजुरी द्या किंवा SMM कडे परत पाठवा
  - बंदरे (port) जोडा आणि बदला
- admin | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  - SAP Code च्या लिस्ट अपलोड करा आणि त्या राज्यांशी जोडा
  - अंतिम मंजुरी द्या
  - लॉजिस्टिक्स रिपोर्ट पाहा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7662caacafa8b468
confidence: ok

- no-sap-code | popular: true
  q: माझा SAP Code लिस्टमध्ये नाही
  a: तुम्ही निवडलेल्या **State** शी जोडलेले कोडच लिस्टमध्ये दिसतात. Admin ला तो कोड अपलोड करायला किंवा **PVT Master Management** स्क्रीनवर तुमच्या राज्याशी जोडायला सांगा.
- sent-back | popular: true
  q: माझ्या गोदामावर "Sent Back to MO" दिसते. मी काय करू?
  a: **Remarks** कॉलम वाचा. त्या ओळीवरची पेन्सिल दाबा, नोंद दुरुस्त करा आणि **Update** दाबा. ती परत RM कडे **Pending RM** म्हणून जाते.
- no-approve
  q: मी RM आहे पण मला Approve बटण दिसत नाही
  a: तुमच्या लेव्हलवर वाट पाहणाऱ्या नोंदींसाठीच बटण दिसते. मंजुरीच्या फिल्टरमध्ये तुमची लेव्हल निवडा, जसे **Pending RM**. RM ला फक्त त्यांच्याच रिजनच्या नोंदी दिसतात.
- save-error
  q: Save केल्यावर फील्डखाली लाल मेसेज दिसतात
  a: **Active** नोंदीसाठी पूर्ण लोकेशन, संपर्क नंबर आणि आवश्यक कागदपत्रे लागतात. लाल खूण असलेले प्रत्येक फील्ड भरा, किंवा जागा वापरात नसेल तर **Status** **Inactive** करा.
- duplicate
  q: SAP Code आधीच आहे असे दिसते
  a: दुसरे गोदाम किंवा रेक पॉइंट तो कोड आधीच वापरत आहे. लिस्टमध्ये तो शोधा. एकच जागा दोनदा जोडू नका.
:::

::: section add-record
title: गोदाम किंवा रेक पॉइंट जोडा
routes: /Logistics
for: 
roles: 
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-basic] | 3 > [data-help=lg-location] | 4 > [data-help=lg-docs] | 5 > [data-help=lg-save]
minutes: 5
source-hash: c9ec25daaeef4964
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Logistics Master** स्क्रीनवर वर एक फॉर्म असतो आणि खाली सेव्ह केलेल्या नोंदींची लिस्ट असते. गोदाम किंवा रेक पॉइंट जोडण्यासाठी फॉर्म वापरा.

1. टॅब निवडा: **Warehouse** किंवा **Rake Point**.
2. **Basic Information** मध्ये **SPIC**, **GREEN STAR** किंवा दोन्ही टॅप करा. **State** निवडा, मग **SAP Code** निवडा; **Name** आपोआप भरते. **Operated By** निवडा. गोदाम असेल तर **Warehouse Category** (**PVT WH**, **C&F WH** किंवा **Both**), **WH Type** निवडा, आणि **Reservation Quantity** बॉक्स भरा.
3. **Primary Location** मध्ये **Use Current Location** दाबा, नकाशावर टॅप करा, किंवा **Google Map URL** पेस्ट करा. मग पत्ता, **Pincode** आणि संपर्क नंबर भरा.
4. **Documents** मध्ये प्रत्येक **Choose ... File** बटण दाबा आणि फाइल निवडा. तपासण्यासाठी डोळ्याचा आयकॉन वापरा. **Active** नोंदीसाठी * खूण असलेली फील्ड आवश्यक आहेत.
5. **Save** दाबा. MO, MDO किंवा JMDO ने सेव्ह केल्यावर नोंद RM कडे **Pending RM** म्हणून जाते.

> Tip: कागद स्कॅन करून अपलोड करण्याआधी त्यावरचा गोदामाचा पत्ता हायलायटरने ठळक करा.
> Careful: फॉर्म रिकामा करायचा असेल तरच **Cancel** दाबा. काहीही सेव्ह होत नाही.

| फील्ड | काय भरायचे |
|---|---|
| **Status** | जागा आता वापरात असेल तर **Active**, नाहीतर **Inactive**. |
| **Operated By** | **Dealer** किंवा **Others**. |
| **WH Type** | **Field WH** किंवा **Rake Point WH** (फक्त PVT WH आणि Both साठी). |
| **Approved Reservation Quantity (MT)** | कंपनीसाठी राखून ठेवलेली जागा, टनांमध्ये. |
| **Latitude & Longitude** | स्वल्पविरामाने वेगळे केलेले दोन आकडे, उदा. 10.7905, 78.7047. |
| **Pincode** | 6 अंकी. **State** आणि **District** आपोआप भरू शकतात. |

## नेहमीच्या अडचणी

- **SAP Code ची लिस्ट रिकामी आहे.** निवडलेल्या **State** शी अजून एकही कोड जोडलेला नाही. Admin ला विचारा.
- **"Primary Location is required".** (Primary Location आवश्यक आहे) **Active** नोंदीसाठी नकाशावरची जागा किंवा Google Map URL आणि पूर्ण पत्ता लागतो.
- **"... saved, but one or more documents failed to upload".** (नोंद सेव्ह झाली, पण एक किंवा जास्त कागदपत्रे अपलोड झाली नाहीत) नोंद सेव्ह झाली आहे. तिच्यावरची पेन्सिल दाबा आणि कागदपत्रे परत अपलोड करा.
- **State, Region किंवा HQ राखाडी आहे.** हे तुमच्या लॉगिनवरून भरले जातात आणि बदलता येत नाहीत.
:::

::: section approve-records
title: नोंद मंजूर करा किंवा परत पाठवा
routes: /Logistics
for: 
roles: RM, RMD, SMM, SMD, AVP, Director, Admin, CorporateAdmin
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-approval-filter] | 3 > [data-help=lg-search] | 4 > [data-help=lg-list]
minutes: 2
source-hash: 5b300e3e278e2af7
confidence: ok

**ही स्क्रीन कशासाठी आहे**
फॉर्मच्या खाली तुमच्या भागातल्या गोदामांची आणि रेक पॉइंटची लिस्ट दिसते. तुमची वाट पाहणाऱ्या नोंदींच्या **Action** कॉलममध्ये मंजुरीचे आणि **Send Back** बटण असते.

1. **Warehouse** किंवा **Rake Point** टॅब निवडा.
2. मंजुरीच्या फिल्टरमध्ये तुमची लेव्हल निवडा: **Pending RM**, **Pending SMM** किंवा **Pending AVP**.
3. नोंद शोधण्यासाठी सर्च बॉक्समध्ये नाव किंवा SAP Code टाइप करा. ती तपासण्यासाठी **Docs** कॉलममधले **View** आणि PDF बटण वापरा.
4. **Approve to SMM**, **Approve to AVP** किंवा **Final Approve** दाबा (तुमच्या लेव्हलचे बटण दिसते). **Approval Remarks** लिहा आणि **Confirm & Proceed** दाबा. परत पाठवायचे असेल तर **Send Back** दाबा, **Send Back Remarks** लिहा आणि **Confirm Send Back** दाबा.

> Tip: मंजूर करताना आणि परत पाठवताना, दोन्ही वेळी शेरा लिहिणे आवश्यक आहे. तो **Remarks** कॉलममध्ये दिसतो.
> Careful: **Send Back** ने नोंद डिलीट होत नाही. ती फक्त एक लेव्हल मागे जाते.

## नेहमीच्या अडचणी

- **मंजुरीचे बटण नाही.** नोंद तुमच्या लेव्हलवर नाही. तिचा **Approval Status** तपासा.
- **"Approval remarks are required."** (मंजुरीचा शेरा आवश्यक आहे) खात्री करण्याआधी शेरा टाइप करा.
- **मला एखादी नोंद दिसत नाही.** RM ला फक्त त्यांचा रिजन, SMM ला त्यांचे राज्य आणि AVP ला त्यांचा झोन दिसतो.
:::

::: section manage-records
title: नोंदी शोधा, दुरुस्त करा आणि डाउनलोड करा
routes: /Logistics
for: 
roles: 
designations: 
minutes: 3
source-hash: 13e0dd985a45cb04
confidence: ok

**ही स्क्रीन कशासाठी आहे**
फॉर्मखालच्या लिस्टमध्ये सेव्ह केलेल्या नोंदी दिसतात, आणि वर आकडे असतात: **Total**, **Active** आणि **Inactive**.

1. टॅब निवडा आणि लिस्टच्या वरचे आकडे पाहा.
2. नावाने शोधा, किंवा लिस्ट लहान करण्यासाठी **All Status** आणि मंजुरीचे फिल्टर वापरा.
3. सेव्ह केलेली कागदपत्रे पाहण्यासाठी, डाउनलोड करण्यासाठी किंवा उघडण्यासाठी **Docs** कॉलममधले **View** दाबा. काम झाल्यावर **Close** दाबा.
4. नोंद PDF म्हणून उघडण्यासाठी **PDF** कॉलममधला SPIC किंवा GREEN STAR लोगो दाबा.
5. नोंद फॉर्ममध्ये आणण्यासाठी पेन्सिल दाबा. बदल करा आणि **Update** दाबा.
6. **Status** कॉलममध्ये **Active** किंवा **Inactive** टॅप करून ते बदला.
7. नोंद डिलीट करण्यासाठी डस्टबिन दाबा, मग खात्रीसाठी **Delete** दाबा.
8. लिस्ट डाउनलोड करण्यासाठी लिस्टच्या वर **Excel** दाबा (फोनवर **Export** आणि मग **Excel**).

> Careful: डिलीट केलेली नोंद परत आणता येत नाही.

## नेहमीच्या अडचणी

- **"Sent Back to MO".** (MO कडे परत पाठवली) **Remarks** वाचा, पेन्सिलने नोंद दुरुस्त करा आणि **Update** दाबा. ती परत RM कडे जाते.
- **Docs चे View बटण राखाडी आहे.** त्या नोंदीसाठी एकही कागदपत्र सेव्ह केलेले नाही.
- **लिस्ट रिकामी आहे.** तुमचे फिल्टर खूप मर्यादित असतील. ते परत **All Status** आणि **All Approval Status** वर ठेवा.
:::

::: section ports
title: बंदर (port) जोडा
routes: /Logistics
for: 
roles: Admin, CorporateAdmin, AVP, Director
designations: 
minutes: 1
source-hash: 264444955d15cae7
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Port** टॅबमध्ये बंदरांची लिस्ट असते. हा टॅब फक्त Admin, AVP आणि Director यांनाच दिसतो. बंदरांसाठी मंजुरीचे टप्पे नसतात.

1. **Port** टॅब उघडा.
2. **Port Name** टाका, आणि तुमच्याकडे असेल तर **Google Map or Lat/Long URL** टाका.
3. **State** आणि **District** निवडा.
4. **Active** किंवा **Inactive** निवडा आणि **Save** दाबा.

## नेहमीच्या अडचणी

- **"Port name is required".** (बंदराचे नाव आवश्यक आहे) सेव्ह करण्याआधी नाव, **State** आणि **District** भरा.
:::

::: section reports
title: लॉजिस्टिक्स रिपोर्ट पाहा
routes: /LogisticsReport
for: 
roles: 
designations: 
minutes: 1
source-hash: 206298ea363d8aeb
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Logistics Reports** स्क्रीनवर राज्यानुसार गोदामे आणि रेक पॉइंट मोजलेले दिसतात, आणि सगळ्यात खाली **TOTAL** ओळ असते.

1. **Logistics Master** स्क्रीनवर **Reports** दाबा.
2. **Warehouse Report** किंवा **Rakepoint Report** निवडा.
3. एकच राज्य पाहण्यासाठी **Search by state...** मध्ये राज्याचे नाव टाइप करा.
4. कॉलम वाचा: **Active**, **Inactive**, **Total**, **As per SAP**, **Pending with MO**, **Pending RM**, **Pending SMM**, **Pending with AVP** आणि **Completed**.
5. नवीन आकडे आणण्यासाठी **Refresh** दाबा. परत जाण्यासाठी **Back to Logistics** दाबा.

> Tip: **As per SAP** म्हणजे SAP Code लिस्टमधल्या कोडची संख्या. **Pending with MO** म्हणजे जे कोड अजून जोडलेले नाहीत किंवा अजून मंजुरीला गेलेले नाहीत.

## नेहमीच्या अडचणी

- **"No data available".** (माहिती उपलब्ध नाही) तुमच्या शोधाशी काहीही जुळत नाही. सर्च बॉक्स रिकामा करा किंवा **Refresh** दाबा.
:::

::: section sap-master
title: SAP Code अपलोड करा आणि राज्ये जोडा
routes: /PVTMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 2bc3660a46728597
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**PVT Master Management** स्क्रीनवर SAP Code च्या लिस्ट असतात. याच लिस्टमधून Logistics फॉर्मवरच्या **SAP Code** बॉक्समध्ये कोड येतात. Admin ही स्क्रीन **Logistics Master** स्क्रीनवरच्या **SAP Code** बटणाने उघडतात.

1. **PVT Master** (गोदामे) किंवा **Rake Point** निवडा.
2. Excel चा नमुना डाउनलोड करण्यासाठी **Sample Format** दाबा आणि तो भरा.
3. **Click to upload or drag and drop** भाग दाबा आणि तुमची .xlsx फाइल निवडा.
4. **Upload Result** वाचा: **Total Records**, **Successfully Inserted** आणि **Duplicate/Skipped**, आणि खाली सोडून दिलेल्या ओळी.
5. लिस्टमध्ये एखाद्या ओळीवर **State** दाबा, **State** निवडा आणि **Save State Mapping** दाबा.
6. Logistics वर परत जाण्यासाठी **Back** दाबा.

> Careful: 10 MB किंवा त्यापेक्षा लहान .xlsx फाइल वापरा. आधीच असलेले कोड सोडून दिले जातात.

## नेहमीच्या अडचणी

- **"Please select a valid .xlsx Excel file."** (योग्य .xlsx Excel फाइल निवडा) फाइल .xlsx म्हणून सेव्ह करा आणि परत प्रयत्न करा.
- **एखाद्या कोडवर "Not Mapped" दिसते.** त्या ओळीवर **State** दाबा आणि त्याचे राज्य निवडा. तोपर्यंत फील्ड स्टाफ राज्य निवडल्यावर तो कोड निवडू शकत नाहीत.
- **State बटण दिसत नाही.** फक्त Admin आणि CorporateAdmin जोडणी बदलू शकतात.
:::

::: section old-master
title: जुनी Logistics Master स्क्रीन
routes: /LogisticsMaster
for: 
roles: 
designations: 
minutes: 1
source-hash: 2bc6b07f78791b25
confidence: ok

**ही स्क्रीन कशासाठी आहे**
हा Logistics फॉर्मचा जुना प्रकार आहे, यात **PVT WH**, **C&F WH** आणि **Rake Point** टॅब असतात. नवीन कामासाठी साइड मेन्यूमधली मुख्य **Logistics Master** स्क्रीन वापरा.

1. टॅब निवडा: **PVT WH**, **C&F WH** किंवा **Rake Point**.
2. मुख्य स्क्रीनप्रमाणेच **Basic Information**, **Reservation Quantity**, **Primary Location** आणि **Documents** भरा.
3. **Save** दाबा, किंवा नोंद बदलण्यासाठी किंवा डिलीट करण्यासाठी लिस्टमधली पेन्सिल आणि डस्टबिन वापरा.

## नेहमीच्या अडचणी

- **"Failed to load PVT WH" किंवा "Failed to load C&F WH".** (PVT WH / C&F WH लोड झाले नाही) हे टॅब या स्क्रीनवर चालत नाहीत. मुख्य **Logistics Master** स्क्रीन वापरा.
- **मंजुरीची बटणे नाहीत.** मंजुरी फक्त मुख्य **Logistics Master** स्क्रीनवरच होते.
:::
