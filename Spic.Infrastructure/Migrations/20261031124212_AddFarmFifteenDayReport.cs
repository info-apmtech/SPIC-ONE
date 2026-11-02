using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmFifteenDayReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FarmFifteenDayReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportingPeriod = table.Column<string>(type: "text", nullable: false),
                    FinancialYear = table.Column<string>(type: "text", nullable: false),
                    PeriodStartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedBy = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TotalIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalExpense = table.Column<decimal>(type: "numeric", nullable: false),
                    NetProfit = table.Column<decimal>(type: "numeric", nullable: false),
                    InventoryRemarks = table.Column<string>(type: "text", nullable: true),
                    IncomeRemarks = table.Column<string>(type: "text", nullable: true),
                    ExpenseRemarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmFifteenDayReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmFifteenDayExpenseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsSubCategory = table.Column<bool>(type: "boolean", nullable: false),
                    IsAutoCalculated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmFifteenDayExpenseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmFifteenDayExpenseItems_FarmFifteenDayReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "FarmFifteenDayReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmFifteenDayIncomeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportId = table.Column<int>(type: "integer", nullable: false),
                    Group = table.Column<string>(type: "text", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    IconKey = table.Column<string>(type: "text", nullable: true),
                    SoldQuantity = table.Column<int>(type: "integer", nullable: false),
                    Uom = table.Column<string>(type: "text", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmFifteenDayIncomeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmFifteenDayIncomeItems_FarmFifteenDayReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "FarmFifteenDayReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FarmFifteenDayInventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    IconKey = table.Column<string>(type: "text", nullable: true),
                    Opening = table.Column<int>(type: "integer", nullable: false),
                    Purchase = table.Column<int>(type: "integer", nullable: false),
                    Hatched = table.Column<int>(type: "integer", nullable: false),
                    Sold = table.Column<int>(type: "integer", nullable: false),
                    Mortality = table.Column<int>(type: "integer", nullable: false),
                    Others = table.Column<int>(type: "integer", nullable: false),
                    Closing = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmFifteenDayInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmFifteenDayInventoryItems_FarmFifteenDayReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "FarmFifteenDayReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmFifteenDayExpenseItems_ReportId",
                table: "FarmFifteenDayExpenseItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmFifteenDayIncomeItems_ReportId",
                table: "FarmFifteenDayIncomeItems",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmFifteenDayInventoryItems_ReportId",
                table: "FarmFifteenDayInventoryItems",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmFifteenDayExpenseItems");

            migrationBuilder.DropTable(
                name: "FarmFifteenDayIncomeItems");

            migrationBuilder.DropTable(
                name: "FarmFifteenDayInventoryItems");

            migrationBuilder.DropTable(
                name: "FarmFifteenDayReports");
        }
    }
}
