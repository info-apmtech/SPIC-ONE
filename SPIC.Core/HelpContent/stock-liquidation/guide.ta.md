---
module: stock-liquidation
title: ஸ்டாக் மற்றும் விற்பனை (liquidation)
icon: bi-boxes
order: 106
language: ta
category: விற்பனை
note: புதிய வழிகாட்டி
meta-source-hash: b8a21e045ebf822e
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: fb227391bc0718ee
confidence: ok

SPIC உரம் கம்பெனியை விட்டுப் போன பிறகு அதைப் பின்தொடரலாம். டீலர்கள் இன்னும் எந்த விற்பனைகளை acknowledge செய்யவில்லை, wholesaler-களிடமும் retailer-களிடமும் எவ்வளவு ஸ்டாக் இருக்கிறது, அது எத்தனை நாள் பழையது, அது எவ்வளவு வேகமாக விற்கப்படுகிறது என்று பார்க்கலாம். இந்த எண்கள் அரசின் உர அமைப்பான IFMS-லிருந்து வருகின்றன.

- ack | icon: bi-hourglass-split | color: #db2777
  title: நிலுவையில் உள்ள acknowledgement
  text: வாங்கியவர் இன்னும் IFMS-ல் உறுதி செய்யாத கம்பெனி மற்றும் டீலர் விற்பனைகளைப் பாருங்கள்.
- stock | icon: bi-boxes | color: #2563eb
  title: ஸ்டாக்கும் அதன் வயதும்
  text: ஒவ்வொரு டீலரிடமும் எவ்வளவு ஸ்டாக் இருக்கிறது, அது எத்தனை நாளாக அங்கே இருக்கிறது என்று பாருங்கள்.
- liquidation | icon: bi-graph-up-arrow | color: #16a34a
  title: விற்பனை (Liquidation)
  text: டீலர்கள் தங்கள் ஸ்டாக்கை எவ்வளவு வேகமாக விற்கிறார்கள், எந்த டீலர்கள் மெதுவாக இருக்கிறார்கள் என்று பாருங்கள்.
:::

::: section flow
title: ஸ்டாக் எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 12cbf705ed2b7495
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- company | type: lane | color: #db2777
  title: SPIC
  sub: கம்பெனி விற்பனை
- dealers | type: lane | color: #7c3aed
  title: டீலர்கள்
  sub: IFMS-ல் உள்ள wholesaler-களும் retailer-களும்
- admin | type: lane | color: #374151 | roles: Admin
  title: Admin
  sub: IFMS கோப்புகளை ஏற்றுவார்
- staff | type: lane | color: #0d9488 | roles: Director, AVP, SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: விற்பனைப் பணியாளர்கள்
  sub: தங்கள் பகுதியின் ரிப்போர்ட்களைக் கவனிப்பார்கள்
- sale | lane: company | col: 1 | icon: bi-receipt | color: #db2777 | next: ack | screen: pending-ack
  title: கம்பெனி விற்பனை
  status: New
  text: SPIC ஒரு wholesaler-க்கு உரம் விற்கும். இன்வாய்ஸ் IFMS-ல் பதிவாகும். Wholesaler-களும் இதே முறையில் retailer-களுக்கு விற்பார்கள்.
- ack | lane: dealers | col: 2 | icon: bi-clipboard-check | color: #16a34a | next: upload | screen: ack-cycle
  title: டீலர் acknowledge செய்வார்
  status: Ack
  text: ஸ்டாக் வந்து சேர்ந்தது என்று வாங்கியவர் IFMS-ல் உறுதி செய்வார். இன்வாய்ஸ் தேதியிலிருந்து பெற்ற நாள் வரையான நாட்கள்தான் acknowledgement சுழற்சி (cycle).
- upload | lane: admin | col: 3 | icon: bi-file-earmark-arrow-up | color: #374151 | next: stock | screen: file-upload
  title: IFMS ரிப்போர்ட்களை ஏற்றுதல்
  text: IFMS ரிப்போர்ட்கள் தினமும் SPIC ONE-க்கு வரும், இரவு நேர import மூலமாகவோ Admin-இன் Excel அப்லோட் மூலமாகவோ.
- stock | lane: dealers | col: 4 | icon: bi-boxes | color: #2563eb | next: sell | screen: stock-report
  title: டீலரிடம் உள்ள ஸ்டாக்
  status: Fresh
  text: Acknowledge செய்த ஸ்டாக் wholesaler அல்லது retailer-இடம் இருக்கும். அது acknowledge செய்த நாளிலிருந்து அதன் வயது கணக்கிடப்படும்.
- sell | lane: dealers | col: 5 | icon: bi-cart-check | color: #16a34a | next: review | screen: liquidation-cycle
  title: விற்கப்பட்டது (liquidated)
  status: Active
  text: டீலர் ஸ்டாக்கை retailer-களுக்கும் விவசாயிகளுக்கும் விற்பார். விற்பனை ஆக ஆக மீதி ஸ்டாக் குறையும்.
