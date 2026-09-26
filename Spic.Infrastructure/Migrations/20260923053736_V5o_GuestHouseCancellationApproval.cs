using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5o_GuestHouseCancellationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdminDecisionAt",
                table: "GuestHouseBookingCancellation",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminDecisionBy",
                table: "GuestHouseBookingCancellation",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "GuestHouseBookingCancellation",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "GuestHouseBookingCancellation",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminDecisionAt",
                table: "GuestHouseBookingCancellation");

            migrationBuilder.DropColumn(
                name: "AdminDecisionBy",
                table: "GuestHouseBookingCancellation");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "GuestHouseBookingCancellation");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "GuestHouseBookingCancellation");
        }
    }
}
