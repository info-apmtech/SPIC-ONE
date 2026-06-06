using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v26CreditLimitInDealerApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvpCreditLimit",
                table: "DealerApprovalHistories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RmCreditLimit",
                table: "DealerApprovalHistories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SmCreditLimit",
                table: "DealerApprovalHistories",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvpCreditLimit",
                table: "DealerApprovalHistories");

            migrationBuilder.DropColumn(
                name: "RmCreditLimit",
                table: "DealerApprovalHistories");

            migrationBuilder.DropColumn(
                name: "SmCreditLimit",
                table: "DealerApprovalHistories");
        }
    }
}
