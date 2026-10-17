using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStartPlaceName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosLiquidationStartPlaceName",
                table: "JmdoTaskAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoilSampleStartPlaceName",
                table: "JmdoTaskAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpcmStartPlaceName",
                table: "JmdoTaskAllocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartPlaceName",
                table: "JmdoTaskAllocationPrograms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartPlaceName",
                table: "JmdoTaskAllocationDealers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosLiquidationStartPlaceName",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SoilSampleStartPlaceName",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "SpcmStartPlaceName",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropColumn(
                name: "StartPlaceName",
                table: "JmdoTaskAllocationPrograms");

            migrationBuilder.DropColumn(
                name: "StartPlaceName",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
