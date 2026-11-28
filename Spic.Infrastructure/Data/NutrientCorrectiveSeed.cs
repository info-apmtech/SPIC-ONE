using SPIC.Core.Entities;

namespace Spic.Infrastructure.Data;

/// <summary>
/// Seed of the leaf-check correctives (farmer portal Task 6, migration V40_LeafCheck): what to do for each nutrient
/// shortage. Crop rows were read on the TNAU Agritech page named in the row (fetched 2026-11-28 through the Internet
/// Archive, web.archive.org/web/2024id_/..., because agritech.tnau.ac.in refused direct connections, as in Task 4).
/// General rows (Crop null) are used when a crop has no row of its own for that nutrient. Every dose is a value read on
/// the TNAU page its row links (fix round 1: the general urea, magnesium and iron sprays use TNAU's 1 % urea (paddy),
/// 2 % MgSO4 + 1 % urea (cotton) and 1 % FeSO4 (groundnut) instead of the brief's example strengths). A row that is not
/// a TNAU value must have SourceUrl null and Source <see cref="GeneralGuidance"/> (the harness checks it); the one
/// in-app row (top-dress per Fertilizer advice) has no link either. An agronomist should sign the seed off before
/// go-live (docs/farmer-portal.md, Task 6). Spray strengths also give grams per litre, which is how farmers mix them
/// (1 % = 10 g per litre).
/// </summary>
public static class NutrientCorrectiveSeed
{
    private const string PaddyExpert = "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures";
    private const string PaddyExpertUrl = "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html";
    private const string Rice = "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)";
    private const string RiceUrl = "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html";
    private const string Cotton = "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)";
    private const string CottonUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html";
    private const string Maize = "TNAU Agritech: Nutrient management, Maize (micronutrients)";
    private const string MaizeUrl = "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html";
    private const string Groundnut = "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)";
    private const string GroundnutUrl = "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html";
    private const string Banana = "TNAU Agritech Horticulture: Banana (micronutrient spray)";
    private const string BananaUrl = "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html";

    /// <summary>The Source of a row whose value is not from a TNAU page (then SourceUrl is null).</summary>
    public const string GeneralGuidance = "General guidance (not a TNAU value); ask your SPIC officer";

    private static NutrientCorrective C(int id, string nutrient, string? crop, string product, string method, string dose,
        string? note, string source, string? url, int sort) => new()
    {
        Id = id, Nutrient = nutrient, Crop = crop, Product = product, Method = method, Dose = dose, Note = note,
        Source = source, SourceUrl = url, SortOrder = sort, IsActive = true
    };

