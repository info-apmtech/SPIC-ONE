using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v12ValuationCertificatePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedValuationCertificatePath",
                table: "DealerAssetLands",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedValuationCertificatePath",
                table: "DealerAssetBuildings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedValuationCertificatePath",
                table: "DealerAssetLands");

            migrationBuilder.DropColumn(
                name: "UploadedValuationCertificatePath",
                table: "DealerAssetBuildings");
        }
    }
}
