---
module: community
title: கம்யூனிட்டி
icon: bi-chat-square-text
order: 115
language: ta
category: அறிவு
note: புதிய வழிகாட்டி
meta-source-hash: 1b034d33bc7629a5
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 59061c3615515658
confidence: ok

**SPIC Knowledge Community** என்பது product-கள் மற்றும் விவசாயம் பற்றிக் கேள்வி கேட்கவும், வயலில் நீங்கள் பார்த்ததைப் பகிரவும், SPIC நிபுணர்களிடமும் மற்ற விவசாயிகளிடமும் பதில் பெறவும் உள்ள ஒரு இடம்.

- ask | icon: bi-patch-question | color: #0d9488
  title: கேள்வி கேளுங்கள்
  text: உங்கள் பிரச்சனையை உங்கள் சொந்த வார்த்தைகளில் எழுதுங்கள். உதவியாக இருந்தால் புகைப்படங்கள் அல்லது ஆவணங்களைச் சேருங்கள்.
- answer | icon: bi-patch-check | color: #16a34a
  title: நிபுணர்களிடம் பதில் பெறுங்கள்
  text: SPIC பணியாளர்கள் **SPIC Expert** என்ற குறியுடன் பதில் தருவார்கள். மற்ற விவசாயிகளும் டீலர்களும் பதில் தரலாம்.
- learn | icon: bi-people | color: #2563eb
  title: மற்றவர்களிடமிருந்து கற்றுக்கொள்ளுங்கள்
  text: பழைய விவாதங்களைத் தேடுங்கள், பிடித்தவற்றைப் பின்தொடருங்கள், product குழுக்களில் சேருங்கள்.
:::

::: section flow
title: ஒரு கேள்வி எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 8b3d8c2b9b5b6d84
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு குழு, ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- members | type: lane | color: #0d9488 | roles: Farmer, Dealer
  title: விவசாயிகளும் டீலர்களும்
  sub: கேட்பது, பதில் தருவது, பகிர்வது
- experts | type: lane | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC நிபுணர்கள்
  sub: SPIC பணியாளர்கள்
- ask | lane: members | col: 1 | icon: bi-pencil-square | color: #d97706 | next: reply, expert | screen: ask-question
  title: கேள்வி கேட்பது
  status: Waiting For Reply
  text: **Create a Discussion** படிவத்தை நிரப்பி **Post Discussion** அழுத்துங்கள். கேள்வி தன் முதல் பதிலுக்காகக் காத்திருக்கும்.
- similar | lane: members | col: 2 | icon: bi-search | color: #ea580c | kind: exception | from: ask | to: ask | screen: ask-question
  title: இதே போன்ற கேள்வி கிடைத்தது
  text: உங்கள் கேள்வி போலவே தெரியும் விவாதங்களை ஆப் காட்டும். பதிலைப் படிக்க ஒன்றைத் திறங்கள், அல்லது **Back to edit** அழுத்துங்கள், அல்லது **Post Anyway** அழுத்துங்கள்.
- reply | lane: members | col: 3 | icon: bi-chat-dots | color: #2563eb | next: resolve | screen: discussion-details
  title: ஒரு உறுப்பினர் பதில் தருகிறார்
  status: Open
  text: வேறொரு விவசாயி அல்லது டீலர் பதில் தருகிறார். காத்திருந்த கேள்வி **Open** ஆகிவிடும்.
- expert | lane: experts | col: 3 | icon: bi-patch-check | color: #16a34a | next: resolve | screen: discussion-details
  title: ஒரு நிபுணர் பதில் தருகிறார்
  status: Expert Answer
  text: SPIC பணியாளர்கள் பதில் தந்ததும், விவாதத்தில் **Expert Answer** என்று காட்டும்.
- resolve | lane: members | col: 4 | icon: bi-check2-circle | color: #64748b | return: reply | screen: discussion-details
  title: தீர்ந்தது என்று குறிப்பது
  status: Resolved
  text: பிரச்சனை தீர்ந்ததும், கேள்வி கேட்டவர் **Mark as Resolved** அழுத்துவார். **Reopen** அழுத்தினால் அது மீண்டும் **Open** ஆகும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: af226e0559d41a5d
confidence: ok

- waiting | color: #d97706
  label: Waiting For Reply
  text: பதிவிடப்பட்டது, ஆனால் இதுவரை யாரும் பதில் தரவில்லை.
