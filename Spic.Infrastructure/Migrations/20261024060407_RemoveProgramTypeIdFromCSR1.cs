using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    public partial class RemoveProgramTypeIdFromCSR1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 20261022044018_UpdateCSR1CropProductsApprovals already drops this column.
            migrationBuilder.Sql("""ALTER TABLE "CSR1" DROP COLUMN IF EXISTS "ProgramTypeId";""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing: the column is owned (and restored) by UpdateCSR1CropProductsApprovals.Down.
        }
    }
}