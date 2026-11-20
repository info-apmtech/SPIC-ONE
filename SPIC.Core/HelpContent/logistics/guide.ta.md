---
module: logistics
title: லாஜிஸ்டிக்ஸ்
icon: bi-truck
order: 113
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
source-hash: 616b9e8fbedcbace
confidence: ok

SPIC மற்றும் GREEN STAR பயன்படுத்தும் குடோன்கள், ரேக் பாயிண்ட்கள், துறைமுகங்கள் எல்லாவற்றையும் ஒரே பட்டியலில் வைத்திருங்கள். களப் பணியாளர்கள் ஒவ்வொரு இடத்தையும் அதன் இருப்பிடம் மற்றும் ஆவணங்களுடன் சேர்ப்பார்கள். பிறகு RM, SMM, AVP வரிசையாக ஒப்புதல் தருவார்கள்.

- add | icon: bi-building-add | color: #475569
  title: களப் பணியாளர்கள் சேர்ப்பார்கள்
  text: MO, MDO அல்லது JMDO ஒரு குடோன் அல்லது ரேக் பாயிண்ட்டை அதன் SAP Code, முகவரி, மேப் இடம், ஆவணங்களுடன் சேர்ப்பார்கள்.
- approve | icon: bi-shield-check | color: #d97706
  title: மூன்று ஒப்புதல்கள்
  text: முதலில் RM, பிறகு SMM, பிறகு AVP பதிவைச் சரிபார்ப்பார்கள். ஒவ்வொருவரும் ஒப்புதல் தரலாம், அல்லது குறிப்புடன் திருப்பி அனுப்பலாம்.
- report | icon: bi-bar-chart-line | color: #2563eb
  title: மொத்த எண்ணிக்கையைப் பாருங்கள்
  text: ஒவ்வொரு மாநிலத்திலும் எத்தனை பதிவுகள் நிலுவையில் உள்ளன, எத்தனை முடிந்துள்ளன என்று அறிக்கை காட்டும்.
:::

::: section flow
title: ஒரு பதிவு எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: caf925a727761bcf
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- admin | type: lane | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  sub: SAP Code பட்டியல்
- maker | type: lane | color: #475569 | roles: MO, MDO, JMDO
  title: MO / MDO / JMDO
  sub: களப் பணியாளர்கள்
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: உங்கள் மண்டலம்
- smm | type: lane | color: #c026d3 | roles: SMM, SMD
  title: SMM
  sub: உங்கள் மாநிலம்
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin
  title: AVP
  sub: இறுதி ஒப்புதல்
- sap | lane: admin | col: 1 | icon: bi-upc-scan | color: #64748b | next: add | screen: sap-master
  title: SAP Code-களை ஏற்றுதல்
  text: Admin, PVT Master மற்றும் Rake Point Master Excel கோப்புகளை அப்லோட் செய்து, ஒவ்வொரு SAP Code-ஐயும் ஒரு மாநிலத்துடன் இணைப்பார்.
- add | lane: maker | col: 2 | icon: bi-building-add | color: #475569 | next: rm-check | screen: add-record
  title: பதிவைச் சேர்த்தல்
  text: SAP Code-ஐத் தேர்வு செய்து, விவரங்கள், இருப்பிடம், ஆவணங்களை நிரப்பி **Save** அழுத்துங்கள். பதிவு RM-க்குப் போகும்.
- rm-check | lane: rm | col: 3 | icon: bi-hourglass-split | color: #d97706 | next: smm-check | return: add | screen: approve-records
  title: RM சரிபார்ப்பார்
  status: Pending RM
  text: RM **Approve to SMM** அழுத்துவார், அல்லது MO, MDO, JMDO-க்குத் திருப்பி அனுப்ப **Send Back** அழுத்துவார்.
- smm-check | lane: smm | col: 4 | icon: bi-hourglass-split | color: #d97706 | next: final | return: rm-check | screen: approve-records
  title: SMM சரிபார்ப்பார்
  status: Pending SMM
  text: SMM **Approve to AVP** அழுத்துவார், அல்லது RM-க்குத் திருப்பி அனுப்ப **Send Back** அழுத்துவார்.
