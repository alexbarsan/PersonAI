using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalMetadataAndAnonymization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JournalNote",
                table: "Dreams",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnonymizationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingUserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequesterPseudonym = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReviewedBySubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymizationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnonymizedUserTombstones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectPseudonym = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AnonymizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymizedUserTombstones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymizationRequests_RequestingUserSubject_Status",
                table: "AnonymizationRequests",
                columns: new[] { "RequestingUserSubject", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymizationRequests_Status_RequestedAt",
                table: "AnonymizationRequests",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymizedUserTombstones_SubjectPseudonym",
                table: "AnonymizedUserTombstones",
                column: "SubjectPseudonym",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnonymizationRequests");

            migrationBuilder.DropTable(
                name: "AnonymizedUserTombstones");

            migrationBuilder.DropColumn(
                name: "JournalNote",
                table: "Dreams");
        }
    }
}
