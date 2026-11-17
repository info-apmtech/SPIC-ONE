---
module: dealer-review
title: டீலர் பரிசீலனை மற்றும் ஒப்புதல்
icon: bi-person-check
order: 102
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
source-hash: c1f3fc37e023de12
confidence: ok

MO ஒரு டீலர் விண்ணப்பத்தைச் சமர்ப்பித்த பிறகு, முதலில் RM, பிறகு SM, பிறகு AVP அதைச் சரிபார்ப்பார்கள். ஒவ்வொருவரும் ஒப்புதல் தந்து ஒரு கிரெடிட் லிமிட்டைப் பரிந்துரைக்கலாம், அல்லது திருத்தத்துக்காகத் திருப்பி அனுப்பலாம். சப் டீலர்கள் மற்றும் பணியாளர்களுக்கான ஒப்புதல் திரைகளும், சில டீலர் ரிப்போர்ட்டுகளும் இந்த மாட்யூலில் உள்ளன.

- check | icon: bi-search | color: #6d28d9
  title: மூன்று நிலைகளில் சரிபார்ப்பு
  text: RM, பிறகு SM, பிறகு AVP விண்ணப்பத்தைத் திறந்து ஒவ்வொரு பகுதியையும் சரிபார்ப்பார்கள்.
- limit | icon: bi-cash-coin | color: #d97706
  title: கிரெடிட் லிமிட் முடிவு
  text: RM மற்றும் SM, SPIC மற்றும் GFL-க்கான கிரெடிட் லிமிட்டைப் பரிந்துரைப்பார்கள். AVP இறுதி லிமிட்டைத் தருவார்.
- back | icon: bi-arrow-counterclockwise | color: #dc2626
  title: திருத்தத்துக்காகத் திருப்பி அனுப்புதல்
  text: எந்த நிலையிலும், காரணத்தையும் திருத்த வேண்டிய பகுதிகளையும் சொல்லி, விண்ணப்பத்தை ஒரு படி பின்னால் திருப்பி அனுப்பலாம்.
:::

::: section flow
title: ஒரு விண்ணப்பம் எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 1f194f956d1d33e7
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- mo | type: lane | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  sub: விண்ணப்பத்தை நிரப்புவார்
- rm | type: lane | color: #0891b2 | roles: RM, RMD
  title: RM
  sub: மண்டல மேலாளர் (Regional Manager)
- sm | type: lane | color: #c026d3 | roles: SMD, SMM
  title: SM
  sub: மாநில மார்க்கெட்டிங் மேலாளர் (State Marketing Manager)
- avp | type: lane | color: #7c3aed | roles: AVP, Director, Admin, CorporateAdmin, SuperAdmin
  title: AVP
  sub: இறுதி ஒப்புதல்
- office | type: lane | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: புதிய டீலர்களுக்கு மட்டும்
- submit | lane: mo | col: 1 | icon: bi-send | color: #64748b | next: rm-check
  title: விண்ணப்பத்தைச் சமர்ப்பித்தல்
  status: Submitted
  text: MO டீலர் விண்ணப்பத்தின் எல்லாப் பகுதிகளையும் நிரப்பிச் சமர்ப்பிப்பார். முழுமையான விண்ணப்பங்கள் மட்டுமே பரிசீலனைப் பட்டியலுக்கு வரும்.
- rm-check | lane: rm | col: 2 | icon: bi-person-check | color: #0891b2 | next: sm-check | return: submit | screen: review-approve
  title: RM சரிபார்த்தல்
  status: In RM
  text: RM விண்ணப்பத்தைச் சரிபார்த்து, RM கிரெடிட் லிமிட்டை உள்ளிட்டு **Proceed to SM** அழுத்துவார், அல்லது MO-க்குத் திருப்பி அனுப்புவார்.
- sent-back | lane: mo | col: 3 | icon: bi-arrow-counterclockwise | color: #dc2626 | kind: exception | from: rm-check | to: submit | screen: send-back
  title: திருத்தி மீண்டும் சமர்ப்பித்தல்
  status: Sent Back
  text: பரிசீலிப்பவர் ஒருவர் காரணத்தையும் திருத்த வேண்டிய பகுதிகளையும் சொல்லி விண்ணப்பத்தைத் திருப்பி அனுப்பியுள்ளார். முந்தைய நிலையில் உள்ளவர் அவற்றைத் திருத்தி மீண்டும் அனுப்புவார்.
- sm-check | lane: sm | col: 3 | icon: bi-clipboard-check | color: #c026d3 | next: avp-check | return: rm-check | screen: review-approve
  title: SM சரிபார்த்தல்
  status: In SMM
  text: SM, RM கிரெடிட் லிமிட்டைப் பார்த்து, SM கிரெடிட் லிமிட்டை உள்ளிட்டு **Proceed to AVP** அழுத்துவார், அல்லது RM-க்குத் திருப்பி அனுப்புவார்.
- avp-check | lane: avp | col: 4 | icon: bi-patch-check | color: #7c3aed | next: office-ids | return: sm-check | screen: review-approve
  title: AVP ஒப்புதல் தருதல்
  status: In AVP
  text: AVP முந்தைய இரண்டு லிமிட்டுகளையும் பார்த்து, இறுதி கிரெடிட் லிமிட்டை உள்ளிட்டு **Approve** அழுத்துவார். டீலர் ஆக்டிவ் ஆகிவிடுவார்.
