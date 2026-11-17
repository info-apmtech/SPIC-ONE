---
module: farm-operations
title: பண்ணை செயல்பாடுகள்
icon: bi-tree
order: 109
language: ta
category: மண் பரிசோதனை
note: புதிய வழிகாட்டி
meta-source-hash: 4c57dbd8a4469315
---

::: section overview
title: இந்த மாட்யூல் உங்களுக்கு என்ன செய்யும்
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: 62c60a0ea650a93e
confidence: ok

SPIC பண்ணையின் கணக்குகளை ஒரே இடத்தில் வைத்துக்கொள்ளுங்கள். பொருட்களையும் அவற்றின் விலையையும் அமைக்கலாம், 15 நாளுக்கு ஒருமுறை ஸ்டாக், விற்பனை, செலவுகளைப் பதிவு செய்யலாம், முழு ஆண்டுக்கான வருமானம், செலவு, லாபம், ஸ்டாக் ஆகியவற்றைப் பார்க்கலாம்.

- products | icon: bi-tags | color: #15803d
  title: பொருட்களும் விலைகளும்
  text: Farm Office புதிய பொருள் அல்லது புதிய விலை கேட்கும். Farm Admin அதற்கு ஒப்புதல் தருவார்.
- report | icon: bi-journal-text | color: #d97706
  title: 15 நாளுக்கு ஒரு ரிப்போர்ட்
  text: 1 முதல் 15 வரை, 16 முதல் மாதக் கடைசி வரை, இருப்பு (inventory), வருமானம், செலவு ஆகியவற்றை உள்ளிட்டு, சமர்ப்பியுங்கள்.
- summaries | icon: bi-graph-up-arrow | color: #2563eb
  title: ஆண்டுக்கான சுருக்கங்கள்
  text: நிதி, இருப்பு, வருமானம், செலவு சுருக்கங்களைப் பார்த்து, அவற்றை Excel-ஆகப் பதிவிறக்கலாம்.
:::

::: section flow
title: பண்ணைக் கணக்குகள் எப்படி நகர்கின்றன, யார் செயல்படுவார்கள்
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 4948b5894215c5ba
confidence: ok

ஒவ்வொரு வரிசையும் ஒரு பணிப்பொறுப்பு (role), ஒவ்வொரு கட்டமும் ஒரு படி. ஒரு படியில் என்ன நடக்கிறது என்று பார்க்க அதைத் தட்டுங்கள்.

- office | type: lane | color: #15803d
  title: Farm Office
  sub: கோரிக்கைகள் மற்றும் 15 நாள் ரிப்போர்ட்கள்
- admin | type: lane | color: #ea580c
  title: Farm Admin
  sub: ஒப்புதல்கள் மற்றும் Product Master
- request | lane: office | col: 1 | icon: bi-tags | color: #d97706 | next: approve | screen: product-rates
  title: பொருள் அல்லது விலை கேட்பது
  status: Pending
  text: **Product & Rate Management** திரையில், புதிய பொருள் அல்லது விலை மாற்றம் கேளுங்கள். அது Farm Admin-க்காகக் காத்திருக்கும்.
- approve | lane: admin | col: 2 | icon: bi-patch-check | color: #16a34a | next: entry | return: request | screen: rate-approvals
  title: விலைக்கு ஒப்புதல் தருவது
  status: Approved
  text: Farm Admin விலையையும் அளவு அலகையும் (unit) சரிபார்த்து ஒப்புதல் தருவார். பிறகு அந்தப் பொருளும் விலையும் 15 நாள் ரிப்போர்ட்டில் பயன்படும்.
- rejected | lane: admin | col: 3 | icon: bi-x-octagon | color: #dc2626 | kind: exception | from: approve | to: request | screen: rate-approvals
  title: கோரிக்கை நிராகரிக்கப்பட்டது
  status: Rejected
  text: Farm Admin ஒரு காரணத்துடன் அதை நிராகரிப்பார். Farm Office கோரிக்கையைத் திருத்தி **Resubmit Request** அழுத்தலாம்.
- entry | lane: office | col: 3 | icon: bi-journal-text | color: #64748b | next: submit | screen: fifteen-day-entry
  title: 15 நாள் ரிப்போர்ட்டை நிரப்புவது
  status: Draft
  text: அந்தக் காலத்துக்கான இருப்பு, வருமானம், செலவு ஆகியவற்றை உள்ளிடுங்கள். ஒவ்வொரு படியும் டிராஃப்டாகச் சேமிக்கப்படும்.
- submit | lane: office | col: 4 | icon: bi-send-check | color: #15803d | next: summaries | screen: report-review
  title: சரிபார்த்துச் சமர்ப்பிப்பது
  status: Submitted
  text: Review பக்கத்தைச் சரிபார்த்து **Submit 15-Day Report** அழுத்துங்கள். சமர்ப்பித்த ரிப்போர்ட்டை எப்போது வேண்டுமானாலும் திறக்கலாம்.
