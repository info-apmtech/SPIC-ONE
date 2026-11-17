---
module: farm-operations
title: फार्मचे कामकाज
icon: bi-tree
order: 109
language: mr
category: माती परीक्षण
note: नवीन मार्गदर्शक
meta-source-hash: 4c57dbd8a4469315
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 62c60a0ea650a93e
confidence: ok

SPIC फार्मचा सगळा हिशोब एकाच ठिकाणी ठेवा. प्रॉडक्ट आणि त्यांचे दर ठरवा, दर 15 दिवसांनी स्टॉक, विक्री आणि खर्च नोंदवा, आणि पूर्ण वर्षाचे उत्पन्न, खर्च, नफा आणि स्टॉक पाहा.

- products | icon: bi-tags | color: #15803d
  title: प्रॉडक्ट आणि दर
  text: Farm Office नवीन प्रॉडक्ट किंवा नवीन दर मागते. Farm Admin त्याला मंजुरी देतात.
- report | icon: bi-journal-text | color: #d97706
  title: दर 15 दिवसांनी एक रिपोर्ट
  text: 1 ते 15 आणि 16 ते महिन्याच्या शेवटपर्यंत, इन्व्हेंटरी, उत्पन्न आणि खर्च भरा, मग सबमिट करा.
- summaries | icon: bi-graph-up-arrow | color: #2563eb
  title: वर्षाचे सारांश
  text: फायनान्स, इन्व्हेंटरी, उत्पन्न आणि खर्चाचे सारांश पाहा, आणि ते Excel मध्ये डाउनलोड करा.
:::

::: section flow
title: फार्मचा हिशोब कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 4948b5894215c5ba
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- office | type: lane | color: #15803d
  title: Farm Office
  sub: विनंत्या आणि 15 दिवसांचे रिपोर्ट
- admin | type: lane | color: #ea580c
  title: Farm Admin
  sub: मंजुऱ्या आणि Product Master
- request | lane: office | col: 1 | icon: bi-tags | color: #d97706 | next: approve | screen: product-rates
  title: प्रॉडक्ट किंवा दर मागा
  status: Pending
  text: **Product & Rate Management** वर नवीन प्रॉडक्ट किंवा दरात बदल मागा. ती विनंती Farm Admin कडे थांबते.
- approve | lane: admin | col: 2 | icon: bi-patch-check | color: #16a34a | next: entry | return: request | screen: rate-approvals
  title: दराला मंजुरी द्या
  status: Approved
  text: Farm Admin किंमत आणि युनिट तपासतात आणि मंजुरी देतात. मग तो प्रॉडक्ट आणि दर 15 दिवसांच्या रिपोर्टमध्ये वापरले जातात.
- rejected | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: approve | to: request | screen: rate-approvals
  title: विनंती नाकारली
  status: Rejected
  text: Farm Admin कारण लिहून ती नाकारतात. Farm Office विनंती बदलून **Resubmit Request** दाबू शकते.
- entry | lane: office | col: 3 | icon: bi-journal-text | color: #64748b | next: submit | screen: fifteen-day-entry
  title: 15 दिवसांचा रिपोर्ट भरा
  status: Draft
  text: त्या कालावधीची इन्व्हेंटरी, उत्पन्न आणि खर्च भरा. प्रत्येक टप्पा ड्राफ्ट म्हणून सेव्ह होतो.
- submit | lane: office | col: 4 | icon: bi-send-check | color: #15803d | next: summaries | screen: report-review
  title: तपासा आणि सबमिट करा
  status: Submitted
  text: रिव्ह्यू पेज तपासा आणि **Submit 15-Day Report** दाबा. सबमिट केलेला रिपोर्ट कधीही उघडता येतो.
- summaries | lane: office | col: 5 | icon: bi-graph-up-arrow | color: #2563eb | screen: finance-summary
  title: सारांश पाहा
  text: फायनान्स, इन्व्हेंटरी, उत्पन्न आणि खर्चाच्या सारांशात आर्थिक वर्षातले सगळे रिपोर्ट एकत्र जोडले जातात.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: e9a76a64316c65fc
