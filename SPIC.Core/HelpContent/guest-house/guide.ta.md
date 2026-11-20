---
module: guest-house
title: கெஸ்ட் ஹவுஸ்
icon: bi-house-door
order: 112
language: ta
category: வசதிகள்
note: புதிய வழிகாட்டி
meta-source-hash: a51949592e8c1854
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 1b36f90eb8520b48
confidence: ok

கம்பெனி கெஸ்ட் ஹவுஸில் ஒரு அறையை புக் செய்யலாம், தேவைப்பட்டால் அதற்குப் பணம் கட்டலாம், உங்கள் எல்லா புக்கிங்குகளையும் ஒரே இடத்தில் பார்க்கலாம். கெஸ்ட் ஹவுஸில், Front Office உங்களை check-in செய்து, அறை எண் தந்து, check-out செய்து, பில் போடுவார்கள்.

- book | icon: bi-calendar-check | color: #b45309
  title: 5 படிகளில் புக் செய்யுங்கள்
  text: கெஸ்ட் ஹவுஸ், தேதிகள், அறை, விருந்தினர் விவரங்களைத் தேர்வு செய்து, பிறகு உறுதி செய்யுங்கள்.
- pay | icon: bi-wallet2 | color: #0d9488
  title: ஊழியர்களுக்கு இலவசம்
  text: கம்பெனி ஊழியர்கள் இலவசமாக புக் செய்யலாம். டீலர்களும் மற்றவர்களும் Razorpay மூலம் ஆன்லைனில் பணம் கட்டுவார்கள்.
- stay | icon: bi-door-open | color: #2563eb
  title: மீதியை Front Office பார்த்துக்கொள்ளும்
  text: அறை எண்களுடன் check-in, check-out, பணம் வாங்குதல், பில்.
:::

::: section flow
title: ஒரு புக்கிங் எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: f66f0d447db14906
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- booker | type: lane | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: ஊழியர் அல்லது டீலர்
  sub: புக் செய்பவர்
- frontoffice | type: lane | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: Front Office
  sub: Receptionist · Admin
- admin | type: lane | color: #dc2626 | roles: Admin, CorporateAdmin
  title: கெஸ்ட் ஹவுஸ் admin
  sub: ரத்து கோரிக்கைகளும் மாஸ்டர்களும்
- book | lane: booker | col: 1 | icon: bi-house-door | color: #b45309 | next: pay | screen: choose-guest-house
  title: தேர்வு செய்து நிரப்புதல்
  status: Pending Payment
  text: கெஸ்ட் ஹவுஸ், தேதிகள், அறையைத் தேர்வு செய்து, விருந்தினர் விவரங்களை நிரப்புங்கள்.
- pay | lane: booker | col: 2 | icon: bi-wallet2 | color: #0d9488 | next: checkin | return: book | screen: review-pay
  title: சரிபார்த்துப் பணம் கட்டுதல்
  status: Confirmed
  text: எல்லாவற்றையும் சரிபார்த்து **Complete Booking** அழுத்துங்கள். ஊழியர்கள் எதுவும் கட்ட வேண்டாம். மற்றவர்கள் ஆன்லைனில் கட்டுவார்கள். பணம் கட்டுவது தோல்வியடைந்தால், புதிய புக்கிங் தொடங்குங்கள்.
- checkin | lane: frontoffice | col: 3 | icon: bi-box-arrow-in-right | color: #2563eb | next: checkout | screen: check-in
  title: Check-in செய்தல்
  status: CheckedIn
  text: Front Office அடையாளச் சான்றைச் சரிபார்த்து அறை எண்களைத் தருவார்கள்.
- checkout | lane: frontoffice | col: 4 | icon: bi-box-arrow-right | color: #7c3aed | next: bill | screen: check-out
  title: Check-out செய்தல்
  status: Completed
  text: Front Office நிலுவையில் உள்ள பணத்தை வாங்கி, விருந்தினரை check-out செய்வார்கள். அறை காலியாகிவிடும்.
- bill | lane: frontoffice | col: 5 | icon: bi-receipt | color: #16a34a | screen: generate-bill
  title: பில் போடுதல்
  status: Billed
  text: Front Office உணவு மற்றும் மற்ற கட்டணங்களைச் சேர்த்து tax invoice-ஐ உருவாக்குவார்கள்.
- cancel | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: pay | screen: cancellation-requests
  title: ரத்து செய்து பணத்தைத் திருப்புதல்
  status: Cancelled
  text: புக் செய்தவர் **My Bookings**-ல் இருந்து ரத்து செய்யக் கேட்பார். Admin ஒப்புதல் தருவார் அல்லது நிராகரிப்பார். ஒப்புதல் கிடைத்த refund, முதலில் பணம் கட்டிய அதே வழிக்குக் கையால் (manual-ஆக) அனுப்பப்படும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 41c01f8852ce2a28
confidence: ok

- pending-payment | color: #d97706
  label: Pending Payment
  text: புக்கிங் சேமிக்கப்பட்டது, ஆன்லைன் பணத்துக்காகக் காத்திருக்கிறது.
- confirmed | color: #16a34a
  label: Confirmed
  text: பணம் கட்டப்பட்டது, அல்லது ஊழியருக்கு இலவசம். அறை உங்களுக்காக வைக்கப்பட்டுள்ளது.
- booking-checked-in | color: #2563eb
  label: CheckedIn
  text: விருந்தினர் இப்போது அறையில் தங்கியிருக்கிறார்.
- completed | color: #0d9488
  label: Completed
  text: விருந்தினர் check-out செய்துவிட்டார்.
- cancelled | color: #dc2626
  label: Cancelled
  text: புக்கிங் ரத்து செய்யப்பட்டது, அல்லது அதற்கான பணம் கட்டுவது நடக்கவில்லை.
- paid | color: #16a34a
  label: Paid
  text: பணம் வந்து சேர்ந்தது.
- pending | color: #d97706
  label: Pending
  text: பணம் இன்னும் வரவில்லை.
- failed | color: #dc2626
  label: Failed
  text: ஆன்லைன் பணம் கட்டுவது நடக்கவில்லை. புதிய புக்கிங் தொடங்குங்கள்.
- complimentary | color: #0d9488
  label: Complimentary
  text: கம்பெனி ஊழியருக்கான இலவச புக்கிங். பணம் கட்ட வேண்டாம்.
- pending-approval | color: #d97706
  label: Pending Approval
  text: உங்கள் ரத்து கோரிக்கை admin-க்காகக் காத்திருக்கிறது.
- approved | color: #16a34a
  label: Approved
  text: Admin ரத்து செய்ய ஒப்புதல் தந்துவிட்டார். அறை விடுவிக்கப்பட்டது.
- rejected | color: #475569
  label: Rejected
  text: Admin ரத்து செய்வதை ஏற்கவில்லை. உங்கள் புக்கிங் தொடர்ந்து இருக்கும்.
