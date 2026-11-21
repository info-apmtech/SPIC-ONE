---
module: sas-sample-collection
title: மண் மாதிரி சேகரிப்பு
icon: bi-droplet
order: 50
language: ta
category: மண் பரிசோதனை
note: புதிய தோற்றம்: மாதிரியின் பயணம், நிலைகள் மற்றும் படிப்படியான வழிகாட்டல்
meta-source-hash: 91cc9c6288b84a36
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 722480765e5f4cad
confidence: ok

SPIC விவசாயியின் நிலத்தில் இருந்து எடுக்கும் மண்ணையும் தண்ணீரையும் மாவட்ட லேபில் பரிசோதிக்கிறது. ஒவ்வொரு மாதிரியையும் வயலில் இருந்து விவசாயியின் அறிக்கை வரை ஆப் பின்தொடர்கிறது. அதனால் மாதிரி எங்கே இருக்கிறது என்று எல்லோரும் பார்க்கலாம்.

- collect | icon: bi-droplet | color: #0d9488
  title: வயலில் சேகரித்தல்
  text: MDO அல்லது JMDO விவசாயியையும் மாதிரிகளையும் பதிவு செய்வார்.
- pay | icon: bi-qr-code | color: #d97706
  title: பணம் கட்டுதல், ஒப்புதல்
  text: பணம் கட்டும் மாதிரிக்கு UPI அல்லது வேறு வழியில் பணம் செலுத்தப்படும். பிறகு அதற்கு ஒப்புதல் தரப்பட்டு, Finance சரிபார்க்கும்.
- report | icon: bi-file-earmark-text | color: #7c3aed
  title: விவசாயிக்கு லேப் அறிக்கை
  text: லேப் மாதிரிகளைப் பரிசோதிக்கும். விவசாயி அறிக்கையைத் தமிழிலோ ஆங்கிலத்திலோ பதிவிறக்கலாம்.
:::

::: section flow
title: ஒரு மாதிரி எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 8e557635907c2edf
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. இலவச மாதிரிக்குப் பணம் கட்டும் படிகள் இல்லை. ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- officer | type: lane | color: #0d9488 | roles: MDO, JMDO
  title: களப் பணியாளர்
  sub: MDO · JMDO
- approver | type: lane | color: #ea580c | roles: Admin, CorporateAdmin
  title: ஒப்புதல் தருபவர்
  sub: பணத்துக்கு ஒப்புதல்
- finance | type: lane | color: #2563eb
  title: Finance
  sub: பணச் சரிபார்ப்பு
- lab | type: lane | color: #7c3aed
  title: லேப்
  sub: மாவட்ட லேப்
- farmer | type: lane | color: #16a34a | roles: Farmer
  title: விவசாயி
  sub: மாதிரியின் உரிமையாளர்
- collect | lane: officer | col: 1 | icon: bi-droplet | color: #64748b | next: pay | screen: collect-sample
  title: மாதிரிகளைச் சேகரித்தல்
  status: Draft
  text: விவசாயியைத் தேர்வு செய்து, ஒவ்வொரு மண் அல்லது தண்ணீர் மாதிரியையும் சேர்த்துச் சேமியுங்கள். இலவச மாதிரி நேரடியாக consignment-க்குப் போகும்.
- pay | lane: officer | col: 2 | icon: bi-qr-code | color: #d97706 | next: approve | screen: record-payment
  title: பணத்தைப் பதிவு செய்தல்
  status: Pending Payment
  text: விவசாயி QR code-ஐ ஸ்கேன் செய்து பணம் கட்டுவார். அதிகாரி பரிவர்த்தனை விவரத்தை உள்ளிட்டு ரசீதை அப்லோட் செய்வார்.
- approve | lane: approver | col: 3 | icon: bi-patch-check | color: #ea580c | next: send, verify | return: pay | screen: approve-payments
  title: பணத்துக்கு ஒப்புதல் தருதல்
  status: Pending Approval
  text: ஒப்புதல் தருபவர் தொகையையும் ரசீதையும் சரிபார்த்து, ஒப்புதல் தந்து Finance-க்கு அனுப்புவார், அல்லது திருப்பி அனுப்புவார்.
- send | lane: officer | col: 4 | icon: bi-truck | color: #0d9488 | next: test | screen: send-consignment
  title: லேபுக்கு அனுப்புதல்
  status: Dispatched
  text: அதிகாரி மாதிரிகளைக் கூரியரிடம் கொடுத்து டிராக்கிங் எண்ணைப் பதிவு செய்வார். இதற்குப் பெயர் consignment.
- verify | lane: finance | col: 4 | icon: bi-bank | color: #2563eb | screen: verify-payments
  title: பணம் வந்ததைச் சரிபார்த்தல்
  status: Finance Verified
  text: பணம் கணக்குக்கு வந்து சேர்ந்ததை Finance உறுதி செய்யும், அல்லது பொருந்தவில்லை (mismatch) என்று குறிக்கும்.
- test | lane: lab | col: 5 | icon: bi-eyedropper | color: #7c3aed | next: report
  title: மாதிரிகளைப் பரிசோதித்தல்
  status: In Progress
  text: லேப் பார்சலைப் பெற்று, ஒவ்வொரு மாதிரியையும் பரிசோதித்து, முடிவுகளை வெளியிடும்.
- report | lane: farmer | col: 6 | icon: bi-file-earmark-arrow-down | color: #16a34a | screen: farmer-my-reports
  title: அறிக்கையைப் பதிவிறக்குதல்
  status: Completed
  text: விவசாயி ஆப்பில் அறிக்கையைப் பார்த்து, தமிழிலோ ஆங்கிலத்திலோ பதிவிறக்கலாம்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 0771c49c0ffc8c78
confidence: ok

- draft | color: #64748b
  label: Draft
  text: சேமிக்கப்பட்டது, இன்னும் சமர்ப்பிக்கவில்லை.
- pending-payment | color: #d97706
  label: Pending Payment
  text: பணம் பதிவு செய்யக் காத்திருக்கிறது.
- pending-approval | color: #ea580c
  label: Pending Approval
  text: பணம் பதிவாகிவிட்டது, ஒப்புதலுக்குக் காத்திருக்கிறது.
- ready | color: #0891b2
  label: Ready consignment
  text: லேபுக்கு அனுப்பத் தயார்.
- dispatched | color: #0d9488
  label: Dispatched
  text: கூரியரிடம் கொடுக்கப்பட்டது.
- in-transit | color: #0d9488
  label: In Transit
  text: லேபுக்குப் போகும் வழியில் உள்ளது.
