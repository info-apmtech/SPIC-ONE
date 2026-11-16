---
module: lab
title: SAS மண் பரிசோதனை லேப்
icon: bi-eyedropper
order: 60
language: ta
---

::: section overview
title: மண் பரிசோதனை லேப் எப்படி வேலை செய்கிறது
routes: /Lab
for: 
roles: 
designations: 
source-hash: d196c24e61d998d8
confidence: ok

**இந்த மாட்யூல் எதற்கு**
ஒவ்வொரு மண் மற்றும் தண்ணீர் மாதிரியும் லேபுக்கு வந்த நாள் முதல் விவசாயிக்கு அறிக்கை கிடைக்கும் வரை லேப் போர்ட்டல் பின்தொடர்கிறது. ஒவ்வொருவரும் தங்கள் வேலைக்கான திரைகளை மட்டுமே பார்ப்பார்கள்.

## ஒரு consignment-ன் பயணம்

1. களப் பணியாளர்கள் விவசாயிகளிடம் இருந்து மாதிரிகளைச் சேகரித்து, கூரியர் மூலம் லேபுக்கு ஒரு **consignment** ஆக அனுப்புவார்கள். வழியில் அது **In Transit** என்று காட்டும்.
2. பார்சல் வந்ததும், லேப் ஒருங்கிணைப்பாளர் **Mark received** அழுத்துவார். Consignment **Batch Pending** ஆகும்.
3. ஒருங்கிணைப்பாளர் ஒன்று அல்லது அதற்கு மேற்பட்ட Batch Pending consignment-களை ஒரு **batch**-ல் சேர்த்து, அதை ஒரு லேப் அனலிஸ்டுக்கு ஒதுக்குவார். Batch **Taken for Analysis** ஆகும்.
4. அனலிஸ்ட் batch-ஐத் திறந்து, ஒவ்வொரு மாதிரிக்கும் பரிசோதனை மதிப்புகளை உள்ளிட்டு, **Submit Values** அழுத்துவார். முடிவு, நிலை, பரிந்துரை ஆகியவற்றை சிஸ்டம் தானாகக் கணக்கிடும்.
5. எல்லா மாதிரிகளும் சமர்ப்பிக்கப்பட்டதும், ஒருங்கிணைப்பாளர் **Complete Batch** அழுத்துவார். இது மதிப்புகளைப் பூட்டி, ஒவ்வொரு மாதிரிக்கும் ஒரு அறிக்கையை உருவாக்கும்.
6. அறிக்கைகளை ஆங்கிலத்திலும் இந்திய மொழிகளிலும் PDF அல்லது Excel ஆகப் பதிவிறக்கி விவசாயிக்குக் கொடுக்கலாம். விவசாயியும் ஆப்பில் அறிக்கையைப் பதிவிறக்கலாம்.

## யார் என்ன செய்வார்கள்

- **Lab coordinator** (லேப் ஒருங்கிணைப்பாளர்): consignment-களைப் பெறுவார், batch-கள் உருவாக்குவார், அனலிஸ்டுகளை ஒதுக்குவார், batch-களை முடிப்பார், லேப் அறிக்கைகளைப் பதிவிறக்குவார்.
- **Lab analyst** (லேப் அனலிஸ்ட்): தனக்கு ஒதுக்கிய batch-களுக்குப் பரிசோதனை மதிப்புகளை உள்ளிடுவார், மாதிரி அறிக்கைகளைப் பதிவிறக்குவார்.
- **Lab admin** (லேப் அட்மின்): **Lab Tracking**-ல் எல்லா batch-களையும் கண்காணிப்பார். இந்தத் திரையில் பார்க்க மட்டுமே முடியும்.

> உங்கள் பதவிக்கு (designation) லேப் பக்கங்கள் இருந்தால் மட்டுமே லேப் திரைகள் திறக்கும். Admin-க்கும் இது தேவை. லேப் மெனு இல்லை என்றால் உங்கள் நிர்வாகியிடம் (administrator) கேளுங்கள்.
:::

::: section coordinator-dashboard
title: லேப் ஒருங்கிணைப்பாளர் டாஷ்போர்டு
routes: /Lab
for: 
roles: 
designations: 
source-hash: 17d85f22de7e96da
confidence: ok

