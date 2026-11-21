---
module: guest-house
title: गेस्ट हाऊस
icon: bi-house-door
order: 112
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
source-hash: 1b36f90eb8520b48
confidence: ok

कंपनीच्या गेस्ट हाऊसमध्ये रूम बुक करा, गरज असेल तर त्याचे पैसे भरा, आणि तुमच्या सगळ्या बुकिंग एकाच ठिकाणी पाहा. गेस्ट हाऊसवर फ्रंट ऑफिस तुमचे चेक-इन करते, तुम्हाला रूम नंबर देते, चेक-आउट करते आणि बिल बनवते.

- book | icon: bi-calendar-check | color: #b45309
  title: 5 टप्प्यांत बुकिंग
  text: गेस्ट हाऊस, तारखा, रूम आणि पाहुण्याचे तपशील निवडा, मग खात्री करा.
- pay | icon: bi-wallet2 | color: #0d9488
  title: कर्मचाऱ्यांसाठी मोफत
  text: कंपनीचे कर्मचारी मोफत बुक करतात. डीलर आणि इतर लोक Razorpay ने ऑनलाइन पैसे भरतात.
- stay | icon: bi-door-open | color: #2563eb
  title: बाकीचे काम फ्रंट ऑफिस करते
  text: रूम नंबरसह चेक-इन, चेक-आउट, पेमेंट आणि बिल.
:::

::: section flow
title: बुकिंग कशी पुढे जाते, आणि कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: f66f0d447db14906
confidence: ok

प्रत्येक ओळ एक रोल आहे आणि प्रत्येक बॉक्स एक टप्पा आहे. तुमच्या ओळीवर "You" (तुम्ही) असे लिहिलेले असते. तिथे काय होते ते पाहण्यासाठी टप्पा टॅप करा.

- booker | type: lane | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: कर्मचारी किंवा डीलर
  sub: बुकिंग करणारी व्यक्ती
- frontoffice | type: lane | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: फ्रंट ऑफिस
  sub: रिसेप्शनिस्ट · ॲडमिन
- admin | type: lane | color: #dc2626 | roles: Admin, CorporateAdmin
  title: गेस्ट हाऊस ॲडमिन
  sub: रद्द करण्याच्या विनंत्या आणि मास्टर
- book | lane: booker | col: 1 | icon: bi-house-door | color: #b45309 | next: pay | screen: choose-guest-house
  title: निवडा आणि भरा
  status: Pending Payment
  text: गेस्ट हाऊस, तारखा आणि रूम निवडा, आणि पाहुण्याचे तपशील भरा.
- pay | lane: booker | col: 2 | icon: bi-wallet2 | color: #0d9488 | next: checkin | return: book | screen: review-pay
  title: तपासा आणि पैसे भरा
  status: Confirmed
  text: सगळे तपासा आणि **Complete Booking** दाबा. कर्मचाऱ्यांना काहीच भरावे लागत नाही. इतर लोक ऑनलाइन पैसे भरतात. पेमेंट फेल झाले तर नवीन बुकिंग सुरू करा.
- checkin | lane: frontoffice | col: 3 | icon: bi-box-arrow-in-right | color: #2563eb | next: checkout | screen: check-in
  title: चेक-इन
  status: CheckedIn
  text: फ्रंट ऑफिस ओळखपत्र तपासते आणि रूम नंबर देते.
- checkout | lane: frontoffice | col: 4 | icon: bi-box-arrow-right | color: #7c3aed | next: bill | screen: check-out
  title: चेक-आउट
  status: Completed
  text: फ्रंट ऑफिस बाकी असलेले पैसे घेते आणि पाहुण्याचे चेक-आउट करते. रूम परत रिकामी होते.
- bill | lane: frontoffice | col: 5 | icon: bi-receipt | color: #16a34a | screen: generate-bill
  title: बिल बनवा
  status: Billed
  text: फ्रंट ऑफिस जेवण आणि इतर खर्च जोडते आणि टॅक्स इन्व्हॉइस तयार करते.
- cancel | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: pay | screen: cancellation-requests
  title: रद्द करणे आणि रिफंड
  status: Cancelled
  text: बुकिंग करणारी व्यक्ती **My Bookings** मधून रद्द करण्याची विनंती करते. ॲडमिन ती मंजूर करतो किंवा नाकारतो. मंजूर झालेला रिफंड ज्या मार्गाने पैसे भरले होते त्याच मार्गावर मॅन्युअली परत केला जातो.
:::

::: section statuses
title: तुम्हाला दिसणारी प्रत्येक स्थिती
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 41c01f8852ce2a28
confidence: ok

- pending-payment | color: #d97706
  label: Pending Payment
  text: बुकिंग सेव्ह झाली आहे आणि ऑनलाइन पेमेंटची वाट पाहत आहे.
- confirmed | color: #16a34a
  label: Confirmed
  text: पैसे भरले आहेत, किंवा कर्मचाऱ्यासाठी मोफत आहे. रूम तुमच्यासाठी राखून ठेवली आहे.
- booking-checked-in | color: #2563eb
  label: CheckedIn
  text: पाहुणा आता रूममध्ये राहत आहे.
- completed | color: #0d9488
  label: Completed
  text: पाहुण्याने चेक-आउट केले आहे.
- cancelled | color: #dc2626
  label: Cancelled
  text: बुकिंग रद्द झाली आहे, किंवा तिचे पेमेंट झाले नाही.
- paid | color: #16a34a
  label: Paid
  text: पैसे मिळाले आहेत.
- pending | color: #d97706
  label: Pending
  text: पैसे अजून मिळालेले नाहीत.
- failed | color: #dc2626
  label: Failed
  text: ऑनलाइन पेमेंट झाले नाही. नवीन बुकिंग सुरू करा.
- complimentary | color: #0d9488
  label: Complimentary
  text: कंपनीच्या कर्मचाऱ्यासाठी मोफत बुकिंग. पैसे भरायचे नाहीत.
