---
module: field-force
title: களப் பணியாளர்கள்
icon: bi-person-walking
order: 107
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
source-hash: d557872762e3ea47
confidence: ok

ஒவ்வொரு வாரத்துக்கான களப் பணியைத் திட்டமிடுங்கள், அதற்கு ஒப்புதல் வாங்குங்கள், வருகையைப் பதிவு செய்யுங்கள், களத்தில் டாஸ்க்குகளைச் செய்யுங்கள். யார் வேலை செய்கிறார்கள், எங்கே இருக்கிறார்கள், இலக்குக்கு எதிராக ஒவ்வொருவரும் எவ்வளவு முடித்திருக்கிறார்கள் என்று மேனேஜர்கள் பார்க்கலாம்.

- plan | icon: bi-calendar-week | color: #ea580c
  title: JMDO வாரத்தைத் திட்டமிடுவார்
  text: ஒவ்வொரு நாளுக்கும் டீலர்களைத் தேர்வு செய்து, SPCM, Soil Sample, PoS liquidation இலக்குகளை வைத்து, திட்டத்தை அனுப்புங்கள்.
- approve | icon: bi-person-check | color: #2563eb
  title: MDO ஒப்புதல் தருவார்
  text: MDO திட்டத்தைச் சரிபார்த்து, தேவைப்பட்டால் டீலர்கள், தேதிகள் அல்லது இலக்குகளை மாற்றி, ஒப்புதல் தருவார்.
- work | icon: bi-geo-alt | color: #16a34a
  title: களத்தில் வேலை, நேரடியாகக் கண்காணிப்பு
  text: JMDO வருகையைப் பதிவு செய்து, ஒவ்வொரு டாஸ்க்கையும் தொடங்கி முடிப்பார். மேனேஜர்கள் டாஸ்க்குகள், இருப்பிடம், ரிப்போர்ட்களைப் பின்தொடர்வார்கள்.
:::

::: section flow
title: ஒரு வாரக் களப் பணி எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2462b99188302817
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- jmdo | type: lane | color: #ea580c | roles: JMDO
  title: JMDO
  sub: களப் பணியாளர்
- mdo | type: lane | color: #2563eb | roles: MDO, MO
  title: MDO
  sub: உங்கள் HQ-வில் உள்ள டீம் மேனேஜர்
- plan | lane: jmdo | col: 1 | icon: bi-calendar-week | color: #d97706 | next: approve | screen: plan-week
  title: வாரத்தைத் திட்டமிடுதல்
  status: Pending
  text: ஒவ்வொரு நாளுக்கும் டீலர்களைத் தேர்வு செய்து, வார இலக்குகளை உள்ளிட்டு, **Request** அழுத்துங்கள். திட்டம் MDO-க்காகக் காத்திருக்கும்.
- approve | lane: mdo | col: 2 | icon: bi-person-check | color: #2563eb | next: attend | screen: review-allocation
  title: சரிபார்த்து ஒப்புதல் தருதல்
  status: Approved
  text: MDO டீலர் டாஸ்க்குகளைச் சேர்க்கலாம் அல்லது நீக்கலாம், தேதிகள், முன்னுரிமை, இலக்குகளை மாற்றலாம், பிறகு **Approve Allocation** அழுத்துவார்.
- attend | lane: jmdo | col: 3 | icon: bi-calendar-check | color: #0d9488 | next: work | screen: mark-attendance
  title: வருகையைப் பதிவு செய்தல்
  status: Present
  text: ஒவ்வொரு வேலை நாளிலும், எந்த டாஸ்க்கையும் தொடங்கும் முன், SPIC ONE மொபைல் ஆப்பில் வருகையைப் பதிவு செய்யுங்கள்.
- work | lane: jmdo | col: 4 | icon: bi-play-circle | color: #7c3aed | next: close | screen: todays-tasks
  title: டாஸ்க்கைத் தொடங்குதல்
  status: In Progress
  text: **Today's Allocation**-ல் **Start** அழுத்தி, பிறகு **Start Task** அழுத்துங்கள். நேரமும் இருப்பிடமும் பதிவாகும்.
- close | lane: jmdo | col: 5 | icon: bi-check-circle | color: #16a34a | next: monitor | screen: todays-tasks
  title: சேமித்து முடித்தல்
  status: Completed
  text: **Close-out form**-ஐ நிரப்பி, **Save Record** அழுத்தி, பிறகு டாஸ்க்கை முடிக்க **Stop Task** அழுத்துங்கள்.
- monitor | lane: mdo | col: 6 | icon: bi-list-check | color: #475569 | screen: task-monitoring
  title: பின்தொடர்தல்
  status: Completed
  text: MDO ஒவ்வொரு டாஸ்க்கையும் **Task Monitoring**-லும், டீமின் இருப்பிடத்தை **Team Tracking**-லும், முன்னேற்றத்தை **Reports**-லும் பார்ப்பார்.
- no-attendance | lane: jmdo | col: 6 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: work | to: attend | screen: mark-attendance
  title: வருகை பதிவு செய்யப்படவில்லை
  status: Yet to Start
  text: இன்றைய வருகையைப் பதிவு செய்யும் முன் டாஸ்க்கைத் தொடங்க முடியாது. ஆப் உங்களுக்காக **Mark Attendance**-ஐத் திறக்கும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 80c93f090c1c74d0
confidence: ok

- pending | color: #d97706
  label: Pending
  text: வாரத் திட்டம் அனுப்பப்பட்டது, MDO-வின் ஒப்புதலுக்காகக் காத்திருக்கிறது. JMDO-க்கு "awaiting MDO approval" என்று தெரியும்.
- approved | color: #16a34a
  label: Approved
  text: MDO வாரத் திட்டத்துக்கு ஒப்புதல் தந்துவிட்டார். அதன் டாஸ்க்குகளை அந்தந்த நாளில் தொடங்கலாம்.
- rejected | color: #dc2626
  label: Rejected
  text: வாரத் திட்டம் ஏற்றுக்கொள்ளப்படவில்லை. திட்டத்தைத் திருத்தி மீண்டும் அனுப்புங்கள்.
- requested | color: #d97706
  label: Requested
  text: Task Monitoring-ல், வாரத் திட்டம் இன்னும் ஒப்புதலுக்காகக் காத்திருக்கும் டாஸ்க்.
- yet-to-start | color: #64748b
  label: Yet to Start
  text: டாஸ்க்குக்கு ஒப்புதல் கிடைத்துவிட்டது, ஆனால் இன்னும் தொடங்கவில்லை.
- not-started | color: #64748b
  label: Not Started
  text: Reports-ல், இது Yet to Start-க்குச் சமம்.
- in-progress | color: #2563eb
  label: In Progress
  text: JMDO Start Task அழுத்தி, அந்த வேலையைச் செய்துகொண்டிருக்கிறார்.
- completed | color: #16a34a
  label: Completed
  text: டாஸ்க் முடிக்கப்பட்டது.
