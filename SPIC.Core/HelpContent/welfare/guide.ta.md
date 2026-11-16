---
module: welfare
title: நலத் திட்டங்கள் (SDWA)
icon: bi-heart
order: 111
language: ta
category: டீலர்கள்
note: புதிய வழிகாட்டி
meta-source-hash: 95f84553d50218f7
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: fa298dd497c945c5
confidence: ok

டீலர்கள் திருமணப் பரிசு, மருத்துவ உதவி, கல்வி உதவி போன்ற SDWA நலத் திட்டங்களுக்கு ஆப்பிலேயே விண்ணப்பிக்கலாம். அதிகாரிகள் ஒவ்வொரு விண்ணப்பத்தையும் படிப்படியாகச் சரிபார்ப்பார்கள். விண்ணப்பம் இப்போது எங்கே இருக்கிறது என்று டீலர் எப்போது வேண்டுமானாலும் பார்க்கலாம்.

- apply | icon: bi-pencil-square | color: #be123c
  title: டீலர் விண்ணப்பிப்பார்
  text: தகுதியைச் சரிபார்த்து, ஒரு திட்டத்தைத் தேர்வு செய்து, படிவத்தை நிரப்பி, ஆவணங்களை அப்லோட் செய்யுங்கள்.
- approve | icon: bi-shield-check | color: #d97706
  title: ஐந்து சரிபார்ப்புகள்
  text: MO, RM, SM, SDWA, SDWA Admin ஆகியோர் வரிசையாகச் சரிபார்ப்பார்கள்.
- benefit | icon: bi-cash-coin | color: #16a34a
  title: இறுதி ஒப்புதல்
  text: SDWA Admin இறுதி ஒப்புதல் தந்து, காசோலை (cheque) விவரங்களைப் பதிவு செய்வார்.
:::

::: section flow
title: ஒரு விண்ணப்பம் எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 942cc75f4b898ba1
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- dealer | type: lane | color: #be123c | roles: Dealer
  title: டீலர்
  sub: விண்ணப்பித்துப் பின்தொடர்வார்
- mo | type: lane | color: #d97706 | roles: MO
  title: MO
  sub: HQ-வைச் சரிபார்ப்பார்
- rm | type: lane | color: #0d9488 | roles: RM
  title: RM
  sub: மண்டலத்தைச் சரிபார்ப்பார்
- sm | type: lane | color: #2563eb | roles: SMM
  title: SM
  sub: மாநிலத்தைச் சரிபார்ப்பார்
- sdwa | type: lane | color: #7c3aed
  title: SDWA
  sub: SDWA · SDWA Admin
- choose | lane: dealer | col: 1 | icon: bi-ui-checks | color: #64748b | next: apply | screen: dashboard
  title: தகுதியைச் சரிபார்த்தல்
  text: டாஷ்போர்டில் **Apply Welfare**-ன் கீழ் உள்ள **Apply Now** அழுத்துங்கள். உங்களுக்குத் தகுதி இருந்தால், ஒரு திட்டத்தைத் தேர்வு செய்யுங்கள்.
- apply | lane: dealer | col: 2 | icon: bi-pencil-square | color: #64748b | next: mo-check | screen: apply-scheme
  title: நிரப்பிச் சமர்ப்பித்தல்
  status: Draft
  text: படிவத்தை நிரப்பி, ஆவணங்களை அப்லோட் செய்து, சமர்ப்பியுங்கள். அதுவரை அது வெறும் டிராஃப்ட் தான்.
- mo-check | lane: mo | col: 3 | icon: bi-search | color: #d97706 | next: rm-check | screen: scheme-approval
  title: MO சரிபார்ப்பு
  status: Pending MO
  text: HQ-வில் உள்ள டீலர்களின் விவரங்கள், விற்பனை, ஆவணங்களை MO சரிபார்ப்பார்.
- rm-check | lane: rm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: sm-check | return: mo-check | screen: scheme-approval
  title: RM சரிபார்ப்பு
  status: Pending RM
  text: RM மண்டலத்துக்காகச் சரிபார்ப்பார். நிராகரித்தால் அது MO-க்குத் திரும்பிப் போகும்.
