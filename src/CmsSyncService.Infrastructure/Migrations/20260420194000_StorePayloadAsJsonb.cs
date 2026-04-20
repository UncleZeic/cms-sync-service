using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmsSyncService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StorePayloadAsJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "cms_entities"
                ALTER COLUMN "Payload" TYPE jsonb
                USING "Payload"::jsonb;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "cms_entities"
                ALTER COLUMN "Payload" TYPE text
                USING "Payload"::text;
                """);
        }
    }
}
