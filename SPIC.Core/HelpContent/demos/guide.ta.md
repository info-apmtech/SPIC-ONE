---
module: demos
title: டெமோ மற்றும் டிரீட்மென்ட்
icon: bi-flower1
order: 108
language: ta
category: களப்பணி
note: புதிய வழிகாட்டி
meta-source-hash: c6011d305dce737c
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b0aaf57f4576cdfb
confidence: ok

விவசாயியின் வயலில் நடக்கும் பயிர் டெமோவை (செயல்விளக்கம்) தொடக்கம் முதல் முடிவு வரை பதிவு செய்யுங்கள். விவசாயி மற்றும் பயிர் விவரங்களை ஒரே ஒரு முறை சேமித்தால் போதும். பிறகு ஒவ்வொரு வயல் வருகையையும் ஒரு டிரீட்மென்ட்டாக, நீங்கள் கவனித்தவை மற்றும் புகைப்படங்களுடன் பதிவு செய்து, ஒவ்வொரு வருகையையும் முந்தைய வருகையுடன் ஒப்பிட்டுப் பாருங்கள்.

- start | icon: bi-pencil-square | color: #65a30d
  title: டெமோவைத் தொடங்குங்கள்
  text: ஒப்புதல் பெற்ற CSR-2 திட்டத்துக்கு விவசாயி, இடம், பயிர் விவரங்களை உள்ளிடுங்கள்.
- treat | icon: bi-camera | color: #d97706
  title: ஒவ்வொரு வருகையையும் பதிவு செய்யுங்கள்
  text: Product, அளவு (dosage), கவனித்தவை, control மற்றும் treated புகைப்படங்களைச் சேமியுங்கள்.
- compare | icon: bi-arrow-left-right | color: #16a34a
  title: ஒப்பிட்டுப் பார்த்து முடியுங்கள்
  text: அடுத்த டிரீட்மென்ட்டைப் பதிவு செய்யும்போது முந்தையதைப் பார்க்கலாம். எந்தப் பிரச்சனையும் இல்லாதபோது டெமோவை முடித்து வையுங்கள்.
:::

::: section flow
title: ஒரு டெமோ எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 672fd04ad6279036
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- csr | type: lane | color: #0891b2
  title: CSR குழு
  sub: CSR-1 மற்றும் CSR-2
- field | type: lane | color: #65a30d | roles: MDO, JMDO
  title: பொறுப்பாளர்
  sub: CSR-1-ல் பெயர் உள்ள களப் பணியாளர்
- eligible | lane: csr | col: 1 | icon: bi-patch-check | color: #64748b | next: start | screen: demo-list
  title: CSR-2 ஒப்புதல் பெற்றது
  status: Not Started
  text: CSR-1-ல் உங்களை **Responsible Person** (பொறுப்பாளர்) என்று குறிப்பிட்டிருப்பார்கள். அதன் CSR-2-க்கு இறுதி ஒப்புதல் கிடைத்ததும், அந்த டெமோ உங்கள் **Demo Documentation** பட்டியலில் தெரியும்.
- start | lane: field | col: 2 | icon: bi-pencil-square | color: #d97706 | next: treat | screen: start-documentation
  title: பதிவைத் தொடங்குதல்
  status: In Progress
  text: விவசாயி, இடம், பயிர் விவரங்களை உள்ளிட்டு **Start Documentation** அழுத்துங்கள்.
- treat | lane: field | col: 3 | icon: bi-camera | color: #64748b | next: close, followup | screen: record-treatment
  title: டிரீட்மென்ட்டைப் பதிவு செய்தல்
  status: Draft
  text: ஒவ்வொரு வயல் வருகையிலும் டிரீட்மென்ட், கவனித்தவை, புகைப்படங்களைப் பதிவு செய்யுங்கள். பிறகு முடிக்க **Save Draft** அழுத்துங்கள், அல்லது **Complete Treatment** அழுத்துங்கள்.
