using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3d_ifmsproductupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IfmsProductId",
                table: "WholesalerStockAsOnTodays",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsProductId",
                table: "WarehouseDistrictGlobalStockReconciliations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsProductId",
                table: "StateGlobalStockReconciliations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsProductId",
                table: "SalesWholesalers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsProductId",
                table: "SalesCompanySales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsProductId",
                table: "SalesAndReceipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IfmsProductId",
                table: "DptReports",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IfmsProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsProducts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IfmsProducts");

            migrationBuilder.DropColumn(
                name: "IfmsProductId",
                table: "WholesalerStockAsOnTodays");

            migrationBuilder.DropColumn(
                name: "IfmsProductId",
                table: "WarehouseDistrictGlobalStockReconciliations");

            migrationBuilder.DropColumn(
                name: "IfmsProductId",
                table: "StateGlobalStockReconciliations");

            migrationBuilder.DropColumn(
                name: "IfmsProductId",
                table: "SalesWholesalers");

            migrationBuilder.DropColumn(
                name: "IfmsProductId",
                table: "SalesCompanySales");

            migrationBuilder.DropColumn(
                name: "IfmsProductId",
                table: "SalesAndReceipts");

            migrationBuilder.DropColumn(
                name: "IfmsProductId",
                table: "DptReports");
        }
    }
}
