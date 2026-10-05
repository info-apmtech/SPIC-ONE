using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _v14UpdateInDealerCreditLimitProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GreenstarMonthlyAvgNetOverdues",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SpicMonthlyAvgNetOverdues",
                table: "DealerCreditLimitProposals",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GreenstarMonthlyAvgNetOverdues",
                table: "DealerCreditLimitProposals");

            migrationBuilder.DropColumn(
                name: "SpicMonthlyAvgNetOverdues",
                table: "DealerCreditLimitProposals");
        }
    }
}