- followup | lane: field | col: 4 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: treat | to: treat | screen: compare-treatments
  title: பிரச்சனை உள்ளது, மீண்டும் வருகை
  status: In Progress
  text: **Problem Observed?**-ல் **Yes** வைத்து டிரீட்மென்ட்டை முடித்திருந்தால், டெமோ திறந்தே இருக்கும். அடுத்த வருகைக்கு **Start Next Treatment** அழுத்துங்கள்.
- close | lane: field | col: 5 | icon: bi-clipboard-check | color: #16a34a | screen: demo-details
  title: டெமோ முடிந்தது
  status: Completed
  text: **Problem Observed?**-ல் **No** வைத்து ஒரு டிரீட்மென்ட்டை முடித்தால், முழு டெமோவும் முடிந்துவிடும். அதன் பிறகு அதைப் பார்க்க மட்டுமே முடியும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: c13ab7e2ba0afb47
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: டெமோ அதன் முதல் வருகைக்காகக் காத்திருக்கிறது. தொடக்கப் படிவத்தை டிராஃப்டாகச் சேமித்திருந்தாலும் இதுவே தெரியும்.
- in-progress | color: #d97706
  label: In Progress
  text: பதிவு தொடங்கிவிட்டது. டிரீட்மென்ட்கள் பதிவு செய்யப்படுகின்றன.
- draft | color: #64748b
  label: Draft
  text: ஒரு டிரீட்மென்ட் சேமிக்கப்பட்டது, ஆனால் முடிக்கப்படவில்லை. அதை இன்னும் மாற்றலாம்.
- completed | color: #16a34a
  label: Completed
  text: டிரீட்மென்ட் என்றால், அது முடிந்துவிட்டது, அதை மாற்ற முடியாது. டெமோ என்றால், அது மூடப்பட்டுவிட்டது, அதைப் பார்க்க மட்டுமே முடியும்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 94984548531519c1
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். நீங்கள் பொறுப்பாளராக உள்ள டெமோக்கள் மட்டுமே உங்களுக்குத் தெரியும்.

- field | color: #65a30d | roles: MDO, JMDO
  title: பொறுப்பாளர்
  - ஒப்புதல் பெற்ற ஒவ்வொரு டெமோவுக்கும் பதிவைத் தொடங்குவது
  - ஒவ்வொரு வயல் வருகையையும் டிரீட்மென்ட்டாகப் பதிவு செய்வது
  - control மற்றும் treated புகைப்படங்களை அப்லோட் செய்வது
  - எந்தப் பிரச்சனையும் இல்லாதபோது டெமோவை முடித்து வைப்பது
- csr | color: #0891b2
  title: CSR குழு
  - CSR-1 உருவாக்கி, Responsible Person-ஐத் (பொறுப்பாளர்) தேர்வு செய்வது
  - டெமோவைப் பதிவு செய்ய முடியும்படி CSR-2-க்கு ஒப்புதல் தருவது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 03c835ce85e68364
confidence: ok

- not-listed | popular: true
  q: என் டெமோ பட்டியலில் இல்லை
  a: CSR-2-க்கு இறுதி ஒப்புதல் கிடைத்த, CSR-1-ல் நீங்கள் **Responsible Person** ஆக உள்ள திட்டங்கள் மட்டுமே பட்டியலில் தெரியும். CSR-1-ஐ உருவாக்கியவரிடம் அதைச் சரிபார்க்கச் சொல்லுங்கள்.
- no-next | popular: true
  q: எனக்கு Start Next Treatment தெரியவில்லை
  a: உங்கள் கடைசி டிரீட்மென்ட் **Completed** ஆகி, டெமோ மூடப்படாமல் இருந்தால் மட்டுமே அது தெரியும். கடைசி டிரீட்மென்ட் **Draft** ஆக இருந்தால், **View Details** மூலம் அதைத் திறந்து முதலில் அதை முடியுங்கள்.
