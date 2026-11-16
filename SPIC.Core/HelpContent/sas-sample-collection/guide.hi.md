---
module: sas-sample-collection
title: मिट्टी के सैंपल का कलेक्शन
icon: bi-droplet
order: 50
language: hi
---

::: section overview
title: मिट्टी के सैंपल का कलेक्शन कैसे होता है
routes: /SampleCollection
for: 
roles: 
designations: 
source-hash: 690edb1b53c94744
confidence: ok

**यह स्क्रीन किस काम की है**
SPIC किसान के खेत की मिट्टी और पानी की जाँच ज़िले की लैब में करता है। ऐप हर सैंपल को खेत से लेकर किसान की रिपोर्ट तक ट्रैक करता है। हर कलेक्शन इन्हीं स्टेप्स से गुज़रता है।

## स्टेप्स

1. **Collect.** (सैंपल लेना) फ़ील्ड अधिकारी (MDO या JMDO) **New Sample Collection** खोलता है, किसान चुनता है और एक या ज़्यादा सैंपल जोड़ता है।
2. **Pay.** (भुगतान) **Paid Sample** के लिए भुगतान ज़रूरी है। अधिकारी UPI या कोई दूसरा भुगतान दर्ज करता है और रसीद की फ़ोटो अपलोड करता है। **Free Sample** में यह स्टेप नहीं होता।
3. **Approve.** (मंज़ूरी) मंज़ूरी देने वाला भुगतान और रसीद जाँचता है। फिर Finance जाँचता है कि पैसा मिल गया है।
4. **लैब को भेजना।** अधिकारी सैंपल कूरियर से भेजता है और ट्रैकिंग नंबर भरता है। इसे कंसाइनमेंट कहते हैं।
5. **Test.** (जाँच) लैब सैंपल लेती है और उनकी जाँच करती है।
6. **Report.** (रिपोर्ट) किसान ऐप में रिपोर्ट देखता है और उसे तमिल या अंग्रेज़ी में डाउनलोड कर सकता है।

## स्टेटस का मतलब

- **Draft**: सेव है, अभी सबमिट नहीं हुआ।
- **Pending Payment**: भुगतान दर्ज होने का इंतज़ार है।
- **Pending Approval**: भुगतान दर्ज हो गया, मंज़ूरी का इंतज़ार है।
- **Ready consignment**: लैब भेजने के लिए तैयार।
- **Dispatched**, **In Transit**, **Delivered to Lab**: लैब के रास्ते में।
- **In Progress**: लैब में जाँच चल रही है।
- **Completed**: नतीजे आ गए हैं।

## आम दिक्कतें

- मेन्यू में **Sample Collection** नहीं दिख रहा: आपके पद (designation) में यह पेज नहीं है। अपने एडमिनिस्ट्रेटर से पूछें।
- JMDO यूज़र्स को यह मेन्यू में नहीं दिखता। इसकी जगह इसे अपने **Soil Sample** टास्क से खोलें।
:::

::: section collection-list
title: अपने सैंपल कलेक्शन ढूँढें और ट्रैक करें
routes: /SampleCollection
for: 
roles: 
designations: 
source-hash: 53f4f23e337f18d1
confidence: ok

**यह स्क्रीन किस काम की है**
**Sample Collection** पेज पर वे सारे कलेक्शन दिखते हैं जो आप देख सकते हैं, उनके भुगतान और लैब स्टेटस के साथ। कोई कलेक्शन ढूँढने और उसकी जानकारी खोलने के लिए इसका इस्तेमाल करें।

## स्टेप्स

1. मेन्यू से **Sample Collection** खोलें।
2. ऊपर के कार्ड में गिनती दिखती है: **Total Samples**, **Free Samples**, **Paid Samples**, **Lab Level** और **Ready to Submit**।
3. सिर्फ़ उन्हीं कलेक्शन को देखने के लिए **Pending Payment** या **Draft** जैसी रंगीन टाइल पर टैप करें। सब देखने के लिए फिर से टैप करें।
4. खोजने के लिए **Search collections...** में किसान का नाम या कलेक्शन ID टाइप करें।
5. और विकल्पों के लिए **Filters** खोलें, **Date Range**, **Location**, **Payment Type**, **Payment Status** या **Status** चुनें, फिर **Apply Filters** पर टैप करें। इन्हें हटाने के लिए **Clear All** पर टैप करें।
6. कलेक्शन खोलने के लिए आँख वाले आइकन पर (या फ़ोन पर कार्ड पर) टैप करें।
7. स्क्रीन पर दिख रही लिस्ट डाउनलोड करने के लिए **Excel** या **PDF** पर टैप करें।
8. नया स्टेटस देखने के लिए **Refresh Data** पर टैप करें।