- sm-check | lane: sm | col: 5 | icon: bi-hourglass-split | color: #d97706 | next: sdwa-check | return: rm-check | screen: scheme-approval
  title: SM சரிபார்ப்பு
  status: Pending SM
  text: SM மாநிலத்துக்காகச் சரிபார்ப்பார். நிராகரித்தால் அது RM-க்குத் திரும்பிப் போகும்.
- sdwa-check | lane: sdwa | col: 6 | icon: bi-patch-check | color: #7c3aed | next: sdwaadmin | return: sm-check | screen: scheme-approval
  title: SDWA ஒப்புதல்
  status: Pending SDWA
  text: SDWA ஒப்புதல் தரும். நிராகரித்தால் அது SM-க்குத் திரும்பிப் போகும்.
- sdwaadmin | lane: sdwa | col: 7 | icon: bi-cash-coin | color: #7c3aed | next: approved | return: sdwa-check | screen: scheme-approval
  title: SDWA Admin இறுதி ஒப்புதல்
  status: Pending SDWA Admin
  text: SDWA Admin இறுதி ஒப்புதல் தந்து, காசோலை எண்ணையும் தொகையையும் உள்ளிடுவார்.
- approved | lane: dealer | col: 8 | icon: bi-check-circle | color: #16a34a | screen: my-applications
  title: ஒப்புதல் கிடைத்தது
  status: Approved
  text: விண்ணப்பத்துக்கு ஒப்புதல் கிடைத்துவிட்டது. டீலர் **My Applications**-ல் **Approved** என்று பார்ப்பார்.
- returned | lane: dealer | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: mo-check | to: apply | screen: my-applications
  title: திருத்தி மீண்டும் சமர்ப்பித்தல்
  status: Rejected by MO – Resubmission Required
  text: MO நிராகரித்தால், அது டீலரிடம் திரும்பி வரும். திருத்தி மீண்டும் சமர்ப்பியுங்கள். அதே Application ID தொடரும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8a133f3149737003
confidence: ok

- draft | color: #64748b
  label: Draft
  text: சேமிக்கப்பட்டது, ஆனால் அனுப்பப்படவில்லை.
- pending-mo | color: #d97706
  label: Pending MO
  text: சரிபார்ப்புக்காக MO-விடம் உள்ளது.
- pending-rm | color: #d97706
  label: Pending RM
  text: சரிபார்ப்புக்காக RM-இடம் உள்ளது.
- pending-sm | color: #d97706
  label: Pending SM
  text: சரிபார்ப்புக்காக SM-இடம் உள்ளது.
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: ஒப்புதலுக்காக SDWA-விடம் உள்ளது.
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: இறுதி ஒப்புதலுக்காக SDWA Admin-இடம் உள்ளது.
- approved | color: #16a34a
  label: Approved
  text: இறுதி ஒப்புதல் கிடைத்துவிட்டது.
- resubmit | color: #dc2626
  label: Rejected by MO – Resubmission Required
  text: இது டீலருக்குத் தெரியும். விண்ணப்பத்தைத் திருத்தி மீண்டும் சமர்ப்பியுங்கள்.
- returned | color: #ea580c
  label: Returned to Dealer
  text: இது அதிகாரிகளுக்குத் தெரியும். விண்ணப்பம் திருத்தத்துக்காக டீலரிடம் திரும்பிப் போயுள்ளது.
- rejected | color: #475569
  label: Rejected
  text: மூடப்பட்டது. தேவைப்பட்டால் மீண்டும் விண்ணப்பியுங்கள்.
- cancelled | color: #94a3b8
  label: Cancelled
  text: விண்ணப்பம் ரத்து செய்யப்பட்டது.
- awaiting-mo | color: #d97706
  label: Awaiting MO Review
  text: MO-க்காகக் காத்திருக்கும் புதிய விண்ணப்பத்துக்கு டாஷ்போர்டில் வரும் பெயர்.
- under-mo | color: #d97706
  label: Under MO Review
  text: MO-விடம் திரும்பி வந்த விண்ணப்பத்துக்கு டாஷ்போர்டில் வரும் பெயர்.
- under-rm | color: #d97706
  label: Under RM Review
  text: Pending RM-க்கு டாஷ்போர்டில் வரும் பெயர்.
