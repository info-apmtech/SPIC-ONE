using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V32_HelpTranslationReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpReviewers",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Languages = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpReviewers", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "HelpReviewItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SectionId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TranslationHash = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SuggestedTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SuggestedBody = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Word = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SayAs = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReviewerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpReviewItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelpReviewItems_Language",
                table: "HelpReviewItems",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_HelpReviewItems_Module_SectionId_Language",
                table: "HelpReviewItems",
                columns: new[] { "Module", "SectionId", "Language" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpReviewers");

            migrationBuilder.DropTable(
                name: "HelpReviewItems");
        }
    }
}
