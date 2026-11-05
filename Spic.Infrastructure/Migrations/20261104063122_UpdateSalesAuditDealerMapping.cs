using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSalesAuditDealerMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualSalesRs",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "CustomerDistrict",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "CustomerNumber",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "MappedBy",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "PlannedCommitmentRs",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "QuantityMT",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "UploadFileName",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "SalesAuditDealerMappings");

            migrationBuilder.AddColumn<int>(
                name: "DealerRegistrationId",
                table: "SalesAuditDealerMappings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SalesAuditDealerMappings_CSR2Id",
                table: "SalesAuditDealerMappings",
                column: "CSR2Id");

            migrationBuilder.CreateIndex(
                name: "IX_SalesAuditDealerMappings_DealerRegistrationId",
                table: "SalesAuditDealerMappings",
                column: "DealerRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesAuditDealerMappings_CSR2s_CSR2Id",
                table: "SalesAuditDealerMappings",
                column: "CSR2Id",
                principalTable: "CSR2s",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesAuditDealerMappings_DealerRegistrations_DealerRegistra~",
                table: "SalesAuditDealerMappings",
                column: "DealerRegistrationId",
                principalTable: "DealerRegistrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesAuditDealerMappings_CSR2s_CSR2Id",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesAuditDealerMappings_DealerRegistrations_DealerRegistra~",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropIndex(
                name: "IX_SalesAuditDealerMappings_CSR2Id",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropIndex(
                name: "IX_SalesAuditDealerMappings_DealerRegistrationId",
                table: "SalesAuditDealerMappings");

            migrationBuilder.DropColumn(
                name: "DealerRegistrationId",
                table: "SalesAuditDealerMappings");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualSalesRs",
                table: "SalesAuditDealerMappings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDistrict",
                table: "SalesAuditDealerMappings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "SalesAuditDealerMappings",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerNumber",
                table: "SalesAuditDealerMappings",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MappedBy",
                table: "SalesAuditDealerMappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedCommitmentRs",
                table: "SalesAuditDealerMappings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "SalesAuditDealerMappings",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SalesAuditDealerMappings",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityMT",
                table: "SalesAuditDealerMappings",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UploadFileName",
                table: "SalesAuditDealerMappings",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "SalesAuditDealerMappings",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