- office-ids | lane: office | col: 5 | icon: bi-upc-scan | color: #16a34a | screen: avp-office-ids
  title: Dealer ID-களை உள்ளிடுதல்
  status: Verified
  text: புதிய டீலர் என்றால், AVP Office, SPIC மற்றும் GFL Dealer ID-களை உள்ளிடும். ஏற்கனவே உள்ள டீலர் என்றால், AVP ஒப்புதலுடன் முடிந்துவிடும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 3678b5e01ec8dd65
confidence: ok

- submitted | color: #64748b
  label: Submitted
  text: MO விண்ணப்பத்தைச் சமர்ப்பித்துவிட்டார்.
- in-rm | color: #0891b2
  label: In RM
  text: மண்டல மேலாளருக்காக (RM) காத்திருக்கிறது.
- in-smm | color: #c026d3
  label: In SMM
  text: மாநில மார்க்கெட்டிங் மேலாளருக்காக (SMM) காத்திருக்கிறது.
- in-avp | color: #7c3aed
  label: In AVP
  text: AVP-யின் இறுதி ஒப்புதலுக்காகக் காத்திருக்கிறது.
- pending | color: #d97706
  label: Pending
  text: பரிசீலனைப் பட்டியலில்: இன்னும் RM, SMM, AVP அல்லது AVP Office-இடம் உள்ளது. இப்போது யாரிடம் உள்ளது என்று **Current Stage** நெடுவரிசை காட்டும்.
- verified | color: #16a34a
  label: Verified
  text: பரிசீலனைப் பட்டியலில்: முழுமையாக ஒப்புதல் கிடைத்துவிட்டது.
- approved | color: #16a34a
  label: Approved
  text: விண்ணப்பத்தில்: AVP ஒப்புதல் தந்துவிட்டார்.
- sent-back | color: #dc2626
  label: Sent Back
  text: பரிசீலனைப் பட்டியலில்: பரிசீலிப்பவர் ஒருவர் திருத்தத்துக்காகத் திருப்பி அனுப்பியுள்ளார்.
- rejected | color: #dc2626
  label: Rejected
  text: விண்ணப்பத்தில்: பரிசீலிப்பவர் ஒருவர் திருப்பி அனுப்பியுள்ளார். இது மூடப்படவில்லை; திருத்திய பிறகு மீண்டும் வரும்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 06465e79ce0431ec
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- mo | color: #64748b | roles: MO, MDO, JMDO
  title: MO
  - டீலர் விண்ணப்பத்தைச் சமர்ப்பிப்பது
  - திருப்பி அனுப்பப்பட்டால் அதைத் திருத்துவது
- rm | color: #0891b2 | roles: RM, RMD
  title: RM
  - உங்கள் மண்டலத்தின் விண்ணப்பங்களைச் சரிபார்ப்பது
  - RM கிரெடிட் லிமிட்டைப் பரிந்துரைப்பது
  - MO-க்குத் திருப்பி அனுப்புவது
- sm | color: #c026d3 | roles: SMD, SMM
  title: SM
  - உங்கள் மாநிலத்தின் விண்ணப்பங்களைச் சரிபார்ப்பது
  - SM கிரெடிட் லிமிட்டைப் பரிந்துரைப்பது
  - RM-க்குத் திருப்பி அனுப்புவது
  - சப் டீலர்கள் மற்றும் பணியாளர்களுக்கு முதல் ஒப்புதல் தருவது
- avp | color: #7c3aed | roles: AVP, Director
  title: AVP
  - இறுதி ஒப்புதலும் கிரெடிட் லிமிட்டும் தருவது
  - SM-க்குத் திருப்பி அனுப்புவது
  - சப் டீலர்கள் மற்றும் பணியாளர்களுக்கு இறுதி ஒப்புதல் தருவது
- admin | color: #374151 | roles: Admin, CorporateAdmin, SuperAdmin
  title: Admin
  - AVP போலவே இறுதி ஒப்புதல் தரலாம்
  - Credit Limit Sales எண்ணிக்கைகளைப் பராமரிப்பது
  - மாநில வாரியான டீலர் சுருக்கத்தைப் பார்ப்பது
- office | color: #0d9488 | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - புதிய டீலர்களுக்கு SPIC மற்றும் GFL Dealer ID-களை உள்ளிடுவது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 724bb434f0c07f99
confidence: ok

- no-button | popular: true
  q: ஒரு விண்ணப்பத்தைத் திறந்தேன், ஆனால் Proceed அல்லது Send Back பட்டன் இல்லை
  a: விண்ணப்பம் இப்போது உங்கள் நிலையில் இல்லை. அது உங்களுக்காகக் காத்திருக்கும்போது மட்டுமே பட்டன்கள் தெரியும். மேலே உள்ள **Status** கார்டையும் முன்னேற்றப் பட்டையையும் பாருங்கள்.
- no-limit-boxes | popular: true
  q: Confirm Proceed சாளரத்தில் கிரெடிட் லிமிட் பெட்டிகள் ஏன் இல்லை?
  a: புதிய டீலரின் முதல் ஒப்புதலுக்கும், ஆக்டிவ் இல்லாத டீலருக்கும், கிரெடிட் லிமிட் இல்லாமல் ஒப்புதலுடன் (consent) விண்ணப்பித்த டீலருக்கும் (அப்போது லிமிட் 0) இவை தெரியாது. டீலர் SPIC-உடன் இருந்தால் மட்டுமே SPIC பெட்டிகளும், GFL-உடன் இருந்தால் மட்டுமே GFL பெட்டிகளும் தெரியும்.
