using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5n_SasSampleCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SampleConsignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CourierService = table.Column<string>(type: "text", nullable: false),
                    TrackingNumber = table.Column<string>(type: "text", nullable: false),
                    TrackingLink = table.Column<string>(type: "text", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PackageCount = table.Column<int>(type: "integer", nullable: false),
                    PackageWeightKg = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    PackagingType = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    ContactMobile = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DispatchedByUserId = table.Column<string>(type: "text", nullable: false),
                    DispatchedByName = table.Column<string>(type: "text", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleConsignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SasCouriers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TrackingUrlTemplate = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SasCouriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SasFarmers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Mobile = table.Column<string>(type: "text", nullable: false),
                    Address1 = table.Column<string>(type: "text", nullable: true),
                    Address2 = table.Column<string>(type: "text", nullable: true),
                    Village = table.Column<string>(type: "text", nullable: true),
                    Taluk = table.Column<string>(type: "text", nullable: true),
                    DistrictId = table.Column<int>(type: "integer", nullable: true),
                    DistrictName = table.Column<string>(type: "text", nullable: true),
                    StateId = table.Column<int>(type: "integer", nullable: true),
                    StateName = table.Column<string>(type: "text", nullable: true),
                    PinCode = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    SurveyNumber = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SasFarmers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SasSampleCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SampleType = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    AmountPerSample = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    NoOfTests = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SasSampleCharges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SasStatusEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollectionId = table.Column<int>(type: "integer", nullable: true),
                    ConsignmentId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ByName = table.Column<string>(type: "text", nullable: true),
                    At = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SasStatusEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsignmentPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConsignmentId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    StoredPath = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsignmentPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsignmentPhotos_SampleConsignments_ConsignmentId",
                        column: x => x.ConsignmentId,
                        principalTable: "SampleConsignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SampleCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CollectedByUserId = table.Column<string>(type: "text", nullable: false),
                    CollectedByName = table.Column<string>(type: "text", nullable: false),
                    CollectedByRole = table.Column<string>(type: "text", nullable: true),
                    HeadquarterId = table.Column<int>(type: "integer", nullable: true),
                    LocationName = table.Column<string>(type: "text", nullable: true),
                    PaymentType = table.Column<int>(type: "integer", nullable: false),
                    PaidCategory = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    ConsignmentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleCollections_SampleConsignments_ConsignmentId",
                        column: x => x.ConsignmentId,
                        principalTable: "SampleConsignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SampleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollectionId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    FarmerId = table.Column<int>(type: "integer", nullable: false),
                    SampleType = table.Column<int>(type: "integer", nullable: false),
                    Crop1 = table.Column<string>(type: "text", nullable: true),
                    Crop2 = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    NoOfTests = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleItems_SampleCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "SampleCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SampleItems_SasFarmers_FarmerId",
                        column: x => x.FarmerId,
                        principalTable: "SasFarmers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SamplePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollectionId = table.Column<int>(type: "integer", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: false),
                    BankGateway = table.Column<string>(type: "text", nullable: true),
                    UtrNumber = table.Column<string>(type: "text", nullable: true),
                    PaidByName = table.Column<string>(type: "text", nullable: false),
                    ContactNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    ProofPath = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByName = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RejectReason = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamplePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SamplePayments_SampleCollections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "SampleCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SampleLabResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SampleItemId = table.Column<int>(type: "integer", nullable: false),
                    Parameter = table.Column<string>(type: "text", nullable: false),
                    NormalRange = table.Column<string>(type: "text", nullable: true),
                    EnteredValue = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    ResultLabel = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Hint = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    EnteredByName = table.Column<string>(type: "text", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleLabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleLabResults_SampleItems_SampleItemId",
                        column: x => x.SampleItemId,
                        principalTable: "SampleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Idempotent catalogue rows (live-site safety, see docs/sas-sample-collection-plan.md).
            migrationBuilder.Sql(@"
-- SampleCollection: fixed id when free, otherwise a generated id (a page registered through Page Management on
-- the live site may already hold that id); never fails, never touches existing rows.
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (72, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SampleCollection', 'SAS', 'Sample Collection', 71, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'SampleCollection', 'SAS', 'Sample Collection', 71, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'SampleCollection');
-- ConsignmentHistory: fixed id when free, otherwise a generated id (a page registered through Page Management on
-- the live site may already hold that id); never fails, never touches existing rows.
INSERT INTO ""Pages"" (""Id"",""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"") VALUES (73, TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'ConsignmentHistory', 'SAS', 'Consignment History', 72, TIMESTAMP '2024-01-01 00:00:00', 'System') ON CONFLICT (""Id"") DO NOTHING;
INSERT INTO ""Pages"" (""CreatedAt"",""CreatedBy"",""HasActions"",""IsActive"",""Key"",""Module"",""Name"",""SortOrder"",""UpdatedAt"",""UpdatedBy"")
SELECT TIMESTAMP '2024-01-01 00:00:00', 'System', TRUE, TRUE, 'ConsignmentHistory', 'SAS', 'Consignment History', 72, TIMESTAMP '2024-01-01 00:00:00', 'System'
WHERE NOT EXISTS (SELECT 1 FROM ""Pages"" WHERE ""Key"" = 'ConsignmentHistory');
SELECT setval(pg_get_serial_sequence('""Pages""', 'Id'), GREATEST((SELECT COALESCE(MAX(""Id""), 0) FROM ""Pages"") + 1, nextval(pg_get_serial_sequence('""Pages""', 'Id'))), false);
");

            migrationBuilder.InsertData(
                table: "SasCouriers",
                columns: new[] { "Id", "IsActive", "Name", "TrackingUrlTemplate" },
                values: new object[,]
                {
                    { 1, true, "Professional Courier", "https://www.tpcindia.com/Tracking2014.aspx?id={0}" },
                    { 2, true, "Blue Dart Express", "https://www.bluedart.com/tracking?awb={0}" },
                    { 3, true, "DTDC", "https://www.dtdc.in/tracking.asp?awb={0}" },
                    { 4, true, "India Post", "https://www.indiapost.gov.in/_layouts/15/DOP.Portal.Tracking/TrackConsignment.aspx" }
                });

            migrationBuilder.InsertData(
                table: "SasSampleCharges",
                columns: new[] { "Id", "AmountPerSample", "Category", "IsActive", "NoOfTests", "SampleType" },
                values: new object[,]
                {
                    { 1, 100m, 0, true, 1, 0 },
                    { 2, 100m, 0, true, 1, 1 },
                    { 3, 150m, 0, true, 2, 2 },
                    { 4, 150m, 1, true, 1, 0 },
                    { 5, 150m, 1, true, 1, 1 },
                    { 6, 200m, 1, true, 2, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsignmentPhotos_ConsignmentId",
                table: "ConsignmentPhotos",
                column: "ConsignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_Code",
                table: "SampleCollections",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_CollectedByUserId",
                table: "SampleCollections",
                column: "CollectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_CollectionDate",
                table: "SampleCollections",
                column: "CollectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_ConsignmentId",
                table: "SampleCollections",
                column: "ConsignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleCollections_Status",
                table: "SampleCollections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SampleConsignments_Code",
                table: "SampleConsignments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SampleConsignments_DispatchedAt",
                table: "SampleConsignments",
                column: "DispatchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SampleConsignments_Status",
                table: "SampleConsignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SampleItems_CollectionId",
                table: "SampleItems",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleItems_FarmerId",
                table: "SampleItems",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleLabResults_SampleItemId",
                table: "SampleLabResults",
                column: "SampleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SamplePayments_CollectionId",
                table: "SamplePayments",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SamplePayments_Status",
                table: "SamplePayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SasFarmers_Mobile",
                table: "SasFarmers",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_SasFarmers_Name",
                table: "SasFarmers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SasFarmers_UserId",
                table: "SasFarmers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SasSampleCharges_SampleType_Category",
                table: "SasSampleCharges",
                columns: new[] { "SampleType", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SasStatusEvents_CollectionId",
                table: "SasStatusEvents",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SasStatusEvents_ConsignmentId",
                table: "SasStatusEvents",
                column: "ConsignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsignmentPhotos");

            migrationBuilder.DropTable(
                name: "SampleLabResults");

            migrationBuilder.DropTable(
                name: "SamplePayments");

            migrationBuilder.DropTable(
                name: "SasCouriers");

            migrationBuilder.DropTable(
                name: "SasSampleCharges");

            migrationBuilder.DropTable(
                name: "SasStatusEvents");

            migrationBuilder.DropTable(
                name: "SampleItems");

            migrationBuilder.DropTable(
                name: "SampleCollections");

            migrationBuilder.DropTable(
                name: "SasFarmers");

            migrationBuilder.DropTable(
                name: "SampleConsignments");

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Pages",
                keyColumn: "Id",
                keyValue: 73);
        }
    }
}
