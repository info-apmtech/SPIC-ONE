using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCSR2IdinDemoTreatment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CSR2Id",
                table: "DemoTreatments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DemoTreatments_CSR2Id",
                table: "DemoTreatments",
                column: "CSR2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DemoTreatments_CSR2s_CSR2Id",
                table: "DemoTreatments",
                column: "CSR2Id",
                principalTable: "CSR2s",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemoTreatments_CSR2s_CSR2Id",
                table: "DemoTreatments");

            migrationBuilder.DropIndex(
                name: "IX_DemoTreatments_CSR2Id",
                table: "DemoTreatments");

            migrationBuilder.DropColumn(
                name: "CSR2Id",
                table: "DemoTreatments");
        }
    }
}