- summaries | lane: office | col: 5 | icon: bi-graph-up-arrow | color: #2563eb | screen: finance-summary
  title: சுருக்கங்களைப் பார்ப்பது
  text: நிதி, இருப்பு, வருமானம், செலவு சுருக்கங்கள், அந்த நிதியாண்டின் எல்லா ரிப்போர்ட்களையும் கூட்டிக் காட்டும்.
:::

::: section statuses
title: நீங்கள் பார்க்கக்கூடிய எல்லா நிலைகளும்
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: e9a76a64316c65fc
confidence: ok

- no-request | color: #64748b
  label: No Request
  text: இந்தப் பொருளுக்கு எந்தக் கோரிக்கையும் இல்லை. இப்போதைய விலையே பயன்படும்.
- pending | color: #d97706
  label: Pending
  text: கோரிக்கை Farm Admin-க்காகக் காத்திருக்கிறது.
- approved | color: #16a34a
  label: Approved
  text: Farm Admin ஒப்புதல் தந்துவிட்டார். புதிய பொருள் அல்லது விலை பயன்பாட்டில் உள்ளது.
- rejected | color: #dc2626
  label: Rejected
  text: Farm Admin மறுத்துவிட்டார். காரணத்தைப் படித்து, திருத்தி, மீண்டும் சமர்ப்பியுங்கள்.
- active | color: #15803d
  label: Active
  text: இந்தப் பொருள் Product Master-ல் பயன்பாட்டில் உள்ளது.
- inactive | color: #475569
  label: Inactive
  text: இந்தப் பொருள் நிறுத்தப்பட்டுள்ளது. புதிய கோரிக்கைகளில் தெரியாது.
- draft | color: #d97706
  label: Draft
  text: 15 நாள் ரிப்போர்ட் சேமிக்கப்பட்டது, ஆனால் சமர்ப்பிக்கப்படவில்லை. இன்னும் மாற்றலாம்.
- submitted | color: #16a34a
  label: Submitted
  text: 15 நாள் ரிப்போர்ட் இறுதியானது. ஒரு admin மட்டுமே அதை மீண்டும் திறக்க முடியும்.
- ready-to-submit | color: #2563eb
  label: Ready to submit
  text: Review படியில், ரிப்போர்ட் தயாராக உள்ளது. Finance Summary-ல், சில ரிப்போர்ட்கள் இன்னும் சமர்ப்பிக்கப்படவில்லை.
- ready-update | color: #d97706
  label: Ready for Update
  text: இப்போதைய காலம் பதிவு செய்யத் திறந்துள்ளது.
- completed | color: #16a34a
  label: Completed
  text: முந்தைய காலம், இருப்புப் படியில் தெரியும்.
- upcoming | color: #7c3aed
  label: Upcoming
  text: அடுத்த காலம், இது முடிந்த பிறகு திறக்கும்.
:::

::: section roles
title: யார் என்ன செய்வார்கள்
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 6c6e07dec3a74424
confidence: ok

உங்கள் பதவிக்கு (designation) அனுமதி உள்ள திரைகள் மட்டுமே உங்களுக்குத் தெரியும்.

- office | color: #15803d
  title: Farm Office
  - புதிய பொருட்களும் விலை மாற்றங்களும் கேட்பது
  - 15 நாள் ரிப்போர்ட்டை நிரப்பிச் சமர்ப்பிப்பது
  - நிதி, இருப்பு, வருமானம், செலவு சுருக்கங்களைச் சரிபார்ப்பது
- admin | color: #ea580c
  title: Farm Admin
  - பொருள் மற்றும் விலைக் கோரிக்கைகளுக்கு ஒப்புதல் தருவது அல்லது நிராகரிப்பது
  - Product Master-ல் பொருட்களையும் விலைகளையும் திருத்துவது
  - ஒவ்வொரு பொருளின் விலை வரலாற்றைப் பார்ப்பது
:::

::: section faq
title: பிரச்சனைகளும் பதில்களும்
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 9b23a5f40e2ecf43
confidence: ok

- no-product | popular: true
  q: என் 15 நாள் ரிப்போர்ட்டில் ஒரு பொருள் இல்லை
  a: ஒப்புதல் கிடைத்த பொருட்கள் மட்டுமே வரும். **Product & Rate Management**-ல் அதைக் கேட்டு, Farm Admin ஒப்புதல் தரும் வரை காத்திருங்கள்.
- change-submitted | popular: true
  q: ரிப்போர்ட்டைச் சமர்ப்பித்த பிறகு ஒரு தவறு தெரிந்தது
  a: சமர்ப்பித்த ரிப்போர்ட்டைத் திருத்த முடியாது. அதை மீண்டும் திறக்கச் சொல்லி உங்கள் admin-இடம் கேளுங்கள்.
