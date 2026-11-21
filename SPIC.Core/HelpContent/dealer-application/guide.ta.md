---
module: dealer-application
title: டீலர் விண்ணப்பம்
icon: bi-shop
order: 101
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
source-hash: b3660f0cee5decd6
confidence: ok

13 சிறிய படிகளில் ஒரு டீலரைப் பதிவு செய்யுங்கள்: டீலர் யார், வியாபாரம், உரிமையாளர்கள், பணம், ஆவணங்கள். பிறகு விண்ணப்பத்தைச் சமர்ப்பியுங்கள். டீலருக்கு ஒப்புதல் கிடைக்கும் முன் RM, SMM, AVP அதைச் சரிபார்ப்பார்கள்.

- steps | icon: bi-list-ol | color: #7c3aed
  title: 13 படிகள், ஒவ்வொன்றாக
  text: Next அழுத்தும்போது ஒவ்வொரு படியும் சேமிக்கப்படும். பிறகு திரும்ப வந்து முடிக்கலாம்.
- check | icon: bi-clipboard-check | color: #d97706
  title: ஒரு கடைசிச் சரிபார்ப்பு
  text: சமர்ப்பிக்கும் முன் என்ன விடுபட்டுள்ளது என்று கடைசிப் படி காட்டும்.
- approve | icon: bi-shield-check | color: #16a34a
  title: மூன்று ஒப்புதல்கள்
  text: முதலில் RM, பிறகு SMM, பிறகு AVP விண்ணப்பத்துக்கு ஒப்புதல் தருவார்கள்.
:::

::: section flow
title: ஒரு விண்ணப்பம் எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2413a14b313769d3
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- applicant | type: lane | color: #7c3aed | roles: MO, MDO, JMDO, Admin
  title: விண்ணப்பிப்பவர்
  sub: MO · MDO · JMDO (புதிய டீலர்: Admin · AVP)
- rm-team | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: மண்டலம்
- smm-team | type: lane | color: #d97706 | roles: SMD, SMM
  title: SMM
  sub: மாநிலம்
- avp-team | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: இறுதி ஒப்புதல்
- office-team | type: lane | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: புதிய டீலர்களுக்கு மட்டும்
- register | lane: applicant | col: 1 | icon: bi-person-plus | color: #64748b | next: steps | screen: register
  title: பதிவைத் தொடங்குதல்
  status: Draft
  text: **Create Existing Dealer** அல்லது **Create New Dealer** அழுத்தி, படி 1 **Dealer Registration**-ஐ நிரப்புங்கள்.
- steps | lane: applicant | col: 2 | icon: bi-list-ol | color: #7c3aed | next: documents | screen: experience
  title: வியாபாரப் படிகளை நிரப்புதல்
  status: Registration In Progress
  text: அனுபவம், விற்பனை, கிடங்குகள், மார்க்கெட், நிறுவனங்கள், உரிமையாளர்கள், விற்பனைத் திட்டம், சொத்துகள், கிரெடிட் லிமிட் ஆகியவற்றை நிரப்புங்கள். ஒவ்வொரு படியிலும் **Next** அழுத்துங்கள்.
- documents | lane: applicant | col: 3 | icon: bi-paperclip | color: #7c3aed | next: submit | screen: enclosures
  title: ஆவணங்களை அப்லோட் செய்தல்
  status: Registration In Progress
  text: **Enclosures** படியில் **Required** என்று குறிக்கப்பட்ட ஒவ்வொரு ஆவணத்தையும் அப்லோட் செய்யுங்கள்.
- submit | lane: applicant | col: 4 | icon: bi-send | color: #0b6b3a | next: rm | screen: final-submission
  title: சரிபார்த்துச் சமர்ப்பித்தல்
  status: Application Ready for Submission
  text: ஒவ்வொரு பகுதியும் முடிந்ததா என்று சரிபார்த்து **Submit Registration** அழுத்துங்கள்.
- incomplete | lane: applicant | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: submit | to: steps | screen: final-submission
  title: ஏதோ விடுபட்டுள்ளது
  text: ஒரு பகுதி அல்லது ஆவணம் விடுபட்டிருந்தால், ஆப் சமர்ப்பிக்காது. பென்சிலைத் தட்டி அந்தப் பகுதியைத் திறந்து, சரி செய்து, மீண்டும் சமர்ப்பியுங்கள்.
- rm | lane: rm-team | col: 5 | icon: bi-hourglass-split | color: #0d9488 | next: smm | return: submit
  title: RM சரிபார்ப்பார்
  status: In RM
  text: RM விண்ணப்பத்துக்கு ஒப்புதல் தருவார், அல்லது நிராகரித்து விண்ணப்பிப்பவருக்குத் திருப்பி அனுப்புவார்.
- smm | lane: smm-team | col: 6 | icon: bi-hourglass-split | color: #d97706 | next: avp | return: rm
  title: SMM சரிபார்ப்பார்
  status: In SMM
  text: SMM ஒப்புதல் தருவார், அல்லது நிராகரித்து RM-க்குத் திருப்பி அனுப்புவார்.
- avp | lane: avp-team | col: 7 | icon: bi-patch-check | color: #2563eb | next: office | return: smm
  title: AVP ஒப்புதல் தருவார்
  status: In AVP
  text: AVP இறுதி ஒப்புதல் தருவார், அல்லது நிராகரித்து SMM-க்குத் திருப்பி அனுப்புவார்.
- office | lane: office-team | col: 8 | icon: bi-123 | color: #64748b | next: done
  title: புதிய டீலருக்கான Dealer ID-கள்
  status: In AVP Office
  text: புதிய டீலருக்கு மட்டும், AVP Office நிறுவனத்தின் Dealer ID-களைச் சேமிப்பார்கள். ஏற்கனவே உள்ள டீலருக்கு இந்தப் படி கிடையாது.
- done | lane: applicant | col: 9 | icon: bi-check-circle | color: #16a34a | screen: application-pdf
  title: டீலருக்கு ஒப்புதல் கிடைத்தது
  status: Approved
  text: விண்ணப்பம் முடிந்துவிட்டது. டீலர் கார்டில் இருந்து டீலர்ஷிப் விண்ணப்பப் படிவத்தைத் திறக்கலாம்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 329a69141c3bcda8
confidence: ok

- draft | color: #64748b
  label: Draft
  text: சேமிக்கப்பட்டது, ஆனால் இன்னும் சமர்ப்பிக்கவில்லை. இப்போதும் மாற்றலாம்.