- refund-manual | color: #7c3aed
  label: Cancelled - Refund to be Processed Manually
  text: Refund, நீங்கள் முதலில் பணம் கட்டிய அதே வழிக்குக் கையால் அனுப்பப்படும்.
- refund-completed | color: #16a34a
  label: Refund Completed
  text: Refund அனுப்பப்பட்டுவிட்டது. உங்கள் வங்கி அல்லது கார்டு ஸ்டேட்மென்ட்டைப் பாருங்கள்.
- no-refund | color: #64748b
  label: Cancelled - No Refund Due
  text: ரத்து விதிமுறைப்படி refund எதுவும் இல்லை.
- refund-failed | color: #dc2626
  label: Refund Failed
  text: Refund-ஐ முடிக்க முடியவில்லை. குழுவினர் உங்களைத் தொடர்புகொள்வார்கள்.
- available | color: #16a34a
  label: Available
  text: Front Office: அறை காலியாக உள்ளது.
- reserved | color: #d97706
  label: Reserved / Booked
  text: Front Office: ஒரு விருந்தினர் அறையை புக் செய்துள்ளார், இன்னும் check-in செய்யவில்லை.
- checked-in | color: #2563eb
  label: Checked-In
  text: Front Office: ஒரு விருந்தினர் இப்போது அறையில் இருக்கிறார்.
- not-billed | color: #64748b
  label: Not Billed
  text: விருந்தினர் check-out செய்துவிட்டார், ஆனால் இன்னும் பில் போடவில்லை.
- billed | color: #16a34a
  label: Billed
  text: இந்தத் தங்குதலுக்கான பில் போடப்பட்டுவிட்டது.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 0b5537897c5b9427
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- booker | color: #b45309 | roles: Dealer, RM, RMD, SMD, SMM, MDO, MO, JMDO, AVP, Director
  title: ஊழியர் அல்லது டீலர்
  - அறை புக் செய்வது
  - உங்கள் புக்கிங்குகளைப் பார்ப்பது, invoice பதிவிறக்குவது
  - ரத்து செய்யக் கேட்பது, refund-ஐப் பின்தொடர்வது
- frontoffice | color: #2563eb | roles: Admin, CorporateAdmin | designations: Receptionist
  title: Front Office
  - விருந்தினர்களை check-in செய்து அறை எண்கள் தருவது
  - பணம் வாங்கி விருந்தினர்களை check-out செய்வது
  - பில் போட்டு அச்சிடுவது
- admin | color: #dc2626 | roles: Admin, CorporateAdmin
  title: கெஸ்ட் ஹவுஸ் admin
  - ரத்து கோரிக்கைகளுக்கு ஒப்புதல் தருவது அல்லது நிராகரிப்பது
  - கெஸ்ட் ஹவுஸ், அறை வகைகள், அறைகளைச் சேர்ப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: c456cc4a4b5c2181
confidence: ok

- free-booking | popular: true
  q: என் மொத்தத் தொகை ஏன் ₹0.00 (Complimentary) என்று காட்டுகிறது?
  a: கம்பெனி ஊழியர்கள் கெஸ்ட் ஹவுஸ் அறைகளை இலவசமாக புக் செய்யலாம். டீலர்களும் மற்றவர்களும் புக் செய்யும்போது ஆன்லைனில் பணம் கட்டுவார்கள்.
- payment-failed | popular: true
  q: என் பணம் கட்டுவது தோல்வியடைந்தது, அல்லது பணம் கட்டும் விண்டோவை மூடிவிட்டேன்
  a: அந்த புக்கிங் நடக்காது, அறை விடுவிக்கப்படும். **Book a Room**-ல் இருந்து புதிய புக்கிங் தொடங்குங்கள். உங்கள் கணக்கில் இருந்து பணம் எடுக்கப்பட்டிருந்தால், அது தானாகவே திருப்பித் தரப்படும்.
- no-cancel
  q: எனக்கு Cancel Booking பட்டன் தெரியவில்லை
  a: அது வரவிருக்கும் புக்கிங்குகளில் மட்டுமே தெரியும். Check-in ஆன தங்குதலை ஆன்லைனில் ரத்து செய்ய முடியாது. Front Office-ஐத் தொடர்புகொள்ளுங்கள்.
- refund-when
  q: என் refund எப்போது கிடைக்கும்?
  a: Admin உங்கள் ரத்துக்கு ஒப்புதல் தந்த பிறகு, 2 வேலை நாட்களுக்குள் refund நீங்கள் முதலில் பணம் கட்டிய வழிக்கே கையால் அனுப்பப்படும். அதைப் பின்தொடர **Refund Status**-ஐத் திறங்கள்.
- no-rooms
  q: "No rooms available" என்று வருகிறது
  a: அந்தத் தேதிகளுக்கு எல்லா அறைகளும் புக் ஆகிவிட்டன. **Change dates** அழுத்தி வேறு தேதிகளில் முயற்சி செய்யுங்கள், அல்லது வேறு கெஸ்ட் ஹவுஸைப் பாருங்கள்.
- early-checkin
  q: Front Office-ஆல் என்னை check-in செய்ய முடியவில்லை
  a: Check-in தேதியிலிருந்து தங்குதல் முடியும் வரை மட்டுமே check-in செய்ய முடியும், அதுவும் **Confirmed** புக்கிங்குக்கு மட்டுமே.
:::

::: section choose-guest-house
title: கெஸ்ட் ஹவுஸையும் தேதிகளையும் தேர்வு செய்வது
routes: /GuestHouse, /GuestHouseBooking
for: 
roles: 
designations: 
minutes: 2
source-hash: 3d1914b34d0d67da
confidence: ok

**இந்தத் திரை எதற்கு**
புக்கிங் இங்கேதான் தொடங்கும். முதலில் **Choose Your Guest House**-ல் கெஸ்ட் ஹவுஸைத் தேர்வு செய்யுங்கள், பிறகு **Guest House Booking**-ல் (5-ல் படி 1) உங்கள் தேதிகளைத் தேர்வு செய்யுங்கள்.

1. மெனுவில் **SDWA**-ஐத் திறந்து **Book a Room** தட்டுங்கள். ஒவ்வொரு கார்டிலும் ஒரு கெஸ்ட் ஹவுஸும் அதன் முகவரியும் போன் எண்ணும் தெரியும்.
2. உங்களுக்கு வேண்டிய கெஸ்ட் ஹவுஸின் கார்டில் **Select** அழுத்துங்கள். பட்டனில் கெஸ்ட் ஹவுஸின் பெயர் இருக்கும்.
3. **Guest House Booking**-ல், **Check-in Date** மற்றும் **Check-in Time** தேர்வு செய்யுங்கள்.
4. **Check-out Date** மற்றும் **Check-out Time** தேர்வு செய்யுங்கள்.
5. **Important Information** பெட்டியைப் படித்து, பிறகு **Check Availability** அழுத்துங்கள்.

> Tip: சரியான அடையாளச் சான்று (Aadhaar அல்லது Passport) எடுத்து வாருங்கள். Check-in-ல் அது தேவைப்படும்.

