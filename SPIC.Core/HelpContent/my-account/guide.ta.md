---
module: my-account
title: என் கணக்கு மற்றும் டாஷ்போர்டு
icon: bi-person-circle
order: 118
language: ta
category: பொது
note: புதிய வழிகாட்டி
meta-source-hash: a6f45f8833df3868
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 4b8137c0f3f7283d
confidence: ok

இவை உங்களைப் பற்றிய திரைகள்: டீலர் பதிவு டாஷ்போர்டு, உங்கள் சொந்தப் புரொஃபைல், உங்களுக்காகக் காத்திருக்கும் வேலைகளின் பட்டியல், SPIC-ஐத் தொடர்பு கொள்ளும் வழி.

- dashboard | icon: bi-speedometer2 | color: #0b6b3a
  title: டீலர் பதிவுகள்
  text: உங்கள் பகுதியின் எல்லா டீலர் விண்ணப்பங்களையும் அவற்றின் நிலையுடன் பாருங்கள். உங்களுக்காகக் காத்திருப்பவற்றில் செயல்படுங்கள்.
- profile | icon: bi-person-badge | color: #2563eb
  title: உங்கள் புரொஃபைல்
  text: உங்கள் பெயர், பதவி, இடம், நிலை ஆகியவற்றைச் சரிபாருங்கள். டீலர்களுக்குத் தங்கள் பதிவு விவரங்களும் தெரியும்.
- help | icon: bi-bell | color: #d97706
  title: அலர்ட்களும் உதவியும்
  text: உங்களுக்காக என்ன காத்திருக்கிறது என்று பாருங்கள், உங்கள் தினசரி வேலையைக் கண்டுபிடியுங்கள், SPIC-க்குச் செய்தி அனுப்புங்கள்.
:::

::: section flow
title: டாஷ்போர்டில் ஒரு டீலர் பதிவு எப்படி நகர்கிறது
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 6696a0bfbcbd48ab
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு டீலரின் டாஷ்போர்டு கார்டில், அது எந்தப் படியை அடைந்துள்ளது என்று தெரியும்.

- creator | type: lane | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO · MDO · JMDO
  sub: உங்கள் HQ
- rm-lane | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: உங்கள் மண்டலம்
- smm-lane | type: lane | color: #7c3aed | roles: SMD, SMM
  title: SMM
  sub: உங்கள் மாநிலம்
- avp-lane | type: lane | color: #2563eb | roles: AVP, Admin
  title: AVP
  sub: இறுதி ஒப்புதல்
- draft | lane: creator | col: 1 | icon: bi-pencil-square | color: #a16207 | next: rm | screen: dashboard
  title: நிரப்பிச் சமர்ப்பித்தல்
  status: Draft
  text: விண்ணப்பம் சமர்ப்பிக்கப்படும் வரை கார்டில் **Draft** என்று காட்டும். தொடர்ந்து நிரப்ப **Edit** அழுத்துங்கள்.
- rm | lane: rm-lane | col: 2 | icon: bi-hourglass-split | color: #1d4ed8 | next: smm | return: draft | screen: dashboard
  title: RM சரிபார்ப்பார்
  status: In RM
  text: RM **Review** அழுத்துவார். RM திருப்பி அனுப்பினால், கார்டில் **Rejected by RM** என்று காட்டும். MO அதை மீண்டும் திருத்தலாம்.
- smm | lane: smm-lane | col: 3 | icon: bi-hourglass-split | color: #7c3aed | next: avp | return: rm | screen: dashboard
  title: SMM சரிபார்ப்பார்
  status: In SMM
  text: SMM **Review** அழுத்துவார். SMM திருப்பி அனுப்பினால், **Rejected by SMM** என்று காட்டி, அது RM-இடம் திரும்பப் போகும்.
- avp | lane: avp-lane | col: 4 | icon: bi-patch-check | color: #4338ca | next: approved | return: smm | screen: dashboard
  title: AVP ஒப்புதல் தருவார்
  status: In AVP
  text: AVP இறுதி ஒப்புதல் தருவார். AVP திருப்பி அனுப்பினால், **Rejected by AVP** என்று காட்டி, அது SMM-இடம் திரும்பப் போகும்.