- in-progress | color: #7c3aed
  label: Registration In Progress
  text: நீங்கள் படிகளை நிரப்பிக்கொண்டிருக்கிறீர்கள்.
- ready | color: #0b6b3a
  label: Application Ready for Submission
  text: எல்லாப் பகுதிகளும் ஆவணங்களும் முடிந்துவிட்டன. சமர்ப்பிக்கலாம்.
- in-rm | color: #0d9488
  label: In RM
  text: சமர்ப்பிக்கப்பட்டது. RM சரிபார்த்துக்கொண்டிருக்கிறார்.
- in-smm | color: #d97706
  label: In SMM
  text: RM ஒப்புதல் தந்தார். SMM சரிபார்த்துக்கொண்டிருக்கிறார்.
- in-avp | color: #2563eb
  label: In AVP
  text: SMM ஒப்புதல் தந்தார். AVP-க்காகக் காத்திருக்கிறது.
- in-office | color: #64748b
  label: In AVP Office
  text: புதிய டீலருக்கு AVP ஒப்புதல் தந்தார். நிறுவனத்தின் Dealer ID-களுக்காகக் காத்திருக்கிறது.
- approved | color: #16a34a
  label: Approved
  text: விண்ணப்பத்துக்கு முழு ஒப்புதல் கிடைத்துவிட்டது.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: விண்ணப்பிப்பவருக்குத் திருப்பி அனுப்பப்பட்டது. திருத்தி மீண்டும் சமர்ப்பியுங்கள்.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: திருத்துவதற்காக RM-க்குத் திருப்பி அனுப்பப்பட்டது.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: திருத்துவதற்காக SMM-க்குத் திருப்பி அனுப்பப்பட்டது.
- uploaded | color: #16a34a
  label: Uploaded
  text: ஆவணம் இணைக்கப்பட்டுள்ளது.
- missing | color: #ea580c
  label: Missing
  text: ஆவணம் இன்னும் இணைக்கப்படவில்லை.
- consent | color: #0891b2
  label: Consent Provided
  text: டீலர் கிரெடிட் லிமிட் விவரங்கள் எதுவும் தரவில்லை. கிரெடிட் லிமிட் படிகள் தவிர்க்கப்படும்.
- active | color: #16a34a
  label: Active
  text: டீலர் இப்போது வியாபாரம் செய்கிறார்.
- inactive | color: #d97706
  label: Inactive
  text: டீலர் இப்போது வியாபாரம் செய்யவில்லை.
- terminated | color: #475569
  label: Terminated
  text: டீலர்ஷிப் முடிவுக்கு வந்துவிட்டது.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 17fdf789d027bf63
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- creator | color: #7c3aed | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - ஏற்கனவே உள்ள டீலர்களைப் பதிவு செய்வது
  - எல்லாப் படிகளையும் நிரப்பி ஆவணங்களை அப்லோட் செய்வது
  - சமர்ப்பிப்பது, RM நிராகரித்த விண்ணப்பத்தைத் திருத்துவது
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - Create New Dealer மூலம் புதிய டீலர்களைப் பதிவு செய்வது
  - AVP இறுதி ஒப்புதல் தருவது
- rm | color: #0d9488 | roles: RM, RMD
  title: RM
  - உங்கள் மண்டலத்தின் விண்ணப்பங்களைச் சரிபார்ப்பது
  - ஒப்புதல் தருவது, அல்லது நிராகரித்து விண்ணப்பிப்பவருக்குத் திருப்பி அனுப்புவது
- smm | color: #d97706 | roles: SMD, SMM
  title: SMM
  - உங்கள் மாநிலத்தின் விண்ணப்பங்களைச் சரிபார்ப்பது
  - ஒப்புதல் தருவது, அல்லது நிராகரித்து RM-க்குத் திருப்பி அனுப்புவது
- office | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - ஒப்புதல் பெற்ற புதிய டீலர்களுக்கு நிறுவனத்தின் Dealer ID-களைச் சேமிப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 30e6b2b2e39499dd
confidence: ok

- warehouse-stuck | popular: true
  q: Warehouse படியைத் தாண்டி என்னால் போக முடியவில்லை
  a: குறைந்தது இரண்டு கிடங்குகள், இரண்டு ரயில் ஹெட்கள், இரண்டு துறைமுகங்கள் தேவை. ஒவ்வொன்றையும் அதன் **Add** பட்டன் மூலம் சேர்த்து, பிறகு **Next** அழுத்துங்கள்.
- not-submitted | popular: true
  q: Submit Registration அழுத்தினால் சில பகுதிகள் முடியவில்லை என்று வருகிறது
  a: **Final Submission**-ல், **Section Completion Status** மற்றும் **Enclosures**-ன் கீழ் ஆரஞ்சு நிறக் குறிகளைத் தேடுங்கள். பென்சிலைத் தட்டி, அந்தப் படியை முடித்து, மீண்டும் சமர்ப்பியுங்கள்.
- red-step
  q: மேலே உள்ள படிப் பட்டையில் ஒரு பகுதி சிவப்பாக உள்ளது
  a: அந்தப் படியில் ஏதோ பிரச்சனை அல்லது விடுபட்ட தகவல் உள்ளது. சிவப்புப் பகுதியைத் தட்டி அந்தப் படியைத் திறந்து சரி செய்யுங்கள்.
- ocr
  q: GST, PAN அல்லது வங்கிக் கணக்கு எண்ணை ஆப் தானாக நிரப்பிவிட்டது
  a: அப்லோட் செய்த கோப்பை ஆப் படிக்கிறது. எண்ணை எப்போதும் சரிபாருங்கள். தவறாக இருந்தால், சரியான எண்ணை டைப் செய்யுங்கள்.
- skipped-credit
  q: Credit Limit படிகள் தவிர்க்கப்பட்டுவிட்டன
  a: **Investment** படியில் சம்மதம் (consent) தரப்பட்டுள்ளது. கிரெடிட் லிமிட்டை நிரப்ப, **Investment**-ஐத் திறந்து **Remove Consent** அழுத்துங்கள்.
- no-code
  q: என் புதிய டீலருக்கு Dealer Code இல்லை
  a: புதிய டீலருக்கு இறுதி ஒப்புதல் கிடைத்த பிறகுதான் Dealer Code வரும்.
:::