## आम दिक्कतें

- **No sample collections found**: फ़िल्टर हटाएँ, या तारीखें (date range) जाँचें।
- **Collections could not be loaded**: अपना इंटरनेट जाँचें और **Try again** पर टैप करें।
- **New Sample Collection** बटन नहीं है: आपका अकाउंट कलेक्शन नहीं बना सकता। सिर्फ़ MDO, JMDO और वे एडमिनिस्ट्रेटर अकाउंट सैंपल ले सकते हैं जिनका पद इसकी इजाज़त देता है।
- डाउनलोड में कुछ ही लाइनें हैं: **Excel** और **PDF** सिर्फ़ मौजूदा पेज सेव करते हैं। ज़्यादा लाइनें लेने के लिए **Items Per Page** बदलें।
:::

::: section collect-sample
title: मिट्टी या पानी का नया सैंपल लें
routes: /SampleCollection/new
for: SampleCollection.Entry
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 4807aa811490f90c
confidence: ok

**यह स्क्रीन किस काम की है**
किसानों से लिए गए सैंपल दर्ज करने के लिए **New Sample Collection** का इस्तेमाल करें। एक कलेक्शन में कई किसानों के सैंपल हो सकते हैं।

## स्टेप्स

1. **Sample Collection** पर **New Sample Collection** पर टैप करें। JMDO: **Tasks Allocation** में **Soil Sample** कार्ड पर **Start** पर टैप करें, या किसी टास्क के **Quick actions** में **Soil Sample Collection** पर टैप करें।
2. ऊपर **Free Sample** या **Paid Sample** चुनें।
3. **Farmer Name** में कम से कम दो अक्षर टाइप करें और किसान चुनें। अगर किसान नया है, तो **New Farmer** पर टैप करें।
4. पेड सैंपल के लिए **Paid Sample Category** में **Farmer** या **NGO** चुनें।
5. **Soil Sample** और **Water Sample** को **Yes** या **No** करें।
6. **Crop 1**, **Crop 2** चुनें और ज़रूरत हो तो **Remarks** लिखें।
7. **Add Another Sample** पर टैप करें। सैंपल अपनी रकम के साथ **Sample Added (Preview)** में दिखता है।
8. हर सैंपल के लिए यही दोहराएँ। किसी लाइन को बदलने के लिए पेंसिल, या हटाने के लिए डस्टबिन इस्तेमाल करें।
9. बाद में पूरा करने के लिए **Save Draft** पर टैप करें, या **Sample Summary** देखने के लिए **Save and Continue** पर टैप करें।
10. समरी जाँचें और **Continue to payment** (पेड) या **Continue to consignment** (फ़्री) पर टैप करें।

## फ़ील्ड का मतलब

- **Collection ID**: सेव करने पर ऐप खुद बनाता है।
- **Collected By**: आपका नाम, अपने-आप भरा होता है।
- **Total Amount**: सारे पेड सैंपल का चार्ज। फ़्री कलेक्शन में **Free** दिखता है।
- **New Farmer**: इसमें **Farmer Name** और 10 अंकों का **Mobile Number** ज़रूरी है। **Use my location** से **Latitude** और **Longitude** भर जाते हैं।

## आम दिक्कतें

- "Choose a farmer, or add a new one" (किसान चुनें, या नया जोड़ें): लिस्ट से नाम चुनें, सिर्फ़ टाइप करना काफ़ी नहीं है।
- "Choose at least one of Soil Sample or Water Sample" (Soil Sample या Water Sample में से कम से कम एक चुनें): इनमें से एक को **Yes** करें।
- किसान का पता ठीक करने के लिए **Edit farmer** पर टैप करें, पता बदलें, फिर **Save farmer details** पर टैप करें।
:::

::: section edit-draft
title: ड्राफ़्ट कलेक्शन पूरा करें या बदलें
routes: /SampleCollection/new, /SampleCollection/view/{id}
for: SampleCollection.Update
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 1e935425e9054d62
confidence: ok

**यह स्क्रीन किस काम की है**
**Save Draft** से सेव किया गया कलेक्शन **Draft** ही रहता है। जब तक आप सबमिट नहीं करते, कुछ भी भुगतान या लैब तक नहीं जाता। आप इसे फिर से खोलकर पूरा कर सकते हैं।