- under-smm | color: #d97706
  label: Under SMM Review
  text: Pending SM-க்கு டாஷ்போர்டில் வரும் பெயர்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 69fdf9c12e670358
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- dealer | color: #be123c | roles: Dealer
  title: டீலர்
  - தகுதியைச் சரிபார்த்து விண்ணப்பிப்பது
  - விண்ணப்பங்களைப் பின்தொடர்வது, PDF பதிவிறக்குவது
  - திருப்பி அனுப்பிய விண்ணப்பத்தைத் திருத்தி மீண்டும் சமர்ப்பிப்பது
- mo | color: #d97706 | roles: MO
  title: MO
  - உங்கள் HQ-வின் விண்ணப்பங்களைச் சரிபார்ப்பது
  - தவறான ஆவணத்தை மாற்றுவது அல்லது நீக்குவது
  - ஒப்புதல் தருவது, அல்லது டீலருக்குத் திருப்பி அனுப்புவது
- rm-sm | color: #0d9488 | roles: RM, SMM
  title: RM மற்றும் SM
  - உங்கள் மண்டலம் அல்லது மாநிலத்தின் விண்ணப்பங்களைச் சரிபார்ப்பது
  - ஒப்புதல் தருவது, அல்லது ஒரு படி பின்னால் திருப்பி அனுப்புவது
- sdwa | color: #7c3aed
  title: SDWA மற்றும் SDWA Admin
  - எல்லாப் பகுதிகளின் விண்ணப்பங்களையும் பார்ப்பது
  - கடைசி இரண்டு ஒப்புதல்களைத் தருவது
  - SDWA Admin காசோலை விவரங்களை உள்ளிடுவார்
- director | color: #475569 | roles: Director
  title: Director
  - எல்லா விண்ணப்பங்களையும் பார்ப்பது
  - SDWA ஒப்புதல் தந்த பிறகு, சில திட்டங்களுக்கு விருப்பப்பட்டால் ஒரு மதிப்பாய்வு (review) தருவது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: cd3161af8efa4aae
confidence: ok

- returned | popular: true
  q: என் விண்ணப்பத்தில் "Rejected by MO – Resubmission Required" என்று காட்டுகிறது
  a: **My Applications**-ஐத் திறந்து, சிவப்புப் பெட்டியில் உள்ள காரணத்தைப் படியுங்கள். **Correct & Resubmit** அழுத்தி, விவரங்களையோ ஆவணங்களையோ சரி செய்து, **Resubmit Application** அழுத்துங்கள். அதே Application ID தொடரும்.
- not-eligible | popular: true
  q: Welfare Eligibility சாளரத்தில் எனக்குத் தகுதி இல்லை என்று வருகிறது
  a: அந்தச் சாளரத்தில் ஒவ்வொரு விதியும் **Required** (தேவை) மற்றும் **Your dealership** (உங்கள் டீலர்ஷிப்) உடன் தெரியும். சிவப்பில் உள்ள விதிகள் இன்னும் பூர்த்தியாகவில்லை. ஏதாவது மதிப்பு தவறு என்று நினைத்தால் உங்கள் MO-விடம் பேசுங்கள்.
- seven-days
  q: "Invitation must be sent at least 7 days in advance" என்று வருகிறது
  a: Wedding Gift, Sathabhishekam, Grahapravesam ஆகியவற்றுக்கு, நிகழ்ச்சித் தேதி இன்றிலிருந்து குறைந்தது 7 நாள் கழித்து இருக்க வேண்டும்.
- cheque
  q: படிவத்தில் "Cancelled Cheque / Bank Passbook is required" என்று வருகிறது
  a: ரத்து செய்த காசோலை அல்லது வங்கிப் பாஸ்புக் பக்கத்தை அப்லோட் செய்யுங்கள். அதில் அச்சிடப்பட்டுள்ளபடியே **Beneficiary Name**-ஐ டைப் செய்யுங்கள்.
- where-is-it
  q: என் விண்ணப்பம் இப்போது யாரிடம் உள்ளது?
  a: **My Applications**-ல் உள்ள வட்டங்களின் வரிசை ஒவ்வொரு படியையும் காட்டும். பச்சை என்றால் முடிந்தது, ஆரஞ்சு என்றால் இப்போது விண்ணப்பம் இருக்கும் படி.
