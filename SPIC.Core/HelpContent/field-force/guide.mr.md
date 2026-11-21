---
module: field-force
title: फील्ड फोर्स
icon: bi-person-walking
order: 107
language: mr
category: फील्डचे काम
note: नवीन गाईड
meta-source-hash: c6011d305dce737c
---

::: section overview
title: हा मॉड्युल तुमच्यासाठी काय करतो
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: d557872762e3ea47
confidence: ok

प्रत्येक आठवड्याच्या फील्डवरच्या कामाचा प्लॅन करा, त्याला मंजुरी घ्या, हजेरी लावा आणि फील्डवर टास्क पूर्ण करा. कोण काम करत आहे, ते कुठे आहेत आणि प्रत्येक जण टार्गेटच्या किती जवळ आहे, हे मॅनेजरना दिसते.

- plan | icon: bi-calendar-week | color: #ea580c
  title: JMDO आठवड्याचा प्लॅन करतात
  text: प्रत्येक दिवसासाठी डीलर निवडा, SPCM, Soil Sample आणि PoS liquidation चे टार्गेट ठरवा, आणि प्लॅन पाठवा.
- approve | icon: bi-person-check | color: #2563eb
  title: MDO त्याला मंजुरी देतात
  text: MDO प्लॅन तपासतात, गरज असेल तर डीलर, तारखा किंवा टार्गेट बदलतात, आणि तो मंजूर करतात.
- work | icon: bi-geo-alt | color: #16a34a
  title: फील्डवर काम, आणि त्यावर थेट लक्ष
  text: JMDO हजेरी लावतात, प्रत्येक टास्क सुरू करतात आणि बंद करतात. मॅनेजर टास्क, लोकेशन आणि रिपोर्ट पाहत राहतात.
:::

::: section flow
title: फील्डवरच्या कामाचा एक आठवडा कसा पुढे जातो, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2462b99188302817
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- jmdo | type: lane | color: #ea580c | roles: JMDO
  title: JMDO
  sub: फील्ड कर्मचारी
- mdo | type: lane | color: #2563eb | roles: MDO, MO
  title: MDO
  sub: तुमच्या HQ वरचे टीम मॅनेजर
- plan | lane: jmdo | col: 1 | icon: bi-calendar-week | color: #d97706 | next: approve | screen: plan-week
  title: आठवड्याचा प्लॅन करा
  status: Pending
  text: प्रत्येक दिवसासाठी डीलर निवडा, आठवड्याचे टार्गेट टाका आणि **Request** दाबा. प्लॅन MDO ची वाट पाहतो.
- approve | lane: mdo | col: 2 | icon: bi-person-check | color: #2563eb | next: attend | screen: review-allocation
  title: तपासा आणि मंजूर करा
  status: Approved
  text: MDO डीलर टास्क जोडू किंवा काढू शकतात, तारखा, प्राधान्य आणि टार्गेट बदलू शकतात, मग **Approve Allocation** दाबतात.
- attend | lane: jmdo | col: 3 | icon: bi-calendar-check | color: #0d9488 | next: work | screen: mark-attendance
  title: हजेरी लावा
  status: Present
  text: कामाच्या प्रत्येक दिवशी, कोणताही टास्क सुरू करण्याआधी SPIC ONE मोबाईल ॲपमध्ये हजेरी लावा.
- work | lane: jmdo | col: 4 | icon: bi-play-circle | color: #7c3aed | next: close | screen: todays-tasks
  title: टास्क सुरू करा
  status: In Progress
  text: **Today's Allocation** वर **Start** दाबा, मग **Start Task** दाबा. वेळ आणि लोकेशनची नोंद होते.
- close | lane: jmdo | col: 5 | icon: bi-check-circle | color: #16a34a | next: monitor | screen: todays-tasks
  title: सेव्ह करा आणि बंद करा
  status: Completed
  text: **Close-out form** भरा, **Save Record** दाबा, मग टास्क बंद करण्यासाठी **Stop Task** दाबा.
- monitor | lane: mdo | col: 6 | icon: bi-list-check | color: #475569 | screen: task-monitoring
  title: पाठपुरावा करा
  status: Completed
  text: MDO प्रत्येक टास्क **Task Monitoring** वर, टीमचे लोकेशन **Team Tracking** वर, आणि प्रगती **Reports** वर पाहतात.
- no-attendance | lane: jmdo | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: work | to: attend | screen: mark-attendance
  title: हजेरी लावलेली नाही
  status: Yet to Start
  text: आजची हजेरी लावल्याशिवाय टास्क सुरू होत नाही. ॲप तुमच्यासाठी **Mark Attendance** उघडते.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 80c93f090c1c74d0
confidence: ok

- pending | color: #d97706
  label: Pending
  text: आठवड्याचा प्लॅन पाठवला आहे आणि MDO च्या मंजुरीची वाट पाहत आहे. JMDO ला "awaiting MDO approval" दिसते.
- approved | color: #16a34a
  label: Approved
  text: MDO ने आठवड्याचा प्लॅन मंजूर केला. त्यातले टास्क त्यांच्या दिवशी सुरू करता येतात.
- rejected | color: #dc2626
  label: Rejected
  text: आठवड्याचा प्लॅन स्वीकारला गेला नाही. प्लॅन बदला आणि परत पाठवा.
- requested | color: #d97706
  label: Requested
  text: Task Monitoring वर, असा टास्क ज्याचा आठवड्याचा प्लॅन अजून मंजुरीची वाट पाहत आहे.
- yet-to-start | color: #64748b
  label: Yet to Start
  text: टास्क मंजूर आहे पण अजून सुरू झालेला नाही.