- sold-locked
  q: இருப்புப் படியில் Sold நெடுவரிசையில் என்னால் டைப் செய்ய முடியவில்லை
  a: **Sold**, **Income** படியில் நீங்கள் உள்ளிடும் **Sold Quantity**-லிருந்து வருகிறது. அதை அங்கே மாற்றுங்கள்.
- no-approvals
  q: Farm Office கோரிக்கைகள் அனுப்பியும், Pending Approvals பட்டியல் காலியாக உள்ளது
  a: **From Date** மற்றும் **To Date** ஃபில்டர்கள் இன்றைய தேதியிலிருந்து தொடங்கும். முந்தைய தேதிகளைத் தேர்வு செய்து **Apply Filters** அழுத்துங்கள்.
- cannot-edit
  q: "This product is still awaiting approval - you can't edit it yet." என்று வருகிறது
  a: Farm Admin ஒப்புதல் தரும் வரை புதிய பொருளை மாற்ற முடியாது. முடிவுக்காகக் காத்திருங்கள்.
:::

::: section farm-dashboard
title: பண்ணையை ஒரே பார்வையில் பார்ப்பது
routes: /FarmDashboard, /FarmOperations
for: 
roles: 
designations: 
minutes: 2
source-hash: 46e18c8ee7fa0ff8
confidence: ok

**இந்தத் திரை எதற்கு**
பண்ணை டாஷ்போர்டு (மெனுவில் **Farm Operations**), தேர்வு செய்த நிதியாண்டுக்கான பணத்தையும் ஸ்டாக்கையும் காட்டும். மற்ற எல்லாப் பண்ணைத் திரைகளுக்கும் இங்கிருந்தே போகலாம்.

1. மேலே வலது பக்கத்தில் **Financial Year**-ஐத் தேர்வு செய்யுங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total Budget**, **Total Income Till Date**, **Total Expense Till Date**, **Net Profit Till Date** மற்றும் **ROI Till Date**.
3. **Quick Action**-ன் கீழ், **Product Management**, **Inventory Summary**, **Income Summary**, **Expense Summary** அல்லது **Finance Summary**-ஐத் தட்டி அந்தத் திரையைத் திறங்கள்.
4. **Next 15-day Period**-ல் **Start 15 Days Entry** அழுத்துங்கள். நீங்கள் டிராஃப்ட் சேமித்திருந்தால், பட்டனில் **Continue 15 Days Entry** என்று இருக்கும்.
5. **Income, Expense & Profit Trends**-ல் **Last 6 Periods**, **Last 3 Periods** அல்லது **All Periods** தேர்வு செய்யுங்கள். ஒரு பட்டையின் எண்களைப் பார்க்க அதன் மேல் வையுங்கள் அல்லது தட்டுங்கள்.
6. **Latest Stock Position** ஒவ்வொரு பொருளின் கடைசி இறுதி ஸ்டாக்கைக் காட்டும். ஒரு பொருளைத் தேடுங்கள், அல்லது இருப்புச் சுருக்கத்தைத் திறக்க **View All** அழுத்துங்கள்.
7. **Income Overview** மற்றும் **Top Expense Categories**, பணம் எங்கிருந்து வந்தது, எங்கே போனது என்று காட்டும். சுருக்கத்தைத் திறக்க **View Details** அழுத்துங்கள்.

> Tip: ROI என்பது நிகர லாபத்தை பட்ஜெட்டால் வகுத்து வரும் எண்.

## பொதுவான பிரச்சனைகள்

- **"No report history yet".** (இதுவரை ரிப்போர்ட் வரலாறு இல்லை) முதலில் ஒரு 15 நாள் ரிப்போர்ட்டைச் சமர்ப்பியுங்கள். அதன் பிறகு வரைபடம் நிரம்பும்.
- **"Could not load dashboard data".** (டாஷ்போர்டு தகவலை ஏற்ற முடியவில்லை) உங்கள் இன்டர்நெட்டைச் சரிபார்த்து **Retry** அழுத்துங்கள்.
- **எண்கள் எல்லாம் பூஜ்ஜியமாக உள்ளன.** சரியான **Financial Year** தேர்வு செய்யப்பட்டுள்ளதா என்று பாருங்கள்.
:::

::: section product-rates
title: புதிய பொருள் அல்லது விலை மாற்றம் கேட்பது
routes: /ProductRateManagement
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-prm-tabs] | 2 > [data-help=fo-prm-status] | 3 > [data-help=fo-prm-search] | 4 > [data-help=fo-prm-actions] | 5 > [data-help=fo-prm-new]
minutes: 3
source-hash: e137e022776b0912
confidence: ok

**இந்தத் திரை எதற்கு**
**Product & Rate Management** திரையில் எல்லாப் பண்ணைப் பொருட்களும், அவற்றின் இப்போதைய விலையும், கடைசிக் கோரிக்கையும் தெரியும். Farm Admin-இடம் புதிய பொருள் அல்லது புதிய விலை கேட்க இதைப் பயன்படுத்துங்கள்.

1. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All Product**, **Livestock** அல்லது **Orchard**.
2. அந்தக் கோரிக்கைகளை மட்டும் பார்க்க **Pending** அல்லது **Approved** தட்டுங்கள். **All** எல்லாப் பொருட்களையும் காட்டும்.
3. ஒரு பொருளை வேகமாகக் கண்டுபிடிக்க **Search products...**-ல் டைப் செய்யுங்கள்.
4. **Action** நெடுவரிசையில், கண் ஐகான் **Request Details**-ஐக் காட்டும். பென்சில், விலை மாற்றம் கேட்கும், அல்லது உங்கள் **Pending** அல்லது **Rejected** கோரிக்கையைத் திருத்தும். குப்பைத்தொட்டி ஒரு **Pending** அல்லது **Rejected** கோரிக்கையை நீக்கும்.
5. ஒரு பொருளைச் சேர்க்க **Request New Product** அழுத்துங்கள். **Live Stock** அல்லது **Orchard** தேர்வு செய்து, **Product**, **UOM** மற்றும் **Price** உள்ளிட்டு, விரும்பினால் **Reason** சேர்த்து, **Request Product** அழுத்துங்கள்.

> Tip: ஒப்புதல் கிடைத்ததும், அந்தப் பொருளும் விலையும் இருப்பு மற்றும் வருமானப் பதிவில் வரும்.
> Careful: கோரிக்கையை நீக்கினால் அதைத் திரும்பப் பெற முடியாது.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Live Stock** / **Orchard** | பொருள் பிரிவு (கால்நடை அல்லது தோட்டம்). |
| **Product** | பொருளின் பெயர், உதாரணமாக Sheep (செம்மறி ஆடு). கட்டாயம். |
| **UOM** | Kg, Unit, Litre அல்லது Nos. கட்டாயம். |
| **Price** | ஒரு அலகுக்கான விலை, ரூபாயில். பூஜ்ஜியத்தை விட அதிகமாக இருக்க வேண்டும். |
| **Reason (Optional)** | உங்களுக்கு ஏன் தேவை. Farm Admin முடிவு செய்ய உதவும். |

## பொதுவான பிரச்சனைகள்

- **"Enter a valid rate."** (சரியான விலையை உள்ளிடுங்கள்) பூஜ்ஜியத்தை விட அதிகமான விலையை உள்ளிடுங்கள்.
- **"This product is still awaiting approval - you can't edit it yet."** (இந்தப் பொருள் இன்னும் ஒப்புதலுக்காகக் காத்திருக்கிறது, இப்போது திருத்த முடியாது) Farm Admin புதிய பொருளுக்கு ஒப்புதல் தரும் வரை காத்திருங்கள்.
- **என் கோரிக்கையில் Rejected என்று காட்டுகிறது.** **Rejection Reason**-ஐப் படிக்க கண் ஐகானைத் தட்டித் திறங்கள். பென்சிலைத் தட்டி, திருத்தி, **Resubmit Request** அழுத்துங்கள்.
- **எனக்கு Request New Product தெரியவில்லை.** உங்கள் பதவியால் இந்தத் திரையைப் பார்க்க மட்டுமே முடியும்.
:::

::: section rate-approvals
title: பொருள் மற்றும் விலைக் கோரிக்கைகளுக்கு ஒப்புதல் தருவது அல்லது நிராகரிப்பது
routes: /ProductRateApprovals
for: 
roles: 
designations: 
minutes: 3
source-hash: 0eac09c794e59c9c
confidence: ok

**இந்தத் திரை எதற்கு**
**Pending Approvals** திரையில், Farm Office-இடமிருந்து வந்து முடிவுக்காகக் காத்திருக்கும் புதிய பொருள் மற்றும் விலை மாற்றக் கோரிக்கைகள் தெரியும். முடிவு செய்த கோரிக்கை இந்தப் பட்டியலிலிருந்து போய்விடும்.

1. **Filters**-ஐத் திறங்கள். **From Date**, **To Date**, **Request Type**, **Product Group** அல்லது **Product** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
2. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All Request**, **New Product** அல்லது **Rate Change**. எத்தனை காத்திருக்கின்றன என்று எண்கள் காட்டும்.
3. ஒரு கோரிக்கையைப் பார்க்க கண் ஐகானைத் தட்டுங்கள்: பொருள், அலகு, இப்போதைய விலை, **Request Price**, யார் கேட்டார், **Reason**.
4. கேட்ட விலையிலேயே ஒப்புதல் தர, வரிசையில் உள்ள **Approve** அழுத்தி உறுதி செய்யுங்கள்.
5. முதலில் விலையையோ அலகையோ மாற்ற, பென்சிலைத் தட்டுங்கள். **Approval Details**-ன் கீழ் **UOM** அல்லது **Approved Price**-ஐ மாற்றி, **Admin Remarks** சேர்த்து, **Approve** அழுத்துங்கள்.
6. மறுக்க, பென்சிலைத் தட்டி, **Admin Remarks**-ல் காரணத்தை எழுதி, **Reject** அழுத்தி, பிறகு உறுதி செய்யுங்கள்.

