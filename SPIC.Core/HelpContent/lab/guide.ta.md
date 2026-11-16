---
module: lab
title: SAS மண் பரிசோதனை லேப்
icon: bi-eyedropper
order: 60
language: ta
category: மண் பரிசோதனை
note: புதிய தோற்றம்: லேபில் மாதிரியின் பயணம், நிலைகள் மற்றும் படிப்படியான வழிகாட்டல்
meta-source-hash: 3e2288cb66a7506f
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: f1de7a25d0e196a5
confidence: ok

ஒவ்வொரு மண் மற்றும் தண்ணீர் மாதிரியும் லேபுக்கு வந்த நாள் முதல் விவசாயிக்கு அறிக்கை கிடைக்கும் வரை லேப் போர்ட்டல் பின்தொடர்கிறது. ஒவ்வொருவரும் தங்கள் வேலைக்கான திரைகளை மட்டுமே பார்ப்பார்கள்.

- receive | icon: bi-box-arrow-in-down | color: #0d9488
  title: பெற்று batch ஆக்குதல்
  text: ஒருங்கிணைப்பாளர் ஒவ்வொரு பார்சலையும் பெற்று, மாதிரிகளை ஒரு அனலிஸ்டுக்கான batch-களாகப் பிரிப்பார்.
- test | icon: bi-eyedropper | color: #7c3aed
  title: பரிசோதித்து மதிப்புகளை உள்ளிடுதல்
  text: அனலிஸ்ட் பரிசோதனை மதிப்புகளை டைப் செய்வார். முடிவுகளையும் ஆலோசனையையும் சிஸ்டம் கணக்கிடும்.
- report | icon: bi-file-earmark-pdf | color: #16a34a
  title: பல மொழிகளில் அறிக்கைகள்
  text: முடிந்த batch-களில் ஒவ்வொரு மாதிரிக்கும் ஒரு அறிக்கை கிடைக்கும், ஆங்கிலத்திலும் இந்திய மொழிகளிலும்.
:::

::: section flow
title: ஒரு consignment லேபில் எப்படி நகர்கிறது
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 998b226207ae8a8b
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- field | type: lane | color: #64748b | roles: MDO, JMDO
  title: களப் பணியாளர்கள்
  sub: MDO · JMDO
- coordinator | type: lane | color: #0d9488
  title: லேப் ஒருங்கிணைப்பாளர்
  sub: பெற்று batch ஆக்குவார்
- analyst | type: lane | color: #7c3aed
  title: லேப் அனலிஸ்ட்
  sub: பரிசோதனை மதிப்புகளை உள்ளிடுவார்
- farmer | type: lane | color: #16a34a | roles: Farmer
  title: விவசாயி
  sub: அறிக்கையைப் பெறுவார்
- send | lane: field | col: 1 | icon: bi-truck | color: #64748b | next: receive
  title: பார்சலை அனுப்புதல்
  status: In Transit
  text: களப் பணியாளர்கள் மாதிரிகளைக் கூரியர் மூலம் ஒரு consignment ஆக லேபுக்கு அனுப்புவார்கள்.
- receive | lane: coordinator | col: 2 | icon: bi-box-arrow-in-down | color: #0d9488 | next: batch | screen: receive-consignments
  title: பெற்றதாகக் குறித்தல்
  status: Batch Pending
  text: பார்சல் வந்ததும், ஒருங்கிணைப்பாளர் **Mark received** அழுத்துவார்.
- batch | lane: coordinator | col: 3 | icon: bi-collection | color: #ea580c | next: test | screen: create-batch
  title: Batch உருவாக்குதல்
  status: Taken for Analysis
  text: ஒருங்கிணைப்பாளர் consignment-களை ஒரு batch-ல் சேர்த்து, அதை ஒரு அனலிஸ்டுக்கு ஒதுக்குவார்.
- test | lane: analyst | col: 4 | icon: bi-eyedropper | color: #7c3aed | next: complete | screen: enter-test-values
  title: பரிசோதனை மதிப்புகளை உள்ளிடுதல்
  status: Auto Result Ready
  text: அனலிஸ்ட் ஒவ்வொரு மாதிரியின் மதிப்புகளையும் டைப் செய்து **Submit Values** அழுத்துவார். முடிவு, நிலை, ஆலோசனை தானாக நிரம்பும்.
- complete | lane: coordinator | col: 5 | icon: bi-patch-check | color: #2563eb | next: report | screen: batch-details
  title: Batch-ஐ முடித்தல்
  status: Report Generated
  text: ஒருங்கிணைப்பாளர் **Complete Batch** அழுத்துவார். மதிப்புகள் பூட்டப்படும், ஒவ்வொரு மாதிரிக்கும் ஒரு அறிக்கை உருவாகும்.
- report | lane: farmer | col: 6 | icon: bi-file-earmark-arrow-down | color: #16a34a
  title: விவசாயிக்கு அறிக்கை
  status: Completed
  text: அறிக்கை விவசாயிக்காக PDF அல்லது Excel ஆகப் பதிவிறக்கப்படும். விவசாயியும் ஆப்பில் அதைப் பதிவிறக்கலாம்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 670249fe2e3bdb4d
confidence: ok

- in-transit | color: #64748b
  label: In Transit
  text: பார்சல் லேபுக்கு வரும் வழியில் உள்ளது.
- batch-pending | color: #d97706
  label: Batch Pending
  text: பெறப்பட்டது, batch-ல் சேர்க்கக் காத்திருக்கிறது.
- batch-created | color: #0891b2
  label: Batch Created
  text: Batch-ல் உள்ளது, இன்னும் அனலிஸ்ட் ஒதுக்கப்படவில்லை.
- taken | color: #ea580c
  label: Taken for Analysis
  text: ஒரு அனலிஸ்டுக்கு ஒதுக்கப்பட்டது.
- pending-entry | color: #d97706
  label: Pending Entry
  text: அனலிஸ்ட் இன்னும் மதிப்புகளை உள்ளிடவில்லை.
- in-progress | color: #7c3aed
  label: In Progress
  text: சில மதிப்புகள் சேமிக்கப்பட்டுள்ளன.