- approved | lane: avp-lane | col: 5 | icon: bi-check-circle | color: #16a34a | screen: dashboard
  title: ஒப்புதல் கிடைத்தது
  status: Approved
  text: கார்டில் **Approved** என்று காட்டும். புதிய டீலர் என்றால், AVP Office நிறுவனத்தின் Dealer ID-களைச் சேமிக்கும் வரை முதலில் **In AVP Office** என்று காட்டும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 196e7278f9089805
confidence: ok

- draft | color: #a16207
  label: Draft
  text: இன்னும் சமர்ப்பிக்கப்படவில்லை. MO, MDO அல்லது JMDO இன்னும் அதைத் திருத்தலாம்.
- in-rm | color: #1d4ed8
  label: In RM
  text: RM சரிபார்க்கக் காத்திருக்கிறது.
- in-smm | color: #7c3aed
  label: In SMM
  text: SMM சரிபார்க்கக் காத்திருக்கிறது.
- in-avp | color: #4338ca
  label: In AVP
  text: AVP-யின் இறுதி ஒப்புதலுக்காகக் காத்திருக்கிறது.
- in-avp-office | color: #0e7490
  label: In AVP Office
  text: புதிய டீலருக்கு ஒப்புதல் கிடைத்துவிட்டது. AVP Office இன்னும் நிறுவனத்தின் Dealer ID-களைச் சேமிக்க வேண்டும்.
- approved | color: #047857
  label: Approved
  text: விண்ணப்பத்துக்கு முழு ஒப்புதல் கிடைத்துவிட்டது.
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: திருத்துவதற்காக MO, MDO அல்லது JMDO-க்குத் திருப்பி அனுப்பப்பட்டது.
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: RM-க்குத் திருப்பி அனுப்பப்பட்டது.
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: SMM-க்குத் திருப்பி அனுப்பப்பட்டது.
- action | color: #2563eb
  label: Action Required
  text: இந்தக் கார்டில் இப்போது நீங்கள் செயல்பட வேண்டிய முறை.
- active | color: #047857
  label: Active
  text: டீலர், அல்லது புரொஃபைலில் உங்கள் சொந்தக் கணக்கு, செயலில் உள்ளது.
- inactive | color: #c2410c
  label: Inactive
  text: டீலர், அல்லது உங்கள் சொந்தக் கணக்கு, இப்போது செயலில் இல்லை.
- terminated | color: #dc2626
  label: Terminated
  text: டீலர்ஷிப் முடிந்துவிட்டது.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5025b20d273029c0
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். எல்லோரும் தங்கள் சொந்தப் புரொஃபைலைத் திறக்கலாம்.

- creator | color: #0b6b3a | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - டீலர் விண்ணப்பங்களை உருவாக்குவது, திருத்துவது
  - RM திருப்பி அனுப்பிய விண்ணப்பங்களைத் திருத்துவது
- approvers | color: #0d9488 | roles: RM, RMD, SMD, SMM
  title: RM, SMM
  - Action Required என்று குறிக்கப்பட்ட கார்டுகளைச் சரிபார்ப்பது
  - டீலர் பட்டியல்களைப் பதிவிறக்குவது
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - இறுதி ஒப்புதல் தருவது
  - டீலர்களை இறக்குமதி செய்வது அல்லது சேர்ப்பது
  - Admin தன் சொந்தப் புரொஃபைலைத் திருத்தலாம்
- dealer | color: #7c3aed | roles: Dealer
  title: டீலர்
  - உங்கள் புரொஃபைலில் பதிவு விவரங்களைச் சரிபார்ப்பது
  - உங்கள் நலத்திட்ட விண்ணப்பங்களைப் பார்ப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 40062c69f8f437fc
confidence: ok

