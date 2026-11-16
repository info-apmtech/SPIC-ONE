
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    public partial class UpdateReasonasEnuminSalesAuditExtension : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "SalesAuditExtensionRequests");

            migrationBuilder.AddColumn<int>(
                name: "Reason",
                table: "SalesAuditExtensionRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "SalesAuditExtensionRequests");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "SalesAuditExtensionRequests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