**இந்தத் திரை எதற்கு**
**Lab Coordinator Dashboard** லேபின் நிலையை ஒரே பார்வையில் காட்டும்: எத்தனை consignment-கள் வந்தன, எத்தனை இன்னும் batch-க்காகக் காத்திருக்கின்றன, பகுப்பாய்வு எவ்வளவு தூரம் முடிந்துள்ளது. நீங்கள் **Lab**-ஐத் திறக்கும்போது, ஒருங்கிணைப்பாளர்களுக்கு ஆப் இந்த டாஷ்போர்டைக் காட்டும். அனலிஸ்டுகள் அவர்களுக்கான டாஷ்போர்டுக்குப் போவார்கள்.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Consignments Received**, **Pending Batch Creation**, **Total Batches Created**, **Taken for Analysis**, **Analysis Completed**, **Completed Batches** மற்றும் **Reports Generated**.
2. **Recent Consignments** அட்டவணை கடைசி ஐந்து consignment-களை அவற்றின் **Status**-உடன் (**Batch Pending** அல்லது **Batch Created**) காட்டும்.
3. அந்த consignment-ஐத் திறக்க, consignment எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள்.
4. முழு **Consignment Details** பட்டியலைத் திறக்க, **Recent Consignments**-ன் பக்கத்தில் உள்ள **View All** அழுத்துங்கள்.
5. **Recently Created Batches** அட்டவணை கடைசி ஐந்து batch-களைக் காட்டும்: **Assigned To**-ல் அனலிஸ்ட், **Current Status**, **Analysis Days**.
6. **Analysis Tracking**-ஐத் திறக்க, **Recently Created Batches**-ன் பக்கத்தில் உள்ள **View All** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No batches yet" என்று வருகிறது.** இன்னும் எந்த batch-ம் உருவாக்கப்படவில்லை. Batch Pending consignment-களைப் பார்க்க **Go to consignments** அழுத்துங்கள்.
- **"The dashboard could not be loaded" என்று வருகிறது.** இணைய இணைப்பைச் சரிபார்த்து **Try again** அழுத்துங்கள்.
- **என்னை முதன்மை Dashboard-க்கு அனுப்பிவிட்டது.** உங்கள் பதவிக்கு ஒருங்கிணைப்பாளர் அல்லது அனலிஸ்ட் லேப் பக்கங்கள் இல்லை. உங்கள் நிர்வாகியிடம் கேளுங்கள்.
:::

::: section receive-consignments
title: லேபில் consignment-களைப் பெறுவது
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
source-hash: fc856d94c96b8437
confidence: ok

**இந்தத் திரை எதற்கு**
**Consignment Details** திரையில் லேபுக்கு அனுப்பிய எல்லா consignment-களும் இருக்கும். ஒரு பார்சலைப் பெற்றதாகக் குறிக்கவும், அதற்குள் என்ன இருக்கிறது என்று பார்க்கவும் இதைப் பயன்படுத்துங்கள்.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Consignments**, **Pending Batch Creation**, **Batched Consignments**, **Water Samples** மற்றும் **Soil Samples**.
2. ஒரு consignment-ஐக் கண்டுபிடிக்க, தேடல் பெட்டியில் consignment எண் அல்லது AWB எண்ணை டைப் செய்யுங்கள், அல்லது **All Statuses**-ல் ஒரு நிலையைத் தேர்வு செய்யுங்கள்.
3. பார்சல் வந்ததும், **In Transit** நிலையில் உள்ள அதன் வரிசையைக் கண்டுபிடியுங்கள்.
4. **Action** நெடுவரிசையில் உள்ள receive ஐகானைத் தட்டுங்கள் (போனில் **Mark received** அழுத்துங்கள்).
5. கேள்விப் பெட்டியில் மீண்டும் **Mark received** அழுத்துங்கள். நிலை **Batch Pending** ஆக மாறும், அதன் மாதிரிகள் batch-க்குத் தயாராகும்.
6. முழு விவரங்களைப் பார்க்க, consignment எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள்.
7. விவரங்கள் திரையில் மாதிரி எண்ணிக்கைகள், State, Region, **Head Quarters**, **History / Timeline**, **Collections**, **Samples**, **Courier & Package** விவரங்கள் ஆகியவை தெரியும்.
8. அனுப்பும்போது எடுத்த புகைப்படங்களைப் பார்க்க **Consignment Photos** அழுத்துங்கள். அந்த மாதிரி சேகரிப்பைத் திறக்க ஒரு **Collection No.**-ஐத் தட்டுங்கள்.
9. Consignment ஏற்கனவே ஒரு batch-ல் இருந்தால், அந்த batch-ஐத் திறக்க **View Batch Details** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **Mark received பட்டன் இல்லை.** ஒருங்கிணைப்பாளர்கள் மட்டுமே பெற முடியும், அதுவும் **In Transit** நிலையில் உள்ள consignment-களை மட்டுமே.
- **"No consignments found" என்று வருகிறது.** உங்கள் தேடல் அல்லது நிலை ஃபில்டர் ரொம்பக் குறுகலாக உள்ளது. **Clear filters** அழுத்துங்கள்.
- **Consignment Photos சாம்பல் நிறத்தில் உள்ளது.** பார்சல் அனுப்பும்போது புகைப்படம் எதுவும் சேர்க்கப்படவில்லை.
:::

::: section create-batch
title: Consignment-களில் இருந்து batch உருவாக்குவது
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
source-hash: 90ca666972940c84
confidence: ok

**இந்தத் திரை எதற்கு**
Batch என்பது ஒரு அனலிஸ்ட் ஒன்றாகப் பரிசோதிக்கும் மாதிரிகளின் குழு. ஒரு batch-ல் பல consignment-கள் இருக்கலாம். அதை **Consignment Details** பட்டியலில் இருந்து உருவாக்குவீர்கள்.

## படிகள்