- fully | color: #16a34a
  label: Fully Completed
  text: முடிவுப் படிவத்தின் விளைவு: எல்லா வேலையும் முடிந்தது.
- partially | color: #d97706
  label: Partially Completed
  text: முடிவுப் படிவத்தின் விளைவு: வேலையின் ஒரு பகுதி மட்டுமே முடிந்தது.
- not-completed | color: #ea580c
  label: Not Completed
  text: முடிவுப் படிவத்தின் விளைவு: வேலையைச் செய்ய முடியவில்லை.
- present | color: #16a34a
  label: Present
  text: முழு வேலை நாளுக்கான வருகை. உங்களை On Duty ஆக்கும்.
- half-day | color: #0d9488
  label: Half Day
  text: அரை நாளுக்கான வருகை. உங்களை On Duty ஆக்கும்.
- on-leave | color: #7c3aed
  label: On Leave
  text: இன்று நீங்கள் விடுப்பில் இருக்கிறீர்கள். நீங்கள் Off Duty.
- absent | color: #dc2626
  label: Absent
  text: வராத (absent) நாளுக்கு வாராந்திரச் சுருக்கத்தில் இது தெரியும்.
- active | color: #16a34a
  label: Active
  text: டீம் டாஷ்போர்டில், இன்று விடுப்பில் இல்லாத JMDO.
- on-duty | color: #16a34a
  label: On Duty
  text: இன்று வருகை Present அல்லது Half Day என்று பதிவாகியுள்ளது.
- off-duty | color: #64748b
  label: Off Duty
  text: இன்று இன்னும் வருகை பதிவாகவில்லை, அல்லது On Leave.
- live | color: #16a34a
  label: Live
  text: Live Tracking ஆன் ஆக உள்ளது, இருப்பிடம் இப்போது வந்துகொண்டிருக்கிறது.
- stale | color: #d97706
  label: Stale
  text: Live Tracking ஆன் ஆக உள்ளது, ஆனால் கடந்த சில நிமிடங்களில் புதிய இருப்பிடம் வரவில்லை.
- offline | color: #dc2626
  label: Offline
  text: Live Tracking ஆன் ஆக உள்ளது, ஆனால் 15 நிமிடங்களுக்கு மேல் இருப்பிடம் எதுவும் வரவில்லை.
- auto-ended | color: #475569
  label: Ended automatically
  text: நீண்ட நேரம் டிராக்கிங்கிலிருந்து எதுவும் வரவில்லை, அதனால் ஆப் அதை நிறுத்திவிட்டது.
- stopped | color: #64748b
  label: Stopped
  text: JMDO Stop Tracking அழுத்தினார்.
- never | color: #94a3b8
  label: Never started
  text: JMDO இன்னும் Live Tracking-ஐத் தொடங்கவில்லை.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: ca8f05faf80da07f
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- jmdo | color: #ea580c | roles: JMDO
  title: JMDO (களப் பணியாளர்)
  - வாரத்தைத் திட்டமிட்டு Request அழுத்துவது
  - ஒவ்வொரு வேலை நாளிலும் வருகையைப் பதிவு செய்வது
  - ஒவ்வொரு டாஸ்க்கையும் தொடங்குவது, சேமிப்பது, முடிப்பது
  - Live Tracking-ஐத் தொடங்குவது, நிறுத்துவது
  - My Reports-ஐப் பார்ப்பது
- mdo | color: #2563eb | roles: MDO, MO
  title: MDO
  - உங்கள் JMDO-க்களின் வாரத் திட்டங்களுக்கு ஒப்புதல் தருவது
  - ஒவ்வொரு JMDO-வுக்கும் ஆண்டு இலக்குகளை வைப்பது
  - டாஸ்க்குகள், டீமின் இருப்பிடம், ரிப்போர்ட்களைப் பின்தொடர்வது
- admin | color: #475569 | roles: Admin
  title: Admin
  - எல்லா HQ-க்களின் திட்டங்களையும் பார்ப்பது
  - ஒப்புதல் கிடைத்த பிறகும் திட்டத்தைத் திருத்துவது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e1bd21cb8311eb35
confidence: ok

- cannot-start | popular: true
  q: என் டாஸ்க்கைத் தொடங்க முடியவில்லை
  a: இரண்டு விஷயங்கள் சரியாக இருக்க வேண்டும். உங்கள் வாரத் திட்டத்துக்கு MDO **Approved** தந்திருக்க வேண்டும், நீங்கள் இன்றைய வருகையைப் பதிவு செய்திருக்க வேண்டும். வருகை பதிவாகவில்லை என்றால், ஆப் உங்களுக்காக **Mark Attendance**-ஐத் திறக்கும்.
- web-attendance | popular: true
  q: மொபைல் ஆப்பில் இருந்து மட்டுமே பதிவு செய்ய முடியும் என்று Mark Attendance சொல்கிறது
  a: Attendance, Live Tracking, Track Playback ஆகியவை உங்கள் போனில் உள்ள SPIC ONE ஆப்பில் மட்டுமே வேலை செய்யும். போனில் ஆப்பைத் திறந்து மீண்டும் முயற்சி செய்யுங்கள்.
- already-sent
  q: "You've already submitted an allocation for that week." (அந்த வாரத்துக்கு ஏற்கனவே திட்டம் அனுப்பிவிட்டீர்கள்)
  a: ஒரு வாரத்துக்கு ஒரு திட்டம் மட்டுமே அனுப்ப முடியும். டீலர்களைச் சேர்க்க அல்லது நீக்க, அல்லது தேதிகள், இலக்குகளை மாற்ற, **Allocation Requests** திரையில் செய்யுமாறு உங்கள் MDO-விடம் கேளுங்கள்.
- spcm-not-closed
  q: டாஸ்க்கை முடிக்கப் போகும்போது "SPCM target not yet reached" (SPCM இலக்கு இன்னும் எட்டவில்லை) என்று வருகிறது
  a: SPCM மற்றும் PoS liquidation டாஸ்க்குகள் வார இலக்கை எட்டினால் மட்டுமே முடியும். மற்ற நாட்களிலும் ரெக்கார்டுகளைச் சேமித்துக்கொண்டே இருங்கள். இலக்கை எட்டியதும் டாஸ்க் தானாகவே முடிந்துவிடும்.
- no-jmdos
  q: எனக்கு "No JMDOs found for your headquarters" (உங்கள் HQ-வில் JMDO-க்கள் இல்லை) என்று வருகிறது
  a: உங்கள் HQ-வே உள்ள JMDO-க்கள்தான் உங்கள் டீம். யாராவது இல்லை என்றால், அவர்களின் புரொஃபைலில் உள்ள HQ-வைச் சரிபார்க்குமாறு அட்மினிடம் கேளுங்கள்.
- offline
  q: Team Tracking-ல் ஒரு JMDO-வுக்கு Stale அல்லது Offline என்று காட்டுகிறது
  a: அந்த JMDO-வின் போனிலிருந்து கொஞ்ச நேரமாக இருப்பிடம் வரவில்லை. சிக்னல் இல்லாமல் இருக்கலாம், அல்லது ஆப் மூடப்பட்டிருக்கலாம். JMDO-வுக்கு போன் செய்து ஆப்பைத் திறக்கச் சொல்லுங்கள்.