- final | lane: avp | col: 5 | icon: bi-patch-check | color: #16a34a | return: smm-check | screen: approve-records
  title: இறுதி ஒப்புதல்
  status: Approved
  text: AVP **Final Approve** அழுத்துவார், அல்லது SMM-க்குத் திருப்பி அனுப்ப **Send Back** அழுத்துவார். ஒப்புதல் கிடைத்ததும் பதிவு முடிந்துவிடும்.
- sentback | lane: maker | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: add | screen: manage-records
  title: திருத்தி மீண்டும் சேமித்தல்
  status: Sent Back to MO
  text: **Remarks**-ஐப் படித்து, பென்சிலை அழுத்தி, பதிவைத் திருத்தி **Update** அழுத்துங்கள். அது மீண்டும் RM-க்குப் போகும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c35bc2efe348d9c7
confidence: ok

ஒரு குடோன் அல்லது ரேக் பாயிண்ட் எந்த நிலையில் உள்ளது என்று **Approval Status** நெடுவரிசை காட்டும். **Status** நெடுவரிசை **Active** அல்லது **Inactive** என்று காட்டும்.

- draft | color: #64748b
  label: Draft
  text: MO, MDO அல்லது JMDO அல்லாத ஒருவர் சேமித்தது. இது ஒப்புதல் வரிசையில் இல்லை.
- pending-rm | color: #d97706
  label: Pending RM
  text: மண்டலத்தின் RM-க்காகக் காத்திருக்கிறது.
- pending-smm | color: #d97706
  label: Pending SMM
  text: RM ஒப்புதல் தந்துவிட்டார். மாநிலத்தின் SMM-க்காகக் காத்திருக்கிறது.
- pending-avp | color: #d97706
  label: Pending AVP
  text: SMM ஒப்புதல் தந்துவிட்டார். இறுதி ஒப்புதலுக்காகக் காத்திருக்கிறது.
- approved | color: #16a34a
  label: Approved
  text: மூன்று ஒப்புதல்களும் முடிந்துவிட்டன.
- back-mo | color: #dc2626
  label: Sent Back to MO
  text: RM திருப்பி அனுப்பிவிட்டார். பதிவைச் செய்த MO, MDO அல்லது JMDO அதைத் திருத்த வேண்டும்.
- back-rm | color: #dc2626
  label: Sent Back to RM
  text: SMM அதை RM-க்குத் திருப்பி அனுப்பிவிட்டார்.
- back-smm | color: #dc2626
  label: Sent Back to SMM
  text: AVP அதை SMM-க்குத் திருப்பி அனுப்பிவிட்டார்.
- active | color: #0d9488
  label: Active
  text: இந்த இடம் பயன்பாட்டில் உள்ளது. இருப்பிட விவரங்களும் பட்டியலில் உள்ள ஆவணங்களும் கட்டாயம்.
- inactive | color: #475569
  label: Inactive
  text: இந்த இடம் இப்போது பயன்பாட்டில் இல்லை. குறைவான விவரங்கள் போதும்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 8acdb7d7714a0307
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- maker | color: #475569 | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - குடோன்களையும் ரேக் பாயிண்ட்களையும் சேர்ப்பது
  - GST, இன்ஷூரன்ஸ், லைசென்ஸ் ஆவணங்களை அப்லோட் செய்வது
  - திருப்பி அனுப்பப்பட்ட பதிவுகளைத் திருத்துவது
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - உங்கள் மண்டலத்தின் பதிவுகளைச் சரிபார்ப்பது
  - SMM-க்கு ஒப்புதல் தருவது, அல்லது பதிவு செய்தவருக்குத் திருப்பி அனுப்புவது
- smm | color: #c026d3 | roles: SMM, SMD
  title: SMM
  - உங்கள் மாநிலத்தின் பதிவுகளைச் சரிபார்ப்பது
  - AVP-க்கு ஒப்புதல் தருவது, அல்லது RM-க்குத் திருப்பி அனுப்புவது
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP, Director
  - இறுதி ஒப்புதல் தருவது, அல்லது SMM-க்குத் திருப்பி அனுப்புவது
  - துறைமுகங்களைச் சேர்ப்பது, மாற்றுவது
