using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJmdoYearlyTargetsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskAllocations_HeadquarterId",
                table: "JmdoTaskAllocations",
                column: "HeadquarterId");

            migrationBuilder.CreateIndex(
                name: "IX_JmdoTaskAllocationDealers_DealerRegistrationId",
                table: "JmdoTaskAllocationDealers",
                column: "DealerRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_JmdoTaskAllocationDealers_DealerRegistrations_DealerRegistr~",
                table: "JmdoTaskAllocationDealers",
                column: "DealerRegistrationId",
                principalTable: "DealerRegistrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JmdoTaskAllocationDealers_DealerRegistrations_DealerRegistr~",
                table: "JmdoTaskAllocationDealers");

            migrationBuilder.DropIndex(
                name: "IX_JmdoTaskAllocations_HeadquarterId",
                table: "JmdoTaskAllocations");

            migrationBuilder.DropIndex(
                name: "IX_JmdoTaskAllocationDealers_DealerRegistrationId",
                table: "JmdoTaskAllocationDealers");
        }
    }
}
