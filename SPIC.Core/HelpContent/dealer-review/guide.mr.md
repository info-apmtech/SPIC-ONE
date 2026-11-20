---
module: dealer-review
title: डीलर तपासणी आणि मंजुरी
icon: bi-person-check
order: 102
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
source-hash: c1f3fc37e023de12
confidence: ok

MO ने डीलर अर्ज सबमिट केल्यावर आधी RM, मग SM आणि मग AVP तो तपासतात. प्रत्येक जण तो मंजूर करून क्रेडिट लिमिट सुचवू शकतो, किंवा दुरुस्तीसाठी परत पाठवू शकतो. या मॉड्युलमध्ये सब डीलर आणि कर्मचाऱ्यांच्या मंजुरीच्या स्क्रीन आणि डीलरचे काही रिपोर्ट पण आहेत.

- check | icon: bi-search | color: #6d28d9
  title: तीन लेव्हल तपासतात
  text: आधी RM, मग SM, मग AVP अर्ज उघडून त्याचा प्रत्येक भाग तपासतात.
- limit | icon: bi-cash-coin | color: #d97706
  title: क्रेडिट लिमिटचा निर्णय
  text: RM आणि SM, SPIC आणि GFL साठी क्रेडिट लिमिट सुचवतात. AVP अंतिम लिमिट देतात.
- back | icon: bi-arrow-counterclockwise | color: #dc2626
  title: दुरुस्तीसाठी परत पाठवा
  text: कोणतीही लेव्हल कारण आणि दुरुस्त करायचे भाग लिहून अर्ज एक टप्पा मागे पाठवू शकते.
:::

::: section flow
title: अर्ज कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 1f194f956d1d33e7
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- mo | type: lane | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  sub: अर्ज भरतात
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: रिजनल मॅनेजर
- sm | type: lane | color: #c026d3 | roles: SMD, SMM
  title: SM
  sub: स्टेट मार्केटिंग मॅनेजर
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin, SuperAdmin
  title: AVP
  sub: अंतिम मंजुरी
- office | type: lane | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: फक्त नवीन डीलरसाठी
- submit | lane: mo | col: 1 | icon: bi-send | color: #64748b | next: rm-check
  title: अर्ज सबमिट करा
  status: Submitted
  text: MO डीलर अर्जाचा प्रत्येक भाग भरून तो सबमिट करतात. फक्त पूर्ण भरलेले अर्जच तपासणीच्या लिस्टमध्ये येतात.
- rm-check | lane: rm | col: 2 | icon: bi-person-check | color: #0891b2 | next: sm-check | return: submit | screen: review-approve
  title: RM तपासतात
  status: In RM
  text: RM अर्ज तपासतात, RM क्रेडिट लिमिट टाकतात आणि **Proceed to SM** दाबतात, किंवा तो MO कडे परत पाठवतात.
- sent-back | lane: mo | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: submit | screen: send-back
  title: दुरुस्त करून परत सबमिट करा
  status: Sent Back
  text: तपासणाऱ्याने कारण आणि दुरुस्त करायचे भाग लिहून अर्ज परत पाठवला आहे. आधीची लेव्हल ते दुरुस्त करून अर्ज परत पुढे पाठवते.
- sm-check | lane: sm | col: 3 | icon: bi-clipboard-check | color: #c026d3 | next: avp-check | return: rm-check | screen: review-approve
  title: SM तपासतात
  status: In SMM
  text: SM ला RM क्रेडिट लिमिट दिसते. ते SM क्रेडिट लिमिट टाकतात आणि **Proceed to AVP** दाबतात, किंवा अर्ज RM कडे परत पाठवतात.
- avp-check | lane: avp | col: 4 | icon: bi-patch-check | color: #7c3aed | next: office-ids | return: sm-check | screen: review-approve
  title: AVP मंजूर करतात
  status: In AVP
  text: AVP ला आधीच्या दोन्ही लिमिट दिसतात. ते अंतिम क्रेडिट लिमिट टाकतात आणि **Approve** दाबतात. मग डीलर ॲक्टिव्ह होतो.