## स्टेप्स

1. **Sample Collection** खोलें और **Draft** टाइल पर टैप करें।
2. आँख वाले आइकन से कलेक्शन खोलें।
3. **Sample Collection Details** में ऊपर **Edit** पर टैप करें।
4. सैंपल जोड़ें, बदलें या हटाएँ, ठीक वैसे ही जैसे नए कलेक्शन में करते हैं।
5. फिर से ड्राफ़्ट में रखने के लिए **Save Draft** पर टैप करें, या **Save and Continue** पर टैप करें।
6. **Sample Summary** में सैंपल जाँचें और **Continue to payment** या **Continue to consignment** पर टैप करें।

> सबमिट करने के बाद कलेक्शन बदला नहीं जा सकता।

## आम दिक्कतें

- **Edit** बटन नहीं है: कलेक्शन पहले ही सबमिट हो चुका है, या आपका पद बदलाव की इजाज़त नहीं देता।
- "This collection has already been submitted" (यह कलेक्शन पहले ही सबमिट हो चुका है): ड्राफ़्ट पहले ही सबमिट हो गया था। स्टेटस देखने के लिए उसकी जानकारी खोलें।
- "That draft no longer exists" (वह ड्राफ़्ट अब मौजूद नहीं है): उसे हटा दिया गया है। नया कलेक्शन शुरू करें।
:::

::: section record-payment
title: पेड सैंपल का भुगतान दर्ज करें
routes: /SampleCollection/payment/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 67de9290d8256a0c
confidence: ok

**यह स्क्रीन किस काम की है**
**Payments** स्क्रीन पर दिखता है कि कितना पैसा लेना है, किसान के स्कैन करने के लिए UPI QR कोड, और भुगतान की जानकारी व रसीद भरने का फ़ॉर्म।

## स्टेप्स

1. **Continue to payment** के बाद **Payments** स्क्रीन खुलती है। आप कलेक्शन की जानकारी वाले पेज पर **Record payment** पर टैप करके भी इसे खोल सकते हैं।
2. **Total Amount to Collect** देखें। सैंपल देखने के लिए **View Sample Summary** पर टैप करें।
3. किसान से कहें कि किसी भी UPI ऐप से **Scan & Pay** के नीचे वाला QR कोड स्कैन करे। इसे सेव करने के लिए **Download QR Code** पर टैप करें।
4. **Payment Information** भरें।
5. **Payment Proof** में **Click to Upload or Drag and Drop** पर टैप करें और रसीद या स्क्रीनशॉट चुनें।
6. **Save Payment** पर टैप करें। अब भुगतान मंज़ूरी का इंतज़ार करता है।

## फ़ील्ड का मतलब

- **Transaction ID** (ज़रूरी): UPI ट्रांज़ैक्शन नंबर।
- **Paid By** (ज़रूरी): किसने भुगतान किया। कलेक्शन के किसानों के नाम सुझाए जाते हैं।
- **Payment Mode**: **UPI**, **Bank Transfer** या **Cash**।
- **Transaction Date**, **Bank / Gateway**, **Payment Gateway / UTR No.**, **Bank Name**, **Contact Number**, **Email (optional)**: जो जानकारी आपके पास है, वह भरें।
- **Remarks**: मंज़ूरी देने वाले के लिए नोट।
- **Payment Proof**: PNG या JPG फ़ोटो, 5 MB तक।

## आम दिक्कतें

- "Please fill in the Transaction ID and Paid By" (Transaction ID और Paid By भरें): दोनों ज़रूरी हैं।
- "Pick a PNG or JPG image of 5 MB or less" (5 MB या उससे छोटी PNG या JPG फ़ोटो चुनें): छोटी फ़ोटो या स्क्रीनशॉट लें।
- **Save Draft** टाइप की गई जानकारी सिर्फ़ इसी फ़ोन पर रखता है। काम **Save Payment** से पूरा करें।
- "Payment already recorded" (भुगतान पहले ही दर्ज है): यह मंज़ूरी का इंतज़ार कर रहा है।
- अगर भुगतान रिजेक्ट हो जाए, तो जानकारी वाले पेज पर कारण के साथ **Payment rejected** दिखता है। उसे ठीक करें और फिर से **Record payment** पर टैप करें।
:::