- pending-approval | color: #d97706
  label: Pending Approval
  text: तुमची रद्द करण्याची विनंती ॲडमिनकडे बाकी आहे.
- approved | color: #16a34a
  label: Approved
  text: ॲडमिनने रद्द करणे मंजूर केले. रूम मोकळी झाली.
- rejected | color: #475569
  label: Rejected
  text: ॲडमिनने रद्द करणे स्वीकारले नाही. तुमची बुकिंग चालू राहते.
- refund-manual | color: #7c3aed
  label: Cancelled - Refund to be Processed Manually
  text: रिफंड तुमच्या मूळ पेमेंटच्या मार्गावर मॅन्युअली परत केला जाईल.
- refund-completed | color: #16a34a
  label: Refund Completed
  text: रिफंड परत केला गेला आहे. तुमचे बँक किंवा कार्ड स्टेटमेंट तपासा.
- no-refund | color: #64748b
  label: Cancelled - No Refund Due
  text: रद्द करण्याच्या नियमांनुसार कोणताही रिफंड मिळत नाही.
- refund-failed | color: #dc2626
  label: Refund Failed
  text: रिफंड पूर्ण होऊ शकला नाही. टीम तुमच्याशी संपर्क करेल.
- available | color: #16a34a
  label: Available
  text: फ्रंट ऑफिस: रूम रिकामी आहे.
- reserved | color: #d97706
  label: Reserved / Booked
  text: फ्रंट ऑफिस: पाहुण्याने रूम बुक केली आहे पण अजून चेक-इन केलेले नाही.
- checked-in | color: #2563eb
  label: Checked-In
  text: फ्रंट ऑफिस: पाहुणा आता रूममध्ये आहे.
- not-billed | color: #64748b
  label: Not Billed
  text: पाहुण्याने चेक-आउट केले आहे पण अजून बिल बनलेले नाही.
- billed | color: #16a34a
  label: Billed
  text: या मुक्कामाचे बिल बनले आहे.
:::

::: section roles
title: कोण काय करते
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 0b5537897c5b9427
confidence: ok

तुमचे कार्ड ठळक दिसते. तुमचा रोल वापरतो त्याच स्क्रीन तुम्हाला दिसतात.

- booker | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: कर्मचारी किंवा डीलर
  - रूम बुक करा
  - तुमच्या बुकिंग पाहा आणि इन्व्हॉइस डाउनलोड करा
  - रद्द करण्याची विनंती करा आणि रिफंडचा मागोवा घ्या
- frontoffice | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: फ्रंट ऑफिस
  - पाहुण्यांचे चेक-इन करा आणि रूम नंबर द्या
  - पैसे घ्या आणि पाहुण्यांचे चेक-आउट करा
  - बिल तयार करा आणि प्रिंट करा
- admin | color: #dc2626 | roles: Admin, CorporateAdmin
  title: गेस्ट हाऊस ॲडमिन
  - रद्द करण्याच्या विनंत्या मंजूर करा किंवा नाकारा
  - गेस्ट हाऊस, रूम टाइप आणि रूम जोडा
:::

::: section faq
title: अडचणी आणि उत्तरे
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: c456cc4a4b5c2181
confidence: ok

- free-booking | popular: true
  q: माझ्या एकूण रकमेत ₹0.00 (Complimentary) का दिसते?
  a: कंपनीचे कर्मचारी गेस्ट हाऊसच्या रूम मोफत बुक करतात. डीलर आणि इतर लोक बुकिंग करताना ऑनलाइन पैसे भरतात.
- payment-failed | popular: true
  q: माझे पेमेंट फेल झाले, किंवा मी पेमेंटची विंडो बंद केली
  a: ती बुकिंग पूर्ण होऊ शकत नाही, आणि रूम मोकळी होते. **Book a Room** मधून नवीन बुकिंग सुरू करा. तुमच्या खात्यातून पैसे कापले गेले असतील तर ते आपोआप परत येतात.
- no-cancel
  q: मला Cancel Booking बटण दिसत नाही
  a: ते फक्त येणाऱ्या (upcoming) बुकिंगवर दिसते. चेक-इन झालेली बुकिंग ऑनलाइन रद्द करता येत नाही. कृपया फ्रंट ऑफिसशी बोला.
- refund-when
  q: मला माझा रिफंड कधी मिळेल?
  a: ॲडमिनने तुमचे रद्द करणे मंजूर केल्यावर, 2 कामाच्या दिवसांत रिफंड तुमच्या मूळ पेमेंटच्या मार्गावर मॅन्युअली परत केला जातो. त्याचा मागोवा घेण्यासाठी **Refund Status** उघडा.
- no-rooms
  q: मला "No rooms available" दिसते
  a: त्या तारखांसाठी सगळ्या रूम बुक आहेत. **Change dates** दाबा आणि दुसऱ्या तारखा, किंवा दुसरे गेस्ट हाऊस पाहा.
- early-checkin
  q: फ्रंट ऑफिस माझे चेक-इन करू शकत नाही
  a: चेक-इन फक्त चेक-इनच्या तारखेपासून मुक्काम संपेपर्यंत, आणि फक्त **Confirmed** बुकिंगसाठीच करता येते.
:::

::: section choose-guest-house
title: गेस्ट हाऊस आणि तुमच्या तारखा निवडा
routes: /GuestHouse, /GuestHouseBooking
for: 
roles: 
designations: 
minutes: 2
source-hash: 3d1914b34d0d67da
confidence: ok

**ही स्क्रीन कशासाठी आहे**
इथून बुकिंग सुरू होते. आधी **Choose Your Guest House** वर गेस्ट हाऊस निवडा, मग **Guest House Booking** (5 पैकी टप्पा 1) वर तुमच्या तारखा निवडा.

