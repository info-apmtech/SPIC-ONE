---
module: publicity
title: விளம்பரப் பொருள் கண்காணிப்பு
icon: bi-megaphone
order: 40
language: ta
category: சந்தைப்படுத்தல்
note: புதிய தோற்றம்: கோரிக்கையின் பயணம், நிலைகள் மற்றும் படிப்படியான வழிகாட்டல்
meta-source-hash: 2abd562a1fb0baf2
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b68fc648b74d721b
confidence: ok

உங்கள் HQ-க்கு விளம்பரப் பொருள் (போஸ்டர், பேனர் போன்றவை) கேட்கலாம், அதற்கு ஒப்புதல் வாங்கலாம், பொருளைப் பெற்று அது வந்து சேர்ந்ததை உறுதி செய்யலாம். ஒவ்வொரு படியும் பதிவாகும். அதனால் ஒரு கோரிக்கை இப்போது எங்கே இருக்கிறது என்று எல்லோரும் பார்க்கலாம்.

- start | icon: bi-pencil-square | color: #0b6b3a
  title: RMDO தொடங்குவார்
  text: பொருள், product, அளவு ஆகியவற்றுடன் ஒரு கோரிக்கையைப் போடுங்கள்.
- approve | icon: bi-shield-check | color: #d97706
  title: ஐந்து ஒப்புதல்கள்
  text: RM, SMD, SMM, ADMIN-MD CS, DGM-MD CS ஆகியோர் வரிசையாகச் சரிபார்ப்பார்கள்.
- close | icon: bi-box-seam | color: #7c3aed
  title: RMDO முடித்து வைப்பார்
  text: என்ன வந்தது என்று உறுதி செய்யுங்கள். குறைவான அல்லது சேதமான பீஸ்களையும் சேர்த்துச் சொல்லுங்கள்.
:::

::: section flow
title: ஒரு கோரிக்கை எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 922a2e0da6a7ddbc
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- rmdo | type: lane | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  sub: உங்கள் HQ
- approvers | type: lane | color: #0d9488 | roles: RM, SMD, SMM
  title: ஒப்புதல் தருபவர்கள்
  sub: RM · SMD · SMM
- mdcs | type: lane | color: #ea580c | roles: MDAdmin, MDHOD | designations: ADMIN-MD CS, ADMIN MD CS, DGM-MD CS, DGM MD CS
  title: MD CS
  sub: ADMIN-MD CS · DGM-MD CS
- vendor | type: lane | color: #64748b
  title: வெண்டர்
  sub: கூரியர்
- raise | lane: rmdo | col: 1 | icon: bi-pencil-square | color: #64748b | next: approve | screen: raise-request
  title: கோரிக்கை போட்டுச் சமர்ப்பித்தல்
  status: Draft
  text: கோரிக்கைப் படிவத்தை நிரப்பி **Submit Request** அழுத்துங்கள். அதுவரை அது உங்களுக்கு மட்டுமே தெரியும் டிராஃப்ட்.
- approve | lane: approvers | col: 2 | icon: bi-hourglass-split | color: #d97706 | next: final | return: raise | screen: review-approve
  title: சரிபார்த்து ஒப்புதல் தருதல்
  status: Pending Approval
  text: RM, பிறகு SMD, பிறகு SMM கோரிக்கையைச் சரிபார்ப்பார்கள். ஒவ்வொருவரும் அதை மாற்றலாம், ஒப்புதல் தந்து அடுத்தவருக்கு அனுப்பலாம், RMDO-க்குத் திருப்பி அனுப்பலாம், அல்லது நிராகரிக்கலாம்.
- final | lane: mdcs | col: 3 | icon: bi-patch-check | color: #2563eb | next: dispatch | screen: review-approve
  title: இறுதி ஒப்புதல்
  status: Final Approved
  text: ADMIN-MD CS ஒப்புதல் தந்து அனுப்புவார். பிறகு DGM-MD CS இறுதி ஒப்புதல் தருவார்.
- dispatch | lane: mdcs | col: 4 | icon: bi-truck | color: #7c3aed | next: ship | screen: create-dispatch
  title: Dispatch உருவாக்குதல்
  status: In Dispatch
  text: ADMIN-MD CS அல்லது DGM-MD CS, வெண்டரிடமிருந்து வரும் dispatch-ஐக் கூரியர் விவரங்களுடன் பதிவு செய்வார்.
- ship | lane: vendor | col: 5 | icon: bi-box-seam | color: #0d9488 | next: ack | screen: update-delivery
  title: HQ-க்கு அனுப்புதல்
  status: Delivered
  text: பொருள் வெண்டரிடமிருந்து பயணிக்கும். MD CS அதை முதலில் **In Transit** என்றும், பிறகு **Delivered** என்றும் குறிப்பார்.
- ack | lane: rmdo | col: 6 | icon: bi-clipboard-check | color: #16a34a | screen: acknowledge-delivery
  title: பெற்றதை உறுதி செய்தல்
  status: Completed
  text: RMDO பொருளை எண்ணிப் பார்த்து **Acknowledgement** சமர்ப்பிப்பார். எல்லாம் சரியாக இருந்தால், கோரிக்கை முடிவடையும்.
- exception | lane: mdcs | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: ack | to: dispatch | screen: resolve-exception
  title: குறைவு அல்லது சேதத்தைச் சரி செய்தல்
  status: Exception Raised
  text: பீஸ்கள் குறைவாகவோ சேதமாகவோ இருந்தால், MD CS மீதியை வெண்டரிடமிருந்து அனுப்புவார், அல்லது வேறு HQ-வில் உள்ள கூடுதல் ஸ்டாக்கை மாற்றுவார்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 06524fbf96639e47
confidence: ok

- draft | color: #64748b
  label: Draft
  text: சேமிக்கப்பட்டது, ஆனால் அனுப்பப்படவில்லை. உங்களுக்கு மட்டுமே தெரியும்.
- pending | color: #d97706
  label: Pending Approval
  text: இப்போது ஒப்புதல் தருபவரிடம் உள்ளது.
- pending-final | color: #d97706
  label: Pending Final Approval
  text: கடைசி ஒப்புதலுக்காக DGM-MD CS-இடம் உள்ளது.
- returned | color: #ea580c
  label: Returned
  text: RMDO-க்குத் திருப்பி அனுப்பப்பட்டது. திருத்தி மீண்டும் சமர்ப்பியுங்கள்.