- not-started | color: #64748b
  label: Not Started
  text: Reports वर, हे Yet to Start सारखेच आहे.
- in-progress | color: #2563eb
  label: In Progress
  text: JMDO ने Start Task दाबले आहे आणि ते त्यावर काम करत आहेत.
- completed | color: #16a34a
  label: Completed
  text: टास्क बंद झाला आहे.
- fully | color: #16a34a
  label: Fully Completed
  text: Close-out चा निकाल: सगळे काम झाले.
- partially | color: #d97706
  label: Partially Completed
  text: Close-out चा निकाल: कामाचा फक्त काही भाग झाला.
- not-completed | color: #ea580c
  label: Not Completed
  text: Close-out चा निकाल: काम होऊ शकले नाही.
- present | color: #16a34a
  label: Present
  text: पूर्ण दिवसाची हजेरी. यामुळे तुम्ही On Duty मध्ये येता.
- half-day | color: #0d9488
  label: Half Day
  text: अर्ध्या दिवसाची हजेरी. यामुळे तुम्ही On Duty मध्ये येता.
- on-leave | color: #7c3aed
  label: On Leave
  text: आज तुम्ही रजेवर आहात. तुम्ही Off Duty आहात.
- absent | color: #dc2626
  label: Absent
  text: गैरहजर म्हणून लावलेल्या दिवसासाठी आठवड्याच्या सारांशात दिसते.
- active | color: #16a34a
  label: Active
  text: टीम डॅशबोर्डवर, असा JMDO जो आज रजेवर नाही.
- on-duty | color: #16a34a
  label: On Duty
  text: आज हजेरी Present किंवा Half Day म्हणून लावली आहे.
- off-duty | color: #64748b
  label: Off Duty
  text: आज अजून हजेरी लावलेली नाही, किंवा On Leave.
- live | color: #16a34a
  label: Live
  text: Live Tracking चालू आहे आणि लोकेशन आत्ता येत आहे.
- stale | color: #d97706
  label: Stale
  text: Live Tracking चालू आहे, पण गेल्या काही मिनिटांत नवीन लोकेशन आले नाही.
- offline | color: #dc2626
  label: Offline
  text: Live Tracking चालू आहे, पण 15 मिनिटांपेक्षा जास्त वेळ लोकेशन आले नाही.
- auto-ended | color: #475569
  label: Ended automatically
  text: ट्रॅकिंग बराच वेळ शांत होते, म्हणून ॲपने ते बंद केले.
- stopped | color: #64748b
  label: Stopped
  text: JMDO ने Stop Tracking दाबले.
- never | color: #94a3b8
  label: Never started
  text: JMDO ने Live Tracking सुरू केलेले नाही.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: ca8f05faf80da07f
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- jmdo | color: #ea580c | roles: JMDO
  title: JMDO (फील्ड कर्मचारी)
  - आठवड्याचा प्लॅन करा आणि Request दाबा
  - कामाच्या प्रत्येक दिवशी हजेरी लावा
  - प्रत्येक टास्क सुरू करा, सेव्ह करा आणि बंद करा
  - Live Tracking सुरू करा आणि बंद करा
  - My Reports तपासा
- mdo | color: #2563eb | roles: MDO, MO
  title: MDO
  - तुमच्या JMDO चे आठवड्याचे प्लॅन मंजूर करा
  - प्रत्येक JMDO चे वर्षाचे टार्गेट ठरवा
  - टास्क, टीमचे लोकेशन आणि रिपोर्ट पाहत राहा
- admin | color: #475569 | roles: Admin
  title: Admin
  - प्रत्येक HQ चे प्लॅन पाहा
  - मंजूर झाल्यानंतरही प्लॅन दुरुस्त करा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e1bd21cb8311eb35
confidence: ok

- cannot-start | popular: true
  q: मी माझा टास्क सुरू करू शकत नाही
  a: दोन गोष्टी झालेल्या पाहिजेत. तुमचा आठवड्याचा प्लॅन MDO ने **Approved** केलेला असावा, आणि तुम्ही आजची हजेरी लावलेली असावी. हजेरी लावलेली नसेल तर ॲप तुमच्यासाठी **Mark Attendance** उघडते.
- web-attendance | popular: true
  q: Mark Attendance वर दिसते की हजेरी फक्त मोबाईल ॲपमधूनच लावता येते
  a: हजेरी, Live Tracking आणि Track Playback फक्त तुमच्या फोनवरच्या SPIC ONE ॲपमध्ये चालतात. फोनवर ॲप उघडा आणि परत प्रयत्न करा.
- already-sent
  q: "You've already submitted an allocation for that week." (त्या आठवड्याचा प्लॅन तुम्ही आधीच पाठवला आहे) असे दिसते
  a: एका आठवड्यासाठी फक्त एकच प्लॅन पाठवता येतो. डीलर जोडायला किंवा काढायला, किंवा तारखा आणि टार्गेट बदलायला तुमच्या MDO ना **Allocation Requests** स्क्रीनवर सांगा.
- spcm-not-closed
  q: टास्क बंद करताना "SPCM target not yet reached" (SPCM टार्गेट अजून पूर्ण झाले नाही) असे दिसते
  a: SPCM आणि PoS liquidation चे टास्क आठवड्याचे टार्गेट पूर्ण झाल्यावरच बंद होतात. इतर दिवशी रेकॉर्ड सेव्ह करत राहा. टार्गेट पूर्ण झाले की टास्क आपोआप बंद होतो.