- missing
  q: ஒரு விண்ணப்பம் என் பரிசீலனைப் பட்டியலில் இல்லை
  a: உங்கள் பகுதியின் முழுமையான விண்ணப்பங்கள் மட்டுமே பட்டியலில் தெரியும். விண்ணப்பத்தின் சில பகுதிகள் இன்னும் காலியாக இருந்தால், அது வராது. எல்லாப் பகுதிகளையும் முடிக்குமாறு MO-விடம் சொல்லுங்கள்.
- remarks
  q: "Remarks are required for New Dealer approval." என்று வருகிறது
  a: புதிய டீலர் என்றால், ஒவ்வொரு நிலையிலும் குறிப்பு (remarks) எழுத வேண்டும். **Remarks** பெட்டியில் டைப் செய்து, மீண்டும் **Confirm & Proceed** அழுத்துங்கள்.
- after-send-back
  q: நான் திருப்பி அனுப்பினால் விண்ணப்பம் எங்கே போகும்?
  a: ஒரு நிலை பின்னால் போகும். RM, MO-க்கு அனுப்புவார்; SM, RM-க்கு; AVP, SM-க்கு. அவர்கள் திருத்திய பிறகு அது மீண்டும் உங்களிடம் வரும்.
:::

::: section review-list
title: பரிசீலனைக்குக் காத்திருக்கும் விண்ணப்பங்களைப் பார்ப்பது
routes: /DealerReviewList
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-list-cards] | 2 > [data-help=dr-list-search] | 3 > [data-help=dr-list-status] | 4 > [data-help=dr-list-table] | 5 > [data-help=dr-list-pages]
minutes: 2
source-hash: d6627d7c7d23093d
confidence: ok

**இந்தத் திரை எதற்கு**
**Dealer Applications Review** திரையில் உங்கள் பகுதியிலிருந்து சமர்ப்பிக்கப்பட்ட டீலர் விண்ணப்பங்கள் தெரியும். உங்களுக்காகக் காத்திருப்பவை மேலே இருக்கும்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Applications**, **Pending Review**, **Verified** மற்றும் **Sent Back**.
2. ஒரு விண்ணப்பத்தை வேகமாகக் கண்டுபிடிக்க, **Search applications...**-ல் விண்ணப்ப ID அல்லது டீலர் பெயரை டைப் செய்யுங்கள்.
3. அவற்றை மட்டும் பார்க்க, நிலைப் பெட்டியில் **Pending**, **Verified** அல்லது **Sent Back** தேர்வு செய்யுங்கள். **All Status** எல்லாவற்றையும் காட்டும்.
4. அட்டவணையில், இப்போது விண்ணப்பம் யாரிடம் உள்ளது என்று **Current Stage** காட்டும், ஒப்புதல் தரப்பட்ட லிமிட்டை **Credit Limit** காட்டும். திறக்க, **Action**-ல் உள்ள கண் ஐகானைத் தட்டுங்கள்.
5. கீழே, எத்தனை வரிசைகள் தெரிய வேண்டும் என்று **Items Per Page**-ல் தேர்வு செய்யுங்கள், பக்கங்களுக்கு இடையே நகர அம்புக்குறிகளைப் பயன்படுத்துங்கள்.

> Tip: RM தன் மண்டலத்தைப் பார்ப்பார், SM மாநிலத்தைப் பார்ப்பார், AVP எல்லா விண்ணப்பங்களையும் பார்ப்பார்.

## பொதுவான பிரச்சனைகள்

- **"No applications found."** (விண்ணப்பங்கள் எதுவும் இல்லை) **All Status** தேர்வு செய்து, தேடல் பெட்டியைக் காலி செய்யுங்கள்.
- **ஒரு விண்ணப்பம் பட்டியலில் இல்லை.** முழுமையான விண்ணப்பங்கள் மட்டுமே பட்டியலில் வரும். எல்லாப் பகுதிகளையும் நிரப்புமாறு MO-விடம் சொல்லுங்கள்.
:::

::: section review-approve
title: விண்ணப்பத்தைச் சரிபார்த்து ஒப்புதல் தருவது
routes: /SavedDealerReview
for: 
roles: 
designations: 
tour: 1 > [data-help=dr-rev-cards] | 2 > [data-help=dr-rev-progress] | 3 > [data-help=dr-rev-timeline] | 4 > [data-help=dr-rev-details] | 5 > [data-help=dr-rev-sendback] | 6 > [data-help=dr-rev-approve]
minutes: 4
source-hash: 337aec253f3ff75e
confidence: ok

**இந்தத் திரை எதற்கு**
**Review - Dealer Application** திரையில் ஒரு விண்ணப்பம் முழுவதும் தெரியும். அது உங்களுக்காகக் காத்திருக்கும்போது, இங்கே ஒப்புதல் தந்து உங்கள் கிரெடிட் லிமிட்டைத் தரலாம், அல்லது திருப்பி அனுப்பலாம்.