::: section register
title: டீலர் பதிவைத் தொடங்குவது (படி 1)
routes: /Register
for: 
roles: 
designations: 
screenshot: register.png
hotspots: 1 @ 2.2,21.9,95.5,33.1 | 2 @ 2.2,57.9,95.5,32.4 | 3 @ 2.2,93.1,95.5,6.9 | 4 @ - | 5 @ - | 6 @ -
tour: 1 > [data-help=dr-basic] | 2 > [data-help=dr-business] | 3 > [data-help=dr-location] | 4 > [data-help=dr-bank] | 5 > [data-help=dr-tax] | 6 > [data-help=dr-license]
minutes: 6
source-hash: 1268f37d3a5ce326
confidence: ok

**இந்தத் திரை எதற்கு**
**Dealer Registration** திரை 13 படிகளில் படி 1. டீலர் யார், கடை எங்கே உள்ளது, வங்கிக் கணக்கு, வரி மற்றும் லைசென்ஸ் விவரங்கள் ஆகியவற்றை இது பதிவு செய்யும். **Register** மெனுவைத் திறந்து **Create Existing Dealer** அழுத்துங்கள். முற்றிலும் புதிய டீலருக்கு Admin மற்றும் AVP **Create New Dealer** பயன்படுத்துவார்கள்.

1. **Basic Information**-ன் கீழ், **Dealer**, **Department** அல்லது **Institution** தேர்வு செய்து, **SPIC**, **GREEN STAR** அல்லது இரண்டையும் தட்டுங்கள். **State**, **Region**, **HQ**-ஐச் சரிபாருங்கள். இவை உங்கள் புரொஃபைலில் இருந்து நிரப்பப்படும். ஏற்கனவே உள்ள டீலர் என்றால், **Active**, **Inactive** அல்லது **Terminated**-ஐயும் தேர்வு செய்யுங்கள்.
2. **Business Information**-ன் கீழ், **Dealer Code & Name** மற்றும் **Parent Dealer Code & Name**-ஐத் தேர்வு செய்யுங்கள் (ஏற்கனவே உள்ள டீலர்களுக்கு மட்டும்). உர லைசென்ஸில் உள்ளபடி **Firm Name**-ஐ டைப் செய்து, **Date of Appointment for SPIC** அல்லது **for Greenstar**-ஐத் தேர்வு செய்யுங்கள்.
3. **Primary Location**-ன் கீழ், கடையில் நின்றுகொண்டு **Use Current Location** அழுத்துங்கள், அல்லது **Google Map URL**-ஐ ஒட்டுங்கள். பிறகு முகவரி, **Pincode**, **District**, **State** மற்றும் மூன்று போன் எண்களை நிரப்புங்கள்.
4. **Bank Details**-ன் கீழ், **Upload Cheque (PDF or Image)** மூலம் காசோலையை அப்லோட் செய்யுங்கள். ஆப் அதைப் படித்துக் கணக்கு விவரங்களை நிரப்பும். **Account Holder Name**, **Account Number**, **Bank Name**, **Bank Branch**, **IFSC Code** ஆகியவற்றைச் சரிபாருங்கள்.
5. **Tax & Compliance**-ன் கீழ், GST சான்றிதழ், PAN கார்டு, e-Aadhaar ஆகியவற்றை PDF கோப்புகளாக அப்லோட் செய்யுங்கள். ஆப் ஒவ்வொரு கோப்பையும் படித்து எண்ணை நிரப்பும். ஒவ்வொரு எண்ணையும் சரிபாருங்கள். **TAN Number** கட்டாயமில்லை.
6. **Wholesale Fertilizer Details (as per iFMS)** அல்லது **Retail Fertilizer Details (as per iFMS)**-ஐ நிரப்புங்கள்: லைசென்ஸ் எண், **Expiry Date**, **mFMS ID**, லைசென்ஸ் கோப்பு. **Office Automation**-க்குப் பதில் சொல்லி, பிறகு **Next** அழுத்துங்கள்.

> Tip: ஏற்கனவே உள்ள டீலர் என்றால், **Trade Deposit Details for SPIC** அல்லது **for Greenstar**-ஐயும் நிரப்புங்கள்: தொகை, **Receipt No**, **Deposit Date**.
> Careful: **Department** அல்லது **Terminated** நிலையைத் தேர்வு செய்தால், இந்த ஒரு பக்கம் மட்டுமே. பட்டனில் **Submit** என்று இருக்கும், அது படிவத்தை உடனே பரிசீலனைக்கு அனுப்பிவிடும்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Firm Name** | உர லைசென்ஸில் உள்ளபடி வியாபாரப் பெயர். கட்டாயம். |
| **Pincode** | 6 இலக்கங்கள். ஆப் அந்தப் பகுதியைக் கண்டுபிடிக்கும். |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 இலக்க மொபைல் எண்கள். |
| **GST Number** | 15 எழுத்துகள், உதாரணமாக 22AAAAA0000A1Z5. |
| **PAN Number** | 10 எழுத்துகள், உதாரணமாக ABCDE1234F. |
| **Aadhaar Number** | சரியாக 12 இலக்கங்கள். |
| GST, PAN, Aadhaar, TAN கோப்புகள் | PDF மட்டும், 5 MB-க்குக் குறைவாக. |
| லைசென்ஸ் | மொத்த (wholesale) அல்லது சில்லறை (retail), குறைந்தது ஒரு லைசென்ஸ், அதன் சான்றிதழுடன். |

## பொதுவான பிரச்சனைகள்

- **Next அழுத்தினாலும் பக்கம் நகரவில்லை.** நட்சத்திரக் குறியுள்ள ஒரு புலம் காலியாக அல்லது தவறாக உள்ளது. புலத்தின் கீழ் உள்ள சிவப்புச் செய்தியைப் பாருங்கள், உதாரணமாக "Pincode must be 6 digits".
- **"At least one Fertilizer License (Wholesale or Retail) is required"** (குறைந்தது ஒரு உர லைசென்ஸ் தேவை) ஒரு லைசென்ஸை நிரப்பி அதன் சான்றிதழை அப்லோட் செய்யுங்கள்.
- **"OCR could not extract the required information from this document."** (இந்த ஆவணத்திலிருந்து தேவையான தகவலை ஆப்-ஆல் படிக்க முடியவில்லை) தெளிவான கோப்பை அப்லோட் செய்யுங்கள், அல்லது விவரங்களை நீங்களே டைப் செய்யுங்கள்.
- **"Only PDF files are allowed" or "File size must be less than 5 MB".** (PDF மட்டுமே அனுமதி, அல்லது கோப்பு 5 MB-க்குக் குறைவாக இருக்க வேண்டும்) ஆவணத்தைச் சிறிய PDF-ஆகச் சேமித்து மீண்டும் அப்லோட் செய்யுங்கள்.
:::

