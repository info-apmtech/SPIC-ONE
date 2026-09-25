using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4BudgetProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AprilCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AugustCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DecemberCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FebruaryCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "JanuaryCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "JulyCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "JuneCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarchCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MayCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NovemberCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OctoberCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SeptemberCount",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AprilCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "AugustCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "DecemberCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "FebruaryCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "JanuaryCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "JulyCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "JuneCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "MarchCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "MayCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "NovemberCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "OctoberCount",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "SeptemberCount",
                table: "BudgetPrograms");
        }
    }
}
