using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIfmsDealerNatureAndMobile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealershipNature",
                table: "IfmsDealers");

            migrationBuilder.DropColumn(
                name: "MobileNo",
                table: "IfmsDealers");

            migrationBuilder.AddColumn<int>(
                name: "DealershipNatureId",
                table: "IfmsDealers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealershipNatureId",
                table: "IfmsDealers");

            migrationBuilder.AddColumn<string>(
                name: "DealershipNature",
                table: "IfmsDealers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileNo",
                table: "IfmsDealers",
                type: "text",
                nullable: true);
        }
    }
}