::: section experience
title: உர வியாபாரத்தில் அனுபவம் (படி 2)
routes: /Experience
for: 
roles: 
designations: 
screenshot: experience.png
minutes: 3
source-hash: 4c4087f2c0c5036d
confidence: ok

**இந்தத் திரை எதற்கு**
டீலர் எத்தனை ஆண்டுகளாக உரம் விற்கிறார், எந்த நிறுவனங்களுக்கு என்று **Experience In Fertilizer Trade** திரை பதிவு செய்யும்.

1. **Year of Experience**-ல், டீலர் எத்தனை ஆண்டுகளாக உர வியாபாரத்தில் இருக்கிறார் என்று உள்ளிடுங்கள்.
2. **Add Experience**-ன் கீழ், **Company Name**-ஐத் தேர்வு செய்து **Number of the years**-ஐ உள்ளிடுங்கள்.
3. அந்த நிறுவனத்துக்கான **Quantity (MT)** மற்றும் **Turnover (Lakhs)**-ஐ உள்ளிடுங்கள்.
4. டீலர் இப்போது அந்த நிறுவனத்துடன் வேலை செய்யவில்லை என்றால் **Still Active**-ஐ அணையுங்கள் (off).
5. **Add Experience** அழுத்துங்கள். நிறுவனம் **Saved Experience**-ன் கீழ் தெரியும். ஒவ்வொரு நிறுவனத்துக்கும் இதையே செய்யுங்கள்.
6. ஒரு பதிவை மாற்ற பென்சிலைப் பயன்படுத்தி **Update Experience** அழுத்துங்கள், அல்லது நீக்க குப்பைத்தொட்டியைத் தட்டுங்கள். பிறகு **Next** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Please select a company."** (ஒரு நிறுவனத்தைத் தேர்வு செய்யுங்கள்) **Add Experience** அழுத்தும் முன் **Company Name**-ஐத் தேர்வு செய்யுங்கள்.
- **படிப் பட்டையில் படி 2 சிவப்பாகிறது.** **Year of Experience**-ஐ உள்ளிட்டு, குறைந்தது ஒரு நிறுவனத்தைச் சேருங்கள்.
:::

::: section annual-sales
title: கடந்த ஆண்டு விற்பனை (படி 3)
routes: /AnnualSales
for: 
roles: 
designations: 
screenshot: annual-sales.png
minutes: 4
source-hash: 3d5342d2000c7789
confidence: ok

**இந்தத் திரை எதற்கு**
டீலர் **Last Financial Year**-ல் எல்லா நிறுவனங்களுக்கும் சேர்த்து என்ன விற்றார் என்று **Annual Sales Data For All Companies** திரை பதிவு செய்யும்.

1. அட்டவணையின் மேலே உள்ள ஒரு வகை (category) டேபைத் தட்டுங்கள்.
2. ஒரு product குழுவைத் தட்டித் திறந்து அதன் product-களைப் பாருங்கள்.
3. ஒவ்வொரு product-க்கும் **Own Retail Qty**-ஐ உள்ளிடுங்கள். டீலருக்கு மொத்த (wholesale) லைசென்ஸ் இருந்தால், **Sale to Dealer Qty**-ஐயும் உள்ளிடுங்கள்.
4. டர்ன்ஓவர் (**TO (₹)in Lakhs**) மற்றும் மொத்தங்கள் தானாகக் கணக்கிடப்படும். அட்டவணையின் கீழ் உள்ள சுருக்கக் கார்டுகளையும் **Grand Total**-ஐயும் சரிபாருங்கள்.
5. ஒரு product இல்லையென்றால், **Add Product** அழுத்தி, **Category**, **Product Group**, **Product Name**, **Rate Per Unit (RPU)** நிரப்பி, **Save** அழுத்துங்கள்.
6. **Next** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Sale to Dealer Qty நெடுவரிசை இல்லை.** படி 1-ல் டீலருக்கு மொத்த (wholesale) லைசென்ஸ் இருந்தால் மட்டுமே அது தெரியும்.
- **படிப் பட்டையில் படி 3 சிவப்பாகிறது.** குறைந்தது ஒரு product-க்கு அளவை உள்ளிடுங்கள்.
- **"A Product with the name ... already exists in this Category."** (இந்தப் பெயரில் ஒரு product ஏற்கனவே இந்த வகையில் உள்ளது) அந்த product ஏற்கனவே பட்டியலில் உள்ளது. அதன் product குழுவில் தேடுங்கள்.
:::

::: section warehouse
title: அருகிலுள்ள கிடங்குகள், ரயில் ஹெட்கள், துறைமுகங்கள் (படி 4)
routes: /Warehouse
for: 
roles: 
designations: 
screenshot: warehouse.png
minutes: 4
source-hash: bd545fbcf4350bec
confidence: ok

**இந்தத் திரை எதற்கு**
டீலருக்கு அருகிலுள்ள வசதிகளை **Warehouse & Logistics** திரை பதிவு செய்யும். படி 1-ல் நீங்கள் தந்த கடையின் இடத்திலிருந்து தூரம் கணக்கிடப்படும்.

1. **Logistics Note Information**-ஐப் படியுங்கள். குறைந்தது இரண்டு கிடங்குகள், இரண்டு ரயில் ஹெட்கள், இரண்டு துறைமுகங்கள் தேவை.
2. **Warehouse** கார்டில், **Facility Name**-ஐத் தேடித் தேர்வு செய்யுங்கள். **Distance (KM)** தானாக நிரம்பும்.
3. **Freight (₹/ton/Km)**-ஐ உள்ளிட்டு **Add Warehouse** அழுத்துங்கள்.
4. **Rail Head** மற்றும் **Port** கார்டுகளிலும் இதையே செய்யுங்கள்.
5. **Selected Nearest Facilities**-ன் கீழ் உள்ள பட்டியலைச் சரிபாருங்கள். மாற்ற பென்சிலையும், நீக்க குப்பைத்தொட்டியையும் பயன்படுத்துங்கள்.
6. **Next** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"A minimum of two Warehouses, two Rail Heads, and two Ports must be configured to proceed further."** (தொடர, குறைந்தது இரண்டு கிடங்குகள், இரண்டு ரயில் ஹெட்கள், இரண்டு துறைமுகங்கள் தேவை) முதலில் விடுபட்ட வசதிகளைச் சேருங்கள்.
- **"Dealer location (Latitude/Longitude) is not set."** (டீலரின் இடம் அமைக்கப்படவில்லை) படி 1-க்குத் திரும்பிப் போய் **Google Map URL** அல்லது **Latitude & Longitude**-ஐ நிரப்புங்கள்.
- **Add பட்டன் சாம்பல் நிறத்தில் உள்ளது.** முதலில் **Facility Name**-ஐத் தேர்வு செய்து, தூரம் வரும் வரை காத்திருங்கள்.
:::