:::

::: section field-dashboard
title: என் களப் பணி டாஷ்போர்டு
routes: /FieldDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: ad0c345e7985b577
confidence: ok

**இந்தத் திரை எதற்கு**
**My Dashboard** என்பது JMDO-வின் முகப்புத் திரை. நீங்கள் டியூட்டியில் இருக்கிறீர்களா, இன்று எத்தனை டாஸ்க்குகள் உள்ளன, Live Tracking, நீங்கள் முடிக்காத டாஸ்க்குகள் ஆகியவை இதில் தெரியும்.

1. மேலே உங்கள் பெயரையும் **On Duty** அல்லது **Off Duty**-ஐயும் பாருங்கள். வருகை பதிவு செய்யவில்லை என்றால், **Mark Attendance** அழுத்துங்கள்.
2. கார்டுகளைப் பாருங்கள்: **Today's Tasks**, **Yet to Start**, **In Progress**, **Completed**.
3. Live Tracking-ஐ ஆன் செய்ய, **Start Live Tracking** அழுத்துங்கள். **Background location access** செய்தியைப் படித்து **Continue** அழுத்துங்கள்.
4. டிராக்கிங் ஆன் ஆக இருக்கும்போது, உங்கள் வழியைப் பார்க்க **View Route** அழுத்துங்கள். நிறுத்த **Stop Tracking**, பிறகு மீண்டும் **Stop Tracking** அழுத்துங்கள். **Keep Tracking** ரத்து செய்யும்.
5. **Continue where you left off**-ன் கீழ், நடந்துகொண்டிருக்கும் டாஸ்க்குக்குத் திரும்ப **view** அழுத்துங்கள். **View All** அழுத்தினால் **Tasks Allocation** திறக்கும்.

> Tip: பட்டனை அழுத்தினால் மட்டுமே Live Tracking தொடங்கும். வருகை அல்லது டாஸ்க்குகளுடன் அது தானாகத் தொடங்காது.

## பொதுவான பிரச்சனைகள்

- **"Mark today's attendance first to enable Start Live Tracking."** (Start Live Tracking-க்கு முதலில் இன்றைய வருகையைப் பதிவு செய்யுங்கள்) வருகையைப் பதிவு செய்து, பிறகு திரும்ப வாருங்கள்.
- **"Mobile app only".** (மொபைல் ஆப்பில் மட்டும்) உங்கள் போனில் உள்ள SPIC ONE ஆப்பில் இருந்து மட்டுமே Live Tracking-ஐத் தொடங்க முடியும்.
- **"Nothing in progress right now".** (இப்போது எதுவும் நடக்கவில்லை) நீங்கள் எந்த டாஸ்க்கையும் தொடங்கவில்லை. ஒன்றைத் தொடங்க **Tasks Allocation**-ஐத் திறங்கள்.
:::

::: section plan-week
title: என் வார டாஸ்க்குகளைத் திட்டமிட்டுக் கோரிக்கை அனுப்புவது
routes: /TasksAllocation
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-week] | 2 > [data-help=ff-steps] | 3 > [data-help=ff-days] | 4 > [data-help=ff-filters] | 5 > [data-help=ff-dealers] | 6 > [data-help=ff-next]
minutes: 4
source-hash: 3effab3c5a7b4e53
confidence: ok

**இந்தத் திரை எதற்கு**
**Tasks Allocation**-ல் உள்ள **All Allocations** டேபில், JMDO ஒரு வாரத்தை (ஞாயிறு முதல் சனி வரை) ஐந்து படிகளில் திட்டமிட்டு, ஒப்புதலுக்காக MDO-க்கு அனுப்புவார்.

1. நீங்கள் திட்டமிட வேண்டிய வாரத்தைத் தேர்வு செய்ய **Previous Week**, **Next Week** பயன்படுத்துங்கள்.
2. இடது பக்கம் உள்ள படிகள்: **Dealer**, **SPCM**, **Soil Sample**, **MD Program**, **PoS liquidation**. ஒரு படியைத் திறக்க அதைத் தட்டுங்கள்.
3. **Dealer** படியில், நீங்கள் திட்டமிடும் நாளைத் தேர்வு செய்ய ஒரு நாள் டேபைத் தட்டுங்கள்.
4. தேடல் பெட்டி (**Search dealer name or code...**) அல்லது **All Districts**, **All Sub Districts**, **All Dealers** பட்டியல்கள் மூலம் டீலர்களைக் கண்டுபிடியுங்கள்.
5. அந்த நாளுக்கு ஒரு டீலரைத் தேர்வு செய்ய அவரின் கார்டைத் தட்டுங்கள். நீக்க மீண்டும் தட்டுங்கள். நீங்கள் எத்தனை டீலர்களைத் தேர்வு செய்தீர்கள் என்று நாள் டேபில் தெரியும்.
6. **Next: SPCM Target** அழுத்துங்கள். SPCM-க்கும் Soil Sample-க்கும் **This Week's Target**-ஐ உள்ளிடுங்கள், MD programs-ஐச் சரிபாருங்கள், PoS liquidation அளவுகளை (**Urea**, **DAP**, **NPKS**, **Others**) உள்ளிடுங்கள், பிறகு **Request** அழுத்துங்கள்.

> Tip: CSR1-ல் உங்களுக்குக் கொடுக்கப்பட்ட MD programs தானாகவே சேர்க்கப்படும். அவற்றை இங்கே சேர்க்கவோ நீக்கவோ முடியாது.
> Careful: ஒரு வாரத்துக்கு ஒரு திட்டம் மட்டுமே அனுப்ப முடியும். **Request**-க்குப் பிறகு, MDO மட்டுமே அதை மாற்ற முடியும்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **This Week's Target** (SPCM) | இந்த வாரம் எத்தனை SPCM அடைய திட்டமிடுகிறீர்கள். |
| **This Week's Target** (Soil Sample) | இந்த வாரம் எத்தனை மண் மாதிரிகள் சேகரிக்கத் திட்டமிடுகிறீர்கள். |
| **Urea**, **DAP**, **NPKS**, **Others** | இந்த வாரம் ஒவ்வொரு product-க்கும் நீங்கள் திட்டமிடும் PoS liquidation அளவு. |

## பொதுவான பிரச்சனைகள்

