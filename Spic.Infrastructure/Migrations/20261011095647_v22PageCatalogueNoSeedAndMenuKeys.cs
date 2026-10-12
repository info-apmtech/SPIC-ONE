using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <summary>
    /// 2026-10-11, found by the pre-deploy dry run of the Abu merge. The enum-based HasData seed of
    /// the "Pages" catalogue was removed from AppDbContext (it turned every PagePermission change
    /// into id-based INSERT / UPDATE / DELETE rows that collided with the live site), but the model
    /// snapshot still carried the 135 seed rows, so EF scaffolded DeleteData for every page row.
    /// This migration brings the snapshot in line WITHOUT touching existing rows, and adds the page
    /// keys introduced since v20 by Key when missing (same rule as V5s / V5t / v19 / v20: sequence
    /// first, generated ids, never update or delete). Page rows are owned by the live database and
    /// Page Management from here on.
    /// </summary>
    public partial class v22PageCatalogueNoSeedAndMenuKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SELECT setval(pg_get_serial_sequence('""Pages""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""Pages"") + 1, nextval(pg_get_serial_sequence('""Pages""', 'Id'))), false);
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'AllocationRequestReview', NULL, 'Allocation Request Review', 127, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'AllocationRequestReview');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'Activities', NULL, 'Activities', 128, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'Activities');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'Farmers', NULL, 'Farmers', 129, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'Farmers');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'Alerts', NULL, 'Alerts', 130, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'Alerts');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'StartDocumentation', NULL, 'Start Documentation', 131, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'StartDocumentation');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'DemoDetails', NULL, 'Demo Details', 132, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'DemoDetails');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'TreatmentDetails', NULL, 'Treatment Details', 133, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'TreatmentDetails');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'Treatment01', NULL, 'Treatment01', 134, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'Treatment01');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'Treatment02', NULL, 'Treatment02', 135, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'Treatment02');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'TreatmentDemoDetails', NULL, 'Treatment Demo Details', 136, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'TreatmentDemoDetails');
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'GHAdmin', NULL, 'GHAdmin', 137, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'GHAdmin');
SELECT setval(pg_get_serial_sequence('""Pages""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""Pages"") + 1, nextval(pg_get_serial_sequence('""Pages""', 'Id'))), false);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Page rows are left in place: they are owned by the live database and may predate this migration.
        }
    }
}