- no-submit
  q: நான் ஒரு அதிகாரி, ஒரு விண்ணப்பத்துக்கு என்னால் ஒப்புதல் தர முடியவில்லை
  a: விண்ணப்பம் உங்கள் நிலையில் இருந்தால், உங்கள் பகுதியைச் சேர்ந்ததாக இருந்தால் மட்டுமே நீங்கள் செயல்பட முடியும். பட்டியலில் அதன் நிலையைப் பாருங்கள்.
:::

::: section dashboard
title: டீலர் டாஷ்போர்டைப் பயன்படுத்துவது
routes: /SDWADashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: 44c37577fe4705d0
confidence: ok

**இந்தத் திரை எதற்கு**
**Dashboard** தான் நல விண்ணப்பங்களுக்கும் விருந்தினர் இல்ல (guest house) முன்பதிவுகளுக்கும் டீலரின் முதல் பக்கம். இங்கே புதிய விண்ணப்பம் தொடங்கலாம், உங்கள் விண்ணப்பங்கள் எப்படிப் போகின்றன என்று பார்க்கலாம்.

1. மேலே உங்கள் பெயர், **Dealer Code**, **Region**, **Profile Completion** ஆகியவற்றைச் சரிபாருங்கள்.
2. **Quick Actions**-ன் கீழ், **Apply Welfare** கார்டில் உள்ள **Apply Now** அழுத்துங்கள். ஆப் முதலில் உங்கள் தகுதியைச் சரிபார்க்கும்.
3. **Welfare Eligibility** சாளரத்தில் உங்கள் **State**, **State Group**, **Dealer Type** மற்றும் ஒவ்வொரு **Eligibility Criteria** விதியும் தெரியும். தகுதி இருந்தால் **Proceed to Apply** அழுத்துங்கள். இல்லையென்றால் **Close** அழுத்துங்கள்.
4. **Application Status Overview**-ஐப் பாருங்கள்: **Pending Applications**, **Approved Applications**, **Rejected Applications**, **Processing Applications**.
5. **Recent Applications**-ன் கீழ், ஒன்றைத் திறக்க **View Details** அழுத்துங்கள், அல்லது **My Applications**-ஐத் திறக்க **View All** அழுத்துங்கள்.
6. சமீபத்திய மாற்றங்களுக்கு **Notifications** மற்றும் **Recent Activity**-ஐப் படியுங்கள்.

> Tip: இந்தத் திரையில் உள்ள **Book Room** மற்றும் **My Bookings** விருந்தினர் இல்லத்துக்கானவை. Guest house வழிகாட்டியைப் பாருங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Unable to verify eligibility. Please try again later."** (தகுதியைச் சரிபார்க்க முடியவில்லை, பிறகு முயலுங்கள்) இன்டர்நெட்டைச் சரிபார்த்து, மீண்டும் **Apply Now** அழுத்துங்கள்.
- **"No applications yet".** (இன்னும் விண்ணப்பங்கள் இல்லை) நீங்கள் இன்னும் விண்ணப்பிக்கவில்லை. **Apply Welfare** அழுத்துங்கள்.
- **எனக்கு Apply Welfare தெரியவில்லை.** SDWA Admin பயனர்கள் இந்தத் திரையில் இருந்து விண்ணப்பிக்க மாட்டார்கள்.
:::

::: section choose-scheme
title: ஒரு நலத் திட்டத்தைத் தேர்வு செய்வது
routes: /WelfareSchemes
for: 
roles: 
designations: 
minutes: 1
source-hash: 71f07f60f43b7d0c
confidence: ok

**இந்தத் திரை எதற்கு**
**Apply for Welfare Schemes** திரையில் ஒவ்வொரு SDWA திட்டமும் ஒரு கார்டாகத் தெரியும். உங்களுக்குத் தேவையானதைத் தேர்வு செய்யுங்கள்.

1. கார்டுகளைப் பாருங்கள்: **Medical Assistance**, **Wedding Gift**, **Grahapravesam**, **Educational Assistance**, **Sathabhishekam**, **Death Relief Assistance**, **Merit Award**, **Distinction Award**.
2. ஒவ்வொரு கார்டிலும் **Documents** (எத்தனை ஆவணங்கள் தேவை) மற்றும் **Eligibility** (யார் விண்ணப்பிக்கலாம்) பாருங்கள்.
3. உங்களுக்கு வேண்டிய திட்டத்தில் **Apply Now** அழுத்துங்கள். அந்தத் திட்டத்துக்கான விண்ணப்பப் படிவம் திறக்கும்.

