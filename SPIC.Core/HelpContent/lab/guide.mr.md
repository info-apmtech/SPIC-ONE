---
module: lab
title: SAS माती परीक्षण लॅब
icon: bi-eyedropper
order: 60
language: mr
---

::: section overview
title: माती परीक्षण लॅब कशी चालते
routes: /Lab
for: 
roles: 
designations: 
source-hash: d196c24e61d998d8
confidence: ok

**हा मॉड्युल कशासाठी आहे**
लॅब पोर्टल मातीच्या आणि पाण्याच्या प्रत्येक नमुन्याचा मागोवा ठेवते: नमुना लॅबला पोहोचल्याच्या दिवसापासून शेतकऱ्याला रिपोर्ट मिळेपर्यंत. प्रत्येक व्यक्तीला फक्त तिच्या कामाच्या स्क्रीन दिसतात.

## एका consignment चा प्रवास

1. फील्ड कर्मचारी शेतकऱ्यांकडून नमुने गोळा करतात आणि ते **consignment** (पार्सल) म्हणून कुरिअरने लॅबला पाठवतात. रस्त्यात असताना त्यावर **In Transit** दिसते.
2. पार्सल पोहोचल्यावर लॅब कोऑर्डिनेटर **Mark received** दाबतात. consignment **Batch Pending** होते.
3. कोऑर्डिनेटर एक किंवा जास्त Batch Pending consignment एका **batch** (बॅच) मध्ये टाकतात आणि ती एका लॅब ॲनालिस्टला देतात. बॅच **Taken for Analysis** होते.
4. ॲनालिस्ट बॅच उघडतात, प्रत्येक नमुन्याचे तपासणीचे आकडे टाकतात आणि **Submit Values** दाबतात. सिस्टम निकाल, स्थिती आणि शिफारस आपोआप काढते.
5. सगळे नमुने सबमिट झाल्यावर कोऑर्डिनेटर **Complete Batch** दाबतात. यामुळे आकडे लॉक होतात आणि प्रत्येक नमुन्यासाठी एक रिपोर्ट तयार होतो.
6. रिपोर्ट PDF किंवा Excel मध्ये, इंग्रजी आणि भारतीय भाषांमध्ये डाउनलोड करून शेतकऱ्याला देता येतात. शेतकरीही ॲपमध्ये रिपोर्ट डाउनलोड करू शकतो.

## कोण काय करते

- **Lab coordinator**: consignment घेतात, बॅच बनवतात, ॲनालिस्ट नेमतात, बॅच पूर्ण करतात, लॅब रिपोर्ट डाउनलोड करतात.
- **Lab analyst**: त्यांना दिलेल्या बॅचसाठी तपासणीचे आकडे टाकतात आणि नमुन्यांचे रिपोर्ट डाउनलोड करतात.
- **Lab admin**: **Lab Tracking** वर सगळ्या बॅच पाहतात. या स्क्रीनवर फक्त पाहता येते.

> तुमच्या पदामध्ये (designation) लॅब पेजेस असतील तरच लॅबच्या स्क्रीन उघडतात. Admin ला सुद्धा हे लागते. लॅबचा मेनू दिसत नसेल तर तुमच्या ॲडमिनिस्ट्रेटरला विचारा.
:::

::: section coordinator-dashboard
title: लॅब कोऑर्डिनेटर डॅशबोर्ड
routes: /Lab
for: 
roles: 
designations: 
source-hash: 17d85f22de7e96da
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Lab Coordinator Dashboard** वर लॅबचे थोडक्यात चित्र दिसते: किती consignment आल्या, किती अजून बॅचची वाट पाहत आहेत आणि तपासणी किती पुढे गेली आहे. कोऑर्डिनेटरनी **Lab** उघडले की ॲप हा डॅशबोर्ड दाखवते. ॲनालिस्टना त्याऐवजी त्यांच्या स्वतःच्या डॅशबोर्डवर नेले जाते.

## पायऱ्या

