---
module: dealer-application
title: डीलर आवेदन
icon: bi-shop
order: 101
language: hi
category: डीलर
note: नई गाइड
meta-source-hash: 95f84553d50218f7
---

::: section overview
title: यह मॉड्यूल आपके लिए क्या करता है
routes: 
for: 
roles: 
designations: 
kind: overview
source-hash: b3660f0cee5decd6
confidence: ok

13 छोटे स्टेप में डीलर का रजिस्ट्रेशन करें: डीलर कौन है, उसका कारोबार, मालिक, पैसों की जानकारी और दस्तावेज़। फिर आवेदन सबमिट करें। डीलर मंज़ूर होने से पहले RM, SMM और AVP इसे जाँचते हैं।

- steps | icon: bi-list-ol | color: #7c3aed
  title: 13 स्टेप, एक बार में एक
  text: Next दबाने पर हर स्टेप सेव हो जाता है। आप बाद में वापस आकर उसे पूरा कर सकते हैं।
- check | icon: bi-clipboard-check | color: #d97706
  title: आख़िर में एक जाँच
  text: सबमिट करने से पहले आख़िरी स्टेप दिखाता है कि क्या बाकी है।
- approve | icon: bi-shield-check | color: #16a34a
  title: तीन मंज़ूरियाँ
  text: पहले RM, फिर SMM, फिर AVP आवेदन मंज़ूर करते हैं।
:::

::: section flow
title: आवेदन कैसे आगे बढ़ता है, और कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: flow
source-hash: 2413a14b313769d3
confidence: ok

हर लाइन एक रोल है और हर बॉक्स एक स्टेप है। आपकी लाइन पर "You" लिखा होता है। किसी स्टेप पर टैप करके देखें कि वहाँ क्या होता है।

- applicant | type: lane | color: #7c3aed | roles: MO, MDO, JMDO, Admin
  title: आवेदक
  sub: MO · MDO · JMDO (नया डीलर: Admin · AVP)
- rm-team | type: lane | color: #0d9488 | roles: RM, RMD
  title: RM
  sub: रीजन
- smm-team | type: lane | color: #d97706 | roles: SMD, SMM
  title: SMM
  sub: राज्य
- avp-team | type: lane | color: #2563eb | roles: AVP
  title: AVP
  sub: आख़िरी मंज़ूरी
- office-team | type: lane | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  sub: सिर्फ़ नए डीलर के लिए
- register | lane: applicant | col: 1 | icon: bi-person-plus | color: #64748b | next: steps | screen: register
  title: रजिस्ट्रेशन शुरू करें
  status: Draft
  text: **Create Existing Dealer** या **Create New Dealer** दबाएँ और पहला स्टेप, **Dealer Registration**, भरें।
- steps | lane: applicant | col: 2 | icon: bi-list-ol | color: #7c3aed | next: documents | screen: experience
  title: कारोबार वाले स्टेप भरें
  status: Registration In Progress
  text: अनुभव, बिक्री, गोदाम, बाज़ार, कंपनियाँ, मालिक, बिक्री का प्लान, संपत्ति और क्रेडिट लिमिट भरें। हर स्टेप पर **Next** दबाएँ।
- documents | lane: applicant | col: 3 | icon: bi-paperclip | color: #7c3aed | next: submit | screen: enclosures
  title: दस्तावेज़ अपलोड करें
  status: Registration In Progress
  text: **Enclosures** स्टेप पर जिस भी दस्तावेज़ पर **Required** लिखा है, उसे अपलोड करें।
- submit | lane: applicant | col: 4 | icon: bi-send | color: #0b6b3a | next: rm | screen: final-submission
  title: जाँचें और सबमिट करें
  status: Application Ready for Submission
  text: देख लें कि हर हिस्सा पूरा है और **Submit Registration** दबाएँ।
- incomplete | lane: applicant | col: 5 | icon: bi-exclamation-triangle | color: #dc2626 | kind: exception | from: submit | to: steps | screen: final-submission
  title: कुछ बाकी है
  text: अगर कोई हिस्सा या दस्तावेज़ बाकी है, तो ऐप आवेदन सबमिट नहीं करता। पेंसिल से वह हिस्सा खोलें, उसे ठीक करें और फिर से सबमिट करें।
- rm | lane: rm-team | col: 5 | icon: bi-hourglass-split | color: #0d9488 | next: smm | return: submit
  title: RM जाँचते हैं
  status: In RM
  text: RM आवेदन मंज़ूर करते हैं, या रिजेक्ट करके आवेदक को लौटा देते हैं।
- smm | lane: smm-team | col: 6 | icon: bi-hourglass-split | color: #d97706 | next: avp | return: rm
  title: SMM जाँचते हैं
  status: In SMM
  text: SMM इसे मंज़ूर करते हैं, या रिजेक्ट करके RM को लौटा देते हैं।
- avp | lane: avp-team | col: 7 | icon: bi-patch-check | color: #2563eb | next: office | return: smm
  title: AVP मंज़ूर करते हैं
  status: In AVP
  text: AVP आख़िरी मंज़ूरी देते हैं, या रिजेक्ट करके SMM को लौटा देते हैं।
- office | lane: office-team | col: 8 | icon: bi-123 | color: #64748b | next: done
  title: नए डीलर के डीलर ID
  status: In AVP Office
  text: सिर्फ़ नए डीलर के लिए AVP Office कंपनी के डीलर ID सेव करता है। पुराने डीलर के लिए यह स्टेप नहीं होता।
