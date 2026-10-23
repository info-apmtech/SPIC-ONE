using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFYIDinBudgetProgramHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocatedAmount",
                table: "BudgetProgramHistory");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "BudgetProgramHistory");

            migrationBuilder.DropColumn(
                name: "FinancialYearId",
                table: "BudgetProgramHistory");

            migrationBuilder.DropColumn(
                name: "HeadQuartersId",
                table: "BudgetProgramHistory");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "BudgetProgramHistory");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "BudgetProgramHistory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedAmount",
                table: "BudgetProgramHistory",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "BudgetProgramHistory",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearId",
                table: "BudgetProgramHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeadQuartersId",
                table: "BudgetProgramHistory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "BudgetProgramHistory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "BudgetProgramHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
