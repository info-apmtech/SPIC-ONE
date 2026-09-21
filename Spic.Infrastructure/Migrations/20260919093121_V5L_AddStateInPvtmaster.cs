using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5L_AddStateInPvtmaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "RakePointMasters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "PVTMasters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateId",
                table: "RakePointMasters");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "PVTMasters");
        }
    }
}
