using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoTreatments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoTreatments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DemoDocumentationId = table.Column<int>(type: "integer", nullable: false),
                    TreatmentNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FieldOfficerUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CropStageId = table.Column<int>(type: "integer", nullable: true),
                    PlantCondition = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    Dosage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApplicationMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TreatmentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApplicationStage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TreatmentRemarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhysicalParameters = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    YieldParameters = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ObservationNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    HasProblem = table.Column<bool>(type: "boolean", nullable: false),
                    ProblemCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProblemDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Recommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActionTaken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoTreatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoTreatments_CropStages_CropStageId",
                        column: x => x.CropStageId,
                        principalTable: "CropStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemoTreatments_DemoDocumentations_DemoDocumentationId",
                        column: x => x.DemoDocumentationId,
                        principalTable: "DemoDocumentations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemoTreatments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemoTreatmentObservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DemoTreatmentId = table.Column<int>(type: "integer", nullable: false),
                    Observation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoTreatmentObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoTreatmentObservations_DemoTreatments_DemoTreatmentId",
                        column: x => x.DemoTreatmentId,
                        principalTable: "DemoTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemoTreatmentPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DemoTreatmentId = table.Column<int>(type: "integer", nullable: false),
                    PhotoType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoTreatmentPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoTreatmentPhotos_DemoTreatments_DemoTreatmentId",
                        column: x => x.DemoTreatmentId,
                        principalTable: "DemoTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoTreatmentObservations_DemoTreatmentId",
                table: "DemoTreatmentObservations",
                column: "DemoTreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoTreatmentPhotos_DemoTreatmentId",
                table: "DemoTreatmentPhotos",
                column: "DemoTreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoTreatments_CropStageId",
                table: "DemoTreatments",
                column: "CropStageId");

            migrationBuilder.CreateIndex(
                name: "IX_DemoTreatments_DemoDocumentationId_TreatmentNumber",
                table: "DemoTreatments",
                columns: new[] { "DemoDocumentationId", "TreatmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoTreatments_ProductId",
                table: "DemoTreatments",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoTreatmentObservations");

            migrationBuilder.DropTable(
                name: "DemoTreatmentPhotos");

            migrationBuilder.DropTable(
                name: "DemoTreatments");
        }
    }
}
