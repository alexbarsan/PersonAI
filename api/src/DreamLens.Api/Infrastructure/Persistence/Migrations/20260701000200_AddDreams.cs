using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamLens.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DreamLensDbContext))]
[Migration("20260701000200_AddDreams")]
public partial class AddDreams : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Dreams",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Mood = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                SleepQuality = table.Column<int>(type: "integer", nullable: true),
                TagsJson = table.Column<string>(type: "text", nullable: false),
                OccurredAt = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ResultJson = table.Column<string>(type: "text", nullable: true),
                ErrorMessage = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Dreams", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Dreams_UserSubject_CreatedAt",
            table: "Dreams",
            columns: ["UserSubject", "CreatedAt"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Dreams");
    }
}
