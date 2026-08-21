using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3t_proprietorpageupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EducationalQualification",
                table: "PartnerFamilyDetails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaritalStatus",
                table: "PartnerFamilyDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationalQualification",
                table: "DealerOwnershipInfos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationalQualification",
                table: "PartnerFamilyDetails");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "PartnerFamilyDetails");

            migrationBuilder.DropColumn(
                name: "EducationalQualification",
                table: "DealerOwnershipInfos");
        }
    }
}
