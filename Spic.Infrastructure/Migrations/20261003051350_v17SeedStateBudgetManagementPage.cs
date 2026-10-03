using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v17SeedStateBudgetManagementPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The target database already has Pages.Id=108/Key="StateBudgetManagement" (seeded by
            // some other means before this migration existed) - insert only if it's still missing,
            // so this never collides with PK_Pages on an environment that already has it.
            migrationBuilder.Sql(@"
                INSERT INTO ""Pages"" (""Id"", ""CreatedAt"", ""CreatedBy"", ""HasActions"", ""IsActive"", ""Key"", ""Module"", ""Name"", ""SortOrder"", ""UpdatedAt"", ""UpdatedBy"")
                SELECT 108, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'StateBudgetManagement', NULL, 'State Budget Management', 107, TIMESTAMP '2024-01-01 00:00:00', 'System'
                WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Id"" = 108);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally a no-op: Up() only inserts this row when it's missing, so this
            // migration can't tell whether a given environment's row 108 was created by it or
            // pre-existed it. Deleting unconditionally here could destroy a pre-existing row
            // this migration never created, which is explicitly out of scope.
        }
    }
}