confidence: ok

- no-request | color: #64748b
  label: No Request
  text: या प्रॉडक्टसाठी कोणतीही विनंती नाही. त्याचा सध्याचा दरच वापरला जातो.
- pending | color: #d97706
  label: Pending
  text: विनंती Farm Admin कडे थांबलेली आहे.
- approved | color: #16a34a
  label: Approved
  text: Farm Admin ने मंजुरी दिली. नवीन प्रॉडक्ट किंवा दर वापरात आहे.
- rejected | color: #dc2626
  label: Rejected
  text: Farm Admin ने नाकारली. कारण वाचा, बदल करा आणि परत सबमिट करा.
- active | color: #15803d
  label: Active
  text: हा प्रॉडक्ट Product Master मध्ये वापरात आहे.
- inactive | color: #475569
  label: Inactive
  text: हा प्रॉडक्ट बंद केला आहे. नवीन विनंत्यांमध्ये तो दिसत नाही.
- draft | color: #d97706
  label: Draft
  text: 15 दिवसांचा रिपोर्ट सेव्ह केला आहे पण सबमिट केलेला नाही. तुम्ही अजूनही त्यात बदल करू शकता.
- submitted | color: #16a34a
  label: Submitted
  text: 15 दिवसांचा रिपोर्ट अंतिम झाला आहे. फक्त ॲडमिनच तो परत उघडू शकतात.
- ready-to-submit | color: #2563eb
  label: Ready to submit
  text: रिव्ह्यू टप्प्यावर याचा अर्थ रिपोर्ट तयार आहे. Finance Summary वर याचा अर्थ काही रिपोर्ट अजून सबमिट झालेले नाहीत.
- ready-update | color: #d97706
  label: Ready for Update
  text: सध्याचा कालावधी नोंदी भरण्यासाठी खुला आहे.
- completed | color: #16a34a
  label: Completed
  text: मागचा कालावधी, इन्व्हेंटरी टप्प्यावर दिसतो.
- upcoming | color: #7c3aed
  label: Upcoming
  text: पुढचा कालावधी, जो हा संपल्यावर उघडतो.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 6c6e07dec3a74424
confidence: ok

तुमचे डेझिग्नेशन परवानगी देते त्याच स्क्रीन तुम्हाला दिसतात.

- office | color: #15803d
  title: Farm Office
  - नवीन प्रॉडक्ट आणि दरातले बदल मागा
  - 15 दिवसांचा रिपोर्ट भरा आणि सबमिट करा
  - फायनान्स, इन्व्हेंटरी, उत्पन्न आणि खर्चाचे सारांश तपासा
- admin | color: #ea580c
  title: Farm Admin
  - प्रॉडक्ट आणि दराच्या विनंत्या मंजूर करा किंवा नाकारा
  - Product Master मध्ये प्रॉडक्ट आणि दर दुरुस्त करा
  - प्रत्येक प्रॉडक्टच्या दराचा इतिहास पाहा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9b23a5f40e2ecf43
confidence: ok

- no-product | popular: true
  q: माझ्या 15 दिवसांच्या रिपोर्टमध्ये एक प्रॉडक्ट दिसत नाही
  a: फक्त मंजूर झालेले प्रॉडक्टच दिसतात. **Product & Rate Management** वर तो मागा आणि Farm Admin मंजुरी देईपर्यंत थांबा.
- change-submitted | popular: true
  q: मी रिपोर्ट सबमिट केला आणि नंतर त्यात चूक सापडली
  a: सबमिट केलेला रिपोर्ट बदलता येत नाही. तुमच्या ॲडमिनला तो परत उघडायला सांगा.
- sold-locked
  q: इन्व्हेंटरी टप्प्यावर मला Sold कॉलममध्ये टाइप करता येत नाही
  a: **Sold** हा आकडा तुम्ही **Income** टप्प्यावर भरलेल्या **Sold Quantity** मधून येतो. तिथेच तो बदला.
