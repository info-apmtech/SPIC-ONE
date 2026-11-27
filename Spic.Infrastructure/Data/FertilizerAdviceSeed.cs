using SPIC.Core.Entities;

namespace Spic.Infrastructure.Data;

/// <summary>
/// Seed of the fertilizer recommendation (farmer portal Task 4, migration V36_FertilizerAdvice). Every value
/// was read on the source page named in the row (TNAU Agritech portal pages, fetched 2026-11-28 through the
/// Internet Archive because agritech.tnau.ac.in refused direct connections). Doses are the TNAU "blanket
/// recommendation" (used when no soil test is available); the advisor scales them by the soil test rating.
/// Crops whose sources disagree (sugarcane, irrigated groundnut) or lack a stage schedule (hybrid rice) are
/// left out on purpose; see docs/farmer-portal.md, Task 4.
///
/// Units: PerHectare rows are kg/ha of N, P2O5, K2O; PerPlant rows are grams per plant (banana, one crop) or
/// per palm (coconut, one year). The splits of a norm add up to its totals (regression harness).
/// </summary>
public static class FertilizerAdviceSeed
{
    private const string TnauRice = "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)";
    private const string TnauRiceUrl = "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html";
    private const string TnauCotton = "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)";
    private const string TnauCottonUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html";
    private const string TnauMaize = "TNAU Agritech: Nutrient management, Maize (updated 29.05.2013)";
    private const string TnauMaizeUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html";
    private const string TnauGroundnut = "TNAU Agritech: Groundnut, rainfed (crop production advisory)";
    private const string TnauGroundnutUrl = "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html";
    private const string TnauBlackgram = "TNAU Agritech: Nutrient management, Blackgram (2013)";
    private const string TnauBlackgramUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_blackgram.html";
    private const string TnauGreengram = "TNAU Agritech: Nutrient management, Greengram (04.06.2013)";
    private const string TnauGreengramUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_greengram.html";
    private const string TnauSoybean = "TNAU Agritech: Nutrient management, Soybean";
    private const string TnauSoybeanUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_soybean.html";
    private const string TnauWheat = "TNAU Agritech: Nutrient management, Wheat";
    private const string TnauWheatUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_wheat.html";
    private const string TnauVeg = "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops";
    private const string TnauVegUrl = "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf";
    private const string TnauSpice = "TNAU Agritech Horticulture: Fertilizer schedule for spice crops";
    private const string TnauSpiceUrl = "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20SPICES.pdf";
    private const string TnauBanana = "TNAU Agritech Horticulture: Banana (application of fertilizers, spacing)";
    private const string TnauBananaUrl = "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html";
    private const string TnauCoconut = "TNAU Agritech Horticulture: Coconut manuring; spacing 7.5 x 7.5 m = 175 palms/ha (coconut spacing page)";
    private const string TnauCoconutUrl = "https://agritech.tnau.ac.in/horticulture/horti_pcrops_coconut_manuring.html";
    private const string TnauSoil = "TNAU Agritech: Soil related constraints and their management";
    private const string TnauSoilUrl = "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html";
    private const string TnauIrrRice = "TNAU Agritech: Irrigation management, Rice";
    private const string TnauIrrRiceUrl = "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cereals_rice.html";
    private const string TnauIrrCotton = "TNAU Agritech: Irrigation management, Cotton";
    private const string TnauIrrCottonUrl = "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html";
    private const string TnauIrrMaize = "TNAU Agritech: Irrigation management, Maize";
    private const string TnauIrrMaizeUrl = "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_maize.html";
    private const string TnauIrrBlackgram = "TNAU Agritech: Irrigation management, Blackgram / Greengram";
    private const string TnauIrrBlackgramUrl = "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_blackgram.html";
    private const string TnauIrrSoybean = "TNAU Agritech: Irrigation management, Soybean";
    private const string TnauIrrSoybeanUrl = "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_soybean.html";
    private const string TnauCritical = "TNAU Agritech: Critical stages for irrigation";
    private const string TnauCriticalUrl = "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_criticalstage.html";

    private const FertilizerWaterRegime Irr = FertilizerWaterRegime.Irrigated;
    private const FertilizerWaterRegime Rf = FertilizerWaterRegime.Rainfed;
    private const FertilizerWaterRegime AnyRegime = FertilizerWaterRegime.Any;
    private const FertilizerDayBasis Sow = FertilizerDayBasis.Sowing;
    private const FertilizerDayBasis Plant = FertilizerDayBasis.Planting;
    private const FertilizerDayBasis Named = FertilizerDayBasis.Calendar;

    private const string RiceNotes = "TNAU: phosphorus all as basal; nitrogen and potash in four equal splits (basal, active tillering, panicle initiation, heading). Do not cut the tillering and panicle initiation doses. Stage days are counted from sowing.";

