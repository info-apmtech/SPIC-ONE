using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicityMaterialCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Category 1 = General (PublicityMaterialCategory enum: Specific = 0, General = 1) - every
            // pre-existing material row (seeded before this column existed, previously shown to both
            // Specific and General Product undifferentiated) defaults to General, the least disruptive
            // bucket, rather than the CLR-default 0 (Specific) that AddColumn would otherwise use.
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "PublicityMaterialTypes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Publicity Material Master initial catalogue (idempotent by Name, same pattern as the
            // original V31_PublicityTracking seed) - Specific Product: Leaflets/Stickers/Posters/Banners
            // (Excel-import + per-SKU-quantity flow); General Product: Streamers/Dangler/Inflatables/
            // Dummies/Product Cut-out/Rollup-Stande/Lama Standee (manual-entry flow, also reused as-is
            // by Gift Articles). SortOrder continues after the 8 pre-existing rows (1-8).
            migrationBuilder.Sql(@"
INSERT INTO ""PublicityMaterialTypes"" (""Name"",""Code"",""IsActive"",""SortOrder"",""Category"")
SELECT v.n, v.c, TRUE, v.o, v.cat FROM (VALUES
 ('Leaflets','LEAFLETS',9,0),('Stickers','STICKERS',10,0),('Posters','POSTERS',11,0),('Banners','BANNERS',12,0),
 ('Streamers','STREAMERS',13,1),('Dangler','DANGLER',14,1),('Inflatables','INFLATABLES',15,1),('Dummies','DUMMIES',16,1),
 ('Product Cut-out','PRODCUTOUT',17,1),('Rollup-Stande','ROLLUPSTANDE',18,1),('Lama Standee','LAMASTANDEE',19,1)
) AS v(n,c,o,cat)
WHERE NOT EXISTS (SELECT 1 FROM ""PublicityMaterialTypes"" m WHERE m.""Name"" = v.n);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PublicityMaterialTypes");
        }
    }
}
