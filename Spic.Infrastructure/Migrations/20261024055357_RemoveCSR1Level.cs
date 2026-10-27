using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCSR1Level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 20261022044018_UpdateCSR1CropProductsApprovals already drops these columns; a plain
            // DropColumn here fails with "column does not exist". IF EXISTS keeps it safe everywhere.
            migrationBuilder.Sql("""
                ALTER TABLE "CSR1"
                    DROP COLUMN IF EXISTS "Level1ApprovedAt",
                    DROP COLUMN IF EXISTS "Level1ApprovedBy",
                    DROP COLUMN IF EXISTS "Level2ApprovedAt",
                    DROP COLUMN IF EXISTS "Level2ApprovedBy",
                    DROP COLUMN IF EXISTS "Level3ApprovedAt",
                    DROP COLUMN IF EXISTS "Level3ApprovedBy";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
