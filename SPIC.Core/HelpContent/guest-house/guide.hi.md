---
module: guest-house
title: गेस्ट हाउस
icon: bi-house-door
order: 112
language: hi
category: सुविधाएँ
note: नई गाइड
meta-source-hash: a51949592e8c1854
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 1b36f90eb8520b48
confidence: ok

कंपनी के गेस्ट हाउस में कमरा बुक करें, ज़रूरत हो तो उसका पैसा भरें, और अपनी सारी बुकिंग एक जगह देखें। गेस्ट हाउस पर फ़्रंट ऑफ़िस आपका चेक-इन करता है, आपको कमरा नंबर देता है, चेक-आउट करता है और बिल बनाता है।

- book | icon: bi-calendar-check | color: #b45309
  title: 5 स्टेप में बुकिंग
  text: गेस्ट हाउस, तारीखें, कमरा और मेहमान की जानकारी चुनें, फिर पक्का करें।
- pay | icon: bi-wallet2 | color: #0d9488
  title: कर्मचारियों के लिए मुफ़्त
  text: कंपनी के कर्मचारी मुफ़्त बुक करते हैं। डीलर और बाकी लोग Razorpay से ऑनलाइन पैसा भरते हैं।
- stay | icon: bi-door-open | color: #2563eb
  title: बाकी काम फ़्रंट ऑफ़िस करता है
  text: कमरा नंबर के साथ चेक-इन, चेक-आउट, पेमेंट और बिल।
:::

::: section flow
title: बुकिंग कैसे आगे बढ़ती है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: f66f0d447db14906
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- booker | type: lane | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: कर्मचारी या डीलर
  sub: बुकिंग करने वाला व्यक्ति
- frontoffice | type: lane | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: फ़्रंट ऑफ़िस
  sub: रिसेप्शनिस्ट · एडमिन
- admin | type: lane | color: #dc2626 | roles: Admin, CorporateAdmin
  title: गेस्ट हाउस एडमिन
  sub: कैंसलेशन और मास्टर
- book | lane: booker | col: 1 | icon: bi-house-door | color: #b45309 | next: pay | screen: choose-guest-house
  title: चुनें और भरें
  status: Pending Payment
  text: गेस्ट हाउस, तारीखें और कमरा चुनें, और मेहमान की जानकारी भरें।
- pay | lane: booker | col: 2 | icon: bi-wallet2 | color: #0d9488 | next: checkin | return: book | screen: review-pay
  title: जाँचें और पैसा भरें
  status: Confirmed
  text: सब कुछ जाँचें और **Complete Booking** दबाएँ। कर्मचारियों को कुछ नहीं भरना होता। बाकी लोग ऑनलाइन पैसा भरते हैं। अगर पेमेंट फ़ेल हो जाए, तो नई बुकिंग शुरू करें।
- checkin | lane: frontoffice | col: 3 | icon: bi-box-arrow-in-right | color: #2563eb | next: checkout | screen: check-in
  title: चेक-इन
  status: CheckedIn
  text: फ़्रंट ऑफ़िस पहचान पत्र जाँचता है और कमरा नंबर देता है।
- checkout | lane: frontoffice | col: 4 | icon: bi-box-arrow-right | color: #7c3aed | next: bill | screen: check-out
  title: चेक-आउट
  status: Completed
  text: फ़्रंट ऑफ़िस बाकी पैसा लेता है और मेहमान का चेक-आउट करता है। कमरा फिर से खाली हो जाता है।
- bill | lane: frontoffice | col: 5 | icon: bi-receipt | color: #16a34a | screen: generate-bill
  title: बिल बनाएँ
  status: Billed
  text: फ़्रंट ऑफ़िस खाने और दूसरे खर्च जोड़ता है और टैक्स इनवॉइस बनाता है।
- cancel | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: pay | screen: cancellation-requests
  title: कैंसल और रिफ़ंड
  status: Cancelled
  text: बुकिंग करने वाला **My Bookings** से कैंसल करने की रिक्वेस्ट डालता है। एडमिन उसे मंज़ूर या रिजेक्ट करता है। मंज़ूर हुआ रिफ़ंड उसी तरीके में हाथ से (मैनुअली) लौटाया जाता है, जिससे पैसा भरा गया था।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 41c01f8852ce2a28
confidence: ok

- pending-payment | color: #d97706
  label: Pending Payment
  text: बुकिंग सेव हो गई है और ऑनलाइन पेमेंट का इंतज़ार है।
- confirmed | color: #16a34a
  label: Confirmed
  text: पैसा भर दिया गया है, या कर्मचारी के लिए मुफ़्त है। कमरा आपके लिए रोक कर रखा गया है।
- booking-checked-in | color: #2563eb
  label: CheckedIn
  text: मेहमान अभी कमरे में रुका हुआ है।
- completed | color: #0d9488
  label: Completed
  text: मेहमान चेक-आउट कर चुका है।
- cancelled | color: #dc2626
  label: Cancelled
  text: बुकिंग कैंसल हो गई है, या उसका पेमेंट नहीं हो पाया।
- paid | color: #16a34a
  label: Paid
  text: पैसा मिल गया है।
- pending | color: #d97706
  label: Pending
  text: पैसा अभी नहीं मिला है।
- failed | color: #dc2626
  label: Failed
  text: ऑनलाइन पेमेंट नहीं हो पाया। नई बुकिंग शुरू करें।
- complimentary | color: #0d9488
  label: Complimentary
  text: कंपनी के कर्मचारी के लिए मुफ़्त बुकिंग। कोई पेमेंट नहीं।
- pending-approval | color: #d97706
  label: Pending Approval
  text: आपकी कैंसल करने की रिक्वेस्ट एडमिन के पास रुकी है।
- approved | color: #16a34a
  label: Approved
  text: एडमिन ने कैंसलेशन मंज़ूर कर दिया। कमरा खाली कर दिया गया है।