- office-ids | lane: office | col: 5 | icon: bi-upc-scan | color: #16a34a | screen: avp-office-ids
  title: डीलर ID टाका
  status: Verified
  text: नवीन डीलरसाठी AVP Office, SPIC आणि GFL चे डीलर ID टाकते. जुन्या डीलरचा अर्ज AVP च्या मंजुरीनंतरच पूर्ण होतो.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 3678b5e01ec8dd65
confidence: ok

- submitted | color: #64748b
  label: Submitted
  text: MO ने अर्ज सबमिट केला आहे.
- in-rm | color: #0891b2
  label: In RM
  text: रिजनल मॅनेजरची वाट पाहत आहे.
- in-smm | color: #c026d3
  label: In SMM
  text: स्टेट मार्केटिंग मॅनेजरची वाट पाहत आहे.
- in-avp | color: #7c3aed
  label: In AVP
  text: AVP च्या अंतिम मंजुरीची वाट पाहत आहे.
- pending | color: #d97706
  label: Pending
  text: तपासणीच्या लिस्टवर: अजून RM, SMM, AVP किंवा AVP Office कडे आहे. तो कोणाकडे आहे ते **Current Stage** कॉलममध्ये दिसते.
- verified | color: #16a34a
  label: Verified
  text: तपासणीच्या लिस्टवर: पूर्ण मंजूर झाला आहे.
- approved | color: #16a34a
  label: Approved
  text: अर्जावर: AVP ने तो मंजूर केला आहे.
- sent-back | color: #dc2626
  label: Sent Back
  text: तपासणीच्या लिस्टवर: तपासणाऱ्याने तो दुरुस्तीसाठी परत पाठवला आहे.
- rejected | color: #dc2626
  label: Rejected
  text: अर्जावर: तपासणाऱ्याने तो परत पाठवला आहे. तो बंद झालेला नाही; दुरुस्तीनंतर तो परत येतो.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 06465e79ce0431ec
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- mo | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  - डीलर अर्ज सबमिट करा
  - परत आल्यावर तो दुरुस्त करा
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - तुमच्या रिजनमधले अर्ज तपासा
  - RM क्रेडिट लिमिट सुचवा
  - MO कडे परत पाठवा
- sm | color: #c026d3 | roles: SMD, SMM
  title: SM
  - तुमच्या राज्यातले अर्ज तपासा
  - SM क्रेडिट लिमिट सुचवा
  - RM कडे परत पाठवा
  - सब डीलर आणि कर्मचाऱ्यांना पहिली मंजुरी द्या
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP
  - अंतिम मंजुरी आणि क्रेडिट लिमिट द्या
  - SM कडे परत पाठवा
  - सब डीलर आणि कर्मचाऱ्यांना अंतिम मंजुरी द्या
- admin | color: #374151 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - AVP सारखी अंतिम मंजुरी देऊ शकतात
  - Credit Limit Sales चे आकडे सांभाळा
  - राज्यनिहाय डीलरचा सारांश पाहा
- office | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - नवीन डीलरसाठी SPIC आणि GFL चे डीलर ID टाका
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 724bb434f0c07f99
confidence: ok

- no-button | popular: true
  q: मी अर्ज उघडला पण Proceed किंवा Send Back बटण दिसत नाही
  a: अर्ज आता तुमच्या लेव्हलवर नाही. तो तुमची वाट पाहत असेल तेव्हाच बटणे दिसतात. वरचे **Status** कार्ड आणि प्रगतीची पट्टी तपासा.
- no-limit-boxes | popular: true
  q: Confirm Proceed विंडोमध्ये क्रेडिट लिमिटचे बॉक्स का दिसत नाहीत?
  a: नवीन डीलरच्या पहिल्या मंजुरीत, बंद (inactive) डीलरसाठी, किंवा डीलरने संमती देऊन क्रेडिट लिमिटशिवाय अर्ज केला असेल तर (तेव्हा लिमिट 0 असते) हे बॉक्स दिसत नाहीत. डीलर SPIC सोबत असेल तरच SPIC चे बॉक्स दिसतात, आणि GFL सोबत असेल तरच GFL चे बॉक्स दिसतात.