- waiting-answers | color: #d97706
  label: Waiting for Answers
  text: நீங்கள் கேள்வி கேட்ட உடனே வரும் "posted" திரையில் இது தெரியும். இதன் அர்த்தமும் Waiting For Reply-ம் ஒன்றுதான்.
- open | color: #2563eb
  label: Open
  text: உறுப்பினர்கள் பதில் தந்துள்ளனர். விவாதம் இன்னும் நடந்துகொண்டிருக்கிறது.
- expert | color: #16a34a
  label: Expert Answer
  text: ஒரு SPIC நிபுணர் பதில் தந்துள்ளார்.
- resolved | color: #64748b
  label: Resolved
  text: பிரச்சனை தீர்ந்தது என்று கேள்வி கேட்டவர் குறித்துவிட்டார். இதை மீண்டும் திறக்கலாம்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: b913d791fc7175cc
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். லாகின் செய்யும் எல்லோரும் விவாதங்களைப் படிக்கலாம், அவற்றில் கலந்துகொள்ளலாம்.

- members | color: #0d9488 | roles: Farmer, Dealer
  title: விவசாயிகளும் டீலர்களும்
  - கேள்வி கேட்பது, புகைப்படங்கள் அல்லது கோப்புகளைச் சேர்ப்பது
  - பதில் தருவது, லைக் செய்வது, சேமிப்பது, விவாதங்களைப் பின்தொடர்வது
  - தங்கள் சொந்தக் கேள்விகளைத் திருத்துவது, நீக்குவது அல்லது தீர்ந்ததாகக் குறிப்பது
- experts | color: #16a34a | roles: Admin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD, MDO, MO, JMDO, SpecialAdmin, SuperAdmin, MDAdmin, MDHOD
  title: SPIC நிபுணர்கள் (SPIC பணியாளர்கள்)
  - கேள்விகளுக்குப் பதில் தருவது. உங்கள் பதில்களில் **SPIC Expert** என்று காட்டும்
  - மற்ற உறுப்பினர்களைப் போலவே கேள்வி கேட்பது, பகிர்வது
- moderators | color: #475569 | roles: Admin, CorporateAdmin
  title: Admin மற்றும் Corporate Admin
  - எந்த விவாதத்தையும் தீர்ந்ததாகக் குறிப்பது, அல்லது மீண்டும் திறப்பது
  - எந்தப் பதிலையும் நீக்குவது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 971404c02ac40d12
confidence: ok

- cannot-post | popular: true
  q: Post Discussion அழுத்தினால் சிவப்பு நிறச் செய்திகள் வருகின்றன
  a: தலைப்பில் குறைந்தது 10 எழுத்துகள் இருக்க வேண்டும், ஒரு **Category** தேர்வு செய்ய வேண்டும், **Description**-ல் குறைந்தது 20 எழுத்துகள் இருக்க வேண்டும். சிவப்பில் உள்ள புலத்தைச் சரிசெய்து மீண்டும் **Post Discussion** அழுத்துங்கள்.
- similar | popular: true
  q: "We found similar discussions" என்று ஆப் சொல்கிறது. நான் என்ன செய்வது?
  a: உங்கள் கேள்விக்கு ஏற்கனவே பதில் இருக்கலாம். அதைப் படிக்க **View Discussion** அழுத்துங்கள். உங்கள் கேள்வி வேறு என்றால், **Post Anyway** அழுத்துங்கள். நீங்கள் எழுதியதை மாற்ற **Back to edit** அழுத்துங்கள்.
- edit-delete
  q: என் கேள்வியை எப்படி மாற்றுவது அல்லது நீக்குவது?
  a: **Discussions**-ஐத் திறந்து, **My Discussions** டேபுக்குப் போய், உங்கள் கேள்வியில் உள்ள மூன்று புள்ளிகளைத் தட்டுங்கள். **Edit** அல்லது **Delete** தேர்வு செய்யுங்கள்.
- no-resolve
  q: எனக்கு Mark as Resolved தெரியவில்லை
  a: கேள்வி கேட்டவர் அல்லது Admin மட்டுமே ஒரு விவாதத்தைத் தீர்ந்ததாகக் குறிக்கவோ மீண்டும் திறக்கவோ முடியும்.
