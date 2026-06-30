using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v5_gflcreditlimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvpCreditLimitGfl",
                table: "DealerApprovalHistories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RmCreditLimitGfl",
                table: "DealerApprovalHistories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SmCreditLimitGfl",
                table: "DealerApprovalHistories",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvpCreditLimitGfl",
                table: "DealerApprovalHistories");

            migrationBuilder.DropColumn(
                name: "RmCreditLimitGfl",
                table: "DealerApprovalHistories");

            migrationBuilder.DropColumn(
                name: "SmCreditLimitGfl",
                table: "DealerApprovalHistories");
        }
    }
}