- no-edit-button | popular: true
  q: டீலர் கார்டில் Edit அல்லது Review எனக்குத் தெரியவில்லை
  a: யாருடைய முறையோ அவருக்கு மட்டுமே இந்தப் பட்டன்கள் தெரியும். இப்போது அது யாரிடம் உள்ளது என்று அறிய, கார்டில் உள்ள நிலையைப் பாருங்கள்.
- wrong-profile | popular: true
  q: என் புரொஃபைலில் என் பெயர், பதவி அல்லது இடம் தவறாக உள்ளது
  a: **Profile** திரையில் அதை நீங்கள் மாற்ற முடியாது. அதைத் திருத்தும்படி உங்கள் நிர்வாகியிடம் (administrator) கேளுங்கள்.
- nothing-waiting
  q: Alerts-ல் "Nothing waiting for you" என்று வருகிறது
  a: இப்போது எந்த ஒப்புதலும், புக்கிங்கும், விண்ணப்பமும் உங்களுக்காகக் காத்திருக்கவில்லை. பிறகு மீண்டும் பாருங்கள்.
- dealer-buttons
  q: என் டீலர் புரொஃபைலில் Download PDF மற்றும் Change Password எதுவும் செய்வதில்லை
  a: இந்த இரண்டு பட்டன்களும் இன்னும் வேலை செய்யவில்லை.
- no-directions
  q: Get Directions அழுத்தினால் "Location permission is required" என்று வருகிறது
  a: ஆப் அல்லது பிரவுசருக்கு லொகேஷன் அனுமதி கொடுத்து, மீண்டும் **Get Directions** அழுத்துங்கள்.
:::

::: section dashboard
title: டீலர் பதிவுகளைப் பார்த்துச் செயல்படுவது
routes: /Dashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-dash-kpis] | 2 > [data-help=ma-dash-filters] | 3 > [data-help=ma-dash-cards] | 4 > [data-help=ma-dash-cards] | 5 > [data-help=ma-dash-actions]
minutes: 3
source-hash: 1b2061e620bf738f
confidence: ok

**இந்தத் திரை எதற்கு**
**Dealer Registrations** திரையில் உங்கள் பகுதியின் ஒவ்வொரு டீலர் விண்ணப்பமும் ஒரு கார்டாகத் தெரியும். மேலே எண்ணிக்கைகள் இருக்கும். பெரும்பாலான பணிப்பொறுப்புகளுக்குத் தங்கள் சொந்த State, Region அல்லது HQ-வின் டீலர்கள் மட்டுமே தெரிவார்கள்.

1. மேலே உள்ள எண்ணிக்கைகளைப் பாருங்கள்: **Total Registrations**, **Draft**, **In RM**, **In SMM**, **In AVP**, **Approved** மற்றும் **Rejected / Returned**. **Status** பெட்டி active, inactive, terminated டீலர்களை எண்ணிக் காட்டும்.
2. தேடல் பெட்டியில் டீலர் கோடு அல்லது நிறுவனப் பெயரை டைப் செய்யுங்கள், அல்லது நிறுவனம், டீலர் வகை, நிலை, மண்டலம், ஒப்புதல் நிலை அல்லது வரிசை முறையைத் தேர்வு செய்யுங்கள். கார்டுகள் உடனே மாறும்.
3. ஒவ்வொரு கார்டிலும் நிறுவனப் பெயர், **Region**, **HQ**, ஒப்புதல் நிலை, டீலர் நிலை ஆகியவை தெரியும். நீல நிற **Action Required** குறி இருந்தால், இப்போது உங்கள் முறை. மேலும் கார்டுகளைப் பார்க்க கீழே **Show more** அழுத்துங்கள்.
4. டிராஃப்டையோ திருப்பி அனுப்பப்பட்ட விண்ணப்பத்தையோ தொடர **Edit** அழுத்துங்கள், உங்களுக்கு அனுப்பப்பட்டதைச் சரிபார்க்க **Review** அழுத்துங்கள். கண் ஐகான் விண்ணப்பத்தைப் படிக்கத் திறக்கும். **SPIC** மற்றும் **GFL** டீலர்ஷிப் PDF-ஐத் திறக்கும்.
5. உங்கள் ஃபில்டர்களுடன் பட்டியலை Excel கோப்பாகப் பதிவிறக்க **Dealer Master** அல்லது **Credit limit status** அழுத்துங்கள். MO, MDO, JMDO ஆகியோருக்கு **Create Existing Dealer** பட்டனும் தெரியும். அது டீலர் விண்ணப்பப் படிவத்தைத் திறக்கும்.

