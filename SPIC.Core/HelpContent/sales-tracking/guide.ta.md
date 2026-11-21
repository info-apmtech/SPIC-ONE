---
module: sales-tracking
title: விற்பனைத் திட்டமும் தணிக்கையும்
icon: bi-graph-up-arrow
order: 105
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
source-hash: 1976de717daddf89
confidence: ok

ஒரு CSR-2 நிகழ்ச்சிக்கு இறுதி ஒப்புதல் கிடைத்த பிறகு, அதன் விற்பனையைத் திட்டமிட்டுச் சரிபார்க்க வேண்டும். product வாரியாக ஒரு விற்பனைத் திட்டம் போடுங்கள், அதை உறுதி செய்யுங்கள், SAP-ல் இருந்து வரும் டீலர் விற்பனையை அந்தத் திட்டத்தின் கீழ் சேமியுங்கள், scheme முடியும்போது இலக்கை அடைந்தீர்களா என்று பாருங்கள்.

- plan | icon: bi-pencil-square | color: #2563eb
  title: திட்டம் போடுங்கள்
  text: scheme தேதிகளையும், விற்க நினைக்கும் product, அளவு, மதிப்பையும் அமையுங்கள்.
- track | icon: bi-graph-up-arrow | color: #0d9488
  title: விற்பனையைச் சேமித்துக் கண்காணியுங்கள்
  text: SAP டீலர் இன்வாய்ஸ்களை அப்லோட் செய்து, ஒவ்வொரு நாளும் திட்டத்தின் கீழ் சேமியுங்கள்.
- review | icon: bi-flag | color: #d97706
  title: கடைசியில் சரிபாருங்கள்
  text: Required Turnover எட்டப்பட்டதா என்று பாருங்கள். இல்லையென்றால், SMM கூடுதல் நேரம் கேட்கலாம்.
:::

::: section flow
title: ஒரு விற்பனைத் தணிக்கை எப்படி நகர்கிறது, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 63b369579a95c59f
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- owner | type: lane | color: #2563eb
  title: நிகழ்ச்சிப் பொறுப்பாளர்
  sub: CSR-1-ல் உள்ள Responsible Person
- data | type: lane | color: #64748b
  title: விற்பனைத் தரவு
  sub: SAP இன்வாய்ஸ் Excel
- managers | type: lane | color: #0d9488 | roles: RM, SMM, SMD, Admin, SuperAdmin
  title: மேலாளர்கள்
  sub: RM · SMM · SMD
- approvers | type: lane | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: நீட்டிப்புக்கு ஒப்புதல் தருபவர்கள்
  sub: AVP · Admin
- start | lane: owner | col: 1 | icon: bi-play-circle | color: #64748b | next: plan | screen: audit-overview
  title: தணிக்கையைத் தொடங்குதல்
  status: Not Started
  text: **Sales Audit** திரையில் **Start Audit** அழுத்துங்கள். நிகழ்ச்சியையும் பட்ஜெட்டையும் சரிபார்த்து, **Continue to sales plan** அழுத்துங்கள்.
- plan | lane: owner | col: 2 | icon: bi-pencil-square | color: #94a3b8 | next: dealers | screen: sales-plan
  title: விற்பனைத் திட்டம் போடுதல்
  status: Draft
  text: scheme தேதிகளையும் product-களையும் உள்ளிடுங்கள். **Save Draft** அல்லது **Save & Continue** அழுத்துங்கள்.
- dealers | lane: owner | col: 3 | icon: bi-people | color: #0891b2 | next: confirm
  title: டீலர்களை இணைத்தல்
  status: Draft
  text: **Dealers** படியில், இந்த நிகழ்ச்சிக்குக் கணக்கில் வரும் விற்பனை எந்த டீலர்களுடையது என்று தேர்வு செய்யுங்கள்.
- confirm | lane: owner | col: 4 | icon: bi-check2-square | color: #2563eb | next: upload | return: plan | screen: plan-review
  title: திட்டத்தை உறுதி செய்தல்
  status: Plan Confirmed
  text: **Review & Confirm Plan** திரையில் எல்லாவற்றையும் சரிபார்த்து **Confirm Sales Plan** அழுத்துங்கள். அதன் பிறகு திட்டம் பூட்டப்படும். அதற்கு முன் **Edit Sales Plan** மூலம் திரும்பிப் போகலாம்.
- upload | lane: data | col: 5 | icon: bi-cloud-arrow-up | color: #64748b | next: save | screen: dealer-sales-upload
  title: SAP விற்பனையை அப்லோட் செய்தல்
  status: New
  text: SAP டீலர் இன்வாய்ஸ் Excel, **Dealer Sales List** திரையில் **Bulk Upload** மூலம் அப்லோட் செய்யப்படும்.
- save | lane: owner | col: 6 | icon: bi-save | color: #d97706 | next: track | screen: daily-sales
  title: தினசரி விற்பனையைச் சேமித்தல்
  status: In Progress
  text: **Review & Confirm Plan** திரையில் **Refresh Sales** அழுத்தி, பிறகு **Save Daily Sales** அழுத்துங்கள். புதிய இன்வாய்ஸ் வரிகள் **Old** ஆகி, சாதனையில் (achievement) சேரும்.
- track | lane: managers | col: 7 | icon: bi-graph-up-arrow | color: #0d9488 | next: review | screen: tracking-list
  title: முன்னேற்றத்தைப் பின்தொடர்தல்
  status: In Progress
  text: RM, SMM, SMD தங்கள் பகுதியின் விற்பனையை **Sales Tracking** திரையில் பார்ப்பார்கள்.
- review | lane: owner | col: 8 | icon: bi-flag | color: #16a34a | screen: period-end-review
  title: காலம் முடிவில் சரிபார்த்தல்
  status: Completed
  text: சேமித்த விற்பனை **Required Turnover**-ஐ எட்டியதும், தணிக்கையில் **Completed** என்று காட்டும். இறுதி முடிவை **Period-End Review** காட்டும்.
