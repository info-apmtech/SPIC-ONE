using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v18DealerRegUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SaleToDealer",
                table: "AnnualSaleDataLastFY",
                newName: "SaleToDealerQty");

            migrationBuilder.RenameColumn(
                name: "OwnRetailsSale",
                table: "AnnualSaleDataLastFY",
                newName: "SaleToDealerAmount");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalApprovalDate",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SettlementAmount",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettlementDate",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TradeDepositAmount",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "TradeDepositDate",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TradeDepositReceiptNo",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Block",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "DealerOwnershipInfos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "DealerOwnershipInfos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaritalStatus",
                table: "DealerOwnershipInfos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PinCode",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShopNoORRoomNoOrBlockNo",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "DealerOwnershipInfos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubVillage",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taluk",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Village",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "GQ10Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ11Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ12Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ1Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ2Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ3Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ4Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ5Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ6Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ7Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ8Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GQ9Mark",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenstarAdditionalCreditLimit",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenstarBGAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarBGNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarBGOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenstarCollateralAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarCollateralNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarCollateralOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenstarFDAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarFDNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarFDOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GreenstarTradeDepositAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarTradeDepositNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GreenstarTradeDepositOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpicBGAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SpicBGNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpicBGOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpicCollateralAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SpicCollateralNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpicCollateralOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpicFDAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SpicFDNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpicFDOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SpicTradeDepositAmount",
                table: "DealerCreditLimitProposals",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SpicTradeDepositNumber",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpicTradeDepositOtherDetails",
                table: "DealerCreditLimitProposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnRetailsSaleAmount",
                table: "AnnualSaleDataLastFY",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnRetailsSaleQty",
                table: "AnnualSaleDataLastFY",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalApprovalDate",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SettlementAmount",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SettlementDate",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "TradeDepositAmount",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "TradeDepositDate",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "TradeDepositReceiptNo",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "Block",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "PinCode",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "ShopNoORRoomNoOrBlockNo",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "SubVillage",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "Taluk",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "Village",
                table: "DealerOwnershipInfos");

            migrationBuilder.DropColumn(
                name: "GQ10Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ11Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ12Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ1Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ2Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ3Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ4Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ5Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ6Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ7Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ8Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GQ9Mark",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarAdditionalCreditLimit",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarBGAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarBGNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarBGOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarCollateralAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarCollateralNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarCollateralOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarFDAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarFDNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarFDOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarTradeDepositAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarTradeDepositNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "GreenstarTradeDepositOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicBGAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicBGNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicBGOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicCollateralAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicCollateralNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicCollateralOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicFDAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicFDNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicFDOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositAmount",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositNumber",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositOtherDetails",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "OwnRetailsSaleAmount",
                table: "AnnualSaleDataLastFY");

            migrationBuilder.DropColumn(
                name: "OwnRetailsSaleQty",
                table: "AnnualSaleDataLastFY");

            migrationBuilder.RenameColumn(
                name: "SaleToDealerQty",
                table: "AnnualSaleDataLastFY",
                newName: "SaleToDealer");

            migrationBuilder.RenameColumn(
                name: "SaleToDealerAmount",
                table: "AnnualSaleDataLastFY",
                newName: "OwnRetailsSale");
        }
    }
}
