using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3k_warehouseupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandFWarehouses");

            migrationBuilder.AddColumn<decimal>(
                name: "GflAdditionalReservationQuantityLitres",
                table: "Warehouses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GflReservationQuantityMT",
                table: "Warehouses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseCategory",
                table: "Warehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PVTMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PVTMasters", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PVTMasters");

            migrationBuilder.DropColumn(
                name: "GflAdditionalReservationQuantityLitres",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "GflReservationQuantityMT",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WarehouseCategory",
                table: "Warehouses");

            migrationBuilder.CreateTable(
                name: "CandFWarehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BasicStateId = table.Column<int>(type: "integer", nullable: true),
                    Block = table.Column<string>(type: "text", nullable: true),
                    ContactNumber = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    DoorNo = table.Column<string>(type: "text", nullable: true),
                    GflAdditionalReservationQuantityLitres = table.Column<decimal>(type: "numeric", nullable: true),
                    GflReservationQuantityMT = table.Column<decimal>(type: "numeric", nullable: true),
                    GoogleURL = table.Column<string>(type: "text", nullable: true),
                    HeadquarterId = table.Column<int>(type: "integer", nullable: true),
                    InGreenStar = table.Column<bool>(type: "boolean", nullable: true),
                    InSpic = table.Column<bool>(type: "boolean", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OperatedBy = table.Column<int>(type: "integer", nullable: true),
                    OtherDocumentPathsJson = table.Column<string>(type: "text", nullable: true),
                    PinCode = table.Column<string>(type: "text", nullable: true),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    Street = table.Column<string>(type: "text", nullable: true),
                    SubVillage = table.Column<string>(type: "text", nullable: true),
                    Taluk = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    Village = table.Column<string>(type: "text", nullable: true),
                    WarehouseCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandFWarehouses", x => x.Id);
                });
        }
    }
}
