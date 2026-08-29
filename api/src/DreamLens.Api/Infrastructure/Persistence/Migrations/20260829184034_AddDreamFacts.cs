using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DreamFacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DreamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FactType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    ExtractionConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    SourceSchemaVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DreamFacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DreamFacts_DreamId_FactType_NormalizedValue",
                table: "DreamFacts",
                columns: new[] { "DreamId", "FactType", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DreamFacts_UserSubject_CreatedAt",
                table: "DreamFacts",
                columns: new[] { "UserSubject", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DreamFacts_UserSubject_FactType_NormalizedValue",
                table: "DreamFacts",
                columns: new[] { "UserSubject", "FactType", "NormalizedValue" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DreamFacts");
        }
    }
}
