using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V33_HelpFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SectionId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Helpful = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelpFeedbacks_Day",
                table: "HelpFeedbacks",
                column: "Day");

            migrationBuilder.CreateIndex(
                name: "IX_HelpFeedbacks_UserId_Module_SectionId_Day",
                table: "HelpFeedbacks",
                columns: new[] { "UserId", "Module", "SectionId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpFeedbacks");
        }
    }
}