1. **Consignment Details**-ஐத் திறங்கள். batch-க்காகக் காத்திருக்கும் consignment-களை மட்டும் பார்க்க, **All Statuses**-ல் **Batch Pending** தேர்வு செய்யுங்கள்.
2. batch-ல் சேர்க்க வேண்டிய ஒவ்வொரு consignment-ன் தொடக்கத்தில் உள்ள பெட்டியை டிக் செய்யுங்கள். மேலே உள்ள பெட்டி பக்கத்தில் உள்ள எல்லா Batch Pending வரிசைகளையும் டிக் செய்யும்.
3. **Create Batch (n)** அழுத்துங்கள். நீங்கள் எத்தனை டிக் செய்தீர்கள் என்று அந்த எண் காட்டும். ஒரு consignment-ன் சொந்தத் திரையில் இருந்தால், அதற்குப் பதிலாக **Create Batch** அழுத்துங்கள்.
4. **Create Batch** படிவத்தை நிரப்பி (கீழே பாருங்கள்) **Confirm** அழுத்துங்கள்.
5. **Batch Created Successfully** பெட்டி புதிய batch எண்ணைக் காட்டும். batch-ஐத் திறக்க **View Batch Details** அழுத்துங்கள், அல்லது எல்லா batch-களையும் பார்க்க **Go To Analysis Tracking** அழுத்துங்கள்.

## புலங்களின் விளக்கம்

- **Batch No**: நீங்கள் Confirm அழுத்திய பிறகு சிஸ்டம் உருவாக்கும்.
- **Batch Created Date**: இயல்பாக இன்றைய தேதி. எதிர்காலத் தேதி அனுமதி இல்லை.
- **Consignment No.**: டிக் செய்த consignment-கள் சிப்களாகக் காட்டப்படும். ஒன்றை நீக்க அந்தச் சிப்பில் உள்ள சிறிய பெருக்கல் குறியை அழுத்துங்கள், அல்லது வேறு ஒரு Batch Pending consignment-ஐச் சேர்க்க பெட்டியில் டைப் செய்யுங்கள்.
- **Assign To**: மாதிரிகளைப் பரிசோதிக்கும் அனலிஸ்ட். இப்போதே தேர்வு செய்தால், batch நேரடியாக **Taken for Analysis** ஆகும். காலியாக விட்டுப் பிறகும் ஒதுக்கலாம்.
- **Priority**: **Low**, **Medium** (இயல்பு) அல்லது **High**. அனலிஸ்டுக்கு இந்த முன்னுரிமை தெரியும்.
- **Remarks**: batch-க்கான ஏதாவது குறிப்பு.

## பொதுவான பிரச்சனைகள்

- **"Pick at least one consignment."** (குறைந்தது ஒரு consignment-ஐத் தேர்வு செய்யுங்கள்) Confirm அழுத்தும் முன் குறைந்தது ஒரு consignment-ஐச் சேருங்கள்.
- **டிக் பெட்டிகள் இல்லை.** **Batch Pending** வரிசைகளை மட்டுமே டிக் செய்ய முடியும், அந்தப் பெட்டிகள் ஒருங்கிணைப்பாளர்களுக்கு மட்டுமே தெரியும்.
:::

::: section analysis-tracking
title: பகுப்பாய்வில் உள்ள batch-களைப் பின்தொடர்வது
routes: /Lab/Analysis
for: 
roles: 
designations: 
source-hash: eb674502e680b760
confidence: ok

**இந்தத் திரை எதற்கு**
**Analysis Tracking** திரையில் எல்லா batch-களும் இருக்கும். ஒவ்வொரு batch-ஐயும் உருவாக்கியது முதல் பகுப்பாய்வு முடியும் வரை பின்தொடரலாம், தாமதமான batch-களைக் கண்டுபிடிக்கலாம்.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Batches**, **Created**, **Taken for Analysis**, **Analysis Completed**, **Completed** மற்றும் **Delayed Analysis**.
2. தேடல் பெட்டியில் batch எண், consignment எண் அல்லது அனலிஸ்ட் பெயரை டைப் செய்யுங்கள்.
3. **All Statuses**-ல் ஒரு நிலையைத் தேர்வு செய்யுங்கள். தாமதமான batch-களை மட்டும் பார்க்க **Delayed** தேர்வு செய்யுங்கள்.
4. **Analysis Days** நெடுவரிசையைப் படியுங்கள்:
- **Pending** (மஞ்சள்): பகுப்பாய்வு இன்னும் தொடங்கவில்லை.
- பச்சை நிற நாட்கள் எண்ணிக்கை: பகுப்பாய்வு நேரத்துக்குள் நடக்கிறது.
- சிவப்பு நிற நாட்கள் எண்ணிக்கை: batch தாமதமாகிவிட்டது (பொதுவாக 5 நாட்களுக்கு மேல் முடியாமல் இருந்தால்).
5. **Batch Details**-ஐத் திறக்க batch எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள்.
6. மேலும் batch-களைப் பார்க்க கீழே உள்ள பக்க எண்களைப் பயன்படுத்துங்கள், அல்லது ஒரு பக்கத்தில் எத்தனை வரிசைகள் தெரிய வேண்டும் என்பதை மாற்றுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No batches found" என்று வருகிறது.** நிலையை அல்லது தேடலை மாற்றுங்கள், அல்லது **Clear filters** அழுத்துங்கள். batch எதுவுமே இல்லை என்றால், **Go to consignments** அழுத்தி ஒன்றை உருவாக்குங்கள்.
- **அனலிஸ்ட் முடித்துவிட்டார், ஆனால் batch Delayed என்று காட்டுகிறது.** எல்லா மாதிரிகளும் சமர்ப்பிக்கப்பட்டு பகுப்பாய்வு முடியும் வரை அது தாமதம் என்றே காட்டும்.
:::

