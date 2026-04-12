using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmsSyncService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndex_Published_AdminDisabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_cms_entities_Published_AdminDisabled",
                table: "cms_entities",
                columns: new[] { "Published", "AdminDisabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cms_entities_Published_AdminDisabled",
                table: "cms_entities");
        }
    }
}