    private static FertilizerCropNorm Norm(int id, string crop, string variant, FertilizerWaterRegime regime, string? soil,
        decimal n, decimal p, decimal k, int? duration, string? notes, string source, string url, int sort,
        FertilizerDoseBasis basis = FertilizerDoseBasis.PerHectare, int? plantsPerHa = null, string? spacing = null) => new()
    {
        Id = id, Crop = crop, Variant = variant, WaterRegime = regime, SoilType = soil, Basis = basis,
        N = n, P2O5 = p, K2O = k, PlantsPerHectare = plantsPerHa, Spacing = spacing, DurationDays = duration,
        Notes = notes, Source = source, SourceUrl = url, SortOrder = sort, IsActive = true
    };

    public static readonly FertilizerCropNorm[] Norms =
    {
        // ---- Paddy (TNAU blanket recommendation, rice main field)
        Norm(1, "Paddy", "Medium duration (about 135 days)", Irr, null, 150m, 50m, 50m, 135, RiceNotes, TnauRice, TnauRiceUrl, 10),
        Norm(2, "Paddy", "Long duration (about 150 days)", Irr, null, 150m, 50m, 50m, 150, RiceNotes, TnauRice, TnauRiceUrl, 11),
        Norm(3, "Paddy", "Short duration, Cauvery delta and Coimbatore (about 105 days)", Irr, null, 150m, 50m, 50m, 105, RiceNotes, TnauRice, TnauRiceUrl, 12),
        Norm(4, "Paddy", "Short duration, other areas (about 105 days)", Irr, null, 120m, 40m, 40m, 105, RiceNotes, TnauRice, TnauRiceUrl, 13),

        // ---- Cotton
        Norm(5, "Cotton", "Irrigated varieties (MCU 5, MCU 12, MCU 13, Suvin, SVPR 2)", Irr, null, 80m, 40m, 40m, null,
            "TNAU: half the N and K and all the P2O5 as basal, the rest of N and K at 40-45 days. If the basal dose could not be given, apply it on the 25th day. Place the fertilizer in a band two-thirds down from the top of the ridge and cover it.",
            TnauCotton, TnauCottonUrl, 20),
        Norm(6, "Cotton", "Irrigated hybrids (e.g. TCHB 213)", Irr, null, 120m, 60m, 60m, null,
            "TNAU: hybrid nitrogen in three equal splits (basal, 40-45 and 60-65 days); all the P2O5 basal; potash half basal and half at 40-45 days, as for varieties.",
            TnauCotton, TnauCottonUrl, 21),
        Norm(7, "Cotton", "Rainfed (SVPR 2, KC 2)", Rf, null, 40m, 20m, 40m, null,
            "TNAU gives no split for rainfed cotton: apply the whole dose at sowing when the soil has moisture. Spray 0.5 % urea + 1 % potassium chloride on the 45th and 65th day if there is moisture.",
            TnauCotton, TnauCottonUrl, 22),

        // ---- Maize
        Norm(8, "Maize", "Irrigated varieties", Irr, null, 135m, 62.5m, 50m, null,
            "TNAU: a quarter of the N and all the P2O5 and K2O at sowing in a furrow 6 cm deep on the side of the ridge (cover with 4 cm of soil); half the N on the 25th day; the last quarter on the 45th day. With Azospirillum seed and soil treatment the N can be cut by 25 %.",
            TnauMaize, TnauMaizeUrl, 30),
        Norm(9, "Maize", "Irrigated hybrids", Irr, null, 150m, 75m, 75m, null,
            "TNAU: hybrid maize 150:75:75, applied like the varieties (a quarter of N basal, half on the 25th day, a quarter on the 45th day).",
            TnauMaize, TnauMaizeUrl, 31),
        Norm(10, "Maize", "Rainfed", Rf, "Black Cotton Soil", 40m, 20m, 0m, null,
            "TNAU: rainfed maize on Vertisols (black soils) 40:20:0; half the N and all the P2O5 with enriched FYM at sowing, the other half of N at tasselling.",
            TnauMaize, TnauMaizeUrl, 32),
        Norm(11, "Maize", "Rainfed", Rf, "Red Soil", 60m, 30m, 30m, null,
            "TNAU: rainfed maize on Alfisols (red soils) 60:30:30; half the N and all the P2O5 and K2O with enriched FYM at sowing, the other half of N at tasselling.",
            TnauMaize, TnauMaizeUrl, 33),

        // ---- Oilseeds and pulses
        Norm(12, "Groundnut", "Rainfed", Rf, null, 10m, 10m, 45m, null,
            "TNAU rainfed groundnut blanket recommendation (no soil test). Gypsum and micronutrients are separate (see the nutrient tips).",
            TnauGroundnut, TnauGroundnutUrl, 40),
        Norm(13, "Black gram", "Rainfed", Rf, null, 12.5m, 25m, 12.5m, null,
            "TNAU: all as basal before sowing, plus 10 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.",
            TnauBlackgram, TnauBlackgramUrl, 50),
        Norm(14, "Black gram", "Irrigated", Irr, null, 25m, 50m, 25m, null,
            "TNAU: all as basal before sowing, plus 20 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.",
            TnauBlackgram, TnauBlackgramUrl, 51),
        Norm(15, "Green gram", "Rainfed", Rf, null, 12.5m, 25m, 12.5m, null,
            "TNAU: all as basal before sowing, plus 10 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.",
            TnauGreengram, TnauGreengramUrl, 52),
        Norm(16, "Green gram", "Irrigated", Irr, null, 25m, 50m, 25m, null,
            "TNAU: all as basal before sowing, plus 20 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.",
            TnauGreengram, TnauGreengramUrl, 53),
        Norm(17, "Soybean", "Irrigated", Irr, null, 20m, 80m, 40m, null,
            "TNAU: all as basal, plus 40 kg sulphur per hectare as gypsum (220 kg gypsum/ha).",
            TnauSoybean, TnauSoybeanUrl, 54),
        Norm(18, "Soybean", "Rainfed", Rf, null, 20m, 40m, 20m, null,
            "TNAU: all as basal when there is enough soil moisture, plus 20 kg sulphur per hectare.",
            TnauSoybean, TnauSoybeanUrl, 55),

        // ---- Wheat
        Norm(19, "Wheat", "Irrigated", Irr, null, 80m, 40m, 40m, null,
            "TNAU: half the N and all the P2O5 and K2O basal in the sowing line; the other half of N at crown root initiation (15-20 days).",
            TnauWheat, TnauWheatUrl, 60),

        // ---- Vegetables and spices (TNAU horticulture fertilizer schedules)
        Norm(20, "Chilli", "Irrigated (transplanted)", Irr, null, 120m, 60m, 30m, null,
            "TNAU: 30:60:30 basal, then 30 kg N each on the 30th, 60th and 90th day after planting. Potash as potassium sulphate improves pod quality.",
            TnauVeg, TnauVegUrl, 70),
        Norm(21, "Tomato", "Varieties (transplanted)", Irr, null, 150m, 100m, 50m, null,
            "TNAU: 75:100:50 basal (with borax 10 kg and zinc sulphate 50 kg per hectare), then 75 kg N on the 30th day at earthing up.",
            TnauVeg, TnauVegUrl, 71),
        Norm(22, "Onion", "Small onion (aggregatum)", Irr, null, 60m, 60m, 30m, null,
            "TNAU: 30:60:30 basal, then 30 kg N on the 30th day.",
            TnauVeg, TnauVegUrl, 72),
        Norm(23, "Onion", "Bellary onion", Irr, null, 100m, 150m, 75m, null,
            "TNAU: 50:150:75 basal (and zinc sulphate 50 kg/ha at the last ploughing), then 50 kg N on the 30th day after planting.",
            TnauVeg, TnauVegUrl, 73),
        Norm(24, "Turmeric", "Irrigated", Irr, null, 150m, 60m, 108m, null,
            "TNAU: 25:60:18 basal, then 25 kg N and 18 kg K2O on the 30th, 60th, 90th, 120th and 150th day after planting (total 150:60:108).",
            TnauSpice, TnauSpiceUrl, 74),

        // ---- Fruit and plantation crops: grams per plant / palm
        Norm(25, "Banana", "Garden land, varieties other than Nendran (e.g. Robusta)", Irr, null, 110m, 35m, 330m, null,
            "TNAU: grams per plant; N and K in three splits (3rd, 5th and 7th month), P in the 3rd month; N as neem coated urea. Tissue culture banana: 50 % extra fertilizer at the 2nd, 4th, 6th and 8th month. Plants are counted at 1.8 x 1.8 m (3,080 per hectare); enter your own count for other spacing.",
            TnauBanana, TnauBananaUrl, 80, FertilizerDoseBasis.PerPlant, 3080, "1.8 x 1.8 m"),
        Norm(26, "Banana", "Garden land, Nendran", Irr, null, 150m, 90m, 300m, null,
            "TNAU: grams per plant; N and K in three splits (3rd, 5th and 7th month), P in the 3rd month; N as neem coated urea. Plants are counted at 1.8 x 1.8 m (3,080 per hectare); enter your own count for other spacing.",
            TnauBanana, TnauBananaUrl, 81, FertilizerDoseBasis.PerPlant, 3080, "1.8 x 1.8 m"),
        Norm(27, "Coconut", "Palms from the 5th year", AnyRegime, null, 560m, 320m, 1200m, null,
            "TNAU: grams per palm per year in two equal splits (June - July and December - January), with 50 kg FYM or compost per palm. Apply in a 1.8 m circle around the palm, mix in and irrigate. Younger palms get a quarter to three quarters of this dose. Palms are counted at 7.5 x 7.5 m (175 per hectare).",
            TnauCoconut, TnauCoconutUrl, 90, FertilizerDoseBasis.PerPlant, 175, "7.5 x 7.5 m")
    };