- no-approvals
  q: Pending Approvals ची लिस्ट रिकामी आहे, पण Farm Office ने विनंत्या पाठवल्या आहेत
  a: **From Date** आणि **To Date** फिल्टर आजच्या तारखेपासून सुरू होतात. आधीच्या तारखा निवडा आणि **Apply Filters** दाबा.
- cannot-edit
  q: "This product is still awaiting approval - you can't edit it yet." (हा प्रॉडक्ट अजून मंजुरीच्या प्रतीक्षेत आहे, आत्ता बदलता येणार नाही) असे दिसते
  a: Farm Admin मंजुरी देईपर्यंत नवीन प्रॉडक्ट बदलता येत नाही. त्यांच्या निर्णयाची वाट पाहा.
:::

::: section farm-dashboard
title: फार्मची सगळी माहिती एका नजरेत पाहा
routes: /FarmDashboard, /FarmOperations
for: 
roles: 
designations: 
minutes: 2
source-hash: 46e18c8ee7fa0ff8
confidence: ok

**ही स्क्रीन कशासाठी आहे**
फार्म डॅशबोर्डवर (मेनूमध्ये **Farm Operations**) निवडलेल्या आर्थिक वर्षाचे पैसे आणि स्टॉक दिसतात, आणि इथून तुम्ही फार्मच्या बाकी सगळ्या स्क्रीनवर जाऊ शकता.

1. वर उजवीकडे **Financial Year** निवडा.
2. कार्डे वाचा: **Total Budget**, **Total Income Till Date**, **Total Expense Till Date**, **Net Profit Till Date** आणि **ROI Till Date**.
3. **Quick Action** खाली ती स्क्रीन उघडण्यासाठी **Product Management**, **Inventory Summary**, **Income Summary**, **Expense Summary** किंवा **Finance Summary** टॅप करा.
4. **Next 15-day Period** मध्ये **Start 15 Days Entry** दाबा. तुम्ही ड्राफ्ट सेव्ह केला असेल तर बटणावर **Continue 15 Days Entry** असे लिहिलेले असते.
5. **Income, Expense & Profit Trends** मध्ये **Last 6 Periods**, **Last 3 Periods** किंवा **All Periods** निवडा. एखाद्या बारचे आकडे पाहण्यासाठी त्यावर पॉइंटर ठेवा.
6. **Latest Stock Position** मध्ये प्रत्येक प्रॉडक्टचा शेवटचा क्लोजिंग स्टॉक दिसतो. प्रॉडक्ट शोधा, किंवा इन्व्हेंटरी सारांश उघडण्यासाठी **View All** दाबा.
7. **Income Overview** आणि **Top Expense Categories** मध्ये पैसे कुठून आले आणि कुठे गेले ते दिसते. सारांश उघडण्यासाठी **View Details** दाबा.

> Tip: ROI म्हणजे निव्वळ नफा भागिले बजेट.

## नेहमीच्या अडचणी

- **"No report history yet".** (अजून कोणताही रिपोर्ट नाही) आधी 15 दिवसांचा एक रिपोर्ट सबमिट करा. त्यानंतर चार्ट भरतो.
- **"Could not load dashboard data".** (डॅशबोर्डचा डेटा आला नाही) तुमचे इंटरनेट तपासा आणि **Retry** दाबा.
- **सगळे आकडे शून्य आहेत.** बरोबर **Financial Year** निवडले आहे का ते तपासा.
:::

::: section product-rates
title: नवीन प्रॉडक्ट किंवा दरात बदल मागा
routes: /ProductRateManagement
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-prm-tabs] | 2 > [data-help=fo-prm-status] | 3 > [data-help=fo-prm-search] | 4 > [data-help=fo-prm-actions] | 5 > [data-help=fo-prm-new]
minutes: 3
source-hash: e137e022776b0912
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Product & Rate Management** वर फार्मचा प्रत्येक प्रॉडक्ट त्याच्या सध्याच्या दरासह आणि त्याच्या शेवटच्या विनंतीसह दिसतो. Farm Admin कडे नवीन प्रॉडक्ट किंवा नवीन दर मागण्यासाठी ही स्क्रीन वापरा.