- **"Fill in at least one step before submitting."** (அனுப்பும் முன் குறைந்தது ஒரு படியையாவது நிரப்புங்கள்) குறைந்தது ஒரு டீலரைத் தேர்வு செய்யுங்கள், அல்லது குறைந்தது ஒரு இலக்கை உள்ளிடுங்கள்.
- **"You've already submitted an allocation for that week."** (அந்த வாரத்துக்கு ஏற்கனவே திட்டம் அனுப்பிவிட்டீர்கள்) **Allocation Requests**-ல் திட்டத்தை மாற்றுமாறு உங்கள் MDO-விடம் கேளுங்கள்.
- **"Allocation submitted — awaiting MDO approval".** (திட்டம் அனுப்பப்பட்டது, MDO ஒப்புதலுக்காகக் காத்திருக்கிறது) திட்டம் அனுப்பப்பட்டுவிட்டது. MDO ஒப்புதல் தந்த பிறகு, டாஸ்க்குகள் **Today's Allocation**-ல் திறக்கும்.
- **"Allocation Rejected".** (திட்டம் நிராகரிக்கப்பட்டது) **Update & Resubmit** அழுத்தி, திட்டத்தைத் திருத்தி, மீண்டும் **Request** அழுத்துங்கள்.
:::

::: section mark-attendance
title: இன்றைய வருகையைப் பதிவு செய்வது
routes: /MarkAttendance
for: 
roles: 
designations: 
minutes: 2
source-hash: 8b14e6f020b3c82e
confidence: ok

**இந்தத் திரை எதற்கு**
எந்த டாஸ்க்கையும் தொடங்கும் முன், ஒவ்வொரு வேலை நாளிலும் உங்கள் வருகையைப் பதிவு செய்யுங்கள். நீங்கள் எங்கே இருக்கிறீர்கள் என்று இது பதிவு செய்வதால், உங்கள் போனில் உள்ள SPIC ONE ஆப்பில் மட்டுமே இது வேலை செய்யும்.

1. மெனுவில் இருந்து **Mark Attendance**-ஐத் திறங்கள், அல்லது **My Dashboard**-ல் **Mark Attendance** அழுத்துங்கள்.
2. உங்கள் இருப்பிடத்தைப் பயன்படுத்த ஆப்புக்கு அனுமதி கொடுங்கள். **Visit Area / Place** உங்கள் இருப்பிடத்தில் இருந்து தானாக நிரப்பப்படும். தேவைப்பட்டால் திருத்துங்கள், இருப்பிடம் கிடைக்கவில்லை என்றால் டைப் செய்யுங்கள்.
3. **Date**-ஐச் சரிபாருங்கள்.
4. **Attendance Status**-ஐத் தேர்வு செய்யுங்கள்: **Present**, **Half Day** அல்லது **On Leave**.
5. **Update Attendance** அழுத்துங்கள். "Attendance updated." என்று தெரியும், பிறகு **My Dashboard**-க்குத் திரும்புவீர்கள்.
6. அதே நாளில் பிறகு மாற்ற, **Attendance marked today**-க்கு அருகில் உள்ள **Edit** அழுத்துங்கள்.
7. வலது பக்கம், **Current Status** (**On Duty** அல்லது **Off Duty**) மற்றும் **Weekly Summary**-ஐப் பாருங்கள்: ஒவ்வொரு நாளின் வருகை, வேலை நேரம், தொடங்கிய டாஸ்க்குகள், பயணித்த தூரம்.

> Careful: காலை 10:00 மணிக்குப் பிறகு திரையில் "Submitted late for today" (இன்று தாமதமாகப் பதிவு) என்று காட்டும். அப்போதும் நீங்கள் சேமிக்கலாம்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Date** | இன்றைய தேதி. கட்டாயம். |
| **Visit Area / Place** | நீங்கள் வேலை செய்யும் கிராமம் அல்லது ஊர். இருப்பிடம் கிடைக்காதபோது கட்டாயம். |
| **Attendance Status** | **Present**, **Half Day** அல்லது **On Leave**. |

## பொதுவான பிரச்சனைகள்

- **"Attendance can only be marked from the SPIC mobile app".** (SPIC மொபைல் ஆப்பில் இருந்து மட்டுமே வருகை பதிவு செய்ய முடியும்) உங்கள் போனில் SPIC ONE-ஐத் திறங்கள்.
- **"Location could not be captured. Please type your Visit Area / Place to save attendance."** (இருப்பிடம் கிடைக்கவில்லை. வருகையைச் சேமிக்க இடத்தை டைப் செய்யுங்கள்) லொகேஷனை ஆன் செய்யுங்கள், அல்லது இடத்தை நீங்களே டைப் செய்யுங்கள்.
- **"Date is required."** (தேதி கட்டாயம்) **Update Attendance** அழுத்தும் முன் தேதியைத் தேர்வு செய்யுங்கள்.
:::

::: section todays-tasks
title: இன்றைய டாஸ்க்குகளைத் தொடங்குவது, சேமிப்பது, முடிப்பது
routes: /TaskDetail
for: 
roles: 
designations: 
minutes: 4
source-hash: 163e059a3e7fb61f
confidence: ok

**இந்தத் திரை எதற்கு**
ஒப்புதல் கிடைத்த ஒவ்வொரு டாஸ்க்கும் தனித் திரையில் திறக்கும். நீங்கள் அதைத் தொடங்கி, என்ன செய்தீர்கள் என்று பதிவு செய்து, முடிப்பீர்கள். டாஸ்க்கில் செலவிட்ட நேரத்தையும், எங்கே தொடங்கினீர்கள் என்பதையும் ஆப் பதிவு செய்யும்.

1. **Tasks Allocation**-ல், **Today's Allocation**-ஐத் திறங்கள். **Yet to Start**, **In Progress**, **Completed** அல்லது **All** தேர்வு செய்து, ஒரு டாஸ்க் கார்டில் **Start** (அல்லது **Continue**) அழுத்துங்கள்.
2. **Task Timeline** (**Assigned**, **Requested**, **Approved**, **Started**, **Closed**) மற்றும் டாஸ்க் விவரங்களைச் சரிபாருங்கள்.
3. **Start Task** அழுத்துங்கள். ஆப் உங்கள் இருப்பிடத்தைப் பதிவு செய்து **Time on task** கடிகாரத்தைத் தொடங்கும்.
4. **Close-out form**-ல் **Outcome**-ஐத் தேர்வு செய்யுங்கள். டீலர் என்றால், **+ Add another Product** அழுத்தி **Product Name** மற்றும் **Stock** சேருங்கள். SPCM என்றால், **Achieved Target** உள்ளிடுங்கள். PoS liquidation என்றால், **Urea**, **NPKS**, **DAP**, **Other** உள்ளிடுங்கள்.
5. **Remarks** எழுதி, **Photo Proof** (டீலர்) அல்லது **Supporting Document**-ஐ இணைத்து, **Save Record** அழுத்துங்கள். சேமித்த ரெக்கார்டுகள் வலது பக்கம் ஒரு அட்டவணையில் தெரியும்.
6. விசிட்டுக்குத் தேவைப்பட்டால், **Quick actions** பயன்படுத்துங்கள்: **Soil Sample Collection** அல்லது **Add Farmer Data**.
7. வேலை முடிந்ததும், **Stop Task** அழுத்தி, பிறகு **Close Task** அழுத்துங்கள்.