    private static int _splitId;

    private static FertilizerNormSplit S(int normId, int seq, string stage, int? from, int? to, FertilizerDayBasis basis, decimal n, decimal p, decimal k) => new()
    {
        Id = ++_splitId, NormId = normId, Seq = seq, Stage = stage, DayFrom = from, DayTo = to, DayBasis = basis, N = n, P2O5 = p, K2O = k
    };

    private static IEnumerable<FertilizerNormSplit> Rice(int normId, decimal n, decimal p, decimal k, (int, int) at, (int, int) pi, (int, int) h) => new[]
    {
        S(normId, 1, "Basal (at transplanting)", null, null, Plant, n / 4, p, k / 4),
        S(normId, 2, "Active tillering", at.Item1, at.Item2, Sow, n / 4, 0, k / 4),
        S(normId, 3, "Panicle initiation", pi.Item1, pi.Item2, Sow, n / 4, 0, k / 4),
        S(normId, 4, "Heading", h.Item1, h.Item2, Sow, n / 4, 0, k / 4)
    };

    private static FertilizerNormSplit[] BuildSplits()
    {
        _splitId = 0;
        var list = new List<FertilizerNormSplit>();
        list.AddRange(Rice(1, 150m, 50m, 50m, (50, 55), (70, 75), (100, 105)));
        list.AddRange(Rice(2, 150m, 50m, 50m, (55, 60), (85, 90), (115, 120)));
        list.AddRange(Rice(3, 150m, 50m, 50m, (35, 40), (45, 50), (70, 75)));
        list.AddRange(Rice(4, 120m, 40m, 40m, (35, 40), (45, 50), (70, 75)));

        list.Add(S(5, 1, "Basal (at sowing)", null, null, Sow, 40m, 40m, 20m));
        list.Add(S(5, 2, "Top dressing", 40, 45, Sow, 40m, 0m, 20m));
        list.Add(S(6, 1, "Basal (at sowing)", null, null, Sow, 40m, 60m, 30m));
        list.Add(S(6, 2, "1st top dressing", 40, 45, Sow, 40m, 0m, 30m));
        list.Add(S(6, 3, "2nd top dressing", 60, 65, Sow, 40m, 0m, 0m));
        list.Add(S(7, 1, "Basal (at sowing)", null, null, Sow, 40m, 20m, 40m));

        list.Add(S(8, 1, "Basal (at sowing)", null, null, Sow, 33.75m, 62.5m, 50m));
        list.Add(S(8, 2, "1st top dressing", 25, 25, Sow, 67.5m, 0m, 0m));
        list.Add(S(8, 3, "2nd top dressing", 45, 45, Sow, 33.75m, 0m, 0m));
        list.Add(S(9, 1, "Basal (at sowing)", null, null, Sow, 37.5m, 75m, 75m));
        list.Add(S(9, 2, "1st top dressing", 25, 25, Sow, 75m, 0m, 0m));
        list.Add(S(9, 3, "2nd top dressing", 45, 45, Sow, 37.5m, 0m, 0m));
        list.Add(S(10, 1, "Basal (at sowing)", null, null, Sow, 20m, 20m, 0m));
        list.Add(S(10, 2, "At tasselling", null, null, Named, 20m, 0m, 0m));
        list.Add(S(11, 1, "Basal (at sowing)", null, null, Sow, 30m, 30m, 30m));
        list.Add(S(11, 2, "At tasselling", null, null, Named, 30m, 0m, 0m));

        list.Add(S(12, 1, "Basal (at sowing)", null, null, Sow, 10m, 10m, 45m));
        list.Add(S(13, 1, "Basal (before sowing)", null, null, Sow, 12.5m, 25m, 12.5m));
        list.Add(S(14, 1, "Basal (before sowing)", null, null, Sow, 25m, 50m, 25m));
        list.Add(S(15, 1, "Basal (before sowing)", null, null, Sow, 12.5m, 25m, 12.5m));
        list.Add(S(16, 1, "Basal (before sowing)", null, null, Sow, 25m, 50m, 25m));
        list.Add(S(17, 1, "Basal (at sowing)", null, null, Sow, 20m, 80m, 40m));
        list.Add(S(18, 1, "Basal (at sowing)", null, null, Sow, 20m, 40m, 20m));

        list.Add(S(19, 1, "Basal (at sowing)", null, null, Sow, 40m, 40m, 40m));
        list.Add(S(19, 2, "Crown root initiation", 15, 20, Sow, 40m, 0m, 0m));

        list.Add(S(20, 1, "Basal (at planting)", null, null, Plant, 30m, 60m, 30m));
        list.Add(S(20, 2, "1st top dressing", 30, 30, Plant, 30m, 0m, 0m));
        list.Add(S(20, 3, "2nd top dressing", 60, 60, Plant, 30m, 0m, 0m));
        list.Add(S(20, 4, "3rd top dressing", 90, 90, Plant, 30m, 0m, 0m));
        list.Add(S(21, 1, "Basal (at planting)", null, null, Plant, 75m, 100m, 50m));
        list.Add(S(21, 2, "Top dressing (earthing up)", 30, 30, Plant, 75m, 0m, 0m));
        list.Add(S(22, 1, "Basal (at planting)", null, null, Plant, 30m, 60m, 30m));
        list.Add(S(22, 2, "Top dressing", 30, 30, Plant, 30m, 0m, 0m));
        list.Add(S(23, 1, "Basal (at planting)", null, null, Plant, 50m, 150m, 75m));
        list.Add(S(23, 2, "Top dressing", 30, 30, Plant, 50m, 0m, 0m));
        list.Add(S(24, 1, "Basal (at planting)", null, null, Plant, 25m, 60m, 18m));
        list.Add(S(24, 2, "1st top dressing", 30, 30, Plant, 25m, 0m, 18m));
        list.Add(S(24, 3, "2nd top dressing", 60, 60, Plant, 25m, 0m, 18m));
        list.Add(S(24, 4, "3rd top dressing", 90, 90, Plant, 25m, 0m, 18m));
        list.Add(S(24, 5, "4th top dressing", 120, 120, Plant, 25m, 0m, 18m));
        list.Add(S(24, 6, "5th top dressing", 150, 150, Plant, 25m, 0m, 18m));

        list.Add(S(25, 1, "3rd month after planting", null, null, Named, 36.67m, 35m, 110m));
        list.Add(S(25, 2, "5th month after planting", null, null, Named, 36.67m, 0m, 110m));
        list.Add(S(25, 3, "7th month after planting", null, null, Named, 36.66m, 0m, 110m));
        list.Add(S(26, 1, "3rd month after planting", null, null, Named, 50m, 90m, 100m));
        list.Add(S(26, 2, "5th month after planting", null, null, Named, 50m, 0m, 100m));
        list.Add(S(26, 3, "7th month after planting", null, null, Named, 50m, 0m, 100m));

        list.Add(S(27, 1, "June - July", null, null, Named, 280m, 160m, 600m));
        list.Add(S(27, 2, "December - January", null, null, Named, 280m, 160m, 600m));
        return list.ToArray();
    }