## பொதுவான பிரச்சனைகள்

- **"Check-out must be after Check-in."** (Check-out, check-in-க்குப் பிறகுதான் இருக்க வேண்டும்) பிந்தைய check-out தேதி அல்லது நேரத்தைத் தேர்வு செய்யுங்கள்.
- **"No guest houses available".** (கெஸ்ட் ஹவுஸ் எதுவும் இல்லை) **Retry** அழுத்துங்கள். இன்னும் அப்படியே வந்தால், SDWA அலுவலகத்தைத் தொடர்புகொள்ளுங்கள்.
- **நான் மீண்டும் Choose Your Guest House பக்கத்துக்கே அனுப்பப்படுகிறேன்.** முதலில் **Select** மூலம் கெஸ்ட் ஹவுஸைத் தேர்வு செய்து, பிறகு தேதிகளைத் தேர்வு செய்யுங்கள்.
:::

::: section choose-room
title: அறையைத் தேர்வு செய்வது
routes: /Rooms
for: 
roles: 
designations: 
minutes: 2
source-hash: 9eefa1291c16d092
confidence: ok

**இந்தத் திரை எதற்கு**
**Available Rooms** திரை (5-ல் படி 2) உங்கள் தேதிகளுக்குக் காலியாக உள்ள அறைகளைக் காட்டும்.

1. அறை கார்டுகளைப் பாருங்கள். ஒவ்வொன்றிலும் அறை வகை, ஒரு இரவுக்கான விலை, எத்தனை அறைகள் காலியாக உள்ளன, எத்தனை விருந்தினர்கள் தங்கலாம் என்பது தெரியும்.
2. அந்த அறைகளை மட்டும் பார்க்க, மேலே ஒரு அறை வகையைத் தட்டுங்கள், அல்லது **Search**-ல் டைப் செய்யுங்கள். **All** எல்லா அறைகளையும் காட்டும்.
3. உங்களுக்குத் தேவையான **Number of Rooms**-ஐத் தேர்வு செய்யுங்கள்.
4. கூடுதல் படுக்கை வேண்டுமென்றால், **Additional Bed**-ஐ டிக் செய்து **No. of Additional Beds**-ஐத் தேர்வு செய்யுங்கள். கூடுதல் படுக்கைக்குத் தனியாக ஒரு இரவுக்கான விலை உண்டு.
5. **Book Now** அழுத்துங்கள்.

> Tip: **View Details** பட்டன் இன்னும் வேலை செய்யாது. அறையின் எல்லா விவரங்களும் கார்டிலேயே உள்ளன.

## பொதுவான பிரச்சனைகள்

- **"No rooms available".** (அறைகள் இல்லை) அந்தத் தேதிகளுக்கு எந்த அறையும் காலியாக இல்லை. **Change dates** அழுத்துங்கள்.
- **"No matching rooms".** (பொருந்தும் அறைகள் இல்லை) **Clear filters** அழுத்துங்கள்.
- **"Up to ... rooms can be booked at a time for this room type."** (இந்த அறை வகையில் ஒரே நேரத்தில் ... அறைகள் வரை மட்டுமே புக் செய்ய முடியும்) மீதியை இரண்டாவது புக்கிங்கில் புக் செய்யுங்கள்.
:::

::: section guest-details
title: விருந்தினர் விவரங்களை நிரப்புவது
routes: /GuestDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: eca96eb90f508346
confidence: ok

**இந்தத் திரை எதற்கு**
**Guest Details** (5-ல் படி 3) திரையில் யார் தங்கப் போகிறார்கள் என்று சொல்லுங்கள். வலது பக்கம் உள்ள **Booking Summary** விலையைக் காட்டும்.

1. **Employee ID / Dealer Code** மற்றும் **Guest Name**-ஐ உள்ளிடுங்கள்.
2. **Company Name** பட்டியலில் இருந்தால் அதைத் தேர்வு செய்து, **Phone Number** மற்றும் **Email ID**-ஐ உள்ளிடுங்கள். டீலர் என்றால், போன் எண்ணும் ஈமெயிலும் டீலர் பதிவில் இருந்து தானாக நிரப்பப்படும்.
3. **Aadhaar / Passport Number**-ஐ உள்ளிட்டு **Nationality**-ஐத் தேர்வு செய்யுங்கள்.
4. **Number of Persons**, **Number of Adults** மற்றும் **Number of Children (below 12 years)**-ஐத் தேர்வு செய்யுங்கள்.
5. விரும்பினால், **Upload ID Proof (Optional)**-ல் அடையாளச் சான்றை அப்லோட் செய்யுங்கள்.
6. **Booking Summary**-ஐச் சரிபாருங்கள்: அறைக் கட்டணம், **GST (5%)** மற்றும் **Total**. பிறகு **Continue Review** அழுத்துங்கள்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Employee ID / Dealer Code** | உங்கள் ஊழியர் ID அல்லது டீலர் கோடு. கட்டாயம். |
| **Guest Name** | தங்கப் போகும் நபரின் பெயர். கட்டாயம். |
| **Phone Number** மற்றும் **Email ID** | விருந்தினரின் தொடர்பு விவரங்கள். கட்டாயம். |
| **Nationality** | Indian, NRI அல்லது Foreigner. கட்டாயம். |
| **Company Name** | பில்லுக்கான கம்பெனியும் அதன் GSTIN-ம். கட்டாயம் இல்லை. |
| **Upload ID Proof (Optional)** | ஒரே ஒரு PDF, JPG அல்லது PNG கோப்பு, 5 MB வரை. |

## பொதுவான பிரச்சனைகள்

- **"Employee ID / Dealer Code is required to continue."** (தொடர Employee ID / Dealer Code கட்டாயம்) கட்டாயமான எல்லாப் புலங்களையும் நிரப்புங்கள். எது விடுபட்டுள்ளது என்று செய்தியே சொல்லும்.
- **"Only PDF, JPG, or PNG files are allowed."** அல்லது **"File must be 5 MB or less."** (PDF, JPG, PNG கோப்புகள் மட்டும், 5 MB அல்லது அதற்குக் குறைவாக) வேறு கோப்பைத் தேர்வு செய்யுங்கள், அல்லது சிறிய புகைப்படம் எடுங்கள்.
- **நான் அறையை மாற்ற வேண்டும்.** **Available Rooms**-க்குத் திரும்ப **Back** அழுத்துங்கள்.
:::

::: section review-pay
title: புக்கிங்கைச் சரிபார்த்து முடிப்பது
routes: /BookingPreview, /Payment
for: 
roles: 
designations: 
minutes: 3
source-hash: 0ae6b596dd70c921
confidence: ok

**இந்தத் திரை எதற்கு**
**Review Your Booking** (5-ல் படி 4) திரையில் எல்லாவற்றையும் சரிபார்க்கிறீர்கள். **Payment** (5-ல் படி 5) திரையில் புக்கிங்கை உறுதி செய்கிறீர்கள். கம்பெனி ஊழியர்கள் எதுவும் கட்ட வேண்டாம். டீலர்களும் மற்றவர்களும் Razorpay மூலம் ஆன்லைனில் பணம் கட்டுவார்கள்.