    public static readonly NutrientCorrective[] Rows =
    {
        // ---- General rows (any crop without its own row) ----
        C(1, "N", null, "Urea (46 % N)", "Foliar", "1 % spray: 10 g urea per litre of water",
            "Repeat every week until the new leaves are green, then top-dress urea as in your fertilizer advice.",
            PaddyExpert, PaddyExpertUrl, 1),
        C(2, "N", null, "Urea (46 % N)", "Soil", "Top-dress the next urea split as in your Fertilizer advice",
            "Apply on moist soil, not just before heavy rain. Fertilizer advice gives the dose for your crop and soil.",
            "SPIC ONE Fertilizer advice (TNAU crop norms)", null, 2),
        C(3, "P", null, "DAP (18-46-0)", "Foliar", "2 % spray: 20 g DAP per litre of water (soak overnight, spray the clear liquid)",
            "TNAU sprays 2 % DAP with 1 % potash (KCl) to improve yield.", Cotton, CottonUrl, 1),
        C(4, "P", null, "SSP or DAP", "Soil", "As basal for the next crop, as per your soil test (Fertilizer advice)",
            "Phosphorus works best at sowing or planting; in a standing crop use the spray.", Rice, RiceUrl, 2),
        C(5, "K", null, "Muriate of potash (KCl, 60 % K2O)", "Foliar", "1 % spray: 10 g per litre of water",
            null, PaddyExpert, PaddyExpertUrl, 1),
        C(6, "K", null, "Muriate of potash (KCl, 60 % K2O)", "Soil", "25 % more potash than the recommended dose",
            "In the next top-dressing and the next crop.", PaddyExpert, PaddyExpertUrl, 2),
        C(7, "Mg", null, "Magnesium sulphate + urea", "Foliar", "2 % magnesium sulphate with 1 % urea: 20 g and 10 g per litre of water",
            "TNAU sprays it at boll formation in cotton.", Cotton, CottonUrl, 1),
        C(8, "Mg", null, "Magnesium sulphate", "Soil", "20 kg per hectare (about 8 kg per acre) as basal",
            "In magnesium-deficient areas.", Cotton, CottonUrl, 2),
        C(9, "Zn", null, "Zinc sulphate", "Soil", "25 kg per hectare (about 10 kg per acre), mixed with 50 kg dry sand",
            "Before sowing or transplanting.", Rice, RiceUrl, 1),
        C(10, "Zn", null, "Zinc sulphate", "Foliar", "0.5 % spray: 5 g per litre of water",
            "Repeat every 7-15 days until the symptoms go.", Rice, RiceUrl, 2),
        C(11, "Fe", null, "Ferrous sulphate", "Foliar", "1 % spray: 10 g per litre of water",
            "TNAU sprays it three times, on the 30th, 40th and 50th day after sowing (groundnut).", Groundnut, GroundnutUrl, 1),
        C(12, "Fe", null, "Ferrous sulphate", "Soil", "50 kg per hectare with 12.5 t farmyard manure",
            "When the soil is short of iron.", Rice, RiceUrl, 2),
        C(13, "S", null, "Gypsum", "Soil", "500 kg per hectare (about 200 kg per acre) at the last ploughing",
            "Gypsum gives sulphur and calcium. SSP and ammonium sulphate also give sulphur.", Rice, RiceUrl, 1),
        C(14, "B", null, "Borax", "Soil", "10 kg per hectare (about 4 kg per acre)",
            "Mix with sand or gypsum for even spreading.", Groundnut, GroundnutUrl, 1),
        C(15, "Ca", null, "Gypsum", "Soil", "400 kg per hectare (about 160 kg per acre) beside the plants",
            "Not on calcareous soil. On acid soil, lime (as per a lime requirement test) gives calcium.", Groundnut, GroundnutUrl, 1),
        C(16, "Ca", null, "Calcium chloride", "Foliar", "A calcium chloride spray for quick correction of a severe shortage",
            "Ask your SPIC officer for the strength for your crop.", PaddyExpert, PaddyExpertUrl, 2),
        C(17, "Mn", null, "Manganese sulphate", "Foliar", "1-5 kg Mn per hectare in about 200 litres of water",
            "On high-pH soil use ammonium sulphate instead of urea.", PaddyExpert, PaddyExpertUrl, 1),

        // ---- Paddy ----
        C(20, "N", "Paddy", "Urea (46 % N)", "Foliar", "1 % spray: 10 g per litre of water, every week until the symptoms go",
            "Top-dress when the leaf colour chart reads below 4 (3 for White Ponni): 35 kg N per hectare in the dry season, 30 kg in the wet season.",
            PaddyExpert, PaddyExpertUrl, 1),
        C(21, "N", "Paddy", "Urea (46 % N)", "Soil", "25 % more nitrogen than the recommended dose",
            "Keep the field flooded; do not let water run off over the bunds just after applying.", PaddyExpert, PaddyExpertUrl, 2),
        C(22, "P", "Paddy", "SSP or DAP", "Soil", "15-30 kg P per hectare",
            "Phosphobacteria as seed coating or seedling dip also helps.", PaddyExpert, PaddyExpertUrl, 1),
        C(23, "K", "Paddy", "Muriate of potash (KCl)", "Foliar", "1 % spray: 10 g per litre of water", null, PaddyExpert, PaddyExpertUrl, 1),
        C(24, "K", "Paddy", "Muriate of potash (KCl)", "Soil", "25 % more potash than the recommended dose",
            "Spread the straw back on the field instead of burning it.", PaddyExpert, PaddyExpertUrl, 2),
        C(25, "Zn", "Paddy", "Zinc sulphate", "Soil", "25 kg per hectare (about 10 kg per acre) with 50 kg dry sand, just before transplanting",
            "12.5 kg per hectare is enough when green manure or enriched FYM is applied; 37.5 kg on saline or sodic soil.", Rice, RiceUrl, 1),
        C(26, "Zn", "Paddy", "Zinc sulphate", "Foliar", "0.5 % spray: 5 g per litre of water",
            "Every 7-10 days (short duration) or 15 days (medium and long duration) until the symptoms go.", Rice, RiceUrl, 2),
        C(27, "Fe", "Paddy", "Ferrous sulphate", "Foliar", "2-3 % spray: 20-30 g per litre of water, 2-3 sprays two weeks apart",
            null, PaddyExpert, PaddyExpertUrl, 1),
        C(28, "Fe", "Paddy", "Ferrous sulphate", "Soil", "50 kg per hectare with 12.5 t farmyard manure",
            "When the soil is short of iron.", Rice, RiceUrl, 2),
        C(29, "S", "Paddy", "Gypsum", "Soil", "500 kg per hectare (about 200 kg per acre) at the last ploughing",
            "Gives sulphur and calcium.", Rice, RiceUrl, 1),
        C(30, "Mg", "Paddy", "Magnesium chloride", "Foliar", "2 % spray: 20 g per litre of water",
            "Kieserite in the soil corrects it quickly too.", PaddyExpert, PaddyExpertUrl, 1),
        C(31, "Mn", "Paddy", "Manganese sulphate", "Foliar", "1-5 kg Mn per hectare in about 200 litres of water",
            "Use ammonium sulphate instead of urea.", PaddyExpert, PaddyExpertUrl, 1),
        C(32, "B", "Paddy", "Borax", "Soil", "0.5-3 kg B per hectare, before planting or as a spray during growth",
            "Avoid too much water draining through the field.", PaddyExpert, PaddyExpertUrl, 1),
        C(33, "Ca", "Paddy", "Gypsum or lime", "Soil", "Gypsum on high-pH (sodic) soil; lime on acid soil",
            "A calcium chloride spray corrects a severe shortage quickly.", PaddyExpert, PaddyExpertUrl, 1),

        // ---- Cotton ----
        C(40, "Zn", "Cotton", "Zinc sulphate", "Soil", "50 kg per hectare (about 20 kg per acre) as basal", null, Cotton, CottonUrl, 1),
        C(41, "Zn", "Cotton", "Zinc sulphate", "Foliar", "0.5 % spray: 5 g per litre, three times at 45, 60 and 75 days", null, Cotton, CottonUrl, 2),
        C(42, "Mg", "Cotton", "Magnesium sulphate + urea", "Foliar", "2 % magnesium sulphate with 1 % urea: 20 g and 10 g per litre",
            "At boll formation; also against leaf reddening.", Cotton, CottonUrl, 1),
        C(43, "Mg", "Cotton", "Magnesium sulphate", "Soil", "20 kg per hectare (about 8 kg per acre) as basal",
            "In magnesium-deficient areas, to prevent reddening.", Cotton, CottonUrl, 2),
        C(44, "N", "Cotton", "Urea + potash (KCl)", "Foliar", "0.5 % urea with 1 % KCl: 5 g and 10 g per litre, on the 45th and 65th day",
            "When the soil has moisture.", Cotton, CottonUrl, 1),
        C(45, "K", "Cotton", "Potash (KCl) + urea", "Foliar", "1 % KCl with 0.5 % urea: 10 g and 5 g per litre, on the 45th and 65th day",
            "When the soil has moisture.", Cotton, CottonUrl, 1),
        C(46, "P", "Cotton", "DAP + potash (KCl)", "Foliar", "2 % DAP with 1 % KCl: 20 g and 10 g per litre",
            "Improves kapas yield.", Cotton, CottonUrl, 1),

        // ---- Groundnut ----
        C(50, "Zn", "Groundnut", "Zinc sulphate", "Soil", "25 kg per hectare (about 10 kg per acre) as basal",
            "When the soil has less than 1.3 ppm zinc.", Groundnut, GroundnutUrl, 1),
        C(51, "Zn", "Groundnut", "Zinc sulphate", "Foliar", "0.5 % spray: 5 g per litre of water", "For the standing crop.", Groundnut, GroundnutUrl, 2),
        C(52, "Fe", "Groundnut", "Ferrous sulphate", "Foliar", "1 % spray: 10 g per litre, on the 30th, 40th and 50th day", null, Groundnut, GroundnutUrl, 1),
        C(53, "B", "Groundnut", "Borax + gypsum", "Soil", "Borax 10 kg per hectare with gypsum, on the 45th day", null, Groundnut, GroundnutUrl, 1),
        C(54, "Ca", "Groundnut", "Gypsum", "Soil", "400 kg per hectare (about 160 kg per acre) beside the plants on the 40th-45th day",
            "Hoe it in and earth up. Not on calcareous soil.", Groundnut, GroundnutUrl, 1),
        C(55, "S", "Groundnut", "Gypsum", "Soil", "400 kg per hectare (about 160 kg per acre) beside the plants on the 40th-45th day",
            "Gypsum gives calcium and sulphur and helps pod filling.", Groundnut, GroundnutUrl, 1),

        // ---- Maize ----
        C(60, "Zn", "Maize", "Zinc sulphate", "Soil", "37.5 kg per hectare (about 15 kg per acre) for hybrid maize", "In zinc-deficient soil.", Maize, MaizeUrl, 1),
        C(61, "S", "Maize", "Sulphur with zinc and boron", "Soil", "40 kg S with 5 kg Zn and 1.5 kg B per hectare", "In deficient soils.", Maize, MaizeUrl, 1),
        C(62, "B", "Maize", "Boron with zinc and sulphur", "Soil", "1.5 kg B with 5 kg Zn and 40 kg S per hectare", "In deficient soils.", Maize, MaizeUrl, 1),

        // ---- Banana ----
        C(70, "Zn", "Banana", "Micronutrient spray", "Foliar", "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %",
            "At 3, 5 and 7 months after planting.", Banana, BananaUrl, 1),
        C(71, "Fe", "Banana", "Micronutrient spray", "Foliar", "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %",
            "At 3, 5 and 7 months after planting.", Banana, BananaUrl, 1),
        C(72, "B", "Banana", "Micronutrient spray", "Foliar", "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %",
            "At 3, 5 and 7 months after planting.", Banana, BananaUrl, 1),
    };
}