- done | lane: applicant | col: 9 | icon: bi-check-circle | color: #16a34a | screen: application-pdf
  title: डीलर मंज़ूर हो गया
  status: Approved
  text: आवेदन पूरा हो गया। आप डीलर कार्ड से डीलरशिप का आवेदन फ़ॉर्म खोल सकते हैं।
:::

::: section statuses
title: हर स्टेटस जो आपको दिख सकता है
routes: 
for: 
roles: 
designations: 
kind: statuses
source-hash: 329a69141c3bcda8
confidence: ok

- draft | color: #64748b
  label: Draft
  text: सेव है पर अभी सबमिट नहीं हुआ। आप अभी भी इसे बदल सकते हैं।
- in-progress | color: #7c3aed
  label: Registration In Progress
  text: आप स्टेप भर रहे हैं।
- ready | color: #0b6b3a
  label: Application Ready for Submission
  text: हर हिस्सा और दस्तावेज़ पूरा है। आप सबमिट कर सकते हैं।
- in-rm | color: #0d9488
  label: In RM
  text: सबमिट हो गया। RM इसे जाँच रहे हैं।
- in-smm | color: #d97706
  label: In SMM
  text: RM ने मंज़ूर कर दिया। SMM इसे जाँच रहे हैं।
- in-avp | color: #2563eb
  label: In AVP
  text: SMM ने मंज़ूर कर दिया। AVP का इंतज़ार है।
- in-office | color: #64748b
  label: In AVP Office
  text: AVP ने नया डीलर मंज़ूर कर दिया। कंपनी के डीलर ID का इंतज़ार है।
- approved | color: #16a34a
  label: Approved
  text: आवेदन पूरी तरह मंज़ूर हो गया है।
- rejected-rm | color: #dc2626
  label: Rejected by RM
  text: आवेदक को लौटा दिया गया। इसे ठीक करें और फिर से सबमिट करें।
- rejected-smm | color: #dc2626
  label: Rejected by SMM
  text: ठीक करने के लिए RM को लौटा दिया गया।
- rejected-avp | color: #dc2626
  label: Rejected by AVP
  text: ठीक करने के लिए SMM को लौटा दिया गया।
- uploaded | color: #16a34a
  label: Uploaded
  text: दस्तावेज़ जुड़ गया है।
- missing | color: #ea580c
  label: Missing
  text: दस्तावेज़ अभी जुड़ा नहीं है।
- consent | color: #0891b2
  label: Consent Provided
  text: डीलर ने क्रेडिट लिमिट की जानकारी नहीं दी। क्रेडिट लिमिट वाले स्टेप छोड़ दिए जाते हैं।
- active | color: #16a34a
  label: Active
  text: डीलर अभी कारोबार कर रहा है।
- inactive | color: #d97706
  label: Inactive
  text: डीलर अभी कारोबार नहीं कर रहा।
- terminated | color: #475569
  label: Terminated
  text: डीलरशिप ख़त्म हो गई है।
:::

::: section roles
title: कौन क्या करता है
routes: 
for: 
roles: 
designations: 
kind: roles
source-hash: 17fdf789d027bf63
confidence: ok

आपका कार्ड हाइलाइट होता है। आपको सिर्फ़ वही स्क्रीन दिखती हैं जो आपका रोल इस्तेमाल करता है।

- creator | color: #7c3aed | roles: MO, MDO, JMDO
  title: MO, MDO, JMDO
  - पुराने डीलर का रजिस्ट्रेशन करना
  - सारे स्टेप भरना और दस्तावेज़ अपलोड करना
  - सबमिट करना, और RM का रिजेक्ट किया आवेदन ठीक करना
- admin | color: #2563eb | roles: Admin, AVP
  title: Admin, AVP
  - Create New Dealer से नए डीलर का रजिस्ट्रेशन करना
  - AVP आख़िरी मंज़ूरी देते हैं
- rm | color: #0d9488 | roles: RM, RMD
  title: RM
  - अपने रीजन के आवेदन जाँचना
  - मंज़ूर करना, या रिजेक्ट करके आवेदक को लौटाना
- smm | color: #d97706 | roles: SMD, SMM
  title: SMM
  - अपने राज्य के आवेदन जाँचना
  - मंज़ूर करना, या रिजेक्ट करके RM को लौटाना
- office | color: #64748b | roles: CommonRole | designations: AVP Office
  title: AVP Office
  - मंज़ूर हुए नए डीलर के लिए कंपनी के डीलर ID सेव करना
:::

::: section faq
title: दिक्कतें और जवाब
routes: 
for: 
roles: 
designations: 
kind: faq
source-hash: 30e6b2b2e39499dd
confidence: ok

- warehouse-stuck | popular: true
  q: मैं Warehouse स्टेप से आगे नहीं जा पा रहा
  a: आपको कम से कम दो गोदाम (warehouse), दो रेल हेड और दो पोर्ट चाहिए। हर एक को उसके **Add** बटन से जोड़ें, फिर **Next** दबाएँ।
- not-submitted | popular: true
  q: Submit Registration दबाने पर लिखा आता है कि कुछ हिस्से अधूरे हैं
  a: **Final Submission** पर **Section Completion Status** और **Enclosures** के नीचे नारंगी निशान ढूँढें। पेंसिल पर टैप करें, वह स्टेप पूरा करें और फिर से सबमिट करें।
- red-step
  q: ऊपर स्टेप वाली पट्टी का एक हिस्सा लाल है
  a: उस स्टेप में कोई गड़बड़ है या जानकारी बाकी है। स्टेप खोलकर ठीक करने के लिए लाल हिस्से पर टैप करें।
- ocr
  q: ऐप ने GST, PAN या बैंक नंबर अपने आप भर दिया
  a: ऐप अपलोड की गई फ़ाइल पढ़ता है। नंबर हमेशा जाँच लें। अगर गलत है, तो सही नंबर टाइप करें।