> Tip: இறுதி ஒப்புதலுக்குப் பிறகு, டீலருக்கு Dealer Code இருக்கும்போது, Admin மற்றும் MO-க்கு மட்டுமே கார்டில் **Credit Limit** தெரியும்.
> Careful: Admin குப்பைத்தொட்டி ஐகான் மூலம் ஒரு பதிவை நீக்கலாம். இதைத் திரும்பப் பெற முடியாது.

## பொதுவான பிரச்சனைகள்

- **"No dealer registrations found."** (டீலர் பதிவுகள் எதுவும் இல்லை) உங்கள் தேடலோ ஃபில்டர்களோ ரொம்பக் குறுகலாக இருக்கலாம். தேடல் பெட்டியைக் காலி செய்து, ஃபில்டர்களை மீண்டும் "All" என்று மாற்றுங்கள்.
- **Edit அல்லது Review பட்டன் இல்லை.** விண்ணப்பம் இப்போது வேறு நிலையில் உள்ளது. கார்டில் உள்ள நிலையைப் பாருங்கள்.
- **மேலே உள்ள எண்கள் என் மண்டலத்துடன் பொருந்தவில்லை.** எண்ணிக்கைகளும் கார்டுகளின் அதே ஃபில்டர்களைப் பின்பற்றும். ஃபில்டர்களை மீண்டும் "All" என்று மாற்றுங்கள்.
:::

::: section dashboard-add-dealers
title: டீலர்களை இறக்குமதி செய்வது அல்லது சேர்ப்பது
routes: /Dashboard
for: 
roles: Admin, AVP
designations: 
minutes: 3
source-hash: 1bfd6d4d63fce849
confidence: ok

**இந்தத் திரை எதற்கு**
Admin மற்றும் AVP, ஏற்கனவே உள்ள டீலர்களை Excel கோப்பிலிருந்து ஆப்-க்குள் கொண்டு வரலாம், ஒரு டீலரைக் கையால் சேர்க்கலாம், அல்லது புதிய டீலர் விண்ணப்பத்தைத் தொடங்கலாம்.

1. Excel மாதிரிக் கோப்பைப் பதிவிறக்க **Sample Format** அழுத்துங்கள்.
2. ஒவ்வொரு டீலருக்கும் ஒரு வரிசையில் **CUSTOMER**, **CUSTOMER NAME**, **State** நிரப்புங்கள். CUSTOMER கோடின் முதல் எழுத்து எந்த நிறுவனக் கோடு என்று சொல்லும், உதாரணமாக SPIC-க்கு **D**.
3. **Import from Excel** அழுத்தி கோப்பைத் (.xlsx அல்லது .xls) தேர்வு செய்யுங்கள். "Importing, please wait..." என்று காட்டும்போது காத்திருங்கள்.
4. முடிவைப் படியுங்கள். இறக்குமதி ஆகாத வரிசைகள் காரணத்துடன் **Skipped Groups**-ன் கீழ் பட்டியலிடப்படும். **Close** அழுத்துங்கள்.
5. ஒரே ஒரு டீலரைச் சேர்க்க, **Add Dealer** அழுத்தி, **CUSTOMER Code**, **CUSTOMER Name**, **State** உள்ளிட்டு, **Save Dealer** அழுத்துங்கள்.
6. புதிய டீலருக்கு முழு விண்ணப்பத்தைத் தொடங்க **Create New Dealer** அழுத்துங்கள்.

