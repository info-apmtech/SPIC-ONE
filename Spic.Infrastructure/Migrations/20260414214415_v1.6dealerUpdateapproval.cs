using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v16dealerUpdateapproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AVPApproved",
                table: "DealerRegistrations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RMApproved",
                table: "DealerRegistrations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SMApproved",
                table: "DealerRegistrations",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AVPApproved",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "RMApproved",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SMApproved",
                table: "DealerRegistrations");
        }
    }
}
