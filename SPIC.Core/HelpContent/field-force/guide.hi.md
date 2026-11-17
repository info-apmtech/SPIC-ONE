---
module: field-force
title: फ़ील्ड फ़ोर्स
icon: bi-person-walking
order: 107
language: hi
category: फ़ील्ड का काम
note: नई गाइड
meta-source-hash: c6011d305dce737c
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: d557872762e3ea47
confidence: ok

हर हफ़्ते के फ़ील्ड के काम का प्लान बनाएँ, उसकी मंज़ूरी लें, हाज़िरी लगाएँ और फ़ील्ड में टास्क पूरे करें। मैनेजर देखते हैं कि कौन काम कर रहा है, वे कहाँ हैं और हर कोई टारगेट के मुकाबले कितना आगे है।

- plan | icon: bi-calendar-week | color: #ea580c
  title: JMDO हफ़्ते का प्लान बनाता है
  text: हर दिन के लिए डीलर चुनें, SPCM, Soil Sample और PoS liquidation के टारगेट तय करें, और प्लान भेजें।
- approve | icon: bi-person-check | color: #2563eb
  title: MDO उसे मंज़ूर करता है
  text: MDO प्लान जाँचता है, ज़रूरत हो तो डीलर, तारीख़ें या टारगेट बदलता है, और उसे मंज़ूर करता है।
- work | icon: bi-geo-alt | color: #16a34a
  title: फ़ील्ड में काम, और उस पर सीधी नज़र
  text: JMDO हाज़िरी लगाता है, हर टास्क शुरू और बंद करता है। मैनेजर टास्क, लोकेशन और रिपोर्ट पर नज़र रखते हैं।
:::

::: section flow
title: फ़ील्ड के काम का एक हफ़्ता कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2462b99188302817
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- jmdo | type: lane | color: #ea580c | roles: JMDO
  title: JMDO
  sub: फ़ील्ड स्टाफ़
- mdo | type: lane | color: #2563eb | roles: MDO, MO
  title: MDO
  sub: आपके HQ का टीम मैनेजर
- plan | lane: jmdo | col: 1 | icon: bi-calendar-week | color: #d97706 | next: approve | screen: plan-week
  title: हफ़्ते का प्लान बनाएँ
  status: Pending
  text: हर दिन के लिए डीलर चुनें, हफ़्ते के टारगेट भरें और **Request** दबाएँ। प्लान MDO का इंतज़ार करता है।
- approve | lane: mdo | col: 2 | icon: bi-person-check | color: #2563eb | next: attend | screen: review-allocation
  title: जाँचें और मंज़ूर करें
  status: Approved
  text: MDO डीलर टास्क जोड़ या हटा सकता है, तारीख़, प्राथमिकता और टारगेट बदल सकता है, फिर **Approve Allocation** दबाता है।
- attend | lane: jmdo | col: 3 | icon: bi-calendar-check | color: #0d9488 | next: work | screen: mark-attendance
  title: हाज़िरी लगाएँ
  status: Present
  text: काम के हर दिन, कोई भी टास्क शुरू करने से पहले SPIC ONE मोबाइल ऐप में हाज़िरी लगाएँ।
- work | lane: jmdo | col: 4 | icon: bi-play-circle | color: #7c3aed | next: close | screen: todays-tasks
  title: टास्क शुरू करें
  status: In Progress
  text: **Today's Allocation** पर **Start** दबाएँ, फिर **Start Task** दबाएँ। समय और लोकेशन दर्ज हो जाते हैं।
- close | lane: jmdo | col: 5 | icon: bi-check-circle | color: #16a34a | next: monitor | screen: todays-tasks
  title: सेव करें और बंद करें
  status: Completed
  text: **Close-out form** भरें, **Save Record** दबाएँ, फिर टास्क बंद करने के लिए **Stop Task** दबाएँ।
- monitor | lane: mdo | col: 6 | icon: bi-list-check | color: #475569 | screen: task-monitoring
  title: नज़र रखें
  status: Completed
  text: MDO हर टास्क **Task Monitoring** पर, टीम की लोकेशन **Team Tracking** पर, और प्रगति **Reports** पर देखता है।
- no-attendance | lane: jmdo | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: work | to: attend | screen: mark-attendance
  title: हाज़िरी नहीं लगी
  status: Yet to Start
  text: आज की हाज़िरी लगाए बिना टास्क शुरू नहीं हो सकता। ऐप आपके लिए **Mark Attendance** खोल देता है।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 80c93f090c1c74d0
confidence: ok

- pending | color: #d97706
  label: Pending
  text: हफ़्ते का प्लान भेज दिया गया है और MDO की मंज़ूरी का इंतज़ार है। JMDO को "awaiting MDO approval" दिखता है।
- approved | color: #16a34a
  label: Approved
  text: MDO ने हफ़्ते का प्लान मंज़ूर कर दिया। इसके टास्क अपने दिन पर शुरू किए जा सकते हैं।
- rejected | color: #dc2626
  label: Rejected
  text: हफ़्ते का प्लान मंज़ूर नहीं हुआ। प्लान ठीक करें और फिर से भेजें।
- requested | color: #d97706
  label: Requested
  text: Task Monitoring पर, ऐसा टास्क जिसका हफ़्ते का प्लान अभी मंज़ूरी का इंतज़ार कर रहा है।
- yet-to-start | color: #64748b
  label: Yet to Start
  text: टास्क मंज़ूर है पर अभी शुरू नहीं हुआ।
- not-started | color: #64748b
  label: Not Started
  text: Reports पर, यह Yet to Start जैसा ही है।