- no-jmdos
  q: मला "No JMDOs found for your headquarters" दिसते
  a: तुमच्याच HQ वरचे JMDO तुमची टीम असतात. कोणी दिसत नसेल, तर त्यांच्या प्रोफाइलवरचे HQ तपासायला admin ला सांगा.
- offline
  q: Team Tracking वर एखाद्या JMDO साठी Stale किंवा Offline दिसते
  a: JMDO च्या फोनने काही वेळ लोकेशन पाठवलेले नाही. कदाचित नेटवर्क नसेल किंवा ॲप बंद असेल. JMDO ला फोन करा आणि ॲप उघडायला सांगा.
:::

::: section field-dashboard
title: माझा फील्ड डॅशबोर्ड
routes: /FieldDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: ad0c345e7985b577
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Dashboard** ही JMDO ची होम स्क्रीन आहे. तुम्ही ड्युटीवर आहात का, आज तुमचे किती टास्क आहेत, Live Tracking, आणि तुम्ही पूर्ण न केलेले टास्क इथे दिसतात.

1. वर तुमचे नाव आणि **On Duty** किंवा **Off Duty** पाहा. हजेरी लावली नसेल तर **Mark Attendance** दाबा.
2. कार्डे पाहा: **Today's Tasks**, **Yet to Start**, **In Progress** आणि **Completed**.
3. Live Tracking चालू करण्यासाठी **Start Live Tracking** दाबा. **Background location access** मेसेज वाचा आणि **Continue** दाबा.
4. ट्रॅकिंग चालू असताना तुमचा रस्ता पाहण्यासाठी **View Route** दाबा, किंवा ते बंद करण्यासाठी **Stop Tracking** दाबा आणि परत **Stop Tracking** दाबा. **Keep Tracking** दाबल्यास ट्रॅकिंग बंद होत नाही.
5. **Continue where you left off** खाली, चालू असलेल्या टास्कवर परत जाण्यासाठी **view** दाबा. **View All** दाबल्यावर **Tasks Allocation** उघडते.

> Tip: तुम्ही बटण दाबल्यावरच Live Tracking सुरू होते. हजेरी किंवा टास्कबरोबर ते आपोआप सुरू होत नाही.

## नेहमीच्या अडचणी

- **"Mark today's attendance first to enable Start Live Tracking."** (Start Live Tracking साठी आधी आजची हजेरी लावा) हजेरी लावा, मग परत या.
- **"Mobile app only".** (फक्त मोबाईल ॲप) Live Tracking फक्त तुमच्या फोनवरच्या SPIC ONE ॲपमधूनच सुरू करता येते.
- **"Nothing in progress right now".** (आत्ता काहीही चालू नाही) तुम्ही कोणताही टास्क सुरू केलेला नाही. टास्क सुरू करण्यासाठी **Tasks Allocation** उघडा.
:::

::: section plan-week
title: माझ्या आठवड्याच्या टास्कचा प्लॅन करा आणि विनंती पाठवा
routes: /TasksAllocation
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-week] | 2 > [data-help=ff-steps] | 3 > [data-help=ff-days] | 4 > [data-help=ff-filters] | 5 > [data-help=ff-dealers] | 6 > [data-help=ff-next]
minutes: 4
source-hash: 3effab3c5a7b4e53
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Tasks Allocation** वरच्या **All Allocations** टॅबवर JMDO पाच टप्प्यांत एका आठवड्याचा (रविवार ते शनिवार) प्लॅन करतात आणि मंजुरीसाठी MDO कडे पाठवतात.

1. तुम्हाला ज्या आठवड्याचा प्लॅन करायचा आहे तो निवडण्यासाठी **Previous Week** आणि **Next Week** वापरा.
2. डावीकडचे टप्पे आहेत **Dealer**, **SPCM**, **Soil Sample**, **MD Program** आणि **PoS liquidation**. टप्पा उघडण्यासाठी तो टॅप करा.
3. **Dealer** टप्प्यात, ज्या दिवसाचा प्लॅन करत आहात तो निवडण्यासाठी दिवसाचा टॅब टॅप करा.
4. सर्च बॉक्स (**Search dealer name or code...**) किंवा **All Districts**, **All Sub Districts** आणि **All Dealers** लिस्ट वापरून डीलर शोधा.
5. त्या दिवसासाठी डीलर निवडायला डीलरचे कार्ड टॅप करा. काढून टाकायला ते परत टॅप करा. दिवसाच्या टॅबवर तुम्ही किती डीलर निवडले ते दिसते.
6. **Next: SPCM Target** दाबा. SPCM आणि Soil Sample साठी **This Week's Target** टाका, MD प्रोग्राम तपासा, PoS liquidation ची संख्या टाका (**Urea**, **DAP**, **NPKS**, **Others**), मग **Request** दाबा.

> Tip: CSR1 मध्ये तुम्हाला दिलेले MD प्रोग्राम आपोआप जोडले जातात. ते तुम्ही इथे जोडू किंवा काढू शकत नाही.
> Careful: एका आठवड्यासाठी फक्त एकच प्लॅन पाठवता येतो. **Request** दाबल्यानंतर फक्त MDO च तो बदलू शकतात.

| फील्ड | काय भरायचे |
|---|---|
| **This Week's Target** (SPCM) | या आठवड्यात तुम्ही किती SPCM पूर्ण करायचा प्लॅन करत आहात. |
| **This Week's Target** (Soil Sample) | या आठवड्यात तुम्ही किती माती नमुने गोळा करायचा प्लॅन करत आहात. |
| **Urea**, **DAP**, **NPKS**, **Others** | या आठवड्यात प्रत्येक प्रॉडक्टसाठी प्लॅन केलेली PoS liquidation ची संख्या. |

## नेहमीच्या अडचणी

