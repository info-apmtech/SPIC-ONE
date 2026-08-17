using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3i_tradedepositattachmentsupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DealershipApplicationFeeFilePath",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GflTradeDepositFilePath",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpicTradeDepositFilePath",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealershipApplicationFeeFilePath",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GflTradeDepositFilePath",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "SpicTradeDepositFilePath",
                table: "DealerRegistrations");
        }
    }
}
