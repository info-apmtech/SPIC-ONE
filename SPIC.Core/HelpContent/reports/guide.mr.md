---
module: reports
title: रिपोर्ट
icon: bi-bar-chart
order: 116
language: mr
category: अहवाल
note: नवीन मार्गदर्शक
meta-source-hash: c3d58c99d0724041
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 0cd39d62cdd793e3
confidence: ok

मुख्य आकडे एकाच ठिकाणी पाहा. **Admin Dashboard** वर वेल्फेअर अर्ज, डीलर आणि गेस्ट हाऊस बुकिंग दिसतात. **Reports Center** वर रिपोर्टची लिस्ट दिसते.

- dashboard | icon: bi-speedometer2 | color: #1d4ed8
  title: Admin Dashboard
  text: वेल्फेअर अर्ज, डीलर आणि गेस्ट हाऊसचे आकडे एकाच पेजवर.
- queue | icon: bi-list-check | color: #d97706
  title: बाकी अर्जांची रांग
  text: कोणते अर्ज वाट पाहत आहेत, कोणत्या टप्प्यावर आणि किती दिवसांपासून ते पाहा.
- reports | icon: bi-file-earmark-bar-graph | color: #7c3aed
  title: Reports Center
  text: रिपोर्टची लिस्ट. हे अजून तयार होत आहे, त्यामुळे दिसणारे रिपोर्ट फक्त नमुने आहेत.
:::

::: section flow
title: तुम्ही या स्क्रीन कशा वापरता
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 7963de51aaa7ddbb
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- office | type: lane | color: #1d4ed8 | roles: Admin, CorporateAdmin, Director
  title: SDWA Admin आणि ऑफिस
  sub: SDWA Admin · Admin · Director
- approvers | type: lane | color: #d97706 | roles: MO, RM, SMM
  title: मंजुरी देणारे
  sub: MO · RM · SM · SDWA · SDWA Admin
- open | lane: office | col: 1 | icon: bi-speedometer2 | color: #1d4ed8 | next: check | screen: admin-dashboard
  title: Admin Dashboard उघडा
  text: मेनूमध्ये **SDWA** उघडा आणि **Admin Dashboard** निवडा.
- check | lane: office | col: 2 | icon: bi-list-check | color: #d97706 | next: act, reports | screen: admin-dashboard
  title: आकडे आणि रांग तपासा
  text: कार्डे, **Approval Workflow Overview** आणि **Pending Action Queue (SDWA)** पाहा.
- act | lane: approvers | col: 3 | icon: bi-patch-check | color: #16a34a
  title: Scheme Approval स्क्रीनवर मंजूर करा
  text: **Quick actions** खाली **Review Pending Approvals** दाबा. मंजुरी **Scheme Approval** स्क्रीनवर दिली जाते, डॅशबोर्डवर नाही.
- reports | lane: office | col: 3 | icon: bi-file-earmark-bar-graph | color: #7c3aed | screen: reports-center
  title: रिपोर्टची लिस्ट पाहा
  text: रिपोर्ट पाहण्यासाठी **Reports Center** उघडा. हे अजून तयार होत आहे.
- no-data | lane: office | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: check | to: open | screen: admin-dashboard
  title: माहिती लोड झाली नाही
  text: वर लाल बॉक्समध्ये **Could not load** लिहिलेले असते आणि बॉक्समध्ये **—** दिसते. थोड्या वेळाने डॅशबोर्ड परत उघडा.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9575d21878652ad1
confidence: ok

- pending-mo | color: #d97706
  label: Pending MO
  text: तपासणीसाठी MO कडे आहे.
- pending-rm | color: #d97706
  label: Pending RM
  text: तपासणीसाठी RM कडे आहे.
- pending-sm | color: #d97706
  label: Pending SM
  text: तपासणीसाठी SM कडे आहे.
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: मंजुरीसाठी SDWA कडे आहे.
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: अंतिम मंजुरीसाठी SDWA Admin कडे आहे.
- high | color: #dc2626
  label: High
  text: रांगेतली प्राधान्य पातळी. डीलरने 5 किंवा जास्त दिवसांपूर्वी अर्ज केला.
- medium | color: #ea580c
  label: Medium
  text: रांगेतली प्राधान्य पातळी. डीलरने 2 ते 4 दिवसांपूर्वी अर्ज केला.
- low | color: #16a34a
  label: Low
  text: रांगेतली प्राधान्य पातळी. डीलरने आज किंवा काल अर्ज केला.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 029f192b703c661f
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमच्या ॲक्सेसनुसार परवानगी असलेल्याच स्क्रीन तुम्हाला दिसतात.