1. एक टॅब निवडा: **All Product**, **Livestock** किंवा **Orchard**.
2. फक्त त्याच विनंत्या पाहण्यासाठी **Pending** किंवा **Approved** टॅप करा. **All** मध्ये सगळे प्रॉडक्ट दिसतात.
3. प्रॉडक्ट पटकन शोधण्यासाठी **Search products...** मध्ये टाइप करा.
4. **Action** कॉलममध्ये डोळ्याचा आयकॉन **Request Details** दाखवतो. पेन्सिल दरात बदल मागते, किंवा तुमची **Pending** किंवा **Rejected** विनंती बदलते. डस्टबिन **Pending** किंवा **Rejected** विनंती डिलीट करते.
5. प्रॉडक्ट जोडण्यासाठी **Request New Product** दाबा. **Live Stock** किंवा **Orchard** निवडा, **Product**, **UOM** आणि **Price** भरा, हवे असल्यास **Reason** लिहा, आणि **Request Product** दाबा.

> Tip: मंजुरी मिळाल्यावर तो प्रॉडक्ट आणि दर इन्व्हेंटरी आणि उत्पन्नाच्या नोंदीत दिसतात.
> Careful: डिलीट केलेली विनंती परत आणता येत नाही.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Live Stock** / **Orchard** | प्रॉडक्टचा गट. |
| **Product** | प्रॉडक्टचे नाव, उदा. मेंढी. भरणे आवश्यक. |
| **UOM** | Kg, Unit, Litre किंवा Nos. भरणे आवश्यक. |
| **Price** | एका युनिटचा दर, रुपयांमध्ये. शून्यापेक्षा जास्त असावा. |
| **Reason (Optional)** | तुम्हाला याची गरज का आहे. Farm Admin ला निर्णय घ्यायला मदत होते. |

## नेहमीच्या अडचणी

- **"Enter a valid rate."** (योग्य दर टाका) शून्यापेक्षा जास्त किंमत टाका.
- **"This product is still awaiting approval - you can't edit it yet."** (हा प्रॉडक्ट अजून मंजुरीच्या प्रतीक्षेत आहे) Farm Admin नवीन प्रॉडक्टला मंजुरी देईपर्यंत थांबा.
- **माझ्या विनंतीवर Rejected दिसते.** **Rejection Reason** वाचण्यासाठी डोळ्याच्या आयकॉनने ती उघडा. पेन्सिल टॅप करा, दुरुस्ती करा आणि **Resubmit Request** दाबा.
- **मला Request New Product दिसत नाही.** तुमच्या डेझिग्नेशनला ही स्क्रीन फक्त पाहता येते.
:::

::: section rate-approvals
title: प्रॉडक्ट आणि दराच्या विनंत्या मंजूर करा किंवा नाकारा
routes: /ProductRateApprovals
for: 
roles: 
designations: 
minutes: 3
source-hash: 0eac09c794e59c9c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Pending Approvals** वर Farm Office कडून आलेल्या आणि निर्णयाची वाट पाहणाऱ्या नवीन प्रॉडक्ट आणि दरातल्या बदलाच्या विनंत्या दिसतात. निर्णय झालेली विनंती या लिस्टमधून निघून जाते.

1. **Filters** उघडा. **From Date**, **To Date**, **Request Type**, **Product Group** किंवा **Product** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
2. एक टॅब निवडा: **All Request**, **New Product** किंवा **Rate Change**. आकड्यांवरून किती विनंत्या थांबल्या आहेत ते कळते.
3. विनंती पाहण्यासाठी डोळ्याचा आयकॉन टॅप करा: प्रॉडक्ट, युनिट, सध्याचा दर, **Request Price**, कोणी मागितले आणि **Reason**.
4. मागितलेल्या किमतीलाच मंजुरी द्यायची असेल तर त्या ओळीत **Approve** दाबा आणि खात्री करा.
5. आधी किंमत किंवा युनिट बदलायचे असेल तर पेन्सिल टॅप करा. **Approval Details** खाली **UOM** किंवा **Approved Price** बदला, **Admin Remarks** लिहा, आणि **Approve** दाबा.
6. नाकारण्यासाठी पेन्सिल टॅप करा, **Admin Remarks** मध्ये कारण लिहा आणि **Reject** दाबा, मग खात्री करा.