- extension | lane: approvers | col: 8 | icon: bi-calendar-plus | color: #dc2626 | kind: exception | from: review | to: track | screen: extension-review
  title: கூடுதல் நேரம் கேட்டல்
  status: Pending
  text: இலக்கை அடையவில்லை என்றால், SMM **Request Extension** அழுத்துவார். AVP அல்லது Admin அதற்கு ஒப்புதல் தருவார் அல்லது நிராகரிப்பார். ஒப்புதல் கிடைத்தால், scheme முடியும் தேதி மாறும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 154f433d358852fc
confidence: ok

- not-started | color: #64748b
  label: Not Started
  text: நிகழ்ச்சிக்கு ஒப்புதல் கிடைத்து, காத்திருக்கிறது. **Start Audit** அழுத்துங்கள்.
- draft | color: #94a3b8
  label: Draft
  text: தணிக்கை தொடங்கிவிட்டது. திட்டம் சேமிக்கப்பட்டுள்ளது, ஆனால் உறுதி செய்யப்படவில்லை.
- plan-confirmed | color: #2563eb
  label: Plan Confirmed
  text: திட்டம் பூட்டப்பட்டுவிட்டது. இனி அதன் கீழ் விற்பனையைச் சேமிக்கலாம்.
- in-progress | color: #d97706
  label: In Progress
  text: விற்பனை சேமிக்கப்பட்டு வருகிறது. இலக்கு இன்னும் எட்டப்படவில்லை.
- sales-tracking | color: #0d9488
  label: Sales Tracking
  text: scheme காலம் நடந்துகொண்டிருக்கிறது.
- period-expired | color: #ea580c
  label: Period Expired
  text: இலக்கை அடையும் முன்பே scheme காலம் முடிந்துவிட்டது.
- extension-request | color: #7c3aed
  label: Extension Request
  text: கூடுதல் நேரம் கேட்கப்பட்டுள்ளது. முடிவுக்காகக் காத்திருக்கிறது.
- completed | color: #16a34a
  label: Completed
  text: சேமித்த விற்பனை Required Turnover-ஐ எட்டிவிட்டது. தணிக்கை மூடப்பட்டது.
- ext-pending | color: #d97706
  label: Pending
  text: ஒரு நீட்டிப்புக் கோரிக்கை AVP அல்லது Admin-க்காகக் காத்திருக்கிறது.
- ext-pending-admin | color: #d97706
  label: Pending Admin Review
  text: உங்கள் நீட்டிப்புக் கோரிக்கை முடிவுக்காகக் காத்திருக்கும்போது tracking திரையில் இது தெரியும்.
- ext-approved | color: #16a34a
  label: Approved
  text: நீட்டிப்புக்கு ஒப்புதல் கிடைத்து, scheme முடியும் தேதி மாற்றப்பட்டது.
- ext-rejected | color: #dc2626
  label: Rejected
  text: நீட்டிப்பு மறுக்கப்பட்டது. scheme முடியும் தேதி மாறவில்லை.
- invoice-new | color: #0891b2
  label: New
  text: தணிக்கையில் இன்னும் சேமிக்கப்படாத SAP இன்வாய்ஸ் வரி.
- invoice-old | color: #475569
  label: Old
  text: ஏற்கனவே சேமிக்கப்பட்ட SAP இன்வாய்ஸ் வரி. இது ஒருபோதும் இரண்டு முறை கணக்கில் வராது.
- met | color: #16a34a
  label: Met
  text: திட்டமிட்ட மதிப்பு குறைந்தது Required Turnover அளவுக்கு உள்ளது.
- not-met | color: #dc2626
  label: Not Met
  text: திட்டமிட்ட மதிப்பு Required Turnover-ஐ விடக் குறைவு. இப்போதைக்குத் திட்டத்தை உறுதி செய்ய முடியாது.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: f676ec8395aa4ec8
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்கள் பணிப்பொறுப்புக்கான திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- owner | color: #2563eb
  title: நிகழ்ச்சிப் பொறுப்பாளர்
  - தணிக்கையைத் தொடங்கி விற்பனைத் திட்டம் போடுவது
  - டீலர்களை இணைத்துத் திட்டத்தை உறுதி செய்வது
  - தினசரி விற்பனையைச் சேமிப்பது
- data | color: #64748b
  title: விற்பனைத் தரவு அப்லோட்
  - SAP டீலர் விற்பனை Excel-ஐ அப்லோட் செய்வது
- managers | color: #0d9488 | roles: RM, SMM, SMD
  title: RM, SMM, SMD
  - உங்கள் பகுதியின் விற்பனைக் கண்காணிப்பைப் பார்ப்பது
  - SMM: இலக்கை அடையாதபோது நீட்டிப்பு கேட்பது
- approvers | color: #7c3aed | roles: AVP, Admin, SuperAdmin
  title: AVP மற்றும் Admin
  - நீட்டிப்புக் கோரிக்கைகளுக்கு ஒப்புதல் தருவது அல்லது நிராகரிப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: e0470c8f452a2a10
confidence: ok

- no-programme | popular: true
  q: என் நிகழ்ச்சி Sales Audit பட்டியலில் இல்லை
  a: இறுதி ஒப்புதல் பெற்ற CSR-2 நிகழ்ச்சிகளில், CSR-1-ல் நீங்கள் Responsible Person ஆக உள்ளவை மட்டுமே இந்தப் பட்டியலில் தெரியும். CSR-2 நிலையையும் Responsible Person-ஐயும் சரிபாருங்கள்.
- confirm-grey | popular: true
  q: Confirm Sales Plan பட்டன் சாம்பல் நிறத்தில் உள்ளது
  a: ஒரு scheme காலம், குறைந்தது ஒரு product, **Met** என்று காட்டும் **Requirement Status**, குறைந்தது ஒரு இணைக்கப்பட்ட டீலர் தேவை. விடுபட்டதை **Edit Sales Plan** அல்லது **Edit Dealer Mapping** மூலம் சரி செய்யுங்கள்.