- rejected | color: #475569
  label: Rejected
  text: एडमिन ने कैंसलेशन नहीं माना। आपकी बुकिंग चालू रहती है।
- refund-manual | color: #7c3aed
  label: Cancelled - Refund to be Processed Manually
  text: रिफ़ंड आपके पेमेंट के उसी तरीके में हाथ से (मैनुअली) लौटाया जाएगा।
- refund-completed | color: #16a34a
  label: Refund Completed
  text: रिफ़ंड लौटा दिया गया है। अपना बैंक या कार्ड स्टेटमेंट देखें।
- no-refund | color: #64748b
  label: Cancelled - No Refund Due
  text: कैंसलेशन के नियम के हिसाब से कोई रिफ़ंड नहीं बनता।
- refund-failed | color: #dc2626
  label: Refund Failed
  text: रिफ़ंड पूरा नहीं हो पाया। टीम आपसे संपर्क करेगी।
- available | color: #16a34a
  label: Available
  text: फ़्रंट ऑफ़िस: कमरा खाली है।
- reserved | color: #d97706
  label: Reserved / Booked
  text: फ़्रंट ऑफ़िस: किसी मेहमान ने कमरा बुक किया है पर अभी चेक-इन नहीं किया।
- checked-in | color: #2563eb
  label: Checked-In
  text: फ़्रंट ऑफ़िस: मेहमान अभी कमरे में है।
- not-billed | color: #64748b
  label: Not Billed
  text: मेहमान चेक-आउट कर चुका है पर अभी बिल नहीं बना।
- billed | color: #16a34a
  label: Billed
  text: इस ठहराव का बिल बन गया है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 0b5537897c5b9427
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- booker | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: कर्मचारी या डीलर
  - कमरा बुक करना
  - अपनी बुकिंग देखना और इनवॉइस डाउनलोड करना
  - कैंसल करने की रिक्वेस्ट डालना और रिफ़ंड पर नज़र रखना
- frontoffice | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: फ़्रंट ऑफ़िस
  - मेहमानों का चेक-इन करना और कमरा नंबर देना
  - पैसा लेना और मेहमानों का चेक-आउट करना
  - बिल बनाना और प्रिंट करना
- admin | color: #dc2626 | roles: Admin, CorporateAdmin
  title: गेस्ट हाउस एडमिन
  - कैंसल करने की रिक्वेस्ट मंज़ूर या रिजेक्ट करना
  - गेस्ट हाउस, रूम टाइप और कमरे जोड़ना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: c456cc4a4b5c2181
confidence: ok

- free-booking | popular: true
  q: मेरे कुल में ₹0.00 (Complimentary) क्यों दिख रहा है?
  a: कंपनी के कर्मचारी गेस्ट हाउस के कमरे मुफ़्त बुक करते हैं। डीलर और बाकी लोग बुकिंग करते समय ऑनलाइन पैसा भरते हैं।
- payment-failed | popular: true
  q: मेरा पेमेंट फ़ेल हो गया, या मैंने पेमेंट की विंडो बंद कर दी
  a: वह बुकिंग पूरी नहीं हो सकती, और कमरा खाली कर दिया जाता है। **Book a Room** से नई बुकिंग शुरू करें। अगर आपके खाते से पैसा कट गया है, तो वह अपने-आप वापस आ जाता है।
- no-cancel
  q: मुझे Cancel Booking बटन नहीं दिख रहा
  a: यह सिर्फ़ आने वाली (upcoming) बुकिंग पर दिखता है। जिस बुकिंग का चेक-इन हो चुका है, उसे ऑनलाइन कैंसल नहीं किया जा सकता। कृपया फ़्रंट ऑफ़िस से बात करें।
- refund-when
  q: मुझे मेरा रिफ़ंड कब मिलेगा?
  a: एडमिन के आपका कैंसलेशन मंज़ूर करने के बाद, 2 कामकाजी दिनों में रिफ़ंड आपके पेमेंट के उसी तरीके में हाथ से लौटाया जाता है। उस पर नज़र रखने के लिए **Refund Status** खोलें।
- no-rooms
  q: मुझे "No rooms available" दिख रहा है
  a: उन तारीखों के लिए सारे कमरे बुक हैं। **Change dates** दबाएँ और दूसरी तारीखें, या कोई दूसरा गेस्ट हाउस आज़माएँ।
- early-checkin
  q: फ़्रंट ऑफ़िस मेरा चेक-इन नहीं कर पा रहा
  a: चेक-इन सिर्फ़ चेक-इन की तारीख से ठहराव खत्म होने तक, और सिर्फ़ **Confirmed** बुकिंग के लिए ही हो सकता है।
:::

::: section choose-guest-house
title: गेस्ट हाउस और अपनी तारीखें चुनें
routes: /GuestHouse, /GuestHouseBooking
for: 
roles: 
designations: 
minutes: 2
source-hash: 3d1914b34d0d67da
confidence: ok

**यह स्क्रीन किस काम की है**
बुकिंग यहीं से शुरू होती है। पहले **Choose Your Guest House** पर गेस्ट हाउस चुनें, फिर **Guest House Booking** (5 में से स्टेप 1) पर अपनी तारीखें चुनें।

1. मेन्यू में **SDWA** खोलें और **Book a Room** पर टैप करें। हर कार्ड पर एक गेस्ट हाउस, उसका पता और फ़ोन नंबर दिखता है।
2. जो गेस्ट हाउस चाहिए, उसके कार्ड पर **Select** दबाएँ। बटन पर गेस्ट हाउस का नाम दिखता है।
3. **Guest House Booking** पर **Check-in Date** और **Check-in Time** चुनें।
4. **Check-out Date** और **Check-out Time** चुनें।
5. **Important Information** बॉक्स पढ़ें, फिर **Check Availability** दबाएँ।