::: section send-consignment
title: सैंपल लैब को भेजें (कंसाइनमेंट)
routes: /SampleCollection/consignment/{id}, /SampleCollection/submitted/{id}
for: 
roles: MDO, JMDO, Admin, CorporateAdmin
designations: 
source-hash: 1820125bf239140d
confidence: ok

**यह स्क्रीन किस काम की है**
जब कलेक्शन फ़्री हो या उसका भुगतान मंज़ूर हो जाए, तो आप सैंपल पैक करके कूरियर को देते हैं। **Upload Consignment** कूरियर की जानकारी और फ़ोटो दर्ज करता है, ताकि लैब को पता रहे कि क्या आ रहा है।

## स्टेप्स

1. कलेक्शन खोलें और **Upload consignment** पर टैप करें। फ़्री सैंपल में **Continue to consignment** से यह सीधे खुल जाता है।
2. **Courier Details** में **Courier Service** चुनें और **Tracking Number (AWB)** टाइप करें।
3. उसी पार्सल में दूसरे तैयार कलेक्शन भेजने के लिए **Include other ready collections** खोलें और उन पर टिक करें। ऊपर दिए **Both**, **Free Sample** और **Paid Sample** से बदलता है कि कौन-से कलेक्शन लिस्ट में दिखें।
4. **Upload Photos and Documents** में **Shipping Label / Waybill** में फ़ोटो जोड़ें। फ़ोन पर आप **Take a photo** पर टैप कर सकते हैं।
5. चाहें तो **Package and contact details** खोलें।
6. **Submit Consignment** पर टैप करें।
7. **Consignment Submitted Successfully!** में ट्रैकिंग नंबर दिखता है। उसे कॉपी करने के लिए कॉपी आइकन पर टैप करें, जानकारी देखने के लिए **View Consignment** पर टैप करें, या **Go to Dashboard** पर टैप करें।

## फ़ील्ड का मतलब

- **Tracking Link (Optional)**, **Expected Delivery Date**: इनसे लैब को पार्सल पर नज़र रखने में मदद मिलती है।
- **Package Photo**, **Any other Document**: ज़रूरी नहीं, PNG या JPG फ़ोटो, हर एक 5 MB तक।
- **Packaging Type**, **No. of Packages**, **Package Weight (kg)**, **Contact Person**, **Mobile**, **Email**, **Collection Notes**: ज़रूरी नहीं।

## आम दिक्कतें

- "Please fill the courier details and add the shipping label photo" (कूरियर की जानकारी भरें और शिपिंग लेबल की फ़ोटो जोड़ें): तीनों ज़रूरी हैं।
- "This collection is not ready to send" (यह कलेक्शन भेजने के लिए तैयार नहीं है): भुगतान अभी दर्ज या मंज़ूर नहीं हुआ है।
- **Save Draft** सिर्फ़ टाइप किए गए फ़ील्ड रखता है। फ़ोटो बाद में फिर से जोड़ें।
- JMDO: कंसाइनमेंट सबमिट करने पर आज का **Soil Sample** टास्क पूरा (completed) मान लिया जाता है।
:::

::: section collection-details
title: सैंपल कलेक्शन की जानकारी पढ़ें
routes: /SampleCollection/view/{id}
for: 
roles: 
designations: 
source-hash: c0ff9ecbae88fe84
confidence: ok

**यह स्क्रीन किस काम की है**
**Sample Collection Details** में एक कलेक्शन की पूरी जानकारी दिखती है: भुगतान, कूरियर, लैब के नतीजे और इतिहास। फ़ील्ड स्टाफ़, डीलर और मैनेजर यहाँ प्रगति देख सकते हैं।

## स्टेप्स

1. **Sample Collection** खोलें और किसी कलेक्शन पर आँख वाले आइकन पर टैप करें।
2. ऊपर की लाइन में कलेक्शन ID, **Paid Sample** या **Free Sample**, और मौजूदा स्टेटस दिखता है।
3. पट्टी में **Consignment ID**, **Transaction ID**, **Courier Partner**, **Samples**, **No. of Tests** और **Payment Status** दिखते हैं। कंसाइनमेंट खोलने के लिए कंसाइनमेंट ID पर टैप करें।
4. **Collection Information** और **Sample Summary** में दिखता है कि सैंपल किसने लिए और हर सैंपल के कितने टेस्ट होने हैं।
5. नतीजे तैयार होने पर **Result Comparison** में लैब की वैल्यू दिखती हैं। अगर कई सैंपल हैं, तो किसी सैंपल के नतीजे देखने के लिए ऊपर उस सैंपल पर टैप करें।
6. **Consignments Details**, **Amount Details** और **History / Timeline** में कूरियर, चार्ज और अब तक का हर स्टेप दिखता है।
7. प्रिंट या डाउनलोड करने के लिए **Export** पर टैप करें।