> Careful: मंजुरी दिल्यावर प्रॉडक्टचा दर लगेच बदलतो. तो पुढच्या 15 दिवसांच्या रिपोर्टमध्ये वापरला जातो.

## नेहमीच्या अडचणी

- **लिस्ट रिकामी आहे.** तारखा आजपासून सुरू होतात. आधीची **From Date** निवडा आणि **Apply Filters** दाबा.
- **"Enter a rejection reason in Admin Remarks."** (Admin Remarks मध्ये नाकारण्याचे कारण लिहा) नाकारण्यासाठी कारण लिहिणे आवश्यक आहे.
- **"Enter a valid approved price."** (योग्य मंजूर किंमत टाका) शून्यापेक्षा जास्त किंमत टाका.
- **Approve बटण दिसत नाही.** तुमच्या डेझिग्नेशनला विनंत्या फक्त पाहता येतात.
:::

::: section product-master
title: Product Master सांभाळा
routes: /FarmProductMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: 858235a42d5c0ca1
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Product Master** म्हणजे फार्मचे सगळे प्रॉडक्ट, त्यांची युनिट आणि सध्याचे दर यांची लिस्ट. मंजूर झालेल्या विनंत्या इथे जोडल्या जातात. Farm Admin इथे थेट प्रॉडक्ट दुरुस्तही करू शकतात.

1. एक टॅब निवडा: **All Product**, **Livestock** किंवा **Orchard**, किंवा प्रॉडक्ट शोधा.
2. **Current Rate** आणि **Status** (**Active** किंवा **Inactive**) तपासा.
3. प्रत्येक मंजूर दर त्याच्या तारखेसह पाहण्यासाठी **View History** दाबा. सगळ्यात नवीन दरावर **Current** असे लिहिलेले असते.
4. प्रॉडक्ट दुरुस्त करण्यासाठी पेन्सिल टॅप करा. **Product Group**, **Product Name**, **UOM** किंवा **Current Rate** बदला.
5. एखादा प्रॉडक्ट वापरणे बंद करायचे असेल तर **Active** वरची खूण काढा. तो दराच्या इतिहासासह लिस्टमध्ये राहतो.
6. **Save Changes** दाबा.

> Careful: इथे केलेला बदल मंजुरीच्या प्रक्रियेतून जात नाही. तो लगेच वापरला जातो.

## नेहमीच्या अडचणी

- **"Enter a product name and UOM."** (प्रॉडक्टचे नाव आणि UOM टाका) दोन्ही भरणे आवश्यक आहे.
- **"No approved rate history yet."** (अजून मंजूर दराचा इतिहास नाही) या प्रॉडक्टसाठी अजून एकही दराची विनंती मंजूर झालेली नाही.
- **पेन्सिल दिसत नाही.** तुमच्या डेझिग्नेशनला Product Master फक्त पाहता येतो.
:::

::: section fifteen-day-entry
title: 15 दिवसांचा रिपोर्ट भरा आणि सबमिट करा
routes: /FifteenDaysEntry, /15daysEntry
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-steps] | 2 > [data-help=fo-period] | 3 > [data-help=fo-inventory] | 4 > [data-help=fo-continue] | 5 > [data-help=fo-continue] | 6 > [data-help=fo-save]
minutes: 6
source-hash: 8adffbba9a65bc8e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
दर 15 दिवसांनी (1 ते 15, आणि 16 ते महिन्याच्या शेवटपर्यंत) फार्म चार टप्प्यांचा एक रिपोर्ट पाठवते: **Inventory**, **Income**, **Expense** आणि **Review**. ही स्क्रीन अजून सबमिट न झालेला पुढचा कालावधी उघडते.