- admin | color: #64748b | roles: Admin, CorporateAdmin
  title: Admin
  - SAP Code பட்டியல்களை அப்லோட் செய்து மாநிலங்களுடன் இணைப்பது
  - இறுதி ஒப்புதல் தருவது
  - லாஜிஸ்டிக்ஸ் அறிக்கையைப் பார்ப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7662caacafa8b468
confidence: ok

- no-sap-code | popular: true
  q: என் SAP Code பட்டியலில் இல்லை
  a: நீங்கள் தேர்வு செய்த **State**-உடன் இணைக்கப்பட்ட code-கள் மட்டுமே பட்டியலில் தெரியும். அந்த code-ஐ அப்லோட் செய்யவோ, **PVT Master Management** திரையில் உங்கள் மாநிலத்துடன் இணைக்கவோ Admin-இடம் கேளுங்கள்.
- sent-back | popular: true
  q: என் குடோனில் "Sent Back to MO" என்று காட்டுகிறது. நான் என்ன செய்வது?
  a: **Remarks** நெடுவரிசையைப் படியுங்கள். அந்த வரிசையில் பென்சிலை அழுத்தி, பதிவைத் திருத்தி **Update** அழுத்துங்கள். அது **Pending RM** ஆக மீண்டும் RM-க்குப் போகும்.
- no-approve
  q: நான் RM, ஆனால் எனக்கு Approve பட்டன் தெரியவில்லை
  a: உங்கள் நிலையில் காத்திருக்கும் பதிவுகளுக்கு மட்டுமே பட்டன் தெரியும். ஒப்புதல் ஃபில்டரில் உங்கள் நிலையை (உதாரணமாக **Pending RM**) தேர்வு செய்யுங்கள். RM-களுக்குத் தங்கள் மண்டலத்தின் பதிவுகள் மட்டுமே தெரியும்.
- save-error
  q: Save அழுத்தினால் புலங்களுக்குக் கீழே சிவப்பு எழுத்தில் செய்திகள் வருகின்றன
  a: **Active** பதிவுக்கு முழு இருப்பிடம், தொடர்பு எண், தேவையான ஆவணங்கள் கட்டாயம். சிவப்பில் குறிக்கப்பட்ட ஒவ்வொரு புலத்தையும் நிரப்புங்கள். இடம் பயன்பாட்டில் இல்லையென்றால் **Status**-ஐ **Inactive** என்று வையுங்கள்.
- duplicate
  q: SAP Code ஏற்கனவே உள்ளது என்று சொல்கிறது
  a: வேறொரு குடோன் அல்லது ரேக் பாயிண்ட் ஏற்கனவே அந்த code-ஐப் பயன்படுத்துகிறது. பட்டியலில் அதைத் தேடிப் பாருங்கள். ஒரே இடத்தை இரண்டு முறை சேர்க்காதீர்கள்.
:::

::: section add-record
title: குடோன் அல்லது ரேக் பாயிண்ட்டைச் சேர்ப்பது
routes: /Logistics
for: 
roles: 
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-basic] | 3 > [data-help=lg-location] | 4 > [data-help=lg-docs] | 5 > [data-help=lg-save]
minutes: 5
source-hash: c9ec25daaeef4964
confidence: ok

**இந்தத் திரை எதற்கு**
**Logistics Master** திரையில் மேலே ஒரு படிவமும், கீழே சேமித்த பதிவுகளின் பட்டியலும் இருக்கும். குடோன் அல்லது ரேக் பாயிண்ட்டைச் சேர்க்க இந்தப் படிவத்தைப் பயன்படுத்துங்கள்.