> Tip: कोई सही पहचान पत्र (आधार या पासपोर्ट) साथ रखें। चेक-इन के समय इसकी ज़रूरत होती है।

## आम दिक्कतें

- **"Check-out must be after Check-in."** (चेक-आउट, चेक-इन के बाद होना चाहिए) बाद की चेक-आउट तारीख या समय चुनें।
- **"No guest houses available".** (कोई गेस्ट हाउस उपलब्ध नहीं) **Retry** दबाएँ। अगर फिर भी यही दिखे, तो SDWA ऑफ़िस से संपर्क करें।
- **मुझे वापस Choose Your Guest House पर भेज दिया जाता है।** पहले **Select** से गेस्ट हाउस चुनें, फिर तारीखें चुनें।
:::

::: section choose-room
title: कमरा चुनें
routes: /Rooms
for: 
roles: 
designations: 
minutes: 2
source-hash: 9eefa1291c16d092
confidence: ok

**यह स्क्रीन किस काम की है**
**Available Rooms** स्क्रीन (5 में से स्टेप 2) पर वे कमरे दिखते हैं जो आपकी तारीखों के लिए खाली हैं।

1. कमरों के कार्ड देखें। हर कार्ड पर कमरे का टाइप, एक रात का रेट, कितने कमरे उपलब्ध हैं और कितने मेहमान रुक सकते हैं, यह दिखता है।
2. सिर्फ़ वही कमरे देखने के लिए ऊपर किसी रूम टाइप पर टैप करें, या **Search** में टाइप करें। **All** से सारे कमरे दिखते हैं।
3. आपको जितने कमरे चाहिए, वह **Number of Rooms** में चुनें।
4. अगर एक्स्ट्रा बेड चाहिए, तो **Additional Bed** पर टिक करें और **No. of Additional Beds** चुनें। एक्स्ट्रा बेड का एक रात का अलग रेट होता है।
5. **Book Now** दबाएँ।

> Tip: **View Details** बटन अभी चालू नहीं है। कमरे की सारी जानकारी कार्ड पर ही है।

## आम दिक्कतें

- **"No rooms available".** (कोई कमरा उपलब्ध नहीं) उन तारीखों के लिए कोई कमरा खाली नहीं है। **Change dates** दबाएँ।
- **"No matching rooms".** (कोई मिलता-जुलता कमरा नहीं) **Clear filters** दबाएँ।
- **"Up to ... rooms can be booked at a time for this room type."** (इस रूम टाइप के लिए एक बार में ... कमरे तक ही बुक हो सकते हैं) बाकी कमरे दूसरी बुकिंग में बुक करें।
:::

::: section guest-details
title: मेहमान की जानकारी भरें
routes: /GuestDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: eca96eb90f508346
confidence: ok

**यह स्क्रीन किस काम की है**
**Guest Details** (5 में से स्टेप 3) पर आप बताते हैं कि कौन रुकेगा। दाईं ओर **Booking Summary** में कीमत दिखती है।

1. **Employee ID / Dealer Code** और **Guest Name** भरें।
2. अगर **Company Name** लिस्ट में है तो उसे चुनें, और **Phone Number** और **Email ID** भरें। डीलर के लिए फ़ोन और ईमेल डीलर रजिस्ट्रेशन से अपने-आप भर जाते हैं।
3. **Aadhaar / Passport Number** भरें और **Nationality** चुनें।
4. **Number of Persons**, **Number of Adults** और **Number of Children (below 12 years)** चुनें।
5. चाहें तो **Upload ID Proof (Optional)** में पहचान पत्र अपलोड करें।
6. **Booking Summary** जाँचें: कमरे का खर्च, **GST (5%)** और **Total**। फिर **Continue Review** दबाएँ।

| फ़ील्ड | क्या भरें |
|---|---|
| **Employee ID / Dealer Code** | आपकी कर्मचारी ID या डीलर कोड। ज़रूरी है। |
| **Guest Name** | जो व्यक्ति रुकेगा, उसका नाम। ज़रूरी है। |
| **Phone Number** और **Email ID** | मेहमान का संपर्क। ज़रूरी है। |
| **Nationality** | Indian, NRI या Foreigner। ज़रूरी है। |
| **Company Name** | बिल के लिए कंपनी और उसका GSTIN। ज़रूरी नहीं। |
| **Upload ID Proof (Optional)** | एक PDF, JPG या PNG फ़ाइल, 5 MB तक। |

## आम दिक्कतें

- **"Employee ID / Dealer Code is required to continue."** (आगे बढ़ने के लिए Employee ID / Dealer Code ज़रूरी है) हर ज़रूरी फ़ील्ड भरें। मैसेज में छूटे हुए फ़ील्ड का नाम होता है।
- **"Only PDF, JPG, or PNG files are allowed."** (सिर्फ़ PDF, JPG या PNG फ़ाइल चलती है) या **"File must be 5 MB or less."** (फ़ाइल 5 MB या उससे छोटी होनी चाहिए) कोई दूसरी फ़ाइल चुनें या छोटी फ़ोटो लें।
- **मुझे कमरा बदलना है।** **Available Rooms** पर लौटने के लिए **Back** दबाएँ।
:::

::: section review-pay
title: बुकिंग जाँचें और पूरी करें
routes: /BookingPreview, /Payment
for: 
roles: 
designations: 
minutes: 3
source-hash: 0ae6b596dd70c921
confidence: ok

**यह स्क्रीन किस काम की है**
**Review Your Booking** (5 में से स्टेप 4) पर आप सब कुछ जाँचते हैं। **Payment** (5 में से स्टेप 5) पर आप बुकिंग पक्की करते हैं। कंपनी के कर्मचारियों को कुछ नहीं भरना होता। डीलर और बाकी लोग Razorpay से ऑनलाइन पैसा भरते हैं।