- auto-result | color: #2563eb
  label: Auto Result Ready
  text: எல்லா மதிப்புகளும் சமர்ப்பிக்கப்பட்டன; முடிவுகள் கணக்கிடப்பட்டன.
- report | color: #16a34a
  label: Report Generated
  text: Batch முடிந்தது, அறிக்கைகள் தயார்.
- delayed | color: #dc2626
  label: Delayed
  text: பகுப்பாய்வு முடியாமல் 5 நாட்களுக்கு மேல் ஆகிவிட்டது.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: a82220da9f08df6e
confidence: ok

உங்கள் பணிப்பொறுப்புக்குப் பொருந்தும் கார்டு தனியாக ஒளிர்ந்து தெரியும்.

- coordinator | color: #0d9488
  title: லேப் ஒருங்கிணைப்பாளர்
  - Consignment-களைப் பெறுவது
  - Batch-கள் உருவாக்கி அனலிஸ்டுகளை ஒதுக்குவது
  - Batch-களை முடித்து அறிக்கைகளைப் பதிவிறக்குவது
- analyst | color: #7c3aed
  title: லேப் அனலிஸ்ட்
  - உங்கள் batch-களுக்குப் பரிசோதனை மதிப்புகளை உள்ளிடுவது
  - மாதிரி அறிக்கைகளைப் பதிவிறக்குவது
- admin | color: #2563eb | roles: Admin
  title: லேப் அட்மின்
  - Lab Tracking-ல் எல்லா batch-களையும் கண்காணிப்பது
  - பார்க்க மட்டும் முடியும்
- field | color: #64748b | roles: MDO, JMDO
  title: களப் பணியாளர்கள்
  - மாதிரிகளை லேபுக்கு அனுப்புவது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: d8c921211d44aab6
confidence: ok

- no-lab-menu | popular: true
  q: எனக்கு லேப் மெனு தெரியவில்லை
  a: உங்கள் பதவிக்கு (designation) லேப் பக்கங்கள் இருந்தால் மட்டுமே லேப் திரைகள் திறக்கும். Admin-க்கும் இது தேவை. உங்கள் நிர்வாகியிடம் (administrator) கேளுங்கள்.
- no-receive
  q: Mark received பட்டன் இல்லை
  a: ஒருங்கிணைப்பாளர்கள் மட்டுமே பெற முடியும், அதுவும் **In Transit** நிலையில் உள்ள consignment-களை மட்டுமே.
- delayed | popular: true
  q: அனலிஸ்ட் முடித்துவிட்டார், ஆனால் batch Delayed என்று காட்டுகிறது
  a: எல்லா மாதிரிகளும் சமர்ப்பிக்கப்பட்டு பகுப்பாய்வு முடியும் வரை அது தாமதம் (Delayed) என்றே காட்டும்.
- edit-values
  q: சமர்ப்பித்த பிறகு ஒரு மதிப்பை மாற்ற முடியுமா?
  a: முடியும். மாதிரியில் **Edit Values** அழுத்துங்கள். Batch முடியும் வரை மட்டுமே இது வேலை செய்யும்.
:::

::: section coordinator-dashboard
title: லேப் ஒருங்கிணைப்பாளர் டாஷ்போர்டு
routes: /Lab
for: 
roles: 
designations: 
screenshot: coordinator-dashboard.png
screenshot-phone: coordinator-dashboard.phone.jpg
minutes: 1
source-hash: edeeb0d1e27dd744
confidence: ok

**இந்தத் திரை எதற்கு**
**Lab Coordinator Dashboard** லேபின் நிலையை ஒரே பார்வையில் காட்டும்: எத்தனை consignment-கள் வந்தன, எத்தனை இன்னும் batch-க்காகக் காத்திருக்கின்றன, பகுப்பாய்வு எவ்வளவு தூரம் முடிந்துள்ளது. நீங்கள் **Lab**-ஐத் திறக்கும்போது, ஒருங்கிணைப்பாளர்களுக்கு ஆப் இந்த டாஷ்போர்டைக் காட்டும். அனலிஸ்டுகள் அவர்களுக்கான டாஷ்போர்டுக்குப் போவார்கள்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Consignments Received**, **Pending Batch Creation**, **Total Batches Created**, **Taken for Analysis**, **Analysis Completed**, **Completed Batches** மற்றும் **Reports Generated**.
2. **Recent Consignments** அட்டவணை கடைசி ஐந்து consignment-களை அவற்றின் **Status**-உடன் (**Batch Pending** அல்லது **Batch Created**) காட்டும்.
3. அந்த consignment-ஐத் திறக்க, consignment எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள்.
4. முழு **Consignment Details** பட்டியலைத் திறக்க, **Recent Consignments**-ன் பக்கத்தில் உள்ள **View All** அழுத்துங்கள்.
5. **Recently Created Batches** அட்டவணை கடைசி ஐந்து batch-களைக் காட்டும்: **Assigned To**-ல் அனலிஸ்ட், **Current Status**, **Analysis Days**.
6. **Analysis Tracking**-ஐத் திறக்க, **Recently Created Batches**-ன் பக்கத்தில் உள்ள **View All** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No batches yet" என்று வருகிறது.** இன்னும் எந்த batch-ம் உருவாக்கப்படவில்லை. Batch Pending consignment-களைப் பார்க்க **Go to consignments** அழுத்துங்கள்.
- **"The dashboard could not be loaded" என்று வருகிறது.** இணைய இணைப்பைச் சரிபார்த்து **Try again** அழுத்துங்கள்.
- **என்னை முதன்மை Dashboard-க்கு அனுப்பிவிட்டது.** உங்கள் பதவிக்கு ஒருங்கிணைப்பாளர் அல்லது அனலிஸ்ட் லேப் பக்கங்கள் இல்லை. உங்கள் நிர்வாகியிடம் கேளுங்கள்.
:::

::: section receive-consignments
title: லேபில் consignment-களைப் பெறுவது
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
screenshot: receive-consignments.png
screenshot-phone: receive-consignments.phone.jpg
minutes: 2
source-hash: 93cb533d61543543
confidence: ok

