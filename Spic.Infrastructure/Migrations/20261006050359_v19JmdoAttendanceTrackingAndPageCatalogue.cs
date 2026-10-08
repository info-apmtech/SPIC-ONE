using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <summary>
    /// 2026-10-06, found by the pre-deploy dry run: the JMDO attendance / GPS tracking entities
    /// (JMDOAttendances, JMDOTrackingSessions, JMDOTrackingPoints) and the page keys added for the
    /// new menus had no migration, so `dotnet ef database update` refused to run (pending model
    /// changes). Tables as EF scaffolded them; page rows by key (see the comment inside Up).
    /// </summary>
    public partial class v19JmdoAttendanceTrackingAndPageCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JMDOAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VisitArea = table.Column<string>(type: "text", nullable: true),
                    AttendanceStatus = table.Column<int>(type: "integer", nullable: false),
                    DutyStatus = table.Column<int>(type: "integer", nullable: false),
                    AttendanceUpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AttendanceLatitude = table.Column<double>(type: "double precision", nullable: true),
                    AttendanceLongitude = table.Column<double>(type: "double precision", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JMDOAttendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JMDOTrackingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttendanceId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    StartLatitude = table.Column<double>(type: "double precision", nullable: true),
                    StartLongitude = table.Column<double>(type: "double precision", nullable: true),
                    EndLatitude = table.Column<double>(type: "double precision", nullable: true),
                    EndLongitude = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JMDOTrackingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JMDOTrackingSessions_JMDOAttendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "JMDOAttendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JMDOTrackingPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrackingSessionId = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JMDOTrackingPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JMDOTrackingPoints_JMDOTrackingSessions_TrackingSessionId",
                        column: x => x.TrackingSessionId,
                        principalTable: "JMDOTrackingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Page catalogue rows for the PagePermission keys added since V5t (AnnualBudgeting ..
            // TasksAllocation). EF scaffolded InsertData with enum-index ids (109-121) that collide
            // with rows production already holds under other ids (V5s / V5t insert with generated
            // ids, v17 hard-codes 108). Same rule as V5s: insert by Key when missing, never touch an
            // existing row. The sequence is moved past the highest id FIRST: v17 inserted id 108 by
            // hand without touching the sequence, so the next generated id would collide with it
            // (found on the local dry run), and again at the end for whatever this migration added.
            migrationBuilder.Sql(@"
SELECT setval(pg_get_serial_sequence('""Pages""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""Pages"") + 1, nextval(pg_get_serial_sequence('""Pages""', 'Id'))), false);
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'AnnualBudgeting', NULL, 'Annual Budgeting', 106, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'AnnualBudgeting');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'StateBudgetManagement', NULL, 'State Budget Management', 107, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'StateBudgetManagement');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'FieldDashboard', NULL, 'Field Dashboard', 108, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'FieldDashboard');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'FieldReports', NULL, 'Field Reports', 109, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'FieldReports');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'MdoDashboard', NULL, 'Mdo Dashboard', 110, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'MdoDashboard');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'AllocationRequests', NULL, 'Allocation Requests', 111, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'AllocationRequests');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'TaskMonitoring', NULL, 'Task Monitoring', 112, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'TaskMonitoring');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'PerformanceTracker', NULL, 'Performance Tracker', 113, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'PerformanceTracker');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'TargetAchievement', NULL, 'Target Achievement', 114, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'TargetAchievement');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'FarmerData', NULL, 'Farmer Data', 115, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'FarmerData');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'MdoReports', NULL, 'Mdo Reports', 116, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'MdoReports');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SdwaCompanyMaster', NULL, 'Sdwa Company Master', 117, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'SdwaCompanyMaster');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'GuestHouseCancellations', NULL, 'Guest House Cancellations', 118, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'GuestHouseCancellations');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'DemoDocumentation', NULL, 'Demo Documentation', 119, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'DemoDocumentation');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SalesAudit', NULL, 'Sales Audit', 120, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'SalesAudit');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'ExtensionRequests', NULL, 'Extension Requests', 121, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'ExtensionRequests');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'MarkAttendance', NULL, 'Mark Attendance', 122, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'MarkAttendance');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'TasksAllocation', NULL, 'Tasks Allocation', 123, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'TasksAllocation');
SELECT setval(pg_get_serial_sequence('""Pages""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""Pages"") + 1, nextval(pg_get_serial_sequence('""Pages""', 'Id'))), false);
");

            migrationBuilder.CreateIndex(
                name: "IX_JMDOAttendances_UserId_Date",
                table: "JMDOAttendances",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JMDOTrackingPoints_TrackingSessionId",
                table: "JMDOTrackingPoints",
                column: "TrackingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_JMDOTrackingSessions_AttendanceId",
                table: "JMDOTrackingSessions",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_JMDOTrackingSessions_UserId",
                table: "JMDOTrackingSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JMDOTrackingPoints");

            migrationBuilder.DropTable(
                name: "JMDOTrackingSessions");

            migrationBuilder.DropTable(
                name: "JMDOAttendances");

            // Page rows added above are left in place (see V5s): they are harmless and may predate this migration.
        }
    }
}
