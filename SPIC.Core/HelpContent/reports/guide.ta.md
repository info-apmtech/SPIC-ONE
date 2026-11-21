---
module: reports
title: அறிக்கைகள்
icon: bi-bar-chart
order: 116
language: ta
category: அறிக்கைகள்
note: புதிய வழிகாட்டி
meta-source-hash: c3d58c99d0724041
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 0cd39d62cdd793e3
confidence: ok

முக்கிய எண்ணிக்கைகளை ஒரே இடத்தில் பாருங்கள். **Admin Dashboard**-ல் நலத்திட்ட விண்ணப்பங்கள், டீலர்கள், கெஸ்ட் ஹவுஸ் புக்கிங்குகள் தெரியும். **Reports Center**-ல் ரிப்போர்ட்டுகளின் பட்டியல் தெரியும்.

- dashboard | icon: bi-speedometer2 | color: #1d4ed8
  title: Admin Dashboard
  text: நலத்திட்ட விண்ணப்பங்கள், டீலர்கள், கெஸ்ட் ஹவுஸ் எண்ணிக்கைகள் ஒரே பக்கத்தில்.
- queue | icon: bi-list-check | color: #d97706
  title: காத்திருக்கும் வரிசை
  text: எந்த விண்ணப்பங்கள் காத்திருக்கின்றன, எந்த நிலையில், எத்தனை நாட்களாக என்று பாருங்கள்.
- reports | icon: bi-file-earmark-bar-graph | color: #7c3aed
  title: Reports Center
  text: ரிப்போர்ட்டுகளின் பட்டியல். இது இன்னும் உருவாக்கப்பட்டு வருகிறது, அதனால் இதில் தெரியும் ரிப்போர்ட்டுகள் மாதிரிகள் மட்டுமே.
:::

::: section flow
title: இந்தத் திரைகளை எப்படிப் பயன்படுத்துவது
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 7963de51aaa7ddbb
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. உங்கள் வரிசையில் "You" (நீங்கள்) என்று குறிக்கப்பட்டிருக்கும். ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- office | type: lane | color: #1d4ed8 | roles: Admin, CorporateAdmin, Director
  title: SDWA Admin மற்றும் அலுவலகம்
  sub: SDWA Admin · Admin · Director
- approvers | type: lane | color: #d97706 | roles: MO, RM, SMM
  title: ஒப்புதல் தருபவர்கள்
  sub: MO · RM · SM · SDWA · SDWA Admin
- open | lane: office | col: 1 | icon: bi-speedometer2 | color: #1d4ed8 | next: check | screen: admin-dashboard
  title: Admin Dashboard-ஐத் திறத்தல்
  text: மெனுவில் **SDWA**-ஐத் திறந்து **Admin Dashboard** தேர்வு செய்யுங்கள்.
- check | lane: office | col: 2 | icon: bi-list-check | color: #d97706 | next: act, reports | screen: admin-dashboard
  title: எண்ணிக்கைகளையும் வரிசையையும் சரிபார்த்தல்
  text: கார்டுகள், **Approval Workflow Overview**, **Pending Action Queue (SDWA)** ஆகியவற்றைப் பாருங்கள்.
- act | lane: approvers | col: 3 | icon: bi-patch-check | color: #16a34a
  title: Scheme Approval திரையில் ஒப்புதல் தருதல்
  text: **Quick actions**-ன் கீழ் உள்ள **Review Pending Approvals** அழுத்துங்கள். ஒப்புதல் தருவது **Scheme Approval** திரையில்தான், டாஷ்போர்டில் அல்ல.
- reports | lane: office | col: 3 | icon: bi-file-earmark-bar-graph | color: #7c3aed | screen: reports-center
  title: ரிப்போர்ட் பட்டியலைப் பார்த்தல்
  text: ரிப்போர்ட்டுகளைப் பார்க்க **Reports Center**-ஐத் திறங்கள். இது இன்னும் உருவாக்கப்பட்டு வருகிறது.
- no-data | lane: office | col: 2 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: check | to: open | screen: admin-dashboard
  title: டேட்டா லோட் ஆகவில்லை
  text: மேலே ஒரு சிவப்புப் பெட்டியில் **Could not load** என்று வரும், பெட்டிகளில் **—** தெரியும். டாஷ்போர்டைப் பிறகு மீண்டும் திறங்கள்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 9575d21878652ad1
confidence: ok

