using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v23Add3LevelApprovalCSR1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Level1ApprovedAt",
                table: "CSR1",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level1ApprovedBy",
                table: "CSR1",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Level2ApprovedAt",
                table: "CSR1",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level2ApprovedBy",
                table: "CSR1",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Level3ApprovedAt",
                table: "CSR1",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level3ApprovedBy",
                table: "CSR1",
                type: "text",
                nullable: true);

            //migrationBuilder.InsertData(
            //    table: "Pages",
            //    columns: new[] { "Id", "CreatedAt", "CreatedBy", "HasActions", "IsActive", "Key", "Module", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
            //    values: new object[] { 125, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", true, true, "RMDHQMapping", null, "RMDHQMapping", 124, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DeleteData(
            //    table: "Pages",
            //    keyColumn: "Id",
            //    keyValue: 125);

            migrationBuilder.DropColumn(
                name: "Level1ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level1ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedBy",
                table: "CSR1");
        }
    }
}