- final | color: #2563eb
  label: Final Approved
  text: ஒப்புதல் கிடைத்துவிட்டது. Dispatch-க்காகக் காத்திருக்கிறது.
- dispatch | color: #7c3aed
  label: In Dispatch
  text: வெண்டர் பொருளை அனுப்பிவிட்டார்.
- delivered | color: #0d9488
  label: Delivered
  text: பொருள் HQ-க்கு வந்து சேர்ந்தது.
- ack-pending | color: #0891b2
  label: Acknowledgement Pending
  text: என்ன வந்தது என்று RMDO உறுதி செய்யக் காத்திருக்கிறது.
- completed | color: #16a34a
  label: Completed
  text: எல்லாம் நல்ல நிலையில் வந்து சேர்ந்தது.
- exception | color: #dc2626
  label: Exception Raised
  text: பீஸ்கள் குறைவாக அல்லது சேதமாக இருந்தன. MD CS அதைச் சரி செய்கிறார்.
- transfer | color: #7c3aed
  label: Transfer Order Received
  text: கூடுதல் ஸ்டாக்கை வேறு HQ-க்கு அனுப்புமாறு உங்கள் HQ-விடம் கேட்கப்பட்டுள்ளது.
- rejected | color: #475569
  label: Rejected
  text: ஒப்புதல் தருபவர் இதை மூடிவிட்டார். தேவைப்பட்டால் புதிய கோரிக்கை போடுங்கள்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 3570dd53fa8c80d1
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- rmdo | color: #0b6b3a | roles: RMD | designations: RMDO
  title: RMDO
  - கோரிக்கைகளைப் போடுவது
  - டெலிவரியை உறுதி செய்வது
  - ஸ்டாக் transfer-க்குக் கூரியர் ஆதாரம் அனுப்புவது
- approvers | color: #0d9488 | roles: RM, SMD, SMM
  title: RM, SMD, SMM
  - உங்கள் பகுதியின் கோரிக்கைகளைச் சரிபார்ப்பது
  - ஒப்புதல் தருவது, திருப்பி அனுப்புவது அல்லது நிராகரிப்பது
- admin | color: #ea580c | roles: MDAdmin | designations: ADMIN-MD CS, ADMIN MD CS
  title: ADMIN-MD CS
  - ஒப்புதல் தந்து அடுத்த நிலைக்கு அனுப்புவது
  - Dispatch உருவாக்குவது, டெலிவரி நிலையைப் புதுப்பிப்பது
  - குறைவு மற்றும் சேதத்தைச் சரி செய்வது
- dgm | color: #7c3aed | roles: MDHOD | designations: DGM-MD CS, DGM MD CS
  title: DGM-MD CS
  - இறுதி ஒப்புதல் தருவது
  - Dispatch உருவாக்குவது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: a2321112e4f4d096
confidence: ok

- no-pencil | popular: true
  q: என் கோரிக்கையைச் சமர்ப்பித்தேன், இப்போது பென்சில் ஐகான் இல்லை
  a: சமர்ப்பித்த பிறகு, ஒப்புதல் தருபவர் மட்டுமே கோரிக்கையை உங்களுக்குத் திருப்பி அனுப்ப முடியும். அது **Returned** என்று காட்டியதும், பென்சில் மீண்டும் வரும்.
- damaged | popular: true
  q: சில பீஸ்கள் சேதமாக அல்லது குறைவாக வந்தன. நான் என்ன செய்வது?
  a: உறுதி செய்யும்போது பெற்ற அளவையும் சேத அளவையும் உள்ளிட்டு, பரவாயில்லை, சமர்ப்பியுங்கள். ஒரு exception ADMIN-MD CS-க்குப் போகும். அவர் குறைந்த பீஸ்களை அனுப்புவார்.
- who-has-it
  q: என் கோரிக்கை இப்போது யாரிடம் உள்ளது?
  a: கோரிக்கையைத் திறங்கள். மேலே **Current Stage** மற்றும் **Pending With** தெரியும். **Approval History**-ல் ஒவ்வொரு செயலும் தெரியும்.
- no-rows
  q: "No requests found" என்று வருகிறது
  a: உங்கள் ஃபில்டர்கள் ரொம்பக் குறுகலாக இருக்கலாம். **Filters**-ஐத் திறந்து **Clear All** அழுத்துங்கள்.
:::

::: section requester-list
title: என் கோரிக்கைகளைப் பார்க்கவும் நிர்வகிக்கவும்
routes: /PublicityRequest
for: 
roles: 
designations: 
screenshot: requester-list.png
hotspots: 1 @ 2.2,12.2,95.5,33.6 | 2 @ 61.6,81.1,24.3,5.1 | 3 @ 2.2,48.2,95.5,27.9 | 4 @ 4.2,87.9,91.6,12.1 | 5 @ 86.9,81.2,8.9,4.8 | 6 @ 80.7,1.9,17,4.8
screenshot-phone: requester-list.phone.jpg
hotspots-phone: 1 @ 3.1,9.9,93.8,35.4 | 2 @ 6.9,86.1,59.1,3 | 3 @ 3.1,46.3,93.8,35.1 | 4 @ 6.9,90,86.2,10 | 5 @ 68.5,86.2,24.5,2.8 | 6 @ 17.4,5.4,46.7,2.8
tour: 1 > [data-help=pub-cards] | 2 > [data-help=pub-search] | 3 > [data-help=pub-filters] | 4 > [data-help=pub-table] | 5 > [data-help=pub-export] | 6 > [data-help=pub-new]
minutes: 2
source-hash: 08092b10939e59ef
confidence: ok

**இந்தத் திரை எதற்கு**
**My Publicity Material Request** திரையில் உங்கள் HQ-வின் எல்லாக் கோரிக்கைகளும் தெரியும். மேலே எண்ணிக்கைகளும், கீழே ஒரு அட்டவணையும் இருக்கும்.

1. மேலே உள்ள **Draft**, **Pending Approval**, **Completed** போன்ற கார்டுகளைப் பாருங்கள். ஒரு கார்டைத் தட்டினால் அந்தக் கோரிக்கைகள் மட்டும் தெரியும். மீண்டும் தட்டினால் எல்லாமே தெரியும்.
2. ஒரு கோரிக்கையை வேகமாகக் கண்டுபிடிக்க, தேடல் பெட்டியில் indent எண், product அல்லது பொருளின் பெயரை டைப் செய்யுங்கள்.
3. **Filters**-ஐத் திறந்து **Date Range**, **Product**, **Publicity Material** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
4. **Action** நெடுவரிசையில், கண் ஐகான் கோரிக்கையைத் திறக்கும். பென்சில் ஒரு **Draft** அல்லது **Returned** கோரிக்கையைத் திருத்தும். குப்பைத்தொட்டி ஒரு **Draft**-ஐ நீக்கும்.
5. பட்டியலை Excel கோப்பாகப் பதிவிறக்க **Export** அழுத்துங்கள்.
6. புதிய கோரிக்கை தொடங்க **Create New Request** அழுத்துங்கள்.