- pending-mo | color: #d97706
  label: Pending MO
  text: சரிபார்ப்பதற்காக MO-இடம் உள்ளது.
- pending-rm | color: #d97706
  label: Pending RM
  text: சரிபார்ப்பதற்காக RM-இடம் உள்ளது.
- pending-sm | color: #d97706
  label: Pending SM
  text: சரிபார்ப்பதற்காக SM-இடம் உள்ளது.
- pending-sdwa | color: #7c3aed
  label: Pending SDWA
  text: ஒப்புதலுக்காக SDWA-இடம் உள்ளது.
- pending-sdwa-admin | color: #7c3aed
  label: Pending SDWA Admin
  text: இறுதி ஒப்புதலுக்காக SDWA Admin-இடம் உள்ளது.
- high | color: #dc2626
  label: High
  text: வரிசையில் முன்னுரிமை. டீலர் 5 அல்லது அதற்கு மேற்பட்ட நாட்களுக்கு முன்பு விண்ணப்பித்தார்.
- medium | color: #ea580c
  label: Medium
  text: வரிசையில் முன்னுரிமை. டீலர் 2 முதல் 4 நாட்களுக்கு முன்பு விண்ணப்பித்தார்.
- low | color: #16a34a
  label: Low
  text: வரிசையில் முன்னுரிமை. டீலர் இன்று அல்லது நேற்று விண்ணப்பித்தார்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 029f192b703c661f
confidence: ok

உங்கள் கார்டு தனியாக ஒளிர்ந்து தெரியும். உங்களுக்கு அனுமதி உள்ள திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- sdwa-admin | color: #1d4ed8
  title: SDWA Admin
  - Admin Dashboard-ல் எல்லா நலத்திட்ட விண்ணப்பங்களையும் கவனிப்பது
  - **Quick actions**-லிருந்து காத்திருக்கும் ஒப்புதல்களைத் திறப்பது
  - கெஸ்ட் ஹவுஸ் புக்கிங்குகளைச் சரிபார்ப்பது
- office | color: #0d9488 | roles: Admin, CorporateAdmin, Director
  title: Admin, Corporate Admin, Director
  - இன்னும் சரிபார்க்கப்பட்டு வரும் எல்லா விண்ணப்பங்களையும் பார்ப்பது
  - டீலர் மற்றும் கெஸ்ட் ஹவுஸ் எண்ணிக்கைகளைப் பார்ப்பது
  - Reports Center-ஐத் திறப்பது
- approvers | color: #d97706 | roles: MO, RM, SMM
  title: MO, RM, SM
  - Admin Dashboard-ஐ உங்களால் திறக்க முடிந்தால், உங்கள் நிலையில் உள்ள விண்ணப்பங்கள் வரிசையில் தெரியும்
  - உங்கள் HQ, மண்டலம் அல்லது மாநிலம் மட்டுமே உங்களுக்குத் தெரியும்
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 7222902e690b16b2
confidence: ok

- dash | popular: true
  q: சில பெட்டிகளில் எண்ணுக்குப் பதிலாக — தெரிகிறது
  a: டேட்டா லோட் ஆகாமல் இருக்கலாம் (எந்தப் பகுதி என்று மேலே உள்ள சிவப்புப் பெட்டி சொல்லும்), அல்லது அந்தப் பெட்டிக்கு இன்னும் டேட்டா இல்லாமல் இருக்கலாம். **Active Schemes**, **Benefit Released**, **SLA Delayed**, **Upcoming Check-ins**, **Cancelled**, **Revenue Collected** மற்றும் **Finance & Benefit Release** பெட்டிகள் இப்போதைக்கு எப்போதும் — என்றே காட்டும்.
- download | popular: true
  q: Reports Center-ல் Download அழுத்தினால் எதுவும் நடக்கவில்லை
  a: Reports Center இன்னும் உருவாக்கப்பட்டு வருகிறது. **Download** இன்னும் கோப்பை உருவாக்காது. அதில் உள்ள ரிப்போர்ட்டுகளும் எண்ணிக்கைகளும் மாதிரிகள், உங்கள் டேட்டா அல்ல.
- icons
  q: வரிசையில் உள்ள டிக், X ஐகான்களைத் தட்டினால் எதுவும் நடக்கவில்லை
  a: இந்த ஐகான்கள் இன்னும் வேலை செய்யாது. **Quick actions**-ன் கீழ் உள்ள **Review Pending Approvals** அழுத்தி, **Scheme Approval** திரையில் ஒப்புதல் தாருங்கள் அல்லது நிராகரியுங்கள்.