> Careful: **Distinction Award** தமிழ்நாட்டில் (TN) உள்ள டீலர்களுக்கும் பணியாளர்களுக்கும் மட்டுமே.

## பொதுவான பிரச்சனைகள்

- **இந்தத் திரையை என்னால் திறக்க முடியவில்லை.** உங்கள் லாகினுக்கு நலத் திட்ட அனுமதி இல்லாமல் இருக்கலாம். உங்கள் MO அல்லது admin-இடம் கேளுங்கள்.
:::

::: section grahapravesam
title: Grahapravesam திட்டம் பற்றிப் படிப்பது
routes: /GrahapravesamScheme
for: 
roles: 
designations: 
minutes: 1
source-hash: a035ff18db9c4a2f
confidence: ok

**இந்தத் திரை எதற்கு**
**Grahapravesam** திரை, விண்ணப்பிக்கும் முன் புதுமனை புகுவிழா பரிசுத் திட்டத்தைப் பற்றி விளக்கும்.

1. மேலே உள்ள கார்டையும் **About the Scheme**-ஐயும் படியுங்கள்.
2. **Eligibility Criteria** மற்றும் **Required Documents**-ஐச் சரிபாருங்கள்.
3. **Frequently Asked Questions** மற்றும் **Important Notes**-ஐப் படியுங்கள்.
4. படிவத்தைத் திறக்க **Apply Now** அழுத்துங்கள், அல்லது வேறு திட்டத்தைத் தேர்வு செய்ய **Back to Schemes** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **என் புதுமனை புகுவிழா தேதியைப் படிவம் ஏற்கவில்லை.** படிவத்துக்கு இன்றிலிருந்து குறைந்தது 7 நாள் கழித்த தேதி வேண்டும்.
:::

::: section apply-scheme
title: நலத் திட்டத்துக்கு விண்ணப்பிப்பது
routes: /ApplyWelfareScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-dealer] | 2 > [data-help=wf-quantity] | 3 > [data-help=wf-details] | 4 > [data-help=wf-documents] | 5 > [data-help=wf-declaration] | 6 > [data-help=wf-actions]
minutes: 5
source-hash: 55566f3677387604
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் விண்ணப்பத்தை நிரப்பி அனுப்ப **Apply for** திரையைப் (உதாரணமாக **Apply for Wedding**) பயன்படுத்துங்கள். ஒவ்வொரு திட்டத்துக்கும் படிவம் கொஞ்சம் மாறும்.

1. உங்கள் டீலர் கார்டைச் (**Location Info** மற்றும் **Personal Info**) சரிபார்த்து, அந்தத் திட்டத்துக்கான **Eligibility Conditions** பெட்டியைப் படியுங்கள்.
2. **Quantity Lifted**-ன் கீழ், **Quantity Lifted during Last Year (MT)** தேர்வு செய்யுங்கள். 3 வருட மொத்தங்கள் தானாக நிரம்பும். பிறகு **Beneficiary Details** திறக்கும்: **Select Beneficiary Group** தேர்வு செய்து, நாமினி மற்றும் பயனாளி விவரங்களை நிரப்புங்கள்.
3. திட்டத்துக்கான பகுதியை நிரப்புங்கள், உதாரணமாக **Wedding Date** உடன் **Wedding Details**, அல்லது **Treatment / Medical Condition** உடன் **Medical Assistance Details**.
4. **Document Upload**-ன் கீழ், ஒவ்வொரு ஆவணத்துக்கும் **Click to upload** தட்டுங்கள். **Cancelled Cheque / Bank Passbook**-க்கு, அதில் அச்சிடப்பட்டுள்ளபடியே **Beneficiary Name**-ஐயும் டைப் செய்யுங்கள்.
5. **Declaration**-ன் கீழ், உரையைப் படித்து பெட்டியில் டிக் செய்யுங்கள்.
6. வைத்துக்கொள்ள **Save as Draft** அழுத்துங்கள், அல்லது **Preview Application** அழுத்துங்கள். சரிபார்த்து, **Submit Application** அழுத்தி, பிறகு **Confirm** அழுத்துங்கள். **Application ID**-ஐக் குறித்துக்கொண்டு **Track Application** அழுத்துங்கள்.