## बटन जो आपको दिख सकते हैं

- **Edit**: कलेक्शन अभी ड्राफ़्ट है।
- **Record payment**: भुगतान अभी बाकी है।
- **Upload consignment**: लैब भेजने के लिए तैयार है।

## आम दिक्कतें

- "The lab has not published the results yet" (लैब ने अभी नतीजे जारी नहीं किए हैं): सैंपल अभी रास्ते में है या उसकी जाँच चल रही है।
- कंसाइनमेंट में **Not dispatched yet**: सैंपल अभी भेजे नहीं गए हैं।
- "Collection not found" (कलेक्शन नहीं मिला): उसे हटा दिया गया है, या आपके पास उसे देखने की अनुमति नहीं है।
:::

::: section admin-review
title: भुगतान मंज़ूर करें, कंसाइनमेंट आगे बढ़ाएँ और नतीजे भरें
routes: /SampleCollection/view/{id}, /ConsignmentHistory/view/{id}
for: 
roles: Admin, CorporateAdmin
designations: 
source-hash: 71850d4b14d9d073
confidence: ok

**यह स्क्रीन किस काम की है**
एडमिनिस्ट्रेटर कलेक्शन की जानकारी वाले पेज पर भुगतान जाँच सकते हैं, कंसाइनमेंट को अगले स्टेज पर ले जा सकते हैं, और लैब के नतीजे टाइप कर सकते हैं।

## स्टेप्स

### कलेक्शन पर भुगतान मंज़ूर करें

1. **Pending Approval** स्टेटस वाला कलेक्शन खोलें।
2. **Payment Approval** में **Amount**, **Transaction ID** और रसीद का मिलान करें। रसीद बड़ी करके देखने के लिए **View proof** पर टैप करें।
3. **Approve payment** पर टैप करें और पुष्टि करें। कलेक्शन लैब भेजने के लिए तैयार हो जाता है।
4. मना करने के लिए **Reject** पर टैप करें, **Reason for rejection** लिखें, फिर **Reject payment** पर टैप करें।

### कंसाइनमेंट आगे बढ़ाएँ

1. **Consignment History** खोलें और कोई कंसाइनमेंट खोलें।
2. **Update status** में अगले स्टेज पर टैप करें, जैसे **In Transit**, **Delivered** या **Completed**।

> कंसाइनमेंट आगे बढ़ाने से उसके अंदर का हर सैंपल भी आगे बढ़ जाता है।

### लैब के नतीजे भरें

1. कलेक्शन की जानकारी वाले पेज पर **Result Comparison** पर जाएँ, सैंपल चुनें और **Enter results** (या **Edit results**) पर टैप करें।
2. **Test Parameter**, **Entered Value**, **Unit**, **Normal Range** और **Result** भरें। और लाइनों के लिए **Add parameter** पर टैप करें।
3. **Save results** पर टैप करें।

## आम दिक्कतें

- "Add a reason so the collector knows what to correct" (कारण लिखें, ताकि सैंपल लेने वाले को पता चले कि क्या ठीक करना है): रिजेक्ट करने के लिए कारण ज़रूरी है।
- **Update status** नहीं है: कंसाइनमेंट पहले ही **Completed** है।
:::

::: section consignment-history
title: लैब को भेजे गए कंसाइनमेंट ट्रैक करें
routes: /ConsignmentHistory, /ConsignmentHistory/view/{id}
for: 
roles: 
designations: 
source-hash: c0eb73648ebf3b77
confidence: ok

**यह स्क्रीन किस काम की है**
**Consignment History** में लैब को भेजे गए सैंपल के हर पार्सल की लिस्ट है, कूरियर, ट्रैकिंग नंबर और मौजूदा स्टेटस के साथ।

## स्टेप्स

