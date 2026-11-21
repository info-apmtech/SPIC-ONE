---
module: budget-csr
title: பட்ஜெட் மற்றும் CSR
icon: bi-wallet2
order: 110
language: ta
category: சந்தைப்படுத்தல்
note: புதிய வழிகாட்டி
meta-source-hash: ed22bedc41464619
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 6fab7f9ab7aa88c7
confidence: ok

வருடாந்திர மார்க்கெட்டிங் பட்ஜெட்டைத் திட்டமிடுங்கள், அதை மாநிலங்கள், மண்டலங்கள், HQ-களுக்குப் பகிர்ந்து கொடுங்கள், நிகழ்ச்சிகள் (program) மூலம் செலவு செய்யுங்கள். ஒவ்வொரு நிகழ்ச்சிக்கும் இரண்டு படிவங்கள் உண்டு: நிகழ்ச்சிக்கு முன் CSR-1 (திட்டம்), நிகழ்ச்சிக்குப் பின் CSR-2 (அறிக்கை). ஒவ்வொரு படிவமும் சரிபார்க்கப்பட்டு ஒப்புதல் பெறும்.

- plan | icon: bi-pie-chart | color: #0891b2
  title: பட்ஜெட்டைத் திட்டமிடுங்கள்
  text: தலைமை அலுவலகம் வருடாந்திர பட்ஜெட்டை நிர்ணயிக்கும். அது மாநிலங்கள், மண்டலங்கள், HQ-களுக்குப் பகிரப்பட்டு, பிறகு மாதம் மாதமாக நிகழ்ச்சிகளுக்குப் பிரிக்கப்படும்.
- csr1 | icon: bi-file-earmark-text | color: #d97706
  title: CSR-1 என்பது திட்டம்
  text: நிகழ்ச்சிக்கு முன், தேதி, இடம், முக்கியப் பயிர்கள், திட்டமிட்ட செலவுகளுடன் CSR-1 போடுங்கள்.
- csr2 | icon: bi-camera | color: #16a34a
  title: CSR-2 என்பது அறிக்கை
  text: நிகழ்ச்சி முடிந்ததும், புகைப்படங்கள், பங்கேற்றவர்கள், உண்மையான செலவுகள், ஆதாரங்களுடன் CSR-2 அறிக்கை அனுப்புங்கள்.
:::

::: section flow
title: பட்ஜெட்டும் நிகழ்ச்சியும் எப்படி நகர்கின்றன, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: a83847a70449e86b
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- office | type: lane | color: #0891b2 | roles: Admin, CorporateAdmin
  title: தலைமை அலுவலகம்
  sub: Admin
- field | type: lane | color: #d97706 | roles: MDO, JMDO, MO, RMD, SMD
  title: களப் பணியாளர்கள்
  sub: MDO · MO · RMDO · SMDO
- approvers | type: lane | color: #0d9488 | roles: RM, SMM
  title: ஒப்புதல் தருபவர்கள்
  sub: RM · SMM
- md | type: lane | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: மூத்த ஒப்புதல் தருபவர்கள்
  sub: MD Admin · MD HOD · AVP
- budget | lane: office | col: 1 | icon: bi-wallet2 | color: #0891b2 | next: plan | screen: state-allocation
  title: பட்ஜெட்டை நிர்ணயித்துப் பகிர்தல்
  status: Active
  text: Admin வருடாந்திர பட்ஜெட்டை உள்ளிட்டு மாநிலங்களுக்குப் பகிர்வார். SMD அல்லது SMM மாநில பட்ஜெட்டை மண்டலங்களுக்குப் பகிர்வார்கள், RM அல்லது RMDO மண்டல பட்ஜெட்டை HQ-களுக்குப் பகிர்வார்கள்.
- plan | lane: field | col: 2 | icon: bi-calendar3 | color: #64748b | next: plan-ok | screen: program-budget
  title: நிகழ்ச்சி பட்ஜெட்டைத் திட்டமிடுதல்
  status: Submitted
  text: MO, RMDO, SMDO தங்கள் பட்ஜெட்டை நிகழ்ச்சிகளுக்கும் மாதங்களுக்கும் பிரித்து, **Submit** அழுத்துவார்கள்.
- plan-ok | lane: approvers | col: 3 | icon: bi-check2-square | color: #2563eb | next: csr1 | screen: program-budget
  title: திட்டத்தைச் சரிபார்த்து ஒப்புதல் தருதல்
  status: Approved
  text: RMDO அல்லது SMDO நிகழ்ச்சி பட்ஜெட்டைச் சரிபார்ப்பார். பிறகு RM அல்லது SMM ஒப்புதல் தருவார்.
- csr1 | lane: field | col: 4 | icon: bi-file-earmark-text | color: #d97706 | next: csr1-ok | screen: create-csr1
  title: CSR-1 போடுதல்
  status: Submitted
  text: ஒப்புதல் பெற்ற நிகழ்ச்சிக்கு, தேதி, HQ, இடம், பொறுப்பாளர், முக்கியப் பயிர்கள், செலவுகளுடன் CSR-1 படிவத்தை நிரப்பி, **Submit for Validation** அழுத்துங்கள்.
- csr1-ok | lane: approvers | col: 5 | icon: bi-patch-check | color: #2563eb | next: csr2 | screen: csr1-list
  title: CSR-1-ஐச் சரிபார்த்து ஒப்புதல் தருதல்
  status: Approved
  text: HQ திட்டங்களை MO சரிபார்ப்பார், மாநிலத் திட்டங்களை SMD சரிபார்ப்பார். பிறகு RM அல்லது SMM ஒப்புதல் தருவார். CSR-1-க்கு ஒரு sanction எண் கிடைக்கும், அது CSR-2-க்குத் தகுதி பெறும்.
- csr2 | lane: field | col: 6 | icon: bi-camera | color: #16a34a | next: csr2-ok | screen: csr2-list
  title: நிகழ்ச்சியை நடத்தி CSR-2 அறிக்கை அனுப்புதல்
  status: Submitted
  text: பொறுப்பாளர் நிகழ்ச்சியை நடத்தி, தேதி, புகைப்படங்கள், பங்கேற்றவர்கள், உண்மையான செலவுகள், ஆதாரங்களைப் பதிவு செய்வார்.
- csr2-ok | lane: approvers | col: 7 | icon: bi-check2-all | color: #16a34a | next: final | screen: csr2-list
  title: CSR-2-ஐச் சரிபார்த்து ஒப்புதல் தருதல்
  status: Approved
  text: MO அல்லது SMD அறிக்கையைச் சரிபார்ப்பார். பிறகு RM அல்லது SMM ஒப்புதல் தருவார்.
- final | lane: office | col: 8 | icon: bi-clipboard-data | color: #0891b2 | screen: final-report
  title: இறுதி அறிக்கை
  text: தலைமை அலுவலகம் ஒவ்வொரு நிகழ்ச்சியையும் அதன் CSR-1 நிலை, CSR-2 நிலை, செலவுகள், மீதமுள்ள பட்ஜெட்டுடன் பார்க்கும்.
- late | lane: md | col: 6 | icon: bi-clock-history | color: #dc2626 | kind: exception | from: csr1 | to: csr1-ok | screen: csr1-list
  title: தாமதமான படிவங்களுக்கு மூன்று ஒப்புதல்கள்
  status: Level1Approved
  text: கடைசி நாளுக்கு (cut-off) பிறகு அனுப்பிய CSR-1 அல்லது CSR-2-க்கு மூன்று ஒப்புதல்கள் வேண்டும்: முதலில் RM அல்லது SMM, பிறகு MD Admin, பிறகு MD HOD.
- shortage | lane: md | col: 7 | icon: bi-arrow-left-right | color: #dc2626 | kind: exception | from: csr2-ok | to: csr2-ok | screen: reallocation
  title: பட்ஜெட் போதவில்லை
  status: PendingMDHOD
  text: பட்ஜெட் குறைவாக இருப்பதால் CSR-2-க்கு ஒப்புதல் தர முடியவில்லை என்றால், RM, SMM அல்லது Admin பட்ஜெட் மறுஒதுக்கீடு (reallocation) கேட்பார்கள். MD HOD இறுதி ஒப்புதல் தந்த பிறகு, ஒப்புதலை மீண்டும் முயற்சிக்கலாம்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 8d3c0359eda444f2