1. वरची कार्डे पाहा: **Total Consignments Received**, **Pending Batch Creation**, **Total Batches Created**, **Taken for Analysis**, **Analysis Completed**, **Completed Batches** आणि **Reports Generated**.
2. **Recent Consignments** टेबलमध्ये शेवटच्या पाच consignment त्यांच्या **Status** सह (**Batch Pending** किंवा **Batch Created**) दिसतात.
3. consignment उघडण्यासाठी तिचा नंबर किंवा डोळ्याचा आयकॉन टॅप करा.
4. पूर्ण **Consignment Details** लिस्ट उघडण्यासाठी **Recent Consignments** च्या बाजूचे **View All** दाबा.
5. **Recently Created Batches** टेबलमध्ये शेवटच्या पाच बॅच, **Assigned To** मधला ॲनालिस्ट, **Current Status** आणि **Analysis Days** दिसतात.
6. **Analysis Tracking** उघडण्यासाठी **Recently Created Batches** च्या बाजूचे **View All** दाबा.

## नेहमीच्या अडचणी

- **मला "No batches yet" दिसते.** अजून एकही बॅच बनलेली नाही. Batch Pending consignment पाहण्यासाठी **Go to consignments** दाबा.
- **मला "The dashboard could not be loaded" दिसते.** तुमचे इंटरनेट कनेक्शन तपासा आणि **Try again** दाबा.
- **मला मुख्य Dashboard वर पाठवले गेले.** तुमच्या पदामध्ये कोऑर्डिनेटर किंवा ॲनालिस्टची लॅब पेजेस नाहीत. तुमच्या ॲडमिनिस्ट्रेटरला विचारा.
:::

::: section receive-consignments
title: लॅबमध्ये consignment घ्या
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
source-hash: fc856d94c96b8437
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Consignment Details** स्क्रीनवर लॅबला पाठवलेल्या सगळ्या consignment दिसतात. पार्सल मिळाले अशी नोंद करण्यासाठी आणि त्यात काय आहे ते तपासण्यासाठी ही स्क्रीन वापरा.

## पायऱ्या

1. वरची कार्डे पाहा: **Total Consignments**, **Pending Batch Creation**, **Batched Consignments**, **Water Samples** आणि **Soil Samples**.
2. consignment शोधण्यासाठी सर्च बॉक्समध्ये consignment नंबर किंवा AWB नंबर टाइप करा, किंवा **All Statuses** मध्ये एक स्थिती निवडा.
3. पार्सल आले की **In Transit** स्थिती असलेली त्याची ओळ शोधा.
4. **Action** कॉलममधला receive आयकॉन टॅप करा (फोनवर **Mark received** दाबा).
5. प्रश्नाच्या बॉक्समध्ये परत **Mark received** दाबा. स्थिती **Batch Pending** होते आणि त्यातले नमुने बॅचसाठी तयार होतात.
6. पूर्ण तपशील पाहण्यासाठी consignment नंबर किंवा डोळ्याचा आयकॉन टॅप करा.
7. तपशील स्क्रीनवर नमुन्यांची संख्या, State, Region आणि **Head Quarters**, **History / Timeline**, **Collections**, **Samples** आणि **Courier & Package** तपशील दिसतात.
8. पाठवताना काढलेले फोटो पाहण्यासाठी **Consignment Photos** दाबा. ते नमुना संकलन उघडण्यासाठी **Collection No.** टॅप करा.
9. consignment आधीच बॅचमध्ये असेल तर बॅच उघडण्यासाठी **View Batch Details** दाबा.

## नेहमीच्या अडचणी

- **Mark received बटण दिसत नाही.** फक्त कोऑर्डिनेटरच consignment घेऊ शकतात, आणि ते सुद्धा फक्त **In Transit** असलेल्या.
- **मला "No consignments found" दिसते.** तुमचा सर्च किंवा स्थितीचा फिल्टर खूप मर्यादित आहे. **Clear filters** दाबा.
- **Consignment Photos राखाडी आहे.** पार्सल पाठवताना कोणतेही फोटो जोडले नव्हते.
:::

::: section create-batch
title: consignment मधून बॅच बनवा
routes: /Lab/Consignments, /Lab/Consignments/{id}
for: 
roles: 
designations: 
source-hash: 90ca666972940c84
confidence: ok

**ही स्क्रीन कशासाठी आहे**
बॅच म्हणजे एक ॲनालिस्ट एकत्र तपासतो अशा नमुन्यांचा गट. एका बॅचमध्ये अनेक consignment असू शकतात. तुम्ही बॅच **Consignment Details** लिस्टमधून बनवता.

## पायऱ्या

