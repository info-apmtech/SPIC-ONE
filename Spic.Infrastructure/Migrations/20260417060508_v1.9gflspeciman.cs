using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v19gflspeciman : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GreenstarSpecimanFilePath",
                table: "DealerRegistrationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExistingCreditLimitAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExistingCreditLimitFrom",
                table: "DealerCreditLimitProposals",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExistingCreditLimitTo",
                table: "DealerCreditLimitProposals",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Block",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "DealerAssetBuildings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PinCode",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShopNoORRoomNoOrBlockNo",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "DealerAssetBuildings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubVillage",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taluk",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Village",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GreenstarSpecimanFilePath",
                table: "DealerRegistrationDocuments");

            migrationBuilder.DropColumn(
                name: "ExistingCreditLimitAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "ExistingCreditLimitFrom",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "ExistingCreditLimitTo",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "Block",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "PinCode",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "ShopNoORRoomNoOrBlockNo",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "SubVillage",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "Taluk",
                table: "DealerAssetBuildings");

            migrationBuilder.DropColumn(
                name: "Village",
                table: "DealerAssetBuildings");
        }
    }
}
