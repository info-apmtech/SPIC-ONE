using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3m_warehouseandrakepointupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarehouseType",
                table: "RackPoints");

            migrationBuilder.AddColumn<bool>(
                name: "AVPApproved",
                table: "Warehouses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AVPApprovedAt",
                table: "Warehouses",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AVPApprovedBy",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalRemarks",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubmittedForReview",
                table: "Warehouses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RMApproved",
                table: "Warehouses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RMApprovedAt",
                table: "Warehouses",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RMApprovedBy",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SMApproved",
                table: "Warehouses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SMApprovedAt",
                table: "Warehouses",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SMApprovedBy",
                table: "Warehouses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AVPApproved",
                table: "RackPoints",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AVPApprovedAt",
                table: "RackPoints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AVPApprovedBy",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalRemarks",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubmittedForReview",
                table: "RackPoints",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RMApproved",
                table: "RackPoints",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RMApprovedAt",
                table: "RackPoints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RMApprovedBy",
                table: "RackPoints",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SMApproved",
                table: "RackPoints",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SMApprovedAt",
                table: "RackPoints",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SMApprovedBy",
                table: "RackPoints",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AVPApproved",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "AVPApprovedAt",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "AVPApprovedBy",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ApprovalRemarks",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsSubmittedForReview",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "RMApproved",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "RMApprovedAt",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "RMApprovedBy",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SMApproved",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SMApprovedAt",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SMApprovedBy",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "AVPApproved",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "AVPApprovedAt",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "AVPApprovedBy",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "ApprovalRemarks",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "IsSubmittedForReview",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "RMApproved",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "RMApprovedAt",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "RMApprovedBy",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "SMApproved",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "SMApprovedAt",
                table: "RackPoints");

            migrationBuilder.DropColumn(
                name: "SMApprovedBy",
                table: "RackPoints");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseType",
                table: "RackPoints",
                type: "integer",
                nullable: true);
        }
    }
}