- delivered | color: #2563eb
  label: Delivered to Lab
  text: பார்சல் லேபுக்கு வந்துவிட்டது.
- in-progress | color: #7c3aed
  label: In Progress
  text: லேப் பரிசோதித்துக்கொண்டிருக்கிறது.
- completed | color: #16a34a
  label: Completed
  text: முடிவுகள் கிடைத்துவிட்டன.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 9c070a1e6fc377cf
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- officer | color: #0d9488 | roles: MDO, JMDO
  title: MDO, JMDO
  - மாதிரிகளைச் சேகரிப்பது
  - பணத்தைப் பதிவு செய்வது
  - Consignment-களை லேபுக்கு அனுப்புவது
- approver | color: #ea580c | roles: Admin, CorporateAdmin
  title: ஒப்புதல் தருபவர்
  - பணத்துக்கு ஒப்புதல் தருவது அல்லது திருப்பி அனுப்புவது
  - Consignment-களை அடுத்த நிலைக்கு நகர்த்துவது
  - லேப் முடிவுகளை உள்ளிடுவது
- finance | color: #2563eb
  title: Finance
  - வந்த பணத்தைச் சரிபார்ப்பது
  - பொருந்தாதவற்றை (mismatch) குறிப்பது
- farmer | color: #16a34a | roles: Farmer
  title: விவசாயி
  - உங்கள் மாதிரிகளைப் பார்ப்பது
  - உங்கள் அறிக்கைகளைப் பதிவிறக்குவது
  - நீங்கள் கட்டிய பணத்தைச் சரிபார்ப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: b121292d9fb622a2
confidence: ok

- no-menu | popular: true
  q: மெனுவில் Sample Collection தெரியவில்லை
  a: உங்கள் பதவியில் (designation) இந்தப் பக்கம் இல்லை. உங்கள் நிர்வாகியிடம் (administrator) கேளுங்கள். JMDO பயனர்கள் இதை **Soil Sample** பணியிலிருந்து (task) திறக்க வேண்டும்.
- payment-rejected | popular: true
  q: என் பணம் நிராகரிக்கப்பட்டது. இப்போது என்ன செய்வது?
  a: விவரங்கள் பக்கத்தில் காரணத்துடன் **Payment rejected** என்று காட்டும். அதைத் திருத்தி, மீண்டும் **Record payment** தட்டுங்கள்.
- not-ready
  q: "This collection is not ready to send" என்று வருகிறது
  a: பணம் இன்னும் பதிவாகவில்லை அல்லது ஒப்புதல் கிடைக்கவில்லை. இலவச மாதிரிகளை உடனே அனுப்பலாம்.
- report-language
  q: அறிக்கையைத் தமிழில் எப்படிப் பெறுவது?
  a: **Download PDF**-ன் பக்கத்தில் உள்ள சிறிய அம்புக்குறியைத் தட்டி, தமிழ் அல்லது ஆங்கிலம் தேர்வு செய்யுங்கள்.
:::

::: section collection-list
title: உங்கள் மாதிரி சேகரிப்புகளைக் கண்டுபிடித்துப் பின்தொடர்வது
routes: /SampleCollection
for: 
roles: 
designations: 
screenshot: collection-list.png
hotspots: 1 @ 2.2,68.5,95.5,15.1 | 2 @ 73.7,88,22.4,4.8 | 3 @ 2.2,30.7,95.5,35.4 | 4 @ 3.8,94.7,92.3,5.3 | 5 @ 62.1,1.9,8.1,4.8 | 6 @ 79.2,1.9,18.5,4.8
screenshot-phone: collection-list.phone.jpg
hotspots-phone: 1 @ 3.1,40.6,93.8,24.4 | 2 @ 6.4,69.4,87.2,3.2 | 3 @ 3.1,36,93.8,3.2 | 4 @ 2,2,8,4 | 5 @ 15.3,4.3,10.2,3 | 6 @ 39.7,4.3,48.8,3
tour: 1 > [data-help=sc-tiles] | 2 > [data-help=sc-search] | 3 > [data-help=sc-filters] | 4 > [data-help=sc-table] | 5 > [data-help=sc-excel] | 6 > [data-help=sc-new]
minutes: 2
source-hash: 01315bd944ae112a
confidence: ok

**இந்தத் திரை எதற்கு**
**Sample Collection** பக்கத்தில், நீங்கள் பார்க்க அனுமதியுள்ள எல்லாச் சேகரிப்புகளும், அவற்றின் பண நிலை மற்றும் லேப் நிலையுடன் தெரியும். ஒரு சேகரிப்பைக் கண்டுபிடித்து அதன் விவரங்களைத் திறக்க இதைப் பயன்படுத்துங்கள்.

மேலே உள்ள கார்டுகள் எண்ணிக்கையைக் காட்டும்: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level** மற்றும் **Ready to Submit**.

1. **Pending Payment** அல்லது **Draft** போன்ற வண்ணக் கட்டத்தைத் தட்டினால் அந்தச் சேகரிப்புகள் மட்டும் தெரியும். மீண்டும் தட்டினால் எல்லாம் தெரியும்.
2. தேட, **Search collections...**-ல் விவசாயி பெயர் அல்லது collection ID-ஐ டைப் செய்யுங்கள்.
3. மேலும் வசதிகளுக்கு, **Filters**-ஐத் திறந்து **Date Range**, **Location**, **Payment Type**, **Payment Status** அல்லது **Status** அமைத்து, **Apply Filters** தட்டுங்கள். அவற்றை நீக்க **Clear All** தட்டுங்கள்.
4. சேகரிப்பைத் திறக்க கண் ஐகானைத் தட்டுங்கள் (போனில் கார்டைத் தட்டுங்கள்).
5. திரையில் தெரியும் பட்டியலைப் பதிவிறக்க **Excel** அல்லது **PDF** தட்டுங்கள். சமீபத்திய நிலையைப் பெற **Refresh Data** தட்டுங்கள்.
6. மாதிரி சேகரிக்கத் தொடங்க **New Sample Collection** தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **No sample collections found** (மாதிரி சேகரிப்பு எதுவும் இல்லை): ஃபில்டர்களை நீக்குங்கள், அல்லது தேதி வரம்பைச் சரிபாருங்கள்.
- **Collections could not be loaded** (சேகரிப்புகளை ஏற்ற முடியவில்லை): இணைய இணைப்பைச் சரிபார்த்து **Try again** தட்டுங்கள்.
- **New Sample Collection** பட்டன் இல்லை: உங்கள் கணக்கால் சேகரிப்பு உருவாக்க முடியாது. பதவி அனுமதிக்கும் MDO, JMDO மற்றும் நிர்வாகி கணக்குகள் மட்டுமே மாதிரி சேகரிக்க முடியும்.
- பதிவிறக்கத்தில் சில வரிசைகள் மட்டுமே உள்ளன: **Excel** மற்றும் **PDF** இப்போதைய பக்கத்தை மட்டுமே சேமிக்கும். அதிக வரிசைகள் சேர்க்க **Items Per Page**-ஐ மாற்றுங்கள்.
:::