- zero-new
  q: Save Daily Sales-ல் 0 New என்று காட்டுகிறது
  a: உங்கள் டீலர்கள், product-கள், scheme தேதிகளுக்குப் பொருந்தும் புதிய SAP இன்வாய்ஸ் வரிகள் எதுவும் இல்லை. சமீபத்திய SAP விற்பனையை **Dealer Sales List** திரையில் அப்லோட் செய்து, பிறகு **Refresh Sales** அழுத்துங்கள்.
- missing-id
  q: "Missing csr2Id" என்று வருகிறது
  a: இந்தத் திரைகளை **Sales Audit** அல்லது **Sales Tracking** பட்டியலில் இருந்துதான் திறக்க வேண்டும், சேமித்த லிங்கில் இருந்து அல்ல. பட்டியலுக்குத் திரும்பிப் போய் நிகழ்ச்சியை மீண்டும் திறங்கள்.
- extension-grey
  q: Request Extension பட்டன் சாம்பல் நிறத்தில் உள்ளது
  a: SMM மட்டுமே **Period-End Review** திரையில் இருந்து நீட்டிப்பு கேட்க முடியும்.
- upload-columns
  q: என் Excel அப்லோட் "Missing columns" என்று தோல்வியடைகிறது
  a: Excel-ன் முதல் வரிசையில் இந்தப் பெயர்கள் இருக்க வேண்டும்: Billing Document, Billing Date, Item, Customer Number, Customer Name, Product Code, Product Name, Quantity, Unit of Measure, Gross Amount.
:::

::: section audit-list
title: என் விற்பனைத் தணிக்கைகளைப் பார்ப்பது
routes: /SalesAudit
for: 
roles: 
designations: 
minutes: 2
source-hash: dc0f5ec250d5599d
confidence: ok

**இந்தத் திரை எதற்கு**
**Sales Audit** திரையில், நீங்கள் பொறுப்பாக உள்ள ஒப்புதல் பெற்ற CSR-2 நிகழ்ச்சிகள், அவற்றின் scheme காலம், turnover, நிலை ஆகியவற்றுடன் தெரியும். ஒவ்வொரு தணிக்கையையும் இங்கிருந்தே தொடங்கலாம் அல்லது தொடரலாம்.

1. **Filters**-ஐத் திறந்து **Sanction Number**, **Program Name**, **Location**, **Date Range** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
2. ஒவ்வொரு படியிலும் எத்தனை நிகழ்ச்சிகள் உள்ளன என்று பார்க்க **Total Programs**, **Not started**, **In Progress**, **Completed** போன்ற கார்டுகளைப் பாருங்கள்.
3. **Sales Audit List**-ல், ஒரு நிகழ்ச்சியைக் கண்டுபிடிக்கத் தேடல் பெட்டியில் டைப் செய்யுங்கள்.
4. ஒவ்வொரு வரிசையிலும் **Scheme Period**, **Required TO (Auto)**, **Actual Turnover**, **Achievement** ஆகியவற்றைச் சரிபாருங்கள்.
5. **Status** நெடுவரிசையில் உள்ள பட்டனை அழுத்துங்கள்: புதிய நிகழ்ச்சிக்கு **Start Audit**, நீங்கள் தொடங்கியதற்கு **View / Continue**, அல்லது **View Achievement**, **Request Extension**, **View Request**, **View Audit**.

## பொதுவான பிரச்சனைகள்

- **"No eligible sales audit records found".** (தகுதியான விற்பனைத் தணிக்கைப் பதிவுகள் இல்லை) இறுதி ஒப்புதல் பெற்ற CSR-2 நிகழ்ச்சிகளில், நீங்கள் Responsible Person ஆக உள்ளவை மட்டுமே இங்கே தெரியும்.
- **தேதி ஃபில்டர் எதுவும் செய்யவில்லை.** தேதிகளைத் தேர்வு செய்து, தேதிப் பெட்டியில் **Done** அழுத்தி, பிறகு **Apply Filters** அழுத்துங்கள்.
- **Scheme Period-ல் "Not Set" என்று உள்ளது.** விற்பனைத் திட்டம் இன்னும் போடப்படவில்லை. **Start Audit** அல்லது **View / Continue** அழுத்துங்கள்.
:::

::: section audit-overview
title: நிகழ்ச்சியையும் பட்ஜெட்டையும் சரிபார்ப்பது
routes: /AuditOverview
for: 
roles: 
designations: 
minutes: 2
source-hash: c767964e677164ff
confidence: ok

**இந்தத் திரை எதற்கு**
**Audit Overview** என்பது படி 1 (**Program**). ஒப்புதல் பெற்ற CSR-2 மற்றும் அதனுடன் இணைந்த CSR-1-ன் விவரங்களையும், **Required Turnover**-ஐ முடிவு செய்யும் பட்ஜெட்டையும் இது காட்டும்.

1. **Sales Audit** திரையில், ஒரு நிகழ்ச்சியில் **Start Audit** அல்லது **View / Continue** அழுத்துங்கள்.
2. மேலே உள்ள பட்டையைச் சரிபாருங்கள்: **Program Type**, **Program Name**, **Sanction Number**, **CSR-2 Status**.
3. **Program Information**-ஐச் சரிபாருங்கள்: மாநிலம், மண்டலம், HQ, CSR-2 நிகழ்ச்சித் தேதி, scheme தேதிகள்.
4. **Program Budget & Turnover**-ஐச் சரிபாருங்கள். **Required Turnover**, **Total Approved Budget**-ல் இருந்து கணக்கிடப்படும்.
5. தணிக்கையைத் தொடங்க **Save Draft** அழுத்துங்கள், அல்லது படி 2-க்குப் போக **Continue to sales plan** அழுத்துங்கள்.

> Tip: விதி பட்ஜெட் கார்டில் காட்டப்பட்டிருக்கும்: ஒவ்வொரு ₹1.50 Lakh பட்ஜெட்டுக்கும் ₹1.00 Cr turnover.

## பொதுவான பிரச்சனைகள்