> Careful: முடிக்கப்பட்ட டாஸ்க்கை மீண்டும் தொடங்க முடியாது.
> Tip: வார இலக்கை எட்டியதும் SPCM மற்றும் PoS liquidation தானாகவே முடிந்துவிடும் ("Record saved - weekly target reached, task completed!").

## பொதுவான பிரச்சனைகள்

- **"Allocation Awaiting MDO Approval".** (திட்டம் MDO ஒப்புதலுக்காகக் காத்திருக்கிறது) MDO உங்கள் வாரத் திட்டத்துக்கு ஒப்புதல் தரும் வரை **Start Task** பட்டன் சாம்பல் நிறத்தில் இருக்கும்.
- **"Mark today's attendance before starting a task."** (டாஸ்க்கைத் தொடங்கும் முன் இன்றைய வருகையைப் பதிவு செய்யுங்கள்) ஆப் **Mark Attendance**-ஐத் திறக்கும். வருகையைப் பதிவு செய்து, பிறகு டாஸ்க்கைத் தொடங்குங்கள்.
- **"Please start the task before saving records."** (ரெக்கார்டுகளைச் சேமிக்கும் முன் டாஸ்க்கைத் தொடங்குங்கள்) முதலில் **Start Task** அழுத்துங்கள்.
- **Soil Sample கார்டு Sample Collection-ஐத் திறக்கிறது.** இது சாதாரணம்தான். மண் மாதிரிகள் sample collection திரைகளில் பதிவு செய்யப்படும்.
:::

::: section task-history
title: என் டாஸ்க் வரலாறு மற்றும் வாராந்திரச் சுருக்கத்தைப் பார்ப்பது
routes: /TasksAllocation
for: 
roles: 
designations: 
minutes: 2
source-hash: 5bb6d9cb3b6d9f29
confidence: ok

**இந்தத் திரை எதற்கு**
**Tasks Allocation**-ல் உள்ள **Task History** மற்றும் **Task Summary** டேப்கள், எல்லா வாரங்களின் உங்கள் டாஸ்க்குகளையும், ஒவ்வொரு வாரத்தின் இலக்கில் எவ்வளவு முடிந்தது என்பதையும் காட்டும்.

1. **Tasks Allocation**-ல், **Task History**-ஐத் திறங்கள். அது இன்றைய டாஸ்க்குகளுடன் திறக்கும்.
2. **From**, **To**, **Dealer**, **Status** தேர்வு செய்து, **Search** அழுத்துங்கள். **Today** அழுத்தினால் இன்றைக்குத் திரும்பும்.
3. ஒவ்வொரு கார்டிலும் டாஸ்க்கும் அதன் நிலையும் தெரியும். அதைத் திறக்க **View Details** அல்லது **Continue** அழுத்துங்கள்.
4. **Task Summary**-ஐத் திறங்கள். கார்டுகளில் **Total Assigned**, **Yet to Start**, **In Progress**, **Completed** தெரியும்.
5. ஒவ்வொரு வாரத்திலும் அதன் திட்ட நிலை, **Complete** சதவீதம், **Dealer Visits**, **MD Programs**, மற்றும் ஒவ்வொரு இலக்கின் **Target**, **Achieved**, **Pending** தெரியும்.
6. **Day-wise Dealer Visits**-ன் கீழ், ஒவ்வொரு நாளும் எத்தனை டீலர் விசிட்கள் முடிந்தன, எத்தனை நிலுவையில் உள்ளன என்று பாருங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No tasks match this filter".** (இந்த ஃபில்டருக்குப் பொருந்தும் டாஸ்க் இல்லை) தேதிகளை விரிவாக்குங்கள், அல்லது **Status**-ஐ **All** ஆக வையுங்கள்.
- **"Detailed view for this task type isn't built yet."** (இந்த டாஸ்க் வகைக்கு விவரத் திரை இன்னும் தயாராகவில்லை) Soil Sample டாஸ்க்குகளுக்கு இங்கே விவரத் திரை இல்லை.
:::

::: section my-reports
title: என் ரிப்போர்ட்கள்
routes: /FieldReports
for: 
roles: 
designations: 
minutes: 1
source-hash: 1d05a6505b4982e6
confidence: ok

**இந்தத் திரை எதற்கு**
இந்த மாதம் நீங்கள் எத்தனை டாஸ்க்குகளை முடித்தீர்கள், எவ்வளவு வேகமாக முடித்தீர்கள் என்று **My Reports** காட்டும்.

1. கார்டுகளைப் பாருங்கள்: **Completed**, **Avg. Close Time**, **Soil Samples**, **Farmers Added**.
2. **Task closures** அட்டவணையில் முடிந்த ஒவ்வொரு டாஸ்க்கும் **Date**, **Task**, **Type**, **Outcome** உடன் இருக்கும்.
3. ஒரு டாஸ்க்கைக் கண்டுபிடிக்க **Search...**-ல் டைப் செய்யுங்கள்.
4. பட்டியலைச் சேமிக்க அல்லது அச்சிட **Copy**, **CSV**, **Excel**, **PDF** அல்லது **Print** பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Nothing closed out this month yet".** (இந்த மாதம் இதுவரை எதுவும் முடிக்கப்படவில்லை) இந்த மாதம் நீங்கள் எந்த டாஸ்க்கையும் முடிக்கவில்லை.
:::

::: section track-playback
title: என் டிராக்கிங் வழியை மீண்டும் பார்ப்பது
routes: /TrackPlayback
for: 
roles: 
designations: 
minutes: 2
source-hash: cd8704c91e7b24e5
confidence: ok

**இந்தத் திரை எதற்கு**
கடந்த 7 நாட்களின் உங்கள் Live Tracking வழியை **Track Playback** வரைபடத்தில் காட்டும். உங்கள் போனில் உள்ள SPIC ONE ஆப்பில் மட்டுமே இது வேலை செய்யும்.

1. மெனுவில் இருந்து **Track Playback**-ஐத் திறங்கள், அல்லது **My Dashboard**-ல் **View Route**, அல்லது **Mark Attendance**-ல் **View Tracking Details** அழுத்துங்கள்.
2. டிராக்கிங் ஆன் ஆக இருந்தால், **Tracking Active** கார்டில் நேரம் தெரியும். நிறுத்த **Stop Tracking** அழுத்துங்கள், அல்லது வழி காட்டுதலைத் திறக்க **Directions to live location** அழுத்துங்கள்.
3. **Other playbacks available**-ன் கீழ், ஒரு நாளின் வழியைக் கொண்டுவர அந்த நாளைத் தட்டுங்கள்.
4. வரைபடத்தில் play அழுத்துங்கள், வழியில் நகர ஸ்லைடரை இழுங்கள், வேகத்தைத் தேர்வு செய்யுங்கள் (**1x**, **2x** அல்லது **4x**).

## பொதுவான பிரச்சனைகள்

