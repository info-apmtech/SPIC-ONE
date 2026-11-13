using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V31_PublicityTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicityFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicityMaterialTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityMaterialTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicityNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TargetUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TargetRole = table.Column<int>(type: "integer", nullable: true),
                    TargetHqId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicityRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IndentNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaterialTypeId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    OriginalQuantity = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    HeadquarterId = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicityRequests_PublicityMaterialTypes_MaterialTypeId",
                        column: x => x.MaterialTypeId,
                        principalTable: "PublicityMaterialTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicityDispatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceHqId = table.Column<int>(type: "integer", nullable: true),
                    SourceRequestId = table.Column<int>(type: "integer", nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Transporter = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TrackingNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VehicleNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ResponsibleUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResponsibleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ResponsibleRole = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    DispatchDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CurrentLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InTransitAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProofByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProofAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicityDispatches_PublicityRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "PublicityRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    DispatchId = table.Column<int>(type: "integer", nullable: true),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActorRole = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicityEvents_PublicityRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "PublicityRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicityAcknowledgements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispatchId = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQty = table.Column<int>(type: "integer", nullable: false),
                    IsDamaged = table.Column<bool>(type: "boolean", nullable: false),
                    DamagedQty = table.Column<int>(type: "integer", nullable: false),
                    AcknowledgedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AcknowledgedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    IsDraft = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicityAcknowledgements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicityAcknowledgements_PublicityDispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalTable: "PublicityDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicityAcknowledgements_DispatchId",
                table: "PublicityAcknowledgements",
                column: "DispatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDispatches_DispatchNo",
                table: "PublicityDispatches",
                column: "DispatchNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDispatches_RequestId",
                table: "PublicityDispatches",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDispatches_SourceHqId",
                table: "PublicityDispatches",
                column: "SourceHqId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityDispatches_SourceRequestId",
                table: "PublicityDispatches",
                column: "SourceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityEvents_RequestId",
                table: "PublicityEvents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityFiles_RequestId",
                table: "PublicityFiles",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityNotifications_TargetRole_TargetHqId",
                table: "PublicityNotifications",
                columns: new[] { "TargetRole", "TargetHqId" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicityNotifications_TargetUserId",
                table: "PublicityNotifications",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRequests_CreatedByUserId",
                table: "PublicityRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRequests_HeadquarterId",
                table: "PublicityRequests",
                column: "HeadquarterId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRequests_IndentNo",
                table: "PublicityRequests",
                column: "IndentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRequests_MaterialTypeId",
                table: "PublicityRequests",
                column: "MaterialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicityRequests_Status",
                table: "PublicityRequests",
                column: "Status");

            // Material master rows (by Name, idempotent; ids are never fixed). PublicityRequest /
            // PublicityReview need no page-catalogue row: RemovePagePermissionInfrastructure (2026-10-15)
            // retired the database-driven "Pages" table in favour of plain PagePermission enum members
            // (Userinfo.cs), so a page key is just an enum member + RoleAccess token, nothing to seed here.
            migrationBuilder.Sql(@"
INSERT INTO ""PublicityMaterialTypes"" (""Name"",""Code"",""IsActive"",""SortOrder"")
SELECT v.n, v.c, TRUE, v.o FROM (VALUES ('Poster','POSTER',1),('Banner','BANNER',2),('Leaflet','LEAFLET',3),('Brochure','BROCHURE',4),
 ('Standee','STANDEE',5),('Wall Painting','WALL',6),('Calendar','CALENDAR',7),('Danglers','DANGLER',8)) AS v(n,c,o)
WHERE NOT EXISTS (SELECT 1 FROM ""PublicityMaterialTypes"" m WHERE m.""Name"" = v.n);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicityAcknowledgements");

            migrationBuilder.DropTable(
                name: "PublicityEvents");

            migrationBuilder.DropTable(
                name: "PublicityFiles");

            migrationBuilder.DropTable(
                name: "PublicityNotifications");

            migrationBuilder.DropTable(
                name: "PublicityDispatches");

            migrationBuilder.DropTable(
                name: "PublicityRequests");

            migrationBuilder.DropTable(
                name: "PublicityMaterialTypes");
        }
    }
}