1. **Review Your Booking**-ல், **Booking Information**, **Booking Summary** மற்றும் **Guest Information**-ஐச் சரிபாருங்கள்.
2. ஏதாவது மாற்ற வேண்டுமென்றால் **Edit Details** அழுத்துங்கள். எல்லாம் சரியாக இருந்தால், **Proceed to Payment** அழுத்துங்கள்.
3. **Payment**-ல், **Booking Summary**-ஐச் சரிபாருங்கள். ஊழியர்களுக்கு **Complimentary (Employee)** என்றும் மொத்தம் ₹0.00 என்றும் தெரியும். மற்றவர்களுக்கு **Online Payment** என்று தெரியும்.
4. **Complete Booking** அழுத்துங்கள். ஆன்லைன் பணம் என்றால், Razorpay விண்டோ திறக்கும். UPI, கார்டு, நெட் பேங்கிங் அல்லது வாலட் மூலம் கட்டுங்கள்.
5. **Booking Confirmed!** பெட்டியில் உங்கள் **Booking ID** மற்றும் **Payment Status** தெரியும். **View My Bookings** அழுத்துங்கள், அல்லது மீண்டும் புக் செய்ய **New Booking** அழுத்துங்கள்.

> Careful: பணம் கட்டும்போது பணம் கட்டும் விண்டோவை மூடாதீர்கள், பக்கத்தை விட்டுப் போகாதீர்கள். பணம் கட்டுவது தோல்வியடைந்தாலோ நீங்கள் அதை மூடினாலோ, இந்த புக்கிங் நடக்காது. நீங்கள் புதிய புக்கிங் தொடங்க வேண்டும்.

## பொதுவான பிரச்சனைகள்

- **"No booking details found".** (புக்கிங் விவரங்கள் இல்லை) புக்கிங் தொடங்கப்படவில்லை, அல்லது அதன் நேரம் முடிந்துவிட்டது. **Book a Room**-ல் இருந்து மீண்டும் தொடங்குங்கள்.
- **பட்டனில் "Payment Failed" என்று காட்டுகிறது.** புதிய புக்கிங் தொடங்குங்கள். பணம் எடுக்கப்பட்டிருந்தால், அது தானாகவே திருப்பித் தரப்படும்.
- **"This room was just booked by someone else for the selected dates. Please try again."** (இந்த அறையை இப்போதுதான் வேறொருவர் புக் செய்துவிட்டார்) அறையையோ தேதிகளையோ மீண்டும் தேர்வு செய்யுங்கள்.
- **"Your room hold has expired..."** (உங்களுக்காக வைத்திருந்த அறையின் நேரம் முடிந்துவிட்டது) பணம் கட்ட அதிக நேரம் எடுத்துக்கொண்டீர்கள். புதிய புக்கிங் தொடங்குங்கள்.
:::

::: section my-bookings
title: என் புக்கிங்குகளைப் பார்ப்பது
routes: /MyBookings
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-mb-cards] | 2 > [data-help=gh-mb-tabs] | 3 > [data-help=gh-mb-list] | 4 > [data-help=gh-mb-list] | 5 > [data-help=gh-mb-new]
minutes: 2
source-hash: eddb90512855180d
confidence: ok

**இந்தத் திரை எதற்கு**
**My Bookings** திரையில் நீங்கள் செய்த எல்லா அறை புக்கிங்குகளும், மேலே எண்ணிக்கைகளுடன் தெரியும். மெனுவில் இருந்து திறங்கள்: **SDWA**, பிறகு **My Bookings / Cancellation**.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Bookings**, **Upcoming**, **Completed** மற்றும் **Cancelled**.
2. ஒரு டேபைத் தட்டுங்கள்: **Upcoming**, **Completed** அல்லது **Cancelled**.
3. ஒவ்வொரு புக்கிங் கார்டிலும் அறை வகை, புக்கிங் நிலை, பணம் கட்டிய நிலை, **Booking ID**, கெஸ்ட் ஹவுஸ், தேதிகள், **Guests**, **Rooms** மற்றும் **Total Amount** தெரியும்.
4. ஒரு புக்கிங்கைத் திறக்க **View Details** அழுத்துங்கள். வரவிருக்கும் புக்கிங்கை ரத்து செய்யக் கேட்க **Cancel Booking** அழுத்துங்கள். ரத்தான புக்கிங்கில் **Refund Status** அழுத்துங்கள்.
5. இன்னொரு அறையை புக் செய்ய **New Booking** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No upcoming bookings".** (வரவிருக்கும் புக்கிங் இல்லை) இன்றோ அதற்குப் பிறகோ உங்களுக்கு எந்த புக்கிங்கும் இல்லை. **Book a room** அழுத்துங்கள்.
- **"Could not load bookings".** (புக்கிங்குகளை ஏற்ற முடியவில்லை) உங்கள் இன்டர்நெட்டைச் சரிபார்த்து **Retry** அழுத்துங்கள்.
- **ஒரு புக்கிங்கில் Cancelled, Failed என்று காட்டுகிறது.** அதன் ஆன்லைன் பணம் கட்டுவது நடக்கவில்லை. மீண்டும் புக் செய்யுங்கள்.
:::

::: section booking-details
title: ஒரு புக்கிங்கைப் பார்த்து invoice பதிவிறக்குவது
routes: /BookingDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 4c0fd3b25f0fb3dd
confidence: ok

**இந்தத் திரை எதற்கு**
**Booking Details** திரையில் ஒரு புக்கிங் பற்றிய எல்லாமே தெரியும்: அறை, தேதிகள், பணம், நிலை, விருந்தினர்.

1. **My Bookings**-ல், ஒரு புக்கிங்கில் **View Details** அழுத்துங்கள்.
2. **Room Details** மற்றும் **Check-in & Check-out** பாருங்கள். அதில் எத்தனை இரவுகள், அறைகள், கூடுதல் படுக்கைகள் என்று இருக்கும். **Room Number** தெரிந்ததும் அது காட்டப்படும்.
3. **Payment Details** பாருங்கள்: அறைக் கட்டணம், **GST (5%)**, **Total Amount**, **Payment Method** மற்றும் **Payment Status**.
4. **Booking Status** பாருங்கள். ரத்தான புக்கிங்கில் **Cancellation Status**-ம் தெரியும்.
5. **Guest Details** பாருங்கள். நீங்கள் அடையாளச் சான்றை அப்லோட் செய்திருந்தால், **View** அல்லது **Download** அழுத்துங்கள்.
6. Tax invoice-ஐ PDF ஆகச் சேமிக்க **Download Invoice** அழுத்துங்கள்.
7. திரும்பிப் போக **Back to My Bookings** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Download Invoice பட்டன் இல்லை.** பணம் **Paid** ஆகி, invoice தயாரானால் மட்டுமே அது தெரியும்.
- **"Booking not found."** (புக்கிங் கிடைக்கவில்லை) நீங்கள் செய்த புக்கிங்குகளை மட்டுமே திறக்க முடியும்.
:::