> Careful: **Confirm** அழுத்திய பிறகு, விண்ணப்பம் MO-க்குப் போய்விடும். உங்களுக்குத் திருப்பி அனுப்பப்பட்டால் மட்டுமே அதை மாற்ற முடியும்.
> Tip: சேமித்த டிராஃப்டுக்கு ஒரு குறிப்பு எண் கிடைக்கும். அது **My Applications**-ல் **Drafts**-ன் கீழ் எண்ணப்படும்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Quantity Lifted during Last Year (MT)** | கடந்த வருடம் நீங்கள் எடுத்த அளவின் வரம்பு. கட்டாயம். |
| **Select Beneficiary Group** | **Direct Dealer**, **Sub Dealer** அல்லது **Approved Employee**. கட்டாயம். |
| **Nominee Name** மற்றும் **Nominee Relationship** | உங்கள் டீலர் பதிவுகளில் உள்ள நாமினி. கட்டாயம். |
| **Select Beneficiary** | குடும்ப உறுப்பினர் ஒருவர், அல்லது **Add new beneficiary**. நேரடி டீலருக்குக் கட்டாயம். |
| நிகழ்ச்சித் தேதி | Wedding, Sathabhishekam, Grahapravesam: இன்றிலிருந்து குறைந்தது 7 நாள் கழித்து. |
| **Date of Death** மற்றும் **Legal Heir Relation** | Death Relief-க்கு மட்டும். உறவு **Others** என்றால், பிரமாணப் பத்திரத்தை (affidavit) அப்லோட் செய்யுங்கள். |
| ஆவணங்கள் | PDF, JPG அல்லது PNG, ஒவ்வொன்றும் 5 MB வரை. |

## பொதுவான பிரச்சனைகள்

- **"Please fix the following errors:"** (கீழே உள்ள பிழைகளைச் சரி செய்யுங்கள்) பட்டன்களுக்கு மேலே உள்ள பட்டியலைப் படித்து, ஒவ்வொன்றையும் சரி செய்யுங்கள்.
- **கோப்பை அப்லோட் செய்தால் எதுவும் நடக்கவில்லை.** கோப்பு PDF, JPG அல்லது PNG ஆக, 5 MB-க்குக் குறைவாக இருக்க வேண்டும்.
- **"Minimum 80% marks is required for the Merit Award."** (Merit Award-க்குக் குறைந்தது 80% மதிப்பெண் வேண்டும்) Merit Award-க்குக் குறைந்தது 80% வேண்டும். Distinction Award-க்கு 80%-க்கு மேல், அரியர் இல்லாமல் வேண்டும்.
- **"A minimum average annual sales of 50,000 MT..."** (சராசரி ஆண்டு விற்பனை குறைந்தது 50,000 MT) சப் டீலருக்காக விண்ணப்பிக்க உங்கள் விற்பனை மிகக் குறைவு (பணியாளருக்கு 5,000 MT).
:::

::: section my-applications
title: என் விண்ணப்பங்களைப் பின்தொடர்வது
routes: /WellfareApplication
for: 
roles: 
designations: 
minutes: 2
source-hash: 62f3f5bcf69937c0
confidence: ok

**இந்தத் திரை எதற்கு**
**My Applications** திரையில் நீங்கள் போட்ட எல்லா நல விண்ணப்பங்களும், அவற்றின் நிலையும், அவை கடந்த படிகளும் தெரியும்.

1. மேலே உள்ள கட்டங்களைப் பாருங்கள்: **Total**, **Pending**, **Approved**, **Rejected**, **Drafts**, **Cancelled**.
2. **Latest First** அல்லது **Oldest First** தேர்வு செய்யுங்கள், ஒரு நிலையைத் தேர்வு செய்யுங்கள், அல்லது **Search by Application ID or Scheme**-ல் டைப் செய்யுங்கள்.
3. ஒவ்வொரு கார்டிலும் திட்டம், Application ID, நிலை தெரியும். வட்டங்களின் வரிசை **Dealer**, **MO**, **RM**, **SM/SMM**, **SDWA**, **SDWA Admin**, **Final** ஆகியவற்றைக் காட்டும். பச்சை என்றால் முடிந்தது, ஆரஞ்சு என்றால் இப்போது இருக்கும் இடம்.
4. எல்லாவற்றையும் பார்க்க **View Details** அழுத்துங்கள், அல்லது ஒரு நகலைச் சேமிக்க **Download PDF** அழுத்துங்கள்.
5. சிவப்புப் பெட்டியில் **Rejected by** என்று இருந்தால், **Reason**-ஐப் படித்து **Correct & Resubmit** அழுத்துங்கள். படிவத்தைச் சரி செய்து, **Resubmit Application** அழுத்தி, பிறகு **Confirm Resubmission** அழுத்துங்கள்.
6. புதிய விண்ணப்பம் தொடங்க **Apply for a scheme** அழுத்துங்கள்.