::: section collect-sample
title: புதிய மண் அல்லது தண்ணீர் மாதிரி சேகரிப்பது
routes: /SampleCollection/new
for: SampleCollection.Entry
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
screenshot: collect-sample.png
hotspots: 1 @ 66,1.9,31.8,5 | 2 @ 4,34.7,29.7,7.8 | 3 @ 35.2,81.7,29.7,8 | 4 @ 78.7,100,17.2,0 | 5 @ 81.8,100,16,0
screenshot-phone: collect-sample.phone.jpg
hotspots-phone: 1 @ 3.1,4.6,93.8,3.2 | 2 @ 6.9,33.7,86.2,8.5 | 3 @ 6.9,100,86.2,0 | 4 @ 6.9,100,86.2,0 | 5 @ 43.8,92.1,52.1,3
tour: 1 > [data-help=sc-paytype] | 2 > [data-help=sc-farmer] | 3 > [data-help=sc-soil] | 4 > [data-help=sc-add] | 5 > [data-help=sc-save]
minutes: 4
source-hash: 3ae93abc0337f008
confidence: ok

**இந்தத் திரை எதற்கு**
விவசாயிகளிடம் இருந்து நீங்கள் எடுக்கும் மாதிரிகளைப் பதிவு செய்ய **New Sample Collection**-ஐப் பயன்படுத்துங்கள். ஒரு சேகரிப்பில் பல விவசாயிகளின் மாதிரிகள் இருக்கலாம்.

**Sample Collection** பக்கத்தில் **New Sample Collection** தட்டி இதைத் திறங்கள். JMDO: **Tasks Allocation**-ல் உள்ள **Soil Sample** கார்டில் **Start** தட்டுங்கள், அல்லது ஒரு பணியின் **Quick actions**-ல் **Soil Sample Collection** தட்டுங்கள்.

1. மேலே **Free Sample** அல்லது **Paid Sample** தேர்வு செய்யுங்கள்.
2. **Farmer Name**-ல் குறைந்தது இரண்டு எழுத்துக்கள் டைப் செய்து விவசாயியைத் தேர்வு செய்யுங்கள். புதிய விவசாயி என்றால் **New Farmer** தட்டுங்கள். பணம் கட்டும் மாதிரி என்றால், **Paid Sample Category**-ன் கீழ் **Farmer** அல்லது **NGO** தேர்வு செய்யுங்கள்.
3. **Soil Sample** மற்றும் **Water Sample**-ஐ **Yes** அல்லது **No** என்று அமையுங்கள். **Crop 1**, **Crop 2** தேர்வு செய்து, தேவைப்பட்டால் **Remarks** சேருங்கள்.
4. **Add Another Sample** தட்டுங்கள். மாதிரி அதன் தொகையுடன் **Sample Added (Preview)**-ல் தெரியும். ஒவ்வொரு மாதிரிக்கும் இதையே செய்யுங்கள்; பென்சில் ஒரு வரிசையை மாற்றும், குப்பைத்தொட்டி அதை நீக்கும்.
5. பிறகு முடிக்க **Save Draft** தட்டுங்கள், அல்லது **Save and Continue** தட்டுங்கள். **Sample Summary**-ஐச் சரிபார்த்து **Continue to payment** (பணம் கட்டும் மாதிரி) அல்லது **Continue to consignment** (இலவச மாதிரி) தட்டுங்கள்.

> Tip: ஒரு சேகரிப்பில் பல விவசாயிகளின் மாதிரிகள் இருக்கலாம்.

## புலங்களின் விளக்கம்

- **Collection ID**: நீங்கள் சேமிக்கும்போது ஆப் உருவாக்கும்.
- **Collected By**: உங்கள் பெயர், தானாக நிரப்பப்படும்.
- **Total Amount**: பணம் கட்டும் எல்லா மாதிரிகளுக்குமான கட்டணம். இலவசச் சேகரிப்பில் **Free** என்று காட்டும்.
- **New Farmer**: **Farmer Name** மற்றும் 10 இலக்க **Mobile Number** தேவை. **Use my location** தட்டினால் **Latitude** மற்றும் **Longitude** தானாக நிரம்பும்.

## பொதுவான பிரச்சனைகள்

- "Choose a farmer, or add a new one" (விவசாயியைத் தேர்வு செய்யுங்கள், அல்லது புதியவரைச் சேருங்கள்): டைப் செய்தால் மட்டும் போதாது, பட்டியலில் இருந்து ஒரு பெயரைத் தேர்வு செய்யுங்கள்.
- "Choose at least one of Soil Sample or Water Sample" (Soil Sample அல்லது Water Sample-ல் குறைந்தது ஒன்றைத் தேர்வு செய்யுங்கள்): அவற்றில் ஒன்றை **Yes** என்று அமையுங்கள்.
- விவசாயியின் முகவரியைத் திருத்த, **Edit farmer** தட்டி, மாற்றி, பிறகு **Save farmer details** தட்டுங்கள்.
:::

::: section edit-draft
title: டிராஃப்ட் சேகரிப்பை முடிப்பது அல்லது மாற்றுவது
routes: /SampleCollection/new, /SampleCollection/view/{id}
for: SampleCollection.Update
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 2
source-hash: 2699d934a7f0674d
confidence: ok

**இந்தத் திரை எதற்கு**
**Save Draft** மூலம் சேமித்த சேகரிப்பு **Draft** ஆகவே இருக்கும். நீங்கள் சமர்ப்பிக்கும் வரை எதுவும் பணம் கட்டும் நிலைக்கோ லேபுக்கோ போகாது. அதை மீண்டும் திறந்து முடிக்கலாம்.

1. **Sample Collection**-ஐத் திறந்து **Draft** கட்டத்தைத் தட்டுங்கள்.
2. கண் ஐகான் மூலம் சேகரிப்பைத் திறங்கள்.
3. **Sample Collection Details**-ன் மேலே உள்ள **Edit** தட்டுங்கள்.
4. புதிய சேகரிப்பில் செய்வது போலவே மாதிரிகளைச் சேருங்கள், மாற்றுங்கள் அல்லது நீக்குங்கள்.
5. மீண்டும் டிராஃப்டாக வைக்க **Save Draft** தட்டுங்கள், அல்லது **Save and Continue** தட்டுங்கள்.
6. **Sample Summary**-ல் மாதிரிகளைச் சரிபார்த்து **Continue to payment** அல்லது **Continue to consignment** தட்டுங்கள்.

