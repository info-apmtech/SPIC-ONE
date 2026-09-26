using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// Seed rows of the LabTranslation table (Key + Lang). English, Tamil, Telugu, Marathi.
///
/// Sources: the product owner's reference report (report (12).pdf: soil report in English, Tamil,
/// Marathi; irrigation water report in English, Tamil, Telugu) wherever it shows the wording;
/// everything else was written for this seed and is marked "// review" where a native speaker
/// should confirm the term (agronomy terms, Telugu soil labels the reference does not show).
///
/// Key families:
///   report.title.*, header.*, label.*, table.*, schedule.*, footer.*, signature.*, text.*,
///   group.{RecommendationGroup}, status.{LabOverallStatus}, response.{result word},
///   param.{LabParameter.Code}.name, product.{fertilizer}, crop.{crop}, texture.{option},
///   note.* (crop suitability templates with {crop} / {params}), hint.{English hint text}.
/// English header lines come from configuration Sas:Lab:ReportHeader; the en rows here are only
/// the fallback text for the other languages.
/// </summary>
public static class LabTranslationSeed
{
    public sealed record Entry(string Key, string En, string? Ta, string? Te, string? Mr);

    public static readonly string[] Languages = { "en", "ta", "te", "mr" };

    public static string CropNoteTemplate(LabReportRules.CropNoteKind kind) => kind switch
    {
        LabReportRules.CropNoteKind.SoilGood => "The soil is suitable for {crop}.",
        LabReportRules.CropNoteKind.SoilCorrect => "The soil is suitable for {crop} after correcting {params}.",
        LabReportRules.CropNoteKind.SoilPoor => "Correct {params} before growing {crop}.",
        LabReportRules.CropNoteKind.WaterGood => "Suitable for irrigation",
        _ => "Use for irrigation with care: {params} out of range."
    };

    public static string CropNoteKey(LabReportRules.CropNoteKind kind) => kind switch
    {
        LabReportRules.CropNoteKind.SoilGood => "note.soil.good",
        LabReportRules.CropNoteKind.SoilCorrect => "note.soil.correct",
        LabReportRules.CropNoteKind.SoilPoor => "note.soil.poor",
        LabReportRules.CropNoteKind.WaterGood => "note.water.good",
        _ => "note.water.caution"
    };