1. मेनूमध्ये **SDWA** उघडा आणि **Book a Room** टॅप करा. प्रत्येक कार्डवर एक गेस्ट हाऊस, त्याचा पत्ता आणि फोन नंबर दिसतो.
2. तुम्हाला हव्या असलेल्या गेस्ट हाऊसच्या कार्डवर **Select** दाबा. बटणावर गेस्ट हाऊसचे नाव दिसते.
3. **Guest House Booking** वर **Check-in Date** आणि **Check-in Time** निवडा.
4. **Check-out Date** आणि **Check-out Time** निवडा.
5. **Important Information** बॉक्स वाचा, मग **Check Availability** दाबा.

> Tip: एक वैध ओळखपत्र (आधार किंवा पासपोर्ट) सोबत ठेवा. चेक-इनच्या वेळी ते लागते.

## नेहमीच्या अडचणी

- **"Check-out must be after Check-in."** (चेक-आउट हे चेक-इनच्या नंतर असले पाहिजे) नंतरची चेक-आउट तारीख किंवा वेळ निवडा.
- **"No guest houses available".** (एकही गेस्ट हाऊस उपलब्ध नाही) **Retry** दाबा. तरीही हेच दिसत असेल तर SDWA ऑफिसशी संपर्क करा.
- **मला परत Choose Your Guest House वर पाठवले जाते.** आधी **Select** ने गेस्ट हाऊस निवडा, मग तारखा निवडा.
:::

::: section choose-room
title: रूम निवडा
routes: /Rooms
for: 
roles: 
designations: 
minutes: 2
source-hash: 9eefa1291c16d092
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Available Rooms** स्क्रीनवर (5 पैकी टप्पा 2) तुमच्या तारखांसाठी रिकाम्या असलेल्या रूम दिसतात.

1. रूमची कार्डे पाहा. प्रत्येक कार्डवर रूमचा प्रकार, एका रात्रीचा दर, किती रूम उपलब्ध आहेत आणि किती पाहुण्यांना परवानगी आहे हे दिसते.
2. फक्त त्या रूम पाहण्यासाठी वर एखादा रूम टाइप टॅप करा, किंवा **Search** मध्ये टाइप करा. **All** ने सगळ्या रूम दिसतात.
3. तुम्हाला लागणाऱ्या रूमची संख्या **Number of Rooms** मध्ये निवडा.
4. जास्तीचा बेड हवा असेल तर **Additional Bed** वर टिक करा आणि **No. of Additional Beds** निवडा. जास्तीच्या बेडचा एका रात्रीचा वेगळा दर असतो.
5. **Book Now** दाबा.

> Tip: **View Details** बटण अजून चालू नाही. रूमची सगळी माहिती कार्डवरच आहे.

## नेहमीच्या अडचणी

- **"No rooms available".** (रूम उपलब्ध नाहीत) त्या तारखांसाठी एकही रूम रिकामी नाही. **Change dates** दाबा.
- **"No matching rooms".** (जुळणाऱ्या रूम नाहीत) **Clear filters** दाबा.
- **"Up to ... rooms can be booked at a time for this room type."** (या रूम टाइपसाठी एका वेळी ... रूमपर्यंतच बुक करता येतात) बाकीच्या रूम दुसऱ्या बुकिंगमध्ये बुक करा.
:::

::: section guest-details
title: पाहुण्याचे तपशील भरा
routes: /GuestDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: eca96eb90f508346
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Guest Details** वर (5 पैकी टप्पा 3) कोण राहणार आहे ते तुम्ही सांगता. उजवीकडच्या **Booking Summary** मध्ये किंमत दिसते.

1. **Employee ID / Dealer Code** आणि **Guest Name** टाका.
2. **Company Name** लिस्टमध्ये असेल तर ते निवडा, आणि **Phone Number** व **Email ID** टाका. डीलरसाठी फोन आणि ईमेल डीलर नोंदणीतून आपोआप भरले जातात.
3. **Aadhaar / Passport Number** टाका आणि **Nationality** निवडा.
4. **Number of Persons**, **Number of Adults** आणि **Number of Children (below 12 years)** निवडा.
5. हवे असेल तर **Upload ID Proof (Optional)** मध्ये ओळखपत्र अपलोड करा.
6. **Booking Summary** तपासा: रूमचा खर्च, **GST (5%)** आणि **Total**. मग **Continue Review** दाबा.

| फील्ड | काय भरायचे |
|---|---|
| **Employee ID / Dealer Code** | तुमचा कर्मचारी ID किंवा डीलर कोड. भरणे आवश्यक. |
| **Guest Name** | जी व्यक्ती राहणार आहे तिचे नाव. भरणे आवश्यक. |
| **Phone Number** आणि **Email ID** | पाहुण्याचा संपर्क तपशील. भरणे आवश्यक. |
| **Nationality** | Indian, NRI किंवा Foreigner. भरणे आवश्यक. |
| **Company Name** | बिलासाठी कंपनी आणि तिचा GSTIN. ऐच्छिक. |
| **Upload ID Proof (Optional)** | एक PDF, JPG किंवा PNG फाइल, 5 MB पर्यंत. |

## नेहमीच्या अडचणी

- **"Employee ID / Dealer Code is required to continue."** (पुढे जाण्यासाठी Employee ID / Dealer Code आवश्यक आहे) प्रत्येक आवश्यक फील्ड भरा. मेसेजमध्ये राहिलेल्या फील्डचे नाव दिसते.
- **"Only PDF, JPG, or PNG files are allowed."** (फक्त PDF, JPG किंवा PNG फाइल चालतात) किंवा **"File must be 5 MB or less."** (फाइल 5 MB किंवा त्यापेक्षा लहान असावी) दुसरी फाइल निवडा किंवा लहान फोटो काढा.
- **मला रूम बदलायची आहे.** **Available Rooms** वर परत जाण्यासाठी **Back** दाबा.
:::

