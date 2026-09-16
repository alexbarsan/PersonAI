using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredNamesAndPremiumGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailNormalized",
                table: "UserProfiles",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredName",
                table: "UserProfiles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PremiumGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EmailNormalized = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    GrantedBySubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    GrantedByEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedBySubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumGrants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_EmailNormalized",
                table: "UserProfiles",
                column: "EmailNormalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PremiumGrants_EmailNormalized_RevokedAt",
                table: "PremiumGrants",
                columns: new[] { "EmailNormalized", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumGrants_UserSubject",
                table: "PremiumGrants",
                column: "UserSubject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PremiumGrants");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_EmailNormalized",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "EmailNormalized",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredName",
                table: "UserProfiles");
        }
    }
}
