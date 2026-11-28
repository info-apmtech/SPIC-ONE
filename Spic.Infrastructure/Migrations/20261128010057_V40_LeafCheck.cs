using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V40_LeafCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeafDiagnoses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FarmerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Crop = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LeafAgeHint = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ImageWidth = table.Column<int>(type: "integer", nullable: false),
                    ImageHeight = table.Column<int>(type: "integer", nullable: false),
                    ImageBytes = table.Column<int>(type: "integer", nullable: false),
                    MetricsJson = table.Column<string>(type: "text", nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    Provider = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BasicReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Condition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    TopNutrient = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeafDiagnoses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutrientCorrectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nutrient = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Crop = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Product = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Dose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutrientCorrectives", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "NutrientCorrectives",
                columns: new[] { "Id", "Crop", "Dose", "IsActive", "Method", "Note", "Nutrient", "Product", "SortOrder", "Source", "SourceUrl" },
                values: new object[,]
                {
                    { 1, null, "1-2 % spray: 10-20 g urea per litre of water", true, "Foliar", "Repeat every week until the new leaves are green, then top-dress urea as in your fertilizer advice.", "N", "Urea (46 % N)", 1, "Task brief (urea 1-2 % spray); TNAU Paddy expert system sprays 1 % urea weekly until the symptoms go", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 2, null, "Top-dress the next urea split as in your Fertilizer advice", true, "Soil", "Apply on moist soil, not just before heavy rain. Fertilizer advice gives the dose for your crop and soil.", "N", "Urea (46 % N)", 2, "SPIC ONE Fertilizer advice (TNAU crop norms, farmer portal Task 4)", null },
                    { 3, null, "2 % spray: 20 g DAP per litre of water (soak overnight, spray the clear liquid)", true, "Foliar", "TNAU sprays 2 % DAP with 1 % potash (KCl) to improve yield.", "P", "DAP (18-46-0)", 1, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 4, null, "As basal for the next crop, as per your soil test (Fertilizer advice)", true, "Soil", "Phosphorus works best at sowing or planting; in a standing crop use the spray.", "P", "SSP or DAP", 2, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 5, null, "1 % spray: 10 g per litre of water", true, "Foliar", null, "K", "Muriate of potash (KCl, 60 % K2O)", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 6, null, "25 % more potash than the recommended dose", true, "Soil", "In the next top-dressing and the next crop.", "K", "Muriate of potash (KCl, 60 % K2O)", 2, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 7, null, "1 % spray: 10 g per litre of water", true, "Foliar", "TNAU's cotton schedule uses a 2 % magnesium sulphate spray with 1 % urea at boll formation.", "Mg", "Magnesium sulphate", 1, "Task brief (MgSO4 1 % spray); TNAU Cotton uses 2 %", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 8, null, "20 kg per hectare (about 8 kg per acre) as basal", true, "Soil", "In magnesium-deficient areas.", "Mg", "Magnesium sulphate", 2, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 9, null, "25 kg per hectare (about 10 kg per acre), mixed with 50 kg dry sand", true, "Soil", "Before sowing or transplanting.", "Zn", "Zinc sulphate", 1, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 10, null, "0.5 % spray: 5 g per litre of water", true, "Foliar", "Repeat every 7-15 days until the symptoms go.", "Zn", "Zinc sulphate", 2, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 11, null, "0.5 % spray with 0.1 % citric acid: 5 g ferrous sulphate and 1 g citric acid per litre", true, "Foliar", "Citric acid keeps the iron active in the spray. Spray 2-3 times, 10-15 days apart.", "Fe", "Ferrous sulphate + citric acid", 1, "Task brief (FeSO4 0.5 % + citric acid); TNAU Groundnut sprays 1 % FeSO4 at 30, 40 and 50 days", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 12, null, "50 kg per hectare with 12.5 t farmyard manure", true, "Soil", "When the soil is short of iron.", "Fe", "Ferrous sulphate", 2, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 13, null, "500 kg per hectare (about 200 kg per acre) at the last ploughing", true, "Soil", "Gypsum gives sulphur and calcium. SSP and ammonium sulphate also give sulphur.", "S", "Gypsum", 1, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 14, null, "10 kg per hectare (about 4 kg per acre)", true, "Soil", "Mix with sand or gypsum for even spreading.", "B", "Borax", 1, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 15, null, "400 kg per hectare (about 160 kg per acre) beside the plants", true, "Soil", "Not on calcareous soil. On acid soil, lime (as per a lime requirement test) gives calcium.", "Ca", "Gypsum", 1, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 16, null, "A calcium chloride spray for quick correction of a severe shortage", true, "Foliar", "Ask your SPIC officer for the strength for your crop.", "Ca", "Calcium chloride", 2, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 17, null, "1-5 kg Mn per hectare in about 200 litres of water", true, "Foliar", "On high-pH soil use ammonium sulphate instead of urea.", "Mn", "Manganese sulphate", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 20, "Paddy", "1 % spray: 10 g per litre of water, every week until the symptoms go", true, "Foliar", "Top-dress when the leaf colour chart reads below 4 (3 for White Ponni): 35 kg N per hectare in the dry season, 30 kg in the wet season.", "N", "Urea (46 % N)", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 21, "Paddy", "25 % more nitrogen than the recommended dose", true, "Soil", "Keep the field flooded; do not let water run off over the bunds just after applying.", "N", "Urea (46 % N)", 2, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 22, "Paddy", "15-30 kg P per hectare", true, "Soil", "Phosphobacteria as seed coating or seedling dip also helps.", "P", "SSP or DAP", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 23, "Paddy", "1 % spray: 10 g per litre of water", true, "Foliar", null, "K", "Muriate of potash (KCl)", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 24, "Paddy", "25 % more potash than the recommended dose", true, "Soil", "Spread the straw back on the field instead of burning it.", "K", "Muriate of potash (KCl)", 2, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 25, "Paddy", "25 kg per hectare (about 10 kg per acre) with 50 kg dry sand, just before transplanting", true, "Soil", "12.5 kg per hectare is enough when green manure or enriched FYM is applied; 37.5 kg on saline or sodic soil.", "Zn", "Zinc sulphate", 1, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 26, "Paddy", "0.5 % spray: 5 g per litre of water", true, "Foliar", "Every 7-10 days (short duration) or 15 days (medium and long duration) until the symptoms go.", "Zn", "Zinc sulphate", 2, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 27, "Paddy", "2-3 % spray: 20-30 g per litre of water, 2-3 sprays two weeks apart", true, "Foliar", null, "Fe", "Ferrous sulphate", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 28, "Paddy", "50 kg per hectare with 12.5 t farmyard manure", true, "Soil", "When the soil is short of iron.", "Fe", "Ferrous sulphate", 2, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 29, "Paddy", "500 kg per hectare (about 200 kg per acre) at the last ploughing", true, "Soil", "Gives sulphur and calcium.", "S", "Gypsum", 1, "TNAU Agritech: Rice main field, inorganic fertilizers and micronutrients (updated Jan 2023)", "https://agritech.tnau.ac.in/agriculture/agri_main_feild_%20InOrganic_biofertilizers_Rice.html" },
                    { 30, "Paddy", "2 % spray: 20 g per litre of water", true, "Foliar", "Kieserite in the soil corrects it quickly too.", "Mg", "Magnesium chloride", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 31, "Paddy", "1-5 kg Mn per hectare in about 200 litres of water", true, "Foliar", "Use ammonium sulphate instead of urea.", "Mn", "Manganese sulphate", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 32, "Paddy", "0.5-3 kg B per hectare, before planting or as a spray during growth", true, "Soil", "Avoid too much water draining through the field.", "B", "Borax", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 33, "Paddy", "Gypsum on high-pH (sodic) soil; lime on acid soil", true, "Soil", "A calcium chloride spray corrects a severe shortage quickly.", "Ca", "Gypsum or lime", 1, "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" },
                    { 40, "Cotton", "50 kg per hectare (about 20 kg per acre) as basal", true, "Soil", null, "Zn", "Zinc sulphate", 1, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 41, "Cotton", "0.5 % spray: 5 g per litre, three times at 45, 60 and 75 days", true, "Foliar", null, "Zn", "Zinc sulphate", 2, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 42, "Cotton", "2 % magnesium sulphate with 1 % urea: 20 g and 10 g per litre", true, "Foliar", "At boll formation; also against leaf reddening.", "Mg", "Magnesium sulphate + urea", 1, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 43, "Cotton", "20 kg per hectare (about 8 kg per acre) as basal", true, "Soil", "In magnesium-deficient areas, to prevent reddening.", "Mg", "Magnesium sulphate", 2, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 44, "Cotton", "0.5 % urea with 1 % KCl: 5 g and 10 g per litre, on the 45th and 65th day", true, "Foliar", "When the soil has moisture.", "N", "Urea + potash (KCl)", 1, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 45, "Cotton", "1 % KCl with 0.5 % urea: 10 g and 5 g per litre, on the 45th and 65th day", true, "Foliar", "When the soil has moisture.", "K", "Potash (KCl) + urea", 1, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 46, "Cotton", "2 % DAP with 1 % KCl: 20 g and 10 g per litre", true, "Foliar", "Improves kapas yield.", "P", "DAP + potash (KCl)", 1, "TNAU Agritech: Nutrient management, Cotton (nutritional disorders correction, updated 19.03.2013)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_cotton.html" },
                    { 50, "Groundnut", "25 kg per hectare (about 10 kg per acre) as basal", true, "Soil", "When the soil has less than 1.3 ppm zinc.", "Zn", "Zinc sulphate", 1, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 51, "Groundnut", "0.5 % spray: 5 g per litre of water", true, "Foliar", "For the standing crop.", "Zn", "Zinc sulphate", 2, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 52, "Groundnut", "1 % spray: 10 g per litre, on the 30th, 40th and 50th day", true, "Foliar", null, "Fe", "Ferrous sulphate", 1, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 53, "Groundnut", "Borax 10 kg per hectare with gypsum, on the 45th day", true, "Soil", null, "B", "Borax + gypsum", 1, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 54, "Groundnut", "400 kg per hectare (about 160 kg per acre) beside the plants on the 40th-45th day", true, "Soil", "Hoe it in and earth up. Not on calcareous soil.", "Ca", "Gypsum", 1, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 55, "Groundnut", "400 kg per hectare (about 160 kg per acre) beside the plants on the 40th-45th day", true, "Soil", "Gypsum gives calcium and sulphur and helps pod filling.", "S", "Gypsum", 1, "TNAU Agritech: Groundnut crop production advisory (nutritional disorders, gypsum)", "https://agritech.tnau.ac.in/govt_schemes_services/aas/groundnut.html" },
                    { 60, "Maize", "37.5 kg per hectare (about 15 kg per acre) for hybrid maize", true, "Soil", "In zinc-deficient soil.", "Zn", "Zinc sulphate", 1, "TNAU Agritech: Nutrient management, Maize (micronutrients)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html" },
                    { 61, "Maize", "40 kg S with 5 kg Zn and 1.5 kg B per hectare", true, "Soil", "In deficient soils.", "S", "Sulphur with zinc and boron", 1, "TNAU Agritech: Nutrient management, Maize (micronutrients)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html" },
                    { 62, "Maize", "1.5 kg B with 5 kg Zn and 40 kg S per hectare", true, "Soil", "In deficient soils.", "B", "Boron with zinc and sulphur", 1, "TNAU Agritech: Nutrient management, Maize (micronutrients)", "https://agritech.tnau.ac.in/agriculture/agri_nutrientmgt_maize.html" },
                    { 70, "Banana", "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %", true, "Foliar", "At 3, 5 and 7 months after planting.", "Zn", "Micronutrient spray", 1, "TNAU Agritech Horticulture: Banana (micronutrient spray)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html" },
                    { 71, "Banana", "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %", true, "Foliar", "At 3, 5 and 7 months after planting.", "Fe", "Micronutrient spray", 1, "TNAU Agritech Horticulture: Banana (micronutrient spray)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html" },
                    { 72, "Banana", "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %", true, "Foliar", "At 3, 5 and 7 months after planting.", "B", "Micronutrient spray", 1, "TNAU Agritech Horticulture: Banana (micronutrient spray)", "https://agritech.tnau.ac.in/horticulture/horti_fruits_banana.html" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeafDiagnoses_FarmerUserId_CreatedAtUtc",
                table: "LeafDiagnoses",
                columns: new[] { "FarmerUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NutrientCorrectives_Nutrient_Crop",
                table: "NutrientCorrectives",
                columns: new[] { "Nutrient", "Crop" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeafDiagnoses");

            migrationBuilder.DropTable(
                name: "NutrientCorrectives");
        }
    }
}
