using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePagePermissionInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The ApplicationPage ("Pages") and DesignationPermission
            // ("DesignationPermissions") entities were removed from the model because
            // the database-driven page permission system is no longer used.
            //
            // Their tables are INTENTIONALLY PRESERVED: this migration is a no-op and
            // must not drop any data. It exists only so the model snapshot moves in
            // step with the code, which keeps future migrations from recreating (or
            // later dropping) those tables.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op for symmetry with Up: nothing was changed, so nothing to reverse.
        }
    }
}