1. **Review Your Booking** पर **Booking Information**, **Booking Summary** और **Guest Information** जाँचें।
2. कुछ बदलना हो तो **Edit Details** दबाएँ। सब सही हो तो **Proceed to Payment** दबाएँ।
3. **Payment** पर **Booking Summary** जाँचें। कर्मचारियों को **Complimentary (Employee)** और कुल ₹0.00 दिखता है। बाकी लोगों को **Online Payment** दिखता है।
4. **Complete Booking** दबाएँ। ऑनलाइन पेमेंट के लिए Razorpay की विंडो खुलती है। UPI, कार्ड, नेट बैंकिंग या वॉलेट से पैसा भरें।
5. **Booking Confirmed!** बॉक्स में आपका **Booking ID** और **Payment Status** दिखता है। **View My Bookings** दबाएँ, या फिर से बुक करने के लिए **New Booking** दबाएँ।

> Careful: पैसा भरते समय पेमेंट की विंडो बंद न करें और पेज न छोड़ें। अगर पेमेंट फ़ेल हो जाए या आप विंडो बंद कर दें, तो यह बुकिंग पूरी नहीं हो सकती और आपको नई बुकिंग शुरू करनी होगी।

## आम दिक्कतें

- **"No booking details found".** (बुकिंग की जानकारी नहीं मिली) बुकिंग शुरू नहीं हुई थी या उसका समय निकल गया है। **Book a Room** से फिर से शुरू करें।
- **बटन पर "Payment Failed" दिख रहा है।** नई बुकिंग शुरू करें। अगर पैसा कट गया है, तो वह अपने-आप वापस आ जाता है।
- **"This room was just booked by someone else for the selected dates. Please try again."** (चुनी गई तारीखों के लिए यह कमरा अभी-अभी किसी और ने बुक कर लिया। फिर से कोशिश करें।) कमरा या तारीखें फिर से चुनें।
- **"Your room hold has expired..."** (आपके लिए रोका गया कमरा अब रुका नहीं है) पैसा भरने में बहुत देर हो गई। नई बुकिंग शुरू करें।
:::

::: section my-bookings
title: मेरी बुकिंग देखें
routes: /MyBookings
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-mb-cards] | 2 > [data-help=gh-mb-tabs] | 3 > [data-help=gh-mb-list] | 4 > [data-help=gh-mb-list] | 5 > [data-help=gh-mb-new]
minutes: 2
source-hash: eddb90512855180d
confidence: ok

**यह स्क्रीन किस काम की है**
**My Bookings** में आपकी की हुई हर कमरा बुकिंग दिखती है, ऊपर गिनती के साथ। इसे मेन्यू से खोलें: **SDWA**, फिर **My Bookings / Cancellation**।

1. ऊपर के कार्ड देखें: **Total Bookings**, **Upcoming**, **Completed** और **Cancelled**।
2. किसी टैब पर टैप करें: **Upcoming**, **Completed** या **Cancelled**।
3. हर बुकिंग कार्ड पर रूम टाइप, बुकिंग का स्टेटस, पेमेंट का स्टेटस, **Booking ID**, गेस्ट हाउस, तारीखें, **Guests**, **Rooms** और **Total Amount** दिखते हैं।
4. बुकिंग खोलने के लिए **View Details** दबाएँ। आने वाली बुकिंग पर कैंसल करने की रिक्वेस्ट के लिए **Cancel Booking** दबाएँ। कैंसल हुई बुकिंग पर **Refund Status** दबाएँ।
5. दूसरा कमरा बुक करने के लिए **New Booking** दबाएँ।

## आम दिक्कतें

- **"No upcoming bookings".** (कोई आने वाली बुकिंग नहीं) आज या उसके बाद की आपकी कोई बुकिंग नहीं है। **Book a room** दबाएँ।
- **"Could not load bookings".** (बुकिंग लोड नहीं हो पाईं) अपना इंटरनेट जाँचें और **Retry** दबाएँ।
- **बुकिंग पर Cancelled और Failed दिख रहा है।** उसका ऑनलाइन पेमेंट नहीं हो पाया। फिर से बुक करें।
:::

::: section booking-details
title: एक बुकिंग देखें और इनवॉइस डाउनलोड करें
routes: /BookingDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 4c0fd3b25f0fb3dd
confidence: ok

**यह स्क्रीन किस काम की है**
**Booking Details** में एक बुकिंग की पूरी जानकारी दिखती है: कमरा, तारीखें, पेमेंट, स्टेटस और मेहमान।

1. **My Bookings** पर किसी बुकिंग पर **View Details** दबाएँ।
2. **Room Details** और **Check-in & Check-out** देखें, साथ में रातों, कमरों और एक्स्ट्रा बेड की गिनती। **Room Number** तय होने पर दिखता है।
3. **Payment Details** देखें: कमरे का खर्च, **GST (5%)**, **Total Amount**, **Payment Method** और **Payment Status**।
4. **Booking Status** देखें। कैंसल हुई बुकिंग पर **Cancellation Status** भी दिखता है।
5. **Guest Details** देखें। अगर आपने पहचान पत्र अपलोड किया था, तो **View** या **Download** दबाएँ।
6. टैक्स इनवॉइस को PDF में सेव करने के लिए **Download Invoice** दबाएँ।
7. वापस जाने के लिए **Back to My Bookings** दबाएँ।

## आम दिक्कतें

- **Download Invoice बटन नहीं है।** यह तभी दिखता है जब पेमेंट **Paid** हो और इनवॉइस तैयार हो।
- **"Booking not found."** (बुकिंग नहीं मिली) आप सिर्फ़ अपनी की हुई बुकिंग ही खोल सकते हैं।
:::

