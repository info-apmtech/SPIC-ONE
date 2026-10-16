using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ifmsdealerinexcelupload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IfmsId",
                table: "WholesalerStockAsOnTodays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsDealerSalesId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsWholesalerSalesId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsId",
                table: "SalesCompanySales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsId",
                table: "SalesAndReceipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsId",
                table: "DptReports",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IfmsId",
                table: "WholesalerStockAsOnTodays");

            migrationBuilder.DropColumn(
                name: "IfmsDealerSalesId",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "IfmsWholesalerSalesId",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "IfmsId",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "IfmsId",
                table: "SalesAndReceipts");

            migrationBuilder.DropColumn(
                name: "IfmsId",
                table: "DptReports");
        }
    }
}