- review | lane: staff | col: 6 | icon: bi-bar-chart | color: #0d9488 | screen: stock-details
  title: கவனித்துப் பின்தொடருதல்
  text: பணியாளர்கள் தங்கள் பகுதியின் ஸ்டாக், விற்பனை, ஸ்டாக் வயது ஆகியவற்றைப் பார்த்து, மெதுவாக இருக்கும் டீலர்களிடம் பின்தொடர்வார்கள்.
- pending | lane: staff | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: sale | to: ack | screen: pending-ack
  title: நேரத்தில் acknowledge செய்யப்படவில்லை
  status: Overdue
  text: வாங்கியவர் acknowledge செய்யவில்லை என்றால், விற்பனை நிலுவையிலேயே இருந்து பழையதாகும். பணியாளர்கள் அதை IFMS-ல் acknowledge செய்யுமாறு டீலரிடம் கேட்பார்கள்.
- aged | lane: staff | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: stock | to: sell | screen: ageing-report
  title: நகராத பழைய ஸ்டாக்
  status: Long Aged
  text: விற்காத ஸ்டாக் பழையதாகி Long Aged அல்லது Critical என்று காட்டும். அதை முதலில் விற்க பணியாளர்கள் டீலருக்கு உதவுவார்கள்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 5b6bc08644fdb0cc
confidence: ok

- new | color: #64748b
  label: New
  text: IFMS நிலை. விற்பனை பதிவாகிவிட்டது, ஆனால் வாங்கியவர் இன்னும் acknowledge செய்யவில்லை.
- ack | color: #16a34a
  label: Ack
  text: IFMS நிலை. ஸ்டாக் வந்து சேர்ந்தது என்று வாங்கியவர் acknowledge செய்துவிட்டார்.
- completed | color: #16a34a
  label: Completed
  text: விற்பனை acknowledge செய்யப்பட்டுவிட்டது. இனி அது நிலுவையில் இல்லை.
- latest | color: #2563eb
  label: Latest
  text: 0 முதல் 10 நாட்களாக acknowledgement-க்குக் காத்திருக்கிறது.
- overdue | color: #dc2626
  label: Overdue
  text: 21 நாட்கள் அல்லது அதற்கு மேல் acknowledgement-க்குக் காத்திருக்கிறது.
- consent | color: #9333ea
  label: Consent of Buyer
  text: விற்பனை வாங்கியவரின் ஒப்புதலுக்காகக் காத்திருக்கிறது என்று IFMS காட்டுகிறது.
- fast | color: #16a34a
  label: Fast
  text: இன்வாய்ஸிலிருந்து 0 முதல் 2 நாட்களுக்குள் acknowledge செய்யப்பட்டது.
- normal | color: #d97706
  label: Normal
  text: இன்வாய்ஸிலிருந்து 3 முதல் 5 நாட்களுக்குள் acknowledge செய்யப்பட்டது.
- delayed | color: #ea580c
  label: Delayed
  text: இன்வாய்ஸுக்குப் பிறகு 6 முதல் 10 நாட்களில் acknowledge செய்யப்பட்டது.
- pending-ack | color: #64748b
  label: Pending ACK
  text: ஸ்டாக் ரிப்போர்ட். இந்த ஸ்டாக்குக்கு IFMS-ல் இன்னும் acknowledgement இல்லை, அதனால் அதற்கு வயது இல்லை.
- fresh | color: #059669
  label: Fresh
  text: 0 முதல் 30 நாள் பழைய ஸ்டாக்.
- medium | color: #34d399
  label: Medium
  text: 31 முதல் 90 நாள் பழைய ஸ்டாக்.
- slow | color: #f59e0b
  label: Slow Moving
  text: 91 முதல் 180 நாள் பழைய ஸ்டாக்.
- long | color: #ef4444
  label: Long Aged
  text: 181 முதல் 364 நாள் பழைய ஸ்டாக்.
- active | color: #16a34a
  label: Active
  text: விற்பனை (Liquidation). டீலர் கடந்த 45 நாட்களுக்குள் இந்த ஸ்டாக்கை விற்றிருக்கிறார்.
- monitoring | color: #d97706
  label: Monitoring
  text: விற்பனை (Liquidation). 46 முதல் 75 நாட்களாக விற்பனை இல்லை. தொடர்ந்து கவனியுங்கள்.