::: section cancel-booking
title: புக்கிங்கை ரத்து செய்வது
routes: /CancelBooking
for: 
roles: 
designations: 
minutes: 3
source-hash: 58fac8bc78e4e38a
confidence: ok

**இந்தத் திரை எதற்கு**
ரத்து செய்யக் கேட்க **Cancel Booking**-ஐப் பயன்படுத்துங்கள். Admin உங்கள் கோரிக்கையைச் சரிபார்ப்பார். Admin ஒப்புதல் தரும் வரை உங்கள் புக்கிங் தொடர்ந்து இருக்கும்.

1. **My Bookings**-ல், வரவிருக்கும் ஒரு புக்கிங்கில் **Cancel Booking** அழுத்துங்கள்.
2. **Important Notice**-ஐப் படியுங்கள். Check-in-க்கு இன்னும் எவ்வளவு நேரம் உள்ளது என்பதைப் பொறுத்து, உங்களுக்கு எத்தனை சதவீதம் refund கிடைக்கும் என்று அது காட்டும்.
3. **Cancellation Reason**-ன் கீழ், ஒரு காரணத்தைத் தேர்வு செய்யுங்கள்.
4. **Refund Calculation**-ஐச் சரிபாருங்கள்: **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)**, **Refund Amount**, **Refund Method** மற்றும் **Estimated Time**.
5. **Continue Cancellation** அழுத்துங்கள். **Confirm Cancellation?** பெட்டியில், **Cancel Booking** அழுத்துங்கள். மனம் மாறினால் **Keep My Booking** அழுத்துங்கள்.
6. **Cancellation Request Submitted** திரையில் உங்கள் **Cancellation ID** மற்றும் **Pending Approval** தெரியும். **Refund Status**, **View Booking** அல்லது **Go to My Bookings** அழுத்துங்கள்.

> Careful: Check-in நெருங்க நெருங்க refund குறைவாக இருக்கும். முதலில் **Cancellation Policy** பெட்டியைப் படியுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Please select a reason for cancellation."** (ரத்து செய்வதற்கான காரணத்தைத் தேர்வு செய்யுங்கள்) முதலில் ஒரு காரணத்தைத் தேர்வு செய்யுங்கள்.
- **"This booking is already checked in and cannot be cancelled online. Please contact the Front Office."** (இந்த புக்கிங் ஏற்கனவே check-in ஆகிவிட்டது, ஆன்லைனில் ரத்து செய்ய முடியாது) Front Office-இடம் பேசுங்கள்.
- **"A cancellation request for this booking has already been submitted".** (இந்த புக்கிங்குக்கு ரத்து கோரிக்கை ஏற்கனவே அனுப்பப்பட்டுவிட்டது) நீங்கள் ஏற்கனவே கேட்டுவிட்டீர்கள். **View Refund Status** அழுத்துங்கள்.
:::

::: section refund-status
title: என் refund-ஐப் பின்தொடர்வது
routes: /RefundStatus
for: 
roles: 
designations: 
minutes: 2
source-hash: f4d86371946e0403
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் ரத்தும் refund-ம் இப்போது எந்த நிலையில் உள்ளன என்று **Refund Status** காட்டும்.

1. **My Bookings**-ல், **Cancelled** டேபைத் திறந்து **Refund Status** அழுத்துங்கள். கோரிக்கை அனுப்பிய பிறகு **Cancel Booking**-ல் இருந்தும் இதைத் திறக்கலாம்.
2. மேலே, **Total Refund Amount**, **Cancellation ID**, **Refund Method**, **Target Refund Date**, **Transaction Ref.** மற்றும் **Current Status** பாருங்கள்.
3. **Refund Breakdown**-ன் கீழ், **Total Paid**, **Cancellation Charges**, **Tax Adjustment (GST)** மற்றும் **Refund Amount** பாருங்கள்.
4. **Refund Tracking**-ன் கீழ், படிகளைப் பின்தொடருங்கள்: **Cancellation Requested**, பிறகு **Pending Admin Approval**, **Approved by Admin** அல்லது **Cancellation Rejected**, பிறகு refund.
5. இலக்குத் தேதிக்குள் பணம் உங்களுக்கு வரவில்லை என்றால், ஆரஞ்சு நிறப் பெட்டியில் உள்ள reference எண்ணுடன் support-ஐத் தொடர்புகொள்ளுங்கள்.
6. **My Bookings**-க்குத் திரும்ப **Back** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **என் புக்கிங் Cancelled டேபில் இல்லை.** Admin ஒப்புதல் தந்த பிறகுதான் அது அங்கே போகும். அதுவரை, அந்த புக்கிங்கின் **Cancel Booking** திரையைத் திறந்து **View Refund Status** அழுத்துங்கள்.
- **"Cancellation Rejected".** (ரத்து நிராகரிக்கப்பட்டது) காரணத்தைப் படியுங்கள். உங்கள் புக்கிங் தொடர்ந்து இருக்கும்.
- **"No Refund Due".** (refund இல்லை) ரத்து விதிமுறைப்படி பணம் எதுவும் திருப்பித் தரப்படாது, உதாரணமாக ஊழியரின் இலவச புக்கிங்கில்.
:::

::: section front-office
title: Front Office-ல் எல்லா அறைகளையும் பார்ப்பது
routes: /FrontOffice
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-fo-cards] | 2 > [data-help=gh-fo-tabs] | 3 > [data-help=gh-fo-search] | 4 > [data-help=gh-fo-bill]
minutes: 2
source-hash: c62b602840a09360
confidence: ok

**இந்தத் திரை எதற்கு**
**Front Office** திரையில் ஒவ்வொரு அறையும் தெரியும்: அது காலியா, புக் ஆகியுள்ளதா, ஆள் தங்கியுள்ளதா, யாரிடம் உள்ளது என்று. மெனுவில் இருந்து திறங்கள்: **SDWA**, பிறகு **Front Office**. Receptionist தன் சொந்த மாநிலத்தின் கெஸ்ட் ஹவுஸை மட்டுமே பார்ப்பார்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Rooms**, **Available**, **Reserved** மற்றும் **Checked-In**.
2. ஒரு டேபைத் தட்டுங்கள்: **All**, **Available**, **Reserved / Booked** அல்லது **Checked-In**.
3. **Search rooms...**-ல் அறை எண், booking ID அல்லது விருந்தினர் பெயரை டைப் செய்யுங்கள், அல்லது **All Guest Houses**-ல் ஒரு கெஸ்ட் ஹவுஸைத் தேர்வு செய்யுங்கள். ஒவ்வொரு வரிசையிலும் அறை, அதன் நிலை, புக்கிங்கும் விருந்தினரும், தேதிகள், செயல்கள் தெரியும்.
4. Check-out செய்த விருந்தினருக்குப் பில் போட **Generate Bill** அழுத்துங்கள். அறைகளை மீண்டும் ஏற்ற **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"You do not have permission to access the Front Office."** (Front Office-ஐத் திறக்க உங்களுக்கு அனுமதி இல்லை) Front Office அனுமதிக்கு உங்கள் administrator-இடம் கேளுங்கள்.
- **"No rooms found".** (அறைகள் எதுவும் இல்லை) **All** மற்றும் **All Guest Houses** தேர்வு செய்து, தேடலை அழியுங்கள்.
:::