- **"No tracking sessions in the last 7 days".** (கடந்த 7 நாட்களில் டிராக்கிங் இல்லை) வழியைப் பதிவு செய்ய **My Dashboard**-ல் இருந்து Live Tracking-ஐத் தொடங்குங்கள்.
- **"Track Playback can only be used from the SPIC mobile app".** (Track Playback-ஐ SPIC மொபைல் ஆப்பில் மட்டுமே பயன்படுத்த முடியும்) உங்கள் போனில் SPIC ONE-ஐத் திறங்கள்.
:::

::: section team-dashboard
title: டீம் டாஷ்போர்டு
routes: /MdoDashboard
for: 
roles: 
designations: 
minutes: 2
source-hash: f544adf24e2f0b9c
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரே HQ-வில் உள்ள JMDO-க்களின் டாஸ்க் வேலையின் மொத்தப் பார்வையை **Team Dashboard** MDO-க்குக் கொடுக்கும்.

1. கார்டுகளைப் பாருங்கள்: **Total Tasks**, **Unassigned**, **Requested by JMDO**, **Approved · Today**, **In Progress**, **Completed**.
2. **JMDO-wise summary**-ல், ஒவ்வொரு JMDO-வையும் **Active** அல்லது **On Leave** உடனும், **Assigned**, **In Progress**, **Completed** எண்ணிக்கைகளுடனும் பாருங்கள்.
3. வாரத் திட்டங்களுக்கு ஒப்புதல் தர **Allocation Requests** அழுத்துங்கள்.
4. சமீபத்திய எண்களைக் கொண்டுவர **Refresh** அழுத்துங்கள்.

> Tip: எந்த வாரத் திட்டமும் அனுப்பாத JMDO-க்களை **Unassigned** எண்ணும். இன்று டியூட்டியில் உள்ள JMDO-க்களை **In Progress** எண்ணும்.

## பொதுவான பிரச்சனைகள்

- **"No JMDOs found for your headquarters".** (உங்கள் HQ-வில் JMDO-க்கள் இல்லை) உங்கள் HQ-வே உள்ள JMDO யாரும் இல்லை. புரொஃபைல்களில் உள்ள HQ-வைச் சரிபார்க்குமாறு அட்மினிடம் கேளுங்கள்.
:::

::: section allocation-requests
title: ஒப்புதலுக்காகக் காத்திருக்கும் வாரத் திட்டங்களைப் பார்ப்பது
routes: /AllocationRequests
for: 
roles: 
designations: 
minutes: 2
source-hash: d233ce1bd4e689c1
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் JMDO-க்கள் ஒப்புதலுக்காக அனுப்பிய வாரத் திட்டங்களை **Allocation Requests** பட்டியலிடும். அட்மினுக்கு ஒப்புதல் கிடைத்த திட்டங்களும் ஒரு **Status** நெடுவரிசையும் தெரியும்.

1. மேலே உள்ள நீலப் பெட்டியைப் படியுங்கள். எத்தனை JMDO-க்கள் உங்கள் ஒப்புதலுக்காகக் காத்திருக்கிறார்கள் என்று அது சொல்லும்.
2. **Search...**, **All Districts**, **All Sub Districts**, **All JMDOs** மூலம் பட்டியலைக் குறையுங்கள்.
3. ஒவ்வொரு வரிசையிலும் **Submitted**, **Dealer Tasks**, **SPCM Target**, **Soil Target**, **MD Programs**, **Pos Liquidation** தெரியும்.
4. ஒரு திட்டத்தைத் திறக்க **Review** அழுத்துங்கள். ஒப்புதல் கிடைத்த திட்டத்துக்கு, அட்மினுக்கு அதற்குப் பதிலாக **Edit** தெரியும்.
5. பட்டியலைச் சேமிக்க அல்லது அச்சிட **Copy**, **CSV**, **Excel**, **PDF** அல்லது **Print** பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No pending allocation requests".** (நிலுவையில் உள்ள திட்டக் கோரிக்கைகள் இல்லை) இப்போது எந்த JMDO-வும் உங்களுக்காகக் காத்திருக்கவில்லை.
- **"No allocation requests match these filters".** (இந்த ஃபில்டர்களுக்குப் பொருந்தும் கோரிக்கை இல்லை) ஃபில்டர்களை மீண்டும் **All Districts**, **All Sub Districts**, **All JMDOs** ஆக வையுங்கள்.
:::

::: section review-allocation
title: வாரத் திட்டத்தைப் பரிசீலித்து ஒப்புதல் தருவது
routes: /AllocationRequestReview
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-ar-dealers] | 2 > [data-help=ff-ar-add] | 3 > [data-help=ff-ar-targets] | 4 > [data-help=ff-ar-programs] | 5 > [data-help=ff-ar-approve]
minutes: 3
source-hash: 791e48a894306411
confidence: ok

**இந்தத் திரை எதற்கு**
**Allocation Requests**-ல் இருந்து **Review** மூலம் ஒரு திட்டத்தைத் திறங்கள். ஒப்புதல் தரும் முன் அதைத் திருத்தலாம். நீங்கள் ஒப்புதல் தந்தவுடன் JMDO டாஸ்க்குகளைத் தொடங்கலாம்.

1. **Dealer Tasks**-ல், ஒவ்வொரு டீலரையும் சரிபாருங்கள். **Date** (அது திட்டமிட்ட வாரத்துக்குள் இருக்க வேண்டும்) அல்லது **Priority** (**Normal** அல்லது **High Priority**)-ஐ மாற்றுங்கள். குப்பைத்தொட்டி ஐகான் அழுத்தி, பிறகு **Remove** அழுத்தினால் டாஸ்க் நீங்கும்.
2. டீலர்களைச் சேர்க்க, **Add dealer task(s)**-ன் கீழ் அவர்களைத் தேர்வு செய்து, **Date** மற்றும் **Priority** தேர்வு செய்து, **Add** அழுத்துங்கள்.
3. **SPCM Target**, **Soil Sample Target**, **PoS Liquidation Targets** (**Urea**, **DAP**, **NPKS**, **Other**) ஆகியவற்றைச் சரிபாருங்கள். பெட்டியை விட்டு வெளியே வந்ததும் மாற்றம் சேமிக்கப்படும்.
4. **MD Programs** சரிபார்ப்பதற்காக மட்டுமே காட்டப்படும். அவை CSR1-ல் இருந்து வருகின்றன, இங்கே மாற்ற முடியாது.
5. கீழே உள்ள சுருக்க வரியைப் படித்து, **Approve Allocation** அழுத்தி, பிறகு **Yes, Approve** அழுத்துங்கள்.

> Careful: டீலர் டாஸ்க்கை நீக்கினால் அதைத் திரும்பப் பெற முடியாது.

## பொதுவான பிரச்சனைகள்