- **"Fill in at least one step before submitting."** (पाठवण्याआधी किमान एक टप्पा भरा) किमान एक डीलर निवडा किंवा किमान एक टार्गेट टाका.
- **"You've already submitted an allocation for that week."** (त्या आठवड्याचा प्लॅन तुम्ही आधीच पाठवला आहे) प्लॅन बदलायला तुमच्या MDO ना **Allocation Requests** वर सांगा.
- **"Allocation submitted — awaiting MDO approval".** (प्लॅन पाठवला आहे, MDO च्या मंजुरीची वाट) प्लॅन पाठवला गेला आहे. MDO ने मंजूर केल्यावर टास्क **Today's Allocation** वर उघडतात.
- **"Allocation Rejected".** (प्लॅन नाकारला) **Update & Resubmit** दाबा, प्लॅन दुरुस्त करा आणि परत **Request** दाबा.
:::

::: section mark-attendance
title: आजची हजेरी लावा
routes: /MarkAttendance
for: 
roles: 
designations: 
minutes: 2
source-hash: 8b14e6f020b3c82e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
कामाच्या प्रत्येक दिवशी कोणताही टास्क सुरू करण्याआधी तुमची हजेरी लावा. हे फक्त तुमच्या फोनवरच्या SPIC ONE ॲपमध्येच चालते, कारण तुम्ही कुठे आहात याची नोंद होते.

1. मेनूमधून **Mark Attendance** उघडा, किंवा **My Dashboard** वर **Mark Attendance** दाबा.
2. ॲपला तुमचे लोकेशन वापरू द्या. **Visit Area / Place** तुमच्या लोकेशनवरून भरले जाते. गरज असेल तर ते दुरुस्त करा, किंवा लोकेशन सापडले नाही तर ते टाइप करा.
3. **Date** तपासा.
4. **Attendance Status** निवडा: **Present**, **Half Day** किंवा **On Leave**.
5. **Update Attendance** दाबा. तुम्हाला "Attendance updated." (हजेरी अपडेट झाली) दिसते आणि तुम्ही **My Dashboard** वर परत जाता.
6. त्याच दिवशी नंतर बदल करायचा असेल तर **Attendance marked today** शेजारी **Edit** दाबा.
7. उजवीकडे **Current Status** (**On Duty** किंवा **Off Duty**) आणि **Weekly Summary** पाहा: प्रत्येक दिवसाची हजेरी, कामाचे तास, सुरू केलेले टास्क आणि अंतर.

> Careful: सकाळी 10:00 नंतर स्क्रीनवर "Submitted late for today" (आज उशिरा लावली) दिसते. तरीही तुम्ही ती सेव्ह करू शकता.

| फील्ड | काय भरायचे |
|---|---|
| **Date** | आजची तारीख. भरणे आवश्यक. |
| **Visit Area / Place** | तुम्ही जिथे काम करत आहात ते गाव किंवा शहर. लोकेशन मिळाले नसेल तर भरणे आवश्यक. |
| **Attendance Status** | **Present**, **Half Day** किंवा **On Leave**. |

## नेहमीच्या अडचणी

- **"Attendance can only be marked from the SPIC mobile app".** (हजेरी फक्त SPIC मोबाईल ॲपमधूनच लावता येते) तुमच्या फोनवर SPIC ONE उघडा.
- **"Location could not be captured. Please type your Visit Area / Place to save attendance."** (लोकेशन मिळाले नाही, हजेरी सेव्ह करण्यासाठी ठिकाण टाइप करा) लोकेशन चालू करा, किंवा ठिकाण स्वतः टाइप करा.
- **"Date is required."** (तारीख भरणे आवश्यक आहे) **Update Attendance** दाबण्याआधी तारीख निवडा.
:::

::: section todays-tasks
title: आजचे टास्क सुरू करा, सेव्ह करा आणि बंद करा
routes: /TaskDetail
for: 
roles: 
designations: 
minutes: 4
source-hash: 163e059a3e7fb61f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
प्रत्येक मंजूर टास्क त्याच्या स्वतःच्या स्क्रीनवर उघडतो. तुम्ही तो सुरू करता, काय केले त्याची नोंद करता, आणि तो बंद करता. टास्कवर किती वेळ लागला आणि तुम्ही तो कुठे सुरू केला याची नोंद ॲप ठेवते.

1. **Tasks Allocation** वर **Today's Allocation** उघडा. **Yet to Start**, **In Progress**, **Completed** किंवा **All** निवडा, मग टास्क कार्डवर **Start** (किंवा **Continue**) दाबा.
2. **Task Timeline** (**Assigned**, **Requested**, **Approved**, **Started**, **Closed**) आणि टास्कचा तपशील तपासा.
3. **Start Task** दाबा. ॲप तुमचे लोकेशन घेते आणि **Time on task** घड्याळ सुरू करते.
4. **Close-out form** मध्ये **Outcome** निवडा. डीलरसाठी **+ Add another Product** दाबा आणि **Product Name** आणि **Stock** टाका. SPCM साठी **Achieved Target** टाका. PoS liquidation साठी **Urea**, **NPKS**, **DAP** आणि **Other** टाका.
5. **Remarks** लिहा, **Photo Proof** (डीलर) किंवा **Supporting Document** जोडा, आणि **Save Record** दाबा. सेव्ह केलेले रेकॉर्ड उजवीकडच्या टेबलमध्ये दिसतात.
6. भेटीत गरज असेल तर **Quick actions** वापरा: **Soil Sample Collection** किंवा **Add Farmer Data**.
7. काम पूर्ण झाल्यावर **Stop Task** दाबा, मग **Close Task** दाबा.