- in-progress | color: #2563eb
  label: In Progress
  text: JMDO ने Start Task दबाया है और उस पर काम कर रहा है।
- completed | color: #16a34a
  label: Completed
  text: टास्क बंद हो गया है।
- fully | color: #16a34a
  label: Fully Completed
  text: Close-out का नतीजा: सारा काम हो गया।
- partially | color: #d97706
  label: Partially Completed
  text: Close-out का नतीजा: काम का सिर्फ़ कुछ हिस्सा हुआ।
- not-completed | color: #ea580c
  label: Not Completed
  text: Close-out का नतीजा: काम नहीं हो पाया।
- present | color: #16a34a
  label: Present
  text: पूरे दिन की हाज़िरी। इससे आप On Duty हो जाते हैं।
- half-day | color: #0d9488
  label: Half Day
  text: आधे दिन की हाज़िरी। इससे आप On Duty हो जाते हैं।
- on-leave | color: #7c3aed
  label: On Leave
  text: आज आप छुट्टी पर हैं। आप Off Duty हैं।
- absent | color: #dc2626
  label: Absent
  text: गैरहाज़िर लगाए गए दिन के लिए हफ़्ते के सारांश में दिखता है।
- active | color: #16a34a
  label: Active
  text: टीम डैशबोर्ड पर, ऐसा JMDO जो आज छुट्टी पर नहीं है।
- on-duty | color: #16a34a
  label: On Duty
  text: आज हाज़िरी Present या Half Day लगी है।
- off-duty | color: #64748b
  label: Off Duty
  text: आज अभी हाज़िरी नहीं लगी, या On Leave है।
- live | color: #16a34a
  label: Live
  text: Live Tracking चालू है और लोकेशन अभी आ रही है।
- stale | color: #d97706
  label: Stale
  text: Live Tracking चालू है, पर पिछले कुछ मिनटों में नई लोकेशन नहीं आई।
- offline | color: #dc2626
  label: Offline
  text: Live Tracking चालू है, पर 15 मिनट से ज़्यादा समय से लोकेशन नहीं आई।
- auto-ended | color: #475569
  label: Ended automatically
  text: ट्रैकिंग काफ़ी देर से चुप थी, इसलिए ऐप ने उसे बंद कर दिया।
- stopped | color: #64748b
  label: Stopped
  text: JMDO ने Stop Tracking दबाया।
- never | color: #94a3b8
  label: Never started
  text: JMDO ने Live Tracking शुरू नहीं किया है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: ca8f05faf80da07f
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- jmdo | color: #ea580c | roles: JMDO
  title: JMDO (फ़ील्ड स्टाफ़)
  - हफ़्ते का प्लान बनाना और Request दबाना
  - काम के हर दिन हाज़िरी लगाना
  - हर टास्क शुरू करना, सेव करना और बंद करना
  - Live Tracking शुरू और बंद करना
  - My Reports देखना
- mdo | color: #2563eb | roles: MDO, MO
  title: MDO
  - अपने JMDO के हफ़्ते के प्लान मंज़ूर करना
  - हर JMDO के साल के टारगेट तय करना
  - टास्क, टीम की लोकेशन और रिपोर्ट पर नज़र रखना
- admin | color: #475569 | roles: Admin
  title: Admin
  - हर HQ के प्लान देखना
  - मंज़ूरी के बाद भी प्लान ठीक करना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e1bd21cb8311eb35
confidence: ok

- cannot-start | popular: true
  q: मैं अपना टास्क शुरू नहीं कर पा रहा
  a: दो बातें ज़रूरी हैं। आपका हफ़्ते का प्लान MDO ने **Approved** किया हो, और आपने आज की हाज़िरी लगाई हो। अगर हाज़िरी नहीं लगी है, तो ऐप आपके लिए **Mark Attendance** खोल देता है।
- web-attendance | popular: true
  q: Mark Attendance पर लिखा आता है कि हाज़िरी सिर्फ़ मोबाइल ऐप से लग सकती है
  a: हाज़िरी, Live Tracking और Track Playback सिर्फ़ आपके फ़ोन के SPIC ONE ऐप में चलते हैं। फ़ोन पर ऐप खोलें और फिर से कोशिश करें।
- already-sent
  q: "You've already submitted an allocation for that week." (उस हफ़्ते का प्लान आप पहले ही भेज चुके हैं) दिख रहा है
  a: एक हफ़्ते के लिए सिर्फ़ एक ही प्लान भेजा जा सकता है। डीलर जोड़ने या हटाने, या तारीख़ और टारगेट बदलने के लिए अपने MDO से **Allocation Requests** स्क्रीन पर कहें।
- spcm-not-closed
  q: टास्क बंद करते समय "SPCM target not yet reached" (SPCM टारगेट अभी पूरा नहीं हुआ) दिखता है
  a: SPCM और PoS liquidation के टास्क हफ़्ते का टारगेट पूरा होने पर ही बंद होते हैं। दूसरे दिनों में रिकॉर्ड सेव करते रहें। टारगेट पूरा होते ही टास्क अपने-आप बंद हो जाता है।
- no-jmdos
  q: मुझे "No JMDOs found for your headquarters" दिख रहा है
  a: आपके ही HQ वाले JMDO आपकी टीम होते हैं। अगर कोई नहीं दिख रहा, तो admin से उसकी प्रोफ़ाइल पर HQ जाँचने को कहें।