- critical | color: #b91c1c
  label: Critical
  text: ஒவ்வொரு திரையிலும் மிக மோசமான பிரிவு. நிலுவையில் உள்ள acknowledgement: 11 முதல் 20 நாட்கள். Acknowledgement சுழற்சி: 10 நாட்களுக்கு மேல். ஸ்டாக் வயது: 365 நாட்கள் அல்லது அதற்கு மேல். விற்பனை (Liquidation): 75 நாட்களுக்கு மேல் விற்பனை இல்லை.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 1e4dca540aba6f06
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- staff | color: #0d9488 | roles: SMD, SMM, RM, RMD, MO, MDO, JMDO
  title: விற்பனைப் பணியாளர்கள்
  - உங்கள் சொந்த மாநிலம், மண்டலம் அல்லது HQ-வின் நிலுவை acknowledgement, ஸ்டாக், ஸ்டாக் வயது, விற்பனை ஆகியவற்றைப் பார்ப்பது
  - Acknowledge செய்யவோ விற்கவோ தாமதிக்கும் டீலர்களிடம் பின்தொடர்வது
  - பட்டியல்களை Excel-ஆகப் பதிவிறக்குவது
- leaders | color: #db2777 | roles: Director, AVP
  title: Director மற்றும் AVP
  - எல்லா மாநிலங்களின் எண்களையும் பார்ப்பது
  - மாநிலங்கள், டீலர்கள், product-களை ஒப்பிடுவது
- admin | color: #374151 | roles: Admin
  title: Admin
  - IFMS Excel ரிப்போர்ட்களைத் தினமும் அப்லோட் செய்வது
  - எல்லா மாநிலங்களின் எண்களையும் பார்ப்பது
  - State-wise Stock Reconciliation-ஐ email-ல் அனுப்புவது
- dealers | color: #7c3aed | roles: Dealer
  title: Wholesaler-களும் retailer-களும்
  - பெற்ற ஸ்டாக்கை IFMS-ல் acknowledge செய்வது
  - ஸ்டாக்கை வேகமாக விற்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 12f38854e10b0f55
confidence: ok

- overdue | popular: true
  q: ஒரு விற்பனை Overdue என்று காட்டுகிறது. நான் என்ன செய்வது?
  a: வாங்கியவர் 21 நாட்கள் அல்லது அதற்கு மேலாக IFMS-ல் ஸ்டாக்கை acknowledge செய்யவில்லை. அதை IFMS-ல் acknowledge செய்யுமாறு டீலரிடம் கேளுங்கள். அடுத்த IFMS புதுப்பிப்புக்குப் பிறகு அது **Completed** என்று காட்டும்.
- old-figures | popular: true
  q: எண்கள் பழையதாகத் தெரிகின்றன அல்லது ரிப்போர்ட் காலியாக உள்ளது
  a: SPIC ONE-ல் கடைசியாக ஏற்றப்பட்ட IFMS டேட்டாவைத்தான் ரிப்போர்ட்கள் காட்டும். இன்றைய கோப்புகள் இன்னும் ஏற்றப்படவில்லை என்றால், **File Upload**-ல் அவற்றை அப்லோட் செய்யுமாறு Admin-இடம் கேளுங்கள், பிறகு **Refresh Data** அழுத்துங்கள்.
- my-area
  q: என்னால் வேறு மாநிலத்தையோ மண்டலத்தையோ தேர்வு செய்ய முடியவில்லை
  a: பெரும்பாலான பணியாளர்கள் தங்கள் சொந்த மாநிலம், மண்டலம் அல்லது HQ-வை மட்டுமே பார்ப்பார்கள். Director, AVP, Admin எல்லா மாநிலங்களையும் பார்ப்பார்கள்.
- no-rows
  q: "No records found." அல்லது "No Data Available" என்று வருகிறது
  a: உங்கள் ஃபில்டர்கள் ரொம்பக் குறுகலாக இருக்கலாம். **Clear All** அழுத்தி, பிறகு **Search** அழுத்துங்கள்.
- pdf
  q: PDF மற்றும் Send Mail பட்டன்கள் சாம்பல் நிறத்தில் உள்ளன
  a: இவை இன்னும் தயாராகவில்லை. பட்டியலைப் பதிவிறக்க **Excel** பயன்படுத்துங்கள்.
:::

::: section pending-ack
title: நிலுவையில் உள்ள acknowledgement-களைப் பார்ப்பது
routes: /CompanySales
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-pa-filters] | 2 > [data-help=sl-pa-cards] | 3 > [data-help=sl-pa-tabs] | 4 > [data-help=sl-pa-table] | 5 > [data-help=sl-pa-export]
minutes: 3
source-hash: bc4973981c2b913d
confidence: ok

**இந்தத் திரை எதற்கு**
**Pending Acknowledgement** திரையில் கம்பெனி, wholesaler, retailer விற்பனைகளும், வாங்கியவர் அவற்றை IFMS-ல் acknowledge செய்தாரா என்பதும் தெரியும். மெனுவில் **Pending Acknowledgement** அழுத்தி இதைத் திறங்கள்.