::: section cancel-booking
title: बुकिंग कैंसल करें
routes: /CancelBooking
for: 
roles: 
designations: 
minutes: 3
source-hash: 58fac8bc78e4e38a
confidence: ok

**यह स्क्रीन किस काम की है**
कैंसल करने की रिक्वेस्ट डालने के लिए **Cancel Booking** का इस्तेमाल करें। एडमिन आपकी रिक्वेस्ट जाँचता है। जब तक एडमिन मंज़ूर नहीं करता, आपकी बुकिंग चालू रहती है।

1. **My Bookings** पर किसी आने वाली बुकिंग पर **Cancel Booking** दबाएँ।
2. **Important Notice** पढ़ें। चेक-इन में कितना समय बाकी है, उसके हिसाब से आपको कितने प्रतिशत रिफ़ंड मिलेगा, यह उसमें दिखता है।
3. **Cancellation Reason** में एक कारण चुनें।
4. **Refund Calculation** जाँचें: **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)**, **Refund Amount**, **Refund Method** और **Estimated Time**।
5. **Continue Cancellation** दबाएँ। **Confirm Cancellation?** में **Cancel Booking** दबाएँ। अगर आपका मन बदल गया है, तो **Keep My Booking** दबाएँ।
6. **Cancellation Request Submitted** में आपका **Cancellation ID** और **Pending Approval** दिखता है। **Refund Status**, **View Booking** या **Go to My Bookings** दबाएँ।

> Careful: चेक-इन जितना पास होगा, रिफ़ंड उतना कम होगा। पहले **Cancellation Policy** बॉक्स पढ़ें।

## आम दिक्कतें

- **"Please select a reason for cancellation."** (कृपया कैंसल करने का कारण चुनें) पहले कारण चुनें।
- **"This booking is already checked in and cannot be cancelled online. Please contact the Front Office."** (इस बुकिंग का चेक-इन हो चुका है, इसे ऑनलाइन कैंसल नहीं किया जा सकता। कृपया फ़्रंट ऑफ़िस से संपर्क करें।) फ़्रंट ऑफ़िस से बात करें।
- **"A cancellation request for this booking has already been submitted".** (इस बुकिंग के लिए कैंसल करने की रिक्वेस्ट पहले ही भेजी जा चुकी है) आप पहले ही रिक्वेस्ट डाल चुके हैं। **View Refund Status** दबाएँ।
:::

::: section refund-status
title: अपने रिफ़ंड पर नज़र रखें
routes: /RefundStatus
for: 
roles: 
designations: 
minutes: 2
source-hash: f4d86371946e0403
confidence: ok

**यह स्क्रीन किस काम की है**
**Refund Status** में दिखता है कि आपका कैंसलेशन और रिफ़ंड अभी कहाँ तक पहुँचा है।

1. **My Bookings** पर **Cancelled** टैब खोलें और **Refund Status** दबाएँ। रिक्वेस्ट भेजने के बाद आप इसे **Cancel Booking** से भी खोल सकते हैं।
2. ऊपर **Total Refund Amount**, **Cancellation ID**, **Refund Method**, **Target Refund Date**, **Transaction Ref.** और **Current Status** देखें।
3. **Refund Breakdown** में **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)** और **Refund Amount** देखें।
4. **Refund Tracking** में स्टेप देखें: **Cancellation Requested**, फिर **Pending Admin Approval**, **Approved by Admin** या **Cancellation Rejected**, फिर रिफ़ंड।
5. अगर तय तारीख तक पैसा नहीं पहुँचा, तो नारंगी बॉक्स में दिया गया रेफ़रेंस बताकर सपोर्ट से संपर्क करें।
6. **My Bookings** पर लौटने के लिए **Back** दबाएँ।

## आम दिक्कतें

- **मेरी बुकिंग Cancelled टैब में नहीं है।** यह वहाँ तभी जाती है जब एडमिन मंज़ूर कर दे। तब तक बुकिंग की **Cancel Booking** स्क्रीन खोलें और **View Refund Status** दबाएँ।
- **"Cancellation Rejected".** (कैंसलेशन रिजेक्ट हुआ) कारण पढ़ें। आपकी बुकिंग चालू रहती है।
- **"No Refund Due".** (कोई रिफ़ंड नहीं बनता) कैंसलेशन के नियम के हिसाब से कोई पैसा वापस नहीं होता, जैसे कर्मचारी की मुफ़्त बुकिंग में।
:::

::: section front-office
title: फ़्रंट ऑफ़िस पर सारे कमरे देखें
routes: /FrontOffice
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-fo-cards] | 2 > [data-help=gh-fo-tabs] | 3 > [data-help=gh-fo-search] | 4 > [data-help=gh-fo-bill]
minutes: 2
source-hash: c62b602840a09360
confidence: ok

**यह स्क्रीन किस काम की है**
**Front Office** स्क्रीन पर हर कमरा दिखता है: वह खाली है, बुक है या उसमें कोई रुका है, और वह किसके पास है। इसे मेन्यू से खोलें: **SDWA**, फिर **Front Office**। रिसेप्शनिस्ट को सिर्फ़ अपने राज्य का गेस्ट हाउस दिखता है।

1. ऊपर के कार्ड देखें: **Total Rooms**, **Available**, **Reserved** और **Checked-In**।
2. किसी टैब पर टैप करें: **All**, **Available**, **Reserved / Booked** या **Checked-In**।
3. **Search rooms...** में कमरा नंबर, बुकिंग ID या मेहमान का नाम टाइप करें, या **All Guest Houses** में गेस्ट हाउस चुनें। हर लाइन में कमरा, उसका स्टेटस, बुकिंग और मेहमान, तारीखें और कार्रवाई (actions) दिखती हैं।
4. जिस मेहमान का चेक-आउट हो चुका है, उसका बिल बनाने के लिए **Generate Bill** दबाएँ। कमरों की लिस्ट फिर से लोड करने के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **"You do not have permission to access the Front Office."** (आपको फ़्रंट ऑफ़िस खोलने की अनुमति नहीं है) अपने एडमिनिस्ट्रेटर से फ़्रंट ऑफ़िस की अनुमति माँगें।
- **"No rooms found".** (कोई कमरा नहीं मिला) **All** और **All Guest Houses** चुनें, और सर्च खाली करें।
:::

