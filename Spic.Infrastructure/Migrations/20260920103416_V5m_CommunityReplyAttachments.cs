using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V5m_CommunityReplyAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplyId",
                table: "CommunityPostAttachments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPostAttachments_ReplyId",
                table: "CommunityPostAttachments",
                column: "ReplyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunityPostAttachments_ReplyId",
                table: "CommunityPostAttachments");

            migrationBuilder.DropColumn(
                name: "ReplyId",
                table: "CommunityPostAttachments");
        }
    }
}
