using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSasFarmerMobileUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SasFarmers_Mobile",
                table: "SasFarmers");

            migrationBuilder.CreateIndex(
                name: "IX_SasFarmers_Mobile",
                table: "SasFarmers",
                column: "Mobile",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SasFarmers_Mobile",
                table: "SasFarmers");

            migrationBuilder.CreateIndex(
                name: "IX_SasFarmers_Mobile",
                table: "SasFarmers",
                column: "Mobile");
        }
    }
}