**இந்தத் திரை எதற்கு**
**Consignment Details** திரையில் லேபுக்கு அனுப்பிய எல்லா consignment-களும் இருக்கும். ஒரு பார்சலைப் பெற்றதாகக் குறிக்கவும், அதற்குள் என்ன இருக்கிறது என்று பார்க்கவும் இதைப் பயன்படுத்துங்கள்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Consignments**, **Pending Batch Creation**, **Batched Consignments**, **Water Samples** மற்றும் **Soil Samples**.
2. ஒரு consignment-ஐக் கண்டுபிடிக்க, தேடல் பெட்டியில் consignment எண் அல்லது AWB எண்ணை டைப் செய்யுங்கள், அல்லது **All Statuses**-ல் ஒரு நிலையைத் தேர்வு செய்யுங்கள்.
3. பார்சல் வந்ததும், **In Transit** நிலையில் உள்ள அதன் வரிசையைக் கண்டுபிடியுங்கள்.
4. **Action** நெடுவரிசையில் உள்ள receive ஐகானைத் தட்டுங்கள் (போனில் **Mark received** அழுத்துங்கள்).
5. கேள்விப் பெட்டியில் மீண்டும் **Mark received** அழுத்துங்கள். நிலை **Batch Pending** ஆக மாறும், அதன் மாதிரிகள் batch-க்குத் தயாராகும்.
6. முழு விவரங்களைப் பார்க்க, consignment எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள்.
7. விவரங்கள் திரையில் மாதிரி எண்ணிக்கைகள், State, Region, **Head Quarters**, **History / Timeline**, **Collections**, **Samples**, **Courier & Package** விவரங்கள் ஆகியவை தெரியும்.
8. அனுப்பும்போது எடுத்த புகைப்படங்களைப் பார்க்க **Consignment Photos** அழுத்துங்கள். அந்த மாதிரி சேகரிப்பைத் திறக்க ஒரு **Collection No.**-ஐத் தட்டுங்கள்.
9. Consignment ஏற்கனவே ஒரு batch-ல் இருந்தால், அந்த batch-ஐத் திறக்க **View Batch Details** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Mark received பட்டன் இல்லை.** ஒருங்கிணைப்பாளர்கள் மட்டுமே பெற முடியும், அதுவும் **In Transit** நிலையில் உள்ள consignment-களை மட்டுமே.
- **"No consignments found" என்று வருகிறது.** உங்கள் தேடல் அல்லது நிலை ஃபில்டர் ரொம்பக் குறுகலாக உள்ளது. **Clear filters** அழுத்துங்கள்.
- **Consignment Photos சாம்பல் நிறத்தில் உள்ளது.** பார்சல் அனுப்பும்போது புகைப்படம் எதுவும் சேர்க்கப்படவில்லை.
:::

::: section create-batch
title: Consignment-களில் இருந்து batch உருவாக்குவது
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: dcc40dda2df92781
confidence: ok

**இந்தத் திரை எதற்கு**
Batch என்பது ஒரு அனலிஸ்ட் ஒன்றாகப் பரிசோதிக்கும் மாதிரிகளின் குழு. ஒரு batch-ல் பல consignment-கள் இருக்கலாம். அதை **Consignment Details** பட்டியலில் இருந்து உருவாக்குவீர்கள்.

1. **Consignment Details**-ஐத் திறங்கள். Batch-க்காகக் காத்திருக்கும் consignment-களை மட்டும் பார்க்க, **All Statuses**-ல் **Batch Pending** தேர்வு செய்யுங்கள்.
2. Batch-ல் சேர்க்க வேண்டிய ஒவ்வொரு consignment-ன் தொடக்கத்தில் உள்ள பெட்டியை டிக் செய்யுங்கள். மேலே உள்ள பெட்டி பக்கத்தில் உள்ள எல்லா Batch Pending வரிசைகளையும் டிக் செய்யும்.
3. **Create Batch (n)** அழுத்துங்கள். நீங்கள் எத்தனை டிக் செய்தீர்கள் என்று அந்த எண் காட்டும். ஒரு consignment-ன் சொந்தத் திரையில் இருந்தால், அதற்குப் பதிலாக **Create Batch** அழுத்துங்கள்.
4. **Create Batch** படிவத்தை நிரப்பி (கீழே பாருங்கள்) **Confirm** அழுத்துங்கள்.
5. **Batch Created Successfully** பெட்டி புதிய batch எண்ணைக் காட்டும். Batch-ஐத் திறக்க **View Batch Details** அழுத்துங்கள், அல்லது எல்லா batch-களையும் பார்க்க **Go To Analysis Tracking** அழுத்துங்கள்.

## புலங்களின் விளக்கம்

- **Batch No**: நீங்கள் Confirm அழுத்திய பிறகு சிஸ்டம் உருவாக்கும்.
- **Batch Created Date**: இயல்பாக இன்றைய தேதி. எதிர்காலத் தேதி அனுமதி இல்லை.
- **Consignment No.**: டிக் செய்த consignment-கள் சிப்களாகக் காட்டப்படும். ஒன்றை நீக்க அந்தச் சிப்பில் உள்ள சிறிய பெருக்கல் குறியை அழுத்துங்கள், அல்லது வேறு ஒரு Batch Pending consignment-ஐச் சேர்க்க பெட்டியில் டைப் செய்யுங்கள்.
- **Assign To**: மாதிரிகளைப் பரிசோதிக்கும் அனலிஸ்ட். இப்போதே தேர்வு செய்தால், batch நேரடியாக **Taken for Analysis** ஆகும். காலியாக விட்டுப் பிறகும் ஒதுக்கலாம்.
- **Priority**: **Low**, **Medium** (இயல்பு) அல்லது **High**. அனலிஸ்டுக்கு இந்த முன்னுரிமை தெரியும்.
- **Remarks**: batch-க்கான ஏதாவது குறிப்பு.

## பொதுவான பிரச்சனைகள்

- **"Pick at least one consignment."** (குறைந்தது ஒரு consignment-ஐத் தேர்வு செய்யுங்கள்) Confirm அழுத்தும் முன் குறைந்தது ஒரு consignment-ஐச் சேருங்கள்.
- **டிக் பெட்டிகள் இல்லை.** **Batch Pending** வரிசைகளை மட்டுமே டிக் செய்ய முடியும், அந்தப் பெட்டிகள் ஒருங்கிணைப்பாளர்களுக்கு மட்டுமே தெரியும்.
:::