1. **Inventory**, **Income**, **Expense** आणि **Review** यांच्यात जाण्यासाठी वरचे टप्पे वापरा. तुम्ही प्रत्येक वेळी पुढे-मागे गेलात की तुमचे काम ड्राफ्ट म्हणून सेव्ह होते.
2. कालावधीची कार्डे तपासा: **Current Reporting**, **Previous Period** आणि **Next Reporting Period**.
3. **Inventory Entry** वर, **Livestock Sales Ledger** मध्ये प्रत्येक प्रॉडक्टसाठी **Purchase / Old Quantity**, **Hatched / New Breed**, **Mortality** आणि **Others** भरा. **Opening Stock** आणि **Closing Stock** आपोआप भरतात. गरज असेल तर **Remarks** लिहा.
4. **Continue to Income** दाबा. **Income Entry** वर **Livestock Sales Ledger** आणि **Orchard Sales** मध्ये प्रत्येक प्रॉडक्टची **Sold Quantity** भरा. दर हा मंजूर दरच असतो, त्यामुळे **Total amount** आपोआप मोजला जातो. **Continue to Expense** दाबा.
5. **Expense Entry** वर **Expenditure Breakup** आणि **Miscellaneous Expenditure** खाली रकमा भरा. एखादी ओळ नसेल तर **Add New Category** किंवा **Add New Sub Category** वापरा. **Continue to Review** दाबा, सगळे तपासा, **Submit 15-Day Report** दाबा आणि मग **Confirm Submit** दाबा.
6. काम थांबवून नंतर परत यायचे असेल तर कधीही **Save as Draft** दाबा.

> Tip: ब्रेकअपमधले **Miscellaneous** म्हणजे सब कॅटेगरींची बेरीज. त्यात टाइप करता येत नाही.
> Careful: एकदा सबमिट केल्यावर ॲडमिनने परत उघडल्याशिवाय रिपोर्ट बदलता येत नाही.

## सबमिट केल्यानंतर

- **15 - Day Report Submitted Successfully!** स्क्रीनवर एकूण आकडे दिसतात.
- रिपोर्ट उघडण्यासाठी **View Submitted Report** दाबा, वर्षाच्या माहितीसाठी **Go to finance summary** दाबा, किंवा पुढच्या कालावधीसाठी **Enter ... Report** दाबा.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **Purchase / Old Quantity** | या कालावधीत विकत घेतलेली जनावरे किंवा वस्तू. |
| **Hatched / New Breed** | या कालावधीत जन्मलेली किंवा अंड्यातून निघालेली नवीन जनावरे. |
| **Mortality** | या कालावधीत मेलेली जनावरे. |
| **Others** | इतर कोणताही बदल. स्टॉक कमी करण्यासाठी हा आकडा शून्यापेक्षा कमी (मायनस) असू शकतो. |
| **Sold Quantity** | तुम्ही किती विकले. यामुळे इन्व्हेंटरी टप्प्यावरचे **Sold** पण भरते. |
| **Amount (₹)** | या कालावधीत त्या खर्चावर लागलेले पैसे. |

## नेहमीच्या अडचणी

- **"No products in this ledger".** (या लेजरमध्ये एकही प्रॉडक्ट नाही) अजून एकही प्रॉडक्ट मंजूर झालेला नाही. **Product & Rate Management** वर तो मागा.
- **मला Sold मध्ये टाइप करता येत नाही.** **Income** टप्प्यावर **Sold Quantity** भरा.
- **दर चुकीचा आहे.** **Product & Rate Management** वर दरात बदल मागा. इथे तो बदलता येत नाही.
- **"Please enter a category name."** (कॅटेगरीचे नाव टाका) **Add Category** दाबण्याआधी नाव टाइप करा.
:::