::: section review-pay
title: बुकिंग तपासा आणि पूर्ण करा
routes: /BookingPreview, /Payment
for: 
roles: 
designations: 
minutes: 3
source-hash: 0ae6b596dd70c921
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Review Your Booking** वर (5 पैकी टप्पा 4) तुम्ही सगळे तपासता. **Payment** वर (5 पैकी टप्पा 5) तुम्ही बुकिंगची खात्री करता. कंपनीच्या कर्मचाऱ्यांना काहीच भरावे लागत नाही. डीलर आणि इतर लोक Razorpay ने ऑनलाइन पैसे भरतात.

1. **Review Your Booking** वर **Booking Information**, **Booking Summary** आणि **Guest Information** तपासा.
2. काही बदलायचे असेल तर **Edit Details** दाबा. सगळे बरोबर असेल तर **Proceed to Payment** दाबा.
3. **Payment** वर **Booking Summary** तपासा. कर्मचाऱ्यांना **Complimentary (Employee)** आणि ₹0.00 एकूण रक्कम दिसते. इतरांना **Online Payment** दिसते.
4. **Complete Booking** दाबा. ऑनलाइन पेमेंटसाठी Razorpay ची विंडो उघडते. UPI, कार्ड, नेट बँकिंग किंवा वॉलेटने पैसे भरा.
5. **Booking Confirmed!** बॉक्समध्ये तुमचा **Booking ID** आणि **Payment Status** दिसतो. **View My Bookings** दाबा, किंवा परत बुक करण्यासाठी **New Booking** दाबा.

> Careful: पैसे भरताना पेमेंटची विंडो बंद करू नका किंवा पेज सोडू नका. पेमेंट फेल झाले किंवा तुम्ही विंडो बंद केली, तर ही बुकिंग पूर्ण होत नाही आणि तुम्हाला नवीन बुकिंग सुरू करावी लागते.

## नेहमीच्या अडचणी

- **"No booking details found".** (बुकिंगचे तपशील सापडले नाहीत) बुकिंग सुरू झाली नव्हती किंवा तिची वेळ संपली आहे. **Book a Room** मधून परत सुरू करा.
- **बटणावर "Payment Failed" दिसते.** नवीन बुकिंग सुरू करा. पैसे कापले गेले असतील तर ते आपोआप परत येतात.
- **"This room was just booked by someone else for the selected dates. Please try again."** (निवडलेल्या तारखांसाठी ही रूम आत्ताच दुसऱ्या कोणीतरी बुक केली. परत प्रयत्न करा.) रूम किंवा तारखा परत निवडा.
- **"Your room hold has expired..."** (तुमच्यासाठी राखून ठेवलेल्या रूमची वेळ संपली) पैसे भरायला खूप वेळ लागला. नवीन बुकिंग सुरू करा.
:::

::: section my-bookings
title: माझ्या बुकिंग पाहा
routes: /MyBookings
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-mb-cards] | 2 > [data-help=gh-mb-tabs] | 3 > [data-help=gh-mb-list] | 4 > [data-help=gh-mb-list] | 5 > [data-help=gh-mb-new]
minutes: 2
source-hash: eddb90512855180d
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**My Bookings** मध्ये तुम्ही केलेली प्रत्येक रूम बुकिंग दिसते, आणि वर आकडे दिसतात. मेनूमधून उघडा: **SDWA**, मग **My Bookings / Cancellation**.

1. वरची कार्डे पाहा: **Total Bookings**, **Upcoming**, **Completed** आणि **Cancelled**.
2. एक टॅब टॅप करा: **Upcoming**, **Completed** किंवा **Cancelled**.
3. प्रत्येक बुकिंगच्या कार्डवर रूम टाइप, बुकिंगची स्थिती, पेमेंटची स्थिती, **Booking ID**, गेस्ट हाऊस, तारखा, **Guests**, **Rooms** आणि **Total Amount** दिसतात.
4. बुकिंग उघडण्यासाठी **View Details** दाबा. येणाऱ्या बुकिंगवर रद्द करण्याची विनंती करण्यासाठी **Cancel Booking** दाबा. रद्द झालेल्या बुकिंगवर **Refund Status** दाबा.
5. दुसरी रूम बुक करण्यासाठी **New Booking** दाबा.

## नेहमीच्या अडचणी

- **"No upcoming bookings".** (येणारी एकही बुकिंग नाही) आज किंवा नंतरची तुमची एकही बुकिंग नाही. **Book a room** दाबा.
- **"Could not load bookings".** (बुकिंग लोड होऊ शकल्या नाहीत) तुमचे इंटरनेट तपासा आणि **Retry** दाबा.
- **बुकिंगवर Cancelled आणि Failed दिसते.** तिचे ऑनलाइन पेमेंट झाले नाही. परत बुक करा.
:::

::: section booking-details
title: एक बुकिंग पाहा आणि इन्व्हॉइस डाउनलोड करा
routes: /BookingDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 4c0fd3b25f0fb3dd
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Booking Details** मध्ये एका बुकिंगची सगळी माहिती दिसते: रूम, तारखा, पेमेंट, स्थिती आणि पाहुणा.

1. **My Bookings** वर एखाद्या बुकिंगवर **View Details** दाबा.
2. **Room Details** आणि **Check-in & Check-out** पाहा, त्यात रात्री, रूम आणि जास्तीच्या बेडची संख्या असते. **Room Number** ठरल्यावर तो दिसतो.
3. **Payment Details** पाहा: रूमचा खर्च, **GST (5%)**, **Total Amount**, **Payment Method** आणि **Payment Status**.
4. **Booking Status** पाहा. रद्द झालेल्या बुकिंगवर **Cancellation Status** पण दिसतो.
5. **Guest Details** पाहा. तुम्ही ओळखपत्र अपलोड केले असेल तर **View** किंवा **Download** दाबा.
6. टॅक्स इन्व्हॉइस PDF म्हणून सेव्ह करण्यासाठी **Download Invoice** दाबा.
7. परत जाण्यासाठी **Back to My Bookings** दाबा.

## नेहमीच्या अडचणी

