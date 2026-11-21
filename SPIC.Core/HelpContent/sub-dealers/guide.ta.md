---
module: sub-dealers
title: சப் டீலர்கள்
icon: bi-people
order: 103
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
source-hash: 0365bef89cdeeedd
confidence: ok

ஒவ்வொரு சப் டீலரைப் பற்றியும் சரியான பதிவு வைத்திருங்கள்: கடை எங்கே இருக்கிறது, அவர்களை எப்படித் தொடர்பு கொள்வது, அவர்களுடைய GST, PAN, mFMS விவரங்கள். சப் டீலர்கள் மற்றும் டீலர்களின் ஊழியர்களுக்கான நலத்திட்டப் பயனாளிகளையும் இதே மாட்யூல் வைத்திருக்கும்.

- import | icon: bi-file-earmark-excel | color: #8b5cf6
  title: Admin மாஸ்டரை அப்லோட் செய்வார்
  text: ஒவ்வொரு State, Region, HQ-க்கும் ஒப்புதல் பெற்ற சப் டீலர் குறியீடுகளின் Excel பட்டியலை Admin இம்போர்ட் செய்வார்.
- maintain | icon: bi-pencil-square | color: #0d9488
  title: MO விவரங்களை நிரப்புவார்
  text: MO, HQ-வின் ஒவ்வொரு சப் டீலரையும் தேர்வு செய்து, இடம், தொடர்பு எண்கள், GST, PAN, mFMS ஆகியவற்றை நிரப்புவார்.
- welfare | icon: bi-heart | color: #d97706
  title: நலத்திட்டப் பயனாளிகள்
  text: சப் டீலர் மற்றும் ஊழியர் பயனாளிகளுக்கு முதலில் SM, பிறகு AVP ஒப்புதல் தருவார்கள்.
:::

::: section flow
title: சப் டீலர் பதிவு எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 0b02b6ac4a67732f
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- admin | type: lane | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin மற்றும் அலுவலகம்
  sub: தலைமை அலுவலகம்
- field | type: lane | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: உங்கள் HQ
- sm | type: lane | color: #d97706 | roles: SMD, SMM
  title: SM
  sub: SMD · SMM
- avp | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: இறுதி ஒப்புதல்
- import | lane: admin | col: 1 | icon: bi-file-earmark-excel | color: #8b5cf6 | next: maintain | screen: import-master
  title: மாஸ்டரை இம்போர்ட் செய்தல்
  text: ஒப்புதல் பெற்ற Excel பட்டியலை Admin அப்லோட் செய்வார். யாராவது விவரங்களைச் சேமிக்கும் வரை, புதிய சப் டீலர்கள் பட்டியலில் தெரிய மாட்டார்கள்.
- maintain | lane: field | col: 2 | icon: bi-pencil-square | color: #16a34a | next: beneficiary | screen: maintain-sub-dealer
  title: விவரங்களை நிரப்புதல்
  status: Active
  text: MO சப் டீலரைத் தேர்வு செய்து, இடம், தொடர்பு எண்கள், GST, PAN, mFMS ஆகியவற்றை நிரப்பி, **Submit** அழுத்துவார். இப்போது அது **Sub Dealer Master** பட்டியலில் தெரியும்.
- beneficiary | lane: admin | col: 3 | icon: bi-person-plus | color: #d97706 | next: sm-approve | screen: beneficiary-add
  title: பயனாளியைச் சேர்த்தல்
  status: Pending
  text: **Sub Dealer & Employee** திரையில், அலுவலகப் பணியாளர்கள் ஒரு சப் டீலரின் அல்லது ஒரு டீலரின் ஊழியரின் நலத்திட்டப் பயனாளியைச் சேர்ப்பார்கள்.
- sm-approve | lane: sm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: avp-approve | screen: beneficiary-approve
  title: SM ஒப்புதல்
  status: Pending
  text: SMD அல்லது SMM பதிவைச் சரிபார்த்து, ஒப்புதல் தருவார் அல்லது நிராகரிப்பார்.
