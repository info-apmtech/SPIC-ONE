using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubDealerEmployeeApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AVPApprovalRemarks",
                table: "SubDealerBeneficiaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AVPApproved",
                table: "SubDealerBeneficiaries",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AVPApprovedAt",
                table: "SubDealerBeneficiaries",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AVPApprovedBy",
                table: "SubDealerBeneficiaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SMApprovalRemarks",
                table: "SubDealerBeneficiaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SMApproved",
                table: "SubDealerBeneficiaries",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SMApprovedAt",
                table: "SubDealerBeneficiaries",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SMApprovedBy",
                table: "SubDealerBeneficiaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AVPApprovalRemarks",
                table: "EmployeeBeneficiaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AVPApproved",
                table: "EmployeeBeneficiaries",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AVPApprovedAt",
                table: "EmployeeBeneficiaries",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AVPApprovedBy",
                table: "EmployeeBeneficiaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SMApprovalRemarks",
                table: "EmployeeBeneficiaries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SMApproved",
                table: "EmployeeBeneficiaries",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SMApprovedAt",
                table: "EmployeeBeneficiaries",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SMApprovedBy",
                table: "EmployeeBeneficiaries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AVPApprovalRemarks",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AVPApproved",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AVPApprovedAt",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AVPApprovedBy",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApprovalRemarks",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApproved",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApprovedAt",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApprovedBy",
                table: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AVPApprovalRemarks",
                table: "EmployeeBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AVPApproved",
                table: "EmployeeBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AVPApprovedAt",
                table: "EmployeeBeneficiaries");

            migrationBuilder.DropColumn(
                name: "AVPApprovedBy",
                table: "EmployeeBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApprovalRemarks",
                table: "EmployeeBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApproved",
                table: "EmployeeBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApprovedAt",
                table: "EmployeeBeneficiaries");

            migrationBuilder.DropColumn(
                name: "SMApprovedBy",
                table: "EmployeeBeneficiaries");
        }
    }
}