1. **Filters**-ஐத் திறந்து **Date Range**, **State**, **District**, **Dealer Type**, **Dealer / Agency Name**, **Product** அல்லது **Status (By Age)** தேர்வு செய்யுங்கள். **Search** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
2. மூன்று கார்டுகளைப் படியுங்கள்: மொத்தம், **Company Sales**, **Wholesaler Sales**. ஒவ்வொன்றிலும் **Completed**, **Latest**, **Critical**, **Overdue**, **Consent of Buyer** தெரியும். கீழே உள்ள சார்ட் மாநிலங்களை ஒப்பிடும்.
3. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All**, **Company Sales**, **Wholesaler Sales** அல்லது **Retailer Sales**. ஒரு விற்பனையைக் கண்டுபிடிக்க, தேடல் பெட்டியில் இன்வாய்ஸ் எண் அல்லது டீலர் பெயரை டைப் செய்யுங்கள்.
4. அட்டவணையில் **Age Status** மற்றும் **Pending Ack Age (Days)** பாருங்கள். முழு **Record Details** பார்க்க **View** அழுத்துங்கள்.
5. பட்டியலைப் பதிவிறக்க **Excel** அழுத்துங்கள்.

> Tip: மிகப் பழைய நிலுவை விற்பனைகளை முதலில் பார்க்க **Pending Ack Age (Days)** வைத்து வரிசைப்படுத்துங்கள், அந்த டீலர்களுக்கு போன் செய்யுங்கள்.

| புலம் | என்ன காட்டும் |
|---|---|
| **Age Status** | **Latest** 0 முதல் 10 நாட்கள், **Critical** 11 முதல் 20 நாட்கள், **Overdue** 21 நாட்கள் அல்லது அதற்கு மேல், அல்லது **Consent of Buyer**. |
| **Workflow Status** | IFMS-லிருந்து வரும் நிலை, உதாரணமாக **New** அல்லது **Ack**. |
| **Quantity (MT)** | மெட்ரிக் டன்னில் அளவு. |

## பொதுவான பிரச்சனைகள்

- **"No records found."** (பதிவுகள் எதுவும் இல்லை) Filters-ல் **Clear All** அழுத்துங்கள், அல்லது தேடல் பெட்டியைக் காலி செய்யுங்கள்.
- **டேப்களை மாற்றினால் கார்டுகள் மாறவில்லை.** இது சாதாரணம்தான். டேப்கள் அட்டவணையையும் Excel கோப்பையும் மட்டுமே மாற்றும்.
- **டீலர் acknowledge செய்த பிறகும் விற்பனை நிலுவையிலேயே உள்ளது.** புதிய IFMS டேட்டா இன்னும் ஏற்றப்படவில்லை. அடுத்த புதுப்பிப்புக்குப் பிறகு மீண்டும் பாருங்கள்.
:::

::: section ack-cycle
title: டீலர்கள் எவ்வளவு வேகமாக acknowledge செய்கிறார்கள் என்று பார்ப்பது
routes: /Acknowledgement
for: 
roles: 
designations: 
minutes: 2
source-hash: 751788d0db9a139a
confidence: ok

**இந்தத் திரை எதற்கு**
**Acknowledgement Cycle** திரையில், இன்வாய்ஸுக்குப் பிறகு டீலர்கள் ஸ்டாக்கை acknowledge செய்ய எத்தனை நாட்கள் எடுத்தார்கள் என்று தெரியும். இதில் acknowledge செய்த விற்பனைகள் மட்டுமே தெரியும்.

1. **Filters**-ஐத் திறந்து **State**, **Region**, **Head Quarters**, **District**, **Product**, **Dealer Name**, **Status**, **Cycle Bucket** அல்லது **Invoice Date From** மற்றும் **Invoice Date To** தேர்வு செய்யுங்கள். **Search** அழுத்துங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total Transactions**, **Fast (0-2 Days)**, **Normal (3-5 Days)**, **Delayed (6-10 Days)** மற்றும் **Critical (> 10 Days)**.
3. இரண்டு top 5 பட்டியல்களையும் எதன்படி பிரிக்க வேண்டும் என்று தேர்வு செய்யுங்கள்: **State**, **Region**, **Head Quarters**, **District**, **Product** அல்லது **Dealer Name**.
4. **Top 5 Fast Performing** பட்டியலை **Top 5 Delayed** பட்டியலுடன் ஒப்பிடுங்கள்.
5. **Acknowledgement Cycle List**-ல் ஒவ்வொரு விற்பனையின் **Ack Cycle (Days)** மற்றும் **Status** பாருங்கள். **Acknowledgement Details** பார்க்க **View** அழுத்துங்கள்.
6. பட்டியலைப் பதிவிறக்க **Excel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **A top 5 list says "No data for the current filters."** (ஒரு top 5 பட்டியலில் "இப்போதைய ஃபில்டர்களுக்கு டேட்டா இல்லை" என்று வருகிறது) ஒரு பிரிவு தரவரிசையில் வர குறைந்தது 50 acknowledge செய்த விற்பனைகள் வேண்டும். ஃபில்டர்களை அகலமாக்குங்கள்.
- **"No acknowledged transactions found."** (acknowledge செய்த பரிவர்த்தனைகள் எதுவும் இல்லை) **Clear All** அழுத்தி, பிறகு **Search** அழுத்துங்கள்.
:::