- avp-approve | lane: avp | col: 5 | icon: bi-patch-check | color: #2563eb | screen: beneficiary-approve
  title: AVP ஒப்புதல்
  status: Approved
  text: AVP இறுதி ஒப்புதல் தருவார். இனி நலத்திட்டத்துக்கு விண்ணப்பிக்கும்போது டீலர் இந்த நபரைத் தேர்வு செய்யலாம்.
- rejected | lane: sm | col: 5 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: sm-approve | screen: beneficiary-approve
  title: நிராகரிக்கப்பட்டது
  status: SM Rejected
  text: SM அல்லது AVP பதிவை நிராகரித்தால், அது அங்கேயே நின்றுவிடும். டீலருக்கு அது காட்டப்படாது.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9462538c97a0b6f7
confidence: ok

- active | color: #16a34a
  label: Active
  text: சப் டீலர் அல்லது பயனாளி பயன்பாட்டில் உள்ளார். Active சப் டீலருக்கு * குறியிட்ட எல்லாப் புலங்களும் தேவை.
- inactive | color: #64748b
  label: Inactive
  text: இப்போது பயன்பாட்டில் இல்லை. Inactive சப் டீலருக்கு State, Region, HQ, Firm Name மட்டும் போதும்.
- pending | color: #d97706
  label: Pending
  text: பயனாளி SM அல்லது AVP ஒப்புதலுக்காகக் காத்திருக்கிறார்.
- approved | color: #2563eb
  label: Approved
  text: இந்த நிலையில் ஒப்புதல் கிடைத்துவிட்டது. Overall நெடுவரிசையில் இருந்தால், SM, AVP இருவரும் ஒப்புதல் தந்துவிட்டார்கள் என்று அர்த்தம்.
- rejected | color: #dc2626
  label: Rejected
  text: இந்த நிலையில் மறுக்கப்பட்டது. SM Status அல்லது AVP Status நெடுவரிசையில் தெரியும்.
- sm-rejected | color: #dc2626
  label: SM Rejected
  text: SM பயனாளியை மறுத்துவிட்டார். அது AVP-க்குப் போகாது.
- avp-rejected | color: #b91c1c
  label: AVP Rejected
  text: SM ஒப்புதல் தந்தார், ஆனால் AVP பயனாளியை மறுத்துவிட்டார்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e89557ba6534118
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- admin | color: #8b5cf6 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - சப் டீலர் மாஸ்டரை Excel-லிருந்து இம்போர்ட் செய்வது
  - எந்த சப் டீலரின் விவரங்களையும் நிரப்புவது அல்லது திருத்துவது
  - சப் டீலர் மற்றும் ஊழியர் பயனாளிகளைச் சேர்ப்பது
- field | color: #0d9488 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - உங்கள் HQ-வின் சப் டீலர்களின் விவரங்களை நிரப்புவது, புதுப்பிப்பது
  - தவறாகச் சேமித்த விவரங்களை நீக்குவது
- rm | color: #64748b | roles: RM, RMD
  title: RM, RMD
  - உங்கள் மண்டலத்தின் சப் டீலர்களைப் பார்ப்பது
  - பட்டியலை Excel-ஆகப் பதிவிறக்குவது
- sm | color: #d97706 | roles: SMD, SMM
  title: SMD, SMM
  - உங்கள் மாநிலத்தின் சப் டீலர்களைப் பார்ப்பது
  - பயனாளிகளுக்கு முதலில் ஒப்புதல் தருவது அல்லது நிராகரிப்பது
- avp | color: #2563eb | roles: AVP, Director
  title: AVP மற்றும் Director
  - உங்கள் பகுதியின் சப் டீலர்களின் விவரங்களை நிரப்புவது அல்லது புதுப்பிப்பது
  - பயனாளிகளுக்கு AVP இறுதி ஒப்புதல் தருவார்
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 3f822e638d6491b2
confidence: ok

- not-in-dropdown | popular: true
  q: Select Imported Sub Dealer-ல் ஒரு சப் டீலரைக் கண்டுபிடிக்க முடியவில்லை
  a: உங்கள் State, Region, HQ-க்கு Admin இம்போர்ட் செய்த சப் டீலர்கள் மட்டுமே படிவத்தில் தெரியும். படிவத்தில் புதிய குறியீட்டை உருவாக்க முடியாது. அதை மாஸ்டரில் சேர்க்குமாறு Admin-இடம் கேளுங்கள்.