> Tip: Admin-க்கு **Total Dealer List** பட்டனும் தெரியும். அது மாநில வாரியாக டீலர்களின் எண்ணிக்கையைக் காட்டும்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **CUSTOMER Code** | நிறுவனப் பதிவுகளில் உள்ள டீலரின் கோடு. கட்டாயம். |
| **CUSTOMER Name** | நிறுவனத்தின் அல்லது டீலரின் பெயர். |
| **State** | டீலரின் மாநிலம். |

## பொதுவான பிரச்சனைகள்

- **சில வரிசைகள் விடுபட்டன.** **Skipped Groups**-ன் கீழ் உள்ள காரணத்தைப் படித்து, கோப்பில் அந்த வரிசைகளைத் திருத்தி, மீண்டும் இறக்குமதி செய்யுங்கள்.
- **"Import failed."** (இறக்குமதி தோல்வி) கோப்பு மூன்று நெடுவரிசைகள் உள்ள Excel மாதிரிக் கோப்புதானா என்று பார்த்து, மீண்டும் முயற்சி செய்யுங்கள்.
:::

::: section profile
title: என் புரொஃபைலைப் பார்ப்பது
routes: /Profile
for: 
roles: 
designations: 
tour: 1 > [data-help=ma-prf-banner] | 2 > [data-help=ma-prf-tabs] | 3 > [data-help=ma-prf-details] | 4 > [data-help=ma-prf-details]
minutes: 2
source-hash: 854cdeee48d9aeee
confidence: ok

**இந்தத் திரை எதற்கு**
**Profile** திரையில் உங்கள் சொந்தக் கணக்கு விவரங்கள் தெரியும். பணியாளர்களுக்குத் தங்கள் பணியாளர் விவரங்கள் தெரியும். டீலர்களுக்குத் தங்கள் பதிவு விவரங்கள் தெரியும்.

1. பச்சை நிறப் பட்டையைப் பாருங்கள். அதில் உங்கள் பெயர், உங்கள் பணிப்பொறுப்பு அல்லது கோடு, உங்கள் பதவி தெரியும். டீலர்களுக்கு **Profile Completion**-உம் தெரியும்.
2. டேப்களைப் பயன்படுத்துங்கள். பணியாளர்களுக்கு ஒரு டேப் தெரியும் (**Employee**, Admin-க்கு **Profile**). டீலர்களுக்கு **Dealer Info**, **Proprietorship**, **Nominees**, **Trade Experience**, **Turnover**, **Assets**, **Welfare Scheme** தெரியும்.
3. கார்டில் உங்கள் விவரங்களைப் படியுங்கள், உதாரணமாக **Designation**, **Region**, **Headquarter**, **Status**. **Turnover**-ல், **SPIC** மற்றும் **Green Star** இடையே மாறலாம்.
4. ஏதாவது தவறாக இருந்தால், அதைத் திருத்தும்படி உங்கள் நிர்வாகியிடம் கேளுங்கள். இந்தத் திரையில் உங்கள் விவரங்களை நீங்கள் மாற்ற முடியாது.

> Careful: டீலர் புரொஃபைலில் **Download PDF** மற்றும் **Change Password** இன்னும் வேலை செய்யவில்லை.

## பொதுவான பிரச்சனைகள்

- **"No employee profile information available."** (பணியாளர் புரொஃபைல் தகவல் இல்லை) உங்கள் பணியாளர் பதிவு உங்கள் லாகினுடன் இணைக்கப்படவில்லை. உங்கள் நிர்வாகியிடம் கேளுங்கள்.
- **"No dealer registration data available."** (டீலர் பதிவுத் தகவல் இல்லை) உங்கள் டீலர் பதிவு இன்னும் உங்கள் லாகினுடன் இணைக்கப்படவில்லை. உங்கள் MO-வைத் தொடர்பு கொள்ளுங்கள்.
- **"No welfare scheme applications found."** (நலத்திட்ட விண்ணப்பங்கள் இல்லை) நீங்கள் இதுவரை எந்த நலத்திட்டத்துக்கும் விண்ணப்பிக்கவில்லை.
:::

