
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    public partial class v27ConvertCSR2StatusToEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve existing CSR-2 records.
            // Stop if an unsupported status exists.

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "CSR2s"
                        WHERE "Status" IS NULL
                           OR lower(btrim("Status")) NOT IN
                              ('draft', 'submitted', '0', '1')
                    ) THEN
                        RAISE EXCEPTION
                            'CSR2s contains an unsupported Status. Check existing values before migration.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CSR2s"
                ALTER COLUMN "Status" DROP DEFAULT;

                ALTER TABLE "CSR2s"
                ALTER COLUMN "Status" TYPE integer
                USING (
                    CASE lower(btrim("Status"))
                        WHEN 'draft' THEN 0
                        WHEN 'submitted' THEN 1
                        WHEN '0' THEN 0
                        WHEN '1' THEN 1
                    END
                );

                ALTER TABLE "CSR2s"
                ALTER COLUMN "Status" SET DEFAULT 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "CSR2s"
                        WHERE "Status" NOT IN (0, 1)
                    ) THEN
                        RAISE EXCEPTION
                            'CSR2s contains an unsupported integer Status.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CSR2s"
                ALTER COLUMN "Status" DROP DEFAULT;

                ALTER TABLE "CSR2s"
                ALTER COLUMN "Status" TYPE character varying(30)
                USING (
                    CASE "Status"
                        WHEN 0 THEN 'Draft'
                        WHEN 1 THEN 'Submitted'
                    END
                );

                ALTER TABLE "CSR2s"
                ALTER COLUMN "Status" SET DEFAULT 'Draft';
                """);
        }
    }
}