::: section market-details
title: மார்க்கெட் விவரங்கள் (படி 5)
routes: /MarketDetails
for: 
roles: 
designations: 
screenshot: market-details.png
minutes: 3
source-hash: a7a15b3e04404881
confidence: ok

**இந்தத் திரை எதற்கு**
டீலர் சேவை செய்யும் பிளாக்குகள் மற்றும் கிராமங்களை, அவற்றின் பயிர்கள் மற்றும் விவசாயிகளுடன் **Market Details** திரை பதிவு செய்யும்.

1. **Blocks / Villages**-ன் கீழ், **Name of Block/ Village**-ஐ டைப் செய்யுங்கள்.
2. **Major Crops**-ஐத் திறந்து முக்கியப் பயிர்களை டிக் செய்யுங்கள். பட்டியலில் தேடலாம்.
3. **No of Farmer**-ஐ உள்ளிடுங்கள். **No of Dealer** பெட்டி தெரிந்தால் அதையும் உள்ளிடுங்கள்.
4. **Season From & To Month** மற்றும் **Irrigation Type**-ஐத் தேர்வு செய்யுங்கள்: **CANAL**, **TANK**, **WELL** அல்லது **RAINFED**.
5. **Add Blocks/ Villages** அழுத்துங்கள். பதிவு **Added Market Details**-ன் கீழ் தெரியும். ஒவ்வொரு பிளாக் அல்லது கிராமத்துக்கும் இதையே செய்யுங்கள்.
6. **Next** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No regional blocks added yet."** (இன்னும் பிளாக் எதுவும் சேர்க்கப்படவில்லை) குறைந்தது ஒரு பிளாக் அல்லது கிராமத்தைச் சேருங்கள். இல்லையென்றால் படி 5 சிவப்பாகக் காட்டும்.
- **எனக்கு No of Dealer தெரியவில்லை.** டீலருக்கு மொத்த (wholesale) லைசென்ஸ் இருந்தால் மட்டுமே அது தெரியும்.
:::

::: section companies
title: டீலரின் பகுதியில் உள்ள நிறுவனங்கள் (படி 6)
routes: /Companies
for: 
roles: 
designations: 
screenshot: companies.png
minutes: 1
source-hash: 4709346d2ff331c4
confidence: ok

**இந்தத் திரை எதற்கு**
டீலரின் பகுதியில் எந்த உர நிறுவனங்கள் விற்கின்றன என்று **Companies Operating In Your Place** திரை காட்டும்.

1. **SPIC** மற்றும் **GREEN STAR** உட்பட, அந்தப் பகுதியில் விற்கும் ஒவ்வொரு நிறுவனத்தையும் தட்டுங்கள். டிக் குறி இருந்தால் அது தேர்வாகியுள்ளது.
2. டிக்கை நீக்க நிறுவனத்தை மீண்டும் தட்டுங்கள்.
3. **Next** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **படிப் பட்டையில் படி 6 சிவப்பாகிறது.** குறைந்தது ஒரு நிறுவனத்தை டிக் செய்யுங்கள்.
:::

::: section proprietor
title: உரிமையாளர் மற்றும் பங்குதாரர் விவரங்கள் (படி 7)
routes: /Proprietor
for: 
roles: 
designations: 
screenshot: proprietor.png
minutes: 6
source-hash: 02521efb7ec5b6b9
confidence: ok

**இந்தத் திரை எதற்கு**
வியாபாரத்தின் சட்ட வகையையும், ஒவ்வொரு உரிமையாளர் அல்லது பங்குதாரரையும் அவர்களின் குடும்பத்துடன் **Proprietor Details** திரை பதிவு செய்யும். இந்த விவரங்கள் SDWA நலத்திட்டத்துக்கும் பயன்படும்.

1. **Business Entity Type**-ஐத் தேர்வு செய்யுங்கள்: **Sole Proprietor**, **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** அல்லது **Society**.
2. **Ownership Information**-ன் கீழ், **Authorized Name**, **Son of / Daughter of**, **Phone Number**, **PAN number**, **Aadhaar Card Number** ஆகியவற்றை நிரப்புங்கள்.
3. **Proprietor / Managing Partner Image**-ஐ அப்லோட் செய்து, **Residential Address Details**-ஐ நிரப்புங்கள்.
4. **Family Members**-ன் கீழ், ஒவ்வொரு உறுப்பினரையும் சேர்த்து **Add Details** அழுத்துங்கள். **Other Occupation / Agencies**-ன் கீழ், **Yes** அல்லது **No** தேர்வு செய்து, வேறு ஏதாவது வியாபாரம் இருந்தால் சேருங்கள்.
5. **Save Details** அழுத்துங்கள். அந்த நபர் **Saved Details**-ன் கீழ் தெரிவார். ஒவ்வொரு பங்குதாரரையும் இதே போலச் சேருங்கள்.
6. **Next** அழுத்துங்கள்.

| Business Entity Type | தேவையான நபர்கள் |
|---|---|
| **Sole Proprietor** | சரியாக 1 உரிமையாளர். |
| **Society** | குறைந்தது 1 அதிகாரம் பெற்ற நபர். |
| **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** | குறைந்தது 2 பங்குதாரர்கள். |

## பொதுவான பிரச்சனைகள்

- **"Please upload Proprietor / Managing Partner Image before saving."** (சேமிக்கும் முன் உரிமையாளர் புகைப்படத்தை அப்லோட் செய்யுங்கள்) புகைப்படத்தை அப்லோட் செய்து, மீண்டும் **Save Details** அழுத்துங்கள்.
- **"Please add at least 2 partners for the selected entity type."** (இந்த வகைக்குக் குறைந்தது 2 பங்குதாரர்களைச் சேருங்கள்) **Next** அழுத்தும் முன் இரண்டாவது பங்குதாரரைச் சேமியுங்கள்.
- **ஒருவரைச் சேமித்த பிறகு படிவம் மறைந்துவிட்டது.** **Sole Proprietor**-க்கு ஒருவர் மட்டுமே அனுமதி. அதை மாற்ற **Saved Details**-ன் கீழ் உள்ள பென்சிலைப் பயன்படுத்துங்கள்.
:::