::: section stock-report
title: டீலர்களிடம் உள்ள ஸ்டாக்கைப் பார்ப்பது
routes: /StockReport
for: 
roles: 
designations: 
tour: 1 > [data-help=sl-sr-filters] | 2 > [data-help=sl-sr-cards] | 3 > [data-help=sl-sr-charts] | 4 > [data-help=sl-sr-search] | 5 > [data-help=sl-sr-table] | 6 > [data-help=sl-sr-export]
minutes: 3
source-hash: 5c6d4945a1c3da6d
confidence: ok

**இந்தத் திரை எதற்கு**
**Stock Report** திரையில், இப்போது ஒவ்வொரு wholesaler மற்றும் retailer-இடமும் எவ்வளவு ஸ்டாக் இருக்கிறது, அது எத்தனை நாள் பழையது என்று தெரியும்.

1. **Filters**-ஐத் திறந்து **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying With**, **Product** அல்லது **Ageing Range (Days)** தேர்வு செய்யுங்கள். **Search** அழுத்துங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total Stock (Current Stock)**, **Dealers Count**, **Average Ageing** மற்றும் **High Ageing Stock**.
3. சார்ட்களைப் பாருங்கள்: **State-wise Stock Availability** இந்த ஆண்டை கடந்த ஆண்டுடன் ஒப்பிடும், **Product-wise Stock Distribution (MT)** ஒவ்வொரு product-இன் பங்கையும் காட்டும்.
4. **Stock Report List**-க்கு மேலே உள்ள தேடல் பெட்டியில் டீலர், மாநிலம் அல்லது product பெயரை டைப் செய்யுங்கள்.
5. அட்டவணையில் **Quantity (MT)**, ஸ்டாக் யாரிடம் உள்ளது (**Lying With**), **Ageing in Days** மற்றும் **Status** பாருங்கள். முழு **Stock Details** பார்க்க **View** அழுத்துங்கள்.
6. பட்டியலைப் பதிவிறக்க **Excel** அழுத்துங்கள்.

> Tip: பழைய ஸ்டாக்கை வேகமாகக் கண்டுபிடிக்க, **Ageing Range (Days)**-ல் **Long Aged (181-364)** மற்றும் **Critical (365+)** தேர்வு செய்யுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No Data Available".** (டேட்டா இல்லை) Filters-ல் **Clear All** அழுத்துங்கள், அல்லது தேடல் பெட்டியைக் காலி செய்யுங்கள்.
- **Ageing-ல் "--" என்றும், நிலை Pending ACK என்றும் காட்டுகிறது.** IFMS-ல் acknowledge செய்த நாளிலிருந்து மட்டுமே வயது கணக்கிடப்படும். Acknowledgement இல்லாத ஸ்டாக்குக்கு இன்னும் வயது இல்லை.
:::

::: section ageing-report
title: பழைய மற்றும் மெதுவாக நகரும் ஸ்டாக்கைக் கண்டுபிடிப்பது
routes: /AgeingReport
for: 
roles: 
designations: 
minutes: 2
source-hash: 366d05a43d5fbe3a
confidence: ok

**இந்தத் திரை எதற்கு**
**Ageing Report** திரையில், ஸ்டாக் டீலர்களிடம் எத்தனை நாளாக இருக்கிறது என்று தெரியும். இதனால் மெதுவாக நகரும் ஸ்டாக்கைக் கண்டுபிடிக்கலாம்.

1. **Filters**-ஐத் திறந்து **State**, **Region**, **Head Quarters**, **District**, **Sub District**, **Lying with**, **Product** அல்லது **Ageing Range (Days)** தேர்வு செய்யுங்கள். **Search** அழுத்துங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total Stock (MT)**, **Average Ageing (Days)**, **30-60 Days Stock (MT)** மற்றும் **60+ Days Stock (MT)**.
3. **State-wise Stock by Ageing (MT)**-ல், ஒரு மாநிலத்தின் ஒவ்வொரு வயதுப் பிரிவையும் பார்க்க அதன் பாரின் மேல் வையுங்கள்.
4. **Ageing by Days (MT)**-ல், ஒவ்வொரு வயதுப் பிரிவிலும் எவ்வளவு ஸ்டாக் உள்ளது என்று பாருங்கள்.
5. **Ageing Report List**-ல் ஒவ்வொரு டீலரின் **Ageing in Days** மற்றும் **Status** பாருங்கள். **Stock Ageing Details** பார்க்க **View** அழுத்துங்கள்.
6. பட்டியலைப் பதிவிறக்க **Excel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No records found."** (பதிவுகள் எதுவும் இல்லை) **Clear All** அழுத்தி, பிறகு **Search** அழுத்துங்கள்.
- **சில பழைய ஸ்டாக் தெரியவில்லை.** IFMS-ல் acknowledge செய்த நாளிலிருந்து மட்டுமே வயது கணக்கிடப்படும்.
:::