confidence: ok

- active | color: #16a34a
  label: Active
  text: இந்த வருடாந்திர பட்ஜெட் பதிவு பயன்பாட்டில் உள்ளது.
- inactive | color: #64748b
  label: Inactive
  text: இந்த வருடாந்திர பட்ஜெட் பதிவு நிறுத்தப்பட்டுள்ளது. அதை மீண்டும் இயக்கலாம்.
- draft | color: #64748b
  label: Draft
  text: சேமிக்கப்பட்டது, ஆனால் அனுப்பப்படவில்லை. இன்னும் மாற்றலாம்.
- submitted | color: #d97706
  label: Submitted
  text: அனுப்பப்பட்டது, சரிபார்ப்புக்காகக் காத்திருக்கிறது.
- pending | color: #d97706
  label: Pending
  text: அடுத்த சரிபார்ப்புக்காகக் காத்திருக்கிறது.
- validated | color: #2563eb
  label: Validated
  text: சரிபார்ப்பவர் சரிபார்த்துவிட்டார். ஒப்புதலுக்காகக் காத்திருக்கிறது.
- level1 | color: #7c3aed
  label: Level1Approved
  text: தாமதமான படிவம் முதல் ஒப்புதலைப் பெற்றது. MD Admin-க்காகக் காத்திருக்கிறது.
- level2 | color: #7c3aed
  label: Level2Approved
  text: தாமதமான படிவம் இரண்டாவது ஒப்புதலைப் பெற்றது. MD HOD-க்காகக் காத்திருக்கிறது.
- approved | color: #16a34a
  label: Approved
  text: முழுமையாக ஒப்புதல் கிடைத்தது. ஒப்புதல் பெற்ற CSR-1 அடுத்து CSR-2-க்குப் போகலாம்.
- rejected | color: #dc2626
  label: Rejected
  text: நிராகரிக்கப்பட்டது. நிராகரிப்புக் காரணத்தைப் படியுங்கள்.
- pending-smm | color: #d97706
  label: PendingSMM
  text: பட்ஜெட் மறுஒதுக்கீடு SMM-க்காகக் காத்திருக்கிறது.
- pending-avp | color: #d97706
  label: PendingAVP
  text: பட்ஜெட் மறுஒதுக்கீடு AVP-க்காகக் காத்திருக்கிறது.
- pending-mdhod | color: #d97706
  label: PendingMDHOD
  text: பட்ஜெட் மறுஒதுக்கீடு கடைசி ஒப்புதல் தருபவரான MD HOD-க்காகக் காத்திருக்கிறது.
- healthy | color: #16a34a
  label: Healthy
  text: HQ-விடம் போதுமான பட்ஜெட் மீதம் உள்ளது.
- near-limit | color: #d97706
  label: Near Limit
  text: HQ தன் பட்ஜெட்டின் பெரும்பகுதியைப் பயன்படுத்திவிட்டது.
- exceeded | color: #dc2626
  label: Exceeded
  text: HQ தன் பட்ஜெட்டை விட அதிகமாகச் செலவு செய்துவிட்டது.
- ext-pending | color: #d97706
  label: Pending Approval
  text: நீட்டிப்புக் கோரிக்கை முடிவுக்காகக் காத்திருக்கிறது.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 5e86c95e22cb73c4
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- admin | color: #0891b2 | roles: Admin, CorporateAdmin
  title: Admin
  - ஒவ்வொரு நிதியாண்டுக்கும் வருடாந்திர பட்ஜெட்டை உள்ளிடுவது
  - வருடாந்திர பட்ஜெட்டை மாநிலங்களுக்குப் பகிர்வது
  - நிகழ்ச்சிப் பட்டியல், sanction எண்கள், RMD HQ இணைப்பைப் பராமரிப்பது
  - மாநிலங்களுக்கு இடையே பட்ஜெட் மறுஒதுக்கீடு கேட்பது
- smm | color: #0d9488 | roles: SMM
  title: SMM
  - மாநில பட்ஜெட்டை மண்டலங்களுக்குப் பகிர்வது
  - நிகழ்ச்சி பட்ஜெட்டுகள், மாநில CSR-1, மாநில CSR-2-க்கு ஒப்புதல் தருவது
  - மண்டலங்களுக்கு இடையே பட்ஜெட் மறுஒதுக்கீடு கேட்பது
  - RM-இடமிருந்து வரும் மறுஒதுக்கீடு கோரிக்கைகளுக்கு ஒப்புதல் தருவது
- smd | color: #ea580c | roles: SMD
  title: SMDO
  - நிகழ்ச்சி பட்ஜெட்டுகளைத் திட்டமிட்டுச் சரிபார்ப்பது
  - மாநில பட்ஜெட்டை மண்டலங்களுக்குப் பகிர்வது
  - மாநில நிகழ்ச்சிகளுக்கு CSR-1 போடுவது
  - மாநில CSR-1, CSR-2-ஐச் சரிபார்ப்பது
- rm | color: #2563eb | roles: RM
  title: RM
  - மண்டல பட்ஜெட்டை HQ-களுக்குப் பகிர்வது
  - நிகழ்ச்சி பட்ஜெட்டுகள், HQ CSR-1, HQ CSR-2-க்கு ஒப்புதல் தருவது
  - HQ-களுக்கு இடையே பட்ஜெட் மறுஒதுக்கீடு கேட்பது
- rmd | color: #9333ea | roles: RMD
  title: RMDO
  - நிகழ்ச்சி பட்ஜெட்டுகளைத் திட்டமிட்டுச் சரிபார்ப்பது
  - மண்டல பட்ஜெட்டை HQ-களுக்குப் பகிர்வது
  - HQ நிகழ்ச்சிகளுக்கு CSR-1 போடுவது
- mo | color: #d97706 | roles: MO
  title: MO
  - நிகழ்ச்சி பட்ஜெட்டுகளைத் திட்டமிடுவது
  - CSR-1 போடுவது
  - HQ CSR-1, CSR-2-ஐச் சரிபார்ப்பது
- mdo | color: #65a30d | roles: MDO, JMDO
  title: MDO மற்றும் JMDO
  - HQ நிகழ்ச்சிகளுக்கு CSR-1 போடுவது
  - நீங்கள் பொறுப்பாளராக இருந்தால், நிகழ்ச்சியை நடத்தி CSR-2 அறிக்கை அனுப்புவது
- senior | color: #7c3aed | roles: MDAdmin, MDHOD, AVP
  title: MD Admin, MD HOD மற்றும் AVP
  - தாமதமான படிவங்களுக்கு MD Admin இரண்டாவது ஒப்புதலும், MD HOD மூன்றாவது ஒப்புதலும் தருவார்கள்
  - பட்ஜெட் மறுஒதுக்கீடுகளுக்கு AVP மற்றும் MD HOD ஒப்புதல் தருவார்கள்
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 471a0e1b0196bf4a
confidence: ok

- csr2-grey | popular: true
  q: CSR-2 பட்டன் சாம்பல் நிறத்தில் உள்ளது
  a: CSR-1 **Approved** ஆன பிறகுதான் CSR-2 திறக்கும். அப்போது **CSR-2 Eligibility** நெடுவரிசையில் **Eligible for CSR-2** என்று காட்டும், பட்டனும் வேலை செய்யும்.
- one-month | popular: true
  q: CSR-1 Create-ல் ஒரு மாதத்துக்கான திட்டங்கள் மட்டுமே தெரிகின்றன
  a: இது சாதாரணம்தான். எந்த மாதம் என்று நீலப் பெட்டி சொல்லும், உதாரணமாக "Showing plans for December 2026 only". CSR-1 கடைசி நாள் (cut-off) வரை அடுத்த மாதத்துக்குத் திட்டமிடலாம்; அதற்குப் பிறகு, அதற்கடுத்த மாதத்துக்கு.