> Careful: டிராஃப்டை நீக்கினால் அதைத் திரும்பப் பெற முடியாது.

## பொதுவான பிரச்சனைகள்

- **"No requests found" என்று வருகிறது.** உங்கள் ஃபில்டர்கள் ரொம்பக் குறுகலாக இருக்கலாம். **Clear All** அழுத்துங்கள்.
- **பென்சில் ஐகான் இல்லை.** சமர்ப்பித்த பிறகு, ஒப்புதல் தருபவர் கோரிக்கையைத் திருப்பி அனுப்பினால் மட்டுமே அதை மாற்ற முடியும்.
- **Export பட்டன் சாம்பல் நிறத்தில் உள்ளது.** பட்டியலில் பதிவிறக்க எந்த வரிசையும் இல்லை.
:::

::: section raise-request
title: விளம்பரக் கோரிக்கை போடுவது
routes: /PublicityRequest/new, /PublicityRequest/edit/{id}
for: 
roles: 
designations: 
screenshot: raise-request.png
hotspots: 1 @ 4.2,20.2,21.5,8.5 | 2 @ 50.9,20.2,21.5,8.5 | 3 @ 4.2,30.4,91.6,20.7 | 4 @ 2.2,73.6,95.5,19.1 | 5 @ 2.2,95,95.5,4.8
screenshot-phone: raise-request.phone.jpg
hotspots-phone: 1 @ 6.9,15.5,86.2,4.9 | 2 @ 6.9,27.4,86.2,4.9 | 3 @ 6.9,39.2,86.2,14.7 | 4 @ 3.1,66.1,93.8,23.6 | 5 @ 0,87.5,100,8.4
tour: 1 > [data-help=pub-material] | 2 > [data-help=pub-qty] | 3 > [data-help=pub-design] | 4 > [data-help=pub-location] | 5 > [data-help=pub-submit]
minutes: 3
source-hash: 74e6a60af7702e79
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் HQ-க்கு விளம்பரப் பொருள் கேட்க **Create Publicity Material Request** திரையைப் பயன்படுத்துங்கள். டிராஃப்டாகச் சேமித்து, பிறகு அனுப்பலாம்.

1. **Request Details**-ன் கீழ், **Type of Publicity Material** மற்றும் **Product** தேர்வு செய்யுங்கள்.
2. உங்களுக்குத் தேவையான **Quantity**-ஐ உள்ளிடுங்கள். **Requested By**-ல் உங்கள் பெயர் ஏற்கனவே இருக்கும்.
3. உங்களிடம் டிசைன் இருந்தால், அந்தக் கோப்பை அப்லோட் பெட்டியில் போடுங்கள், அல்லது **or click to browse** தட்டுங்கள். தேவைப்பட்டால் **Remarks** சேருங்கள்.
4. **Location**-ன் கீழ், **State**, **Region** மற்றும் **Headquarter (HQ)**-ஐச் சரிபாருங்கள். இவை உங்கள் புரொஃபைலில் இருந்து தானாக நிரப்பப்படும்.
5. பிறகு முடிக்க வைத்துக்கொள்ள **Save as Draft** அழுத்துங்கள், அல்லது RM-க்கு அனுப்ப **Submit Request** அழுத்தி, பிறகு **Submit** அழுத்துங்கள்.

> Tip: டிராஃப்டைப் பட்டியலில் இருந்து மீண்டும் திறந்து, பிறகு முடிக்கலாம்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Type of Publicity Material** | உங்களுக்குத் தேவையான பொருள், உதாரணமாக போஸ்டர் அல்லது பேனர். கட்டாயம். |
| **Product** | இந்தப் பொருள் எந்த product-க்கானது. டைப் செய்து தேடலாம். கட்டாயம். |
| **Quantity** | தேவையான பீஸ்களின் எண்ணிக்கை. கட்டாயம். |
| **Design file** | ஒரே ஒரு கோப்பு, PDF, JPG, PNG, DOC அல்லது DOCX, 10 MB வரை. |
| **State**, **Region**, **Headquarter (HQ)** | பொருள் எங்கே டெலிவரி ஆக வேண்டும். கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"Select the state, region and headquarter."** (மாநிலம், மண்டலம், HQ-ஐத் தேர்வு செய்யுங்கள்) முதலில் State, பிறகு Region, பிறகு HQ தேர்வு செய்யுங்கள்.
- **"is larger than 10 MB"** (10 MB-ஐ விடப் பெரியது) கோப்பைச் சிறியதாக்குங்கள், அல்லது புதிய புகைப்படம் எடுங்கள். பிறகு மீண்டும் அப்லோட் செய்யுங்கள்.
- **சமர்ப்பித்த பிறகு என்னால் மாற்ற முடியவில்லை.** இது சாதாரணம்தான். திருப்பி அனுப்பப்பட்ட கோரிக்கையை மட்டுமே மீண்டும் மாற்ற முடியும்.
:::

::: section track-request
title: என் கோரிக்கையைப் பின்தொடர்வது
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
screenshot: track-request.png
screenshot-phone: track-request.phone.jpg
minutes: 2
source-hash: 533996d4d473e58d
confidence: ok

**இந்தத் திரை எதற்கு**
**My Publicity Material Request Details** திரையில் ஒரு கோரிக்கையின் எல்லா விவரங்களும் தெரியும்: அது இப்போது எங்கே இருக்கிறது, யார் ஒப்புதல் தந்தார்கள், dispatch விவரம், ஆவணங்கள்.