- missing
  q: माझ्या तपासणीच्या लिस्टमध्ये एक अर्ज दिसत नाही
  a: लिस्टमध्ये फक्त तुमच्या भागातले पूर्ण भरलेले अर्ज दिसतात. अर्जाचे काही भाग अजून रिकामे असतील तर तो दिसत नाही. MO ला सगळे भाग पूर्ण करायला सांगा.
- remarks
  q: मला "Remarks are required for New Dealer approval." दिसते
  a: नवीन डीलरसाठी प्रत्येक लेव्हलने शेरा लिहिणे आवश्यक आहे. **Remarks** बॉक्समध्ये शेरा टाइप करा आणि परत **Confirm & Proceed** दाबा.
- after-send-back
  q: मी अर्ज परत पाठवल्यावर तो कुठे जातो?
  a: एक लेव्हल मागे. RM तो MO कडे पाठवतात, SM तो RM कडे, आणि AVP तो SM कडे. ते दुरुस्त करतात आणि अर्ज परत तुमच्याकडे येतो.
:::

::: section review-list
title: तपासणीसाठी वाट पाहणारे अर्ज पाहा
routes: /DealerReviewList
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-list-cards] | 2 > [data-help=dr-list-search] | 3 > [data-help=dr-list-status] | 4 > [data-help=dr-list-table] | 5 > [data-help=dr-list-pages]
minutes: 2
source-hash: d6627d7c7d23093d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Dealer Applications Review** स्क्रीनवर तुमच्या भागातले सबमिट झालेले डीलर अर्ज दिसतात. तुमची वाट पाहणारे अर्ज सगळ्यात वर असतात.

1. वरची कार्डे पाहा: **Total Applications**, **Pending Review**, **Verified** आणि **Sent Back**.
2. अर्ज पटकन शोधण्यासाठी **Search applications...** मध्ये अर्जाचा ID किंवा डीलरचे नाव टाइप करा.
3. फक्त तेच अर्ज पाहण्यासाठी स्थितीच्या बॉक्समध्ये **Pending**, **Verified** किंवा **Sent Back** निवडा. **All Status** ने सगळे दिसतात.
4. टेबलमध्ये **Current Stage** मध्ये अर्ज आता कोणाकडे आहे ते दिसते आणि **Credit Limit** मध्ये मंजूर लिमिट दिसते. अर्ज उघडण्यासाठी **Action** मधला डोळ्याचा आयकॉन टॅप करा.
5. खाली **Items Per Page** मध्ये किती ओळी दाखवायच्या ते निवडा आणि पेज बदलण्यासाठी बाण वापरा.

> Tip: RM ला रिजन दिसतो, SM ला राज्य दिसते, आणि AVP ला सगळे अर्ज दिसतात.

## नेहमीच्या अडचणी

- **"No applications found."** (एकही अर्ज सापडला नाही) **All Status** निवडा आणि सर्च बॉक्स रिकामा करा.
- **एखादा अर्ज लिस्टमध्ये नाही.** फक्त पूर्ण भरलेले अर्जच लिस्टमध्ये येतात. MO ला प्रत्येक भाग भरायला सांगा.
:::

::: section review-approve
title: अर्ज तपासा आणि मंजूर करा
routes: /SavedDealerReview
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-rev-cards] | 2 > [data-help=dr-rev-progress] | 3 > [data-help=dr-rev-timeline] | 4 > [data-help=dr-rev-details] | 5 > [data-help=dr-rev-sendback] | 6 > [data-help=dr-rev-approve]
minutes: 4
source-hash: 337aec253f3ff75e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Review - Dealer Application** स्क्रीनवर एक पूर्ण अर्ज दिसतो. तो तुमची वाट पाहत असेल तेव्हा तुम्ही इथे तो मंजूर करून तुमची क्रेडिट लिमिट देता, किंवा तो परत पाठवता.