- sdwa-admin | color: #1d4ed8
  title: SDWA Admin
  - Admin Dashboard वर सगळ्या वेल्फेअर अर्जांवर लक्ष ठेवा
  - **Quick actions** मधून बाकी मंजुऱ्या उघडा
  - गेस्ट हाऊस बुकिंग तपासा
- office | color: #0d9488 | roles: Admin, CorporateAdmin, Director
  title: Admin, Corporate Admin, Director
  - तपासणी अजून चालू असलेला प्रत्येक अर्ज पाहा
  - डीलर आणि गेस्ट हाऊसचे आकडे पाहा
  - Reports Center उघडा
- approvers | color: #d97706 | roles: MO, RM, SMM
  title: MO, RM, SM
  - तुम्ही Admin Dashboard उघडू शकत असाल, तर रांगेत तुमच्या टप्प्यावरचे अर्ज दिसतात
  - तुम्हाला फक्त तुमचे HQ, रिजन किंवा राज्य दिसते
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7222902e690b16b2
confidence: ok

- dash | popular: true
  q: काही बॉक्समध्ये आकड्याऐवजी — दिसते
  a: एकतर माहिती लोड झाली नाही (वरच्या लाल बॉक्समध्ये कोणता भाग ते लिहिलेले असते), किंवा त्या बॉक्ससाठी अजून माहितीच नाही. **Active Schemes**, **Benefit Released**, **SLA Delayed**, **Upcoming Check-ins**, **Cancelled**, **Revenue Collected** आणि **Finance & Benefit Release** चे बॉक्स सध्या नेहमी — दाखवतात.
- download | popular: true
  q: Reports Center मध्ये मी Download दाबतो आणि काहीच होत नाही
  a: Reports Center अजून तयार होत आहे. **Download** ने अजून फाइल तयार होत नाही, आणि त्यावरचे रिपोर्ट आणि आकडे फक्त नमुने आहेत, तुमची खरी माहिती नाही.
- icons
  q: रांगेतले टिक आणि क्रॉस आयकॉन काहीच करत नाहीत
  a: हे आयकॉन अजून चालत नाहीत. **Quick actions** खाली **Review Pending Approvals** दाबा आणि **Scheme Approval** स्क्रीनवर मंजूर करा किंवा नाकारा.
- nothing-pending
  q: रांगेत "Nothing pending" (काहीही बाकी नाही) दिसते
  a: तुमच्या टप्प्यावर एकही अर्ज वाट पाहत नाही. MO, RM आणि SM यांना फक्त त्यांचेच HQ, रिजन किंवा राज्य दिसते.
- could-not-load
  q: मला "Could not load: welfare approvals" दिसते
  a: वेल्फेअरची माहिती फक्त MO, RM, SM, SDWA, SDWA Admin, Admin, Corporate Admin आणि Director यांनाच दिसते. तुम्हाला ती दिसायला हवी असेल, तर तुमच्या admin ला तुमचा ॲक्सेस तपासायला सांगा आणि मग पेज परत उघडा.
:::

::: section admin-dashboard
title: Admin Dashboard वाचा
routes: /ReportDashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=rpt-kpis] | 2 > [data-help=rpt-workflow] | 3 > [data-help=rpt-queue] | 4 > [data-help=rpt-charts] | 5 > [data-help=rpt-guesthouse] | 6 > [data-help=rpt-quick]
minutes: 3
source-hash: c608b65421e5a6ab
confidence: ok

**ही स्क्रीन कशासाठी आहे**
SDWA Admin साठीच्या **Dashboard** वर वेल्फेअर अर्ज, डीलर आणि गेस्ट हाऊस बुकिंग एकाच पेजवर दिसतात. मेनूमध्ये **SDWA** > **Admin Dashboard** ने ही स्क्रीन उघडा. इथे तुम्ही फक्त पाहता. मंजुरी **Scheme Approval** स्क्रीनवर दिली जाते.