- list-empty | popular: true
  q: Admin சப் டீலர்களை அப்லோட் செய்துவிட்டார், ஆனால் Sub Dealer Master பட்டியல் காலியாக உள்ளது
  a: விவரங்கள் சேமிக்கப்பட்ட சப் டீலர்கள் மட்டுமே பட்டியலில் தெரியும். **Maintain Sub Dealer** அழுத்தி, சப் டீலரைத் தேர்வு செய்து, விவரங்களை நிரப்பி, **Submit** அழுத்துங்கள்.
- submit-grey
  q: Submit பட்டன் சாம்பல் நிறத்தில் உள்ளது
  a: முதலில் ஒரு சப் டீலரைத் தேர்வு செய்து, அதன் விவரங்கள் ஏறும் வரை காத்திருங்கள். GST PDF, இடம் அல்லது பின்கோடு படிக்கப்படும்போதும் காத்திருங்கள்.
- edit-grey
  q: பட்டியலில் பென்சில், குப்பைத்தொட்டி ஐகான்கள் சாம்பல் நிறத்தில் உள்ளன
  a: உங்கள் பணிப்பொறுப்பால் சப் டீலர்களைப் பார்க்க மட்டுமே முடியும். MO, MDO, JMDO, Admin, AVP, Director மட்டுமே அவற்றை மாற்ற முடியும்.
- no-approve
  q: நான் ஒப்புதல் தருபவர், ஆனால் பயனாளியின் வரிசையில் டிக் அல்லது பெருக்கல் குறி தெரியவில்லை
  a: **SM Status** **Pending** ஆக இருக்கும்போது மட்டுமே SM-க்கு அவை தெரியும். SM ஒப்புதல் தந்த பிறகு, **AVP Status** **Pending** ஆக இருக்கும்போது மட்டுமே AVP-க்கு அவை தெரியும்.
:::

::: section sub-dealer-list
title: சப் டீலர் பட்டியலைப் பார்ப்பது
routes: /SubDealerList
for: 
roles: 
designations: 
tour: 1 > [data-help=sdl-cards] | 2 > [data-help=sdl-filters] | 3 > [data-help=sdl-table] | 4 > [data-help=sdl-actions] | 5 > [data-help=sdl-summary]
minutes: 2
source-hash: eb1071bb27a63554
confidence: ok

**இந்தத் திரை எதற்கு**
**Sub Dealer Master** திரையில், சப் டீலர் படிவம் மூலம் விவரங்கள் சேமிக்கப்பட்ட சப் டீலர்கள் தெரிவார்கள். MO தன் HQ-வையும், RM மண்டலத்தையும், SMM அல்லது SMD மாநிலத்தையும் பார்ப்பார்கள். நீங்கள் எந்தப் பகுதியைப் பார்க்கிறீர்கள் என்று மேலே ஒரு குறிப்பு சொல்லும்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Sub Dealers**, **Active**, **Inactive**.
2. **Search**-ல் குறியீடு, பெயர், மொபைல், GST, PAN அல்லது mFMS ID டைப் செய்யுங்கள். பட்டியலைச் சுருக்க **State**, **Region**, **HQ** அல்லது **Status** தேர்வு செய்யுங்கள். உங்கள் சொந்தப் பகுதி ஏற்கனவே தேர்வு செய்யப்பட்டு, பூட்டப்பட்டிருக்கும்.
3. **Action** நெடுவரிசையில், கண் ஐகான் சப் டீலரைப் பார்க்கத் திறக்கும். பென்சில் திருத்தத் திறக்கும். குப்பைத்தொட்டி அதன் சேமித்த விவரங்களை நீக்கும்.
4. மேலே உள்ள பட்டன்களைப் பயன்படுத்துங்கள். **Refresh** பட்டியலை மீண்டும் ஏற்றும். **Export All Saved** சேமித்த எல்லா சப் டீலர்களையும் Excel-ஆகப் பதிவிறக்கும். **Maintain Sub Dealer** படிவத்தைத் திறக்கும்.
5. கீழே **State-wise Sub Dealer Summary**-க்கு ஸ்க்ரோல் செய்யுங்கள். ஒவ்வொரு மாநிலத்தின் மாஸ்டர் பதிவுகளை, சேமித்த **Active**, **Inactive** சப் டீலர்களுடன் அது ஒப்பிட்டுக் காட்டும்.