1. டேபைத் தேர்வு செய்யுங்கள்: **Warehouse** அல்லது **Rake Point**.
2. **Basic Information**-ல், **SPIC**, **GREEN STAR** அல்லது இரண்டையும் தட்டுங்கள். **State**-ஐத் தேர்வு செய்து, பிறகு **SAP Code**-ஐத் தேர்வு செய்யுங்கள்; **Name** தானாக நிரம்பும். **Operated By**-ஐத் தேர்வு செய்யுங்கள். குடோன் என்றால், **Warehouse Category** (**PVT WH**, **C&F WH** அல்லது **Both**), **WH Type** ஆகியவற்றைத் தேர்வு செய்து, **Reservation Quantity** பெட்டியை நிரப்புங்கள்.
3. **Primary Location**-ல், **Use Current Location** அழுத்துங்கள், அல்லது மேப்பில் தட்டுங்கள், அல்லது **Google Map URL**-ஐ ஒட்டுங்கள் (paste). பிறகு முகவரி, **Pincode**, தொடர்பு எண் ஆகியவற்றை நிரப்புங்கள்.
4. **Documents**-ல், ஒவ்வொரு **Choose ... File** பட்டனையும் அழுத்திக் கோப்பைத் தேர்வு செய்யுங்கள். கண் ஐகானை வைத்து அதைச் சரிபாருங்கள். **Active** பதிவுக்கு * குறியிட்ட புலங்கள் கட்டாயம்.
5. **Save** அழுத்துங்கள். MO, MDO அல்லது JMDO சேமித்தால், பதிவு **Pending RM** ஆக RM-க்குப் போகும்.

> Tip: ஆவணத்தை ஸ்கேன் செய்து அப்லோட் செய்யும் முன், அதில் உள்ள குடோன் முகவரியை ஹைலைட்டரால் குறித்துக்கொள்ளுங்கள்.
> Careful: படிவத்தை அழிக்க வேண்டுமென்றால் மட்டுமே **Cancel** அழுத்துங்கள். எதுவும் சேமிக்கப்படாது.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Status** | இடம் இப்போது பயன்பாட்டில் இருந்தால் **Active**, இல்லையென்றால் **Inactive**. |
| **Operated By** | **Dealer** அல்லது **Others**. |
| **WH Type** | **Field WH** அல்லது **Rake Point WH** (PVT WH மற்றும் Both-க்கு மட்டும்). |
| **Approved Reservation Quantity (MT)** | நிறுவனத்துக்காக ஒதுக்கப்பட்ட இடம், டன் கணக்கில். |
| **Latitude & Longitude** | கமாவுடன் இரண்டு எண்கள், உதாரணமாக 10.7905, 78.7047. |
| **Pincode** | 6 இலக்கங்கள். **State** மற்றும் **District** தானாக நிரம்பலாம். |

## பொதுவான பிரச்சனைகள்

- **SAP Code பட்டியல் காலியாக உள்ளது.** தேர்வு செய்த **State**-உடன் இதுவரை எந்த code-உம் இணைக்கப்படவில்லை. Admin-இடம் கேளுங்கள்.
- **"Primary Location is required".** (Primary Location கட்டாயம்) **Active** பதிவுக்கு மேப் இடம் அல்லது Google Map URL, முழு முகவரி தேவை.
- **"... saved, but one or more documents failed to upload".** (சேமிக்கப்பட்டது, ஆனால் சில ஆவணங்கள் அப்லோட் ஆகவில்லை) பதிவு சேமிக்கப்பட்டுவிட்டது. அதில் பென்சிலை அழுத்தி ஆவணங்களை மீண்டும் அப்லோட் செய்யுங்கள்.
- **State, Region அல்லது HQ சாம்பல் நிறத்தில் உள்ளது.** இவை உங்கள் லாகினில் இருந்து வருகின்றன, மாற்ற முடியாது.
:::

::: section approve-records
title: பதிவுக்கு ஒப்புதல் தருவது அல்லது திருப்பி அனுப்புவது
routes: /Logistics
for: 
roles: RM, RMD, SMM, SMD, AVP, Director, Admin, CorporateAdmin
designations: 
tour: 1 > [data-help=lg-tabs] | 2 > [data-help=lg-approval-filter] | 3 > [data-help=lg-search] | 4 > [data-help=lg-list]
minutes: 2
source-hash: 5b300e3e278e2af7
confidence: ok

**இந்தத் திரை எதற்கு**
படிவத்துக்குக் கீழே உள்ள பட்டியலில் உங்கள் பகுதியின் குடோன்களும் ரேக் பாயிண்ட்களும் தெரியும். உங்களுக்காகக் காத்திருக்கும் பதிவுகளில், **Action** நெடுவரிசையில் ஒப்புதல் பட்டனும் **Send Back** பட்டனும் இருக்கும்.