1. **Consignment Details** उघडा. बॅचची वाट पाहणाऱ्या consignment च पाहण्यासाठी **All Statuses** मध्ये **Batch Pending** निवडा.
2. बॅचमध्ये हव्या असलेल्या प्रत्येक consignment च्या सुरुवातीच्या बॉक्सवर टिक करा. सर्वात वरचा बॉक्स पेजवरच्या सगळ्या Batch Pending ओळींवर टिक करतो.
3. **Create Batch (n)** दाबा. तुम्ही किती टिक केल्या तो आकडा त्यात दिसतो. एका consignment च्या स्वतःच्या स्क्रीनवर त्याऐवजी **Create Batch** दाबा.
4. **Create Batch** फॉर्म भरा (खाली पाहा) आणि **Confirm** दाबा.
5. **Batch Created Successfully** बॉक्समध्ये नवीन बॅच नंबर दिसतो. बॅच उघडण्यासाठी **View Batch Details** दाबा, किंवा सगळ्या बॅच पाहण्यासाठी **Go To Analysis Tracking** दाबा.

## फील्डची माहिती

- **Batch No**: तुम्ही Confirm दाबल्यावर सिस्टम बनवते.
- **Batch Created Date**: आपोआप आजची तारीख. पुढची (भविष्यातली) तारीख चालत नाही.
- **Consignment No.**: टिक केलेल्या consignment छोट्या चिप म्हणून दिसतात. चिप काढण्यासाठी तिच्यावरची छोटी फुली दाबा, किंवा दुसरी Batch Pending consignment जोडण्यासाठी बॉक्समध्ये टाइप करा.
- **Assign To**: नमुने तपासणारा ॲनालिस्ट. आत्ताच निवडला तर बॅच थेट **Taken for Analysis** होते. हे रिकामे ठेवून नंतरही नेमता येते.
- **Priority**: **Low**, **Medium** (आपोआप निवडलेले) किंवा **High**. ॲनालिस्टला ही प्राधान्यता दिसते.
- **Remarks**: बॅचसाठी कोणतीही नोंद.

## नेहमीच्या अडचणी

- **"Pick at least one consignment."** (किमान एक consignment निवडा) Confirm दाबण्याआधी किमान एक consignment जोडा.
- **टिक करायचे बॉक्स दिसत नाहीत.** फक्त **Batch Pending** ओळींवरच टिक करता येते, आणि फक्त कोऑर्डिनेटरनाच हे बॉक्स दिसतात.
:::

::: section analysis-tracking
title: तपासणी चालू असलेल्या बॅचचा मागोवा घ्या
routes: /Lab/Analysis
for: 
roles: 
designations: 
source-hash: eb674502e680b760
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Analysis Tracking** स्क्रीनवर सगळ्या बॅच दिसतात. त्यामुळे बॅच बनल्यापासून तपासणी पूर्ण होईपर्यंत तिचा मागोवा घेता येतो आणि उशीर झालेल्या बॅच ओळखता येतात.

## पायऱ्या

1. वरची कार्डे पाहा: **Total Batches**, **Created**, **Taken for Analysis**, **Analysis Completed**, **Completed** आणि **Delayed Analysis**.
2. सर्च बॉक्समध्ये बॅच नंबर, consignment नंबर किंवा ॲनालिस्टचे नाव टाइप करा.
3. **All Statuses** मध्ये स्थिती निवडा. उशीर झालेल्या बॅचच पाहण्यासाठी **Delayed** निवडा.
4. **Analysis Days** कॉलम वाचा:
- **Pending** (पिवळा): तपासणी सुरू झालेली नाही.
- दिवसांचा हिरवा आकडा: तपासणी वेळेवर चालू आहे.
- दिवसांचा लाल आकडा: बॅचला उशीर झाला आहे (साधारणपणे 5 दिवसांपेक्षा जास्त झाले आणि पूर्ण झाली नाही).
5. **Batch Details** उघडण्यासाठी बॅच नंबर किंवा डोळ्याचा आयकॉन टॅप करा.
6. जास्त बॅच पाहण्यासाठी खालचे पेज नंबर वापरा, किंवा प्रत्येक पेजवर किती ओळी दिसाव्यात ते बदला.

## नेहमीच्या अडचणी

- **मला "No batches found" दिसते.** स्थिती किंवा सर्च बदला, किंवा **Clear filters** दाबा. एकही बॅच नसेल तर **Go to consignments** दाबा आणि एक बॅच बनवा.
- **ॲनालिस्टने काम संपवले तरी बॅचवर Delayed दिसते.** सगळे नमुने सबमिट होऊन तपासणी पूर्ण होईपर्यंत ती Delayed च राहते.
:::