- **Download Invoice बटण दिसत नाही.** पेमेंट **Paid** असेल आणि इन्व्हॉइस तयार असेल तेव्हाच ते दिसते.
- **"Booking not found."** (बुकिंग सापडली नाही) तुम्ही स्वतः केलेल्या बुकिंगच उघडू शकता.
:::

::: section cancel-booking
title: बुकिंग रद्द करा
routes: /CancelBooking
for: 
roles: 
designations: 
minutes: 3
source-hash: 58fac8bc78e4e38a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
रद्द करण्याची विनंती करण्यासाठी **Cancel Booking** वापरा. ॲडमिन तुमची विनंती तपासतो. ॲडमिन मंजूर करेपर्यंत तुमची बुकिंग चालू राहते.

1. **My Bookings** वर येणाऱ्या बुकिंगवर **Cancel Booking** दाबा.
2. **Important Notice** वाचा. चेक-इनला किती वेळ बाकी आहे त्यानुसार तुम्हाला किती टक्के रिफंड मिळेल ते त्यात दिसते.
3. **Cancellation Reason** खाली एक कारण निवडा.
4. **Refund Calculation** तपासा: **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)**, **Refund Amount**, **Refund Method** आणि **Estimated Time**.
5. **Continue Cancellation** दाबा. **Confirm Cancellation?** मध्ये **Cancel Booking** दाबा. तुमचा विचार बदलला असेल तर **Keep My Booking** दाबा.
6. **Cancellation Request Submitted** मध्ये तुमचा **Cancellation ID** आणि **Pending Approval** दिसते. **Refund Status**, **View Booking** किंवा **Go to My Bookings** दाबा.

> Careful: चेक-इन जितके जवळ, तितका रिफंड कमी. आधी **Cancellation Policy** बॉक्स वाचा.

## नेहमीच्या अडचणी

- **"Please select a reason for cancellation."** (कृपया रद्द करण्याचे कारण निवडा) आधी कारण निवडा.
- **"This booking is already checked in and cannot be cancelled online. Please contact the Front Office."** (या बुकिंगचे चेक-इन आधीच झाले आहे, ती ऑनलाइन रद्द करता येत नाही. कृपया फ्रंट ऑफिसशी संपर्क करा.) फ्रंट ऑफिसशी बोला.
- **"A cancellation request for this booking has already been submitted".** (या बुकिंगसाठी रद्द करण्याची विनंती आधीच पाठवली आहे) तुम्ही आधीच विनंती केली आहे. **View Refund Status** दाबा.
:::

::: section refund-status
title: माझ्या रिफंडचा मागोवा घ्या
routes: /RefundStatus
for: 
roles: 
designations: 
minutes: 2
source-hash: f4d86371946e0403
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Refund Status** मध्ये तुमचे रद्द करणे आणि रिफंड आता कुठपर्यंत आले आहे ते दिसते.

1. **My Bookings** वर **Cancelled** टॅब उघडा आणि **Refund Status** दाबा. विनंती पाठवल्यावर **Cancel Booking** मधूनही ते उघडता येते.
2. वर **Total Refund Amount**, **Cancellation ID**, **Refund Method**, **Target Refund Date**, **Transaction Ref.** आणि **Current Status** पाहा.
3. **Refund Breakdown** खाली **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)** आणि **Refund Amount** पाहा.
4. **Refund Tracking** खाली टप्पे पाहा: **Cancellation Requested**, मग **Pending Admin Approval**, **Approved by Admin** किंवा **Cancellation Rejected**, मग रिफंड.
5. ठरलेल्या तारखेपर्यंत पैसे मिळाले नाहीत, तर नारिंगी बॉक्समध्ये दिलेला संदर्भ (reference) सांगून सपोर्टशी संपर्क करा.
6. **My Bookings** वर परत जाण्यासाठी **Back** दाबा.

## नेहमीच्या अडचणी

- **माझी बुकिंग Cancelled टॅबमध्ये नाही.** ॲडमिनने मंजूर केल्यावरच ती तिथे जाते. तोपर्यंत बुकिंगची **Cancel Booking** स्क्रीन उघडा आणि **View Refund Status** दाबा.
- **"Cancellation Rejected".** (रद्द करणे नाकारले) कारण वाचा. तुमची बुकिंग चालू राहते.
- **"No Refund Due".** (रिफंड मिळणार नाही) रद्द करण्याच्या नियमांनुसार पैसे परत मिळत नाहीत, उदा. कर्मचाऱ्याच्या मोफत बुकिंगसाठी.
:::

::: section front-office
title: फ्रंट ऑफिसवर सगळ्या रूम पाहा
routes: /FrontOffice
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-fo-cards] | 2 > [data-help=gh-fo-tabs] | 3 > [data-help=gh-fo-search] | 4 > [data-help=gh-fo-bill]
minutes: 2
source-hash: c62b602840a09360
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Front Office** स्क्रीनवर प्रत्येक रूम दिसते: ती रिकामी आहे, बुक आहे की त्यात कोणी राहत आहे, आणि ती कोणाकडे आहे. मेनूमधून उघडा: **SDWA**, मग **Front Office**. रिसेप्शनिस्टला फक्त त्याच्या स्वतःच्या राज्यातले गेस्ट हाऊस दिसते.

1. वरची कार्डे पाहा: **Total Rooms**, **Available**, **Reserved** आणि **Checked-In**.
2. एक टॅब टॅप करा: **All**, **Available**, **Reserved / Booked** किंवा **Checked-In**.
3. **Search rooms...** मध्ये रूम नंबर, बुकिंग ID किंवा पाहुण्याचे नाव टाइप करा, किंवा **All Guest Houses** मध्ये गेस्ट हाऊस निवडा. प्रत्येक ओळीत रूम, तिची स्थिती, बुकिंग आणि पाहुणा, तारखा आणि कृती (actions) दिसतात.
4. चेक-आउट झालेल्या पाहुण्याचे बिल बनवण्यासाठी **Generate Bill** दाबा. रूमची लिस्ट परत लोड करण्यासाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **"You do not have permission to access the Front Office."** (तुम्हाला फ्रंट ऑफिस उघडण्याची परवानगी नाही) तुमच्या ॲडमिनिस्ट्रेटरकडे फ्रंट ऑफिसची परवानगी मागा.
- **"No rooms found".** (एकही रूम सापडली नाही) **All** आणि **All Guest Houses** निवडा, आणि सर्च रिकामा करा.
:::

