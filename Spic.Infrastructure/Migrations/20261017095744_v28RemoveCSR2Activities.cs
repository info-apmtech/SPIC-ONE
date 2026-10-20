using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v28RemoveCSR2Activities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CSR2Activities");

            migrationBuilder.AddColumn<string>(
                name: "BillNo",
                table: "CSR2Expenses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillNo",
                table: "CSR2Expenses");

            migrationBuilder.CreateTable(
                name: "CSR2Activities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CSR2Id = table.Column<int>(type: "integer", nullable: false),
                    ActivityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ParticipantsCount = table.Column<int>(type: "integer", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CSR2Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CSR2Activities_CSR2s_CSR2Id",
                        column: x => x.CSR2Id,
                        principalTable: "CSR2s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CSR2Activities_CSR2Id_SortOrder",
                table: "CSR2Activities",
                columns: new[] { "CSR2Id", "SortOrder" });
        }
    }
}
