using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V42_FarmerPortalHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyAcceptedAt",
                table: "FarmerProfiles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyVersion",
                table: "FarmerProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Dose", "Note", "Product", "Source", "SourceUrl" },
                values: new object[] { "0.5 % spray: 5 g manganese sulphate per litre of water", "Spray every two weeks until the new leaves are green.", "Manganese sulphate (MnSO4)", "TNAU Agritech: Mineral nutrition, Groundnut manganese deficiency (correction measure)", "https://agritech.tnau.ac.in/agriculture/plant_nutri/gnut_mangan.html" });

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 22,
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 31,
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 32,
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 61,
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 62,
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 70,
                column: "Dose",
                value: "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %: 5 g + 2 g + 2 g + 1 g per litre");

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 71,
                column: "Dose",
                value: "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %: 5 g + 2 g + 2 g + 1 g per litre");

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 72,
                column: "Dose",
                value: "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %: 5 g + 2 g + 2 g + 1 g per litre");

            migrationBuilder.InsertData(
                table: "NutrientCorrectives",
                columns: new[] { "Id", "Crop", "Dose", "IsActive", "Method", "Note", "Nutrient", "Product", "SortOrder", "Source", "SourceUrl" },
                values: new object[] { 18, null, "5-10 kg manganese sulphate per hectare (about 2-4 kg per acre)", true, "Soil", null, "Mn", "Manganese sulphate (MnSO4)", 2, "TNAU Agritech: Mineral nutrition, Groundnut manganese deficiency (correction measure)", "https://agritech.tnau.ac.in/agriculture/plant_nutri/gnut_mangan.html" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DropColumn(
                name: "PrivacyAcceptedAt",
                table: "FarmerProfiles");

            migrationBuilder.DropColumn(
                name: "PrivacyVersion",
                table: "FarmerProfiles");

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Dose", "Note", "Product", "Source", "SourceUrl" },
                values: new object[] { "1-5 kg Mn per hectare in about 200 litres of water", "On high-pH soil use ammonium sulphate instead of urea.", "Manganese sulphate", "TNAU Agritech Expert System for Paddy: nutritional disorders and corrective measures", "https://agritech.tnau.ac.in/expert_system/paddy/nutrientmanagement.html" });

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 22,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 31,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 32,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 61,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 62,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 70,
                column: "Dose",
                value: "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %");

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 71,
                column: "Dose",
                value: "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %");

            migrationBuilder.UpdateData(
                table: "NutrientCorrectives",
                keyColumn: "Id",
                keyValue: 72,
                column: "Dose",
                value: "Zinc sulphate 0.5 % + ferrous sulphate 0.2 % + copper sulphate 0.2 % + boric acid 0.1 %");
        }
    }
}