::: section check-in
title: मेहमान का चेक-इन करें
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 3
source-hash: 4d3a8c4aabfb6c8a
confidence: ok

**यह स्क्रीन किस काम की है**
मेहमान के आने पर उसका चेक-इन करें और उसे कमरा नंबर दें।

1. **Front Office** पर **Reserved / Booked** पर टैप करें और बुकिंग ढूँढें।
2. **Actions** कॉलम में **Check-In** दबाएँ।
3. **Check-In** बॉक्स में **Booking ID**, **Guest Name**, **Room Type** और **Guest House** जाँचें। अगर पहचान पत्र जुड़ा है, तो उसे देखने के लिए **View** दबाएँ।
4. **Number of Persons** और **Number of Extra Beds** जाँचें, और ज़रूरत हो तो ठीक करें।
5. **Assign Physical Room(s)** में देने वाले कमरा नंबरों पर टैप करें। जितने कमरे बुक हुए थे, ठीक उतने ही चुनें।
6. **Confirm Check-In** दबाएँ। अब कमरे पर **Checked-In** दिखता है।

> Careful: चेक-इन से पहले मेहमान का पहचान पत्र (आधार या पासपोर्ट) जाँच लें।

## आम दिक्कतें

- **"This booking's check-in date has not arrived yet."** (इस बुकिंग की चेक-इन तारीख अभी नहीं आई) चेक-इन की तारीख पर या उसके बाद चेक-इन करें।
- **"This booking's stay period has passed and it can no longer be checked in."** (इस बुकिंग के ठहराव का समय निकल चुका है, अब चेक-इन नहीं हो सकता) ठहराव की तारीखें खत्म हो चुकी हैं।
- **"Please select exactly ... physical room(s) before confirming check-in."** (चेक-इन पक्का करने से पहले ठीक ... कमरे चुनें) ज़्यादा या कम कमरा नंबरों पर टैप करें।
- **"No physical rooms are currently free for the booked stay period."** (बुक किए गए ठहराव के समय के लिए अभी कोई कमरा खाली नहीं है) बुकिंग करने वाले से संपर्क करें या कोई दूसरा रूम टाइप चुनें।
- **"These rooms were just assigned to another overlapping stay. Please refresh and try again."** (ये कमरे अभी-अभी उसी समय के किसी दूसरे ठहराव को दे दिए गए। रिफ़्रेश करके फिर से कोशिश करें।) **Refresh** दबाएँ और दूसरे कमरे चुनें।
:::

::: section check-out
title: मेहमान का चेक-आउट करें और पैसा लें
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 2
source-hash: cf102cb774da6ac0
confidence: ok

**यह स्क्रीन किस काम की है**
मेहमान के जाते समय पक्का करें कि पैसा मिल गया है, फिर उसका चेक-आउट करें। कमरा फिर से खाली हो जाता है।

1. **Front Office** पर **Checked-In** पर टैप करें और मेहमान को ढूँढें।
2. **Actions** कॉलम में **Check-Out** दबाएँ।
3. **Check-Out & Payment** में **Room Number**, तारीखें, **Number of Persons**, **Extra Beds**, **Total Amount**, **Payment Method** और **Payment Status** जाँचें।
4. अगर **Payment Status** में **Pending** है, तो पैसा लें और **Confirm Payment Received** दबाएँ।
5. जब **Paid** दिखे, तो **Confirm Check-Out** दबाएँ। कमरे पर फिर से **Available** दिखता है।
6. अब **Generate Bill** से बिल बनाएँ।

## आम दिक्कतें

- **"Payment must be received before check-out."** (चेक-आउट से पहले पैसा मिलना ज़रूरी है) पहले **Confirm Payment Received** दबाएँ।
- **"Check-Out is only allowed for a booking that is currently checked in."** (चेक-आउट सिर्फ़ उसी बुकिंग का हो सकता है जिसका अभी चेक-इन हुआ है) मेहमान का चेक-इन नहीं हुआ है। **Status** देखें।
:::

::: section bills
title: पूरे हुए ठहराव और बिल ढूँढें
routes: /GenerateBill
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-gb-filters] | 2 > [data-help=gh-gb-go] | 3 > [data-help=gh-gb-tabs] | 4 > [data-help=gh-gb-find]
minutes: 2
source-hash: 5194cb8d5292259e
confidence: ok

**यह स्क्रीन किस काम की है**
**Generate Bill** स्क्रीन पर वे ठहराव दिखते हैं जिनका चेक-आउट हो चुका है, और पहले से बने बिल भी। इसे मेन्यू से (**SDWA**, फिर **Generate Bill**) या **Front Office** से खोलें।

1. **Filters** में **From Date** और **To Date** चुनें। ठहराव के लिए ये चेक-आउट की तारीखें हैं और बिल के लिए बिल की तारीखें।
2. **Search** दबाएँ। **Today** से सिर्फ़ आज का दिखता है। **Refresh** से लिस्ट फिर से लोड होती है।
3. चेक-आउट हो चुके ठहराव देखने के लिए **Completed Bookings** पर टैप करें, या पहले से बने बिल देखने के लिए **Generated Bills** पर टैप करें।
4. सर्च बॉक्स में बुकिंग ID या मेहमान का नाम टाइप करें। ठहराव पर **Bill Status** में **Not Billed** या **Billed** दिखता है। **Generate Bill** या **View Bill** दबाएँ।