::: section profile-edit
title: என் புரொஃபைலைத் திருத்துவது (Admin)
routes: /Profile
for: 
roles: Admin
designations: 
minutes: 1
source-hash: 50d60fcf9a6af1a1
confidence: ok

**இந்தத் திரை எதற்கு**
Admin பணிப்பொறுப்புக்கு மட்டுமே **Edit Profile** பட்டன் தெரியும். Admin தன் சொந்தப் பெயர், மின்னஞ்சல், பதவியை மாற்றலாம்.

1. **Admin Details** கார்டில் **Edit Profile** அழுத்துங்கள்.
2. **Name**, **Email** அல்லது **Designation**-ஐ மாற்றுங்கள். Role, User Name, Status ஆகியவற்றை இங்கே மாற்ற முடியாது.
3. **Save Changes** அழுத்துங்கள். "Profile updated successfully." (புரொஃபைல் வெற்றிகரமாகப் புதுப்பிக்கப்பட்டது) என்ற செய்தி வரும்.
4. சேமிக்காமல் மூட **Cancel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Your profile could not be saved. Please try again."** (உங்கள் புரொஃபைலைச் சேமிக்க முடியவில்லை) உங்கள் நெட்வொர்க்கைச் சரிபார்த்து, மீண்டும் **Save Changes** அழுத்துங்கள்.
- **படிவம் பூட்டப்பட்டு, உங்கள் விவரங்களை ஏற்ற முடியவில்லை என்று காட்டுகிறது.** **Cancel** அழுத்தி, மீண்டும் **Edit Profile** திறங்கள்.
:::

::: section user-details
title: உங்கள் புரொஃபைலை முழுமையாக்குவது (முன்னோட்டம்)
routes: /UserProfile
for: 
roles: 
designations: 
minutes: 1
source-hash: e4cca852b7305bb4
confidence: ok

**இந்தத் திரை எதற்கு**
**User Details** திரை ஒரு டிசைன் முன்னோட்டம் (preview) மட்டுமே. அது மெனுவில் இல்லை, இன்னும் எதையும் சேமிப்பதில்லை.

1. திரையில் **Complete Your Profile** என்றும், **Full Name**, **Mobile Number**, **Address**, **District** ஆகிய புலங்களும் தெரியும்.
2. **Continue** பட்டன் இந்த விவரங்களை இன்னும் சேமிப்பதில்லை.

## பொதுவான பிரச்சனைகள்

- **என் விவரங்கள் சேமிக்கப்படவில்லை.** இது சாதாரணம்தான். இந்தத் திரை ஒரு முன்னோட்டம் மட்டுமே. உங்கள் உண்மையான விவரங்கள் **Profile**-ல் உள்ளன.
:::

::: section alerts
title: எனக்காக என்ன காத்திருக்கிறது என்று பார்ப்பது
routes: /Alerts
for: 
roles: 
designations: 
minutes: 1
source-hash: b3694f1c480ea2a4
confidence: ok

**இந்தத் திரை எதற்கு**
**Alerts** திரையில், உங்கள் செயல் தேவைப்படக்கூடிய வரிசைகள் (queues) பட்டியலிடப்படும். போன் அல்லது டேப்லெட்டில், ஆப்-ன் மேலே உள்ள மணி ஐகானை அழுத்தி இதைத் திறங்கள். உங்கள் பணிப்பொறுப்பு பயன்படுத்தக்கூடிய லிங்க்குகள் மட்டுமே இதில் தெரியும்.

1. **Approvals waiting**-ன் கீழ், ஒரு வரிசையைத் தட்டுங்கள், உதாரணமாக **Scheme Approval** அல்லது **Dealer Application Review**.
2. **Bookings and applications**-ன் கீழ், **My Bookings** அல்லது **Welfare Schemes** தட்டுங்கள்.
3. அந்தத் திரையில் உள்ள வேலைகளை முடித்து, பிறகு இங்கே திரும்பி வாருங்கள்.

