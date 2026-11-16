---
module: schemes
title: ஸ்கீம்கள் மற்றும் லக்கி டிரா
icon: bi-gift
order: 104
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
source-hash: 01c42ebca4d8bbab
confidence: ok

ஒரு ஸ்கீம், SPIC பொருட்களை வாங்கும் டீலர்களுக்கும் விவசாயிகளுக்கும் பரிசு தரும். ஒவ்வொரு முறை வாங்கும்போதும் ஒரு லக்கி டிரா என்ட்ரி கிடைக்கும். டிரா தேதியில் வெற்றியாளர்கள் தேர்வு செய்யப்படுவார்கள், அவர்களின் பரிசு ஆப்பில் தெரியும்.

- create | icon: bi-pencil-square | color: #d97706
  title: ஸ்கீம் குழு அதை அமைக்கும்
  text: பெயர், இடங்கள், product விதிகள், பரிசு, டிரா தேதிகள்.
- join | icon: bi-qr-code-scan | color: #0d9488
  title: டீலர்களும் விவசாயிகளும் பங்கேற்பார்கள்
  text: product-ல் உள்ள QR code-ஐ ஸ்கேன் செய்யுங்கள். ஒவ்வொரு வாங்குதலுக்கும் 1 என்ட்ரி கிடைக்கும்.
- win | icon: bi-trophy | color: #7c3aed
  title: லக்கி டிரா வெற்றியாளர்களைத் தேர்வு செய்யும்
  text: வெற்றியாளர்களுக்கு ஒரு செய்தி, பரிசு, அதை எப்படிப் பெறுவது என்பது தெரியும்.
:::

::: section flow
title: ஒரு ஸ்கீம் எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 801d9ad88aaa016d
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- team | type: lane | color: #d97706 | roles: Admin, CorporateAdmin
  title: ஸ்கீம் குழு
  sub: அலுவலகப் பணியாளர்கள்
- people | type: lane | color: #0d9488 | roles: Dealer, Farmer
  title: டீலர்களும் விவசாயிகளும்
  sub: ஸ்கீம் உறுப்பினர்கள்
- create | lane: team | col: 1 | icon: bi-pencil-square | color: #64748b | next: review | screen: add-scheme
  title: ஸ்கீமை உருவாக்குதல்
  status: Draft
  text: **Create Scheme** படிவத்தில் இடங்கள், product விதிகள், பரிசு, டிரா அமைப்புகளை நிரப்பி, **Next** அழுத்துங்கள்.
- review | lane: team | col: 2 | icon: bi-clipboard-check | color: #d97706 | next: scan | screen: review-scheme
  title: சரிபார்த்துத் தொடங்குதல்
  status: Active
  text: **Review Your Scheme**-ல் ஒவ்வொரு விவரத்தையும் சரிபார்த்து, ஸ்கீமைத் தொடங்க **Active Scheme** அழுத்துங்கள்.
- scan | lane: people | col: 3 | icon: bi-qr-code-scan | color: #0d9488 | next: draw | screen: lucky-draw
  title: வாங்கி ஸ்கேன் செய்தல்
  status: Ticket Activated
  text: product-ல் உள்ள QR code-ஐ ஸ்கேன் செய்து, வாங்கிய product-களைத் தேர்வு செய்யுங்கள். ஒவ்வொரு வாங்குதலுக்கும் 1 லக்கி டிரா என்ட்ரி கிடைக்கும்.
- draw | lane: team | col: 4 | icon: bi-ticket-perforated | color: #2563eb | next: announce | screen: scheme-overview
  title: லக்கி டிரா
  status: Selected
  text: டிரா தேதியில், எல்லா என்ட்ரிகளிலிருந்தும் வெற்றியாளர்கள் தேர்வு செய்யப்படுவார்கள். ஸ்கீமில் அமைத்தபடி டிரா **Automatic** அல்லது **Manual** ஆக இருக்கும்.
- announce | lane: people | col: 5 | icon: bi-megaphone | color: #7c3aed | next: prize | screen: winner-announcement
  title: வெற்றியாளருக்குத் தெரிவித்தல்
  status: Notified
  text: வெற்றியாளருக்குப் பரிசு மற்றும் ஸ்கீம் பெயருடன் **Congratulations!** என்று தெரியும்.
- prize | lane: people | col: 6 | icon: bi-gift | color: #16a34a | screen: winner-details
  title: பரிசைப் பெறுதல்
  status: Prize Distributed
  text: **Winner Details**-ல் பரிசு, அதன் நிலை, அது கொடுக்கப்பட்ட தேதி தெரியும்.