> சமர்ப்பித்த பிறகு, சேகரிப்பை மாற்ற முடியாது.

## பொதுவான பிரச்சனைகள்

- **Edit** பட்டன் இல்லை: சேகரிப்பு ஏற்கனவே சமர்ப்பிக்கப்பட்டுவிட்டது, அல்லது உங்கள் பதவி மாற்றங்களை அனுமதிக்கவில்லை.
- "This collection has already been submitted" (இந்தச் சேகரிப்பு ஏற்கனவே சமர்ப்பிக்கப்பட்டுவிட்டது): டிராஃப்ட் முன்பே சமர்ப்பிக்கப்பட்டது. நிலையைப் பின்தொடர அதன் விவரங்களைத் திறங்கள்.
- "That draft no longer exists" (அந்த டிராஃப்ட் இப்போது இல்லை): அது நீக்கப்பட்டுவிட்டது. புதிய சேகரிப்பைத் தொடங்குங்கள்.
:::

::: section record-payment
title: பணம் கட்டும் மாதிரிக்குப் பணத்தைப் பதிவு செய்வது
routes: /SampleCollection/payment/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: c5b827bf8559be23
confidence: ok

**இந்தத் திரை எதற்கு**
**Payments** திரையில் எவ்வளவு பணம் வசூலிக்க வேண்டும், விவசாயி ஸ்கேன் செய்ய ஒரு UPI QR code, பண விவரங்களுக்கும் ரசீதுக்குமான ஒரு படிவம் ஆகியவை இருக்கும்.

1. **Continue to payment**-க்குப் பிறகு **Payments** திரை திறக்கும். சேகரிப்பு விவரங்களில் **Record payment** தட்டியும் திறக்கலாம்.
2. **Total Amount to Collect**-ஐச் சரிபாருங்கள். மாதிரிகளைப் பார்க்க **View Sample Summary** தட்டுங்கள்.
3. **Scan & Pay**-ன் கீழ் உள்ள QR code-ஐ ஏதாவது ஒரு UPI ஆப் மூலம் ஸ்கேன் செய்யுமாறு விவசாயியிடம் சொல்லுங்கள். அதைச் சேமிக்க **Download QR Code** தட்டுங்கள்.
4. **Payment Information**-ஐ நிரப்புங்கள்.
5. **Payment Proof**-ன் கீழ், **Click to Upload or Drag and Drop** தட்டி, ரசீது அல்லது ஸ்கிரீன்ஷாட்டைத் தேர்வு செய்யுங்கள்.
6. **Save Payment** தட்டுங்கள். இப்போது பணம் ஒப்புதலுக்குக் காத்திருக்கும்.

## புலங்களின் விளக்கம்

- **Transaction ID** (கட்டாயம்): UPI பரிவர்த்தனை எண்.
- **Paid By** (கட்டாயம்): யார் பணம் கட்டினார்கள். சேகரிப்பில் உள்ள விவசாயிகளின் பெயர்கள் காட்டப்படும்.
- **Payment Mode**: **UPI**, **Bank Transfer** அல்லது **Cash**.
- **Transaction Date**, **Bank / Gateway**, **Payment Gateway / UTR No.**, **Bank Name**, **Contact Number**, **Email (optional)**: உங்களிடம் உள்ளதை நிரப்புங்கள்.
- **Remarks**: ஒப்புதல் தருபவருக்கான குறிப்பு.
- **Payment Proof**: PNG அல்லது JPG படம், 5 MB வரை.

## பொதுவான பிரச்சனைகள்

- "Please fill in the Transaction ID and Paid By" (Transaction ID மற்றும் Paid By-ஐ நிரப்புங்கள்): இரண்டுமே கட்டாயம்.
- "Pick a PNG or JPG image of 5 MB or less" (5 MB அல்லது அதற்குக் குறைவான PNG அல்லது JPG படத்தைத் தேர்வு செய்யுங்கள்): சிறிய புகைப்படம் அல்லது ஸ்கிரீன்ஷாட் எடுங்கள்.
- **Save Draft** டைப் செய்த விவரங்களை இந்தப் போனில் மட்டுமே வைத்திருக்கும். **Save Payment** மூலம் முடியுங்கள்.
- "Payment already recorded" (பணம் ஏற்கனவே பதிவாகிவிட்டது): அது ஒப்புதலுக்குக் காத்திருக்கிறது.
- பணம் நிராகரிக்கப்பட்டால், விவரங்கள் பக்கத்தில் காரணத்துடன் **Payment rejected** என்று காட்டும். அதைத் திருத்தி, மீண்டும் **Record payment** தட்டுங்கள்.
:::

::: section send-consignment
title: மாதிரிகளை லேபுக்கு அனுப்புவது (consignment)
routes: /SampleCollection/consignment/{id}, /SampleCollection/submitted/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: de0b21e8f58ece6a
confidence: ok

**இந்தத் திரை எதற்கு**
சேகரிப்பு இலவசமாக இருந்தால், அல்லது அதன் பணத்துக்கு ஒப்புதல் கிடைத்தால், மாதிரிகளை பேக் செய்து கூரியரிடம் கொடுப்பீர்கள். என்ன வருகிறது என்று லேபுக்குத் தெரிய, **Upload Consignment** கூரியர் விவரங்களையும் புகைப்படங்களையும் பதிவு செய்யும்.

1. சேகரிப்பைத் திறந்து **Upload consignment** தட்டுங்கள். இலவச மாதிரிக்கு, **Continue to consignment** நேரடியாக இதைத் திறக்கும்.
2. **Courier Details**-ன் கீழ், **Courier Service**-ஐத் தேர்வு செய்து **Tracking Number (AWB)**-ஐ டைப் செய்யுங்கள்.
3. தயாராக உள்ள மற்ற சேகரிப்புகளையும் அதே பார்சலில் அனுப்ப, **Include other ready collections**-ஐத் திறந்து அவற்றை டிக் செய்யுங்கள். மேலே உள்ள **Both**, **Free Sample**, **Paid Sample** எவை பட்டியலில் தெரியும் என்பதை மாற்றும்.
4. **Upload Photos and Documents**-ன் கீழ், **Shipping Label / Waybill**-ல் ஒரு புகைப்படம் சேருங்கள். போனில் **Take a photo** தட்டலாம்.
5. விருப்பம் இருந்தால் **Package and contact details**-ஐத் திறங்கள்.
6. **Submit Consignment** தட்டுங்கள்.
7. **Consignment Submitted Successfully!** டிராக்கிங் எண்ணைக் காட்டும். அதை நகலெடுக்க copy ஐகானைத் தட்டுங்கள், விவரங்களைப் பார்க்க **View Consignment** தட்டுங்கள், அல்லது **Go to Dashboard** தட்டுங்கள்.