- offline
  q: Team Tracking पर किसी JMDO के लिए Stale या Offline दिख रहा है
  a: JMDO के फ़ोन ने कुछ समय से लोकेशन नहीं भेजी है। हो सकता है नेटवर्क न हो या ऐप बंद हो। JMDO को फ़ोन करें और ऐप खोलने को कहें।
:::

::: section field-dashboard
title: मेरा फ़ील्ड डैशबोर्ड
routes: /FieldDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: ad0c345e7985b577
confidence: ok

**यह स्क्रीन किस काम की है**
**My Dashboard** JMDO की होम स्क्रीन है। इसमें दिखता है कि आप ड्यूटी पर हैं या नहीं, आज आपके कितने टास्क हैं, Live Tracking, और वे टास्क जो आपने अभी पूरे नहीं किए।

1. ऊपर अपना नाम और **On Duty** या **Off Duty** देखें। अगर हाज़िरी नहीं लगाई है, तो **Mark Attendance** दबाएँ।
2. कार्ड देखें: **Today's Tasks**, **Yet to Start**, **In Progress** और **Completed**।
3. Live Tracking चालू करने के लिए **Start Live Tracking** दबाएँ। **Background location access** वाला मैसेज पढ़ें और **Continue** दबाएँ।
4. ट्रैकिंग चालू रहते हुए अपना रास्ता देखने के लिए **View Route** दबाएँ, या उसे बंद करने के लिए **Stop Tracking** और फिर दोबारा **Stop Tracking** दबाएँ। **Keep Tracking** दबाने पर ट्रैकिंग बंद नहीं होती।
5. **Continue where you left off** में, चल रहे टास्क पर वापस जाने के लिए **view** दबाएँ। **View All** से **Tasks Allocation** खुलता है।

> Tip: Live Tracking तभी शुरू होती है जब आप बटन दबाते हैं। यह हाज़िरी या टास्क के साथ अपने-आप शुरू नहीं होती।

## आम दिक्कतें

- **"Mark today's attendance first to enable Start Live Tracking."** (Start Live Tracking के लिए पहले आज की हाज़िरी लगाएँ) हाज़िरी लगाएँ, फिर वापस आएँ।
- **"Mobile app only".** (सिर्फ़ मोबाइल ऐप) Live Tracking सिर्फ़ आपके फ़ोन के SPIC ONE ऐप से ही शुरू की जा सकती है।
- **"Nothing in progress right now".** (अभी कुछ भी चालू नहीं है) आपने कोई टास्क शुरू नहीं किया है। टास्क शुरू करने के लिए **Tasks Allocation** खोलें।
:::

::: section plan-week
title: अपने हफ़्ते के टास्क का प्लान बनाएँ और रिक्वेस्ट भेजें
routes: /TasksAllocation
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-week] | 2 > [data-help=ff-steps] | 3 > [data-help=ff-days] | 4 > [data-help=ff-filters] | 5 > [data-help=ff-dealers] | 6 > [data-help=ff-next]
minutes: 4
source-hash: 3effab3c5a7b4e53
confidence: ok

**यह स्क्रीन किस काम की है**
**Tasks Allocation** के **All Allocations** टैब पर JMDO पाँच स्टेप में एक हफ़्ते (रविवार से शनिवार) का प्लान बनाता है और मंज़ूरी के लिए MDO को भेजता है।

1. जिस हफ़्ते का प्लान बनाना है, उसे चुनने के लिए **Previous Week** और **Next Week** का इस्तेमाल करें।
2. बाईं तरफ़ के स्टेप हैं **Dealer**, **SPCM**, **Soil Sample**, **MD Program** और **PoS liquidation**। स्टेप खोलने के लिए उस पर टैप करें।
3. **Dealer** स्टेप में, जिस दिन का प्लान बना रहे हैं उसे चुनने के लिए दिन वाले टैब पर टैप करें।
4. सर्च बॉक्स (**Search dealer name or code...**) या **All Districts**, **All Sub Districts** और **All Dealers** लिस्ट से डीलर ढूँढें।
5. उस दिन के लिए डीलर चुनने को डीलर कार्ड पर टैप करें। हटाने के लिए उस पर फिर से टैप करें। दिन वाले टैब पर दिखता है कि आपने कितने डीलर चुने।
6. **Next: SPCM Target** दबाएँ। SPCM और Soil Sample के लिए **This Week's Target** भरें, MD प्रोग्राम जाँचें, PoS liquidation की मात्रा भरें (**Urea**, **DAP**, **NPKS**, **Others**), फिर **Request** दबाएँ।

> Tip: CSR1 में आपको दिए गए MD प्रोग्राम अपने-आप जुड़ जाते हैं। आप उन्हें यहाँ जोड़ या हटा नहीं सकते।
> Careful: एक हफ़्ते के लिए सिर्फ़ एक ही प्लान भेजा जा सकता है। **Request** के बाद सिर्फ़ MDO ही उसे बदल सकता है।

| फ़ील्ड | क्या भरें |
|---|---|
| **This Week's Target** (SPCM) | इस हफ़्ते आप कितने SPCM पूरे करने का प्लान बना रहे हैं। |
| **This Week's Target** (Soil Sample) | इस हफ़्ते आप मिट्टी के कितने सैंपल लेने का प्लान बना रहे हैं। |
| **Urea**, **DAP**, **NPKS**, **Others** | इस हफ़्ते हर प्रोडक्ट के लिए प्लान की गई PoS liquidation की मात्रा। |

## आम दिक्कतें