- must-equal
  q: மொத்தம் ஒதுக்கப்பட்ட பட்ஜெட்டுக்குச் சமமாக இருக்க வேண்டும் என்று Submit சொல்கிறது
  a: **Budgeting Management**-ல் உங்கள் முழு பட்ஜெட்டையும் நிகழ்ச்சிகளுக்குப் பிரித்துக் கொடுக்க வேண்டும். இன்னும் எவ்வளவு ஒதுக்க வேண்டும் என்று செய்தி காட்டும். மொத்தம் சமமாகும் வரை நிகழ்ச்சி எண்ணிக்கைகளைச் சேருங்கள்.
- no-money
  q: பட்ஜெட் குறைவாக இருப்பதால் CSR-2-க்கு ஒப்புதல் தர முடியவில்லை
  a: வேறு மாநிலம், மண்டலம் அல்லது HQ-விலிருந்து பணத்தை மாற்ற, RM, SMM அல்லது Admin **Budget Reallocations**-ல் கோரிக்கை போடுவார்கள். MD HOD ஒப்புதல் தந்த பிறகு, ஒப்புதலை மீண்டும் முயற்சியுங்கள்.
- three-levels
  q: என் CSR-1 அல்லது CSR-2-க்கு ஏன் மூன்று ஒப்புதல்கள் வேண்டும்?
  a: அது கடைசி நாளுக்கு (cut-off) பிறகு அனுப்பப்பட்டது. தாமதமான படிவங்கள் முதலில் RM அல்லது SMM-க்கு, பிறகு MD Admin-க்கு, பிறகு MD HOD-க்குப் போகும்.
- save-state-first
  q: "Save this state's allocation above before distributing it to regions." என்று வருகிறது
  a: அந்த மாநிலத்துக்கு இன்னும் பட்ஜெட் இல்லை. அந்த நிதியாண்டுக்கான மாநில ஒதுக்கீட்டை Admin முதலில் சேமிக்க வேண்டும்.
:::

::: section annual-budget
title: வருடாந்திர பட்ஜெட்டை உள்ளிடுவது
routes: /AnnualBudgeting
for: 
roles: 
designations: 
minutes: 2
source-hash: 97c15e0498b487cc
confidence: ok

**இந்தத் திரை எதற்கு**
ஒவ்வொரு நிதியாண்டுக்குமான மொத்த பட்ஜெட்டை உள்ளிட Admin **Annual Budgeting** திரையைப் பயன்படுத்துவார். மற்ற எல்லா பட்ஜெட் திரைகளும் இந்தத் தொகையிலிருந்துதான் தொடங்கும்.

1. **Add Annual Budgeting**-ன் கீழ், **Financial Year (FY)**-ஐத் தேர்வு செய்யுங்கள்.
2. **Amount**-ஐ உள்ளிடுங்கள்.
3. சுவிட்சை **Active**-லேயே விடுங்கள், அல்லது பதிவை **Inactive** ஆக்க அதைத் தட்டுங்கள்.
4. **Save** அழுத்துங்கள். படிவத்தை அழிக்க **Cancel** அழுத்துங்கள்.
5. **Existing Annual Budgeting Records**-ல், FY மூலம் தேடுங்கள், அல்லது பட்டியலை வடிகட்ட **Active** அல்லது **Inactive** தேர்வு செய்யுங்கள்.
6. ஒரு பதிவைத் திருத்த பென்சிலைத் தட்டி, பிறகு **Update** அழுத்துங்கள். அதை நிறுத்த வட்டக் கோடு ஐகானையும், மீண்டும் இயக்க அம்புக்குறி ஐகானையும் தட்டுங்கள்.

> Tip: ஒவ்வொரு நிதியாண்டுக்கும் ஒரே ஒரு பட்ஜெட் பதிவுதான் இருக்க முடியும், அது active ஆக இருந்தாலும் இல்லாவிட்டாலும்.

## பொதுவான பிரச்சனைகள்

- **"Please select a Financial Year".** (நிதியாண்டைத் தேர்வு செய்யுங்கள்) சேமிக்கும் முன் ஒரு வருடத்தைத் தேர்வு செய்யுங்கள்.
- **"Amount must be greater than zero".** (தொகை பூஜ்ஜியத்தை விட அதிகமாக இருக்க வேண்டும்) 0-க்கு மேல் ஒரு தொகையை உள்ளிடுங்கள்.
- **"An Annual Budgeting record for ... already exists".** (இந்த வருடத்துக்கான பதிவு ஏற்கனவே உள்ளது) புதிதாகச் சேர்ப்பதற்குப் பதிலாக, இருக்கும் பதிவைத் திருத்துங்கள்.
- **Save பட்டன் சாம்பல் நிறத்தில் உள்ளது.** இன்னும் எந்த நிதியாண்டும் இல்லை. முதலில் Financial Master-ல் ஒன்றைச் சேருங்கள்.
:::

::: section state-allocation
title: பட்ஜெட்டை மாநிலங்கள், மண்டலங்கள், HQ-களுக்குப் பகிர்வது
routes: /StateBudgetManagement
for: 
roles: Admin, SuperAdmin, CorporateAdmin, Director, AVP, SMD, SMM, RM, RMD
designations: 
tour: 1 > [data-help=bc-sb-tabs] | 2 > [data-help=bc-sb-select] | 3 > [data-help=bc-sb-cards] | 4 > [data-help=bc-sb-table] | 5 > [data-help=bc-sb-save]
minutes: 3
source-hash: 535c04c3b89862f4
confidence: ok

**இந்தத் திரை எதற்கு**
**State Budgeting Management** திரை வருடாந்திர பட்ஜெட்டை ஒவ்வொரு நிலையாகக் கீழே பகிர்கிறது. Admin அதை மாநிலங்களுக்குப் பகிர்வார், SMD அல்லது SMM ஒரு மாநிலத்தின் பட்ஜெட்டை அதன் மண்டலங்களுக்குப் பகிர்வார்கள், RM அல்லது RMDO ஒரு மண்டலத்தின் பட்ஜெட்டை அதன் HQ-களுக்குப் பகிர்வார்கள்.

1. உங்கள் பணிப்பொறுப்புக்கான டேப் திறக்கும்: **State Allocation** (Admin), **Region Allocation** (SMD, SMM) அல்லது **Headquarters Allocation** (RM, RMDO).
2. **Financial Year**-ஐத் தேர்வு செய்யுங்கள். மண்டல டேபில் **State**-ஐயும் தேர்வு செய்யுங்கள். HQ டேபில் **State** மற்றும் **Region**-ஐத் தேர்வு செய்யுங்கள்.
3. **Annual Budget**, **Allocated Budget**, **Remaining Budget**, அல்லது **State Allocated Budget**, **State MDO Budget** போன்ற கார்டுகளைச் சரிபாருங்கள்.
4. அட்டவணையில், ஒவ்வொரு மாநிலம், மண்டலம் அல்லது HQ-க்கான தொகையை டைப் செய்யுங்கள். மண்டல மற்றும் HQ டேப்களில், முதலில் மேலே உள்ள **State MDO Amount** அல்லது **Region MDO Amount**-ஐ உள்ளிடுங்கள்.
5. சேமியுங்கள். மாநில டேபில் **Save Draft** அழுத்தி, பிறகு **Submit For Validation** அழுத்துங்கள். மற்ற டேப்களில் **Save Region Allocation** அல்லது **Save Headquarters Allocation** அழுத்துங்கள்.

> Careful: மாநில டேபில், சேமிப்பதற்கு முன் முழு வருடாந்திர பட்ஜெட்டும் பகிரப்பட்டிருக்க வேண்டும்.

## பொதுவான பிரச்சனைகள்

- **"All budget must be allocated to states."** (முழு பட்ஜெட்டும் மாநிலங்களுக்கு ஒதுக்கப்பட வேண்டும்) இன்னும் மீதமுள்ள தொகையைச் செய்தி காட்டும். அதை மாநிலங்களுக்குப் பகிருங்கள்.
- **"Region allocation cannot exceed the remaining state budget"** அல்லது **"Headquarters allocation cannot exceed the remaining region budget".** (ஒதுக்கீடு மீதமுள்ள பட்ஜெட்டை விட அதிகமாக இருக்கக்கூடாது) தொகையைக் குறையுங்கள்.
- **"Save this region's allocation above before distributing it to headquarters."** (HQ-களுக்குப் பகிரும் முன் இந்த மண்டலத்தின் ஒதுக்கீட்டைச் சேமியுங்கள்) அந்த மண்டலத்துக்கு இன்னும் பட்ஜெட் இல்லை. SMD அல்லது SMM முதலில் மண்டல ஒதுக்கீட்டைச் சேமிக்க வேண்டும்.
- **"You do not have access to any budget allocation level."** (எந்த பட்ஜெட் ஒதுக்கீட்டு நிலைக்கும் உங்களுக்கு அனுமதி இல்லை) உங்கள் பணிப்பொறுப்பு பட்ஜெட்டைப் பகிராது. உங்கள் நிர்வாகியிடம் கேளுங்கள்.
:::