- closed-early
  q: ஒரு டிரீட்மென்ட்டை முடித்ததும் டெமோ மூடிவிட்டது
  a: **Problem Observed?**-ல் **No** வைத்து டிரீட்மென்ட்டை முடித்தால், முழு டெமோவும் முடிந்துவிடும். முடிந்த டெமோவைப் பார்க்க மட்டுமே முடியும். அதனால் **Complete Treatment** அழுத்தும் முன் இந்தப் பதிலைச் சரிபாருங்கள்.
- photos
  q: என் புகைப்படங்கள் சேமிக்கப்படவில்லை
  a: **Save Draft** அல்லது **Complete Treatment** அழுத்தும்போதுதான் புகைப்படங்கள் அப்லோட் ஆகும். 10 MB அல்லது அதற்குக் குறைவான JPG அல்லது PNG கோப்புகளை, ஒரே நேரத்தில் 20 வரை பயன்படுத்துங்கள்.
- missing-id
  q: "Missing or invalid csr2Id in URL" என்று வருகிறது
  a: டெமோவைத் தேர்வு செய்யாமல் திரை திறக்கப்பட்டுள்ளது. **Demo Documentation**-ஐத் திறந்து, அந்த டெமோவின் **Action** நெடுவரிசையில் உள்ள பட்டனை அழுத்துங்கள்.
:::

::: section demo-list
title: என் டெமோக்களைப் பார்ப்பது
routes: /DemoDocumentation
for: 
roles: 
designations: 
minutes: 2
source-hash: 4947901ab12cde8b
confidence: ok

**இந்தத் திரை எதற்கு**
**Demo Documentation** திரையில் உங்களுக்கு ஒதுக்கப்பட்ட, ஒப்புதல் பெற்ற எல்லா CSR-2 திட்டங்களும் தெரியும். ஒவ்வொன்றும் எந்த நிலையில் உள்ளது, எத்தனை டிரீட்மென்ட்கள் முடிந்தன என்பதும் தெரியும்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Demonstrations**, **Not Started**, **In Progress** மற்றும் **Completed**.
2. **Filters**-ன் கீழ், **Sanction Number**, **Program Name**, **Product**, **Crop**, **District**, **Stage** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
3. Sanction எண், திட்டம், product, பயிர் அல்லது இடம் மூலம் டெமோவைக் கண்டுபிடிக்க **Search demos...** பெட்டியில் டைப் செய்யுங்கள்.
4. அட்டவணையில், ஒவ்வொரு டெமோவுக்கும் **Current Stage**, **Date of Sowing**, **Treatments**, **Last Treatment** மற்றும் **Document Status**-ஐப் படியுங்கள்.
5. **Action** நெடுவரிசையில், புதிய டெமோவுக்கு **Start Documentation**, நடந்துகொண்டிருக்கும் டெமோவுக்கு **View / Continue**, முடிந்த டெமோவுக்கு **View** அழுத்துங்கள்.
6. மேலும் வரிசைகளைப் பார்க்க கீழே உள்ள **Items Per Page** மற்றும் பக்க எண்களைப் பயன்படுத்துங்கள்.

> Tip: உங்களுக்கு ஒதுக்கப்பட்ட, ஒப்புதல் பெற்ற CSR-2 திட்டங்கள் மட்டுமே பட்டியலில் இருக்கும்.

## பொதுவான பிரச்சனைகள்

- **"No eligible demonstration records found".** (தகுதியான டெமோ எதுவும் இல்லை) ஒப்புதல் பெற்ற எந்த CSR-2-லும் இன்னும் நீங்கள் பொறுப்பாளராக இல்லை, அல்லது உங்கள் ஃபில்டர்கள் ரொம்பக் குறுகலாக உள்ளன. **Clear All** அழுத்துங்கள்.
- **"Unable to load approved CSR-2 demonstrations".** (ஒப்புதல் பெற்ற CSR-2 டெமோக்களைக் காட்ட முடியவில்லை) ஆப்பால் சர்வரை அடைய முடியவில்லை. இணைய இணைப்பைச் சரிபார்த்து, திரையை மீண்டும் திறங்கள்.
:::