- file-not-attached
  q: என் கோப்பு அல்லது புகைப்படம் சேரவில்லை
  a: ஒரு கேள்வியில் உள்ள ஒவ்வொரு கோப்பும் 10 MB அல்லது அதற்குக் குறைவாக இருக்க வேண்டும். ஒரு கேள்வியில் 6 கோப்புகள் வரை சேர்க்கலாம். ஒரு பதிலில், ஒவ்வொன்றும் 5 MB வரை உள்ள 3 படங்கள் வரை சேர்க்கலாம்.
- expert-answers
  q: SPIC நிபுணர்களின் பதில்களை எப்படிக் கண்டுபிடிப்பது?
  a: **Discussions**-ஐத் திறந்து **Expert Answers** டேபைத் தட்டுங்கள். ஒரு விவாதத்தில், நிபுணர்களின் பதில்களில் **SPIC Expert** குறி இருக்கும்.
:::

::: section community-home
title: Knowledge Community முகப்புப் பக்கத்தைப் பயன்படுத்துவது
routes: /Community
for: 
roles: 
designations: 
minutes: 2
source-hash: ea1bb46391aec31c
confidence: ok

**இந்தத் திரை எதற்கு**
**SPIC Knowledge Community** முகப்புப் பக்கத்திலிருந்துதான் நீங்கள் தொடங்குவீர்கள். இங்கே கேள்வி கேட்கலாம், புதிய விவாதங்களைப் பார்க்கலாம், ஒரு product-க்கான விவாதங்களைத் திறக்கலாம்.

1. தேடல் பெட்டியில் உங்கள் கேள்வி அல்லது product பெயரை டைப் செய்து **Ask a Question** அழுத்துங்கள். நீங்கள் டைப் செய்த வார்த்தைகளே தலைப்பாக, கேள்விப் படிவம் திறக்கும்.
2. தேடல் பெட்டிக்குக் கீழே உள்ள எண்களைப் பாருங்கள்: **Community Members**, **Discussions** மற்றும் **Expert Answer**.
3. **Recent Discussions**-ன் கீழ், ஒரு விவாதத்தைத் திறக்க அதைத் தட்டுங்கள். பட்டியலைக் கட்டங்களாக (grid) அல்லது வரிசைகளாகக் காட்ட இரண்டு சிறிய பட்டன்களைப் பயன்படுத்துங்கள்.
4. டேபுகள் மற்றும் ஃபில்டர்களுடன் முழுப் பட்டியலைப் பார்க்க **View All Discussions** அழுத்துங்கள்.
5. **Popular Product**-ன் கீழ், ஒரு product குழுவில் சேர **Join** அழுத்துங்கள். அது **Joined** என்று மாறும். விலக மீண்டும் அழுத்துங்கள்.
6. அந்த product பற்றிய விவாதங்களை மட்டும் பார்க்க, product கார்டில் உள்ள **Discussions** அழுத்துங்கள்.
7. **New Here?** பெட்டியில், புதிய கேள்வி தொடங்க **Ask a Question** அழுத்துங்கள்.

> Tip: நீங்கள் கட்டங்களாகப் பார்க்கத் தேர்வு செய்தீர்களா, வரிசைகளாகப் பார்க்கத் தேர்வு செய்தீர்களா என்பதை ஆப் நினைவில் வைத்துக்கொள்ளும்.

## Product படத்தை மாற்றுவது

- Digital Library-ஐத் திருத்தக்கூடிய Admin மற்றும் பணியாளர்களுக்கு, ஒவ்வொரு product படத்திலும் ஒரு கேமரா ஐகான் தெரியும்.
- கேமராவைத் தட்டி, 3 MB வரை உள்ள JPG, PNG அல்லது WEBP படத்தைத் தேர்வு செய்யுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Discussions could not be loaded".** (விவாதங்களைக் காட்ட முடியவில்லை) இணைப்பு தோல்வியடைந்தது. **Try again** அழுத்துங்கள்.
- **"No discussions yet".** (இதுவரை விவாதங்கள் இல்லை) இதுவரை யாரும் கேள்வி கேட்கவில்லை. முதல் ஆளாகக் கேட்க **Ask a Question** அழுத்துங்கள்.
- **"That picture is larger than 3 MB."** (அந்தப் படம் 3 MB-ஐ விடப் பெரியது) சிறிய product படத்தைத் தேர்வு செய்யுங்கள்.
:::