::: section batch-details
title: Batch-ஐ ஒதுக்குவது, பின்தொடர்வது, முடிப்பது
routes: /Lab/Analysis/{id}
for: 
roles: 
designations: 
source-hash: ad50ad4baaed5137
confidence: ok

**இந்தத் திரை எதற்கு**
**Batch Details** திரை ஒரு batch-ன் எல்லா விவரங்களையும் காட்டும்: அதன் மாதிரிகள், பரிசோதனை அளவுருக்கள் (parameters), ஆவணங்கள், வரலாறு. ஒருங்கிணைப்பாளர்கள் இங்கேயே அனலிஸ்டை ஒதுக்கி batch-ஐ முடிப்பார்கள்.

## படிகள்

1. மேலே உள்ள பட்டையில் **Batch No.**, **Consignment No.**, **Total Samples**, **Assigned Date**, **Current Status**, **Analysis Days** தெரியும்.
2. டேப்களைப் பயன்படுத்துங்கள்: **Overview** (முன்னேற்றம், மாதிரிச் சுருக்கம், timeline), **Samples**, **Parameters**, **Documents** மற்றும் **Activity Log**.
3. batch-ஐ ஒரு அனலிஸ்டுக்குக் கொடுக்க **Assign** அழுத்துங்கள் (அனலிஸ்டை மாற்ற **Reassign**). **Assign To** மற்றும் **Priority** தேர்வு செய்து, **Save** அழுத்துங்கள்.
4. batch-க்கு அனலிஸ்ட் இருந்தும் இன்னும் தொடங்கவில்லை என்றால், **Take for Analysis** அழுத்தி உறுதி செய்யுங்கள்.
5. **Samples** டேபில், ஒரு மாதிரியின் பரிசோதனை மதிப்புகளைப் பார்க்க அதைத் தட்டுங்கள். பட்டியலைப் பதிவிறக்க **Export** அழுத்துங்கள்.
6. **Documents** டேபில், **Upload** அழுத்தி, **Files**, **Document Type**, ஒரு **Description** தேர்வு செய்து, மீண்டும் **Upload** அழுத்துங்கள். ஆவணத்தைப் பதிவிறக்க, பார்க்க அல்லது நீக்க ஐகான்களைப் பயன்படுத்துங்கள்.
7. எல்லா மாதிரிகளும் சமர்ப்பிக்கப்பட்டதும், "Every sample is completed" (எல்லா மாதிரிகளும் முடிந்தன) என்று ஒரு குறிப்பு வரும். **Complete Batch** அழுத்துங்கள்.
8. **Final Remarks** டைப் செய்து **Complete Batch** அழுத்துங்கள். மதிப்புகள் பூட்டப்பட்டு அறிக்கைகள் உருவாகும்.

## பொதுவான பிரச்சனைகள்

- **Complete Batch பட்டன் இல்லை.** சில மாதிரிகள் இன்னும் சமர்ப்பிக்கப்படவில்லை. **Samples** டேபில் **Not Started** அல்லது **In Progress** உள்ளதா என்று பாருங்கள்.
- **பட்டன்கள் இல்லை.** batch ஏற்கனவே **Completed** ஆகிவிட்டது, அல்லது உங்களுக்கு ஒருங்கிணைப்பாளர் உரிமை இல்லை.
- **ஒரு கோப்பு சேர்க்கப்படவில்லை.** கோப்புகள் PDF, JPG, PNG, WEBP, XLSX அல்லது DOCX ஆக இருக்க வேண்டும், ஒவ்வொன்றும் 20 MB வரை, ஒரு முறைக்கு 10 கோப்புகள் வரை.
:::

::: section lab-reports
title: லேப் அறிக்கைகளைப் பதிவிறக்குவது
routes: /Lab/Reports
for: 
roles: 
designations: 
source-hash: 4669cdab3bbe7c4e
confidence: ok

**இந்தத் திரை எதற்கு**
batch முடிந்ததும் உருவாகும் அறிக்கைகள் **Lab Reports** திரையில் இருக்கும். இங்கிருந்து ஒரு அறிக்கையின் விவரங்களைத் திறக்கலாம், விவசாயி படிக்கும் மொழியில் PDF அல்லது Excel ஆகப் பதிவிறக்கலாம்.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Reports**, **Batch Reports**, **Sample Reports**, **Generated Today** மற்றும் **Downloads Completed**.
2. பட்டியலைச் சுருக்க **Filters** அழுத்துங்கள். **From** மற்றும் **To** தேதிகள், **State**, **Sample Type** அல்லது **Financial Year** தேர்வு செய்து, **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
3. தேடல் பெட்டியில் batch, consignment அல்லது அறிக்கை எண்ணை டைப் செய்யுங்கள்.
4. **Lab Report Details**-ஐத் திறக்க கண் ஐகானைத் தட்டுங்கள் (போனில் **View Report**).
5. விவரங்களில் **Report Summary**, **Batch Details**, **Consignment Details**, **Analysis Details**, **Status Timeline** ஆகியவை தெரியும்.
6. batch அறிக்கையை ஆங்கிலத்தில் பெற **Download PDF** அழுத்துங்கள். வேறு மொழியில் பெற, அதன் பக்கத்தில் உள்ள சிறிய அம்புக்குறியை அழுத்தி, பட்டியலில் **Tamil**, **Telugu**, **Marathi** அல்லது வேறு மொழியைத் தேர்வு செய்யுங்கள்.
7. அதே போல **Download Excel** அழுத்துங்கள். ஒவ்வொரு மாதிரி அறிக்கைக்கும் ஒரு Excel கோப்பு கிடைக்கும்.
8. பட்டியலுக்குத் திரும்ப **Close** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No reports found" என்று வருகிறது.** batch முடிந்த பிறகுதான் அறிக்கைகள் வரும். உங்கள் ஃபில்டர்களையும் சரிபாருங்கள்.
- **பதிவிறக்க பட்டன்கள் சாம்பல் நிறத்தில் உள்ளன.** அறிக்கை இன்னும் தயாராகவில்லை.
- **PDF ஆங்கிலத்தில் வந்தது.** இந்த அறிக்கைக்கு அந்த மொழி இன்னும் தயாராகாமல் இருக்கலாம். அதனால் ஆங்கிலப் பிரதி கொடுக்கப்பட்டது.
:::