::: section report-review
title: सबमिट केलेला 15 दिवसांचा रिपोर्ट उघडा
routes: /FifteenDayReportReview, /15DayReportReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 1abbfd7769868ce9
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**15 - Day Report Review** स्क्रीनवर सबमिट केलेला एक रिपोर्ट पूर्ण दिसतो आणि त्याची मागच्या कालावधीशी तुलना होते.

1. सबमिट केल्यावर **View Submitted Report** ने, किंवा **Finance Summary** वरच्या डोळ्याच्या आयकॉनने ती उघडा.
2. वर कालावधी, स्थिती आणि तो कोणी सबमिट केला ते पाहा.
3. कार्डे वाचा: **Total Income**, **Total Expense**, **Net Profit** आणि **Total Products**. बाणांवरून मागच्या 15 दिवसांशी तुलना कळते.
4. प्रत्येक भाग उघडण्यासाठी किंवा बंद करण्यासाठी **Inventory Summary**, **Income Summary** किंवा **Expense Summary** टॅप करा.
5. खाली **Financial Summary** आणि **Comparison with Previous** तपासा.
6. परत जाण्यासाठी **Back to Finance Summary List** दाबा.

## नेहमीच्या अडचणी

- **"No previous period to compare".** (तुलना करायला मागचा कालावधी नाही) हा आर्थिक वर्षातला पहिला रिपोर्ट आहे.
- **"No inventory records found for this period."** (या कालावधीसाठी इन्व्हेंटरीची नोंद नाही) त्या रिपोर्टसाठी कोणताही स्टॉक भरला गेला नव्हता.
:::

::: section finance-summary
title: वर्षाचा फायनान्स सारांश पाहा
routes: /FinanceSummary, /FinanceSummaryOverall
for: 
roles: 
designations: 
minutes: 2
source-hash: d2b850b02c31e1d6
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Finance Summary (Overall)** मध्ये एका आर्थिक वर्षाचे सगळे 15 दिवसांचे रिपोर्ट एकत्र जोडले जातात आणि प्रत्येक रिपोर्टची लिस्ट दिसते.

1. **Financial Year** आणि **View** निवडा: **15-Day View**, **Monthly View** किंवा **Quarterly View**.
2. कार्डे वाचा: **Annual Budget**, **Total Income**, **Total Expense**, **Net Profit** आणि **ROI**.
3. **Analytics Validation Trend** मध्ये **Last 6 Months**, **Last 3 Months** किंवा **Last 1 Year** निवडा.
4. रिपोर्टच्या इतिहासात लिस्ट लहान करण्यासाठी **From Date** आणि **To Date** ठरवा.
5. **Status** कॉलममध्ये **Submitted** किंवा **Draft** दिसते. रिपोर्ट उघडण्यासाठी डोळ्याचा आयकॉन टॅप करा.
6. लिस्ट सेव्ह करण्यासाठी **Download Excel** दाबा.

## नेहमीच्या अडचणी

- **"No report records found for the selected filter."** (निवडलेल्या फिल्टरसाठी एकही रिपोर्ट नाही) तारखा वाढवा किंवा दुसरे आर्थिक वर्ष निवडा.
- **वरच्या बॅजवर Ready to submit असे लिहिले आहे.** वर्षातला किमान एक रिपोर्ट अजून ड्राफ्ट आहे.
:::

::: section inventory-summary
title: प्रत्येक प्रॉडक्टच्या स्टॉकची हालचाल पाहा
routes: /InventorySummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 7b39d227504cc4c7
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Inventory Summary** मध्ये प्रत्येक 15 दिवसांच्या कालावधीत प्रत्येक प्रॉडक्टचा स्टॉक कसा बदलला ते दिसते.

1. **Filters** उघडा. **From Date**, **To Date**, **Category** आणि **Product** निवडा, मग **Apply Filters** दाबा. **Clear All** ने फिल्टर निघतात.
2. एखादा प्रॉडक्ट पाहण्यासाठी त्याचा टॅब टॅप करा.
3. कार्डे वाचा: **Total Purchased**, **Total Hatched**, **Total Sold**, **Mortality** आणि **Closing Stock**.
4. **Stock Movement History** टेबलमध्ये क्रम लावण्यासाठी कॉलमचे नाव टॅप करा. अजून ड्राफ्टमध्ये असलेल्या कालावधीवर **Draft** असा टॅग असतो.
5. टेबल सेव्ह करण्यासाठी **Download Excel** किंवा **Download PDF** दाबा.