> Careful: बंद केलेला टास्क परत सुरू करता येत नाही.
> Tip: आठवड्याचे टार्गेट पूर्ण झाले की SPCM आणि PoS liquidation चे टास्क आपोआप बंद होतात ("Record saved - weekly target reached, task completed!").

## नेहमीच्या अडचणी

- **"Allocation Awaiting MDO Approval".** (प्लॅन MDO च्या मंजुरीची वाट पाहत आहे) MDO तुमचा आठवड्याचा प्लॅन मंजूर करेपर्यंत **Start Task** बटण राखाडी राहते.
- **"Mark today's attendance before starting a task."** (टास्क सुरू करण्याआधी आजची हजेरी लावा) ॲप **Mark Attendance** उघडते. हजेरी लावा, मग टास्क सुरू करा.
- **"Please start the task before saving records."** (रेकॉर्ड सेव्ह करण्याआधी टास्क सुरू करा) आधी **Start Task** दाबा.
- **Soil Sample चे कार्ड Sample Collection उघडते.** हे नेहमीचेच आहे. माती नमुन्यांची नोंद नमुना गोळा करण्याच्या स्क्रीनवर होते.
:::

::: section task-history
title: माझ्या टास्कचा इतिहास आणि आठवड्याचा सारांश पाहा
routes: /TasksAllocation
for: 
roles: 
designations: 
minutes: 2
source-hash: 5bb6d9cb3b6d9f29
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Tasks Allocation** वरच्या **Task History** आणि **Task Summary** टॅबवर प्रत्येक आठवड्याचे तुमचे टास्क दिसतात, आणि प्रत्येक आठवड्याच्या टार्गेटपैकी किती काम झाले तेही दिसते.

1. **Tasks Allocation** वर **Task History** उघडा. ते आजच्या टास्कवर उघडते.
2. **From**, **To**, **Dealer** आणि **Status** निवडा, मग **Search** दाबा. **Today** दाबल्यावर परत आजच्या दिवसावर येता.
3. प्रत्येक कार्डवर टास्क आणि त्याची स्थिती दिसते. तो उघडण्यासाठी **View Details** किंवा **Continue** दाबा.
4. **Task Summary** उघडा. कार्डांवर **Total Assigned**, **Yet to Start**, **In Progress** आणि **Completed** दिसतात.
5. प्रत्येक आठवड्याची प्लॅनची स्थिती, **Complete** टक्केवारी, **Dealer Visits**, **MD Programs** आणि प्रत्येक टार्गेटचे **Target**, **Achieved** आणि **Pending** दिसतात.
6. **Day-wise Dealer Visits** खाली प्रत्येक दिवशी किती डीलर भेटी झाल्या आणि किती बाकी आहेत ते पाहा.

## नेहमीच्या अडचणी

- **"No tasks match this filter".** (या फिल्टरनुसार एकही टास्क नाही) तारखांची रेंज वाढवा किंवा **Status** ला **All** करा.
- **"Detailed view for this task type isn't built yet."** (या प्रकारच्या टास्कचा तपशील अजून तयार नाही) Soil Sample टास्कसाठी इथे तपशीलाची स्क्रीन नाही.
:::

::: section my-reports
title: माझे रिपोर्ट
routes: /FieldReports
for: 
roles: 
designations: 
minutes: 1
source-hash: 1d05a6505b4982e6
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Reports** वर या महिन्यात तुम्ही किती टास्क बंद केले आणि किती लवकर, ते दिसते.

1. कार्डे पाहा: **Completed**, **Avg. Close Time**, **Soil Samples** आणि **Farmers Added**.
2. **Task closures** टेबलमध्ये बंद झालेला प्रत्येक टास्क **Date**, **Task**, **Type** आणि **Outcome** सह दिसतो.
3. टास्क शोधण्यासाठी **Search...** मध्ये टाइप करा.
4. लिस्ट सेव्ह किंवा प्रिंट करण्यासाठी **Copy**, **CSV**, **Excel**, **PDF** किंवा **Print** वापरा.

## नेहमीच्या अडचणी

- **"Nothing closed out this month yet".** (या महिन्यात अजून काहीही बंद झालेले नाही) या महिन्यात तुम्ही एकही टास्क बंद केलेला नाही.
:::

::: section track-playback
title: माझ्या ट्रॅकिंगचा रस्ता परत पाहा
routes: /TrackPlayback
for: 
roles: 
designations: 
minutes: 2
source-hash: cd8704c91e7b24e5
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Track Playback** वर गेल्या 7 दिवसांच्या तुमच्या Live Tracking चा रस्ता नकाशावर दिसतो. हे फक्त तुमच्या फोनवरच्या SPIC ONE ॲपमध्येच चालते.

1. मेनूमधून **Track Playback** उघडा, किंवा **My Dashboard** वर **View Route** दाबा, किंवा **Mark Attendance** वर **View Tracking Details** दाबा.
2. ट्रॅकिंग चालू असेल तर **Tracking Active** कार्डवर वेळ दिसते. ते बंद करण्यासाठी **Stop Tracking** दाबा, किंवा दिशा पाहण्यासाठी **Directions to live location** दाबा.
3. **Other playbacks available** खाली, त्या दिवसाचा रस्ता पाहण्यासाठी दिवस टॅप करा.
4. नकाशावर play दाबा, रस्त्यावर पुढे-मागे जाण्यासाठी स्लायडर ओढा, आणि वेग निवडा (**1x**, **2x** किंवा **4x**).

## नेहमीच्या अडचणी

