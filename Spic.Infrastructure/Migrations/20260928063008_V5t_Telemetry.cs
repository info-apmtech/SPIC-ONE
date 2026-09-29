using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5t_Telemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppErrorLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    App = table.Column<int>(type: "integer", nullable: false),
                    AppVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StackTrace = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Route = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppPageViews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Route = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    App = table.Column<int>(type: "integer", nullable: false),
                    AppVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPageViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRequestLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    App = table.Column<int>(type: "integer", nullable: false),
                    AppVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRequestLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRouteDaily",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    App = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Route = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Hits = table.Column<int>(type: "integer", nullable: false),
                    Users = table.Column<int>(type: "integer", nullable: false),
                    Errors = table.Column<int>(type: "integer", nullable: false),
                    AvgDurationMs = table.Column<int>(type: "integer", nullable: false),
                    P95DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRouteDaily", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUsageDaily",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    App = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActiveUsers = table.Column<int>(type: "integer", nullable: false),
                    Requests = table.Column<int>(type: "integer", nullable: false),
                    PageViews = table.Column<int>(type: "integer", nullable: false),
                    Errors = table.Column<int>(type: "integer", nullable: false),
                    AvgDurationMs = table.Column<int>(type: "integer", nullable: false),
                    P95DurationMs = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsageDaily", x => x.Id);
                });

            // Page key "Metrics" (PagePermission, appended last). EF scaffolded InsertData with the
            // enum-index id 106, which on live databases can already belong to another page (V5s gave
            // the merged keys generated ids). Same idempotent pattern as V5s: insert by Key when
            // missing with a generated id, never touch an existing row, then move the sequence on.
            migrationBuilder.Sql(@"
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'Metrics', 'Administration', 'Metrics', 105, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'Metrics');
SELECT setval(pg_get_serial_sequence('""Pages""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""Pages"") + 1, nextval(pg_get_serial_sequence('""Pages""', 'Id'))), false);
");

            migrationBuilder.CreateIndex(
                name: "IX_AppErrorLogs_At",
                table: "AppErrorLogs",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_AppErrorLogs_Fingerprint_At",
                table: "AppErrorLogs",
                columns: new[] { "Fingerprint", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_AppErrorLogs_TraceId",
                table: "AppErrorLogs",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppPageViews_At",
                table: "AppPageViews",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_AppPageViews_UserId_At",
                table: "AppPageViews",
                columns: new[] { "UserId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRequestLogs_At",
                table: "AppRequestLogs",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_AppRequestLogs_UserId_At",
                table: "AppRequestLogs",
                columns: new[] { "UserId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRouteDaily_Day_App_Kind_Route",
                table: "AppRouteDaily",
                columns: new[] { "Day", "App", "Kind", "Route" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsageDaily_Day_App_Role",
                table: "AppUsageDaily",
                columns: new[] { "Day", "App", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppErrorLogs");

            migrationBuilder.DropTable(
                name: "AppPageViews");

            migrationBuilder.DropTable(
                name: "AppRequestLogs");

            migrationBuilder.DropTable(
                name: "AppRouteDaily");

            migrationBuilder.DropTable(
                name: "AppUsageDaily");

            // The Metrics page row is kept (harmless, and designations may already reference it).
        }
    }
}