- not-won | lane: people | col: 4 | icon: bi-emoji-smile | color: #ea580c | kind: exception | from: draw | to: scan
  title: இந்த முறை தேர்வாகவில்லை
  status: Better Luck Next Time!
  text: தொடர்ந்து வாங்கி ஸ்கேன் செய்யுங்கள். என்ட்ரிகள் அதிகமானால் அடுத்த டிராவில் வாய்ப்பும் அதிகம்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 663b2eff26f57f93
confidence: ok

- draft | color: #64748b
  label: Draft
  text: ஸ்கீம் சேமிக்கப்பட்டது, ஆனால் வெளியிடப்படவில்லை. டீலர்களுக்கும் விவசாயிகளுக்கும் இது தெரியாது.
- active-draft | color: #d97706
  label: ACTIVE DRAFT
  text: ஸ்கீமைத் தொடங்கும் முன் review திரையில் தெரியும்.
- active | color: #16a34a
  label: Active
  text: ஸ்கீம் நடந்துகொண்டிருக்கிறது. ஒரு என்ட்ரியில் இது இருந்தால், அந்த என்ட்ரி இந்தச் சுற்றில் இன்னும் வெல்லலாம்.
- participating | color: #0d9488
  label: Participating
  text: டீலர் அல்லது விவசாயி ஸ்கீமில் பங்கேற்கிறார்.
- verified | color: #0891b2
  label: Verified
  text: வாங்குதல் அல்லது டீலர் சரிபார்க்கப்பட்டுவிட்டது.
- ticket | color: #2563eb
  label: Ticket Activated
  text: உங்கள் வாங்குதலுக்கு ஒரு புதிய லக்கி டிரா என்ட்ரி சேர்க்கப்பட்டது.
- used | color: #64748b
  label: Used
  text: இந்த என்ட்ரி ஏற்கனவே ஒரு டிராவில் சேர்ந்துவிட்டது.
- expired | color: #475569
  label: Expired
  text: இந்த என்ட்ரியால் இனி வெல்ல முடியாது.
- selected | color: #7c3aed
  label: Selected
  text: டிராவில் வெற்றியாளராகத் தேர்வு செய்யப்பட்டது.
- notified | color: #d97706
  label: Notified
  text: வெற்றியாளருக்குத் தெரிவிக்கப்பட்டது.
- claimed | color: #16a34a
  label: Claimed
  text: வெற்றியாளர் பரிசைப் பெற்றுக்கொண்டார்.
- distributed | color: #16a34a
  label: Prize Distributed
  text: பரிசு வெற்றியாளருக்குக் கொடுக்கப்பட்டது.
- completed | color: #16a34a
  label: Completed
  text: இந்தத் தேதியின் டிரா முடிந்தது.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 89f40baf961ca43b
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- team | color: #d97706 | roles: Admin, CorporateAdmin
  title: ஸ்கீம் குழு
  - ஸ்கீம்களை உருவாக்குவது, சரிபார்ப்பது
  - ஸ்கீமைத் தொடங்குவது
  - டீலர்கள், என்ட்ரிகள், வெற்றியாளர்களைக் கண்காணிப்பது
- dealer | color: #0d9488 | roles: Dealer
  title: டீலர்
  - product-களை ஸ்கேன் செய்து என்ட்ரிகள் சேர்ப்பது
  - உங்கள் என்ட்ரிகளையும் முன்னேற்றத்தையும் பார்ப்பது
  - வெற்றியாளர்களையும் உங்கள் பரிசையும் பார்ப்பது
- farmer | color: #7c3aed | roles: Farmer
  title: விவசாயி
  - நீங்கள் வாங்கிய product-களை ஸ்கேன் செய்வது
  - உங்கள் என்ட்ரிகளைப் பார்ப்பது
  - வெற்றியாளர்களையும் உங்கள் பரிசையும் பார்ப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9ef969d93e7ac18c
confidence: ok

- how-to-win | popular: true
  q: வெல்லும் வாய்ப்பு எனக்கு எப்படிக் கிடைக்கும்?
  a: ஒவ்வொரு வாங்குதலுக்கும் உங்களுக்கு 1 என்ட்ரி கிடைக்கும். என்ட்ரிகள் 100, 100 ஆகச் சுற்றுகளாக எண்ணப்படும். **Grand Draw**-ஐத் திறக்க 100 என்ட்ரிகளை எட்டுங்கள். லக்கி டிரா பக்கத்தில் **How It Works**-ஐப் பாருங்கள்.
