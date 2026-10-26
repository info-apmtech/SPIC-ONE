using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCSR1Level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
      name: "Level1ApprovedAt",
      table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level1ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedBy",
                table: "CSR1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