::: section sales-planning
title: வரும் ஆண்டுக்கான விற்பனைத் திட்டம் (படி 8)
routes: /SalesPlaning
for: 
roles: 
designations: 
screenshot: sales-planning.png
minutes: 4
source-hash: b908e1cc20e482d4
confidence: ok

**இந்தத் திரை எதற்கு**
வரும் நிதியாண்டில், ஏப்ரல் முதல் மார்ச் வரை ஒவ்வொரு மாதமும் டீலர் எவ்வளவு விற்கத் திட்டமிடுகிறார் என்று **Sales Planning & Forecast** திரை பதிவு செய்யும்.

1. **Business Plan**-ன் கீழ், ஒரு வகை (category) டேபைத் தட்டுங்கள்.
2. ஒரு product குழுவைத் தட்டித் திறங்கள்.
3. ஒவ்வொரு product-க்கும், **April** முதல் **March** வரை ஒவ்வொரு மாதத்தின் கீழும் திட்டமிட்ட அளவை உள்ளிடுங்கள். லட்சத்தில் மதிப்பும் **Total**-உம் தானாகக் கணக்கிடப்படும்.
4. **Monthly Total** வரிசையையும், **Fertilizer Products Summary** போன்ற சுருக்கக் கார்டுகளையும் சரிபாருங்கள்.
5. **Next** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **படிப் பட்டையில் படி 8 சிவப்பாகிறது.** குறைந்தது ஒரு product-க்குத் திட்டத்தை உள்ளிடுங்கள்.
:::

::: section investment
title: சொத்துகள், கடன்கள், கிரெடிட் சம்மதம் (படி 9)
routes: /Investment
for: 
roles: 
designations: 
screenshot: investment.png
minutes: 5
source-hash: 6476f16e152121ca
confidence: ok

**இந்தத் திரை எதற்கு**
டீலருக்கு என்ன சொந்தம், என்ன கடன் உள்ளது என்று **Investment** திரை பதிவு செய்து, **Net Financial Position**-ஐக் காட்டும்.

1. டீலர் கிரெடிட் லிமிட் விவரங்களைத் தரமாட்டார் என்றால், **Give Consent** அழுத்துங்கள். அப்போது கிரெடிட் லிமிட் படிகள் தவிர்க்கப்பட்டு பூஜ்ஜியமாகப் பதிவாகும். **Remove Consent** அதை ரத்து செய்யும்.
2. **Assets (Banking & Properties)**-ன் கீழ், **Banking**, **Land** அல்லது **Building**-ல் குறைந்தது ஒன்றைச் சேருங்கள். கார்டை நிரப்பி **Add Banking**, **Add Land** அல்லது **Add Building** அழுத்துங்கள்.
3. நிலம் மற்றும் கட்டடங்களுக்கு, உங்களிடம் இருந்தால் **EC (Encumbrance Certificate)**, **Property Document**, **Valuation Certificate** ஆகியவற்றை அப்லோட் செய்யுங்கள்.
4. **Movable** மற்றும் **Infrastructure**-ன் கீழ், **Movable Assets Value**, **Own Godown Capacity**, **Rent Godown Capacity** ஆகியவற்றை உள்ளிடுங்கள்.
5. **Loan Liabilities List**-ன் கீழ், ஒவ்வொரு கடனுக்கும் **Loan Source / Institution** மற்றும் **Loan Value (₹) In Lakhs**-ஐ உள்ளிட்டு, **Add Loan** அழுத்துங்கள்.
6. **Net Financial Position**-ஐச் சரிபாருங்கள்: **Fixed Assets** கழித்தல் **Total Liabilities**. பிறகு **Next** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Please add details for at least one of Banking, Land, or Building before proceeding."** (தொடரும் முன் Banking, Land, Building-ல் குறைந்தது ஒன்றைச் சேருங்கள்) முதலில் ஒரு சொத்தைச் சேருங்கள்.
- **"OD Value is required when Banking details are provided."** (வங்கி விவரம் தந்தால் OD மதிப்பு கட்டாயம்) **OD Limit (₹) Lakhs**-ஐ உள்ளிடுங்கள்.
- **"Market Value is required when Land details are provided."** (நில விவரம் தந்தால் சந்தை மதிப்பு கட்டாயம்) சந்தை மதிப்பை உள்ளிடுங்கள். கட்டடத்துக்கும் இதே விதி.
:::

::: section credit-limit
title: கிரெடிட் லிமிட் அல்லது டிரேட் டெபாசிட் (படிகள் 10 மற்றும் 11)
routes: /CreditLimit, /CreditLimitForGreenStar
for: 
roles: 
designations: 
screenshot: credit-limit.png
minutes: 5
source-hash: d34b3a1b414aa494
confidence: ok

**இந்தத் திரை எதற்கு**
படி 10 SPIC-க்கு, படி 11 Greenstar-க்கு. படி 1-ல் **GREEN STAR** தேர்வு செய்திருந்தால் மட்டுமே படி 11 தெரியும். ஏற்கனவே உள்ள டீலருக்கு, இந்தத் திரைகள் (**Credit Limit — SPIC** மற்றும் **Credit Limit — GreenStar**) பரிந்துரைக்கப்படும் கிரெடிட் லிமிட்டைக் கணக்கிடும். புதிய டீலருக்கு, இவை டிரேட் டெபாசிட்டைப் பதிவு செய்யும்.

1. மேலே உள்ள டீலர் சுருக்கம், **Existing Credit Limit** (**Valid From** மற்றும் **Valid To**), விற்பனைத் திட்டக் கார்டுகளைச் சரிபாருங்கள்.
2. **Details of Securities**-ன் கீழ், **Fixed Deposit with SPIC** அல்லது **BG in favour of SPIC** போன்ற ஒவ்வொரு பிணைக்கும் **Number**, **Other Details**, **Amount (₹) In Lakhs** ஆகியவற்றை உள்ளிடுங்கள்.
3. **SPIC Valuation** அல்லது **Greenstar Valuation** அட்டவணையில், பெரும்பாலான மதிப்புகள் முந்தைய படிகளிலிருந்து வரும். **Monthly Average Net Overdues during last year**-ஐ நீங்களே உள்ளிடுங்கள். இது கட்டாயம், 0 அல்லது அதற்கு மேல்.
4. **Grand Total** மதிப்பெண்ணையும், **Recommended Credit Limit** பெட்டியையும் சரிபாருங்கள்: **Proposed Limit**, **Credit Multiplier**, **MO Recommended Credit Limit**.
5. **Additional Credit Limit (₹) In Lakhs** ஏதாவது இருந்தால் உள்ளிட்டு, **Next** அழுத்துங்கள்.

