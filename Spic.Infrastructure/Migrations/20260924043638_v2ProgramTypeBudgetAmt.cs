using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v2ProgramTypeBudgetAmt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetAmount",
                table: "ProgramTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsChangeAmount",
                table: "ProgramTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetAmount",
                table: "ProgramTypes");

            migrationBuilder.DropColumn(
                name: "IsChangeAmount",
                table: "ProgramTypes");
        }
    }
}