- nothing-pending
  q: வரிசையில் "Nothing pending" என்று வருகிறது
  a: உங்கள் நிலையில் எந்த விண்ணப்பமும் காத்திருக்கவில்லை. MO, RM, SM தங்கள் சொந்த HQ, மண்டலம் அல்லது மாநிலத்தை மட்டுமே பார்ப்பார்கள்.
- could-not-load
  q: "Could not load: welfare approvals" என்று வருகிறது
  a: நலத்திட்ட டேட்டா MO, RM, SM, SDWA, SDWA Admin, Admin, Corporate Admin, Director ஆகியோருக்கு மட்டுமே காட்டப்படும். நீங்கள் இதைப் பார்க்க வேண்டியவர் என்றால், உங்கள் அனுமதியைச் சரிபார்க்கச் சொல்லி உங்கள் admin-இடம் கேளுங்கள், பிறகு பக்கத்தை மீண்டும் திறங்கள்.
:::

::: section admin-dashboard
title: Admin Dashboard-ஐப் படிப்பது
routes: /ReportDashboard
for: 
roles: 
designations: 
tour: 1 > [data-help=rpt-kpis] | 2 > [data-help=rpt-workflow] | 3 > [data-help=rpt-queue] | 4 > [data-help=rpt-charts] | 5 > [data-help=rpt-guesthouse] | 6 > [data-help=rpt-quick]
minutes: 3
source-hash: c608b65421e5a6ab
confidence: ok

**இந்தத் திரை எதற்கு**
SDWA Admin-க்கான **Dashboard** நலத்திட்ட விண்ணப்பங்கள், டீலர்கள், கெஸ்ட் ஹவுஸ் புக்கிங்குகளை ஒரே பக்கத்தில் காட்டும். மெனுவில் **SDWA** > **Admin Dashboard** மூலம் இதைத் திறங்கள். இங்கே நீங்கள் பார்க்க மட்டுமே செய்வீர்கள். ஒப்புதல் தருவது **Scheme Approval** திரையில்தான்.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Welfare Applications**, **Pending Final Approval**, **Approved Applications**, **Rejected Applications**, **Total Dealers** மற்றும் **Guest House Bookings**.
2. **Approval Workflow Overview**-ல், **Dealer** முதல் **Completed** வரையிலான நிலைகளையும், எத்தனை விண்ணப்பங்கள் **Pending at MO**, **Pending at RM**, **Pending at SMM**, **Pending at SDWA**, **Rejected** நிலையில் உள்ளன என்பதையும் பாருங்கள்.
3. **Pending Action Queue (SDWA)**-ல், காத்திருக்கும் ஒவ்வொரு விண்ணப்பத்தையும் அதன் **Current Stage**, **Pending Since**, **Priority** உடன் பாருங்கள். மேலும் வரிசைகளைப் பார்க்க **Show more** அழுத்துங்கள்.
4. ஒவ்வொரு திட்டத்துக்கும் எத்தனை விண்ணப்பங்கள் உள்ளன என்று **Scheme Distribution** காட்டும். மாதம் வாரியாக விண்ணப்பங்களை **Monthly Application Trend** காட்டும். அவற்றுக்குக் கீழே டீலர் எண்ணிக்கைகளும் **Regional Distribution** அட்டவணையும் இருக்கும்.
5. புக்கிங்குகளையும், ஒவ்வொரு கெஸ்ட் ஹவுஸும் எவ்வளவு நிரம்பியுள்ளது என்பதையும் **Guest House Overview** காட்டும். Front office-ஐத் திறக்க **View All Bookings** அழுத்துங்கள்.
6. **Quick actions**-ன் கீழ், விண்ணப்பங்களுக்கு ஒப்புதல் தர **Review Pending Approvals** அழுத்துங்கள். **Dealer Directory**, **Manage Schemes**, **Guest House Bookings** அந்தந்தத் திரைகளைத் திறக்கும்.

> Careful: வரிசையில் உள்ள கண், டிக், X, பதிவிறக்க ஐகான்கள், **Search zones...** பெட்டிகள், **Generate Reports** மற்றும் **Gallery** கார்டுகள் இன்னும் வேலை செய்யாது.

## திரையின் மற்ற பகுதிகள்