::: section batch-details
title: बॅच नेमा, तिचा मागोवा घ्या आणि पूर्ण करा
routes: /Lab/Analysis/{id}
for: 
roles: 
designations: 
source-hash: ad50ad4baaed5137
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Batch Details** स्क्रीनवर एका बॅचची सगळी माहिती दिसते: तिचे नमुने, तपासणीचे घटक (parameters), कागदपत्रे आणि इतिहास. कोऑर्डिनेटर इथेच ॲनालिस्ट नेमतात आणि बॅच पूर्ण करतात.

## पायऱ्या

1. वरच्या पट्टीवर **Batch No.**, **Consignment No.**, **Total Samples**, **Assigned Date**, **Current Status** आणि **Analysis Days** दिसतात.
2. टॅब वापरा: **Overview** (प्रगती, नमुन्यांचा सारांश, टाइमलाइन), **Samples**, **Parameters**, **Documents** आणि **Activity Log**.
3. बॅच एखाद्या ॲनालिस्टला देण्यासाठी **Assign** दाबा (ॲनालिस्ट बदलण्यासाठी **Reassign**). **Assign To** आणि **Priority** निवडा, मग **Save** दाबा.
4. बॅचला ॲनालिस्ट नेमला आहे पण काम सुरू झाले नसेल तर **Take for Analysis** दाबा आणि खात्री करा.
5. **Samples** टॅबवर नमुन्याचे तपासणीचे आकडे पाहण्यासाठी तो नमुना टॅप करा. लिस्ट डाउनलोड करण्यासाठी **Export** दाबा.
6. **Documents** टॅबवर **Upload** दाबा, **Files**, **Document Type** आणि **Description** निवडा, मग परत **Upload** दाबा. कागदपत्र डाउनलोड करण्यासाठी, पाहण्यासाठी किंवा डिलीट करण्यासाठी आयकॉन वापरा.
7. सगळे नमुने सबमिट झाले की "Every sample is completed" (सगळे नमुने पूर्ण झाले) अशी नोंद दिसते. **Complete Batch** दाबा.
8. **Final Remarks** टाइप करा आणि **Complete Batch** दाबा. आकडे लॉक होतात आणि रिपोर्ट तयार होतात.

## नेहमीच्या अडचणी

- **Complete Batch बटण दिसत नाही.** काही नमुने अजून सबमिट झालेले नाहीत. **Samples** टॅबवर **Not Started** किंवा **In Progress** तपासा.
- **बटणे दिसत नाहीत.** बॅच आधीच **Completed** झाली आहे, किंवा तुम्हाला कोऑर्डिनेटरचे अधिकार नाहीत.
- **फाइल जोडली गेली नाही.** फाइल PDF, JPG, PNG, WEBP, XLSX किंवा DOCX असावी, प्रत्येकी 20 MB पर्यंत, आणि एका वेळी 10.
:::

::: section lab-reports
title: लॅब रिपोर्ट डाउनलोड करा
routes: /Lab/Reports
for: 
roles: 
designations: 
source-hash: 4669cdab3bbe7c4e
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Lab Reports** स्क्रीनवर बॅच पूर्ण झाल्यावर तयार झालेले रिपोर्ट दिसतात. इथून तुम्ही रिपोर्टचा तपशील उघडू शकता आणि शेतकरी वाचतो त्या भाषेत तो PDF किंवा Excel म्हणून डाउनलोड करू शकता.

## पायऱ्या

1. वरची कार्डे पाहा: **Total Reports**, **Batch Reports**, **Sample Reports**, **Generated Today** आणि **Downloads Completed**.
2. लिस्ट छोटी करण्यासाठी **Filters** दाबा. **From** आणि **To** तारखा, **State**, **Sample Type** किंवा **Financial Year** निवडा, मग **Apply Filters** दाबा. ते काढण्यासाठी **Clear All** दाबा.
3. सर्च बॉक्समध्ये बॅच, consignment किंवा रिपोर्ट नंबर टाइप करा.
4. **Lab Report Details** उघडण्यासाठी डोळ्याचा आयकॉन (फोनवर **View Report**) टॅप करा.
5. तपशीलात **Report Summary**, **Batch Details**, **Consignment Details**, **Analysis Details** आणि **Status Timeline** दिसतात.
6. बॅचचा रिपोर्ट इंग्रजीमध्ये घेण्यासाठी **Download PDF** दाबा. दुसरी भाषा हवी असेल तर त्याच्या बाजूचा छोटा बाण दाबा आणि लिस्टमधून **Tamil**, **Telugu**, **Marathi** किंवा दुसरी भाषा निवडा.
7. **Download Excel** पण असेच दाबा. प्रत्येक नमुन्याच्या रिपोर्टसाठी एक Excel फाइल मिळते.
8. लिस्टवर परत जाण्यासाठी **Close** दाबा.