- **"The date must fall within the JMDO's allocated week"** (தேதி JMDO-வின் திட்ட வாரத்துக்குள் இருக்க வேண்டும்) **Dealer Tasks**-ன் கீழ் காட்டப்படும் வாரத்துக்குள் ஒரு தேதியைத் தேர்வு செய்யுங்கள்.
- **"Already assigned on that date"** (அந்தத் தேதியில் ஏற்கனவே ஒதுக்கப்பட்டுள்ளது) அந்த டீலர் அந்த நாளுக்கு ஏற்கனவே திட்டமிடப்பட்டுள்ளார்.
- **"Couldn't load this allocation."** (இந்தத் திட்டத்தைத் திறக்க முடியவில்லை) **Back to all requests** மூலம் திரும்பிப் போய், திட்டத்தை மீண்டும் திறங்கள்.
:::

::: section task-monitoring
title: என் டீமின் எல்லா டாஸ்க்குகளையும் பின்தொடர்வது
routes: /TaskMonitoring
for: 
roles: 
designations: 
tour: 1 > [data-help=ff-tm-filters] | 2 > [data-help=ff-tm-apply] | 3 > [data-help=ff-tm-search] | 4 > [data-help=ff-tm-refresh]
minutes: 2
source-hash: 0c1b0fa3ecdc07f2
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் JMDO-க்களின் எல்லா டாஸ்க்குகளையும் **Task Monitoring** பட்டியலிடும்: யாரிடம் உள்ளது, அதன் வகை, எங்கே, எப்போது முடிக்க வேண்டும், அதன் நிலை. இது இன்று முடிக்க வேண்டிய டாஸ்க்குகளுடன் திறக்கும்.

1. **Filters**-ன் கீழ், ஒன்று அல்லது அதற்கு மேற்பட்ட **JMDO**, **Task Type**, **Status**, **State** அல்லது **Region** தேர்வு செய்து, **Date Range**-ஐ வையுங்கள்.
2. பொருந்தும் டாஸ்க்குகளைக் காட்ட **Apply Filters** அழுத்துங்கள். **Clear All** தேதிகள் உட்பட எல்லா ஃபில்டர்களையும் நீக்கும்.
3. **Tasks** அட்டவணையில், பட்டியலைக் குறைக்க **Search...**-ல் டைப் செய்யுங்கள். **Requested** டாஸ்க்கில் ஒரு கண் ஐகான் இருக்கும், அது ஒப்புதலுக்காக அதன் வாரத் திட்டத்தைத் திறக்கும்.
4. சமீபத்திய நிலையைக் கொண்டுவர **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No tasks match these filters".** (இந்த ஃபில்டர்களுக்குப் பொருந்தும் டாஸ்க் இல்லை) எல்லாத் தேதிகளின் டாஸ்க்குகளையும் பார்க்க **Clear All** அழுத்துங்கள்.
- **"No tasks yet".** (இதுவரை டாஸ்க்குகள் இல்லை) உங்கள் HQ-வில் எந்த JMDO-வும் வாரத் திட்டம் அனுப்பவில்லை.
:::

::: section team-tracking
title: என் டீம் எங்கே இருக்கிறது என்று பார்ப்பது
routes: /TeamTracking
for: 
roles: 
designations: 
minutes: 1
source-hash: 158915a8c6a5812a
confidence: ok

**இந்தத் திரை எதற்கு**
உங்கள் HQ-வில் உள்ள JMDO-க்களின் Live Tracking நிலையை **Team Tracking** காட்டும்.

1. **JMDO tracking status** அட்டவணையில், ஒவ்வொரு JMDO-வின் **Status**-ஐப் பாருங்கள்: **Live**, **Stale**, **Offline**, **Stopped**, **Ended automatically** அல்லது **Never started**.
2. கடைசியாக வந்த இருப்பிடத்தின் நேரத்தையும் இடத்தையும் **Last Seen** மற்றும் **Last Location** காட்டும்.
3. JMDO எப்போது டிராக்கிங்கைத் தொடங்கினார் என்று **Session Started** காட்டும்.
4. ஒரு JMDO-வைக் கண்டுபிடிக்க **Search...**-ல் டைப் செய்யுங்கள், சமீபத்திய நிலைக்கு **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **ஒரு JMDO-வுக்கு "Never started" என்று காட்டுகிறது.** அந்த JMDO ஆப்பில் Live Tracking-ஐத் தொடங்கவில்லை.
- **Stale அல்லது Offline.** போனிலிருந்து கொஞ்ச நேரமாக இருப்பிடம் வரவில்லை. JMDO-வுக்கு போன் செய்யுங்கள்.
:::

::: section yearly-targets
title: JMDO-க்களுக்கு ஆண்டு இலக்குகளை வைப்பது
routes: /TargetAchievement
for: 
roles: 
designations: 
minutes: 2
source-hash: e1cdc1ea7b68c98e
confidence: ok

**இந்தத் திரை எதற்கு**
**Yearly Target Allocation**-ல், நிதியாண்டுக்கான (ஏப்ரல் முதல் மார்ச் வரை) ஒவ்வொரு JMDO-வின் SPCM மற்றும் Soil Sample இலக்கை MDO வைப்பார்.

1. **Allocate target to a JMDO**-ன் கீழ், **JMDO**-வைத் தேர்வு செய்யுங்கள். ஏற்கனவே சேமித்த இலக்கு தானாக நிரப்பப்படும்.
2. **SPCM Target (yearly)** மற்றும் **Soil Sample Target (yearly)** உள்ளிடுங்கள்.
3. **Save Target Allocation** அழுத்துங்கள்.
4. **Current yearly targets** அட்டவணையில் எல்லா JMDO-க்களின் இலக்கும் தெரியும். ஒரு வரிசையைப் படிவத்தில் கொண்டுவந்து மாற்ற பென்சில் ஐகானை அழுத்துங்கள்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **JMDO** | களப் பணியாளர். கட்டாயம். |
| **SPCM Target (yearly)** | முழு எண், பூஜ்ஜியம் அல்லது அதற்கு மேல். கட்டாயம். |
| **Soil Sample Target (yearly)** | முழு எண், பூஜ்ஜியம் அல்லது அதற்கு மேல். கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"Enter valid, non-negative SPCM and Soil Sample targets."** (சரியான, மைனஸ் இல்லாத SPCM மற்றும் Soil Sample இலக்குகளை உள்ளிடுங்கள்) பூஜ்ஜியம் அல்லது அதற்கு மேலான முழு எண்களைப் பயன்படுத்துங்கள்.
- **Save பட்டன் சாம்பல் நிறத்தில் உள்ளது.** ஒரு JMDO-வைத் தேர்வு செய்து, இரண்டு இலக்குகளையும் நிரப்புங்கள்.
:::

::: section team-reports
title: டீம் ரிப்போர்ட்கள்
routes: /MdoReports
for: 
roles: 
designations: 
minutes: 2
source-hash: 4f488c0a407fcae5
confidence: ok

**இந்தத் திரை எதற்கு**
இந்த மாதம் உங்கள் JMDO-க்கள் எப்படி வேலை செய்கிறார்கள் என்று **Reports** காட்டும்: முடிந்த டாஸ்க்குகள், எடுத்த நேரம், மண் மாதிரிகள், விவசாயிகள்.