## புலங்களின் விளக்கம்

- **Tracking Link (Optional)**, **Expected Delivery Date**: பார்சலைப் பின்தொடர லேபுக்கு உதவும்.
- **Package Photo**, **Any other Document**: விருப்பப் புகைப்படங்கள், PNG அல்லது JPG, ஒவ்வொன்றும் 5 MB வரை.
- **Packaging Type**, **No. of Packages**, **Package Weight (kg)**, **Contact Person**, **Mobile**, **Email**, **Collection Notes**: விருப்பம்.

## பொதுவான பிரச்சனைகள்

- "Please fill the courier details and add the shipping label photo" (கூரியர் விவரங்களை நிரப்பி, shipping label புகைப்படத்தைச் சேருங்கள்): மூன்றுமே கட்டாயம்.
- "This collection is not ready to send" (இந்தச் சேகரிப்பு அனுப்பத் தயாராக இல்லை): பணம் இன்னும் பதிவாகவில்லை அல்லது ஒப்புதல் கிடைக்கவில்லை.
- **Save Draft** டைப் செய்த புலங்களை மட்டுமே வைத்திருக்கும். புகைப்படங்களைப் பிறகு மீண்டும் சேருங்கள்.
- JMDO: consignment-ஐச் சமர்ப்பித்தால் இன்றைய **Soil Sample** பணி முடிந்ததாகக் குறிக்கப்படும்.
:::

::: section collection-details
title: மாதிரி சேகரிப்பின் விவரங்களைப் படிப்பது
routes: /SampleCollection/view/{id}
for: 
roles: 
designations: 
screenshot: collection-details.png
screenshot-phone: collection-details.phone.jpg
minutes: 2
source-hash: 0b6c9585e9bd5ccc
confidence: ok

**இந்தத் திரை எதற்கு**
**Sample Collection Details** ஒரு சேகரிப்பின் எல்லா விவரங்களையும் காட்டும்: பணம், கூரியர், லேப் முடிவுகள், வரலாறு. களப் பணியாளர்கள், டீலர்கள், மேலாளர்கள் இங்கே முன்னேற்றத்தைப் பின்தொடரலாம்.

1. **Sample Collection**-ஐத் திறந்து, ஒரு சேகரிப்பில் கண் ஐகானைத் தட்டுங்கள்.
2. மேல் வரியில் collection ID, **Paid Sample** அல்லது **Free Sample**, இப்போதைய நிலை ஆகியவை தெரியும்.
3. பட்டையில் **Consignment ID**, **Transaction ID**, **Courier Partner**, **Samples**, **No. of Tests**, **Payment Status** தெரியும். Consignment-ஐத் திறக்க consignment ID-ஐத் தட்டுங்கள்.
4. **Collection Information** மற்றும் **Sample Summary**-ல் மாதிரிகளை யார் சேகரித்தார்கள், ஒவ்வொன்றுக்கும் எத்தனை பரிசோதனைகள் தேவை என்பது இருக்கும்.
5. லேப் மதிப்புகள் தயாரானதும் **Result Comparison**-ல் தெரியும். பல மாதிரிகள் இருந்தால், ஒரு மாதிரியின் முடிவுகளைப் பார்க்க மேலே அதைத் தட்டுங்கள்.
6. **Consignments Details**, **Amount Details**, **History / Timeline** ஆகியவை கூரியர், கட்டணங்கள், இதுவரை நடந்த ஒவ்வொரு படியையும் காட்டும்.
7. அச்சிட அல்லது பதிவிறக்க **Export** தட்டுங்கள்.

## நீங்கள் பார்க்கக்கூடிய பட்டன்கள்

- **Edit**: சேகரிப்பு இன்னும் டிராஃப்ட் நிலையில் உள்ளது.
- **Record payment**: பணம் இன்னும் நிலுவையில் உள்ளது.
- **Upload consignment**: லேபுக்கு அனுப்பத் தயார்.

## பொதுவான பிரச்சனைகள்

- "The lab has not published the results yet" (லேப் இன்னும் முடிவுகளை வெளியிடவில்லை): மாதிரி இன்னும் வழியில் உள்ளது, அல்லது பரிசோதனை நடக்கிறது.
- Consignment-ன் கீழ் **Not dispatched yet**: மாதிரிகள் இன்னும் அனுப்பப்படவில்லை.
- "Collection not found" (சேகரிப்பு கிடைக்கவில்லை): அது நீக்கப்பட்டுவிட்டது, அல்லது உங்களுக்கு அனுமதி இல்லை.
:::

::: section admin-review
title: பணத்துக்கு ஒப்புதல், consignment நகர்த்துதல், முடிவுகளை உள்ளிடுதல்
routes: /SampleCollection/view/{id}, /ConsignmentHistory/view/{id}
for: 
roles: Admin, CorporateAdmin
designations: 
minutes: 3
source-hash: 16838640fb4dadb0
confidence: ok

**இந்தத் திரை எதற்கு**
நிர்வாகிகள் சேகரிப்பு விவரங்கள் பக்கத்தில் பணத்தைப் பரிசீலிக்கலாம், ஒரு consignment-ஐ அடுத்த நிலைக்கு நகர்த்தலாம், லேப் முடிவுகளை டைப் செய்யலாம்.

### சேகரிப்பில் பணத்துக்கு ஒப்புதல் தருவது

1. **Pending Approval** நிலையில் உள்ள ஒரு சேகரிப்பைத் திறங்கள்.
2. **Payment Approval**-ல், **Amount**, **Transaction ID**, ரசீது ஆகியவற்றை ஒப்பிட்டுப் பாருங்கள். ரசீதைப் பெரிதாக்க **View proof** தட்டுங்கள்.
3. **Approve payment** தட்டி உறுதி செய்யுங்கள். சேகரிப்பு அனுப்பத் தயாராகும்.
4. நிராகரிக்க, **Reject** தட்டி, **Reason for rejection** எழுதி, பிறகு **Reject payment** தட்டுங்கள்.

