using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3PVTMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetAmount",
                table: "ProgramTypes");

            migrationBuilder.AddColumn<bool>(
                name: "IsSpecialityProduct",
                table: "PVTMasters",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetAmount",
                table: "ProgramMasters",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSpecialityProduct",
                table: "PVTMasters");

            migrationBuilder.DropColumn(
                name: "BudgetAmount",
                table: "ProgramMasters");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetAmount",
                table: "ProgramTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