- skipped-credit
  q: Credit Limit वाले स्टेप छूट गए
  a: **Investment** स्टेप पर सहमति (consent) दी गई थी। क्रेडिट लिमिट भरने के लिए **Investment** खोलें और **Remove Consent** दबाएँ।
- no-code
  q: मेरे नए डीलर का Dealer Code नहीं है
  a: नए डीलर को Dealer Code आख़िरी मंज़ूरी के बाद ही मिलता है।
:::

::: section register
title: डीलर का रजिस्ट्रेशन शुरू करें (स्टेप 1)
routes: /Register
for: 
roles: 
designations: 
screenshot: register.png
hotspots: 1 @ 2.2,21.9,95.5,33.1 | 2 @ 2.2,57.9,95.5,32.4 | 3 @ 2.2,93.1,95.5,6.9 | 4 @ - | 5 @ - | 6 @ -
screenshot-phone: register.phone.jpg
hotspots-phone: 1 @ 3.1,14.7,93.8,42.9 | 2 @ 3.1,58.8,93.8,39.3 | 3 @ 3.1,99.2,93.8,0.8 | 4 @ - | 5 @ - | 6 @ -
tour: 1 > [data-help=dr-basic] | 2 > [data-help=dr-business] | 3 > [data-help=dr-location] | 4 > [data-help=dr-bank] | 5 > [data-help=dr-tax] | 6 > [data-help=dr-license]
minutes: 6
source-hash: 1268f37d3a5ce326
confidence: ok

**यह स्क्रीन किस काम की है**
**Dealer Registration** स्क्रीन 13 में से पहला स्टेप है। इसमें दर्ज होता है कि डीलर कौन है, दुकान कहाँ है, बैंक खाता, और टैक्स व लाइसेंस की जानकारी। **Register** मेन्यू खोलें और **Create Existing Dealer** दबाएँ। बिल्कुल नए डीलर के लिए Admin और AVP **Create New Dealer** इस्तेमाल करते हैं।

1. **Basic Information** में **Dealer**, **Department** या **Institution** चुनें, और **SPIC**, **GREEN STAR** या दोनों पर टैप करें। **State**, **Region** और **HQ** देख लें। ये आपकी प्रोफ़ाइल से भरे जाते हैं। पुराने डीलर के लिए **Active**, **Inactive** या **Terminated** भी चुनें।
2. **Business Information** में **Dealer Code & Name** और **Parent Dealer Code & Name** चुनें (सिर्फ़ पुराने डीलर के लिए)। खाद के लाइसेंस पर जैसा लिखा है, वैसा ही **Firm Name** टाइप करें और **Date of Appointment for SPIC** या **for Greenstar** चुनें।
3. **Primary Location** में दुकान पर खड़े होकर **Use Current Location** दबाएँ, या **Google Map URL** पेस्ट करें। फिर पता, **Pincode**, **District**, **State** और तीनों फ़ोन नंबर भरें।
4. **Bank Details** में **Upload Cheque (PDF or Image)** से चेक अपलोड करें। ऐप उसे पढ़कर खाते की जानकारी भर देता है। **Account Holder Name**, **Account Number**, **Bank Name**, **Bank Branch** और **IFSC Code** जाँच लें।
5. **Tax & Compliance** में GST सर्टिफ़िकेट, PAN कार्ड और e-Aadhaar PDF फ़ाइल के रूप में अपलोड करें। ऐप हर फ़ाइल पढ़कर नंबर भर देता है। हर नंबर जाँच लें। **TAN Number** भरना ज़रूरी नहीं है।
6. **Wholesale Fertilizer Details (as per iFMS)** या **Retail Fertilizer Details (as per iFMS)** भरें: लाइसेंस नंबर, **Expiry Date**, **mFMS ID** और लाइसेंस की फ़ाइल। **Office Automation** का जवाब दें, फिर **Next** दबाएँ।

> Tip: पुराने डीलर के लिए **Trade Deposit Details for SPIC** या **for Greenstar** भी भरें: रकम, **Receipt No** और **Deposit Date**।
> Careful: अगर आप **Department** या स्टेटस **Terminated** चुनते हैं, तो यही एक पेज होता है। बटन पर **Submit** लिखा होता है और वह फ़ॉर्म तुरंत जाँच के लिए भेज देता है।

## फ़ील्ड का मतलब

| फ़ील्ड | क्या भरें |
|---|---|
| **Firm Name** | खाद के लाइसेंस पर लिखा कारोबार का नाम। ज़रूरी है। |
| **Pincode** | 6 अंक। ऐप इससे इलाका ढूँढ लेता है। |
| **Official Contact Number**, **WhatsApp Number**, **Alternative Number** | 10 अंक के मोबाइल नंबर। |
| **GST Number** | 15 अक्षर, जैसे 22AAAAA0000A1Z5। |
| **PAN Number** | 10 अक्षर, जैसे ABCDE1234F। |
| **Aadhaar Number** | ठीक 12 अंक। |
| GST, PAN, Aadhaar और TAN की फ़ाइलें | सिर्फ़ PDF, 5 MB से कम। |
| लाइसेंस | कम से कम एक लाइसेंस, होलसेल या रिटेल, उसके सर्टिफ़िकेट के साथ। |

## आम दिक्कतें

