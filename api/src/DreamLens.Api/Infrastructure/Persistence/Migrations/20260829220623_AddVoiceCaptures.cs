using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceCaptures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoiceCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Language = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    RetainRecording = table.Column<bool>(type: "boolean", nullable: false),
                    SourceAssetKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Transcript = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceCaptures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceCaptures_UserSubject_CreatedAt",
                table: "VoiceCaptures",
                columns: new[] { "UserSubject", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceCaptures_UserSubject_Status_CreatedAt",
                table: "VoiceCaptures",
                columns: new[] { "UserSubject", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceCaptures");
        }
    }
}
