using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCSR1CropProductsApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // Remove old approval columns from CSR1
            // =========================================================

            migrationBuilder.DropColumn(
                name: "Level1ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level1ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level2ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedAt",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "Level3ApprovedBy",
                table: "CSR1");

            migrationBuilder.DropColumn(
                name: "ProgramTypeId",
                table: "CSR1");


            // =========================================================
            // CSR1Products1
            // Existing table has no data.
            // Replace integer columns with integer[].
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products1");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products1");

            migrationBuilder.AddColumn<int[]>(
                name: "CropId",
                table: "CSR1Products1",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());

            migrationBuilder.AddColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products1",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());


            // =========================================================
            // CSR1Products2
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products2");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products2");

            migrationBuilder.AddColumn<int[]>(
                name: "CropId",
                table: "CSR1Products2",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());

            migrationBuilder.AddColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products2",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());


            // =========================================================
            // CSR1Products3
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products3");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products3");

            migrationBuilder.AddColumn<int[]>(
                name: "CropId",
                table: "CSR1Products3",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());

            migrationBuilder.AddColumn<int[]>(
                name: "ProductId",
                table: "CSR1Products3",
                type: "integer[]",
                nullable: false,
                defaultValue: Array.Empty<int>());


            // =========================================================
            // Create CSR1ApprovalDetails
            // =========================================================

            migrationBuilder.CreateTable(
                name: "CSR1ApprovalDetails",
                columns: table => new
                {
                    Id = table.Column<int>(
                            type: "integer",
                            nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    CSR1Id = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    Level1ApprovedBy = table.Column<string>(
                        type: "text",
                        nullable: true),

                    Level1ApprovedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: true),

                    Level2ApprovedBy = table.Column<string>(
                        type: "text",
                        nullable: true),

                    Level2ApprovedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: true),

                    Level3ApprovedBy = table.Column<string>(
                        type: "text",
                        nullable: true),

                    Level3ApprovedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: true),

                    CreatedBy = table.Column<string>(
                        type: "text",
                        nullable: true),

                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: false),

                    UpdatedBy = table.Column<string>(
                        type: "text",
                        nullable: true),

                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp without time zone",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_CSR1ApprovalDetails",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_CSR1ApprovalDetails_CSR1_CSR1Id",
                        column: x => x.CSR1Id,
                        principalTable: "CSR1",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CSR1ApprovalDetails_CSR1Id",
                table: "CSR1ApprovalDetails",
                column: "CSR1Id");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // Remove approval details table
            // =========================================================

            migrationBuilder.DropTable(
                name: "CSR1ApprovalDetails");


            // =========================================================
            // CSR1Products1
            // Convert structure back to old scalar integer columns.
            // This assumes rollback is only used before new data matters.
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products1");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products1");

            migrationBuilder.AddColumn<int>(
                name: "CropId",
                table: "CSR1Products1",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "CSR1Products1",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            // =========================================================
            // CSR1Products2
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products2");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products2");

            migrationBuilder.AddColumn<int>(
                name: "CropId",
                table: "CSR1Products2",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "CSR1Products2",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            // =========================================================
            // CSR1Products3
            // =========================================================

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "CSR1Products3");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CSR1Products3");

            migrationBuilder.AddColumn<int>(
                name: "CropId",
                table: "CSR1Products3",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "CSR1Products3",
                type: "integer",
                nullable: false,
                defaultValue: 0);


            // =========================================================
            // Restore old CSR1 columns
            // =========================================================

            migrationBuilder.AddColumn<DateTime>(
                name: "Level1ApprovedAt",
                table: "CSR1",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level1ApprovedBy",
                table: "CSR1",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Level2ApprovedAt",
                table: "CSR1",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level2ApprovedBy",
                table: "CSR1",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Level3ApprovedAt",
                table: "CSR1",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Level3ApprovedBy",
                table: "CSR1",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgramTypeId",
                table: "CSR1",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}