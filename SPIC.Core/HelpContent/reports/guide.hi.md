---
module: reports
title: रिपोर्ट
icon: bi-bar-chart
order: 116
language: hi
category: रिपोर्ट
note: नई गाइड
meta-source-hash: c3d58c99d0724041
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 0cd39d62cdd793e3
confidence: ok

मुख्य आँकड़े एक ही जगह देखें। **Admin Dashboard** पर वेलफ़ेयर आवेदन, डीलर और गेस्ट हाउस बुकिंग दिखती हैं। **Reports Center** पर रिपोर्ट की लिस्ट दिखती है।

- dashboard | icon: bi-speedometer2 | color: #1d4ed8
  title: Admin Dashboard
  text: वेलफ़ेयर आवेदन, डीलर और गेस्ट हाउस के आँकड़े एक ही पेज पर।
- queue | icon: bi-list-check | color: #d97706
  title: बाकी आवेदनों की कतार
  text: देखें कि कौन से आवेदन इंतज़ार कर रहे हैं, किस स्टेज पर और कितने दिनों से।
- reports | icon: bi-file-earmark-bar-graph | color: #7c3aed
  title: Reports Center
  text: रिपोर्ट की लिस्ट। यह अभी बन रहा है, इसलिए दिखने वाली रिपोर्ट सिर्फ़ नमूने हैं।
:::

::: section flow
title: आप ये स्क्रीन कैसे इस्तेमाल करते हैं
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 7963de51aaa7ddbb
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- office | type: lane | color: #1d4ed8 | roles: Admin, CorporateAdmin, Director
  title: SDWA Admin और ऑफ़िस
  sub: SDWA Admin · Admin · Director
- approvers | type: lane | color: #d97706 | roles: MO, RM, SMM
  title: मंज़ूरी देने वाले
  sub: MO · RM · SM · SDWA · SDWA Admin
- open | lane: office | col: 1 | icon: bi-speedometer2 | color: #1d4ed8 | next: check | screen: admin-dashboard
  title: Admin Dashboard खोलें
  text: मेन्यू में **SDWA** खोलें और **Admin Dashboard** चुनें।
- check | lane: office | col: 2 | icon: bi-list-check | color: #d97706 | next: act, reports | screen: admin-dashboard
  title: आँकड़े और कतार जाँचें
  text: कार्ड, **Approval Workflow Overview** और **Pending Action Queue (SDWA)** देखें।
- act | lane: approvers | col: 3 | icon: bi-patch-check | color: #16a34a
  title: Scheme Approval स्क्रीन पर मंज़ूर करें
  text: **Quick actions** के नीचे **Review Pending Approvals** दबाएँ। मंज़ूरी **Scheme Approval** स्क्रीन पर दी जाती है, डैशबोर्ड पर नहीं।
- reports | lane: office | col: 3 | icon: bi-file-earmark-bar-graph | color: #7c3aed | screen: reports-center
  title: रिपोर्ट की लिस्ट देखें
  text: रिपोर्ट देखने के लिए **Reports Center** खोलें। यह अभी बन रहा है।
- no-data | lane: office | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: check | to: open | screen: admin-dashboard
  title: डेटा लोड नहीं हुआ
  text: ऊपर लाल बॉक्स में **Could not load** लिखा होता है और बॉक्स में **—** दिखता है। थोड़ी देर बाद डैशबोर्ड फिर से खोलें।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9575d21878652ad1
confidence: ok

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
- high | color: #dc2626
  label: High
  text: कतार में प्राथमिकता। डीलर ने 5 या उससे ज़्यादा दिन पहले आवेदन किया था।
- medium | color: #ea580c
  label: Medium
  text: कतार में प्राथमिकता। डीलर ने 2 से 4 दिन पहले आवेदन किया था।
- low | color: #16a34a
  label: Low
  text: कतार में प्राथमिकता। डीलर ने आज या कल आवेदन किया था।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 029f192b703c661f
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जिनकी आपके एक्सेस में इजाज़त है।

- sdwa-admin | color: #1d4ed8
  title: SDWA Admin
  - Admin Dashboard पर सभी वेलफ़ेयर आवेदनों पर नज़र रखना
  - **Quick actions** से बाकी मंज़ूरियाँ खोलना
  - गेस्ट हाउस बुकिंग जाँचना