::: section check-in
title: விருந்தினரை check-in செய்வது
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 3
source-hash: 4d3a8c4aabfb6c8a
confidence: ok

**இந்தத் திரை எதற்கு**
விருந்தினர் வந்ததும், அவரை check-in செய்து அவருக்கான அறை எண்களைத் தாருங்கள்.

1. **Front Office**-ல், **Reserved / Booked** தட்டி புக்கிங்கைக் கண்டுபிடியுங்கள்.
2. **Actions** நெடுவரிசையில் **Check-In** அழுத்துங்கள்.
3. **Check-In** பெட்டியில், **Booking ID**, **Guest Name**, **Room Type** மற்றும் **Guest House**-ஐச் சரிபாருங்கள். அடையாளச் சான்று இணைக்கப்பட்டிருந்தால், அதைப் பார்க்க **View** அழுத்துங்கள்.
4. **Number of Persons** மற்றும் **Number of Extra Beds**-ஐச் சரிபார்த்து, தேவைப்பட்டால் திருத்துங்கள்.
5. **Assign Physical Room(s)**-ன் கீழ், தர வேண்டிய அறை எண்களைத் தட்டுங்கள். புக் செய்த அதே எண்ணிக்கையில் சரியாக அறைகளைத் தேர்வு செய்யுங்கள்.
6. **Confirm Check-In** அழுத்துங்கள். இப்போது அறையில் **Checked-In** என்று காட்டும்.

> Careful: Check-in செய்யும் முன் விருந்தினரின் அடையாளச் சான்றை (Aadhaar அல்லது Passport) சரிபாருங்கள்.

## பொதுவான பிரச்சனைகள்

- **"This booking's check-in date has not arrived yet."** (இந்த புக்கிங்கின் check-in தேதி இன்னும் வரவில்லை) Check-in தேதி அன்றோ அதற்குப் பிறகோ check-in செய்யுங்கள்.
- **"This booking's stay period has passed and it can no longer be checked in."** (தங்கும் காலம் முடிந்துவிட்டது, இனி check-in செய்ய முடியாது) தங்கும் தேதிகள் முடிந்துவிட்டன.
- **"Please select exactly ... physical room(s) before confirming check-in."** (check-in உறுதி செய்யும் முன் சரியாக ... அறை(கள்) தேர்வு செய்யுங்கள்) இன்னும் அதிகமாகவோ குறைவாகவோ அறை எண்களைத் தட்டுங்கள்.
- **"No physical rooms are currently free for the booked stay period."** (புக் செய்த காலத்துக்கு இப்போது எந்த அறையும் காலியாக இல்லை) புக் செய்தவரைத் தொடர்புகொள்ளுங்கள், அல்லது வேறு அறை வகையைத் தேர்வு செய்யுங்கள்.
- **"These rooms were just assigned to another overlapping stay. Please refresh and try again."** (இந்த அறைகள் இப்போதுதான் வேறொரு தங்குதலுக்குத் தரப்பட்டுவிட்டன) **Refresh** அழுத்தி வேறு அறைகளைத் தேர்வு செய்யுங்கள்.
:::

::: section check-out
title: விருந்தினரை check-out செய்து பணம் வாங்குவது
routes: /FrontOffice
for: 
roles: 
designations: 
minutes: 2
source-hash: cf102cb774da6ac0
confidence: ok

**இந்தத் திரை எதற்கு**
விருந்தினர் கிளம்பும்போது, பணம் வந்துவிட்டதா என்று உறுதி செய்து, பிறகு அவரை check-out செய்யுங்கள். அறை மீண்டும் காலியாகும்.

1. **Front Office**-ல், **Checked-In** தட்டி விருந்தினரைக் கண்டுபிடியுங்கள்.
2. **Actions** நெடுவரிசையில் **Check-Out** அழுத்துங்கள்.
3. **Check-Out & Payment**-ல், **Room Number**, தேதிகள், **Number of Persons**, **Extra Beds**, **Total Amount**, **Payment Method** மற்றும் **Payment Status**-ஐச் சரிபாருங்கள்.
4. **Payment Status** **Pending** என்று இருந்தால், பணத்தை வாங்கி **Confirm Payment Received** அழுத்துங்கள்.
5. **Paid** என்று காட்டியதும், **Confirm Check-Out** அழுத்துங்கள். அறையில் மீண்டும் **Available** என்று காட்டும்.
6. இப்போது **Generate Bill** மூலம் பில் போடுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Payment must be received before check-out."** (check-out-க்கு முன் பணம் வந்திருக்க வேண்டும்) முதலில் **Confirm Payment Received** அழுத்துங்கள்.
- **"Check-Out is only allowed for a booking that is currently checked in."** (இப்போது check-in-ல் உள்ள புக்கிங்குக்கு மட்டுமே check-out செய்ய முடியும்) விருந்தினர் check-in ஆகவில்லை. **Status**-ஐப் பாருங்கள்.
:::

::: section bills
title: முடிந்த தங்குதல்களையும் பில்களையும் கண்டுபிடிப்பது
routes: /GenerateBill
for: 
roles: 
designations: 
tour: 1 > [data-help=gh-gb-filters] | 2 > [data-help=gh-gb-go] | 3 > [data-help=gh-gb-tabs] | 4 > [data-help=gh-gb-find]
minutes: 2
source-hash: 5194cb8d5292259e
confidence: ok

**இந்தத் திரை எதற்கு**
**Generate Bill** திரையில் check-out ஆன தங்குதல்களும் ஏற்கனவே போட்ட பில்களும் இருக்கும். மெனுவில் இருந்து (**SDWA**, பிறகு **Generate Bill**) அல்லது **Front Office**-ல் இருந்து திறங்கள்.

1. **Filters**-ன் கீழ், **From Date** மற்றும் **To Date** தேர்வு செய்யுங்கள். தங்குதல்களுக்கு இவை check-out தேதிகள், பில்களுக்கு பில் தேதிகள்.
2. **Search** அழுத்துங்கள். **Today** இன்றையதை மட்டும் காட்டும். **Refresh** பட்டியலை மீண்டும் ஏற்றும்.
3. Check-out ஆன தங்குதல்களைப் பார்க்க **Completed Bookings** தட்டுங்கள், ஏற்கனவே போட்ட பில்களைப் பார்க்க **Generated Bills** தட்டுங்கள்.
4. தேடல் பெட்டியில் booking ID அல்லது விருந்தினர் பெயரை டைப் செய்யுங்கள். ஒரு தங்குதலில், **Bill Status** **Not Billed** அல்லது **Billed** என்று காட்டும். **Generate Bill** அல்லது **View Bill** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"From Date cannot be after To Date."** (From Date, To Date-க்குப் பிறகு இருக்கக் கூடாது) தேதிகளை மாற்றுங்கள்.
- **ஒரு தங்குதல் பட்டியலில் இல்லை.** Check-out ஆன தங்குதல்கள் மட்டுமே, அவற்றின் check-out தேதிப்படி தெரியும். தேதி இடைவெளியை அதிகமாக்குங்கள்.
:::