::: section start-documentation
title: டெமோவுக்குப் பதிவைத் தொடங்குவது
routes: /StartDocumentation
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-sd-program] | 2 > [data-help=dm-sd-farmer] | 3 > [data-help=dm-sd-crop] | 4 > [data-help=dm-sd-actions]
minutes: 4
source-hash: a86f4d3929f78e21
confidence: ok

**இந்தத் திரை எதற்கு**
முதல் வருகையில், டெமோவின் விவசாயி, இடம், பயிர் விவரங்களைச் சேமிக்க **Start Documentation** திரையைப் பயன்படுத்துங்கள். பட்டியலில் **Start Documentation** அழுத்தி இதைத் திறங்கள்.

1. **Program Details (From CSR-2)**-ஐச் சரிபாருங்கள்: **Program Type**, **Program Name**, **Sanction Number**, **Actual Event Location** மற்றும் **Focus Products**.
2. **Farmer & Location Details**-ன் கீழ், **Farmer Name**, **Mobile Number**, **Place / Village** மற்றும் **Farmer Address**-ஐ உள்ளிடுங்கள். **Crop** மற்றும் **Crop Type** தேர்வு செய்து, **Pin Code** உள்ளிட்டு, பிறகு **State** மற்றும் **District** தேர்வு செய்யுங்கள்.
3. **Crop Information**-ன் கீழ், **Date Of Sowing** உள்ளிட்டு, **Current Crop Stage** தேர்வு செய்து, **Expected Harvest Date** உள்ளிட்டு, தேவைப்பட்டால் **Remarks** சேருங்கள்.
4. பிறகு முடிக்க **Save Draft** அழுத்துங்கள், அல்லது தொடங்க **Start Documentation** அழுத்துங்கள். **Start Documentation** அழுத்தினால் **Demo Details** திரை திறக்கும்.

> Careful: **Start Documentation** அழுத்திய பிறகு, பட்டியலிலிருந்து இந்தப் படிவத்துக்குப் பதிலாக **Demo Details** திறக்கும். தொடங்கும் முன் விவரங்களைச் சரிபாருங்கள்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Farmer Name** | டெமோ வயலின் உரிமையாளரான விவசாயி. கட்டாயம். |
| **Mobile Number** | விவசாயியின் 10 இலக்க மொபைல் எண். கட்டாயம். |
| **Crop** | இந்த CSR-2-ன் focus products-ல் உள்ள பயிர்கள் மட்டுமே தெரியும். கட்டாயம். |
| **Crop Type** மற்றும் **Current Crop Stage** | பயிரைத் தேர்வு செய்த பிறகு இவை வரும். கட்டாயம். |
| **Pin Code** | 6 இலக்கம். கட்டாயம். |
| **State** மற்றும் **District** | முதலில் State, பிறகு District தேர்வு செய்யுங்கள். கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"Select State, District, Crop, Crop Type and Current Crop Stage."** (State, District, Crop, Crop Type, Current Crop Stage தேர்வு செய்யுங்கள்) இவற்றில் ஏதோ ஒரு பட்டியல் காலியாக உள்ளது. ஒவ்வொன்றையும் தேர்வு செய்யுங்கள்.
- **"Enter farmer name, a 10-digit mobile number and a 6-digit PIN code."** (விவசாயி பெயர், 10 இலக்க மொபைல் எண், 6 இலக்க PIN code உள்ளிடுங்கள்) மொபைல் எண்ணையும் PIN code-ஐயும் சரிபாருங்கள்.
- **"This CSR-2 is not finally approved or is outside your permitted scope."** (இந்த CSR-2-க்கு இறுதி ஒப்புதல் இல்லை, அல்லது அது உங்கள் அனுமதி வரம்புக்கு வெளியே உள்ளது) இந்த டெமோ உங்களுக்கு ஒதுக்கப்படவில்லை. பட்டியலுக்குத் திரும்புங்கள்.
:::

::: section demo-details
title: ஒரு டெமோவையும் அதன் டிரீட்மென்ட்களையும் பார்ப்பது
routes: /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 7530f32fb5322456
confidence: ok