1. பட்டியலில் கண் ஐகானைத் தட்டி கோரிக்கையைத் திறங்கள்.
2. மேலே **Current Stage**, **Status**, இப்போது கோரிக்கை யாரிடம் இருக்கிறது (**Pending With**) என்பதைப் பாருங்கள்.
3. **Request Details & History**-ல், **Request Timeline**-ஐயும், ஒவ்வொரு செயலும் குறிப்பும் உள்ள **Approval History** அட்டவணையையும் பாருங்கள்.
4. ஒவ்வொரு dispatch-ஐயும் பார்க்க **Tracking & Delivery**-ஐத் திறங்கள்: வெண்டர், டிரான்ஸ்போர்ட்டர், டிராக்கிங் எண், டெலிவரி நிலை.
5. பொருள் வந்து சேர்ந்ததும், அதை உறுதி செய்ய **Acknowledgement**-ஐத் திறங்கள்.
6. அப்லோட் செய்த எல்லாக் கோப்புகளையும் பார்க்க **Related Documents**-ஐத் திறங்கள்.
7. கோரிக்கையின் PDF வேண்டுமென்றால் **Download Report** அழுத்துங்கள், அச்சிட **Print** அழுத்துங்கள்.
8. கோரிக்கை **Returned** ஆகியிருந்தால், **Edit & Resubmit** அழுத்தி, திருத்தி, மீண்டும் சமர்ப்பியுங்கள்.

## பொதுவான பிரச்சனைகள்

- **Tracking & Delivery-ல் "Not dispatched yet" என்று வருகிறது.** பொருள் இன்னும் ஒப்புதல் நிலையில் உள்ளது, அல்லது dispatch இன்னும் செய்யப்படவில்லை. **Current Stage**-ஐப் பாருங்கள்.
- **Status-ல் "Rejected" என்று காட்டுகிறது.** கோரிக்கை மூடப்பட்டுவிட்டது. காரணத்தை **Approval History**-ல் படியுங்கள். தேவைப்பட்டால் புதிய கோரிக்கை போடுங்கள்.
- **எனக்கு Edit & Resubmit தெரியவில்லை.** கோரிக்கை உங்களுக்குத் திருப்பி அனுப்பப்பட்டால் மட்டுமே அது தெரியும்.
:::

::: section acknowledge-delivery
title: பெற்ற பொருளை உறுதி செய்வது
routes: /PublicityRequest/{id}
for: 
roles: 
designations: 
minutes: 3
source-hash: 2e6ce69978f16d26
confidence: ok

**இந்தத் திரை எதற்கு**
பொருள் உங்கள் HQ-க்கு வந்ததும், எத்தனை பீஸ் வந்தது, அவை எந்த நிலையில் உள்ளன என்று நீங்கள் உறுதி செய்ய வேண்டும். இதனால் கோரிக்கை மூடப்படும், அல்லது பிரச்சனை இருந்தால் ADMIN-MD CS-க்குத் தெரிவிக்கப்படும்.

1. கோரிக்கையைத் திறந்து **Tracking & Delivery**-க்குப் போங்கள்.
2. **Awaiting Acknowledgement** பெட்டியில் **Acknowledge** அழுத்துங்கள். இது **Acknowledgement** டேபைத் திறக்கும்.
3. **Dispatch Summary**-ஐச் சரிபாருங்கள் (dispatch அளவு மற்றும் தேதி).
4. **Quantity Received**-ஐ உள்ளிடுங்கள்.
5. **In Good Condition** அல்லது **In Damaged Condition** தேர்வு செய்யுங்கள். சேதம் இருந்தால் **Damaged Quantity**-ஐ உள்ளிடுங்கள்.
6. பொருளைச் சரிபார்த்தவர் யார் என்று **Acknowledged By**-ல் தேர்வு செய்யுங்கள்.
7. **Remarks** எழுதி, பொருளின் புகைப்படங்களை அப்லோட் செய்யுங்கள்.
8. **Exception Details** பெட்டி வந்தால் அதைப் பாருங்கள். அதில் குறைவு, அதிகம் அல்லது சேதம் இருந்தால் தெரியும்.
9. பிறகு முடிக்க **Save as Draft** அழுத்துங்கள், அல்லது **Submit Acknowledgement** அழுத்தி, பிறகு **Submit** அழுத்துங்கள்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Quantity Received** | உண்மையில் எண்ணிய பீஸ்கள். கட்டாயம். |
| **Material Condition** | நல்ல நிலை அல்லது சேதம். கட்டாயம். |
| **Damaged Quantity** | 1 முதல் பெற்ற அளவு வரை. |
| **Acknowledged By** | பொருளைப் பெற்ற நபர். கட்டாயம். |
| **Photos** | பெற்ற பொருளின் புகைப்படங்கள், ஒவ்வொன்றும் 10 MB வரை. |

## பொதுவான பிரச்சனைகள்

- **"Damaged quantity must be between 1 and the received quantity."** (சேத அளவு 1-க்கும் பெற்ற அளவுக்கும் இடையில் இருக்க வேண்டும்) எண்களை மீண்டும் சரிபாருங்கள்.
- **குறைவு அல்லது சேதம் கண்டுபிடிக்கப்பட்டது.** பரவாயில்லை, சமர்ப்பியுங்கள். ஒரு exception ADMIN-MD CS-க்குப் போகும். அவர் குறைந்த பீஸ்களை அனுப்புவார்.
- **Acknowledge பட்டன் இல்லை.** பொருள் இன்னும் dispatch ஆகவில்லை, அல்லது ஏற்கனவே உறுதி செய்யப்பட்டுவிட்டது.
:::

::: section transfer-proof
title: கூடுதல் ஸ்டாக்கை வேறு HQ-க்கு அனுப்புவது
routes: /PublicityRequest/{id}/transfer/{dispatchId}, /PublicityRequest/done
for: 
roles: 
designations: 
minutes: 3
source-hash: e7a33ab19f8314b5
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் HQ-க்குத் தேவையை விட அதிகப் பொருள் வந்திருந்தால், அதில் கொஞ்சத்தைக் குறைவாக உள்ள வேறு HQ-க்கு அனுப்பச் சொல்லி ADMIN-MD CS கேட்கலாம். நீங்கள் அதைக் கூரியரில் அனுப்பி, கூரியர் ரசீதை இங்கே அப்லோட் செய்ய வேண்டும்.