- entries-count | popular: true
  q: என்னிடம் எத்தனை என்ட்ரிகள் உள்ளன?
  a: **Lucky Draw**-ஐத் திறந்து **View All Entries** அழுத்துங்கள் (போனில் **View Entries**). இந்தச் சுற்றில் உங்கள் active என்ட்ரிகளை **TOTAL ENTRIES** கார்டு காட்டும்.
- not-won
  q: எனக்கு "Better Luck Next Time!" என்று வருகிறது
  a: இந்த டிராவில் நீங்கள் தேர்வாகவில்லை. உங்கள் வாங்குதல்களைத் தொடர்ந்து ஸ்கேன் செய்யுங்கள். என்ட்ரிகள் அதிகமானால் அடுத்த முறை வாய்ப்பும் அதிகம்.
- prize
  q: நான் வென்றேன். என் பரிசை எப்படிப் பெறுவது?
  a: **Winner Details**-ஐத் திறங்கள். **Prize Information**-ல் பரிசும் அதன் **Status**-ம் தெரியும். உதவிக்கு, **Need Help?** பெட்டியில் **Contact Support** அழுத்துங்கள்.
- change-scheme
  q: ஸ்கீம் தொடங்கும் முன் அதை மாற்ற முடியுமா?
  a: முடியும். **Review Scheme**-ல், ஒரு product விதியைத் திருத்த பென்சிலையும், நீக்க குப்பைத்தொட்டியையும் பயன்படுத்துங்கள். தயாராகும் வரை வெளியிடாமல் வைத்திருக்க **Save Draft** அழுத்துங்கள்.
:::

::: section scheme-list
title: எல்லா ஸ்கீம்களையும் பார்ப்பது
routes: /Schemes
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-stats] | 2 > [data-help=sch-filters] | 3 > [data-help=sch-grid] | 4 > [data-help=sch-view] | 5 > [data-help=sch-create]
minutes: 2
source-hash: 799c692a3e1f6262
confidence: ok

**இந்தத் திரை எதற்கு**
**Schemes** திரையில் எல்லா லக்கி டிரா ஸ்கீம்களும் தெரியும், மேலே மொத்த எண்ணிக்கைகளும் இருக்கும். பக்க மெனுவில் **Scheme List** அழுத்தி இதைத் திறங்கள்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Schemes**, **Overall Dealer**, **Participating dealer**, **Winners**.
2. ஒரு ஸ்கீமைக் கண்டுபிடிக்க **Search Schemes....** பெட்டியில் டைப் செய்யுங்கள். வகை (**Both**, விவசாயி அல்லது டீலர்), நிலை (**All Status**), மாநிலம் (**All States**) ஆகியவற்றைத் தேர்வு செய்யப் பட்டியல்களைப் பயன்படுத்துங்கள்.
3. ஒவ்வொரு ஸ்கீம் கார்டிலும் பெயர், யாருக்கானது, நிலை (உதாரணமாக **Active**), தேதிகள், மாநிலம், மாவட்டம், **Participating dealer** மற்றும் **Winners** எண்ணிக்கைகள், **THE GRAND REWARD** ஆகியவை தெரியும்.
4. ஒரு கார்டில் **View Details** அழுத்தி அதன் **Scheme Overview**-ஐத் திறங்கள்.
5. புதிய ஸ்கீம் உருவாக்க **Create Scheme** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **ஒரு ஸ்கீமைக் கண்டுபிடிக்க முடியவில்லை.** தேடல் பெட்டியைக் காலி செய்து, பட்டியல்களை மீண்டும் **Both**, **All Status**, **All States** என்று வையுங்கள்.
- **என் மெனுவில் Scheme List இல்லை.** உங்கள் பதவிக்கு (designation) ஸ்கீம்களைப் பார்க்க அனுமதி இல்லை. உங்கள் admin-இடம் கேளுங்கள்.
:::

::: section add-scheme
title: ஸ்கீம் உருவாக்குவது
routes: /AddScheme
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-add-type] | 2 > [data-help=sch-add-basic] | 3 > [data-help=sch-add-rules] | 4 > [data-help=sch-add-prize] | 5 > [data-help=sch-add-draw] | 6 > [data-help=sch-add-next]
minutes: 5
source-hash: 6e0920ccd77cef79
confidence: ok

**இந்தத் திரை எதற்கு**
புதிய லக்கி டிரா ஸ்கீமை அமைக்க **Create Scheme** திரையைப் பயன்படுத்துங்கள்: யாருக்கானது, எங்கே நடக்கும், எந்த product-கள் கணக்கில் வரும், பரிசு என்ன, டிரா எப்போது.