::: section generate-bill
title: ஒரு தங்குதலுக்குப் பில் போடுவது
routes: /GenerateBill
for: 
roles: 
designations: 
minutes: 4
source-hash: 71ab93bc25bfd36f
confidence: ok

**இந்தத் திரை எதற்கு**
Check-out-க்குப் பிறகு, அந்தத் தங்குதலுக்கான tax invoice-ஐ உருவாக்குங்கள். ஒவ்வொரு தங்குதலுக்கும் ஒரே ஒரு முறைதான் பில் போட முடியும்.

1. **Completed Bookings**-ல், **Not Billed** என்று காட்டும் தங்குதலில் **Generate Bill** அழுத்துங்கள். **New Bill** திரை திறக்கும்.
2. **Booking Summary**-ஐச் சரிபாருங்கள்: **Booking**, **Guest**, **Company Name**, **GSTIN**, **Guest House**, **Room** மற்றும் **Stay**.
3. **Bill Items**-ன் கீழ், அறைக்கான வரி தானாக நிரப்பப்பட்டிருக்கும். இருந்தால் **Food Bill** மற்றும் **Additional Beds** தொகைகளை உள்ளிடுங்கள்.
4. லாண்டரி அல்லது மற்ற கட்டணங்களுக்கு, **Add Charge** அழுத்துங்கள். விவரம், **Qty**, **Rate**, **CGST %** மற்றும் **SGST %** உள்ளிடுங்கள். சேர்த்த வரியை **Remove** நீக்கும்.
5. **Totals, Discount & Payment**-ன் கீழ், தேவைப்பட்டால் **Discount**, **Advance Payment** மற்றும் **Round Off** உள்ளிடுங்கள். **Payment Method**-ஐத் தேர்வு செய்து, **Payment received**-ஐ டிக் செய்து, **Remarks** சேருங்கள்.
6. **Balance Payable**-ஐச் சரிபார்த்து, பிறகு **Generate Bill** அழுத்துங்கள். புதிய பில் **Generated Bills** டேபில் தெரியும்.
7. Invoice-ஐப் பார்க்க **View** அழுத்துங்கள், பிறகு **Download PDF** அல்லது **Print** அழுத்துங்கள். பட்டியலில் உள்ள **PDF** அதை நேரடியாகப் பதிவிறக்கும்.

> Careful: **Generate Bill** அழுத்தும் முன் ஒவ்வொரு தொகையையும் சரிபாருங்கள். அதே தங்குதலுக்கு மீண்டும் பில் போட முடியாது.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Food Bill** | தங்கியிருந்தபோது தந்த உணவின் அளவும் விலையும். |
| **Add Charge** | வேறு ஏதாவது கட்டணம், அதற்கான CGST % மற்றும் SGST %-உடன். |
| **Advance Payment** | இந்தத் தங்குதலுக்கு ஏற்கனவே கட்டிய பணம். தெரிந்தால் தானாக நிரப்பப்படும். |
| **Payment Method** | UPI, Card, Net Banking அல்லது Cash. ஊழியரின் இலவசத் தங்குதலுக்கு Complimentary என்றும் காட்டும். |

## பொதுவான பிரச்சனைகள்

- **"Discount cannot exceed Grand Total."** அல்லது **"Advance Payment cannot exceed Grand Total."** (Grand Total-ஐ விட அதிகமாக இருக்கக் கூடாது) தொகையைக் குறையுங்கள்.
- **"Balance Payable cannot be negative."** (Balance Payable மைனஸில் இருக்கக் கூடாது) **Discount**, **Advance Payment** மற்றும் **Round Off**-ஐச் சரிபாருங்கள்.
- **"A bill has already been generated for this booking".** (இந்த புக்கிங்குக்கு ஏற்கனவே பில் போடப்பட்டுவிட்டது) அதற்குப் பதிலாக **View Bill** மூலம் அதைத் திறங்கள்.
:::

::: section bill-list
title: போட்ட எல்லாப் பில்களையும் பார்ப்பது
routes: /BillList
for: 
roles: 
designations: 
minutes: 1
source-hash: 863b55025a37bbd6
confidence: ok

**இந்தத் திரை எதற்கு**
**Bill List** திரையில் போட்ட எல்லா invoice-களும், புதியவை முதலில், தெரியும். இதே பில்கள் **Generate Bill**-ன் **Generated Bills** டேபிலும் உள்ளன.

1. அட்டவணையைப் பாருங்கள்: **Bill #**, **Booking**, **Guest**, **Guest House**, **Room**, **Bill Date**, தேதிகள், **Total**, **Balance** மற்றும் **Payment**.
2. ஒரு invoice-ஐத் திறக்க **View** அழுத்துங்கள். அதில், **Download PDF** அல்லது **Print** அழுத்துங்கள்.
3. Invoice-ஐ உடனே பதிவிறக்க **PDF** அழுத்துங்கள்.
4. இன்னும் வரிசைகளைப் பார்க்க **Show more** அழுத்துங்கள்.
5. புதிய பில் போட **Generate Bill** அழுத்துங்கள், அல்லது மீண்டும் ஏற்ற **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No bills yet".** (இன்னும் பில்கள் இல்லை) இதுவரை எந்தப் பில்லும் போடப்படவில்லை. **Generate Bill**-ல் இருந்து ஒன்றைப் போடுங்கள்.
:::

::: section cancellation-requests
title: ரத்து கோரிக்கைகளுக்கு ஒப்புதல் தருவது அல்லது நிராகரிப்பது
routes: /GuestHouseCancellations
for: 
roles: 
designations: 
minutes: 3
source-hash: abf61f8cc73c49f2
confidence: ok

**இந்தத் திரை எதற்கு**
யாராவது ஒரு புக்கிங்கை ரத்து செய்யக் கேட்டால், அந்தக் கோரிக்கை இங்கே வரும். மெனுவில் இருந்து திறங்கள்: **SDWA**, பிறகு **Cancellation Requests**.