::: section liquidation-cycle
title: ஸ்டாக் எவ்வளவு வேகமாக விற்கப்படுகிறது என்று பார்ப்பது
routes: /LiquidationCycle
for: 
roles: 
designations: 
minutes: 3
source-hash: f76600523961f075
confidence: ok

**இந்தத் திரை எதற்கு**
**Liquidation Cycle** திரையில், ஒவ்வொரு டீலரிடமும் எவ்வளவு ஸ்டாக் இருந்தது, அவர் எவ்வளவு விற்றார், கடைசி விற்பனைக்குப் பிறகு எத்தனை நாள் ஆனது என்று தெரியும்.

1. **Filters**-ஐத் திறந்து **Date From**, **Date To**, **State**, **Region**, **HQ**, **District**, **Sub District**, **Dealer Name**, **Product** அல்லது **Financial Year** தேர்வு செய்யுங்கள். **Apply Filters** அழுத்துங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total Stock (MT)**, **Liquidated (MT)** மற்றும் **Balance Stock (MT)**.
3. **Dealer Liquidation Performance**-ன் கீழ், **Top 5 Performance**, **State-wise**, **All**, **Company Sales**, **Wholesalers** அல்லது **Retailers** தேர்வு செய்யுங்கள்.
4. வேகமான பட்டியலை மெதுவான பட்டியலுடன் ஒப்பிடுங்கள், உதாரணமாக **Top 5 Fast Dealers** மற்றும் **Top 5 Slow Dealers**.
5. **Liquidation Cycle List**-ல் **Stock (MT)**, **Ageing (Days)**, **Sales (MT)** மற்றும் **Status** பாருங்கள். **Liquidation Details** பார்க்க **View** அழுத்துங்கள்.
6. பட்டியலைப் பதிவிறக்க **Excel** அழுத்துங்கள்.

> Tip: இங்கே **Ageing (Days)** என்றால், அந்த product-ஐ டீலர் கடைசியாக விற்றதிலிருந்து ஆன நாட்கள்.

## பொதுவான பிரச்சனைகள்

- **"No records found."** (பதிவுகள் எதுவும் இல்லை) **Clear All** அழுத்தி, பிறகு **Apply Filters** அழுத்துங்கள்.
- **என்னால் வேறு மாநிலத்தைத் தேர்வு செய்ய முடியவில்லை.** உங்கள் பகுதி உங்கள் login-ஆல் நிர்ணயிக்கப்படுகிறது. Director, AVP, Admin எல்லா மாநிலங்களையும் பார்ப்பார்கள்.
:::

::: section stock-details
title: மாநில வாரியாக தினசரி ஸ்டாக்கும் விற்பனையும் பார்ப்பது
routes: /StockDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: b30cf269de71aa03
confidence: ok

**இந்தத் திரை எதற்கு**
**Stock Details** திரையில் ஒவ்வொரு மாநிலத்துக்கும் ஒரு வரி இருக்கும்: தொடக்க ஸ்டாக், சப்ளை, விற்பனை, இறுதி ஸ்டாக். இது தினமும் புதுப்பிக்கப்படும்.

1. மேலே **Date as on** மற்றும் **Period** பாருங்கள்.
2. **Filters**-ஐத் திறந்து **From Date**, **To Date**, **Financial Year**, **State**, **Region**, **Head Quarters** அல்லது **Product** தேர்வு செய்யுங்கள். **Search** அழுத்துங்கள்.
3. கார்டுகளைப் படியுங்கள்: **Total Stock**, **Total Sales**, **Closing Stock** மற்றும் **Sales %**.
4. **Stock List**-ல் ஒவ்வொரு மாநிலத்தின் **Opening Stock**, **Supplies**, **Total Stock**, **Sales**, **Total Sales**, **Closing Stock** மற்றும் **Sales %** படியுங்கள். கடைசி வரி **Grand Total**.
5. ஒரு மாநிலத்தைக் கண்டுபிடிக்க, தேடல் பெட்டியில் அதன் பெயரை டைப் செய்யுங்கள்.
6. பட்டியலைப் பதிவிறக்க **Excel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No Data Available".** (டேட்டா இல்லை) **Clear All** அழுத்தி, பிறகு **Search** அழுத்துங்கள்.
- **நெடுவரிசைகளின் தேதிகள் மாறுகின்றன.** நெடுவரிசைத் தலைப்புகள் நீங்கள் தேர்வு செய்த காலத்தின் தேதிகளைக் காட்டும்.
:::

::: section product-stock
title: மாநில வாரியாக ஒவ்வொரு product-இன் ஸ்டாக்கைப் பார்ப்பது
routes: /ProductWiseStockAvailability
for: 
roles: 
designations: 
minutes: 2
source-hash: 5858eb268707eedc
confidence: ok