1. மேலே உள்ள கார்டுகளைச் சரிபாருங்கள்: **Dealer Name**, **Dealer Code**, **Submitted By** மற்றும் **Status**.
2. முன்னேற்றப் பட்டை ஒவ்வொரு நிலையையும் காட்டும்: **MO**, **RM**, **SM** மற்றும் **AVP**, கூடவே **Under Review**, **Pending**, **Approved** அல்லது **Rejected**.
3. யார் செயல்பட்டார், எப்போது, அவர்களின் குறிப்பு, அவர்கள் தந்த கிரெடிட் லிமிட் ஆகியவற்றை **Timeline** மற்றும் **Action History** காட்டும்.
4. ஒவ்வொரு பகுதியையும் தட்டித் திறந்து சரிபாருங்கள்: **Company Details**, **Experience In Fertilizer Trade**, **Annual Sales Data**, **Proprietor Details**, **Investment**, **Credit Limit Proposal**, **Enclosures** மற்றும் மற்றவை.
5. ஏதாவது தவறாக இருந்தால், **Send Back** அழுத்துங்கள் ("விண்ணப்பத்தைத் திருப்பி அனுப்புவது" பகுதியைப் பாருங்கள்).
6. எல்லாம் சரியாக இருந்தால், **Proceed to SM** (RM), **Proceed to AVP** (SM) அல்லது **Approve** (AVP) அழுத்துங்கள். **Confirm Proceed** சாளரத்தில் உங்கள் கிரெடிட் லிமிட்டையும் **Remarks**-ஐயும் உள்ளிட்டு, **Confirm & Proceed** அழுத்துங்கள்.

> Careful: நீங்கள் அனுப்பிய பிறகு, விண்ணப்பம் அடுத்த நிலைக்குப் போய்விடும். அதன் பிறகு உங்களால் மாற்ற முடியாது.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **RM Credit Limit (SPIC)**, **RM Credit Limit (GFL)** | RM பரிந்துரைக்கும் லிமிட். RM-க்குத் தெரியும். |
| **SM Credit Limit (SPIC)**, **SM Credit Limit (GFL)** | SM பரிந்துரைக்கும் லிமிட். RM லிமிட்டுகள் மேலே **(Approved)** என்று தெரியும். |
| **AVP Credit Limit (SPIC)**, **AVP Credit Limit (GFL)** | இறுதி லிமிட். RM மற்றும் SM லிமிட்டுகள் மேலே தெரியும். |
| **Remarks** | உங்கள் குறிப்புகள். புதிய டீலருக்குக் கட்டாயம், மற்றவற்றுக்கு விருப்பம். |

## பொதுவான பிரச்சனைகள்

- **Proceed அல்லது Send Back பட்டன் இல்லை.** விண்ணப்பம் இப்போது உங்களுக்காகக் காத்திருக்கவில்லை. நீங்கள் படிக்க மட்டுமே முடியும்.
- **பச்சை நிற "Credit Limit Consent" பெட்டி வருகிறது.** டீலர் கிரெடிட் லிமிட் விவரங்கள் இல்லாமல் விண்ணப்பித்துள்ளார். அதனால் கிரெடிட் லிமிட் 0.
- **திரை Dashboard-க்குப் போய்விடுகிறது.** இது சாதாரணம்தான். நீங்கள் ஒப்புதல் தந்த பிறகு, ஆப் வெற்றிச் செய்தியைக் காட்டி Dashboard-ஐத் திறக்கும்.
:::

::: section send-back
title: விண்ணப்பத்தைத் திருப்பி அனுப்புவது
routes: /SavedDealerReview
for: 
roles: RM, RMD, SMD, SMM, AVP, Director, Admin, CorporateAdmin, SuperAdmin
designations: 
minutes: 2
source-hash: b3b23aec15238c4a
confidence: ok

**இந்தத் திரை எதற்கு**
விண்ணப்பத்தில் தவறுகள் இருந்தால், காரணத்துடன் ஒரு நிலை பின்னால் திருப்பி அனுப்புங்கள். RM, **Marketing Officer (MO)**-க்கு அனுப்புவார்; SM, **Regional Manager (RM)**-க்கு; AVP, **State Manager (SM)**-க்கு.

1. விண்ணப்பத்தைத் திறந்து கீழே உள்ள **Send Back** அழுத்துங்கள்.
2. **Send Back Application** சாளரத்தில், **Application Summary** மற்றும் **Send Back To**-ஐச் சரிபாருங்கள்.
3. **Reason for Sending Back** எழுதுங்கள். இது கட்டாயம்.
4. **Sections Requiring Correction**-ன் கீழ், திருத்த வேண்டிய பகுதிகளை டிக் செய்யுங்கள், உதாரணமாக **Tax & Compliance**, **Investment** அல்லது **Enclosures**. சில ஏற்கனவே டிக் செய்யப்பட்டிருக்கும்; சரியாக உள்ளவற்றின் டிக்கை எடுத்துவிடுங்கள்.
5. **Priority Level**-ஐத் தேர்வு செய்யுங்கள்: **High**, **Medium** அல்லது **Low**.
6. **Send Back to MO**, **Send Back to RM** அல்லது **Send Back to SM** அழுத்துங்கள் (பட்டன் உங்கள் நிலையைக் காட்டும்), அல்லது நிறுத்த **Cancel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Reason is required."** (காரணம் கட்டாயம்) முதலில் காரணத்தை எழுதுங்கள்.
- **"Please select at least one correction section."** (குறைந்தது ஒரு திருத்தப் பகுதியைத் தேர்வு செய்யுங்கள்) குறைந்தது ஒரு பகுதியை டிக் செய்யுங்கள்.
- **"Unable to send back dealer application."** (டீலர் விண்ணப்பத்தைத் திருப்பி அனுப்ப முடியவில்லை) உங்கள் இன்டர்நெட்டைச் சரிபார்த்து மீண்டும் முயற்சி செய்யுங்கள்.
:::

