using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalesAndReceiptSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Company",
                table: "SalesAndReceipts");

            migrationBuilder.DropColumn(
                name: "DealerId",
                table: "SalesAndReceipts");

            migrationBuilder.DropColumn(
                name: "DealerNature",
                table: "SalesAndReceipts");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesAndReceipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DealershipNatureId",
                table: "SalesAndReceipts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesAndReceipts");

            migrationBuilder.DropColumn(
                name: "DealershipNatureId",
                table: "SalesAndReceipts");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "SalesAndReceipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealerId",
                table: "SalesAndReceipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealerNature",
                table: "SalesAndReceipts",
                type: "text",
                nullable: true);
        }
    }
}