**இந்தத் திரை எதற்கு**
**Demo Details** திரையில் டெமோவின் சுருக்கம், விவசாயி விவரங்கள், **Treatment Journey** (பதிவான எல்லா டிரீட்மென்ட்களின் பட்டியல்) ஆகியவை தெரியும்.

1. பட்டியலில் **View / Continue** அல்லது **View** அழுத்தி டெமோவைத் திறங்கள்.
2. மேலே உள்ள கார்டைச் சரிபாருங்கள்: product, **Sanction Number**, **Crop**, **Date of Sowing:**, **Crop Stage:** மற்றும் **Expected Harvest:**.
3. விவசாயி கார்டைச் சரிபாருங்கள்: **Farmer**, **Mobile Number**, **District**, **State**, **Pin Code** மற்றும் **Location**.
4. இன்னும் எந்த டிரீட்மென்ட்டும் பதிவாகவில்லை என்றால், **Start First Treatment** அழுத்துங்கள்.
5. **Treatment Journey**-ல், ஒவ்வொரு டிரீட்மென்ட்டும் அதன் நிலை, **Visit Date**, **Plant Stage**, **Dosage** மற்றும் **Remarks**-ஐக் காட்டும். முடிந்தவற்றின் எண்ணிக்கையை **Treatments Done** காட்டும்.
6. ஒரு டிரீட்மென்ட்டைத் திறக்க அதில் **View Details** அழுத்துங்கள்.
7. கடைசி டிரீட்மென்ட் **Completed** ஆனதும், அடுத்த வருகைக்கு **Start Next Treatment** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Start Next Treatment பட்டன் இல்லை.** கடைசி டிரீட்மென்ட் இன்னும் **Draft** ஆக உள்ளது, அல்லது டெமோ **Completed** ஆகிவிட்டது.
- **"Farmer and crop documentation is not available yet."** (விவசாயி மற்றும் பயிர் பதிவு இன்னும் இல்லை) தொடக்கப் படிவம் இன்னும் தொடங்கப்படவில்லை. பட்டியலில் டெமோவைத் திறந்து **Start Documentation** அழுத்துங்கள்.
- **"CSR-2 not found, not finally approved, or outside your permitted scope."** (CSR-2 கிடைக்கவில்லை, இறுதி ஒப்புதல் இல்லை, அல்லது உங்கள் அனுமதி வரம்புக்கு வெளியே உள்ளது) இந்த டெமோ உங்களுக்கு ஒதுக்கப்படவில்லை.
:::

::: section record-treatment
title: வயல் வருகையில் டிரீட்மென்ட்டைப் பதிவு செய்வது
routes: /TreatmentDetails
for: 
roles: 
designations: 
tour: 1 > [data-help=dm-td-visit] | 2 > [data-help=dm-td-treatment] | 3 > [data-help=dm-td-observations] | 4 > [data-help=dm-td-images] | 5 > [data-help=dm-td-problem] | 6 > [data-help=dm-td-actions]
minutes: 5
source-hash: d955aff37a8e757f
confidence: ok

**இந்தத் திரை எதற்கு**
ஒவ்வொரு வயல் வருகையிலும், என்ன போடப்பட்டது, நீங்கள் என்ன பார்த்தீர்கள், control மற்றும் treated செடிகளின் புகைப்படங்கள் ஆகியவற்றைப் பதிவு செய்ய டிரீட்மென்ட் திரையைப் (உதாரணமாக **Treatment 01**) பயன்படுத்துங்கள். **Start First Treatment**, **Start Next Treatment** அல்லது **View Details** மூலம் இதைத் திறங்கள்.