- **"No tracking sessions in the last 7 days".** (गेल्या 7 दिवसांत एकही ट्रॅकिंग नाही) रस्त्याची नोंद करण्यासाठी **My Dashboard** वरून Live Tracking सुरू करा.
- **"Track Playback can only be used from the SPIC mobile app".** (Track Playback फक्त SPIC मोबाईल ॲपमधूनच वापरता येते) तुमच्या फोनवर SPIC ONE उघडा.
:::

::: section team-dashboard
title: टीम डॅशबोर्ड
routes: /MdoDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: f544adf24e2f0b9c
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Team Dashboard** वर MDO ला त्याच HQ वरच्या सगळ्या JMDO च्या टास्कच्या कामाचा एकत्र आढावा दिसतो.

1. कार्डे पाहा: **Total Tasks**, **Unassigned**, **Requested by JMDO**, **Approved · Today**, **In Progress** आणि **Completed**.
2. **JMDO-wise summary** मध्ये प्रत्येक JMDO **Active** किंवा **On Leave** सह, आणि **Assigned**, **In Progress** आणि **Completed** च्या संख्या पाहा.
3. आठवड्याचे प्लॅन मंजूर करण्यासाठी **Allocation Requests** दाबा.
4. नवीन आकडे पाहण्यासाठी **Refresh** दाबा.

> Tip: **Unassigned** मध्ये असे JMDO मोजले जातात ज्यांनी एकही आठवड्याचा प्लॅन पाठवलेला नाही. **In Progress** मध्ये आज ड्युटीवर असलेले JMDO मोजले जातात.

## नेहमीच्या अडचणी

- **"No JMDOs found for your headquarters".** (तुमच्या HQ साठी एकही JMDO सापडला नाही) तुमच्याच HQ वर एकही JMDO नाही. प्रोफाइलवरचे HQ तपासायला admin ला सांगा.
:::

::: section allocation-requests
title: मंजुरीसाठी वाट पाहणारे आठवड्याचे प्लॅन पाहा
routes: /AllocationRequests
for: 
roles: 
designations: 
minutes: 2
source-hash: d233ce1bd4e689c1
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Allocation Requests** वर तुमच्या JMDO नी मंजुरीसाठी पाठवलेले आठवड्याचे प्लॅन दिसतात. Admin ला मंजूर झालेले प्लॅन आणि **Status** कॉलमही दिसतो.

1. वरचा निळा बॉक्स वाचा. किती JMDO तुमच्या मंजुरीची वाट पाहत आहेत ते त्यात दिसते.
2. **Search...**, **All Districts**, **All Sub Districts** आणि **All JMDOs** वापरून लिस्ट कमी करा.
3. प्रत्येक ओळीत **Submitted**, **Dealer Tasks**, **SPCM Target**, **Soil Target**, **MD Programs** आणि **Pos Liquidation** दिसतात.
4. प्लॅन उघडण्यासाठी **Review** दाबा. मंजूर झालेल्या प्लॅनसाठी Admin ला त्याऐवजी **Edit** दिसते.
5. लिस्ट सेव्ह किंवा प्रिंट करण्यासाठी **Copy**, **CSV**, **Excel**, **PDF** किंवा **Print** वापरा.

## नेहमीच्या अडचणी

- **"No pending allocation requests".** (एकही प्लॅन बाकी नाही) आत्ता एकही JMDO तुमची वाट पाहत नाही.
- **"No allocation requests match these filters".** (या फिल्टरनुसार एकही प्लॅन नाही) फिल्टर परत **All Districts**, **All Sub Districts** आणि **All JMDOs** वर करा.
:::

::: section review-allocation
title: आठवड्याचा प्लॅन तपासा आणि मंजूर करा
routes: /AllocationRequestReview
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-ar-dealers] | 2 > [data-help=ff-ar-add] | 3 > [data-help=ff-ar-targets] | 4 > [data-help=ff-ar-programs] | 5 > [data-help=ff-ar-approve]
minutes: 3
source-hash: 791e48a894306411
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Allocation Requests** वरून **Review** दाबून प्लॅन उघडा. मंजूर करण्याआधी तुम्ही तो दुरुस्त करू शकता. तुम्ही मंजूर करताच JMDO टास्क सुरू करू शकतात.

1. **Dealer Tasks** मध्ये प्रत्येक डीलर तपासा. **Date** (ती प्लॅन केलेल्या आठवड्यातच असली पाहिजे) किंवा **Priority** (**Normal** किंवा **High Priority**) बदला. डस्टबिनवर टॅप करून **Remove** दाबल्यावर टास्क निघतो.
2. डीलर जोडायचे असतील तर **Add dealer task(s)** खाली ते निवडा, **Date** आणि **Priority** निवडा, आणि **Add** दाबा.
3. **SPCM Target**, **Soil Sample Target** आणि **PoS Liquidation Targets** (**Urea**, **DAP**, **NPKS**, **Other**) तपासा. बॉक्समधून बाहेर आल्यावर बदल सेव्ह होतो.
4. **MD Programs** फक्त तपासण्यासाठी दाखवले आहेत. ते CSR1 मधून येतात आणि इथे बदलता येत नाहीत.
5. खालची सारांशाची ओळ वाचा, **Approve Allocation** दाबा, मग **Yes, Approve** दाबा.

> Careful: डीलर टास्क काढल्यावर तो परत आणता येत नाही.

## नेहमीच्या अडचणी

