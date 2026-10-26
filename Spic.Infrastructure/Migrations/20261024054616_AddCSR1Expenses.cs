using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCSR1Expenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Compliments",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Inputs",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "JeepRunningExpenses",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Others",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Photography",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "PrintingAndStationery",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "PublicityMaterial",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Refreshments",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "ServiceChargesLCA",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "StageArrangements",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "TransportRent",
                table: "CSR1");

            migrationBuilder.CreateTable(
                name: "CSR1Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR1Id = table.Column<int>(type: "integer", nullable: false),
                    ExpenseId = table.Column<int>(type: "integer", nullable: true),
                    ExpenseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR1Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR1Expenses_CSR1_CSR1Id",
                        column: x => x.CSR1Id,
                        principalTable: "CSR1",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CSR1Expenses_CSR1Id",
                table: "CSR1Expenses",
                column: "CSR1Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CSR1Expenses");

            migrationBuilder.AddColumn<decimal>(
                name: "Compliments",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Inputs",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "JeepRunningExpenses",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Others",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Photography",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrintingAndStationery",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PublicityMaterial",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Refreshments",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargesLCA",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StageArrangements",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportRent",
                table: "CSR1",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