1. **Notifications**-ஐத் திறந்து ஸ்டாக் transfer order-ஐத் தட்டி, **View Transfer** அழுத்துங்கள். அல்லது உங்கள் கோரிக்கையைத் திறந்து, **Tracking & Delivery**-க்குப் போய், **Stock Transfer Order** கார்டில் **Create Transfer** அழுத்தலாம்.
2. **Read Information (Read Only)** பகுதியைச் சரிபாருங்கள்: **Destination HQ** மற்றும் **Transfer Quantity**.
3. பொருளை பேக் செய்து கூரியரில் அனுப்புங்கள்.
4. **Upload Courier / Dispatch Proof**-ன் கீழ், கூரியர் ரசீதின் தெளிவான புகைப்படத்தை அப்லோட் செய்யுங்கள்.
5. ஆப் ரசீதைப் படித்து **Courier / Transporter**, **Tracking No. / AWB**, **Dispatch Date** ஆகியவற்றைத் தானாக நிரப்பும். அவற்றைச் சரிபார்த்து, தேவைப்பட்டால் திருத்துங்கள்.
6. உங்களிடம் இருந்தால் **Vehicle No** மற்றும் **Remarks (Optional)** சேருங்கள்.
7. **Submit Proof to Admin** அழுத்துங்கள்.
8. **Dispatch Proof Submitted Successfully!** திரை திறக்கும். **Go to Tracking & Delivery** அல்லது **Back to List** அழுத்துங்கள்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Courier / Transporter** | கூரியர் நிறுவனத்தின் பெயர். கட்டாயம். |
| **Tracking No. / AWB** | கூரியர் ரசீதில் உள்ள எண். கட்டாயம். |
| **Dispatch Date** | நீங்கள் அனுப்பிய தேதி. கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"Upload the courier / dispatch proof."** (கூரியர் / dispatch ஆதாரத்தை அப்லோட் செய்யுங்கள்) சமர்ப்பிக்கும் முன் ரசீதை அப்லோட் செய்யுங்கள்.
- **விவரங்கள் தானாக நிரப்பப்படவில்லை.** புகைப்படம் மங்கலாக இருக்கலாம், அல்லது அது PDF ஆக இருக்கலாம். விவரங்களை நீங்களே டைப் செய்யுங்கள்.
- **"Transfer order not found".** (transfer order கிடைக்கவில்லை) ஆதாரம் ஏற்கனவே சமர்ப்பிக்கப்பட்டுவிட்டது. அதை மாற்ற, **Tracking & Delivery** டேபில் உள்ள **Update Proof**-ஐப் பயன்படுத்துங்கள்.
:::

::: section requester-notifications
title: என் விளம்பர அறிவிப்புகள்
routes: /PublicityRequest/notifications
for: 
roles: 
designations: 
screenshot: requester-notifications.png
hotspots: 1 @ 4.2,14.7,91.6,7 | 2 @ 4.2,23.6,91.6,76.4 | 3 @ 4.2,23.6,91.6,76.4 | 4 @ 84.5,1.9,13.3,4.8 | 5 @ 68.7,1.9,15.1,4.8
screenshot-phone: requester-notifications.phone.jpg
hotspots-phone: 1 @ 6.9,11,86.2,3.7 | 2 @ 6.9,15.8,86.2,84.2 | 3 @ 6.9,15.8,86.2,84.2 | 4 @ 46.4,5.4,36.4,2.8 | 5 @ 3.1,5.4,41.3,2.8
tour: 1 > [data-help=pub-notif-tabs] | 2 > [data-help=pub-notif-list] | 3 > [data-help=pub-notif-list] | 4 > [data-help=pub-notif-readall] | 5 > [data-help=pub-notif-back]
minutes: 1
source-hash: 4cd27f470cab35ac
confidence: ok

**இந்தத் திரை எதற்கு**
**Notifications** திரையில் உங்கள் கோரிக்கைகள் பற்றிய அறிவிப்புகள் இருக்கும்: திருப்பி அனுப்பியவை, dispatch-கள், exception-கள், ஸ்டாக் transfer order-கள். பட்டியல் பக்கத்தில் **Notifications** அழுத்தி இதைத் திறங்கள்.

1. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All**, **Stock Transfer**, **Exception** அல்லது **Dispatch & Approvals**.
2. படிக்காதவற்றுக்கு ஒரு புள்ளி இருக்கும். விவரங்களைத் திறக்க ஒன்றைத் தட்டுங்கள்.
3. Transfer order என்றால், கூரியர் ஆதாரத்தை அப்லோட் செய்ய **View Transfer** அழுத்துங்கள். கோரிக்கையைத் திறக்க **View Details** அழுத்துங்கள்.
4. ஒன்றை மட்டும் படித்ததாகக் குறிக்க **Mark As Read** அழுத்துங்கள், எல்லாவற்றுக்கும் **Mark All As Read** அழுத்துங்கள்.
5. திரும்பிப் போக **Back to List Page** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No notifications".** (அறிவிப்புகள் இல்லை) உங்கள் கோரிக்கைகளில் இதுவரை புதிதாக எதுவும் நடக்கவில்லை.
- **Mark All As Read சாம்பல் நிறத்தில் உள்ளது.** எல்லாமே ஏற்கனவே படிக்கப்பட்டுவிட்டன.
:::

::: section reviewer-list
title: என் பரிசீலனைக்கு வந்த கோரிக்கைகளைப் பார்ப்பது
routes: /PublicityReview
for: 
roles: 
designations: 
screenshot: reviewer-list.png
screenshot-phone: reviewer-list.phone.jpg
minutes: 2
source-hash: 7eaa380f878c5c45
confidence: ok

**இந்தத் திரை எதற்கு**
ஒப்புதல் தருபவர்களும் MD CS பணியாளர்களும் தங்கள் பகுதியின் கோரிக்கைகளைப் பார்க்க இந்தப் பட்டியலைப் பயன்படுத்துவார்கள். தலைப்பு **Submitted Requests** என்று இருக்கும் (RM-க்கு **My Publicity Material Request** என்று இருக்கும்).

1. மேலே உள்ள கார்டுகளைப் பாருங்கள். RM-க்கு **Pending for My Approval**, **Approved by RM**, **Dispatch & Delivery**, **Completed** மற்றும் **Returned** தெரியும். மற்றவர்களுக்கு **Total Submitted**, **Pending Review**, **Dispatch Pending**, **Exceptions** மற்றும் **Completed** போன்ற கார்டுகள் தெரியும்.
2. சிவப்பு நிற **New Request for Review** பெட்டி வந்தால், **View New Requests** அழுத்துங்கள். RM, SMD, SMM, ADMIN-MD CS ஆகியோருக்கு இது consolidation திரையைத் திறக்கும்.
3. **Filters**-ஐத் திறந்து **Date Range**, **Product**, **Publicity Material**, **State**, **Region**, **HQ**, **Current Stage** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள்.
4. உங்களுக்காகக் காத்திருக்கும் வரிசைகளில் **Status** நெடுவரிசையில் **Pending** என்று இருக்கும். நீங்கள் ஏற்கனவே அனுப்பியவற்றில் **Approved** என்று இருக்கும்.
5. கோரிக்கையைத் திறக்க கண் ஐகானைத் தட்டுங்கள்.
6. பட்டியலை Excel-ஆகப் பதிவிறக்க **Export** அழுத்துங்கள்.
7. உங்கள் நிலையில் காத்திருக்கும் எல்லாக் கோரிக்கைகளையும் மண்டல வாரியாகப் பார்க்க **Consolidation** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **எனக்கு State அல்லது Region ஃபில்டர்கள் தெரியவில்லை.** RM, SMD, SMM தங்கள் சொந்தப் பகுதியை மட்டுமே பார்ப்பார்கள். அதனால் சில ஃபில்டர்கள் மறைக்கப்பட்டிருக்கும்.
- **பட்டியல் காலியாக உள்ளது.** Filters-ல் **Clear All** அழுத்துங்கள். இதுவரை எந்தக் கோரிக்கையும் உங்களிடம் வராமல் இருக்கலாம்.
:::