- **"The date must fall within the JMDO's allocated week"** (तारीख JMDO च्या प्लॅन केलेल्या आठवड्यातच असावी) **Dealer Tasks** खाली दाखवलेल्या आठवड्यातली तारीख निवडा.
- **"Already assigned on that date"** (त्या तारखेला आधीच दिलेला आहे) तो डीलर त्या दिवसासाठी आधीच प्लॅनमध्ये आहे.
- **"Couldn't load this allocation."** (हा प्लॅन उघडता आला नाही) **Back to all requests** ने परत जा आणि प्लॅन परत उघडा.
:::

::: section task-monitoring
title: माझ्या टीमच्या प्रत्येक टास्कवर लक्ष ठेवा
routes: /TaskMonitoring
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-tm-filters] | 2 > [data-help=ff-tm-apply] | 3 > [data-help=ff-tm-search] | 4 > [data-help=ff-tm-refresh]
minutes: 2
source-hash: 0c1b0fa3ecdc07f2
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Task Monitoring** वर तुमच्या JMDO चा प्रत्येक टास्क दिसतो: तो कोणाकडे आहे, त्याचा प्रकार, कुठे, कधीपर्यंत करायचा आणि त्याची स्थिती. ही स्क्रीन आज करायच्या टास्कवर उघडते.

1. **Filters** खाली एक किंवा जास्त **JMDO**, **Task Type**, **Status**, **State** किंवा **Region** निवडा, आणि **Date Range** ठरवा.
2. जुळणारे टास्क पाहण्यासाठी **Apply Filters** दाबा. **Clear All** ने तारखांसह सगळे फिल्टर निघतात.
3. **Tasks** टेबलमध्ये लिस्ट कमी करण्यासाठी **Search...** मध्ये टाइप करा. **Requested** टास्कवर डोळ्याचा आयकॉन असतो, तो मंजुरीसाठी त्याचा आठवड्याचा प्लॅन उघडतो.
4. नवीन स्थिती पाहण्यासाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **"No tasks match these filters".** (या फिल्टरनुसार एकही टास्क नाही) सगळ्या तारखांचे टास्क पाहण्यासाठी **Clear All** दाबा.
- **"No tasks yet".** (अजून एकही टास्क नाही) तुमच्या HQ वरच्या एकाही JMDO ने आठवड्याचा प्लॅन पाठवलेला नाही.
:::

::: section team-tracking
title: माझी टीम कुठे आहे ते पाहा
routes: /TeamTracking
for: 
roles: 
designations: 
minutes: 1
source-hash: 158915a8c6a5812a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Team Tracking** वर तुमच्या HQ वरच्या JMDO च्या Live Tracking ची स्थिती दिसते.

1. **JMDO tracking status** टेबलमध्ये प्रत्येक JMDO चा **Status** पाहा: **Live**, **Stale**, **Offline**, **Stopped**, **Ended automatically** किंवा **Never started**.
2. **Last Seen** आणि **Last Location** मध्ये शेवटचे लोकेशन कधी आणि कुठून आले ते दिसते.
3. **Session Started** मध्ये JMDO ने ट्रॅकिंग कधी सुरू केले ते दिसते.
4. JMDO शोधण्यासाठी **Search...** मध्ये टाइप करा, आणि नवीन स्थितीसाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **एखाद्या JMDO साठी "Never started" दिसते.** त्या JMDO ने ॲपमध्ये Live Tracking सुरू केलेले नाही.
- **Stale किंवा Offline.** फोनने काही वेळ लोकेशन पाठवलेले नाही. JMDO ला फोन करा.
:::

::: section yearly-targets
title: JMDO चे वर्षाचे टार्गेट ठरवा
routes: /TargetAchievement
for: 
roles: 
designations: 
minutes: 2
source-hash: e1cdc1ea7b68c98e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Yearly Target Allocation** वर MDO प्रत्येक JMDO चे आर्थिक वर्षाचे (एप्रिल ते मार्च) SPCM आणि Soil Sample टार्गेट ठरवतात.

1. **Allocate target to a JMDO** खाली **JMDO** निवडा. आधी सेव्ह केलेले टार्गेट आपोआप भरले जाते.
2. **SPCM Target (yearly)** आणि **Soil Sample Target (yearly)** टाका.
3. **Save Target Allocation** दाबा.
4. **Current yearly targets** टेबलमध्ये प्रत्येक JMDO चे टार्गेट दिसते. एखादी ओळ फॉर्ममध्ये आणून बदलण्यासाठी पेन्सिल दाबा.

| फील्ड | काय भरायचे |
|---|---|
| **JMDO** | फील्ड अधिकारी. भरणे आवश्यक. |
| **SPCM Target (yearly)** | पूर्ण संख्या, शून्य किंवा जास्त. भरणे आवश्यक. |
| **Soil Sample Target (yearly)** | पूर्ण संख्या, शून्य किंवा जास्त. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"Enter valid, non-negative SPCM and Soil Sample targets."** (SPCM आणि Soil Sample चे योग्य, उणे नसलेले टार्गेट टाका) पूर्ण संख्या वापरा, शून्य किंवा जास्त.
- **Save बटण राखाडी आहे.** JMDO निवडा आणि दोन्ही टार्गेट भरा.
:::

::: section team-reports
title: टीमचे रिपोर्ट
routes: /MdoReports
for: 
roles: 
designations: 
minutes: 2
source-hash: 4f488c0a407fcae5
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Reports** वर या महिन्यात तुमचे JMDO कसे काम करत आहेत ते दिसते: बंद झालेले टास्क, लागलेला वेळ, माती नमुने आणि शेतकरी.

