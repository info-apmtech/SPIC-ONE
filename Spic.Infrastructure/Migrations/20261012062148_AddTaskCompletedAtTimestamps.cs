using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskCompletedAtTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PosLiquidationCompletedAt",
                table: "JmdoTaskAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoilSampleCompletedAt",
                table: "JmdoTaskAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpcmCompletedAt",
                table: "JmdoTaskAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "JmdoTaskAllocationPrograms",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "JmdoTaskAllocationDealers",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosLiquidationCompletedAt",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SoilSampleCompletedAt",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SpcmCompletedAt",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "JmdoTaskAllocationPrograms");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