### Consignment-ஐ அடுத்த நிலைக்கு நகர்த்துவது

1. **Consignment History**-ஐத் திறந்து ஒரு consignment-ஐத் திறங்கள்.
2. **Update status**-ன் கீழ், **In Transit**, **Delivered** அல்லது **Completed** போன்ற அடுத்த நிலையைத் தட்டுங்கள்.

> Consignment-ஐ நகர்த்தினால், அதில் உள்ள எல்லா மாதிரிகளும் நகரும்.

### லேப் முடிவுகளை உள்ளிடுவது

1. சேகரிப்பு விவரங்களில், **Result Comparison**-க்குப் போய், மாதிரியைத் தேர்வு செய்து, **Enter results** (அல்லது **Edit results**) தட்டுங்கள்.
2. **Test Parameter**, **Entered Value**, **Unit**, **Normal Range**, **Result** நிரப்புங்கள். கூடுதல் வரிசைகளுக்கு **Add parameter** தட்டுங்கள்.
3. **Save results** தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- "Add a reason so the collector knows what to correct" (சேகரித்தவருக்கு எதைத் திருத்த வேண்டும் என்று தெரிய காரணத்தைச் சேருங்கள்): நிராகரிப்புக்குக் காரணம் தேவை.
- **Update status** இல்லை: consignment ஏற்கனவே **Completed** ஆகிவிட்டது.
:::

::: section consignment-history
title: லேபுக்கு அனுப்பிய consignment-களைப் பின்தொடர்வது
routes: /ConsignmentHistory, /ConsignmentHistory/view/{id}
for: 
roles: 
designations: 
screenshot: consignment-history.png
screenshot-phone: consignment-history.phone.jpg
minutes: 2
source-hash: c4455c0c6e2c1ab2
confidence: ok

**இந்தத் திரை எதற்கு**
**Consignment History**-ல் லேபுக்கு அனுப்பிய மாதிரிகளின் ஒவ்வொரு பார்சலும், கூரியர், டிராக்கிங் எண், இப்போதைய நிலையுடன் இருக்கும்.

1. மெனுவில் இருந்து **Consignment History**-ஐத் திறங்கள்.
2. கார்டுகள் **Total Consignments**, **Total Consignment Sent**, **Total Samples**, **Delivered** மற்றும் **Pending**-ஐக் காட்டும்.
3. ஒன்றைக் கண்டுபிடிக்க **Search consignment, AWB, transaction...**-ல் டைப் செய்யுங்கள்.
4. **Filters**-ஐத் திறந்து **From**, **To**, **Status**, **Courier Partner** அல்லது **Payment Type** தேர்வு செய்து, **Apply Filters** தட்டுங்கள்.
5. கூரியரின் டிராக்கிங் பக்கத்தைத் திறக்க டிராக்கிங் எண்ணைத் தட்டுங்கள்.
6. **Consignment Details**-ஐத் திறக்க consignment ID அல்லது கண் ஐகானைத் தட்டுங்கள்.
7. விவரங்கள் பக்கத்தில் timeline, உள்ளே உள்ள மாதிரிகள், தொகை ஆகியவற்றைப் பாருங்கள். லேபிள் மற்றும் பார்சல் புகைப்படங்களைப் பார்க்க **Click and View Photos and Files** தட்டுங்கள், அவற்றைச் சேமிக்க **Download All** தட்டுங்கள்.
8. பதிவிறக்க **Excel** அல்லது **PDF** பயன்படுத்துங்கள், புதுப்பிக்க **Refresh Data** பயன்படுத்துங்கள்.

## புலங்களின் விளக்கம்

- **Current Status**: **Pending Pickup**, **Dispatched**, **In Transit**, **Delivered** அல்லது **Completed**.
- **AWB/Tracking No.**: கூரியரின் டிராக்கிங் எண்.
- **Total samples**: போனில், பட்டியலைப் பார்க்க மாதிரிகளின் எண்ணிக்கையைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **No consignments found** (consignment எதுவும் இல்லை): வேறு தேதி வரம்பு, நிலை அல்லது கூரியரை முயற்சி செய்யுங்கள், அல்லது **Clear filters** தட்டுங்கள்.
- **No photos yet** (இன்னும் புகைப்படம் இல்லை): consignment அனுப்பும்போது எதுவும் இணைக்கப்படவில்லை.
- JMDO பயனர்களுக்கு இந்தப் பக்கம் மெனுவில் தெரியாது.
:::

::: section farmer-my-samples
title: உங்கள் மண் மாதிரிகளைப் பார்ப்பது (விவசாயி)
routes: /Sas/MySamples, /SampleCollection/view/{id}
for: 
roles: Farmer
designations: 
screenshot: farmer-my-samples.png
screenshot-phone: farmer-my-samples.phone.jpg
minutes: 2
source-hash: eac7ad7b8677316c
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் MO அதிகாரி உங்களிடம் இருந்து சேகரித்த ஒவ்வொரு மண் அல்லது தண்ணீர் மாதிரியையும், அது இப்போது எங்கே இருக்கிறது என்பதையும் **My Samples** காட்டும்.

1. மெனுவில் இருந்து **My Samples**-ஐத் திறங்கள்.
2. ஒவ்வொரு கார்டிலும் மாதிரி எண், consignment, சேகரித்த அதிகாரி, தேதி, பயிர் ஆகியவை இருக்கும்.
3. வண்ண லேபிள்கள் **Paid** அல்லது **Free**, மாதிரியின் நிலை, **Report: Ready** அல்லது **Report: Not Ready** ஆகியவற்றைக் காட்டும்.
4. ஒரு மாதிரியைக் கண்டுபிடிக்க, **Search sample, consignment, crop**-ல் டைப் செய்யுங்கள், தேதிகளைத் தேர்வு செய்யுங்கள், அல்லது வகை, நிலை அல்லது பயிரைத் தேர்வு செய்யுங்கள்.
5. மாதிரியைத் திறக்க **View Details** தட்டுங்கள்.
6. விவரங்கள் பக்கத்தில் **Sample Details**, **Farmer Details**, **Payment Details**, மற்றும் **Sample Collected by MO** முதல் **Report Ready for Download** வரையிலான **Sample Journey** படிகளைப் பாருங்கள்.
7. அறிக்கை தயாரானதும் **Download PDF** தட்டுங்கள். தமிழ் அல்லது ஆங்கிலம் தேர்வு செய்ய அதன் பக்கத்தில் உள்ள சிறிய அம்புக்குறியைத் தட்டுங்கள்.

## நிலை (Status) என்றால் என்ன

