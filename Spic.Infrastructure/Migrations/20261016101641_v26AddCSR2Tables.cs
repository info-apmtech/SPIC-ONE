using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v26AddCSR2Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CSR2s",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR1Id = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActualEventDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EventCoordinatorEmployeeId = table.Column<int>(type: "integer", nullable: true),
                    ActualLocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpectedParticipantsCount = table.Column<int>(type: "integer", nullable: true),
                    IsOrganized = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedBudgetSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubmittedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2s", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2s_CSR1_CSR1Id",
                        column: x => x.CSR1Id,
                        principalTable: "CSR1",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CSR2Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    ActivityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ParticipantsCount = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2Activities_CSR2s_CSR2Id",
                        column: x => x.CSR2Id,
                        principalTable: "CSR2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CSR2Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    EmployeeInformationId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RoleName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2Employees_CSR2s_CSR2Id",
                        column: x => x.CSR2Id,
                        principalTable: "CSR2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CSR2Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    CategoryKey = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PlannedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ExpenseDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2Expenses_CSR2s_CSR2Id",
                        column: x => x.CSR2Id,
                        principalTable: "CSR2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CSR2Media",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UseInDigitalLibrary = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2Media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2Media_CSR2s_CSR2Id",
                        column: x => x.CSR2Id,
                        principalTable: "CSR2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CSR2Participants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2Participants_CSR2s_CSR2Id",
                        column: x => x.CSR2Id,
                        principalTable: "CSR2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CSR2ExpenseProofs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2ExpenseId = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2ExpenseProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2ExpenseProofs_CSR2Expenses_CSR2ExpenseId",
                        column: x => x.CSR2ExpenseId,
                        principalTable: "CSR2Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Activities_CSR2Id_SortOrder",
                table: "CSR2Activities",
                columns: new[] { "CSR2Id", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Employees_CSR2Id_EmployeeInformationId",
                table: "CSR2Employees",
                columns: new[] { "CSR2Id", "EmployeeInformationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CSR2ExpenseProofs_CSR2ExpenseId",
                table: "CSR2ExpenseProofs",
                column: "CSR2ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Expenses_CSR2Id_CategoryKey",
                table: "CSR2Expenses",
                columns: new[] { "CSR2Id", "CategoryKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Media_CSR2Id_MediaType",
                table: "CSR2Media",
                columns: new[] { "CSR2Id", "MediaType" });

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Participants_CSR2Id",
                table: "CSR2Participants",
                column: "CSR2Id");

            migrationBuilder.CreateIndex(
                name: "IX_CSR2s_CSR1Id",
                table: "CSR2s",
                column: "CSR1Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CSR2Activities");

            migrationBuilder.DropTable(
                name: "CSR2Employees");

            migrationBuilder.DropTable(
                name: "CSR2ExpenseProofs");

            migrationBuilder.DropTable(
                name: "CSR2Media");

            migrationBuilder.DropTable(
                name: "CSR2Participants");

            migrationBuilder.DropTable(
                name: "CSR2Expenses");

            migrationBuilder.DropTable(
                name: "CSR2s");
        }
    }
}
