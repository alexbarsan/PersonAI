using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamFactProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizationVersion",
                table: "DreamFacts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "v1");

            migrationBuilder.AddColumn<string>(
                name: "SourceField",
                table: "DreamFacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizationVersion",
                table: "DreamFacts");

            migrationBuilder.DropColumn(
                name: "SourceField",
                table: "DreamFacts");
        }
    }
}
