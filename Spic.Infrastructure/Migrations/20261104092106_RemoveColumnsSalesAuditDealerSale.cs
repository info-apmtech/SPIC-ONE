using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColumnsSalesAuditDealerSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesAuditDealerSales_DealerRegistrations_DealerRegistratio~",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropIndex(
                name: "IX_SalesAuditDealerSales_DealerRegistrationId",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "BasicValue",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "BillingDate",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "BillingDocument",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "CustomerDistrict",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "CustomerNumber",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "DealerRegistrationId",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "Item",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "RegionCode",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "RegionDescription",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "SalesOffice",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "SalesOfficeDescription",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "SalesOrder",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "SalesOrderDate",
                table: "SalesAuditDealerSales");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "SalesAuditDealerSales");

            migrationBuilder.Sql("""
                    ALTER TABLE "SalesAuditDealerSales"
                    ALTER COLUMN "SourceRowJson"
                    TYPE jsonb
                    USING "SourceRowJson"::jsonb;
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SourceRowJson",
                table: "SalesAuditDealerSales",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<decimal>(
                name: "BasicValue",
                table: "SalesAuditDealerSales",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingDate",
                table: "SalesAuditDealerSales",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "BillingDocument",
                table: "SalesAuditDealerSales",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerDistrict",
                table: "SalesAuditDealerSales",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "SalesAuditDealerSales",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerNumber",
                table: "SalesAuditDealerSales",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DealerRegistrationId",
                table: "SalesAuditDealerSales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "SalesAuditDealerSales",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Item",
                table: "SalesAuditDealerSales",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "SalesAuditDealerSales",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SalesAuditDealerSales",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "SalesAuditDealerSales",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RegionCode",
                table: "SalesAuditDealerSales",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegionDescription",
                table: "SalesAuditDealerSales",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalesOffice",
                table: "SalesAuditDealerSales",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalesOfficeDescription",
                table: "SalesAuditDealerSales",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalesOrder",
                table: "SalesAuditDealerSales",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SalesOrderDate",
                table: "SalesAuditDealerSales",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "SalesAuditDealerSales",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SalesAuditDealerSales_DealerRegistrationId",
                table: "SalesAuditDealerSales",
                column: "DealerRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesAuditDealerSales_DealerRegistrations_DealerRegistratio~",
                table: "SalesAuditDealerSales",
                column: "DealerRegistrationId",
                principalTable: "DealerRegistrations",
                principalColumn: "Id");
        }
    }
}