- **Payment Pending**: பணம் கட்டுவது இன்னும் முடியவில்லை.
- **Awaiting Dispatch**: லேபுக்கு அனுப்பக் காத்திருக்கிறது.
- **In Transit**: லேபுக்குப் போகும் வழியில் உள்ளது.
- **Under Analysis**: லேப் பரிசோதித்துக்கொண்டிருக்கிறது.
- **Report Ready**: நீங்கள் அறிக்கையைப் பதிவிறக்கலாம்.

## பொதுவான பிரச்சனைகள்

- **No samples yet** (இன்னும் மாதிரிகள் இல்லை): உங்கள் அதிகாரி உங்கள் பெயரில் இன்னும் மாதிரி எதுவும் சேர்க்கவில்லை. உங்கள் MO அதிகாரியிடம் கேளுங்கள்.
- **Download PDF** இல்லை: லேப் இன்னும் அறிக்கையை முடிக்கவில்லை.
:::

::: section farmer-my-reports
title: உங்கள் மண் பரிசோதனை அறிக்கையைப் பதிவிறக்குவது (விவசாயி)
routes: /Sas/MyReports
for: 
roles: Farmer
designations: 
screenshot: farmer-my-reports.png
screenshot-phone: farmer-my-reports.phone.jpg
minutes: 2
source-hash: 40b826ab2246cad3
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் மாதிரிகளுக்கு லேப் தயாரித்த பரிசோதனை அறிக்கைகளை **My Reports** பட்டியலிடும். ஒவ்வொரு அறிக்கையையும் தமிழிலோ ஆங்கிலத்திலோ பதிவிறக்கலாம்.

1. மெனுவில் இருந்து **My Reports**-ஐத் திறங்கள்.
2. கார்டுகள் **Total Reports**, **Ready to Download**, **Downloaded** மற்றும் **Under Process**-ஐக் காட்டும்.
3. **Reports List**-ல் உங்கள் அறிக்கையைக் கண்டுபிடியுங்கள். பட்டியலைச் சுருக்க **Search report, sample, crop**, தேதிகள், **All Statuses** அல்லது **All Crops** பயன்படுத்துங்கள்.
4. அறிக்கையைச் சேமிக்க **Download PDF** தட்டுங்கள். தமிழ் அல்லது ஆங்கிலம் தேர்வு செய்ய அதன் பக்கத்தில் உள்ள சிறிய அம்புக்குறியைத் தட்டுங்கள்.
5. **Report Details**-ஐப் படிக்க கண் ஐகானைத் தட்டுங்கள் (போனில் **View**): பரிசோதனை முடிவுகள், பரிந்துரைகள், பயிர் ஆலோசனை.

## புலங்களின் விளக்கம்

- **Reports No.**: அறிக்கையின் எண்.
- **Sample No.**: இந்த அறிக்கை எந்த மாதிரிக்கானது.
- **Status**: **Report Ready** (இன்னும் பதிவிறக்கவில்லை), **Downloaded** அல்லது **Printed**.
- **Under Process**: லேபில் இன்னும் பரிசோதனையில் உள்ள மாதிரிகள்.

## பொதுவான பிரச்சனைகள்

- **No reports yet** (இன்னும் அறிக்கைகள் இல்லை): லேப் இன்னும் முடிக்கவில்லை. எத்தனை மாதிரிகள் பரிசோதனையில் உள்ளன என்று செய்தி சொல்லும்.
- **Reports are not available right now** (இப்போது அறிக்கைகள் கிடைக்கவில்லை): இணைய இணைப்பைச் சரிபார்த்து **Try again** தட்டுங்கள்.
- கோப்பு திறக்கவில்லை: உங்கள் போனின் downloads ஃபோல்டரைப் பாருங்கள்.
:::

::: section farmer-payments
title: உங்கள் மாதிரிப் பணத்தைச் சரிபார்ப்பது (விவசாயி)
routes: /Sas/Payments, /Sas/Payments/{id}
for: 
roles: Farmer
designations: 
minutes: 2
source-hash: f07974a161df2664
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் பணம் கட்டும் மண் மாதிரிகளுக்குச் செலுத்திய பணத்தையும், SPIC அதற்கு ஒப்புதல் தந்து பெற்றுக்கொண்டதா என்பதையும் **Payments** காட்டும். இங்கே நீங்கள் பார்க்க மட்டுமே செய்வீர்கள்; எதையும் நிரப்ப வேண்டியதில்லை.

1. மெனுவில் இருந்து **Payments**-ஐத் திறங்கள்.
2. கார்டுகள் **Total Paid Samples**, **Finance Verified**, **Approval Pending** மற்றும் **Payment Issues**-ஐக் காட்டும்.
3. **Search sample no., transaction ID...** மூலம் தேடுங்கள், அல்லது தேதிகள், நிலை அல்லது பயிரைத் தேர்வு செய்யுங்கள்.
4. **Payment Details**-ஐத் திறக்க மாதிரி எண், கண் ஐகான் அல்லது **View Details** தட்டுங்கள்.
5. தொகை, **Payment Mode**, **Transaction ID**, **Payment Proof**-ன் கீழ் உள்ள ரசீது, **Verification Journey** ஆகியவற்றைப் பாருங்கள்.

## நிலை (Status) என்றால் என்ன

- **Admin Approval Pending**: உங்கள் பணம் ஒப்புதலுக்குக் காத்திருக்கிறது.
- **Admin Approved**: ஒப்புதல் கிடைத்தது, Finance-க்காகக் காத்திருக்கிறது.
- **Finance Verified**: பணம் முழுமையாக உறுதி செய்யப்பட்டது.
- **Payment Issue**: பணம் திருப்பி அனுப்பப்பட்டது அல்லது பொருந்தவில்லை. உங்கள் MO அதிகாரி அதைச் சரி செய்வார்.

## பொதுவான பிரச்சனைகள்

- **No payments found** (பணம் எதுவும் இல்லை): உங்களுக்குப் பணம் கட்டும் மாதிரிகள் இல்லை, அல்லது உங்கள் மாதிரிகள் இலவசம்.
- **Payment Issue** நீண்ட நாள் இருக்கிறது: **Transaction ID**-உடன் உங்கள் MO அதிகாரியைத் தொடர்பு கொள்ளுங்கள்.
:::

::: section approve-payments
title: மாதிரிப் பணத்துக்கு ஒப்புதல் தந்து Finance-க்கு அனுப்புவது
routes: /Sas/Payments, /Sas/Payments/{id}
for: SasPaymentApproval.View
roles: 
designations: 
minutes: 3
source-hash: 2c0a5e7b4b9a51d2
confidence: ok