::: section browse-discussions
title: விவாதங்களைக் கண்டுபிடித்துப் பின்தொடர்வது
routes: /Community/discussions
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-list-stats] | 2 > [data-help=cm-list-tabs] | 3 > [data-help=cm-list-filters] | 4 > [data-help=cm-list-tools] | 5 > [data-help=cm-list-card] | 6 > [data-help=cm-list-new]
minutes: 3
source-hash: 6111e390f7350721
confidence: ok

**இந்தத் திரை எதற்கு**
**Discussions** திரையில் கம்யூனிட்டியில் உள்ள எல்லாக் கேள்விகளும் பட்டியலாகத் தெரியும். இங்கே தேடலாம், ஃபில்டர் செய்யலாம், விவாதங்களைப் பின்தொடரலாம், உங்கள் சொந்தக் கேள்விகளை நிர்வகிக்கலாம்.

1. மேலே உள்ள கட்டங்களைப் பாருங்கள்: **Total Discussions**, **Active Members**, **Expert Answers** மற்றும் **New This Month**.
2. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All Discussion**, **My Discussions**, **Trending**, **Unanswered**, **Following** அல்லது **Expert Answers**.
3. **Filters**-ஐத் திறந்து **Date Range**, **Category**, **Product**, **Crop**, **Status** அல்லது **Tags** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
4. ஒரு தலைப்பைக் கண்டுபிடிக்க **Sort by:** (**Latest Activity**, **Most Replies**, **Most Views** அல்லது **Oldest**) பயன்படுத்துங்கள், **Search discussions...**-ல் டைப் செய்யுங்கள்.
5. ஒரு விவாதத்தைத் திறக்க அதைத் தட்டுங்கள். அதைப் பின்தொடர, அந்த நபரின் படத்துக்குக் கீழே உள்ள **Follow** அழுத்துங்கள். உங்கள் சொந்தக் கேள்விகளில், **Edit** அல்லது **Delete** செய்ய மூன்று புள்ளிகளைத் தட்டுங்கள்.
6. புதிய கேள்வி கேட்க **Start a Discussion** அழுத்துங்கள்.

> Careful: ஒரு விவாதத்தை நீக்கினால், அதுவும் அதன் பதில்களும் கம்யூனிட்டியிலிருந்து நீங்கிவிடும்.

## டேபுகள் என்ன காட்டும்

- **My Discussions**: நீங்கள் கேட்ட கேள்விகள்.
- **Trending**: கடந்த 30 நாட்களில் அதிகம் பேசப்பட்ட கேள்விகள்.
- **Unanswered**: இதுவரை பதில் வராத கேள்விகள்.
- **Following**: நீங்கள் பின்தொடரும் கேள்விகள்.
- **Expert Answers**: SPIC நிபுணர் பதில் தந்த கேள்விகள்.

## பொதுவான பிரச்சனைகள்

- **"No discussions found".** (விவாதங்கள் எதுவும் கிடைக்கவில்லை) வேறு டேபைப் பாருங்கள், அல்லது **Clear filters** அழுத்துங்கள்.
- **ஒரு விவாதத்தில் மூன்று புள்ளிகள் இல்லை.** உங்கள் சொந்தக் கேள்விகளை மட்டுமே திருத்தவோ நீக்கவோ முடியும்.
- **Tags ஃபில்டர் வேலை செய்யவில்லை.** **Apply Filters** அழுத்தும் முன், ஒரு tag-ஐ டைப் செய்து Enter அழுத்துங்கள்.
:::

::: section ask-question
title: கேள்வி கேட்பது
routes: /Community/new, /Community/posted/{id}
for: 
roles: 
designations: 
tour: 1 > [data-help=cm-new-title] | 2 > [data-help=cm-new-choices] | 3 > [data-help=cm-new-tags] | 4 > [data-help=cm-new-desc] | 5 > [data-help=cm-new-files] | 6 > [data-help=cm-new-post]
minutes: 3
source-hash: a42a0e7fda40c257
confidence: ok

**இந்தத் திரை எதற்கு**
கம்யூனிட்டியிடம் கேள்வி கேட்க அல்லது உங்கள் அனுபவத்தைப் பகிர **Create a Discussion** திரையைப் பயன்படுத்துங்கள். பிறகு SPIC நிபுணர்களும் மற்ற விவசாயிகளும் பதில் தருவார்கள்.