> Careful: குப்பைத்தொட்டி சேமித்த விவரங்களை மட்டுமே நீக்கும், சப் டீலரை அல்ல. யாராவது மீண்டும் விவரங்களை நிரப்பும் வரை அது இந்தப் பட்டியலில் தெரியாது.

## பொதுவான பிரச்சனைகள்

- **"No saved Sub Dealers found".** (சேமித்த சப் டீலர்கள் இல்லை) விவரங்கள் சேமிக்கப்பட்ட சப் டீலர்கள் மட்டுமே இங்கே தெரியும். Admin அப்லோட் செய்து, யாரும் நிரப்பாத வரிசைகள் மறைக்கப்பட்டிருக்கும்.
- **பென்சிலும் குப்பைத்தொட்டியும் சாம்பல் நிறத்தில் உள்ளன.** உங்கள் பணிப்பொறுப்பால் பார்க்க மட்டுமே முடியும். MO, MDO, JMDO, Admin, AVP, Director மட்டுமே சப் டீலர்களை மாற்ற முடியும்.
- **Maintain Sub Dealer பட்டன் இல்லை.** உங்கள் பணிப்பொறுப்பால் பட்டியலைப் பார்க்க மட்டுமே முடியும்.
- **"No saved Sub Dealer records are available to export."** (பதிவிறக்க சேமித்த சப் டீலர் பதிவுகள் இல்லை) உங்கள் பகுதியில் இதுவரை எதுவும் சேமிக்கப்படவில்லை.
:::

::: section maintain-sub-dealer
title: சப் டீலரின் விவரங்களை நிரப்புவது அல்லது புதுப்பிப்பது
routes: /SubDealerRegistration
for: 
roles: 
designations: 
tour: 1 > [data-help=sdr-business] | 2 > [data-help=sdr-basic] | 3 > [data-help=sdr-location] | 4 > [data-help=sdr-tax] | 5 > [data-help=sdr-mfms] | 6 > [data-help=sdr-submit]
minutes: 5
source-hash: e410f07c765072a5
confidence: ok

**இந்தத் திரை எதற்கு**
Admin ஏற்கனவே இம்போர்ட் செய்த சப் டீலரின் விவரங்களை நிரப்ப **Sub Dealer Maintenance** திரையைப் பயன்படுத்துங்கள். இங்கே புதிய சப் டீலர் குறியீட்டை உருவாக்க முடியாது. பட்டியலில் உள்ள **Maintain Sub Dealer** மூலமோ, ஒரு வரிசையில் உள்ள பென்சில் மூலமோ இதைத் திறங்கள்.

1. **Business Information**-ன் கீழ், **Select Imported Sub Dealer** தட்டி, குறியீடு அல்லது பெயரை டைப் செய்து சப் டீலரைத் தேர்வு செய்யுங்கள். அதன் சேமித்த விவரங்கள் ஏறும். **Firm Name**-ஐச் சரிபாருங்கள் (GST-ல் உள்ள வணிகப் பெயர்படி).
2. **Basic Information**-ன் கீழ், **State**, **Region**, **HQ**-ஐச் சரிபார்த்து, **Status** தேர்வு செய்யுங்கள்: **Active** அல்லது **Inactive**.
3. **Primary Location**-ன் கீழ், **Use Current Location** அழுத்துங்கள், அல்லது மேப்பில் தட்டுங்கள், அல்லது Google Maps லிங்கை ஒட்டுங்கள். முகவரியை நிரப்புங்கள். 6 இலக்க **Pincode** டைப் செய்தால் State, District, Block, Taluk, Village தானாக நிரம்பும். **Official Contact Number**, **WhatsApp Number**, **Alternative Number** ஆகியவற்றை உள்ளிடுங்கள்.
4. **Tax & Compliance**-ன் கீழ், **Upload GST Certificate (PDF)** தட்டுங்கள். ஆப் PDF-ஐப் படித்து **GST Number**, **Legal Name**, **Trade Name**, **Constitution of Business** ஆகியவற்றை நிரப்பும். அவற்றைச் சரிபார்த்து, பிறகு **PAN Number** உள்ளிடுங்கள்.
5. **mFMS Information**-ன் கீழ், **Retail mFMS ID** உள்ளிடுங்கள். **Wholesale mFMS ID** கட்டாயமில்லை.
6. **Submit** அழுத்துங்கள். சப் டீலர் சேமிக்கப்பட்டது என்று பச்சை நிறச் செய்தி வரும், அடுத்ததற்காகப் படிவம் காலியாகும்.