::: section avp-office-ids
title: புதிய டீலருக்கு Dealer ID-களை உள்ளிடுவது
routes: /SavedDealerReview
for: 
roles: CommonRole
designations: AVP Office
minutes: 2
source-hash: e77b528ade25e538
confidence: ok

**இந்தத் திரை எதற்கு**
AVP ஒரு புதிய டீலருக்கு ஒப்புதல் தந்த பிறகு, AVP Office நிறுவனத்தின் Dealer ID-களை உள்ளிடும். இதனுடன் விண்ணப்பம் முடிவடையும்.

1. பரிசீலனைப் பட்டியலிலிருந்து விண்ணப்பத்தைத் திறங்கள். அதன் **Current Stage**-ல் **AVP Office** என்று இருக்கும்.
2. **AVP Office - Dealer ID Submission** பெட்டி வரை கீழே ஸ்க்ரோல் செய்யுங்கள்.
3. **SPIC Dealer ID** மற்றும்/அல்லது **GFL Dealer ID**-ஐ உள்ளிடுங்கள். டீலர் விண்ணப்பித்த நிறுவனங்கள் மட்டுமே தெரியும்.
4. தேவைப்பட்டால் **Remarks (optional)** சேருங்கள்.
5. **Submit Dealer IDs** அழுத்துங்கள். ஆப் வெற்றிச் செய்தியைக் காட்டி Dashboard-ஐத் திறக்கும்.

## பொதுவான பிரச்சனைகள்

- **"SPIC Dealer ID is required for this application."** (இந்த விண்ணப்பத்துக்கு SPIC Dealer ID கட்டாயம்) டீலர் SPIC-உடன் இருக்கிறார். SPIC Dealer ID-ஐ உள்ளிடுங்கள்.
- **"GFL Dealer ID is required for this application."** (இந்த விண்ணப்பத்துக்கு GFL Dealer ID கட்டாயம்) டீலர் GFL-உடன் இருக்கிறார். GFL Dealer ID-ஐ உள்ளிடுங்கள்.
- **எனக்கு அந்தப் பெட்டி தெரியவில்லை.** AVP இன்னும் ஒப்புதல் தரவில்லை, அல்லது ID-கள் ஏற்கனவே உள்ளிடப்பட்டுவிட்டன.
:::

::: section view-application
title: விண்ணப்பத்தைப் பார்ப்பது (படிக்க மட்டும்)
routes: /ReviewDealer
for: 
roles: 
designations: 
minutes: 1
source-hash: ee8404c8e5c3aad3
confidence: ok

**இந்தத் திரை எதற்கு**
இந்தப் பழைய **Review - Dealer Application** திரையில் ஒரு விண்ணப்பம் தெரியும், ஆனால் ஒப்புதல் பட்டன்கள் எதுவும் இருக்காது.

1. மேலே உள்ள கார்டுகளைச் சரிபாருங்கள்: **Dealer Name**, **Dealer ID**, **Total Experience**, **Total Sales**, **Investment**, **Credit Limit** மற்றும் **Status**.
2. **Company Details** அல்லது **Investment** போன்ற ஒவ்வொரு பகுதியையும் தட்டித் திறங்கள்.
3. **Enclosures**-ன் கீழ், ஒரு ஆவணத்தின் பெயரைத் தட்டி அதைத் திறங்கள்.

> Tip: ஒப்புதல் தர அல்லது திருப்பி அனுப்ப, அதற்குப் பதிலாக **Dealer Applications Review** பட்டியலிலிருந்து விண்ணப்பத்தைத் திறங்கள்.

## பொதுவான பிரச்சனைகள்

- **திரை காலியாக உள்ளது.** ஒரு விண்ணப்பத்தைத் தேர்வு செய்தால் மட்டுமே இது திறக்கும். பரிசீலனைப் பட்டியலுக்குப் போய் கண் ஐகானைப் பயன்படுத்துங்கள்.
:::

::: section sm-approvals
title: சப் டீலர்கள் மற்றும் பணியாளர்களுக்கு ஒப்புதல் தருவது (SM)
routes: /SMMApprovals
for: 
roles: SMD, SMM
designations: 
tour: 1 > [data-help=dr-sm-tabs] | 2 > [data-help=dr-sm-cards] | 3 > [data-help=dr-sm-table] | 4 > [data-help=dr-sm-table]
minutes: 2
source-hash: e13ee4a27ffd7b42
confidence: ok

**இந்தத் திரை எதற்கு**
**SM Approval** திரையில், உங்கள் மாநிலத்தின் டீலர்களுக்காகச் சேர்க்கப்பட்ட சப் டீலர்களும் பணியாளர்களும் தெரிவார்கள். முதலில் SM சரிபார்ப்பார்; பிறகு அவை AVP-க்குப் போகும்.

1. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All Pending**, **Sub Dealers** அல்லது **Employees**.
2. கார்டுகள் **Total Pending**, **Sub Dealers** மற்றும் **Employees**-ஐக் காட்டும். ஒரு வகையை மட்டும் பார்க்க **Filter** பயன்படுத்துங்கள்.
3. **Pending SM Approvals**-ல், **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** மற்றும் **Nominee**-ஐச் சரிபாருங்கள்.
4. **Approve** அல்லது **Reject** அழுத்தி, **Remarks** எழுதி, பிறகு **Confirm Approve** அல்லது **Confirm Reject** அழுத்துங்கள்.

> Tip: ஒப்புதலுக்குக் குறிப்பு கட்டாயமில்லை, ஆனால் நிராகரிக்கும்போது காரணத்தை எழுதுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No Pending Approvals".** (நிலுவையில் ஒப்புதல்கள் இல்லை) இப்போது எதுவும் உங்களுக்காகக் காத்திருக்கவில்லை.
- **"Show more" என்று தெரிகிறது.** முதலில் 50 வரிசைகள் மட்டுமே தெரியும். மீதியைப் பார்க்க **Show more** அழுத்துங்கள்.
:::

::: section avp-approvals
title: சப் டீலர்கள் மற்றும் பணியாளர்களுக்கு இறுதி ஒப்புதல் (AVP)
routes: /AVPApprovals
for: 
roles: AVP
designations: 
minutes: 2
source-hash: 61b6fcc972ffa53e
confidence: ok

**இந்தத் திரை எதற்கு**
**AVP Approval** திரையில், SM ஏற்கனவே ஒப்புதல் தந்த சப் டீலர்களும் பணியாளர்களும் தெரிவார்கள். AVP இறுதி ஒப்புதல் தருவார்.

1. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All Pending**, **Sub Dealers** அல்லது **Employees**.
2. **Total Pending**, **Sub Dealers** மற்றும் **Employees** கார்டுகளைச் சரிபாருங்கள். ஒரு வகையை மட்டும் பார்க்க **Filter** பயன்படுத்துங்கள்.
3. **Pending AVP Approvals**-ல், **Dealer Code**, **Item Code**, **Item Name**, **Beneficiary** மற்றும் **Nominee**-ஐச் சரிபாருங்கள்.
4. **Approve** அல்லது **Reject** அழுத்துங்கள். சாளரத்தில் **SM Status: Approved** என்று தெரியும்.
5. **Remarks** எழுதி, **Confirm Approve** அல்லது **Confirm Reject** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No Pending AVP Approvals".** (நிலுவையில் AVP ஒப்புதல்கள் இல்லை) SM ஒப்புதல் தந்த எந்தப் பதிவும் உங்களுக்காகக் காத்திருக்கவில்லை.
- **"Action failed".** (செயல் தோல்வியடைந்தது) மீண்டும் முயற்சி செய்யுங்கள். தொடர்ந்து தோல்வியடைந்தால், உங்கள் admin-இடம் சொல்லுங்கள்.
:::

::: section credit-limit-sales
title: கிரெடிட் லிமிட்டுக்கான டீலர் விற்பனையைப் பராமரிப்பது
routes: /CreditLimitSales
for: 
roles: 
designations: 
minutes: 3
source-hash: 580721465ba2267a
confidence: ok

**இந்தத் திரை எதற்கு**
**Credit Limit Sales** திரையில் ஒவ்வொரு டீலரின் கடந்த கால விற்பனை, ஆண்டு மற்றும் product வாரியாகச் சேமிக்கப்படும். பரிசீலிப்பவர்கள் இந்த எண்ணிக்கைகளை விண்ணப்பத்தின் **Credit Limit Proposal**-ல் பார்ப்பார்கள்.

1. **Add New Credit Limit Sales**-ன் கீழ், **State** மற்றும் **Financial Year**-ஐத் தேர்வு செய்யுங்கள்.
2. **Dealer Code** அல்லது **Dealer Name** மூலம் டீலரைத் தேர்வு செய்யுங்கள். இரண்டு பெட்டிகளும் ஒரே டீலரைத்தான் தேர்வு செய்யும்.
3. **Product**, **Product Group** மற்றும் **Category**-ஐத் தேர்வு செய்யுங்கள்.
4. **Quantity** மற்றும் **Gross Amount**-ஐ உள்ளிட்டு, **Save** அழுத்துங்கள். **Cancel** படிவத்தைக் காலி செய்யும்.
5. ஒரே நேரத்தில் பல வரிசைகளைச் சேர்க்க, Excel வடிவத்தைப் பெற **Sample Format** அழுத்தி, அதை நிரப்பி, **Bulk Upload** அழுத்தி, **Financial Year**-ஐயும் கோப்பையும் தேர்வு செய்யுங்கள்.
6. **Existing Credit Limit Sales**-ல், code அல்லது பெயர் மூலம் தேடுங்கள். பென்சில் ஒரு வரிசையைத் திருத்தும், குப்பைத்தொட்டி அதை நீக்கும்.

> Careful: ஒரு வரிசையை நீக்கினால் அதைத் திரும்பப் பெற முடியாது.

## பொதுவான பிரச்சனைகள்