::: section check-in
title: पाहुण्याचे चेक-इन करा
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 3
source-hash: 4d3a8c4aabfb6c8a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
पाहुणा आल्यावर त्याचे चेक-इन करा आणि त्याला रूम नंबर द्या.

1. **Front Office** वर **Reserved / Booked** टॅप करा आणि बुकिंग शोधा.
2. **Actions** कॉलममध्ये **Check-In** दाबा.
3. **Check-In** बॉक्समध्ये **Booking ID**, **Guest Name**, **Room Type** आणि **Guest House** तपासा. ओळखपत्र जोडलेले असेल तर ते पाहण्यासाठी **View** दाबा.
4. **Number of Persons** आणि **Number of Extra Beds** तपासा, आणि गरज असेल तर दुरुस्त करा.
5. **Assign Physical Room(s)** खाली द्यायचे रूम नंबर टॅप करा. जितक्या रूम बुक केल्या होत्या, बरोबर तितक्याच निवडा.
6. **Confirm Check-In** दाबा. रूमवर आता **Checked-In** दिसते.

> Careful: चेक-इन करण्याआधी पाहुण्याचे ओळखपत्र (आधार किंवा पासपोर्ट) तपासा.

## नेहमीच्या अडचणी

- **"This booking's check-in date has not arrived yet."** (या बुकिंगची चेक-इनची तारीख अजून आलेली नाही) चेक-इनच्या तारखेला किंवा त्यानंतर चेक-इन करा.
- **"This booking's stay period has passed and it can no longer be checked in."** (या बुकिंगचा मुक्कामाचा काळ संपला आहे, आता चेक-इन करता येणार नाही) मुक्कामाच्या तारखा संपल्या आहेत.
- **"Please select exactly ... physical room(s) before confirming check-in."** (चेक-इनची खात्री करण्याआधी बरोबर ... रूम निवडा) जास्त किंवा कमी रूम नंबर टॅप करा.
- **"No physical rooms are currently free for the booked stay period."** (बुक केलेल्या मुक्कामाच्या काळासाठी आता एकही रूम रिकामी नाही) बुकिंग करणाऱ्या व्यक्तीशी संपर्क करा किंवा दुसरा रूम टाइप निवडा.
- **"These rooms were just assigned to another overlapping stay. Please refresh and try again."** (या रूम आत्ताच त्याच काळातल्या दुसऱ्या मुक्कामाला दिल्या गेल्या. रिफ्रेश करून परत प्रयत्न करा.) **Refresh** दाबा आणि दुसऱ्या रूम निवडा.
:::

::: section check-out
title: पाहुण्याचे चेक-आउट करा आणि पैसे घ्या
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 2
source-hash: cf102cb774da6ac0
confidence: ok

**ही स्क्रीन कशासाठी आहे**
पाहुणा निघताना पैसे मिळाले आहेत याची खात्री करा, मग त्याचे चेक-आउट करा. रूम परत रिकामी होते.

1. **Front Office** वर **Checked-In** टॅप करा आणि पाहुणा शोधा.
2. **Actions** कॉलममध्ये **Check-Out** दाबा.
3. **Check-Out & Payment** मध्ये **Room Number**, तारखा, **Number of Persons**, **Extra Beds**, **Total Amount**, **Payment Method** आणि **Payment Status** तपासा.
4. **Payment Status** मध्ये **Pending** असेल तर पैसे घ्या आणि **Confirm Payment Received** दाबा.
5. **Paid** दिसल्यावर **Confirm Check-Out** दाबा. रूमवर परत **Available** दिसते.
6. आता **Generate Bill** ने बिल बनवा.

## नेहमीच्या अडचणी

- **"Payment must be received before check-out."** (चेक-आउटच्या आधी पैसे मिळाले पाहिजेत) आधी **Confirm Payment Received** दाबा.
- **"Check-Out is only allowed for a booking that is currently checked in."** (आता चेक-इन असलेल्या बुकिंगचेच चेक-आउट करता येते) पाहुण्याचे चेक-इन झालेले नाही. **Status** तपासा.
:::

::: section bills
title: पूर्ण झालेले मुक्काम आणि बिले शोधा
routes: /GenerateBill
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-gb-filters] | 2 > [data-help=gh-gb-go] | 3 > [data-help=gh-gb-tabs] | 4 > [data-help=gh-gb-find]
minutes: 2
source-hash: 5194cb8d5292259e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Generate Bill** स्क्रीनवर चेक-आउट झालेले मुक्काम आणि आधीच बनलेली बिले दिसतात. मेनूमधून (**SDWA**, मग **Generate Bill**) किंवा **Front Office** मधून उघडा.

1. **Filters** खाली **From Date** आणि **To Date** निवडा. मुक्कामासाठी या चेक-आउटच्या तारखा असतात आणि बिलांसाठी बिलाच्या तारखा.
2. **Search** दाबा. **Today** ने फक्त आजचे दिसते. **Refresh** ने लिस्ट परत लोड होते.
3. चेक-आउट झालेले मुक्काम पाहण्यासाठी **Completed Bookings** टॅप करा, किंवा आधीच बनलेली बिले पाहण्यासाठी **Generated Bills** टॅप करा.
4. सर्च बॉक्समध्ये बुकिंग ID किंवा पाहुण्याचे नाव टाइप करा. मुक्कामावर **Bill Status** मध्ये **Not Billed** किंवा **Billed** दिसते. **Generate Bill** किंवा **View Bill** दाबा.

## नेहमीच्या अडचणी