> Tip: **Active** சப் டீலருக்கு * குறியிட்ட எல்லாப் புலங்களும் தேவை. **Inactive** சப் டீலருக்கு State, Region, HQ, Firm Name மட்டும் போதும்.
> Careful: கண் ஐகான் மூலம் சப் டீலரைத் திறந்தால், எல்லாப் புலங்களும் சாம்பல் நிறத்தில் இருக்கும், **Submit** பட்டன் இருக்காது. மாற்ற பென்சிலைப் பயன்படுத்துங்கள்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Select Imported Sub Dealer** | Admin அப்லோட் செய்ததில் உள்ள சப் டீலர். கட்டாயம். |
| **Status** | சப் டீலர் இப்போது இயங்கினால் **Active**, இல்லையென்றால் **Inactive**. |
| **Google Map URL** மற்றும் **Latitude & Longitude** | கடையின் இடம். Active-க்குக் கட்டாயம். |
| **Pincode** | 6 இலக்கங்கள். இது ஊர்ப் பெயர்களை உங்களுக்காக நிரப்பும். |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 இலக்க மொபைல் எண்கள். Active-க்குக் கட்டாயம். |
| **GST Number** | 15 எழுத்துகள், உதாரணமாக 22AAAAA0000A1Z5. Active-க்குக் கட்டாயம். |
| **Upload GST Certificate (PDF)** | ஒரே ஒரு PDF கோப்பு, 5 MB வரை. Active-க்குக் கட்டாயம். |
| **PAN Number** | 10 எழுத்துகள், உதாரணமாக ABCDE1234F. Active-க்குக் கட்டாயம். |
| **Retail mFMS ID** | Active-க்குக் கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"Please correct the highlighted fields."** (குறிக்கப்பட்ட புலங்களைச் சரி செய்யுங்கள்) ஒவ்வொரு புலத்தின் கீழும் உள்ள சிவப்பு எழுத்தைப் படித்து, அதைச் சரி செய்யுங்கள்.
- **Submit பட்டன் சாம்பல் நிறத்தில் உள்ளது.** முதலில் ஒரு சப் டீலரைத் தேர்வு செய்து, அது ஏறும் வரை காத்திருங்கள். GST PDF, இடம் அல்லது பின்கோடு படிக்கப்படும்போதும் காத்திருங்கள்.
- **"Shortened URL detected."** (சுருக்கப்பட்ட லிங்க் கண்டுபிடிக்கப்பட்டது) லிங்கை பிரவுசரில் திறந்து, அட்ரஸ் பாரில் உள்ள முழு முகவரியைக் காப்பி செய்து ஒட்டுங்கள்.
- **"Choose a non-empty GST PDF of 5 MB or smaller."** (காலியில்லாத, 5 MB அல்லது அதற்குக் குறைவான GST PDF-ஐத் தேர்வு செய்யுங்கள்) 5 MB அல்லது அதற்குக் குறைவான PDF கோப்பை அப்லோட் செய்யுங்கள்.
- **"GST could not be extracted."** (GST விவரங்களைப் படிக்க முடியவில்லை) GST விவரங்களை நீங்களே டைப் செய்து, சரிபாருங்கள்.
- **"The selected Sub Dealer does not have a valid Sub Dealer Code. Contact Admin."** (தேர்வு செய்த சப் டீலருக்குச் சரியான குறியீடு இல்லை. Admin-ஐத் தொடர்பு கொள்ளுங்கள்) மாஸ்டர் பதிவு தவறாக உள்ளது. அதைச் சரி செய்யுமாறு Admin-இடம் கேளுங்கள்.
:::