::: section review-approve
title: கோரிக்கையைப் பரிசீலித்து ஒப்புதல் தருவது
routes: /PublicityReview/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: 3d13e1d79363e813
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரு கோரிக்கையைத் திறந்து சரிபார்த்து, ஒப்புதல் தரலாம், மாற்றலாம், அல்லது நிராகரிக்கலாம். கோரிக்கை உங்களுக்காகக் காத்திருக்கும்போது மட்டுமே பட்டன்கள் தெரியும்.

1. பட்டியலிலிருந்து அல்லது consolidation திரையிலிருந்து கோரிக்கையைத் திறங்கள்.
2. மேலே உள்ள பட்டை, **Request Timeline**, **Approval History** ஆகியவற்றைச் சரிபாருங்கள்.
3. ஏதாவது குறிப்பு இருந்தால் **Additional Remarks (if any)**-ல் டைப் செய்யுங்கள்.
4. முதலில் பொருள், அளவு அல்லது இடத்தை மாற்ற வேண்டுமென்றால் **Edit** அழுத்துங்கள்.
5. ஒப்புதல் தர, அடுத்த நிலைக்கான **Approve & Forward to** பட்டனை அழுத்துங்கள் (உதாரணமாக **Approve & Forward to SMD**). பிறகு **Approve & Forward** அழுத்தி உறுதி செய்யுங்கள்.
6. நிராகரிக்க, **Reject** அழுத்தி, **Reason for rejection** எழுதி, மீண்டும் **Reject** அழுத்துங்கள்.

> Careful: Reject செய்தால் கோரிக்கை நிரந்தரமாக மூடப்படும். RMDO புதிய கோரிக்கை போட வேண்டும்.

## ஒப்புதல் வரிசை

- RM, SMD-க்கு அனுப்புவார். SMD, SMM-க்கு. SMM, ADMIN - MD CS-க்கு. ADMIN-MD CS, DGM - MD CS-க்கு.
- DGM-MD CS இறுதி ஒப்புதல் தருவார். இந்த நிலையில் **Edit** இல்லை.

## பொதுவான பிரச்சனைகள்

- **Approve பட்டன் இல்லை, தலைப்பில் "Request Details" என்று உள்ளது.** கோரிக்கை இப்போது உங்கள் நிலையில் இல்லை. நீங்கள் பார்க்க, **Print** செய்ய அல்லது **Download Report** செய்ய மட்டுமே முடியும்.
- **"Enter the reason for rejection."** (நிராகரிப்பதற்கான காரணத்தை எழுதுங்கள்) காரணம் கட்டாயம்.
:::

::: section reviewer-edit
title: அனுப்பும் முன் கோரிக்கையைத் திருத்துவது
routes: /PublicityReview/edit/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: e76c7e9756536fde
confidence: ok

**இந்தத் திரை எதற்கு**
**Edit Publicity Material Request** திரையில், யாருடைய முறையோ அந்த ஒப்புதல் தருபவர் கோரிக்கையைத் திருத்தலாம். மாற்றத்தைச் சேமித்தால், அதுவே ஒப்புதல் தந்து அடுத்த நிலைக்கு அனுப்பிவிடும்.

1. கோரிக்கையில் **Edit** அழுத்துங்கள். Consolidation திரையில் பென்சில் ஐகானைத் தட்டுங்கள்.
2. **Type of Publicity Material**, **Product**, **Quantity**, **Requested By** அல்லது **Remarks**-ஐ மாற்றுங்கள்.
3. தேவைப்பட்டால் டிசைன் கோப்பைச் சேருங்கள் அல்லது நீக்குங்கள்.
4. **State**, **Region**, **Headquarter (HQ)**-ஐச் சரிபாருங்கள்.
5. அடுத்த நிலைக்கான **Save & Forward to** பட்டனை அழுத்துங்கள் (உதாரணமாக **Save & Forward to SMM**).
6. மாற்றம் சேமிக்கப்படும், **Approval History**-ல் எழுதப்படும், கோரிக்கை அடுத்த நிலைக்குப் போகும்.

## பொதுவான பிரச்சனைகள்

- **திருத்தும் பக்கத்துக்குப் பதிலாக விவரங்கள் பக்கம் திறக்கிறது.** கோரிக்கை இப்போது உங்களுக்காகக் காத்திருக்கவில்லை.
- **நான் சேமிக்க மட்டும் நினைத்தேன், அனுப்ப அல்ல.** இங்கே சேமித்தால் எப்போதும் அடுத்த நிலைக்குப் போய்விடும். இப்போதே அனுப்ப வேண்டாமென்றால் **Back to List** பயன்படுத்துங்கள்.
:::

::: section consolidation
title: பல கோரிக்கைகளுக்கு ஒன்றாக ஒப்புதல் தருவது
routes: /PublicityReview/consolidation
for: 
roles: 
designations: 
screenshot: consolidation.png
screenshot-phone: consolidation.phone.jpg
minutes: 3
source-hash: abde9049ef5f5976
confidence: ok

**இந்தத் திரை எதற்கு**
Consolidation திரையில், உங்கள் நிலையில் காத்திருக்கும் எல்லாக் கோரிக்கைகளும் மண்டல வாரியாகத் தெரியும். அதனால் எல்லாவற்றுக்கும் ஒரே முறையில் ஒப்புதல் தரலாம். RM தன் மண்டலத்தைப் பார்ப்பார், SMD மற்றும் SMM மாநிலத்தைப் பார்ப்பார்கள், ADMIN-MD CS முழு இந்தியாவையும் பார்ப்பார். DGM-MD CS இந்தத் திரையைப் பயன்படுத்துவதில்லை.