1. மேலே, ஸ்கீம் யாருக்கானது என்று தேர்வு செய்யுங்கள்: **Both**, **Farmer** அல்லது **Dealer**.
2. **Scheme Type & Basic Info**-ன் கீழ், **Scheme Name** மற்றும் **Draw points** உள்ளிட்டு, **State**, **District**, **Dealer** தேர்வு செய்யுங்கள். **Start Date** மற்றும் **End Date** தேர்வு செய்யுங்கள். **Scheme Status** பெட்டியில் **Draft - Scheme is not published** என்று தெரியும்.
3. **Add Product Rule**-ன் கீழ், **Category** மற்றும் **Product** தேர்வு செய்து, **Dealer Quantity** மற்றும் **Farmer Quantity** உள்ளிட்டு, **+ Add Rule** அழுத்துங்கள். ஒவ்வொரு விதியும் **Product Rules List**-ல் தெரியும். ஒரு விதியை நீக்க குப்பைத்தொட்டியையும், திருத்த பென்சிலையும் பயன்படுத்துங்கள்.
4. **Winner Prize Details**-ன் கீழ், **Prize Name** மற்றும் **Prize Description** உள்ளிட்டு, பரிசின் புகைப்படம் சேர்க்க **Click to upload image** தட்டுங்கள்.
5. **Lucky Draw Configuration**-ன் கீழ், **Automatic** அல்லது **Manual** தேர்வு செய்து, **Frequency** (**Daily**, **Weekly**, **Monthly** அல்லது **Custom**) தேர்வு செய்து, **Date** மற்றும் **Draw Time** அமையுங்கள்.
6. பிறகு தொடர வைத்துக்கொள்ள **Save Draft** அழுத்துங்கள், அல்லது ஸ்கீமைச் சரிபார்க்க **Next** அழுத்துங்கள்.

> Tip: பரிசின் பெயர், விவரம், புகைப்படம் ஆகியவை ஸ்கீம் பக்கத்திலும், வெற்றியாளர் செய்தியிலும், டிரா அறிவிப்பிலும் டீலர்களுக்குத் தெரியும்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Scheme Name** | எல்லோரும் பார்க்கும் ஒரு சுருக்கமான பெயர். |
| **Draw points** | டிராவுக்குத் தகுதி பெற ஒரு உறுப்பினருக்குத் தேவையான points. |
| **State**, **District**, **Dealer** | ஸ்கீம் எங்கே நடக்கும். ஒன்றுக்கு மேற்பட்டவற்றைத் தேர்வு செய்யலாம். |
| **Dealer Quantity**, **Farmer Quantity** | இந்த விதிக்கு ஒரு டீலர் அல்லது விவசாயி எவ்வளவு product வாங்க வேண்டும். |
| **Prize Image** | ஒரே ஒரு JPG அல்லது PNG புகைப்படம், 5 MB வரை. |
| **Draw Time** | டிரா நடக்கும் நேரம். |

## பொதுவான பிரச்சனைகள்

- **புகைப்படம் அப்லோட் ஆகவில்லை.** 5 MB-க்குக் குறைவான JPG அல்லது PNG பயன்படுத்துங்கள்.
- **ஒரு படி பின்னால் போக வேண்டும்.** **Previous Step** அழுத்துங்கள்.
:::

::: section review-scheme
title: ஸ்கீமைச் சரிபார்த்துத் தொடங்குவது
routes: /ReviewScheme
for: 
roles: 
designations: 
minutes: 3
source-hash: 5c87d7eaecfc2110
confidence: ok

**இந்தத் திரை எதற்கு**
உருவாக்கும் படிவத்தில் **Next** அழுத்திய பிறகு, **Review Scheme** திரை புதிய ஸ்கீமின் எல்லா விவரங்களையும் ஒரே இடத்தில் காட்டும். அதனால் தொடங்கும் முன் சரிபார்க்கலாம்.

