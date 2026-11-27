using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V36_FertilizerAdvice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CropSoilTips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Crop = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    SoilType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    WaterRegime = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropSoilTips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FertilizerCropNorms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Crop = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Variant = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    WaterRegime = table.Column<int>(type: "integer", nullable: false),
                    SoilType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Basis = table.Column<int>(type: "integer", nullable: false),
                    N = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    P2O5 = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    K2O = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PlantsPerHectare = table.Column<int>(type: "integer", nullable: true),
                    Spacing = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FertilizerCropNorms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FertilizerRecommendationRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FarmerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NormId = table.Column<int>(type: "integer", nullable: false),
                    Crop = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Variant = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SoilType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AreaAcres = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SoilN = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SoilP = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SoilK = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SoilPh = table.Column<decimal>(type: "numeric(4,1)", nullable: false),
                    NStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    KStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PhBand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NeedNKgPerAcre = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NeedPKgPerAcre = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NeedKKgPerAcre = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FromSoilReport = table.Column<bool>(type: "boolean", nullable: false),
                    SoilReportId = table.Column<int>(type: "integer", nullable: true),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InputsJson = table.Column<string>(type: "text", nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FertilizerRecommendationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FertilizerNormSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NormId = table.Column<int>(type: "integer", nullable: false),
                    Seq = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DayFrom = table.Column<int>(type: "integer", nullable: true),
                    DayTo = table.Column<int>(type: "integer", nullable: true),
                    DayBasis = table.Column<int>(type: "integer", nullable: false),
                    N = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    P2O5 = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    K2O = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FertilizerNormSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FertilizerNormSplits_FertilizerCropNorms_NormId",
                        column: x => x.NormId,
                        principalTable: "FertilizerCropNorms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CropSoilTips",
                columns: new[] { "Id", "Category", "Crop", "IsActive", "SoilType", "SortOrder", "Source", "SourceUrl", "Text", "WaterRegime" },
                values: new object[,]
                {
                    { 1, "Soil", null, true, "Black Cotton Soil", 1, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Black soil lets water in slowly. Plough deep with a mould board or disc plough in summer so water soaks in and drains better.", 0 },
                    { 2, "Soil", null, true, "Black Cotton Soil", 2, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Add FYM, composted coir pith or pressmud at 25 t/ha (about 10 t/acre) every year to improve drainage and soil structure.", 0 },
                    { 3, "Water", null, true, "Black Cotton Soil", 3, "TNAU Agritech: Irrigation management, Rice", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cereals_rice.html", "Do not let water stand in the field. Where it collects after rain or irrigation, cut open drains (TNAU: about 60 cm deep and 45 cm wide for waterlogged fields).", 0 },
                    { 4, "Soil", null, true, "Clay Loam", 4, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Heavy clay soil: plough deep with a mould board or disc plough in summer to help water soak in and drain.", 0 },
                    { 5, "Water", null, true, "Clay Loam", 5, "TNAU Agritech: Irrigation management, Rice", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cereals_rice.html", "Do not let water stand in the field. Where it collects, cut open drains (TNAU: about 60 cm deep and 45 cm wide for waterlogged fields).", 0 },
                    { 6, "Soil", null, true, "Red Soil", 6, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Red soil loses water fast. Add tank silt or black soil at 25 t/ha (about 10 t/acre) a year, together with FYM, composted coir pith or pressmud at 25 t/ha.", 0 },
                    { 7, "Soil", null, true, "Red Soil", 7, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "If a hard layer below 15 cm stops the roots, chisel plough at 0.5 m spacing in both directions once in three years and add FYM or composted coir pith at 12.5 t/ha (about 5 t/acre).", 0 },
                    { 8, "Soil", null, true, "Red Soil", 8, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "If the surface forms a hard crust after rain, break it by harrowing; lime or gypsum at 2 t/ha with FYM at 12.5 t/ha prevents it.", 0 },
                    { 9, "Soil", null, true, "Sandy / Sandy Loam", 9, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Sandy soil loses water and fertilizer fast. Roll it with a 400 kg stone roller (or an oil drum filled with stones) 8 times at the right moisture, once in three years.", 0 },
                    { 10, "Soil", null, true, "Sandy / Sandy Loam", 10, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Coastal sandy soil: add tank silt to make it more productive.", 0 },
                    { 11, "Soil", null, true, "Laterite Soil", 11, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Laterite soils (for example in Pudukkottai and Kanyakumari) are often acidic. Below pH 6, apply lime as per the lime requirement test of your soil.", 0 },
                    { 12, "Soil", null, true, "Laterite Soil", 12, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Where iron or aluminium toxicity shows up (common in these soils when flooded), lime as per the lime requirement and organic manure reduce it.", 0 },
                    { 13, "Soil", null, true, "Coastal Saline", 13, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Saline soil (EC above 4 dS/m on the soil report): make field and main drains 60 cm deep and 45 cm wide and wash the salts out with water.", 0 },
                    { 14, "Soil", null, true, "Coastal Saline", 14, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "Apply FYM at 5 t/ha (about 2 t/acre) 10-15 days before transplanting or sowing to ease the salt problem.", 0 },
                    { 15, "Soil", "Cotton", true, null, 15, "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html", "Spread 12.5 t/ha (about 5 t/acre) of FYM or compost, or 2.5 t/ha of vermicompost, on the field before ploughing.", 0 },
                    { 16, "Nutrient", "Cotton", true, null, 16, "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html", "Zinc-deficient soil: zinc sulphate 50 kg/ha (about 20 kg/acre) as basal, or a 0.5 % zinc sulphate spray three times at 45, 60 and 75 days.", 0 },
                    { 17, "Nutrient", "Cotton", true, null, 17, "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html", "Where leaves turn red or magnesium is short: magnesium sulphate 20 kg/ha (about 8 kg/acre) as basal; a 2 % magnesium sulphate + 1 % urea spray at boll formation if needed.", 0 },
                    { 18, "Nutrient", "Cotton", true, null, 18, "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html", "Seed treatment and seed-line application of Azospirillum and Phosphobacteria (or Azophos) saves about 25 % of the nitrogen.", 0 },
                    { 19, "Water", "Cotton", true, null, 19, "TNAU Agritech: Irrigation management, Cotton", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html", "Irrigate right after sowing and give a life irrigation on the 5th day. The flowering phase (about 45-100 days) needs plenty of water; give less water at maturity and stop after about 150 days.", 1 },
                    { 20, "Water", "Cotton", true, "Black Cotton Soil", 20, "TNAU Agritech: Irrigation management, Cotton", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html", "Heavy (black) soil schedule: about the 20th and 40th day, then in flowering about the 55th, 70th, 85th and 100th day.", 1 },
                    { 21, "Water", "Cotton", true, "Clay Loam", 21, "TNAU Agritech: Irrigation management, Cotton", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html", "Heavy soil schedule: about the 20th and 40th day, then in flowering about the 55th, 70th, 85th and 100th day.", 1 },
                    { 22, "Water", "Cotton", true, "Sandy / Sandy Loam", 22, "TNAU Agritech: Irrigation management, Cotton", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html", "Light soil schedule: about the 20th and 35th day, then in flowering about the 48th, 60th, 72nd and 84th day.", 1 },
                    { 23, "Water", "Cotton", true, "Red Soil", 23, "TNAU Agritech: Irrigation management, Cotton", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html", "Light soil schedule: about the 20th and 35th day, then in flowering about the 48th, 60th, 72nd and 84th day.", 1 },
                    { 24, "Water", "Cotton", true, "Black Cotton Soil", 24, "TNAU Agritech: Irrigation management, Cotton", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html", "Skip-furrow or alternate-furrow irrigation suits heavy soils and saves water (skip furrow saves about half).", 1 },
                    { 25, "Water", "Cotton", true, "Clay Loam", 25, "TNAU Agritech: Irrigation management, Cotton", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cotton.html", "Skip-furrow or alternate-furrow irrigation suits clay and loam soils and saves water (skip furrow saves about half).", 1 },
                    { 26, "Nutrient", "Paddy", true, null, 26, "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html", "Zinc sulphate 25 kg/ha (about 10 kg/acre) mixed with 50 kg of dry sand just before transplanting; 12.5 kg/ha is enough when green manure (6.25 t/ha) or enriched FYM is applied.", 0 },
                    { 27, "Nutrient", "Paddy", true, null, 27, "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html", "Gypsum 500 kg/ha (about 200 kg/acre) at the last ploughing gives calcium and sulphur.", 0 },
                    { 28, "Nutrient", "Paddy", true, "Coastal Saline", 28, "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html", "Saline or sodic soil: zinc sulphate 37.5 kg/ha (about 15 kg/acre).", 0 },
                    { 29, "Water", "Paddy", true, null, 29, "TNAU Agritech: Irrigation management, Rice", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cereals_rice.html", "Keep 2 cm of water for 7 days after transplanting. Then irrigate when the ponded water has just gone (clay soil) or one day later (loamy soil) in summer; in winter 1-2 days (clay) or 3 days (loamy) after it has gone.", 0 },
                    { 30, "Water", "Paddy", true, null, 30, "TNAU Agritech: Irrigation management, Rice", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_cereals_rice.html", "Panicle initiation, booting, heading and flowering are critical: do not delay irrigation then. Stop irrigating about 15 days before harvest.", 0 },
                    { 31, "Soil", "Maize", true, null, 31, "TNAU Agritech: Nutrient management, Maize (updated 29.05.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html", "Spread 12.5 t/ha (about 5 t/acre) of FYM, compost or composted coir pith with Azospirillum and Phosphobacteria (2 kg/ha each) or Azophos (4 kg/ha) before ploughing.", 0 },
                    { 32, "Nutrient", "Maize", true, null, 32, "TNAU Agritech: Nutrient management, Maize (updated 29.05.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html", "Zinc-deficient soil: zinc sulphate 37.5 kg/ha (about 15 kg/acre), or the micronutrient mixture of the Department of Agriculture 12.5 kg/ha mixed with sand.", 0 },
                    { 33, "Water", "Maize", true, null, 33, "TNAU Agritech: Irrigation management, Maize", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_maize.html", "45-65 days after sowing (flowering) is the most critical time for water: the yield drops a lot if the crop is short of water then.", 1 },
                    { 34, "Water", "Maize", true, "Black Cotton Soil", 34, "TNAU Agritech: Irrigation management, Maize", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_maize.html", "Heavy soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 25th, 36th, 48th, 60th, 72nd and 85th day.", 1 },
                    { 35, "Water", "Maize", true, "Clay Loam", 35, "TNAU Agritech: Irrigation management, Maize", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_maize.html", "Heavy soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 25th, 36th, 48th, 60th, 72nd and 85th day.", 1 },
                    { 36, "Water", "Maize", true, "Sandy / Sandy Loam", 36, "TNAU Agritech: Irrigation management, Maize", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_maize.html", "Light soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 22nd, 32nd, 40th, 50th, 60th, 72nd, 85th and 95th day.", 1 },
                    { 37, "Water", "Maize", true, "Red Soil", 37, "TNAU Agritech: Irrigation management, Maize", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_maize.html", "Light soil schedule: after sowing, life irrigations on the 4th and 12th day, then about the 22nd, 32nd, 40th, 50th, 60th, 72nd, 85th and 95th day.", 1 },
                    { 38, "Nutrient", "Groundnut", true, null, 38, "TNAU Agritech: Groundnut, rainfed (crop production advisory)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html", "Gypsum 400 kg/ha (about 160 kg/acre) beside the plants between the 40th and 70th day (depending on soil moisture), hoed in and earthed up: better pod filling. Do not use gypsum on calcareous soil.", 0 },
                    { 39, "Nutrient", "Groundnut", true, null, 39, "TNAU Agritech: Groundnut, rainfed (crop production advisory)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html", "Zinc sulphate 25 kg/ha (about 10 kg/acre) as basal when soil zinc is below 1.3 ppm.", 0 },
                    { 40, "Nutrient", "Groundnut", true, null, 40, "TNAU Agritech: Groundnut, rainfed (crop production advisory)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html", "Micronutrient mixture of the Department of Agriculture 12.5 kg/ha, mixed with sand to 50 kg and spread on the surface right after sowing. Do not mix it into the soil.", 0 },
                    { 41, "Soil", "Groundnut", true, null, 41, "TNAU Agritech: Groundnut, rainfed (crop production advisory)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html", "If the surface crusts, lime at 2 t/ha with FYM or composted coir pith at 12.5 t/ha helps the seedlings come up.", 0 },
                    { 42, "Soil", "Black gram", true, null, 42, "TNAU Agritech: Nutrient management, Blackgram (2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_blackgram.html", "Lime is recommended for pulses when the soil pH is below 6.0.", 0 },
                    { 43, "Nutrient", "Black gram", true, null, 43, "TNAU Agritech: Nutrient management, Blackgram (2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_blackgram.html", "Irrigated crop: zinc sulphate 25 kg/ha (about 10 kg/acre).", 1 },
                    { 44, "Nutrient", "Black gram", true, null, 44, "TNAU Agritech: Nutrient management, Blackgram (2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_blackgram.html", "Spray 1 % urea on the 30th and 45th day after sowing for a better yield; a 2 % DAP spray at flowering and 15 days later also helps.", 0 },
                    { 45, "Water", "Black gram", true, null, 45, "TNAU Agritech: Irrigation management, Blackgram / Greengram", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_blackgram.html", "Irrigate right after sowing, a life irrigation on the 3rd day, then every 7-10 days. Flowering and pod formation must not lack water; never let water stand.", 1 },
                    { 46, "Soil", "Green gram", true, null, 46, "TNAU Agritech: Nutrient management, Blackgram (2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_blackgram.html", "Lime is recommended for pulses when the soil pH is below 6.0.", 0 },
                    { 47, "Nutrient", "Green gram", true, null, 47, "TNAU Agritech: Nutrient management, Greengram (04.06.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_greengram.html", "Irrigated crop: zinc sulphate 25 kg/ha (about 10 kg/acre).", 1 },
                    { 48, "Nutrient", "Green gram", true, null, 48, "TNAU Agritech: Nutrient management, Greengram (04.06.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_greengram.html", "Spray 1 % urea on the 30th and 45th day after sowing for a better yield.", 0 },
                    { 49, "Water", "Green gram", true, null, 49, "TNAU Agritech: Irrigation management, Blackgram / Greengram", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_blackgram.html", "Irrigate right after sowing, a life irrigation on the 3rd day, then every 7-10 days. Flowering and pod formation must not lack water; never let water stand.", 1 },
                    { 50, "Nutrient", "Soybean", true, null, 50, "TNAU Agritech: Nutrient management, Soybean", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_soybean.html", "Irrigated crop: zinc sulphate 25 kg/ha (about 10 kg/acre).", 1 },
                    { 51, "Water", "Soybean", true, null, 51, "TNAU Agritech: Irrigation management, Soybean", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_soybean.html", "Irrigate after sowing, a life irrigation on the 3rd day, then every 7-10 days in summer or 10-15 days in winter. Soybean is harmed by standing water; keep it free of water stress from flowering to maturity.", 1 },
                    { 52, "Soil", "Wheat", true, null, 52, "TNAU Agritech: Nutrient management, Wheat", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_wheat.html", "Spread 12.5 t/ha (about 5 t/acre) of FYM or compost on the field before ploughing.", 0 },
                    { 53, "Water", "Wheat", true, null, 53, "TNAU Agritech: Critical stages for irrigation", "https://agritech.tnau.ac.in/agriculture/agri_irrigationmgt_criticalstage.html", "Crown root initiation (about 15-20 days) is the most critical stage for water; tillering, jointing, booting, flowering, milk and dough stages also need water.", 1 },
                    { 54, "Soil", "Chilli", true, null, 54, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", "Apply FYM at 25 t/ha (about 10 t/acre) before planting.", 0 },
                    { 55, "Nutrient", "Chilli", true, null, 55, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", "Giving the potash as potassium sulphate improves the quality of the pods.", 0 },
                    { 56, "Soil", "Tomato", true, null, 56, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", "Apply FYM at 25 t/ha (about 10 t/acre) before planting.", 0 },
                    { 57, "Nutrient", "Tomato", true, null, 57, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", "Basal borax 10 kg/ha (about 4 kg/acre) and zinc sulphate 50 kg/ha (about 20 kg/acre).", 0 },
                    { 58, "Soil", "Onion", true, null, 58, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", "Apply FYM at 25 t/ha (about 10 t/acre) with Azospirillum and Phosphobacteria 2 kg/ha each before planting.", 0 },
                    { 59, "Nutrient", "Onion", true, null, 59, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", "Bellary onion: zinc sulphate 50 kg/ha (about 20 kg/acre) at the last ploughing.", 0 },
                    { 60, "Soil", "Turmeric", true, null, 60, "TNAU Agritech Horticulture: Fertilizer schedule for spice crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20SPICES.pdf", "Mix in FYM 10 t/ha (about 4 t/acre) and neem or groundnut cake 200 kg/ha at planting; apply Azospirillum and Phosphobacteria 10 kg/ha each one month after planting.", 0 },
                    { 61, "Nutrient", "Turmeric", true, null, 61, "TNAU Agritech Horticulture: Fertilizer schedule for spice crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20SPICES.pdf", "Basal ferrous sulphate 30 kg/ha and zinc sulphate 15 kg/ha. For boron, iron and zinc shortage at rhizome development, TNAU advises a micronutrient spray twice at 25-day intervals.", 0 },
                    { 62, "Water", "Banana", true, null, 62, "TNAU Agritech Horticulture: Banana (application of fertilizers, spacing)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html", "Irrigate right after planting and again after 4 days, then once a week (garden land) or every 10-15 days (wetland). Irrigate well after every manure application.", 0 },
                    { 63, "Water", "Banana", true, null, 63, "TNAU Agritech Horticulture: Banana (application of fertilizers, spacing)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html", "With drip: 5-10 litres per plant a day up to the 4th month, 10-15 litres from the 5th month to shooting, and 15 litres from shooting until 15 days before harvest.", 0 },
                    { 64, "Nutrient", "Banana", true, null, 64, "TNAU Agritech Horticulture: Banana (application of fertilizers, spacing)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html", "Spray zinc sulphate 0.5 %, ferrous sulphate 0.2 %, copper sulphate 0.2 % and boric acid 0.1 % at 3, 5 and 7 months after planting for yield and quality.", 0 },
                    { 65, "Soil", "Coconut", true, null, 65, "TNAU Agritech Horticulture: Coconut manuring; spacing 7.5 x 7.5 m = 175 palms/ha (coconut spacing page)", "https://agritech.tnau.ac.in/horticulture/horti_pcrops_coconut_manuring.html", "Give 50 kg of FYM, compost or green manure per palm a year; sunnhemp sown in the basin (50 g per palm) and ploughed in before flowering also works.", 0 },
                    { 66, "Nutrient", "Coconut", true, null, 66, "TNAU Agritech Horticulture: Coconut manuring; spacing 7.5 x 7.5 m = 175 palms/ha (coconut spacing page)", "https://agritech.tnau.ac.in/horticulture/horti_pcrops_coconut_manuring.html", "Biofertilizers: 50 g Azospirillum, 50 g Phosphobacteria (or 100 g Azophos) and 50 g VAM per palm every 6 months, mixed with compost; never mixed with chemical fertilizer.", 0 }
                });

            migrationBuilder.InsertData(
                table: "FertilizerCropNorms",
                columns: new[] { "Id", "Basis", "Crop", "DurationDays", "IsActive", "K2O", "N", "Notes", "P2O5", "PlantsPerHectare", "SoilType", "SortOrder", "Source", "SourceUrl", "Spacing", "Variant", "WaterRegime" },
                values: new object[,]
                {
                    { 1, 0, "Paddy", 135, true, 50m, 150m, "TNAU: phosphorus all as basal; nitrogen and potash in four equal splits (basal, active tillering, panicle initiation, heading). Do not cut the tillering and panicle initiation doses. Stage days are counted from sowing.", 50m, null, null, 10, "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html", null, "Medium duration (about 135 days)", 1 },
                    { 2, 0, "Paddy", 150, true, 50m, 150m, "TNAU: phosphorus all as basal; nitrogen and potash in four equal splits (basal, active tillering, panicle initiation, heading). Do not cut the tillering and panicle initiation doses. Stage days are counted from sowing.", 50m, null, null, 11, "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html", null, "Long duration (about 150 days)", 1 },
                    { 3, 0, "Paddy", 105, true, 50m, 150m, "TNAU: phosphorus all as basal; nitrogen and potash in four equal splits (basal, active tillering, panicle initiation, heading). Do not cut the tillering and panicle initiation doses. Stage days are counted from sowing.", 50m, null, null, 12, "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html", null, "Short duration, Cauvery delta and Coimbatore (about 105 days)", 1 },
                    { 4, 0, "Paddy", 105, true, 40m, 120m, "TNAU: phosphorus all as basal; nitrogen and potash in four equal splits (basal, active tillering, panicle initiation, heading). Do not cut the tillering and panicle initiation doses. Stage days are counted from sowing.", 40m, null, null, 13, "TNAU Agritech: Rice main field, application of inorganic fertilizers (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html", null, "Short duration, other areas (about 105 days)", 1 },
                    { 5, 0, "Cotton", null, true, 40m, 80m, "TNAU: half the N and K and all the P2O5 as basal, the rest of N and K at 40-45 days. If the basal dose could not be given, apply it on the 25th day. Place the fertilizer in a band two-thirds down from the top of the ridge and cover it.", 40m, null, null, 20, "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html", null, "Irrigated varieties (MCU 5, MCU 12, MCU 13, Suvin, SVPR 2)", 1 },
                    { 6, 0, "Cotton", null, true, 60m, 120m, "TNAU: hybrid nitrogen in three equal splits (basal, 40-45 and 60-65 days); all the P2O5 basal; potash half basal and half at 40-45 days, as for varieties.", 60m, null, null, 21, "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html", null, "Irrigated hybrids (e.g. TCHB 213)", 1 },
                    { 7, 0, "Cotton", null, true, 40m, 40m, "TNAU gives no split for rainfed cotton: apply the whole dose at sowing when the soil has moisture. Spray 0.5 % urea + 1 % potassium chloride on the 45th and 65th day if there is moisture.", 20m, null, null, 22, "TNAU Agritech: Nutrient management, Cotton (updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html", null, "Rainfed (SVPR 2, KC 2)", 2 },
                    { 8, 0, "Maize", null, true, 50m, 135m, "TNAU: a quarter of the N and all the P2O5 and K2O at sowing in a furrow 6 cm deep on the side of the ridge (cover with 4 cm of soil); half the N on the 25th day; the last quarter on the 45th day. With Azospirillum seed and soil treatment the N can be cut by 25 %.", 62.5m, null, null, 30, "TNAU Agritech: Nutrient management, Maize (updated 29.05.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html", null, "Irrigated varieties", 1 },
                    { 9, 0, "Maize", null, true, 75m, 150m, "TNAU: hybrid maize 150:75:75, applied like the varieties (a quarter of N basal, half on the 25th day, a quarter on the 45th day).", 75m, null, null, 31, "TNAU Agritech: Nutrient management, Maize (updated 29.05.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html", null, "Irrigated hybrids", 1 },
                    { 10, 0, "Maize", null, true, 0m, 40m, "TNAU: rainfed maize on Vertisols (black soils) 40:20:0; half the N and all the P2O5 with enriched FYM at sowing, the other half of N at tasselling.", 20m, null, "Black Cotton Soil", 32, "TNAU Agritech: Nutrient management, Maize (updated 29.05.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html", null, "Rainfed", 2 },
                    { 11, 0, "Maize", null, true, 30m, 60m, "TNAU: rainfed maize on Alfisols (red soils) 60:30:30; half the N and all the P2O5 and K2O with enriched FYM at sowing, the other half of N at tasselling.", 30m, null, "Red Soil", 33, "TNAU Agritech: Nutrient management, Maize (updated 29.05.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html", null, "Rainfed", 2 },
                    { 12, 0, "Groundnut", null, true, 45m, 10m, "TNAU rainfed groundnut blanket recommendation (no soil test). Gypsum and micronutrients are separate (see the nutrient tips).", 10m, null, null, 40, "TNAU Agritech: Groundnut, rainfed (crop production advisory)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html", null, "Rainfed", 2 },
                    { 13, 0, "Black gram", null, true, 12.5m, 12.5m, "TNAU: all as basal before sowing, plus 10 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.", 25m, null, null, 50, "TNAU Agritech: Nutrient management, Blackgram (2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_blackgram.html", null, "Rainfed", 2 },
                    { 14, 0, "Black gram", null, true, 25m, 25m, "TNAU: all as basal before sowing, plus 20 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.", 50m, null, null, 51, "TNAU Agritech: Nutrient management, Blackgram (2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_blackgram.html", null, "Irrigated", 1 },
                    { 15, 0, "Green gram", null, true, 12.5m, 12.5m, "TNAU: all as basal before sowing, plus 10 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.", 25m, null, null, 52, "TNAU Agritech: Nutrient management, Greengram (04.06.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_greengram.html", null, "Rainfed", 2 },
                    { 16, 0, "Green gram", null, true, 25m, 25m, "TNAU: all as basal before sowing, plus 20 kg sulphur per hectare as gypsum when SSP is not the phosphorus source.", 50m, null, null, 53, "TNAU Agritech: Nutrient management, Greengram (04.06.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_greengram.html", null, "Irrigated", 1 },
                    { 17, 0, "Soybean", null, true, 40m, 20m, "TNAU: all as basal, plus 40 kg sulphur per hectare as gypsum (220 kg gypsum/ha).", 80m, null, null, 54, "TNAU Agritech: Nutrient management, Soybean", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_soybean.html", null, "Irrigated", 1 },
                    { 18, 0, "Soybean", null, true, 20m, 20m, "TNAU: all as basal when there is enough soil moisture, plus 20 kg sulphur per hectare.", 40m, null, null, 55, "TNAU Agritech: Nutrient management, Soybean", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_soybean.html", null, "Rainfed", 2 },
                    { 19, 0, "Wheat", null, true, 40m, 80m, "TNAU: half the N and all the P2O5 and K2O basal in the sowing line; the other half of N at crown root initiation (15-20 days).", 40m, null, null, 60, "TNAU Agritech: Nutrient management, Wheat", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_wheat.html", null, "Irrigated", 1 },
                    { 20, 0, "Chilli", null, true, 30m, 120m, "TNAU: 30:60:30 basal, then 30 kg N each on the 30th, 60th and 90th day after planting. Potash as potassium sulphate improves pod quality.", 60m, null, null, 70, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", null, "Irrigated (transplanted)", 1 },
                    { 21, 0, "Tomato", null, true, 50m, 150m, "TNAU: 75:100:50 basal (with borax 10 kg and zinc sulphate 50 kg per hectare), then 75 kg N on the 30th day at earthing up.", 100m, null, null, 71, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", null, "Varieties (transplanted)", 1 },
                    { 22, 0, "Onion", null, true, 30m, 60m, "TNAU: 30:60:30 basal, then 30 kg N on the 30th day.", 60m, null, null, 72, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", null, "Small onion (aggregatum)", 1 },
                    { 23, 0, "Onion", null, true, 75m, 100m, "TNAU: 50:150:75 basal (and zinc sulphate 50 kg/ha at the last ploughing), then 50 kg N on the 30th day after planting.", 150m, null, null, 73, "TNAU Agritech Horticulture: Fertilizer schedule for vegetable crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20VEGETABLES.pdf", null, "Bellary onion", 1 },
                    { 24, 0, "Turmeric", null, true, 108m, 150m, "TNAU: 25:60:18 basal, then 25 kg N and 18 kg K2O on the 30th, 60th, 90th, 120th and 150th day after planting (total 150:60:108).", 60m, null, null, 74, "TNAU Agritech Horticulture: Fertilizer schedule for spice crops", "https://agritech.tnau.ac.in/horticulture/FERTILIZER%20SCHEDULE%20FOR%20SPICES.pdf", null, "Irrigated", 1 },
                    { 25, 1, "Banana", null, true, 330m, 110m, "TNAU: grams per plant; N and K in three splits (3rd, 5th and 7th month), P in the 3rd month; N as neem coated urea. Tissue culture banana: 50 % extra fertilizer at the 2nd, 4th, 6th and 8th month. Plants are counted at 1.8 x 1.8 m (3,080 per hectare); enter your own count for other spacing.", 35m, 3080, null, 80, "TNAU Agritech Horticulture: Banana (application of fertilizers, spacing)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html", "1.8 x 1.8 m", "Garden land, varieties other than Nendran (e.g. Robusta)", 1 },
                    { 26, 1, "Banana", null, true, 300m, 150m, "TNAU: grams per plant; N and K in three splits (3rd, 5th and 7th month), P in the 3rd month; N as neem coated urea. Plants are counted at 1.8 x 1.8 m (3,080 per hectare); enter your own count for other spacing.", 90m, 3080, null, 81, "TNAU Agritech Horticulture: Banana (application of fertilizers, spacing)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html", "1.8 x 1.8 m", "Garden land, Nendran", 1 },
                    { 27, 1, "Coconut", null, true, 1200m, 560m, "TNAU: grams per palm per year in two equal splits (June - July and December - January), with 50 kg FYM or compost per palm. Apply in a 1.8 m circle around the palm, mix in and irrigate. Younger palms get a quarter to three quarters of this dose. Palms are counted at 7.5 x 7.5 m (175 per hectare).", 320m, 175, null, 90, "TNAU Agritech Horticulture: Coconut manuring; spacing 7.5 x 7.5 m = 175 palms/ha (coconut spacing page)", "https://agritech.tnau.ac.in/horticulture/horti_pcrops_coconut_manuring.html", "7.5 x 7.5 m", "Palms from the 5th year", 0 }
                });

            migrationBuilder.InsertData(
                table: "FertilizerNormSplits",
                columns: new[] { "Id", "DayBasis", "DayFrom", "DayTo", "K2O", "N", "NormId", "P2O5", "Seq", "Stage" },
                values: new object[,]
                {
                    { 1, 1, null, null, 12.5m, 37.5m, 1, 50m, 1, "Basal (at transplanting)" },
                    { 2, 0, 50, 55, 12.5m, 37.5m, 1, 0m, 2, "Active tillering" },
                    { 3, 0, 70, 75, 12.5m, 37.5m, 1, 0m, 3, "Panicle initiation" },
                    { 4, 0, 100, 105, 12.5m, 37.5m, 1, 0m, 4, "Heading" },
                    { 5, 1, null, null, 12.5m, 37.5m, 2, 50m, 1, "Basal (at transplanting)" },
                    { 6, 0, 55, 60, 12.5m, 37.5m, 2, 0m, 2, "Active tillering" },
                    { 7, 0, 85, 90, 12.5m, 37.5m, 2, 0m, 3, "Panicle initiation" },
                    { 8, 0, 115, 120, 12.5m, 37.5m, 2, 0m, 4, "Heading" },
                    { 9, 1, null, null, 12.5m, 37.5m, 3, 50m, 1, "Basal (at transplanting)" },
                    { 10, 0, 35, 40, 12.5m, 37.5m, 3, 0m, 2, "Active tillering" },
                    { 11, 0, 45, 50, 12.5m, 37.5m, 3, 0m, 3, "Panicle initiation" },
                    { 12, 0, 70, 75, 12.5m, 37.5m, 3, 0m, 4, "Heading" },
                    { 13, 1, null, null, 10m, 30m, 4, 40m, 1, "Basal (at transplanting)" },
                    { 14, 0, 35, 40, 10m, 30m, 4, 0m, 2, "Active tillering" },
                    { 15, 0, 45, 50, 10m, 30m, 4, 0m, 3, "Panicle initiation" },
                    { 16, 0, 70, 75, 10m, 30m, 4, 0m, 4, "Heading" },
                    { 17, 0, null, null, 20m, 40m, 5, 40m, 1, "Basal (at sowing)" },
                    { 18, 0, 40, 45, 20m, 40m, 5, 0m, 2, "Top dressing" },
                    { 19, 0, null, null, 30m, 40m, 6, 60m, 1, "Basal (at sowing)" },
                    { 20, 0, 40, 45, 30m, 40m, 6, 0m, 2, "1st top dressing" },
                    { 21, 0, 60, 65, 0m, 40m, 6, 0m, 3, "2nd top dressing" },
                    { 22, 0, null, null, 40m, 40m, 7, 20m, 1, "Basal (at sowing)" },
                    { 23, 0, null, null, 50m, 33.75m, 8, 62.5m, 1, "Basal (at sowing)" },
                    { 24, 0, 25, 25, 0m, 67.5m, 8, 0m, 2, "1st top dressing" },
                    { 25, 0, 45, 45, 0m, 33.75m, 8, 0m, 3, "2nd top dressing" },
                    { 26, 0, null, null, 75m, 37.5m, 9, 75m, 1, "Basal (at sowing)" },
                    { 27, 0, 25, 25, 0m, 75m, 9, 0m, 2, "1st top dressing" },
                    { 28, 0, 45, 45, 0m, 37.5m, 9, 0m, 3, "2nd top dressing" },
                    { 29, 0, null, null, 0m, 20m, 10, 20m, 1, "Basal (at sowing)" },
                    { 30, 2, null, null, 0m, 20m, 10, 0m, 2, "At tasselling" },
                    { 31, 0, null, null, 30m, 30m, 11, 30m, 1, "Basal (at sowing)" },
                    { 32, 2, null, null, 0m, 30m, 11, 0m, 2, "At tasselling" },
                    { 33, 0, null, null, 45m, 10m, 12, 10m, 1, "Basal (at sowing)" },
                    { 34, 0, null, null, 12.5m, 12.5m, 13, 25m, 1, "Basal (before sowing)" },
                    { 35, 0, null, null, 25m, 25m, 14, 50m, 1, "Basal (before sowing)" },
                    { 36, 0, null, null, 12.5m, 12.5m, 15, 25m, 1, "Basal (before sowing)" },
                    { 37, 0, null, null, 25m, 25m, 16, 50m, 1, "Basal (before sowing)" },
                    { 38, 0, null, null, 40m, 20m, 17, 80m, 1, "Basal (at sowing)" },
                    { 39, 0, null, null, 20m, 20m, 18, 40m, 1, "Basal (at sowing)" },
                    { 40, 0, null, null, 40m, 40m, 19, 40m, 1, "Basal (at sowing)" },
                    { 41, 0, 15, 20, 0m, 40m, 19, 0m, 2, "Crown root initiation" },
                    { 42, 1, null, null, 30m, 30m, 20, 60m, 1, "Basal (at planting)" },
                    { 43, 1, 30, 30, 0m, 30m, 20, 0m, 2, "1st top dressing" },
                    { 44, 1, 60, 60, 0m, 30m, 20, 0m, 3, "2nd top dressing" },
                    { 45, 1, 90, 90, 0m, 30m, 20, 0m, 4, "3rd top dressing" },
                    { 46, 1, null, null, 50m, 75m, 21, 100m, 1, "Basal (at planting)" },
                    { 47, 1, 30, 30, 0m, 75m, 21, 0m, 2, "Top dressing (earthing up)" },
                    { 48, 1, null, null, 30m, 30m, 22, 60m, 1, "Basal (at planting)" },
                    { 49, 1, 30, 30, 0m, 30m, 22, 0m, 2, "Top dressing" },
                    { 50, 1, null, null, 75m, 50m, 23, 150m, 1, "Basal (at planting)" },
                    { 51, 1, 30, 30, 0m, 50m, 23, 0m, 2, "Top dressing" },
                    { 52, 1, null, null, 18m, 25m, 24, 60m, 1, "Basal (at planting)" },
                    { 53, 1, 30, 30, 18m, 25m, 24, 0m, 2, "1st top dressing" },
                    { 54, 1, 60, 60, 18m, 25m, 24, 0m, 3, "2nd top dressing" },
                    { 55, 1, 90, 90, 18m, 25m, 24, 0m, 4, "3rd top dressing" },
                    { 56, 1, 120, 120, 18m, 25m, 24, 0m, 5, "4th top dressing" },
                    { 57, 1, 150, 150, 18m, 25m, 24, 0m, 6, "5th top dressing" },
                    { 58, 2, null, null, 110m, 36.67m, 25, 35m, 1, "3rd month after planting" },
                    { 59, 2, null, null, 110m, 36.67m, 25, 0m, 2, "5th month after planting" },
                    { 60, 2, null, null, 110m, 36.66m, 25, 0m, 3, "7th month after planting" },
                    { 61, 2, null, null, 100m, 50m, 26, 90m, 1, "3rd month after planting" },
                    { 62, 2, null, null, 100m, 50m, 26, 0m, 2, "5th month after planting" },
                    { 63, 2, null, null, 100m, 50m, 26, 0m, 3, "7th month after planting" },
                    { 64, 2, null, null, 600m, 280m, 27, 160m, 1, "June - July" },
                    { 65, 2, null, null, 600m, 280m, 27, 160m, 2, "December - January" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropSoilTips_Crop_SoilType",
                table: "CropSoilTips",
                columns: new[] { "Crop", "SoilType" });

            migrationBuilder.CreateIndex(
                name: "IX_FertilizerCropNorms_Crop_SortOrder",
                table: "FertilizerCropNorms",
                columns: new[] { "Crop", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FertilizerNormSplits_NormId_Seq",
                table: "FertilizerNormSplits",
                columns: new[] { "NormId", "Seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FertilizerRecommendationRuns_FarmerUserId_CreatedAtUtc",
                table: "FertilizerRecommendationRuns",
                columns: new[] { "FarmerUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropSoilTips");

            migrationBuilder.DropTable(
                name: "FertilizerNormSplits");

            migrationBuilder.DropTable(
                name: "FertilizerRecommendationRuns");

            migrationBuilder.DropTable(
                name: "FertilizerCropNorms");
        }
    }
}