- office | color: #0d9488 | roles: Admin, CorporateAdmin, Director
  title: Admin, Corporate Admin, Director
  - हर वह आवेदन देखना जिसकी जाँच अभी चल रही है
  - डीलर और गेस्ट हाउस के आँकड़े देखना
  - Reports Center खोलना
- approvers | color: #d97706 | roles: MO, RM, SMM
  title: MO, RM, SM
  - अगर आप Admin Dashboard खोल सकते हैं, तो कतार में आपके स्टेज के आवेदन दिखते हैं
  - आपको सिर्फ़ अपना HQ, रीजन या राज्य दिखता है
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7222902e690b16b2
confidence: ok

- dash | popular: true
  q: कुछ बॉक्स में नंबर की जगह — दिखता है
  a: या तो डेटा लोड नहीं हुआ (ऊपर लाल बॉक्स में लिखा होता है कि कौन सा हिस्सा), या उस बॉक्स के लिए अभी कोई डेटा नहीं है। **Active Schemes**, **Benefit Released**, **SLA Delayed**, **Upcoming Check-ins**, **Cancelled**, **Revenue Collected** और **Finance & Benefit Release** वाले बॉक्स अभी हमेशा — दिखाते हैं।
- download | popular: true
  q: Reports Center में Download दबाने पर कुछ नहीं होता
  a: Reports Center अभी बन रहा है। **Download** से अभी कोई फ़ाइल नहीं बनती, और इस पर दिखने वाली रिपोर्ट और आँकड़े सिर्फ़ नमूने हैं, आपका असली डेटा नहीं।
- icons
  q: कतार में टिक और क्रॉस वाले आइकन कुछ नहीं करते
  a: ये आइकन अभी काम नहीं करते। **Quick actions** के नीचे **Review Pending Approvals** दबाएँ और **Scheme Approval** स्क्रीन पर मंज़ूर या रिजेक्ट करें।
- nothing-pending
  q: कतार में "Nothing pending" (कुछ भी बाकी नहीं) दिख रहा है
  a: आपके स्टेज पर कोई आवेदन इंतज़ार नहीं कर रहा है। MO, RM और SM को सिर्फ़ अपना HQ, रीजन या राज्य दिखता है।
- could-not-load
  q: मुझे "Could not load: welfare approvals" दिख रहा है
  a: वेलफ़ेयर का डेटा सिर्फ़ MO, RM, SM, SDWA, SDWA Admin, Admin, Corporate Admin और Director को दिखता है। अगर आपको यह दिखना चाहिए, तो अपने admin से अपना एक्सेस जाँचने को कहें, फिर पेज दोबारा खोलें।
:::

::: section admin-dashboard
title: Admin Dashboard पढ़ें
routes: /ReportDashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=rpt-kpis] | 2 > [data-help=rpt-workflow] | 3 > [data-help=rpt-queue] | 4 > [data-help=rpt-charts] | 5 > [data-help=rpt-guesthouse] | 6 > [data-help=rpt-quick]
minutes: 3
source-hash: c608b65421e5a6ab
confidence: ok

**यह स्क्रीन किस काम की है**
SDWA Admin के लिए बने **Dashboard** पर वेलफ़ेयर आवेदन, डीलर और गेस्ट हाउस बुकिंग एक ही पेज पर दिखती हैं। इसे मेन्यू में **SDWA** > **Admin Dashboard** से खोलें। यहाँ आप सिर्फ़ देखते हैं। मंज़ूरी **Scheme Approval** स्क्रीन पर दी जाती है।

1. ऊपर के कार्ड देखें: **Total Welfare Applications**, **Pending Final Approval**, **Approved Applications**, **Rejected Applications**, **Total Dealers** और **Guest House Bookings**।
2. **Approval Workflow Overview** में **Dealer** से **Completed** तक के स्टेज देखें, और यह कि कितने आवेदन **Pending at MO**, **Pending at RM**, **Pending at SMM**, **Pending at SDWA** और **Rejected** हैं।
3. **Pending Action Queue (SDWA)** में इंतज़ार कर रहा हर आवेदन उसके **Current Stage**, **Pending Since** और **Priority** के साथ देखें। और लाइनें देखने के लिए **Show more** दबाएँ।
4. **Scheme Distribution** दिखाता है कि हर स्कीम में कितने आवेदन हैं। **Monthly Application Trend** हर महीने के आवेदन दिखाता है। इनके नीचे डीलर की गिनती और **Regional Distribution** टेबल है।
5. **Guest House Overview** बुकिंग दिखाता है और यह भी कि हर गेस्ट हाउस कितना भरा है। फ़्रंट ऑफ़िस खोलने के लिए **View All Bookings** दबाएँ।
6. **Quick actions** के नीचे आवेदन मंज़ूर करने के लिए **Review Pending Approvals** दबाएँ। **Dealer Directory**, **Manage Schemes** और **Guest House Bookings** अपनी-अपनी स्क्रीन खोलते हैं।