1. **Review Your Scheme** பேனரைப் படியுங்கள்.
2. **Scheme Details**-ன் கீழ், **APPLICABLE FOR:**, நிலை (**ACTIVE DRAFT**), ஸ்கீம் பெயர், உருவாக்கிய தேதி, விவரம் ஆகியவற்றைச் சரிபாருங்கள்.
3. **Campine Time Line**-ன் கீழ் **Start Date** மற்றும் **End Date**-ஐயும், அதன் பக்கத்தில் உள்ள பரிசு கார்டையும் சரிபாருங்கள்.
4. **Product Rules** அட்டவணையைச் சரிபாருங்கள். ஒரு விதியைத் திருத்த பென்சிலையும், நீக்க குப்பைத்தொட்டியையும் பயன்படுத்துங்கள்.
5. **Target Segment** (**State**, **District**, **Dealers**) மற்றும் **Review Checklist** (**Draw Points**, **Product Points**, **Draw Mode**, **Frequency**, **Draw Date**, **Draw Time**) ஆகியவற்றைச் சரிபாருங்கள்.
6. வெளியிடாமல் வைத்திருக்க **Save Draft** அழுத்துங்கள், அல்லது ஸ்கீமைத் தொடங்க **Active Scheme** அழுத்துங்கள்.

> Careful: **Active Scheme** அழுத்திய பிறகு, டீலர்களுக்கும் விவசாயிகளுக்கும் ஸ்கீம் தெரியும், அவர்கள் என்ட்ரிகள் சேர்க்கத் தொடங்கலாம்.

## பொதுவான பிரச்சனைகள்

- **ஒரு விவரம் தவறாக உள்ளது.** **Active Scheme** அழுத்தும் முன், உருவாக்கும் படிவத்துக்குத் திரும்பிப் போய் அதைத் திருத்துங்கள்.
- **நான் இன்னும் தொடங்கத் தயாராக இல்லை.** **Save Draft** அழுத்துங்கள். ஸ்கீம் டிராஃப்டாகவே இருக்கும்.
:::

::: section scheme-overview
title: ஒரு ஸ்கீமைக் கண்காணிப்பது
routes: /SchemeOverview
for: 
roles: 
designations: 
minutes: 3
source-hash: 55ace08ba0ddb992
confidence: ok

**இந்தத் திரை எதற்கு**
**Scheme Overview** திரை ஒரு ஸ்கீம் எப்படிப் போகிறது என்று காட்டும்: யார் பங்கேற்கிறார்கள், எத்தனை என்ட்ரிகள் உள்ளன, யார் வென்றார்கள், முந்தைய எல்லா டிராக்களும். ஸ்கீம் பட்டியலில் **View Details** அழுத்தி இதைத் திறங்கள்.

1. மேலே, ஸ்கீம் பெயர், யாருக்கானது, நிலை (உதாரணமாக **Active**), **States**, **Duration** ஆகியவற்றைப் பாருங்கள்.
2. கார்டுகளைப் பாருங்கள்: **Participating dealer**, **Total Entries**, **Winners**.
3. **Overview** டேபில், grand reward-ஐயும் **Draw Setting**-ஐயும் பாருங்கள்: **Draw Mode**, **Frequency**, **Next Draw Date**, **Last Draw Date**.
4. **Participating dealer** டேபில், தேடுங்கள் அல்லது ஒரு மாநிலத்தைத் தேர்வு செய்யுங்கள். ஒவ்வொரு உறுப்பினரும் அவரது **Points** மற்றும் **Entries**-உடன் தெரிவார். மேலும் பார்க்க ஒரு கார்டைத் தட்டுங்கள்.
5. **Winners** டேபில், ஒவ்வொரு வெற்றியாளரையும் நிலை (**Selected**, **Notified** அல்லது **Claimed**), **Prize**, **Draw Date** ஆகியவற்றுடன் பாருங்கள். மேலும் பார்க்க ஒரு கார்டைத் தட்டுங்கள்.
6. **Draw History** டேபில், முந்தைய ஒவ்வொரு டிராவையும் அதன் தேதி, **Completed**, பரிசு, எத்தனை வெற்றியாளர்கள் தேர்வானார்கள் என்பதுடன் பாருங்கள்.

## பொதுவான பிரச்சனைகள்

- **Winners டேப் காலியாக உள்ளது.** இந்த ஸ்கீமுக்கு இன்னும் எந்த டிராவும் நடக்கவில்லை. **Overview** டேபில் **Next Draw Date**-ஐப் பாருங்கள்.
- **ஒரு உறுப்பினர் பட்டியலில் இல்லை.** **All States** தேர்வு செய்து, தேடல் பெட்டியைக் காலி செய்யுங்கள்.
:::

::: section scan-qr
title: Product-ன் QR code-ஐ ஸ்கேன் செய்வது
routes: /qr-scanner
for: 
roles: 
designations: 
minutes: 1
source-hash: 4ad0b8818ad680ef
confidence: ok

**இந்தத் திரை எதற்கு**
நீங்கள் வாங்கிய SPIC product-ல் உள்ள QR code-ஐ ஸ்கேன் செய்ய **QR Scanner**-ஐப் பயன்படுத்துங்கள். இப்படித்தான் ஒரு வாங்குதல் லக்கி டிராவுக்குக் கணக்கில் வரும்.

