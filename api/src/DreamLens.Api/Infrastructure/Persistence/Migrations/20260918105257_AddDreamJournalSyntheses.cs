using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamJournalSyntheses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DreamJournalSyntheses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EncryptedResultJson = table.Column<string>(type: "text", nullable: false),
                    SourceDreamCount = table.Column<int>(type: "integer", nullable: false),
                    SourceLatestDreamAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DreamJournalSyntheses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DreamJournalSyntheses_SourceLatestDreamAt_GeneratedAt",
                table: "DreamJournalSyntheses",
                columns: new[] { "SourceLatestDreamAt", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DreamJournalSyntheses_UserSubject",
                table: "DreamJournalSyntheses",
                column: "UserSubject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DreamJournalSyntheses");
        }
    }
}
