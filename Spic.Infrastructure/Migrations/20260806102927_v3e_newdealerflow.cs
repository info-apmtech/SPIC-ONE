using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3e_newdealerflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DealershipApplicationFeeAmount",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DealershipApplicationFeeBankId",
                table: "DealerRegistrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DealershipApplicationFeeDDDate",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealershipApplicationFeeDDNumber",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealershipApplicationFeePayableAt",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GflTradeDepositDDAmount",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GflTradeDepositDDBankId",
                table: "DealerRegistrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GflTradeDepositDDDate",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GflTradeDepositDDNumber",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNewDealerRegistration",
                table: "DealerRegistrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SpicTradeDepositDDAmount",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpicTradeDepositDDBankId",
                table: "DealerRegistrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpicTradeDepositDDDate",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpicTradeDepositDDNumber",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealershipApplicationFeeAmount",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "DealershipApplicationFeeBankId",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "DealershipApplicationFeeDDDate",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "DealershipApplicationFeeDDNumber",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "DealershipApplicationFeePayableAt",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GflTradeDepositDDAmount",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GflTradeDepositDDBankId",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GflTradeDepositDDDate",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GflTradeDepositDDNumber",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "IsNewDealerRegistration",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositDDAmount",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositDDBankId",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositDDDate",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositDDNumber",
                table: "DealerRegistrations");
        }
    }
}
