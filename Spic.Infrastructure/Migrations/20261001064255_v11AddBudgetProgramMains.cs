using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v11AddBudgetProgramMains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "FinancialYear",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "BudgetPrograms");

            migrationBuilder.AddColumn<int>(
                name: "BudgetProgramMains",
                table: "BudgetPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BudgetProgramMainsId",
                table: "BudgetPrograms",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BudgetProgramMains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FinancialYear = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ValidateBy = table.Column<string>(type: "text", nullable: false),
                    ValidateAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SIDAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetProgramMains", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPrograms_BudgetProgramMains_BudgetProgramMainsId",
                table: "BudgetPrograms");

            migrationBuilder.DropTable(
                name: "BudgetProgramMains");

            migrationBuilder.DropIndex(
                name: "IX_BudgetPrograms_BudgetProgramMainsId",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "BudgetProgramMains",
                table: "BudgetPrograms");

            migrationBuilder.DropColumn(
                name: "BudgetProgramMainsId",
                table: "BudgetPrograms");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BudgetPrograms",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "BudgetPrograms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinancialYear",
                table: "BudgetPrograms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BudgetPrograms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "BudgetPrograms",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "BudgetPrograms",
                type: "text",
                nullable: true);
        }
    }
}