## புதிய டீலருக்கு

- படி 10-ல், **Application Fee** (**Bank**, **DD Number**, **DD Date**, **Amount (₹)**, **Payable At**) மற்றும் **Trade Deposit Details**-ஐ நிரப்புங்கள். இரண்டு இணைப்புகளையும் PDF, JPG அல்லது PNG-ஆக அப்லோட் செய்யுங்கள்.
- படி 11-ல், **GFL Trade Deposit Details**-ஐ நிரப்பி இணைப்பை அப்லோட் செய்யுங்கள்.

> Tip: ஒப்புதல் பெற்ற டீலருக்கு, டீலர் கார்டில் உள்ள **Credit Limit** பட்டன் இந்தத் திரையைத் தனியாகத் திறக்கும். அப்போது கீழே உள்ள பட்டனில் **Submit** என்று இருக்கும்.

## பொதுவான பிரச்சனைகள்

- **"Monthly Average Net Overdues during last year is required and must be 0 or greater."** (இந்த மதிப்பு கட்டாயம், 0 அல்லது அதற்கு மேல் இருக்க வேண்டும்) மதிப்பு 0 ஆக இருந்தாலும் அதை உள்ளிடுங்கள்.
- **"Complete all SPIC Application Fee and Trade Deposit fields, and upload both attachments."** (எல்லாப் புலங்களையும் நிரப்பி இரண்டு இணைப்புகளையும் அப்லோட் செய்யுங்கள்) ஒவ்வொரு பெட்டியையும் நிரப்பி இரண்டு கோப்புகளையும் அப்லோட் செய்யுங்கள்.
- **இந்தப் படிகள் திறக்கவில்லை.** **Investment** படியில் சம்மதம் (consent) தரப்பட்டுள்ளது.
:::

::: section enclosures
title: ஆவணங்களை அப்லோட் செய்வது (படி 12)
routes: /Enclosures
for: 
roles: 
designations: 
minutes: 5
source-hash: f0405468232d5c91
confidence: ok

**இந்தத் திரை எதற்கு**
**Enclosures** திரை எல்லா ஆவணங்களையும் ஒரே இடத்தில் சேகரிக்கும். எந்த ஆவணங்கள் **Uploaded**, எவை **Missing** என்று இது காட்டும்.

1. மேலே உள்ள **Document Completion Progress**-ஐச் சரிபாருங்கள். எத்தனை ஆவணங்கள் அப்லோட் ஆகியுள்ளன, எத்தனை கட்டாய ஆவணங்கள் நிலுவையில் உள்ளன என்று அது காட்டும்.
2. **Documents already added in previous steps**-ன் கீழ், GST, PAN, Aadhaar, லைசென்ஸ் கோப்புகளைச் சரிபாருங்கள். **Missing** என்று உள்ளதை **Upload Document** மூலம் அப்லோட் செய்யுங்கள்.
3. **Partner / Proprietor Documents** மற்றும் **Property Documents (Land & Building)**-ஐச் சரிபாருங்கள். விடுபட்ட புகைப்படம், Aadhaar, PAN, EC, சொத்து ஆவணம் அல்லது மதிப்பீட்டுச் சான்றிதழை அப்லோட் செய்யுங்கள்.
4. **Additional Documents**-ன் கீழ், ஆண்டைத் தேர்வு செய்து **IT Returns for Last Year** மற்றும் **IT Returns for Last 2nd Year**-ஐயும், **Required** என்று குறிக்கப்பட்ட மற்ற எல்லா ஆவணங்களையும் அப்லோட் செய்யுங்கள்.
5. மாதிரிக் கையொப்பத்துக்கு, **Download Template** அழுத்தி, அதை அச்சிட்டு, எல்லாக் கையொப்பதாரர்களிடமும் கையெழுத்து வாங்கி, **Spic Specimen Signature** அல்லது **Greenstar Specimen Signature**-ன் கீழ் அப்லோட் செய்யுங்கள்.
6. கடைசிச் சரிபார்ப்புக்குப் போக **Next** அழுத்துங்கள்.

> Careful: ஆவணத்தை நீக்கினால் அதைத் திரும்பப் பெற முடியாது. தவறுதலாக நீக்கிவிட்டால், சரியான கோப்பை மீண்டும் அப்லோட் செய்யுங்கள்.

| ஆவணம் | என்ன அப்லோட் செய்ய வேண்டும் |
|---|---|
| பெரும்பாலான ஆவணங்கள் | PDF, JPG அல்லது PNG, 10 MB வரை. |
| **List of Retailers with Complete Details** | கேட்கப்படும்போது Excel அல்லது CSV கோப்பு. வடிவத்துக்கு **Download Sample Template** அழுத்துங்கள். |
| **Request Letter for Dealership** | புதிய டீலர்களுக்கு மட்டும். நிறுவனத்தின் லெட்டர்ஹெட்டில், முத்திரை மற்றும் கையொப்பத்துடன். |

## பொதுவான பிரச்சனைகள்

- **"You still have ... required document(s) pending."** (இன்னும் சில கட்டாய ஆவணங்கள் நிலுவையில் உள்ளன) **Required** மற்றும் **Missing** என்று குறிக்கப்பட்ட ஒவ்வொரு ஆவணத்தையும் அப்லோட் செய்யுங்கள்.
- **"No partners found. Add partners in Proprietor step first."** (பங்குதாரர்கள் இல்லை. முதலில் Proprietor படியில் சேருங்கள்) படி 7-க்குத் திரும்பிப் போய் உரிமையாளர்கள் அல்லது பங்குதாரர்களைச் சேமியுங்கள்.
:::

::: section final-submission
title: விண்ணப்பத்தைச் சரிபார்த்துச் சமர்ப்பிப்பது (படி 13)
routes: /FinalSubmission
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-fs-progress] | 2 > [data-help=dr-fs-sections] | 3 > [data-help=dr-fs-enclosures] | 4 > [data-help=dr-fs-terms] | 5 > [data-help=dr-fs-result]
minutes: 3
source-hash: ec4c07bdc1524795
confidence: ok

**இந்தத் திரை எதற்கு**
**Final Submission** திரைதான் கடைசிப் படி. எது முடிந்துள்ளது, எது விடுபட்டுள்ளது என்று காட்டி, விண்ணப்பத்தை ஒப்புதலுக்கு அனுப்பும்.