1. वरची कार्डे तपासा: **Dealer Name**, **Dealer Code**, **Submitted By** आणि **Status**.
2. प्रगतीच्या पट्टीवर प्रत्येक लेव्हल दिसते: **MO**, **RM**, **SM** आणि **AVP**, सोबत **Under Review**, **Pending**, **Approved** किंवा **Rejected**.
3. **Timeline** आणि **Action History** मध्ये कोणी, कधी काय केले, त्यांचा शेरा आणि त्यांनी दिलेली क्रेडिट लिमिट दिसते.
4. प्रत्येक भाग उघडून तपासण्यासाठी तो टॅप करा: **Company Details**, **Experience In Fertilizer Trade**, **Annual Sales Data**, **Proprietor Details**, **Investment**, **Credit Limit Proposal**, **Enclosures** आणि बाकीचे.
5. काही चुकीचे असेल तर **Send Back** दाबा ("अर्ज परत पाठवा" पाहा).
6. सगळे बरोबर असेल तर **Proceed to SM** (RM), **Proceed to AVP** (SM) किंवा **Approve** (AVP) दाबा. **Confirm Proceed** विंडोमध्ये तुमची क्रेडिट लिमिट आणि **Remarks** टाका, मग **Confirm & Proceed** दाबा.

> Careful: पुढे पाठवल्यावर अर्ज पुढच्या लेव्हलला जातो आणि तुम्ही तो बदलू शकत नाही.

## फील्डची माहिती

| फील्ड | काय भरायचे |
|---|---|
| **RM Credit Limit (SPIC)**, **RM Credit Limit (GFL)** | RM सुचवतात ती लिमिट. RM ला दिसते. |
| **SM Credit Limit (SPIC)**, **SM Credit Limit (GFL)** | SM सुचवतात ती लिमिट. RM च्या लिमिट वर **(Approved)** म्हणून दिसतात. |
| **AVP Credit Limit (SPIC)**, **AVP Credit Limit (GFL)** | अंतिम लिमिट. RM आणि SM च्या लिमिट वर दिसतात. |
| **Remarks** | तुमचा शेरा. नवीन डीलरसाठी आवश्यक, नाहीतर ऐच्छिक. |

## नेहमीच्या अडचणी

- **Proceed किंवा Send Back बटण नाही.** अर्ज आता तुमची वाट पाहत नाही. तुम्ही तो फक्त वाचू शकता.
- **हिरवा "Credit Limit Consent" बॉक्स.** डीलरने क्रेडिट लिमिटच्या तपशिलाशिवाय अर्ज केला आहे, त्यामुळे क्रेडिट लिमिट 0 आहे.
- **स्क्रीन Dashboard वर जाते.** हे नेहमीचेच आहे. तुम्ही मंजूर केल्यावर ॲप यशस्वी झाल्याचा मेसेज दाखवते आणि Dashboard उघडते.
:::

::: section send-back
title: अर्ज परत पाठवा
routes: /SavedDealerReview
for: 
roles: RM, RMD, SMD, SMM, AVP, Director, Admin, CorporateAdmin, SuperAdmin
designations: 
minutes: 2
source-hash: b3b23aec15238c4a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
अर्जात चुका असतील तर कारण लिहून तो एक लेव्हल मागे पाठवा. RM तो **Marketing Officer (MO)** कडे पाठवतात, SM तो **Regional Manager (RM)** कडे, आणि AVP तो **State Manager (SM)** कडे.

1. अर्ज उघडा आणि खाली **Send Back** दाबा.
2. **Send Back Application** विंडोमध्ये **Application Summary** आणि **Send Back To** तपासा.
3. **Reason for Sending Back** लिहा. हे आवश्यक आहे.
4. **Sections Requiring Correction** खाली दुरुस्त करायच्या भागांवर टिक करा, उदा. **Tax & Compliance**, **Investment** किंवा **Enclosures**. काहींवर आधीच टिक असते; जे भाग बरोबर आहेत त्यांची टिक काढा.
5. **Priority Level** निवडा: **High**, **Medium** किंवा **Low**.
6. **Send Back to MO**, **Send Back to RM** किंवा **Send Back to SM** दाबा (बटणावर तुमची लेव्हल दिसते), किंवा थांबवण्यासाठी **Cancel** दाबा.

## नेहमीच्या अडचणी