1. Product-ல் உள்ள QR code-க்கு மேல் போனைப் பிடியுங்கள். Code சதுரக் கட்டத்துக்குள் இருக்கட்டும்.
2. இருட்டாக இருந்தால், ஃபிளாஷுக்கு மின்னல் ஐகானைத் தட்டுங்கள். கேமராவை மாற்ற கேமரா ஐகானைத் தட்டுங்கள்.
3. Code சிறியதாகவோ தூரத்திலோ இருந்தால், zoom பட்டையை **+** பக்கம் நகர்த்துங்கள்.
4. ஏற்கனவே உள்ள ஒரு புகைப்படத்திலிருந்து code-ஐ ஸ்கேன் செய்ய, படம் ஐகானைத் தட்டுங்கள்.
5. உங்கள் லக்கி டிரா என்ட்ரிகளைப் பார்க்க **View Entries** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Code படிக்கப்படவில்லை.** கேமராவைத் துடைத்து, ஃபிளாஷை ஆன் செய்து, சுமார் ஒரு கை நீள தூரத்தில் போனை அசையாமல் பிடியுங்கள்.
- **கேமரா திறக்கவில்லை.** உங்கள் போன் settings-ல், கேமராவைப் பயன்படுத்த ஆப்புக்கு அனுமதி கொடுங்கள்.
:::

::: section scan-product
title: ஸ்கேன் செய்த product-களை உள்ளிடுவது
routes: /Scanproduct
for: 
roles: 
designations: 
minutes: 2
source-hash: c9f9991efd4bd6c1
confidence: ok

**இந்தத் திரை எதற்கு**
ஸ்கேன் செய்த பிறகு, இந்தத் திரையில் நிறுவனமும் அதன் product-களும் தெரியும். ஒவ்வொரு product-ஐயும் எத்தனை வாங்கினீர்கள் என்று உள்ளிட்டுச் சமர்ப்பியுங்கள்.

1. மேலே உள்ள நிறுவன கார்டைச் சரிபாருங்கள்: பெயர், **VERIFIED**, முகவரி, போன், email.
2. **Products**-ன் கீழ், நீங்கள் வாங்கிய ஒவ்வொரு product-ஐயும் கண்டுபிடியுங்கள்.
3. ஒன்றைச் சேர்க்க **+** அழுத்துங்கள். ஒன்றைக் குறைக்க **−** அழுத்துங்கள். இரண்டுக்கும் நடுவில் உள்ள எண்தான் அளவு.
4. **Submit** அழுத்துங்கள்.
5. மீண்டும் ஸ்கேன் செய்ய, **Back To Scan** பக்கத்தில் உள்ள பின் அம்புக்குறியைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **அளவு பூஜ்ஜியத்துக்குக் கீழே போகவில்லை.** அது சாதாரணம்தான். பூஜ்ஜியம் என்றால் அந்த product-ஐ நீங்கள் வாங்கவில்லை.
- **தவறான நிறுவனம் தெரிகிறது.** **Back To Scan**-க்குப் போய், சரியான QR code-ஐ ஸ்கேன் செய்யுங்கள்.
:::

::: section select-products
title: டீலரிடம் வாங்கிய product-களைத் தேர்வு செய்வது
routes: /SelectPurchasedProducts
for: 
roles: 
designations: 
minutes: 2
source-hash: 79749dd21c644750
confidence: ok

**இந்தத் திரை எதற்கு**
**Select Purchased Products** திரையில் நீங்கள் வாங்கிய டீலர், உங்கள் லக்கி டிரா முன்னேற்றம், டீலர் விற்கும் product-கள் தெரியும். நீங்கள் வாங்கிய product-களைத் தேர்வு செய்யுங்கள்.

1. டீலர் கார்டைச் சரிபாருங்கள்: பெயர், **VERIFIED**, முகவரி, போன், email.
2. **Current Progress**-ஐப் பாருங்கள்: உங்கள் **Total Lucky Draw Entries**, grand draw-க்கு இன்னும் எத்தனை தேவை என்பது.
3. **Available Products**-ன் கீழ், நீங்கள் வாங்கிய ஒவ்வொரு product-ஐயும் கண்டுபிடியுங்கள்.
4. எத்தனை வாங்கினீர்கள் என்று அமைக்க, **Qty** பக்கத்தில் உள்ள **+** மற்றும் **−** பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **என் product தெரியவில்லை.** இந்த ஸ்கீமில் டீலர் அதை விற்காமல் இருக்கலாம். டீலரிடம் கேளுங்கள்.
- **இது தவறான டீலர்.** பின்னால் போய், சரியான டீலரின் QR code-ஐ ஸ்கேன் செய்யுங்கள்.
:::