- **Next दबाने पर पेज आगे नहीं बढ़ता।** स्टार वाला कोई फ़ील्ड खाली है या गलत भरा है। फ़ील्ड के नीचे लाल मैसेज देखें, जैसे "Pincode must be 6 digits"।
- **"At least one Fertilizer License (Wholesale or Retail) is required"** (कम से कम एक खाद लाइसेंस, होलसेल या रिटेल, ज़रूरी है) एक लाइसेंस भरें और उसका सर्टिफ़िकेट अपलोड करें।
- **"OCR could not extract the required information from this document."** (ऐप इस दस्तावेज़ से जानकारी नहीं पढ़ पाया) ज़्यादा साफ़ फ़ाइल अपलोड करें, या जानकारी ख़ुद टाइप करें।
- **"Only PDF files are allowed" or "File size must be less than 5 MB".** (सिर्फ़ PDF फ़ाइल चलेगी, या फ़ाइल 5 MB से छोटी होनी चाहिए) दस्तावेज़ को छोटी PDF के रूप में सेव करें और फिर से अपलोड करें।
:::

::: section experience
title: खाद के कारोबार का अनुभव (स्टेप 2)
routes: /Experience
for: 
roles: 
designations: 
screenshot: experience.png
screenshot-phone: experience.phone.jpg
minutes: 3
source-hash: 4c4087f2c0c5036d
confidence: ok

**यह स्क्रीन किस काम की है**
**Experience In Fertilizer Trade** स्क्रीन पर दर्ज होता है कि डीलर कितने साल से खाद बेच रहा है, और किन कंपनियों के लिए।

1. **Year of Experience** में भरें कि डीलर कितने साल से खाद के कारोबार में है।
2. **Add Experience** में **Company Name** चुनें और **Number of the years** भरें।
3. उस कंपनी के लिए **Quantity (MT)** और **Turnover (Lakhs)** भरें।
4. अगर डीलर अब उस कंपनी के साथ काम नहीं करता, तो **Still Active** बंद कर दें।
5. **Add Experience** दबाएँ। कंपनी **Saved Experience** के नीचे दिखती है। हर कंपनी के लिए ऐसा ही करें।
6. किसी रिकॉर्ड को बदलने के लिए पेंसिल इस्तेमाल करें और **Update Experience** दबाएँ, या मिटाने के लिए डस्टबिन इस्तेमाल करें। फिर **Next** दबाएँ।

## आम दिक्कतें

- **"Please select a company."** (कृपया कंपनी चुनें) **Add Experience** दबाने से पहले **Company Name** चुनें।
- **स्टेप वाली पट्टी पर स्टेप 2 लाल हो जाता है।** **Year of Experience** भरें और कम से कम एक कंपनी जोड़ें।
:::

::: section annual-sales
title: पिछले साल की बिक्री (स्टेप 3)
routes: /AnnualSales
for: 
roles: 
designations: 
screenshot: annual-sales.png
screenshot-phone: annual-sales.phone.jpg
minutes: 4
source-hash: 3d5342d2000c7789
confidence: ok

**यह स्क्रीन किस काम की है**
**Annual Sales Data For All Companies** स्क्रीन पर दर्ज होता है कि डीलर ने **Last Financial Year** में सभी कंपनियों का मिलाकर कितना बेचा।

1. टेबल के ऊपर किसी कैटेगरी टैब पर टैप करें।
2. किसी प्रोडक्ट ग्रुप को खोलकर उसके प्रोडक्ट देखने के लिए उस पर टैप करें।
3. हर प्रोडक्ट के लिए **Own Retail Qty** भरें। अगर डीलर के पास होलसेल लाइसेंस है, तो **Sale to Dealer Qty** भी भरें।
4. टर्नओवर (**TO (₹)in Lakhs**) और टोटल ऐप अपने आप निकाल देता है। टेबल के नीचे समरी कार्ड और **Grand Total** जाँच लें।
5. अगर कोई प्रोडक्ट लिस्ट में नहीं है, तो **Add Product** दबाएँ, **Category**, **Product Group**, **Product Name** और **Rate Per Unit (RPU)** भरें, और **Save** दबाएँ।
6. **Next** दबाएँ।

## आम दिक्कतें

- **Sale to Dealer Qty कॉलम नहीं दिख रहा।** यह तभी दिखता है जब स्टेप 1 में डीलर का होलसेल लाइसेंस भरा हो।
- **स्टेप वाली पट्टी पर स्टेप 3 लाल हो जाता है।** कम से कम एक प्रोडक्ट की मात्रा भरें।
- **"A Product with the name ... already exists in this Category."** (इस नाम का प्रोडक्ट इस कैटेगरी में पहले से है) प्रोडक्ट पहले से लिस्ट में है। उसे उसके प्रोडक्ट ग्रुप में ढूँढें।
:::

::: section warehouse
title: पास के गोदाम, रेल हेड और पोर्ट (स्टेप 4)
routes: /Warehouse
for: 
roles: 
designations: 
screenshot: warehouse.png
screenshot-phone: warehouse.phone.jpg
minutes: 4
source-hash: bd545fbcf4350bec
confidence: ok

**यह स्क्रीन किस काम की है**
**Warehouse & Logistics** स्क्रीन पर डीलर के सबसे पास की सुविधाएँ दर्ज होती हैं। दूरी उस दुकान की जगह से निकाली जाती है जो आपने स्टेप 1 में दी थी।

1. **Logistics Note Information** पढ़ें। आपको कम से कम दो गोदाम, दो रेल हेड और दो पोर्ट चाहिए।
2. **Warehouse** कार्ड में **Facility Name** खोजकर चुनें। **Distance (KM)** अपने आप भर जाता है।
3. **Freight (₹/ton/Km)** भरें और **Add Warehouse** दबाएँ।
4. **Rail Head** और **Port** कार्ड में भी ऐसा ही करें।
5. **Selected Nearest Facilities** के नीचे की लिस्ट जाँच लें। किसी को बदलने के लिए पेंसिल, या मिटाने के लिए डस्टबिन इस्तेमाल करें।
6. **Next** दबाएँ।