- **"All required fields must be filled."** (கட்டாயமான எல்லாப் புலங்களையும் நிரப்ப வேண்டும்) State, Financial Year, டீலர், Product, Product Group மற்றும் Category-ஐத் தேர்வு செய்யுங்கள்.
- **"Please select a financial year before uploading."** (அப்லோட் செய்யும் முன் நிதியாண்டைத் தேர்வு செய்யுங்கள்) முதலில் அப்லோட் சாளரத்தில் ஆண்டைத் தேர்வு செய்யுங்கள்.
- **Save, Bulk Upload, பென்சில் அல்லது குப்பைத்தொட்டி இல்லை.** உங்கள் பதவிக்கு (designation) சேர்க்க, மாற்ற அல்லது நீக்க அனுமதி இல்லை. உங்கள் admin-இடம் கேளுங்கள்.
:::

::: section state-summary
title: மாநில வாரியான டீலர் சுருக்கம்
routes: /DealerStateSummary
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP
designations: 
minutes: 1
source-hash: d5685e7a98ccb663
confidence: ok

**இந்தத் திரை எதற்கு**
**Total Dealers — State Wise** திரை ஒவ்வொரு மாநிலத்திலும் உள்ள டீலர்களை எண்ணி, SPIC மற்றும் GFL-க்கு அவர்களின் விண்ணப்பங்கள் எங்கே உள்ளன என்று காட்டும்.

1. **TOTAL DEALERS** மற்றும் **COMPLETED** கார்டுகளைச் சரிபாருங்கள்.
2. **Dealer Summary**-ல், ஒவ்வொரு வரிசையும் ஒரு மாநிலம். நெடுவரிசைகள் **As Per SAP Data**, **Saved Dealers**, **MO**, **RM**, **SMM**, **AVP** மற்றும் **Completed**-ஐக் காட்டும், ஒவ்வொன்றும் **SPIC** மற்றும் **GFL**-க்கு.
3. ஒரு நெடுவரிசைத் தலைப்பைத் தட்டினால் அதன்படி வரிசைப்படுத்தும். மீண்டும் தட்டினால் தலைகீழாக மாறும்.
4. கீழே உள்ள **Total** வரிசை எல்லா மாநிலங்களையும் கூட்டிக் காட்டும்.
5. புதிய எண்ணிக்கைகளைப் பெற **Refresh** அழுத்துங்கள், அல்லது **Back to Dashboard** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Access restricted".** (அனுமதி இல்லை) இந்த ரிப்போர்ட் Admin, Director மற்றும் AVP பயனர்களுக்கு மட்டுமே.
- **ஒரு மாநிலம் "Unknown" என்று காட்டுகிறது.** மாநிலப் பட்டியல் லோட் ஆகவில்லை. **Refresh** அழுத்துங்கள்.
:::

::: section dealer-directory
title: டீலர் டைரக்டரி
routes: /DealerDirectory
for: 
roles: 
designations: 
minutes: 1
source-hash: db991abf0063ad82
confidence: ok

**இந்தத் திரை எதற்கு**
**Dealer Directory** திரை, வரப்போகும் டீலர் பட்டியலின் ஒரு முன்னோட்டம் மட்டுமே. இதில் உள்ள எண்ணிக்கைகளும் வரிசைகளும் உதாரணங்கள் மட்டுமே, இதன் பட்டன்களும் ஃபில்டர்களும் இன்னும் வேலை செய்யாது.

1. அமைப்பைப் பாருங்கள்: **Total Dealers**, **Active Dealers**, **Regions Covered** மற்றும் **Recently Added** கார்டுகள்.
2. **Filters** பெட்டியில் பின்னர் **State**, **Region**, **Head Quarters** மற்றும் **Status**-ஐத் தேர்வு செய்யலாம்.
3. **Dealer List** அட்டவணையில் பின்னர் **Dealer Code**, **Firm Name**, **State**, **Head Quarters**, **Mobile No.**, **Email** மற்றும் **Status** தெரியும்.

> Tip: உண்மையான விண்ணப்பத்தைக் கண்டுபிடிக்க, **Dealer Applications Review** பட்டியலைப் பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Excel, PDF அல்லது Print அழுத்தினால் எதுவும் நடக்கவில்லை.** இந்தப் பட்டன்கள் இன்னும் வேலை செய்யவில்லை.
:::

::: section dealer-mapping
title: விற்பனை தணிக்கைத் திட்டத்தில் டீலர்களை இணைப்பது
routes: /DealerMapping
for: 
roles: 
designations: 
minutes: 3
source-hash: 42aed46703ffd3d8
confidence: ok

**இந்தத் திரை எதற்கு**
**Dealer Mapping** திரை, விற்பனை தணிக்கைத் திட்டத்தின் (sales audit program) 3-வது படியான **Dealers** ஆகும். திட்டத்தில் உள்ள டீலர்களின் பட்டியலை அப்லோட் செய்து, அவர்களின் SAP விற்பனையைத் திட்டத்துடன் ஒப்பிடுவீர்கள்.