- **"Reason is required."** (कारण लिहिणे आवश्यक आहे) आधी कारण लिहा.
- **"Please select at least one correction section."** (किमान एक दुरुस्तीचा भाग निवडा) किमान एका भागावर टिक करा.
- **"Unable to send back dealer application."** (डीलर अर्ज परत पाठवता आला नाही) तुमचे इंटरनेट तपासा आणि परत प्रयत्न करा.
:::

::: section avp-office-ids
title: नवीन डीलरसाठी डीलर ID टाका
routes: /SavedDealerReview
for: 
roles: CommonRole
designations: AVP Office
minutes: 2
source-hash: e77b528ade25e538
confidence: ok

**ही स्क्रीन कशासाठी आहे**
AVP ने नवीन डीलर मंजूर केल्यावर AVP Office कंपनीचे डीलर ID टाकते. यामुळे अर्ज पूर्ण होतो.

1. तपासणीच्या लिस्टमधून अर्ज उघडा. त्याच्या **Current Stage** मध्ये **AVP Office** दिसते.
2. खाली **AVP Office - Dealer ID Submission** बॉक्सपर्यंत स्क्रोल करा.
3. **SPIC Dealer ID** आणि/किंवा **GFL Dealer ID** टाका. डीलरने ज्या कंपन्यांसाठी अर्ज केला त्याच दिसतात.
4. गरज असेल तर **Remarks (optional)** टाका.
5. **Submit Dealer IDs** दाबा. ॲप यशस्वी झाल्याचा मेसेज दाखवते आणि Dashboard उघडते.

## नेहमीच्या अडचणी

- **"SPIC Dealer ID is required for this application."** (या अर्जासाठी SPIC डीलर ID आवश्यक आहे) डीलर SPIC सोबत आहे. SPIC डीलर ID टाका.
- **"GFL Dealer ID is required for this application."** (या अर्जासाठी GFL डीलर ID आवश्यक आहे) डीलर GFL सोबत आहे. GFL डीलर ID टाका.
- **मला बॉक्स दिसत नाही.** AVP ने अजून तो मंजूर केलेला नाही, किंवा ID आधीच टाकलेले आहेत.
:::

::: section view-application
title: अर्ज पाहा (फक्त वाचण्यासाठी)
routes: /ReviewDealer
for: 
roles: 
designations: 
minutes: 1
source-hash: ee8404c8e5c3aad3
confidence: ok

**ही स्क्रीन कशासाठी आहे**
ही जुनी **Review - Dealer Application** स्क्रीन मंजुरीच्या बटणांशिवाय एक अर्ज दाखवते.

1. वरची कार्डे तपासा: **Dealer Name**, **Dealer ID**, **Total Experience**, **Total Sales**, **Investment**, **Credit Limit** आणि **Status**.
2. प्रत्येक भाग उघडण्यासाठी तो टॅप करा, जसे **Company Details** किंवा **Investment**.
3. **Enclosures** खाली कागदपत्र उघडण्यासाठी त्याचे नाव टॅप करा.

> Tip: मंजूर करायचा किंवा परत पाठवायचा असेल तर त्याऐवजी अर्ज **Dealer Applications Review** लिस्टमधून उघडा.

## नेहमीच्या अडचणी

- **स्क्रीन रिकामी आहे.** अर्ज निवडल्यावरच ही स्क्रीन उघडते. तपासणीच्या लिस्टवर जा आणि डोळ्याचा आयकॉन वापरा.
:::

::: section sm-approvals
title: सब डीलर आणि कर्मचाऱ्यांना मंजुरी द्या (SM)
routes: /SMMApprovals
for: 
roles: SMD, SMM
designations: 
tour: 1 > [data-help=dr-sm-tabs] | 2 > [data-help=dr-sm-cards] | 3 > [data-help=dr-sm-table] | 4 > [data-help=dr-sm-table]
minutes: 2
source-hash: e13ee4a27ffd7b42
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**SM Approval** स्क्रीनवर तुमच्या राज्यातल्या डीलरसाठी जोडलेले सब डीलर आणि कर्मचारी दिसतात. आधी SM ते तपासतात; मग ते AVP कडे जातात.