## नेहमीच्या अडचणी

- **मला "No reports found" दिसते.** बॅच पूर्ण झाल्यावरच रिपोर्ट दिसतात. तुमचे फिल्टरही तपासा.
- **डाउनलोडची बटणे राखाडी आहेत.** रिपोर्ट अजून तयार नाही.
- **PDF इंग्रजीमध्ये आली.** या रिपोर्टसाठी ती भाषा अजून तयार नसेल, म्हणून इंग्रजी प्रत दिली जाते.
:::

::: section analyst-dashboard
title: लॅब ॲनालिस्ट डॅशबोर्ड
routes: /Lab/Analyst
for: 
roles: 
designations: 
source-hash: 976c980ff899d680
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Lab Analyst Dashboard** वर तुम्हाला दिलेले काम आणि आधी काय करायचे ते दिसते. ॲनालिस्टनी **Lab** उघडले की ॲप ही स्क्रीन दाखवते.

## पायऱ्या

1. वरची कार्डे पाहा: **Assigned Batches**, **Pending Value Entry** (आकड्यांची वाट पाहणारे नमुने), **Auto Result Ready** (ज्यांचा निकाल काढला गेला आहे असे नमुने), **Reports Generated** (या आर्थिक वर्षातले) आणि **Previous FY Reports**.
2. **Today's Work Priority** तपासा:
- **Pending value entry batches** (High): आकड्यांची वाट पाहणाऱ्या बॅच पाहण्यासाठी **Entry Now** दाबा.
- **Auto result ready for review** (Medium): निकाल तपासण्यासाठी **Preview** दाबा.
- **Generated reports for download** (Low): **View Reports** दाबा.
3. किंवा **Quick Actions** वापरा: **Go to Batch List**, **Continue Test Entry** (तुम्ही शेवटी काम केलेली बॅच उघडते), **View Test Reports** आणि **View Previous FY Reports**.
4. नवीन आकडे पाहण्यासाठी **Refresh** दाबा.

## नेहमीच्या अडचणी

- **सगळे आकडे शून्य आहेत.** अजून तुम्हाला एकही बॅच दिलेली नाही. तुमच्या लॅब कोऑर्डिनेटरला विचारा.
- **मला "Your dashboard could not be loaded" दिसते.** तुमचे कनेक्शन तपासा आणि **Try again** दाबा.
:::

::: section analyst-batch-list
title: मला दिलेल्या बॅच शोधा
routes: /Lab/TestEntry
for: 
roles: 
designations: 
source-hash: 3ec8fc5442d5b1f7
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Test Value Entry** स्क्रीनवर तुम्हाला दिलेल्या बॅच दिसतात. तपासणीचे आकडे टाकायला इथून सुरुवात करा किंवा पुढे चालू ठेवा.

## पायऱ्या

1. वरची कार्डे पाहा: **Assigned Batches**, **Pending Value Entry**, **Auto Result Ready** आणि **Report Generated**.
2. लिस्ट छोटी करण्यासाठी **Filters** उघडा आणि **Date Range**, **Sample Type**, **Status**, **Priority**, **Financial Year** किंवा **Assigned By** निवडा. **Apply Filters** दाबा. ते काढण्यासाठी **Clear All** दाबा.
3. सर्च बॉक्समध्ये बॅच किंवा consignment नंबर टाइप करा.
4. **Assigned Batch List** मध्ये **Pending Entries**, **Completed Entries**, **Priority** आणि **Status** तपासा. **High** प्राधान्यतेच्या बॅच आधी करा.
5. बॅच उघडण्यासाठी ओळ, किंवा **Action** कॉलममधला आयकॉन टॅप करा. आयकॉनचा अर्थ:
- **Start entry**: स्थिती **Pending Entry**, अजून काहीही टाकलेले नाही.
- **Continue entry**: स्थिती **In Progress**, काही आकडे सेव्ह झाले आहेत.
- **Preview results**: स्थिती **Auto Result Ready**, सगळे आकडे सबमिट झाले आहेत.
- **View**: स्थिती **Report Generated**, बॅच बंद झाली आहे.

