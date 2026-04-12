using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmsSyncService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtUtcIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_cms_entities_UpdatedAtUtc_Id",
                table: "cms_entities",
                columns: new[] { "UpdatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cms_entities_UpdatedAtUtc_Id",
                table: "cms_entities");
        }
    }
}