::: section program-budget
title: நிகழ்ச்சி பட்ஜெட்டுகளைத் திட்டமிடுவது, சரிபார்ப்பது, ஒப்புதல் தருவது
routes: /BudgetingManagements
for: 
roles: 
designations: 
minutes: 4
source-hash: 9d6de3d0ef847b35
confidence: ok

**இந்தத் திரை எதற்கு**
**Budgeting Management**-ல், MO, RMDO, SMDO தங்கள் பட்ஜெட்டை நிகழ்ச்சிகளுக்கும் மாதங்களுக்கும் பிரிப்பார்கள். சேமித்த திட்டங்களை RMDO, SMDO சரிபார்ப்பார்கள், RM, SMM ஒப்புதல் தருவார்கள்.

1. **Program-wise Budget Planning**-ன் மேலே நிதியாண்டைத் தேர்வு செய்யுங்கள்.
2. கார்டுகளைச் சரிபாருங்கள்: உங்கள் பட்ஜெட் (உதாரணமாக **Available Budget**), **Allocated Budget**, **Remaining Budget** மற்றும் **SMDO Budget**.
3. **Budget plan list**-ல், **April** முதல் **March** வரை ஒவ்வொரு மாதமும் எத்தனை நிகழ்ச்சிகள் நடத்துவீர்கள் என்று உள்ளிடுங்கள். ஒரு நிகழ்ச்சிக்கு அனுமதி இருந்தால், அதன் **Budget Amount**-ஐ மாற்றலாம். ஒவ்வொரு வரிசையின் **Total Budget** தானாகப் புதுப்பிக்கப்படும்.
4. **Submit** அழுத்துங்கள். முழு பட்ஜெட்டும் ஒதுக்கப்பட்டிருக்க வேண்டும்.
5. சரிபார்க்க (RMDO அல்லது SMDO), **Validate Budget** அழுத்தி, **Saved program budgets awaiting validation**-ல் ஒரு திட்டத்தைத் தேர்வு செய்து, தேவைப்பட்டால் எண்ணிக்கைகளைத் திருத்தி, **Validate** அழுத்துங்கள். உங்கள் சொந்தத் திட்டத்துக்குத் திரும்ப **Create Budget** அழுத்துங்கள்.
6. ஒப்புதல் தர (RM அல்லது SMM), **Validated program budgets awaiting approval**-ல் ஒரு திட்டத்தைத் தேர்வு செய்து, தேவைப்பட்டால் எண்ணிக்கைகளைத் திருத்தி, **Approve** அழுத்துங்கள்.
7. அட்டவணையைப் பகிர **Copy**, **CSV**, **Excel**, **PDF** அல்லது **Print** பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Total allocated budget ... must equal allotted budget ..."** (மொத்த ஒதுக்கீடு கொடுக்கப்பட்ட பட்ஜெட்டுக்குச் சமமாக இருக்க வேண்டும்) இன்னும் கொஞ்சம் பட்ஜெட் மீதம் உள்ளது. எதுவும் மீதம் இல்லாத வரை நிகழ்ச்சி எண்ணிக்கைகளைச் சேருங்கள்.
- **"... exceeds allotted budget ..."** (கொடுக்கப்பட்ட பட்ஜெட்டை விட அதிகம்) நீங்கள் அதிகமாகத் திட்டமிட்டுவிட்டீர்கள். எண்ணிக்கைகளைக் குறையுங்கள்.
- **"No eligible budgets found for your assigned scope and selected FY."** (உங்கள் பகுதிக்கும் தேர்வு செய்த வருடத்துக்கும் பட்ஜெட் எதுவும் இல்லை) இந்த வருடத்தில் உங்களுக்காக எதுவும் காத்திருக்கவில்லை. வருடத்தைச் சரிபாருங்கள்.
- **"Only MO, RMDO and SMDO can save program budgets."** (MO, RMDO, SMDO மட்டுமே நிகழ்ச்சி பட்ஜெட்டைச் சேமிக்க முடியும்) உங்கள் பணிப்பொறுப்பால் பார்க்க, சரிபார்க்க அல்லது ஒப்புதல் தர மட்டுமே முடியும்.
:::

::: section budget-submissions
title: நான் அனுப்பிய பட்ஜெட்டுகளைப் பின்தொடர்வது
routes: /BudgetSubmissions
for: 
roles: 
designations: 
minutes: 2
source-hash: 3e92ca02097adb1c
confidence: ok

**இந்தத் திரை எதற்கு**
**Budget Submissions** திரையில் நீங்கள் அனுப்பிய நிகழ்ச்சி பட்ஜெட்டுகள் பட்டியலாகத் தெரியும், ஒவ்வொன்றும் இப்போது எந்த நிலையில் உள்ளது என்றும் காட்டும்.

1. கார்டுகளைப் பாருங்கள்: **Program Budget**, **Approved**, **Pending validation**, **Rejected** மற்றும் **Drafts**.
2. **Date Range**, **Program Type**, **Program Name** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
3. ஒரு நிகழ்ச்சியை வேகமாகக் கண்டுபிடிக்க **Search Program...**-ல் டைப் செய்யுங்கள்.
4. அட்டவணையில் **Program Budget**, **Submitted Date**, **Status** மற்றும் **Validation Due**-ஐச் சரிபாருங்கள்.
5. விவரங்களைப் பார்க்க கண் ஐகானைத் தட்டுங்கள். அங்கிருந்து **Print** அல்லது **Export PDF** செய்யலாம்.

## பொதுவான பிரச்சனைகள்

- **"No budget submission records found."** (அனுப்பிய பட்ஜெட் பதிவுகள் எதுவும் இல்லை) நீங்கள் இன்னும் திட்டம் அனுப்பவில்லை, அல்லது ஃபில்டர்கள் ரொம்பக் குறுகலாக உள்ளன. **Clear All** அழுத்துங்கள்.
:::

::: section hq-budget-overview
title: HQ பட்ஜெட் பயன்பாட்டைப் பார்ப்பது
routes: /BudgetOverview
for: 
roles: 
designations: 
minutes: 1
source-hash: 6911fbefa137aa04
confidence: ok

**இந்தத் திரை எதற்கு**
**HQ Budget Allocation** திரை ஒரு மண்டலத்தின் பட்ஜெட் அதன் HQ-களுக்கு எப்படிப் பகிரப்பட்டுள்ளது, ஒவ்வொரு HQ-வும் எவ்வளவு பயன்படுத்தியுள்ளது என்று காட்டும்.

1. கார்டுகளைப் பாருங்கள்: **Total Regional Budget**, **Allocated to HQs**, **Remaining Budget** மற்றும் **Utilizes Budget**.
2. **Budget plan list**-ல், ஒவ்வொரு HQ-வின் **Allocated**, **Utilized**, **Remaining**, **Utilization %**, மற்றும் அதன் **Pending CSR-1**, **Pending CSR-2**-ஐச் சரிபாருங்கள்.
3. **Status**-ல் **Healthy**, **Near Limit** அல்லது **Exceeded** என்று காட்டும்.
4. HQ-வின் பட்ஜெட் பயன்பாட்டைத் திறக்க கண் ஐகானைத் தட்டுங்கள்.
5. **Budgeting Management**-க்குப் போக **Allocate Budget** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **எண்கள் மாறுவதில்லை.** இந்தத் திரை இன்னும் உருவாக்கப்பட்டு வருகிறது. உண்மையான எண்களுக்கு **Budgeting Management** மற்றும் **State Budgeting Management** பயன்படுத்துங்கள்.
:::

::: section reallocation
title: மறுஒதுக்கீடு மூலம் பட்ஜெட்டை மாற்றுவது
routes: /BudgetReallocations
for: 
roles: 
designations: 
minutes: 3
source-hash: 61df1dfc270033ec
confidence: ok