::: section import-master
title: சப் டீலர் மாஸ்டரை இம்போர்ட் செய்வது
routes: /SubDealerList
for: 
roles: Admin, SuperAdmin, CorporateAdmin
designations: 
minutes: 3
source-hash: c6fda3f4afafbb6e
confidence: ok

**இந்தத் திரை எதற்கு**
ஒப்புதல் பெற்ற சப் டீலர்களின் Excel பட்டியலை Admin அப்லோட் செய்வார். இம்போர்ட் முதலில் ஒவ்வொரு வரிசையையும் சரிபார்த்துக் காட்டும். **Proceed Import** அழுத்தும் வரை எதுவும் சேமிக்கப்படாது.

1. **Sub Dealer Master**-ல், **Import Sub Dealers** அழுத்துங்கள்.
2. **Download Sample Format** அழுத்துங்கள். **Sub Dealer Code**, **Sub Dealer Name**, **HQ**, **Region**, **State** நெடுவரிசைகளை நிரப்புங்கள். ஆப்பில் உள்ள அதே State, Region, HQ பெயர்களைப் பயன்படுத்துங்கள்.
3. **Choose Excel file**-ன் கீழ், .xlsx கோப்பைத் தேர்வு செய்யுங்கள்.
4. **Check Import** அழுத்துங்கள். **Import summary**-ல் **New**, **Already same**, **Rejected** வரிசைகள் தெரியும். இன்னும் எதுவும் சேமிக்கப்படவில்லை.
5. நிராகரிக்கப்பட்ட ஒவ்வொரு வரிசைக்கும் **Reason**-ஐப் படியுங்கள். தேவைப்பட்டால் Excel-ஐத் திருத்தி மீண்டும் சரிபாருங்கள்.
6. புதிய வரிசைகளைச் சேமிக்க **Proceed Import** அழுத்துங்கள்.
7. இனி புதிய சப் டீலர்களைப் படிவத்தில் தேர்வு செய்யலாம். அவர்களுடைய விவரங்கள் சேமிக்கப்பட்ட பிறகுதான் அவர்கள் பட்டியலில் தெரிவார்கள்.

> Careful: ஏற்கனவே உள்ள சப் டீலரின் நிறுவனம், State, Region அல்லது HQ-வை இம்போர்ட் மூலம் மாற்ற முடியாது.
> Tip: தவறான அல்லது காலியான குறியீடுகளைச் சரி செய்ய SuperAdmin-க்கு **Correct Sub Dealer Codes**, **Repair Missing Codes** ஆகியவையும் தெரியும். இரண்டுமே எதையும் சேமிக்கும் முன் ஒரு முன்னோட்டம் காட்டும்.

## பொதுவான பிரச்சனைகள்

- **"Warning: Existing Sub Dealer master conflict detected!"** (ஏற்கனவே உள்ள சப் டீலர் மாஸ்டருடன் முரண்பாடு) ஏற்கனவே உள்ள ஒரு குறியீட்டுக்கு, கோப்பில் வேறு நிறுவனம், State, Region அல்லது HQ கொடுக்கப்பட்டுள்ளது. அதை மாற்ற Admin-ஐத் தொடர்பு கொள்ளுங்கள்.
- **சில வரிசைகளில் Sub Dealer Code காலியாக அல்லது கண்ணுக்குத் தெரியாமல் உள்ளது.** அந்த வரிசைகள் நிராகரிக்கப்பட்டன, சேமிக்கப்படவில்லை. காட்டப்படும் Excel வரிசை எண்களைச் சரிபாருங்கள்.
- **Check Import சாம்பல் நிறத்தில் உள்ளது.** முதலில் ஒரு .xlsx கோப்பைத் தேர்வு செய்யுங்கள். கோப்பு 10 MB வரை இருக்கலாம்.
:::

::: section beneficiary-list
title: சப் டீலர் மற்றும் ஊழியர் பயனாளிகளைப் பார்ப்பது
routes: /SubDealerEmployeeMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: dfcd6ed3ad5ab51a
confidence: ok