> Careful: कतार में आँख, टिक, क्रॉस और डाउनलोड वाले आइकन, **Search zones...** बॉक्स, और **Generate Reports** और **Gallery** कार्ड अभी काम नहीं करते।

## स्क्रीन के बाकी हिस्से

- **Recent Activities** आख़िरी 5 घटनाएँ दिखाता है, जैसे नया आवेदन, कोई मंज़ूरी या गेस्ट हाउस का बिल।
- **Finance & Benefit Release** में अभी कोई डेटा नहीं है। इसमें — और "No payment releases" (कोई पेमेंट जारी नहीं हुआ) दिखता है।

## आम दिक्कतें

- **लाल बॉक्स में "Could not load" लिखा है।** डेटा का वह हिस्सा लोड नहीं हुआ। उसके बॉक्स में — दिखता है। थोड़ी देर बाद पेज फिर से खोलें।
- **Pending at MO की गिनती ज़्यादा लगती है।** इसमें वे आवेदन भी गिने जाते हैं जो सुधार के लिए डीलर को लौटाए गए हैं।
- **Pending Since बहुत ज़्यादा लगता है।** यह डीलर के आवेदन करने के दिन से गिनता है, आवेदन के इस स्टेज पर पहुँचने के दिन से नहीं।
- **कतार में "Nothing pending" दिख रहा है।** अभी आपके स्टेज पर कोई आवेदन इंतज़ार नहीं कर रहा।
:::

::: section reports-center
title: Reports Center देखें
routes: /ReportsCenter
for: 
roles: 
designations: 
minutes: 1
source-hash: fea64cc958f10b95
confidence: ok

**यह स्क्रीन किस काम की है**
**Reports Center** पर आप बुकिंग, बिलिंग, मंज़ूरी और डीलर की रिपोर्ट देख और डाउनलोड कर सकेंगे। यह अभी बन रहा है: इस पर दिखने वाले आँकड़े, रिपोर्ट और तारीखें सिर्फ़ नमूने हैं, और फ़िल्टर और डाउनलोड अभी काम नहीं करते।

1. ऊपर के कार्ड देखें: **Total Reports**, **Booking Reports**, **Billing Reports** और **Approval Reports**।
2. टैब (**All Reports**, **Booking Reports**, **Billing Reports**, **Approval Reports**), **Search** बॉक्स और **All Categories** और **All Formats** लिस्ट दिखती हैं, पर इनसे अभी लिस्ट नहीं बदलती।
3. हर रिपोर्ट कार्ड पर रिपोर्ट का नाम, उसमें क्या है, **Available Formats** और एक तारीख दिखती है। कार्ड ये हैं: **Dealer Information**, **Tirupathi Bookings**, **T-Nagar Bills**, **MO Reports**, **RM Reports** और **Tirupathi Bills**।
4. **Category**, **Records**, **File Types**, **Generated By** और एक छोटी टेबल वाला नमूना खोलने के लिए **Preview** दबाएँ। इसे बंद करने के लिए **Close** या X दबाएँ।
5. **Download**, **Download PDF** और **Download Excel** से अभी कोई फ़ाइल नहीं बनती।

> Careful: इस स्क्रीन के आँकड़े इस्तेमाल न करें। ये सिर्फ़ नमूने हैं, आपका असली डेटा नहीं।

## आम दिक्कतें

- **Download दबाने पर कुछ नहीं होता।** डाउनलोड अभी तैयार नहीं हैं।
- **हर Preview में Dealer Information ही दिखता है।** अभी सभी Preview बटन एक ही नमूना खोलते हैं।
- **टैब और सर्च से लिस्ट नहीं बदलती।** ये अभी काम नहीं करते।
:::