1. உங்கள் கேள்வியை **Discussion Title**-ல் டைப் செய்யுங்கள்.
2. ஒரு **Category** தேர்வு செய்யுங்கள். அது ஒரு product அல்லது பயிர் பற்றியது என்றால், **Related Product** மற்றும் **Crop**-ஐயும் தேர்வு செய்யுங்கள்.
3. **Tags**-ன் கீழ், ஒரு நோய் அல்லது பிரச்சனை போன்ற ஒரு சிறிய வார்த்தையை டைப் செய்து Enter அல்லது காற்புள்ளி (comma) அழுத்துங்கள். 6 tag-கள் வரை சேர்க்கலாம்.
4. **Description**-ல் உங்கள் கேள்வியையும், நீங்கள் என்ன செய்து பார்த்தீர்கள், என்ன கவனித்தீர்கள் என்பதையும் எழுதுங்கள்.
5. **Attachments**-ன் கீழ், புகைப்படங்கள் அல்லது கோப்புகளைப் பெட்டியில் போடுங்கள், அல்லது **Browse Files** அழுத்துங்கள். ஒரு கோப்பை நீக்க அதன் பெருக்கல் குறியை (X) தட்டுங்கள்.
6. **Post Discussion** அழுத்துங்கள். **We found similar discussions** என்று ஆப் காட்டினால், அவற்றைப் படியுங்கள், அல்லது **Post Anyway** அழுத்துங்கள். **Your question looks unique!** என்று காட்டினால், மீண்டும் **Post Discussion** அழுத்துங்கள்.

> Tip: இலை, செடி அல்லது product லேபிளின் தெளிவான புகைப்படம் இருந்தால், நிபுணர்கள் வேகமாகப் பதில் தருவார்கள்.

## பதிவிட்ட பிறகு

- **Your question has been posted** திரையில் தலைப்பு, **Category**, **Posted** நேரம், **Status** மற்றும் **Views** தெரியும்.
- உங்கள் கேள்வியைத் திறக்க **View Discussion** அழுத்துங்கள், அல்லது முகப்புப் பக்கத்துக்குப் போக **Back to Community** அழுத்துங்கள்.

## உங்கள் கேள்வியைத் திருத்துவது

- **Discussions**-ல், உங்கள் கேள்வியில் உள்ள மூன்று புள்ளிகளைத் தட்டி **Edit** தேர்வு செய்யுங்கள். அதே படிவம் திறக்கும்.
- எழுத்தை மாற்றி **Save Changes** அழுத்துங்கள்.
- நீங்கள் டைப் செய்ததை அழிக்கும் முன் **Cancel** உங்களிடம் கேட்கும். தொடர்ந்து திருத்த **Keep editing** அழுத்துங்கள்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Discussion Title** | உங்கள் கேள்வி ஒரே வரியில். குறைந்தது 10 எழுத்துகள். கட்டாயம். |
| **Category** | கேள்வியின் வகை. கட்டாயம். |
| **Related Product** | இது எந்த SPIC product பற்றியது (இருந்தால்). |
| **Crop** | பயிர், உதாரணமாக நெல் அல்லது தக்காளி. |
| **Tags** | மற்றவர்கள் இதைக் கண்டுபிடிக்க உதவும் சிறிய வார்த்தைகள், 6 வரை. |
| **Description** | முழுக் கேள்வி. குறைந்தது 20 எழுத்துகள். கட்டாயம். |
| **Attachments** | படங்கள், PDF அல்லது Word கோப்புகள், ஒவ்வொன்றும் 10 MB வரை. |

## பொதுவான பிரச்சனைகள்

- **"Please enter a discussion title."** அல்லது **"The title needs at least 10 characters."** (தலைப்பை எழுதுங்கள் / தலைப்பில் குறைந்தது 10 எழுத்துகள் வேண்டும்) இன்னும் நீளமான தலைப்பு எழுதுங்கள்.
- **"Please add a little more detail (at least 20 characters)."** (இன்னும் கொஞ்சம் விவரம் சேருங்கள், குறைந்தது 20 எழுத்துகள்) **Description**-ல் இன்னும் அதிகமாக எழுதுங்கள்.
- **"That file is larger than 10 MB and was not attached."** (அந்தக் கோப்பு 10 MB-ஐ விடப் பெரியது, அதனால் சேர்க்கப்படவில்லை) கோப்பைச் சிறியதாக்குங்கள், அல்லது புதிய புகைப்படம் எடுங்கள்.
- **"Your question was posted, but the attachments could not be uploaded."** (உங்கள் கேள்வி பதிவானது, ஆனால் இணைப்புகளை அப்லோட் செய்ய முடியவில்லை) கேள்வி சேமிக்கப்பட்டுவிட்டது. உங்கள் இணைய இணைப்பைச் சரிபாருங்கள்.
:::