> Careful: ஒப்புதல் தந்தவுடனே பொருளின் விலை மாறிவிடும். அது அடுத்த 15 நாள் ரிப்போர்ட்டில் பயன்படும்.

## பொதுவான பிரச்சனைகள்

- **பட்டியல் காலியாக உள்ளது.** தேதிகள் இன்றிலிருந்து தொடங்குகின்றன. முந்தைய **From Date**-ஐத் தேர்வு செய்து **Apply Filters** அழுத்துங்கள்.
- **"Enter a rejection reason in Admin Remarks."** (Admin Remarks-ல் நிராகரிப்புக் காரணத்தை எழுதுங்கள்) நிராகரிக்கக் காரணம் கட்டாயம்.
- **"Enter a valid approved price."** (சரியான ஒப்புதல் விலையை உள்ளிடுங்கள்) பூஜ்ஜியத்தை விட அதிகமான விலையை உள்ளிடுங்கள்.
- **Approve பட்டன் இல்லை.** உங்கள் பதவியால் கோரிக்கைகளைப் பார்க்க மட்டுமே முடியும்.
:::

::: section product-master
title: Product Master-ஐ நிர்வகிப்பது
routes: /FarmProductMaster
for: 
roles: 
designations: 
minutes: 2
source-hash: 858235a42d5c0ca1
confidence: ok

**இந்தத் திரை எதற்கு**
**Product Master** என்பது எல்லாப் பண்ணைப் பொருட்கள், அவற்றின் அலகுகள், இப்போதைய விலைகள் உள்ள பட்டியல். ஒப்புதல் கிடைத்த கோரிக்கைகள் இங்கே சேரும். Farm Admin ஒரு பொருளை நேரடியாகவும் திருத்தலாம்.

1. ஒரு டேபைத் தேர்வு செய்யுங்கள்: **All Product**, **Livestock** அல்லது **Orchard**, அல்லது ஒரு பொருளைத் தேடுங்கள்.
2. **Current Rate** மற்றும் **Status** (**Active** அல்லது **Inactive**) சரிபாருங்கள்.
3. ஒப்புதல் கிடைத்த ஒவ்வொரு விலையையும் அதன் தேதியுடன் பார்க்க **View History** அழுத்துங்கள். கடைசி விலைக்கு **Current** என்ற குறி இருக்கும்.
4. ஒரு பொருளைத் திருத்த, பென்சிலைத் தட்டுங்கள். **Product Group**, **Product Name**, **UOM** அல்லது **Current Rate**-ஐ மாற்றுங்கள்.
5. ஒரு பொருளைப் பயன்படுத்துவதை நிறுத்த **Active**-ல் உள்ள டிக்கை எடுங்கள். அது அதன் விலை வரலாற்றுடன் பட்டியலிலேயே இருக்கும்.
6. **Save Changes** அழுத்துங்கள்.

> Careful: இங்கே செய்யும் திருத்தம் ஒப்புதல் வழியாகப் போகாது. மாற்றம் உடனே பயன்பாட்டுக்கு வரும்.

## பொதுவான பிரச்சனைகள்

- **"Enter a product name and UOM."** (பொருளின் பெயரையும் UOM-ஐயும் உள்ளிடுங்கள்) இரண்டும் கட்டாயம்.
- **"No approved rate history yet."** (இதுவரை ஒப்புதல் கிடைத்த விலை வரலாறு இல்லை) இந்தப் பொருளுக்கு இன்னும் எந்த விலைக் கோரிக்கைக்கும் ஒப்புதல் கிடைக்கவில்லை.
- **பென்சில் ஐகான் இல்லை.** உங்கள் பதவியால் Product Master-ஐப் பார்க்க மட்டுமே முடியும்.
:::

::: section fifteen-day-entry
title: 15 நாள் ரிப்போர்ட்டை நிரப்பிச் சமர்ப்பிப்பது
routes: /FifteenDaysEntry, /15daysEntry
for: 
roles: 
designations: 
tour: 1 > [data-help=fo-steps] | 2 > [data-help=fo-period] | 3 > [data-help=fo-inventory] | 4 > [data-help=fo-continue] | 5 > [data-help=fo-continue] | 6 > [data-help=fo-save]
minutes: 6
source-hash: 8adffbba9a65bc8e
confidence: ok

