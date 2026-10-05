using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3welfareApprovalRecommendationComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Relationship",
                table: "WelfareApplications");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "WelfareApplicationApprovals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "WelfareApplicationApprovals",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeBeneficiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BeneficiaryId = table.Column<long>(type: "bigint", nullable: true),
                    DealerCode = table.Column<string>(type: "text", nullable: false),
                    EmployeeName = table.Column<string>(type: "text", nullable: false),
                    BeneficiaryName = table.Column<string>(type: "text", nullable: false),
                    DOB = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Relationship = table.Column<string>(type: "text", nullable: true),
                    MaritalStatus = table.Column<string>(type: "text", nullable: true),
                    EducationalQualification = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeBeneficiaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubDealerBeneficiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BeneficiaryId = table.Column<long>(type: "bigint", nullable: true),
                    DealerCode = table.Column<string>(type: "text", nullable: false),
                    MainDealerFirmName = table.Column<string>(type: "text", nullable: false),
                    HQ = table.Column<string>(type: "text", nullable: true),
                    BranchDistrict = table.Column<string>(type: "text", nullable: true),
                    SubDealerCode = table.Column<string>(type: "text", nullable: false),
                    SubDealerName = table.Column<string>(type: "text", nullable: false),
                    SubDealerDistrict = table.Column<string>(type: "text", nullable: true),
                    NomineeName = table.Column<string>(type: "text", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "text", nullable: false),
                    DOB = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Relationship = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubDealerBeneficiaries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeBeneficiaries");

            migrationBuilder.DropTable(
                name: "SubDealerBeneficiaries");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "WelfareApplicationApprovals");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "WelfareApplicationApprovals");

            migrationBuilder.AddColumn<string>(
                name: "Relationship",
                table: "WelfareApplications",
                type: "text",
                nullable: true);
        }
    }
}
