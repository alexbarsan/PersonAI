using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSensitiveSafetyReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensitiveDreamSafetyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DreamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SubjectPseudonym = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReviewRequired = table.Column<bool>(type: "boolean", nullable: false),
                    RestrictsElaboration = table.Column<bool>(type: "boolean", nullable: false),
                    EncryptedDreamText = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveDreamSafetyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensitiveDreamSafetyEvents_Dreams_DreamId",
                        column: x => x.DreamId,
                        principalTable: "Dreams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SensitiveReviewAccessAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SafetyEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveReviewAccessAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensitiveReviewAccessAudits_SensitiveDreamSafetyEvents_Safe~",
                        column: x => x.SafetyEventId,
                        principalTable: "SensitiveDreamSafetyEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SensitiveReviewNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SafetyEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DreamId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectPseudonym = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Route = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveReviewNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensitiveReviewNotifications_SensitiveDreamSafetyEvents_Saf~",
                        column: x => x.SafetyEventId,
                        principalTable: "SensitiveDreamSafetyEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveDreamSafetyEvents_DreamId",
                table: "SensitiveDreamSafetyEvents",
                column: "DreamId");

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveDreamSafetyEvents_Status_ExpiresAt",
                table: "SensitiveDreamSafetyEvents",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveDreamSafetyEvents_UserSubject_DreamId",
                table: "SensitiveDreamSafetyEvents",
                columns: new[] { "UserSubject", "DreamId" });

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveReviewAccessAudits_SafetyEventId_AccessedAt",
                table: "SensitiveReviewAccessAudits",
                columns: new[] { "SafetyEventId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveReviewNotifications_SafetyEventId",
                table: "SensitiveReviewNotifications",
                column: "SafetyEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveReviewNotifications_Status_CreatedAt",
                table: "SensitiveReviewNotifications",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensitiveReviewAccessAudits");

            migrationBuilder.DropTable(
                name: "SensitiveReviewNotifications");

            migrationBuilder.DropTable(
                name: "SensitiveDreamSafetyEvents");
        }
    }
}