1. **Visit Details**-ன் கீழ், **Visit Date** மற்றும் **Field Officer / Visibility**-ஐச் சரிபார்த்து, பிறகு **Current Plant Stage** மற்றும் **Plant Condition** தேர்வு செய்யுங்கள்.
2. **Treatment Applied**-ன் கீழ், **Product Used** தேர்வு செய்து, **Dosage** உள்ளிட்டு, **Application Method** தேர்வு செய்து, **Treatment Date** உள்ளிட்டு, **Stage of Application** தேர்வு செய்து, **Remarks** சேருங்கள்.
3. **Observations**-ன் கீழ், **Physical Parameters** மற்றும் **Yield Parameters** எழுதி, பொருந்தும் **Quick Observations**-ஐத் தட்டி, **Observation Notes** எழுதுங்கள்.
4. **Control Images** மற்றும் **Treated Images**-ன் கீழ், **Upload Image** அழுத்தி புகைப்படங்களைத் தேர்வு செய்யுங்கள். Control புகைப்படங்கள் product போடாத செடிகளையும், treated புகைப்படங்கள் product போட்ட செடிகளையும் காட்டும்.
5. **Problem Observed?**-ன் கீழ், **Yes** அல்லது **No** தேர்வு செய்யுங்கள். **Yes** என்றால், **Problem Category**, **Problem Description**, **Severity**, **Recommendation / Monitoring Action** மற்றும் **Action Taken** நிரப்புங்கள்.
6. பிறகு முடிக்க **Save Draft** அழுத்துங்கள், அல்லது வருகை முடிந்ததும் **Complete Treatment** அழுத்துங்கள்.

> Careful: **Problem Observed?** தொடக்கத்தில் **Yes** என்றே இருக்கும். **No** வைத்து டிரீட்மென்ட்டை முடித்தால், முழு டெமோவும் முடிந்துவிடும், அதன் பிறகு வேறு டிரீட்மென்ட் எதுவும் சேர்க்க முடியாது.

> Tip: இரண்டாவது டிரீட்மென்ட்டிலிருந்து, மேலே உள்ள **Previous Treatment Reference** பெட்டி கடைசி வருகையைக் காட்டும். அதனால் ஒப்பிட்டுப் பார்க்கலாம்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Visit Date** மற்றும் **Treatment Date** | தானாக இன்றைய தேதி இருக்கும். டிரீட்மென்ட் தேதி வருகைத் தேதிக்கு முன்பாக இருக்கக் கூடாது. முடிக்கக் கட்டாயம். |
| **Current Plant Stage** | இன்று பார்த்த பயிரின் நிலை. முடிக்கக் கட்டாயம். |
| **Product Used** | போடப்பட்ட product. முடிக்கக் கட்டாயம். |
| **Dosage** | உதாரணமாக 2 ml/L. |
| **Control Images**, **Treated Images** | JPG அல்லது PNG, ஒவ்வொன்றும் 10 MB அல்லது அதற்குக் குறைவு, ஒரே நேரத்தில் 20 வரை. |

## பொதுவான பிரச்சனைகள்

- **"Enter Visit Date, Treatment Date, Current Plant Stage and Product Used before completing the treatment."** (டிரீட்மென்ட்டை முடிக்கும் முன் இந்த நான்கையும் உள்ளிடுங்கள்) இந்த நான்கு புலங்களையும் நிரப்பி, மீண்டும் **Complete Treatment** அழுத்துங்கள்.
- **"Treatment date cannot precede visit date."** (டிரீட்மென்ட் தேதி வருகைத் தேதிக்கு முன்பாக இருக்கக் கூடாது) **Treatment Date**-ஐ வருகைத் தேதி அல்லது அதற்குப் பிறகான தேதிக்கு மாற்றுங்கள்.
- **"Only JPG and PNG images are allowed."** அல்லது **"Each image must be 10MB or less."** (JPG, PNG மட்டுமே; ஒவ்வொரு படமும் 10MB அல்லது அதற்குக் குறைவு) வேறு புகைப்படங்களைத் தேர்வு செய்யுங்கள், அல்லது அவற்றைச் சிறியதாக்குங்கள்.
- **"This treatment has already been completed."** (இந்த டிரீட்மென்ட் ஏற்கனவே முடிந்துவிட்டது) முடிந்த டிரீட்மென்ட்டைப் பார்க்க மட்டுமே முடியும்.
:::

