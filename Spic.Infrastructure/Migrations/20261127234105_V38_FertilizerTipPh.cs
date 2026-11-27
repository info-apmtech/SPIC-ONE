using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V38_FertilizerTipPh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PhMax",
                table: "CropSoilTips",
                type: "numeric(4,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PhMin",
                table: "CropSoilTips",
                type: "numeric(4,1)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { 6.5m, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { 6.5m, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { 6.5m, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 41,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { 6.5m, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 42,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { 6.0m, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 43,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 44,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 45,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 46,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { 6.0m, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 47,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 48,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 49,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 50,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 51,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 52,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 53,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 54,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 55,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 56,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 57,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 58,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 59,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 60,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 61,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 62,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 63,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 64,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 65,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 66,
                columns: new[] { "PhMax", "PhMin" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "CropSoilTips",
                columns: new[] { "Id", "Category", "Crop", "IsActive", "PhMax", "PhMin", "SoilType", "SortOrder", "Source", "SourceUrl", "Text", "WaterRegime" },
                values: new object[] { 67, "Soil", null, true, null, 6.5m, "Red Soil", 8, "TNAU Agritech: Soil related constraints and their management", "https://agritech.tnau.ac.in/agriculture/agri_reosurcemgt_soil_soilcontraints.html", "If the surface forms a hard crust after rain, break it by harrowing; gypsum at 2 t/ha with FYM at 12.5 t/ha prevents it.", 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CropSoilTips",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DropColumn(
                name: "PhMax",
                table: "CropSoilTips");

            migrationBuilder.DropColumn(
                name: "PhMin",
                table: "CropSoilTips");
        }
    }
}
