using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBudgetProgramStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'BudgetProgramMains'
          AND column_name = 'Status'
          AND data_type IN ('text', 'character varying')
    ) THEN
        ALTER TABLE ""BudgetProgramMains""
        ALTER COLUMN ""Status"" TYPE integer
        USING (
            CASE TRIM(""Status"")
                WHEN 'Draft' THEN 0
                WHEN 'Submitted' THEN 1
                WHEN 'Valid' THEN 2
                WHEN 'Validated' THEN 2
                WHEN 'Approved' THEN 3
                WHEN 'Rejected' THEN 4
                ELSE 0
            END
        );
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'BudgetProgramMains'
          AND column_name = 'Status'
          AND data_type = 'integer'
    ) THEN
        ALTER TABLE ""BudgetProgramMains""
        ALTER COLUMN ""Status"" TYPE text
        USING (
            CASE ""Status""
                WHEN 0 THEN 'Draft'
                WHEN 1 THEN 'Submitted'
                WHEN 2 THEN 'Validated'
                WHEN 3 THEN 'Approved'
                WHEN 4 THEN 'Rejected'
                ELSE 'Draft'
            END
        );
    END IF;
END $$;
");
        }
    }
}