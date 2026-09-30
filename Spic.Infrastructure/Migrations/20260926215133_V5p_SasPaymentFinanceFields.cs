using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5p_SasPaymentFinanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinanceReceivedDate",
                table: "SamplePayments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinanceVerifiedAmount",
                table: "SamplePayments",
                type: "numeric(12,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinanceReceivedDate",
                table: "SamplePayments");

            migrationBuilder.DropColumn(
                name: "FinanceVerifiedAmount",
                table: "SamplePayments");
        }
    }
}