## आम दिक्कतें

- **"From Date cannot be after To Date."** (From Date, To Date के बाद नहीं हो सकती) तारीखें बदलें।
- **कोई ठहराव लिस्ट में नहीं है।** सिर्फ़ चेक-आउट हो चुके ठहराव, उनकी चेक-आउट तारीख के हिसाब से दिखते हैं। तारीखों का दायरा बढ़ाएँ।
:::

::: section generate-bill
title: ठहराव का बिल बनाएँ
routes: /GenerateBill
for: 
roles: 
designations: 
minutes: 4
source-hash: 71ab93bc25bfd36f
confidence: ok

**यह स्क्रीन किस काम की है**
चेक-आउट के बाद ठहराव का टैक्स इनवॉइस बनाएँ। हर ठहराव का बिल सिर्फ़ एक बार बन सकता है।

1. **Completed Bookings** पर जिस ठहराव पर **Not Billed** दिख रहा है, उस पर **Generate Bill** दबाएँ। **New Bill** स्क्रीन खुलती है।
2. **Booking Summary** जाँचें: **Booking**, **Guest**, **Company Name**, **GSTIN**, **Guest House**, **Room** और **Stay**।
3. **Bill Items** में कमरे की लाइन पहले से भरी होती है। अगर हों, तो **Food Bill** और **Additional Beds** की रकम भरें।
4. लॉन्ड्री या दूसरे खर्च के लिए **Add Charge** दबाएँ। विवरण, **Qty**, **Rate**, **CGST %** और **SGST %** भरें। **Remove** से जोड़ी गई लाइन हट जाती है।
5. **Totals, Discount & Payment** में ज़रूरत हो तो **Discount**, **Advance Payment** और **Round Off** भरें। **Payment Method** चुनें, **Payment received** पर टिक करें और **Remarks** लिखें।
6. **Balance Payable** जाँचें, फिर **Generate Bill** दबाएँ। नया बिल **Generated Bills** टैब पर दिखता है।
7. इनवॉइस देखने के लिए **View** दबाएँ, फिर **Download PDF** या **Print** दबाएँ। लिस्ट पर **PDF** बटन इसे सीधे डाउनलोड करता है।

> Careful: **Generate Bill** दबाने से पहले हर रकम जाँच लें। उसी ठहराव का बिल दोबारा नहीं बन सकता।

| फ़ील्ड | क्या भरें |
|---|---|
| **Food Bill** | ठहराव के दौरान दिए गए खाने की मात्रा और रेट। |
| **Add Charge** | कोई भी दूसरा खर्च, उसके अपने CGST % और SGST % के साथ। |
| **Advance Payment** | इस ठहराव के लिए पहले से भरा गया पैसा। पता हो तो यह अपने-आप भर जाता है। |
| **Payment Method** | UPI, Card, Net Banking या Cash। कर्मचारी के मुफ़्त ठहराव पर Complimentary भी दिखता है। |

## आम दिक्कतें

- **"Discount cannot exceed Grand Total."** (Discount, Grand Total से ज़्यादा नहीं हो सकता) या **"Advance Payment cannot exceed Grand Total."** (Advance Payment, Grand Total से ज़्यादा नहीं हो सकता) रकम कम करें।
- **"Balance Payable cannot be negative."** (Balance Payable माइनस में नहीं हो सकता) **Discount**, **Advance Payment** और **Round Off** जाँचें।
- **"A bill has already been generated for this booking".** (इस बुकिंग का बिल पहले ही बन चुका है) इसकी जगह **View Bill** से उसे खोलें।
:::

::: section bill-list
title: सारे बने हुए बिल देखें
routes: /BillList
for: 
roles: 
designations: 
minutes: 1
source-hash: 863b55025a37bbd6
confidence: ok

**यह स्क्रीन किस काम की है**
**Bill List** स्क्रीन पर बना हुआ हर इनवॉइस दिखता है, सबसे नया सबसे ऊपर। यही बिल **Generate Bill** के **Generated Bills** टैब पर भी दिखते हैं।

1. टेबल देखें: **Bill #**, **Booking**, **Guest**, **Guest House**, **Room**, **Bill Date**, तारीखें, **Total**, **Balance** और **Payment**।
2. इनवॉइस खोलने के लिए **View** दबाएँ। उसमें **Download PDF** या **Print** दबाएँ।
3. इनवॉइस तुरंत डाउनलोड करने के लिए **PDF** दबाएँ।
4. और लाइनें देखने के लिए **Show more** दबाएँ।
5. नया बिल बनाने के लिए **Generate Bill** दबाएँ, या फिर से लोड करने के लिए **Refresh** दबाएँ।

## आम दिक्कतें

- **"No bills yet".** (अभी कोई बिल नहीं) अभी तक कोई बिल नहीं बना है। **Generate Bill** से एक बनाएँ।
:::

::: section cancellation-requests
title: कैंसल करने की रिक्वेस्ट मंज़ूर या रिजेक्ट करें
routes: /GuestHouseCancellations
for: 
roles: 
designations: 
minutes: 3
source-hash: abf61f8cc73c49f2
confidence: ok

**यह स्क्रीन किस काम की है**
जब कोई बुकिंग कैंसल करने की रिक्वेस्ट डालता है, तो वह यहाँ आती है। इसे मेन्यू से खोलें: **SDWA**, फिर **Cancellation Requests**।