**இந்தத் திரை எதற்கு**
பட்ஜெட் குறைவாக இருப்பதால் CSR-2-க்கு ஒப்புதல் தர முடியவில்லை என்றால், **Budget Reallocations** திரை அதே நிலையில் உள்ள ஒரு பட்ஜெட்டிலிருந்து இன்னொன்றுக்குப் பணத்தை மாற்றும். RM, HQ-களுக்கு இடையே பணத்தை மாற்றுவார், SMM மண்டலங்களுக்கு இடையே, Admin மாநிலங்களுக்கு இடையே.

1. **New Reallocation Request**-ன் கீழ், **Initiated by** மற்றும் நிலை பேட்ஜைச் சரிபாருங்கள்.
2. **Financial Year**-ஐத் தேர்வு செய்யுங்கள். கேட்டால், **State** மற்றும் **Region**-ஐத் தேர்வு செய்யுங்கள்.
3. **From** (பணம் தரும் பட்ஜெட்) மற்றும் **To** (பணம் தேவைப்படும் பட்ஜெட்) தேர்வு செய்யுங்கள். **Source Available Budget** மற்றும் **Selected destination**-ஐச் சரிபாருங்கள்.
4. **Reallocation Amount (₹)**-ஐ உள்ளிட்டு, **Reason for Reallocation** எழுதுங்கள்.
5. **Approval route**-ஐச் சரிபாருங்கள், உதாரணமாக RM, SMM, AVP, MD HOD. **Apply Reallocation** அழுத்துங்கள்.
6. ஒப்புதல் தருபவர்கள்: **Reallocation Requests**-ல், **Approval / Rejection Remarks** எழுதி, அந்த வரிசையில் **Approve** அல்லது **Reject** அழுத்துங்கள்.
7. சமீபத்திய நிலையைப் பார்க்க **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Source budget shortage".** (பணம் தரும் பட்ஜெட்டில் போதுமான பணம் இல்லை) அதில் மீதமுள்ளதை விடத் தொகை அதிகம். அதைக் குறையுங்கள், அல்லது வேறு source-ஐத் தேர்வு செய்யுங்கள்.
- **"No eligible budgets found."** (தகுதியான பட்ஜெட் எதுவும் இல்லை) வருடத்தையும், உங்கள் மாநிலம் அல்லது மண்டலத்துக்கான ஒதுக்கீடுகள் ஒப்புதல் பெற்றுள்ளனவா என்பதையும் சரிபாருங்கள்.
- **"Apply permissions are unavailable for this login."** (இந்த லாகினுக்குக் கோரிக்கை போடும் அனுமதி இல்லை) RM, SMM, Admin மட்டுமே மறுஒதுக்கீடு கேட்க முடியும்.
:::

::: section program-master
title: நிகழ்ச்சிப் பட்டியலைப் பராமரிப்பது
routes: /ProgramMaster
for: 
roles: 
designations: 
minutes: 3
source-hash: 800f0d1d4704db06
confidence: ok

**இந்தத் திரை எதற்கு**
**Program Master** திரையில் நிகழ்ச்சி வகைகள், நிகழ்ச்சிகளின் பட்டியல், யார் அவற்றைத் திட்டமிடலாம், ஒவ்வொரு மாநிலத்திலும் நிகழ்ச்சித் தொகை என்ன என்பவை வைக்கப்படும்.

1. **Program Types**-ஐத் திறங்கள். **Program Type Name**-ஐ உள்ளிட்டு, **Is Amount Change**-ஐ **Allowed** அல்லது **Not Allowed** என்று வைத்து, **Save** அழுத்துங்கள்.
2. **Program Master**-ஐத் திறங்கள். **Program Type**-ஐத் தேர்வு செய்து, **Program Name**-ஐ உள்ளிட்டு, **MO**, **RMDO**, **SMDO**-ஐ **Eligible** அல்லது **Not Eligible** என்று வையுங்கள். **Save** அழுத்துங்கள்.
3. ஒரு வரிசையைத் திருத்த அதிலுள்ள பென்சிலைத் தட்டுங்கள்.
4. **State Mapping**-ஐத் திறங்கள் (அல்லது ஒரு நிகழ்ச்சியில் உள்ள மேப் பின்னைத் தட்டுங்கள்). **Program**-ஐத் தேர்வு செய்யுங்கள்.
5. ஒவ்வொரு மாநிலத்துக்கும், **Applicable**-ஐ ஆன் அல்லது ஆஃப் செய்யுங்கள். பொருந்தும் மாநிலத்துக்கு, **Program Amount (₹)**-ஐ உள்ளிட்டு அந்த வரிசையில் **Save** அழுத்துங்கள்.

> Tip: **Budgeting Management**-ல் திட்டமிடுபவர்கள் ஒரு நிகழ்ச்சியின் பட்ஜெட் தொகையை மாற்றலாமா என்பதை **Is Amount Change** முடிவு செய்யும்.

## பொதுவான பிரச்சனைகள்

- **"View only - you do not have update permission."** (பார்க்க மட்டும், மாற்றும் அனுமதி இல்லை) மாற்றும் அனுமதிக்கு உங்கள் நிர்வாகியிடம் கேளுங்கள்.
:::

::: section rmd-hq-mapping
title: ஒரு RMDO-க்கு HQ-களை ஒதுக்குவது
routes: /RMDHQMapping
for: 
roles: 
designations: 
minutes: 2
source-hash: 1c5c04d12fa1b412
confidence: ok

**இந்தத் திரை எதற்கு**
**RMD Headquarters Mapping** திரை ஒரு RMDO-வை ஒன்று அல்லது அதற்கு மேற்பட்ட HQ-களுடன் ஒரே பதிவில் இணைக்கும்.

1. **Assign Headquarters**-ன் கீழ், **Employee (RMD)**-ஐத் தேர்வு செய்யுங்கள்.
2. **State**-ஐத் தேர்வு செய்து, பிறகு **Region**-ஐத் தேர்வு செய்யுங்கள்.
3. **Select Headquarters** தட்டி HQ-களை டிக் செய்யுங்கள், அல்லது **Select All**. **Done** அழுத்துங்கள்.
4. **Save Mapping** அழுத்துங்கள். **Clear** படிவத்தைக் காலி செய்யும்.
5. **Existing RMD HQ Mappings**-ல், திருத்த பென்சிலைத் தட்டுங்கள் (பிறகு **Update Mapping**), நீக்க பெருக்கல் குறியைத் தட்டுங்கள் (பிறகு **Confirm**).

## பொதுவான பிரச்சனைகள்

- **"No headquarters found."** (HQ எதுவும் இல்லை) முதலில் மண்டலத்தைத் தேர்வு செய்யுங்கள், அல்லது அந்த மண்டலத்தில் HQ எதுவும் இல்லை.
:::

::: section sanction-sequence
title: Sanction எண்களின் தொடக்க மதிப்பை வைப்பது
routes: /SanctionNumberSequence
for: 
roles: 
designations: 
minutes: 2
source-hash: 7e920cca79cfe7a9
confidence: ok

**இந்தத் திரை எதற்கு**
ஒப்புதல் பெற்ற CSR-1-க்கு ஒரு sanction எண் கிடைக்கும். **Sanction Number Sequence** திரை ஒவ்வொரு மாநிலம் அல்லது மண்டலத்துக்கும் இந்த எண்களின் தொடக்க மதிப்பை வைக்கும்.

1. **Sequence Scope**-ஐத் தேர்வு செய்யுங்கள்: **State (SMM)** அல்லது **Region (RM / RMM)**.
2. **State**-ஐத் தேர்வு செய்யுங்கள், மண்டல scope என்றால் **Region**-ஐயும் தேர்வு செய்யுங்கள். **State Code** மற்றும் **Region Code** master-களிலிருந்து தானாக நிரப்பப்படும்.
3. தொடக்க மதிப்பை உள்ளிடுங்கள்.
4. **Save Sequence** அழுத்துங்கள். **Clear** படிவத்தைக் காலி செய்யும்.
5. **Existing Sanction Number Sequences** பட்டியலைச் சரிபாருங்கள். மாநிலம், மண்டலம் அல்லது code மூலம் தேடலாம்.

