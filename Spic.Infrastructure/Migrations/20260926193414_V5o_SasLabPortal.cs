using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5o_SasLabPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminRemarks",
                table: "SamplePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "SamplePayments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "SamplePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "SamplePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanceRemarks",
                table: "SamplePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinanceStatus",
                table: "SamplePayments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinanceVerifiedAt",
                table: "SamplePayments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanceVerifiedByName",
                table: "SamplePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ForwardedAt",
                table: "SamplePayments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForwardedToName",
                table: "SamplePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoRemarks",
                table: "SamplePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMode",
                table: "SamplePayments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransactionDate",
                table: "SamplePayments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VerifiedAmount",
                table: "SamplePayments",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LabParameterId",
                table: "SampleLabResults",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnalysisCompletedAt",
                table: "SampleItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AnalysisStartedAt",
                table: "SampleItems",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnalysisStatus",
                table: "SampleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "SampleConsignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabCropRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Crop = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: true),
                    Product = table.Column<string>(type: "text", nullable: false),
                    KgPerAcre = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabCropRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    NormalRange = table.Column<string>(type: "text", nullable: true),
                    RangeMin = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    RangeMax = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    ReportingLimit = table.Column<string>(type: "text", nullable: true),
                    AppliesTo = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValueType = table.Column<int>(type: "integer", nullable: false),
                    Options = table.Column<string>(type: "text", nullable: true),
                    DerivedFromCode = table.Column<string>(type: "text", nullable: true),
                    DerivedFactor = table.Column<decimal>(type: "numeric", nullable: true),
                    LowLabel = table.Column<string>(type: "text", nullable: false),
                    NormalLabel = table.Column<string>(type: "text", nullable: false),
                    HighLabel = table.Column<string>(type: "text", nullable: false),
                    ModerateFrom = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    LowHint = table.Column<string>(type: "text", nullable: true),
                    NormalHint = table.Column<string>(type: "text", nullable: true),
                    HighHint = table.Column<string>(type: "text", nullable: true),
                    RecommendationGroup = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Lang = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTranslations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SampleBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    BatchDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "text", nullable: true),
                    AssignedToName = table.Column<string>(type: "text", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AssignedByName = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TakenForAnalysisAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AnalysisStartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AnalysisCompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FinalRemarks = table.Column<string>(type: "text", nullable: true),
                    ReportCode = table.Column<string>(type: "text", nullable: true),
                    ReportGeneratedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ByUserId = table.Column<string>(type: "text", nullable: true),
                    ByName = table.Column<string>(type: "text", nullable: true),
                    ByRole = table.Column<string>(type: "text", nullable: true),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabActivities_SampleBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "SampleBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StoredPath = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "text", nullable: true),
                    UploadedByName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabDocuments_SampleBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "SampleBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    SampleItemId = table.Column<int>(type: "integer", nullable: false),
                    SampleType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    GeneratedByName = table.Column<string>(type: "text", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    LastDownloadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PrintedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FinancialYearStart = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabReports_SampleBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "SampleBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabReports_SampleItems_SampleItemId",
                        column: x => x.SampleItemId,
                        principalTable: "SampleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LabCropRecommendations",
                columns: new[] { "Id", "Crop", "DayNumber", "IsActive", "KgPerAcre", "Product", "SortOrder", "Stage" },
                values: new object[,]
                {
                    { 1, "Banana", null, true, 300m, "SPIC Jyoti", 1, 0 },
                    { 2, "Banana", null, true, 200m, "SPIC Gypsum", 2, 0 },
                    { 3, "Banana", null, true, 100m, "SPIC Sangamam", 3, 0 },
                    { 4, "Banana", null, true, 0m, "SPIC DAP", 4, 0 },
                    { 5, "Banana", null, true, 0m, "SPIC Urea", 5, 0 },
                    { 6, "Banana", null, true, 0m, "Potash", 6, 0 },
                    { 7, "Banana", null, true, 12m, "SPIC Zinc Sulphate", 7, 0 },
                    { 8, "Banana", null, true, 0m, "Ferrous Sulphate", 8, 0 },
                    { 9, "Banana", null, true, 0m, "Manganese Sulphate", 9, 0 },
                    { 10, "Banana", null, true, 5m, "Copper Sulphate", 10, 0 },
                    { 11, "Banana", 90, true, 82.5m, "SPIC DAP", 1, 1 },
                    { 12, "Banana", 90, true, 0m, "SPIC Urea", 2, 1 },
                    { 13, "Banana", 90, true, 150m, "Potash", 3, 1 },
                    { 14, "Banana", 150, true, 125m, "SPIC Urea", 1, 2 },
                    { 15, "Banana", 150, true, 112.5m, "Potash", 2, 2 },
                    { 16, "Banana", 210, true, 125m, "SPIC Urea", 1, 3 },
                    { 17, "Banana", 210, true, 112.5m, "Potash", 2, 3 }
                });

            migrationBuilder.InsertData(
                table: "LabParameters",
                columns: new[] { "Id", "AppliesTo", "Code", "DerivedFactor", "DerivedFromCode", "HighHint", "HighLabel", "IsActive", "LowHint", "LowLabel", "ModerateFrom", "Name", "NormalHint", "NormalLabel", "NormalRange", "Options", "RangeMax", "RangeMin", "RecommendationGroup", "ReportingLimit", "SortOrder", "Unit", "ValueType" },
                values: new object[,]
                {
                    { 1, 0, "S-PH", null, null, "Apply gypsum to reduce alkalinity", "Alkaline", true, "Apply lime to correct soil acidity", "Acidic", null, "pH", "Suitable for crop growth", "Neutral", "6.5 - 7.5", null, 7.5m, 6.5m, "General", "0.1", 1, "-", 0 },
                    { 2, 0, "S-EC", null, null, "Improve drainage and leach salts", "Saline", true, "No salinity issue", "Safe", null, "EC", "No salinity issue", "Safe", "0 - 1.0", null, 1.0m, 0m, "General", "0.01", 2, "dS/m", 0 },
                    { 3, 0, "S-OC", null, null, "Organic matter is high; no addition needed", "High", true, "Add organic manure / compost", "Low", null, "Organic Carbon", "Maintain organic matter", "Medium", "0.5 - 0.75", null, 0.75m, 0.5m, "Organic", "0.01", 3, "%", 0 },
                    { 4, 0, "S-N", null, null, "Reduce nitrogen-based fertilizer", "High", true, "Apply nitrogen fertilizer", "Low", null, "Nitrogen", "Maintain standard nitrogen dosage", "Medium", "280 - 560", null, 560m, 280m, "Fertilizer", "1", 4, "kg/ha", 0 },
                    { 5, 0, "S-P", null, null, "Reduce phosphorus-based fertilizer", "High", true, "Apply phosphorus fertilizer", "Low", null, "Phosphorus", "Maintain standard phosphorus dosage", "Medium", "22 - 56", null, 56m, 22m, "Fertilizer", "1", 5, "kg/ha", 0 },
                    { 6, 0, "S-K", null, null, "Reduce potassium-based fertilizer", "High", true, "Apply potassium fertilizer", "Low", null, "Potassium", "Maintain standard potassium dosage", "Medium", "110 - 280", null, 280m, 110m, "Fertilizer", "1", 6, "kg/ha", 0 },
                    { 7, 0, "S-ZN", null, null, "Avoid further zinc application", "High", true, "Apply zinc micronutrient", "Low", null, "Zinc", "Zinc is adequate", "Medium", "0.6 - 1.2", null, 1.2m, 0.6m, "Micronutrient", "0.1", 7, "ppm", 0 },
                    { 8, 0, "S-FE", null, null, "Avoid further iron application", "High", true, "Apply iron micronutrient (ferrous sulphate)", "Low", null, "Iron", "Iron is adequate", "Medium", "4.5 - 9.0", null, 9.0m, 4.5m, "Micronutrient", "0.1", 8, "ppm", 0 },
                    { 9, 0, "S-MN", null, null, "Avoid further manganese application", "High", true, "Apply manganese micronutrient", "Low", null, "Manganese", "Manganese is adequate", "Medium", "2.0 - 4.0", null, 4.0m, 2.0m, "Micronutrient", "0.1", 9, "ppm", 0 },
                    { 10, 0, "S-CU", null, null, "Avoid further copper application", "High", true, "Apply copper micronutrient", "Low", null, "Copper", "Copper is adequate", "Medium", "0.2 - 0.5", null, 0.5m, 0.2m, "Micronutrient", "0.01", 10, "ppm", 0 },
                    { 11, 0, "S-B", null, null, "Avoid further boron application", "High", true, "Apply borax", "Low", null, "Boron", "Boron is adequate", "Medium", "0.5 - 1.0", null, 1.0m, 0.5m, "Micronutrient", "0.01", 11, "ppm", 0 },
                    { 12, 0, "S-S", null, null, "Avoid further sulphur application", "High", true, "Apply sulphur (gypsum)", "Low", null, "Sulphur", "Sulphur is adequate", "Medium", "10 - 20", null, 20m, 10m, "Fertilizer", "0.1", 12, "ppm", 0 },
                    { 13, 1, "W-PH", null, null, "Water is alkaline; treat before use", "Alkaline", true, "Water is acidic; neutralise before use", "Acidic", null, "pH", "Suitable for irrigation", "Neutral", "6.5 - 8.5", null, 8.5m, 6.5m, "General", "0.1", 1, "-", 0 },
                    { 14, 1, "W-EC", null, null, "Saline water; blend with fresh water", "Saline", true, "No salinity issue", "Safe", null, "EC", "No salinity issue", "Safe", "0 - 0.75", null, 0.75m, 0m, "General", "0.01", 2, "dS/m", 0 },
                    { 15, 1, "W-TDS", null, null, "High dissolved solids; use with caution", "High", true, "No issue", "Safe", null, "Total Dissolved Solids", "No issue", "Safe", "0 - 500", null, 500m, 0m, "General", "1", 3, "mg/L", 0 },
                    { 16, 1, "W-CL", null, null, "Chloride is high; avoid on sensitive crops", "High", true, "No issue", "Safe", null, "Chloride", "No issue", "Safe", "0 - 4", null, 4m, 0m, "General", "0.1", 4, "meq/L", 0 },
                    { 17, 1, "W-SO4", null, null, "Sulphate is high", "High", true, "No issue", "Safe", null, "Sulphate", "No issue", "Safe", "0 - 4", null, 4m, 0m, "General", "0.1", 5, "meq/L", 0 },
                    { 18, 1, "W-CO3", null, null, "Carbonate is high; apply gypsum", "High", true, "No issue", "Safe", null, "Carbonate", "No issue", "Safe", "0 - 0.5", null, 0.5m, 0m, "General", "0.01", 6, "meq/L", 0 },
                    { 19, 1, "W-HCO3", null, null, "Bicarbonate is high; apply gypsum", "High", true, "No issue", "Safe", null, "Bicarbonate", "No issue", "Safe", "0 - 2.5", null, 2.5m, 0m, "General", "0.1", 7, "meq/L", 0 },
                    { 20, 1, "W-NA", null, null, "Sodium is high; risk of sodicity", "High", true, "No issue", "Safe", null, "Sodium", "No issue", "Safe", "0 - 3", null, 3m, 0m, "General", "0.1", 8, "meq/L", 0 },
                    { 21, 1, "W-CA", null, null, "Calcium is high", "High", true, "Calcium is low", "Low", null, "Calcium", "Calcium is adequate", "Medium", "1 - 5", null, 5m, 1m, "General", "0.1", 9, "meq/L", 0 },
                    { 22, 1, "W-MG", null, null, "Magnesium is high", "High", true, "Magnesium is low", "Low", null, "Magnesium", "Magnesium is adequate", "Medium", "0.5 - 3", null, 3m, 0.5m, "General", "0.1", 10, "meq/L", 0 },
                    { 23, 1, "W-SAR", null, null, "Sodicity hazard; apply gypsum", "High", true, "No sodicity hazard", "Safe", null, "Sodium Adsorption Ratio", "No sodicity hazard", "Safe", "0 - 10", null, 10m, 0m, "General", "0.1", 11, "-", 0 },
                    { 24, 1, "W-RSC", null, null, "Unsuitable without gypsum treatment", "High", true, "Safe for irrigation", "Safe", null, "Residual Sodium Carbonate", "Safe for irrigation", "Safe", "0 - 1.25", null, 1.25m, 0m, "General", "0.01", 12, "meq/L", 0 },
                    { 25, 1, "W-NO3", null, null, "Nitrate is high", "High", true, "No issue", "Safe", null, "Nitrate", "No issue", "Safe", "0 - 10", null, 10m, 0m, "General", "0.1", 13, "mg/L", 0 },
                    { 26, 1, "W-MB", null, null, "Microbial contamination; disinfect before use", "High", true, "No contamination", "Safe", null, "Total Coliforms", "No contamination", "Safe", "0 - 1", null, 1m, 0m, "General", "1", 14, "CFU/100mL", 0 },
                    { 27, 0, "S-TEX", null, null, null, "-", true, null, "-", null, "Texture", null, "-", null, "Sandy|Loamy Sand|Sandy Loam|Loam|Silt Loam|Clay Loam|Sandy Clay|Silty Clay|Clay|Sandy Clay Silt", null, null, "General", null, 0, null, 1 },
                    { 28, 0, "S-OM", 1.724m, "S-OC", "Organic matter is high; no addition needed", "High", true, "Add organic manure / compost", "Low", null, "Organic Matter", "Maintain organic matter", "Moderate", "0.87 - 1.29", null, 1.29m, 0.87m, "Organic", "0.01", 3, "%", 0 }
                });

            // Page catalogue rows: fixed id when free, otherwise a generated id (a page registered through
            // Page Management on the live site may already hold that id); never fails, never touches existing
            // rows. Same pattern as V5l / V5n. All lookups are by Key.
            migrationBuilder.Sql(@"
-- LabDashboard
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (74, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabDashboard', 'SAS Lab', 'Lab Dashboard', 73, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabDashboard', 'SAS Lab', 'Lab Dashboard', 73, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'LabDashboard');
-- LabConsignments
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (75, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabConsignments', 'SAS Lab', 'Consignment Details', 74, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabConsignments', 'SAS Lab', 'Consignment Details', 74, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'LabConsignments');
-- LabAnalysis
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (76, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabAnalysis', 'SAS Lab', 'Analysis Tracking', 75, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabAnalysis', 'SAS Lab', 'Analysis Tracking', 75, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'LabAnalysis');
-- LabReports
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (77, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabReports', 'SAS Lab', 'Lab Reports', 76, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabReports', 'SAS Lab', 'Lab Reports', 76, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'LabReports');
-- LabTestEntry
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (78, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabTestEntry', 'SAS Lab', 'Test Value Entry', 77, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabTestEntry', 'SAS Lab', 'Test Value Entry', 77, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'LabTestEntry');
-- LabTracking
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (79, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabTracking', 'SAS Lab', 'Lab Tracking', 78, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'LabTracking', 'SAS Lab', 'Lab Tracking', 78, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'LabTracking');
-- SasPaymentApproval
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (80, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SasPaymentApproval', 'SAS', 'Payment Approval', 79, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SasPaymentApproval', 'SAS', 'Payment Approval', 79, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'SasPaymentApproval');
-- SasPaymentVerification
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (81, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SasPaymentVerification', 'SAS', 'Payment Verification', 80, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SasPaymentVerification', 'SAS', 'Payment Verification', 80, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'SasPaymentVerification');
SELECT setval(pg_get_serial_sequence('""Pages""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""Pages"") + 1, nextval(pg_get_serial_sequence('""Pages""', 'Id'))), false);
");

            migrationBuilder.CreateIndex(
                name: "IX_SamplePayments_Code",
                table: "SamplePayments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SamplePayments_FinanceStatus",
                table: "SamplePayments",
                column: "FinanceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SampleLabResults_LabParameterId",
                table: "SampleLabResults",
                column: "LabParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleItems_AnalysisStatus",
                table: "SampleItems",
                column: "AnalysisStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SampleConsignments_BatchId",
                table: "SampleConsignments",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LabActivities_BatchId_At",
                table: "LabActivities",
                columns: new[] { "BatchId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_LabActivities_Kind",
                table: "LabActivities",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_LabCropRecommendations_Crop_Stage_SortOrder",
                table: "LabCropRecommendations",
                columns: new[] { "Crop", "Stage", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LabDocuments_BatchId",
                table: "LabDocuments",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LabDocuments_Kind",
                table: "LabDocuments",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_LabParameters_AppliesTo_SortOrder",
                table: "LabParameters",
                columns: new[] { "AppliesTo", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LabParameters_Code",
                table: "LabParameters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_BatchId",
                table: "LabReports",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_Code",
                table: "LabReports",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_FinancialYearStart",
                table: "LabReports",
                column: "FinancialYearStart");

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_GeneratedAt",
                table: "LabReports",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabReports_SampleItemId_SampleType",
                table: "LabReports",
                columns: new[] { "SampleItemId", "SampleType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTranslations_Key_Lang",
                table: "LabTranslations",
                columns: new[] { "Key", "Lang" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleBatches_AssignedToUserId",
                table: "SampleBatches",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleBatches_BatchDate",
                table: "SampleBatches",
                column: "BatchDate");

            migrationBuilder.CreateIndex(
                name: "IX_SampleBatches_Code",
                table: "SampleBatches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleBatches_ReportCode",
                table: "SampleBatches",
                column: "ReportCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleBatches_Status",
                table: "SampleBatches",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_SampleConsignments_SampleBatches_BatchId",
                table: "SampleConsignments",
                column: "BatchId",
                principalTable: "SampleBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""Pages"" WHERE ""Key"" IN ('LabDashboard', 'LabConsignments', 'LabAnalysis', 'LabReports', 'LabTestEntry', 'LabTracking', 'SasPaymentApproval', 'SasPaymentVerification');");

            migrationBuilder.DropForeignKey(
                name: "FK_SampleConsignments_SampleBatches_BatchId",
                table: "SampleConsignments");

            migrationBuilder.DropTable(
                name: "LabActivities");

            migrationBuilder.DropTable(
                name: "LabCropRecommendations");

            migrationBuilder.DropTable(
                name: "LabDocuments");

            migrationBuilder.DropTable(
                name: "LabParameters");

            migrationBuilder.DropTable(
                name: "LabReports");

            migrationBuilder.DropTable(
                name: "LabTranslations");

            migrationBuilder.DropTable(
                name: "SampleBatches");

            migrationBuilder.DropIndex(
                name: "IX_SamplePayments_Code",
                table: "SamplePayments");

            migrationBuilder.DropIndex(
                name: "IX_SamplePayments_FinanceStatus",
                table: "SamplePayments");

            migrationBuilder.DropIndex(
                name: "IX_SampleLabResults_LabParameterId",
                table: "SampleLabResults");

            migrationBuilder.DropIndex(
                name: "IX_SampleItems_AnalysisStatus",
                table: "SampleItems");

            migrationBuilder.DropIndex(
                name: "IX_SampleConsignments_BatchId",
                table: "SampleConsignments");

            migrationBuilder.DropColumn(
                name: "AdminRemarks",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "FinanceRemarks",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "FinanceStatus",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "FinanceVerifiedAt",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "FinanceVerifiedByName",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "ForwardedAt",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "ForwardedToName",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "MoRemarks",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "VerifiedAmount",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "LabParameterId",
                table: "SampleLabResults");

            migrationBuilder.DropColumn(
                name: "AnalysisCompletedAt",
                table: "SampleItems");

            migrationBuilder.DropColumn(
                name: "AnalysisStartedAt",
                table: "SampleItems");

            migrationBuilder.DropColumn(
                name: "AnalysisStatus",
                table: "SampleItems");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "SampleConsignments");
        }
    }
}
