
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    public partial class v27ConvertCSR1StatusToEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "CSR1"
                ALTER COLUMN "Status" DROP DEFAULT;

                ALTER TABLE "CSR1"
                ALTER COLUMN "Status" TYPE integer
                USING (
                    CASE lower(btrim("Status"))
                        WHEN 'draft' THEN 0
                        WHEN 'submittedforvalidation' THEN 1
                        WHEN 'submitted' THEN 2
                        WHEN 'pending' THEN 3
                        WHEN 'validated' THEN 4
                        WHEN 'approved' THEN 5
                        WHEN 'rejected' THEN 6
                        WHEN 'level1approved' THEN 7
                        WHEN 'level2approved' THEN 8
                        ELSE "Status"::integer
                    END
                );

                ALTER TABLE "CSR1"
                ALTER COLUMN "Status" SET DEFAULT 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "CSR1"
                ALTER COLUMN "Status" DROP DEFAULT;

                ALTER TABLE "CSR1"
                ALTER COLUMN "Status" TYPE character varying(50)
                USING (
                    CASE "Status"
                        WHEN 0 THEN 'Draft'
                        WHEN 1 THEN 'SubmittedForValidation'
                        WHEN 2 THEN 'Submitted'
                        WHEN 3 THEN 'Pending'
                        WHEN 4 THEN 'Validated'
                        WHEN 5 THEN 'Approved'
                        WHEN 6 THEN 'Rejected'
                        WHEN 7 THEN 'Level1Approved'
                        WHEN 8 THEN 'Level2Approved'
                        ELSE "Status"::text
                    END
                );

                ALTER TABLE "CSR1"
                ALTER COLUMN "Status" SET DEFAULT 'Draft';
                """);
        }
    }
}