1. मेन्यू से **Consignment History** खोलें।
2. कार्ड में **Total Consignments**, **Total Consignment Sent**, **Total Samples**, **Delivered** और **Pending** दिखते हैं।
3. कोई कंसाइनमेंट ढूँढने के लिए **Search consignment, AWB, transaction...** में टाइप करें।
4. **Filters** खोलकर **From**, **To**, **Status**, **Courier Partner** या **Payment Type** चुनें, फिर **Apply Filters** पर टैप करें।
5. कूरियर का ट्रैकिंग पेज खोलने के लिए ट्रैकिंग नंबर पर टैप करें।
6. **Consignment Details** खोलने के लिए कंसाइनमेंट ID या आँख वाले आइकन पर टैप करें।
7. जानकारी वाले पेज पर टाइमलाइन, अंदर के सैंपल और रकम देखें। लेबल और पैकेज की फ़ोटो देखने के लिए **Click and View Photos and Files** पर टैप करें, और उन्हें सेव करने के लिए **Download All** पर टैप करें।
8. डाउनलोड करने के लिए **Excel** या **PDF** इस्तेमाल करें, और अपडेट करने के लिए **Refresh Data**।

## फ़ील्ड का मतलब

- **Current Status**: **Pending Pickup**, **Dispatched**, **In Transit**, **Delivered** या **Completed**।
- **AWB/Tracking No.**: कूरियर का ट्रैकिंग नंबर।
- **Total samples**: फ़ोन पर सैंपल की लिस्ट देखने के लिए सैंपल की गिनती पर टैप करें।

## आम दिक्कतें

- **No consignments found**: दूसरी तारीखें, स्टेटस या कूरियर चुनकर देखें, या **Clear filters** पर टैप करें।
- **No photos yet**: कंसाइनमेंट भेजते समय कुछ भी अटैच नहीं किया गया था।
- JMDO यूज़र्स को यह पेज मेन्यू में नहीं दिखता।
:::

::: section farmer-my-samples
title: अपने मिट्टी के सैंपल देखें (किसान)
routes: /Sas/MySamples, /SampleCollection/view/{id}
for: 
roles: Farmer
designations: 
source-hash: fa5d1d2f177d288c
confidence: ok

**यह स्क्रीन किस काम की है**
**My Samples** में मिट्टी या पानी का वह हर सैंपल दिखता है जो आपके MO अधिकारी ने आपसे लिया, और यह भी कि हर सैंपल अभी कहाँ है।

## स्टेप्स

1. मेन्यू से **My Samples** खोलें।
2. हर कार्ड पर सैंपल नंबर, कंसाइनमेंट, सैंपल लेने वाला अधिकारी, तारीख और फ़सल दिखती है।
3. रंगीन लेबल पर **Paid** या **Free**, सैंपल का स्टेटस, और **Report: Ready** या **Report: Not Ready** दिखता है।
4. कोई सैंपल ढूँढने के लिए **Search sample, consignment, crop** में टाइप करें, तारीखें चुनें, या कोई टाइप, स्टेटस या फ़सल चुनें।
5. सैंपल खोलने के लिए **View Details** पर टैप करें।
6. जानकारी वाले पेज पर **Sample Details**, **Farmer Details**, **Payment Details** और **Sample Journey** के स्टेप देखें, **Sample Collected by MO** से लेकर **Report Ready for Download** तक।
7. रिपोर्ट तैयार होने पर **Download PDF** पर टैप करें। तमिल या अंग्रेज़ी चुनने के लिए उसके बगल वाले छोटे तीर पर टैप करें।

## स्टेटस का मतलब

- **Payment Pending**: भुगतान अभी पूरा नहीं हुआ है।
- **Awaiting Dispatch**: लैब भेजे जाने का इंतज़ार है।
- **In Transit**: लैब के रास्ते में है।
- **Under Analysis**: लैब में इसकी जाँच चल रही है।
- **Report Ready**: आप रिपोर्ट डाउनलोड कर सकते हैं।

## आम दिक्कतें

- **No samples yet**: आपके अधिकारी ने अभी आपके नाम से कोई सैंपल नहीं जोड़ा है। अपने MO अधिकारी से पूछें।
- **Download PDF** नहीं दिख रहा: लैब ने अभी रिपोर्ट पूरी नहीं की है।
:::

::: section farmer-my-reports
title: अपनी मिट्टी जाँच रिपोर्ट डाउनलोड करें (किसान)
routes: /Sas/MyReports
for: 
roles: Farmer
designations: 
source-hash: 810f0b093cade052
confidence: ok

**यह स्क्रीन किस काम की है**
**My Reports** में वे जाँच रिपोर्ट दिखती हैं जो लैब ने आपके सैंपल के लिए बनाईं। आप हर रिपोर्ट तमिल या अंग्रेज़ी में डाउनलोड कर सकते हैं।

## स्टेप्स