- **"Fill in at least one step before submitting."** (भेजने से पहले कम से कम एक स्टेप भरें) कम से कम एक डीलर चुनें या कम से कम एक टारगेट भरें।
- **"You've already submitted an allocation for that week."** (उस हफ़्ते का प्लान आप पहले ही भेज चुके हैं) प्लान बदलवाने के लिए अपने MDO से **Allocation Requests** पर कहें।
- **"Allocation submitted — awaiting MDO approval".** (प्लान भेज दिया गया, MDO की मंज़ूरी का इंतज़ार) प्लान भेज दिया गया है। MDO के मंज़ूर करने के बाद टास्क **Today's Allocation** पर खुलते हैं।
- **"Allocation Rejected".** (प्लान रिजेक्ट हुआ) **Update & Resubmit** दबाएँ, प्लान ठीक करें और फिर से **Request** दबाएँ।
:::

::: section mark-attendance
title: आज की हाज़िरी लगाएँ
routes: /MarkAttendance
for: 
roles: 
designations: 
minutes: 2
source-hash: 8b14e6f020b3c82e
confidence: ok

**यह स्क्रीन किस काम की है**
काम के हर दिन कोई भी टास्क शुरू करने से पहले अपनी हाज़िरी लगाएँ। यह सिर्फ़ आपके फ़ोन के SPIC ONE ऐप में ही चलता है, क्योंकि इसमें आप कहाँ हैं यह दर्ज होता है।

1. मेन्यू से **Mark Attendance** खोलें, या **My Dashboard** पर **Mark Attendance** दबाएँ।
2. ऐप को अपनी लोकेशन इस्तेमाल करने दें। **Visit Area / Place** आपकी लोकेशन से भर जाता है। ज़रूरत हो तो इसे ठीक करें, या लोकेशन न मिले तो इसे टाइप करें।
3. **Date** जाँच लें।
4. **Attendance Status** चुनें: **Present**, **Half Day** या **On Leave**।
5. **Update Attendance** दबाएँ। आपको "Attendance updated." (हाज़िरी अपडेट हो गई) दिखता है और आप **My Dashboard** पर वापस पहुँच जाते हैं।
6. उसी दिन बाद में बदलना हो, तो **Attendance marked today** के पास **Edit** दबाएँ।
7. दाईं तरफ़ **Current Status** (**On Duty** या **Off Duty**) और **Weekly Summary** देखें: हर दिन की हाज़िरी, काम के घंटे, शुरू किए गए टास्क और दूरी।

> Careful: सुबह 10:00 बजे के बाद स्क्रीन पर "Submitted late for today" (आज देर से लगाई) दिखता है। आप फिर भी इसे सेव कर सकते हैं।

| फ़ील्ड | क्या भरें |
|---|---|
| **Date** | आज की तारीख़। ज़रूरी है। |
| **Visit Area / Place** | वह गाँव या शहर जहाँ आप काम कर रहे हैं। लोकेशन न मिलने पर ज़रूरी है। |
| **Attendance Status** | **Present**, **Half Day** या **On Leave**। |

## आम दिक्कतें

- **"Attendance can only be marked from the SPIC mobile app".** (हाज़िरी सिर्फ़ SPIC मोबाइल ऐप से लग सकती है) अपने फ़ोन पर SPIC ONE खोलें।
- **"Location could not be captured. Please type your Visit Area / Place to save attendance."** (लोकेशन नहीं मिली, हाज़िरी सेव करने के लिए जगह टाइप करें) लोकेशन चालू करें, या जगह ख़ुद टाइप करें।
- **"Date is required."** (तारीख़ ज़रूरी है) **Update Attendance** दबाने से पहले तारीख़ चुनें।
:::

::: section todays-tasks
title: आज के टास्क शुरू करें, सेव करें और बंद करें
routes: /TaskDetail
for: 
roles: 
designations: 
minutes: 4
source-hash: 163e059a3e7fb61f
confidence: ok

**यह स्क्रीन किस काम की है**
हर मंज़ूर टास्क अपनी अलग स्क्रीन पर खुलता है। आप उसे शुरू करते हैं, जो किया उसे दर्ज करते हैं, और उसे बंद करते हैं। ऐप टास्क पर लगा समय और आपने उसे कहाँ शुरू किया, यह दर्ज करता है।

1. **Tasks Allocation** पर **Today's Allocation** खोलें। **Yet to Start**, **In Progress**, **Completed** या **All** चुनें, फिर टास्क कार्ड पर **Start** (या **Continue**) दबाएँ।
2. **Task Timeline** (**Assigned**, **Requested**, **Approved**, **Started**, **Closed**) और टास्क की जानकारी जाँचें।
3. **Start Task** दबाएँ। ऐप आपकी लोकेशन लेता है और **Time on task** घड़ी शुरू करता है।
4. **Close-out form** में **Outcome** चुनें। डीलर के लिए **+ Add another Product** दबाएँ और **Product Name** और **Stock** भरें। SPCM के लिए **Achieved Target** भरें। PoS liquidation के लिए **Urea**, **NPKS**, **DAP** और **Other** भरें।
5. **Remarks** लिखें, **Photo Proof** (डीलर) या **Supporting Document** जोड़ें, और **Save Record** दबाएँ। सेव किए गए रिकॉर्ड दाईं तरफ़ की टेबल में दिखते हैं।
6. विज़िट में ज़रूरत हो तो **Quick actions** का इस्तेमाल करें: **Soil Sample Collection** या **Add Farmer Data**।
7. काम पूरा होने पर **Stop Task** दबाएँ, फिर **Close Task** दबाएँ।