> Tip: மீண்டும் சமர்ப்பித்த விண்ணப்பத்துக்கு அதே Application ID தொடரும். அது மீண்டும் MO-விடமிருந்து தொடங்கும்.

## பொதுவான பிரச்சனைகள்

- **"No matching applications".** (பொருந்தும் விண்ணப்பங்கள் இல்லை) **Clear filters** அழுத்துங்கள்.
- **Download PDF-ல் "Failed – Tap to Retry" என்று வருகிறது.** இன்டர்நெட்டைச் சரிபார்த்து மீண்டும் தட்டுங்கள்.
- **Correct & Resubmit பட்டன் இல்லை.** MO உங்களுக்குத் திருப்பி அனுப்பிய விண்ணப்பத்தை மட்டுமே மீண்டும் சமர்ப்பிக்க முடியும்.
:::

::: section scheme-approval
title: நல விண்ணப்பங்களைச் சரிபார்த்து ஒப்புதல் தருவது
routes: /SchemeApproval
for: 
roles: 
designations: 
tour: 1 > [data-help=wf-ap-cards] | 2 > [data-help=wf-ap-tabs] | 3 > [data-help=wf-ap-search] | 4 > [data-help=wf-ap-table] | 5 > [data-help=wf-ap-table] | 6 > [data-help=wf-ap-pages]
minutes: 3
source-hash: 06b3fdb4303eee15
confidence: ok

**இந்தத் திரை எதற்கு**
MO, RM, SM, SDWA, SDWA Admin ஆகியோர் தங்கள் நிலையில் காத்திருக்கும் நல விண்ணப்பங்களைச் சரிபார்க்க **Approval** திரையைப் பயன்படுத்துவார்கள். MO தன் HQ-வையும், RM மண்டலத்தையும், SM மாநிலத்தையும் பார்ப்பார்கள். SDWA, SDWA Admin எல்லாப் பகுதிகளையும் பார்ப்பார்கள்.

1. கார்டுகளைப் பாருங்கள்: **Total Applications**, உங்கள் நிலைக்கான **Pending** (உதாரணமாக **Pending MO**), **Validated by MO**, **Rejected**, **Completed**.
2. ஒரு டேபைத் தேர்வு செய்யுங்கள், உதாரணமாக **Pending**, உங்கள் நிலைக்கான **Approved by**, **Validated by MO**, **Recommended by RM**, **Approved by SM**, **Rejected** அல்லது **Completed**.
3. **Search dealer / application...**-ல் டைப் செய்யுங்கள், அல்லது **All Schemes**-ல் ஒரு திட்டத்தைத் தேர்வு செய்யுங்கள்.
4. அட்டவணையில் **Status** மற்றும் **Approval Flow (MO → RM → SM → SDWA → SDWA Admin)** பாருங்கள். பச்சை டிக் என்றால் முடிந்தது, நீல வட்டம் என்றால் இப்போது விண்ணப்பம் இருக்கும் நிலை, சிவப்புக் குறுக்குக் குறி என்றால் நிராகரிப்பு.
5. விண்ணப்பத்தைத் திறக்க கண் ஐகானைத் தட்டுங்கள். விவரங்களையும் ஆவணங்களையும் சரிபார்த்து, **Comments** தேர்வு செய்யுங்கள். **Recommendation** தானாக நிரம்பும். **Submit** அழுத்துங்கள்.
6. மேலும் வரிசைகளைப் பார்க்க **Items Per Page** மற்றும் அம்புக்குறிகளைப் பயன்படுத்துங்கள்.

