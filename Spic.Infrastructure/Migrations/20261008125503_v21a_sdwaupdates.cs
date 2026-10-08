using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v21a_sdwaupdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "GuestHouses",
                type: "integer",
                nullable: true);

            //migrationBuilder.InsertData(
            //    table: "Pages",
            //    columns: new[] { "Id", "CreatedAt", "CreatedBy", "HasActions", "IsActive", "Key", "Module", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
            //    values: new object[,]
            //    {
            //        { 125, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Activities", null, "Activities", 124, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 126, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Farmers", null, "Farmers", 125, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 127, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Alerts", null, "Alerts", 126, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 128, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "StartDocumentation", null, "Start Documentation", 127, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 129, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "DemoDetails", null, "Demo Details", 128, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 130, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "TreatmentDetails", null, "Treatment Details", 129, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 131, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Treatment01", null, "Treatment01", 130, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 132, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "Treatment02", null, "Treatment02", 131, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 133, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "TreatmentDemoDetails", null, "Treatment Demo Details", 132, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" },
            //        { 134, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "GHAdmin", null, "GHAdmin", 133, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" }
            //    });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 125);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 126);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 127);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 128);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 129);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 130);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 131);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 132);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 133);

            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 134);

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "GuestHouses");
        }
    }
}
