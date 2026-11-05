using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColumnsinSalesAuditMatchedInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingDate",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "BillingDocument",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "DealerCode",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "DealerName",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "GrossAmountRs",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "QuantityMT",
                table: "SalesAuditMatchedInvoices");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SalesAuditMatchedInvoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BillingDate",
                table: "SalesAuditMatchedInvoices",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "BillingDocument",
                table: "SalesAuditMatchedInvoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DealerCode",
                table: "SalesAuditMatchedInvoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DealerName",
                table: "SalesAuditMatchedInvoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmountRs",
                table: "SalesAuditMatchedInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "SalesAuditMatchedInvoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "SalesAuditMatchedInvoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SalesAuditMatchedInvoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityMT",
                table: "SalesAuditMatchedInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SalesAuditMatchedInvoices",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