> Careful: நிராகரிக்கும் கருத்து (comment) விண்ணப்பத்தை ஒரு படி பின்னால் அனுப்பும். MO நிராகரித்தால், அது டீலரிடம் திரும்பிப் போகும்.
> Tip: SDWA Admin **Cheque Number** மற்றும் **Cheque Amount**-ஐயும் உள்ளிட வேண்டும். **Cheque Image**-ஐயும் சேர்க்கலாம்.

## ஒப்புதல் வரிசை

- MO, பிறகு RM, பிறகு SM, பிறகு SDWA, பிறகு SDWA Admin இறுதி ஒப்புதல் தருவார்.
- Admin, Corporate Admin, Director ஆகியோர் பட்டியலைத் திறக்கலாம், ஆனால் ஒப்புதல் தர மாட்டார்கள். SDWA ஒப்புதல் தந்த பிறகு, Death Relief, Educational Assistance, Merit Award, Medical Assistance ஆகியவற்றுக்கு Director விருப்பப்பட்டால் ஒரு மதிப்பாய்வு சேர்க்கலாம்.

## பொதுவான பிரச்சனைகள்

- **"No applications found for this view."** (இந்தப் பார்வையில் விண்ணப்பங்கள் இல்லை) இந்த டேபில் எதுவும் காத்திருக்கவில்லை. வேறு டேபைப் பாருங்கள்.
- **"Unable to load the approval queue. Please try again."** (ஒப்புதல் பட்டியலை ஏற்ற முடியவில்லை) **Retry** அழுத்துங்கள்.
- **Submit பட்டன் இல்லை.** விண்ணப்பம் இப்போது உங்கள் நிலையில் இல்லை.
- **"Please select a comment."** (ஒரு கருத்தைத் தேர்வு செய்யுங்கள்) **Submit** அழுத்தும் முன் ஒரு கருத்தைத் தேர்வு செய்யுங்கள்.
:::

::: section company-master
title: SDWA நிறுவனங்களை நிர்வகிப்பது
routes: /SdwaCompanyMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: de3ec8b1ed8fc6f6
confidence: ok

**இந்தத் திரை எதற்கு**
**SDWA Company Details** திரையில், பில்லிங்குக்காக விருந்தினர் இல்லங்களுடன் இணைக்கப்பட்ட நிறுவனங்களின் பட்டியல் இருக்கும்.

1. கார்டுகளைப் பாருங்கள்: **Total Companies**, **Active**, **Inactive**.
2. **Add New Company**-ன் கீழ், **Company Name**, **Short Name / Code**, **GSTIN** ஆகியவற்றை உள்ளிடுங்கள்.
3. **Guest House(s)**-ல், ஒவ்வொரு விருந்தினர் இல்லத்தையும் டைப் செய்து தட்டுங்கள். ஒரு பெயரை நீக்க அதிலுள்ள சிறிய x-ஐத் தட்டுங்கள்.
4. சுவிட்சை **Active** அல்லது **Inactive** என வைத்து, **Save** அழுத்துங்கள். **Cancel** படிவத்தைக் காலி செய்யும்.
5. **Existing Companies**-ல், தேடுங்கள் அல்லது **All Status**, **Active**, **Inactive** தேர்வு செய்யுங்கள். திருத்த பென்சிலையும், நிலையை மாற்ற அந்த நிலையையும், நீக்க குப்பைத்தொட்டியையும் தட்டுங்கள்.

> Careful: நீக்கியதைத் திரும்பப் பெற முடியாது. முன்பதிவுகளுடன் இணைக்கப்பட்ட நிறுவனத்தை நீக்க முடியாது.

## பொதுவான பிரச்சனைகள்

- **"GSTIN is required."** அல்லது **"Company Name is required."** (GSTIN / நிறுவனப் பெயர் கட்டாயம்) இரண்டு புலங்களையும் நிரப்புங்கள்.
- **"At least one Guest House must be selected."** (குறைந்தது ஒரு விருந்தினர் இல்லத்தைத் தேர்வு செய்ய வேண்டும்) குறைந்தது ஒரு விருந்தினர் இல்லத்தைத் தேர்வு செய்யுங்கள்.
- **எனக்கு Add New Company படிவம் தெரியவில்லை.** உங்கள் லாகினால் இந்தப் பட்டியலைப் பார்க்க மட்டுமே முடியும்.
:::
