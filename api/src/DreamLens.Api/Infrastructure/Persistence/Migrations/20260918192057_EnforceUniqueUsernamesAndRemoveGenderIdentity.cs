using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueUsernamesAndRemoveGenderIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenderIdentity",
                table: "UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "PreferredNameNormalized",
                table: "UserProfiles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "UserProfiles"
                SET "PreferredNameNormalized" = upper(btrim("PreferredName"))
                WHERE "PreferredName" IS NOT NULL
                  AND btrim("PreferredName") <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_PreferredNameNormalized",
                table: "UserProfiles",
                column: "PreferredNameNormalized",
                unique: true,
                filter: "\"PreferredNameNormalized\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_PreferredNameNormalized",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredNameNormalized",
                table: "UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "GenderIdentity",
                table: "UserProfiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
