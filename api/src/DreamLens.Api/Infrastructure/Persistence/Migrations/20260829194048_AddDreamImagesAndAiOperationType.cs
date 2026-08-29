using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamImagesAndAiOperationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OperationType",
                table: "AiCostLedger",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "dream.interpretation");

            migrationBuilder.CreateTable(
                name: "DreamImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DreamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Style = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssetKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DreamImages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DreamImages_DreamId_UserSubject_CreatedAt",
                table: "DreamImages",
                columns: new[] { "DreamId", "UserSubject", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DreamImages_UserSubject_CreatedAt",
                table: "DreamImages",
                columns: new[] { "UserSubject", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DreamImages");

            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "AiCostLedger");
        }
    }
}
