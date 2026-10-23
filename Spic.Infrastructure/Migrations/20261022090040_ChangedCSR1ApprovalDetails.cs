using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedCSR1ApprovalDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level1ApprovedAt",
                table: "CSR1ApprovalDetails");

            migrationBuilder.DropColumn(
                name: "Level1ApprovedBy",
                table: "CSR1ApprovalDetails");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedAt",
                table: "CSR1ApprovalDetails");

            migrationBuilder.RenameColumn(
                name: "Level3ApprovedBy",
                table: "CSR1ApprovalDetails",
                newName: "ApproverLevel");

            migrationBuilder.RenameColumn(
                name: "Level3ApprovedAt",
                table: "CSR1ApprovalDetails",
                newName: "ApprovedAt");

            migrationBuilder.RenameColumn(
                name: "Level2ApprovedBy",
                table: "CSR1ApprovalDetails",
                newName: "ApprovedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApproverLevel",
                table: "CSR1ApprovalDetails",
                newName: "Level3ApprovedBy");

            migrationBuilder.RenameColumn(
                name: "ApprovedBy",
                table: "CSR1ApprovalDetails",
                newName: "Level2ApprovedBy");

            migrationBuilder.RenameColumn(
                name: "ApprovedAt",
                table: "CSR1ApprovalDetails",
                newName: "Level3ApprovedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "Level1ApprovedAt",
                table: "CSR1ApprovalDetails",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level1ApprovedBy",
                table: "CSR1ApprovalDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Level2ApprovedAt",
                table: "CSR1ApprovalDetails",
                type: "timestamp without time zone",
                nullable: true);
        }
    }
}