1. मेन्यू से **My Reports** खोलें।
2. कार्ड में **Total Reports**, **Ready to Download**, **Downloaded** और **Under Process** दिखते हैं।
3. **Reports List** में अपनी रिपोर्ट ढूँढें। लिस्ट छोटी करने के लिए **Search report, sample, crop**, तारीखें, **All Statuses** या **All Crops** इस्तेमाल करें।
4. रिपोर्ट सेव करने के लिए **Download PDF** पर टैप करें। तमिल या अंग्रेज़ी चुनने के लिए उसके बगल वाले छोटे तीर पर टैप करें।
5. **Report Details** पढ़ने के लिए आँख वाले आइकन (या फ़ोन पर **View**) पर टैप करें: जाँच के नतीजे, सुझाव और फ़सल के लिए सलाह।

## फ़ील्ड का मतलब

- **Reports No.**: रिपोर्ट का नंबर।
- **Sample No.**: वह सैंपल जिसकी यह रिपोर्ट है।
- **Status**: **Report Ready** (अभी डाउनलोड नहीं हुई), **Downloaded** या **Printed**।
- **Under Process**: वे सैंपल जिनकी लैब में अभी जाँच चल रही है।

## आम दिक्कतें

- **No reports yet**: लैब ने अभी काम पूरा नहीं किया है। मैसेज में बताया जाता है कि कितने सैंपल की जाँच चल रही है।
- **Reports are not available right now**: अपना इंटरनेट जाँचें और **Try again** पर टैप करें।
- फ़ाइल नहीं खुली: अपने फ़ोन का डाउनलोड (downloads) फ़ोल्डर देखें।
:::

::: section farmer-payments
title: अपने सैंपल के भुगतान देखें (किसान)
routes: /Sas/Payments, /Sas/Payments/{id}
for: 
roles: Farmer
designations: 
source-hash: 41753a10de9d41d6
confidence: ok

**यह स्क्रीन किस काम की है**
**Payments** में आपके पेड मिट्टी सैंपल के लिए किए गए भुगतान दिखते हैं, और यह भी कि SPIC ने उन्हें मंज़ूर किया है और पैसा मिला है या नहीं। यहाँ आप सिर्फ़ देखते हैं; कुछ भरना नहीं है।

## स्टेप्स

1. मेन्यू से **Payments** खोलें।
2. कार्ड में **Total Paid Samples**, **Finance Verified**, **Approval Pending** और **Payment Issues** दिखते हैं।
3. **Search sample no., transaction ID...** से खोजें, या तारीखें, कोई स्टेटस या फ़सल चुनें।
4. **Payment Details** खोलने के लिए सैंपल नंबर, आँख वाले आइकन या **View Details** पर टैप करें।
5. रकम, **Payment Mode**, **Transaction ID**, **Payment Proof** में रसीद, और **Verification Journey** देखें।

## स्टेटस का मतलब

- **Admin Approval Pending**: आपका भुगतान मंज़ूरी का इंतज़ार कर रहा है।
- **Admin Approved**: मंज़ूर हो गया, Finance का इंतज़ार है।
- **Finance Verified**: भुगतान पूरी तरह पक्का हो गया है।
- **Payment Issue**: भुगतान लौटा दिया गया या मेल नहीं खाया। आपका MO अधिकारी इसे ठीक करेगा।

## आम दिक्कतें

- **No payments found**: आपके कोई पेड सैंपल नहीं हैं, या आपके सैंपल फ़्री थे।
- **Payment Issue** काफ़ी समय से बना हुआ है: **Transaction ID** के साथ अपने MO अधिकारी से संपर्क करें।
:::

::: section approve-payments
title: सैंपल के भुगतान मंज़ूर करें और Finance को भेजें
routes: /Sas/Payments, /Sas/Payments/{id}
for: SasPaymentApproval.View
roles: 
designations: 
source-hash: 4a8d8dd3f9015aa4
confidence: ok

**यह स्क्रीन किस काम की है**
**Payment Approval** में वे भुगतान दिखते हैं जो फ़ील्ड अधिकारियों ने पेड सैंपल के लिए दर्ज किए। आप हर रसीद और रकम जाँचते हैं, फिर उसे मंज़ूर करके Finance को भेजते हैं, या अधिकारी को वापस भेज देते हैं।

## स्टेप्स

