using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v3sWelfareScheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WelfareApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealerId = table.Column<int>(type: "integer", nullable: false),
                    SchemeName = table.Column<int>(type: "integer", nullable: false),
                    ApplicationNumber = table.Column<string>(type: "text", nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DealerCode = table.Column<string>(type: "text", nullable: true),
                    DealerName = table.Column<string>(type: "text", nullable: true),
                    DealershipNature = table.Column<string>(type: "text", nullable: true),
                    MobileNumber = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    QuantityLifted = table.Column<int>(type: "integer", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "text", nullable: true),
                    Relationship = table.Column<string>(type: "text", nullable: true),
                    BeneficiaryDateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NomineeName = table.Column<string>(type: "text", nullable: true),
                    NomineeRelationship = table.Column<string>(type: "text", nullable: true),
                    BeneficiaryNameAsInCheque = table.Column<string>(type: "text", nullable: true),
                    LeafOrBankPassbook = table.Column<string>(type: "text", nullable: true),
                    MarriageDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EventDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OwnershipType = table.Column<string>(type: "text", nullable: true),
                    EventVenue = table.Column<string>(type: "text", nullable: true),
                    Course = table.Column<string>(type: "text", nullable: true),
                    EduYear = table.Column<int>(type: "integer", nullable: true),
                    CollegeName = table.Column<string>(type: "text", nullable: true),
                    TotalNumberOfCourses = table.Column<int>(type: "integer", nullable: true),
                    IsFirstApplication = table.Column<bool>(type: "boolean", nullable: true),
                    MedicalTreatmentType = table.Column<string>(type: "text", nullable: true),
                    DateOfDeath = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LegalHeirName = table.Column<string>(type: "text", nullable: true),
                    DeathCause = table.Column<string>(type: "text", nullable: true),
                    MeritCandidateName = table.Column<string>(type: "text", nullable: true),
                    MeritFatherName = table.Column<string>(type: "text", nullable: true),
                    ExaminationAppeared = table.Column<string>(type: "text", nullable: true),
                    BoardName = table.Column<string>(type: "text", nullable: true),
                    MaximumMarks = table.Column<int>(type: "integer", nullable: true),
                    MarksObtained = table.Column<int>(type: "integer", nullable: true),
                    MeritPercentage = table.Column<double>(type: "double precision", nullable: true),
                    DistinctionCandidateName = table.Column<string>(type: "text", nullable: true),
                    DistinctionFatherName = table.Column<string>(type: "text", nullable: true),
                    ProfessionalCourseName = table.Column<string>(type: "text", nullable: true),
                    CourseCompletionYear = table.Column<string>(type: "text", nullable: true),
                    UniversityName = table.Column<string>(type: "text", nullable: true),
                    DistinctionMaximumMarks = table.Column<int>(type: "integer", nullable: true),
                    DistinctionMarksObtained = table.Column<int>(type: "integer", nullable: true),
                    DistinctionAggregatePercentage = table.Column<double>(type: "double precision", nullable: true),
                    HasArrears = table.Column<bool>(type: "boolean", nullable: true),
                    IsWholesaleDealerEmployee = table.Column<bool>(type: "boolean", nullable: true),
                    BeneficiaryGroup = table.Column<string>(type: "text", nullable: true),
                    SubDealerId = table.Column<int>(type: "integer", nullable: true),
                    SubDealerName = table.Column<string>(type: "text", nullable: true),
                    EmployeeId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeName = table.Column<string>(type: "text", nullable: true),
                    AverageQuantityLifted3Years = table.Column<decimal>(type: "numeric", nullable: true),
                    LastYearQuantityLifted = table.Column<decimal>(type: "numeric", nullable: true),
                    IsDeclarationConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelfareApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WelfareApplications_DealerRegistrations_DealerId",
                        column: x => x.DealerId,
                        principalTable: "DealerRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WelfareApplicationApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WelfareApplicationId = table.Column<int>(type: "integer", nullable: false),
                    ApprovalLevel = table.Column<int>(type: "integer", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    ApprovedBy = table.Column<string>(type: "text", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelfareApplicationApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WelfareApplicationApprovals_WelfareApplications_WelfareAppl~",
                        column: x => x.WelfareApplicationId,
                        principalTable: "WelfareApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WelfareApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WelfareApplicationId = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: true),
                    DocumentName = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelfareApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WelfareApplicationDocuments_WelfareApplications_WelfareAppl~",
                        column: x => x.WelfareApplicationId,
                        principalTable: "WelfareApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WelfareApplicationApprovals_WelfareApplicationId",
                table: "WelfareApplicationApprovals",
                column: "WelfareApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_WelfareApplicationDocuments_WelfareApplicationId",
                table: "WelfareApplicationDocuments",
                column: "WelfareApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_WelfareApplications_DealerId",
                table: "WelfareApplications",
                column: "DealerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WelfareApplicationApprovals");

            migrationBuilder.DropTable(
                name: "WelfareApplicationDocuments");

            migrationBuilder.DropTable(
                name: "WelfareApplications");
        }
    }
}