    public static readonly Entry[] Entries =
    {
        // ------------------------------------------------------------ titles and header
        new("report.title.soil", "SOIL SAMPLE ANALYTICAL REPORT", "மண் மாதிரி பகுப்பாய்வு அறிக்கை", "మట్టి నమూనా విశ్లేషణ నివేదిక", "माती नमुना विश्लेषणात्मक अहवाल"),
        new("report.title.water", "IRRIGATION WATER ANALYTICAL REPORT", "பாசனநீர் பரிசோதனை முடிவுகள்", "నీటిపారుదల నీటి విశ్లేషణ నివేదిక", "सिंचन पाणी विश्लेषणात्मक अहवाल"),
        new("header.title", "SPIC AGRICULTURE SERVICES", "ஸ்பிக் வேளாண் சேவை மையம்", "స్పిక్ అగ్రికల్చరల్ సర్వీసెస్", "स्पीक शेतीविषयक सुविधा"),
        new("header.address", "SPIC Ltd, Muthaiyahpuram, Tuticorin - 628005.", "SPIC லிமிடெட், முத்தையாபுரம், தூத்துக்குடி - 628 005.", "SPIC లిమిటెడ్, ముత్తయ్యపురం, తూత్తుకుడి - 628 005.", "स्पीक लिमिटेड, मुथीयापुरम, तुतिकोरिन - ६२८ ००५."),
        new("header.phone", "Phone : 235 6222, Cell : 83000 26080", "தொலைபேசி : 235 6222, செல் : 83000 26080", "ఫోన్ : 235 6222, సెల్ : 83000 26080", "फोन - २३५ ६२२२, सेल - ८३००० २६०८०"),
        new("header.customerCare", "Customer Service Centre: 044 230 0010", "வாடிக்கையாளர் சேவை மையம்: 044 230 0010", "కస్టమర్ సర్వీస్ సెంటర్: 044 230 0010", "ग्राहक सेवा केंद्र - ०४४ २३० ००१०"),

        // ------------------------------------------------------------ farmer block (soil)
        new("label.farmerName", "Farmer Name", "பெயர்", "రైతు పేరు", "नाव"),
        new("label.address", "Address", "முகவரி", "చిరునామా", "पत्ता"),
        new("label.mobile", "Mobile Number", "அலைபேசி எண்", "మొబైల్ నంబర్", "मोबाईल नंबर"),
        new("label.surveyNumber", "Survey Number", "சர்வே எண்", "సర్వే నంబర్", "जमीन सर्वे क्रमांक"),
        new("label.sampleNumber", "Sample Number", "மாதிரி எண்", "నమూనా నంబర్", "नमुना क्रमांक"),
        new("label.labNumber", "Lab Number", "ஆய்வுக்கூட எண்", "ల్యాబ్ నంబర్", "लॅब क्रमांक"),
        new("label.batchNumber", "Batch Number", "தொகுதி எண்", "బ్యాచ్ నంబర్", "बिल्ला क्रमांक"),
        new("label.crop", "Crop", "பயிர்", "పంట", "पीक"),

        // ------------------------------------------------------------ farmer block (water)
        new("label.farmerNameAddress", "Farmer's Name & Address", "விவசாயியின் பெயர் மற்றும் முகவரி:", "రైతు పేరు మరియు చిరునామా", "शेतकऱ्याचे नाव व पत्ता"),
        new("label.date", "Date", "தேதி", "తేదీ", "दिनांक"),
        new("label.labNo", "Lab No", "ஆய்வுக்கூட எண்", "ల్యాబ్ నంబర్", "लॅब क्रमांक"),

        // ------------------------------------------------------------ table headings
        new("table.parameters", "Parameters", "பரிசோதனை", "పారామితి", "मापदंड"),
        new("table.result", "Result", "இருப்பு", "ఫలితం", "इनपुट"),
        new("table.response", "Response", "குறிப்பு", "ప్రతిస్పందన", "प्रतिसाद"),
        new("table.optimumRange", "Optimum Range", "இருக்க வேண்டிய அளவு", "సరైన పరిధి", "इष्टतम पातळी"),
        new("table.sno", "S. No", "S. No", "S. No", "अ. क्र."),
        new("table.result.water", "Result", "முடிவுகள்", "ఫలితాలు", "निकाल"),
        new("table.remarks", "Remarks", "குறிப்பு", "వ్యాఖ్యలు", "शेरा"),
        new("table.unit", "Unit", "அலகு", "యూనిట్", "एकक"),

        // ------------------------------------------------------------ sections
        new("label.recommendations", "Recommendations", "பரிந்துரைகள்", "సిఫార్సులు", "शिफारसी"), // review mr (reference prints "स्वभावतः")
        new("label.fertilizerSchedule", "Recommendations (Kg/acre)", "மண் பரிசோதனையின் படி உர சிபாரிசுகள் (கிலோ / ஏக்கர்)", "ఎరువుల సిఫార్సులు (కిలో / ఎకరం)", "माती परीक्षणाच्या शिफारशीनुसार आलेले परिणाम (किलो / एकर)"),
        new("label.note", "Note", "குறிப்பு", "గమనిక", "टीप"),
        new("label.cropNote", "Crop Suitability Note", "பயிர் பொருத்தக் குறிப்பு", "పంట అనుకూలత గమనిక", "पीक योग्यता टीप"),
        new("label.overallStatus", "Overall Soil Status", "மண்ணின் ஒட்டுமொத்த நிலை", "నేల మొత్తం స్థితి", "जमिनीची एकूण स्थिती"),
        new("text.nil", "Nil", "இல்லை", "శూన్యం", "निरंक"),
        new("text.suitableForIrrigation", "Suitable for irrigation", "சிறந்த பாசன நீர்", "నీటిపారుదలకి అనుకూలం", "सिंचनासाठी योग्य"),
        new("text.reportNo", "Report No", "அறிக்கை எண்", "నివేదిక సంఖ్య", "अहवाल क्रमांक"),
        new("text.generatedOn", "Generated on", "உருவாக்கிய தேதி", "తయారైన తేదీ", "तयार दिनांक"),
        new("text.proposedCrop", "the proposed crop", "பயிர்", "పంట", "पीक"),

        // ------------------------------------------------------------ recommendation headings
        new("group.Fertilizer", "Fertilizer Recommendation", "உர பரிந்துரை", "ఎరువుల సిఫార్సు", "खत शिफारस"),
        new("group.Organic", "Organic Recommendation", "அங்கக உர பரிந்துரை", "సేంద్రియ సిఫార్సు", "सेंद्रिय शिफारस"),
        new("group.Micronutrient", "Micronutrient Recommendation", "நுண்ணூட்டச் சத்து பரிந்துரை", "సూక్ష్మపోషకాల సిఫార్సు", "सूक्ष्म अन्नद्रव्य शिफारस"),
        new("group.General", "General Recommendation", "பொது பரிந்துரை", "సాధారణ సిఫార్సు", "सामान्य शिफारस"),

        // ------------------------------------------------------------ overall status
        new("status.Good", "Good", "நல்லது", "మంచిది", "चांगली"),
        new("status.NeedsImprovement", "Needs Improvement", "மேம்பாடு தேவை", "మెరుగుదల అవసరం", "सुधारणा आवश्यक"),
        new("status.Poor", "Poor", "மோசம்", "బలహీనం", "कमकुवत"),

        // ------------------------------------------------------------ response words
        new("response.Low", "Low", "குறைவு", "తక్కువ", "कमी"),
        new("response.Medium", "Medium", "மிதம்", "మధ్యస్థం", "मध्यम"),
        new("response.High", "High", "அதிகம்", "ఎక్కువ", "जास्त"),
        new("response.Normal", "Normal", "மிதம்", "సాధారణ", "सामान्य"),
        new("response.Moderate", "Moderate", "மிதம்", "మధ్యస్థం", "मध्यम"),
        new("response.Critical", "Critical", "பாதிக்கக்கூடியது", "క్లిష్టమైన", "चिंताजनक"),
        new("response.Safe", "Safe", "பாதுகாப்பானது", "సురక్షితం", "सुरक्षित"),
        new("response.Saline", "Saline", "உவர்ப்பு", "లవణీయం", "क्षारयुक्त"),
        new("response.Neutral", "Neutral", "நடுநிலை", "తటస్థం", "उदासीन"), // review
        new("response.Acidic", "Acidic", "அமிலத்தன்மை", "ఆమ్ల", "आम्लीय"),
        new("response.Alkaline", "Alkaline", "காரத்தன்மை", "క్షార", "अल्कधर्मी"),
        new("response.Good", "Good", "நல்லது", "మంచిది", "चांगले"),
        new("response.Deficient", "Deficient", "பற்றாக்குறை", "లోపం", "कमतरता"),
        new("response.Excess", "Excess", "மிகுதி", "అధికం", "अतिरिक्त"),
        new("response.Gypsum Not Required", "Gypsum Not Required", "ஸ்பிக் ஜிப்சம் தேவையில்லை", "జిప్సం అవసరం లేదు", "जिप्सम आवश्यक नाही"),

        // ------------------------------------------------------------ parameters (soil)
        new("param.S-TEX.name", "Texture", "மண் அமைப்பு", "నేల ఆకృతి", "पोत"),
        new("param.S-PH.name", "pH", "அமில-கார நிலை", "ఉదజని సూచిక", "पी एच"),
        new("param.S-EC.name", "EC", "உப்பின் நிலை", "విద్యుత్ వాహకత", "इ सि"),
        new("param.S-OC.name", "Organic Carbon", "அங்ககக் கார்பன்", "సేంద్రియ కర్బనం", "सेंद्रीय करब"),
        new("param.S-OM.name", "Organic Matter", "அங்ககப் பொருள்", "సేంద్రియ పదార్థం", "सेंद्रीय घटक"),
        new("param.S-N.name", "Nitrogen", "தழைச்சத்து", "నత్రజని", "नत्र"),
        new("param.S-P.name", "Phosphorus", "மணிச்சத்து", "భాస్వరం", "स्फुरद"),
        new("param.S-K.name", "Potassium", "சாம்பல் சத்து", "పొటాష్", "पोटॅश"),
        new("param.S-ZN.name", "Zinc", "சிங்க்", "జింక్", "झिंक"),
        new("param.S-FE.name", "Iron", "இரும்பு", "ఇనుము", "लोह"),
        new("param.S-MN.name", "Manganese", "மாங்கனீசு", "మాంగనీస్", "मॅंगनीज"),
        new("param.S-CU.name", "Copper", "காப்பர்", "రాగి", "कॉपर"),
        new("param.S-B.name", "Boron", "போரான்", "బోరాన్", "बोरॉन"),
        new("param.S-S.name", "Sulphur", "சல்பர்", "గంధకం", "सल्फर"),

        // ------------------------------------------------------------ parameters (water)
        new("param.W-PH.name", "pH", "கார அமிலத்தன்மை", "ఉదజనిసూచిక", "सामू (पी एच)"),
        new("param.W-EC.name", "EC", "கரைந்துள்ள உப்புக்களின் அளவு", "విద్యుత్ వాహకత", "विद्युत वाहकता"),
        new("param.W-TDS.name", "Total Dissolved Solids", "மொத்த கரைந்த திடப்பொருட்கள்", "మొత్తం కరిగిన ఘనపదార్థాలు", "एकूण विरघळलेले घन पदार्थ"),
        new("param.W-CL.name", "Chloride", "குளோரைடு", "క్లోరైడ్స్", "क्लोराईड"),
        new("param.W-SO4.name", "Sulphate", "சல்பேட்", "సల్ఫేట్", "सल्फेट"),
        new("param.W-CO3.name", "Carbonate", "கார்பனேட்", "కార్బొనేట్", "कार्बोनेट"),
        new("param.W-HCO3.name", "Bicarbonate", "பை-கார்பனேட்", "బైకార్బొనేట్లు", "बायकार्बोनेट"),
        new("param.W-NA.name", "Sodium", "சோடியம்", "సోడియం", "सोडियम"),
        new("param.W-CA.name", "Calcium", "கால்சியம்", "కాల్షియం", "कॅल्शियम"),
        new("param.W-MG.name", "Magnesium", "மெக்னீசியம்", "మెగ్నీషియం", "मॅग्नेशियम"),
        new("param.W-SAR.name", "Sodium Adsorption Ratio", "உட்கிரகிக்கும் சோடிய விகிதம்", "సోడియం శోషణ నిష్పత్తి", "सोडियम शोषण गुणोत्तर"),
        new("param.W-RSC.name", "Residual Sodium Carbonate", "எஞ்சியுள்ள சோடியம் கார்பனேட்", "అవశేష సోడియం కార్బొనేట్", "अवशिष्ट सोडियम कार्बोनेट"),
        new("param.W-NO3.name", "Nitrate", "நைட்ரேட்", "నైట్రేట్", "नायट्रेट"),
        new("param.W-MB.name", "Total Coliforms", "மொத்த கோலிஃபார்ம்", "మొత్తం కోలిఫారాలు", "एकूण कोलिफॉर्म"), // review

        // ------------------------------------------------------------ soil texture options
        new("texture.Sandy", "Sandy", "மணல்", "ఇసుక", "वाळू"),
        new("texture.Loamy Sand", "Loamy Sand", "வண்டல் மணல்", "ఒండ్రు ఇసుక", "पोयटायुक्त वाळू"), // review
        new("texture.Sandy Loam", "Sandy Loam", "மணல் கலந்த வண்டல்", "ఇసుక ఒండ్రు", "वालुकामय पोयटा"), // review
        new("texture.Loam", "Loam", "வண்டல்", "ఒండ్రు నేల", "पोयटा"),
        new("texture.Silt Loam", "Silt Loam", "நுண்மணல் வண்டல்", "సిల్ట్ ఒండ్రు", "गाळाचा पोयटा"), // review
        new("texture.Clay Loam", "Clay Loam", "களி வண்டல்", "బంక ఒండ్రు", "चिकण पोयटा"), // review
        new("texture.Sandy Clay", "Sandy Clay", "மணல் கலந்த களி", "ఇసుక బంకమట్టి", "वालुकामय चिकणमाती"),
        new("texture.Silty Clay", "Silty Clay", "நுண்மணல் களி", "సిల్ట్ బంకమట్టి", "गाळयुक्त चिकणमाती"), // review
        new("texture.Clay", "Clay", "களிமண்", "బంకమట్టి", "चिकणमाती"),
        new("texture.Sandy Clay Silt", "Sandy Clay Silt", "களி மணல் கலந்த வண்டல்", "ఇసుక బంకమట్టి ఒండ్రు", "मातवाळू"), // review te

        // ------------------------------------------------------------ fertilizer schedule
        new("schedule.basal", "Basal Application", "அடியுரம்", "దుక్కిలో ఎరువు", "बेसल अप्लिकेशन"), // review te
        new("schedule.topDressing", "Top Dressing", "மேலுரம்", "పైపాటు ఎరువు", "टॉप ड्रेसिंग"), // review te
        new("schedule.app1", "1st Application", "முதல் மேலுரம்", "1వ దఫా", "१ ली टॉप ड्रेसिंग"),
        new("schedule.app2", "2nd Application", "இரண்டாம் மேலுரம்", "2వ దఫా", "२ री टॉप ड्रेसिंग"),
        new("schedule.app3", "3rd Application", "மூன்றாம் மேலுரம்", "3వ దఫా", "३ री टॉप ड्रेसिंग"),
        new("schedule.thDay", "th day", "வது நாள்", "వ రోజు", "वा दिवस"),
        new("schedule.general", "General", "பொது", "సాధారణ", "सामान्य"),
        new("schedule.generalNote", "No fertilizer schedule is configured for {crop}; the general schedule is shown.", "{crop} பயிருக்கு உர அட்டவணை இல்லை; பொது அட்டவணை காட்டப்பட்டுள்ளது.", "{crop} పంటకు ఎరువుల పట్టిక లేదు; సాధారణ పట్టిక చూపబడింది.", "{crop} पिकासाठी खत वेळापत्रक उपलब्ध नाही; सामान्य वेळापत्रक दाखवले आहे."),
        new("product.SPIC Jyoti", "SPIC Jyoti", "ஸ்பிக் ஜோதி", "స్పిక్ జ్యోతి", "स्पीक ज्योती"),
        new("product.SPIC Gypsum", "SPIC Gypsum", "ஜிப்ஸம்", "స్పిక్ జిప్సం", "स्पीक जिप्सम"),
        new("product.SPIC Sangamam", "SPIC Sangamam", "சங்கமம்", "స్పిక్ సంగమం", "स्पीक संगमम"),
        new("product.SPIC DAP", "SPIC DAP", "டிஏபி", "స్పిక్ డిఏపి", "स्पीक डी एपी"),
        new("product.SPIC Urea", "SPIC Urea", "யூரியா", "స్పిక్ యూరియా", "स्पीक युरीया"),
        new("product.Potash", "Potash", "சாம்பல் சத்து", "పొటాష్", "पोटॅश"),
        new("product.SPIC Zinc Sulphate", "SPIC Zinc Sulphate", "துத்தநாக சல்பேட்", "స్పిక్ జింక్ సల్ఫేట్", "झिंक सल्फेट"),
        new("product.Ferrous Sulphate", "Ferrous Sulphate", "இரும்பு சல்பேட்", "ఫెర్రస్ సల్ఫేట్", "फेरस सल्फेट"),
        new("product.Manganese Sulphate", "Manganese Sulphate", "மாங்கனீசு சல்பேட்", "మాంగనీస్ సల్ఫేట్", "मॅंगनीज सल्फेट"),
        new("product.Copper Sulphate", "Copper Sulphate", "காப்பர் சல்பேட்", "కాపర్ సల్ఫేట్", "कॉपर सल्फेट"),

        // ------------------------------------------------------------ crops (v1 fallback list)
        new("crop.Banana", "Banana", "வாழை", "అరటి", "केळी"),
        new("crop.Paddy", "Paddy", "நெல்", "వరి", "भात"),
        new("crop.Wheat", "Wheat", "கோதுமை", "గోధుమ", "गहू"),
        new("crop.Tomato", "Tomato", "தக்காளி", "టమాటా", "टोमॅटो"),
        new("crop.Sugarcane", "Sugarcane", "கரும்பு", "చెరకు", "ऊस"),
        new("crop.Cotton", "Cotton", "பருத்தி", "పత్తి", "कापूस"),
        new("crop.Groundnut", "Groundnut", "நிலக்கடலை", "వేరుశనగ", "भुईमूग"),
        new("crop.Maize", "Maize", "மக்காச்சோளம்", "మొక్కజొన్న", "मका"),

        // ------------------------------------------------------------ closing line and signatures
        new("footer.slogan", "!! Healthy Soil. Wealthy Farmer. !!", "மண்ணின் வளமே ! விவசாயியின் நலம் !", "!! ఆరోగ్యకరమైన నేల. సంపన్న రైతు. !!", "!! सशक्त जमिन, संपन्न किसान !!"),
        new("signature.soil.role", "OFFICER", "அதிகாரி", "అధికారి", "अधिकारी"),
        new("signature.soil.org", "SPIC AGRICULTURE SERVICES", "ஸ்பிக் வேளாண் சேவை மையம்", "స్పిక్ వ్యవసాయ సేవా కేంద్రం", "स्पीक शेतीविषयक सुविधा"),
        new("signature.water.role", "AUTHORIZED SIGNATORY", "அதிகாரி", "అధికారి", "अधिकृत स्वाक्षरीकर्ता"),
        new("signature.water.org", "SPIC SOIL TESTING LAB", "ஸ்பிக் வேளாண் சேவை மையம்", "స్పిక్ వ్యవసాయ సేవా కేంద్రం", "स्पीक माती परीक्षण प्रयोगशाळा"),

        // ------------------------------------------------------------ crop suitability notes
        new("note.soil.good", "The soil is suitable for {crop}.", "இந்த மண் {crop} பயிருக்கு ஏற்றது.", "ఈ నేల {crop} పంటకు అనుకూలం.", "ही जमीन {crop} पिकासाठी योग्य आहे."),
        new("note.soil.correct", "The soil is suitable for {crop} after correcting {params}.", "{params} சரிசெய்த பின் இந்த மண் {crop} பயிருக்கு ஏற்றது.", "{params} సరిచేసిన తర్వాత ఈ నేల {crop} పంటకు అనుకూలం.", "{params} सुधारल्यानंतर ही जमीन {crop} पिकासाठी योग्य आहे."),
        new("note.soil.poor", "Correct {params} before growing {crop}.", "{crop} பயிரிடும் முன் {params} சரிசெய்யவும்.", "{crop} పంట వేసే ముందు {params} సరిచేయండి.", "{crop} पीक घेण्यापूर्वी {params} सुधारा."),
        new("note.water.good", "Suitable for irrigation", "சிறந்த பாசன நீர்", "నీటిపారుదలకి అనుకూలం", "सिंचनासाठी योग्य"),
        new("note.water.caution", "Use for irrigation with care: {params} out of range.", "கவனத்துடன் பாசனத்திற்கு பயன்படுத்தவும்: {params} வரம்பிற்கு வெளியே உள்ளது.", "జాగ్రత్తగా నీటిపారుదలకు వాడండి: {params} పరిధి దాటి ఉన్నాయి.", "सिंचनासाठी काळजीपूर्वक वापरा: {params} मर्यादेबाहेर आहे."),

        // ------------------------------------------------------------ recommendation lines (LabParameter hints)
        new("hint.Apply lime to correct soil acidity", "Apply lime to correct soil acidity", "மண்ணின் அமிலத்தன்மையை சரிசெய்ய சுண்ணாம்பு இடவும்", "నేల ఆమ్లత్వాన్ని సరిచేయడానికి సున్నం వేయండి", "जमिनीची आम्लता सुधारण्यासाठी चुना वापरा"),
        new("hint.Suitable for crop growth", "Suitable for crop growth", "பயிர் வளர்ச்சிக்கு ஏற்றது", "పంట పెరుగుదలకు అనుకూలం", "पीक वाढीसाठी योग्य"),
        new("hint.Apply gypsum to reduce alkalinity", "Apply gypsum to reduce alkalinity", "காரத்தன்மையைக் குறைக்க ஜிப்சம் இடவும்", "క్షారత్వాన్ని తగ్గించడానికి జిప్సం వేయండి", "क्षारता कमी करण्यासाठी जिप्सम वापरा"),
        new("hint.No salinity issue", "No salinity issue", "உவர்ப்பு பிரச்சினை இல்லை", "లవణీయత సమస్య లేదు", "क्षारतेची समस्या नाही"),
        new("hint.Improve drainage and leach salts", "Improve drainage and leach salts", "வடிகால் வசதியை மேம்படுத்தி உப்புகளை வெளியேற்றவும்", "మురుగు నీటి పారుదల మెరుగుపరచి లవణాలను కడిగివేయండి", "निचरा सुधारा आणि क्षार धुऊन काढा"),
        new("hint.Add organic manure / compost", "Add organic manure / compost", "தொழு உரம் / மட்கிய உரம் இடவும்", "పశువుల ఎరువు / కంపోస్ట్ వేయండి", "शेणखत / कंपोस्ट खत वापरा"),
        new("hint.Maintain organic matter", "Maintain organic matter", "அங்ககப் பொருளைப் பராமரிக்கவும்", "సేంద్రియ పదార్థాన్ని కాపాడండి", "सेंद्रिय घटक टिकवून ठेवा"),
        new("hint.Organic matter is high; no addition needed", "Organic matter is high; no addition needed", "அங்ககப் பொருள் அதிகம்; கூடுதலாக இட வேண்டியதில்லை", "సేంద్రియ పదార్థం ఎక్కువగా ఉంది; అదనంగా అవసరం లేదు", "सेंद्रिय घटक जास्त आहेत; अधिक देण्याची गरज नाही"),
        new("hint.Apply nitrogen fertilizer", "Apply nitrogen fertilizer", "தழைச்சத்து உரம் இடவும்", "నత్రజని ఎరువు వేయండి", "नत्रयुक्त खत द्या"),
        new("hint.Maintain standard nitrogen dosage", "Maintain standard nitrogen dosage", "வழக்கமான தழைச்சத்து அளவைப் பின்பற்றவும்", "సాధారణ నత్రజని మోతాదు పాటించండి", "नत्राची नेहमीची मात्रा ठेवा"),
        new("hint.Reduce nitrogen-based fertilizer", "Reduce nitrogen-based fertilizer", "தழைச்சத்து உரத்தைக் குறைக்கவும்", "నత్రజని ఎరువును తగ్గించండి", "नत्रयुक्त खत कमी करा"),
        new("hint.Apply phosphorus fertilizer", "Apply phosphorus fertilizer", "மணிச்சத்து உரம் இடவும்", "భాస్వరం ఎరువు వేయండి", "स्फुरदयुक्त खत द्या"),
        new("hint.Maintain standard phosphorus dosage", "Maintain standard phosphorus dosage", "வழக்கமான மணிச்சத்து அளவைப் பின்பற்றவும்", "సాధారణ భాస్వరం మోతాదు పాటించండి", "स्फुरदाची नेहमीची मात्रा ठेवा"),
        new("hint.Reduce phosphorus-based fertilizer", "Reduce phosphorus-based fertilizer", "மணிச்சத்து உரத்தைக் குறைக்கவும்", "భాస్వరం ఎరువును తగ్గించండి", "स्फुरदयुक्त खत कमी करा"),
        new("hint.Apply potassium fertilizer", "Apply potassium fertilizer", "சாம்பல் சத்து உரம் இடவும்", "పొటాష్ ఎరువు వేయండి", "पालाशयुक्त खत द्या"),
        new("hint.Maintain standard potassium dosage", "Maintain standard potassium dosage", "வழக்கமான சாம்பல் சத்து அளவைப் பின்பற்றவும்", "సాధారణ పొటాష్ మోతాదు పాటించండి", "पालाशची नेहमीची मात्रा ठेवा"),
        new("hint.Reduce potassium-based fertilizer", "Reduce potassium-based fertilizer", "சாம்பல் சத்து உரத்தைக் குறைக்கவும்", "పొటాష్ ఎరువును తగ్గించండి", "पालाशयुक्त खत कमी करा"),
        new("hint.Apply zinc micronutrient", "Apply zinc micronutrient", "துத்தநாக நுண்ணூட்டம் இடவும்", "జింక్ సూక్ష్మపోషకం వేయండి", "झिंक सूक्ष्म अन्नद्रव्य द्या"),
        new("hint.Zinc is adequate", "Zinc is adequate", "துத்தநாகம் போதுமான அளவில் உள்ளது", "జింక్ తగినంత ఉంది", "झिंक पुरेसे आहे"),
        new("hint.Avoid further zinc application", "Avoid further zinc application", "மேலும் துத்தநாகம் இட வேண்டாம்", "ఇంకా జింక్ వేయవద్దు", "आणखी झिंक देऊ नका"),
        new("hint.Apply iron micronutrient (ferrous sulphate)", "Apply iron micronutrient (ferrous sulphate)", "இரும்பு நுண்ணூட்டம் (இரும்பு சல்பேட்) இடவும்", "ఇనుము సూక్ష్మపోషకం (ఫెర్రస్ సల్ఫేట్) వేయండి", "लोह सूक्ष्म अन्नद्रव्य (फेरस सल्फेट) द्या"),
        new("hint.Iron is adequate", "Iron is adequate", "இரும்புச்சத்து போதுமான அளவில் உள்ளது", "ఇనుము తగినంత ఉంది", "लोह पुरेसे आहे"),
        new("hint.Avoid further iron application", "Avoid further iron application", "மேலும் இரும்புச்சத்து இட வேண்டாம்", "ఇంకా ఇనుము వేయవద్దు", "आणखी लोह देऊ नका"),
        new("hint.Apply manganese micronutrient", "Apply manganese micronutrient", "மாங்கனீசு நுண்ணூட்டம் இடவும்", "మాంగనీస్ సూక్ష్మపోషకం వేయండి", "मॅंगनीज सूक्ष्म अन्नद्रव्य द्या"),
        new("hint.Manganese is adequate", "Manganese is adequate", "மாங்கனீசு போதுமான அளவில் உள்ளது", "మాంగనీస్ తగినంత ఉంది", "मॅंगनीज पुरेसे आहे"),
        new("hint.Avoid further manganese application", "Avoid further manganese application", "மேலும் மாங்கனீசு இட வேண்டாம்", "ఇంకా మాంగనీస్ వేయవద్దు", "आणखी मॅंगनीज देऊ नका"),
        new("hint.Apply copper micronutrient", "Apply copper micronutrient", "காப்பர் நுண்ணூட்டம் இடவும்", "రాగి సూక్ష్మపోషకం వేయండి", "तांबे (कॉपर) सूक्ष्म अन्नद्रव्य द्या"),
        new("hint.Copper is adequate", "Copper is adequate", "காப்பர் போதுமான அளவில் உள்ளது", "రాగి తగినంత ఉంది", "तांबे पुरेसे आहे"),
        new("hint.Avoid further copper application", "Avoid further copper application", "மேலும் காப்பர் இட வேண்டாம்", "ఇంకా రాగి వేయవద్దు", "आणखी तांबे देऊ नका"),
        new("hint.Apply borax", "Apply borax", "போராக்ஸ் இடவும்", "బోరాక్స్ వేయండి", "बोरॅक्स द्या"),
        new("hint.Boron is adequate", "Boron is adequate", "போரான் போதுமான அளவில் உள்ளது", "బోరాన్ తగినంత ఉంది", "बोरॉन पुरेसे आहे"),
        new("hint.Avoid further boron application", "Avoid further boron application", "மேலும் போரான் இட வேண்டாம்", "ఇంకా బోరాన్ వేయవద్దు", "आणखी बोरॉन देऊ नका"),
        new("hint.Apply sulphur (gypsum)", "Apply sulphur (gypsum)", "சல்பர் (ஜிப்சம்) இடவும்", "గంధకం (జిప్సం) వేయండి", "गंधक (जिप्सम) द्या"),
        new("hint.Sulphur is adequate", "Sulphur is adequate", "சல்பர் போதுமான அளவில் உள்ளது", "గంధకం తగినంత ఉంది", "गंधक पुरेसे आहे"),
        new("hint.Avoid further sulphur application", "Avoid further sulphur application", "மேலும் சல்பர் இட வேண்டாம்", "ఇంకా గంధకం వేయవద్దు", "आणखी गंधक देऊ नका"),
        new("hint.Water is acidic; neutralise before use", "Water is acidic; neutralise before use", "நீர் அமிலத்தன்மை கொண்டது; பயன்படுத்தும் முன் நடுநிலையாக்கவும்", "నీరు ఆమ్లంగా ఉంది; వాడే ముందు తటస్థీకరించండి", "पाणी आम्लीय आहे; वापरण्यापूर्वी उदासीन करा"),
        new("hint.Suitable for irrigation", "Suitable for irrigation", "சிறந்த பாசன நீர்", "నీటిపారుదలకి అనుకూలం", "सिंचनासाठी योग्य"),
        new("hint.Water is alkaline; treat before use", "Water is alkaline; treat before use", "நீர் காரத்தன்மை கொண்டது; பயன்படுத்தும் முன் சுத்திகரிக்கவும்", "నీరు క్షారంగా ఉంది; వాడే ముందు శుద్ధి చేయండి", "पाणी अल्कधर्मी आहे; वापरण्यापूर्वी प्रक्रिया करा"),
        new("hint.Saline water; blend with fresh water", "Saline water; blend with fresh water", "உவர் நீர்; நல்ல நீருடன் கலந்து பயன்படுத்தவும்", "లవణ నీరు; మంచి నీటితో కలిపి వాడండి", "क्षारयुक्त पाणी; गोड्या पाण्यात मिसळून वापरा"),
        new("hint.No issue", "No issue", "பிரச்சினை இல்லை", "సమస్య లేదు", "समस्या नाही"),
        new("hint.High dissolved solids; use with caution", "High dissolved solids; use with caution", "கரைந்த திடப்பொருட்கள் அதிகம்; கவனத்துடன் பயன்படுத்தவும்", "కరిగిన ఘనపదార్థాలు ఎక్కువ; జాగ్రత్తగా వాడండి", "विरघळलेले घन पदार्थ जास्त; काळजीपूर्वक वापरा"),
        new("hint.Chloride is high; avoid on sensitive crops", "Chloride is high; avoid on sensitive crops", "குளோரைடு அதிகம்; உணர்திறன் மிக்க பயிர்களுக்கு தவிர்க்கவும்", "క్లోరైడ్ ఎక్కువ; సున్నితమైన పంటలకు వాడవద్దు", "क्लोराईड जास्त; संवेदनशील पिकांसाठी टाळा"),
        new("hint.Sulphate is high", "Sulphate is high", "சல்பேட் அதிகம்", "సల్ఫేట్ ఎక్కువ", "सल्फेट जास्त"),
        new("hint.Carbonate is high; apply gypsum", "Carbonate is high; apply gypsum", "கார்பனேட் அதிகம்; ஜிப்சம் இடவும்", "కార్బొనేట్ ఎక్కువ; జిప్సం వేయండి", "कार्बोनेट जास्त; जिप्सम वापरा"),
        new("hint.Bicarbonate is high; apply gypsum", "Bicarbonate is high; apply gypsum", "பை-கார்பனேட் அதிகம்; ஜிப்சம் இடவும்", "బైకార్బొనేట్ ఎక్కువ; జిప్సం వేయండి", "बायकार्बोनेट जास्त; जिप्सम वापरा"),
        new("hint.Sodium is high; risk of sodicity", "Sodium is high; risk of sodicity", "சோடியம் அதிகம்; களர் தன்மை ஏற்படும் அபாயம்", "సోడియం ఎక్కువ; చౌడు ప్రమాదం", "सोडियम जास्त; जमीन चोपण होण्याचा धोका"),
        new("hint.Calcium is low", "Calcium is low", "கால்சியம் குறைவு", "కాల్షియం తక్కువ", "कॅल्शियम कमी"),
        new("hint.Calcium is adequate", "Calcium is adequate", "கால்சியம் போதுமான அளவில் உள்ளது", "కాల్షియం తగినంత ఉంది", "कॅल्शियम पुरेसे आहे"),
        new("hint.Calcium is high", "Calcium is high", "கால்சியம் அதிகம்", "కాల్షియం ఎక్కువ", "कॅल्शियम जास्त"),
        new("hint.Magnesium is low", "Magnesium is low", "மெக்னீசியம் குறைவு", "మెగ్నీషియం తక్కువ", "मॅग्नेशियम कमी"),
        new("hint.Magnesium is adequate", "Magnesium is adequate", "மெக்னீசியம் போதுமான அளவில் உள்ளது", "మెగ్నీషియం తగినంత ఉంది", "मॅग्नेशियम पुरेसे आहे"),
        new("hint.Magnesium is high", "Magnesium is high", "மெக்னீசியம் அதிகம்", "మెగ్నీషియం ఎక్కువ", "मॅग्नेशियम जास्त"),
        new("hint.No sodicity hazard", "No sodicity hazard", "களர் அபாயம் இல்லை", "చౌడు ప్రమాదం లేదు", "सोडियमचा धोका नाही"),
        new("hint.Sodicity hazard; apply gypsum", "Sodicity hazard; apply gypsum", "களர் அபாயம்; ஜிப்சம் இடவும்", "చౌడు ప్రమాదం; జిప్సం వేయండి", "सोडियमचा धोका; जिप्सम वापरा"),
        new("hint.Safe for irrigation", "Safe for irrigation", "பாசனத்திற்கு பாதுகாப்பானது", "నీటిపారుదలకు సురక్షితం", "सिंचनासाठी सुरक्षित"),
        new("hint.Unsuitable without gypsum treatment", "Unsuitable without gypsum treatment", "ஜிப்சம் சுத்திகரிப்பு இல்லாமல் பயன்படுத்த ஏற்றதல்ல", "జిప్సం శుద్ధి లేకుండా వాడటానికి అనుకూలం కాదు", "जिप्सम प्रक्रियेशिवाय वापरण्यास अयोग्य"),
        new("hint.Nitrate is high", "Nitrate is high", "நைட்ரேட் அதிகம்", "నైట్రేట్ ఎక్కువ", "नायट्रेट जास्त"),
        new("hint.No contamination", "No contamination", "மாசு இல்லை", "కాలుష్యం లేదు", "प्रदूषण नाही"),
        new("hint.Microbial contamination; disinfect before use", "Microbial contamination; disinfect before use", "நுண்ணுயிர் மாசு; பயன்படுத்தும் முன் கிருமி நீக்கம் செய்யவும்", "సూక్ష్మజీవుల కాలుష్యం; వాడే ముందు క్రిమిసంహారం చేయండి", "सूक्ष्मजीव प्रदूषण; वापरण्यापूर्वी निर्जंतुक करा"),
    };

    /// <summary>(Key, Lang, Text) rows of the seed (null translations are skipped: English fallback).</summary>
    public static IEnumerable<LabTranslation> Rows()
    {
        foreach (var e in Entries)
        {
            yield return new LabTranslation { Key = e.Key, Lang = "en", Text = e.En };
            if (!string.IsNullOrWhiteSpace(e.Ta)) yield return new LabTranslation { Key = e.Key, Lang = "ta", Text = e.Ta! };
            if (!string.IsNullOrWhiteSpace(e.Te)) yield return new LabTranslation { Key = e.Key, Lang = "te", Text = e.Te! };
            if (!string.IsNullOrWhiteSpace(e.Mr)) yield return new LabTranslation { Key = e.Key, Lang = "mr", Text = e.Mr! };
        }
    }
}
