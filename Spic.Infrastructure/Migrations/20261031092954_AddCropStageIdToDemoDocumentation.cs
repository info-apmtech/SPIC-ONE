using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCropStageIdToDemoDocumentation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CropStage",
                table: "DemoDocumentations");

            migrationBuilder.AddColumn<int>(
                name: "CropStageId",
                table: "DemoDocumentations",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CropStageId",
                table: "DemoDocumentations");

            migrationBuilder.AddColumn<string>(
                name: "CropStage",
                table: "DemoDocumentations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
