using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3_creditlimitsalesdataupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubGroupId",
                table: "DealerCreditLimitSalesData",
                newName: "ProductId");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "DealerCreditLimitSalesData",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "DealerCreditLimitSalesData");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "DealerCreditLimitSalesData",
                newName: "SubGroupId");
        }
    }
}
