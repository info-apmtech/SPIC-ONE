using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v12RemovedBudgetProgramMainscolumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPrograms_BudgetProgramMains_BudgetProgramMainsId",
                table: "BudgetPrograms");

            migrationBuilder.DropIndex(
                name: "IX_BudgetPrograms_BudgetProgramMainsId",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "BudgetProgramMainsId",
                table: "BudgetPrograms");

            migrationBuilder.RenameColumn(
                name: "BudgetProgramMains",
                table: "BudgetPrograms",
                newName: "BudgetProgramMainId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPrograms_BudgetProgramMainId",
                table: "BudgetPrograms",
                column: "BudgetProgramMainId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetPrograms_BudgetProgramMains_BudgetProgramMainId",
                table: "BudgetPrograms",
                column: "BudgetProgramMainId",
                principalTable: "BudgetProgramMains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPrograms_BudgetProgramMains_BudgetProgramMainId",
                table: "BudgetPrograms");

            migrationBuilder.DropIndex(
                name: "IX_BudgetPrograms_BudgetProgramMainId",
                table: "BudgetPrograms");

            migrationBuilder.RenameColumn(
                name: "BudgetProgramMainId",
                table: "BudgetPrograms",
                newName: "BudgetProgramMains");

            migrationBuilder.AddColumn<int>(
                name: "BudgetProgramMainsId",
                table: "BudgetPrograms",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPrograms_BudgetProgramMainsId",
                table: "BudgetPrograms",
                column: "BudgetProgramMainsId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetPrograms_BudgetProgramMains_BudgetProgramMainsId",
                table: "BudgetPrograms",
                column: "BudgetProgramMainsId",
                principalTable: "BudgetProgramMains",
                principalColumn: "Id");
        }
    }
}
