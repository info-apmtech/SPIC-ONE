using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v4IfmsAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IfmsAutomationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Trigger = table.Column<int>(type: "integer", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    SitePortalReachable = table.Column<bool>(type: "boolean", nullable: false),
                    LoginSucceeded = table.Column<bool>(type: "boolean", nullable: false),
                    CaptchaMethod = table.Column<string>(type: "text", nullable: true),
                    CaptchaAttempts = table.Column<int>(type: "integer", nullable: false),
                    OtpMethod = table.Column<string>(type: "text", nullable: true),
                    ReportsTotal = table.Column<int>(type: "integer", nullable: false),
                    ReportsSucceeded = table.Column<int>(type: "integer", nullable: false),
                    ReportsFailed = table.Column<int>(type: "integer", nullable: false),
                    RowsInserted = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RowsSkipped = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AlertSent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsAutomationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfmsChallengeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunId = table.Column<int>(type: "integer", nullable: true),
                    ChallengeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ImageBase64 = table.Column<string>(type: "text", nullable: true),
                    Prompt = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    FailedGuesses = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Answer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AnsweredBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsChallengeRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfmsOtpMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Sender = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExtractedOtp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ConsumedByRunId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsOtpMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfmsPortalSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PortalUserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StorageStateJson = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastValidatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    InvalidatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    InvalidationReason = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsPortalSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfmsAutomationReportRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RunId = table.Column<int>(type: "integer", nullable: false),
                    JobKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CategoryId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ReportTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AppliedFilters = table.Column<string>(type: "text", nullable: true),
                    ReportDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DownloadedFileName = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ArchivedFilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DownloadedBytes = table.Column<long>(type: "bigint", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    RowsInserted = table.Column<int>(type: "integer", nullable: false),
                    RowsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RowsSkipped = table.Column<int>(type: "integer", nullable: false),
                    Warnings = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfmsAutomationReportRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IfmsAutomationReportRuns_IfmsAutomationRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "IfmsAutomationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IfmsAutomationReportRuns_RunId_JobKey",
                table: "IfmsAutomationReportRuns",
                columns: new[] { "RunId", "JobKey" });

            migrationBuilder.CreateIndex(
                name: "IX_IfmsAutomationRuns_ReportDate_StartedAt",
                table: "IfmsAutomationRuns",
                columns: new[] { "ReportDate", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IfmsChallengeRequests_Status_CreatedAt",
                table: "IfmsChallengeRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IfmsOtpMessages_ConsumedAt_ReceivedAt",
                table: "IfmsOtpMessages",
                columns: new[] { "ConsumedAt", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IfmsPortalSessions_PortalUserName_IsActive",
                table: "IfmsPortalSessions",
                columns: new[] { "PortalUserName", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IfmsAutomationReportRuns");

            migrationBuilder.DropTable(
                name: "IfmsChallengeRequests");

            migrationBuilder.DropTable(
                name: "IfmsOtpMessages");

            migrationBuilder.DropTable(
                name: "IfmsPortalSessions");

            migrationBuilder.DropTable(
                name: "IfmsAutomationRuns");
        }
    }
}