## नेहमीच्या अडचणी

- **"From Date cannot be later than To Date."** (From Date ही To Date नंतरची असू शकत नाही) तारखा परत निवडा.
- **"No products found".** (एकही प्रॉडक्ट सापडला नाही) आधी Product Master मध्ये प्रॉडक्ट जोडले गेले पाहिजेत.
:::

::: section income-summary
title: उत्पन्नाचा सारांश पाहा
routes: /IncomeSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: d11284cff39730df
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Income Summary** मध्ये प्रत्येक रिपोर्टिंग कालावधीतले फार्मच्या विक्रीचे उत्पन्न दिसते, पशुधन (livestock) आणि फळबाग (orchard) असे वेगवेगळे.

1. **Filters** उघडा. **Period Type** (**15 Days** किंवा **1 Month**), **From Date**, **To Date** आणि **Income Category** निवडा, मग **Apply Filters** दाबा.
2. कार्डे वाचा: **Total Income Till Date**, **Sales Records**, **Products Covered** आणि **Reporting Period**. शेवटच्या कार्डवर शेवटचा कालावधी अपडेट करायला किती दिवस उरले ते दिसते.
3. **Livestock Sales Ledger** टेबलमध्ये प्रत्येक कालावधीचे **Livestock Income**, **Orchard Income**, **Total Income** आणि **Status** पाहा.
4. त्या कालावधीतला प्रत्येक प्रॉडक्ट, संख्या, दर आणि उत्पन्न पाहण्यासाठी **View** मधला डोळ्याचा आयकॉन टॅप करा.
5. प्रत्येक प्रॉडक्टचा वाटा पाहण्यासाठी **Live Income Overview** आणि **Orchard Income Overview** पाहा.
6. टेबल सेव्ह करण्यासाठी **Download Excel** दाबा.

## नेहमीच्या अडचणी

- **"Editing window closed".** (बदल करण्याची मुदत संपली) शेवटचा कालावधी अपडेट करण्याची वेळ संपली आहे.
- **"Could not load income data".** (उत्पन्नाचा डेटा आला नाही) **Retry** दाबा.
:::

::: section expense-summary
title: खर्चाचा सारांश पाहा
routes: /ExpenseSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 9200f1d0c1608503
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Expense Summary** मध्ये प्रत्येक रिपोर्टिंग कालावधीतला फार्मचा खर्च आणि सगळ्यात जास्त खर्चाच्या कॅटेगरी दिसतात.

1. **Filters** उघडा. **Period Type**, **From Date**, **To Date** आणि **Expense Category** निवडा, मग **Apply Filters** दाबा.
2. कार्डे वाचा: **Total Expense**, **Highest Expense Category**, **Expense Categories** आणि **Summary Coverage**.
3. **15 - Day Expense Summary** मध्ये प्रत्येक कालावधीचा **Total Expense (₹)** आणि **Status** पाहा.
4. त्या कालावधीचा **Expenditure Breakup (Farm Operations)** आणि **Miscellaneous Expenditure (Sub-Categories)** पाहण्यासाठी **View** मधला डोळ्याचा आयकॉन टॅप करा.
5. तुलना करण्यासाठी **Monthly Expense Trend** आणि **Expense Category Overview** पाहा.
6. टेबल सेव्ह करण्यासाठी **Download Excel** दाबा.

## नेहमीच्या अडचणी

- **"No expense data yet".** (अजून खर्चाचा डेटा नाही) आधी खर्चासह 15 दिवसांचा एक रिपोर्ट सबमिट करा.
- **"Could not load expense data".** (खर्चाचा डेटा आला नाही) **Retry** दाबा.
:::