1. **Warehouse** அல்லது **Rake Point** டேபைத் தேர்வு செய்யுங்கள்.
2. ஒப்புதல் ஃபில்டரில் உங்கள் நிலையைத் தேர்வு செய்யுங்கள்: **Pending RM**, **Pending SMM** அல்லது **Pending AVP**.
3. பதிவைக் கண்டுபிடிக்க, தேடல் பெட்டியில் பெயர் அல்லது SAP Code-ஐ டைப் செய்யுங்கள். அதைச் சரிபார்க்க **Docs** நெடுவரிசையில் உள்ள **View**-ஐயும் PDF பட்டனையும் பயன்படுத்துங்கள்.
4. **Approve to SMM**, **Approve to AVP** அல்லது **Final Approve** அழுத்துங்கள் (உங்கள் நிலைக்கான பட்டன் தெரியும்). **Approval Remarks** எழுதி **Confirm & Proceed** அழுத்துங்கள். திருப்பி அனுப்ப, **Send Back** அழுத்தி, **Send Back Remarks** எழுதி, **Confirm Send Back** அழுத்துங்கள்.

> Tip: ஒப்புதல் தருவதற்கும் திருப்பி அனுப்புவதற்கும் குறிப்பு கட்டாயம். அது **Remarks** நெடுவரிசையில் தெரியும்.
> Careful: **Send Back** பதிவை நீக்காது. அது ஒரு நிலை மட்டுமே பின்னுக்கு நகர்த்தும்.

## பொதுவான பிரச்சனைகள்

- **ஒப்புதல் பட்டன் இல்லை.** பதிவு உங்கள் நிலையில் இல்லை. அதன் **Approval Status**-ஐப் பாருங்கள்.
- **"Approval remarks are required."** (ஒப்புதல் குறிப்பு கட்டாயம்) உறுதி செய்யும் முன் ஒரு குறிப்பை டைப் செய்யுங்கள்.
- **எனக்கு ஒரு பதிவு தெரியவில்லை.** RM-களுக்குத் தங்கள் மண்டலம், SMM-களுக்குத் தங்கள் மாநிலம், AVP-களுக்குத் தங்கள் zone மட்டுமே தெரியும்.
:::

::: section manage-records
title: பதிவுகளைக் கண்டுபிடிப்பது, திருத்துவது, பதிவிறக்குவது
routes: /Logistics
for: 
roles: 
designations: 
minutes: 3
source-hash: 13e0dd985a45cb04
confidence: ok

**இந்தத் திரை எதற்கு**
படிவத்துக்குக் கீழே உள்ள பட்டியலில் சேமித்த பதிவுகள் தெரியும். மேலே எண்ணிக்கைகள் இருக்கும்: **Total**, **Active**, **Inactive**.

1. டேபைத் தேர்வு செய்து, பட்டியலின் மேலே உள்ள எண்ணிக்கைகளைப் பாருங்கள்.
2. பெயரை வைத்துத் தேடுங்கள், அல்லது பட்டியலைக் குறைக்க **All Status** மற்றும் ஒப்புதல் ஃபில்டர்களைப் பயன்படுத்துங்கள்.
3. சேமித்த ஆவணங்களைப் பார்க்க, பதிவிறக்க அல்லது திறக்க **Docs** நெடுவரிசையில் **View** அழுத்துங்கள். முடிந்ததும் **Close** அழுத்துங்கள்.
4. பதிவை PDF ஆகத் திறக்க, **PDF** நெடுவரிசையில் உள்ள SPIC அல்லது GREEN STAR லோகோவை அழுத்துங்கள்.
5. பதிவைப் படிவத்தில் ஏற்ற பென்சிலை அழுத்துங்கள். மாற்றம் செய்து **Update** அழுத்துங்கள்.
6. **Status** நெடுவரிசையில் **Active** அல்லது **Inactive**-ஐத் தட்டி அதை மாற்றுங்கள்.
7. பதிவை நீக்க குப்பைத்தொட்டியை அழுத்தி, உறுதி செய்ய **Delete** அழுத்துங்கள்.
8. பட்டியலைப் பதிவிறக்க, பட்டியலுக்கு மேலே உள்ள **Excel** அழுத்துங்கள் (போனில், **Export** பிறகு **Excel**).

