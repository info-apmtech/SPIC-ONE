using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _v12UpdateInProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductGroup",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "productId",
                table: "Products",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "productId",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "ProductGroup",
                table: "Products",
                type: "text",
                nullable: true);
        }
    }
}
