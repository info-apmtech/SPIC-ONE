using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4b_UpdateInWelfareApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChequeAmount",
                table: "WelfareApplications",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChequeImagePath",
                table: "WelfareApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChequeNumber",
                table: "WelfareApplications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChequeAmount",
                table: "WelfareApplications");

            migrationBuilder.DropColumn(
                name: "ChequeImagePath",
                table: "WelfareApplications");

            migrationBuilder.DropColumn(
                name: "ChequeNumber",
                table: "WelfareApplications");
        }
    }
}