> Careful: ஒரு sequence உருவாக்கிய பிறகு அதை மாற்ற முடியாது.

## பொதுவான பிரச்சனைகள்

- **"Code not configured in State master".** (State master-ல் code அமைக்கப்படவில்லை) முதலில் அதன் master-ல் மாநிலம் அல்லது மண்டல code-ஐச் சேருங்கள்.
:::

::: section create-csr1
title: ஒரு நிகழ்ச்சிக்கு CSR-1 போடுவது
routes: /CREATE-CSR-1Management
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c1-banner] | 2 > [data-help=bc-c1-search] | 3 > [data-help=bc-c1-plan] | 4 > [data-help=bc-c1-focus] | 5 > [data-help=bc-c1-expense] | 6 > [data-help=bc-c1-actions]
minutes: 5
source-hash: 640ab3b5f3b1dedd
confidence: ok

**இந்தத் திரை எதற்கு**
CSR-1 என்பது ஒரு நிகழ்ச்சிக்கு முன் போடும் திட்டம். **CSR-1 Program - Wise Budget Allocation** திரையில் ஒப்புதல் பெற்ற உங்கள் நிகழ்ச்சி பட்ஜெட்டுகள் தெரியும். ஒன்றைத் தேர்வு செய்து CSR-1 படிவத்தை நிரப்புங்கள்.

1. நீலப் பெட்டியைப் படியுங்கள். நீங்கள் எந்த மாதத்துக்குத் திட்டமிடுகிறீர்கள் என்று அது சொல்லும்.
2. **Budget plan list**-ல் நிகழ்ச்சியைக் கண்டுபிடித்து (**Search Program...** அல்லது **Filters** பயன்படுத்துங்கள்), **Remaining Programs**-ஐச் சரிபார்த்து, **Apply CSR 1** அழுத்துங்கள்.
3. **CSR-1 Plan Details**-ன் கீழ், நிகழ்ச்சியைச் சரிபார்த்து **Budget**, **HQ**, **Location**, **Responsible Person** மற்றும் **Date** நிரப்புங்கள்.
4. **Focus Details**-ன் கீழ், குறைந்தது ஒரு **Focused Crop**-ஐயும் அதற்கு ஒரு **Focused Product**-ஐயும் தேர்வு செய்யுங்கள். ஒவ்வொரு பெட்டியிலும் மூன்று பயிர்கள், மூன்று products வரை சேர்க்கலாம்.
5. **Expense Breakdown**-ன் கீழ், ஒவ்வொரு செலவுக்கும் திட்டமிட்ட தொகையை உள்ளிடுங்கள். **Total Expenses** பட்ஜெட்டுக்குள் இருக்க வேண்டும்.
6. பிறகு முடிக்க **Save Draft** அழுத்துங்கள், அல்லது அனுப்ப **Submit for Validation** அழுத்துங்கள்.

> Tip: **Responsible Person** தான் நிகழ்ச்சியை நடத்தி CSR-2 அறிக்கை அனுப்புவார்.

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Budget** | இந்த நிகழ்ச்சிக்கான பணம். கட்டாயம். |
| **HQ** மற்றும் **Location** | நிகழ்ச்சி எங்கே நடக்கிறது. கட்டாயம். |
| **Responsible Person** | அதை நடத்தும் அதிகாரி. கட்டாயம். |
| **Date** | நிகழ்ச்சித் தேதி, திட்டமிட்ட மாதத்துக்குள். கட்டாயம். |

## பொதுவான பிரச்சனைகள்

- **"Budget Exceeded"** அல்லது **"Total Expense Breakdown ... exceeds the allocated Budget".** (செலவு ஒதுக்கிய பட்ஜெட்டை விட அதிகம்) செலவுத் தொகைகளைக் குறையுங்கள்.
- **"Please enter at least one expense amount."** (குறைந்தது ஒரு செலவுத் தொகையை உள்ளிடுங்கள்) குறைந்தது ஒரு திட்டமிட்ட செலவை உள்ளிடுங்கள்.
- **"Please select at least one Product for Focus Crop 1."** (Focus Crop 1-க்குக் குறைந்தது ஒரு Product-ஐத் தேர்வு செய்யுங்கள்) நீங்கள் தேர்வு செய்த ஒவ்வொரு பயிருக்கும் ஒரு product-ஐத் தேர்வு செய்யுங்கள்.
- **"CSR-1 creation is not available for Admin role."** (Admin-க்கு CSR-1 உருவாக்க முடியாது) Admin, **CSR-1 Management**-ல் CSR-1-ஐப் பார்க்க மட்டுமே முடியும்.
:::

::: section csr1-list
title: CSR-1-ஐப் பின்தொடர்வது, சரிபார்ப்பது, ஒப்புதல் தருவது
routes: /CSR-1List
for: 
roles: 
designations: 
minutes: 3
source-hash: 33a5bb3a053b06a1
confidence: ok

**இந்தத் திரை எதற்கு**
**CSR-1 Management**-ல் CSR-1 திட்டங்கள் பட்டியலாகத் தெரியும். களப் பணியாளர்கள் தங்களுடையதைப் பின்தொடர்வார்கள். MO, SMD அவற்றைச் சரிபார்ப்பார்கள், RM, SMM ஒப்புதல் தருவார்கள்.

1. கார்டுகளைப் பாருங்கள்: **Total CSR -1**, **Approved**, **Pending**, **Rejected** மற்றும் **Drafts**.
2. **Filters**-ன் கீழ், **From Date**, **To Date**, **Program Type**, **Program Name** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள்.
3. **CSR-1 plan list**-ல், **Status**, **Sanction Number** மற்றும் **CSR-2 Eligibility**-ஐச் சரிபாருங்கள். **State** அல்லது **HQ** டேக், திட்டம் எந்த நிலையைச் சேர்ந்தது என்று காட்டும்.
4. **CSR-1 Program Details**-ஐத் திறக்க கண் ஐகானைத் தட்டுங்கள்.
5. அது உங்களுக்காகக் காத்திருந்தால், **Validate** அழுத்துங்கள், அல்லது ஒப்புதல் பட்டனை அழுத்துங்கள் (**Approve**, அல்லது தாமதமான திட்டத்துக்கு நிலையின் பெயர்). நிராகரிக்க, **Reject** அழுத்தி, குறிப்புகளை எழுதி, **Confirm Reject** அழுத்துங்கள்.
6. திட்டம் **Approved** ஆனதும், நிகழ்ச்சியின் அறிக்கையை அனுப்ப வரிசையில் உள்ள **CSR-2** அழுத்துங்கள், அல்லது விவரங்களில் உள்ள **Open CSR-2** அழுத்துங்கள்.

> Careful: குப்பைத்தொட்டி ஐகான் CSR-1-ஐ மீண்டும் கேட்காமல் உடனே நீக்கிவிடும்.

## பொதுவான பிரச்சனைகள்

- **விவரங்களில் "Awaiting MO Validation" அல்லது "Awaiting SMD Validation" என்று காட்டுகிறது.** யாராவது ஒப்புதல் தருவதற்கு முன் திட்டம் சரிபார்க்கப்பட வேண்டும்.
- **CSR-2 பட்டன் சாம்பல் நிறத்தில் உள்ளது.** CSR-1 இன்னும் **Approved** ஆகவில்லை.
- **சிவப்பு நிற "Rejection Reason" பெட்டி வருகிறது.** பட்டியலுக்குக் கீழே உள்ள காரணத்தைப் படியுங்கள்.
:::

::: section csr2-list
title: CSR-2 அறிக்கை அனுப்புவது, சரிபார்ப்பது, ஒப்புதல் தருவது
routes: /CSR-2List
for: 
roles: 
designations: 
tour: 1 > [data-help=bc-c2-cards] | 2 > [data-help=bc-c2-filters] | 3 > [data-help=bc-c2-table] | 4 > [data-help=bc-c2-table] | 5 > [data-help=bc-c2-table]
minutes: 3
source-hash: 3490f090fad6d820
confidence: ok