::: section analysis-tracking
title: பகுப்பாய்வில் உள்ள batch-களைப் பின்தொடர்வது
routes: /Lab/Analysis
for: 
roles: 
designations: 
screenshot: analysis-tracking.png
hotspots: 1 @ 3.7,13.6,92.5,21.5 | 2 @ 16.1,39.1,78.2,5.3 | 3 @ 76.5,39.1,17.8,5.3 | 4 @ 5.7,46,88.6,54 | 5 @ 5.7,46,88.6,54
screenshot-phone: analysis-tracking.phone.jpg
hotspots-phone: 1 @ 6.2,7.4,87.7,27.5 | 2 @ 10,39.1,80,6.8 | 3 @ 10,42.8,80,3 | 4 @ 2,2,8,4 | 5 @ 2,2,8,4
tour: 1 > [data-help=lab-at-cards] | 2 > [data-help=lab-at-search] | 3 > [data-help=lab-at-status] | 4 > [data-help=lab-at-table] | 5 > [data-help=lab-at-table]
minutes: 2
source-hash: 38d1f11b380940ea
confidence: ok

**இந்தத் திரை எதற்கு**
**Analysis Tracking** திரையில் எல்லா batch-களும் இருக்கும். ஒவ்வொரு batch-ஐயும் உருவாக்கியது முதல் பகுப்பாய்வு முடியும் வரை பின்தொடரலாம், தாமதமான batch-களைக் கண்டுபிடிக்கலாம்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Batches**, **Created**, **Taken for Analysis**, **Analysis Completed**, **Completed** மற்றும் **Delayed Analysis**.
2. தேடல் பெட்டியில் batch எண், consignment எண் அல்லது அனலிஸ்ட் பெயரை டைப் செய்யுங்கள்.
3. **All Statuses**-ல் ஒரு நிலையைத் தேர்வு செய்யுங்கள். தாமதமான batch-களை மட்டும் பார்க்க **Delayed** தேர்வு செய்யுங்கள்.
4. எந்த batch-கள் நேரத்துக்குள் உள்ளன என்று பார்க்க **Analysis Days** நெடுவரிசையைப் படியுங்கள் (கீழே உள்ள அட்டவணையைப் பாருங்கள்).
5. **Batch Details**-ஐத் திறக்க batch எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள். மேலும் batch-களைப் பார்க்க கீழே உள்ள பக்க எண்களைப் பயன்படுத்துங்கள்.

## Analysis Days விளக்கம்

| காட்டுவது | பொருள் |
|---|---|
| **Pending** (மஞ்சள்) | பகுப்பாய்வு இன்னும் தொடங்கவில்லை. |
| பச்சை நிறத்தில் நாட்களின் எண்ணிக்கை | பகுப்பாய்வு நேரத்துக்குள் நடக்கிறது. |
| சிவப்பு நிறத்தில் நாட்களின் எண்ணிக்கை | Batch தாமதமாகிவிட்டது (பொதுவாக 5 நாட்களுக்கு மேல் முடியாமல் இருந்தால்). |

## பொதுவான பிரச்சனைகள்

- **"No batches found" என்று வருகிறது.** நிலையை அல்லது தேடலை மாற்றுங்கள், அல்லது **Clear filters** அழுத்துங்கள். Batch எதுவுமே இல்லை என்றால், **Go to consignments** அழுத்தி ஒன்றை உருவாக்குங்கள்.
- **அனலிஸ்ட் முடித்துவிட்டார், ஆனால் batch Delayed என்று காட்டுகிறது.** எல்லா மாதிரிகளும் சமர்ப்பிக்கப்பட்டு பகுப்பாய்வு முடியும் வரை அது தாமதம் என்றே காட்டும்.
:::

::: section batch-details
title: Batch-ஐ ஒதுக்குவது, பின்தொடர்வது, முடிப்பது
routes: /Lab/Analysis/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: 16c6bd2291162e28
confidence: ok

**இந்தத் திரை எதற்கு**
**Batch Details** திரை ஒரு batch-ன் எல்லா விவரங்களையும் காட்டும்: அதன் மாதிரிகள், பரிசோதனை அளவுருக்கள் (parameters), ஆவணங்கள், வரலாறு. ஒருங்கிணைப்பாளர்கள் இங்கேயே அனலிஸ்டை ஒதுக்கி batch-ஐ முடிப்பார்கள்.

1. மேலே உள்ள பட்டையில் **Batch No.**, **Consignment No.**, **Total Samples**, **Assigned Date**, **Current Status**, **Analysis Days** தெரியும்.
2. டேப்களைப் பயன்படுத்துங்கள்: **Overview** (முன்னேற்றம், மாதிரிச் சுருக்கம், timeline), **Samples**, **Parameters**, **Documents** மற்றும் **Activity Log**.
3. Batch-ஐ ஒரு அனலிஸ்டுக்குக் கொடுக்க **Assign** அழுத்துங்கள் (அனலிஸ்டை மாற்ற **Reassign**). **Assign To** மற்றும் **Priority** தேர்வு செய்து, **Save** அழுத்துங்கள்.
4. Batch-க்கு அனலிஸ்ட் இருந்தும் இன்னும் தொடங்கவில்லை என்றால், **Take for Analysis** அழுத்தி உறுதி செய்யுங்கள்.
5. **Samples** டேபில், ஒரு மாதிரியின் பரிசோதனை மதிப்புகளைப் பார்க்க அதைத் தட்டுங்கள். பட்டியலைப் பதிவிறக்க **Export** அழுத்துங்கள்.
6. **Documents** டேபில், **Upload** அழுத்தி, **Files**, **Document Type**, ஒரு **Description** தேர்வு செய்து, மீண்டும் **Upload** அழுத்துங்கள். ஆவணத்தைப் பதிவிறக்க, பார்க்க அல்லது நீக்க ஐகான்களைப் பயன்படுத்துங்கள்.
7. எல்லா மாதிரிகளும் சமர்ப்பிக்கப்பட்டதும், "Every sample is completed" (எல்லா மாதிரிகளும் முடிந்தன) என்று ஒரு குறிப்பு வரும். **Complete Batch** அழுத்துங்கள்.
8. **Final Remarks** டைப் செய்து **Complete Batch** அழுத்துங்கள். மதிப்புகள் பூட்டப்பட்டு அறிக்கைகள் உருவாகும்.