::: section analyst-dashboard
title: லேப் அனலிஸ்ட் டாஷ்போர்டு
routes: /Lab/Analyst
for: 
roles: 
designations: 
source-hash: 976c980ff899d680
confidence: ok

**இந்தத் திரை எதற்கு**
**Lab Analyst Dashboard** உங்களுக்கு ஒதுக்கப்பட்ட வேலையையும், முதலில் எதைச் செய்ய வேண்டும் என்பதையும் காட்டும். ஒரு அனலிஸ்ட் **Lab**-ஐத் திறக்கும்போது, ஆப் இந்தத் திரையைக் காட்டும்.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Assigned Batches**, **Pending Value Entry** (மதிப்புகளுக்காகக் காத்திருக்கும் மாதிரிகள்), **Auto Result Ready** (முடிவு கணக்கிடப்பட்ட மாதிரிகள்), **Reports Generated** (இந்த நிதியாண்டு) மற்றும் **Previous FY Reports**.
2. **Today's Work Priority**-ஐப் பாருங்கள்:
- **Pending value entry batches** (High): மதிப்புகளுக்காகக் காத்திருக்கும் batch-களைப் பார்க்க **Entry Now** அழுத்துங்கள்.
- **Auto result ready for review** (Medium): முடிவுகளைச் சரிபார்க்க **Preview** அழுத்துங்கள்.
- **Generated reports for download** (Low): **View Reports** அழுத்துங்கள்.
3. அல்லது **Quick Actions** பயன்படுத்துங்கள்: **Go to Batch List**, **Continue Test Entry** (நீங்கள் கடைசியாக வேலை செய்த batch-ஐத் திறக்கும்), **View Test Reports** மற்றும் **View Previous FY Reports**.
4. சமீபத்திய எண்ணிக்கைகளைப் பெற **Refresh** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **எல்லா எண்களும் பூஜ்ஜியமாக உள்ளன.** இன்னும் எந்த batch-ம் உங்களுக்கு ஒதுக்கப்படவில்லை. உங்கள் லேப் ஒருங்கிணைப்பாளரிடம் கேளுங்கள்.
- **"Your dashboard could not be loaded" என்று வருகிறது.** இணைப்பைச் சரிபார்த்து **Try again** அழுத்துங்கள்.
:::

::: section analyst-batch-list
title: எனக்கு ஒதுக்கிய batch-களைக் கண்டுபிடிப்பது
routes: /Lab/TestEntry
for: 
roles: 
designations: 
source-hash: 3ec8fc5442d5b1f7
confidence: ok

**இந்தத் திரை எதற்கு**
**Test Value Entry** திரையில் உங்களுக்கு ஒதுக்கிய batch-கள் இருக்கும். பரிசோதனை மதிப்புகளை உள்ளிடுவதை இங்கிருந்து தொடங்குங்கள் அல்லது தொடருங்கள்.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Assigned Batches**, **Pending Value Entry**, **Auto Result Ready** மற்றும் **Report Generated**.
2. பட்டியலைச் சுருக்க, **Filters**-ஐத் திறந்து **Date Range**, **Sample Type**, **Status**, **Priority**, **Financial Year** அல்லது **Assigned By** தேர்வு செய்யுங்கள். **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
3. தேடல் பெட்டியில் batch அல்லது consignment எண்ணை டைப் செய்யுங்கள்.
4. **Assigned Batch List**-ல் **Pending Entries**, **Completed Entries**, **Priority**, **Status** ஆகியவற்றைப் பாருங்கள். **High** முன்னுரிமை batch-களை முதலில் செய்யுங்கள்.
5. batch-ஐத் திறக்க ஒரு வரிசையை, அல்லது **Action** நெடுவரிசையில் உள்ள ஐகானைத் தட்டுங்கள். ஐகானின் பொருள்:
- **Start entry**: நிலை **Pending Entry**, இன்னும் எதுவும் உள்ளிடவில்லை.
- **Continue entry**: நிலை **In Progress**, சில மதிப்புகள் சேமிக்கப்பட்டுள்ளன.
- **Preview results**: நிலை **Auto Result Ready**, எல்லா மதிப்புகளும் சமர்ப்பிக்கப்பட்டுவிட்டன.
- **View**: நிலை **Report Generated**, batch மூடப்பட்டுவிட்டது.

