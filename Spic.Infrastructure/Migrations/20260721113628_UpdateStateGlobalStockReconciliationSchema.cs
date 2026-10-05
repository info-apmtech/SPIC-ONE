using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStateGlobalStockReconciliationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesGlobalStockReconciliations");

            migrationBuilder.DropTable(
                name: "StateWiseGlobalStockReconciliations");

            migrationBuilder.CreateTable(
                name: "StateGlobalStockReconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_StateGlobalStockReconciliations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StateGlobalStockReconciliations");

            migrationBuilder.CreateTable(
                name: "SalesGlobalStockReconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClosingGIT = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingStock = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Dispatches = table.Column<decimal>(type: "numeric", nullable: false),
                    OpeningGIT = table.Column<decimal>(type: "numeric", nullable: false),
                    OpeningStock = table.Column<decimal>(type: "numeric", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductionImports = table.Column<decimal>(type: "numeric", nullable: false),
                    Receipt = table.Column<decimal>(type: "numeric", nullable: false),
                    Sales = table.Column<decimal>(type: "numeric", nullable: false),
                    SalesReturn = table.Column<decimal>(type: "numeric", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    StateName = table.Column<string>(type: "text", nullable: true),
                    StockAdjustment = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGlobalStockReconciliations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StateWiseGlobalStockReconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClosingGIT = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingStock = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Dispatches = table.Column<decimal>(type: "numeric", nullable: false),
                    OpeningGIT = table.Column<decimal>(type: "numeric", nullable: false),
                    OpeningStock = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductionImports = table.Column<decimal>(type: "numeric", nullable: false),
                    Receipt = table.Column<decimal>(type: "numeric", nullable: false),
                    Sales = table.Column<decimal>(type: "numeric", nullable: false),
                    SalesReturn = table.Column<decimal>(type: "numeric", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    StockAdjustment = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateWiseGlobalStockReconciliations", x => x.Id);
                });
        }
    }
}