    public static readonly FertilizerNormSplit[] Splits = BuildSplits();

    private static int _tipId;

    private static CropSoilTip T(string? crop, string? soil, FertilizerWaterRegime regime, string category, string text, string source, string url) => new()
    {
        Id = ++_tipId, Crop = crop, SoilType = soil, WaterRegime = regime, Category = category, Text = text,
        Source = source, SourceUrl = url, SortOrder = _tipId, IsActive = true
    };

    private static CropSoilTip[] BuildTips()
    {
        _tipId = 0;
        const string black = "Black Cotton Soil", red = "Red Soil", sandy = "Sandy / Sandy Loam", clay = "Clay Loam",
            laterite = "Laterite Soil", saline = "Coastal Saline";
        var list = new List<CropSoilTip>
        {
            // ---- soil types (every crop)
            T(null, black, AnyRegime, "Soil", "Black soil lets water in slowly. Plough deep with a mould board or disc plough in summer so water soaks in and drains better.", TnauSoil, TnauSoilUrl),
            T(null, black, AnyRegime, "Soil", "Add FYM, composted coir pith or pressmud at 25 t/ha (about 10 t/acre) every year to improve drainage and soil structure.", TnauSoil, TnauSoilUrl),
            T(null, black, AnyRegime, "Water", "Do not let water stand in the field. Where it collects after rain or irrigation, cut open drains (TNAU: about 60 cm deep and 45 cm wide for waterlogged fields).", TnauIrrRice, TnauIrrRiceUrl),
            T(null, clay, AnyRegime, "Soil", "Heavy clay soil: plough deep with a mould board or disc plough in summer to help water soak in and drain.", TnauSoil, TnauSoilUrl),
            T(null, clay, AnyRegime, "Water", "Do not let water stand in the field. Where it collects, cut open drains (TNAU: about 60 cm deep and 45 cm wide for waterlogged fields).", TnauIrrRice, TnauIrrRiceUrl),
            T(null, red, AnyRegime, "Soil", "Red soil loses water fast. Add tank silt or black soil at 25 t/ha (about 10 t/acre) a year, together with FYM, composted coir pith or pressmud at 25 t/ha.", TnauSoil, TnauSoilUrl),
            T(null, red, AnyRegime, "Soil", "If a hard layer below 15 cm stops the roots, chisel plough at 0.5 m spacing in both directions once in three years and add FYM or composted coir pith at 12.5 t/ha (about 5 t/acre).", TnauSoil, TnauSoilUrl),
            T(null, red, AnyRegime, "Soil", "If the surface forms a hard crust after rain, break it by harrowing; lime or gypsum at 2 t/ha with FYM at 12.5 t/ha prevents it.", TnauSoil, TnauSoilUrl),
            T(null, sandy, AnyRegime, "Soil", "Sandy soil loses water and fertilizer fast. Roll it with a 400 kg stone roller (or an oil drum filled with stones) 8 times at the right moisture, once in three years.", TnauSoil, TnauSoilUrl),
            T(null, sandy, AnyRegime, "Soil", "Coastal sandy soil: add tank silt to make it more productive.", TnauSoil, TnauSoilUrl),
            T(null, laterite, AnyRegime, "Soil", "Laterite soils (for example in Pudukkottai and Kanyakumari) are often acidic. Below pH 6, apply lime as per the lime requirement test of your soil.", TnauSoil, TnauSoilUrl),
            T(null, laterite, AnyRegime, "Soil", "Where iron or aluminium toxicity shows up (common in these soils when flooded), lime as per the lime requirement and organic manure reduce it.", TnauSoil, TnauSoilUrl),
            T(null, saline, AnyRegime, "Soil", "Saline soil (EC above 4 dS/m on the soil report): make field and main drains 60 cm deep and 45 cm wide and wash the salts out with water.", TnauSoil, TnauSoilUrl),
            T(null, saline, AnyRegime, "Soil", "Apply FYM at 5 t/ha (about 2 t/acre) 10-15 days before transplanting or sowing to ease the salt problem.", TnauSoil, TnauSoilUrl),

            // ---- Cotton
            T("Cotton", null, AnyRegime, "Soil", "Spread 12.5 t/ha (about 5 t/acre) of FYM or compost, or 2.5 t/ha of vermicompost, on the field before ploughing.", TnauCotton, TnauCottonUrl),
            T("Cotton", null, AnyRegime, "Nutrient", "Zinc-deficient soil: zinc sulphate 50 kg/ha (about 20 kg/acre) as basal, or a 0.5 % zinc sulphate spray three times at 45, 60 and 75 days.", TnauCotton, TnauCottonUrl),
            T("Cotton", null, AnyRegime, "Nutrient", "Where leaves turn red or magnesium is short: magnesium sulphate 20 kg/ha (about 8 kg/acre) as basal; a 2 % magnesium sulphate + 1 % urea spray at boll formation if needed.", TnauCotton, TnauCottonUrl),
            T("Cotton", null, AnyRegime, "Nutrient", "Seed treatment and seed-line application of Azospirillum and Phosphobacteria (or Azophos) saves about 25 % of the nitrogen.", TnauCotton, TnauCottonUrl),
            T("Cotton", null, Irr, "Water", "Irrigate right after sowing and give a life irrigation on the 5th day. The flowering phase (about 45-100 days) needs plenty of water; give less water at maturity and stop after about 150 days.", TnauIrrCotton, TnauIrrCottonUrl),
            T("Cotton", black, Irr, "Water", "Heavy (black) soil schedule: about the 20th and 40th day, then in flowering about the 55th, 70th, 85th and 100th day.", TnauIrrCotton, TnauIrrCottonUrl),
            T("Cotton", clay, Irr, "Water", "Heavy soil schedule: about the 20th and 40th day, then in flowering about the 55th, 70th, 85th and 100th day.", TnauIrrCotton, TnauIrrCottonUrl),
            T("Cotton", sandy, Irr, "Water", "Light soil schedule: about the 20th and 35th day, then in flowering about the 48th, 60th, 72nd and 84th day.", TnauIrrCotton, TnauIrrCottonUrl),
            T("Cotton", red, Irr, "Water", "Light soil schedule: about the 20th and 35th day, then in flowering about the 48th, 60th, 72nd and 84th day.", TnauIrrCotton, TnauIrrCottonUrl),
            T("Cotton", black, Irr, "Water", "Skip-furrow or alternate-furrow irrigation suits heavy soils and saves water (skip furrow saves about half).", TnauIrrCotton, TnauIrrCottonUrl),
            T("Cotton", clay, Irr, "Water", "Skip-furrow or alternate-furrow irrigation suits clay and loam soils and saves water (skip furrow saves about half).", TnauIrrCotton, TnauIrrCottonUrl),

            // ---- Paddy
            T("Paddy", null, AnyRegime, "Nutrient", "Zinc sulphate 25 kg/ha (about 10 kg/acre) mixed with 50 kg of dry sand just before transplanting; 12.5 kg/ha is enough when green manure (6.25 t/ha) or enriched FYM is applied.", TnauRice, TnauRiceUrl),
            T("Paddy", null, AnyRegime, "Nutrient", "Gypsum 500 kg/ha (about 200 kg/acre) at the last ploughing gives calcium and sulphur.", TnauRice, TnauRiceUrl),
            T("Paddy", saline, AnyRegime, "Nutrient", "Saline or sodic soil: zinc sulphate 37.5 kg/ha (about 15 kg/acre).", TnauRice, TnauRiceUrl),
            T("Paddy", null, AnyRegime, "Water", "Keep 2 cm of water for 7 days after transplanting. Then irrigate when the ponded water has just gone (clay soil) or one day later (loamy soil) in summer; in winter 1-2 days (clay) or 3 days (loamy) after it has gone.", TnauIrrRice, TnauIrrRiceUrl),
            T("Paddy", null, AnyRegime, "Water", "Panicle initiation, booting, heading and flowering are critical: do not delay irrigation then. Stop irrigating about 15 days before harvest.", TnauIrrRice, TnauIrrRiceUrl),

            // ---- Maize
            T("Maize", null, AnyRegime, "Soil", "Spread 12.5 t/ha (about 5 t/acre) of FYM, compost or composted coir pith with Azospirillum and Phosphobacteria (2 kg/ha each) or Azophos (4 kg/ha) before ploughing.", TnauMaize, TnauMaizeUrl),
            T("Maize", null, AnyRegime, "Nutrient", "Zinc-deficient soil: zinc sulphate 37.5 kg/ha (about 15 kg/acre), or the micronutrient mixture of the Department of Agriculture 12.5 kg/ha mixed with sand.", TnauMaize, TnauMaizeUrl),
            T("Maize", null, Irr, "Water", "45-65 days after sowing (flowering) is the most critical time for water: the yield drops a lot if the crop is short of water then.", TnauIrrMaize, TnauIrrMaizeUrl),
            T("Maize", black, Irr, "Water", "Heavy soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 25th, 36th, 48th, 60th, 72nd and 85th day.", TnauIrrMaize, TnauIrrMaizeUrl),
            T("Maize", clay, Irr, "Water", "Heavy soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 25th, 36th, 48th, 60th, 72nd and 85th day.", TnauIrrMaize, TnauIrrMaizeUrl),
            T("Maize", sandy, Irr, "Water", "Light soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 22nd, 32nd, 40th, 50th, 60th, 72nd, 85th and 95th day.", TnauIrrMaize, TnauIrrMaizeUrl),
            T("Maize", red, Irr, "Water", "Light soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 22nd, 32nd, 40th, 50th, 60th, 72nd, 85th and 95th day.", TnauIrrMaize, TnauIrrMaizeUrl),

            // ---- Groundnut
            T("Groundnut", null, AnyRegime, "Nutrient", "Gypsum 400 kg/ha (about 160 kg/acre) beside the plants between the 40th and 70th day (depending on soil moisture), hoed in and earthed up: better pod filling. Do not use gypsum on calcareous soil.", TnauGroundnut, TnauGroundnutUrl),
            T("Groundnut", null, AnyRegime, "Nutrient", "Zinc sulphate 25 kg/ha (about 10 kg/acre) as basal when soil zinc is below 1.3 ppm.", TnauGroundnut, TnauGroundnutUrl),
            T("Groundnut", null, AnyRegime, "Nutrient", "Micronutrient mixture of the Department of Agriculture 12.5 kg/ha, mixed with sand to 50 kg and spread on the surface right after sowing. Do not mix it into the soil.", TnauGroundnut, TnauGroundnutUrl),
            T("Groundnut", null, AnyRegime, "Soil", "If the surface crusts, lime at 2 t/ha with FYM or composted coir pith at 12.5 t/ha helps the seedlings come up.", TnauGroundnut, TnauGroundnutUrl),

            // ---- Pulses
            T("Black gram", null, AnyRegime, "Soil", "Lime is recommended for pulses when the soil pH is below 6.0.", TnauBlackgram, TnauBlackgramUrl),
            T("Black gram", null, Irr, "Nutrient", "Irrigated crop: zinc sulphate 25 kg/ha (about 10 kg/acre).", TnauBlackgram, TnauBlackgramUrl),
            T("Black gram", null, AnyRegime, "Nutrient", "Spray 1 % urea on the 30th and 45th day after sowing for a better yield; a 2 % DAP spray at flowering and 15 days later also helps.", TnauBlackgram, TnauBlackgramUrl),
            T("Black gram", null, Irr, "Water", "Irrigate right after sowing, a life irrigation on the 3rd day, then every 7-10 days. Flowering and pod formation must not lack water; never let water stand.", TnauIrrBlackgram, TnauIrrBlackgramUrl),
            T("Green gram", null, AnyRegime, "Soil", "Lime is recommended for pulses when the soil pH is below 6.0.", TnauBlackgram, TnauBlackgramUrl),
            T("Green gram", null, Irr, "Nutrient", "Irrigated crop: zinc sulphate 25 kg/ha (about 10 kg/acre).", TnauGreengram, TnauGreengramUrl),
            T("Green gram", null, AnyRegime, "Nutrient", "Spray 1 % urea on the 30th and 45th day after sowing for a better yield.", TnauGreengram, TnauGreengramUrl),
            T("Green gram", null, Irr, "Water", "Irrigate right after sowing, a life irrigation on the 3rd day, then every 7-10 days. Flowering and pod formation must not lack water; never let water stand.", TnauIrrBlackgram, TnauIrrBlackgramUrl),
            T("Soybean", null, Irr, "Nutrient", "Irrigated crop: zinc sulphate 25 kg/ha (about 10 kg/acre).", TnauSoybean, TnauSoybeanUrl),
            T("Soybean", null, Irr, "Water", "Irrigate after sowing, a life irrigation on the 3rd day, then every 7-10 days in summer or 10-15 days in winter. Soybean is harmed by standing water; keep it free of water stress from flowering to maturity.", TnauIrrSoybean, TnauIrrSoybeanUrl),

            // ---- Wheat
            T("Wheat", null, AnyRegime, "Soil", "Spread 12.5 t/ha (about 5 t/acre) of FYM or compost on the field before ploughing.", TnauWheat, TnauWheatUrl),
            T("Wheat", null, Irr, "Water", "Crown root initiation (about 15-20 days) is the most critical stage for water; tillering, jointing, booting, flowering, milk and dough stages also need water.", TnauCritical, TnauCriticalUrl),

            // ---- Vegetables and spices
            T("Chilli", null, AnyRegime, "Soil", "Apply FYM at 25 t/ha (about 10 t/acre) before planting.", TnauVeg, TnauVegUrl),
            T("Chilli", null, AnyRegime, "Nutrient", "Giving the potash as potassium sulphate improves the quality of the pods.", TnauVeg, TnauVegUrl),
            T("Tomato", null, AnyRegime, "Soil", "Apply FYM at 25 t/ha (about 10 t/acre) before planting.", TnauVeg, TnauVegUrl),
            T("Tomato", null, AnyRegime, "Nutrient", "Basal borax 10 kg/ha (about 4 kg/acre) and zinc sulphate 50 kg/ha (about 20 kg/acre).", TnauVeg, TnauVegUrl),
            T("Onion", null, AnyRegime, "Soil", "Apply FYM at 25 t/ha (about 10 t/acre) with Azospirillum and Phosphobacteria 2 kg/ha each before planting.", TnauVeg, TnauVegUrl),
            T("Onion", null, AnyRegime, "Nutrient", "Bellary onion: zinc sulphate 50 kg/ha (about 20 kg/acre) at the last ploughing.", TnauVeg, TnauVegUrl),
            T("Turmeric", null, AnyRegime, "Soil", "Mix in FYM 10 t/ha (about 4 t/acre) and neem or groundnut cake 200 kg/ha at planting; apply Azospirillum and Phosphobacteria 10 kg/ha each one month after planting.", TnauSpice, TnauSpiceUrl),
            T("Turmeric", null, AnyRegime, "Nutrient", "Basal ferrous sulphate 30 kg/ha and zinc sulphate 15 kg/ha. For boron, iron and zinc shortage at rhizome development, TNAU advises a micronutrient spray twice at 25-day intervals.", TnauSpice, TnauSpiceUrl),

            // ---- Banana and coconut
            T("Banana", null, AnyRegime, "Water", "Irrigate right after planting and again after 4 days, then once a week (garden land) or every 10-15 days (wetland). Irrigate well after every manure application.", TnauBanana, TnauBananaUrl),
            T("Banana", null, AnyRegime, "Water", "With drip: 5-10 litres per plant a day up to the 4th month, 10-15 litres from the 5th month to shooting, and 15 litres from shooting until 15 days before harvest.", TnauBanana, TnauBananaUrl),
            T("Banana", null, AnyRegime, "Nutrient", "Spray zinc sulphate 0.5 %, ferrous sulphate 0.2 %, copper sulphate 0.2 % and boric acid 0.1 % at 3, 5 and 7 months after planting for yield and quality.", TnauBanana, TnauBananaUrl),
            T("Coconut", null, AnyRegime, "Soil", "Give 50 kg of FYM, compost or green manure per palm a year; sunnhemp sown in the basin (50 g per palm) and ploughed in before flowering also works.", TnauCoconut, TnauCoconutUrl),
            T("Coconut", null, AnyRegime, "Nutrient", "Biofertilizers: 50 g Azospirillum, 50 g Phosphobacteria (or 100 g Azophos) and 50 g VAM per palm every 6 months, mixed with compost; never mixed with chemical fertilizer.", TnauCoconut, TnauCoconutUrl)
        };
        return list.ToArray();
    }

    public static readonly CropSoilTip[] Tips = BuildTips();
}