- **"From Date cannot be after To Date."** (From Date ही To Date नंतरची असू शकत नाही) तारखा बदला.
- **एखादा मुक्काम लिस्टमध्ये नाही.** फक्त चेक-आउट झालेले मुक्काम, त्यांच्या चेक-आउटच्या तारखेनुसार दिसतात. तारखांचा काळ वाढवा.
:::

::: section generate-bill
title: मुक्कामाचे बिल बनवा
routes: /GenerateBill
for: 
roles: 
designations: 
minutes: 4
source-hash: 71ab93bc25bfd36f
confidence: ok

**ही स्क्रीन कशासाठी आहे**
चेक-आउट झाल्यावर मुक्कामाचे टॅक्स इन्व्हॉइस बनवा. प्रत्येक मुक्कामाचे बिल फक्त एकदाच बनवता येते.

1. **Completed Bookings** वर **Not Billed** दिसणाऱ्या मुक्कामावर **Generate Bill** दाबा. **New Bill** स्क्रीन उघडते.
2. **Booking Summary** तपासा: **Booking**, **Guest**, **Company Name**, **GSTIN**, **Guest House**, **Room** आणि **Stay**.
3. **Bill Items** खाली रूमची ओळ आधीच भरलेली असते. **Food Bill** आणि **Additional Beds** च्या रकमा असतील तर टाका.
4. लॉन्ड्री किंवा इतर खर्चासाठी **Add Charge** दाबा. वर्णन, **Qty**, **Rate**, **CGST %** आणि **SGST %** टाका. **Remove** ने जोडलेली ओळ काढली जाते.
5. **Totals, Discount & Payment** खाली गरज असेल तर **Discount**, **Advance Payment** आणि **Round Off** टाका. **Payment Method** निवडा, **Payment received** वर टिक करा आणि **Remarks** लिहा.
6. **Balance Payable** तपासा, मग **Generate Bill** दाबा. नवीन बिल **Generated Bills** टॅबवर दिसते.
7. इन्व्हॉइस पाहण्यासाठी **View** दाबा, मग **Download PDF** किंवा **Print** दाबा. लिस्टवरचे **PDF** बटण ते थेट डाउनलोड करते.

> Careful: **Generate Bill** दाबण्याआधी प्रत्येक रक्कम तपासा. त्याच मुक्कामाचे बिल परत बनवता येत नाही.

| फील्ड | काय भरायचे |
|---|---|
| **Food Bill** | मुक्कामात दिलेल्या जेवणाची संख्या आणि दर. |
| **Add Charge** | इतर कोणताही खर्च, त्याच्या स्वतःच्या CGST % आणि SGST % सह. |
| **Advance Payment** | या मुक्कामासाठी आधीच भरलेले पैसे. माहीत असेल तर हे आपोआप भरले जाते. |
| **Payment Method** | UPI, Card, Net Banking किंवा Cash. कर्मचाऱ्याच्या मोफत मुक्कामासाठी Complimentary पण दिसते. |

## नेहमीच्या अडचणी

- **"Discount cannot exceed Grand Total."** (Discount हा Grand Total पेक्षा जास्त असू शकत नाही) किंवा **"Advance Payment cannot exceed Grand Total."** (Advance Payment हा Grand Total पेक्षा जास्त असू शकत नाही) रक्कम कमी करा.
- **"Balance Payable cannot be negative."** (Balance Payable उणे असू शकत नाही) **Discount**, **Advance Payment** आणि **Round Off** तपासा.
- **"A bill has already been generated for this booking".** (या बुकिंगचे बिल आधीच बनले आहे) त्याऐवजी **View Bill** ने ते उघडा.
:::

::: section bill-list
title: बनलेली सगळी बिले पाहा
routes: /BillList
for: 
roles: 
designations: 
minutes: 1
source-hash: 863b55025a37bbd6
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Bill List** स्क्रीनवर बनलेले प्रत्येक इन्व्हॉइस दिसते, सगळ्यात नवीन आधी. हीच बिले **Generate Bill** च्या **Generated Bills** टॅबवर पण दिसतात.

1. टेबल पाहा: **Bill #**, **Booking**, **Guest**, **Guest House**, **Room**, **Bill Date**, तारखा, **Total**, **Balance** आणि **Payment**.
2. इन्व्हॉइस उघडण्यासाठी **View** दाबा. त्यात **Download PDF** किंवा **Print** दाबा.
3. इन्व्हॉइस लगेच डाउनलोड करण्यासाठी **PDF** दाबा.
4. आणखी ओळी पाहण्यासाठी **Show more** दाबा.
5. नवीन बिल बनवण्यासाठी **Generate Bill** दाबा, किंवा परत लोड करण्यासाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **"No bills yet".** (अजून एकही बिल नाही) अजून एकही बिल बनलेले नाही. **Generate Bill** मधून एक बनवा.
:::

::: section cancellation-requests
title: रद्द करण्याच्या विनंत्या मंजूर करा किंवा नाकारा
routes: /GuestHouseCancellations
for: 
roles: 
designations: 
minutes: 3
source-hash: abf61f8cc73c49f2
confidence: ok

**ही स्क्रीन कशासाठी आहे**
कोणी बुकिंग रद्द करण्याची विनंती केली की ती इथे येते. मेनूमधून उघडा: **SDWA**, मग **Cancellation Requests**.

1. एक टॅब टॅप करा: **Pending Approval**, **Approved**, **Rejected** किंवा **All**.
2. **Search requests...** मध्ये टाइप करा, किंवा **All Guest Houses** मध्ये गेस्ट हाऊस निवडा.
3. ओळ वाचा: बुकिंग, पाहुणा, मुक्काम, भरलेली रक्कम (**Paid**), **Refund**, **Reason** आणि विनंती कधी केली (**Requested**).
4. स्वीकारण्यासाठी **Approve** दाबा, बॉक्स वाचा आणि **Approve Cancellation** दाबा. बुकिंग रद्द होते आणि रूम मोकळी होते.
5. नाकारण्यासाठी **Reject** दाबा, **Reason for rejection** लिहा आणि **Reject Request** दाबा. बुकिंग चालू राहते.
6. रिफंड मंजूर केल्यावर, तो 2 कामाच्या दिवसांत मूळ पेमेंटच्या मार्गावर मॅन्युअली परत करा.

