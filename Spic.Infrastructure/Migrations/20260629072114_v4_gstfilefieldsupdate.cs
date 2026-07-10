using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4_gstfilefieldsupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GSTConstitutionofBusiness",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GSTLegalName",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GSTTradeName",
                table: "DealerRegistrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GSTConstitutionofBusiness",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GSTLegalName",
                table: "DealerRegistrations");

            migrationBuilder.DropColumn(
                name: "GSTTradeName",
                table: "DealerRegistrations");
        }
    }
}
