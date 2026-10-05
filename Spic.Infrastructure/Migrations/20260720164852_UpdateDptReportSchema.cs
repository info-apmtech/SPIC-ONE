using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDptReportSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Company",
                table: "DptReports");

            migrationBuilder.DropColumn(
                name: "DealershipNature",
                table: "DptReports");

            migrationBuilder.DropColumn(
                name: "RetailerId",
                table: "DptReports");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "DptReports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DealershipNatureId",
                table: "DptReports",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DptReports");

            migrationBuilder.DropColumn(
                name: "DealershipNatureId",
                table: "DptReports");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "DptReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealershipNature",
                table: "DptReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetailerId",
                table: "DptReports",
                type: "text",
                nullable: true);
        }
    }
}