## பொதுவான பிரச்சனைகள்

- **ஒரு batch காணவில்லை.** அது வேறு அனலிஸ்டுக்கு ஒதுக்கப்பட்டிருக்கலாம், அல்லது ஒரு ஃபில்டர் அதை மறைக்கலாம். **Clear filters** அழுத்துங்கள்.
- **"Batches could not be loaded" என்று வருகிறது.** இணைப்பைச் சரிபார்த்து **Try again** அழுத்துங்கள்.
:::

::: section enter-test-values
title: ஒரு மாதிரிக்குப் பரிசோதனை மதிப்புகளை உள்ளிடுவது
routes: /Lab/TestEntry/{id}
for: 
roles: 
designations: 
source-hash: 0543541df7d26c4a
confidence: ok

**இந்தத் திரை எதற்கு**
**Sample-wise Entry**-ல் ஒரு batch-ல் உள்ள ஒவ்வொரு மாதிரியின் பரிசோதனை மதிப்புகளையும் டைப் செய்வீர்கள். சாதாரண வரம்பு (normal range), முடிவு, நிலை, பரிந்துரை ஆகியவற்றை சிஸ்டம் தானாக நிரப்பும்.

## படிகள்

1. மேலே உள்ள **Batch Summary**-ஐச் சரிபாருங்கள்.
2. **Samples in this Batch**-ல், வேலை செய்ய வேண்டிய மாதிரியின் ஐகானைத் தட்டுங்கள், அல்லது **Next pending sample** அழுத்துங்கள்.
3. **Selected Sample** விவரங்களைச் சரிபாருங்கள் (விவசாயி, கிராமம், பயிர்).
4. **Test Value Entry Table**-ல், ஒவ்வொரு மதிப்பையும் **Test Value**-ல் டைப் செய்யுங்கள். **Texture**-க்கு பட்டியலில் இருந்து தேர்வு செய்யுங்கள். **Auto-calculated** என்று குறிக்கப்பட்ட வரிசைகள் தானாக நிரம்பும்.
5. நீங்கள் டைப் செய்யும்போது **Result**, **Status**, **Recommendation Hint** மாறுவதைப் பாருங்கள். கீழே **Recommendation Preview** மற்றும் **Fertilizer Schedule (Kg/acre)**-ஐப் பாருங்கள்.
6. முடிக்காத வேலையை வைத்துக்கொள்ள **Save Draft** அழுத்துங்கள்.
7. எல்லா மதிப்புகளும் உள்ளிட்டதும், **Submit Values** அழுத்தி, பிறகு **Submit Test Values?** பெட்டியில் **Submit** அழுத்துங்கள்.
8. **Submitted Successfully**-ல், **Preview Report** அல்லது **Cancel** அழுத்தி, பிறகு அடுத்த மாதிரியைத் தேர்வு செய்யுங்கள்.
9. சமர்ப்பித்த மாதிரியைத் திருத்த **Edit Values** அழுத்துங்கள். batch முடியும் வரை மட்டுமே இது வேலை செய்யும்.

## புலங்களின் விளக்கம்

- **Normal Range**: இந்தப் பரிசோதனைக்கான நல்ல வரம்பு.
- **Status**: உங்கள் மதிப்பைப் பொறுத்து **Normal**, **Deficient**, **Moderate** அல்லது **Excess**.
- **Overall Soil Status** (அல்லது **Overall Water Status**): எல்லா மதிப்புகளையும் பொறுத்து **Good**, **Needs Improvement** அல்லது **Poor**.

## பொதுவான பிரச்சனைகள்

- **"Enter every parameter before submitting."** (சமர்ப்பிக்கும் முன் எல்லா அளவுருக்களையும் உள்ளிடுங்கள்) செய்தியில் சொல்லப்பட்ட அளவுருக்களை நிரப்புங்கள்.
- **"Fix the values that are not numbers."** (எண் அல்லாத மதிப்புகளைச் சரி செய்யுங்கள்) இலக்கங்களையும் புள்ளியையும் மட்டுமே பயன்படுத்துங்கள்.
- **"Leave this sample?"** (இந்த மாதிரியை விட்டுப் போகவா?) உங்கள் மதிப்புகள் சேமிக்கப்படவில்லை. **Stay** அழுத்தி, பிறகு **Save Draft** அழுத்துங்கள்.
- **எல்லாம் பூட்டப்பட்டுள்ளது.** batch முடிந்துவிட்டது. **View Report** அழுத்துங்கள்.
:::

::: section analyst-test-reports
title: பரிசோதனை அறிக்கைகள் மற்றும் முந்தைய ஆண்டு அறிக்கைகள்
routes: /Lab/TestEntry/Reports, /Lab/TestEntry/Reports/Previous
for: 
roles: 
designations: 
source-hash: 40645f32067eb759
confidence: ok

