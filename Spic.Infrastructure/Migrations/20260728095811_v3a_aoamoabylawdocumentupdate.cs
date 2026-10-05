using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3a_aoamoabylawdocumentupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArticlesOfAssociationFilePath",
                table: "DealerRegistrationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ByLaw",
                table: "DealerRegistrationDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemorandumOfAssociationFilePath",
                table: "DealerRegistrationDocuments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticlesOfAssociationFilePath",
                table: "DealerRegistrationDocuments");

            migrationBuilder.DropColumn(
                name: "ByLaw",
                table: "DealerRegistrationDocuments");

            migrationBuilder.DropColumn(
                name: "MemorandumOfAssociationFilePath",
                table: "DealerRegistrationDocuments");
        }
    }
}