1. मेन्यू से **Payment Approval** खोलें।
2. **Pending Admin Approval** टैब पर टैप करें।
3. **Payment Approval Details** खोलने के लिए **Payment Ref No.** या आँख वाले आइकन पर टैप करें।
4. **Expected Amount**, **Paid Amount** और **Difference** का मिलान करें, और **Payment Proof Preview** में रसीद जाँचें।
5. **Approve & Forward to Finance** पर टैप करें।
6. **Verified Amount** (या **Other amount**) चुनें, **Remarks** लिखें, **I confirm that the payment proof, transaction details and amount are verified.** पर टिक करें (मैं पुष्टि करता/करती हूँ कि भुगतान का सबूत, ट्रांज़ैक्शन की जानकारी और रकम जाँच ली गई है), फिर **Approve & Forward** पर टैप करें।
7. मना करने के लिए **Reject Payment** पर टैप करें, **Reason for rejection** लिखें और फिर से **Reject Payment** पर टैप करें।

## फ़ील्ड का मतलब

- **Admin Status**: **Pending Review**, **Admin Approved** या **Returned to MO**।
- **Finance Status**: **Not Forwarded**, **Pending**, **Verified** या **Mismatch**।
- **Difference**: जब भुगतान की गई रकम उतनी नहीं होती जितनी होनी चाहिए, तो यह लाल दिखता है।

## आम दिक्कतें

- "Tick the confirmation to continue" (आगे बढ़ने के लिए पुष्टि वाले बॉक्स पर टिक करें): मंज़ूर करने से पहले बॉक्स पर टिक करें।
- "Enter a verified amount greater than zero" (शून्य से ज़्यादा जाँची हुई रकम भरें): रकम चुनें या टाइप करें।
- कोई एक्शन बटन नहीं है: भुगतान पर फ़ैसला पहले ही हो चुका है।
- पुराने भुगतान ढूँढने के लिए **Filters** (तारीखें, **State**, **Region**, **Payment Mode**, **Status**) इस्तेमाल करें।
:::

::: section verify-payments
title: मिले हुए भुगतान की जाँच करें (Finance)
routes: /Sas/Payments, /Sas/Payments/{id}, /Sas/Payments/History
for: SasPaymentVerification.View
roles: 
designations: 
source-hash: ec36e4e58f4e4b58
confidence: ok

**यह स्क्रीन किस काम की है**
**Payment Verification** में वे भुगतान दिखते हैं जो मंज़ूरी देने वाले ने Finance को भेजे हैं। आप पक्का करते हैं कि पैसा खाते में आ गया है। **Payment History** में वह हर भुगतान रहता है जिसका काम आप पूरा कर चुके हैं।

## स्टेप्स

1. मेन्यू से **Payment Verification** खोलें।
2. **Pending Verification** टैब पर टैप करें।
3. किसी भुगतान को उसके **Payment Ref No.** या आँख वाले आइकन से खोलें। **Payment Proof Details** खुलता है।
4. **Payment Proof Preview** में रसीद को अपने बैंक स्टेटमेंट से मिलाएँ।
5. अगर पैसा आ गया है, तो **Verify Payment** पर टैप करें। **Verified Amount** चुनें, **Received Date** डालें, **Remarks** लिखें, **I have verified the payment proof, transaction ID, and amount.** पर टिक करें (मैंने भुगतान का सबूत, ट्रांज़ैक्शन ID और रकम जाँच ली है) और **Confirm** पर टैप करें।
6. अगर आपको भुगतान नहीं मिल रहा, तो **Mark Mismatch** पर टैप करें, **Mismatch remarks** लिखें और फिर से **Mark Mismatch** पर टैप करें।
7. पूरे हो चुके भुगतान देखने के लिए **Payment History** खोलें। **All**, **Verified**, **Failed** और **Mismatch** टैब इस्तेमाल करें।

## फ़ील्ड का मतलब

- **Verified Amount**: असल में मिली रकम। जितनी आनी थी उससे कम रकम mismatch के रूप में सेव होती है।
- **Payment Verified**: पैसा मिलना पक्का हो गया।
- **Amount Mismatch**: जितना पैसा आना था, उससे कम आया।
- **Failed**: **Mark Mismatch** से मार्क किया गया; कोई पैसा नहीं मिला।

## आम दिक्कतें

- "Enter the amount received" (मिली हुई रकम भरें): रकम चुनें या टाइप करें।
- "Tick the confirmation to continue" (आगे बढ़ने के लिए पुष्टि वाले बॉक्स पर टिक करें): पुष्टि करने से पहले बॉक्स पर टिक करें।
- कोई एक्शन बटन नहीं है: भुगतान पहले ही जाँचा या मार्क किया जा चुका है।
:::