- **Recent Activities** கடைசி 5 நிகழ்வுகளைக் காட்டும், உதாரணமாக புதிய விண்ணப்பம், ஒரு ஒப்புதல் அல்லது கெஸ்ட் ஹவுஸ் பில்.
- **Finance & Benefit Release**-ல் இன்னும் டேட்டா இல்லை. அது — என்றும் "No payment releases" என்றும் காட்டும்.

## பொதுவான பிரச்சனைகள்

- **சிவப்புப் பெட்டியில் "Could not load" என்று வருகிறது.** டேட்டாவின் அந்தப் பகுதி லோட் ஆகவில்லை. அதன் பெட்டிகளில் — தெரியும். பிறகு பக்கத்தை மீண்டும் திறங்கள்.
- **Pending at MO எண்ணிக்கை அதிகமாகத் தெரிகிறது.** திருத்தத்துக்காக டீலருக்குத் திருப்பி அனுப்பப்பட்ட விண்ணப்பங்களையும் அது சேர்த்து எண்ணுகிறது.
- **Pending Since ரொம்ப நீளமாகத் தெரிகிறது.** விண்ணப்பம் இந்த நிலைக்கு வந்த நாளிலிருந்து அல்ல, டீலர் விண்ணப்பித்த நாளிலிருந்து அது நாட்களை எண்ணுகிறது.
- **வரிசையில் "Nothing pending" என்று வருகிறது.** இப்போது உங்கள் நிலையில் எந்த விண்ணப்பமும் காத்திருக்கவில்லை.
:::

::: section reports-center
title: Reports Center-ஐப் பார்ப்பது
routes: /ReportsCenter
for: 
roles: 
designations: 
minutes: 1
source-hash: fea64cc958f10b95
confidence: ok

**இந்தத் திரை எதற்கு**
**Reports Center** புக்கிங், பில்லிங், ஒப்புதல், டீலர் ரிப்போர்ட்டுகளைப் பார்க்கவும் பதிவிறக்கவும் உதவும். இது இன்னும் உருவாக்கப்பட்டு வருகிறது: இதில் உள்ள எண்ணிக்கைகள், ரிப்போர்ட்டுகள், தேதிகள் எல்லாம் மாதிரிகள். ஃபில்டர்களும் பதிவிறக்கமும் இன்னும் வேலை செய்யாது.

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Reports**, **Booking Reports**, **Billing Reports** மற்றும் **Approval Reports**.
2. டேபுகள் (**All Reports**, **Booking Reports**, **Billing Reports**, **Approval Reports**), **Search** பெட்டி, **All Categories** மற்றும் **All Formats** பட்டியல்கள் தெரியும், ஆனால் அவை இன்னும் பட்டியலை மாற்றாது.
3. ஒவ்வொரு ரிப்போர்ட் கார்டிலும் ரிப்போர்ட்டின் பெயர், அதில் என்ன உள்ளது, **Available Formats**, ஒரு தேதி ஆகியவை தெரியும். கார்டுகள்: **Dealer Information**, **Tirupathi Bookings**, **T-Nagar Bills**, **MO Reports**, **RM Reports** மற்றும் **Tirupathi Bills**.
4. **Category**, **Records**, **File Types**, **Generated By** மற்றும் ஒரு சிறிய அட்டவணையுடன் ஒரு மாதிரியைத் திறக்க **Preview** அழுத்துங்கள். அதை மூட **Close** அல்லது X அழுத்துங்கள்.
5. **Download**, **Download PDF**, **Download Excel** இன்னும் கோப்பை உருவாக்காது.

> Careful: இந்தத் திரையில் உள்ள எண்ணிக்கைகளைப் பயன்படுத்தாதீர்கள். அவை மாதிரிகள், உங்கள் உண்மையான டேட்டா அல்ல.

## பொதுவான பிரச்சனைகள்

- **Download அழுத்தினால் எதுவும் நடக்கவில்லை.** பதிவிறக்கம் இன்னும் தயாராகவில்லை.
- **எல்லா Preview-களும் Dealer Information-ஐயே காட்டுகின்றன.** இப்போதைக்கு எல்லா Preview பட்டன்களும் ஒரே மாதிரியைத் திறக்கும்.
- **டேபுகளும் தேடலும் பட்டியலை மாற்றவில்லை.** அவை இன்னும் வேலை செய்யவில்லை.
:::
