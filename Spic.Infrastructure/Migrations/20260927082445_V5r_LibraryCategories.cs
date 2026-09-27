using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5r_LibraryCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, computedColumnSql: "lower(\"Name\")", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryCategories_LibraryCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "LibraryCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryCategories_Kind_NormalizedName",
                table: "LibraryCategories",
                columns: new[] { "Kind", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryCategories_ParentCategoryId",
                table: "LibraryCategories",
                column: "ParentCategoryId");

            // Seed the defaults LibraryController used to hard-code (DefaultCategories /
            // DefaultSubCategories), with explicit ids so a re-run is a no-op. Sub-categories are
            // attached to the category they belong to; an admin can move them on the
            // Library Categories page. The identity is moved past the seeded ids afterwards.
            migrationBuilder.Sql(@"
INSERT INTO ""LibraryCategories"" (""Id"",""Kind"",""Name"",""ParentCategoryId"",""SortOrder"",""IsActive"",""CreatedBy"",""CreatedAt"",""UpdatedBy"",""UpdatedAt"") VALUES
 (1, 0, 'Phosphatic Fertilizers',  NULL, 1, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (2, 0, 'Nitrogenous Fertilizers', NULL, 2, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (3, 0, 'Potassic Fertilizers',    NULL, 3, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (4, 0, 'Micronutrients',          NULL, 4, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (5, 0, 'Organic & Bio',           NULL, 5, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (6, 0, 'Speciality Products',     NULL, 6, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00')
ON CONFLICT DO NOTHING;
INSERT INTO ""LibraryCategories"" (""Id"",""Kind"",""Name"",""ParentCategoryId"",""SortOrder"",""IsActive"",""CreatedBy"",""CreatedAt"",""UpdatedBy"",""UpdatedAt"") VALUES
 (101, 1, 'DAP',              1, 1, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (102, 1, 'Complex',          1, 2, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (103, 1, 'Urea',             2, 1, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (104, 1, 'Soil Conditioner', 5, 1, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00'),
 (105, 1, 'Water Soluble',    6, 1, TRUE, 'System', TIMESTAMP '2024-01-01 00:00:00', 'System', TIMESTAMP '2024-01-01 00:00:00')
ON CONFLICT DO NOTHING;
SELECT setval(pg_get_serial_sequence('""LibraryCategories""', 'Id'),
              GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""LibraryCategories""), 1000));
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryCategories");
        }
    }
}