- **"Missing csr2Id".** (csr2Id இல்லை) **Sales Audit** பட்டியலில் இருந்து **Start Audit** மூலம் இந்தத் திரையைத் திறங்கள்.
- **"Access denied (403)".** (அனுமதி இல்லை) இந்த நிகழ்ச்சிக்கு நீங்கள் Responsible Person அல்ல.
- **Save Draft சாம்பல் நிறத்தில் உள்ளது.** தணிக்கை ஏற்கனவே இறுதியாகச் சமர்ப்பிக்கப்பட்டுவிட்டது.
:::

::: section sales-plan
title: விற்பனைத் திட்டம் போடுவது
routes: /SalesPlan
for: 
roles: 
designations: 
tour: 1 > [data-help=st-plan-period] | 2 > [data-help=st-plan-add] | 3 > [data-help=st-plan-table] | 4 > [data-help=st-plan-summary] | 5 > [data-help=st-plan-actions]
minutes: 4
source-hash: 2570b597b9143eea
confidence: ok

**இந்தத் திரை எதற்கு**
**Sales Plan & Scheme Period** என்பது படி 2. scheme தேதிகளையும், விற்க நினைக்கும் product, அளவு, மதிப்பையும் இங்கே அமையுங்கள்.

1. **Scheme Period**-ன் கீழ், **From Date** மற்றும் **To Date** தேர்வு செய்யுங்கள். **Duration** மாதங்களையும் நாட்களையும் காட்டும்.
2. ஒரு வரிசையைச் சேர்க்க **+ Add Product** அழுத்துங்கள்.
3. ஒவ்வொரு வரிசையிலும் **Product Group** மற்றும் **Product** தேர்வு செய்து, **Planned Quantity** மற்றும் **Planned Value (₹)** உள்ளிடுங்கள். குப்பைத்தொட்டி ஐகான் ஒரு வரிசையை நீக்கும்.
4. **Plan Summary & Comparison**-ஐச் சரிபாருங்கள். **Requirement Status**-ல் **Met** என்று காட்ட வேண்டும்: **Total Planned Commitment** குறைந்தது **Required Turnover** அளவுக்கு இருக்க வேண்டும்.
5. உங்கள் வேலையை வைத்துக்கொள்ள **Save Draft** அழுத்துங்கள், அல்லது சேமித்து **Dealers** படிக்குப் போக **Save & Continue** அழுத்துங்கள்.

> Careful: திட்டம் உறுதி செய்யப்பட்ட பிறகு, இந்தத் திரை பூட்டப்படும். அப்போது பட்டனில் **Continue to Dealer Mapping** என்று இருக்கும்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **From Date**, **To Date** | scheme தொடங்கும் மற்றும் முடியும் தேதி. முடியும் தேதி தொடக்கத்துக்கு முன் இருக்கக் கூடாது. |
| **Product Group** | இதை முதலில் தேர்வு செய்யுங்கள். பிறகு **Product** பட்டியலில் அதன் product-கள் தெரியும். |
| **Product** | ஒவ்வொரு product-ம் ஒரு முறை மட்டுமே. |
| **Unit** | எப்போதும் MT. |
| **Planned Quantity** | திட்டமிட்ட விற்பனை, MT-ல். எதிர்மறை (minus) கூடாது. |
| **Planned Value (₹)** | திட்டமிட்ட விற்பனை மதிப்பு, ரூபாயில். எதிர்மறை (minus) கூடாது. |

## பொதுவான பிரச்சனைகள்

- **"Select a product and enter non-negative quantities and values for every row."** (ஒவ்வொரு வரிசையிலும் product தேர்வு செய்து, minus இல்லாத அளவும் மதிப்பும் உள்ளிடுங்கள்) ஒவ்வொரு வரிசையையும் நிரப்புங்கள், அல்லது காலியான வரிசைகளைக் குப்பைத்தொட்டி ஐகானால் நீக்குங்கள்.
- **"The same product cannot appear more than once."** (ஒரே product ஒன்றுக்கு மேல் வரக் கூடாது) திரும்ப வந்த வரிசையை நீக்குங்கள்.
- **"Scheme end date must not precede the start date."** (scheme முடியும் தேதி தொடக்கத் தேதிக்கு முன் இருக்கக் கூடாது) From Date அன்றோ அதற்குப் பிறகோ வரும் To Date-ஐத் தேர்வு செய்யுங்கள்.
- **Requirement Status-ல் Not Met என்று காட்டுகிறது.** மொத்தம் Required Turnover-ஐ எட்டும் வரை product-களைச் சேருங்கள் அல்லது திட்டமிட்ட மதிப்புகளை உயர்த்துங்கள்.
:::

::: section plan-review
title: திட்டத்தைச் சரிபார்த்து உறுதி செய்வது
routes: /PlanReview
for: 
roles: 
designations: 
tour: 1 > [data-help=st-review-summary] | 2 > [data-help=st-review-budget] | 3 > [data-help=st-review-plan] | 4 > [data-help=st-review-status] | 5 > [data-help=st-review-actions]
minutes: 3
source-hash: ee688fac1f288bce
confidence: ok

**இந்தத் திரை எதற்கு**
**Review & Confirm Plan** என்பது படி 4. நிகழ்ச்சி, பட்ஜெட், திட்டம், டீலர்களைச் சரிபார்த்து, பிறகு திட்டத்தை உறுதி செய்யுங்கள். ஒரு முறை மட்டுமே உறுதி செய்வீர்கள்; அதன் பிறகு விற்பனைக் கண்காணிப்பு தொடங்கும்.

1. மேலே உள்ள பட்டையையும் (**Program Type**, **Program Name**, **Scheme Period**, **Scheme Duration**) அதன் கீழ் உள்ள **Program Information**-ஐயும் சரிபாருங்கள்.
2. **Budget Details**-ஐச் சரிபாருங்கள்: **CSR1 Approved Budget**, **Additional Approved Budget**, **Total Approved Budget**, **Required Turnover**.
3. **Product - wise Sales Plan & Summary**-ஐச் சரிபாருங்கள். திட்டத்தை மாற்ற **Edit Sales Plan** அழுத்துங்கள்.
4. **Sales Achievement & Dealer Allocation**-ல், **Requirement Status**-ல் **Met** என்று காட்ட வேண்டும், **Mapped Dealers** 0-ஐ விட அதிகமாக இருக்க வேண்டும். டீலர்களை மாற்ற **Edit Dealer Mapping** அழுத்துங்கள்.
5. **Confirm Sales Plan** அழுத்தி, பிறகு **Yes, Confirm & Save** அழுத்துங்கள். **Sales Plan Confirmed** பெட்டியில் **View Tracking →** அல்லது **Back to Sales Audit** அழுத்துங்கள்.

