using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V41_YieldForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CropYieldNorms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Crop = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IrrigationClass = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TypicalTPerHa = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    LowTPerHa = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    HighTPerHa = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    StateAverageTPerHa = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    RainLowMm = table.Column<int>(type: "integer", nullable: false),
                    RainHighMm = table.Column<int>(type: "integer", nullable: false),
                    WaterNeedMinMm = table.Column<int>(type: "integer", nullable: false),
                    WaterNeedMaxMm = table.Column<int>(type: "integer", nullable: false),
                    YieldResponseFactor = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    Product = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Unit = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropYieldNorms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YieldPredictions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FarmerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NormId = table.Column<int>(type: "integer", nullable: false),
                    Crop = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IrrigationClass = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AreaAcres = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Ndvi = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    NdviSource = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RainfallMm = table.Column<int>(type: "integer", nullable: false),
                    NpkPercent = table.Column<int>(type: "integer", nullable: false),
                    ExpectedT = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    LowT = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    HighT = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ConfidenceLabel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YieldPredictions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CropYieldNorms",
                columns: new[] { "Id", "Crop", "HighTPerHa", "IrrigationClass", "IsActive", "LowTPerHa", "Notes", "Product", "RainHighMm", "RainLowMm", "SortOrder", "Source", "SourceUrl", "StateAverageTPerHa", "TypicalTPerHa", "Unit", "WaterNeedMaxMm", "WaterNeedMinMm", "YieldResponseFactor" },
                values: new object[,]
                {
                    { 1, "Paddy", 7.0m, "Irrigated", true, 4.5m, "State average 3,500 kg/ha is milled rice; at 67 % milling out-turn about 5.2 t/ha of paddy. Ky assumed (rice is not in FAO paper 33).", "paddy grain", 1300, 800, 10, "TNAU rice varieties: CO 51 6,623 kg/ha, ADT 45 5,900 kg/ha; DES Statistical Hand Book of Tamil Nadu 2022-23: rice 3,500 kg/ha; FAO water need (Irrigation Water Management manual 3, table 5); Ky assumed (rice is not in FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/CO51.html", 5.2m, 5.8m, "t", 700, 450, 1.10m },
                    { 2, "Paddy", 4.5m, "Rainfed", true, 2.5m, "Semi-dry / rainfed rice of the Ramanathapuram and Sivaganga tracts. Ky assumed.", "paddy grain", 1300, 800, 11, "TNAU ARS Paramakudi: ANNA (R) 4 3.7 t/ha, PMK (R) 3 3.2-3.5 t/ha; FAO water need (Irrigation Water Management manual 3, table 5); Ky assumed (rice is not in FAO I&D paper 33)", "https://tnau.ac.in/site/ars-paramakudi/?p=379", null, 3.6m, "t", 700, 450, 1.10m },
                    { 3, "Maize", 9.0m, "Irrigated", true, 5.5m, "Hybrids. FAO: a good commercial irrigated grain yield is 6-9 t/ha.", "grain", 900, 500, 20, "TNAU maize hybrid COH(M) 8: 7,500-8,000 kg/ha irrigated; DES Statistical Hand Book of Tamil Nadu 2022-23: maize 7,007 kg/ha; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/agri_maize_Characteristics_hybrids%20COhm8.html", 7.0m, 7.5m, "t", 800, 500, 1.25m },
                    { 4, "Maize", 6.0m, "Rainfed", true, 3.5m, null, "grain", 900, 500, 21, "TNAU maize hybrid COH(M) 8: 5,000-5,500 kg/ha rainfed; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/agri_maize_Characteristics_hybrids%20COhm8.html", null, 5.0m, "t", 800, 500, 1.25m },
                    { 5, "Cotton", 2.6m, "Irrigated", true, 1.4m, "The state average is lint (313 kg/ha in 2022-23, about 0.9 t/ha of kapas at 34 % ginning), so it is not shown.", "seed cotton (kapas)", 1100, 600, 30, "TNAU cotton varieties (seed cotton, irrigated): CO 14 1,768, CCH 2623 1,800, SVPR 6 2,312 kg/ha; DES Statistical Hand Book of Tamil Nadu 2022-23: cotton 313 kg lint/ha; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/CT%20CO14.html", null, 2.0m, "t", 1300, 700, 0.85m },
                    { 6, "Cotton", 1.8m, "Rainfed", true, 0.7m, null, "seed cotton (kapas)", 1100, 600, 31, "TNAU cotton: Sumangala 1,200 kg/ha seed cotton in rainfed tracts; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://agritech.tnau.ac.in/CropProduction/Fibre%20crops/fibrecrops_cotton.html", null, 1.2m, "t", 1300, 700, 0.85m },
                    { 7, "Groundnut", 3.2m, "Irrigated", true, 1.8m, null, "dry pods", 800, 500, 40, "TNAU groundnut varieties (pods, irrigated): VRI 8 2,700 kg/ha, TMV 14 2,360 kg/ha; DES Statistical Hand Book of Tamil Nadu 2022-23: groundnut 2,598 kg/ha pods; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/grounnut%20VRI%208.html", 2.6m, 2.6m, "t", 700, 500, 0.70m },
                    { 8, "Groundnut", 2.6m, "Rainfed", true, 1.2m, null, "dry pods", 800, 500, 41, "TNAU groundnut varieties (pods, rainfed): VRI 8 2,130 kg/ha, TMV 14 2,222 kg/ha; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/grounnut%20VRI%208.html", null, 2.0m, "t", 700, 500, 0.70m },
                    { 9, "Sugarcane", 140m, "Irrigated", true, 85m, "Irrigated only (no rainfed row): the water need is far above the rainfall.", "cane", 1400, 800, 50, "TNAU promising sugarcane varieties: Co 86032 110 t/ha, CoC 24 133 t/ha; DES Statistical Hand Book of Tamil Nadu 2022-23: sugarcane 111 t/ha; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://agritech.tnau.ac.in/crop_improvement/crop_imprv_prom_varieties_sugarcrops.html", 111m, 115m, "t", 2500, 1500, 1.20m },
                    { 10, "Banana", 70m, "Irrigated", true, 35m, "Irrigated only (no rainfed row). Ky: FAO gives 1.2-1.35.", "fruit bunches", 1400, 800, 60, "ICAR-NRCB: Robusta 47.2 t/ha conventional planting, 65.9 t/ha paired rows; TNAU Grand Naine about 30 kg per plant; DES Statistical Hand Book of Tamil Nadu 2022-23 table 4.20: banana 39.4 t/ha; FAO water need (Irrigation Water Management manual 3, table 5) and Ky (FAO I&D paper 33)", "https://nrcb.icar.gov.in/oldwebsite/crop-production.php", 39.4m, 50m, "t", 2200, 1200, 1.30m },
                    { 11, "Black gram", 1.1m, "Irrigated", true, 0.6m, "Water need and Ky of FAO's bean (pulses).", "grain", 700, 450, 70, "TNAU Pulses Research Station Vamban: VBN 10 900 kg/ha irrigated, VBN 8 900 kg/ha; FAO water need and Ky of FAO's bean as a pulses proxy (Irrigation Water Management manual 3, table 5; FAO I&D paper 33)", "https://tnau.ac.in/site/prs-vamban/?p=379", null, 0.9m, "t", 500, 300, 1.15m },
                    { 12, "Black gram", 0.9m, "Rainfed", true, 0.45m, "Water need and Ky of FAO's bean (pulses).", "grain", 700, 450, 71, "TNAU Pulses Research Station Vamban: VBN 10 750 kg/ha rainfed; DES Statistical Hand Book of Tamil Nadu 2022-23: black gram 675 kg/ha; FAO water need and Ky of FAO's bean as a pulses proxy (Irrigation Water Management manual 3, table 5; FAO I&D paper 33)", "https://tnau.ac.in/site/prs-vamban/?p=379", 0.68m, 0.75m, "t", 500, 300, 1.15m },
                    { 13, "Green gram", 1.25m, "Irrigated", true, 0.7m, "Water need and Ky of FAO's bean (pulses).", "grain", 700, 450, 80, "TNAU green gram VBN 4: 1,251 kg/ha irrigated (EJPB 2021); TNAU green gram 850-1,000 kg/ha; FAO water need and Ky of FAO's bean as a pulses proxy (Irrigation Water Management manual 3, table 5; FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/CropProduction/Pulses/organic%20package%20greengram.html", null, 1.0m, "t", 500, 300, 1.15m },
                    { 14, "Green gram", 1.0m, "Rainfed", true, 0.35m, "Water need and Ky of FAO's bean (pulses). The state average (361 kg/ha) is low: most green gram is a rice-fallow or rainfed catch crop.", "grain", 700, 450, 81, "TNAU green gram VBN 4: 936 kg/ha rainfed (EJPB 2021); DES Statistical Hand Book of Tamil Nadu 2022-23: green gram 361 kg/ha; FAO water need and Ky of FAO's bean as a pulses proxy (Irrigation Water Management manual 3, table 5; FAO I&D paper 33)", "https://agritech.tnau.ac.in/agriculture/CropProduction/Pulses/organic%20package%20greengram.html", 0.36m, 0.8m, "t", 500, 300, 1.15m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropYieldNorms_Crop_IrrigationClass",
                table: "CropYieldNorms",
                columns: new[] { "Crop", "IrrigationClass" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YieldPredictions_FarmerUserId_CreatedAtUtc",
                table: "YieldPredictions",
                columns: new[] { "FarmerUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropYieldNorms");

            migrationBuilder.DropTable(
                name: "YieldPredictions");
        }
    }
}
