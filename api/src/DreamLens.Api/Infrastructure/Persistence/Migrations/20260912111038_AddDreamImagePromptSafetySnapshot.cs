using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamImagePromptSafetySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModerationCategoriesJson",
                table: "DreamImages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "PromptMode",
                table: "DreamImages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "legacy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModerationCategoriesJson",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "PromptMode",
                table: "DreamImages");
        }
    }
}