1. **Report Filters**-ன் கீழ், **Report Type**, **JMDO**, **On or after** (தொடக்கத் தேதி) தேர்வு செய்யுங்கள்.
2. **Generate Report** அழுத்துங்கள்.
3. கார்டுகளைப் பாருங்கள்: **Tasks Closed**, **Avg. Turnaround**, **Soil Samples**, **Farmers Covered**.
4. **JMDO-wise summary**-ல் ஒவ்வொரு JMDO-வுக்கும் **Tasks Closed**, **On-time %**, **Soil Samples**, **Farmers Added** தெரியும்.
5. **Detailed Task Report**-ல் ஒவ்வொரு டாஸ்க்கும் **Resolution**, **Status**, **Hours Taken** உடன் தெரியும்.
6. ஒரு அட்டவணையில் **Copy**, **CSV**, **Excel**, **PDF** அல்லது **Print** பயன்படுத்துங்கள், மீண்டும் கொண்டுவர **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Sales Report-ல் எதுவும் இல்லை.** விற்பனைத் தரவு இன்னும் டாஸ்க் அமைப்பில் சேர்க்கப்படவில்லை.
- **Farmer Coverage-க்கு விவர அட்டவணை இல்லை.** விவசாயிகளின் எண்ணிக்கை கார்டுகளிலும் சுருக்கத்திலும் மட்டுமே காட்டப்படும்.
:::

::: section performance-tracker
title: JMDO-க்களின் செயல்திறனை ஒப்பிடுவது
routes: /PerformanceTracker
for: 
roles: 
designations: 
minutes: 1
source-hash: 203ffe19ee1cb6d6
confidence: ok

**இந்தத் திரை எதற்கு**
**Performance Tracker** JMDO-க்களைச் சிறப்பாகச் செய்பவர் முதல் குறைவாகச் செய்பவர் வரை வரிசைப்படுத்தும். இப்போதைக்கு இது மாதிரி எண்களைக் காட்டுகிறது, உண்மையான தரவு அல்ல.

1. **Filters**-ஐத் திறந்து ஒரு **JMDO**-வைத் தேர்வு செய்யுங்கள்.
2. **Apply Filters** அழுத்துங்கள். **Clear All** மீண்டும் எல்லா JMDO-க்களையும் காட்டும்.
3. ஒவ்வொரு JMDO-வுக்கும் **Rank**, **SPCM Achv.**, **Soil Achv.**, **Attendance** படியுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Detailed performance view ... isn't built yet."** (விரிவான செயல்திறன் பார்வை இன்னும் தயாராகவில்லை) கண் ஐகானுக்கு இன்னும் விவரப் பக்கம் இல்லை.
:::

::: section farmer-data
title: விவசாயிகள் தரவு
routes: /FarmerData
for: 
roles: 
designations: 
minutes: 1
source-hash: 78a4629a8079544f
confidence: ok

**இந்தத் திரை எதற்கு**
**Farmer Data** என்பது உங்கள் பகுதியில் உள்ள விவசாயிகளின் பட்டியல், புதியவரைச் சேர்க்க ஒரு படிவமும் உண்டு. இப்போதைக்கு இந்தத் திரை ஒரு முன்னோட்டம் மட்டுமே: புதிய பதிவுகள் சர்வரில் சேமிக்கப்படாது.

1. விவசாயிகளை **Contact**, **Location**, **Crop(s)**, **Acres** உடன் பாருங்கள்.
2. **Add Farmer Entry**-ன் கீழ், **MO H.Q.**, **Month**, **Programme** தேர்வு செய்யுங்கள்.
3. **Farmer Name**, **Contact Number**, **Location**, **Contact Address**, **Acres**, **Crop 1**, **Crop 2** உள்ளிடுங்கள்.
4. **Save farmer entry** அழுத்துங்கள். ஒரு வரிசையில் உள்ள பென்சில் ஐகான் அதைப் படிவத்தில் நகலெடுக்கும்.

> Tip: மண் மாதிரிக்காக ஒரு விவசாயியைப் பதிவு செய்ய, டாஸ்க்கில் உள்ள **Add Farmer Data** பயன்படுத்துங்கள். அது sample collection திரையைத் திறக்கும்.

## பொதுவான பிரச்சனைகள்

- **Save பட்டன் சாம்பல் நிறத்தில் உள்ளது.** **Farmer Name** கட்டாயம்.
- **பக்கத்தை விட்டு வெளியே போனதும் என் பதிவு காணாமல் போனது.** இந்தத் திரை இன்னும் சர்வரில் சேமிப்பதில்லை.
:::

::: section farmers-hub
title: விவசாயிகள் மையம்
routes: /Farmers
for: 
roles: 
designations: 
minutes: 1
source-hash: 6be3ac6e9f35012a
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரு விவசாயியின் முன் களப் பணியாளர் பயன்படுத்தும் கருவிகளை **Farmers** ஒன்றாகத் தரும். **Farmer Portal is on its way** (விவசாயிகள் போர்ட்டல் விரைவில் வருகிறது) என்று ஒரு அறிவிப்பு இருக்கும்.

1. வரவிருக்கும் Farmer Portal பற்றிய அறிவிப்பைப் படியுங்கள்.
2. **Help a farmer now**-ன் கீழ், products, அளவு (dosage), பயன்பாடு பற்றிய பதில்களுக்கு **Ask SPIC AI** தட்டுங்கள்.
3. விவசாயிகள் மற்றும் SPIC நிபுணர்களின் கேள்வி பதில்களுக்கு **Knowledge Community** தட்டுங்கள்.
4. product விவரங்களைக் காட்ட **Product Information**, **Product Brochures** அல்லது **AI Videos** தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **Knowledge Community இல்லை.** உங்கள் பணிப்பொறுப்புக்கு community அணுகல் இல்லை.
:::

::: section rmd-dashboard
title: RMD டாஷ்போர்டு
routes: /RMDDashboard
for: 
roles: 
designations: 
minutes: 1
source-hash: 4698a8dc0e852e00
confidence: ok

**இந்தத் திரை எதற்கு**
RMD டாஷ்போர்டு மண்டலத்தின் CSR மற்றும் பட்ஜெட் பற்றிய மொத்தப் பார்வையைக் காட்டும். இப்போதைக்கு இதன் எண்கள் மாதிரிகள் மட்டுமே, உண்மையான தரவு அல்ல. இதன் பட்டன்கள் எதையும் சேமிக்காது.

1. மேலே **Budget Overview** மற்றும் **CSR Workflow Summary**-ஐப் பாருங்கள்.
2. **RM Approval Status**, **Budget Utilization** போன்ற வரைபடங்களைப் பாருங்கள்.
3. **Quick Actions**, **Recent Programs & Reports**, **Recent Activity**, **System Updates** ஆகியவற்றைப் பாருங்கள்.

## பொதுவான பிரச்சனைகள்

- **எண்கள் என் மண்டலத்துடன் பொருந்தவில்லை.** இப்போதைக்கு இந்தத் திரையில் உள்ள எண்கள் மாதிரிகள் மட்டுமே.
:::