> Careful: बंद किया गया टास्क फिर से शुरू नहीं किया जा सकता।
> Tip: हफ़्ते का टारगेट पूरा होने पर SPCM और PoS liquidation के टास्क अपने-आप बंद हो जाते हैं ("Record saved - weekly target reached, task completed!")।

## आम दिक्कतें

- **"Allocation Awaiting MDO Approval".** (प्लान MDO की मंज़ूरी का इंतज़ार कर रहा है) जब तक MDO आपका हफ़्ते का प्लान मंज़ूर नहीं करता, **Start Task** बटन ग्रे रहता है।
- **"Mark today's attendance before starting a task."** (टास्क शुरू करने से पहले आज की हाज़िरी लगाएँ) ऐप **Mark Attendance** खोलता है। हाज़िरी लगाएँ, फिर टास्क शुरू करें।
- **"Please start the task before saving records."** (रिकॉर्ड सेव करने से पहले टास्क शुरू करें) पहले **Start Task** दबाएँ।
- **Soil Sample वाला कार्ड Sample Collection खोलता है।** यह सामान्य है। मिट्टी के सैंपल सैंपल कलेक्शन वाली स्क्रीन पर दर्ज होते हैं।
:::

::: section task-history
title: अपने टास्क का इतिहास और हफ़्ते का सारांश देखें
routes: /TasksAllocation
for: 
roles: 
designations: 
minutes: 2
source-hash: 5bb6d9cb3b6d9f29
confidence: ok

**यह स्क्रीन किस काम की है**
**Tasks Allocation** के **Task History** और **Task Summary** टैब पर हर हफ़्ते के आपके टास्क दिखते हैं, और यह भी कि हर हफ़्ते के टारगेट का कितना काम हुआ।

1. **Tasks Allocation** पर **Task History** खोलें। यह आज के टास्क पर खुलता है।
2. **From**, **To**, **Dealer** और **Status** चुनें, फिर **Search** दबाएँ। **Today** दबाने से आप आज पर वापस आ जाते हैं।
3. हर कार्ड पर टास्क और उसका स्टेटस दिखता है। उसे खोलने के लिए **View Details** या **Continue** दबाएँ।
4. **Task Summary** खोलें। कार्ड पर **Total Assigned**, **Yet to Start**, **In Progress** और **Completed** दिखते हैं।
5. हर हफ़्ते का प्लान स्टेटस, **Complete** प्रतिशत, **Dealer Visits**, **MD Programs** और हर टारगेट का **Target**, **Achieved** और **Pending** दिखता है।
6. **Day-wise Dealer Visits** में देखें कि हर दिन कितनी डीलर विज़िट हुईं और कितनी बाकी हैं।

## आम दिक्कतें

- **"No tasks match this filter".** (इस फ़िल्टर से कोई टास्क नहीं मिला) तारीख़ों की रेंज बढ़ाएँ या **Status** को **All** करें।
- **"Detailed view for this task type isn't built yet."** (इस तरह के टास्क की पूरी जानकारी वाली स्क्रीन अभी नहीं बनी) Soil Sample टास्क के लिए यहाँ जानकारी वाली स्क्रीन नहीं है।
:::

::: section my-reports
title: मेरी रिपोर्ट
routes: /FieldReports
for: 
roles: 
designations: 
minutes: 1
source-hash: 1d05a6505b4982e6
confidence: ok

**यह स्क्रीन किस काम की है**
**My Reports** पर दिखता है कि इस महीने आपने कितने टास्क बंद किए और कितनी जल्दी।

1. कार्ड देखें: **Completed**, **Avg. Close Time**, **Soil Samples** और **Farmers Added**।
2. **Task closures** टेबल में हर बंद टास्क **Date**, **Task**, **Type** और **Outcome** के साथ दिखता है।
3. टास्क ढूँढने के लिए **Search...** में टाइप करें।
4. लिस्ट सेव या प्रिंट करने के लिए **Copy**, **CSV**, **Excel**, **PDF** या **Print** का इस्तेमाल करें।

## आम दिक्कतें

- **"Nothing closed out this month yet".** (इस महीने अभी कुछ बंद नहीं हुआ) आपने इस महीने कोई टास्क बंद नहीं किया है।
:::

::: section track-playback
title: अपनी ट्रैकिंग का रास्ता फिर से देखें
routes: /TrackPlayback
for: 
roles: 
designations: 
minutes: 2
source-hash: cd8704c91e7b24e5
confidence: ok

**यह स्क्रीन किस काम की है**
**Track Playback** पर पिछले 7 दिनों की आपकी Live Tracking का रास्ता नक्शे पर दिखता है। यह सिर्फ़ आपके फ़ोन के SPIC ONE ऐप में ही चलता है।

1. मेन्यू से **Track Playback** खोलें, या **My Dashboard** पर **View Route** दबाएँ, या **Mark Attendance** पर **View Tracking Details** दबाएँ।
2. अगर ट्रैकिंग चालू है, तो **Tracking Active** कार्ड पर समय दिखता है। उसे बंद करने के लिए **Stop Tracking** दबाएँ, या रास्ता देखने के लिए **Directions to live location** दबाएँ।
3. **Other playbacks available** में, किसी दिन का रास्ता देखने के लिए उस दिन पर टैप करें।
4. नक्शे पर play दबाएँ, रास्ते पर आगे-पीछे जाने के लिए स्लाइडर खींचें, और स्पीड चुनें (**1x**, **2x** या **4x**)।

## आम दिक्कतें