1. ஒரு டேபைத் தட்டுங்கள்: **Pending Approval**, **Approved**, **Rejected** அல்லது **All**.
2. **Search requests...**-ல் டைப் செய்யுங்கள், அல்லது **All Guest Houses**-ல் ஒரு கெஸ்ட் ஹவுஸைத் தேர்வு செய்யுங்கள்.
3. வரிசையைப் படியுங்கள்: புக்கிங், விருந்தினர், தங்குதல், கட்டிய தொகை (**Paid**), **Refund**, **Reason**, எப்போது கேட்கப்பட்டது (**Requested**).
4. ஏற்க, **Approve** அழுத்தி, பெட்டியைப் படித்து, **Approve Cancellation** அழுத்துங்கள். புக்கிங் ரத்தாகும், அறை விடுவிக்கப்படும்.
5. மறுக்க, **Reject** அழுத்தி, **Reason for rejection** எழுதி, **Reject Request** அழுத்துங்கள். புக்கிங் தொடர்ந்து இருக்கும்.
6. Refund-க்கு ஒப்புதல் தந்த பிறகு, 2 வேலை நாட்களுக்குள் அதை முதலில் பணம் கட்டிய வழிக்கே கையால் அனுப்புங்கள்.

> Careful: ஆப் தானாக refund அனுப்பாது. யாராவது ஒருவர் அதை ஆப்புக்கு வெளியே அனுப்ப வேண்டும்.

## பொதுவான பிரச்சனைகள்

- **"You do not have permission to review cancellation requests."** (ரத்து கோரிக்கைகளைப் பார்க்க உங்களுக்கு அனுமதி இல்லை) இந்த அனுமதிக்கு உங்கள் administrator-இடம் கேளுங்கள்.
- **"The guest is checked in. Check the guest out instead of cancelling."** (விருந்தினர் check-in ஆகியுள்ளார், ரத்து செய்வதற்குப் பதிலாக check-out செய்யுங்கள்) **Front Office**-ல் **Check-Out** பயன்படுத்துங்கள்.
- **Reject பட்டன் இல்லை.** இந்தக் கோரிக்கைக்கான refund ஏற்கனவே நடந்துகொண்டிருக்கிறது.
:::

::: section guest-house-master
title: கெஸ்ட் ஹவுஸ், அறை வகைகள், அறைகளைச் சேர்ப்பது
routes: /GuestHouseMaster
for: 
roles: 
designations: 
minutes: 4
source-hash: 7f6f2fee8bd4e156
confidence: ok

**இந்தத் திரை எதற்கு**
மக்கள் எதை புக் செய்யலாம் என்பதை அமைக்க admin-கள் **Guest House & Room Master**-ஐப் பயன்படுத்துவார்கள். மெனுவில் இருந்து திறங்கள்: **SDWA**, பிறகு **Guest House**. முதலில் கெஸ்ட் ஹவுஸ், பிறகு அறை வகைகள், பிறகு அறைகளைச் சேருங்கள்.

1. **Guest House** டேபில், **Name**-ஐயும், தெரிந்தால் **Address**, **Phone Number**, **State** மற்றும் **Description**-ஐயும் உள்ளிடுங்கள். புகைப்படம் சேர்க்க **Upload Image** அழுத்துங்கள்.
2. **Room Type** டேபில், **Room Type Name** உள்ளிடுங்கள், உதாரணமாக AC Deluxe Room.
3. **Rooms** டேபில், **Guest House** மற்றும் **Room Type (Master)** தேர்வு செய்து, பிறகு **Room Type**, **Room Number**, **No. of Rooms** மற்றும் **Rate (₹)** உள்ளிடுங்கள்.
4. ஸ்விட்சை **Active** அல்லது **Inactive** ஆக வைத்து **Save** அழுத்துங்கள். **Cancel** படிவத்தை அழிக்கும்.
5. **Guest House** அல்லது **Rooms** டேபில் பலவற்றை ஒரே நேரத்தில் சேர்க்க, **Download Template** அழுத்தி, அதை நிரப்பி, பிறகு **Upload File** அழுத்துங்கள்.
6. அட்டவணையில், தேடல் பெட்டியையும் **All Status**-ஐயும் பயன்படுத்துங்கள். மாற்ற **Active** அல்லது **Inactive** தட்டுங்கள், திருத்த பென்சிலைத் தட்டுங்கள், நீக்க குப்பைத்தொட்டியைத் தட்டுங்கள்.

> Careful: நீக்கியதைத் திரும்பப் பெற முடியாது. சில காலம் புக்கிங்கை நிறுத்த, அதற்குப் பதிலாக **Inactive** ஆக வையுங்கள். Inactive கெஸ்ட் ஹவுஸ்கள் புக்கிங்கில் தெரியாது.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Name** | கெஸ்ட் ஹவுஸின் பெயர். கட்டாயம். |
| **State** | கெஸ்ட் ஹவுஸ் உள்ள மாநிலம். அந்த மாநிலத்தின் receptionist-கள் அதைக் கவனிப்பார்கள். |
| **No. of Rooms** | இந்த வகையில் எத்தனை அறைகள். 1 அல்லது அதற்கு மேல் முழு எண். கட்டாயம். |
| **Rate (₹)** | ஒரு இரவுக்கான விலை. கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"No. of Rooms must be a positive whole number."** (No. of Rooms 1 அல்லது அதற்கு மேல் முழு எண்ணாக இருக்க வேண்டும்) 1 அல்லது அதற்கு மேல் உள்ளிடுங்கள்.
- **"Rate must be a valid amount."** (Rate சரியான தொகையாக இருக்க வேண்டும்) ஒரு எண்ணை உள்ளிடுங்கள், உதாரணமாக 1200.
- **எனக்கு அட்டவணைகள் தெரிகின்றன, ஆனால் படிவம் இல்லை.** நீங்கள் பார்க்க மட்டுமே முடியும். சேர்க்க அல்லது மாற்றும் உரிமைக்கு உங்கள் administrator-இடம் கேளுங்கள்.
:::

::: section design-previews
title: இன்னும் உருவாக்கப்பட்டு வரும் திரைகள்
routes: /GuestBooking, /RoomDetails, /CancelledBookingDetails, /PaymentManagement
for: 
roles: 
designations: 
minutes: 1
source-hash: 0ad754e112fa7bee
confidence: ok

**இந்தத் திரை எதற்கு**
சில திரைகள் ஆரம்ப டிசைன்கள் மட்டுமே. அவை உங்கள் தகவலை அல்ல, மாறாத மாதிரித் தகவலைக் காட்டும். அவற்றின் பட்டன்கள் எதையும் சேமிக்காது.

1. மாதிரி விருந்தினர் விவரங்களுடன் **Review Your Booking**, **AC Deluxe Room** அறை விவரங்கள், **Cancelled Booking Details** அல்லது **Payment Management** திறந்திருந்தால், நீங்கள் இந்த டிசைன் திரைகளில் ஒன்றில் இருக்கிறீர்கள்.
2. அங்கே எந்தத் தகவலையும் உள்ளிடாதீர்கள், நம்பாதீர்கள்.
3. அறையை புக் செய்ய **Book a Room** பயன்படுத்துங்கள். உங்கள் புக்கிங்குகளைப் பார்க்க, ரத்து செய்ய அல்லது பின்தொடர **My Bookings / Cancellation** பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **எண்கள் ஒருபோதும் மாறுவதில்லை.** இது மாதிரித் தகவல். மேலே சொன்ன உண்மையான திரைகளைப் பயன்படுத்துங்கள்.
:::
