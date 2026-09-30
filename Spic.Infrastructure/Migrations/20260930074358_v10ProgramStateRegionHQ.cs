using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v10ProgramStateRegionHQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetAmount",
                table: "ProgramMasters");

            migrationBuilder.CreateTable(
                name: "ProgramStateBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramStateBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramStateBudgets_ProgramMasters_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "ProgramMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramStateBudgets_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramRegionBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramStateBudgetId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramRegionBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramRegionBudgets_ProgramStateBudgets_ProgramStateBudget~",
                        column: x => x.ProgramStateBudgetId,
                        principalTable: "ProgramStateBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramRegionBudgets_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramHQBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramRegionBudgetId = table.Column<int>(type: "integer", nullable: false),
                    HQId = table.Column<int>(type: "integer", nullable: false),
                    HeadquartersId = table.Column<int>(type: "integer", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramHQBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramHQBudgets_Headquarters_HeadquartersId",
                        column: x => x.HeadquartersId,
                        principalTable: "Headquarters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProgramHQBudgets_ProgramRegionBudgets_ProgramRegionBudgetId",
                        column: x => x.ProgramRegionBudgetId,
                        principalTable: "ProgramRegionBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramHQBudgets_HeadquartersId",
                table: "ProgramHQBudgets",
                column: "HeadquartersId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramHQBudgets_ProgramRegionBudgetId",
                table: "ProgramHQBudgets",
                column: "ProgramRegionBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRegionBudgets_ProgramStateBudgetId",
                table: "ProgramRegionBudgets",
                column: "ProgramStateBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRegionBudgets_RegionId",
                table: "ProgramRegionBudgets",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramStateBudgets_ProgramId",
                table: "ProgramStateBudgets",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramStateBudgets_StateId",
                table: "ProgramStateBudgets",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramHQBudgets");

            migrationBuilder.DropTable(
                name: "ProgramRegionBudgets");

            migrationBuilder.DropTable(
                name: "ProgramStateBudgets");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetAmount",
                table: "ProgramMasters",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