::: section discussion-details
title: ஒரு விவாதத்தைப் படித்துப் பதில் தருவது
routes: /Community/discussion/{id}
for: 
roles: 
designations: 
minutes: 3
source-hash: 59af4bc85c32cb5c
confidence: ok

**இந்தத் திரை எதற்கு**
**Discussion Details** திரையில் ஒரு கேள்வி, அதன் புகைப்படங்கள், கோப்புகள் மற்றும் எல்லாப் பதில்களும் தெரியும். இங்கே பதில் தரலாம், லைக் செய்யலாம், சேமிக்கலாம், பின்தொடரலாம், பகிரலாம்.

1. மேலே, யார் கேட்டார், அவருடைய குறி (**Farmer**, **Dealer** அல்லது **SPIC Expert**), தேதி மற்றும் நிலை ஆகியவற்றைப் பாருங்கள்.
2. கேள்வியைப் படியுங்கள். ஒரு புகைப்படத்தைப் பெரிதாகப் பார்க்க அதைத் தட்டுங்கள். ஒரு கோப்பைத் திறக்க அதைத் தட்டுங்கள்.
3. மேலே உள்ள **Save**, **Share** மற்றும் **Follow** பயன்படுத்துங்கள். கேள்வியை லைக் செய்ய கட்டைவிரல் பட்டனை அழுத்துங்கள்.
4. **Replied**-ன் கீழ், பதில்களைப் படியுங்கள். **Latest**, **Oldest** அல்லது **Most Liked** தேர்வு செய்ய **Sort by:** பயன்படுத்துங்கள்.
5. ஒரு பதிலுக்கு மட்டும் பதில் தர, அதன் **reply** பட்டனைத் தட்டுங்கள். அந்த நபரின் பெயருடன் ஒரு பெட்டி திறக்கும். உங்கள் பதிலை எழுதி **Reply** அழுத்துங்கள்.
6. கேள்விக்குப் பதில் தர, கீழே உள்ள **Write a reply...**-ல் டைப் செய்யுங்கள். தேவைப்பட்டால் 3 படங்கள் வரை அல்லது ஒரு லிங்க் சேர்த்து, அனுப்பும் பட்டனை அழுத்துங்கள்.
7. உங்கள் பிரச்சனை தீர்ந்ததும், **Mark as Resolved** அழுத்தி உறுதி செய்யுங்கள். இன்னும் உதவி தேவைப்பட்டால் **Reopen** அழுத்துங்கள்.

> Tip: கம்ப்யூட்டரில், Enter அழுத்தினால் பதில் அனுப்பப்படும். புதிய வரி தொடங்க Shift-உடன் Enter அழுத்துங்கள்.
> Careful: ஒரு பதிலில் உள்ள குப்பைத்தொட்டி ஐகான் அந்தப் பதிலை நீக்கும். உங்கள் சொந்தப் பதில்களை மட்டுமே நீக்க முடியும். Admin எந்தப் பதிலையும் நீக்கலாம்.

## பொதுவான பிரச்சனைகள்

- **"Discussion not found".** (விவாதம் கிடைக்கவில்லை) கேள்வி நீக்கப்பட்டுவிட்டது. **Back to Discussions** அழுத்துங்கள்.
- **"Please write a few words with your images."** (படங்களுடன் சில வார்த்தைகள் எழுதுங்கள்) பதிலில் படங்கள் மட்டும் போதாது, கொஞ்சம் எழுத்தும் வேண்டும்.
- **"A reply can carry up to 3 images."** (ஒரு பதிலில் 3 படங்கள் வரை மட்டுமே சேர்க்கலாம்) ஒரு படத்தை நீக்குங்கள், அல்லது மீதியை இன்னொரு பதிலில் போடுங்கள்.
- **Mark as Resolved பட்டன் இல்லை.** கேள்வி கேட்டவர் அல்லது Admin மட்டுமே தீர்ந்ததாகக் குறிக்கவோ மீண்டும் திறக்கவோ முடியும்.
:::
