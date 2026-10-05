using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesGlobalStockReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SalesGlobalStockReconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    StateName = table.Column<string>(type: "text", nullable: true),
                    IsPlantRow = table.Column<bool>(type: "boolean", nullable: false),
                    OpeningStock = table.Column<decimal>(type: "numeric", nullable: false),
                    OpeningGIT = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductionImports = table.Column<decimal>(type: "numeric", nullable: false),
                    Receipt = table.Column<decimal>(type: "numeric", nullable: false),
                    Dispatches = table.Column<decimal>(type: "numeric", nullable: false),
                    Sales = table.Column<decimal>(type: "numeric", nullable: false),
                    SalesReturn = table.Column<decimal>(type: "numeric", nullable: false),
                    StockAdjustment = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingGIT = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingStock = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGlobalStockReconciliations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesGlobalStockReconciliations");
        }
    }
}