::: section compare-treatments
title: டிரீட்மென்ட்களை ஒப்பிட்டுத் தொடர்ந்து கவனிப்பது
routes: /TreatmentDetails, /DemoDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: 393196d831ee40ab
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரு பிரச்சனை கண்டுபிடிக்கப்பட்டால், டெமோ திறந்தே இருக்கும், நீங்கள் மீண்டும் வயலுக்குப் போவீர்கள். ஒவ்வொரு புதிய டிரீட்மென்ட்டும் முந்தையதைக் காட்டும். அதனால் பயிர் முன்னேறியதா என்று பார்க்கலாம்.

1. **Demo Details**-ல், **Start Next Treatment** அழுத்துங்கள். அடுத்த டிரீட்மென்ட் எண் திறக்கும்.
2. **Previous Treatment Reference** பெட்டியைப் படியுங்கள்: **Visit Date**, **Current Plant Stage**, **Dosage Applied**, **Method**, **Field Observation** மற்றும் **Yield Parameters**.
3. முந்தைய டிரீட்மென்ட்டை அதன் புகைப்படங்களுடன் திறக்க **View Full Details** அழுத்துங்கள்.
4. இன்றைய வருகையையும் புகைப்படங்களையும் அதே முறையில் பதிவு செய்து, என்ன மாறியது என்று **Observation Notes**-ல் எழுதுங்கள்.
5. **Treatment Journey**-ல் எல்லா டிரீட்மென்ட்களையும் பார்க்க **Back to Demo Details** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Previous Treatment Reference பெட்டி இல்லை.** இது இரண்டாவது டிரீட்மென்ட்டிலிருந்து மட்டுமே தெரியும்.
- **பழைய டிரீட்மென்ட்டை மாற்ற வேண்டும்.** **Completed** ஆன டிரீட்மென்ட்டை மாற்ற முடியாது. திருத்தத்தை அடுத்த டிரீட்மென்ட்டில் எழுதுங்கள்.
:::

::: section sample-screens
title: மாதிரி டிரீட்மென்ட் திரைகள்
routes: /TreatmentDemoDetails, /Treatment01, /Treatment02
for: 
roles: 
designations: 
minutes: 1
source-hash: 13af1fa77b22fd32
confidence: ok

**இந்தத் திரை எதற்கு**
**Treatment Demo Details**, **Treatment 01** மற்றும் **Treatment 02** மெனுக்கள், மாறாத உதாரண மதிப்புகளுடன் மாதிரித் திரைகளைத் திறக்கும். ஒரு டெமோ எப்படி இருக்கும் என்று அவை காட்டும். அங்கே நீங்கள் டைப் செய்யும் எதுவும் சேமிக்கப்படாது.

1. **Treatment Demo Details**, மூன்று டிரீட்மென்ட்கள் உள்ள **Treatment Journey**-உடன் ஒரு உதாரண **Demo Details** திரையைக் காட்டும்.
2. **Treatment 01**, முடிந்த ஒரு டிரீட்மென்ட்டின் உதாரணத்தைக் காட்டும்: **Visit Details**, **Treatment Applied**, **Observations**, புகைப்படங்கள் மற்றும் **Problem Identification & Recommendation**.
3. **Treatment 02**, மேலே **Previous Treatment Reference - Treatment 01** பெட்டியுடன் இரண்டாவது டிரீட்மென்ட்டின் உதாரணத்தைக் காட்டும்.
4. உண்மையான வேலையைப் பதிவு செய்ய, **Demo Documentation**-ஐத் திறந்து உங்கள் டெமோவைத் தேர்வு செய்யுங்கள்.

> Careful: இந்த மாதிரித் திரைகளில் உள்ள பட்டன்கள் எதையும் சேமிக்காது.

## பொதுவான பிரச்சனைகள்

- **நான் உள்ளிட்டவை மறைந்துவிட்டன.** இவை மாதிரித் திரைகள். உண்மையான டெமோவைப் பதிவு செய்ய **Demo Documentation** பயன்படுத்துங்கள்.
:::