1. **Report Filters** खाली **Report Type**, **JMDO** आणि **On or after** (सुरुवातीची तारीख) निवडा.
2. **Generate Report** दाबा.
3. कार्डे पाहा: **Tasks Closed**, **Avg. Turnaround**, **Soil Samples** आणि **Farmers Covered**.
4. **JMDO-wise summary** मध्ये प्रत्येक JMDO चे **Tasks Closed**, **On-time %**, **Soil Samples** आणि **Farmers Added** दिसतात.
5. **Detailed Task Report** मध्ये प्रत्येक टास्क **Resolution**, **Status** आणि **Hours Taken** सह दिसतो.
6. टेबलवर **Copy**, **CSV**, **Excel**, **PDF** किंवा **Print** वापरा, आणि परत लोड करण्यासाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **Sales Report मध्ये काहीच दिसत नाही.** विक्रीचा डेटा अजून टास्क सिस्टिममध्ये नाही.
- **Farmer Coverage साठी तपशीलाचे टेबल नाही.** शेतकऱ्यांचे आकडे फक्त कार्डांवर आणि सारांशात दिसतात.
:::

::: section performance-tracker
title: JMDO च्या कामगिरीची तुलना करा
routes: /PerformanceTracker
for: 
roles: 
designations: 
minutes: 1
source-hash: 203ffe19ee1cb6d6
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Performance Tracker** वर JMDO ची सगळ्यात चांगल्यापासून सगळ्यात कमी कामगिरीपर्यंत क्रमवारी दिसते. सध्या इथे नमुना आकडे आहेत, खरा डेटा नाही.

1. **Filters** उघडा आणि **JMDO** निवडा.
2. **Apply Filters** दाबा. **Clear All** दाबल्यावर परत सगळे JMDO दिसतात.
3. प्रत्येक JMDO चे **Rank**, **SPCM Achv.**, **Soil Achv.** आणि **Attendance** वाचा.

## नेहमीच्या अडचणी

- **"Detailed performance view ... isn't built yet."** (कामगिरीचा तपशील अजून तयार नाही) डोळ्याच्या आयकॉनसाठी अजून तपशीलाचे पेज नाही.
:::

::: section farmer-data
title: शेतकऱ्यांची माहिती
routes: /FarmerData
for: 
roles: 
designations: 
minutes: 1
source-hash: 78a4629a8079544f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Farmer Data** वर तुमच्या भागातल्या शेतकऱ्यांची लिस्ट आणि नवीन शेतकरी जोडण्याचा फॉर्म आहे. सध्या ही स्क्रीन फक्त नमुना आहे: नवीन नोंदी सर्व्हरवर सेव्ह होत नाहीत.

1. शेतकरी **Contact**, **Location**, **Crop(s)** आणि **Acres** सह पाहा.
2. **Add Farmer Entry** खाली **MO H.Q.**, **Month** आणि **Programme** निवडा.
3. **Farmer Name**, **Contact Number**, **Location**, **Contact Address**, **Acres**, **Crop 1** आणि **Crop 2** टाका.
4. **Save farmer entry** दाबा. ओळीवरची पेन्सिल ती ओळ फॉर्ममध्ये कॉपी करते.

> Tip: माती नमुन्यासाठी शेतकऱ्याची नोंद करायची असेल तर टास्कवरचे **Add Farmer Data** वापरा. त्याने नमुना गोळा करण्याची स्क्रीन उघडते.

## नेहमीच्या अडचणी

- **Save बटण राखाडी आहे.** **Farmer Name** भरणे आवश्यक आहे.
- **पेज सोडल्यावर माझी नोंद गायब झाली.** ही स्क्रीन अजून सर्व्हरवर सेव्ह करत नाही.
:::

::: section farmers-hub
title: शेतकरी हब
routes: /Farmers
for: 
roles: 
designations: 
minutes: 1
source-hash: 6be3ac6e9f35012a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Farmers** वर फील्ड अधिकारी शेतकऱ्यासमोर वापरतात ती सगळी साधने एकत्र आहेत. एका सूचनेत लिहिले आहे की **Farmer Portal is on its way** (Farmer Portal लवकरच येत आहे).

1. येणाऱ्या Farmer Portal बद्दलची सूचना वाचा.
2. **Help a farmer now** खाली, प्रॉडक्ट, डोस आणि वापर याबद्दल उत्तरे मिळवण्यासाठी **Ask SPIC AI** टॅप करा.
3. शेतकरी आणि SPIC तज्ज्ञांचे प्रश्न आणि उत्तरे पाहण्यासाठी **Knowledge Community** टॅप करा.
4. प्रॉडक्टची माहिती दाखवण्यासाठी **Product Information**, **Product Brochures** किंवा **AI Videos** टॅप करा.

## नेहमीच्या अडचणी

- **Knowledge Community दिसत नाही.** तुमच्या रोलला community वापरण्याची परवानगी नाही.
:::

::: section rmd-dashboard
title: RMD डॅशबोर्ड
routes: /RMDDashboard
for: 
roles: 
designations: 
minutes: 1
source-hash: 4698a8dc0e852e00
confidence: ok

**ही स्क्रीन कशासाठी आहे**
RMD डॅशबोर्डवर रिजनसाठी CSR आणि बजेटचा आढावा दिसतो. सध्या इथले आकडे नमुना आहेत, खरा डेटा नाही, आणि इथली बटणे काहीही सेव्ह करत नाहीत.

1. वर **Budget Overview** आणि **CSR Workflow Summary** पाहा.
2. **RM Approval Status** आणि **Budget Utilization** सारखे चार्ट पाहा.
3. **Quick Actions**, **Recent Programs & Reports**, **Recent Activity** आणि **System Updates** पाहा.

## नेहमीच्या अडचणी

- **आकडे माझ्या रिजनशी जुळत नाहीत.** या स्क्रीनवरचे आकडे सध्या नमुना आहेत.
:::