**இந்தத் திரை எதற்கு**
பணம் கட்டும் மாதிரிகளுக்குக் களப் பணியாளர்கள் பதிவு செய்த பணத்தை **Payment Approval** பட்டியலிடும். ஒவ்வொரு ரசீதையும் தொகையையும் சரிபார்த்து, ஒப்புதல் தந்து Finance-க்கு அனுப்புவீர்கள், அல்லது அதிகாரிக்குத் திருப்பி அனுப்புவீர்கள்.

1. மெனுவில் இருந்து **Payment Approval**-ஐத் திறங்கள்.
2. **Pending Admin Approval** டேபைத் தட்டுங்கள்.
3. **Payment Approval Details**-ஐத் திறக்க ஒரு **Payment Ref No.** அல்லது கண் ஐகானைத் தட்டுங்கள்.
4. **Expected Amount**, **Paid Amount**, **Difference** ஆகியவற்றை ஒப்பிட்டு, **Payment Proof Preview**-ன் கீழ் உள்ள ரசீதைச் சரிபாருங்கள்.
5. **Approve & Forward to Finance** தட்டுங்கள்.
6. **Verified Amount** (அல்லது **Other amount**) தேர்வு செய்து, **Remarks** சேர்த்து, **I confirm that the payment proof, transaction details and amount are verified.** (பண ஆதாரம், பரிவர்த்தனை விவரங்கள், தொகை சரிபார்க்கப்பட்டன என்று உறுதி செய்கிறேன்) என்பதை டிக் செய்து, **Approve & Forward** தட்டுங்கள்.
7. நிராகரிக்க, **Reject Payment** தட்டி, **Reason for rejection** எழுதி, மீண்டும் **Reject Payment** தட்டுங்கள்.

## புலங்களின் விளக்கம்

- **Admin Status**: **Pending Review**, **Admin Approved** அல்லது **Returned to MO**.
- **Finance Status**: **Not Forwarded**, **Pending**, **Verified** அல்லது **Mismatch**.
- **Difference**: கட்டிய தொகை எதிர்பார்த்த தொகையாக இல்லாவிட்டால் சிவப்பாகக் காட்டும்.

## பொதுவான பிரச்சனைகள்

- "Tick the confirmation to continue" (தொடர உறுதிப்படுத்தலை டிக் செய்யுங்கள்): ஒப்புதல் தரும் முன் பெட்டியை டிக் செய்யுங்கள்.
- "Enter a verified amount greater than zero" (பூஜ்ஜியத்தை விட அதிகமான சரிபார்த்த தொகையை உள்ளிடுங்கள்): தொகையைத் தேர்வு செய்யுங்கள் அல்லது டைப் செய்யுங்கள்.
- செயல் பட்டன்கள் இல்லை: இந்தப் பணம் பற்றி ஏற்கனவே முடிவு எடுக்கப்பட்டுவிட்டது.
- பழைய பணப் பதிவுகளைக் கண்டுபிடிக்க **Filters** (தேதிகள், **State**, **Region**, **Payment Mode**, **Status**) பயன்படுத்துங்கள்.
:::

::: section verify-payments
title: வந்த பணத்தைச் சரிபார்ப்பது (Finance)
routes: /Sas/Payments, /Sas/Payments/{id}, /Sas/Payments/History
for: SasPaymentVerification.View
roles: 
designations: 
minutes: 3
source-hash: 788890513e2baf14
confidence: ok

**இந்தத் திரை எதற்கு**
ஒப்புதல் தருபவர் Finance-க்கு அனுப்பிய பணத்தை **Payment Verification** பட்டியலிடும். பணம் கணக்குக்கு வந்து சேர்ந்ததை நீங்கள் உறுதி செய்வீர்கள். நீங்கள் முடித்த எல்லாப் பணப் பதிவுகளும் **Payment History**-ல் இருக்கும்.

1. மெனுவில் இருந்து **Payment Verification**-ஐத் திறங்கள்.
2. **Pending Verification** டேபைத் தட்டுங்கள்.
3. ஒரு பணப் பதிவை அதன் **Payment Ref No.** அல்லது கண் ஐகான் மூலம் திறங்கள். **Payment Proof Details** திறக்கும்.
4. **Payment Proof Preview**-ன் கீழ் உள்ள ரசீதை உங்கள் வங்கி அறிக்கையுடன் (bank statement) ஒப்பிட்டுப் பாருங்கள்.
5. பணம் வந்திருந்தால், **Verify Payment** தட்டுங்கள். **Verified Amount** தேர்வு செய்து, **Received Date** அமைத்து, **Remarks** சேர்த்து, **I have verified the payment proof, transaction ID, and amount.** (பண ஆதாரம், transaction ID, தொகையைச் சரிபார்த்தேன்) என்பதை டிக் செய்து, **Confirm** தட்டுங்கள்.
6. பணத்தைக் கண்டுபிடிக்க முடியவில்லை என்றால், **Mark Mismatch** தட்டி, **Mismatch remarks** எழுதி, மீண்டும் **Mark Mismatch** தட்டுங்கள்.
7. முடித்த பணப் பதிவுகளைப் பார்க்க **Payment History**-ஐத் திறங்கள். **All**, **Verified**, **Failed**, **Mismatch** டேப்களைப் பயன்படுத்துங்கள்.

## புலங்களின் விளக்கம்

- **Verified Amount**: உண்மையில் வந்த தொகை. எதிர்பார்த்ததை விடக் குறைவாக இருந்தால் mismatch ஆகச் சேமிக்கப்படும்.
- **Payment Verified**: பணம் உறுதி செய்யப்பட்டது.
- **Amount Mismatch**: எதிர்பார்த்ததை விடக் குறைவான பணம் வந்தது.
- **Failed**: **Mark Mismatch** மூலம் குறிக்கப்பட்டது; பணம் எதுவும் கிடைக்கவில்லை.

## பொதுவான பிரச்சனைகள்

- "Enter the amount received" (வந்த தொகையை உள்ளிடுங்கள்): தொகையைத் தேர்வு செய்யுங்கள் அல்லது டைப் செய்யுங்கள்.
- "Tick the confirmation to continue" (தொடர உறுதிப்படுத்தலை டிக் செய்யுங்கள்): உறுதி செய்யும் முன் பெட்டியை டிக் செய்யுங்கள்.
- செயல் பட்டன்கள் இல்லை: இந்தப் பணம் ஏற்கனவே சரிபார்க்கப்பட்டுவிட்டது அல்லது குறிக்கப்பட்டுவிட்டது.
:::