1. वरची कार्डे पाहा: **Total Welfare Applications**, **Pending Final Approval**, **Approved Applications**, **Rejected Applications**, **Total Dealers** आणि **Guest House Bookings**.
2. **Approval Workflow Overview** मध्ये **Dealer** पासून **Completed** पर्यंतचे टप्पे पाहा, आणि किती अर्ज **Pending at MO**, **Pending at RM**, **Pending at SMM**, **Pending at SDWA** आणि **Rejected** आहेत ते पाहा.
3. **Pending Action Queue (SDWA)** मध्ये वाट पाहणारा प्रत्येक अर्ज त्याच्या **Current Stage**, **Pending Since** आणि **Priority** सह पाहा. आणखी ओळी पाहण्यासाठी **Show more** दाबा.
4. **Scheme Distribution** मध्ये प्रत्येक योजनेत किती अर्ज आहेत ते दिसते. **Monthly Application Trend** मध्ये दर महिन्याचे अर्ज दिसतात. त्यांच्या खाली डीलरचे आकडे आणि **Regional Distribution** टेबल असते.
5. **Guest House Overview** मध्ये बुकिंग आणि प्रत्येक गेस्ट हाऊस किती भरले आहे ते दिसते. फ्रंट ऑफिस उघडण्यासाठी **View All Bookings** दाबा.
6. **Quick actions** खाली अर्ज मंजूर करण्यासाठी **Review Pending Approvals** दाबा. **Dealer Directory**, **Manage Schemes** आणि **Guest House Bookings** त्या त्या स्क्रीन उघडतात.

> Careful: रांगेतले डोळा, टिक, क्रॉस आणि डाउनलोड आयकॉन, **Search zones...** बॉक्स, आणि **Generate Reports** आणि **Gallery** कार्डे अजून चालत नाहीत.

## स्क्रीनचे इतर भाग

- **Recent Activities** मध्ये शेवटच्या 5 घटना दिसतात, जसे नवीन अर्ज, मंजुरी किंवा गेस्ट हाऊसचे बिल.
- **Finance & Benefit Release** मध्ये अजून माहिती नाही. त्यात — आणि "No payment releases" (कोणतेही पेमेंट दिलेले नाही) दिसते.

## नेहमीच्या अडचणी

- **लाल बॉक्समध्ये "Could not load" लिहिलेले आहे.** माहितीचा तो भाग लोड झाला नाही. त्याच्या बॉक्समध्ये — दिसते. थोड्या वेळाने पेज परत उघडा.
- **Pending at MO चा आकडा जास्त वाटतो.** त्यात दुरुस्तीसाठी डीलरकडे परत पाठवलेले अर्जही मोजले जातात.
- **Pending Since खूप जास्त वाटते.** हे डीलरने अर्ज केल्यापासूनचे दिवस मोजते, अर्ज या टप्प्यावर आल्यापासूनचे नाही.
- **रांगेत "Nothing pending" दिसते.** आता तुमच्या टप्प्यावर एकही अर्ज वाट पाहत नाही.
:::

::: section reports-center
title: Reports Center पाहा
routes: /ReportsCenter
for: 
roles: 
designations: 
minutes: 1
source-hash: fea64cc958f10b95
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Reports Center** वर तुम्हाला बुकिंग, बिलिंग, मंजुरी आणि डीलरचे रिपोर्ट पाहता आणि डाउनलोड करता येतील. हे अजून तयार होत आहे: त्यावरचे आकडे, रिपोर्ट आणि तारखा फक्त नमुने आहेत, आणि फिल्टर आणि डाउनलोड अजून चालत नाहीत.

1. वरची कार्डे पाहा: **Total Reports**, **Booking Reports**, **Billing Reports** आणि **Approval Reports**.
2. टॅब (**All Reports**, **Booking Reports**, **Billing Reports**, **Approval Reports**), **Search** बॉक्स आणि **All Categories** आणि **All Formats** लिस्ट दिसतात, पण त्यांनी अजून लिस्ट बदलत नाही.
3. प्रत्येक रिपोर्ट कार्डवर रिपोर्टचे नाव, त्यात काय आहे, **Available Formats** आणि एक तारीख दिसते. कार्डे अशी आहेत: **Dealer Information**, **Tirupathi Bookings**, **T-Nagar Bills**, **MO Reports**, **RM Reports** आणि **Tirupathi Bills**.
4. **Category**, **Records**, **File Types**, **Generated By** आणि एक छोटे टेबल असलेला नमुना उघडण्यासाठी **Preview** दाबा. तो बंद करण्यासाठी **Close** किंवा X दाबा.
5. **Download**, **Download PDF** आणि **Download Excel** ने अजून फाइल तयार होत नाही.

> Careful: या स्क्रीनवरचे आकडे वापरू नका. ते फक्त नमुने आहेत, तुमची खरी माहिती नाही.

## नेहमीच्या अडचणी

- **Download दाबल्यावर काहीच होत नाही.** डाउनलोड अजून तयार नाहीत.
- **प्रत्येक Preview मध्ये Dealer Information च दिसते.** सध्या सगळी Preview बटणे एकच नमुना उघडतात.
- **टॅब आणि सर्चने लिस्ट बदलत नाही.** ते अजून चालत नाहीत.
:::
