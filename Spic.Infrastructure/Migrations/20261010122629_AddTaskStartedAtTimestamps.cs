using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStartedAtTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PosLiquidationStartedAt",
                table: "JmdoTaskAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SoilSampleStartedAt",
                table: "JmdoTaskAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpcmStartedAt",
                table: "JmdoTaskAllocations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "JmdoTaskAllocationPrograms",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "JmdoTaskAllocationDealers",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosLiquidationStartedAt",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SoilSampleStartedAt",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SpcmStartedAt",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "JmdoTaskAllocationPrograms");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
