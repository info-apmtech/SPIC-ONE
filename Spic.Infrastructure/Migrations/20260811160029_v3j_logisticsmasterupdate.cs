using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3j_logisticsmasterupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BasicStateId",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Block",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "Warehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DoorNo",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FertilizerLicenseDocumentPath",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GflAdditionalReservationQuantityMT",
                table: "Warehouses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GflApprovedReservationQuantityMT",
                table: "Warehouses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GstDocumentPath",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeadquarterId",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InGreenStar",
                table: "Warehouses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InSpic",
                table: "Warehouses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceDocumentPath",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Warehouses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Warehouses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperatedBy",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDocumentPathsJson",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinCode",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpicAdditionalReservationQuantityMT",
                table: "Warehouses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpicApprovedReservationQuantityMT",
                table: "Warehouses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "Warehouses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubVillage",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taluk",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Village",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseType",
                table: "Warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BasicStateId",
                table: "RackPoints",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Block",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeadquarterId",
                table: "RackPoints",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InGreenStar",
                table: "RackPoints",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InSpic",
                table: "RackPoints",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "RackPoints",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "RackPoints",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperatedBy",
                table: "RackPoints",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDocumentPathsJson",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinCode",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "RackPoints",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SAPCode",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubVillage",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taluk",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Village",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseType",
                table: "RackPoints",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandFWarehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    WarehouseCode = table.Column<string>(type: "text", nullable: false),
                    InSpic = table.Column<bool>(type: "boolean", nullable: true),
                    InGreenStar = table.Column<bool>(type: "boolean", nullable: true),
                    BasicStateId = table.Column<int>(type: "integer", nullable: true),
                    RegionId = table.Column<int>(type: "integer", nullable: true),
                    HeadquarterId = table.Column<int>(type: "integer", nullable: true),
                    OperatedBy = table.Column<int>(type: "integer", nullable: true),
                    GoogleURL = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    DoorNo = table.Column<string>(type: "text", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    SubVillage = table.Column<string>(type: "text", nullable: true),
                    PinCode = table.Column<string>(type: "text", nullable: true),
                    Village = table.Column<string>(type: "text", nullable: true),
                    Block = table.Column<string>(type: "text", nullable: true),
                    Taluk = table.Column<string>(type: "text", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    DistrictId = table.Column<int>(type: "integer", nullable: false),
                    ContactNumber = table.Column<string>(type: "text", nullable: true),
                    GflReservationQuantityMT = table.Column<decimal>(type: "numeric", nullable: true),
                    GflAdditionalReservationQuantityLitres = table.Column<decimal>(type: "numeric", nullable: true),
                    OtherDocumentPathsJson = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandFWarehouses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandFWarehouses");

            migrationBuilder.DropColumn(
                name: "BasicStateId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Block",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "DoorNo",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "FertilizerLicenseDocumentPath",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "GflAdditionalReservationQuantityMT",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "GflApprovedReservationQuantityMT",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "GstDocumentPath",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "HeadquarterId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "InGreenStar",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "InSpic",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "InsuranceDocumentPath",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OperatedBy",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OtherDocumentPathsJson",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PinCode",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SpicAdditionalReservationQuantityMT",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SpicApprovedReservationQuantityMT",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SubVillage",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Taluk",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Village",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WarehouseType",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "BasicStateId",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "Block",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "HeadquarterId",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "InGreenStar",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "InSpic",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "OperatedBy",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "OtherDocumentPathsJson",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "PinCode",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "SAPCode",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "SubVillage",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "Taluk",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "Village",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "WarehouseType",
                table: "RackPoints");
        }
    }
}