**இந்தத் திரை எதற்கு**
**SDWA** மெனுவில் உள்ள **Sub Dealer & Employee** திரை, சப் டீலர்கள் மற்றும் டீலர்களின் ஊழியர்களுக்கான நலத்திட்டப் பயனாளிகளை வைத்திருக்கும். ஒவ்வொரு பதிவுக்கும் முதலில் SM ஒப்புதலும், பிறகு AVP ஒப்புதலும் தேவை.

1. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **Sub Dealer** அல்லது **Employee**.
2. கார்டுகளைப் பாருங்கள்: மொத்தம், **Active**, **Inactive**.
3. தேடல் பெட்டியில் டைப் செய்யுங்கள். **All Status**, **Active** அல்லது **Inactive**, மற்றும் **All Approval**, **Pending SM**, **Pending AVP**, **Approved** அல்லது **Rejected** தேர்வு செய்யுங்கள்.
4. அட்டவணையில், ஒவ்வொரு பதிவுக்கும் **SM Status**, **AVP Status**, **Overall** ஆகியவற்றைப் படியுங்கள்.
5. உங்கள் பணிப்பொறுப்பால் பதிவுகளை மாற்ற முடியுமென்றால், **Status** நெடுவரிசையில் **Active** அல்லது **Inactive** தட்டி அதை மாற்றலாம்.

## பொதுவான பிரச்சனைகள்

- **"You do not have permission to view this data."** (இந்தத் தரவைப் பார்க்க உங்களுக்கு அனுமதி இல்லை) Sub Dealer & Employee-க்கு அனுமதி தருமாறு உங்கள் admin-இடம் கேளுங்கள்.
- **மேலே படிவம் இல்லை.** உங்கள் பதவியால் பதிவுகளைப் பார்க்க மட்டுமே முடியும்.
- **ஒரு பதிவு நீண்ட நேரமாக Pending என்று காட்டுகிறது.** அது SM-க்காகவும், பிறகு AVP-க்காகவும் காத்திருக்கிறது. அது எங்கே உள்ளது என்று பார்க்க **Pending SM** அல்லது **Pending AVP** ஃபில்டரைப் பயன்படுத்துங்கள்.
:::

::: section beneficiary-add
title: பயனாளியைச் சேர்ப்பது அல்லது மாற்றுவது
routes: /SubDealerEmployeeMaster
for: SubDealerEmployeeMaster.Entry
roles: 
designations: 
minutes: 3
source-hash: c972236359c62bca
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரு சப் டீலரின் அல்லது ஒரு டீலரின் ஊழியரின் நலத்திட்டப் பயனாளியை, ஒவ்வொருவராகவோ ஒரு Excel கோப்பிலிருந்தோ சேருங்கள். ஒவ்வொரு புதிய பதிவும் SM ஒப்புதலுக்காக **Pending** நிலையில் தொடங்கும்.

1. **Sub Dealer** அல்லது **Employee** டேபைத் தேர்வு செய்யுங்கள். மேலே உள்ள படிவத்தில் **Add New Sub Dealer** அல்லது **Add New Employee** என்று இருக்கும்.
2. சப் டீலர் என்றால், **Main Dealer Code**, **Main Dealer / Firm Name**, **Sub Dealer Code**, **Sub Dealer Name**, **Beneficiary Name** ஆகியவற்றை நிரப்புங்கள். ஊழியர் என்றால், **Dealer Code**, **Employee Name**, **Beneficiary Name** ஆகியவற்றை நிரப்புங்கள்.
3. உங்களிடம் உள்ள மற்ற விவரங்களையும் சேருங்கள், உதாரணமாக **Beneficiary ID**, **Nominee Name**, **Date of Birth**, **Relationship**.
4. ஸ்விட்சை **Active**-லேயே வைத்து, **Save** அழுத்துங்கள்.
5. பலரை ஒரே முறையில் சேர்க்க, **Download Template** அழுத்தி, அதை நிரப்பி, பிறகு **Upload File** அழுத்தி Excel கோப்பைத் தேர்வு செய்யுங்கள்.
6. ஒரு பதிவை மாற்ற, **Action** நெடுவரிசையில் உள்ள பென்சிலைத் தட்டி, திருத்தி, **Save** அழுத்துங்கள். குப்பைத்தொட்டி பதிவை நீக்கும்.

