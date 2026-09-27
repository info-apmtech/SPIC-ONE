using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5q_LabCropRecommendationNutrient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nutrient",
                table: "LabCropRecommendations",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 1,
                column: "Nutrient",
                value: "Organic");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 2,
                column: "Nutrient",
                value: "Gypsum");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 3,
                column: "Nutrient",
                value: "Organic");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 4,
                column: "Nutrient",
                value: "N,P");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 5,
                column: "Nutrient",
                value: "N");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 6,
                column: "Nutrient",
                value: "K");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 7,
                column: "Nutrient",
                value: "Zn");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 8,
                column: "Nutrient",
                value: "Fe");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 9,
                column: "Nutrient",
                value: "Mn");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 10,
                column: "Nutrient",
                value: "Cu");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 11,
                column: "Nutrient",
                value: "N,P");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 12,
                column: "Nutrient",
                value: "N");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 13,
                column: "Nutrient",
                value: "K");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 14,
                column: "Nutrient",
                value: "N");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 15,
                column: "Nutrient",
                value: "K");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 16,
                column: "Nutrient",
                value: "N");

            migrationBuilder.UpdateData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 17,
                column: "Nutrient",
                value: "K");

            migrationBuilder.InsertData(
                table: "LabCropRecommendations",
                columns: new[] { "Id", "Crop", "DayNumber", "IsActive", "KgPerAcre", "Nutrient", "Product", "SortOrder", "Stage" },
                values: new object[,]
                {
                    { 18, "General", null, true, 300m, "Organic", "SPIC Jyoti", 1, 0 },
                    { 19, "General", null, true, 200m, "Gypsum", "SPIC Gypsum", 2, 0 },
                    { 20, "General", null, true, 100m, "Organic", "SPIC Sangamam", 3, 0 },
                    { 21, "General", null, true, 0m, "N,P", "SPIC DAP", 4, 0 },
                    { 22, "General", null, true, 0m, "N", "SPIC Urea", 5, 0 },
                    { 23, "General", null, true, 0m, "K", "Potash", 6, 0 },
                    { 24, "General", null, true, 12m, "Zn", "SPIC Zinc Sulphate", 7, 0 },
                    { 25, "General", null, true, 0m, "Fe", "Ferrous Sulphate", 8, 0 },
                    { 26, "General", null, true, 0m, "Mn", "Manganese Sulphate", 9, 0 },
                    { 27, "General", null, true, 5m, "Cu", "Copper Sulphate", 10, 0 },
                    { 28, "General", 90, true, 82.5m, "N,P", "SPIC DAP", 1, 1 },
                    { 29, "General", 90, true, 0m, "N", "SPIC Urea", 2, 1 },
                    { 30, "General", 90, true, 150m, "K", "Potash", 3, 1 },
                    { 31, "General", 150, true, 125m, "N", "SPIC Urea", 1, 2 },
                    { 32, "General", 150, true, 112.5m, "K", "Potash", 2, 2 },
                    { 33, "General", 210, true, 125m, "N", "SPIC Urea", 1, 3 },
                    { 34, "General", 210, true, 112.5m, "K", "Potash", 2, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "LabCropRecommendations",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DropColumn(
                name: "Nutrient",
                table: "LabCropRecommendations");
        }
    }
}