::: section lucky-draw
title: என் லக்கி டிரா
routes: /Luckydraw
for: 
roles: 
designations: 
tour: 1 > [data-help=sch-ld-entry] | 2 > [data-help=sch-ld-prize] | 3 > [data-help=sch-ld-progress] | 4 > [data-help=sch-ld-how] | 5 > [data-help=sch-ld-actions]
minutes: 2
source-hash: f6f7b0a7b08535be
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் வாங்குதல் சரிபார்க்கப்பட்ட பிறகு, **Lucky Draw** பக்கத்தில் **You're in the Lucky Draw!** என்ற செய்தி, நீங்கள் வெல்லக்கூடிய பரிசு, grand draw-க்கு நீங்கள் எவ்வளவு அருகில் இருக்கிறீர்கள் என்பது தெரியும்.

1. புதிய என்ட்ரியைப் பாருங்கள்: **+1 Lucky Draw Entry**, **Ticket Activated**, **Entry ID**, தேதி.
2. **Grand Prize**-ஐ அதன் தோராயமான மதிப்புடனும், **Previous Winners**-ஐயும் பாருங்கள். மேலும் வெற்றியாளர்களைப் பார்க்க **View All** அழுத்துங்கள்.
3. **CURRENT CYCLE PROGRESS**-ன் கீழ், 100-ல் உங்கள் என்ட்ரிகள், **Next Milestone**, **Grand Draw** ஆகியவற்றைப் பாருங்கள்.
4. **How It Works**-ஐப் படியுங்கள்: ஒவ்வொரு வாங்குதலுக்கும் 1 என்ட்ரி, 100 என்ட்ரிகள் ஆனால் grand draw திறக்கும்.
5. மேலும் என்ட்ரிகள் சேர்க்க **Scan Another Product** அழுத்துங்கள், அல்லது பட்டியலைப் பார்க்க **View All Entries** அழுத்துங்கள். போனில் இந்தப் பட்டன்கள் **Scan QR** மற்றும் **View Entries** என்று இருக்கும்.

## பொதுவான பிரச்சனைகள்

- **என் என்ட்ரி எண்ணிக்கை கூடவில்லை.** வாங்குதல் சரிபார்க்கப்பட்ட பிறகுதான் என்ட்ரி சேர்க்கப்படும். பிறகு **View All Entries**-ல் மீண்டும் பாருங்கள்.
- **GOLD MILESTONE என்றால் என்ன?** **Grand Draw**-ஐத் திறக்கும் 100-என்ட்ரி அடையாளம் அது.
:::

::: section my-entries
title: என் எல்லா என்ட்ரிகளையும் பார்ப்பது
routes: /LuckyDrawList
for: 
roles: 
designations: 
minutes: 2
source-hash: a976b5d2def67294
confidence: ok

**இந்தத் திரை எதற்கு**
**Over All Entries** திரையில் இந்தச் சுற்றில் உங்களிடம் உள்ள எல்லா லக்கி டிரா என்ட்ரிகளும், ஒவ்வொன்றுக்கான product-கள் மற்றும் points-உடன் தெரியும்.

1. **TOTAL ENTRIES** கார்டு, 100-ல் உங்களிடம் எத்தனை active என்ட்ரிகள் உள்ளன என்று 25, 50, 75 மற்றும் **GOLD MILESTONE** அடையாளங்களுடன் காட்டும்.
2. **Your Entries List**-ன் கீழ், ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All**, **Active**, **Used** அல்லது **Expired**.
3. ஒவ்வொரு கார்டிலும் என்ட்ரி எண், அதன் நிலை, தேதி, product-கள், **No.of Products**, **Total Products**, **Earned Points** தெரியும்.
4. அந்த என்ட்ரியைப் பார்க்க கார்டில் **View Details** அழுத்துங்கள்.
5. லக்கி டிரா பக்கத்துக்குத் திரும்ப பின் அம்புக்குறியைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **பட்டியல் காலியாக உள்ளது.** இந்த டேபில் உங்களுக்கு என்ட்ரிகள் இல்லை. **All** தேர்வு செய்யுங்கள்.
- **சில என்ட்ரிகளில் Expired என்று காட்டுகிறது.** அவற்றால் இனி வெல்ல முடியாது. என்ட்ரிகள் சேர்க்க, புதிய வாங்குதல்களைத் தொடர்ந்து ஸ்கேன் செய்யுங்கள்.
:::