- **"No tracking sessions in the last 7 days".** (पिछले 7 दिनों में कोई ट्रैकिंग नहीं) रास्ता दर्ज करने के लिए **My Dashboard** से Live Tracking शुरू करें।
- **"Track Playback can only be used from the SPIC mobile app".** (Track Playback सिर्फ़ SPIC मोबाइल ऐप से चलता है) अपने फ़ोन पर SPIC ONE खोलें।
:::

::: section team-dashboard
title: टीम डैशबोर्ड
routes: /MdoDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: f544adf24e2f0b9c
confidence: ok

**यह स्क्रीन किस काम की है**
**Team Dashboard** पर MDO को उसी HQ के सभी JMDO के टास्क के काम का एक साथ हाल दिखता है।

1. कार्ड देखें: **Total Tasks**, **Unassigned**, **Requested by JMDO**, **Approved · Today**, **In Progress** और **Completed**।
2. **JMDO-wise summary** में हर JMDO को **Active** या **On Leave** के साथ, और **Assigned**, **In Progress** और **Completed** की गिनती देखें।
3. हफ़्ते के प्लान मंज़ूर करने के लिए **Allocation Requests** दबाएँ।
4. नए आँकड़े देखने के लिए **Refresh** दबाएँ।

> Tip: **Unassigned** में वे JMDO गिने जाते हैं जिन्होंने कोई भी हफ़्ते का प्लान नहीं भेजा। **In Progress** में वे JMDO गिने जाते हैं जो आज ड्यूटी पर हैं।

## आम दिक्कतें

- **"No JMDOs found for your headquarters".** (आपके HQ के लिए कोई JMDO नहीं मिला) आपके ही HQ पर कोई JMDO नहीं है। admin से प्रोफ़ाइल पर HQ जाँचने को कहें।
:::

::: section allocation-requests
title: मंज़ूरी का इंतज़ार कर रहे हफ़्ते के प्लान देखें
routes: /AllocationRequests
for: 
roles: 
designations: 
minutes: 2
source-hash: d233ce1bd4e689c1
confidence: ok

**यह स्क्रीन किस काम की है**
**Allocation Requests** पर वे हफ़्ते के प्लान दिखते हैं जो आपके JMDO ने मंज़ूरी के लिए भेजे हैं। Admin को मंज़ूर हो चुके प्लान और एक **Status** कॉलम भी दिखता है।

1. ऊपर का नीला बॉक्स पढ़ें। इसमें लिखा होता है कि कितने JMDO आपकी मंज़ूरी का इंतज़ार कर रहे हैं।
2. **Search...**, **All Districts**, **All Sub Districts** और **All JMDOs** से लिस्ट छोटी करें।
3. हर लाइन में **Submitted**, **Dealer Tasks**, **SPCM Target**, **Soil Target**, **MD Programs** और **Pos Liquidation** दिखते हैं।
4. प्लान खोलने के लिए **Review** दबाएँ। मंज़ूर हो चुके प्लान के लिए Admin को इसकी जगह **Edit** दिखता है।
5. लिस्ट सेव या प्रिंट करने के लिए **Copy**, **CSV**, **Excel**, **PDF** या **Print** का इस्तेमाल करें।

## आम दिक्कतें

- **"No pending allocation requests".** (कोई प्लान बाकी नहीं) अभी कोई JMDO आपका इंतज़ार नहीं कर रहा।
- **"No allocation requests match these filters".** (इन फ़िल्टर से कोई प्लान नहीं मिला) फ़िल्टर वापस **All Districts**, **All Sub Districts** और **All JMDOs** पर करें।
:::

::: section review-allocation
title: हफ़्ते का प्लान जाँचें और मंज़ूर करें
routes: /AllocationRequestReview
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-ar-dealers] | 2 > [data-help=ff-ar-add] | 3 > [data-help=ff-ar-targets] | 4 > [data-help=ff-ar-programs] | 5 > [data-help=ff-ar-approve]
minutes: 3
source-hash: 791e48a894306411
confidence: ok

**यह स्क्रीन किस काम की है**
**Allocation Requests** से **Review** दबाकर प्लान खोलें। मंज़ूर करने से पहले आप उसे ठीक कर सकते हैं। आपके मंज़ूर करते ही JMDO टास्क शुरू कर सकता है।

1. **Dealer Tasks** में हर डीलर जाँचें। **Date** (यह प्लान वाले हफ़्ते के अंदर होनी चाहिए) या **Priority** (**Normal** या **High Priority**) बदलें। डस्टबिन पर टैप करके **Remove** दबाने पर टास्क हट जाता है।
2. डीलर जोड़ने हों तो **Add dealer task(s)** में उन्हें चुनें, **Date** और **Priority** चुनें, और **Add** दबाएँ।
3. **SPCM Target**, **Soil Sample Target** और **PoS Liquidation Targets** (**Urea**, **DAP**, **NPKS**, **Other**) जाँचें। बॉक्स से बाहर निकलते ही बदलाव सेव हो जाता है।
4. **MD Programs** सिर्फ़ जाँचने के लिए दिखाए जाते हैं। ये CSR1 से आते हैं और यहाँ बदले नहीं जा सकते।
5. नीचे सारांश वाली लाइन पढ़ें, **Approve Allocation** दबाएँ, फिर **Yes, Approve** दबाएँ।

> Careful: डीलर टास्क हटाने के बाद उसे वापस नहीं लाया जा सकता।

## आम दिक्कतें