## नेहमीच्या अडचणी

- **एखादी बॅच दिसत नाही.** ती दुसऱ्या ॲनालिस्टला दिलेली असेल, किंवा एखाद्या फिल्टरमुळे लपली असेल. **Clear filters** दाबा.
- **मला "Batches could not be loaded" दिसते.** तुमचे कनेक्शन तपासा आणि **Try again** दाबा.
:::

::: section enter-test-values
title: नमुन्याचे तपासणीचे आकडे टाका
routes: /Lab/TestEntry/{id}
for: 
roles: 
designations: 
source-hash: 0543541df7d26c4a
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Sample-wise Entry** वर तुम्ही बॅचमधल्या प्रत्येक नमुन्याचे तपासणीचे आकडे टाइप करता. सिस्टम सामान्य रेंज, निकाल, स्थिती आणि शिफारस आपोआप भरते.

## पायऱ्या

1. वरचा **Batch Summary** तपासा.
2. **Samples in this Batch** मध्ये ज्या नमुन्यावर काम करायचे त्याचा आयकॉन टॅप करा, किंवा **Next pending sample** दाबा.
3. **Selected Sample** चा तपशील तपासा (शेतकरी, गाव, पीक).
4. **Test Value Entry Table** मध्ये प्रत्येक आकडा **Test Value** मध्ये टाइप करा. **Texture** साठी लिस्टमधून निवडा. **Auto-calculated** खूण असलेल्या ओळी आपोआप भरतात.
5. तुम्ही टाइप करताना **Result**, **Status** आणि **Recommendation Hint** बदलताना पाहा. खाली **Recommendation Preview** आणि **Fertilizer Schedule (Kg/acre)** पाहा.
6. अपूर्ण काम ठेवण्यासाठी **Save Draft** दाबा.
7. सगळे आकडे टाकून झाल्यावर **Submit Values** दाबा, मग **Submit Test Values?** बॉक्समध्ये **Submit** दाबा.
8. **Submitted Successfully** मध्ये **Preview Report** किंवा **Cancel** दाबा, मग पुढचा नमुना निवडा.
9. सबमिट केलेला नमुना दुरुस्त करण्यासाठी **Edit Values** दाबा. बॅच पूर्ण होईपर्यंतच हे चालते.

## फील्डची माहिती

- **Normal Range**: या तपासणीसाठी चांगली रेंज.
- **Status**: **Normal**, **Deficient**, **Moderate** किंवा **Excess**, तुमच्या आकड्यावरून.
- **Overall Soil Status** (किंवा **Overall Water Status**): **Good**, **Needs Improvement** किंवा **Poor**, सगळ्या आकड्यांवरून.

## नेहमीच्या अडचणी

- **"Enter every parameter before submitting."** (सबमिट करण्याआधी सगळे घटक भरा) संदेशात नाव दिलेले घटक भरा.
- **"Fix the values that are not numbers."** (आकडे नसलेल्या नोंदी दुरुस्त करा) फक्त अंक आणि दशांश बिंदू (.) वापरा.
- **"Leave this sample?"** (हा नमुना सोडायचा?) तुमचे आकडे सेव्ह झालेले नाहीत. **Stay** दाबा, मग **Save Draft** दाबा.
- **सगळे लॉक झाले आहे.** बॅच पूर्ण झाली आहे. **View Report** दाबा.
:::

::: section analyst-test-reports
title: तपासणी रिपोर्ट आणि मागच्या वर्षांचे रिपोर्ट
routes: /Lab/TestEntry/Reports, /Lab/TestEntry/Reports/Previous
for: 
roles: 
designations: 
source-hash: 40645f32067eb759
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Test Reports** स्क्रीनवर या आर्थिक वर्षातल्या (एप्रिल ते मार्च) तुमच्या प्रत्येक बॅचचा रिपोर्ट सारांश दिसतो. **Previous FY Reports** मध्ये मागच्या वर्षांचे तसेच दिसते.

## पायऱ्या