> Careful: உறுதி செய்த பிறகு, திட்டத்தையோ scheme தேதிகளையோ டீலர்களையோ மாற்ற முடியாது.

## பொதுவான பிரச்சனைகள்

- **Confirm Sales Plan சாம்பல் நிறத்தில் உள்ளது.** ஒரு scheme காலத்தைச் சேமியுங்கள், **Requirement Status**-ல் **Met** என்று காட்டுவதை உறுதி செய்யுங்கள், குறைந்தது ஒரு டீலரை இணையுங்கள்.
- **"Missing csr2Id. Open Plan Review from Dealer Mapping."** (csr2Id இல்லை. Dealer Mapping-ல் இருந்து Plan Review-ஐத் திறங்கள்) **Sales Audit** பட்டியலில் இருந்து நிகழ்ச்சியை மீண்டும் திறங்கள்.
- **நான் இப்போது நிறுத்திவிட்டுப் பிறகு முடிக்க வேண்டும்.** **Save Draft** அழுத்துங்கள். உங்கள் திட்டமும் டீலர்களும் அப்படியே இருக்கும்.
:::

::: section daily-sales
title: தினசரி விற்பனையைத் தணிக்கையில் சேமிப்பது
routes: /PlanReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 3c54cfd763cbb61e
confidence: ok

**இந்தத் திரை எதற்கு**
திட்டம் உறுதி செய்யப்பட்ட பிறகு, புதிய SAP விற்பனையைச் சாதனையில் (achievement) சேர்க்க **Review & Confirm Plan** திரைக்குத் திரும்பி வாருங்கள். புதிய விற்பனை அப்லோட் ஆகும் ஒவ்வொரு முறையும் இதைச் செய்யுங்கள்.

1. சமீபத்திய SAP டீலர் விற்பனை **Dealer Sales List** திரையில் அப்லோட் செய்யப்பட்டுள்ளதா என்று உறுதி செய்யுங்கள்.
2. அந்த நிகழ்ச்சிக்கான **Review & Confirm Plan**-ஐத் திறங்கள். **Sales Tracking Dashboard**-ல் இருந்து **Back to Plan Review** அழுத்தலாம்.
3. **Matched Dealer Invoice Details**-ல் **Refresh Sales** அழுத்துங்கள். ஒவ்வொரு இன்வாய்ஸ் வரியிலும் **New** (இன்னும் சேமிக்கப்படவில்லை) அல்லது **Old** (ஏற்கனவே சேமிக்கப்பட்டது) என்று காட்டும்.
4. கீழே உள்ள **Save Daily Sales** (எத்தனை New என்று அது காட்டும்) அல்லது மேலே உள்ள **Save Sales Progress** அழுத்தி, பிறகு **Yes, Save Daily Sales** அழுத்துங்கள்.
5. **Daily Sales Saved Successfully** பெட்டியில் சேமித்த இன்வாய்ஸ்களும் **Cumulative Achievement**-ம் தெரியும். **Continue** அல்லது **Back to Sales Audit** அழுத்துங்கள்.
6. சேமித்த விற்பனை Required Turnover-ஐ எட்டியதும், பட்டனில் **Audit Completed** என்று காட்டும், தணிக்கை மூடப்படும்.

> Tip: Old இன்வாய்ஸ் வரிகள் ஒருபோதும் இரண்டு முறை கணக்கில் வராது, அதனால் மீண்டும் சேமிப்பது பாதுகாப்பானதுதான்.

## பொதுவான பிரச்சனைகள்

- **Save Daily Sales சாம்பல் நிறத்தில் உள்ளது அல்லது 0 New என்று காட்டுகிறது.** புதிய இன்வாய்ஸ் வரிகள் எதுவும் இல்லை. அடுத்த SAP விற்பனையை அப்லோட் செய்து **Refresh Sales** அழுத்துங்கள்.
- **"SAP products excluded because no unambiguous planned-product name matched".** (திட்டத்தில் உள்ள product பெயருடன் தெளிவாகப் பொருந்தாததால் SAP product-கள் விடப்பட்டன) SAP product பெயர் உங்கள் திட்டத்தில் உள்ள ஒரு product-உடன் பொருந்த வேண்டும்.
- **சில இன்வாய்ஸ்கள் கணக்கில் வரவில்லை.** scheme காலத்துக்கு வெளியே உள்ள வரிகளும், MT அல்லாத வேறு unit உள்ள வரிகளும் விடப்படும்.
:::

::: section dealer-sales-upload
title: SAP டீலர் விற்பனையை அப்லோட் செய்வது
routes: /DealerSalesUpload
for: 
roles: 
designations: 
tour: 1 > [data-help=st-upload-btn] | 2 > [data-help=st-upload-btn] | 3 > [data-help=st-upload-search] | 4 > [data-help=st-upload-table] | 5 > [data-help=st-upload-export] | 6 > [data-help=st-upload-footer]
minutes: 3
source-hash: 7a499220079c04f2
confidence: ok

**இந்தத் திரை எதற்கு**
**Dealer Sales List**-ல் SAP-ல் இருந்து வந்த டீலர் இன்வாய்ஸ் வரிகள் இருக்கும். SAP Excel-ஐ இங்கே அப்லோட் செய்யுங்கள்; விற்பனைத் தணிக்கைகள் இந்த வரிகளை வைத்துச் சாதனையைக் கணக்கிடும்.