## आम दिक्कतें

- **"A minimum of two Warehouses, two Rail Heads, and two Ports must be configured to proceed further."** (आगे बढ़ने के लिए कम से कम दो गोदाम, दो रेल हेड और दो पोर्ट जोड़ने होंगे) पहले बाकी सुविधाएँ जोड़ें।
- **"Dealer location (Latitude/Longitude) is not set."** (डीलर की जगह नहीं भरी गई है) स्टेप 1 पर वापस जाएँ और **Google Map URL** या **Latitude & Longitude** भरें।
- **Add बटन ग्रे है।** पहले **Facility Name** चुनें, और दूरी दिखने तक रुकें।
:::

::: section market-details
title: बाज़ार की जानकारी (स्टेप 5)
routes: /MarketDetails
for: 
roles: 
designations: 
screenshot: market-details.png
screenshot-phone: market-details.phone.jpg
minutes: 3
source-hash: a7a15b3e04404881
confidence: ok

**यह स्क्रीन किस काम की है**
**Market Details** स्क्रीन पर वे ब्लॉक और गाँव दर्ज होते हैं जहाँ डीलर माल देता है, साथ में वहाँ की फ़सलें और किसान।

1. **Blocks / Villages** में **Name of Block/ Village** टाइप करें।
2. **Major Crops** खोलें और मुख्य फ़सलों पर टिक करें। आप लिस्ट में खोज सकते हैं।
3. **No of Farmer** भरें, और अगर **No of Dealer** बॉक्स दिख रहा है तो उसे भी भरें।
4. **Season From & To Month** और **Irrigation Type** चुनें: **CANAL**, **TANK**, **WELL** या **RAINFED**।
5. **Add Blocks/ Villages** दबाएँ। एंट्री **Added Market Details** के नीचे दिखती है। हर ब्लॉक या गाँव के लिए ऐसा ही करें।
6. **Next** दबाएँ।

## आम दिक्कतें

- **"No regional blocks added yet."** (अभी तक कोई ब्लॉक नहीं जोड़ा गया) कम से कम एक ब्लॉक या गाँव जोड़ें। नहीं तो स्टेप 5 लाल दिखता है।
- **मुझे No of Dealer नहीं दिख रहा।** यह तभी दिखता है जब डीलर के पास होलसेल लाइसेंस हो।
:::

::: section companies
title: डीलर के इलाके की कंपनियाँ (स्टेप 6)
routes: /Companies
for: 
roles: 
designations: 
screenshot: companies.png
screenshot-phone: companies.phone.jpg
minutes: 1
source-hash: 4709346d2ff331c4
confidence: ok

**यह स्क्रीन किस काम की है**
**Companies Operating In Your Place** स्क्रीन दिखाती है कि डीलर के इलाके में कौन-सी खाद कंपनियाँ बिकती हैं।

1. इलाके में बिकने वाली हर कंपनी पर टैप करें, **SPIC** और **GREEN STAR** पर भी। चुनी गई कंपनी पर टिक दिखता है।
2. टिक हटाने के लिए कंपनी पर फिर से टैप करें।
3. **Next** दबाएँ।

## आम दिक्कतें

- **स्टेप वाली पट्टी पर स्टेप 6 लाल हो जाता है।** कम से कम एक कंपनी पर टिक करें।
:::

::: section proprietor
title: मालिक और पार्टनर की जानकारी (स्टेप 7)
routes: /Proprietor
for: 
roles: 
designations: 
screenshot: proprietor.png
minutes: 6
source-hash: 02521efb7ec5b6b9
confidence: ok

**यह स्क्रीन किस काम की है**
**Proprietor Details** स्क्रीन पर कारोबार का कानूनी प्रकार और हर मालिक या पार्टनर, उनके परिवार के साथ, दर्ज होता है। यह जानकारी SDWA कल्याण योजना के लिए भी इस्तेमाल होती है।

1. **Business Entity Type** चुनें: **Sole Proprietor**, **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** या **Society**।
2. **Ownership Information** में **Authorized Name**, **Son of / Daughter of**, **Phone Number**, **PAN number** और **Aadhaar Card Number** भरें।
3. **Proprietor / Managing Partner Image** अपलोड करें और **Residential Address Details** भरें।
4. **Family Members** में हर सदस्य जोड़ें और **Add Details** दबाएँ। **Other Occupation / Agencies** में **Yes** या **No** चुनें और कोई दूसरा कारोबार हो तो उसे जोड़ें।
5. **Save Details** दबाएँ। वह व्यक्ति **Saved Details** के नीचे दिखता है। हर पार्टनर को ऐसे ही जोड़ें।
6. **Next** दबाएँ।

| Business Entity Type | कितने लोग चाहिए |
|---|---|
| **Sole Proprietor** | ठीक 1 मालिक। |
| **Society** | कम से कम 1 अधिकृत व्यक्ति। |
| **Partnership**, **LLP**, **PVT LTD**, **PUB LTD** | कम से कम 2 पार्टनर। |

## आम दिक्कतें