1. பட்டியல் பக்கத்தில் **Consolidation** அழுத்துங்கள்.
2. கார்டுகளைச் சரிபாருங்கள்: **Submission ID**, **Region** அல்லது **State**, **Total Region**, **Total Requests** மற்றும் **Total Requirement**.
3. அட்டவணையில், ஒரு மண்டலத்தைத் திறந்து அதன் HQ கோரிக்கைகளைப் பார்க்க, **Action**-ல் உள்ள அம்புக்குறியைத் தட்டுங்கள்.
4. கோரிக்கையைப் பார்க்க கண் ஐகானையும், திருத்த பென்சில் ஐகானையும் தட்டுங்கள்.
5. கீழே உள்ள பெட்டியைப் படியுங்கள். எத்தனை மண்டலங்கள் உங்களிடம் வந்துள்ளன என்று அது காட்டும்.
6. எல்லா மண்டலங்களும் உங்களிடம் வந்ததும், அடுத்த நிலைக்கான **Approve & Submit to** பட்டனை அழுத்தி, பிறகு **Approve & Submit** அழுத்தி உறுதி செய்யுங்கள்.
7. அட்டவணையைப் பதிவிறக்க **Export** அழுத்துங்கள், அல்லது பட்டியலுக்குத் திரும்ப **View Submitted Request** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Approve & Submit சாம்பல் நிறத்தில் உள்ளது.** சில மண்டலங்கள் இன்னும் முந்தைய நிலையில் நிலுவையில் உள்ளன. எல்லா மண்டலங்களும் உங்களிடம் வரும் வரை காத்திருங்கள்.
- **"Nothing waiting for you".** (உங்களுக்காக எதுவும் காத்திருக்கவில்லை) இப்போது எந்தக் கோரிக்கையும் உங்கள் நிலையில் இல்லை.
- **Request Qty-யும் Approved Qty-யும் வேறுபடுகின்றன.** ஒப்புதல் தருபவர் ஒருவர் அளவை மாற்றியுள்ளார். கோரிக்கையில் உள்ள **Approval History**-ஐப் பாருங்கள்.
:::

::: section create-dispatch
title: Dispatch உருவாக்குவது
routes: /PublicityReview/{id}/dispatch, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3
source-hash: 039c89aba91e084f
confidence: ok

**இந்தத் திரை எதற்கு**
இறுதி ஒப்புதலுக்குப் பிறகு, ADMIN-MD CS அல்லது DGM-MD CS வெண்டரிடமிருந்து வரும் dispatch-ஐ **Create Dispatch** திரையில் பதிவு செய்வார். மாற்றுப் பொருளுக்கான (மீண்டும் தொடங்கும்) dispatch-க்கும் இதே திரைதான்.

1. **Final Approved** கோரிக்கையைத் திறந்து **Create Dispatch** அழுத்துங்கள்.
2. **Read Information (Read Only)** பகுதியைச் சரிபாருங்கள்.
3. **Feed Logistics & Dispatch Data**-ன் கீழ், **Dispatch Date**, **Dispatch Quantity**, **Vendor** மற்றும் **Transporter / Courier** நிரப்புங்கள்.
4. தெரிந்தால் **Vehicle No**, **Tracking No.**, **Expected Delivery Date** சேருங்கள்.
5. **Responsible SMDO / RM / RMDO / MO**-ஐத் தேர்வு செய்யுங்கள்.
6. **Dispatch / Note Approval Document**-ஐ அப்லோட் செய்யுங்கள். உங்களிடம் இருந்தால் டிரான்ஸ்போர்ட் சீட்டு அல்லது E-way bill-ஐயும் சேருங்கள்.
7. **Delivery Address**-ஐத் தேர்வு செய்யுங்கள் அல்லது டைப் செய்யுங்கள்.
8. **Confirm and Create Dispatch** அழுத்துங்கள்.
9. **Dispatch Created Successfully!** திரை திறக்கும். **Go to Tracking & Delivery** அழுத்துங்கள்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Dispatch Quantity** | இப்போது அனுப்பும் பீஸ்கள். மாற்றுப் பொருள் என்றால், நிலுவையில் உள்ள அளவிலிருந்து தொடங்கும். |
| **Vendor** | பட்டியலில் இருந்து தேர்வு செய்யுங்கள், அல்லது புதிய பெயரை டைப் செய்யுங்கள். கட்டாயம். |
| **Responsible SMDO / RM / RMDO / MO** | பொருளைப் பெறும் அதிகாரி. கட்டாயம். |
| **Dispatch / Note Approval Document** | முதல் dispatch-க்குக் கட்டாயம். |
| **Delivery Address** | சேமித்த முகவரியைத் தட்டுங்கள், அல்லது ஒன்றை டைப் செய்யுங்கள். கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"Upload the dispatch / note approval document."** (dispatch / note approval ஆவணத்தை அப்லோட் செய்யுங்கள்) உறுதி செய்யும் முன் அதை அப்லோட் செய்யுங்கள்.
- **Create Dispatch பட்டன் இல்லை.** கோரிக்கை இன்னும் **Final Approved** ஆகவில்லை.
:::

::: section update-delivery
title: Dispatch மற்றும் டெலிவரி நிலையைப் புதுப்பிப்பது
routes: /PublicityReview/{id}
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 2
source-hash: 70a3a9c616c85eba
confidence: ok

**இந்தத் திரை எதற்கு**
Dispatch செய்த பிறகு, பொருள் எங்கே இருக்கிறது என்று RMDO-வும் ஒப்புதல் தருபவர்களும் பார்க்க, MD CS அதன் முன்னேற்றத்தைப் புதுப்பிப்பார்.