> Careful: நீக்கிய பதிவைத் திரும்பப் பெற முடியாது. ஒரு பதிவை மாற்றினாலும், அதன் SM, AVP ஒப்புதல் மாறாது.

## பொதுவான பிரச்சனைகள்

- **"Main Dealer Code is required."** (Main Dealer Code கட்டாயம்) * குறியிட்ட எல்லாப் புலங்களையும் நிரப்புங்கள். எந்தப் புலம் விடுபட்டுள்ளது என்று செய்தி சொல்லும்.
- **"already exists" என்று சேமிப்பு தோல்வியடைகிறது.** அதே Dealer Code, Sub Dealer Code, Beneficiary Name, DOB கொண்ட ஒரு பயனாளி ஏற்கனவே பட்டியலில் உள்ளார். அதைத் தேடிப் பாருங்கள்.
- **"Upload completed with ... skipped row(s)".** (அப்லோட் முடிந்தது, சில வரிசைகள் விடப்பட்டன) செய்திக்குக் கீழே காட்டப்படும் விடப்பட்ட வரிசைகளைப் படியுங்கள். அவற்றைக் காப்பி செய்ய **Copy** அழுத்தி, கோப்பைத் திருத்தி, அந்த வரிசைகளை மீண்டும் அப்லோட் செய்யுங்கள்.
:::

::: section beneficiary-approve
title: பயனாளிக்கு ஒப்புதல் தருவது அல்லது நிராகரிப்பது
routes: /SubDealerEmployeeMaster
for: 
roles: SMD, SMM, AVP
designations: 
minutes: 2
source-hash: c429d512dd70afcb
confidence: ok

**இந்தத் திரை எதற்கு**
ஒவ்வொரு பயனாளிக்கும் முதலில் SMD அல்லது SMM ஒப்புதல் தருவார். AVP இறுதி ஒப்புதல் தருவார். ஒப்புதல் பெற்ற, Active ஆக உள்ள பயனாளிகளை மட்டுமே நலத்திட்ட விண்ணப்பத்தில் டீலர் தேர்வு செய்ய முடியும்.

1. **Sub Dealer** அல்லது **Employee** டேபைத் தேர்வு செய்யுங்கள்.
2. ஒப்புதல் ஃபில்டரை **Pending SM** (SMD, SMM) அல்லது **Pending AVP** (AVP) என்று வையுங்கள்.
3. பதிவின் விவரங்களைச் சரிபாருங்கள்: டீலர் குறியீடு, பெயர்கள், பிறந்த தேதி, உறவுமுறை.
4. **Action** நெடுவரிசையில், ஒப்புதல் தர பச்சை டிக்கையும், நிராகரிக்க சிவப்புப் பெருக்கல் குறியையும் அழுத்துங்கள். போனில் **Approve** அல்லது **Reject** அழுத்துங்கள்.
5. **Remarks (optional)** டைப் செய்து, **Approve** அல்லது **Reject** அழுத்துங்கள்.
6. **SM Status** அல்லது **AVP Status** நெடுவரிசை மாறும். SM ஒப்புதல் தந்த பிறகுதான் AVP-க்கு அந்தப் பதிவு தெரியும்.

> Careful: நிராகரிக்கப்பட்ட பதிவு அங்கேயே நின்றுவிடும். நலத்திட்டங்களுக்கு அது டீலருக்குக் காட்டப்படாது.

## பொதுவான பிரச்சனைகள்

- **வரிசையில் டிக் அல்லது பெருக்கல் குறி இல்லை.** **SM Status** **Pending** ஆக இருக்கும்போது மட்டுமே SM-க்கு அவை தெரியும். SM ஒப்புதல் தந்த பிறகு, **AVP Status** **Pending** ஆக இருக்கும்போது மட்டுமே AVP-க்கு அவை தெரியும்.
- **Overall நெடுவரிசையில் SM Rejected என்று உள்ளது.** SM பதிவை மறுத்துவிட்டார். அதனால் அது AVP-க்குப் போகாது.
:::