1. किसी टैब पर टैप करें: **Pending Approval**, **Approved**, **Rejected** या **All**।
2. **Search requests...** में टाइप करें, या **All Guest Houses** में गेस्ट हाउस चुनें।
3. लाइन पढ़ें: बुकिंग, मेहमान, ठहराव, भरी गई रकम (**Paid**), **Refund**, **Reason** और रिक्वेस्ट कब आई (**Requested**)।
4. मानने के लिए **Approve** दबाएँ, बॉक्स पढ़ें और **Approve Cancellation** दबाएँ। बुकिंग कैंसल हो जाती है और कमरा खाली हो जाता है।
5. मना करने के लिए **Reject** दबाएँ, **Reason for rejection** लिखें और **Reject Request** दबाएँ। बुकिंग चालू रहती है।
6. रिफ़ंड मंज़ूर करने के बाद, 2 कामकाजी दिनों में उसे पेमेंट के उसी तरीके में हाथ से लौटाएँ।

> Careful: ऐप खुद रिफ़ंड नहीं भेजता। किसी को इसे ऐप के बाहर लौटाना होगा।

## आम दिक्कतें

- **"You do not have permission to review cancellation requests."** (आपको कैंसल करने की रिक्वेस्ट जाँचने की अनुमति नहीं है) अपने एडमिनिस्ट्रेटर से यह अनुमति माँगें।
- **"The guest is checked in. Check the guest out instead of cancelling."** (मेहमान का चेक-इन हो चुका है। कैंसल करने की जगह मेहमान का चेक-आउट करें।) **Front Office** पर **Check-Out** का इस्तेमाल करें।
- **Reject बटन नहीं है।** इस रिक्वेस्ट का रिफ़ंड पहले से चल रहा है।
:::

::: section guest-house-master
title: गेस्ट हाउस, रूम टाइप और कमरे जोड़ें
routes: /GuestHouseMaster
for: 
roles: 
designations: 
minutes: 4
source-hash: 7f6f2fee8bd4e156
confidence: ok

**यह स्क्रीन किस काम की है**
लोग क्या बुक कर सकते हैं, यह तय करने के लिए एडमिन **Guest House & Room Master** का इस्तेमाल करते हैं। इसे मेन्यू से खोलें: **SDWA**, फिर **Guest House**। पहले गेस्ट हाउस जोड़ें, फिर रूम टाइप, फिर कमरे।

1. **Guest House** टैब पर **Name** भरें, और पता हो तो **Address**, **Phone Number**, **State** और **Description** भरें। फ़ोटो जोड़ने के लिए **Upload Image** दबाएँ।
2. **Room Type** टैब पर **Room Type Name** भरें, जैसे AC Deluxe Room।
3. **Rooms** टैब पर **Guest House** और **Room Type (Master)** चुनें, फिर **Room Type**, **Room Number**, **No. of Rooms** और **Rate (₹)** भरें।
4. स्विच को **Active** या **Inactive** पर रखें और **Save** दबाएँ। **Cancel** से फ़ॉर्म खाली हो जाता है।
5. **Guest House** या **Rooms** टैब पर एक साथ कई जोड़ने के लिए **Download Template** दबाएँ, उसे भरें, फिर **Upload File** दबाएँ।
6. टेबल में सर्च बॉक्स और **All Status** का इस्तेमाल करें। स्टेटस बदलने के लिए **Active** या **Inactive** पर टैप करें, बदलाव के लिए पेंसिल, या मिटाने के लिए डस्टबिन पर टैप करें।

> Careful: मिटाया हुआ वापस नहीं आता। कुछ समय के लिए बुकिंग रोकनी हो, तो इसकी जगह **Inactive** करें। Inactive गेस्ट हाउस बुकिंग के लिए नहीं दिखते।

| फ़ील्ड | क्या भरें |
|---|---|
| **Name** | गेस्ट हाउस का नाम। ज़रूरी है। |
| **State** | गेस्ट हाउस किस राज्य में है। उस राज्य के रिसेप्शनिस्ट इसे संभालते हैं। |
| **No. of Rooms** | इस टाइप के कितने कमरे। पूरी संख्या, 1 या ज़्यादा। ज़रूरी है। |
| **Rate (₹)** | एक रात का रेट। ज़रूरी है। |

## आम दिक्कतें

- **"No. of Rooms must be a positive whole number."** (No. of Rooms, 1 या उससे ज़्यादा की पूरी संख्या होनी चाहिए) 1 या ज़्यादा भरें।
- **"Rate must be a valid amount."** (Rate सही रकम होनी चाहिए) कोई संख्या भरें, जैसे 1200।
- **मुझे टेबल दिख रही हैं पर फ़ॉर्म नहीं।** आप सिर्फ़ देख सकते हैं। जोड़ने या बदलने का अधिकार अपने एडमिनिस्ट्रेटर से माँगें।
:::

::: section design-previews
title: वे स्क्रीन जो अभी बन रही हैं
routes: /GuestBooking, /RoomDetails, /CancelledBookingDetails, /PaymentManagement
for: 
roles: 
designations: 
minutes: 1
source-hash: 0ad754e112fa7bee
confidence: ok

**यह स्क्रीन किस काम की है**
कुछ स्क्रीन अभी शुरुआती डिज़ाइन हैं। उनमें आपका डेटा नहीं, बल्कि पहले से तय नमूना दिखता है, और उनके बटन कुछ भी सेव नहीं करते।

1. अगर आपने नमूना मेहमान की जानकारी वाला **Review Your Booking**, **AC Deluxe Room** कमरे की जानकारी, **Cancelled Booking Details** या **Payment Management** खोला है, तो आप इन्हीं डिज़ाइन स्क्रीन में से किसी एक पर हैं।
2. वहाँ कोई डेटा न भरें, और वहाँ दिखे डेटा पर भरोसा न करें।
3. कमरा बुक करने के लिए **Book a Room** का इस्तेमाल करें। अपनी बुकिंग देखने, कैंसल करने या उन पर नज़र रखने के लिए **My Bookings / Cancellation** का इस्तेमाल करें।

## आम दिक्कतें

- **आँकड़े कभी नहीं बदलते।** यह नमूना है। ऊपर बताई गई असली स्क्रीन का इस्तेमाल करें।
:::