- **"The date must fall within the JMDO's allocated week"** (तारीख़ JMDO के प्लान वाले हफ़्ते के अंदर होनी चाहिए) **Dealer Tasks** में दिखाए गए हफ़्ते के अंदर की तारीख़ चुनें।
- **"Already assigned on that date"** (उस तारीख़ पर पहले से दिया हुआ है) वह डीलर उस दिन के लिए पहले से प्लान में है।
- **"Couldn't load this allocation."** (यह प्लान खुल नहीं पाया) **Back to all requests** से वापस जाएँ और प्लान फिर से खोलें।
:::

::: section task-monitoring
title: अपनी टीम के हर टास्क पर नज़र रखें
routes: /TaskMonitoring
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-tm-filters] | 2 > [data-help=ff-tm-apply] | 3 > [data-help=ff-tm-search] | 4 > [data-help=ff-tm-refresh]
minutes: 2
source-hash: 0c1b0fa3ecdc07f2
confidence: ok

**यह स्क्रीन किस काम की है**
**Task Monitoring** पर आपके JMDO का हर टास्क दिखता है: वह किसके पास है, उसका टाइप, कहाँ, कब तक करना है और उसका स्टेटस। यह आज के टास्क पर खुलता है।

1. **Filters** में एक या ज़्यादा **JMDO**, **Task Type**, **Status**, **State** या **Region** चुनें, और **Date Range** तय करें।
2. मिलते-जुलते टास्क देखने के लिए **Apply Filters** दबाएँ। **Clear All** से तारीख़ों समेत सारे फ़िल्टर हट जाते हैं।
3. **Tasks** टेबल में लिस्ट छोटी करने के लिए **Search...** में टाइप करें। **Requested** टास्क पर आँख वाला आइकन होता है, जो मंज़ूरी के लिए उसका हफ़्ते का प्लान खोलता है।
4. नया स्टेटस देखने के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **"No tasks match these filters".** (इन फ़िल्टर से कोई टास्क नहीं मिला) हर तारीख़ के टास्क देखने के लिए **Clear All** दबाएँ।
- **"No tasks yet".** (अभी कोई टास्क नहीं) आपके HQ के किसी JMDO ने हफ़्ते का प्लान नहीं भेजा है।
:::

::: section team-tracking
title: देखें कि मेरी टीम कहाँ है
routes: /TeamTracking
for: 
roles: 
designations: 
minutes: 1
source-hash: 158915a8c6a5812a
confidence: ok

**यह स्क्रीन किस काम की है**
**Team Tracking** पर आपके HQ के JMDO की Live Tracking का स्टेटस दिखता है।

1. **JMDO tracking status** टेबल में हर JMDO का **Status** देखें: **Live**, **Stale**, **Offline**, **Stopped**, **Ended automatically** या **Never started**।
2. **Last Seen** और **Last Location** में दिखता है कि आख़िरी लोकेशन कब और कहाँ से आई।
3. **Session Started** में दिखता है कि JMDO ने ट्रैकिंग कब शुरू की।
4. JMDO ढूँढने के लिए **Search...** में टाइप करें, और नए स्टेटस के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **किसी JMDO के लिए "Never started" दिख रहा है।** उस JMDO ने ऐप में Live Tracking शुरू नहीं की है।
- **Stale या Offline.** फ़ोन ने कुछ समय से लोकेशन नहीं भेजी है। JMDO को फ़ोन करें।
:::

::: section yearly-targets
title: JMDO के साल के टारगेट तय करें
routes: /TargetAchievement
for: 
roles: 
designations: 
minutes: 2
source-hash: e1cdc1ea7b68c98e
confidence: ok

**यह स्क्रीन किस काम की है**
**Yearly Target Allocation** पर MDO हर JMDO के वित्त वर्ष (अप्रैल से मार्च) के SPCM और Soil Sample टारगेट तय करता है।

1. **Allocate target to a JMDO** में **JMDO** चुनें। पहले से सेव टारगेट अपने-आप भर जाता है।
2. **SPCM Target (yearly)** और **Soil Sample Target (yearly)** भरें।
3. **Save Target Allocation** दबाएँ।
4. **Current yearly targets** टेबल में हर JMDO का टारगेट दिखता है। किसी लाइन को फ़ॉर्म में लाकर बदलने के लिए पेंसिल दबाएँ।

| फ़ील्ड | क्या भरें |
|---|---|
| **JMDO** | फ़ील्ड अधिकारी। ज़रूरी है। |
| **SPCM Target (yearly)** | पूरी संख्या, शून्य या उससे ज़्यादा। ज़रूरी है। |
| **Soil Sample Target (yearly)** | पूरी संख्या, शून्य या उससे ज़्यादा। ज़रूरी है। |

## आम दिक्कतें

- **"Enter valid, non-negative SPCM and Soil Sample targets."** (SPCM और Soil Sample के सही टारगेट भरें, माइनस में नहीं) पूरी संख्या भरें, शून्य या उससे ज़्यादा।
- **Save बटन ग्रे है।** JMDO चुनें और दोनों टारगेट भरें।
:::

::: section team-reports
title: टीम की रिपोर्ट
routes: /MdoReports
for: 
roles: 
designations: 
minutes: 2
source-hash: 4f488c0a407fcae5
confidence: ok

**यह स्क्रीन किस काम की है**
**Reports** पर दिखता है कि इस महीने आपके JMDO कैसा काम कर रहे हैं: बंद किए गए टास्क, लगा समय, मिट्टी के सैंपल और किसान।

