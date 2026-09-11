using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamImageRequestSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedPrompt",
                table: "DreamImages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCostUsd",
                table: "DreamImages",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "DreamImages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "DreamImages",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PromptVersion",
                table: "DreamImages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "DreamImages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "DreamImages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "DreamImages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPrompt",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "EstimatedCostUsd",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "DreamImages");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "DreamImages");
        }
    }
}