**இந்தத் திரை எதற்கு**
ஒவ்வொரு 15 நாளுக்கும் (1 முதல் 15 வரை, 16 முதல் மாதக் கடைசி வரை) பண்ணை ஒரு ரிப்போர்ட் அனுப்பும். அதில் நான்கு படிகள் உண்டு: **Inventory**, **Income**, **Expense** மற்றும் **Review**. இன்னும் சமர்ப்பிக்காத அடுத்த காலத்தை இந்தத் திரை திறக்கும்.

1. **Inventory**, **Income**, **Expense**, **Review** இடையே போக மேலே உள்ள படிகளைப் பயன்படுத்துங்கள். ஒவ்வொரு முறை நகரும்போதும் உங்கள் வேலை டிராஃப்டாகச் சேமிக்கப்படும்.
2. காலக் கார்டுகளைச் சரிபாருங்கள்: **Current Reporting**, **Previous Period** மற்றும் **Next Reporting Period**.
3. **Inventory Entry**-ல், **Livestock Sales Ledger**-ல் ஒவ்வொரு பொருளுக்கும் **Purchase / Old Quantity**, **Hatched / New Breed**, **Mortality** மற்றும் **Others** உள்ளிடுங்கள். **Opening Stock** மற்றும் **Closing Stock** தானாக நிரம்பும். தேவைப்பட்டால் **Remarks** சேருங்கள்.
4. **Continue to Income** அழுத்துங்கள். **Income Entry**-ல், **Livestock Sales Ledger** மற்றும் **Orchard Sales**-ல் ஒவ்வொரு பொருளுக்கும் **Sold Quantity** உள்ளிடுங்கள். விலை, ஒப்புதல் கிடைத்த விலைதான், அதனால் **Total amount** தானாகக் கணக்கிடப்படும். **Continue to Expense** அழுத்துங்கள்.
5. **Expense Entry**-ல், **Expenditure Breakup** மற்றும் **Miscellaneous Expenditure**-ன் கீழ் தொகைகளை உள்ளிடுங்கள். ஒரு வரி இல்லையென்றால் **Add New Category** அல்லது **Add New Sub Category** பயன்படுத்துங்கள். **Continue to Review** அழுத்தி, எல்லாவற்றையும் சரிபார்த்து, **Submit 15-Day Report** அழுத்தி, பிறகு **Confirm Submit** அழுத்துங்கள்.
6. நிறுத்திவிட்டுப் பிறகு திரும்ப வர, எப்போது வேண்டுமானாலும் **Save as Draft** அழுத்துங்கள்.

> Tip: Breakup-ல் உள்ள **Miscellaneous** என்பது துணைப் பிரிவுகளின் மொத்தம். அதில் நீங்கள் டைப் செய்ய முடியாது.
> Careful: சமர்ப்பித்த பிறகு, ஒரு admin மீண்டும் திறந்தால் தவிர, ரிப்போர்ட்டைத் திருத்த முடியாது.

## சமர்ப்பித்த பிறகு

- **15 - Day Report Submitted Successfully!** திரை மொத்தங்களைக் காட்டும்.
- அதைத் திறக்க **View Submitted Report** அழுத்துங்கள், ஆண்டுக் கணக்குக்கு **Go to finance summary** அழுத்துங்கள், அல்லது அடுத்த காலத்துக்கு **Enter ... Report** அழுத்துங்கள்.

## புலங்களின் விளக்கம்

| புலம் | என்ன உள்ளிட வேண்டும் |
|---|---|
| **Purchase / Old Quantity** | இந்தக் காலத்தில் வாங்கிய விலங்குகள் அல்லது பொருட்கள். |
| **Hatched / New Breed** | இந்தக் காலத்தில் புதிதாகப் பிறந்த அல்லது குஞ்சு பொரித்த விலங்குகள். |
| **Mortality** | இந்தக் காலத்தில் இறந்த விலங்குகள். |
| **Others** | வேறு ஏதாவது மாற்றம். ஸ்டாக்கைக் குறைக்க இது பூஜ்ஜியத்துக்குக் கீழே (மைனஸ்) இருக்கலாம். |
| **Sold Quantity** | எத்தனை விற்றீர்கள். இது இருப்புப் படியில் உள்ள **Sold**-ஐயும் நிரப்பும். |
| **Amount (₹)** | இந்தக் காலத்தில் அந்தச் செலவுக்கு ஆன பணம். |

## பொதுவான பிரச்சனைகள்

- **"No products in this ledger".** (இந்த லெட்ஜரில் பொருட்கள் இல்லை) இன்னும் எந்தப் பொருளுக்கும் ஒப்புதல் கிடைக்கவில்லை. **Product & Rate Management**-ல் அதைக் கேளுங்கள்.
- **Sold-ல் என்னால் டைப் செய்ய முடியவில்லை.** **Income** படியில் **Sold Quantity**-ஐ உள்ளிடுங்கள்.
- **விலை தவறாக உள்ளது.** **Product & Rate Management**-ல் விலை மாற்றம் கேளுங்கள். இங்கே அதை மாற்ற முடியாது.
- **"Please enter a category name."** (பிரிவின் பெயரை உள்ளிடுங்கள்) **Add Category** அழுத்தும் முன் ஒரு பெயரை டைப் செய்யுங்கள்.
:::