> Careful: நீக்கிய பதிவைத் திரும்பப் பெற முடியாது.

## பொதுவான பிரச்சனைகள்

- **"Sent Back to MO".** (MO-க்குத் திருப்பி அனுப்பப்பட்டது) **Remarks**-ஐப் படித்து, பென்சில் மூலம் பதிவைத் திருத்தி **Update** அழுத்துங்கள். அது மீண்டும் RM-க்குப் போகும்.
- **Docs-ல் உள்ள View பட்டன் சாம்பல் நிறத்தில் உள்ளது.** அந்தப் பதிவுக்கு எந்த ஆவணமும் சேமிக்கப்படவில்லை.
- **பட்டியல் காலியாக உள்ளது.** உங்கள் ஃபில்டர்கள் ரொம்பக் குறுகலாக இருக்கலாம். அவற்றை மீண்டும் **All Status** மற்றும் **All Approval Status** என்று வையுங்கள்.
:::

::: section ports
title: துறைமுகத்தைச் சேர்ப்பது
routes: /Logistics
for: 
roles: Admin, CorporateAdmin, AVP, Director
designations: 
minutes: 1
source-hash: 264444955d15cae7
confidence: ok

**இந்தத் திரை எதற்கு**
**Port** டேபில் துறைமுகங்களின் பட்டியல் இருக்கும். Admin, AVP, Director-க்கு மட்டுமே இந்த டேப் தெரியும். துறைமுகங்களுக்கு ஒப்புதல் படிகள் இல்லை.

1. **Port** டேபைத் திறங்கள்.
2. **Port Name**-ஐ உள்ளிடுங்கள். உங்களிடம் இருந்தால் **Google Map or Lat/Long URL**-ஐயும் உள்ளிடுங்கள்.
3. **State** மற்றும் **District**-ஐத் தேர்வு செய்யுங்கள்.
4. **Active** அல்லது **Inactive** தேர்வு செய்து **Save** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Port name is required".** (துறைமுகப் பெயர் கட்டாயம்) சேமிக்கும் முன் பெயர், **State**, **District** ஆகியவற்றை உள்ளிடுங்கள்.
:::

::: section reports
title: லாஜிஸ்டிக்ஸ் அறிக்கையைப் பார்ப்பது
routes: /LogisticsReport
for: 
roles: 
designations: 
minutes: 1
source-hash: 206298ea363d8aeb
confidence: ok

**இந்தத் திரை எதற்கு**
**Logistics Reports** திரை, குடோன்களையும் ரேக் பாயிண்ட்களையும் மாநில வாரியாக எண்ணிக் காட்டும். கடைசியில் **TOTAL** வரிசை இருக்கும்.

1. **Logistics Master** திரையில் **Reports** அழுத்துங்கள்.
2. **Warehouse Report** அல்லது **Rakepoint Report** தேர்வு செய்யுங்கள்.
3. ஒரு மாநிலத்தை மட்டும் பார்க்க, **Search by state...**-ல் மாநிலத்தின் பெயரை டைப் செய்யுங்கள்.
4. நெடுவரிசைகளைப் படியுங்கள்: **Active**, **Inactive**, **Total**, **As per SAP**, **Pending with MO**, **Pending RM**, **Pending SMM**, **Pending with AVP** மற்றும் **Completed**.
5. சமீபத்திய எண்களைப் பெற **Refresh** அழுத்துங்கள். திரும்பிப் போக **Back to Logistics** அழுத்துங்கள்.

> Tip: **As per SAP** என்பது SAP Code பட்டியலில் உள்ள code-களின் எண்ணிக்கை. **Pending with MO** என்பது இன்னும் சேர்க்கப்படாத அல்லது இன்னும் ஒப்புதலுக்கு வராத code-கள்.

## பொதுவான பிரச்சனைகள்

- **"No data available".** (தகவல் இல்லை) உங்கள் தேடலுக்குப் பொருந்தும் எதுவும் இல்லை. தேடல் பெட்டியை அழியுங்கள், அல்லது **Refresh** அழுத்துங்கள்.
:::