1. கோரிக்கையைத் திறந்து **Tracking & Delivery**-க்குப் போங்கள்.
2. Dispatch கார்டைக் கண்டுபிடியுங்கள் (**Dispatch 1**, **Dispatch 2** இப்படி).
3. பொருள் வெண்டரிடமிருந்து கிளம்பியதும் **Mark In Transit** அழுத்துங்கள். **Expected Delivery Date** மற்றும் **Current Location / Hub** சேர்த்து, **Save** அழுத்துங்கள்.
4. பொருள் HQ-க்கு வந்ததும் **Mark Delivered** அழுத்துங்கள். தேவைப்பட்டால் **Current Location / Hub** சேர்த்து, **Save** அழுத்துங்கள்.
5. ஸ்டாக் transfer என்றால், அனுப்பும் (source) RMDO கூரியர் ஆதாரத்தை அப்லோட் செய்த பிறகு **Update Dispatch Details** அழுத்துங்கள். **Transporter / Courier** மற்றும் **Tracking No. (AWB)**-ஐச் சரிபார்த்து, **Save** அழுத்துங்கள். அதன் பிறகு transfer-ல் **Transfer Created** என்று காட்டும்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Transporter / Courier** மற்றும் **Tracking No. (AWB)** | Transfer-ஐ உறுதி செய்யும்போது கட்டாயம். |
| **Expected Delivery Date** | பொருள் எப்போது வந்து சேர வேண்டும். |
| **Current Location / Hub** | பொருள் இப்போது எங்கே இருக்கிறது, உதாரணமாக ஒரு கூரியர் hub. |

## பொதுவான பிரச்சனைகள்

- **Dispatch கார்டில் பட்டன்கள் இல்லை.** Dispatch ஏற்கனவே உறுதி செய்யப்பட்டுவிட்டது, அல்லது source RMDO-விடமிருந்து transfer ஆதாரம் இன்னும் வரவில்லை ("Courier proof is pending from the source RMDO").
- **RMDO-வால் உறுதி செய்ய முடியவில்லை.** முதலில் பொருள் dispatch ஆக வேண்டும்.
:::

::: section resolve-exception
title: குறைவு அல்லது சேதம் exception-ஐச் சரி செய்வது
routes: /PublicityReview/{id}, /PublicityReview/done
for: 
roles: 
designations: ADMIN MD CS, ADMIN-MD CS, DGM MD CS, DGM-MD CS
minutes: 3
source-hash: 981f49f742015d88
confidence: ok

**இந்தத் திரை எதற்கு**
RMDO குறைவான பீஸ் அல்லது சேதமான பீஸ் என்று தெரிவித்தால், கோரிக்கையில் **Exception Raised** என்று காட்டும். குறைந்த அளவை எப்படி அனுப்புவது என்று நீங்கள் முடிவு செய்வீர்கள்.

1. கோரிக்கையைத் திறந்து (பட்டியலில் உள்ள **Exceptions** கார்டு இவற்றைக் கண்டுபிடிக்க உதவும்) **Tracking & Delivery**-க்குப் போங்கள்.
2. சிவப்பு நிற **Exception Raised** பெட்டியில், **Exception Type**, **Raised By**, **Remarks** ஆகியவற்றைப் படியுங்கள்.
3. **Resolve Exception** அழுத்துங்கள். பெட்டியில் நிலுவையில் உள்ள அளவு தெரியும்.
4. மீதியை வெண்டரிடமிருந்து அனுப்ப, **Re-initiate dispatch from the vendor** தேர்வு செய்து, **Continue to Dispatch** அழுத்தி, மாற்றுப் பொருளுக்கான dispatch படிவத்தை நிரப்புங்கள்.
5. வேறு HQ-வின் கூடுதல் ஸ்டாக்கை மாற்ற, **Stock transfer from another HQ's excess** தேர்வு செய்து, **Source Request (Excess)**-ஐத் தேர்வு செய்து, **Transfer Quantity**-ஐச் சரிபார்த்து, **Remarks** சேர்த்து, **Send Transfer Order** அழுத்துங்கள்.
6. Transfer என்றால், **Transfer Order Sent Successfully** திரை திறக்கும். ஸ்டாக்கை அனுப்பி கூரியர் ஆதாரம் அப்லோட் செய்யுமாறு source HQ-வின் RMDO-க்குத் தெரிவிக்கப்படும்.
7. ஆதாரம் வந்ததும், transfer கார்டில் உள்ள **Update Dispatch Details** மூலம் அதை உறுதி செய்யுங்கள்.

## பொதுவான பிரச்சனைகள்

- **ஸ்டாக் transfer வழி சாம்பல் நிறத்தில் உள்ளது ("no excess available").** வேறு எந்த HQ-விடமும் கூடுதல் ஸ்டாக் இல்லை. வெண்டர் வழியைப் பயன்படுத்துங்கள்.
- **"Select the source request and quantity."** (source கோரிக்கையையும் அளவையும் தேர்வு செய்யுங்கள்) ஒரு source-ஐத் தேர்வு செய்து, பூஜ்ஜியத்தை விட அதிகமான அளவை உள்ளிடுங்கள்.
- **Excess only.** (அதிகம் மட்டும்) RMDO-க்குக் கூடுதல் பீஸ்கள் வந்திருந்தால், கோரிக்கை முடிந்ததாகும். அந்தக் கூடுதல் ஸ்டாக்கைப் பிறகு வேறு HQ-க்குப் பயன்படுத்தலாம்.
:::

::: section reviewer-notifications
title: ஒப்புதல் தருபவர் மற்றும் MD CS அறிவிப்புகள்
routes: /PublicityReview/notifications
for: 
roles: 
designations: 
minutes: 1
source-hash: 3fcc7d3b610404d1
confidence: ok

**இந்தத் திரை எதற்கு**
**Notifications** திரையில் உங்கள் பகுதியின் கோரிக்கைகள் பற்றிய அறிவிப்புகள் இருக்கும்: புதிய ஒப்புதல்கள், dispatch-கள், exception-கள், ஸ்டாக் transfer-கள்.

1. பட்டியல் பக்கத்தில் **Notifications** அழுத்துங்கள்.
2. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All**, **Stock Transfer**, **Exception** அல்லது **Dispatch & Approvals**.
3. படிக்காதவற்றுக்கு ஒரு புள்ளி இருக்கும். படிக்க ஒன்றைத் தட்டுங்கள்.
4. கோரிக்கையை **Tracking & Delivery** டேபில் திறக்க **View Details** அழுத்துங்கள்.
5. ஒன்றை மட்டும் படித்ததாகக் குறிக்க **Mark As Read** அழுத்துங்கள், எல்லாவற்றுக்கும் **Mark All As Read** அழுத்துங்கள்.
6. திரும்பிப் போக **Back to List Page** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No notifications".** (அறிவிப்புகள் இல்லை) இதுவரை புதிதாக எதுவும் நடக்கவில்லை.
- **ஒரு exception அறிவிப்பு வந்துள்ளது.** கோரிக்கையைத் திறந்து **Resolve Exception** பயன்படுத்துங்கள் (ADMIN-MD CS மற்றும் DGM-MD CS மட்டும்).
:::