## பொதுவான பிரச்சனைகள்

- **Complete Batch பட்டன் இல்லை.** சில மாதிரிகள் இன்னும் சமர்ப்பிக்கப்படவில்லை. **Samples** டேபில் **Not Started** அல்லது **In Progress** உள்ளதா என்று பாருங்கள்.
- **பட்டன்கள் இல்லை.** Batch ஏற்கனவே **Completed** ஆகிவிட்டது, அல்லது உங்களுக்கு ஒருங்கிணைப்பாளர் உரிமை இல்லை.
- **ஒரு கோப்பு சேர்க்கப்படவில்லை.** கோப்புகள் PDF, JPG, PNG, WEBP, XLSX அல்லது DOCX ஆக இருக்க வேண்டும், ஒவ்வொன்றும் 20 MB வரை, ஒரு முறைக்கு 10 கோப்புகள் வரை.
:::

::: section lab-reports
title: லேப் அறிக்கைகளைப் பதிவிறக்குவது
routes: /Lab/Reports
for: 
roles: 
designations: 
screenshot: lab-reports.png
screenshot-phone: lab-reports.phone.jpg
minutes: 2
source-hash: bbe4683cfa84f8af
confidence: ok

**இந்தத் திரை எதற்கு**
Batch முடிந்ததும் உருவாகும் அறிக்கைகள் **Lab Reports** திரையில் இருக்கும். இங்கிருந்து ஒரு அறிக்கையின் விவரங்களைத் திறக்கலாம், விவசாயி படிக்கும் மொழியில் PDF அல்லது Excel ஆகப் பதிவிறக்கலாம்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Reports**, **Batch Reports**, **Sample Reports**, **Generated Today** மற்றும் **Downloads Completed**.
2. பட்டியலைச் சுருக்க **Filters** அழுத்துங்கள். **From** மற்றும் **To** தேதிகள், **State**, **Sample Type** அல்லது **Financial Year** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
3. தேடல் பெட்டியில் batch, consignment அல்லது அறிக்கை எண்ணை டைப் செய்யுங்கள்.
4. **Lab Report Details**-ஐத் திறக்க கண் ஐகானைத் தட்டுங்கள் (போனில் **View Report**).
5. விவரங்களில் **Report Summary**, **Batch Details**, **Consignment Details**, **Analysis Details**, **Status Timeline** ஆகியவை தெரியும்.
6. Batch அறிக்கையை ஆங்கிலத்தில் பெற **Download PDF** அழுத்துங்கள். வேறு மொழியில் பெற, அதன் பக்கத்தில் உள்ள சிறிய அம்புக்குறியை அழுத்தி, பட்டியலில் **Tamil**, **Telugu**, **Marathi** அல்லது வேறு மொழியைத் தேர்வு செய்யுங்கள்.
7. அதே போல **Download Excel** அழுத்துங்கள். ஒவ்வொரு மாதிரி அறிக்கைக்கும் ஒரு Excel கோப்பு கிடைக்கும்.
8. பட்டியலுக்குத் திரும்ப **Close** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No reports found" என்று வருகிறது.** Batch முடிந்த பிறகுதான் அறிக்கைகள் வரும். உங்கள் ஃபில்டர்களையும் சரிபாருங்கள்.
- **பதிவிறக்க பட்டன்கள் சாம்பல் நிறத்தில் உள்ளன.** அறிக்கை இன்னும் தயாராகவில்லை.
- **PDF ஆங்கிலத்தில் வந்தது.** இந்த அறிக்கைக்கு அந்த மொழி இன்னும் தயாராகாமல் இருக்கலாம். அதனால் ஆங்கிலப் பிரதி கொடுக்கப்பட்டது.
:::

::: section analyst-dashboard
title: லேப் அனலிஸ்ட் டாஷ்போர்டு
routes: /Lab/Analyst
for: 
roles: 
designations: 
screenshot: analyst-dashboard.png
screenshot-phone: analyst-dashboard.phone.jpg
minutes: 1
source-hash: 84e531434d1c8d93
confidence: ok

**இந்தத் திரை எதற்கு**
**Lab Analyst Dashboard** உங்களுக்கு ஒதுக்கப்பட்ட வேலையையும், முதலில் எதைச் செய்ய வேண்டும் என்பதையும் காட்டும். ஒரு அனலிஸ்ட் **Lab**-ஐத் திறக்கும்போது, ஆப் இந்தத் திரையைக் காட்டும்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Assigned Batches**, **Pending Value Entry** (மதிப்புகளுக்காகக் காத்திருக்கும் மாதிரிகள்), **Auto Result Ready** (முடிவு கணக்கிடப்பட்ட மாதிரிகள்), **Reports Generated** (இந்த நிதியாண்டு) மற்றும் **Previous FY Reports**.
2. **Today's Work Priority**-ஐப் பாருங்கள்:
- **Pending value entry batches** (High): மதிப்புகளுக்காகக் காத்திருக்கும் batch-களைப் பார்க்க **Entry Now** அழுத்துங்கள்.
- **Auto result ready for review** (Medium): முடிவுகளைச் சரிபார்க்க **Preview** அழுத்துங்கள்.
- **Generated reports for download** (Low): **View Reports** அழுத்துங்கள்.
3. அல்லது **Quick Actions** பயன்படுத்துங்கள்: **Go to Batch List**, **Continue Test Entry** (நீங்கள் கடைசியாக வேலை செய்த batch-ஐத் திறக்கும்), **View Test Reports** மற்றும் **View Previous FY Reports**.
4. சமீபத்திய எண்ணிக்கைகளைப் பெற **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **எல்லா எண்களும் பூஜ்ஜியமாக உள்ளன.** இன்னும் எந்த batch-ம் உங்களுக்கு ஒதுக்கப்படவில்லை. உங்கள் லேப் ஒருங்கிணைப்பாளரிடம் கேளுங்கள்.
- **"Your dashboard could not be loaded" என்று வருகிறது.** இணைப்பைச் சரிபார்த்து **Try again** அழுத்துங்கள்.
:::