**இந்தத் திரை எதற்கு**
CSR-2 என்பது நிகழ்ச்சி நடந்த பிறகு அனுப்பும் அறிக்கை. **CSR-2 Program Execution** பட்டியலில் உங்கள் நிகழ்ச்சிகள் வருகை, மீடியா, செலவுகள், முன்னேற்றத்துடன் தெரியும்.

1. கார்டுகளைப் பாருங்கள்: **Total CSR-2**, **Approved**, **Submitted** மற்றும் **Drafts**. ஒரு கார்டைத் தட்டினால் அந்த நிகழ்ச்சிகள் மட்டும் தெரியும்.
2. **Filters**-ன் கீழ், தேதிகள், **Program Type**, **Program Name**, **Location** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
3. **Assigned Programs**-ல், **Approved Budget**, **Attendance**, **Media**, **Actual Expense**, **Report Completion** மற்றும் **Status**-ஐச் சரிபாருங்கள்.
4. CSR-2 படிவத்தைத் திறக்க **View Details** அழுத்துங்கள். நிகழ்ச்சித் தேதி, புகைப்படங்கள், பங்கேற்றவர்கள், ஒவ்வொரு உண்மையான செலவையும் அதன் ஆதாரத்துடன் நிரப்பி, அனுப்புங்கள்.
5. ஒரு அறிக்கை உங்களுக்காகக் காத்திருந்தால், **Validate** (MO அல்லது SMD) அழுத்துங்கள், அல்லது **Approve CSR-2** அல்லது **Approve Level 1** போன்ற ஒப்புதல் பட்டனை அழுத்துங்கள்.

> Tip: அறிக்கை **Draft**-லிருந்து **Approved** வரை நகரும்போது **Report Completion** கூடிக்கொண்டே போகும்.

## பொதுவான பிரச்சனைகள்

- **"No CSR-2 records found."** (CSR-2 பதிவுகள் எதுவும் இல்லை) ஒப்புதல் பெற்ற எந்த CSR-1-ம் உங்களுக்கு ஒதுக்கப்படவில்லை, அல்லது ஃபில்டர்கள் ரொம்பக் குறுகலாக உள்ளன.
- **"You are not authorized to validate this CSR-2."** (இந்த CSR-2-ஐச் சரிபார்க்க உங்களுக்கு அனுமதி இல்லை) வேறு பணிப்பொறுப்பு உள்ளவர் அதைச் சரிபார்க்க வேண்டும்.
- **பட்ஜெட் காரணமாக ஒப்புதல் தோல்வியடைகிறது.** RM, SMM அல்லது Admin-இடம் பட்ஜெட் மறுஒதுக்கீடு கேளுங்கள்.
:::

::: section csr2-assigned
title: எனக்கு ஒதுக்கப்பட்ட நிகழ்ச்சிகளைத் திறப்பது
routes: /CSR-2ProgramExecution
for: 
roles: 
designations: 
minutes: 1
source-hash: 6d55229f3d01c986
confidence: ok

**இந்தத் திரை எதற்கு**
இந்த **CSR-2 Program Execution** திரையில் உங்களுக்கு ஒதுக்கப்பட்ட, ஒப்புதல் பெற்ற CSR-1 நிகழ்ச்சிகள் தெரியும். அதனால் அவற்றின் CSR-2 அறிக்கைகளைத் தொடங்கலாம்.

1. மேலே உள்ள **Total CSR -2**-ஐப் பாருங்கள்.
2. **Assigned Programs**-ல், நிகழ்ச்சியை அதன் **Sanction Number**, **Program Type** அல்லது **Program** மூலம் கண்டுபிடியுங்கள்.
3. **Approved Budget** மற்றும் **Status**-ஐச் சரிபாருங்கள்.
4. அந்த நிகழ்ச்சிக்கான CSR-2 படிவத்தைத் திறக்க **View Program** அழுத்துங்கள்.
5. சமீபத்திய பட்டியலைப் பெற **Refresh Data** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No approved CSR-1 programs found."** (ஒப்புதல் பெற்ற CSR-1 நிகழ்ச்சிகள் எதுவும் இல்லை) உங்கள் CSR-1 திட்டங்கள் எதற்கும் இன்னும் ஒப்புதல் கிடைக்கவில்லை.
:::

::: section csr2-form-preview
title: CSR-2 Management படிவம்
routes: /CSR2, /csrplandetails2
for: 
roles: 
designations: 
minutes: 1
source-hash: 7593c2c21789d74d
confidence: ok

**இந்தத் திரை எதற்கு**
**CSR-2 Management** திரை ஒரு CSR-2 அறிக்கையின் பகுதிகளைக் காட்டும்: பட்ஜெட் கார்டுகள், திட்ட விவரங்கள், முக்கியப் பயிர் விவரங்கள், ஆதாரங்களுடன் செலவுப் பிரிவு. உண்மையான நிகழ்ச்சிக்கு அறிக்கை அனுப்ப, **CSR-2 List**-ஐத் திறந்து **View Details** அழுத்துங்கள்.

1. கார்டுகளைச் சரிபாருங்கள்: **Approved Budget**, **Actual Expense** மற்றும் **Difference**.
2. **CSR-2 Plan Details**-ஐப் பாருங்கள்: நிகழ்ச்சி, நிகழ்ச்சிகளின் எண்ணிக்கை, பட்ஜெட், HQ, இடம்.
3. **Focus Details**-ஐப் பாருங்கள்: முக்கியப் பயிர்களும் products-ம்.
4. **Expense Breakdown**-ஐப் பாருங்கள்: ஒவ்வொரு செலவுக்கும் ஒரு தொகை, ஒரு **Date** மற்றும் **Upload proof** இருக்கும்.

## பொதுவான பிரச்சனைகள்

- **இங்கே Save Draft அல்லது Submit CSR-2 எதுவும் செய்வதில்லை.** அறிக்கை அனுப்ப **CSR-2 List** மற்றும் **View Details** பயன்படுத்துங்கள்.
:::

::: section final-report
title: இறுதி CSR அறிக்கையைப் பார்ப்பது
routes: /FinalReportCSRView
for: 
roles: 
designations: 
minutes: 1
source-hash: d8668b0d71867c79
confidence: ok

**இந்தத் திரை எதற்கு**
**Final Report - CSR Program Tracking** ஒவ்வொரு நிகழ்ச்சியையும் திட்டம் முதல் அறிக்கை வரை காட்டும்: CSR-1 நிலை, CSR-2 நிலை, sanction எண், செலவுகள், மீதமுள்ள பட்ஜெட்.

1. கார்டுகளைப் பாருங்கள்: **Total Programs**, **Approved**, **Pending**, **Rejected** மற்றும் **Drafts**.
2. தேதிகள், **Program Type**, **Program Name**, **CSR Type** அல்லது **Status** தேர்வு செய்ய **Filters** பயன்படுத்தி, **Apply Filters** அழுத்துங்கள்.
3. **CSR-2 plan list**-ல், **Total Expenses**, **Approved Budget**, **Remaining Budget**-ஐ ஒப்பிட்டுப் பாருங்கள். **Report Status**-ல் **Completed** அல்லது **Pending** என்று காட்டும்.
4. நிகழ்ச்சி விவரங்களைப் பார்க்க கண் ஐகானைத் தட்டி, பிறகு **Print** அல்லது **Export PDF** செய்யுங்கள்.

## பொதுவான பிரச்சனைகள்

- **எண்கள் மாறுவதில்லை.** இந்தத் திரை இன்னும் உருவாக்கப்பட்டு வருகிறது. உண்மையான பதிவுகளுக்கு **CSR-1 Management** மற்றும் **CSR-2 List** பயன்படுத்துங்கள்.
:::

::: section rmd-queue
title: RMD சரிபார்ப்பு வரிசை
routes: /RMDValidationQueue
for: 
roles: 
designations: 
minutes: 2
source-hash: 641c748d235d2f14
confidence: ok

**இந்தத் திரை எதற்கு**
**RMD Validation Queue**-ல் Market Officer-களிடமிருந்து வந்த CSR-1, CSR-2 சமர்ப்பிப்புகள் பட்டியலாகத் தெரியும்.