1. **Bulk Upload** அழுத்தி SAP Excel கோப்பைத் தேர்வு செய்யுங்கள் (.xlsx அல்லது .xls, 5 MB வரை).
2. செய்தியைப் படியுங்கள்: **New records**, **Already imported**, **Repeated in Excel**. **Upload errors** அட்டவணை வந்தால், அந்த வரிசைகளை Excel-ல் திருத்தி மீண்டும் அப்லோட் செய்யுங்கள்.
3. வரிகளைக் கண்டுபிடிக்க, தேடல் பெட்டியில் இன்வாய்ஸ், டீலர் அல்லது product-ஐ டைப் செய்யுங்கள்.
4. வரிசைகளைச் சரிபாருங்கள்: **Billing Date**, **Billing Document**, **Dealer Code**, **Dealer Name**, **Product Name**, **Quantity**, **Unit**, **Gross Amount (₹)**.
5. பட்டியலை வெளியே எடுக்க **Copy**, **CSV**, **Excel**, **PDF** அல்லது **Print** பயன்படுத்துங்கள்.
6. கீழே **Items Per Page** தேர்வு செய்து, பக்கங்களுக்கு இடையில் நகருங்கள், மொத்த **Gross sales**-ஐப் பாருங்கள்.

> Tip: அதே கோப்பை மீண்டும் அப்லோட் செய்வது பாதுகாப்பானதுதான். ஏற்கனவே இறக்குமதியான வரிகள் விடப்படும்.

## Excel நெடுவரிசைகள்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Billing Document**, **Item** | இன்வாய்ஸ் எண்ணும் வரி எண்ணும். கட்டாயம். |
| **Billing Date** | இன்வாய்ஸ் தேதி. சரியான தேதியாக இருக்க வேண்டும். |
| **Customer Number**, **Customer Name** | டீலர் குறியீடும் டீலர் பெயரும். கட்டாயம். |
| **Product Code**, **Product Name** | கட்டாயம். தணிக்கையில் கணக்கில் வர, பெயர் திட்டமிட்ட product-உடன் பொருந்த வேண்டும். |
| **Quantity**, **Unit of Measure**, **Gross Amount** | எதிர்மறை (minus) கூடாது. MT வரிகள் மட்டுமே தணிக்கையில் கணக்கில் வரும். |

## பொதுவான பிரச்சனைகள்

- **"Missing columns".** (நெடுவரிசைகள் இல்லை) Excel-ன் முதல் வரிசையில் மேலே உள்ள எல்லா நெடுவரிசைப் பெயர்களும் இருக்க வேண்டும்.
- **"Excel validation failed; no rows saved."** (Excel சரிபார்ப்பு தோல்வி; எந்த வரிசையும் சேமிக்கப்படவில்லை) **Upload errors** அட்டவணையைப் (**Excel Row**, **Field**, **Reason**) படித்து, அந்த வரிசைகளைத் திருத்தி மீண்டும் அப்லோட் செய்யுங்கள்.
- **"Maximum 10,000 invoice rows allowed."** (அதிகபட்சம் 10,000 இன்வாய்ஸ் வரிசைகள் மட்டுமே) கோப்பைச் சிறிய கோப்புகளாகப் பிரியுங்கள்.
:::

::: section sales-tracking
title: இலக்குக்கு எதிராக விற்பனையைப் பின்தொடர்வது
routes: /SalesTracking
for: 
roles: 
designations: 
minutes: 3
source-hash: 409f69e798e447e9
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரு நிகழ்ச்சியின் Required Turnover-ல் எவ்வளவு எட்டப்பட்டுள்ளது என்று **Sales Tracking Dashboard** காட்டும். நிகழ்ச்சிப் பொறுப்பாளர் திட்டத்தை உறுதி செய்த பிறகு இதைத் திறப்பார். மேலாளர்கள் **Sales Tracking** பட்டியலில் இருந்து இதைப் பார்க்க மட்டும் (read-only) திறப்பார்கள்.

1. திட்டத்தை உறுதி செய்த பிறகு **View Tracking →** மூலம், அல்லது **Sales Tracking** பட்டியலில் **View** மூலம் இதைத் திறங்கள்.
2. மேலே உள்ள கட்டங்களைப் படியுங்கள்: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %**, **Remaining to Achieve**, **Tracking Status**.
3. **Extension Request** பெட்டி வந்தால், அதன் நிலையைப் படியுங்கள். காரணத்தையும் குறிப்புகளையும் பார்க்க **View Request Status** அழுத்துங்கள்.
4. **Turnover Achievement Overview** மற்றும் **Monthly Progress Trend**-ஐப் பாருங்கள். scheme 7 நாட்களுக்குள் முடியும் என்றால் ஒரு எச்சரிக்கை வரும்.
5. product அட்டவணையையும் டீலர் அட்டவணைகளையும் சரிபாருங்கள். **Dealer Sales Achievement - SAP Invoice Matches**-ல், இன்வாய்ஸ் வரிகளைக் காட்ட அல்லது மறைக்க **View Details** அல்லது **Hide Details** அழுத்துங்கள்.
6. நிகழ்ச்சிப் பொறுப்பாளர் **Import actual sales achievements (SAP invoice report)**-ன் கீழ் ஒரு SAP இன்வாய்ஸ் ரிப்போர்ட்டை அப்லோட் செய்யலாம். புதிய அப்லோட் முந்தையதை மாற்றிவிடும்.
7. Excel நகல் வேண்டுமென்றால் **Download Tracking Report** அழுத்துங்கள், அல்லது **Period-End Review**-ஐத் திறக்க **Go To Final Review** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Awaiting Sales Data".** (விற்பனைத் தரவுக்காகக் காத்திருக்கிறது) இன்னும் எந்தச் சாதனையும் சேமிக்கப்படவில்லை. **Review & Confirm Plan** திரையில் தினசரி விற்பனையைச் சேமியுங்கள்.
- **இறக்குமதிப் பெட்டி இல்லை.** read-only பார்வையில், திட்டம் உறுதி செய்யப்படும் முன், தணிக்கை இறுதியாகச் சமர்ப்பிக்கப்பட்ட பிறகு அது மறைக்கப்படும்.
- **"Upload an XLSX or XLS file up to 5 MB."** (5 MB வரையிலான XLSX அல்லது XLS கோப்பை அப்லோட் செய்யுங்கள்) சிறிய Excel கோப்பைத் தேர்வு செய்யுங்கள்.
- **Go To Final Review சாம்பல் நிறத்தில் உள்ளது.** இன்னும் எந்த product சாதனையும் சேமிக்கப்படவில்லை.
:::