1. वरची कार्डे पाहा: **Total Batches**, **Total Sample Reports**, **Reports Generated Today** आणि **Downloads Completed**.
2. जुने रिपोर्ट पाहण्यासाठी **Previous FY Reports** दाबा. परत येण्यासाठी **Current FY Reports** दाबा.
3. लिस्ट छोटी करण्यासाठी **Filters** उघडा आणि **Financial Year**, **Date Range**, **Sample Type**, **State**, **Region** किंवा **Head Quarters** निवडा. **Apply Filters** दाबा. ते काढण्यासाठी **Clear All** दाबा.
4. सर्च बॉक्समध्ये बॅच किंवा रिपोर्ट नंबर टाइप करा.
5. **Batch-wise Report Summary** मध्ये प्रत्येक बॅचसाठी **Reports Generated** आणि **Pending Reports** तपासा.
6. प्रत्येक नमुन्याच्या रिपोर्टसह **Batch Report Details** उघडण्यासाठी ओळ, किंवा **View reports** आयकॉन टॅप करा.

## नेहमीच्या अडचणी

- **मला "No batch reports found" दिसते.** कोऑर्डिनेटरनी बॅच पूर्ण केल्यावरच रिपोर्ट दिसतात. आर्थिक वर्ष आणि फिल्टरही तपासा.
- **मला "Reports are not available" दिसते.** रिपोर्ट सर्व्हिसकडून उत्तर आले नाही. थोड्या वेळाने परत प्रयत्न करा.
- **एखादा जुना रिपोर्ट दिसत नाही.** **Previous FY Reports** उघडा आणि त्याचे वर्ष निवडा.
:::

::: section batch-report-details
title: बॅचमधल्या नमुन्यांचे रिपोर्ट उघडा आणि डाउनलोड करा
routes: /Lab/TestEntry/Reports/{id}
for: 
roles: 
designations: 
source-hash: 683ffbc338437756
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Batch Report Details** मध्ये एका बॅचमधल्या प्रत्येक नमुन्याचा रिपोर्ट दिसतो. प्रत्येक नमुन्याचा त्याच्या शेतकऱ्यासाठी वेगळा रिपोर्ट असतो. इथे तुम्ही रिपोर्ट पाहता, डाउनलोड करता आणि प्रिंट झाले अशी नोंद करता.

## पायऱ्या

1. कार्डे पाहा: **Total Samples**, **Soil Samples**, **Water Samples**, **Reports Generated** आणि **Downloads Completed**.
2. **Batch Information** तपासा: बॅच, consignment, State, Region, **Head Quarters**, **Date of Dispatch** आणि **Financial Year**.
3. नमुना शोधण्यासाठी **Sample Type**, **Status**, **Crops** आणि **Villages** हे फिल्टर वापरा.
4. नमुन्याचा रिपोर्ट स्क्रीनवर उघडण्यासाठी **View** दाबा. त्यात निकाल, शिफारसी आणि खताचे वेळापत्रक दिसते.
5. एक रिपोर्ट इंग्रजीमध्ये सेव्ह करण्यासाठी **Download PDF** दाबा. दुसरी भाषा निवडण्यासाठी, जसे **Tamil**, **Telugu** किंवा **Marathi**, त्याच्या बाजूचा छोटा बाण दाबा.
6. बॅचचे सगळे रिपोर्ट एकदम सेव्ह करण्यासाठी **Download All Reports** दाबा. भाषा निवडण्यासाठी त्याचा बाण वापरा.
7. शेतकऱ्यासाठी रिपोर्ट प्रिंट केल्यावर **Mark printed** दाबा. त्याचा **Report Status** बदलून **Printed** होतो.

## फील्डची माहिती

- **Report Status**: **Generated** (तयार), **Downloaded** (किमान एकदा सेव्ह केला) किंवा **Printed**.
- **Reports No.**: शेतकऱ्याच्या रिपोर्टवर छापलेला रिपोर्ट नंबर.

## नेहमीच्या अडचणी

- **मला "This batch report is not available" दिसते.** बॅच अजून पूर्ण झालेली नाही. नमुने तपासण्यासाठी **Open the batch** दाबा.
- **Mark printed राखाडी आहे.** रिपोर्टची आधीच **Printed** अशी नोंद झाली आहे.
:::

::: section lab-tracking
title: ॲडमिनसाठी लॅब ट्रॅकिंग
routes: /Lab/Tracking
for: 
roles: 
designations: 
source-hash: dde8a3beb72cd9d3
confidence: ok