- **"Please upload Proprietor / Managing Partner Image before saving."** (सेव करने से पहले मालिक / मैनेजिंग पार्टनर की फ़ोटो अपलोड करें) फ़ोटो अपलोड करें, फिर से **Save Details** दबाएँ।
- **"Please add at least 2 partners for the selected entity type."** (चुने गए प्रकार के लिए कम से कम 2 पार्टनर जोड़ें) **Next** दबाने से पहले दूसरा पार्टनर सेव करें।
- **एक व्यक्ति सेव करने के बाद फ़ॉर्म गायब हो गया।** **Sole Proprietor** के लिए सिर्फ़ एक ही व्यक्ति चलता है। उसे बदलने के लिए **Saved Details** के नीचे की पेंसिल इस्तेमाल करें।
:::

::: section sales-planning
title: अगले साल की बिक्री का प्लान (स्टेप 8)
routes: /SalesPlaning
for: 
roles: 
designations: 
screenshot: sales-planning.png
screenshot-phone: sales-planning.phone.jpg
minutes: 4
source-hash: b908e1cc20e482d4
confidence: ok

**यह स्क्रीन किस काम की है**
**Sales Planning & Forecast** स्क्रीन पर दर्ज होता है कि अगले वित्त वर्ष में, अप्रैल से मार्च तक, डीलर हर महीने कितना बेचने का प्लान करता है।

1. **Business Plan** में किसी कैटेगरी टैब पर टैप करें।
2. किसी प्रोडक्ट ग्रुप को खोलने के लिए उस पर टैप करें।
3. हर प्रोडक्ट के लिए **April** से **March** तक हर महीने के नीचे प्लान की गई मात्रा भरें। लाख में कीमत और **Total** ऐप अपने आप निकाल देता है।
4. **Monthly Total** वाली लाइन और समरी कार्ड जाँच लें, जैसे **Fertilizer Products Summary**।
5. **Next** दबाएँ।

## आम दिक्कतें

- **स्टेप वाली पट्टी पर स्टेप 8 लाल हो जाता है।** कम से कम एक प्रोडक्ट का प्लान भरें।
:::

::: section investment
title: संपत्ति, कर्ज़ और क्रेडिट की सहमति (स्टेप 9)
routes: /Investment
for: 
roles: 
designations: 
screenshot: investment.png
screenshot-phone: investment.phone.jpg
minutes: 5
source-hash: 6476f16e152121ca
confidence: ok

**यह स्क्रीन किस काम की है**
**Investment** स्क्रीन पर दर्ज होता है कि डीलर के पास क्या है और उस पर कितना कर्ज़ है, और **Net Financial Position** दिखती है।

1. अगर डीलर क्रेडिट लिमिट की जानकारी नहीं देगा, तो **Give Consent** दबाएँ। फिर क्रेडिट लिमिट वाले स्टेप छोड़ दिए जाते हैं और शून्य दर्ज होते हैं। **Remove Consent** से यह वापस हो जाता है।
2. **Assets (Banking & Properties)** में **Banking**, **Land** या **Building** में से कम से कम एक जोड़ें। कार्ड भरें और **Add Banking**, **Add Land** या **Add Building** दबाएँ।
3. ज़मीन और बिल्डिंग के लिए, अगर आपके पास हों, तो **EC (Encumbrance Certificate)**, **Property Document** और **Valuation Certificate** अपलोड करें।
4. **Movable** और **Infrastructure** में **Movable Assets Value**, **Own Godown Capacity** और **Rent Godown Capacity** भरें।
5. **Loan Liabilities List** में हर कर्ज़ का **Loan Source / Institution** और **Loan Value (₹) In Lakhs** भरें, और **Add Loan** दबाएँ।
6. **Net Financial Position** जाँच लें: **Fixed Assets** में से **Total Liabilities** घटाकर। फिर **Next** दबाएँ।

## आम दिक्कतें

- **"Please add details for at least one of Banking, Land, or Building before proceeding."** (आगे बढ़ने से पहले Banking, Land या Building में से कम से कम एक की जानकारी जोड़ें) पहले एक संपत्ति जोड़ें।
- **"OD Value is required when Banking details are provided."** (Banking की जानकारी देने पर OD Value ज़रूरी है) **OD Limit (₹) Lakhs** भरें।
- **"Market Value is required when Land details are provided."** (Land की जानकारी देने पर Market Value ज़रूरी है) बाज़ार भाव भरें। बिल्डिंग के लिए भी यही नियम है।
:::

::: section credit-limit
title: क्रेडिट लिमिट या ट्रेड डिपॉज़िट (स्टेप 10 और 11)
routes: /CreditLimit, /CreditLimitForGreenStar
for: 
roles: 
designations: 
screenshot: credit-limit.png
screenshot-phone: credit-limit.phone.jpg
minutes: 5
source-hash: d34b3a1b414aa494
confidence: ok

**यह स्क्रीन किस काम की है**
स्टेप 10 SPIC के लिए है और स्टेप 11 Greenstar के लिए है। स्टेप 11 तभी दिखता है जब स्टेप 1 में **GREEN STAR** चुना गया हो। पुराने डीलर के लिए ये स्क्रीन (**Credit Limit — SPIC** और **Credit Limit — GreenStar**) सुझाई गई क्रेडिट लिमिट निकालती हैं। नए डीलर के लिए इनमें इसकी जगह ट्रेड डिपॉज़िट भरा जाता है।

1. ऊपर डीलर की समरी, **Existing Credit Limit** (**Valid From** और **Valid To**) और बिक्री प्लान वाले कार्ड जाँच लें।
2. **Details of Securities** में हर सिक्योरिटी के लिए **Number**, **Other Details** और **Amount (₹) In Lakhs** भरें, जैसे **Fixed Deposit with SPIC** या **BG in favour of SPIC**।
3. **SPIC Valuation** या **Greenstar Valuation** टेबल में ज़्यादातर आँकड़े पिछले स्टेप से आते हैं। **Monthly Average Net Overdues during last year** आप ख़ुद भरें। यह ज़रूरी है, 0 या उससे ज़्यादा।
4. **Grand Total** स्कोर और **Recommended Credit Limit** बॉक्स जाँच लें: **Proposed Limit**, **Credit Multiplier** और **MO Recommended Credit Limit**।
5. अगर कोई **Additional Credit Limit (₹) In Lakhs** हो तो भरें, फिर **Next** दबाएँ।