**இந்தத் திரை எதற்கு**
**Product-wise Stock Availability** திரையில், ஒவ்வொரு மாநிலத்திலும் ஒவ்வொரு product எவ்வளவு ஸ்டாக்கில் உள்ளது என்று தெரியும்.

1. **Filters**-ஐத் திறந்து **State**, **Region**, **Head Quarters**, **Date From** அல்லது **Date To** தேர்வு செய்யுங்கள். **Search** அழுத்துங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total States**, **Total Products**, **Total Quantity (MT)**, **Highest Stock State** மற்றும் **Low Stock Alerts**.
3. **Stock Availability List**-ல், ஒவ்வொரு வரிசையும் ஒரு மாநிலம், ஒவ்வொரு நெடுவரிசையும் ஒரு product. **Total (MT)** என்பது அந்த மாநிலத்தின் மொத்தம்.
4. ஒரு மாநிலத்தைக் கண்டுபிடிக்க, தேடல் பெட்டியில் அதன் பெயரை டைப் செய்யுங்கள். கடைசி வரி **Grand Total**.
5. அட்டவணையைப் பதிவிறக்க **Excel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No Data Available".** (டேட்டா இல்லை) **Clear All** அழுத்தி, பிறகு **Search** அழுத்துங்கள்.
- **Low Stock Alerts அதிகமாக உள்ளது.** இந்த மாநிலங்களில் ஸ்டாக் அளவு வரம்புக்குக் கீழே உள்ளது. அவற்றுக்கு சப்ளையைத் திட்டமிடுங்கள்.
:::

::: section stock-reconciliation
title: மாநில வாரியான ஸ்டாக் சரிபார்ப்பைப் (reconciliation) பார்ப்பது
routes: /StateWiseGlobalStockReconciliation
for: 
roles: 
designations: 
minutes: 2
source-hash: 50c58211c889d4c4
confidence: ok

**இந்தத் திரை எதற்கு**
**State-wise Stock Reconciliation** திரை, ஒரு நாளுக்கு, மாநிலம் மாநிலமாக, கம்பெனியின் ஸ்டாக்கை wholesaler-கள் மற்றும் retailer-களிடம் உள்ள ஸ்டாக்குடன் ஒப்பிட்டுப் பொருத்தும்.

1. **As On Date:** தேர்வு செய்து **View** அழுத்துங்கள். இன்றைய தேதிக்குத் திரும்ப **Today** அழுத்துங்கள்.
2. **Partial Report - Pending Uploads** பெட்டி வந்தால், அந்த நாளின் சில ரிப்போர்ட்கள் இன்னும் ஏற்றப்படவில்லை என்று அர்த்தம்.
3. **Urea Stock** அட்டவணையில் ஒவ்வொரு மாநிலத்தின் **Company Stock**, **Wholesaler Stock**, **Retail Point Stock**, வழியில் உள்ள ஸ்டாக் (GIT) மற்றும் **Overall Stock** படியுங்கள்.
4. Product மற்றும் மாநில வாரியான ஸ்டாக்குக்கு கீழே **GFL Stock** அட்டவணைக்கு ஸ்க்ரோல் செய்யுங்கள்.
5. ஒரு மாநிலத்தைக் கண்டுபிடிக்க, தேடல் பெட்டியில் அதன் பெயரை டைப் செய்யுங்கள்.
6. பதிவிறக்க **Export Excel** அழுத்துங்கள், அல்லது ரிப்போர்ட்டை அனுப்ப **Send Email** அழுத்துங்கள். **To (recipient email)** உள்ளிட்டு, தேவைப்பட்டால் **CC (optional)** சேர்த்து, **Send Email** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No Reconciliation Data Available".** (சரிபார்ப்பு டேட்டா இல்லை) அந்தத் தேதிக்கான ரிப்போர்ட்கள் அப்லோட் செய்யப்படவில்லை. வேறு தேதியைத் தேர்வு செய்யுங்கள், அல்லது அவற்றை அப்லோட் செய்யுமாறு Admin-இடம் கேளுங்கள். Admin-கள் **Go to File Upload** அழுத்தலாம்.
- **ஒரு நெடுவரிசையில் எண்கள் இல்லை.** அந்த நாளுக்கான அந்த ரிப்போர்ட் இல்லை. **Partial Report** பெட்டியைப் பாருங்கள்.
:::

::: section rankings
title: முன்னணி மாவட்டங்கள், wholesaler-கள், retailer-கள்
routes: /TopRankingDistrict, /TopRankingWholesalers, /TopRankingRetailers
for: 
roles: 
designations: 
minutes: 1
source-hash: aa745aaa1c48ea10
confidence: ok

**இந்தத் திரை எதற்கு**
குறைந்த நேரத்தில் அதிக ஸ்டாக் விற்ற மாவட்டங்கள், wholesaler-கள், retailer-களை ranking திரைகள் பட்டியலிடும். மெனுவிலிருந்து **Top Ranking District**, **Top Ranking Wholesaler** அல்லது **Top Ranking Retailers** திறங்கள்.