1. **Report Filters** में **Report Type**, **JMDO** और **On or after** (शुरू की तारीख़) चुनें।
2. **Generate Report** दबाएँ।
3. कार्ड देखें: **Tasks Closed**, **Avg. Turnaround**, **Soil Samples** और **Farmers Covered**।
4. **JMDO-wise summary** में हर JMDO के **Tasks Closed**, **On-time %**, **Soil Samples** और **Farmers Added** दिखते हैं।
5. **Detailed Task Report** में हर टास्क **Resolution**, **Status** और **Hours Taken** के साथ दिखता है।
6. टेबल पर **Copy**, **CSV**, **Excel**, **PDF** या **Print** का इस्तेमाल करें, और दोबारा लोड करने के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **Sales Report में कुछ नहीं दिखता।** बिक्री का डेटा अभी टास्क सिस्टम में नहीं है।
- **Farmer Coverage में पूरी जानकारी वाली टेबल नहीं है।** किसानों के आँकड़े सिर्फ़ कार्ड और सारांश में दिखते हैं।
:::

::: section performance-tracker
title: JMDO के काम की तुलना करें
routes: /PerformanceTracker
for: 
roles: 
designations: 
minutes: 1
source-hash: 203ffe19ee1cb6d6
confidence: ok

**यह स्क्रीन किस काम की है**
**Performance Tracker** पर JMDO को सबसे अच्छे से सबसे कम काम के क्रम में दिखाया जाता है। अभी इसमें सैंपल आँकड़े हैं, असली डेटा नहीं।

1. **Filters** खोलें और **JMDO** चुनें।
2. **Apply Filters** दबाएँ। **Clear All** से फिर से सभी JMDO दिखते हैं।
3. हर JMDO का **Rank**, **SPCM Achv.**, **Soil Achv.** और **Attendance** पढ़ें।

## आम दिक्कतें

- **"Detailed performance view ... isn't built yet."** (काम की पूरी जानकारी वाली स्क्रीन अभी नहीं बनी) आँख वाले आइकन के लिए अभी कोई जानकारी वाला पेज नहीं है।
:::

::: section farmer-data
title: किसानों की जानकारी
routes: /FarmerData
for: 
roles: 
designations: 
minutes: 1
source-hash: 78a4629a8079544f
confidence: ok

**यह स्क्रीन किस काम की है**
**Farmer Data** पर आपके इलाके के किसानों की लिस्ट और नया किसान जोड़ने का फ़ॉर्म है। अभी यह स्क्रीन सिर्फ़ एक नमूना है: नई एंट्री सर्वर पर सेव नहीं होतीं।

1. किसानों को **Contact**, **Location**, **Crop(s)** और **Acres** के साथ देखें।
2. **Add Farmer Entry** में **MO H.Q.**, **Month** और **Programme** चुनें।
3. **Farmer Name**, **Contact Number**, **Location**, **Contact Address**, **Acres**, **Crop 1** और **Crop 2** भरें।
4. **Save farmer entry** दबाएँ। किसी लाइन पर पेंसिल दबाने से वह लाइन फ़ॉर्म में कॉपी हो जाती है।

> Tip: मिट्टी के सैंपल के लिए किसान दर्ज करना हो, तो टास्क पर **Add Farmer Data** का इस्तेमाल करें, जो सैंपल कलेक्शन वाली स्क्रीन खोलता है।

## आम दिक्कतें

- **Save बटन ग्रे है।** **Farmer Name** ज़रूरी है।
- **पेज छोड़ने के बाद मेरी एंट्री गायब हो गई।** यह स्क्रीन अभी सर्वर पर सेव नहीं करती।
:::

::: section farmers-hub
title: किसान हब
routes: /Farmers
for: 
roles: 
designations: 
minutes: 1
source-hash: 6be3ac6e9f35012a
confidence: ok

**यह स्क्रीन किस काम की है**
**Farmers** पर वे सारे टूल एक जगह हैं जो फ़ील्ड अधिकारी किसान के सामने इस्तेमाल करता है। एक सूचना में लिखा है कि **Farmer Portal is on its way** (Farmer Portal जल्द आ रहा है)।

1. आने वाले Farmer Portal के बारे में सूचना पढ़ें।
2. **Help a farmer now** में, प्रोडक्ट, डोज़ और इस्तेमाल के बारे में जवाब पाने के लिए **Ask SPIC AI** पर टैप करें।
3. किसानों और SPIC के जानकारों के सवाल-जवाब देखने के लिए **Knowledge Community** पर टैप करें।
4. प्रोडक्ट की जानकारी दिखाने के लिए **Product Information**, **Product Brochures** या **AI Videos** पर टैप करें।

## आम दिक्कतें

- **Knowledge Community नहीं दिख रहा।** आपके रोल को community इस्तेमाल करने की अनुमति नहीं है।
:::

::: section rmd-dashboard
title: RMD डैशबोर्ड
routes: /RMDDashboard
for: 
roles: 
designations: 
minutes: 1
source-hash: 4698a8dc0e852e00
confidence: ok

**यह स्क्रीन किस काम की है**
RMD डैशबोर्ड पर रीजन के लिए CSR और बजट का हाल दिखता है। अभी इसके आँकड़े सैंपल हैं, असली डेटा नहीं, और इसके बटन कुछ भी सेव नहीं करते।

1. ऊपर **Budget Overview** और **CSR Workflow Summary** देखें।
2. **RM Approval Status** और **Budget Utilization** जैसे चार्ट देखें।
3. **Quick Actions**, **Recent Programs & Reports**, **Recent Activity** और **System Updates** देखें।

## आम दिक्कतें

- **आँकड़े मेरे रीजन से मेल नहीं खाते।** इस स्क्रीन के आँकड़े अभी सैंपल हैं।
:::