## नए डीलर के लिए

- स्टेप 10 पर **Application Fee** (**Bank**, **DD Number**, **DD Date**, **Amount (₹)**, **Payable At**) और **Trade Deposit Details** भरें। दोनों अटैचमेंट PDF, JPG या PNG में अपलोड करें।
- स्टेप 11 पर **GFL Trade Deposit Details** भरें और अटैचमेंट अपलोड करें।

> Tip: मंज़ूर हो चुके डीलर के लिए, डीलर कार्ड पर **Credit Limit** बटन यह स्क्रीन अलग से खोलता है। तब नीचे के बटन पर **Submit** लिखा होता है।

## आम दिक्कतें

- **"Monthly Average Net Overdues during last year is required and must be 0 or greater."** (यह आँकड़ा ज़रूरी है और 0 या उससे ज़्यादा होना चाहिए) आँकड़ा भरें, चाहे वह 0 ही हो।
- **"Complete all SPIC Application Fee and Trade Deposit fields, and upload both attachments."** (SPIC Application Fee और Trade Deposit के सारे फ़ील्ड भरें, और दोनों अटैचमेंट अपलोड करें) हर बॉक्स भरें और दोनों फ़ाइलें अपलोड करें।
- **ये स्टेप नहीं खुलते।** **Investment** स्टेप पर सहमति (consent) दी गई थी।
:::

::: section enclosures
title: दस्तावेज़ अपलोड करें (स्टेप 12)
routes: /Enclosures
for: 
roles: 
designations: 
screenshot: enclosures.png
minutes: 5
source-hash: f0405468232d5c91
confidence: ok

**यह स्क्रीन किस काम की है**
**Enclosures** स्क्रीन पर सारे दस्तावेज़ एक जगह जमा होते हैं। यहाँ दिखता है कि कौन-से दस्तावेज़ **Uploaded** हैं और कौन-से **Missing** हैं।

1. ऊपर **Document Completion Progress** देखें। इसमें दिखता है कि कितने दस्तावेज़ अपलोड हो गए और कितने ज़रूरी दस्तावेज़ बाकी हैं।
2. **Documents already added in previous steps** में GST, PAN, Aadhaar और लाइसेंस की फ़ाइलें जाँचें। जो **Missing** है, उसे **Upload Document** से अपलोड करें।
3. **Partner / Proprietor Documents** और **Property Documents (Land & Building)** जाँचें। बाकी फ़ोटो, Aadhaar, PAN, EC, प्रॉपर्टी का दस्तावेज़ या वैल्यूएशन सर्टिफ़िकेट अपलोड करें।
4. **Additional Documents** में साल चुनें और **IT Returns for Last Year** और **IT Returns for Last 2nd Year** अपलोड करें, साथ ही हर वह दस्तावेज़ जिस पर **Required** लिखा है।
5. सिग्नेचर के नमूने के लिए **Download Template** दबाएँ, उसे प्रिंट करें, सभी साइन करने वालों के साइन लें, और **Spic Specimen Signature** या **Greenstar Specimen Signature** में अपलोड करें।
6. आख़िरी जाँच पर जाने के लिए **Next** दबाएँ।

> Careful: मिटाया गया दस्तावेज़ वापस नहीं आता। अगर ग़लती से मिट जाए, तो सही फ़ाइल फिर से अपलोड करें।

| दस्तावेज़ | क्या अपलोड करें |
|---|---|
| ज़्यादातर दस्तावेज़ | PDF, JPG या PNG, 10 MB तक। |
| **List of Retailers with Complete Details** | माँगे जाने पर Excel या CSV फ़ाइल। फ़ॉर्मेट के लिए **Download Sample Template** दबाएँ। |
| **Request Letter for Dealership** | सिर्फ़ नए डीलर के लिए। फ़र्म के लेटरहेड पर, मोहर और साइन के साथ। |

## आम दिक्कतें

- **"You still have ... required document(s) pending."** (अभी ... ज़रूरी दस्तावेज़ बाकी हैं) हर वह दस्तावेज़ अपलोड करें जिस पर **Required** और **Missing** लिखा है।
- **"No partners found. Add partners in Proprietor step first."** (कोई पार्टनर नहीं मिला। पहले Proprietor स्टेप में पार्टनर जोड़ें) स्टेप 7 पर वापस जाएँ और मालिक या पार्टनर सेव करें।
:::

::: section final-submission
title: आवेदन जाँचें और सबमिट करें (स्टेप 13)
routes: /FinalSubmission
for: 
roles: 
designations: 
screenshot: final-submission.png
hotspots: 1 @ 2.2,21.9,95.5,13 | 2 @ 2.2,37.8,95.5,31.3 | 3 @ 2.2,72,95.5,28 | 4 @ - | 5 @ -
screenshot-phone: final-submission.phone.jpg
hotspots-phone: 1 @ 3.1,13.3,93.8,7.5 | 2 @ 3.1,22.5,93.8,38.9 | 3 @ 3.1,63.1,93.8,36.9 | 4 @ - | 5 @ -
tour: 1 > [data-help=dr-fs-progress] | 2 > [data-help=dr-fs-sections] | 3 > [data-help=dr-fs-enclosures] | 4 > [data-help=dr-fs-terms] | 5 > [data-help=dr-fs-result]
minutes: 3
source-hash: ec4c07bdc1524795
confidence: ok