> Tip: ஆப் ஸ்டோர் வெளியீட்டுடன் புஷ் அறிவிப்புகள் (push notifications) வரும். அதுவரை இந்தத் திரையைப் பாருங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Nothing waiting for you".** (உங்களுக்காக எதுவும் காத்திருக்கவில்லை) உங்கள் பணிப்பொறுப்புக்கு இங்கே ஒப்புதல், புக்கிங் அல்லது விண்ணப்பத் திரைகள் எதுவும் இல்லை.
- **ஒரு வரிசை திறக்கிறது, ஆனால் காலியாக உள்ளது.** உங்கள் பணிப்பொறுப்புக்கு இந்த லிங்க் எப்போதும் தெரியும். ஏதோ காத்திருக்கிறது என்று அதற்கு அர்த்தம் இல்லை.
:::

::: section activities
title: என் தினசரி வேலையைத் திறப்பது
routes: /Activities
for: 
roles: 
designations: 
minutes: 1
source-hash: 3155933a5c612823
confidence: ok

**இந்தத் திரை எதற்கு**
**My Activities** திரை உங்கள் களப்பணி, ஒப்புதல்கள், விருந்தினர் இல்ல (guest house) புக்கிங்குகள், ரிப்போர்ட்டுகள் எல்லாவற்றையும் ஒரே இடத்தில் சேர்த்துக் காட்டும். போனில், MO பணியாளர்கள் இதைக் கீழே உள்ள டேப் பட்டையில் காணலாம்.

1. **Field activities**-ன் கீழ், உங்களுக்கு இருந்தால் **Sample Collection** அல்லது **Consignment History** தட்டுங்கள்.
2. **Pending on you**-ன் கீழ், **Scheme Approval** அல்லது **Pending Acknowledgement** போன்ற ஒரு வரிசையைத் தட்டுங்கள்.
3. **Guest house**-ன் கீழ், **Book a room** அல்லது **My Bookings** தட்டுங்கள்.
4. **Reports**-ன் கீழ், **Reports Center** தட்டுங்கள்.

> Tip: ஒரு பகுதியின் லிங்க்குகளில் குறைந்தது ஒன்றையாவது உங்கள் பணிப்பொறுப்பு பயன்படுத்த முடிந்தால் மட்டுமே அந்தப் பகுதி தெரியும்.

## பொதுவான பிரச்சனைகள்

- **எனக்கு "More of the Sales Activity System is on its way" மட்டுமே தெரிகிறது.** உங்கள் பணிப்பொறுப்புக்கு இன்னும் களப்பணித் திரைகள் இல்லை. அவை பிறகு சேர்க்கப்படும்.
:::

::: section contact-us
title: SPIC-ஐத் தொடர்பு கொள்வது
routes: /ContactUs
for: 
roles: 
designations: 
minutes: 2
source-hash: 6825b4b8292fb086
confidence: ok

**இந்தத் திரை எதற்கு**
**Contact Us** திரையில் SPIC அலுவலக முகவரி, மின்னஞ்சல், போன் எண், ஒரு வரைபடம் (map), உங்கள் கேள்வியை அனுப்ப ஒரு படிவம் ஆகியவை இருக்கும்.

1. மூன்று கார்டுகளில் உள்ள அலுவலக முகவரி, மின்னஞ்சல், போன் எண்ணைப் படியுங்கள். அலுவலக நேரம் திங்கள் முதல் வெள்ளி வரை, காலை 9:30 முதல் மாலை 6:00 வரை.
2. உங்கள் மேப் ஆப்-ல் அலுவலகத்தைப் பார்க்க **Open in Maps** அழுத்துங்கள்.
3. நீங்கள் இருக்கும் இடத்திலிருந்து சாலை வழி, தூரம், பயண நேரம் ஆகியவற்றைப் பார்க்க **Get Directions** அழுத்துங்கள்.
4. **Send Us a Message**-ன் கீழ், **Full Name**, **Email Address**, **Enquiry Type**, **Subject**, **Message** நிரப்புங்கள்.
5. **Send Feedback** அழுத்துங்கள். "Thank you! Your enquiry has been sent." (நன்றி! உங்கள் கேள்வி அனுப்பப்பட்டது) என்ற செய்தி வரும்.

