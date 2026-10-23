using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFYIdinAnnualBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FY",
                table: "AnnualBudgetings");

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearId",
                table: "AnnualBudgetings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AnnualBudgetings_FinancialYearId",
                table: "AnnualBudgetings",
                column: "FinancialYearId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AnnualBudgetings_FinancialYears_FinancialYearId",
                table: "AnnualBudgetings",
                column: "FinancialYearId",
                principalTable: "FinancialYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnnualBudgetings_FinancialYears_FinancialYearId",
                table: "AnnualBudgetings");

            migrationBuilder.DropIndex(
                name: "IX_AnnualBudgetings_FinancialYearId",
                table: "AnnualBudgetings");

            migrationBuilder.DropColumn(
                name: "FinancialYearId",
                table: "AnnualBudgetings");

            migrationBuilder.AddColumn<string>(
                name: "FY",
                table: "AnnualBudgetings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
