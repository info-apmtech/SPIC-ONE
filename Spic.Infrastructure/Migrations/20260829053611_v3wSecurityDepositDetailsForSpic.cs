using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3wSecurityDepositDetailsForSpic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SpicSecurityDepositAmount",
                table: "DealerRegistrations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpicSecurityDepositDate",
                table: "DealerRegistrations",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpicSecurityDepositReceiptNo",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpicSecurityDepositAmount",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SpicSecurityDepositDate",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SpicSecurityDepositReceiptNo",
                table: "DealerRegistrations");
        }
    }
}