1. **Sales Plan**-லிருந்து **Save & Continue** அல்லது **Continue to Dealer Mapping** மூலம் இதைத் திறங்கள்.
2. **Upload Dealer List**-ன் கீழ், Excel கோப்பைப் போடுங்கள் அல்லது **Upload File** அழுத்துங்கள். கோப்பில் **Dealer Code** நெடுவரிசை இருக்க வேண்டும்.
3. செய்தியைப் படியுங்கள். சில வரிசைகள் விடுபட்டிருந்தால், **Row No.**, **Dealer Code** மற்றும் **Reason**-ஐப் பார்க்க **View Errors** அழுத்துங்கள்.
4. **Dealer List**-ல், code அல்லது பெயர் மூலம் தேடுங்கள். குப்பைத்தொட்டி ஒரு டீலரைத் திட்டத்திலிருந்து நீக்கும்.
5. **Dealer Sales Achievement - SAP Invoice Matches**-ல், திட்டமிட்ட விற்பனையையும் உண்மையான விற்பனையையும் சரிபாருங்கள். அவற்றை மீண்டும் லோட் செய்ய **Refresh Matches** அழுத்துங்கள்.
6. **Plan Summary & Comparison**-ஐச் சரிபாருங்கள்: **Required Turnover**, **Achievement Amount**, **Difference** மற்றும் **Progress %**.
7. **Continue to Plan Review** அழுத்துங்கள், அல்லது **Back to Sales Plan** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Missing CSR-2 ID. Open this page from Sales Plan."** (CSR-2 ID இல்லை. இந்தப் பக்கத்தை Sales Plan-லிருந்து திறங்கள்) சேமித்த லிங்கிலிருந்து அல்ல, Sales Plan-லிருந்து திரையைத் திறங்கள்.
- **"Upload an .xlsx or .xls file up to 5 MB."** (5 MB வரையிலான .xlsx அல்லது .xls கோப்பை அப்லோட் செய்யுங்கள்) 5 MB-ஐ விடச் சிறிய Excel கோப்பைப் பயன்படுத்துங்கள்.
- **Continue to Plan Review சாம்பல் நிறத்தில் உள்ளது.** முதலில் குறைந்தது ஒரு டீலரை அப்லோட் செய்யுங்கள்.
:::

::: section training-program
title: டீலர் பயிற்சித் திட்டத்தைப் பதிவு செய்வது (CSR-2)
routes: /DealerManagementTrainingProgram
for: 
roles: 
designations: 
minutes: 5
source-hash: f2c7c96961e19cd8
confidence: ok

**இந்தத் திரை எதற்கு**
CSR-1 திட்டத்துக்கு ஒப்புதல் கிடைத்த பிறகு, நிகழ்ச்சி உண்மையில் எப்படி நடந்தது என்று பதிவு செய்ய இந்தத் திரையைப் பயன்படுத்துங்கள்: தேதி, புகைப்படங்கள், பங்கேற்றவர்கள், செலவுகள். **CSR-2 Program Execution** பட்டியலில் **View Program** மூலம் இது திறக்கும்.

1. **Plan & Start**-ல், **Approved CSR-1 Plan**-ஐச் சரிபாருங்கள். **Focus Details**, **Actual Event Date**, **Start Time**, **End Time** மற்றும் **Actual Location**-ஐ நிரப்புங்கள். நிகழ்ச்சியின் புகைப்படம், வீடியோவுக்கு **Take Photo** மற்றும் **Record Video** பயன்படுத்துங்கள். **Start Event** அழுத்தி, உறுதி செய்ய மீண்டும் **Start Event** அழுத்துங்கள்.
2. **Participants & Employees**-ல், **Phone Number** மூலம் பங்கேற்பாளரைத் தேடி, **Role**-ஐத் தேர்வு செய்து **Add Participant** அழுத்துங்கள். இன்னும் பதிவு செய்யாதவருக்கு **New Farmer** பயன்படுத்துங்கள். பணியாளர்களை **Add Employee** மூலம் சேருங்கள்.
3. **Expense & Proof**-ல், ஒவ்வொரு செலவுக்கும் **Actual amount (₹)**, **Bill No.** மற்றும் **Date**-ஐ உள்ளிட்டு, **Upload proofs** அழுத்துங்கள். கார்டுகள் **Approved Budget**-ஐயும் **Actual Expense**-ஐயும் ஒப்பிட்டுக் காட்டும்.
4. **Review & Submit**-ல், எல்லாவற்றையும் சரிபாருங்கள். ஒரு கார்டில் **Edit** அழுத்தினால், பின்னால் போய் அதை மாற்றலாம்.
5. பிறகு முடிக்க **Save Draft** அழுத்துங்கள். அனுப்ப **Apply CSR-2** அழுத்துங்கள், அல்லது அது உங்களுக்காகக் காத்திருந்தால் **Validate CSR-2** அல்லது ஒப்புதல் பட்டனை அழுத்துங்கள்.

> Tip: புகைப்படங்களிலும் வீடியோக்களிலும் GPS இடம், தேதி மற்றும் நேரம் பதிவாகும்.

## பொதுவான பிரச்சனைகள்

- **"CSR-2 can be opened only for an approved CSR-1."** (ஒப்புதல் பெற்ற CSR-1-க்கு மட்டுமே CSR-2 திறக்க முடியும்) CSR-1 திட்டத்துக்கு ஒப்புதல் கிடைக்கும் வரை காத்திருங்கள்.
- **எனக்கு இரண்டு படிகள் மட்டுமே தெரிகின்றன.** சில பணிப்பொறுப்புகள் **Plan & Start** மற்றும் **Participants & Employees** மட்டுமே நிரப்பி, பிறகு **Apply CSR-2** அழுத்துவார்கள்.
- **"Enter a valid 10-digit mobile number."** (சரியான 10 இலக்க மொபைல் எண்ணை உள்ளிடுங்கள்) ஃபோன் எண்ணைச் சரிபாருங்கள்.
:::