**இந்தத் திரை எதற்கு**
**Test Reports** திரை இந்த நிதியாண்டில் (ஏப்ரல் முதல் மார்ச் வரை) உங்கள் ஒவ்வொரு batch-க்குமான அறிக்கைச் சுருக்கத்தைக் காட்டும். **Previous FY Reports** முந்தைய ஆண்டுகளுக்கு இதையே காட்டும்.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Batches**, **Total Sample Reports**, **Reports Generated Today** மற்றும் **Downloads Completed**.
2. பழைய அறிக்கைகளைப் பார்க்க **Previous FY Reports** அழுத்துங்கள். திரும்ப வர **Current FY Reports** அழுத்துங்கள்.
3. பட்டியலைச் சுருக்க, **Filters**-ஐத் திறந்து **Financial Year**, **Date Range**, **Sample Type**, **State**, **Region** அல்லது **Head Quarters** தேர்வு செய்யுங்கள். **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
4. தேடல் பெட்டியில் batch அல்லது அறிக்கை எண்ணை டைப் செய்யுங்கள்.
5. **Batch-wise Report Summary**-ல் ஒவ்வொரு batch-க்கும் **Reports Generated** மற்றும் **Pending Reports**-ஐப் பாருங்கள்.
6. ஒவ்வொரு மாதிரியின் அறிக்கையும் உள்ள **Batch Report Details**-ஐத் திறக்க ஒரு வரிசையை, அல்லது **View reports** ஐகானைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No batch reports found" என்று வருகிறது.** ஒருங்கிணைப்பாளர் batch-ஐ முடித்த பிறகுதான் அறிக்கைகள் வரும். நிதியாண்டையும் ஃபில்டர்களையும் சரிபாருங்கள்.
- **"Reports are not available" என்று வருகிறது.** அறிக்கை சேவை பதில் தரவில்லை. சிறிது நேரம் கழித்து மீண்டும் முயற்சி செய்யுங்கள்.
- **ஒரு பழைய அறிக்கை காணவில்லை.** **Previous FY Reports**-ஐத் திறந்து அதன் ஆண்டைத் தேர்வு செய்யுங்கள்.
:::

::: section batch-report-details
title: ஒரு batch-ன் மாதிரி அறிக்கைகளைத் திறந்து பதிவிறக்குவது
routes: /Lab/TestEntry/Reports/{id}
for: 
roles: 
designations: 
source-hash: 683ffbc338437756
confidence: ok

**இந்தத் திரை எதற்கு**
**Batch Report Details** ஒரு batch-ல் உள்ள ஒவ்வொரு மாதிரியின் அறிக்கையையும் பட்டியலிடும். ஒவ்வொரு மாதிரிக்கும் அதன் விவசாயிக்கான தனி அறிக்கை உண்டு. இங்கே அறிக்கைகளைப் பார்க்கலாம், பதிவிறக்கலாம், அச்சிட்டதாகக் குறிக்கலாம்.

## படிகள்

1. கார்டுகளைப் பாருங்கள்: **Total Samples**, **Soil Samples**, **Water Samples**, **Reports Generated** மற்றும் **Downloads Completed**.
2. **Batch Information**-ஐச் சரிபாருங்கள்: batch, consignment, State, Region, **Head Quarters**, **Date of Dispatch** மற்றும் **Financial Year**.
3. ஒரு மாதிரியைக் கண்டுபிடிக்க **Sample Type**, **Status**, **Crops**, **Villages** ஃபில்டர்களைப் பயன்படுத்துங்கள்.
4. ஒரு மாதிரி அறிக்கையைத் திரையில் திறக்க **View** அழுத்துங்கள். அதில் முடிவுகள், பரிந்துரைகள், உர அட்டவணை ஆகியவை இருக்கும்.
5. ஒரு அறிக்கையை ஆங்கிலத்தில் சேமிக்க **Download PDF** அழுத்துங்கள். **Tamil**, **Telugu** அல்லது **Marathi** போன்ற வேறு மொழியைத் தேர்வு செய்ய அதன் பக்கத்தில் உள்ள சிறிய அம்புக்குறியை அழுத்துங்கள்.
6. batch-ன் எல்லா அறிக்கைகளையும் ஒரே நேரத்தில் சேமிக்க **Download All Reports** அழுத்துங்கள். மொழியைத் தேர்வு செய்ய அதன் அம்புக்குறியைப் பயன்படுத்துங்கள்.
7. விவசாயிக்காக அறிக்கையை அச்சிட்ட பிறகு **Mark printed** அழுத்துங்கள். அதன் **Report Status** **Printed** ஆக மாறும்.

## புலங்களின் விளக்கம்

- **Report Status**: **Generated** (தயார்), **Downloaded** (குறைந்தது ஒரு முறை சேமிக்கப்பட்டது) அல்லது **Printed**.
- **Reports No.**: விவசாயியின் அறிக்கையில் அச்சிடப்படும் அறிக்கை எண்.

## பொதுவான பிரச்சனைகள்

- **"This batch report is not available" என்று வருகிறது.** batch இன்னும் முடியவில்லை. மாதிரிகளைச் சரிபார்க்க **Open the batch** அழுத்துங்கள்.
- **Mark printed சாம்பல் நிறத்தில் உள்ளது.** அறிக்கை ஏற்கனவே **Printed** என்று குறிக்கப்பட்டுவிட்டது.
:::

::: section lab-tracking
title: அட்மின்களுக்கான லேப் கண்காணிப்பு
routes: /Lab/Tracking
for: 
roles: 
designations: 
source-hash: dde8a3beb72cd9d3
confidence: ok