::: section analyst-batch-list
title: எனக்கு ஒதுக்கிய batch-களைக் கண்டுபிடிப்பது
routes: /Lab/TestEntry
for: 
roles: 
designations: 
screenshot: analyst-batch-list.png
hotspots: 1 @ 2.2,9.8,95.5,17.9 | 2 @ 2.2,30.1,95.5,34.9 | 3 @ 75.2,69.6,20.6,4.8 | 4 @ 4.2,76.1,91.6,23.9
screenshot-phone: analyst-batch-list.phone.jpg
hotspots-phone: 1 @ 3.1,5.8,93.8,16.9 | 2 @ 3.1,23.7,93.8,3.5 | 3 @ 6.9,31.2,86.2,3 | 4 @ 2,2,8,4
tour: 1 > [data-help=lab-bl-cards] | 2 > [data-help=lab-bl-filters] | 3 > [data-help=lab-bl-search] | 4 > [data-help=lab-bl-table]
minutes: 2
source-hash: 42bf99c9e09c8175
confidence: ok

**இந்தத் திரை எதற்கு**
**Test Value Entry** திரையில் உங்களுக்கு ஒதுக்கிய batch-கள் இருக்கும். பரிசோதனை மதிப்புகளை உள்ளிடுவதை இங்கிருந்து தொடங்குங்கள் அல்லது தொடருங்கள்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Assigned Batches**, **Pending Value Entry**, **Auto Result Ready** மற்றும் **Report Generated**.
2. பட்டியலைச் சுருக்க, **Filters**-ஐத் திறந்து **Date Range**, **Sample Type**, **Status**, **Priority**, **Financial Year** அல்லது **Assigned By** தேர்வு செய்யுங்கள். **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
3. தேடல் பெட்டியில் batch அல்லது consignment எண்ணை டைப் செய்யுங்கள்.
4. **Assigned Batch List**-ல் **Pending Entries**, **Completed Entries**, **Priority**, **Status** ஆகியவற்றைப் பாருங்கள். **High** முன்னுரிமை batch-களை முதலில் செய்யுங்கள். Batch-ஐத் திறக்க ஒரு வரிசையை, அல்லது **Action** நெடுவரிசையில் உள்ள ஐகானைத் தட்டுங்கள்.

## Action ஐகானின் பொருள்

| ஐகான் | எப்போது தெரியும் |
|---|---|
| **Start entry** | நிலை **Pending Entry**, இன்னும் எதுவும் உள்ளிடவில்லை. |
| **Continue entry** | நிலை **In Progress**, சில மதிப்புகள் சேமிக்கப்பட்டுள்ளன. |
| **Preview results** | நிலை **Auto Result Ready**, எல்லா மதிப்புகளும் சமர்ப்பிக்கப்பட்டுவிட்டன. |
| **View** | நிலை **Report Generated**, batch மூடப்பட்டுவிட்டது. |

## பொதுவான பிரச்சனைகள்

- **ஒரு batch காணவில்லை.** அது வேறு அனலிஸ்டுக்கு ஒதுக்கப்பட்டிருக்கலாம், அல்லது ஒரு ஃபில்டர் அதை மறைக்கலாம். **Clear filters** அழுத்துங்கள்.
- **"Batches could not be loaded" என்று வருகிறது.** இணைப்பைச் சரிபார்த்து **Try again** அழுத்துங்கள்.
:::

::: section enter-test-values
title: ஒரு மாதிரிக்குப் பரிசோதனை மதிப்புகளை உள்ளிடுவது
routes: /Lab/TestEntry/{id}
for: 
roles: 
designations: 
minutes: 4
source-hash: d752cb5b39f927f4
confidence: ok

**இந்தத் திரை எதற்கு**
**Sample-wise Entry**-ல் ஒரு batch-ல் உள்ள ஒவ்வொரு மாதிரியின் பரிசோதனை மதிப்புகளையும் டைப் செய்வீர்கள். சாதாரண வரம்பு (normal range), முடிவு, நிலை, பரிந்துரை ஆகியவற்றை சிஸ்டம் தானாக நிரப்பும்.

1. மேலே உள்ள **Batch Summary**-ஐச் சரிபாருங்கள்.
2. **Samples in this Batch**-ல், வேலை செய்ய வேண்டிய மாதிரியின் ஐகானைத் தட்டுங்கள், அல்லது **Next pending sample** அழுத்துங்கள்.
3. **Selected Sample** விவரங்களைச் சரிபாருங்கள் (விவசாயி, கிராமம், பயிர்).
4. **Test Value Entry Table**-ல், ஒவ்வொரு மதிப்பையும் **Test Value**-ல் டைப் செய்யுங்கள். **Texture**-க்கு பட்டியலில் இருந்து தேர்வு செய்யுங்கள். **Auto-calculated** என்று குறிக்கப்பட்ட வரிசைகள் தானாக நிரம்பும்.
5. நீங்கள் டைப் செய்யும்போது **Result**, **Status**, **Recommendation Hint** மாறுவதைப் பாருங்கள். கீழே **Recommendation Preview** மற்றும் **Fertilizer Schedule (Kg/acre)**-ஐப் பாருங்கள்.
6. முடிக்காத வேலையை வைத்துக்கொள்ள **Save Draft** அழுத்துங்கள்.
7. எல்லா மதிப்புகளும் உள்ளிட்டதும், **Submit Values** அழுத்தி, பிறகு **Submit Test Values?** பெட்டியில் **Submit** அழுத்துங்கள்.
8. **Submitted Successfully**-ல், **Preview Report** அல்லது **Cancel** அழுத்தி, பிறகு அடுத்த மாதிரியைத் தேர்வு செய்யுங்கள்.
9. சமர்ப்பித்த மாதிரியைத் திருத்த **Edit Values** அழுத்துங்கள். Batch முடியும் வரை மட்டுமே இது வேலை செய்யும்.