::: section report-review
title: சமர்ப்பித்த 15 நாள் ரிப்போர்ட்டைத் திறப்பது
routes: /FifteenDayReportReview, /15DayReportReview
for: 
roles: 
designations: 
minutes: 2
source-hash: 1abbfd7769868ce9
confidence: ok

**இந்தத் திரை எதற்கு**
**15 - Day Report Review** திரை, சமர்ப்பித்த ஒரு ரிப்போர்ட்டை முழுமையாகக் காட்டி, அதை முந்தைய காலத்துடன் ஒப்பிடும்.

1. சமர்ப்பித்த பிறகு **View Submitted Report** மூலம், அல்லது **Finance Summary**-ல் உள்ள கண் ஐகான் மூலம் இதைத் திறங்கள்.
2. மேலே, காலம், நிலை, யார் சமர்ப்பித்தார் என்பதைப் பாருங்கள்.
3. கார்டுகளைப் படியுங்கள்: **Total Income**, **Total Expense**, **Net Profit** மற்றும் **Total Products**. அம்புக்குறிகள் முந்தைய 15 நாளுடன் ஒப்பிட்டுக் காட்டும்.
4. ஒவ்வொரு பகுதியையும் திறக்க அல்லது மூட **Inventory Summary**, **Income Summary** அல்லது **Expense Summary**-ஐத் தட்டுங்கள்.
5. கீழே உள்ள **Financial Summary** மற்றும் **Comparison with Previous**-ஐச் சரிபாருங்கள்.
6. திரும்பிப் போக **Back to Finance Summary List** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No previous period to compare".** (ஒப்பிட முந்தைய காலம் இல்லை) இது இந்த நிதியாண்டின் முதல் ரிப்போர்ட்.
- **"No inventory records found for this period."** (இந்தக் காலத்துக்கு இருப்புப் பதிவுகள் இல்லை) அந்த ரிப்போர்ட்டில் ஸ்டாக் எதுவும் உள்ளிடப்படவில்லை.
:::

::: section finance-summary
title: ஆண்டுக்கான நிதிச் சுருக்கத்தைப் பார்ப்பது
routes: /FinanceSummary, /FinanceSummaryOverall
for: 
roles: 
designations: 
minutes: 2
source-hash: d2b850b02c31e1d6
confidence: ok

**இந்தத் திரை எதற்கு**
**Finance Summary (Overall)** ஒரு நிதியாண்டின் எல்லா 15 நாள் ரிப்போர்ட்களையும் கூட்டி, ஒவ்வொரு ரிப்போர்ட்டையும் பட்டியலிடும்.

1. **Financial Year**-ஐயும் **View**-ஐயும் தேர்வு செய்யுங்கள்: **15-Day View**, **Monthly View** அல்லது **Quarterly View**.
2. கார்டுகளைப் படியுங்கள்: **Annual Budget**, **Total Income**, **Total Expense**, **Net Profit** மற்றும் **ROI**.
3. **Analytics Validation Trend**-ல் **Last 6 Months**, **Last 3 Months** அல்லது **Last 1 Year** தேர்வு செய்யுங்கள்.
4. ரிப்போர்ட் வரலாற்றில், பட்டியலைக் குறைக்க **From Date** மற்றும் **To Date** அமையுங்கள்.
5. **Status** நெடுவரிசையில் **Submitted** அல்லது **Draft** என்று தெரியும். ரிப்போர்ட்டைத் திறக்க கண் ஐகானைத் தட்டுங்கள்.
6. பட்டியலைச் சேமிக்க **Download Excel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No report records found for the selected filter."** (தேர்வு செய்த ஃபில்டருக்கு ரிப்போர்ட் பதிவுகள் இல்லை) தேதிகளை அகலமாக்குங்கள், அல்லது வேறு நிதியாண்டைத் தேர்வு செய்யுங்கள்.
- **மேலே உள்ள குறியில் Ready to submit என்று உள்ளது.** அந்த ஆண்டில் குறைந்தது ஒரு ரிப்போர்ட் இன்னும் டிராஃப்டாக உள்ளது.
:::

::: section inventory-summary
title: பொருள் வாரியாக ஸ்டாக் நகர்வைப் பார்ப்பது
routes: /InventorySummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 7b39d227504cc4c7
confidence: ok

**இந்தத் திரை எதற்கு**
**Inventory Summary**, ஒவ்வொரு 15 நாள் காலத்திலும் ஒவ்வொரு பொருளின் ஸ்டாக் எப்படி மாறியது என்று காட்டும்.