::: section sap-master
title: SAP Code-களை அப்லோட் செய்து மாநிலங்களுடன் இணைப்பது
routes: /PVTMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 2bc3660a46728597
confidence: ok

**இந்தத் திரை எதற்கு**
**PVT Master Management** திரையில் SAP Code பட்டியல்கள் இருக்கும். லாஜிஸ்டிக்ஸ் படிவத்தில் உள்ள **SAP Code** பெட்டிக்கு இவைதான் வருகின்றன. **Logistics Master** திரையில் உள்ள **SAP Code** பட்டன் மூலம் Admin இதைத் திறப்பார்.

1. **PVT Master** (குடோன்கள்) அல்லது **Rake Point** தேர்வு செய்யுங்கள்.
2. Excel வடிவத்தைப் பதிவிறக்க **Sample Format** அழுத்தி, அதை நிரப்புங்கள்.
3. **Click to upload or drag and drop** பகுதியை அழுத்தி, உங்கள் .xlsx கோப்பைத் தேர்வு செய்யுங்கள்.
4. **Upload Result**-ஐப் படியுங்கள்: **Total Records**, **Successfully Inserted**, **Duplicate/Skipped**, கூடவே விடுபட்ட வரிசைகளின் பட்டியல்.
5. பட்டியலில், ஒரு வரிசையில் **State** அழுத்தி, **State**-ஐத் தேர்வு செய்து **Save State Mapping** அழுத்துங்கள்.
6. லாஜிஸ்டிக்ஸுக்குத் திரும்ப **Back** அழுத்துங்கள்.

> Careful: 10 MB அல்லது அதற்குக் குறைவான .xlsx கோப்பைப் பயன்படுத்துங்கள். ஏற்கனவே உள்ள code-கள் விடப்படும்.

## பொதுவான பிரச்சனைகள்

- **"Please select a valid .xlsx Excel file."** (சரியான .xlsx Excel கோப்பைத் தேர்வு செய்யுங்கள்) கோப்பை .xlsx ஆகச் சேமித்து மீண்டும் முயற்சி செய்யுங்கள்.
- **ஒரு code-ல் "Not Mapped" என்று காட்டுகிறது.** அந்த வரிசையில் **State** அழுத்தி அதன் மாநிலத்தைத் தேர்வு செய்யுங்கள். அதுவரை, களப் பணியாளர்கள் மாநிலத்தைத் தேர்வு செய்த பிறகு அந்த code-ஐத் தேர்வு செய்ய முடியாது.
- **State பட்டன் இல்லை.** Admin மற்றும் CorporateAdmin மட்டுமே இணைப்பை மாற்ற முடியும்.
:::

::: section old-master
title: பழைய Logistics Master திரை
routes: /LogisticsMaster
for: 
roles: 
designations: 
minutes: 1
source-hash: 2bc6b07f78791b25
confidence: ok

**இந்தத் திரை எதற்கு**
இது லாஜிஸ்டிக்ஸ் படிவத்தின் பழைய பதிப்பு. இதில் **PVT WH**, **C&F WH**, **Rake Point** என்ற டேப்கள் இருக்கும். புதிய வேலைக்கு, பக்க மெனுவில் உள்ள முதன்மை **Logistics Master** திரையைப் பயன்படுத்துங்கள்.

1. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **PVT WH**, **C&F WH** அல்லது **Rake Point**.
2. முதன்மைத் திரையில் உள்ளது போலவே **Basic Information**, **Reservation Quantity**, **Primary Location**, **Documents** ஆகியவற்றை நிரப்புங்கள்.
3. **Save** அழுத்துங்கள். பதிவை மாற்ற அல்லது நீக்க, பட்டியலில் உள்ள பென்சில் மற்றும் குப்பைத்தொட்டியைப் பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Failed to load PVT WH" or "Failed to load C&F WH".** (PVT WH / C&F WH ஏற்ற முடியவில்லை) இந்தத் திரையில் இந்த டேப்கள் வேலை செய்யாது. முதன்மை **Logistics Master** திரையைப் பயன்படுத்துங்கள்.
- **ஒப்புதல் பட்டன்கள் இல்லை.** ஒப்புதல்கள் முதன்மை **Logistics Master** திரையில் மட்டுமே நடக்கும்.
:::