1. एक टॅब निवडा: **All Pending**, **Sub Dealers** किंवा **Employees**.
2. कार्डांवर **Total Pending**, **Sub Dealers** आणि **Employees** दिसतात. एकाच प्रकारचे पाहण्यासाठी **Filter** वापरा.
3. **Pending SM Approvals** मध्ये **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** आणि **Nominee** तपासा.
4. **Approve** किंवा **Reject** दाबा, **Remarks** लिहा, मग **Confirm Approve** किंवा **Confirm Reject** दाबा.

> Tip: मंजुरीसाठी शेरा ऐच्छिक आहे, पण नाकारताना कारण लिहा.

## नेहमीच्या अडचणी

- **"No Pending Approvals".** (कोणतीही मंजुरी बाकी नाही) आता तुमच्याकडे काहीही बाकी नाही.
- **मला "Show more" दिसते.** सुरुवातीला फक्त 50 ओळी दिसतात. बाकीच्या पाहण्यासाठी **Show more** दाबा.
:::

::: section avp-approvals
title: सब डीलर आणि कर्मचाऱ्यांना अंतिम मंजुरी (AVP)
routes: /AVPApprovals
for: 
roles: AVP
designations: 
minutes: 2
source-hash: 61b6fcc972ffa53e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**AVP Approval** स्क्रीनवर SM ने आधीच मंजूर केलेले सब डीलर आणि कर्मचारी दिसतात. AVP अंतिम मंजुरी देतात.

1. एक टॅब निवडा: **All Pending**, **Sub Dealers** किंवा **Employees**.
2. **Total Pending**, **Sub Dealers** आणि **Employees** ही कार्डे तपासा. एकाच प्रकारचे पाहण्यासाठी **Filter** वापरा.
3. **Pending AVP Approvals** मध्ये **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** आणि **Nominee** तपासा.
4. **Approve** किंवा **Reject** दाबा. विंडोमध्ये **SM Status: Approved** दिसते.
5. **Remarks** लिहा आणि **Confirm Approve** किंवा **Confirm Reject** दाबा.

## नेहमीच्या अडचणी

- **"No Pending AVP Approvals".** (AVP ची कोणतीही मंजुरी बाकी नाही) SM ने मंजूर केलेली एकही नोंद तुमची वाट पाहत नाही.
- **"Action failed".** (कृती अयशस्वी झाली) परत प्रयत्न करा. तरीही होत नसेल तर तुमच्या admin ला सांगा.
:::

::: section credit-limit-sales
title: क्रेडिट लिमिटसाठी डीलरची विक्री नोंदवा
routes: /CreditLimitSales
for: 
roles: 
designations: 
minutes: 3
source-hash: 580721465ba2267a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Credit Limit Sales** स्क्रीनवर प्रत्येक डीलरची मागची विक्री वर्ष आणि प्रॉडक्टनुसार ठेवली जाते. तपासणारे हे आकडे अर्जाच्या **Credit Limit Proposal** मध्ये पाहतात.

1. **Add New Credit Limit Sales** खाली **State** आणि **Financial Year** निवडा.
2. **Dealer Code** किंवा **Dealer Name** ने डीलर निवडा. दोन्ही बॉक्स एकच डीलर निवडतात.
3. **Product**, **Product Group** आणि **Category** निवडा.
4. **Quantity** आणि **Gross Amount** टाका, मग **Save** दाबा. **Cancel** ने फॉर्म रिकामा होतो.
5. एकदम अनेक ओळी जोडायच्या असतील तर Excel चा नमुना मिळवण्यासाठी **Sample Format** दाबा, तो भरा, **Bulk Upload** दाबा, आणि **Financial Year** व फाइल निवडा.
6. **Existing Credit Limit Sales** मध्ये कोड किंवा नावाने शोधा. पेन्सिल ओळ बदलते आणि डस्टबिन ती डिलीट करते.

> Careful: डिलीट केलेली ओळ परत आणता येत नाही.

## नेहमीच्या अडचणी

- **"All required fields must be filled."** (सगळी आवश्यक फील्ड भरली पाहिजेत) State, Financial Year, डीलर, Product, Product Group आणि Category निवडा.
- **"Please select a financial year before uploading."** (अपलोड करण्याआधी आर्थिक वर्ष निवडा) आधी अपलोड विंडोमध्ये वर्ष निवडा.
- **Save, Bulk Upload, पेन्सिल किंवा डस्टबिन दिसत नाही.** तुमच्या पदाला जोडण्याची, बदलण्याची किंवा डिलीट करण्याची परवानगी नाही. तुमच्या admin ला विचारा.
:::