::: section tracking-list
title: என் பகுதியின் விற்பனைக் கண்காணிப்பைப் பார்ப்பது
routes: /SalesTrackingList
for: 
roles: 
designations: 
minutes: 2
source-hash: e2e94d6371a53595
confidence: ok

**இந்தத் திரை எதற்கு**
**Sales Tracking** மெனு உங்கள் பகுதியின் நிகழ்ச்சிகளின் பட்டியலைப் பார்க்க மட்டும் (read-only) திறக்கும். RM மண்டலத்தையும், SMM HQ-வையும், SMD மாநிலத்தையும் பார்ப்பார்கள், Admin எல்லாவற்றையும் பார்ப்பார்.

1. **Filters**-ஐத் திறந்து **Sanction Number**, **Program Name**, **Location**, **Date Range** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
2. ஒரு நிகழ்ச்சியைக் கண்டுபிடிக்கத் தேடல் பெட்டியில் டைப் செய்யுங்கள்.
3. ஒவ்வொரு வரிசையிலும் **Scheme Period**, **Required TO (Auto)**, **Actual Turnover**, **Achievement**, **Document Status** ஆகியவற்றைச் சரிபாருங்கள்.
4. அந்த நிகழ்ச்சியின் **Sales Tracking Dashboard**-ஐத் திறக்க **View** அழுத்துங்கள்.
5. பக்கங்களுக்கு இடையில் நகர **Items Per Page**-ஐயும் அம்புக்குறிகளையும் பயன்படுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **role-க்கு அனுமதி இல்லை அல்லது பகுதி ஒதுக்கப்படவில்லை என்று பிழை வருகிறது.** உங்கள் லாகினுக்கு மண்டலம், HQ அல்லது மாநிலத்துடன் RM, SMM அல்லது SMD role தேவை. நிர்வாகியிடம் (administrator) கேளுங்கள்.
- **"From date cannot be after To date."** (From தேதி To தேதிக்குப் பிறகு இருக்கக் கூடாது) தேதி வரம்பைச் சரி செய்யுங்கள்.
- **"No accessible Sales Audit records found."** (பார்க்கக்கூடிய Sales Audit பதிவுகள் இல்லை) **Clear All** அழுத்துங்கள். உங்கள் பகுதியில் இதுவரை ஒப்புதல் பெற்ற நிகழ்ச்சி எதுவும் இல்லாமல் இருக்கலாம்.
:::

::: section period-end-review
title: scheme முடிவில் முடிவைச் சரிபார்ப்பது
routes: /PeriodEndReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 93280350556420ae
confidence: ok

**இந்தத் திரை எதற்கு**
**Period-End Review** என்பது கண்காணிப்பின் கடைசிப் படி. நிகழ்ச்சி தன் **Required Turnover**-ஐ எட்டியதா என்று காட்டும், எட்டவில்லை என்றால் SMM கூடுதல் நேரம் கேட்க உதவும்.

1. **Sales Tracking Dashboard**-ல் **Go To Final Review** அல்லது **View Final Review** அழுத்துங்கள்.
2. **Program Summary**-ஐயும் **Scheme period ends on**-ல் உள்ள தேதியையும் சரிபாருங்கள்.
3. **Final Program Summary**-ல் **Required Turnover (TO)**, **Planned Commitment**, **Actual Turnover Achieved**, **Balance to Target**, **Achievement** ஆகியவற்றைப் படியுங்கள்.
4. **Target Outcome & Next Step**-ல் **Target Achieved** அல்லது **Target Not Achieved** என்று காட்டும்.
5. இலக்கை அடையவில்லை என்றால், SMM **Request Extension** அழுத்துவார். இது காரணத்தையும் புதிய முடியும் தேதியையும் தர **Request Extension** திரையைத் திறக்கும்.
6. வெளியேற **Back to Sales Tracking**, **Close** அல்லது **Back to List** அழுத்துங்கள்.

> Tip: இந்தத் திரை பார்த்துச் சரிபார்க்க மட்டுமே. இது scheme-ஐ மூடாது.

## பொதுவான பிரச்சனைகள்

- **Request Extension சாம்பல் நிறத்தில் உள்ளது.** SMM மட்டுமே நீட்டிப்பு கேட்க முடியும்.
- **Proceed to Final Review மற்றும் Go To Final Review சாம்பல் நிறத்தில் உள்ளன.** இறுதிச் சரிபார்ப்புப் படி இன்னும் கிடைக்கவில்லை.
- **"Missing CSR2 ID".** (CSR2 ID இல்லை) இந்தத் திரையை **Sales Tracking**-ல் இருந்து திறங்கள்.
:::

::: section audit-details
title: திட்டம் மற்றும் சாதனைத் தாளை நிரப்புவது
routes: /SalesAuditDetails
for: 
roles: 
designations: 
minutes: 3
source-hash: 609a8329530a3911
confidence: ok

**இந்தத் திரை எதற்கு**
**Sales Audit — Plan & Achievement** என்பது scheme தேதிகள், பட்ஜெட், திட்டம், உண்மையான விற்பனை எல்லாம் உள்ள ஒரே தாள். **Sales Audit** பட்டியல் இதை **View Achievement** அல்லது **Request Extension** மூலம் திறக்கும்.