**यह स्क्रीन किस काम की है**
**Final Submission** स्क्रीन आख़िरी स्टेप है। यहाँ दिखता है कि क्या पूरा है और क्या बाकी है, और यहीं से आवेदन मंज़ूरी के लिए जाता है।

1. ऊपर प्रोग्रेस वाला कार्ड देखें। इसमें दिखता है कि कितने हिस्से पूरे हो गए। 100% होने पर उस पर **Application Ready for Submission** लिखा आता है।
2. **Section Completion Status** में हरा टिक मतलब वह हिस्सा पूरा है, और नारंगी निशान मतलब कुछ बाकी है। वह स्टेप खोलकर ठीक करने के लिए पेंसिल पर टैप करें।
3. **Enclosures** कार्ड में **Documents Uploaded** और **Missing Documents** जाँचें। बाकी दस्तावेज़ पर नारंगी क्रॉस होता है।
4. **Terms and Conditions** और **I hereby declare** वाले पॉइंट पढ़ें। सबमिट करने पर आप इन्हें मान लेते हैं।
5. जब हरा **All sections completed!** बॉक्स दिखे, तो **Submit Registration** दबाएँ। आपको **Registration submitted successfully!** दिखेगा और आप डीलर लिस्ट पर वापस पहुँच जाएँगे, जहाँ आवेदन पर **In RM** दिखता है।

> Tip: ऊपर स्टेप वाली पट्टी के किसी भी हिस्से पर टैप करके आप सीधे उस स्टेप पर जा सकते हैं।

## आम दिक्कतें

- **"Please complete all ... incomplete section(s) before submitting."** (सबमिट करने से पहले सारे ... अधूरे हिस्से पूरे करें) पहले नारंगी निशान वाला हर हिस्सा ठीक करें।
- **"Error submitting registration"** (रजिस्ट्रेशन सबमिट करने में दिक्कत आई) मैसेज पढ़ें। उसमें लिखा होता है कि सर्वर को क्या कमी मिली। उसे ठीक करें और फिर से सबमिट करें।
- **Submit Registration बटन नहीं दिख रहा।** आवेदन सिर्फ़ देखने के लिए खोला गया है। उसे डीलर कार्ड पर **Edit** से खोलें। **Edit** तभी दिखता है जब आवेदन **Draft** या **Rejected by RM** हो।
:::

::: section application-pdf
title: डीलरशिप का आवेदन फ़ॉर्म देखें
routes: /DealershipPDF/{dealerId:int?}
for: 
roles: 
designations: 
minutes: 2
source-hash: 916c338c0a45b3ba
confidence: ok

**यह स्क्रीन किस काम की है**
यह स्क्रीन पूरा आवेदन एक **DEALERSHIP APPLICATION FORM** के रूप में दिखाती है, SPIC या Greenstar के लिए। आवेदन पढ़ने या उसकी छपी हुई कॉपी रखने के लिए इसका इस्तेमाल करें।

1. डीलर लिस्ट में डीलर का कार्ड ढूँढें और **SPIC** या **GFL** पर टैप करें।
2. फ़ॉर्म खुलता है, जिसके ऊपर कंपनी का नाम और लोगो होता है।
3. हर हिस्सा पढ़ने के लिए नीचे स्क्रॉल करें: **Dealer Details**, **Business Information**, **Bank Details**, **Proprietor / Partner Details**, **Sales Planning & Forecast**, **Investment Details** और भी।
4. आख़िर में क्रेडिट लिमिट, **Recommended Credit Limit** और **Attached Documents** देखें।
5. कॉपी रखने के लिए अपने ब्राउज़र या फ़ोन का प्रिंट विकल्प इस्तेमाल करें।

## आम दिक्कतें

- **ऊपर "Credit Limit Consent: GIVEN" लिखा है।** डीलर ने क्रेडिट लिमिट की जानकारी नहीं दी, इसलिए क्रेडिट लिमिट 0 है।
- **GFL बटन नहीं दिख रहा।** डीलर का रजिस्ट्रेशन सिर्फ़ SPIC के लिए हुआ है।
:::

::: section entry-info
title: लकी ड्रॉ एंट्री की जानकारी
routes: /EntryInfo
for: 
roles: 
designations: 
screenshot: entry-info.png
screenshot-phone: entry-info.phone.jpg
minutes: 1
source-hash: 24f199e7d7d10b2f
confidence: ok

**यह स्क्रीन किस काम की है**
**Entry Information** स्क्रीन पर लकी ड्रॉ के लिए एक ख़रीद की एंट्री दिखती है। यह स्क्रीन अभी बन रही है: इसमें असली एंट्री नहीं, सिर्फ़ नमूने के आँकड़े दिखते हैं।

1. ऊपर एंट्री नंबर और उसका स्टेटस देखें।
2. एंट्री कार्ड में **Date**, **Dealer Name**, **Source**, **Location**, **Product** और **Purchase Amount** देखें।
3. **LUCKY DRAW INFO** में देखें कि एंट्री योग्य है या नहीं, और अभी का राउंड (cycle) कहाँ तक पहुँचा है।
4. **Verification** में **Invoice Number** देखें, और यह भी कि एंट्री गिनी गई है या नहीं।
5. वापस जाने के लिए ऊपर पीछे वाला तीर दबाएँ।

## आम दिक्कतें

- **आँकड़े कभी नहीं बदलते।** अभी यह स्क्रीन सिर्फ़ नमूने के आँकड़े दिखाती है।
:::