1. டேபைத் தேர்வு செய்யுங்கள்: **CSR-1 Submissions** அல்லது **CSR-2 Reports**. எத்தனை உள்ளன என்று பேட்ஜ் காட்டும்.
2. **Date Range**, **Program Type**, **Head Quarters** அல்லது **Status** தேர்வு செய்ய **Filters**-ஐத் திறந்து, **Apply Filters** அழுத்துங்கள்.
3. ஒரு சமர்ப்பிப்பைக் கண்டுபிடிக்கத் தேடல் பெட்டியில் டைப் செய்யுங்கள்.
4. **MO Name**, **Program Name**, **Head Quarters**, **Total Budget**, **Validation Due** மற்றும் **Status**-ஐச் சரிபாருங்கள்.
5. சமீபத்திய பட்டியலைப் பெற **Refresh Data** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Failed to load data".** (டேட்டாவை ஏற்ற முடியவில்லை) உங்கள் இணைய இணைப்பைச் சரிபார்த்து **Try Again** அழுத்துங்கள்.
- **"No submissions found".** (சமர்ப்பிப்புகள் எதுவும் இல்லை) Filters-ல் **Clear All** அழுத்துங்கள்.
:::

::: section mo-validation
title: MO சமர்ப்பிப்புச் சரிபார்ப்பு
routes: /MOSubmissionValidation
for: 
roles: 
designations: 
minutes: 1
source-hash: fbb06076726e4306
confidence: ok

**இந்தத் திரை எதற்கு**
**MO Submission Validation** திரையில் Market Officer-களிடமிருந்து வந்த CSR-1, CSR-2 சமர்ப்பிப்புகள், திட்டமிட்ட மற்றும் உண்மையான பட்ஜெட்டுகளுடன் தெரியும்.

1. டேபைத் தேர்வு செய்யுங்கள்: **CSR-1 Submissions** அல்லது **CSR-2 Reports**.
2. **Date Range**, **Program Type**, **Head Quatres** அல்லது **Status** தேர்வு செய்ய **Filters** பயன்படுத்துங்கள்.
3. **CSR-2 Expense Report Validation List**-ல், **Planned Budget**, **Actual Budget**, **Variance**-ஐ ஒப்பிட்டுப் பாருங்கள்.
4. **Validation Due** மற்றும் **CSR - 1 Status**-ஐச் சரிபாருங்கள்.

## பொதுவான பிரச்சனைகள்

- **பட்டியல் மாறுவதில்லை.** இந்தத் திரை இன்னும் உருவாக்கப்பட்டு வருகிறது. CSR-1-ஐ **CSR-1 Management**-லும், CSR-2-ஐ **CSR-2 List**-லும் சரிபாருங்கள்.
:::

::: section rm-approval-status
title: RM ஒப்புதல் நிலை
routes: /RMApprovalStatus
for: 
roles: 
designations: 
minutes: 1
source-hash: dd292461c2960406
confidence: ok

**இந்தத் திரை எதற்கு**
RMD சரிபார்த்து RM-க்கு அனுப்பிய CSR-1, CSR-2 சமர்ப்பிப்புகளை **RM Approval Status** திரை பின்தொடரும்.

1. டேபைத் தேர்வு செய்யுங்கள்: **CSR-1 Submissions** அல்லது **CSR-2 Report**.
2. கார்டுகளைப் பாருங்கள்: **Sent to RM**, **Pending RM Approval**, **Approved by RM** மற்றும் **Rejected / Correction**.
3. **CSR-1 RM Approval Status List**-ல், **Mo Submitted**, **RMD Validated** மற்றும் **Status**-ஐச் சரிபாருங்கள்.
4. நிகழ்ச்சி விவரங்களையும் **Process Timeline**-ஐயும் பார்க்க கண் ஐகானைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **பட்டியல் மாறுவதில்லை.** இந்தத் திரை இன்னும் உருவாக்கப்பட்டு வருகிறது. CSR-1-க்கு **CSR-1 Management**-லும், CSR-2-க்கு **CSR-2 List**-லும் ஒப்புதல் தாருங்கள்.
:::

::: section request-extension
title: திட்ட நீட்டிப்பு கேட்பது
routes: /RequestExtension/{CSRId:int}
for: 
roles: 
designations: 
minutes: 2
source-hash: c4d4cc4e073e1bda
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரு sales audit திட்டத்தின் தேவையான turnover (விற்பனை அளவு) நேரத்துக்குள் எட்டப்படவில்லை என்றால், **Request Extension** திரை மூலம் பிந்தைய முடிவுத் தேதியைக் கேட்கலாம். இதை **Period-End Review**-லிருந்து திறங்கள்.

1. **Scheme Information (Read Only)**-ஐச் சரிபாருங்கள்: **Sanction Number**, **Current Scheme Period**, **Required Turnover (TO)**, **Actual Turnover Achieved** மற்றும் **Balance to Target**.
2. **Requested New End Date**-ஐத் தேர்வு செய்யுங்கள். அது இப்போதைய முடிவுத் தேதிக்குப் பிறகு இருக்க வேண்டும்.
3. **Reason for Extension**-ஐத் தேர்வு செய்யுங்கள்.
4. **Remarks / Explanation (Optional)**-ஐ 500 எழுத்துகள் வரை சேருங்கள்.
5. **Submit Extension Request** அழுத்துங்கள்.
6. **Extension Request Submitted** பெட்டியில் **Request ID** மற்றும் **Pending Approval** தெரியும். **View My Request** அல்லது **Go to Sales Audit List** அழுத்துங்கள்.

> Tip: கோரிக்கைக்கு ஒப்புதல் கிடைக்கும் வரை திட்டத்தின் முடிவுத் தேதி மாறாது.

## பொதுவான பிரச்சனைகள்

- **"Select the new end date."** அல்லது **"The new end date must be after ..."** (புதிய முடிவுத் தேதி பிந்தையதாக இருக்க வேண்டும்) பிந்தைய தேதியைத் தேர்வு செய்யுங்கள்.
- **"A request is already awaiting approval for this CSR."** (இந்த CSR-க்கு ஒரு கோரிக்கை ஏற்கனவே ஒப்புதலுக்காகக் காத்திருக்கிறது) முதல் கோரிக்கையின் முடிவுக்காகக் காத்திருங்கள்.
:::

::: section welfare-application
title: நலத்திட்ட விண்ணப்பத்தைப் பரிசீலிப்பது
routes: /ApprovalDetail/{Id:int}, /MOApproval/{Id:int}
for: 
roles: 
designations: 
minutes: 3
source-hash: a63f984a39e5444b
confidence: ok

**இந்தத் திரை எதற்கு**
**Application Details** திரை ஒரு டீலரின் ஒரு நலத்திட்ட (SDWA) விண்ணப்பத்தைக் காட்டும். அதனால் இப்போதைய நிலையில் உள்ள ஒப்புதல் தருபவர் அதைச் சரிபார்த்துச் செயல்படலாம். இதை **Scheme Approval**-லிருந்து திறங்கள்.

1. மேலே உள்ள **Approval Workflow**-ஐச் சரிபாருங்கள். MO முதல் SDWA Admin வரை ஒவ்வொரு நிலையும் அதில் தெரியும்.
2. **Applicant Information**, **Nominee & Beneficiary Information** மற்றும் **Eligibility Information**-ஐப் படியுங்கள்.
3. **Uploaded Documents**-ன் கீழ், ஒவ்வொரு கோப்பையும் சரிபார்க்க **View** அல்லது **Download** அழுத்துங்கள்.
4. முந்தைய நிலைகளின் **Previous Remarks**-ஐப் படியுங்கள்.
5. உங்கள் முறை என்றால், **Comments**-ஐத் தேர்வு செய்யுங்கள். **Recommendation** தானாக நிரம்பும். SDWA Admin, **Cheque Number** மற்றும் **Cheque Amount**-ஐயும் உள்ளிடுவார்.
6. **Submit** அழுத்துங்கள். பட்டியலுக்குத் திரும்ப **Back to Approvals** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Submit பட்டன் இல்லை.** விண்ணப்பம் இப்போது உங்கள் நிலையில் இல்லை.
- **விண்ணப்பம் நிராகரிக்கப்பட்டது என்று ஒரு சிவப்புப் பெட்டி சொல்கிறது.** திருத்தத்துக்காக அது டீலருக்குத் திருப்பி அனுப்பப்பட்டது, அல்லது மூடப்பட்டது.
:::