1. **Sales Audit** திரையில், **Status** நெடுவரிசையில் **View Achievement** அல்லது **Request Extension** அழுத்துங்கள்.
2. **CSR-2 Auto-Filled Program Information**-ஐச் சரிபாருங்கள்.
3. **Scheme Period & Budget**-ன் கீழ், **Scheme From**, **Scheme To**, **Additional Approved Budget (₹)** ஆகியவற்றைச் சரிபாருங்கள். **Required Turnover (1.5×)** மற்றும் **Audit Status** கீழே காட்டப்படும்.
4. **P — Sales Plan / Committee (Product-wise Breakup)**-ன் கீழ், **+ Add Product** அழுத்தி product, அளவு, மதிப்பை உள்ளிடுங்கள். **Remove** ஒரு வரிசையை நீக்கும்.
5. **SP — Sales / Achievement**-ன் கீழ், உண்மையான விற்பனையை அதே முறையில் சேருங்கள். **Actual** மற்றும் **Achievement** மொத்தங்களைக் காட்டும்.
6. உங்கள் வேலையை வைத்துக்கொள்ள **Save Draft** அழுத்துங்கள், அல்லது தணிக்கையை மூட **Final Submit** அழுத்துங்கள்.

> Careful: **Final Submit**-க்குப் பிறகு தாளைப் பார்க்க மட்டுமே முடியும்.
> Careful: **Additional Approved Budget**-ஐ, அதன் தனி ஒப்புதலைச் சரிபார்த்த பிறகே உள்ளிடுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Complete scheme dates and product plan meeting the required target."** (இலக்கை எட்டும் scheme தேதிகளையும் product திட்டத்தையும் முழுமையாக்குங்கள்) Final Submit-க்கு இரண்டு தேதிகளும், குறைந்தது ஒரு திட்ட வரிசையும், Required Turnover அளவுக்காவது திட்ட மற்றும் உண்மையான மொத்தங்களும் தேவை.
- **"Invalid scheme period."** (தவறான scheme காலம்) Scheme To, Scheme From-க்கு முன் இருக்கக் கூடாது.
- **"Invalid CSR-2 ID".** (தவறான CSR-2 ID) இந்தத் திரையை **Sales Audit** பட்டியலில் இருந்து திறங்கள்.
:::

::: section extension-list
title: நீட்டிப்புக் கோரிக்கைகளைப் பார்ப்பது
routes: /ExtensionRequests
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: b342a221494b9432
confidence: ok

**இந்தத் திரை எதற்கு**
ஒரு நிகழ்ச்சி இலக்கை அடையாதபோது கூடுதல் நேரம் கேட்டு வந்த கோரிக்கைகளை **Extension Request List** காட்டும். AVP மற்றும் Admin அவற்றை இங்கே சரிபார்ப்பார்கள்.

1. கார்டுகளைப் பாருங்கள்: **Total Request**, **Pending Request**, **Rejected**, **Approved**.
2. **Filters**-ஐத் திறந்து **Sanction Number**, **Program Type**, **Location**, **Request On** அல்லது **Status** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
3. ID, sanction எண், நிகழ்ச்சி, இடம் அல்லது பெயர் மூலம் கோரிக்கையைக் கண்டுபிடிக்கத் தேடல் பெட்டியில் டைப் செய்யுங்கள்.
4. ஒவ்வொரு வரிசையிலும் **Requested By**, **Scheme Period**, **Request New Date**, **Achievement** ஆகியவற்றைச் சரிபாருங்கள்.
5. ஒரு கோரிக்கையைத் திறக்க **View Request** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"You do not have permission to view extension requests."** (நீட்டிப்புக் கோரிக்கைகளைப் பார்க்க உங்களுக்கு அனுமதி இல்லை) AVP மற்றும் Admin மட்டுமே இந்தப் பட்டியலைத் திறக்க முடியும்.
- **"No records found".** (பதிவுகள் இல்லை) Filters-ல் **Clear All** அழுத்துங்கள்.
:::

::: section extension-review
title: நீட்டிப்புக்கு ஒப்புதல் தருவது அல்லது நிராகரிப்பது
routes: /ExtensionRequestDetails
for: 
roles: AVP, Admin, SuperAdmin
designations: 
minutes: 2
source-hash: 38c09035527b95d4
confidence: ok

**இந்தத் திரை எதற்கு**
**Extension Request Details** ஒரு கோரிக்கையை, நிகழ்ச்சியின் செயல்பாடு மற்றும் கூடுதல் நேரத்துக்கான காரணத்துடன் காட்டும். இங்கே ஒப்புதல் தரலாம் அல்லது நிராகரிக்கலாம்.

1. **Extension Request List**-ல் **View Request** அழுத்துங்கள்.
2. தலைப்புப் பகுதியைச் சரிபாருங்கள்: நிலை, **Request ID**, **Request On**.
3. **Scheme Performance**-ஐப் படியுங்கள்: **Required Turnover (TO)**, **Actual Turnover**, **Achievement %**, **Remaining to Achieve**.
4. **Extension Request**-ஐப் படியுங்கள்: **Current Scheme Period**, **Request New End Date**, **Requested By**, காரணம், குறிப்புகள்.
5. **Dealer-wise Actual Sales**-ஐச் சரிபாருங்கள்.
6. **Pending** கோரிக்கைக்கு, **Approve Extension** அல்லது **Reject Request** அழுத்துங்கள்.
7. **Extension Approved Successfully** அல்லது **Extension Rejected Successfully** திறக்கும். பட்டியலுக்குத் திரும்ப **View Request Details** அழுத்துங்கள்.

> Careful: பட்டன்கள் உடனே செயல்படும். இரண்டாவது உறுதிப் பெட்டி இல்லை. ஒப்புதல் தந்தால் scheme முடியும் தேதி மாறும்.

## பொதுவான பிரச்சனைகள்

- **Approve அல்லது Reject பட்டன்கள் இல்லை.** கோரிக்கை ஏற்கனவே **Approved** அல்லது **Rejected** ஆகிவிட்டது.
- **"This request is already approved or the scheme end date has changed. Reload to review it."** (இந்தக் கோரிக்கைக்கு ஏற்கனவே ஒப்புதல் கிடைத்துவிட்டது அல்லது scheme முடியும் தேதி மாறிவிட்டது. மீண்டும் லோட் செய்து சரிபாருங்கள்) பக்கத்தை மீண்டும் லோட் செய்து மறுபடி சரிபாருங்கள்.
- **"Request not found".** (கோரிக்கை கிடைக்கவில்லை) பட்டியலில் இருந்து கோரிக்கையை மீண்டும் திறங்கள்.
:::