**இந்தத் திரை எதற்கு**
**Lab Tracking** என்பது லேபில் உள்ள எல்லா batch-களுக்குமான அட்மின் பார்வை. consignment-கள், batch-கள், பகுப்பாய்வு முன்னேற்றம் ஆகியவற்றைக் கண்காணிக்க இதைப் பயன்படுத்துங்கள். இங்கே எதையும் மாற்ற முடியாது.

## படிகள்

1. மேலே உள்ள கார்டுகளைப் பாருங்கள்: **Total Batches**, **Batch Created**, **Taken for analysis** மற்றும் **Analysis Completed**.
2. ஒரு நிலையை மட்டும் காட்ட டேப்களைப் பயன்படுத்துங்கள்: **All Batches**, **Batch Created**, **Taken for Analysis** அல்லது **Analysis Completed**. ஒவ்வொரு டேபும் அதன் எண்ணிக்கையைக் காட்டும்.
3. பட்டியலைச் சுருக்க, **Filters**-ஐத் திறந்து **Date Range**, **Batch No.**, **Consignment No.**, **Assigned To**, **State**, **Region** அல்லது **Current Stage** தேர்வு செய்யுங்கள். **Apply Filters** அழுத்துங்கள். அவற்றை நீக்க **Clear All** அழுத்துங்கள்.
4. **Lab Tracking List**-க்கு மேலே உள்ள தேடல் பெட்டியில் batch எண், consignment எண் அல்லது அனலிஸ்ட் பெயரை டைப் செய்யுங்கள்.
5. **Analysis Days**-ஐப் படியுங்கள்: **Pending** (மஞ்சள்) என்றால் தொடங்கவில்லை, பச்சை என்றால் நேரத்துக்குள், சிவப்பு என்றால் தாமதம்.
6. batch கடைசியாக எப்போது நகர்ந்தது என்று பார்க்க **Last Updated**-ஐப் பாருங்கள்.
7. batch பார்வையைத் திறக்க batch எண்ணை அல்லது கண் ஐகானைத் தட்டுங்கள்.

## பொதுவான பிரச்சனைகள்

- **"No batches found" என்று வருகிறது.** **Clear filters** அழுத்தி **All Batches** டேபைத் தேர்வு செய்யுங்கள்.
- **என் மெனுவில் Lab Tracking இல்லை.** உங்கள் பதவிக்கு Lab Tracking பக்கம் தேவை. Admin role மட்டும் போதாது. உங்கள் நிர்வாகியிடம் கேளுங்கள்.
:::

::: section batch-view
title: அட்மினாக ஒரு batch-ஐப் பார்ப்பது
routes: /Lab/Tracking/{id}
for: 
roles: 
designations: 
source-hash: afb705e77b16edb3
confidence: ok

**இந்தத் திரை எதற்கு**
Batch பார்வை, ஒரு batch-ன் முழுக் கதையையும் ஒரே பக்கத்தில் அட்மினுக்குக் காட்டும்: மாதிரிகள் எங்கிருந்து வந்தன, கூரியர், batch, பகுப்பாய்வு முன்னேற்றம், timeline. இதில் பார்க்க மட்டுமே முடியும்.

## படிகள்

1. மேலே உள்ள பட்டையில் **Batch No.**, **Consignment No.**, **Test Samples**, **Samples**, **Assigned To (Analyst)**, **Batch Created On**, **Last Updated** தெரியும்.
2. **Source Details** மாதிரிகளைச் சேகரித்தவர் யார், அவரது **Mobile No.**, **Location**, **Collection Date** ஆகியவற்றைக் காட்டும்.
3. **Consignment Details** **Dispatch Date**, **AWB / Tracking No**, **Expected Delivery Date** ஆகியவற்றைக் காட்டும். கூரியர் தளத்தில் பார்சலைப் பின்தொடர டிராக்கிங் எண்ணைத் தட்டுங்கள்.
4. **Batch Details** மாதிரி எண்ணிக்கைகள், **Batch Created By**, **Batch Created Date**, **Assigned Analyst** ஆகியவற்றைக் காட்டும்.
5. **Analysis Process** ஒவ்வொரு மாதிரி வகைக்கும் எத்தனை மாதிரிகள் **Completed**, எத்தனை **Not Started** என்று, ஒரு **Completed %** பட்டையுடன் காட்டும்.
6. **Timeline** ஒவ்வொரு படியையும் தேதி மற்றும் செய்தவர் பெயருடன் காட்டும்: **Consignment Delivered to Lab**, **Lab Entry Created**, **Batch Created**, **Taken for Analysis**, **Analysis Completed** மற்றும் **Report Generated**. சாம்பல் நிறப் படிகள் இன்னும் நடக்கவில்லை.
7. batch மாதிரிகளை Excel கோப்பாகப் பதிவிறக்க **Export** அழுத்துங்கள்.
8. பட்டியலுக்குத் திரும்ப **Back to Lab Tracking** அழுத்துங்கள்.

## பொதுவான பிரச்சனைகள்

- **"The batch could not be loaded" என்று வருகிறது.** batch நீக்கப்பட்டிருக்கலாம், அல்லது உங்களுக்கு Lab Tracking இல்லாமல் இருக்கலாம். **Try again** அழுத்துங்கள்.
:::