**ही स्क्रीन कशासाठी आहे**
**Lab Tracking** मध्ये ॲडमिनला लॅबमधल्या सगळ्या बॅच दिसतात. consignment, बॅच आणि तपासणीची प्रगती पाहण्यासाठी हे वापरा. इथे तुम्ही काहीही बदलू शकत नाही.

## पायऱ्या

1. वरची कार्डे पाहा: **Total Batches**, **Batch Created**, **Taken for analysis** आणि **Analysis Completed**.
2. एकच टप्पा पाहण्यासाठी टॅब वापरा: **All Batches**, **Batch Created**, **Taken for Analysis** किंवा **Analysis Completed**. प्रत्येक टॅबवर त्याचा आकडा दिसतो.
3. लिस्ट छोटी करण्यासाठी **Filters** उघडा आणि **Date Range**, **Batch No.**, **Consignment No.**, **Assigned To**, **State**, **Region** किंवा **Current Stage** निवडा. **Apply Filters** दाबा. ते काढण्यासाठी **Clear All** दाबा.
4. **Lab Tracking List** च्या वरच्या सर्च बॉक्समध्ये बॅच नंबर, consignment नंबर किंवा ॲनालिस्टचे नाव टाइप करा.
5. **Analysis Days** वाचा: **Pending** (पिवळा) म्हणजे सुरू झाले नाही, हिरवा म्हणजे वेळेवर, लाल म्हणजे उशीर झाला.
6. बॅच शेवटची कधी पुढे गेली ते पाहण्यासाठी **Last Updated** तपासा.
7. बॅचचे दृश्य उघडण्यासाठी बॅच नंबर किंवा डोळ्याचा आयकॉन टॅप करा.

## नेहमीच्या अडचणी

- **मला "No batches found" दिसते.** **Clear filters** दाबा आणि **All Batches** टॅब निवडा.
- **माझ्या मेनूमध्ये Lab Tracking नाही.** तुमच्या पदामध्ये Lab Tracking पेज असले पाहिजे. फक्त Admin रोल पुरेसा नाही. तुमच्या ॲडमिनिस्ट्रेटरला विचारा.
:::

::: section batch-view
title: ॲडमिन म्हणून एक बॅच पाहा
routes: /Lab/Tracking/{id}
for: 
roles: 
designations: 
source-hash: afb705e77b16edb3
confidence: ok

**ही स्क्रीन कशासाठी आहे**
बॅचच्या दृश्यात ॲडमिनला एका बॅचची पूर्ण कहाणी एकाच पेजवर दिसते: नमुने कुठून आले, कुरिअर, बॅच, तपासणीची प्रगती आणि टाइमलाइन. इथे फक्त पाहता येते.

## पायऱ्या

1. वरच्या पट्टीवर **Batch No.**, **Consignment No.**, **Test Samples**, **Samples**, **Assigned To (Analyst)**, **Batch Created On** आणि **Last Updated** दिसतात.
2. **Source Details** मध्ये नमुने कोणी गोळा केले, त्यांचा **Mobile No.**, **Location** आणि **Collection Date** दिसतात.
3. **Consignment Details** मध्ये **Dispatch Date**, **AWB / Tracking No** आणि **Expected Delivery Date** दिसतात. कुरिअरच्या साइटवर पार्सलचा मागोवा घेण्यासाठी ट्रॅकिंग नंबर टॅप करा.
4. **Batch Details** मध्ये नमुन्यांची संख्या, **Batch Created By**, **Batch Created Date** आणि **Assigned Analyst** दिसतात.
5. **Analysis Process** मध्ये प्रत्येक नमुना प्रकारासाठी किती नमुने **Completed** आणि **Not Started** आहेत ते **Completed %** पट्टीसह दिसते.
6. **Timeline** मध्ये प्रत्येक टप्पा दिसतो: **Consignment Delivered to Lab**, **Lab Entry Created**, **Batch Created**, **Taken for Analysis**, **Analysis Completed** आणि **Report Generated**, तारीख आणि ते कोणी केले यासह. राखाडी टप्पे अजून झालेले नाहीत.
7. बॅचचे नमुने Excel फाइल म्हणून डाउनलोड करण्यासाठी **Export** दाबा.
8. लिस्टवर परत जाण्यासाठी **Back to Lab Tracking** दाबा.

## नेहमीच्या अडचणी

- **मला "The batch could not be loaded" दिसते.** बॅच काढून टाकली असेल, किंवा तुम्हाला Lab Tracking ची परवानगी नसेल. **Try again** दाबा.
:::