::: section winner-announcement
title: வெற்றியாளர் செய்தி
routes: /WinnerPopUp
for: 
roles: 
designations: 
minutes: 1
source-hash: a8394f0ccc4ee5a5
confidence: ok

**இந்தத் திரை எதற்கு**
நீங்கள் வென்றால், ஒரு **Congratulations!** செய்தி திறக்கும்: **You are a Lucky Draw Winner**. அதில் உங்கள் பரிசும் ஸ்கீமும் தெரியும்.

1. பரிசின் பெயரையும் ஸ்கீம் பெயரையும் படியுங்கள்.
2. **Winner Details**-ஐத் திறக்க **View Details** அழுத்துங்கள்.
3. லக்கி டிரா பக்கத்துக்குத் திரும்ப செய்தியை மூடுங்கள்.

## பொதுவான பிரச்சனைகள்

- **செய்தியைத் தவறுதலாக மூடிவிட்டேன்.** பக்க மெனுவிலிருந்து **Winner Details**-ஐத் திறங்கள்.
:::

::: section winner-details
title: என் பரிசு விவரங்கள்
routes: /WinnerDetails
for: 
roles: 
designations: 
minutes: 2
source-hash: e8beec48b189b974
confidence: ok

**இந்தத் திரை எதற்கு**
**Winner Details** திரையில் உங்கள் லக்கி டிரா பரிசும், அது உங்களுக்கு எப்படி வந்து சேரும் என்பதும் தெரியும்.

1. மேலே, **You are a Winner** மற்றும் **Draw ID**-ஐப் பாருங்கள்.
2. **Draw Summary**-ல் அந்த டிராவின் **Participants**, **Winners**, **Entries** தெரியும்.
3. **Winner Information**-ல் **Winner Name**, **Type**, **Dealer Code**, **Mobile Number**, **Points Earned** தெரியும்.
4. **Prize Information**-ல் **Prize Name**, **Prize Value**, **Status** (உதாரணமாக **Prize Distributed**), **Distributed Date** தெரியும்.
5. **Scheme Information** மற்றும் **Activity Information**-ல் ஸ்கீம், **Draw Date**, யார் அறிவித்தார், எப்போது என்பது தெரியும்.
6. **Better Luck Next Time!** என்று தெரிந்தால், என்ட்ரிகள் சேர்க்க **Scan QR** அழுத்துங்கள், அல்லது அவற்றைப் பார்க்க **View Entries** அழுத்துங்கள்.
7. உங்கள் பரிசு பற்றி எந்தக் கேள்வி இருந்தாலும், **Need Help?** பெட்டியில் **Contact Support** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **மொபைல் எண் தவறாக உள்ளது.** குழு உங்களைத் தொடர்பு கொள்ள **Contact Support** அழுத்துங்கள்.
- **பரிசு இன்னும் கொடுக்கப்படவில்லை.** **Prize Information**-ன் கீழ் **Status**-ஐப் பாருங்கள், அல்லது **Contact Support** அழுத்துங்கள்.
:::

::: section scheme-winners
title: எல்லா ஸ்கீம் வெற்றியாளர்களையும் பார்ப்பது
routes: /CelebrationWinners
for: 
roles: 
designations: 
minutes: 1
source-hash: 633e72a075f230fb
confidence: ok

**இந்தத் திரை எதற்கு**
**Scheme Winners** திரையில் முந்தைய அறிவிப்புகளின் லக்கி டிரா வெற்றியாளர்கள் தெரியும்.

1. **Celebrating Our Winners!** பேனரில் **Total Winners Announced** தெரியும்.
2. ஸ்கீம் (**All Schemes**), ஆண்டு, வகை (**All Types**), இடம் (**All Locations**) ஆகியவற்றைத் தேர்வு செய்யப் பட்டியல்களைப் பயன்படுத்துங்கள்.
3. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All Winners**, **Dealer Winners** அல்லது **Farmer Winners**.
4. ஒவ்வொரு கார்டிலும் வெற்றியாளர், **Dealer** அல்லது **Farmer**, ஸ்கீம், இடம், **Prize**, **Draw Date** தெரியும்.
5. ஒரு வெற்றியாளர் பற்றி மேலும் பார்க்க **View Details** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **ஒரு வெற்றியாளர் தெரியவில்லை.** **All Schemes**, **All Types**, **All Locations** தேர்வு செய்து, ஆண்டைச் சரிபாருங்கள்.
:::