## புலங்களின் விளக்கம்

- **Normal Range**: இந்தப் பரிசோதனைக்கான நல்ல வரம்பு.
- **Status**: உங்கள் மதிப்பைப் பொறுத்து **Normal**, **Deficient**, **Moderate** அல்லது **Excess**.
- **Overall Soil Status** (அல்லது **Overall Water Status**): எல்லா மதிப்புகளையும் பொறுத்து **Good**, **Needs Improvement** அல்லது **Poor**.

## பொதுவான பிரச்சனைகள்

- **"Enter every parameter before submitting."** (சமர்ப்பிக்கும் முன் எல்லா அளவுருக்களையும் உள்ளிடுங்கள்) செய்தியில் சொல்லப்பட்ட அளவுருக்களை நிரப்புங்கள்.
- **"Fix the values that are not numbers."** (எண் அல்லாத மதிப்புகளைச் சரி செய்யுங்கள்) இலக்கங்களையும் புள்ளியையும் மட்டுமே பயன்படுத்துங்கள்.
- **"Leave this sample?"** (இந்த மாதிரியை விட்டுப் போகவா?) உங்கள் மதிப்புகள் சேமிக்கப்படவில்லை. **Stay** அழுத்தி, பிறகு **Save Draft** அழுத்துங்கள்.
- **எல்லாம் பூட்டப்பட்டுள்ளது.** Batch முடிந்துவிட்டது. **View Report** அழுத்துங்கள்.
:::

::: section analyst-test-reports
title: பரிசோதனை அறிக்கைகள் மற்றும் முந்தைய ஆண்டு அறிக்கைகள்
routes: /Lab/TestEntry/Reports, /Lab/TestEntry/Reports/Previous
for: 
roles: 
designations: 
minutes: 2
source-hash: f0ab8dbc77d485c5
confidence: ok

**இந்தத் திரை எதற்கு**
**Test Reports** திரை இந்த நிதியாண்டில் (ஏப்ரல் முதல் மார்ச் வரை) உங்கள் ஒவ்வொரு batch-க்குமான அறிக்கைச் சுருக்கத்தைக் காட்டும். **Previous FY Reports** முந்தைய ஆண்டுகளுக்கு இதையே காட்டும்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Batches**, **Total Sample Reports**, **Reports Generated Today** மற்றும் **Downloads Completed**.
2. பழைய அறிக்கைகளைப் பார்க்க **Previous FY Reports** அழுத்துங்கள். திரும்ப வர **Current FY Reports** அழுத்துங்கள்.
3. பட்டியலைச் சுருக்க, **Filters**-ஐத் திறந்து **Financial Year**, **Date Range**, **Sample Type**, **State**, **Region** அல்லது **Head Quarters** தேர்வு செய்யுங்கள். **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
4. தேடல் பெட்டியில் batch அல்லது அறிக்கை எண்ணை டைப் செய்யுங்கள்.
5. **Batch-wise Report Summary**-ல் ஒவ்வொரு batch-க்கும் **Reports Generated** மற்றும் **Pending Reports**-ஐப் பாருங்கள்.
6. ஒவ்வொரு மாதிரியின் அறிக்கையும் உள்ள **Batch Report Details**-ஐத் திறக்க ஒரு வரிசையை, அல்லது **View reports** ஐகானைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No batch reports found" என்று வருகிறது.** ஒருங்கிணைப்பாளர் batch-ஐ முடித்த பிறகுதான் அறிக்கைகள் வரும். நிதியாண்டையும் ஃபில்டர்களையும் சரிபாருங்கள்.
- **"Reports are not available" என்று வருகிறது.** அறிக்கை சேவை பதில் தரவில்லை. சிறிது நேரம் கழித்து மீண்டும் முயற்சி செய்யுங்கள்.
- **ஒரு பழைய அறிக்கை காணவில்லை.** **Previous FY Reports**-ஐத் திறந்து அதன் ஆண்டைத் தேர்வு செய்யுங்கள்.
:::

::: section batch-report-details
title: ஒரு batch-ன் மாதிரி அறிக்கைகளைத் திறந்து பதிவிறக்குவது
routes: /Lab/TestEntry/Reports/{id}
for: 
roles: 
designations: 
minutes: 2
source-hash: 8c8efa3d44f215ee
confidence: ok

**இந்தத் திரை எதற்கு**
**Batch Report Details** ஒரு batch-ல் உள்ள ஒவ்வொரு மாதிரியின் அறிக்கையையும் பட்டியலிடும். ஒவ்வொரு மாதிரிக்கும் அதன் விவசாயிக்கான தனி அறிக்கை உண்டு. இங்கே அறிக்கைகளைப் பார்க்கலாம், பதிவிறக்கலாம், அச்சிட்டதாகக் குறிக்கலாம்.

1. கார்டுகளைப் பாருங்கள்: **Total Samples**, **Soil Samples**, **Water Samples**, **Reports Generated** மற்றும் **Downloads Completed**.
2. **Batch Information**-ஐச் சரிபாருங்கள்: batch, consignment, State, Region, **Head Quarters**, **Date of Dispatch** மற்றும் **Financial Year**.
3. ஒரு மாதிரியைக் கண்டுபிடிக்க **Sample Type**, **Status**, **Crops**, **Villages** ஃபில்டர்களைப் பயன்படுத்துங்கள்.
4. ஒரு மாதிரி அறிக்கையைத் திரையில் திறக்க **View** அழுத்துங்கள். அதில் முடிவுகள், பரிந்துரைகள், உர அட்டவணை ஆகியவை இருக்கும்.
5. ஒரு அறிக்கையை ஆங்கிலத்தில் சேமிக்க **Download PDF** அழுத்துங்கள். **Tamil**, **Telugu** அல்லது **Marathi** போன்ற வேறு மொழியைத் தேர்வு செய்ய அதன் பக்கத்தில் உள்ள சிறிய அம்புக்குறியை அழுத்துங்கள்.
6. Batch-ன் எல்லா அறிக்கைகளையும் ஒரே நேரத்தில் சேமிக்க **Download All Reports** அழுத்துங்கள். மொழியைத் தேர்வு செய்ய அதன் அம்புக்குறியைப் பயன்படுத்துங்கள்.
7. விவசாயிக்காக அறிக்கையை அச்சிட்ட பிறகு **Mark printed** அழுத்துங்கள். அதன் **Report Status** **Printed** ஆக மாறும்.

