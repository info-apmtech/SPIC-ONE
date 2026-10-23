using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFYIdinBudgetProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "April",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "August",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "December",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "February",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "January",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "July",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "June",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "March",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "May",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "November",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "October",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "September",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "FinancialYear",
                table: "BudgetProgramMains");

            migrationBuilder.DropColumn(
                name: "SIDAmount",
                table: "BudgetProgramMains");

            migrationBuilder.RenameColumn(
                name: "TotalBudget",
                table: "BudgetPrograms",
                newName: "ProgramBudget");

            // BudgetProgramStatus is stored as integer.
            // BudgetProgramMains is currently empty, so recreating the column is safe.
            migrationBuilder.DropColumn(
                name: "Status",
                table: "BudgetProgramMains");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BudgetProgramMains",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearId",
                table: "BudgetProgramMains",
                type: "integer",
                nullable: false);

            migrationBuilder.CreateTable(
                name: "BudgetProgramHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetProgramMainId = table.Column<int>(type: "integer", nullable: false),
                    BudgetProgramId = table.Column<int>(type: "integer", nullable: true),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    FinancialYearId = table.Column<int>(type: "integer", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    HeadQuartersId = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ProgramBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    AprilCount = table.Column<decimal>(type: "numeric", nullable: false),
                    MayCount = table.Column<decimal>(type: "numeric", nullable: false),
                    JuneCount = table.Column<decimal>(type: "numeric", nullable: false),
                    JulyCount = table.Column<decimal>(type: "numeric", nullable: false),
                    AugustCount = table.Column<decimal>(type: "numeric", nullable: false),
                    SeptemberCount = table.Column<decimal>(type: "numeric", nullable: false),
                    OctoberCount = table.Column<decimal>(type: "numeric", nullable: false),
                    NovemberCount = table.Column<decimal>(type: "numeric", nullable: false),
                    DecemberCount = table.Column<decimal>(type: "numeric", nullable: false),
                    JanuaryCount = table.Column<decimal>(type: "numeric", nullable: false),
                    FebruaryCount = table.Column<decimal>(type: "numeric", nullable: false),
                    MarchCount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActionBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ActionAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetProgramHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetProgramHistory_BudgetProgramMains_BudgetProgramMainId",
                        column: x => x.BudgetProgramMainId,
                        principalTable: "BudgetProgramMains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetProgramMains_FinancialYearId",
                table: "BudgetProgramMains",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetProgramHistory_BudgetProgramMainId",
                table: "BudgetProgramHistory",
                column: "BudgetProgramMainId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetProgramMains_FinancialYears_FinancialYearId",
                table: "BudgetProgramMains",
                column: "FinancialYearId",
                principalTable: "FinancialYears",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetProgramMains_FinancialYears_FinancialYearId",
                table: "BudgetProgramMains");

            migrationBuilder.DropTable(
                name: "BudgetProgramHistory");

            migrationBuilder.DropIndex(
                name: "IX_BudgetProgramMains_FinancialYearId",
                table: "BudgetProgramMains");

            migrationBuilder.DropColumn(
                name: "FinancialYearId",
                table: "BudgetProgramMains");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BudgetProgramMains");

            migrationBuilder.RenameColumn(
                name: "ProgramBudget",
                table: "BudgetPrograms",
                newName: "TotalBudget");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BudgetProgramMains",
                type: "text",
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<decimal>(
                name: "April",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "August",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "December",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "February",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "January",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "July",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "June",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "March",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "May",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "November",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "October",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "September",
                table: "BudgetPrograms",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FinancialYear",
                table: "BudgetProgramMains",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SIDAmount",
                table: "BudgetProgramMains",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
