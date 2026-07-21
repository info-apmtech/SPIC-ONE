using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalesCompanySaleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AckThrough",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "DealershipNature",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "Marketer",
                table: "SalesCompanySales");

            migrationBuilder.AddColumn<int>(
                name: "AckThroughId",
                table: "SalesCompanySales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DealershipNatureId",
                table: "SalesCompanySales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManufacturerId",
                table: "SalesCompanySales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketerId",
                table: "SalesCompanySales",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AckThroughId",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "DealershipNatureId",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "MarketerId",
                table: "SalesCompanySales");

            migrationBuilder.AddColumn<string>(
                name: "AckThrough",
                table: "SalesCompanySales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealershipNature",
                table: "SalesCompanySales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "SalesCompanySales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marketer",
                table: "SalesCompanySales",
                type: "text",
                nullable: true);
        }
    }
}