1. மேலே உள்ள முன்னேற்றக் கார்டைப் பாருங்கள். எத்தனை பகுதிகள் முடிந்துள்ளன என்று அது காட்டும். 100% ஆனதும் **Application Ready for Submission** என்று காட்டும்.
2. **Section Completion Status**-ன் கீழ், பச்சை டிக் என்றால் அந்தப் பகுதி முடிந்துவிட்டது, ஆரஞ்சுக் குறி என்றால் ஏதோ விடுபட்டுள்ளது. அந்தப் படியைத் திறந்து சரி செய்ய பென்சிலைத் தட்டுங்கள்.
3. **Enclosures** கார்டில், **Documents Uploaded** மற்றும் **Missing Documents**-ஐச் சரிபாருங்கள். விடுபட்ட ஆவணத்துக்கு ஆரஞ்சு நிறப் பெருக்கல் குறி (X) இருக்கும்.
4. **Terms and Conditions** மற்றும் **I hereby declare** குறிப்புகளைப் படியுங்கள். சமர்ப்பிக்கும்போது அவற்றை ஏற்றுக்கொள்கிறீர்கள்.
5. பச்சை நிற **All sections completed!** பெட்டி தெரிந்ததும், **Submit Registration** அழுத்துங்கள். **Registration submitted successfully!** என்று வரும், பிறகு டீலர் பட்டியலுக்குத் திரும்புவீர்கள். அங்கே விண்ணப்பம் **In RM** என்று காட்டும்.

> Tip: மேலே உள்ள படிப் பட்டையின் எந்தப் பகுதியையும் தட்டி, நேராக அந்தப் படிக்குப் போகலாம்.

## பொதுவான பிரச்சனைகள்

- **"Please complete all ... incomplete section(s) before submitting."** (சமர்ப்பிக்கும் முன் முடிக்காத பகுதிகளை முடியுங்கள்) முதலில் ஆரஞ்சுக் குறியுள்ள ஒவ்வொரு பகுதியையும் சரி செய்யுங்கள்.
- **"Error submitting registration"** (பதிவைச் சமர்ப்பிப்பதில் பிழை) செய்தியைப் படியுங்கள். சர்வர் என்ன விடுபட்டதாகக் கண்டது என்று அது சொல்லும். அதைச் சரி செய்து மீண்டும் சமர்ப்பியுங்கள்.
- **Submit Registration பட்டன் இல்லை.** விண்ணப்பம் பார்க்க மட்டும் திறக்கப்பட்டுள்ளது. டீலர் கார்டில் உள்ள **Edit** மூலம் அதைத் திறங்கள். விண்ணப்பம் **Draft** அல்லது **Rejected by RM** நிலையில் இருக்கும்போது மட்டுமே **Edit** தெரியும்.
:::

::: section application-pdf
title: டீலர்ஷிப் விண்ணப்பப் படிவத்தைப் பார்ப்பது
routes: /DealershipPDF/{dealerId:int?}
for: 
roles: 
designations: 
minutes: 2
source-hash: 916c338c0a45b3ba
confidence: ok

**இந்தத் திரை எதற்கு**
SPIC-க்கு அல்லது Greenstar-க்கு, முழு விண்ணப்பத்தையும் ஒரே **DEALERSHIP APPLICATION FORM**-ஆக இந்தத் திரை காட்டும். விண்ணப்பத்தைப் படிக்க அல்லது அச்சிட்ட நகலை வைத்துக்கொள்ள இதைப் பயன்படுத்துங்கள்.

1. டீலர் பட்டியலில், டீலர் கார்டைக் கண்டுபிடித்து **SPIC** அல்லது **GFL**-ஐத் தட்டுங்கள்.
2. மேலே நிறுவனத்தின் பெயர் மற்றும் லோகோவுடன் படிவம் திறக்கும்.
3. ஒவ்வொரு பகுதியையும் படிக்கக் கீழே ஸ்க்ரோல் செய்யுங்கள்: **Dealer Details**, **Business Information**, **Bank Details**, **Proprietor / Partner Details**, **Sales Planning & Forecast**, **Investment Details** மற்றும் பல.
4. கடைசிக்கு அருகில், கிரெடிட் லிமிட், **Recommended Credit Limit**, **Attached Documents** ஆகியவற்றைப் பாருங்கள்.
5. நகல் வைத்துக்கொள்ள, உங்கள் பிரவுசர் அல்லது சாதனத்தின் அச்சிடும் (print) வசதியைப் பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **மேலே "Credit Limit Consent: GIVEN" என்று உள்ளது.** டீலர் கிரெடிட் லிமிட் விவரங்கள் எதுவும் தரவில்லை, அதனால் கிரெடிட் லிமிட் 0.
- **GFL பட்டன் இல்லை.** டீலர் SPIC-க்கு மட்டுமே பதிவு செய்யப்பட்டுள்ளார்.
:::

::: section entry-info
title: லக்கி டிரா பதிவு விவரங்கள்
routes: /EntryInfo
for: 
roles: 
designations: 
minutes: 1
source-hash: 24f199e7d7d10b2f
confidence: ok

**இந்தத் திரை எதற்கு**
லக்கி டிராவுக்கான ஒரு வாங்குதல் பதிவை **Entry Information** திரை காட்டும். இந்தத் திரை இன்னும் உருவாக்கப்பட்டு வருகிறது: இது மாதிரி மதிப்புகளைக் காட்டும், உண்மையான பதிவுகளை அல்ல.

1. மேலே, பதிவு எண்ணையும் அதன் நிலையையும் பாருங்கள்.
2. பதிவுக் கார்டில், **Date**, **Dealer Name**, **Source**, **Location**, **Product**, **Purchase Amount** ஆகியவற்றைப் பாருங்கள்.
3. **LUCKY DRAW INFO**-ன் கீழ், பதிவு தகுதியானதா, தற்போதைய சுற்று எவ்வளவு தூரம் வந்துள்ளது என்று பாருங்கள்.
4. **Verification**-ன் கீழ், **Invoice Number**-ஐயும், பதிவு கணக்கில் சேர்க்கப்பட்டதா என்பதையும் பாருங்கள்.
5. திரும்பிப் போக மேலே உள்ள பின் அம்புக்குறியை அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **மதிப்புகள் ஒருபோதும் மாறுவதில்லை.** இப்போதைக்கு இந்தத் திரை மாதிரி மதிப்புகளை மட்டுமே காட்டும்.
:::