> Careful: **Send Email**, **Call Office**, **WhatsApp Us** இன்னும் வேலை செய்யவில்லை. கார்டுகளில் உள்ள மின்னஞ்சல் அல்லது போன் எண்ணைப் பயன்படுத்துங்கள்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Enquiry Type** | **Welfare Schemes**, **Room Bookings** அல்லது **General Enquiry**. |
| **Email Address** | SPIC உங்களுக்குப் பதில் அனுப்பக்கூடிய மின்னஞ்சல். |
| **Message** | உங்கள் கேள்வி, சில வரிகளில். |

## பொதுவான பிரச்சனைகள்

- **"Please fill in every field before sending."** (அனுப்பும் முன் எல்லாப் புலங்களையும் நிரப்புங்கள்) ஐந்து புலங்களும் கட்டாயம்.
- **"Please enter a valid email address."** (சரியான மின்னஞ்சல் முகவரியை உள்ளிடுங்கள்) மின்னஞ்சலைச் சரிபாருங்கள், உதாரணமாக name@example.com.
- **"Location permission is required to calculate the distance to SPIC Office."** (SPIC அலுவலகத்துக்கான தூரத்தைக் கணக்கிட லொகேஷன் அனுமதி தேவை) ஆப்-க்கு லொகேஷன் அனுமதி கொடுத்து, மீண்டும் **Get Directions** அழுத்துங்கள்.
- **"Your enquiry could not be sent."** (உங்கள் கேள்வியை அனுப்ப முடியவில்லை) உங்கள் நெட்வொர்க்கைச் சரிபார்த்து, பிறகு மீண்டும் முயற்சி செய்யுங்கள்.
:::

::: section gallery
title: புகைப்படங்களும் வீடியோக்களும் (முன்னோட்டம்)
routes: /Gallery, /Album, /ViewVideo
for: 
roles: 
designations: 
minutes: 1
source-hash: 00d0d57d14bc58ed
confidence: ok

**இந்தத் திரை எதற்கு**
**Gallery**-ல் SDWA நிகழ்ச்சிகள், கூட்டங்கள், விருந்தினர் இல்லம் ஆகியவற்றின் மாதிரிப் படங்கள் தெரியும். இந்தத் திரைகள் மாதிரிப் படங்களுடன் கூடிய டிசைன் முன்னோட்டம் மட்டுமே, இன்னும் உண்மையான ஆல்பங்கள் இல்லை.

1. **Gallery**-ல், **Photos** மற்றும் **Videos**-ஐ ஸ்க்ரோல் செய்து பாருங்கள்.
2. **Guest House Rooms** ஆல்பத்தைத் திறக்க, ஒரு புகைப்படத்தில் **View Album** அழுத்துங்கள்.
3. ஆல்பத்தில், **All Media**, **Photos** அல்லது **Videos** தட்டுங்கள், அல்லது தேடல் பெட்டியில் டைப் செய்யுங்கள்.
4. வீடியோ கார்டைத் தட்டுங்கள். இன்னும் எந்த வீடியோவும் வெளியிடப்படாததால், வீடியோ திரையில் "Video will appear here" என்று காட்டும்.

> Tip: **Gallery**-ல் உள்ள டேப்கள், தேடல் பெட்டி, வரிசைப் பட்டியல் இன்னும் வேலை செய்யவில்லை. ஒவ்வொரு **View Album** பட்டனும் அதே மாதிரி ஆல்பத்தைத்தான் திறக்கும்.

## பொதுவான பிரச்சனைகள்

- **வீடியோ ஓடவில்லை.** இன்னும் எந்த வீடியோவும் வெளியிடப்படவில்லை.
:::
