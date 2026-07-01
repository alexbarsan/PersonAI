using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DreamLensDbContext))]
[Migration("20260701000100_AddUserProfiles")]
public partial class AddUserProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Age = table.Column<int>(type: "integer", nullable: true),
                Sex = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                GenderIdentity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Timezone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EncryptedTraitsJson = table.Column<string>(type: "text", nullable: false),
                ConsentAiProcessing = table.Column<bool>(type: "boolean", nullable: false),
                ConsentSensitiveTraits = table.Column<bool>(type: "boolean", nullable: false),
                ConsentHistoryUse = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserProfiles", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserProfiles_UserSubject",
            table: "UserProfiles",
            column: "UserSubject",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserProfiles");
    }
}