::: section state-summary
title: राज्यनिहाय डीलरचा सारांश
routes: /DealerStateSummary
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP
designations: 
minutes: 1
source-hash: d5685e7a98ccb663
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Total Dealers — State Wise** स्क्रीन प्रत्येक राज्यातले डीलर मोजते आणि SPIC व GFL साठी त्यांचे अर्ज कुठे आहेत ते दाखवते.

1. **TOTAL DEALERS** आणि **COMPLETED** ही कार्डे तपासा.
2. **Dealer Summary** मध्ये प्रत्येक ओळ एक राज्य आहे. कॉलममध्ये **As Per SAP Data**, **Saved Dealers**, **MO**, **RM**, **SMM**, **AVP** आणि **Completed** दिसतात, प्रत्येक **SPIC** आणि **GFL** साठी.
3. त्या कॉलमनुसार क्रम लावण्यासाठी कॉलमचे नाव टॅप करा. उलटा क्रम लावण्यासाठी परत टॅप करा.
4. खालच्या **Total** ओळीत सगळ्या राज्यांची बेरीज असते.
5. नवीन आकडे आणण्यासाठी **Refresh** दाबा, किंवा **Back to Dashboard** दाबा.

## नेहमीच्या अडचणी

- **"Access restricted".** (परवानगी नाही) हा रिपोर्ट फक्त Admin, Director आणि AVP युजरसाठी आहे.
- **एखाद्या राज्यासमोर "Unknown" दिसते.** राज्यांची लिस्ट लोड झाली नाही. **Refresh** दाबा.
:::

::: section dealer-directory
title: डीलर डिरेक्टरी
routes: /DealerDirectory
for: 
roles: 
designations: 
minutes: 1
source-hash: db991abf0063ad82
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Dealer Directory** स्क्रीन ही पुढे येणाऱ्या डीलर लिस्टची झलक आहे. त्यावरचे आकडे आणि ओळी फक्त उदाहरण आहेत, आणि तिची बटणे आणि फिल्टर अजून काम करत नाहीत.

1. रचना पाहा: **Total Dealers**, **Active Dealers**, **Regions Covered** आणि **Recently Added** ची कार्डे.
2. **Filters** बॉक्समध्ये तुम्हाला **State**, **Region**, **Head Quarters** आणि **Status** निवडता येईल.
3. **Dealer List** टेबलमध्ये **Dealer Code**, **Firm Name**, **State**, **Head Quarters**, **Mobile No.**, **Email** आणि **Status** दिसतील.

> Tip: खरा अर्ज शोधायचा असेल तर **Dealer Applications Review** लिस्ट वापरा.

## नेहमीच्या अडचणी

- **Excel, PDF किंवा Print काहीच करत नाही.** ही बटणे अजून काम करत नाहीत.
:::

::: section dealer-mapping
title: डीलरना सेल्स ऑडिट प्रोग्रामशी जोडा
routes: /DealerMapping
for: 
roles: 
designations: 
minutes: 3
source-hash: 42aed46703ffd3d8
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Dealer Mapping** स्क्रीन ही सेल्स ऑडिट प्रोग्रामची तिसरी पायरी, **Dealers**, आहे. तुम्ही प्रोग्राममधल्या डीलरची लिस्ट अपलोड करता आणि त्यांची SAP विक्री प्लॅनशी जुळवून पाहता.

