using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3o_subdealerregistrtionupdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PANNo",
                table: "SubDealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetailMFMSId",
                table: "SubDealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WholesaleMFMSId",
                table: "SubDealerRegistrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PANNo",
                table: "SubDealerRegistrations");

            migrationBuilder.DropColumn(
                name: "RetailMFMSId",
                table: "SubDealerRegistrations");

            migrationBuilder.DropColumn(
                name: "WholesaleMFMSId",
                table: "SubDealerRegistrations");
        }
    }
}