1. **Filters**-ஐத் திறங்கள். **From Date**, **To Date**, **Category** மற்றும் **Product** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். **Clear All** அவற்றை நீக்கும்.
2. ஒரு பொருளைப் பார்க்க அதன் டேபைத் தட்டுங்கள்.
3. கார்டுகளைப் படியுங்கள்: **Total Purchased**, **Total Hatched**, **Total Sold**, **Mortality** மற்றும் **Closing Stock**.
4. **Stock Movement History** அட்டவணையில், வரிசைப்படுத்த ஒரு நெடுவரிசைப் பெயரைத் தட்டுங்கள். இன்னும் டிராஃப்டில் உள்ள காலத்துக்கு **Draft** என்ற குறி இருக்கும்.
5. அட்டவணையைச் சேமிக்க **Download Excel** அல்லது **Download PDF** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"From Date cannot be later than To Date."** (From Date, To Date-க்குப் பிறகு இருக்கக் கூடாது) தேதிகளை மீண்டும் தேர்வு செய்யுங்கள்.
- **"No products found".** (பொருட்கள் எதுவும் இல்லை) முதலில் Product Master-ல் பொருட்கள் சேர்க்கப்பட வேண்டும்.
:::

::: section income-summary
title: வருமானச் சுருக்கத்தைப் பார்ப்பது
routes: /IncomeSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: d11284cff39730df
confidence: ok

**இந்தத் திரை எதற்கு**
**Income Summary**, ஒவ்வொரு ரிப்போர்ட் காலத்துக்குமான பண்ணை விற்பனை வருமானத்தைக் கால்நடை (livestock), தோட்டம் (orchard) என்று பிரித்துக் காட்டும்.

1. **Filters**-ஐத் திறங்கள். **Period Type** (**15 Days** அல்லது **1 Month**), **From Date**, **To Date** மற்றும் **Income Category** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total Income Till Date**, **Sales Records**, **Products Covered** மற்றும் **Reporting Period**. கடைசிக் கார்டு, கடைசிக் காலத்தைப் புதுப்பிக்க இன்னும் எத்தனை நாள் உள்ளது என்று காட்டும்.
3. **Livestock Sales Ledger** அட்டவணையில், ஒவ்வொரு காலத்துக்கும் **Livestock Income**, **Orchard Income**, **Total Income** மற்றும் **Status** பாருங்கள்.
4. அந்தக் காலத்தின் ஒவ்வொரு பொருள், அளவு, விலை, வருமானம் ஆகியவற்றைப் பார்க்க **View**-ல் உள்ள கண் ஐகானைத் தட்டுங்கள்.
5. ஒவ்வொரு பொருளின் பங்கைப் பார்க்க **Live Income Overview** மற்றும் **Orchard Income Overview** பாருங்கள்.
6. அட்டவணையைச் சேமிக்க **Download Excel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"Editing window closed".** (திருத்தும் நேரம் முடிந்துவிட்டது) கடைசிக் காலத்தைப் புதுப்பிக்கும் நேரம் முடிந்துவிட்டது.
- **"Could not load income data".** (வருமானத் தகவலை ஏற்ற முடியவில்லை) **Retry** அழுத்துங்கள்.
:::

::: section expense-summary
title: செலவுச் சுருக்கத்தைப் பார்ப்பது
routes: /ExpenseSummary
for: 
roles: 
designations: 
minutes: 2
source-hash: 9200f1d0c1608503
confidence: ok

**இந்தத் திரை எதற்கு**
**Expense Summary**, ஒவ்வொரு ரிப்போர்ட் காலத்துக்குமான பண்ணைச் செலவுகளையும், அதிகம் செலவான பிரிவுகளையும் காட்டும்.

1. **Filters**-ஐத் திறங்கள். **Period Type**, **From Date**, **To Date** மற்றும் **Expense Category** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள்.
2. கார்டுகளைப் படியுங்கள்: **Total Expense**, **Highest Expense Category**, **Expense Categories** மற்றும் **Summary Coverage**.
3. **15 - Day Expense Summary**-ல், ஒவ்வொரு காலத்தின் **Total Expense (₹)** மற்றும் **Status** பாருங்கள்.
4. அந்தக் காலத்தின் **Expenditure Breakup (Farm Operations)** மற்றும் **Miscellaneous Expenditure (Sub-Categories)** பார்க்க **View**-ல் உள்ள கண் ஐகானைத் தட்டுங்கள்.
5. ஒப்பிட்டுப் பார்க்க **Monthly Expense Trend** மற்றும் **Expense Category Overview** பாருங்கள்.
6. அட்டவணையைச் சேமிக்க **Download Excel** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No expense data yet".** (இதுவரை செலவுத் தகவல் இல்லை) முதலில் செலவுகளுடன் ஒரு 15 நாள் ரிப்போர்ட்டைச் சமர்ப்பியுங்கள்.
- **"Could not load expense data".** (செலவுத் தகவலை ஏற்ற முடியவில்லை) **Retry** அழுத்துங்கள்.
:::