1. உங்களுக்குத் தேவையான ranking-ஐ மெனுவிலிருந்து திறங்கள்.
2. மேலே உள்ள கார்டுகளைப் படியுங்கள், உதாரணமாக மொத்த எண்கள் மற்றும் **Avg Liquidation Cycle**.
3. பட்டியலில் rank 1 தான் சிறந்தது. ஒவ்வொன்றின் **State**, **Region**, **HQ**, **District** மற்றும் **Sub-District** பாருங்கள்.

> Careful: இந்த ranking திரைகள் இன்னும் நேரடி (live) டேட்டாவுடன் இணைக்கப்பட்டு வருகின்றன. இப்போது அவற்றில் உள்ள எண்களும் ஃபில்டர்களும் உதாரணங்கள் மட்டுமே. உண்மையான எண்களுக்கு **Liquidation Cycle** பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **ஃபில்டர் போட்டாலும் பட்டியல் மாறவில்லை.** இந்தத் திரை இன்னும் நேரடி டேட்டாவுடன் இணைக்கப்படவில்லை. **Liquidation Cycle**-ல் உள்ள **Top 5 Performance** பயன்படுத்துங்கள்.
:::

::: section file-upload
title: IFMS Excel ரிப்போர்ட்களை அப்லோட் செய்வது
routes: /ExcelFormatFileUpload
for: 
roles: Admin
designations: 
tour: 1 > [data-help=sl-fu-grid] | 2 > [data-help=sl-fu-file] | 3 > [data-help=sl-fu-date] | 4 > [data-help=sl-fu-upload]
minutes: 3
source-hash: 3c1d667ca6804537
confidence: ok

**இந்தத் திரை எதற்கு**
தினசரி IFMS ரிப்போர்ட்களை ஏற்ற Admin **Excel Format File Upload** திரையைப் பயன்படுத்துவார். எல்லா ஸ்டாக் மற்றும் விற்பனைத் திரைகளும் இந்த டேட்டாவைத்தான் படிக்கின்றன. மெனுவில் **File Upload** அழுத்தி இதைத் திறங்கள்.

1. உங்கள் ரிப்போர்ட்டுக்கான கார்டைக் கண்டுபிடியுங்கள்: **Retail Stocks**, **Company Sales**, **Sales and Receipt**, **Wholesale Sales**, **Wholesale Stock As On Today**, **State Wise Global Stock Reconciliation** அல்லது **Warehouse and RakePoint Global Stock**.
2. **Excel File**-ன் கீழ், IFMS-லிருந்து நீங்கள் பதிவிறக்கிய கோப்பைத் தேர்வு செய்யுங்கள்.
3. கார்டில் **Report Date** இருந்தால், அது ரிப்போர்ட்டின் தேதிதானா என்று சரிபாருங்கள். அது இன்றைய தேதியில் தொடங்கும்.
4. **Upload** அழுத்தி, **Uploading...** என்று காட்டும்போது காத்திருங்கள். பிறகு மொத்த வரிசைகள், எத்தனை சேர்க்கப்பட்டன, புதுப்பிக்கப்பட்டன, விடப்பட்டன என்று ஒரு செய்தி காட்டும்.

> Tip: ஒவ்வொரு கார்டும் ஒரு ரிப்போர்ட்டுக்கானது. உங்கள் IFMS ரிப்போர்ட்டின் பெயருடன் பொருந்தும் கார்டைத் தேர்வு செய்யுங்கள்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Excel File** | ஒரே ஒரு .xlsx அல்லது .csv கோப்பு, 100 MB வரை. |
| **Report Date** | ரிப்போர்ட் எந்தத் தேதிக்கானது. Retail Stocks, Sales and Receipt, State Wise Global Stock Reconciliation மற்றும் Warehouse and RakePoint Global Stock-க்குத் தேவை. |

## பொதுவான பிரச்சனைகள்

- **"Please select a file first."** (முதலில் ஒரு கோப்பைத் தேர்வு செய்யுங்கள்) அதே கார்டில் கோப்பைத் தேர்வு செய்து, பிறகு **Upload** அழுத்துங்கள்.
- **"Only .xlsx and .csv files are supported."** (.xlsx, .csv கோப்புகள் மட்டுமே ஏற்கப்படும்) கோப்பை .xlsx அல்லது .csv ஆகச் சேமித்து மீண்டும் முயற்சி செய்யுங்கள்.
- **"The selected file exceeds the 100 MB upload limit."** (தேர்வு செய்த கோப்பு 100 MB வரம்பை விடப் பெரியது) கோப்பைச் சிறிய கோப்புகளாகப் பிரியுங்கள்.
- **Upload Failed.** (அப்லோட் தோல்வியடைந்தது) செய்தியைப் படியுங்கள். அந்தக் கார்டுக்கான சரியான ரிப்போர்ட்தானா என்று சரிபார்த்து, மீண்டும் முயற்சி செய்யுங்கள்.
:::