> Careful: ॲप स्वतः रिफंड पाठवत नाही. कोणीतरी तो ॲपच्या बाहेर परत केला पाहिजे.

## नेहमीच्या अडचणी

- **"You do not have permission to review cancellation requests."** (तुम्हाला रद्द करण्याच्या विनंत्या तपासण्याची परवानगी नाही) तुमच्या ॲडमिनिस्ट्रेटरकडे ही परवानगी मागा.
- **"The guest is checked in. Check the guest out instead of cancelling."** (पाहुण्याचे चेक-इन झाले आहे. रद्द करण्याऐवजी पाहुण्याचे चेक-आउट करा.) **Front Office** वर **Check-Out** वापरा.
- **Reject बटण दिसत नाही.** या विनंतीचा रिफंड आधीच चालू आहे.
:::

::: section guest-house-master
title: गेस्ट हाऊस, रूम टाइप आणि रूम जोडा
routes: /GuestHouseMaster
for: 
roles: 
designations: 
minutes: 4
source-hash: 7f6f2fee8bd4e156
confidence: ok

**ही स्क्रीन कशासाठी आहे**
लोक काय बुक करू शकतात ते ठरवण्यासाठी ॲडमिन **Guest House & Room Master** वापरतात. मेनूमधून उघडा: **SDWA**, मग **Guest House**. आधी गेस्ट हाऊस जोडा, मग रूम टाइप, मग रूम.

1. **Guest House** टॅबवर **Name** टाका, आणि माहीत असतील तर **Address**, **Phone Number**, **State** आणि **Description** टाका. फोटो जोडण्यासाठी **Upload Image** दाबा.
2. **Room Type** टॅबवर **Room Type Name** टाका, उदा. AC Deluxe Room.
3. **Rooms** टॅबवर **Guest House** आणि **Room Type (Master)** निवडा, मग **Room Type**, **Room Number**, **No. of Rooms** आणि **Rate (₹)** टाका.
4. स्विच **Active** किंवा **Inactive** वर ठेवा आणि **Save** दाबा. **Cancel** ने फॉर्म रिकामा होतो.
5. **Guest House** किंवा **Rooms** टॅबवर एकाच वेळी अनेक जोडण्यासाठी **Download Template** दाबा, ते भरा, मग **Upload File** दाबा.
6. टेबलमध्ये सर्च बॉक्स आणि **All Status** वापरा. स्थिती बदलण्यासाठी **Active** किंवा **Inactive** टॅप करा, एडिट करण्यासाठी पेन्सिल, किंवा डिलीट करण्यासाठी डस्टबिन टॅप करा.

> Careful: डिलीट केलेले परत आणता येत नाही. काही काळ बुकिंग थांबवायची असेल तर त्याऐवजी **Inactive** करा. Inactive गेस्ट हाऊस बुकिंगसाठी दिसत नाहीत.

| फील्ड | काय भरायचे |
|---|---|
| **Name** | गेस्ट हाऊसचे नाव. भरणे आवश्यक. |
| **State** | गेस्ट हाऊस कोणत्या राज्यात आहे. त्या राज्याचे रिसेप्शनिस्ट ते सांभाळतात. |
| **No. of Rooms** | या प्रकारच्या किती रूम. पूर्ण संख्या, 1 किंवा जास्त. भरणे आवश्यक. |
| **Rate (₹)** | एका रात्रीचा दर. भरणे आवश्यक. |

## नेहमीच्या अडचणी

- **"No. of Rooms must be a positive whole number."** (No. of Rooms ही 1 किंवा त्यापेक्षा जास्त पूर्ण संख्या असावी) 1 किंवा जास्त टाका.
- **"Rate must be a valid amount."** (Rate योग्य रक्कम असावी) एक आकडा टाका, उदा. 1200.
- **मला टेबल दिसतात पण फॉर्म दिसत नाही.** तुम्ही फक्त पाहू शकता. जोडण्याचे किंवा बदलण्याचे अधिकार तुमच्या ॲडमिनिस्ट्रेटरकडे मागा.
:::

::: section design-previews
title: अजून तयार होत असलेल्या स्क्रीन
routes: /GuestBooking, /RoomDetails, /CancelledBookingDetails, /PaymentManagement
for: 
roles: 
designations: 
minutes: 1
source-hash: 0ad754e112fa7bee
confidence: ok

**ही स्क्रीन कशासाठी आहे**
काही स्क्रीन अजून सुरुवातीचे डिझाइन आहेत. त्यात तुमचा डेटा नसतो, ठरलेला नमुना मजकूर दिसतो, आणि त्यांची बटणे काहीही सेव्ह करत नाहीत.

1. तुम्ही नमुना पाहुण्याच्या तपशीलासह **Review Your Booking**, **AC Deluxe Room** रूमचे तपशील, **Cancelled Booking Details** किंवा **Payment Management** उघडले असेल, तर तुम्ही यापैकी एका डिझाइन स्क्रीनवर आहात.
2. तिथे कोणताही डेटा टाकू नका, आणि तिथे दिसणाऱ्या डेटावर विश्वास ठेवू नका.
3. रूम बुक करण्यासाठी **Book a Room** वापरा. तुमच्या बुकिंग पाहण्यासाठी, रद्द करण्यासाठी किंवा त्यांचा मागोवा घेण्यासाठी **My Bookings / Cancellation** वापरा.

## नेहमीच्या अडचणी

- **आकडे कधीच बदलत नाहीत.** हा नमुना मजकूर आहे. वर सांगितलेल्या खऱ्या स्क्रीन वापरा.
:::