1. **Sales Plan** मधून **Save & Continue** किंवा **Continue to Dealer Mapping** ने ही स्क्रीन उघडा.
2. **Upload Dealer List** खाली Excel फाइल टाका किंवा **Upload File** दाबा. फाइलमध्ये **Dealer Code** कॉलम असला पाहिजे.
3. मेसेज वाचा. काही ओळी सोडल्या गेल्या असतील तर **Row No.**, **Dealer Code** आणि **Reason** पाहण्यासाठी **View Errors** दाबा.
4. **Dealer List** मध्ये कोड किंवा नावाने शोधा. डस्टबिन डीलरला प्रोग्राममधून काढते.
5. **Dealer Sales Achievement - SAP Invoice Matches** मध्ये ठरवलेली आणि प्रत्यक्ष विक्री तपासा. ती परत लोड करण्यासाठी **Refresh Matches** दाबा.
6. **Plan Summary & Comparison** तपासा: **Required Turnover**, **Achievement Amount**, **Difference** आणि **Progress %**.
7. **Continue to Plan Review** दाबा, किंवा **Back to Sales Plan** दाबा.

## नेहमीच्या अडचणी

- **"Missing CSR-2 ID. Open this page from Sales Plan."** (CSR-2 ID नाही. हे पेज Sales Plan मधून उघडा) सेव्ह केलेल्या लिंकवरून नाही, तर Sales Plan मधून ही स्क्रीन उघडा.
- **"Upload an .xlsx or .xls file up to 5 MB."** (5 MB पर्यंतची .xlsx किंवा .xls फाइल अपलोड करा) 5 MB पेक्षा लहान Excel फाइल वापरा.
- **Continue to Plan Review राखाडी आहे.** आधी किमान एक डीलर अपलोड करा.
:::

::: section training-program
title: डीलर ट्रेनिंग प्रोग्रामची माहिती द्या (CSR-2)
routes: /DealerManagementTrainingProgram
for: 
roles: 
designations: 
minutes: 5
source-hash: f2c7c96961e19cd8
confidence: ok

**ही स्क्रीन कशासाठी आहे**
CSR-1 प्लॅन मंजूर झाल्यावर, प्रोग्राम प्रत्यक्षात कसा झाला ते कळवण्यासाठी ही स्क्रीन वापरा: तारीख, फोटो, सहभागी लोक आणि खर्च. **CSR-2 Program Execution** लिस्टमधून **View Program** ने ही स्क्रीन उघडते.

1. **Plan & Start** वर **Approved CSR-1 Plan** तपासा. **Focus Details**, **Actual Event Date**, **Start Time**, **End Time** आणि **Actual Location** भरा. कार्यक्रमाचे फोटो-व्हिडिओसाठी **Take Photo** आणि **Record Video** वापरा. **Start Event** दाबा, मग खात्री करण्यासाठी परत **Start Event** दाबा.
2. **Participants & Employees** वर सहभागी व्यक्तीला **Phone Number** ने शोधा, **Role** निवडा आणि **Add Participant** दाबा. अजून नोंदणी न झालेल्या व्यक्तीसाठी **New Farmer** वापरा. कर्मचारी **Add Employee** ने जोडा.
3. **Expense & Proof** वर प्रत्येक खर्चासाठी **Actual amount (₹)**, **Bill No.** आणि **Date** टाका, आणि **Upload proofs** दाबा. कार्डांवर **Approved Budget** आणि **Actual Expense** यांची तुलना दिसते.
4. **Review & Submit** वर सगळे तपासा. मागे जाऊन बदल करण्यासाठी कार्डवर **Edit** दाबा.
5. नंतर पूर्ण करायचे असेल तर **Save Draft** दाबा. पाठवण्यासाठी **Apply CSR-2** दाबा, किंवा तो तुमची वाट पाहत असेल तर **Validate CSR-2** किंवा मंजुरीचे बटण दाबा.

> Tip: फोटो आणि व्हिडिओवर GPS ठिकाण, तारीख आणि वेळ छापलेली असते.

## नेहमीच्या अडचणी

- **"CSR-2 can be opened only for an approved CSR-1."** (मंजूर CSR-1 साठीच CSR-2 उघडता येतो) CSR-1 प्लॅन मंजूर होईपर्यंत थांबा.
- **मला फक्त दोनच पायऱ्या दिसतात.** काही रोल फक्त **Plan & Start** आणि **Participants & Employees** भरतात, मग **Apply CSR-2** दाबतात.
- **"Enter a valid 10-digit mobile number."** (बरोबर 10 अंकी मोबाइल नंबर टाका) फोन नंबर तपासा.
:::