## புலங்களின் விளக்கம்

- **Report Status**: **Generated** (தயார்), **Downloaded** (குறைந்தது ஒரு முறை சேமிக்கப்பட்டது) அல்லது **Printed**.
- **Reports No.**: விவசாயியின் அறிக்கையில் அச்சிடப்படும் அறிக்கை எண்.

## பொதுவான பிரச்சனைகள்

- **"This batch report is not available" என்று வருகிறது.** Batch இன்னும் முடியவில்லை. மாதிரிகளைச் சரிபார்க்க **Open the batch** அழுத்துங்கள்.
- **Mark printed சாம்பல் நிறத்தில் உள்ளது.** அறிக்கை ஏற்கனவே **Printed** என்று குறிக்கப்பட்டுவிட்டது.
:::

::: section lab-tracking
title: அட்மின்களுக்கான லேப் கண்காணிப்பு
routes: /Lab/Tracking
for: 
roles: 
designations: 
minutes: 2
source-hash: 4713e37bae159d20
confidence: ok

**இந்தத் திரை எதற்கு**
**Lab Tracking** என்பது லேபில் உள்ள எல்லா batch-களுக்குமான அட்மின் பார்வை. Consignment-கள், batch-கள், பகுப்பாய்வு முன்னேற்றம் ஆகியவற்றைக் கண்காணிக்க இதைப் பயன்படுத்துங்கள். இங்கே எதையும் மாற்ற முடியாது.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Batches**, **Batch Created**, **Taken for analysis** மற்றும் **Analysis Completed**.
2. ஒரு நிலையை மட்டும் காட்ட டேப்களைப் பயன்படுத்துங்கள்: **All Batches**, **Batch Created**, **Taken for Analysis** அல்லது **Analysis Completed**. ஒவ்வொரு டேபும் அதன் எண்ணிக்கையைக் காட்டும்.
3. பட்டியலைச் சுருக்க, **Filters**-ஐத் திறந்து **Date Range**, **Batch No.**, **Consignment No.**, **Assigned To**, **State**, **Region** அல்லது **Current Stage** தேர்வு செய்யுங்கள். **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
4. **Lab Tracking List**-க்கு மேலே உள்ள தேடல் பெட்டியில் batch எண், consignment எண் அல்லது அனலிஸ்ட் பெயரை டைப் செய்யுங்கள்.
5. **Analysis Days**-ஐப் படியுங்கள்: **Pending** (மஞ்சள்) என்றால் தொடங்கவில்லை, பச்சை என்றால் நேரத்துக்குள், சிவப்பு என்றால் தாமதம்.
6. Batch கடைசியாக எப்போது நகர்ந்தது என்று பார்க்க **Last Updated**-ஐப் பாருங்கள்.
7. Batch பார்வையைத் திறக்க batch எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No batches found" என்று வருகிறது.** **Clear filters** அழுத்தி **All Batches** டேபைத் தேர்வு செய்யுங்கள்.
- **என் மெனுவில் Lab Tracking இல்லை.** உங்கள் பதவிக்கு Lab Tracking பக்கம் தேவை. Admin role மட்டும் போதாது. உங்கள் நிர்வாகியிடம் கேளுங்கள்.
:::

::: section batch-view
title: அட்மினாக ஒரு batch-ஐப் பார்ப்பது
routes: /Lab/Tracking/{id}
for: 
roles: 
designations: 
minutes: 1
source-hash: 56cb817a9b7e82f1
confidence: ok

**இந்தத் திரை எதற்கு**
Batch பார்வை, ஒரு batch-ன் முழுக் கதையையும் ஒரே பக்கத்தில் அட்மினுக்குக் காட்டும்: மாதிரிகள் எங்கிருந்து வந்தன, கூரியர், batch, பகுப்பாய்வு முன்னேற்றம், timeline. இதில் பார்க்க மட்டுமே முடியும்.

1. மேலே உள்ள பட்டையில் **Batch No.**, **Consignment No.**, **Test Samples**, **Samples**, **Assigned To (Analyst)**, **Batch Created On**, **Last Updated** தெரியும்.
2. **Source Details** மாதிரிகளைச் சேகரித்தவர் யார், அவரது **Mobile No.**, **Location**, **Collection Date** ஆகியவற்றைக் காட்டும்.
3. **Consignment Details** **Dispatch Date**, **AWB / Tracking No**, **Expected Delivery Date** ஆகியவற்றைக் காட்டும். கூரியர் தளத்தில் பார்சலைப் பின்தொடர டிராக்கிங் எண்ணைத் தட்டுங்கள்.
4. **Batch Details** மாதிரி எண்ணிக்கைகள், **Batch Created By**, **Batch Created Date**, **Assigned Analyst** ஆகியவற்றைக் காட்டும்.
5. **Analysis Process** ஒவ்வொரு மாதிரி வகைக்கும் எத்தனை மாதிரிகள் **Completed**, எத்தனை **Not Started** என்று, ஒரு **Completed %** பட்டையுடன் காட்டும்.
6. **Timeline** ஒவ்வொரு படியையும் தேதி மற்றும் செய்தவர் பெயருடன் காட்டும்: **Consignment Delivered to Lab**, **Lab Entry Created**, **Batch Created**, **Taken for Analysis**, **Analysis Completed** மற்றும் **Report Generated**. சாம்பல் நிறப் படிகள் இன்னும் நடக்கவில்லை.
7. Batch மாதிரிகளை Excel கோப்பாகப் பதிவிறக்க **Export** அழுத்துங்கள்.
8. பட்டியலுக்குத் திரும்ப **Back to Lab Tracking** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"The batch could not be loaded" என்று வருகிறது.** Batch நீக்கப்பட்டிருக்கலாம், அல்லது உங்களுக்கு Lab Tracking இல்லாமல் இருக்கலாம். **Try again** அழுத்துங்கள்.
:::
