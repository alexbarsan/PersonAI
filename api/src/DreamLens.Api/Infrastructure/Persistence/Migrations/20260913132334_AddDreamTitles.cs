using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Dreams",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Dreams"
                SET "Title" = LEFT(
                    COALESCE(
                        NULLIF(BTRIM("ResultJson"::jsonb ->> 'title'), ''),
                        NULLIF(BTRIM("ResultJson"::jsonb ->> 'summary'), ''),
                        NULLIF(BTRIM("Text"), ''),
                        'Untitled dream'),
                    120)
                WHERE "Title" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Dreams");
        }
    }
}
