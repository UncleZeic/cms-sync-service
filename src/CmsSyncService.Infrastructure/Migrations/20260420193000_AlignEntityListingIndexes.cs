using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmsSyncService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignEntityListingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cms_entities_Published_AdminDisabled",
                table: "cms_entities");

            migrationBuilder.DropIndex(
                name: "IX_cms_entities_UpdatedAtUtc_Id",
                table: "cms_entities");

            migrationBuilder.CreateIndex(
                name: "IX_cms_entities_UpdatedAtUtc_desc_Id",
                table: "cms_entities",
                columns: new[] { "UpdatedAtUtc", "Id" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_cms_entities_Published_AdminDisabled_UpdatedAtUtc_desc_Id",
                table: "cms_entities",
                columns: new[] { "Published", "AdminDisabled", "UpdatedAtUtc", "Id" },
                descending: new[] { false, false, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cms_entities_Published_AdminDisabled_UpdatedAtUtc_desc_Id",
                table: "cms_entities");

            migrationBuilder.DropIndex(
                name: "IX_cms_entities_UpdatedAtUtc_desc_Id",
                table: "cms_entities");

            migrationBuilder.CreateIndex(
                name: "IX_cms_entities_Published_AdminDisabled",
                table: "cms_entities",
                columns: new[] { "Published", "AdminDisabled" });

            migrationBuilder.CreateIndex(
                name: "IX_cms_entities_UpdatedAtUtc_Id",
                table: "cms_entities",
                columns: new[] { "UpdatedAtUtc", "Id" });
        }
    }
}
